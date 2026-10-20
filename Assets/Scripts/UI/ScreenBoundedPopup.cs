using UnityEngine;

// Runs after popup placement, including tooltips positioned at the pointer.
[DefaultExecutionOrder(1000), RequireComponent(typeof(RectTransform))]
public sealed class ScreenBoundedPopup : MonoBehaviour
{
    Canvas canvas;
    RectTransform target;
    void Awake(){target=(RectTransform)transform;canvas=GetComponentInParent<Canvas>()?.rootCanvas;}
    void LateUpdate()
    {
        if(canvas==null||target==null||canvas.renderMode==RenderMode.WorldSpace)return;
        var safe=ResponsiveScreenLayout.GetSafeCanvasRect(canvas,Screen.safeArea);
        safe=Rect.MinMaxRect(safe.xMin+12,safe.yMin+12,safe.xMax-12,safe.yMax-12);
        var root=(RectTransform)canvas.transform;
        var bounds=ResponsiveScreenLayout.BoundsInCanvas(root,target,ResponsiveScreenLayout.MeasureContent(target));
        float fit=Mathf.Min(1,safe.width/Mathf.Max(1,bounds.size.x),safe.height/Mathf.Max(1,bounds.size.y));
        if(fit<1){target.localScale*=fit;bounds=ResponsiveScreenLayout.BoundsInCanvas(root,target,ResponsiveScreenLayout.MeasureContent(target));}
        float dx=bounds.size.x>safe.width?safe.center.x-bounds.center.x:bounds.min.x<safe.xMin?safe.xMin-bounds.min.x:bounds.max.x>safe.xMax?safe.xMax-bounds.max.x:0;
        float dy=bounds.size.y>safe.height?safe.center.y-bounds.center.y:bounds.min.y<safe.yMin?safe.yMin-bounds.min.y:bounds.max.y>safe.yMax?safe.yMax-bounds.max.y:0;
        target.position+=root.TransformVector(new Vector3(dx,dy,0));
    }
}
