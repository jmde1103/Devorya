using System.Collections;
using UnityEngine;

public partial class PieceManager
{
    public Piece FindTauntInterceptor(Piece attacker, Piece target)
    {
        if (attacker == null || target == null || boardManager == null || pieces == null ||
            target.Team == PieceTeam.Neutral || !attacker.IsEnemyOf(target) ||
            GetPieceAt(target.X, target.Y) != target) return null;
        // Stable board order resolves multiple guards. Only the original target is searched.
        for (int y = Mathf.Max(0, target.Y - 1); y <= Mathf.Min(boardManager.Height - 1, target.Y + 1); y++)
        for (int x = Mathf.Max(0, target.X - 1); x <= Mathf.Min(boardManager.Width - 1, target.X + 1); x++)
        {
            Piece guard = GetPieceAt(x, y);
            if (guard != null && guard != target && guard.Team == target.Team &&
                guard.HasStatusEffect(StatusEffectType.Taunt)) return guard;
        }
        return null;
    }

    public bool TrySwapForTaunt(Piece target, Piece guard)
    {
        if (target == null || guard == null || target == guard || boardManager == null || pieces == null ||
            target.Team == PieceTeam.Neutral || target.Team != guard.Team ||
            !guard.HasStatusEffect(StatusEffectType.Taunt) ||
            Mathf.Abs(target.X - guard.X) > 1 || Mathf.Abs(target.Y - guard.Y) > 1 ||
            GetPieceAt(target.X, target.Y) != target || GetPieceAt(guard.X, guard.Y) != guard) return false;
        int tx = target.X, ty = target.Y, gx = guard.X, gy = guard.Y;
        Tile targetTile = boardManager.GetTile(tx, ty), guardTile = boardManager.GetTile(gx, gy);
        if (targetTile == null || guardTile == null) return false;
        // Forced displacement does not consume movement or change turn/ability usage.
        pieces[tx, ty] = guard;
        pieces[gx, gy] = target;
        guard.SetPosition(tx, ty, targetTile);
        target.SetPosition(gx, gy, guardTile);
        guard.transform.position = GetPieceWorldPosition(targetTile);
        target.transform.position = GetPieceWorldPosition(guardTile);
        SetPieceSortingOrder(guard.gameObject, tx, ty);
        SetPieceSortingOrder(target.gameObject, gx, gy);
        return true;
    }

    public IEnumerator PlayTauntSwapAnimation(Piece target, Piece guard, Vector3 targetStart, Vector3 guardStart)
    {
        Vector3 targetEnd = target.transform.position, guardEnd = guard.transform.position;
        for (float elapsed = 0; elapsed < 0.18f; elapsed += Time.deltaTime)
        {
            if (target == null || guard == null) yield break;
            float t = Mathf.SmoothStep(0, 1, elapsed / 0.18f);
            target.transform.position = Vector3.Lerp(targetStart, targetEnd, t);
            guard.transform.position = Vector3.Lerp(guardStart, guardEnd, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.15f);
            yield return null;
        }
        if (target != null) target.transform.position = targetEnd;
        if (guard != null) guard.transform.position = guardEnd;
    }
}