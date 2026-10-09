using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// <변경부분>
// 상점 PriceText 전용 시각 연출.
//
// 역할:
// 1. World Space TextMeshPro의 Sorting Layer / Order 설정
// 2. TMP SDF Material의 Glow 적용
// 3. 평상시 미세한 전압 흔들림
// 4. 일정 간격마다 짧게 발생하는 기계식 Flicker
//
// 가격 숫자의 실제 값 변경은 ShopItemDisplay가 담당하고,
// 이 스크립트는 표시 연출만 담당한다.
[RequireComponent(typeof(TextMeshPro))]
public class ShopPriceTextFlicker : MonoBehaviour
{
    [Header("Renderer Sorting")]

    // PriceFrame과 동일한 Sorting Layer를 사용하고
    // Sorting Order만 PriceFrame보다 높게 설정하는 것을 권장한다.
    [SerializeField]
    private string sortingLayerName =
        "Default";

    [SerializeField]
    private int sortingOrder =
        30;


    [Header("Glow")]

    // TMP SDF Shader의 Glow 기능 사용 여부.
    [SerializeField]
    private bool useGlow =
        true;

    // 기존 TMP Text 색상을 그대로 Glow 색상으로 사용하고
    // 이 값으로 Glow 투명도만 조절한다.
    [SerializeField, Range(0f, 1f)]
    private float glowAlpha =
        0.55f;

    [SerializeField, Range(-1f, 1f)]
    private float glowOffset =
        0f;

    [SerializeField, Range(0f, 1f)]
    private float glowInner =
        0.05f;

    [SerializeField, Range(0f, 1f)]
    private float glowOuter =
        0.2f;

    [SerializeField, Range(0.01f, 1f)]
    private float glowPower =
       0.75f;


    [Header("Scene Light")]

    // <변경부분>
    // TMP Material Glow와 별개로
    // 실제 주변 Sprite를 비출 Light2D.
    //
    // PriceText 근처에 작은 Spot Light 2D를 배치하고 연결한다.
    [SerializeField]
    private Light2D sceneGlowLight;

    // <변경부분>
    // Text Flicker와 실제 Light2D 밝기를
    // 동일한 Intensity 비율로 동기화할지 결정한다.
    [SerializeField]
    private bool syncSceneLightWithFlicker =
        true;

    // <변경부분>
    // Light2D Inspector에서 설정한 원래 Intensity에
    // 추가로 곱할 배율.
    [SerializeField, Min(0f)]
    private float sceneLightIntensityMultiplier =
        1f;


    [Header("Idle Flicker")]

    // 평상시에도 완전히 고정된 조명이 아니라
    // 오래된 전기 장치처럼 아주 조금씩 밝기가 흔들린다.
    [SerializeField, Range(0f, 0.3f)]
    private float idleFlickerAmount =
        0.05f;

    [SerializeField, Min(0.01f)]
    private float idleFlickerSpeed =
        2.5f;


    [Header("Mechanical Flicker")]

    // 큰 Flicker가 다시 발생하기까지의 최소 / 최대 시간.
    [SerializeField, Min(0.1f)]
    private float flickerIntervalMin =
        3.5f;

    [SerializeField, Min(0.1f)]
    private float flickerIntervalMax =
        7f;

    // Flicker 순간 숫자가 얼마나 어두워질지 설정한다.
    //
    // 0 = 완전히 꺼짐
    // 1 = 변화 없음
    [SerializeField, Range(0f, 1f)]
    private float flickerIntensityMin =
        0.25f;

    [SerializeField, Range(0f, 1f)]
    private float flickerIntensityMax =
        0.55f;

    // 한 번의 Flicker 이벤트 안에서
    // 몇 차례 짧게 깜빡일지 설정한다.
    [SerializeField, Min(1)]
    private int flickerCountMin =
        1;

    [SerializeField, Min(1)]
    private int flickerCountMax =
        3;

    // 한 번 어두워진 상태가 유지되는 시간.
    [SerializeField, Min(0.01f)]
    private float flickerDarkDurationMin =
        0.025f;

    [SerializeField, Min(0.01f)]
    private float flickerDarkDurationMax =
        0.065f;

    // 짧은 Flicker 사이 정상 밝기로 돌아오는 시간.
    [SerializeField, Min(0.01f)]
    private float flickerRecoveryDurationMin =
        0.03f;

    [SerializeField, Min(0.01f)]
    private float flickerRecoveryDurationMax =
        0.09f;


    // 실제 World Space TextMeshPro.
    private TextMeshPro priceText;

    // TextMeshPro가 사용하는 MeshRenderer.
    //
    // 기본 Inspector에서 TMP의 Sorting Layer가 보이지 않는 문제를
    // 이 Renderer를 직접 제어하여 해결한다.
    private Renderer textRenderer;

