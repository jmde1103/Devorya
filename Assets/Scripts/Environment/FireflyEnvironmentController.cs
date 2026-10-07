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
        if (targetCamera != null)
        {
            return;
        }


        targetCamera =
            Camera.main;
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

    private void ApplyScreenEdgeSteering(
        FireflyEnvironmentUnit unit,
        FireflyRuntimeState state)
    {
        if (targetCamera == null ||
            unit.root == null)
        {
            return;
        }


        Vector3 viewportPosition =
            targetCamera.WorldToViewportPoint(
                unit.root.position
            );


        float margin =
            Mathf.Clamp(
                currentSettings.edgeSteerViewportMargin,
                0f,
                0.45f
            );


        bool nearEdge =
            viewportPosition.x < margin ||
            viewportPosition.x > 1f - margin ||
            viewportPosition.y < margin ||
            viewportPosition.y > 1f - margin;


        if (!nearEdge)
        {
            return;
        }


        Vector3 cameraCenter =
            GetCameraCenterAtWorldZ(
                unit.root.position.z
            );


        Vector2 directionToCenter =
            new Vector2(
                cameraCenter.x -
                unit.root.position.x,

                cameraCenter.y -
                unit.root.position.y
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
            targetCamera == null)
        {
            return;
        }


        float padding =
            Mathf.Clamp(
                currentSettings.spawnViewportPadding,
                0f,
                0.45f
            );


        float viewportX =
            Random.Range(
                padding,
                1f - padding
            );

        float viewportY =
            Random.Range(
                padding,
                1f - padding
            );


        float worldZ =
            unit.root.position.z;


        float cameraDistance =
            Mathf.Abs(
                worldZ -
                targetCamera.transform.position.z
            );


        Vector3 worldPosition =
            targetCamera.ViewportToWorldPoint(
                new Vector3(
                    viewportX,
                    viewportY,
                    cameraDistance
                )
            );


        worldPosition.z =
            worldZ;


        unit.root.position =
            worldPosition;
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
