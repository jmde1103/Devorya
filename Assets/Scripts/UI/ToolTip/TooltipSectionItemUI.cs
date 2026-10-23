using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> Tooltip 팝업 하단의 추가 설명 블록 하나를 표시하는 UI
// Text / StatusEffect 등 여러 Section 프리팹이 이 스크립트를 공통으로 사용할 수 있다.
public class TooltipSectionItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;

    // <변경부분> 상태효과 / 키워드 / 태그 같은 보조 분류 텍스트
    [SerializeField] private TMP_Text categoryText;

    // <변경부분> 설명 본문 텍스트
    [SerializeField] private TMP_Text mainDescriptionText;

    // <변경부분> 상태효과나 키워드 블록에 표시할 아이콘
    [SerializeField] private Image iconImage;

    // <변경부분> TooltipSectionData 내용을 UI에 반영
    public void Refresh(TooltipSectionData sectionData)
    {
        if (sectionData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        TooltipTextStyle.Apply(titleText, TooltipTextStyle.StatusRole(sectionData.statusEffectData), true);
        TooltipTextStyle.Apply(categoryText, TooltipTextRole.Secondary);
        TooltipTextStyle.Apply(mainDescriptionText, TooltipTextRole.Body);
        gameObject.SetActive(true);

        if (backgroundImage != null)
        {
            backgroundImage.color = sectionData.sectionColor;
        }

        if (titleText != null)
        {
            titleText.text = sectionData.sectionTitle;
        }

        if (categoryText != null)
        {
            categoryText.text = sectionData.sectionCategory;
            categoryText.gameObject.SetActive(string.IsNullOrEmpty(sectionData.sectionCategory) == false);
        }

        if (mainDescriptionText != null)
        {
            mainDescriptionText.text = sectionData.sectionDescription;
        }

        ResizeToDescription();

        if (iconImage != null)
        {
            iconImage.sprite = sectionData.sectionIcon;
            iconImage.enabled = sectionData.sectionIcon != null;
            iconImage.gameObject.SetActive(sectionData.sectionIcon != null);
        }
    }
    private float authoredHeight, authoredTextHeight, authoredBackgroundHeight;
    private Vector2[] authoredChildPositions;
    private RectTransform[] authoredChildren;

    // Grow downward without changing the prefab's authored width, header or icon placement.
    private void ResizeToDescription()
    {
        if (mainDescriptionText == null) return;
        var root = transform as RectTransform;
        var body = mainDescriptionText.rectTransform;
        if (root == null) return;
        if (authoredChildren == null)
        {
            authoredHeight = root.rect.height;
            authoredTextHeight = body.rect.height;
            authoredBackgroundHeight = backgroundImage != null ? backgroundImage.rectTransform.rect.height : 0;
            authoredChildren = new RectTransform[root.childCount];
            authoredChildPositions = new Vector2[root.childCount];
            for (int i = 0; i < root.childCount; i++)
            {
                authoredChildren[i] = root.GetChild(i) as RectTransform;
                if (authoredChildren[i] != null) authoredChildPositions[i] = authoredChildren[i].anchoredPosition;
            }
        }
        float height = Mathf.Max(authoredTextHeight, Mathf.Ceil(mainDescriptionText.GetPreferredValues(
            mainDescriptionText.text, Mathf.Max(1f, body.rect.width), Mathf.Infinity).y) + 1f);
        float extra = height - authoredTextHeight;
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, authoredHeight + extra);
        for (int i = 0; i < authoredChildren.Length; i++)
            if (authoredChildren[i] != null)
                authoredChildren[i].anchoredPosition = authoredChildPositions[i] + Vector2.up * extra * (1f - authoredChildren[i].anchorMax.y);
        body.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        body.anchoredPosition -= Vector2.up * extra * (1f - body.pivot.y);
        if (backgroundImage != null)
        {
            var background = backgroundImage.rectTransform;
            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, authoredBackgroundHeight + extra);
            background.anchoredPosition -= Vector2.up * extra * (1f - background.pivot.y);
            if (backgroundImage.sprite != null) backgroundImage.type = Image.Type.Sliced;
        }
        var layout = GetComponent<LayoutElement>();
        if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = authoredHeight + extra;
        mainDescriptionText.ForceMeshUpdate();
    }
}