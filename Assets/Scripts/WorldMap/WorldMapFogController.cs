using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 월드맵의 셀 단위 포그 표시,
// 탐사 완료 영역과 다음 탐사 후보 영역을 관리한다.
//
// 마커가 지나간 셀은 완전히 밝게 유지하고,
// 현재 탐사 완료 노드와 연결된 미탐사 노드·길은
// 옅은 포그 상태로 미리 표시한다.
public class WorldMapFogController : MonoBehaviour
{
    [Header("Map References")]
    // 현재 월드맵의 Grid와 Map Data를 제공하는 Builder
    [SerializeField]
    private WorldMapBuilder worldMapBuilder;

    // 실제 길 타일이 배치된 Tilemap
    //
    // 연결 경로 Preview를 만들 때
    // 실제 길 타일이 존재하는 셀만 밝히는 데 사용한다.
    [SerializeField]
    private Tilemap pathTilemap;

    // 전체 월드맵 포그를 표시하는 전용 Tilemap
    [SerializeField]
    private Tilemap fogTilemap;

    // 모든 포그 셀에 공통으로 사용할 Tile Asset
    [SerializeField]
    private TileBase fogTile;

    [Header("Fog Color")]
    // 포그의 기본 색상
    //
    // Alpha는 각 셀의 탐사 상태에 따라 코드에서 별도로 적용한다.
    [SerializeField]
    private Color fogColor =
        Color.black;

    // 아직 전혀 발견되지 않은 지역의 포그 투명도
    [SerializeField, Range(0f, 1f)]
    private float hiddenAlpha =
        0.92f;

    // 현재 탐사 노드와 연결된
    // 미탐사 노드·길의 중심 포그 투명도
    [SerializeField, Range(0f, 1f)]
    private float previewAlpha =
        0.55f;

    [Header("Fog Radius")]
    // 마커가 지나간 셀 바깥으로
    // 완전한 어둠까지 그라데이션할 셀 반경
    [SerializeField, Min(0)]
    private int exploredGradientRadius =
        3;

    // 다음 탐사 후보 길 주변에 적용할
    // 옅은 Preview 그라데이션 반경
    [SerializeField, Min(0)]
    private int previewGradientRadius =
        1;

    // 노드 중심 주변에서 함께 밝힐 셀 반경
    [SerializeField, Min(0)]
    private int nodeAreaRadius =
        1;

    [Header("Fog Animation")]
    // 포그 Alpha가 목표값까지 변하는 속도
    //
    // 셀마다 별도 코루틴을 생성하지 않고,
    // 하나의 코루틴이 변경 중인 모든 셀을 함께 처리한다.
    [SerializeField, Min(0.01f)]
    private float fogFadeSpeed =
        1.2f;

    [Header("Route Preview")]
    // Route 선상에 있더라도 실제 PathTilemap 타일이 없는 셀은
    // Preview 영역에서 제외할지 여부
    [SerializeField]
    private bool revealOnlyExistingPathTiles =
        true;

    // 현재 각 셀에 실제 적용된 Alpha
    private readonly Dictionary<Vector3Int, float>
        currentAlphaByCell =
            new Dictionary<Vector3Int, float>();

    // 각 셀이 최종적으로 도달해야 할 목표 Alpha
    private readonly Dictionary<Vector3Int, float>
        targetAlphaByCell =
            new Dictionary<Vector3Int, float>();

    // 현재 Alpha 애니메이션이 진행 중인 셀 목록
    private readonly HashSet<Vector3Int>
        animatingCells =
            new HashSet<Vector3Int>();

    // 반복문 도중 완료된 셀을 안전하게 제거하기 위한 임시 목록
    private readonly List<Vector3Int>
        completedAnimationCells =
            new List<Vector3Int>();

    // 런타임 포그 상태 복원에 사용할 임시 목록
    private readonly List<Vector2Int>
        restoredFogCells =
            new List<Vector2Int>();

