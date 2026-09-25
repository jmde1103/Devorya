using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// <변경부분>
// RunStateManager의 엔드 타이머 상태를 화면에 표시하는 공용 UI.
//
// Battle / WorldMap 양쪽에서 동일한 컴포넌트를 재사용한다.
//
// 이 컴포넌트는 타이머 데이터를 직접 저장하거나 변경하지 않고,
// RunStateManager의 현재 상태만 읽어서 표시한다.
public class RunEndTimerUI : MonoBehaviour
{
    [Header("UI References")]

    // 남아 있는 엔드 타이머 Cycle을 표시하는 중앙 숫자.
    [SerializeField]
    private TMP_Text remainingCycleText;


    // 현재 24 Tick 진행 위치에 맞춰 회전시킬 UI Root.
    //
    // 실제 Arrow 또는 외곽 회전 이미지의 부모 RectTransform을 연결한다.
    [SerializeField]
    private RectTransform tickRotationRoot;


    [Header("Tick Rotation")]

    // UI 기준으로 시계방향 진행 여부.
    //
    // Unity UI의 Z축 기준에서는
    // 시계방향 회전이 음수 각도이므로 기본값 true를 사용한다.
    [SerializeField]
    private bool rotateClockwise =
        true;


    // Tick 0일 때 사용할 기본 Z 회전값.
    //
    // 이미지의 기본 방향이 정확히 12시라면 0을 사용한다.
    // 원본 이미지 방향이 다를 경우 Inspector에서 보정할 수 있다.
    [SerializeField]
    private float tickZeroAngle =
     0f;


    [Header("Tick Animation")]
    // <변경부분>
    // End Timer Tick이 변경되었을 때
    // 현재 각도에서 다음 목표 각도까지 회전하는 시간.
    //
    // Tick 수가 여러 개 한 번에 증가하더라도
    // 전체 이동을 이 시간 동안 부드럽게 처리한다.
    [SerializeField, Min(0f)]
    private float rotationDuration =
        0.18f;


    // <변경부분>
    // 회전 보간에 사용할 Curve.
    //
    // 기본값은 시작과 끝이 자연스럽게 감속되는 EaseInOut.
    [SerializeField]
    private AnimationCurve rotationCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // <변경부분>
    // Time.timeScale 영향을 받지 않고
    // UI 연출 자체는 항상 동일한 속도로 재생할지 여부.
    [SerializeField]
    private bool useUnscaledTime =
        true;


    [Header("Count Change Noise")]
    // <변경부분>
    // 중앙의 Remaining Cycle 숫자가 실제로 변경될 때
    // 재생할 기존 UI Noise Animator.
    //
    // UIButtonNoiseAnimator의 버튼 입력은 사용하지 않고
    // PlayNoise()만 직접 호출한다.
    [SerializeField]
    private UIButtonNoiseAnimator countChangeNoiseAnimator;


    [Header("Tick Arrival Impact")]
    // <변경부분>
    // 바늘이 목표 Tick에 도착했을 때
    // 짧게 확대할 실제 Tick Marker.
    //
    // 가능하면 TickRotationRoot 전체가 아니라
    // 그 자식의 실제 Marker / Arrow RectTransform을 연결한다.
    [SerializeField]
    private RectTransform tickImpactTarget;


    // <변경부분>
    // Tick Marker와 동일한 비율로 확대/축소할
    // 중앙 Remaining Cycle 숫자.
    //
    // 비워두면 remainingCycleText의 RectTransform을
    // 자동으로 사용한다.
    [SerializeField]
    private RectTransform cycleTextImpactTarget;


    // <변경부분>
    // 목표 Tick 도착 순간 적용할 확대 배율.
    //
    // Tick Marker와 중앙 숫자에
    // 동일한 배율을 함께 적용한다.
    [SerializeField, Min(1f)]
    private float tickImpactScaleMultiplier =
        1.08f;


