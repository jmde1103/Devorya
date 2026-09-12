using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

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
    // Event Step 목록과 별도로
    // Dialogue 관리 영역의 펼침 상태를 저장한다.
    private readonly Dictionary<int, bool>
        dialogueStepFoldouts =
            new Dictionary<int, bool>();

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
    // EventSceneData Inspector가 활성화되면
    // Scene View 입력을 감지한다.
    private void OnEnable()
    {
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

        // <변경부분>
        isSelectingMoveActorTile =
            false;

        selectingMoveActorStepIndex =
            -1;
    }

    public override void OnInspectorGUI()
    {
        EventSceneData eventData =
            (EventSceneData)target;

        DrawEventInfo(
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

            bool canSync =
                string.IsNullOrWhiteSpace(
                    eventData.localizationId) == false &&
                HasAnyDialoguePage(
                    eventData
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
                    "Localization ID와 최소 1개의 Dialogue Page가 필요합니다.",
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


    // <변경부분>
    // Event Step 전체 목록을 제작한다.
    private void DrawEventSteps(
        EventSceneData eventData)
    {
        EditorGUILayout.LabelField(
            "Event Steps",
            EditorStyles.boldLabel
        );

        if (eventData.steps == null)
        {
            Undo.RecordObject(
                eventData,
                "Initialize Event Steps"
            );

            eventData.steps =
                new List<EventSceneStepData>();

            EditorUtility.SetDirty(
                eventData
            );
        }

        for (int stepIndex = 0;
             stepIndex < eventData.steps.Count;
             stepIndex++)
        {
            EventSceneStepData step =
                eventData.steps[stepIndex];

            if (step == null)
            {
                step =
                    new EventSceneStepData();

                eventData.steps[stepIndex] =
                    step;

                EditorUtility.SetDirty(
                    eventData
                );
            }

            bool expanded =
                GetStepFoldout(
                    stepIndex
                );

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();

                string stepLabel =
                    string.IsNullOrWhiteSpace(
                        step.stepName)
                        ? $"Step {stepIndex}"
                        : $"Step {stepIndex} - {step.stepName}";

                expanded =
                    EditorGUILayout.Foldout(
                        expanded,
                        stepLabel,
                        true
                    );

                stepFoldouts[stepIndex] =
                    expanded;

                using (new EditorGUI.DisabledScope(
                           stepIndex <= 0))
                {
                    if (GUILayout.Button(
                            "↑",
                            GUILayout.Width(28)))
                    {
                        MoveStep(
                            eventData,
                            stepIndex,
                            stepIndex - 1
                        );

                        EditorGUILayout.EndHorizontal();

                        GUIUtility.ExitGUI();
                        return;
                    }
                }

                using (new EditorGUI.DisabledScope(
                           stepIndex >=
                           eventData.steps.Count - 1))
                {
                    if (GUILayout.Button(
                            "↓",
                            GUILayout.Width(28)))
                    {
                        MoveStep(
                            eventData,
                            stepIndex,
                            stepIndex + 1
                        );

                        EditorGUILayout.EndHorizontal();

                        GUIUtility.ExitGUI();
                        return;
                    }
                }

                if (GUILayout.Button(
                        "삭제",
                        GUILayout.Width(45)))
                {
                    RemoveStep(
                        eventData,
                        stepIndex
                    );

                    EditorGUILayout.EndHorizontal();

                    GUIUtility.ExitGUI();
                    return;
                }

                EditorGUILayout.EndHorizontal();

                if (expanded == false)
                {
                    continue;
                }

                EditorGUI.BeginChangeCheck();

                string newStepName =
                    EditorGUILayout.TextField(
                        "Step Name",
                        step.stepName
                    );

                EventSceneStepType newStepType =
                    (EventSceneStepType)
                    EditorGUILayout.EnumPopup(
                        "Step Type",
                        step.stepType
                    );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        eventData,
                        "Edit Event Scene Step"
                    );

                    step.stepName =
                        newStepName;

                    step.stepType =
                        newStepType;

                    EditorUtility.SetDirty(
                        eventData
                    );
                }

                EditorGUILayout.Space(5);

                DrawStepContents(
    eventData,
    step
);
            }

            EditorGUILayout.Space(5);
        }

        if (GUILayout.Button(
                "+ Event Step 추가"))
        {
            AddStep(
                eventData
            );

            GUIUtility.ExitGUI();
        }
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

            string stepLabel =
                $"Step {stepIndex} - {safeStepName}";

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
            }

            EditorGUILayout.Space(6);
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
    // Step Type별 필요한 설정만 표시한다.
    private void DrawStepContents(
     EventSceneData eventData,
     EventSceneStepData step)
    {
        switch (step.stepType)
        {
            case EventSceneStepType.None:

                EditorGUILayout.HelpBox(
                    "아무 동작도 하지 않는 Step입니다.",
                    MessageType.None
                );

                break;

            case EventSceneStepType.Dialogue:

                // <변경부분>
                // Dialogue 실제 내용은 Step 내부에 펼치지 않는다.
                // 아래 Dialogue Pages 전용 영역에서 통합 관리한다.
                int dialoguePageCount =
                    step.dialoguePages != null
                        ? step.dialoguePages.Count
                        : 0;

                EditorGUILayout.HelpBox(
                    $"Dialogue 내용은 아래 Dialogue Pages 영역에서 관리합니다.\n" +
                    $"현재 Page: {dialoguePageCount}",
                    MessageType.None
                );

                break;


            case EventSceneStepType.SpawnActor:

                DrawSpawnActorStep(
                    eventData,
                    step
                );

                break;


            // <변경부분>
            case EventSceneStepType.MoveActor:

                DrawMoveActorStep(
                    eventData,
                    step
                );

                break;


            case EventSceneStepType.Wait:

                EditorGUI.BeginChangeCheck();

                float newWaitDuration =
                    EditorGUILayout.FloatField(
                        "Wait Duration",
                        step.waitDuration
                    );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        eventData,
                        "Edit Event Wait Duration"
                    );

                    step.waitDuration =
                        Mathf.Max(
                            0f,
                            newWaitDuration
                        );

                    EditorUtility.SetDirty(
                        eventData
                    );
                }

                break;

            case EventSceneStepType.CompleteSequence:

                EditorGUILayout.HelpBox(
                    "현재 Event Scene Sequence를 완료합니다.",
                    MessageType.Info
                );

                break;

            default:

                // <변경부분>
                // Actor / Camera 기능은 enum만 확보된 상태.
                // 실제 필드는 기능 구현 단계에서 하나씩 추가한다.
                EditorGUILayout.HelpBox(
                    $"{step.stepType} 기능은 아직 구현 전입니다.",
                    MessageType.Warning
                );

                break;
        }
    }

    // <변경부분>
    // SpawnActor Step 전용 제작 UI.
    private void DrawSpawnActorStep(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        EditorGUILayout.LabelField(
    "Actor",
    EditorStyles.boldLabel
);

        EditorGUI.BeginChangeCheck();

        // <변경부분>
        // 이후 다른 Event Step에서 동일 Actor를 참조하기 위한 고유 ID.
        string newActorId =
            EditorGUILayout.TextField(
                "Actor ID",
                step.spawnActorId
            );

        PieceData newPieceData =
                    (PieceData)
            EditorGUILayout.ObjectField(
                "Piece Data",
                step.spawnActorPieceData,
                typeof(PieceData),
                false
            );

        PieceTeam newTeam =
            (PieceTeam)
            EditorGUILayout.EnumPopup(
                "Visual Team",
                step.spawnActorTeam
            );

        bool newUseAbsorbedVisual =
    step.spawnActorUseAbsorbedPlayerVisual;

        // <변경부분>
        // 흡수 Player 외형은 Player Visual일 때만 의미가 있으므로
        // Player 선택 시에만 Inspector에 표시한다.
        if (newTeam ==
            PieceTeam.Player)
        {
            newUseAbsorbedVisual =
                EditorGUILayout.Toggle(
                    "Use Absorbed Player Visual",
                    step.spawnActorUseAbsorbedPlayerVisual
                );
        }

        // <변경부분>
        // Visual Prefab의 Pivot 위치를 Event Scene에서만 보정한다.
        Vector2 newVisualOffset =
            EditorGUILayout.Vector2Field(
                "Visual Offset",
                step.spawnActorVisualOffset
            );

        // <변경부분>
        // Event Scene 연출용 좌우 반전.
        bool newFlipX =
    EditorGUILayout.Toggle(
        "Flip X",
        step.spawnActorFlipX
    );

        // <변경부분>
        // Actor가 생성될 때 Fade In되는 시간을 설정한다.
        // <변경부분>
        // Born Clip이 없는 Actor에게만 적용되는 fallback Fade 시간.
        float newFadeInDuration =
            EditorGUILayout.FloatField(
                "Fallback Fade In Duration",
                step.spawnActorFadeInDuration
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Spawn Actor"
            );

            // <변경부분>
            step.spawnActorId =
                newActorId;

            step.spawnActorPieceData =
                newPieceData;

            step.spawnActorTeam =
                newTeam;

            step.spawnActorUseAbsorbedPlayerVisual =
    newUseAbsorbedVisual;

            // <변경부분>
            step.spawnActorVisualOffset =
    newVisualOffset;

            step.spawnActorFlipX =
                newFlipX;

            // <변경부분>
            step.spawnActorFadeInDuration =
                Mathf.Max(
                    0f,
                    newFadeInDuration
                );

            EditorUtility.SetDirty(
                eventData
            );
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Spawn Position",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        Vector2Int newPosition =
            EditorGUILayout.Vector2IntField(
                "Background Tile",
                step.spawnActorPosition
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Spawn Position"
            );

            step.spawnActorPosition =
                newPosition;

            EditorUtility.SetDirty(
                eventData
            );
        }

        EditorGUILayout.Space(3);

        bool isThisStepSelecting =
            isSelectingSpawnActorTile &&
            selectingSpawnActorStepIndex ==
                GetStepIndex(
                    eventData,
                    step
                );

        string buttonLabel =
            isThisStepSelecting
                ? "Scene 타일 선택 취소"
                : "Scene에서 타일 선택";

        if (GUILayout.Button(
                buttonLabel))
        {
            int stepIndex =
                GetStepIndex(
                    eventData,
                    step
                );

            if (isThisStepSelecting)
            {
                isSelectingSpawnActorTile =
                    false;

                selectingSpawnActorStepIndex =
                    -1;
            }
            else
            {
                // <변경부분>
                // MoveActor 목적지 선택 상태가 남아 있으면
                // HandleSceneGUI에서 MoveActor 입력이 우선 처리되므로
                // SpawnActor 선택 시작 시 반드시 해제한다.
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

        if (isThisStepSelecting)
        {
            EditorGUILayout.HelpBox(
                "Scene View에서 원하는 BackgroundTile을 클릭하세요.\n" +
                "Alt 입력은 Scene View 카메라 조작으로 유지됩니다.",
                MessageType.Info
            );
        }

        if (string.IsNullOrWhiteSpace(
        step.spawnActorId))
        {
            EditorGUILayout.HelpBox(
                "SpawnActor를 실행하려면 Actor ID가 필요합니다.",
                MessageType.Warning
            );
        }

        if (step.spawnActorPieceData == null)
        {
            EditorGUILayout.HelpBox(
                "SpawnActor를 실행하려면 Piece Data가 필요합니다.",
                MessageType.Warning
            );
        }
    }

    // <변경부분>
    // MoveActor Step 전용 제작 UI.
    //
    // Actor ID와 목적지 BackgroundTile,
    // 이동 시간 / 포물선 높이를 설정한다.
    private void DrawMoveActorStep(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        EditorGUILayout.LabelField(
            "Actor",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        string newActorId =
            EditorGUILayout.TextField(
                "Actor ID",
                step.moveActorId
            );

        float newDuration =
            EditorGUILayout.FloatField(
                "Move Duration",
                step.moveActorDuration
            );

        float newArcHeight =
            EditorGUILayout.FloatField(
                "Arc Height",
                step.moveActorArcHeight
            );

        // <변경부분>
        // 현재 MoveActor Step에서
        // Actor 방향을 새로 지정할지 선택한다.
        bool newChangeFlipX =
            EditorGUILayout.Toggle(
                "Change Flip X",
                step.moveActorChangeFlipX
            );

        bool newFlipX =
            step.moveActorFlipX;

        // <변경부분>
        // 방향을 변경하도록 설정한 경우에만
        // 실제 Flip X 값을 표시한다.
        if (newChangeFlipX)
        {
            newFlipX =
                EditorGUILayout.Toggle(
                    "Flip X",
                    step.moveActorFlipX
                );
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Move Actor"
            );

            step.moveActorId =
                newActorId;

            step.moveActorDuration =
                Mathf.Max(
                    0f,
                    newDuration
                );

            step.moveActorArcHeight =
                Mathf.Max(
                    0f,
                    newArcHeight
                );

            // <변경부분>
            step.moveActorChangeFlipX =
                newChangeFlipX;

            step.moveActorFlipX =
                newFlipX;

            EditorUtility.SetDirty(
                eventData
            );
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Destination",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        Vector2Int newDestination =
            EditorGUILayout.Vector2IntField(
                "Background Tile",
                step.moveActorDestination
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Move Destination"
            );

            step.moveActorDestination =
                newDestination;

            EditorUtility.SetDirty(
                eventData
            );
        }

        int stepIndex =
            GetStepIndex(
                eventData,
                step
            );

        bool isThisStepSelecting =
            isSelectingMoveActorTile &&
            selectingMoveActorStepIndex ==
                stepIndex;

        string buttonLabel =
            isThisStepSelecting
                ? "Scene 타일 선택 취소"
                : "Scene에서 목적지 선택";

        if (GUILayout.Button(
                buttonLabel))
        {
            if (isThisStepSelecting)
            {
                isSelectingMoveActorTile =
                    false;

                selectingMoveActorStepIndex =
                    -1;
            }
            else
            {
                // <변경부분>
                // SpawnActor 좌표 선택과 동시에 실행되지 않도록 정리한다.
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

        if (isThisStepSelecting)
        {
            EditorGUILayout.HelpBox(
                "Scene View에서 이동할 BackgroundTile을 클릭하세요.",
                MessageType.Info
            );
        }

        EditorGUILayout.HelpBox(
            "목적지를 Actor의 현재 좌표와 동일하게 지정하면 " +
            "다른 타일로 이동하지 않고 제자리에서 위로 뛰었다 내려옵니다.",
            MessageType.None
        );

        if (string.IsNullOrWhiteSpace(
                step.moveActorId))
        {
            EditorGUILayout.HelpBox(
                "MoveActor를 실행하려면 Actor ID가 필요합니다.",
                MessageType.Warning
            );
        }
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


    private void MoveStep(
        EventSceneData eventData,
        int fromIndex,
        int toIndex)
    {
        if (eventData.steps == null ||
            fromIndex < 0 ||
            fromIndex >= eventData.steps.Count ||
            toIndex < 0 ||
            toIndex >= eventData.steps.Count ||
            fromIndex == toIndex)
        {
            return;
        }

        Undo.RecordObject(
            eventData,
            "Move Event Scene Step"
        );

        EventSceneStepData step =
            eventData.steps[fromIndex];

        eventData.steps.RemoveAt(
            fromIndex
        );

        eventData.steps.Insert(
            toIndex,
            step
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
    // 한국어 원문을 EventScene_Dialogue KO Table에 동기화하고
    // Page별 LocalizedString 참조를 연결한다.
    //
    // 기존 EN / JA 값은 덮어쓰지 않는다.
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

        for (int stepIndex = 0;
             stepIndex < eventData.steps.Count;
             stepIndex++)
        {
            EventSceneStepData step =
                eventData.steps[stepIndex];

            if (step == null ||
                step.stepType !=
                    EventSceneStepType.Dialogue ||
                step.dialoguePages == null)
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


    private void EnsureAllLocalizationMetadata(
        EventSceneData eventData)
    {
        if (eventData == null ||
            eventData.steps == null)
        {
            return;
        }

        HashSet<string> usedStepIds =
            new HashSet<string>();

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

                if (step == null ||
                    step.stepType !=
                        EventSceneStepType.Dialogue ||
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


    private bool GetStepFoldout(
        int stepIndex)
    {
        if (stepFoldouts.TryGetValue(
                stepIndex,
                out bool expanded))
        {
            return expanded;
        }

        stepFoldouts[stepIndex] =
            true;

        return true;
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
    // EventSceneData의 SpawnActor 위치를 Scene View에 표시하고,
    // 타일 선택 모드에서는 BackgroundTile 클릭 입력도 처리한다.
    //
    // Spawn Marker는 Handles로만 그리므로
    // Scene GameObject / Runtime / Build에는 아무 영향이 없다.
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

        // <변경부분>
        // 타일 선택 모드 여부와 관계없이
        // 현재 EventSceneData의 모든 SpawnActor 위치를 표시한다.
        if (backgroundManager != null)
        {
            DrawSpawnActorMarkers(
                eventData,
                backgroundManager
            );

            // <변경부분>
            // MoveActor 목적지도 Scene View에 표시한다.
            DrawMoveActorMarkers(
                eventData,
                backgroundManager
            );
        }

        // <변경부분>
        // MoveActor 목적지 선택 모드가 먼저 처리된다.
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

        // <변경부분>
        // Scene View 좌측 상단에 현재 타일 선택 상태를 표시한다.
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

        // <변경부분>
        // BackgroundManager의 공용 좌표 조회를 그대로 사용한다.
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
