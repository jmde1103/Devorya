using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

[InitializeOnLoad]
public static class ShopSellingChecks
{
    const string Pending="ShopSellingChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static ShopSellingChecks()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            { SessionState.SetBool(Pending,false);var host=new GameObject("ShopSellingCheckHost");UnityEngine.Object.DontDestroyOnLoad(host);host.AddComponent<PieceAnimationManager>().StartCoroutine(Guard()); }
        };
    }
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,Flags).GetValue(o);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
    static void Check(bool pass,string msg){if(!pass)throw new Exception(msg);Debug.Log("SHOP_CHECK: "+msg);}
    public static void Run(){ShopSellingSetup.Run();EditorSceneManager.OpenScene("Assets/Scenes/EventScene.unity");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static IEnumerator Guard()
    {
        var checks=Checks();while(true){object next;try{if(!checks.MoveNext())break;next=checks.Current;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
        Debug.Log("SHOP_SELLING_PLAYMODE_PASS");EditorApplication.Exit(0);
    }
    static void Activate(Transform t){for(;t!=null;t=t.parent)t.gameObject.SetActive(true);}
    static IEnumerator Checks()
    {
        yield return LocalizationSettings.InitializationOperation;
        yield return new WaitForSecondsRealtime(5);
        var sequence=UnityEngine.Object.FindFirstObjectByType<EventSceneSequenceController>();sequence.StopAllCoroutines();
        Set(sequence,"isSequenceActive",false);Call(sequence,"SetEventScenePlayerInteractionLocked",false,false);Call(sequence,"SetRunHUDVisibleImmediately",true);
        var shop=UnityEngine.Object.FindFirstObjectByType<ShopController>();Check(shop!=null,"Existing shop controller loaded");
        var run=RunStateManager.Instance;Check(run!=null,"Existing RunState loaded");
        var gold=UnityEngine.Object.FindFirstObjectByType<RunGoldUI>(FindObjectsInactive.Include);Check(gold!=null,"Existing EventScene gold UI");Activate(gold.transform);
        while(run.GetGoldAmount()>0)run.TrySpendGold(run.GetGoldAmount());run.AddGold(100);yield return new WaitForSecondsRealtime(0.9f);
        Check(gold.DisplayedGold==100,"Gold reaches actual 100");
        var data=AssetDatabase.LoadAssetAtPath<EventSceneData>("Assets/Devorya/Data/Event/EventScene/Shop/Event_Shop_FieldStage#1.asset");
        var merchantStep=data.steps.First(s=>s.stepType==EventSceneStepType.SpawnActor&&s.spawnActorIsShopkeeper);
        Check(merchantStep.spawnActorPieceData!=null,"Existing shop merchant PieceData retained");
        var background=UnityEngine.Object.FindFirstObjectByType<BackgroundManager>();background.LoadMapFromData(data.backgroundMapData);
        sequence.StartCoroutine((IEnumerator)Call(sequence,"ExecuteSpawnActorStepRoutine",merchantStep));yield return new WaitForSecondsRealtime(2);
        var merchant=UnityEngine.Object.FindFirstObjectByType<ShopMerchant>();Check(merchant!=null,"SpawnActor creates merchant");
        Check(UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.Physics2DRaycaster>()!=null,"Scene has merchant physics click raycaster");
        Check(merchant.GetComponent<Piece>()==null,"Merchant not registered as battle Piece");
        Check(merchant.GetComponent<EventSceneActor>().VisualObject!=null,"Existing Spine visual created");
        var field=merchant.GetComponentInChildren<FieldAbsorbButton>(true);Check(field!=null&&field.name=="FieldAbsorbButtonRoot","Reuses shared FieldAbsorbButtonRoot");
        merchant.ShowButton();Check(field.gameObject.activeSelf,"Merchant interaction opens shared button");
        Get<Button>(field,"absorbButton").onClick.Invoke();Check(shop.CurrentSaleState==ShopController.SaleState.SellSelecting,"Shared button enters selling");
        Check(Get<Button>(shop,"purchaseButton").interactable==false,"Sell disabled without item");
        Check(Get<TMP_Text>(shop,"purchaseLabel").text==ShopSaleLocalization.Sell,"Buy label becomes localized Sell");
        Check(Get<Image>(field,"absorbIconImage").sprite==Get<Sprite>(shop,"sellCancelIcon"),"Shared button changes to cancel icon");
        var prompt=Get<ShopFailurePopupUI>(shop,"shopFailurePopupUI");Check(prompt!=null,"Reuses existing failure popup");yield return new WaitForSecondsRealtime(1.5f);
        Check(Get<TMP_Text>(prompt,"messageText").text==ShopSaleLocalization.Instruction,"Persistent localized sale instruction");
        prompt.Show("Failure");yield return new WaitForSecondsRealtime(2);
        Check(Get<TMP_Text>(prompt,"messageText").text==ShopSaleLocalization.Instruction,"Failure gives way to valid sale instruction");
        while(run.GetBattleItemsCopy().Count>0)run.RemoveBattleItemAt(0);
        var source=AssetDatabase.FindAssets("t:BattleItemData").Select(x=>AssetDatabase.LoadAssetAtPath<BattleItemData>(AssetDatabase.GUIDToAssetPath(x))).First(x=>x!=null&&x.itemType!=BattleItemType.None);
        var item=UnityEngine.Object.Instantiate(source);item.sellPrice=19;run.TryAddBattleItem(item,RunStateManager.MaxBattleItemCount);
        var bar=Get<RunItemBarUI>(shop,"runItemBarUI");bar.RefreshFromRunState();bar.SetShopContext(shop);
        var slots=Get<BattleItemSlotUI[]>(bar,"itemSlotUIs");Check(Get<Button>(slots[0],"slotButton").interactable,"Owned slot accepts actual button input");Get<Button>(slots[0],"slotButton").onClick.Invoke();
        Check(shop.CurrentSaleState==ShopController.SaleState.SellReady,"Owned item slot selects sale");
        var product=shop.GetComponentInChildren<ShopItemDisplay>(true);
        Check(product!=null,"Existing shop product available");product.Initialize(item,10);
        shop.HandleItemClicked(product);Check(Get<ShopItemDisplay>(shop,"selectedItemDisplay")==null,"Sale blocks product selection");
        Call(product,"RefreshInteractionState",true);Check(!product.IsInteractionAllowed,"Sale blocks product collider input");
        Check(run.GetGoldAmount()==100&&run.GetBattleItemsCopy().Count==1,"Preview does not mutate inventory or gold");
        yield return new WaitForSecondsRealtime(0.9f);
        Check(gold.DisplayedGold==119&&gold.IsPreviewing,"100 to 119 sale preview");
        Check(Get<TMP_Text>(gold,"previewDeltaText").text=="+19G","Sale delta displayed");
        Check(Get<Image>(shop,"saleVisual")!=null,"Visual copy travels to merchant");
        Check(Get<TMP_Text>(gold,"goldAmountText").color==Get<Color>(gold,"previewColor"),"Preview alone uses green");
        var tooltip=TooltipPopupUI.Instance;tooltip.Show(TooltipViewData.FromBattleItemData(item).WithShopPrice(19),Vector2.zero,Vector2.zero);
        Check(Get<TMP_Text>(tooltip,"shopPriceText").gameObject.activeSelf,"Shop tooltip displays sale price");
        tooltip.Show(TooltipViewData.FromBattleItemData(item),Vector2.zero,Vector2.zero);
        Check(!Get<TMP_Text>(tooltip,"shopPriceText").gameObject.activeSelf,"Ordinary tooltip hides sale price");tooltip.Hide();
        Get<Button>(field,"absorbButton").onClick.Invoke();yield return new WaitForSecondsRealtime(0.9f);
        Check(shop.CurrentSaleState==ShopController.SaleState.Normal&&gold.DisplayedGold==100&&!gold.IsPreviewing,"X restores normal mode and actual gold");
        Check(run.GetGoldAmount()==100&&run.GetBattleItemsCopy().Count==1,"Cancel does not sell");
        Call(product,"RefreshInteractionState",true);Check(product.IsInteractionAllowed,"Cancel restores purchase interaction");
        shop.HandleItemClicked(product);shop.OnPurchaseButtonClicked();yield return new WaitForSecondsRealtime(0.9f);
        Check(run.GetGoldAmount()==90&&run.GetBattleItemsCopy().Count==2&&gold.DisplayedGold==90,"Existing shop purchase updates inventory and animated gold");
        run.RemoveBattleItemAt(1);run.AddGold(10);yield return new WaitForSecondsRealtime(0.9f);
        shop.BeginSelling();shop.SelectSaleItem(0,item,slots[0].transform as RectTransform);yield return new WaitForSecondsRealtime(0.9f);
        Check(shop.ConfirmSale(),"Sale commits");Check(!shop.ConfirmSale(),"Double confirm cannot repeat transaction");
        Check(run.GetGoldAmount()==119&&run.GetBattleItemsCopy().Count==0,"Sale removes one and grants exact gold");
        Check(gold.TargetGold==119&&gold.DisplayedGold==119&&!gold.IsPreviewing,"Confirm does not count preview twice");
        Check(shop.CurrentSaleState==ShopController.SaleState.SellSelecting,"Continuous sale stays in selection mode");
        run.TryAddBattleItem(item,RunStateManager.MaxBattleItemCount);shop.SelectSaleItem(0,item,slots[0].transform as RectTransform);
        Check(shop.ConfirmSale()&&run.GetGoldAmount()==138,"Continuous second sale");
        var unsellable=UnityEngine.Object.Instantiate(item);unsellable.sellPrice=0;run.TryAddBattleItem(unsellable,RunStateManager.MaxBattleItemCount);
        shop.SelectSaleItem(0,unsellable,slots[0].transform as RectTransform);Check(!shop.ConfirmSale()&&run.GetGoldAmount()==138,"Zero price rejected without payment");
        unsellable.sellPrice=-5;Check(unsellable.SellPrice==0,"Negative price clamped");run.RemoveBattleItemAt(0);
        run.TryAddBattleItem(item,RunStateManager.MaxBattleItemCount);shop.SelectSaleItem(0,item,slots[0].transform as RectTransform);run.RemoveBattleItemAt(0);yield return null;
        Check(!shop.IsSelling&&!gold.IsPreviewing,"Lost inventory target clears sale state");
        foreach(string code in new[]{"ko","en","ja"})
        { LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(code);yield return null;Check(!ShopSaleLocalization.Sell.StartsWith("No translation")&&ShopSaleLocalization.Price(19).Contains("19G"),"Sale localization "+code); }
        shop.BeginSelling();shop.enabled=false;yield return null;Check(!gold.IsPreviewing&&!shop.IsSelling,"Disable cleans sale state");shop.enabled=true;
        shop.BeginSelling();merchant.gameObject.SetActive(false);yield return null;
        Check(!shop.IsSelling&&!gold.IsPreviewing,"Merchant disappearance cleans sale");
        // Exercise the same component through actual scene transitions and actual Gold events.
        foreach(string scene in new[]{"EventScene","WorldMapScene","BattleScene"})
        {
            if(SceneManager.GetActiveScene().name!=scene){yield return SceneManager.LoadSceneAsync(scene);yield return new WaitForSecondsRealtime(5);}
            run=RunStateManager.Instance;
            gold=UnityEngine.Object.FindFirstObjectByType<RunGoldUI>(FindObjectsInactive.Include);Check(gold!=null,scene+" shared Gold UI exists");Activate(gold.transform);gold.enabled=false;gold.enabled=true;yield return null;
            Check(gold.DisplayedGold==run.GetGoldAmount(),scene+" initial gold displayed immediately");
            var label=Get<TMP_Text>(gold,"goldAmountText");Color color=label.color;Vector3 scale=label.transform.localScale;Vector2 pos=label.rectTransform.anchoredPosition;
            int before=run.GetGoldAmount();Time.timeScale=0;run.AddGold(150);yield return null;
            Check(gold.DisplayedGold<before+150,scene+" change animates instead of snapping");yield return new WaitForSecondsRealtime(0.9f);
            Check(gold.DisplayedGold==before+150,scene+" increase unscaled");run.TrySpendGold(30);yield return new WaitForSecondsRealtime(0.9f);
            Check(gold.DisplayedGold==before+120,scene+" decrease unscaled");
            run.AddGold(100);yield return new WaitForSecondsRealtime(0.12f);int intermediate=gold.DisplayedGold;run.AddGold(10000);yield return null;
            Check(gold.DisplayedGold>=intermediate,scene+" retarget starts from display");yield return new WaitForSecondsRealtime(0.9f);
            Check(gold.DisplayedGold==run.GetGoldAmount(),scene+" final value matches actual");
            Check(label.color==color&&label.transform.localScale==scale&&label.rectTransform.anchoredPosition==pos,scene+" color scale position restored");
            gold.gameObject.SetActive(false);run.AddGold(20);gold.gameObject.SetActive(true);Check(gold.DisplayedGold==run.GetGoldAmount(),scene+" reenable snaps to actual");Time.timeScale=1;
        }
    }
}
