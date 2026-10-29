using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;

public partial class EventSequenceDataEditor
{
    private static string GetTutorialInstructionKey(EventSequenceData sequence, EventSequenceStepData step)
    {
        if (sequence == null || step == null || string.IsNullOrWhiteSpace(sequence.localizationId) ||
            string.IsNullOrWhiteSpace(step.tutorialInstructionLocalizationId)) return string.Empty;
        return $"event.{sequence.localizationId}.instruction.{step.tutorialInstructionLocalizationId}";
    }

    private static bool HasAnyTutorialInstruction(EventSequenceData sequence)
    {
        return sequence.steps != null && sequence.steps.Exists(step =>
            step != null && step.IsForceActionStep && step.showTutorialInstruction);
    }

    private void EnsureTutorialInstructionMetadata(EventSequenceData sequence, EventSequenceStepData step, HashSet<string> usedIds)
    {
        if (!step.IsForceActionStep || (!step.showTutorialInstruction && string.IsNullOrEmpty(step.tutorialInstructionLocalizationId))) return;
        string oldKey = GetTutorialInstructionKey(sequence, step);
        if (string.IsNullOrEmpty(step.tutorialInstructionLocalizationId) || !usedIds.Add(step.tutorialInstructionLocalizationId))
        {
            step.tutorialInstructionLocalizationId = CreateStableId("instruction");
            usedIds.Add(step.tutorialInstructionLocalizationId);
            var collection = DevoryaLocalizationEditorUtility.GetStringTableCollection(TableCollectionName);
            string newKey = GetTutorialInstructionKey(sequence, step);
            if (collection != null && !string.IsNullOrEmpty(oldKey))
            {
                CopyEventSequenceTranslationValues(collection, oldKey, newKey, step.tutorialInstructionText);
                step.tutorialInstructionLocalizedText = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, newKey);
            }
            else step.tutorialInstructionLocalizedText = new LocalizedString();
        }
        if (step.tutorialInstructionLocalizedText == null) step.tutorialInstructionLocalizedText = new LocalizedString();
    }

    private void ProcessTutorialInstructionSettings(ref float y, Rect rect, SerializedProperty step, bool draw)
    {
        y += 5f;
        ProcessEventStepSectionLabel(ref y, rect, "튜토리얼 안내", draw);
        ProcessEventStepProperty(ref y, rect, step, "showTutorialInstruction", "튜토리얼 안내 표시", false, draw);
        if (!step.FindPropertyRelative("showTutorialInstruction").boolValue) return;
        var id = step.FindPropertyRelative("tutorialInstructionLocalizationId");
        if (draw && string.IsNullOrEmpty(id.stringValue)) id.stringValue = CreateStableId("instruction");
        ProcessEventStepProperty(ref y, rect, step, "tutorialInstructionText", "한국어", false, draw);
        var sequence = (EventSequenceData)target;
        string key = string.IsNullOrEmpty(sequence.localizationId) || string.IsNullOrEmpty(id.stringValue)
            ? string.Empty : $"event.{sequence.localizationId}.instruction.{id.stringValue}";
        if (draw)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.TextField(new Rect(rect.x, y, rect.width, 20), "고정 Localization Key", key);
        }
        y += 25f;
        var collection = DevoryaLocalizationEditorUtility.GetStringTableCollection(TableCollectionName);
        foreach (string code in new[] { "en", "ja" })
        {
            if (draw)
            {
                var table = collection == null ? null : DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection, code);
                EditorGUI.LabelField(new Rect(rect.x, y, rect.width, 20), code == "en" ? "영어" : "일본어");
                using (new EditorGUI.DisabledScope(table == null || string.IsNullOrEmpty(key)))
                {
                    string text = table == null || string.IsNullOrEmpty(key) ? string.Empty : DevoryaLocalizationEditorUtility.GetTableValue(table, key);
                    EditorGUI.BeginChangeCheck();
                    string updated = EditorGUI.TextArea(new Rect(rect.x, y + 20, rect.width, 55), text);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(table, "Edit Tutorial Instruction Translation");
                        Undo.RecordObject(collection.SharedData, "Edit Tutorial Instruction Key");
                        DevoryaLocalizationEditorUtility.SetTableValue(table, key, updated);
                        EditorUtility.SetDirty(collection.SharedData);
                    }
                }
            }
            y += 80f;
        }
        ProcessEventStepHelpBox(ref y, rect,
            "아래 Localization 생성 / 한국어 동기화로 연결합니다. 비어 있는 번역은 한국어로 표시합니다.", MessageType.Info, 2, draw);
    }
}
