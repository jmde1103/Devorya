using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class Merge1024SecondChecks
{
    const string Pending="Merge1024SecondChecks";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static Merge1024SecondChecks()
    {
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))
            {
                SessionState.SetBool(Pending,false);
                new GameObject("MergeSecondCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static object Field(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("MERGE_SECOND_CHECK: "+message);}
    static IEnumerator Guard()
    {
        var routine=Checks();
        while(true)
        {
            object next;
            try{if(!routine.MoveNext())break;next=routine.Current;}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}
            yield return next;
        }
        Debug.Log("MERGE1024_SECOND_PLAYMODE_PASS");EditorApplication.Exit(0);
    }
    static IEnumerator Checks()
    {
        yield return new WaitForSecondsRealtime(3);
        var bm=BattleManager.Instance;bm.StopAllCoroutines();
        var sequence=UnityEngine.Object.FindAnyObjectByType<EventSequenceController>();
        sequence.StopAllCoroutines();Set(sequence,"isSequenceActive",true);
        Check((int)EventSequenceStepType.SetBattleUIVisibility==17&&(int)EventSequenceStepType.ScreenShake==18&&(int)EventSequenceStepType.StopScreenShake==19,"Battle enum values retained");
        Check((int)EventSceneStepType.RelicReward==19&&(int)EventSceneStepType.StopScreenShake==20,"EventScene enum values retained");
        var parent=new GameObject("HiddenParent",typeof(CanvasGroup)).GetComponent<CanvasGroup>();parent.alpha=0;
        var one=new GameObject("RegisteredOne",typeof(CanvasGroup)).GetComponent<CanvasGroup>();one.transform.SetParent(parent.transform);one.alpha=.4f;
        var two=new GameObject("RegisteredTwo",typeof(CanvasGroup)).GetComponent<CanvasGroup>();two.alpha=.6f;
        var list=(IList)Field(sequence,"battleUIBindings");
        var bindingType=list.GetType().GetGenericArguments()[0];
        foreach(var pair in new[]{(id:"One",group:one),(id:"Two",group:two)})
        {
            var binding=Activator.CreateInstance(bindingType,true);
            bindingType.GetField("uiId").SetValue(binding,pair.id);bindingType.GetField("canvasGroup").SetValue(binding,pair.group);
            list.Add(binding);
        }
        var step=new EventSequenceStepData{battleUIUseRegisteredTargets=true,battleUIFadeDuration=.05f,battleUIWaitForComplete=true};
        step.battleUIVisibilityTargets.Add(new EventBattleUIVisibilityTarget{uiId="One",show=true});
        step.battleUIVisibilityTargets.Add(new EventBattleUIVisibilityTarget{uiId="Two",show=false});
        yield return sequence.StartCoroutine((IEnumerator)Call(sequence,"ExecuteSetBattleUIVisibilityStepRoutine",step));
        Check(Mathf.Approximately(one.alpha,1)&&one.ignoreParentGroups&&Mathf.Approximately(two.alpha,0),"Registered multiple UI targets, fade, parent override");
        Call(sequence,"RestoreBattleUIVisibilityImmediately");
        Check(Mathf.Approximately(one.alpha,.4f)&&!one.ignoreParentGroups&&Mathf.Approximately(two.alpha,.6f),"Registered UI original state restored");
        Set(sequence,"endTimerRootBattleCanvasGroup",one);Set(sequence,"goldRootCanvasGroup",two);
        var legacy=new EventSequenceStepData{battleUIUseRegisteredTargets=false,battleUIChangeEndTimerRoot=true,battleUIShowEndTimerRoot=false,battleUIChangeGoldRoot=true,battleUIShowGoldRoot=false,battleUIFadeDuration=0};
        yield return sequence.StartCoroutine((IEnumerator)Call(sequence,"ExecuteSetBattleUIVisibilityStepRoutine",legacy));
        Check(one.alpha==0&&two.alpha==0,"Legacy EndTimer and Gold compatibility");
        Call(sequence,"RestoreBattleUIVisibilityImmediately");
        var shake=new GameObject("ShakeCheck").transform;var initial=new Vector3(2,3,4);shake.localPosition=initial;
        Set(sequence,"cameraShakeTarget",shake);
        Call(sequence,"StartCameraShake",0f,.2f,true);
        yield return new WaitForSecondsRealtime(.1f);
        Check((bool)Field(sequence,"isContinuousCameraShakeActive")&&Field(sequence,"cameraShakeCoroutine")!=null,"Battle continuous shake remains active");
        Call(sequence,"StopCameraShakeImmediately");
        Check(shake.localPosition==initial&&Field(sequence,"cameraShakeCoroutine")==null,"Battle StopScreenShake restores position");
        Call(sequence,"StartCameraShake",.05f,.2f,false);
        yield return new WaitForSecondsRealtime(.2f);
        Check(shake.localPosition==initial&&Field(sequence,"cameraShakeCoroutine")==null,"Battle timed shake finishes and restores");
        var eventScene=new GameObject("EventSceneCheck").AddComponent<EventSceneSequenceController>();
        Set(eventScene,"isSequenceActive",true);Set(eventScene,"cameraShakeTarget",shake);
        var pixel=UnityEngine.Object.FindAnyObjectByType<PixelCameraController>();
        Set(eventScene,"pixelCameraController",pixel);
        var target=new GameObject("CameraShotCheck").transform;
        pixel.StartEventCameraShot(target,Vector2.zero,1f,.3f);
        Call(eventScene,"StartCameraShake",0f,.2f,true);
        Check(pixel.IsEventCameraShotPlaying,"Continuous shake does not stop CameraShot");
        yield return new WaitForSecondsRealtime(.5f);
        Check(!pixel.IsEventCameraShotPlaying&&(bool)Field(eventScene,"isContinuousCameraShakeActive"),"CameraShot completes while continuous shake persists");
        Call(eventScene,"StopCameraShakeImmediately");
        Check(shake.localPosition==initial,"EventScene StopScreenShake restores position");
        Call(eventScene,"StartCameraShake",0f,.2f,true);
        eventScene.enabled=false;
        Check(shake.localPosition==initial&&Field(eventScene,"cameraShakeCoroutine")==null,"EventScene disable safely clears shake");
        var gameOver=UnityEngine.Object.FindAnyObjectByType<BattleGameOverSequence>(FindObjectsInactive.Include);
        Check(gameOver!=null&&gameOver.transform.parent==null,"GameOver root retained");
        foreach(var name in new[]{"screenGlitchController","signalNoiseUI","corruptionFX","inputBlockerGroup","blackoutGroup","messageGroup","mainMessageText","redGhostText","cyanGhostText"})
            Check(Field(gameOver,name)!=null,"GameOver reference "+name);
    }
}
