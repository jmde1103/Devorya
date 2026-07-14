using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> 전투 종료 후 실제 획득한 기물 복구,
// 금화, 아이템, 유물 보상을 슬롯 형태로 표시하는 팝업 UI
public class BattleRewardPopupUI : MonoBehaviour
{
    [Header("Root")]
    // <변경부분> 실제로 활성화/비활성화할 보상 팝업 루트
    [SerializeField] private GameObject popupRoot;

    [Header("Recovery Reward")]
    // <변경부분> 복구된 기물 슬롯이 생성될 부모
    [SerializeField] private Transform recoverySlotParent;

    // <변경부분> 복구 기물이 없을 때 숨길 복구 보상 영역
    [SerializeField] private GameObject recoveryAreaObject;

    [Header("Battle Drop")]
    // <변경부분> 금화, 아이템, 유물 슬롯이 생성될 부모
    [SerializeField] private Transform dropSlotParent;

    // <변경부분> 드롭 보상이 없을 때 숨길 보상 영역
    [SerializeField] private GameObject dropAreaObject;

    [Header("Slot")]
    // <변경부분> 기물, 금화, 아이템, 유물이 공통으로 사용하는 슬롯 프리팹
    [SerializeField] private BattleRewardSlotUI rewardSlotPrefab;

    [Header("Gold")]
    // <변경부분> 금화 슬롯에 표시할 아이콘
    [SerializeField] private Sprite goldIconSprite;

    // <변경부분> 금화 슬롯 Long Press Tooltip에 사용할 고정 설명 데이터
    [SerializeField] private TooltipData goldTooltipData;

    [Header("Confirm")]
    // <변경부분> 보상 확인 후 맵으로 돌아가는 버튼
    [SerializeField] private Button confirmButton;

    [Header("Open Animation")]
    // <변경부분> 기존 코루틴 팝업 오픈 애니메이터
    [SerializeField] private PopupOpenAnimator popupOpenAnimator;

    // <변경부분> 확인 버튼 클릭 후 맵 이동을 요청할 전투 종료 컨트롤러
    private BattleEndFlowController battleEndFlowController;

    private void Awake()
    {
        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        if (popupOpenAnimator == null)
        {
            popupOpenAnimator = GetComponent<PopupOpenAnimator>();
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnClickConfirm);
            confirmButton.onClick.AddListener(OnClickConfirm);
        }

