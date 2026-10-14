using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// <변경부분> 보상 팝업에 생성되는 공통 슬롯 하나를 관리
// 기물, 금화, 아이템, 유물이 모두 같은 슬롯 프리팹을 사용한다.
public class BattleRewardSlotUI : MonoBehaviour
{
    [Header("Drop Reward UI")]
    // <변경부분>
    // 아이템 / 유물 / 금화 드롭 보상에 사용하는 긴 바 형태 루트
    [SerializeField] private GameObject dropLayoutRoot;

    // 드롭 보상 아이콘
    [SerializeField] private Image dropIconImage;

    // 드롭 보상 이름 (가운데 표시)
    [SerializeField] private TMP_Text rewardNameText;

    // 드롭 보상 개수 (숫자만 표시, x 아이콘은 프리팹에서 별도 처리)
    [SerializeField] private TMP_Text dropAmountText;

    [Header("Recovery Reward UI")]
    // <변경부분>
    // 회복된 기물 전용 기존 슬롯 형태 루트
    [SerializeField] private GameObject recoveryLayoutRoot;

    // 회복된 기물 타입 아이콘
    [SerializeField] private Image recoveryIconImage;

    // 회복 수량 표시
    [SerializeField] private TMP_Text recoveryAmountText;

    [Header("Tooltip")]
    // <변경부분> 슬롯 전체를 꾹 눌렀을 때 기존 TooltipPopupUI를 호출
    [SerializeField] private TooltipTrigger tooltipTrigger;

    [Header("Recovery Type Icons")]
    // <변경부분> Recovery 보상 슬롯에서 PieceType별로 표시할 타입 아이콘
    [SerializeField] private Sprite pawnTypeIconSprite;
    [SerializeField] private Sprite rookTypeIconSprite;
    [SerializeField] private Sprite bishopTypeIconSprite;
    [SerializeField] private Sprite knightTypeIconSprite;
    [SerializeField] private Sprite kingTypeIconSprite;
    [SerializeField] private Sprite queenTypeIconSprite;
    [SerializeField] private Sprite specialTypeIconSprite;

    // <변경부분>
    // 현재 공용 Reward Slot이 Recovery Piece를 표시하고 있는지 기록한다.
    private bool isRecoveryRewardSlot;

    // <변경부분>
    // Locale 변경 시 현재 기물 이름/설명을 다시 Resolve하기 위한 source.
    private PieceData cachedRecoveryPieceData;

    // <변경부분>
    // 현재 이 Reward Slot이 Gold를 표시하고 있는지 기록한다.
    private bool isGoldRewardSlot;

    // <변경부분>
    // Gold Tooltip의 정적 콘텐츠 SSOT.
    //
    // Locale 변경 시 TooltipData_Gold에서
    // 현재 Locale 문자열을 다시 resolve하기 위해 source를 보관한다.
    private TooltipData cachedGoldTooltipData;

    // <변경부분>
    // "현재 보유 금화: {gold}"를 다시 생성하기 위한
    // 현재 RunState Gold 수량.
    //
    // 실제 Gold 상태의 SSOT를 여기로 옮기는 것이 아니라,
     // 이미 RefreshGold()로 전달된 현재 표시값만 Runtime cache한다.
    private int cachedCurrentGoldAmount;

    // <변경부분>
    // 선택 수령 가능한 보상 슬롯에서만 설정되는 지급 요청.
    //
    // true  = 실제 보상 지급 완료
    // false = 지급 실패, 슬롯 유지
    private Func<bool> tryClaimReward;

    // 수령 성공 후 팝업 레이아웃 갱신을 요청하는 콜백.
    private Action onClaimSucceeded;

    // 클릭 수령에 사용하는 Button.
    // 기존 프리팹에 없으면 런타임에서만 추가한다.
    private Button claimButton;

    // <변경부분>
    // 수령 성공 시 슬롯이 바로 사라지지 않고
    // 짧은 확대 + 잔상 연출 후 사라지도록 하는 코루틴.
    private Coroutine claimSuccessCoroutine;

    [Header("Claim Animation")]
    // <변경부분>
    // 수령 성공 시 슬롯 전체 확대 연출 길이
    [SerializeField, Min(0f)]
    private float claimPopDuration = 0.18f;

