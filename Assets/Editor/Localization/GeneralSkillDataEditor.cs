using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// <변경부분> GeneralSkillData의 기존 작성 방식을 유지하면서
// Unity Localization Table 생성 / 연결 / 번역 편집을
// 같은 Inspector 안에서 처리하기 위한 전용 Editor.
//
// 공통 Localization Table 처리 기능은
// DevoryaLocalizationEditorUtility를 사용한다.
[CustomEditor(typeof(GeneralSkillData))]
public class GeneralSkillDataEditor : Editor
{
    // <변경부분> 일반스킬 문자열을 저장할 String Table Collection 이름.
    private const string TableCollectionName =
        "General_Skill";

    // <변경부분> Inspector에서 English / Japanese 번역 영역
    // 펼침 상태를 유지한다.
    private bool showTranslations = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // <변경부분> 기존 GeneralSkillData 필드는 모두 그대로 표시하되,
        // LocalizedString 참조 필드는 개발자가 직접 설정하지 않도록 숨긴다.
        //
        // LocalizedString은 아래 Localization 영역의
        // 자동 동기화 버튼을 통해 연결한다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizedSkillName",
            "localizedDescription",
            "localizedTooltipDescriptionFormat"
        );

        serializedObject.ApplyModifiedProperties();

        GeneralSkillData skillData =
            (GeneralSkillData)target;

        EditorGUILayout.Space(10);

        DrawLocalizationInspector(
            skillData
        );
    }

    // <변경부분> GeneralSkillData Inspector 하단에 표시할
    // Devorya 전용 Localization 관리 영역.
    private void DrawLocalizationInspector(
        GeneralSkillData skillData)
    {
        EditorGUILayout.LabelField(
            "Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            if (skillData.skillType ==
                GeneralSkillType.None)
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

            string tooltipKey =
                $"{baseKey}.tooltip";

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
                "Tooltip",
                tooltipKey
            );

            EditorGUILayout.Space(6);

            // <변경부분> 현재 GeneralSkillData에 작성된
            // 한국어 원문을 Korean Table에 동기화하고,
            // 3개의 LocalizedString 참조를 자동으로 연결한다.
            if (GUILayout.Button(
                    "Localization 생성 / 한국어 동기화"))
            {
                SyncLocalization(
                    skillData,
                    nameKey,
                    descriptionKey,
                    tooltipKey
                );
            }

            EditorGUILayout.Space(4);

            bool localizationReady =
                skillData.localizedSkillName != null &&
                skillData.localizedSkillName.IsEmpty == false &&
                skillData.localizedDescription != null &&
                skillData.localizedDescription.IsEmpty == false &&
                skillData.localizedTooltipDescriptionFormat != null &&
                skillData.localizedTooltipDescriptionFormat.IsEmpty == false;

            if (localizationReady == false)
            {
                EditorGUILayout.HelpBox(
                    "아직 Localization이 연결되지 않았습니다.\n" +
                    "위 버튼을 누르면 General_Skill Table의 Key 생성, " +
                    "한국어 원문 등록, LocalizedString 연결을 한 번에 처리합니다.",
                    MessageType.Info
                );

                DrawOpenLocalizationTableButton();

                return;
            }

            EditorGUILayout.HelpBox(
                "Localization 연결 완료\n" +
                "Skill Name / Description / Tooltip Description Format을 수정한 뒤 " +
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
                    tooltipKey
                );
            }

            EditorGUILayout.Space(6);

            DrawOpenLocalizationTableButton();
        }
    }

    // <변경부분> English / Japanese 번역을
    // GeneralSkillData Inspector 안에서 직접 편집한다.
    private void DrawTranslations(
        string nameKey,
        string descriptionKey,
        string tooltipKey)
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

        // <변경부분> Description과 Tooltip의 실제 사용 우선순위를
        // Inspector에서 바로 확인할 수 있도록 안내한다.
        EditorGUILayout.HelpBox(
            "전투 Tooltip 하단 본문은 해당 언어의 Tooltip Description을 우선 사용합니다.\n" +
            "Tooltip Description이 비어 있으면 같은 언어의 Description을 사용합니다.",
            MessageType.Info
        );

        EditorGUILayout.Space(4);

        DrawLocaleTranslation(
            collection,
            "English",
            "en",
            nameKey,
            descriptionKey,
            tooltipKey
        );

        EditorGUILayout.Space(8);

        DrawLocaleTranslation(
            collection,
            "Japanese",
            "ja",
            nameKey,
            descriptionKey,
            tooltipKey
        );
    }

    // <변경부분> 특정 Locale의
    // Name / Description / Tooltip 문자열을 표시하고 수정한다.
    private void DrawLocaleTranslation(
        StringTableCollection collection,
        string displayName,
        string localeCode,
        string nameKey,
        string descriptionKey,
        string tooltipKey)
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

        string currentTooltip =
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    tooltipKey
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

        // <변경부분> 현재 전투 Tooltip에서 실제로 우선 사용되는
        // 문장이라는 점을 Inspector에서 명확하게 표시한다.
        EditorGUILayout.LabelField(
            "Tooltip Description (전투 Tooltip 본문)"
        );

        string newTooltip =
            EditorGUILayout.TextArea(
                currentTooltip,
                GUILayout.MinHeight(55)
            );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                table,
                $"Edit {displayName} General Skill Localization"
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
                    tooltipKey,
                    newTooltip
                );

            EditorUtility.SetDirty(
                collection.SharedData
            );
        }
    }

    // <변경부분> 현재 GeneralSkillData의 한국어 원문을
    // General_Skill Korean Table에 동기화하고
    // LocalizedString 참조를 자동으로 연결한다.
    private void SyncLocalization(
        GeneralSkillData skillData,
        string nameKey,
        string descriptionKey,
        string tooltipKey)
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
                "General_Skill String Table Collection을 생성하거나 찾을 수 없습니다.",
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
            "Sync General Skill Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync General Skill Korean Localization"
        );

        // <변경부분> 기존 GeneralSkillData에 직접 작성한
        // 한국어 이름을 Korean Localization 원문으로 사용한다.
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                nameKey,
                skillData.skillName
            );

        // <변경부분> 기존 한국어 Description을
        // Korean Localization 원문으로 사용한다.
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                descriptionKey,
                skillData.description
            );

        // <변경부분> 기존 Tooltip Description Format도
        // Korean Localization 원문으로 그대로 저장한다.
        //
        // {value} / {percent} 문자열은 변경하지 않고 Table에 저장한다.
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                tooltipKey,
                skillData.tooltipDescriptionFormat
            );

        // <변경부분> 공용 Utility를 통해
        // Table GUID + Entry ID 기반 LocalizedString 참조를 생성한다.
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

        skillData.localizedTooltipDescriptionFormat =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    tooltipKey
                );

        if (skillData.localizedSkillName == null ||
            skillData.localizedSkillName.IsEmpty ||
            skillData.localizedDescription == null ||
            skillData.localizedDescription.IsEmpty ||
            skillData.localizedTooltipDescriptionFormat == null ||
            skillData.localizedTooltipDescriptionFormat.IsEmpty)
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
            $"General Skill Localization 동기화 완료: " +
            $"{skillData.name} / " +
            $"{nameKey} / " +
            $"{descriptionKey} / " +
            $"{tooltipKey}"
        );
    }

    // <변경부분> GeneralSkillType을 안정적인 내부 ID로 사용하여
    // Localization Key의 snake_case 이름을 생성한다.
    //
    // ChanceAttack
    // → general_skill.chance_attack
    private string GetSkillBaseKey(
        GeneralSkillData skillData)
    {
        string snakeCaseName =
            DevoryaLocalizationEditorUtility
                .ToSnakeCase(
                    skillData.skillType.ToString()
                );

        return
            $"general_skill.{snakeCaseName}";
    }

    // <변경부분> Unity 기본 Localization Tables 창을 연다.
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
