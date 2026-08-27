using System.Collections.Generic;
using UnityEngine;

// WorldMapData에 저장된 노드 배치 정보를 읽어
// 현재 맵 씬의 NodeRoot 아래에 노드 프리팹을 자동 생성한다.
public class WorldMapBuilder : MonoBehaviour
{
    [Header("Map Data")]
    // 현재 씬에서 생성할 월드맵 데이터
    [SerializeField]
    private WorldMapData worldMapData;

    [Header("Scene References")]
    // 16×16 맵 배치 좌표를 월드 좌표로 변환할 Grid
    [SerializeField]
    private Grid mapGrid;

    // 자동 생성된 노드가 들어갈 부모 Transform
    [SerializeField]
    private Transform nodeRoot;

    // 모든 노드가 공통으로 사용하는 기본 프리팹
    [SerializeField]
    private MapNodeRuntime mapNodePrefab;

    // 씬의 기본 맵 배경을 표시하는 SpriteRenderer
    [SerializeField]
    private SpriteRenderer baseMapRenderer;

    [Header("Generation")]
    // 플레이 모드 시작 시 자동으로 노드를 다시 생성할지 여부
    [SerializeField]
    private bool generateOnStart = true;

    // 생성된 노드를 Node ID로 검색하기 위한 런타임 목록
    private readonly Dictionary<string, MapNodeRuntime>
        generatedNodesById =
            new Dictionary<string, MapNodeRuntime>();

    // 맵 노드 에디터가 현재 연결된 WorldMapData를 확인할 때 사용한다.
    public WorldMapData WorldMapData
    {
        get { return worldMapData; }
    }

    // 맵 노드 에디터가 월드 좌표와 Grid 좌표를 변환할 때 사용한다.
    public Grid MapGrid
    {
        get { return mapGrid; }
    }

    // 맵 노드 에디터가 생성된 노드의 부모를 확인할 때 사용한다.
    public Transform NodeRoot
    {
        get { return nodeRoot; }
    }

    // 맵 노드 에디터가 공통 노드 프리팹을 확인할 때 사용한다.
    public MapNodeRuntime MapNodePrefab
    {
        get { return mapNodePrefab; }
    }

    // 맵 노드 에디터가 현재 배경 Renderer를 확인할 때 사용한다.
    public SpriteRenderer BaseMapRenderer
    {
        get { return baseMapRenderer; }
    }

    private void Awake()
    {
        // 모바일에서도 목표 프레임을 60FPS로 설정한다.
        Application.targetFrameRate = 60;

        // 모바일에서는 VSync 대신 targetFrameRate를 사용한다.
        QualitySettings.vSyncCount = 0;
    }

    private void Start()
    {
        // 자동 생성 옵션이 켜져 있으면
        // 현재 WorldMapData를 기준으로 맵을 구성한다.
        if (generateOnStart)
        {
            GenerateMap();
        }
    }

    // 배경과 노드를 현재 WorldMapData 기준으로 다시 생성한다.
    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        // 기존에 생성된 노드를 먼저 제거하여
        // 중복 노드가 쌓이지 않도록 한다.
        ClearGeneratedNodes();

        if (ValidateRequiredReferences() == false)
        {
            return;
        }

        // 데이터에 등록된 배경 Sprite를 씬의 BaseMap에 적용한다.
        ApplyBackgroundSprite();

        // 데이터에 저장된 모든 노드 배치 정보를 순서대로 생성한다.
        for (int i = 0;
             i < worldMapData.nodePlacements.Count;
             i++)
        {
            MapNodePlacementData placementData =
                worldMapData.nodePlacements[i];

            GenerateNode(
                placementData,
                i
            );
        }

