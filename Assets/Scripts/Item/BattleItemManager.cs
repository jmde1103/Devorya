using System.Collections.Generic;
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

    [Header("Debug Test Item")]

    // <변경부분> 개발용 아이템 추가 버튼으로
    // 어떤 아이템을 지급할지 Inspector에서 선택한다.
    //
    // 기본값은 젤루 변환 아이템으로 설정한다.
    // 필요할 때 다른 BattleItemType으로 변경하여
    // 같은 디버그 버튼으로 여러 아이템을 테스트할 수 있다.
    [SerializeField]
    private BattleItemType debugTestItemType =
        BattleItemType.ChangeSelectedPieceToJelluPawn;

    // <변경부분> BattleManager에서 전투 시작 시 아이템 매니저를 초기화하는 함수
    public void Initialize(BattleManager owner, BattleUIController uiController)
    {
        battleManager = owner;
        battleUIController = uiController;

        // <변경부분> 이전 전투에서 RunStateManager에 저장한
        // 아이템을 현재 전투 슬롯에 복원
        RestoreItemsFromRunState();

        RefreshItemSlotUI();
    }

    // <변경부분> RunStateManager에 저장된 아이템을
    // 현재 전투 슬롯로 복원
    private void RestoreItemsFromRunState()
    {
        InventoryVersion++;
        itemSlots = new BattleItemData[MaxItemSlotCount];

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "아이템 복원 생략: RunStateManager가 없습니다."
            );

            return;
        }

        List<BattleItemData> savedItems =
            RunStateManager.Instance.GetBattleItemsCopy();

        int restoreCount =
            Mathf.Min(savedItems.Count, MaxItemSlotCount);

        for (int i = 0; i < restoreCount; i++)
        {
            itemSlots[i] = savedItems[i];
        }

        Debug.Log($"런 아이템 복원 완료: {restoreCount}개");
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

    // <변경부분> 디버그 버튼으로 테스트 아이템을 수동 추가하는 함수.
    //
    // 실제 지급 아이템은 Inspector의 Debug Test Item Type에서 선택하며,
    // 자동 지급이 아니라 개발 중 아이템 기능 테스트용으로만 사용한다.
    public void AddTestItemForDebug()
    {
        if (debugTestItemType ==
            BattleItemType.None)
        {
            Debug.LogWarning(
                "디버그 아이템 추가 실패: " +
                "Debug Test Item Type이 None입니다."
            );

            return;
        }

        AddBattleItemByType(
            debugTestItemType
        );
    }

    // <변경부분> 전투 아이템을 왼쪽 빈 슬롯부터 추가하고
    // RunStateManager에도 저장
    public bool AddBattleItem(BattleItemData itemData)
    {
        if (itemData == null ||
            itemData.itemType == BattleItemType.None)
        {
            Debug.LogWarning("추가할 아이템 데이터가 없습니다.");
            return false;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "아이템 획득 실패: RunStateManager가 없습니다."
            );

            return false;
        }

        // <변경부분> 런 저장 목록에 먼저 추가해
        // 슬롯 제한과 씬 유지 여부를 한곳에서 관리
        if (RunStateManager.Instance.TryAddBattleItem(
                itemData,
                MaxItemSlotCount) == false)
        {
            return false;
        }

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null &&
                itemSlots[i].itemType != BattleItemType.None)
            {
                continue;
            }

            InventoryVersion++;
            itemSlots[i] = itemData;
            RefreshItemSlotUI();

            Debug.Log(
                $"아이템 획득: {itemData.itemName} / 슬롯 {i}"
            );

            return true;
        }

        // <변경부분> 저장과 슬롯이 불일치한 예외 상황이면
        // 방금 추가한 런 아이템을 원복
        RunStateManager.Instance.RemoveBattleItemAt(
            RunStateManager.Instance.GetBattleItemsCopy().Count - 1
        );

        Debug.LogWarning(
            "아이템 슬롯 추가 실패: 런 저장과 전투 슬롯 상태가 일치하지 않습니다."
        );

        return false;
    }

    // <변경부분> 특정 슬롯의 아이템을 사용하는 함수
    public BattleItemData GetItemAtSlot(int index) =>
        index >= 0 && index < itemSlots.Length ? itemSlots[index] : null;

    // Compatibility entry point: selection only, never immediate consumption.
    public void UseItemAtSlot(int slotIndex)
    {
        if (battleManager != null) battleManager.BeginItemTargeting(slotIndex);
    }

    public int InventoryVersion { get; private set; }
    public bool MatchesRunInventory()
    {
        if (RunStateManager.Instance == null) return false;
        var saved = RunStateManager.Instance.GetBattleItemsCopy();
        int count = 0;
        foreach (var item in itemSlots) if (item != null) count++;
        if (saved.Count != count) return false;
        for (int i = 0; i < saved.Count; i++) if (saved[i] != itemSlots[i]) return false;
        return true;
    }

    private bool applyingItem;
    public BattleItemFailure TryUseItemAtSlot(int slotIndex, BattleItemData expected, Piece target)
    {
        if (applyingItem) return BattleItemFailure.Busy;
        if (expected == null || GetItemAtSlot(slotIndex) != expected) return BattleItemFailure.MissingItem;
        if (battleManager == null || RunStateManager.Instance == null) return BattleItemFailure.Requirements;
        if (!MatchesRunInventory()) return BattleItemFailure.MissingItem;
        var saved = RunStateManager.Instance.GetBattleItemsCopy();
        if (slotIndex >= saved.Count || saved[slotIndex] != expected) return BattleItemFailure.MissingItem;
        applyingItem = true;
        try
        {
            var failure = battleManager.TryApplyBattleItemEffect(expected, target);
            if (failure != BattleItemFailure.None) return failure;
            RunStateManager.Instance.RemoveBattleItemAt(slotIndex);
            InventoryVersion++;
            itemSlots[slotIndex] = null;
            CompressItemSlots();
            RefreshItemSlotUI();
            return BattleItemFailure.None;
        }
        finally { applyingItem = false; }
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
