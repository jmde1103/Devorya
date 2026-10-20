using System;
using UnityEngine;
using UnityEngine.UI;

// Positions complete HUD groups in canvas coordinates, independently of legacy 100x100 parents.
[DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
public sealed class ResponsiveScreenLayout : MonoBehaviour
{
    [Serializable] public class Element
    {
        [InspectorName("대상 UI")] public RectTransform target;
        [InspectorName("화면 기준점")] public Vector2 anchor = new Vector2(.5f,.5f);
        [InspectorName("가장자리 여백·중앙 이동")] public Vector2 inset = new Vector2(24,24);
        [HideInInspector] public Bounds contentBounds;
        [HideInInspector] public Vector3 originalScale;
    }
    [SerializeField, InspectorName("반응형 UI 묶음")] public Element[] elements = Array.Empty<Element>();
    Canvas canvas;
    Rect lastSafe, lastPixels, lastCanvas;
    Vector2Int lastScreen;
    bool dirty=true;
    void Awake(){canvas=GetComponent<Canvas>();}
    void OnEnable(){dirty=true;}
    void OnRectTransformDimensionsChange(){dirty=true;}
    void LateUpdate()
    {
        if(canvas==null)canvas=GetComponent<Canvas>();
        // Resolve pending canvas geometry before positioning legacy nested HUD roots.
        if(dirty) Canvas.ForceUpdateCanvases();
        var rect=((RectTransform)canvas.transform).rect;
        var screen=new Vector2Int(Screen.width,Screen.height);
        if(dirty||lastSafe!=Screen.safeArea||lastPixels!=canvas.pixelRect||lastCanvas!=rect||screen!=lastScreen){
            lastSafe=Screen.safeArea;lastPixels=canvas.pixelRect;lastCanvas=rect;lastScreen=screen;dirty=false;
            ApplyLayout(GetSafeCanvasRect(canvas,Screen.safeArea));
        }
    }
    public static Rect GetSafeCanvasRect(Canvas canvas,Rect screenSafe)
    {
        Rect pixels=canvas.pixelRect, local=((RectTransform)canvas.transform).rect;
        if(pixels.width<=0||pixels.height<=0)return local;
        float x0=Mathf.Max(pixels.xMin,screenSafe.xMin),y0=Mathf.Max(pixels.yMin,screenSafe.yMin);
        float x1=Mathf.Min(pixels.xMax,screenSafe.xMax),y1=Mathf.Min(pixels.yMax,screenSafe.yMax);
        if(x1<=x0||y1<=y0)return local;
        return Rect.MinMaxRect(local.xMin+(x0-pixels.xMin)/pixels.width*local.width,local.yMin+(y0-pixels.yMin)/pixels.height*local.height,
            local.xMin+(x1-pixels.xMin)/pixels.width*local.width,local.yMin+(y1-pixels.yMin)/pixels.height*local.height);
    }
    public void ApplyLayout(Rect safe)
    {
        if(canvas==null)canvas=GetComponent<Canvas>();
        var root=(RectTransform)canvas.transform;
        foreach(var entry in elements){
            if(entry.target==null||entry.target.parent==null)continue;
            entry.target.localScale=entry.originalScale;
            Bounds bounds=BoundsInCanvas(root,entry.target,entry.contentBounds);
            float fit=Mathf.Min(1,(safe.width-48)/Mathf.Max(1,bounds.size.x),(safe.height-48)/Mathf.Max(1,bounds.size.y));
            if(fit<1){entry.target.localScale=entry.originalScale*Mathf.Max(.01f,fit);bounds=BoundsInCanvas(root,entry.target,entry.contentBounds);}
            Vector2 desired=new Vector2(Place(safe.xMin,safe.xMax,bounds.size.x,entry.anchor.x,entry.inset.x),Place(safe.yMin,safe.yMax,bounds.size.y,entry.anchor.y,entry.inset.y));
            desired.x=Mathf.Clamp(desired.x,safe.xMin+bounds.extents.x,safe.xMax-bounds.extents.x);
            desired.y=Mathf.Clamp(desired.y,safe.yMin+bounds.extents.y,safe.yMax-bounds.extents.y);
            Vector3 delta=root.TransformVector(new Vector3(desired.x-bounds.center.x,desired.y-bounds.center.y,0));
            entry.target.position+=delta;
        }
    }
    static float Place(float min,float max,float size,float anchor,float inset)
    {
        if(anchor<.25f)return min+inset+size*.5f;
        if(anchor>.75f)return max-inset-size*.5f;
        return (min+max)*.5f+inset;
    }
    public static Bounds MeasureContent(RectTransform target)
    {
        bool any=false;Bounds result=default;var corners=new Vector3[4];
        foreach(var graphic in target.GetComponentsInChildren<Graphic>(true)){
            graphic.rectTransform.GetWorldCorners(corners);
            foreach(var point in corners){var local=target.InverseTransformPoint(point);if(!any){result=new Bounds(local,Vector3.zero);any=true;}else result.Encapsulate(local);}
        }
        return any?result:new Bounds(target.rect.center,target.rect.size);
    }
    public static Bounds BoundsInCanvas(RectTransform canvas,RectTransform target,Bounds local)
    {
        var min=local.min;var max=local.max;
        Bounds result=new Bounds(canvas.InverseTransformPoint(target.TransformPoint(min)),Vector3.zero);
        result.Encapsulate(canvas.InverseTransformPoint(target.TransformPoint(new Vector3(max.x,min.y,0))));
        result.Encapsulate(canvas.InverseTransformPoint(target.TransformPoint(new Vector3(min.x,max.y,0))));
        result.Encapsulate(canvas.InverseTransformPoint(target.TransformPoint(max)));return result;
    }
}