    // 모든 셀의 Alpha를 함께 처리하는 단일 코루틴
    private Coroutine fogAnimationCoroutine;

    // 마지막으로 마커가 지나간 Grid 셀
    private Vector3Int lastMarkerCell;

    // 마지막 마커 셀이 설정됐는지 확인한다.
    private bool hasLastMarkerCell;

    // 포그 초기화가 정상적으로 끝났는지 확인한다.
    private bool isInitialized;

    private void Awake()
    {
        // 노드와 마커가 본격적으로 초기화되기 전에
        // 전체 포그 Tilemap을 먼저 생성한다.
        InitializeFog();
    }

    // 현재 맵에서 사용하는 Grid를 반환한다.
    private Grid GetMapGrid()
    {
        if (worldMapBuilder == null)
        {
            return null;
        }

        return worldMapBuilder.MapGrid;
    }

    // 현재 맵의 고유 ID를 반환한다.
    private string GetCurrentMapId()
    {
        if (worldMapBuilder == null ||
            worldMapBuilder.WorldMapData == null)
        {
            return null;
        }

        return
            worldMapBuilder.WorldMapData.mapId;
    }

    // 포그 생성에 필요한 참조를 검사한다.
    private bool ValidateReferences()
    {
        if (worldMapBuilder == null)
        {
            Debug.LogWarning(
                "월드맵 포그 초기화 실패: " +
                "World Map Builder가 연결되지 않았습니다."
            );

            return false;
        }

        if (worldMapBuilder.WorldMapData == null)
        {
            Debug.LogWarning(
                "월드맵 포그 초기화 실패: " +
                "World Map Data가 연결되지 않았습니다."
            );

            return false;
        }

        if (worldMapBuilder.MapGrid == null)
        {
            Debug.LogWarning(
                "월드맵 포그 초기화 실패: " +
                "World Map Builder의 Map Grid가 연결되지 않았습니다."
            );

            return false;
        }

        if (fogTilemap == null)
        {
            Debug.LogWarning(
                "월드맵 포그 초기화 실패: " +
                "Fog Tilemap이 연결되지 않았습니다."
            );

            return false;
        }

        if (fogTile == null)
        {
            Debug.LogWarning(
                "월드맵 포그 초기화 실패: " +
                "Fog Tile이 연결되지 않았습니다."
            );

            return false;
        }

        if (pathTilemap == null)
        {
            Debug.LogWarning(
                "월드맵 포그 경로 Preview 경고: " +
                "Path Tilemap이 연결되지 않았습니다. " +
                "Route 선상의 모든 셀을 Preview로 처리합니다."
            );
        }

        return true;
    }

    // 전체 맵을 포그로 채우고
    // 기존 런타임 탐사 상태를 복원한다.
    [ContextMenu("Initialize Fog")]
    public void InitializeFog()
    {
        if (fogAnimationCoroutine != null)
        {
            StopCoroutine(
                fogAnimationCoroutine
            );

            fogAnimationCoroutine =
                null;
        }

        currentAlphaByCell.Clear();
        targetAlphaByCell.Clear();
        animatingCells.Clear();
        completedAnimationCells.Clear();

        hasLastMarkerCell =
            false;

        isInitialized =
            false;

        if (ValidateReferences() == false)
        {
            return;
        }

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        fogTilemap.ClearAllTiles();

        // WorldMapData의 전체 Grid 영역을
        // 완전히 미탐사 상태의 포그로 채운다.
        for (int y = 0;
             y < mapData.gridHeight;
             y++)
        {
            for (int x = 0;
                 x < mapData.gridWidth;
                 x++)
            {
                Vector3Int cellPosition =
                    new Vector3Int(
                        x,
                        y,
                        0
                    );

                fogTilemap.SetTile(
                    cellPosition,
                    fogTile
                );

                // 각 타일의 색상과 Alpha를
                // 셀별로 변경할 수 있도록 Color 잠금을 해제한다.
                fogTilemap.SetTileFlags(
                    cellPosition,
                    TileFlags.None
                );

                currentAlphaByCell.Add(
                    cellPosition,
                    hiddenAlpha
                );

                targetAlphaByCell.Add(
                    cellPosition,
                    hiddenAlpha
                );

                ApplyCellColor(
                    cellPosition,
                    hiddenAlpha
                );
            }
        }

        isInitialized =
            true;

        // 전투 씬 이동 후 월드맵으로 돌아왔을 때도
        // 이전 탐사 영역을 다시 적용한다.
        RestoreRuntimeFogState();
    }

