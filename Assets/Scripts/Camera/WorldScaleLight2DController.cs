using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


public class WorldScaleLight2DController : MonoBehaviour
{
    private class LightState
    {
        public Light2D light;

        public float baseInnerRadius;

        public float baseOuterRadius;


        public LightState(
            Light2D targetLight)
        {
            light =
                targetLight;

            baseInnerRadius =
                targetLight.pointLightInnerRadius;

            baseOuterRadius =
                targetLight.pointLightOuterRadius;
        }
    }


    [Header("Camera Zoom")]
    [SerializeField]
    private PixelCameraController pixelCameraController;


    [Header("World Root")]

    // 비워두면 이 컴포넌트가 붙어 있는
    // Transform을 WorldRoot로 사용한다.
    [SerializeField]
    private Transform worldRoot;


    [Header("Zoom Compensation")]

    // Light2D Inspector에서 작성한 Radius의
    // 기준 WorldRoot Scale.
    //
    // 현재 DEVORYA 기본값은 1.
    [SerializeField, Min(0.01f)]
    private float referenceWorldScale =
        1f;


    [Header("Runtime Light Detection")]

    // Decoration / Event 등에서
    // Runtime에 새 Light2D가 생성되는 상황도
    // 자동으로 감지한다.
    [SerializeField]
    private bool autoRefreshLights =
        true;


    // 매 Frame 전체 Hierarchy를 검색하지 않고
    // 이 간격으로만 신규 Light를 확인한다.
    [SerializeField, Min(0.1f)]
    private float lightRefreshInterval =
        1f;


    [Header("Runtime")]
    [SerializeField]
    private bool restoreRadiusOnDisable =
        true;


    private readonly List<LightState>
        lightStates =
            new List<LightState>(32);


    private float lastAppliedWorldScale =
        -1f;

    private float nextRefreshTime =
        0f;


    private bool hasWarnedMissingCamera;


    private void Awake()
    {
        ResolveReferences();

        RefreshLights();
    }


    private void OnEnable()
    {
        ResolveReferences();

        RefreshLights();

        nextRefreshTime =
            Time.unscaledTime +
            lightRefreshInterval;

        ApplyCurrentWorldScale();
    }


    private void LateUpdate()
    {
        if (pixelCameraController == null)
        {
            ResolveReferences();

            if (pixelCameraController == null)
            {
                return;
            }
        }


        // =====================================================
        // Runtime에 새로 생성된 Light 확인
        // =====================================================

        if (autoRefreshLights &&
            Time.unscaledTime >=
            nextRefreshTime)
        {
            RefreshLights();

            nextRefreshTime =
                Time.unscaledTime +
                lightRefreshInterval;
        }


        // =====================================================
        // Zoom 변경 확인
        // =====================================================

        float currentWorldScale =
            pixelCameraController
                .CurrentWorldScale;


        if (Mathf.Approximately(
                currentWorldScale,
                lastAppliedWorldScale))
        {
            return;
        }


        ApplyCurrentWorldScale();
    }


    private void OnDisable()
    {
        if (restoreRadiusOnDisable)
        {
            RestoreBaseRadii();
        }


        lastAppliedWorldScale =
            -1f;
    }


    private void ResolveReferences()
    {
        if (worldRoot == null)
        {
            worldRoot =
                transform;
        }


        if (pixelCameraController ==
            null)
        {
            pixelCameraController =
                FindFirstObjectByType<
                    PixelCameraController
                >();
        }


        if (pixelCameraController == null &&
            hasWarnedMissingCamera == false)
        {
            hasWarnedMissingCamera =
                true;


            Debug.LogWarning(
                "[WorldScaleLight2DController] " +
                "PixelCameraController를 찾을 수 없습니다.",
                this
            );
        }
    }


    // =========================================================
    // Light 자동 검색
    // =========================================================

    public void RefreshLights()
    {
        if (worldRoot == null)
        {
            return;
        }


        Light2D[] foundLights =
            worldRoot
                .GetComponentsInChildren<
                    Light2D
                >(
                    true
                );


        // =====================================================
        // 파괴된 Light 제거
        // =====================================================

        for (int i =
                 lightStates.Count - 1;
             i >= 0;
             i--)
        {
            LightState state =
                lightStates[i];


            if (state == null ||
                state.light == null)
            {
                lightStates.RemoveAt(
                    i
                );
            }
        }


        // =====================================================
        // 신규 Point / Spot Light 등록
        // =====================================================

        for (int i = 0;
             i < foundLights.Length;
             i++)
        {
            Light2D light =
                foundLights[i];


            if (light == null)
            {
                continue;
            }


            // DEVORYA에서 사용하는
            // Point / Spot Light만 Radius 보정.
            //
            // Global / Sprite / Freeform은 제외한다.
            if (light.lightType !=
                Light2D.LightType.Point)
            {
                continue;
            }


            if (ContainsLight(
                    light))
            {
                continue;
            }


            lightStates.Add(
                new LightState(
                    light
                )
            );
        }


        // 새 Light가 추가된 직후에도
        // 현재 Zoom 상태를 바로 적용한다.
        ApplyCurrentWorldScale();
    }


    private bool ContainsLight(
        Light2D targetLight)
    {
        for (int i = 0;
             i < lightStates.Count;
             i++)
        {
            LightState state =
                lightStates[i];


            if (state != null &&
                state.light ==
                targetLight)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // Zoom Compensation
    // =========================================================

    private void ApplyCurrentWorldScale()
    {
        if (pixelCameraController == null)
        {
            return;
        }


        float currentWorldScale =
            Mathf.Max(
                0.01f,
                pixelCameraController
                    .CurrentWorldScale
            );


        float safeReferenceScale =
            Mathf.Max(
                0.01f,
                referenceWorldScale
            );


        float radiusScale =
            currentWorldScale /
            safeReferenceScale;


        for (int i = 0;
             i < lightStates.Count;
             i++)
        {
            LightState state =
                lightStates[i];


            if (state == null ||
                state.light == null)
            {
                continue;
            }


            state.light
                .pointLightInnerRadius =
                    state.baseInnerRadius *
                    radiusScale;


            state.light
                .pointLightOuterRadius =
                    state.baseOuterRadius *
                    radiusScale;
        }


        lastAppliedWorldScale =
            currentWorldScale;
    }


    private void RestoreBaseRadii()
    {
        for (int i = 0;
             i < lightStates.Count;
             i++)
        {
            LightState state =
                lightStates[i];


            if (state == null ||
                state.light == null)
            {
                continue;
            }


            state.light
                .pointLightInnerRadius =
                    state.baseInnerRadius;


            state.light
                .pointLightOuterRadius =
                    state.baseOuterRadius;
        }
    }
}