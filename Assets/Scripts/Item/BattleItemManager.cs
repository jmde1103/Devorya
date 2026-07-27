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

    // <변경부분> 디버그 버튼으로 방어 아이템을 수동 추가하는 함수
    // 자동 지급이 아니라 개발 중 상태효과 아이템 테스트용으로만 사용한다.
    public void AddTestItemForDebug()
    {
        // <변경부분> BattleItemDatabase에서
        // 방어 상태효과 부여 아이템 데이터를 찾아 빈 슬롯에 추가한다.
        AddBattleItemByType(
            BattleItemType.ApplyStatusEffectToSelectedPiece
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

        // <변경부분> 실제 사용에 성공한 아이템을
        // 런 저장 목록에서도 같은 슬롯 기준으로 제거
        if (RunStateManager.Instance != null)
        {
            RunStateManager.Instance.RemoveBattleItemAt(slotIndex);
        }
        else
        {
            Debug.LogWarning(
                "아이템 사용 저장 반영 실패: RunStateManager가 없습니다."
            );
        }

        itemSlots[slotIndex] = null;

        CompressItemSlots();
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
