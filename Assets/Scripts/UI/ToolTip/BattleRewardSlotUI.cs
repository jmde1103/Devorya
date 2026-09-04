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

    [Header("Recovery Type Icons")]
    // <변경부분> Recovery 보상 슬롯에서 PieceType별로 표시할 타입 아이콘
    [SerializeField] private Sprite pawnTypeIconSprite;
    [SerializeField] private Sprite rookTypeIconSprite;
    [SerializeField] private Sprite bishopTypeIconSprite;
    [SerializeField] private Sprite knightTypeIconSprite;
    [SerializeField] private Sprite kingTypeIconSprite;
    [SerializeField] private Sprite queenTypeIconSprite;
    [SerializeField] private Sprite specialTypeIconSprite;

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
    // 기물 외형 이미지가 아니라 PieceType에 맞는 타입 아이콘을 사용한다.
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
            GetRecoveryTypeIcon(
                pieceData.pieceType
            );

        ApplyCommon(
            displayIcon,
            amount,
            TooltipViewData.FromPieceData(
                pieceData
            )
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

    // <변경부분> Gold Reward Tooltip 표시.
    //
    // Gold의 아이콘과 기존 한국어 원문은 현재 연결된 TooltipData를 유지하고,
    // 실제 플레이어 노출 title / category / description은
    // Battle_UI의 현재 Locale 문자열을 사용한다.
    //
    // 현재 별도 Gold/Currency Data owner가 존재하지 않으므로
    // Reward 화면에서 사용하는 Gold Tooltip 문자열만
    // Battle_UI의 Reward namespace에서 관리한다.
    public void RefreshGold(
        TooltipData goldTooltipData,
        int acquiredGoldAmount,
        int currentGoldAmount)
    {
        if (goldTooltipData == null)
        {
            Debug.LogWarning(
                "금화 보상 슬롯 표시 실패: " +
                "goldTooltipData가 연결되지 않았습니다."
            );

            gameObject.SetActive(false);
            return;
        }

        // ScriptableObject 원본 자체는 수정하지 않고
        // 현재 팝업 표시용 TooltipViewData만 생성한다.
        TooltipViewData goldTooltipViewData =
            TooltipViewData.FromTooltipData(
                goldTooltipData
            );

        if (goldTooltipViewData != null)
        {
            // 기존 TooltipData의 한국어 값은
            // Localization 누락 시 fallback으로 유지한다.
            goldTooltipViewData.title =
                BattleUILocalization
                    .GetRewardGoldTitle(
                        goldTooltipData.title
                    );

            goldTooltipViewData.category =
                BattleUILocalization
                    .GetRewardGoldCategory(
                        goldTooltipData.category
                    );

            string baseDescription =
                BattleUILocalization
                    .GetRewardGoldDescription(
                        goldTooltipData.mainDescription
                    );

            string currentGoldText =
                BattleUILocalization
                    .GetRewardCurrentGoldText(
                        currentGoldAmount
                    );

            // 기본 설명이 비어 있는 TooltipData라도
            // 현재 Gold 수량은 항상 정상 표시한다.
            if (string.IsNullOrWhiteSpace(
                    baseDescription))
            {
                goldTooltipViewData.mainDescription =
                    currentGoldText;
            }
            else
            {
                goldTooltipViewData.mainDescription =
                    $"{baseDescription}\n\n" +
                    $"{currentGoldText}";
            }
        }

        // 아이콘과 이번 전투에서 획득한 수량 표시는
        // Localization 대상이 아니므로 기존 구조를 그대로 유지한다.
        ApplyCommon(
            goldTooltipData.icon,
            acquiredGoldAmount,
            goldTooltipViewData
        );
    }

    // <변경부분> PieceType에 맞는 Recovery 타입 아이콘 반환
    private Sprite GetRecoveryTypeIcon(
        PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return pawnTypeIconSprite;

            case PieceType.Rook:
                return rookTypeIconSprite;

            case PieceType.Bishop:
                return bishopTypeIconSprite;

            case PieceType.Knight:
                return knightTypeIconSprite;

            case PieceType.King:
                return kingTypeIconSprite;

            case PieceType.Queen:
                return queenTypeIconSprite;

            case PieceType.Special:
                return specialTypeIconSprite;

            default:
                Debug.LogWarning(
                    $"Recovery 타입 아이콘 없음: {pieceType}"
                );

                return null;
        }
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
            iconImage.enabled =
                icon != null;
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