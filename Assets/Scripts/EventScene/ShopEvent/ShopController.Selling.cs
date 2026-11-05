using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public partial class ShopController
{
    public enum SaleState { [InspectorName("구매")] Normal, [InspectorName("판매 선택 중")] SellSelecting, [InspectorName("판매 준비")] SellReady }
    [Header("아이템 판매")]
    [SerializeField, InspectorName("기존 기물 판매 버튼 원본")] private FieldAbsorbButton merchantFieldButtonTemplate;
    [SerializeField, InspectorName("판매 시작 아이콘")] private Sprite sellIcon;
    [SerializeField, InspectorName("판매 취소 아이콘")] private Sprite sellCancelIcon;
    [HideInInspector, SerializeField, Min(0.05f), InspectorName("판매 아이콘 이동 시간 (이전 데이터)")] private float saleIconTravelTime=0.35f;
    private SaleState saleState;
    private EventSceneSequenceController saleSequence;
    private ShopMerchant merchant;
    private BattleItemData saleItem;
    private int saleSlot=-1;
    private List<BattleItemData> saleInventorySnapshot;
    private bool committingSale, boundShopContext;
    private TMP_Text purchaseLabel;
    private LocalizeStringEvent purchaseLabelLocalization;
    private bool originalLabelLocalizationEnabled;
    private string originalPurchaseLabel;
    private RunGoldUI[] goldViews;
    public SaleState CurrentSaleState=>saleState;
    public bool IsSelling=>saleState!=SaleState.Normal;
    public bool HasSelectedSaleItem => IsSelling && saleItem != null;
    public Sprite SaleCancelIcon => sellCancelIcon;
    public Sprite CurrentSaleIcon=>IsSelling?(saleItem != null && saleItem.iconSprite != null ? saleItem.iconSprite : sellCancelIcon):sellIcon;
    public bool CanInteractWithSale
    {
        get
        {
            if(!isActiveAndEnabled || isExitingShop)return false;
            return saleSequence==null || !saleSequence.IsPlayerInteractionLocked;
        }
    }
    private void Start() { InitializeSaleUI(); }
    private void InitializeSaleUI()
    {
        if(saleSequence==null)saleSequence=FindFirstObjectByType<EventSceneSequenceController>();
        if(runItemBarUI==null)runItemBarUI=FindFirstObjectByType<RunItemBarUI>();
        if(shopFailurePopupUI==null)shopFailurePopupUI=FindFirstObjectByType<ShopFailurePopupUI>(FindObjectsInactive.Include);
        goldViews=FindObjectsByType<RunGoldUI>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        if(purchaseButton!=null && purchaseLabel==null)
        {
            purchaseLabel=purchaseButton.GetComponentInChildren<TMP_Text>(true);
            if(purchaseLabel!=null)purchaseLabelLocalization=purchaseLabel.GetComponent<LocalizeStringEvent>();
        }
    }
    public void RegisterMerchant(EventSceneActor actor)
    {
        if(actor==null)return;
        if (merchant != null && merchant.gameObject == actor.gameObject)
        {
            merchant.Initialize(this, merchantFieldButtonTemplate);
            return;
        }
        CancelSelling();InitializeSaleUI();
        merchant=actor.GetComponent<ShopMerchant>()??actor.gameObject.AddComponent<ShopMerchant>();
        merchant.Initialize(this,merchantFieldButtonTemplate);BindInventoryForShop(true);
    }
    public void MerchantUnavailable(ShopMerchant value) { if(merchant==value) { CancelSelling();merchant=null;BindInventoryForShop(false); } }
    private void BindInventoryForShop(bool enable)
    {
        if(runItemBarUI!=null)runItemBarUI.SetShopContext(enable?this:null);
        boundShopContext=enable;
    }
    public void BeginSelling()
    {
        if(!CanInteractWithSale || merchant==null || IsSelling)return;
        ClearSelection();InitializeSaleUI();BindInventoryForShop(true);
        if(purchaseLabel!=null)originalPurchaseLabel=purchaseLabel.text;
        if(purchaseLabelLocalization!=null) { originalLabelLocalizationEnabled=purchaseLabelLocalization.enabled;purchaseLabelLocalization.enabled=false; }
        saleState=SaleState.SellSelecting;RefreshSaleLanguage();merchant.RefreshButton();
    }
    public void SelectSaleItem(int index,BattleItemData item,RectTransform from)
    {
        if(!IsSelling || !CanInteractWithSale || committingSale)return;
        var run=RunStateManager.Instance;var items=run!=null?run.GetBattleItemsCopy():null;
        if(items==null || index<0 || index>=items.Count || items[index]!=item || item==null || item.SellPrice<=0)
        { if(shopFailurePopupUI!=null)shopFailurePopupUI.Show(ShopSaleLocalization.Unavailable);return; }
        ClearSaleTarget(false);saleItem=item;saleSlot=index;saleInventorySnapshot=items;saleState=SaleState.SellReady;
        if(runItemBarUI!=null)runItemBarUI.SetSaleSelection(index);
        if(merchant!=null)merchant.RefreshButton();
        if(goldViews!=null)foreach(var view in goldViews)if(view!=null)view.ShowSalePreview(this,item.SellPrice);
        RefreshSellButton();
    }
    private bool SelectionStillValid()
    {
        if(saleItem==null || saleItem.SellPrice<=0 || RunStateManager.Instance==null || saleInventorySnapshot==null)return false;
        return RunStateManager.Instance.MatchesBattleItemSnapshot(saleInventorySnapshot) && saleSlot >= 0 && saleSlot < saleInventorySnapshot.Count && saleInventorySnapshot[saleSlot] == saleItem;
    }
    public bool ConfirmSale()
    {
        if(saleState!=SaleState.SellReady || committingSale || !CanInteractWithSale)return false;
        if(!SelectionStillValid()) { CancelSelling();return false; }
        committingSale=true;
        if(goldViews!=null)foreach(var view in goldViews)if(view!=null)view.EndSalePreview(this,true);
        bool sold=RunStateManager.Instance.TrySellBattleItem(saleSlot,saleItem);
        ClearSaleTarget(false);saleState=SaleState.SellSelecting;committingSale=false;
        if(goldViews!=null)foreach(var view in goldViews)if(view!=null)view.RefreshImmediately();
        if(runItemBarUI!=null)runItemBarUI.RefreshFromRunState();
        RefreshSaleLanguage();
        if(merchant!=null)merchant.RefreshButton();
        if(!sold && shopFailurePopupUI!=null)shopFailurePopupUI.Show(ShopSaleLocalization.Unavailable);
        return sold;
    }
    public void CancelSelling()
    {
        bool wasSelling=IsSelling;
        ClearSaleTarget(true);saleState=SaleState.Normal;
        if(shopFailurePopupUI!=null)shopFailurePopupUI.EndInstruction(this);
        if(wasSelling && purchaseLabel!=null)purchaseLabel.text=originalPurchaseLabel;
        if(wasSelling && purchaseLabelLocalization!=null) { purchaseLabelLocalization.enabled=originalLabelLocalizationEnabled;if(originalLabelLocalizationEnabled)purchaseLabelLocalization.StringReference.RefreshString(); }
        if(merchant!=null)merchant.RefreshButton();
        if(wasSelling)UpdatePurchaseButtonState();
    }
    private void ClearSaleTarget(bool returnGold)
    {
        if(runItemBarUI!=null)runItemBarUI.SetSaleSelection(-1);
        if(returnGold && goldViews!=null)foreach(var view in goldViews)if(view!=null)view.EndSalePreview(this);
        saleItem=null;saleSlot=-1;saleInventorySnapshot=null;
    }
    private void RefreshSaleLanguage()
    {
        if(!IsSelling)return;
        if(shopFailurePopupUI!=null)shopFailurePopupUI.ShowInstruction(this,ShopSaleLocalization.Instruction);
        RefreshSellButton();
    }
    private void RefreshSellButton()
    {
        if(purchaseButton==null)return;
        purchaseButton.gameObject.SetActive(true);purchaseButton.interactable=saleState==SaleState.SellReady && CanInteractWithSale;
        if(purchaseLabel!=null)purchaseLabel.text=ShopSaleLocalization.Sell;
    }
    private void Update()
    {
        bool context=isActiveAndEnabled && (merchant!=null || hasInitializedShopInventory);
        if(context!=boundShopContext)BindInventoryForShop(context);
        if(runItemBarUI!=null)runItemBarUI.SetSaleSelection(IsSelling?saleSlot:-1);
        if(!IsSelling)return;
        if(merchant==null || !merchant.isActiveAndEnabled || (saleState==SaleState.SellReady && !SelectionStillValid())) { CancelSelling();return; }
        if(purchaseButton!=null)purchaseButton.interactable=saleState==SaleState.SellReady && CanInteractWithSale;
    }
}
