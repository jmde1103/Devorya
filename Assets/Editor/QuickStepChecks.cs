using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class QuickStepChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception("QuickStep: "+message);checks++;}
    static void Set(object obj,string field,object value){obj.GetType().GetField(field,Private).SetValue(obj,value);}
    static object Get(object obj,string field)=>obj.GetType().GetField(field,Private).GetValue(obj);
    static IEnumerator Execute(BattleManager b,Piece p,Tile tile)=>(IEnumerator)typeof(BattleManager).GetMethod("ExecutePieceActionRoutine",Private).Invoke(b,new object[]{p,tile});
    public static IEnumerator Run()
    {
        var original=AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>("Assets/Devorya/Data/Skills/General/\ud2b9\uc131_\ub370\uc774\ud130\ubca0\uc774\uc2a4.asset");
        var data=original.GetData(GeneralSkillType.QuickStep);
        Check(data!=null&&data.quickStepPercent==30&&data.skillName=="빠른 발걸음","Registered trait with 30% default");
        foreach(PieceTeam team in new[]{PieceTeam.Player,PieceTeam.Enemy})
        foreach(string mode in new[]{"success","failure","blocked","absorbed","priority","skip"})
        {
            if(mode=="absorbed"&&team==PieceTeam.Enemy)continue;
            var f=new SynthesisAbilityChecks.Fixture();
            var b=f.root.AddComponent<BattleManager>();b.enabled=false;
            Set(b,"boardManager",f.board);Set(b,"pieceManager",f.manager);Set(b,"battleMoveValidator",f.validator);Set(b,"battleSkillManager",f.skill);
            Set(b,"currentTurn",team==PieceTeam.Player?BattleTurn.Player:BattleTurn.Enemy);
            Set(b,"playerDefeatCondition",BattleDefeatConditionType.None);Set(b,"enemyDefeatCondition",BattleDefeatConditionType.None);
            for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++){
                typeof(Tile).GetProperty("X").SetValue(f.board.GetTile(x,y),x);typeof(Tile).GetProperty("Y").SetValue(f.board.GetTile(x,y),y);
            }
            var db=ScriptableObject.CreateInstance<GeneralSkillDatabase>();var quick=UnityEngine.Object.Instantiate(data);
            quick.quickStepPercent=mode=="failure"?0:100;
            var chance=UnityEngine.Object.Instantiate(original.GetData(GeneralSkillType.ChanceAttack));chance.chanceAttackPercent=100;
            Set(db,"generalSkillDatas",new[]{quick,chance});Set(f.skill,"generalSkillDatabase",db);
            PieceTeam other=team==PieceTeam.Player?PieceTeam.Enemy:PieceTeam.Player;
            var actor=f.Spawn("Devorya Rook",team,1,0);
            var victim=f.Spawn("Jellu Pawn",other,1,2);
            var enemy=f.Spawn("Devorya Pawn",other,1,4);
            var ally=f.Spawn("Devorya Pawn",team,4,0);
            if(mode!="absorbed")actor.AddGeneralSkill(GeneralSkillType.QuickStep);
            else{victim.AddGeneralSkill(GeneralSkillType.QuickStep);Set(b,"isAbsorbMode",true);}
            if(mode=="blocked")victim.AddStatusEffect(f.statuses.GetData(StatusEffectType.Defence));
            if(mode=="priority")actor.AddGeneralSkill(GeneralSkillType.ChanceAttack);
            var turn=Get(b,"currentTurn");
            yield return Execute(b,actor,f.board.GetTile(1,2));
            if(mode=="success"||mode=="skip")
            {
                Check(actor.IsQuickStepMove&&!actor.CanAttackThisTurn&&actor.CanMoveThisTurn,"Move-only bonus activated");
                Check(Get(b,"currentTurn").Equals(turn)&&b.GetChanceAttackBonusPiece()==actor,"Turn retained for same piece");
                actor.SetUniqueSkillForEvent(UniqueSkillType.JelluSynthesis);
                Check(!actor.CanUseUniqueSkill()&&!f.skill.CanUseUniqueSkillEffect(actor),"Ability unavailable during extra move");
                Check(!b.TryExecuteBattleAction(actor,new Vector2Int(1,4)),"Direct capture rejected");
                Check(!b.TryExecuteBattleAction(ally,new Vector2Int(4,1)),"Other piece action rejected");
                var positions=f.validator.GetSelectablePositions(actor);
                Check(positions.Count>0&&positions.TrueForAll(p=>f.manager.GetPieceAt(p.x,p.y)==null),"All highlighted moves are empty");
                var actions=new List<BattleAIAction>();new BattleAIActionGenerator(f.board,f.manager,f.validator).GenerateActions(team,actions);
                Check(!actions.Exists(a=>a.ActingPiece==actor&&a.ActionType!=BattleAIActionType.Move),"AI has no attack or ability candidates");
                if(mode=="skip")typeof(BattleManager).GetMethod("EndTurn",Private).Invoke(b,null);
                else yield return Execute(b,actor,f.board.GetTile(2,2));
                Check(!actor.IsQuickStepMove&&actor.CanAttackThisTurn&&b.GetChanceAttackBonusPiece()==null,"Restriction and selection clear after move or skip");
                Check(!Get(b,"currentTurn").Equals(turn),"Exactly one extra move then turn ends");
                Check(enemy!=null,"Adjacent enemy not captured during bonus");
            }
            else if(mode=="priority")Check(!actor.IsQuickStepMove&&b.GetChanceAttackBonusPiece()==actor&&actor.CanAttackThisTurn,"Existing full extra action takes precedence");
            else Check(!actor.IsQuickStepMove&&!Get(b,"currentTurn").Equals(turn),"No bonus on failed roll, blocked attack or newly absorbed trait");
            f.Dispose();UnityEngine.Object.Destroy(db);UnityEngine.Object.Destroy(quick);UnityEngine.Object.Destroy(chance);yield return null;
        }
        {
            var f=new SynthesisAbilityChecks.Fixture();Set(f.skill,"generalSkillDatabase",original);
            var p=f.Spawn("Devorya Pawn",PieceTeam.Player,2,2);p.AddGeneralSkill(GeneralSkillType.QuickStep);
            var snapshot=p.GetGeneralSkillDataCopy(GeneralSkillType.QuickStep);var state=UnityEngine.Random.state;
            try{for(int seed=0;seed<100;seed++){
                UnityEngine.Random.InitState(seed);bool expected=UnityEngine.Random.Range(0f,100f)<30;
                UnityEngine.Random.InitState(seed);Check(f.skill.TryActivateQuickStep(p,snapshot)==expected,"Exact 30% roll threshold");
            }}finally{UnityEngine.Random.state=state;}
            Check(!f.skill.TryActivateQuickStep(p,null),"Pre-action ownership required");
            f.Dispose();yield return null;
        }
        File.WriteAllText("Logs/quickstep-results.txt","PASS: "+checks+" quick-step assertions; chance, real attacks, blocked attacks, player/AI restrictions, absorption, priority and turn cleanup.\n");
        Debug.Log("QuickStep checks PASS: "+checks);
    }
}
