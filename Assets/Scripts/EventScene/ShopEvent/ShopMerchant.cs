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
    public Transform IconAnchor => iconAnchor != null ? iconAnchor : transform;
    public void Initialize(ShopController owner, FieldAbsorbButton template)
    {
        shop=owner;
        var anchor=new GameObject("SaleIconAnchor");anchor.transform.SetParent(transform,false);anchor.transform.localPosition=Vector3.up*1.4f;iconAnchor=anchor.transform;
        var collider=GetComponent<BoxCollider2D>();collider.size=new Vector2(0.8f,1.1f);collider.offset=new Vector2(0,0.45f);
        if(template==null) { Debug.LogWarning("상점 판매 버튼 원본 연결이 필요합니다.",this);return; }
        // Instantiate the exact shared FieldAbsorbButtonRoot subtree, not another button implementation.
        fieldButton=Instantiate(template,transform,false);fieldButton.name="FieldAbsorbButtonRoot";
        var canvas=fieldButton.GetComponent<Canvas>();if(canvas==null)canvas=fieldButton.gameObject.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=Camera.main;canvas.sortingOrder=100;
        if(fieldButton.GetComponent<GraphicRaycaster>()==null)fieldButton.gameObject.AddComponent<GraphicRaycaster>();
        fieldButton.transform.localScale=Vector3.one*0.01f;fieldButton.transform.localPosition=new Vector3(0,-0.35f,-0.1f);
        fieldButton.Hide();
    }
    public void OnPointerClick(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)ShowButton(); }
    public void ShowButton()
    {
        if(shop==null || !shop.CanInteractWithSale || fieldButton==null)return;
        fieldButton.ShowContextAction(()=> { if(shop.IsSelling)shop.CancelSelling();else shop.BeginSelling(); },shop.CurrentSaleIcon);
    }
    public void RefreshButton() { if(fieldButton!=null)fieldButton.SetContextIcon(shop.CurrentSaleIcon); }
    private void OnDisable() { if(shop!=null)shop.MerchantUnavailable(this);if(fieldButton!=null)fieldButton.ClearContextAction(); }
}
