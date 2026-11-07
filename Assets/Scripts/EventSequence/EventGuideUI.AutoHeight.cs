using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class EventGuideUI
{
    [SerializeField, InspectorName("대화 내용에 맞춰 높이 자동 조절")]
    private bool autoDialogueHeight = true;
    private RectTransform dialogueHeightPanel;
    private RectTransform dialogueHeightFooter;
    private float dialogueMinimumHeight, dialogueFooterY;
    private Vector4 dialoguePadding;
    private string measuredDialogue;
    private float measuredDialogueWidth, measuredDialogueFontSize;
    private TMP_FontAsset measuredDialogueFont;

    private void LateUpdate()
    {
        if (isDialoguePlaying) RefreshDialogueHeight(false);
    }

    private void RefreshDialogueHeight(bool force)
    {
        if (!autoDialogueHeight || guideText == null) return;
        var textRect = guideText.rectTransform;
        if (dialogueHeightPanel == null)
        {
            // Existing scenes use a small text rect with large negative TMP margins.
            // Convert its effective writing area to normal insets without moving the panel.
            var background = guideText.GetComponentInParent<Image>();
            if (background == null || background.transform == textRect) return;
            dialogueHeightPanel = background.rectTransform;
            var panel = dialogueHeightPanel;
            dialogueMinimumHeight = panel.rect.height;
            Vector4 margin = guideText.margin;
            Vector3 low = panel.InverseTransformPoint(textRect.TransformPoint(
                new Vector3(textRect.rect.xMin + margin.x, textRect.rect.yMin + margin.w)));
            Vector3 high = panel.InverseTransformPoint(textRect.TransformPoint(
                new Vector3(textRect.rect.xMax - margin.z, textRect.rect.yMax - margin.y)));
            dialoguePadding = new Vector4(Mathf.Max(0, low.x - panel.rect.xMin),
                Mathf.Max(0, panel.rect.yMax - high.y), Mathf.Max(0, panel.rect.xMax - high.x),
                Mathf.Max(0, low.y - panel.rect.yMin));
            // Keep the existing arrow's parent and move it with the lower edge.
            if (continueArrowImage != null && continueArrowImage.transform.IsChildOf(panel))
            {
                Transform footer = continueArrowImage.transform;
                while (footer.parent != panel) footer = footer.parent;
                dialogueHeightFooter = footer as RectTransform;
                dialogueFooterY = dialogueHeightFooter.anchoredPosition.y;
            }
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(dialoguePadding.x, dialoguePadding.w);
            textRect.offsetMax = new Vector2(-dialoguePadding.z, -dialoguePadding.y);
            guideText.margin = Vector4.zero;
            guideText.textWrappingMode = TextWrappingModes.Normal;
            if (background.sprite != null && background.sprite.border != Vector4.zero)
                background.type = Image.Type.Sliced;
        }
        float width = Mathf.Max(1, textRect.rect.width);
        if (!force && measuredDialogue == guideText.text && Mathf.Approximately(width, measuredDialogueWidth)
            && measuredDialogueFont == guideText.font && Mathf.Approximately(measuredDialogueFontSize, guideText.fontSize)) return;
        measuredDialogue = guideText.text;
        measuredDialogueWidth = width;
        measuredDialogueFont = guideText.font;
        measuredDialogueFontSize = guideText.fontSize;
        // Measure the entire localized page, not just the characters currently typed.
        float contentHeight = guideText.GetPreferredValues(guideText.text, width, Mathf.Infinity).y;
        float height = Mathf.Max(dialogueMinimumHeight,
            Mathf.Ceil(contentHeight + dialoguePadding.y + dialoguePadding.w + 2));
        dialogueHeightPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        if (dialogueHeightFooter != null)
        {
            var position = dialogueHeightFooter.anchoredPosition;
            position.y = dialogueFooterY - (height - dialogueMinimumHeight) *
                ((dialogueHeightFooter.anchorMin.y + dialogueHeightFooter.anchorMax.y) * .5f);
            dialogueHeightFooter.anchoredPosition = position;
        }
    }
}
