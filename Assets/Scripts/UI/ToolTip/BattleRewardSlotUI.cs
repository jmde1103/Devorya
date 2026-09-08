using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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

    // <변경부분>
    // 현재 공용 Reward Slot이 Recovery Piece를 표시하고 있는지 기록한다.
    private bool isRecoveryRewardSlot;

    // <변경부분>
    // Locale 변경 시 현재 기물 이름/설명을 다시 Resolve하기 위한 source.
    private PieceData cachedRecoveryPieceData;

    // <변경부분>
    // 현재 이 Reward Slot이 Gold를 표시하고 있는지 기록한다.
    private bool isGoldRewardSlot;

    // <변경부분>
    // Gold Tooltip의 정적 콘텐츠 SSOT.
    //
    // Locale 변경 시 TooltipData_Gold에서
    // 현재 Locale 문자열을 다시 resolve하기 위해 source를 보관한다.
    private TooltipData cachedGoldTooltipData;

    // <변경부분>
    // "현재 보유 금화: {gold}"를 다시 생성하기 위한
    // 현재 RunState Gold 수량.
    //
    // 실제 Gold 상태의 SSOT를 여기로 옮기는 것이 아니라,
    // 이미 RefreshGold()로 전달된 현재 표시값만 Runtime cache한다.
    private int cachedCurrentGoldAmount;

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

    // <변경부분>
    // Reward Popup이 열린 상태에서 Locale이 변경되면
    // Gold Tooltip의 문자열 데이터만 현재 Locale 기준으로 다시 만든다.
    private void OnEnable()
    {
        // 중복 등록 방지.
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;
    }

    // <변경부분>
    // 복구된 기물 Slot을 표시한다.
    //
    // PieceData:
    // 현재 Locale 기준 기물 이름 / 기물 자체 설명.
    //
    // Battle_UI:
    // Reward 복구 화면 전용 Category / 안내 문장.
    //
    // Ability 정보는 Recovery Tooltip에 표시하지 않는다.
    public void RefreshRecoveryPiece(
        PieceData pieceData,
        int amount)
    {
        ClearGoldRuntimeCache();
        ClearRecoveryRuntimeCache();

        if (pieceData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        isRecoveryRewardSlot =
            true;

        cachedRecoveryPieceData =
            pieceData;

        Sprite displayIcon =
            GetRecoveryTypeIcon(
                pieceData.pieceType
            );

        TooltipViewData recoveryTooltipViewData =
            CreateRecoveryTooltipViewData(
                pieceData
            );

        ApplyCommon(
            displayIcon,
            amount,
            recoveryTooltipViewData
        );
    }

    // <변경부분>
    // Recovery 화면에서 사용할 최종 TooltipViewData를 생성한다.
    //
    // PieceData가 소유하는 이름/설명을 현재 Locale 기준으로 가져온 뒤,
    // Reward 전용 Category와 복구 안내 문장만 조합한다.
    //
    // Ability / Trait 등 상세 전투 정보는
    // Recovery Tooltip에서 별도로 표시하지 않는다.
    private TooltipViewData CreateRecoveryTooltipViewData(
        PieceData pieceData)
    {
        if (pieceData == null)
        {
            return null;
        }

        TooltipViewData recoveryTooltipViewData =
            TooltipViewData.FromPieceData(
                pieceData
            );

        if (recoveryTooltipViewData == null)
        {
            return null;
        }

        string pieceDisplayName =
            recoveryTooltipViewData.title;

        string recoveryDescription =
    BattleUILocalization
        .GetRewardRecoveryDescription(
            pieceDisplayName
        );

        // PieceData 자체 Description은 그대로 보존하지만,
        // Recovery Reward Tooltip에서는 표시하지 않는다.
        //
        // 해당 Description은 이후 도감 / 기물 상세정보 등
        // PieceData 자체 정보를 보여주는 UI에서 재사용한다.
        recoveryTooltipViewData.category =
            BattleUILocalization
                .GetRewardRecoveryCategory();

        recoveryTooltipViewData.mainDescription =
            recoveryDescription;

        return recoveryTooltipViewData;
    }
    // <변경부분> 아이템 보상 슬롯 표시
    public void RefreshItem(
        BattleItemData itemData,
        int amount)
    {
        ClearGoldRuntimeCache();
        ClearRecoveryRuntimeCache();

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
        ClearGoldRuntimeCache();
        ClearRecoveryRuntimeCache();
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
    // Gold의 정적 콘텐츠인
    // title / category / mainDescription / icon은
    // TooltipData_Gold가 Localization SSOT로 직접 관리한다.
    //
    // "현재 보유 금화: {gold}"는 런타임 상태가 포함된 UI 문장이므로
    // Battle_UI Localization을 계속 사용한다.
    //
    // Locale 변경 시 이 함수에서 저장한 source와 현재 Gold 표시값을 이용해
    // Tooltip 문자열 데이터만 다시 생성한다.
    public void RefreshGold(
    TooltipData goldTooltipData,
    int acquiredGoldAmount,
    int currentGoldAmount)
    {
        ClearRecoveryRuntimeCache();

        if (goldTooltipData == null)
        {
            ClearGoldRuntimeCache();

            Debug.LogWarning(
                "금화 보상 슬롯 표시 실패: " +
                "goldTooltipData가 연결되지 않았습니다."
            );

            gameObject.SetActive(false);
            return;
        }

        // 현재 Slot이 Gold를 표시하고 있다는 사실과
        // Locale 재생성에 필요한 source만 Runtime으로 보관한다.
        isGoldRewardSlot =
            true;

        cachedGoldTooltipData =
            goldTooltipData;

        cachedCurrentGoldAmount =
            currentGoldAmount;

        TooltipViewData goldTooltipViewData =
            CreateGoldTooltipViewData(
                goldTooltipData,
                currentGoldAmount
            );

        // 획득 수량과 아이콘 표시 구조는 기존 그대로 유지한다.
        ApplyCommon(
            goldTooltipData.icon,
            acquiredGoldAmount,
            goldTooltipViewData
        );
    }

    // <변경부분>
    // Gold Tooltip의 표시용 ViewData를
    // 호출 시점의 현재 Locale 기준으로 생성한다.
    //
    // TooltipData_Gold:
    // title / category / mainDescription
    //
    // Battle_UI:
    // 현재 보유 Gold 문장
    //
    // 실제 Gold 숫자:
    // RefreshGold()로 전달된 RunState 기반 값
    private TooltipViewData CreateGoldTooltipViewData(
        TooltipData goldTooltipData,
        int currentGoldAmount)
    {
        if (goldTooltipData == null)
        {
            return null;
        }

        TooltipViewData goldTooltipViewData =
            TooltipViewData.FromTooltipData(
                goldTooltipData
            );

        if (goldTooltipViewData == null)
        {
            return null;
        }

        string baseDescription =
            goldTooltipViewData.mainDescription;

        string currentGoldText =
            BattleUILocalization
                .GetRewardCurrentGoldText(
                    currentGoldAmount
                );

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

        return goldTooltipViewData;
    }

    // <변경부분>
    // Reward Popup이 열린 상태에서 Locale이 변경되면
    // 현재 Slot Tooltip의 문자열 데이터만 다시 Resolve한다.
    //
    // Icon / Amount / Slot 활성화 / Popup Animation은
    // 다시 실행하지 않는다.
    private void OnSelectedLocaleChanged(
        Locale selectedLocale)
    {
        if (gameObject.activeInHierarchy == false ||
            tooltipTrigger == null)
        {
            return;
        }

        // Recovery Piece.
        if (isRecoveryRewardSlot &&
     cachedRecoveryPieceData != null)
        {
            TooltipViewData recoveryTooltipViewData =
                CreateRecoveryTooltipViewData(
                    cachedRecoveryPieceData
                );

            tooltipTrigger.SetTooltipViewData(
                recoveryTooltipViewData
            );

            return;
        }

        // Gold.
        if (isGoldRewardSlot &&
            cachedGoldTooltipData != null)
        {
            TooltipViewData localizedGoldTooltipViewData =
                CreateGoldTooltipViewData(
                    cachedGoldTooltipData,
                    cachedCurrentGoldAmount
                );

            tooltipTrigger.SetTooltipViewData(
                localizedGoldTooltipViewData
            );
        }
    }

    // <변경부분>
    // 공용 Reward Slot이 다른 보상 타입으로 재사용될 때
    // 이전 Recovery Piece source를 제거한다.
    private void ClearRecoveryRuntimeCache()
    {
        isRecoveryRewardSlot =
            false;

        cachedRecoveryPieceData =
            null;
    }

    // <변경부분>
    // 공용 Reward Slot이 Gold 이외의 보상으로 재사용될 때
    // 이전 Gold Locale refresh 정보가 남지 않도록 정리한다.
    private void ClearGoldRuntimeCache()
    {
        isGoldRewardSlot =
            false;

        cachedGoldTooltipData =
            null;

        cachedCurrentGoldAmount =
            0;
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