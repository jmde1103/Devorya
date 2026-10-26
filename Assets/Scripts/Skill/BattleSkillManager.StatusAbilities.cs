using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleSkillManager
{
    private UniqueSkillDatabase effectAbilityDatabase;
    static bool IsStatusAbility(UniqueSkillType type) =>
        type == UniqueSkillType.GuardSelf || type == UniqueSkillType.TeleportSelf ||
        type == UniqueSkillType.Virus || type == UniqueSkillType.EvadeSelf;
    static StatusEffectType SelfStatus(UniqueSkillType type) =>
        type == UniqueSkillType.GuardSelf ? StatusEffectType.Defence :
        type == UniqueSkillType.TeleportSelf ? StatusEffectType.Teleport : StatusEffectType.Evasion;
    List<Piece> VirusTargets(Piece caster)
    {
        var list = new List<Piece>();
        if (caster == null || boardManager == null || pieceManager == null) return list;
        for (int y=0;y<boardManager.Height;y++) for(int x=0;x<boardManager.Width;x++)
        {
            var target = pieceManager.GetPieceAt(x,y);
            if (target != null && target.Team != PieceTeam.Neutral && caster.IsEnemyOf(target)) list.Add(target);
        }
        return list;
    }
    bool CanUseStatusAbility(Piece piece)
    {
        if (piece == null || piece.IsQuickStepMove || piece.IsAbilityBlocked || statusEffectDatabase == null) return false;
        if (piece.UniqueSkill != UniqueSkillType.Virus) return statusEffectDatabase.GetData(SelfStatus(piece.UniqueSkill)) != null;
        var data = effectAbilityDatabase != null ? effectAbilityDatabase.GetData(UniqueSkillType.Virus) : null;
        return data != null && data.targetPixelBurstPrefab != null &&
            statusEffectDatabase.GetData(StatusEffectType.AttackDisabled) != null &&
            statusEffectDatabase.GetData(StatusEffectType.MoveDisabled) != null && VirusTargets(piece).Count > 0;
    }
    bool ApplySelfStatusAbility(Piece piece)
    {
        // Virus must use the coroutine path so particles finish before statuses are applied.
        if (piece.UniqueSkill == UniqueSkillType.Virus || !CanUseStatusAbility(piece)) return false;
        piece.AddStatusEffect(statusEffectDatabase.GetData(SelfStatus(piece.UniqueSkill)));
        return true;
    }
    void ApplyVirusStatus(Piece target)
    {
        var effect = UnityEngine.Random.Range(0, 2) == 0
            ? StatusEffectType.AttackDisabled : StatusEffectType.MoveDisabled;
        // The incoming team's turn start consumes one tick. Keep the restriction
        // active through that turn without changing shared status duration rules.
        target.AddStatusEffect(statusEffectDatabase.GetData(effect), 2);
    }
    IEnumerator UseVirusRoutine(Piece caster, Action<bool> completed)
    {
        if (!CanUseStatusAbility(caster)) { completed?.Invoke(false); yield break; }
        var animator = caster.GetComponentInChildren<PieceSpineAnimationController>();
        if (animator != null) yield return animator.PlaySelectRoutine();
        if (caster == null) { completed?.Invoke(false); yield break; }
        if (animator != null) animator.PlayIdle();
        var targets = VirusTargets(caster);
        for(int i=0;i<targets.Count;i++) { int j=UnityEngine.Random.Range(i,targets.Count); var t=targets[i];targets[i]=targets[j];targets[j]=t; }
        if(targets.Count>3) targets.RemoveRange(3,targets.Count-3);
        if(targets.Count==0) { completed?.Invoke(false); yield break; }
        var effects = new List<PixelBurstEffect>();
        try
        {
            var prefab = effectAbilityDatabase.GetData(UniqueSkillType.Virus).targetPixelBurstPrefab;
            foreach(var target in targets)
            {
                if(target==null)continue;
                var fx=Instantiate(prefab,target.transform.position,Quaternion.identity,transform);
                fx.name="VirusPixelBurst";
                foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>())
                {
                    ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main=ps.main;main.loop=false;
                }
                fx.PlayAtPosition(target.transform.position);
                effects.Add(fx);
            }
            bool alive;
            do
            {
                alive=false;
                foreach(var fx in effects) if(fx!=null)
                    foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>())
                        if(ps.IsAlive(true)) alive=true;
                if(alive)yield return null;
            }while(alive && caster!=null);
            if(caster==null) { completed?.Invoke(false);yield break; }
            bool applied=false;
            foreach(var target in targets)
            {
                if(target==null || pieceManager.GetPieceAt(target.X,target.Y)!=target)continue;
                ApplyVirusStatus(target);
                applied=true;
            }
            completed?.Invoke(applied);
        }
        finally { foreach(var fx in effects) if(fx!=null)Destroy(fx.gameObject); }
    }
}
