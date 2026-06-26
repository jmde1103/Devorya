using UnityEngine;
using UnityEngine.UI;

// <변경부분> 전투 아이템 슬롯 하나의 아이콘 표시와 클릭 입력을 관리하는 UI
public class BattleItemSlotUI : MonoBehaviour
{
    // 이 슬롯을 관리하는 상위 전투 UI 컨트롤러
    private BattleUIController battleUIController;

    // 이 슬롯의 번호
    private int slotIndex;

    [Header("UI")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image itemIconImage;

    // <변경부분> 아이템 슬롯을 꾹 눌렀을 때 아이템 설명 팝업을 표시할 TooltipTrigger
    [SerializeField] private TooltipTrigger tooltipTrigger;

    // <변경부분> 슬롯 번호와 상위 UI를 저장하고 버튼 클릭 이벤트를 연결하는 함수
    public void Initialize(BattleUIController owner, int index)
    {
        // 상위 전투 UI 컨트롤러 저장
        battleUIController = owner;

        // 현재 슬롯 번호 저장
        slotIndex = index;

        // 슬롯 버튼이 인스펙터에 연결되지 않았다면 현재 오브젝트에서 찾음
        if (slotButton == null)
        {
            slotButton = GetComponent<Button>();
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

        // 슬롯 버튼 클릭 시 OnClickSlot이 실행되도록 연결
        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(OnClickSlot);
            slotButton.onClick.AddListener(OnClickSlot);
        }
    }

    // <변경부분> 현재 슬롯에 들어있는 아이템 데이터에 맞게 아이콘을 갱신하는 함수
    public void Refresh(BattleItemData itemData)
    {
        // 아이템 데이터가 있고, 아이템 타입이 None이 아니면 아이템이 있는 상태
        bool hasItem = itemData != null && itemData.itemType != BattleItemType.None;

        // 아이템 아이콘 이미지 갱신
        if (itemIconImage != null)
        {
            itemIconImage.sprite = hasItem ? itemData.iconSprite : null;
            itemIconImage.enabled = hasItem && itemData.iconSprite != null;
        }

        // 아이템이 있는 슬롯만 클릭 가능하게 처리
        if (slotButton != null)
        {
            slotButton.interactable = hasItem;
        }

        // 아이템이 있는 슬롯만 클릭 가능하게 처리
        if (slotButton != null)
        {
            slotButton.interactable = hasItem;
        }

        // <변경부분> 아이템이 있는 슬롯에는 아이템 데이터 기반 Tooltip을 연결하고, 빈 슬롯은 Tooltip을 제거
        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetTooltipViewData(
                hasItem ? TooltipViewData.FromBattleItemData(itemData) : null
            );
        }
    }

    // <변경부분> 슬롯 클릭 시 상위 BattleUIController에 슬롯 번호를 전달하는 함수
    private void OnClickSlot()
    {
        // 상위 전투 UI 컨트롤러가 없으면 아이템 사용 요청 불가
        if (battleUIController == null)
        {
            Debug.LogWarning("BattleUIController가 연결되지 않았습니다.");
            return;
        }

        // 클릭한 슬롯 번호를 BattleUIController에 전달
        battleUIController.OnClickItemSlot(slotIndex);
    }
}