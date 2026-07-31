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

    [SerializeField] private BattleRewardSlotUI rewardSlotPrefab;

    // <변경부분> 금화 이름, 아이콘, 기본 설명을 모두 보관하는 TooltipData
    // 별도의 goldIconSprite는 사용하지 않는다.
    [SerializeField] private TooltipData goldTooltipData;

    [SerializeField] private Button confirmButton;
    [Header("Open Animation")]
    // <변경부분> 기존 코루틴 팝업 오픈 애니메이터
    [SerializeField]
    private PopupOpenAnimator popupOpenAnimator;


    [Header("Slot Layout")]
    // <변경부분> RecoverySlotParent 또는 DropSlotParent에
    // LayoutGroup이 없을 때 HorizontalLayoutGroup을 자동 추가할지 여부
    [SerializeField]
    private bool addHorizontalLayoutWhenMissing = true;

    // <변경부분> 자동 생성되는 보상 슬롯 사이의 간격
    [SerializeField]
    private float automaticSlotSpacing = 12f;


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
            popupOpenAnimator =
                GetComponent<PopupOpenAnimator>();
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(
                OnClickConfirm
            );

            confirmButton.onClick.AddListener(
                OnClickConfirm
            );
        }

        // <변경부분> 슬롯 부모에 LayoutGroup이 빠져 있어도
        // 생성된 슬롯들이 같은 위치에 겹치지 않도록 자동 보정한다.
        EnsureSlotParentLayout(
            recoverySlotParent
        );

        EnsureSlotParentLayout(
            dropSlotParent
        );

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

        // <변경부분> Recovery 제목 영역은 보상 유무와 관계없이 항상 표시한다.
        if (recoveryAreaObject != null)
        {
            recoveryAreaObject.SetActive(true);
        }

        // 실제 Recovery 슬롯이 있을 때만 슬롯 부모를 표시한다.
        if (recoverySlotParent != null)
        {
            recoverySlotParent.gameObject.SetActive(
                recoverySlotCount > 0
            );
        }

        // <변경부분> Drop 제목 영역도 보상 유무와 관계없이 항상 표시한다.
        if (dropAreaObject != null)
        {
            dropAreaObject.SetActive(true);
        }

        // 실제 Drop 슬롯이 있을 때만 슬롯 부모를 표시한다.
        if (dropSlotParent != null)
        {
            dropSlotParent.gameObject.SetActive(
                dropSlotCount > 0
            );
        }

        // <변경부분> 생성된 슬롯의 부모와 팝업 전체 레이아웃을
        // 즉시 다시 계산해서 같은 위치에 겹치는 현상을 방지한다.
        RefreshRewardLayouts();

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

            // <변경부분> RunStateManager에서 보상 적용 후의
            // 현재 총 금화 수량을 가져온다.
            int currentGoldAmount = 0;

            if (RunStateManager.Instance != null)
            {
                currentGoldAmount =
                    RunStateManager.Instance.GetGoldAmount();
            }
            else
            {
                // RunStateManager가 없는 테스트 상황에서는
                // 이번에 획득한 금화량을 임시 총량으로 사용한다.
                currentGoldAmount = goldAmount;

                Debug.LogWarning(
                    "금화 툴팁 표시: " +
                    "RunStateManager가 없어 이번 획득량을 현재 금화량으로 표시합니다."
                );
            }

            // <변경부분> 금화 아이콘은 TooltipData에서 가져오고,
            // 이번 획득량과 현재 총량을 함께 전달한다.
            goldSlot.RefreshGold(
                goldTooltipData,
                goldAmount,
                currentGoldAmount
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
    // <변경부분> Slot Parent에 LayoutGroup이 빠져 있으면
    // 런타임에 HorizontalLayoutGroup을 추가해 슬롯 겹침을 방지한다.
    private void EnsureSlotParentLayout(
        Transform parent)
    {
        if (parent == null ||
            addHorizontalLayoutWhenMissing == false)
        {
            return;
        }

        LayoutGroup existingLayoutGroup =
            parent.GetComponent<LayoutGroup>();

        // 이미 HorizontalLayoutGroup 또는 GridLayoutGroup이 있으면
        // 기존 Inspector 설정을 그대로 사용한다.
        if (existingLayoutGroup != null)
        {
            return;
        }

        HorizontalLayoutGroup horizontalLayoutGroup =
            parent.gameObject
                .AddComponent<HorizontalLayoutGroup>();

        horizontalLayoutGroup.spacing =
            automaticSlotSpacing;

        horizontalLayoutGroup.childAlignment =
            TextAnchor.MiddleCenter;

        // BattleRewardSlot 프리팹의 RectTransform 크기를 그대로 사용
        horizontalLayoutGroup.childControlWidth = false;
        horizontalLayoutGroup.childControlHeight = false;
        horizontalLayoutGroup.childForceExpandWidth = false;
        horizontalLayoutGroup.childForceExpandHeight = false;

        Debug.Log(
            $"보상 슬롯 레이아웃 자동 추가: " +
            $"{parent.name}"
        );
    }


    // <변경부분> 슬롯 생성과 영역 활성화가 끝난 뒤
    // 각각의 슬롯 부모와 팝업 전체 레이아웃을 즉시 갱신한다.
    private void RefreshRewardLayouts()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform recoveryParentRect =
            recoverySlotParent as RectTransform;

        if (recoveryParentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                recoveryParentRect
            );
        }

        RectTransform dropParentRect =
            dropSlotParent as RectTransform;

        if (dropParentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                dropParentRect
            );
        }

        RectTransform popupRect =
            popupRoot != null
                ? popupRoot.transform as RectTransform
                : null;

        if (popupRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                popupRect
            );
        }
    }


    // <변경부분> 이전에 동적으로 생성한 보상 슬롯 제거
    private void ClearCreatedSlots(
        Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject childObject =
                parent.GetChild(i).gameObject;

            // Destroy는 프레임 마지막에 처리되기 때문에
            // 먼저 비활성화해서 새 슬롯과 잠깐 겹치는 현상을 막는다.
            childObject.SetActive(false);

            Destroy(childObject);
        }
    }

    // <변경부분> 확인 버튼 클릭 시 보상창을 닫고
    // BattleEndFlowController가 설정한 다음 전투 씬 또는 맵 씬으로 이동
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
