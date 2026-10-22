using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class SceneUILayoutPlayChecks
{
    const string Pending="SceneUILayoutPlayChecks";
    static SceneUILayoutPlayChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("LayoutCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Check());}};}
    public static void Run(){
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var go=new GameObject("AuthoredPlayCanvas",typeof(RectTransform),typeof(Canvas));go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;var root=(RectTransform)go.transform;root.sizeDelta=new Vector2(1920,1080);
        var child=new GameObject("AuthoredPlayHUD",typeof(RectTransform),typeof(Image));var target=(RectTransform)child.transform;target.SetParent(root,false);target.sizeDelta=new Vector2(100,50);target.anchoredPosition=new Vector2(-384,162);target.localScale=new Vector3(1.2f,1.2f,1);
        var layout=go.AddComponent<ResponsiveScreenLayout>();layout.followScenePlacement=true;layout.elements=new[]{new ResponsiveScreenLayout.Element{target=target,anchor=Vector2.one,inset=Vector2.zero}};
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static IEnumerator Check(){yield return null;yield return null;try{
        var layout=GameObject.Find("AuthoredPlayCanvas").GetComponent<ResponsiveScreenLayout>();var entry=layout.elements[0];
        if(Vector2.Distance(entry.authoredCenter,new Vector2(.3f,.65f))>.001f)throw new Exception("Play entry did not capture unsaved manual edit");
        if(Vector2.Distance(entry.target.anchoredPosition,new Vector2(-384,162))>.1f)throw new Exception("Runtime replaced manual placement");
        if(Vector3.Distance(entry.target.localScale,new Vector3(1.2f,1.2f,1))>.001f)throw new Exception("Runtime replaced manual scale");
        Debug.Log("SCENE_UI_PLAY_PASS: unsaved edit captured on play entry; runtime position and scale retained.");EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
