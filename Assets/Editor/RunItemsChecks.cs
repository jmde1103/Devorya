using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
[InitializeOnLoad]
public static class RunItemsChecks
{
    const string Pending="RunItemsChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception("RunItems: "+message);count++;}
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,Flags).GetValue(o);
    static RunItemsChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);var host=new GameObject("RunItemsChecksHost");UnityEngine.Object.DontDestroyOnLoad(host);host.AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    public static void SetupAndRun()
    {
        var report=new List<string>();
        foreach(var guid in AssetDatabase.FindAssets("t:BattleItemData"))
        {
            var item=AssetDatabase.LoadAssetAtPath<BattleItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if(!BattleItemEffectHandler.ChangesPiece(item))continue;
            var collection=LocalizationEditorSettings.GetStringTableCollection(item.localizedDescription.TableReference.TableCollectionName);
            if(collection==null)throw new Exception("Missing item localization "+item.name);
            string[] locales={"ko","en","ja"},suffix={" [킹]에게는 사용할 수 없습니다."," Cannot be used on a [King]."," ［キング］には使用できません。"},terms={"[킹]","[King]","［キング］"};
            for(int i=0;i<3;i++)
            {
                var table=(StringTable)collection.GetTable(new LocaleIdentifier(locales[i]));var key=item.localizedDescription.TableEntryReference;
                var entry=key.KeyId!=0?table.GetEntry(key.KeyId):table.GetEntry(key.Key);
                if(entry==null)throw new Exception("Missing item entry "+item.name);
                if(!entry.Value.EndsWith(suffix[i]))entry.Value+=suffix[i];
                var md=entry.GetMetadata<TooltipSemanticMetadata>();if(md==null){md=new TooltipSemanticMetadata();entry.AddMetadata(md);}
                if(!md.spans.Exists(x=>x.text==terms[i]))md.spans.Add(new TooltipSemanticMetadata.Span{text=terms[i],role=TooltipTextRole.PieceType});
                if(i==0)item.description=entry.Value;EditorUtility.SetDirty(table);report.Add(item.name+" | "+entry.Key+" | "+locales[i]+" | "+entry.Value);
            }
            EditorUtility.SetDirty(item);
        }
        AssetDatabase.SaveAssets();File.WriteAllLines("Logs/run-items-translations.txt",report);Run();
    }
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static IEnumerator Guard()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Checks());
        while(stack.Count>0){object next;try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        Debug.Log("RUN_ITEMS_PLAY_PASS checks="+count);EditorApplication.Exit(0);
    }
    static void SeedMap()
    {
        WorldMapRuntimeState.SetCurrentNode("previous-run-node");
        foreach(var field in typeof(WorldMapRuntimeState).GetFields(BindingFlags.Static|BindingFlags.NonPublic))
        {
            var value=field.GetValue(null);
            if(value is HashSet<string> set)set.Add("previous-run-node");
        }
    }
    static void AssertMapClear()
    {
        Check(string.IsNullOrEmpty(WorldMapRuntimeState.CurrentNodeId),"Current node reset");
        foreach(var field in typeof(WorldMapRuntimeState).GetFields(BindingFlags.Static|BindingFlags.NonPublic))
        {
            var value=field.GetValue(null);
            if(value is IDictionary dict)Check(dict.Count==0,"Map dictionary cleared: "+field.Name);
            if(value is HashSet<string> set)Check(set.Count==0,"Map nodes cleared: "+field.Name);
        }
    }
    public static IEnumerator StateAndFrozenChecks()
    {
        RunStateManager.ResetCurrentRun();
        var stage=AssetDatabase.LoadAssetAtPath<StageBattleData>("Assets/Devorya/Data/Battle/StageBattleData_Battle.asset");
        RunStateManager.SetNewRunEndTimer(1,23,stage,"BattleScene");var run=RunStateManager.Instance;
        run.AdvanceEndTimer(1);
        Check(run.EndTimerDestination==EndTimerDestinationType.Battle&&run.TryPrepareEndDestination(out var scene)&&scene=="BattleScene"&&WorldMapRuntimeState.IsDirectBattle,"Legacy battle destination and payload registration");
        RunStateManager.ResetCurrentRun();RunStateManager.SetNewRunEndTimer(1,23,null,"BattleScene",EndTimerDestinationType.Event,null,"DoesNotExist");run.AdvanceEndTimer(1);
        Check(!run.TryPrepareEndDestination(out _)&&!run.EndDestinationEntered,"Invalid ending scene never consumes ending entry");
        RunStateManager.ResetCurrentRun();RunStateManager.SetNewRunEndTimer(2,3);
        Check(run.EndTimerDestination==EndTimerDestinationType.Battle,"Legacy API clears previous event destination");
        // Seed actual map dictionaries (fog and shop stock), not just node sets.
        foreach(var field in typeof(WorldMapRuntimeState).GetFields(BindingFlags.Static|BindingFlags.NonPublic))
        {
            if(field.GetValue(null) is IDictionary dict && field.FieldType.IsGenericType)
            {
                var valueType=field.FieldType.GetGenericArguments()[1];
                dict["old-run"]=Activator.CreateInstance(valueType);
            }
        }
        RunStateManager.ResetCurrentRun();AssertMapClear();RunStateManager.SetNewRunEndTimer(2,3);
        var f=new SynthesisAbilityChecks.Fixture();
        var b=(BattleManager)typeof(StatusAbilitiesChecks).GetMethod("Battle",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{f});
        var piece=f.Spawn("Devorya Pawn",PieceTeam.Enemy,0,0);piece.AddStatusEffect(f.statuses.GetData(StatusEffectType.Teleport));
        run.BeginStageEndTimerOverride(18,true,StageEndTimerExitMode.RestoreOriginal);
        Set(b,"isBattleEnded",false);Set(b,"currentTurn",BattleTurn.Enemy);
        typeof(BattleManager).GetMethod("EndTurn",Flags).Invoke(b,null);
        float limit=Time.realtimeSinceStartup+20;
        while(Get<bool>(b,"isResolvingTurnEnd")){if(Time.realtimeSinceStartup>limit)throw new Exception("Frozen timer blocked turn");yield return null;}
        Check(Get<BattleTurn>(b,"currentTurn")==BattleTurn.Player&&!piece.HasStatusEffect(StatusEffectType.Teleport)&&run.CurrentEndTimerTick==18,"Frozen end timer preserves teleport and normal turn progression");
        run.EndStageEndTimerOverride();Check(run.CurrentEndTimerTick==3,"Frozen stage restores saved time");
        run.BeginStageEndTimerOverride(19,false,StageEndTimerExitMode.KeepChangedTime);run.AdvanceEndTimer(1);run.EndStageEndTimerOverride();Check(run.CurrentEndTimerTick==20,"KeepChangedTime preserves stage elapsed time");
        f.Dispose();RunStateManager.ResetCurrentRun();UnityEngine.Object.Destroy(run.gameObject);yield return null;
        Debug.Log("END_TIMER_REGRESSION_PASS checks="+count);
    }
    static IEnumerator Checks()
    {
        yield return (IEnumerator)typeof(BattleItemUXChecks).GetMethod("Checks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        var bm=BattleManager.Instance;var pm=Get<PieceManager>(bm,"pieceManager");var handler=Get<BattleItemEffectHandler>(bm,"battleItemEffectHandler");
        var king=pm.GetPieceAt(0,0);
        var effect=ScriptableObject.CreateInstance<StatusEffectData>();effect.effectType=StatusEffectType.Defence;
        var item=ScriptableObject.CreateInstance<BattleItemData>();item.itemType=BattleItemType.ApplyStatusEffectToSelectedPiece;item.applyStatusEffectData=effect;item.blockUseOnKing=true;
        Check(handler.TryApplyItemEffect(item,king)&&king.HasStatusEffect(StatusEffectType.Defence)&&king.PieceType==PieceType.King,"Legacy King flag does not block status item");
        item.itemType=BattleItemType.ChangeSelectedPieceToJelluPawn;
        Check(handler.ValidateItemEffect(item,king)==BattleItemFailure.King&&!handler.TryApplyItemEffect(item,king)&&king.PieceType==PieceType.King,"Transformation King rejected without mutation");
        // Validate the whole transformation list before any effect mutates the piece.
        var nonKing=pm.GetPieceAt(1,1);
        var composite=ScriptableObject.CreateInstance<BattleItemData>();composite.itemType=BattleItemType.ChangeSelectedPieceToJelluPawn;composite.useItemEffectList=true;
        composite.itemEffects=new List<BattleItemEffectEntry>{new BattleItemEffectEntry{effectType=BattleItemEffectType.ChangePiece,changeTargetPieceType=PieceType.King},new BattleItemEffectEntry{effectType=BattleItemEffectType.ChangePiece,changeTargetPieceType=PieceType.Pawn}};
        var originalType=nonKing.PieceType;
        Check(handler.ValidateItemEffect(composite,nonKing)==BattleItemFailure.King&&!handler.TryApplyItemEffect(composite,nonKing)&&nonKing.PieceType==originalType,"Projected King transformation fails atomically");
        composite.itemEffects[1]=new BattleItemEffectEntry{effectType=BattleItemEffectType.ApplyStatusEffect,statusEffectData=effect};
        Check(handler.TryApplyItemEffect(composite,nonKing)&&nonKing.PieceType==PieceType.King&&nonKing.HasStatusEffect(StatusEffectType.Defence),"Status after permitted non-King transformation remains valid");
        var manager=Get<BattleItemManager>(bm,"battleItemManager");var inventory=RunStateManager.Instance;
        bm.CancelItemTargeting();Set(bm,"itemMovePiece",null);typeof(BattleManager).GetMethod("ClearQuickStepBonus",Flags).Invoke(bm,null);
        while(inventory.GetBattleItemsCopy().Count>0)inventory.RemoveBattleItemAt(0);
        manager.Initialize(bm,Get<BattleUIController>(bm,"battleUIController"));
        item.itemType=BattleItemType.ApplyStatusEffectToSelectedPiece;king.RemoveStatusEffect(StatusEffectType.Defence);manager.AddBattleItem(item);
        Set(bm,"currentTurn",BattleTurn.Player);Set(bm,"isActionAnimating",false);bm.UseItemAtSlot(0);yield return null;
        var kingTile=Get<BoardManager>(bm,"boardManager").GetTile(king.X,king.Y);bm.SelectTile(kingTile);
        Check(Get<Piece>(bm,"selectedPiece")==king&&inventory.GetBattleItemsCopy().Count==1,"King first click selects without consuming");yield return null;bm.SelectTile(kingTile);
        Check(king.PieceType==PieceType.King&&king.HasStatusEffect(StatusEffectType.Defence)&&inventory.GetBattleItemsCopy().Count==0,"King confirmation consumes item once and preserves identity");
        var slot=new GameObject("SelectionVisualCheck",typeof(RectTransform),typeof(Image),typeof(SlotHoverFeedback));
        var feedback=slot.GetComponent<SlotHoverFeedback>();feedback.SetSelected(true,Color.red,0,1.15f);feedback.OnPointerExit(null);
        yield return new WaitForSecondsRealtime(.3f);
        Check(slot.transform.localScale.x>1.1f&&slot.GetComponent<Outline>().enabled&&slot.GetComponent<Outline>().effectColor.r>.9f,"Selected item stays enlarged and red after pointer exit");
        feedback.SetSelected(false,Color.red,0);yield return new WaitForSecondsRealtime(.3f);Check(slot.transform.localScale.x<1.01f&&!slot.GetComponent<Outline>().enabled,"Deselection restores visual");UnityEngine.Object.Destroy(slot);
        var run=RunStateManager.Instance;
        bm.CancelItemTargeting();bm.enabled=false;
        for(int repeat=0;repeat<2;repeat++)
        {
            SeedMap();TextCutsceneRuntimeState.SetPendingCutsceneData(ScriptableObject.CreateInstance<TextCutsceneData>());WorldMapEventRuntimeState.Queue(ScriptableObject.CreateInstance<WorldMapEventData>());
            run.BeginStageEndTimerOverride(18,true,StageEndTimerExitMode.RestoreOriginal);
            RunStateManager.ResetCurrentRun();AssertMapClear();
            Check(TextCutsceneRuntimeState.PendingCutsceneData==null&&WorldMapEventRuntimeState.Consume()==null,"Cutscene and map event cleared");
            Check(run.GetBattleItemsCopy().Count==0&&run.GetPlayerPieceCount()==0&&run.GetBattleRelicsCopy().Count==0,"Run inventories cleared");
            RunStateManager.SetNewRunEndTimer(2,3,null,"BattleScene");
            Check(run.CurrentEndTimerTick==3&&!run.EndDestinationEntered,"Fresh timer applied");
            Check(run.BeginStageEndTimerOverride(18,true,StageEndTimerExitMode.RestoreOriginal),"Old stage override reset");
            run.AdvanceEndTimer(1);Check(run.CurrentEndTimerTick==18,"Frozen timer remains fixed");run.EndStageEndTimerOverride();Check(run.CurrentEndTimerTick==3,"Original timer restored");
        }
        var eventData=ScriptableObject.CreateInstance<EventSceneData>();
        RunStateManager.SetNewRunEndTimer(1,23,null,"BattleScene",EndTimerDestinationType.Event,eventData,"EventScene");run.AdvanceEndTimer(1);
        Check(run.TryPrepareEndDestination(out var scene)&&scene=="EventScene","Event destination prepared");
        Check(RunStateManager.TryConsumeEndEvent(out var delivered)&&delivered==eventData,"Configured event data delivered");
        Check(!run.TryPrepareEndDestination(out _),"Final event cannot repeat");
        RunStateManager.ResetCurrentRun();RunStateManager.SetNewRunEndTimer(1,23,null,"BattleScene",EndTimerDestinationType.Event,null,"EventScene");run.AdvanceEndTimer(1);
        Check(run.TryPrepareEndDestination(out scene),"Explicit None event prepared");
        yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return null;
        var eventController=UnityEngine.Object.FindFirstObjectByType<EventSceneController>();
        Check(eventController!=null&&Get<EventSceneData>(eventController,"eventSceneData")==null&&Get<bool>(eventController,"hasStarted"),"Actual EventScene skips Inspector fallback on explicit None");
        Check(!run.TryPrepareEndDestination(out _),"Returning from event cannot trigger another ending");
        // Actual BattleScene defeat -> unchanged game-over sequence -> Title, twice.
        for(int repeat=0;repeat<2;repeat++)
        {
            RunStateManager.ResetCurrentRun();RunStateManager.SetNewRunEndTimer(2,0);
            yield return SceneManager.LoadSceneAsync("BattleScene");yield return new WaitForSecondsRealtime(5);
            bm=BattleManager.Instance;bm.StopAllCoroutines();bm.enabled=false;
            SeedMap();WorldMapEventRuntimeState.Queue(ScriptableObject.CreateInstance<WorldMapEventData>());
            var flow=UnityEngine.Object.FindFirstObjectByType<BattleEndFlowController>();
            flow.HandleBattleEnd(BattleResult.Lose);
            float limit=Time.realtimeSinceStartup+45;
            while(SceneManager.GetActiveScene().name!="Title"){if(Time.realtimeSinceStartup>limit)throw new Exception("Game over did not reach Title");yield return null;}
            AssertMapClear();Check(WorldMapEventRuntimeState.Consume()==null&&Time.timeScale==1,"Game over clears pending events and restores time");
            RunStateManager.SetNewRunEndTimer(3,7);Check(run.CurrentEndTimerTick==7&&run.RemainingEndTimerCycles==3&&!run.EndDestinationEntered,"Restart after defeat gets fresh timer");
        }
    }
}