    // <변경부분>
    // 확대 후 원래 크기로 돌아오는 전체 시간.
    [SerializeField, Min(0f)]
    private float tickImpactDuration =
        0.12f;


    // <변경부분>
    // 0 = 원래 크기
    // 1 = 최대 확대
    // 다시 0 = 원래 크기
    //
    // Inspector에서 임팩트 감각을 조절할 수 있다.
    [SerializeField]
    private AnimationCurve tickImpactCurve =
        new AnimationCurve(
            new Keyframe(
                0f,
                0f
            ),
            new Keyframe(
                0.35f,
                1f
            ),
            new Keyframe(
                1f,
                0f
            )
        );


    // <변경부분>
    // 각 Tick의 회전이 끝난 순간
    // End Timer UI 전체에 재생할 Noise Animator.
    //
    // Count Change Noise와는 별도 Animator 사용을 권장한다.
    [SerializeField]
    private UIButtonNoiseAnimator tickArrivalNoiseAnimator;


    [Header("Tick Arrival Color Flash")]
    // <변경부분>
    // Tick 도착 순간 빨간색 Flash를 적용할 UI Root.
    //
    // 비워두면 이 RunEndTimerUI가 붙어 있는 Transform을 사용한다.
    // EndTimerRoot를 직접 연결하는 것을 권장한다.
    [SerializeField]
    private Transform colorFlashRoot;


    // <변경부분>
    // Flash 순간 도달할 빨간색.
    //
    // Alpha는 사용하지 않는다.
    // 각 Image가 원래 가지고 있던 Alpha와
    // UIButtonNoiseAnimator의 Alpha Flicker를 그대로 유지한다.
    [SerializeField]
    private Color colorFlashColor =
        new Color(
            1f,
            0.08f,
            0.08f,
            1f
        );


    // <변경부분>
    // 원래 색 → Red → 원래 색으로 돌아오는 전체 시간.
    [SerializeField, Min(0f)]
    private float colorFlashDuration =
        0.14f;


    // <변경부분>
    // 0 = 원래 색
    // 1 = 완전한 Red Flash
    // 다시 0 = 원래 색.
    //
    // 빠르게 빨간색에 도달한 뒤
    // 조금 더 부드럽게 복귀하도록 기본 Curve를 구성한다.
    [SerializeField]
    private AnimationCurve colorFlashCurve =
        new AnimationCurve(
            new Keyframe(
                0f,
                0f
            ),
            new Keyframe(
                0.25f,
                1f
            ),
            new Keyframe(
                1f,
                0f
            )
        );


    // 현재 이벤트를 구독 중인 RunStateManager.
    //
    // Scene 전환이나 오브젝트 Enable/Disable 과정에서
    // 중복 이벤트 구독이 발생하지 않도록 보관한다.
    private RunStateManager subscribedRunStateManager;


    // <변경부분>
    // 현재 실행 중인 Red Flash Coroutine.
    private Coroutine colorFlashCoroutine;


    // <변경부분>
    // Red Flash 대상으로 잡힌 EndTimerRoot 하위 Image 목록.
    private Image[] colorFlashImages;


    // <변경부분>
    // Flash 직전 각 Image가 가지고 있던 원래 색.
    //
    // 각 Image마다 서로 다른 색을 사용하고 있어도
    // 정확하게 자기 색으로 돌아갈 수 있도록 개별 저장한다.
    private Color[] colorFlashBaseColors;


    // <변경부분>
    // 현재 실행 중인 Tick 회전 Coroutine.
    private Coroutine rotationCoroutine;


    // <변경부분>
    // Tick 도착 확대 연출 Coroutine.
    private Coroutine tickImpactCoroutine;


    // <변경부분>
    // Tick Marker의 원래 Scale.
    //
    // 임팩트가 반복되어도 Scale이 누적되지 않도록
    // 최초 정상 상태를 별도로 보관한다.
    private Vector3 tickImpactBaseScale =
        Vector3.one;


