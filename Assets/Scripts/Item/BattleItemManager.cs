using UnityEngine;

// <변경부분> 전투 중 사용하는 소모성 아이템 슬롯과 사용 흐름을 관리하는 매니저
public class BattleItemManager : MonoBehaviour
{
    // 전투 아이템 슬롯 최대 개수
    private const int MaxItemSlotCount = 4;

    // 현재 전투에서 보유 중인 소모성 아이템 슬롯
    private BattleItemData[] itemSlots = new BattleItemData[MaxItemSlotCount];

    // 아이템 효과 실행을 요청할 전투 매니저
    private BattleManager battleManager;

    // 아이템 슬롯 UI 갱신을 요청할 UI 컨트롤러
    private BattleUIController battleUIController;

    [Header("Item Database")]
    // <변경부분> BattleItemType으로 BattleItemData를 찾는 아이템 데이터베이스
    [SerializeField] private BattleItemDatabase battleItemDatabase;

    [Header("Test Item")]
    // <변경부분> 테스트용으로 전투 시작 시 지급할 아이템 타입
    [SerializeField] private BattleItemType testStartItemType = BattleItemType.ChangeSelectedPieceToJelluPawn;

    // 게임 시작 시 테스트 아이템을 지급할지 여부
    [SerializeField] private bool addTestStartItem = true;

    // <변경부분> BattleManager에서 전투 시작 시 아이템 매니저를 초기화하는 함수
    public void Initialize(BattleManager owner, BattleUIController uiController)
    {
        // 아이템 효과 실행을 요청할 BattleManager 저장
        battleManager = owner;

        // 아이템 슬롯 UI 갱신을 요청할 BattleUIController 저장
        battleUIController = uiController;

        // 게임 시작 시 아이템 슬롯 UI 초기화
        RefreshItemSlotUI();

        // <변경부분> 테스트용 아이템 타입이 설정되어 있으면 Database에서 찾아 전투 시작 시 1개 지급
        if (addTestStartItem && testStartItemType != BattleItemType.None)
        {
            AddBattleItemByType(testStartItemType);
        }
    }

    // <변경부분> 아이템 타입을 받아 Database에서 BattleItemData를 찾은 뒤 슬롯에 추가하는 함수
    public void AddBattleItemByType(BattleItemType itemType)
    {
        if (itemType == BattleItemType.None)
        {
            Debug.LogWarning("추가할 아이템 타입이 None입니다.");
            return;
        }

        if (battleItemDatabase == null)
        {
            Debug.LogWarning("BattleItemDatabase가 연결되지 않아 아이템을 추가할 수 없습니다.");
            return;
        }

        BattleItemData itemData = battleItemDatabase.GetData(itemType);

        if (itemData == null)
        {
            Debug.LogWarning($"BattleItemDatabase에서 아이템 데이터를 찾을 수 없습니다: {itemType}");
            return;
        }

        AddBattleItem(itemData);
    }

    // <변경부분> 전투 아이템을 왼쪽 빈 슬롯부터 추가하는 함수
    public void AddBattleItem(BattleItemData itemData)
    {
        // 추가할 아이템 데이터가 없으면 종료
        if (itemData == null || itemData.itemType == BattleItemType.None)
        {
            Debug.LogWarning("추가할 아이템 데이터가 없습니다.");
            return;
        }

        // 왼쪽 슬롯부터 빈칸을 찾음
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null && itemSlots[i].itemType != BattleItemType.None)
            {
                continue;
            }

            itemSlots[i] = itemData;

            // 아이템 획득 후 슬롯 UI 갱신
            RefreshItemSlotUI();

            Debug.Log($"아이템 획득: {itemData.itemName} / 슬롯 {i}");
            return;
        }

        Debug.Log("아이템 슬롯이 가득 찼습니다.");
    }

    public void AddTestItemForDebug()
    {
        // <변경부분> 테스트 아이템 타입이 없으면 추가 불가
        if (testStartItemType == BattleItemType.None)
        {
            Debug.LogWarning("테스트 아이템 타입이 설정되지 않았습니다.");
            return;
        }

        // <변경부분> 테스트 아이템 타입으로 Database에서 데이터를 찾아 슬롯에 추가
        AddBattleItemByType(testStartItemType);
    }


    // <변경부분> 특정 슬롯의 아이템을 사용하는 함수
    public void UseItemAtSlot(int slotIndex)
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        // BattleManager에게 현재 아이템을 사용할 수 있는 전투 상태인지 확인 요청
        if (battleManager.CanUseBattleItem() == false)
        {
            return;
        }

        // 슬롯 번호가 잘못되었으면 종료
        if (slotIndex < 0 || slotIndex >= itemSlots.Length)
        {
            Debug.LogWarning($"잘못된 아이템 슬롯 번호입니다: {slotIndex}");
            return;
        }

        // 해당 슬롯에 아이템이 없으면 종료
        BattleItemData itemData = itemSlots[slotIndex];

        if (itemData == null || itemData.itemType == BattleItemType.None)
        {
            Debug.Log("해당 슬롯에 사용할 아이템이 없습니다.");
            return;
        }

        // BattleManager에게 실제 아이템 효과 실행 요청
        bool itemUsed = battleManager.TryApplyBattleItemEffect(itemData);

        // 효과가 실패했으면 아이템을 소모하지 않음
        if (itemUsed == false)
        {
            return;
        }

        // 사용한 아이템 제거
        itemSlots[slotIndex] = null;

        // 빈칸이 생기면 왼쪽부터 다시 정렬
        CompressItemSlots();

        // 아이템 사용 후 UI 갱신
        RefreshItemSlotUI();

        Debug.Log($"아이템 사용 완료: {itemData.itemName}");
    }

    // <변경부분> 아이템 사용 후 빈 슬롯을 제거하고 왼쪽부터 다시 채우는 함수
    private void CompressItemSlots()
    {
        BattleItemData[] compressedSlots = new BattleItemData[MaxItemSlotCount];
        int targetIndex = 0;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null || itemSlots[i].itemType == BattleItemType.None)
            {
                continue;
            }

            compressedSlots[targetIndex] = itemSlots[i];
            targetIndex++;
        }

        itemSlots = compressedSlots;
    }

    // <변경부분> 현재 아이템 슬롯 정보를 UI에 반영하는 함수
    private void RefreshItemSlotUI()
    {
        if (battleUIController == null)
        {
            return;
        }

        battleUIController.RefreshItemSlots(itemSlots);
    }
}
