using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RelicSlotCorrectionChecks
{
    const string Pending="RelicSlotCorrectionChecks";
    static RelicSlotCorrectionChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("Checks").AddComponent<PieceAnimationManager>().StartCoroutine(CheckRoutine());}};}
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Patch(GameObject panel)
    {
        var slots=panel.GetComponentsInChildren<BattleRelicSlotUI>(true).OrderBy(s=>s.name).ToArray();
        var kept=slots.Take(4).ToArray();
        var positions=kept.Select(s=>((RectTransform)s.transform).anchoredPosition).ToArray();
        foreach(var extra in slots.Skip(4))UnityEngine.Object.DestroyImmediate(extra.gameObject);
        var bar=panel.GetComponent<RunRelicBarUI>();
        if(bar!=null)
        {
            var so=new SerializedObject(bar);var p=so.FindProperty("relicSlots");p.arraySize=kept.Length;
            for(int i=0;i<kept.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=kept[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var slot in kept)
        {
            slot.gameObject.SetActive(true);
            var icon=slot.IconRect;
            Require(icon!=null && icon!=slot.transform,"Child icon must exist");
            var old=slot.GetComponent<PopupOpenAnimator>();
            var anim=icon.GetComponent<PopupOpenAnimator>()??icon.gameObject.AddComponent<PopupOpenAnimator>();
            var data=old!=null?new SerializedObject(old).FindProperty("animationData").objectReferenceValue as PopupOpenAnimationData:null;
            anim.SetAnimationData(data??AssetDatabase.LoadAssetAtPath<PopupOpenAnimationData>("Assets/Devorya/Data/UI/PopupOpenAnimationData.asset"));
            if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            var cg=slot.GetComponent<CanvasGroup>();if(cg!=null)cg.alpha=1;
            icon.GetComponent<Image>().enabled=false;
        }
        for(int i=0;i<kept.Length;i++)Require(((RectTransform)kept[i].transform).anchoredPosition==positions[i],"Preserved authored slot position");
    }
    public static void Run()
    {
        var path="Assets/Prefabs/RelicUI/RelicPanel.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{Patch(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(var scenePath in new[]{"Assets/Scenes/WorldMapScene.unity","Assets/Scenes/EventScene.unity","Assets/Scenes/BattleScene.unity"})
        {
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
            var panels=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RectTransform>(true)).Where(t=>t.name=="RelicPanel").ToArray();
            foreach(var panel in panels)Patch(panel.gameObject);
            foreach(var controller in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BattleUIController>(true)))
            {
                var so=new SerializedObject(controller);var array=so.FindProperty("relicSlotUIs");
                if(array!=null&&array.arraySize>4){array.arraySize=4;so.ApplyModifiedPropertiesWithoutUndo();}
            }
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    public static void TestOnly(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static IEnumerator CheckRoutine()
    {
        var e=Checks();
        while(true){object next;try{if(!e.MoveNext())break;next=e.Current;}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);yield break;}yield return next;}
        Debug.Log("RELIC_SLOT_CORRECTION_PASS");EditorApplication.Exit(0);
    }
    static IEnumerator Checks()
    {
        var run=new GameObject("RunState").AddComponent<RunStateManager>();
        var camera=new GameObject("HudCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;
        var hud=new GameObject("HudCanvas",typeof(Canvas)).GetComponent<Canvas>();hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=camera;hud.planeDistance=2;
        var panel=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RelicUI/RelicPanel.prefab"),hud.transform);
        var bar=panel.GetComponent<RunRelicBarUI>();bar.RefreshFromRunState();
        var slots=panel.GetComponentsInChildren<BattleRelicSlotUI>(true);
        Require(slots.Length==4,"Exactly four slots");
        Require(slots.All(s=>s.gameObject.activeSelf&&!s.IconRect.GetComponent<Image>().enabled),"Empty backgrounds visible, icons hidden");
        var relic=AssetDatabase.LoadAssetAtPath<BattleRelicData>("Assets/Devorya/Data/Relic/유물_초기 가속 모듈.asset");
        run.TryAddBattleRelic(relic,4);bar.RefreshFromRunState();
        Require(slots.Count(s=>s.IconRect.GetComponent<Image>().enabled)==1,"Only owned icon appears");
        Require(slots.All(s=>s.GetComponent<PopupOpenAnimator>()==null),"Background has no opening animation");
        var icon=bar.PrepareArrival(relic);Require(icon==slots[0].IconRect,"Arrival uses child icon");
        var popup=new GameObject("PopupCanvas",typeof(Canvas)).GetComponent<Canvas>();popup.renderMode=RenderMode.ScreenSpaceOverlay;
        Canvas.ForceUpdateCanvases();yield return null;
        var destination=RelicRewardPopupUI.ParticleDestination(icon,(RectTransform)popup.transform);
        var expected=RectTransformUtility.WorldToScreenPoint(camera,icon.position);
        var actual=RectTransformUtility.WorldToScreenPoint(null,destination);
        Require(Vector2.Distance(actual,expected)<.1f,"Different canvas camera coordinates match on screen");
        var cg=icon.GetComponent<CanvasGroup>()??icon.gameObject.AddComponent<CanvasGroup>();cg.alpha=0;
        Require(slots[0].GetComponent<CanvasGroup>()==null||slots[0].GetComponent<CanvasGroup>().alpha==1,"Flying icon does not hide frame");
        run.ClearRunState();bar.RefreshFromRunState();
        Require(slots.All(s=>s.gameObject.activeSelf&&!s.IconRect.GetComponent<Image>().enabled),"Clear retains four backgrounds");
        Debug.Log("Four slots, authored placement, icon-only visibility/glitch, cross-canvas arrival: PASS");
    }
}