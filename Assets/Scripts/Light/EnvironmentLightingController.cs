using UnityEngine;
using UnityEngine.Rendering.Universal;


// <변경부분>
// EnvironmentVisualProfile에 저장된 값을
// 실제 EnvironmentLightingRig의 Light2D에 적용한다.
//
// 또한 현재 Scene에서 직접 튜닝한 LightingRig 값을
// EnvironmentVisualProfile로 다시 저장할 수 있도록 한다.
public class EnvironmentLightingController : MonoBehaviour
{
    [Header("Light References")]

    [SerializeField]
    private Light2D globalAmbient;

    [SerializeField]
    private Light2D sunKey;

    [SerializeField]
    private Light2D skyFill;


    // 지정한 EnvironmentVisualProfile의 값을
    // 현재 LightingRig에 적용한다.
    public bool ApplyProfile(
        EnvironmentVisualProfile profile)
    {
        if (profile == null)
        {
            Debug.LogWarning(
                "환경 조명 적용 실패: " +
                "EnvironmentVisualProfile이 없습니다."
            );

            return false;
        }

        bool hasMissingReference =
            false;


        if (globalAmbient != null)
        {
            ApplyGlobalLightSettings(
                globalAmbient,
                profile.globalAmbient
            );
        }
        else
        {
            hasMissingReference =
                true;
        }


        if (sunKey != null)
        {
            ApplyPointLightSettings(
                sunKey,
                profile.sunKey
            );
        }
        else
        {
            hasMissingReference =
                true;
        }


        if (skyFill != null)
        {
            ApplyPointLightSettings(
                skyFill,
                profile.skyFill
            );
        }
        else
        {
            hasMissingReference =
                true;
        }


        if (hasMissingReference)
        {
            Debug.LogWarning(
                "환경 조명 Profile은 적용했지만 " +
                "EnvironmentLightingController의 일부 Light2D 참조가 비어 있습니다."
            );
        }

        return true;
    }


    // 현재 LightingRig의 값을
    // 지정한 EnvironmentVisualProfile에 복사한다.
    public bool CaptureCurrentToProfile(
        EnvironmentVisualProfile profile)
    {
        if (profile == null)
        {
            Debug.LogWarning(
                "환경 조명 저장 실패: " +
                "EnvironmentVisualProfile이 없습니다."
            );

            return false;
        }

        if (globalAmbient == null ||
            sunKey == null ||
            skyFill == null)
        {
            Debug.LogWarning(
                "환경 조명 저장 실패: " +
                "Global_Ambient / Sun_Key / Sky_Fill 연결을 확인하세요."
            );

            return false;
        }


        CaptureGlobalLightSettings(
            globalAmbient,
            profile.globalAmbient
        );

        CapturePointLightSettings(
            sunKey,
            profile.sunKey
        );

        CapturePointLightSettings(
            skyFill,
            profile.skyFill
        );

        return true;
    }


    private void ApplyGlobalLightSettings(
        Light2D targetLight,
        EnvironmentGlobalLightSettings settings)
    {
        if (targetLight == null ||
            settings == null)
        {
            return;
        }

        targetLight.enabled =
            settings.enabled;

        targetLight.color =
            settings.color;

        targetLight.intensity =
            Mathf.Max(
                0f,
                settings.intensity
            );
    }


    private void ApplyPointLightSettings(
        Light2D targetLight,
        EnvironmentPointLightSettings settings)
    {
        if (targetLight == null ||
            settings == null)
        {
            return;
        }

        targetLight.enabled =
            settings.enabled;

        targetLight.color =
            settings.color;

        targetLight.intensity =
            Mathf.Max(
                0f,
                settings.intensity
            );

        targetLight.falloffIntensity =
            Mathf.Clamp01(
                settings.falloffIntensity
            );


        targetLight.transform.localPosition =
            settings.localPosition;

        targetLight.transform.localEulerAngles =
            settings.localEulerAngles;


        float safeInnerRadius =
            Mathf.Max(
                0f,
                settings.innerRadius
            );

        float safeOuterRadius =
            Mathf.Max(
                safeInnerRadius,
                settings.outerRadius
            );

        targetLight.pointLightInnerRadius =
            safeInnerRadius;

        targetLight.pointLightOuterRadius =
            safeOuterRadius;


        float safeInnerAngle =
            Mathf.Clamp(
                settings.innerAngle,
                0f,
                360f
            );

        float safeOuterAngle =
            Mathf.Clamp(
                settings.outerAngle,
                safeInnerAngle,
                360f
            );

        targetLight.pointLightInnerAngle =
            safeInnerAngle;

        targetLight.pointLightOuterAngle =
            safeOuterAngle;
    }


    private void CaptureGlobalLightSettings(
        Light2D sourceLight,
        EnvironmentGlobalLightSettings settings)
    {
        if (sourceLight == null ||
            settings == null)
        {
            return;
        }

        settings.enabled =
            sourceLight.enabled;

        settings.color =
            sourceLight.color;

        settings.intensity =
            sourceLight.intensity;
    }


    private void CapturePointLightSettings(
        Light2D sourceLight,
        EnvironmentPointLightSettings settings)
    {
        if (sourceLight == null ||
            settings == null)
        {
            return;
        }

        settings.enabled =
            sourceLight.enabled;

        settings.color =
            sourceLight.color;

        settings.intensity =
            sourceLight.intensity;

        settings.falloffIntensity =
            sourceLight.falloffIntensity;


        settings.localPosition =
            sourceLight.transform.localPosition;

        settings.localEulerAngles =
            sourceLight.transform.localEulerAngles;


        settings.innerRadius =
            sourceLight.pointLightInnerRadius;

        settings.outerRadius =
            sourceLight.pointLightOuterRadius;

        settings.innerAngle =
            sourceLight.pointLightInnerAngle;

        settings.outerAngle =
            sourceLight.pointLightOuterAngle;
    }
}
