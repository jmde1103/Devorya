using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// <변경부분> 전투 화면의 버튼 UI를 관리하는 컨트롤러
public class BattleUIController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private BattleManager battleManager;

    [Header("Action Buttons")]
    [SerializeField] private Button absorbButton;
    [SerializeField] private Button uniqueSkillButton;

    // <변경부분> 테스트용 아이템 추가 버튼
    [SerializeField] private Button debugAddItemButton;
    // <변경부분> 테스트용 강제 턴 넘기기 버튼
    [SerializeField] private Button debugForceEndTurnButton;
    // <변경부분> 테스트용 유물 추가 버튼
    [SerializeField] private Button debugAddRelicButton;


    [Header("Piece Status UI")]
    // <변경부분> 플레이어 선택 기물 정보를 표시하는 스테이터스 UI
    [SerializeField] private PieceStatusUIController playerStatusUIController;
    // <변경부분> 상대 선택 기물 정보를 표시하는 스테이터스 UI
    [SerializeField] private PieceStatusUIController enemyStatusUIController;

    [Header("Absorb Icon")]
    // <변경부분> 정상 전투 중 흡수 OFF / ON 아이콘을 표시하는 Image
    [SerializeField] private Image absorbIconImage;

    [SerializeField] private Sprite absorbOffSprite;
    [SerializeField] private Sprite absorbOnSprite;

    // <변경부분> 초기 배치 단계에서만 표시할 별도의 체크 아이콘 Image
    // 별도 RectTransform을 사용하므로 Inspector에서 크기를 독립적으로 설정한다.
    [SerializeField] private Image deploymentConfirmIconImage;


    [Header("Icon Pixel Burst Effect")]
    // <변경부분> 흡수/고유스킬 아이콘 클릭 시 생성할 검은 픽셀 파티클 프리팹
    [SerializeField] private PixelBurstEffect iconPixelBurstEffectPrefab;

    // <변경부분> 생성된 파티클을 넣어둘 부모 Transform
    // 비워두면 월드에 직접 생성
    [SerializeField] private Transform iconPixelBurstEffectParent;

    // <변경부분> 흡수 아이콘 파티클 기준 위치
    [SerializeField] private RectTransform absorbIconPixelBurstAnchor;

    // <변경부분> 고유스킬 아이콘 파티클 기준 위치
    [SerializeField] private RectTransform uniqueSkillIconPixelBurstAnchor;

    [Header("Button Icon Noise Animation")]
    // <변경부분> 흡수 아이콘이 표시될 때 재생할 버튼 노이즈 애니메이터
    [SerializeField] private UIButtonNoiseAnimator absorbIconNoiseAnimator;

    // <변경부분> 고유스킬 아이콘이 표시될 때 재생할 버튼 노이즈 애니메이터
    [SerializeField] private UIButtonNoiseAnimator uniqueSkillIconNoiseAnimator;

    // <변경부분> 기물 선택으로 액션 버튼이 표시될 때 아이콘 노이즈 애니메이션을 재생할지 여부
    [SerializeField] private bool playActionIconNoiseOnShow = true;

    // <변경부분> 액션 버튼 표시 직후 노이즈 애니메이션을 한 프레임 뒤 재생하기 위한 코루틴
    private Coroutine actionButtonNoiseCoroutine;

    [Header("Last Enemy Absorb Highlight")]
    // <변경부분> 마지막 Enemy 1기 강제 흡수가 가능할 때
    // 흡수 버튼 위치에 생성해 반복 재생할 ParticleSystem 프리팹
    [SerializeField]
    private ParticleSystem lastEnemyAbsorbParticlePrefab;

    // <변경부분> 파티클 생성 위치와 부모로 사용할 Transform
    // 비워두면 Absorb Button의 RectTransform을 자동 사용한다.
    [SerializeField]
    private Transform lastEnemyAbsorbParticleParent;

    // <변경부분> 생성된 파티클 프리팹에 적용할 로컬 위치 보정
    [SerializeField]
    private Vector3 lastEnemyAbsorbParticleLocalPosition =
        Vector3.zero;

    // <변경부분> 생성된 파티클 프리팹에 적용할 로컬 스케일
    [SerializeField]
    private Vector3 lastEnemyAbsorbParticleLocalScale =
        Vector3.one;

    // <변경부분> 흡수 버튼 확대·축소에 사용할 RectTransform
    [SerializeField]
    private RectTransform absorbButtonRectTransform;

    // <변경부분> 마지막 적 흡수 안내 중 버튼이 커지는 최대 배율
    [SerializeField, Min(1f)]
    private float lastEnemyAbsorbPulseScale =
        1.08f;

    // <변경부분> 확대 → 축소 한 사이클에 걸리는 시간
    [SerializeField, Min(0.1f)]
    private float lastEnemyAbsorbPulseDuration =
        1.2f;

    // <변경부분> 현재 마지막 적 강제 흡수 안내 모드인지 확인
    private bool isLastEnemyAbsorbMode =
        false;

    // <변경부분> 흡수 버튼 확대·축소 코루틴
    private Coroutine lastEnemyAbsorbPulseCoroutine;

    // <변경부분> 현재 생성되어 재생 중인
    // 마지막 적 흡수 파티클 인스턴스
    private ParticleSystem lastEnemyAbsorbParticleInstance;

    // <변경부분> 강조 연출 종료 후 복구할
    // 흡수 버튼의 원래 스케일
    private Vector3 defaultAbsorbButtonScale =
        Vector3.one;

    [Header("Unique Skill Icon")]
    [SerializeField] private Image uniqueSkillIconImage;
    // <변경부분> 고유스킬 쿨타임 숫자 뒤에 표시할 검정 배경 이미지
    [SerializeField] private GameObject uniqueSkillCooldownImageObject;
    // <변경부분> 고유스킬 버튼 위에 표시할 쿨타임 숫자 텍스트
    [SerializeField] private TMP_Text uniqueSkillCooldownText;

    [Header("Unique Skill Database")]
    // <변경부분> 고유스킬 타입으로 아이콘 데이터를 찾기 위한 데이터베이스
    [SerializeField] private UniqueSkillDatabase uniqueSkillDatabase;

    [Header("Tooltip")]
    // <변경부분> 흡수 버튼을 꾹 눌렀을 때 표시할 Tooltip 데이터
    [SerializeField] private TooltipData absorbTooltipData;

    // <변경부분> 흡수 버튼에 붙어 있는 TooltipTrigger
    [SerializeField] private TooltipTrigger absorbTooltipTrigger;

    // <변경부분> 고유스킬 버튼에 붙어 있는 TooltipTrigger
    [SerializeField] private TooltipTrigger uniqueSkillTooltipTrigger;

    [Header("Unique Skill Failure Message")]
    // <변경부분> 고유스킬 사용 실패 이유를 표시할 텍스트
    [SerializeField] private TMP_Text uniqueSkillFailureText;

    // <변경부분> 실패 메시지 페이드 처리를 위한 CanvasGroup
    [SerializeField] private CanvasGroup uniqueSkillFailureCanvasGroup;

    // <변경부분> 실패 메시지 팝업이 표시될 때 글리치 오픈 애니메이션을 재생하는 컴포넌트
    [SerializeField] private PopupOpenAnimator uniqueSkillFailurePopupOpenAnimator;

    // <변경부분> 실패 메시지가 유지되는 시간
    [SerializeField] private float uniqueSkillFailureHoldDuration = 0.75f;

    // <변경부분> 실패 메시지가 사라지는 페이드 시간
    [SerializeField] private float uniqueSkillFailureFadeDuration = 0.25f;

    // <변경부분> Time.timeScale 영향을 받지 않고 메시지를 표시할지 여부
    [SerializeField] private bool useUnscaledTimeForFailureMessage = true;

    // <변경부분> 현재 실행 중인 실패 메시지 코루틴
    private Coroutine uniqueSkillFailureMessageCoroutine;

    // <변경부분> 전투 중 사용하는 아이템 슬롯 UI 목록
    [Header("Item Slots")]
    [SerializeField] private BattleItemSlotUI[] itemSlotUIs;

    // <변경부분> 아이템 슬롯 전체를 감싸는 바 루트 오브젝트
    // 보유 아이템이 하나도 없으면 숨기고, 1개 이상이면 표시한다.
    [SerializeField] private GameObject itemSlotBarRoot;

    [Header("Relic Slots")]
    // <변경부분> 전투 중 보유한 유물을 표시하는 유물 슬롯 UI 목록
    [SerializeField] private BattleRelicSlotUI[] relicSlotUIs;

    private void Start()
    {
        // 흡수 버튼 클릭 이벤트 연결
        if (absorbButton != null)
        {
            absorbButton.onClick.AddListener(OnClickAbsorbButton);
        }

        // 고유 스킬 버튼 클릭 이벤트 연결
        if (uniqueSkillButton != null)
        {
            uniqueSkillButton.onClick.AddListener(OnClickUniqueSkillButton);
        }

        // <변경부분> 테스트용 아이템 추가 버튼 클릭 이벤트 연결
        if (debugAddItemButton != null)
        {
            debugAddItemButton.onClick.AddListener(OnClickDebugAddItemButton);
        }

        // <변경부분> 테스트용 강제 턴 넘기기 버튼 클릭 이벤트 연결
        if (debugForceEndTurnButton != null)
        {
            debugForceEndTurnButton.onClick.AddListener(OnClickDebugForceEndTurnButton);
        }

        // <변경부분> 테스트용 유물 추가 버튼 클릭 이벤트 연결
        if (debugAddRelicButton != null)
        {
            debugAddRelicButton.onClick.AddListener(OnClickDebugAddRelicButton);
        }

        // <변경부분> 아이템 슬롯 버튼 클릭 이벤트 연결
        InitializeItemSlots();

        // <변경부분> 흡수/고유스킬 버튼 Tooltip 초기화
        InitializeActionButtonTooltips();

        // <변경부분> 흡수/고유스킬 아이콘 노이즈 애니메이터 자동 연결
        AutoBindButtonIconNoiseAnimators();

        // <변경부분> 마지막 적 강제 흡수 강조에 사용할
        // 흡수 버튼 RectTransform과 원래 스케일을 저장한다.
        if (absorbButtonRectTransform == null &&
            absorbButton != null)
        {
            absorbButtonRectTransform =
                absorbButton.GetComponent<RectTransform>();
        }

        if (absorbButtonRectTransform != null)
        {
            defaultAbsorbButtonScale =
                absorbButtonRectTransform.localScale;
        }

        SetLastEnemyAbsorbMode(
            false
        );

        // <변경부분> 게임 시작 시 배치 완료 체크 아이콘은 숨긴다.
        // 실제 배치 단계가 시작되면 SetPlayerDeploymentMode(true)에서 표시한다.
        if (deploymentConfirmIconImage != null)
        {
            deploymentConfirmIconImage.gameObject.SetActive(
                false
            );
        }

        // <변경부분> 고유스킬 실패 메시지 팝업 오픈 애니메이터 자동 연결
        AutoBindUniqueSkillFailurePopupAnimator();

        // <변경부분> 고유스킬 실패 메시지 UI 초기화
        HideUniqueSkillFailureMessageImmediately();

        // 게임 시작 시 액션 버튼 숨김
        HideActionButtons();
    }

    // <변경부분> 흡수 버튼 클릭 시
    // 초기 배치 중에는 배치 완료,
    // 정상 전투 중에는 기존 흡수 모드를 실행한다.
    private void OnClickAbsorbButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning(
                "BattleManager가 연결되지 않았습니다."
            );

            return;
        }

        // 흡수 또는 체크 아이콘 위치에서
        // 기존 픽셀 파티클 연출 재생
        PlayIconPixelBurst(
            absorbIconPixelBurstAnchor
        );

        // <변경부분> 초기 배치 단계에서는
        // 현재 클릭 이벤트와 버튼 노이즈 애니메이션 호출이 모두 끝난 뒤
        // 다음 프레임에 배치 완료 처리를 실행한다.
        //
        // 같은 클릭 프레임 안에서 Absorb Button을 비활성화하면
        // UIButtonNoiseAnimator가 비활성 오브젝트에서 코루틴을 시작하려 해
        // Coroutine couldn't be started 오류가 발생한다.
        if (battleManager.IsPlayerDeploymentPhase)
        {
            StartCoroutine(
                ConfirmPlayerDeploymentAfterClickRoutine()
            );

            return;
        }

        // <변경부분> 마지막 Enemy 1기 강제 흡수가 가능한 상태라면
        // 일반 흡수 모드 대신 Player King의 즉시 흡수 공격을 실행한다.
        if (battleManager.TryStartLastEnemyAbsorb())
        {
            return;
        }

        // 마지막 적 강제 흡수 조건이 아니라면
        // 기존 흡수 버튼처럼 흡수 모드를 ON/OFF 한다.
        battleManager.ToggleAbsorbMode();
    }

    // <변경부분> 체크 버튼 클릭과 UIButtonNoiseAnimator 호출이
    // 모두 처리된 다음 프레임에 초기 배치를 완료한다.
    private IEnumerator ConfirmPlayerDeploymentAfterClickRoutine()
    {
        // 현재 클릭 이벤트가 완전히 끝날 때까지 한 프레임 대기
        yield return null;

        // 대기 중 참조가 사라졌거나 이미 배치가 종료됐다면 중복 실행하지 않는다.
        if (battleManager == null ||
            battleManager.IsPlayerDeploymentPhase == false)
        {
            yield break;
        }

        battleManager.ConfirmPlayerDeployment();
    }

    // <변경부분> 고유 스킬 버튼 클릭 시 BattleManager의 고유 스킬 사용 호출
    private void OnClickUniqueSkillButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        // <변경부분> 고유스킬 아이콘 클릭 위치에서 검은 픽셀 파티클 재생
        PlayIconPixelBurst(uniqueSkillIconPixelBurstAnchor);

        battleManager.UseSelectedPieceSkill();
    }

    // <변경부분> 아이템 슬롯 UI를 초기화하는 함수
    private void InitializeItemSlots()
    {
        // 아이템 슬롯 배열이 없으면 종료
        if (itemSlotUIs == null)
        {
            return;
        }

        // 각 슬롯에 자신의 번호와 상위 UI를 알려줌
        for (int i = 0; i < itemSlotUIs.Length; i++)
        {
            if (itemSlotUIs[i] == null)
            {
                continue;
            }

            itemSlotUIs[i].Initialize(this, i);
        }
    }

    // <변경부분> 전투 액션 버튼에 TooltipData를 연결하는 함수
    private void InitializeActionButtonTooltips()
    {
        // 흡수 버튼은 전투 내내 같은 설명을 사용하므로 시작 시 한 번만 연결
        if (absorbTooltipTrigger != null)
        {
            absorbTooltipTrigger.SetTooltipData(absorbTooltipData);
        }

        // 고유스킬 버튼은 선택한 기물에 따라 Tooltip이 바뀌므로 초기에는 비움
        if (uniqueSkillTooltipTrigger != null)
        {
            uniqueSkillTooltipTrigger.SetTooltipData(null);
        }
    }

    // <변경부분> 흡수/고유스킬 아이콘에 붙은 UIButtonNoiseAnimator를 자동으로 찾는 함수
    private void AutoBindButtonIconNoiseAnimators()
    {
        if (absorbIconNoiseAnimator == null && absorbIconImage != null)
        {
            absorbIconNoiseAnimator = absorbIconImage.GetComponent<UIButtonNoiseAnimator>();
        }

        if (uniqueSkillIconNoiseAnimator == null && uniqueSkillIconImage != null)
        {
            uniqueSkillIconNoiseAnimator = uniqueSkillIconImage.GetComponent<UIButtonNoiseAnimator>();
        }
    }

    // <변경부분> 기물 선택으로 액션 버튼이 표시될 때 아이콘 노이즈 애니메이션을 예약하는 함수
    private void PlayActionButtonIconNoiseOnShow()
    {
        if (playActionIconNoiseOnShow == false)
        {
            return;
        }

        if (actionButtonNoiseCoroutine != null)
        {
            StopCoroutine(actionButtonNoiseCoroutine);
        }

        actionButtonNoiseCoroutine = StartCoroutine(PlayActionButtonIconNoiseOnShowRoutine());
    }

    // <변경부분> SetActive 직후 UI 갱신이 끝난 다음 프레임에 아이콘 노이즈 애니메이션 재생
    private IEnumerator PlayActionButtonIconNoiseOnShowRoutine()
    {
        yield return null;

        AutoBindButtonIconNoiseAnimators();

        if (absorbIconNoiseAnimator != null &&
            absorbButton != null &&
            absorbButton.gameObject.activeInHierarchy)
        {
            absorbIconNoiseAnimator.PlayNoise();
        }

        if (uniqueSkillIconNoiseAnimator != null &&
            uniqueSkillButton != null &&
            uniqueSkillButton.gameObject.activeInHierarchy)
        {
            uniqueSkillIconNoiseAnimator.PlayNoise();
        }

        actionButtonNoiseCoroutine = null;
    }

    // <변경부분> 외부 컨트롤러에서 특정 UI 위치에 검은 픽셀 파티클을 재생할 때 사용하는 함수
    public void PlayIconPixelBurstAt(RectTransform targetAnchor)
    {
        PlayIconPixelBurst(targetAnchor);
    }

    // <변경부분> 지정한 UI 아이콘 위치에서 검은 픽셀 파티클을 생성하고 재생
    private void PlayIconPixelBurst(RectTransform targetAnchor)
    {
        if (iconPixelBurstEffectPrefab == null)
        {
            return;
        }

        if (targetAnchor == null)
        {
            return;
        }

        PixelBurstEffect effect = iconPixelBurstEffectParent != null
            ? Instantiate(iconPixelBurstEffectPrefab, iconPixelBurstEffectParent)
            : Instantiate(iconPixelBurstEffectPrefab);

        effect.PlayAtPositionAndDestroy(targetAnchor.position);
    }

    // <변경부분> 아이템 슬롯 클릭 시 BattleManager에 아이템 사용 요청
    public void OnClickItemSlot(int slotIndex)
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.UseItemAtSlot(slotIndex);
    }

    // <변경부분> 아이템 슬롯 UI 전체를 현재 아이템 목록에 맞게 갱신하고,
    // 보유 아이템이 하나도 없으면 아이템 슬롯 바 전체를 숨긴다.
    public void RefreshItemSlots(BattleItemData[] itemSlots)
    {
        bool hasAnyItem = false;

        // <변경부분> 실제 보유 아이템이 하나라도 있는지 먼저 검사한다.
        if (itemSlots != null)
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                BattleItemData itemData =
                    itemSlots[i];

                if (itemData == null ||
                    itemData.itemType ==
                        BattleItemType.None)
                {
                    continue;
                }

                hasAnyItem = true;
                break;
            }
        }

        // <변경부분> 아이템이 0개면 바 전체를 숨기고,
        // 하나라도 있으면 다시 표시한다.
        if (itemSlotBarRoot != null)
        {
            itemSlotBarRoot.SetActive(
                hasAnyItem
            );
        }

        if (itemSlotUIs == null)
        {
            return;
        }

        // 개별 슬롯은 기존 방식대로
        // 아이콘과 클릭 가능 여부만 갱신한다.
        for (int i = 0; i < itemSlotUIs.Length; i++)
        {
            if (itemSlotUIs[i] == null)
            {
                continue;
            }

            BattleItemData itemData = null;

            if (itemSlots != null &&
                i < itemSlots.Length)
            {
                itemData =
                    itemSlots[i];
            }

            itemSlotUIs[i].Refresh(
                itemData
            );
        }
    }

    // <변경부분> 현재 유물 슬롯 정보를 UI에 반영하는 함수
    public void RefreshRelicSlots(BattleRelicData[] relicSlots)
    {
        // 유물 슬롯 UI 배열이 없으면 갱신할 대상이 없음
        if (relicSlotUIs == null)
        {
            return;
        }

        // 슬롯 UI 개수만큼 유물 아이콘 표시 상태를 갱신
        for (int i = 0; i < relicSlotUIs.Length; i++)
        {
            if (relicSlotUIs[i] == null)
            {
                continue;
            }

            BattleRelicData relicData = null;

            // 실제 유물 배열에 해당 슬롯 데이터가 있으면 가져옴
            if (relicSlots != null && i < relicSlots.Length)
            {
                relicData = relicSlots[i];
            }

            relicSlotUIs[i].Refresh(relicData);
        }
    }


    // 선택된 기물 상태에 따라 버튼 표시 갱신
    public void RefreshSelectedPieceButtons(Piece selectedPiece)
    {
        // <변경부분> 선택한 플레이어 기물 정보를 왼쪽 하단 스테이터스 UI에 표시
        if (playerStatusUIController != null)
        {
            playerStatusUIController.Refresh(selectedPiece);
        }

        // 선택된 기물이 없으면 버튼 숨김
        if (selectedPiece == null)
        {
            HideActionButtons();
            return;
        }

        // 플레이어 기물을 선택하면 흡수 버튼 표시
        SetAbsorbButtonVisible(true);

        // <변경부분> 기물을 새로 선택할 때 흡수 아이콘은 기본 OFF 상태로 표시
        SetAbsorbModeIcon(false);

        bool hasUniqueSkill = selectedPiece.UniqueSkill != UniqueSkillType.None;
        SetUniqueSkillButtonVisible(hasUniqueSkill);

        // <변경부분> 고유스킬 버튼이 켜지는 순간 Prefab 기본 상태로 켜진 Cooldown UI를 먼저 초기화
        HideUniqueSkillCooldownUI();

        // <변경부분> 고유 스킬이 있으면 해당 스킬 아이콘으로 변경
        if (hasUniqueSkill)
        {
            SetUniqueSkillIcon(selectedPiece.UniqueSkill);
        }

        // <변경부분> 선택된 기물의 고유스킬 쿨타임 숫자 갱신
        // 실제 쿨타임이 남아 있을 때만 다시 Cooldown UI가 켜짐
        RefreshUniqueSkillCooldownText(selectedPiece);

        // <변경부분> 기물 선택으로 액션 버튼이 표시된 뒤 흡수/고유스킬 아이콘 노이즈 애니메이션 재생
        PlayActionButtonIconNoiseOnShow();
    }

    // <변경부분> 상대 기물 정보를 오른쪽 상단 스테이터스 UI에 표시하는 함수
    public void RefreshEnemyStatus(Piece enemyPiece)
    {
        // <변경부분> 상대 스테이터스 갱신 호출 확인
        Debug.Log("상대 스테이터스 갱신 호출: " + enemyPiece.PieceType);

        if (enemyStatusUIController != null)
        {
            enemyStatusUIController.Refresh(enemyPiece);
        }
        else
        {
            Debug.LogWarning("Enemy Status UI Controller가 연결되지 않았습니다.");
        }
    }

    // <변경부분> 상대 기물 스테이터스 UI를 숨기는 함수
    public void ClearEnemyStatus()
    {
        if (enemyStatusUIController != null)
        {
            enemyStatusUIController.Clear();
        }
    }

    // <변경부분> 플레이어 초기 배치 단계의 액션 버튼 상태를 적용한다.
    //
    // 배치 중:
    // 기존 흡수 아이콘 숨김
    // 별도 체크 아이콘 표시
    // 고유스킬 버튼 숨김
    //
    // 배치 종료:
    // 체크 아이콘 숨김
    // 기존 흡수 아이콘 복구
    public void SetPlayerDeploymentMode(
        bool isDeploymentMode)
    {
        if (isDeploymentMode)
        {
            // 배치 완료 체크 버튼으로 사용할
            // 기존 Absorb Button 루트는 계속 표시한다.
            SetAbsorbButtonVisible(
                true
            );

            SetUniqueSkillButtonVisible(
                false
            );

            // <변경부분> 기존 흡수 아이콘은 숨긴다.
            if (absorbIconImage != null)
            {
                absorbIconImage.gameObject.SetActive(
                    false
                );
            }

            // <변경부분> 별도의 체크 아이콘 Image를 표시한다.
            // 별도 RectTransform이므로 Inspector에서 원하는 크기로 설정할 수 있다.
            if (deploymentConfirmIconImage != null)
            {
                deploymentConfirmIconImage.gameObject.SetActive(
                    true
                );

                deploymentConfirmIconImage.enabled =
                    true;
            }

            // 배치 중에는 체크 버튼이므로
            // 기존 흡수 Tooltip을 표시하지 않는다.
            if (absorbTooltipTrigger != null)
            {
                absorbTooltipTrigger.SetTooltipData(
                    null
                );
            }

            return;
        }

        // <변경부분> 배치 종료 후 체크 아이콘을 숨긴다.
        if (deploymentConfirmIconImage != null)
        {
            deploymentConfirmIconImage.gameObject.SetActive(
                false
            );
        }

        // <변경부분> 정상 전투용 흡수 아이콘을 다시 표시한다.
        if (absorbIconImage != null)
        {
            absorbIconImage.gameObject.SetActive(
                true
            );

            absorbIconImage.enabled =
                true;
        }

        // 기존 흡수 OFF 아이콘 복구
        SetAbsorbModeIcon(
            false
        );

        // 기존 흡수 Tooltip 복구
        if (absorbTooltipTrigger != null)
        {
            absorbTooltipTrigger.SetTooltipData(
                absorbTooltipData
            );
        }
    }

    // <변경부분> 마지막 Enemy 1기 강제 흡수 가능 상태를 UI에 적용한다.
    //
    // 활성화:
    // 흡수 버튼을 선택 여부와 관계없이 표시하고,
    // ParticleSystem 프리팹과 확대·축소 코루틴을 시작한다.
    //
    // 비활성화:
    // 코루틴과 파티클을 중단하고
    // 버튼 스케일을 원래 상태로 복구한다.
    public void SetLastEnemyAbsorbMode(
        bool isActive)
    {
        if (isLastEnemyAbsorbMode ==
            isActive)
        {
            // 시작 상태처럼 이미 false인 경우에도
            // 남아 있을 수 있는 파티클과
            // 버튼 스케일을 확실히 정리한다.
            if (isActive == false)
            {
                StopLastEnemyAbsorbHighlight();
            }

            return;
        }

        isLastEnemyAbsorbMode =
            isActive;

        if (isActive)
        {
            SetAbsorbButtonVisible(
                true
            );

            SetAbsorbModeIcon(
                false
            );

            // <변경부분> 마지막 Enemy 1기 상태가 처음 활성화되는 순간
            // 기존 SkillFailurePopup을 재사용해 마무리 흡수 사용법을 안내한다.
            //
            // SetLastEnemyAbsorbMode가 false → true로 바뀔 때만 이 분기로 들어오므로
            // 보드 상태를 반복 갱신해도 안내 문구가 계속 중복 출력되지 않는다.
            ShowUniqueSkillFailureMessage(
                "흡수 버튼을 눌러 \n마무리 흡수를 사용하세요."
            );

            // <변경부분> 파티클 프리팹 생성 및 재생
            PlayLastEnemyAbsorbParticle();

            if (lastEnemyAbsorbPulseCoroutine !=
                null)
            {
                StopCoroutine(
                    lastEnemyAbsorbPulseCoroutine
                );
            }

            lastEnemyAbsorbPulseCoroutine =
                StartCoroutine(
                    PlayLastEnemyAbsorbPulseRoutine()
                );

            return;
        }

        StopLastEnemyAbsorbHighlight();
    }

    // <변경부분> 마지막 적 강제 흡수 안내용
    // ParticleSystem 프리팹을 흡수 버튼 위치에 생성하고 재생한다.
    private void PlayLastEnemyAbsorbParticle()
    {
        // 기존 생성 인스턴스가 남아 있다면 먼저 정리한다.
        StopLastEnemyAbsorbParticle();

        if (lastEnemyAbsorbParticlePrefab ==
            null)
        {
            return;
        }

        Transform particleParent =
            lastEnemyAbsorbParticleParent;

        // 별도 부모를 지정하지 않았다면
        // 흡수 버튼 RectTransform을 부모로 사용한다.
        if (particleParent == null &&
            absorbButtonRectTransform != null)
        {
            particleParent =
                absorbButtonRectTransform;
        }

        if (particleParent == null &&
            absorbButton != null)
        {
            particleParent =
                absorbButton.transform;
        }

        if (particleParent != null)
        {
            // <변경부분> 흡수 버튼 자식으로 파티클 생성
            lastEnemyAbsorbParticleInstance =
                Instantiate(
                    lastEnemyAbsorbParticlePrefab,
                    particleParent
                );

            Transform particleTransform =
                lastEnemyAbsorbParticleInstance
                    .transform;

            particleTransform.localPosition =
                lastEnemyAbsorbParticleLocalPosition;

            particleTransform.localRotation =
                Quaternion.identity;

            particleTransform.localScale =
                lastEnemyAbsorbParticleLocalScale;
        }
        else
        {
            // 부모를 찾지 못한 경우 월드에 생성
            lastEnemyAbsorbParticleInstance =
                Instantiate(
                    lastEnemyAbsorbParticlePrefab
                );

            if (absorbButtonRectTransform != null)
            {
                lastEnemyAbsorbParticleInstance
                    .transform.position =
                    absorbButtonRectTransform.position;
            }
        }

        // 루트와 자식 ParticleSystem을 함께 재생한다.
        lastEnemyAbsorbParticleInstance.Play(
            true
        );
    }

    // <변경부분> 생성된 마지막 적 흡수 파티클을
    // 즉시 정지하고 인스턴스를 제거한다.
    private void StopLastEnemyAbsorbParticle()
    {
        if (lastEnemyAbsorbParticleInstance ==
            null)
        {
            return;
        }

        lastEnemyAbsorbParticleInstance.Stop(
            true,
            ParticleSystemStopBehavior
                .StopEmittingAndClear
        );

        Destroy(
            lastEnemyAbsorbParticleInstance.gameObject
        );

        lastEnemyAbsorbParticleInstance =
            null;
    }

    // <변경부분> 마지막 적 강제 흡수 버튼을
    // 천천히 확대·축소하는 연출을 반복한다.
    private IEnumerator PlayLastEnemyAbsorbPulseRoutine()
    {
        if (absorbButtonRectTransform == null &&
            absorbButton != null)
        {
            absorbButtonRectTransform =
                absorbButton.GetComponent<RectTransform>();
        }

        if (absorbButtonRectTransform != null)
        {
            defaultAbsorbButtonScale =
                absorbButtonRectTransform.localScale;
        }

        float safeDuration =
            Mathf.Max(
                0.1f,
                lastEnemyAbsorbPulseDuration
            );

        while (isLastEnemyAbsorbMode)
        {
            float elapsedTime =
                0f;

            while (elapsedTime < safeDuration &&
                   isLastEnemyAbsorbMode)
            {
                elapsedTime +=
                    Time.unscaledDeltaTime;

                float normalizedTime =
                    Mathf.Clamp01(
                        elapsedTime /
                        safeDuration
                    );

                // 0 → 1 → 0으로 천천히 반복되는 값
                float pulseValue =
                    (
                        Mathf.Sin(
                            normalizedTime *
                            Mathf.PI *
                            2f -
                            Mathf.PI *
                            0.5f
                        ) +
                        1f
                    ) *
                    0.5f;

                float scaleMultiplier =
                    Mathf.Lerp(
                        1f,
                        Mathf.Max(
                            1f,
                            lastEnemyAbsorbPulseScale
                        ),
                        pulseValue
                    );

                if (absorbButtonRectTransform != null)
                {
                    absorbButtonRectTransform.localScale =
                        defaultAbsorbButtonScale *
                        scaleMultiplier;
                }

                yield return null;
            }
        }

        lastEnemyAbsorbPulseCoroutine =
            null;
    }

    // <변경부분> 마지막 적 흡수 강조 연출을 즉시 중단하고
    // 흡수 버튼과 파티클을 기본 상태로 복구한다.
    private void StopLastEnemyAbsorbHighlight()
    {
        if (lastEnemyAbsorbPulseCoroutine != null)
        {
            StopCoroutine(
                lastEnemyAbsorbPulseCoroutine
            );

            lastEnemyAbsorbPulseCoroutine =
                null;
        }

        if (absorbButtonRectTransform != null)
        {
            absorbButtonRectTransform.localScale =
                defaultAbsorbButtonScale;
        }

        StopLastEnemyAbsorbParticle();
    }

    // <변경부분> 흡수 모드 상태에 따라 버튼 위 아이콘만 변경
    public void SetAbsorbModeIcon(bool isAbsorbMode)
    {
        if (absorbIconImage == null)
        {
            return;
        }

        absorbIconImage.sprite = isAbsorbMode ? absorbOnSprite : absorbOffSprite;
    }

    // <변경부분> 고유 스킬 종류에 맞는 아이콘을 표시하는 함수
    private void SetUniqueSkillIcon(UniqueSkillType skillType)
    {
        // 고유 스킬 아이콘 이미지가 없으면 종료
        if (uniqueSkillIconImage == null)
        {
            return;
        }

        // 데이터베이스가 없으면 아이콘 숨김
        if (uniqueSkillDatabase == null)
        {
            uniqueSkillIconImage.sprite = null;
            uniqueSkillIconImage.enabled = false;

            Debug.LogWarning("BattleUIController에 UniqueSkillDatabase가 연결되지 않았습니다.");
            return;
        }

        // 고유스킬 데이터 검색
        UniqueSkillData skillData = uniqueSkillDatabase.GetData(skillType);

        // 데이터가 없거나 아이콘이 없으면 아이콘 숨김
        if (skillData == null || skillData.iconSprite == null)
        {
            uniqueSkillIconImage.sprite = null;
            uniqueSkillIconImage.enabled = false;

            // <변경부분> 표시할 고유스킬 데이터가 없으면 Tooltip도 비움
            if (uniqueSkillTooltipTrigger != null)
            {
                uniqueSkillTooltipTrigger.SetTooltipViewData(null);
            }

            Debug.LogWarning($"고유 스킬 아이콘을 찾지 못했습니다: {skillType}");
            return;
        }

        // 데이터에 등록된 아이콘 적용
        uniqueSkillIconImage.gameObject.SetActive(true);
        uniqueSkillIconImage.sprite = skillData.iconSprite;
        uniqueSkillIconImage.enabled = true;

        // <변경부분> 이전 UI 애니메이션이나 비활성화 상태 때문에 아이콘이 안 보이지 않도록 알파 복구
        Color iconColor = uniqueSkillIconImage.color;
        iconColor.a = 1f;
        uniqueSkillIconImage.color = iconColor;

        // <변경부분> 이전 노이즈 애니메이션 문제로 스케일이 0이면 아이콘이 안 보이므로 기본 스케일로 복구
        RectTransform iconRectTransform = uniqueSkillIconImage.rectTransform;
        if (iconRectTransform != null)
        {
            bool isScaleBroken =
                Mathf.Approximately(iconRectTransform.localScale.x, 0f) ||
                Mathf.Approximately(iconRectTransform.localScale.y, 0f);

            if (isScaleBroken)
            {
                iconRectTransform.localScale = Vector3.one;
            }
        }

        // <변경부분> 현재 선택된 고유스킬 설명 Tooltip 연결
        if (uniqueSkillTooltipTrigger != null)
        {
            // <변경부분> 고유스킬 데이터의 기존 이름/설명/아이콘으로 Tooltip을 자동 구성
            uniqueSkillTooltipTrigger.SetTooltipViewData(TooltipViewData.FromUniqueSkillData(skillData));
        }
    }

    // <변경부분> 선택된 기물의 고유스킬 쿨타임 숫자와 배경 이미지를 갱신하는 함수
    private void RefreshUniqueSkillCooldownText(Piece selectedPiece)
    {
        // 선택 기물이 없거나 고유스킬이 없으면 쿨타임 UI 숨김
        if (selectedPiece == null || selectedPiece.UniqueSkill == UniqueSkillType.None)
        {
            HideUniqueSkillCooldownUI();
            return;
        }

        // 선택 기물의 현재 고유스킬 쿨타임 가져오기
        int cooldown = selectedPiece.GetUniqueSkillCooldown();

        // 쿨타임이 없으면 쿨타임 UI 숨김
        if (cooldown <= 0)
        {
            HideUniqueSkillCooldownUI();
            return;
        }

        // <변경부분> 쿨타임이 남아 있으면 검정 배경 Image 활성화
        if (uniqueSkillCooldownImageObject != null)
        {
            uniqueSkillCooldownImageObject.SetActive(true);
        }

        // <변경부분> 쿨타임 숫자 표시
        if (uniqueSkillCooldownText != null)
        {
            uniqueSkillCooldownText.text = cooldown.ToString();
            uniqueSkillCooldownText.gameObject.SetActive(true);
        }
    }

    // <변경부분> 고유스킬 쿨타임 배경 이미지와 숫자를 숨기는 함수
    private void HideUniqueSkillCooldownUI()
    {
        // 쿨타임 배경 Image 오브젝트 숨김
        if (uniqueSkillCooldownImageObject != null)
        {
            uniqueSkillCooldownImageObject.SetActive(false);
        }

        // 쿨타임 숫자 텍스트 숨김
        if (uniqueSkillCooldownText != null)
        {
            uniqueSkillCooldownText.text = "";
            uniqueSkillCooldownText.gameObject.SetActive(false);
        }
    }



    // <변경부분> 흡수 버튼 루트 오브젝트의 표시 여부를 변경한다.
    //
    // 초기 배치 체크 버튼, 일반 흡수 버튼,
    // 마지막 적 마무리 흡수 버튼이 같은 루트를 사용하므로
    // 모든 표시/숨김 처리를 이 함수에서 공통으로 담당한다.
    public void SetAbsorbButtonVisible(
        bool isVisible)
    {
        if (absorbButton == null)
        {
            return;
        }

        absorbButton.gameObject.SetActive(
            isVisible
        );
    }

    // <변경부분> 고유 스킬 버튼 표시/숨김
    public void SetUniqueSkillButtonVisible(bool isVisible)
    {
        if (uniqueSkillButton == null)
        {
            return;
        }

        uniqueSkillButton.gameObject.SetActive(isVisible);

        // <변경부분> 고유스킬 버튼을 숨길 때 Cooldown UI도 반드시 같이 숨김
        if (isVisible == false)
        {
            HideUniqueSkillCooldownUI();
        }
    }

    // <변경부분> 액션 버튼 전체 숨김
    public void HideActionButtons()
    {
        // <변경부분> 초기 배치 중에는 우측 체크 버튼을 유지하고
        // 고유스킬 버튼과 스테이터스 UI만 숨긴다.
        if (battleManager != null &&
            battleManager.IsPlayerDeploymentPhase)
        {
            SetPlayerDeploymentMode(
                true
            );

            if (playerStatusUIController != null)
            {
                playerStatusUIController.Clear();
            }

            if (enemyStatusUIController != null)
            {
                enemyStatusUIController.Clear();
            }

            return;
        }

        // <변경부분> 마지막 Enemy 1기 강제 흡수 가능 상태에서는
        // 선택 기물이 없어도 흡수 버튼과 강조 연출을 유지한다.
        if (isLastEnemyAbsorbMode)
        {
            SetAbsorbButtonVisible(
                true
            );

            SetUniqueSkillButtonVisible(
                false
            );

            if (playerStatusUIController != null)
            {
                playerStatusUIController.Clear();
            }

            if (enemyStatusUIController != null)
            {
                enemyStatusUIController.Clear();
            }

            return;
        }

        SetAbsorbButtonVisible(false);
        SetUniqueSkillButtonVisible(false);

        // 흡수 버튼 아이콘을 기본 OFF 상태로 변경
        SetAbsorbModeIcon(false);

        if (uniqueSkillIconImage != null)
        {
            uniqueSkillIconImage.sprite = null;
            uniqueSkillIconImage.enabled = false;
        }

        // <변경부분> 선택 기물이 사라지면 고유스킬 Tooltip도 비움
        if (uniqueSkillTooltipTrigger != null)
        {
            uniqueSkillTooltipTrigger.SetTooltipViewData(null);
        }

        // <변경부분> 고유스킬 쿨타임 배경 이미지와 숫자 숨김
        HideUniqueSkillCooldownUI();

        // <변경부분> 플레이어 스테이터스 UI 숨김
        if (playerStatusUIController != null)
        {
            playerStatusUIController.Clear();
        }

        // <변경부분> 상대 스테이터스 UI 숨김
        if (enemyStatusUIController != null)
        {
            enemyStatusUIController.Clear();
        }
    }

    // <변경부분> 고유스킬 실패 메시지 팝업에 연결된 PopupOpenAnimator를 자동으로 찾는 함수
    private void AutoBindUniqueSkillFailurePopupAnimator()
    {
        if (uniqueSkillFailurePopupOpenAnimator != null)
        {
            return;
        }

        if (uniqueSkillFailureText == null)
        {
            return;
        }

        // <변경부분> Text 자기 자신 또는 부모 오브젝트에서 PopupOpenAnimator 탐색
        uniqueSkillFailurePopupOpenAnimator =
            uniqueSkillFailureText.GetComponentInParent<PopupOpenAnimator>(true);
    }

    // <변경부분> 고유스킬 사용 실패 메시지를 화면에 표시하는 함수
    public void ShowUniqueSkillFailureMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (uniqueSkillFailureText == null)
        {
            Debug.LogWarning($"고유스킬 실패 메시지 Text가 연결되지 않았습니다: {message}");
            return;
        }

        if (uniqueSkillFailureCanvasGroup == null)
        {
            uniqueSkillFailureCanvasGroup = uniqueSkillFailureText.GetComponent<CanvasGroup>();

            if (uniqueSkillFailureCanvasGroup == null)
            {
                uniqueSkillFailureCanvasGroup = uniqueSkillFailureText.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (uniqueSkillFailureMessageCoroutine != null)
        {
            StopCoroutine(uniqueSkillFailureMessageCoroutine);
            uniqueSkillFailureMessageCoroutine = null;
        }

        uniqueSkillFailureMessageCoroutine = StartCoroutine(ShowUniqueSkillFailureMessageRoutine(message));
    }

    // <변경부분> 고유스킬 실패 메시지를 일정 시간 표시한 뒤 페이드 아웃하는 코루틴
    private IEnumerator ShowUniqueSkillFailureMessageRoutine(string message)
    {
        uniqueSkillFailureText.text = message;
        uniqueSkillFailureText.gameObject.SetActive(true);

        uniqueSkillFailureCanvasGroup.alpha = 1f;
        uniqueSkillFailureCanvasGroup.interactable = false;
        uniqueSkillFailureCanvasGroup.blocksRaycasts = false;

        // <변경부분> 실패 메시지가 다시 표시될 때마다 글리치 오픈 애니메이션 재생
        AutoBindUniqueSkillFailurePopupAnimator();

        if (uniqueSkillFailurePopupOpenAnimator != null)
        {
            uniqueSkillFailurePopupOpenAnimator.PlayOpen();
        }

        float holdElapsed = 0f;

        while (holdElapsed < uniqueSkillFailureHoldDuration)
        {
            holdElapsed += useUnscaledTimeForFailureMessage
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            yield return null;
        }

        float fadeElapsed = 0f;
        float fadeDuration = Mathf.Max(0.001f, uniqueSkillFailureFadeDuration);

        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += useUnscaledTimeForFailureMessage
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
            uniqueSkillFailureCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        HideUniqueSkillFailureMessageImmediately();

        uniqueSkillFailureMessageCoroutine = null;
    }

    // <변경부분> 고유스킬 실패 메시지를 즉시 숨기는 함수
    private void HideUniqueSkillFailureMessageImmediately()
    {
        if (uniqueSkillFailureMessageCoroutine != null)
        {
            StopCoroutine(uniqueSkillFailureMessageCoroutine);
            uniqueSkillFailureMessageCoroutine = null;
        }

        if (uniqueSkillFailureText != null)
        {
            uniqueSkillFailureText.text = "";
            uniqueSkillFailureText.gameObject.SetActive(false);
        }

        if (uniqueSkillFailureCanvasGroup != null)
        {
            uniqueSkillFailureCanvasGroup.alpha = 0f;
            uniqueSkillFailureCanvasGroup.interactable = false;
            uniqueSkillFailureCanvasGroup.blocksRaycasts = false;
        }
    }

    // <변경부분> 테스트용 아이템 추가 버튼 클릭 시 BattleManager에 아이템 추가 요청
    private void OnClickDebugAddItemButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.AddTestItemForDebug();
    }

    // <변경부분> 테스트용 강제 턴 넘기기 버튼 클릭 시 BattleManager에 턴 종료 요청
    private void OnClickDebugForceEndTurnButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.DebugForceEndTurn();
    }

    // <변경부분> 테스트용 유물 추가 버튼 클릭 시 BattleManager에 유물 추가 요청
    private void OnClickDebugAddRelicButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.AddTestRelicForDebug();
    }
}