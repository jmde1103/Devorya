using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// <변경부분> Tooltip 공용 문자열을 관리하는 Editor Window.
//
// 개별 ScriptableObject에 속하지 않는
// Category / 남은 턴 등의 문자열을
// Tooltip_Common String Table에서 관리한다.
public class TooltipCommonLocalizationEditorWindow :
    EditorWindow
{
    private const string TableCollectionName =
        "Tooltip_Common";

    private const string GeneralSkillCategoryKey =
        "tooltip.category.general_skill";

    private const string UniqueSkillCategoryKey =
        "tooltip.category.unique_skill";

    private const string ItemCategoryKey =
        "tooltip.category.item";

    private const string StatusEffectCategoryKey =
        "tooltip.category.status_effect";

    private const string RelicCategoryKey =
        "tooltip.category.relic";

    private const string RemainingTurnKey =
        "tooltip.status.remaining_turn";

    private string englishGeneralSkill = "";
    private string englishUniqueSkill = "";
    private string englishItem = "";
    private string englishStatusEffect = "";
    private string englishRelic = "";
    private string englishRemainingTurn = "";

    private string japaneseGeneralSkill = "";
    private string japaneseUniqueSkill = "";
    private string japaneseItem = "";
    private string japaneseStatusEffect = "";
    private string japaneseRelic = "";
    private string japaneseRemainingTurn = "";

    [MenuItem(
        "Tools/Devorya/Localization/Tooltip Common")]
    public static void OpenWindow()
    {
        TooltipCommonLocalizationEditorWindow window =
            GetWindow<
                TooltipCommonLocalizationEditorWindow>(
                "Tooltip Common Localization"
            );

        window.minSize =
            new Vector2(
                460f,
                500f
            );

        window.LoadTranslations();
    }

    private void OnEnable()
    {
        LoadTranslations();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Tooltip Common Localization",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(4);

        EditorGUILayout.HelpBox(
            "Tooltip 공용 Category와 동적 UI 문구를 관리합니다.\n" +
            "Korean은 프로젝트 기본 원문으로 자동 동기화되며 " +
            "English / Japanese는 아래에서 직접 편집합니다.",
            MessageType.Info
        );

        EditorGUILayout.Space(8);

        if (GUILayout.Button(
                "Table 생성 / 한국어 기본값 동기화"))
        {
            SyncKoreanDefaults();

            LoadTranslations();
        }

        EditorGUILayout.Space(10);

        DrawEnglishFields();

        EditorGUILayout.Space(12);

        DrawJapaneseFields();

        EditorGUILayout.Space(12);

        if (GUILayout.Button(
                "English / Japanese 저장"))
        {
            SaveTranslations();
        }

        EditorGUILayout.Space(6);

        if (GUILayout.Button(
                "Localization Tables 열기"))
        {
            DevoryaLocalizationEditorUtility
                .OpenLocalizationTables();
        }
    }

    private void DrawEnglishFields()
    {
        EditorGUILayout.LabelField(
            "English",
            EditorStyles.boldLabel
        );

        englishGeneralSkill =
            EditorGUILayout.TextField(
                "General Skill",
                englishGeneralSkill
            );

        englishUniqueSkill =
            EditorGUILayout.TextField(
                "Unique Skill",
                englishUniqueSkill
            );

        englishItem =
            EditorGUILayout.TextField(
                "Item",
                englishItem
            );

        englishStatusEffect =
            EditorGUILayout.TextField(
                "Status Effect",
                englishStatusEffect
            );

        englishRelic =
            EditorGUILayout.TextField(
                "Relic",
                englishRelic
            );

        englishRemainingTurn =
            EditorGUILayout.TextField(
                "Remaining Turn",
                englishRemainingTurn
            );

        EditorGUILayout.HelpBox(
            "남은 턴 문장에는 {turn}을 유지해주세요.\n" +
            "예: Turns Remaining: {turn}",
            MessageType.None
        );
    }

    private void DrawJapaneseFields()
    {
        EditorGUILayout.LabelField(
            "Japanese",
            EditorStyles.boldLabel
        );

        japaneseGeneralSkill =
            EditorGUILayout.TextField(
                "General Skill",
                japaneseGeneralSkill
            );

        japaneseUniqueSkill =
            EditorGUILayout.TextField(
                "Unique Skill",
                japaneseUniqueSkill
            );

        japaneseItem =
            EditorGUILayout.TextField(
                "Item",
                japaneseItem
            );

        japaneseStatusEffect =
            EditorGUILayout.TextField(
                "Status Effect",
                japaneseStatusEffect
            );

        japaneseRelic =
            EditorGUILayout.TextField(
                "Relic",
                japaneseRelic
            );

        japaneseRemainingTurn =
            EditorGUILayout.TextField(
                "Remaining Turn",
                japaneseRemainingTurn
            );

        EditorGUILayout.HelpBox(
            "남은 턴 문장에는 {turn}을 유지해주세요.\n" +
            "예: 残りターン: {turn}",
            MessageType.None
        );
    }

    // <변경부분> 공용 Table을 생성하고
    // 기존 게임에서 사용하던 한국어 문자열을 SSOT 원문으로 등록한다.
    //
    // English / Japanese 값은 변경하지 않는다.
    private void SyncKoreanDefaults()
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
                "Tooltip_Common String Table Collection을 생성할 수 없습니다.",
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

        // EN / JA Table도 미리 생성해
        // 아래 번역 입력을 바로 사용할 수 있게 한다.
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
            koreanTable,
            "Sync Tooltip Common Korean Localization"
        );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                GeneralSkillCategoryKey,
                "일반스킬"
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                UniqueSkillCategoryKey,
                "고유스킬"
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                ItemCategoryKey,
                "아이템"
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                StatusEffectCategoryKey,
                "상태효과"
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                RelicCategoryKey,
                "유물"
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                koreanTable,
                RemainingTurnKey,
                "남은 턴: {turn}턴"
            );

        EditorUtility.SetDirty(
            koreanTable
        );

        EditorUtility.SetDirty(
            collection.SharedData
        );

        AssetDatabase.SaveAssets();

        Debug.Log(
            "Tooltip Common 한국어 동기화 완료"
        );
    }

    private void LoadTranslations()
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

        StringTable englishTable =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    "en"
                );

        StringTable japaneseTable =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    "ja"
                );

        if (englishTable != null)
        {
            englishGeneralSkill =
                GetValue(
                    englishTable,
                    GeneralSkillCategoryKey
                );

            englishUniqueSkill =
                GetValue(
                    englishTable,
                    UniqueSkillCategoryKey
                );

            englishItem =
                GetValue(
                    englishTable,
                    ItemCategoryKey
                );

            englishStatusEffect =
                GetValue(
                    englishTable,
                    StatusEffectCategoryKey
                );

            englishRelic =
                GetValue(
                    englishTable,
                    RelicCategoryKey
                );

            englishRemainingTurn =
                GetValue(
                    englishTable,
                    RemainingTurnKey
                );
        }

        if (japaneseTable != null)
        {
            japaneseGeneralSkill =
                GetValue(
                    japaneseTable,
                    GeneralSkillCategoryKey
                );

            japaneseUniqueSkill =
                GetValue(
                    japaneseTable,
                    UniqueSkillCategoryKey
                );

            japaneseItem =
                GetValue(
                    japaneseTable,
                    ItemCategoryKey
                );

            japaneseStatusEffect =
                GetValue(
                    japaneseTable,
                    StatusEffectCategoryKey
                );

            japaneseRelic =
                GetValue(
                    japaneseTable,
                    RelicCategoryKey
                );

            japaneseRemainingTurn =
                GetValue(
                    japaneseTable,
                    RemainingTurnKey
                );
        }
    }

    private string GetValue(
        StringTable table,
        string key)
    {
        return
            DevoryaLocalizationEditorUtility
                .GetTableValue(
                    table,
                    key
                );
    }

    private void SaveTranslations()
    {
        StringTableCollection collection =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTableCollection(
                    TableCollectionName
                );

        if (collection == null)
        {
            return;
        }

        StringTable englishTable =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    "en"
                );

        StringTable japaneseTable =
            DevoryaLocalizationEditorUtility
                .GetOrCreateStringTable(
                    collection,
                    "ja"
                );

        if (englishTable != null)
        {
            Undo.RecordObject(
                englishTable,
                "Edit English Tooltip Common Localization"
            );

            SaveLocaleValues(
                englishTable,
                englishGeneralSkill,
                englishUniqueSkill,
                englishItem,
                englishStatusEffect,
                englishRelic,
                englishRemainingTurn
            );

            EditorUtility.SetDirty(
                englishTable
            );
        }

        if (japaneseTable != null)
        {
            Undo.RecordObject(
                japaneseTable,
                "Edit Japanese Tooltip Common Localization"
            );

            SaveLocaleValues(
                japaneseTable,
                japaneseGeneralSkill,
                japaneseUniqueSkill,
                japaneseItem,
                japaneseStatusEffect,
                japaneseRelic,
                japaneseRemainingTurn
            );

            EditorUtility.SetDirty(
                japaneseTable
            );
        }

        EditorUtility.SetDirty(
            collection.SharedData
        );

        AssetDatabase.SaveAssets();

        Debug.Log(
            "Tooltip Common EN / JA 저장 완료"
        );
    }

    private void SaveLocaleValues(
        StringTable table,
        string generalSkill,
        string uniqueSkill,
        string item,
        string statusEffect,
        string relic,
        string remainingTurn)
    {
        DevoryaLocalizationEditorUtility
            .SetTableValue(
                table,
                GeneralSkillCategoryKey,
                generalSkill
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                table,
                UniqueSkillCategoryKey,
                uniqueSkill
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                table,
                ItemCategoryKey,
                item
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                table,
                StatusEffectCategoryKey,
                statusEffect
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                table,
                RelicCategoryKey,
                relic
            );

        DevoryaLocalizationEditorUtility
            .SetTableValue(
                table,
                RemainingTurnKey,
                remainingTurn
            );
    }
}