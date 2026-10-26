using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class TooltipBodySizeChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,Flags).GetValue(o);
    static void Call(object o,string m)=>o.GetType().GetMethod(m,Flags).Invoke(o,null);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static float Edge(RectTransform r,Transform root,int corner){var c=new Vector3[4];r.GetWorldCorners(c);return root.InverseTransformPoint(c[corner]).y;}
    static void CheckSections(Transform canvas)
    {
        var parent=new GameObject("SectionsTest",typeof(RectTransform),typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
        parent.SetParent(canvas,false);parent.sizeDelta=new Vector2(550,2000);
        var layout=parent.GetComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childControlWidth=false;layout.childForceExpandWidth=false;layout.spacing=12;
        var prefab=AssetDatabase.LoadAssetAtPath<TooltipSectionItemUI>("Assets/Prefabs/TooltipSection UI/TooltipSection_SatusEffect.prefab");
        var first=UnityEngine.Object.Instantiate(prefab,parent);var second=UnityEngine.Object.Instantiate(prefab,parent);
        var body=Get<TMP_Text>(first,"mainDescriptionText");var bg=Get<Image>(first,"layoutBackgroundImage");
        var title=Get<TMP_Text>(first,"titleText");var root=(RectTransform)first.transform;
        Check(bg!=null,"Section resize background is serialized");
        var color=bg.color;var border=bg.sprite.border;
        var data=new TooltipSectionData {sectionTitle="Status",sectionDescription="Short",sectionColor=Color.clear};
        first.Refresh(data);second.Refresh(data);LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        float shortHeight=root.rect.height;float titleFromTop=root.rect.yMax-Edge(title.rectTransform,root,1);
        float shortBgHeight=bg.rectTransform.rect.height;float gap=Edge(root,parent,0)-Edge((RectTransform)second.transform,parent,1);
        foreach(var sentence in new[]{"긴 상태효과 설명이 아래쪽 테두리 밖으로 넘치지 않도록 배경도 함께 늘어납니다. ","A longer status effect description must remain inside its section background without overlapping the next section. ","長い状態効果の説明文に合わせて背景が下方向へ伸びます。 "})
        {
            data.sectionDescription=string.Concat(Enumerable.Repeat(sentence,12));first.Refresh(data);LayoutRebuilder.ForceRebuildLayoutImmediate(parent);body.ForceMeshUpdate(true);
            Check(root.rect.height>shortHeight && bg.rectTransform.rect.height>shortBgHeight,"Long section expands root and background");
            Check(Mathf.Abs((root.rect.height-shortHeight)-(bg.rectTransform.rect.height-shortBgHeight))<.1f,"Background tracks full section growth");
            Check(Mathf.Abs((root.rect.yMax-Edge(title.rectTransform,root,1))-titleFromTop)<.1f,"Section header keeps top offset");
            Check(Edge(body.rectTransform,root,0)>Edge(bg.rectTransform,root,0)+5,"Section body keeps bottom padding");
            var info=body.textInfo;
            for(int i=0;i<info.characterCount;i++) if(info.characterInfo[i].isVisible)
            {
                var c=info.characterInfo[i];var low=bg.rectTransform.InverseTransformPoint(body.transform.TransformPoint(c.bottomLeft));var high=bg.rectTransform.InverseTransformPoint(body.transform.TransformPoint(c.topRight));
                Check(low.y>bg.rectTransform.rect.yMin+2 && high.y<bg.rectTransform.rect.yMax && low.x>=bg.rectTransform.rect.xMin && high.x<=bg.rectTransform.rect.xMax,"Rendered glyph inside background");
            }
            Check(Mathf.Abs(Edge(root,parent,0)-Edge((RectTransform)second.transform,parent,1)-gap)<.1f,"Following section keeps layout gap");
            float h=root.rect.height;first.Refresh(data);LayoutRebuilder.ForceRebuildLayoutImmediate(parent);Check(Mathf.Abs(root.rect.height-h)<.1f,"Repeated section refresh does not accumulate height");
        }
        data.sectionDescription="Short";first.Refresh(data);LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        Check(Mathf.Abs(root.rect.height-shortHeight)<.1f && Mathf.Abs(bg.rectTransform.rect.height-shortBgHeight)<.1f,"Short section restores authored height");
        Check(bg.color==color && bg.type==Image.Type.Sliced && bg.sprite.border==border && border.y>0 && border.w>0,"Section keeps authored color and sliced border");
        UnityEngine.Object.DestroyImmediate(parent.gameObject);
        Debug.Log("TOOLTIP_SECTION_SIZE_PASS: KR/EN/JA rendered glyph bounds, background growth, spacing, repeat refresh, shrink, colors and borders.");
    }
    public static void Run()
    {
        try{
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var canvas=new GameObject("TooltipTestCanvas",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(1920,1080);
            CheckSections(canvas.transform);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Run/CommonTooltipPopup.prefab");
            var go=UnityEngine.Object.Instantiate(prefab,canvas.transform);go.SetActive(true);var ui=go.GetComponent<TooltipPopupUI>();
            var bg=Get<RectTransform>(ui,"baseTooltipBackground");var text=Get<TMP_Text>(ui,"mainDescriptionText");var sections=Get<RectTransform>(ui,"sectionParent");
            var title=Get<TMP_Text>(ui,"titleText");var titlePosition=title.rectTransform.localPosition;
            var hierarchy=go.GetComponentsInChildren<Transform>(true).Select(t=>new {target=t,parent=t.parent,name=t.name}).ToArray();
            typeof(TooltipPopupUI).GetField("baseTooltipBackground",Flags).SetValue(ui,null);
            text.text="짧은 설명입니다.";Call(ui,"ResizeBaseTooltipToDescription");Call(ui,"AlignSectionParentBelowBaseTooltip");
            float height=bg.rect.height,top=Edge(bg,go.transform,1),textTop=Edge(text.rectTransform,go.transform,1),sectionTop=Edge(sections,go.transform,1);
            foreach(var line in new[]{"긴 설명을 표시할 때 배경이 아래로 늘어나고 하단 상태효과 섹션도 함께 내려갑니다. ","Long descriptions expand the tooltip background downward while keeping its decorative corners intact. ","説明文が長い場合は背景が下方向に伸び、下部の状態効果セクションも一緒に移動します。 "}){
                text.text=string.Concat(Enumerable.Repeat(line,8));Call(ui,"ResizeBaseTooltipToDescription");Call(ui,"AlignSectionParentBelowBaseTooltip");
                Check(bg.rect.height>height,"Long text grows background");Check(Mathf.Abs(Edge(bg,go.transform,1)-top)<.02f,"Background top fixed");Check(Mathf.Abs(Edge(text.rectTransform,go.transform,1)-textTop)<.02f,"Text top fixed");
                Check(Edge(text.rectTransform,go.transform,0)>Edge(bg,go.transform,0),"Text bottom inside background");
                Check(Mathf.Abs((sectionTop-Edge(sections,go.transform,1))-(bg.rect.height-height))<.05f,"Section follows exact height increase");
                Check(title.rectTransform.localPosition==titlePosition,"Title unchanged");
            }
            typeof(TooltipPopupUI).GetField("baseTooltipBackground",Flags).SetValue(ui,null);
            text.text="짧은 설명입니다.";Call(ui,"ResizeBaseTooltipToDescription");Call(ui,"AlignSectionParentBelowBaseTooltip");
            Check(Mathf.Abs(bg.rect.height-height)<.02f&&Mathf.Abs(Edge(sections,go.transform,1)-sectionTop)<.02f,"Short text restores original body and section positions");
            Check(hierarchy.All(t=>t.target!=null && t.target.parent==t.parent && t.target.name==t.name),"Authored hierarchy unchanged (TMP may create fallback-font submeshes)");
            Check(bg.GetComponent<Image>().type==Image.Type.Sliced,"Sliced rendering");Check(bg.GetComponent<Image>().sprite.border==new Vector4(28,19,28,19),"Sprite Editor border imported");
            Debug.Log("TOOLTIP_BODY_SIZE_PASS: Korean/English/Japanese long text, fixed top edges, padded text bounds, exact section shift, short-text restore, original hierarchy, sliced sprite borders.");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
