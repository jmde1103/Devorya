using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// <변경부분> 공통 PieceObject에서 Sprite 외형과 Spine 외형을 교체 관리하는 컨트롤러
public class PieceVisualController : MonoBehaviour
{
    [Header("Sprite Visual")]
    // 기존 PieceObject에 붙어 있던 SpriteRenderer
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Spine Visual")]
    // Spine Visual 프리팹이 생성될 부모 위치
    [SerializeField] private Transform spineVisualRoot;

    // 현재 생성된 Spine Visual 오브젝트
    private GameObject currentSpineVisualObject;

    // 현재 적용 중인 Spine Visual 프리팹
    private GameObject currentSpineVisualPrefab;

    // 현재 Spine 애니메이션 컨트롤러
    private PieceSpineAnimationController currentSpineAnimationController;


    // <변경부분>
    // 현재 Spine Visual이 Player Color Adjustment 대상인지 저장.
    //
    // Spine이 LateUpdate에서 Material/Mesh를 다시 갱신한 뒤에도
    // 동일한 시각 상태를 다시 적용하기 위해 사용한다.
    private bool currentShouldApplyPlayerColorAdjustment;


    // <변경부분>
    // 현재 Spine Visual의 SkeletonRenderer.
    //
    // Spine의 Mesh / Material 갱신이 완료된 직후
    // Color Adjustment를 다시 적용하기 위해 사용한다.
    private SkeletonRenderer currentSkeletonRenderer;


    // <변경부분>
    // 현재 Spine Visual 아래의 Renderer 목록.
    //
    // OnMeshAndMaterialsUpdated가 반복 호출될 때마다
    // GetComponentsInChildren을 다시 호출하지 않도록 캐시한다.
    private Renderer[] currentSpineRenderers;


    // <변경부분>
    // Renderer의 Shared Material을 읽을 때
    // 매번 Material[] 배열을 새로 생성하지 않기 위한 재사용 버퍼.
    private readonly List<Material> sharedMaterialBuffer =
        new List<Material>(4);


    // <변경부분>
    // =====================================================
    // Player Spine Color Adjustment
    // =====================================================

    [Header("Player Spine Color Adjustment")]

    // Player 기물의 Spine 색상 보정을 사용할지 여부.
    //
    // false:
    // 모든 기물이 기존 Material 색상을 그대로 사용.
    //
    // true:
    // 아래 조건에 해당하는 Player Spine에만
    // Hue / Saturation / Brightness를 적용.
    [SerializeField]
    private bool usePlayerSpineColorAdjustment =
        true;


    // <변경부분>
    // true:
    // 흡수된 Player 외형에만 색상 보정을 적용한다.
    //
    // 따라서 흡수 전 기본 데보리아 기물은
    // 기존 검은색 외형을 그대로 유지한다.
    //
    // false:
    // 모든 Player Spine 외형에 색상 보정을 적용한다.
    [SerializeField]
    private bool applyOnlyToAbsorbedPlayerVisual =
        true;


    // 현재 Spine Shader의:
    // _Hue("Hue", Range(-0.5, 0.5))
    [SerializeField, Range(-0.5f, 0.5f)]
    private float playerHue =
        0f;


    // 현재 Spine Shader의:
    // _Saturation("Saturation", Range(0, 2))
    //
    // 0 = 완전 무채색
    // 1 = 원본 채도
    [SerializeField, Range(0f, 2f)]
    private float playerSaturation =
        0f;


    // 현재 Spine Shader의:
    // _Brightness("Brightness", Range(0, 2))
    //
    // 1 = 원본 밝기
    [SerializeField, Range(0f, 2f)]
    private float playerBrightness =
        1f;


    // <변경부분>
    // =====================================================
    // Player Spine Color Transition
    // =====================================================

    [Header("Player Spine Color Transition")]


    // <변경부분>
    // 현재 흡수 Player의 최종 표시를
    // Color Adjustment 적용 상태로 사용할지 여부.
    //
    // true:
    // 현재 Inspector의 Hue / Saturation / Brightness 사용.
    //
    // false:
    // 원본 색상 사용.
    //
    // 현재 기본값은 회색 외형이므로 true.
    // 이후 설정창의 "원래색 / 회색" 옵션과 연결할 수 있다.
    [SerializeField]
    private bool useAdjustedPlayerColor =
        true;


