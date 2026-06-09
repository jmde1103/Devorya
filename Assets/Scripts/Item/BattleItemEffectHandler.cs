using UnityEngine;

// <변경부분> 전투 아이템의 실제 효과 실행을 담당하는 클래스
public class BattleItemEffectHandler : MonoBehaviour
{
    // <변경부분> 아이템 효과로 기물 정보를 변경할 때 사용하는 기물 매니저
    private PieceManager pieceManager;

    // <변경부분> BattleManager에서 전투 시작 시 아이템 효과 핸들러를 초기화하는 함수
    public void Initialize(PieceManager pieceManagerRef)
    {
        // 기물 변경/외형 갱신을 처리할 PieceManager 저장
        pieceManager = pieceManagerRef;
    }

    // <변경부분> 아이템 종류에 따라 실제 효과를 실행하는 함수
    public bool TryApplyItemEffect(BattleItemData itemData, Piece targetPiece)
    {
        // 아이템 데이터가 없으면 효과 실행 실패
        if (itemData == null)
        {
            return false;
        }

        switch (itemData.itemType)
        {
            case BattleItemType.ChangeSelectedPieceToJelluPawn:
                return UseChangePieceToJelluPawnItem(targetPiece);

            default:
                Debug.LogWarning($"아직 구현되지 않은 아이템 효과입니다: {itemData.itemType}");
                return false;
        }
    }

    // <변경부분> 선택한 플레이어 기물을 젤루 폰으로 변경하는 아이템 효과
    private bool UseChangePieceToJelluPawnItem(Piece targetPiece)
    {
        // 기물 매니저가 없으면 효과 실행 불가
        if (pieceManager == null)
        {
            Debug.LogWarning("PieceManager가 연결되지 않아 아이템 효과를 실행할 수 없습니다.");
            return false;
        }

        // 선택된 기물이 없으면 아이템 효과 실행 실패
        if (targetPiece == null)
        {
            Debug.Log("젤루 폰으로 변경할 플레이어 기물을 먼저 선택해야 합니다.");
            return false;
        }

        // 플레이어 기물만 아이템 대상으로 허용
        if (targetPiece.Team != PieceTeam.Player)
        {
            Debug.Log("플레이어 기물에만 아이템을 사용할 수 있습니다.");
            return false;
        }

        // Player King은 현재 승패 조건과 충돌할 수 있으므로 변경 불가
        if (targetPiece.PieceType == PieceType.King)
        {
            Debug.Log("Player King은 젤루 폰으로 변경할 수 없습니다.");
            return false;
        }

        // 선택 기물을 젤루 폰 정보로 변경
        pieceManager.ChangePieceToJelluPawn(targetPiece);

        Debug.Log("아이템 효과 성공: 선택한 기물을 젤루 폰으로 변경했습니다.");

        return true;
    }
}
