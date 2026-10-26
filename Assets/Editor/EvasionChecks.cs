using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class EvasionChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception("Evasion: "+message);checks++;}
    static void Set(object obj,string field,object value){obj.GetType().GetField(field,Private).SetValue(obj,value);}
    static void Seed(bool success){for(int seed=0;seed<100;seed++){UnityEngine.Random.InitState(seed);if((UnityEngine.Random.Range(0f,100f)<50)==success){UnityEngine.Random.InitState(seed);return;}}throw new Exception("No seed");}
    public static IEnumerator Run()
    {
        foreach(PieceTeam team in new[]{PieceTeam.Player,PieceTeam.Enemy})
        foreach(string mode in new[]{"success","failed","defence","surrounded","taunt"})
        {
            var f=new SynthesisAbilityChecks.Fixture();var b=f.root.AddComponent<BattleManager>();b.enabled=false;
            Set(b,"boardManager",f.board);Set(b,"pieceManager",f.manager);Set(b,"battleMoveValidator",f.validator);Set(b,"battleSkillManager",f.skill);Set(b,"isBattleEnded",true);
            for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++){
                typeof(Tile).GetProperty("X").SetValue(f.board.GetTile(x,y),x);typeof(Tile).GetProperty("Y").SetValue(f.board.GetTile(x,y),y);
            }
            var attacker=f.Spawn("Devorya Rook",team,2,0);PieceTeam other=team==PieceTeam.Player?PieceTeam.Enemy:PieceTeam.Player;
            var target=f.Spawn("Devorya Bishop",other,2,2);Piece defender=target; f.Spawn("Devorya Pawn",other,4,5);
            var effect=f.statuses.GetData(StatusEffectType.Evasion);
            Check(effect!=null&&effect.effectName=="회피"&&effect.durationTurn==1,"Registered one-turn status");
            if(mode=="taunt"){
                defender=f.Spawn("Devorya Pawn",other,1,2);defender.AddStatusEffect(f.statuses.GetData(StatusEffectType.Taunt));
            }
            defender.AddStatusEffect(effect);
            if(mode=="defence")defender.AddStatusEffect(f.statuses.GetData(StatusEffectType.Defence));
            if(mode=="surrounded")for(int y=1;y<=3;y++)for(int x=1;x<=3;x++)if(x!=2||y!=2)f.Spawn("Devorya Pawn",other,x,y);
            // Even an absorb attempt misses and leaves the attacker unchanged when evasion succeeds.
            if(team==PieceTeam.Player&&mode=="success")Set(b,"isAbsorbMode",true);
            Seed(mode=="success"||mode=="taunt"||mode=="surrounded");
            yield return (IEnumerator)typeof(BattleManager).GetMethod("ExecutePieceActionRoutine",Private).Invoke(b,new object[]{attacker,f.board.GetTile(2,2)});
            yield return null;
            if(mode=="success"||mode=="taunt")
            {
                Check(defender!=null&&(defender.X!=2||defender.Y!=2),"Defender survives on a different square");
                Check(Math.Abs(defender.X-2)<=1&&Math.Abs(defender.Y-2)<=1,"Only adjacent escape square");
                Check(f.manager.GetPieceAt(2,2)==attacker&&attacker.X==2&&attacker.Y==2,"Attacker enters original target square");
                Check(f.manager.GetPieceAt(defender.X,defender.Y)==defender&&defender.CurrentTile==f.board.GetTile(defender.X,defender.Y),"Escape grid and tile agree");
                Check(attacker.PieceType==PieceType.Rook,"Evaded absorption does not copy target");
                if(mode=="taunt")Check(target!=null&&target.X==1&&target.Y==2,"Taunt-protected ally stays safe");
                Check(!defender.HasStatusEffect(StatusEffectType.Evasion),"Successful evasion consumed");
                defender.ReduceActionRestrictionTurnAndRemoveExpired();
                defender.ReduceStatusEffectTurnAndRemoveExpired();Check(!defender.HasStatusEffect(StatusEffectType.Evasion),"Effect expires next own turn");
            }
            else if(mode=="defence")Check(defender!=null&&attacker.Y==0&&!defender.HasStatusEffect(StatusEffectType.Defence)&&defender.HasStatusEffect(StatusEffectType.Evasion),"Only successful defence consumed");
            else Check(defender==null&&f.manager.GetPieceAt(2,2)==attacker,"Failed roll or no space resolves normal capture");
            f.Dispose();yield return null;
        }
        {
            var f=new SynthesisAbilityChecks.Fixture();var attacker=f.Spawn("Devorya Rook",PieceTeam.Player,0,0);var target=f.Spawn("Devorya Pawn",PieceTeam.Enemy,2,2);
            target.AddStatusEffect(f.statuses.GetData(StatusEffectType.Evasion));
            // One legal destination; all other adjacent spaces are obstructed or unwalkable.
            for(int y=1;y<=3;y++)for(int x=1;x<=3;x++)if(x!=3||y!=3)typeof(Tile).GetProperty("IsWalkable").SetValue(f.board.GetTile(x,y),false);
            var state=UnityEngine.Random.state;
            try{for(int seed=0;seed<100;seed++){
                UnityEngine.Random.InitState(seed);bool expected=UnityEngine.Random.Range(0f,100f)<50;
                UnityEngine.Random.InitState(seed);bool actual=f.manager.TryChooseEvasionDestination(attacker,target,out Vector2Int dest);
                Check(actual==expected,"Exact 50% threshold");if(actual)Check(dest==new Vector2Int(3,3),"Unwalkable squares excluded");
            }}finally{UnityEngine.Random.state=state;}
            typeof(Tile).GetProperty("HasObstacle").SetValue(f.board.GetTile(3,3),true);
            Seed(true);Check(!f.manager.TryChooseEvasionDestination(attacker,target,out _),"Obstructed final square prevents evasion");
            Check(!f.manager.TryChooseEvasionDestination(target,target,out _),"No friendly/self attack trigger");
            f.Dispose();yield return null;
        }
        File.WriteAllText("Logs/evasion-results.txt","PASS: "+checks+" evasion assertions; player/enemy attacks, 50% chance, occupied/blocked tiles, absorption, taunt, defence and expiry.\n");
        Debug.Log("Evasion checks PASS: "+checks);
    }
}
