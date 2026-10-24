using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    [SerializeField, InspectorName("아이템 대상 확인 색상")]
    private Color itemConfirmColor = new Color(.2f, 1f, .4f, .8f);
    [SerializeField, Min(0), InspectorName("아이템 대상 확인 강도")]
    private float itemConfirmIntensity = 1f;
    private int targetItemSlot = -1;
    private BattleItemData targetItem;
    private int targetInventoryVersion;
    private Piece confirmedItemPiece;
    private Tile confirmedItemTile;
    private readonly List<Tile> itemTargetTiles = new List<Tile>();
    private bool executingItem;
    private int lastItemInputFrame = -1;
    public bool IsItemTargeting => targetItemSlot >= 0;
    public int SelectedItemSlot => targetItemSlot;
    public bool IsSelectedItem(int index, BattleItemData item) => IsItemTargeting && targetItemSlot == index && targetItem == item;

    public BattleItemFailure GetItemInputFailure()
    {
        if (!isActiveAndEnabled || isBattleEnded || currentTurn != BattleTurn.Player)
            return BattleItemFailure.WrongTurn;
        if (isPlayerDeploymentPhase || isBattleAnnouncementPlaying || isActionAnimating ||
            (eventSequenceController != null && (eventSequenceController.IsDialogueBlockingBattleInput ||
             eventSequenceController.IsForcedBattleInputActive)))
            return BattleItemFailure.Busy;
        return BattleItemFailure.None;
    }

    private void ShowItemFailure(BattleItemFailure failure)
    {
        if (battleUIController != null && failure != BattleItemFailure.None)
            battleUIController.ShowUniqueSkillFailureMessage(BattleItemFailureText.Get(failure));
    }

    public void BeginItemTargeting(int index)
    {
        if (executingItem) return;
        if (IsItemTargeting && targetItemSlot == index) { CancelItemTargeting(); return; }
        CancelItemTargeting();
        var failure = GetItemInputFailure();
        if (failure != BattleItemFailure.None) { ShowItemFailure(failure); return; }
        var item = battleItemManager != null ? battleItemManager.GetItemAtSlot(index) : null;
        if (item == null) { ShowItemFailure(BattleItemFailure.MissingItem); return; }
        SelectPiece(null);
        if (battleUIController != null)
        {
            battleUIController.ClearPlayerStatusOnly();
            battleUIController.ClearEnemyStatus();
            battleUIController.SetAbsorbModeIcon(false);
        }
        isAbsorbMode = false;
        targetItemSlot = index;
        targetItem = item;
        targetInventoryVersion = battleItemManager.InventoryVersion;
        if (!RefreshItemTargets()) { CancelItemTargeting(); ShowItemFailure(BattleItemFailure.NoCandidates); return; }
        if (battleUIController != null)
        {
            battleUIController.SetItemTargetSlot(index);
            battleUIController.ShowUniqueSkillFailureMessage(
                new UnityEngine.Localization.LocalizedString("Battle_UI", "battle.ui.item.select_target").GetLocalizedString());
        }
    }

    private void ClearItemTiles()
    {
        foreach (var tile in itemTargetTiles) if (tile != null) tile.HideHighlight();
        itemTargetTiles.Clear();
        confirmedItemPiece = null;
        confirmedItemTile = null;
    }

    public void CancelItemTargeting()
    {
        ClearItemTiles();
        targetItemSlot = -1;
        targetItem = null;
        if (battleUIController != null) battleUIController.SetItemTargetSlot(-1);
    }

    private bool RefreshItemTargets()
    {
        ClearItemTiles();
        if (boardManager == null || pieceManager == null || battleItemEffectHandler == null || targetItem == null) return false;
        for (int x = 0; x < boardManager.Width; x++)
        for (int y = 0; y < boardManager.Height; y++)
        {
            var piece = pieceManager.GetPieceAt(x, y);
            if (battleItemEffectHandler.ValidateItemEffect(targetItem, piece) != BattleItemFailure.None) continue;
            var tile = boardManager.GetTile(x, y);
            if (tile == null) continue;
            itemTargetTiles.Add(tile);
            tile.ShowHighlight(); // Same overlay, fade, glow and color as deployment.
        }
        return itemTargetTiles.Count > 0;
    }

    private bool ItemSnapshotValid() => battleItemManager != null &&
        battleItemManager.InventoryVersion == targetInventoryVersion &&
        battleItemManager.GetItemAtSlot(targetItemSlot) == targetItem && battleItemManager.MatchesRunInventory();

    private void UpdateItemTargeting()
    {
        if (!IsItemTargeting || executingItem) return;
        if (GetItemInputFailure() != BattleItemFailure.None ||
            Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        { CancelItemTargeting(); return; }
        if (!ItemSnapshotValid())
        { CancelItemTargeting(); ShowItemFailure(BattleItemFailure.MissingItem); return; }
        if (confirmedItemTile != null && (confirmedItemPiece == null || !confirmedItemPiece.gameObject.activeInHierarchy ||
            pieceManager.GetPieceAt(confirmedItemTile.X, confirmedItemTile.Y) != confirmedItemPiece))
        {
            if (!RefreshItemTargets()) CancelItemTargeting();
            ShowItemFailure(BattleItemFailure.MissingTarget);
        }
    }

    public void HandleItemTargetClick(Tile tile)
    {
        if (!IsItemTargeting || executingItem || lastItemInputFrame == Time.frameCount) return;
        lastItemInputFrame = Time.frameCount;
        var failure = GetItemInputFailure();
        if (failure != BattleItemFailure.None) { CancelItemTargeting(); ShowItemFailure(failure); return; }
        if (!ItemSnapshotValid())
        { CancelItemTargeting(); ShowItemFailure(BattleItemFailure.MissingItem); return; }
        if (confirmedItemTile != null && (confirmedItemPiece == null ||
            !confirmedItemPiece.gameObject.activeInHierarchy ||
            pieceManager.GetPieceAt(confirmedItemTile.X, confirmedItemTile.Y) != confirmedItemPiece))
        {
            if (!RefreshItemTargets()) CancelItemTargeting();
            ShowItemFailure(BattleItemFailure.MissingTarget);
            return;
        }
        if (tile == null) return;
        var piece = pieceManager.GetPieceAt(tile.X, tile.Y);
        if (piece == null) return;
        failure = battleItemEffectHandler.ValidateItemEffect(targetItem, piece);
        if (failure != BattleItemFailure.None)
        {
            if (!RefreshItemTargets()) CancelItemTargeting();
            ShowItemFailure(failure);
            return;
        }
        if (confirmedItemPiece != piece || confirmedItemTile != tile)
        {
            if (confirmedItemTile != null) confirmedItemTile.ShowHighlight();
            confirmedItemPiece = piece;
            confirmedItemTile = tile;
            if (!itemTargetTiles.Contains(tile)) itemTargetTiles.Add(tile);
            tile.ShowItemConfirmHighlight(itemConfirmColor, itemConfirmIntensity);
            return;
        }
        executingItem = true;
        try
        {
            // Clear overlays before ExtraMove can create its normal movement highlights.
            ClearItemTiles();
            failure = battleItemManager.TryUseItemAtSlot(targetItemSlot, targetItem, piece);
            if (failure == BattleItemFailure.None)
            {
                CancelItemTargeting();
                if (battleUIController != null) battleUIController.RefreshSelectedPieceButtons(selectedPiece);
            }
            else
            {
                if (failure == BattleItemFailure.MissingItem || !RefreshItemTargets()) CancelItemTargeting();
                ShowItemFailure(failure);
            }
        }
        finally { executingItem = false; }
    }

    public BattleItemFailure TryApplyBattleItemEffect(BattleItemData item, Piece target)
    {
        var failure = GetItemInputFailure();
        if (failure != BattleItemFailure.None) return failure;
        if (battleItemEffectHandler == null) return BattleItemFailure.Requirements;
        return battleItemEffectHandler.TryApplyItemEffect(item, target, out failure) ? BattleItemFailure.None : failure;
    }

    private void OnDisable() { CancelItemTargeting(); }
}
