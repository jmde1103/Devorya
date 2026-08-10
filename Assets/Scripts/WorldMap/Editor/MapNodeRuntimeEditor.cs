#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

// MapNodeRuntime의 기본 Inspector를 유지하면서
// Connection과 Route Grid 좌표를 Scene View에서
// 직접 편집할 수 있는 기능을 추가한다.
[CustomEditor(typeof(MapNodeRuntime))]
public class MapNodeRuntimeEditor : Editor
{
    // 현재 Scene View에서 Route를 편집 중인지 여부
    private bool isRouteEditMode;

    // 현재 편집 중인 Connection의 배열 인덱스
    private int routeEditConnectionIndex =
        -1;

    // Scene View에서 현재 마우스가 올라간 Grid 좌표
    private Vector2Int hoveredRouteGridPosition;

    // 현재 마우스가 정상적인 Grid 영역에 있는지 여부
    private bool hasValidHoveredRouteCell;

    private void OnEnable()
    {
        // Scene View에서 Route 좌표를 클릭할 수 있도록
        // Scene GUI 이벤트를 등록한다.
        SceneView.duringSceneGui +=
            OnSceneGUI;
    }

    private void OnDisable()
    {
        // Inspector가 닫히거나 다른 오브젝트를 선택하면
        // Route 편집 상태를 종료하고 이벤트를 해제한다.
        SceneView.duringSceneGui -=
            OnSceneGUI;

        isRouteEditMode =
            false;

        routeEditConnectionIndex =
            -1;

        hasValidHoveredRouteCell =
            false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 기존 MapNodeRuntime의 SerializeField는
        // Unity 기본 Inspector 방식으로 그대로 표시한다.
        DrawDefaultInspector();

        EditorGUILayout.Space(12f);

        DrawRouteEditorSection();

        EditorGUILayout.Space(12f);

        DrawWorldMapDataApplySection();

        serializedObject.ApplyModifiedProperties();
    }

    // Scene View Route 편집용 Inspector UI를 표시한다.
    private void DrawRouteEditorSection()
    {
        EditorGUILayout.LabelField(
            "Route Scene Editor",
            EditorStyles.boldLabel
        );

        SerializedProperty connectionsProperty =
            serializedObject.FindProperty(
                "connections"
            );

        if (connectionsProperty == null)
        {
            EditorGUILayout.HelpBox(
                "MapNodeRuntime의 Connections 필드를 찾지 못했습니다.",
                MessageType.Error
            );

            return;
        }

        if (connectionsProperty.arraySize <=
            0)
        {
            EditorGUILayout.HelpBox(
                "먼저 Connections에 연결할 노드를 추가해주세요.\n" +
                "Connection을 만든 뒤 Route를 Scene View에서 편집할 수 있습니다.",
                MessageType.Info
            );

            StopRouteEditMode();

            return;
        }

        // 현재 편집 인덱스가 배열 범위를 벗어나지 않도록 보정한다.
        if (routeEditConnectionIndex >=
            connectionsProperty.arraySize)
        {
            routeEditConnectionIndex =
                connectionsProperty.arraySize - 1;
        }

        if (routeEditConnectionIndex <
            0)
        {
            routeEditConnectionIndex =
                0;
        }

        string[] connectionLabels =
            new string[
                connectionsProperty.arraySize
            ];

        // Connection 번호와 Target Node ID를 함께 표시하여
        // 어느 Route를 편집하는지 쉽게 구분한다.
        for (int i = 0;
             i < connectionsProperty.arraySize;
             i++)
        {
            SerializedProperty connectionProperty =
                connectionsProperty
                    .GetArrayElementAtIndex(
                        i
                    );

            SerializedProperty targetNodeIdProperty =
                connectionProperty
                    .FindPropertyRelative(
                        "targetNodeId"
                    );

            string targetNodeId =
                targetNodeIdProperty != null
                    ? targetNodeIdProperty.stringValue
                    : string.Empty;

            if (string.IsNullOrWhiteSpace(
                    targetNodeId))
            {
                targetNodeId =
                    "Target 미설정";
            }

            connectionLabels[i] =
                $"Element {i} → {targetNodeId}";
        }

        routeEditConnectionIndex =
            EditorGUILayout.Popup(
                "Edit Connection",
                routeEditConnectionIndex,
                connectionLabels
            );

        SerializedProperty selectedConnectionProperty =
            connectionsProperty
                .GetArrayElementAtIndex(
                    routeEditConnectionIndex
                );

        SerializedProperty routeGridPositionsProperty =
            selectedConnectionProperty
                .FindPropertyRelative(
                    "routeGridPositions"
                );

        EditorGUILayout.LabelField(
            "Route Point Count",
            routeGridPositionsProperty != null
                ? routeGridPositionsProperty.arraySize.ToString()
                : "0"
        );

        EditorGUILayout.Space(4f);

        Color previousBackgroundColor =
            GUI.backgroundColor;

        GUI.backgroundColor =
            isRouteEditMode
                ? new Color(
                    1f,
                    0.65f,
                    0.45f
                )
                : new Color(
                    0.65f,
                    1f,
                    0.65f
                );

        string routeEditButtonLabel =
            isRouteEditMode
                ? "Route 편집 종료"
                : "Route 편집 시작";

        if (GUILayout.Button(
                routeEditButtonLabel,
                GUILayout.Height(34f)))
        {
            isRouteEditMode =
                !isRouteEditMode;

            if (isRouteEditMode)
            {
                // Route 편집 중에는 Unity 기본 Transform 도구가
                // Scene 클릭을 가로채지 않도록 비활성화한다.
                Tools.current =
                    Tool.None;
            }

            SceneView.RepaintAll();
        }

        GUI.backgroundColor =
            previousBackgroundColor;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(
                    "마지막 Point 삭제"))
            {
                RemoveLastRoutePoint();
            }

