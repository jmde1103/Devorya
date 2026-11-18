using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runtime-only vertical expansion, measured from the authored instance, including scene overrides.
public sealed class RecompositionVerticalPanel
{
    private struct State
    {
        public RectTransform rect; public float y, sizeY;
        public State(RectTransform r) { rect=r; y=r.anchoredPosition.y; sizeY=r.sizeDelta.y; }
        public void Restore() { if(rect==null)return;var p=rect.anchoredPosition;p.y=y;rect.anchoredPosition=p;var s=rect.sizeDelta;s.y=sizeY;rect.sizeDelta=s; }
    }
    private readonly State[] states;
    private readonly TMP_Text[] texts;
    private readonly RectTransform panel, background;
    private readonly float baseHeight, backgroundHeight;
    public Rect BaselineBounds { get; }
    public RectTransform Panel => panel;
    public RecompositionVerticalPanel(RectTransform root, params TMP_Text[] labels)
    {
        panel=root;
        states=root.GetComponentsInChildren<RectTransform>(true).Select(r=>new State(r)).ToArray();
        texts=labels.Where(t=>t!=null).OrderByDescending(t=>Bounds(t.rectTransform,root).yMax).ToArray();
        baseHeight=root.rect.height;
        background=root.Find("Background") as RectTransform;
        backgroundHeight=background!=null?background.rect.height:0f;
        BaselineBounds=Bounds(root,root.parent);
    }
    public void Restore() { foreach(var state in states)state.Restore(); }
    public void Resize(bool flowText=true)
    {
        Restore();
        float contentBottom=float.PositiveInfinity;
        foreach(var t in texts)
        {
            if(!t.gameObject.activeSelf||string.IsNullOrEmpty(t.text))continue;
            var rect=t.rectTransform;
            t.ForceMeshUpdate(true,true);
            // Auto Size resolves using the user's range. Do not enlarge a text box that already fits.
            if(!t.enableAutoSizing||t.isTextOverflowing)
            {
                float preferred=t.GetPreferredValues(t.text,Mathf.Max(1,rect.rect.width),Mathf.Infinity).y;
                if(preferred>rect.rect.height+.1f)SetHeightKeepingTop(rect,preferred);
                t.ForceMeshUpdate(true,true);
            }
            if(t.textInfo.characterCount==0)continue;
            // Empty space in the authored text Rect is not rendered content and must not grow the panel.
            var rendered=t.textBounds;
            float bottom=panel.InverseTransformPoint(t.transform.TransformPoint(new Vector3(rendered.center.x,rendered.min.y,0))).y;
            contentBottom=Mathf.Min(contentBottom,bottom);
        }
        float boundary=background!=null?Mathf.Max(panel.rect.yMin,Bounds(background,panel).yMin):panel.rect.yMin;
        float overflow=float.IsPositiveInfinity(contentBottom)?0f:Mathf.Max(0f,boundary-contentBottom);
        float height=baseHeight+overflow;
        float delta=height-panel.rect.height;
        var children=new List<(RectTransform r,float h)>();
        foreach(Transform child in panel)if(child is RectTransform rect&&rect!=background)children.Add((rect,rect.rect.height));
        SetHeightKeepingTop(panel,height);
        foreach(var item in children)
        {
            var child=item.r;float anchor=Mathf.Lerp(child.anchorMin.y,child.anchorMax.y,child.pivot.y);
            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,item.h);
            child.anchoredPosition+=new Vector2(0,delta*(1-anchor));
        }
        if(background!=null)
        {
            float anchor=Mathf.Lerp(background.anchorMin.y,background.anchorMax.y,background.pivot.y);
            // Undo anchor movement, then grow from the background's own authored top.
            float anchorStretch=(background.anchorMax.y-background.anchorMin.y)*delta;
            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,background.rect.height-anchorStretch);
            background.anchoredPosition+=new Vector2(0,delta*(1-anchor));
            SetHeightKeepingTop(background,backgroundHeight+height-baseHeight);
        }
        var layout=panel.GetComponent<LayoutElement>();
        if(layout!=null)layout.minHeight=layout.preferredHeight=height;
        LayoutRebuilder.MarkLayoutForRebuild(panel);
    }
    public static void SetHeightKeepingTop(RectTransform rect,float height)
    {
        float delta=height-rect.rect.height;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        rect.anchoredPosition+=new Vector2(0,-delta*(1-rect.pivot.y));
    }
    public static Rect Bounds(RectTransform rect,Transform relativeTo)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        for(int i=0;i<4;i++)corners[i]=relativeTo!=null?relativeTo.InverseTransformPoint(corners[i]):corners[i];
        return Rect.MinMaxRect(corners.Min(p=>p.x),corners.Min(p=>p.y),corners.Max(p=>p.x),corners.Max(p=>p.y));
    }
    public static bool OverlapX(Rect a,Rect b)=>a.xMin<b.xMax-.1f&&a.xMax>b.xMin+.1f;
    public static void MoveVertical(RectTransform rect,Transform space,float amount)
    {
        var delta=space.TransformVector(new Vector3(0,amount,0));
        var local=rect.parent.InverseTransformVector(delta);
        rect.anchoredPosition+=new Vector2(0,local.y);
    }
}
