using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;

public static class TutorialChoiceSetup
{
    static void Ref(UnityEngine.Object o,string name,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    public static void Run()
    {
        try {
            var title=EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            if(title.GetRootGameObjects().Any(g=>g.name=="TutorialChoiceCanvas"))throw new Exception("Popup already exists; do not overwrite authored layout.");
            var shop=EditorSceneManager.OpenScene("Assets/Scenes/EventScene.unity",OpenSceneMode.Additive);
            var buttons=shop.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Button>(true)).ToArray();
            var purchase=buttons.First(b=>b.name=="ShopPurchaseButton");var exit=buttons.First(b=>b.name=="ShopExitButton");
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(title);
            var root=new GameObject("TutorialChoiceCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var modal=Rect("TutorialChoiceModal",root.transform,Vector2.zero,Vector2.zero);modal.anchorMin=Vector2.zero;modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=Vector2.zero;
            var shade=modal.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.65f);
            var card=Rect("TutorialChoicePanel",modal,new Vector2(800,370),Vector2.zero);
            var panel=Rect("QuestionPanel",card,new Vector2(800,210),new Vector2(0,65));
            var bg=panel.gameObject.AddComponent<Image>();var source=purchase.GetComponent<Image>();bg.sprite=source.sprite;bg.type=source.type;bg.color=source.color;
            var text=UnityEngine.Object.Instantiate(purchase.GetComponentInChildren<TMP_Text>(true),panel);text.name="QuestionText";
            foreach(var local in text.GetComponents<LocalizeStringEvent>())UnityEngine.Object.DestroyImmediate(local);
            text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=new Vector2(35,20);text.rectTransform.offsetMax=new Vector2(-35,-20);text.rectTransform.localScale=Vector3.one;
            text.text="튜토리얼을 진행하시겠습니까?";text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;text.fontSizeMin=24;text.fontSizeMax=38;text.raycastTarget=false;
            var yes=CopyButton(purchase,"YesButton",card,new Vector2(-150,-115),"예");
            var no=CopyButton(exit,"NoButton",card,new Vector2(150,-115),"아니오");
            var group=card.gameObject.AddComponent<CanvasGroup>();var animator=card.gameObject.AddComponent<PopupOpenAnimator>();
            Ref(animator,"targetRect",card);Ref(animator,"canvasGroup",group);Ref(animator,"animationData",AssetDatabase.LoadAssetAtPath<PopupOpenAnimationData>("Assets/Devorya/Data/UI/PopupOpenAnimationData.asset"));
            var popup=modal.gameObject.AddComponent<TutorialChoicePopup>();Ref(popup,"questionText",text);Ref(popup,"yesButton",yes);Ref(popup,"noButton",no);Ref(popup,"popupAnimator",animator);
            var controller=title.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TitleMenuController>(true)).First();
            Ref(controller,"tutorialChoicePopup",popup);Ref(controller,"bootCutsceneData",AssetDatabase.LoadAssetAtPath<TextCutsceneData>("Assets/Devorya/Data/Cutscene/Prologue_Boot.asset"));
            var layout=root.AddComponent<ResponsiveScreenLayout>();layout.followScenePlacement=true;layout.elements=new[]{new ResponsiveScreenLayout.Element{target=card}};Canvas.ForceUpdateCanvases();layout.CaptureScenePlacement();
            modal.gameObject.SetActive(false);
            var collection=DevoryaLocalizationEditorUtility.GetOrCreateStringTableCollection("Event_UI");
            string[] locales={"ko","en","ja"},questions={"튜토리얼을 진행하시겠습니까?","Would you like to play the tutorial?","チュートリアルをプレイしますか？"},yesTexts={"예","Yes","はい"},noTexts={"아니오","No","いいえ"};
            for(int i=0;i<3;i++) { var table=DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection,locales[i]);DevoryaLocalizationEditorUtility.SetTableValue(table,"title.tutorial.question",questions[i]);DevoryaLocalizationEditorUtility.SetTableValue(table,"title.tutorial.yes",yesTexts[i]);DevoryaLocalizationEditorUtility.SetTableValue(table,"title.tutorial.no",noTexts[i]); }
            EditorSceneManager.CloseScene(shop,true);EditorSceneManager.MarkSceneDirty(title);EditorSceneManager.SaveScene(title);AssetDatabase.SaveAssets();
            Debug.Log("TUTORIAL_CHOICE_SETUP_PASS: title popup installed, shop button visuals copied, 3 locales, original boot data preserved.");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Button CopyButton(Button source,string name,Transform parent,Vector2 position,string label)
    {
        var b=UnityEngine.Object.Instantiate(source,parent);b.name=name;b.gameObject.SetActive(true);b.onClick=new Button.ButtonClickedEvent();b.interactable=true;
        var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=new Vector2(240,90);r.localScale=Vector3.one;
        foreach(var local in b.GetComponentsInChildren<LocalizeStringEvent>(true))UnityEngine.Object.DestroyImmediate(local);
        var text=b.GetComponentInChildren<TMP_Text>(true);text.text=label;text.alignment=TextAlignmentOptions.Center;
        var cg=b.GetComponent<CanvasGroup>();if(cg!=null){cg.alpha=1;cg.interactable=true;cg.blocksRaycasts=true;}
        var nav=b.navigation;nav.mode=Navigation.Mode.Automatic;b.navigation=nav;return b;
    }
}
