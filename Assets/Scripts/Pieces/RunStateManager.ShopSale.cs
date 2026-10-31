public partial class RunStateManager
{
    public bool MatchesBattleItemSnapshot(System.Collections.Generic.IReadOnlyList<BattleItemData> snapshot)
    {
        if (snapshot == null || snapshot.Count != battleItemDataList.Count) return false;
        for (int i = 0; i < snapshot.Count; i++) if (snapshot[i] != battleItemDataList[i]) return false;
        return true;
    }
    // Commit both changes together; never grant currency for a stale inventory slot.
    public bool TrySellBattleItem(int index, BattleItemData expectedItem)
    {
        if (expectedItem == null || expectedItem.SellPrice <= 0 || index < 0 ||
            index >= battleItemDataList.Count || battleItemDataList[index] != expectedItem ||
            (long)goldAmount + expectedItem.SellPrice > int.MaxValue) return false;
        if (!RemoveBattleItemAt(index)) return false;
        AddGold(expectedItem.SellPrice);
        return true;
    }
}
