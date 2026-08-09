using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 상대 기물의 타입 아이콘 위치에 표시되는
// 필드 전용 흡수 버튼을 관리한다.
//
// 기존 하단 흡수 버튼과 동일하게:
// OFF / ON 아이콘 토글
// TooltipTrigger
// UIButtonNoiseAnimator
// PixelBurstEffect
// 구조를 재사용한다.
public class FieldAbsorbButton : MonoBehaviour
{
    [Header("Button")]
    // 실제 클릭 입력을 받는 Button
    [SerializeField]
    private Button absorbButton;

    // 흡수 OFF / ON 스프라이트를 표시하는 Image
    [SerializeField]
    private Image absorbIconImage;

    [Header("Tooltip")]
    // 기존 하단 흡수 버튼과 동일한 TooltipData를 받을 트리거
    //
    // 실제 Button 오브젝트에 TooltipTrigger를 추가하고
    // 이 필드에 연결한다.
    [SerializeField]
    private TooltipTrigger absorbTooltipTrigger;

    [Header("Noise Animation")]
    // 버튼 등장 및 클릭 시 기존 노이즈 애니메이션을 재생한다.
    //
    // 기존 흡수 아이콘과 동일하게
    // AbsorbIcon Image에 붙은 UIButtonNoiseAnimator를 연결한다.
    [SerializeField]
    private UIButtonNoiseAnimator absorbIconNoiseAnimator;

    [Header("Pixel Burst")]
    // 버튼 클릭 파티클의 기준 위치
    [SerializeField]
    private RectTransform pixelBurstAnchor;

    // 필드 버튼용 파티클 부모
    //
    // World Space Canvas를 사용한다면
    // 해당 Canvas 또는 이펙트 전용 자식을 연결한다.
    //
    // 비워두면 BattleUIController의 기존 파티클 부모를 사용한다.
    [SerializeField]
    private Transform pixelBurstEffectParent;

    // 기존 흡수 UI 데이터를 제공하는 컨트롤러
    private BattleUIController battleUIController;

    // 흡수 모드가 변경됐을 때 BattleManager에 전달할 콜백
    private Action<bool> absorbModeChangedAction;

    // BattleUIController의 기존 흡수 OFF / ON 스프라이트
    private Sprite absorbOffSprite;
    private Sprite absorbOnSprite;

    // 현재 필드 흡수 버튼의 ON / OFF 상태
    private bool isAbsorbMode;

    // 버튼 표시 직후 오픈 노이즈를 실행하는 코루틴
    private Coroutine openNoiseCoroutine;

    // BattleManager와 Piece가 Show()를 명시적으로 호출했을 때만
    // 필드 흡수 버튼이 활성화되도록 관리한다.
    //
    // TypeIconRoot 부모가 켜졌다는 이유만으로
    // 흡수 버튼까지 함께 표시되는 문제를 차단한다.
    private bool isShowRequested =
        false;

    public bool IsVisible
    {
        get
        {
            // GameObject의 Active 상태뿐 아니라
            // 실제 Show 요청이 들어온 상태인지 함께 확인한다.
            return
                isShowRequested &&
                gameObject.activeInHierarchy;
        }
    }

    public bool IsAbsorbMode
    {
        get
        {
            return
                isAbsorbMode;
        }
    }

    private void Awake()
    {
        AutoBindReferences();

        // 필드 흡수 버튼의 초기 숨김 상태는
        // Piece.Awake()에서 fieldAbsorbButton.Hide()로 처리한다.
        //
        // 비활성 상태의 버튼을 처음 Show()할 때는
        // isShowRequested를 true로 설정한 뒤 SetActive(true)를 호출하며,
        // 그 활성화 과정에서 Awake()가 처음 실행될 수 있다.
        //
        // 따라서 여기서 isShowRequested를 false로 덮어쓰거나
        // 자기 GameObject를 다시 비활성화하면
        // 첫 번째 표시 요청이 취소되므로 상태를 변경하지 않는다.
    }