        Debug.Log(
            $"월드맵 생성 완료: " +
            $"{worldMapData.mapDisplayName} / " +
            $"노드 {generatedNodesById.Count}개"
        );
    }

    // 월드맵을 생성하는 데 필요한 참조가 모두 연결됐는지 검사한다.
    private bool ValidateRequiredReferences()
    {
        if (worldMapData == null)
        {
            Debug.LogWarning(
                "월드맵 생성 실패: World Map Data가 연결되지 않았습니다."
            );

            return false;
        }

        if (mapGrid == null)
        {
            Debug.LogWarning(
                "월드맵 생성 실패: Map Grid가 연결되지 않았습니다."
            );

            return false;
        }

        if (nodeRoot == null)
        {
            Debug.LogWarning(
                "월드맵 생성 실패: Node Root가 연결되지 않았습니다."
            );

            return false;
        }

        if (mapNodePrefab == null)
        {
            Debug.LogWarning(
                "월드맵 생성 실패: Map Node Prefab이 연결되지 않았습니다."
            );

            return false;
        }

        return true;
    }

    // WorldMapData에 등록된 배경 Sprite를 BaseMap에 적용한다.
    private void ApplyBackgroundSprite()
    {
        if (baseMapRenderer == null)
        {
            return;
        }

        if (worldMapData.backgroundSprite == null)
        {
            Debug.LogWarning(
                "월드맵 배경 적용 생략: " +
                "WorldMapData의 Background Sprite가 비어 있습니다."
            );

            return;
        }

        baseMapRenderer.sprite =
            worldMapData.backgroundSprite;
    }

    // 노드 배치 데이터 하나를 실제 노드 GameObject로 생성한다.
    private void GenerateNode(
        MapNodePlacementData placementData,
        int placementIndex)
    {
        if (placementData == null)
        {
            Debug.LogWarning(
                $"월드맵 노드 생성 생략: " +
                $"Node Placements의 Element {placementIndex}가 비어 있습니다."
            );

            return;
        }

        // Grid 범위를 벗어난 좌표는 생성하지 않는다.
        if (IsInsideMapGrid(
                placementData.gridPosition) ==
            false)
        {
            Debug.LogWarning(
                $"월드맵 노드 생성 실패: " +
                $"{placementData.nodeId}의 좌표 " +
                $"({placementData.gridPosition.x}, " +
                $"{placementData.gridPosition.y})가 " +
                $"맵 Grid 범위를 벗어났습니다."
            );

            return;
        }

        // 노드 ID가 비어 있으면 진행도와 연결 관계를 관리할 수 없다.
        if (string.IsNullOrWhiteSpace(
                placementData.nodeId))
        {
            Debug.LogWarning(
                $"월드맵 노드 생성 실패: " +
                $"Node Placements의 Element {placementIndex}에 " +
                $"Node ID가 없습니다."
            );

            return;
        }

        // 같은 Node ID가 이미 생성됐다면 중복 생성하지 않는다.
        if (generatedNodesById.ContainsKey(
                placementData.nodeId))
        {
            Debug.LogWarning(
                $"월드맵 노드 생성 실패: " +
                $"중복된 Node ID가 존재합니다. " +
                $"{placementData.nodeId}"
            );

            return;
        }

        // MapNodePlacementData의 2D Grid 좌표를
        // Unity Grid가 사용하는 3D 셀 좌표로 변환한다.
        Vector3Int cellPosition =
            new Vector3Int(
                placementData.gridPosition.x,
                placementData.gridPosition.y,
                0
            );

        // 해당 Grid 셀의 정확한 월드 중심 위치를 가져온다.
        Vector3 nodeWorldPosition =
            mapGrid.GetCellCenterWorld(
                cellPosition
            );

        // 공통 노드 프리팹을 NodeRoot 아래에 생성한다.
        MapNodeRuntime createdNode =
            Instantiate(
                mapNodePrefab,
                nodeWorldPosition,
                Quaternion.identity,
                nodeRoot
            );

        // Hierarchy에서 노드의 위치와 ID를 쉽게 확인할 수 있도록
        // 생성된 GameObject 이름에 Node ID와 좌표를 표시한다.
        createdNode.gameObject.name =
            $"MapNode_" +
            $"{placementData.nodeId}_" +
            $"({placementData.gridPosition.x}," +
            $"{placementData.gridPosition.y})";

        // 배치 데이터의 ID, 표시 이름, 스타일,
        // 이동할 씬 이름, 실제 전투 StageBattleData,
        // 연결된 다음 Node ID 목록,
        // 초기 해금 상태를 런타임 노드에 전달한다.
        // 배치 데이터에 저장된 실제 Node Type과
        // 현재 표시용 Style Data를 각각 Runtime 노드에 전달한다.
        //
        // Node Type = 게임플레이 역할
        // Style Data = Sprite / Color / Collider 등 시각 표현
        createdNode.Initialize(
            placementData.nodeId,
            placementData.nodeDisplayName,
            placementData.nodeType,
            placementData.nodeStyleData,
            placementData.targetSceneName,
            placementData.stageBattleData,

            // 연결 대상 Node ID와 Route Grid 좌표를
            // 한 세트로 Runtime 노드에 전달한다.
            placementData.connections,

            placementData.initiallyUnlocked
        );

        // 시작 지점처럼 처음부터 클리어된 노드는
        // 배치 데이터의 초기 상태를 그대로 적용한다.
        createdNode.SetCleared(
            placementData.initiallyCleared
        );

        // 이후 ID 검색과 노드 연결 처리에 사용할 수 있도록 등록한다.
        generatedNodesById.Add(
            placementData.nodeId,
            createdNode
        );
    }

    // 지정한 Grid 좌표가 현재 WorldMapData의
    // 가로·세로 범위 안에 있는지 확인한다.
    private bool IsInsideMapGrid(
        Vector2Int gridPosition)
    {
        if (worldMapData == null)
        {
            return false;
        }

        return
            gridPosition.x >= 0 &&
            gridPosition.x <
                worldMapData.gridWidth &&
            gridPosition.y >= 0 &&
            gridPosition.y <
                worldMapData.gridHeight;
    }

    // 이전에 자동 생성된 모든 노드를 제거한다.
    [ContextMenu("Clear Generated Nodes")]
    public void ClearGeneratedNodes()
    {
        generatedNodesById.Clear();

        if (nodeRoot == null)
        {
            return;
        }

        // 뒤에서부터 제거하여
        // 자식 인덱스가 변경되어도 안전하게 처리한다.
        for (int i = nodeRoot.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                nodeRoot.GetChild(i);

            if (child == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                // 플레이 모드에서는 Destroy를 사용한다.
                Destroy(
                    child.gameObject
                );
            }
            else
            {
                // 에디터 모드에서 Context Menu로 실행할 때는
                // 즉시 삭제하여 Scene에 잔여 오브젝트가 남지 않게 한다.
                DestroyImmediate(
                    child.gameObject
                );
            }
        }
    }

    // 다른 맵 시스템이 Node ID로 생성된 노드를 찾을 때 사용한다.
    public MapNodeRuntime GetGeneratedNode(
        string nodeId)
    {
        if (string.IsNullOrWhiteSpace(
                nodeId))
        {
            return null;
        }

        MapNodeRuntime foundNode;

        if (generatedNodesById.TryGetValue(
                nodeId,
                out foundNode))
        {
            return foundNode;
        }

        return null;
    }

    // MapNodeRuntime Inspector에서 수정한
    // 연결 대상 Node ID와 Route Grid 좌표를
    // 같은 Node ID를 가진 원본 WorldMapData에 함께 저장한다.
    //
    // 해금·클리어 같은 런타임 진행 상태는
    // 원본 데이터에 저장하지 않는다.
    public bool ApplyConnectionsFromRuntime(
        MapNodeRuntime runtimeNode)
    {
        if (runtimeNode == null)
        {
            Debug.LogWarning(
                "노드 연결 정보 적용 실패: " +
                "MapNodeRuntime이 없습니다."
            );

            return false;
        }

        if (worldMapData == null ||
            worldMapData.nodePlacements == null)
        {
            Debug.LogWarning(
                "노드 연결 정보 적용 실패: " +
                "WorldMapData가 연결되지 않았습니다."
            );

            return false;
        }

        string targetNodeId =
            runtimeNode.GetNodeId();

        if (string.IsNullOrWhiteSpace(
                targetNodeId))
        {
            Debug.LogWarning(
                "노드 연결 정보 적용 실패: " +
                "Node ID가 비어 있습니다."
            );

            return false;
        }

        MapNodePlacementData foundPlacement =
            null;

        // 같은 Node ID를 가진 원본 노드 데이터를 찾는다.
        for (int i = 0;
             i < worldMapData.nodePlacements.Count;
             i++)
        {
            MapNodePlacementData placement =
                worldMapData.nodePlacements[i];

            if (placement == null ||
                string.IsNullOrWhiteSpace(
                    placement.nodeId))
            {
                continue;
            }

            if (placement.nodeId.Trim() !=
                targetNodeId.Trim())
            {
                continue;
            }

            foundPlacement =
                placement;

            break;
        }

        if (foundPlacement == null)
        {
            Debug.LogWarning(
                $"노드 연결 정보 적용 실패: " +
                $"{targetNodeId}에 해당하는 " +
                $"MapNodePlacementData를 찾지 못했습니다."
            );

            return false;
        }

        List<MapNodeConnectionData> runtimeConnections =
            runtimeNode.GetConnectionsCopy();

        // 기존 Connection 목록을 새 데이터로 교체한다.
        foundPlacement.connections =
            new List<MapNodeConnectionData>();

        for (int i = 0;
             i < runtimeConnections.Count;
             i++)
        {
            MapNodeConnectionData runtimeConnection =
                runtimeConnections[i];

            if (runtimeConnection == null ||
                string.IsNullOrWhiteSpace(
                    runtimeConnection.targetNodeId))
            {
                continue;
            }

            string normalizedConnectedNodeId =
                runtimeConnection.targetNodeId.Trim();

            // 자기 자신으로 연결되는 Connection은 저장하지 않는다.
            if (normalizedConnectedNodeId ==
                targetNodeId.Trim())
            {
                Debug.LogWarning(
                    $"노드 자기 연결 제외: " +
                    $"{targetNodeId}"
                );

                continue;
            }

            // 같은 목적지 Connection이 중복 저장되지 않도록 검사한다.
            bool alreadyAdded =
                false;

            for (int j = 0;
                 j < foundPlacement.connections.Count;
                 j++)
            {
                MapNodeConnectionData existingConnection =
                    foundPlacement.connections[j];

                if (existingConnection != null &&
                    existingConnection.targetNodeId ==
                        normalizedConnectedNodeId)
                {
                    alreadyAdded =
                        true;

                    break;
                }
            }

            if (alreadyAdded)
            {
                continue;
            }

            MapNodeConnectionData savedConnection =
                new MapNodeConnectionData();

            savedConnection.targetNodeId =
                normalizedConnectedNodeId;

            // Route Grid 좌표도 원본과 별도의 List로 복사한다.
            savedConnection.routeGridPositions =
                runtimeConnection.routeGridPositions != null
                    ? new List<Vector2Int>(
                        runtimeConnection.routeGridPositions
                    )
                    : new List<Vector2Int>();

            foundPlacement.connections.Add(
                savedConnection
            );
        }

#if UNITY_EDITOR
        // 에디터에서 변경한 WorldMapData를
        // 실제 ScriptableObject 에셋에 저장한다.
        UnityEditor.EditorUtility.SetDirty(
            worldMapData
        );

        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log(
            $"노드 연결/Route 적용 완료: " +
            $"{targetNodeId} / " +
            $"Connection {foundPlacement.connections.Count}개"
        );

        return true;
    }
}
