using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> 스테이터스 창에 표시되는 상태이상 슬롯 UI
public class StatusEffectSlotUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Icon")]
    [SerializeField] private Image iconImage;

    [Header("Texts")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text stackText;

    // <변경부분> 상태이상 슬롯을 빈 상태로 초기화하는 함수
    public void Clear()
    {
        // 슬롯 루트 비활성화
        if (root != null)
        {
            root.SetActive(false);
        }

        // 아이콘 초기화
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        // 남은 턴 텍스트 초기화
        if (turnText != null)
        {
            turnText.text = "";
        }

        // 중첩 텍스트 초기화
        if (stackText != null)
        {
            stackText.text = "";
        }
    }

    // <변경부분> 상태이상 데이터를 슬롯에 표시하는 함수
    public void Refresh(StatusEffectData statusEffectData, OwnedStatusEffectData ownedStatusEffectData)
    {
        // 표시할 상태이상 정보가 없으면 슬롯 비움
        if (statusEffectData == null || ownedStatusEffectData == null)
        {
            Clear();
            return;
        }

        // 슬롯 루트 활성화
        if (root != null)
        {
            root.SetActive(true);
        }

        // 상태이상 아이콘 표시
        if (iconImage != null)
        {
            iconImage.sprite = statusEffectData.iconSprite;
            iconImage.enabled = statusEffectData.iconSprite != null;
            iconImage.preserveAspect = true;
        }

        // 남은 턴 표시
        if (turnText != null)
        {
            turnText.text = ownedStatusEffectData.remainingTurn.ToString();
        }

        // 중첩 수 표시
        // 현재 퇴화는 1중첩 기준이므로 2 이상일 때만 표시
        if (stackText != null)
        {
            if (ownedStatusEffectData.stackCount > 1)
            {
                stackText.text = ownedStatusEffectData.stackCount.ToString();
            }
            else
            {
                stackText.text = "";
            }
        }
    }
}
