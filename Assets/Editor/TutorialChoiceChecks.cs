using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using TMPro;
[InitializeOnLoad]
public static class TutorialChoiceChecks
{
    const string Pending="TutorialChoiceChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static TutorialChoiceChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);var host=new GameObject("TutorialChoiceCheckHost");UnityEngine.Object.DontDestroyOnLoad(host);host.AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
    static void Call(object o,string method)=>o.GetType().GetMethod(method,Flags).Invoke(o,null);
    static object Get(object o,string field)=>o.GetType().GetField(field,Flags).GetValue(o);
    static IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Cases());while(stack.Count>0){object next;try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(next is IEnumerator routine)stack.Push(routine);else yield return next;}Debug.Log("TUTORIAL_CHOICE_PLAY_PASS: modal input lock, localized labels, single callback, boot before both routes, No -> WorldMap, Yes -> original BattleScene, source data unchanged, one-shot override consumed.");EditorApplication.Exit(0);}
    static IEnumerator WaitScene(string name){float until=Time.realtimeSinceStartup+20;while(SceneManager.GetActiveScene().name!=name){Check(Time.realtimeSinceStartup<until,"Scene timeout: "+name);yield return null;}yield return null;yield return null;}
    static IEnumerator Cases()
    {
        var sampleEvent=ScriptableObject.CreateInstance<EventSceneData>();
        var sampleWorld=ScriptableObject.CreateInstance<WorldMapEventData>();
        TextCutsceneRuntimeState.SetPendingCutsceneData(null,"EventScene",sampleEvent,sampleWorld);
        TextCutsceneRuntimeState.ConsumeSkipEvents(out var eventResult,out var worldResult);
        Check(eventResult==sampleEvent && worldResult==sampleWorld,"Both optional events forwarded");
        TextCutsceneRuntimeState.ConsumeSkipEvents(out eventResult,out worldResult);
        Check(eventResult==null && worldResult==null,"Events consumed once");
        TextCutsceneRuntimeState.SetPendingCutsceneData(null,"WorldMapScene",sampleEvent,sampleWorld);
        TextCutsceneRuntimeState.SetPendingCutsceneData(null);
        TextCutsceneRuntimeState.ConsumeSkipEvents(out eventResult,out worldResult);
        Check(eventResult==null && worldResult==null,"Normal tutorial clears stale skip data");
        TextCutsceneRuntimeState.Clear();
        UnityEngine.Object.Destroy(sampleEvent); UnityEngine.Object.Destroy(sampleWorld);
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale("ko");
        yield return null;
        foreach(bool tutorial in new[]{false,true}) {
            var title=UnityEngine.Object.FindFirstObjectByType<TitleMenuController>();
            typeof(TitleMenuController).GetField("initialEndTimerCycles",Flags).SetValue(title,23);
            typeof(TitleMenuController).GetField("initialEndTimerTick",Flags).SetValue(title,5);
                        var button=(UnityEngine.UI.Button)Get(title,"playButton");
            var hover=button.GetComponent<TitlePlayHoverHandler>();Check(hover!=null,"Play hover handler installed");
            hover.OnPointerEnter(null);Check(Get(title,"selectedMenu").ToString()=="Play"&&!(bool)Get(title,"isChoosingTutorial"),"Hover selects without starting");
            hover.OnPointerEnter(null);hover.OnPointerExit(null);Check(Get(title,"selectedMenu").ToString()=="None","Hover exit restores idle");
            if(tutorial)hover.OnPointerEnter(null); // First pass tests click without hover (touch).
            button.onClick.Invoke();Check((bool)Get(title,"isChoosingTutorial"),"Single click opens choice");
            button.onClick.Invoke();hover.OnPointerExit(null);Check((bool)Get(title,"isChoosingTutorial"),"Repeated input preserves modal");
            yield return new WaitForSecondsRealtime(.7f);
            var popup=UnityEngine.Object.FindFirstObjectByType<TutorialChoicePopup>();Check(popup!=null&&popup.IsOpen,"Choice popup opens");
            Check(((TMP_Text)Get(popup,"questionText")).text=="튜토리얼을 진행하시겠습니까?","Question localization");
            Call(title,"OnClickSetting");Check(!(bool)Get(title,"isLoadingScene"),"Background input blocked");
            if(!tutorial){ScreenCapture.CaptureScreenshot("Logs/tutorial-choice-popup.png");yield return null;yield return null;}
            if(tutorial)popup.ChooseYes();else popup.ChooseNo();
            popup.ChooseYes(); // A second event must be ignored.
            yield return WaitScene("TextCutsceneScene");
            var cutscene=UnityEngine.Object.FindFirstObjectByType<TextCutsceneController>();
            var data=(TextCutsceneData)Get(cutscene,"cutsceneData");
            Check(data.name=="Prologue_Boot"&&data.nextSceneName=="BattleScene"&&data.nextStageBattleData!=null,"Original boot data retained");
            Check((string)Get(cutscene,"nextSceneOverride")== (tutorial?null:"WorldMapScene"),"Per-playback routing");
            Check(TextCutsceneRuntimeState.PendingCutsceneData==null&&TextCutsceneRuntimeState.ConsumeNextSceneOverride()==null,"Pending state consumed");
            // Finish at the existing completion entry point; lengthy typing is not needed for route verification.
            cutscene.StopAllCoroutines();Call(cutscene,"LoadNextScene");
            yield return WaitScene(tutorial?"BattleScene":"WorldMapScene");
            Check(RunStateManager.Instance != null && RunStateManager.Instance.RemainingEndTimerCycles==23 && RunStateManager.Instance.CurrentEndTimerTick==5,"Title timer applies to both routes");
            if(!tutorial){SceneManager.LoadScene("Title");yield return WaitScene("Title");}
        }
    }
}
