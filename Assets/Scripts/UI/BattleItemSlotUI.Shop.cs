using UnityEngine;
public partial class BattleItemSlotUI
{
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
