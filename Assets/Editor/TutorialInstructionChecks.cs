using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

[InitializeOnLoad]
public static class TutorialInstructionChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const string Pending = "TutorialInstructionChecks";
    static TutorialInstructionChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                new GameObject("TutorialInstructionCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
            }
        };
    }
    static void Check(bool pass, string message) { if (!pass) throw new Exception(message); Debug.Log("INSTRUCTION_CHECK: " + message); }
    static object Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Flags).Invoke(o,args);
    static T Get<T>(object o, string field) => (T)o.GetType().GetField(field,Flags).GetValue(o);
    static void Set(object o, string field, object value) => o.GetType().GetField(field,Flags).SetValue(o,value);
    public static void Run()
    {
        try { CheckEditor(); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); return; }
        EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }
    static void CheckEditor()
    {
        var collection = DevoryaLocalizationEditorUtility.GetStringTableCollection("Event_Dialogue");
        var originals = new Dictionary<string,byte[]>();
        foreach(var obj in collection.StringTables.Cast<UnityEngine.Object>().Concat(new UnityEngine.Object[]{collection.SharedData}))
        {
            string path=AssetDatabase.GetAssetPath(obj); originals[path]=File.ReadAllBytes(path);
        }
        var data=ScriptableObject.CreateInstance<EventSequenceData>();
        data.localizationId="instruction_check_"+Guid.NewGuid().ToString("N");
        var step=new EventSequenceStepData{stepType=EventSequenceStepType.ForceButton,showMarker=false,showTutorialInstruction=true,tutorialInstructionText="표시된 버튼을 누르세요."};
        data.steps=new List<EventSequenceStepData>{step};
        var editor=Editor.CreateEditor(data);
        try
        {
            Check(!new EventSequenceStepData().showTutorialInstruction,"Existing/new data defaults OFF");
            Check((bool)Call(editor,"HasAnyTutorialInstruction",data),"Force-only sequence supports localization sync");
            Call(editor,"SyncLocalization",data);
            string id=step.tutorialInstructionLocalizationId;
            long entry=step.tutorialInstructionLocalizedText.TableEntryReference.KeyId;
            Check(!string.IsNullOrEmpty(id)&&entry!=0,"Stable instruction ID and reference generated");
            string key=collection.SharedData.GetEntry(entry).Key;
            var en=(StringTable)collection.GetTable("en"); var ja=(StringTable)collection.GetTable("ja");
            Check(string.IsNullOrEmpty(DevoryaLocalizationEditorUtility.GetTableValue(en,key)),"No invented English translation");
            DevoryaLocalizationEditorUtility.SetTableValue(en,key,"Select the highlighted button.");
            DevoryaLocalizationEditorUtility.SetTableValue(ja,key,"表示されたボタンを押してください。");
            step.stepName="Renamed"; data.steps.Insert(0,new EventSequenceStepData()); step.tutorialInstructionText="변경된 안내";
            Call(editor,"SyncLocalization",data);
            Check(step.tutorialInstructionLocalizationId==id&&step.tutorialInstructionLocalizedText.TableEntryReference.KeyId==entry,"Reorder/rename/Korean sync preserve key and entry ID");
            Check(en.GetEntry(key).Value=="Select the highlighted button."&&ja.GetEntry(key).Value=="表示されたボタンを押してください。","Korean sync preserves English and Japanese");
            Call(editor,"DuplicateEventSequenceStep",data,1);
            var copy=data.steps[2];
            Check(copy.tutorialInstructionLocalizationId!=id&&copy.tutorialInstructionLocalizedText.TableEntryReference.KeyId!=entry,"Duplicate has independent ID and reference");
            long copied=copy.tutorialInstructionLocalizedText.TableEntryReference.KeyId;
            Check(en.GetEntry(copied).Value==en.GetEntry(entry).Value&&ja.GetEntry(copied).Value==ja.GetEntry(entry).Value,"Duplicate copies EN/JA");
            var ko=(StringTable)collection.GetTable("ko");
            Check(ko.GetEntry(copied).Value==ko.GetEntry(entry).Value,"Duplicate copies Korean");
            var serialized=new SerializedObject(data);
            var prop=serialized.FindProperty("steps").GetArrayElementAtIndex(1);
            Check(prop.FindPropertyRelative("showTutorialInstruction").boolValue &&
                !prop.FindPropertyRelative("showMarker").boolValue &&
                prop.FindPropertyRelative("tutorialInstructionText").stringValue=="변경된 안내",
                "Instruction and marker serialize independently");

        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(editor);UnityEngine.Object.DestroyImmediate(data);
            foreach(var pair in originals) File.WriteAllBytes(pair.Key,pair.Value);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
    static IEnumerator Guard()
    {
        var tests=Checks();
        while(true)
        {
            object next;
            try { if(!tests.MoveNext())break; next=tests.Current; }
            catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1);yield break; }
            yield return next;
        }
        Debug.Log("TUTORIAL_INSTRUCTION_PLAYMODE_PASS");EditorApplication.Exit(0);
    }
    static IEnumerator Checks()
    {
        yield return LocalizationSettings.InitializationOperation;
        yield return new WaitForSecondsRealtime(5);
        var bm=BattleManager.Instance;bm.StopAllCoroutines();Set(bm,"battleAIManager",null);
        var controller=UnityEngine.Object.FindFirstObjectByType<EventSequenceController>();controller.StopSequence();
        var ui=UnityEngine.Object.FindFirstObjectByType<BattleUIController>();
        var text=Get<TMP_Text>(ui,"uniqueSkillFailureText");
        var canvas=Get<CanvasGroup>(ui,"uniqueSkillFailureCanvasGroup");
        float failureWait=Get<float>(ui,"uniqueSkillFailureHoldDuration")+Get<float>(ui,"uniqueSkillFailureFadeDuration")+0.7f;
        Check(text!=null&&canvas!=null,"Uses existing BattleScene failure popup");
        ui.BeginTutorialInstruction("First");yield return new WaitForSecondsRealtime(0.6f);
        Time.timeScale=0f;float alpha=canvas.alpha;yield return new WaitForSecondsRealtime(0.65f);
        Check(Mathf.Abs(alpha-canvas.alpha)>0.02f&&canvas.alpha>=0.49f&&canvas.alpha<=1f,"Slow pulse runs unscaled and stays readable");
        Check(!canvas.blocksRaycasts&&!canvas.interactable,"Instruction does not block input");Time.timeScale=1f;
        ui.ShowUniqueSkillFailureMessage("Failure");yield return null;
        Check(text.text=="Failure","Failure takes priority");
        ui.UpdateTutorialInstruction("Updated");yield return new WaitForSecondsRealtime(failureWait);
        Check(text.text=="Updated"&&text.gameObject.activeSelf,"Failure restores latest instruction");
        ui.ShowUniqueSkillFailureMessage("Failure2");yield return null;ui.EndTutorialInstruction();
        yield return new WaitForSecondsRealtime(failureWait);
        Check(!text.gameObject.activeSelf,"Completed instruction is not resurrected");
        ui.BeginTutorialInstruction("Old");ui.ShowUniqueSkillFailureMessage("Failure3");yield return null;
        ui.BeginTutorialInstruction("New");yield return new WaitForSecondsRealtime(failureWait);
        Check(text.text=="New","New step supersedes old instruction during failure");ui.EndTutorialInstruction();
        ui.ShowUniqueSkillFailureMessage("Normal");yield return new WaitForSecondsRealtime(failureWait);
        Check(!text.gameObject.activeSelf,"Normal failure still auto closes");
        foreach(bool marker in new[]{false,true}) foreach(bool instruction in new[]{false,true})
        {
            var data=ScriptableObject.CreateInstance<EventSequenceData>();
            data.steps=new List<EventSequenceStepData>{new EventSequenceStepData{stepType=EventSequenceStepType.ForceButton,targetButton=EventSequenceButtonType.UniqueSkill,showMarker=marker,showTutorialInstruction=instruction,tutorialInstructionText="Button"}};
            controller.StartSequence(data);yield return new WaitForSecondsRealtime(0.2f);
            Check(Get<bool>(ui,"tutorialInstructionActive")==instruction,"Marker/instruction independent: "+marker+"/"+instruction);
            controller.NotifyButtonPressed(EventSequenceButtonType.Absorb);
            Check(Get<bool>(ui,"tutorialInstructionActive")==instruction,"Wrong button does not complete instruction");
            controller.NotifyButtonPressed(EventSequenceButtonType.UniqueSkill);
            Check(!Get<bool>(ui,"tutorialInstructionActive"),"Correct button immediately clears instruction");controller.StopSequence();
        }
        var piece=UnityEngine.Object.FindObjectsByType<Piece>(FindObjectsSortMode.None).First(p=>p.Team==PieceTeam.Player);
        var pieceData=ScriptableObject.CreateInstance<EventSequenceData>();
        pieceData.steps=new List<EventSequenceStepData>{new EventSequenceStepData{stepType=EventSequenceStepType.ForcePieceSelect,targetPieceTeam=piece.Team,targetPiecePosition=new Vector2Int(piece.X,piece.Y),showTutorialInstruction=true,tutorialInstructionText="Piece"}};
        controller.StartSequence(pieceData);yield return null;controller.NotifyPieceSelected(null);
        Check(Get<bool>(ui,"tutorialInstructionActive"),"Invalid piece keeps instruction");controller.NotifyPieceSelected(piece);
        Check(!Get<bool>(ui,"tutorialInstructionActive"),"Valid piece completes instruction");controller.StopSequence();
        var tile=UnityEngine.Object.FindObjectsByType<Tile>(FindObjectsSortMode.None).First();
        pieceData.steps[0]=new EventSequenceStepData{stepType=EventSequenceStepType.ForceTileSelect,targetTilePosition=new Vector2Int(tile.X,tile.Y),showTutorialInstruction=true,tutorialInstructionText="Tile"};
        controller.StartSequence(pieceData);yield return null;controller.NotifyTileSelected(null);
        Check(Get<bool>(ui,"tutorialInstructionActive"),"Invalid tile keeps instruction");controller.NotifyTileSelected(tile);
        Check(!Get<bool>(ui,"tutorialInstructionActive"),"Valid tile completes instruction");controller.StopSequence();
        // Use temporary in-memory entries: no shipped translation is rewritten.
        var tables=new List<StringTable>(); string key="instruction_runtime_"+Guid.NewGuid().ToString("N");
        foreach(string code in new[]{"ko","en","ja"})
        {
            var locale=LocalizationSettings.AvailableLocales.GetLocale(code);
            var op=LocalizationSettings.StringDatabase.GetTableAsync("Event_Dialogue",locale);yield return op;
            tables.Add(op.Result);op.Result.AddEntry(key,code+" instruction");
        }
        pieceData.steps[0].tutorialInstructionLocalizedText=new LocalizedString("Event_Dialogue",key);
        foreach(string code in new[]{"ko","en","ja"})
        {
            LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(code);
            if(!controller.IsSequenceActive)controller.StartSequence(pieceData);
            yield return new WaitForSecondsRealtime(0.3f);
            Check(text.text==code+" instruction","Live instruction locale: "+code);
        }
        tables[1].GetEntry(key).Value="";
        LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale("en");yield return new WaitForSecondsRealtime(0.3f);
        Check(text.text=="Tile","Missing translation uses Korean source");
        controller.StopSequence();LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale("ja");yield return new WaitForSecondsRealtime(0.3f);
        Check(!text.gameObject.activeSelf,"Stopped sequence unsubscribes locale updates");
        controller.StartSequence(pieceData);yield return null;controller.enabled=false;yield return null;
        Check(!Get<bool>(ui,"tutorialInstructionActive"),"Controller disable cleans instruction");controller.enabled=true;
        ui.BeginTutorialInstruction("Disabled");ui.enabled=false;yield return null;
        Check(!text.gameObject.activeSelf&&Get<Coroutine>(ui,"tutorialInstructionCoroutine")==null,"UI disable stops pulse and hides popup");ui.enabled=true;
        foreach(var table in tables)table.RemoveEntry(key);
    }
}