    // WorldMapRuntimeState에 저장된
    // Preview와 Explored 셀을 현재 Tilemap에 복원한다.
    private void RestoreRuntimeFogState()
    {
        string mapId =
            GetCurrentMapId();

        if (string.IsNullOrWhiteSpace(
                mapId))
        {
            return;
        }

        // Preview 영역을 먼저 복원한다.
        WorldMapRuntimeState.CopyPreviewFogCells(
            mapId,
            restoredFogCells
        );

        for (int i = 0;
             i < restoredFogCells.Count;
             i++)
        {
            Vector2Int savedCell =
                restoredFogCells[i];

            RevealPreviewCellInternal(
                new Vector3Int(
                    savedCell.x,
                    savedCell.y,
                    0
                ),
                false,
                false
            );
        }

        // Explored가 Preview보다 우선하므로
        // 완전 탐사 영역을 마지막에 복원한다.
        WorldMapRuntimeState.CopyExploredFogCells(
            mapId,
            restoredFogCells
        );

        for (int i = 0;
             i < restoredFogCells.Count;
             i++)
        {
            Vector2Int savedCell =
                restoredFogCells[i];

            RevealExploredCellInternal(
                new Vector3Int(
                    savedCell.x,
                    savedCell.y,
                    0
                ),
                false,
                false
            );
        }
    }

    // Player Marker의 현재 월드 위치를 확인하고,
    // 새로운 Grid 셀에 진입했을 때만 탐사 처리한다.
    public void TrackMarkerWorldPosition(
        Vector3 markerWorldPosition)
    {
        if (isInitialized == false)
        {
            return;
        }

        Grid mapGrid =
            GetMapGrid();

        if (mapGrid == null)
        {
            return;
        }

        Vector3Int markerCell =
            mapGrid.WorldToCell(
                markerWorldPosition
            );

        markerCell.z =
            0;

        if (hasLastMarkerCell &&
            markerCell ==
            lastMarkerCell)
        {
            return;
        }

        lastMarkerCell =
            markerCell;

        hasLastMarkerCell =
            true;

        RevealExploredCell(
            markerCell
        );
    }

    // 노드 중심과 주변 셀을 완전 탐사 상태로 전환한다.
    public void RevealExploredNodeArea(
        Vector3 nodeWorldPosition)
    {
        if (isInitialized == false)
        {
            return;
        }

        Grid mapGrid =
            GetMapGrid();

        if (mapGrid == null)
        {
            return;
        }

        Vector3Int centerCell =
            mapGrid.WorldToCell(
                nodeWorldPosition
            );

        centerCell.z =
            0;

        for (int y = -nodeAreaRadius;
             y <= nodeAreaRadius;
             y++)
        {
            for (int x = -nodeAreaRadius;
                 x <= nodeAreaRadius;
                 x++)
            {
                if (
                    x * x +
                    y * y >
                    nodeAreaRadius *
                    nodeAreaRadius
                )
                {
                    continue;
                }

                RevealExploredCell(
                    centerCell +
                    new Vector3Int(
                        x,
                        y,
                        0
                    )
                );
            }
        }
    }

