using System;
using UnityEditor;
using UnityEngine;

public static class AbilitySealedSetup
{
    public static void Run()
    {
        try
        {
            const string folder = "Assets/Devorya/Data/StatusEffect/";
            var db = AssetDatabase.LoadAssetAtPath<StatusEffectDatabase>(folder + "상태효과_데이터베이스.asset");
            var data = AssetDatabase.LoadAssetAtPath<StatusEffectData>(folder + "상태효과_능력봉인.asset");
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StatusEffectData>();
                data.effectType = StatusEffectType.AbilitySealed;
                data.effectName = "능력봉인";
                data.description = "[능력봉인] 상태에서는 능력을 사용할 수 없습니다.";
                data.durationTurn = 1;
                data.maxStack = 1;
                data.iconSprite = db.GetData(StatusEffectType.ActionDisabled).iconSprite;
                var collection = DevoryaLocalizationEditorUtility.GetOrCreateStringTableCollection("Status_Effect");
                string[] locales = { "ko", "en", "ja" };
                string[] names = { "능력봉인", "Ability Sealed", "能力封印" };
                string[] descriptions = { data.description, "While [Ability Sealed], this piece cannot use abilities.", "[能力封印]状態では能力を使用できません。" };
                for (int i = 0; i < locales.Length; i++)
                {
                    var table = DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection, locales[i]);
                    if (table == null) throw new Exception("Missing locale: " + locales[i]);
                    DevoryaLocalizationEditorUtility.SetTableValue(table, "status_effect_ability_sealed_name", names[i]);
                    DevoryaLocalizationEditorUtility.SetTableValue(table, "status_effect_ability_sealed_description", descriptions[i]);
                }
                data.localizedEffectName = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, "status_effect_ability_sealed_name");
                data.localizedDescription = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, "status_effect_ability_sealed_description");
                AssetDatabase.CreateAsset(data, folder + "상태효과_능력봉인.asset");
            }
            if (db.GetData(StatusEffectType.AbilitySealed) == null)
            {
                var serialized = new SerializedObject(db);
                var list = serialized.FindProperty("statusEffectDataList");
                int index = list.arraySize++;
                list.GetArrayElementAtIndex(index).objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            if (db.GetData(StatusEffectType.AbilitySealed) != data || data.durationTurn != 1 || data.iconSprite == null)
                throw new Exception("AbilitySealed data validation failed");
            StatusRestrictionChecks.Run();
            Debug.Log("ABILITY_SEALED_CHECKS_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
