using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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

    // <변경부분> 복구 보상 영역의 제목 TMP
    //
    // 실제 표시 문구는 Battle_UI String Table에서 가져오며,
    // 기존 Prefab에 입력되어 있는 한국어 문구는 Localization 실패 시 fallback으로 사용한다.
    [SerializeField]
    private TMP_Text recoveryTitleText;

    [Header("Battle Drop")]
    // <변경부분> 금화, 아이템, 유물 슬롯이 생성될 부모
    [SerializeField] private Transform dropSlotParent;

    // <변경부분> 드롭 보상이 없을 때 숨길 보상 영역
    [SerializeField] private GameObject dropAreaObject;

    // <변경부분> 전투 드롭 보상 영역의 제목 TMP
    //
    // Recovery 제목과 동일하게 Battle_UI String Table을 사용하고,
    // 기존 Prefab 문구를 안전한 한국어 fallback으로 유지한다.
    [SerializeField]
    private TMP_Text dropTitleText;

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

    // <변경부분> Reward Popup 제목과 Continue 문구의 기존 한국어 원문 보관
    //
    // Localization 값이 비어 있거나 연결되지 않은 경우에도
    // 현재 Prefab에 입력되어 있던 한국어 문구를 그대로 사용할 수 있도록
    // Awake()에서 최초 TMP 문자열을 한 번 저장한다.
    //
    // Runtime Locale 변경으로 TMP text가 바뀐 뒤의 값을 fallback으로 다시 저장하면 안 되므로
    // 최초 Awake 시점에만 캡처한다.
    private string recoveryTitleFallbackText =
        string.Empty;

    private string dropTitleFallbackText =
        string.Empty;

    private string continueFallbackText =
        string.Empty;

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


    // <변경부분>
    // 승리 팝업에서 이번 전투의 흡수 횟수를
    // 1씩 차감하고 회복 기물 슬롯을 공개하는 연출 설정.
    [Header("Absorb Settlement")]

    // <변경부분>
    // 기존 BattleUIRoot의 Absorb Count를 표시하는 컨트롤러.
    // 별도 카운트 UI는 생성하지 않는다.
    [SerializeField]
    private BattleUIController battleUIController;

    // <변경부분>
    // 흡수 카운트가 1씩 차감되는 간격.
    [SerializeField, Min(0.05f)]
    private float absorbSettlementStepInterval = 0.45f;

    // <변경부분>
    // 회복 기물 슬롯이 나타나거나 수량이 증가할 때
    // 확대/축소 연출에 걸리는 시간.
    [SerializeField, Min(0f)]
    private float recoverySlotPopDuration = 0.22f;

    // <변경부분> 슬롯 애니메이션의 시작 크기.
    [SerializeField, Range(0.1f, 1f)]
    private float recoverySlotStartScale = 0.75f;

    // <변경부분> 슬롯 애니메이션의 최대 확대 크기.
    [SerializeField, Min(1f)]
    private float recoverySlotPeakScale = 1.12f;

    // <변경부분> 현재 정산 코루틴.
    private Coroutine absorbSettlementCoroutine;

    // <변경부분>
    // true인 동안 리워드 팝업 클릭에 의한 Scene 이동을 막는다.
    private bool isAbsorbSettlementLocked;

    // <변경부분>
    // 다음 회복 기물을 공개할 종류별 슬롯 인덱스.
    private int nextRecoverySettlementIndex;

    // <변경부분>
    // 기존 보상 데이터를 변경하지 않고,
    // UI에 표시한 회복 수량만 별도로 추적한다.
    private sealed class RecoverySettlementEntry
    {
        public PieceData pieceData;
        public int totalAmount;
        public int displayedAmount;
        public BattleRewardSlotUI slotUI;
    }

    // <변경부분>
    // 이미 지급이 완료된 회복 기물의 시각적 정산 목록.
    private readonly List<RecoverySettlementEntry>
        recoverySettlementEntries =
            new List<RecoverySettlementEntry>();


    // <변경부분>
    // Battle Event Sequence가 진행 중일 때
    // 리워드 팝업의 씬 이동을 차단하기 위한 참조.
    //
    // 일반 전투에서는 Sequence가 비활성 상태이므로
    // 기존 보상 확인 기능에 영향을 주지 않는다.
    [Header("Event Sequence")]
    [SerializeField]
    private EventSequenceController eventSequenceController;


    // <변경부분> 확인 버튼 클릭 후 맵 이동을 요청할 전투 종료 컨트롤러
    private BattleEndFlowController battleEndFlowController;

    private void Awake()
    {
        if (recoveryTitleText != null)
        {
            recoveryTitleFallbackText =
                recoveryTitleText.text;
        }

        if (dropTitleText != null)
        {
            dropTitleFallbackText =
                dropTitleText.text;
        }

        if (continueText != null)
        {
            continueFallbackText =
                continueText.text;
        }

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

    // <변경부분> Reward Popup Controller가 활성화되어 있는 동안
    // Locale 변경 이벤트를 구독한다.
    //
    // BattleRewardPopupUI GameObject 자체는 계속 활성 상태를 유지하고
    // 실제 화면 표시 여부는 popupRoot가 담당하므로,
    // Popup이 열린 상태에서 언어를 변경해도 제목/Continue 문구를 즉시 갱신할 수 있다.
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    // <변경부분> 현재 Locale이 변경되면
    // Reward Popup의 고정 UI 문구만 현재 언어 기준으로 다시 계산한다.
    //
    // Slot Tooltip은 각 BattleRewardSlotUI가 별도로 Locale 변경을 구독한다.
    private void OnSelectedLocaleChanged(
        Locale locale)
    {
        RefreshLocalizedTexts();
    }

    // <변경부분> 씬 전환이나 외부 처리로
    // 팝업이 비활성화되는 경우에도
    // 진행 중인 UI 애니메이션을 안전하게 정리한다.
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        StopContinueTextBlink();

        // <변경부분>
        // Scene 종료 중 정산 코루틴이 남지 않도록 정리한다.
        StopAbsorbSettlement();
    }


    // <변경부분>
    // Event Sequence가 진행 중이면
    // 리워드 팝업에서 다음 Scene으로 이동할 수 없다.
    //
    // 기존 EventSequenceController의 런타임 상태를 사용하므로
    // 별도의 중복 잠금 플래그를 만들지 않는다.
    private bool IsEventSequenceBlockingRewardConfirm()
    {
        return
            eventSequenceController != null &&
            eventSequenceController.IsSequenceActive;
    }


    // <변경부분>
    // 리워드 팝업이 열려 있는 동안
    // Event Sequence 종료 상태를 확인한다.
    //
    // 정산과 Event Sequence가 모두 끝나면
    // Click to continue 안내를 자동으로 복구한다.
    private void Update()
    {
        if (popupRoot == null ||
            !popupRoot.activeInHierarchy)
        {
            return;
        }

        // <변경부분>
        // Dialogue 등 Event Step이 진행 중이면
        // 클릭 안내 문구도 보이지 않게 한다.
        if (IsEventSequenceBlockingRewardConfirm())
        {
            if (continueTextBlinkCoroutine != null)
            {
                StopContinueTextBlink();
            }

            SetContinueTextAlpha(0f);
            return;
        }

        // <변경부분>
        // 흡수 정산도 완료되고 Event Sequence도 끝났다면
        // 기존 클릭 안내 애니메이션을 복구한다.
        if (!isAbsorbSettlementLocked &&
            continueText != null &&
            continueTextBlinkCoroutine == null)
        {
            StartContinueTextBlink();
        }
    }


    private void RefreshLocalizedTexts()
    {
        if (recoveryTitleText != null)
        {
            recoveryTitleText.text =
                BattleUILocalization
                    .GetRewardRecoveryTitle(
                        recoveryTitleFallbackText
                    );
        }

        if (dropTitleText != null)
        {
            dropTitleText.text =
                BattleUILocalization
                    .GetRewardDropTitle(
                        dropTitleFallbackText
                    );
        }

        if (continueText != null)
        {
            continueText.text =
                BattleUILocalization
                    .GetRewardContinueText(
                        continueFallbackText
                    );
        }
    }

    // <변경부분>
    // 전투 종료 컨트롤러가 보상 계산을 끝낸 뒤 호출한다.
    //
    // absorbCount:
    // 이번 전투의 실제 흡수 성공 횟수.
    //
    // 마지막 인자의 기본값은 0으로 유지하여
    // 기존 4개 인자 호출 코드와의 호환성을 보호한다.
    public void Show(
    BattleEndFlowController owner,
    List<BattleRecoveryRewardRuntimeData> recoveryRewards,
    int goldAmount,
    List<BattleRewardOptionRuntimeData> acquiredRewards,
    int absorbCount = 0)
    {
        battleEndFlowController = owner;

        // <변경부분>
        // 명시적 참조가 없는 경우 현재 씬에서 자동 검색한다.
        if (eventSequenceController == null)
        {
            eventSequenceController =
                FindObjectOfType<EventSequenceController>();
        }

        RefreshLocalizedTexts();

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

        // <변경부분>
        // 이전 정산 코루틴과 Continue 애니메이션을 먼저 정리한다.
        StopAbsorbSettlement();
        StopContinueTextBlink();

        // <변경부분>
        // 이번 전투 흡수 횟수로 정산 시작 상태를 설정한다.
        int safeAbsorbCount =
            Mathf.Max(0, absorbCount);

        isAbsorbSettlementLocked =
      safeAbsorbCount > 0;

        // <변경부분>
        // 흡수 정산 중이거나 Event Sequence 진행 중이면
        // 팝업을 여는 순간부터 확인 버튼 입력을 차단한다.
        //
        // 마지막 Dialogue 클릭이 팝업 확인 클릭으로
        // 이어지는 문제도 방지한다.
        if (confirmButton != null)
        {
            confirmButton.interactable =
                !isAbsorbSettlementLocked &&
                !IsEventSequenceBlockingRewardConfirm();
        }

        nextRecoverySettlementIndex = 0;
        recoverySettlementEntries.Clear();

        // <변경부분>
        // 기존 전투 HUD에 있는 Absorb Count를 재사용한다.
        //
        // Inspector 연결이 비어 있다면 현재 Scene에서 찾는다.
        if (battleUIController == null)
        {
            battleUIController =
                FindObjectOfType<BattleUIController>();
        }

        // <변경부분>
        // 정산 시작 시 이번 전투의 흡수 수량을 표시한다.
        // 이 단계에서는 애니메이션을 재생하지 않는다.
        if (battleUIController != null)
        {
            battleUIController.RefreshAbsorbCount(
                safeAbsorbCount,
                false
            );
        }

        // 실제 팝업 표시 Root만 활성화한다.
        popupRoot.SetActive(
            true
        );

        // <변경부분> 이전 전투에서 생성된 슬롯 제거
        ClearCreatedSlots(recoverySlotParent);
        ClearCreatedSlots(dropSlotParent);

        // <변경부분>
        // 흡수 정산이 필요하다면 Recovery 슬롯을
        // 미리 생성하되 처음에는 숨겨 놓는다.
        //
        // Drop 보상은 기존 표시 방식을 그대로 유지한다.
        int recoverySlotCount =
            CreateRecoverySlots(
                recoveryRewards,
                isAbsorbSettlementLocked
            );

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
        // <변경부분>
        // 흡수 정산 중에는 Continue 문구를 숨기고
        // 카운트가 0이 될 때까지 확인 클릭을 잠근다.
        if (isAbsorbSettlementLocked)
        {
            SetContinueTextAlpha(0f);

            absorbSettlementCoroutine =
                StartCoroutine(
                    PlayAbsorbSettlementRoutine(
                        safeAbsorbCount
                    )
                );
        }
        else
        {
            // <변경부분>
            // 흡수가 0개였다면 별도 정산 없이
            // 기존 Continue 문구와 클릭 기능을 즉시 사용한다.
            StartContinueTextBlink();
        }

        Debug.Log(
            $"전투 보상 팝업 표시: " +
            $"복구 {recoverySlotCount}종 / " +
            $"드롭 {dropSlotCount}종"
        );
    }

    // <변경부분> 복구된 기물을 종류별 슬롯으로 생성
    // <변경부분>
    // 복구된 기물을 종류별 슬롯으로 생성한다.
    //
    // hideForSettlement == false:
    // 기존과 동일하게 최종 획득 수량을 즉시 표시한다.
    //
    // hideForSettlement == true:
    // 슬롯을 미리 생성하되 화면에서는 숨겨 놓는다.
    // 흡수 카운트 2회 차감마다 하나씩 공개한다.
    //
    // 실제 회복 보상은 여기서 지급하지 않는다.
    private int CreateRecoverySlots(
        List<BattleRecoveryRewardRuntimeData> recoveryRewards,
        bool hideForSettlement)
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

            // <변경부분>
            // 기존 Reward Slot 프리팹을 그대로 사용한다.
            BattleRewardSlotUI slotUI =
                Instantiate(
                    rewardSlotPrefab,
                    recoverySlotParent
                );

            // <변경부분>
            // 정산 중에는 최초 표시 수량을 1로 준비한다.
            // 정산이 없을 때는 기존처럼 최종 수량을 표시한다.
            slotUI.RefreshRecoveryPiece(
                reward.pieceData,
                hideForSettlement
                    ? 1
                    : reward.amount
            );

            if (hideForSettlement)
            {
                // <변경부분>
                // 실제 획득한 PieceData와 총 수량을
                // 시각적 정산 정보로만 별도 저장한다.
                recoverySettlementEntries.Add(
                    new RecoverySettlementEntry
                    {
                        pieceData = reward.pieceData,
                        totalAmount = reward.amount,
                        displayedAmount = 0,
                        slotUI = slotUI
                    }
                );

                // <변경부분>
                // 정산 전에는 슬롯을 표시하지 않는다.
                //
                // HorizontalLayoutGroup은 비활성 슬롯을
                // 레이아웃에서 제외하므로 빈칸이 생기지 않는다.
                slotUI.gameObject.SetActive(false);
            }

            createdCount++;
        }

        return createdCount;
    }


    // <변경부분>
    // 승리 리워드 흡수 카운트 정산 연출.
    //
    // - 현재 카운트를 1씩 감소시킨다.
    // - 2회 차감마다 회복 기물 1개를 공개한다.
    // - 기존 BattleUIController의 아이콘 팝/잔상 연출을 재사용한다.
    // - 모두 0이 될 때까지 보상 팝업 확인 클릭을 잠근다.
    // - TimeScale과 관계없이 연출을 진행한다.
    //
    // 주의:
    // 실제 회복 기물은 BattleEndFlowController에서
    // 이미 RunState에 지급한 상태다.
    // 이 함수는 UI 표시만 담당한다.
    private IEnumerator PlayAbsorbSettlementRoutine(
        int initialCount)
    {
        int spentCount = 0;

        for (int remaining = initialCount - 1;
             remaining >= 0;
             remaining--)
        {
            // <변경부분>
            // Inspector에서 설정한 간격만큼 기다린 뒤
            // 카운트를 하나 차감한다.
            yield return new WaitForSecondsRealtime(
                Mathf.Max(
                    0.05f,
                    absorbSettlementStepInterval
                )
            );

            // <변경부분>
            // 기존 배틀 흡수 카운트 UI의 숫자를 감소시킨다.
            //
            // true를 전달하여 기존 확대/축소 및
            // 잔상 애니메이션도 함께 실행한다.
            if (battleUIController != null)
            {
                battleUIController.RefreshAbsorbCount(
                    remaining,
                    true
                );
            }

            spentCount++;

            // <변경부분>
            // 2회 차감될 때마다 회복 기물 1개를 표시한다.
            if (spentCount % 2 == 0)
            {
                BattleRewardSlotUI revealedSlot =
                    RevealNextRecoveryReward();

                if (revealedSlot != null)
                {
                    // <변경부분>
                    // 회복 슬롯이 나타나는 순간 팝 애니메이션을
                    // 끝까지 재생한 다음 다음 카운트를 차감한다.
                    yield return PlayRecoverySlotPopRoutine(
                        revealedSlot
                    );
                }
            }
        }

        // <변경부분>
        // 흡수 카운트가 0까지 정산된 뒤에만
        // 팝업 클릭에 의한 Scene 이동을 허용한다.
        isAbsorbSettlementLocked = false;

        absorbSettlementCoroutine = null;

        // <변경부분>
        // 정산 완료 후 Click to continue 문구를 표시하고
        // 기존 반복 페이드 애니메이션을 시작한다.
        StartContinueTextBlink();
    }


    // <변경부분>
    // 이미 획득한 회복 기물 중 하나를 UI에 공개한다.
    //
    // 같은 종류의 기물이라면 기존 슬롯 수량을 증가시키고,
    // 처음 공개하는 종류라면 숨겨 둔 슬롯을 활성화한다.
    //
    // 실제 보상 지급은 절대 실행하지 않는다.
    private BattleRewardSlotUI RevealNextRecoveryReward()
    {
        while (nextRecoverySettlementIndex <
               recoverySettlementEntries.Count)
        {
            RecoverySettlementEntry entry =
                recoverySettlementEntries[
                    nextRecoverySettlementIndex
                ];

            if (entry == null ||
                entry.displayedAmount >= entry.totalAmount)
            {
                nextRecoverySettlementIndex++;
                continue;
            }

            // <변경부분>
            // 해당 종류의 UI 표시 수량을 1 증가시킨다.
            entry.displayedAmount++;

            if (entry.slotUI != null)
            {
                // <변경부분>
                // BattleRewardSlotUI 기존 기능 재사용.
                // 비활성 슬롯이라면 이 호출에서 활성화된다.
                entry.slotUI.RefreshRecoveryPiece(
                    entry.pieceData,
                    entry.displayedAmount
                );
            }

            // <변경부분>
            // 이 종류의 최종 회복 수량을 모두 표시했다면
            // 다음 종류의 슬롯으로 이동한다.
            if (entry.displayedAmount >= entry.totalAmount)
            {
                nextRecoverySettlementIndex++;
            }

            // <변경부분>
            // 새 슬롯이 나타난 뒤 가로 레이아웃을 갱신한다.
            RefreshRewardLayouts();

            return entry.slotUI;
        }

        // <변경부분>
        // 최대 기물 수 제한 등에 의해 실제 지급 보상이 없다면
        // 슬롯을 추가하지 않고 카운트 정산만 계속 진행한다.
        return null;
    }


    // <변경부분>
    // 회복 슬롯이 나타나거나 수량이 증가할 때의 팝 연출.
    //
    // 현재 프리팹 Scale을 기준으로
    // 작게 시작 → 약간 확대 → 원래 크기로 복귀한다.
    private IEnumerator PlayRecoverySlotPopRoutine(
        BattleRewardSlotUI slotUI)
    {
        if (slotUI == null ||
            recoverySlotPopDuration <= 0f)
        {
            yield break;
        }

        RectTransform slotRect =
            slotUI.transform as RectTransform;

        if (slotRect == null)
        {
            yield break;
        }

        // <변경부분>
        // 기존 프리팹 Scale을 기준값으로 보존한다.
        Vector3 originalScale =
            slotRect.localScale;

        float elapsed = 0f;

        float duration =
            Mathf.Max(
                0.01f,
                recoverySlotPopDuration
            );

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float scale;

            // <변경부분>
            // 0~40% 구간에서는 작게 시작해서 빠르게 확대한다.
            if (progress < 0.4f)
            {
                float t =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        progress / 0.4f
                    );

                scale =
                    Mathf.Lerp(
                        recoverySlotStartScale,
                        recoverySlotPeakScale,
                        t
                    );
            }
            else
            {
                // <변경부분>
                // 40~100% 구간에서는 원래 크기로 복귀한다.
                float t =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        (progress - 0.4f) / 0.6f
                    );

                scale =
                    Mathf.Lerp(
                        recoverySlotPeakScale,
                        1f,
                        t
                    );
            }

            slotRect.localScale =
                originalScale * scale;

            yield return null;
        }

        // <변경부분> 최종 크기 복구.
        slotRect.localScale =
            originalScale;
    }


    // <변경부분>
    // 씬 종료 또는 Show 재호출 시
    // 흡수 정산 코루틴이 중복 실행되지 않도록 중단한다.
    private void StopAbsorbSettlement()
    {
        if (absorbSettlementCoroutine != null)
        {
            StopCoroutine(
                absorbSettlementCoroutine
            );

            absorbSettlementCoroutine = null;
        }
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

        // <변경부분>
        // 흡수 정산 또는 Event Sequence 진행 중에는
        // Continue 안내를 표시하지 않는다.
        if (isAbsorbSettlementLocked ||
            IsEventSequenceBlockingRewardConfirm())
        {
            SetContinueTextAlpha(0f);
            return;
        }

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
    // <변경부분>
    // 보상 팝업 전체 클릭 시 처리.
    //
    // 흡수 정산 중에는 클릭을 무시한다.
    // 정산이 모두 끝난 뒤에만 기존 Scene 이동을 실행한다.
    private void OnClickConfirm()
    {
        // <변경부분>
        // 흡수 정산과 Event Sequence가 모두 완료되어야
        // 팝업 확인을 통해 Scene을 이동할 수 있다.
        //
        // Button.interactable 외에도 함수 내부에서 검사하여
        // 다른 경로의 중복 호출까지 방어한다.
        if (isAbsorbSettlementLocked ||
            IsEventSequenceBlockingRewardConfirm())
        {
            return;
        }

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

        // <변경부분>
        // 모든 정산이 완료된 경우에만 기존 Scene 이동 실행.
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
