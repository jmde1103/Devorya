using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 타이틀 화면의 검은 픽셀들을 관리한다.
// 평상시에는 화면 곳곳에서 부유하고,
// 메뉴 선택 시 일부 픽셀은 버튼으로 흡수되어 사라지며
// 남은 픽셀은 미리 지정한 3가지 패턴 중 하나의 위치로 이동한다.
public class TitleFloatingPixelEffect : MonoBehaviour
{
    [System.Serializable]
    public class PixelPattern
    {
        [Header("Pattern Points")]

        // 이 패턴에서 살아남은 픽셀들이 배치될 위치
        // 현재 구조에서는 3개를 연결하는 것을 기준으로 사용
        [SerializeField]
        private List<RectTransform> points =
            new List<RectTransform>();

        public List<RectTransform> Points => points;
    }


    [Header("Pixels")]

    // 화면에 배치된 검은 픽셀 목록
    [SerializeField]
    private List<RectTransform> pixelRects =
        new List<RectTransform>();


    [Header("Default Floating")]

    // 평상시 각 픽셀이 원래 위치 주변에서 움직이는 거리
    [SerializeField]
    private float floatRadius = 15f;

    // 평상시 최소 부유 속도
    [SerializeField]
    private float minFloatSpeed = 0.35f;

    // 평상시 최대 부유 속도
    [SerializeField]
    private float maxFloatSpeed = 0.8f;


    [Header("Gather")]

    // 버튼으로 모이는 전체 시간
    [SerializeField]
    private float gatherDuration = 0.4f;

    // 버튼 안으로 흡수되어 사라질 픽셀 개수
    [SerializeField]
    private int absorbedPixelCount = 5;

    // 흡수되는 픽셀이 버튼 중심 주변으로 들어가는 범위
    [SerializeField]
    private float absorbRadius = 12f;

    // 전체 이동 진행률 중
    // 이 시점부터 흡수 픽셀 Fade Out 시작
    [Range(0f, 1f)]
    [SerializeField]
    private float absorbFadeStart = 0.35f;


    [Header("Gathered Floating")]

    // 패턴 위치에 도착한 픽셀이
    // 해당 위치 주변에서 움직이는 거리
    [SerializeField]
    private float gatheredFloatRadius = 10f;

    // 패턴 위치 주변 부유 속도
    [SerializeField]
    private float gatheredFloatSpeed = 0.8f;


    [Header("Reset")]

    // 버튼 외부 클릭 후 원위치로 돌아가는 시간
    [SerializeField]
    private float resetDuration = 0.4f;


    // =========================================================
    // RUNTIME DATA
    // =========================================================

    // 처음 화면에 배치한 픽셀 위치
    private readonly List<Vector2> basePositions =
        new List<Vector2>();

    // 각 픽셀별 랜덤 부유 속도
    private readonly List<float> floatingSpeeds =
        new List<float>();

    // 각 픽셀별 랜덤 시작 위상
    private readonly List<float> floatingPhases =
        new List<float>();

    // 패턴 위치로 이동한 후
    // 살아남은 픽셀들의 기준 위치
    private readonly List<Vector2> gatheredBasePositions =
        new List<Vector2>();

    // 각 픽셀의 Alpha 조절용 CanvasGroup
    private readonly List<CanvasGroup> pixelCanvasGroups =
        new List<CanvasGroup>();

    // 현재 선택에서 흡수된 픽셀인지 여부
    private readonly List<bool> absorbedPixels =
        new List<bool>();


    // 현재 메뉴 선택 상태인지 여부
    private bool isGathered;

    // 이동 또는 복귀 애니메이션 중인지 여부
    private bool isAnimating;

    // 현재 효과 코루틴
    private Coroutine effectCoroutine;


    private void Awake()
    {
        CachePixelData();
    }