    // 아직 방문하지 않은 인접 노드 주변을
    // 옅은 Preview 포그 상태로 표시한다.
    public void RevealPreviewNodeArea(
        Vector3 nodeWorldPosition)
    {
        if (isInitialized == false)
        {
            return;
        }

        Grid mapGrid =
            GetMapGrid();

        if (mapGrid == null)
        {
            return;
        }

        Vector3Int centerCell =
            mapGrid.WorldToCell(
                nodeWorldPosition
            );

        centerCell.z =
            0;

        for (int y = -nodeAreaRadius;
             y <= nodeAreaRadius;
             y++)
        {
            for (int x = -nodeAreaRadius;
                 x <= nodeAreaRadius;
                 x++)
            {
                if (
                    x * x +
                    y * y >
                    nodeAreaRadius *
                    nodeAreaRadius
                )
                {
                    continue;
                }

                RevealPreviewCell(
                    centerCell +
                    new Vector3Int(
                        x,
                        y,
                        0
                    )
                );
            }
        }
    }

    // 두 노드 사이의 Route와 실제 PathTilemap을 기준으로
    // 다음 탐사 후보 길을 옅게 표시한다.
    public void RevealPreviewRoute(
        MapNodeRuntime fromNode,
        MapNodeRuntime toNode,
        WorldMapRouteData route,
        bool useReverseWaypoints)
    {
        if (isInitialized == false ||
            fromNode == null ||
            toNode == null ||
            route == null)
        {
            return;
        }

        List<Vector3> routePoints =
            new List<Vector3>();

        routePoints.Add(
            fromNode.transform.position
        );

        if (route.waypoints != null)
        {
            if (useReverseWaypoints)
            {
                for (int i = route.waypoints.Count - 1;
                     i >= 0;
                     i--)
                {
                    Transform waypoint =
                        route.waypoints[i];

                    if (waypoint != null)
                    {
                        routePoints.Add(
                            waypoint.position
                        );
                    }
                }
            }
            else
            {
                for (int i = 0;
                     i < route.waypoints.Count;
                     i++)
                {
                    Transform waypoint =
                        route.waypoints[i];

                    if (waypoint != null)
                    {
                        routePoints.Add(
                            waypoint.position
                        );
                    }
                }
            }
        }

        routePoints.Add(
            toNode.transform.position
        );

        // 출발 노드, Waypoint, 목적지 노드 사이를
        // Grid 셀 선으로 연결하여 실제 길 타일만 Preview 처리한다.
        for (int i = 0;
             i < routePoints.Count - 1;
             i++)
        {
            RevealPreviewLine(
                routePoints[i],
                routePoints[i + 1]
            );
        }

        // 길뿐 아니라 목적지 노드 주변도
        // 옅은 포그 상태로 위치를 표시한다.
        RevealPreviewNodeArea(
            toNode.transform.position
        );
    }

    // 두 월드 위치 사이를 Grid 셀로 변환하고,
    // Bresenham 방식으로 지나가는 모든 셀을 찾는다.
    private void RevealPreviewLine(
        Vector3 startWorldPosition,
        Vector3 endWorldPosition)
    {
        Grid mapGrid =
            GetMapGrid();

        if (mapGrid == null)
        {
            return;
        }

        Vector3Int startCell =
            mapGrid.WorldToCell(
                startWorldPosition
            );

        Vector3Int endCell =
            mapGrid.WorldToCell(
                endWorldPosition
            );

        int currentX =
            startCell.x;

        int currentY =
            startCell.y;

        int targetX =
            endCell.x;

        int targetY =
            endCell.y;

        int deltaX =
            Mathf.Abs(
                targetX -
                currentX
            );

        int stepX =
            currentX <
            targetX
                ? 1
                : -1;

        int deltaY =
            -Mathf.Abs(
                targetY -
                currentY
            );

        int stepY =
            currentY <
            targetY
                ? 1
                : -1;

        int error =
            deltaX +
            deltaY;

        while (true)
        {
            Vector3Int currentCell =
                new Vector3Int(
                    currentX,
                    currentY,
                    0
                );

            bool hasPathTile =
                pathTilemap == null ||
                pathTilemap.HasTile(
                    currentCell
                );

            if (
                revealOnlyExistingPathTiles == false ||
                hasPathTile
            )
            {
                RevealPreviewCell(
                    currentCell
                );
            }

            if (currentX == targetX &&
                currentY == targetY)
            {
                break;
            }

            int doubledError =
                error *
                2;

            if (doubledError >=
                deltaY)
            {
                error +=
                    deltaY;

                currentX +=
                    stepX;
            }

            if (doubledError <=
                deltaX)
            {
                error +=
                    deltaX;

                currentY +=
                    stepY;
            }
        }
    }

