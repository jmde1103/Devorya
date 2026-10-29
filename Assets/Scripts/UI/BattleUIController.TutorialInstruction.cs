using System.Collections;
using UnityEngine;

public partial class BattleUIController
{
    [Header("튜토리얼 안내 점멸")]
    [SerializeField, Range(0.1f, 1f), InspectorName("최소 알파")]
    private float tutorialInstructionMinAlpha = 0.5f;
    [SerializeField, Range(0.1f, 1f), InspectorName("최대 알파")]
    private float tutorialInstructionMaxAlpha = 1f;
    [SerializeField, Min(0.05f), InspectorName("편도 점멸 시간")]
    private float tutorialInstructionPulseDuration = 1.2f;
    private bool tutorialInstructionActive;
    private string tutorialInstructionMessage;
    private Coroutine tutorialInstructionCoroutine;
    private int tutorialInstructionGeneration;

    public void BeginTutorialInstruction(string message)
    {
        EndTutorialInstruction();
        tutorialInstructionActive = true;
        tutorialInstructionMessage = message ?? string.Empty;
        ResumeTutorialInstruction();
    }

    public void UpdateTutorialInstruction(string message)
    {
        if (!tutorialInstructionActive) return;
        tutorialInstructionMessage = message ?? string.Empty;
        if (uniqueSkillFailureMessageCoroutine != null) return;
        if (tutorialInstructionCoroutine == null) ResumeTutorialInstruction();
        else if (uniqueSkillFailureText != null) uniqueSkillFailureText.text = tutorialInstructionMessage;
    }

    public void EndTutorialInstruction()
    {
        bool wasActive = tutorialInstructionActive;
        tutorialInstructionActive = false;
        tutorialInstructionMessage = null;
        PauseTutorialInstruction();
        // A failure owns the popup until its original hold/fade completes.
        if (wasActive && uniqueSkillFailureMessageCoroutine == null)
        {
            if (uniqueSkillFailurePopupOpenAnimator != null)
                uniqueSkillFailurePopupOpenAnimator.CompleteImmediately();
            HideUniqueSkillFailureMessageImmediately();
        }
    }

    private void PauseTutorialInstruction()
    {
        tutorialInstructionGeneration++;
        if (tutorialInstructionCoroutine != null) StopCoroutine(tutorialInstructionCoroutine);
        tutorialInstructionCoroutine = null;
    }

    private void ResumeTutorialInstruction()
    {
        if (!tutorialInstructionActive || uniqueSkillFailureMessageCoroutine != null ||
            !isActiveAndEnabled || uniqueSkillFailureText == null ||
            string.IsNullOrWhiteSpace(tutorialInstructionMessage)) return;
        PauseTutorialInstruction();
        if (uniqueSkillFailureCanvasGroup == null)
            uniqueSkillFailureCanvasGroup = uniqueSkillFailureText.GetComponent<CanvasGroup>() ??
                uniqueSkillFailureText.gameObject.AddComponent<CanvasGroup>();
        uniqueSkillFailureText.text = tutorialInstructionMessage;
        uniqueSkillFailureText.gameObject.SetActive(true);
        uniqueSkillFailureCanvasGroup.alpha = 1f;
        uniqueSkillFailureCanvasGroup.interactable = false;
        uniqueSkillFailureCanvasGroup.blocksRaycasts = false;
        AutoBindUniqueSkillFailurePopupAnimator();
        if (uniqueSkillFailurePopupOpenAnimator != null) uniqueSkillFailurePopupOpenAnimator.PlayOpen();
        KeepTutorialInstructionNonBlocking();
        tutorialInstructionCoroutine = StartCoroutine(PulseTutorialInstruction(tutorialInstructionGeneration));
    }

    private void KeepTutorialInstructionNonBlocking()
    {
        if (!tutorialInstructionActive || uniqueSkillFailureMessageCoroutine != null || uniqueSkillFailureCanvasGroup == null) return;
        // PopupOpenAnimator can apply its own raycast settings during and after opening.
        uniqueSkillFailureCanvasGroup.interactable = false;
        uniqueSkillFailureCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator PulseTutorialInstruction(int generation)
    {
        // The existing open animation owns alpha first; never replay it on a pulse.
        yield return null;
        while (uniqueSkillFailurePopupOpenAnimator != null && uniqueSkillFailurePopupOpenAnimator.IsPlaying)
            yield return null;
        float elapsed = 0f;
        while (tutorialInstructionActive && generation == tutorialInstructionGeneration &&
            uniqueSkillFailureText != null && uniqueSkillFailureText.gameObject.activeInHierarchy)
        {
            float max = Mathf.Clamp(tutorialInstructionMaxAlpha, 0.1f, 1f);
            float min = Mathf.Clamp(tutorialInstructionMinAlpha, 0.1f, max);
            float t = Mathf.PingPong(elapsed / Mathf.Max(0.05f, tutorialInstructionPulseDuration), 1f);
            uniqueSkillFailureCanvasGroup.alpha = Mathf.Lerp(max, min, Mathf.SmoothStep(0f, 1f, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (generation == tutorialInstructionGeneration)
        {
            tutorialInstructionCoroutine = null;
            EndTutorialInstruction();
        }
    }
}
