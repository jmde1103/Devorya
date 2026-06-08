using UnityEngine;
using UnityEngine.UI;

// <변경부분> 전투 유물 슬롯 하나의 아이콘 표시를 관리하는 UI
public class BattleRelicSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image relicIconImage;

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
    }
}
