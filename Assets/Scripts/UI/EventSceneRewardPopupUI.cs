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

    // <변경부분> 전투 복구와 동일한 기물 보유 한도.
    private const int MaxPlayerPieceCount = 10;

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

    // <변경부분>
    // Confirm Button 안의 문구를 보상 형태에 따라 전환한다.
    //
    // Drop Confirm Label:
    // "전리품을 받지 않고 넘기기"
    //
    // Restoration Confirm Label:
    // "계속하기"
    //
    // 실제 문구의 KR / EN / JA 로컬라이제이션은
    // 각 Text 오브젝트가 기존 Localization 방식을 그대로 사용한다.
    [Header("Confirm Button Labels")]

    [SerializeField]
    private GameObject dropConfirmLabelObject;

    [SerializeField]
    private GameObject restorationConfirmLabelObject;

    [Header("Reward Slot")]
    [SerializeField]
    private BattleRewardSlotUI rewardSlotPrefab;

    [SerializeField]
    private TooltipData goldTooltipData;

    // <변경부분>
    // 전투 보상창과 동일한 복구 슬롯 프리팹을
    // 이벤트 보상창에서도 사용한다.
    [Header("Restoration Reward UI")]
    [SerializeField]
    private Transform recoverySlotParent;

    [SerializeField]
    private GameObject recoveryAreaObject;

    [SerializeField]
    private GameObject dropAreaObject;

    [SerializeField]
    private BattleRewardSlotUI pieceRepairSlotPrefab;

    // <변경부분>
    // BattleEndFlowController와 같은
    // 폰 90% / 상위 기물 10%의 설정.
    [Header("Devorya Restoration Pieces")]
    [SerializeField, Range(0, 100)]
    private int devoryaPawnRewardPercent = 90;

    [SerializeField]
    private PieceData devoryaPawnData;

    [SerializeField]
    private PieceData devoryaKnightData;

    [SerializeField]
    private PieceData devoryaRookData;

    [SerializeField]
    private PieceData devoryaBishopData;

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

    // <변경부분>
    // 복구 결과는 플레이어가 Confirm할 때까지 유지한다.
    private bool hasRestorationSection;

    // <변경부분>
    // 생성한 복구 슬롯만 추적하여 팝업 종료 시 정리한다.
    private readonly List<BattleRewardSlotUI> createdRecoverySlots =
        new List<BattleRewardSlotUI>();

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
    // <변경부분>
    // 기존 보상 전용 호출을 보호한다.
    public bool TryShow(BattleRewardData rewardData, Action onFinished)
    {
        return TryShow(rewardData, true, false, 0, onFinished);
    }

    // <변경부분>
    // 전리품과 가상 흡수 카운터 복구를 독립적으로 선택한다.
    public bool TryShow(
        BattleRewardData rewardData,
        bool useDropRewards,
        bool useRestoration,
        int virtualAbsorbCount,
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

          if ((!useDropRewards && !useRestoration) ||
            popupRoot == null ||
            confirmButton == null ||
            (useDropRewards &&
             (rewardData == null || dropSlotParent == null || rewardSlotPrefab == null)) ||
            (useRestoration &&
             (recoverySlotParent == null || pieceRepairSlotPrefab == null ||
              devoryaPawnData == null || recoverySlotParent == dropSlotParent)))
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
        // <변경부분>
        // 전리품 OFF이면 BattleRewardData 없이 복구만 실행 가능하다.
        List<BattleRewardOptionRuntimeData> rewards =
            useDropRewards
                ? rewardData.CreateRewardOptions()
                : new List<BattleRewardOptionRuntimeData>();

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

        // <변경부분>
        // 한 번만 지급하며, 전투 흡수 카운터는 수정하지 않는다.
        // 전투와 같은 보유 상한 / 확률을 사용한다.
        List<BattleRecoveryRewardRuntimeData> restored =
            useRestoration
                ? ApplyEventRestorationReward(
                    Mathf.Max(0, virtualAbsorbCount),
                    RunStateManager.Instance)
                : new List<BattleRecoveryRewardRuntimeData>();

        onRewardSelectionFinished =
            onFinished;

        pendingRewards.AddRange(
            rewards
        );

        initialRewardCount =
      pendingRewards.Count;

        hasRestorationSection = useRestoration; // <변경부분>

        pendingClaimAnimations = 0;
        finishRequested = false;
        isOpen = true;

        confirmButton.interactable = true;

        // <변경부분>
        // 전리품이 없는 복구 전용 보상창에서는
        // Confirm Button을 "계속하기" 문구로 전환한다.
        //
        // 전리품이 하나라도 사용되는 보상창에서는
        // 기존 "전리품을 받지 않고 넘기기" 문구를 유지한다.
        RefreshConfirmButtonLabel(
            useDropRewards,
            useRestoration
        );

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

        // <변경부분>
        // 전투 복구와 같은 RewardSlot UI를 사용해
        // 복구 결과를 표시한다.
        // 이미 RunState에 지급했으므로 Claim은 연결하지 않는다.
        for (int i = 0; i < restored.Count; i++)
        {
            BattleRecoveryRewardRuntimeData reward = restored[i];

            if (reward == null ||
                reward.pieceData == null ||
                reward.amount <= 0)
            {
                continue;
            }

            BattleRewardSlotUI slot = Instantiate(
                pieceRepairSlotPrefab,
                recoverySlotParent
            );

            slot.RefreshRecoveryPiece(reward.pieceData, reward.amount);
            createdRecoverySlots.Add(slot);
        }

        // <변경부분>
        // 선택하지 않은 보상 영역은 숨긴다.
        if (dropAreaObject != null)
        {
            dropAreaObject.SetActive(rewards.Count > 0);
        }

        if (dropSlotParent != null)
        {
            dropSlotParent.gameObject.SetActive(rewards.Count > 0);
        }

        if (recoveryAreaObject != null)
        {
            recoveryAreaObject.SetActive(createdRecoverySlots.Count > 0);
        }

        if (recoverySlotParent != null)
        {
            recoverySlotParent.gameObject.SetActive(createdRecoverySlots.Count > 0);
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
    // BattleEndFlowController와 동일한 기물 복구
    // 수식 / 확률 / 상한을 적용한다.
    // RunState에 지급한 뒤 결과를 PieceData별로 모아서 반환한다.
    private List<BattleRecoveryRewardRuntimeData> ApplyEventRestorationReward(
        int virtualAbsorbCount,
        RunStateManager runState)
    {
        List<BattleRecoveryRewardRuntimeData> recovered =
            new List<BattleRecoveryRewardRuntimeData>();

        int rewardCount = Mathf.Max(0, virtualAbsorbCount) / 2;

        for (int i = 0; i < rewardCount; i++)
        {
            if (runState.GetPlayerPieceCount() >= MaxPlayerPieceCount)
            {
                break;
            }

            PieceData pieceData = RollEventRestorationPieceData();

            if (pieceData == null)
            {
                continue;
            }

            PlayerPieceRuntimeData runtimeData =
                PlayerPieceRuntimeData.CreateFromPieceData(pieceData, false);

            if (runtimeData == null ||
                !runState.TryAddPlayerPiece(runtimeData, MaxPlayerPieceCount))
            {
                continue;
            }

            BattleRecoveryRewardRuntimeData existing = null;

            for (int j = 0; j < recovered.Count; j++)
            {
                if (recovered[j].pieceData == pieceData)
                {
                    existing = recovered[j];
                    break;
                }
            }

            if (existing != null)
            {
                existing.amount++;
            }
            else
            {
                recovered.Add(
                    new BattleRecoveryRewardRuntimeData(pieceData, 1)
                );
            }
        }

        Debug.Log(
            $"Event Restoration: 가상 흡수 {virtualAbsorbCount}, " +
            $"실제 복구 {GetRestoredPieceCount(recovered)}개",
            this
        );

        return recovered;
    }


    // <변경부분>
    // 전투 복구와 동일한 90% 폰 / 10% 상위 기물 추첨.
    private PieceData RollEventRestorationPieceData()
    {
        if (UnityEngine.Random.Range(0, 100) <
            Mathf.Clamp(devoryaPawnRewardPercent, 0, 100))
        {
            return devoryaPawnData;
        }

        List<PieceData> advancedCandidates = new List<PieceData>();

        if (devoryaKnightData != null)
        {
            advancedCandidates.Add(devoryaKnightData);
        }

        if (devoryaRookData != null)
        {
            advancedCandidates.Add(devoryaRookData);
        }

        if (devoryaBishopData != null)
        {
            advancedCandidates.Add(devoryaBishopData);
        }

        return advancedCandidates.Count == 0
            ? devoryaPawnData
            : advancedCandidates[
                UnityEngine.Random.Range(0, advancedCandidates.Count)
            ];
    }


    // <변경부분>
    // 실제 지급된 복구 기물 수를 계산한다.
    private static int GetRestoredPieceCount(
        List<BattleRecoveryRewardRuntimeData> recovered)
    {
        int count = 0;

        for (int i = 0; i < recovered.Count; i++)
        {
            count += recovered[i].amount;
        }

        return count;
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
        (!hasRestorationSection &&
         initialRewardCount > 0 &&
         pendingRewards.Count == 0))
        {
            FinishRewardSelection();
        }
    }

    // <변경부분>
    // 현재 Reward Selection의 구성에 따라
    // Confirm Button의 안내 문구를 전환한다.
    //
    // Drop Rewards ON:
    // "전리품을 받지 않고 넘기기"
    //
    // Drop Rewards OFF + Restoration ON:
    // "계속하기"
    //
    // 복구는 TryShow()에서 이미 RunState에 자동 지급되므로
    // Restoration 전용 Confirm Button은
    // 보상 거절이 아니라 결과 확인 / 다음 Step 진행 역할만 한다.
    private void RefreshConfirmButtonLabel(
        bool useDropRewards,
        bool useRestoration)
    {
        if (dropConfirmLabelObject == null ||
            restorationConfirmLabelObject == null)
        {
            Debug.LogWarning(
                "Event Reward Popup: " +
                "Confirm Button Label Object 연결을 확인해주세요.",
                this
            );

            return;
        }

        bool isRestorationOnly =
            !useDropRewards &&
            useRestoration;

        dropConfirmLabelObject.SetActive(
            !isRestorationOnly
        );

        restorationConfirmLabelObject.SetActive(
            isRestorationOnly
        );
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
        hasRestorationSection = false;

        // <변경부분>
        // 이전 복구 슬롯을 중복 생성하지 않도록 정리한다.
        for (int i = 0; i < createdRecoverySlots.Count; i++)
        {
            if (createdRecoverySlots[i] != null)
            {
                createdRecoverySlots[i].gameObject.SetActive(false);
                Destroy(createdRecoverySlots[i].gameObject);
            }
        }

        createdRecoverySlots.Clear();

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

        // <변경부분>
        // 복구 슬롯 레이아웃도 별도로 갱신한다.
        RectTransform recoveryRect =
            recoverySlotParent as RectTransform;

        if (recoveryRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(recoveryRect);
        }
    }
}
