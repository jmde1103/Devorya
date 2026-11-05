using TMPro;
using UnityEngine;

public class RunGoldUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text goldAmountText;
    [Header("Display")]
    [SerializeField] private bool useThousandsSeparator = true;
    [SerializeField, Range(0.25f, 0.8f), InspectorName("골드 카운팅 시간")] private float countDuration = 0.5f;
    [SerializeField, Min(1f), InspectorName("숫자 확대 배율")] private float pulseScale = 1.12f;
    [SerializeField, Min(0.05f), InspectorName("숫자 확대 시간")] private float pulseDuration = 0.25f;
    [SerializeField, InspectorName("판매 예상 금액 색상")] private Color previewColor = new Color(0.2f, 0.85f, 0.3f);
    [SerializeField, InspectorName("판매 증감액 텍스트 (선택)")] private TMP_Text previewDeltaText;
    private RunStateManager source;
    private object previewOwner;
    private int previewAmount, actualGold, targetGold;
    private float displayedGold, startGold, elapsed, pulseElapsed;
    private bool initialized, counting, pulsing, captured;
    private Color originalColor;
    private Vector3 originalScale;
    private float deltaElapsed, deltaAlpha;
    private bool deltaClosing;
    public int DisplayedGold => Mathf.RoundToInt(displayedGold);
    public int TargetGold => targetGold;
    public bool IsPreviewing => previewOwner != null;

    private void OnEnable() { initialized = false; Bind(); }
    private void OnDisable()
    {
        if (source != null) source.GoldChanged -= OnGoldChanged;
        source = null; previewOwner = null; previewAmount = 0; counting = pulsing = false;
        if (goldAmountText != null && captured) { goldAmountText.color = originalColor; goldAmountText.transform.localScale = originalScale; }
        if (previewDeltaText != null) previewDeltaText.gameObject.SetActive(false);
        initialized = false;
    }
    private void Bind()
    {
        var manager = RunStateManager.Instance;
        if (source == manager && initialized) return;
        if (source != null) source.GoldChanged -= OnGoldChanged;
        source = manager;
        if (source == null) return;
        Capture(); source.GoldChanged += OnGoldChanged;
        actualGold = source.GetGoldAmount(); displayedGold = targetGold = actualGold;
        counting = pulsing = false; initialized = true; Render();
    }
    private void Capture()
    {
        if (captured || goldAmountText == null) return;
        originalColor = goldAmountText.color; originalScale = goldAmountText.transform.localScale; captured = true;
    }
    // External refresh requests must not snap an in-progress change animation.
    public void RefreshImmediately() { Bind(); if (source != null) OnGoldChanged(source.GetGoldAmount()); }
    private void OnGoldChanged(int value)
    {
        actualGold = value;
        SetTarget((int)System.Math.Min(int.MaxValue, (long)value + previewAmount));
    }
    private void SetTarget(int value)
    {
        if (!initialized) { displayedGold = targetGold = value; initialized = true; Render(); return; }
        if (value == targetGold) return;
        startGold = displayedGold; targetGold = value; elapsed = pulseElapsed = 0f;
        counting = !Mathf.Approximately(startGold, value); pulsing = counting;
    }
    public void ShowSalePreview(object owner, int amount)
    {
        if (owner == null) return;
        Bind(); previewOwner = owner; previewAmount = Mathf.Max(0, amount);
        if (goldAmountText != null) goldAmountText.color = previewColor;
        EnsureDeltaText(); deltaClosing = false; deltaElapsed = 0; deltaAlpha = 1;
        if (previewDeltaText != null) { previewDeltaText.text = "+" + previewAmount + "G"; previewDeltaText.gameObject.SetActive(true); }
        OnGoldChanged(source != null ? source.GetGoldAmount() : actualGold);
    }
    public void EndSalePreview(object owner, bool committing = false)
    {
        if (!ReferenceEquals(previewOwner, owner)) return;
        previewOwner = null; previewAmount = 0; deltaClosing = true; deltaElapsed = 0;
        if (goldAmountText != null && captured) goldAmountText.color = originalColor;
        // The following transaction event targets the same number already previewed.
        if (!committing) OnGoldChanged(source != null ? source.GetGoldAmount() : actualGold);
    }
    private readonly Vector3[] deltaBounds = new Vector3[4];
    private void EnsureDeltaText()
    {
        if (goldAmountText == null) return;
        if (previewDeltaText == null)
        {
            var go = new GameObject("SaleGoldDelta", typeof(RectTransform));
            go.transform.SetParent(goldAmountText.transform.parent, false);
            previewDeltaText = go.AddComponent<TextMeshProUGUI>();
        }
        previewDeltaText.font = goldAmountText.font;
        previewDeltaText.fontSharedMaterial = goldAmountText.fontSharedMaterial;
        previewDeltaText.fontSize = goldAmountText.fontSize;
        previewDeltaText.enableAutoSizing = false;
        previewDeltaText.alignment = TextAlignmentOptions.Center;
        previewDeltaText.raycastTarget = false;
        LayoutDeltaText();
    }
    private void LateUpdate()
    {
        if (previewDeltaText != null && previewDeltaText.gameObject.activeSelf) LayoutDeltaText();
    }
    private void LayoutDeltaText()
    {
        if (goldAmountText == null || previewDeltaText == null) return;
        var rect = previewDeltaText.rectTransform;
        var parent = goldAmountText.transform.parent as RectTransform;
        if (parent == null) return;
        if (rect.parent != parent) rect.SetParent(parent, false);
        var background = goldAmountText.transform.parent.GetComponentInParent<UnityEngine.UI.Image>();
        var boundsRect = background != null ? background.rectTransform : goldAmountText.rectTransform;
        boundsRect.GetWorldCorners(deltaBounds);
        float top = float.NegativeInfinity;
        foreach(var point in deltaBounds) top = Mathf.Max(top, parent.InverseTransformPoint(point).y);
        rect.anchorMin = rect.anchorMax = parent.pivot;
        rect.pivot = new Vector2(.5f,.5f);
        rect.sizeDelta = new Vector2(Mathf.Max(goldAmountText.rectTransform.rect.width, previewDeltaText.preferredWidth), goldAmountText.fontSize * 1.4f);
        Vector3 center = parent.InverseTransformPoint(goldAmountText.rectTransform.TransformPoint(goldAmountText.rectTransform.rect.center));
        float gap = goldAmountText.fontSize * .15f * Mathf.Abs(originalScale.y);
        rect.anchoredPosition = new Vector2(center.x, top + gap + rect.rect.height * Mathf.Abs(originalScale.y) * .5f);
    }
    private void Update()
    {
        Bind(); if (!initialized) return;
        float dt = Time.unscaledDeltaTime;
        if (counting)
        {
            elapsed += dt; float t = Mathf.Clamp01(elapsed / Mathf.Clamp(countDuration, 0.25f, 0.8f));
            displayedGold = Mathf.Lerp(startGold, targetGold, 1f - Mathf.Pow(1f - t, 3));
            if (t >= 1f) { displayedGold = targetGold; counting = false; }
            Render();
        }
        if (pulsing && goldAmountText != null)
        {
            pulseElapsed += dt; float t = Mathf.Clamp01(pulseElapsed / Mathf.Max(0.05f, pulseDuration));
            goldAmountText.transform.localScale = originalScale * (1f + (pulseScale - 1f) * Mathf.Sin(Mathf.PI * t));
            if (t >= 1) { goldAmountText.transform.localScale = originalScale; pulsing = false; }
        }
        if (previewDeltaText != null && previewDeltaText.gameObject.activeSelf)
        {
            deltaElapsed += dt; float t = Mathf.Clamp01(deltaElapsed / 0.25f);
            float scale = deltaClosing ? Mathf.Lerp(1f, 0.9f, t) : Mathf.Lerp(0.9f, 1f, t) + 0.2f * Mathf.Sin(Mathf.PI*t);
            previewDeltaText.transform.localScale = originalScale * scale;
            previewDeltaText.color = new Color(1,1,1,deltaClosing ? 1f-t : deltaAlpha);
            if (deltaClosing && t >= 1) previewDeltaText.gameObject.SetActive(false);
        }
    }
    private void Render()
    {
        if (goldAmountText != null) goldAmountText.text = (useThousandsSeparator ? DisplayedGold.ToString("N0") : DisplayedGold.ToString()) + "<space=0.08em>G";
    }
}
