using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[InitializeOnLoad]
public static class WorldMapEventSetup
{
    const string Pending="Devorya.WorldMapEventChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static WorldMapEventSetup(){EditorApplication.playModeStateChanged+=state=>{
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){
            SessionState.SetBool(Pending,false);
            new GameObject("World map event checks").AddComponent<PieceAnimationManager>().StartCoroutine(Guard(Checks()));
        }
    };}
    static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Set(object target,string field,object value){target.GetType().GetField(field,Flags).SetValue(target,value);}
    static T Get<T>(object target,string field){return (T)target.GetType().GetField(field,Flags).GetValue(target);}
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    public static void InstallAndCheck()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run in an isolated batch editor.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/WorldMapScene.unity");
        var guide=UnityEngine.Object.FindAnyObjectByType<EventGuideUI>(FindObjectsInactive.Include);
        var dayNight=UnityEngine.Object.FindAnyObjectByType<WorldMapDayNightLightingController>();
        if(guide==null||dayNight==null)throw new Exception("World map dialogue/lighting missing");
        var controller=UnityEngine.Object.FindAnyObjectByType<WorldMapEventController>();
        if(controller==null)controller=new GameObject("WorldMapEventController").AddComponent<WorldMapEventController>();
        var canvas=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas && c.gameObject.scene==scene).ToArray();
        var glitch=UnityEngine.Object.FindAnyObjectByType<EventScreenGlitchController>();
        if(glitch==null){
            var overlay=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Run/ScreenGlitchCanvas.prefab"));
            overlay.name="WorldMapGlitchCanvas";
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Run/ScreenGlitchController.prefab"));
            glitch=go.GetComponent<EventScreenGlitchController>();Assign(glitch,"screenOverlay",overlay.GetComponentInChildren<RawImage>(true));
        }
        Image flash=Get<Image>(controller,"flashImage");
        if(flash==null){
            var flashCanvas=new GameObject("WorldMapFlashCanvas",typeof(Canvas),typeof(CanvasScaler));
            var c=flashCanvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=6;
            var go=new GameObject("FlashOverlay",typeof(RectTransform),typeof(Image));go.transform.SetParent(flashCanvas.transform,false);
            flash=go.GetComponent<Image>();flash.raycastTarget=false;flash.color=Color.clear;
            flash.rectTransform.anchorMin=Vector2.zero;flash.rectTransform.anchorMax=Vector2.one;flash.rectTransform.offsetMin=flash.rectTransform.offsetMax=Vector2.zero;go.SetActive(false);
        }
        var groups=new List<CanvasGroup>();
        foreach(var c in canvas)CollectUI(c.transform,guide.transform,glitch,flash,groups);
        foreach(var group in groups) Debug.Log("World map UI target: "+group.name+" / "+group.gameObject.scene.path+" / "+group.hideFlags);
        Assign(controller,"dialogueUI",guide);Assign(controller,"glitch",glitch);Assign(controller,"flashImage",flash);Assign(controller,"dayNight",dayNight);
        var light=(Light2D)new SerializedObject(dayNight).FindProperty("globalLight").objectReferenceValue;
        Assign(controller,"globalLight",light);
        var serialized=new SerializedObject(controller);var targets=serialized.FindProperty("controlledUI");targets.arraySize=groups.Count;
        for(int i=0;i<groups.Count;i++)targets.GetArrayElementAtIndex(i).objectReferenceValue=groups[i];serialized.ApplyModifiedPropertiesWithoutUndo();
        const string folder="Assets/Devorya/Data/Event/WorldMapEvents";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Devorya/Data/Event","WorldMapEvents");
        var data=AssetDatabase.LoadAssetAtPath<WorldMapEventData>(folder+"/월드맵_기본 이벤트.asset");
        if(data==null){data=ScriptableObject.CreateInstance<WorldMapEventData>();data.eventName="월드맵 기본 이벤트";AssetDatabase.CreateAsset(data,folder+"/월드맵_기본 이벤트.asset");}
        Assign(controller,"eventData",data);
        if(AssetDatabase.LoadAssetAtPath<WorldMapEventData>(folder+"/월드맵_연출 예제.asset")==null){
            var example=ScriptableObject.CreateInstance<WorldMapEventData>();example.eventName="월드맵 연출 예제";
            example.steps.Add(new WorldMapEventStep{stepName="조작 잠금",type=WorldMapEventStepType.PlayerInput});
            example.steps.Add(new WorldMapEventStep{stepName="월드맵 UI 숨김",type=WorldMapEventStepType.UIVisibility,showUI=false});
            example.steps.Add(new WorldMapEventStep{stepName="대화",dialoguePages=new List<string>{"월드맵 이벤트 대화입니다."}});
            example.steps.Add(new WorldMapEventStep{stepName="글리치",type=WorldMapEventStepType.ScreenGlitch});
            example.steps.Add(new WorldMapEventStep{stepName="조명과 반짝임",type=WorldMapEventStepType.GlobalLighting,lightIntensity=.4f,screenFlash=true,flashCount=2});
            example.steps.Add(new WorldMapEventStep{stepName="UI 복구",type=WorldMapEventStepType.UIVisibility});
            example.steps.Add(new WorldMapEventStep{stepName="조작 해제",type=WorldMapEventStepType.PlayerInput,lockPlayerInput=false});
            AssetDatabase.CreateAsset(example,folder+"/월드맵_연출 예제.asset");
        }
        Check(light!=null&&Get<EventGuideUI>(controller,"dialogueUI")!=null,"Scene wiring");
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        scene=EditorSceneManager.OpenScene("Assets/Scenes/WorldMapScene.unity"); controller=UnityEngine.Object.FindAnyObjectByType<WorldMapEventController>();
        Check(Get<CanvasGroup[]>(controller,"controlledUI").Length>0 && Get<CanvasGroup[]>(controller,"controlledUI").All(g=>g!=null),"Saved scene UI references survive reload");
        Debug.Log("World map scene event setup complete; controlled UI groups: "+groups.Count);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static void CollectUI(Transform root,Transform guide,EventScreenGlitchController glitch,Image flash,List<CanvasGroup> groups)
    {
        if(root==guide||root==flash.transform||root.GetComponent<RawImage>()!=null)return;
        var overlay=Get<RawImage>(glitch,"screenOverlay"); if(overlay!=null&&(overlay.transform==root||overlay.transform.IsChildOf(root)))return;
        if(guide.IsChildOf(root)||flash.transform.IsChildOf(root)){
            foreach(Transform child in root)CollectUI(child,guide,glitch,flash,groups);
        }else { var group=root.GetComponent<CanvasGroup>(); if(group==null)group=root.gameObject.AddComponent<CanvasGroup>(); groups.Add(group); }
    }
    static IEnumerator Checks()
    {
        var root=new GameObject("World map fixture");root.SetActive(false);
        var controller=root.AddComponent<WorldMapEventController>();
        var ui=new GameObject("HUD",typeof(CanvasGroup)).GetComponent<CanvasGroup>();ui.transform.SetParent(root.transform);ui.alpha=.8f;
        Set(controller,"controlledUI",new[]{ui});
        var light=new GameObject("Global light").AddComponent<Light2D>();light.transform.SetParent(root.transform);light.lightType=Light2D.LightType.Global;light.color=Color.white;light.intensity=.9f;Set(controller,"globalLight",light);
        var flash=new GameObject("Flash",typeof(RectTransform),typeof(Image)).GetComponent<Image>();flash.transform.SetParent(root.transform);Set(controller,"flashImage",flash);
        var guideObject=new GameObject("Dialogue",typeof(RectTransform));guideObject.transform.SetParent(root.transform);
        var guide=guideObject.AddComponent<EventGuideUI>();
        var text=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();text.transform.SetParent(guideObject.transform);
        Set(guide,"guideRoot",guideObject);Set(guide,"guideText",text);Set(guide,"characterInterval",.001f);Set(controller,"dialogueUI",guide);
        var glitch=root.AddComponent<EventScreenGlitchController>();
        var raw=new GameObject("Glitch",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();raw.transform.SetParent(root.transform);
        var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Run/ScreenGlitchController.prefab").GetComponent<EventScreenGlitchController>();
        Set(glitch,"screenOverlay",raw);Set(glitch,"glitchMaterial",Get<Material>(template,"glitchMaterial"));Set(controller,"glitch",glitch);
        root.SetActive(true);yield return null;
        var data=ScriptableObject.CreateInstance<WorldMapEventData>();
        data.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.PlayerInput});
        data.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.UIVisibility,showUI=false});
        data.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.Wait,waitDuration=1});
        controller.Play(data);yield return null;
        Check(WorldMapEventController.InputLocked&&ui.alpha==0&&!ui.interactable,"Lock and hide");
        controller.StopEvent();Check(!WorldMapEventController.InputLocked&&Mathf.Approximately(ui.alpha,.8f)&&ui.interactable,"Cancel restores UI and input");
        data.steps.Clear();data.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.PlayerInput});data.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.PlayerInput,lockPlayerInput=false});data.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.Wait,waitDuration=.1f});
        controller.Play(data);Check(!WorldMapEventController.InputLocked,"Explicit unlock step");yield return new WaitForSecondsRealtime(.2f);
        data.steps.Clear();data.steps.Add(new WorldMapEventStep{dialoguePages=new List<string>{"월드맵 대화 검증"}});
        controller.Play(data);yield return new WaitForSecondsRealtime(.15f);
        Check(guide.IsDialoguePlaying&&WorldMapEventController.InputLocked&&text.text=="월드맵 대화 검증","Dialogue uses existing UI and locks map");
        controller.StopEvent();Check(!guide.IsDialoguePlaying&&!WorldMapEventController.InputLocked,"Dialogue cancellation");
        data.restoreLightingOnComplete=false;data.steps.Clear();data.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.GlobalLighting,lightIntensity=.2f,lightFadeDuration=.12f,screenFlash=true,flashCount=2,flashDuration=.05f,flashInterval=.02f});data.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.ScreenGlitch,glitchDuration=.1f});
        controller.Play(data);yield return null;Check(flash.gameObject.activeSelf,"Flash begins");
        float deadline=Time.realtimeSinceStartup+3;while(controller.IsPlaying&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(!controller.IsPlaying&&Mathf.Approximately(light.intensity,.2f)&&!flash.gameObject.activeSelf&&!glitch.IsPlaying,"Light, repeated flash and glitch complete");
        light.intensity=.9f;data.restoreLightingOnComplete=true;controller.Play(data);
        deadline=Time.realtimeSinceStartup+3;while(controller.IsPlaying&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(!controller.IsPlaying&&Mathf.Approximately(light.intensity,.9f),"Natural completion restores lighting");
        controller.Play(data);yield return null;root.SetActive(false);Check(!controller.IsPlaying&&!WorldMapEventController.InputLocked&&Mathf.Approximately(light.intensity,.9f),"Disable restores state");
        UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(data);
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);bool failed=false;float deadline=Time.realtimeSinceStartup+30;
        while(stack.Count>0){object next;try{if(Time.realtimeSinceStartup>deadline)throw new Exception("Timeout");if(!stack.Peek().MoveNext()){stack.Pop();continue;}next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);failed=true;break;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        Debug.Log((failed?"FAIL":"PASS")+": World map events "+checks+" checks.");EditorApplication.Exit(failed?1:0);
    }
}