            if (GUILayout.Button(
                    "Route 전체 삭제"))
            {
                ClearCurrentRoute();
            }
        }

        if (isRouteEditMode)
        {
            EditorGUILayout.HelpBox(
                "Scene View에서 Route를 순서대로 찍습니다.\n" +
                "왼쪽 클릭 : Route Point 추가\n" +
                "오른쪽 클릭 : 마지막 Route Point 삭제\n\n" +
                "출발 노드와 목적지 노드 위치는 자동 처리되므로 " +
                "중간 경유점만 찍으면 됩니다.",
                MessageType.Warning
            );

            if (hasValidHoveredRouteCell)
            {
                EditorGUILayout.LabelField(
                    "현재 Grid",
                    $"({hoveredRouteGridPosition.x}, " +
                    $"{hoveredRouteGridPosition.y})"
                );
            }
        }
    }

    // 현재 Runtime 노드의 Connection 전체를
    // 원본 WorldMapData에 저장하는 UI를 표시한다.
    private void DrawWorldMapDataApplySection()
    {
        EditorGUILayout.LabelField(
            "World Map Data",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Connections의 Target Node ID와 Route Grid Positions를 수정한 뒤 " +
            "아래 버튼을 누르면 현재 노드의 연결/경로 정보가 " +
            "WorldMapData 원본에 저장됩니다.",
            MessageType.Info
        );

        MapNodeRuntime runtimeNode =
            (MapNodeRuntime)target;

        if (runtimeNode == null)
        {
            return;
        }

        if (GUILayout.Button(
                "WorldMapData에 연결/Route 적용",
                GUILayout.Height(34f)))
        {
            ApplyConnections(
                runtimeNode
            );
        }
    }

    // Scene View에서 Route 편집 입력과
    // 현재 Route의 시각적 표시를 처리한다.
    private void OnSceneGUI(
        SceneView sceneView)
    {
        if (isRouteEditMode == false)
        {
            hasValidHoveredRouteCell =
                false;

            return;
        }

        MapNodeRuntime runtimeNode =
            target as MapNodeRuntime;

        if (runtimeNode == null)
        {
            StopRouteEditMode();

            return;
        }

        WorldMapBuilder worldMapBuilder =
            FindWorldMapBuilder(
                runtimeNode
            );

        if (worldMapBuilder == null ||
            worldMapBuilder.MapGrid == null ||
            worldMapBuilder.WorldMapData == null)
        {
            return;
        }

        SerializedProperty connectionsProperty =
            serializedObject.FindProperty(
                "connections"
            );

        if (connectionsProperty == null ||
            routeEditConnectionIndex < 0 ||
            routeEditConnectionIndex >=
                connectionsProperty.arraySize)
        {
            return;
        }

        SerializedProperty connectionProperty =
            connectionsProperty
                .GetArrayElementAtIndex(
                    routeEditConnectionIndex
                );

        SerializedProperty routeGridPositionsProperty =
            connectionProperty
                .FindPropertyRelative(
                    "routeGridPositions"
                );

        if (routeGridPositionsProperty == null)
        {
            return;
        }

        // 현재 저장되어 있는 Route를 Scene View에 선과 점으로 표시한다.
        DrawCurrentRoute(
            runtimeNode,
            worldMapBuilder,
            connectionProperty,
            routeGridPositionsProperty
        );

        Event currentEvent =
            Event.current;

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
            hasValidHoveredRouteCell =
                false;

            return;
        }

        Vector3 mouseWorldPosition =
            mouseRay.GetPoint(
                enterDistance
            );

        Vector3Int hoveredCell =
            worldMapBuilder.MapGrid
                .WorldToCell(
                    mouseWorldPosition
                );

        hoveredCell.z =
            0;

        hoveredRouteGridPosition =
            new Vector2Int(
                hoveredCell.x,
                hoveredCell.y
            );

        hasValidHoveredRouteCell =
            IsInsideMapGrid(
                worldMapBuilder,
                hoveredRouteGridPosition
            );

        if (hasValidHoveredRouteCell)
        {
            DrawHoveredRouteCell(
                worldMapBuilder.MapGrid,
                hoveredCell
            );
        }

        // Route 편집 중에는 Scene 기본 오브젝트 선택을 막는다.
        HandleUtility.AddDefaultControl(
            GUIUtility.GetControlID(
                FocusType.Passive
            )
        );

        if (hasValidHoveredRouteCell ==
            false)
        {
            return;
        }

        // Alt + 마우스는 Scene View 이동/회전에 사용하므로
        // Route Point 입력으로 처리하지 않는다.
        if (currentEvent.alt)
        {
            return;
        }

        // 왼쪽 클릭으로 현재 Grid 셀을
        // Route Grid Positions 마지막에 추가한다.
        if (currentEvent.type ==
                EventType.MouseDown &&
            currentEvent.button == 0)
        {
            AddRoutePoint(
                routeGridPositionsProperty,
                hoveredRouteGridPosition
            );

            currentEvent.Use();

            SceneView.RepaintAll();
            Repaint();

            return;
        }

        // 오른쪽 클릭으로 마지막 Route Point를 제거한다.
        if (currentEvent.type ==
                EventType.MouseDown &&
            currentEvent.button == 1)
        {
            RemoveLastRoutePoint();

            currentEvent.Use();

            SceneView.RepaintAll();
            Repaint();
        }
    }

    // 현재 Route에 새로운 Grid Point를 추가한다.
    private void AddRoutePoint(
        SerializedProperty routeGridPositionsProperty,
        Vector2Int gridPosition)
    {
        if (routeGridPositionsProperty == null)
        {
            return;
        }

        // 동일한 좌표를 연속으로 여러 번 찍는 실수를 막는다.
        if (routeGridPositionsProperty.arraySize >
            0)
        {
            SerializedProperty lastPointProperty =
                routeGridPositionsProperty
                    .GetArrayElementAtIndex(
                        routeGridPositionsProperty.arraySize - 1
                    );

            if (lastPointProperty.vector2IntValue ==
                gridPosition)
            {
                return;
            }
        }

        Undo.RecordObject(
            target,
            "Add World Map Route Point"
        );

        int newIndex =
            routeGridPositionsProperty.arraySize;

        routeGridPositionsProperty
            .InsertArrayElementAtIndex(
                newIndex
            );

        SerializedProperty newPointProperty =
            routeGridPositionsProperty
                .GetArrayElementAtIndex(
                    newIndex
                );

        newPointProperty.vector2IntValue =
            gridPosition;

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(
            target
        );
    }

    // 현재 선택된 Connection의
    // 마지막 Route Point를 제거한다.
    private void RemoveLastRoutePoint()
    {
        SerializedProperty routeProperty =
            GetCurrentRouteGridPositionsProperty();

        if (routeProperty == null ||
            routeProperty.arraySize <=
            0)
        {
            return;
        }

        Undo.RecordObject(
            target,
            "Remove World Map Route Point"
        );

        routeProperty.DeleteArrayElementAtIndex(
            routeProperty.arraySize - 1
        );

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(
            target
        );

        SceneView.RepaintAll();
        Repaint();
    }

    // 현재 Connection에 저장된
    // Route Point를 모두 제거한다.
    private void ClearCurrentRoute()
    {
        SerializedProperty routeProperty =
            GetCurrentRouteGridPositionsProperty();

        if (routeProperty == null ||
            routeProperty.arraySize <=
            0)
        {
            return;
        }

        bool confirmed =
            EditorUtility.DisplayDialog(
                "Route 전체 삭제",
                "현재 Connection의 Route Grid Positions를 모두 삭제합니다.",
                "삭제",
                "취소"
            );

        if (confirmed == false)
        {
            return;
        }

        Undo.RecordObject(
            target,
            "Clear World Map Route"
        );

        routeProperty.ClearArray();

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(
            target
        );

        SceneView.RepaintAll();
        Repaint();
    }

    // 현재 선택된 Connection의
    // Route Grid Positions SerializedProperty를 반환한다.
    private SerializedProperty
        GetCurrentRouteGridPositionsProperty()
    {
        serializedObject.Update();

        SerializedProperty connectionsProperty =
            serializedObject.FindProperty(
                "connections"
            );

        if (connectionsProperty == null ||
            routeEditConnectionIndex < 0 ||
            routeEditConnectionIndex >=
                connectionsProperty.arraySize)
        {
            return null;
        }

        SerializedProperty connectionProperty =
            connectionsProperty
                .GetArrayElementAtIndex(
                    routeEditConnectionIndex
                );

        return
            connectionProperty.FindPropertyRelative(
                "routeGridPositions"
            );
    }

    // 현재 Connection의 Route를
    // Scene View에서 선과 점으로 시각화한다.
    private void DrawCurrentRoute(
        MapNodeRuntime runtimeNode,
        WorldMapBuilder worldMapBuilder,
        SerializedProperty connectionProperty,
        SerializedProperty routeGridPositionsProperty)
    {
        Grid mapGrid =
            worldMapBuilder.MapGrid;

        if (mapGrid == null)
        {
            return;
        }

        Vector3 previousWorldPosition =
            runtimeNode.transform.position;

        // 출발 노드부터 각 Route Point까지
        // 순서대로 선을 그린다.
        for (int i = 0;
             i < routeGridPositionsProperty.arraySize;
             i++)
        {
            SerializedProperty pointProperty =
                routeGridPositionsProperty
                    .GetArrayElementAtIndex(
                        i
                    );

            Vector2Int gridPosition =
                pointProperty.vector2IntValue;

            Vector3 pointWorldPosition =
                mapGrid.GetCellCenterWorld(
                    new Vector3Int(
                        gridPosition.x,
                        gridPosition.y,
                        0
                    )
                );

            Handles.DrawAAPolyLine(
                3f,
                previousWorldPosition,
                pointWorldPosition
            );

            Handles.DrawSolidDisc(
                pointWorldPosition,
                Vector3.forward,
                0.06f
            );

            Handles.Label(
                pointWorldPosition +
                new Vector3(
                    0.08f,
                    0.08f,
                    0f
                ),
                $"{i} ({gridPosition.x}, {gridPosition.y})"
            );

            previousWorldPosition =
                pointWorldPosition;
        }

        // Target Node가 현재 Scene에 생성되어 있다면
        // 마지막 Route Point에서 목적지 노드까지도 선으로 표시한다.
        SerializedProperty targetNodeIdProperty =
            connectionProperty.FindPropertyRelative(
                "targetNodeId"
            );

        if (targetNodeIdProperty == null ||
            string.IsNullOrWhiteSpace(
                targetNodeIdProperty.stringValue))
        {
            return;
        }

        MapNodeRuntime targetNode =
            worldMapBuilder.GetGeneratedNode(
                targetNodeIdProperty.stringValue.Trim()
            );

        if (targetNode == null)
        {
            return;
        }

        Handles.DrawAAPolyLine(
            3f,
            previousWorldPosition,
            targetNode.transform.position
        );
    }

    // 현재 마우스가 올라간 Route Grid 셀을
    // Scene View에서 사각형으로 표시한다.
    private void DrawHoveredRouteCell(
        Grid mapGrid,
        Vector3Int cellPosition)
    {
        Vector3 cellCenter =
            mapGrid.GetCellCenterWorld(
                cellPosition
            );

        Vector3 cellSize =
            mapGrid.cellSize;

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

        Handles.DrawAAPolyLine(
            3f,
            corners[0],
            corners[1],
            corners[2],
            corners[3],
            corners[0]
        );
    }

    // 지정한 Grid 좌표가 현재 WorldMapData의
    // 실제 맵 범위 안인지 검사한다.
    private bool IsInsideMapGrid(
        WorldMapBuilder worldMapBuilder,
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

    // 현재 노드가 속한 Scene의
    // WorldMapBuilder를 찾는다.
    private WorldMapBuilder FindWorldMapBuilder(
        MapNodeRuntime runtimeNode)
    {
        if (runtimeNode == null)
        {
            return null;
        }

        WorldMapBuilder worldMapBuilder =
            runtimeNode.GetComponentInParent<WorldMapBuilder>();

        // NodeRoot가 WorldMapBuilder의 직접 자식이 아닐 수 있으므로
        // 부모 탐색 실패 시 Scene에서 다시 찾는다.
        if (worldMapBuilder == null)
        {
            worldMapBuilder =
                Object.FindFirstObjectByType<WorldMapBuilder>();
        }

        return worldMapBuilder;
    }

    // Route Scene 편집 상태를 종료한다.
    private void StopRouteEditMode()
    {
        isRouteEditMode =
            false;

        routeEditConnectionIndex =
            -1;

        hasValidHoveredRouteCell =
            false;

        SceneView.RepaintAll();
    }

    // 현재 MapNodeRuntime의 Connection 전체를
    // 원본 WorldMapData에 저장한다.
    private void ApplyConnections(
        MapNodeRuntime runtimeNode)
    {
        if (runtimeNode == null)
        {
            return;
        }

        WorldMapBuilder worldMapBuilder =
            FindWorldMapBuilder(
                runtimeNode
            );

        if (worldMapBuilder == null)
        {
            Debug.LogWarning(
                "노드 연결 정보 적용 실패: " +
                "현재 Scene에서 WorldMapBuilder를 찾지 못했습니다."
            );

            return;
        }

        // Inspector 및 Scene View에서 편집한 값을
        // Runtime 컴포넌트에 먼저 확정한다.
        serializedObject.ApplyModifiedProperties();

        // Target Node ID와 Route Grid Positions를
        // 원본 WorldMapData에 함께 저장한다.
        bool applied =
            worldMapBuilder
                .ApplyConnectionsFromRuntime(
                    runtimeNode
                );

        if (applied == false)
        {
            return;
        }

        EditorUtility.SetDirty(
            runtimeNode
        );

        Repaint();

        Debug.Log(
            $"MapNodeRuntime Inspector 적용 완료: " +
            $"{runtimeNode.GetNodeId()}"
        );
    }
}

#endif