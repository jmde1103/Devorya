using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

// 노드 해금, 검은 구체 이동,
// 전투 씬 진입과 복귀 진행도를 관리한다.
public class WorldMapProgressController : MonoBehaviour
{
    public static WorldMapProgressController Instance
    {
        get;
        private set;
    }

    [Header("Map References")]
    // WorldMapData를 읽고 실제 노드를 생성하는 Builder
    [SerializeField]
    private WorldMapBuilder worldMapBuilder;

    // <변경부분> 전투를 완료한 노드에 적용할
    // Cleared 타입 전용 스타일 데이터
    [SerializeField]
    private MapNodeStyleData clearedNodeStyleData;

    // 맵 위에서 이동하는 검은 구체의 이동 기준 부모 Transform
    //
    // 실제 위치 이동은 PlayerMarkerRoot가 담당하고,
    // 시각적 위치 보정은 자식 Spine 오브젝트의 Local Position으로 처리한다.
    [SerializeField]
    private Transform playerMarker;
    // 마커가 지나간 셀의 탐사 처리와
    // 현재 노드에서 이어지는 다음 노드·길 Preview를 관리한다.
    [SerializeField]
    private WorldMapFogController worldMapFogController;

    // 마커 이동 중 줌·드래그 입력을 잠그고
    // 카메라가 Player Marker를 따라가게 하는 컨트롤러
    [SerializeField]
    private WorldMapCameraController worldMapCameraController;

    [Header("Player Marker Animation")]
    // 검은 구체 애니메이션을 재생하는 Spine SkeletonAnimation
    //
    // PlayerMarkerRoot의 자식인 실제 Spine 오브젝트를 연결한다.
    [SerializeField]
    private SkeletonAnimation playerMarkerSkeletonAnimation;

    // Player Marker의 실제 화면 정렬을 담당하는 Renderer
    //
    // Spine SkeletonAnimation 오브젝트에 연결된
    // MeshRenderer를 지정한다.
    [SerializeField]
    private Renderer playerMarkerRenderer;

    // 포그 전환이 끝난 뒤 복원할
    // Player Marker의 원래 Sorting Layer ID
    private int originalMarkerSortingLayerId;

    // 포그 전환이 끝난 뒤 복원할
    // Player Marker의 원래 Order in Layer
    private int originalMarkerSortingOrder;

    // Player Marker의 기존 정렬값을 저장했는지 확인한다.
    private bool hasStoredOriginalMarkerSorting;

    // 노드 도착 직후 재생할 선택 애니메이션
    [SerializeField]
    private string markerSelectAnimationName =
        "Select";

    // Select 다음에 재생할 흡수 애니메이션
    [SerializeField]
    private string markerAbsorbAnimationName =
        "Absorb";

    // Absorb 다음에 재생할 마무리 애니메이션
    [SerializeField]
    private string markerDownAbsorbAnimationName =
        "Down_Absorb";

    // 각 애니메이션 사이에 추가할 짧은 대기시간
    [SerializeField, Min(0f)]
    private float markerAnimationInterval =
        0.05f;

    [Header("Movement")]
    // 검은 구체가 경로를 따라 이동하는 속도
    //
    // 기존 3보다 빠른 5를 기본값으로 사용하여
    // 노드 사이 이동이 지나치게 느리지 않도록 한다.
    [SerializeField, Min(0.01f)]
    private float markerMoveSpeed =
    5f;

    // 목적지에 도착한 뒤 전투 씬으로 이동하기 전 대기 시간
    [SerializeField, Min(0f)]
    private float waitBeforeSceneMove =
        0.25f;

    // 현재 검은 구체가 이동 중인지 확인한다.
    private bool isMovingMarker;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(
                gameObject
            );

