using System.Collections;
using UnityEngine;

// <변경부분> 모든 팝업형 UI에 붙여서 데이터 기반 오픈 애니메이션을 실행하는 공통 컴포넌트
[RequireComponent(typeof(RectTransform))]
public class PopupOpenAnimator : MonoBehaviour
{
    [Header("Data")]
    // <변경부분> 이 팝업에 적용할 오픈 애니메이션 데이터
    [SerializeField] private PopupOpenAnimationData animationData;

    [Header("Target")]
    // <변경부분> 실제로 흔들고 스케일을 조정할 RectTransform
    [SerializeField] private RectTransform targetRect;

    // <변경부분> 알파/레이캐스트 제어용 CanvasGroup
    [SerializeField] private CanvasGroup canvasGroup;

    // 현재 실행 중인 애니메이션 코루틴
    private Coroutine openCoroutine;

    // <변경부분> 현재 오픈 애니메이션에서 사용할 기준 위치
    // TooltipPopupUI가 위치를 먼저 설정한 뒤 PlayOpen()을 호출하므로
    // 매 오픈마다 정상적인 현재 위치를 새 기준으로 저장한다.
    private Vector2 baseAnchoredPosition;

    // <변경부분> 애니메이션의 고정 원본 스케일
    // 애니메이션 도중 재호출돼도 찌그러진 현재 스케일을
    // 새로운 기준으로 다시 저장하지 않도록 초기화 시 한 번만 기록한다.
    private Vector3 originalLocalScale = Vector3.one;

    // <변경부분> 원본 스케일이 정상적으로 저장됐는지 확인한다.
    private bool hasCapturedOriginalScale = false;

    private void Awake()
    {
        // 필요한 UI 참조를 준비한다.
        EnsureReferences();

        // <변경부분> 글리치 애니메이션이 적용되기 전
        // Inspector에 설정된 정상 스케일을 한 번만 저장한다.
        CaptureOriginalScaleIfNeeded();

        // 초기 위치를 안전한 기준 위치로 저장한다.
        if (targetRect != null)
        {
            baseAnchoredPosition =
                targetRect.anchoredPosition;
        }
    }

    private void OnDisable()
    {
        // <변경부분> 비활성화 시 실행 중인 글리치 코루틴을 중단한다.
        StopOpenCoroutine();

        // <변경부분> 애니메이션 중간 상태에서 팝업이 꺼지더라도
        // 찌그러진 스케일과 흔들린 위치가 남지 않도록
        // 반드시 정상적인 최종 상태로 복구한다.
        ApplyFinalState();
    }

    // <변경부분> 외부에서 애니메이션 데이터를 교체할 때 사용
    public void SetAnimationData(PopupOpenAnimationData newData)
    {
        animationData = newData;
    }

    // <변경부분> 팝업 오픈 애니메이션 실행
    public void PlayOpen()
    {
        // 필요한 UI 참조를 다시 확인한다.
        EnsureReferences();

        if (targetRect == null)
        {
            return;
        }

        // 원본 스케일은 최초 한 번만 저장한다.
        CaptureOriginalScaleIfNeeded();

        // <변경부분> 이전 글리치 애니메이션이 남아 있다면 먼저 중단한다.
        StopOpenCoroutine();

        // <변경부분> TooltipPopupUI가 이미 새 위치를 계산해 적용했으므로
        // 현재 위치를 이번 오픈 애니메이션의 정상 기준 위치로 저장한다.
        baseAnchoredPosition =
            targetRect.anchoredPosition;

        // <변경부분> 이전 애니메이션에서 남은 찌그러진 스케일을 제거한다.
        // 새 애니메이션은 항상 동일한 원본 스케일에서 시작한다.
        RestoreBaseTransformBeforeOpen();

        openCoroutine =
            StartCoroutine(
                PlayOpenRoutine()
            );
    }

    // <변경부분> 즉시 최종 상태로 보정
    public void CompleteImmediately()
    {
        StopOpenCoroutine();
        ApplyFinalState();
    }

