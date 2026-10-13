using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;


// <변경부분>
// WorldMap 전용 낮/밤 조명 컨트롤러.
//
// - 기존 RunStateManager의 낮/밤 판정을 그대로 사용한다.
// - EnvironmentVisualProfile을 사용하지 않는다.
// - Global Light 2D의 Color만 변경한다.
// - 월드맵 진입 시 현재 시간대의 색상을 즉시 적용한다.
// - 월드맵 이용 중 낮/밤이 변경되면 부드럽게 전환한다.
// - 전환 도중 목표가 변경되면 현재 색상에서 다시 전환한다.
// - Global Light의 Intensity, Enabled 등은 변경하지 않는다.
// - Player Marker의 Spot Light도 제어하지 않는다.
// - 월드맵 타이머를 추가로 진행시키지 않는다.
public class WorldMapDayNightLightingController : MonoBehaviour
{
    [Header("Global Light 2D")]

    // 월드맵 전체를 비추는 Global Light 2D.
    [SerializeField]
    private Light2D globalLight;


    [Header("Day / Night Colors")]

    // 낮 시간대 Global Light 색상.
    [SerializeField]
    private Color dayColor = Color.white;

    // 밤 시간대 Global Light 색상.
    [SerializeField]
    private Color nightColor =
        new Color(0.45f, 0.55f, 0.8f, 1f);


    // <변경부분>
    // 플레이어 마커에 설치한 Spot Light 2D.
    // EnvironmentVisualProfile 없이 직접 제어한다.
    [Header("Player Marker Spot Light 2D")]

    [SerializeField]
    private Light2D playerMarkerSpotLight;

    // <변경부분>
    // 밤 -> 낮 페이드 과정에서 사용할 색상.
    // 낮에는 Spot Light 컴포넌트 자체가 비활성화된다.
    [SerializeField]
    private Color daySpotColor = Color.white;

    // <변경부분>
    // 기존 Inspector 직렬화 데이터 호환을 위해 유지한다.
    // 낮 Spot Light는 완전히 OFF이므로 더 이상 사용하지 않는다.
    [HideInInspector]
    [SerializeField, Min(0f)]
    private float daySpotIntensity = 0f;

    // <변경부분>
    // 밤 시간대 Spot Light 색상과 강도.
    // 이 값을 이용해 마커 주변을 밝힌다.
    [SerializeField]
    private Color nightSpotColor = Color.white;

    [SerializeField, Min(0f)]
    private float nightSpotIntensity = 0.9f;


    [Header("Transition")]

    // <변경부분>
    // 낮/밤 색상 전환에 걸리는 시간.
    // Battle / Event와 동일하게 기본 3초.
    [SerializeField, Min(0f)]
    private float transitionDuration = 3f;


    // <변경부분>
    // 현재 이벤트를 구독 중인 런 매니저.
    private RunStateManager subscribedRunStateManager;

    // <변경부분>
    // 진행 중인 색상 전환 코루틴.
    private Coroutine transitionCoroutine;

    // <변경부분>
    // 현재 시간대의 색상을 최초 적용했는지 확인한다.
    private bool hasAppliedInitialLighting;

    // <변경부분>
    // 현재 목표 시간대.
    private RunDayNightState currentTargetState;

    // <변경부분>
    // 참조 누락 경고를 반복 출력하지 않는다.
    private bool hasLoggedMissingReference;


    private void OnEnable()
    {
        // <변경부분>
        // RunStateManager가 준비되어 있다면
        // 현재 낮/밤 상태를 즉시 적용한다.
        TrySubscribeAndInitialize();
    }


    private void Start()
    {
        // <변경부분>
        // 다른 컴포넌트의 Awake 이후에도
        // 정상적으로 초기화될 수 있도록 재확인한다.
        TrySubscribeAndInitialize();
    }


    private void Update()
    {
        // <변경부분>
        // RunStateManager가 늦게 생성된 경우에만
        // 이벤트 연결을 다시 시도한다.
        //
        // 연결된 이후에는 매 프레임 색상을 계산하지 않는다.
        if (subscribedRunStateManager == null)
        {
            TrySubscribeAndInitialize();
        }
    }


