using UnityEngine.Localization;
public static class ShopSaleLocalization
{
    public static string Text(string key, string fallback)
    {
        var reference=new LocalizedString("Event_UI", "event.ui.shop."+key);
        string value=reference.GetLocalizedString();
        return string.IsNullOrWhiteSpace(value)||value.StartsWith("No translation") ? fallback : value;
    }
    public static string Price(int value) => Text("sell_price","판매금액 : {price}G").Replace("{price}",value.ToString());
    public static string Sell => Text("sell","판매");
    public static string Instruction => Text("sell_instruction","판매할 아이템을 클릭해 주세요");
    public static string Unavailable => Text("sell_unavailable","이 아이템은 판매할 수 없습니다.");
}
