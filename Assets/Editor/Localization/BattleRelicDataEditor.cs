using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// BattleRelicData의 기존 한국어 원문 작성 방식을 유지하면서
// Localization Table 생성 / 연결 / 번역 편집을
// 동일 Inspector에서 처리하는 전용 Editor.
[CustomEditor(typeof(BattleRelicData))]
public class BattleRelicDataEditor : Editor
{
    // 유물 전용 String Table Collection.
    private const string TableCollectionName =
        "Battle_Relic";

    // English / Japanese 번역 영역 펼침 상태.
    private bool showTranslations = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 기존 BattleRelicData 설정값은 그대로 표시한다.
        //
        // LocalizedString 참조는 아래 Localization 영역에서
        // 자동 관리하므로 기본 Inspector에서는 숨긴다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizedRelicName",
            "localizedDescription"
        );

        serializedObject.ApplyModifiedProperties();

        BattleRelicData relicData =
            (BattleRelicData)target;

        EditorGUILayout.Space(10);

        DrawLocalizationInspector(
            relicData
        );
    }

    // BattleRelicData Inspector 하단의
    // Localization 관리 영역.
    private void DrawLocalizationInspector(
        BattleRelicData relicData)
    {
        EditorGUILayout.LabelField(
            "Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            if (relicData.relicType ==
                BattleRelicType.None)
            {
                EditorGUILayout.HelpBox(
                    "Localization Key를 자동 생성하려면 먼저 Relic Type을 지정해주세요.",
                    MessageType.Warning
                );

                return;
            }

            string baseKey =
                GetRelicBaseKey(
                    relicData
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

            // 현재 Asset의 한국어 원문을 Korean Table에 동기화하고
            // LocalizedString 참조까지 자동 연결한다.
            if (GUILayout.Button(
                    "Localization 생성 / 한국어 동기화"))
            {
                SyncLocalization(
                    relicData,
                    nameKey,
                    descriptionKey
                );
            }

            EditorGUILayout.Space(4);

            bool localizationReady =
                relicData.localizedRelicName != null &&
                relicData.localizedRelicName.IsEmpty == false &&
                relicData.localizedDescription != null &&
                relicData.localizedDescription.IsEmpty == false;

            if (localizationReady == false)
            {
                EditorGUILayout.HelpBox(
                    "아직 Localization이 연결되지 않았습니다.\n" +
                    "위 버튼을 누르면 Battle_Relic Table의 Key 생성, " +
                    "한국어 원문 등록, LocalizedString 연결을 한 번에 처리합니다.",
                    MessageType.Info
                );

                DrawOpenLocalizationTableButton();

                return;
            }

            EditorGUILayout.HelpBox(
                "Localization 연결 완료\n" +
                "Relic Name과 Description을 수정한 뒤 " +
                "\"Localization 생성 / 한국어 동기화\"를 다시 누르면 " +
                "한국어 원문만 갱신됩니다. English / Japanese 값은 유지됩니다.",
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

    // English / Japanese 번역을
    // 현재 BattleRelicData Inspector에서 직접 편집한다.
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

        EditorGUILayout.Space(4);

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

    // 지정 Locale의 Name / Description 값을
    // Inspector에서 직접 편집한다.
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
                $"Edit {displayName} Battle Relic Localization"
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

    // 현재 BattleRelicData의 한국어 원문을
    // Battle_Relic Korean Table에 동기화하고
    // LocalizedString 참조를 자동 연결한다.
    private void SyncLocalization(
        BattleRelicData relicData,
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
                "Battle_Relic String Table Collection을 생성하거나 찾을 수 없습니다.",
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
            relicData,
            "Sync Battle Relic Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Battle Relic Korean Localization"
        );

        // BattleRelicData에 직접 작성한 한국어 원문을
        // Korean Table의 원본으로 사용한다.
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                nameKey,
                relicData.relicName
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                descriptionKey,
                relicData.description
            );

        relicData.localizedRelicName =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    nameKey
                );

        relicData.localizedDescription =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    descriptionKey
                );

        if (relicData.localizedRelicName == null ||
            relicData.localizedRelicName.IsEmpty ||
            relicData.localizedDescription == null ||
            relicData.localizedDescription.IsEmpty)
        {
            EditorUtility.DisplayDialog(
                "Localization 연결 실패",
                "Localization Key 생성 결과를 확인해주세요.",
                "확인"
            );

            return;
        }

        EditorUtility.SetDirty(
            relicData
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
            $"BattleRelic Localization 동기화 완료: " +
            $"{relicData.name} / " +
            $"{nameKey} / " +
            $"{descriptionKey}"
        );
    }

    // BattleRelicType을 안정적인 내부 ID로 사용해
    // snake_case Localization Key를 생성한다.
    //
    // AbsorbChanceAttackOncePerTurn
    // -> relic.absorb_chance_attack_once_per_turn
    private string GetRelicBaseKey(
        BattleRelicData relicData)
    {
        string snakeCaseName =
            DevoryaLocalizationEditorUtility
                .ToSnakeCase(
                    relicData.relicType.ToString()
                );

        return
            $"relic.{snakeCaseName}";
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
