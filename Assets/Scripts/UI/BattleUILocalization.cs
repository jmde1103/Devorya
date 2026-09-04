using UnityEngine.Localization;

// 전투 화면에서 사용하는 공용 UI 안내 문구 Localization.
//
// 실제 번역 문자열은 Battle_UI String Table에서 관리하고,
// 코드에는 기존 한국어 문구만 fallback으로 유지한다.
public static class BattleUILocalization
{
    private const string TableCollectionName =
        "Battle_UI";

    private const string DeploymentInstructionKey =
        "battle.ui.deployment_instruction";

    private const string FinalAbsorptionInstructionKey =
        "battle.ui.final_absorption_instruction";

    // Tooltip_Common 등에서 이미 사용 중인 방식과 동일하게
    // LocalizedString 참조를 한 번 생성하고
    // 호출 시점의 현재 Locale 문자열을 가져온다.
    private static readonly LocalizedString
        deploymentInstruction =
            new LocalizedString(
                TableCollectionName,
                DeploymentInstructionKey
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