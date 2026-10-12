// <변경부분>
// 낮·밤 광원 페이드 Coroutine 사용.
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;


public class FlameLightFlickerController : MonoBehaviour
{
    [System.Serializable]
    private class LightTarget
    {
        [Header("Light")]
        [SerializeField]
        private Light2D light;

        [Header("Per Light Multiplier")]
        [SerializeField, Min(0f)]
        private float intensityMultiplier =
            1f;

        [SerializeField, Range(0f, 1f)]
        private float colorInfluence =
            1f;


        [System.NonSerialized]
        public float originalIntensity;

        [System.NonSerialized]
        public Color originalColor;


        public Light2D Light =>
            light;

        public float IntensityMultiplier =>
            intensityMultiplier;

        public float ColorInfluence =>
            colorInfluence;
    }


    [Header("Target Lights")]
    [SerializeField]
    private LightTarget[] targetLights;


    [Header("Flicker Speed")]

    // 불꽃 전체가 천천히 흔들리는 큰 흐름.
    [SerializeField, Min(0f)]
    private float mainFlickerSpeed =
        2.2f;

    // 빠르고 작은 불규칙 흔들림.
    [SerializeField, Min(0f)]
    private float detailFlickerSpeed =
        8f;

    // 빠른 흔들림이 전체 결과에 섞이는 비율.
    [SerializeField, Range(0f, 1f)]
    private float detailFlickerAmount =
        0.35f;


    [Header("Intensity")]

    // 원래 Light2D Intensity에서
    // 위아래로 변화할 수 있는 비율.
    //
    // 0.2 =
    // 약 80% ~ 120%.
    [SerializeField, Range(0f, 1f)]
    private float intensityVariation =
        0.22f;

    // 급격하게 어두워졌다가 돌아오는
    // 작은 불꽃 흔들림을 얼마나 섞을지 결정.
    [SerializeField, Range(0f, 1f)]
    private float irregularDipAmount =
        0.12f;

    [SerializeField, Min(0f)]
    private float irregularDipSpeed =
        3.5f;


    [Header("Color")]

    [SerializeField]
    private bool useColorFlicker =
        true;

    // 불꽃이 약해졌을 때.
    // 조금 더 붉고 주황색.
    [SerializeField]
    private Color dimColor =
        new Color(
            1f,
            0.38f,
            0.08f,
            1f
        );

    // 불꽃이 강해졌을 때.
    // 조금 더 밝은 노란색.
    [SerializeField]
    private Color brightColor =
        new Color(
            1f,
            0.82f,
            0.38f,
            1f
        );


    [Header("Wind / Light Sway")]

    // 실제 랜턴이나 횃불 오브젝트 전체가 아니라
    // Light2D를 담고 있는 별도 자식을 연결하는 것을 권장.
    //
    // 비워두면 위치 흔들림은 사용하지 않는다.
    [SerializeField]
    private Transform lightSwayRoot;

    // 기본적인 바람 방향.
    [SerializeField]
    private Vector2 windDirection =
        new Vector2(
            1f,
            0.15f
        );

    // 바람 방향으로 흔들리는 거리.
    [SerializeField, Min(0f)]
    private float windSwayAmount =
        0.035f;

    // 바람과 수직 방향의 작은 난류.
    [SerializeField, Min(0f)]
    private float turbulenceAmount =
        0.015f;

    [SerializeField, Min(0f)]
    private float swaySpeed =
        1.8f;

    // Light Root가 아주 조금 회전하도록 한다.
    [SerializeField, Min(0f)]
    private float rotationSwayDegrees =
        1.5f;


    [Header("Runtime")]

    // 일반적으로 게임 Pause에서는
    // 불꽃도 같이 멈추는 것이 자연스럽기 때문에 false 권장.
    [SerializeField]
    private bool useUnscaledTime =
        false;

    // Disable 시 원래 Light 값과 Transform으로 복구.
    [SerializeField]
    private bool restoreOnDisable =
        true;


    // <변경부분>
    // 체크된 광원만 밤에 켜지고 낮에는 Intensity 0으로 유지한다.
    // 기존 랜턴/횃불 Sprite와 GameObject는 비활성화하지 않는다.
    [Header("Day Night Light")]
    [SerializeField]
    private bool nightOnly = false;

    // <변경부분>
    // 환경 조명 페이드와 동일한 시간으로 설정한다. 기본값 3초.
    [SerializeField, Min(0f)]
    private float dayNightFadeDuration = 3f;

    // <변경부분>
    // 기존 불꽃 흔들림 결과에 곱할 낮/밤 밝기 배율(0~1).
    private float dayNightIntensityMultiplier = 1f;

