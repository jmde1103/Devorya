using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleSkillManager
{
    private readonly List<Piece> reinforcementCandidates = new List<Piece>();

    private bool HasReinforcementEffects()
    {
        return statusEffectDatabase != null &&
            statusEffectDatabase.GetData(StatusEffectType.Breakthrough) != null &&
            statusEffectDatabase.GetData(StatusEffectType.Defence) != null;
    }

    private bool CanReinforce(Piece caster)
    {
        if (caster == null || caster.HasStatusEffect(StatusEffectType.ActionDisabled) || !HasReinforcementEffects()) return false;
        JelluSynthesisRules.FillReinforcementMaterials(caster, boardManager, pieceManager, reinforcementCandidates);
        return reinforcementCandidates.Count > 0;
    }

    public bool CanUseReinforcementTarget(Piece caster, Piece target)
    {
        return caster != null && caster.UniqueSkill == UniqueSkillType.JelluReinforce &&
            !caster.HasStatusEffect(StatusEffectType.ActionDisabled) && HasReinforcementEffects() &&
            JelluSynthesisRules.IsReinforcementMaterial(caster, target, pieceManager);
    }

    private IEnumerator UseJelluReinforcementRoutine(Piece caster, Piece material, Action<bool> onComplete)
    {
        JelluSynthesisRules.FillReinforcementMaterials(caster, boardManager, pieceManager, reinforcementCandidates);
        if (reinforcementCandidates.Count == 0) { onComplete?.Invoke(false); yield break; }
        material = reinforcementCandidates[UnityEngine.Random.Range(0, reinforcementCandidates.Count)];
        if (!CanUseReinforcementTarget(caster, material)) { onComplete?.Invoke(false); yield break; }
        Vector3 materialStart = material.transform.position;
        yield return pieceManager.PlaySynthesisMaterialMoveAnimation(material, null, caster);
        if (!CanUseReinforcementTarget(caster, material))
        {
            if (material != null) material.transform.position = materialStart;
            onComplete?.Invoke(false);
            yield break;
        }
        // Material consumption is not a combat death and does not trigger death effects.
        pieceManager.RemovePiece(material);
        caster.AddStatusEffect(statusEffectDatabase.GetData(StatusEffectType.Breakthrough), 1);
        caster.AddStatusEffect(statusEffectDatabase.GetData(StatusEffectType.Defence), 1);
        onComplete?.Invoke(true);
    }
}
