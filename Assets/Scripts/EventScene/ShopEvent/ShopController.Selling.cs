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
    [SerializeField, Min(0.05f), InspectorName("판매 아이콘 이동 시간")] private float saleIconTravelTime=0.35f;
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
    private Image saleVisual;
    private RectTransform saleCanvas;
    private Vector2 saleVisualStart;
    private float saleVisualElapsed;
    public SaleState CurrentSaleState=>saleState;
    public bool IsSelling=>saleState!=SaleState.Normal;
    public Sprite CurrentSaleIcon=>IsSelling?sellCancelIcon:sellIcon;
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
        CreateSaleVisual(from);
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
        if(returnGold && goldViews!=null)foreach(var view in goldViews)if(view!=null)view.EndSalePreview(this);
        if(saleVisual!=null)Destroy(saleVisual.gameObject);saleVisual=null;
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
    private void CreateSaleVisual(RectTransform from)
    {
        if(from==null || merchant==null || saleItem==null)return;
        var canvas=from.GetComponentInParent<Canvas>();if(canvas==null)return;
        canvas=canvas.rootCanvas;saleCanvas=canvas.transform as RectTransform;
        var go=new GameObject("SaleItemPreview",typeof(RectTransform));go.transform.SetParent(saleCanvas,false);
        saleVisual=go.AddComponent<Image>();saleVisual.sprite=saleItem.iconSprite;saleVisual.preserveAspect=true;saleVisual.raycastTarget=false;
        saleVisual.rectTransform.sizeDelta=new Vector2(48,48);
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(saleCanvas,RectTransformUtility.WorldToScreenPoint(camera,from.position),camera,out saleVisualStart);
        saleVisual.rectTransform.anchoredPosition=saleVisualStart;saleVisualElapsed=0;
    }
    private void Update()
    {
        bool context=isActiveAndEnabled && (merchant!=null || hasInitializedShopInventory);
        if(context!=boundShopContext)BindInventoryForShop(context);
        if(!IsSelling)return;
        if(merchant==null || !merchant.isActiveAndEnabled || (saleState==SaleState.SellReady && !SelectionStillValid())) { CancelSelling();return; }
        if(purchaseButton!=null)purchaseButton.interactable=saleState==SaleState.SellReady && CanInteractWithSale;
        if(saleVisual==null || saleCanvas==null)return;
        var canvas=saleCanvas.GetComponent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var worldCamera=Camera.main;if(worldCamera==null)return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(saleCanvas,worldCamera.WorldToScreenPoint(merchant.IconAnchor.position),camera,out var target);
        saleVisualElapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(saleVisualElapsed/Mathf.Max(0.05f,saleIconTravelTime));
        saleVisual.rectTransform.anchoredPosition=Vector2.Lerp(saleVisualStart,target,Mathf.SmoothStep(0,1,t))+Vector2.up*Mathf.Sin(t*Mathf.PI)*30;
        saleVisual.color=new Color(1,1,1,t<1?1:InstructionPulse.Alpha(saleVisualElapsed-saleIconTravelTime));
    }
}
