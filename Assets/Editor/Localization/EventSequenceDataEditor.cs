using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
// <변경부분>
// Battle Event Step 목록을
// 부드러운 Drag Reorder 방식으로 표시하기 위해 사용.
using UnityEditorInternal;

// <변경부분>
// EventSequenceData의 Dialogue 제작과 Localization을
// 하나의 Inspector에서 관리하기 위한 Editor.
//
// 기본 Event Sequence 설정은 기존 Inspector 구조를 유지하고,
// Dialogue Page만 별도의 중앙 관리 영역에서 편집한다.
//
// 한국어 원문:
// EventSequenceStepData.dialoguePages
//
// 실제 Localization:
// Event_Dialogue String Table
//
// English / Japanese:
// 이 Inspector에서 직접 편집
[CustomEditor(typeof(EventSequenceData))]
public class EventSequenceDataEditor : Editor
{
    private const string TableCollectionName =
        "Event_Dialogue";

    private readonly Dictionary<int, bool>
        dialogueStepFoldouts =
            new Dictionary<int, bool>();

    // <변경부분>
    // Battle Event Step 각각의 Foldout 상태.
    //
    // Dialogue Localization Foldout과는 별도로 관리한다.
    private readonly Dictionary<int, bool>
        eventStepFoldouts =
            new Dictionary<int, bool>();

    // <변경부분>
    // ReorderableList 렌더링 중에는 목록을 직접 변경하지 않는다.
    // 복제 버튼을 누르면 목록 그리기가 끝난 뒤 처리한다.
    private int pendingDuplicateStepIndex = -1;

    // <변경부분>
    // Event Step Header 한 줄의 높이.
    private const float EventStepHeaderHeight =
        24f;

    // <변경부분>
    // Battle Event Sequence의 Steps를
    // Unity ReorderableList로 표시한다.
    //
    // Event Scene Editor와 동일하게
    // Drag 중 다른 Step들이 자연스럽게 밀리면서
    // 순서를 재배치할 수 있다.
    private ReorderableList eventStepsReorderableList;

    // <변경부분>
    // EventSequenceData.steps SerializedProperty.
    private SerializedProperty eventStepsProperty;

    // <변경부분>
    // Inspector가 활성화될 때
    // Battle Event Step ReorderableList를 준비한다.
    private void OnEnable()
    {
        InitializeEventStepsReorderableList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // <변경부분>
        // Steps는 Unity 기본 PropertyField로 표시하지 않는다.
        //
        // 아래 DrawEventSteps()에서
        // Event Scene과 동일한 ReorderableList +
        // Step Type 전용 상세 Editor로 관리한다.
        //
        // localizationId 역시 기존처럼
        // Localization 전용 영역에서 관리한다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizationId",
            "steps"
        );

        serializedObject.ApplyModifiedProperties();

        EventSequenceData sequenceData =
            (EventSequenceData)target;

        EditorGUILayout.Space(12);

        // <변경부분>
        // Battle Event Step 목록 / 선택 Step 상세 편집.
        DrawEventSteps(
            sequenceData
        );

        EditorGUILayout.Space(12);

