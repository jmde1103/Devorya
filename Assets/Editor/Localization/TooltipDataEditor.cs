using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// <변경부분>
// TooltipData의 기존 한국어 원문 작성 방식을 유지하면서
// Localization Table 생성 / 한국어 동기화 / EN·JA 편집을
// 동일 Inspector에서 처리하기 위한 전용 Editor.
//
// TooltipData에는 enum이나 별도의 고정 ID가 없으므로
// Asset 이름 대신 Unity Asset GUID를 stable Localization identity로 사용한다.
//
// Asset 이름 또는 폴더 위치가 변경되어도 .meta GUID가 유지되는 한
// 기존 Localization Key와 번역 데이터가 유지된다.
[CustomEditor(typeof(TooltipData))]
public class TooltipDataEditor : Editor
{
    // TooltipData 콘텐츠 전용 String Table Collection.
    private const string TableCollectionName =
        "Tooltip_Data";

    // English / Japanese 번역 영역 펼침 상태.
    private bool showTranslations = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 기존 TooltipData authoring 필드는 그대로 표시한다.
        //
        // LocalizedString 참조는 개발자가 직접 연결하지 않고
        // 아래 Localization 영역에서 자동 관리하므로 숨긴다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizedTitle",
            "localizedCategory",
            "localizedMainDescription"
        );

        serializedObject.ApplyModifiedProperties();

        TooltipData tooltipData =
            (TooltipData)target;

        EditorGUILayout.Space(10);

        DrawLocalizationInspector(
            tooltipData
        );
    }

    // TooltipData Inspector 하단의 Localization 관리 영역.
    private void DrawLocalizationInspector(
        TooltipData tooltipData)
    {
        EditorGUILayout.LabelField(
            "Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            string baseKey =
                GetTooltipBaseKey(
                    tooltipData
                );

            if (string.IsNullOrWhiteSpace(
                    baseKey))
            {
                EditorGUILayout.HelpBox(
                    "이 TooltipData Asset의 GUID를 확인할 수 없습니다.\n" +
                    "프로젝트 Asset으로 저장된 TooltipData인지 확인해주세요.",
                    MessageType.Warning
                );

                return;
            }

            string titleKey =
                $"{baseKey}.title";

            string categoryKey =
                $"{baseKey}.category";

            string descriptionKey =
                $"{baseKey}.description";

            EditorGUILayout.LabelField(
                "자동 Localization Key",
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                "Title",
                titleKey
            );

            EditorGUILayout.LabelField(
                "Category",
                categoryKey
            );

            EditorGUILayout.LabelField(
                "Description",
                descriptionKey
            );

            EditorGUILayout.Space(6);

            // 기존 TooltipData의 한국어 raw 값을
            // Korean Table에 동기화하고
            // 세 LocalizedString 참조를 자동 연결한다.
            //
            // 이 작업은 Korean 값만 갱신하며
            // 기존 English / Japanese 번역은 덮어쓰지 않는다.
            if (GUILayout.Button(
                    "Localization 생성 / 한국어 동기화"))
            {
                SyncLocalization(
                    tooltipData,
                    titleKey,
                    categoryKey,
                    descriptionKey
                );
            }

            EditorGUILayout.Space(4);

            bool localizationReady =
                tooltipData.localizedTitle != null &&
                tooltipData.localizedTitle.IsEmpty == false &&
                tooltipData.localizedCategory != null &&
                tooltipData.localizedCategory.IsEmpty == false &&
                tooltipData.localizedMainDescription != null &&
                tooltipData.localizedMainDescription.IsEmpty == false;

            if (localizationReady == false)
            {
                EditorGUILayout.HelpBox(
                    "아직 Localization이 연결되지 않았습니다.\n" +
                    "위 버튼을 누르면 Tooltip_Data Table의 Key 생성, " +
                    "한국어 원문 등록, LocalizedString 연결을 한 번에 처리합니다.",
                    MessageType.Info
                );

                DrawOpenLocalizationTableButton();

                return;
            }

            EditorGUILayout.HelpBox(
                "Localization 연결 완료\n" +
                "Title / Category / Description의 한국어 원문을 수정한 뒤 " +
                "\"Localization 생성 / 한국어 동기화\"를 다시 누르면 " +
                "한국어 값만 갱신됩니다. English / Japanese 번역은 유지됩니다.",
                MessageType.Info
            );

            EditorGUILayout.Space(4);

            showTranslations =
                EditorGUILayout.Foldout(
                    showTranslations,
                    "Translations",
                    true
                );

            if (showTranslations)
            {
                DrawTranslations(
                    titleKey,
                    categoryKey,
                    descriptionKey
                );
            }

            EditorGUILayout.Space(6);

            DrawOpenLocalizationTableButton();
        }
    }

    // 현재 TooltipData Inspector에서
    // English / Japanese 번역을 직접 편집한다.
    private void DrawTranslations(
        string titleKey,
        string categoryKey,
        string descriptionKey)
    {
        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetStringTableCollection(
                    TableCollectionName
                );

        if (collection == null)
        {
            EditorGUILayout.HelpBox(
                $"{TableCollectionName} String Table Collection을 찾을 수 없습니다.",
                MessageType.Warning
            );

            return;
        }

        EditorGUILayout.Space(4);

        DrawLocaleTranslation(
            collection,
            "English",
            "en",
            titleKey,
            categoryKey,
            descriptionKey
        );

        EditorGUILayout.Space(8);

        DrawLocaleTranslation(
            collection,
            "Japanese",
            "ja",
            titleKey,
            categoryKey,
            descriptionKey
        );
    }

    // 특정 Locale의 Title / Category / Description을
    // TooltipData Inspector에서 직접 수정한다.
    private void DrawLocaleTranslation(
        StringTableCollection collection,
        string displayName,
        string localeCode,
        string titleKey,
        string categoryKey,
        string descriptionKey)
    {
        StringTable table =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    localeCode
                );

        EditorGUILayout.LabelField(
            displayName,
            EditorStyles.boldLabel
        );

        if (table == null)
        {
            EditorGUILayout.HelpBox(
                $"{displayName} Locale 또는 String Table을 찾을 수 없습니다.",
                MessageType.Warning
            );

            return;
        }

        string currentTitle =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    titleKey
                );

        string currentCategory =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    categoryKey
                );

        string currentDescription =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    descriptionKey
                );

        EditorGUI.BeginChangeCheck();

        string newTitle =
            EditorGUILayout.TextField(
                "Title",
                currentTitle
            );

        string newCategory =
            EditorGUILayout.TextField(
                "Category",
                currentCategory
            );

        EditorGUILayout.LabelField(
            "Description"
        );

        string newDescription =
            EditorGUILayout.TextArea(
                currentDescription,
                GUILayout.MinHeight(55)
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                table,
                $"Edit {displayName} Tooltip Data Localization"
            );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    titleKey,
                    newTitle
                );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    categoryKey,
                    newCategory
                );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    descriptionKey,
                    newDescription
                );

            EditorUtility.SetDirty(
                collection.SharedData
            );
        }
    }

    // 현재 TooltipData의 한국어 raw 값을 Korean Table에 동기화하고
    // LocalizedString 참조를 자동 연결한다.
    private void SyncLocalization(
        TooltipData tooltipData,
        string titleKey,
        string categoryKey,
        string descriptionKey)
    {
        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTableCollection(
                    TableCollectionName
                );

        if (collection == null)
        {
            EditorUtility.DisplayDialog(
                "Localization 생성 실패",
                "Tooltip_Data String Table Collection을 생성하거나 찾을 수 없습니다.",
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

        if (koreanTable == null)
        {
            EditorUtility.DisplayDialog(
                "Localization 생성 실패",
                "Korean (ko) Locale 또는 String Table을 찾을 수 없습니다.",
                "확인"
            );

            return;
        }

        Undo.RecordObject(
            tooltipData,
            "Sync Tooltip Data Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Tooltip Data Korean Localization"
        );

        // 기존 TooltipData에 직접 작성한 한국어 값을
        // Korean Localization Table의 원문으로 사용한다.
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                titleKey,
                tooltipData.title
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                categoryKey,
                tooltipData.category
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                descriptionKey,
                tooltipData.mainDescription
            );

        // 공용 Utility를 통해
        // Table GUID + Entry ID 기반 LocalizedString 참조를 만든다.
        tooltipData.localizedTitle =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    titleKey
                );

        tooltipData.localizedCategory =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    categoryKey
                );

        tooltipData.localizedMainDescription =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    descriptionKey
                );

        if (tooltipData.localizedTitle == null ||
            tooltipData.localizedTitle.IsEmpty ||
            tooltipData.localizedCategory == null ||
            tooltipData.localizedCategory.IsEmpty ||
            tooltipData.localizedMainDescription == null ||
            tooltipData.localizedMainDescription.IsEmpty)
        {
            EditorUtility.DisplayDialog(
                "Localization 연결 실패",
                "Localization Key 생성 결과를 확인해주세요.",
                "확인"
            );

            return;
        }

        EditorUtility.SetDirty(
            tooltipData
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
            $"TooltipData Localization 동기화 완료: " +
            $"{tooltipData.name} / " +
            $"{titleKey} / " +
            $"{categoryKey} / " +
            $"{descriptionKey}"
        );
    }

    // TooltipData에는 enum/type 기반 고정 ID가 없으므로
    // Unity Asset GUID를 Localization identity로 사용한다.
    //
    // Asset 이름 또는 폴더가 변경되어도 .meta 파일이 유지되면
    // 동일 GUID가 유지되므로 Localization Key도 바뀌지 않는다.
    private string GetTooltipBaseKey(
        TooltipData tooltipData)
    {
        if (tooltipData == null)
        {
            return string.Empty;
        }

        string assetPath =
            AssetDatabase.GetAssetPath(
                tooltipData
            );

        if (string.IsNullOrWhiteSpace(
                assetPath))
        {
            return string.Empty;
        }

        string assetGuid =
            AssetDatabase.AssetPathToGUID(
                assetPath
            );

        if (string.IsNullOrWhiteSpace(
                assetGuid))
        {
            return string.Empty;
        }

        return
            $"tooltip_data.{assetGuid}";
    }

    // 필요할 경우 Unity 기본 Localization Tables 창을 연다.
    private void DrawOpenLocalizationTableButton()
    {
        if (GUILayout.Button(
                "Localization Tables 열기"))
        {
            DevoryaLocalizationEditorUtility
                .OpenLocalizationTables();
        }
    }
}
