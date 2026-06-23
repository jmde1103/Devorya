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
                // <변경부분> 젤루 폰 변환 효과도 BattleItemData에 저장된 값 기준으로 실행
                return UseChangePieceItem(itemData, targetPiece);

            default:
                Debug.LogWarning($"아직 구현되지 않은 아이템 효과입니다: {itemData.itemType}");
                return false;
        }
    }

    // <변경부분> BattleItemData에 저장된 값 기준으로 선택한 기물 정보를 변경하는 아이템 효과
    private bool UseChangePieceItem(BattleItemData itemData, Piece targetPiece)
    {
        // 기물 매니저가 없으면 효과 실행 불가
        if (pieceManager == null)
        {
            Debug.LogWarning("PieceManager가 연결되지 않아 아이템 효과를 실행할 수 없습니다.");
            return false;
        }

        // 아이템 데이터가 없으면 효과 실행 실패
        if (itemData == null)
        {
            return false;
        }

        // 선택된 기물이 없으면 아이템 효과 실행 실패
        if (targetPiece == null)
        {
            Debug.Log("아이템을 사용할 플레이어 기물을 먼저 선택해야 합니다.");
            return false;
        }

        // <변경부분> 데이터에서 플레이어 기물 전용 여부를 확인
        if (itemData.onlyPlayerPiece && targetPiece.Team != PieceTeam.Player)
        {
            Debug.Log("플레이어 기물에만 아이템을 사용할 수 있습니다.");
            return false;
        }

        // <변경부분> 데이터에서 King 사용 금지 여부를 확인
        if (itemData.blockUseOnKing && targetPiece.PieceType == PieceType.King)
        {
            Debug.Log("King 기물에는 이 아이템을 사용할 수 없습니다.");
            return false;
        }

        // <변경부분> PieceData가 연결되어 있으면 PieceData 기준으로 기물 정보를 변경
        // RefreshPieceVisual()은 CurrentPieceData 기준으로 외형을 갱신하므로 이 방식이 기본이다.
        if (itemData.changeTargetPieceData != null)
        {
            targetPiece.ChangePieceData(
                itemData.changeTargetPieceData,
                itemData.useAbsorbedJelluVisual
            );
        }
        else
        {
            // <변경부분> PieceData가 비어 있을 때만 기존 방식으로 타입/고유스킬만 변경
            // 이 경우 CurrentPieceData는 바뀌지 않으므로 외형 갱신은 기존 데이터 기준으로 유지될 수 있다.
            targetPiece.ChangePieceData(
                itemData.changeTargetPieceType,
                itemData.changeTargetUniqueSkill,
                itemData.useAbsorbedJelluVisual
            );

            Debug.LogWarning($"아이템 변환 대상 PieceData가 비어 있습니다: {itemData.itemName}");
        }

        // <변경부분> 데이터에 설정된 일반스킬이 있으면 지정 레벨로 부여
        if (itemData.changeTargetGeneralSkill != GeneralSkillType.None)
        {
            targetPiece.SetTestGeneralSkill(
                itemData.changeTargetGeneralSkill,
                itemData.changeTargetGeneralSkillLevel
            );
        }

        // <변경부분> 데이터 변경 후 필드 외형 / 스테이터스 이미지 / 타입 아이콘 위치를 즉시 갱신
        pieceManager.RefreshPieceVisual(targetPiece);

        Debug.Log($"아이템 효과 성공: {itemData.itemName} / 변경 타입 {itemData.changeTargetPieceType} / 고유스킬 {itemData.changeTargetUniqueSkill}");

        return true;
    }
}
