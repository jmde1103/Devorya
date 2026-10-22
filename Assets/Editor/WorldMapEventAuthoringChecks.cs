using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class WorldMapEventAuthoringChecks
{
    const string Folder="Assets/__WorldMapAuthoringValidation";
    const string Table="__WorldMapAuthoringValidation";
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    public static void Run()
    {
        bool created=false;Editor inspector=null;
        try {
            if(AssetDatabase.IsValidFolder(Folder))throw new Exception("Validation folder already exists; refusing to replace it");
            AssetDatabase.CreateFolder("Assets","__WorldMapAuthoringValidation");created=true;
            var collection=LocalizationEditorSettings.CreateStringTableCollection(Table,Folder);
            var data=ScriptableObject.CreateInstance<WorldMapEventData>();AssetDatabase.CreateAsset(data,Folder+"/Fixture.asset");
            var step=new WorldMapEventStep{stepName="대화 검증"};step.dialoguePages.Add("첫 페이지");step.dialoguePages.Add("두 번째 페이지");data.steps.Add(step);
            foreach(WorldMapEventStepType type in Enum.GetValues(typeof(WorldMapEventStepType)))if(type!=WorldMapEventStepType.Dialogue)data.steps.Add(new WorldMapEventStep{type=type});
            inspector=Editor.CreateEditor(data);Check(inspector is WorldMapEventDataEditor,"Custom editor selection");
            var list=(ReorderableList)typeof(WorldMapEventDataEditor).GetField("list",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(inspector);
            Check(list.draggable&&list.displayAdd&&list.displayRemove,"Same reorder/add/remove controls");
            var serialized=inspector.serializedObject;var steps=serialized.FindProperty("steps");
            for(int i=0;i<data.steps.Count;i++){steps.GetArrayElementAtIndex(i).isExpanded=true;Check(list.elementHeightCallback(i)>24,"Valid height for each step type");}
            WorldMapEventAuthoring.Sync(data,Table);
            string first=WorldMapEventAuthoring.Key(data,step,0),second=WorldMapEventAuthoring.Key(data,step,1);
            var en=collection.GetTable("en") as StringTable;var ja=collection.GetTable("ja") as StringTable;
            DevoryaLocalizationEditorUtility.SetTableValue(en,first,"First");DevoryaLocalizationEditorUtility.SetTableValue(ja,first,"最初");
            Check(WorldMapEventAuthoring.ReferenceKey(step.localizedDialoguePages[0])==first,"Runtime reference points to stable key");
            WorldMapEventAuthoring.MovePage(data,step,0,1);
            Check(step.dialoguePages[1]=="첫 페이지"&&WorldMapEventAuthoring.Key(data,step,1)==first&&WorldMapEventAuthoring.ReferenceKey(step.localizedDialoguePages[1])==first,"Move keeps text and translations paired");
            var copy=WorldMapEventAuthoring.Duplicate(data,0,Table);string copied=WorldMapEventAuthoring.Key(data,copy,1);
            Check(copied!=first&&copy.dialoguePages[1]=="첫 페이지","Duplicate has independent IDs and same content");
            Check(DevoryaLocalizationEditorUtility.GetTableValue(en,copied)=="First"&&DevoryaLocalizationEditorUtility.GetTableValue(ja,copied)=="最初","Duplicate preserves EN and JA");
            DevoryaLocalizationEditorUtility.SetTableValue(en,copied,"Changed copy");Check(DevoryaLocalizationEditorUtility.GetTableValue(en,first)=="First","Editing copy leaves original translation intact");
            step.dialoguePages[1]="한국어 수정";data.eventName="이름 변경";data.steps.Reverse();WorldMapEventAuthoring.Sync(data,Table);
            Check(WorldMapEventAuthoring.ReferenceKey(step.localizedDialoguePages[1])==first&&DevoryaLocalizationEditorUtility.GetTableValue(en,first)=="First","Rename and step reorder preserve translations");
            Check(DevoryaLocalizationEditorUtility.GetTableValue(collection.GetTable("ko") as StringTable,first)=="한국어 수정","Korean sync updates existing key");
            WorldMapEventAuthoring.RemovePage(data,step,0);Check(step.dialoguePages.Count==1&&step.dialoguePageIds.Count==1&&step.localizedDialoguePages.Count==1&&WorldMapEventAuthoring.Key(data,step,0)==first,"Remove preserves remaining page identity");
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int before=step.dialoguePages.Count;WorldMapEventAuthoring.AddPage(data,step);Undo.FlushUndoRecordObjects();Undo.PerformUndo();
            step=data.steps.Single(s=>s.stepName=="대화 검증");Check(step.dialoguePages.Count==before,"Page add supports Undo");
            AssetDatabase.SaveAssets();Check(AssetDatabase.LoadAssetAtPath<WorldMapEventData>(Folder+"/Fixture.asset").steps.Count==7,"Save retains all types and duplicate");
            Debug.Log("WORLD MAP AUTHORING PASS: "+checks+" checks.");
        }catch(Exception e){Debug.LogException(e);Finish(inspector,created);EditorApplication.Exit(1);return;}
        Finish(inspector,created);EditorApplication.Exit(0);
    }
    static void Finish(Editor inspector,bool created){if(inspector!=null)UnityEngine.Object.DestroyImmediate(inspector);Undo.ClearAll();if(created)AssetDatabase.DeleteAsset(Folder);AssetDatabase.SaveAssets();}
}
