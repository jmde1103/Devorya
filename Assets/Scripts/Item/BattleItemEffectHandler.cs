using UnityEngine;

// <변경부분> 전투 아이템의 실제 효과 실행을 담당하는 클래스
public class BattleItemEffectHandler : MonoBehaviour
{
    // 아이템 효과로 기물 정보를 변경할 때 사용하는 기물 매니저
    private PieceManager pieceManager;

    // BattleManager에서 전투 시작 시
    // 아이템 효과 핸들러를 초기화하는 함수
    public void Initialize(
        PieceManager pieceManagerRef)
    {
        pieceManager =
            pieceManagerRef;
    }

    // 아이템 종류에 따라 실제 효과를 실행하는 함수
    public bool TryApplyItemEffect(
        BattleItemData itemData,
        Piece targetPiece)
    {
        if (itemData == null)
        {
            return false;
        }

        switch (itemData.itemType)
        {
            case BattleItemType.ChangeSelectedPieceToJelluPawn:
                return UseChangePieceItem(
                    itemData,
                    targetPiece
                );

            case BattleItemType.ApplyStatusEffectToSelectedPiece:
                // <변경부분> 선택한 기물에
                // 지정된 상태효과를 부여한다.
                return UseStatusEffectItem(
                    itemData,
                    targetPiece
                );

            default:
                Debug.LogWarning(
                    $"아직 구현되지 않은 아이템 효과입니다: " +
                    $"{itemData.itemType}"
                );

                return false;
        }
    }

    // BattleItemData에 저장된 값 기준으로
    // 선택한 기물 정보를 변경하는 아이템 효과
    private bool UseChangePieceItem(
        BattleItemData itemData,
        Piece targetPiece)
    {
        if (pieceManager == null)
        {
            Debug.LogWarning(
                "PieceManager가 연결되지 않아 " +
                "아이템 효과를 실행할 수 없습니다."
            );

            return false;
        }

        if (itemData == null)
        {
            return false;
        }

        if (targetPiece == null)
        {
            Debug.Log(
                "아이템을 사용할 플레이어 기물을 먼저 선택해야 합니다."
            );

            return false;
        }

        if (itemData.onlyPlayerPiece &&
            targetPiece.Team != PieceTeam.Player)
        {
            Debug.Log(
                "플레이어 기물에만 아이템을 사용할 수 있습니다."
            );

            return false;
        }

        if (itemData.blockUseOnKing &&
            targetPiece.PieceType == PieceType.King)
        {
            Debug.Log(
                "King 기물에는 이 아이템을 사용할 수 없습니다."
            );

            return false;
        }

        // PieceData가 연결되어 있으면
        // 데이터 기준으로 기물 정보를 변경한다.
        if (itemData.changeTargetPieceData != null)
        {
            targetPiece.ChangePieceData(
                itemData.changeTargetPieceData,
                itemData.useAbsorbedJelluVisual
            );
        }
        else
        {
            // PieceData가 없을 때만
            // 기존 타입·고유스킬 방식으로 변경한다.
            targetPiece.ChangePieceData(
                itemData.changeTargetPieceType,
                itemData.changeTargetUniqueSkill,
                itemData.useAbsorbedJelluVisual
            );

            Debug.LogWarning(
                $"아이템 변환 대상 PieceData가 비어 있습니다: " +
                $"{itemData.itemName}"
            );
        }

        // 데이터에 설정된 일반스킬이 있으면 부여한다.
        if (itemData.changeTargetGeneralSkill !=
            GeneralSkillType.None)
        {
            targetPiece.SetTestGeneralSkill(
                itemData.changeTargetGeneralSkill,
                itemData.changeTargetGeneralSkillLevel
            );
        }

        // 변경된 PieceData 기준으로 외형과 UI를 갱신한다.
        pieceManager.RefreshPieceVisual(
            targetPiece
        );

        Debug.Log(
            $"아이템 효과 성공: {itemData.itemName} / " +
            $"변경 타입 {itemData.changeTargetPieceType} / " +
            $"고유스킬 {itemData.changeTargetUniqueSkill}"
        );

        return true;
    }

    // <변경부분> BattleItemData에 연결된 상태효과를
    // 현재 선택한 기물에 부여하는 아이템 효과
    private bool UseStatusEffectItem(
        BattleItemData itemData,
        Piece targetPiece)
    {
        if (itemData == null)
        {
            return false;
        }

        if (targetPiece == null)
        {
            Debug.Log(
                "상태효과 아이템을 사용할 기물을 먼저 선택해야 합니다."
            );

            return false;
        }

        // 플레이어 기물 전용 조건 검사
        if (itemData.onlyPlayerPiece &&
            targetPiece.Team != PieceTeam.Player)
        {
            Debug.Log(
                "플레이어 기물에만 이 아이템을 사용할 수 있습니다."
            );

            return false;
        }

        // King 사용 제한 검사
        if (itemData.blockUseOnKing &&
            targetPiece.PieceType == PieceType.King)
        {
            Debug.Log(
                "King 기물에는 이 아이템을 사용할 수 없습니다."
            );

            return false;
        }

        // 실제 적용할 상태효과 데이터 검사
        if (itemData.applyStatusEffectData == null)
        {
            Debug.LogWarning(
                $"상태효과 아이템 데이터가 연결되지 않았습니다: " +
                $"{itemData.itemName}"
            );

            return false;
        }

        // 상태효과 부여
        targetPiece.AddStatusEffect(
            itemData.applyStatusEffectData
        );

        Debug.Log(
            $"상태효과 아이템 사용 성공: " +
            $"{itemData.itemName} / " +
            $"대상 {targetPiece.Team} {targetPiece.PieceType} / " +
            $"상태효과 {itemData.applyStatusEffectData.effectType}"
        );

        return true;
    }
}