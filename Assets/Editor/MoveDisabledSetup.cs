using System;
using UnityEditor;
using UnityEngine;

public static class MoveDisabledSetup
{
    public static void Run()
    {
        try
        {
            const string folder = "Assets/Devorya/Data/StatusEffect/";
            var db = AssetDatabase.LoadAssetAtPath<StatusEffectDatabase>(folder + "상태효과_데이터베이스.asset");
            var data = AssetDatabase.LoadAssetAtPath<StatusEffectData>(folder + "상태효과_이동불가.asset");
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StatusEffectData>();
                data.effectType = StatusEffectType.MoveDisabled;
                data.effectName = "이동불가";
                data.description = "[이동불가] 상태에서는 상대 기물을 잡는 이동만 할 수 있습니다.";
                data.durationTurn = 1;
                data.maxStack = 1;
                data.iconSprite = db.GetData(StatusEffectType.ActionDisabled).iconSprite;
                var collection = DevoryaLocalizationEditorUtility.GetOrCreateStringTableCollection("Status_Effect");
                string[] locales = { "ko", "en", "ja" };
                string[] names = { "이동불가", "Move Disabled", "移動不可" };
                string[] descriptions = { data.description, "While [Move Disabled], this piece can only move to capture an opposing piece.", "[移動不可]状態では、相手の駒を取る移動のみ可能です。" };
                for (int i = 0; i < locales.Length; i++)
                {
                    var table = DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection, locales[i]);
                    if (table == null) throw new Exception("Missing locale: " + locales[i]);
                    DevoryaLocalizationEditorUtility.SetTableValue(table, "status_effect_move_disabled_name", names[i]);
                    DevoryaLocalizationEditorUtility.SetTableValue(table, "status_effect_move_disabled_description", descriptions[i]);
                }
                data.localizedEffectName = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, "status_effect_move_disabled_name");
                data.localizedDescription = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, "status_effect_move_disabled_description");
                AssetDatabase.CreateAsset(data, folder + "상태효과_이동불가.asset");
            }
            if (db.GetData(StatusEffectType.MoveDisabled) == null)
            {
                var serialized = new SerializedObject(db);
                var list = serialized.FindProperty("statusEffectDataList");
                int index = list.arraySize++;
                list.GetArrayElementAtIndex(index).objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            if (db.GetData(StatusEffectType.MoveDisabled) != data || data.durationTurn != 1 || data.iconSprite == null)
                throw new Exception("MoveDisabled data validation failed");
            StatusRestrictionChecks.Run();
            Debug.Log("MOVE_DISABLED_CHECKS_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
