using UnityEngine;

// <변경부분> 스테이지 시작 시 적 기물에게
// 일반스킬을 확률적으로 부여하는 규칙 데이터
[System.Serializable]
public class EnemyGeneralSkillGrantRule
{
    [Header("Skill")]
    // 랜덤으로 부여할 일반스킬 타입
    public GeneralSkillType skillType =
        GeneralSkillType.None;

    [Header("Chance")]
    // 각 적 기물에게 이 스킬이 부여될 확률
    // 0이면 부여되지 않고 100이면 항상 부여된다.
    [Range(0, 100)]
    public int grantChancePercent = 0;

    [Header("Target Rule")]
    // King도 랜덤 부여 대상에 포함할지 여부
    public bool allowKing = false;

    // 현재 규칙이 유효한지 확인한다.
    public bool IsValid()
    {
        if (skillType ==
            GeneralSkillType.None)
        {
            return false;
        }

        return grantChancePercent > 0;
    }

    // 확률 판정을 통해 실제로 스킬을 부여할지 결정한다.
    public bool RollGrant()
    {
        if (IsValid() == false)
        {
            return false;
        }

        return Random.Range(
            0,
            100
        ) < grantChancePercent;
    }
}