        // 기존 Dialogue / SpeechBubble / Localization 구조 유지.
        DrawLocalizationInspector(
            sequenceData
        );
    }

    // <변경부분>
    // EventSequenceData.steps를
    // Unity ReorderableList에 연결한다.
    //
    // 기존 Event Scene Editor와 동일하게:
    // - 부드러운 Drag Reorder
    // - 왼쪽 Drag Handle
    // - Step01 / Step02 번호
    // - + / - 추가 삭제
    // 구조를 사용한다.
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
        // ReorderableList 안에서는 GUILayout로 실제 높이를 측정하지 않는다.
        //
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

                if (GetEventStepFoldout(
                        index) == false)
                {
                    return EventStepHeaderHeight;
                }

                SerializedProperty stepProperty =
                    eventStepsProperty
                        .GetArrayElementAtIndex(
                            index
                        );

                if (stepProperty == null)
                {
                    return EventStepHeaderHeight;
                }

                return
                    EventStepHeaderHeight +
                    GetExpandedEventStepHeight(
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

                SerializedProperty stepProperty =
                    eventStepsProperty
                        .GetArrayElementAtIndex(
                            index
                        );

                if (stepProperty == null)
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
                    "None";

                if (stepTypeProperty != null &&
                    stepTypeProperty.enumValueIndex >= 0 &&
                    stepTypeProperty.enumValueIndex <
                        stepTypeProperty.enumDisplayNames.Length)
                {
                    stepTypeName =
                        stepTypeProperty.enumDisplayNames[
                            stepTypeProperty.enumValueIndex];
                }

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
                    GetEventStepFoldout(
                        index
                    );

                // <변경부분>
                // 왼쪽 ReorderableList Drag Handle과
                // Foldout 화살표가 겹치지 않도록 여백을 유지한다.
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
                    eventStepFoldouts[index] =
                        newExpanded;

                    Repaint();
                }

                if (newExpanded == false)
                {
                    return;
                }

                // <변경부분>
                // GUILayout / EditorGUILayout을 사용하지 않고
                // ReorderableList가 제공한 Rect 안에서
                // EditorGUI로 직접 상세 UI를 그린다.
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

                DrawExpandedEventStep(
       contentRect,
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
                EventSequenceData sequenceData =
                    target as EventSequenceData;

                if (sequenceData == null)
                {
                    return;
                }

                // 현재 Serialized 변경부터 확정한다.
                serializedObject
                    .ApplyModifiedProperties();

                AddEventStep(
                    sequenceData
                );

                serializedObject.Update();

                eventStepsProperty =
                    serializedObject.FindProperty(
                        "steps"
                    );

                if (eventStepsProperty != null &&
     eventStepsProperty.arraySize > 0)
                {
                    list.index =
                        eventStepsProperty.arraySize -
                        1;

                    // <변경부분>
                    // Step 추가 후 기존 Foldout 상태를 정리하고,
                    // 방금 추가한 마지막 Step만 바로 펼쳐준다.
                    eventStepFoldouts.Clear();

                    eventStepFoldouts[
                        eventStepsProperty.arraySize - 1
                    ] = true;
                }

                Repaint();
            };


        // =====================================================
        // Remove
        // =====================================================

        eventStepsReorderableList.onRemoveCallback =
            list =>
            {
                EventSequenceData sequenceData =
                    target as EventSequenceData;

                if (sequenceData == null ||
                    sequenceData.steps == null ||
                    list.index < 0 ||
                    list.index >=
                        sequenceData.steps.Count)
                {
                    return;
                }

                int removeIndex =
                    list.index;

                serializedObject
                    .ApplyModifiedProperties();

                RemoveEventStep(
                    sequenceData,
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
                // <변경부분>
                // Step 삭제 후 Index 기반 Foldout 상태를 초기화한다.
                eventStepFoldouts.Clear();

                // Dialogue / SpeechBubble Localization Foldout도
                // Step Index가 달라졌으므로 함께 초기화한다.
                dialogueStepFoldouts.Clear();

                Repaint();
            };


        // =====================================================
        // Reorder
        // =====================================================

        eventStepsReorderableList.onReorderCallback =
            list =>
            {
                EventSequenceData sequenceData =
                    target as EventSequenceData;

                if (sequenceData == null)
                {
                    return;
                }

                // <변경부분>
                // 실제 List Reorder는 Unity가 수행한다.
                //
                // EventSequenceStepData 객체 전체가 이동하므로
                // Dialogue / SpeechBubble Stable Localization ID도
                // 해당 Step과 같이 이동한다.
                serializedObject
                    .ApplyModifiedProperties();

                EditorUtility.SetDirty(
                    sequenceData
                );

                // <변경부분>
                // Drag로 Step 순서가 바뀌었으므로
                // Index 기반 Foldout 상태를 초기화한다.
                eventStepFoldouts.Clear();

                // Dialogue / SpeechBubble Foldout 역시
                // Step Index 기준이므로 함께 초기화한다.
                dialogueStepFoldouts.Clear();

                Repaint();
            };
    }

    // <변경부분>
    // Dialogue Step만 대상으로 전체 펼치기 / 접기를 수행한다.
    //
    // SpeechBubble Foldout 상태에는 영향을 주지 않는다.
    private void DrawDialogueStepFoldoutControls(
        EventSequenceData sequenceData)
    {
        if (sequenceData == null ||
            sequenceData.steps == null)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(
                    "▼ Dialogue 전체 펼치기"))
            {
                SetAllDialogueStepFoldouts(
                    sequenceData,
                    true
                );
            }

            if (GUILayout.Button(
                    "▲ Dialogue 전체 접기"))
            {
                SetAllDialogueStepFoldouts(
                    sequenceData,
                    false
                );
            }
        }
    }


    private void SetAllDialogueStepFoldouts(
        EventSequenceData sequenceData,
        bool expanded)
    {
        if (sequenceData == null ||
            sequenceData.steps == null)
        {
            return;
        }

        for (int i = 0;
             i < sequenceData.steps.Count;
             i++)
        {
            EventSequenceStepData step =
                sequenceData.steps[i];

            if (step == null ||
                step.stepType !=
                    EventSequenceStepType.Dialogue)
            {
                continue;
            }

            dialogueStepFoldouts[i] =
                expanded;
        }

        Repaint();
    }

    // <변경부분>
    // Battle Event Steps.
    //
    // 각 Step 자체가 Foldout이며,
    // 펼친 Step 바로 아래에서 해당 Step을 수정한다.
    //
    // ReorderableList Drag Reorder는 그대로 유지한다.
    private void DrawEventSteps(
        EventSequenceData sequenceData)
    {
        EditorGUILayout.LabelField(
            "Event Steps",
            EditorStyles.boldLabel
        );

        if (sequenceData == null)
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
                "EventSequenceData.steps를 찾을 수 없습니다.",
                MessageType.Error
            );

            return;
        }

        serializedObject.Update();

        int stepCount =
            eventStepsProperty.arraySize;

        // <변경부분>
        // 목록 최상단 전체 펼치기 / 접기.
        DrawEventStepFoldoutControls(
            stepCount
        );

        EditorGUILayout.Space(3);

        eventStepsReorderableList
     .DoLayoutList();

        serializedObject
            .ApplyModifiedProperties();

        // <변경부분>
        // ReorderableList 렌더링이 끝난 다음 복제를 처리한다.
        if (pendingDuplicateStepIndex >= 0)
        {
            int sourceIndex =
                pendingDuplicateStepIndex;

            pendingDuplicateStepIndex = -1;

            DuplicateEventSequenceStep(
                sequenceData,
                sourceIndex
            );

            // <변경부분>
            // Step 개수가 변경되었으므로
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
            // 현재 GUI Event에서 변경 전 Layout을 재사용하지 않는다.
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
        // 긴 Sequence에서는 아래까지 내려온 상태에서도
        // 다시 위로 올라갈 필요 없이 전체 상태를 변경할 수 있다.
        DrawEventStepFoldoutControls(
            eventStepsProperty.arraySize
        );
    }

    private float GetExpandedEventStepHeight(
       SerializedProperty stepProperty,
       int stepIndex)
    {
        Rect measureRect =
            new Rect(
                0f,
                0f,
                500f,
                0f
            );

        return
            ProcessExpandedEventStep(
                measureRect,
                stepProperty,
                stepIndex,
                false
            );
    }


    // <변경부분>
    // 펼쳐진 Battle Event Step을
    // ReorderableList가 제공한 Rect 내부에 직접 그린다.
    private void DrawExpandedEventStep(
        Rect contentRect,
        SerializedProperty stepProperty,
        int stepIndex)
    {
        ProcessExpandedEventStep(
            contentRect,
            stepProperty,
            stepIndex,
            true
        );
    }


    // <변경부분>
    // draw == false:
    //   필요한 높이만 계산.
    //
    // draw == true:
    //   동일한 계산 순서로 실제 EditorGUI를 출력.
    //
    // 높이 계산과 실제 Draw 순서를 하나의 함수에서 처리하므로
    // 두 로직이 서로 어긋나지 않는다.
    private float ProcessExpandedEventStep(
        Rect contentRect,
        SerializedProperty stepProperty,
        int stepIndex,
        bool draw)
    {
        float startY =
            contentRect.y;

        float currentY =
            startY;

        if (stepProperty == null)
        {
            ProcessEventStepHelpBox(
                ref currentY,
                contentRect,
                "Event Step Property를 찾을 수 없습니다.",
                MessageType.Error,
                1,
                draw
            );

            return
                Mathf.Max(
                    40f,
                    currentY - startY
                );
        }

        ProcessEventStepProperty(
            ref currentY,
            contentRect,
            stepProperty,
            "stepName",
            "Step Name",
            false,
            draw
        );

        ProcessEventStepProperty(
            ref currentY,
            contentRect,
            stepProperty,
            "stepType",
            "Step Type",
            false,
            draw
        );

        currentY +=
            7f;

        SerializedProperty stepTypeProperty =
            stepProperty.FindPropertyRelative(
                "stepType"
            );

        if (stepTypeProperty == null)
        {
            ProcessEventStepHelpBox(
                ref currentY,
                contentRect,
                "Step Type Property를 찾을 수 없습니다.",
                MessageType.Error,
                1,
                draw
            );

            return
                Mathf.Max(
                    40f,
                    currentY - startY
                );
        }

        EventSequenceStepType stepType =
            (EventSequenceStepType)
            stepTypeProperty.intValue;

        switch (stepType)
        {
            case EventSequenceStepType.None:

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "아무 동작도 하지 않는 Step입니다.",
                    MessageType.None,
                    1,
                    draw
                );

                break;


            case EventSequenceStepType.Dialogue:

                SerializedProperty dialoguePagesProperty =
                    stepProperty.FindPropertyRelative(
                        "dialoguePages"
                    );

                int dialoguePageCount =
                    dialoguePagesProperty != null
                        ? dialoguePagesProperty.arraySize
                        : 0;

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "Dialogue 내용은 아래 Event Dialogue Localization의 " +
                    "Dialogue Pages 영역에서 관리합니다.\n" +
                    $"현재 Page: {dialoguePageCount}",
                    MessageType.Info,
                    2,
                    draw
                );

                break;


            case EventSequenceStepType.ForcePieceSelect:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Piece Target",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "targetPieceTeam",
                    "Target Team",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "targetPiecePosition",
                    "Target Position",
                    false,
                    draw
                );

                ProcessEventMarkerSettings(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw
                );

                break;


            case EventSequenceStepType.ForceTileSelect:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Tile Target",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "targetTilePosition",
                    "Target Position",
                    false,
                    draw
                );

                ProcessEventMarkerSettings(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw
                );

                break;


            case EventSequenceStepType.ForceButton:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Button Target",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "targetButton",
                    "Target Button",
                    false,
                    draw
                );

                ProcessEventMarkerSettings(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    draw
                );

                break;


            case EventSequenceStepType.SpawnPiece:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Spawn Piece",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "spawnPieceData",
                    "Piece Data",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "spawnPieceTeam",
                    "Team",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "spawnPosition",
                    "Spawn Position",
                    false,
                    draw
                );

                currentY +=
                    5f;

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Spawn Override",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "spawnOverride",
                    "Override Settings",
                    true,
                    draw
                );

                break;


            case EventSequenceStepType.RemovePiece:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Remove Piece",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "removePiecePosition",
                    "Piece Position",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "checkRemovePieceTeam",
                    "Check Team",
                    false,
                    draw
                );

                SerializedProperty checkTeamProperty =
                    stepProperty.FindPropertyRelative(
                        "checkRemovePieceTeam"
                    );

                if (checkTeamProperty != null &&
                    checkTeamProperty.boolValue)
                {
                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "removePieceTeam",
                        "Piece Team",
                        false,
                        draw
                    );
                }

                break;


            case EventSequenceStepType.Wait:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Wait",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "waitDuration",
                    "Wait Duration",
                    false,
                    draw
                );

                break;


            case EventSequenceStepType.CompleteSequence:

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "현재 Event Sequence를 즉시 완료합니다.",
                    MessageType.Info,
                    1,
                    draw
                );

                break;


            case EventSequenceStepType.CommitPlayerPiecesToRunState:

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "현재 Board의 Player Piece 상태를 RunState에 저장합니다.\n" +
                    "추가 설정값은 없습니다.",
                    MessageType.Info,
                    2,
                    draw
                );

                break;


            case EventSequenceStepType.AdvanceBattleTurn:

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "BattleManager의 정상 EndTurn 흐름을 사용해 다음 진영으로 Turn을 넘깁니다.\n" +
                    "추가 설정값은 없습니다.",
                    MessageType.Info,
                    2,
                    draw
                );

                break;


            case EventSequenceStepType.ExecutePieceAction:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Execute Piece Action",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "actionPieceTeam",
                    "Piece Team",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "actionPiecePosition",
                    "Piece Position",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "actionTargetPosition",
                    "Target Position",
                    false,
                    draw
                );

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "Target Position이 빈 Tile이면 이동하고, 상대 Piece가 있으면 공격으로 처리됩니다.",
                    MessageType.None,
                    2,
                    draw
                );

                break;


            case EventSequenceStepType.ExecutePieceUniqueSkill:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Execute Piece Ability",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "uniqueSkillPieceTeam",
                    "Piece Team",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "uniqueSkillPiecePosition",
                    "Piece Position",
                    false,
                    draw
                );

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "지정한 Piece가 현재 보유한 Ability를 사용합니다.",
                    MessageType.None,
                    1,
                    draw
                );

                break;


            case EventSequenceStepType.SpeechBubble:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Speech Bubble",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "speechBubblePieceTeam",
                    "Piece Team",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "speechBubblePiecePosition",
                    "Piece Position",
                    false,
                    draw
                );

                currentY +=
                    4f;

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Presentation",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "speechBubbleDuration",
                    "Duration",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "speechBubbleTypingSpeed",
                    "Typing Speed",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "speechBubbleUseEmphasisShake",
                    "Use Emphasis Shake",
                    false,
                    draw
                );

                SerializedProperty emphasisProperty =
                    stepProperty.FindPropertyRelative(
                        "speechBubbleUseEmphasisShake"
                    );

                if (emphasisProperty != null &&
                    emphasisProperty.boolValue)
                {
                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "speechBubbleEmphasisStrength",
                        "Emphasis Strength",
                        false,
                        draw
                    );
                }

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "speechBubbleWaitForComplete",
                    "Wait For Complete",
                    false,
                    draw
                );

                ProcessEventStepHelpBox(
    ref currentY,
    contentRect,
    "SpeechBubble의 Korean / English / Japanese Text는 " +
    "아래 Speech Bubble Texts 영역에서 관리합니다.",
    MessageType.Info,
    2,
    draw
);

                break;


            // =====================================================
            // Battle UI Visibility
            // =====================================================

            // <변경부분>
            // EndTimerRoot_Battle과 GoldRoot를 각각 제어한다.
            case EventSequenceStepType.SetBattleUIVisibility:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "End Timer Root",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "battleUIChangeEndTimerRoot",
                    "Change End Timer Root",
                    false,
                    draw
                );

                SerializedProperty changeEndTimerProperty =
                    stepProperty.FindPropertyRelative(
                        "battleUIChangeEndTimerRoot"
                    );

                bool changeEndTimer =
                    changeEndTimerProperty != null &&
                    changeEndTimerProperty.boolValue;

                if (changeEndTimer)
                {
                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "battleUIShowEndTimerRoot",
                        "Show End Timer Root",
                        false,
                        draw
                    );
                }

                currentY += 6f;

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Gold Root",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "battleUIChangeGoldRoot",
                    "Change Gold Root",
                    false,
                    draw
                );

                SerializedProperty changeGoldProperty =
                    stepProperty.FindPropertyRelative(
                        "battleUIChangeGoldRoot"
                    );

                bool changeGold =
                    changeGoldProperty != null &&
                    changeGoldProperty.boolValue;

                if (changeGold)
                {
                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "battleUIShowGoldRoot",
                        "Show Gold Root",
                        false,
                        draw
                    );
                }

                currentY += 6f;

                // =================================================
                // UI Fade
                // =================================================

                // <변경부분>
                // 두 UI 중 하나라도 변경하는 경우에만
                // Fade 옵션을 표시한다.
                if (changeEndTimer || changeGold)
                {
                    ProcessEventStepSectionLabel(
                        ref currentY,
                        contentRect,
                        "UI Fade",
                        draw
                    );

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "battleUIFadeDuration",
                        "Fade Duration",
                        false,
                        draw
                    );

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "battleUIWaitForComplete",
                        "Wait For Complete",
                        false,
                        draw
                    );
                }

                currentY += 6f;

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "Change OFF: 현재 UI 상태 유지.\n" +
                    "Show OFF: Fade Out / Show ON: Fade In.\n" +
                    "Fade Duration이 0이면 즉시 전환합니다.\n" +
                    "Wait For Complete를 켜면 페이드 완료 후 진행합니다.\n" +
                    "시퀀스 종료 또는 중단 시 최초 상태로 복구합니다.",
                    MessageType.Info,
                    5,
                    draw
                );

                break;


            // <변경부분>
            // 보상 팝업을 표시한 뒤에도
            // 다음 Event Step으로 진행하는 전용 Step.
            //
            // 추가 Serialized 필드가 필요하지 않으므로
            // 안내 문구만 표시한다.
            case EventSequenceStepType.ShowBattleRewardPopup:

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    "기존 Battle 승리 / 보상 처리를 실행하고 " +
                    "리워드 팝업을 표시합니다.\n" +
                    "Event Sequence는 종료하지 않으므로 " +
                    "뒤에 Dialogue Step을 배치할 수 있습니다.",
                    MessageType.Info,
                    3,
                    draw
                );

                break;


            // =====================================================
            // Screen Glitch
            // =====================================================

            // <변경부분>
            // 기존 EventScene의 ScreenGlitch와 동일한
            // Inspector 설정을 Battle Event에서도 사용한다.
            case EventSequenceStepType.ScreenGlitch:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Screen Glitch",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "screenGlitchStrength",
                    "Glitch Strength",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "screenGlitchDuration",
                    "Glitch Duration",
                    false,
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "screenGlitchWaitForComplete",
                    "Wait For Complete",
                    false,
                    draw
                );

                SerializedProperty glitchWaitProperty =
                    stepProperty.FindPropertyRelative(
                        "screenGlitchWaitForComplete"
                    );

                bool waitForGlitch =
                    glitchWaitProperty != null &&
                    glitchWaitProperty.boolValue;

                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    waitForGlitch
                        ? "화면 글리치가 끝난 뒤 다음 Event Step으로 진행합니다."
                        : "화면 글리치를 시작한 뒤 기다리지 않고 다음 Event Step으로 진행합니다.",
                    MessageType.Info,
                    2,
                    draw
                );

                break;


            // =====================================================
            // Global Lighting
            // =====================================================

            // <변경부분>
            // 기존 EventScene과 동일하게
            // Global Light와 Screen Flash를 각각 ON/OFF한다.
            case EventSequenceStepType.GlobalLighting:

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Global Lighting",
                    draw
                );

                // <변경부분>
                // Global Light 사용 여부.
                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "globalLightingUseGlobalLight",
                    "Use Global Light",
                    false,
                    draw
                );

                SerializedProperty useGlobalLightProperty =
                    stepProperty.FindPropertyRelative(
                        "globalLightingUseGlobalLight"
                    );

                bool useGlobalLight =
                    useGlobalLightProperty != null &&
                    useGlobalLightProperty.boolValue;

                // <변경부분>
                // Global Light가 ON일 때만
                // 색상, 밝기, Fade Duration을 표시한다.
                if (useGlobalLight)
                {
                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingTargetColor",
                        "Target Color",
                        false,
                        draw
                    );

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingTargetIntensity",
                        "Target Intensity",
                        false,
                        draw
                    );

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingFadeDuration",
                        "Fade Duration",
                        false,
                        draw
                    );
                }

                currentY += 5f;

                // =================================================
                // Character Fill
                // =================================================

                // <변경부분>
                // Character_Fill 사용 여부를 독립적으로 설정한다.
                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Character Fill",
                    draw
                );

                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "globalLightingUseCharacterFill",
                    "Use Character Fill",
                    false,
                    draw
                );

                SerializedProperty useCharacterFillProperty =
                    stepProperty.FindPropertyRelative(
                        "globalLightingUseCharacterFill"
                    );

                bool useCharacterFill =
                    useCharacterFillProperty != null &&
                    useCharacterFillProperty.boolValue;

                // <변경부분>
                // 체크박스가 ON일 때만 색상, 밝기, 페이드 시간을 표시한다.
                if (useCharacterFill)
                {
                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingCharacterFillTargetColor",
                        "Fill Color",
                        false,
                        draw
                    );

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingCharacterFillTargetIntensity",
                        "Fill Intensity",
                        false,
                        draw
                    );

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingCharacterFillFadeDuration",
                        "Fill Fade Duration",
                        false,
                        draw
                    );
                }

                currentY += 5f;

                // =================================================
                // Screen Flash
                // =================================================

                ProcessEventStepSectionLabel(
                    ref currentY,
                    contentRect,
                    "Screen Flash",
                    draw
                );

                // <변경부분>
                // 화면 플래시 사용 여부.
                ProcessEventStepProperty(
                    ref currentY,
                    contentRect,
                    stepProperty,
                    "globalLightingUseScreenFlash",
                    "Use Screen Flash",
                    false,
                    draw
                );

                SerializedProperty useScreenFlashProperty =
                    stepProperty.FindPropertyRelative(
                        "globalLightingUseScreenFlash"
                    );

                bool useScreenFlash =
                    useScreenFlashProperty != null &&
                    useScreenFlashProperty.boolValue;

                // <변경부분>
                // Screen Flash가 ON일 때만
                // 색상, 강도, 지속 시간을 표시한다.
                if (useScreenFlash)
                {
                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingFlashColor",
                        "Flash Color",
                        false,
                        draw
                    );

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingFlashStrength",
                        "Flash Strength",
                        false,
                        draw
                    );

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingFlashDuration",
                        "Flash Duration",
                        false,
                        draw
                    );
                }


                // =================================================
                // Wait For Complete
                // =================================================

                // <변경부분>
                // 두 연출 중 하나라도 활성화되어 있을 때만
                // Wait For Complete를 표시한다.
                //
                // 실제 대기 처리는 다음 런타임 작업에서 구현한다.
                if (useGlobalLight || useCharacterFill || useScreenFlash)
                {
                    currentY += 5f;

                    ProcessEventStepProperty(
                        ref currentY,
                        contentRect,
                        stepProperty,
                        "globalLightingWaitForComplete",
                        "Wait For Complete",
                        false,
                        draw
                    );
                }

                // <변경부분>
                // 세 효과의 ON/OFF 상태를 모두 반영한다.
                ProcessEventStepHelpBox(
                    ref currentY,
                    contentRect,
                    useGlobalLight || useCharacterFill || useScreenFlash
                        ? "Global Light, Character Fill, Screen Flash는 각각 선택적으로 실행됩니다.\n" +
                          "Wait For Complete는 활성화된 모든 효과가 끝날 때까지 기다립니다."
                        : "Global Light, Character Fill, Screen Flash가 모두 OFF입니다. " +
                          "이 Step에서는 아무 연출도 실행하지 않습니다.",
                    useGlobalLight || useCharacterFill || useScreenFlash
                        ? MessageType.Info
                        : MessageType.None,
                    3,
                    draw
                );

                break;
        }

        // <변경부분>
        // 펼쳐진 모든 Battle Event Step의 맨 아래에
        // 현재 Step을 복제하는 버튼을 표시한다.
        //
        // 높이 계산과 실제 그리기에 동일한 함수를 사용한다.
        currentY += 8f;

        if (ProcessEventStepButton(
                ref currentY,
                contentRect,
                "Duplicate Step Below",
                draw))
        {
            // <변경부분>
            // ReorderableList Draw 중에는 Insert하지 않고
            // 목록 그리기가 끝난 뒤 처리하도록 예약한다.
            pendingDuplicateStepIndex =
                stepIndex;
        }

        return
            Mathf.Max(
                40f,
                currentY - startY
            );
    }


    // <변경부분>
    // Rect 기반 Button.
    // 높이 계산과 실제 GUI 출력을 함께 처리한다.
    private bool ProcessEventStepButton(
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
    // Rect 기반 PropertyField.
    //
    // 높이 계산과 실제 Draw에서 동일하게 사용한다.
    private void ProcessEventStepProperty(
        ref float currentY,
        Rect contentRect,
        SerializedProperty stepProperty,
        string propertyName,
        string displayName,
        bool includeChildren,
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
            ProcessEventStepHelpBox(
                ref currentY,
                contentRect,
                $"Step Property를 찾을 수 없습니다: {propertyName}",
                MessageType.Warning,
                1,
                draw
            );

            return;
        }

        GUIContent label =
            new GUIContent(
                displayName
            );

        float propertyHeight =
            EditorGUI.GetPropertyHeight(
                property,
                label,
                includeChildren
            );

        if (draw)
        {
            Rect propertyRect =
                new Rect(
                    contentRect.x,
                    currentY,
                    contentRect.width,
                    propertyHeight
                );

            EditorGUI.PropertyField(
                propertyRect,
                property,
                label,
                includeChildren
            );
        }

        currentY +=
            propertyHeight +
            EditorGUIUtility.standardVerticalSpacing;
    }


    // <변경부분>
    // Rect 기반 Section Label.
    private void ProcessEventStepSectionLabel(
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
    //
    // lineCount를 사용해 ReorderableList의 높이 계산과
    // 실제 HelpBox 높이를 동일하게 유지한다.
    private void ProcessEventStepHelpBox(
        ref float currentY,
        Rect contentRect,
        string message,
        MessageType messageType,
        int lineCount,
        bool draw)
    {
        float helpBoxHeight =
            Mathf.Max(
                32f,
                EditorGUIUtility.singleLineHeight *
                Mathf.Max(
                    1,
                    lineCount
                ) +
                12f
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
    // ForcePieceSelect / ForceTileSelect / ForceButton에서 사용하는
    // Marker 설정을 Rect 기반으로 출력한다.
    private void ProcessEventMarkerSettings(
        ref float currentY,
        Rect contentRect,
        SerializedProperty stepProperty,
        bool draw)
    {
        currentY +=
            5f;

        ProcessEventStepSectionLabel(
            ref currentY,
            contentRect,
            "Marker",
            draw
        );

        ProcessEventStepProperty(
            ref currentY,
            contentRect,
            stepProperty,
            "showMarker",
            "Show Marker",
            false,
            draw
        );

        SerializedProperty showMarkerProperty =
            stepProperty.FindPropertyRelative(
                "showMarker"
            );

        if (showMarkerProperty == null ||
            showMarkerProperty.boolValue == false)
        {
            return;
        }

        ProcessEventStepProperty(
            ref currentY,
            contentRect,
            stepProperty,
            "markerDisplayType",
            "Display Type",
            false,
            draw
        );

        ProcessEventStepProperty(
            ref currentY,
            contentRect,
            stepProperty,
            "markerPositionOffset",
            "Position Offset",
            false,
            draw
        );
    }

    // <변경부분>
    // Event Step 목록의 전체 펼치기 / 접기 버튼.
    //
    // 동일한 버튼을 목록 위와 아래 양쪽에서 사용한다.
    private void DrawEventStepFoldoutControls(
        int stepCount)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(
                    "▼ 전체 펼치기"))
            {
                SetAllEventStepFoldouts(
                    stepCount,
                    true
                );
            }

            if (GUILayout.Button(
                    "▲ 전체 접기"))
            {
                SetAllEventStepFoldouts(
                    stepCount,
                    false
                );
            }
        }
    }

    private void SetAllEventStepFoldouts(
    int stepCount,
    bool expanded)
    {
        eventStepFoldouts.Clear();

        for (int i = 0;
             i < stepCount;
             i++)
        {
            eventStepFoldouts[i] =
                expanded;
        }

        Repaint();
    }


    // <변경부분>
    // 개별 Battle Event Step의 Foldout 상태.
    //
    // 기본값은 접힘으로 한다.
    private bool GetEventStepFoldout(
        int stepIndex)
    {
        if (eventStepFoldouts.TryGetValue(
                stepIndex,
                out bool expanded))
        {
            return expanded;
        }

        eventStepFoldouts[stepIndex] =
            false;

        return false;
    }

    // <변경부분>
    // 새 Battle Event Step을 추가한다.
    //
    // ReorderableList 기본 Add를 사용하면
    // 직전 Element의 값이 복제될 가능성이 있으므로,
    // 항상 완전히 새로운 EventSequenceStepData를 생성한다.
    //
    // 특히 Dialogue / SpeechBubble Stable ID가
    // 복제되는 문제를 방지한다.
    private void AddEventStep(
        EventSequenceData sequenceData)
    {
        if (sequenceData == null)
        {
            return;
        }

        Undo.RecordObject(
            sequenceData,
            "Add Event Sequence Step"
        );

        if (sequenceData.steps == null)
        {
            sequenceData.steps =
                new List<EventSequenceStepData>();
        }

        sequenceData.steps.Add(
            new EventSequenceStepData()
        );

        EditorUtility.SetDirty(
      sequenceData
  );
    }


    // =====================================================
    // Duplicate Event Step
    // =====================================================

    // <변경부분>
    // 현재 Battle Event Step을 복제해 원본 바로 아래에 삽입한다.
    //
    // Unity Asset 참조는 유지하고
    // List와 중첩 Serializable 데이터는 독립적으로 복사한다.
    private void DuplicateEventSequenceStep(
        EventSequenceData sequenceData,
        int sourceIndex)
    {
        if (sequenceData == null ||
            sequenceData.steps == null ||
            sourceIndex < 0 ||
            sourceIndex >= sequenceData.steps.Count)
        {
            return;
        }

        EventSequenceStepData source =
            sequenceData.steps[sourceIndex];

        if (source == null)
        {
            return;
        }

        // <변경부분>
        // 복제 작업 전체를 Undo로 되돌릴 수 있도록 기록한다.
        Undo.RecordObject(
            sequenceData,
            "Duplicate Event Sequence Step"
        );

        // <변경부분>
        // Unity 직렬화를 사용한 깊은 복사.
        // 원본 Step의 모든 설정값을 유지한다.
        EventSequenceStepData copy =
            new EventSequenceStepData();

        EditorJsonUtility.FromJsonOverwrite(
            EditorJsonUtility.ToJson(source),
            copy
        );

        // =================================================
        // Localization ID
        // =================================================

        // <변경부분>
        // 복제본은 원본과 다른 Localization ID를 가져야 한다.
        copy.dialogueLocalizationId =
            CreateStableId("dialogue");

        copy.speechBubbleLocalizationId =
            CreateStableId("speech");

        // <변경부분>
        // Dialogue 페이지 수와 Localization Metadata 수를 맞춘다.
        if (copy.stepType ==
            EventSequenceStepType.Dialogue)
        {
            EnsureStepLocalizationMetadata(
                copy
            );
        }

        // <변경부분>
        // 복제한 Dialogue Page마다 새로운 Stable ID를 생성한다.
        if (copy.dialogueLocalizationPages != null)
        {
            foreach (
                EventDialoguePageLocalizationData page
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

        // <변경부분>
        // SpeechBubble도 기존 Localization Reference를 공유하지 않는다.
        copy.speechBubbleLocalizedText =
            new LocalizedString();


        // =================================================
        // Localization Table Copy
        // =================================================

        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        bool canCopyLocalization =
            collection != null &&
            !string.IsNullOrWhiteSpace(
                sequenceData.localizationId
            );

        if (canCopyLocalization)
        {
            if (collection.SharedData != null)
            {
                Undo.RecordObject(
                    collection.SharedData,
                    "Duplicate Event Sequence Localization Keys"
                );
            }

            // <변경부분>
            // Dialogue의 페이지별 KR / EN / JA 번역값을
            // 새로운 Localization Key로 복사한다.
            if (copy.stepType ==
                    EventSequenceStepType.Dialogue &&
                copy.dialoguePages != null)
            {
                for (int i = 0;
                     i < copy.dialoguePages.Count;
                     i++)
                {
                    EventDialoguePageLocalizationData sourcePage =
                        GetPageLocalizationData(
                            source,
                            i
                        );

                    EventDialoguePageLocalizationData targetPage =
                        GetPageLocalizationData(
                            copy,
                            i
                        );

                    if (targetPage == null)
                    {
                        continue;
                    }

                    string oldKey =
                        GetPageKey(
                            sequenceData,
                            source,
                            sourcePage
                        );

                    string newKey =
                        GetPageKey(
                            sequenceData,
                            copy,
                            targetPage
                        );

                    CopyEventSequenceTranslationValues(
                        collection,
                        oldKey,
                        newKey,
                        copy.dialoguePages[i]
                    );

                    targetPage.localizedText =
                        DevoryaLocalizationEditorUtility
                            .CreateLocalizedStringReference(
                                collection,
                                newKey
                            );
                }
            }

            // <변경부분>
            // SpeechBubble의 KR / EN / JA 번역도
            // 새 Localization Key로 독립 복사한다.
            else if (copy.stepType ==
                     EventSequenceStepType.SpeechBubble)
            {
                string oldKey =
                    GetSpeechBubbleKey(
                        sequenceData,
                        source
                    );

                string newKey =
                    GetSpeechBubbleKey(
                        sequenceData,
                        copy
                    );

                CopyEventSequenceTranslationValues(
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


        // =================================================
        // Insert Step
        // =================================================

        // <변경부분>
        // 원본 바로 아래에 복제된 Step을 삽입한다.
        sequenceData.steps.Insert(
            sourceIndex + 1,
            copy
        );

        // <변경부분>
        // 기존 Foldout 상태를 보존하면서
        // 새로 복제한 Step은 펼쳐진 상태로 만든다.
        ShiftEventSequenceFoldoutsAfterDuplicate(
            sourceIndex
        );

        EditorUtility.SetDirty(
            sequenceData
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


    // =====================================================
    // Duplicate Foldout Handling
    // =====================================================

    // <변경부분>
    // Step이 삽입되면 기존 Index 기반 Foldout 상태를 이동한다.
    private void ShiftEventSequenceFoldoutsAfterDuplicate(
        int sourceIndex)
    {
        Dictionary<int, bool> shiftedSteps =
            new Dictionary<int, bool>();

        foreach (
            KeyValuePair<int, bool> entry
            in eventStepFoldouts)
        {
            int newIndex =
                entry.Key > sourceIndex
                    ? entry.Key + 1
                    : entry.Key;

            shiftedSteps[newIndex] =
                entry.Value;
        }

        eventStepFoldouts.Clear();

        foreach (
            KeyValuePair<int, bool> entry
            in shiftedSteps)
        {
            eventStepFoldouts[entry.Key] =
                entry.Value;
        }

        eventStepFoldouts[sourceIndex + 1] =
            true;

        // <변경부분>
        // Dialogue / SpeechBubble Localization 영역도
        // Step Index를 사용하므로 함께 이동시킨다.
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


    // =====================================================
    // Duplicate Localization
    // =====================================================

    // <변경부분>
    // 원본 Localization Table의 한국어 / 영어 / 일본어 값을
    // 복제 Step의 새로운 Key로 독립 복사한다.
    //
    // 기존 번역 Key와 값은 절대 변경하지 않는다.
    private void CopyEventSequenceTranslationValues(
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
            // EN / JA 번역이 없다면 임의의 번역문을 생성하지 않는다.
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
                "Duplicate Event Sequence Translation"
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


    // <변경부분>
    // 선택한 Battle Event Step을 제거한다.
    //
    // Localization Table Entry 자체는 즉시 제거하지 않는다.
    // 기존 안정성 정책을 그대로 유지한다.
    private void RemoveEventStep(
        EventSequenceData sequenceData,
        int stepIndex)
    {
        if (sequenceData == null ||
            sequenceData.steps == null ||
            stepIndex < 0 ||
            stepIndex >=
                sequenceData.steps.Count)
        {
            return;
        }

        Undo.RecordObject(
            sequenceData,
            "Remove Event Sequence Step"
        );

        sequenceData.steps.RemoveAt(
            stepIndex
        );

        EditorUtility.SetDirty(
            sequenceData
        );
    }

    private void DrawLocalizationInspector(
        EventSequenceData sequenceData)
    {
        EditorGUILayout.LabelField(
            "Event Dialogue Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            DrawLocalizationId(
                sequenceData
            );

            EditorGUILayout.Space(6);

            DrawLocalizationSyncButton(
                sequenceData
            );

            EditorGUILayout.Space(8);

            DrawDialoguePages(
     sequenceData
 );

            EditorGUILayout.Space(8);

            // <변경부분>
            // Dialogue와 같은 Event_Dialogue Collection을 사용하면서
            // SpeechBubble Text도 이 Inspector에서 직접 관리한다.
            DrawSpeechBubbleTexts(
                sequenceData
            );

            EditorGUILayout.Space(8);

            DrawTranslationStatus(
                sequenceData
            );

            EditorGUILayout.Space(6);

            if (GUILayout.Button(
                    "Localization Tables 열기"))
            {
                DevoryaLocalizationEditorUtility
                    .OpenLocalizationTables();
            }
        }
    }

    // Sequence 전체에서 사용하는 고정 Localization ID.
    //
    // 한 번 Localization을 생성한 뒤에는
    // Asset 이름이나 sequenceName이 바뀌어도
    // 이 ID를 변경하지 않는 것을 원칙으로 한다.
    private void DrawLocalizationId(
        EventSequenceData sequenceData)
    {
        EditorGUILayout.LabelField(
            "Stable Localization ID",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        string newLocalizationId =
            EditorGUILayout.TextField(
                "Localization ID",
                sequenceData.localizationId
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                sequenceData,
                "Edit Event Localization ID"
            );

            sequenceData.localizationId =
                SanitizeLocalizationId(
                    newLocalizationId
                );

            EditorUtility.SetDirty(
                sequenceData
            );
        }

        if (string.IsNullOrWhiteSpace(
                sequenceData.localizationId))
        {
            EditorGUILayout.HelpBox(
                "Localization ID가 아직 없습니다.\n" +
                "아래 버튼으로 Asset 이름을 기준으로 한 번 생성한 뒤 " +
                "이후에는 변경하지 않는 것을 권장합니다.",
                MessageType.Warning
            );

            if (GUILayout.Button(
                    "Asset 이름으로 Localization ID 생성"))
            {
                Undo.RecordObject(
                    sequenceData,
                    "Generate Event Localization ID"
                );

                sequenceData.localizationId =
                    CreateDefaultLocalizationId(
                        sequenceData
                    );

                EditorUtility.SetDirty(
                    sequenceData
                );
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "이 ID는 번역 Key의 고정 identity입니다.\n" +
                "Localization 생성 후에는 sequenceName이나 Asset 이름을 바꿔도 " +
                "이 값은 변경하지 마세요.",
                MessageType.Info
            );
        }
    }

    private void DrawLocalizationSyncButton(
        EventSequenceData sequenceData)
    {
        bool canSync =
    string.IsNullOrWhiteSpace(
        sequenceData.localizationId) == false &&
    (
        HasAnyDialoguePage(
            sequenceData
        ) ||
        HasAnySpeechBubbleStep(
            sequenceData
        )
    );

        using (new EditorGUI.DisabledScope(
                   canSync == false))
        {
            if (GUILayout.Button(
                    "Localization 생성 / 한국어 동기화"))
            {
                SyncLocalization(
                    sequenceData
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
                "한국어 원문만 Event_Dialogue KO Table에 동기화합니다.\n" +
                "기존 English / Japanese 번역은 덮어쓰지 않습니다.",
                MessageType.None
            );
        }
    }

    private void DrawDialoguePages(
    EventSequenceData sequenceData)
    {
        EditorGUILayout.LabelField(
            "Dialogue Pages",
            EditorStyles.boldLabel
        );

        if (sequenceData.steps == null ||
            sequenceData.steps.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Event Step이 없습니다.",
                MessageType.Info
            );

            return;
        }

        // <변경부분>
        // Dialogue 목록 최상단에서도
        // 전체 Dialogue Step을 한 번에 펼치거나 접을 수 있다.
        DrawDialogueStepFoldoutControls(
            sequenceData
        );

        EditorGUILayout.Space(4);

        bool foundDialogueStep =
            false;

        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        for (int stepIndex = 0;
             stepIndex < sequenceData.steps.Count;
             stepIndex++)
        {
            EventSequenceStepData step =
                sequenceData.steps[stepIndex];

            if (step == null ||
                step.stepType !=
                    EventSequenceStepType.Dialogue)
            {
                continue;
            }

            foundDialogueStep =
                true;

            bool expanded =
                GetStepFoldout(
                    stepIndex
                );

            string safeStepName =
    string.IsNullOrWhiteSpace(
        step.stepName)
        ? "Dialogue"
        : step.stepName;

            // <변경부분>
            // 위 Event Steps 목록과 동일한 1-based 번호를 사용한다.
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

                EditorGUILayout.Space(4);

                if (step.dialoguePages == null)
                {
                    EditorGUILayout.HelpBox(
                        "Dialogue Page 목록이 없습니다.",
                        MessageType.Info
                    );
                }
                else
                {
                    for (int pageIndex = 0;
                         pageIndex <
                         step.dialoguePages.Count;
                         pageIndex++)
                    {
                        bool listChanged =
                        DrawDialoguePage(
                        sequenceData,
                        step,
                        pageIndex,
                        collection
                        );

                        if (listChanged)
                        {
                            GUIUtility.ExitGUI();
                            return;
                        }

                        EditorGUILayout.Space(8);
                    }
                }

                if (GUILayout.Button(
                        "+ Dialogue Page 추가"))
                {
                    AddDialoguePage(
                        sequenceData,
                        step
                    );

                    GUIUtility.ExitGUI();
                    return;
                }
            }

            EditorGUILayout.Space(6);
        }

        // <변경부분>
        // Dialogue 목록 끝까지 내려온 상태에서도
        // 위로 다시 올라가지 않고 전체 펼치기 / 접기를 사용할 수 있다.
        if (foundDialogueStep)
        {
            EditorGUILayout.Space(4);

            DrawDialogueStepFoldoutControls(
                sequenceData
            );
        }

        if (foundDialogueStep == false)
        {
            EditorGUILayout.HelpBox(
                "현재 EventSequenceData에 Dialogue Step이 없습니다.",
                MessageType.Info
            );
        }
    }



    // <변경부분>
    // 사용하지 않던 stepIndex 인자를 제거한다.
    private bool DrawDialoguePage(
        EventSequenceData sequenceData,
        EventSequenceStepData step,
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
                    "▲",
                    GUILayout.Width(28)))
            {
                MoveDialoguePage(
                    sequenceData,
                    step,
                    pageIndex,
                    pageIndex - 1
                );

                EditorGUILayout.EndHorizontal();
                return true;
            }
        }

        using (new EditorGUI.DisabledScope(
                   step.dialoguePages == null ||
                   pageIndex >=
                       step.dialoguePages.Count - 1))
        {
            if (GUILayout.Button(
                    "▼",
                    GUILayout.Width(28)))
            {
                MoveDialoguePage(
                    sequenceData,
                    step,
                    pageIndex,
                    pageIndex + 1
                );

                EditorGUILayout.EndHorizontal();
                return true;
            }
        }

        if (GUILayout.Button(
                "삭제",
                GUILayout.Width(45)))
        {
            RemoveDialoguePage(
                sequenceData,
                step,
                pageIndex
            );

            EditorGUILayout.EndHorizontal();
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
                GUILayout.MinHeight(55)
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                sequenceData,
                "Edit Event Dialogue Korean"
            );

            step.dialoguePages[pageIndex] =
                newKorean;

            EditorUtility.SetDirty(
                sequenceData
            );
        }

        EventDialoguePageLocalizationData
            localizationData =
                GetPageLocalizationData(
                    step,
                    pageIndex
                );

        string pageKey =
            GetPageKey(
                sequenceData,
                step,
                localizationData
            );

        if (string.IsNullOrWhiteSpace(
                pageKey))
        {
            EditorGUILayout.HelpBox(
                "Localization 생성 / 한국어 동기화를 실행하면 " +
                "Stable Key와 EN / JA 입력란이 생성됩니다.",
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
                "Event_Dialogue Collection을 찾을 수 없습니다.",
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

    // <변경부분>
    // Battle EventSequence의 SpeechBubble 한국어 원문과
    // EN / JA 번역을 기존 Event_Dialogue Collection에서 관리한다.
    private void DrawSpeechBubbleTexts(
        EventSequenceData sequenceData)
    {
        EditorGUILayout.LabelField(
            "Speech Bubble Texts",
            EditorStyles.boldLabel
        );

        if (sequenceData == null ||
            sequenceData.steps == null ||
            sequenceData.steps.Count == 0)
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
             stepIndex < sequenceData.steps.Count;
             stepIndex++)
        {
            EventSequenceStepData step =
                sequenceData.steps[stepIndex];

            if (step == null ||
                step.stepType !=
                    EventSequenceStepType.SpeechBubble)
            {
                continue;
            }

            foundSpeechBubbleStep =
                true;

            bool expanded =
                GetStepFoldout(
                    stepIndex
                );

            string safeStepName =
                string.IsNullOrWhiteSpace(
                    step.stepName)
                    ? "SpeechBubble"
                    : step.stepName;

            // <변경부분>
            // Event Steps 목록과 동일한 번호 체계를 사용한다.
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
                        sequenceData,
                        "Edit Event SpeechBubble Korean"
                    );

                    step.speechBubbleText =
                        newKorean;

                    EditorUtility.SetDirty(
                        sequenceData
                    );
                }

                string key =
                    GetSpeechBubbleKey(
                        sequenceData,
                        step
                    );

                if (string.IsNullOrWhiteSpace(
                        key))
                {
                    EditorGUILayout.HelpBox(
                        "Localization 생성 / 한국어 동기화를 실행하면 " +
                        "Stable Key와 EN / JA 입력란이 생성됩니다.",
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
                        "Event_Dialogue Collection을 찾을 수 없습니다.",
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
                "현재 EventSequenceData에 SpeechBubble Step이 없습니다.",
                MessageType.Info
            );
        }
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
                GUILayout.MinHeight(55)
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                table,
                $"Edit {displayName} Event Dialogue"
            );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    key,
                    newText
                );

            EditorUtility.SetDirty(
                collection.SharedData
            );
        }
    }

    private void AddDialoguePage(
        EventSequenceData sequenceData,
        EventSequenceStepData step)
    {
        if (step == null)
        {
            return;
        }

        Undo.RecordObject(
            sequenceData,
            "Add Event Dialogue Page"
        );

        if (step.dialoguePages == null)
        {
            step.dialoguePages =
                new List<string>();
        }

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
            sequenceData
        );
    }

    private void RemoveDialoguePage(
        EventSequenceData sequenceData,
        EventSequenceStepData step,
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
            sequenceData,
            "Remove Event Dialogue Page"
        );

        EnsureStepLocalizationMetadata(
            step
        );

        step.dialoguePages.RemoveAt(
            pageIndex
        );

        if (step.dialogueLocalizationPages != null &&
            pageIndex <
                step.dialogueLocalizationPages.Count)
        {
            step.dialogueLocalizationPages.RemoveAt(
                pageIndex
            );
        }

        EditorUtility.SetDirty(
            sequenceData
        );
    }

    private void MoveDialoguePage(
        EventSequenceData sequenceData,
        EventSequenceStepData step,
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
            sequenceData,
            "Move Event Dialogue Page"
        );

        EnsureStepLocalizationMetadata(
            step
        );

        string pageText =
            step.dialoguePages[fromIndex];

        step.dialoguePages.RemoveAt(
            fromIndex
        );

        step.dialoguePages.Insert(
            toIndex,
            pageText
        );

        EventDialoguePageLocalizationData
            localizationData =
                step.dialogueLocalizationPages[
                    fromIndex];

        step.dialogueLocalizationPages
            .RemoveAt(
                fromIndex
            );

        step.dialogueLocalizationPages
            .Insert(
                toIndex,
                localizationData
            );

        EditorUtility.SetDirty(
            sequenceData
        );
    }

    // 기존 한국어 원문을 KO Table에 등록하고
    // 페이지별 LocalizedString 참조를 연결한다.
    //
    // EN / JA Table은 생성만 하고 기존 값은 절대 덮어쓰지 않는다.
    private void SyncLocalization(
        EventSequenceData sequenceData)
    {
        if (sequenceData == null ||
            string.IsNullOrWhiteSpace(
                sequenceData.localizationId))
        {
            return;
        }

        if (IsLocalizationIdDuplicate(
                sequenceData))
        {
            EditorUtility.DisplayDialog(
                "Localization ID 중복",
                "다른 EventSequenceData가 동일한 Localization ID를 사용하고 있습니다.\n" +
                "현재 Asset의 Localization ID를 변경해주세요.",
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
            EditorUtility.DisplayDialog(
                "Localization 생성 실패",
                "Event_Dialogue String Table Collection을 생성할 수 없습니다.",
                "확인"
            );

            return;
        }

        StringTable koreanTable =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    "ko"
                );

        // 번역 입력을 바로 사용할 수 있도록
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
            EditorUtility.DisplayDialog(
                "Localization 생성 실패",
                "Korean (ko) String Table을 생성할 수 없습니다.",
                "확인"
            );

            return;
        }

        Undo.RecordObject(
            sequenceData,
            "Sync Event Dialogue Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Event Dialogue Korean"
        );

        EnsureAllLocalizationMetadata(
            sequenceData
        );

        if (sequenceData.steps != null)
        {
            for (int stepIndex = 0;
                 stepIndex <
                 sequenceData.steps.Count;
                 stepIndex++)
            {
                EventSequenceStepData step =
                    sequenceData.steps[
                        stepIndex];

                if (step == null)
                {
                    continue;
                }

                // <변경부분>
                // 기존 Dialogue Page Localization은 그대로 유지한다.
                if (step.stepType ==
                    EventSequenceStepType.Dialogue)
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
                        EventDialoguePageLocalizationData
                            localizationData =
                                step.dialogueLocalizationPages[
                                    pageIndex];

                        string key =
                            GetPageKey(
                                sequenceData,
                                step,
                                localizationData
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

                        localizationData.localizedText =
                            DevoryaLocalizationEditorUtility
                                .CreateLocalizedStringReference(
                                    collection,
                                    key
                                );
                    }

                    continue;
                }

                // <변경부분>
                // SpeechBubble도 같은 Event_Dialogue Collection에 등록하지만
                // Dialogue Page와는 독립된 Stable Key를 사용한다.
                if (step.stepType ==
                    EventSequenceStepType.SpeechBubble)
                {
                    string key =
                        GetSpeechBubbleKey(
                            sequenceData,
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
            sequenceData
        );

        EditorUtility.SetDirty(
            koreanTable
        );

        EditorUtility.SetDirty(
            collection.SharedData
        );

        AssetDatabase.SaveAssets();

        serializedObject.Update();

        Debug.Log(
            $"Event Dialogue Localization 동기화 완료: " +
            $"{sequenceData.name} / " +
            $"{sequenceData.localizationId}"
        );
    }

    // <변경부분>
    // Dialogue와 SpeechBubble이 Step 순서와 무관한
    // Stable Localization ID를 유지하도록 Metadata를 준비한다.
    private void EnsureAllLocalizationMetadata(
        EventSequenceData sequenceData)
    {
        if (sequenceData == null ||
            sequenceData.steps == null)
        {
            return;
        }

        HashSet<string> usedStepIds =
            new HashSet<string>();

        for (int i = 0;
             i < sequenceData.steps.Count;
             i++)
        {
            EventSequenceStepData step =
                sequenceData.steps[i];

            if (step == null)
            {
                continue;
            }

            if (step.stepType ==
                EventSequenceStepType.Dialogue)
            {
                if (string.IsNullOrWhiteSpace(
                        step.dialogueLocalizationId) ||
                    usedStepIds.Contains(
                        step.dialogueLocalizationId))
                {
                    step.dialogueLocalizationId =
                        CreateStableId(
                            "dialogue"
                        );
                }

                usedStepIds.Add(
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
                    EventDialoguePageLocalizationData
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

                continue;
            }

            if (step.stepType ==
                EventSequenceStepType.SpeechBubble)
            {
                if (string.IsNullOrWhiteSpace(
                        step.speechBubbleLocalizationId) ||
                    usedStepIds.Contains(
                        step.speechBubbleLocalizationId))
                {
                    step.speechBubbleLocalizationId =
                        CreateStableId(
                            "speech"
                        );
                }

                usedStepIds.Add(
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
        EventSequenceStepData step)
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
                    EventDialoguePageLocalizationData
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
                    step.dialogueLocalizationPages[
                        i]
                        .localizationId))
            {
                step.dialogueLocalizationPages[
                    i]
                    .localizationId =
                        CreateStableId(
                            "page"
                        );
            }
        }
    }

    private EventDialoguePageLocalizationData
        CreatePageLocalizationData()
    {
        return
            new EventDialoguePageLocalizationData
            {
                localizationId =
                    CreateStableId(
                        "page"
                    ),

                localizedText =
                    new LocalizedString()
            };
    }

    private EventDialoguePageLocalizationData
        GetPageLocalizationData(
            EventSequenceStepData step,
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
        EventSequenceData sequenceData,
        EventSequenceStepData step,
        EventDialoguePageLocalizationData
            pageData)
    {
        if (sequenceData == null ||
            step == null ||
            pageData == null ||
            string.IsNullOrWhiteSpace(
                sequenceData.localizationId) ||
            string.IsNullOrWhiteSpace(
                step.dialogueLocalizationId) ||
            string.IsNullOrWhiteSpace(
                pageData.localizationId))
        {
            return string.Empty;
        }

        return
            $"event." +
            $"{sequenceData.localizationId}." +
            $"{step.dialogueLocalizationId}." +
            $"{pageData.localizationId}";
    }

    // <변경부분>
    // SpeechBubble은 Step Index가 아니라
    // 자체 Stable Localization ID로 Key를 구성한다.
    private string GetSpeechBubbleKey(
        EventSequenceData sequenceData,
        EventSequenceStepData step)
    {
        if (sequenceData == null ||
            step == null ||
            string.IsNullOrWhiteSpace(
                sequenceData.localizationId) ||
            string.IsNullOrWhiteSpace(
                step.speechBubbleLocalizationId))
        {
            return string.Empty;
        }

        return
            $"event." +
            $"{sequenceData.localizationId}." +
            $"speech." +
            $"{step.speechBubbleLocalizationId}";
    }


    private void DrawTranslationStatus(
        EventSequenceData sequenceData)
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

        if (sequenceData.steps != null)
        {
            for (int stepIndex = 0;
                 stepIndex <
                 sequenceData.steps.Count;
                 stepIndex++)
            {
                EventSequenceStepData step =
                    sequenceData.steps[
                        stepIndex];

                if (step == null ||
                    step.stepType !=
                        EventSequenceStepType.Dialogue ||
                    step.dialoguePages == null)
                {
                    continue;
                }

                for (int pageIndex = 0;
                     pageIndex <
                     step.dialoguePages.Count;
                     pageIndex++)
                {
                    total++;

                    EventDialoguePageLocalizationData
                        pageData =
                            GetPageLocalizationData(
                                step,
                                pageIndex
                            );

                    string key =
                        GetPageKey(
                            sequenceData,
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
            }
        }

        EditorGUILayout.LabelField(
            "Translation Status",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            $"Total Pages: {total}\n" +
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
        EventSequenceData sequenceData)
    {
        if (sequenceData == null ||
            sequenceData.steps == null)
        {
            return false;
        }

        for (int i = 0;
             i < sequenceData.steps.Count;
             i++)
        {
            EventSequenceStepData step =
                sequenceData.steps[i];

            if (step != null &&
                step.stepType ==
                    EventSequenceStepType.Dialogue &&
                step.dialoguePages != null &&
                step.dialoguePages.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분>
    // SpeechBubble Step 하나만 있어도
    // Localization 생성 / KO Sync를 실행할 수 있게 한다.
    private bool HasAnySpeechBubbleStep(
        EventSequenceData sequenceData)
    {
        if (sequenceData == null ||
            sequenceData.steps == null)
        {
            return false;
        }

        for (int i = 0;
             i < sequenceData.steps.Count;
             i++)
        {
            EventSequenceStepData step =
                sequenceData.steps[i];

            if (step != null &&
                step.stepType ==
                    EventSequenceStepType.SpeechBubble)
            {
                return true;
            }
        }

        return false;
    }


    private bool GetStepFoldout(
        int stepIndex)
    {
        bool expanded;

        if (dialogueStepFoldouts.TryGetValue(
                stepIndex,
                out expanded))
        {
            return expanded;
        }

        dialogueStepFoldouts[
            stepIndex] =
                true;

        return true;
    }

    // 다른 EventSequenceData와 ID가 겹치면
    // 같은 Table Key를 공유하게 되므로 동기화를 막는다.
    private bool IsLocalizationIdDuplicate(
        EventSequenceData sequenceData)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:EventSequenceData"
            );

        for (int i = 0;
             i < guids.Length;
             i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );

            EventSequenceData other =
                AssetDatabase.LoadAssetAtPath<
                    EventSequenceData
                >(
                    path
                );

            if (other == null ||
                other == sequenceData)
            {
                continue;
            }

            if (string.Equals(
                    other.localizationId,
                    sequenceData.localizationId,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private string CreateDefaultLocalizationId(
        EventSequenceData sequenceData)
    {
        string source =
            sequenceData != null
                ? sequenceData.name
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
                "event"
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