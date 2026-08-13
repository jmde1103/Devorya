using System.Collections;
using UnityEngine;

// 타이틀 씬에서 다른 씬으로 넘어가기 전
// 화면 전체 확대 + 블랙 페이드 전환을 담당한다.
public class TitleSceneTransitionController : MonoBehaviour
{
    [Header("Zoom Target")]

    // 확대할 타이틀 전체 Root.
    // SpriteRenderer 기반 오브젝트들이 들어있는 부모 Transform도 사용 가능.
    [SerializeField]
    private Transform transitionRoot;


    [Header("Black Fade")]

    // 화면 전체를 덮는 검은색 UI 오브젝트의 CanvasGroup
    [SerializeField]
    private CanvasGroup blackFadeCanvasGroup;


    [Header("Zoom Setting")]

    // 최종 확대 배율
    [SerializeField]
    private float targetScale = 1.12f;

    // 전체 전환 시간
    [SerializeField]
    private float transitionDuration = 0.8f;


    [Header("Fade Setting")]

    // 전체 전환 중 블랙 페이드가 시작되는 시점
    // 0.0 = 처음부터
    // 0.4 = 확대가 40% 진행된 후부터
    [Range(0f, 1f)]
    [SerializeField]
    private float fadeStartNormalized = 0.35f;

    // 최종 블랙 Alpha
    [Range(0f, 1f)]
    [SerializeField]
    private float targetFadeAlpha = 1f;


    // 처음 화면 크기
    private Vector3 baseScale;

    // 중복 실행 방지
    private bool isPlaying;


    private void Awake()
    {
        // 처음 크기 저장
        if (transitionRoot != null)
        {
            baseScale =
                transitionRoot.localScale;
        }


        // 시작 시 검은 화면은 완전히 숨김
        if (blackFadeCanvasGroup != null)
        {
            blackFadeCanvasGroup.alpha = 0f;

            // 평상시에는 버튼 클릭을 방해하지 않도록 함
            blackFadeCanvasGroup.blocksRaycasts = false;
            blackFadeCanvasGroup.interactable = false;
        }
    }


    // =========================================================
    // TRANSITION
    // =========================================================

    // 확대 + 블랙 페이드 전환 실행
    public IEnumerator PlayTransition()
    {
        if (isPlaying)
        {
            yield break;
        }

        isPlaying = true;


        // 전환 시작 후 추가 입력 차단
        if (blackFadeCanvasGroup != null)
        {
            blackFadeCanvasGroup.blocksRaycasts = true;
        }


        Vector3 startScale =
            transitionRoot != null
                ? transitionRoot.localScale
                : Vector3.one;


        Vector3 endScale =
            baseScale *
            targetScale;


        float elapsed = 0f;


        while (elapsed < transitionDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    transitionDuration
                );


            // 전체 움직임을 부드럽게 보간
            float smoothT =
                t * t *
                (3f - 2f * t);


            // =====================================================
            // 화면 전체 확대
            // =====================================================

            if (transitionRoot != null)
            {
                transitionRoot.localScale =
                    Vector3.Lerp(
                        startScale,
                        endScale,
                        smoothT
                    );
            }


            // =====================================================
            // 블랙 페이드
            // =====================================================

            if (blackFadeCanvasGroup != null)
            {
                // 지정 시점 전까지는 Alpha 0 유지
                float fadeT =
                    Mathf.InverseLerp(
                        fadeStartNormalized,
                        1f,
                        t
                    );


                // 블랙도 갑자기 나타나지 않고
                // 부드럽게 화면 전체를 덮도록 보간
                float fadeSmoothT =
                    fadeT *
                    fadeT *
                    (3f - 2f * fadeT);


                blackFadeCanvasGroup.alpha =
                    Mathf.Lerp(
                        0f,
                        targetFadeAlpha,
                        fadeSmoothT
                    );
            }


            yield return null;
        }


        // =========================================================
        // 최종 상태 정확하게 적용
        // =========================================================

        if (transitionRoot != null)
        {
            transitionRoot.localScale =
                endScale;
        }


        if (blackFadeCanvasGroup != null)
        {
            blackFadeCanvasGroup.alpha =
                targetFadeAlpha;
        }
    }
}
