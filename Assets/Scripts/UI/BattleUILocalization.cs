using UnityEngine.Localization;

// 전투 화면에서 사용하는 공용 UI 안내 문구 Localization.
//
// 실제 번역 문자열은 Battle_UI String Table에서 관리하고,
// 코드에는 기존 한국어 문구만 fallback으로 유지한다.
public static class BattleUILocalization
{
    private const string TableCollectionName =
    "Battle_UI";

    // <변경부분> 전투 시작 전 Player 기물 자리 배치 안내
    //
    // 기존 Battle_UI Localization 완료 항목.
    // Serialized Data와 관계없는 String Table Key이므로 기존 Key를 절대 변경하지 않는다.
    private const string DeploymentInstructionKey =
        "battle.ui.deployment_instruction";

    // <변경부분> 마지막 Enemy 기물 마무리 흡수 안내
    //
    // 기존 KR / EN / JA 테스트 완료 항목.
    // Reward Localization을 추가하더라도 기존 Key를 삭제하거나 교체하면 안 된다.
    private const string FinalAbsorptionInstructionKey =
        "battle.ui.final_absorption_instruction";


    // ============================================================
    // Reward Popup
    // ============================================================

    // <변경부분> 복구 보상 영역 제목
    private const string RewardRecoveryTitleKey =
        "battle.ui.reward.recovery_title";

    // <변경부분> 전투 드롭 보상 영역 제목
    private const string RewardDropTitleKey =
        "battle.ui.reward.drop_title";

    // <변경부분> Reward Popup 하단 계속 진행 안내
    private const string RewardContinueKey =
      "battle.ui.reward.continue";


    // ============================================================
    // Reward Gold Runtime UI
    // ============================================================

    // <변경부분>
    // Gold의 title / category / description은
    // TooltipData_Gold가 Localization SSOT로 관리한다.
    //
    // Battle_UI에서는 정적 Gold 콘텐츠를 중복 관리하지 않고,
    // 런타임 Gold 수량이 들어가는 이 문장만 관리한다.
    //
    // {gold}에는 현재 RunState의 실제 Gold 수량이 들어간다.
    private const string RewardCurrentGoldKey =
        "battle.ui.reward.current_gold";

    // 기존 Battle_UI Localization 항목.
    //
    // Gold Tooltip 정적 문자열 제거와 관계없는 기존 항목이므로
    // 반드시 유지한다.
    private static readonly LocalizedString
        deploymentInstruction =
            new LocalizedString(
                TableCollectionName,
                DeploymentInstructionKey
            );

    // Reward Popup 복구 영역 제목.
    private static readonly LocalizedString
        rewardRecoveryTitle =
            new LocalizedString(
                TableCollectionName,
                RewardRecoveryTitleKey
            );

    // Reward Popup 전투 드롭 영역 제목.
    private static readonly LocalizedString
        rewardDropTitle =
            new LocalizedString(
                TableCollectionName,
                RewardDropTitleKey
            );

    // Reward Popup 하단 계속 진행 안내.
    private static readonly LocalizedString
        rewardContinue =
            new LocalizedString(
                TableCollectionName,
                RewardContinueKey
            );

    // <변경부분>
    // Gold의 정적 Tooltip 콘텐츠는 TooltipData_Gold가 담당하고,
    // Battle_UI에는 런타임 현재 보유량 문장만 유지한다.
    private static readonly LocalizedString
        rewardCurrentGold =
            new LocalizedString(
                TableCollectionName,
                RewardCurrentGoldKey
            );

    private static readonly LocalizedString
        finalAbsorptionInstruction =
            new LocalizedString(
                TableCollectionName,
                FinalAbsorptionInstructionKey
            );

    // 플레이어 초기 기물 배치 안내.
    public static string GetDeploymentInstruction()
    {
        return GetLocalizedTextOrFallback(
            deploymentInstruction,
            "기물 자리 배치를 진행하고\n 체크 버튼을 누르세요."
        );
    }

    // 마지막 Enemy 기물 마무리 흡수 안내.
    public static string GetFinalAbsorptionInstruction()
    {
        return GetLocalizedTextOrFallback(
            finalAbsorptionInstruction,
            "흡수 버튼을 눌러 \n마무리 흡수를 사용하세요."
        );
    }

    public static string GetRewardRecoveryTitle(
    string fallbackText)
    {
        return GetLocalizedTextOrFallback(
            rewardRecoveryTitle,
            fallbackText
        );
    }

    public static string GetRewardDropTitle(
        string fallbackText)
    {
        return GetLocalizedTextOrFallback(
            rewardDropTitle,
            fallbackText
        );
    }

    public static string GetRewardContinueText(
    string fallbackText)
    {
        return GetLocalizedTextOrFallback(
            rewardContinue,
            fallbackText
        );
    }


    // <변경부분> 현재 RunState Gold 수량을
    // 현재 Locale 문장 안의 {gold} 위치에 삽입한다.
    //
    // Smart String 의존성을 추가하지 않고
    // 기존 DEVORYA 공용 Localization 방식과 동일하게
    // 문자열 Resolve 후 placeholder만 교체한다.
    public static string GetRewardCurrentGoldText(
        int currentGoldAmount)
    {
        string localizedText =
            GetLocalizedTextOrFallback(
                rewardCurrentGold,
                "현재 보유 금화: {gold}"
            );

        int safeGoldAmount =
            currentGoldAmount < 0
                ? 0
                : currentGoldAmount;

        return localizedText.Replace(
            "{gold}",
            safeGoldAmount.ToString("N0")
        );
    }

    // 현재 Locale 기준 번역 문자열을 반환한다.
    //
    // Localization 참조가 없거나 값이 비어 있으면
    // 기존 한국어 문구를 fallback으로 사용한다.
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