using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class EventSequenceController
{
    private BattleUIController tutorialInstructionUI;
    private EventSequenceStepData tutorialInstructionStep;
    private Coroutine tutorialInstructionTranslation;
    private int tutorialInstructionRequest;

    private void BeginForceTutorialInstruction(EventSequenceStepData step)
    {
        EndForceTutorialInstruction();
        if (step == null || !step.IsForceActionStep || !step.showTutorialInstruction) return;
        if (tutorialInstructionUI == null)
            tutorialInstructionUI = FindFirstObjectByType<BattleUIController>(FindObjectsInactive.Include);
        if (tutorialInstructionUI == null) return;
        tutorialInstructionStep = step;
        tutorialInstructionUI.BeginTutorialInstruction(step.tutorialInstructionText);
        LocalizationSettings.SelectedLocaleChanged += OnTutorialInstructionLocaleChanged;
        OnTutorialInstructionLocaleChanged(LocalizationSettings.SelectedLocale);
    }

    private void OnTutorialInstructionLocaleChanged(Locale locale)
    {
        int request = ++tutorialInstructionRequest;
        if (tutorialInstructionTranslation != null) StopCoroutine(tutorialInstructionTranslation);
        tutorialInstructionTranslation = StartCoroutine(ResolveTutorialInstruction(request, locale));
    }

    private IEnumerator ResolveTutorialInstruction(int request, Locale locale)
    {
        // Explicitly request the current table entry, avoiding missing-entry diagnostic strings.
        yield return null;
        var step = tutorialInstructionStep;
        if (step == null) yield break;
        string text = step.tutorialInstructionText ?? string.Empty;
        var reference = step.tutorialInstructionLocalizedText;
        if (reference != null && !reference.IsEmpty && locale != null)
        {
            var operation = LocalizationSettings.StringDatabase.GetTableEntryAsync(
                reference.TableReference, reference.TableEntryReference, locale, FallbackBehavior.DontUseFallback);
            yield return operation;
            if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result.Entry != null)
            {
                string translated = operation.Result.Entry.LocalizedValue;
                if (!string.IsNullOrWhiteSpace(translated)) text = translated;
            }
        }
        if (request != tutorialInstructionRequest || step != tutorialInstructionStep) yield break;
        if (tutorialInstructionUI != null) tutorialInstructionUI.UpdateTutorialInstruction(text);
        tutorialInstructionTranslation = null;
    }

    private void EndForceTutorialInstruction()
    {
        tutorialInstructionRequest++;
        LocalizationSettings.SelectedLocaleChanged -= OnTutorialInstructionLocaleChanged;
        if (tutorialInstructionTranslation != null) StopCoroutine(tutorialInstructionTranslation);
        tutorialInstructionTranslation = null;
        tutorialInstructionStep = null;
        if (tutorialInstructionUI != null) tutorialInstructionUI.EndTutorialInstruction();
    }
}
