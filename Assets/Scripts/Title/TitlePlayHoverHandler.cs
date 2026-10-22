using UnityEngine;
using UnityEngine.EventSystems;

// Forward hover events from the existing Play button without replacing its listeners.
[DisallowMultipleComponent]
public sealed class TitlePlayHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TitleMenuController owner;
    public void Initialize(TitleMenuController controller) => owner = controller;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.OnPlayPointerEnter();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.OnPlayPointerExit();
    }
}
