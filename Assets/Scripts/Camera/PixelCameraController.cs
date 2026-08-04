using System.Collections;
using UnityEngine;
using UnityEngine.U2D;

// Pixel Perfect 2D Camera Controller
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(PixelPerfectCamera))]
public class PixelCameraController : MonoBehaviour
{
    // 카메라 컴포넌트
    private Camera cam;

    // 픽셀 퍼펙트 카메라 컴포넌트
    private PixelPerfectCamera pixelCam;

    [Header("Start Settings")]
    // 시작 시 카메라 위치
    public Vector2 startPosition = Vector2.zero;

    // 시작 시 카메라 Z 위치
    [SerializeField] private float cameraZPosition = -10f;

    [Header("World Zoom")]
    // 확대/축소 대상 월드 루트
    [SerializeField] private Transform worldRoot;

    // 최소 월드 확대 배율
    [SerializeField] private float minWorldScale = 1f;

    // 최대 월드 확대 배율
    [SerializeField] private float maxWorldScale = 2f;

    // 마우스 휠 확대 속도
    [SerializeField] private float mouseZoomSpeed = 0.15f;

    // 모바일 두 손가락 확대 속도
    [SerializeField] private float pinchZoomSpeed = 0.005f;

    // 확대 부드러움 정도
    [SerializeField] private float zoomSmoothSpeed = 10f;

    // 현재 월드 확대 배율
    private float currentWorldScale = 1f;

    // 목표 월드 확대 배율
    private float targetWorldScale = 1f;

    [Header("Battle Start Zoom Animation")]
    // <변경부분> 배틀 시작 시 WorldRoot 확대 연출을 사용할지 여부
    [SerializeField] private bool playStartZoomAnimation = true;

    // <변경부분> 시작 확대 연출 전 잠깐 대기할 시간
    [SerializeField] private float startZoomDelay = 0.1f;

    // <변경부분> 배틀 화면이 처음 표시될 때 사용할 시작 배율
    [SerializeField] private float startZoomScale = 1f;

    // <변경부분> 시작 확대 연출이 끝난 뒤 유지할 최종 배율
    [SerializeField] private float startZoomTargetScale = 1.2f;

    // <변경부분> 시작 배율에서 최종 배율까지 확대되는 시간
    [SerializeField] private float startZoomDuration = 0.8f;

