using UnityEngine;

public partial class BattleManager
{
    private Piece quickStepBonusPiece;

    private bool CanActivateQuickStep(Piece piece, OwnedGeneralSkillData beforeAction)
    {
        if (piece == null || beforeAction == null || piece.IsQuickStepMove || battleSkillManager == null || battleMoveValidator == null) return false;
        foreach (Vector2Int position in battleMoveValidator.GetSelectablePositions(piece))
            if (pieceManager.GetPieceAt(position.x, position.y) == null)
                return battleSkillManager.TryActivateQuickStep(piece, beforeAction);
        return false;
    }

    private void ActivateQuickStepBonus(Piece piece)
    {
        ClearQuickStepBonus();
        quickStepBonusPiece = piece;
        piece.SetQuickStepMove(true);
        // Reuse existing player/AI extra-action selection and turn ownership.
        ActivateChanceAttackBonus(piece);
    }

    private void ClearQuickStepBonus()
    {
        if (quickStepBonusPiece != null)
        {
            quickStepBonusPiece.SetQuickStepMove(false);
            if (chanceAttackBonusPiece == quickStepBonusPiece) chanceAttackBonusPiece = null;
        }
        quickStepBonusPiece = null;
    }

    // All extra-action producers share these ownership fields.
    public bool HasPendingPlayerBonusAction => !isBattleEnded && !isPlayerDeploymentPhase &&
        currentTurn == BattleTurn.Player &&
        (chanceAttackBonusPiece != null || quickStepBonusPiece != null || itemMovePiece != null);

    public bool CanCancelBonusAction => HasPendingPlayerBonusAction && !IsItemTargeting &&
        !executingItem && !isResolvingTurnEnd && GetItemInputFailure() == BattleItemFailure.None;

    public bool TryCancelBonusAction()
    {
        if (!CanCancelBonusAction) return false;
        // Item-granted movement is in addition to the player's normal action.
        bool retainNormalAction = itemMovePiece != null;
        itemMovePiece = null;
        ClearQuickStepBonus();
        chanceAttackBonusPiece = null;
        chanceAttackContinuousCount = 0;
        pendingActionTile = null;
        pendingAttackTargetPiece = null;
        isAbsorbMode = false;
        SelectPiece(null);
        if (battleUIController != null)
        {
            battleUIController.SetAbsorbModeIcon(false);
            battleUIController.RefreshSharedCancelButton();
        }
        if (!retainNormalAction &&
            (eventSequenceController == null || !eventSequenceController.ShouldHoldBattleTurnForSequence))
            EndTurn();
        return true;
    }
}