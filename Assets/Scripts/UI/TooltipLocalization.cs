using UnityEngine.Localization;

// <변경부분> Tooltip 전체에서 공통으로 사용하는
// 플레이어 노출 문자열 Localization을 관리한다.
//
// 개별 Item / Skill / StatusEffect Data에 속하지 않는
// Category, 남은 턴 같은 공용 UI 문자열만 담당한다.
public static class TooltipLocalization
{
    private const string TableCollectionName =
        "Tooltip_Common";

    private const string GeneralSkillCategoryKey =
        "tooltip.category.general_skill";

    private const string UniqueSkillCategoryKey =
        "tooltip.category.unique_skill";

    private const string ItemCategoryKey =
        "tooltip.category.item";

    private const string StatusEffectCategoryKey =
        "tooltip.category.status_effect";

    private const string RelicCategoryKey =
        "tooltip.category.relic";

    private const string RemainingTurnKey =
        "tooltip.status.remaining_turn";

    // <변경부분> 각 Key의 LocalizedString을 한 번만 생성해
    // TooltipViewData가 만들어질 때 현재 Locale 값을 조회한다.
    private static readonly LocalizedString
        generalSkillCategory =
            new LocalizedString(
                TableCollectionName,
                GeneralSkillCategoryKey
            );

    private static readonly LocalizedString
        uniqueSkillCategory =
            new LocalizedString(
                TableCollectionName,
                UniqueSkillCategoryKey
            );

    private static readonly LocalizedString
        itemCategory =
            new LocalizedString(
                TableCollectionName,
                ItemCategoryKey
            );

    private static readonly LocalizedString
        statusEffectCategory =
            new LocalizedString(
                TableCollectionName,
                StatusEffectCategoryKey
            );

    private static readonly LocalizedString
        relicCategory =
            new LocalizedString(
                TableCollectionName,
                RelicCategoryKey
            );

    private static readonly LocalizedString
        remainingTurn =
            new LocalizedString(
                TableCollectionName,
                RemainingTurnKey
            );

    public static string GetGeneralSkillCategory()
    {
        return GetLocalizedTextOrFallback(
            generalSkillCategory,
            "일반스킬"
        );
    }

    public static string GetUniqueSkillCategory()
    {
        return GetLocalizedTextOrFallback(
            uniqueSkillCategory,
            "고유스킬"
        );
    }

    public static string GetItemCategory()
    {
        return GetLocalizedTextOrFallback(
            itemCategory,
            "아이템"
        );
    }

    public static string GetStatusEffectCategory()
    {
        return GetLocalizedTextOrFallback(
            statusEffectCategory,
            "상태효과"
        );
    }

    public static string GetRelicCategory()
    {
        return GetLocalizedTextOrFallback(
            relicCategory,
            "유물"
        );
    }

    // <변경부분> 남은 턴 문장은 언어별 어순 차이를 고려하여
    // 문장 전체를 Localization하고 {turn} 자리만 런타임에 치환한다.
    //
    // KO: 남은 턴: {turn}턴
    // EN: Turns Remaining: {turn}
    // JA: 残りターン: {turn}
    public static string GetRemainingTurnText(
        int turn)
    {
        string format =
            GetLocalizedTextOrFallback(
                remainingTurn,
                "남은 턴: {turn}턴"
            );

        return format.Replace(
            "{turn}",
            turn.ToString()
        );
    }

    // <변경부분> 해당 Locale에 번역값이 존재하지 않을 경우
    // 기존 한국어 표시 문자열을 안전하게 fallback한다.
    private static string GetLocalizedTextOrFallback(
        LocalizedString localizedString,
        string fallbackText)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return fallbackText;
        }

        string localizedText =
            localizedString.GetLocalizedString();

        if (string.IsNullOrWhiteSpace(
                localizedText))
        {
            return fallbackText;
        }

        return localizedText;
    }
}
