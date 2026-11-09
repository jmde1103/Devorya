

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class WorldMapMenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, ISubmitHandler
{
    [Header("Visuals")]
    [SerializeField] private RectTransform normal;
    [SerializeField] private RectTransform hover;
    [SerializeField, Min(0f)]
    private float hoverFadeDuration = 0.12f;

    [Header("Click Icon Feedback")]
    [SerializeField] private RectTransform normalIcon;
    [SerializeField] private RectTransform hoverIcon;

    [SerializeField, Min(1f)]
    private float clickScaleMultiplier = 1.1f;

    [SerializeField, Min(0f)]
    private float clickExpandDuration = 0.06f;

    [SerializeField, Min(0f)]
    private float clickReturnDuration = 0.10f;

    private Button clickButton;
    private CanvasGroup rootGroup;
    private CanvasGroup normalGroup;
    private CanvasGroup hoverGroup;
    private Coroutine fadeRoutine;
    private Coroutine clickRoutine;
    private Vector3 normalIconOriginalScale;
    private Vector3 hoverIconOriginalScale;
    private bool prepared;
    private bool available;

    public RectTransform MenuRect => (RectTransform)transform;
    public CanvasGroup RootGroup => rootGroup;

    private void Awake()
    {
        Prepare();
    }

    private void OnDisable()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        StopClickFeedback();
        SetHoverAmount(0f);
    }

    public bool Prepare()
    {
        if (prepared)
            return rootGroup != null &&
                   normalGroup != null &&
                   hoverGroup != null;

        prepared = true;
        clickButton = GetComponent<Button>();

        if (normal == null)
            normal = transform.Find("Normal") as RectTransform;

        if (hover == null)
            hover = transform.Find("Hover") as RectTransform;

        if (normal == null || hover == null)
        {
            Debug.LogError(
                "[WorldMapMenuButton] Normal/Hover 자식이 없습니다.",
                this);
            return false;
        }

        rootGroup = GetOrAddGroup(gameObject);
        normalGroup = GetOrAddGroup(normal.gameObject);
        hoverGroup = GetOrAddGroup(hover.gameObject);

        // Normal / Hover 아래 Icon을 자동 탐색한다.
        // 이름이 다르면 Inspector에서 아이콘을 직접 연결한다.
        if (normalIcon == null)
            normalIcon = normal.Find("Icon") as RectTransform;

        if (hoverIcon == null)
            hoverIcon = hover.Find("Icon") as RectTransform;

        if (normalIcon != null)
            normalIconOriginalScale = normalIcon.localScale;

        if (hoverIcon != null)
            hoverIconOriginalScale = hoverIcon.localScale;

        if (normalIcon == null || hoverIcon == null)
        {
            Debug.LogWarning(
                "[WorldMapMenuButton] Normal/Hover의 Icon 연결이 없어 " +
                "클릭 확대 효과는 실행되지 않습니다.",
                this);
        }

        // 시각 오브젝트는 클릭을 가로채지 않는다.
        normalGroup.blocksRaycasts = false;
        normalGroup.interactable = false;
        hoverGroup.blocksRaycasts = false;
        hoverGroup.interactable = false;

        SetHoverAmount(0f);
        SetAvailable(false);
        return true;
    }

    public void SetAvailable(bool value)
    {
        if (!prepared && !Prepare()) return;
        if (rootGroup == null) return;

        available = value;
        clickButton.interactable = value;
        rootGroup.interactable = value;
        rootGroup.blocksRaycasts = value;

        if (!value)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            SetHoverAmount(0f);
            StopClickFeedback();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (available)
            StartHoverFade(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (available)
            StartHoverFade(false);
    }

    // 마우스/터치 누름 시 클릭 반응을 먼저 보여준다.
    // 기존 Button.onClick 이벤트는 그대로 유지한다.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PlayClickFeedback();
    }

    // 키보드/게임패드의 Submit 입력에도 동일한 반응을 준다.
    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickFeedback();
    }

    private void StartHoverFade(bool hovered)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(
            FadeHover(hovered ? 1f : 0f));
    }

    private IEnumerator FadeHover(float target)
    {
        float start = hoverGroup.alpha;
        float elapsed = 0f;

        if (hoverFadeDuration <= 0f)
        {
            SetHoverAmount(target);
            fadeRoutine = null;
            yield break;
        }

        while (elapsed < hoverFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / hoverFadeDuration);

            SetHoverAmount(
                Mathf.Lerp(start, target, t));

            yield return null;
        }

        SetHoverAmount(target);
        fadeRoutine = null;
    }

    private void SetHoverAmount(float amount)
    {
        if (normalGroup != null)
            normalGroup.alpha = 1f - amount;

        if (hoverGroup != null)
            hoverGroup.alpha = amount;
    }

    private void PlayClickFeedback()
    {
        if (!available || !isActiveAndEnabled ||
            normalIcon == null || hoverIcon == null)
            return;

        // 빠르게 반복 클릭해도 기존 애니메이션이 누적되지 않는다.
        StopClickFeedback();
        clickRoutine = StartCoroutine(AnimateClick());
    }

    private IEnumerator AnimateClick()
    {
        float targetScale = Mathf.Max(1f, clickScaleMultiplier);

        yield return AnimateIconScale(
            1f, targetScale, clickExpandDuration);

        yield return AnimateIconScale(
            targetScale, 1f, clickReturnDuration);

        SetIconScale(1f);
        clickRoutine = null;
    }

    private IEnumerator AnimateIconScale(
        float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetIconScale(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            SetIconScale(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetIconScale(to);
    }

    private void SetIconScale(float scaleMultiplier)
    {
        if (normalIcon != null)
            normalIcon.localScale = normalIconOriginalScale * scaleMultiplier;

        if (hoverIcon != null)
            hoverIcon.localScale = hoverIconOriginalScale * scaleMultiplier;
    }

    private void StopClickFeedback()
    {
        if (clickRoutine != null)
        {
            StopCoroutine(clickRoutine);
            clickRoutine = null;
        }

        SetIconScale(1f);
    }

    private static CanvasGroup GetOrAddGroup(GameObject target)
    {
        CanvasGroup group =
            target.GetComponent<CanvasGroup>();

        return group != null
            ? group
            : target.AddComponent<CanvasGroup>();
    }
}
