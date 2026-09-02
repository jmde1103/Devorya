using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// BattleItemData의 기존 직관적인 작성 방식을 유지하면서
// Unity Localization Table 생성 / 연결 / 번역 편집을
// 같은 Inspector 안에서 처리하기 위한 전용 Editor.
//
// Localization의 공통 Table 처리 기능은
// DevoryaLocalizationEditorUtility에서 관리한다.
[CustomEditor(typeof(BattleItemData))]
public class BattleItemDataEditor : Editor
{
    // 아이템 문자열을 저장하는 String Table Collection.
    private const string TableCollectionName =
        "Battle_Item";

    // Inspector에서 번역 영역을 접었다 펼 수 있도록 상태를 보관한다.
    private bool showTranslations = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 기존 BattleItemData의 모든 일반 필드는 그대로 표시한다.
        //
        // LocalizedString 참조는 개발자가 직접 연결하지 않고
        // 아래 Localization 영역에서 자동 관리하므로 숨긴다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizedItemName",
            "localizedDescription"
        );

        serializedObject.ApplyModifiedProperties();

        BattleItemData itemData =
            (BattleItemData)target;

        EditorGUILayout.Space(10);

        DrawLocalizationInspector(
            itemData
        );
    }

    // BattleItemData Inspector 아래쪽에 표시할
    // 데보리아 전용 Localization 편집 영역.
    private void DrawLocalizationInspector(
        BattleItemData itemData)
    {
        EditorGUILayout.LabelField(
            "Localization",
            EditorStyles.boldLabel
        );

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            if (itemData.itemType ==
                BattleItemType.None)
            {
                EditorGUILayout.HelpBox(
                    "Localization Key를 자동 생성하려면 " +
                    "먼저 Item Type을 지정해주세요.",
                    MessageType.Warning
                );

                return;
            }

            string baseKey =
                GetItemBaseKey(
                    itemData
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

            // BattleItemData의 현재 한국어 원문을
            // Korean Table에 동기화하고
            // LocalizedString 참조까지 자동 연결한다.
            if (GUILayout.Button(
                    "Localization 생성 / 한국어 동기화"))
            {
                SyncLocalization(
                    itemData,
                    nameKey,
                    descriptionKey
                );
            }

            EditorGUILayout.Space(4);

            bool localizationReady =
                itemData.localizedItemName != null &&
                itemData.localizedItemName.IsEmpty == false &&
                itemData.localizedDescription != null &&
                itemData.localizedDescription.IsEmpty == false;

            if (localizationReady == false)
            {
                EditorGUILayout.HelpBox(
                    "아직 Localization이 연결되지 않았습니다.\n" +
                    "위 버튼을 누르면 Battle_Item Table의 Key 생성, " +
                    "한국어 원문 등록, LocalizedString 연결을 한 번에 처리합니다.",
                    MessageType.Info
                );

                DrawOpenLocalizationTableButton();

                return;
            }

            EditorGUILayout.HelpBox(
                "Localization 연결 완료\n" +
                "Item Name과 Description을 수정한 뒤 " +
                "\"Localization 생성 / 한국어 동기화\"를 다시 누르면 " +
                "한국어 원문만 갱신됩니다. 영어와 일본어 번역은 유지됩니다.",
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
    // BattleItemData Inspector 안에서 직접 편집한다.
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

    // 특정 Locale의 Name / Description을
    // 현재 BattleItemData Inspector에서 직접 수정한다.
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
                $"Edit {displayName} Battle Item Localization"
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
                collection.SharedData
            );
        }
    }

    // 현재 BattleItemData의 한국어 원문을
    // Battle_Item Korean Table에 동기화하고,
    // 해당 Entry에 LocalizedString 참조를 자동 연결한다.
    private void SyncLocalization(
        BattleItemData itemData,
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
                "Battle_Item String Table Collection을 생성하거나 찾을 수 없습니다.",
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
            itemData,
            "Sync Battle Item Localization"
        );

        Undo.RecordObject(
            koreanTable,
            "Sync Battle Item Korean Localization"
        );

        // 기존 BattleItemData에 직접 작성한 한국어 값을
        // Korean Localization Table의 원문으로 사용한다.
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                nameKey,
                itemData.itemName
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                descriptionKey,
                itemData.description
            );

        // 공용 Utility를 통해 Table GUID + Entry ID 기반
        // LocalizedString 참조를 생성한다.
        itemData.localizedItemName =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    nameKey
                );

        itemData.localizedDescription =
            DevoryaLocalizationEditorUtility
                .CreateLocalizedStringReference(
                    collection,
                    descriptionKey
                );

        if (itemData.localizedItemName == null ||
            itemData.localizedItemName.IsEmpty ||
            itemData.localizedDescription == null ||
            itemData.localizedDescription.IsEmpty)
        {
            EditorUtility.DisplayDialog(
                "Localization 연결 실패",
                "Localization Key 생성 결과를 확인해주세요.",
                "확인"
            );

            return;
        }

        EditorUtility.SetDirty(
            itemData
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
            $"BattleItem Localization 동기화 완료: " +
            $"{itemData.name} / " +
            $"{nameKey} / " +
            $"{descriptionKey}"
        );
    }

    // BattleItemType을 Localization Key용 snake_case로 변환한다.
    //
    // ChangeSelectedPieceToJelluPawn
    // → item.change_selected_piece_to_jellu_pawn
    private string GetItemBaseKey(
        BattleItemData itemData)
    {
        string snakeCaseName =
            DevoryaLocalizationEditorUtility
                .ToSnakeCase(
                    itemData.itemType.ToString()
                );

        return
            $"item.{snakeCaseName}";
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