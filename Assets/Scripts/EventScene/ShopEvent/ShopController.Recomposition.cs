using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public partial class ShopController
{
    private EventSceneRecompositionCandidateController recompositionButtonOwner;
    private LocalizedString recompositionPreviousLabel;
    private bool recompositionPreviousActive, recompositionPreviousInteractable, recompositionPreviousLocalizationEnabled;
    private string recompositionPreviousText;
    private RecompositionTextSizing recompositionButtonSizing;

    public bool AcquireRecompositionButton(EventSceneRecompositionCandidateController owner)
    {
        if (owner == null || purchaseButton == null || IsSelling) return false;
        if (recompositionButtonOwner != null) return recompositionButtonOwner == owner;
        // Do not discard unknown Inspector callbacks while borrowing this button.
        if (purchaseButton.onClick.GetPersistentEventCount() != 0)
        {
            Debug.LogWarning("재구성: 구매 버튼의 Inspector 이벤트를 확인하세요. 버튼 재사용을 중단합니다.", this);
            return false;
        }
        InitializeSaleUI();
        if (purchaseLabelLocalization == null) return false;
        recompositionPreviousActive = purchaseButton.gameObject.activeSelf;
        recompositionPreviousInteractable = purchaseButton.interactable;
        recompositionPreviousText = purchaseLabel.text;
        recompositionPreviousLabel = purchaseLabelLocalization.StringReference;
        recompositionPreviousLocalizationEnabled = purchaseLabelLocalization.enabled;
        recompositionButtonOwner = owner;
        recompositionButtonSizing = new RecompositionTextSizing(purchaseLabel);
        recompositionButtonSizing.Apply(true);
        purchaseLabelLocalization.StringReference = new LocalizedString("Event_UI", "event.ui.recomposition");
        purchaseLabelLocalization.enabled = true;
        purchaseLabelLocalization.RefreshString();
        return true;
    }

    public void SetRecompositionButtonState(EventSceneRecompositionCandidateController owner, bool visible, bool enabled)
    {
        if (recompositionButtonOwner != owner || purchaseButton == null) return;
        bool opening = visible && !purchaseButton.gameObject.activeSelf;
        purchaseButton.gameObject.SetActive(visible);
        purchaseButton.interactable = visible && enabled;
        if (opening)
        {
            var animator = purchaseButton.GetComponent<PopupOpenAnimator>();
            if (animator != null && animator.isActiveAndEnabled) animator.PlayOpen();
        }
    }

    public void ReleaseRecompositionButton(EventSceneRecompositionCandidateController owner)
    {
        if (recompositionButtonOwner != owner) return;
        recompositionButtonOwner = null;
        recompositionButtonSizing?.Restore();
        recompositionButtonSizing = null;
        if (purchaseLabelLocalization != null)
        {
            purchaseLabelLocalization.StringReference = recompositionPreviousLabel;
            purchaseLabelLocalization.enabled = recompositionPreviousLocalizationEnabled;
            if (recompositionPreviousLocalizationEnabled) purchaseLabelLocalization.RefreshString();
            else if (purchaseLabel != null) purchaseLabel.text = recompositionPreviousText;
        }
        if (purchaseButton != null)
        {
            purchaseButton.gameObject.SetActive(recompositionPreviousActive);
            purchaseButton.interactable = recompositionPreviousInteractable;
        }
    }
}
