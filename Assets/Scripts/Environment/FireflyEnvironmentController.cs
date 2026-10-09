using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


[System.Serializable]
public class FireflyEnvironmentUnit
{
    [Header("Object")]

    public Transform root;

    public SpriteRenderer spriteRenderer;


    [Header("Lights")]

    public Light2D spotLight;

    public Light2D spriteLight;


    [Header("Per Firefly Multiplier")]

    [Min(0f)]
    public float moveSpeedMultiplier =
        1f;

    [Min(0f)]
    public float spotLightIntensityMultiplier =
        1f;

    [Min(0f)]
    public float spriteLightIntensityMultiplier =
        1f;
}


public class FireflyEnvironmentController : MonoBehaviour
{
    private enum VisibilityState
    {
        Hidden,
        FadeIn,
        Visible,
        FadeOut
    }


    private class FireflyRuntimeState
    {
        public VisibilityState visibilityState;

        public float stateTimer;

        public float directionTimer;

        public float moveSpeed;

        public float intensityScale;

        public Vector2 currentDirection;

        public Vector2 targetDirection;
    }

    [Header("Camera")]

    [SerializeField]
    private Camera targetCamera;

    // <변경부분>
    // 현재 화면이 아니라
    // PixelCameraController의 최대 Zoom Out 영역을
    // 반딧불 Spawn / 이동 기준으로 사용한다.
    [SerializeField]
    private PixelCameraController pixelCameraController;


    [Header("Fireflies")]

    [SerializeField]
    private FireflyEnvironmentUnit[] fireflies;


    [Header("Rig Calibration")]

    [SerializeField, Min(0f)]
    private float movementSpeedMultiplier =
        1f;

    [SerializeField, Min(0f)]
    private float spotLightIntensityMultiplier =
        1f;

    [SerializeField, Min(0f)]
    private float spriteLightIntensityMultiplier =
        1f;

    [SerializeField, Min(0f)]
    private float fadeDurationMultiplier =
        1f;


    private EnvironmentFireflySettings
       currentSettings;

    private FireflyRuntimeState[]
        runtimeStates;


    // 랜턴 / 횃불 / 모닥불 등
    // 반딧불이 피해야 할 외부 Point / Spot Light.
    private readonly List<Light2D>
        externalAvoidanceLights =
            new List<Light2D>(16);


    private float nextExternalLightRefreshTime =
        0f;


