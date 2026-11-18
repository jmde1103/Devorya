using System.Collections.Generic;

public partial class Piece
{
    // Only the fields touched by ChangePieceData; traits/status effects stay on the same Piece.
    public sealed class RecompositionState
    {
        internal PieceData data;
        internal PieceType type;
        internal UniqueSkillType ability;
        internal int cooldown;
        internal bool used, absorbed;
        internal PieceSpeciesTag[] species;
    }
    public RecompositionState CaptureRecompositionState() => new RecompositionState
    {
        data = CurrentPieceData, type = PieceType, ability = UniqueSkill,
        cooldown = uniqueSkillCooldown, used = hasUsedUniqueSkillThisTurn,
        absorbed = IsAbsorbedJelluVisual, species = GetSpeciesTagsCopy()
    };
    public void RestoreRecompositionState(RecompositionState state)
    {
        SetCurrentPieceData(state.data); PieceType = state.type; UniqueSkill = state.ability;
        uniqueSkillCooldown = state.cooldown; hasUsedUniqueSkillThisTurn = state.used;
        IsAbsorbedJelluVisual = state.absorbed; SetSpeciesTags(state.species);
    }
}