    // <변경부분>
    // Tick Marker의 원래 Scale을 저장했는지 여부.
    private bool hasCapturedTickImpactBaseScale;


    // <변경부분>
    // 중앙 숫자의 원래 Scale.
    //
    // Tick Marker와 같은 배율로 확대하되
    // 숫자 자체의 Inspector Scale은 그대로 보존하기 위해
    // 별도의 기준값을 저장한다.
    private Vector3 cycleTextImpactBaseScale =
        Vector3.one;


    // <변경부분>
    // 중앙 숫자의 원래 Scale을 저장했는지 여부.
    private bool hasCapturedCycleTextImpactBaseScale;


    // <변경부분>
    // 실제 UI가 현재 표시 중인 연속 회전 각도.
    //
    // 23 Tick → 0 Tick 전환에서도
    // 반대 방향으로 되돌아가지 않고
    // -345° → -360°처럼 계속 같은 방향으로 회전시키기 위해
    // Transform의 정규화된 Euler 값과 별도로 관리한다.
    private float currentVisualRotation;


    // <변경부분>
    // 현재 타이머 상태가 최종적으로 도달해야 하는 연속 회전 각도.
    //
    // 회전 도중 다음 Tick이 들어와도
    // 이전 목표값을 잃지 않고 다음 진행량을 누적한다.
    private float targetVisualRotation;


    // <변경부분>
    // 최초 Refresh인지 판별한다.
    //
    // Scene이 처음 열릴 때는 현재 Run 상태까지
    // 애니메이션하지 않고 즉시 올바른 위치에 표시한다.
    private bool hasDisplayedTimerState;


    // <변경부분>
    // 이전 Refresh에서 확인했던 Run Timer 값.
    //
    // 실제로 몇 Tick이 전진했는지 계산하고,
    // Remaining Cycle 숫자가 변경되었는지 판별한다.
    private int lastRemainingCycles;
    private int lastCurrentTick;


    private void OnEnable()
    {
        TrySubscribeToRunStateManager();


        // <변경부분>
        // Tick 도착 확대 효과가 반복되어도
        // Scale이 누적되지 않도록 최초 기준 Scale을 저장한다.
        CaptureTickImpactBaseScaleIfNeeded();


        Refresh();
    }


    private void Start()
    {
        // OnEnable 시점에 RunStateManager의 Awake가 아직 실행되지 않은
        // 특수한 초기화 순서에도 대응하기 위해 Start에서 한 번 더 확인한다.
        TrySubscribeToRunStateManager();

        Refresh();
    }


    private void OnDisable()
    {
        UnsubscribeFromRunStateManager();


        // <변경부분>
        // Scene 전환 또는 UI 비활성화 중
        // 회전 Coroutine이 남지 않도록 정리한다.
        if (rotationCoroutine != null)
        {
            StopCoroutine(
                rotationCoroutine
            );

            rotationCoroutine =
                null;
        }


        // <변경부분>
        // Tick Impact가 진행 중인 상태에서 UI가 꺼져도
        // 확대된 Scale이 남지 않도록 즉시 원상복구한다.
        StopTickImpactAndRestore();


        // <변경부분>
        // Red Flash 도중 Scene 전환이나 비활성화가 발생해도
        // 빨간 색상이 UI에 남지 않도록 원래 RGB로 복구한다.
        StopColorFlashAndRestore();
    }


    // 현재 RunStateManager를 찾아
    // 엔드 타이머 변경 이벤트를 구독한다.
    private void TrySubscribeToRunStateManager()
    {
        RunStateManager currentRunStateManager =
            RunStateManager.Instance;


        if (currentRunStateManager == null)
        {
            return;
        }


        if (subscribedRunStateManager ==
            currentRunStateManager)
        {
            return;
        }


        UnsubscribeFromRunStateManager();


        subscribedRunStateManager =
            currentRunStateManager;

        subscribedRunStateManager.EndTimerChanged +=
            Refresh;
    }


