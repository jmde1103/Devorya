using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed partial class EventSceneRecompositionCandidateController : MonoBehaviour
{
    public sealed class Candidate
    {
        public int Index { get; }
        public Vector2Int GridPosition { get; }
        public PieceData Data { get; }
        public Tile Tile { get; }
        public Piece Piece { get; }
        internal Candidate(int index, Vector2Int grid, PieceData data, Tile tile, Piece piece)
        { Index = index; GridPosition = grid; Data = data; Tile = tile; Piece = piece; }
    }

    [Header("후보 표시")]
    [SerializeField, InspectorName("후보 루트 (비어 있으면 독립 루트 자동 생성)")]
    private Transform candidateRoot;
    [SerializeField, InspectorName("홀수 후보 타일")]
    private TileType firstTileType = TileType.Water;
    [SerializeField, InspectorName("짝수 후보 타일")]
    private TileType secondTileType = TileType.Forest;
    [SerializeField, InspectorName("후보 기물 위치 미세 조정")]
    [Tooltip("기존 배치에 더할 월드 좌표 X/Y 값입니다. 0이면 기존 배치와 동일하며 후보 재생성 시 적용됩니다.")]
    private Vector2 candidatePieceVisualOffset = Vector2.zero;
    [Header("기존 글리치 등장 재사용")]
    [SerializeField, InspectorName("팝업 등장 데이터 (비어 있으면 씬에서 찾기)")]
    private PopupOpenAnimationData appearanceAnimation;
    [SerializeField, Min(0f), InspectorName("후보별 등장 간격")]
    private float staggerSeconds = 0.04f;

    private readonly List<Candidate> candidates = new List<Candidate>();
    private readonly List<EventSceneFormationIntroAnimator.FadeVisual> visuals =
        new List<EventSceneFormationIntroAnimator.FadeVisual>();
    private Transform generationRoot;
    private Transform ownedRoot;
    private Piece source;
    private bool showTypeIcons = true;
    private Coroutine appearance;
    private PopupOpenAnimationData fallbackAnimation;
    public IReadOnlyList<Candidate> Candidates => candidates.AsReadOnly();
    public int TotalCandidateCount { get; private set; }
    public int HiddenCandidateCount => Mathf.Max(0, TotalCandidateCount - candidates.Count);
    public bool IsAppearing { get; private set; }

    // Coordinates were fitted to the supplied images using the existing isometric grid.
    // 4/5/7/8 are proposed extensions, pending user visual confirmation.
    public static Vector2Int[] GetGridPositions(int count)
    {
        switch (count)
        {
            case 1: return Cells(2,4);
            case 2: return Cells(1,4, 3,4);
            case 3: return Cells(0,4, 2,4, 4,4);
            case 4: return Cells(1,5, 3,5, 1,3, 3,3);
            case 5: return Cells(0,5, 2,5, 4,5, 1,3, 3,3);
            case 6: return Cells(0,5, 2,5, 4,5, 0,3, 2,3, 4,3);
            case 7: return Cells(2,7, 0,5, 2,5, 4,5, 0,3, 2,3, 4,3);
            case 8: return Cells(1,7, 3,7, 0,5, 2,5, 4,5, 0,3, 2,3, 4,3);
            case 9: return Cells(0,7, 2,7, 4,7, 0,5, 2,5, 4,5, 0,3, 2,3, 4,3);
            default: return new Vector2Int[0];
        }
    }
    private static Vector2Int[] Cells(params int[] xy)
    {
        var cells = new Vector2Int[xy.Length / 2];
        for (int i = 0; i < cells.Length; i++) cells[i] = new Vector2Int(xy[i*2], xy[i*2+1]);
        return cells;
    }
    public static bool TryGetGridFrame(BoardManager board, out Vector3 origin, out Vector3 x, out Vector3 y)
    {
        origin = x = y = Vector3.zero;
        if (board == null || board.Width != 5 || board.Height != 2) return false;
        var p00 = board.GetTile(0,0); var p10 = board.GetTile(1,0); var p01 = board.GetTile(0,1);
        if (p00 == null || p10 == null || p01 == null) return false;
        origin = p00.transform.position;
        x = p10.transform.position - origin; y = p01.transform.position - origin;
        if (Vector3.Cross(x,y).sqrMagnitude < 0.000001f) return false;
        for (int row=0; row<2; row++) for (int col=0; col<5; col++)
        {
            var tile = board.GetTile(col,row);
            if (tile == null || (tile.transform.position - (origin + col*x + row*y)).sqrMagnitude > 0.000001f)
                return false;
        }
        return true;
    }
    public void SetTypeIconsVisible(bool visible)
    {
        showTypeIcons = visible;
        RefreshCandidateIcons();
    }
    public void Show(Piece selected, BoardManager board, PieceManager manager)
    {
        ClearCandidates();
        if (!isActiveAndEnabled || selected == null || selected.Team != PieceTeam.Player ||
            manager == null || RunStateManager.Instance == null) return;
        if (!TryGetGridFrame(board, out var origin, out var gridX, out var gridY)) return;
        if (selected.CurrentTile != board.GetTile(selected.X, selected.Y) ||
            manager.GetPieceAt(selected.X,selected.Y) != selected) return;
        var dataList = new List<PieceData>();
        var seen = new HashSet<PieceData>();
        // Reserve a visible slot for returning to the actual default; never modify the catalog.
        var defaultData = manager.GetRecompositionDefaultData(selected.PieceType);
        if (defaultData != null && defaultData != selected.CurrentPieceData && seen.Add(defaultData))
            dataList.Add(defaultData);
        foreach (var data in RunStateManager.Instance.GetAbsorbedPieces(selected.PieceType))
            if (data != null && data != selected.CurrentPieceData && seen.Add(data)) dataList.Add(data);
        TotalCandidateCount = dataList.Count;
        if (dataList.Count == 0) return;
        source = selected;
        // Never parent a candidate under the managed formation hierarchy.
        Transform formationHierarchy = board.transform;
        while (formationHierarchy != null && !manager.transform.IsChildOf(formationHierarchy))
            formationHierarchy = formationHierarchy.parent;
        if (candidateRoot == null || (formationHierarchy != null &&
            (candidateRoot == formationHierarchy || candidateRoot.IsChildOf(formationHierarchy))))
        {
            if (ownedRoot == null)
            {
                ownedRoot = new GameObject("RecompositionCandidateRoot").transform;
                if (formationHierarchy != null) ownedRoot.SetParent(formationHierarchy.parent, true);
            }
            candidateRoot = ownedRoot;
        }
        generationRoot = new GameObject("CandidateGeneration").transform;
        generationRoot.SetParent(candidateRoot, true);
        var positions = GetGridPositions(Mathf.Min(9, dataList.Count));
        for (int i=0; i<positions.Length; i++)
        {
            var grid = positions[i];
            // Keep the water tile in the middle of a three-card row (matches the references).
            TileType type = positions.Length == 1 || (positions.Length == 2 ? i == 1 : grid.x == 2)
                ? firstTileType : secondTileType;
            var tile = board.CreateDetachedDisplayTile(grid, origin + gridX*grid.x + gridY*grid.y, generationRoot, type);
            if (tile == null) { ClearCandidates(); Debug.LogWarning("재구성 후보 타일 생성 실패: 기존 타일 프리팹/데이터를 확인하세요.", this); return; }
            var piece = manager.CreateDetachedRecompositionPreview(dataList[i], tile, generationRoot);
            if (piece == null) { ClearCandidates(); Debug.LogWarning("재구성 후보 기물 생성 실패: 기존 기물 프리팹을 확인하세요.", this); return; }
            // Keep per-data visual prefab transforms and virtual tile coordinates intact.
            piece.transform.position += (Vector3)candidatePieceVisualOffset;
            piece.SetTypeIconVisible(showTypeIcons);
            candidates.Add(new Candidate(i, grid, dataList[i], tile, piece));
            foreach (var collider in tile.GetComponentsInChildren<Collider2D>(true)) collider.enabled = true;
            var tileVisual = new EventSceneFormationIntroAnimator.FadeVisual(tile.transform);
            var pieceVisual = new EventSceneFormationIntroAnimator.FadeVisual(piece.transform);
            visuals.Add(tileVisual); visuals.Add(pieceVisual);
            tileVisual.SetAlpha(0); pieceVisual.SetAlpha(0);
        }
        CaptureCandidatePlacement(origin, gridX, gridY);
        IsAppearing = true;
        AcquireButton();
        RefreshButton();
        appearance = StartCoroutine(Appear());
    }
    private PopupOpenAnimationData ResolveAnimation()
    {
        if (appearanceAnimation != null) return appearanceAnimation;
        foreach (var popup in FindObjectsByType<PopupOpenAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (popup.AnimationData != null) { appearanceAnimation = popup.AnimationData; return appearanceAnimation; }
        if (fallbackAnimation == null)
        {
            fallbackAnimation = ScriptableObject.CreateInstance<PopupOpenAnimationData>();
            fallbackAnimation.hideFlags = HideFlags.HideAndDontSave;
        }
        return fallbackAnimation;
    }
    private IEnumerator Appear()
    {
        var data = ResolveAnimation();
        float duration = Mathf.Max(0.01f, data.duration);
        float total = duration + Mathf.Max(0, candidates.Count-1)*staggerSeconds;
        float elapsed = 0;
        while (elapsed < total)
        {
            for (int i=0; i<candidates.Count; i++)
            {
                float t = (elapsed-i*staggerSeconds)/duration;
                float alpha = t < 0 ? 0 : t >= 1 ? 1 : PopupOpenAnimator.EvaluateFlickerAlpha(data,t);
                visuals[2*i].SetAlpha(alpha); visuals[2*i+1].SetAlpha(alpha);
            }
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        foreach (var visual in visuals) visual.Restore();
        IsAppearing = false; appearance = null;
        RefreshCandidateIcons();
        RefreshButton();
    }
    public void ClearCandidates()
    {
        StopSelectionTransition();
        HideCandidateTooltip();
        CancelRecomposition();
        ClearSelection();
        ResetPointer();
        processing = false;
        StopSelectionAnimations();
        ReleaseButton();
        if (appearance != null) StopCoroutine(appearance);
        appearance = null; IsAppearing = false; source = null;
        // Deactivate immediately: Destroy is deferred until end of frame.
        if (generationRoot != null) { generationRoot.gameObject.SetActive(false); Destroy(generationRoot.gameObject); }
        generationRoot = null; candidates.Clear(); visuals.Clear(); TotalCandidateCount = 0;
    }
    private void LateUpdate()
    {
        if (!CanSelect) HideCandidateTooltip();
        else RefreshCandidateTooltipLocale();
        if (generationRoot != null && (source == null || !source.gameObject.activeInHierarchy)) ClearCandidates();
    }
    private void OnDisable() { ClearCandidates(); }
    private void OnDestroy()
    {
        ClearCandidates();
        if (ownedRoot != null) Destroy(ownedRoot.gameObject);
        if (fallbackAnimation != null) Destroy(fallbackAnimation);
    }
}
