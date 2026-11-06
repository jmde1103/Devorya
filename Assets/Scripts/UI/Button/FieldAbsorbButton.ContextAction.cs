using System;
using UnityEngine;
using UnityEngine.UI;

public partial class FieldAbsorbButton : UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    private Action contextAction;
    private Sprite contextBackground;
    private Vector2 contextIconSize;

    private Sprite contextNormalIcon, contextHoverIcon;
    private float contextNormalScale = 1f;
    private bool contextHovered;
    public void SetContextHoverIcons(Sprite normal, Sprite hover, float normalScale)
    {
        contextNormalIcon = normal; contextHoverIcon = hover; contextNormalScale = normalScale;
        RefreshContextHoverIcon();
    }
    private void RefreshContextHoverIcon()
    {
        if (contextNormalIcon != null)
            SetContextIcon(contextHovered && contextHoverIcon != null ? contextHoverIcon : contextNormalIcon,
                contextHovered && contextHoverIcon != null ? .82f : contextNormalScale);
    }
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData data) { contextHovered = true; RefreshContextHoverIcon(); }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData data) { contextHovered = false; RefreshContextHoverIcon(); }

    // Only called on the merchant clone. Pixel units come from the actual Piece icon sprite.
    public void ConfigureContextLayout(SpriteRenderer background, Sprite referenceIcon)
    {
        contextBackground = background.sprite;
        Vector2 size = contextBackground.rect.size;
        var root = (RectTransform)transform;
        root.sizeDelta = size;
        root.pivot = contextBackground.pivot / size;
        var buttonRect = (RectTransform)absorbButton.transform;
        buttonRect.anchorMin = buttonRect.anchorMax = root.pivot;
        buttonRect.pivot = root.pivot;
        buttonRect.anchoredPosition3D = Vector3.zero;
        buttonRect.sizeDelta = size;
        buttonRect.localScale = Vector3.one;
        buttonRect.localRotation = Quaternion.identity;
        absorbBackgroundImage.type = Image.Type.Simple;
        absorbBackgroundImage.preserveAspect = true;
        absorbBackgroundImage.raycastTarget = true;
        var iconRect = absorbIconImage.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition3D = Vector3.zero;
        iconRect.localScale = Vector3.one;
        iconRect.localRotation = Quaternion.identity;
        Vector2 iconSize = referenceIcon != null
            ? (Vector2)referenceIcon.bounds.size * contextBackground.pixelsPerUnit : size;
        contextIconSize = Vector2.Min(iconSize, size);
        iconRect.sizeDelta = contextIconSize;
        absorbIconImage.raycastTarget = false;
    }
    public void ShowContextAction(Action action, Sprite icon)
    {
        battleUIController = null;
        Show(null, null);
        contextAction = action;
        if (absorbTooltipTrigger != null) absorbTooltipTrigger.enabled = false;
        SetContextIcon(icon);
    }
    public void SetContextIcon(Sprite icon, float scale = 1f)
    {
        if (contextBackground != null && absorbBackgroundImage != null)
        {
            absorbBackgroundImage.sprite = contextBackground;
            absorbBackgroundImage.color = Color.white;
        }
        if (absorbIconImage == null || icon == null) return;
        absorbIconImage.sprite = icon; absorbIconImage.type = Image.Type.Simple;
        if (contextBackground != null) absorbIconImage.rectTransform.sizeDelta = contextIconSize * scale;
        absorbIconImage.preserveAspect = true;
        absorbIconImage.color = Color.white;
    }
    public void ClearContextAction() { contextHovered=false; contextNormalIcon=contextHoverIcon=null; contextAction = null; Hide(); }
}