            return;
        }

        Instance =
            this;
    }

    private void Start()
    {
        // WorldMapBuilder의 Start에서 노드가 생성된 다음
        // 진행도와 Player Marker 위치를 적용하기 위해
        // 초기화 코루틴을 실행한다.
        StartCoroutine(
            InitializeWorldMapRoutine()
        );
    }

    private void OnDestroy()
    {
        if (Instance ==
            this)
        {
            Instance =
                null;
        }
    }

    // 월드맵 생성 완료 후
    // 시작 노드, 전투 승리 결과, 해금 상태를 적용한다.
    private IEnumerator InitializeWorldMapRoutine()
    {
        yield return null;

        if (ValidateReferences() == false)
        {
            yield break;
        }

        // 전투 승리 후 맵으로 돌아온 상태라면
        // 방금 완료한 전투 노드를 클리어 처리한다.
        string completedBattleNodeId =
            WorldMapRuntimeState
                .ApplyPendingBattleWin();

        if (string.IsNullOrWhiteSpace(
                completedBattleNodeId) ==
            false)
        {
            UnlockConnectedNodes(
                completedBattleNodeId
            );
        }

        // 런타임 위치가 없다면
        // Initially Cleared로 설정된 시작 노드를 찾는다.
        if (string.IsNullOrWhiteSpace(
                WorldMapRuntimeState.CurrentNodeId))
        {
            MapNodePlacementData startPlacement =
                FindInitialStartPlacement();

            if (startPlacement == null)
            {
                Debug.LogWarning(
                    "월드맵 시작 실패: " +
                    "Initially Cleared가 활성화된 시작 노드가 없습니다."
                );

                yield break;
            }

            WorldMapRuntimeState
                .InitializeStartNode(
                    startPlacement.nodeId
                );

            // 처음부터 클리어된 시작 노드의
            // 다음 연결 노드들을 해금한다.
            UnlockConnectedNodes(
                startPlacement.nodeId
            );
        }

        // 런타임 진행도에 맞춰 노드 상태와
        // Player Marker의 시작 위치를 적용한다.
        ApplyRuntimeNodeStates();
        PlaceMarkerAtCurrentNode();

        // 현재 탐사 완료 노드 주변을 밝히고,
        // 해당 노드와 연결된 미탐사 노드·길을 Preview 상태로 표시한다.
        RefreshFogForCurrentNode();

        // 맵이 시작될 때도 이동 중과 동일하게
        // Player Marker 전용 밝기 반경을 즉시 적용한다.
        //
        // 기존에는 시작 노드의 Node Area Radius만 적용되어
        // 이동 중보다 밝은 영역이 좁게 표시되고 있었다.
        if (worldMapFogController != null &&
            playerMarker != null)
        {
            worldMapFogController
                .RevealExploredMarkerArea(
                    playerMarker.position
                );
        }

        // 노드·마커·포그 초기화가 모두 완료된 다음,
        // 카메라를 현재 Player Marker 중심에 맞추고
        // 1배율에서 2배율까지 부드럽게 확대한다.
        if (worldMapCameraController != null)
        {
            yield return
                worldMapCameraController
                    .PlayMapStartZoomRoutine(
                        playerMarker
                    );
        }
    }

    // 월드맵 진행 기능에 필요한 참조를 검사한다.
    private bool ValidateReferences()
    {
        if (worldMapBuilder == null)
        {
            Debug.LogWarning(
                "월드맵 진행 초기화 실패: " +
                "World Map Builder가 연결되지 않았습니다."
            );

            return false;
        }

        if (worldMapBuilder.WorldMapData == null)
        {
            Debug.LogWarning(
                "월드맵 진행 초기화 실패: " +
                "World Map Data가 연결되지 않았습니다."
            );

            return false;
        }

        // <변경부분> 클리어 노드 표시용 스타일이 빠졌다면
        // 진행은 계속하되 화면 스타일 변경이 되지 않음을 알린다.
        if (clearedNodeStyleData == null)
        {
            Debug.LogWarning(
                "월드맵 Cleared 스타일 연결 경고: " +
                "Cleared Node Style Data가 연결되지 않았습니다. " +
                "노드 해금은 진행되지만 클리어 Sprite는 변경되지 않습니다."
            );
        }

        if (playerMarker == null)
        {
            Debug.LogWarning(
                "월드맵 진행 초기화 실패: " +
                "Player Marker가 연결되지 않았습니다."
            );

            return false;
        }

        // WorldMapFogController가 같은 GameObject에 있다면
        // Inspector 연결이 빠져 있어도 자동으로 찾는다.
        if (worldMapFogController == null)
        {
            worldMapFogController =
                GetComponent<WorldMapFogController>();
        }

        if (worldMapFogController == null)
        {
            Debug.LogWarning(
                "월드맵 포그 연결 경고: " +
                "World Map Fog Controller가 연결되지 않았습니다. " +
                "노드 진행은 작동하지만 포그 탐사는 갱신되지 않습니다."
            );
        }

        // Inspector에서 카메라 컨트롤러 연결이 빠졌다면
        // 현재 씬의 Main Camera에서 자동으로 찾는다.
        if (worldMapCameraController == null &&
            Camera.main != null)
        {
            worldMapCameraController =
                Camera.main.GetComponent<WorldMapCameraController>();
        }

        if (worldMapCameraController == null)
        {
            Debug.LogWarning(
                "월드맵 카메라 연결 경고: " +
                "World Map Camera Controller가 연결되지 않았습니다. " +
                "마커 이동은 가능하지만 줌 잠금과 카메라 추적은 실행되지 않습니다."
            );
        }

        // PlayerMarkerRoot만 Inspector에 연결된 경우,
        // 자식에서 실제 Spine SkeletonAnimation을 자동으로 찾는다.
        if (playerMarkerSkeletonAnimation == null)
        {
            playerMarkerSkeletonAnimation =
                playerMarker.GetComponentInChildren<SkeletonAnimation>(
                    true
                );
        }

        if (playerMarkerSkeletonAnimation == null)
        {
            Debug.LogWarning(
                "Player Marker 애니메이션 연결 경고: " +
                "PlayerMarkerRoot 자식에서 SkeletonAnimation을 찾지 못했습니다. " +
                "노드 이동은 실행되지만 도착 애니메이션은 생략됩니다."
            );
        }

        // Inspector에서 Player Marker Renderer 연결이 빠졌다면
        // PlayerMarkerRoot 자식에서 Spine MeshRenderer를 자동으로 찾는다.
        if (playerMarkerRenderer == null &&
            playerMarker != null)
        {
            playerMarkerRenderer =
                playerMarker.GetComponentInChildren<Renderer>(
                    true
                );
        }

        if (playerMarkerRenderer == null)
        {
            Debug.LogWarning(
                "Player Marker Renderer 연결 경고: " +
                "PlayerMarkerRoot 자식에서 Renderer를 찾지 못했습니다. " +
                "포그 전환 중 마커를 포그 위로 올리는 처리는 생략됩니다."
            );
        }

        return true;
    }

    // 처음부터 클리어 상태인 시작 노드를 찾는다.
    private MapNodePlacementData
        FindInitialStartPlacement()
    {
        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        for (int i = 0;
             i < mapData.nodePlacements.Count;
             i++)
        {
            MapNodePlacementData placement =
                mapData.nodePlacements[i];

            if (placement == null)
            {
                continue;
            }

            if (placement.initiallyCleared)
            {
                return placement;
            }
        }

        return null;
    }

    // 현재 런타임 진행도에 맞춰
    // 생성된 각 노드의 해금·클리어 상태를 갱신한다.
    private void ApplyRuntimeNodeStates()
    {
        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        for (int i = 0;
             i < mapData.nodePlacements.Count;
             i++)
        {
            MapNodePlacementData placement =
                mapData.nodePlacements[i];

            if (placement == null)
            {
                continue;
            }

            MapNodeRuntime runtimeNode =
                worldMapBuilder.GetGeneratedNode(
                    placement.nodeId
                );

            if (runtimeNode == null)
            {
                continue;
            }

            // 현재 노드가 처음부터 클리어 상태인지,
            // 또는 런타임에서 전투를 완료한 노드인지 먼저 확인한다.
            bool isCleared =
                placement.initiallyCleared ||
                WorldMapRuntimeState
                    .IsNodeCleared(
                        placement.nodeId
                    );

            // 클리어된 노드는 이미 플레이어가 방문한 구역이므로
            // 별도의 Unlock 기록이 없어도 항상 이동 가능 상태로 처리한다.
            bool isUnlocked =
                placement.initiallyUnlocked ||
                WorldMapRuntimeState
                    .IsNodeUnlocked(
                        placement.nodeId
                    ) ||
                isCleared;

            runtimeNode.SetUnlocked(
                isUnlocked
            );

            // 런타임 클리어 상태와 함께
            // Cleared 전용 스타일 데이터도 노드에 전달한다.
            //
            // 클리어된 전투 노드는 기존 전투 노드 Sprite 대신
            // Cleared 노드 Sprite와 색상으로 즉시 변경된다.
            runtimeNode.SetCleared(
                isCleared,
                clearedNodeStyleData
            );
        }
    }

    // 현재 저장된 노드 위치로
    // 검은 구체를 즉시 배치한다.
    private void PlaceMarkerAtCurrentNode()
    {
        string currentNodeId =
            WorldMapRuntimeState.CurrentNodeId;

        MapNodeRuntime currentNode =
            worldMapBuilder.GetGeneratedNode(
                currentNodeId
            );

        if (currentNode == null)
        {
            Debug.LogWarning(
                $"Player Marker 배치 실패: " +
                $"{currentNodeId} 노드를 찾을 수 없습니다."
            );

            return;
        }

        playerMarker.position =
            currentNode.transform.position;
    }

    // MapNodeRuntime이 클릭됐을 때 호출한다.
    public void TryMoveToNode(
        MapNodeRuntime targetNode)
    {
        if (isMovingMarker)
        {
            return;
        }

        if (targetNode == null)
        {
            return;
        }

        // 잠긴 미클리어 노드만 이동을 차단한다.
        //
        // 이미 클리어된 노드는 과거에 방문한 구역이므로
        // 현재 Unlock 값과 관계없이 다시 이동할 수 있다.
        if (targetNode.IsUnlocked() == false &&
            targetNode.IsCleared() == false)
        {
            Debug.Log(
                $"노드 이동 불가: " +
                $"{targetNode.GetNodeDisplayName()} 노드는 잠겨 있습니다."
            );

            return;
        }

        string currentNodeId =
            WorldMapRuntimeState.CurrentNodeId;

        string targetNodeId =
            targetNode.GetNodeId();

        if (string.IsNullOrWhiteSpace(
                currentNodeId) ||
            string.IsNullOrWhiteSpace(
                targetNodeId))
        {
            return;
        }

        if (currentNodeId ==
            targetNodeId)
        {
            Debug.Log(
                "현재 위치한 노드입니다."
            );

            return;
        }

        MapNodeConnectionData connection;

        bool useReverseRoute;

        // 현재 노드 또는 목적지 노드에 저장된 Connection을 찾는다.
        //
        // 정방향:
        // Current → Target Connection 사용
        //
        // 역방향:
        // Target → Current Connection을 찾아
        // Route Grid Positions를 역순으로 사용한다.
        if (TryFindConnection(
                currentNodeId,
                targetNodeId,
                out connection,
                out useReverseRoute) ==
            false)
        {
            Debug.Log(
                $"노드 이동 불가: " +
                $"{currentNodeId} ↔ {targetNodeId} 사이에 " +
                $"Connection이 없습니다."
            );

            return;
        }

        StartCoroutine(
            MoveMarkerToNodeRoutine(
                targetNode,
                connection,
                useReverseRoute
            )
        );
    }

    // 두 노드 사이의 Connection을 찾는다.
    //
    // Connection은 한쪽 노드에만 등록되어 있어도
    // 실제 이동에서는 양방향으로 사용할 수 있다.
    //
    // 역방향 Connection을 사용하면
    // Route Grid Positions를 반대 순서로 이동한다.
    private bool TryFindConnection(
        string fromNodeId,
        string toNodeId,
        out MapNodeConnectionData foundConnection,
        out bool useReverseRoute)
    {
        foundConnection =
            null;

        useReverseRoute =
            false;

        MapNodePlacementData fromPlacement =
            FindPlacementById(
                fromNodeId
            );

        MapNodePlacementData toPlacement =
            FindPlacementById(
                toNodeId
            );

        // 현재 노드 → 목적지 방향 Connection을 먼저 찾는다.
        if (fromPlacement != null &&
            fromPlacement.connections != null)
        {
            for (int i = 0;
                 i < fromPlacement.connections.Count;
                 i++)
            {
                MapNodeConnectionData connection =
                    fromPlacement.connections[i];

                if (connection == null ||
                    string.IsNullOrWhiteSpace(
                        connection.targetNodeId))
                {
                    continue;
                }

                if (connection.targetNodeId.Trim() ==
                    toNodeId.Trim())
                {
                    foundConnection =
                        connection;

                    useReverseRoute =
                        false;

                    return true;
                }
            }
        }

        // 반대편 노드에 목적지 → 현재 Connection이 있다면
        // 같은 Route를 역방향으로 사용할 수 있다.
        if (toPlacement != null &&
            toPlacement.connections != null)
        {
            for (int i = 0;
                 i < toPlacement.connections.Count;
                 i++)
            {
                MapNodeConnectionData connection =
                    toPlacement.connections[i];

                if (connection == null ||
                    string.IsNullOrWhiteSpace(
                        connection.targetNodeId))
                {
                    continue;
                }

                if (connection.targetNodeId.Trim() ==
                    fromNodeId.Trim())
                {
                    foundConnection =
                        connection;

                    useReverseRoute =
                        true;

                    return true;
                }
            }
        }

        return false;
    }

    // 검은 구체가 Waypoint를 순서대로 지나
    // 목적지 노드까지 이동한다.
    //
    // 미클리어 노드에 도착하면 기존처럼 전투 씬으로 진입하고,
    // 이미 클리어된 노드에 도착하면 위치만 변경한 뒤 월드맵에 남는다.
    private IEnumerator MoveMarkerToNodeRoutine(
    MapNodeRuntime targetNode,
    MapNodeConnectionData connection,
    bool useReverseRoute)
    {
        isMovingMarker =
            true;

        // 마커가 경로 이동을 시작하는 순간부터
        // 현재 확대 배율을 잠그고 카메라가 마커를 따라가게 한다.
        if (worldMapCameraController != null)
        {
            worldMapCameraController
                .SetMarkerFollow(
                    playerMarker,
                    true
                );
        }

        // 현재 Connection에 저장된 Grid Route 좌표를
        // 실제 월드 위치로 변환해 순서대로 이동한다.
        if (connection != null &&
            connection.routeGridPositions != null)
        {
            Grid mapGrid =
                worldMapBuilder.MapGrid;

            if (mapGrid != null)
            {
                if (useReverseRoute)
                {
                    // 과거 노드로 돌아갈 때는
                    // 저장된 Route Grid 좌표를 역순으로 따라간다.
                    for (int i =
                             connection.routeGridPositions.Count - 1;
                         i >= 0;
                         i--)
                    {
                        Vector2Int routeGridPosition =
                            connection.routeGridPositions[i];

                        Vector3 routeWorldPosition =
                            mapGrid.GetCellCenterWorld(
                                new Vector3Int(
                                    routeGridPosition.x,
                                    routeGridPosition.y,
                                    0
                                )
                            );

                        yield return
                            MoveMarkerToPositionRoutine(
                                routeWorldPosition
                            );
                    }
                }
                else
                {
                    // 새로운 노드 방향으로 이동할 때는
                    // 저장된 Route Grid 좌표 순서를 그대로 따른다.
                    for (int i = 0;
                         i <
                         connection.routeGridPositions.Count;
                         i++)
                    {
                        Vector2Int routeGridPosition =
                            connection.routeGridPositions[i];

                        Vector3 routeWorldPosition =
                            mapGrid.GetCellCenterWorld(
                                new Vector3Int(
                                    routeGridPosition.x,
                                    routeGridPosition.y,
                                    0
                                )
                            );

                        yield return
                            MoveMarkerToPositionRoutine(
                                routeWorldPosition
                            );
                    }
                }
            }
        }

        // 마지막 Waypoint 위치와 관계없이
        // 최종적으로 목적지 노드 중심에 정확히 배치한다.
        yield return
      MoveMarkerToPositionRoutine(
          targetNode.transform.position
      );

        // 목적지 노드에 실제로 도착했으므로
        // 노드 중심과 주변 셀을 완전 탐사 상태로 전환한다.
        if (worldMapFogController != null)
        {
            worldMapFogController
                .RevealExploredNodeArea(
                    targetNode.transform.position
                );
        }

        string targetNodeId =
            targetNode.GetNodeId();

        // 목적지 노드의 원본 배치 데이터를 가져온다.
        //
        // 화면에 적용된 Sprite나 MapNodeRuntime 내부 표시 상태가 아니라,
        // WorldMapData와 런타임 진행도를 기준으로 실제 클리어 여부를 판단한다.
        MapNodePlacementData targetPlacement =
     FindPlacementById(
         targetNodeId
     );

        // 목적지 노드의 클리어 여부를 세 가지 기준으로 확인한다.
        //
        // 1. MapNodeRuntime에 실제 적용된 클리어 상태
        // 2. 전투 완료 후 저장된 런타임 클리어 기록
        // 3. WorldMapData에서 처음부터 클리어된 시작 노드 설정
        //
        // 시작 노드처럼 Target Scene Name이 없는 노드도
        // 하나의 기준이라도 클리어 상태라면 전투 씬 검사 없이 이동을 완료한다.
        bool isTargetNodeCleared =
    targetNode.IsCleared() ||
    WorldMapRuntimeState.IsNodeCleared(
        targetNodeId
    ) ||
    (
        targetPlacement != null &&
        targetPlacement.initiallyCleared
    );

        // 현재 월드맵 위치를 목적지 노드로 갱신한다.
        WorldMapRuntimeState.SetCurrentNode(
            targetNodeId
        );

        // 이미 클리어된 노드는 Target Scene Name이 없어도 정상 이동한다.
        //
        // 전투 씬에 다시 들어가지 않고,
        // 마커 위치만 변경한 뒤 월드맵에서 계속 조작할 수 있도록 한다.
        if (isTargetNodeCleared)
        {
            isMovingMarker =
                false;

            // 클리어된 노드에 도착해 월드맵에 계속 남는 경우
            // 카메라 추적을 종료하고 줌·드래그 조작을 다시 허용한다.
            if (worldMapCameraController != null)
            {
                worldMapCameraController
                    .SetMarkerFollow(
                        null,
                        false
                    );
            }

            // 클리어된 노드로 이동한 경우
            // 해당 노드를 기준으로 새로 연결된 미탐사 노드와 길을 표시한다.
            RefreshFogForCurrentNode();

            Debug.Log(
                $"클리어 노드 이동 완료: " +
                $"{targetNode.GetNodeDisplayName()}"
            );

            yield break;
        }

        // 미클리어 노드에 진입할 때
        // 이동할 Scene과 해당 노드의 StageBattleData를 함께 가져온다.
        string targetSceneName =
            targetNode.GetTargetSceneName();

        StageBattleData targetStageBattleData =
            targetNode.GetStageBattleData();

        // Battle / BossBattle 노드는
        // 실제 전투 StageBattleData가 반드시 필요하다.
        //
        // Event / Shop 등은 StageBattleData 없이
        // 기존 Scene 이동만 사용할 수 있다.
        MapNodeType targetNodeType =
            targetNode.GetNodeType();

        bool requiresBattleStageData =
            targetNodeType == MapNodeType.Battle ||
            targetNodeType == MapNodeType.BossBattle;

        if (requiresBattleStageData &&
            targetStageBattleData == null)
        {
            Debug.LogWarning(
                $"전투 노드 진입 실패: " +
                $"{targetNode.GetNodeDisplayName()} 노드에 " +
                $"Stage Battle Data가 연결되지 않았습니다."
            );

            isMovingMarker =
                false;

            if (worldMapCameraController != null)
            {
                worldMapCameraController
                    .SetMarkerFollow(
                        null,
                        false
                    );
            }

            yield break;
        }

        // 아직 클리어하지 않은 노드의 ID와
        // 해당 노드에서 사용할 StageBattleData를
        // 다음 씬에서 사용할 런타임 상태로 저장한다.
        WorldMapRuntimeState.BeginBattleNode(
            targetNodeId,
            targetStageBattleData
        );

        // 새로운 노드에 도착했을 때만
        // Select → Absorb → Down_Absorb 애니메이션을 재생한다.
        yield return
            PlayMarkerNodeEnterAnimationRoutine();

        if (waitBeforeSceneMove >
            0f)
        {
            yield return
                new WaitForSeconds(
                    waitBeforeSceneMove
                );
        }

        if (string.IsNullOrWhiteSpace(
        targetSceneName))
        {
            Debug.LogWarning(
                $"전투 씬 이동 실패: " +
                $"{targetNode.GetNodeDisplayName()} 노드의 " +
                $"Target Scene Name이 비어 있습니다."
            );

            isMovingMarker =
                false;

            // 씬 이동이 취소되어 월드맵에 남으므로
            // 카메라 추적과 입력 잠금을 반드시 해제한다.
            if (worldMapCameraController != null)
            {
                worldMapCameraController
                    .SetMarkerFollow(
                        null,
                        false
                    );
            }

            yield break;
        }

        // 이번 씬 전환은 Player Marker가 마지막으로 내려찍은 위치를
        // 중심으로 둥글게 소용돌이치며 퍼져 나가야 하므로,
        // 마커를 숨기기 전에 중심 좌표를 먼저 저장한다.
        Vector3 sceneTransitionCenterWorldPosition =
            playerMarker != null
                ? playerMarker.position
                : targetNode.transform.position;

        // 포그가 아직 완전한 검정이 아닌 동안에는
        // Player Marker가 포그 아래로 가려지지 않도록
        // Fog Tilemap보다 Order in Layer를 1 높게 설정한다.
        //
        // 마커는 포그가 완전히 닫힌 뒤에만 비활성화한다.
        SetPlayerMarkerAboveFog();

        // 맵 시작 연출의 역재생처럼 보이도록
        // 카메라 축소와 포그 수축을 같은 시간에 동시에 실행한다.
        float sceneCloseDuration =
            0.9f;

        Coroutine cameraCloseCoroutine =
            null;

        Coroutine fogCloseCoroutine =
            null;

        if (worldMapCameraController != null &&
            playerMarker != null)
        {
            cameraCloseCoroutine =
                StartCoroutine(
                    worldMapCameraController
                        .PlayMapCloseZoomRoutine(
                            playerMarker,
                            sceneCloseDuration
                        )
                );
        }

        if (worldMapFogController != null)
        {
            fogCloseCoroutine =
                StartCoroutine(
                    worldMapFogController
                        .PlaySceneCloseTransition(
                            sceneTransitionCenterWorldPosition,
                            sceneCloseDuration
                        )
                );
        }

        // 두 연출이 모두 끝난 다음에만 전투 씬을 불러온다.
        if (fogCloseCoroutine != null)
        {
            yield return
                fogCloseCoroutine;
        }

        // 포그가 완전히 검게 닫힌 다음에만
        // Player Marker를 숨긴다.
        //
        // 전환 중에는 마커가 포그 위에 보이고,
        // 완전한 검은 화면이 된 뒤 자연스럽게 사라진다.
        if (playerMarker != null)
        {
            playerMarker.gameObject.SetActive(
                false
            );
        }

        // 씬 전환이 취소되거나 현재 오브젝트가 유지될 가능성에 대비해
        // 변경했던 Sorting Layer와 Order 값을 원래 상태로 복원한다.
        RestorePlayerMarkerSorting();

        yield return null;

        Debug.Log(
            $"월드맵 노드 도착: " +
            $"{targetNode.GetNodeDisplayName()} → " +
            $"{targetSceneName}"
        );

        SceneManager.LoadScene(
            targetSceneName
        );
    }

    // 포그 전환이 진행되는 동안
    // Player Marker를 Fog Tilemap보다 위에 표시한다.
    //
    // 포그와 동일한 Sorting Layer를 사용하고,
    // Order in Layer만 포그보다 1 높게 적용한다.
    private void SetPlayerMarkerAboveFog()
    {
        if (playerMarkerRenderer == null ||
            worldMapFogController == null)
        {
            return;
        }

        // 최초 한 번만 기존 정렬값을 저장하여
        // 전환이 끝난 뒤 원래 상태로 되돌릴 수 있게 한다.
        if (hasStoredOriginalMarkerSorting ==
            false)
        {
            originalMarkerSortingLayerId =
                playerMarkerRenderer.sortingLayerID;

            originalMarkerSortingOrder =
                playerMarkerRenderer.sortingOrder;

            hasStoredOriginalMarkerSorting =
                true;
        }

        playerMarkerRenderer.sortingLayerID =
            worldMapFogController
                .GetFogSortingLayerId();

        playerMarkerRenderer.sortingOrder =
            worldMapFogController
                .GetFogSortingOrder() +
            1;
    }

    // 포그 전환이 끝난 뒤
    // Player Marker의 원래 Sorting Layer와 Order를 복원한다.
    private void RestorePlayerMarkerSorting()
    {
        if (playerMarkerRenderer == null ||
            hasStoredOriginalMarkerSorting ==
            false)
        {
            return;
        }

        playerMarkerRenderer.sortingLayerID =
            originalMarkerSortingLayerId;

        playerMarkerRenderer.sortingOrder =
            originalMarkerSortingOrder;

        hasStoredOriginalMarkerSorting =
            false;
    }

    // 검은 구체가 전투 노드에 도착했을 때
    // Select → Absorb → Down_Absorb 순서로 애니메이션을 재생한다.
    //
    // 모든 애니메이션은 루프 없이 1회 재생하며,
    // 마지막 Down_Absorb가 끝난 다음 전투 씬으로 이동한다.
    private IEnumerator PlayMarkerNodeEnterAnimationRoutine()
    {
        if (playerMarkerSkeletonAnimation == null)
        {
            Debug.LogWarning(
                "Player Marker 도착 애니메이션 생략: " +
                "SkeletonAnimation이 연결되지 않았습니다."
            );

            yield break;
        }

        // 이전에 재생 중인 트랙과 대기 애니메이션을 제거하여
        // 도착 연출이 기존 애니메이션과 겹치지 않도록 한다.
        playerMarkerSkeletonAnimation.AnimationState
            .ClearTrack(
                0
            );

        // 목적지 노드를 선택하는 연출
        yield return
            PlayMarkerAnimationOnceRoutine(
                markerSelectAnimationName
            );

        if (markerAnimationInterval >
            0f)
        {
            yield return
                new WaitForSeconds(
                    markerAnimationInterval
                );
        }

        // 노드 또는 스테이지를 흡수하는 연출
        yield return
            PlayMarkerAnimationOnceRoutine(
                markerAbsorbAnimationName
            );

        if (markerAnimationInterval >
            0f)
        {
            yield return
                new WaitForSeconds(
                    markerAnimationInterval
                );
        }

        // 스테이지 진입 직전 마무리 흡수 연출
        yield return
            PlayMarkerAnimationOnceRoutine(
                markerDownAbsorbAnimationName
            );

        Debug.Log(
            "Player Marker 노드 도착 애니메이션 완료: " +
            $"{markerSelectAnimationName} → " +
            $"{markerAbsorbAnimationName} → " +
            $"{markerDownAbsorbAnimationName}"
        );
    }

    // 지정한 Spine 애니메이션을 0번 트랙에서
    // 루프 없이 한 번 재생하고 종료될 때까지 기다린다.
    private IEnumerator PlayMarkerAnimationOnceRoutine(
        string animationName)
    {
        if (playerMarkerSkeletonAnimation == null ||
            string.IsNullOrWhiteSpace(
                animationName))
        {
            yield break;
        }

        Spine.Animation animation =
            playerMarkerSkeletonAnimation
                .SkeletonDataAsset
                .GetSkeletonData(
                    true
                )
                .FindAnimation(
                    animationName
                );

        if (animation == null)
        {
            Debug.LogWarning(
                $"Player Marker 애니메이션 재생 실패: " +
                $"{animationName} 애니메이션을 찾지 못했습니다."
            );

            yield break;
        }

        TrackEntry trackEntry =
            playerMarkerSkeletonAnimation
                .AnimationState
                .SetAnimation(
                    0,
                    animationName,
                    false
                );

        if (trackEntry == null)
        {
            Debug.LogWarning(
                $"Player Marker 애니메이션 재생 실패: " +
                $"{animationName} TrackEntry를 생성하지 못했습니다."
            );

            yield break;
        }

        // Spine 애니메이션의 실제 길이를 기준으로 기다리므로
        // 애니메이션 길이가 달라져도 코드 시간을 다시 수정할 필요가 없다.
        float animationDuration =
            Mathf.Max(
                0f,
                animation.Duration
            );

        float elapsedTime =
            0f;

        while (elapsedTime <
               animationDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }
    }

    // 검은 구체를 지정한 월드 위치까지
    // 일정한 속도로 부드럽게 이동한다.
    //
    // 이동 중 새 Grid 셀을 지날 때마다
    // 해당 셀과 주변 포그를 영구 탐사 상태로 전환한다.
    private IEnumerator MoveMarkerToPositionRoutine(
        Vector3 targetPosition)
    {
        while (Vector3.Distance(
                   playerMarker.position,
                   targetPosition) >
               0.001f)
        {
            playerMarker.position =
                Vector3.MoveTowards(
                    playerMarker.position,
                    targetPosition,
                    markerMoveSpeed *
                    Time.deltaTime
                );

            if (worldMapFogController != null)
            {
                worldMapFogController
                    .TrackMarkerWorldPosition(
                        playerMarker.position
                    );
            }

            yield return null;
        }

        playerMarker.position =
            targetPosition;

        // 마지막 위치도 정확하게 탐사 처리하여
        // 프레임 이동량 때문에 목적지 셀이 빠지지 않도록 한다.
        if (worldMapFogController != null)
        {
            worldMapFogController
                .TrackMarkerWorldPosition(
                    playerMarker.position
                );
        }
    }

    // 현재 탐사 완료 노드를 중심으로
    // Connections에 등록된 미탐사 노드와
    // 해당 Route Grid 길을 Preview 상태로 표시한다.
    private void RefreshFogForCurrentNode()
    {
        if (worldMapFogController == null)
        {
            return;
        }

        string currentNodeId =
            WorldMapRuntimeState.CurrentNodeId;

        if (string.IsNullOrWhiteSpace(
                currentNodeId))
        {
            return;
        }

        MapNodeRuntime currentNode =
            worldMapBuilder.GetGeneratedNode(
                currentNodeId
            );

        if (currentNode == null)
        {
            return;
        }

        worldMapFogController
            .RevealExploredNodeArea(
                currentNode.transform.position
            );

        MapNodePlacementData currentPlacement =
            FindPlacementById(
                currentNodeId
            );

        if (currentPlacement == null)
        {
            return;
        }

        bool isCurrentNodeCleared =
            currentNode.IsCleared() ||
            currentPlacement.initiallyCleared ||
            WorldMapRuntimeState.IsNodeCleared(
                currentNodeId
            );

        if (isCurrentNodeCleared == false ||
            currentPlacement.connections == null)
        {
            return;
        }

        for (int i = 0;
             i < currentPlacement.connections.Count;
             i++)
        {
            MapNodeConnectionData connection =
                currentPlacement.connections[i];

            if (connection == null ||
                string.IsNullOrWhiteSpace(
                    connection.targetNodeId))
            {
                continue;
            }

            string connectedNodeId =
                connection.targetNodeId.Trim();

            MapNodeRuntime connectedNode =
                worldMapBuilder.GetGeneratedNode(
                    connectedNodeId
                );

            if (connectedNode == null)
            {
                continue;
            }

            if (connectedNode.IsCleared() ||
                WorldMapRuntimeState.IsNodeCleared(
                    connectedNodeId))
            {
                continue;
            }

            if (connectedNode.IsUnlocked() ==
                false)
            {
                continue;
            }

            // Connection 자체에 저장된 Route Grid 좌표를 사용해
            // 다음 탐사 후보 길과 목적지 노드를 Preview 처리한다.
            worldMapFogController
                .RevealPreviewRoute(
                    currentNode,
                    connectedNode,
                    connection,
                    false
                );
        }
    }

    // 클리어한 노드의 Connections에 등록된
    // 다음 목적지 노드들을 모두 해금한다.
    private void UnlockConnectedNodes(
        string clearedNodeId)
    {
        MapNodePlacementData clearedPlacement =
            FindPlacementById(
                clearedNodeId
            );

        if (clearedPlacement == null ||
            clearedPlacement.connections == null)
        {
            return;
        }

        for (int i = 0;
             i < clearedPlacement.connections.Count;
             i++)
        {
            MapNodeConnectionData connection =
                clearedPlacement.connections[i];

            if (connection == null ||
                string.IsNullOrWhiteSpace(
                    connection.targetNodeId))
            {
                continue;
            }

            WorldMapRuntimeState.UnlockNode(
                connection.targetNodeId.Trim()
            );
        }
    }

    // Node ID로 WorldMapData의
    // 배치 정보를 찾는다.
    private MapNodePlacementData FindPlacementById(
        string nodeId)
    {
        if (string.IsNullOrWhiteSpace(
                nodeId))
        {
            return null;
        }

        if (worldMapBuilder == null ||
            worldMapBuilder.WorldMapData == null ||
            worldMapBuilder.WorldMapData.nodePlacements == null)
        {
            return null;
        }

        // Inspector나 데이터 입력 중 들어간 앞뒤 공백 때문에
        // 같은 Node ID를 다른 값으로 인식하지 않도록 정리한다.
        string normalizedNodeId =
            nodeId.Trim();

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        for (int i = 0;
             i < mapData.nodePlacements.Count;
             i++)
        {
            MapNodePlacementData placement =
                mapData.nodePlacements[i];

            if (placement == null ||
                string.IsNullOrWhiteSpace(
                    placement.nodeId))
            {
                continue;
            }

            string normalizedPlacementNodeId =
                placement.nodeId.Trim();

            // 대소문자는 구분하되,
            // 앞뒤 공백을 제거한 실제 Node ID끼리 비교한다.
            if (string.Equals(
                    normalizedPlacementNodeId,
                    normalizedNodeId,
                    StringComparison.Ordinal))
            {
                return placement;
            }
        }

        return null;
    }
}
