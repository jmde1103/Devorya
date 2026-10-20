using UnityEngine;

public partial class PieceDatabase
{
    public PieceData GetSynthesisPromotionData(Piece source, PieceType type)
    {
        if (source == null || pieceDataList == null) return null;
        // Base data defines the species; runtime tags may also include an absorbed Jellu tag.
        PieceSpeciesTag[] species = source.CurrentPieceData != null
            ? source.CurrentPieceData.speciesTags : source.GetSpeciesTagsCopy();
        if (species == null || species.Length == 0) return null;
        foreach (PieceData candidate in pieceDataList)
        {
            if (candidate == null || candidate.pieceType != type || candidate.speciesTags == null) continue;
            bool matches = true;
            foreach (PieceSpeciesTag tag in species)
                if (tag != PieceSpeciesTag.None && System.Array.IndexOf(candidate.speciesTags, tag) < 0) matches = false;
            foreach (PieceSpeciesTag tag in candidate.speciesTags)
                if (tag != PieceSpeciesTag.None && System.Array.IndexOf(species, tag) < 0) matches = false;
            if (matches) return candidate;
        }
        return null;
    }
}