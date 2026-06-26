using System.Collections.Generic;
using UnityEngine;

// <변경부분> 일반스킬 하나의 기본 설정을 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "GeneralSkillData", menuName = "Devorya/Skill/General Skill Data")]
public class GeneralSkillData : ScriptableObject
{
    [Header("Basic")]
    // 일반스킬 종류
    public GeneralSkillType skillType = GeneralSkillType.None;

    // 인스펙터와 UI에 표시할 일반스킬 이름
    public string skillName;

    // 일반스킬 아이콘
    public Sprite iconSprite;

    // 일반스킬 기본 설명
    [TextArea]
    public string description;

    [Header("Tooltip")]
    [TextArea(2, 5)]
    // <변경부분> 레벨별 수치를 자동 반영할 Tooltip 설명 형식
    // {level} = 현재 레벨
    // {value} 또는 {percent} = 현재 레벨에 맞는 주요 수치
    // 예: 적 처치 시 {percent}% 확률로 추가 행동을 얻습니다.
    public string tooltipDescriptionFormat;

    // <변경부분> 일반스킬 설명 팝업 하단에 추가로 붙일 설명 블록 목록
    // 이름, 아이콘은 기존 skillName / iconSprite를 그대로 사용한다.
    public List<TooltipSectionData> tooltipSections = new List<TooltipSectionData>();

    [Header("Level")]
    // <변경부분> 일반스킬 최대 레벨
    public int maxLevel = 3;

    [Header("Chance Attack")]
    // <변경부분> ChanceAttack LV1 발동 확률
    public int chanceAttackLevel1Percent = 30;

    // <변경부분> ChanceAttack LV2 발동 확률
    public int chanceAttackLevel2Percent = 50;

    // <변경부분> ChanceAttack LV3 발동 확률
    public int chanceAttackLevel3Percent = 80;

    // <변경부분> ChanceAttack 연속 발동 시 적용할 확률 감소 배율
    // 예: 0.3333이면 연속 발동 1회마다 확률이 1/3로 감소
    public float chanceAttackContinuousPenaltyRate = 1f / 3f;

    [Header("Defense")]
    // <변경부분> Defense LV1 발동 확률
    public int defenseLevel1Percent = 30;

    // <변경부분> Defense LV2 발동 확률
    public int defenseLevel2Percent = 50;

    // <변경부분> Defense LV3 발동 확률
    public int defenseLevel3Percent = 80;

    [Header("Insight")]
    // <변경부분> Insight LV1 발동 확률
    public int insightLevel1Percent = 30;

    // <변경부분> Insight LV2 발동 확률
    public int insightLevel2Percent = 50;

    // <변경부분> Insight LV3 발동 확률
    public int insightLevel3Percent = 80;

    // <변경부분> 전달받은 레벨 기준 ChanceAttack 기본 발동 확률 반환
    public int GetChanceAttackPercent(int level)
    {
        switch (level)
        {
            case 1:
                return chanceAttackLevel1Percent;

            case 2:
                return chanceAttackLevel2Percent;

            case 3:
                return chanceAttackLevel3Percent;

            default:
                return 0;
        }
    }

    // <변경부분> 연속 발동 횟수에 따른 ChanceAttack 최종 배율 반환
    public float GetChanceAttackContinuousPenaltyMultiplier(int continuousCount)
    {
        if (continuousCount <= 0)
        {
            return 1f;
        }

        return Mathf.Pow(chanceAttackContinuousPenaltyRate, continuousCount);
    }

    // <변경부분> 전달받은 레벨 기준 Defense 기본 발동 확률 반환
    public int GetDefensePercent(int level)
    {
        switch (level)
        {
            case 1:
                return defenseLevel1Percent;

            case 2:
                return defenseLevel2Percent;

            case 3:
                return defenseLevel3Percent;

            default:
                return 0;
        }
    }

    // <변경부분> 전달받은 레벨 기준 Insight 기본 발동 확률 반환
    public int GetInsightPercent(int level)
    {
        switch (level)
        {
            case 1:
                return insightLevel1Percent;

            case 2:
                return insightLevel2Percent;

            case 3:
                return insightLevel3Percent;

            default:
                return 0;
        }
    }

    // <변경부분> 현재 레벨 기준 Tooltip 설명에 들어갈 주요 수치를 반환
    public int GetTooltipMainValue(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, maxLevel);

        switch (skillType)
        {
            case GeneralSkillType.ChanceAttack:
                return GetChanceAttackPercent(clampedLevel);

            case GeneralSkillType.Defense:
                return GetDefensePercent(clampedLevel);

            case GeneralSkillType.Insight:
                return GetInsightPercent(clampedLevel);
        }

        return 0;
    }

    // <변경부분> 현재 레벨 기준으로 Tooltip 설명 문장을 생성
    public string GetTooltipDescriptionByLevel(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, maxLevel);

        // Tooltip 전용 설명 형식이 비어 있으면 기존 description을 사용
        string sourceText = string.IsNullOrEmpty(tooltipDescriptionFormat)
            ? description
            : tooltipDescriptionFormat;

        int value = GetTooltipMainValue(clampedLevel);

        return sourceText
            .Replace("{level}", clampedLevel.ToString())
            .Replace("{value}", value.ToString())
            .Replace("{percent}", value.ToString());
    }
}
