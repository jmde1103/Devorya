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

    // 다음 탐사 후보 노드와 길 주변에 적용할
    // 옅은 Preview 그라데이션 반경
    //
    // 중심 Preview 영역 바깥으로 두 셀까지 점차 어두워져
    // 완전 미탐사 포그와 자연스럽게 연결되도록 한다.
    [SerializeField, Min(0)]
    private int previewGradientRadius =
        2;

    // Player Marker 주변에서
    // 완전히 밝게 탐사 처리할 셀 반경
    //
    // 값이 2이면 마커 중심 기준 5×5 영역이
    // 완전 탐사 상태로 유지된다.
    [SerializeField, Min(0)]
    private int markerAreaRadius =
        2;

    // 노드 중심 주변에서 함께 밝힐 셀 반경
    //
    // 일반 노드와 Preview 노드 영역은
    // 기존 3×3 범위를 유지한다.
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

    [Header("Scene Transition")]
    // 현재 열린 포그 영역이 마커 중심으로
    // 다시 수축하여 닫히는 데 걸리는 시간
    [SerializeField, Min(0.01f)]
    private float sceneFogCloseDuration =
    0.9f;

    // 포그가 닫히는 원형 경계의 부드러운 폭
    //
    // 값이 작으면 타일 단위로 선명하게 닫히고,
    // 값이 크면 여러 셀에 걸쳐 부드럽게 어두워진다.
    [SerializeField, Range(0.01f, 10f)]
    private float sceneFogCloseEdgeWidth =
        3.5f;

    // 포그 종료 전환 중 마커 주변에서
    // 가장 밝게 유지할 추가 셀 반경
    //
    // 1이면 마커 중심뿐 아니라 주변 한 칸까지
    // 같은 밝은 중심 영역으로 취급한다.
    [SerializeField, Range(0f, 5f)]
    private float sceneFogCenterClearRadius =
        1f;

    // 종료 연출 전체 진행 중
    // 화면 전체 검은색 페이드를 시작할 시점
    //
    // 0이면 연출 시작과 동시에 전체 화면이 서서히 어두워지고,
    // 0.3이면 전체 진행도의 30% 이후부터 페이드가 시작된다.
    [SerializeField, Range(0f, 0.9f)]
    private float sceneFogGlobalFadeStart =
        0.15f;

    // 최종적으로 포그 전체에 적용할 검은색 Alpha
    [SerializeField, Range(0f, 1f)]
    private float sceneFinalFogAlpha =
        1f;

    // 전체 포그가 닫힌 뒤
    // 씬을 전환하기 전 유지할 시간
    [SerializeField, Min(0f)]
    private float sceneCloseHoldDuration =
        0.1f;

    // 현재 포그 씬 전환 효과가 실행 중인지 확인한다.
    private bool isSceneTransitionPlaying;

    // 흰색 전환 시작 전
    // 각 셀에 적용되어 있던 실제 색상을 임시 보관한다.
    private readonly Dictionary<Vector3Int, Color>
        sceneTransitionStartColors =
            new Dictionary<Vector3Int, Color>();

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

    // 현재 포그 Tilemap Renderer가 사용하는
    // Sorting Layer ID를 반환한다.
    //
    // Player Marker를 포그 위에 표시할 때
    // 동일한 Sorting Layer를 사용하기 위한 값이다.
    public int GetFogSortingLayerId()
    {
        if (fogTilemap == null)
        {
            return 0;
        }

        TilemapRenderer fogTilemapRenderer =
            fogTilemap.GetComponent<TilemapRenderer>();

        if (fogTilemapRenderer == null)
        {
            return 0;
        }

        return
            fogTilemapRenderer.sortingLayerID;
    }

    // 현재 포그 Tilemap Renderer가 사용하는
    // Order in Layer 값을 반환한다.
    //
    // Player Marker는 전환 중 이 값보다 1 높은 순서로 표시한다.
    public int GetFogSortingOrder()
    {
        if (fogTilemap == null)
        {
            return 0;
        }

        TilemapRenderer fogTilemapRenderer =
            fogTilemap.GetComponent<TilemapRenderer>();

        if (fogTilemapRenderer == null)
        {
            return 0;
        }

        return
            fogTilemapRenderer.sortingOrder;
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

    // Player Marker의 현재 위치를 중심으로
    // 마커 전용 반경만큼 완전 탐사 영역을 적용한다.
    //
    // 맵 시작 배치와 실제 이동 중 탐사에
    // 동일한 밝기 범위를 사용하기 위한 공통 함수다.
    public void RevealExploredMarkerArea(
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

        // 마커 중심에서 원형 반경 안에 포함되는 셀만
        // 완전 탐사 영역으로 처리한다.
        //
        // Marker Area Radius가 2일 때 5×5 범위를 검사하지만,
        // 중심에서 반경 2를 초과하는 모서리 셀은 제외하여
        // 가장 밝은 영역이 사각형으로 보이지 않게 한다.
        for (int y = -markerAreaRadius;
     y <= markerAreaRadius;
     y++)
        {
            for (int x = -markerAreaRadius;
                 x <= markerAreaRadius;
                 x++)
            {
                // 가장 밝은 영역은 원형이 아니라
                // "사각형에서 4개 꼭짓점만 뺀 형태"로 처리한다.
                //
                // Marker Area Radius가 2일 때 결과는:
                //
                // · ■ ■ ■ ·
                // ■ ■ ■ ■ ■
                // ■ ■ ■ ■ ■
                // ■ ■ ■ ■ ■
                // · ■ ■ ■ ·
                //
                // 즉 바깥 4개 꼭짓점만 제외한다.
                if (Mathf.Abs(x) == markerAreaRadius &&
                    Mathf.Abs(y) == markerAreaRadius)
                {
                    continue;
                }

                RevealExploredCell(
                    markerCell +
                    new Vector3Int(
                        x,
                        y,
                        0
                    )
                );
            }
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

        // 마커가 새로운 셀에 진입할 때마다
        // 마커 전용 밝기 반경을 적용한다.
        RevealExploredMarkerArea(
            markerWorldPosition
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

        // 노드 중심의 완전 탐사 영역은
        // 대각선 셀까지 포함한 사각형 형태로 밝힌다.
        //
        // Node Area Radius가 1이면
        // 중심을 포함한 3×3 셀이 완전히 밝아진다.
        for (int y = -nodeAreaRadius;
             y <= nodeAreaRadius;
             y++)
        {
            for (int x = -nodeAreaRadius;
                 x <= nodeAreaRadius;
                 x++)
            {
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

        // 다음 탐사 후보 노드도 대각선 셀까지 포함하여
        // 사각형 형태의 Preview 중심 영역을 만든다.
        //
        // 중심 영역 바깥쪽은 Preview Gradient Radius에 따라
        // 완전 미탐사 포그까지 점차 어두워진다.
        for (int y = -nodeAreaRadius;
             y <= nodeAreaRadius;
             y++)
        {
            for (int x = -nodeAreaRadius;
                 x <= nodeAreaRadius;
                 x++)
            {
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

    // 두 노드 사이의 Connection Route Grid 좌표와
    // 실제 PathTilemap을 기준으로 다음 탐사 후보 길을 표시한다.
    public void RevealPreviewRoute(
        MapNodeRuntime fromNode,
        MapNodeRuntime toNode,
        MapNodeConnectionData connection,
        bool useReverseRoute)
    {
        if (isInitialized == false ||
            fromNode == null ||
            toNode == null ||
            connection == null)
        {
            return;
        }

        Grid mapGrid =
            GetMapGrid();

        if (mapGrid == null)
        {
            return;
        }

        List<Vector3> routePoints =
            new List<Vector3>();

        // 출발 노드 중심을 Route 첫 지점으로 사용한다.
        routePoints.Add(
            fromNode.transform.position
        );

        if (connection.routeGridPositions !=
            null)
        {
            if (useReverseRoute)
            {
                // 역방향 Preview에서는 Route 좌표를 뒤에서부터 사용한다.
                for (int i =
                         connection.routeGridPositions.Count - 1;
                     i >= 0;
                     i--)
                {
                    Vector2Int routeGridPosition =
                        connection.routeGridPositions[i];

                    routePoints.Add(
                        mapGrid.GetCellCenterWorld(
                            new Vector3Int(
                                routeGridPosition.x,
                                routeGridPosition.y,
                                0
                            )
                        )
                    );
                }
            }
            else
            {
                // 정방향 Preview에서는 저장된 순서를 그대로 사용한다.
                for (int i = 0;
                     i <
                     connection.routeGridPositions.Count;
                     i++)
                {
                    Vector2Int routeGridPosition =
                        connection.routeGridPositions[i];

                    routePoints.Add(
                        mapGrid.GetCellCenterWorld(
                            new Vector3Int(
                                routeGridPosition.x,
                                routeGridPosition.y,
                                0
                            )
                        )
                    );
                }
            }
        }

        // 마지막에는 목적지 노드 중심까지 연결한다.
        routePoints.Add(
            toNode.transform.position
        );

        // 각 Route 지점 사이를 Grid 선으로 연결하여
        // 실제 Path Tile이 있는 셀만 Preview 처리한다.
        for (int i = 0;
             i < routePoints.Count - 1;
             i++)
        {
            RevealPreviewLine(
                routePoints[i],
                routePoints[i + 1]
            );
        }

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

    // 맵 시작 연출을 반대로 재생하는 느낌으로,
    // 마커 주변의 열린 포그 영역을 바깥쪽부터 다시 닫는다.
    //
    // 카메라 축소와 같은 시간 동안 실행되며,
    // 마지막에는 모든 셀이 완전한 검은 포그로 덮인다.
    public IEnumerator PlaySceneCloseTransition(
        Vector3 centerWorldPosition,
        float requestedDuration = -1f)
    {
        if (isInitialized == false ||
            isSceneTransitionPlaying)
        {
            yield break;
        }

        if (worldMapBuilder == null ||
            worldMapBuilder.WorldMapData == null ||
            fogTilemap == null)
        {
            yield break;
        }

        Grid mapGrid =
            GetMapGrid();

        if (mapGrid == null)
        {
            yield break;
        }

        isSceneTransitionPlaying =
            true;

        // 일반 탐사 포그 애니메이션이 종료 연출에 개입하지 않도록 중단한다.
        if (fogAnimationCoroutine != null)
        {
            StopCoroutine(
                fogAnimationCoroutine
            );

            fogAnimationCoroutine =
                null;
        }

        animatingCells.Clear();
        completedAnimationCells.Clear();
        sceneTransitionStartColors.Clear();

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        Vector3Int centerCell =
            mapGrid.WorldToCell(
                centerWorldPosition
            );

        centerCell.z =
            0;

        // 종료 연출이 시작되는 순간의 셀별 색상을 저장한다.
        //
        // 탐사 완료, Preview, 미탐사 상태를 그대로 보존한 채
        // 바깥쪽 셀부터 검정으로 닫기 위해 사용한다.
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

                sceneTransitionStartColors[cellPosition] =
                    fogTilemap.GetColor(
                        cellPosition
                    );
            }
        }

        float maximumRadius =
            GetMaximumRadiusFromCenterCell(
                mapData,
                centerCell
            );

        float safeDuration =
            requestedDuration > 0f
                ? requestedDuration
                : sceneFogCloseDuration;

        safeDuration =
            Mathf.Max(
                0.01f,
                safeDuration
            );

        Color finalFogColor =
            fogColor;

        finalFogColor.a =
            Mathf.Clamp01(
                sceneFinalFogAlpha
            );

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    safeDuration
                );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            // 시작 시에는 전체 반경이 열려 있고,
            // 시간이 지날수록 열린 반경이 마커 중심으로 줄어든다.
            float openRadius =
                Mathf.Lerp(
                    maximumRadius +
                    sceneFogCloseEdgeWidth,
                    -sceneFogCloseEdgeWidth,
                    smoothTime
                );

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

                    // 현재 셀이 Player Marker 중심에서
                    // 실제로 얼마나 떨어져 있는지 계산한다.
                    float rawDistanceFromCenter =
                        Vector2.Distance(
                            new Vector2(
                                x,
                                y
                            ),
                            new Vector2(
                                centerCell.x,
                                centerCell.y
                            )
                        );

                    // 마커 중심 주변의 지정 반경은
                    // 모두 중심과 동일한 거리 0으로 취급한다.
                    //
                    // Scene Fog Center Clear Radius가 1이면
                    // 마커 주변 한 칸까지 가장 밝은 중심 영역으로 유지되고,
                    // 그 바깥쪽부터 포그 수축 그라데이션이 시작된다.
                    float distanceFromCenter =
                        Mathf.Max(
                            0f,
                            rawDistanceFromCenter -
                            sceneFogCenterClearRadius
                        );

                    // 열린 반경보다 바깥쪽 셀부터 검은 포그로 닫힌다.
                    //
                    // Edge Width 범위에서는 시작 색상과 검은색을 보간하여
                    // 경계가 계단처럼 갑자기 닫히지 않도록 한다.
                    float radialCloseBlend =
                        Mathf.InverseLerp(
                            openRadius -
                            sceneFogCloseEdgeWidth,
                            openRadius +
                            sceneFogCloseEdgeWidth,
                            distanceFromCenter
                        );

                    radialCloseBlend =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            radialCloseBlend
                        );

                    // 원형 포그 수축과 별개로 화면 전체도 천천히 검게 페이드한다.
                    //
                    // 전체 진행도가 Scene Fog Global Fade Start에 도달하기 전에는
                    // 기존 화면 밝기를 유지하고, 그 이후부터 종료 시점까지
                    // 모든 셀이 단계적으로 최종 검은색에 가까워진다.
                    float globalFadeProgress =
                        Mathf.InverseLerp(
                            sceneFogGlobalFadeStart,
                            1f,
                            smoothTime
                        );

                    float globalFadeBlend =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            globalFadeProgress
                        );

                    // 원형 수축과 전체 화면 페이드를 결합한다.
                    //
                    // 이미 원형 경계 바깥으로 닫힌 셀은 검은색을 유지하고,
                    // 아직 열린 중심 영역도 시간이 지날수록 천천히 어두워진다.
                    float combinedCloseBlend =
                        1f -
                        (
                            1f -
                            radialCloseBlend
                        ) *
                        (
                            1f -
                            globalFadeBlend
                        );

                    Color startColor;

                    if (sceneTransitionStartColors.TryGetValue(
                            cellPosition,
                            out startColor) ==
                        false)
                    {
                        startColor =
                            fogTilemap.GetColor(
                                cellPosition
                            );
                    }

                    // 셀의 시작 색상에서 최종 검은색까지
                    // 결합된 진행값을 사용해 부드럽게 페이드한다.
                    Color appliedColor =
                        Color.Lerp(
                            startColor,
                            finalFogColor,
                            combinedCloseBlend
                        );

                    fogTilemap.SetColor(
                        cellPosition,
                        appliedColor
                    );
                }
            }

            yield return null;
        }

        // 마지막에는 모든 셀을 완전한 검은 포그로 고정한다.
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

                currentAlphaByCell[cellPosition] =
                    finalFogColor.a;

                targetAlphaByCell[cellPosition] =
                    finalFogColor.a;

                fogTilemap.SetColor(
                    cellPosition,
                    finalFogColor
                );
            }
        }

        sceneTransitionStartColors.Clear();

        if (sceneCloseHoldDuration >
            0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    sceneCloseHoldDuration
                );
        }

        isSceneTransitionPlaying =
            false;
    }

    // 중심 셀에서 맵 네 모서리까지의 거리 중
    // 가장 큰 값을 반환한다.
    //
    // 전환이 마지막에 맵 전체를 완전히 덮도록
    // 외곽 최대 반경을 기준으로 사용한다.
    private float GetMaximumRadiusFromCenterCell(
        WorldMapData mapData,
        Vector3Int centerCell)
    {
        Vector2[] cornerPositions =
        {
        new Vector2(
            0f,
            0f
        ),
        new Vector2(
            mapData.gridWidth - 1,
            0f
        ),
        new Vector2(
            0f,
            mapData.gridHeight - 1
        ),
        new Vector2(
            mapData.gridWidth - 1,
            mapData.gridHeight - 1
        )
    };

        float maximumRadius =
            0f;

        for (int i = 0;
             i < cornerPositions.Length;
             i++)
        {
            float distance =
                Vector2.Distance(
                    new Vector2(
                        centerCell.x,
                        centerCell.y
                    ),
                    cornerPositions[i]
                );

            if (distance >
                maximumRadius)
            {
                maximumRadius =
                    distance;
            }
        }

        return maximumRadius;
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
