using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class PopupScreenGlitchChecks
{
    const string Pending="PopupScreenGlitchChecks";
    static PopupScreenGlitchChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("PopupGlitchTestHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    public static void Run(){
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/PopupLocalGlitch.shader");
        if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Local glitch shader compile error");
        const string materialPath="Assets/Shaders/M_PopupLocalGlitch.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
        foreach(var guid in AssetDatabase.FindAssets("t:PopupOpenAnimationData")){
            var data=AssetDatabase.LoadAssetAtPath<PopupOpenAnimationData>(AssetDatabase.GUIDToAssetPath(guid));
            data.useScreenGlitch=true;data.screenGlitchMaterial=material;EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static PopupOpenAnimator Popup(Transform parent,PopupOpenAnimationData data){var go=new GameObject("TestPopup",typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var p=go.AddComponent<PopupOpenAnimator>();p.SetAnimationData(data);return p;}
    static Canvas[] Overlays()=>UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.name=="PopupScreenGlitchCanvas").ToArray();
    static IEnumerator Guard(){var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Cases());while(stack.Count>0){object current;try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}current=stack.Peek().Current;}catch(Exception e){Time.timeScale=1;Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;}Time.timeScale=1;Debug.Log("POPUP_LOCAL_GLITCH_PASS: target images only, no fullscreen overlay, unrelated UI preserved, paused playback, repeat, disable, completion and material restoration.");EditorApplication.Exit(0);}
    static IEnumerator Cases(){
        var original=AssetDatabase.LoadAssetAtPath<PopupOpenAnimationData>("Assets/Devorya/Data/UI/PopupOpenAnimationData.asset");var data=UnityEngine.Object.Instantiate(original);data.duration=1f;data.useUnscaledTime=true;
        var root=new GameObject("TestCanvas",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();root.renderMode=RenderMode.ScreenSpaceOverlay;
        var a=Popup(root.transform,data);var b=Popup(root.transform,data);
        var imageA=a.GetComponent<Image>();var imageB=b.GetComponent<Image>();var originalA=imageA.material;var originalB=imageB.material;
        yield return new WaitForSecondsRealtime(.5f);Time.timeScale=0;a.PlayOpen();yield return new WaitForSecondsRealtime(.06f);
        Check(a.IsPlaying&&Overlays().Length==0,"No fullscreen overlay");
        Check(imageA.material.shader.name=="Devorya/UI/PopupLocalGlitch"&&imageA.material.GetFloat("_GlitchAmount")>0,"Target image glitched");
        Check(imageB.material==originalB,"Unrelated UI unchanged");Check(imageA.raycastTarget,"Input setting preserved");
        b.PlayOpen();a.CompleteImmediately();Check(imageA.material==originalA&&imageB.material!=originalB,"Independent material restoration");
        b.gameObject.SetActive(false);Check(imageB.material==originalB,"Disable restores material");
        a.PlayOpen();yield return new WaitForSecondsRealtime(.04f);a.PlayOpen();yield return new WaitForSecondsRealtime(1.2f);
        Check(!a.IsPlaying&&imageA.material==originalA&&Overlays().Length==0,"Replay and completion restore local effect");
        Check(data.screenGlitchMaterial.GetFloat("_GlitchAmount")==0,"Shared material unchanged");
        data.useScreenGlitch=false;a.PlayOpen();Check(imageA.material==originalA,"Opt-out");a.CompleteImmediately();
        data.useScreenGlitch=true;data.screenGlitchMaterial=null;a.PlayOpen();yield return new WaitForSecondsRealtime(1.2f);Check(imageA.material==originalA,"Missing material fallback");
        UnityEngine.Object.Destroy(root.gameObject);UnityEngine.Object.Destroy(data);yield return null;
    }
}