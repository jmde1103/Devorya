using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Scene View의 Grid 셀을 클릭하여
// WorldMapData에 노드를 추가하거나 삭제하는 전용 에디터이다.
public class WorldMapNodeEditorWindow : EditorWindow
{
    // 현재 Scene에서 노드 생성과 삭제를 담당할 Builder
    [SerializeField]
    private WorldMapBuilder worldMapBuilder;

    [Header("Node Style Data")]
    // 일반 전투 노드 스타일
    [SerializeField]
    private MapNodeStyleData battleNodeStyle;

    // 보스 전투 노드 스타일
    [SerializeField]
    private MapNodeStyleData bossBattleNodeStyle;

    // 일반 이벤트 노드 스타일
    [SerializeField]
    private MapNodeStyleData eventNodeStyle;

    // 유적지 이벤트 노드 스타일
    [SerializeField]
    private MapNodeStyleData ruinsEventNodeStyle;

    // 클리어된 노드 스타일
    [SerializeField]
    private MapNodeStyleData clearedNodeStyle;

    // 상점 노드 스타일
    [SerializeField]
    private MapNodeStyleData shopNodeStyle;

    // 현재 배치할 노드 종류
    [SerializeField]
    private MapNodeType selectedNodeType =
        MapNodeType.Battle;

    // 새로 생성하는 노드의 표시 이름
    [SerializeField]
    private string newNodeDisplayName =
        "새 노드";

    // 새로 생성하는 노드가 이동할 씬 이름
    [SerializeField]
    private string newTargetSceneName =
        string.Empty;

    // 새로 생성하는 전투 노드에서 사용할 StageBattleData
    //
    // Battle / BossBattle 노드에서는 실제 전투 구성을 결정한다.
    [SerializeField]
    private StageBattleData newStageBattleData;

    // 새 노드의 초기 해금 상태
    [SerializeField]
    private bool newNodeInitiallyUnlocked;

    // 새 노드의 포그 해제 반경
    [SerializeField]
    [Min(0)]
    private int newNodeRevealRadius = 2;

    // Scene View 클릭 배치 기능이 활성화됐는지 여부
    [SerializeField]
    private bool isPlacementModeActive;

    // 현재 마우스가 올라간 Grid 셀
    private Vector2Int hoveredGridPosition;
    // 현재 마우스가 유효한 맵 Grid 위에 있는지 여부
    private bool hasValidHoveredCell;

    // 현재 선택하여 정보를 수정 중인 노드
    private MapNodePlacementData selectedPlacement;

    // 선택된 노드의 표시 이름을 수정하기 위한 임시 값
    private string editNodeDisplayName;

    // 선택된 노드의 타입을 수정하기 위한 임시 값
    private MapNodeType editNodeType =
        MapNodeType.Battle;

    // 선택된 노드의 Grid 좌표를 수정하기 위한 임시 값
    private Vector2Int editGridPosition;

    // 선택된 노드의 이동 대상 씬 이름을 수정하기 위한 임시 값
    private string editTargetSceneName;

    // 선택된 노드의 StageBattleData를 수정하기 위한 임시 값
    private StageBattleData editStageBattleData;

    // 선택된 노드의 초기 해금 상태를 수정하기 위한 임시 값
    private bool editInitiallyUnlocked;

    // 선택된 노드의 초기 클리어 상태를 수정하기 위한 임시 값
    private bool editInitiallyCleared;

    // 선택된 노드의 포그 공개 반경을 수정하기 위한 임시 값
    private int editRevealRadius = 2;

    // 긴 에디터 내용을 스크롤해서 확인할 수 있도록
    // 현재 스크롤 위치를 저장한다.
    private Vector2 editorScrollPosition;

    // Unity 상단 메뉴에서 에디터 창을 연다.
    [MenuItem("Window/Devorya/World Map Node Editor")]
    public static void OpenWindow()
    {
        WorldMapNodeEditorWindow window =
            GetWindow<WorldMapNodeEditorWindow>();

        window.titleContent =
            new GUIContent("World Map Node Editor");

        window.minSize =
            new Vector2(360f, 600f);

        window.Show();
    }

    private void OnEnable()
    {
        // 에디터 창이 활성화되면
        // Scene View의 입력과 그리기 이벤트를 구독한다.
        SceneView.duringSceneGui +=
            OnSceneGUI;
    }

    private void OnDisable()
    {
        // 에디터 창이 닫히거나 비활성화되면
        // Scene View 이벤트 구독을 해제한다.
        SceneView.duringSceneGui -=
            OnSceneGUI;

        hasValidHoveredCell =
            false;
    }