    // 지정한 셀을 완전 탐사 상태로 등록한다.
    private void RevealExploredCell(
        Vector3Int centerCell)
    {
        RevealExploredCellInternal(
            centerCell,
            true,
            true
        );
    }

    // 지정한 셀을 다음 탐사 후보 상태로 등록한다.
    private void RevealPreviewCell(
        Vector3Int centerCell)
    {
        RevealPreviewCellInternal(
            centerCell,
            true,
            true
        );
    }

    // 탐사 완료 셀을 저장하고,
    // 중심은 완전히 밝게, 외곽은 점차 어둡게 처리한다.
    private void RevealExploredCellInternal(
        Vector3Int centerCell,
        bool saveRuntimeState,
        bool animate)
    {
        if (IsInsideMap(
                centerCell) ==
            false)
        {
            return;
        }

        if (saveRuntimeState)
        {
            WorldMapRuntimeState.RegisterExploredFogCell(
                GetCurrentMapId(),
                new Vector2Int(
                    centerCell.x,
                    centerCell.y
                )
            );
        }

        ApplyGradientAroundCell(
            centerCell,
            0f,
            exploredGradientRadius,
            animate
        );
    }

    // Preview 셀을 저장하고,
    // 중심은 옅은 포그, 외곽은 완전 미탐사 상태로 연결한다.
    private void RevealPreviewCellInternal(
        Vector3Int centerCell,
        bool saveRuntimeState,
        bool animate)
    {
        if (IsInsideMap(
                centerCell) ==
            false)
        {
            return;
        }

        if (saveRuntimeState)
        {
            WorldMapRuntimeState.RegisterPreviewFogCell(
                GetCurrentMapId(),
                new Vector2Int(
                    centerCell.x,
                    centerCell.y
                )
            );
        }

        ApplyGradientAroundCell(
            centerCell,
            previewAlpha,
            previewGradientRadius,
            animate
        );
    }

    // 중심 셀에서 바깥쪽으로 갈수록
    // Hidden Alpha에 가까워지는 셀 단위 그라데이션을 만든다.
    private void ApplyGradientAroundCell(
        Vector3Int centerCell,
        float centerAlpha,
        int gradientRadius,
        bool animate)
    {
        if (gradientRadius <=
            0)
        {
            SetCellTargetAlpha(
                centerCell,
                centerAlpha,
                animate
            );

            return;
        }

        for (int y = -gradientRadius;
             y <= gradientRadius;
             y++)
        {
            for (int x = -gradientRadius;
                 x <= gradientRadius;
                 x++)
            {
                float distance =
                    Mathf.Sqrt(
                        x * x +
                        y * y
                    );

                if (distance >
                    gradientRadius)
                {
                    continue;
                }

                float normalizedDistance =
                    distance /
                    gradientRadius;

                float targetAlpha =
                    Mathf.Lerp(
                        centerAlpha,
                        hiddenAlpha,
                        normalizedDistance
                    );

                SetCellTargetAlpha(
                    centerCell +
                    new Vector3Int(
                        x,
                        y,
                        0
                    ),
                    targetAlpha,
                    animate
                );
            }
        }
    }

