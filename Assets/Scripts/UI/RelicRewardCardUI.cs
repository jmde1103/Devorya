using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using UnityEngine.EventSystems;

public sealed class RelicRewardCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, InspectorName("선택 버튼")] Button selectButton;
    [SerializeField, InspectorName("유물 아이콘")] Image relicIcon;
    [SerializeField, InspectorName("제목")] TMP_Text title;
    [SerializeField, InspectorName("설명")] TMP_Text description;
    [SerializeField, InspectorName("Spine 배경")] SkeletonGraphic spineGraphic;
    [SerializeField, InspectorName("Spine 애니메이션")] SkeletonAnimation spineAnimation;
    [SerializeField, InspectorName("등장 애니메이션 이름")] string openAnimation = "animation";
    [SerializeField, InspectorName("대기 애니메이션 이름")] string idleAnimation = "Idle";
    [SerializeField, InspectorName("배경 글리치")] PopupOpenAnimator spineGlitch;
    [SerializeField, InspectorName("아이콘 글리치")] PopupOpenAnimator iconGlitch;
    [SerializeField, InspectorName("팝업 글리치")] PopupOpenAnimator panelGlitch;
    [SerializeField, InspectorName("텍스트 글리치")] PopupOpenAnimator textGlitch;
    [SerializeField, InspectorName("희귀도 아이콘")] RectTransform[] diamonds;
    [SerializeField, Min(1), InspectorName("희귀도 간격")] float diamondSpacing = 28;
    [SerializeField, Min(.05f), InspectorName("희귀도 등장 시간")] float diamondDuration = .18f;
    public BattleRelicData Data { get; private set; }
    public bool Ready { get; private set; }
    [SerializeField, InspectorName("호버 확대 대상")] RectTransform visualRoot;
    [SerializeField, InspectorName("선택 아웃라인")] Outline[] outlines;
    RelicRarityPalette.Colors Colors => Data != null ? Data.RarityColors : RelicRarityPalette.Colors.Default;
    public Color ParticleColor => Colors.particles;
    bool hovered, selected;
    public void OnPointerEnter(PointerEventData e) { if (Ready && selectButton.interactable) hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = false; }
    public void MarkSelected() { selected = true; hovered = false; }
    void Update()
    {
        if (visualRoot == null) return;
        float desired = hovered && !selected ? 1.045f : 1f;
        visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, Vector3.one * desired, 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
        foreach (var outline in outlines)
        {
            outline.enabled = selected || hovered;
            var color = selected ? Colors.selected : Colors.hover;
            color.a *= .6f + .4f * Mathf.Sin(Time.unscaledTime * 9);
            outline.effectColor = color;
        }
    }
    // White card backgrounds use inverted base colors; semantic rich-text colors stay unchanged.
    static Color CardTextColor(Color color) => new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
    void SetTitle(string text) { TooltipTextStyle.Apply(title, TooltipTextRole.Title, true); title.color = CardTextColor(title.color); title.text = text; }
    void SetDescription(string text) { TooltipTextStyle.Apply(description, TooltipTextRole.Body); description.color = CardTextColor(description.color); description.text = TooltipTextStyle.Format(Data.localizedDescription, text); }
    public void Bind(BattleRelicData data, Action<RelicRewardCardUI> choose)
    {
        Unbind();
        Data = data; Ready = false;
        selectButton.interactable = false;
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => { if (Ready) choose(this); });
        relicIcon.sprite = data.iconSprite;
        SetTitle(data.relicName); SetDescription(data.description);
        if (!data.localizedRelicName.IsEmpty) data.localizedRelicName.StringChanged += SetTitle;
        if (!data.localizedDescription.IsEmpty) data.localizedDescription.StringChanged += SetDescription;
        foreach (var a in new[] { spineGlitch, iconGlitch, panelGlitch, textGlitch })
            if (a != null) a.gameObject.SetActive(false);
        foreach (var diamond in diamonds) diamond.gameObject.SetActive(false);
    }
    void Unbind()
    {
        if (Data == null) return;
        Data.localizedRelicName.StringChanged -= SetTitle;
        Data.localizedDescription.StringChanged -= SetDescription;
    }
    void OnDestroy() { Unbind(); }
    public void SetSelectable(bool value) { selectButton.interactable = value && Ready; }
    IEnumerator Glitch(PopupOpenAnimator animator)
    {
        if (animator == null) yield break;
        animator.gameObject.SetActive(true);
        if (animator == iconGlitch) animator.SetGlitchFlashColor(Colors.iconGlitch);
        animator.PlayOpen();
        while (animator.IsPlaying) yield return null;
    }
    public IEnumerator Reveal()
    {
        spineGlitch.gameObject.SetActive(true);
        spineGlitch.PlayOpen();
        spineGraphic.Initialize(false);
        spineAnimation.Initialize(false);
        var animation = spineGraphic.Skeleton.Data.FindAnimation(openAnimation);
        if (animation != null)
        {
            var track = spineAnimation.AnimationState.SetAnimation(0, openAnimation, false);
            while (!track.IsComplete) yield return null;
        }
        if (spineGraphic.Skeleton.Data.FindAnimation(idleAnimation) != null)
            spineAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
        while (spineGlitch.IsPlaying) yield return null;
        yield return Glitch(iconGlitch);
        yield return Glitch(panelGlitch);
        yield return Glitch(textGlitch);
        int count = Mathf.Clamp(Data.rarity, 1, Mathf.Min(3, diamonds.Length));
        for (int n = 1; n <= count; n++)
        {
            var added = diamonds[n - 1]; added.gameObject.SetActive(true);
            added.anchoredPosition = Vector2.zero;
            var starts = new Vector2[n];
            for (int j = 0; j < n; j++) starts[j] = diamonds[j].anchoredPosition;
            for (float elapsed = 0; elapsed < diamondDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / diamondDuration);
                for (int j = 0; j < n; j++)
                    diamonds[j].anchoredPosition = Vector2.Lerp(starts[j], new Vector2((j - (n - 1) * .5f) * diamondSpacing, 0), Mathf.SmoothStep(0, 1, t));
                added.localScale = Vector3.one * (t < .65f ? Mathf.Lerp(.2f, 1.25f, t / .65f) : Mathf.Lerp(1.25f, 1, (t - .65f) / .35f));
                added.GetComponent<CanvasGroup>().alpha = Mathf.Clamp01(t * 2);
                yield return null;
            }
            for (int j = 0; j < n; j++) diamonds[j].anchoredPosition = new Vector2((j - (n - 1) * .5f) * diamondSpacing, 0);
            added.localScale = Vector3.one; added.GetComponent<CanvasGroup>().alpha = 1;
        }
        Ready = true;
    }
}