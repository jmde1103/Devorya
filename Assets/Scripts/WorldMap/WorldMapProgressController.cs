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
    [SerializeField, Min(0.01f)]
    private float markerMoveSpeed =
        3f;

    // 목적지에 도착한 뒤 전투 씬으로 이동하기 전 대기 시간
    [SerializeField, Min(0f)]
    private float waitBeforeSceneMove =
        0.25f;

    // 현재 검은 구체가 이동 중인지 확인한다.
    private bool isMovingMarker;

    // 월드맵 초기화 코루틴
    private Coroutine initializeCoroutine;

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
        // 진행도와 검은 구체 위치를 적용해야 하므로
        // 한 프레임 기다린 뒤 초기화한다.
        initializeCoroutine =
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

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

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

        ApplyRuntimeNodeStates();
        PlaceMarkerAtCurrentNode();

        initializeCoroutine =
            null;
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

            bool isUnlocked =
                placement.initiallyUnlocked ||
                WorldMapRuntimeState
                    .IsNodeUnlocked(
                        placement.nodeId
                    );

            bool isCleared =
                placement.initiallyCleared ||
                WorldMapRuntimeState
                    .IsNodeCleared(
                        placement.nodeId
                    );

            runtimeNode.SetUnlocked(
    isUnlocked
);

            // <변경부분> 런타임 클리어 상태와 함께
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

        if (targetNode.IsUnlocked() ==
            false)
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

        WorldMapRouteData route =
            FindRoute(
                currentNodeId,
                targetNodeId
            );

        if (route == null)
        {
            Debug.LogWarning(
                $"노드 이동 실패: " +
                $"{currentNodeId} → {targetNodeId} 경로 데이터가 없습니다."
            );

            return;
        }

        StartCoroutine(
            MoveMarkerToNodeRoutine(
                targetNode,
                route
            )
        );
    }

    // 현재 노드 데이터에서 목적지 노드가
    // 연결 목록에 등록되어 있는지 검사한다.
    private bool IsConnectedNode(
        string fromNodeId,
        string toNodeId)
    {
        MapNodePlacementData fromPlacement =
            FindPlacementById(
                fromNodeId
            );

        if (fromPlacement == null ||
            fromPlacement.connectedNodeIds == null)
        {
            return false;
        }

        return
            fromPlacement.connectedNodeIds
                .Contains(
                    toNodeId
                );
    }

    // 지정한 출발 노드와 도착 노드에 해당하는
    // 발표용 Waypoint 경로를 찾는다.
    private WorldMapRouteData FindRoute(
        string fromNodeId,
        string toNodeId)
    {
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

            if (route.fromNodeId ==
                    fromNodeId &&
                route.toNodeId ==
                    toNodeId)
            {
                return route;
            }
        }

        return null;
    }

    // 검은 구체가 Waypoint를 순서대로 지나
    // 목적지 노드에 도착한 뒤 전투 씬으로 이동한다.
    private IEnumerator MoveMarkerToNodeRoutine(
        MapNodeRuntime targetNode,
        WorldMapRouteData route)
    {
        isMovingMarker =
            true;

        if (route.waypoints != null)
        {
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

        // 마지막 Waypoint가 노드 중심과 정확히 일치하지 않더라도
        // 최종적으로 목적지 노드 중심에 검은 구체를 배치한다.
        yield return
            MoveMarkerToPositionRoutine(
                targetNode.transform.position
            );

        string targetNodeId =
            targetNode.GetNodeId();

        string targetSceneName =
            targetNode.GetTargetSceneName();

        WorldMapRuntimeState.SetCurrentNode(
    targetNodeId
);

        WorldMapRuntimeState.BeginBattleNode(
            targetNodeId
        );

        // 검은 구체가 목적지 노드에 도착한 뒤
        // Select → Absorb → Down_Absorb 순서로 재생한다.
        //
        // 모든 애니메이션 재생이 끝난 후에만
        // 실제 전투 씬으로 이동한다.
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

            yield return null;
        }

        playerMarker.position =
            targetPosition;
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

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        for (int i = 0;
             i < mapData.nodePlacements.Count;
             i++)
        {
            MapNodePlacementData placement =
                mapData.nodePlacements[i];

            if (placement != null &&
                placement.nodeId ==
                nodeId)
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
