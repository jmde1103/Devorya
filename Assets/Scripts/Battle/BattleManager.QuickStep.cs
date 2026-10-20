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
}