    private void Update()
    {
        // 코루틴이 이동을 제어하는 동안에는
        // Update에서 위치를 변경하지 않는다.
        if (isAnimating)
        {
            return;
        }


        // =========================================================
        // 버튼 주변 패턴 위치에 모인 상태
        // =========================================================

        if (isGathered &&
            gatheredBasePositions.Count == pixelRects.Count)
        {
            for (int i = 0; i < pixelRects.Count; i++)
            {
                RectTransform pixel =
                    pixelRects[i];

                if (pixel == null)
                {
                    continue;
                }

                // 흡수되어 사라진 픽셀은 움직이지 않는다.
                if (absorbedPixels[i])
                {
                    continue;
                }


                float time =
                    Time.unscaledTime *
                    gatheredFloatSpeed *
                    floatingSpeeds[i] +
                    floatingPhases[i];


                float x =
                    Mathf.Sin(time) *
                    gatheredFloatRadius;

                float y =
                    Mathf.Cos(time * 0.81f) *
                    gatheredFloatRadius;


                // 지정된 패턴 위치 주변에서만
                // 작은 범위로 둥둥 움직인다.
                pixel.anchoredPosition =
                    gatheredBasePositions[i] +
                    new Vector2(x, y);
            }

            return;
        }


        // =========================================================
        // 기본 화면 부유 상태
        // =========================================================

        for (int i = 0; i < pixelRects.Count; i++)
        {
            RectTransform pixel =
                pixelRects[i];

            if (pixel == null)
            {
                continue;
            }


            float time =
                Time.unscaledTime *
                floatingSpeeds[i] +
                floatingPhases[i];


            float x =
                Mathf.Sin(time) *
                floatRadius;

            float y =
                Mathf.Cos(time * 0.73f) *
                floatRadius;


            // 처음 배치 위치 주변에서 천천히 부유
            pixel.anchoredPosition =
                basePositions[i] +
                new Vector2(x, y);
        }
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    // 픽셀의 초기 위치와
    // 랜덤 애니메이션 정보를 저장한다.
    private void CachePixelData()
    {
        basePositions.Clear();
        floatingSpeeds.Clear();
        floatingPhases.Clear();
        gatheredBasePositions.Clear();
        pixelCanvasGroups.Clear();
        absorbedPixels.Clear();


        for (int i = 0; i < pixelRects.Count; i++)
        {
            RectTransform pixel =
                pixelRects[i];


            if (pixel == null)
            {
                basePositions.Add(Vector2.zero);
                floatingSpeeds.Add(minFloatSpeed);
                floatingPhases.Add(0f);
                pixelCanvasGroups.Add(null);
                absorbedPixels.Add(false);

                continue;
            }


            // 현재 Inspector에서 직접 배치한 위치를 저장
            basePositions.Add(
                pixel.anchoredPosition
            );


            // 각 픽셀이 똑같이 움직이지 않도록
            // 서로 다른 속도를 부여
            floatingSpeeds.Add(
                Random.Range(
                    minFloatSpeed,
                    maxFloatSpeed
                )
            );


            // 시작 위상도 랜덤하게 설정
            floatingPhases.Add(
                Random.Range(
                    0f,
                    Mathf.PI * 2f
                )
            );


            // 투명도 제어용 CanvasGroup
            CanvasGroup canvasGroup =
                pixel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    pixel.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;

            pixelCanvasGroups.Add(
                canvasGroup
            );


            absorbedPixels.Add(false);
        }
    }


    // =========================================================
    // GATHER
    // =========================================================

    // 지정 버튼으로 픽셀을 모은다.
    // 살아남은 픽셀은 전달받은 패턴 3개 중
    // 하나를 랜덤 선택해 해당 Point 위치로 이동한다.
    public void GatherTo(
        RectTransform targetRect,
        List<PixelPattern> patterns)
    {
        if (targetRect == null)
        {
            return;
        }

        if (patterns == null ||
            patterns.Count == 0)
        {
            Debug.LogError(
                "TitleFloatingPixelEffect: " +
                "사용할 Pixel Pattern이 없습니다."
            );

            return;
        }


        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );
        }


        // 새 메뉴를 선택할 때
        // 이전 선택에서 사라졌던 픽셀 Alpha 복구
        RestoreAllPixelAlpha();


        // 3가지 패턴 중 하나 랜덤 선택
        PixelPattern selectedPattern =
            patterns[
                Random.Range(
                    0,
                    patterns.Count
                )
            ];


