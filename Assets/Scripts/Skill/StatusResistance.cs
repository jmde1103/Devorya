using System.Collections.Generic;
using UnityEngine;

public static class StatusResistance
{
    public static int GetPercent(GeneralSkillType trait)
    {
        switch (trait)
        {
            case GeneralSkillType.AttackDisabledResistanceBasic: return 15;
            case GeneralSkillType.AttackDisabledResistanceIntermediate: return 30;
            case GeneralSkillType.AttackDisabledResistanceAdvanced: return 50;
            case GeneralSkillType.ActionDisabledResistanceBasic: return 10;
            case GeneralSkillType.ActionDisabledResistanceIntermediate: return 20;
            case GeneralSkillType.ActionDisabledResistanceAdvanced: return 30;
            case GeneralSkillType.MoveDisabledResistanceBasic: return 15;
            case GeneralSkillType.MoveDisabledResistanceIntermediate: return 30;
            case GeneralSkillType.MoveDisabledResistanceAdvanced: return 50;
            case GeneralSkillType.AbilitySealedResistanceBasic: return 15;
            case GeneralSkillType.AbilitySealedResistanceIntermediate: return 30;
            case GeneralSkillType.AbilitySealedResistanceAdvanced: return 50;
            default: return 0;
        }
    }

    public static StatusEffectType GetEffect(GeneralSkillType trait)
    {
        switch (trait)
        {
            case GeneralSkillType.AttackDisabledResistanceBasic:
            case GeneralSkillType.AttackDisabledResistanceIntermediate:
            case GeneralSkillType.AttackDisabledResistanceAdvanced: return StatusEffectType.AttackDisabled;
            case GeneralSkillType.ActionDisabledResistanceBasic:
            case GeneralSkillType.ActionDisabledResistanceIntermediate:
            case GeneralSkillType.ActionDisabledResistanceAdvanced: return StatusEffectType.ActionDisabled;
            case GeneralSkillType.MoveDisabledResistanceBasic:
            case GeneralSkillType.MoveDisabledResistanceIntermediate:
            case GeneralSkillType.MoveDisabledResistanceAdvanced: return StatusEffectType.MoveDisabled;
            case GeneralSkillType.AbilitySealedResistanceBasic:
            case GeneralSkillType.AbilitySealedResistanceIntermediate:
            case GeneralSkillType.AbilitySealedResistanceAdvanced: return StatusEffectType.AbilitySealed;
            default: return StatusEffectType.None;
        }
    }

    public static int GetChance(IReadOnlyList<OwnedGeneralSkillData> traits, StatusEffectType effect)
    {
        if (traits == null || effect == StatusEffectType.None) return 0;
        int chance = 0;
        foreach (var trait in traits)
            if (trait != null && GetEffect(trait.skillType) == effect)
                chance = Mathf.Max(chance, GetPercent(trait.skillType));
        return chance;
    }

    public static bool TryResist(IReadOnlyList<OwnedGeneralSkillData> traits, StatusEffectType effect)
    {
        int chance = GetChance(traits, effect);
        // Do not consume random values for effects without applicable resistance.
        return chance > 0 && Random.Range(0, 100) < chance;
    }
}