    // 지정한 셀의 목표 Alpha를 갱신한다.
    //
    // 한 번 밝아진 셀은 더 어두운 상태로 되돌리지 않고,
    // 현재보다 더 밝아지는 변경만 허용한다.
    private void SetCellTargetAlpha(
        Vector3Int cellPosition,
        float newTargetAlpha,
        bool animate)
    {
        if (IsInsideMap(
                cellPosition) ==
            false)
        {
            return;
        }

        float previousTargetAlpha;

        if (targetAlphaByCell.TryGetValue(
                cellPosition,
                out previousTargetAlpha) ==
            false)
        {
            return;
        }

        newTargetAlpha =
            Mathf.Clamp01(
                newTargetAlpha
            );

        // Alpha가 작을수록 포그가 더 밝게 열린 상태이다.
        //
        // 기존보다 어두운 값은 적용하지 않아
        // 탐사 영역이 다시 가려지지 않도록 한다.
        if (newTargetAlpha >=
            previousTargetAlpha -
            0.0001f)
        {
            return;
        }

        targetAlphaByCell[cellPosition] =
            newTargetAlpha;

        if (animate == false)
        {
            currentAlphaByCell[cellPosition] =
                newTargetAlpha;

            ApplyCellColor(
                cellPosition,
                newTargetAlpha
            );

            return;
        }

        animatingCells.Add(
            cellPosition
        );

        if (fogAnimationCoroutine ==
            null)
        {
            fogAnimationCoroutine =
                StartCoroutine(
                    AnimateFogRoutine()
                );
        }
    }

    // 변경 중인 모든 셀을 하나의 코루틴에서
    // 현재 Alpha부터 목표 Alpha까지 함께 보간한다.
    private IEnumerator AnimateFogRoutine()
    {
        while (animatingCells.Count >
               0)
        {
            completedAnimationCells.Clear();

            foreach (
                Vector3Int cellPosition
                in animatingCells)
            {
                float currentAlpha =
                    currentAlphaByCell[
                        cellPosition
                    ];

                float targetAlpha =
                    targetAlphaByCell[
                        cellPosition
                    ];

                currentAlpha =
                    Mathf.MoveTowards(
                        currentAlpha,
                        targetAlpha,
                        fogFadeSpeed *
                        Time.unscaledDeltaTime
                    );

                currentAlphaByCell[cellPosition] =
                    currentAlpha;

                ApplyCellColor(
                    cellPosition,
                    currentAlpha
                );

                if (Mathf.Abs(
                        currentAlpha -
                        targetAlpha) <=
                    0.0001f)
                {
                    completedAnimationCells.Add(
                        cellPosition
                    );
                }
            }

            for (int i = 0;
                 i <
                 completedAnimationCells.Count;
                 i++)
            {
                animatingCells.Remove(
                    completedAnimationCells[i]
                );
            }

            yield return null;
        }

        fogAnimationCoroutine =
            null;
    }

    // 지정한 셀의 Tile 색상과 Alpha를 실제 Tilemap에 적용한다.
    private void ApplyCellColor(
        Vector3Int cellPosition,
        float alpha)
    {
        Color cellColor =
            fogColor;

        cellColor.a =
            Mathf.Clamp01(
                alpha
            );

        fogTilemap.SetColor(
            cellPosition,
            cellColor
        );
    }

    // 지정한 셀이 현재 월드맵 Grid 범위 안인지 확인한다.
    private bool IsInsideMap(
        Vector3Int cellPosition)
    {
        if (worldMapBuilder == null ||
            worldMapBuilder.WorldMapData == null)
        {
            return false;
        }

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        return
            cellPosition.x >= 0 &&
            cellPosition.x <
                mapData.gridWidth &&
            cellPosition.y >= 0 &&
            cellPosition.y <
                mapData.gridHeight;
    }
}
