using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

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

    [Header("Localization")]

    // <변경부분> 플레이어에게 표시할 일반스킬 이름 Localization 참조.
    //
    // 기존 skillName은 삭제하지 않는다.
    // Localization이 연결되지 않았거나 현재 언어의 값이 비어 있으면
    // 기존 skillName을 fallback으로 사용한다.
    public LocalizedString localizedSkillName =
        new LocalizedString();

    // <변경부분> 플레이어에게 표시할 일반스킬 기본 설명 Localization 참조.
    //
    // 기존 description은 한국어 원문과 fallback 용도로 그대로 유지한다.
    public LocalizedString localizedDescription =
        new LocalizedString();

    // <변경부분> {value} / {percent}가 들어가는
    // Tooltip 설명 형식의 Localization 참조.
    //
    // 번역 Table에도 {value} 또는 {percent} 문자열을 그대로 작성하며,
    // 실제 확률 숫자 치환은 기존 런타임 코드에서 처리한다.
    public LocalizedString localizedTooltipDescriptionFormat =
        new LocalizedString();

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

    // <변경부분> 현재 선택된 Locale 기준으로
    // 플레이어에게 표시할 일반스킬 이름을 반환한다.
    //
    // Localization이 아직 연결되지 않은 기존 데이터에서는
    // 기존 skillName을 그대로 사용한다.
    public string GetLocalizedSkillName()
    {
        return GetLocalizedTextOrFallback(
            localizedSkillName,
            skillName
        );
    }

    // <변경부분> 현재 선택된 Locale 기준으로
    // 일반스킬 기본 설명을 반환한다.
    //
    // Localization이 없거나 현재 Locale 문자열이 비어 있으면
    // 기존 description을 fallback으로 사용한다.
    public string GetLocalizedDescription()
    {
        return GetLocalizedTextOrFallback(
            localizedDescription,
            description
        );
    }

    // <변경부분> LocalizedString에서 현재 Locale의 문자열만 가져온다.
    //
    // 이 함수에서는 기존 한국어 문자열로 fallback하지 않는다.
    // Tooltip처럼 여러 Localization 후보의 우선순위를 직접 판단해야 할 때 사용한다.
    private string GetLocalizedText(
        LocalizedString localizedString)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return string.Empty;
        }

        string localizedText =
            localizedString.GetLocalizedString();

        if (string.IsNullOrWhiteSpace(
                localizedText))
        {
            return string.Empty;
        }

        return localizedText;
    }

    // <변경부분> 현재 Locale 문자열이 존재하면 해당 문자열을 사용하고,
    // 존재하지 않을 경우에만 기존 한국어 원문을 fallback으로 반환한다.
    //
    // 일반적인 Name / Description 조회에서 사용한다.
    private string GetLocalizedTextOrFallback(
        LocalizedString localizedString,
        string fallbackText)
    {
        string localizedText =
            GetLocalizedText(
                localizedString
            );

        if (string.IsNullOrWhiteSpace(
                localizedText))
        {
            return fallbackText;
        }

        return localizedText;
    }

    // <변경부분> 현재 Locale 기준 Tooltip 설명을 만든다.
    //
    // 중요한 우선순위:
    //
    // 1. 현재 Locale의 Tooltip Description Format
    // 2. 현재 Locale의 Description
    // 3. 기존 한국어 Tooltip Description Format
    // 4. 기존 한국어 Description
    //
    // 현재 Locale 번역이 존재하는데도
    // 한국어 fallback이 먼저 선택되는 문제를 방지한다.
    public string GetLocalizedTooltipDescription()
    {
        // 1순위:
        // 현재 Locale의 전용 Tooltip 문장
        string sourceText =
            GetLocalizedText(
                localizedTooltipDescriptionFormat
            );

        // 2순위:
        // 현재 Locale의 기본 Description
        if (string.IsNullOrWhiteSpace(
                sourceText))
        {
            sourceText =
                GetLocalizedText(
                    localizedDescription
                );
        }

        // 3순위:
        // 현재 Locale 번역이 모두 없는 경우에만
        // 기존 한국어 Tooltip 문장을 fallback으로 사용한다.
        if (string.IsNullOrWhiteSpace(
                sourceText))
        {
            sourceText =
                tooltipDescriptionFormat;
        }

        // 4순위:
        // 기존 Tooltip 문장도 없는 경우
        // 기존 한국어 Description을 마지막 fallback으로 사용한다.
        if (string.IsNullOrWhiteSpace(
                sourceText))
        {
            sourceText =
                description;
        }

        if (string.IsNullOrEmpty(
                sourceText))
        {
            return string.Empty;
        }

        int value =
            GetTooltipMainValue();

        // <변경부분> 기존 General Skill의 동적 확률 치환 규칙은 그대로 유지한다.
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

    // <변경부분> 기존 호출부와의 호환성을 유지하기 위한 함수.
    //
    // 기존 UI에서 GetTooltipDescription()을 호출하더라도
    // 이제 자동으로 현재 Locale 기준 Tooltip을 반환한다.
    public string GetTooltipDescription()
    {
        return GetLocalizedTooltipDescription();
    }
}