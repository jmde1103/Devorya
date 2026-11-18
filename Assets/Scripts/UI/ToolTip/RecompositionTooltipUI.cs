using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Author-owned Canvas layout; only temporary vertical overflow corrections are permitted.
[DefaultExecutionOrder(100)]
public sealed class RecompositionTooltipUI : MonoBehaviour
{
    [SerializeField, InspectorName("기물 이름")] private TMP_Text pieceNameText;
    [SerializeField, InspectorName("기물 타입")] private TMP_Text pieceTypeText;
    [SerializeField, InspectorName("능력 이름")] private TMP_Text abilityNameText;
    [SerializeField, InspectorName("능력 분류")] private TMP_Text abilityCategoryText;
    [SerializeField, InspectorName("능력 설명")] private TMP_Text abilityDescriptionText;
    [SerializeField, InspectorName("능력 아이콘")] private Image abilityIcon;
    [SerializeField, InspectorName("상태효과 영역")] private GameObject statusEffectPanel;
    [SerializeField, InspectorName("상태효과 목록")] private RectTransform statusEffectList;
    [SerializeField, InspectorName("상태효과 카드")] private RecompositionStatusEffectCard statusCardPrefab;
    [SerializeField, InspectorName("등장 연출")] private PopupOpenAnimator openAnimator;
    [SerializeField, InspectorName("편집 화면 예시 카드")] private GameObject editorPreviewCard;
    [SerializeField, Min(0f), InspectorName("능력과 상태효과 최소 간격")] private float abilityStatusMinimumGap = 14f;
    private RectTransform abilityBackground;
    private float abilityBackgroundBaselineBottom;
    private readonly List<RecompositionStatusEffectCard> cards = new List<RecompositionStatusEffectCard>();
    private TooltipViewData currentView;
    private bool initialized, statusEnabled, iconEnabled, hasAbility;
    private RecompositionVerticalPanel headerHeight, abilityHeight;
    private RectTransform statusRect;
    private Vector2 statusPosition;
    private Rect statusBaseline;
    private RecompositionStatusEffectCard cardTemplate;
    public TooltipViewData CurrentView => currentView;
    public int CardCount => cards.Count;
    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        var header=transform.Find("HeaderPanel") as RectTransform;
        var ability=transform.Find("AbilityPanel") as RectTransform;
        if(header!=null)headerHeight=new RecompositionVerticalPanel(header,pieceNameText,pieceTypeText);
        if(ability!=null)abilityHeight=new RecompositionVerticalPanel(ability,abilityNameText,abilityCategoryText,abilityDescriptionText);
        abilityBackground=ability!=null?ability.Find("Background") as RectTransform:null;
        if(abilityBackground==null)abilityBackground=ability;
        if(abilityBackground!=null)abilityBackgroundBaselineBottom=RecompositionVerticalPanel.Bounds(abilityBackground,transform).yMin;
        statusRect=statusEffectPanel!=null?statusEffectPanel.transform as RectTransform:null;
        if(statusRect!=null){statusPosition=statusRect.anchoredPosition;statusBaseline=RecompositionVerticalPanel.Bounds(statusRect,transform);}
        if(editorPreviewCard!=null)
        {
            // Resolve the existing list's height before hiding the template. Scene overrides remain intact.
            if(statusEffectList!=null)LayoutRebuilder.ForceRebuildLayoutImmediate(statusEffectList);
            cardTemplate=editorPreviewCard.GetComponent<RecompositionStatusEffectCard>();
            if(Application.isPlaying)editorPreviewCard.SetActive(false);
        }
        if(cardTemplate==null)cardTemplate=statusCardPrefab;
        statusEnabled = statusEffectPanel != null && statusEffectPanel.activeSelf;
        iconEnabled = abilityIcon != null && abilityIcon.enabled;
    }
    private void Awake() { Initialize(); if (currentView == null) gameObject.SetActive(false); }
    public bool IsShowing(TooltipViewData view) => view != null && currentView == view && gameObject.activeInHierarchy;
    public void HideIfShowing(TooltipViewData view) { if (view != null && currentView == view) Hide(); }
    public void Show(TooltipViewData view)
    {
        if (view == null) { Hide(); return; }
        Initialize();
        if (openAnimator != null && openAnimator.IsPlaying) openAnimator.CompleteImmediately();
        ClearCards();
        currentView = view;
        SetText(pieceNameText, view.title, TooltipTextRole.Title, true);
        SetText(pieceTypeText, view.category, TooltipTextRole.Secondary);
        TooltipSectionData ability = null;
        if (view.sections != null)
            foreach (var section in view.sections)
            {
                if (section == null) continue;
                if (section.sectionType == TooltipSectionType.Ability) { ability = section; continue; }
                var resolved = section.CreateResolvedCopy();
                if (resolved == null || cardTemplate == null || statusEffectList == null) continue;
                var card = Instantiate(cardTemplate, statusEffectList, false);
                card.name = "StatusEffectCard";
                card.Refresh(resolved);
                card.gameObject.SetActive(true);
                cards.Add(card);
            }
        hasAbility=ability!=null;
        if(abilityHeight!=null)abilityHeight.Panel.gameObject.SetActive(hasAbility);
        SetText(abilityNameText, ability?.sectionTitle ?? string.Empty, TooltipTextRole.Title, true);
        SetText(abilityCategoryText, ability?.sectionCategory ?? string.Empty, TooltipTextRole.Secondary);
        SetText(abilityDescriptionText, ability?.sectionDescription ?? string.Empty, TooltipTextRole.Body);
        if (abilityIcon != null) { abilityIcon.sprite = ability?.sectionIcon; abilityIcon.enabled = iconEnabled && abilityIcon.sprite != null; }
        if (statusEffectPanel != null) statusEffectPanel.SetActive(statusEnabled && cards.Count > 0);
        gameObject.SetActive(true);
        ResizeInformation();
        if (Application.isPlaying && openAnimator != null) openAnimator.PlayOpen();
    }
    private void RestoreInformation()
    {
        headerHeight?.Restore();abilityHeight?.Restore();
        if(statusRect!=null){var p=statusRect.anchoredPosition;p.y=statusPosition.y;statusRect.anchoredPosition=p;}
    }
    private void ResizeInformation()
    {
        RestoreInformation();headerHeight?.Resize();
        if(hasAbility)abilityHeight?.Resize();
        if(hasAbility&&headerHeight!=null&&abilityHeight!=null)MoveBelow(headerHeight,abilityHeight.Panel,abilityHeight.BaselineBounds);
        if(statusRect!=null&&abilityHeight==null&&headerHeight!=null)
            MoveBelow(headerHeight,statusRect,statusBaseline);
        if(statusEffectList!=null)LayoutRebuilder.ForceRebuildLayoutImmediate(statusEffectList);
        PlaceStatusBelowAbility();
    }
    private void PlaceStatusBelowAbility()
    {
        if(statusRect==null||cards.Count==0)return;
        if(!hasAbility)
        {
            if(headerHeight==null)return;
            var headerBackground=headerHeight.Panel.Find("Background") as RectTransform;
            float headerBottom=RecompositionVerticalPanel.Bounds(headerBackground!=null?headerBackground:headerHeight.Panel,transform).yMin;
            RecompositionVerticalPanel.MoveVertical(statusRect,transform,headerBottom-Mathf.Max(0f,abilityStatusMinimumGap)-StatusVisualTop());
            return;
        }
        if(abilityBackground==null)return;
        // Measure the final rendered background after both text growth and Header displacement.
        var bottom=RecompositionVerticalPanel.Bounds(abilityBackground,transform).yMin;
        // Short descriptions retain the authored layout, even when it uses a deliberately small gap.
        if(bottom>=abilityBackgroundBaselineBottom-.1f)return;
        float missingGap=StatusVisualTop()+Mathf.Max(0f,abilityStatusMinimumGap)-bottom;
        if(missingGap>0f)RecompositionVerticalPanel.MoveVertical(statusRect,transform,-missingGap);
    }
    private float StatusVisualTop()
    {
        float top=float.NegativeInfinity;
        foreach(var card in cards)
        {
            if(card==null||!card.gameObject.activeSelf)continue;
            var background=card.transform.Find("Background") as RectTransform;
            top=Mathf.Max(top,RecompositionVerticalPanel.Bounds(background!=null?background:(RectTransform)card.transform,transform).yMax);
        }
        if(float.IsNegativeInfinity(top))
            top=RecompositionVerticalPanel.Bounds(statusEffectList!=null?statusEffectList:statusRect,transform).yMax;
        return top;
    }
    private void MoveBelow(RecompositionVerticalPanel upper,RectTransform lower,Rect originalLower)
    {
        var before=upper.BaselineBounds;
        var now=RecompositionVerticalPanel.Bounds(upper.Panel,transform);
        var target=RecompositionVerticalPanel.Bounds(lower,transform);
        if(!RecompositionVerticalPanel.OverlapX(now,target)||originalLower.yMax>before.yMax||now.yMin>=before.yMin-.1f)return;
        if(target.yMax<=now.yMin+.1f)return;
        float gap=Mathf.Max(0,before.yMin-originalLower.yMax);
        float excess=target.yMax-(now.yMin-gap);
        if(excess>0)RecompositionVerticalPanel.MoveVertical(lower,transform,-excess);
    }
    internal static void SetText(TMP_Text target, string value, TooltipTextRole role, bool bold = false)
    {
        if (target == null) return;
        target.text = value ?? string.Empty;
        // Keep the authored font, Auto Size, color and style. Semantic rich-text tags remain in the supplied text.
    }
    private void ClearCards()
    {
        foreach (var card in cards)
            if (card != null) { card.gameObject.SetActive(false); if (Application.isPlaying) Destroy(card.gameObject); else DestroyImmediate(card.gameObject); }
        cards.Clear();
    }
    public void Hide()
    {
        if (openAnimator != null && openAnimator.IsPlaying) openAnimator.CompleteImmediately();
        currentView = null;
        ClearCards();
        RestoreInformation();
        gameObject.SetActive(false);
    }
}