    public void ApplyProfile(
        EnvironmentVisualProfile visualProfile)
    {
        if (visualProfile == null ||
            visualProfile.fireflies == null)
        {
            currentSettings =
                null;

            gameObject.SetActive(
                false
            );

            return;
        }


        EnvironmentFireflySettings settings =
            visualProfile.fireflies;


        if (settings.enabled == false)
        {
            currentSettings =
                null;

            gameObject.SetActive(
                false
            );

            return;
        }


        currentSettings =
            settings;


        gameObject.SetActive(
            true
        );


        ResolveCamera();

        EnsureRuntimeStates();


        if (Application.isPlaying)
        {
            RefreshExternalAvoidanceLights();

            ScheduleNextExternalLightRefresh();

            InitializeRuntimeFireflies();
        }
        else
        {
            ApplyEditorPreview();
        }
    }


    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }


        // <변경부분>
        // Inspector 연결이 없으면
        // 실제 Target Camera에서 PixelCameraController를 찾는다.
        if (pixelCameraController == null &&
            targetCamera != null)
        {
            pixelCameraController =
                targetCamera.GetComponent<
                    PixelCameraController
                >();
        }
    }


    private void EnsureRuntimeStates()
    {
        if (fireflies == null)
        {
            runtimeStates =
                null;

            return;
        }


        if (runtimeStates != null &&
            runtimeStates.Length ==
            fireflies.Length)
        {
            return;
        }


        runtimeStates =
            new FireflyRuntimeState[
                fireflies.Length
            ];


        for (int i = 0;
             i < runtimeStates.Length;
             i++)
        {
            runtimeStates[i] =
                new FireflyRuntimeState();
        }
    }


    private void InitializeRuntimeFireflies()
    {
        if (currentSettings == null ||
            fireflies == null)
        {
            return;
        }


        EnsureRuntimeStates();


        for (int i = 0;
             i < fireflies.Length;
             i++)
        {
            FireflyEnvironmentUnit unit =
                fireflies[i];

            FireflyRuntimeState state =
                runtimeStates[i];


            if (unit == null ||
                unit.root == null ||
                state == null)
            {
                continue;
            }


            ResetMovementState(
                unit,
                state
            );


            bool startsVisible =
                Random.value <=
                currentSettings.startVisibleChance;


            if (startsVisible)
            {
                PlaceInsideCameraView(
                    unit
                );

                state.visibilityState =
                    VisibilityState.Visible;

                state.stateTimer =
                    RandomRangeSafe(
                        currentSettings.visibleDurationMin,
                        currentSettings.visibleDurationMax
                    );

                ApplyVisibility(
                    unit,
                    state,
                    1f
                );
            }
            else
            {
                state.visibilityState =
                    VisibilityState.Hidden;

                state.stateTimer =
                    RandomRangeSafe(
                        0f,
                        currentSettings.hiddenDurationMax
                    );

                ApplyVisibility(
                    unit,
                    state,
                    0f
                );
            }
        }
    }


    private void ApplyEditorPreview()
    {
        if (currentSettings == null ||
            fireflies == null)
        {
            return;
        }


        EnsureRuntimeStates();


        for (int i = 0;
             i < fireflies.Length;
             i++)
        {
            FireflyEnvironmentUnit unit =
                fireflies[i];

            FireflyRuntimeState state =
                runtimeStates[i];


            if (unit == null ||
                state == null)
            {
                continue;
            }


            state.intensityScale =
                1f;


            ApplyVisibility(
                unit,
                state,
                1f
            );
        }
    }


    private void Update()
    {
        if (currentSettings == null ||
            fireflies == null)
        {
            return;
        }


        if (targetCamera == null)
        {
            ResolveCamera();

            if (targetCamera == null)
            {
                return;
            }
        }

        EnsureRuntimeStates();


        float deltaTime =
            Time.deltaTime;


        UpdateExternalAvoidanceLightCache();


        for (int i = 0;
             i < fireflies.Length;
             i++)
        {
            FireflyEnvironmentUnit unit =
                fireflies[i];

            FireflyRuntimeState state =
                runtimeStates[i];


            if (unit == null ||
                unit.root == null ||
                state == null)
            {
                continue;
            }


            UpdateFirefly(
                unit,
                state,
                deltaTime
            );
        }
    }


    private void UpdateFirefly(
        FireflyEnvironmentUnit unit,
        FireflyRuntimeState state,
        float deltaTime)
    {
        switch (state.visibilityState)
        {
            case VisibilityState.Hidden:
                {
                    state.stateTimer -=
                        deltaTime;

                    if (state.stateTimer <= 0f)
                    {
                        BeginFadeIn(
                            unit,
                            state
                        );
                    }

                    break;
                }


            case VisibilityState.FadeIn:
                {
                    UpdateMovement(
                        unit,
                        state,
                        deltaTime
                    );


                    state.stateTimer -=
                        deltaTime;


                    float fadeDuration =
                        GetSafeFadeDuration();


                    float visibility =
                        1f -
                        Mathf.Clamp01(
                            state.stateTimer /
                            fadeDuration
                        );


                    ApplyVisibility(
                        unit,
                        state,
                        visibility
                    );


                    if (state.stateTimer <= 0f)
                    {
                        state.visibilityState =
                            VisibilityState.Visible;

                        state.stateTimer =
                            RandomRangeSafe(
                                currentSettings.visibleDurationMin,
                                currentSettings.visibleDurationMax
                            );

                        ApplyVisibility(
                            unit,
                            state,
                            1f
                        );
                    }

                    break;
                }


            case VisibilityState.Visible:
                {
                    UpdateMovement(
                        unit,
                        state,
                        deltaTime
                    );


                    state.stateTimer -=
                        deltaTime;


                    if (state.stateTimer <= 0f)
                    {
                        BeginFadeOut(
                            state
                        );
                    }

                    break;
                }


            case VisibilityState.FadeOut:
                {
                    UpdateMovement(
                        unit,
                        state,
                        deltaTime
                    );


                    state.stateTimer -=
                        deltaTime;


                    float fadeDuration =
                        GetSafeFadeDuration();


                    float visibility =
                        Mathf.Clamp01(
                            state.stateTimer /
                            fadeDuration
                        );


                    ApplyVisibility(
                        unit,
                        state,
                        visibility
                    );


                    if (state.stateTimer <= 0f)
                    {
                        state.visibilityState =
                            VisibilityState.Hidden;

                        state.stateTimer =
                            RandomRangeSafe(
                                currentSettings.hiddenDurationMin,
                                currentSettings.hiddenDurationMax
                            );

                        ApplyVisibility(
                            unit,
                            state,
                            0f
                        );
                    }

                    break;
                }
        }
    }


    private void BeginFadeIn(
        FireflyEnvironmentUnit unit,
        FireflyRuntimeState state)
    {
        PlaceInsideCameraView(
            unit
        );


        ResetMovementState(
            unit,
            state
        );


        state.visibilityState =
            VisibilityState.FadeIn;

        state.stateTimer =
            GetSafeFadeDuration();


        ApplyVisibility(
            unit,
            state,
            0f
        );
    }


    private void BeginFadeOut(
        FireflyRuntimeState state)
    {
        state.visibilityState =
            VisibilityState.FadeOut;

        state.stateTimer =
            GetSafeFadeDuration();
    }


    private void ResetMovementState(
        FireflyEnvironmentUnit unit,
        FireflyRuntimeState state)
    {
        Vector2 randomDirection =
            GetRandomDirection();


        state.currentDirection =
            randomDirection;

        state.targetDirection =
            GetRandomDirection();


        float speedRandomness =
            Mathf.Clamp01(
                currentSettings.moveSpeedRandomness
            );


        float speedScale =
            Random.Range(
                1f - speedRandomness,
                1f + speedRandomness
            );


        state.moveSpeed =
            currentSettings.moveSpeed *
            speedScale *
            movementSpeedMultiplier *
            Mathf.Max(
                0f,
                unit.moveSpeedMultiplier
            );


        state.directionTimer =
            RandomRangeSafe(
                currentSettings.directionChangeIntervalMin,
                currentSettings.directionChangeIntervalMax
            );


        float intensityRandomness =
            Mathf.Clamp01(
                currentSettings.intensityRandomness
            );


        state.intensityScale =
            Random.Range(
                1f - intensityRandomness,
                1f + intensityRandomness
            );
    }


    private void UpdateMovement(
        FireflyEnvironmentUnit unit,
        FireflyRuntimeState state,
        float deltaTime)
    {
        if (unit.root == null)
        {
            return;
        }


        state.directionTimer -=
            deltaTime;


        if (state.directionTimer <= 0f)
        {
            state.targetDirection =
                GetRandomDirection();

            state.directionTimer =
                RandomRangeSafe(
                    currentSettings.directionChangeIntervalMin,
                    currentSettings.directionChangeIntervalMax
                );
        }


        // 기존 랜덤 이동 방향에
        // 군집 / 외부 광원 회피 방향을 부드럽게 섞는다.
        ApplyAvoidanceSteering(
            unit,
            state
        );


        // 화면 바깥으로 빠져나가는 것을 막는 처리는
        // 항상 마지막 우선순위로 유지한다.
        ApplyScreenEdgeSteering(
            unit,
            state
        );


        float smoothness =
            Mathf.Max(
                0f,
                currentSettings.turnSmoothness
            );


        float interpolation =
            1f -
            Mathf.Exp(
                -smoothness *
                deltaTime
            );


        state.currentDirection =
            Vector2.Lerp(
                state.currentDirection,
                state.targetDirection,
                interpolation
            );


        if (state.currentDirection.sqrMagnitude >
            0.0001f)
        {
            state.currentDirection.Normalize();
        }


        Vector3 movement =
            new Vector3(
                state.currentDirection.x,
                state.currentDirection.y,
                0f
            ) *
            state.moveSpeed *
            deltaTime;


        unit.root.position +=
            movement;
    }


    private void ApplyAvoidanceSteering(
    FireflyEnvironmentUnit unit,
    FireflyRuntimeState state)
    {
        if (currentSettings == null ||
            unit == null ||
            unit.root == null ||
            state == null)
        {
            return;
        }


        Vector2 steeringDirection =
            state.targetDirection.sqrMagnitude >
            0.0001f
                ? state.targetDirection.normalized
                : state.currentDirection;


        if (TryGetClusterAvoidance(
                unit,
                state,
                out Vector2 clusterDirection,
                out float clusterInfluence))
        {
            steeringDirection +=
                clusterDirection *
                currentSettings
                    .clusterAvoidanceStrength *
                clusterInfluence;
        }


        if (TryGetExternalLightAvoidance(
                unit,
                state,
                out Vector2 lightDirection,
                out float lightInfluence))
        {
            steeringDirection +=
                lightDirection *
                currentSettings
                    .externalLightAvoidanceStrength *
                lightInfluence;
        }


        if (steeringDirection.sqrMagnitude <=
            0.0001f)
        {
            return;
        }


        state.targetDirection =
            steeringDirection.normalized;
    }


    private bool TryGetClusterAvoidance(
        FireflyEnvironmentUnit unit,
        FireflyRuntimeState state,
        out Vector2 avoidanceDirection,
        out float influence)
    {
        avoidanceDirection =
            Vector2.zero;

        influence =
            0f;


        if (currentSettings.useClusterAvoidance ==
                false ||
            currentSettings.clusterAvoidanceRadius <=
                0f ||
            fireflies == null ||
            runtimeStates == null)
        {
            return false;
        }


        float radius =
            currentSettings.clusterAvoidanceRadius *
            GetRigWorldScale();

        float radiusSquared =
            radius *
            radius;


        int nearbyCount =
            0;

        Vector2 accumulatedDirection =
            Vector2.zero;


        Vector2 currentPosition =
            new Vector2(
                unit.root.position.x,
                unit.root.position.y
            );


        for (int i = 0;
             i < fireflies.Length;
             i++)
        {
            FireflyEnvironmentUnit otherUnit =
                fireflies[i];


            if (otherUnit == null ||
                otherUnit == unit ||
                otherUnit.root == null ||
                i >= runtimeStates.Length)
            {
                continue;
            }


            FireflyRuntimeState otherState =
                runtimeStates[i];


            // 완전히 꺼진 반딧불은
            // 실제 광량 중첩이 없으므로 제외한다.
            if (otherState == null ||
                otherState.visibilityState ==
                    VisibilityState.Hidden)
            {
                continue;
            }


            Vector2 otherPosition =
                new Vector2(
                    otherUnit.root.position.x,
                    otherUnit.root.position.y
                );


            Vector2 awayVector =
                currentPosition -
                otherPosition;


            float distanceSquared =
                awayVector.sqrMagnitude;


            if (distanceSquared >
                radiusSquared)
            {
                continue;
            }


            nearbyCount++;


            if (awayVector.sqrMagnitude >
                0.0001f)
            {
                float distance =
                    Mathf.Sqrt(
                        distanceSquared
                    );


                float proximity =
                    1f -
                    Mathf.Clamp01(
                        distance /
                        radius
                    );


                accumulatedDirection +=
                    awayVector.normalized *
                    Mathf.Lerp(
                        0.35f,
                        1f,
                        proximity
                    );
            }
        }


        int allowedClusterCount =
            Mathf.Max(
                2,
                currentSettings
                    .maxFirefliesInCluster
            );


        // 기본값 2:
        //
        // 주변에 한 마리
        // → 자기 포함 2마리
        // → 허용.
        //
        // 주변에 두 마리
        // → 자기 포함 3마리
        // → 회피 시작.
        if (nearbyCount <
            allowedClusterCount)
        {
            return false;
        }


        if (accumulatedDirection.sqrMagnitude <=
            0.0001f)
        {
            Vector2 fallbackDirection =
                state.currentDirection
                    .sqrMagnitude >
                0.0001f
                    ? state.currentDirection
                    : Vector2.right;


            accumulatedDirection =
                new Vector2(
                    -fallbackDirection.y,
                    fallbackDirection.x
                );
        }


        avoidanceDirection =
            accumulatedDirection.normalized;


        influence =
            Mathf.Clamp(
                1f +
                Mathf.Max(
                    0,
                    nearbyCount -
                    allowedClusterCount
                ) *
                0.25f,
                1f,
                1.5f
            );


        return true;
    }


    private bool TryGetExternalLightAvoidance(
        FireflyEnvironmentUnit unit,
        FireflyRuntimeState state,
        out Vector2 avoidanceDirection,
        out float influence)
    {
        avoidanceDirection =
            Vector2.zero;

        influence =
            0f;


        if (currentSettings.useExternalLightAvoidance ==
                false ||
            externalAvoidanceLights.Count == 0)
        {
            return false;
        }


        Vector2 currentPosition =
            new Vector2(
                unit.root.position.x,
                unit.root.position.y
            );


        Vector2 accumulatedDirection =
            Vector2.zero;

        float strongestProximity =
            0f;

        bool foundLight =
            false;


        float radiusMultiplier =
            Mathf.Max(
                0f,
                currentSettings
                    .externalLightAvoidanceRadiusMultiplier
            );


        for (int i = 0;
             i < externalAvoidanceLights.Count;
             i++)
        {
            Light2D light =
                externalAvoidanceLights[i];


            if (light == null ||
                light.isActiveAndEnabled == false ||
                light.intensity <= 0.001f)
            {
                continue;
            }


            float avoidanceRadius =
                light.pointLightOuterRadius *
                radiusMultiplier;


            if (avoidanceRadius <=
                0.0001f)
            {
                continue;
            }


            Vector2 lightPosition =
                new Vector2(
                    light.transform.position.x,
                    light.transform.position.y
                );


            Vector2 awayVector =
                currentPosition -
                lightPosition;


            float distance =
                awayVector.magnitude;


            if (distance >
                avoidanceRadius)
            {
                continue;
            }


            foundLight =
                true;


            float proximity =
                1f -
                Mathf.Clamp01(
                    distance /
                    avoidanceRadius
                );


            strongestProximity =
                Mathf.Max(
                    strongestProximity,
                    proximity
                );


            if (awayVector.sqrMagnitude >
                0.0001f)
            {
                accumulatedDirection +=
                    awayVector.normalized *
                    Mathf.Lerp(
                        0.3f,
                        1f,
                        proximity
                    );
            }
        }


        if (foundLight == false)
        {
            return false;
        }


        if (accumulatedDirection.sqrMagnitude <=
            0.0001f)
        {
            Vector2 fallbackDirection =
                state.currentDirection
                    .sqrMagnitude >
                0.0001f
                    ? state.currentDirection
                    : Vector2.right;


            accumulatedDirection =
                new Vector2(
                    -fallbackDirection.y,
                    fallbackDirection.x
                );
        }


        avoidanceDirection =
            accumulatedDirection.normalized;


        influence =
            Mathf.Lerp(
                0.45f,
                1f,
                strongestProximity
            );


        return true;
    }


    private void UpdateExternalAvoidanceLightCache()
    {
        if (currentSettings == null ||
            currentSettings.useExternalLightAvoidance ==
                false)
        {
            return;
        }


        if (Time.unscaledTime <
            nextExternalLightRefreshTime)
        {
            return;
        }


        RefreshExternalAvoidanceLights();

        ScheduleNextExternalLightRefresh();
    }


    private void ScheduleNextExternalLightRefresh()
    {
        if (currentSettings == null)
        {
            return;
        }


        nextExternalLightRefreshTime =
            Time.unscaledTime +
            Mathf.Max(
                0.1f,
                currentSettings
                    .externalLightRefreshInterval
            );
    }


    private void RefreshExternalAvoidanceLights()
    {
        externalAvoidanceLights.Clear();


        if (currentSettings == null ||
            currentSettings.useExternalLightAvoidance ==
                false)
        {
            return;
        }


        FlameLightFlickerController[]
            lightSources =
                FindObjectsByType<
                    FlameLightFlickerController
                >(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );


        for (int i = 0;
             i < lightSources.Length;
             i++)
        {
            FlameLightFlickerController source =
                lightSources[i];


            if (source == null)
            {
                continue;
            }


            Light2D[] lights =
                source
                    .GetComponentsInChildren<
                        Light2D
                    >(
                        true
                    );


            Light2D representativeLight =
                null;

            float largestRadius =
                -1f;


            // 한 랜턴 Prefab 안에 Main / Glow Light가
            // 여러 개 있더라도 하나의 광원 오브젝트로 취급한다.
            //
            // 가장 범위가 큰 Point Light 하나만 대표로 사용한다.
            for (int lightIndex = 0;
                 lightIndex < lights.Length;
                 lightIndex++)
            {
                Light2D light =
                    lights[lightIndex];


                if (light == null ||
                    light.lightType !=
                        Light2D.LightType.Point ||
                    light.isActiveAndEnabled == false ||
                    IsFireflyOwnedLight(
                        light
                    ))
                {
                    continue;
                }


                if (light.pointLightOuterRadius <=
                    largestRadius)
                {
                    continue;
                }


                largestRadius =
                    light.pointLightOuterRadius;

                representativeLight =
                    light;
            }


            if (representativeLight == null ||
                externalAvoidanceLights.Contains(
                    representativeLight))
            {
                continue;
            }


            externalAvoidanceLights.Add(
                representativeLight
            );
        }
    }


    private bool IsFireflyOwnedLight(
        Light2D targetLight)
    {
        if (targetLight == null ||
            fireflies == null)
        {
            return false;
        }


        for (int i = 0;
             i < fireflies.Length;
             i++)
        {
            FireflyEnvironmentUnit unit =
                fireflies[i];


            if (unit == null)
            {
                continue;
            }


            if (unit.spotLight ==
                    targetLight ||
                unit.spriteLight ==
                    targetLight)
            {
                return true;
            }
        }


        return false;
    }


    private float GetRigWorldScale()
    {
        Vector3 lossyScale =
            transform.lossyScale;


        float scaleX =
            Mathf.Abs(
                lossyScale.x
            );

        float scaleY =
            Mathf.Abs(
                lossyScale.y
            );


        return Mathf.Max(
            0.01f,
            (
                scaleX +
                scaleY
            ) *
            0.5f
        );
    }

    // <변경부분>
    // 현재 Zoom In 화면이 아니라
    // "최대 Zoom Out에서 볼 수 있는 전체 월드 영역"을
    // 현재 WorldRoot Scale 기준의 World 좌표로 계산한다.
    //
    // 예:
    // Min Scale = 1
    // Current Scale = 2
    //
    // 최대 Zoom Out에서 보이던 월드 영역 역시
    // 현재 World 좌표에서는 두 배 크기로 확대되어 있으므로
    // Camera View 크기에 Zoom Ratio를 곱해서 계산한다.
    private bool TryGetMaxZoomOutWorldArea(
        out Vector2 areaMin,
        out Vector2 areaMax,
        out Vector2 areaCenter)
    {
        areaMin =
            Vector2.zero;

        areaMax =
            Vector2.zero;

        areaCenter =
            Vector2.zero;


        if (targetCamera == null)
        {
            return false;
        }


        float zoomRatio =
            1f;


        if (pixelCameraController != null)
        {
            float safeMinWorldScale =
                Mathf.Max(
                    0.0001f,
                    pixelCameraController
                        .MinWorldScale
                );

            float safeCurrentWorldScale =
                Mathf.Max(
                    safeMinWorldScale,
                    pixelCameraController
                        .CurrentWorldScale
                );


            zoomRatio =
                safeCurrentWorldScale /
                safeMinWorldScale;


            Vector2 baseCenter =
                pixelCameraController
                    .startPosition;


            // WorldRoot가 Camera 중심과 다른 Pivot을 사용하는 경우에도
            // Scale에 의해 전체 월드 중심이 이동하는 양을 함께 반영한다.
            Transform worldRoot =
                pixelCameraController
                    .WorldRoot;

            if (worldRoot != null)
            {
                Vector2 rootPivot =
                    new Vector2(
                        worldRoot.position.x,
                        worldRoot.position.y
                    );

                areaCenter =
                    rootPivot +
                    (
                        baseCenter -
                        rootPivot
                    ) *
                    zoomRatio;
            }
            else
            {
                areaCenter =
                    baseCenter;
            }
        }
        else
        {
            // PixelCameraController가 없는 예외 Scene에서는
            // 기존 현재 Camera 영역을 fallback으로 사용한다.
            areaCenter =
                new Vector2(
                    targetCamera.transform.position.x,
                    targetCamera.transform.position.y
                );
        }


        float halfHeight =
            Mathf.Max(
                0.0001f,
                targetCamera.orthographicSize
            ) *
            zoomRatio;

        float halfWidth =
            halfHeight *
            Mathf.Max(
                0.0001f,
                targetCamera.aspect
            );


        areaMin =
            new Vector2(
                areaCenter.x -
                    halfWidth,

                areaCenter.y -
                    halfHeight
            );

        areaMax =
            new Vector2(
                areaCenter.x +
                    halfWidth,

                areaCenter.y +
                    halfHeight
            );


        return true;
    }

    private void ApplyScreenEdgeSteering(
      FireflyEnvironmentUnit unit,
      FireflyRuntimeState state)
    {
        if (targetCamera == null ||
            unit.root == null ||
            currentSettings == null)
        {
            return;
        }


        // <변경부분>
        // 현재 Camera Viewport가 아니라
        // 최대 Zoom Out에서 보이던 전체 월드 영역을 사용한다.
        if (TryGetMaxZoomOutWorldArea(
                out Vector2 areaMin,
                out Vector2 areaMax,
                out Vector2 areaCenter) ==
            false)
        {
            return;
        }


        float margin =
            Mathf.Clamp(
                currentSettings.edgeSteerViewportMargin,
                0f,
                0.45f
            );


        float areaWidth =
            areaMax.x -
            areaMin.x;

        float areaHeight =
            areaMax.y -
            areaMin.y;


        float safeMinX =
            areaMin.x +
            areaWidth *
            margin;

        float safeMaxX =
            areaMax.x -
            areaWidth *
            margin;

        float safeMinY =
            areaMin.y +
            areaHeight *
            margin;

        float safeMaxY =
            areaMax.y -
            areaHeight *
            margin;


        Vector3 fireflyPosition =
            unit.root.position;


        bool nearEdge =
            fireflyPosition.x <
                safeMinX ||
            fireflyPosition.x >
                safeMaxX ||
            fireflyPosition.y <
                safeMinY ||
            fireflyPosition.y >
                safeMaxY;


        if (nearEdge == false)
        {
            return;
        }


        Vector2 directionToCenter =
            new Vector2(
                areaCenter.x -
                fireflyPosition.x,

                areaCenter.y -
                fireflyPosition.y
            );


        if (directionToCenter.sqrMagnitude <=
            0.0001f)
        {
            return;
        }


        state.targetDirection =
            directionToCenter.normalized;
    }


    private void PlaceInsideCameraView(
     FireflyEnvironmentUnit unit)
    {
        if (unit == null ||
            unit.root == null ||
            targetCamera == null ||
            currentSettings == null)
        {
            return;
        }


        float padding =
            Mathf.Clamp(
                currentSettings.spawnViewportPadding,
                0f,
                0.45f
            );


        float worldZ =
            unit.root.position.z;


        // <변경부분>
        // 현재 확대된 Camera 화면이 아니라
        // 최대 Zoom Out 전체 월드 영역을 Spawn 범위로 사용한다.
        if (TryGetMaxZoomOutWorldArea(
                out Vector2 areaMin,
                out Vector2 areaMax,
                out Vector2 areaCenter) ==
            false)
        {
            return;
        }


        float areaWidth =
            areaMax.x -
            areaMin.x;

        float areaHeight =
            areaMax.y -
            areaMin.y;


        float spawnMinX =
            areaMin.x +
            areaWidth *
            padding;

        float spawnMaxX =
            areaMax.x -
            areaWidth *
            padding;

        float spawnMinY =
            areaMin.y +
            areaHeight *
            padding;

        float spawnMaxY =
            areaMax.y -
            areaHeight *
            padding;


        // 반딧불 재등장 위치를 찾을 최대 시도 횟수.
        //
        // 외부 광원이 화면 대부분을 덮는 극단적인 상황에서도
        // 무한 반복하지 않도록 횟수를 제한한다.
        const int maximumSpawnAttempts =
            16;


        Vector3 bestPosition =
            unit.root.position;

        float bestClearance =
            float.NegativeInfinity;


        for (int attemptIndex = 0;
             attemptIndex < maximumSpawnAttempts;
             attemptIndex++)
        {
            // <변경부분>
            // 현재 Camera Viewport 좌표가 아니라
            // 최대 Zoom Out 전체 월드 범위에서 직접 후보를 뽑는다.
            Vector3 candidatePosition =
                new Vector3(
                    Random.Range(
                        spawnMinX,
                        spawnMaxX
                    ),

                    Random.Range(
                        spawnMinY,
                        spawnMaxY
                    ),

                    worldZ
                );


            // 현재 후보 위치가
            // 가장 가까운 외부 광원의 생성 금지 범위로부터
            // 얼마나 여유가 있는지 계산한다.
            //
            // 0 이상:
            // 외부 광원 회피 범위 밖의 안전한 위치.
            //
            // 0 미만:
            // 외부 광원 회피 범위 안쪽.
            float clearance =
                GetExternalLightSpawnClearance(
                    candidatePosition
                );


            // 안전한 후보를 찾았다면
            // 더 이상 탐색하지 않고 즉시 해당 위치를 사용한다.
            if (clearance >= 0f)
            {
                unit.root.position =
                    candidatePosition;

                return;
            }


            // 모든 후보가 외부 광원과 겹치는 상황에 대비하여
            // 그중 가장 덜 겹치는 위치를 fallback으로 기억한다.
            if (clearance >
                bestClearance)
            {
                bestClearance =
                    clearance;

                bestPosition =
                    candidatePosition;
            }
        }


        // 최대 횟수 안에 완전히 안전한 위치를 찾지 못했다면
        // 외부 광원으로부터 가장 여유가 컸던 후보를 사용한다.
        unit.root.position =
            bestPosition;
    }


    // 반딧불 재등장 후보 위치와
    // 현재 화면에 활성화되어 있는 다른 반딧불 사이의
    // 최소 생성 여유 거리를 계산한다.
    //
    // 양수:
    // 모든 활성 반딧불의 생성 금지 범위 밖.
    //
    // 음수:
    // 하나 이상의 반딧불 생성 금지 범위 안.
    //
    // 완전히 Hidden 상태인 반딧불은 현재 화면에서 빛나지 않으며,
    // 다음 등장 때 다시 새 위치를 결정하므로 판정에서 제외한다.
    private float GetFireflySpawnClearance(
        FireflyEnvironmentUnit spawningUnit,
        Vector3 candidatePosition)
    {
        if (currentSettings == null ||
            currentSettings.useClusterAvoidance ==
                false ||
            fireflies == null ||
            runtimeStates == null)
        {
            return float.PositiveInfinity;
        }


        float rigWorldScale =
            GetRigWorldScale();


        // 이동 중 사용하는 Cluster Avoidance Radius에
        // 재등장 전용 Padding을 추가한다.
        float spawnExclusionRadius =
            (
                Mathf.Max(
                    0f,
                    currentSettings.clusterAvoidanceRadius
                ) +
                Mathf.Max(
                    0f,
                    currentSettings.clusterSpawnPadding
                )
            ) *
            rigWorldScale;


        if (spawnExclusionRadius <=
            0.0001f)
        {
            return float.PositiveInfinity;
        }


        Vector2 candidatePosition2D =
            new Vector2(
                candidatePosition.x,
                candidatePosition.y
            );


        float minimumClearance =
            float.PositiveInfinity;

        bool foundActiveFirefly =
            false;


        for (int i = 0;
             i < fireflies.Length;
             i++)
        {
            FireflyEnvironmentUnit otherUnit =
                fireflies[i];


            if (otherUnit == null ||
                otherUnit == spawningUnit ||
                otherUnit.root == null ||
                i >= runtimeStates.Length)
            {
                continue;
            }


            FireflyRuntimeState otherState =
                runtimeStates[i];


            // 완전히 숨겨져 있는 반딧불은
            // 실제 화면에 존재하지 않는 것으로 취급한다.
            //
            // FadeIn / Visible / FadeOut 상태는
            // 모두 현재 화면에서 광원이 보일 수 있으므로
            // 생성 거리 판정에 포함한다.
            if (otherState == null ||
                otherState.visibilityState ==
                    VisibilityState.Hidden)
            {
                continue;
            }


            Vector2 otherPosition =
                new Vector2(
                    otherUnit.root.position.x,
                    otherUnit.root.position.y
                );


            float distance =
                Vector2.Distance(
                    candidatePosition2D,
                    otherPosition
                );


            float clearance =
                distance -
                spawnExclusionRadius;


            minimumClearance =
                Mathf.Min(
                    minimumClearance,
                    clearance
                );


            foundActiveFirefly =
                true;
        }


        if (foundActiveFirefly == false)
        {
            return float.PositiveInfinity;
        }


        return minimumClearance;
    }


    // 반딧불 재등장 후보 위치와
    // 외부 광원 생성 금지 범위 사이의 최소 여유 거리를 계산한다.
    //
    // 양수:
    // 모든 외부 광원 범위 밖.
    //
    // 음수:
    // 하나 이상의 외부 광원 범위 안.
    //
    // 회피할 외부 광원이 없다면 PositiveInfinity를 반환하여
    // 기존처럼 첫 랜덤 위치를 바로 사용할 수 있게 한다.
    private float GetExternalLightSpawnClearance(
        Vector3 candidatePosition)
    {
        if (currentSettings == null ||
            currentSettings.useExternalLightAvoidance ==
                false ||
            externalAvoidanceLights.Count == 0)
        {
            return float.PositiveInfinity;
        }


        float radiusMultiplier =
            Mathf.Max(
                0f,
                currentSettings
                    .externalLightAvoidanceRadiusMultiplier
            );


        // Spawn Padding은 WorldRoot Scale 1 기준의 거리값으로 취급한다.
        //
        // 현재 Firefly Rig의 실제 World Scale을 반영하여
        // 카메라 Zoom 상태가 달라져도 비슷한 시각적 여유를 유지한다.
        float spawnPadding =
            Mathf.Max(
                0f,
                currentSettings
                    .externalLightSpawnPadding
            ) *
            GetRigWorldScale();


        Vector2 candidatePosition2D =
            new Vector2(
                candidatePosition.x,
                candidatePosition.y
            );


        float minimumClearance =
            float.PositiveInfinity;

        bool foundValidLight =
            false;


        for (int i = 0;
             i < externalAvoidanceLights.Count;
             i++)
        {
            Light2D light =
                externalAvoidanceLights[i];


            if (light == null ||
                light.isActiveAndEnabled == false ||
                light.intensity <= 0.001f ||
                light.pointLightOuterRadius <= 0.0001f)
            {
                continue;
            }


            float avoidanceRadius =
                light.pointLightOuterRadius *
                radiusMultiplier;


            // 이동 중 회피하는 기존 범위보다
            // Spawn Padding만큼 더 떨어진 곳에서 등장하도록 한다.
            float spawnExclusionRadius =
                avoidanceRadius +
                spawnPadding;


            if (spawnExclusionRadius <=
                0.0001f)
            {
                continue;
            }


            Vector2 lightPosition =
                new Vector2(
                    light.transform.position.x,
                    light.transform.position.y
                );


            float distance =
                Vector2.Distance(
                    candidatePosition2D,
                    lightPosition
                );


            float clearance =
                distance -
                spawnExclusionRadius;


            minimumClearance =
                Mathf.Min(
                    minimumClearance,
                    clearance
                );


            foundValidLight =
                true;
        }


        if (foundValidLight == false)
        {
            return float.PositiveInfinity;
        }


        return minimumClearance;
    }


    private Vector3 GetCameraCenterAtWorldZ(
        float worldZ)
    {
        float cameraDistance =
            Mathf.Abs(
                worldZ -
                targetCamera.transform.position.z
            );


        Vector3 center =
            targetCamera.ViewportToWorldPoint(
                new Vector3(
                    0.5f,
                    0.5f,
                    cameraDistance
                )
            );


        center.z =
            worldZ;


        return center;
    }


    private void ApplyVisibility(
        FireflyEnvironmentUnit unit,
        FireflyRuntimeState state,
        float visibility)
    {
        if (currentSettings == null)
        {
            return;
        }


        float safeVisibility =
            Mathf.Clamp01(
                visibility
            );


        if (unit.spriteRenderer != null)
        {
            Color spriteColor =
                currentSettings.spriteColor;

            spriteColor.a *=
                safeVisibility;

            unit.spriteRenderer.color =
                spriteColor;
        }


        if (unit.spotLight != null)
        {
            unit.spotLight.color =
                currentSettings.spotLightColor;

            unit.spotLight.intensity =
                currentSettings.spotLightIntensity *
                state.intensityScale *
                spotLightIntensityMultiplier *
                Mathf.Max(
                    0f,
                    unit.spotLightIntensityMultiplier
                ) *
                safeVisibility;
        }


        if (unit.spriteLight != null)
        {
            unit.spriteLight.color =
                currentSettings.spriteLightColor;

            unit.spriteLight.intensity =
                currentSettings.spriteLightIntensity *
                state.intensityScale *
                spriteLightIntensityMultiplier *
                Mathf.Max(
                    0f,
                    unit.spriteLightIntensityMultiplier
                ) *
                safeVisibility;
        }
    }


    private Vector2 GetRandomDirection()
    {
        Vector2 direction =
            Random.insideUnitCircle;


        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return Vector2.right;
        }


        return direction.normalized;
    }


    private float GetSafeFadeDuration()
    {
        return Mathf.Max(
            0.0001f,
            currentSettings.fadeDuration *
            fadeDurationMultiplier
        );
    }


    private float RandomRangeSafe(
        float valueA,
        float valueB)
    {
        float minimum =
            Mathf.Min(
                valueA,
                valueB
            );

        float maximum =
            Mathf.Max(
                valueA,
                valueB
            );


        return Random.Range(
            minimum,
            maximum
        );
    }
}