    // <변경부분> 시작 확대 속도 곡선
    // 초반에 확대되고 마지막에 부드럽게 멈추도록 설정한다.
    [SerializeField]
    private AnimationCurve startZoomCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(1f, 1f, 0f, 0f)
        );

    // <변경부분> 시작 확대 애니메이션이 진행 중인지 확인한다.
    // 연출 중에는 휠과 핀치 입력이 목표 배율을 바꾸지 않도록 사용한다.
    private bool isPlayingStartZoomAnimation = false;

    // <변경부분> 현재 실행 중인 시작 확대 코루틴
    private Coroutine startZoomCoroutine;

    [Header("Piece Selection Focus")]
    // <변경부분> 기물을 클릭했을 때 카메라 중심을 기물 위치로 이동할지 여부
    [SerializeField]
    private bool focusCameraOnPieceSelection =
        true;

    // <변경부분> 기물 선택 위치까지 카메라가 이동하는 시간
    [SerializeField, Min(0f)]
    private float pieceFocusMoveDuration =
        0.22f;

    // <변경부분> 기물 중심에서 카메라를 보정할 월드 좌표 오프셋
    [SerializeField]
    private Vector2 pieceFocusWorldOffset =
        Vector2.zero;

    // <변경부분> 현재 실행 중인 기물 선택 포커스 코루틴
    private Coroutine pieceFocusCoroutine;

    [Header("Moving Tile Follow")]
    // <변경부분> 선택한 기물이 이동·공격할 때
    // 기물 Transform 대신 목표 타일 중심으로 카메라를 이동할지 여부
    [SerializeField]
    private bool followTargetTileWhileMoving =
      true;

    // <변경부분> 목표 타일 중심까지 카메라가 이동하는 시간
    [SerializeField, Min(0f)]
    private float movingTileFollowDuration =
        0.22f;

    // <변경부분> 이동·공격 중 목표 타일 중심에 적용할 월드 좌표 오프셋
    [SerializeField]
    private Vector2 movingTileFollowWorldOffset =
        Vector2.zero;

    // <변경부분> 현재 실행 중인 목표 타일 이동 코루틴
    private Coroutine movingTileFollowCoroutine;

    // <변경부분> 이동 종료 시 유지할 마지막 목표 타일
    private Transform movingTileFollowTarget;

    [Header("Last Piece Attack Cinematic")]

    [Header("Last Piece Attack Cinematic")]
    // <변경부분> 마지막 Enemy 기물 공격 시 카메라 연출을 사용할지 여부
    [SerializeField]
    private bool useLastPieceAttackCinematic =
        true;

    // <변경부분> 공격 지점으로 빠르게 카메라가 이동하는 시간
    [SerializeField, Min(0f)]
    private float lastPieceFocusMoveDuration =
        0.16f;

    // <변경부분> 마지막 공격에서 사용할 WorldRoot 고정 확대 배율
    [SerializeField, Min(0.01f)]
    private float lastPieceAttackWorldScale =
        1.65f;

    // <변경부분> 공격 시작과 동시에 현재 배율에서
    // 고정 확대 배율까지 변화하는 시간
    [SerializeField, Min(0f)]
    private float lastPieceAttackZoomDuration =
        0.28f;

    // <변경부분> 마지막 공격 중 적용할 슬로우 모션 배율
    [SerializeField, Range(0.01f, 1f)]
    private float lastPieceAttackTimeScale =
        0.25f;

    // <변경부분> 공격 종료 후 이전 위치와 줌으로 복귀하는 시간
    [SerializeField, Min(0f)]
    private float lastPieceAttackRestoreDuration =
        0.35f;

    // <변경부분> 마지막 공격 연출 중 사용자 드래그와 줌 입력을 잠근다.
    private bool isPlayingLastPieceAttackCinematic =
        false;

    // <변경부분> 마지막 공격 중 확대를 진행하는 코루틴
    private Coroutine lastPieceAttackZoomCoroutine;

    // <변경부분> 마지막 공격 시작 전 카메라 상태 저장
    private Vector3 savedCameraPositionBeforeCinematic;

    private float savedWorldScaleBeforeCinematic =
        1f;

    private float savedTargetWorldScaleBeforeCinematic =
        1f;

    private float savedTimeScaleBeforeCinematic =
        1f;

    private float savedFixedDeltaTimeBeforeCinematic =
        0.02f;

    // <변경부분> 연출 중 대상이 제거돼도
    // 마지막 공격 위치를 유지하기 위한 값
    private Transform lastPieceAttackTarget;

    private Vector3 lastPieceAttackTargetWorldPosition;

    [Header("Move")]
    // 마우스 드래그 이동 속도
    public float mouseDragSpeed = 0.3f;

    // 모바일 드래그 이동 속도
    public float touchDragSpeed = 0.01f;

    // 최소 줌 상태에서 이동을 막기 위한 허용 오차
    [SerializeField] private float minZoomMoveThreshold = 0.01f;

    // 최소 줌 기준 화면 중심 위치
    private Vector3 baseCameraPosition;

    [Header("Camera Bounds")]
    // 기본 화면 기준 카메라 이동 가능 최소 좌표
    public Vector2 minBounds;

    // 기본 화면 기준 카메라 이동 가능 최대 좌표
    public Vector2 maxBounds;

    private void Start()
    {
        // 카메라 컴포넌트 가져오기
        cam = GetComponent<Camera>();

        // 픽셀 퍼펙트 카메라 컴포넌트 가져오기
        pixelCam = GetComponent<PixelPerfectCamera>();

        // 카메라를 Orthographic 모드로 설정
        cam.orthographic = true;

        // 시작 위치 적용
        transform.position = new Vector3(
            startPosition.x,
            startPosition.y,
            cameraZPosition
        );

        // 최소 줌 기준 카메라 위치 저장
        baseCameraPosition = transform.position;

        if (playStartZoomAnimation)
        {
            // <변경부분> 시작 확대 연출을 사용할 때는
            // WorldRoot를 시작 배율로 먼저 표시한다.
            currentWorldScale =
                Mathf.Clamp(
                    startZoomScale,
                    minWorldScale,
                    maxWorldScale
                );

            targetWorldScale =
                currentWorldScale;

            ApplyWorldZoom();
            ClampCameraByZoom();

            startZoomCoroutine =
                StartCoroutine(
                    PlayStartZoomAnimationRoutine()
                );

            return;
        }

        // 시작 연출을 사용하지 않으면 기존 최소 줌으로 초기화
        currentWorldScale =
            minWorldScale;

        targetWorldScale =
            minWorldScale;

        ApplyWorldZoom();

        // 시작 시 최소 줌 위치로 고정
        ClampCameraByZoom();
    }

    // <변경부분> 배틀 시작 시 WorldRoot를
    // 시작 배율에서 X/Y 1.2까지 부드럽게 확대한다.
    //
    // 연출 종료 시 currentWorldScale과 targetWorldScale을
    // 모두 최종 배율로 저장하므로 다음 프레임에
    // 기존 최소 배율로 되돌아가지 않는다.
    private IEnumerator PlayStartZoomAnimationRoutine()
    {
        isPlayingStartZoomAnimation =
            true;

        if (startZoomDelay > 0f)
        {
            yield return new WaitForSeconds(
                startZoomDelay
            );
        }

        float animationStartScale =
            Mathf.Clamp(
                startZoomScale,
                minWorldScale,
                maxWorldScale
            );

        float animationTargetScale =
            Mathf.Clamp(
                startZoomTargetScale,
                minWorldScale,
                maxWorldScale
            );

        currentWorldScale =
            animationStartScale;

        targetWorldScale =
            animationStartScale;

        ApplyWorldZoom();

        float safeDuration =
            Mathf.Max(
                0f,
                startZoomDuration
            );

        if (safeDuration <= 0f)
        {
            currentWorldScale =
                animationTargetScale;

            targetWorldScale =
                animationTargetScale;

            ApplyWorldZoom();

            isPlayingStartZoomAnimation =
                false;

            startZoomCoroutine =
                null;

            yield break;
        }

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

            float curvedTime =
                startZoomCurve == null
                    ? Mathf.SmoothStep(
                        0f,
                        1f,
                        normalizedTime
                    )
                    : startZoomCurve.Evaluate(
                        normalizedTime
                    );

            currentWorldScale =
                Mathf.LerpUnclamped(
                    animationStartScale,
                    animationTargetScale,
                    curvedTime
                );

            // 연출 도중에도 내부 목표 배율을 현재 값으로 맞춰
            // 다른 처리에서 이전 배율을 다시 사용하지 않도록 한다.
            targetWorldScale =
                currentWorldScale;

            ApplyWorldZoom();
            ClampCameraByZoom();

            yield return null;
        }

        // <변경부분> 연출이 끝난 후 최종 배율을
        // 현재 배율과 목표 배율 양쪽에 모두 저장한다.
        //
        // 이후 UpdateWorldZoom()이 실행돼도
        // 1.2에서 1.0으로 되돌아가지 않는다.
        currentWorldScale =
            animationTargetScale;

        targetWorldScale =
            animationTargetScale;

        ApplyWorldZoom();
        ClampCameraByZoom();

        isPlayingStartZoomAnimation =
            false;

        startZoomCoroutine =
            null;
    }

    private void Update()
    {
        // <변경부분> 시작 확대 애니메이션이 진행 중일 때는
        // 코루틴이 WorldRoot 배율을 직접 제어한다.
        //
        // 이 시간 동안 일반 줌 갱신을 실행하면
        // 기존 targetWorldScale이 연출 값을 덮어쓸 수 있으므로
        // 사용자 줌 및 드래그 입력을 잠시 막는다.
        if (isPlayingStartZoomAnimation)
        {
            ClampCameraByZoom();
            return;
        }

        // <변경부분> 마지막 기물 공격 카메라 연출 중에는
        // 코루틴이 위치와 줌을 직접 제어하므로 사용자 입력을 막는다.
        if (isPlayingLastPieceAttackCinematic)
        {
            return;
        }


        // PC 마우스 휠 확대/축소 처리
        HandleMouseZoom();

        // 모바일 두 손가락 확대/축소 처리
        HandleMobilePinchZoom();

        // 월드 확대 배율 부드럽게 적용
        UpdateWorldZoom();

        // PC 우클릭 드래그 이동
        HandlePCDrag();

        // 모바일 두 손가락 드래그 이동
        HandleMobileDrag();

        // 현재 줌 배율에 맞게 카메라 이동 범위 제한
        ClampCameraByZoom();
    }

    // <변경부분> 선택한 기물의 이동·공격 동안
    // 목표 타일 중심으로 카메라 이동을 시작한다.
    //
    // 기물 Transform을 직접 추적하지 않으므로
    // 점프, 내려찍기, 반동 애니메이션의 높이 변화는 따라가지 않는다.
    public void StartFollowingMovingTile(
        Transform tileTransform)
    {
        if (followTargetTileWhileMoving == false ||
            tileTransform == null ||
            isPlayingLastPieceAttackCinematic)
        {
            return;
        }

        if (pieceFocusCoroutine != null)
        {
            StopCoroutine(
                pieceFocusCoroutine
            );

            pieceFocusCoroutine =
                null;
        }

        if (movingTileFollowCoroutine != null)
        {
            StopCoroutine(
                movingTileFollowCoroutine
            );
        }

        movingTileFollowTarget =
            tileTransform;

        movingTileFollowCoroutine =
            StartCoroutine(
                FocusOnMovingTileRoutine(
                    tileTransform
                )
            );
    }

    // <변경부분> 목표 타일 중심 이동을 종료한다.
    //
    // keepFinalFocus가 true면 목표 타일 중심을
    // 현재 카메라 위치로 확정한다.
    public void StopFollowingMovingTile(
        bool keepFinalFocus)
    {
        if (movingTileFollowCoroutine != null)
        {
            StopCoroutine(
                movingTileFollowCoroutine
            );

            movingTileFollowCoroutine =
                null;
        }

        if (keepFinalFocus &&
            movingTileFollowTarget != null &&
            isPlayingLastPieceAttackCinematic == false)
        {
            Vector3 requestedPosition =
                new Vector3(
                    movingTileFollowTarget.position.x +
                        movingTileFollowWorldOffset.x,
                    movingTileFollowTarget.position.y +
                        movingTileFollowWorldOffset.y,
                    cameraZPosition
                );

            transform.position =
                GetClampedCameraPosition(
                    requestedPosition,
                    currentWorldScale
                );
        }

        movingTileFollowTarget =
            null;
    }

    // <변경부분> 현재 카메라 위치에서 목표 타일 중심까지
    // 부드럽게 이동한다.
    private IEnumerator FocusOnMovingTileRoutine(
        Transform tileTransform)
    {
        Vector3 startCameraPosition =
            transform.position;

        float safeDuration =
            Mathf.Max(
                0f,
                movingTileFollowDuration
            );

        float elapsedTime =
            0f;

        while (elapsedTime < safeDuration &&
               tileTransform != null &&
               isPlayingLastPieceAttackCinematic == false)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                safeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsedTime /
                        safeDuration
                    );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            Vector3 requestedPosition =
                new Vector3(
                    tileTransform.position.x +
                        movingTileFollowWorldOffset.x,
                    tileTransform.position.y +
                        movingTileFollowWorldOffset.y,
                    cameraZPosition
                );

            Vector3 targetCameraPosition =
                GetClampedCameraPosition(
                    requestedPosition,
                    currentWorldScale
                );

            transform.position =
                Vector3.Lerp(
                    startCameraPosition,
                    targetCameraPosition,
                    smoothTime
                );

            yield return null;
        }

        if (tileTransform != null &&
            isPlayingLastPieceAttackCinematic == false)
        {
            Vector3 requestedPosition =
                new Vector3(
                    tileTransform.position.x +
                        movingTileFollowWorldOffset.x,
                    tileTransform.position.y +
                        movingTileFollowWorldOffset.y,
                    cameraZPosition
                );

            transform.position =
                GetClampedCameraPosition(
                    requestedPosition,
                    currentWorldScale
                );
        }

        movingTileFollowCoroutine =
            null;
    }

    // <변경부분> 클릭한 기물이 올라가 있는 타일을
    // 현재 화면의 중심으로 부드럽게 이동시킨다.
    //
    // 기물 Transform이 아니라 Tile Transform을 사용하므로
    // Select 애니메이션이나 기물 높이 변화에 영향을 받지 않는다.
    public void FocusOnTile(
        Transform tileTransform)
    {
        if (focusCameraOnPieceSelection == false ||
            tileTransform == null ||
            isPlayingStartZoomAnimation ||
            isPlayingLastPieceAttackCinematic)
        {
            return;
        }

        // 이전 클릭 포커스가 진행 중이라면 중단하고
        // 새로 클릭한 타일을 기준으로 다시 이동한다.
        if (pieceFocusCoroutine != null)
        {
            StopCoroutine(
                pieceFocusCoroutine
            );

            pieceFocusCoroutine =
                null;
        }

        pieceFocusCoroutine =
            StartCoroutine(
                FocusOnTileRoutine(
                    tileTransform
                )
            );
    }

    // <변경부분> 현재 카메라 위치에서
    // 선택된 기물이 올라가 있는 타일 중심까지 부드럽게 이동한다.
    private IEnumerator FocusOnTileRoutine(
        Transform tileTransform)
    {
        Vector3 startCameraPosition =
            transform.position;

        float safeDuration =
            Mathf.Max(
                0f,
                pieceFocusMoveDuration
            );

        float elapsedTime =
            0f;

        while (elapsedTime < safeDuration &&
               tileTransform != null &&
               isPlayingLastPieceAttackCinematic == false)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                safeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsedTime /
                        safeDuration
                    );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            // <변경부분> 기물 위치가 아니라
            // 고정된 타일 월드 위치를 카메라 중심으로 사용한다.
            Vector3 requestedPosition =
                new Vector3(
                    tileTransform.position.x +
                        pieceFocusWorldOffset.x,
                    tileTransform.position.y +
                        pieceFocusWorldOffset.y,
                    cameraZPosition
                );

            Vector3 targetCameraPosition =
                GetClampedCameraPosition(
                    requestedPosition,
                    currentWorldScale
                );

            transform.position =
                Vector3.Lerp(
                    startCameraPosition,
                    targetCameraPosition,
                    smoothTime
                );

            yield return null;
        }

        // 코루틴이 정상 종료됐다면
        // 타일 중심 위치를 정확하게 한 번 더 적용한다.
        if (tileTransform != null &&
            isPlayingLastPieceAttackCinematic == false)
        {
            Vector3 requestedPosition =
                new Vector3(
                    tileTransform.position.x +
                        pieceFocusWorldOffset.x,
                    tileTransform.position.y +
                        pieceFocusWorldOffset.y,
                    cameraZPosition
                );

            transform.position =
                GetClampedCameraPosition(
                    requestedPosition,
                    currentWorldScale
                );
        }

        pieceFocusCoroutine =
            null;
    }

    // <변경부분> 마지막 Enemy 공격 전에
    // 기존 카메라 상태를 저장하고 공격 대상 타일 중심으로 빠르게 이동한다.
    //
    // 기물 Transform이 아닌 Tile Transform을 받으므로
    // 공격 애니메이션, 점프, 내려찍기와 무관하게 중심이 고정된다.
    public IEnumerator PrepareLastPieceAttackCinematicRoutine(
        Transform targetTileTransform)
    {
        if (useLastPieceAttackCinematic == false ||
            targetTileTransform == null)
        {
            yield break;
        }

        // 일반 목표 타일 이동 코루틴이 남아 있다면 중단한다.
        if (movingTileFollowCoroutine != null)
        {
            StopCoroutine(
                movingTileFollowCoroutine
            );

            movingTileFollowCoroutine =
                null;
        }

        movingTileFollowTarget =
            null;

        // 기물 선택 포커스가 진행 중이라면 중단한다.
        if (pieceFocusCoroutine != null)
        {
            StopCoroutine(
                pieceFocusCoroutine
            );

            pieceFocusCoroutine =
                null;
        }

        // 전투 시작 줌이 아직 진행 중이면
        // 마지막 공격 연출이 우선하도록 중단한다.
        if (startZoomCoroutine != null)
        {
            StopCoroutine(
                startZoomCoroutine
            );

            startZoomCoroutine =
                null;

            isPlayingStartZoomAnimation =
                false;
        }

        isPlayingLastPieceAttackCinematic =
            true;

        // <변경부분> 마지막 공격 시작 전
        // 카메라 위치와 현재 줌 상태를 저장한다.
        savedCameraPositionBeforeCinematic =
            transform.position;

        savedWorldScaleBeforeCinematic =
            currentWorldScale;

        savedTargetWorldScaleBeforeCinematic =
            targetWorldScale;

        // <변경부분> 기존 시간 배율과 물리 갱신 간격도 저장한다.
        savedTimeScaleBeforeCinematic =
            Time.timeScale;

        savedFixedDeltaTimeBeforeCinematic =
            Time.fixedDeltaTime;

        // <변경부분> 마지막 공격 카메라 기준을
        // 기물이 아닌 고정 타일 Transform으로 저장한다.
        lastPieceAttackTarget =
            targetTileTransform;

        lastPieceAttackTargetWorldPosition =
            targetTileTransform.position;

        Vector3 startCameraPosition =
            transform.position;

        float safeDuration =
            Mathf.Max(
                0f,
                lastPieceFocusMoveDuration
            );

        float elapsedTime =
            0f;

        // <변경부분> 실제 공격 시작 전에
        // 현재 위치에서 마지막 적 타일 중심으로 빠르게 이동한다.
        while (elapsedTime < safeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            // WorldRoot 확대 상태에 따라 타일의 월드 위치가 달라질 수 있으므로
            // 현재 프레임의 타일 위치를 계속 갱신한다.
            UpdateLastPieceAttackTargetPosition();

            float normalizedTime =
                safeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsedTime /
                        safeDuration
                    );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            Vector3 targetCameraPosition =
                new Vector3(
                    lastPieceAttackTargetWorldPosition.x,
                    lastPieceAttackTargetWorldPosition.y,
                    cameraZPosition
                );

            transform.position =
                Vector3.Lerp(
                    startCameraPosition,
                    targetCameraPosition,
                    smoothTime
                );

            yield return null;
        }

        UpdateLastPieceAttackTargetPosition();

        // 이동 완료 후 타일 중심 위치를 정확히 확정한다.
        transform.position =
            new Vector3(
                lastPieceAttackTargetWorldPosition.x,
                lastPieceAttackTargetWorldPosition.y,
                cameraZPosition
            );
    }

    // <변경부분> 실제 공격 시작과 동시에 슬로우 모션과
    // 고정 확대 배율까지의 부드러운 줌을 시작한다.
    public void StartLastPieceAttackSlowMotion()
    {
        if (isPlayingLastPieceAttackCinematic == false)
        {
            return;
        }

        float safeTimeScale =
            Mathf.Clamp(
                lastPieceAttackTimeScale,
                0.01f,
                1f
            );

        Time.timeScale =
            safeTimeScale;

        // 물리 업데이트 간격도 시간 배율에 맞춰 조정한다.
        Time.fixedDeltaTime =
            savedFixedDeltaTimeBeforeCinematic *
            safeTimeScale;

        if (lastPieceAttackZoomCoroutine != null)
        {
            StopCoroutine(
                lastPieceAttackZoomCoroutine
            );
        }

        lastPieceAttackZoomCoroutine =
            StartCoroutine(
                PlayLastPieceAttackZoomRoutine()
            );
    }

    // <변경부분> 공격 중 대상 위치를 계속 중심에 두면서
    // 현재 줌에서 마지막 공격용 줌까지 확대한다.
    private IEnumerator PlayLastPieceAttackZoomRoutine()
    {
        float startScale =
            currentWorldScale;

        float targetScale =
            Mathf.Clamp(
                lastPieceAttackWorldScale,
                minWorldScale,
                maxWorldScale
            );

        float safeDuration =
            Mathf.Max(
                0f,
                lastPieceAttackZoomDuration
            );

        float elapsedTime =
            0f;

        while (elapsedTime < safeDuration &&
               isPlayingLastPieceAttackCinematic)
        {
            // 슬로우 모션과 관계없이 카메라 줌은 원래 속도로 진행한다.
            elapsedTime +=
                Time.unscaledDeltaTime;

            UpdateLastPieceAttackTargetPosition();

            float normalizedTime =
                safeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsedTime /
                        safeDuration
                    );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            currentWorldScale =
                Mathf.Lerp(
                    startScale,
                    targetScale,
                    smoothTime
                );

            targetWorldScale =
                currentWorldScale;

            ApplyWorldZoom();

            // 확대 중에도 공격 위치를 계속 화면 중심에 유지한다.
            transform.position =
                new Vector3(
                    lastPieceAttackTargetWorldPosition.x,
                    lastPieceAttackTargetWorldPosition.y,
                    cameraZPosition
                );

            yield return null;
        }

        if (isPlayingLastPieceAttackCinematic)
        {
            currentWorldScale =
                targetScale;

            targetWorldScale =
                targetScale;

            ApplyWorldZoom();
        }

        lastPieceAttackZoomCoroutine =
            null;
    }

    // <변경부분> 마지막 공격이 끝난 뒤
    // 시간 배율을 정상화하고 이전 카메라 위치와 줌으로 복구한다.
    public IEnumerator RestoreAfterLastPieceAttackCinematicRoutine()
    {
        if (isPlayingLastPieceAttackCinematic == false)
        {
            yield break;
        }

        if (lastPieceAttackZoomCoroutine != null)
        {
            StopCoroutine(
                lastPieceAttackZoomCoroutine
            );

            lastPieceAttackZoomCoroutine =
                null;
        }

        // <변경부분> 공격 애니메이션이 끝났으므로
        // 기존 시간 배율과 물리 업데이트 간격을 먼저 복구한다.
        Time.timeScale =
            savedTimeScaleBeforeCinematic;

        Time.fixedDeltaTime =
            savedFixedDeltaTimeBeforeCinematic;

        Vector3 restoreStartPosition =
            transform.position;

        float restoreStartScale =
            currentWorldScale;

        float safeDuration =
            Mathf.Max(
                0f,
                lastPieceAttackRestoreDuration
            );

        float elapsedTime =
            0f;

        while (elapsedTime < safeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                safeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsedTime /
                        safeDuration
                    );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            transform.position =
                Vector3.Lerp(
                    restoreStartPosition,
                    savedCameraPositionBeforeCinematic,
                    smoothTime
                );

            currentWorldScale =
                Mathf.Lerp(
                    restoreStartScale,
                    savedWorldScaleBeforeCinematic,
                    smoothTime
                );

            targetWorldScale =
                currentWorldScale;

            ApplyWorldZoom();

            yield return null;
        }

        // <변경부분> 오차 없이 원래 위치와 배율 확정
        transform.position =
            savedCameraPositionBeforeCinematic;

        currentWorldScale =
            savedWorldScaleBeforeCinematic;

        targetWorldScale =
            savedTargetWorldScaleBeforeCinematic;

        ApplyWorldZoom();
        ClampCameraByZoom();

        lastPieceAttackTarget =
            null;

        isPlayingLastPieceAttackCinematic =
            false;
    }

    // <변경부분> 대상이 살아 있는 동안 현재 월드 위치를 갱신하고,
    // 제거된 뒤에는 마지막으로 저장한 공격 위치를 유지한다.
    private void UpdateLastPieceAttackTargetPosition()
    {
        if (lastPieceAttackTarget != null)
        {
            lastPieceAttackTargetWorldPosition =
                lastPieceAttackTarget.position;
        }
    }

    // <변경부분> 현재 줌 배율에 맞는 이동 가능 범위 안에서
    // 요청받은 카메라 위치를 반환한다.
    private Vector3 GetClampedCameraPosition(
        Vector3 requestedPosition,
        float worldScale)
    {
        if (worldScale <=
            minWorldScale +
            minZoomMoveThreshold)
        {
            return new Vector3(
                baseCameraPosition.x,
                baseCameraPosition.y,
                cameraZPosition
            );
        }

        float zoomMoveRate =
            (worldScale / minWorldScale) -
            1f;

        float allowedX =
            (maxBounds.x - minBounds.x) *
            zoomMoveRate *
            0.5f;

        float allowedY =
            (maxBounds.y - minBounds.y) *
            zoomMoveRate *
            0.5f;

        float minX =
            baseCameraPosition.x -
            allowedX;

        float maxX =
            baseCameraPosition.x +
            allowedX;

        float minY =
            baseCameraPosition.y -
            allowedY;

        float maxY =
            baseCameraPosition.y +
            allowedY;

        return new Vector3(
            Mathf.Clamp(
                requestedPosition.x,
                minX,
                maxX
            ),
            Mathf.Clamp(
                requestedPosition.y,
                minY,
                maxY
            ),
            cameraZPosition
        );
    }

    // PC 마우스 휠 확대/축소 처리
    private void HandleMouseZoom()
    {
        // 마우스 휠 입력값
        float scroll = Input.mouseScrollDelta.y;

        // 휠 입력이 없으면 종료
        if (Mathf.Abs(scroll) <= 0.01f)
        {
            return;
        }

        // 휠 위로 올리면 월드 확대, 아래로 내리면 월드 축소
        targetWorldScale += scroll * mouseZoomSpeed;

        // 월드 확대 배율 제한
        targetWorldScale = Mathf.Clamp(targetWorldScale, minWorldScale, maxWorldScale);
    }

    // 모바일 두 손가락 핀치 확대/축소 처리
    private void HandleMobilePinchZoom()
    {
        // 두 손가락 터치가 아니면 종료
        if (Input.touchCount != 2)
        {
            return;
        }

        // 첫 번째 터치 정보
        Touch touch0 = Input.GetTouch(0);

        // 두 번째 터치 정보
        Touch touch1 = Input.GetTouch(1);

        // 이전 프레임의 첫 번째 터치 위치
        Vector2 prevTouch0 = touch0.position - touch0.deltaPosition;

        // 이전 프레임의 두 번째 터치 위치
        Vector2 prevTouch1 = touch1.position - touch1.deltaPosition;

        // 이전 프레임의 두 손가락 거리
        float prevDistance = Vector2.Distance(prevTouch0, prevTouch1);

        // 현재 프레임의 두 손가락 거리
        float currentDistance = Vector2.Distance(touch0.position, touch1.position);

        // 두 손가락 거리 변화량
        float pinchDelta = currentDistance - prevDistance;

        // 손가락을 벌리면 월드 확대, 오므리면 월드 축소
        targetWorldScale += pinchDelta * pinchZoomSpeed;

        // 월드 확대 배율 제한
        targetWorldScale = Mathf.Clamp(targetWorldScale, minWorldScale, maxWorldScale);
    }

    // 월드 확대 배율 부드럽게 적용
    private void UpdateWorldZoom()
    {
        // 현재 월드 확대 배율을 목표 배율로 부드럽게 이동
        currentWorldScale = Mathf.Lerp(
            currentWorldScale,
            targetWorldScale,
            Time.deltaTime * zoomSmoothSpeed
        );

        // 월드 확대 적용
        ApplyWorldZoom();
    }

    // WorldRoot Scale 기준 확대/축소 적용
    private void ApplyWorldZoom()
    {
        // 월드 루트가 없으면 종료
        if (worldRoot == null)
        {
            return;
        }

        // WorldRoot 전체 스케일 변경
        worldRoot.localScale = new Vector3(
            currentWorldScale,
            currentWorldScale,
            1f
        );
    }

    // PC 우클릭 드래그 이동
    private void HandlePCDrag()
    {
        // 최소 줌 상태에서는 카메라 이동 불가
        if (CanMoveCameraByZoom() == false)
        {
            return;
        }

        // 우클릭 중이 아니면 종료
        if (Input.GetMouseButton(1) == false)
        {
            return;
        }

        // 마우스 X 이동량
        float moveX = Input.GetAxis("Mouse X");

        // 마우스 Y 이동량
        float moveY = Input.GetAxis("Mouse Y");

        // 확대 상태일수록 이동량을 줄여 조작감 유지
        float zoomAdjustedSpeed = mouseDragSpeed / currentWorldScale;

        // 카메라 위치 이동
        transform.position -= new Vector3(
            moveX * zoomAdjustedSpeed,
            moveY * zoomAdjustedSpeed,
            0f
        );
    }

    // <변경부분> 모바일 두 손가락 드래그 이동
    private void HandleMobileDrag()
    {
        // 최소 줌 상태에서는 카메라 이동 불가
        if (CanMoveCameraByZoom() == false)
        {
            return;
        }

        // <변경부분> 두 손가락 터치가 아니면 카메라 이동을 하지 않음
        // 한 손가락 터치는 기물/타일 선택 전용으로 사용
        if (Input.touchCount != 2)
        {
            return;
        }

        // 첫 번째 터치 정보
        Touch touch0 = Input.GetTouch(0);

        // 두 번째 터치 정보
        Touch touch1 = Input.GetTouch(1);

        // <변경부분> 두 손가락 중 하나라도 움직이지 않았다면 이동 처리하지 않음
        if (touch0.phase != TouchPhase.Moved && touch1.phase != TouchPhase.Moved)
        {
            return;
        }

        // <변경부분> 현재 프레임의 두 손가락 중심점 계산
        Vector2 currentCenter = (touch0.position + touch1.position) * 0.5f;

        // <변경부분> 이전 프레임의 두 손가락 중심점 계산
        Vector2 previousCenter =
            ((touch0.position - touch0.deltaPosition) +
             (touch1.position - touch1.deltaPosition)) * 0.5f;

        // <변경부분> 두 손가락 중심점이 이동한 만큼 카메라 이동
        Vector2 delta = currentCenter - previousCenter;

        // 확대 상태일수록 이동량을 줄여 조작감 유지
        float zoomAdjustedSpeed = touchDragSpeed / currentWorldScale;

        // 카메라 위치 이동
        transform.position -= new Vector3(
            delta.x * zoomAdjustedSpeed,
            delta.y * zoomAdjustedSpeed,
            0f
        );
    }

    // 현재 줌 상태에서 카메라 이동 가능 여부
    private bool CanMoveCameraByZoom()
    {
        // 현재 줌이 최소 줌보다 충분히 커졌을 때만 이동 가능
        return currentWorldScale > minWorldScale + minZoomMoveThreshold;
    }

    // 현재 줌 배율에 따라 카메라 이동 범위 제한
    private void ClampCameraByZoom()
    {
        // 현재 카메라 위치
        Vector3 pos = transform.position;

        // 최소 줌 상태에서는 시작 위치로 고정
        if (CanMoveCameraByZoom() == false)
        {
            transform.position = new Vector3(
                baseCameraPosition.x,
                baseCameraPosition.y,
                baseCameraPosition.z
            );

            return;
        }

        // <변경부분> 현재 확대 배율 기준으로 최소 줌 화면 영역 끝까지 이동할 수 있는 비율 계산
        float zoomMoveRate = (currentWorldScale / minWorldScale) - 1f;

        // <변경부분> X축에서 최소 줌 화면 영역을 다시 볼 수 있는 이동 거리 계산
        float allowedX = (maxBounds.x - minBounds.x) * zoomMoveRate * 0.5f;

        // <변경부분> Y축에서 최소 줌 화면 영역을 다시 볼 수 있는 이동 거리 계산
        float allowedY = (maxBounds.y - minBounds.y) * zoomMoveRate * 0.5f;

        // X축 최소 이동 좌표
        float minX = baseCameraPosition.x - allowedX;

        // X축 최대 이동 좌표
        float maxX = baseCameraPosition.x + allowedX;

        // Y축 최소 이동 좌표
        float minY = baseCameraPosition.y - allowedY;

        // Y축 최대 이동 좌표
        float maxY = baseCameraPosition.y + allowedY;

        // X 좌표 제한
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        // Y 좌표 제한
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        // 제한된 위치 적용
        transform.position = new Vector3(
            pos.x,
            pos.y,
            baseCameraPosition.z
        );
    }
}