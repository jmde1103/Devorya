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
        // Element
        // =====================================================

        eventStepsReorderableList.elementHeight =
            EditorGUIUtility.singleLineHeight +
            6f;

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
                    stepProperty
                        .FindPropertyRelative(
                            "stepName"
                        );

                SerializedProperty stepTypeProperty =
                    stepProperty
                        .FindPropertyRelative(
                            "stepType"
                        );

                string stepName =
                    stepNameProperty != null
                        ? stepNameProperty.stringValue
                        : string.Empty;

                string stepTypeName =
                    stepTypeProperty != null &&
                    stepTypeProperty.enumValueIndex >= 0 &&
                    stepTypeProperty.enumValueIndex <
                        stepTypeProperty.enumDisplayNames.Length
                        ? stepTypeProperty
                            .enumDisplayNames[
                                stepTypeProperty.enumValueIndex
                            ]
                        : "None";

                // <변경부분>
                // 실제 Data에 번호를 저장하지 않고
                // 현재 List 순서를 기준으로 표시용 번호만 생성한다.
                //
                // Drag Reorder 후 자동으로 다시 계산된다.
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
                    $"Step{stepNumber} - " +
                    $"{stepDisplayName}";

                rect.y +=
                    3f;

                rect.height =
                    EditorGUIUtility.singleLineHeight;

                EditorGUI.LabelField(
                    rect,
                    label
                );
            };


        // =====================================================
        // Select
        // =====================================================

        eventStepsReorderableList.onSelectCallback =
            list =>
            {
                Repaint();
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

                // Step Index 기반 Foldout 표시 상태는
                // 순서가 달라졌으므로 초기화한다.
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

                // 기존 Foldout은 Index 기준이므로
                // 잘못된 Step에 붙지 않도록 초기화한다.
                dialogueStepFoldouts.Clear();

                Repaint();
            };
    }

    // <변경부분>
    // Battle Event Steps 목록과
    // 현재 선택한 Step의 전용 설정을 표시한다.
    //
    // 목록:
    // Step01 - 이름
    // Step02 - 이름
    //
    // 상세:
    // 선택한 Step Type과 관계있는 필드만 표시한다.
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

        eventStepsReorderableList
            .DoLayoutList();

        if (eventStepsProperty.arraySize <= 0)
        {
            serializedObject
                .ApplyModifiedProperties();

            EditorGUILayout.HelpBox(
                "Event Step이 없습니다.",
                MessageType.Info
            );

            return;
        }

        int selectedIndex =
            eventStepsReorderableList.index;

        if (selectedIndex < 0 ||
            selectedIndex >=
                eventStepsProperty.arraySize)
        {
            serializedObject
                .ApplyModifiedProperties();

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "위 Steps 목록에서 편집할 Step을 선택하세요.",
                MessageType.None
            );

            return;
        }

        SerializedProperty stepProperty =
            eventStepsProperty
                .GetArrayElementAtIndex(
                    selectedIndex
                );

        if (stepProperty == null)
        {
            serializedObject
                .ApplyModifiedProperties();

            return;
        }

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            DrawSelectedEventStep(
                selectedIndex,
                stepProperty
            );
        }



        serializedObject
            .ApplyModifiedProperties();
    }

    // <변경부분>
    // 선택한 Battle Event Step 하나만 편집한다.
    //
    // EventSequenceStepData 전체 필드를 펼치지 않고
    // 현재 Step Type에 필요한 항목만 표시한다.
    private void DrawSelectedEventStep(
        int selectedIndex,
        SerializedProperty stepProperty)
    {
        SerializedProperty stepNameProperty =
            stepProperty.FindPropertyRelative(
                "stepName"
            );

        SerializedProperty stepTypeProperty =
            stepProperty.FindPropertyRelative(
                "stepType"
            );

        if (stepNameProperty == null ||
            stepTypeProperty == null)
        {
            EditorGUILayout.HelpBox(
                "Event Step의 Basic Property를 찾을 수 없습니다.",
                MessageType.Error
            );

            return;
        }

        string stepNumber =
            (selectedIndex + 1).ToString(
                "D2"
            );

        string stepDisplayName =
            string.IsNullOrWhiteSpace(
                stepNameProperty.stringValue)
                ? stepTypeProperty.enumDisplayNames[
                    stepTypeProperty.enumValueIndex]
                : stepNameProperty.stringValue;

        EditorGUILayout.LabelField(
            $"Step{stepNumber} - {stepDisplayName}",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(
            stepNameProperty,
            new GUIContent(
                "Step Name"
            )
        );

        EditorGUILayout.PropertyField(
            stepTypeProperty,
            new GUIContent(
                "Step Type"
            )
        );

        EditorGUILayout.Space(7);

        EventSequenceStepType stepType =
            (EventSequenceStepType)
            stepTypeProperty.intValue;

        DrawSelectedEventStepContents(
            stepProperty,
            stepType
        );
    }

    // <변경부분>
    // Step Type에 실제로 관계있는 설정만 표시한다.
    //
    // 다른 Step Type용 데이터는 삭제하지 않고
    // 단순히 Inspector에서 숨긴다.
    //
    // 따라서 기존 EventSequenceData Asset의 값은 보존된다.
    private void DrawSelectedEventStepContents(
        SerializedProperty stepProperty,
        EventSequenceStepType stepType)
    {
        switch (stepType)
        {
            case EventSequenceStepType.None:

                EditorGUILayout.HelpBox(
                    "아무 동작도 하지 않는 Step입니다.",
                    MessageType.None
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

                EditorGUILayout.HelpBox(
                    "Dialogue 내용은 아래 Event Dialogue Localization의 " +
                    "Dialogue Pages 영역에서 관리합니다.\n" +
                    $"현재 Page: {dialoguePageCount}",
                    MessageType.Info
                );

                break;


            case EventSequenceStepType.ForcePieceSelect:

                EditorGUILayout.LabelField(
                    "Piece Target",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "targetPieceTeam",
                    "Target Team"
                );

                DrawStepProperty(
                    stepProperty,
                    "targetPiecePosition",
                    "Target Position"
                );

                DrawMarkerSettings(
                    stepProperty
                );

                break;


            case EventSequenceStepType.ForceTileSelect:

                EditorGUILayout.LabelField(
                    "Tile Target",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "targetTilePosition",
                    "Target Position"
                );

                DrawMarkerSettings(
                    stepProperty
                );

                break;


            case EventSequenceStepType.ForceButton:

                EditorGUILayout.LabelField(
                    "Button Target",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "targetButton",
                    "Target Button"
                );

                DrawMarkerSettings(
                    stepProperty
                );

                break;


            case EventSequenceStepType.SpawnPiece:

                EditorGUILayout.LabelField(
                    "Spawn Piece",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "spawnPieceData",
                    "Piece Data"
                );

                DrawStepProperty(
                    stepProperty,
                    "spawnPieceTeam",
                    "Team"
                );

                DrawStepProperty(
                    stepProperty,
                    "spawnPosition",
                    "Spawn Position"
                );

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField(
                    "Spawn Override",
                    EditorStyles.boldLabel
                );

                // <변경부분>
                // Spawn Override 안의 값들은 전부 SpawnPiece와 관계있으므로
                // 해당 구조만 자식까지 표시한다.
                DrawStepProperty(
                    stepProperty,
                    "spawnOverride",
                    "Override Settings",
                    true
                );

                break;


            case EventSequenceStepType.RemovePiece:

                EditorGUILayout.LabelField(
                    "Remove Piece",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "removePiecePosition",
                    "Piece Position"
                );

                SerializedProperty checkTeamProperty =
                    stepProperty.FindPropertyRelative(
                        "checkRemovePieceTeam"
                    );

                if (checkTeamProperty != null)
                {
                    EditorGUILayout.PropertyField(
                        checkTeamProperty,
                        new GUIContent(
                            "Check Team"
                        )
                    );

                    if (checkTeamProperty.boolValue)
                    {
                        DrawStepProperty(
                            stepProperty,
                            "removePieceTeam",
                            "Piece Team"
                        );
                    }
                }

                break;


            case EventSequenceStepType.Wait:

                EditorGUILayout.LabelField(
                    "Wait",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "waitDuration",
                    "Wait Duration"
                );

                break;


            case EventSequenceStepType.CompleteSequence:

                EditorGUILayout.HelpBox(
                    "현재 Event Sequence를 즉시 완료합니다.",
                    MessageType.Info
                );

                break;


            case EventSequenceStepType.CommitPlayerPiecesToRunState:

                EditorGUILayout.HelpBox(
                    "현재 Board의 Player Piece 상태를 RunState에 저장합니다.\n" +
                    "추가 설정값은 없습니다.",
                    MessageType.Info
                );

                break;


            case EventSequenceStepType.AdvanceBattleTurn:

                EditorGUILayout.HelpBox(
                    "BattleManager의 정상 EndTurn 흐름을 사용해 " +
                    "다음 진영으로 Turn을 넘깁니다.\n" +
                    "추가 설정값은 없습니다.",
                    MessageType.Info
                );

                break;


            case EventSequenceStepType.ExecutePieceAction:

                EditorGUILayout.LabelField(
                    "Execute Piece Action",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "actionPieceTeam",
                    "Piece Team"
                );

                DrawStepProperty(
                    stepProperty,
                    "actionPiecePosition",
                    "Piece Position"
                );

                DrawStepProperty(
                    stepProperty,
                    "actionTargetPosition",
                    "Target Position"
                );

                EditorGUILayout.HelpBox(
                    "Target Position이 빈 Tile이면 이동, " +
                    "상대 Piece가 있으면 공격으로 처리됩니다.",
                    MessageType.None
                );

                break;


            case EventSequenceStepType.ExecutePieceUniqueSkill:

                EditorGUILayout.LabelField(
                    "Execute Piece Ability",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "uniqueSkillPieceTeam",
                    "Piece Team"
                );

                DrawStepProperty(
                    stepProperty,
                    "uniqueSkillPiecePosition",
                    "Piece Position"
                );

                EditorGUILayout.HelpBox(
                    "지정한 Piece가 현재 보유한 Ability를 사용합니다.",
                    MessageType.None
                );

                break;


            case EventSequenceStepType.SpeechBubble:

                EditorGUILayout.LabelField(
                    "Speech Bubble",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "speechBubblePieceTeam",
                    "Piece Team"
                );

                DrawStepProperty(
                    stepProperty,
                    "speechBubblePiecePosition",
                    "Piece Position"
                );

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField(
                    "Presentation",
                    EditorStyles.boldLabel
                );

                DrawStepProperty(
                    stepProperty,
                    "speechBubbleDuration",
                    "Duration"
                );

                DrawStepProperty(
                    stepProperty,
                    "speechBubbleTypingSpeed",
                    "Typing Speed"
                );

                SerializedProperty emphasisProperty =
                    stepProperty.FindPropertyRelative(
                        "speechBubbleUseEmphasisShake"
                    );

                if (emphasisProperty != null)
                {
                    EditorGUILayout.PropertyField(
                        emphasisProperty,
                        new GUIContent(
                            "Use Emphasis Shake"
                        )
                    );

                    if (emphasisProperty.boolValue)
                    {
                        DrawStepProperty(
                            stepProperty,
                            "speechBubbleEmphasisStrength",
                            "Emphasis Strength"
                        );
                    }
                }

                DrawStepProperty(
                    stepProperty,
                    "speechBubbleWaitForComplete",
                    "Wait For Complete"
                );

                EditorGUILayout.HelpBox(
                    "SpeechBubble의 Korean / English / Japanese Text는 " +
                    "아래 Speech Bubble Texts 영역에서 관리합니다.",
                    MessageType.Info
                );

                break;
        }
    }

    // <변경부분>
    // 선택한 Step 내부의 특정 Serialized Field만 표시한다.
    //
    // 직접 EventSequenceStepData 필드를 수정하지 않고
    // SerializedProperty를 사용하므로:
    // - Undo
    // - Prefab/Asset Serialization
    // - Unity Inspector 변경 감지
    // 를 그대로 사용할 수 있다.
    private void DrawStepProperty(
        SerializedProperty stepProperty,
        string propertyName,
        string displayName,
        bool includeChildren = false)
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
            EditorGUILayout.HelpBox(
                $"Step Property를 찾을 수 없습니다: {propertyName}",
                MessageType.Warning
            );

            return;
        }

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(
                displayName
            ),
            includeChildren
        );
    }

    // <변경부분>
    // ForcePieceSelect / ForceTileSelect / ForceButton에서만
    // Marker 관련 설정을 표시한다.
    //
    // 다른 Step Type에서는 Marker 설정을 숨긴다.
    private void DrawMarkerSettings(
        SerializedProperty stepProperty)
    {
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Marker",
            EditorStyles.boldLabel
        );

        SerializedProperty showMarkerProperty =
            stepProperty.FindPropertyRelative(
                "showMarker"
            );

        if (showMarkerProperty == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(
            showMarkerProperty,
            new GUIContent(
                "Show Marker"
            )
        );

        if (showMarkerProperty.boolValue == false)
        {
            return;
        }

        DrawStepProperty(
            stepProperty,
            "markerDisplayType",
            "Display Type"
        );

        DrawStepProperty(
            stepProperty,
            "markerPositionOffset",
            "Position Offset"
        );
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

    // EventSequenceData 전체의 Dialogue Step을 모아
    // 하나의 Inspector 영역에서 편집한다.
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