using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

[InitializeOnLoad]
public static class RelicUIRevisionChecks
{
    const string Pending="RelicUIRevisionChecks";
    static PopupOpenAnimationData animation;
    static readonly System.Reflection.BindingFlags Flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    static RelicUIRevisionChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("RevisionCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    static void Set(UnityEngine.Object target,string name,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static T Ensure<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    static void WireSlot(BattleRelicSlotUI slot)
    {
        var icon=slot.GetComponentsInChildren<Image>(true).FirstOrDefault(i=>i.gameObject!=slot.gameObject);
        if(icon==null){var go=new GameObject("RelicIcon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(slot.transform,false);icon=go.GetComponent<Image>();}
        var r=icon.rectTransform;r.localScale=Vector3.one;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(32,32);
        icon.preserveAspect=true;icon.raycastTarget=false;
        Set(slot,"relicIconImage",icon);
        Ensure<SlotHoverFeedback>(slot.gameObject);
        Ensure<PopupOpenAnimator>(slot.gameObject).SetAnimationData(animation);
    }
    static void WireDecoration(GameObject go)
    {
        var decoration=go.transform.Find("RelicBar");
        if(decoration!=null)Set(Ensure<RelicPanelVisibility>(go),"decoration",decoration.gameObject);
    }
    static void WirePanel(GameObject go)
    {
        WireDecoration(go);
        var slots=go.GetComponentsInChildren<BattleRelicSlotUI>(true).OrderBy(s=>s.name).ToList();
        // Keep the four authored slots and add overflow slots below them, without moving them.
        for(int i=slots.Count;i<10&&slots.Count>0;i++)
        {
            var clone=UnityEngine.Object.Instantiate(slots[0],slots[0].transform.parent);clone.name="RelicSlot_"+i;
            var r=(RectTransform)clone.transform;var first=(RectTransform)slots[0].transform;
            r.anchoredPosition=new Vector2(first.anchoredPosition.x-((i-4)%2)*37,first.anchoredPosition.y-(i-3)*38);
            slots.Add(clone);
        }
        foreach(var slot in slots)WireSlot(slot);
        var bar=Ensure<RunRelicBarUI>(go);var so=new SerializedObject(bar);var p=so.FindProperty("relicSlots");p.arraySize=slots.Count;
        for(int i=0;i<slots.Count;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=slots[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void PatchPrefab(string path,Action<GameObject> patch)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try{patch(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Run() { RelicSlotCorrectionChecks.Run(); }
    static IEnumerator Guard()
    {
        bool error=false;Application.LogCallback onLog=(m,t,k)=>{if(k==LogType.Error||k==LogType.Exception)error=true;};Application.logMessageReceived+=onLog;
        var stack=new Stack<IEnumerator>();stack.Push(Checks());
        while(stack.Count>0){object next;try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        Application.logMessageReceived-=onLog;Debug.Log(error?"RELIC_UI_REVISION_FAILED":"RELIC_UI_REVISION_PASS");EditorApplication.Exit(error?1:0);
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static IEnumerator Capture(RelicRewardPopupUI popup)
    {
        var cameraObject = new GameObject("PreviewCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.17f,.18f,.2f);
        camera.transform.position = new Vector3(0,0,-10); camera.orthographic = true;
        var canvas = popup.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var rt = new RenderTexture(1920,1080,24);
        camera.targetTexture = rt;
        yield return null;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var old = RenderTexture.active; RenderTexture.active = rt;
        var texture = new Texture2D(1920,1080,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
        System.IO.File.WriteAllBytes("Logs/relic-ui-revision-preview.png",texture.EncodeToPNG());
        RenderTexture.active = old; camera.targetTexture = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
        UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(cameraObject);
    }
    static IEnumerator Checks()
    {
        var run=new GameObject("RunState").AddComponent<RunStateManager>();
        var panel=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RelicUI/RelicPanel.prefab"));
        var bar=panel.GetComponent<RunRelicBarUI>();bar.RefreshFromRunState();
        var slots=panel.GetComponentsInChildren<BattleRelicSlotUI>(true);
        Check(slots.Length==10&&slots.All(s=>!s.gameObject.activeSelf),"Empty relic slots hidden");
        var relic=AssetDatabase.LoadAssetAtPath<BattleRelicData>("Assets/Devorya/Data/Relic/유물_초기 가속 모듈.asset");
        run.TryAddBattleRelic(relic,10);bar.RefreshFromRunState();
        Check(slots.Count(s=>s.gameObject.activeSelf)==1,"Only owned slot shown");
        var slot=slots.First(s=>s.CurrentRelicData==relic);
        var icon=(Image)new SerializedObject(slot).FindProperty("relicIconImage").objectReferenceValue;
        Check(icon.transform!=slot.transform&&icon.sprite==relic.iconSprite,"Icon uses authored child, not slot frame");
        Check(slot.GetComponent<PopupOpenAnimator>().IsPlaying,"Slot glitch open");
        yield return new WaitForSecondsRealtime(.3f);
        var hover=slot.GetComponent<SlotHoverFeedback>();var scale=slot.transform.localScale;
        hover.OnPointerEnter(null);yield return new WaitForSecondsRealtime(.2f);
        Check(slot.transform.localScale.x>scale.x,"Slot hover enlarges");
        hover.OnPointerExit(null);yield return new WaitForSecondsRealtime(.4f);
        Check(Mathf.Abs(slot.transform.localScale.x-scale.x)<.01f,"Hover returns to authored scale");
        run.ClearRunState();bar.RefreshFromRunState();Check(slots.All(s=>!s.gameObject.activeSelf),"Clear hides slots");
        yield return null;
        Check(!panel.transform.Find("RelicBar").gameObject.activeSelf,"Empty decoration hidden");
        // Exercise actual popup and custom color configuration without regenerating user prefabs.
        yield return UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation;
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale=UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale("en");
        var owner=new GameObject("Owner").AddComponent<EventSceneSequenceController>();
        var hudGroup=panel.AddComponent<CanvasGroup>();hudGroup.alpha=0;
        Set(owner,"runHUDCanvasGroup",hudGroup);
        var list=AssetDatabase.LoadAssetAtPath<RelicRewardListData>("Assets/Devorya/Data/Relic/RelicReward/유물보상_기본 목록.asset");
        var routine=owner.StartCoroutine(RelicRewardPopupUI.ShowRoutine(owner,list));
        while(RelicRewardPopupUI.Active==null)yield return null;
        var popup=RelicRewardPopupUI.Active;
        float deadline=Time.realtimeSinceStartup+15;
        while(!(bool)typeof(RelicRewardPopupUI).GetField("accepting",Flags).GetValue(popup)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(Time.realtimeSinceStartup<deadline,"Cards ready");
        var cards=popup.GetComponentsInChildren<RelicRewardCardUI>();
        foreach(var card in cards)
        {
            foreach(var text in card.GetComponentsInChildren<TMP_Text>(true)){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Text fits: "+text.name);}
            var so=new SerializedObject(card);var colors=so.FindProperty("rarityColors");Check(colors.arraySize==3,"Three editable rarity palettes");
        }
        yield return Capture(popup);
        var profile=new SerializedObject(cards[0]);profile.FindProperty("rarityColors").GetArrayElementAtIndex(cards[0].Data.rarity-1).FindPropertyRelative("particles").colorValue=Color.cyan;profile.ApplyModifiedPropertiesWithoutUndo();
        Check(cards[0].ParticleColor==Color.cyan,"Editable particle color");
        var selectedData=cards[0].Data;
        cards[0].GetComponent<Button>().onClick.Invoke();yield return routine;
        Check(hudGroup.alpha==1,"Grouped HUD revealed after selection");
        var arrival=RelicRewardArrivalHUD.ResolveTarget(selectedData);
        Check(arrival!=null && arrival.IsChildOf(panel.transform),"Authored panel receives reward");
        Check(run.GetBattleRelicsCopy().Count==1,"Reward still acquired once");
        Debug.Log("Revision checks: empty/owned slots, child icon, glitch, hover, text bounds, rarity palettes and acquisition");
    }
}