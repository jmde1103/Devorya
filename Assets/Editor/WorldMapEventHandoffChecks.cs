using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class WorldMapEventHandoffChecks
{
    const string Pending="Devorya.WorldMapEventHandoffChecks";
    static int checks;
    static WorldMapEventHandoffChecks(){EditorApplication.playModeStateChanged+=s=>{
        if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){
            SessionState.SetBool(Pending,false);new GameObject("WorldMapHandoffChecks").AddComponent<PieceAnimationManager>().StartCoroutine(Guard(Checks()));
        }
    };}
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static void Check(bool b,string message){if(!b)throw new Exception(message);checks++;}
    static WorldMapEventData Data(){var d=ScriptableObject.CreateInstance<WorldMapEventData>();d.steps.Add(new WorldMapEventStep{type=WorldMapEventStepType.Wait,waitDuration=20});return d;}
    static IEnumerator Checks()
    {
        var a=Data();var b=Data();WorldMapEventRuntimeState.Clear();
        WorldMapEventRuntimeState.Queue(null);Check(WorldMapEventRuntimeState.Consume()==null,"Empty transition is safe");
        WorldMapEventRuntimeState.Queue(a);WorldMapEventRuntimeState.Queue(null);Check(WorldMapEventRuntimeState.Consume()==a,"Intermediate scene without data preserves handoff");
        Check(WorldMapEventRuntimeState.Consume()==null,"Consumed handoff does not replay");
        WorldMapEventRuntimeState.Queue(a);WorldMapEventRuntimeState.Queue(b);Check(WorldMapEventRuntimeState.Consume()==b,"New explicit data replaces pending handoff");
        WorldMapEventRuntimeState.Queue(a);WorldMapRuntimeState.Clear();Check(WorldMapEventRuntimeState.Consume()==null,"New run clears pending handoff");
        var first=new GameObject("WorldMapFirstVisit").AddComponent<WorldMapEventController>();
        typeof(WorldMapEventController).GetField("eventData",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(first,a);
        for(int i=0;i<5;i++)yield return null;
        Check(!first.IsPlaying,"Inspector fallback does not autoplay on entry");UnityEngine.Object.Destroy(first.gameObject);yield return null;
        WorldMapEventRuntimeState.Queue(a);var second=new GameObject("WorldMapRequestedVisit").AddComponent<WorldMapEventController>();
        for(int i=0;i<5;i++)yield return null;
        Check(second.IsPlaying&&WorldMapEventRuntimeState.Consume()==null,"Pending event starts and clears immediately");second.StopEvent();UnityEngine.Object.Destroy(second.gameObject);yield return null;
        var third=new GameObject("WorldMapReturnVisit").AddComponent<WorldMapEventController>();for(int i=0;i<5;i++)yield return null;
        Check(!third.IsPlaying,"Returning without new data does not repeat");UnityEngine.Object.Destroy(third.gameObject);yield return null;
        WorldMapEventRuntimeState.Queue(b);var fourth=new GameObject("WorldMapNextRequestedVisit").AddComponent<WorldMapEventController>();for(int i=0;i<5;i++)yield return null;
        Check(fourth.IsPlaying,"Later request still runs");fourth.StopEvent();UnityEngine.Object.Destroy(fourth.gameObject);yield return null;
        foreach(var pair in new[]{(typeof(EventSceneData),"completionWorldMapEventData"),(typeof(EventSequenceData),"completionWorldMapEventData"),(typeof(EventSceneStepData),"worldMapEventData"),(typeof(TextCutsceneData),"nextWorldMapEventData"),(typeof(StageBattleData),"victoryWorldMapEventData")})
            Check(pair.Item1.GetField(pair.Item2)?.FieldType==typeof(WorldMapEventData),"Optional transition data exists: "+pair.Item1.Name);
        UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);WorldMapEventRuntimeState.Clear();
        yield return (IEnumerator)typeof(WorldMapEventSetup).GetMethod("Checks",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);bool failed=false;float deadline=Time.realtimeSinceStartup+60;
        while(stack.Count>0){object next;try{if(Time.realtimeSinceStartup>deadline)throw new Exception("Timeout");if(!stack.Peek().MoveNext()){stack.Pop();continue;}next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);failed=true;break;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        Debug.Log((failed?"FAIL":"PASS")+": World map event handoff "+checks+" checks plus existing event playback checks.");EditorApplication.Exit(failed?1:0);
    }
}
