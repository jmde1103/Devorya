using UnityEngine;
public partial class BattleManager
{
    private Piece itemMovePiece;
    public bool CanGrantItemMove(Piece piece, PieceType? projectedType = null)
    {
        if (!CanUseBattleItem() || isActionAnimating || piece == null || piece.Team != PieceTeam.Player ||
            !piece.CanMoveThisTurn || piece.HasStatusEffect(StatusEffectType.MoveDisabled) ||
            chanceAttackBonusPiece != null || itemMovePiece != null || battleMoveValidator == null || pieceManager == null) return false;
        foreach(var position in battleMoveValidator.GetSelectablePositions(piece, projectedType))
            if(pieceManager.GetPieceAt(position.x,position.y)==null)return true;
        return false;
    }
    internal void CommitValidatedItemMove(Piece piece)
    {
        ActivateQuickStepBonus(piece);
        itemMovePiece = piece;
    }
    public bool TryGrantItemMove(Piece piece)
    {
        if(!CanGrantItemMove(piece))return false;
        CommitValidatedItemMove(piece);
        return true;
    }
}
