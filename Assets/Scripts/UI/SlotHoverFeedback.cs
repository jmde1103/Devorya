using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SlotHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, InspectorName("확대 배율")] float hoverScale = 1.06f;
    [SerializeField, InspectorName("확대 반응 속도")] float speed = 14;
    [SerializeField, InspectorName("아웃라인 색")] Color outlineColor = Color.white;
    [SerializeField, InspectorName("아웃라인 두께")] Vector2 outlineSize = new Vector2(1.5f,-1.5f);
    bool selected;
    Color selectedColor;
    float selectedSpeed;
    public void SetSelected(bool value, Color color, float pulseSpeed)
    { selected=value; selectedColor=color; selectedSpeed=pulseSpeed; }
    Outline outline;
    Vector3 baseScale;
    bool initialized, hovered, available = true;
    void Awake() { Initialize(); }
    void Initialize()
    {
        if(initialized)return;
        initialized=true;baseScale=transform.localScale;
        var graphic=GetComponent<Graphic>();
        if(graphic==null)graphic=GetComponentInChildren<Image>(true);
        if(graphic!=null)
        {
            outline=graphic.GetComponent<Outline>();
            if(outline==null)outline=graphic.gameObject.AddComponent<Outline>();
            outline.effectDistance=outlineSize;outline.enabled=false;
        }
    }
    public void SetAvailable(bool value) { available=value;if(!value){hovered=false;Restore();} }
    public void OnPointerEnter(PointerEventData e) { if(available)hovered=true; }
    public void OnPointerExit(PointerEventData e) { hovered=false; }
    void LateUpdate()
    {
        if(!initialized)Initialize();
        // Opening animation owns the transform until it finishes.
        var opening=GetComponent<PopupOpenAnimator>();
        if(opening!=null&&opening.IsPlaying)return;
        transform.localScale=Vector3.Lerp(transform.localScale,baseScale*(hovered?hoverScale:1),1-Mathf.Exp(-speed*Time.unscaledDeltaTime));
        if(outline!=null){outline.enabled=(hovered||selected)&&available;
            var color=selected?selectedColor:outlineColor;
            if(selected)color.a*=.45f+.55f*(.5f+.5f*Mathf.Sin(Time.unscaledTime*selectedSpeed*Mathf.PI*2));
            outline.effectColor=color;}
    }
    void Restore(){if(initialized)transform.localScale=baseScale;if(outline!=null)outline.enabled=false;}
    void OnDisable(){selected=false;hovered=false;Restore();}
}