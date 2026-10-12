using System.Collections;
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
    private Light2D characterFill;

    [SerializeField]
    private Light2D sunKey;

    [SerializeField]
    private Light2D skyFill;


    // <변경부분>
    // 낮/밤 전환 중 진행 중인 조명 보간을 관리한다.
    private Coroutine profileTransitionCoroutine;


    // 지정한 EnvironmentVisualProfile의 값을
    // 현재 LightingRig에 적용한다.
    public bool ApplyProfile(
      EnvironmentVisualProfile profile)
    {
        // <변경부분>
        // 씬 로드/에디터의 즉시 적용은 진행 중인 페이드를 종료한다.
        StopProfileTransition();

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

        // 야간 등 어두운 환경에서
        // Piece를 보조하는 캐릭터 전용 Spot Light를 적용한다.
        if (characterFill != null)
        {
            ApplyPointLightSettings(
                characterFill,
                profile.characterFill
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


    // <변경부분>
    // 낮/밤 전환 중에는 현재 Light2D 상태부터 목표 프로필까지 부드럽게 변경한다.
    // 씬 최초 로드는 기존 ApplyProfile()을 사용해 즉시 설정한다.
    public void TransitionToProfile(
        EnvironmentVisualProfile profile,
        float duration)
    {
        if (profile == null)
        {
            return;
        }

        StopProfileTransition();

        if (!Application.isPlaying ||
            !gameObject.activeInHierarchy ||
            duration <= 0f)
        {
            ApplyProfile(profile);
            return;
        }

        // <변경부분>
        // 시작값은 ScriptableObject가 아니라 현재 실제 Light2D에서 캡처한다.
        // 전환 도중 새 이벤트가 발생해도 현재 밝기에서 이어서 진행한다.
        EnvironmentGlobalLightSettings globalFrom =
            CaptureGlobalSnapshot(globalAmbient);

        EnvironmentPointLightSettings characterFrom =
            CapturePointSnapshot(characterFill);

        EnvironmentPointLightSettings sunFrom =
            CapturePointSnapshot(sunKey);

        EnvironmentPointLightSettings skyFrom =
            CapturePointSnapshot(skyFill);

        profileTransitionCoroutine = StartCoroutine(
            BlendToProfile(
                profile,
                duration,
                globalFrom,
                characterFrom,
                sunFrom,
                skyFrom
            )
        );
    }

    // <변경부분>
    // 실행 중인 환경 조명 페이드를 중단한다.
    private void StopProfileTransition()
    {
        if (profileTransitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(profileTransitionCoroutine);
        profileTransitionCoroutine = null;
    }

    // <변경부분>
    // Global Light의 현재 상태를 임시 데이터로 복사한다.
    private EnvironmentGlobalLightSettings CaptureGlobalSnapshot(
        Light2D light)
    {
        if (light == null)
        {
            return null;
        }

        EnvironmentGlobalLightSettings result =
            new EnvironmentGlobalLightSettings();

        CaptureGlobalLightSettings(light, result);
        result.intensity = light.enabled ? light.intensity : 0f;

        return result;
    }

    // <변경부분>
    // Point/Spot Light의 현재 상태를 임시 데이터로 복사한다.
    private EnvironmentPointLightSettings CapturePointSnapshot(
        Light2D light)
    {
        if (light == null)
        {
            return null;
        }

        EnvironmentPointLightSettings result =
            new EnvironmentPointLightSettings();

        CapturePointLightSettings(light, result);
        result.intensity = light.enabled ? light.intensity : 0f;

        return result;
    }

    // <변경부분>
    // 지정한 시간 동안 모든 환경 Light2D 값을 보간한다.
    private IEnumerator BlendToProfile(
        EnvironmentVisualProfile profile,
        float duration,
        EnvironmentGlobalLightSettings globalFrom,
        EnvironmentPointLightSettings characterFrom,
        EnvironmentPointLightSettings sunFrom,
        EnvironmentPointLightSettings skyFrom)
    {
        float elapsed = 0f;

        // <변경부분>
        // 꺼져 있던 조명도 밝기 0부터 자연스럽게 켤 수 있도록 준비한다.
        PrepareBlendLight(
            globalAmbient, globalFrom, profile.globalAmbient);

        PrepareBlendLight(
            characterFill, characterFrom, profile.characterFill);

        PrepareBlendLight(
            sunKey, sunFrom, profile.sunKey);

        PrepareBlendLight(
            skyFill, skyFrom, profile.skyFill);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            BlendGlobalLight(
                globalAmbient, globalFrom, profile.globalAmbient, t);

            BlendPointLight(
                characterFill, characterFrom, profile.characterFill, t);

            BlendPointLight(
                sunKey, sunFrom, profile.sunKey, t);

            BlendPointLight(
                skyFill, skyFrom, profile.skyFill, t);

            yield return null;
        }

        profileTransitionCoroutine = null;

        // <변경부분>
        // 페이드 완료 시 enabled를 포함한 목표 프로필 값을 정확히 적용한다.
        ApplyProfile(profile);
    }

    // <변경부분>
    // 꺼져 있던 Global Light를 보간 동안 사용할 수 있게 한다.
    private void PrepareBlendLight(
        Light2D light,
        EnvironmentGlobalLightSettings from,
        EnvironmentGlobalLightSettings to)
    {
        if (light == null || from == null || to == null)
        {
            return;
        }

        light.enabled = from.intensity > 0f || to.enabled;
    }

    // <변경부분>
    // 꺼져 있던 Point/Spot Light를 보간 동안 사용할 수 있게 한다.
    private void PrepareBlendLight(
        Light2D light,
        EnvironmentPointLightSettings from,
        EnvironmentPointLightSettings to)
    {
        if (light == null || from == null || to == null)
        {
            return;
        }

        light.enabled = from.intensity > 0f || to.enabled;
    }

    // <변경부분>
    // Global Light의 색상과 밝기를 변경한다.
    private void BlendGlobalLight(
        Light2D light,
        EnvironmentGlobalLightSettings from,
        EnvironmentGlobalLightSettings to,
        float t)
    {
        if (light == null || from == null || to == null)
        {
            return;
        }

        light.color = Color.Lerp(from.color, to.color, t);

        light.intensity = Mathf.Lerp(
            from.intensity,
            to.enabled ? Mathf.Max(0f, to.intensity) : 0f,
            t
        );
    }

    // <변경부분>
    // Point/Spot Light의 색상, 밝기, 감쇠, 위치, 각도, 범위를 변경한다.
    private void BlendPointLight(
        Light2D light,
        EnvironmentPointLightSettings from,
        EnvironmentPointLightSettings to,
        float t)
    {
        if (light == null || from == null || to == null)
        {
            return;
        }

        light.color = Color.Lerp(from.color, to.color, t);

        light.intensity = Mathf.Lerp(
            from.intensity,
            to.enabled ? Mathf.Max(0f, to.intensity) : 0f,
            t
        );

        light.falloffIntensity =
            Mathf.Lerp(from.falloffIntensity, to.falloffIntensity, t);

        light.transform.localPosition =
            Vector3.Lerp(from.localPosition, to.localPosition, t);

        light.transform.localRotation = Quaternion.Slerp(
            Quaternion.Euler(from.localEulerAngles),
            Quaternion.Euler(to.localEulerAngles),
            t
        );

        float innerRadius = Mathf.Max(
            0f,
            Mathf.Lerp(from.innerRadius, to.innerRadius, t)
        );

        float outerRadius = Mathf.Max(
            innerRadius,
            Mathf.Lerp(from.outerRadius, to.outerRadius, t)
        );

        light.pointLightInnerRadius = innerRadius;
        light.pointLightOuterRadius = outerRadius;

        float innerAngle = Mathf.Clamp(
            Mathf.Lerp(from.innerAngle, to.innerAngle, t),
            0f,
            360f
        );

        float outerAngle = Mathf.Clamp(
            Mathf.Lerp(from.outerAngle, to.outerAngle, t),
            innerAngle,
            360f
        );

        light.pointLightInnerAngle = innerAngle;
        light.pointLightOuterAngle = outerAngle;
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
       characterFill == null ||
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
            characterFill,
            profile.characterFill
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
