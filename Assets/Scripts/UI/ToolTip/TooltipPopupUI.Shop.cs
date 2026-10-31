using TMPro;
using UnityEngine;
public partial class TooltipPopupUI
{
    private TMP_Text shopPriceText;
    private bool showShopPrice;
    private void PrepareShopPrice(TooltipViewData data)
    {
        showShopPrice = data != null && data.shopSellPrice.HasValue;
        if (baseTooltipBackground == null && popupRoot != null) baseTooltipBackground = popupRoot.Find("Background") as RectTransform;
        if (showShopPrice && shopPriceText == null && baseTooltipBackground != null && mainDescriptionText != null)
        {
            var go = new GameObject("ShopSellPrice", typeof(RectTransform));go.transform.SetParent(baseTooltipBackground,false);
            shopPriceText = go.AddComponent<TextMeshProUGUI>();shopPriceText.font=mainDescriptionText.font;
            shopPriceText.fontSharedMaterial=mainDescriptionText.fontSharedMaterial;
            shopPriceText.fontSize=mainDescriptionText.fontSize*0.7f;shopPriceText.color=mainDescriptionText.color;
            shopPriceText.alignment=TextAlignmentOptions.BottomRight;shopPriceText.raycastTarget=false;
            var r=shopPriceText.rectTransform;r.anchorMin=new Vector2(0,0);r.anchorMax=new Vector2(1,0);
            r.pivot=new Vector2(0.5f,0);r.offsetMin=new Vector2(20,12);r.offsetMax=new Vector2(-20,40);
        }
        if (shopPriceText != null)
        {
            shopPriceText.gameObject.SetActive(showShopPrice);
            shopPriceText.text=showShopPrice ? ShopSaleLocalization.Price(data.shopSellPrice.Value) : string.Empty;
        }
    }
}
