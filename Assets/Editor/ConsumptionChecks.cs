using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class ConsumptionChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static int count;
    static void Check(bool value,string message){if(!value)throw new Exception("Consumption: "+message);count++;}
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
    public static void SetupAndRun()
    {
        var collection=LocalizationEditorSettings.GetStringTableCollection("Status_Effect");
        string[] names={"공간이동","방어","회피","돌파"};
        string[] locales={"ko","en","ja"};
        string[] teleport={"자신의 진영 턴이 종료되면 필드의 빈 타일 중 한 곳으로 무작위 이동한 뒤 [공간이동] 상태가 해제됩니다.","At the end of its team's turn, moves to a random empty tile on the field, then removes [Teleportation].","自陣営のターン終了時、フィールドの空いているマスのいずれかにランダムで移動し、その後［空間移動］状態が解除されます。"};
        string[][] suffixes={null,
            new[]{" 방어에 성공하면 [방어] 상태가 해제됩니다."," Removes [Defense] after successfully blocking an attack."," 攻撃の防御に成功すると、［防御］状態が解除されます。"},
            new[]{" 회피에 성공하면 [회피] 상태가 해제됩니다."," Removes [Evasion] after successfully evading an attack."," 回避に成功すると、［回避］状態が解除されます。"},
            new[]{" 상대의 방어를 무시하여 공격에 성공하면 [돌파] 상태가 해제됩니다."," Removes [Breakthrough] after a successful attack that bypasses the target's [Defense]."," 相手の［防御］を無視して攻撃に成功すると、［突破］状態が解除されます。"}};
        var report=new List<string>();
        for(int i=0;i<names.Length;i++)
        {
            var data=AssetDatabase.LoadAssetAtPath<StatusEffectData>("Assets/Devorya/Data/StatusEffect/상태효과_"+names[i]+".asset");
            for(int j=0;j<3;j++)
            {
                var table=(StringTable)collection.GetTable(new LocaleIdentifier(locales[j]));
                var key=data.localizedDescription.TableEntryReference;
                var entry=key.KeyId!=0?table.GetEntry(key.KeyId):table.GetEntry(key.Key);
                if(entry==null)throw new Exception("Missing existing status description: "+names[i]);
                string value=i==0?teleport[j]:entry.Value.EndsWith(suffixes[i][j])?entry.Value:entry.Value+suffixes[i][j];
                entry.Value=value;
                var md=entry.GetMetadata<TooltipSemanticMetadata>();
                if(md==null){md=new TooltipSemanticMetadata();entry.AddMetadata(md);}
                string term=i==0?new[]{"[공간이동]","[Teleportation]","［空間移動］"}[j]:i==1?new[]{"[방어]","[Defense]","［防御］"}[j]:i==2?new[]{"[회피]","[Evasion]","［回避］"}[j]:new[]{"[돌파]","[Breakthrough]","［突破］"}[j];
                if(!md.spans.Exists(x=>x.text==term))md.spans.Add(new TooltipSemanticMetadata.Span{text=term,role=TooltipTextRole.Positive});
                if(j==0)data.description=value;
                report.Add(names[i]+" | "+entry.Key+" | "+locales[j]+" | "+value);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();File.WriteAllLines("Logs/consume-translations.txt",report);
        StatusAbilitiesChecks.Run();
    }
    static BattleManager Battle(SynthesisAbilityChecks.Fixture f)
    {
        var b=f.root.AddComponent<BattleManager>();b.enabled=false;
        Set(b,"boardManager",f.board);Set(b,"pieceManager",f.manager);Set(b,"battleMoveValidator",f.validator);Set(b,"battleSkillManager",f.skill);Set(b,"isBattleEnded",true);
        Set(b,"isPlayerDeploymentPhase",false);
        for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++)
        {typeof(Tile).GetProperty("X").SetValue(f.board.GetTile(x,y),x);typeof(Tile).GetProperty("Y").SetValue(f.board.GetTile(x,y),y);}
        return b;
    }
    static void Seed(bool success)
    {for(int i=0;i<100;i++){UnityEngine.Random.InitState(i);if((UnityEngine.Random.Range(0f,100)<50)==success){UnityEngine.Random.InitState(i);return;}}}
    static IEnumerator Attack(BattleManager b,SynthesisAbilityChecks.Fixture f,Piece p,int x=2,int y=2)
    {yield return (IEnumerator)typeof(BattleManager).GetMethod("ExecutePieceActionRoutine",Flags).Invoke(b,new object[]{p,f.board.GetTile(x,y)});}
    public static IEnumerator Run()
    {
        foreach(var team in new[]{PieceTeam.Player,PieceTeam.Enemy})
        foreach(string mode in new[]{"defence","evade","breakthrough","noDefence","absorbGuard","absorbBreakthrough","lastEnemy","insightEvade"})
        {
            // Absorption is player-only in the current battle rules.
            if(team==PieceTeam.Enemy && (mode=="absorbGuard"||mode=="absorbBreakthrough"||mode=="insightEvade"))continue;
            var f=new SynthesisAbilityChecks.Fixture();var b=Battle(f);
            var other=team==PieceTeam.Player?PieceTeam.Enemy:PieceTeam.Player;
            var attacker=f.Spawn("Devorya Rook",team,2,0);var target=f.Spawn("Devorya Bishop",other,2,2);
            if(mode!="lastEnemy")f.Spawn("Devorya King",other,4,5);
            if(mode!="noDefence")target.AddStatusEffect(f.statuses.GetData(StatusEffectType.Defence),3);
            attacker.AddStatusEffect(f.statuses.GetData(StatusEffectType.Breakthrough),3);
            if(mode=="defence"||mode=="evade"||mode=="insightEvade")attacker.RemoveStatusEffect(StatusEffectType.Breakthrough);
            if(mode=="evade"||mode=="insightEvade")target.AddStatusEffect(f.statuses.GetData(StatusEffectType.Evasion),3);
            if(mode=="absorbGuard"||mode=="insightEvade")target.AddGeneralSkill(GeneralSkillType.AbsorbGuard);
            Set(b,"isAbsorbMode",mode=="absorbGuard"||mode=="absorbBreakthrough"||mode=="insightEvade");
            var db=AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>("Assets/Devorya/Data/Skills/General/특성_데이터베이스.asset");
            var insight=db.GetData(GeneralSkillType.Insight);int saved=insight.insightPercent;
            try
            {
                if(mode=="insightEvade"){Set(f.skill,"generalSkillDatabase",db);attacker.AddGeneralSkill(GeneralSkillType.Insight);insight.insightPercent=100;}
                // Queries and AI candidate generation must not consume statuses.
                f.validator.GetSelectablePositions(attacker);
                new BattleAIActionGenerator(f.board,f.manager,f.validator).GenerateActions(team,new List<BattleAIAction>());
                if(mode!="noDefence")Check(target.HasStatusEffect(StatusEffectType.Defence),"Queries preserve defence");
                Seed(true);yield return Attack(b,f,attacker);yield return null;
                if(mode=="defence")
                {
                    Check(target!=null&&!target.HasStatusEffect(StatusEffectType.Defence)&&attacker.Y==0,"Defence consumed after bounce");
                    yield return Attack(b,f,attacker);yield return null;
                    Check(target==null,"No second defence without regrant");
                }
                else if(mode=="evade")Check(target!=null&&!target.HasStatusEffect(StatusEffectType.Evasion)&&target.HasStatusEffect(StatusEffectType.Defence),"Only successful evasion consumed");
                else if(mode=="absorbGuard"||mode=="insightEvade")
                {
                    Check(target!=null&&target.HasStatusEffect(StatusEffectType.Defence),"Absorb trait preserves unused defence");
                    if(mode=="insightEvade")Check(target.HasStatusEffect(StatusEffectType.Evasion),"Insight cancellation preserves evasion");
                    else Check(attacker.HasStatusEffect(StatusEffectType.Breakthrough),"Absorb guard prevents breakthrough use");
                }
                else
                {
                    Check(target==null,"Attack result preserved: "+mode);
                    bool shouldConsume=mode=="breakthrough"||mode=="absorbBreakthrough"||(mode=="lastEnemy"&&other!=PieceTeam.Enemy);
                    Check(attacker.HasStatusEffect(StatusEffectType.Breakthrough)!=shouldConsume,"Breakthrough consumes only effective bypass: "+mode);
                }
            }
            finally{insight.insightPercent=saved;f.Dispose();}
            yield return null;
        }
        {
            var f=new SynthesisAbilityChecks.Fixture();Battle(f);var p=f.Spawn("Devorya Pawn",PieceTeam.Player,0,0);
            foreach(var type in new[]{StatusEffectType.Defence,StatusEffectType.Evasion,StatusEffectType.Breakthrough,StatusEffectType.Teleport})
            {
                var d=f.statuses.GetData(type);p.AddStatusEffect(d,2);p.ReduceStatusEffectTurnAndRemoveExpired();Check(p.HasStatusEffect(type),"Untriggered effect survives remaining turn");
                p.AddStatusEffect(d,2);Check(p.RemoveStatusEffect(type)&&!p.RemoveStatusEffect(type),"Regrant and single removal remain safe");
            }
            foreach(var type in new[]{StatusEffectType.AttackDisabled,StatusEffectType.MoveDisabled,StatusEffectType.ActionDisabled,StatusEffectType.AbilitySealed})
            {
                p.AddStatusEffect(f.statuses.GetData(type),2);
                f.validator.GetSelectablePositions(p);f.validator.GetSelectablePositions(p);
                Check(p.HasStatusEffect(type),"Restriction queries do not consume persistent state");p.RemoveStatusEffect(type);
            }
            f.Dispose();yield return null;
        }
        // Existing normal/last/final absorption and bounce regressions.
        yield return (IEnumerator)typeof(AbsorbGuardSetupChecks).GetMethod("Checks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        Debug.Log("CONSUMPTION_PLAY_PASS checks="+count);
    }
}
