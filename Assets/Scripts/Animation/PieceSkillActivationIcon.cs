using System.Collections;
using UnityEngine;

// <변경부분> 기물 위에 일반스킬 또는 고유스킬 아이콘을 표시하고
// 확대 등장 → 원래 크기 복귀 → 위로 이동하며 페이드아웃하는 연출을 담당한다.
public class PieceSkillActivationIcon : MonoBehaviour
{
    [Header("References")]
    // <변경부분> 실제 스킬 아이콘을 표시할 SpriteRenderer
    [SerializeField]
    private SpriteRenderer iconRenderer;

    [Header("Animation Position")]
    // <변경부분> 연출 시작 시 아이콘의 로컬 위치
    [SerializeField]
    private Vector3 startLocalPosition =
        new Vector3(
            0f,
            0.75f,
            0f
        );

    // <변경부분> 페이드아웃하면서 위로 올라갈 로컬 거리
    [SerializeField]
    private float riseDistance =
        0.35f;

    [Header("Animation Scale")]
    // <변경부분> 아이콘이 처음 나타날 때의 시작 배율
    [SerializeField]
    private float startScale =
        0f;

    // <변경부분> 순간적으로 크게 튀어나올 최대 배율
    [SerializeField]
    private float popScale =
        1.35f;

    // <변경부분> 확대 후 돌아올 기본 배율
    [SerializeField]
    private float settleScale =
        1f;

    [Header("Animation Duration")]
    // <변경부분> 0에서 최대 크기까지 빠르게 확대되는 시간
    [SerializeField]
    private float popDuration =
        0.09f;

    // <변경부분> 최대 크기에서 기본 크기로 돌아오는 시간
    [SerializeField]
    private float settleDuration =
        0.08f;

    // <변경부분> 기본 크기에서 잠시 유지되는 시간
    [SerializeField]
    private float holdDuration =
        0.12f;

    // <변경부분> 위로 이동하며 페이드아웃되는 시간
    [SerializeField]
    private float riseAndFadeDuration =
        0.4f;

    [Header("Sorting")]
    // <변경부분> 기물과 타입 아이콘보다 앞에 표시할 정렬 순서
    [SerializeField]
    private int sortingOrder =
      500;

    [Header("Effect Timing")]
    // <변경부분> 스킬 실제 효과를 실행할 시점이다.
    // 기본값 0.17초는 확대와 기본 크기 복귀가 끝나는 시점이다.
    [SerializeField]
    private float effectTriggerDelay =
        0.17f;

    // <변경부분> 현재 실행 중인 아이콘 연출 코루틴
    private Coroutine playCoroutine;

    // <변경부분> SpriteRenderer의 원래 RGB 색상
    private Color baseColor =
        Color.white;

