using UnityEngine;

// <변경부분> 전투 아이템의 실제 효과 실행을 담당하는 클래스
public partial class BattleItemEffectHandler : MonoBehaviour
{
    // 아이템 효과로 기물 정보를 변경할 때 사용하는 기물 매니저
    private PieceManager pieceManager; private BattleManager battleManager;

    // BattleManager에서 전투 시작 시
    // 아이템 효과 핸들러를 초기화하는 함수
    public void Initialize(
        PieceManager pieceManagerRef, BattleManager battleManagerRef = null)
    {
        battleManager = battleManagerRef;
        pieceManager =
            pieceManagerRef;
    }

    // 아이템 종류에 따라 실제 효과를 실행하는 함수
    public bool TryApplyItemEffect(
        BattleItemData itemData,
        Piece targetPiece)
    {
        if (ValidateItemEffect(itemData, targetPiece) != BattleItemFailure.None) return false;

        // <변경부분>
        // 새로운 목록형 효과를 사용하는 아이템이라면
        // 여러 효과를 등록 순서대로 실행한다.
        if (itemData.useItemEffectList)
        {
            return TryApplyEffectList(itemData, targetPiece);
        }

        // 기존 아이템은 원래의 단일 효과 분기를 유지한다.
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

    // <변경부분>
    // 목록에 등록한 모든 효과의 설정을 먼저 확인하고,
    // 문제가 없다면 Inspector에 배치한 순서대로 실행한다.
    private bool TryApplyEffectList(
        BattleItemData itemData,
        Piece targetPiece)
    {
        if (targetPiece == null)
        {
            Debug.Log("아이템을 사용할 기물을 먼저 선택해야 합니다.");
            return false;
        }

        if (itemData.onlyPlayerPiece &&
            targetPiece.Team != PieceTeam.Player)
        {
            Debug.Log("플레이어 기물에만 아이템을 사용할 수 있습니다.");
            return false;
        }

        if (itemData.blockUseOnKing &&
            targetPiece.PieceType == PieceType.King)
        {
            Debug.Log("King 기물에는 이 아이템을 사용할 수 없습니다.");
            return false;
        }

        if (itemData.itemEffects == null ||
            itemData.itemEffects.Count == 0)
        {
            Debug.LogWarning(
                $"아이템 효과 목록이 비어 있습니다: {itemData.itemName}"
            );
            return false;
        }

        // 공통 ValidateItemEffect에서 전체 목록을 검사했다.
        // 변환 후 이동 가능 여부도 검사하므로 여기서 변환 전 조건으로 재검사하지 않는다.

        for (int i = 0; i < itemData.itemEffects.Count; i++)
        {
            BattleItemEffectEntry effect = itemData.itemEffects[i];
            bool applied;

            switch (effect.effectType)
            {
                case BattleItemEffectType.ChangePiece:
                    applied = UseChangePieceItem(
                        itemData,
                        targetPiece,
                        effect
                    );
                    break;

                case BattleItemEffectType.ExtraMove:
                    applied = true; // Validated before any mutation; committed after the effect list.
                    break;
                case BattleItemEffectType.RemoveStatusEffect:
                    applied = targetPiece.RemoveStatusEffect(effect.statusEffectData.effectType);
                    break;
                case BattleItemEffectType.ApplyStatusEffect:
                    applied = UseStatusEffectItem(
                        itemData,
                        targetPiece,
                        effect
                    );
                    break;

                default:
                    return false;
            }

            if (!applied)
            {
                Debug.LogError(
                    $"아이템 효과 실행 실패: {itemData.itemName} / {i + 1}"
                );
                return false;
            }
        }

        foreach (var effect in itemData.itemEffects)
            if (effect.effectType == BattleItemEffectType.ExtraMove)
                battleManager.CommitValidatedItemMove(targetPiece);
        return true;
    }

    // BattleItemData에 저장된 값 기준으로
    // 선택한 기물 정보를 변경하는 아이템 효과
    private bool UseChangePieceItem(
      BattleItemData itemData,
      Piece targetPiece,
      BattleItemEffectEntry effect = null) // <변경부분>
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

        // <변경부분>
        // 목록형이면 해당 Effect Entry의 값을 사용하고,
        // 기존 아이템이면 원래 BattleItemData 값을 사용한다.
        PieceData pieceData = effect != null
            ? effect.changeTargetPieceData
            : itemData.changeTargetPieceData;

        PieceType pieceType = effect != null
            ? effect.changeTargetPieceType
            : itemData.changeTargetPieceType;

        UniqueSkillType ability = effect != null
            ? effect.changeTargetUniqueSkill
            : itemData.changeTargetUniqueSkill;

        GeneralSkillType trait = effect != null
            ? effect.changeTargetGeneralSkill
            : itemData.changeTargetGeneralSkill;

        bool absorbedVisual = effect != null
            ? effect.useAbsorbedJelluVisual
            : itemData.useAbsorbedJelluVisual;

        if (pieceData != null)
        {
            targetPiece.ChangePieceData(
                pieceData,
                absorbedVisual
            );
        }
        else
        {
            // 기존 타입·Ability 방식도 지원한다.
            targetPiece.ChangePieceData(
                pieceType,
                ability,
                absorbedVisual
            );

            Debug.LogWarning(
                $"아이템 변환 대상 PieceData가 비어 있습니다: " +
                $"{itemData.itemName}"
            );
        }

        // <변경부분> 데이터에 설정된 일반스킬이 있으면
        // 레벨 없이 중복되지 않게 부여한다.
        //
        // 이미 같은 일반스킬을 가지고 있다면
        // AddGeneralSkill 내부에서 추가하지 않는다.
        // <변경부분>
        // 목록형 효과에서도 선택한 Trait을 정상 부여한다.
        if (trait != GeneralSkillType.None)
        {
            targetPiece.AddGeneralSkill(
                trait
            );
        }

        // 변경된 PieceData 기준으로 외형과 UI를 갱신한다.
        pieceManager.RefreshPieceVisual(
            targetPiece
        );


        // <변경부분>
        // 현재 아이템 변형에는 별도의 Born / Fade 연출이 없으므로
        // 외형 교체 직후 원본 색상에서 목표 회색으로 전환한다.
        //
        // 추후 아이템 전용 변형 연출이 추가되면
        // Start 호출 위치만 해당 연출 종료 뒤로 이동하면 된다.
        PieceVisualController visualController =
            targetPiece.GetComponent<PieceVisualController>();

        if (visualController != null)
        {
            visualController
                .PreparePlayerColorAdjustmentTransition();

            visualController
                .StartPreparedPlayerColorAdjustmentTransition();
        }


        Debug.Log(
       $"아이템 효과 성공: {itemData.itemName} / " +
       $"변경 타입 {pieceType} / 고유스킬 {ability}"
   );

        return true;
    }

    // <변경부분> BattleItemData에 연결된 상태효과를
    // 현재 선택한 기물에 부여하는 아이템 효과
    private bool UseStatusEffectItem(
        BattleItemData itemData,
        Piece targetPiece,
        BattleItemEffectEntry effect = null) // <변경부분>
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

        // <변경부분>
        // 효과 목록의 StatusEffectData를 우선 사용한다.
        // 기존 단일 효과 아이템에서는 원래 필드를 사용한다.
        StatusEffectData statusEffectData = effect != null
            ? effect.statusEffectData
            : itemData.applyStatusEffectData;

        if (statusEffectData == null)
        {
            Debug.LogWarning(
                $"상태효과 아이템 데이터가 연결되지 않았습니다: " +
                $"{itemData.itemName}"
            );

            return false;
        }

        // <변경부분>
        // 목록의 각 Status Effect 항목마다 한 번씩 실행된다.
        targetPiece.AddStatusEffect(
            statusEffectData
        );

        Debug.Log(
            $"상태효과 아이템 사용 성공: " +
            $"{itemData.itemName} / " +
            $"대상 {targetPiece.Team} {targetPiece.PieceType} / " +
            $"상태효과 {statusEffectData.effectType}"
        );

        return true;
    }
}