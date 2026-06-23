using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> Tooltip 팝업 하단의 추가 설명 블록 하나를 표시하는 UI
public class TooltipSectionItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

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

        if (descriptionText != null)
        {
            descriptionText.text = sectionData.sectionDescription;
        }
    }
}
