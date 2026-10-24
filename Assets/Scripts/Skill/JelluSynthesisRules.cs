using System.Collections.Generic;
using UnityEngine;

public static class JelluSynthesisRules
{
    public static bool IsMatchingMaterial(Piece caster, Piece material)
    {
        // 합성 사용자와 재료가 반드시 존재해야 하며,
        // 자기 자신을 합성 재료로 사용할 수 없다.
        if (caster == null ||
            material == null ||
            caster == material)
        {
            return false;
        }

        // 합성 능력을 가진 Pawn만 합성을 사용할 수 있다.
        // 재료는 같은 진영의 Pawn이어야 한다.
        // Neutral 및 다른 진영의 기물은 재료로 사용할 수 없다.
        if (caster.Team == PieceTeam.Neutral ||
            material.Team != caster.Team ||
            caster.UniqueSkill != UniqueSkillType.JelluSynthesis ||
            caster.PieceType != PieceType.Pawn ||
            material.PieceType != PieceType.Pawn)
        {
            return false;
        }

        // 현재 실제 기물의 PieceData를 기준으로 비교한다.
        // 종족 태그는 흡수 과정에서 추가될 수 있으므로
        // 합성 재료의 정체성 판정에는 사용하지 않는다.
        PieceData casterData =
            caster.CurrentPieceData;

        PieceData materialData =
            material.CurrentPieceData;

        // PieceData가 없거나 현재 Pawn 데이터와
        // 실제 기물 타입이 일치하지 않으면 합성을 허용하지 않는다.
        if (casterData == null ||
            materialData == null ||
            casterData.pieceType != PieceType.Pawn ||
            materialData.pieceType != PieceType.Pawn)
        {
            return false;
        }

        // pieceId는 기물 종류를 구분하는 내부 고유 ID다.
        // ID가 없는 기물은 다른 종족과 잘못 합성되지 않도록 제외한다.
        if (string.IsNullOrWhiteSpace(casterData.pieceId) ||
            string.IsNullOrWhiteSpace(materialData.pieceId))
        {
            return false;
        }

        // 합성 사용자와 재료의 현재 기물 ID가 정확히 같아야 한다.
        //
        // Jellu Pawn    + Jellu Pawn    = 허용
        // Devorya Pawn  + Devorya Pawn  = 허용
        // Jellu Pawn    + Devorya Pawn  = 불가
        //
        // 새 종족도 별도의 하드코딩 없이 동일한 규칙을 사용한다.
        if (string.Equals(
                casterData.pieceId,
                materialData.pieceId,
                System.StringComparison.Ordinal) == false)
        {
            return false;
        }

        // 기존 합성 범위인 주변 8칸을 유지한다.
        return
            Mathf.Abs(caster.X - material.X) <= 1 &&
            Mathf.Abs(caster.Y - material.Y) <= 1;
    }

    public static bool IsReinforcementMaterial(Piece caster, Piece material, PieceManager manager)
    {
        return caster != null && material != null && manager != null && caster != material &&
            caster.Team != PieceTeam.Neutral && caster.UniqueSkill == UniqueSkillType.JelluReinforce &&
            material.Team == caster.Team &&
            material.PieceType == PieceType.Pawn &&
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
