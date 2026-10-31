using UnityEditor;
using UnityEngine;
public static class ShopSellingSetup
{
    public static void Run()
    {
        var collection=DevoryaLocalizationEditorUtility.GetStringTableCollection("Event_UI");
        if(collection==null)throw new System.Exception("Event_UI missing");
        string[] keys={"sell","sell_instruction","sell_price","sell_unavailable"};
        string[][] values={
            new[]{"판매","판매할 아이템을 클릭해 주세요","판매금액 : {price}G","이 아이템은 판매할 수 없습니다."},
            new[]{"Sell","Click an item to sell.","Sell Price: {price}G","This item cannot be sold."},
            new[]{"売却","売却するアイテムをクリックしてください","売却価格：{price}G","このアイテムは売却できません。"}};
        string[] codes={"ko","en","ja"};
        for(int l=0;l<codes.Length;l++)
        {
            var table=DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection,codes[l]);
            for(int k=0;k<keys.Length;k++)
            {
                string key="event.ui.shop."+keys[k];
                if(table.GetEntry(key)==null)DevoryaLocalizationEditorUtility.SetTableValue(table,key,values[l][k]);
            }
        }
        AssetDatabase.SaveAssets();Debug.Log("SHOP_SELLING_SETUP_PASS");
    }
}
