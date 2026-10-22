using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class TutorialChoiceLayoutChecks
{
    public static void Run()
    {
        try{
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            var root=scene.GetRootGameObjects().First(g=>g.name=="TutorialChoiceCanvas");
            foreach(var g in scene.GetRootGameObjects())if(g!=root)g.SetActive(false);
            root.transform.GetChild(0).gameObject.SetActive(true);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;root.GetComponent<CanvasScaler>().enabled=false;
            var rect=(RectTransform)root.transform;rect.position=Vector3.zero;rect.localScale=Vector3.one;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
            var layout=root.GetComponent<ResponsiveScreenLayout>();
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1024,768),new Vector2(2340,1080),new Vector2(1080,2340)}){
                var logical=size/Mathf.Min(size.x/1920,size.y/1080);rect.sizeDelta=logical;Canvas.ForceUpdateCanvases();layout.ApplyLayout(new Rect(-logical/2,logical));
                foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing)throw new Exception("Text overflow: "+text.name);}
                foreach(var entry in layout.elements){var b=ResponsiveScreenLayout.BoundsInCanvas(rect,entry.target,ResponsiveScreenLayout.MeasureContent(entry.target));if(b.min.x<rect.rect.xMin-.5||b.max.x>rect.rect.xMax+.5||b.min.y<rect.rect.yMin-.5||b.max.y>rect.rect.yMax+.5)throw new Exception("Popup outside viewport");}
                if(size.x!=1920)continue;
                var go=new GameObject("TutorialPreviewCamera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=logical.y/2;camera.transform.position=new Vector3(0,0,-100);camera.farClipPlane=500;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.16f,.2f);camera.cullingMask=1<<5;
                var rt=new RenderTexture((int)size.x,(int)size.y,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var png=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);png.Apply();File.WriteAllBytes("Logs/tutorial-choice-preview.png",png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(go);
            }
            Debug.Log("TUTORIAL_CHOICE_LAYOUT_PASS: four aspect ratios, text overflow, popup bounds, 1920x1080 preview. Scene not saved.");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