    private void OnDisable()
    {
        // <변경부분>
        // Scene 종료 및 오브젝트 비활성화 시
        // 기존 이벤트 구독을 해제한다.
        if (subscribedRunStateManager != null)
        {
            subscribedRunStateManager.DayNightStateChanged -=
                HandleDayNightStateChanged;

            subscribedRunStateManager = null;
        }

        StopLightingTransition();

        hasAppliedInitialLighting = false;
    }


    // <변경부분>
    // RunStateManager의 낮/밤 이벤트를 구독하고
    // 현재 시간대의 Global Light 색상을 즉시 적용한다.
    private void TrySubscribeAndInitialize()
    {
        // <변경부분>
        // Global Light와 플레이어 마커 Spot Light가
        // 모두 연결되어 있어야 낮/밤 초기화를 진행한다.
        if (globalLight == null ||
            playerMarkerSpotLight == null)
        {
            if (!hasLoggedMissingReference)
            {
                Debug.LogWarning(
                    "[WorldMapDayNightLightingController] " +
                    "Global Light 또는 Player Marker Spot Light " +
                    "참조가 비어 있습니다.",
                    this
                );

                hasLoggedMissingReference = true;
            }

            return;
        }

        RunStateManager runStateManager =
            RunStateManager.Instance;

        if (runStateManager == null)
        {
            return;
        }

        // <변경부분>
        // 다른 런 매니저로 변경되었을 경우
        // 이전 이벤트 구독을 해제한다.
        if (subscribedRunStateManager != runStateManager)
        {
            if (subscribedRunStateManager != null)
            {
                subscribedRunStateManager.DayNightStateChanged -=
                    HandleDayNightStateChanged;
            }

            subscribedRunStateManager =
                runStateManager;

            subscribedRunStateManager.DayNightStateChanged -=
                HandleDayNightStateChanged;

            subscribedRunStateManager.DayNightStateChanged +=
                HandleDayNightStateChanged;

            hasAppliedInitialLighting = false;
        }

        // <변경부분>
        // 월드맵 최초 진입 시에는 페이드 없이
        // 현재 런의 시간대에 맞는 색상을 적용한다.
        //
        // 배틀이나 이벤트에서 밤으로 바뀐 뒤 돌아오더라도
        // 월드맵의 조명 상태가 일치한다.
        if (!hasAppliedInitialLighting)
        {
            currentTargetState =
                runStateManager.CurrentDayNightState;

            ApplyColorImmediately(
                GetColor(currentTargetState)
            );

            hasAppliedInitialLighting = true;
        }
    }


    // <변경부분>
    // 기존 RunStateManager에서 낮/밤이 바뀌면 호출된다.
    private void HandleDayNightStateChanged(
        RunDayNightState newState)
    {
        if (hasAppliedInitialLighting &&
            currentTargetState == newState)
        {
            return;
        }

        currentTargetState =
            newState;

        TransitionToColor(
            GetColor(newState)
        );
    }


    // <변경부분>
    // 현재 시간대에 사용할 색상을 반환한다.
    private Color GetColor(
        RunDayNightState state)
    {
        return state == RunDayNightState.Night
            ? nightColor
            : dayColor;
    }


    // <변경부분>
    // <변경부분>
    // 월드맵 진입 시 현재 낮/밤에 맞는 조명을 즉시 적용한다.
    //
    // Global Light: 색상만 변경.
    // Marker Spot Light: 색상과 Intensity 변경.
    private void ApplyColorImmediately(
        Color targetColor)
    {
        StopLightingTransition();

        if (globalLight == null ||
            playerMarkerSpotLight == null)
        {
            return;
        }

        // 기존 Global Light의 Intensity, Enabled 유지.
        globalLight.color = targetColor;

        // <변경부분>
        // 현재 시간대에 따라 Spot Light 목표값 결정.
        bool isNight =
            currentTargetState == RunDayNightState.Night;

        playerMarkerSpotLight.color =
            isNight ? nightSpotColor : daySpotColor;

        // <변경부분>
        // 밤이면 설정한 광량을 적용한다.
        // 낮이면 광량을 0으로 초기화한다.
        playerMarkerSpotLight.intensity =
            isNight
                ? Mathf.Max(0f, nightSpotIntensity)
                : 0f;

        // <변경부분>
        // 낮에는 Light2D 컴포넌트 자체를 OFF한다.
        // 밤에 월드맵에 진입한 경우에만 즉시 ON한다.
        playerMarkerSpotLight.enabled = isNight;
    }