    // 기존 RunStateManager 이벤트 구독을 안전하게 해제한다.
    private void UnsubscribeFromRunStateManager()
    {
        if (subscribedRunStateManager == null)
        {
            return;
        }


        subscribedRunStateManager.EndTimerChanged -=
            Refresh;

        subscribedRunStateManager =
            null;
    }


    // RunStateManager의 현재 상태를 읽어
    // 중앙 숫자와 Tick 회전을 갱신한다.
    public void Refresh()
    {
        RunStateManager runStateManager =
            RunStateManager.Instance;


        if (runStateManager == null)
        {
            return;
        }


        int currentRemainingCycles =
            runStateManager.RemainingEndTimerCycles;

        int currentTick =
            Mathf.Clamp(
                runStateManager.CurrentEndTimerTick,
                0,
                RunStateManager.EndTimerTicksPerCycle - 1
            );


        // 남아 있는 큰 Cycle 숫자 표시.
        if (remainingCycleText != null)
        {
            remainingCycleText.text =
                currentRemainingCycles.ToString();
        }


        // <변경부분>
        // Scene 진입 직후 최초 Refresh에서는
        // 저장된 Run 진행 상태까지 회전 연출을 재생하지 않는다.
        //
        // 예:
        // Battle → WorldMap으로 넘어왔을 때 이미 Tick 13이라면
        // 0부터 13까지 회전하지 않고 바로 13 위치에 표시한다.
        if (hasDisplayedTimerState == false)
        {
            hasDisplayedTimerState =
                true;

            lastRemainingCycles =
                currentRemainingCycles;

            lastCurrentTick =
                currentTick;

            SnapRotationToTick(
                currentTick
            );

            return;
        }


        bool remainingCycleChanged =
            currentRemainingCycles !=
            lastRemainingCycles;


        // <변경부분>
        // 이전 상태와 현재 상태를 이용해
        // 실제로 앞으로 몇 Tick 진행했는지 계산한다.
        //
        // 예:
        // 15 / 23 → 14 / 0
        //
        // (15 - 14) * 24 + (0 - 23)
        // = 1 Tick
        //
        // 따라서 마지막 Tick에서도 바늘이 뒤로 되돌아가지 않고
        // -345° → -360°로 자연스럽게 한 칸 더 진행한다.
        int advancedTickCount =
            (
                lastRemainingCycles -
                currentRemainingCycles
            ) *
            RunStateManager.EndTimerTicksPerCycle +
            (
                currentTick -
                lastCurrentTick
            );


        if (advancedTickCount > 0)
        {
            StartRotationAnimation(
                advancedTickCount
            );
        }
        else if (
            currentTick != lastCurrentTick ||
            currentRemainingCycles != lastRemainingCycles)
        {
            // <변경부분>
            // ClearRunState처럼 시간이 앞으로 진행한 것이 아니라
            // 상태 자체가 초기화되거나 되돌아간 경우에는
            // 불필요하게 역회전하지 않고 즉시 새 위치에 맞춘다.
            SnapRotationToTick(
                currentTick
            );
        }


        // <변경부분>
        // 작은 Tick 변화가 아니라
        // 중앙에 표시되는 Remaining Cycle 숫자가 실제로 바뀌었을 때만
        // 기존 UI Noise Animation을 재생한다.
        if (remainingCycleChanged &&
            countChangeNoiseAnimator != null)
        {
            countChangeNoiseAnimator.PlayNoise();
        }


        lastRemainingCycles =
            currentRemainingCycles;

        lastCurrentTick =
            currentTick;
    }