    private void OnGUI()
    {
        // 에디터 항목이 창 높이를 넘어가더라도
        // 아래 내용을 스크롤해서 사용할 수 있도록 한다.
        editorScrollPosition =
            EditorGUILayout.BeginScrollView(
                editorScrollPosition
            );

        EditorGUILayout.Space(6f);

        EditorGUILayout.LabelField(
            "월드맵 노드 배치 에디터",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "배치 모드에서 Scene View의 Grid를 조작합니다.\n" +
            "빈 셀 왼쪽 클릭: 새 노드 생성\n" +
            "기존 노드 왼쪽 클릭: 노드 선택\n" +
            "기존 노드 오른쪽 클릭: 노드 삭제",
            MessageType.Info
        );

        EditorGUILayout.Space(8f);

        DrawBuilderSection();

        EditorGUILayout.Space(10f);

        DrawNodeStyleSection();

        EditorGUILayout.Space(10f);

        DrawNewNodeSection();

        EditorGUILayout.Space(10f);

        DrawPlacementControls();

        EditorGUILayout.Space(10f);

        // 현재 선택된 노드의 정보를 수정하는 영역을 표시한다.
        DrawSelectedNodeSection();

        EditorGUILayout.Space(10f);

        DrawCurrentMapInformation();

        EditorGUILayout.EndScrollView();
    }

    // WorldMapBuilder 연결 영역을 표시한다.
    private void DrawBuilderSection()
    {
        EditorGUILayout.LabelField(
            "맵 Builder",
            EditorStyles.boldLabel
        );

        // Builder가 변경됐는지 확인하기 위해
        // 기존 참조를 먼저 저장한다.
        WorldMapBuilder previousBuilder =
            worldMapBuilder;

        worldMapBuilder =
            (WorldMapBuilder)EditorGUILayout.ObjectField(
                "World Map Builder",
                worldMapBuilder,
                typeof(WorldMapBuilder),
                true
            );

        // 다른 맵 Builder로 변경됐다면
        // 이전 맵의 노드 선택 정보를 제거한다.
        if (previousBuilder !=
            worldMapBuilder)
        {
            ClearSelectedPlacement();
        }

        if (worldMapBuilder == null)
        {
            if (GUILayout.Button(
                    "현재 선택 오브젝트에서 Builder 찾기"))
            {
                TryAssignBuilderFromSelection();
            }

            EditorGUILayout.HelpBox(
                "WorldMapManager에 연결된 " +
                "WorldMapBuilder를 넣어주세요.",
                MessageType.Warning
            );

            return;
        }

        if (worldMapBuilder.WorldMapData == null)
        {
            EditorGUILayout.HelpBox(
                "선택한 WorldMapBuilder에 " +
                "World Map Data가 연결되지 않았습니다.",
                MessageType.Error
            );
        }

        if (worldMapBuilder.MapGrid == null)
        {
            EditorGUILayout.HelpBox(
                "선택한 WorldMapBuilder에 " +
                "Map Grid가 연결되지 않았습니다.",
                MessageType.Error
            );
        }
    }

    // 노드 종류별 Style Data 연결 영역을 표시한다.
    private void DrawNodeStyleSection()
    {
        EditorGUILayout.LabelField(
            "노드 스타일",
            EditorStyles.boldLabel
        );

        battleNodeStyle =
            DrawStyleField(
                "일반 전투",
                battleNodeStyle
            );

        bossBattleNodeStyle =
            DrawStyleField(
                "보스 전투",
                bossBattleNodeStyle
            );

        eventNodeStyle =
            DrawStyleField(
                "일반 이벤트",
                eventNodeStyle
            );

        ruinsEventNodeStyle =
            DrawStyleField(
                "유적지 이벤트",
                ruinsEventNodeStyle
            );

        clearedNodeStyle =
            DrawStyleField(
                "노드 클리어",
                clearedNodeStyle
            );

        shopNodeStyle =
            DrawStyleField(
                "상점",
                shopNodeStyle
            );
    }

    // MapNodeStyleData 선택 필드를 그린다.
    private MapNodeStyleData DrawStyleField(
        string label,
        MapNodeStyleData currentValue)
    {
        return
            (MapNodeStyleData)EditorGUILayout.ObjectField(
                label,
                currentValue,
                typeof(MapNodeStyleData),
                false
            );
    }

