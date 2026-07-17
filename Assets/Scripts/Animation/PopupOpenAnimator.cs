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

    // 애니메이션 시작 시점의 기준 위치
    private Vector2 baseAnchoredPosition;

    // <변경부분> 애니메이션 시작 시점의 원래 스케일
    // 상대 스테이터스 창처럼 X Scale이 -1인 UI도 유지하기 위해 저장
    private Vector3 baseLocalScale;

    private void Awake()
    {
        // Target이 비어 있으면 현재 오브젝트 RectTransform 사용
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }

        // CanvasGroup이 없으면 자동 추가
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void OnDisable()
    {
        // 비활성화될 때 진행 중인 애니메이션 정리
        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
            openCoroutine = null;
        }
    }

    // <변경부분> 외부에서 애니메이션 데이터를 교체할 때 사용
    public void SetAnimationData(PopupOpenAnimationData newData)
    {
        animationData = newData;
    }

    // <변경부분> 팝업 오픈 애니메이션 실행
    public void PlayOpen()
    {
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
        }

        openCoroutine = StartCoroutine(
            PlayOpenRoutine()
        );
    }

    // <변경부분> 즉시 최종 상태로 보정
    public void CompleteImmediately()
    {
        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
            openCoroutine = null;
        }

        ApplyFinalState();
    }

    // <변경부분> 실제 팝업 오픈 애니메이션 코루틴
    private IEnumerator PlayOpenRoutine()
    {
        if (animationData == null)
        {
            ApplyFinalState();
            yield break;
        }

        // 애니메이션 시작 위치 저장
        baseAnchoredPosition = targetRect.anchoredPosition;

        // <변경부분> 현재 UI의 원래 스케일 저장
        // EnemyStatusPanel처럼 X Scale이 -1인 경우 이 값을 기준으로 애니메이션을 곱한다.
        baseLocalScale = targetRect.localScale;

        // CanvasGroup 기본 상태 적용
        canvasGroup.blocksRaycasts = animationData.blocksRaycasts;
        canvasGroup.interactable = animationData.interactable;

        float duration = Mathf.Max(0.001f, animationData.duration);
        float elapsed = 0f;

        // <변경부분> 시작 스케일을 절대값이 아니라 원래 스케일에 곱해서 적용
        targetRect.localScale = new Vector3(
            baseLocalScale.x * animationData.startScale.x,
            baseLocalScale.y * animationData.startScale.y,
            baseLocalScale.z
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
                baseLocalScale.x * currentScale.x,
                baseLocalScale.y * currentScale.y,
                baseLocalScale.z
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

    // <변경부분> 애니메이션 종료 후 팝업을 정확한 최종 상태로 보정
    private void ApplyFinalState()
    {
        if (targetRect != null)
        {
            if (animationData != null)
            {
                // <변경부분> 최종 스케일도 원래 스케일에 곱해서 적용
                // EnemyStatusPanel처럼 X Scale이 -1인 UI도 -1 상태를 유지한다.
                targetRect.localScale = new Vector3(
                    baseLocalScale.x * animationData.endScale.x,
                    baseLocalScale.y * animationData.endScale.y,
                    baseLocalScale.z
                );
            }
            else
            {
                // <변경부분> 데이터가 없을 때도 Vector3.one으로 강제하지 않고 원래 스케일로 복구
                targetRect.localScale = baseLocalScale;
            }

            targetRect.anchoredPosition = baseAnchoredPosition;
        }

        if (canvasGroup != null)
        {
            if (animationData != null)
            {
                canvasGroup.alpha = animationData.endAlpha;
                canvasGroup.blocksRaycasts = animationData.blocksRaycasts;
                canvasGroup.interactable = animationData.interactable;
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }
    }
} 