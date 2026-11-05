using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ShopMerchantEntryChecks
{
    const string Pending="ShopMerchantEntryChecks";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static ShopMerchantEntryChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);var g=new GameObject("MerchantEntryCheckHost");UnityEngine.Object.DontDestroyOnLoad(g);g.AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/WorldMapScene.unity");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,F).GetValue(o);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,F).SetValue(o,v);
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Debug.Log("MERCHANT_ENTRY_CHECK: "+text);}
    static IEnumerator Guard(){var e=Checks();while(true){object n;try{if(!e.MoveNext())break;n=e.Current;}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);yield break;}yield return n;}Debug.Log("MERCHANT_ENTRY_PASS");EditorApplication.Exit(0);}
    static void Click(Vector2 point,GameObject expected)
    {
        var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        var target=hits.Count>0?ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject):null;
        Check(target==expected,"Screen raycast reaches "+expected.name+" (top="+(hits.Count>0?hits[0].gameObject.name:"none")+")");
        ExecuteEvents.Execute(target,data,ExecuteEvents.pointerClickHandler);
    }
    static void Click(Button b){var c=b.GetComponentInParent<Canvas>();Click(RectTransformUtility.WorldToScreenPoint(c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera,b.transform.position),b.gameObject);}
    static void CheckMerchantLayout(ShopMerchant merchant, FieldAbsorbButton field, ShopController shop)
    {
        var actor = merchant.GetComponent<EventSceneActor>();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PiecePrefab.prefab").GetComponent<Piece>();
        var box = source.TypeIconLayoutBackground;
        var image = Get<Image>(field, "absorbBackgroundImage");
        var icon = Get<Image>(field, "absorbIconImage");
        var button = Get<Button>(field, "absorbButton");
        Check(Vector3.Distance(field.transform.localPosition, actor.PieceData.GetTypeIconPosition(actor.VisualTeam, actor.UseAbsorbedPlayerVisual)) < 0.00001f, "Merchant position matches PieceData icon position");
        Check(image.sprite == box.sprite && image.color == Color.white, "Original black Piece background and untinted pixel colors preserved");
        Check(icon.sprite == shop.CurrentSaleIcon && icon.color == Color.white, "Current gold or white X sprite preserved");
        Check(Vector3.Distance(icon.transform.position, button.transform.position) < 0.00001f, "Icon centered inside button");
        Check(image.rectTransform.rect.size == box.sprite.rect.size, "Button rect uses actual background pixel dimensions");
        Check(Vector3.Distance(field.transform.localScale, Vector3.one / box.sprite.pixelsPerUnit) < 0.00001f, "World canvas scale matches Piece sprite pixels per unit");
        var reference = UnityEngine.Object.Instantiate(box.gameObject);
        reference.name = "MerchantIconReferenceCheck";
        reference.transform.SetParent(merchant.transform, false);
        reference.transform.localPosition = field.transform.localPosition;
        reference.GetComponent<SpriteRenderer>().enabled = false;
        var camera = Camera.main;
        var savedRect = camera.pixelRect; var savedSize = camera.orthographicSize;
        foreach (float aspect in new[] {16f/9f, 16f/10f, 20f/9f})
        foreach (float zoom in new[] {0.75f, 1f, 1.5f})
        {
            float width = Mathf.Min(Screen.width, Screen.height * aspect);
            camera.pixelRect = new Rect((Screen.width-width)*0.5f, 0, width, width/aspect);
            camera.orthographicSize = savedSize * zoom;
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4]; image.rectTransform.GetWorldCorners(corners);
            Vector2 actual = camera.WorldToScreenPoint(corners[2]) - camera.WorldToScreenPoint(corners[0]);
            var bounds = box.sprite.bounds;
            Vector2 expected = camera.WorldToScreenPoint(reference.transform.TransformPoint(bounds.max)) - camera.WorldToScreenPoint(reference.transform.TransformPoint(bounds.min));
            Check(Vector2.Distance(actual, expected)<0.01f, "Projected size matches Piece reference: aspect="+aspect+" zoom="+zoom);
            var point = RectTransformUtility.WorldToScreenPoint(camera, button.transform.position);
            var data = new PointerEventData(EventSystem.current) { position=point };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject, "Actual screen raycast matches visible button at aspect="+aspect+" zoom="+zoom);
        }
        camera.pixelRect=savedRect;camera.orthographicSize=savedSize;
        UnityEngine.Object.Destroy(reference);
        Debug.Log("MERCHANT_UI_REFERENCE: localPosition="+field.transform.localPosition+" pixels="+box.sprite.rect.size+" PPU="+box.sprite.pixelsPerUnit+" iconRect="+icon.rectTransform.rect.size);
    }
    static IEnumerator CheckMerchantDialogues(ShopMerchant merchant)
    {
        var original = Get<EventSceneStepData>(merchant, "settings");
        var step = new EventSceneStepData { stepType=EventSceneStepType.SpawnActor, spawnActorIsShopkeeper=true };
        step.merchantClickDialogues.Add(new MerchantClickDialogue { koreanText="첫 번째 클릭 대사" });
        step.merchantClickDialogues.Add(new MerchantClickDialogue { koreanText="두 번째 클릭 대사" });
        merchant.Configure(step);
        Check(merchant.NextClickDialogue()=="첫 번째 클릭 대사" && merchant.NextClickDialogue()=="두 번째 클릭 대사" && merchant.NextClickDialogue()=="첫 번째 클릭 대사", "Independent merchant dialogues cycle in authored order");
        step.merchantClickTypingSpeed=0;step.merchantClickDuration=.5f;merchant.Configure(step);
        var speech=Get<ActorSpeechBubbleUI>(merchant.GetComponent<EventSceneActor>(),"speechBubbleUI");
        foreach(string expected in new[]{"첫 번째 클릭 대사","두 번째 클릭 대사","첫 번째 클릭 대사"})
        {
            Physics2D.SyncTransforms();
            Click(Camera.main.WorldToScreenPoint(merchant.GetComponent<BoxCollider2D>().bounds.center),merchant.gameObject);
            yield return null;
            Check(Get<TMPro.TMP_Text>(speech,"bubbleText").text==expected,"Merchant screen click displays next actual speech bubble");
        }
        Check(merchant.GetComponentsInChildren<ActorSpeechBubbleUI>(true).Length==1,"Rapid clicks reuse one speech bubble");
        step.merchantClickDialogues.RemoveAt(1);merchant.Configure(step);
        Check(merchant.NextClickDialogue()==merchant.NextClickDialogue(), "Single merchant dialogue repeats");
        step.merchantClickDialogues.Clear();
        Check(merchant.NextClickDialogue()==string.Empty, "Empty merchant dialogues safely leave selling available");
        step.merchantClickDialogues.Add(new MerchantClickDialogue { koreanText="테스트" });
        step.merchantClickDialogues.Add(new MerchantClickDialogue { koreanText="테스트 2" });
        var data=ScriptableObject.CreateInstance<EventSceneData>();data.localizationId="merchant_check";data.steps=new List<EventSceneStepData>{step};
        var editor=Editor.CreateEditor(data);
        var ensure=editor.GetType().GetMethod("EnsureAllLocalizationMetadata", F);
        ensure.Invoke(editor,new object[]{data});
        string first=step.merchantClickDialogues[0].localizationId, second=step.merchantClickDialogues[1].localizationId;
        step.merchantClickDialogues.Reverse();ensure.Invoke(editor,new object[]{data});
        Check(step.merchantClickDialogues[1].localizationId==first && step.merchantClickDialogues[0].localizationId==second, "Reordering retains merchant translation IDs");
        step.merchantClickDialogues.Add(new MerchantClickDialogue { localizationId=first });ensure.Invoke(editor,new object[]{data});
        Check(step.merchantClickDialogues[2].localizationId!=first, "Duplicated dialogue receives separate translation ID");
        step.merchantClickDialogues.RemoveAt(0);ensure.Invoke(editor,new object[]{data});
        Check(step.merchantClickDialogues[0].localizationId==first, "Deleting another dialogue preserves existing translation ID");
        var collection=DevoryaLocalizationEditorUtility.GetStringTableCollection("EventScene_Dialogue");
        var tables=collection.StringTables.ToArray();
        var ko=tables.First(t=>t.LocaleIdentifier.Code=="ko");
        var entry=ko.Values.First(e=>!string.IsNullOrWhiteSpace(e.LocalizedValue) && tables.Where(t=>t.LocaleIdentifier.Code=="en"||t.LocaleIdentifier.Code=="ja").All(t=>!string.IsNullOrWhiteSpace(t.GetEntry(e.KeyId)?.LocalizedValue)));
        var line=step.merchantClickDialogues[0];line.localizedText=new UnityEngine.Localization.LocalizedString("EventScene_Dialogue",entry.KeyId);
        var oldLocale=UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
        foreach(string code in new[]{"ko","en","ja"})
        {
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale=UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale(code);
            yield return null;
            Check(line.GetText()==tables.First(t=>t.LocaleIdentifier.Code==code).GetEntry(entry.KeyId).LocalizedValue, "Merchant dialogue resolves existing SSOT locale "+code);
        }
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale=oldLocale;
        UnityEngine.Object.DestroyImmediate(editor);UnityEngine.Object.Destroy(data);
        merchant.Configure(original);
        Check(Mathf.Approximately(merchant.SelectedIconScale,1.15f), "Legacy merchant defaults to 1.15 selected icon scale");
    }

    static void CaptureMerchantScreen()
    {
        var camera=Camera.main;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
        var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(Screen.width,Screen.height,24);
        var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try
        {
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+1;}
            camera.targetTexture=rt;Canvas.ForceUpdateCanvases();
            Canvas.ForceUpdateCanvases();camera.Render();
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            System.IO.File.WriteAllBytes("Logs/merchant-selected-item.png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
            RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(image);Canvas.ForceUpdateCanvases();
        }
    }

    static IEnumerator Checks()
    {
        yield return new WaitForSecondsRealtime(3);
        var map=AssetDatabase.LoadAssetAtPath<WorldMapData>("Assets/Devorya/Data/WorldMap/ForestWorldMapData.asset");
        var node=map.nodePlacements.First(n=>n.nodeType==MapNodeType.Shop);
        Check(node.eventSceneData!=null,"Actual world map shop has EventSceneData");
        Check(node.eventSceneData.steps.Count(s=>s.stepType==EventSceneStepType.SpawnActor&&s.spawnActorIsShopkeeper)==1,"Exactly one serialized merchant flag in actual shop data");
        WorldMapRuntimeState.SetCurrentNode(node.nodeId);
        Check(WorldMapRuntimeState.BeginShopNode(node.eventSceneData,node.shopInventoryData,node.shopLevel),"Actual BeginShopNode path succeeds");
        yield return SceneManager.LoadSceneAsync(node.targetSceneName);
        yield return null;
        var sequence=UnityEngine.Object.FindFirstObjectByType<EventSceneSequenceController>();
        float waited=0;
        while(waited<45)
        {
            var guide=UnityEngine.Object.FindFirstObjectByType<EventGuideUI>();
            if(guide!=null&&guide.IsDialoguePlaying){Set(guide,"isTypingSkipRequested",true);if(Get<bool>(guide,"canAcceptAdvanceInput"))Set(guide,"isAdvanceRequested",true);}
            if(waited>2&&!sequence.IsPlayerInteractionLocked)break;
            yield return new WaitForSecondsRealtime(0.25f);waited+=0.25f;
        }
        Check(!sequence.IsPlayerInteractionLocked,"Authored CameraShot dialogue speech and unlock finish");
        Check(Get<EventSceneData>(sequence,"currentEventSceneData")==node.eventSceneData,"Running data equals world map requested asset");
        var merchant=UnityEngine.Object.FindFirstObjectByType<ShopMerchant>();Check(merchant!=null,"Full event registers merchant without direct Spawn invocation");
        Check(merchant.GetComponent<BoxCollider2D>()!=null,"Merchant runtime collider exists");
        var shop=UnityEngine.Object.FindFirstObjectByType<ShopController>();var actor=merchant.GetComponent<EventSceneActor>();
        shop.RegisterMerchant(actor);shop.RegisterMerchant(actor);
        Check(merchant.GetComponentsInChildren<FieldAbsorbButton>(true).Length==1&&merchant.transform.Cast<Transform>().Count(t=>t.name=="SaleIconAnchor")==1,"Repeated registration creates no duplicate button or anchor");
        yield return CheckMerchantDialogues(merchant);
        Physics2D.SyncTransforms();Canvas.ForceUpdateCanvases();yield return null;
        Click(Camera.main.WorldToScreenPoint(merchant.GetComponent<BoxCollider2D>().bounds.center),merchant.gameObject);
        var field=merchant.GetComponentInChildren<FieldAbsorbButton>(true);Check(field.gameObject.activeInHierarchy,"Merchant click reveals shared button");
        Check(Get<EventSceneStepData>(merchant,"settings")!=null,"SpawnActor settings configured independently for merchant click");
        yield return new WaitForSecondsRealtime(0.8f);CheckMerchantLayout(merchant,field,shop);Click(Get<Button>(field,"absorbButton"));
        yield return new WaitForSecondsRealtime(0.8f);
        CheckMerchantLayout(merchant,field,shop);
        Check(shop.IsSelling,"Raycast button click enters selling");
        Check(Vector2.Distance(Get<Image>(field,"absorbIconImage").rectTransform.sizeDelta,new Vector2(16,16)*.82f)<.001f,"Only X glyph reduced by 18 percent");
        var run=RunStateManager.Instance;while(run.GetBattleItemsCopy().Count>0)run.RemoveBattleItemAt(0);if(run.GetGoldAmount()>0)run.TrySpendGold(run.GetGoldAmount());run.AddGold(100);
        var item=UnityEngine.Object.Instantiate(AssetDatabase.FindAssets("t:BattleItemData").Select(x=>AssetDatabase.LoadAssetAtPath<BattleItemData>(AssetDatabase.GUIDToAssetPath(x))).First(x=>x.itemType!=BattleItemType.None));item.sellPrice=19;run.TryAddBattleItem(item,RunStateManager.MaxBattleItemCount);
        var bar=Get<RunItemBarUI>(shop,"runItemBarUI");bar.RefreshFromRunState();bar.SetShopContext(shop);yield return new WaitForSecondsRealtime(1);
        var slot=Get<BattleItemSlotUI[]>(bar,"itemSlotUIs")[0];Click(Get<Button>(slot,"slotButton"));yield return new WaitForSecondsRealtime(0.8f);
        var icon=Get<Image>(field,"absorbIconImage");
        Check(icon.sprite==item.iconSprite,"Selected item appears inside merchant button");
        Check(Vector2.Distance(icon.rectTransform.sizeDelta,new Vector2(16,16))<.001f,"Selected item fits original 16 pixel icon area");
        Check(GameObject.Find("SaleItemPreview")==null,"No travelling or overhead sale item preview remains");
        var feedback=Get<Image>(slot,"itemIconImage").GetComponent<SlotHoverFeedback>();
        var outline=Get<Outline>(feedback,"outline");
        Check(Get<bool>(feedback,"selected") && outline.enabled && Mathf.Approximately(outline.effectColor.r,1f) && Mathf.Approximately(outline.effectColor.g,1f) && Mathf.Approximately(outline.effectColor.b,1f),"Selected source item outline is white");
        Check(outline.effectDistance==new Vector2(1.5f,-1.5f),"Sale outline matches normal item feedback thickness");
        var hoverData=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(Camera.main,icon.transform.position)};
        var hoverHits=new List<RaycastResult>();EventSystem.current.RaycastAll(hoverData,hoverHits);
        Check(hoverHits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hoverHits[0].gameObject)==Get<Button>(field,"absorbButton").gameObject,"Hover screen position reaches merchant button");
        for(var t=hoverHits[0].gameObject.transform;t!=null;t=t.parent)ExecuteEvents.Execute(t.gameObject,hoverData,ExecuteEvents.pointerEnterHandler);
        Check(icon.sprite==shop.SaleCancelIcon && Vector2.Distance(icon.rectTransform.sizeDelta,new Vector2(16,16)*.82f)<.001f,"Pointer enter replaces selected item with small X");
        for(var t=hoverHits[0].gameObject.transform;t!=null;t=t.parent)ExecuteEvents.Execute(t.gameObject,hoverData,ExecuteEvents.pointerExitHandler);
        Check(icon.sprite==item.iconSprite,"Pointer exit restores selected item");
        CaptureMerchantScreen();yield return new WaitForSecondsRealtime(.5f);
        var gold=UnityEngine.Object.FindFirstObjectByType<RunGoldUI>();Check(gold.DisplayedGold==119&&run.GetGoldAmount()==100,"Real slot click previews 119 without changing actual 100");
        var amountLabel=Get<TMPro.TMP_Text>(gold,"goldAmountText");var delta=Get<TMPro.TMP_Text>(gold,"previewDeltaText");
        Check(delta.font==amountLabel.font && Mathf.Approximately(delta.fontSize,amountLabel.fontSize),"Plus amount matches main gold font and size");
        var bg=amountLabel.transform.parent.GetComponentInParent<Image>();
        var bgCorners=new Vector3[4];var deltaCorners=new Vector3[4];
        (bg!=null?bg.rectTransform:amountLabel.rectTransform).GetWorldCorners(bgCorners);delta.rectTransform.GetWorldCorners(deltaCorners);
        Check(deltaCorners.Min(p=>p.y)>bgCorners.Max(p=>p.y),"Plus amount sits above gold background without overlap");
        for(var t=hoverHits[0].gameObject.transform;t!=null;t=t.parent)ExecuteEvents.Execute(t.gameObject,hoverData,ExecuteEvents.pointerEnterHandler);

        Click(Get<Button>(field,"absorbButton"));yield return new WaitForSecondsRealtime(0.8f);Check(!shop.IsSelling&&gold.DisplayedGold==100,"Screen X click cancels preview");
        Check(!Get<bool>(feedback,"selected"),"Cancel clears original slot outline");
        Click(Get<Button>(field,"absorbButton"));Click(Get<Button>(slot,"slotButton"));yield return new WaitForSecondsRealtime(0.8f);Click(Get<Button>(shop,"purchaseButton"));
        Check(run.GetGoldAmount()==119&&run.GetBattleItemsCopy().Count==0,"Screen Sell click removes exactly one and grants 19");
        // Verify inactive lookup branch without modifying authored event data.
        shop.gameObject.SetActive(false);Set(sequence,"shopController",null);
        var spawn=JsonUtility.FromJson<EventSceneStepData>(JsonUtility.ToJson(node.eventSceneData.steps.First(s=>s.spawnActorIsShopkeeper)));spawn.spawnActorId="InactiveShopCheck";
        yield return sequence.StartCoroutine((IEnumerator)sequence.GetType().GetMethod("ExecuteSpawnActorStepRoutine",F).Invoke(sequence,new object[]{spawn}));
        Check(Get<ShopMerchant>(shop,"merchant")!=null,"Inactive ShopController receives registered merchant");shop.gameObject.SetActive(true);yield return null;
        shop.BeginSelling();Check(shop.IsSelling,"Registered merchant works after shop reenable");
        Get<Button>(shop,"exitShopButton").onClick.Invoke();yield return new WaitForSecondsRealtime(5);
        Check(SceneManager.GetActiveScene().name=="WorldMapScene","Shop Exit returns to world map");
        Check(RunStateManager.Instance.GetGoldAmount()==119,"Exit preserves actual sold gold");
    }
}