    // <변경부분>
    // 원본 색상과 보정 색상 사이를
    // Coroutine으로 부드럽게 전환할지 여부.
    [SerializeField]
    private bool animatePlayerColorTransition =
        true;


    // <변경부분>
    // 원본 색상 → 보정 색상 전환 시간.
    //
    // 기본 0.5초.
    // Inspector에서 실제 화면을 보면서 조절한다.
    [SerializeField, Min(0f)]
    private float playerColorTransitionDuration =
        0.5f;


    // <변경부분>
    // 색상 전환 속도 곡선.
    //
    // 처음과 끝을 약간 부드럽게 처리하기 위해
    // 기본 EaseInOut을 사용한다.
    [SerializeField]
    private AnimationCurve playerColorTransitionCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // <변경부분>
    // 현재 실행 중인 색상 전환 Coroutine.
    private Coroutine playerColorTransitionCoroutine;


    // <변경부분>
    // Born 등의 등장 연출이 끝난 뒤
    // 색상 전환을 시작해야 하는 상태인지 저장한다.
    private bool isPlayerColorTransitionPrepared;


    // <변경부분>
    // 현재 화면에 적용 중인 Color Adjustment 진행도.
    //
    // 0 = Material 원본 색상
    // 1 = Inspector의 playerHue / playerSaturation / playerBrightness
    private float currentPlayerColorAdjustmentProgress =
        1f;


    // <변경부분>
    // Shader Property 이름을 매번 문자열로 검색하지 않도록
    // 시작 시 ID로 변환하여 재사용한다.
    private static readonly int HuePropertyId =
     Shader.PropertyToID("_Hue");

    private static readonly int SaturationPropertyId =
        Shader.PropertyToID("_Saturation");

    private static readonly int BrightnessPropertyId =
        Shader.PropertyToID("_Brightness");


    // <변경부분>
    // Spine Shader의 Material Inspector에 있는
    // "Color Adjustment" 체크박스에 대응하는 Shader Keyword.
    //
    // 공용 원본 Material은 수정하지 않고,
    // 필요한 경우 기물 전용 Runtime Material에서만 활성화한다.
    private const string ColorAdjustmentKeyword =
        "_COLOR_ADJUST";


    // <변경부분>
    // 흡수 Player 전용 Color Adjustment를 적용하기 위해
    // 원본 Spine Material과 기물 전용 Runtime Material의 대응을 저장한다.
    //
    // MaterialPropertyBlock은 사용하지 않고
    // SkeletonRenderer.CustomMaterialOverride로
    // 기물별 Material을 분리하여 적용한다.
    private readonly Dictionary<Material, Material>
        playerColorRuntimeMaterials =
            new Dictionary<Material, Material>(4);


    public PieceSpineAnimationController CurrentSpineAnimationController =>
        currentSpineAnimationController;


    private void Awake()
    {
        // 기존 SpriteRenderer 자동 탐색
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // SpineVisualRoot가 없으면 자동 생성
        if (spineVisualRoot == null)
        {
            Transform foundRoot = transform.Find("SpineVisualRoot");

            if (foundRoot != null)
            {
                spineVisualRoot = foundRoot;
            }
            else
            {
                GameObject rootObject = new GameObject("SpineVisualRoot");
                rootObject.transform.SetParent(transform, false);
                rootObject.transform.localPosition = Vector3.zero;
                rootObject.transform.localRotation = Quaternion.identity;
                rootObject.transform.localScale = Vector3.one;

                spineVisualRoot = rootObject.transform;
            }
        }
    }


    // <변경부분>
    // PieceObject가 파괴될 때 생성했던
    // 기물 전용 Runtime Material도 함께 정리한다.
    private void OnDestroy()
    {
        StopPlayerColorAdjustmentTransition();

        ClearPlayerColorRuntimeMaterials();
    }


