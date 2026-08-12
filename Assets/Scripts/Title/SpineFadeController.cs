using System.Collections;
using UnityEngine;
using Spine;
using Spine.Unity;

public class SpineFadeController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Fade Setting")]

    // Inspector에서 Spine 전체 투명도를 0 ~ 1 범위로 직접 조절
    [Range(0f, 1f)]
    [SerializeField] private float alpha = 1f;

    // 마지막으로 적용한 Alpha 값 저장
    private float lastAppliedAlpha = -1f;

    // 오브젝트 시작 시 Inspector의 Alpha 값을 Spine에 적용
    private void Start()
    {
        ApplyAlpha(alpha);
    }

    // 실행 중 Inspector의 Alpha 값을 변경하면 즉시 반영
    private void Update()
    {
        if (!Mathf.Approximately(alpha, lastAppliedAlpha))
        {
            ApplyAlpha(alpha);
        }
    }

    // Spine 전체를 현재 Alpha에서 0까지 서서히 투명하게 만듦
    public void FadeOut(float duration)
    {
        StartCoroutine(FadeRoutine(alpha, 0f, duration));
    }

    // Spine 전체를 현재 Alpha에서 1까지 서서히 나타나게 만듦
    public void FadeIn(float duration)
    {
        StartCoroutine(FadeRoutine(alpha, 1f, duration));
    }

    // Skeleton 전체 Alpha를 시간에 따라 변경
    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
        {
            yield break;
        }

        // 시간이 0 이하라면 즉시 최종 Alpha 적용
        if (duration <= 0f)
        {
            alpha = endAlpha;
            ApplyAlpha(alpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // 현재 페이드 Alpha 계산
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);

            // Inspector의 Alpha 값도 현재 상태와 동기화
            alpha = currentAlpha;

            // 실제 Spine Skeleton에 Alpha 적용
            ApplyAlpha(currentAlpha);

            yield return null;
        }

        // 마지막 Alpha 값을 정확하게 고정
        alpha = endAlpha;
        ApplyAlpha(endAlpha);
    }

    // Spine Skeleton의 RGB는 유지하고 Alpha만 변경
    private void ApplyAlpha(float targetAlpha)
    {
        if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
        {
            return;
        }

        // 현재 Skeleton 색상 가져오기
        Color currentColor = skeletonAnimation.Skeleton.GetColor();

        // Alpha 값만 변경
        currentColor.a = targetAlpha;

        // 변경된 색상을 Skeleton 전체에 적용
        skeletonAnimation.Skeleton.SetColor(currentColor);

        // 마지막 적용값 저장
        lastAppliedAlpha = targetAlpha;
    }
}