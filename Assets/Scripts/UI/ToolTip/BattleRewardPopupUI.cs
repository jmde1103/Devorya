using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    // <변경부분> 팝업 전체 클릭 영역으로 사용할 버튼
    // 새 UI에서는 별도의 작은 확인 버튼이 아니라
    // 팝업 페이지 전체를 덮는 투명 Button을 연결한다.
    [SerializeField]
    private Button confirmButton;

    [Header("Continue Text Animation")]
    // <변경부분> 팝업 하단의 "Click to continue" 안내 문구
    [SerializeField]
    private TMP_Text continueText;

    // <변경부분> 안내 문구가 가장 흐려졌을 때의 알파값
    // 완전히 사라지지 않도록 기본값은 0.25로 사용한다.
    [SerializeField, Range(0f, 1f)]
    private float continueTextMinAlpha = 0.25f;

    // <변경부분> 최대 알파 → 최소 알파 또는
    // 최소 알파 → 최대 알파로 이동하는 시간
    [SerializeField, Min(0.01f)]
    private float continueTextFadeDuration = 0.8f;

    // <변경부분> 현재 실행 중인
    // Click to continue 반복 애니메이션 코루틴
    private Coroutine continueTextBlinkCoroutine;

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

        // <변경부분> BattleRewardPopupUI와 PopupOpenAnimator가
        // 붙어 있는 Controller GameObject 자체는 비활성화하지 않는다.
        //
        // 이 오브젝트가 비활성화되면
        // PopupOpenAnimator.PlayOpen()과
        // Click to continue 반복 애니메이션의
        // StartCoroutine()을 실행할 수 없다.
        //
        // 실제 화면 표시 / 숨김은 별도의 popupRoot 자식 오브젝트가 담당한다.
        if (popupRoot != null &&
            popupRoot != gameObject)
        {
            popupRoot.SetActive(
                false
            );
        }
        else if (popupRoot == gameObject)
        {
            Debug.LogWarning(
                "BattleRewardPopupUI 설정 경고: " +
                "Popup Root가 BattleRewardPopupUI가 붙은 " +
                "GameObject 자체로 연결되어 있습니다. " +
                "실제 팝업 표시용 자식 Root를 별도로 연결하세요."
            );
        }
    }

    // <변경부분> 씬 전환이나 외부 처리로
    // 팝업이 비활성화되는 경우에도
    // Click to continue 애니메이션을 안전하게 정리한다.
    private void OnDisable()
    {
        StopContinueTextBlink();
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

        // <변경부분> 코루틴을 실행하는 BattleRewardPopupUI가
        // 비활성화된 상태에서는 PopupOpenAnimator와
        // Continue Text 애니메이션을 시작할 수 없다.
        //
        // 정상 구조에서는 이 GameObject는 항상 Active이고,
        // 실제 시각적 팝업 Root만 ON / OFF 한다.
        if (gameObject.activeInHierarchy == false)
        {
            Debug.LogWarning(
                "보상 팝업 표시 실패: " +
                "BattleRewardPopupUI GameObject가 비활성화되어 있습니다."
            );

            return;
        }

        // 실제 팝업 표시 Root만 활성화한다.
        popupRoot.SetActive(
            true
        );

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

        // <변경부분> 보상 팝업이 완전히 표시되면
        // "Click to continue" 안내 문구의
        // 부드러운 반복 페이드 애니메이션을 시작한다.
        StartContinueTextBlink();

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

    // <변경부분> "Click to continue" 텍스트의
    // 반복 페이드 애니메이션을 시작한다.
    private void StartContinueTextBlink()
    {
        // 이전에 실행 중이던 코루틴이 있다면
        // 중복 실행되지 않도록 먼저 종료한다.
        StopContinueTextBlink();

        if (continueText == null)
        {
            return;
        }

        // 팝업이 열릴 때는 항상
        // 완전히 보이는 상태에서 시작한다.
        SetContinueTextAlpha(
            1f
        );

        continueTextBlinkCoroutine =
            StartCoroutine(
                ContinueTextBlinkRoutine()
            );
    }

    // <변경부분> "Click to continue" 문구를
    // 최대 알파와 최소 알파 사이에서
    // 부드럽게 계속 왕복시키는 코루틴
    private IEnumerator ContinueTextBlinkRoutine()
    {
        while (true)
        {
            // 완전히 보이는 상태에서
            // 지정한 최소 알파까지 천천히 흐려진다.
            yield return
                FadeContinueTextAlphaRoutine(
                    1f,
                    continueTextMinAlpha
                );

            // 최소 알파 상태에서
            // 다시 완전히 보이는 상태까지 밝아진다.
            yield return
                FadeContinueTextAlphaRoutine(
                    continueTextMinAlpha,
                    1f
                );
        }
    }

    // <변경부분> 지정한 두 알파값 사이를
    // 일정 시간 동안 부드럽게 보간한다.
    //
    // UI 안내 애니메이션이므로
    // Time.timeScale 영향을 받지 않는
    // unscaledDeltaTime을 사용한다.
    private IEnumerator FadeContinueTextAlphaRoutine(
        float startAlpha,
        float endAlpha)
    {
        float elapsedTime =
            0f;

        float duration =
            Mathf.Max(
                0.01f,
                continueTextFadeDuration
            );

        while (elapsedTime < duration)
        {
            if (continueText == null)
            {
                yield break;
            }

            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );

            // 시작과 끝에서 속도가 자연스럽게 줄어드는
            // SmoothStep 곡선을 사용한다.
            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            float currentAlpha =
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    smoothProgress
                );

            SetContinueTextAlpha(
                currentAlpha
            );

            yield return null;
        }

        SetContinueTextAlpha(
            endAlpha
        );
    }

    // <변경부분> TMP 텍스트의 기존 RGB 색상은 유지하고
    // 알파값만 변경한다.
    private void SetContinueTextAlpha(
        float alpha)
    {
        if (continueText == null)
        {
            return;
        }

        Color textColor =
            continueText.color;

        textColor.a =
            Mathf.Clamp01(
                alpha
            );

        continueText.color =
            textColor;
    }

    // <변경부분> 실행 중인 안내 문구 코루틴을 종료하고
    // 텍스트를 정상 알파값으로 복구한다.
    private void StopContinueTextBlink()
    {
        if (continueTextBlinkCoroutine != null)
        {
            StopCoroutine(
                continueTextBlinkCoroutine
            );

            continueTextBlinkCoroutine =
                null;
        }

        SetContinueTextAlpha(
            1f
        );
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
    // <변경부분> 보상 팝업 전체 클릭 시
    // 안내 문구 애니메이션을 종료하고
    // BattleEndFlowController가 설정한 맵 씬으로 이동한다.
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

        // 팝업이 닫히기 전에
        // Click to continue 반복 애니메이션을 정리한다.
        StopContinueTextBlink();

        if (popupRoot != null)
        {
            popupRoot.SetActive(
                false
            );
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
