using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Spine.Unity;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
// < 변경부분 >
// Event Step을 Unity 기본 방식으로 부드럽게 Drag Reorder하기 위해 사용.
using UnityEditorInternal;

// <변경부분>
// EventSceneData 하나에서
//
// - Event 기본 정보
// - Background
// - Event Steps
// - Dialogue Localization
// - Completion
//
// 을 모두 제작하기 위한 전용 Editor.
//
// 기존 Battle / Tutorial용 EventSequenceDataEditor와
// 완전히 별개의 제작 환경이다.
[CustomEditor(typeof(EventSceneData))]
public class EventSceneDataEditor : Editor
{
    private const string TableCollectionName =
     "EventScene_Dialogue";

    private readonly Dictionary<int, bool>
       stepFoldouts =
           new Dictionary<int, bool>();

    // <변경부분>
    // ReorderableList를 그리는 도중에는 Step 목록을 수정하지 않는다.
    // 버튼 클릭 후 목록 렌더링이 끝나면 복제 요청을 처리한다.
    private int pendingDuplicateStepIndex = -1;

    // <변경부분>
    // Event Scene Step Header 한 줄의 기본 높이.
    private const float EventStepHeaderHeight =
        24f;


    // <변경부분>
    // Event Step 목록과 별도로
    // Dialogue 관리 영역의 펼침 상태를 저장한다.
    private readonly Dictionary<int, bool>
        dialogueStepFoldouts =
            new Dictionary<int, bool>();


    // <변경부분>
    // Event Scene Steps를 Unity 기본 ReorderableList로 관리한다.
    //
    // 기존 직접 구현한 Drag & Drop과 달리,
    // Unity가 Drag 중 Element 위치 이동 / 삽입 위치 / 시각 피드백을
    // 직접 처리하므로 Battle Event Steps와 같은 부드러운 조작감을 사용한다.
    private ReorderableList eventStepsReorderableList;

    // <변경부분>
    // EventSceneData.steps의 SerializedProperty.
    private SerializedProperty eventStepsProperty;

    // <변경부분>
    // Scene View에서 SpawnActor의 BackgroundTile 좌표를
    // 선택하고 있는지 여부.
    private bool isSelectingSpawnActorTile =
        false;

    // <변경부분>
    // 현재 Scene View 좌표 선택을 요청한 Step Index.
    private int selectingSpawnActorStepIndex =
        -1;

    // <변경부분>
    // Scene View에서 MoveActor 목적지를 선택하고 있는지 여부.
    private bool isSelectingMoveActorTile =
        false;

    // <변경부분>
    // 현재 목적지 좌표 선택을 요청한 MoveActor Step Index.
    private int selectingMoveActorStepIndex =
     -1;


    // <변경부분>
    // Scene View에서 CameraShot의 BackgroundTile Target을
    // 선택하고 있는지 여부.
    private bool isSelectingCameraShotTile =
        false;

    // <변경부분>
    // 현재 CameraShot Target 선택을 요청한 Step Index.
    private int selectingCameraShotStepIndex =
        -1;


    // <변경부분>
    // EventSceneData Inspector가 활성화되면
    // Scene View 입력을 감지한다.
    private void OnEnable()
    {
        // <변경부분>
        // Battle Event Steps처럼 Unity가 직접 관리하는
        // 부드러운 Drag Reorder List를 생성한다.
        InitializeEventStepsReorderableList();

        SceneView.duringSceneGui +=
            HandleSceneGUI;
    }

    // <변경부분>
    // Inspector가 닫히거나 다른 Asset으로 이동하면
    // Scene View 입력 감지를 해제한다.
    private void OnDisable()
    {
        SceneView.duringSceneGui -=
            HandleSceneGUI;

        isSelectingSpawnActorTile =
    false;

        selectingSpawnActorStepIndex =
            -1;

        isSelectingMoveActorTile =
    false;

        selectingMoveActorStepIndex =
            -1;

        isSelectingCameraShotTile =
    false;

        selectingCameraShotStepIndex =
            -1;
    }

    // <변경부분>
    // EventSceneData.steps를 Unity ReorderableList에 연결한다.
    //
    // 목적:
    // - Battle Event Steps와 동일한 자연스러운 Drag Reorder
    // - 왼쪽 Drag Handle 제공
    // - Drag 중 다른 Step이 실시간으로 자리를 이동
    // - SerializedProperty 기반 Undo / Serialization 유지
    private void InitializeEventStepsReorderableList()
    {
        eventStepsProperty =
            serializedObject.FindProperty(
                "steps"
            );

        if (eventStepsProperty == null)
        {
            eventStepsReorderableList =
                null;

            return;
        }

        eventStepsReorderableList =
            new ReorderableList(
                serializedObject,
                eventStepsProperty,
                true,   // draggable
                true,   // displayHeader
                true,   // displayAddButton
                true    // displayRemoveButton
            );

        // =====================================================
        // Header
        // =====================================================

        eventStepsReorderableList.drawHeaderCallback =
            rect =>
            {
                EditorGUI.LabelField(
                    rect,
                    $"Steps    {eventStepsProperty.arraySize}"
                );
            };


        // =====================================================
        // Element Height
        // =====================================================

        // <변경부분>
        // ReorderableList 내부에서는 GUILayout 높이 측정을 사용하지 않는다.
        // 현재 Step Type과 SerializedProperty 상태를 기준으로
        // 필요한 높이를 직접 계산한다.
        eventStepsReorderableList.elementHeightCallback =
            index =>
            {
                if (index < 0 ||
                    index >=
                        eventStepsProperty.arraySize)
                {
                    return EventStepHeaderHeight;
                }

                if (GetStepFoldout(
                        index) == false)
                {
                    return EventStepHeaderHeight;
                }

                EventSceneData eventData =
                    target as EventSceneData;

                if (eventData == null ||
                    eventData.steps == null ||
                    index >= eventData.steps.Count)
                {
                    return EventStepHeaderHeight;
                }

                EventSceneStepData step =
                    eventData.steps[index];

                SerializedProperty stepProperty =
                    eventStepsProperty
                        .GetArrayElementAtIndex(
                            index
                        );

                if (step == null ||
                    stepProperty == null)
                {
                    return EventStepHeaderHeight;
                }

                return
                    EventStepHeaderHeight +
                    GetExpandedEventSceneStepHeight(
                        eventData,
                        step,
                        stepProperty,
                        index
                    ) +
                    8f;
            };


        // =====================================================
        // Element
        // =====================================================

        eventStepsReorderableList.drawElementCallback =
            (
                rect,
                index,
                isActive,
                isFocused
            ) =>
            {
                if (index < 0 ||
                    index >=
                        eventStepsProperty.arraySize)
                {
                    return;
                }

                EventSceneData eventData =
                    target as EventSceneData;

                if (eventData == null ||
                    eventData.steps == null ||
                    index >= eventData.steps.Count)
                {
                    return;
                }

                EventSceneStepData step =
                    eventData.steps[index];

                SerializedProperty stepProperty =
                    eventStepsProperty
                        .GetArrayElementAtIndex(
                            index
                        );

                if (step == null ||
                    stepProperty == null)
                {
                    return;
                }

                SerializedProperty stepNameProperty =
                    stepProperty.FindPropertyRelative(
                        "stepName"
                    );

                SerializedProperty stepTypeProperty =
                    stepProperty.FindPropertyRelative(
                        "stepType"
                    );

                string stepName =
                    stepNameProperty != null
                        ? stepNameProperty.stringValue
                        : string.Empty;

                string stepTypeName =
                    stepTypeProperty != null
                        ? ((EventSceneStepType)
                            stepTypeProperty.intValue)
                            .ToString()
                        : "None";

                string stepNumber =
                    (index + 1).ToString(
                        "D2"
                    );

                string stepDisplayName =
                    string.IsNullOrWhiteSpace(
                        stepName)
                        ? stepTypeName
                        : stepName;

                string label =
                    $"Step{stepNumber} - {stepDisplayName}";

                bool expanded =
                    GetStepFoldout(
                        index
                    );

                // <변경부분>
                // ReorderableList Drag Handle과 Foldout 화살표가
                // 겹치지 않도록 왼쪽 여백을 유지한다.
                const float leftPadding =
                    18f;

                Rect headerRect =
                    new Rect(
                        rect.x + leftPadding,
                        rect.y + 2f,
                        Mathf.Max(
                            0f,
                            rect.width - leftPadding
                        ),
                        EditorGUIUtility.singleLineHeight
                    );

                bool newExpanded =
                    EditorGUI.Foldout(
                        headerRect,
                        expanded,
                        label,
                        true
                    );

                if (newExpanded != expanded)
                {
                    stepFoldouts[index] =
                        newExpanded;

                    Repaint();
                }

                if (newExpanded == false)
                {
                    return;
                }

                // <변경부분>
                // Battle Event와 동일하게
                // GUILayout / EditorGUILayout을 사용하지 않고
                // ReorderableList Rect 안에 EditorGUI로 직접 그린다.
                Rect contentRect =
                    new Rect(
                        rect.x + leftPadding,
                        rect.y +
                            EventStepHeaderHeight,
                        Mathf.Max(
                            0f,
                            rect.width - leftPadding
                        ),
                        Mathf.Max(
                            40f,
                            rect.height -
                            EventStepHeaderHeight -
                            4f
                        )
                    );

                DrawExpandedEventSceneStep(
                    contentRect,
                    eventData,
                    step,
                    stepProperty,
                    index
                );
            };


        // =====================================================
        // Add
        // =====================================================

        eventStepsReorderableList.onAddCallback =
            list =>
            {
                EventSceneData eventData =
                    target as EventSceneData;

                if (eventData == null)
                {
                    return;
                }

                // 현재 Serialized 변경을 먼저 반영한다.
                serializedObject
                    .ApplyModifiedProperties();

                AddStep(
                    eventData
                );

                // 직접 List를 수정했으므로
                // SerializedProperty를 다시 동기화한다.
                serializedObject.Update();

                eventStepsProperty =
                    serializedObject.FindProperty(
                        "steps"
                    );

                if (eventStepsProperty != null &&
                    eventStepsProperty.arraySize > 0)
                {
                    int newStepIndex =
                        eventStepsProperty.arraySize -
                        1;

                    list.index =
                        newStepIndex;

                    // <변경부분>
                    // 새로 만든 Step은 바로 수정할 수 있도록 펼친다.
                    stepFoldouts[newStepIndex] =
                        true;
                }

                Repaint();
            };


        // =====================================================
        // Remove
        // =====================================================

        eventStepsReorderableList.onRemoveCallback =
            list =>
            {
                EventSceneData eventData =
                    target as EventSceneData;

                if (eventData == null ||
                    eventData.steps == null ||
                    list.index < 0 ||
                    list.index >=
                        eventData.steps.Count)
                {
                    return;
                }

                int removeIndex =
                    list.index;

                serializedObject
                    .ApplyModifiedProperties();

                // 기존 RemoveStep을 그대로 재사용한다.
                //
                // Localization Table Entry를 즉시 지우지 않는
                // 기존 안전 정책도 그대로 유지된다.
                RemoveStep(
                    eventData,
                    removeIndex
                );

                serializedObject.Update();

                eventStepsProperty =
                    serializedObject.FindProperty(
                        "steps"
                    );

                if (eventStepsProperty == null ||
                    eventStepsProperty.arraySize <= 0)
                {
                    list.index =
                        -1;
                }
                else
                {
                    list.index =
                        Mathf.Clamp(
                            removeIndex,
                            0,
                            eventStepsProperty.arraySize -
                            1
                        );
                }

                // <변경부분>
                // 삭제하면 뒤쪽 Step Index가 당겨지므로
                // Index 기반 Foldout 상태를 초기화한다.
                stepFoldouts.Clear();

                // Dialogue Foldout도 Step Index 기반이므로 같이 초기화한다.
                dialogueStepFoldouts.Clear();

                CancelAllEventSceneTileSelection();

                Repaint();
                SceneView.RepaintAll();
            };


        // =====================================================
        // Reorder
        // =====================================================

        eventStepsReorderableList.onReorderCallback =
            list =>
            {
                EventSceneData eventData =
                    target as EventSceneData;

                if (eventData == null)
                {
                    return;
                }

                // <변경부분>
                // SerializedProperty 기반 Reorder라
                // 실제 순서 변경은 Unity가 수행한다.
                //
                // 별도의 RemoveAt / Insert 계산은 하지 않는다.
                serializedObject
                    .ApplyModifiedProperties();

                EditorUtility.SetDirty(
                    eventData
                );

                // <변경부분>
                // Drag Reorder 후에는 Step Index가 달라지므로
                // Index 기반 UI 상태를 전부 초기화한다.
                stepFoldouts.Clear();
                dialogueStepFoldouts.Clear();

                // Scene View Tile 선택 중에 Step 순서가 바뀌면
                // 이전 Index가 다른 Step을 가리킬 수 있으므로 안전하게 종료한다.
                CancelAllEventSceneTileSelection();

                Repaint();
                SceneView.RepaintAll();
            };
    }

    // <변경부분>
    // Step 순서 변경 / 삭제 시
    // 기존 Step Index를 사용하는 Scene View 선택 상태를 모두 해제한다.
    private void CancelAllEventSceneTileSelection()
    {
        isSelectingSpawnActorTile =
            false;

        selectingSpawnActorStepIndex =
            -1;

        isSelectingMoveActorTile =
            false;

        selectingMoveActorStepIndex =
            -1;

        isSelectingCameraShotTile =
            false;

        selectingCameraShotStepIndex =
            -1;
    }

    public override void OnInspectorGUI()
    {
        EventSceneData eventData =
            (EventSceneData)target;

        // <변경부분>
        // 분기 목적지용 Step ID가 없거나 중복된 경우
        // 기존 ID를 우선 보존하면서 필요한 ID만 생성한다.
        EnsureUniqueBranchStepIds(
            eventData
        );

        DrawEventInfo(
     eventData
 );

        EditorGUILayout.Space(10);

        // <변경부분>
        // EventScene의 공용 Run HUD 표시 설정을 편집한다.
        DrawEventUISettings(
            eventData
        );

        EditorGUILayout.Space(10);

        DrawLocalizationHeader(
            eventData
        );

        EditorGUILayout.Space(10);

        DrawEventSteps(
    eventData
);

        EditorGUILayout.Space(10);

        // <변경부분>
        // Step 목록과 Dialogue 편집 영역을 분리한다.
        // 기존 Tutorial EventSequenceDataEditor와 동일한 관리 방식.
        DrawDialoguePages(
    eventData
);

        EditorGUILayout.Space(10);

        // <변경부분>
        // Dialogue와 동일한 EventScene_Dialogue Collection을 사용하면서
        // SpeechBubble의 KR / EN / JA를 별도 관리 영역에서 편집한다.
        DrawSpeechBubbleTexts(
            eventData
        );

        EditorGUILayout.Space(10);

        DrawTranslationStatus(
            eventData
        );

        EditorGUILayout.Space(10);

        DrawCompletion(
            eventData
        );
    }


