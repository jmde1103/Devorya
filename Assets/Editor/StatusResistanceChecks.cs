using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public static class StatusResistanceChecks
{
    [MenuItem("Tools/DEVORYA/Install and Check Resistance Traits")]
    public static void Run()
    {
        var randomState = Random.state;
        try
        {
            Install();
            Check();
            Debug.Log("STATUS_RESISTANCE_CHECKS_PASS: twelve traits, all 100 roll boundaries per trait, reapplication, unrelated effects, strongest grade, database and tooltips.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
        finally { Random.state = randomState; }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Install()
    {
        var database = AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>("Assets/Devorya/Data/Skills/General/특성_데이터베이스.asset");
        Require(database != null, "Trait database missing");
        var serialized = new SerializedObject(database);
        var entries = serialized.FindProperty("generalSkillDatas");
        var collection = DevoryaLocalizationEditorUtility.GetOrCreateStringTableCollection("General_Skill");
        string[] grades = { "초급", "중급", "고급" };
        string[] englishGrades = { "Basic", "Intermediate", "Advanced" };
        string[] japaneseGrades = { "初級", "中級", "上級" };
        for (int i = 0; i < 12; i++)
        {
            var type = (GeneralSkillType)(5 + i);
            string effectName = i < 3 ? "공격불가" : i < 6 ? "행동불가" : i < 9 ? "이동불가" : "능력봉인";
            string name = effectName + " 저항 " + new[] { "I", "II", "III" }[i % 3];
            string path = "Assets/Devorya/Data/Skills/General/특성_" + name.Replace(':', '_') + ".asset";
            string oldPath = "Assets/Devorya/Data/Skills/General/특성_저항력(" + grades[i % 3] + ")_ " + effectName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<GeneralSkillData>(path) == null &&
                AssetDatabase.LoadAssetAtPath<GeneralSkillData>(oldPath) != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(oldPath);
                string error = AssetDatabase.MoveAsset(oldPath, path);
                Require(string.IsNullOrEmpty(error), error);
                Require(AssetDatabase.AssetPathToGUID(path) == guid, "Rename must preserve GUID");
            }
            var data = AssetDatabase.LoadAssetAtPath<GeneralSkillData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<GeneralSkillData>();
                var status = AssetDatabase.LoadAssetAtPath<StatusEffectData>("Assets/Devorya/Data/StatusEffect/상태효과_" + effectName + ".asset");
                Require(status != null, "Status asset missing: " + effectName);
                data.skillType = type;
                data.skillName = name;
                data.description = effectName + " 상태효과를 받을 때 " + StatusResistance.GetPercent(type) + "% 확률로 저항하여 해당 효과를 받지 않는다.";
                data.tooltipDescriptionFormat = effectName + " 상태효과를 받을 때 {percent}% 확률로 저항하여 해당 효과를 받지 않는다.";
                data.iconSprite = status.iconSprite;
                data.tooltipSections = new List<TooltipSectionData> { new TooltipSectionData { statusEffectData = status } };
                string key = "general_skill_resistance_" + type;
                string[] locales = { "ko", "en", "ja" };
                string enEffect = i < 3 ? "Attack Disabled" : i < 6 ? "Action Disabled" : i < 9 ? "Move Disabled" : "Ability Sealed";
                string jaEffect = i < 3 ? "攻撃不可" : i < 6 ? "行動不可" : i < 9 ? "移動不可" : "能力封印";
                string[] names = { name, "Resistance (" + englishGrades[i % 3] + "): " + enEffect, "耐性（" + japaneseGrades[i % 3] + "）：" + jaEffect };
                string[] formats = { data.tooltipDescriptionFormat, "Has a {percent}% chance to resist " + enEffect + " when it is applied.", jaEffect + "が付与される際、{percent}%の確率で抵抗し、その効果を受けない。" };
                for (int j = 0; j < locales.Length; j++)
                {
                    var table = DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection, locales[j]);
                    Require(table != null, "Locale missing: " + locales[j]);
                    DevoryaLocalizationEditorUtility.SetTableValue(table, key + "_name", names[j]);
                    DevoryaLocalizationEditorUtility.SetTableValue(table, key + "_description", formats[j].Replace("{percent}", StatusResistance.GetPercent(type).ToString()));
                    DevoryaLocalizationEditorUtility.SetTableValue(table, key + "_format", formats[j]);
                }
                data.localizedSkillName = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, key + "_name");
                data.localizedDescription = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, key + "_description");
                data.localizedTooltipDescriptionFormat = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, key + "_format");
                AssetDatabase.CreateAsset(data, path);
            }
            data.skillName = name;
            data.name = "특성_" + name;
            EditorUtility.SetDirty(data);
            var korean = DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection, "ko");
            DevoryaLocalizationEditorUtility.SetTableValue(korean, "general_skill_resistance_" + type + "_name", name);
            Require(data.skillName == name && AssetDatabase.GetAssetPath(data) == path, "Renamed display and asset");
            bool found = false;
            for (int j = 0; j < entries.arraySize; j++)
                if (entries.GetArrayElementAtIndex(j).objectReferenceValue == data) found = true;
            if (!found)
            {
                int index = entries.arraySize++;
                entries.GetArrayElementAtIndex(index).objectReferenceValue = data;
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    private static void Check()
    {
        var seeds = new int[100];
        var found = new bool[100];
        int count = 0;
        for (int seed = 0; count < 100 && seed < 100000; seed++)
        {
            Random.InitState(seed);
            int roll = Random.Range(0, 100);
            if (!found[roll]) { found[roll] = true; seeds[roll] = seed; count++; }
        }
        Require(count == 100, "Seed coverage");
        var database = AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>("Assets/Devorya/Data/Skills/General/특성_데이터베이스.asset");
        int[] expected = { 15, 30, 50, 10, 20, 30, 15, 30, 50, 15, 30, 50 };
        for (int i = 0; i < 12; i++)
        {
            var type = (GeneralSkillType)(5 + i);
            var data = database.GetData(type);
            Require(data != null && data.GetTooltipMainValue() == expected[i], "Database/tooltip " + type);
            Require(data.tooltipSections.Count == 1 && data.tooltipSections[0].statusEffectData.effectType == StatusResistance.GetEffect(type), "Tooltip section " + type);
            for (int roll = 0; roll < 100; roll++)
            {
                var go = new GameObject("ResistanceTestPiece");
                var effect = ScriptableObject.CreateInstance<StatusEffectData>();
                try
                {
                    var piece = go.AddComponent<Piece>();
                    piece.AddGeneralSkill(type);
                    effect.effectType = StatusResistance.GetEffect(type);
                    effect.maxStack = 3;
                    Random.InitState(seeds[roll]);
                    piece.AddStatusEffect(effect, 2);
                    var applied = piece.GetStatusEffectDataCopy(effect.effectType);
                    Require((applied == null) == (roll < expected[i]), "Application boundary " + type + " roll=" + roll);
                    if (applied != null)
                    {
                        Random.InitState(seeds[0]);
                        piece.AddStatusEffect(effect, 5);
                        applied = piece.GetStatusEffectDataCopy(effect.effectType);
                        Require(applied.remainingTurn == 2 && applied.stackCount == 1, "Resisted refresh changed existing effect");
                    }
                    effect.effectType = StatusEffectType.Defence;
                    Random.InitState(seeds[0]);
                    var before = Random.state;
                    piece.AddStatusEffect(effect, 1);
                    Require(piece.GetStatusEffectDataCopy(effect.effectType) != null, "Unrelated effect blocked");
                    Require(JsonUtility.ToJson(before) == JsonUtility.ToJson(Random.state), "Unrelated effect consumed randomness");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(effect); }
            }
        }
        var traits = new List<OwnedGeneralSkillData>();
        for (int i = 5; i <= 16; i++) traits.Add(new OwnedGeneralSkillData((GeneralSkillType)i));
        Require(StatusResistance.GetChance(traits, StatusEffectType.AttackDisabled) == 50, "Attack resistance stacking");
        Require(StatusResistance.GetChance(traits, StatusEffectType.ActionDisabled) == 30, "Action resistance stacking");
        Require(StatusResistance.GetChance(traits, StatusEffectType.MoveDisabled) == 50, "Move resistance stacking");
        Require(StatusResistance.GetChance(traits, StatusEffectType.AbilitySealed) == 50, "Ability seal resistance stacking");
        Require(StatusResistance.GetChance(null, StatusEffectType.ActionDisabled) == 0, "No traits");
    }
}
