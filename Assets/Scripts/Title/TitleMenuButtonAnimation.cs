using System.Collections;
using UnityEngine;
using TMPro;

// 타이틀 메뉴 버튼의
// 기본 부유 / 선택 확대 / 텍스트 색상 변화를 담당한다.
[RequireComponent(typeof(RectTransform))]
public class TitleMenuButtonAnimation : MonoBehaviour
{
    [Header("Target")]

    // 실제로 움직이고 확대할 버튼 RectTransform
    [SerializeField]
    private RectTransform targetRect;

    // 버튼에 표시되는 TextMeshPro 텍스트
    [SerializeField]
    private TMP_Text buttonText;


    [Header("Floating")]

    // 평상시 위아래로 움직이는 거리
    [SerializeField]
    private float floatingDistance = 6f;

    // 위아래 부유 속도
    [SerializeField]
    private float floatingSpeed = 1.2f;

    // 여러 버튼이 완전히 같은 타이밍으로 움직이지 않도록
    // 버튼마다 다른 값을 줄 수 있는 시작 위상
    [SerializeField]
    private float floatingPhaseOffset = 0f;


    [Header("Selected Scale")]

    // 선택 후 최종 버튼 크기
    [SerializeField]
    private float selectedScale = 1.1f;

    // 선택 순간 살짝 더 커졌다가 돌아오는 크기
    [SerializeField]
    private float selectedOvershootScale = 1.15f;

    // 선택 확대 전체 시간
    [SerializeField]
    private float selectDuration = 0.18f;


    [Header("Text Color")]

    // 기본 상태 글자 색상
    [SerializeField]
    private Color normalTextColor = Color.white;

    // 버튼 선택 상태 글자 색상
    [SerializeField]
    private Color selectedTextColor = Color.black;


    [Header("Text Outline")]

    // 글자 외곽선 색상
    [SerializeField]
    private Color outlineColor = Color.black;

    // TextMeshPro Outline 굵기
    // 0 ~ 1 범위에서 사용
    [Range(0f, 1f)]
    [SerializeField]
    private float outlineWidth = 0.2f;


    // 현재 선택 여부
    private bool isSelected;

    // 처음 위치
    private Vector2 baseAnchoredPosition;

    // 처음 스케일
    private Vector3 baseLocalScale;

    // 선택 확대 코루틴
    private Coroutine selectCoroutine;


    private void Awake()
    {
        // Target을 연결하지 않았다면
        // 현재 오브젝트의 RectTransform 자동 사용
        if (targetRect == null)
        {
            targetRect =
                GetComponent<RectTransform>();
        }


        // 버튼 Text를 연결하지 않았다면
        // 자식에서 자동 검색
        if (buttonText == null)
        {
            buttonText =
                GetComponentInChildren<TMP_Text>();
        }


        // 처음 위치 저장
        baseAnchoredPosition =
            targetRect.anchoredPosition;

        // 처음 크기 저장
        baseLocalScale =
            targetRect.localScale;


        // 텍스트 기본 상태 적용
        ApplyNormalTextVisual();
    }


    private void Update()
    {
        if (targetRect == null)
        {
            return;
        }


        // =========================================================
        // 버튼 기본 부유 애니메이션
        // =========================================================

        float floatingY =
            Mathf.Sin(
                Time.unscaledTime *
                floatingSpeed +
                floatingPhaseOffset
            ) *
            floatingDistance;


        targetRect.anchoredPosition =
            baseAnchoredPosition +
            new Vector2(
                0f,
                floatingY
            );
    }


    // =========================================================
    // SELECT
    // =========================================================

    // 버튼 첫 클릭 시 실행
    public void PlaySelectAnimation()
    {
        isSelected = true;


        // 선택 즉시 글자를 검은색으로 변경
        ApplySelectedTextVisual();


        if (selectCoroutine != null)
        {
            StopCoroutine(
                selectCoroutine
            );
        }


        selectCoroutine =
            StartCoroutine(
                PlaySelectRoutine()
            );
    }


    // 버튼이 살짝 튀었다가
    // 선택 크기로 안착하는 애니메이션
    private IEnumerator PlaySelectRoutine()
    {
        float halfDuration =
            Mathf.Max(
                0.01f,
                selectDuration * 0.5f
            );


        Vector3 overshootScale =
            baseLocalScale *
            selectedOvershootScale;


        Vector3 finalSelectedScale =
            baseLocalScale *
            selectedScale;


        // 현재 크기 → 살짝 크게
        yield return
            ScaleRoutine(
                targetRect.localScale,
                overshootScale,
                halfDuration
            );


        // 크게 튄 상태 → 선택 크기
        yield return
            ScaleRoutine(
                targetRect.localScale,
                finalSelectedScale,
                halfDuration
            );


        selectCoroutine = null;
    }


    // =========================================================
    // RESET
    // =========================================================

    // 다른 버튼 선택 또는 배경 클릭 시
    // 버튼을 기본 상태로 복원
    public void ResetSelection()
    {
        isSelected = false;


        if (selectCoroutine != null)
        {
            StopCoroutine(
                selectCoroutine
            );

            selectCoroutine = null;
        }


        if (targetRect != null)
        {
            // 버튼 크기 원상 복구
            targetRect.localScale =
                baseLocalScale;
        }


        // 글자를 다시 흰색으로 복구
        ApplyNormalTextVisual();
    }


    // 현재 버튼 선택 여부 반환
    public bool IsSelected()
    {
        return isSelected;
    }


    // =========================================================
    // TEXT VISUAL
    // =========================================================

    // 기본 상태
    // White Text + Black Outline
    private void ApplyNormalTextVisual()
    {
        if (buttonText == null)
        {
            return;
        }


        // 기본 글자색 = 흰색
        buttonText.color =
            normalTextColor;


        // 아웃라인 적용
        ApplyTextOutline();
    }


    // 선택 상태
    // Black Text + Black Outline
    private void ApplySelectedTextVisual()
    {
        if (buttonText == null)
        {
            return;
        }


        // 선택 글자색 = 검은색
        buttonText.color =
            selectedTextColor;


        // 아웃라인은 계속 검은색 유지
        ApplyTextOutline();
    }


    // TextMeshPro 아웃라인 설정
    private void ApplyTextOutline()
    {
        if (buttonText == null)
        {
            return;
        }


        // 해당 텍스트 전용 Material Instance를 사용해서
        // 다른 TMP 텍스트에 영향을 주지 않도록 한다.
        Material material =
            buttonText.fontMaterial;


        material.SetColor(
            ShaderUtilities.ID_OutlineColor,
            outlineColor
        );


        material.SetFloat(
            ShaderUtilities.ID_OutlineWidth,
            outlineWidth
        );


        // Material 변경사항 반영
        buttonText.UpdateMeshPadding();
    }


    // =========================================================
    // SCALE
    // =========================================================

    // 시작 크기에서 목표 크기까지
    // 부드럽게 보간하는 공통 코루틴
    private IEnumerator ScaleRoutine(
        Vector3 startScale,
        Vector3 endScale,
        float duration)
    {
        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            // 부드러운 Ease In-Out
            float smoothT =
                t * t *
                (3f - 2f * t);


            targetRect.localScale =
                Vector3.Lerp(
                    startScale,
                    endScale,
                    smoothT
                );


            yield return null;
        }


        // 마지막 크기 정확히 적용
        targetRect.localScale =
            endScale;
    }
}
