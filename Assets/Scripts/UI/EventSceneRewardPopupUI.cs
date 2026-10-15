using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// <변경부분>
// Event Scene 전용 선택형 전리품 팝업.
//
// 전투 종료 및 Scene 이동 로직은 사용하지 않는다.
// BattleRewardData와 BattleRewardSlotUI만 재사용한다.
//
// 보상을 모두 수령하거나 Confirm을 누르면
// 완료 Callback을 통해 다음 Event Scene Step을 실행한다.
public class EventSceneRewardPopupUI : MonoBehaviour
{
    private const int MaxRelicCount = 10;

    [Header("Popup UI")]
    [SerializeField]
    private GameObject popupRoot;

    // <변경부분>
    // Event Scene 보상창의 공용 글리치 오픈 애니메이션.
    // 실제 RewardPopup 패널에 부착된 PopupOpenAnimator를 연결한다.
    [Header("Open Animation")]
    [SerializeField]
    private PopupOpenAnimator popupOpenAnimator;

    [SerializeField]
    private Transform dropSlotParent;

    [SerializeField]
    private Button confirmButton;

    [Header("Reward Slot")]
    [SerializeField]
    private BattleRewardSlotUI rewardSlotPrefab;

    [SerializeField]
    private TooltipData goldTooltipData;

    [Header("Run HUD - Optional")]
    [SerializeField]
    private RunItemBarUI runItemBarUI;

    [SerializeField]
    private RunGoldUI runGoldUI;

    // 이번 이벤트에서 생성됐지만 아직 수령하지 않은 보상.
    private readonly List<BattleRewardOptionRuntimeData>
        pendingRewards =
            new List<BattleRewardOptionRuntimeData>();

    // 다음 Event Scene Step으로 진행할 Callback.
    private Action onRewardSelectionFinished;

    // 수령에 성공했지만 애니메이션이 끝나지 않은 슬롯 수.
    private int pendingClaimAnimations;

    // 처음 생성한 전리품 수.
    private int initialRewardCount;

    private bool isOpen;
    private bool finishRequested;


    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(
                OnClickConfirm
            );