        popupRoot.SetActive(false);
    }

    // <변경부분> 전투 종료 컨트롤러가 보상 계산을 끝낸 뒤 호출
    public void Show(
        BattleEndFlowController owner,
        List<BattleRecoveryRewardRuntimeData> recoveryRewards,
        int goldAmount,
        List<BattleRewardOptionRuntimeData> acquiredRewards)
    {
        battleEndFlowController = owner;

        if (popupRoot == null)
        {
            Debug.LogWarning(
                "보상 팝업 표시 실패: popupRoot가 없습니다."
            );

            return;
        }

        popupRoot.SetActive(true);

        // <변경부분> 이전 전투에서 생성된 슬롯 제거
        ClearCreatedSlots(recoverySlotParent);
        ClearCreatedSlots(dropSlotParent);

        // <변경부분> 실제 보상 데이터에 맞춰 슬롯 생성
        int recoverySlotCount =
            CreateRecoverySlots(recoveryRewards);

        int dropSlotCount =
            CreateDropSlots(goldAmount, acquiredRewards);

        if (recoveryAreaObject != null)
        {
            recoveryAreaObject.SetActive(
                recoverySlotCount > 0
            );
        }

        if (dropAreaObject != null)
        {
            dropAreaObject.SetActive(
                dropSlotCount > 0
            );
        }

        // <변경부분> 슬롯 생성 후 레이아웃을 먼저 갱신하고
        // 기존 팝업 오픈 애니메이션 재생
        RectTransform popupRect =
            popupRoot.transform as RectTransform;

        if (popupRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                popupRect
            );
        }

        if (popupOpenAnimator != null)
        {
            popupOpenAnimator.PlayOpen();
        }

        Debug.Log(
            $"전투 보상 팝업 표시: " +
            $"복구 {recoverySlotCount}종 / " +
            $"드롭 {dropSlotCount}종"
        );
    }

    // <변경부분> 복구된 기물을 종류별 슬롯으로 생성
    private int CreateRecoverySlots(
        List<BattleRecoveryRewardRuntimeData> recoveryRewards)
    {
        if (recoveryRewards == null ||
            recoverySlotParent == null ||
            rewardSlotPrefab == null)
        {
            return 0;
        }

        int createdCount = 0;

        for (int i = 0; i < recoveryRewards.Count; i++)
        {
            BattleRecoveryRewardRuntimeData reward =
                recoveryRewards[i];

            if (reward == null ||
                reward.pieceData == null ||
                reward.amount <= 0)
            {
                continue;
            }

            BattleRewardSlotUI slotUI =
                Instantiate(
                    rewardSlotPrefab,
                    recoverySlotParent
                );

            slotUI.RefreshRecoveryPiece(
                reward.pieceData,
                reward.amount
            );

            createdCount++;
        }

        return createdCount;
    }

    // <변경부분> 금화, 아이템, 유물 슬롯을 순차적으로 생성
    private int CreateDropSlots(
        int goldAmount,
        List<BattleRewardOptionRuntimeData> acquiredRewards)
    {
        if (dropSlotParent == null ||
            rewardSlotPrefab == null)
        {
            return 0;
        }

        int createdCount = 0;

        // <변경부분> 금화는 가장 먼저 표시
        if (goldAmount > 0)
        {
            BattleRewardSlotUI goldSlot =
                Instantiate(
                    rewardSlotPrefab,
                    dropSlotParent
                );

            goldSlot.RefreshGold(
                goldIconSprite,
                goldAmount,
                goldTooltipData
            );

            createdCount++;
        }

        // <변경부분> 같은 아이템이나 유물이 여러 개면
        // 슬롯 하나에 수량으로 합산
        List<BattleRewardDisplayGroup> groups =
            GroupAcquiredRewards(acquiredRewards);

        for (int i = 0; i < groups.Count; i++)
        {
            BattleRewardDisplayGroup group =
                groups[i];

            BattleRewardSlotUI slotUI =
                Instantiate(
                    rewardSlotPrefab,
                    dropSlotParent
                );

            if (group.itemData != null)
            {
                slotUI.RefreshItem(
                    group.itemData,
                    group.amount
                );

                createdCount++;
            }
            else if (group.relicData != null)
            {
                slotUI.RefreshRelic(
                    group.relicData,
                    group.amount
                );

                createdCount++;
            }
        }

        return createdCount;
    }

    // <변경부분> 동일한 BattleItemData 또는 BattleRelicData를
    // 하나의 슬롯 수량으로 합산
    private List<BattleRewardDisplayGroup>
        GroupAcquiredRewards(
            List<BattleRewardOptionRuntimeData> acquiredRewards)
    {
        List<BattleRewardDisplayGroup> groups =
            new List<BattleRewardDisplayGroup>();

        if (acquiredRewards == null)
        {
            return groups;
        }

        for (int i = 0; i < acquiredRewards.Count; i++)
        {
            BattleRewardOptionRuntimeData reward =
                acquiredRewards[i];

            if (reward == null)
            {
                continue;
            }

            BattleRewardDisplayGroup existingGroup =
                null;

            for (int j = 0; j < groups.Count; j++)
            {
                if (groups[j].Matches(reward))
                {
                    existingGroup = groups[j];
                    break;
                }
            }

            if (existingGroup != null)
            {
                existingGroup.amount++;
            }
            else
            {
                groups.Add(
                    BattleRewardDisplayGroup.Create(reward)
                );
            }
        }

        return groups;
    }

    // <변경부분> 이전에 동적으로 생성한 보상 슬롯 제거
    private void ClearCreatedSlots(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(
                parent.GetChild(i).gameObject
            );
        }
    }

    // <변경부분> 확인 버튼 클릭 시 보상창을 닫고 맵 씬으로 이동
    private void OnClickConfirm()
    {
        if (battleEndFlowController == null)
        {
            Debug.LogWarning(
                "보상 확인 실패: " +
                "BattleEndFlowController가 없습니다."
            );

            return;
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        battleEndFlowController.MoveToMapScene();
    }

    // <변경부분> UI 표시용으로 같은 아이템·유물 수량을 묶는 내부 데이터
    private class BattleRewardDisplayGroup
    {
        public BattleItemData itemData;
        public BattleRelicData relicData;
        public int amount = 1;

        public static BattleRewardDisplayGroup Create(
            BattleRewardOptionRuntimeData reward)
        {
            return new BattleRewardDisplayGroup
            {
                itemData = reward.itemData,
                relicData = reward.relicData,
                amount = 1
            };
        }

        public bool Matches(
            BattleRewardOptionRuntimeData reward)
        {
            if (itemData != null)
            {
                return reward.itemData == itemData;
            }

            if (relicData != null)
            {
                return reward.relicData == relicData;
            }

            return false;
        }
    }
}
