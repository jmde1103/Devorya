using System;
using System.Collections;
using System.IO;

using UnityEngine;

public static class StatusFeedbackChecks
{
    static int checks;
    static void Check(bool value,string message) { if(!value)throw new Exception("Ability revision: "+message); checks++; }

    public static IEnumerator Run()
    {
        yield return new WaitForSeconds(.7f);
        var f=new SynthesisAbilityChecks.Fixture();
        var attacker=f.Spawn("Devorya Rook",PieceTeam.Player,2,0);
        var defender=f.Spawn("Devorya Pawn",PieceTeam.Enemy,2,2);
        defender.AddStatusEffect(f.statuses.GetData(StatusEffectType.Defence));

        Vector3 start=attacker.transform.position;
        bool escaped=false,done=false,overlap=false;
        Vector3 defenderStart=defender.transform.position,previousAttacker=attacker.transform.position,previousDefender=defenderStart;
        f.animation.StartCoroutine(Observe(f.manager.PlayEvadedAttack(attacker,defender,new Vector2Int(3,2),()=>{
            Check(Vector3.Distance(defenderStart,defender.transform.position)<.01f,"Dodge waits for attack slam");
            Check(Vector3.Distance(start,attacker.transform.position)>.1f,"Attacker approached before dodge");
            escaped=true;
        }),()=>done=true));
        float deadline=Time.time+5;
        while(!done&&Time.time<deadline){
            if(escaped&&Vector3.Distance(previousAttacker,attacker.transform.position)>.001f&&Vector3.Distance(previousDefender,defender.transform.position)>.001f)overlap=true;
            if(!escaped)Check(Vector3.Distance(defenderStart,defender.transform.position)<.01f,"No premature evasion movement");
            previousAttacker=attacker.transform.position;previousDefender=defender.transform.position;
            yield return null;
        }
        Check(done&&escaped&&overlap,"Attack slam and evasion move in the same frames");
        Check(defender.X==3&&defender.Y==2,"Dodge grid position committed");
        f.Dispose();yield return null;

        foreach(string id in new[]{"Jellu Pawn","Jellu Knight","Jellu Bishop","Jellu Rook","Jellu King","Devorya Pawn","Devorya Knight","Devorya Bishop","Devorya Rook","Devorya King","Dummy Pawn","Dummy Knight","Dummy Rook"}) foreach(PieceTeam team in new[]{PieceTeam.Player,PieceTeam.Enemy}){
            f=new SynthesisAbilityChecks.Fixture();var rook=f.Spawn(id,team,2,2); rook.SetUniqueSkillForEvent(UniqueSkillType.JelluReinforce);
            var a=f.Spawn("Devorya Pawn",team,1,2);var b=f.Spawn("Jellu Pawn",team,3,2);
            var bishop=f.Spawn("Devorya Bishop",team,2,1);var king=f.Spawn("Jellu King",team,2,3);
            Check(f.skill.CanUseUniqueSkillEffect(rook),"Any species and type can reinforce: "+id); bool used=false;
            yield return f.skill.TryUseUniqueSkillRoutine(rook,null,result=>used=result);yield return null;
            Check(used&&(a==null^b==null),"Exactly one adjacent allied pawn absorbed");
            Check(bishop!=null&&king!=null,"Other piece types remain untouched");
            Check(rook.HasStatusEffect(StatusEffectType.Defence)&&rook.HasStatusEffect(StatusEffectType.Breakthrough),"Random reinforcement grants both effects");
            f.Dispose();yield return null;
        }
        Debug.Log("Ability revision checks PASS: "+checks);
    }
    static IEnumerator Observe(IEnumerator routine,Action done){yield return routine;done();}
}
