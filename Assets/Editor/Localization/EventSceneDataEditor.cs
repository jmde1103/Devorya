using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Spine.Unity;
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


            case EventSceneStepType.MoveActor:

                DrawMoveActorStep(
                    eventData,
                    step
                );

                break;


            // <변경부분>
            // Event Scene 전용 공격 연출.
            case EventSceneStepType.AttackActor:

                DrawAttackActorStep(
                    eventData,
                    step
                );

                break;


            // Event Actor의 Spine Animation...
            case EventSceneStepType.PlayActorAnimation:

                DrawPlayActorAnimationStep(
                    eventData,
                    step
                );

                break;


            // <변경부분>
            // Event Actor 위에 표시할 SpeechBubble을 설정한다.
            case EventSceneStepType.SpeechBubble:

                DrawSpeechBubbleStep(
                    eventData,
                    step
                );

                break;


            // Event Scene에서 지정 Actor를 제거한다.
            case EventSceneStepType.RemoveActor:

                DrawRemoveActorStep(
                    eventData,
                    step
                );

                break;


            // <변경부분>
            // Event Scene 독립 화면 흔들림 연출.
            case EventSceneStepType.ScreenShake:

                DrawScreenShakeStep(
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

    // <변경부분>
    // AttackActor Step 전용 제작 UI.
    //
    // Success:
    // Target 위치까지 이동
    // → Target 즉시 제거
    // → 공격자가 해당 위치를 점유.
    //
    // Failure:
    // Target 앞 충돌 지점까지 이동
    // → Target Shake
    // → Defense 방식의 2단 Bounce
    // → 원래 위치 복귀.
    private void DrawAttackActorStep(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        EditorGUILayout.LabelField(
            "Attack Actor",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        string newActorId =
            EditorGUILayout.TextField(
                "Attacker ID",
                step.attackActorId
            );

        string newTargetActorId =
            EditorGUILayout.TextField(
                "Target ID",
                step.attackTargetActorId
            );

        EventSceneAttackResult newResult =
            (EventSceneAttackResult)
            EditorGUILayout.EnumPopup(
                "Attack Result",
                step.attackResult
            );

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField(
            "Attack Movement",
            EditorStyles.boldLabel
        );

        float newApproachDuration =
            EditorGUILayout.FloatField(
                "Attack Duration",
                step.attackApproachDuration
            );

        float newArcHeight =
            EditorGUILayout.FloatField(
                "Arc Height",
                step.attackArcHeight
            );

        float newApproachRatio =
            step.attackApproachRatio;

        float newShakeDuration =
            step.attackTargetShakeDuration;

        float newShakeIntensity =
            step.attackTargetShakeIntensity;

        float newFallShortDistance =
            step.attackFailureFallShortDistance;

        float newFallBackDuration =
            step.attackFailureFallBackDuration;

        float newFirstBounceDuration =
            step.attackFailureFirstBounceDuration;

        float newFirstBounceHeight =
            step.attackFailureFirstBounceHeight;

        float newSecondBounceDuration =
            step.attackFailureSecondBounceDuration;

        float newSecondBounceHeight =
            step.attackFailureSecondBounceHeight;

        float newFinalReturnDuration =
            step.attackFailureFinalReturnDuration;

        // <변경부분>
        // Failure일 때만 Defense Bounce 관련 값을 표시한다.
        // <변경부분>
        // 공격 성공/실패 모두 실제 충돌 순간
        // 화면 흔들림을 사용한다.
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField(
            "Impact Screen Shake",
            EditorStyles.boldLabel
        );

        newShakeDuration =
            EditorGUILayout.FloatField(
                "Shake Duration",
                step.attackTargetShakeDuration
            );

        newShakeIntensity =
            EditorGUILayout.FloatField(
                "Shake Strength",
                step.attackTargetShakeIntensity
            );


        // <변경부분>
        // Failure일 때만 충돌 위치와 Defense Bounce 설정을 표시한다.
        if (newResult ==
            EventSceneAttackResult.Failure)
        {
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(
                "Failure Impact",
                EditorStyles.boldLabel
            );

            newApproachRatio =
                EditorGUILayout.Slider(
                    "Impact Ratio",
                    step.attackApproachRatio,
                    0f,
                    1f
                );

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(
                "Defense Bounce",
                EditorStyles.boldLabel
            );

            newFallShortDistance =
                EditorGUILayout.FloatField(
                    "Fall Short Distance",
                    step.attackFailureFallShortDistance
                );

            newFallBackDuration =
                EditorGUILayout.FloatField(
                    "Fall Back Duration",
                    step.attackFailureFallBackDuration
                );

            newFirstBounceDuration =
                EditorGUILayout.FloatField(
                    "First Bounce Duration",
                    step.attackFailureFirstBounceDuration
                );

            newFirstBounceHeight =
                EditorGUILayout.FloatField(
                    "First Bounce Height",
                    step.attackFailureFirstBounceHeight
                );

            newSecondBounceDuration =
                EditorGUILayout.FloatField(
                    "Second Bounce Duration",
                    step.attackFailureSecondBounceDuration
                );

            newSecondBounceHeight =
                EditorGUILayout.FloatField(
                    "Second Bounce Height",
                    step.attackFailureSecondBounceHeight
                );

            newFinalReturnDuration =
                EditorGUILayout.FloatField(
                    "Final Return Duration",
                    step.attackFailureFinalReturnDuration
                );
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField(
            "Direction",
            EditorStyles.boldLabel
        );

        bool newChangeFlipX =
            EditorGUILayout.Toggle(
                "Change Flip X",
                step.attackChangeFlipX
            );

        bool newFlipX =
            step.attackFlipX;

        if (newChangeFlipX)
        {
            newFlipX =
                EditorGUILayout.Toggle(
                    "Flip X",
                    step.attackFlipX
                );
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Attack Actor"
            );

            step.attackActorId =
                newActorId;

            step.attackTargetActorId =
                newTargetActorId;

            step.attackResult =
                newResult;

            step.attackApproachDuration =
                Mathf.Max(
                    0f,
                    newApproachDuration
                );

            step.attackArcHeight =
                Mathf.Max(
                    0f,
                    newArcHeight
                );

            step.attackApproachRatio =
                Mathf.Clamp01(
                    newApproachRatio
                );

            step.attackTargetShakeDuration =
                Mathf.Max(
                    0f,
                    newShakeDuration
                );

            step.attackTargetShakeIntensity =
                Mathf.Max(
                    0f,
                    newShakeIntensity
                );

            step.attackFailureFallShortDistance =
                Mathf.Max(
                    0f,
                    newFallShortDistance
                );

            step.attackFailureFallBackDuration =
                Mathf.Max(
                    0f,
                    newFallBackDuration
                );

            step.attackFailureFirstBounceDuration =
                Mathf.Max(
                    0f,
                    newFirstBounceDuration
                );

            step.attackFailureFirstBounceHeight =
                Mathf.Max(
                    0f,
                    newFirstBounceHeight
                );

            step.attackFailureSecondBounceDuration =
                Mathf.Max(
                    0f,
                    newSecondBounceDuration
                );

            step.attackFailureSecondBounceHeight =
                Mathf.Max(
                    0f,
                    newSecondBounceHeight
                );

            step.attackFailureFinalReturnDuration =
                Mathf.Max(
                    0f,
                    newFinalReturnDuration
                );

            step.attackChangeFlipX =
                newChangeFlipX;

            step.attackFlipX =
                newFlipX;

            EditorUtility.SetDirty(
                eventData
            );
        }

        EditorGUILayout.Space(4);

        if (string.IsNullOrWhiteSpace(
                step.attackActorId))
        {
            EditorGUILayout.HelpBox(
                "AttackActor를 실행하려면 Attacker ID가 필요합니다.",
                MessageType.Warning
            );
        }

        if (string.IsNullOrWhiteSpace(
                step.attackTargetActorId))
        {
            EditorGUILayout.HelpBox(
                "AttackActor를 실행하려면 Target ID가 필요합니다.",
                MessageType.Warning
            );
        }

        if (string.IsNullOrWhiteSpace(
                step.attackActorId) == false &&
            step.attackActorId ==
                step.attackTargetActorId)
        {
            EditorGUILayout.HelpBox(
                "Attacker와 Target에 동일한 Actor ID를 사용할 수 없습니다.",
                MessageType.Warning
            );
        }

        if (step.attackResult ==
            EventSceneAttackResult.Success)
        {
            EditorGUILayout.HelpBox(
                "Success: Target 위치까지 공격한 뒤 Target을 즉시 제거하고 공격자가 해당 위치를 점유합니다.",
                MessageType.Info
            );
        }
    }
            // <변경부분>
            // ScreenShake Step 전용 제작 UI.
            //
            // AttackActor에서 이미 사용 중인
            // Event Scene 공용 Camera Shake를
            // 독립 Step으로 실행하기 위한 설정이다.
    private void DrawScreenShakeStep(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        EditorGUILayout.LabelField(
            "Screen Shake",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        float newDuration =
            EditorGUILayout.FloatField(
                "Shake Duration",
                step.screenShakeDuration
            );

        float newStrength =
            EditorGUILayout.FloatField(
                "Shake Strength",
                step.screenShakeStrength
            );

        bool newWaitForComplete =
            EditorGUILayout.Toggle(
                "Wait For Complete",
                step.screenShakeWaitForComplete
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Screen Shake"
            );

            step.screenShakeDuration =
                Mathf.Max(
                    0f,
                    newDuration
                );

            step.screenShakeStrength =
                Mathf.Max(
                    0f,
                    newStrength
                );

            step.screenShakeWaitForComplete =
                newWaitForComplete;

            EditorUtility.SetDirty(
                eventData
            );
        }

        EditorGUILayout.Space(4);

        if (step.screenShakeWaitForComplete)
        {
            EditorGUILayout.HelpBox(
                "화면 흔들림이 끝난 뒤 다음 Event Step으로 진행합니다.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "화면 흔들림을 시작한 뒤 기다리지 않고 다음 Event Step으로 진행합니다.",
                MessageType.Info
            );
        }
    }

    // <변경부분>
    // Standalone Event Scene SpeechBubble Step 전용 Inspector.
    //
    // 현재 1단계에서는 Runtime 동작 검증을 위해
    // Actor / 한국어 원문 / 연출값을 직접 편집한다.
    //
    // EventScene_Dialogue Stable Localization 연결과
    // EN / JA 편집은 다음 단계에서 추가한다.
    private void DrawSpeechBubbleStep(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        EditorGUILayout.LabelField(
            "Speech Bubble",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        string newActorId =
            EditorGUILayout.TextField(
                "Actor ID",
                step.speechBubbleActorId
            );

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField(
            "Korean"
        );

        string newText =
            EditorGUILayout.TextArea(
                step.speechBubbleText ??
                string.Empty,
                GUILayout.MinHeight(55)
            );

        EditorGUILayout.Space(4);

        float newDuration =
            EditorGUILayout.FloatField(
                "Duration",
                step.speechBubbleDuration
            );

        float newTypingSpeed =
            EditorGUILayout.FloatField(
                "Typing Speed",
                step.speechBubbleTypingSpeed
            );

        bool newUseEmphasisShake =
            EditorGUILayout.Toggle(
                "Use Emphasis Shake",
                step.speechBubbleUseEmphasisShake
            );

        float newEmphasisStrength =
            step.speechBubbleEmphasisStrength;

        if (newUseEmphasisShake)
        {
            newEmphasisStrength =
                EditorGUILayout.FloatField(
                    "Emphasis Strength",
                    step.speechBubbleEmphasisStrength
                );
        }

        bool newWaitForComplete =
            EditorGUILayout.Toggle(
                "Wait For Complete",
                step.speechBubbleWaitForComplete
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Scene Speech Bubble"
            );

            step.speechBubbleActorId =
                newActorId;

            step.speechBubbleText =
                newText;

            step.speechBubbleDuration =
                Mathf.Max(
                    0f,
                    newDuration
                );

            step.speechBubbleTypingSpeed =
                Mathf.Max(
                    0f,
                    newTypingSpeed
                );

            step.speechBubbleUseEmphasisShake =
                newUseEmphasisShake;

            step.speechBubbleEmphasisStrength =
                Mathf.Max(
                    0f,
                    newEmphasisStrength
                );

            step.speechBubbleWaitForComplete =
                newWaitForComplete;

            EditorUtility.SetDirty(
                eventData
            );
        }

        EditorGUILayout.Space(4);

        if (string.IsNullOrWhiteSpace(
                step.speechBubbleActorId))
        {
            EditorGUILayout.HelpBox(
                "SpeechBubble을 실행하려면 이전 SpawnActor에서 생성한 Actor ID가 필요합니다.",
                MessageType.Warning
            );
        }

        if (string.IsNullOrWhiteSpace(
                step.speechBubbleText))
        {
            EditorGUILayout.HelpBox(
                "표시할 SpeechBubble 문장을 입력해주세요.",
                MessageType.Warning
            );
        }

        EditorGUILayout.HelpBox(
            "현재 Korean은 Runtime 검증용 fallback입니다. " +
            "다음 단계에서 EventScene_Dialogue Stable Localization과 EN / JA 편집을 연결합니다.",
            MessageType.Info
        );
    }


    // <변경부분>
    // PlayActorAnimation Step 전용 제작 UI.
    //
    // 지정 Actor의 이전 SpawnActor를 추적하여
    // 실제 PieceData의 Spine Visual Prefab에서
    // Animation 목록을 읽어 Dropdown으로 표시한다.
    //
    // Loop / 완료 대기 / Idle 복귀 / Mix 시간도 함께 설정한다.
    private void DrawPlayActorAnimationStep(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        EditorGUILayout.LabelField(
            "Actor Animation",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        string newActorId =
            EditorGUILayout.TextField(
                "Actor ID",
                step.playAnimationActorId
            );

        string newAnimationName =
            step.playAnimationName;

        // <변경부분>
        // 현재 Step보다 앞에 있는 SpawnActor를 추적하여
        // 실제 Spine Prefab Animation 목록을 가져온다.
        string[] animationNames =
            GetSpineAnimationNamesForActor(
                eventData,
                step,
                newActorId,
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
                    step.playAnimationName)
                {
                    currentPopupIndex =
                        i + 1;

                    break;
                }
            }

            int selectedPopupIndex =
                EditorGUILayout.Popup(
                    "Spine Animation",
                    currentPopupIndex,
                    popupOptions.ToArray()
                );

            if (selectedPopupIndex > 0 &&
                selectedPopupIndex <=
                    animationNames.Length)
            {
                newAnimationName =
                    animationNames[
                        selectedPopupIndex - 1];
            }

            if (string.IsNullOrWhiteSpace(
                    sourcePrefabName) == false)
            {
                EditorGUILayout.LabelField(
                    "Source Prefab",
                    sourcePrefabName
                );
            }

            // <변경부분>
            // 기존 문자열 값이 현재 Spine Prefab에 없다면
            // 값을 임의 삭제하지 않고 경고만 표시한다.
            if (string.IsNullOrWhiteSpace(
                    step.playAnimationName) == false &&
                currentPopupIndex == 0)
            {
                EditorGUILayout.HelpBox(
                    $"현재 Animation '{step.playAnimationName}'을 " +
                    "Spine Prefab에서 찾을 수 없습니다. " +
                    "Dropdown에서 다시 선택해주세요.",
                    MessageType.Warning
                );
            }
        }
        else
        {
            // <변경부분>
            // 아직 이전 SpawnActor를 찾을 수 없거나
            // Spine Prefab을 읽지 못하는 경우에는
            // 기존 문자열 입력 방식을 fallback으로 유지한다.
            newAnimationName =
                EditorGUILayout.TextField(
                    "Animation Name",
                    step.playAnimationName
                );

            EditorGUILayout.HelpBox(
                "이 Actor ID와 연결된 이전 SpawnActor의 " +
                "Spine Prefab을 찾지 못해 Animation Dropdown을 표시할 수 없습니다.",
                MessageType.Info
            );
        }

        float newMixDuration =
            EditorGUILayout.FloatField(
                "Mix Duration",
                step.playAnimationMixDuration
            );

        bool newLoop =
            EditorGUILayout.Toggle(
                "Loop",
                step.playAnimationLoop
            );

        bool newWaitForComplete =
            step.playAnimationWaitForComplete;

        bool newReturnToIdle =
            step.playAnimationReturnToIdle;

        if (newLoop == false)
        {
            newWaitForComplete =
                EditorGUILayout.Toggle(
                    "Wait Until Complete",
                    step.playAnimationWaitForComplete
                );

            if (newWaitForComplete)
            {
                // <변경부분>
                // Animation 종료 후 자동으로 Idle로 복귀할지 결정한다.
                //
                // OFF이면 현재 Animation의 마지막 Pose를 유지하여
                // 다음 PlayActorAnimation Step과 바로 연결할 수 있다.
                newReturnToIdle =
                    EditorGUILayout.Toggle(
                        "Auto Return To Idle",
                        step.playAnimationReturnToIdle
                    );
            }
        }
        else
        {
            newWaitForComplete =
                false;

            newReturnToIdle =
                false;
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Actor Animation"
            );

            step.playAnimationActorId =
                newActorId;

            step.playAnimationName =
                newAnimationName;

            step.playAnimationMixDuration =
                Mathf.Max(
                    0f,
                    newMixDuration
                );

            step.playAnimationLoop =
                newLoop;

            step.playAnimationWaitForComplete =
                newWaitForComplete;

            step.playAnimationReturnToIdle =
                newReturnToIdle;

            EditorUtility.SetDirty(
                eventData
            );
        }

        EditorGUILayout.Space(4);

        if (string.IsNullOrWhiteSpace(
                step.playAnimationActorId))
        {
            EditorGUILayout.HelpBox(
                "PlayActorAnimation을 실행하려면 Actor ID가 필요합니다.",
                MessageType.Warning
            );
        }

        if (string.IsNullOrWhiteSpace(
                step.playAnimationName))
        {
            EditorGUILayout.HelpBox(
                "실행할 Spine Animation을 선택해주세요.",
                MessageType.Warning
            );
        }

        if (step.playAnimationLoop)
        {
            EditorGUILayout.HelpBox(
                "Loop Animation은 반복 재생을 시작한 뒤 " +
                "즉시 다음 Event Step으로 진행합니다.",
                MessageType.Info
            );
        }
        else if (step.playAnimationWaitForComplete)
        {
            EditorGUILayout.HelpBox(
                step.playAnimationReturnToIdle
                    ? "Animation 완료까지 기다린 뒤 Mix를 적용하여 Idle로 자동 복귀합니다."
                    : "Animation 완료 후 Idle로 복귀하지 않고 마지막 Pose를 유지합니다. 다음 Animation Step은 이 Pose에서 바로 연결됩니다.",
                MessageType.None
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Animation 재생을 시작한 뒤 완료를 기다리지 않고 " +
                "즉시 다음 Event Step으로 진행합니다.",
                MessageType.None
            );
        }
    }

    // <변경부분>
    // RemoveActor Step 전용 제작 UI.
    //
    // Actor ID와 제거 방식을 설정한다.
    // FadeOut일 때만 Fade Duration을 표시한다.
    private void DrawRemoveActorStep(
        EventSceneData eventData,
        EventSceneStepData step)
    {
        EditorGUILayout.LabelField(
            "Remove Actor",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        string newActorId =
            EditorGUILayout.TextField(
                "Actor ID",
                step.removeActorId
            );

        EventSceneRemoveMode newRemoveMode =
            (EventSceneRemoveMode)
            EditorGUILayout.EnumPopup(
                "Remove Mode",
                step.removeActorMode
            );

        float newFadeOutDuration =
            step.removeActorFadeOutDuration;

        // <변경부분>
        // FadeOut일 때만 Fade 시간을 표시한다.
        if (newRemoveMode ==
            EventSceneRemoveMode.FadeOut)
        {
            newFadeOutDuration =
                EditorGUILayout.FloatField(
                    "Fade Out Duration",
                    step.removeActorFadeOutDuration
                );
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                eventData,
                "Edit Event Remove Actor"
            );

            step.removeActorId =
                newActorId;

            step.removeActorMode =
                newRemoveMode;

            step.removeActorFadeOutDuration =
                Mathf.Max(
                    0f,
                    newFadeOutDuration
                );

            EditorUtility.SetDirty(
                eventData
            );
        }

        EditorGUILayout.Space(4);

        if (string.IsNullOrWhiteSpace(
                step.removeActorId))
        {
            EditorGUILayout.HelpBox(
                "RemoveActor를 실행하려면 Actor ID가 필요합니다.",
                MessageType.Warning
            );
        }

        if (step.removeActorMode ==
            EventSceneRemoveMode.FadeOut)
        {
            EditorGUILayout.HelpBox(
                "Actor가 지정 시간 동안 Fade Out된 뒤 Event Scene에서 제거됩니다.",
                MessageType.None
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Actor를 Fade 없이 즉시 Event Scene에서 제거합니다.",
                MessageType.None
            );
        }

        EditorGUILayout.HelpBox(
            "Death 등의 Animation이 필요하면 RemoveActor 전에 " +
            "PlayActorAnimation Step을 배치해주세요.",
            MessageType.Info
        );
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