    // <변경부분>
    // 수령 성공 시 슬롯 전체 최대 확대 배율
    [SerializeField, Min(1f)]
    private float claimPeakScale = 1.12f;

    // <변경부분>
    // 아이콘 잔상 생성 횟수
    [SerializeField, Min(0)]
    private int claimAfterimageCount = 2;

    // <변경부분>
    // 잔상 하나가 사라질 때까지 걸리는 시간
    [SerializeField, Min(0.01f)]
    private float claimAfterimageFadeDuration = 0.16f;

    // <변경부분>
    // 잔상이 원본보다 살짝 커지며 퍼지는 배율
    [SerializeField, Min(1f)]
    private float claimAfterimageScaleMultiplier = 1.08f;


    // <변경부분>
    // 금화·아이템·유물 슬롯에만 선택 수령 기능을 연결한다.
    //
    // Recovery 슬롯에는 호출하지 않으므로
    // 기존 자동 복구 보상 표시에는 영향을 주지 않는다.
    public void ConfigureClaim(
        Func<bool> claimAction,
        Action successAction)
    {
        tryClaimReward = claimAction;
        onClaimSucceeded = successAction;

        if (claimAction == null)
        {
            return;
        }

        // 루트에 클릭 판정용 Graphic이 있는지 확인한다.
        Graphic rootGraphic =
            GetComponent<Graphic>();

        if (rootGraphic == null)
        {
            // 아이콘 Image가 자식에만 있는 프리팹도
            // 슬롯 전체를 클릭할 수 있도록 만든다.
            Image clickAreaImage =
                gameObject.AddComponent<Image>();

            clickAreaImage.color =
                Color.clear;

            clickAreaImage.raycastTarget =
                true;

            rootGraphic =
                clickAreaImage;
        }
        else
        {
            rootGraphic.raycastTarget =
                true;
        }

        if (claimButton == null)
        {
            claimButton =
                GetComponent<Button>();

            if (claimButton == null)
            {
                claimButton =
                    gameObject.AddComponent<Button>();

                // 기존 보상 슬롯의 외형을 변경하지 않는다.
                claimButton.transition =
                    Selectable.Transition.None;
            }
        }

        claimButton.targetGraphic =
            rootGraphic;

        // 중복 리스너 등록 방지.
        claimButton.onClick.RemoveListener(
            OnClickClaim
        );

        claimButton.onClick.AddListener(
            OnClickClaim
        );

        claimButton.interactable =
            true;
    }


    // <변경부분>
    // 플레이어가 보상 슬롯을 클릭했을 때 실행한다.
    private void OnClickClaim()
    {
        if (tryClaimReward == null)
        {
            return;
        }

        // RunStateManager에 실제 지급이 성공했는지
        // BattleEndFlowController를 통해 확인한다.
        bool claimSucceeded =
            tryClaimReward.Invoke();

        if (claimSucceeded == false)
        {
            // 아이템 슬롯 초과, 유물 중복 등의 경우
            // 보상을 지급하지 않고 슬롯을 유지한다.
            return;
        }

        Action successCallback =
            onClaimSucceeded;

        // 한 번 성공한 슬롯은 재클릭할 수 없도록 한다.
        tryClaimReward = null;
        onClaimSucceeded = null;

        if (claimButton != null)
        {
            claimButton.interactable =
                false;

            claimButton.onClick.RemoveListener(
                OnClickClaim
            );
        }

        if (tooltipTrigger != null)
        {
            tooltipTrigger.enabled = false;
        }

        if (claimSuccessCoroutine != null)
        {
            StopCoroutine(claimSuccessCoroutine);
            claimSuccessCoroutine = null;
        }

        claimSuccessCoroutine =
            StartCoroutine(
                PlayClaimSuccessRoutine(successCallback)
            );
    }