        effectCoroutine =
            StartCoroutine(
                GatherRoutine(
                    targetRect,
                    selectedPattern
                )
            );
    }


    // 픽셀을 버튼과 패턴 포인트로 이동시키는 실제 코루틴
    private IEnumerator GatherRoutine(
        RectTransform targetRect,
        PixelPattern selectedPattern)
    {
        isAnimating = true;

        float elapsed = 0f;


        // 현재 시작 위치
        List<Vector2> startPositions =
            new List<Vector2>();

        // 최종 목적 위치
        List<Vector2> targetPositions =
            new List<Vector2>();


        // 이번 선택에서 흡수될 5개 랜덤 선택
        SelectRandomAbsorbedPixels();


        // 살아남는 픽셀 인덱스를 따로 저장
        List<int> survivorIndexes =
            new List<int>();


        for (int i = 0; i < absorbedPixels.Count; i++)
        {
            if (!absorbedPixels[i] &&
                pixelRects[i] != null)
            {
                survivorIndexes.Add(i);
            }
        }


        // 살아남는 픽셀 수보다
        // 패턴 Point가 부족하면 오류
        if (selectedPattern == null ||
            selectedPattern.Points == null ||
            selectedPattern.Points.Count <
            survivorIndexes.Count)
        {
            Debug.LogError(
                "TitleFloatingPixelEffect: " +
                "선택된 Pattern의 Point 개수가 부족합니다. " +
                $"필요: {survivorIndexes.Count}"
            );

            isAnimating = false;
            effectCoroutine = null;

            yield break;
        }


        RectTransform pixelParent =
            transform as RectTransform;


        // 버튼 중심 위치
        Vector2 targetButtonPosition =
            Vector2.zero;


        if (pixelParent != null)
        {
            Vector3 targetLocal =
                pixelParent.InverseTransformPoint(
                    targetRect.position
                );

            targetButtonPosition =
                new Vector2(
                    targetLocal.x,
                    targetLocal.y
                );
        }


        // 살아남은 픽셀이
        // Pattern Point를 순서대로 사용하도록 번호 관리
        int survivorPointIndex = 0;


        // =========================================================
        // 픽셀별 목적 위치 계산
        // =========================================================

        for (int i = 0; i < pixelRects.Count; i++)
        {
            RectTransform pixel =
                pixelRects[i];


            if (pixel == null)
            {
                startPositions.Add(Vector2.zero);
                targetPositions.Add(Vector2.zero);

                continue;
            }


            // 현재 위치 저장
            startPositions.Add(
                pixel.anchoredPosition
            );


            // -----------------------------------------------------
            // 흡수되는 픽셀
            // -----------------------------------------------------

            if (absorbedPixels[i])
            {
                // 버튼 중심 근처로 빨려들어가며 사라짐
                Vector2 absorbOffset =
                    Random.insideUnitCircle *
                    absorbRadius;


                targetPositions.Add(
                    targetButtonPosition +
                    absorbOffset
                );

                continue;
            }


            // -----------------------------------------------------
            // 살아남는 픽셀
            // -----------------------------------------------------

            RectTransform point =
                selectedPattern.Points[
                    survivorPointIndex
                ];

            survivorPointIndex++;


            if (point == null)
            {
                targetPositions.Add(
                    pixel.anchoredPosition
                );

                continue;
            }


            Vector3 pointLocal =
                pixelParent.InverseTransformPoint(
                    point.position
                );


            // 네가 Scene에서 직접 배치한 Point 위치를
            // 정확한 최종 목적지로 사용
            targetPositions.Add(
                new Vector2(
                    pointLocal.x,
                    pointLocal.y
                )
            );
        }


        // =========================================================
        // 이동 + 흡수 Fade Out
        // =========================================================

        while (elapsed < gatherDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    gatherDuration
                );


            // 뒤로 갈수록 빨려드는 느낌
            float moveT =
                t * t;


            for (int i = 0; i < pixelRects.Count; i++)
            {
                RectTransform pixel =
                    pixelRects[i];


                if (pixel == null)
                {
                    continue;
                }


                pixel.anchoredPosition =
                    Vector2.Lerp(
                        startPositions[i],
                        targetPositions[i],
                        moveT
                    );


                // 흡수되는 5개만 Fade Out
                if (absorbedPixels[i] &&
                    pixelCanvasGroups[i] != null)
                {
                    float fadeT =
                        Mathf.InverseLerp(
                            absorbFadeStart,
                            1f,
                            t
                        );


                    pixelCanvasGroups[i].alpha =
                        Mathf.Lerp(
                            1f,
                            0f,
                            fadeT
                        );
                }
            }


            yield return null;
        }


        // =========================================================
        // 최종 상태
        // =========================================================

        gatheredBasePositions.Clear();


        for (int i = 0; i < pixelRects.Count; i++)
        {
            RectTransform pixel =
                pixelRects[i];


            if (pixel != null)
            {
                pixel.anchoredPosition =
                    targetPositions[i];
            }


            // 흡수된 픽셀은 완전히 숨김
            if (absorbedPixels[i] &&
                pixelCanvasGroups[i] != null)
            {
                pixelCanvasGroups[i].alpha =
                    0f;
            }


            // 모든 픽셀의 최종 위치 저장
            // 흡수되지 않은 픽셀만 Update에서 사용됨
            gatheredBasePositions.Add(
                targetPositions[i]
            );
        }


        isGathered = true;
        isAnimating = false;
        effectCoroutine = null;
    }


    // =========================================================
    // RANDOM ABSORB
    // =========================================================

    // 전체 픽셀 중 지정 개수를
    // 랜덤으로 흡수 대상으로 선택
    private void SelectRandomAbsorbedPixels()
    {
        for (int i = 0; i < absorbedPixels.Count; i++)
        {
            absorbedPixels[i] = false;
        }


        List<int> availableIndexes =
            new List<int>();


        for (int i = 0; i < pixelRects.Count; i++)
        {
            if (pixelRects[i] != null)
            {
                availableIndexes.Add(i);
            }
        }


        int absorbCount =
            Mathf.Min(
                absorbedPixelCount,
                availableIndexes.Count
            );


        for (int i = 0; i < absorbCount; i++)
        {
            int randomListIndex =
                Random.Range(
                    0,
                    availableIndexes.Count
                );


            int pixelIndex =
                availableIndexes[
                    randomListIndex
                ];


            absorbedPixels[pixelIndex] =
                true;


            availableIndexes.RemoveAt(
                randomListIndex
            );
        }
    }


    // =========================================================
    // RESET
    // =========================================================

    // 버튼 외부 클릭 시
    // 모든 픽셀을 처음 위치로 되돌린다.
    public void ResetToDefault()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );
        }


        effectCoroutine =
            StartCoroutine(
                ResetRoutine()
            );
    }


    // 원래 위치 + Alpha 1 상태로
    // 부드럽게 복귀
    private IEnumerator ResetRoutine()
    {
        isAnimating = true;

        float elapsed = 0f;


        List<Vector2> startPositions =
            new List<Vector2>();

        List<float> startAlphas =
            new List<float>();


        for (int i = 0; i < pixelRects.Count; i++)
        {
            if (pixelRects[i] != null)
            {
                startPositions.Add(
                    pixelRects[i].anchoredPosition
                );
            }
            else
            {
                startPositions.Add(
                    Vector2.zero
                );
            }


            if (pixelCanvasGroups[i] != null)
            {
                startAlphas.Add(
                    pixelCanvasGroups[i].alpha
                );
            }
            else
            {
                startAlphas.Add(1f);
            }
        }


        while (elapsed < resetDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    resetDuration
                );


            // 부드러운 Ease In-Out
            float smoothT =
                t * t *
                (3f - 2f * t);


            for (int i = 0; i < pixelRects.Count; i++)
            {
                RectTransform pixel =
                    pixelRects[i];


                if (pixel == null)
                {
                    continue;
                }


                // 처음 위치로 복귀
                pixel.anchoredPosition =
                    Vector2.Lerp(
                        startPositions[i],
                        basePositions[i],
                        smoothT
                    );


                // 흡수되어 사라졌던 픽셀도
                // 원래 투명도로 복귀
                if (pixelCanvasGroups[i] != null)
                {
                    pixelCanvasGroups[i].alpha =
                        Mathf.Lerp(
                            startAlphas[i],
                            1f,
                            smoothT
                        );
                }
            }


            yield return null;
        }


        // 최종 상태 정확히 고정
        for (int i = 0; i < pixelRects.Count; i++)
        {
            if (pixelRects[i] != null)
            {
                pixelRects[i].anchoredPosition =
                    basePositions[i];
            }


            if (pixelCanvasGroups[i] != null)
            {
                pixelCanvasGroups[i].alpha =
                    1f;
            }


            absorbedPixels[i] = false;
        }


        gatheredBasePositions.Clear();

        isGathered = false;
        isAnimating = false;
        effectCoroutine = null;
    }


    // 모든 픽셀 Alpha를 즉시 1로 복구
    private void RestoreAllPixelAlpha()
    {
        for (int i = 0;
             i < pixelCanvasGroups.Count;
             i++)
        {
            if (pixelCanvasGroups[i] != null)
            {
                pixelCanvasGroups[i].alpha =
                    1f;
            }
        }
    }
}