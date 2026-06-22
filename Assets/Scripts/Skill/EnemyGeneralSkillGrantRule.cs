using UnityEngine;

// <변경부분> 스테이지 시작 시 적 기물에게 일반스킬을 랜덤으로 부여하기 위한 규칙 데이터
// 예: ChanceAttack 80%, Defense 10%, Insight 30% 같은 스테이지별 적 강화 규칙을 표현한다.
[System.Serializable]
public class EnemyGeneralSkillGrantRule
{
    [Header("Skill")]
    // <변경부분> 랜덤으로 부여할 일반스킬 타입
    public GeneralSkillType skillType = GeneralSkillType.None;

    [Header("Chance")]
    // <변경부분> 이 스킬이 각 적 기물에게 부여될 확률
    // 0이면 절대 부여되지 않고, 100이면 항상 부여된다.
    [Range(0, 100)]
    public int grantChancePercent = 0;

    [Header("Level Range")]
    // <변경부분> 부여될 수 있는 최소 레벨
    public int minLevel = 1;

    // <변경부분> 부여될 수 있는 최대 레벨
    public int maxLevel = 1;

    [Header("Target Rule")]
    // <변경부분> King 기물도 이 랜덤 부여 대상에 포함할지 여부
    public bool allowKing = false;

    // <변경부분> 현재 규칙이 유효한지 확인
    public bool IsValid()
    {
        if (skillType == GeneralSkillType.None)
        {
            return false;
        }

        if (grantChancePercent <= 0)
        {
            return false;
        }

        if (minLevel <= 0)
        {
            return false;
        }

        if (maxLevel < minLevel)
        {
            return false;
        }

        return true;
    }

    // <변경부분> 확률 판정을 통해 실제로 스킬을 부여할지 결정
    public bool RollGrant()
    {
        if (IsValid() == false)
        {
            return false;
        }

        return Random.Range(0, 100) < grantChancePercent;
    }

    // <변경부분> minLevel~maxLevel 범위 안에서 랜덤 레벨 반환
    public int RollLevel()
    {
        if (maxLevel < minLevel)
        {
            return minLevel;
        }

        // Random.Range(int, int)는 두 번째 값이 exclusive라서 +1 필요
        return Random.Range(minLevel, maxLevel + 1);
    }
}