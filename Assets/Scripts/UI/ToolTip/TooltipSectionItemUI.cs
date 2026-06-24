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

        if (iconImage != null)
        {
            iconImage.sprite = sectionData.sectionIcon;
            iconImage.enabled = sectionData.sectionIcon != null;
            iconImage.gameObject.SetActive(sectionData.sectionIcon != null);
        }
    }
}