    // <변경부분>
    // 현재 Global Light 색상에서 목표 색상으로
    // 부드러운 페이드 전환을 시작한다.
    private void TransitionToColor(
        Color targetColor)
    {
        if (globalLight == null)
        {
            return;
        }

        StopLightingTransition();

        if (transitionDuration <= 0f ||
            !isActiveAndEnabled)
        {
            ApplyColorImmediately(
                targetColor
            );

            return;
        }

        transitionCoroutine =
            StartCoroutine(
                TransitionColorRoutine(
                    targetColor
                )
            );
    }


    // <변경부분>
    // 낮/밤 전환 시 Global Light 색상과
    // 플레이어 마커 Spot Light 색상·광량을 함께 보간한다.
    //
    // SmoothStep + unscaledDeltaTime 사용.
    private IEnumerator TransitionColorRoutine(
        Color targetColor)
    {
        // <변경부분>
        // 현재 실제 조명값을 시작값으로 저장한다.
        // 전환 도중 시간이 다시 바뀌어도 자연스럽게 이어진다.
        Color startGlobalColor =
            globalLight.color;

        Color startSpotColor =
            playerMarkerSpotLight.color;

        float startSpotIntensity =
            playerMarkerSpotLight.enabled
                ? playerMarkerSpotLight.intensity
                : 0f;

        // <변경부분>
        // 현재 목표 시간대의 Spot Light 설정을 저장한다.
        bool isNight =
            currentTargetState == RunDayNightState.Night;

        Color targetSpotColor =
            isNight ? nightSpotColor : daySpotColor;

        // <변경부분>
        // 낮에는 반드시 광량 0을 목표로 한다.
        float targetSpotIntensity =
            isNight
                ? Mathf.Max(0f, nightSpotIntensity)
                : 0f;

        // <변경부분>
        // 페이드 중에는 Spot Light를 활성화해
        // 낮 -> 밤 / 밤 -> 낮 양쪽 연출을 자연스럽게 처리한다.
        //
        // 낮 전환 완료 후에는 완전히 OFF한다.
        playerMarkerSpotLight.intensity =
            startSpotIntensity;

        playerMarkerSpotLight.enabled = true;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / transitionDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            // <변경부분>
            // 월드맵 Global Light 색상 보간.
            globalLight.color =
                Color.Lerp(
                    startGlobalColor,
                    targetColor,
                    smoothProgress
                );

            // <변경부분>
            // 마커 Spot Light 색상 보간.
            playerMarkerSpotLight.color =
                Color.Lerp(
                    startSpotColor,
                    targetSpotColor,
                    smoothProgress
                );

            // <변경부분>
            // 마커 Spot Light 광량 보간.
            // 낮 -> 밤: 점차 밝아짐.
            // 밤 -> 낮: 점차 어두워짐.
            playerMarkerSpotLight.intensity =
                Mathf.Lerp(
                    startSpotIntensity,
                    targetSpotIntensity,
                    smoothProgress
                );

            yield return null;
        }

        // <변경부분>
        // 전환 완료 시 최종값을 정확하게 적용한다.
        globalLight.color =
            targetColor;

        playerMarkerSpotLight.color =
      targetSpotColor;

        playerMarkerSpotLight.intensity =
            targetSpotIntensity;

        // <변경부분>
        // 밤 -> 낮 페이드가 완전히 끝난 후에만
        // Spot Light 2D 컴포넌트를 비활성화한다.
        //
        // 낮 -> 밤 전환 완료 시에는 활성 상태를 유지한다.
        playerMarkerSpotLight.enabled = isNight;

        transitionCoroutine = null;
    }


    // <변경부분>
    // 진행 중인 전환을 안전하게 중지한다.
    private void StopLightingTransition()
    {
        if (transitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            transitionCoroutine
        );

        transitionCoroutine =
            null;
    }
}