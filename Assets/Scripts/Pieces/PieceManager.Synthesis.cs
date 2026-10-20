using System.Collections.Generic;
using UnityEngine;

public partial class PieceManager
{
    static readonly PieceType[] SynthesisPromotionTypes = { PieceType.Knight, PieceType.Bishop, PieceType.Rook };

    public bool HasSynthesisPromotion(Piece source)
    {
        if (source == null || source.PieceType == PieceType.King || pieceDatabase == null) return false;
        foreach (PieceType type in SynthesisPromotionTypes)
            if (pieceDatabase.GetSynthesisPromotionData(source, type) != null) return true;
        return false;
    }

    public PieceData ChooseSynthesisPromotion(Piece source)
    {
        if (!HasSynthesisPromotion(source)) return null;
        var candidates = new List<PieceData>(3);
        foreach (PieceType type in SynthesisPromotionTypes)
        {
            PieceData data = pieceDatabase.GetSynthesisPromotionData(source, type);
            if (data != null) candidates.Add(data);
        }
        return candidates[Random.Range(0, candidates.Count)];
    }

    public bool PromoteBySynthesis(Piece source, PieceData promotion)
    {
        if (source == null || promotion == null || pieceDatabase == null ||
            pieceDatabase.GetSynthesisPromotionData(source, promotion.pieceType) != promotion) return false;
        PieceSpeciesTag[] species = source.GetSpeciesTagsCopy();
        bool absorbed = source.IsAbsorbedJelluVisual;
        source.ChangePieceData(promotion, absorbed);
        source.SetSpeciesTags(species);
        RefreshPieceVisual(source);
        PieceVisualController visual = source.GetComponent<PieceVisualController>();
        if (visual != null) visual.PreparePlayerColorAdjustmentTransition();
        return true;
    }
}