    private void OnEnable()
    {
        // TypeIconRoot가 활성화되면서
        // 자식 흡수 버튼이 의도치 않게 함께 켜진 경우
        // 즉시 다시 비활성화한다.
        if (isShowRequested == false)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        AutoBindReferences();

        if (absorbButton != null)
        {
            absorbButton.onClick.RemoveListener(
                HandleButtonClicked
            );

            absorbButton.onClick.AddListener(
                HandleButtonClicked
            );
        }
    }

    private void OnDisable()
    {
        if (absorbButton != null)
        {
            absorbButton.onClick.RemoveListener(
                HandleButtonClicked
            );
        }

        StopOpenNoiseCoroutine();

        absorbModeChangedAction =
            null;

        isAbsorbMode =
            false;

        // 버튼이 어떤 경로로 비활성화되든
        // 다음 TypeIconRoot 활성화 때 자동으로 다시 켜지지 않도록
        // 표시 요청 상태도 함께 초기화한다.
        isShowRequested =
            false;
    }

    // BattleUIController가 관리 중인
    // 기존 흡수 TooltipData, OFF / ON 스프라이트와
    // 픽셀 파티클 실행 구조를 전달받는다.
    public void Initialize(
        BattleUIController ownerUIController,
        TooltipData tooltipData,
        Sprite offSprite,
        Sprite onSprite)
    {
        AutoBindReferences();

        battleUIController =
            ownerUIController;

        absorbOffSprite =
            offSprite;

        absorbOnSprite =
            onSprite;

        // 기존 하단 흡수 버튼과 동일한 TooltipData를 사용한다.
        if (absorbTooltipTrigger != null)
        {
            absorbTooltipTrigger.SetTooltipData(
                tooltipData
            );
        }

        SetAbsorbMode(
            false
        );
    }

    // 공격 가능한 상대 기물을 처음 선택했을 때
    // OFF 상태로 버튼을 표시한다.
    public void Show(
    Action<bool> modeChangedAction,
    BattleUIController ownerUIController)
    {
        AutoBindReferences();

        // BattleUIController가 기존 흡수 버튼 설정을 전달한다.
        if (ownerUIController != null)
        {
            ownerUIController
                .ConfigureFieldAbsorbButton(
                    this
                );
        }

        absorbModeChangedAction =
            modeChangedAction;

        SetAbsorbMode(
            false
        );

        // OnEnable이 버튼 표시를 허용할 수 있도록
        // GameObject를 켜기 전에 명시적인 표시 요청을 저장한다.
        isShowRequested =
            true;

        gameObject.SetActive(
            true
        );

        if (absorbButton != null)
        {
            absorbButton.interactable =
                true;
        }

        // 기존 액션 버튼처럼
        // 활성화가 완료된 다음 프레임에 오픈 노이즈를 재생한다.
        StartOpenNoiseAnimation();
    }

    // 선택 취소, 다른 대상 선택, 턴 종료 시
    // 버튼과 흡수 ON 상태를 모두 초기화한다.
    public void Hide()
    {
        StopOpenNoiseCoroutine();

        absorbModeChangedAction =
            null;

        SetAbsorbMode(
            false
        );

        if (absorbButton != null)
        {
            absorbButton.interactable =
                false;
        }

        // 이후 TypeIconRoot가 다시 활성화되더라도
        // 흡수 버튼이 자동으로 함께 켜지지 않도록 먼저 초기화한다.
        isShowRequested =
            false;

        gameObject.SetActive(
            false
        );
    }

    // 외부 초기화가 필요할 때
    // 필드 버튼 아이콘 상태를 직접 적용한다.
    public void SetAbsorbMode(
        bool isActive)
    {
        isAbsorbMode =
            isActive;

        if (absorbIconImage == null)
        {
            return;
        }

        Sprite targetSprite =
            isAbsorbMode
                ? absorbOnSprite
                : absorbOffSprite;

        if (targetSprite != null)
        {
            absorbIconImage.sprite =
                targetSprite;
        }

        absorbIconImage.enabled =
            true;

        Color iconColor =
            absorbIconImage.color;

        iconColor.a =
            1f;

        absorbIconImage.color =
            iconColor;
    }

