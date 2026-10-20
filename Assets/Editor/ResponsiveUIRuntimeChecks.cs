using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ResponsiveUIRuntimeChecks
{
    const string Pending="Devorya.ResponsiveUIRuntimeChecks";
    static ResponsiveUIRuntimeChecks()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){
                SessionState.SetBool(Pending,false);
                new GameObject("UIValidationRunner").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static IEnumerator Guard()
    {
        var routine=Checks();
        while(true){object next;try{if(!routine.MoveNext())break;next=routine.Current;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
        Debug.Log("RESPONSIVE RUNTIME PASS: HUD edge placement, resize correction, tooltip containment and oversized tooltip fit.");EditorApplication.Exit(0);
    }
    static IEnumerator Checks()
    {
        var canvas=new GameObject("ValidationCanvas",typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var parent=new GameObject("LegacyParent",typeof(RectTransform)).GetComponent<RectTransform>();parent.SetParent(canvas.transform,false);parent.sizeDelta=new Vector2(100,100);
        var hud=new GameObject("CornerHUD",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();hud.SetParent(parent,false);hud.sizeDelta=new Vector2(300,120);
        var layout=canvas.gameObject.AddComponent<ResponsiveScreenLayout>();layout.elements=new[]{new ResponsiveScreenLayout.Element{target=hud,anchor=Vector2.zero,inset=new Vector2(24,24),originalScale=Vector3.one,contentBounds=ResponsiveScreenLayout.MeasureContent(hud)}};
        for(int frame=0;frame<10;frame++)yield return null;
        var root=(RectTransform)canvas.transform;var safe=ResponsiveScreenLayout.GetSafeCanvasRect(canvas,Screen.safeArea);
        var b=ResponsiveScreenLayout.BoundsInCanvas(root,hud,ResponsiveScreenLayout.MeasureContent(hud));
        Require(Mathf.Abs(b.min.x-safe.xMin-24)<1&&Mathf.Abs(b.min.y-safe.yMin-24)<1,"HUD edge placement failed: bounds="+b+" safe="+safe+" scale="+canvas.scaleFactor);
        layout.ApplyLayout(new Rect(-400,-300,800,600));b=ResponsiveScreenLayout.BoundsInCanvas(root,hud,ResponsiveScreenLayout.MeasureContent(hud));Require(Mathf.Abs(b.min.x+376)<1,"Resize placement failed");
        var popup=new GameObject("Tooltip",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();popup.SetParent(canvas.transform,false);popup.sizeDelta=new Vector2(400,200);popup.anchoredPosition=new Vector2(10000,10000);popup.gameObject.AddComponent<ScreenBoundedPopup>();
        for(int frame=0;frame<10;frame++)yield return null;Contain(root,popup,safe);
        popup.sizeDelta=new Vector2(10000,10000);popup.anchoredPosition=new Vector2(-10000,-10000);
        for(int frame=0;frame<10;frame++)yield return null;Contain(root,popup,safe);
        UnityEngine.Object.Destroy(canvas.gameObject);
    }
    static void Contain(RectTransform root,RectTransform target,Rect safe){var b=ResponsiveScreenLayout.BoundsInCanvas(root,target,ResponsiveScreenLayout.MeasureContent(target));Require(b.min.x>=safe.xMin-.5f&&b.min.y>=safe.yMin-.5f&&b.max.x<=safe.xMax+.5f&&b.max.y<=safe.yMax+.5f,"Tooltip outside safe area");}
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
