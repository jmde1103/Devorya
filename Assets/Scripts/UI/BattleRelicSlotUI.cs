using UnityEngine;
using UnityEngine.UI;

// <변경부분> 전투 유물 슬롯 하나의 아이콘 표시를 관리하는 UI
public class BattleRelicSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image relicIconImage;

    // <변경부분> 유물 슬롯을 꾹 눌렀을 때 유물 설명 팝업을 표시할 TooltipTrigger
    [SerializeField] private TooltipTrigger tooltipTrigger;

    // <변경부분> 현재 슬롯에 들어있는 유물 데이터에 맞게 아이콘을 갱신하는 함수
    public void Refresh(BattleRelicData relicData)
    {
        // 유물 데이터가 있고, 유물 타입이 None이 아니면 유물이 있는 상태
        bool hasRelic = relicData != null && relicData.relicType != BattleRelicType.None;

        // 유물 아이콘 이미지 갱신
        if (relicIconImage != null)
        {
            relicIconImage.sprite = hasRelic ? relicData.iconSprite : null;
            relicIconImage.enabled = hasRelic && relicData.iconSprite != null;
        }

        // <변경부분> TooltipTrigger가 인스펙터에 연결되지 않았다면 현재 오브젝트 또는 자식에서 찾음
        if (tooltipTrigger == null)
        {
            tooltipTrigger = GetComponent<TooltipTrigger>();

            if (tooltipTrigger == null)
            {
                tooltipTrigger = GetComponentInChildren<TooltipTrigger>();
            }
        }

        // <변경부분> 유물이 있는 슬롯에는 유물 데이터 기반 Tooltip을 연결하고, 빈 슬롯은 Tooltip을 제거
        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetTooltipViewData(
                hasRelic ? TooltipViewData.FromBattleRelicData(relicData) : null
            );
        }
    }
}
