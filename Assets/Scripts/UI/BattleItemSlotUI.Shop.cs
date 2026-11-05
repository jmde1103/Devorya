using UnityEngine;
public partial class BattleItemSlotUI
{
    public void SetSaleSelected(bool selected)
    {
        if (itemIconImage == null) return;
        // The slot root is a transparent click surface: outline the visible item sprite instead.
        var feedback = itemIconImage.GetComponent<SlotHoverFeedback>();
        if (feedback == null && selected) feedback = itemIconImage.gameObject.AddComponent<SlotHoverFeedback>();
        if (feedback != null)
        {
            var outline = itemIconImage.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null) outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        if (feedback != null) feedback.SetSelected(selected && shopOwner != null && currentItemData != null,
            Color.white, 2f, 1f);
    }
    private ShopController shopOwner;
    private int shopSlotIndex;
    public void SetShopContext(ShopController owner, int index)
    {
        if (!isReadOnlyMode) return;
        shopOwner = owner; shopSlotIndex = index;
        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(OnClickSlot);
            if (owner != null) slotButton.onClick.AddListener(OnClickSlot);
        }
        RefreshTooltip();
    }
}