    // <변경부분> 실제 팝업 오픈 애니메이션 코루틴
    private IEnumerator PlayOpenRoutine()
    {
        if (animationData == null)
        {
            ApplyFinalState();
            openCoroutine = null;
            yield break;
        }

        // CanvasGroup 기본 상태 적용
        canvasGroup.blocksRaycasts = animationData.blocksRaycasts;
        canvasGroup.interactable = animationData.interactable;

        float duration = Mathf.Max(0.001f, animationData.duration);
        float elapsed = 0f;

        // <변경부분> 시작 스케일을 절대값이 아니라 원래 스케일에 곱해서 적용
        targetRect.localScale = new Vector3(
    originalLocalScale.x *
    animationData.startScale.x,

    originalLocalScale.y *
    animationData.startScale.y,

    originalLocalScale.z
);

        canvasGroup.alpha = animationData.startAlpha;

        while (elapsed < duration)
        {
            float deltaTime = animationData.useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            elapsed += deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = animationData.scaleCurve.Evaluate(t);

            // 스케일 보간
            Vector2 currentScale = Vector2.Lerp(
                animationData.startScale,
                animationData.endScale,
                curveT
            );

            // <변경부분> 보간 스케일도 원래 스케일에 곱해서 적용
            // X Scale이 -1인 UI는 끝까지 - 방향을 유지한다.
            targetRect.localScale = new Vector3(
            originalLocalScale.x *
            currentScale.x,

            originalLocalScale.y *
            currentScale.y,

            originalLocalScale.z
            );

            // 알파 깜빡임
            if (animationData.useAlphaFlicker)
            {
                float baseAlpha = Mathf.Lerp(animationData.startAlpha, animationData.endAlpha, t);

                int flickerIndex = Mathf.FloorToInt(t * animationData.flickerCount);
                bool isFlickerLow = flickerIndex % 2 == 0 && t < 0.9f;

                canvasGroup.alpha = isFlickerLow
                    ? Mathf.Min(baseAlpha, animationData.flickerMinAlpha)
                    : baseAlpha;
            }
            else
            {
                canvasGroup.alpha = Mathf.Lerp(animationData.startAlpha, animationData.endAlpha, t);
            }

            // 위치 지지직 흔들림
            if (animationData.usePositionJitter)
            {
                float jitterPower = 1f - t;

                Vector2 jitterOffset = new Vector2(
                    Random.Range(-animationData.jitterRange.x, animationData.jitterRange.x),
                    Random.Range(-animationData.jitterRange.y, animationData.jitterRange.y)
                ) * jitterPower;

                targetRect.anchoredPosition = baseAnchoredPosition + jitterOffset;
            }

            yield return null;
        }

        ApplyFinalState();

        openCoroutine = null;
    }

    private void EnsureReferences()
    {
        // Target이 비어 있으면 현재 오브젝트의 RectTransform을 사용한다.
        if (targetRect == null)
        {
            targetRect =
                GetComponent<RectTransform>();
        }

        // CanvasGroup이 비어 있으면 현재 오브젝트에서 찾고,
        // 없으면 새로 추가한다.
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    // <변경부분> 글리치가 적용되기 전 정상 스케일을 최초 한 번만 저장한다.
    private void CaptureOriginalScaleIfNeeded()
    {
        if (hasCapturedOriginalScale)
        {
            return;
        }

        if (targetRect == null)
        {
            return;
        }

        originalLocalScale =
            targetRect.localScale;

        hasCapturedOriginalScale = true;
    }

    // <변경부분> 현재 실행 중인 오픈 코루틴을 안전하게 중단한다.
    private void StopOpenCoroutine()
    {
        if (openCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            openCoroutine
        );

        openCoroutine = null;
    }

    // <변경부분> 새 오픈 애니메이션을 시작하기 전에
    // 이전 글리치에서 남은 위치·스케일·알파 상태를 정상화한다.
    private void RestoreBaseTransformBeforeOpen()
    {
        if (targetRect != null)
        {
            targetRect.localScale =
                originalLocalScale;

            targetRect.anchoredPosition =
                baseAnchoredPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    // <변경부분> 애니메이션 종료 후 팝업을 정확한 최종 상태로 보정
    private void ApplyFinalState()
    {
        EnsureReferences();
        CaptureOriginalScaleIfNeeded();

        if (targetRect != null)
        {
            if (animationData != null)
            {
                // <변경부분> 고정된 원본 스케일을 기준으로
                // 애니메이션 데이터의 최종 배율을 적용한다.
                targetRect.localScale =
                    new Vector3(
                        originalLocalScale.x *
                        animationData.endScale.x,

                        originalLocalScale.y *
                        animationData.endScale.y,

                        originalLocalScale.z
                    );
            }
            else
            {
                // 애니메이션 데이터가 없으면
                // Inspector의 원본 스케일로 복구한다.
                targetRect.localScale =
                    originalLocalScale;
            }

            // 흔들림 도중 중단됐더라도
            // 저장된 정상 위치로 복구한다.
            targetRect.anchoredPosition =
                baseAnchoredPosition;
        }

        if (canvasGroup != null)
        {
            if (animationData != null)
            {
                canvasGroup.alpha =
                    animationData.endAlpha;

                canvasGroup.blocksRaycasts =
                    animationData.blocksRaycasts;

                canvasGroup.interactable =
                    animationData.interactable;
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }
    }
} 