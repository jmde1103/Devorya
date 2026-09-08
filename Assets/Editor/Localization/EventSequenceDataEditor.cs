using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

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

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 기존 EventSequenceData / Step 설정은 그대로 표시한다.
        //
        // localizationId는 아래 전용 Localization 영역에서
        // 별도로 관리한다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizationId"
        );

        serializedObject.ApplyModifiedProperties();

        EventSequenceData sequenceData =
            (EventSequenceData)target;

        EditorGUILayout.Space(12);

        DrawLocalizationInspector(
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
            HasAnyDialoguePage(
                sequenceData
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
                "Localization ID와 최소 1개의 Dialogue Page가 필요합니다.",
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
                                stepIndex,
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

    private bool DrawDialoguePage(
        EventSequenceData sequenceData,
        EventSequenceStepData step,
        int stepIndex,
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

    // 기존 dialoguePages의 Serialized 데이터를 유지하면서
    // 필요한 Stable Metadata만 같은 개수로 맞춘다.
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

            if (step == null ||
                step.stepType !=
                    EventSequenceStepType.Dialogue)
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