using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Keep the legacy component name/GUID, and support both title menu buttons.
[DisallowMultipleComponent]
public sealed class TitlePlayHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    TitleMenuController owner;
    bool setting, hovering;
    Rect hoverBounds;
    Button button;
    public void Initialize(TitleMenuController controller, bool isSetting=false)
    {
        owner=controller;setting=isSetting;button=GetComponent<Button>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(Application.isMobilePlatform || hovering || owner==null || !button.IsInteractable())return;
        var corners=new Vector3[4];((RectTransform)transform).GetWorldCorners(corners);
        var camera=eventData!=null?eventData.enterEventCamera:null;
        Vector2 min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),max=min;
        foreach(var corner in corners){var p=RectTransformUtility.WorldToScreenPoint(camera,corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        hoverBounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        hovering=true;
        if(setting)owner.OnSettingPointerEnter();else owner.OnPlayPointerEnter();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if(Application.isMobilePlatform || !hovering)return;
        // A glitch changes the visible raycast surface; it must not count as moving the mouse away.
        if(eventData!=null && hoverBounds.Contains(eventData.position))return;
        ExitHover();
    }
    void LateUpdate()
    {
        if(hovering && (!button.IsInteractable() || (Mouse.current!=null && !hoverBounds.Contains(Mouse.current.position.ReadValue()))))ExitHover();
    }
    void ExitHover()
    {
        hovering=false;
        if(owner==null)return;
        if(setting)owner.OnSettingPointerExit();else owner.OnPlayPointerExit();
    }
    void OnDisable(){if(hovering)ExitHover();}
}