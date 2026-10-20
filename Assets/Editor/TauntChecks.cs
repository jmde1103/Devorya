using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class TauntChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int assertions;
    static void Check(bool value, string message) { if(!value) throw new Exception("Taunt: "+message); assertions++; }
    static void Set(object obj,string field,object value) { obj.GetType().GetField(field,Private).SetValue(obj,value); }
    static BattleManager Battle(SynthesisAbilityChecks.Fixture f)
    {
        var b=f.root.AddComponent<BattleManager>(); b.enabled=false;
        Set(b,"boardManager",f.board); Set(b,"pieceManager",f.manager); Set(b,"battleMoveValidator",f.validator); Set(b,"battleSkillManager",f.skill);
        Set(b,"isBattleEnded",true); // Stop automatic turn progression after resolving the real attack.
        for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++)
        {
            Tile t=f.board.GetTile(x,y);
            typeof(Tile).GetProperty("X").SetValue(t,x); typeof(Tile).GetProperty("Y").SetValue(t,y);
        }
        return b;
    }
    public static IEnumerator Run()
    {
        foreach(PieceTeam defending in new[]{PieceTeam.Player,PieceTeam.Enemy})
        foreach(string mode in new[]{"normal","defence","breakthrough","absorb","degeneration"})
        {
            if(mode=="absorb"&&defending==PieceTeam.Player)continue;
            var f=new SynthesisAbilityChecks.Fixture(); var b=Battle(f);
            PieceTeam attacking=defending==PieceTeam.Player?PieceTeam.Enemy:PieceTeam.Player;
            var attacker=f.Spawn("Devorya Rook",attacking,2,0);
            var target=f.Spawn("Devorya King",defending,2,2);
            var guard=f.Spawn("Jellu Bishop",defending,1,2);
            var taunt=f.statuses.GetData(StatusEffectType.Taunt);
            Check(taunt!=null&&taunt.durationTurn==1&&taunt.maxStack==1&&taunt.effectName=="도발","Registered one-turn asset");
            guard.AddStatusEffect(taunt);
            guard.AddStatusEffect(f.statuses.GetData(StatusEffectType.ActionDisabled),1);
            guard.AddGeneralSkill(GeneralSkillType.Insight);
            if(mode=="defence"||mode=="breakthrough")guard.AddStatusEffect(f.statuses.GetData(StatusEffectType.Defence),1);
            if(mode=="breakthrough")attacker.AddStatusEffect(f.statuses.GetData(StatusEffectType.Breakthrough),1);
            if(mode=="degeneration")guard.AddStatusEffect(f.statuses.GetData(StatusEffectType.Degeneration),1);
            if(mode=="absorb")Set(b,"isAbsorbMode",true);
            Check(f.manager.FindTauntInterceptor(attacker,target)==guard,"Same-team guard selected even when action disabled");
            yield return (IEnumerator)typeof(BattleManager).GetMethod("ExecutePieceActionRoutine",Private).Invoke(b,new object[]{attacker,f.board.GetTile(2,2)});
            yield return null;
            Check(target!=null&&target.PieceType==PieceType.King&&target.X==1&&target.Y==2,"Original target saved and moved to guard square");
            Check(f.manager.GetPieceAt(1,2)==target&&target.CurrentTile==f.board.GetTile(1,2),"Protected piece board/tile synchronized");
            Check(Vector3.Distance(target.transform.position,new Vector3(1,2.25f,0))<.001f,"Swap animation reaches correct position");
            if(mode=="defence")
            {
                Check(guard!=null&&f.manager.GetPieceAt(2,2)==guard&&guard.CurrentTile==f.board.GetTile(2,2),"Guard defence absorbs redirected hit");
                Check(attacker.X==2&&attacker.Y==0,"Blocked attacker returns to origin");
                Check(guard.HasStatusEffect(StatusEffectType.Taunt),"Intercept does not consume taunt");
                guard.ReduceActionRestrictionTurnAndRemoveExpired();Check(guard.HasStatusEffect(StatusEffectType.Taunt),"No expiry at turn end");
                guard.ReduceStatusEffectTurnAndRemoveExpired();Check(!guard.HasStatusEffect(StatusEffectType.Taunt),"Expiry at next own turn start");
            }
            else
            {
                Check(guard==null&&f.manager.GetPieceAt(2,2)==attacker,"Guard captured instead of original target");
                if(mode=="absorb")Check(attacker.PieceType==PieceType.Bishop&&attacker.HasGeneralSkill(GeneralSkillType.Insight),"Absorption uses guard data and traits");
                if(mode=="degeneration")
                {
                    Piece newborn=null; foreach(Piece p in f.root.GetComponentsInChildren<Piece>())if(p.Team==defending&&p.PieceType==PieceType.Pawn)newborn=p;
                    Check(newborn!=null&&newborn.HasGeneralSkill(GeneralSkillType.Insight),"Guard degeneration and inherited traits execute");
                }
            }
            f.Dispose(); yield return null;
        }
        // Selection coverage: every adjacent direction, non-neighbors, same-team attacks, neutral and expired guards.
        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
        {
            if(dx==0&&dy==0)continue;
            var f=new SynthesisAbilityChecks.Fixture();
            var attacker=f.Spawn("Devorya Rook",PieceTeam.Player,0,0);
            var target=f.Spawn("Devorya Pawn",PieceTeam.Enemy,2,2);
            var guard=f.Spawn("Jellu Pawn",PieceTeam.Enemy,2+dx,2+dy);
            guard.AddStatusEffect(f.statuses.GetData(StatusEffectType.Taunt));
            Check(f.manager.FindTauntInterceptor(attacker,target)==guard,"Eight-neighbor selection");
            guard.ReduceStatusEffectTurnAndRemoveExpired();
            Check(f.manager.FindTauntInterceptor(attacker,target)==null,"Expired guard excluded");
            f.Dispose();yield return null;
        }
        {
            var f=new SynthesisAbilityChecks.Fixture();
            var attacker=f.Spawn("Devorya Rook",PieceTeam.Player,2,0);
            var target=f.Spawn("Devorya Pawn",PieceTeam.Enemy,2,2);
            var first=f.Spawn("Jellu Pawn",PieceTeam.Enemy,1,1);
            var second=f.Spawn("Jellu Pawn",PieceTeam.Enemy,3,3);
            var far=f.Spawn("Jellu Pawn",PieceTeam.Enemy,4,5);
            var friendly=f.Spawn("Jellu Pawn",PieceTeam.Player,3,2);
            var neutral=f.Spawn("Jellu Pawn",PieceTeam.Neutral,1,2);
            foreach(var p in new[]{first,second,far,friendly,neutral,target})p.AddStatusEffect(f.statuses.GetData(StatusEffectType.Taunt));
            Check(f.manager.FindTauntInterceptor(attacker,target)==first,"Multiple guards use stable board order");
            Check(f.manager.FindTauntInterceptor(first,target)==null,"Friendly non-attack cannot intercept");
            Check(f.manager.FindTauntInterceptor(attacker,neutral)==null,"Neutral target is not protected");
            first.ReduceStatusEffectTurnAndRemoveExpired();second.ReduceStatusEffectTurnAndRemoveExpired();
            Check(f.manager.FindTauntInterceptor(attacker,target)==null,"Self, distant, neutral and opposing guards excluded");
            Check(!f.manager.TrySwapForTaunt(target,far)&&f.manager.GetPieceAt(2,2)==target,"Invalid swap leaves board intact");
            f.Dispose();yield return null;
        }
        File.WriteAllText("Logs/taunt-results.txt","PASS: "+assertions+" taunt assertions; real attack, swap, defence, breakthrough, absorption, degeneration and expiry.\n");
        Debug.Log("Taunt checks PASS: "+assertions);
    }
}
