using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class EventSceneRecompositionCandidateController
{
    [SerializeField, InspectorName("후보 선택 타일 색상")]
    private Color selectionColor = new Color(1f, 0.12f, 0.12f, 1f);
    private EventSceneFormationBoardController formation;
    private ShopController shop;
    private Candidate selectedCandidate, pressedCandidate;
    private bool ownsPointer, pointerDragged, processing;
    private int pointerId = -1;
    private Vector2 pointerStart;
    private readonly Dictionary<Piece, Coroutine> selectionAnimations = new Dictionary<Piece, Coroutine>();
    public Candidate SelectedCandidate => selectedCandidate;
    public Piece SourcePiece => source;
    public bool CanSelect => isActiveAndEnabled && !IsAppearing && !IsSelectionTransitioning && !processing && formation != null &&
        formation.IsFormationOpen && !formation.IsFormationIntroPlaying && source != null &&
        formation.SelectedFormationPiece == source && source.gameObject.activeInHierarchy &&
        formation.BoardManager != null && formation.BoardManager.GetTile(source.X, source.Y) == source.CurrentTile;
    public bool CanConfirm => CanSelect && selectedCandidate != null && candidates.Contains(selectedCandidate) &&
        selectedCandidate.Piece != null && selectedCandidate.Tile != null && selectedCandidate.Data != null &&
        selectedCandidate.Data.pieceType == source.PieceType && source.CurrentPieceData != selectedCandidate.Data;

    public void Configure(EventSceneFormationBoardController owner, ShopController sharedShop)
    {
        formation = owner;
        shop = sharedShop != null ? sharedShop : FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
    }
    private void AcquireButton() { if (shop != null) shop.AcquireRecompositionButton(this); }
    private void ReleaseButton() { if (shop != null) shop.ReleaseRecompositionButton(this); }
    private void RefreshButton() { if (shop != null) shop.SetRecompositionButtonState(this, candidates.Count > 0, CanConfirm); }

    // Validated session entry point; execution owns the rollback snapshot.
    public bool TryConfirmSelection()
    {
        return CanConfirm && BeginRecomposition();
    }
    public void SetProcessing(bool value) { if (value) HideCandidateTooltip(); processing = value; ResetPointer(); RefreshButton(); }
    public bool SelectCandidateTile(Tile tile)
    {
        if (!CanSelect || tile == null) return false;
        var next = candidates.Find(c => c.Tile == tile);
        if (next == null || !next.Tile.gameObject.activeInHierarchy || (selectedCandidate != null && next != selectedCandidate)) return false;
        bool toggleOff = next == selectedCandidate;
        ClearSelection();
        if (!toggleOff)
        {
            selectedCandidate = next;
            next.Tile.ShowItemConfirmHighlight(selectionColor, 1f);
            PlaySelection(next.Piece, true);
        }
        RefreshCandidateIcons(); RefreshButton();
        StartSelectionTransition(next, !toggleOff);
        return true;
    }
    private void ClearSelection()
    {
        if (selectedCandidate != null)
        {
            if (selectedCandidate.Tile != null) selectedCandidate.Tile.HideHighlight();
            if (selectedCandidate.Piece != null) PlaySelection(selectedCandidate.Piece, false);
        }
        selectedCandidate = null;
        RefreshCandidateIcons(); RefreshButton();
    }
    private void RefreshCandidateIcons()
    {
        foreach (var candidate in candidates)
        {
            if (candidate.Piece == null) continue;
            bool selected = candidate == selectedCandidate;
            candidate.Piece.SetTypeIconSelected(selected);
            candidate.Piece.SetTypeIconSortingPriority(selected ? 1 : 0);
            candidate.Piece.SetTypeIconDimmed(selectedCandidate != null && !selected);
            candidate.Piece.SetTypeIconVisible(showTypeIcons || selected);
        }
    }
    private void PlaySelection(Piece piece, bool selected)
    {
        if (selectionAnimations.TryGetValue(piece, out var previous) && previous != null) StopCoroutine(previous);
        var visual = piece.GetComponent<PieceVisualController>();
        var animator = visual != null ? visual.CurrentSpineAnimationController : piece.GetComponentInChildren<PieceSpineAnimationController>();
        if (animator == null) return;
        if (!isActiveAndEnabled) { animator.PlayIdle(); return; }
        // Same routines as PieceAnimationManager, owned here so rapid switches and teardown cancel old work.
        selectionAnimations[piece] = StartCoroutine(selected ? animator.PlaySelectRoutine() : animator.PlayDownToIdleRoutine());
    }
    private void StopSelectionAnimations()
    {
        foreach (var routine in selectionAnimations.Values) if (routine != null) StopCoroutine(routine);
        selectionAnimations.Clear();
    }
    public Candidate CandidateAtScreenPosition(Camera camera, Vector2 position)
    {
        if (camera == null) return null;
        var world = camera.ScreenToWorldPoint(new Vector3(position.x, position.y, Mathf.Abs(camera.transform.position.z)));
        foreach (var hit in Physics2D.OverlapPointAll(world))
        {
            var tile = hit.GetComponentInParent<Tile>();
            var candidate = candidates.Find(c => c.Tile == tile);
            if (candidate != null) return candidate;
        }
        return null;
    }
    public bool BeginPointer(Camera camera, Vector2 position, int id)
    {
        ResetPointer();
        if (Tile.IsScreenPositionOverUI(position)) return false;
        var hit = CandidateAtScreenPosition(camera, position);
        if (hit == null) return false;
        ownsPointer = true; pointerId = id; pointerStart = position;
        pressedCandidate = CanSelect ? hit : null;
        return true;
    }
    public void MovePointer(Vector2 position, float threshold)
    {
        if (Vector2.Distance(pointerStart, position) >= threshold) pointerDragged = true;
    }
    public bool EndPointer(Camera camera, Vector2 position, int id, float threshold)
    {
        bool consumed = ownsPointer;
        var candidate = pressedCandidate;
        bool valid = consumed && id == pointerId && !pointerDragged && CanSelect && candidate != null &&
            Vector2.Distance(pointerStart, position) < threshold && !Tile.IsScreenPositionOverUI(position) &&
            CandidateAtScreenPosition(camera, position) == candidate;
        ResetPointer();
        if (valid)
        {
            SelectCandidateTile(candidate.Tile);

        }
        return consumed;
    }
    private void ResetPointer() { ownsPointer = pointerDragged = false; pressedCandidate = null; pointerId = -1; }
    public bool HandleInput(Camera camera, float threshold)
    {
        if (Application.isMobilePlatform)
        {
            if (Input.touchCount != 1) { bool owned = ownsPointer; ResetPointer(); return owned; }
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) return BeginPointer(camera, touch.position, touch.fingerId);
            if (!ownsPointer) return false;
            if (touch.phase == TouchPhase.Canceled) { ResetPointer(); return true; }
            MovePointer(touch.position, threshold);
            if (touch.phase == TouchPhase.Ended) return EndPointer(camera, touch.position, touch.fingerId, threshold);
            return true;
        }
        if (Input.GetMouseButtonDown(0)) return BeginPointer(camera, Input.mousePosition, -1);
        if (!ownsPointer) return false;
        MovePointer(Input.mousePosition, threshold);
        if (Input.GetMouseButtonUp(0)) return EndPointer(camera, Input.mousePosition, -1, threshold);
        return true;
    }
}
