using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class WornBootsChecks
{
    const string Pending="WornBootsChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Path="Assets/Devorya/Data/Item/아이템_낡은 장화.asset";
    static WornBootsChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("BootsCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    static void Set(object o,string key,object value)=>o.GetType().GetField(key,Flags).SetValue(o,value);
    static object Get(object o,string key)=>o.GetType().GetField(key,Flags).GetValue(o);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        var data=AssetDatabase.LoadAssetAtPath<BattleItemData>(Path);
        if(data==null){
            data=ScriptableObject.CreateInstance<BattleItemData>();data.itemType=BattleItemType.WornBoots;data.itemName="낡은 장화";
            data.description="선택한 아군 기물에게 [공격불가]를 1턴 부여하고, 즉시 추가 이동 기회를 1회 제공합니다.";
            data.onlyPlayerPiece=true;data.blockUseOnKing=false;data.useItemEffectList=true;
            data.iconSprite=AssetDatabase.LoadAssetAtPath<BattleItemData>("Assets/Devorya/Data/Item/아이템_투지열매.asset").iconSprite;
            var effect=AssetDatabase.LoadAssetAtPath<StatusEffectData>("Assets/Devorya/Data/StatusEffect/상태효과_공격불가.asset");
            data.itemEffects=new List<BattleItemEffectEntry>{new BattleItemEffectEntry{effectType=BattleItemEffectType.ApplyStatusEffect,statusEffectData=effect},new BattleItemEffectEntry{effectType=BattleItemEffectType.ExtraMove}};
            var collection=DevoryaLocalizationEditorUtility.GetOrCreateStringTableCollection("Battle_Item");
            string[] locales={"ko","en","ja"},names={"낡은 장화","Worn Boots","古びた長靴"},descriptions={data.description,"Applies [Attack Disabled] for 1 turn to the selected allied piece and immediately grants one extra move.","選択した味方の駒に[攻撃不可]を1ターン付与し、即座に追加移動を1回与えます。"};
            for(int i=0;i<3;i++){var table=DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection,locales[i]);DevoryaLocalizationEditorUtility.SetTableValue(table,"battle_item_WornBoots_name",names[i]);DevoryaLocalizationEditorUtility.SetTableValue(table,"battle_item_WornBoots_description",descriptions[i]);}
            data.localizedItemName=DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection,"battle_item_WornBoots_name");data.localizedDescription=DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection,"battle_item_WornBoots_description");AssetDatabase.CreateAsset(data,Path);
            var db=AssetDatabase.LoadAssetAtPath<BattleItemDatabase>("Assets/Devorya/Data/Item/아이템_데이터베이스.asset");var so=new SerializedObject(db);var list=so.FindProperty("itemDatas");int index=list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=data;so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Cases());while(stack.Count>0){object next;try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}Debug.Log("WORN_BOOTS_PASS: blocked targets, no partial mutation, move-only, original action retained, exactly one bonus and expiry");EditorApplication.Exit(0);}
    static IEnumerator Cases()
    {
        var f=new SynthesisAbilityChecks.Fixture();var b=f.root.AddComponent<BattleManager>();b.enabled=false;
        Set(b,"boardManager",f.board);Set(b,"pieceManager",f.manager);Set(b,"battleMoveValidator",f.validator);Set(b,"battleSkillManager",f.skill);Set(b,"currentTurn",BattleTurn.Player);Set(b,"isPlayerDeploymentPhase",false);Set(b,"playerDefeatCondition",BattleDefeatConditionType.None);Set(b,"enemyDefeatCondition",BattleDefeatConditionType.None);
        for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++){typeof(Tile).GetProperty("X").SetValue(f.board.GetTile(x,y),x);typeof(Tile).GetProperty("Y").SetValue(f.board.GetTile(x,y),y);}
        var actor=f.Spawn("Devorya Rook",PieceTeam.Player,1,0);f.Spawn("Jellu Pawn",PieceTeam.Enemy,4,4);
        var handler=f.root.AddComponent<BattleItemEffectHandler>();handler.Initialize(f.manager,b);var data=AssetDatabase.LoadAssetAtPath<BattleItemData>(Path);
        actor.AddStatusEffect(f.statuses.GetData(StatusEffectType.MoveDisabled));Check(!handler.TryApplyItemEffect(data,actor),"Immobile target rejected");Check(!actor.HasStatusEffect(StatusEffectType.AttackDisabled),"Failed item left no partial effect");actor.RemoveStatusEffect(StatusEffectType.MoveDisabled);
        Check(handler.TryApplyItemEffect(data,actor),"Boots used");Check(actor.IsQuickStepMove&&!actor.CanAttackThisTurn&&actor.HasStatusEffect(StatusEffectType.AttackDisabled),"Immediate move-only bonus");Check(!handler.TryApplyItemEffect(data,actor),"Pending bonus cannot be overwritten");
        yield return (IEnumerator)typeof(BattleManager).GetMethod("ExecutePieceActionRoutine",Flags).Invoke(b,new object[]{actor,f.board.GetTile(2,0)});
        Check(Get(b,"currentTurn").Equals(BattleTurn.Player)&&!actor.IsQuickStepMove&&b.GetChanceAttackBonusPiece()==null,"Extra move retains original action");Check(actor.HasStatusEffect(StatusEffectType.AttackDisabled),"Attack lock remains");
        yield return (IEnumerator)typeof(BattleManager).GetMethod("ExecutePieceActionRoutine",Flags).Invoke(b,new object[]{actor,f.board.GetTile(3,0)});
        Check(Get(b,"currentTurn").Equals(BattleTurn.Enemy),"Normal move ends turn");Check(actor.HasStatusEffect(StatusEffectType.AttackDisabled),"Effect survives enemy turn");
        actor.ReduceStatusEffectTurnAndRemoveExpired();Check(!actor.HasStatusEffect(StatusEffectType.AttackDisabled),"One-turn expiry");f.Dispose();
    }
}
