public enum BattleItemType
{
    // 아이템이 없는 상태
    [UnityEngine.InspectorName("\uc5c6\uc74c")]
    None,

    // <변경부분> 선택한 플레이어 기물을
    // 지정된 PieceData 기준으로 변경하는 아이템
    [UnityEngine.InspectorName("\uae30\ubb3c \ubcc0\ud658")]
    ChangeSelectedPieceToJelluPawn,

    // <변경부분> 선택한 기물에
    // BattleItemData에 연결된 상태효과를 부여하는 아이템
    [UnityEngine.InspectorName("\uc0c1\ud0dc\ud6a8\uacfc \ubd80\uc5ec")]
    ApplyStatusEffectToSelectedPiece
}