using UnityEngine;
public partial class BattleManager
{
    private Piece itemMovePiece;
    public bool CanGrantItemMove(Piece piece)
    {
        if (!CanUseBattleItem() || isActionAnimating || piece == null || piece.Team != PieceTeam.Player ||
            !piece.CanMoveThisTurn || piece.HasStatusEffect(StatusEffectType.MoveDisabled) ||
            chanceAttackBonusPiece != null || itemMovePiece != null || battleMoveValidator == null || pieceManager == null) return false;
        foreach(var position in battleMoveValidator.GetSelectablePositions(piece))
            if(pieceManager.GetPieceAt(position.x,position.y)==null)return true;
        return false;
    }
    public bool TryGrantItemMove(Piece piece)
    {
        if(!CanGrantItemMove(piece))return false;
        ActivateQuickStepBonus(piece);
        itemMovePiece=piece;
        return true;
    }
}
