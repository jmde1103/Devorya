using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> PopupOpenAnimationData를 재사용해서 버튼 아이콘 클릭 시 노이즈 애니메이션을 재생하는 컴포넌트
public class UIButtonNoiseAnimator : MonoBehaviour
{
    [Header("Data")]
    // <변경부분> 팝업 글리치와 같은 데이터 구조를 버튼 아이콘 노이즈에도 재사용
    [SerializeField] private PopupOpenAnimationData noiseAnimationData;

    [Header("Button")]
    // <변경부분> 클릭 이벤트를 받을 버튼
    // 비워두면 부모에서 Button을 자동 탐색
    [SerializeField] private Button targetButton;

    [Header("Animation Target")]
    // <변경부분> 실제로 흔들고 스케일을 조정할 아이콘 RectTransform
    // 비워두면 현재 오브젝트 RectTransform 사용
    [SerializeField] private RectTransform targetRect;

    // <변경부분> 알파 깜빡임을 적용할 UI 그래픽
    // 비워두면 현재 오브젝트의 Graphic을 자동 탐색
    [SerializeField] private Graphic targetGraphic;

    // 현재 실행 중인 노이즈 애니메이션 코루틴
    private Coroutine noiseCoroutine;

    // 애니메이션 시작 전 기준 위치
    private Vector2 baseAnchoredPosition;

    // 애니메이션 시작 전 기준 스케일
    private Vector3 baseLocalScale;

    // 애니메이션 시작 전 기준 알파
    private float baseAlpha = 1f;

    // <변경부분> 기준 위치/스케일/알파가 정상 저장되었는지 여부
    // 저장 전 RestoreFinalState()가 실행되어 Scale이 0이 되는 문제를 방지
    private bool hasCapturedBaseState = false;

    private void Awake()
    {
        AutoBindReferences();
    }

