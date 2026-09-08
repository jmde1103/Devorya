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
    // Reward Recovery Piece Tooltip
    // ============================================================

    // <변경부분>
    // 복구 기물 Tooltip Category.
    private const string RewardRecoveryCategoryKey =
        "battle.ui.reward.recovery_category";

    // <변경부분>
    // 복구 기물 안내 문장.
    //
    // {piece}에는 PieceData.GetLocalizedDisplayName() 결과가 들어간다.
    private const string RewardRecoveryDescriptionKey =
        "battle.ui.reward.recovery_description";


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


    // ============================================================
    // Ability Failure Popup
    // ============================================================
    //
    // 개별 Ability 자체의 조건 실패 문구는
    // UniqueSkillData.condition_fail을 SSOT로 계속 사용한다.
    //
    // 여기서는 BattleManager가 판단하는
    // 공통 Ability 사용 실패 상황만 Battle_UI에서 관리한다.

    private const string AbilityFailActionInProgressKey =
        "battle.ui.ability_fail.action_in_progress";

    private const string AbilityFailSelectPieceKey =
        "battle.ui.ability_fail.select_piece";

    private const string AbilityFailWrongTurnPieceKey =
        "battle.ui.ability_fail.wrong_turn_piece";

    private const string AbilityFailDataMissingKey =
        "battle.ui.ability_fail.data_missing";

    private const string AbilityFailAlreadyUsedKey =
        "battle.ui.ability_fail.already_used";

    private const string AbilityFailNoAbilityKey =
        "battle.ui.ability_fail.no_ability";

    private const string AbilityFailCooldownRemainingKey =
        "battle.ui.ability_fail.cooldown_remaining";

    private const string AbilityFailUnavailableKey =
        "battle.ui.ability_fail.unavailable";

    private const string AbilityFailRequirementsNotMetKey =
        "battle.ui.ability_fail.requirements_not_met";

    private const string AbilityFailConditionUnmetKey =
        "battle.ui.ability_fail.condition_unmet";


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

    // Reward Recovery Tooltip Category.
    private static readonly LocalizedString
        rewardRecoveryCategory =
            new LocalizedString(
                TableCollectionName,
                RewardRecoveryCategoryKey
            );

    // Reward Recovery Tooltip 설명.
    private static readonly LocalizedString
        rewardRecoveryDescription =
            new LocalizedString(
                TableCollectionName,
                RewardRecoveryDescriptionKey
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


    // Ability Failure Popup 공통 문구.
    private static readonly LocalizedString
        abilityFailActionInProgress =
            new LocalizedString(
                TableCollectionName,
                AbilityFailActionInProgressKey
            );

    private static readonly LocalizedString
        abilityFailSelectPiece =
            new LocalizedString(
                TableCollectionName,
                AbilityFailSelectPieceKey
            );

    private static readonly LocalizedString
        abilityFailWrongTurnPiece =
            new LocalizedString(
                TableCollectionName,
                AbilityFailWrongTurnPieceKey
            );

    private static readonly LocalizedString
        abilityFailDataMissing =
            new LocalizedString(
                TableCollectionName,
                AbilityFailDataMissingKey
            );

    private static readonly LocalizedString
        abilityFailAlreadyUsed =
            new LocalizedString(
                TableCollectionName,
                AbilityFailAlreadyUsedKey
            );

    private static readonly LocalizedString
        abilityFailNoAbility =
            new LocalizedString(
                TableCollectionName,
                AbilityFailNoAbilityKey
            );

    private static readonly LocalizedString
        abilityFailCooldownRemaining =
            new LocalizedString(
                TableCollectionName,
                AbilityFailCooldownRemainingKey
            );

    private static readonly LocalizedString
        abilityFailUnavailable =
            new LocalizedString(
                TableCollectionName,
                AbilityFailUnavailableKey
            );

    private static readonly LocalizedString
        abilityFailRequirementsNotMet =
            new LocalizedString(
                TableCollectionName,
                AbilityFailRequirementsNotMetKey
            );

    private static readonly LocalizedString
        abilityFailConditionUnmet =
            new LocalizedString(
                TableCollectionName,
                AbilityFailConditionUnmetKey
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

    // <변경부분>
    // 복구 기물 Tooltip Category를
    // 현재 Locale 기준으로 반환한다.
    public static string GetRewardRecoveryCategory()
    {
        return GetLocalizedTextOrFallback(
            rewardRecoveryCategory,
            "기물 복구"
        );
    }

    // <변경부분>
    // 현재 Locale의 Recovery 안내 문장을 가져온 뒤
    // {piece}를 실제 PieceData 표시명으로 교체한다.
    public static string GetRewardRecoveryDescription(
        string pieceDisplayName)
    {
        string localizedText =
            GetLocalizedTextOrFallback(
                rewardRecoveryDescription,
                "전투 종료 후 복구된 {piece} 기물입니다."
            );

        return localizedText.Replace(
            "{piece}",
            pieceDisplayName ?? string.Empty
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


    // ============================================================
    // Ability Failure Popup
    // ============================================================

    // 다른 전투 행동 연출이 진행 중일 때.
    public static string GetAbilityFailActionInProgress()
    {
        return GetLocalizedTextOrFallback(
            abilityFailActionInProgress,
            "현재 다른 행동이 진행 중입니다."
        );
    }

    // Ability를 사용할 기물이 선택되지 않았을 때.
    public static string GetAbilityFailSelectPiece()
    {
        return GetLocalizedTextOrFallback(
            abilityFailSelectPiece,
            "능력을 사용할 기물을 먼저 선택해야 합니다."
        );
    }

    // 현재 턴의 기물이 아닐 때.
    public static string GetAbilityFailWrongTurnPiece()
    {
        return GetLocalizedTextOrFallback(
            abilityFailWrongTurnPiece,
            "현재 턴의 기물만 능력을 사용할 수 있습니다."
        );
    }

    // UniqueSkillData 연결이 누락된 예외 상황.
    public static string GetAbilityFailDataMissing()
    {
        return GetLocalizedTextOrFallback(
            abilityFailDataMissing,
            "능력 데이터를 찾을 수 없습니다."
        );
    }

    // oncePerTurn Ability를 이번 턴에 이미 사용했을 때.
    public static string GetAbilityFailAlreadyUsed()
    {
        return GetLocalizedTextOrFallback(
            abilityFailAlreadyUsed,
            "이번 턴에는 이미 능력을 사용했습니다."
        );
    }

    // 선택한 기물이 Ability를 보유하지 않았을 때.
    public static string GetAbilityFailNoAbility()
    {
        return GetLocalizedTextOrFallback(
            abilityFailNoAbility,
            "이 기물은 능력이 없습니다."
        );
    }

    // Ability 쿨타임이 남아 있을 때.
    //
    // {turn} placeholder는 String Table의 KR / EN / JA에서
    // 어순을 자유롭게 구성할 수 있도록 문장 전체를 Localization한다.
    public static string GetAbilityFailCooldownRemaining(
        int remainingTurns)
    {
        string localizedText =
            GetLocalizedTextOrFallback(
                abilityFailCooldownRemaining,
                "능력 쿨타임이 {turn}턴 남았습니다."
            );

        int safeRemainingTurns =
            remainingTurns < 0
                ? 0
                : remainingTurns;

        return localizedText.Replace(
            "{turn}",
            safeRemainingTurns.ToString()
        );
    }

    // Ability가 존재하지만 현재 사용할 수 없는 공통 fallback.
    public static string GetAbilityFailUnavailable()
    {
        return GetLocalizedTextOrFallback(
            abilityFailUnavailable,
            "현재 능력을 사용할 수 없습니다."
        );
    }

    // requiredDeathStack 등 Battle 공통 선행 조건이 부족할 때.
    public static string GetAbilityFailRequirementsNotMet()
    {
        return GetLocalizedTextOrFallback(
            abilityFailRequirementsNotMet,
            "능력 사용 조건이 부족합니다."
        );
    }

    // 개별 UniqueSkillData.condition_fail을 얻지 못했을 때만
    // 사용하는 최종 공통 fallback.
    //
    // 개별 Ability 조건 실패 문구의 SSOT를
    // Battle_UI로 옮기는 용도가 아니다.
    public static string GetAbilityFailConditionUnmet()
    {
        return GetLocalizedTextOrFallback(
            abilityFailConditionUnmet,
            "조건이 맞지 않아 능력을 사용할 수 없습니다."
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