    // 새로 생성할 노드의 기본 설정을 표시한다.
    private void DrawNewNodeSection()
    {
        EditorGUILayout.LabelField(
            "새 노드 설정",
            EditorStyles.boldLabel
        );

        selectedNodeType =
            (MapNodeType)EditorGUILayout.EnumPopup(
                "Node Type",
                selectedNodeType
            );

        newNodeDisplayName =
            EditorGUILayout.TextField(
                "Display Name",
                newNodeDisplayName
            );

        newTargetSceneName =
    EditorGUILayout.TextField(
        "Target Scene Name",
        newTargetSceneName
    );

        // Battle / BossBattle 노드에서 사용할
        // 실제 StageBattleData를 직접 선택한다.
        newStageBattleData =
            (StageBattleData)EditorGUILayout.ObjectField(
                "Stage Battle Data",
                newStageBattleData,
                typeof(StageBattleData),
                false
            );

        newNodeInitiallyUnlocked =
            EditorGUILayout.Toggle(
                        "Initially Unlocked",
                newNodeInitiallyUnlocked
            );

        newNodeRevealRadius =
            EditorGUILayout.IntField(
                "Reveal Radius",
                newNodeRevealRadius
            );

        newNodeRevealRadius =
            Mathf.Max(
                0,
                newNodeRevealRadius
            );

        if (selectedNodeType ==
            MapNodeType.Cleared)
        {
            EditorGUILayout.HelpBox(
                "Cleared 타입은 생성 시 " +
                "Initially Unlocked와 Initially Cleared가 " +
                "자동으로 활성화됩니다.",
                MessageType.Info
            );
        }
    }

    // 배치 모드 시작·종료 및 전체 갱신 버튼을 표시한다.
    private void DrawPlacementControls()
    {
        EditorGUILayout.LabelField(
            "배치 도구",
            EditorStyles.boldLabel
        );

        Color previousBackgroundColor =
            GUI.backgroundColor;

        GUI.backgroundColor =
            isPlacementModeActive
                ? new Color(1f, 0.65f, 0.45f)
                : new Color(0.65f, 1f, 0.65f);

        string placementButtonLabel =
            isPlacementModeActive
                ? "배치 모드 종료"
                : "배치 모드 시작";

        if (GUILayout.Button(
                placementButtonLabel,
                GUILayout.Height(36f)))
        {
            isPlacementModeActive =
                !isPlacementModeActive;

            // 배치 모드에서는 Unity 기본 Transform 도구가
            // 클릭을 가로채지 않도록 None 도구로 변경한다.
            if (isPlacementModeActive)
            {
                Tools.current =
                    Tool.None;
            }

            SceneView.RepaintAll();
        }

        GUI.backgroundColor =
            previousBackgroundColor;

        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(
                    "데이터 기준 노드 갱신"))
            {
                GenerateMapPreview();
            }

