using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// <변경부분> StatusEffectData의 기존 한국어 작성 방식을 유지하면서
// Unity Localization Table 생성 / 연결 / 번역 편집을
// 같은 Inspector에서 처리하기 위한 전용 Editor.
//
// 공통 Localization 처리는
// DevoryaLocalizationEditorUtility를 사용한다.
[CustomEditor(typeof(StatusEffectData))]
public class StatusEffectDataEditor : Editor
{
    // <변경부분> 상태효과 전용 String Table Collection 이름
    private const string TableCollectionName =
        "Status_Effect";

    // English / Japanese 번역 영역 펼침 상태
    private bool showTranslations = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // <변경부분> 기존 StatusEffectData 필드는 그대로 표시하되,
        // LocalizedString 참조는 자동 연결하므로 Inspector에서 숨긴다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizedEffectName",
            "localizedDescription"
        );

        serializedObject.ApplyModifiedProperties();

        StatusEffectData statusEffectData =
            (StatusEffectData)target;

        EditorGUILayout.Space(10);

        DrawLocalizationInspector(
            statusEffectData
        );
    }

    // <변경부분> StatusEffectData Inspector 하단의
    // Localization 관리 영역
    private void DrawLocalizationInspector(
        StatusEffectData statusEffectData)
    {
        EditorGUILayout.LabelField(
            "Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            if (statusEffectData.effectType ==
                StatusEffectType.None)
            {
                EditorGUILayout.HelpBox(
                    "Localization Key를 자동 생성하려면 " +
                    "먼저 Effect Type을 지정해주세요.",
                    MessageType.Warning
                );

                return;
            }

            string baseKey =
                GetStatusEffectBaseKey(
                    statusEffectData
                );

            string nameKey =
                $"{baseKey}.name";

            string descriptionKey =
                $"{baseKey}.description";

            EditorGUILayout.LabelField(
                "자동 Localization Key",
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                "Name",
                nameKey
            );

            EditorGUILayout.LabelField(
                "Description",
                descriptionKey
            );

            EditorGUILayout.Space(6);

            // <변경부분> 현재 Asset에 작성된 한국어 원문을
            // Korean Table에 동기화하고
            // LocalizedString 참조를 자동 연결한다.
            if (GUILayout.Button(
                    "Localization 생성 / 한국어 동기화"))
            {
                SyncLocalization(
                    statusEffectData,
                    nameKey,
                    descriptionKey
                );
            }

            EditorGUILayout.Space(4);

            bool localizationReady =
                statusEffectData.localizedEffectName != null &&
                statusEffectData.localizedEffectName.IsEmpty == false &&
                statusEffectData.localizedDescription != null &&
                statusEffectData.localizedDescription.IsEmpty == false;

            if (localizationReady == false)
            {
                EditorGUILayout.HelpBox(
                    "아직 Localization이 연결되지 않았습니다.\n" +
                    "위 버튼을 누르면 Status_Effect Table의 Key 생성, " +
                    "한국어 원문 등록, LocalizedString 연결을 한 번에 처리합니다.",
                    MessageType.Info
                );

                DrawOpenLocalizationTableButton();

                return;
            }

            EditorGUILayout.HelpBox(
                "Localization 연결 완료\n" +
                "Effect Name / Description을 수정한 뒤 " +
                "\"Localization 생성 / 한국어 동기화\"를 다시 누르면 " +
                "한국어 원문만 갱신됩니다. English / Japanese 번역은 유지됩니다.",
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
                    nameKey,
                    descriptionKey
                );
            }

            EditorGUILayout.Space(6);

            DrawOpenLocalizationTableButton();
        }
    }

    // <변경부분> English / Japanese 번역 편집 영역
    private void DrawTranslations(
        string nameKey,
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

        DrawLocaleTranslation(
            collection,
            "English",
            "en",
            nameKey,
            descriptionKey
        );

        EditorGUILayout.Space(8);

        DrawLocaleTranslation(
            collection,
            "Japanese",
            "ja",
            nameKey,
            descriptionKey
        );
    }

    // <변경부분> 특정 Locale의
    // Name / Description을 Inspector에서 직접 편집한다.
    private void DrawLocaleTranslation(
        StringTableCollection collection,
        string displayName,
        string localeCode,
        string nameKey,
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

        string currentName =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    nameKey
                );

        string currentDescription =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    descriptionKey
                );

        EditorGUI.BeginChangeCheck();

        string newName =
            EditorGUILayout.TextField(
                "Name",
                currentName
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
                $"Edit {displayName} Status Effect Localization"
            );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    nameKey,
                    newName
                );

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    descriptionKey,
                    newDescription
                );

            EditorUtility.SetDirty(
                table
            );

            EditorUtility.SetDirty(
                collection.SharedData
            );
        }
    }

    // <변경부분> 기존 StatusEffectData의 한국어 원문을
    // Korean Table에 동기화하고 LocalizedString 참조를 연결한다.
    private void SyncLocalization(
        StatusEffectData statusEffectData,
        string nameKey,
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
                "Status_Effect String Table Collection을 생성하거나 찾을 수 없습니다.",
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
            statusEffectData,
            "Sync Status Effect Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Status Effect Korean Localization"
        );

        // <변경부분> 기존 한국어 상태효과 이름 동기화
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                nameKey,
                statusEffectData.effectName
            );

        // <변경부분> 기존 한국어 상태효과 설명 동기화
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                descriptionKey,
                statusEffectData.description
            );

        statusEffectData.localizedEffectName =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    nameKey
                );

        statusEffectData.localizedDescription =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    descriptionKey
                );

        if (statusEffectData.localizedEffectName == null ||
            statusEffectData.localizedEffectName.IsEmpty ||
            statusEffectData.localizedDescription == null ||
            statusEffectData.localizedDescription.IsEmpty)
        {
            EditorUtility.DisplayDialog(
                "Localization 연결 실패",
                "Localization Key 생성 결과를 확인해주세요.",
                "확인"
            );

            return;
        }

        EditorUtility.SetDirty(
            statusEffectData
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
            $"Status Effect Localization 동기화 완료: " +
            $"{statusEffectData.name} / " +
            $"{nameKey} / " +
            $"{descriptionKey}"
        );
    }

    // <변경부분> StatusEffectType을 안정적인 내부 ID로 사용하여
    // snake_case Localization Key를 만든다.
    //
    // Breakthrough
    // → status_effect.breakthrough
    //
    // Defence
    // → status_effect.defence
    private string GetStatusEffectBaseKey(
        StatusEffectData statusEffectData)
    {
        string snakeCaseName =
            DevoryaLocalizationEditorUtility
                .ToSnakeCase(
                    statusEffectData.effectType.ToString()
                );

        return
            $"status_effect.{snakeCaseName}";
    }

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