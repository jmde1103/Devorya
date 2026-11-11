using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[InitializeOnLoad]
public static class EventButtonLocalizationSetup
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Pending="EventButtonLocalizationChecks";
    static EventButtonLocalizationSetup(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("EventButtonLocalizationChecks").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,F).GetValue(o);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("EVENT_BUTTON_CHECK: "+message);}
    public static void Run()
    {
        var collection=DevoryaLocalizationEditorUtility.GetStringTableCollection("Event_UI");
        string[] keys={"event.ui.shop.buy","event.ui.exit","event.ui.shop.sell"};
        string[] codes={"ko","en","ja"};
        string[][] values={new[]{"구매","나가기","판매"},new[]{"Buy","Exit","Sell"},new[]{"購入","退出","売却"}};
        for(int i=0;i<3;i++)for(int k=0;k<3;k++)
        {
            var table=DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection,codes[i]);
            if(string.IsNullOrWhiteSpace(table.GetEntry(keys[k])?.Value))
                DevoryaLocalizationEditorUtility.SetTableValue(table,keys[k],values[i][k]);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/EventScene.unity");
        var shop=UnityEngine.Object.FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
        Bind(Get<Button>(shop,"purchaseButton"),"event.ui.shop.buy","구매");
        Bind(Get<Button>(shop,"exitShopButton"),"event.ui.exit","나가기");
        var formation=UnityEngine.Object.FindFirstObjectByType<EventSceneFormationBoardController>(FindObjectsInactive.Include);
        Check(Get<Button>(formation,"exitFormationButton")==Get<Button>(shop,"exitShopButton"),"PieceSetting and shop share the localized Exit button");
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static void Bind(Button button,string key,string korean)
    {
        var text=button.GetComponentInChildren<TMP_Text>(true);
        var loc=text.GetComponent<LocalizeStringEvent>()??text.gameObject.AddComponent<LocalizeStringEvent>();
        loc.StringReference=DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(DevoryaLocalizationEditorUtility.GetStringTableCollection("Event_UI"),key);
        if(loc.OnUpdateString.GetPersistentEventCount()==0)
        {
            var setter=(UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>),text,typeof(TMP_Text).GetProperty("text").GetSetMethod());
            UnityEventTools.AddPersistentListener(loc.OnUpdateString,setter);
        }
        text.text=korean;EditorUtility.SetDirty(loc);EditorUtility.SetDirty(text);
    }
    static IEnumerator Guard(){var e=Checks();while(true){object n;try{if(!e.MoveNext())break;n=e.Current;}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);yield break;}yield return n;}Debug.Log("EVENT_BUTTON_LOCALIZATION_PASS");EditorApplication.Exit(0);}
    static IEnumerator Checks()
    {
        yield return LocalizationSettings.InitializationOperation;yield return new WaitForSecondsRealtime(1);
        var seq=UnityEngine.Object.FindFirstObjectByType<EventSceneSequenceController>();if(seq!=null)seq.StopAllCoroutines();
        var shop=UnityEngine.Object.FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
        // Isolate text ownership from merchant availability; production label methods remain unchanged.
        shop.enabled=false;
        var buy=Get<Button>(shop,"purchaseButton");var exit=Get<Button>(shop,"exitShopButton");
        buy.gameObject.SetActive(true);exit.gameObject.SetActive(true);
        var text=buy.GetComponentInChildren<TMP_Text>(true);var exitText=exit.GetComponentInChildren<TMP_Text>(true);var loc=text.GetComponent<LocalizeStringEvent>();
        string[] codes={"ko","en","ja"};string[][] values={new[]{"구매","나가기","판매"},new[]{"Buy","Exit","Sell"},new[]{"購入","退出","売却"}};
        for(int i=0;i<3;i++)
        {
            LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(codes[i]);yield return new WaitForSecondsRealtime(.4f);
            Check(text.text==values[i][0],codes[i]+" purchase label resolves and updates");
            Check(exitText.text==values[i][1],codes[i]+" shop/PieceSetting exit label resolves and updates");
            Check(ShopSaleLocalization.Sell==values[i][2],codes[i]+" sale text exists in SSOT");
        }
        typeof(ShopController).GetMethod("InitializeSaleUI",F).Invoke(shop,null);
        typeof(ShopController).GetField("originalPurchaseLabel",F).SetValue(shop,text.text);
        typeof(ShopController).GetField("originalLabelLocalizationEnabled",F).SetValue(shop,true);loc.enabled=false;
        typeof(ShopController).GetField("saleState",F).SetValue(shop,ShopController.SaleState.SellSelecting);
        for(int i=0;i<3;i++)
        {
            LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(codes[i]);yield return new WaitForSecondsRealtime(.3f);
            typeof(ShopController).GetMethod("RefreshSaleLanguage",F).Invoke(shop,null);
            Check(text.text==values[i][2],codes[i]+" sale mode owns button text");
        }
        shop.CancelSelling();yield return new WaitForSecondsRealtime(.4f);
        Check(loc.enabled&&text.text=="購入","Cancel restores current locale purchase translation");
        exit.gameObject.SetActive(false);LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale("ko");yield return null;exit.gameObject.SetActive(true);yield return new WaitForSecondsRealtime(.3f);
        Check(exitText.text=="나가기","Shared exit updates after inactive formation/shop mode switch");
    }
}