    // <변경부분>
    // 수령 성공 시 짧은 확대 + 잔상 연출을 재생한 뒤
    // 슬롯을 숨기고 레이아웃 갱신 콜백을 호출한다.
    private IEnumerator PlayClaimSuccessRoutine(
        Action successCallback)
    {
        RectTransform slotRect =
            transform as RectTransform;

        Vector3 originalScale =
            slotRect != null
                ? slotRect.localScale
                : transform.localScale;

        int spawnedAfterimageCount = 0;
        float duration =
            Mathf.Max(
                0.01f,
                claimPopDuration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float scale;

            // 흡수 카운트 느낌과 비슷하게
            // 빠르게 커졌다가 원래 크기로 복귀
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
                        1f,
                        claimPeakScale,
                        t
                    );
            }
            else
            {
                float t =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        (progress - 0.4f) / 0.6f
                    );

                scale =
                    Mathf.Lerp(
                        claimPeakScale,
                        1f,
                        t
                    );
            }

            if (slotRect != null)
            {
                slotRect.localScale =
                    originalScale * scale;
            }
            else
            {
                transform.localScale =
                    originalScale * scale;
            }

            // <변경부분>
            // 드롭 보상 전용 아이콘에만 잔상 연출을 적용한다.
            // 기물 복구 아이콘은 기존 정산 연출을 유지한다.
            if (dropIconImage != null &&
                claimAfterimageCount > 0)
            {
                int targetAfterimageCount =
                    Mathf.FloorToInt(
                        progress * claimAfterimageCount
                    );

                while (spawnedAfterimageCount <
                       targetAfterimageCount)
                {
                    spawnedAfterimageCount++;
                    SpawnClaimAfterimage();
                }
            }

            yield return null;
        }

        if (slotRect != null)
        {
            slotRect.localScale =
                originalScale;
        }
        else
        {
            transform.localScale =
                originalScale;
        }

        claimSuccessCoroutine = null;

        // 애니메이션이 끝난 뒤 슬롯을 숨긴다.
        gameObject.SetActive(false);

        // 숨긴 뒤 나머지 슬롯 레이아웃을 갱신한다.
        successCallback?.Invoke();
    }

    // <변경부분>
    // 드롭 보상 클릭 수령 시 아이콘 기준 잔상을 생성한다.
    // 회복된 기물은 기존 흡수 정산 연출을 그대로 사용한다.
    private void SpawnClaimAfterimage()
    {
        if (dropIconImage == null)
        {
            return;
        }

        RectTransform iconRect =
            dropIconImage.rectTransform;

        if (iconRect == null ||
            iconRect.parent == null)
        {
            return;
        }

        GameObject afterimageObject =
            new GameObject(
                "ClaimAfterimage",
                typeof(RectTransform),
                typeof(Image)
            );

        afterimageObject.transform.SetParent(
            iconRect.parent,
            false
        );

        RectTransform afterimageRect =
            afterimageObject.GetComponent<RectTransform>();

        afterimageRect.anchorMin =
            iconRect.anchorMin;
        afterimageRect.anchorMax =
            iconRect.anchorMax;
        afterimageRect.pivot =
            iconRect.pivot;
        afterimageRect.anchoredPosition =
            iconRect.anchoredPosition;
        afterimageRect.sizeDelta =
            iconRect.sizeDelta;
        afterimageRect.localScale =
            iconRect.localScale;

        Image afterimageImage =
            afterimageObject.GetComponent<Image>();

        afterimageImage.sprite =
            dropIconImage.sprite;
        afterimageImage.preserveAspect =
            dropIconImage.preserveAspect;
        afterimageImage.raycastTarget =
            false;

        Color afterimageColor =
            dropIconImage.color;

        afterimageColor.a =
            Mathf.Clamp01(
                afterimageColor.a * 0.55f
            );

        afterimageImage.color =
            afterimageColor;

        // 원본 아이콘 뒤에 위치하도록 먼저 보이게
        afterimageObject.transform.SetSiblingIndex(
            iconRect.GetSiblingIndex()
        );

        StartCoroutine(
            FadeClaimAfterimageRoutine(
                afterimageRect,
                afterimageImage
            )
        );
    }

    // <변경부분>
    // 잔상이 살짝 커지며 사라지는 연출
    private IEnumerator FadeClaimAfterimageRoutine(
        RectTransform afterimageRect,
        Image afterimageImage)
    {
        if (afterimageRect == null ||
            afterimageImage == null)
        {
            yield break;
        }

        Vector3 startScale =
            afterimageRect.localScale;

        Vector3 endScale =
            startScale *
            claimAfterimageScaleMultiplier;

        Color startColor =
            afterimageImage.color;

        float elapsed = 0f;
        float duration =
            Mathf.Max(
                0.01f,
                claimAfterimageFadeDuration
            );

        while (elapsed < duration)
        {
            if (afterimageRect == null ||
                afterimageImage == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            afterimageRect.localScale =
                Vector3.Lerp(
                    startScale,
                    endScale,
                    smoothProgress
                );

            Color currentColor =
                startColor;

            currentColor.a =
                Mathf.Lerp(
                    startColor.a,
                    0f,
                    smoothProgress
                );

            afterimageImage.color =
                currentColor;

            yield return null;
        }

        if (afterimageRect != null)
        {
            Destroy(afterimageRect.gameObject);
        }
    }

    private void Awake()
    {
        // <변경부분> Inspector 연결이 빠져도 루트 또는 자식에서 자동 탐색
        if (tooltipTrigger == null)
        {
            tooltipTrigger =
                GetComponent<TooltipTrigger>();

            if (tooltipTrigger == null)
            {
                tooltipTrigger =
                    GetComponentInChildren<TooltipTrigger>(
                        true
                    );
            }
        }

        // 드롭 루트 자동 탐색
        if (dropLayoutRoot == null)
        {
            Transform found =
                transform.Find("DropLayoutRoot");

            if (found != null)
            {
                dropLayoutRoot = found.gameObject;
            }
        }

        // 회복 루트 자동 탐색
        if (recoveryLayoutRoot == null)
        {
            Transform found =
                transform.Find("RecoveryLayoutRoot");

            if (found != null)
            {
                recoveryLayoutRoot = found.gameObject;
            }
        }

        // <변경부분>
        // 별도 LayoutRoot가 없는 독립형 프리팹도 지원한다.
        //
        // LayoutRoot가 있으면 해당 자식에서 찾고,
        // 없다면 프리팹 루트의 직접 자식에서 찾는다.
        Transform dropSearchRoot =
            dropLayoutRoot != null
                ? dropLayoutRoot.transform
                : transform;

        if (dropIconImage == null)
        {
            Transform found =
                dropSearchRoot.Find("IconImage");

            if (found != null)
            {
                dropIconImage =
                    found.GetComponent<Image>();
            }
        }

        if (rewardNameText == null)
        {
            Transform found =
                dropSearchRoot.Find("RewardNameText");

            if (found != null)
            {
                rewardNameText =
                    found.GetComponent<TMP_Text>();
            }
        }

        if (dropAmountText == null)
        {
            Transform found =
                dropSearchRoot.Find("AmountText");

            if (found != null)
            {
                dropAmountText =
                    found.GetComponent<TMP_Text>();
            }
        }

        // <변경부분>
        // PieceRepairSlot에서도 독립적인 IconImage를 사용한다.
        Transform recoverySearchRoot =
            recoveryLayoutRoot != null
                ? recoveryLayoutRoot.transform
                : transform;

        if (recoveryIconImage == null)
        {
            Transform found =
                recoverySearchRoot.Find("RecoveryIconImage");

            // 기존 기물 회복 프리팹에서는
            // 이미지 이름이 IconImage일 수 있으므로
            // 동일한 이름도 지원한다.
            if (found == null)
            {
                found =
                    recoverySearchRoot.Find("IconImage");
            }

            if (found != null)
            {
                recoveryIconImage =
                    found.GetComponent<Image>();
            }
        }

        if (recoveryAmountText == null)
        {
            Transform found =
                recoverySearchRoot.Find("AmountText");

            if (found != null)
            {
                recoveryAmountText =
                    found.GetComponent<TMP_Text>();
            }
        }
    }

    // <변경부분>
    // Reward Popup이 열린 상태에서 Locale이 변경되면
    // Gold Tooltip의 문자열 데이터만 현재 Locale 기준으로 다시 만든다.
    private void OnEnable()
    {
        // 중복 등록 방지.
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        // <변경부분>
        // 보상 슬롯 비활성화 시 클릭 리스너를 정리한다.
        // 런타임에 생성된 슬롯이 중복 호출되는 것을 방지한다.
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(
                OnClickClaim
            );
        }

        if (claimSuccessCoroutine != null)
        {
            StopCoroutine(claimSuccessCoroutine);
            claimSuccessCoroutine = null;
        }

        tryClaimReward = null;
        onClaimSucceeded = null;
    }

    // <변경부분>
    // 복구된 기물 Slot을 표시한다.
    //
    // PieceData:
    // 현재 Locale 기준 기물 이름 / 기물 자체 설명.
    //
    // Battle_UI:
    // Reward 복구 화면 전용 Category / 안내 문장.
    //
    // Ability 정보는 Recovery Tooltip에 표시하지 않는다.
    public void RefreshRecoveryPiece(
      PieceData pieceData,
      int amount)
    {
        ClearGoldRuntimeCache();
        ClearRecoveryRuntimeCache();

        if (pieceData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        isRecoveryRewardSlot =
            true;

        cachedRecoveryPieceData =
            pieceData;

        Sprite displayIcon =
            GetRecoveryTypeIcon(
                pieceData.pieceType
            );

        TooltipViewData recoveryTooltipViewData =
            CreateRecoveryTooltipViewData(
                pieceData
            );

        ApplyRecoveryLayout(
            displayIcon,
            amount,
            recoveryTooltipViewData
        );
    }

    // <변경부분>
    // Recovery 화면에서 사용할 최종 TooltipViewData를 생성한다.
    //
    // PieceData가 소유하는 이름/설명을 현재 Locale 기준으로 가져온 뒤,
    // Reward 전용 Category와 복구 안내 문장만 조합한다.
    //
    // Ability / Trait 등 상세 전투 정보는
    // Recovery Tooltip에서 별도로 표시하지 않는다.
    private TooltipViewData CreateRecoveryTooltipViewData(
        PieceData pieceData)
    {
        if (pieceData == null)
        {
            return null;
        }

        TooltipViewData recoveryTooltipViewData =
            TooltipViewData.FromPieceData(
                pieceData
            );

        if (recoveryTooltipViewData == null)
        {
            return null;
        }

        string pieceDisplayName =
            recoveryTooltipViewData.title;

        string recoveryDescription =
    BattleUILocalization
        .GetRewardRecoveryDescription(
            pieceDisplayName
        );

        // PieceData 자체 Description은 그대로 보존하지만,
        // Recovery Reward Tooltip에서는 표시하지 않는다.
        //
        // 해당 Description은 이후 도감 / 기물 상세정보 등
        // PieceData 자체 정보를 보여주는 UI에서 재사용한다.
        recoveryTooltipViewData.category =
            BattleUILocalization
                .GetRewardRecoveryCategory();

        recoveryTooltipViewData.mainDescription =
            recoveryDescription;

        return recoveryTooltipViewData;
    }
    // <변경부분> 아이템 보상 슬롯 표시
    public void RefreshItem(
     BattleItemData itemData,
     int amount)
    {
        ClearGoldRuntimeCache();
        ClearRecoveryRuntimeCache();

        if (itemData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        TooltipViewData tooltipViewData =
            TooltipViewData.FromBattleItemData(
                itemData
            );

        string displayName =
            tooltipViewData != null
                ? tooltipViewData.title
                : itemData.itemName;

        ApplyDropLayout(
            itemData.iconSprite,
            displayName,
            amount,
            tooltipViewData
        );
    }

    // <변경부분> 유물 보상 슬롯 표시
    public void RefreshRelic(
      BattleRelicData relicData,
      int amount)
    {
        ClearGoldRuntimeCache();
        ClearRecoveryRuntimeCache();

        if (relicData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        TooltipViewData tooltipViewData =
            TooltipViewData.FromBattleRelicData(
                relicData
            );

        string displayName =
            tooltipViewData != null
                ? tooltipViewData.title
                : relicData.relicName;

        ApplyDropLayout(
            relicData.iconSprite,
            displayName,
            amount,
            tooltipViewData
        );
    }

    // <변경부분> Gold Reward Tooltip 표시.
    //
    // Gold의 정적 콘텐츠인
    // title / category / mainDescription / icon은
    // TooltipData_Gold가 Localization SSOT로 직접 관리한다.
    //
    // "현재 보유 금화: {gold}"는 런타임 상태가 포함된 UI 문장이므로
    // Battle_UI Localization을 계속 사용한다.
    //
    // Locale 변경 시 이 함수에서 저장한 source와 현재 Gold 표시값을 이용해
    // Tooltip 문자열 데이터만 다시 생성한다.
    public void RefreshGold(
      TooltipData goldTooltipData,
      int acquiredGoldAmount,
      int currentGoldAmount)
    {
        ClearRecoveryRuntimeCache();

        if (goldTooltipData == null)
        {
            ClearGoldRuntimeCache();

            Debug.LogWarning(
                "금화 보상 슬롯 표시 실패: " +
                "goldTooltipData가 연결되지 않았습니다."
            );

            gameObject.SetActive(false);
            return;
        }

        isGoldRewardSlot =
            true;

        cachedGoldTooltipData =
            goldTooltipData;

        cachedCurrentGoldAmount =
            currentGoldAmount;

        TooltipViewData goldTooltipViewData =
            CreateGoldTooltipViewData(
                goldTooltipData,
                currentGoldAmount
            );

        string displayName =
            goldTooltipViewData != null
                ? goldTooltipViewData.title
                : goldTooltipData.title;

        ApplyDropLayout(
            goldTooltipData.icon,
            displayName,
            acquiredGoldAmount,
            goldTooltipViewData
        );
    }

    // <변경부분>
    // Gold Tooltip의 표시용 ViewData를
    // 호출 시점의 현재 Locale 기준으로 생성한다.
    //
    // TooltipData_Gold:
    // title / category / mainDescription
    //
    // Battle_UI:
    // 현재 보유 Gold 문장
    //
    // 실제 Gold 숫자:
    // RefreshGold()로 전달된 RunState 기반 값
    private TooltipViewData CreateGoldTooltipViewData(
        TooltipData goldTooltipData,
        int currentGoldAmount)
    {
        if (goldTooltipData == null)
        {
            return null;
        }

        TooltipViewData goldTooltipViewData =
            TooltipViewData.FromTooltipData(
                goldTooltipData
            );

        if (goldTooltipViewData == null)
        {
            return null;
        }

        string baseDescription =
            goldTooltipViewData.mainDescription;

        string currentGoldText =
            BattleUILocalization
                .GetRewardCurrentGoldText(
                    currentGoldAmount
                );

        if (string.IsNullOrWhiteSpace(
                baseDescription))
        {
            goldTooltipViewData.mainDescription =
                currentGoldText;
        }
        else
        {
            goldTooltipViewData.mainDescription =
                $"{baseDescription}\n\n" +
                $"{currentGoldText}";
        }

        return goldTooltipViewData;
    }

    // <변경부분>
    // Reward Popup이 열린 상태에서 Locale이 변경되면
    // 현재 Slot Tooltip의 문자열 데이터만 다시 Resolve한다.
    //
    // Icon / Amount / Slot 활성화 / Popup Animation은
    // 다시 실행하지 않는다.
    private void OnSelectedLocaleChanged(
        Locale selectedLocale)
    {
        if (gameObject.activeInHierarchy == false ||
            tooltipTrigger == null)
        {
            return;
        }

        // Recovery Piece.
        if (isRecoveryRewardSlot &&
     cachedRecoveryPieceData != null)
        {
            TooltipViewData recoveryTooltipViewData =
                CreateRecoveryTooltipViewData(
                    cachedRecoveryPieceData
                );

            tooltipTrigger.SetTooltipViewData(
                recoveryTooltipViewData
            );

            return;
        }

        // Gold.
        if (isGoldRewardSlot &&
            cachedGoldTooltipData != null)
        {
            TooltipViewData localizedGoldTooltipViewData =
                CreateGoldTooltipViewData(
                    cachedGoldTooltipData,
                    cachedCurrentGoldAmount
                );

            tooltipTrigger.SetTooltipViewData(
                localizedGoldTooltipViewData
            );
        }
    }

    // <변경부분>
    // 공용 Reward Slot이 다른 보상 타입으로 재사용될 때
    // 이전 Recovery Piece source를 제거한다.
    private void ClearRecoveryRuntimeCache()
    {
        isRecoveryRewardSlot =
            false;

        cachedRecoveryPieceData =
            null;
    }

    // <변경부분>
    // 공용 Reward Slot이 Gold 이외의 보상으로 재사용될 때
    // 이전 Gold Locale refresh 정보가 남지 않도록 정리한다.
    private void ClearGoldRuntimeCache()
    {
        isGoldRewardSlot =
            false;

        cachedGoldTooltipData =
            null;

        cachedCurrentGoldAmount =
            0;
    }

    // <변경부분> PieceType에 맞는 Recovery 타입 아이콘 반환
    private Sprite GetRecoveryTypeIcon(
        PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return pawnTypeIconSprite;

            case PieceType.Rook:
                return rookTypeIconSprite;

            case PieceType.Bishop:
                return bishopTypeIconSprite;

            case PieceType.Knight:
                return knightTypeIconSprite;

            case PieceType.King:
                return kingTypeIconSprite;

            case PieceType.Queen:
                return queenTypeIconSprite;

            case PieceType.Special:
                return specialTypeIconSprite;

            default:
                Debug.LogWarning(
                    $"Recovery 타입 아이콘 없음: {pieceType}"
                );

                return null;
        }
    }

    // <변경부분>
    // BattleRewardSlot 프리팹 전용 드롭 보상 표시.
    //
    // 이제 회복 기물과 프리팹이 분리되어 있으므로
    // 다른 레이아웃을 비활성화하지 않는다.
    private void ApplyDropLayout(
        Sprite icon,
        string rewardName,
        int amount,
        TooltipViewData tooltipViewData)
    {
        gameObject.SetActive(true);

        // 드롭 전용 레이아웃 루트가 별도로 있다면
        // 해당 루트만 활성화한다.
        if (dropLayoutRoot != null)
        {
            dropLayoutRoot.SetActive(true);
        }

        if (dropIconImage != null)
        {
            dropIconImage.sprite = icon;
            dropIconImage.enabled =
                icon != null;
        }
        else
        {
            Debug.LogWarning(
                "BattleRewardSlot: Drop Icon Image가 연결되지 않았습니다.",
                this
            );
        }

        if (rewardNameText != null)
        {
            rewardNameText.text =
                string.IsNullOrWhiteSpace(rewardName)
                    ? string.Empty
                    : rewardName;
        }

        if (dropAmountText != null)
        {
            dropAmountText.text =
                Mathf.Max(0, amount).ToString();
        }

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetTooltipViewData(
                tooltipViewData
            );
        }
    }

    // <변경부분>
    // PieceRepairSlot 프리팹 전용 기물 복구 표시.
    //
    // 복구 슬롯에서는 금화/아이템/유물 레이아웃을
    // 찾아서 비활성화하지 않는다.
    //
    // 기존 흡수 정산에서 숨김 → 공개되는 흐름은
    // BattleRewardPopupUI가 별도로 관리한다.
    private void ApplyRecoveryLayout(
        Sprite icon,
        int amount,
        TooltipViewData tooltipViewData)
    {
        gameObject.SetActive(true);

        // 복구 전용 레이아웃 루트가 별도로 있다면
        // 해당 루트만 활성화한다.
        if (recoveryLayoutRoot != null)
        {
            recoveryLayoutRoot.SetActive(true);
        }

        if (recoveryIconImage != null)
        {
            recoveryIconImage.sprite = icon;
            recoveryIconImage.enabled =
                icon != null;
        }
        else
        {
            Debug.LogWarning(
                "PieceRepairSlot: Recovery Icon Image가 연결되지 않았습니다.",
                this
            );
        }

        if (recoveryAmountText != null)
        {
            recoveryAmountText.text =
                Mathf.Max(0, amount).ToString();
        }

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetTooltipViewData(
                tooltipViewData
            );
        }
    }
}