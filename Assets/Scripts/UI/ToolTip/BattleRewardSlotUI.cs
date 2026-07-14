using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> 보상 팝업에 생성되는 공통 슬롯 하나를 관리
// 기물, 금화, 아이템, 유물이 모두 같은 슬롯 프리팹을 사용한다.
public class BattleRewardSlotUI : MonoBehaviour
{
    [Header("UI")]
    // <변경부분> 실제 보상 아이콘
    [SerializeField] private Image iconImage;

    // <변경부분> 보상 개수를 × 숫자 형식으로 표시
    [SerializeField] private TMP_Text amountText;

    [Header("Tooltip")]
    // <변경부분> 슬롯 전체를 꾹 눌렀을 때 기존 TooltipPopupUI를 호출
    [SerializeField] private TooltipTrigger tooltipTrigger;

    private void Awake()
    {
        // <변경부분> Inspector 연결이 빠져도 루트 또는 자식에서 자동 탐색
        if (tooltipTrigger == null)
        {
            tooltipTrigger =
                GetComponent<TooltipTrigger>();

            if (tooltipTrigger == null)
            {
                tooltipTrigger =
                    GetComponentInChildren<TooltipTrigger>(
                        true
                    );
            }
        }
    }

    // <변경부분> 복구된 기물 슬롯 표시
    public void RefreshRecoveryPiece(
        PieceData pieceData,
        int amount)
    {
        if (pieceData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        Sprite displayIcon =
            pieceData.playerStatusSprite != null
                ? pieceData.playerStatusSprite
                : pieceData.playerSprite;

        ApplyCommon(
            displayIcon,
            amount,
            TooltipViewData.FromPieceData(pieceData)
        );
    }

    // <변경부분> 아이템 보상 슬롯 표시
    public void RefreshItem(
        BattleItemData itemData,
        int amount)
    {
        if (itemData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        ApplyCommon(
            itemData.iconSprite,
            amount,
            TooltipViewData.FromBattleItemData(
                itemData
            )
        );
    }

    // <변경부분> 유물 보상 슬롯 표시
    public void RefreshRelic(
        BattleRelicData relicData,
        int amount)
    {
        if (relicData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        ApplyCommon(
            relicData.iconSprite,
            amount,
            TooltipViewData.FromBattleRelicData(
                relicData
            )
        );
    }

    // <변경부분> 금화 보상 슬롯 표시
    public void RefreshGold(
        Sprite goldIcon,
        int amount,
        TooltipData goldTooltipData)
    {
        ApplyCommon(
            goldIcon,
            amount,
            TooltipViewData.FromTooltipData(
                goldTooltipData
            )
        );
    }

    // <변경부분> 보상 종류와 관계없이 아이콘, 수량, Tooltip을 공통 적용
    private void ApplyCommon(
        Sprite icon,
        int amount,
        TooltipViewData tooltipViewData)
    {
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (amountText != null)
        {
            amountText.text =
                $"× {Mathf.Max(0, amount)}";
        }

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetTooltipViewData(
                tooltipViewData
            );
        }
    }
}