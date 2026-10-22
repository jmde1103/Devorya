using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ResponsiveUISetup
{
    static readonly Dictionary<string,string> EnglishNames=new Dictionary<string,string>{
        {"월드맵 이벤트 관리자","WorldMapEventController"},{"월드맵 글리치 화면","WorldMapGlitchCanvas"},
        {"월드맵 반짝임 화면","WorldMapFlashCanvas"},{"반짝임","FlashOverlay"}};
    static readonly Dictionary<string,Vector4> Dock=new Dictionary<string,Vector4>{
        {"PlayerStatusPanel",new Vector4(0,0,24,24)},{"EnemyStatusPanel",new Vector4(1,1,24,24)},
        {"Absorb Button",new Vector4(1,0,24,24)},{"Skill Button",new Vector4(1,0,24,260)},
        {"Type Icon Button",new Vector4(1,.5f,24,0)},{"Absorb Count",new Vector4(.5f,1,0,24)},
        {"TurnInfoPanel",new Vector4(.5f,.5f,0,160)},{"PieceRepairSlot",new Vector4(.5f,0,0,100)},
        {"ItemSlotPanel",new Vector4(.5f,0,0,24)},{"RelicPanel",new Vector4(.5f,1,0,120)},
        {"EndTimerRoot_Battle",new Vector4(0,1,24,100)},{"EndTimerRoot_Run",new Vector4(0,1,24,100)},
        {"GoldRoot",new Vector4(0,1,520,24)},{"GoldRoot_Run",new Vector4(0,1,520,24)},
        {"RunItemBarRoot",new Vector4(.5f,1,0,24)},{"RunItemBarVeiwer",new Vector4(.5f,1,0,24)},
        {"DialoguePanel",new Vector4(.5f,0,0,180)},{"SelectPanelRoot",new Vector4(.5f,.5f,0,80)},
        {"BattleRewardPopup",new Vector4(.5f,.5f,0,0)},{"RewardPopup",new Vector4(.5f,.5f,0,0)},
        {"SkillFailurePopup",new Vector4(.5f,0,0,180)},{"ShopFailurePopup",new Vector4(.5f,0,0,160)},
        {"ShopPurchaseButton",new Vector4(1,0,244,24)},{"ShopExitButton",new Vector4(1,0,24,24)},
        {"CutsceneTextRoot",new Vector4(.5f,.5f,0,0)}};
    static int checks, groups;
    public static void Run()
    {
        try{
            foreach(var name in new[]{"BattleScene","WorldMapScene","EventScene","Title","TextCutsceneScene","BattleScenePVP"}){
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(EnglishNames.TryGetValue(t.name,out var english))t.name=english;
                foreach(var root in scene.GetRootGameObjects())foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)){
                    if(!canvas.isRootCanvas||canvas.renderMode==RenderMode.WorldSpace)continue;
                    if(canvas.name.Contains("Glitch")||canvas.name.Contains("Flash"))continue;
                    var scaler=canvas.GetComponent<CanvasScaler>();if(scaler==null)scaler=canvas.gameObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                    var layout=canvas.GetComponent<ResponsiveScreenLayout>();if(layout==null)layout=canvas.gameObject.AddComponent<ResponsiveScreenLayout>();
                    if(layout.followScenePlacement)continue; // Preserve manually authored layouts.
                    var targets=new List<RectTransform>();Collect((RectTransform)canvas.transform,targets);
                    var rect=(RectTransform)canvas.transform;var mode=canvas.renderMode;var size=rect.sizeDelta;
                    canvas.renderMode=RenderMode.WorldSpace;rect.sizeDelta=new Vector2(1920,1080);Canvas.ForceUpdateCanvases();
                    var entries=new List<ResponsiveScreenLayout.Element>();
                    foreach(var target in targets){
                        if(target.name.Contains("Tooltip")){if(target.GetComponent<ScreenBoundedPopup>()==null)target.gameObject.AddComponent<ScreenBoundedPopup>();continue;}
                        var local=ResponsiveScreenLayout.MeasureContent(target);var bounds=ResponsiveScreenLayout.BoundsInCanvas(rect,target,local);
                        Vector4 dock;
                        if(!Dock.TryGetValue(target.name,out dock)){
                            float ax=target.name=="RunStatusHUD"?0:bounds.center.x < -480?0:bounds.center.x>480?1:.5f;
                            float ay=target.name=="RunStatusHUD"?1:bounds.center.y < -270?0:bounds.center.y>270?1:.5f;
                            dock=new Vector4(ax,ay,ax==0?Mathf.Max(24,bounds.min.x+960):ax==1?Mathf.Max(24,960-bounds.max.x):bounds.center.x,
                                ay==0?Mathf.Max(24,bounds.min.y+540):ay==1?Mathf.Max(24,540-bounds.max.y):bounds.center.y);
                        }
                        entries.Add(new ResponsiveScreenLayout.Element{target=target,anchor=new Vector2(dock.x,dock.y),inset=new Vector2(dock.z,dock.w),contentBounds=local,originalScale=target.localScale});groups++;
                    }
                    layout.elements=entries.ToArray();rect.sizeDelta=size;canvas.renderMode=mode;EditorUtility.SetDirty(layout);EditorUtility.SetDirty(scaler);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                // Validate the saved scene, not transient setup objects.
                scene=EditorSceneManager.OpenScene(scene.path);
                foreach(var root in scene.GetRootGameObjects())foreach(var layout in root.GetComponentsInChildren<ResponsiveScreenLayout>(true))Verify(layout,name);
            }
            Debug.Log("RESPONSIVE UI PASS: "+groups+" groups; "+checks+" viewport containment checks. Six scenes saved.");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void VerifyOnly()
    {
        try {
            foreach(var name in new[]{"BattleScene","WorldMapScene","EventScene","Title","TextCutsceneScene","BattleScenePVP"}) {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                foreach(var root in scene.GetRootGameObjects())foreach(var layout in root.GetComponentsInChildren<ResponsiveScreenLayout>(true))Verify(layout,name);
            }
            Debug.Log("RESPONSIVE SAVED SCENES PASS: "+checks+" containment checks.");EditorApplication.Exit(0);
        } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Collect(RectTransform parent,List<RectTransform> result)
    {
        foreach(Transform child in parent){if(!(child is RectTransform rect))continue;
            string name=child.name.Trim();
            if(name=="SafeAreaRoot"){rect.offsetMin=rect.offsetMax=Vector2.zero;Collect(rect,result);continue;}
            if(name=="BattleUIRoot"||name=="RunHUDRoot"||name=="EventUI"||name=="GuideRoot"){
                if(name=="EventUI"||name=="GuideRoot"){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
                Collect(rect,result);continue;
            }
            if(name=="MarkerLayer"||name=="EventUIMarker")continue;
            if(name=="BlackFade"||name=="BlackBackground"||name=="BackgroundClickArea"||name=="InputBlocker"){
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;continue;
            }
            result.Add(rect);
        }
    }
    static void Verify(ResponsiveScreenLayout layout,string sceneName)
    {
        var canvas=layout.GetComponent<Canvas>();var rect=(RectTransform)canvas.transform;
        canvas.renderMode=RenderMode.WorldSpace;layout.GetComponent<CanvasScaler>().enabled=false;
        rect.localScale=Vector3.one;rect.position=Vector3.zero;rect.rotation=Quaternion.identity;
        foreach(var resolution in new[]{new Vector2(1920,1080),new Vector2(1280,720),new Vector2(1024,768),new Vector2(2557,1599),new Vector2(2560,1080),new Vector2(2340,1080),new Vector2(1080,2340)}){
            float scale=Mathf.Min(resolution.x/1920,resolution.y/1080);Vector2 logical=resolution/scale;rect.sizeDelta=logical;
            Canvas.ForceUpdateCanvases();Rect safe=new Rect(-logical*.5f,logical);
            if(resolution.x==2340){safe.xMin+=96/scale;safe.xMax-=96/scale;safe.yMin+=32/scale;}
            layout.ApplyLayout(safe);
            foreach(var entry in layout.elements){
                if(entry.target==null)throw new Exception("Missing UI target "+sceneName);
                var b=ResponsiveScreenLayout.BoundsInCanvas(rect,entry.target,entry.contentBounds);
                if(b.min.x<safe.xMin-.5f||b.min.y<safe.yMin-.5f||b.max.x>safe.xMax+.5f||b.max.y>safe.yMax+.5f)throw new Exception("Clipped "+sceneName+"/"+entry.target.name+" at "+resolution);
                checks++;
            }
            if(sceneName=="BattleScene"&&(resolution.x==1920||resolution.x==1024||resolution.x==2340))Capture(canvas,resolution,logical);
        }
    }
    static void Capture(Canvas canvas,Vector2 resolution,Vector2 logical)
    {
        foreach(var entry in canvas.GetComponent<ResponsiveScreenLayout>().elements){
            if(new[]{"PlayerStatusPanel","EnemyStatusPanel","Absorb Button","Skill Button","Type Icon Button","Absorb Count","ItemSlotPanel"}.Contains(entry.target.name)){
                for(Transform t=entry.target;t!=null&&t!=canvas.transform;t=t.parent)t.gameObject.SetActive(true);
            }
        }
        foreach(var t in canvas.GetComponentsInChildren<Transform>(true))
            if(t.name.Contains("Popup")||t.name.Contains("Tooltip"))t.gameObject.SetActive(false);
        var go=new GameObject("UILayoutPreviewCamera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=logical.y*.5f;camera.transform.position=new Vector3(0,0,-100);camera.farClipPlane=500;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.2f);camera.cullingMask=1<<5;
        foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
        var texture=new RenderTexture((int)resolution.x,(int)resolution.y,24);camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
        var png=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);png.Apply();
        File.WriteAllBytes("Logs/ui-layout-"+(int)resolution.x+"x"+(int)resolution.y+".png",png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(go);
    }
}
