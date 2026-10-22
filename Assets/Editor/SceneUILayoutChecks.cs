using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class SceneUILayoutChecks
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            foreach(var name in new[]{"BattleScene","WorldMapScene","EventScene","Title","TextCutsceneScene","BattleScenePVP"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                Canvas.ForceUpdateCanvases();
                foreach(var layout in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResponsiveScreenLayout>(true)))
                {
                    var entries=layout.elements.Where(e=>e!=null&&e.target!=null).ToArray();
                    var positions=entries.Select(e=>e.target.position).ToArray();
                    var scales=entries.Select(e=>e.target.localScale).ToArray();
                    layout.followScenePlacement=true;
                    layout.CaptureScenePlacement();
                    for(int i=0;i<entries.Length;i++)Check(entries[i].target.position==positions[i]&&entries[i].target.localScale==scales[i],"Migration moved authored UI");
                    EditorUtility.SetDirty(layout);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                scene=EditorSceneManager.OpenScene(scene.path);
                foreach(var layout in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ResponsiveScreenLayout>(true)))
                {
                    Check(layout.followScenePlacement,"Saved authored mode");
                    var canvas=layout.GetComponent<Canvas>();var root=(RectTransform)canvas.transform;
                    canvas.renderMode=RenderMode.WorldSpace;
                    var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null)scaler.enabled=false;
                    root.localScale=Vector3.one;root.position=Vector3.zero;root.rotation=Quaternion.identity;
                    foreach(var resolution in new[]{new Vector2(1920,1080),new Vector2(1280,720),new Vector2(1024,768),new Vector2(2560,1080),new Vector2(2340,1080),new Vector2(1080,2340)})
                    {
                        var logical=resolution/Mathf.Min(resolution.x/1920,resolution.y/1080);root.sizeDelta=logical;Canvas.ForceUpdateCanvases();
                        var safe=new Rect(-logical/2,logical);if(resolution.x==2340){safe.xMin+=96;safe.xMax-=96;safe.yMin+=32;}
                        layout.ApplyLayout(safe);
                        foreach(var entry in layout.elements)
                        {
                            if(entry==null||entry.target==null)continue;
                            var bounds=ResponsiveScreenLayout.BoundsInCanvas(root,entry.target,ResponsiveScreenLayout.MeasureContent(entry.target));
                            Check(bounds.min.x>=safe.xMin-.5f&&bounds.max.x<=safe.xMax+.5f&&bounds.min.y>=safe.yMin-.5f&&bounds.max.y<=safe.yMax+.5f,"Clipped "+name+"/"+entry.target.name+" "+resolution);
                        }
                    }
                }
            }
            // A new manual position and size must replace old dock and relative settings.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var go=new GameObject("AuthoredCanvas",typeof(RectTransform),typeof(Canvas));var canvasTest=go.GetComponent<Canvas>();canvasTest.renderMode=RenderMode.WorldSpace;
            var rootTest=(RectTransform)go.transform;rootTest.sizeDelta=new Vector2(1920,1080);
            var targetGo=new GameObject("ManualHUD",typeof(RectTransform),typeof(Image));var target=(RectTransform)targetGo.transform;target.SetParent(rootTest,false);target.sizeDelta=new Vector2(100,60);target.anchoredPosition=new Vector2(-480,270);target.localScale=new Vector3(1.5f,1.5f,1);
            var test=go.AddComponent<ResponsiveScreenLayout>();test.followScenePlacement=true;test.elements=new[]{new ResponsiveScreenLayout.Element{target=target,anchor=Vector2.one,inset=new Vector2(999,999),relativeTo=rootTest,relativeOffset=new Vector2(999,999)}};
            Canvas.ForceUpdateCanvases();SceneUILayoutAuthoring.Capture(go.scene);
            Check(Vector2.Distance(test.elements[0].authoredCenter,new Vector2(.25f,.75f))<.001f,"Capture manual normalized position");
            target.anchoredPosition=Vector2.zero;test.ApplyLayout(new Rect(-960,-540,1920,1080));Check(Vector2.Distance(target.anchoredPosition,new Vector2(-480,270))<.01f,"Manual placement restored, legacy dock ignored");Check(target.localScale==new Vector3(1.5f,1.5f,1),"Manual scale retained");
            rootTest.sizeDelta=new Vector2(2560,1440);Canvas.ForceUpdateCanvases();test.ApplyLayout(new Rect(-1280,-720,2560,1440));Check(Vector2.Distance(target.anchoredPosition,new Vector2(-640,360))<.01f,"Proportional placement after resize");
            rootTest.sizeDelta=new Vector2(1920,1080);target.anchoredPosition=new Vector2(300,-200);target.localScale=Vector3.one;Canvas.ForceUpdateCanvases();SceneUILayoutAuthoring.Capture(go.scene);test.ApplyLayout(rootTest.rect);Check(Vector2.Distance(target.anchoredPosition,new Vector2(300,-200))<.01f,"Subsequent manual edit replaces prior position");
            Debug.Log("SCENE_UI_LAYOUT_PASS: "+checks+" checks; six saved scenes, manual placement and scale, repeat edits, aspect ratios and safe-area containment.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
