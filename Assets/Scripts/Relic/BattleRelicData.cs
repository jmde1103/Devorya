using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

// <변경부분> 전투 중 보유하면 지속 효과를 제공하는 유물 하나의 기본 데이터를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "BattleRelicData", menuName = "Devorya/Battle/Relic Data")]
public class BattleRelicData : ScriptableObject
{
    [Header("Basic")]
    // 유물 종류
    public BattleRelicType relicType = BattleRelicType.None;

    // 인스펙터와 로그에서 확인할 유물 이름
    public string relicName;

    // 유물 슬롯에 표시할 아이콘 이미지
    public Sprite iconSprite;

    // 유물 효과 설명
    [TextArea]
    public string description;

    // 유물 설명 팝업 하단에 추가로 표시할 Tooltip Section 목록.
    //
    // Section의 이름 / 설명 / 아이콘 / Category는
    // 연결된 StatusEffectData와 Localization에서 자동으로 가져온다.
    public List<TooltipSectionData> tooltipSections =
        new List<TooltipSectionData>();

    [Header("Localization")]

    // 플레이어에게 표시할 유물 이름 Localization 참조.
    //
    // 기존 relicName은 한국어 원문 및 fallback으로 유지한다.
    public LocalizedString localizedRelicName =
        new LocalizedString();

    // 플레이어에게 표시할 유물 설명 Localization 참조.
    //
    // 기존 description은 한국어 원문 및 fallback으로 유지한다.
    public LocalizedString localizedDescription =
        new LocalizedString();

    // 현재 Locale 기준 유물 이름을 반환한다.
    //
    // Localization이 연결되지 않았거나 번역값이 비어 있으면
    // 기존 한국어 relicName을 사용한다.
    public string GetLocalizedRelicName()
    {
        return GetLocalizedTextOrFallback(
            localizedRelicName,
            relicName
        );
    }

    // 현재 Locale 기준 유물 설명을 반환한다.
    //
    // Localization이 연결되지 않았거나 번역값이 비어 있으면
    // 기존 한국어 description을 사용한다.
    public string GetLocalizedDescription()
    {
        return GetLocalizedTextOrFallback(
            localizedDescription,
            description
        );
    }

    // LocalizedString에서 현재 Locale 문자열을 가져온다.
    //
    // 사용할 수 있는 문자열이 없다면 기존 한국어 원문을
    // fallback으로 사용하여 기존 Asset 호환성을 유지한다.
    private string GetLocalizedTextOrFallback(
        LocalizedString localizedString,
        string fallbackText)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return fallbackText ?? string.Empty;
        }

        string localizedText =
            localizedString.GetLocalizedString();

        if (string.IsNullOrWhiteSpace(
                localizedText))
        {
            return fallbackText ?? string.Empty;
        }

        return localizedText;
    }

    [Header("Absorb Chance Attack Effect")]
    // <변경부분> 유물 효과가 플레이어 턴에만 발동 가능한지 여부
    public bool onlyPlayerTurn = true;

    // <변경부분> 한 플레이어 턴에 1번만 발동 가능한지 여부
    public bool oncePerTurn = true;

    // <변경부분> 추가 행동 가능한 이동/공격 타일이 있어야만 발동할지 여부
    public bool requireSelectableTile = true;

    // 유물 발동 확률.
    // 100이면 확정 발동, 50이면 50% 확률로 발동한다.
    public float triggerChancePercent = 100f;

    // 유물 발동 시 흡수 직후
    // 해당 기물의 고유스킬 사용 제한을 해제할지 여부.
    public bool enableUniqueSkillAfterAbsorb = true;
}