    private void OnEnable()
    {
        AutoBindReferences();

        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(PlayNoise);
            targetButton.onClick.AddListener(PlayNoise);
        }
    }

    private void OnDisable()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(PlayNoise);
        }

        if (noiseCoroutine != null)
        {
            StopCoroutine(noiseCoroutine);
            noiseCoroutine = null;

            // <변경부분> 실제 애니메이션 중단 시에만 원래 상태로 복구
            RestoreFinalState();
        }
    }

    // <변경부분> 필요한 참조를 자동으로 연결하는 함수
    private void AutoBindReferences()
    {
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        if (targetButton == null)
        {
            targetButton = GetComponentInParent<Button>();
        }
    }

    // <변경부분> 버튼 아이콘 노이즈 애니메이션 재생
    public void PlayNoise()
    {
        AutoBindReferences();

        if (targetRect == null)
        {
            return;
        }

        if (noiseAnimationData == null)
        {
            return;
        }

        if (noiseCoroutine != null)
        {
            StopCoroutine(noiseCoroutine);

            // <변경부분> 이전 애니메이션이 실제로 기준값을 저장한 경우에만 복구
            RestoreFinalState();
        }

        noiseCoroutine = StartCoroutine(PlayNoiseRoutine());
    }

    // <변경부분> 애니메이션 시작 전 현재 UI 상태를 기준값으로 저장
    private void CaptureBaseState()
    {
        if (targetRect != null)
        {
            baseAnchoredPosition = targetRect.anchoredPosition;
            baseLocalScale = targetRect.localScale;
        }

        if (targetGraphic != null)
        {
            baseAlpha = targetGraphic.color.a;
        }

        hasCapturedBaseState = true;
    }

    // <변경부분> 실제 노이즈 애니메이션 코루틴
    private IEnumerator PlayNoiseRoutine()
    {
        // <변경부분> 애니메이션 시작 전 현재 상태 저장
        CaptureBaseState();

        float duration = Mathf.Max(0.001f, noiseAnimationData.duration);
        float elapsed = 0f;

        // <변경부분> 시작 스케일 적용
        ApplyScale(noiseAnimationData.startScale);

        // <변경부분> 시작 알파 적용
        ApplyAlpha(noiseAnimationData.startAlpha);

        while (elapsed < duration)
        {
            float deltaTime = noiseAnimationData.useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            elapsed += deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = noiseAnimationData.scaleCurve.Evaluate(t);

            // <변경부분> PopupOpenAnimationData의 스케일 값을 그대로 재사용
            Vector2 currentScale = Vector2.Lerp(
                noiseAnimationData.startScale,
                noiseAnimationData.endScale,
                curveT
            );

            ApplyScale(currentScale);

            // <변경부분> PopupOpenAnimationData의 알파 깜빡임 값을 그대로 재사용
            ApplyAlphaFlicker(t);

            // <변경부분> PopupOpenAnimationData의 위치 지터 값을 그대로 재사용
            ApplyPositionJitter(t);

            yield return null;
        }

        RestoreFinalState();
        noiseCoroutine = null;
    }

    // <변경부분> 원래 스케일에 배율을 곱해서 적용
    // X Scale이 -1인 UI에도 안전하게 동작
    private void ApplyScale(Vector2 scaleMultiplier)
    {
        targetRect.localScale = new Vector3(
            baseLocalScale.x * scaleMultiplier.x,
            baseLocalScale.y * scaleMultiplier.y,
            baseLocalScale.z
        );
    }

    // <변경부분> 알파값 적용
    private void ApplyAlpha(float alpha)
    {
        if (targetGraphic == null)
        {
            return;
        }

        Color color = targetGraphic.color;
        color.a = alpha;
        targetGraphic.color = color;
    }

    // <변경부분> 알파 깜빡임 적용
    private void ApplyAlphaFlicker(float t)
    {
        if (targetGraphic == null)
        {
            return;
        }

        if (noiseAnimationData.useAlphaFlicker == false)
        {
            float normalAlpha = Mathf.Lerp(
                noiseAnimationData.startAlpha,
                noiseAnimationData.endAlpha,
                t
            );

            ApplyAlpha(normalAlpha);
            return;
        }

        float baseLerpAlpha = Mathf.Lerp(
            noiseAnimationData.startAlpha,
            noiseAnimationData.endAlpha,
            t
        );

        int flickerIndex = Mathf.FloorToInt(t * noiseAnimationData.flickerCount);
        bool isFlickerLow = flickerIndex % 2 == 0 && t < 0.9f;

        float finalAlpha = isFlickerLow
            ? Mathf.Min(baseLerpAlpha, noiseAnimationData.flickerMinAlpha)
            : baseLerpAlpha;

        ApplyAlpha(finalAlpha);
    }

    // <변경부분> 위치 지터 적용
    private void ApplyPositionJitter(float t)
    {
        if (noiseAnimationData.usePositionJitter == false)
        {
            targetRect.anchoredPosition = baseAnchoredPosition;
            return;
        }

        float jitterPower = 1f - t;

        Vector2 jitterOffset = new Vector2(
            Random.Range(-noiseAnimationData.jitterRange.x, noiseAnimationData.jitterRange.x),
            Random.Range(-noiseAnimationData.jitterRange.y, noiseAnimationData.jitterRange.y)
        ) * jitterPower;

        targetRect.anchoredPosition = baseAnchoredPosition + jitterOffset;
    }

    // <변경부분> 애니메이션 종료 후 원래 상태로 복구
    private void RestoreFinalState()
    {
        // <변경부분> 기준값이 저장되지 않은 상태에서는 복구하지 않음
        // 이 처리가 없으면 baseLocalScale 기본값인 0,0,0 때문에 버튼이 사라질 수 있음
        if (hasCapturedBaseState == false)
        {
            return;
        }

        if (targetRect != null)
        {
            targetRect.anchoredPosition = baseAnchoredPosition;
            targetRect.localScale = baseLocalScale;
        }

        if (targetGraphic != null)
        {
            Color color = targetGraphic.color;
            color.a = baseAlpha;
            targetGraphic.color = color;
        }
    }
}