    // 필드 흡수 버튼 클릭 처리
    //
    // 클릭 즉시 공격하지 않고
    // OFF / ON 상태만 반전한다.
    private void HandleButtonClicked()
    {
        // 기존 흡수 버튼과 같은 픽셀 파티클을 재생한다.
        if (battleUIController != null)
        {
            battleUIController
                .PlayFieldAbsorbPixelBurstAt(
                    pixelBurstAnchor,
                    pixelBurstEffectParent
                );
        }

        // 첫 클릭은 OFF → ON,
        // 다시 클릭하면 ON → OFF로 변경한다.
        SetAbsorbMode(
            isAbsorbMode == false
        );

        // BattleManager에는 현재 모드 상태만 전달한다.
        //
        // 실제 흡수 공격은 이후 빨간 타일을 다시 클릭했을 때
        // 기존 ExecutePieceActionRoutine에서 실행한다.
        absorbModeChangedAction?.Invoke(
            isAbsorbMode
        );

        // UIButtonNoiseAnimator는 자기 OnEnable에서
        // Button.onClick에 PlayNoise를 이미 등록한다.
        //
        // 여기서 다시 PlayNoise를 직접 호출하지 않아
        // 클릭 노이즈가 두 번 실행되지 않게 한다.
    }

    // 버튼이 표시될 때 기존 액션 아이콘과 동일하게
    // 한 프레임 뒤 노이즈 애니메이션을 실행한다.
    private void StartOpenNoiseAnimation()
    {
        StopOpenNoiseCoroutine();

        // 부모 또는 자기 오브젝트가 비활성 상태라면
        // 코루틴 오류를 방지하기 위해 오픈 노이즈만 생략한다.
        //
        // 정상 프리팹에서는 FieldAbsorbCanvas가 항상 활성화되어 있으므로
        // 첫 표시부터 아래 코루틴이 정상적으로 실행된다.
        if (gameObject.activeInHierarchy == false ||
            isActiveAndEnabled == false)
        {
            return;
        }

        openNoiseCoroutine =
            StartCoroutine(
                PlayOpenNoiseRoutine()
            );
    }

    private IEnumerator PlayOpenNoiseRoutine()
    {
        yield return null;

        if (gameObject.activeInHierarchy &&
            absorbIconNoiseAnimator != null)
        {
            absorbIconNoiseAnimator.PlayNoise();
        }

        openNoiseCoroutine =
            null;
    }

    private void StopOpenNoiseCoroutine()
    {
        if (openNoiseCoroutine ==
            null)
        {
            return;
        }

        StopCoroutine(
            openNoiseCoroutine
        );

        openNoiseCoroutine =
            null;
    }

    // Inspector 연결이 빠졌을 때
    // 현재 필드 버튼 내부에서 필요한 참조를 찾는다.
    private void AutoBindReferences()
    {
        if (absorbButton == null)
        {
            absorbButton =
                GetComponent<Button>();

            if (absorbButton == null)
            {
                absorbButton =
                    GetComponentInChildren<Button>(
                        true
                    );
            }
        }

        if (absorbIconImage == null &&
            absorbButton != null)
        {
            absorbIconImage =
                absorbButton.GetComponentInChildren<Image>(
                    true
                );
        }

        if (absorbTooltipTrigger == null &&
            absorbButton != null)
        {
            absorbTooltipTrigger =
                absorbButton.GetComponent<TooltipTrigger>();

            if (absorbTooltipTrigger == null)
            {
                absorbTooltipTrigger =
                    absorbButton.GetComponentInChildren<TooltipTrigger>(
                        true
                    );
            }
        }

        if (absorbIconNoiseAnimator == null &&
            absorbIconImage != null)
        {
            absorbIconNoiseAnimator =
                absorbIconImage.GetComponent<UIButtonNoiseAnimator>();
        }

        if (pixelBurstAnchor == null &&
            absorbIconImage != null)
        {
            pixelBurstAnchor =
                absorbIconImage.rectTransform;
        }
    }
}