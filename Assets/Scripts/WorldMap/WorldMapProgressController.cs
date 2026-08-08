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

    [Header("Player Marker Animation")]
    // 검은 구체 애니메이션을 재생하는 Spine SkeletonAnimation
    //
    // PlayerMarkerRoot의 자식인 실제 Spine 오브젝트를 연결한다.
    [SerializeField]
    private SkeletonAnimation playerMarkerSkeletonAnimation;

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

    [Header("Route")]
    // 발표용 노드 사이 이동 경로 목록
    //
    // 각 경로는 출발 노드, 도착 노드,
    // 실제로 따라갈 Waypoint Transform 목록으로 구성한다.
    [SerializeField]
    private List<WorldMapRouteData> routes =
        new List<WorldMapRouteData>();

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

        // 현재 탐사 완료 노드 주변을 완전히 밝히고,
        // 해당 노드와 연결된 미탐사 노드·길을 Preview 상태로 표시한다.
        RefreshFogForCurrentNode();
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

        // PlayerMarkerRoot만 Inspector에 연결된 경우,

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

        // 현재 노드의 Connected Node IDs에
        // 목적지 노드가 등록되어 있는지 확인한다.
        if (IsConnectedNode(
                currentNodeId,
                targetNodeId) ==
            false)
        {
            Debug.Log(
                $"노드 이동 불가: " +
                $"{currentNodeId}에서 {targetNodeId}로 연결된 길이 없습니다."
            );

            return;
        }

        WorldMapRouteData route;

        bool useReverseWaypoints;

        // 현재 이동 방향과 일치하는 Route를 찾는다.
        //
        // 역방향 이동이라면 기존 Route의 Waypoint를
        // 마지막부터 처음 순서로 사용한다.
        if (TryFindRoute(
                currentNodeId,
                targetNodeId,
                out route,
                out useReverseWaypoints) ==
            false)
        {
            Debug.LogWarning(
                $"노드 이동 실패: " +
                $"{currentNodeId} ↔ {targetNodeId} 경로 데이터가 없습니다."
            );

            return;
        }

        StartCoroutine(
            MoveMarkerToNodeRoutine(
                targetNode,
                route,
                useReverseWaypoints
            )
        );
    }

    // 두 노드 사이에 연결된 길이 존재하는지 검사한다.
    //
    // 월드맵의 Connected Node IDs는 진행 방향 기준으로
    // 한쪽 노드에만 등록되어 있어도 실제 길은 양방향으로 사용한다.
    private bool IsConnectedNode(
        string fromNodeId,
        string toNodeId)
    {
        MapNodePlacementData fromPlacement =
            FindPlacementById(
                fromNodeId
            );

        MapNodePlacementData toPlacement =
            FindPlacementById(
                toNodeId
            );

        // 현재 노드에서 목적지 노드로 직접 연결되어 있다면
        // 기존 진행 방향 이동을 허용한다.
        bool hasForwardConnection =
            fromPlacement != null &&
            fromPlacement.connectedNodeIds != null &&
            fromPlacement.connectedNodeIds.Contains(
                toNodeId
            );

        if (hasForwardConnection)
        {
            return true;
        }

        // 목적지 노드 쪽에서 현재 노드로 연결되어 있다면
        // 같은 길을 역방향으로 돌아가는 이동을 허용한다.
        bool hasReverseConnection =
            toPlacement != null &&
            toPlacement.connectedNodeIds != null &&
            toPlacement.connectedNodeIds.Contains(
                fromNodeId
            );

        return hasReverseConnection;
    }

    // 지정한 두 노드 사이의 Route를 찾고,
    // Waypoint를 정방향으로 사용할지 역방향으로 사용할지 반환한다.
    private bool TryFindRoute(
        string fromNodeId,
        string toNodeId,
        out WorldMapRouteData foundRoute,
        out bool useReverseWaypoints)
    {
        foundRoute =
            null;

        useReverseWaypoints =
            false;

        for (int i = 0;
             i < routes.Count;
             i++)
        {
            WorldMapRouteData route =
                routes[i];

            if (route == null)
            {
                continue;
            }

            // Route 데이터에 등록된 정방향 이동
            if (route.fromNodeId ==
                    fromNodeId &&
                route.toNodeId ==
                    toNodeId)
            {
                foundRoute =
                    route;

                useReverseWaypoints =
                    false;

                return true;
            }

            // 기존 Route의 출발·도착이 반대라면
            // Waypoint 순서를 뒤집어 같은 길을 역방향으로 사용한다.
            if (route.fromNodeId ==
                    toNodeId &&
                route.toNodeId ==
                    fromNodeId)
            {
                foundRoute =
                    route;

                useReverseWaypoints =
                    true;

                return true;
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
        WorldMapRouteData route,
        bool useReverseWaypoints)
    {
        isMovingMarker =
            true;

        if (route.waypoints != null)
        {
            if (useReverseWaypoints)
            {
                // 이전 클리어 노드로 돌아갈 때는
                // 기존 Waypoint 목록을 마지막부터 처음 순서로 따라간다.
                for (int i = route.waypoints.Count - 1;
                     i >= 0;
                     i--)
                {
                    Transform waypoint =
                        route.waypoints[i];

                    if (waypoint == null)
                    {
                        continue;
                    }

                    yield return
                        MoveMarkerToPositionRoutine(
                            waypoint.position
                        );
                }
            }
            else
            {
                // 새로운 노드 방향으로 이동할 때는
                // 기존 Waypoint 순서를 그대로 사용한다.
                for (int i = 0;
                     i < route.waypoints.Count;
                     i++)
                {
                    Transform waypoint =
                        route.waypoints[i];

                    if (waypoint == null)
                    {
                        continue;
                    }

                    yield return
                        MoveMarkerToPositionRoutine(
                            waypoint.position
                        );
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

            // 클리어된 노드로 이동한 경우
            // 해당 노드를 기준으로 새로 연결된 미탐사 노드와 길을 표시한다.
            RefreshFogForCurrentNode();

            Debug.Log(
                $"클리어 노드 이동 완료: " +
                $"{targetNode.GetNodeDisplayName()}"
            );

            yield break;
        }

        // 미클리어 노드에 진입할 때만
        // 전투 씬 이름을 가져오고 유효성을 검사한다.
        string targetSceneName =
            targetNode.GetTargetSceneName();

        // 아직 클리어하지 않은 노드만
        // 현재 전투 대상 노드로 등록한다.
        WorldMapRuntimeState.BeginBattleNode(
            targetNodeId
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

            yield break;
        }

        Debug.Log(
            $"월드맵 노드 도착: " +
            $"{targetNode.GetNodeDisplayName()} → " +
            $"{targetSceneName}"
        );

        SceneManager.LoadScene(
            targetSceneName
        );
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
    // 연결된 미탐사 노드와 해당 Route 길을 Preview 상태로 표시한다.
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

        // 현재 위치한 노드 주변은
        // 항상 완전히 탐사된 영역으로 처리한다.
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

        // 아직 클리어되지 않은 노드에서는
        // 연결된 다음 노드 Preview를 열지 않는다.
        bool isCurrentNodeCleared =
            currentNode.IsCleared() ||
            currentPlacement.initiallyCleared ||
            WorldMapRuntimeState.IsNodeCleared(
                currentNodeId
            );

        if (isCurrentNodeCleared == false ||
            currentPlacement.connectedNodeIds ==
                null)
        {
            return;
        }

        for (int i = 0;
             i <
             currentPlacement.connectedNodeIds.Count;
             i++)
        {
            string connectedNodeId =
                currentPlacement
                    .connectedNodeIds[i];

            MapNodeRuntime connectedNode =
                worldMapBuilder.GetGeneratedNode(
                    connectedNodeId
                );

            if (connectedNode == null)
            {
                continue;
            }

            // 이미 탐사 완료된 노드는
            // 기존 완전 탐사 포그 상태를 그대로 유지한다.
            if (connectedNode.IsCleared() ||
                WorldMapRuntimeState.IsNodeCleared(
                    connectedNodeId))
            {
                continue;
            }

            // 현재 진행도에서 해금되지 않은 먼 노드는
            // Preview 상태로도 공개하지 않는다.
            if (connectedNode.IsUnlocked() ==
                false)
            {
                continue;
            }

            WorldMapRouteData route;

            bool useReverseWaypoints;

            if (TryFindRoute(
                    currentNodeId,
                    connectedNodeId,
                    out route,
                    out useReverseWaypoints))
            {
                // 현재 노드에서 연결된 미탐사 노드까지
                // 실제 Route와 PathTilemap 길을 옅게 표시한다.
                worldMapFogController
                    .RevealPreviewRoute(
                        currentNode,
                        connectedNode,
                        route,
                        useReverseWaypoints
                    );
            }
            else
            {
                // Route 데이터가 빠져 있더라도
                // 연결된 목적지 노드의 위치는 Preview로 표시한다.
                worldMapFogController
                    .RevealPreviewNodeArea(
                        connectedNode
                            .transform
                            .position
                    );

                Debug.LogWarning(
                    $"포그 Route Preview 생략: " +
                    $"{currentNodeId} ↔ {connectedNodeId} " +
                    $"경로 데이터가 없습니다."
                );
            }
        }
    }

    // 클리어한 노드에 연결된 다음 노드들을
    // 모두 런타임 해금 상태로 등록한다.
    private void UnlockConnectedNodes(
        string clearedNodeId)
    {
        MapNodePlacementData clearedPlacement =
            FindPlacementById(
                clearedNodeId
            );

        if (clearedPlacement == null ||
            clearedPlacement.connectedNodeIds == null)
        {
            return;
        }

        for (int i = 0;
             i <
             clearedPlacement.connectedNodeIds.Count;
             i++)
        {
            string connectedNodeId =
                clearedPlacement
                    .connectedNodeIds[i];

            WorldMapRuntimeState.UnlockNode(
                connectedNodeId
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

// 발표용 노드 사이 이동 경로 데이터
//
// PathTilemap을 직접 길 찾기하지 않고
// Scene에 배치한 Waypoint를 순서대로 따라간다.
[Serializable]
public class WorldMapRouteData
{
    // 경로가 시작되는 노드 ID
    public string fromNodeId;

    // 경로가 끝나는 노드 ID
    public string toNodeId;

    // 검은 구체가 순서대로 통과할 경유점 목록
    public List<Transform> waypoints =
        new List<Transform>();
}