            confirmButton.onClick.AddListener(
                OnClickConfirm
            );
        }

        // 이벤트씬 진입 시 기본 숨김.
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }


    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(
                OnClickConfirm
            );
        }
    }


    // <변경부분>
    // 이벤트 전리품 팝업을 표시한다.
    //
    // 성공하면 true를 반환하며,
    // 완료 Callback을 받을 때까지 Event Sequence가 대기한다.
    public bool TryShow(
        BattleRewardData rewardData,
        Action onFinished)
    {
        if (isOpen)
        {
            Debug.LogWarning(
                "Event Reward Popup: 이미 표시 중입니다.",
                this
            );

            return false;
        }

        if (rewardData == null ||
            popupRoot == null ||
            dropSlotParent == null ||
            rewardSlotPrefab == null ||
            confirmButton == null)
        {
            Debug.LogWarning(
                "Event Reward Popup: " +
                "보상 데이터 또는 UI 참조가 비어 있습니다.",
                this
            );

            return false;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "Event Reward Popup: " +
                "RunStateManager를 찾을 수 없습니다.",
                this
            );

            return false;
        }

        // <변경부분>
        // 해당 Step을 실행할 때 보상 후보를 한 번만 추첨한다.
        List<BattleRewardOptionRuntimeData> rewards =
            rewardData.CreateRewardOptions();

        if (rewards == null)
        {
            return false;
        }

        // 슬롯으로 표시할 수 없는 데이터가 있다면
        // 일부 보상이 누락된 팝업을 표시하지 않는다.
        for (int i = 0; i < rewards.Count; i++)
        {
            BattleRewardOptionRuntimeData reward =
                rewards[i];

            bool isValid =
                reward != null &&
                (
                    reward.rewardType ==
                        BattleRewardOptionType.Gold &&
                    reward.goldAmount > 0 &&
                    goldTooltipData != null
                    ||
                    reward.rewardType ==
                        BattleRewardOptionType.Item &&
                    reward.itemData != null
                    ||
                    reward.rewardType ==
                        BattleRewardOptionType.Relic &&
                    reward.relicData != null
                );

            if (!isValid)
            {
                Debug.LogWarning(
                    "Event Reward Popup: " +
                    "유효하지 않은 보상 또는 Gold Tooltip 설정이 있습니다.",
                    this
                );

                return false;
            }
        }

        HideImmediately();

        onRewardSelectionFinished =
            onFinished;

        pendingRewards.AddRange(
            rewards
        );

        initialRewardCount =
            pendingRewards.Count;

        pendingClaimAnimations = 0;
        finishRequested = false;
        isOpen = true;

        confirmButton.interactable = true;

        popupRoot.SetActive(true);

        // =================================================
        // Reward Slots
        // =================================================

        for (int i = 0; i < rewards.Count; i++)
        {
            BattleRewardOptionRuntimeData reward =
                rewards[i];

            BattleRewardSlotUI slot =
                Instantiate(
                    rewardSlotPrefab,
                    dropSlotParent
                );

            switch (reward.rewardType)
            {
                case BattleRewardOptionType.Gold:

                    slot.RefreshGold(
                        goldTooltipData,
                        reward.goldAmount,
                        RunStateManager.Instance.GetGoldAmount()
                    );

                    break;

                case BattleRewardOptionType.Item:

                    slot.RefreshItem(
                        reward.itemData,
                        1
                    );

                    break;

                case BattleRewardOptionType.Relic:

                    slot.RefreshRelic(
                        reward.relicData,
                        1
                    );

                    break;
            }

            // 보상 지급이 성공한 경우에만
            // 기존 확대·잔상 애니메이션을 재생한다.
            slot.ConfigureClaim(
       () => TryClaimReward(reward),
       OnClaimAnimationFinished
   );
        }

        RefreshLayouts();

        // <변경부분>
        // 모든 Reward Slot 생성과 Layout 갱신이 완료된 후
        // 기존 공용 PopupOpenAnimator의 글리치 효과를 실행한다.
        //
        // 보상 슬롯이 생성되기 전에 애니메이션이 시작되면
        // 슬롯이 뒤늦게 나타날 수 있으므로 이 위치에서 호출한다.
        if (popupOpenAnimator != null)
        {
            popupOpenAnimator.PlayOpen();
        }
        else
        {
            Debug.LogWarning(
                "Event Reward Popup: " +
                "PopupOpenAnimator가 연결되지 않아 " +
                "오픈 애니메이션 없이 표시합니다.",
                this
            );
        }

        return true;
    }


    // <변경부분>
    // 전투 보상 컨트롤러를 경유하지 않고
    // RunStateManager에 이벤트 보상을 지급한다.
    private bool TryClaimReward(
        BattleRewardOptionRuntimeData reward)
    {
        if (!isOpen ||
            finishRequested ||
            reward == null ||
            !pendingRewards.Contains(reward))
        {
            return false;
        }

        RunStateManager runState =
            RunStateManager.Instance;

        if (runState == null)
        {
            return false;
        }

        bool succeeded = false;

        switch (reward.rewardType)
        {
            case BattleRewardOptionType.Gold:

                if (reward.goldAmount > 0)
                {
                    runState.AddGold(
                        reward.goldAmount
                    );

                    succeeded = true;
                }

                break;

            case BattleRewardOptionType.Item:

                succeeded =
                    runState.TryAddBattleItem(
                        reward.itemData,
                        RunStateManager.MaxBattleItemCount
                    );

                break;

            case BattleRewardOptionType.Relic:

                succeeded =
                    runState.TryAddBattleRelic(
                        reward.relicData,
                        MaxRelicCount
                    );

                break;
        }

        if (!succeeded)
        {
            // 인벤토리 초과 또는 유물 중복이면
            // 슬롯을 그대로 유지한다.
            return false;
        }

        // 같은 후보의 중복 지급 방지.
        pendingRewards.Remove(reward);

        // 실제 지급 직후 애니메이션 대기 개수를 증가시킨다.
        pendingClaimAnimations++;

        // 이벤트씬에서 Run HUD가 표시되어 있다면
        // 실제 보유 데이터로 즉시 갱신한다.
        if (runItemBarUI != null)
        {
            runItemBarUI.RefreshFromRunState();
        }

        if (runGoldUI != null)
        {
            runGoldUI.RefreshImmediately();
        }

        return true;
    }


    // <변경부분>
    // BattleRewardSlotUI의 확대·잔상 애니메이션 종료 후 호출.
    private void OnClaimAnimationFinished()
    {
        if (!isOpen)
        {
            return;
        }

        pendingClaimAnimations =
            Mathf.Max(
                0,
                pendingClaimAnimations - 1
            );

        RefreshLayouts();

        if (pendingClaimAnimations > 0)
        {
            return;
        }

        // 모든 보상을 받았거나,
        // Confirm으로 남은 보상을 포기한 경우에만 종료.
        if (finishRequested ||
            initialRewardCount > 0 &&
            pendingRewards.Count == 0)
        {
            FinishRewardSelection();
        }
    }


    // <변경부분>
    // ConfirmButton을 클릭하면 미수령 보상은 포기한다.
    //
    // 진행 중인 슬롯 애니메이션이 있다면
    // 마지막 애니메이션이 종료될 때까지 기다린다.
    private void OnClickConfirm()
    {
        if (!isOpen ||
            finishRequested)
        {
            return;
        }

        finishRequested = true;

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }

        if (pendingClaimAnimations == 0)
        {
            FinishRewardSelection();
        }
    }


    // <변경부분>
    // 실제 보상 지급은 이미 끝났으므로
    // 남은 미수령 후보만 버리고 다음 Step으로 진행한다.
    private void FinishRewardSelection()
    {
        if (!isOpen)
        {
            return;
        }

        Action callback =
            onRewardSelectionFinished;

        HideImmediately();

        callback?.Invoke();
    }


    // <변경부분>
    // 이벤트 강제 종료 / 씬 비활성화 시
    // 다음 Step Callback 없이 팝업만 정리한다.
    public void HideImmediately()
    {
        isOpen = false;
        finishRequested = false;

        pendingClaimAnimations = 0;
        initialRewardCount = 0;

        pendingRewards.Clear();

        onRewardSelectionFinished = null;

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        ClearCreatedSlots();
    }


    private void ClearCreatedSlots()
    {
        if (dropSlotParent == null)
        {
            return;
        }

        for (int i = dropSlotParent.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject child =
                dropSlotParent.GetChild(i).gameObject;

            child.SetActive(false);
            Destroy(child);
        }
    }


    private void RefreshLayouts()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform parentRect =
            dropSlotParent as RectTransform;

        if (parentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                parentRect
            );
        }
    }
}
