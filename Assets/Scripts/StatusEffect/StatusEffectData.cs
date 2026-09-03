using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

// <변경부분> 상태이상 기본 데이터를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "StatusEffectData", menuName = "Devorya/Status Effect Data")]
public class StatusEffectData : ScriptableObject
{
    // 상태이상 종류
    public StatusEffectType effectType;

    // 상태이상 이름
    public string effectName;

    // 상태이상 설명
    [TextArea]
    public string description;

    // <변경부분> 상태효과 설명 팝업 하단에 추가로 붙일 설명 블록 목록
    //
    // Tooltip Section 자체의 Localization은
    // 이후 Tooltip 공용 문자열 작업에서 별도로 처리한다.
    public List<TooltipSectionData> tooltipSections =
        new List<TooltipSectionData>();

    [Header("Localization")]

    // <변경부분> 현재 Locale 기준 상태효과 이름.
    //
    // Localization이 연결되지 않았거나
    // 현재 Locale의 번역값이 비어 있으면
    // 기존 effectName을 한국어 fallback으로 사용한다.
    public LocalizedString localizedEffectName =
        new LocalizedString();

    // <변경부분> 현재 Locale 기준 상태효과 설명.
    //
    // 기존 description은 삭제하지 않고
    // 한국어 원문 + fallback 데이터로 그대로 유지한다.
    public LocalizedString localizedDescription =
        new LocalizedString();

    // <변경부분> 상태이상 UI에 표시할 아이콘
    public Sprite iconSprite;

    // <변경부분> 상태이상 유지 턴
    // 퇴화는 1턴 유지
    public int durationTurn = 1;

    // <변경부분> 상태이상 최대 중첩 수
    // 현재 퇴화는 1개만 의미 있게 사용하지만, 이후 확장을 위해 데이터로 관리
    public int maxStack = 1;

    // <변경부분> 현재 Locale 기준 상태효과 이름을 반환한다.
    //
    // Localization이 아직 연결되지 않은 기존 StatusEffectData도
    // 기존 effectName으로 정상 표시되도록 fallback한다.
    public string GetLocalizedEffectName()
    {
        return GetLocalizedTextOrFallback(
            localizedEffectName,
            effectName
        );
    }

    // <변경부분> 현재 Locale 기준 상태효과 설명을 반환한다.
    public string GetLocalizedDescription()
    {
        return GetLocalizedTextOrFallback(
            localizedDescription,
            description
        );
    }

    // <변경부분> 현재 Locale 문자열을 가져오고,
    // 사용할 수 없는 경우 기존 한국어 원문을 반환한다.
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
}
