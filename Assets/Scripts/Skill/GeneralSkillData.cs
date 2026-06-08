using UnityEngine;

// <변경부분> 일반 스킬 하나의 종류와 레벨을 저장하는 데이터
[System.Serializable]
public class GeneralSkillData
{
    // 일반 스킬 종류
    public GeneralSkillType skillType;

    // 현재 일반 스킬 레벨
    public int level;

    // 일반 스킬 데이터 생성
    public GeneralSkillData(GeneralSkillType skillType, int level)
    {
        // 일반 스킬 종류 저장
        this.skillType = skillType;

        // 일반 스킬 레벨 저장
        this.level = Mathf.Clamp(level, 1, 3);
    }
}