    // <변경부분>
    // 진행 중인 페이드와 영속 런 매니저 이벤트 구독을 관리한다.
    private Coroutine dayNightFadeCoroutine;
    private RunStateManager subscribedRunStateManager;


    private float randomSeed;


    private Vector3 originalSwayLocalPosition;

    private Quaternion originalSwayLocalRotation;


    private bool hasCachedOriginalValues;

    private bool hasWarnedMissingLights;


    private void Awake()
    {
        ResolveTargetLights();

        CacheOriginalValues();

        InitializeRandomSeed();
    }


    private void OnEnable()
    {
        ResolveTargetLights();

        CacheOriginalValues();

        InitializeRandomSeed();

        // <변경부분>
        // Night Only 광원은 RunStateManager가 아직 준비되지 않았더라도
        // 최초 활성화 시 기본적으로 밝기 0에서 시작한다.
        //
        // 원본 광량을 먼저 캐시한 뒤 적용하므로
        // 밤이 되었을 때 원래 Intensity를 정상 복원할 수 있다.
        if (Application.isPlaying && nightOnly)
        {
            SetDayNightIntensityImmediately(0f);
        }

        // <변경부분>
        // RunStateManager가 준비되어 있다면 현재 낮/밤 상태를 즉시 반영한다.
        // 밤이라면 저장된 원본 광량으로 복원하고,
        // 낮이라면 Intensity 0을 유지한다.
        TrySubscribeDayNightState();
    }


    // <변경부분>
    // Scene Awake 순서상 RunStateManager가 늦게 준비되는 경우도 대응한다.
    private void Start()
    {
        TrySubscribeDayNightState();
    }


    private void OnDisable()
    {
        // <변경부분>
        // Scene 이동이나 Decoration 삭제 시 영속 RunState 이벤트를 해제한다.
        if (subscribedRunStateManager != null)
        {
            subscribedRunStateManager.DayNightStateChanged -=
                HandleDayNightStateChanged;

            subscribedRunStateManager = null;
        }

        // <변경부분>
        // 비활성화된 오브젝트가 이전 페이드 상태를 보유하지 않도록 정리한다.
        if (dayNightFadeCoroutine != null)
        {
            StopCoroutine(dayNightFadeCoroutine);
            dayNightFadeCoroutine = null;
        }

        dayNightIntensityMultiplier = 1f;

        if (restoreOnDisable)
        {
            RestoreOriginalValues();
        }
    }


    // <변경부분>
    // 활성화된 개별 Prefab이 RunStateManager 이벤트를 직접 구독한다.
    // BackgroundMapData에는 Scene GameObject 참조를 저장하지 않는다.
    private void TrySubscribeDayNightState()
    {
        if (!Application.isPlaying || !nightOnly)
        {
            return;
        }

        RunStateManager manager =
            RunStateManager.Instance;

        if (subscribedRunStateManager == manager)
        {
            return;
        }

        if (subscribedRunStateManager != null)
        {
            subscribedRunStateManager.DayNightStateChanged -=
                HandleDayNightStateChanged;

            subscribedRunStateManager = null;
        }

        if (manager == null)
        {
            return;
        }

        subscribedRunStateManager = manager;

        subscribedRunStateManager.DayNightStateChanged +=
            HandleDayNightStateChanged;

        // <변경부분>
        // 새 맵 로드 시에는 페이드하지 않고
        // 현재 시간대의 광량을 즉시 적용한다.
        SetDayNightIntensityImmediately(
            manager.CurrentDayNightState == RunDayNightState.Night
                ? 1f
                : 0f
        );
    }


    // <변경부분>
    // 낮과 밤의 상태 변경 시 불꽃 Flicker는 유지하고
    // 낮/밤 밝기 배율만 0~1 사이에서 전환한다.
    private void HandleDayNightStateChanged(
        RunDayNightState state)
    {
        float targetMultiplier =
            state == RunDayNightState.Night
                ? 1f
                : 0f;

        if (dayNightFadeCoroutine != null)
        {
            StopCoroutine(dayNightFadeCoroutine);
            dayNightFadeCoroutine = null;
        }

        if (!isActiveAndEnabled ||
            dayNightFadeDuration <= 0f)
        {
            SetDayNightIntensityImmediately(targetMultiplier);
            return;
        }

        // <변경부분>
        // 전환 도중 낮/밤이 다시 바뀌어도
        // 현재 밝기에서 새로운 목표값까지 페이드한다.
        dayNightFadeCoroutine = StartCoroutine(
            FadeDayNightIntensity(targetMultiplier)
        );
    }


