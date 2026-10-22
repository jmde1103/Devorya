using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

[CustomEditor(typeof(WorldMapEventData))]
public sealed class WorldMapEventDataEditor : Editor
{
    const float HeaderHeight=24;
    ReorderableList list;
    SerializedProperty steps;
    Action pending;
    readonly HashSet<WorldMapEventStep> expandedDialogue=new HashSet<WorldMapEventStep>();
    WorldMapEventData Data => (WorldMapEventData)target;
    void OnEnable()
    {
        steps=serializedObject.FindProperty("steps");
        list=new ReorderableList(serializedObject,steps,true,true,true,true);
        list.drawHeaderCallback=r=>EditorGUI.LabelField(r,$"Steps    {steps.arraySize}");
        list.elementHeightCallback=i=>HeaderHeight+(steps.GetArrayElementAtIndex(i).isExpanded?Body(default,steps.GetArrayElementAtIndex(i),i,false):0);
        list.drawElementCallback=(r,i,active,focused)=>{
            var p=steps.GetArrayElementAtIndex(i);var type=p.FindPropertyRelative("type");var name=p.FindPropertyRelative("stepName").stringValue;
            if(string.IsNullOrWhiteSpace(name))name=type.enumDisplayNames[type.enumValueIndex];
            var header=new Rect(r.x+18,r.y+2,r.width-18,EditorGUIUtility.singleLineHeight);
            p.isExpanded=EditorGUI.Foldout(header,p.isExpanded,$"Step{i+1:D2} - {name}",true);
            if(p.isExpanded)Body(new Rect(r.x+18,r.y+HeaderHeight,r.width-18,r.height-HeaderHeight),p,i,true);
        };
        list.onAddCallback=_=>pending=()=>{Undo.RecordObject(Data,"Add World Map Event Step");Data.steps.Add(new WorldMapEventStep());EditorUtility.SetDirty(Data);};
        list.onRemoveCallback=l=>{int i=l.index;if(i>=0&&i<Data.steps.Count)pending=()=>{Undo.RecordObject(Data,"Remove World Map Event Step");expandedDialogue.Remove(Data.steps[i]);Data.steps.RemoveAt(i);EditorUtility.SetDirty(Data);};};
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject,"m_Script","steps");
        EditorGUILayout.Space(12);EditorGUILayout.LabelField("Event Steps",EditorStyles.boldLabel);
        StepFoldouts();list.DoLayoutList();StepFoldouts();
        serializedObject.ApplyModifiedProperties();
        if(Data.steps.Count==0)EditorGUILayout.HelpBox("Event Step이 없습니다. + 버튼으로 추가하세요.",MessageType.Info);
        FlushPending();
        EditorGUILayout.Space(12);DrawDialogue();FlushPending();
    }
    void FlushPending()
    {
        if(pending==null)return;var action=pending;pending=null;action();serializedObject.Update();Repaint();GUIUtility.ExitGUI();
    }
    void StepFoldouts()
    {
        using(new EditorGUILayout.HorizontalScope()) {
            if(GUILayout.Button("▼ 전체 펼치기"))for(int i=0;i<steps.arraySize;i++)steps.GetArrayElementAtIndex(i).isExpanded=true;
            if(GUILayout.Button("▲ 전체 접기"))for(int i=0;i<steps.arraySize;i++)steps.GetArrayElementAtIndex(i).isExpanded=false;
        }
    }
    float Body(Rect rect,SerializedProperty step,int index,bool draw)
    {
        float y=rect.y;
        foreach(var field in WorldMapEventStepDrawer.Fields(step)) {
            if(field.field=="dialoguePages"||field.field=="localizedDialoguePages")continue;
            var child=step.FindPropertyRelative(field.field);float h=EditorGUI.GetPropertyHeight(child,true);
            if(draw)EditorGUI.PropertyField(new Rect(rect.x,y,rect.width,h),child,new GUIContent(field.label),true);
            y+=h+4;
        }
        if(step.FindPropertyRelative("type").intValue==(int)WorldMapEventStepType.Dialogue) {
            if(draw)EditorGUI.HelpBox(new Rect(rect.x,y,rect.width,44),"대화 내용은 아래 Event Dialogue Localization의 Dialogue Pages에서 관리합니다.\n현재 Page: "+step.FindPropertyRelative("dialoguePages").arraySize,MessageType.Info);
            y+=48;
        }
        if(draw&&GUI.Button(new Rect(rect.x,y,rect.width,22),"Duplicate Step Below (바로 아래 복제)"))pending=()=>WorldMapEventAuthoring.Duplicate(Data,index);
        return y-rect.y+30;
    }
    void DrawDialogue()
    {
        EditorGUILayout.LabelField("Event Dialogue Localization",EditorStyles.boldLabel);
        using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
            EditorGUILayout.LabelField("Localization ID",AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Data)));
            EditorGUILayout.HelpBox("에셋과 페이지의 고정 ID를 사용합니다. 이름이나 스텝 순서를 바꿔도 번역 연결은 유지됩니다.",MessageType.None);
            if(GUILayout.Button("Localization 생성 / 한국어 동기화"))pending=()=>{WorldMapEventAuthoring.Sync(Data);AssetDatabase.SaveAssets();};
            EditorGUILayout.Space(8);EditorGUILayout.LabelField("Dialogue Pages",EditorStyles.boldLabel);
            using(new EditorGUILayout.HorizontalScope()) {
                if(GUILayout.Button("▼ 전체 펼치기"))foreach(var step in Data.steps)if(step.type==WorldMapEventStepType.Dialogue)expandedDialogue.Add(step);
                if(GUILayout.Button("▲ 전체 접기"))expandedDialogue.Clear();
            }
            int pages=0, english=0, japanese=0;
            for(int i=0;i<Data.steps.Count;i++) {
                var step=Data.steps[i];if(step==null||step.type!=WorldMapEventStepType.Dialogue)continue;
                int count=Math.Max(step.dialoguePages?.Count??0,step.localizedDialoguePages?.Count??0);
                bool open=EditorGUILayout.Foldout(expandedDialogue.Contains(step),$"Step{i+1:D2} - {step.stepName} / {count} Pages",true);
                if(open)expandedDialogue.Add(step);else expandedDialogue.Remove(step);
                for(int page=0;page<count;page++) {
                    var reference=page<(step.localizedDialoguePages?.Count??0)?step.localizedDialoguePages[page]:null;
                    var collection=WorldMapEventAuthoring.Collection(reference);var key=WorldMapEventAuthoring.ReferenceKey(reference);
                    pages++;if(HasText(collection,"en",key))english++;if(HasText(collection,"ja",key))japanese++;
                    if(open)DrawPage(step,page,count,collection,key);
                }
                if(open&&GUILayout.Button("+ Dialogue Page"))pending=()=>WorldMapEventAuthoring.AddPage(Data,step);
            }
            EditorGUILayout.Space(8);EditorGUILayout.LabelField($"번역 상태: {pages} Pages / English {english}/{pages} / Japanese {japanese}/{pages}");
            if(GUILayout.Button("Localization Tables 열기"))DevoryaLocalizationEditorUtility.OpenLocalizationTables();
        }
    }
    void DrawPage(WorldMapEventStep step,int page,int count,StringTableCollection collection,string key)
    {
        using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
            using(new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField($"Page {page+1}",EditorStyles.boldLabel);
                using(new EditorGUI.DisabledScope(page==0))if(GUILayout.Button("▲",GUILayout.Width(28)))pending=()=>WorldMapEventAuthoring.MovePage(Data,step,page,page-1);
                using(new EditorGUI.DisabledScope(page==count-1))if(GUILayout.Button("▼",GUILayout.Width(28)))pending=()=>WorldMapEventAuthoring.MovePage(Data,step,page,page+1);
                if(GUILayout.Button("삭제",GUILayout.Width(45)))pending=()=>WorldMapEventAuthoring.RemovePage(Data,step,page);
            }
            string old=page<(step.dialoguePages?.Count??0)?step.dialoguePages[page]:"";
            EditorGUILayout.LabelField("Korean");EditorGUI.BeginChangeCheck();string value=EditorGUILayout.TextArea(old??"",GUILayout.MinHeight(55));
            if(EditorGUI.EndChangeCheck()){Undo.RecordObject(Data,"Edit World Map Dialogue Korean");WorldMapEventAuthoring.Normalize(step);step.dialoguePages[page]=value;EditorUtility.SetDirty(Data);}
            if(collection==null||string.IsNullOrEmpty(key))EditorGUILayout.HelpBox("Localization 생성 / 한국어 동기화를 실행하면 Stable Key와 EN / JA 입력란이 생성됩니다.",MessageType.None);
            else {
                EditorGUILayout.LabelField("Key",key);
                Translation(collection,"English","en",key);Translation(collection,"Japanese","ja",key);
            }
        }
    }
    static bool HasText(StringTableCollection collection,string locale,string key)=>!string.IsNullOrWhiteSpace(DevoryaLocalizationEditorUtility.GetTableValue(collection?.GetTable(locale) as StringTable,key));
    static void Translation(StringTableCollection collection,string label,string locale,string key)
    {
        var table=collection.GetTable(locale) as StringTable;
        EditorGUILayout.LabelField(label);string old=DevoryaLocalizationEditorUtility.GetTableValue(table,key);
        EditorGUI.BeginChangeCheck();string value=EditorGUILayout.TextArea(old,GUILayout.MinHeight(45));
        if(EditorGUI.EndChangeCheck()) {
            table=DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection,locale);if(table==null)return;
            Undo.RecordObject(table,"Edit World Map Dialogue Translation");Undo.RecordObject(collection.SharedData,"Edit World Map Dialogue Key");DevoryaLocalizationEditorUtility.SetTableValue(table,key,value);
        }
    }
}
