using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// <변경부분> 전투 유물 슬롯 하나의 아이콘 표시를 관리하는 UI
public class BattleRelicSlotUI : MonoBehaviour
{


    // 현재 슬롯에 표시 중인 유물 데이터.
    //
    // Locale 변경 시 아이콘이나 슬롯 상태는 건드리지 않고
    // Tooltip만 현재 언어 기준으로 다시 생성하기 위해 저장한다.
    private BattleRelicData currentRelicData;

    [Header("UI")]
    [SerializeField] private Image relicIconImage;

    // <변경부분> 유물 슬롯을 꾹 눌렀을 때 유물 설명 팝업을 표시할 TooltipTrigger
    [SerializeField] private TooltipTrigger tooltipTrigger;

    // 슬롯이 활성화되어 있는 동안 Locale 변경 이벤트를 구독한다.
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    // 비활성화 시 반드시 이벤트 구독을 해제한다.
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;
    }

    // 게임 실행 중 언어가 변경되면
    // 현재 유물 Tooltip만 새로운 Locale 기준으로 다시 만든다.
    private void OnSelectedLocaleChanged(
        Locale locale)
    {
        RefreshTooltip();
    }

    // 현재 슬롯의 유물 데이터에 맞게
    // 아이콘과 Tooltip을 갱신한다.
    public void Refresh(
        BattleRelicData relicData)
    {
        bool hasRelic =
            relicData != null &&
            relicData.relicType != BattleRelicType.None;

        // Locale 변경 시 Tooltip을 다시 만들 수 있도록
        // 현재 실제 유물 데이터를 보관한다.
        currentRelicData =
            hasRelic
                ? relicData
                : null;

        // 유물 아이콘 갱신.
        if (relicIconImage != null)
        {
            relicIconImage.sprite =
                hasRelic
                    ? relicData.iconSprite
                    : null;

            relicIconImage.enabled =
                hasRelic &&
                relicData.iconSprite != null;
        }

        // TooltipTrigger가 Inspector에 연결되지 않았다면
        // 현재 오브젝트 또는 자식에서 자동으로 찾는다.
        if (tooltipTrigger == null)
        {
            tooltipTrigger =
                GetComponent<TooltipTrigger>();

            if (tooltipTrigger == null)
            {
                tooltipTrigger =
                    GetComponentInChildren<TooltipTrigger>();
            }
        }

        // 현재 Locale 기준으로 Tooltip 데이터를 생성한다.
        RefreshTooltip();
    }

    // 현재 유물 데이터와 현재 Locale을 기준으로
    // TooltipViewData를 다시 생성한다.
    //
    // Locale 변경 시에는 아이콘이나 슬롯 상태를 갱신하지 않고
    // 이 함수만 다시 호출한다.
    private void RefreshTooltip()
    {
        if (tooltipTrigger == null)
        {
            return;
        }

        tooltipTrigger.SetTooltipViewData(
            currentRelicData != null
                ? TooltipViewData.FromBattleRelicData(
                    currentRelicData
                )
                : null
        );
    }
}
