using UnityEngine;
using UnityEngine.UI;

public partial class PieceManager
{
    // Exact database ID lookup only: never fall back to another species of the same type.
    public PieceData GetRecompositionDefaultData(PieceType type)
    {
        if (pieceDatabase == null) return null;
        string id = GetDevoryaPieceId(type);
        if (string.IsNullOrEmpty(id)) id = "Devorya " + type;
        var data = pieceDatabase.GetData(id);
        return data != null && data.pieceType == type ? data : null;
    }

    // Display-only creation: never writes the manager grid or runtime source map.
    public Piece CreateDetachedRecompositionPreview(PieceData data, Tile tile, Transform parent)
    {
        if (data == null || tile == null || piecePrefab == null) return null;
        var obj = Instantiate(piecePrefab, GetPieceWorldPosition(tile), Quaternion.identity);
        obj.name = $"CandidatePiece_{data.pieceType}_{tile.X}_{tile.Y}";
        obj.transform.localScale = Vector3.Scale(piecePrefab.transform.localScale,
            pieceParent != null ? pieceParent.lossyScale : Vector3.one);
        obj.transform.SetParent(parent, true);
        var piece = obj.GetComponent<Piece>();
        if (piece == null) { Destroy(obj); return null; }
        // Enemy is only a visual preset here; this object is never a combat participant.
        piece.Initialize(data.pieceType, PieceTeam.Enemy, tile.X, tile.Y, tile, false,
            data.uniqueSkill, data.speciesTags);
        piece.SetCurrentPieceData(data);
        piece.SetAbsorbedJelluVisual(false);
        ApplyDefaultGeneralSkillsFromData(piece, data);
        RefreshPieceVisual(piece);
        piece.SetTypeIconVisible(true);
        piece.SetTypeIconSelected(false);
        piece.SetTypeIconDimmed(false);
        foreach (var collider in obj.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        foreach (var graphic in obj.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        foreach (var raycaster in obj.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
        PlayPieceIdleAnimation(piece);
        return piece;
    }
}
