using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class EventSceneRecompositionCandidateController
{
    private float selectionMoveDuration = .35f;
    public void SetSelectionMoveDuration(float seconds) { selectionMoveDuration = Mathf.Max(0f, seconds); }
    private Coroutine selectionTransition;
    public bool IsSelectionTransitioning { get; private set; }
    private readonly Dictionary<Candidate, Vector3> originalTilePositions = new Dictionary<Candidate, Vector3>();
    private readonly Dictionary<Candidate, Vector3> originalPiecePositions = new Dictionary<Candidate, Vector3>();
    private readonly Dictionary<Collider2D, bool> originalColliderStates = new Dictionary<Collider2D, bool>();
    private Vector3 singleCandidatePosition;

    private void CaptureCandidatePlacement(Vector3 origin, Vector3 gridX, Vector3 gridY)
    {
        var one = GetGridPositions(1)[0];
        singleCandidatePosition = origin + gridX * one.x + gridY * one.y;
        foreach (var c in candidates)
        {
            originalTilePositions[c] = c.Tile.transform.position;
            originalPiecePositions[c] = c.Piece.transform.position;
            foreach (var collider in c.Tile.GetComponentsInChildren<Collider2D>(true)) originalColliderStates[collider] = collider.enabled;
            foreach (var collider in c.Piece.GetComponentsInChildren<Collider2D>(true)) originalColliderStates[collider] = collider.enabled;
        }
    }
    private void StartSelectionTransition(Candidate candidate, bool focus)
    {
        HideCandidateTooltip();
        IsSelectionTransitioning = true;
        ResetPointer(); RefreshButton();
        selectionTransition = StartCoroutine(AnimateSelection(candidate, focus));
    }
    private void SetCandidateInput(Candidate c, bool enabled)
    {
        foreach (var collider in c.Tile.GetComponentsInChildren<Collider2D>(true))
            if (collider != null) collider.enabled = enabled && originalColliderStates.TryGetValue(collider, out var state) && state;
        foreach (var collider in c.Piece.GetComponentsInChildren<Collider2D>(true))
            if (collider != null) collider.enabled = enabled && originalColliderStates.TryGetValue(collider, out var state) && state;
    }
    private IEnumerator AnimateSelection(Candidate selected, bool focus)
    {
        if (candidates.Count > 1)
        {
            var animation = ResolveAnimation();
            float fadeDuration = Mathf.Max(.01f, animation.duration);
            float moveDuration = Mathf.Max(0f, selectionMoveDuration);
            float duration = Mathf.Max(fadeDuration, moveDuration);
            var tileFrom = selected.Tile.transform.position;
            var pieceFrom = selected.Piece.transform.position;
            var tileTo = focus ? singleCandidatePosition : originalTilePositions[selected];
            var pieceTo = originalPiecePositions[selected] + (tileTo - originalTilePositions[selected]);
            foreach (var c in candidates)
            {
                if (c == selected) continue;
                SetCandidateInput(c, false);
                if (!focus) { c.Tile.gameObject.SetActive(true); c.Piece.gameObject.SetActive(true); }
            }
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float move = moveDuration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
                selected.Tile.transform.position = Vector3.Lerp(tileFrom, tileTo, move);
                selected.Piece.transform.position = Vector3.Lerp(pieceFrom, pieceTo, move);
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float fade = t >= 1f ? 1f : PopupOpenAnimator.EvaluateFlickerAlpha(animation, t);
                for (int i = 0; i < candidates.Count; i++)
                    if (candidates[i] != selected) { visuals[2*i].SetAlpha(focus ? 1f-fade : fade); visuals[2*i+1].SetAlpha(focus ? 1f-fade : fade); }
                yield return null; elapsed += Time.unscaledDeltaTime;
            }
            selected.Tile.transform.position = tileTo; selected.Piece.transform.position = pieceTo;
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i]; if (c == selected) continue;
                visuals[2*i].SetAlpha(1f); visuals[2*i+1].SetAlpha(1f);
                c.Tile.gameObject.SetActive(!focus); c.Piece.gameObject.SetActive(!focus);
                SetCandidateInput(c, !focus);
            }
        }
        IsSelectionTransitioning = false; selectionTransition = null;
        RefreshCandidateIcons(); RefreshButton();
        if (focus && selectedCandidate == selected) ShowCandidateTooltip(selected, Vector2.zero);
    }
    private void StopSelectionTransition()
    {
        if (selectionTransition != null) StopCoroutine(selectionTransition);
        selectionTransition = null; IsSelectionTransitioning = false;
        originalTilePositions.Clear(); originalPiecePositions.Clear(); originalColliderStates.Clear();
    }
}