    // Shared Material을 변경하지 않도록
    // 현재 PriceText 전용 Material Instance를 사용한다.
    private Material runtimeMaterial;

    // Inspector에서 지정한 원래 Text 색상.
    private Color baseTextColor;

    // 현재 Glow의 기본 색상.
    private Color baseGlowColor;

    // <변경부분>
    // Scene Light의 Inspector 기준 원래 Intensity.
    private float baseSceneLightIntensity;

    // 평상시 미세 Flicker가
    // 여러 PriceText에서 완전히 같은 움직임을 하지 않도록
    // 각 인스턴스마다 다른 Noise 위치를 사용한다.
    private float idleNoiseOffset;

    private float nextFlickerTime;

    private Coroutine flickerCoroutine;


    private void Awake()
    {
        CacheComponents();

        CaptureBaseVisualState();

        ApplyRendererSorting();

        CreateRuntimeMaterial();

        ApplyGlowSettings();
    }


    private void OnEnable()
    {
        CacheComponents();

        ApplyRendererSorting();

        if (Application.isPlaying)
        {
            CreateRuntimeMaterial();

            ApplyGlowSettings();

            ScheduleNextFlicker();
        }
    }


    private void OnDisable()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(
                flickerCoroutine
            );

            flickerCoroutine =
                null;
        }

        ApplyVisualIntensity(
            1f
        );
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        // 최소 / 최대 값이 Inspector에서 뒤집히지 않도록 정리한다.
        flickerIntervalMax =
            Mathf.Max(
                flickerIntervalMin,
                flickerIntervalMax
            );

        flickerIntensityMax =
            Mathf.Max(
                flickerIntensityMin,
                flickerIntensityMax
            );

        flickerCountMax =
            Mathf.Max(
                flickerCountMin,
                flickerCountMax
            );

        flickerDarkDurationMax =
            Mathf.Max(
                flickerDarkDurationMin,
                flickerDarkDurationMax
            );

        flickerRecoveryDurationMax =
            Mathf.Max(
                flickerRecoveryDurationMin,
                flickerRecoveryDurationMax
            );

        // Play하지 않아도 Sorting은
        // Scene View에서 바로 확인할 수 있게 적용한다.
        CacheComponents();

        ApplyRendererSorting();
    }