    // <변경부분>
    // 실제 진행된 Tick 수만큼
    // 현재 목표 회전값에 각도를 누적하고 Coroutine을 시작한다.
    private void StartRotationAnimation(
    int advancedTickCount)
    {
        if (tickRotationRoot == null ||
            advancedTickCount <= 0)
        {
            return;
        }


        // <변경부분>
        // 이전 Tick의 착지 확대가 아직 진행 중이라면
        // 다음 회전 전에 정상 Scale로 복원한다.
        StopTickImpactAndRestore();


        float degreesPerTick =
                360f /
            RunStateManager.EndTimerTicksPerCycle;

        float rotationDirection =
            rotateClockwise
                ? -1f
                : 1f;


        // <변경부분>
        // 현재 Transform의 Euler 값을 다시 목표값으로 사용하지 않고,
        // 연속된 논리 각도에 진행량을 누적한다.
        //
        // 이렇게 해야 23 → 0에서도
        // -345 → -360 방향으로 계속 회전한다.
        targetVisualRotation +=
            advancedTickCount *
            degreesPerTick *
            rotationDirection;


        if (rotationCoroutine != null)
        {
            StopCoroutine(
                rotationCoroutine
            );

            rotationCoroutine =
                null;
        }


        if (rotationDuration <= 0f)
        {
            currentVisualRotation =
                targetVisualRotation;

            ApplyVisualRotation(
                currentVisualRotation
            );


            // <변경부분>
            // 회전 시간이 0인 설정에서도
            // 목표 Tick에 도착한 임팩트는 동일하게 실행한다.
            PlayTickArrivalImpact();


            return;
        }


        rotationCoroutine =
            StartCoroutine(
                PlayRotationRoutine(
                    currentVisualRotation,
                    targetVisualRotation
                )
            );
    }


    // <변경부분>
    // 현재 시각 각도에서 목표 각도까지
    // Coroutine으로 부드럽게 회전한다.
    private IEnumerator PlayRotationRoutine(
        float startRotation,
        float endRotation)
    {
        float elapsedTime =
            0f;


        while (elapsedTime <
               rotationDuration)
        {
            float deltaTime =
                useUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            elapsedTime +=
                deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsedTime /
                    rotationDuration
                );


            float curvedT =
                rotationCurve != null
                    ? rotationCurve.Evaluate(t)
                    : t;


            currentVisualRotation =
                Mathf.LerpUnclamped(
                    startRotation,
                    endRotation,
                    curvedT
                );


            ApplyVisualRotation(
                currentVisualRotation
            );


            yield return null;
        }


        currentVisualRotation =
     endRotation;

        ApplyVisualRotation(
            currentVisualRotation
        );


        rotationCoroutine =
            null;


