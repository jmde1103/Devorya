using UnityEngine;
using UnityEngine.Localization;

public enum BattleItemFailure
{
    [InspectorName("없음")] None,
    [InspectorName("사용 불가 턴")] WrongTurn,
    [InspectorName("행동 진행 중")] Busy,
    [InspectorName("대상 사용 불가")] InvalidPiece,
    [InspectorName("킹 사용 불가")] King,
    [InspectorName("플레이어 전용")] PlayerOnly,
    [InspectorName("해제 상태 없음")] MissingStatus,
    [InspectorName("추가 이동 불가")] ExtraMove,
    [InspectorName("효과 조건 불충족")] Requirements,
    [InspectorName("대상 없음")] NoCandidates,
    [InspectorName("아이템 없음")] MissingItem,
    [InspectorName("대상 소실")] MissingTarget,
    [InspectorName("실행 실패")] Execution
}

public static class BattleItemFailureText
{
    public static string Get(BattleItemFailure reason) =>
        new LocalizedString("Battle_UI", "battle.ui.item_fail." + reason.ToString()).GetLocalizedString();
}
