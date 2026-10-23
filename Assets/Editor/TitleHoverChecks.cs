using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class TitleHoverChecks
{
    const string Pending="TitleHoverChecks";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static TitleHoverChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("HoverCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static object Field(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static IEnumerator Guard()
    {
        var e=Checks();
        while(true){object next;try{if(!e.MoveNext())break;next=e.Current;}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);yield break;}yield return next;}
        Debug.Log("TITLE_HOVER_PASS");EditorApplication.Exit(0);
    }
    static IEnumerator Checks()
    {
        yield return null;
        var owner=UnityEngine.Object.FindAnyObjectByType<TitleMenuController>();
        Check(owner!=null,"Title controller");
        var nativeMouse=Mouse.current;if(nativeMouse!=null)InputSystem.DisableDevice(nativeMouse);
        var eventSystem=EventSystem.current;eventSystem.enabled=false;
        var mouse=InputSystem.AddDevice<Mouse>();
        foreach(string prefix in new[]{"play","setting"})
        {
            var button=(Button)Field(owner,prefix+"Button");
            var handler=button.GetComponent<TitlePlayHoverHandler>();
            Check(handler!=null,"Hover registered: "+prefix);
            Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)button.transform;
            var canvas=button.GetComponentInParent<Canvas>().rootCanvas;
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var point=RectTransformUtility.WorldToScreenPoint(camera,rect.position);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});InputSystem.Update();
            var pointer=new PointerEventData(EventSystem.current){position=point,pointerEnter=button.gameObject};
            // Match the actual raycast event camera for camera-space title canvases.
            pointer.pointerCurrentRaycast=new RaycastResult{gameObject=button.gameObject,module=canvas.GetComponent<GraphicRaycaster>()};
            handler.OnPointerEnter(pointer);
            var animation=(TitleMenuButtonAnimation)Field(owner,prefix+"ButtonAnimation");
            var initial=Field(animation,"selectCoroutine");
            for(int i=0;i<12;i++){handler.OnPointerExit(pointer);handler.OnPointerEnter(pointer);}
            Check(Equals(initial,Field(animation,"selectCoroutine")),"Spurious exit/enter does not restart selection");
            Check(Field(owner,"selectedMenu").ToString()==(prefix=="play"?"Play":"Setting"),"Correct menu selected");
            // Keep the virtual pointer held in the background batch-mode player.
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime)
            {
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});InputSystem.Update();
                yield return null;
            }
            Check(Field(owner,"selectedMenu").ToString()==(prefix=="play"?"Play":"Setting"),"Held hover remains selected");
            var glitch=(PopupOpenAnimator)Field(owner,prefix+"PopupOpenAnimator");
            Check(!glitch.IsPlaying,"Glitch finishes while hovered");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-500,-500)});InputSystem.Update();
            pointer.position=new Vector2(-500,-500);handler.OnPointerExit(pointer);
            Check(Field(owner,"selectedMenu").ToString()=="None","Real exit clears selection");
        }
        InputSystem.RemoveDevice(mouse);if(nativeMouse!=null)InputSystem.EnableDevice(nativeMouse);eventSystem.enabled=true;
        Debug.Log("Play and Setting: hover registration, repeated raycast exits, held hover, glitch completion, actual exit PASS");
    }
}