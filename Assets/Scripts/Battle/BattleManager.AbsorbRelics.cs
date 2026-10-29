using UnityEngine;

public partial class BattleManager
{
    private bool absorbOverchargePending;
    private bool absorbOverchargeActive;

    public bool CanUseInitialAcceleration(Piece piece)
    {
        return piece != null && piece.Team == PieceTeam.Player &&
            piece.PieceType == PieceType.Pawn &&
            !piece.HasMovedThisBattle &&
            HasRelic(BattleRelicType.InitialAcceleration);
    }

    private void QueueAbsorbOvercharge(Piece piece)
    {
        if (piece != null && piece.Team == PieceTeam.Player &&
            HasRelic(BattleRelicType.AbsorbOvercharge))
            absorbOverchargePending = true;
    }

    // Called before switching turns. Pending and active are independent so a new
    // absorption during the boosted turn can reserve the following player turn.
    private void AdvanceAbsorbOverchargeTurn()
    {
        if (currentTurn == BattleTurn.Player)
        {
            absorbOverchargeActive = false;
            currentPlayerAbilityEnergy = Mathf.Min(currentPlayerAbilityEnergy, MaxPlayerAbilityEnergy);
        }
        else
        {
            absorbOverchargeActive = absorbOverchargePending;
            absorbOverchargePending = false;
        }
    }

    private bool TryActivateResidualPropulsion(Piece piece)
    {
        if (piece == null || piece.Team != PieceTeam.Player || currentTurn != BattleTurn.Player ||
            battleMoveValidator == null || pieceManager == null ||
            !HasRelic(BattleRelicType.ResidualPropulsion)) return false;
        foreach (Vector2Int position in battleMoveValidator.GetSelectablePositions(piece))
        {
            if (pieceManager.GetPieceAt(position.x, position.y) != null) continue;
            ActivateQuickStepBonus(piece);
            return true;
        }
        return false;
    }
}