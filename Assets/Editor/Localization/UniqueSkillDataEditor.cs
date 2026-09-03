using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// <변경부분> UniqueSkillData의 기존 한국어 작성 방식을 유지하면서
// Unity Localization Table 생성 / 연결 / 번역 편집을
// 같은 Inspector 안에서 처리하기 위한 전용 Editor.
//
// 공통 Table 처리 기능은
// DevoryaLocalizationEditorUtility를 사용한다.
[CustomEditor(typeof(UniqueSkillData))]
public class UniqueSkillDataEditor : Editor
{
    // <변경부분> 고유스킬 전용 String Table Collection.
    private const string TableCollectionName =
        "Unique_Skill";

    // English / Japanese 번역 영역 펼침 상태
    private bool showTranslations = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // <변경부분> 기존 UniqueSkillData 필드는 그대로 표시하되,
        // LocalizedString 참조는 개발자가 직접 수정하지 않도록 숨긴다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizedSkillName",
            "localizedDescription",
            "localizedConditionFailMessage"
        );

        serializedObject.ApplyModifiedProperties();

        UniqueSkillData skillData =
            (UniqueSkillData)target;

        EditorGUILayout.Space(10);

        DrawLocalizationInspector(
            skillData
        );
    }

    // <변경부분> Inspector 하단 Localization 관리 영역
    private void DrawLocalizationInspector(
        UniqueSkillData skillData)
    {
        EditorGUILayout.LabelField(
            "Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            if (skillData.skillType ==
                UniqueSkillType.None)
            {
                EditorGUILayout.HelpBox(
                    "Localization Key를 자동 생성하려면 " +
                    "먼저 Skill Type을 지정해주세요.",
                    MessageType.Warning
                );

                return;
            }

            string baseKey =
                GetSkillBaseKey(
                    skillData
                );

            string nameKey =
                $"{baseKey}.name";

            string descriptionKey =
                $"{baseKey}.description";

            string conditionFailKey =
                $"{baseKey}.condition_fail";

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

            EditorGUILayout.LabelField(
                "Condition Fail",
                conditionFailKey
            );

            EditorGUILayout.Space(6);

            // <변경부분> 현재 Asset의 한국어 원문을 Korean Table에 등록하고
            // LocalizedString 참조를 자동 연결한다.
            if (GUILayout.Button(
                    "Localization 생성 / 한국어 동기화"))
            {
                SyncLocalization(
                    skillData,
                    nameKey,
                    descriptionKey,
                    conditionFailKey
                );
            }

            EditorGUILayout.Space(4);

            bool localizationReady =
                skillData.localizedSkillName != null &&
                skillData.localizedSkillName.IsEmpty == false &&
                skillData.localizedDescription != null &&
                skillData.localizedDescription.IsEmpty == false &&
                skillData.localizedConditionFailMessage != null &&
                skillData.localizedConditionFailMessage.IsEmpty == false;

            if (localizationReady == false)
            {
                EditorGUILayout.HelpBox(
                    "아직 Localization이 연결되지 않았습니다.\n" +
                    "위 버튼을 누르면 Unique_Skill Table의 Key 생성, " +
                    "한국어 원문 등록, LocalizedString 연결을 한 번에 처리합니다.",
                    MessageType.Info
                );

                DrawOpenLocalizationTableButton();

                return;
            }

            EditorGUILayout.HelpBox(
                "Localization 연결 완료\n" +
                "Skill Name / Description / Condition Fail Message를 수정한 뒤 " +
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
                    descriptionKey,
                    conditionFailKey
                );
            }

            EditorGUILayout.Space(6);

            DrawOpenLocalizationTableButton();
        }
    }

    // <변경부분> English / Japanese 번역 편집
    private void DrawTranslations(
        string nameKey,
        string descriptionKey,
        string conditionFailKey)
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
            descriptionKey,
            conditionFailKey
        );

        EditorGUILayout.Space(8);

        DrawLocaleTranslation(
            collection,
            "Japanese",
            "ja",
            nameKey,
            descriptionKey,
            conditionFailKey
        );
    }

    // <변경부분> 특정 Locale의
    // Name / Description / Condition Fail Message를 편집한다.
    private void DrawLocaleTranslation(
        StringTableCollection collection,
        string displayName,
        string localeCode,
        string nameKey,
        string descriptionKey,
        string conditionFailKey)
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

        string currentConditionFail =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    conditionFailKey
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

        EditorGUILayout.Space(3);

        EditorGUILayout.LabelField(
            "Condition Fail Message"
        );

        string newConditionFail =
            EditorGUILayout.TextArea(
                currentConditionFail,
                GUILayout.MinHeight(45)
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                table,
                $"Edit {displayName} Unique Skill Localization"
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

            DevoryaLocalizationEditorUtility
                .SetTableValue(
                    table,
                    conditionFailKey,
                    newConditionFail
                );

            EditorUtility.SetDirty(
                collection.SharedData
            );
        }
    }

    // <변경부분> 기존 한국어 원문을 Korean Table에 동기화하고
    // LocalizedString 참조를 연결한다.
    private void SyncLocalization(
        UniqueSkillData skillData,
        string nameKey,
        string descriptionKey,
        string conditionFailKey)
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
                "Unique_Skill String Table Collection을 생성하거나 찾을 수 없습니다.",
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
            skillData,
            "Sync Unique Skill Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Unique Skill Korean Localization"
        );

        // 기존 한국어 Skill Name 동기화
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                nameKey,
                skillData.skillName
            );

        // 기존 한국어 Description 동기화
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                descriptionKey,
                skillData.description
            );

        // <변경부분> 스킬별 조건 실패 메시지도
        // Unique_Skill Table에서 함께 관리한다.
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                conditionFailKey,
                skillData.conditionFailMessage
            );

        skillData.localizedSkillName =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    nameKey
                );

        skillData.localizedDescription =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    descriptionKey
                );

        skillData.localizedConditionFailMessage =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    conditionFailKey
                );

        if (skillData.localizedSkillName == null ||
            skillData.localizedSkillName.IsEmpty ||
            skillData.localizedDescription == null ||
            skillData.localizedDescription.IsEmpty ||
            skillData.localizedConditionFailMessage == null ||
            skillData.localizedConditionFailMessage.IsEmpty)
        {
            EditorUtility.DisplayDialog(
                "Localization 연결 실패",
                "Localization Key 생성 결과를 확인해주세요.",
                "확인"
            );

            return;
        }

        EditorUtility.SetDirty(
            skillData
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
            $"Unique Skill Localization 동기화 완료: " +
            $"{skillData.name} / " +
            $"{nameKey} / " +
            $"{descriptionKey} / " +
            $"{conditionFailKey}"
        );
    }

    // <변경부분> UniqueSkillType을 안정적인 내부 ID로 사용한다.
    //
    // HornHeadbutt
    // → unique_skill.horn_headbutt
    private string GetSkillBaseKey(
        UniqueSkillData skillData)
    {
        string snakeCaseName =
            DevoryaLocalizationEditorUtility
                .ToSnakeCase(
                    skillData.skillType.ToString()
                );

        return
            $"unique_skill.{snakeCaseName}";
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