    private void Awake()
    {
        // Inspector 연결이 비어 있으면
        // 자기 자신 또는 자식에서 자동 탐색한다.
        if (iconRenderer == null)
        {
            iconRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (iconRenderer == null)
        {
            iconRenderer =
                GetComponentInChildren<SpriteRenderer>(
                    true
                );
        }

        if (iconRenderer != null)
        {
            baseColor =
                iconRenderer.color;

            iconRenderer.sortingOrder =
                sortingOrder;

            iconRenderer.enabled =
                false;
        }

        transform.localPosition =
            startLocalPosition;

        transform.localScale =
            Vector3.zero;
    }

    private void OnDisable()
    {
        // <변경부분> 기물이 제거되거나 비활성화될 때
        // 연출 상태가 남지 않도록 즉시 초기화한다.
        StopAndHideImmediately();
    }

    // <변경부분> 전달받은 스킬 아이콘으로 연출을 처음부터 재생한다.
    //
    // 같은 기물에서 다른 스킬이 연속 발동하면
    // 기존 연출을 중단하고 새 아이콘으로 다시 시작한다.
    public void Play(
        Sprite skillIcon)
    {
        if (skillIcon == null ||
            iconRenderer == null)
        {
            return;
        }

        if (playCoroutine != null)
        {
            StopCoroutine(
                playCoroutine
            );

            playCoroutine =
                null;
        }

        gameObject.SetActive(
            true
        );

        iconRenderer.sprite =
            skillIcon;

        iconRenderer.sortingOrder =
            sortingOrder;

        iconRenderer.enabled =
            true;

        Color visibleColor =
            baseColor;

        visibleColor.a =
            1f;

        iconRenderer.color =
            visibleColor;

        transform.localPosition =
            startLocalPosition;

        transform.localScale =
            Vector3.one *
            Mathf.Max(
                0f,
                startScale
            );

        playCoroutine =
            StartCoroutine(
                PlayRoutine()
            );
    }

    // <변경부분> 아이콘 연출을 시작한 뒤
    // Inspector에 설정한 실제 효과 실행 시점까지 기다린다.
    //
    // 호출한 전투 코루틴은 이 함수가 끝난 다음
    // 실제 스킬 효과를 적용하면 된다.
    public IEnumerator PlayBeforeEffectRoutine(
        Sprite skillIcon)
    {
        if (skillIcon == null ||
            iconRenderer == null)
        {
            yield break;
        }

        // 전체 아이콘 연출 시작
        Play(
            skillIcon
        );

        float safeTriggerDelay =
            Mathf.Max(
                0f,
                effectTriggerDelay
            );

        // 아이콘 확대와 크기 복귀가 끝날 때까지 대기
        if (safeTriggerDelay > 0f)
        {
            yield return
                new WaitForSeconds(
                    safeTriggerDelay
                );
        }
    }

    // <변경부분> 확대 등장, 크기 복귀,
    // 상승 및 페이드아웃을 순서대로 실행한다.
    private IEnumerator PlayRoutine()
    {
        // 0 → 최대 크기로 빠르게 확대
        yield return
            AnimateScaleRoutine(
                Mathf.Max(
                    0f,
                    startScale
                ),
                Mathf.Max(
                    0f,
                    popScale
                ),
                popDuration
            );

        // 최대 크기 → 기본 크기로 복귀
        yield return
            AnimateScaleRoutine(
                Mathf.Max(
                    0f,
                    popScale
                ),
                Mathf.Max(
                    0f,
                    settleScale
                ),
                settleDuration
            );

        float safeHoldDuration =
            Mathf.Max(
                0f,
                holdDuration
            );

        // 기본 크기로 잠시 유지
        if (safeHoldDuration > 0f)
        {
            yield return
                new WaitForSeconds(
                    safeHoldDuration
                );
        }

        Vector3 riseStartPosition =
            transform.localPosition;

        Vector3 riseTargetPosition =
            riseStartPosition +
            Vector3.up *
            riseDistance;

        float safeFadeDuration =
            Mathf.Max(
                0.001f,
                riseAndFadeDuration
            );

        float elapsedTime =
            0f;

        // 위로 이동하면서 동시에 페이드아웃
        while (elapsedTime <
               safeFadeDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    safeFadeDuration
                );

            float easedTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            transform.localPosition =
                Vector3.Lerp(
                    riseStartPosition,
                    riseTargetPosition,
                    easedTime
                );

            Color fadeColor =
                baseColor;

            fadeColor.a =
                Mathf.Lerp(
                    1f,
                    0f,
                    normalizedTime
                );

            iconRenderer.color =
                fadeColor;

            yield return null;
        }

        StopAndHideImmediately();
    }

    // <변경부분> 지정한 시작 배율에서 목표 배율까지
    // SmoothStep으로 변화시킨다.
    private IEnumerator AnimateScaleRoutine(
        float fromScale,
        float toScale,
        float duration)
    {
        float safeDuration =
            Mathf.Max(
                0.001f,
                duration
            );

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    safeDuration
                );

            float easedTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            float currentScale =
                Mathf.Lerp(
                    fromScale,
                    toScale,
                    easedTime
                );

            transform.localScale =
                Vector3.one *
                currentScale;

            yield return null;
        }

        transform.localScale =
            Vector3.one *
            toScale;
    }

    // <변경부분> 현재 연출을 정리하고
    // 아이콘을 초기 비표시 상태로 복구한다.
    private void StopAndHideImmediately()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(
                playCoroutine
            );

            playCoroutine =
                null;
        }

        transform.localPosition =
            startLocalPosition;

        transform.localScale =
            Vector3.zero;

        if (iconRenderer != null)
        {
            Color hiddenColor =
                baseColor;

            hiddenColor.a =
                0f;

            iconRenderer.color =
                hiddenColor;

            iconRenderer.enabled =
                false;
        }
    }
}