        // <변경부분>
        // 실제 목표 Tick 위치에 정확히 도착한 뒤에만
        // Marker 확대 + 전체 UI Noise 임팩트를 재생한다.
        PlayTickArrivalImpact();
    }


    // <변경부분>
    // Scene 최초 진입이나 Run 초기화처럼
    // 회전 연출이 필요하지 않은 경우 현재 Tick 위치에 즉시 맞춘다.
    private void SnapRotationToTick(
        int tick)
    {
        if (tickRotationRoot == null)
        {
            return;
        }


        if (rotationCoroutine != null)
        {
            StopCoroutine(
                rotationCoroutine
            );

            rotationCoroutine =
                null;
        }


        float degreesPerTick =
            360f /
            RunStateManager.EndTimerTicksPerCycle;

        float rotationDirection =
            rotateClockwise
                ? -1f
                : 1f;


        targetVisualRotation =
            tickZeroAngle +
            tick *
            degreesPerTick *
            rotationDirection;

        currentVisualRotation =
            targetVisualRotation;


        ApplyVisualRotation(
            currentVisualRotation
        );
    }


    // <변경부분>
    // Tick Marker의 정상 Scale을 최초 한 번 저장한다.
    private void CaptureTickImpactBaseScaleIfNeeded()
    {
        // <변경부분>
        // 별도의 Marker가 지정되지 않았다면
        // 최소 fallback으로 Tick Rotation Root를 사용한다.
        if (tickImpactTarget == null)
        {
            tickImpactTarget =
                tickRotationRoot;
        }


        // <변경부분>
        // 숫자 Impact Target을 따로 지정하지 않았다면
        // 현재 Remaining Cycle Text의 RectTransform을 사용한다.
        if (cycleTextImpactTarget == null &&
            remainingCycleText != null)
        {
            cycleTextImpactTarget =
                remainingCycleText.rectTransform;
        }


        // <변경부분>
        // Marker의 기준 Scale은 최초 한 번만 저장한다.
        if (hasCapturedTickImpactBaseScale == false &&
            tickImpactTarget != null)
        {
            tickImpactBaseScale =
                tickImpactTarget.localScale;

            hasCapturedTickImpactBaseScale =
                true;
        }


        // <변경부분>
        // 중앙 숫자의 기준 Scale도 최초 한 번만 따로 저장한다.
        if (hasCapturedCycleTextImpactBaseScale == false &&
            cycleTextImpactTarget != null)
        {
            cycleTextImpactBaseScale =
                cycleTextImpactTarget.localScale;

            hasCapturedCycleTextImpactBaseScale =
                true;
        }
    }


    // <변경부분>
    // 목표 Tick에 실제로 도착했을 때
    // Marker Scale Pulse와 전체 UI Noise를 동시에 시작한다.
    private void PlayTickArrivalImpact()
    {
        CaptureTickImpactBaseScaleIfNeeded();


        // <변경부분>
        // Tick 도착 순간 지정된 UI 영역에
        // 기존 Noise Animation을 한 번 재생한다.
        if (tickArrivalNoiseAnimator != null)
        {
            tickArrivalNoiseAnimator.PlayNoise();
        }


        // <변경부분>
        // Noise와 정확히 같은 착지 순간에
        // EndTimerRoot 하위 Image들을 빠르게 Red Flash시킨다.
        PlayColorFlash();


        // <변경부분>
        // Marker와 숫자 둘 중 하나라도 정상적으로 연결되어 있으면

        // <변경부분>
        // Marker와 숫자 둘 중 하나라도 정상적으로 연결되어 있으면
        // Impact 연출을 실행할 수 있다.
        bool hasTickImpactTarget =
            tickImpactTarget != null &&
            hasCapturedTickImpactBaseScale;

        bool hasCycleTextImpactTarget =
            cycleTextImpactTarget != null &&
            hasCapturedCycleTextImpactBaseScale;


        if (hasTickImpactTarget == false &&
            hasCycleTextImpactTarget == false)
        {
            return;
        }


        StopTickImpactAndRestore();


        if (tickImpactDuration <= 0f ||
            tickImpactScaleMultiplier <= 1f)
        {
            if (hasTickImpactTarget)
            {
                tickImpactTarget.localScale =
                    tickImpactBaseScale;
            }


            if (hasCycleTextImpactTarget)
            {
                cycleTextImpactTarget.localScale =
                    cycleTextImpactBaseScale;
            }


            return;
        }


        tickImpactCoroutine =
            StartCoroutine(
                PlayTickImpactRoutine()
            );
    }


    // <변경부분>
    // Tick Marker를 짧게 확대했다가
    // 정확한 원래 Scale로 되돌리는 착지 Coroutine.
    private IEnumerator PlayTickImpactRoutine()
    {
        float elapsedTime =
            0f;


        // <변경부분>
        // Tick Marker의 확대 목표 Scale.
        Vector3 enlargedTickScale =
            new Vector3(
                tickImpactBaseScale.x *
                tickImpactScaleMultiplier,

                tickImpactBaseScale.y *
                tickImpactScaleMultiplier,

                tickImpactBaseScale.z
            );


        // <변경부분>
        // 중앙 숫자도 Marker와 정확히 동일한 배율을 적용한다.
        Vector3 enlargedCycleTextScale =
            new Vector3(
                cycleTextImpactBaseScale.x *
                tickImpactScaleMultiplier,

                cycleTextImpactBaseScale.y *
                tickImpactScaleMultiplier,

                cycleTextImpactBaseScale.z
            );


        while (elapsedTime <
               tickImpactDuration)
        {
            float deltaTime =
                useUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            elapsedTime +=
                deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsedTime /
                    tickImpactDuration
                );


            float impactT =
                tickImpactCurve != null
                    ? tickImpactCurve.Evaluate(t)
                    : Mathf.Sin(
                        t *
                        Mathf.PI
                    );


            // <변경부분>
            // Tick Marker 확대/축소.
            if (tickImpactTarget != null &&
                hasCapturedTickImpactBaseScale)
            {
                tickImpactTarget.localScale =
                    Vector3.LerpUnclamped(
                        tickImpactBaseScale,
                        enlargedTickScale,
                        impactT
                    );
            }


            // <변경부분>
            // 중앙 숫자도 같은 프레임,
            // 같은 Curve,
            // 같은 비율로 확대/축소한다.
            if (cycleTextImpactTarget != null &&
                hasCapturedCycleTextImpactBaseScale)
            {
                cycleTextImpactTarget.localScale =
                    Vector3.LerpUnclamped(
                        cycleTextImpactBaseScale,
                        enlargedCycleTextScale,
                        impactT
                    );
            }


            yield return null;
        }


        // <변경부분>
        // Coroutine 종료 시 두 UI 모두
        // 최초 Scale로 정확하게 복구한다.
        if (tickImpactTarget != null &&
            hasCapturedTickImpactBaseScale)
        {
            tickImpactTarget.localScale =
                tickImpactBaseScale;
        }


        if (cycleTextImpactTarget != null &&
            hasCapturedCycleTextImpactBaseScale)
        {
            cycleTextImpactTarget.localScale =
                cycleTextImpactBaseScale;
        }


        tickImpactCoroutine =
            null;
    }


    // <변경부분>
    // 진행 중인 Marker Impact를 중단하고
    // 최초 정상 Scale로 복구한다.
    private void StopTickImpactAndRestore()
    {
        if (tickImpactCoroutine != null)
        {
            StopCoroutine(
                tickImpactCoroutine
            );

            tickImpactCoroutine =
                null;
        }


        if (tickImpactTarget != null &&
            hasCapturedTickImpactBaseScale)
        {
            tickImpactTarget.localScale =
                tickImpactBaseScale;
        }


        // <변경부분>
        // 숫자도 Impact 도중 중단됐을 경우
        // 반드시 자신의 원래 Scale로 복구한다.
        if (cycleTextImpactTarget != null &&
            hasCapturedCycleTextImpactBaseScale)
        {
            cycleTextImpactTarget.localScale =
                cycleTextImpactBaseScale;
        }
    }

    // <변경부분>
    // Tick 도착 순간 EndTimerRoot 하위의 모든 Image를
    // 빠르게 빨간색으로 Flash시킨다.
    private void PlayColorFlash()
    {
        // 이전 Flash가 남아 있다면
        // 반드시 원래 색으로 먼저 복원한다.
        StopColorFlashAndRestore();


        if (colorFlashRoot == null)
        {
            colorFlashRoot =
                transform;
        }


        // <변경부분>
        // 실행 시점의 실제 Image 목록을 다시 가져온다.
        //
        // UI Hierarchy가 변경되거나
        // 비활성화된 Image가 존재하는 경우에도 대응한다.
        colorFlashImages =
            colorFlashRoot.GetComponentsInChildren<Image>(
                true
            );


        if (colorFlashImages == null ||
            colorFlashImages.Length <= 0)
        {
            colorFlashBaseColors =
                null;

            return;
        }


        colorFlashBaseColors =
            new Color[colorFlashImages.Length];


        // <변경부분>
        // Flash 직전 각 Image의 실제 색상을 개별 저장한다.
        for (int i = 0;
             i < colorFlashImages.Length;
             i++)
        {
            Image targetImage =
                colorFlashImages[i];


            if (targetImage == null)
            {
                continue;
            }


            colorFlashBaseColors[i] =
                targetImage.color;
        }


        if (colorFlashDuration <= 0f)
        {
            RestoreColorFlashRgb();

            return;
        }


        colorFlashCoroutine =
            StartCoroutine(
                PlayColorFlashRoutine()
            );
    }


    // <변경부분>
    // 원래 RGB → Red → 원래 RGB로
    // 부드럽고 빠르게 왕복하는 Flash Coroutine.
    private IEnumerator PlayColorFlashRoutine()
    {
        float elapsedTime =
            0f;


        while (elapsedTime <
               colorFlashDuration)
        {
            float deltaTime =
                useUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            elapsedTime +=
                deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsedTime /
                    colorFlashDuration
                );


            float flashT =
                colorFlashCurve != null
                    ? colorFlashCurve.Evaluate(t)
                    : Mathf.Sin(
                        t *
                        Mathf.PI
                    );


            for (int i = 0;
                 i < colorFlashImages.Length;
                 i++)
            {
                Image targetImage =
                    colorFlashImages[i];


                if (targetImage == null ||
                    i >= colorFlashBaseColors.Length)
                {
                    continue;
                }


                Color baseColor =
                    colorFlashBaseColors[i];


                // <변경부분>
                // Alpha는 현재 값을 그대로 사용한다.
                //
                // 따라서 같은 순간 UIButtonNoiseAnimator가
                // Alpha Flicker를 실행해도 서로 덮어쓰지 않는다.
                float currentAlpha =
                    targetImage.color.a;


                Color targetRedColor =
                    new Color(
                        colorFlashColor.r,
                        colorFlashColor.g,
                        colorFlashColor.b,
                        currentAlpha
                    );


                Color flashColor =
                    Color.LerpUnclamped(
                        new Color(
                            baseColor.r,
                            baseColor.g,
                            baseColor.b,
                            currentAlpha
                        ),
                        targetRedColor,
                        flashT
                    );


                flashColor.a =
                    currentAlpha;


                targetImage.color =
                    flashColor;
            }


            yield return null;
        }


        RestoreColorFlashRgb();


        colorFlashCoroutine =
            null;
    }


    // <변경부분>
    // Red Flash를 중단하고
    // 각 Image를 Flash 직전의 RGB로 복구한다.
    //
    // Alpha는 다른 Noise Animation이 제어하고 있을 수 있으므로
    // 현재 Alpha를 그대로 유지한다.
    private void StopColorFlashAndRestore()
    {
        if (colorFlashCoroutine != null)
        {
            StopCoroutine(
                colorFlashCoroutine
            );

            colorFlashCoroutine =
                null;
        }


        RestoreColorFlashRgb();
    }


    // <변경부분>
    // 저장된 원래 RGB만 복구한다.
    //
    // Alpha를 건드리지 않아
    // 기존 UIButtonNoiseAnimator의 Flicker와 충돌하지 않는다.
    private void RestoreColorFlashRgb()
    {
        if (colorFlashImages == null ||
            colorFlashBaseColors == null)
        {
            return;
        }


        int restoreCount =
            Mathf.Min(
                colorFlashImages.Length,
                colorFlashBaseColors.Length
            );


        for (int i = 0;
             i < restoreCount;
             i++)
        {
            Image targetImage =
                colorFlashImages[i];


            if (targetImage == null)
            {
                continue;
            }


            Color currentColor =
                targetImage.color;

            Color baseColor =
                colorFlashBaseColors[i];


            currentColor.r =
                baseColor.r;

            currentColor.g =
                baseColor.g;

            currentColor.b =
                baseColor.b;


            targetImage.color =
                currentColor;
        }
    }


    // <변경부분>
    // 계산된 연속 회전 각도를 실제 RectTransform에 적용한다.
    private void ApplyVisualRotation(
        float zRotation)
    {
        if (tickRotationRoot == null)
        {
            return;
        }


        Vector3 currentEulerAngles =
            tickRotationRoot.localEulerAngles;

        currentEulerAngles.z =
            zRotation;

        tickRotationRoot.localEulerAngles =
            currentEulerAngles;
    }
}
