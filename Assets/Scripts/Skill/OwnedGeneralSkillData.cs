using UnityEngine;

// <변경부분> 기물이 실제로 보유한 일반스킬 타입과 레벨을 저장하는 런타임 데이터
[System.Serializable]
public class OwnedGeneralSkillData
{
    // 보유한 일반스킬 종류
    public GeneralSkillType skillType;

    // 현재 보유 레벨
    public int level;

    // <변경부분> 일반스킬 보유 데이터 생성
    public OwnedGeneralSkillData(GeneralSkillType skillType, int level)
    {
        this.skillType = skillType;
        this.level = Mathf.Clamp(level, 1, 3);
    }

    // <변경부분> 기존 보유 일반스킬 데이터를 복사해서 행동 시작 전 상태 저장에 사용
    public OwnedGeneralSkillData(OwnedGeneralSkillData original)
    {
        if (original == null)
        {
            skillType = GeneralSkillType.None;
            level = 0;
            return;
        }

        skillType = original.skillType;
        level = original.level;
    }
}
