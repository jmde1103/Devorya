using System.Collections.Generic;
using UnityEngine;


// <변경부분>
// RunStateManager가 보유 중인 Battle Item을
// Battle 외의 UI에서 읽기 전용으로 표시하는 공용 Item Bar.
//
// 현재 WorldMap HUD에서 사용한다.
//
// 실제 아이템 사용 책임은 가지지 않으며,
// BattleItemSlotUI의 아이콘 / Localization / Tooltip 기능만 재사용한다.
public class RunItemBarUI : MonoBehaviour
{
    // 기존 BattleItemManager와 동일한 최대 아이템 슬롯 수.
    private const int MaxItemSlotCount =
        4;


    [Header("Item Slots")]

    // 왼쪽부터 순서대로 0 ~ 3번 슬롯을 연결한다.
    [SerializeField]
    private BattleItemSlotUI[] itemSlotUIs;


    private void Awake()
    {
        InitializeReadOnlySlots();
    }


    private void OnEnable()
    {
        RefreshFromRunState();
    }


    private void Start()
    {
        // <변경부분>
        // Scene 초기화 순서상 OnEnable 시점에
        // RunStateManager 준비가 끝나지 않은 경우를 대비하여
        // Start에서도 한 번 더 현재 Run 상태를 반영한다.
        RefreshFromRunState();
    }


    // <변경부분>
    // 연결된 모든 Slot을 WorldMap용 읽기 전용 상태로 초기화한다.
    private void InitializeReadOnlySlots()
    {
        if (itemSlotUIs == null)
        {
            return;
        }


        for (int i = 0;
             i < itemSlotUIs.Length;
             i++)
        {
            BattleItemSlotUI slotUI =
                itemSlotUIs[i];


            if (slotUI == null)
            {
                continue;
            }


            slotUI.InitializeReadOnly();
        }
    }


    // <변경부분>
    // 현재 RunStateManager의 아이템 목록을 읽어서
    // 월드맵의 4개 Slot에 순서대로 표시한다.
    //
    // 데이터가 없는 나머지 Slot은 빈 슬롯 상태로 Refresh한다.
    public void RefreshFromRunState()
    {
        if (itemSlotUIs == null ||
            itemSlotUIs.Length == 0)
        {
            return;
        }


        List<BattleItemData> ownedItems =
            null;


        if (RunStateManager.Instance != null)
        {
            ownedItems =
                RunStateManager.Instance
                    .GetBattleItemsCopy();
        }


        int visibleSlotCount =
            Mathf.Min(
                itemSlotUIs.Length,
                MaxItemSlotCount
            );


        for (int i = 0;
             i < visibleSlotCount;
             i++)
        {
            BattleItemSlotUI slotUI =
                itemSlotUIs[i];


            if (slotUI == null)
            {
                continue;
            }


            BattleItemData itemData =
                null;


            if (ownedItems != null &&
                i < ownedItems.Count)
            {
                itemData =
                    ownedItems[i];
            }


            slotUI.Refresh(
                itemData
            );
        }


        // <변경부분>
        // Inspector에 실수로 4개보다 많은 Slot을 연결한 경우
        // 남는 Slot에 이전 아이콘이 남지 않도록 빈 상태로 정리한다.
        for (int i = visibleSlotCount;
             i < itemSlotUIs.Length;
             i++)
        {
            if (itemSlotUIs[i] == null)
            {
                continue;
            }


            itemSlotUIs[i].Refresh(
                null
            );
        }
    }
}
