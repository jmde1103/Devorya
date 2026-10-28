using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TooltipStyleChecks
{
    const string Pending="TooltipStyleChecks.Pending";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static TooltipStyleChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {
                SessionState.SetBool(Pending,false);
                new GameObject("TooltipCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags).GetValue(target);
    static IEnumerator Guard()
    {
        var iterator=Checks();
        while(true)
        {
            object next;
            try {if(!iterator.MoveNext())break;next=iterator.Current;}
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);yield break;}
            yield return next;
        }
        Debug.Log("TOOLTIP_STYLE_PLAYMODE_PASS");
        EditorApplication.Exit(0);
    }
    static ScriptableObject[] Assets(string type)=>AssetDatabase.FindAssets("t:"+type)
        .Select(g=>AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
    static TooltipViewData View(ScriptableObject data)
    {
        if(data is GeneralSkillData a)return TooltipViewData.FromGeneralSkillData(a);
        if(data is UniqueSkillData b)return TooltipViewData.FromUniqueSkillData(b);
        if(data is BattleItemData c)return TooltipViewData.FromBattleItemData(c);
        if(data is StatusEffectData d)return TooltipViewData.FromStatusEffectData(d);
        return TooltipViewData.FromBattleRelicData((BattleRelicData)data);
    }
    static IEnumerator Checks()
    {
        yield return LocalizationSettings.InitializationOperation;
        var plain="<b>Text</b> 15%";
        Check(TooltipTextStyle.Format(plain,(TooltipSemanticMetadata)null)==plain,"Unstyled backward compatibility");
        var md=new TooltipSemanticMetadata();
        md.spans.Add(new TooltipSemanticMetadata.Span{text="{percent}%",role=TooltipTextRole.Value});
        Check(TooltipTextStyle.Format("30% and 7",md,30)=="<color=#E4C668>30%</color> and 7","Only explicit numbers, dynamic values");
        string[] dark={"FFFFFF","C9CECA","737A75","75C9D6","D87969","E4C668","B7A6D9"};
        string[] light={"1F1F1F","1F1F1F","666666","2F7F8D","A64F43","8A6A00","70599C"};
        for(int i=0;i<dark.Length;i++)
        {
            var role=(TooltipTextRole)i;
            Check(ColorUtility.ToHtmlStringRGB(TooltipTextStyle.Palette.Get(role))==dark[i],"Dark palette regression "+role);
            Check(ColorUtility.ToHtmlStringRGB(TooltipTextStyle.Palette.Get(role,TooltipSurface.LightSurface))==light[i],"Light palette "+role);
            var semantic=new TooltipSemanticMetadata();
            semantic.spans.Add(new TooltipSemanticMetadata.Span{text="test",role=role});
            Check(TooltipTextStyle.Format("test 7",semantic,surface:TooltipSurface.LightSurface)=="<color=#"+light[i]+">test</color> 7","Light semantic role "+role);
        }
        var root=new GameObject("TooltipCheckCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Run/CommonTooltipPopup.prefab");
        var instance=UnityEngine.Object.Instantiate(prefab,root.transform);
        var popup=instance.GetComponent<TooltipPopupUI>();
        var title=Field<TMP_Text>(popup,"titleText");
        var body=Field<TMP_Text>(popup,"mainDescriptionText");
        var category=Field<TMP_Text>(popup,"categoryText");
        var cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RelicUI/RelicRewardCard.prefab");
        var cardObject=UnityEngine.Object.Instantiate(cardPrefab,root.transform);
        var card=cardObject.GetComponent<RelicRewardCardUI>();
        int count=0;
        foreach(string locale in new[]{"ko","en","ja"})
        {
            LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(locale);
            yield return null;
            foreach(string type in new[]{"GeneralSkillData","UniqueSkillData","BattleItemData","StatusEffectData","BattleRelicData"})
            foreach(var data in Assets(type))
            {
                var view=View(data);
                popup.Show(view,new Vector2(Screen.width*.5f,Screen.height*.65f),Vector2.zero);
                yield return null;
                Canvas.ForceUpdateCanvases();
                body.ForceMeshUpdate();
                Check(title.color==TooltipTextStyle.Palette.title && title.fontStyle==FontStyles.Bold,"Title style "+data.name);
                Check(body.color==TooltipTextStyle.Palette.body && body.fontStyle==FontStyles.Normal,"Body style "+data.name);
                Check(category.color==TooltipTextStyle.Palette.secondary,"Category style");
                Check(!body.text.Contains("{percent}") && !body.text.Contains("{value}"),"Resolved placeholders "+data.name);
                Check(!body.isTextOverflowing,"Body overflow "+locale+" "+data.name);
                foreach(var section in instance.GetComponentsInChildren<TooltipSectionItemUI>())
                {
                    var sectionBody=Field<TMP_Text>(section,"mainDescriptionText");
                    sectionBody.ForceMeshUpdate();
                    Check(!sectionBody.isTextOverflowing,"Section overflow "+locale+" "+data.name);
                }
                if(data is BattleRelicData relic) Check(!relic.GetLocalizedDescription().Contains("<color"),"Non-tooltip relic text unchanged");
                popup.Hide();
                Check(!Field<RectTransform>(popup,"popupRoot").gameObject.activeSelf,"Close works");
                count++;
            }
            foreach(var relic in Assets("BattleRelicData").Cast<BattleRelicData>())
            {
                card.Bind(relic,_=>{});
                yield return card.Reveal();
                yield return null;
                var cardTitle=Field<TMP_Text>(card,"title");
                var cardBody=Field<TMP_Text>(card,"description");
                Canvas.ForceUpdateCanvases();
                cardBody.ForceMeshUpdate();
                Check(card.Ready,"Card reveal completes");
                Check(cardTitle.color==TooltipTextStyle.Palette.lightTitle && cardTitle.fontStyle==FontStyles.Bold,"Card light title");
                Check(cardBody.color==TooltipTextStyle.Palette.lightBody,"Card light body");
                Check(cardBody.text==TooltipTextStyle.Format(relic.localizedDescription,relic.GetLocalizedDescription(),surface:TooltipSurface.LightSurface),"Card localized semantics "+locale+" "+relic.name);
                Check(!cardBody.isTextOverflowing,"Card overflow "+locale+" "+relic.name);
                Check(!cardBody.GetParsedText().Contains("<color"),"Card rich tags");
            }
            Debug.Log("TOOLTIP_LOCALE_PASS "+locale);
        }
        Debug.Log("TOOLTIP_CHECKED_ASSET_LOCALES "+count);
    }
}
