
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// <변경부분>
// EventScene의 URP 2D 화면 글리치를 제어하는 컴포넌트.
// 기존 PopupOpenAnimator와 독립적으로 동작한다.
public class EventScreenGlitchController : MonoBehaviour
{
    [Header("Glitch UI")]
    [SerializeField]
    private RawImage screenOverlay;

    [SerializeField]
    private Material glitchMaterial;

    [Header("Preview Settings")]
    [SerializeField, Range(0f, 1f)]
    private float previewStrength = 0.55f;

    [SerializeField, Min(0.05f)]
    private float previewDuration = 0.4f;

    private static readonly int GlitchAmountId =
        Shader.PropertyToID("_GlitchAmount");

    private static readonly int GlitchPhaseId =
        Shader.PropertyToID("_GlitchPhase");

    // <변경부분>
    // 원본 Material Asset을 수정하지 않도록
    // 이 컴포넌트 전용 Material 인스턴스를 사용한다.
    private Material runtimeMaterial;

    private Coroutine glitchCoroutine;

    public bool IsPlaying
    {
        get { return glitchCoroutine != null; }
    }

    private void Awake()
    {
        EnsureMaterial();
    }

    private bool EnsureMaterial()
    {
        if (screenOverlay == null ||
            glitchMaterial == null)
        {
            Debug.LogWarning(
                "Screen Glitch 설정 누락: " +
                "Screen Overlay 또는 Glitch Material이 없습니다.",
                this
            );

            return false;
        }

        if (runtimeMaterial != null)
        {
            return true;
        }

        runtimeMaterial = new Material(glitchMaterial);
        runtimeMaterial.name =
            glitchMaterial.name + " (Runtime)";

        runtimeMaterial.SetFloat(GlitchAmountId, 0f);

        screenOverlay.material = runtimeMaterial;
        screenOverlay.raycastTarget = false;
        screenOverlay.enabled = false;

        return true;
    }

    // <변경부분>
    // Play Mode 중 컴포넌트의 우측 메뉴에서
    // "Play Glitch Preview"로 즉시 테스트할 수 있다.
    [ContextMenu("Play Glitch Preview")]
    public void PlayGlitchPreview()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Screen Glitch Preview는 Play Mode에서 실행해 주세요.",
                this
            );

            return;
        }

        PlayGlitch(previewStrength, previewDuration);
    }

    // <변경부분>
    // 추후 EventSceneSequenceController가 호출할 수 있는
    // 공용 글리치 재생 함수.
    public void PlayGlitch(float strength, float duration)
    {
        if (!isActiveAndEnabled ||
            !EnsureMaterial())
        {
            return;
        }

        StopGlitch();

        glitchCoroutine = StartCoroutine(
            PlayGlitchRoutine(
                Mathf.Clamp01(strength),
                Mathf.Max(0.05f, duration)
            )
        );
    }

    private IEnumerator PlayGlitchRoutine(
        float strength,
        float duration)
    {
        screenOverlay.enabled = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime =
                Mathf.Clamp01(elapsed / duration);

            // <변경부분>
            // 0~20%: 빠르게 강해짐
            // 20~70%: 최대 강도 유지
            // 70~100%: 정상 화면으로 복귀
            float envelope;

            if (normalizedTime < 0.2f)
            {
                envelope = normalizedTime / 0.2f;
            }
            else if (normalizedTime < 0.7f)
            {
                envelope = 1f;
            }
            else
            {
                envelope =
                    (1f - normalizedTime) / 0.3f;
            }

            runtimeMaterial.SetFloat(
                GlitchAmountId,
                strength * Mathf.Clamp01(envelope)
            );

            // <변경부분>
            // 시간 정지 중에도 글리치 패턴이 움직이도록
            // Unscaled Time을 직접 전달한다.
            runtimeMaterial.SetFloat(
                GlitchPhaseId,
                Time.unscaledTime
            );

            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        ResetGlitchVisual();
        glitchCoroutine = null;
    }

    public void StopGlitch()
    {
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }

        ResetGlitchVisual();
    }

    private void ResetGlitchVisual()
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(
                GlitchAmountId,
                0f
            );
        }

        if (screenOverlay != null)
        {
            screenOverlay.enabled = false;
        }
    }

    private void OnDisable()
    {
        StopGlitch();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        if (screenOverlay != null &&
            screenOverlay.material == runtimeMaterial)
        {
            screenOverlay.material = null;
        }

        Destroy(runtimeMaterial);
        runtimeMaterial = null;
    }
}