    // <변경부분> PieceData 기준으로 Sprite 또는 Spine 외형을 적용
    public void ApplyVisual(
        PieceData pieceData,
        PieceTeam team,
        bool isAbsorbedPlayerVisual)
    {
        if (pieceData == null)
        {
            ApplySprite(
                null
            );

            return;
        }


        // <변경부분>
        // Player이면서 색상 보정 기능이 활성화되어 있고,
        // 설정에 따라 흡수 외형 조건까지 만족하는지 검사한다.
        bool shouldApplyPlayerColorAdjustment =
            ShouldApplyPlayerColorAdjustment(
                team,
                isAbsorbedPlayerVisual
            );


        GameObject spineVisualPrefab =
            pieceData.GetSpineVisualPrefab(
                team,
                isAbsorbedPlayerVisual
            );


        if (spineVisualPrefab != null)
        {
            ApplySpineVisual(
                spineVisualPrefab,
                shouldApplyPlayerColorAdjustment
            );

            return;
        }


        // 현재 색상 보정 기능은 Spine 외형에만 적용한다.
        Sprite spriteToApply =
            pieceData.GetSprite(
                team,
                isAbsorbedPlayerVisual
            );

        ApplySprite(
            spriteToApply
        );
    }

    // <변경부분> 기존 SpriteRenderer 방식으로 외형 적용
    private void ApplySprite(Sprite sprite)
    {
        ClearSpineVisual();

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.enabled = sprite != null;
    }