    // <변경부분>
    // 환경 조명과 동일한 SmoothStep 방식의 부드러운 페이드.
    // TimeScale과 관계없이 실제 시간 기준으로 작동한다.
    private IEnumerator FadeDayNightIntensity(
        float targetMultiplier)
    {
        float startMultiplier =
            dayNightIntensityMultiplier;

        float elapsed = 0f;

        float duration =
            Mathf.Max(0.0001f, dayNightFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            dayNightIntensityMultiplier = Mathf.Lerp(
                startMultiplier,
                targetMultiplier,
                t
            );

            yield return null;
        }

        dayNightIntensityMultiplier =
            targetMultiplier;

        dayNightFadeCoroutine = null;
    }


    // <변경부분>
    // Scene 최초 진입 시 한 프레임 동안 밝은 랜턴이
    // 보이지 않도록 현재 시간대의 광량을 즉시 설정한다.
    private void SetDayNightIntensityImmediately(
        float value)
    {
        dayNightIntensityMultiplier =
            Mathf.Clamp01(value);

        if (!hasCachedOriginalValues ||
            targetLights == null)
        {
            return;
        }

        for (int i = 0;
             i < targetLights.Length;
             i++)
        {
            LightTarget target =
                targetLights[i];

            if (target == null ||
                target.Light == null)
            {
                continue;
            }

            target.Light.intensity =
                target.originalIntensity *
                target.IntensityMultiplier *
                dayNightIntensityMultiplier;
        }
    }


    private void Update()
    {
        // <변경부분>
        // 런 매니저가 늦게 생성되거나 교체된 경우
        // 현재 시간대 동기화 및 이벤트 재구독을 수행한다.
        if (nightOnly &&
            Application.isPlaying &&
            subscribedRunStateManager != RunStateManager.Instance)
        {
            TrySubscribeDayNightState();
        }

        if (targetLights == null ||
            targetLights.Length == 0)
        {
            return;
        }


        float currentTime =
            useUnscaledTime
                ? Time.unscaledTime
                : Time.time;


        // =====================================================
        // Main Flicker
        // =====================================================

        float mainNoise =
            Mathf.PerlinNoise(
                randomSeed,
                currentTime *
                mainFlickerSpeed
            );


        // =====================================================
        // Detail Flicker
        // =====================================================

        float detailNoise =
            Mathf.PerlinNoise(
                randomSeed +
                13.371f,
                currentTime *
                detailFlickerSpeed
            );


        float flickerValue =
            Mathf.Lerp(
                mainNoise,
                detailNoise,
                detailFlickerAmount
            );


        // 너무 기계적으로 선형 변화하지 않도록
        // 가운데 흐름을 조금 부드럽게 만든다.
        flickerValue =
            Mathf.SmoothStep(
                0f,
                1f,
                flickerValue
            );


        // =====================================================
        // Irregular Dip
        // =====================================================

        float dipNoise =
            Mathf.PerlinNoise(
                randomSeed +
                37.127f,
                currentTime *
                irregularDipSpeed
            );


        // 특정 구간에서만 약간 더 크게 반응하도록
        // Noise 하단부를 강조한다.
        float dip =
            Mathf.InverseLerp(
                0.32f,
                0f,
                dipNoise
            );


        dip =
            Mathf.Clamp01(
                dip
            );


        // =====================================================
        // Intensity
        // =====================================================

        float minIntensityFactor =
            1f -
            intensityVariation;

        float maxIntensityFactor =
            1f +
            intensityVariation;


        float intensityFactor =
            Mathf.Lerp(
                minIntensityFactor,
                maxIntensityFactor,
                flickerValue
            );


        intensityFactor *=
            1f -
            (
                dip *
                irregularDipAmount
            );


        // =====================================================
        // Color
        // =====================================================

        Color flickerColor =
            Color.Lerp(
                dimColor,
                brightColor,
                flickerValue
            );


        ApplyToLights(
            intensityFactor,
            flickerColor
        );


        // =====================================================
        // Wind Sway
        // =====================================================

        ApplyWindSway(
            currentTime
        );
    }


