using System.Collections.Generic;
using UnityEngine;

public partial class PieceManager
{
    public bool TryChooseEvasionDestination(Piece attacker, Piece target, out Vector2Int destination)
    {
        destination = default;
        if (attacker == null || target == null || boardManager == null || pieces == null ||
            !attacker.IsEnemyOf(target) || !target.HasStatusEffect(StatusEffectType.Evasion) ||
            GetPieceAt(target.X, target.Y) != target) return false;
        var candidates = new List<Vector2Int>(8);
        for (int y = Mathf.Max(0, target.Y - 1); y <= Mathf.Min(boardManager.Height - 1, target.Y + 1); y++)
        for (int x = Mathf.Max(0, target.X - 1); x <= Mathf.Min(boardManager.Width - 1, target.X + 1); x++)
        {
            Tile tile = boardManager.GetTile(x, y);
            if (tile != null && tile.IsWalkable && !tile.HasObstacle && GetPieceAt(x, y) == null)
                candidates.Add(new Vector2Int(x, y));
        }
        if (candidates.Count == 0 || Random.Range(0f, 100f) >= 50f) return false;
        destination = candidates[Random.Range(0, candidates.Count)];
        return true;
    }
}