using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// <변경부분> 전투 아이템 슬롯 하나의 아이콘 표시와 클릭 입력을 관리하는 UI
public class BattleItemSlotUI : MonoBehaviour
{
    // 이 슬롯을 관리하는 상위 전투 UI 컨트롤러
    private BattleUIController battleUIController;

    // 이 슬롯의 번호
    private int slotIndex;

    // <변경부분> 현재 슬롯에 연결되어 있는 아이템 데이터.
    //
    // Locale이 변경되었을 때 TooltipViewData를
    // 현재 언어 기준으로 다시 생성하기 위해 보관한다.
    private BattleItemData currentItemData;

    [Header("UI")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image itemIconImage;

    // <변경부분> 아이템 슬롯을 꾹 눌렀을 때 아이템 설명 팝업을 표시할 TooltipTrigger
    [SerializeField] private TooltipTrigger tooltipTrigger;

    // <변경부분> 이 슬롯이 활성화되어 있는 동안
    // Unity Localization의 Locale 변경 이벤트를 받는다.
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    // <변경부분> 슬롯이 비활성화되거나 제거될 때
    // 이벤트 구독을 반드시 해제하여 중복 호출을 방지한다.
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;
    }

    // <변경부분> 게임 실행 중 언어가 변경되면
    // 현재 아이템 Tooltip 문자열을 새 Locale 기준으로 다시 만든다.
    private void OnSelectedLocaleChanged(
        Locale locale)
    {
        RefreshTooltip();
    }

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

    // <변경부분> 현재 슬롯에 들어있는 아이템 데이터에 맞게
    // 아이콘, 버튼 클릭 가능 여부, Tooltip을 갱신한다.
    //
    // 개별 슬롯 오브젝트는 숨기지 않는다.
    // 아이템 바 전체 표시 여부는 BattleUIController에서 별도로 처리한다.
    public void Refresh(BattleItemData itemData)
    {
        bool hasItem =
            itemData != null &&
            itemData.itemType != BattleItemType.None;

        // <변경부분> Locale 변경 시 Tooltip을 다시 만들 수 있도록
        // 현재 슬롯의 실제 아이템 데이터를 보관한다.
        currentItemData =
            hasItem
                ? itemData
                : null;

        // <변경부분> 이전 갱신에서 슬롯 오브젝트가 비활성화됐을 수 있으므로
        // 모든 슬롯은 항상 다시 활성화한다.
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(
                true
            );
        }

        // 아이템 아이콘 이미지 갱신
        if (itemIconImage != null)
        {
            itemIconImage.sprite =
                hasItem
                    ? itemData.iconSprite
                    : null;

            itemIconImage.enabled =
                hasItem &&
                itemData.iconSprite != null;
        }

        // 아이템이 들어 있는 슬롯만 클릭 가능하게 처리
        if (slotButton != null)
        {
            slotButton.interactable =
                hasItem;
        }

        // <변경부분> 현재 Locale 기준으로
        // 아이템 Tooltip 표시 데이터를 생성한다.
        RefreshTooltip();


    }

    // <변경부분> 현재 아이템 데이터와 현재 Locale을 기준으로
    // TooltipViewData를 다시 생성하여 TooltipTrigger에 전달한다.
    //
    // 게임 실행 중 언어를 변경한 경우에도
    // Tooltip을 다시 열면 변경된 언어가 표시된다.
    private void RefreshTooltip()
    {
        if (tooltipTrigger == null)
        {
            return;
        }

        tooltipTrigger.SetTooltipViewData(
            currentItemData != null
                ? TooltipViewData.FromBattleItemData(
                    currentItemData
                )
                : null
        );
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