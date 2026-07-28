using System.Collections.Generic;
using UnityEngine;

// <변경부분> 일반스킬 하나의 기본 설정을 관리하는 ScriptableObject
// 일반스킬 레벨 시스템은 제거되었으며,
// 각 스킬은 하나의 고정 확률과 설명만 사용한다.
[CreateAssetMenu(
    fileName = "GeneralSkillData",
    menuName = "Devorya/Skill/General Skill Data"
)]
public class GeneralSkillData : ScriptableObject
{
    [Header("Basic")]
    // 일반스킬 종류
    public GeneralSkillType skillType =
        GeneralSkillType.None;

    // Inspector와 UI에 표시할 일반스킬 이름
    public string skillName;

    // 일반스킬 아이콘
    public Sprite iconSprite;

    // 일반스킬 기본 설명
    [TextArea]
    public string description;

    [Header("Tooltip")]
    [TextArea(2, 5)]
    // <변경부분> 고정 확률을 자동 반영할 Tooltip 설명 형식
    //
    // {value} 또는 {percent} 자리에
    // 현재 일반스킬의 고정 확률이 들어간다.
    //
    // 예:
    // 적 처치 시 {percent}% 확률로 추가 행동을 얻습니다.
    //
    // Defense 예:
    // 이동 완료 시 {percent}% 확률로 방어 상태를 얻습니다.
    public string tooltipDescriptionFormat;

    // 일반스킬 설명 팝업 하단에 추가로 붙일 설명 블록 목록
    public List<TooltipSectionData> tooltipSections =
        new List<TooltipSectionData>();

    [Header("Chance Attack")]
    // <변경부분> ChanceAttack 고정 발동 확률
    [Range(0, 100)]
    public int chanceAttackPercent = 30;

    // ChanceAttack 연속 발동 시 적용할 확률 감소 배율
    // 예: 0.3333이면 연속 발동할 때마다 확률이 1/3로 감소한다.
    [Range(0f, 1f)]
    public float chanceAttackContinuousPenaltyRate =
        1f / 3f;

    [Header("Defense")]
    // <변경부분> 이동 완료 후 Defence 상태효과를 얻을 고정 확률
    // 현재 기획 기본값은 15%다.
    [Range(0, 100)]
    public int defenseGrantChancePercent = 15;

    [Header("Insight")]
    // <변경부분> Insight 고정 발동 확률
    [Range(0, 100)]
    public int insightPercent = 30;

    // <변경부분> ChanceAttack의 고정 발동 확률을 반환한다.
    public int GetChanceAttackPercent()
    {
        return Mathf.Clamp(
            chanceAttackPercent,
            0,
            100
        );
    }

    // ChanceAttack 연속 발동 횟수에 따른 최종 배율을 반환한다.
    public float GetChanceAttackContinuousPenaltyMultiplier(
        int continuousCount)
    {
        if (continuousCount <= 0)
        {
            return 1f;
        }

        float penaltyRate =
            Mathf.Clamp01(
                chanceAttackContinuousPenaltyRate
            );

        return Mathf.Pow(
            penaltyRate,
            continuousCount
        );
    }

    // <변경부분> 이동 완료 후 Defence 상태효과 부여 확률을 반환한다.
    public int GetDefenseGrantChancePercent()
    {
        return Mathf.Clamp(
            defenseGrantChancePercent,
            0,
            100
        );
    }

    // <변경부분> Insight의 고정 발동 확률을 반환한다.
    public int GetInsightPercent()
    {
        return Mathf.Clamp(
            insightPercent,
            0,
            100
        );
    }

    // <변경부분> 현재 일반스킬의 Tooltip에 표시할
    // 대표 고정 확률을 반환한다.
    public int GetTooltipMainValue()
    {
        switch (skillType)
        {
            case GeneralSkillType.ChanceAttack:
                return GetChanceAttackPercent();

            case GeneralSkillType.Defense:
                return GetDefenseGrantChancePercent();

            case GeneralSkillType.Insight:
                return GetInsightPercent();
        }

        return 0;
    }

    // <변경부분> 레벨 없이 현재 일반스킬의
    // 고정 확률을 반영한 Tooltip 설명을 생성한다.
    public string GetTooltipDescription()
    {
        string sourceText =
            string.IsNullOrEmpty(
                tooltipDescriptionFormat
            )
                ? description
                : tooltipDescriptionFormat;

        int value =
            GetTooltipMainValue();

        return sourceText
            .Replace(
                "{value}",
                value.ToString()
            )
            .Replace(
                "{percent}",
                value.ToString()
            );
    }
}