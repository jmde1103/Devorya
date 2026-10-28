using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class TooltipTranslationImport
{
    [Serializable] public class Input { public Row[] entries; }
    [Serializable] public class Row
    {
        public string collection, key, locale, text, previous;
        public TooltipSemanticMetadata.Span[] spans;
    }

    public static void Run() => Import("Logs/tooltip-translation-input.json", 384, false);
    public static void RunGuideV2() => Import("Logs/text-guide-v2-input.json", 135, true);
    public static void RunGuideV2Corrections() => Import("Logs/text-guide-v2-corrections.json", 14, true);

    static void Import(string inputPath, int expectedCount, bool verifyPrevious)
    {
        try
        {
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(inputPath));
            if(input.entries.Length != expectedCount) throw new Exception("Unexpected input count");
            // Validate the entire migration before touching any tables. Refuse stale exports.
            if (verifyPrevious)
            foreach (var row in input.entries)
            {
                var c = LocalizationEditorSettings.GetStringTableCollection(row.collection);
                var t = c?.GetTable(new LocaleIdentifier(row.locale)) as StringTable;
                var e = t?.GetEntry(row.key);
                if (e == null || (e.Value != row.previous && e.Value != row.text))
                    throw new Exception("Source changed since review: " + row.key + " / " + row.locale);
                if (row.spans.Any(span => !row.text.Contains(span.text)))
                    throw new Exception("Invalid semantic span: " + row.key);
            }
            foreach(var row in input.entries)
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(row.collection);
                if(collection == null) throw new Exception("Missing collection "+row.collection);
                var shared = collection.SharedData.GetEntry(row.key);
                if(shared == null) throw new Exception("Missing existing key "+row.key);
                var table = collection.GetTable(new LocaleIdentifier(row.locale)) as StringTable;
                if(table == null) throw new Exception("Missing locale "+row.locale);
                var entry = table.GetEntry(shared.Id);
                if(entry == null) throw new Exception("Missing existing locale entry "+row.key);
                entry.Value = row.text;
                var metadata = entry.GetMetadata<TooltipSemanticMetadata>();
                if(metadata != null) entry.RemoveMetadata(metadata);
                if(row.spans.Length > 0)
                {
                    metadata = new TooltipSemanticMetadata();
                    metadata.spans.AddRange(row.spans);
                    entry.AddMetadata(metadata);
                }
                EditorUtility.SetDirty(table);
            }
            // Keep existing references and sync only their Korean fallback fields.
            int synced = 0;
            foreach(string type in new[]{"GeneralSkillData","UniqueSkillData","BattleItemData","StatusEffectData","BattleRelicData"})
            foreach(string guid in AssetDatabase.FindAssets("t:"+type))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                bool changed=false;
                foreach(var field in asset.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))
                {
                    if(field.FieldType != typeof(LocalizedString)) continue;
                    var reference = field.GetValue(asset) as LocalizedString;
                    if(reference == null || reference.IsEmpty) continue;
                    var collection = LocalizationEditorSettings.GetStringTableCollection(reference.TableReference);
                    if(collection == null) continue;
                    var shared = reference.TableEntryReference.KeyId != 0 ? collection.SharedData.GetEntry(reference.TableEntryReference.KeyId) : collection.SharedData.GetEntry(reference.TableEntryReference.Key);
                    if(shared == null) continue;
                    var row=input.entries.FirstOrDefault(r=>r.locale=="ko" && r.collection==collection.TableCollectionName && r.key==shared.Key);
                    if(row==null) continue;
                    string fallback=char.ToLowerInvariant(field.Name[9])+field.Name.Substring(10);
                    var fallbackField=asset.GetType().GetField(fallback);
                    if(fallbackField==null || fallbackField.FieldType!=typeof(string)) continue;
                    if((string)fallbackField.GetValue(asset)!=row.text){fallbackField.SetValue(asset,row.text);changed=true;synced++;}
                }
                if(changed) EditorUtility.SetDirty(asset);
            }
            const string folder="Assets/Resources";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets","Resources");
            const string path=folder+"/TooltipStylePalette.asset";
            if(AssetDatabase.LoadAssetAtPath<TooltipStylePalette>(path)==null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TooltipStylePalette>(),path);
            if (verifyPrevious) EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<TooltipStylePalette>(path));
            AssetDatabase.SaveAssets();
            Debug.Log("TOOLTIP_IMPORT_PASS entries="+input.entries.Length+" fallbackFields="+synced);
            TooltipStyleChecks.Run();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