    // <변경부분>
    // Event Scene 자체의 기본 정보와 Background를 편집한다.
    private void DrawEventInfo(
        EventSceneData eventData)
    {
        EditorGUILayout.LabelField(
            "Event Scene",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();

            string newEventName =
                EditorGUILayout.TextField(
                    "Event Name",
                    eventData.eventName
                );

            string newDescription =
                EditorGUILayout.TextArea(
                    eventData.description ?? string.Empty,
                    GUILayout.MinHeight(55)
                );

            BackgroundMapData newBackgroundMapData =
                (BackgroundMapData)
                EditorGUILayout.ObjectField(
                    "Background Map Data",
                    eventData.backgroundMapData,
                    typeof(BackgroundMapData),
                    false
                );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    eventData,
                    "Edit Event Scene Info"
                );

                eventData.eventName =
                    newEventName;

                eventData.description =
                    newDescription;

                eventData.backgroundMapData =
                    newBackgroundMapData;

                EditorUtility.SetDirty(
                    eventData
                );
            }
        }
    }


    // <변경부분>
    // Event Scene의 공용 Run HUD와
    // 상점 나가기 버튼 표시 방식을 편집한다.
    private void DrawEventUISettings(
        EventSceneData eventData)
    {
        EditorGUILayout.LabelField(
            "Event UI",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();

            bool newShowRunHUD =
                EditorGUILayout.Toggle(
                    "Show Run HUD During Player Interaction",
                    eventData.showRunHUDDuringPlayerInteraction
                );

            // <변경부분>
            // 일반 이벤트는 기본 OFF.
            // 상점 이벤트에서만 체크하여 버튼을 표시한다.
            bool newShowShopExitButton =
                EditorGUILayout.Toggle(
                    "Show Shop Exit Button During Player Interaction",
                    eventData.showShopExitButtonDuringPlayerInteraction
                );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    eventData,
                    "Edit Event Scene UI"
                );

                eventData.showRunHUDDuringPlayerInteraction =
                    newShowRunHUD;

                eventData.showShopExitButtonDuringPlayerInteraction =
                    newShowShopExitButton;

                EditorUtility.SetDirty(
                    eventData
                );
            }

            EditorGUILayout.HelpBox(
                "Run HUD\n" +
                "OFF: 일반 이벤트처럼 Run HUD를 숨깁니다.\n" +
                "ON: UnlockPlayerInteraction 실행 시 Run HUD를 표시합니다.\n\n" +
                "Shop Exit Button\n" +
                "OFF: 상점 나가기 버튼을 숨깁니다.\n" +
                "ON: UnlockPlayerInteraction 실행 시 버튼을 표시합니다.\n" +
                "LockPlayerInteraction 실행 시 다시 숨깁니다.",
                MessageType.Info
            );
        }
    }


    // <변경부분>
    // Event Scene Dialogue Localization 공통 설정.
    private void DrawLocalizationHeader(
        EventSceneData eventData)
    {
        EditorGUILayout.LabelField(
            "Event Scene Dialogue Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();

            string newLocalizationId =
                EditorGUILayout.TextField(
                    "Localization ID",
                    eventData.localizationId
                );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    eventData,
                    "Edit Event Scene Localization ID"
                );

                eventData.localizationId =
                    SanitizeLocalizationId(
                        newLocalizationId
                    );

                EditorUtility.SetDirty(
                    eventData
                );
            }

            if (string.IsNullOrWhiteSpace(
                    eventData.localizationId))
            {
                EditorGUILayout.HelpBox(
                    "Localization ID가 없습니다. " +
                    "Asset 이름을 기준으로 한 번 생성한 뒤 유지하는 것을 권장합니다.",
                    MessageType.Warning
                );

                if (GUILayout.Button(
                        "Asset 이름으로 Localization ID 생성"))
                {
                    Undo.RecordObject(
                        eventData,
                        "Generate Event Scene Localization ID"
                    );

                    eventData.localizationId =
                        CreateDefaultLocalizationId(
                            eventData
                        );

                    EditorUtility.SetDirty(
                        eventData
                    );
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Localization ID는 번역 Key의 고정 identity입니다. " +
                    "번역 생성 이후에는 변경하지 마세요.",
                    MessageType.Info
                );
            }

            EditorGUILayout.Space(5);

            // <변경부분>
            // Dialogue가 하나도 없어도 SpeechBubble Step이 존재하면
            // Localization 생성 / 한국어 Sync를 실행할 수 있다.
            bool canSync =
                string.IsNullOrWhiteSpace(
                    eventData.localizationId) == false &&
                (
                    HasAnyDialoguePage(
                        eventData
                    ) ||
                    HasAnySpeechBubbleStep(
                        eventData
                    )
                );

            using (new EditorGUI.DisabledScope(
                       canSync == false))
            {
                if (GUILayout.Button(
                        "Localization 생성 / 한국어 동기화"))
                {
                    SyncLocalization(
                        eventData
                    );
                }
            }

            if (canSync == false)
            {
                EditorGUILayout.HelpBox(
                    "Localization ID와 최소 1개의 Dialogue Page 또는 SpeechBubble Step이 필요합니다.",
                    MessageType.None
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "한국어 원문만 EventScene_Dialogue KO Table에 동기화합니다.\n" +
                    "기존 English / Japanese 번역은 덮어쓰지 않습니다.",
                    MessageType.None
                );
            }

            if (GUILayout.Button(
                    "Localization Tables 열기"))
            {
                DevoryaLocalizationEditorUtility
                    .OpenLocalizationTables();
            }
        }
    }


    private void DrawEventSteps(
      EventSceneData eventData)
    {
        EditorGUILayout.LabelField(
            "Event Steps",
            EditorStyles.boldLabel
        );

        if (eventData == null)
        {
            return;
        }

        if (eventStepsReorderableList == null ||
            eventStepsProperty == null)
        {
            InitializeEventStepsReorderableList();
        }

        if (eventStepsReorderableList == null ||
            eventStepsProperty == null)
        {
            EditorGUILayout.HelpBox(
                "EventSceneData.steps를 찾을 수 없습니다.",
                MessageType.Error
            );

            return;
        }

        serializedObject.Update();

        int stepCount =
            eventStepsProperty.arraySize;

        // <변경부분>
        // Event Steps 최상단 전체 펼치기 / 접기.
        DrawEventSceneStepFoldoutControls(
            stepCount
        );

        EditorGUILayout.Space(3);

        eventStepsReorderableList
     .DoLayoutList();

        serializedObject
            .ApplyModifiedProperties();

        // <변경부분>
        // 버튼 클릭으로 예약된 Step 복제를
        // ReorderableList의 Draw가 끝난 후 처리한다.
        if (pendingDuplicateStepIndex >= 0)
        {
            int sourceIndex =
                pendingDuplicateStepIndex;

            pendingDuplicateStepIndex = -1;

            DuplicateEventSceneStep(
                eventData,
                sourceIndex
            );

            // <변경부분>
            // 목록 구조가 변경되었으므로
            // SerializedProperty를 다시 동기화한다.
            serializedObject.Update();

            eventStepsProperty =
                serializedObject.FindProperty(
                    "steps"
                );

            if (eventStepsReorderableList != null)
            {
                eventStepsReorderableList.index =
                    sourceIndex + 1;
            }

            Repaint();

            // <변경부분>
            // 목록의 Element 개수가 변경된 현재 GUI Event를
            // 종료하여 Layout 불일치를 방지한다.
            GUIUtility.ExitGUI();
        }

        if (stepCount <= 0)
        {
            EditorGUILayout.HelpBox(
                "Event Step이 없습니다.",
                MessageType.Info
            );

            return;
        }

        EditorGUILayout.Space(3);

        // <변경부분>
        // 긴 Step 목록의 최하단에서도
        // 다시 위로 올라가지 않고 전체 상태를 바꿀 수 있다.
        DrawEventSceneStepFoldoutControls(
      eventStepsProperty.arraySize
  );
    }


    // <변경부분>
    // Event Scene Step 목록의
    // 전체 펼치기 / 전체 접기 버튼.
    //
    // 동일한 버튼을 Steps 목록의
    // 위와 아래 양쪽에서 사용한다.
    // <변경부분>
    // 펼쳐진 Event Scene Step의 높이를 Rect 기반으로 계산한다.
    private float GetExpandedEventSceneStepHeight(
        EventSceneData eventData,
        EventSceneStepData step,
        SerializedProperty stepProperty,
        int stepIndex)
    {
        Rect measureRect =
            new Rect(
                0f,
                0f,
                Mathf.Max(
                    320f,
                    EditorGUIUtility.currentViewWidth - 80f
                ),
                0f
            );

        return
            ProcessExpandedEventSceneStep(
                measureRect,
                eventData,
                step,
                stepProperty,
                stepIndex,
                false
            );
    }


    // <변경부분>
    // 펼쳐진 Event Scene Step을 ReorderableList 내부에 직접 그린다.
    private void DrawExpandedEventSceneStep(
        Rect contentRect,
        EventSceneData eventData,
        EventSceneStepData step,
        SerializedProperty stepProperty,
        int stepIndex)
    {
        ProcessExpandedEventSceneStep(
            contentRect,
            eventData,
            step,
            stepProperty,
            stepIndex,
            true
        );
    }


    // <변경부분>
    // draw == false : 필요한 높이 계산
    // draw == true  : 같은 순서로 실제 EditorGUI 출력
    private float ProcessExpandedEventSceneStep(
        Rect contentRect,
        EventSceneData eventData,
        EventSceneStepData step,
        SerializedProperty stepProperty,
        int stepIndex,
        bool draw)
    {
        float startY =
            contentRect.y;

        float currentY =
            startY;

        if (eventData == null ||
            step == null ||
            stepProperty == null)
        {
            ProcessEventSceneHelpBox(
                ref currentY,
                contentRect,
                "Event Scene Step 정보를 찾을 수 없습니다.",
                MessageType.Error,
                draw
            );

            return
                Mathf.Max(
                    40f,
                    currentY - startY
                );
        }

        ProcessEventSceneProperties(
            ref currentY,
            contentRect,
            stepProperty,
            draw,
            "stepName", "Step Name",
            "stepType", "Step Type"
        );

        currentY +=
            7f;

        SerializedProperty stepTypeProperty =
            stepProperty.FindPropertyRelative(
                "stepType"
            );

        if (stepTypeProperty == null)
        {
            ProcessEventSceneHelpBox(
                ref currentY,
                contentRect,
                "Step Type Property를 찾을 수 없습니다.",
                MessageType.Error,
                draw
            );

            return
                Mathf.Max(
                    40f,
                    currentY - startY
                );
        }

        EventSceneStepType stepType =
            (EventSceneStepType)
            stepTypeProperty.intValue;

        switch (stepType)
        {
            case EventSceneStepType.None:

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "아무 동작도 하지 않는 Step입니다.",
                    MessageType.None,
                    draw
                );

                break;


            case EventSceneStepType.Dialogue:

                int dialoguePageCount =
                    step.dialoguePages != null
                        ? step.dialoguePages.Count
                        : 0;

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "Dialogue 내용은 아래 Dialogue Pages 영역에서 관리합니다.\n" +
                    $"현재 Page: {dialoguePageCount}",
                    MessageType.None,
                    draw
                );

                break;


            case EventSceneStepType.Wait:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Wait",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "waitDuration", "Wait Duration"
                );

                break;


            case EventSceneStepType.SpawnActor:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Actor",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "spawnActorId", "Actor ID",
                    "spawnActorPieceData", "Piece Data",
                    "spawnActorTeam", "Visual Team"
                );

                SerializedProperty spawnTeamProperty =
                    stepProperty.FindPropertyRelative(
                        "spawnActorTeam"
                    );

                if (spawnTeamProperty != null &&
                    spawnTeamProperty.intValue ==
                        (int)PieceTeam.Player)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "spawnActorUseAbsorbedPlayerVisual",
                        "Use Absorbed Player Visual"
                    );
                }

                ProcessEventSceneProperties(
        ref currentY,
        contentRect,
        stepProperty,
        draw,
        "spawnActorVisualOffset", "Visual Offset",
        "spawnActorFlipX", "Flip X",
        "spawnActorFadeInDuration",
        "Fallback Fade In Duration"
    );

                currentY +=
                    5f;

                // <변경부분>
                // SpawnActor의 실제 Unity Sorting Layer 설정.
                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Visual Sorting",
                    draw
                );

                ProcessEventActorSortingLayerPopup(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "spawnActorSortingLayerName",
                    draw
                );

                currentY +=
                    5f;

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Spawn Position",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "spawnActorPosition", "Background Tile"
                );

                bool isSpawnSelecting =
                    isSelectingSpawnActorTile &&
                    selectingSpawnActorStepIndex ==
                        stepIndex;

                if (ProcessEventSceneButton(
                        ref currentY,
                        contentRect,
                        isSpawnSelecting
                            ? "Scene 타일 선택 취소"
                            : "Scene에서 타일 선택",
                        draw))
                {
                    if (isSpawnSelecting)
                    {
                        isSelectingSpawnActorTile =
                            false;

                        selectingSpawnActorStepIndex =
                            -1;
                    }
                    else
                    {
                        isSelectingMoveActorTile =
                            false;

                        selectingMoveActorStepIndex =
                            -1;

                        isSelectingSpawnActorTile =
                            true;

                        selectingSpawnActorStepIndex =
                            stepIndex;

                        SceneView.RepaintAll();
                    }
                }

                if (isSpawnSelecting)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "Scene View에서 원하는 BackgroundTile을 클릭하세요.\n" +
                        "Alt 입력은 Scene View 카메라 조작으로 유지됩니다.",
                        MessageType.Info,
                        draw
                    );
                }

                SerializedProperty spawnActorIdProperty =
                    stepProperty.FindPropertyRelative(
                        "spawnActorId"
                    );

                if (spawnActorIdProperty != null &&
                    string.IsNullOrWhiteSpace(
                        spawnActorIdProperty.stringValue))
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "SpawnActor를 실행하려면 Actor ID가 필요합니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                SerializedProperty spawnPieceDataProperty =
                    stepProperty.FindPropertyRelative(
                        "spawnActorPieceData"
                    );

                if (spawnPieceDataProperty != null &&
                    spawnPieceDataProperty.objectReferenceValue ==
                        null)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "SpawnActor를 실행하려면 Piece Data가 필요합니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                break;


            case EventSceneStepType.MoveActor:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Actor",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "moveActorId", "Actor ID",
                    "moveActorDuration", "Move Duration",
                    "moveActorArcHeight", "Arc Height",
                    "moveActorChangeFlipX", "Change Flip X"
                );

                SerializedProperty moveChangeFlipProperty =
                    stepProperty.FindPropertyRelative(
                        "moveActorChangeFlipX"
                    );

                if (moveChangeFlipProperty != null &&
     moveChangeFlipProperty.boolValue)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "moveActorFlipX", "Flip X"
                    );
                }

                currentY +=
                    5f;

                // <변경부분>
                // MoveActor에서 실제 Sorting Layer를 변경한다.
                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Visual Sorting",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "moveActorChangeSortingLayer",
                    "Change Sorting Layer"
                );

                SerializedProperty moveChangeSortingLayerProperty =
                    stepProperty.FindPropertyRelative(
                        "moveActorChangeSortingLayer"
                    );

                if (moveChangeSortingLayerProperty != null &&
                    moveChangeSortingLayerProperty.boolValue)
                {
                    ProcessEventActorSortingLayerPopup(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "moveActorSortingLayerName",
                        draw
                    );
                }

                currentY +=
                    5f;

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Destination",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "moveActorDestination", "Background Tile"
                );

                bool isMoveSelecting =
                    isSelectingMoveActorTile &&
                    selectingMoveActorStepIndex ==
                        stepIndex;

                if (ProcessEventSceneButton(
                        ref currentY,
                        contentRect,
                        isMoveSelecting
                            ? "Scene 타일 선택 취소"
                            : "Scene에서 목적지 선택",
                        draw))
                {
                    if (isMoveSelecting)
                    {
                        isSelectingMoveActorTile =
                            false;

                        selectingMoveActorStepIndex =
                            -1;
                    }
                    else
                    {
                        isSelectingSpawnActorTile =
                            false;

                        selectingSpawnActorStepIndex =
                            -1;

                        isSelectingMoveActorTile =
                            true;

                        selectingMoveActorStepIndex =
                            stepIndex;

                        SceneView.RepaintAll();
                    }
                }

                if (isMoveSelecting)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "Scene View에서 이동할 BackgroundTile을 클릭하세요.",
                        MessageType.Info,
                        draw
                    );
                }

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "목적지를 Actor의 현재 좌표와 동일하게 지정하면 " +
                    "제자리 점프로 처리됩니다.",
                    MessageType.None,
                    draw
                );

                SerializedProperty moveActorIdProperty =
                    stepProperty.FindPropertyRelative(
                        "moveActorId"
                    );

                if (moveActorIdProperty != null &&
                    string.IsNullOrWhiteSpace(
                        moveActorIdProperty.stringValue))
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "MoveActor를 실행하려면 Actor ID가 필요합니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                break;


            case EventSceneStepType.AttackActor:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Attack Actor",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "attackActorId", "Attacker ID",
                    "attackTargetActorId", "Target ID",
                    "attackResult", "Attack Result"
                );

                currentY +=
                    4f;

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Attack Movement",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "attackApproachDuration", "Attack Duration",
                    "attackArcHeight", "Arc Height"
                );

                currentY +=
                    4f;

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Impact Screen Shake",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "attackTargetShakeDuration", "Shake Duration",
                    "attackTargetShakeIntensity", "Shake Strength"
                );

                SerializedProperty attackResultProperty =
                    stepProperty.FindPropertyRelative(
                        "attackResult"
                    );

                if (attackResultProperty != null &&
                    attackResultProperty.intValue ==
                        (int)EventSceneAttackResult.Failure)
                {
                    currentY +=
                        4f;

                    ProcessEventSceneSectionLabel(
                        ref currentY,
                        contentRect,
                        "Failure Impact",
                        draw
                    );

                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "attackApproachRatio", "Impact Ratio"
                    );

                    currentY +=
                        4f;

                    ProcessEventSceneSectionLabel(
                        ref currentY,
                        contentRect,
                        "Defense Bounce",
                        draw
                    );

                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "attackFailureFallShortDistance",
                        "Fall Short Distance",

                        "attackFailureFallBackDuration",
                        "Fall Back Duration",

                        "attackFailureFirstBounceDuration",
                        "First Bounce Duration",

                        "attackFailureFirstBounceHeight",
                        "First Bounce Height",

                        "attackFailureSecondBounceDuration",
                        "Second Bounce Duration",

                        "attackFailureSecondBounceHeight",
                        "Second Bounce Height",

                        "attackFailureFinalReturnDuration",
                        "Final Return Duration"
                    );
                }

                currentY +=
                    4f;

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Direction",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "attackChangeFlipX", "Change Flip X"
                );

                SerializedProperty attackChangeFlipProperty =
                    stepProperty.FindPropertyRelative(
                        "attackChangeFlipX"
                    );

                if (attackChangeFlipProperty != null &&
                    attackChangeFlipProperty.boolValue)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "attackFlipX", "Flip X"
                    );
                }

                SerializedProperty attackerIdProperty =
                    stepProperty.FindPropertyRelative(
                        "attackActorId"
                    );

                SerializedProperty targetIdProperty =
                    stepProperty.FindPropertyRelative(
                        "attackTargetActorId"
                    );

                string attackerId =
                    attackerIdProperty != null
                        ? attackerIdProperty.stringValue
                        : string.Empty;

                string targetId =
                    targetIdProperty != null
                        ? targetIdProperty.stringValue
                        : string.Empty;

                if (string.IsNullOrWhiteSpace(
                        attackerId))
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "AttackActor를 실행하려면 Attacker ID가 필요합니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                if (string.IsNullOrWhiteSpace(
                        targetId))
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "AttackActor를 실행하려면 Target ID가 필요합니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                if (string.IsNullOrWhiteSpace(
                        attackerId) == false &&
                    attackerId ==
                        targetId)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "Attacker와 Target에 동일한 Actor ID를 사용할 수 없습니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                if (attackResultProperty != null &&
                    attackResultProperty.intValue ==
                        (int)EventSceneAttackResult.Success)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "Success: Target 위치까지 공격한 뒤 Target을 즉시 제거하고 " +
                        "공격자가 해당 위치를 점유합니다.",
                        MessageType.Info,
                        draw
                    );
                }

                break;


            case EventSceneStepType.AbsorbActor:

                // 기존 enum 값은 유지하지만
                // 현재 기능 자체는 의도적으로 보류 중이다.
                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "AbsorbActor 기능은 현재 보류 상태입니다.",
                    MessageType.Warning,
                    draw
                );

                break;


            case EventSceneStepType.PlayActorAnimation:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Actor Animation",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "playAnimationActorId", "Actor ID"
                );

                SerializedProperty animationActorIdProperty =
                    stepProperty.FindPropertyRelative(
                        "playAnimationActorId"
                    );

                SerializedProperty animationNameProperty =
                    stepProperty.FindPropertyRelative(
                        "playAnimationName"
                    );

                string animationActorId =
                    animationActorIdProperty != null
                        ? animationActorIdProperty.stringValue
                        : string.Empty;

                string currentAnimationName =
                    animationNameProperty != null
                        ? animationNameProperty.stringValue
                        : string.Empty;

                string[] animationNames =
                    GetSpineAnimationNamesForActor(
                        eventData,
                        step,
                        animationActorId,
                        out string sourcePrefabName
                    );

                if (animationNames.Length > 0)
                {
                    List<string> popupOptions =
                        new List<string>();

                    popupOptions.Add(
                        "<Select Animation>"
                    );

                    for (int i = 0;
                         i < animationNames.Length;
                         i++)
                    {
                        popupOptions.Add(
                            animationNames[i]
                        );
                    }

                    int currentPopupIndex =
                        0;

                    for (int i = 0;
                         i < animationNames.Length;
                         i++)
                    {
                        if (animationNames[i] ==
                            currentAnimationName)
                        {
                            currentPopupIndex =
                                i + 1;

                            break;
                        }
                    }

                    int selectedPopupIndex =
                        ProcessEventScenePopup(
                            ref currentY,
                            contentRect,
                            "Spine Animation",
                            currentPopupIndex,
                            popupOptions.ToArray(),
                            draw
                        );

                    if (draw &&
                        animationNameProperty != null &&
                        selectedPopupIndex > 0 &&
                        selectedPopupIndex <=
                            animationNames.Length)
                    {
                        animationNameProperty.stringValue =
                            animationNames[
                                selectedPopupIndex - 1];

                        currentAnimationName =
                            animationNameProperty.stringValue;
                    }

                    if (string.IsNullOrWhiteSpace(
                            sourcePrefabName) == false)
                    {
                        ProcessEventSceneValueLabel(
                            ref currentY,
                            contentRect,
                            "Source Prefab",
                            sourcePrefabName,
                            draw
                        );
                    }

                    if (string.IsNullOrWhiteSpace(
                            currentAnimationName) == false &&
                        currentPopupIndex == 0)
                    {
                        ProcessEventSceneHelpBox(
                            ref currentY,
                            contentRect,
                            $"현재 Animation '{currentAnimationName}'을 " +
                            "Spine Prefab에서 찾을 수 없습니다.",
                            MessageType.Warning,
                            draw
                        );
                    }
                }
                else
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "playAnimationName", "Animation Name"
                    );

                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "이 Actor ID와 연결된 이전 SpawnActor의 " +
                        "Spine Prefab을 찾지 못했습니다.",
                        MessageType.Info,
                        draw
                    );
                }

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "playAnimationMixDuration", "Mix Duration",
                    "playAnimationLoop", "Loop"
                );

                SerializedProperty loopProperty =
                    stepProperty.FindPropertyRelative(
                        "playAnimationLoop"
                    );

                SerializedProperty waitProperty =
                    stepProperty.FindPropertyRelative(
                        "playAnimationWaitForComplete"
                    );

                SerializedProperty returnToIdleProperty =
                    stepProperty.FindPropertyRelative(
                        "playAnimationReturnToIdle"
                    );

                bool isLoop =
                    loopProperty != null &&
                    loopProperty.boolValue;

                if (isLoop == false)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "playAnimationWaitForComplete",
                        "Wait Until Complete"
                    );

                    if (waitProperty != null &&
                        waitProperty.boolValue)
                    {
                        ProcessEventSceneProperties(
                            ref currentY,
                            contentRect,
                            stepProperty,
                            draw,
                            "playAnimationReturnToIdle",
                            "Auto Return To Idle"
                        );
                    }
                }
                else if (draw)
                {
                    // 기존 Editor와 동일하게
                    // Loop에서는 Wait / Idle Return을 사용하지 않는다.
                    if (waitProperty != null)
                    {
                        waitProperty.boolValue =
                            false;
                    }

                    if (returnToIdleProperty != null)
                    {
                        returnToIdleProperty.boolValue =
                            false;
                    }
                }

                if (string.IsNullOrWhiteSpace(
                        animationActorId))
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "PlayActorAnimation을 실행하려면 Actor ID가 필요합니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                if (string.IsNullOrWhiteSpace(
                        currentAnimationName))
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "실행할 Spine Animation을 선택해주세요.",
                        MessageType.Warning,
                        draw
                    );
                }

                bool waitForAnimation =
                    waitProperty != null &&
                    waitProperty.boolValue;

                bool returnToIdle =
                    returnToIdleProperty != null &&
                    returnToIdleProperty.boolValue;

                if (isLoop)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "Loop Animation은 반복 재생을 시작한 뒤 " +
                        "즉시 다음 Event Step으로 진행합니다.",
                        MessageType.Info,
                        draw
                    );
                }
                else if (waitForAnimation)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        returnToIdle
                            ? "Animation 완료까지 기다린 뒤 Mix를 적용하여 Idle로 자동 복귀합니다."
                            : "Animation 완료 후 Idle로 복귀하지 않고 마지막 Pose를 유지합니다. " +
                              "다음 Animation Step은 이 Pose에서 바로 연결됩니다.",
                        MessageType.None,
                        draw
                    );
                }
                else
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "Animation 재생을 시작한 뒤 완료를 기다리지 않고 " +
                        "즉시 다음 Event Step으로 진행합니다.",
                        MessageType.None,
                        draw
                    );
                }

                break;


            case EventSceneStepType.SpeechBubble:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Speech Bubble",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "speechBubbleActorId", "Actor ID",
                    "speechBubbleDuration", "Duration",
                    "speechBubbleTypingSpeed", "Typing Speed",
                    "speechBubbleUseEmphasisShake",
                    "Use Emphasis Shake"
                );

                SerializedProperty emphasisProperty =
                    stepProperty.FindPropertyRelative(
                        "speechBubbleUseEmphasisShake"
                    );

                if (emphasisProperty != null &&
                    emphasisProperty.boolValue)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "speechBubbleEmphasisStrength",
                        "Emphasis Strength"
                    );
                }

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "speechBubbleWaitForComplete",
                    "Wait For Complete"
                );

                SerializedProperty speechActorIdProperty =
                    stepProperty.FindPropertyRelative(
                        "speechBubbleActorId"
                    );

                if (speechActorIdProperty != null &&
                    string.IsNullOrWhiteSpace(
                        speechActorIdProperty.stringValue))
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "SpeechBubble을 실행하려면 이전 SpawnActor에서 생성한 Actor ID가 필요합니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "Korean / English / Japanese Text는 " +
                    "아래 Speech Bubble Texts 영역에서 관리합니다.",
                    MessageType.Info,
                    draw
                );

                break;


            case EventSceneStepType.RemoveActor:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Remove Actor",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "removeActorId", "Actor ID",
                    "removeActorMode", "Remove Mode"
                );

                SerializedProperty removeModeProperty =
                    stepProperty.FindPropertyRelative(
                        "removeActorMode"
                    );

                bool useFadeOut =
                    removeModeProperty != null &&
                    removeModeProperty.intValue ==
                        (int)EventSceneRemoveMode.FadeOut;

                if (useFadeOut)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "removeActorFadeOutDuration",
                        "Fade Out Duration"
                    );
                }

                SerializedProperty removeActorIdProperty =
                    stepProperty.FindPropertyRelative(
                        "removeActorId"
                    );

                if (removeActorIdProperty != null &&
                    string.IsNullOrWhiteSpace(
                        removeActorIdProperty.stringValue))
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "RemoveActor를 실행하려면 Actor ID가 필요합니다.",
                        MessageType.Warning,
                        draw
                    );
                }

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    useFadeOut
                        ? "Actor가 지정 시간 동안 Fade Out된 뒤 Event Scene에서 제거됩니다."
                        : "Actor를 Fade 없이 즉시 Event Scene에서 제거합니다.",
                    MessageType.None,
                    draw
                );

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "Death 등의 Animation이 필요하면 RemoveActor 전에 " +
                    "PlayActorAnimation Step을 배치해주세요.",
                    MessageType.Info,
                    draw
                );

                break;


            case EventSceneStepType.CameraShot:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Camera Shot",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "cameraShotTargetType", "Target Type"
                );

                SerializedProperty cameraTargetTypeProperty =
                    stepProperty.FindPropertyRelative(
                        "cameraShotTargetType"
                    );

                bool isBackgroundTileTarget =
                    cameraTargetTypeProperty != null &&
                    cameraTargetTypeProperty.intValue ==
                        (int)EventSceneCameraShotTargetType.BackgroundTile;

                currentY +=
                    4f;

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Target",
                    draw
                );

                if (isBackgroundTileTarget)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "cameraShotTargetPosition",
                        "Background Tile"
                    );

                    bool isCameraSelecting =
                        isSelectingCameraShotTile &&
                        selectingCameraShotStepIndex ==
                            stepIndex;

                    if (ProcessEventSceneButton(
                            ref currentY,
                            contentRect,
                            isCameraSelecting
                                ? "Scene 타일 선택 취소"
                                : "Scene에서 Camera Target 선택",
                            draw))
                    {
                        if (isCameraSelecting)
                        {
                            CancelCameraShotTileSelection();
                        }
                        else
                        {
                            isSelectingSpawnActorTile =
                                false;

                            selectingSpawnActorStepIndex =
                                -1;

                            isSelectingMoveActorTile =
                                false;

                            selectingMoveActorStepIndex =
                                -1;

                            isSelectingCameraShotTile =
                                true;

                            selectingCameraShotStepIndex =
                                stepIndex;

                            SceneView.RepaintAll();
                        }
                    }

                    if (isCameraSelecting)
                    {
                        ProcessEventSceneHelpBox(
                            ref currentY,
                            contentRect,
                            "Scene View에서 화면 중앙에 표시할 " +
                            "BackgroundTile을 클릭하세요.",
                            MessageType.Info,
                            draw
                        );
                    }
                }
                else
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "cameraShotTargetActorId",
                        "Actor ID"
                    );

                    SerializedProperty targetActorIdProperty =
                        stepProperty.FindPropertyRelative(
                            "cameraShotTargetActorId"
                        );

                    if (targetActorIdProperty != null &&
                        string.IsNullOrWhiteSpace(
                            targetActorIdProperty.stringValue))
                    {
                        ProcessEventSceneHelpBox(
                            ref currentY,
                            contentRect,
                            "Actor Target을 사용하려면 이전 SpawnActor에서 " +
                            "생성한 Actor ID가 필요합니다.",
                            MessageType.Warning,
                            draw
                        );
                    }
                }

                currentY +=
                    4f;

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Camera",
                    draw
                );

                // cameraShotWorldOffset은 Serialized 호환을 위해 유지하지만
                // 현재 Event CameraShot Inspector에서는 노출하지 않는다.
                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "cameraShotWorldScale", "Zoom Scale",
                    "cameraShotDuration", "Move Duration",
                    "cameraShotWaitForComplete",
                    "Wait For Complete"
                );

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    isBackgroundTileTarget
                        ? "현재 Camera 위치에서 선택한 BackgroundTile 중심으로 이동하며, " +
                          "선택한 Tile이 화면 정중앙에 표시됩니다.\n" +
                          "Zoom Scale 값이 커질수록 화면이 확대됩니다."
                        : "현재 Camera 위치에서 선택한 Actor 위치로 이동하며, " +
                          "Actor가 화면 정중앙에 표시됩니다.\n" +
                          "Zoom Scale 값이 커질수록 화면이 확대됩니다.",
                    MessageType.Info,
                    draw
                );

                break;


            // =====================================================
            // Screen Shake
            // =====================================================

            case EventSceneStepType.ScreenShake:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Screen Shake",
                    draw
                );

                // <변경부분>
                // 기존 시간제와 지속 흔들림 중 사용할 방식을 선택한다.
                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "screenShakeContinuous", "Continuous"
                );

                SerializedProperty screenShakeContinuousProperty =
                    stepProperty.FindPropertyRelative(
                        "screenShakeContinuous"
                    );

                bool isContinuousScreenShake =
                    screenShakeContinuousProperty != null &&
                    screenShakeContinuousProperty.boolValue;

                if (isContinuousScreenShake)
                {
                    // <변경부분>
                    // 지속 모드에서는 Duration과 대기 옵션이 필요 없다.
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "screenShakeStrength", "Shake Strength"
                    );

                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "지속 화면 흔들림을 시작합니다.\n" +
                        "흔들림을 유지한 채 다음 Step으로 진행하며,\n" +
                        "StopScreenShake Step에서 종료합니다.",
                        MessageType.Info,
                        draw
                    );
                }
                else
                {
                    // <변경부분>
                    // 기존 시간제 ScreenShake 설정을 그대로 제공한다.
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "screenShakeDuration", "Shake Duration",
                        "screenShakeStrength", "Shake Strength",
                        "screenShakeWaitForComplete",
                        "Wait For Complete"
                    );

                    SerializedProperty screenShakeWaitProperty =
                        stepProperty.FindPropertyRelative(
                            "screenShakeWaitForComplete"
                        );

                    bool waitForShake =
                        screenShakeWaitProperty != null &&
                        screenShakeWaitProperty.boolValue;

                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        waitForShake
                            ? "화면 흔들림이 끝난 뒤 다음 Event Step으로 진행합니다."
                            : "화면 흔들림을 시작한 뒤 기다리지 않고 다음 Event Step으로 진행합니다.",
                        MessageType.Info,
                        draw
                    );
                }

                break;


            // =====================================================
            // Stop Screen Shake
            // =====================================================

            case EventSceneStepType.StopScreenShake:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Stop Screen Shake",
                    draw
                );

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "진행 중인 화면 흔들림을 즉시 종료합니다.\n" +
                    "카메라의 흔들림 위치를 원래대로 복구합니다.",
                    MessageType.Info,
                    draw
                );

                break;


            // <변경부분>
            // Event Scene 전용 ScreenGlitch 설정.
            case EventSceneStepType.ScreenGlitch:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Screen Glitch",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "screenGlitchStrength", "Glitch Strength",
                    "screenGlitchDuration", "Glitch Duration",
                    "screenGlitchWaitForComplete",
                    "Wait For Complete"
                );

                SerializedProperty screenGlitchWaitProperty =
                    stepProperty.FindPropertyRelative(
                        "screenGlitchWaitForComplete"
                    );

                bool waitForGlitch =
                    screenGlitchWaitProperty != null &&
                    screenGlitchWaitProperty.boolValue;

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    waitForGlitch
                        ? "화면 글리치가 끝난 뒤 다음 Event Step으로 진행합니다."
                        : "화면 글리치를 시작한 뒤 기다리지 않고 다음 Event Step으로 진행합니다.",
                    MessageType.Info,
                    draw
                );

                break;


            // <변경부분>
            // <변경부분>
            // <변경부분>
            // Global Ambient 조명과 Screen Flash를 각각 ON/OFF한다.
            case EventSceneStepType.GlobalLighting:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Global Lighting",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "globalLightingUseGlobalLight", "Use Global Light"
                );

                SerializedProperty useGlobalLightProperty =
                    stepProperty.FindPropertyRelative(
                        "globalLightingUseGlobalLight"
                    );

                bool useGlobalLight =
                    useGlobalLightProperty != null &&
                    useGlobalLightProperty.boolValue;

                // <변경부분>
                // Global Light가 ON일 때만 기존 조명 설정을 표시한다.
                if (useGlobalLight)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "globalLightingTargetColor", "Target Color",
                        "globalLightingTargetIntensity", "Target Intensity",
                        "globalLightingFadeDuration", "Fade Duration"
                    );
                }

                currentY += 5f;

                // <변경부분>
                // EventScene에서도 Character_Fill 조명을 독립적으로 제어한다.
                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Character Fill",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "globalLightingUseCharacterFill",
                    "Use Character Fill"
                );

                SerializedProperty useCharacterFillProperty =
                    stepProperty.FindPropertyRelative(
                        "globalLightingUseCharacterFill"
                    );

                bool useCharacterFill =
                    useCharacterFillProperty != null &&
                    useCharacterFillProperty.boolValue;

                if (useCharacterFill)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "globalLightingCharacterFillTargetColor",
                        "Fill Color",
                        "globalLightingCharacterFillTargetIntensity",
                        "Fill Intensity",
                        "globalLightingCharacterFillFadeDuration",
                        "Fill Fade Duration"
                    );
                }

                currentY += 5f;

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Screen Flash",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "globalLightingUseScreenFlash", "Use Screen Flash"
                );

                SerializedProperty useFlashProperty =
                    stepProperty.FindPropertyRelative(
                        "globalLightingUseScreenFlash"
                    );

                bool useScreenFlash =
                    useFlashProperty != null &&
                    useFlashProperty.boolValue;

                if (useScreenFlash)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "globalLightingFlashColor", "Flash Color",
                        "globalLightingFlashStrength", "Flash Strength",
                        "globalLightingFlashDuration", "Flash Duration"
                    );
                }

                // <변경부분>
                // 세 효과 중 하나라도 활성화되어 있으면 대기 옵션 표시.
                if (useGlobalLight || useCharacterFill || useScreenFlash)
                {
                    currentY += 5f;

                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "globalLightingWaitForComplete", "Wait For Complete"
                    );
                }

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    useGlobalLight || useCharacterFill || useScreenFlash
                        ? "Global Light, Character Fill, Screen Flash는 각각 선택적으로 실행됩니다.\n" +
                          "Wait For Complete는 활성화된 모든 효과가 끝날 때까지 기다립니다."
                        : "Global Light, Character Fill, Screen Flash가 모두 OFF입니다. " +
                          "이 스텝에서는 아무 연출도 실행하지 않습니다.",
                    useGlobalLight || useCharacterFill || useScreenFlash
                        ? MessageType.Info
                        : MessageType.None,
                    draw
                );

                break;


            case EventSceneStepType.CompleteSequence:

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "현재 Event Scene Sequence를 완료합니다.",
                    MessageType.Info,
                    draw
                );

                break;


            case EventSceneStepType.UnlockPlayerInteraction:

                // <변경부분>
                // 일반 상호작용 Unlock과 수동 Camera 입력 Unlock을
                // 개별적으로 제어할 수 있도록 설정을 추가한다.
                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Camera Control",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "unlockPlayerInteractionLockManualCamera",
                    "Lock Camera Movement / Zoom"
                );

                // <변경부분>
                // 카메라 잠금 여부에 따라 설명을 구분한다.
                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    step.unlockPlayerInteractionLockManualCamera
                        ? "플레이어의 일반 UI / Scene 상호작용을 허용합니다.\n" +
                          "수동 Camera 드래그 / 휠 줌 / 핀치 줌은 잠급니다.\n" +
                          "CameraShot 등의 이벤트 연출은 정상 실행됩니다."
                        : "플레이어의 일반 UI / Scene 상호작용을 허용합니다.\n" +
                          "수동 Camera 드래그 / 휠 줌 / 핀치 줌도 허용합니다.",
                    MessageType.Info,
                    draw
                );

                break;


            case EventSceneStepType.LockPlayerInteraction:

                ProcessEventSceneHelpBox(
                    ref currentY,
                    contentRect,
                    "Event Scene의 일반 플레이어 조작을 다시 잠급니다.\n" +
                    "자유 상호작용 이후 Dialogue / Camera 연출 등으로 다시 진입할 때 사용합니다.",
                    MessageType.Info,
                    draw
                );

                break;


            // <변경부분>
            // 전리품 보상과 가상 흡수량 기반 기물 복구를 독립 설정한다.
            case EventSceneStepType.RelicReward:
                ProcessEventSceneProperties(ref currentY, contentRect, stepProperty, draw, "relicRewardList", "유물 보상 목록");
                break;
            case EventSceneStepType.ShowRewardSelection:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Reward Selection",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "rewardSelectionUseDropRewards", "Use Drop Rewards"
                );

                SerializedProperty useDropProperty =
                    stepProperty.FindPropertyRelative("rewardSelectionUseDropRewards");

                bool useDropRewards =
                    useDropProperty != null && useDropProperty.boolValue;

                if (useDropRewards)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "rewardSelectionData", "Battle Reward Data"
                    );
                }

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "rewardSelectionUseRestoration", "Use Restoration"
                );

                SerializedProperty useRestorationProperty =
                    stepProperty.FindPropertyRelative("rewardSelectionUseRestoration");

                bool useRestoration =
                    useRestorationProperty != null && useRestorationProperty.boolValue;

                if (useRestoration)
                {
                    ProcessEventSceneProperties(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        draw,
                        "rewardSelectionRestorationAbsorbCount", "Restoration Absorb Count"
                    );
                }

                if (useDropRewards && step.rewardSelectionData == null)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "Use Drop Rewards가 ON입니다. BattleRewardData를 연결해 주세요.",
                        MessageType.Warning,
                        draw
                    );
                }
                else if (!useDropRewards && !useRestoration)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "두 보상 모두 OFF이므로 팝업 없이 다음 Step으로 이동합니다.",
                        MessageType.Info,
                        draw
                    );
                }
                else
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "전리품은 선택 수령, 복구는 팝업 표시 시 즉시 지급됩니다.\n" +
                        "가상 흡수 2회당 기물 1개, 최대 보유 10개입니다. " +
                        "가상 흡수량은 전투 기록을 변경하지 않습니다.",
                        MessageType.Info,
                        draw
                    );
                }

                break;


            // <변경부분>
            // Event Scene에서 지정된 StageBattleData를 사용해
            // BattleScene으로 이동하는 Step.
            case EventSceneStepType.StartBattle:

                ProcessEventSceneSectionLabel(
                    ref currentY,
                    contentRect,
                    "Start Battle",
                    draw
                );

                ProcessEventSceneProperties(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw,
                    "worldMapEventData",
                    "월드맵 이벤트 데이터 (선택)",
                    "startBattleStageData",
                    "Stage Battle Data",
                    "clearCurrentWorldMapNodeOnVictory",
                    "Clear Current Node On Victory"
                );

                if (step.startBattleStageData == null)
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "StageBattleData가 연결되지 않았습니다.\n" +
                        "BattleScene에서 사용할 전투 데이터 Asset을 연결해 주세요.",
                        MessageType.Warning,
                        draw
                    );
                }
                else
                {
                    ProcessEventSceneHelpBox(
                        ref currentY,
                        contentRect,
                        "지정한 StageBattleData를 BattleScene으로 전달합니다.\n" +
                        "ON: 월드맵에서 진입한 이벤트 노드를 전투 승리 후 클리어합니다.\n" +
                        "OFF: 월드맵 노드 클리어와 관계없는 독립 전투로 처리합니다.\n" +
                        "전투 승리 후 목적지는 StageBattleData의 Victory Scene Name을 사용합니다.",
                        MessageType.Info,
                        draw
                    );
                }

                break;
        }

        // <변경부분>
        // 펼쳐진 모든 Event Scene Step의 맨 아래에
        // 현재 Step을 바로 아래로 복제하는 버튼을 표시한다.
        //
        // ProcessExpandedEventSceneStep은 높이 계산에도 사용되므로
        // 버튼의 높이 역시 동일한 공용 함수를 통해 계산한다.
        currentY += 8f;

        if (ProcessEventSceneButton(
                ref currentY,
                contentRect,
                "Duplicate Step Below",
                draw))
        {
            // <변경부분>
            // ReorderableList Draw 도중 직접 Insert하지 않고
            // 목록 그리기가 끝난 뒤 처리하도록 예약한다.
            pendingDuplicateStepIndex = stepIndex;
        }

        return
            Mathf.Max(
                40f,
                currentY - startY
            );
    }


    // <변경부분>
    // propertyName / displayName을 한 쌍으로 받아
    // 여러 SerializedProperty를 연속으로 출력한다.
    private void ProcessEventSceneProperties(
        ref float currentY,
        Rect contentRect,
        SerializedProperty stepProperty,
        bool draw,
        params string[] propertyAndLabelPairs)
    {
        if (propertyAndLabelPairs == null)
        {
            return;
        }

        for (int i = 0;
             i + 1 < propertyAndLabelPairs.Length;
             i += 2)
        {
            ProcessEventSceneProperty(
                ref currentY,
                contentRect,
                stepProperty,
                propertyAndLabelPairs[i],
                propertyAndLabelPairs[i + 1],
                draw
            );
        }
    }


    // <변경부분>
    // Rect 기반 SerializedProperty 출력.
    private void ProcessEventSceneProperty(
        ref float currentY,
        Rect contentRect,
        SerializedProperty stepProperty,
        string propertyName,
        string displayName,
        bool draw)
    {
        if (stepProperty == null ||
            string.IsNullOrWhiteSpace(
                propertyName))
        {
            return;
        }

        SerializedProperty property =
            stepProperty.FindPropertyRelative(
                propertyName
            );

        if (property == null)
        {
            ProcessEventSceneHelpBox(
                ref currentY,
                contentRect,
                $"Step Property를 찾을 수 없습니다: {propertyName}",
                MessageType.Warning,
                draw
            );

            return;
        }

        GUIContent label =
            new GUIContent(
                displayName
            );

        // Min / Range 등의 기존 Property Attribute도
        // SerializedProperty를 통해 그대로 적용된다.
        float propertyHeight =
            EditorGUI.GetPropertyHeight(
                property,
                label,
                false
            );

        if (draw)
        {
            EditorGUI.PropertyField(
                new Rect(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    propertyHeight
                ),
                property,
                label,
                false
            );
        }

        currentY +=
            propertyHeight +
            EditorGUIUtility.standardVerticalSpacing;
    }


    // <변경부분>
    // Rect 기반 Section Label.
    private void ProcessEventSceneSectionLabel(
        ref float currentY,
        Rect contentRect,
        string label,
        bool draw)
    {
        float lineHeight =
            EditorGUIUtility.singleLineHeight;

        if (draw)
        {
            EditorGUI.LabelField(
                new Rect(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    lineHeight
                ),
                label,
                EditorStyles.boldLabel
            );
        }

        currentY +=
            lineHeight +
            EditorGUIUtility.standardVerticalSpacing;
    }


    // <변경부분>
    // Rect 기반 HelpBox.
    private void ProcessEventSceneHelpBox(
        ref float currentY,
        Rect contentRect,
        string message,
        MessageType messageType,
        bool draw)
    {
        float helpBoxHeight =
            Mathf.Max(
                32f,
                EditorStyles.helpBox.CalcHeight(
                    new GUIContent(
                        message
                    ),
                    Mathf.Max(
                        120f,
                        contentRect.width
                    )
                ) +
                6f
            );

        if (draw)
        {
            EditorGUI.HelpBox(
                new Rect(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    helpBoxHeight
                ),
                message,
                messageType
            );
        }

        currentY +=
            helpBoxHeight +
            EditorGUIUtility.standardVerticalSpacing;
    }


    // <변경부분>
    // Rect 기반 Button.
    private bool ProcessEventSceneButton(
        ref float currentY,
        Rect contentRect,
        string label,
        bool draw)
    {
        float buttonHeight =
            EditorGUIUtility.singleLineHeight +
            2f;

        bool clicked =
            false;

        if (draw)
        {
            clicked =
                GUI.Button(
                    new Rect(
                        contentRect.x,
                        currentY,
                        contentRect.width,
                        buttonHeight
                    ),
                    label
                );
        }

        currentY +=
            buttonHeight +
            EditorGUIUtility.standardVerticalSpacing;

        return clicked;
    }


    // <변경부분>
    // Rect 기반 Popup.
    private int ProcessEventScenePopup(
        ref float currentY,
        Rect contentRect,
        string displayName,
        int selectedIndex,
        string[] options,
        bool draw)
    {
        float lineHeight =
            EditorGUIUtility.singleLineHeight;

        int result =
            selectedIndex;

        if (draw)
        {
            result =
                EditorGUI.Popup(
                    new Rect(
                        contentRect.x,
                        currentY,
                        contentRect.width,
                        lineHeight
                    ),
                    displayName,
                    selectedIndex,
                    options
                );
        }

        currentY +=
     lineHeight +
     EditorGUIUtility.standardVerticalSpacing;

        return result;
    }


    // <변경부분>
    // Unity 프로젝트에 실제 등록된 Sorting Layer를 표시한다.
    //
    // 첫 번째 옵션인 Auto는 명시적 Layer Override를 사용하지 않고
    // BackgroundManager의 기존 정렬 계산을 따른다는 의미다.
    //
    // 기존 ReorderableList와 동일한 Rect 기반으로 그리므로
    // Element 높이 계산과 실제 GUI 높이가 일치한다.
    private void ProcessEventActorSortingLayerPopup(
        ref float currentY,
        Rect contentRect,
        SerializedProperty stepProperty,
        string propertyName,
        bool draw)
    {
        SerializedProperty layerNameProperty =
            stepProperty.FindPropertyRelative(
                propertyName
            );

        if (layerNameProperty == null)
        {
            ProcessEventSceneHelpBox(
                ref currentY,
                contentRect,
                $"Sorting Layer Property를 찾을 수 없습니다: {propertyName}",
                MessageType.Warning,
                draw
            );

            return;
        }

        List<string> layerNames =
            new List<string>
            {
                string.Empty
            };

        List<string> layerLabels =
            new List<string>
            {
                "Auto (Existing Depth Sorting)"
            };

        SortingLayer[] sortingLayers =
            SortingLayer.layers;

        for (int i = 0;
             i < sortingLayers.Length;
             i++)
        {
            layerNames.Add(
                sortingLayers[i].name
            );

            layerLabels.Add(
                sortingLayers[i].name
            );
        }

        string currentLayerName =
            layerNameProperty.stringValue ??
            string.Empty;

        int selectedIndex =
            layerNames.IndexOf(
                currentLayerName
            );

        // <변경부분>
        // 기존에 선택한 Layer가 삭제되었더라도
        // 자동으로 다른 Layer로 바꾸지 않고 상태를 보여준다.
        if (selectedIndex < 0)
        {
            layerNames.Add(
                currentLayerName
            );

            layerLabels.Add(
                $"<Missing Layer: {currentLayerName}>"
            );

            selectedIndex =
                layerNames.Count - 1;
        }

        int newIndex =
            ProcessEventScenePopup(
                ref currentY,
                contentRect,
                "Sorting Layer",
                selectedIndex,
                layerLabels.ToArray(),
                draw
            );

        if (draw &&
            newIndex != selectedIndex &&
            newIndex >= 0 &&
            newIndex < layerNames.Count)
        {
            // 변경 내용은 기존 DrawEventSteps의
            // serializedObject.ApplyModifiedProperties()에서 저장된다.
            layerNameProperty.stringValue =
                layerNames[newIndex];
        }
    }


    // <변경부분>
    // Source Prefab 같은 읽기 전용 값을 한 줄로 표시한다.
    private void ProcessEventSceneValueLabel(
        ref float currentY,
        Rect contentRect,
        string label,
        string value,
        bool draw)
    {
        float lineHeight =
            EditorGUIUtility.singleLineHeight;

        if (draw)
        {
            EditorGUI.LabelField(
                new Rect(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    lineHeight
                ),
                label,
                value
            );
        }

        currentY +=
            lineHeight +
            EditorGUIUtility.standardVerticalSpacing;
    }


    private void DrawEventSceneStepFoldoutControls(
        int stepCount)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(
                    "▼ 전체 펼치기"))
            {
                SetAllEventSceneStepFoldouts(
                    stepCount,
                    true
                );
            }

            if (GUILayout.Button(
                    "▲ 전체 접기"))
            {
                SetAllEventSceneStepFoldouts(
                    stepCount,
                    false
                );
            }
        }
    }


    // <변경부분>
    // 모든 Event Scene Step Foldout 상태를
    // 한 번에 변경한다.
    private void SetAllEventSceneStepFoldouts(
     int stepCount,
     bool expanded)
    {
        stepFoldouts.Clear();

        for (int i = 0;
             i < stepCount;
             i++)
        {
            stepFoldouts[i] =
                expanded;
        }

        Repaint();
    }


    // <변경부분>
    // Event Scene의 Dialogue Step만 대상으로
    // 전체 펼치기 / 전체 접기 버튼을 표시한다.
    //
    // 메인 Event Steps Foldout과는 별개의 상태이며,
    // SpeechBubble Foldout에는 영향을 주지 않는다.
    private void DrawDialogueStepFoldoutControls(
        EventSceneData eventData)
    {
        if (eventData == null ||
            eventData.steps == null)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(
                    "▼ Dialogue 전체 펼치기"))
            {
                SetAllDialogueStepFoldouts(
                    eventData,
                    true
                );
            }

            if (GUILayout.Button(
                    "▲ Dialogue 전체 접기"))
            {
                SetAllDialogueStepFoldouts(
                    eventData,
                    false
                );
            }
        }
    }


    // <변경부분>
    // Dialogue 타입인 Step의 Foldout 상태만
    // 한 번에 변경한다.
    private void SetAllDialogueStepFoldouts(
        EventSceneData eventData,
        bool expanded)
    {
        if (eventData == null ||
            eventData.steps == null)
        {
            return;
        }

        for (int i = 0;
             i < eventData.steps.Count;
             i++)
        {
            EventSceneStepData step =
                eventData.steps[i];

            if (step == null ||
                step.stepType !=
                    EventSceneStepType.Dialogue)
            {
                continue;
            }

            dialogueStepFoldouts[i] =
                expanded;
        }

        Repaint();
    }


    // <변경부분>
    // Event Scene 전체의 Dialogue Step만 따로 모아서 관리한다.
    //
    // 기존 Tutorial EventSequenceDataEditor와 동일하게
    // 일반 Step 목록과 Dialogue 제작 영역을 분리하여
    // Step Inspector가 지나치게 길어지는 것을 방지한다.
    private void DrawDialoguePages(
        EventSceneData eventData)
    {
        EditorGUILayout.LabelField(
            "Dialogue Pages",
            EditorStyles.boldLabel
        );

        if (eventData == null ||
    eventData.steps == null ||
    eventData.steps.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Event Step이 없습니다.",
                MessageType.Info
            );

            return;
        }

        // <변경부분>
        // Dialogue 목록 최상단에서도
        // 모든 Dialogue Step을 한 번에 펼치거나 접을 수 있다.
        DrawDialogueStepFoldoutControls(
            eventData
        );

        EditorGUILayout.Space(4);

        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        bool foundDialogueStep =
            false;

        for (int stepIndex = 0;
             stepIndex < eventData.steps.Count;
             stepIndex++)
        {
            EventSceneStepData step =
                eventData.steps[stepIndex];

            if (step == null ||
                step.stepType !=
                    EventSceneStepType.Dialogue)
            {
                continue;
            }

            foundDialogueStep =
                true;

            bool expanded =
                GetDialogueStepFoldout(
                    stepIndex
                );

            string safeStepName =
     string.IsNullOrWhiteSpace(
         step.stepName)
         ? "Dialogue"
         : step.stepName;

            // <변경부분>
            // 메인 Event Steps와 동일하게
            // 1부터 시작하는 두 자리 Step 번호를 표시한다.
            //
            // 예:
            // Step01 - 시작 대사
            // Step02 - 설명
            string stepNumber =
                (stepIndex + 1).ToString(
                    "D2"
                );

            string stepLabel =
                $"Step{stepNumber} - {safeStepName}";

            expanded =
                EditorGUILayout.Foldout(
                    expanded,
                    stepLabel,
                    true
                );

            dialogueStepFoldouts[
                stepIndex] =
                    expanded;

            if (expanded == false)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                if (string.IsNullOrWhiteSpace(
                        step.dialogueLocalizationId))
                {
                    EditorGUILayout.LabelField(
                        "Step Localization ID",
                        "Localization 동기화 시 자동 생성"
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Step Localization ID",
                        step.dialogueLocalizationId
                    );
                }

                if (step.dialoguePages == null)
                {
                    step.dialoguePages =
                        new List<string>();
                }

                if (step.dialogueLocalizationPages == null)
                {
                    step.dialogueLocalizationPages =
                        new List<
                            EventSceneDialoguePageLocalizationData
                        >();
                }

                EditorGUILayout.Space(4);

                for (int pageIndex = 0;
                     pageIndex <
                     step.dialoguePages.Count;
                     pageIndex++)
                {
                    bool listChanged =
                        DrawDialoguePage(
                            eventData,
                            step,
                            pageIndex,
                            collection
                        );

                    if (listChanged)
                    {
                        return;
                    }

                    EditorGUILayout.Space(8);
                }

                if (GUILayout.Button(
        "+ Dialogue Page 추가"))
                {
                    AddDialoguePage(
                        eventData,
                        step
                    );

                    GUIUtility.ExitGUI();
                    return;
                }

                // <변경부분>
                // 이 Dialogue Step의 마지막 페이지에서
                // 사용할 선택지와 분기 목적지를 편집한다.
                //
                // Dialogue 원문 편집 영역을 유지하면서
                // 그 바로 아래에 선택지를 추가한다.
                EditorGUILayout.Space(10);

                DrawDialogueChoices(
                    eventData,
                    step,
                    collection
                );
            }

            EditorGUILayout.Space(6);
        }

        // <변경부분>
        // Dialogue 목록 맨 아래까지 내려온 상태에서도
        // 다시 위로 올라가지 않고 전체 펼치기 / 접기를 사용할 수 있다.
        if (foundDialogueStep)
        {
            EditorGUILayout.Space(4);

            DrawDialogueStepFoldoutControls(
                eventData
            );
        }

        if (foundDialogueStep == false)
        {
            EditorGUILayout.HelpBox(
                "현재 EventSceneData에 Dialogue Step이 없습니다.",
                MessageType.Info
            );
        }
    }


    // <변경부분>
    // Dialogue 마지막 페이지에 표시할 선택지를 관리한다.
    //
    // 선택지 추가 / 삭제 / 순서 이동,
    // 한국어 원문 / 분기 대상 / 영어·일본어 번역을
    // 기존 EventSceneData Inspector에서 편집한다.
    private void DrawDialogueChoices(
        EventSceneData eventData,
        EventSceneStepData step,
        StringTableCollection collection)
    {
        EditorGUILayout.LabelField(
            "Dialogue Choices",
            EditorStyles.boldLabel
        );

        List<EventSceneDialogueChoiceData> choices =
            step.dialogueChoices;

        int choiceCount =
            choices != null
                ? choices.Count
                : 0;

        if (choiceCount == 0)
        {
            EditorGUILayout.HelpBox(
                "선택지가 없습니다. 기존 Dialogue 방식으로 진행합니다.",
                MessageType.None
            );
        }

        if (step.dialoguePages == null ||
            step.dialoguePages.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "선택지를 표시하려면 Dialogue Page가 최소 1개 필요합니다.",
                MessageType.Warning
            );
        }

        for (int choiceIndex = 0;
             choiceIndex < choiceCount;
             choiceIndex++)
        {
            EventSceneDialogueChoiceData choice =
                choices[choiceIndex];

            if (choice == null)
            {
                EditorGUILayout.HelpBox(
                    $"Choice {choiceIndex + 1} 데이터가 비어 있습니다.",
                    MessageType.Warning
                );

                continue;
            }

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                // =============================================
                // Choice Header
                // =============================================

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    $"Choice {choiceIndex + 1}",
                    EditorStyles.boldLabel
                );

                bool moveUp;
                bool moveDown;

                using (new EditorGUI.DisabledScope(
                           choiceIndex == 0))
                {
                    moveUp =
                        GUILayout.Button(
                            "▲",
                            GUILayout.Width(28)
                        );
                }

                using (new EditorGUI.DisabledScope(
                           choiceIndex >= choiceCount - 1))
                {
                    moveDown =
                        GUILayout.Button(
                            "▼",
                            GUILayout.Width(28)
                        );
                }

                bool remove =
                    GUILayout.Button(
                        "삭제",
                        GUILayout.Width(45)
                    );

                EditorGUILayout.EndHorizontal();

                if (moveUp || moveDown || remove)
                {
                    Undo.RecordObject(
                        eventData,
                        "Edit Event Scene Dialogue Choices"
                    );

                    if (remove)
                    {
                        choices.RemoveAt(
                            choiceIndex
                        );
                    }
                    else
                    {
                        int targetIndex =
                            moveUp
                                ? choiceIndex - 1
                                : choiceIndex + 1;

                        EventSceneDialogueChoiceData temp =
                            choices[targetIndex];

                        choices[targetIndex] =
                            choice;

                        choices[choiceIndex] =
                            temp;
                    }

                    EditorUtility.SetDirty(
                        eventData
                    );

                    GUIUtility.ExitGUI();
                    return;
                }

                // =============================================
                // Korean
                // =============================================

                EditorGUILayout.LabelField(
                    "Korean"
                );

                EditorGUI.BeginChangeCheck();

                string newKorean =
                    EditorGUILayout.TextArea(
                        choice.koreanText ?? string.Empty,
                        GUILayout.MinHeight(45)
                    );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        eventData,
                        "Edit Event Scene Choice Korean"
                    );

                    choice.koreanText =
                        newKorean;

                    EditorUtility.SetDirty(
                        eventData
                    );
                }

                // =============================================
                // Target Step
                // =============================================

                List<string> targetIds =
                    new List<string>
                    {
                        string.Empty
                    };

                List<string> targetLabels =
                    new List<string>
                    {
                        "<Select Target Step>"
                    };

                if (eventData.steps != null)
                {
                    for (int i = 0;
                         i < eventData.steps.Count;
                         i++)
                    {
                        EventSceneStepData targetStep =
                            eventData.steps[i];

                        if (targetStep == null ||
                            string.IsNullOrWhiteSpace(
                                targetStep.stepId))
                        {
                            continue;
                        }

                        string targetName =
                            string.IsNullOrWhiteSpace(
                                targetStep.stepName)
                                ? targetStep.stepType.ToString()
                                : targetStep.stepName;

                        targetIds.Add(
                            targetStep.stepId
                        );

                        targetLabels.Add(
                            $"Step{(i + 1):D2} - " +
                            $"{targetName} " +
                            $"[{targetStep.stepType}]"
                        );
                    }
                }

                string currentTargetId =
                    choice.targetStepId ??
                    string.Empty;

                int selectedTargetIndex =
                    targetIds.IndexOf(
                        currentTargetId
                    );

                bool missingTarget =
                    selectedTargetIndex < 0;

                if (missingTarget)
                {
                    targetIds.Add(
                        currentTargetId
                    );

                    targetLabels.Add(
                        $"<Missing Target: {currentTargetId}>"
                    );

                    selectedTargetIndex =
                        targetIds.Count - 1;
                }

                EditorGUI.BeginChangeCheck();

                int newTargetIndex =
                    EditorGUILayout.Popup(
                        "Target Step",
                        selectedTargetIndex,
                        targetLabels.ToArray()
                    );

                if (EditorGUI.EndChangeCheck() &&
                    newTargetIndex >= 0 &&
                    newTargetIndex < targetIds.Count)
                {
                    Undo.RecordObject(
                        eventData,
                        "Edit Event Scene Choice Target"
                    );

                    choice.targetStepId =
                        targetIds[newTargetIndex];

                    EditorUtility.SetDirty(
                        eventData
                    );
                }

                if (string.IsNullOrWhiteSpace(
                        currentTargetId))
                {
                    EditorGUILayout.HelpBox(
                        "분기할 Target Step을 선택해 주세요.",
                        MessageType.Warning
                    );
                }
                else if (missingTarget)
                {
                    EditorGUILayout.HelpBox(
                        "지정한 Target Step을 찾을 수 없습니다. " +
                        "삭제되었거나 ID가 변경되었는지 확인해 주세요.",
                        MessageType.Warning
                    );
                }
                else if (currentTargetId == step.stepId)
                {
                    EditorGUILayout.HelpBox(
                        "자기 자신의 Dialogue Step으로 이동합니다. " +
                        "반복 분기가 의도된 것인지 확인해 주세요.",
                        MessageType.Warning
                    );
                }

                // =============================================
                // Localization
                // =============================================

                string key =
                    GetDialogueChoiceKey(
                        eventData,
                        choice
                    );

                if (string.IsNullOrWhiteSpace(
                        key))
                {
                    EditorGUILayout.HelpBox(
                        "Localization 생성 / 한국어 동기화를 실행하면 " +
                        "선택지의 고유 번역 키와 번역 입력란이 준비됩니다.",
                        MessageType.None
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Key",
                        key
                    );

                    if (collection == null)
                    {
                        EditorGUILayout.HelpBox(
                            "EventScene_Dialogue Collection을 찾을 수 없습니다.",
                            MessageType.Warning
                        );
                    }
                    else
                    {
                        EditorGUILayout.Space(3);

                        DrawLocaleTranslation(
                            collection,
                            "English",
                            "en",
                            key
                        );

                        EditorGUILayout.Space(4);

                        DrawLocaleTranslation(
                            collection,
                            "Japanese",
                            "ja",
                            key
                        );
                    }
                }
            }

            EditorGUILayout.Space(5);
        }

        // =============================================
        // Add Choice
        // =============================================

        if (GUILayout.Button(
                "+ Dialogue Choice 추가"))
        {
            Undo.RecordObject(
                eventData,
                "Add Event Scene Dialogue Choice"
            );

            if (step.dialogueChoices == null)
            {
                step.dialogueChoices =
                    new List<EventSceneDialogueChoiceData>();
            }

            step.dialogueChoices.Add(
                new EventSceneDialogueChoiceData
                {
                    localizationId =
                        CreateStableId("choice"),

                    koreanText =
                        string.Empty,

                    localizedText =
                        new LocalizedString(),

                    targetStepId =
                        string.Empty
                }
            );

            EditorUtility.SetDirty(
                eventData
            );

            GUIUtility.ExitGUI();
        }
    }


    // <변경부분>
    // Standalone Event Scene의 SpeechBubble 한국어 원문과
    // EN / JA 번역을 EventScene_Dialogue Collection에서 관리한다.
    //
    // Step Index를 Localization Key에 사용하지 않고
    // speechBubbleLocalizationId를 Stable ID로 사용하므로
    // Step 순서가 변경되어도 기존 번역 연결을 유지한다.
    private void DrawSpeechBubbleTexts(
        EventSceneData eventData)
    {
        EditorGUILayout.LabelField(
            "Speech Bubble Texts",
            EditorStyles.boldLabel
        );

        if (eventData == null ||
            eventData.steps == null ||
            eventData.steps.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Event Step이 없습니다.",
                MessageType.Info
            );

            return;
        }

        bool foundSpeechBubbleStep =
            false;

        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        for (int stepIndex = 0;
             stepIndex < eventData.steps.Count;
             stepIndex++)
        {
            EventSceneStepData step =
                eventData.steps[stepIndex];

            if (step == null ||
                step.stepType !=
                    EventSceneStepType.SpeechBubble)
            {
                continue;
            }

            foundSpeechBubbleStep =
                true;

            bool expanded =
                GetDialogueStepFoldout(
                    stepIndex
                );

            string safeStepName =
    string.IsNullOrWhiteSpace(
        step.stepName)
        ? "SpeechBubble"
        : step.stepName;

            // <변경부분>
            // 메인 Event Steps와 동일한
            // 1-based 두 자리 Step 번호를 사용한다.
            string stepNumber =
                (stepIndex + 1).ToString(
                    "D2"
                );

            expanded =
                EditorGUILayout.Foldout(
                    expanded,
                    $"Step{stepNumber} - {safeStepName}",
                    true
                );

            dialogueStepFoldouts[
                stepIndex] =
                    expanded;

            if (expanded == false)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                if (string.IsNullOrWhiteSpace(
                        step.speechBubbleLocalizationId))
                {
                    EditorGUILayout.LabelField(
                        "Speech Localization ID",
                        "Localization 동기화 시 자동 생성"
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Speech Localization ID",
                        step.speechBubbleLocalizationId
                    );
                }

                EditorGUILayout.Space(4);

                // <변경부분>
                // Data에 저장되는 한국어 authoring 원문.
                // Runtime Localization 누락 시 fallback으로도 사용한다.
                EditorGUILayout.LabelField(
                    "Korean"
                );

                string currentKorean =
                    step.speechBubbleText ??
                    string.Empty;

                EditorGUI.BeginChangeCheck();

                string newKorean =
                    EditorGUILayout.TextArea(
                        currentKorean,
                        GUILayout.MinHeight(55)
                    );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        eventData,
                        "Edit Event Scene SpeechBubble Korean"
                    );

                    step.speechBubbleText =
                        newKorean;

                    EditorUtility.SetDirty(
                        eventData
                    );
                }

                string key =
                    GetSpeechBubbleKey(
                        eventData,
                        step
                    );

                if (string.IsNullOrWhiteSpace(
                        key))
                {
                    EditorGUILayout.HelpBox(
                        "Localization 생성 / 한국어 동기화를 실행하면 " +
                        "Stable Key와 EN / JA 입력란이 활성화됩니다.",
                        MessageType.None
                    );

                    continue;
                }

                EditorGUILayout.LabelField(
                    "Key",
                    key
                );

                if (collection == null)
                {
                    EditorGUILayout.HelpBox(
                        "EventScene_Dialogue Collection을 찾을 수 없습니다.",
                        MessageType.Warning
                    );

                    continue;
                }

                EditorGUILayout.Space(3);

                DrawLocaleTranslation(
                    collection,
                    "English",
                    "en",
                    key
                );

                EditorGUILayout.Space(4);

                DrawLocaleTranslation(
                    collection,
                    "Japanese",
                    "ja",
                    key
                );
            }

            EditorGUILayout.Space(6);
        }

        if (foundSpeechBubbleStep == false)
        {
            EditorGUILayout.HelpBox(
                "현재 EventSceneData에 SpeechBubble Step이 없습니다.",
                MessageType.Info
            );
        }
    }

    // <변경부분>
    // 현재 PlayActorAnimation Step보다 앞에서
    // 같은 Actor ID를 생성한 SpawnActor Step을 찾는다.
    //
    // Runtime에서도 Animation Step은
    // SpawnActor 이후에 실행되어야 하므로
    // 현재 Step 뒤쪽의 SpawnActor는 검색하지 않는다.
    private EventSceneStepData
        FindSpawnActorStepBefore(
            EventSceneData eventData,
            EventSceneStepData targetStep,
            string actorId)
    {
        if (eventData == null ||
            eventData.steps == null ||
            targetStep == null ||
            string.IsNullOrWhiteSpace(
                actorId))
        {
            return null;
        }

        int targetStepIndex =
            GetStepIndex(
                eventData,
                targetStep
            );

        if (targetStepIndex <= 0)
        {
            return null;
        }

        for (int i = targetStepIndex - 1;
             i >= 0;
             i--)
        {
            EventSceneStepData previousStep =
                eventData.steps[i];

            if (previousStep == null ||
                previousStep.stepType !=
                    EventSceneStepType.SpawnActor)
            {
                continue;
            }

            if (previousStep.spawnActorId ==
                actorId)
            {
                return previousStep;
            }
        }

        return null;
    }

    // <변경부분>
    // Actor ID와 연결된 SpawnActor의 PieceData를 기준으로
    // 실제 사용될 Spine Visual Prefab을 찾고,
    // SkeletonDataAsset 안의 Animation 이름 목록을 반환한다.
    private string[] GetSpineAnimationNamesForActor(
        EventSceneData eventData,
        EventSceneStepData targetStep,
        string actorId,
        out string sourcePrefabName)
    {
        sourcePrefabName =
            string.Empty;

        List<string> animationNames =
            new List<string>();

        EventSceneStepData spawnStep =
            FindSpawnActorStepBefore(
                eventData,
                targetStep,
                actorId
            );

        if (spawnStep == null ||
            spawnStep.spawnActorPieceData == null)
        {
            return
                animationNames.ToArray();
        }

        // <변경부분>
        // SpawnActor Runtime과 동일한 기준으로
        // 실제 Visual Prefab을 가져온다.
        GameObject visualPrefab =
            spawnStep.spawnActorPieceData
                .GetSpineVisualPrefab(
                    spawnStep.spawnActorTeam,
                    spawnStep
                        .spawnActorUseAbsorbedPlayerVisual
                );

        if (visualPrefab == null)
        {
            return
                animationNames.ToArray();
        }

        sourcePrefabName =
            visualPrefab.name;

        SkeletonAnimation skeletonAnimation =
            visualPrefab
                .GetComponentInChildren<
                    SkeletonAnimation
                >(
                    true
                );

        if (skeletonAnimation == null ||
            skeletonAnimation
                .SkeletonDataAsset == null)
        {
            return
                animationNames.ToArray();
        }

        // <변경부분>
        // Scene Instance를 생성하지 않고
        // Prefab이 참조하는 SkeletonDataAsset을 직접 읽는다.
        Spine.SkeletonData skeletonData =
            skeletonAnimation
                .SkeletonDataAsset
                .GetSkeletonData(
                    true
                );

        if (skeletonData == null ||
            skeletonData.Animations == null)
        {
            return
                animationNames.ToArray();
        }

        Spine.ExposedList<Spine.Animation>
            animations =
                skeletonData.Animations;

        for (int i = 0;
             i < animations.Count;
             i++)
        {
            Spine.Animation animation =
                animations.Items[i];

            if (animation == null ||
                string.IsNullOrWhiteSpace(
                    animation.Name))
            {
                continue;
            }

            animationNames.Add(
                animation.Name
            );
        }

        return
            animationNames.ToArray();
    }

    private bool DrawDialoguePage(
     EventSceneData eventData,
     EventSceneStepData step,
     int pageIndex,
     StringTableCollection collection)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            $"Page {pageIndex + 1}",
            EditorStyles.boldLabel
        );

        using (new EditorGUI.DisabledScope(
                   pageIndex <= 0))
        {
            if (GUILayout.Button(
                    "↑",
                    GUILayout.Width(28)))
            {
                MoveDialoguePage(
                    eventData,
                    step,
                    pageIndex,
                    pageIndex - 1
                );

                EditorGUILayout.EndHorizontal();

                GUIUtility.ExitGUI();
                return true;
            }
        }

        using (new EditorGUI.DisabledScope(
                   pageIndex >=
                   step.dialoguePages.Count - 1))
        {
            if (GUILayout.Button(
                    "↓",
                    GUILayout.Width(28)))
            {
                MoveDialoguePage(
                    eventData,
                    step,
                    pageIndex,
                    pageIndex + 1
                );

                EditorGUILayout.EndHorizontal();

                GUIUtility.ExitGUI();
                return true;
            }
        }

        if (GUILayout.Button(
                "삭제",
                GUILayout.Width(45)))
        {
            RemoveDialoguePage(
                eventData,
                step,
                pageIndex
            );

            EditorGUILayout.EndHorizontal();

            GUIUtility.ExitGUI();
            return true;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            "Korean"
        );

        string currentKorean =
            step.dialoguePages[pageIndex] ??
            string.Empty;

        EditorGUI.BeginChangeCheck();

        string newKorean =
            EditorGUILayout.TextArea(
                currentKorean,
                GUILayout.MinHeight(60)
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Scene Dialogue Korean"
            );

            step.dialoguePages[pageIndex] =
                newKorean;

            EditorUtility.SetDirty(
                eventData
            );
        }

        EventSceneDialoguePageLocalizationData
            localizationData =
                GetPageLocalizationData(
                    step,
                    pageIndex
                );

        string pageKey =
            GetPageKey(
                eventData,
                step,
                localizationData
            );

        if (string.IsNullOrWhiteSpace(
                pageKey))
        {
            EditorGUILayout.HelpBox(
                "Localization 생성 / 한국어 동기화를 실행하면 " +
                "Stable Key와 EN / JA 입력란이 활성화됩니다.",
                MessageType.None
            );

            return false;
        }

        EditorGUILayout.LabelField(
            "Key",
            pageKey
        );

        if (collection == null)
        {
            EditorGUILayout.HelpBox(
                "EventScene_Dialogue Collection을 찾을 수 없습니다.",
                MessageType.Warning
            );

            return false;
        }

        EditorGUILayout.Space(3);

        DrawLocaleTranslation(
            collection,
            "English",
            "en",
            pageKey
        );

        EditorGUILayout.Space(4);

        DrawLocaleTranslation(
            collection,
            "Japanese",
            "ja",
            pageKey
        );

        return false;
    }


    private void DrawLocaleTranslation(
        StringTableCollection collection,
        string displayName,
        string localeCode,
        string key)
    {
        StringTable table =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    localeCode
                );

        EditorGUILayout.LabelField(
            displayName
        );

        if (table == null)
        {
            EditorGUILayout.HelpBox(
                $"{displayName} String Table을 찾을 수 없습니다.",
                MessageType.Warning
            );

            return;
        }

        string currentText =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    key
                );

        EditorGUI.BeginChangeCheck();

        string newText =
            EditorGUILayout.TextArea(
                currentText,
                GUILayout.MinHeight(60)
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                table,
                $"Edit {displayName} Event Scene Dialogue"
            );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    key,
                    newText
                );

            EditorUtility.SetDirty(
                table
            );

            if (collection.SharedData != null)
            {
                EditorUtility.SetDirty(
                    collection.SharedData
                );
            }
        }
    }


    // <변경부분>
    // Event Scene 종료 설정.
    private void DrawCompletion(
        EventSceneData eventData)
    {
        EditorGUILayout.LabelField(
            "Completion",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();

            EventSceneCompletionType newCompletionType =
                (EventSceneCompletionType)
                EditorGUILayout.EnumPopup(
                    "Completion Type",
                    eventData.completionType
                );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    eventData,
                    "Edit Event Scene Completion"
                );

                eventData.completionType =
                    newCompletionType;

                EditorUtility.SetDirty(
                    eventData
                );
            }

            // <변경부분>
            // EventSceneData 자체의 완료 옵션이므로
            // 별도 Event Step이 아닌 Completion 영역에 표시한다.
            EditorGUI.BeginChangeCheck();

            bool newClearWorldMapNode =
                EditorGUILayout.Toggle(
                    "Clear Current World Map Node On Completion",
                    eventData.clearCurrentWorldMapNodeOnCompletion
                );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    eventData,
                    "Edit Event Scene Node Completion"
                );

                eventData.clearCurrentWorldMapNodeOnCompletion =
                    newClearWorldMapNode;

                EditorUtility.SetDirty(
                    eventData
                );
            }

            if (eventData.clearCurrentWorldMapNodeOnCompletion)
            {
                EditorGUILayout.HelpBox(
                    eventData.completionType ==
                        EventSceneCompletionType.None
                        ? "Completion Type이 None이면 Scene을 떠나지 않으므로 " +
                          "노드 클리어가 등록되지 않습니다. " +
                          "WorldMap 또는 LoadScene을 지정해 주세요."
                        : "월드맵 Event / RuinsEvent에서 진입해 정상적으로 " +
                          "Scene을 떠날 때, 다음 월드맵 복귀 시 현재 노드를 " +
                          "Cleared로 바꾸고 연결 노드를 해금합니다. " +
                          "상점과 전투 승리 처리는 기존 경로를 유지합니다.",
                    eventData.completionType ==
                        EventSceneCompletionType.None
                        ? MessageType.Warning
                        : MessageType.Info
                );
            }

            if (eventData.completionType != EventSceneCompletionType.None)
            {
                EditorGUI.BeginChangeCheck();
                var nextWorldMapEvent = (WorldMapEventData)EditorGUILayout.ObjectField(
                    "월드맵 이벤트 데이터 (선택)", eventData.completionWorldMapEventData, typeof(WorldMapEventData), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(eventData, "Edit World Map Event Handoff");
                    eventData.completionWorldMapEventData = nextWorldMapEvent;
                    EditorUtility.SetDirty(eventData);
                }
            }
            if (eventData.completionType ==
                EventSceneCompletionType.LoadScene)
            {
                EditorGUI.BeginChangeCheck();

                string newSceneName =
                    EditorGUILayout.TextField(
                        "Completion Scene Name",
                        eventData.completionSceneName
                    );

                TextCutsceneData newCutsceneData =
                    (TextCutsceneData)
                    EditorGUILayout.ObjectField(
                        "Completion Cutscene Data",
                        eventData.completionCutsceneData,
                        typeof(TextCutsceneData),
                        false
                    );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        eventData,
                        "Edit Event Scene Load Scene"
                    );

                    eventData.completionSceneName =
                        newSceneName;

                    eventData.completionCutsceneData =
                        newCutsceneData;

                    EditorUtility.SetDirty(
                        eventData
                    );
                }
            }
        }
    }


    // <변경부분>
    // Dialogue Choice의 분기 목적지를 식별하기 위한
    // 모든 Event Step의 Stable ID를 보장한다.
    //
    // 기존 ID는 그대로 유지하며,
    // 비어 있거나 중복된 ID만 새로 생성한다.
    //
    // Step Name 수정 / Reorder는 ID에 영향을 주지 않는다.
    private void EnsureUniqueBranchStepIds(
        EventSceneData eventData)
    {
        if (eventData == null ||
            eventData.steps == null)
        {
            return;
        }

        HashSet<string> usedIds =
            new HashSet<string>();

        bool recordedUndo = false;

        for (int i = 0;
             i < eventData.steps.Count;
             i++)
        {
            EventSceneStepData step =
                eventData.steps[i];

            if (step == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(
                    step.stepId) &&
                usedIds.Add(step.stepId))
            {
                continue;
            }

            if (!recordedUndo)
            {
                Undo.RecordObject(
                    eventData,
                    "Initialize Event Scene Branch Step IDs"
                );

                recordedUndo = true;
            }

            string newId;

            do
            {
                newId =
                    CreateStableId(
                        "step"
                    );
            }
            while (usedIds.Contains(newId));

            step.stepId = newId;

            usedIds.Add(newId);
        }

        if (recordedUndo)
        {
            EditorUtility.SetDirty(
                eventData
            );
        }
    }


    private void AddStep(
     EventSceneData eventData)
    {
        Undo.RecordObject(
            eventData,
            "Add Event Scene Step"
        );

        if (eventData.steps == null)
        {
            eventData.steps =
                new List<EventSceneStepData>();
        }

        eventData.steps.Add(
            new EventSceneStepData()
        );

        EditorUtility.SetDirty(
            eventData
        );
    }


    // <변경부분>
    // 선택한 Event Scene Step의 설정을 깊은 복사한 뒤
    // 원본 바로 아래에 새로운 Step으로 삽입한다.
    //
    // 모든 Inspector 설정은 유지하지만
    // Step ID 및 Localization ID는 새로 생성한다.
    private void DuplicateEventSceneStep(
        EventSceneData eventData,
        int sourceIndex)
    {
        if (eventData == null ||
            eventData.steps == null ||
            sourceIndex < 0 ||
            sourceIndex >= eventData.steps.Count)
        {
            return;
        }

        EventSceneStepData source =
            eventData.steps[sourceIndex];

        if (source == null)
        {
            return;
        }

        // <변경부분>
        // Unity Undo를 통해 Step 복제를 되돌릴 수 있도록 한다.
        Undo.RecordObject(
            eventData,
            "Duplicate Event Scene Step"
        );

        // <변경부분>
        // Unity 직렬화 형식으로 전체 설정을 복제한다.
        // List 및 중첩 Serializable Data는 독립적인 객체로 복사하고
        // PieceData 등 Unity Asset 참조는 동일하게 유지한다.
        EventSceneStepData copy =
            new EventSceneStepData();

        EditorJsonUtility.FromJsonOverwrite(
            EditorJsonUtility.ToJson(source),
            copy
        );

        // <변경부분>
        // 원본 Step으로 이동하는 기존 Dialogue Choice의
        // 목적지 ID는 보존하기 위해 새 Step에만 새 ID를 부여한다.
        copy.stepId =
            CreateStableId("step");

        copy.dialogueLocalizationId =
            CreateStableId("dialogue");

        copy.speechBubbleLocalizationId =
            CreateStableId("speech");

        // <변경부분>
        // Dialogue Page 개수와 Metadata 개수를 맞춘다.
        if (copy.stepType == EventSceneStepType.Dialogue)
        {
            EnsureStepLocalizationMetadata(copy);
        }

        if (copy.dialogueLocalizationPages != null)
        {
            foreach (
                EventSceneDialoguePageLocalizationData page
                in copy.dialogueLocalizationPages)
            {
                if (page == null)
                {
                    continue;
                }

                page.localizationId =
                    CreateStableId("page");

                page.localizedText =
                    new LocalizedString();
            }
        }

        if (copy.dialogueChoices != null)
        {
            foreach (
                EventSceneDialogueChoiceData choice
                in copy.dialogueChoices)
            {
                if (choice == null)
                {
                    continue;
                }

                choice.localizationId =
                    CreateStableId("choice");

                choice.localizedText =
                    new LocalizedString();

                // targetStepId는 원본과 같은 설정값으로 유지한다.
            }
        }

        copy.speechBubbleLocalizedText =
            new LocalizedString();

        // <변경부분>
        // 기존 KR / EN / JA String Table이 존재한다면
        // 원본 번역값을 새 Localization Key로 독립 복사한다.
        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        bool canCopyLocalization =
            collection != null &&
            !string.IsNullOrWhiteSpace(
                eventData.localizationId);

        if (canCopyLocalization)
        {
            if (collection.SharedData != null)
            {
                Undo.RecordObject(
                    collection.SharedData,
                    "Duplicate Event Scene Localization Keys"
                );
            }

            if (copy.stepType == EventSceneStepType.Dialogue)
            {
                for (int i = 0;
                     i < copy.dialoguePages.Count;
                     i++)
                {
                    EventSceneDialoguePageLocalizationData
                        sourcePage =
                            GetPageLocalizationData(source, i);

                    EventSceneDialoguePageLocalizationData
                        copiedPage =
                            GetPageLocalizationData(copy, i);

                    if (copiedPage == null)
                    {
                        continue;
                    }

                    string oldKey =
                        GetPageKey(
                            eventData,
                            source,
                            sourcePage
                        );

                    string newKey =
                        GetPageKey(
                            eventData,
                            copy,
                            copiedPage
                        );

                    CopyEventSceneTranslationValues(
                        collection,
                        oldKey,
                        newKey,
                        copy.dialoguePages[i]
                    );

                    copiedPage.localizedText =
                        DevoryaLocalizationEditorUtility
                            .CreateLocalizedStringReference(
                                collection,
                                newKey
                            );
                }

                if (copy.dialogueChoices != null &&
                    source.dialogueChoices != null)
                {
                    int choiceCount =
                        Mathf.Min(
                            source.dialogueChoices.Count,
                            copy.dialogueChoices.Count
                        );

                    for (int i = 0;
                         i < choiceCount;
                         i++)
                    {
                        EventSceneDialogueChoiceData sourceChoice =
                            source.dialogueChoices[i];

                        EventSceneDialogueChoiceData copiedChoice =
                            copy.dialogueChoices[i];

                        if (sourceChoice == null ||
                            copiedChoice == null)
                        {
                            continue;
                        }

                        string oldKey =
                            GetDialogueChoiceKey(
                                eventData,
                                sourceChoice
                            );

                        string newKey =
                            GetDialogueChoiceKey(
                                eventData,
                                copiedChoice
                            );

                        CopyEventSceneTranslationValues(
                            collection,
                            oldKey,
                            newKey,
                            copiedChoice.koreanText
                        );

                        copiedChoice.localizedText =
                            DevoryaLocalizationEditorUtility
                                .CreateLocalizedStringReference(
                                    collection,
                                    newKey
                                );
                    }
                }
            }

            if (copy.stepType == EventSceneStepType.SpeechBubble)
            {
                string oldKey =
                    GetSpeechBubbleKey(
                        eventData,
                        source
                    );

                string newKey =
                    GetSpeechBubbleKey(
                        eventData,
                        copy
                    );

                CopyEventSceneTranslationValues(
                    collection,
                    oldKey,
                    newKey,
                    copy.speechBubbleText
                );

                copy.speechBubbleLocalizedText =
                    DevoryaLocalizationEditorUtility
                        .CreateLocalizedStringReference(
                            collection,
                            newKey
                        );
            }
        }

        // <변경부분>
        // 원본 바로 다음 인덱스에 복제본을 삽입한다.
        eventData.steps.Insert(
            sourceIndex + 1,
            copy
        );

        // <변경부분>
        // 기존 펼침 상태는 보존하고
        // 새 복제 Step만 펼쳐진 상태로 표시한다.
        ShiftEventSceneFoldoutsAfterDuplicate(
            sourceIndex
        );

        // <변경부분>
        // 삽입 후 인덱스가 바뀌므로
        // 기존 Scene View Tile 선택 상태는 정리한다.
        CancelAllEventSceneTileSelection();

        EditorUtility.SetDirty(
            eventData
        );

        if (canCopyLocalization)
        {
            if (collection.SharedData != null)
            {
                EditorUtility.SetDirty(
                    collection.SharedData
                );
            }

            AssetDatabase.SaveAssets();
        }
    }


    // <변경부분>
    // Step 삽입 시 기존 Index 기반 펼침 상태를 한 칸 이동한다.
    private void ShiftEventSceneFoldoutsAfterDuplicate(
        int sourceIndex)
    {
        Dictionary<int, bool> shiftedSteps =
            new Dictionary<int, bool>();

        foreach (
            KeyValuePair<int, bool> entry
            in stepFoldouts)
        {
            int newIndex =
                entry.Key > sourceIndex
                    ? entry.Key + 1
                    : entry.Key;

            shiftedSteps[newIndex] =
                entry.Value;
        }

        stepFoldouts.Clear();

        foreach (
            KeyValuePair<int, bool> entry
            in shiftedSteps)
        {
            stepFoldouts[entry.Key] =
                entry.Value;
        }

        stepFoldouts[sourceIndex + 1] =
            true;

        // <변경부분>
        // 아래 Dialogue 관리 영역도 Step Index 기반이므로
        // 동일하게 삽입된 위치 이후 인덱스를 이동한다.
        Dictionary<int, bool> shiftedDialogues =
            new Dictionary<int, bool>();

        foreach (
            KeyValuePair<int, bool> entry
            in dialogueStepFoldouts)
        {
            int newIndex =
                entry.Key > sourceIndex
                    ? entry.Key + 1
                    : entry.Key;

            shiftedDialogues[newIndex] =
                entry.Value;
        }

        dialogueStepFoldouts.Clear();

        foreach (
    KeyValuePair<int, bool> entry
    in shiftedDialogues)
        {
            dialogueStepFoldouts[entry.Key] =
                entry.Value;
        }
    }


    // <변경부분>
    // 기존 Localization Table의 KR / EN / JA 값을
    // 복제 Step의 신규 Key로 복사한다.
    //
    // 원본 번역 Key나 번역문은 수정하지 않는다.
    private void CopyEventSceneTranslationValues(
        StringTableCollection collection,
        string sourceKey,
        string targetKey,
        string koreanFallback)
    {
        if (collection == null ||
            string.IsNullOrWhiteSpace(targetKey))
        {
            return;
        }

        string[] localeCodes =
        {
            "ko",
            "en",
            "ja"
        };

        foreach (string localeCode in localeCodes)
        {
            StringTable table =
                collection.GetTable(localeCode)
                    as StringTable;

            if (table == null)
            {
                continue;
            }

            StringTableEntry sourceEntry =
                string.IsNullOrWhiteSpace(sourceKey)
                    ? null
                    : table.GetEntry(sourceKey);

            // <변경부분>
            // EN / JA 번역이 없는 경우에는
            // 임의의 번역문을 생성하지 않는다.
            if (sourceEntry == null &&
                localeCode != "ko")
            {
                continue;
            }

            string copiedValue =
                sourceEntry != null
                    ? sourceEntry.LocalizedValue
                    : koreanFallback ?? string.Empty;

            Undo.RecordObject(
                table,
                "Duplicate Event Scene Translation"
            );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    targetKey,
                    copiedValue
                );

            EditorUtility.SetDirty(
                table
            );
        }
    }


    private void RemoveStep(
        EventSceneData eventData,
        int stepIndex)
    {
        if (eventData.steps == null ||
            stepIndex < 0 ||
            stepIndex >= eventData.steps.Count)
        {
            return;
        }

        Undo.RecordObject(
            eventData,
            "Remove Event Scene Step"
        );

        // <변경부분>
        // 기존 Localization Table Entry는 즉시 삭제하지 않는다.
        // 잘못 삭제한 Step 복구 및 Stable Data 보호를 위해
        // 미사용 Key 정리는 별도 Cleanup 단계에서 처리한다.
        eventData.steps.RemoveAt(
            stepIndex
        );

        EditorUtility.SetDirty(
            eventData
        );
    }


    private void AddDialoguePage(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        Undo.RecordObject(
            eventData,
            "Add Event Scene Dialogue Page"
        );

        EnsureStepLocalizationMetadata(
            step
        );

        step.dialoguePages.Add(
            string.Empty
        );

        step.dialogueLocalizationPages.Add(
            CreatePageLocalizationData()
        );

        EditorUtility.SetDirty(
            eventData
        );
    }


    private void RemoveDialoguePage(
        EventSceneData eventData,
        EventSceneStepData step,
        int pageIndex)
    {
        if (step == null ||
            step.dialoguePages == null ||
            pageIndex < 0 ||
            pageIndex >= step.dialoguePages.Count)
        {
            return;
        }

        Undo.RecordObject(
            eventData,
            "Remove Event Scene Dialogue Page"
        );

        EnsureStepLocalizationMetadata(
            step
        );

        step.dialoguePages.RemoveAt(
            pageIndex
        );

        if (pageIndex <
            step.dialogueLocalizationPages.Count)
        {
            step.dialogueLocalizationPages.RemoveAt(
                pageIndex
            );
        }

        EditorUtility.SetDirty(
            eventData
        );
    }


    private void MoveDialoguePage(
        EventSceneData eventData,
        EventSceneStepData step,
        int fromIndex,
        int toIndex)
    {
        if (step == null ||
            step.dialoguePages == null ||
            fromIndex < 0 ||
            fromIndex >= step.dialoguePages.Count ||
            toIndex < 0 ||
            toIndex >= step.dialoguePages.Count ||
            fromIndex == toIndex)
        {
            return;
        }

        Undo.RecordObject(
            eventData,
            "Move Event Scene Dialogue Page"
        );

        EnsureStepLocalizationMetadata(
            step
        );

        string pageText =
            step.dialoguePages[fromIndex];

        EventSceneDialoguePageLocalizationData
            pageLocalization =
                step.dialogueLocalizationPages[
                    fromIndex];

        step.dialoguePages.RemoveAt(
            fromIndex
        );

        step.dialoguePages.Insert(
            toIndex,
            pageText
        );

        step.dialogueLocalizationPages.RemoveAt(
            fromIndex
        );

        step.dialogueLocalizationPages.Insert(
            toIndex,
            pageLocalization
        );

        EditorUtility.SetDirty(
            eventData
        );
    }


    // <변경부분>
    // EventSceneData의 Dialogue Page와 SpeechBubble 한국어 원문을
    // EventScene_Dialogue KO Table에 동기화한다.
    //
    // EN / JA 기존 번역값은 수정하지 않는다.
    private void SyncLocalization(
        EventSceneData eventData)
    {
        if (eventData == null ||
            string.IsNullOrWhiteSpace(
                eventData.localizationId))
        {
            return;
        }

        if (IsLocalizationIdDuplicate(
                eventData))
        {
            EditorUtility.DisplayDialog(
                "Localization ID 중복",
                "다른 EventSceneData가 동일한 Localization ID를 사용하고 있습니다.",
                "확인"
            );

            return;
        }

        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTableCollection(
                    TableCollectionName
                );

        if (collection == null)
        {
            return;
        }

        StringTable koreanTable =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    "ko"
                );

        // <변경부분>
        // Inspector에서 바로 번역 입력이 가능하도록
        // EN / JA Table도 함께 준비한다.
        DevoryaLocalizationEditorUtility
            .GetOrCreateStringTable(
                collection,
                "en"
            );

        DevoryaLocalizationEditorUtility
            .GetOrCreateStringTable(
                collection,
                "ja"
            );

        if (koreanTable == null)
        {
            return;
        }

        Undo.RecordObject(
            eventData,
            "Sync Event Scene Dialogue Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Event Scene Dialogue Korean"
        );

        EnsureAllLocalizationMetadata(
            eventData
        );

        if (eventData.steps != null)
        {
            for (int stepIndex = 0;
                 stepIndex < eventData.steps.Count;
                 stepIndex++)
            {
                EventSceneStepData step =
                    eventData.steps[stepIndex];

                if (step == null)
                {
                    continue;
                }

                // =================================================
                // Dialogue
                // =================================================

                if (step.stepType ==
                    EventSceneStepType.Dialogue)
                {
                    if (step.dialoguePages == null)
                    {
                        continue;
                    }

                    for (int pageIndex = 0;
                         pageIndex <
                         step.dialoguePages.Count;
                         pageIndex++)
                    {
                        EventSceneDialoguePageLocalizationData
                            pageData =
                                step.dialogueLocalizationPages[
                                    pageIndex];

                        string key =
                            GetPageKey(
                                eventData,
                                step,
                                pageData
                            );

                        if (string.IsNullOrWhiteSpace(
                                key))
                        {
                            continue;
                        }

                        DevoryaLocalizationEditorUtility
                            .SetTableValue(
                                koreanTable,
                                key,
                                step.dialoguePages[
                                    pageIndex]
                            );

                        pageData.localizedText =
     DevoryaLocalizationEditorUtility
         .CreateLocalizedStringReference(
             collection,
             key
         );
                    }

                    // <변경부분>
                    // Dialogue의 선택지도 동일한 Collection에
                    // 한국어 원문을 동기화한다.
                    //
                    // 선택지 고유 Key를 사용하므로
                    // 기존 Dialogue Page 번역은 변경하지 않는다.
                    if (step.dialogueChoices != null)
                    {
                        for (int choiceIndex = 0;
                             choiceIndex <
                                 step.dialogueChoices.Count;
                             choiceIndex++)
                        {
                            EventSceneDialogueChoiceData choice =
                                step.dialogueChoices[choiceIndex];

                            if (choice == null)
                            {
                                continue;
                            }

                            string choiceKey =
                                GetDialogueChoiceKey(
                                    eventData,
                                    choice
                                );

                            if (string.IsNullOrWhiteSpace(
                                    choiceKey))
                            {
                                continue;
                            }

                            DevoryaLocalizationEditorUtility
                                .SetTableValue(
                                    koreanTable,
                                    choiceKey,
                                    choice.koreanText ??
                                        string.Empty
                                );

                            choice.localizedText =
                                DevoryaLocalizationEditorUtility
                                    .CreateLocalizedStringReference(
                                        collection,
                                        choiceKey
                                    );
                        }
                    }

                    continue;
                }

                // =================================================
                // Speech Bubble
                // =================================================

                // <변경부분>
                // Dialogue와 동일한 EventScene_Dialogue Collection을 사용하지만
                // speech 전용 Stable Key Namespace를 사용한다.
                if (step.stepType ==
                    EventSceneStepType.SpeechBubble)
                {
                    string key =
                        GetSpeechBubbleKey(
                            eventData,
                            step
                        );

                    if (string.IsNullOrWhiteSpace(
                            key))
                    {
                        continue;
                    }

                    DevoryaLocalizationEditorUtility
                        .SetTableValue(
                            koreanTable,
                            key,
                            step.speechBubbleText ??
                            string.Empty
                        );

                    step.speechBubbleLocalizedText =
                        DevoryaLocalizationEditorUtility
                            .CreateLocalizedStringReference(
                                collection,
                                key
                            );
                }
            }
        }

        EditorUtility.SetDirty(
            eventData
        );

        EditorUtility.SetDirty(
            koreanTable
        );

        if (collection.SharedData != null)
        {
            EditorUtility.SetDirty(
                collection.SharedData
            );
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Event Scene Dialogue Localization 동기화 완료: " +
            $"{eventData.name} / " +
            $"{eventData.localizationId}"
        );
    }

    // <변경부분>
    // Dialogue와 SpeechBubble 모두 Step 순서와 무관한
    // Stable Localization Metadata를 준비한다.
    private void EnsureAllLocalizationMetadata(
        EventSceneData eventData)
    {
        if (eventData == null ||
            eventData.steps == null)
        {
            return;
        }

        // Dialogue와 SpeechBubble은 서로 다른 Key Namespace를 사용하므로
        // 각 타입 내부에서 Stable ID 중복을 검사한다.
        HashSet<string> usedDialogueStepIds =
         new HashSet<string>();

        HashSet<string> usedSpeechIds =
            new HashSet<string>();

        // <변경부분>
        // 같은 EventSceneData 안의 모든 Dialogue Choice가
        // 서로 다른 Localization ID를 갖도록 관리한다.
        HashSet<string> usedChoiceIds =
            new HashSet<string>();

        for (int i = 0;
             i < eventData.steps.Count;
             i++)
        {
            EventSceneStepData step =
                eventData.steps[i];

            if (step == null)
            {
                continue;
            }

            // =================================================
            // Dialogue
            // =================================================

            if (step.stepType ==
                EventSceneStepType.Dialogue)
            {
                if (string.IsNullOrWhiteSpace(
                        step.dialogueLocalizationId) ||
                    usedDialogueStepIds.Contains(
                        step.dialogueLocalizationId))
                {
                    step.dialogueLocalizationId =
                        CreateStableId(
                            "dialogue"
                        );
                }

                usedDialogueStepIds.Add(
                    step.dialogueLocalizationId
                );

                EnsureStepLocalizationMetadata(
                    step
                );

                HashSet<string> usedPageIds =
                    new HashSet<string>();

                for (int pageIndex = 0;
                     pageIndex <
                     step.dialogueLocalizationPages.Count;
                     pageIndex++)
                {
                    EventSceneDialoguePageLocalizationData
                        pageData =
                            step.dialogueLocalizationPages[
                                pageIndex];

                    if (pageData == null)
                    {
                        pageData =
                            CreatePageLocalizationData();

                        step.dialogueLocalizationPages[
                            pageIndex] =
                                pageData;
                    }

                    if (string.IsNullOrWhiteSpace(
                            pageData.localizationId) ||
                        usedPageIds.Contains(
                            pageData.localizationId))
                    {
                        pageData.localizationId =
                            CreateStableId(
                                "page"
                            );
                    }

                    usedPageIds.Add(
    pageData.localizationId
);
                }

                // <변경부분>
                // Dialogue Choice의 Localization ID는
                // 페이지 번호와 무관하게 안정적으로 유지한다.
                //
                // 중복 ID를 발견하면 처음 등장한 선택지의 ID를
                // 우선 보존하고 중복된 선택지만 새 ID를 부여한다.
                if (step.dialogueChoices != null)
                {
                    for (int choiceIndex = 0;
                         choiceIndex <
                             step.dialogueChoices.Count;
                         choiceIndex++)
                    {
                        EventSceneDialogueChoiceData choice =
                            step.dialogueChoices[choiceIndex];

                        if (choice == null)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(
                                choice.localizationId) ||
                            usedChoiceIds.Contains(
                                choice.localizationId))
                        {
                            string newChoiceId;

                            do
                            {
                                newChoiceId =
                                    CreateStableId(
                                        "choice"
                                    );
                            }
                            while (usedChoiceIds.Contains(
                                       newChoiceId));

                            choice.localizationId =
                                newChoiceId;
                        }

                        usedChoiceIds.Add(
                            choice.localizationId
                        );

                        if (choice.localizedText == null)
                        {
                            choice.localizedText =
                                new LocalizedString();
                        }
                    }
                }

                continue;
            }

            // =================================================
            // Speech Bubble
            // =================================================

            if (step.stepType ==
                EventSceneStepType.SpeechBubble)
            {
                if (string.IsNullOrWhiteSpace(
                        step.speechBubbleLocalizationId) ||
                    usedSpeechIds.Contains(
                        step.speechBubbleLocalizationId))
                {
                    step.speechBubbleLocalizationId =
                        CreateStableId(
                            "speech"
                        );
                }

                usedSpeechIds.Add(
                    step.speechBubbleLocalizationId
                );

                if (step.speechBubbleLocalizedText == null)
                {
                    step.speechBubbleLocalizedText =
                        new LocalizedString();
                }
            }
        }
    }

    private void EnsureStepLocalizationMetadata(
        EventSceneStepData step)
    {
        if (step == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                step.dialogueLocalizationId))
        {
            step.dialogueLocalizationId =
                CreateStableId(
                    "dialogue"
                );
        }

        if (step.dialoguePages == null)
        {
            step.dialoguePages =
                new List<string>();
        }

        if (step.dialogueLocalizationPages == null)
        {
            step.dialogueLocalizationPages =
                new List<
                    EventSceneDialoguePageLocalizationData
                >();
        }

        while (
            step.dialogueLocalizationPages.Count <
            step.dialoguePages.Count)
        {
            step.dialogueLocalizationPages.Add(
                CreatePageLocalizationData()
            );
        }

        while (
            step.dialogueLocalizationPages.Count >
            step.dialoguePages.Count)
        {
            step.dialogueLocalizationPages.RemoveAt(
                step.dialogueLocalizationPages.Count -
                1
            );
        }

        for (int i = 0;
             i <
             step.dialogueLocalizationPages.Count;
             i++)
        {
            if (step.dialogueLocalizationPages[i] ==
                null)
            {
                step.dialogueLocalizationPages[i] =
                    CreatePageLocalizationData();
            }

            if (string.IsNullOrWhiteSpace(
                    step.dialogueLocalizationPages[i]
                        .localizationId))
            {
                step.dialogueLocalizationPages[i]
                    .localizationId =
                        CreateStableId(
                            "page"
                        );
            }
        }
    }


    private EventSceneDialoguePageLocalizationData
        CreatePageLocalizationData()
    {
        return
            new EventSceneDialoguePageLocalizationData
            {
                localizationId =
                    CreateStableId(
                        "page"
                    ),

                localizedText =
                    new LocalizedString()
            };
    }


    private EventSceneDialoguePageLocalizationData
        GetPageLocalizationData(
            EventSceneStepData step,
            int pageIndex)
    {
        if (step == null ||
            step.dialogueLocalizationPages == null ||
            pageIndex < 0 ||
            pageIndex >=
                step.dialogueLocalizationPages.Count)
        {
            return null;
        }

        return
            step.dialogueLocalizationPages[
                pageIndex];
    }


    private string GetPageKey(
        EventSceneData eventData,
        EventSceneStepData step,
        EventSceneDialoguePageLocalizationData
            pageData)
    {
        if (eventData == null ||
            step == null ||
            pageData == null ||
            string.IsNullOrWhiteSpace(
                eventData.localizationId) ||
            string.IsNullOrWhiteSpace(
                step.dialogueLocalizationId) ||
            string.IsNullOrWhiteSpace(
                pageData.localizationId))
        {
            return string.Empty;
        }

        return
           $"event_scene." +
           $"{eventData.localizationId}." +
           $"{step.dialogueLocalizationId}." +
           $"{pageData.localizationId}";
    }


    // <변경부분>
    // Dialogue Choice 전용 Stable Localization Key.
    //
    // 형식:
    // event_scene.<eventId>.choice.<choiceId>
    //
    // Dialogue Page / SpeechBubble와 Namespace를 분리한다.
    // 선택지 순서를 변경해도 번역 Key는 유지된다.
    private string GetDialogueChoiceKey(
        EventSceneData eventData,
        EventSceneDialogueChoiceData choice)
    {
        if (eventData == null ||
            choice == null ||
            string.IsNullOrWhiteSpace(
                eventData.localizationId) ||
            string.IsNullOrWhiteSpace(
                choice.localizationId))
        {
            return string.Empty;
        }

        return
            $"event_scene." +
            $"{eventData.localizationId}." +
            $"choice." +
            $"{choice.localizationId}";
    }


    // <변경부분>
    // SpeechBubble은 Step Index가 아니라
    // 자체 Stable Localization ID로 Key를 구성한다.
    //
    // 형식:
    // event_scene.<eventId>.speech.<speechId>
    private string GetSpeechBubbleKey(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        if (eventData == null ||
            step == null ||
            string.IsNullOrWhiteSpace(
                eventData.localizationId) ||
            string.IsNullOrWhiteSpace(
                step.speechBubbleLocalizationId))
        {
            return string.Empty;
        }

        return
            $"event_scene." +
            $"{eventData.localizationId}." +
            $"speech." +
            $"{step.speechBubbleLocalizationId}";
    }
    // <변경부분>
    // Dialogue Page + SpeechBubble Text를 모두
    // EventScene_Dialogue 번역 진행률에 포함한다.
    private void DrawTranslationStatus(
        EventSceneData eventData)
    {
        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        if (collection == null)
        {
            return;
        }

        StringTable koreanTable =
            collection.GetTable(
                "ko"
            ) as StringTable;

        StringTable englishTable =
            collection.GetTable(
                "en"
            ) as StringTable;

        StringTable japaneseTable =
            collection.GetTable(
                "ja"
            ) as StringTable;

        int total =
            0;

        int koreanCount =
            0;

        int englishCount =
            0;

        int japaneseCount =
            0;

        if (eventData.steps != null)
        {
            for (int stepIndex = 0;
                 stepIndex <
                 eventData.steps.Count;
                 stepIndex++)
            {
                EventSceneStepData step =
                    eventData.steps[stepIndex];

                if (step == null)
                {
                    continue;
                }

                // =================================================
                // Dialogue Pages
                // =================================================

                if (step.stepType ==
                        EventSceneStepType.Dialogue &&
                    step.dialoguePages != null)
                {
                    for (int pageIndex = 0;
                         pageIndex <
                         step.dialoguePages.Count;
                         pageIndex++)
                    {
                        total++;

                        EventSceneDialoguePageLocalizationData
                            pageData =
                                GetPageLocalizationData(
                                    step,
                                    pageIndex
                                );

                        string key =
                            GetPageKey(
                                eventData,
                                step,
                                pageData
                            );

                        if (string.IsNullOrWhiteSpace(
                                key))
                        {
                            continue;
                        }

                        if (HasTableValue(
                                koreanTable,
                                key))
                        {
                            koreanCount++;
                        }

                        if (HasTableValue(
                                englishTable,
                                key))
                        {
                            englishCount++;
                        }

                        if (HasTableValue(
          japaneseTable,
          key))
                        {
                            japaneseCount++;
                        }
                    }

                    // <변경부분>
                    // Dialogue Choice도 번역 진행률에 포함한다.
                    //
                    // Dialogue Page와 동일하게
                    // KR / EN / JA에 실제 문구가 존재하는지 검사한다.
                    if (step.dialogueChoices != null)
                    {
                        for (int choiceIndex = 0;
                             choiceIndex <
                                 step.dialogueChoices.Count;
                             choiceIndex++)
                        {
                            EventSceneDialogueChoiceData choice =
                                step.dialogueChoices[choiceIndex];

                            if (choice == null)
                            {
                                continue;
                            }

                            total++;

                            string choiceKey =
                                GetDialogueChoiceKey(
                                    eventData,
                                    choice
                                );

                            if (string.IsNullOrWhiteSpace(
                                    choiceKey))
                            {
                                continue;
                            }

                            if (HasTableValue(
                                    koreanTable,
                                    choiceKey))
                            {
                                koreanCount++;
                            }

                            if (HasTableValue(
                                    englishTable,
                                    choiceKey))
                            {
                                englishCount++;
                            }

                            if (HasTableValue(
                                    japaneseTable,
                                    choiceKey))
                            {
                                japaneseCount++;
                            }
                        }
                    }

                    continue;
                }

                // =================================================
                // Speech Bubble
                // =================================================

                if (step.stepType ==
                    EventSceneStepType.SpeechBubble)
                {
                    total++;

                    string key =
                        GetSpeechBubbleKey(
                            eventData,
                            step
                        );

                    if (string.IsNullOrWhiteSpace(
                            key))
                    {
                        continue;
                    }

                    if (HasTableValue(
                            koreanTable,
                            key))
                    {
                        koreanCount++;
                    }

                    if (HasTableValue(
                            englishTable,
                            key))
                    {
                        englishCount++;
                    }

                    if (HasTableValue(
                            japaneseTable,
                            key))
                    {
                        japaneseCount++;
                    }
                }
            }
        }

        EditorGUILayout.LabelField(
            "Translation Status",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            $"Total Texts: {total}\n" +
            $"KR: {koreanCount} / {total}\n" +
            $"EN: {englishCount} / {total}\n" +
            $"JA: {japaneseCount} / {total}",
            MessageType.Info
        );
    }


    private bool HasTableValue(
        StringTable table,
        string key)
    {
        if (table == null ||
            string.IsNullOrWhiteSpace(
                key))
        {
            return false;
        }

        string value =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    key
                );

        return
            string.IsNullOrWhiteSpace(
                value) == false;
    }


    private bool HasAnyDialoguePage(
        EventSceneData eventData)
    {
        if (eventData == null ||
            eventData.steps == null)
        {
            return false;
        }

        for (int i = 0;
             i < eventData.steps.Count;
             i++)
        {
            EventSceneStepData step =
                eventData.steps[i];

            if (step != null &&
                step.stepType ==
                    EventSceneStepType.Dialogue &&
                step.dialoguePages != null &&
                step.dialoguePages.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분>
    // Dialogue Page가 없어도 SpeechBubble Step 하나만 존재하면
    // Localization 생성 / KO Sync가 가능하도록 확인한다.
    private bool HasAnySpeechBubbleStep(
        EventSceneData eventData)
    {
        if (eventData == null ||
            eventData.steps == null)
        {
            return false;
        }

        for (int i = 0;
             i < eventData.steps.Count;
             i++)
        {
            EventSceneStepData step =
                eventData.steps[i];

            if (step != null &&
                step.stepType ==
                    EventSceneStepType.SpeechBubble)
            {
                return true;
            }
        }

        return false;
    }

    private bool GetStepFoldout(
     int stepIndex)
    {
        if (stepFoldouts.TryGetValue(
                stepIndex,
                out bool expanded))
        {
            return expanded;
        }

        // <변경부분>
        // Event Scene을 처음 열었을 때는
        // Step 수가 많아도 Inspector가 지나치게 길어지지 않도록
        // 기본 상태를 접힘으로 시작한다.
        stepFoldouts[stepIndex] =
            false;

        return false;
    }

    // <변경부분>
    // 현재 Step 객체가 EventSceneData 안에서
    // 몇 번째 Step인지 찾는다.
    //
    // Scene View 타일 선택 시
    // 어떤 SpawnActor Step을 수정해야 하는지 식별하는 데 사용한다.
    private int GetStepIndex(
        EventSceneData eventData,
        EventSceneStepData targetStep)
    {
        if (eventData == null ||
            eventData.steps == null ||
            targetStep == null)
        {
            return -1;
        }

        for (int i = 0;
             i < eventData.steps.Count;
             i++)
        {
            if (eventData.steps[i] ==
                targetStep)
            {
                return i;
            }
        }

        return -1;
    }

    // <변경부분>
    // Dialogue 전용 관리 영역의 Foldout 상태를 반환한다.
    private bool GetDialogueStepFoldout(
    int stepIndex)
    {
        if (dialogueStepFoldouts.TryGetValue(
                stepIndex,
                out bool expanded))
        {
            return expanded;
        }

        dialogueStepFoldouts[
            stepIndex] =
                true;

        return true;
    }


    // <변경부분>
    // EventSceneData의 Spawn / Move / CameraShot 위치를
    // Scene View Marker로 표시하고,
    // 각 Step의 BackgroundTile 좌표 선택 입력을 처리한다.
    private void HandleSceneGUI(
        SceneView sceneView)
    {
        EventSceneData eventData =
            target as EventSceneData;

        if (eventData == null ||
            eventData.steps == null)
        {
            return;
        }

        BackgroundManager backgroundManager =
            UnityEngine.Object
                .FindObjectOfType<
                    BackgroundManager
                >();

        if (backgroundManager != null)
        {
            DrawSpawnActorMarkers(
                eventData,
                backgroundManager
            );

            DrawMoveActorMarkers(
                eventData,
                backgroundManager
            );

            // <변경부분>
            // CameraShot BackgroundTile Target도
            // 별도의 Camera Marker로 항상 표시한다.
            DrawCameraShotMarkers(
                eventData,
                backgroundManager
            );
        }

        // <변경부분>
        // 각 좌표 선택 모드는 동시에 하나만 존재한다.
        if (isSelectingCameraShotTile)
        {
            HandleCameraShotTileSelection(
                eventData,
                backgroundManager
            );

            return;
        }

        if (isSelectingMoveActorTile)
        {
            HandleMoveActorTileSelection(
                eventData,
                backgroundManager
            );

            return;
        }

        // 기존 SpawnActor 좌표 선택.
        if (isSelectingSpawnActorTile == false)
        {
            return;
        }

        if (selectingSpawnActorStepIndex < 0 ||
            selectingSpawnActorStepIndex >=
                eventData.steps.Count)
        {
            CancelSpawnActorTileSelection();
            return;
        }

        EventSceneStepData step =
            eventData.steps[
                selectingSpawnActorStepIndex];

        if (step == null ||
            step.stepType !=
                EventSceneStepType.SpawnActor)
        {
            CancelSpawnActorTileSelection();
            return;
        }

        Handles.BeginGUI();

        GUI.Box(
            new Rect(
                10f,
                10f,
                280f,
                50f
            ),
            "SpawnActor 타일 선택 중\n" +
            "원하는 BackgroundTile을 클릭하세요."
        );

        Handles.EndGUI();

        Event currentEvent =
            Event.current;

        if (currentEvent == null)
        {
            return;
        }

        // Alt는 Scene View 카메라 조작에 사용한다.
        if (currentEvent.alt)
        {
            return;
        }

        if (currentEvent.type !=
                EventType.MouseDown ||
            currentEvent.button != 0)
        {
            return;
        }

        if (backgroundManager == null)
        {
            Debug.LogWarning(
                "Event Scene 타일 선택 실패: " +
                "현재 Scene에서 BackgroundManager를 찾을 수 없습니다."
            );

            CancelSpawnActorTileSelection();
            return;
        }

        Ray mouseRay =
            HandleUtility
                .GUIPointToWorldRay(
                    currentEvent.mousePosition
                );

        Vector3 worldPosition =
            mouseRay.origin;

        if (backgroundManager
                .TryGetBackgroundGridPosition(
                    worldPosition,
                    out int gridX,
                    out int gridY) == false)
        {
            return;
        }

        BackgroundTile selectedTile =
            backgroundManager
                .GetBackgroundTileAt(
                    gridX,
                    gridY
                );

        if (selectedTile == null)
        {
            return;
        }

        Undo.RecordObject(
            eventData,
            "Select Event Spawn Actor Tile"
        );

        step.spawnActorPosition =
            new Vector2Int(
                selectedTile.X,
                selectedTile.Y
            );

        EditorUtility.SetDirty(
            eventData
        );

        isSelectingSpawnActorTile =
            false;

        selectingSpawnActorStepIndex =
            -1;

        currentEvent.Use();

        Repaint();
        SceneView.RepaintAll();
    }

    // <변경부분>
    // CameraShot Step의 BackgroundTile Target을
    // Scene View 클릭으로 선택한다.
    private void HandleCameraShotTileSelection(
        EventSceneData eventData,
        BackgroundManager backgroundManager)
    {
        if (eventData == null ||
            eventData.steps == null ||
            selectingCameraShotStepIndex < 0 ||
            selectingCameraShotStepIndex >=
                eventData.steps.Count)
        {
            CancelCameraShotTileSelection();
            return;
        }

        EventSceneStepData step =
            eventData.steps[
                selectingCameraShotStepIndex];

        if (step == null ||
            step.stepType !=
                EventSceneStepType.CameraShot ||
            step.cameraShotTargetType !=
                EventSceneCameraShotTargetType.BackgroundTile)
        {
            CancelCameraShotTileSelection();
            return;
        }

        Handles.BeginGUI();

        GUI.Box(
            new Rect(
                10f,
                10f,
                320f,
                50f
            ),
            "CameraShot Target 선택 중\n" +
            "Camera가 바라볼 BackgroundTile을 클릭하세요."
        );

        Handles.EndGUI();

        Event currentEvent =
            Event.current;

        if (currentEvent == null ||
            currentEvent.alt)
        {
            return;
        }

        if (currentEvent.type !=
                EventType.MouseDown ||
            currentEvent.button != 0)
        {
            return;
        }

        if (backgroundManager == null)
        {
            Debug.LogWarning(
                "Event Scene CameraShot 타일 선택 실패: " +
                "BackgroundManager를 찾을 수 없습니다."
            );

            CancelCameraShotTileSelection();
            return;
        }

        Ray mouseRay =
            HandleUtility
                .GUIPointToWorldRay(
                    currentEvent.mousePosition
                );

        Vector3 worldPosition =
            mouseRay.origin;

        if (backgroundManager
                .TryGetBackgroundGridPosition(
                    worldPosition,
                    out int gridX,
                    out int gridY) == false)
        {
            return;
        }

        BackgroundTile selectedTile =
            backgroundManager
                .GetBackgroundTileAt(
                    gridX,
                    gridY
                );

        if (selectedTile == null)
        {
            return;
        }

        Undo.RecordObject(
            eventData,
            "Select Event Camera Shot Tile"
        );

        step.cameraShotTargetPosition =
            new Vector2Int(
                selectedTile.X,
                selectedTile.Y
            );

        EditorUtility.SetDirty(
            eventData
        );

        isSelectingCameraShotTile =
            false;

        selectingCameraShotStepIndex =
            -1;

        currentEvent.Use();

        Repaint();
        SceneView.RepaintAll();
    }


    // <변경부분>
    // CameraShot Scene View 타일 선택 상태를 종료한다.
    private void CancelCameraShotTileSelection()
    {
        isSelectingCameraShotTile =
            false;

        selectingCameraShotStepIndex =
            -1;

        Repaint();
        SceneView.RepaintAll();
    }

    // <변경부분>
    // Scene View에서 MoveActor의 목적지 BackgroundTile을 선택한다.
    private void HandleMoveActorTileSelection(
        EventSceneData eventData,
        BackgroundManager backgroundManager)
    {
        if (eventData == null ||
            eventData.steps == null ||
            selectingMoveActorStepIndex < 0 ||
            selectingMoveActorStepIndex >=
                eventData.steps.Count)
        {
            CancelMoveActorTileSelection();
            return;
        }

        EventSceneStepData step =
            eventData.steps[
                selectingMoveActorStepIndex];

        if (step == null ||
            step.stepType !=
                EventSceneStepType.MoveActor)
        {
            CancelMoveActorTileSelection();
            return;
        }

        Handles.BeginGUI();

        GUI.Box(
            new Rect(
                10f,
                10f,
                300f,
                50f
            ),
            "MoveActor 목적지 선택 중\n" +
            "원하는 BackgroundTile을 클릭하세요."
        );

        Handles.EndGUI();

        Event currentEvent =
            Event.current;

        if (currentEvent == null ||
            currentEvent.alt)
        {
            return;
        }

        if (currentEvent.type !=
                EventType.MouseDown ||
            currentEvent.button != 0)
        {
            return;
        }

        if (backgroundManager == null)
        {
            Debug.LogWarning(
                "Event Scene MoveActor 타일 선택 실패: " +
                "BackgroundManager를 찾을 수 없습니다."
            );

            CancelMoveActorTileSelection();
            return;
        }

        Ray mouseRay =
            HandleUtility
                .GUIPointToWorldRay(
                    currentEvent.mousePosition
                );

        Vector3 worldPosition =
            mouseRay.origin;

        if (backgroundManager
                .TryGetBackgroundGridPosition(
                    worldPosition,
                    out int gridX,
                    out int gridY) == false)
        {
            return;
        }

        BackgroundTile selectedTile =
            backgroundManager
                .GetBackgroundTileAt(
                    gridX,
                    gridY
                );

        if (selectedTile == null)
        {
            return;
        }

        Undo.RecordObject(
            eventData,
            "Select Event Move Actor Tile"
        );

        step.moveActorDestination =
            new Vector2Int(
                selectedTile.X,
                selectedTile.Y
            );

        EditorUtility.SetDirty(
            eventData
        );

        isSelectingMoveActorTile =
            false;

        selectingMoveActorStepIndex =
            -1;

        currentEvent.Use();

        Repaint();
        SceneView.RepaintAll();
    }

    // <변경부분>
    // MoveActor Scene 좌표 선택 상태를 종료한다.
    private void CancelMoveActorTileSelection()
    {
        isSelectingMoveActorTile =
            false;

        selectingMoveActorStepIndex =
            -1;

        Repaint();
        SceneView.RepaintAll();
    }

    // <변경부분>
    // EventSceneData에 등록된 모든 SpawnActor 위치를
    // Scene View에 제작용 Marker로 표시한다.
    //
    // Handles로만 표시하므로
    // 실제 Scene GameObject / Runtime / Build에는 영향을 주지 않는다.
    private void DrawSpawnActorMarkers(
        EventSceneData eventData,
        BackgroundManager backgroundManager)
    {
        if (eventData == null ||
            eventData.steps == null ||
            backgroundManager == null)
        {
            return;
        }

        Color previousColor =
            Handles.color;



        // <변경부분>
        // Unity 기본 HelpBox 배경을 사용하여
        // 밝거나 복잡한 Background 위에서도 글자가 묻히지 않게 한다.
        GUIStyle markerBoxStyle =
            new GUIStyle(
                EditorStyles.helpBox
            );

        markerBoxStyle.alignment =
            TextAnchor.MiddleCenter;

        markerBoxStyle.normal.textColor =
            Color.white;

        markerBoxStyle.fontStyle =
            FontStyle.Bold;

        markerBoxStyle.fontSize =
            12;

        markerBoxStyle.padding =
            new RectOffset(
                7,
                7,
                4,
                4
            );

        for (int stepIndex = 0;
             stepIndex < eventData.steps.Count;
             stepIndex++)
        {
            EventSceneStepData step =
                eventData.steps[stepIndex];

            if (step == null ||
                step.stepType !=
                    EventSceneStepType.SpawnActor)
            {
                continue;
            }

            BackgroundTile spawnTile =
                backgroundManager
                    .GetBackgroundTileAt(
                        step.spawnActorPosition.x,
                        step.spawnActorPosition.y
                    );

            if (spawnTile == null)
            {
                continue;
            }

            Vector3 markerPosition =
                spawnTile.transform.position;

            bool isCurrentSelectingStep =
                isSelectingSpawnActorTile &&
                selectingSpawnActorStepIndex ==
                    stepIndex;

            // <변경부분>
            // 기존 0.08보다 크게 표시한다.
            // 현재 선택 중인 SpawnActor는 추가로 더 크게 강조한다.
            float markerSize =
                HandleUtility.GetHandleSize(
                    markerPosition
                ) *
                (
                    isCurrentSelectingStep
                        ? 0.18f
                        : 0.14f
                );

            Color markerColor =
                isCurrentSelectingStep
                    ? new Color(
                        1f,
                        0.65f,
                        0f,
                        1f
                    )
                    : new Color(
                        0.15f,
                        1f,
                        0.35f,
                        1f
                    );

            Handles.color =
                markerColor;

            // <변경부분>
            // 기존 Wire Disc만 사용하지 않고
            // 반투명 채움 원을 함께 그려서 타일 위에서도 잘 보이게 한다.
            Handles.DrawSolidDisc(
                markerPosition,
                Vector3.forward,
                markerSize
            );

            // <변경부분>
            // 바깥 테두리는 한 번 더 크게 그려
            // Spawn 위치의 경계를 분명하게 표시한다.
            Handles.color =
                Color.white;

            Handles.DrawWireDisc(
                markerPosition,
                Vector3.forward,
                markerSize * 1.15f
            );

            // <변경부분>
            // 정확한 타일 중심점을 알 수 있도록
            // 중앙 십자선을 표시한다.
            Handles.DrawLine(
                markerPosition +
                Vector3.left *
                markerSize * 0.8f,
                markerPosition +
                Vector3.right *
                markerSize * 0.8f
            );

            Handles.DrawLine(
                markerPosition +
                Vector3.up *
                markerSize * 0.8f,
                markerPosition +
                Vector3.down *
                markerSize * 0.8f
            );

            string actorLabel =
                string.IsNullOrWhiteSpace(
                    step.spawnActorId)
                    ? "Actor ID 없음"
                    : step.spawnActorId;

            string labelText =
                $"Spawn {stepIndex} : {actorLabel}";

            // <변경부분>
            // Label을 World 위치에 바로 그리지 않고
            // GUI 좌표로 변환하여 배경 Box와 함께 표시한다.
            Vector2 guiPosition =
                HandleUtility
                    .WorldToGUIPoint(
                        markerPosition
                    );

            Vector2 labelSize =
                markerBoxStyle
                    .CalcSize(
                        new GUIContent(
                            labelText
                        )
                    );

            Rect labelRect =
                new Rect(
                    guiPosition.x -
                        labelSize.x * 0.5f,
                    guiPosition.y -
                        markerSize * 80f -
                        labelSize.y -
                        6f,
                    labelSize.x,
                    labelSize.y
                );

            Handles.BeginGUI();

            GUI.Box(
                labelRect,
                labelText,
                markerBoxStyle
            );

            Handles.EndGUI();
        }

        Handles.color =
            previousColor;
    }

    // <변경부분>
    // EventSceneData에 등록된 모든 MoveActor 목적지를
    // Scene View에 제작용 Marker로 표시한다.
    //
    // 이전 SpawnActor / MoveActor Step을 순서대로 추적하여
    // 해당 Actor의 이동 시작 위치도 함께 계산한다.
    //
    // 시작 위치와 목적지가 같으면
    // 일반 이동이 아니라 제자리 Jump로 표시한다.
    private void DrawMoveActorMarkers(
        EventSceneData eventData,
        BackgroundManager backgroundManager)
    {
        if (eventData == null ||
            eventData.steps == null ||
            backgroundManager == null)
        {
            return;
        }

        Color previousColor =
            Handles.color;

        // <변경부분>
        // MoveActor Marker의 Actor ID / Step 정보를
        // Scene View에서 쉽게 확인하기 위한 Label Style.
        GUIStyle labelStyle =
            new GUIStyle(
                EditorStyles.helpBox
            );

        labelStyle.alignment =
            TextAnchor.MiddleCenter;

        labelStyle.normal.textColor =
            Color.white;

        labelStyle.fontStyle =
            FontStyle.Bold;

        labelStyle.fontSize =
            12;

        labelStyle.padding =
            new RectOffset(
                7,
                7,
                4,
                4
            );

        for (int stepIndex = 0;
             stepIndex < eventData.steps.Count;
             stepIndex++)
        {
            EventSceneStepData step =
                eventData.steps[stepIndex];

            if (step == null ||
                step.stepType !=
                    EventSceneStepType.MoveActor)
            {
                continue;
            }

            // <변경부분>
            // MoveActor에 설정된 목적지 BackgroundTile을 찾는다.
            BackgroundTile destinationTile =
                backgroundManager
                    .GetBackgroundTileAt(
                        step.moveActorDestination.x,
                        step.moveActorDestination.y
                    );

            if (destinationTile == null)
            {
                continue;
            }

            Vector3 destinationPosition =
                destinationTile.transform.position;

            float markerSize =
                HandleUtility.GetHandleSize(
                    destinationPosition
                ) * 0.11f;

            // <변경부분>
            // MoveActor 목적지는 Spawn Marker와 구분되도록
            // 파란색 계열 Marker로 표시한다.
            Handles.color =
                new Color(
                    0.25f,
                    0.65f,
                    1f,
                    1f
                );

            Handles.DrawWireDisc(
                destinationPosition,
                Vector3.forward,
                markerSize
            );

            // <변경부분>
            // 이 MoveActor Step이 실행되기 직전
            // 해당 Actor가 어느 Tile에 있는지 계산한다.
            bool hasSourcePosition =
                TryGetPlannedActorPositionBeforeStep(
                    eventData,
                    stepIndex,
                    step.moveActorId,
                    out Vector2Int sourceGridPosition
                );

            // <변경부분>
            // 시작 좌표와 목적지가 같으면
            // 제자리 Jump Step이다.
            bool isJump =
                hasSourcePosition &&
                sourceGridPosition ==
                    step.moveActorDestination;

            // <변경부분>
            // 일반 이동이라면 시작 Tile → 목적지 Tile 선을 표시한다.
            //
            // 제자리 Jump는 시작과 끝이 같으므로
            // 이동선을 그리지 않는다.
            if (hasSourcePosition &&
                isJump == false)
            {
                BackgroundTile sourceTile =
                    backgroundManager
                        .GetBackgroundTileAt(
                            sourceGridPosition.x,
                            sourceGridPosition.y
                        );

                if (sourceTile != null)
                {
                    Handles.DrawAAPolyLine(
                        3f,
                        sourceTile.transform.position,
                        destinationPosition
                    );
                }
            }

            string actorLabel =
                string.IsNullOrWhiteSpace(
                    step.moveActorId)
                    ? "Actor ID 없음"
                    : step.moveActorId;

            string moveLabel =
                isJump
                    ? $"Jump {stepIndex} : {actorLabel}"
                    : $"Move {stepIndex} : {actorLabel}";

            Vector2 guiPosition =
                HandleUtility.WorldToGUIPoint(
                    destinationPosition
                );

            Vector2 labelSize =
                labelStyle.CalcSize(
                    new GUIContent(
                        moveLabel
                    )
                );

            Rect labelRect =
                new Rect(
                    guiPosition.x -
                        labelSize.x * 0.5f,
                    guiPosition.y -
                        labelSize.y -
                        20f,
                    labelSize.x,
                    labelSize.y
                );

            Handles.BeginGUI();

            GUI.Box(
                labelRect,
                moveLabel,
                labelStyle
            );

            Handles.EndGUI();
        }

        Handles.color =
            previousColor;
    }

    // <변경부분>
    // BackgroundTile을 Target으로 사용하는 모든 CameraShot 위치를
    // Scene View에 Camera 전용 Marker로 표시한다.
    //
    // SpawnActor의 원형 Marker와 혼동되지 않도록
    // 마름모 형태의 Outline Marker를 사용한다.
    private void DrawCameraShotMarkers(
        EventSceneData eventData,
        BackgroundManager backgroundManager)
    {
        if (eventData == null ||
            eventData.steps == null ||
            backgroundManager == null)
        {
            return;
        }

        Color previousColor =
            Handles.color;

        GUIStyle labelStyle =
            new GUIStyle(
                EditorStyles.helpBox
            );

        labelStyle.alignment =
            TextAnchor.MiddleCenter;

        labelStyle.normal.textColor =
            Color.white;

        labelStyle.fontStyle =
            FontStyle.Bold;

        labelStyle.fontSize =
            12;

        labelStyle.padding =
            new RectOffset(
                7,
                7,
                4,
                4
            );

        for (int stepIndex = 0;
             stepIndex < eventData.steps.Count;
             stepIndex++)
        {
            EventSceneStepData step =
                eventData.steps[stepIndex];

            if (step == null ||
                step.stepType !=
                    EventSceneStepType.CameraShot ||
                step.cameraShotTargetType !=
                    EventSceneCameraShotTargetType.BackgroundTile)
            {
                continue;
            }

            BackgroundTile targetTile =
                backgroundManager
                    .GetBackgroundTileAt(
                        step.cameraShotTargetPosition.x,
                        step.cameraShotTargetPosition.y
                    );

            if (targetTile == null)
            {
                continue;
            }

            Vector3 markerPosition =
                targetTile.transform.position;

            bool isCurrentSelectingStep =
                isSelectingCameraShotTile &&
                selectingCameraShotStepIndex ==
                    stepIndex;

            float markerSize =
                HandleUtility.GetHandleSize(
                    markerPosition
                ) *
                (
                    isCurrentSelectingStep
                        ? 0.18f
                        : 0.14f
                );

            // <변경부분>
            // Spawn의 원형 Marker와 시각적으로 완전히 구분되는
            // 마름모 Camera Target Marker.
            Handles.color =
                isCurrentSelectingStep
                    ? new Color(
                        1f,
                        0.75f,
                        0.15f,
                        1f
                    )
                    : new Color(
                        0.85f,
                        0.35f,
                        1f,
                        1f
                    );

            Vector3 top =
                markerPosition +
                Vector3.up *
                markerSize;

            Vector3 right =
                markerPosition +
                Vector3.right *
                markerSize;

            Vector3 bottom =
                markerPosition +
                Vector3.down *
                markerSize;

            Vector3 left =
                markerPosition +
                Vector3.left *
                markerSize;

            Handles.DrawAAPolyLine(
                4f,
                top,
                right,
                bottom,
                left,
                top
            );

            // Camera Target의 정확한 중심을 작은 점으로 표시한다.
            Handles.DrawSolidDisc(
                markerPosition,
                Vector3.forward,
                markerSize * 0.16f
            );

            string labelText =
                $"Camera {stepIndex}";

            Vector2 guiPosition =
                HandleUtility.WorldToGUIPoint(
                    markerPosition
                );

            Vector2 labelSize =
                labelStyle.CalcSize(
                    new GUIContent(
                        labelText
                    )
                );

            Rect labelRect =
                new Rect(
                    guiPosition.x -
                        labelSize.x * 0.5f,
                    guiPosition.y -
                        labelSize.y -
                        24f,
                    labelSize.x,
                    labelSize.y
                );

            Handles.BeginGUI();

            GUI.Box(
                labelRect,
                labelText,
                labelStyle
            );

            Handles.EndGUI();
        }

        Handles.color =
            previousColor;
    }

    // <변경부분>
    // 지정된 Step이 실행되기 직전까지
    // EventSceneData의 SpawnActor / MoveActor Step을 순서대로 확인하여
    // 해당 Actor가 계획상 어느 BackgroundTile에 있는지 계산한다.
    //
    // Runtime Actor가 아직 생성되지 않은 Editor 상태에서도
    // Scene View에 Move 경로를 미리 표시하기 위해 사용한다.
    private bool TryGetPlannedActorPositionBeforeStep(
        EventSceneData eventData,
        int beforeStepIndex,
        string actorId,
        out Vector2Int gridPosition)
    {
        gridPosition =
            Vector2Int.zero;

        if (eventData == null ||
            eventData.steps == null ||
            string.IsNullOrWhiteSpace(
                actorId))
        {
            return false;
        }

        bool foundActor =
            false;

        // <변경부분>
        // 현재 MoveActor Step 자체는 제외하고
        // 그 이전 Step까지만 검사한다.
        int safeEndIndex =
            Mathf.Min(
                beforeStepIndex,
                eventData.steps.Count
            );

        for (int i = 0;
             i < safeEndIndex;
             i++)
        {
            EventSceneStepData previousStep =
                eventData.steps[i];

            if (previousStep == null)
            {
                continue;
            }

            // <변경부분>
            // 해당 Actor가 Spawn된 최초 위치.
            if (previousStep.stepType ==
                    EventSceneStepType.SpawnActor &&
                previousStep.spawnActorId ==
                    actorId)
            {
                gridPosition =
                    previousStep.spawnActorPosition;

                foundActor =
                    true;

                continue;
            }

            // <변경부분>
            // Spawn 이후 같은 Actor의 MoveActor가 있었다면
            // 가장 최근 목적지가 현재 위치가 된다.
            if (previousStep.stepType ==
                    EventSceneStepType.MoveActor &&
                previousStep.moveActorId ==
                    actorId &&
                foundActor)
            {
                gridPosition =
                    previousStep.moveActorDestination;
            }

            // <변경부분>
            // 성공한 AttackActor는 공격자가 Target의 위치를 점유한다.
            //
            // 현재 Step 직전까지 Target이 실제로 어느 Tile에 있었는지
            // 같은 계획 위치 계산을 재사용하여 가져온다.
            if (previousStep.stepType ==
                    EventSceneStepType.AttackActor &&
                previousStep.attackResult ==
                    EventSceneAttackResult.Success)
            {
                // Target이 현재 조회 중인 Actor라면
                // 성공 공격으로 제거된 상태다.
                if (previousStep.attackTargetActorId ==
                    actorId)
                {
                    foundActor =
                        false;

                    continue;
                }

                // 현재 조회 중인 Actor가 공격자라면
                // 공격 성공 후 Target 위치를 점유한다.
                if (previousStep.attackActorId ==
                        actorId &&
                    foundActor)
                {
                    if (TryGetPlannedActorPositionBeforeStep(
                            eventData,
                            i,
                            previousStep.attackTargetActorId,
                            out Vector2Int targetGridPosition))
                    {
                        gridPosition =
                            targetGridPosition;
                    }
                }
            }

            // <변경부분>
            // RemoveActor 이후에는 해당 Actor가 없는 상태로 계산한다.
            //
            // 이후 동일 ID가 다시 Spawn되면 위 SpawnActor 처리에서
            // 다시 foundActor = true가 된다.
            if (previousStep.stepType ==
                    EventSceneStepType.RemoveActor &&
                previousStep.removeActorId ==
                    actorId)
            {
                foundActor =
                    false;
            }
        }

        return foundActor;
    }


    // <변경부분>
    // SpawnActor Scene 좌표 선택 상태를 종료한다.
    //
    // 잘못된 Step / Scene 변경 / BackgroundManager 누락 등
    // 선택을 계속할 수 없는 상황에서도 공통으로 사용한다.
    private void CancelSpawnActorTileSelection()
    {
        isSelectingSpawnActorTile =
            false;

        selectingSpawnActorStepIndex =
            -1;

        Repaint();
        SceneView.RepaintAll();
    }

    private bool IsLocalizationIdDuplicate(
        EventSceneData eventData)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:EventSceneData"
            );

        for (int i = 0;
             i < guids.Length;
             i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );

            EventSceneData other =
                AssetDatabase.LoadAssetAtPath<
                    EventSceneData
                >(
                    path
                );

            if (other == null ||
                other == eventData)
            {
                continue;
            }

            if (string.Equals(
                    other.localizationId,
                    eventData.localizationId,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }


    private string CreateDefaultLocalizationId(
        EventSceneData eventData)
    {
        string source =
            eventData != null
                ? eventData.name
                : string.Empty;

        string sanitized =
            SanitizeLocalizationId(
                source
            );

        if (string.IsNullOrWhiteSpace(
                sanitized) == false)
        {
            return sanitized;
        }

        return
            CreateStableId(
                "event_scene"
            );
    }


    private string SanitizeLocalizationId(
        string source)
    {
        if (string.IsNullOrWhiteSpace(
                source))
        {
            return string.Empty;
        }

        string snakeCase =
            DevoryaLocalizationEditorUtility
                .ToSnakeCase(
                    source.Trim()
                );

        string sanitized =
            Regex.Replace(
                snakeCase,
                "[^a-z0-9_]+",
                "_"
            );

        sanitized =
            Regex.Replace(
                sanitized,
                "_+",
                "_"
            );

        return
            sanitized.Trim('_');
    }


    private string CreateStableId(
        string prefix)
    {
        string guid =
            Guid.NewGuid()
                .ToString("N")
                .Substring(
                    0,
                    8
                );

        return
            $"{prefix}_{guid}";
    }
}
