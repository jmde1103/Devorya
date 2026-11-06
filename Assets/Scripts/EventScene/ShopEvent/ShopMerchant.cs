using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// An EventSceneActor only: never register a Piece with battle or AI managers.
[RequireComponent(typeof(BoxCollider2D))]
public class ShopMerchant : MonoBehaviour, IPointerClickHandler
{
    private ShopController shop;
    private FieldAbsorbButton fieldButton;
    private Transform iconAnchor;
    private EventSceneStepData settings;
    private int nextDialogue;
    public float SelectedIconScale => settings != null && settings.selectedSaleItemIconScale > 0 ? settings.selectedSaleItemIconScale : 1.15f;
    public void Configure(EventSceneStepData step) { settings = step; nextDialogue = 0; }
    private void LateUpdate()
    {
        if (iconAnchor != null) iconAnchor.localPosition = Vector3.up * (settings != null ? settings.selectedSaleItemIconYOffset : 0.65f);
    }
    public string NextClickDialogue()
    {
        var lines = settings?.merchantClickDialogues;
        if (lines == null || lines.Count == 0) return string.Empty;
        for (int i = 0; i < lines.Count; i++)
        {
            int index = nextDialogue % lines.Count;
            nextDialogue = (index + 1) % lines.Count;
            string text = lines[index]?.GetText();
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        return string.Empty;
    }
    public Transform IconAnchor
    {
        get { LateUpdate(); return iconAnchor != null ? iconAnchor : transform; }
    }
    public void Initialize(ShopController owner, FieldAbsorbButton template)
    {
        shop=owner;
        if (iconAnchor == null)
        {
            var anchor=new GameObject("SaleIconAnchor");anchor.transform.SetParent(transform,false);anchor.transform.localPosition=Vector3.up*0.65f;iconAnchor=anchor.transform;
        }
        if (fieldButton != null) return;
        var collider=GetComponent<BoxCollider2D>();collider.size=new Vector2(0.8f,1.1f);collider.offset=new Vector2(0,0.45f);
        if(template==null) { Debug.LogError("[ShopMerchant] 판매 버튼 원본 연결이 필요합니다. ShopController의 기존 기물 판매 버튼 원본을 확인하세요.",this);return; }
        var pieceTemplate = template.GetComponentInParent<Piece>(true);
        var actor = GetComponent<EventSceneActor>();
        var referenceRoot = pieceTemplate != null ? pieceTemplate.TypeIconLayoutRoot : null;
        var referenceBox = pieceTemplate != null ? pieceTemplate.TypeIconLayoutBackground : null;
        if (referenceRoot == null || referenceBox == null || referenceBox.sprite == null || actor == null || actor.PieceData == null)
        {
            Debug.LogError("[ShopMerchant] 기물 아이콘 원본 및 상인 기물 데이터 연결을 확인하세요.", this);
            return;
        }
        // Instantiate only the shared button subtree; never modify the Piece prefab.
        fieldButton=Instantiate(template,transform,false);fieldButton.name="FieldAbsorbButtonRoot";
        var canvas=fieldButton.GetComponent<Canvas>();if(canvas==null)canvas=fieldButton.gameObject.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=Camera.main;
        canvas.sortingLayerID=referenceBox.sortingLayerID;canvas.sortingOrder=referenceBox.sortingOrder;
        if(fieldButton.GetComponent<GraphicRaycaster>()==null)fieldButton.gameObject.AddComponent<GraphicRaycaster>();
        fieldButton.transform.localPosition = actor.PieceData.GetTypeIconPosition(actor.VisualTeam, actor.UseAbsorbedPlayerVisual)
            + referenceRoot.localRotation * Vector3.Scale(referenceRoot.localScale, referenceBox.transform.localPosition);
        fieldButton.transform.localRotation = referenceRoot.localRotation * referenceBox.transform.localRotation;
        fieldButton.transform.localScale = Vector3.Scale(referenceRoot.localScale, referenceBox.transform.localScale)
            / referenceBox.sprite.pixelsPerUnit;
        fieldButton.ConfigureContextLayout(referenceBox, pieceTemplate.GetTypeIconLayoutSprite(actor.PieceData.pieceType));
        fieldButton.Hide();
    }
    public void OnPointerClick(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)ShowButton(); }
    public void ShowButton()
    {
        if(shop==null || !shop.CanInteractWithSale || fieldButton==null)return;
        string text = NextClickDialogue();
        if (!string.IsNullOrWhiteSpace(text))
            GetComponent<EventSceneActor>()?.PlaySpeechBubbleDetached(text,
                Mathf.Max(0, settings.merchantClickDuration), Mathf.Max(0, settings.merchantClickTypingSpeed),
                settings.merchantClickEmphasis, Mathf.Max(0, settings.merchantClickEmphasisStrength));
        fieldButton.ShowContextAction(()=> { if(shop.IsSelling)shop.CancelSelling();else shop.BeginSelling(); },shop.CurrentSaleIcon);
        RefreshButton();
    }
    public void RefreshButton() { if(fieldButton!=null)fieldButton.SetContextHoverIcons(shop.CurrentSaleIcon, shop.IsSelling ? shop.SaleCancelIcon : null, shop.IsSelling && !shop.HasSelectedSaleItem ? .82f : 1f); }
    private void OnDisable() { if(shop!=null)shop.MerchantUnavailable(this);if(fieldButton!=null)fieldButton.ClearContextAction(); }
}