            if (GUILayout.Button(
                    "생성 노드 전체 삭제"))
            {
                ClearMapPreview();
            }
        }

        if (isPlacementModeActive)
        {
            EditorGUILayout.HelpBox(
                "현재 배치 모드가 활성화되어 있습니다.\n" +
                "왼쪽 클릭: 노드 생성\n" +
                "오른쪽 클릭: 노드 삭제",
                MessageType.Warning
            );
        }
    }

    // 현재 선택된 노드의 정보를 표시하고 수정한다.
    private void DrawSelectedNodeSection()
    {
        EditorGUILayout.LabelField(
            "선택 노드 수정",
            EditorStyles.boldLabel
        );

        if (selectedPlacement == null)
        {
            EditorGUILayout.HelpBox(
                "배치 모드에서 기존 노드가 있는 Grid 셀을 " +
                "왼쪽 클릭하면 노드를 선택할 수 있습니다.",
                MessageType.Info
            );

            return;
        }

        // Node ID는 연결 관계와 진행도 저장에서 사용되므로
        // 에디터에서 실수로 변경하지 못하도록 읽기 전용으로 표시한다.
        EditorGUI.BeginDisabledGroup(
            true
        );

        EditorGUILayout.TextField(
            "Node ID",
            selectedPlacement.nodeId
        );

        EditorGUI.EndDisabledGroup();

        editNodeDisplayName =
            EditorGUILayout.TextField(
                "Display Name",
                editNodeDisplayName
            );

        editNodeType =
            (MapNodeType)EditorGUILayout.EnumPopup(
                "Node Type",
                editNodeType
            );

        editGridPosition =
            EditorGUILayout.Vector2IntField(
                "Grid Position",
                editGridPosition
            );

        editTargetSceneName =
    EditorGUILayout.TextField(
        "Target Scene Name",
        editTargetSceneName
    );

        // 현재 선택된 전투 노드에 연결할
        // StageBattleData를 변경한다.
        editStageBattleData =
            (StageBattleData)EditorGUILayout.ObjectField(
                "Stage Battle Data",
                editStageBattleData,
                typeof(StageBattleData),
                false
            );

        editInitiallyUnlocked =
            EditorGUILayout.Toggle(
                        "Initially Unlocked",
                editInitiallyUnlocked
            );

        editInitiallyCleared =
            EditorGUILayout.Toggle(
                "Initially Cleared",
                editInitiallyCleared
            );

        editRevealRadius =
    EditorGUILayout.IntField(
        "Reveal Radius",
        editRevealRadius
    );

        EditorGUILayout.Space(6f);

        // 노드 연결과 Route는 이제 생성된 MapNodeRuntime Inspector에서
        // Target Node ID + Route Grid Positions를 한 세트로 관리한다.
        EditorGUILayout.HelpBox(
            "노드 연결과 Route는 생성된 MapNodeRuntime의 " +
            "Connections에서 설정합니다.",
            MessageType.Info
        );

        if (editNodeType ==
            MapNodeType.Cleared)
        {
            EditorGUILayout.HelpBox(
                "Cleared 타입으로 수정하면 " +
                "Initially Unlocked와 Initially Cleared가 " +
                "자동으로 활성화됩니다.",
                MessageType.Info
            );
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(
                    "선택 노드 수정 적용",
                    GUILayout.Height(30f)))
            {
                ApplySelectedNodeChanges();
            }

            if (GUILayout.Button(
                    "선택 해제",
                    GUILayout.Height(30f)))
            {
                ClearSelectedPlacement();

                SceneView.RepaintAll();
            }
        }
    }

    // 현재 연결된 맵 데이터의 기본 정보를 표시한다.
    private void DrawCurrentMapInformation()
    {
        if (worldMapBuilder == null ||
            worldMapBuilder.WorldMapData == null)
        {
            return;
        }

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        EditorGUILayout.LabelField(
            "현재 맵 정보",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "Map ID",
            mapData.mapId
        );

        EditorGUILayout.LabelField(
            "Map Name",
            mapData.mapDisplayName
        );

        EditorGUILayout.LabelField(
            "Grid Size",
            $"{mapData.gridWidth} × " +
            $"{mapData.gridHeight}"
        );

        EditorGUILayout.LabelField(
            "Node Count",
            mapData.nodePlacements.Count.ToString()
        );

        if (hasValidHoveredCell)
        {
            EditorGUILayout.LabelField(
                "현재 Grid 좌표",
                $"({hoveredGridPosition.x}, " +
                $"{hoveredGridPosition.y})"
            );
        }
    }

    // 현재 Hierarchy 선택 오브젝트에서
    // WorldMapBuilder를 자동으로 찾아 연결한다.
    private void TryAssignBuilderFromSelection()
    {
        if (Selection.activeGameObject == null)
        {
            return;
        }

        worldMapBuilder =
            Selection.activeGameObject
                .GetComponent<WorldMapBuilder>();

        if (worldMapBuilder == null)
        {
            worldMapBuilder =
                Selection.activeGameObject
                    .GetComponentInParent<WorldMapBuilder>();
        }

        // 새로운 Builder를 연결했으므로
        // 이전 맵의 노드 선택 상태를 초기화한다.
        ClearSelectedPlacement();

        Repaint();
    }

    // Scene View에서 Grid 위치 표시 및 마우스 입력을 처리한다.
    private void OnSceneGUI(
        SceneView sceneView)
    {
        if (isPlacementModeActive == false)
        {
            hasValidHoveredCell =
                false;

            return;
        }

        if (CanUsePlacementEditor() == false)
        {
            hasValidHoveredCell =
                false;

            return;
        }

        Event currentEvent =
            Event.current;

        // 마우스가 실제로 움직였을 때만
        // Scene View와 에디터 창을 다시 그린다.
        //
        // 매 OnSceneGUI 호출마다 Repaint하면
        // Scene View가 무한 갱신되어 에디터가 심하게 느려진다.
        if (currentEvent.type ==
            EventType.MouseMove)
        {
            sceneView.Repaint();
            Repaint();
        }

        // Scene View 마우스 위치에서
        // Z=0 평면상의 월드 좌표를 계산한다.
        Ray mouseRay =
            HandleUtility.GUIPointToWorldRay(
                currentEvent.mousePosition
            );

        Plane mapPlane =
            new Plane(
                Vector3.forward,
                Vector3.zero
            );

        float enterDistance;

        if (mapPlane.Raycast(
                mouseRay,
                out enterDistance) ==
            false)
        {
            hasValidHoveredCell =
                false;

            return;
        }

        Vector3 mouseWorldPosition =
            mouseRay.GetPoint(
                enterDistance
            );

        Vector3Int cellPosition =
            worldMapBuilder.MapGrid
                .WorldToCell(
                    mouseWorldPosition
                );

        hoveredGridPosition =
            new Vector2Int(
                cellPosition.x,
                cellPosition.y
            );

        hasValidHoveredCell =
            IsInsideMapGrid(
                hoveredGridPosition
            );

        if (hasValidHoveredCell)
        {
            DrawHoveredCell(
                cellPosition
            );
        }

        // 배치 모드에서 기본 Scene 선택 입력을 막는다.
        HandleUtility.AddDefaultControl(
            GUIUtility.GetControlID(
                FocusType.Passive
            )
        );

        if (hasValidHoveredCell == false)
        {
            return;
        }

        // 왼쪽 마우스 버튼을 눌렀을 때
        // 빈 셀이면 새 노드를 생성하고,
        // 기존 노드가 있는 셀이면 해당 노드를 선택한다.
        if (currentEvent.type ==
                EventType.MouseDown &&
            currentEvent.button == 0 &&
            currentEvent.alt == false)
        {
            HandlePlacementLeftClick(
                hoveredGridPosition
            );

            currentEvent.Use();

            // 노드 생성 또는 선택 결과를 즉시 표시한다.
            sceneView.Repaint();
            Repaint();

            return;
        }

        // 오른쪽 마우스 버튼을 눌렀을 때
        // 해당 Grid 셀에 있는 노드를 삭제한다.
        if (currentEvent.type ==
                EventType.MouseDown &&
            currentEvent.button == 1 &&
            currentEvent.alt == false)
        {
            RemoveNodeAtGrid(
                hoveredGridPosition
            );

            currentEvent.Use();

            // 노드 삭제 결과를 즉시 표시한다.
            sceneView.Repaint();
            Repaint();
        }
    }

    // 배치 모드에서 Scene View의 왼쪽 클릭을 처리한다.
    private void HandlePlacementLeftClick(
        Vector2Int gridPosition)
    {
        // 클릭한 Grid 셀에 이미 노드가 있는지 확인한다.
        MapNodePlacementData foundPlacement =
            FindNodeAtGrid(
                gridPosition
            );

        if (foundPlacement == null)
        {
            // 빈 셀이면 기존 방식대로 새 노드를 생성한다.
            AddNodeAtGrid(
                gridPosition
            );

            return;
        }

        // 기존 노드가 있는 셀이면
        // 새 노드를 생성하지 않고 해당 노드를 선택한다.
        SelectPlacement(
            foundPlacement
        );
    }

    // 현재 마우스가 올라간 Grid 셀을
    // Scene View에 사각형으로 표시한다.
    private void DrawHoveredCell(
        Vector3Int cellPosition)
    {
        Grid grid =
            worldMapBuilder.MapGrid;

        Vector3 cellCenter =
            grid.GetCellCenterWorld(
                cellPosition
            );

        Vector3 cellSize =
            grid.cellSize;

        Vector3 halfSize =
            new Vector3(
                cellSize.x * 0.5f,
                cellSize.y * 0.5f,
                0f
            );

        Vector3[] corners =
        {
            cellCenter +
            new Vector3(
                -halfSize.x,
                -halfSize.y,
                0f
            ),

            cellCenter +
            new Vector3(
                -halfSize.x,
                halfSize.y,
                0f
            ),

            cellCenter +
            new Vector3(
                halfSize.x,
                halfSize.y,
                0f
            ),

            cellCenter +
            new Vector3(
                halfSize.x,
                -halfSize.y,
                0f
            )
        };

        // 현재 마우스 셀에 존재하는 노드를 확인한다.
        MapNodePlacementData hoveredPlacement =
            FindNodeAtGrid(
                hoveredGridPosition
            );

        if (hoveredPlacement != null &&
            hoveredPlacement ==
            selectedPlacement)
        {
            // 현재 선택된 노드는 노란색 테두리로 표시한다.
            Handles.color =
                new Color(
                    1f,
                    0.9f,
                    0.2f,
                    1f
                );
        }
        else if (hoveredPlacement != null)
        {
            // 선택되지 않은 기존 노드는 빨간색으로 표시한다.
            Handles.color =
                new Color(
                    1f,
                    0.35f,
                    0.35f,
                    1f
                );
        }
        else
        {
            // 비어 있는 Grid 셀은 초록색으로 표시한다.
            Handles.color =
                new Color(
                    0.35f,
                    1f,
                    0.45f,
                    1f
                );
        }

        Handles.DrawAAPolyLine(
            3f,
            corners[0],
            corners[1],
            corners[2],
            corners[3],
            corners[0]
        );
    }

    // 선택한 Grid 셀에 새 노드 데이터를 추가한다.
    private void AddNodeAtGrid(
        Vector2Int gridPosition)
    {
        if (CanUsePlacementEditor() == false)
        {
            return;
        }

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        if (FindNodeAtGrid(
                gridPosition) != null)
        {
            Debug.LogWarning(
                $"노드 배치 실패: Grid " +
                $"({gridPosition.x}, {gridPosition.y})에는 " +
                $"이미 노드가 있습니다."
            );

            return;
        }

        MapNodeStyleData selectedStyle =
     GetNodeStyleByType(
         selectedNodeType
     );

        if (selectedStyle == null)
        {
            Debug.LogWarning(
                $"노드 배치 실패: " +
                $"{selectedNodeType} 타입의 " +
                $"Node Style Data가 연결되지 않았습니다."
            );

            return;
        }

        // ScriptableObject 변경을 Undo로 되돌릴 수 있게 기록한다.
        Undo.RecordObject(
            mapData,
            "Add World Map Node"
        );

        MapNodePlacementData newPlacement =
            new MapNodePlacementData();

        newPlacement.nodeId =
            GenerateUniqueNodeId(
                selectedNodeType
            );

        newPlacement.nodeDisplayName =
            string.IsNullOrWhiteSpace(
                newNodeDisplayName)
                ? newPlacement.nodeId
                : newNodeDisplayName;

        newPlacement.gridPosition =
            gridPosition;

        newPlacement.nodeType =
            selectedNodeType;

        newPlacement.nodeStyleData =
            selectedStyle;

        newPlacement.targetSceneName =
    newTargetSceneName;

        // 새 노드에 선택한 StageBattleData를 저장한다.
        //
        // 전투 노드는 이 데이터가 BattleScene까지 전달되며,
        // 전투가 아닌 노드는 null 상태도 허용한다.
        newPlacement.stageBattleData =
            newStageBattleData;

        // 클리어 노드는 시작 지점으로 사용할 수 있도록
        // 처음부터 해금·클리어 상태로 저장한다.
        bool isClearedNode =
            selectedNodeType ==
            MapNodeType.Cleared;

        newPlacement.initiallyUnlocked =
            isClearedNode ||
            newNodeInitiallyUnlocked;

        newPlacement.initiallyCleared =
            isClearedNode;

        newPlacement.revealRadius =
            newNodeRevealRadius;

        mapData.nodePlacements.Add(
            newPlacement
        );

        // 데이터 변경 사항을 Unity 에셋에 저장 대상으로 표시한다.
        EditorUtility.SetDirty(
            mapData
        );

        AssetDatabase.SaveAssets();

        // 새로 생성한 노드를 즉시 선택하여
        // 에디터 창에서 바로 정보를 수정할 수 있게 한다.
        SelectPlacement(
            newPlacement
        );

        // 변경된 데이터 기준으로 Scene 미리보기를 즉시 갱신한다.
        worldMapBuilder.GenerateMap();

        Debug.Log(
            $"월드맵 노드 배치 완료: " +
            $"{newPlacement.nodeId} / " +
            $"Grid ({gridPosition.x}, {gridPosition.y})"
        );
    }

    // 선택한 Grid 셀에 존재하는 노드를 삭제한다.
    private void RemoveNodeAtGrid(
        Vector2Int gridPosition)
    {
        if (CanUsePlacementEditor() == false)
        {
            return;
        }

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        MapNodePlacementData foundPlacement =
            FindNodeAtGrid(
                gridPosition
            );

        if (foundPlacement == null)
        {
            return;
        }

        Undo.RecordObject(
            mapData,
            "Remove World Map Node"
        );

        // 다른 노드의 연결 목록에 삭제 대상 ID가 남지 않도록
        // 모든 연결 데이터에서 해당 ID도 함께 제거한다.
        RemoveNodeIdFromConnections(
            foundPlacement.nodeId
        );

        mapData.nodePlacements.Remove(
     foundPlacement
 );

        // 현재 선택된 노드를 삭제했다면
        // 선택 정보와 수정용 임시 값도 함께 초기화한다.
        if (selectedPlacement ==
            foundPlacement)
        {
            ClearSelectedPlacement();
        }

        EditorUtility.SetDirty(
            mapData
        );

        AssetDatabase.SaveAssets();

        worldMapBuilder.GenerateMap();

        Debug.Log(
            $"월드맵 노드 삭제 완료: " +
            $"{foundPlacement.nodeId} / " +
            $"Grid ({gridPosition.x}, {gridPosition.y})"
        );
    }

    // 삭제되는 Node ID를 다른 노드들의
    // Connection 목록에서도 함께 제거한다.
    private void RemoveNodeIdFromConnections(
        string removedNodeId)
    {
        if (string.IsNullOrWhiteSpace(
                removedNodeId))
        {
            return;
        }

        List<MapNodePlacementData> placements =
            worldMapBuilder.WorldMapData
                .nodePlacements;

        for (int i = 0;
             i < placements.Count;
             i++)
        {
            MapNodePlacementData placement =
                placements[i];

            if (placement == null ||
                placement.connections == null)
            {
                continue;
            }

            placement.connections.RemoveAll(
                connection =>
                    connection != null &&
                    connection.targetNodeId ==
                        removedNodeId
            );
        }
    }

    // 지정한 노드를 선택하고
    // 현재 데이터 값을 수정용 임시 필드에 복사한다.
    private void SelectPlacement(
        MapNodePlacementData placement)
    {
        selectedPlacement =
            placement;

        LoadSelectedPlacementValues();

        Repaint();
        SceneView.RepaintAll();
    }

    // 선택된 노드의 현재 값을
    // 에디터 수정용 임시 필드에 복사한다.
    private void LoadSelectedPlacementValues()
    {
        if (selectedPlacement == null)
        {
            return;
        }

        editNodeDisplayName =
            selectedPlacement.nodeDisplayName;

        editNodeType =
            selectedPlacement.nodeType;

        editGridPosition =
            selectedPlacement.gridPosition;

        editTargetSceneName =
     selectedPlacement.targetSceneName;

        // 선택된 노드의 기존 StageBattleData를
        // 수정용 임시 값에 복사한다.
        editStageBattleData =
            selectedPlacement.stageBattleData;

        editInitiallyUnlocked =
            selectedPlacement.initiallyUnlocked;

        editInitiallyCleared =
            selectedPlacement.initiallyCleared;

        editRevealRadius =
     selectedPlacement.revealRadius;

       
    }

    // 현재 노드 선택과 수정용 임시 값을 초기화한다.
    private void ClearSelectedPlacement()
    {
        selectedPlacement =
            null;

        editNodeDisplayName =
            string.Empty;

        editNodeType =
            MapNodeType.Battle;

        editGridPosition =
            Vector2Int.zero;

        editTargetSceneName =
     string.Empty;

        // 이전에 선택했던 노드의
        // StageBattleData 참조가 새 선택에 남지 않도록 초기화한다.
        editStageBattleData =
            null;

        editInitiallyUnlocked =
            false;

        editInitiallyCleared =
            false;

        editRevealRadius =
     2;

        Repaint();
    }

    // 선택한 노드의 수정 내용을 WorldMapData에 적용한다.
    private void ApplySelectedNodeChanges()
    {
        if (selectedPlacement == null)
        {
            return;
        }

        if (CanUsePlacementEditor() == false)
        {
            return;
        }

        // 수정하려는 좌표가 현재 맵 범위 안인지 확인한다.
        if (IsInsideMapGrid(
                editGridPosition) == false)
        {
            Debug.LogWarning(
                $"노드 수정 실패: Grid " +
                $"({editGridPosition.x}, {editGridPosition.y})는 " +
                $"맵 범위를 벗어났습니다."
            );

            return;
        }

        // 수정하려는 좌표에 다른 노드가 이미 있는지 확인한다.
        MapNodePlacementData nodeAtTargetGrid =
            FindNodeAtGrid(
                editGridPosition
            );

        if (nodeAtTargetGrid != null &&
            nodeAtTargetGrid != selectedPlacement)
        {
            Debug.LogWarning(
                $"노드 수정 실패: Grid " +
                $"({editGridPosition.x}, {editGridPosition.y})에는 " +
                $"이미 다른 노드가 있습니다."
            );

            return;
        }

        // 수정된 노드 타입에 맞는 Style Data를 가져온다.
        MapNodeStyleData editedStyle =
            GetNodeStyleByType(
                editNodeType
            );

        if (editedStyle == null)
        {
            Debug.LogWarning(
                $"노드 수정 실패: " +
                $"{editNodeType} 타입의 " +
                $"Node Style Data가 연결되지 않았습니다."
            );

            return;
        }

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        // 노드 정보 변경을 Undo로 되돌릴 수 있도록 기록한다.
        Undo.RecordObject(
            mapData,
            "Edit World Map Node"
        );

        selectedPlacement.nodeDisplayName =
            string.IsNullOrWhiteSpace(
                editNodeDisplayName)
                ? selectedPlacement.nodeId
                : editNodeDisplayName;

        selectedPlacement.nodeType =
            editNodeType;

        selectedPlacement.nodeStyleData =
            editedStyle;

        selectedPlacement.gridPosition =
            editGridPosition;

        selectedPlacement.targetSceneName =
     editTargetSceneName;

        // 수정 UI에서 선택한 StageBattleData를
        // 실제 MapNodePlacementData에 저장한다.
        selectedPlacement.stageBattleData =
            editStageBattleData;

        bool isClearedNode =
            editNodeType ==
            MapNodeType.Cleared;

        // Cleared 타입은 시작 지점 또는 이미 완료된 노드이므로
        // 반드시 해금·클리어 상태로 저장한다.
        selectedPlacement.initiallyUnlocked =
            isClearedNode ||
            editInitiallyUnlocked;

        selectedPlacement.initiallyCleared =
            isClearedNode ||
            editInitiallyCleared;

        selectedPlacement.revealRadius =
    Mathf.Max(
        0,
        editRevealRadius
    );

        // Connection과 Route는 MapNodeRuntime Inspector에서
        // 별도로 수정하고 WorldMapData에 적용하므로
        // 월드맵 노드 에디터에서는 건드리지 않는다.

        EditorUtility.SetDirty(
            mapData
        );

        AssetDatabase.SaveAssets();

        // 자동 보정된 Cleared 상태 등을 수정 UI에도 다시 반영한다.
        LoadSelectedPlacementValues();

        // 변경된 좌표와 Sprite가 Scene에 즉시 반영되도록
        // 전체 노드 미리보기를 다시 생성한다.
        worldMapBuilder.GenerateMap();

        Debug.Log(
            $"월드맵 노드 수정 완료: " +
            $"{selectedPlacement.nodeId}"
        );

        SceneView.RepaintAll();
        Repaint();
    }

    // 지정한 Grid 셀에 배치된 노드 데이터를 찾는다.
    private MapNodePlacementData FindNodeAtGrid(
        Vector2Int gridPosition)
    {
        if (worldMapBuilder == null ||
            worldMapBuilder.WorldMapData == null)
        {
            return null;
        }

        List<MapNodePlacementData> placements =
            worldMapBuilder.WorldMapData
                .nodePlacements;

        for (int i = 0;
             i < placements.Count;
             i++)
        {
            MapNodePlacementData placement =
                placements[i];

            if (placement == null)
            {
                continue;
            }

            if (placement.gridPosition ==
                gridPosition)
            {
                return placement;
            }
        }

        return null;
    }

    // 지정한 노드 타입에 해당하는
    // MapNodeStyleData를 반환한다.
    private MapNodeStyleData GetNodeStyleByType(
        MapNodeType nodeType)
    {
        switch (nodeType)
        {
            case MapNodeType.Battle:
                return battleNodeStyle;

            case MapNodeType.BossBattle:
                return bossBattleNodeStyle;

            case MapNodeType.Event:
                return eventNodeStyle;

            case MapNodeType.RuinsEvent:
                return ruinsEventNodeStyle;

            case MapNodeType.Cleared:
                return clearedNodeStyle;

            case MapNodeType.Shop:
                return shopNodeStyle;

            default:
                return null;
        }
    }

    // 맵 ID, 노드 타입, 순번을 조합하여
    // 중복되지 않는 Node ID를 자동 생성한다.
    private string GenerateUniqueNodeId(
        MapNodeType nodeType)
    {
        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        string mapId =
            string.IsNullOrWhiteSpace(
                mapData.mapId)
                ? "WorldMap"
                : mapData.mapId;

        int sequence = 1;

        while (true)
        {
            string candidateId =
                $"{mapId}_{nodeType}_{sequence:00}";

            bool alreadyExists =
                false;

            for (int i = 0;
                 i < mapData.nodePlacements.Count;
                 i++)
            {
                MapNodePlacementData placement =
                    mapData.nodePlacements[i];

                if (placement != null &&
                    placement.nodeId ==
                    candidateId)
                {
                    alreadyExists =
                        true;

                    break;
                }
            }

            if (alreadyExists == false)
            {
                return candidateId;
            }

            sequence++;
        }
    }

    // 지정한 Grid 좌표가 현재 맵 범위 안인지 확인한다.
    private bool IsInsideMapGrid(
        Vector2Int gridPosition)
    {
        if (worldMapBuilder == null ||
            worldMapBuilder.WorldMapData == null)
        {
            return false;
        }

        WorldMapData mapData =
            worldMapBuilder.WorldMapData;

        return
            gridPosition.x >= 0 &&
            gridPosition.x <
                mapData.gridWidth &&
            gridPosition.y >= 0 &&
            gridPosition.y <
                mapData.gridHeight;
    }

    // 에디터 배치 기능에 필요한 참조가
    // 모두 준비됐는지 확인한다.
    private bool CanUsePlacementEditor()
    {
        return
            worldMapBuilder != null &&
            worldMapBuilder.WorldMapData != null &&
            worldMapBuilder.MapGrid != null &&
            worldMapBuilder.NodeRoot != null &&
            worldMapBuilder.MapNodePrefab != null;
    }

    // WorldMapData 기준으로
    // Scene의 노드 미리보기를 다시 생성한다.
    private void GenerateMapPreview()
    {
        if (worldMapBuilder == null)
        {
            return;
        }

        worldMapBuilder.GenerateMap();

        SceneView.RepaintAll();
    }

    // Scene의 NodeRoot 아래에 생성된
    // 모든 노드 미리보기를 삭제한다.
    private void ClearMapPreview()
    {
        if (worldMapBuilder == null)
        {
            return;
        }

        worldMapBuilder.ClearGeneratedNodes();

        SceneView.RepaintAll();
    }
}
