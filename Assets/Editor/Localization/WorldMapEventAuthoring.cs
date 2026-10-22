using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

// Editor-only operations keep text, translation references and stable IDs together.
public static class WorldMapEventAuthoring
{
    public const string TableName = "WorldMap_Dialogue";
    public static void Normalize(WorldMapEventStep step)
    {
        step.dialoguePages ??= new List<string>();
        step.localizedDialoguePages ??= new List<LocalizedString>();
        step.dialoguePageIds ??= new List<string>();
        int count=Math.Max(step.dialoguePages.Count,step.localizedDialoguePages.Count);
        while(step.dialoguePages.Count<count)step.dialoguePages.Add("");
        while(step.localizedDialoguePages.Count<count)step.localizedDialoguePages.Add(new LocalizedString());
        while(step.dialoguePageIds.Count<count)step.dialoguePageIds.Add("");
        var used=new HashSet<string>();
        for(int i=0;i<count;i++) {
            if(string.IsNullOrEmpty(step.dialoguePageIds[i])||!used.Add(step.dialoguePageIds[i])) {
                step.dialoguePageIds[i]=Guid.NewGuid().ToString("N");used.Add(step.dialoguePageIds[i]);
            }
        }
    }
    public static string Key(WorldMapEventData data,WorldMapEventStep step,int page)
    {
        return "worldmap_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data))+"_"+step.dialoguePageIds[page];
    }
    public static StringTableCollection Collection(LocalizedString reference)
    {
        return reference==null||reference.IsEmpty?null:LocalizationEditorSettings.GetStringTableCollection(reference.TableReference);
    }
    public static string ReferenceKey(LocalizedString reference)
    {
        var collection=Collection(reference);
        if(collection==null)return null;
        var entry=reference.TableEntryReference;
        return (entry.KeyId!=0?collection.SharedData.GetEntry(entry.KeyId):collection.SharedData.GetEntry(entry.Key))?.Key;
    }
    public static void AddPage(WorldMapEventData data,WorldMapEventStep step)
    {
        Undo.RecordObject(data,"Add Dialogue Page");Normalize(step);
        step.dialoguePages.Add("");step.localizedDialoguePages.Add(new LocalizedString());step.dialoguePageIds.Add(Guid.NewGuid().ToString("N"));EditorUtility.SetDirty(data);
    }
    public static void MovePage(WorldMapEventData data,WorldMapEventStep step,int from,int to)
    {
        Undo.RecordObject(data,"Move Dialogue Page");Normalize(step);
        Move(step.dialoguePages,from,to);Move(step.localizedDialoguePages,from,to);Move(step.dialoguePageIds,from,to);EditorUtility.SetDirty(data);
    }
    static void Move<T>(List<T> list,int from,int to){var value=list[from];list.RemoveAt(from);list.Insert(to,value);}
    public static void RemovePage(WorldMapEventData data,WorldMapEventStep step,int page)
    {
        Undo.RecordObject(data,"Remove Dialogue Page");Normalize(step);
        step.dialoguePages.RemoveAt(page);step.localizedDialoguePages.RemoveAt(page);step.dialoguePageIds.RemoveAt(page);EditorUtility.SetDirty(data);
    }
    public static WorldMapEventStep Duplicate(WorldMapEventData data,int index,string tableName=TableName)
    {
        Undo.RecordObject(data,"Duplicate World Map Event Step");
        var copy=JsonUtility.FromJson<WorldMapEventStep>(JsonUtility.ToJson(data.steps[index]));
        copy.stepName+=" (복사)";copy.dialoguePageIds.Clear();Normalize(copy);
        // Copy translation values to independent keys; editing the copy must not change its source.
        if(copy.type==WorldMapEventStepType.Dialogue&&copy.localizedDialoguePages.Exists(r=>r!=null&&!r.IsEmpty))SyncStep(data,copy,tableName);
        data.steps.Insert(index+1,copy);EditorUtility.SetDirty(data);return copy;
    }
    public static void Sync(WorldMapEventData data,string tableName=TableName)
    {
        Undo.RecordObject(data,"Sync World Map Dialogue Localization");
        foreach(var step in data.steps)if(step!=null&&step.type==WorldMapEventStepType.Dialogue)SyncStep(data,step,tableName);
        EditorUtility.SetDirty(data);
    }
    static void SyncStep(WorldMapEventData data,WorldMapEventStep step,string tableName)
    {
        Normalize(step);
        var collection=DevoryaLocalizationEditorUtility.GetOrCreateStringTableCollection(tableName);
        var tables=new Dictionary<string,StringTable>();
        Undo.RecordObject(collection.SharedData,"Sync World Map Dialogue Keys");
        foreach(var locale in new[]{"ko","en","ja"}) {
            var table=DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection,locale);
            if(table==null)throw new InvalidOperationException("Locale missing: "+locale);
            Undo.RecordObject(table,"Sync World Map Dialogue Translation");tables.Add(locale,table);
        }
        for(int i=0;i<step.dialoguePages.Count;i++) {
            string key=Key(data,step,i);var old=step.localizedDialoguePages[i];var oldCollection=Collection(old);string oldKey=ReferenceKey(old);
            foreach(var locale in new[]{"ko","en","ja"}) {
                var table=tables[locale];var oldTable=oldCollection?.GetTable(locale) as StringTable;
                string previous=DevoryaLocalizationEditorUtility.GetTableValue(oldTable,oldKey);
                if(locale=="ko") {
                    // Existing reference-only pages keep their Korean text when first adopting this editor.
                    if(string.IsNullOrEmpty(step.dialoguePages[i])&&!string.IsNullOrEmpty(previous))step.dialoguePages[i]=previous;
                    DevoryaLocalizationEditorUtility.SetTableValue(table,key,step.dialoguePages[i]);
                } else if(table.GetEntry(key)==null)DevoryaLocalizationEditorUtility.SetTableValue(table,key,previous);
            }
            step.localizedDialoguePages[i]=DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection,key);
        }
    }
}
