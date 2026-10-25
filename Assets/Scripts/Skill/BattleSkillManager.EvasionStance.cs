using System;
using System.Collections;
using UnityEngine;

public partial class BattleSkillManager
{
    public IEnumerator TryGrantEvasionAfterMoveRoutine(Piece piece,
        OwnedGeneralSkillData beforeAction, Action<bool> onComplete)
    {
        if (piece == null || beforeAction == null ||
            beforeAction.skillType != GeneralSkillType.EvasionStance ||
            generalSkillDatabase == null || statusEffectDatabase == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }
        var skill = generalSkillDatabase.GetData(GeneralSkillType.EvasionStance);
        var effect = statusEffectDatabase.GetData(StatusEffectType.Evasion);
        if (skill == null || effect == null || skill.GetTooltipMainValue() <= 0 ||
            UnityEngine.Random.Range(0f, 100f) >= skill.GetTooltipMainValue())
        {
            onComplete?.Invoke(false);
            yield break;
        }
        yield return piece.PlaySkillActivationIconBeforeEffectRoutine(skill.iconSprite);
        if (piece == null) { onComplete?.Invoke(false); yield break; }
        piece.AddStatusEffect(effect);
        onComplete?.Invoke(true);
    }
}
