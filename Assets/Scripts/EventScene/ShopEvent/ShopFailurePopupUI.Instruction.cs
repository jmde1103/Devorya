using System.Collections;
using UnityEngine;
public partial class ShopFailurePopupUI
{
    private object instructionOwner;
    private string instructionMessage;
    private Coroutine instructionCoroutine;
    public void ShowInstruction(object owner, string message)
    {
        if (owner == null) return;
        bool same = ReferenceEquals(owner, instructionOwner);
        instructionOwner=owner;instructionMessage=message;
        if (displayCoroutine!=null) return;
        if (same && instructionCoroutine!=null) { messageText.text=message;return; }
        ResumeInstruction();
    }
    public void EndInstruction(object owner)
    {
        if (!ReferenceEquals(owner,instructionOwner)) return;
        instructionOwner=null;PauseInstruction();
        if(displayCoroutine==null) { if(popupOpenAnimator!=null)popupOpenAnimator.CompleteImmediately();HideVisualImmediately(); }
    }
    private void PauseInstruction() { if(instructionCoroutine!=null)StopCoroutine(instructionCoroutine);instructionCoroutine=null; }
    private void ResumeInstruction()
    {
        if(instructionOwner==null || displayCoroutine!=null || !isActiveAndEnabled) return;
        CacheReferences();if(messageText==null || popupCanvasGroup==null)return;
        PauseInstruction();messageText.text=instructionMessage;messageText.gameObject.SetActive(true);
        if(popupOpenAnimator!=null)popupOpenAnimator.PlayOpen();
        popupCanvasGroup.blocksRaycasts=false;popupCanvasGroup.interactable=false;
        instructionCoroutine=StartCoroutine(PulseInstruction());
    }
    private IEnumerator PulseInstruction()
    {
        yield return null;
        float openWait=0;
        while(popupOpenAnimator!=null && popupOpenAnimator.IsPlaying && openWait<maximumOpenWaitDuration)
        { openWait+=Time.unscaledDeltaTime;popupCanvasGroup.blocksRaycasts=false;popupCanvasGroup.interactable=false;yield return null; }
        if(popupOpenAnimator!=null && popupOpenAnimator.IsPlaying)popupOpenAnimator.CompleteImmediately();
        float elapsed=0;
        while(instructionOwner!=null && messageText.gameObject.activeInHierarchy)
        {
            popupCanvasGroup.blocksRaycasts=false;popupCanvasGroup.interactable=false;
            popupCanvasGroup.alpha=InstructionPulse.Alpha(elapsed);elapsed+=Time.unscaledDeltaTime;yield return null;
        }
        instructionCoroutine=null;instructionOwner=null;
    }
}