#endif


    private void Update()
    {
        if (priceText == null)
        {
            return;
        }

        // 큰 Flicker Coroutine이 실행 중일 때는
        // 해당 연출이 밝기를 전담한다.
        if (flickerCoroutine != null)
        {
            return;
        }


        if (Time.unscaledTime >=
            nextFlickerTime)
        {
            flickerCoroutine =
                StartCoroutine(
                    MechanicalFlickerRoutine()
                );

            return;
        }


        // <변경부분>
        // 평상시에는 Perlin Noise를 이용해
        // 거의 눈치채지 못할 정도의 밝기 흔들림을 만든다.
        float noiseValue =
            Mathf.PerlinNoise(
                idleNoiseOffset,
                Time.unscaledTime *
                    idleFlickerSpeed
            );

        float intensity =
            1f -
            noiseValue *
            idleFlickerAmount;

        ApplyVisualIntensity(
            intensity
        );
    }


    // 필요한 TMP / Renderer 참조를 가져온다.
    private void CacheComponents()
    {
        if (priceText == null)
        {
            priceText =
                GetComponent<TextMeshPro>();
        }

        if (textRenderer == null)
        {
            textRenderer =
                GetComponent<Renderer>();
        }
    }


    // 원래 Text / Light 상태를 Flicker 기준값으로 저장한다.
    private void CaptureBaseVisualState()
    {
        if (priceText != null)
        {
            baseTextColor =
                priceText.color;
        }
        else
        {
            baseTextColor =
                Color.white;
        }

        idleNoiseOffset =
            Random.Range(
                0f,
                100f
            );

        baseGlowColor =
            new Color(
                baseTextColor.r,
                baseTextColor.g,
                baseTextColor.b,
                glowAlpha
            );

        // <변경부분>
        // 실제 Light2D의 원래 밝기를 기준값으로 저장한다.
        if (sceneGlowLight != null)
        {
            baseSceneLightIntensity =
                sceneGlowLight.intensity;
        }
    }


    // <변경부분>
    // TMP의 MeshRenderer를 직접 제어하여
    // SpriteRenderer의 Sorting Layer와 동일한 방식으로 정렬한다.
    private void ApplyRendererSorting()
    {
        if (textRenderer == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                sortingLayerName) ==
            false)
        {
            textRenderer.sortingLayerName =
                sortingLayerName;
        }

        textRenderer.sortingOrder =
            sortingOrder;
    }


    // Shared TMP Material을 직접 수정하면
    // 같은 Font Material을 사용하는 다른 Text까지 영향을 받는다.
    //
    // 따라서 이 PriceText 전용 Material Instance를 가져온다.
    private void CreateRuntimeMaterial()
    {
        if (Application.isPlaying == false ||
            priceText == null ||
            runtimeMaterial != null)
        {
            return;
        }

        runtimeMaterial =
            priceText.fontMaterial;
    }


    // TMP Distance Field Material의 Glow 설정을 적용한다.
    private void ApplyGlowSettings()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        if (useGlow == false)
        {
            runtimeMaterial.DisableKeyword(
                "GLOW_ON"
            );

            return;
        }

        runtimeMaterial.EnableKeyword(
            "GLOW_ON"
        );

        baseGlowColor =
            new Color(
                baseTextColor.r,
                baseTextColor.g,
                baseTextColor.b,
                glowAlpha
            );


        SetMaterialColorIfAvailable(
            "_GlowColor",
            baseGlowColor
        );

        SetMaterialFloatIfAvailable(
            "_GlowOffset",
            glowOffset
        );

        SetMaterialFloatIfAvailable(
            "_GlowInner",
            glowInner
        );

        SetMaterialFloatIfAvailable(
            "_GlowOuter",
            glowOuter
        );

        SetMaterialFloatIfAvailable(
            "_GlowPower",
            glowPower
        );
    }


    // <변경부분>
    // 일정 간격으로 짧게 전압이 끊기는 듯한
    // 오래된 기계식 Display Flicker를 재생한다.
    private IEnumerator MechanicalFlickerRoutine()
    {
        int flickerCount =
            Random.Range(
                flickerCountMin,
                flickerCountMax + 1
            );

        for (int i = 0;
             i < flickerCount;
             i++)
        {
            float darkIntensity =
                Random.Range(
                    flickerIntensityMin,
                    flickerIntensityMax
                );

            ApplyVisualIntensity(
                darkIntensity
            );

            yield return
                new WaitForSecondsRealtime(
                    Random.Range(
                        flickerDarkDurationMin,
                        flickerDarkDurationMax
                    )
                );


            ApplyVisualIntensity(
                1f
            );

            if (i <
                flickerCount - 1)
            {
                yield return
                    new WaitForSecondsRealtime(
                        Random.Range(
                            flickerRecoveryDurationMin,
                            flickerRecoveryDurationMax
                        )
                    );
            }
        }


        ApplyVisualIntensity(
            1f
        );

        ScheduleNextFlicker();

        flickerCoroutine =
            null;
    }


    // Text 본체 / TMP Glow / 실제 Light2D를
    // 동일한 밝기 비율로 제어하여
    // 오래된 기계식 Display의 전압 흔들림을 표현한다.
    private void ApplyVisualIntensity(
        float intensity)
    {
        intensity =
            Mathf.Clamp01(
                intensity
            );

        if (priceText != null)
        {
            Color textColor =
                baseTextColor;

            textColor.a =
                baseTextColor.a *
                intensity;

            priceText.color =
                textColor;
        }


        if (useGlow &&
            runtimeMaterial != null)
        {
            Color glowColor =
                baseGlowColor;

            glowColor.a =
                baseGlowColor.a *
                intensity;

            SetMaterialColorIfAvailable(
                "_GlowColor",
                glowColor
            );
        }


        // <변경부분>
        // Material Glow와 별개로
        // 실제 Light2D가 주변 환경을 비추게 한다.
        //
        // Flicker 동기화가 켜져 있으면
        // 숫자가 어두워지는 순간 실제 빛도 함께 약해진다.
        if (sceneGlowLight != null)
        {
            float lightIntensityRate =
                syncSceneLightWithFlicker
                    ? intensity
                    : 1f;

            sceneGlowLight.intensity =
                baseSceneLightIntensity *
                sceneLightIntensityMultiplier *
                lightIntensityRate;
        }
    }


    private void ScheduleNextFlicker()
    {
        nextFlickerTime =
            Time.unscaledTime +
            Random.Range(
                flickerIntervalMin,
                flickerIntervalMax
            );
    }


    // 현재 TMP Shader에 해당 Property가 있는 경우에만 적용한다.
    //
    // 다른 TMP Shader를 사용하더라도
    // Missing Property Error가 발생하지 않게 한다.
    private void SetMaterialFloatIfAvailable(
        string propertyName,
        float value)
    {
        if (runtimeMaterial == null ||
            runtimeMaterial.HasProperty(
                propertyName) ==
            false)
        {
            return;
        }

        runtimeMaterial.SetFloat(
            propertyName,
            value
        );
    }


    private void SetMaterialColorIfAvailable(
        string propertyName,
        Color value)
    {
        if (runtimeMaterial == null ||
            runtimeMaterial.HasProperty(
                propertyName) ==
            false)
        {
            return;
        }

        runtimeMaterial.SetColor(
            propertyName,
            value
        );
    }
}
