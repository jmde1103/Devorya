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

    // <변경부분>
    // 이전 단일 효과 설정을 확인할 수 있는 접기 메뉴.
    private bool showLegacyEffectFields;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // <변경부분>
        // 기존 Basic 필드들은 그대로 표시한다.
        //
        // 개별 효과의 설정은 Item Effects 목록에서만 표시한다.
        // 기존 필드는 삭제하지 않고 접기 메뉴에서 보존한다.
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "localizedItemName",
            "localizedDescription",
            "useItemEffectList",
            "itemEffects",
            "tooltipSections",
            "changeTargetPieceData",
            "changeTargetPieceType",
            "changeTargetUniqueSkill",
            "changeTargetGeneralSkill",
            "useAbsorbedJelluVisual",
            "applyStatusEffectData",
            "applyStatusEffectTooltipSectionColor",
            "onlyPlayerPiece",
            "blockUseOnKing"
        );

        // <변경부분>
        DrawItemEffectsInspector();

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField(
            "Additional Tooltip Sections",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "실제 상태효과는 Item Effects에 추가하세요. " +
            "이곳은 게임 효과와 별개인 보충 Tooltip 섹션입니다.",
            MessageType.Info
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("tooltipSections"),
            true
        );

        serializedObject.ApplyModifiedProperties();

        BattleItemData itemData =
            (BattleItemData)target;

        EditorGUILayout.Space(10);

        // 기존 Localization 편집 기능 유지.
        DrawLocalizationInspector(
            itemData
        );
    }

    // <변경부분>
    // 실제 아이템 효과를 선택식 목록으로 관리한다.
    //
    // 기존 Asset은 목록형으로 전환하기 전까지
    // 예전 단일 효과 방식으로 계속 작동한다.
    private void DrawItemEffectsInspector()
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Item Effects",
            EditorStyles.boldLabel
        );

        // 모든 효과에 공통으로 적용되는 아이템 사용 조건.
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("onlyPlayerPiece"),
            new GUIContent("Only Player Piece")
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("blockUseOnKing"),
            new GUIContent("Block Use On King")
        );

        SerializedProperty list =
            serializedObject.FindProperty("itemEffects");

        SerializedProperty useList =
            serializedObject.FindProperty("useItemEffectList");

        if (!useList.boolValue)
        {
            EditorGUILayout.HelpBox(
                "기존 단일 효과 방식으로 작동 중입니다. " +
                "Add Effect를 누르면 기존 효과를 먼저 목록으로 " +
                "복사한 뒤 새로운 효과를 추가합니다.",
                MessageType.Info
            );

            if (GUILayout.Button("Convert Legacy Effect to List"))
            {
                ConvertLegacyEffectToList();
                GUIUtility.ExitGUI();
            }
        }
        else if (list.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "효과 목록이 비어 있습니다. " +
                "아이템은 사용할 수 없으며 기존 효과가 " +
                "자동으로 다시 실행되지 않습니다.",
                MessageType.Warning
            );
        }

        // 등록된 효과를 목록 순서대로 표시한다.
        for (int i = 0; i < list.arraySize; i++)
        {
            DrawEffectEntry(list, i);
        }

        // <변경부분>
        // 목록에 추가할 효과 종류를 선택하는 메뉴.
        if (GUILayout.Button("+ Add Effect"))
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(
                new GUIContent("Change Piece"),
                false,
                () => AddEffect(BattleItemEffectType.ChangePiece)
            );

            menu.AddItem(
                new GUIContent("Apply Status Effect"),
                false,
                () => AddEffect(BattleItemEffectType.ApplyStatusEffect)
            );

            menu.ShowAsContext();
        }

        // 이전 데이터는 실제 Asset에서 삭제하지 않는다.
        showLegacyEffectFields =
            EditorGUILayout.Foldout(
                showLegacyEffectFields,
                "Legacy Effect Data (Read / Edit)",
                true
            );

        if (showLegacyEffectFields)
        {
            EditorGUILayout.HelpBox(
                "이전 아이템 Asset의 직렬화 필드입니다. " +
                "Item Effects가 활성화되면 게임 효과에는 " +
                "반영되지 않습니다.",
                MessageType.Info
            );

            string[] oldFields =
            {
                "changeTargetPieceData",
                "changeTargetPieceType",
                "changeTargetUniqueSkill",
                "changeTargetGeneralSkill",
                "useAbsorbedJelluVisual",
                "applyStatusEffectData",
                "applyStatusEffectTooltipSectionColor"
            };

            for (int i = 0; i < oldFields.Length; i++)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(oldFields[i])
                );
            }
        }
    }


    // <변경부분>
    // 등록된 효과 한 개의 Inspector를 그린다.
    // 종류에 맞는 설정만 표시한다.
    private void DrawEffectEntry(
        SerializedProperty list,
        int index)
    {
        SerializedProperty entry =
            list.GetArrayElementAtIndex(index);

        SerializedProperty typeProperty =
            entry.FindPropertyRelative("effectType");

        BattleItemEffectType type =
            (BattleItemEffectType)typeProperty.enumValueIndex;

        using (new EditorGUILayout.VerticalScope(
                   EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    $"Effect {index + 1} - {type}",
                    EditorStyles.boldLabel
                );

                // 효과를 위로 이동.
                GUI.enabled = index > 0;

                if (GUILayout.Button("▲", GUILayout.Width(28)))
                {
                    list.MoveArrayElement(index, index - 1);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }

                // 효과를 아래로 이동.
                GUI.enabled = index < list.arraySize - 1;

                if (GUILayout.Button("▼", GUILayout.Width(28)))
                {
                    list.MoveArrayElement(index, index + 1);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = true;

                // 효과 삭제.
                if (GUILayout.Button("X", GUILayout.Width(28)))
                {
                    list.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.PropertyField(
                typeProperty,
                new GUIContent("Effect Type")
            );

            type =
                (BattleItemEffectType)typeProperty.enumValueIndex;

            if (type == BattleItemEffectType.ChangePiece)
            {
                SerializedProperty pieceData =
                    entry.FindPropertyRelative(
                        "changeTargetPieceData"
                    );

                EditorGUILayout.PropertyField(pieceData);

                // PieceData를 사용하지 않는 이전 방식일 때만
                // Piece Type / Ability 보조 설정을 보여준다.
                if (pieceData.objectReferenceValue == null)
                {
                    EditorGUILayout.PropertyField(
                        entry.FindPropertyRelative(
                            "changeTargetPieceType"
                        )
                    );

                    EditorGUILayout.PropertyField(
                        entry.FindPropertyRelative(
                            "changeTargetUniqueSkill"
                        )
                    );
                }

                EditorGUILayout.PropertyField(
                    entry.FindPropertyRelative(
                        "changeTargetGeneralSkill"
                    )
                );

                EditorGUILayout.PropertyField(
                    entry.FindPropertyRelative(
                        "useAbsorbedJelluVisual"
                    )
                );
            }
            else if (type ==
                     BattleItemEffectType.ApplyStatusEffect)
            {
                EditorGUILayout.PropertyField(
                    entry.FindPropertyRelative(
                        "statusEffectData"
                    )
                );

                EditorGUILayout.PropertyField(
                    entry.FindPropertyRelative(
                        "statusEffectTooltipSectionColor"
                    )
                );
            }
        }
    }


    // <변경부분>
    // 기존 단일 효과를 새 목록의 첫 번째 항목으로 복사한다.
    // 이전 필드는 삭제하지 않고 유지한다.
    private void CopyLegacyEffectIfNeeded(
        SerializedProperty list)
    {
        SerializedProperty useList =
            serializedObject.FindProperty("useItemEffectList");

        if (useList.boolValue)
        {
            return;
        }

        BattleItemData item =
            (BattleItemData)target;

        BattleItemEffectType type;

        if (item.itemType ==
            BattleItemType.ChangeSelectedPieceToJelluPawn)
        {
            type = BattleItemEffectType.ChangePiece;
        }
        else if (item.itemType ==
                 BattleItemType.ApplyStatusEffectToSelectedPiece)
        {
            type = BattleItemEffectType.ApplyStatusEffect;
        }
        else
        {
            useList.boolValue = true;
            return;
        }

        int index = list.arraySize;
        list.arraySize++;

        SerializedProperty entry =
            list.GetArrayElementAtIndex(index);

        InitializeNewEffect(entry, type);

        if (type == BattleItemEffectType.ChangePiece)
        {
            entry.FindPropertyRelative(
                "changeTargetPieceData"
            ).objectReferenceValue =
                serializedObject.FindProperty(
                    "changeTargetPieceData"
                ).objectReferenceValue;

            entry.FindPropertyRelative(
                "changeTargetPieceType"
            ).enumValueIndex =
                serializedObject.FindProperty(
                    "changeTargetPieceType"
                ).enumValueIndex;

            entry.FindPropertyRelative(
                "changeTargetUniqueSkill"
            ).enumValueIndex =
                serializedObject.FindProperty(
                    "changeTargetUniqueSkill"
                ).enumValueIndex;

            entry.FindPropertyRelative(
                "changeTargetGeneralSkill"
            ).enumValueIndex =
                serializedObject.FindProperty(
                    "changeTargetGeneralSkill"
                ).enumValueIndex;

            entry.FindPropertyRelative(
                "useAbsorbedJelluVisual"
            ).boolValue =
                serializedObject.FindProperty(
                    "useAbsorbedJelluVisual"
                ).boolValue;
        }
        else
        {
            entry.FindPropertyRelative(
                "statusEffectData"
            ).objectReferenceValue =
                serializedObject.FindProperty(
                    "applyStatusEffectData"
                ).objectReferenceValue;

            entry.FindPropertyRelative(
                "statusEffectTooltipSectionColor"
            ).colorValue =
                serializedObject.FindProperty(
                    "applyStatusEffectTooltipSectionColor"
                ).colorValue;
        }

        useList.boolValue = true;
    }


    // <변경부분>
    // 새 Effect Entry의 기본값을 안전하게 초기화한다.
    private void InitializeNewEffect(
        SerializedProperty entry,
        BattleItemEffectType type)
    {
        entry.FindPropertyRelative(
            "effectType"
        ).enumValueIndex = (int)type;

        entry.FindPropertyRelative(
            "changeTargetPieceData"
        ).objectReferenceValue = null;

        SetEnumByName(
            entry.FindPropertyRelative("changeTargetPieceType"),
            "Pawn"
        );

        SetEnumByName(
            entry.FindPropertyRelative("changeTargetUniqueSkill"),
            "None"
        );

        SetEnumByName(
            entry.FindPropertyRelative("changeTargetGeneralSkill"),
            "None"
        );

        entry.FindPropertyRelative(
            "useAbsorbedJelluVisual"
        ).boolValue = true;

        entry.FindPropertyRelative(
            "statusEffectData"
        ).objectReferenceValue = null;

        entry.FindPropertyRelative(
            "statusEffectTooltipSectionColor"
        ).colorValue = Color.white;
    }


    // <변경부분>
    // Enum 선언값의 순서에 의존하지 않고
    // 이름으로 기본값을 지정한다.
    private void SetEnumByName(
        SerializedProperty property,
        string enumName)
    {
        if (property == null ||
            property.propertyType != SerializedPropertyType.Enum)
        {
            return;
        }

        int index = System.Array.IndexOf(
            property.enumNames,
            enumName
        );

        if (index >= 0)
        {
            property.enumValueIndex = index;
        }
    }


    // <변경부분>
    // 기존 단일 효과만 목록으로 복사하는 버튼.
    private void ConvertLegacyEffectToList()
    {
        serializedObject.Update();

        CopyLegacyEffectIfNeeded(
            serializedObject.FindProperty("itemEffects")
        );

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);
        Repaint();
    }


    // <변경부분>
    // 새로운 효과 추가.
    // 기존 아이템이라면 먼저 기존 단일 효과를 복사한다.
    private void AddEffect(BattleItemEffectType type)
    {
        serializedObject.Update();

        SerializedProperty list =
            serializedObject.FindProperty("itemEffects");

        CopyLegacyEffectIfNeeded(list);

        int index = list.arraySize;
        list.arraySize++;

        InitializeNewEffect(
            list.GetArrayElementAtIndex(index),
            type
        );

        serializedObject.FindProperty(
            "useItemEffectList"
        ).boolValue = true;

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);
        Repaint();
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