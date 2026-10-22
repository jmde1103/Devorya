using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
public static class LocalizationInspectorSyncChecks
{
    public static void Run()
    {
        try {
            int assets=0,refs=0;
            var types=new[]{typeof(UniqueSkillData),typeof(GeneralSkillData),typeof(StatusEffectData),typeof(BattleItemData),typeof(BattleRelicData)};
            foreach(var type in types) foreach(var guid in AssetDatabase.FindAssets("t:"+type.Name)) {
                var asset=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var editor=Editor.CreateEditor(asset);var tableName=(string)editor.GetType().GetField("TableCollectionName",BindingFlags.NonPublic|BindingFlags.Static).GetRawConstantValue();
                var fields=type==typeof(UniqueSkillData)?new[]{"localizedSkillName","localizedDescription","localizedConditionFailMessage"}:
                    type==typeof(GeneralSkillData)?new[]{"localizedSkillName","localizedDescription","localizedTooltipDescriptionFormat"}:
                    type==typeof(StatusEffectData)?new[]{"localizedEffectName","localizedDescription"}:
                    type==typeof(BattleItemData)?new[]{"localizedItemName","localizedDescription"}:new[]{"localizedRelicName","localizedDescription"};
                var c=LocalizationEditorSettings.GetStringTableCollection(tableName);var before=new Dictionary<string,string>();var ids=new List<long>();var args=new List<object>{asset};
                foreach(var field in fields){var reference=(LocalizedString)type.GetField(field).GetValue(asset);var key=DevoryaLocalizationEditorUtility.ResolveLinkedKey(reference,tableName,"INVALID");if(key=="INVALID")throw new Exception(asset.name+" unresolved Inspector key");
                    ids.Add(reference.TableEntryReference.KeyId);args.Add(key);
                    foreach(var locale in new[]{"en","ja"}){var entry=((StringTable)c.GetTable(locale)).GetEntry(key);if(entry==null||string.IsNullOrWhiteSpace(entry.Value))throw new Exception(asset.name+" missing "+locale+" "+key);before[locale+key]=entry.Value;} refs++;
                }
                editor.GetType().GetMethod("SyncLocalization",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(editor,args.ToArray());
                for(int i=0;i<fields.Length;i++){var reference=(LocalizedString)type.GetField(fields[i]).GetValue(asset);if(reference.TableEntryReference.KeyId!=ids[i])throw new Exception("Sync changed reference: "+asset.name);var key=(string)args[i+1];foreach(var locale in new[]{"en","ja"})if(((StringTable)c.GetTable(locale)).GetEntry(key).Value!=before[locale+key])throw new Exception("Sync changed translation: "+asset.name);}
                UnityEngine.Object.DestroyImmediate(editor);assets++;
            }
            AssetDatabase.SaveAssets();Debug.Log("LOCALIZATION_INSPECTOR_SYNC_PASS: "+assets+" assets, "+refs+" linked fields; English/Japanese populated; Inspector sync preserves existing keys and translations.");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