    private void ApplyToLights(
        float intensityFactor,
        Color flickerColor)
    {
        for (int i = 0;
             i < targetLights.Length;
             i++)
        {
            LightTarget target =
                targetLights[i];


            if (target == null ||
                target.Light == null)
            {
                continue;
            }


            // <변경부분>
            // 기존 Flicker 강도에 낮/밤 밝기 배율만 곱한다.
            // Intensity가 0이어도 Sprite/Light GameObject는 활성 상태를 유지한다.
            target.Light.intensity =
                target.originalIntensity *
                intensityFactor *
                target.IntensityMultiplier *
                dayNightIntensityMultiplier;


            if (useColorFlicker)
            {
                target.Light.color =
                    Color.Lerp(
                        target.originalColor,
                        flickerColor,
                        target.ColorInfluence
                    );
            }
            else
            {
                target.Light.color =
                    target.originalColor;
            }
        }
    }


    private void ApplyWindSway(
        float currentTime)
    {
        if (lightSwayRoot == null)
        {
            return;
        }


        Vector2 normalizedWindDirection =
            windDirection.sqrMagnitude >
            0.0001f
                ? windDirection.normalized
                : Vector2.right;


        Vector2 perpendicularDirection =
            new Vector2(
                -normalizedWindDirection.y,
                normalizedWindDirection.x
            );


        float windNoise =
            Mathf.PerlinNoise(
                randomSeed +
                71.23f,
                currentTime *
                swaySpeed
            );


        float turbulenceNoise =
            Mathf.PerlinNoise(
                randomSeed +
                97.41f,
                currentTime *
                swaySpeed *
                1.7f
            );


        float windOffset =
            (
                windNoise -
                0.5f
            ) *
            2f *
            windSwayAmount;


        float turbulenceOffset =
            (
                turbulenceNoise -
                0.5f
            ) *
            2f *
            turbulenceAmount;


        Vector2 finalOffset =
            normalizedWindDirection *
            windOffset +
            perpendicularDirection *
            turbulenceOffset;


        lightSwayRoot.localPosition =
            originalSwayLocalPosition +
            new Vector3(
                finalOffset.x,
                finalOffset.y,
                0f
            );


        float rotationNoise =
            Mathf.PerlinNoise(
                randomSeed +
                121.73f,
                currentTime *
                swaySpeed
            );


        float rotationOffset =
            (
                rotationNoise -
                0.5f
            ) *
            2f *
            rotationSwayDegrees;


        lightSwayRoot.localRotation =
            originalSwayLocalRotation *
            Quaternion.Euler(
                0f,
                0f,
                rotationOffset
            );
    }


    private void ResolveTargetLights()
    {
        bool hasValidTarget =
            false;


        if (targetLights != null)
        {
            for (int i = 0;
                 i < targetLights.Length;
                 i++)
            {
                LightTarget target =
                    targetLights[i];


                if (target != null &&
                    target.Light != null)
                {
                    hasValidTarget =
                        true;

                    break;
                }
            }
        }


        if (hasValidTarget)
        {
            return;
        }


        if (hasWarnedMissingLights ==
            false)
        {
            hasWarnedMissingLights =
                true;

            Debug.LogWarning(
                "[FlameLightFlickerController] " +
                "Target Lights가 연결되어 있지 않습니다.",
                this
            );
        }
    }


    private void CacheOriginalValues()
    {
        if (targetLights != null)
        {
            for (int i = 0;
                 i < targetLights.Length;
                 i++)
            {
                LightTarget target =
                    targetLights[i];


                if (target == null ||
                    target.Light == null)
                {
                    continue;
                }


                target.originalIntensity =
                    target.Light.intensity;

                target.originalColor =
                    target.Light.color;
            }
        }


        if (lightSwayRoot != null)
        {
            originalSwayLocalPosition =
                lightSwayRoot.localPosition;

            originalSwayLocalRotation =
                lightSwayRoot.localRotation;
        }


        hasCachedOriginalValues =
            true;
    }


    private void RestoreOriginalValues()
    {
        if (hasCachedOriginalValues ==
            false)
        {
            return;
        }


        if (targetLights != null)
        {
            for (int i = 0;
                 i < targetLights.Length;
                 i++)
            {
                LightTarget target =
                    targetLights[i];


                if (target == null ||
                    target.Light == null)
                {
                    continue;
                }


                target.Light.intensity =
                    target.originalIntensity;

                target.Light.color =
                    target.originalColor;
            }
        }


        if (lightSwayRoot != null)
        {
            lightSwayRoot.localPosition =
                originalSwayLocalPosition;

            lightSwayRoot.localRotation =
                originalSwayLocalRotation;
        }
    }


    private void InitializeRandomSeed()
    {
        // UnityEngine.Random을 사용하지 않는다.
        //
        // 전투 / AI / 맵 생성 등의 Random State를
        // 환경 연출 때문에 변경하지 않기 위함.
        randomSeed =
            Mathf.Abs(
                GetInstanceID()
            ) *
            0.01373f;
    }
}
