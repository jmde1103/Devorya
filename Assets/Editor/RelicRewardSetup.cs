using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Spine.Unity;

[InitializeOnLoad]
public static class RelicRewardSetup
{
    const string Pending = "RelicRewardSetupChecks";
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static PopupOpenAnimationData animation;
    static TMP_FontAsset font;
    static GameObject tooltipTemplate;
    static RelicRewardSetup()
    {
        EditorApplication.playModeStateChanged += s => {
            if(s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {
                SessionState.SetBool(Pending,false);
                new GameObject("RelicTestHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
            }
        };
    }
    static void Set(UnityEngine.Object o, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(o); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name,typeof(RectTransform));
        var r = (RectTransform)go.transform; r.SetParent(parent,false); r.sizeDelta=size;r.anchoredPosition=pos;return r;
    }
    static Image Image(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pos)
    {
        var r=Rect(name,parent,size,pos);var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;
    }
    static PopupOpenAnimator Glitch(RectTransform r)
    {
        r.gameObject.AddComponent<CanvasGroup>();var a=r.gameObject.AddComponent<PopupOpenAnimator>();a.SetAnimationData(animation);return a;
    }
    static TMP_Text Text(string name,Transform parent,Vector2 size,Vector2 pos,float fontSize)
    {
        TMP_Text template = null;
        if (tooltipTemplate != null)
            foreach (var candidate in tooltipTemplate.GetComponentsInChildren<TMP_Text>(true))
                if (candidate.name == name) template = candidate;
        TMP_Text t;
        if (template != null)
        {
            t = UnityEngine.Object.Instantiate(template, parent);
            t.name = name;
            var rect = t.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.sizeDelta = size; rect.anchoredPosition = pos; rect.localScale = Vector3.one;
        }
        else { var r = Rect(name,parent,size,pos); t = r.gameObject.AddComponent<TextMeshProUGUI>(); }
        t.font=font;t.fontSize=fontSize;t.color=new Color(.09f,.09f,.09f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
        t.enableAutoSizing=true;t.fontSizeMin=16;t.fontSizeMax=fontSize;t.textWrappingMode=TextWrappingModes.Normal;return t;
    }
    public static void Run()
    {
        System.IO.Directory.CreateDirectory("Assets/Prefabs/RelicUI/Resources");
        System.IO.Directory.CreateDirectory("Assets/Prefabs/RelicUI");
        System.IO.Directory.CreateDirectory("Assets/Devorya/Data/RelicReward");
        AssetDatabase.Refresh();
        tooltipTemplate=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Run/CommonTooltipPopup.prefab");
        animation=AssetDatabase.LoadAssetAtPath<PopupOpenAnimationData>("Assets/Devorya/Data/UI/PopupOpenAnimationData.asset");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/NeoDunggeunmoPro-Regular SDF.asset");
        var panelSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Relic_UI.png");
        var diamondSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Relic_UI_Lv.png");
        var card=Rect("RelicRewardCard",null,new Vector2(420,640),Vector2.zero);
        card.gameObject.AddComponent<CanvasGroup>();
        var hit=card.gameObject.AddComponent<Image>();hit.color=Color.clear;
        var button=card.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
        var ui=card.gameObject.AddComponent<RelicRewardCardUI>();Set(ui,"selectButton",button);
        var visual=Rect("Visual",card,new Vector2(420,640),Vector2.zero);Set(ui,"visualRoot",visual);
        var popupRoot=Rect("PopupRoot",visual,new Vector2(420,450),Vector2.zero);
        var panel=Image("Background",popupRoot,panelSprite,new Vector2(420,450),new Vector2(0,-65));
        // Preserve the authored notch and pixel corners: use original aspect ratio.
        panel.preserveAspect=true;
        Set(ui,"panelGlitch",Glitch(panel.rectTransform));
        var background=Rect("IconBackground",visual,new Vector2(210,210),new Vector2(0,190));
        Set(ui,"spineGlitch",Glitch(background));
        var skeleton=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Sprites/SpineAnime/RelicIconAnime/Relic_Icon_Anim_SkeletonData.asset");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/RelicUI/RelicSpineUI.mat");
        if(material==null)
        {
            var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Spine/Runtime/spine-unity/Materials/UI-StraightAlphaTex/SkeletonGraphicDefault-Straight.mat");
            material=new Material(template); material.SetFloat("_CanvasGroupCompatible",1); material.EnableKeyword("_CANVAS_GROUP_COMPATIBLE");
            AssetDatabase.CreateAsset(material,"Assets/Prefabs/RelicUI/RelicSpineUI.mat");
        }
        var spine=SkeletonGraphic.NewSkeletonGraphicGameObject(skeleton,background,material);
        spine.skeletonRenderer.gameObject.name="RelicIconSpine";
        spine.skeletonRenderer.transform.localScale=Vector3.one*3.1f;
        spine.skeletonRenderer.raycastTarget=false;
        Set(ui,"spineGraphic",spine.skeletonRenderer);Set(ui,"spineAnimation",spine.skeletonAnimation);
        var icon=Image("RelicIcon",visual,null,new Vector2(92,92),new Vector2(0,190));icon.preserveAspect=true;
        Set(ui,"relicIcon",icon);Set(ui,"iconGlitch",Glitch(icon.rectTransform));
        var content=Rect("TextContent",popupRoot,new Vector2(338,270),new Vector2(0,-50));
        Set(ui,"textGlitch",Glitch(content));
        Set(ui,"title",Text("TitleText",content,new Vector2(338,65),new Vector2(0,100),30));
        Set(ui,"description",Text("MainDescriptionText",content,new Vector2(330,190),new Vector2(0,-32),24));
        var rarity=Rect("SectionParent",popupRoot,new Vector2(160,30),new Vector2(0,-248));
        var serialized=new SerializedObject(ui);var diamonds=serialized.FindProperty("diamonds");diamonds.arraySize=3;
        for(int i=0;i<3;i++){var d=Image("Diamond"+(i+1),rarity,diamondSprite,new Vector2(21,21),Vector2.zero);d.gameObject.AddComponent<CanvasGroup>();diamonds.GetArrayElementAtIndex(i).objectReferenceValue=d.rectTransform;}
        var outlines=serialized.FindProperty("outlines");outlines.arraySize=2;
        var outline=panel.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(3,-3);outline.enabled=false;outlines.GetArrayElementAtIndex(0).objectReferenceValue=outline;
        outline=icon.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(2,-2);outline.enabled=false;outlines.GetArrayElementAtIndex(1).objectReferenceValue=outline;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var cardPrefab=PrefabUtility.SaveAsPrefabAsset(card.gameObject,"Assets/Prefabs/RelicUI/RelicRewardCard.prefab");
        UnityEngine.Object.DestroyImmediate(card.gameObject);
        var root=Rect("RelicRewardPopup",null,new Vector2(1920,1080),Vector2.zero);
        var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
        var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var dim=Image("InputBlocker",root,null,Vector2.zero,Vector2.zero);dim.color=new Color(0,0,0,.48f);dim.raycastTarget=true;dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;
        var safe=Rect("SafeArea",root,Vector2.zero,Vector2.zero);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;
        var row=Rect("Cards",safe,Vector2.zero,Vector2.zero);
        var popup=root.gameObject.AddComponent<RelicRewardPopupUI>();Set(popup,"cardPrefab",cardPrefab.GetComponent<RelicRewardCardUI>());Set(popup,"safeArea",safe);Set(popup,"cardRoot",row);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/RelicUI/Resources/RelicRewardPopup.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);
        var hudRoot=Rect("RelicInventoryHUD",null,new Vector2(1920,1080),Vector2.zero);
        var hudCanvas=hudRoot.gameObject.AddComponent<Canvas>();hudCanvas.renderMode=RenderMode.ScreenSpaceOverlay;hudCanvas.sortingOrder=210;
        var hudScaler=hudRoot.gameObject.AddComponent<CanvasScaler>();hudScaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;hudScaler.referenceResolution=new Vector2(1920,1080);hudScaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        hudRoot.gameObject.AddComponent<GraphicRaycaster>();
        var hudSafe=Rect("SafeArea",hudRoot,Vector2.zero,Vector2.zero);hudSafe.anchorMin=Vector2.zero;hudSafe.anchorMax=Vector2.one;
        var tray=Rect("RelicSlots",hudSafe,new Vector2(520,50),new Vector2(-24,-24));tray.anchorMin=tray.anchorMax=tray.pivot=new Vector2(1,1);
        var layout=tray.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=8;layout.childAlignment=TextAnchor.MiddleRight;layout.childControlWidth=false;layout.childControlHeight=false;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;
        var slot=Image("SlotTemplate",hudRoot,null,new Vector2(44,44),Vector2.zero);slot.gameObject.AddComponent<TooltipTrigger>();slot.raycastTarget=true;slot.gameObject.SetActive(false);
        var hud=hudRoot.gameObject.AddComponent<RelicRewardArrivalHUD>();Set(hud,"safeArea",hudSafe);Set(hud,"slotRoot",tray);Set(hud,"slotTemplate",slot);
        PrefabUtility.SaveAsPrefabAsset(hudRoot.gameObject,"Assets/Prefabs/RelicUI/Resources/RelicInventoryHUD.prefab");UnityEngine.Object.DestroyImmediate(hudRoot.gameObject);
        string path="Assets/Devorya/Data/RelicReward/유물보상_기본 목록.asset";
        var list=AssetDatabase.LoadAssetAtPath<RelicRewardListData>(path);
        if(list==null)
        {
            list=ScriptableObject.CreateInstance<RelicRewardListData>();list.rewardName="기본 유물 선택";list.description="기존 아이템 보상과 동일한 독립 확률 판정 후 최대 3개 중 하나를 선택합니다.";
            list.relicDropTable=new BattleRelicRewardDropEntry[3];
            string[] names={"초기 가속 모듈","잔류 추진 제어기","흡수 과충전 장치"};
            for(int i=0;i<3;i++)list.relicDropTable[i]=new BattleRelicRewardDropEntry{relicData=AssetDatabase.LoadAssetAtPath<BattleRelicData>("Assets/Devorya/Data/Relic/유물_"+names[i]+".asset"),dropChancePercent=100};
            AssetDatabase.CreateAsset(list,path);
        }
        EditorUtility.ClearDirty(font);
        AssetDatabase.SaveAssets();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static IEnumerator Guard()
    {
        bool runtimeError=false;
        Application.LogCallback onLog=(message,trace,type)=>{if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)runtimeError=true;};
        Application.logMessageReceived+=onLog;
        var stack=new Stack<IEnumerator>();stack.Push(Checks());
        while(stack.Count>0){object next;try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        Application.logMessageReceived-=onLog;
        Debug.Log(runtimeError ? "RELIC_REWARD_FAILED_RUNTIME_ERROR" : "RELIC_REWARD_PASS");EditorApplication.Exit(runtimeError ? 1 : 0);
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static IEnumerator Capture(RelicRewardPopupUI popup)
    {
        var cameraObject = new GameObject("PreviewCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.17f,.18f,.2f);
        camera.transform.position = new Vector3(0,0,-10); camera.orthographic = true;
        var canvas = popup.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var rt = new RenderTexture(1920,1080,24);
        camera.targetTexture = rt;
        yield return null;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var old = RenderTexture.active; RenderTexture.active = rt;
        var texture = new Texture2D(1920,1080,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
        System.IO.File.WriteAllBytes("Logs/relic-reward-preview.png",texture.EncodeToPNG());
        RenderTexture.active = old; camera.targetTexture = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
        UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(cameraObject);
    }
    static IEnumerator RunSceneStep(EventSceneSequenceController scene, EventSceneStepData step, Action complete)
    {
        yield return (IEnumerator)typeof(EventSceneSequenceController).GetMethod("ExecuteStepRoutine",Flags).Invoke(scene,new object[]{step});
        complete();
    }
    static IEnumerator ChooseReady()
    {
        float deadline = Time.realtimeSinceStartup + 15;
        while(RelicRewardPopupUI.Active == null && Time.realtimeSinceStartup < deadline) yield return null;
        var popup = RelicRewardPopupUI.Active; Check(popup != null, "Step opened popup");
        while(!(bool)typeof(RelicRewardPopupUI).GetField("accepting",Flags).GetValue(popup) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(Time.realtimeSinceStartup < deadline, "Step choices ready");
        popup.GetComponentInChildren<RelicRewardCardUI>().GetComponent<Button>().onClick.Invoke();
    }
    static IEnumerator Checks()
    {
        var run=new GameObject("RunState").AddComponent<RunStateManager>();
        var host=new GameObject("Owner").AddComponent<PieceAnimationManager>();
        new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
        var source=AssetDatabase.LoadAssetAtPath<RelicRewardListData>("Assets/Devorya/Data/RelicReward/유물보상_기본 목록.asset");
        var data=UnityEngine.Object.Instantiate(source);
        for(int i=0;i<data.relicDropTable.Length;i++)
        {
            data.relicDropTable[i].relicData=UnityEngine.Object.Instantiate(data.relicDropTable[i].relicData);
            data.relicDropTable[i].relicData.rarity=i+1;
        }
        Check(data.CreateChoices(run).Count==3,"Three unique choices");
        for(int count=1;count<=3;count++)
        {
            data.maxChoices=count;
            Check(data.CreateChoices(run).Count==count,"Choice count");
            var routine=host.StartCoroutine(RelicRewardPopupUI.ShowRoutine(host,data));
            float timeout=Time.realtimeSinceStartup+15;
            while(RelicRewardPopupUI.Active==null&&Time.realtimeSinceStartup<timeout)yield return null;
            var popup=RelicRewardPopupUI.Active;Check(popup!=null,"Popup spawned");
            while(!((bool)typeof(RelicRewardPopupUI).GetField("accepting",Flags).GetValue(popup))&&Time.realtimeSinceStartup<timeout)yield return null;
            Check(Time.realtimeSinceStartup<timeout,"Spine and all reveal phases complete");
            var cards=popup.GetComponentsInChildren<RelicRewardCardUI>();
            Check(cards.Length==count,"Instantiated count");
            foreach(var c in cards)
            {
                var diamonds=(RectTransform[])typeof(RelicRewardCardUI).GetField("diamonds",Flags).GetValue(c);
                int visible=0;float centered=0;
                foreach(var diamond in diamonds)if(diamond.gameObject.activeSelf){visible++;centered+=diamond.anchoredPosition.x;}
                Check(visible==c.Data.rarity&&Mathf.Abs(centered)<.01f,"Rarity count and centered spacing");
            }
            float sum=0;foreach(var c in cards)sum+=((RectTransform)c.transform).anchoredPosition.x;
            Check(Mathf.Abs(sum)<.01f,"Centered row");
            var chosen=cards[0].Data;
            if(count==3) { yield return Capture(popup); }
            cards[0].GetComponent<Button>().onClick.Invoke();
            cards[0].GetComponent<Button>().onClick.Invoke();
            if(count>1)cards[1].GetComponent<Button>().onClick.Invoke();
            yield return routine;
            Check(run.GetBattleRelicsCopy().Count==1&&run.HasBattleRelic(chosen.relicType),"Exactly one relic saved");
            Check(RelicRewardPopupUI.Active==null,"Popup released after arrival");
            Check(!data.CreateChoices(run).Contains(chosen),"Owned relic filtered");
            run.ClearRunState();
        }
        foreach(var entry in data.relicDropTable)entry.dropChancePercent=0;
        Check(data.CreateChoices(run).Count==0,"Zero chance gives empty choices");
        yield return RelicRewardPopupUI.ShowRoutine(host,data);
        Check(!RelicRewardPopupUI.IsOpen,"Empty rewards do not lock");
        // Cancellation must release input and must not grant a relic.
        foreach(var entry in data.relicDropTable)entry.dropChancePercent=100;
        host.StartCoroutine(RelicRewardPopupUI.ShowRoutine(host,data));
        yield return null;
        RelicRewardPopupUI.CloseOwned(host);
        yield return null;
        Check(!RelicRewardPopupUI.IsOpen&&run.GetBattleRelicsCopy().Count==0,"Cancelled reveal grants nothing");
        var world = new GameObject("WorldEventOwner").AddComponent<WorldMapEventController>();
        var worldData = ScriptableObject.CreateInstance<WorldMapEventData>();
        worldData.steps.Add(new WorldMapEventStep { type = WorldMapEventStepType.RelicReward, relicRewardList = data });
        world.Play(worldData);
        yield return ChooseReady();
        float deadline = Time.realtimeSinceStartup + 5;
        while(world.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
        Check(!world.IsPlaying && !WorldMapEventController.InputLocked && run.GetBattleRelicsCopy().Count == 1, "World-map step resumes after acquisition");
        UnityEngine.Object.Destroy(world.gameObject); UnityEngine.Object.Destroy(worldData);
        run.ClearRunState(); yield return null;
        var scene = new GameObject("EventSceneOwner").AddComponent<EventSceneSequenceController>();
        var sceneStep = new EventSceneStepData { stepType = EventSceneStepType.RelicReward, relicRewardList = data };
        bool stepCompleted = false;
        host.StartCoroutine(RunSceneStep(scene, sceneStep, () => stepCompleted = true));
        yield return ChooseReady();
        deadline = Time.realtimeSinceStartup + 5;
        while(!stepCompleted && Time.realtimeSinceStartup < deadline) yield return null;
        Check(stepCompleted && run.GetBattleRelicsCopy().Count == 1, "Event-scene step resumes after acquisition");
        UnityEngine.Object.Destroy(scene.gameObject);
        foreach(var entry in data.relicDropTable)UnityEngine.Object.Destroy(entry.relicData);
        UnityEngine.Object.Destroy(data);UnityEngine.Object.Destroy(run.gameObject);UnityEngine.Object.Destroy(host.gameObject);
    }
}