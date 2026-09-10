using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

// <변경부분>
// TextCutsceneData의 대사 제작과 Localization을
// 하나의 Inspector에서 관리하기 위한 Editor.
//
// 한국어 원문:
// TextCutsceneData.textPages
//
// 실제 Localization:
// Cutscene_Dialogue String Table
//
// English / Japanese:
// 이 Inspector에서 직접 편집한다.
//
// 기존 textPages Serialized 데이터는 그대로 유지하고,
// Page별 Stable ID와 LocalizedString Metadata만 함께 관리한다.
[CustomEditor(typeof(TextCutsceneData))]
public class TextCutsceneDataEditor : Editor
{
    private const string TableCollectionName =
        "Cutscene_Dialogue";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // <변경부분>
        // textPages는 아래 Cutscene Dialogue Localization 영역에서
        // 한국어 / 영어 / 일본어와 함께 편집한다.
        //
        // localizationId와 textPageLocalizationPages는
        // Localization 내부 관리용이므로 기본 Inspector에서는 숨긴다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizationId",
            "textPages",
            "textPageLocalizationPages"
        );

        serializedObject.ApplyModifiedProperties();

        TextCutsceneData cutsceneData =
            (TextCutsceneData)target;

        EditorGUILayout.Space(12);

        DrawLocalizationInspector(
            cutsceneData
        );
    }

    // <변경부분>
    // TextCutscene Dialogue Localization 전용 Inspector 영역.
    private void DrawLocalizationInspector(
        TextCutsceneData cutsceneData)
    {
        EditorGUILayout.LabelField(
            "Cutscene Dialogue Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            DrawLocalizationId(
                cutsceneData
            );

            EditorGUILayout.Space(6);

            DrawLocalizationSyncButton(
                cutsceneData
            );

            EditorGUILayout.Space(8);

            DrawTextPages(
                cutsceneData
            );

            EditorGUILayout.Space(8);

            DrawTranslationStatus(
                cutsceneData
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

    // <변경부분>
    // 이 TextCutsceneData 전체에서 사용하는 고정 Localization ID.
    //
    // Localization 생성 이후에는
    // Asset 이름이나 cutsceneName이 변경되어도
    // 이 ID를 그대로 유지하는 것을 원칙으로 한다.
    private void DrawLocalizationId(
        TextCutsceneData cutsceneData)
    {
        EditorGUILayout.LabelField(
            "Stable Localization ID",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        string newLocalizationId =
            EditorGUILayout.TextField(
                "Localization ID",
                cutsceneData.localizationId
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                cutsceneData,
                "Edit Cutscene Localization ID"
            );

            cutsceneData.localizationId =
                SanitizeLocalizationId(
                    newLocalizationId
                );

            EditorUtility.SetDirty(
                cutsceneData
            );
        }

        if (string.IsNullOrWhiteSpace(
                cutsceneData.localizationId))
        {
            EditorGUILayout.HelpBox(
                "Localization ID가 아직 없습니다.\n" +
                "아래 버튼으로 Asset 이름을 기준으로 한 번 생성한 뒤 " +
                "Localization 생성 후에는 변경하지 않는 것을 권장합니다.",
                MessageType.Warning
            );

            if (GUILayout.Button(
                    "Asset 이름으로 Localization ID 생성"))
            {
                Undo.RecordObject(
                    cutsceneData,
                    "Generate Cutscene Localization ID"
                );

                cutsceneData.localizationId =
                    CreateDefaultLocalizationId(
                        cutsceneData
                    );

                EditorUtility.SetDirty(
                    cutsceneData
                );
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "이 ID는 번역 Key의 고정 identity입니다.\n" +
                "Localization 생성 후에는 cutsceneName이나 Asset 이름을 바꾸더라도 " +
                "이 값은 변경하지 마세요.",
                MessageType.Info
            );
        }
    }

    // <변경부분>
    // 한국어 원문을 Cutscene_Dialogue KO Table에 동기화한다.
    //
    // EN / JA Table은 준비만 하고
    // 기존 번역 내용은 절대 덮어쓰지 않는다.
    private void DrawLocalizationSyncButton(
        TextCutsceneData cutsceneData)
    {
        bool canSync =
            string.IsNullOrWhiteSpace(
                cutsceneData.localizationId) == false &&
            cutsceneData.textPages != null &&
            cutsceneData.textPages.Count > 0;

        using (new EditorGUI.DisabledScope(
                   canSync == false))
        {
            if (GUILayout.Button(
                    "Localization 생성 / 한국어 동기화"))
            {
                SyncLocalization(
                    cutsceneData
                );
            }
        }

        if (canSync == false)
        {
            EditorGUILayout.HelpBox(
                "Localization ID와 최소 1개의 Text Page가 필요합니다.",
                MessageType.None
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "한국어 원문만 Cutscene_Dialogue KO Table에 동기화합니다.\n" +
                "기존 English / Japanese 번역은 덮어쓰지 않습니다.",
                MessageType.None
            );
        }
    }

    // <변경부분>
    // 기존 textPages를 한국어 원문으로 사용하면서
    // 같은 위치에서 EN / JA 번역까지 편집한다.
    private void DrawTextPages(
        TextCutsceneData cutsceneData)
    {
        EditorGUILayout.LabelField(
            "Text Pages",
            EditorStyles.boldLabel
        );

        if (cutsceneData.textPages == null)
        {
            Undo.RecordObject(
                cutsceneData,
                "Initialize Cutscene Text Pages"
            );

            cutsceneData.textPages =
                new List<string>();

            EditorUtility.SetDirty(
                cutsceneData
            );
        }

        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        for (int pageIndex = 0;
             pageIndex <
             cutsceneData.textPages.Count;
             pageIndex++)
        {
            bool listChanged =
                DrawTextPage(
                    cutsceneData,
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

        if (GUILayout.Button(
                "+ Text Page 추가"))
        {
            AddTextPage(
                cutsceneData
            );

            GUIUtility.ExitGUI();
        }
    }

    // <변경부분>
    // 한 Page의 한국어 원문 / Stable Key / EN / JA를 표시한다.
    private bool DrawTextPage(
        TextCutsceneData cutsceneData,
        int pageIndex,
        StringTableCollection collection)
    {
        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
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
                        "위",
                        GUILayout.Width(35)))
                {
                    MoveTextPage(
                        cutsceneData,
                        pageIndex,
                        pageIndex - 1
                    );

                    EditorGUILayout.EndHorizontal();
                    return true;
                }
            }

            using (new EditorGUI.DisabledScope(
                       pageIndex >=
                       cutsceneData.textPages.Count - 1))
            {
                if (GUILayout.Button(
                        "아래",
                        GUILayout.Width(45)))
                {
                    MoveTextPage(
                        cutsceneData,
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
                RemoveTextPage(
                    cutsceneData,
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
                cutsceneData.textPages[
                    pageIndex] ??
                string.Empty;

            EditorGUI.BeginChangeCheck();

            string newKorean =
                EditorGUILayout.TextArea(
                    currentKorean,
                    GUILayout.MinHeight(70)
                );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    cutsceneData,
                    "Edit Cutscene Korean Text"
                );

                cutsceneData.textPages[
                    pageIndex] =
                        newKorean;

                EditorUtility.SetDirty(
                    cutsceneData
                );
            }

            TextCutscenePageLocalizationData
                localizationData =
                    GetPageLocalizationData(
                        cutsceneData,
                        pageIndex
                    );

            string pageKey =
                GetPageKey(
                    cutsceneData,
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
                    "Cutscene_Dialogue Collection을 찾을 수 없습니다.",
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
        }

        return false;
    }

    // <변경부분>
    // 지정 Locale의 Table Entry를 Inspector에서 직접 편집한다.
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
                GUILayout.MinHeight(70)
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                table,
                $"Edit {displayName} Cutscene Dialogue"
            );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    key,
                    newText
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
    // 한국어 원문 Page와 Localization Metadata를 동시에 추가한다.
    private void AddTextPage(
        TextCutsceneData cutsceneData)
    {
        if (cutsceneData == null)
        {
            return;
        }

        Undo.RecordObject(
            cutsceneData,
            "Add Cutscene Text Page"
        );

        if (cutsceneData.textPages == null)
        {
            cutsceneData.textPages =
                new List<string>();
        }

        EnsureLocalizationMetadata(
            cutsceneData
        );

        cutsceneData.textPages.Add(
            string.Empty
        );

        cutsceneData
            .textPageLocalizationPages
            .Add(
                CreatePageLocalizationData()
            );

        EditorUtility.SetDirty(
            cutsceneData
        );
    }

    // <변경부분>
    // Page 삭제 시 한국어 원문과 대응 Metadata를 함께 삭제한다.
    //
    // 기존 String Table Entry는 자동 삭제하지 않는다.
    // 데이터 보존을 우선하고 Unused Key 정리는 별도 Tool에서 처리한다.
    private void RemoveTextPage(
        TextCutsceneData cutsceneData,
        int pageIndex)
    {
        if (cutsceneData == null ||
            cutsceneData.textPages == null ||
            pageIndex < 0 ||
            pageIndex >=
                cutsceneData.textPages.Count)
        {
            return;
        }

        Undo.RecordObject(
            cutsceneData,
            "Remove Cutscene Text Page"
        );

        EnsureLocalizationMetadata(
            cutsceneData
        );

        cutsceneData.textPages.RemoveAt(
            pageIndex
        );

        if (cutsceneData
                .textPageLocalizationPages != null &&
            pageIndex <
            cutsceneData
                .textPageLocalizationPages.Count)
        {
            cutsceneData
                .textPageLocalizationPages
                .RemoveAt(
                    pageIndex
                );
        }

        EditorUtility.SetDirty(
            cutsceneData
        );
    }

    // <변경부분>
    // Page 이동 시 한국어 원문과 Stable Localization Metadata를
    // 반드시 같이 이동시킨다.
    //
    // 이 구조 때문에 Page 순서가 변경되어도
    // 기존 EN / JA 번역 Key가 다른 문장으로 밀리지 않는다.
    private void MoveTextPage(
        TextCutsceneData cutsceneData,
        int fromIndex,
        int toIndex)
    {
        if (cutsceneData == null ||
            cutsceneData.textPages == null ||
            fromIndex < 0 ||
            fromIndex >=
                cutsceneData.textPages.Count ||
            toIndex < 0 ||
            toIndex >=
                cutsceneData.textPages.Count ||
            fromIndex == toIndex)
        {
            return;
        }

        Undo.RecordObject(
            cutsceneData,
            "Move Cutscene Text Page"
        );

        EnsureLocalizationMetadata(
            cutsceneData
        );

        string pageText =
            cutsceneData.textPages[
                fromIndex];

        cutsceneData.textPages.RemoveAt(
            fromIndex
        );

        cutsceneData.textPages.Insert(
            toIndex,
            pageText
        );

        TextCutscenePageLocalizationData
            localizationData =
                cutsceneData
                    .textPageLocalizationPages[
                        fromIndex];

        cutsceneData
            .textPageLocalizationPages
            .RemoveAt(
                fromIndex
            );

        cutsceneData
            .textPageLocalizationPages
            .Insert(
                toIndex,
                localizationData
            );

        EditorUtility.SetDirty(
            cutsceneData
        );
    }

    // <변경부분>
    // 기존 textPages 한국어 원문을 KO Table에 등록하고
    // Page별 LocalizedString 참조를 연결한다.
    //
    // EN / JA Table은 생성만 하며 기존 값을 변경하지 않는다.
    private void SyncLocalization(
        TextCutsceneData cutsceneData)
    {
        if (cutsceneData == null ||
            string.IsNullOrWhiteSpace(
                cutsceneData.localizationId))
        {
            return;
        }

        if (IsLocalizationIdDuplicate(
                cutsceneData))
        {
            EditorUtility.DisplayDialog(
                "Localization ID 중복",
                "다른 TextCutsceneData가 동일한 Localization ID를 사용하고 있습니다.\n" +
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
                "Cutscene_Dialogue String Table Collection을 생성할 수 없습니다.",
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

        // EN / JA Table도 함께 준비하지만
        // 기존 번역 값은 수정하지 않는다.
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
            cutsceneData,
            "Sync Cutscene Dialogue Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Cutscene Dialogue Korean"
        );

        EnsureLocalizationMetadata(
            cutsceneData
        );

        for (int pageIndex = 0;
             pageIndex <
             cutsceneData.textPages.Count;
             pageIndex++)
        {
            TextCutscenePageLocalizationData
                localizationData =
                    cutsceneData
                        .textPageLocalizationPages[
                            pageIndex];

            string key =
                GetPageKey(
                    cutsceneData,
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
                    cutsceneData.textPages[
                        pageIndex]
                );

            localizationData.localizedText =
                DevoryaLocalizationEditorUtility
                    .CreateLocalizedStringReference(
                        collection,
                        key
                    );
        }

        EditorUtility.SetDirty(
            cutsceneData
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

        serializedObject.Update();

        Debug.Log(
            $"Cutscene Dialogue Localization 동기화 완료: " +
            $"{cutsceneData.name} / " +
            $"{cutsceneData.localizationId}"
        );
    }

    // <변경부분>
    // 기존 textPages의 Serialized 데이터를 유지하면서
    // Localization Metadata 개수만 정확히 맞춘다.
    //
    // 기존 Metadata가 있으면 유지하고,
    // 부족한 경우에만 새 Stable Page ID를 생성한다.
    private void EnsureLocalizationMetadata(
        TextCutsceneData cutsceneData)
    {
        if (cutsceneData == null)
        {
            return;
        }

        if (cutsceneData.textPages == null)
        {
            cutsceneData.textPages =
                new List<string>();
        }

        if (cutsceneData
                .textPageLocalizationPages == null)
        {
            cutsceneData
                .textPageLocalizationPages =
                    new List<
                        TextCutscenePageLocalizationData
                    >();
        }

        while (
            cutsceneData
                .textPageLocalizationPages.Count <
            cutsceneData.textPages.Count)
        {
            cutsceneData
                .textPageLocalizationPages
                .Add(
                    CreatePageLocalizationData()
                );
        }

        while (
            cutsceneData
                .textPageLocalizationPages.Count >
            cutsceneData.textPages.Count)
        {
            cutsceneData
                .textPageLocalizationPages
                .RemoveAt(
                    cutsceneData
                        .textPageLocalizationPages
                        .Count - 1
                );
        }

        HashSet<string> usedPageIds =
            new HashSet<string>();

        for (int i = 0;
             i <
             cutsceneData
                 .textPageLocalizationPages.Count;
             i++)
        {
            TextCutscenePageLocalizationData
                pageData =
                    cutsceneData
                        .textPageLocalizationPages[i];

            if (pageData == null)
            {
                pageData =
                    CreatePageLocalizationData();

                cutsceneData
                    .textPageLocalizationPages[i] =
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

            if (pageData.localizedText == null)
            {
                pageData.localizedText =
                    new LocalizedString();
            }
        }
    }

    // <변경부분>
    // 신규 Page용 Stable Localization Metadata를 생성한다.
    private TextCutscenePageLocalizationData
        CreatePageLocalizationData()
    {
        return
            new TextCutscenePageLocalizationData
            {
                localizationId =
                    CreateStableId(
                        "page"
                    ),

                localizedText =
                    new LocalizedString()
            };
    }

    private TextCutscenePageLocalizationData
        GetPageLocalizationData(
            TextCutsceneData cutsceneData,
            int pageIndex)
    {
        if (cutsceneData == null ||
            cutsceneData
                .textPageLocalizationPages == null ||
            pageIndex < 0 ||
            pageIndex >=
            cutsceneData
                .textPageLocalizationPages.Count)
        {
            return null;
        }

        return
            cutsceneData
                .textPageLocalizationPages[
                    pageIndex];
    }

    // <변경부분>
    // Page Index가 아니라
    // Cutscene Stable ID + Page Stable ID로 Key를 생성한다.
    //
    // 예:
    // cutscene.prologue_boot.page_a1b2c3d4
    private string GetPageKey(
        TextCutsceneData cutsceneData,
        TextCutscenePageLocalizationData pageData)
    {
        if (cutsceneData == null ||
            pageData == null ||
            string.IsNullOrWhiteSpace(
                cutsceneData.localizationId) ||
            string.IsNullOrWhiteSpace(
                pageData.localizationId))
        {
            return string.Empty;
        }

        return
            $"cutscene." +
            $"{cutsceneData.localizationId}." +
            $"{pageData.localizationId}";
    }

    // <변경부분>
    // 현재 Cutscene의 KR / EN / JA 번역 입력 상태를 표시한다.
    private void DrawTranslationStatus(
        TextCutsceneData cutsceneData)
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
            cutsceneData.textPages != null
                ? cutsceneData.textPages.Count
                : 0;

        int koreanCount =
            0;

        int englishCount =
            0;

        int japaneseCount =
            0;

        for (int pageIndex = 0;
             pageIndex < total;
             pageIndex++)
        {
            TextCutscenePageLocalizationData
                pageData =
                    GetPageLocalizationData(
                        cutsceneData,
                        pageIndex
                    );

            string key =
                GetPageKey(
                    cutsceneData,
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

    // <변경부분>
    // 다른 TextCutsceneData와 Stable ID가 겹치면
    // 같은 Table Key를 공유하게 되므로 동기화를 막는다.
    private bool IsLocalizationIdDuplicate(
        TextCutsceneData cutsceneData)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:TextCutsceneData"
            );

        for (int i = 0;
             i < guids.Length;
             i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );

            TextCutsceneData other =
                AssetDatabase.LoadAssetAtPath<
                    TextCutsceneData
                >(
                    path
                );

            if (other == null ||
                other == cutsceneData)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    other.localizationId))
            {
                continue;
            }

            if (string.Equals(
                    other.localizationId,
                    cutsceneData.localizationId,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분>
    // 최초 Cutscene Stable ID는 Asset 이름에서 생성한다.
    //
    // 생성 이후에는 Asset 이름이 변경되어도
    // 기존 ID를 자동 변경하지 않는다.
    private string CreateDefaultLocalizationId(
        TextCutsceneData cutsceneData)
    {
        string source =
            cutsceneData != null
                ? cutsceneData.name
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
                "cutscene"
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