    // <변경부분> Spine Visual 프리팹을 생성해서 외형 적용
    private void ApplySpineVisual(
        GameObject spineVisualPrefab,
        bool shouldApplyPlayerColorAdjustment)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled =
                false;
        }


        // <변경부분>
        // 현재 외형이 회색톤 적용 대상인지 저장한다.
        //
        // 이후 Spine이 LateUpdate에서 Material을 다시 갱신하더라도
        // 이 값을 기준으로 동일한 Color Adjustment를 복구한다.
        currentShouldApplyPlayerColorAdjustment =
            shouldApplyPlayerColorAdjustment;


        // 이미 같은 Spine Visual 프리팹을 사용 중이면 새로 생성하지 않음
        if (currentSpineVisualObject != null &&
            currentSpineVisualPrefab == spineVisualPrefab)
        {
            currentSpineVisualObject.SetActive(
                true
            );


            if (currentSpineAnimationController != null)
            {
                currentSpineAnimationController.PlayIdle();
            }


            // <변경부분>
            // 이전 버전에서 생성된 Visual이거나
            // Renderer 캐시가 아직 없는 경우 다시 확보한다.
            if (currentSpineRenderers == null ||
                currentSpineRenderers.Length == 0 ||
                currentSkeletonRenderer == null)
            {
                CacheCurrentSpineRendererState();
            }


            // <변경부분>
            // 현재 회색톤 적용 여부에 맞춰
            // Spine Material 갱신 Callback 연결 상태도 갱신한다.
            UpdateSpineColorAdjustmentCallbackSubscription();


            // <변경부분>
            // 색상 보정 대상인 Spine에만
            // 기물 전용 Runtime Material Override를 적용한다.
            //
            // 색상 보정 대상이 아닌 일반 기물은
            // 원본 Spine Material 상태를 그대로 사용한다.
            if (shouldApplyPlayerColorAdjustment)
            {
                // 색상 전환을 준비 중이거나 실제 전환 중이라면
                // RefreshPieceVisual이 다시 호출되더라도
                // 최종 회색값으로 갑자기 점프하지 않고
                // 현재 진행 중인 값을 그대로 유지한다.
                if (isPlayerColorTransitionPrepared ||
                    playerColorTransitionCoroutine != null)
                {
                    ApplySpineColorAdjustmentProgress(
                        currentPlayerColorAdjustmentProgress
                    );
                }
                else
                {
                    ApplySpineColorAdjustment(
                        true
                    );
                }
            }
            else
            {
                // <변경부분>
                // 같은 Spine Visual을 계속 사용하더라도
                // 더 이상 색상 보정 대상이 아니면
                // CustomMaterialOverride를 제거하고 원본 Material로 복구한다.
                ClearPlayerColorRuntimeMaterials();
            }


            return;
        }


        ClearSpineVisual();


        if (spineVisualRoot == null)
        {
            return;
        }


        // <변경부분>
        // ClearSpineVisual()에서 초기화되므로
        // 새 Visual의 적용 상태를 다시 저장한다.
        currentShouldApplyPlayerColorAdjustment =
            shouldApplyPlayerColorAdjustment;


        currentSpineVisualPrefab =
            spineVisualPrefab;


        // 프리팹의 로컬 위치/스케일 세팅을 유지한 채
        // SpineVisualRoot 아래에 생성
        currentSpineVisualObject =
            Instantiate(
                spineVisualPrefab,
                spineVisualRoot,
                false
            );


        currentSpineAnimationController =
            currentSpineVisualObject
                .GetComponentInChildren<
                    PieceSpineAnimationController
                >();


        // <변경부분>
        // 새 Spine Visual의 Renderer와 SkeletonRenderer를
        // 한 번만 찾아서 캐시한다.
        CacheCurrentSpineRendererState();


        // <변경부분>
        // 회색톤 적용 대상인 경우에만
        // Spine Material 갱신 완료 Callback을 연결한다.
        UpdateSpineColorAdjustmentCallbackSubscription();


        if (currentSpineAnimationController != null)
        {
            currentSpineAnimationController.PlayIdle();
        }


        // <변경부분>
        // 실제 Color Adjustment 대상인 Spine에만
        // 기물 전용 Runtime Material Override를 적용한다.
        //
        // 일반 Enemy / Neutral / 흡수 전 Player는
        // 원본 Spine Renderer 상태를 그대로 유지한다.
        if (shouldApplyPlayerColorAdjustment)
        {
            ApplySpineColorAdjustment(
                true
            );
        }
    }

    // <변경부분>
    // 현재 Spine Visual의 Renderer와 SkeletonRenderer를 캐시한다.
    //
    // Spine 외형이 실제로 교체될 때만 호출하며,
    // 매 프레임 GetComponentsInChildren을 반복하지 않는다.
    private void CacheCurrentSpineRendererState()
    {
        if (currentSpineVisualObject == null)
        {
            currentSpineRenderers =
                null;

            currentSkeletonRenderer =
                null;

            return;
        }


        currentSpineRenderers =
            currentSpineVisualObject
                .GetComponentsInChildren<Renderer>(
                    true
                );


        currentSkeletonRenderer =
            currentSpineVisualObject
                .GetComponentInChildren<SkeletonRenderer>(
                    true
                );
    }


    // <변경부분>
    // Spine의 Mesh / Material 갱신 완료 Callback 연결 상태를 관리한다.
    //
    // 기본 데보리아 / Enemy / Neutral 등
    // Color Adjustment가 필요 없는 외형에는 Callback을 연결하지 않아
    // 불필요한 반복 처리를 피한다.
    private void UpdateSpineColorAdjustmentCallbackSubscription()
    {
        if (currentSkeletonRenderer == null)
        {
            return;
        }


        // 중복 등록 방지.
        currentSkeletonRenderer
            .OnMeshAndMaterialsUpdated -=
            HandleSpineMeshAndMaterialsUpdated;


        if (currentShouldApplyPlayerColorAdjustment)
        {
            currentSkeletonRenderer
                .OnMeshAndMaterialsUpdated +=
                HandleSpineMeshAndMaterialsUpdated;
        }
    }


    // <변경부분>
    // Spine이 LateUpdate에서 Mesh와 Material 갱신을 끝낸 직후 호출된다.
    //
    // 합성 / 아이템 변형 / 향후 다른 외형 변경 과정에서
    // Spine이 Renderer Material 상태를 다시 갱신하더라도
    // 흡수 Player의 Hue / Saturation / Brightness를 다시 보장한다.
    private void HandleSpineMeshAndMaterialsUpdated(
    ISkeletonRenderer spineRenderer)
    {
        if (currentShouldApplyPlayerColorAdjustment == false)
        {
            return;
        }


        // <변경부분>
        // Spine이 Material을 갱신하더라도
        // 무조건 최종 회색값을 적용하지 않는다.
        //
        // 현재 Coroutine이 진행 중이라면
        // 현재 진행도에 해당하는 색상값을 그대로 다시 적용한다.
        ApplySpineColorAdjustmentProgress(
            currentPlayerColorAdjustmentProgress
        );
    }

    // <변경부분>
    // 현재 Piece가 Player Spine Color Adjustment 대상인지 판단한다.
    private bool ShouldApplyPlayerColorAdjustment(
        PieceTeam team,
        bool isAbsorbedPlayerVisual)
    {
        // 기능 자체를 끈 경우에는 아무 기물에도 적용하지 않는다.
        if (usePlayerSpineColorAdjustment == false)
        {
            return false;
        }


        // Enemy / Neutral에는 적용하지 않는다.
        if (team != PieceTeam.Player)
        {
            return false;
        }


        // <변경부분>
        // 기본 설정에서는 흡수 전 데보리아 외형을 예외 처리한다.
        //
        // applyOnlyToAbsorbedPlayerVisual = true:
        // 흡수 외형인 Player만 회색화.
        if (applyOnlyToAbsorbedPlayerVisual &&
            isAbsorbedPlayerVisual == false)
        {
            return false;
        }


        return true;
    }

    // <변경부분>
    // 등장 / 변형 연출이 시작되기 전에
    // Color Adjustment 전환을 준비한다.
    //
    // 흡수 / 합성처럼 외형이 새로 변경된 직후 호출하면
    // Born이 재생되는 동안에는 원래 색상을 유지하고,
    // Born 종료 후 PlayPreparedPlayerColorAdjustmentTransitionRoutine()
    // 을 통해 회색으로 전환할 수 있다.
    public void PreparePlayerColorAdjustmentTransition()
    {
        if (currentShouldApplyPlayerColorAdjustment == false)
        {
            return;
        }


        StopPlayerColorAdjustmentTransition();


        // 설정에서 원래색을 사용하는 상태라면
        // 전환할 필요 없이 원래색으로 유지한다.
        if (useAdjustedPlayerColor == false)
        {
            isPlayerColorTransitionPrepared =
                false;

            currentPlayerColorAdjustmentProgress =
                0f;

            ApplySpineColorAdjustmentProgress(
                currentPlayerColorAdjustmentProgress
            );

            return;
        }


        isPlayerColorTransitionPrepared =
            true;


        // <변경부분>
        // Born이 시작되기 전에 원래 색상으로 되돌린다.
        currentPlayerColorAdjustmentProgress =
            0f;

        ApplySpineColorAdjustmentProgress(
            currentPlayerColorAdjustmentProgress
        );
    }


    // <변경부분>
    // PreparePlayerColorAdjustmentTransition()으로 준비한 전환을
    // 외부에서 비동기적으로 시작한다.
    //
    // 아이템처럼 별도의 Born 대기 없이
    // 바로 색상 전환을 시작할 때 사용한다.
    public void StartPreparedPlayerColorAdjustmentTransition()
    {
        if (isPlayerColorTransitionPrepared == false)
        {
            return;
        }


        isPlayerColorTransitionPrepared =
            false;


        StartPlayerColorAdjustmentTransitionTo(
            1f
        );
    }


    // <변경부분>
    // Born Animation 등의 연출 뒤에
    // 색상 전환이 완전히 끝날 때까지 기다려야 할 때 사용한다.
    public IEnumerator PlayPreparedPlayerColorAdjustmentTransitionRoutine()
    {
        if (isPlayerColorTransitionPrepared == false)
        {
            yield break;
        }


        StartPreparedPlayerColorAdjustmentTransition();


        while (playerColorTransitionCoroutine != null)
        {
            yield return null;
        }
    }


    // <변경부분>
    // 이후 설정창에서
    //
    // 원래색:
    // useAdjustedColor = false
    //
    // 회색:
    // useAdjustedColor = true
    //
    // 로 연결할 수 있는 공용 진입점.
    //
    // 현재는 기본값이 회색이며,
    // 실제 Settings 저장 시스템은 나중에 연결한다.
    public void SetUseAdjustedPlayerColor(
        bool useAdjustedColor,
        bool animateTransition)
    {
        useAdjustedPlayerColor =
            useAdjustedColor;

        isPlayerColorTransitionPrepared =
            false;


        float targetProgress =
            currentShouldApplyPlayerColorAdjustment &&
            useAdjustedPlayerColor
                ? 1f
                : 0f;


        if (animateTransition == false ||
            animatePlayerColorTransition == false ||
            playerColorTransitionDuration <= 0f ||
            isActiveAndEnabled == false)
        {
            StopPlayerColorAdjustmentTransition();

            currentPlayerColorAdjustmentProgress =
                targetProgress;

            ApplySpineColorAdjustmentProgress(
                currentPlayerColorAdjustmentProgress
            );

            return;
        }


        StartPlayerColorAdjustmentTransitionTo(
            targetProgress
        );
    }


    // <변경부분>
    // 현재 표시값에서 지정한 Progress까지
    // Color Adjustment Coroutine을 시작한다.
    private void StartPlayerColorAdjustmentTransitionTo(
        float targetProgress)
    {
        StopPlayerColorAdjustmentTransition();


        targetProgress =
            Mathf.Clamp01(
                targetProgress
            );


        if (animatePlayerColorTransition == false ||
            playerColorTransitionDuration <= 0f ||
            isActiveAndEnabled == false)
        {
            currentPlayerColorAdjustmentProgress =
                targetProgress;

            ApplySpineColorAdjustmentProgress(
                currentPlayerColorAdjustmentProgress
            );

            return;
        }


        playerColorTransitionCoroutine =
            StartCoroutine(
                PlayerColorAdjustmentTransitionRoutine(
                    targetProgress
                )
            );
    }


    // <변경부분>
    // 현재 색상 진행도에서 목표 진행도까지
    // 부드럽게 변화시키는 실제 Coroutine.
    private IEnumerator PlayerColorAdjustmentTransitionRoutine(
        float targetProgress)
    {
        float startProgress =
            currentPlayerColorAdjustmentProgress;

        float elapsedTime =
            0f;


        while (elapsedTime <
               playerColorTransitionDuration)
        {
            if (currentSpineVisualObject == null)
            {
                playerColorTransitionCoroutine =
                    null;

                yield break;
            }


            elapsedTime +=
                Time.deltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    playerColorTransitionDuration
                );


            float curvedTime =
                playerColorTransitionCurve != null
                    ? playerColorTransitionCurve.Evaluate(
                        normalizedTime
                    )
                    : normalizedTime;


            currentPlayerColorAdjustmentProgress =
                Mathf.Lerp(
                    startProgress,
                    targetProgress,
                    curvedTime
                );


            ApplySpineColorAdjustmentProgress(
                currentPlayerColorAdjustmentProgress
            );


            yield return null;
        }


        currentPlayerColorAdjustmentProgress =
            targetProgress;


        ApplySpineColorAdjustmentProgress(
            currentPlayerColorAdjustmentProgress
        );


        playerColorTransitionCoroutine =
            null;
    }


    // <변경부분>
    // 실행 중인 색상 전환을 안전하게 중단한다.
    private void StopPlayerColorAdjustmentTransition()
    {
        if (playerColorTransitionCoroutine == null)
        {
            return;
        }


        StopCoroutine(
            playerColorTransitionCoroutine
        );


        playerColorTransitionCoroutine =
            null;
    }


    // <변경부분>
    // 즉시 최종 색상 상태를 적용하는 호환용 함수.
    //
    // 기존 호출 구조는 유지하면서
    // 내부적으로 0~1 Progress 시스템을 사용한다.
    private void ApplySpineColorAdjustment(
        bool shouldApplyPlayerColorAdjustment)
    {
        StopPlayerColorAdjustmentTransition();

        isPlayerColorTransitionPrepared =
            false;


        currentPlayerColorAdjustmentProgress =
            shouldApplyPlayerColorAdjustment &&
            useAdjustedPlayerColor
                ? 1f
                : 0f;


        ApplySpineColorAdjustmentProgress(
            currentPlayerColorAdjustmentProgress
        );
    }


    // <변경부분>
    // 현재 SkeletonRenderer의 SkeletonDataAsset에 등록된
    // 원본 Atlas Material을 기준으로
    // 기물 전용 Runtime Material을 생성하고
    // CustomMaterialOverride에 등록한다.
    //
    // Material을 그대로 복제하므로
    // Shader / Normal Map / Texture / Keyword 설정은
    // 원본 Material 상태를 그대로 유지한다.
    private void EnsurePlayerColorRuntimeMaterials()
    {
        if (currentSkeletonRenderer == null ||
            playerColorRuntimeMaterials.Count > 0)
        {
            return;
        }


        SkeletonDataAsset skeletonDataAsset =
            currentSkeletonRenderer.SkeletonDataAsset;


        if (skeletonDataAsset == null ||
            skeletonDataAsset.atlasAssets == null)
        {
            return;
        }


        Dictionary<Material, Material> customMaterialOverride =
            null;


        for (int atlasIndex = 0;
             atlasIndex < skeletonDataAsset.atlasAssets.Length;
             atlasIndex++)
        {
            AtlasAssetBase atlasAsset =
                skeletonDataAsset.atlasAssets[atlasIndex];


            if (atlasAsset == null)
            {
                continue;
            }


            foreach (Material originalMaterial in atlasAsset.Materials)
            {
                if (originalMaterial == null ||
                    playerColorRuntimeMaterials.ContainsKey(
                        originalMaterial
                    ))
                {
                    continue;
                }


                bool hasHue =
                    originalMaterial.HasProperty(
                        HuePropertyId
                    );

                bool hasSaturation =
                    originalMaterial.HasProperty(
                        SaturationPropertyId
                    );

                bool hasBrightness =
                    originalMaterial.HasProperty(
                        BrightnessPropertyId
                    );


                if (hasHue == false ||
                    hasSaturation == false ||
                    hasBrightness == false)
                {
                    continue;
                }


                // <변경부분>
                // 원본 Material 전체를 복제하므로
                // Normal Map을 포함한 기존 Material 설정을 보존한다.
                Material runtimeMaterial =
                    new Material(
                        originalMaterial
                    );


                runtimeMaterial.name =
                    originalMaterial.name +
                    " (Player Color Runtime)";


                // 공용 원본 Material은 절대 수정하지 않고
                // 필요한 Keyword는 기물 전용 복제본에서만 활성화한다.
                if (runtimeMaterial.IsKeywordEnabled(
                        ColorAdjustmentKeyword) == false)
                {
                    runtimeMaterial.EnableKeyword(
                        ColorAdjustmentKeyword
                    );
                }


                playerColorRuntimeMaterials.Add(
                    originalMaterial,
                    runtimeMaterial
                );


                if (customMaterialOverride == null)
                {
                    customMaterialOverride =
                        currentSkeletonRenderer
                            .CustomMaterialOverride;
                }


                customMaterialOverride[
                    originalMaterial
                ] =
                    runtimeMaterial;
            }
        }
    }


    // <변경부분>
    // 현재 기물에 등록한 Spine Material Override를 제거하고
    // 생성했던 Runtime Material을 함께 파괴한다.
    private void ClearPlayerColorRuntimeMaterials()
    {
        if (playerColorRuntimeMaterials.Count == 0)
        {
            return;
        }


        if (currentSkeletonRenderer != null)
        {
            Dictionary<Material, Material> customMaterialOverride =
                currentSkeletonRenderer
                    .CustomMaterialOverride;


            foreach (KeyValuePair<Material, Material> materialPair
                     in playerColorRuntimeMaterials)
            {
                if (materialPair.Key != null)
                {
                    customMaterialOverride.Remove(
                        materialPair.Key
                    );
                }
            }
        }


        foreach (KeyValuePair<Material, Material> materialPair
                 in playerColorRuntimeMaterials)
        {
            if (materialPair.Value != null)
            {
                Destroy(
                    materialPair.Value
                );
            }
        }


        playerColorRuntimeMaterials.Clear();
    }


    // <변경부분>
    // 현재 생성된 Spine Visual의
    // 기물 전용 Runtime Material에
    // Color Adjustment 진행도를 적용한다.
    //
    // progress:
    // 0 = 원본 Material의 Hue / Saturation / Brightness
    // 1 = Inspector에 설정된 Player 보정값
    private void ApplySpineColorAdjustmentProgress(
        float progress)
    {
        if (usePlayerSpineColorAdjustment == false ||
            currentSpineVisualObject == null ||
            currentSkeletonRenderer == null)
        {
            return;
        }


        progress =
            Mathf.Clamp01(
                progress
            );


        EnsurePlayerColorRuntimeMaterials();


        if (playerColorRuntimeMaterials.Count == 0)
        {
            return;
        }


        foreach (KeyValuePair<Material, Material> materialPair
                 in playerColorRuntimeMaterials)
        {
            Material originalMaterial =
                materialPair.Key;

            Material runtimeMaterial =
                materialPair.Value;


            if (originalMaterial == null ||
                runtimeMaterial == null)
            {
                continue;
            }


            float originalHue =
                originalMaterial.GetFloat(
                    HuePropertyId
                );

            float originalSaturation =
                originalMaterial.GetFloat(
                    SaturationPropertyId
                );

            float originalBrightness =
                originalMaterial.GetFloat(
                    BrightnessPropertyId
                );


            runtimeMaterial.SetFloat(
                HuePropertyId,
                Mathf.Lerp(
                    originalHue,
                    playerHue,
                    progress
                )
            );


            runtimeMaterial.SetFloat(
                SaturationPropertyId,
                Mathf.Lerp(
                    originalSaturation,
                    playerSaturation,
                    progress
                )
            );


            runtimeMaterial.SetFloat(
                BrightnessPropertyId,
                Mathf.Lerp(
                    originalBrightness,
                    playerBrightness,
                    progress
                )
            );
        }
    }
    private void ClearSpineVisual()
    {
        // <변경부분>
        // 외형 교체 전에 이전 Visual에서 실행 중이던
        // Color Adjustment Coroutine을 먼저 정리한다.
        StopPlayerColorAdjustmentTransition();

        isPlayerColorTransitionPrepared =
            false;

        currentPlayerColorAdjustmentProgress =
            0f;


        // <변경부분>
        // 파괴될 SkeletonRenderer가
        // PieceVisualController의 Callback을 계속 참조하지 않도록
        // 먼저 이벤트 연결을 해제한다.
        // PieceVisualController의 Callback을 계속 참조하지 않도록
        // 먼저 이벤트 연결을 해제한다.
        if (currentSkeletonRenderer != null)
        {
            currentSkeletonRenderer
                .OnMeshAndMaterialsUpdated -=
                HandleSpineMeshAndMaterialsUpdated;
        }


        // <변경부분>
        // SkeletonRenderer 참조를 비우기 전에
        // 현재 기물에 등록한 Material Override와
        // Runtime Material을 먼저 정리한다.
        ClearPlayerColorRuntimeMaterials();


        currentSkeletonRenderer =
            null;

        currentSpineRenderers =
            null;

        currentShouldApplyPlayerColorAdjustment =
            false;

        sharedMaterialBuffer.Clear();


        if (currentSpineVisualObject != null)
        {
            Destroy(
                currentSpineVisualObject
            );
        }


        currentSpineVisualObject =
            null;

        currentSpineVisualPrefab =
            null;

        currentSpineAnimationController =
            null;
    }

    // <변경부분> 현재 외형 렌더러의 정렬 순서 갱신
    public void SetSortingOrder(int sortingOrder)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (currentSpineAnimationController != null)
        {
            currentSpineAnimationController.SetSortingOrder(sortingOrder);
            return;
        }

        if (currentSpineVisualObject == null)
        {
            return;
        }

        Renderer[] renderers = currentSpineVisualObject.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = sortingOrder;
        }
    }
}