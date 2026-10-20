using UnityEngine;

public partial class BattleSkillManager
{
    public bool TryActivateQuickStep(Piece piece, OwnedGeneralSkillData beforeAction)
    {
        if (piece == null || beforeAction == null || beforeAction.skillType != GeneralSkillType.QuickStep || generalSkillDatabase == null) return false;
        GeneralSkillData data = generalSkillDatabase.GetData(GeneralSkillType.QuickStep);
        return data != null && Random.Range(0f, 100f) < Mathf.Clamp(data.quickStepPercent, 0, 100);
    }
}