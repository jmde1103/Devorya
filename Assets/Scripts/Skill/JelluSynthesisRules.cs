using System.Collections.Generic;
using UnityEngine;

public static class JelluSynthesisRules
{
    public static bool IsMatchingMaterial(Piece caster, Piece material)
    {
        return caster != null && material != null && caster != material &&
            caster.Team != PieceTeam.Neutral && material.Team == caster.Team &&
            material.PieceType != PieceType.King && material.PieceType == caster.PieceType &&
            Mathf.Abs(caster.X - material.X) <= 1 && Mathf.Abs(caster.Y - material.Y) <= 1;
    }

    public static bool IsReinforcementMaterial(Piece caster, Piece material, PieceManager manager)
    {
        return caster != null && material != null && manager != null && caster != material &&
            caster.Team != PieceTeam.Neutral && caster.PieceType == PieceType.Rook &&
            caster.HasSpeciesTag(PieceSpeciesTag.Jellu) && material.Team == caster.Team &&
            material.PieceType != PieceType.King &&
            Mathf.Abs(caster.X - material.X) <= 1 && Mathf.Abs(caster.Y - material.Y) <= 1 &&
            manager.GetPieceAt(caster.X, caster.Y) == caster &&
            manager.GetPieceAt(material.X, material.Y) == material;
    }

    public static void FillReinforcementMaterials(Piece caster, BoardManager board, PieceManager manager, List<Piece> results)
    {
        results.Clear();
        if (caster == null || board == null || manager == null) return;
        for (int y = Mathf.Max(0, caster.Y - 1); y <= Mathf.Min(board.Height - 1, caster.Y + 1); y++)
        for (int x = Mathf.Max(0, caster.X - 1); x <= Mathf.Min(board.Width - 1, caster.X + 1); x++)
        {
            Piece candidate = manager.GetPieceAt(x, y);
            if (IsReinforcementMaterial(caster, candidate, manager)) results.Add(candidate);
        }
    }
}
