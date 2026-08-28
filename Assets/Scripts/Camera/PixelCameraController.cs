using System.Collections;
using UnityEngine;
using UnityEngine.U2D;

// Pixel Perfect 2D Camera Controller
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(PixelPerfectCamera))]
public class PixelCameraController : MonoBehaviour
{
    // 실제 Orthographic Camera 제어에 사용하는 카메라 컴포넌트.
    private Camera cam;

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

    [Header("Move Sensitivity")]
    // <변경부분> PC 마우스 드래그 이동 감도
    // 모바일 감도와 별도로 관리한다.
    public float mouseDragSpeed =
    0.3f;

    // <변경부분> 모바일 터치 드래그 이동 감도
    // PC 감도와 별도로 관리한다.
    public float touchDragSpeed =
        0.01f;

    [Header("PC Left Drag")]
    // <변경부분> 좌클릭 후 실제 카메라 Drag로 확정하기 위해
    // 마우스가 이동해야 하는 최소 화면 픽셀 거리.
    //
    // 단순 클릭 중 발생하는 미세한 마우스 흔들림으로
    // 카메라가 움직이는 것을 방지한다.
    [SerializeField, Min(0f)]
    private float pcDragStartThresholdPixels =
        8f;

    // <변경부분> 현재 좌클릭이
    // 카메라 Drag를 시작할 수 있는 영역에서 시작되었는지 여부.
    private bool isPCDragCandidate =
        false;

    // <변경부분> Drag Threshold를 넘어
    // 실제 카메라 이동 상태로 확정되었는지 여부.
    private bool isPCDragging =
        false;

    // <변경부분> 좌클릭을 처음 누른 화면 좌표.
    // Drag Threshold 계산에 사용한다.
    private Vector2 pcDragStartScreenPosition;

    // <변경부분> PC 좌클릭을 처음 눌렀던 Battle Tile.
    //
    // Mouse Up까지 Drag로 전환되지 않았고,
    // 눌렀던 Tile과 뗀 Tile이 동일할 때만
    // 실제 Battle Click으로 확정한다.
    private Tile pcPressedTile =
        null;


    [Header("Mobile Touch Input")]

    // <변경부분> 한 손가락 Tap과 Camera Drag를 구분하는 최소 화면 픽셀 거리.
    // PC보다 손가락 입력 오차가 크므로 기본값을 더 크게 사용한다.
    [SerializeField, Min(0f)]
    private float mobileDragStartThresholdPixels =
        24f;

    // <변경부분> 현재 한 손가락 입력이
    // Tap / Drag 판정 후보인지 여부.
    private bool isMobileSingleTouchCandidate =
        false;

    // <변경부분> 현재 한 손가락 입력이 Threshold를 넘어
    // 실제 Camera Drag로 확정되었는지 여부.
    private bool isMobileDragging =
        false;

    // <변경부분> 현재 두 손가락 Pinch가
    // 입력을 소유하고 있는지 여부.
    private bool isMobilePinchActive =
        false;

    // <변경부분> Pinch 시작 시
    // 두 손가락 모두 Battle World에서 시작했는지 저장한다.
    //
    // UI에서 시작한 손가락이 하나라도 있으면
    // 해당 Pinch는 World Zoom을 수행하지 않는다.
    private bool isMobilePinchAllowed =
        false;

    // <변경부분> Pinch 또는 UI Touch가 시작된 뒤
    // 모든 손가락이 화면에서 떨어질 때까지
    // 새 1 Finger Gesture를 시작하지 않는다.
    //
    // 특히 2 Finger -> 1 Finger 전환 직후
    // 남은 손가락 때문에 Camera가 튀는 현상을 방지한다.
    private bool waitForAllMobileTouchesReleased =
        false;

    // <변경부분> 현재 한 손가락 Gesture를 소유한 Finger ID.
    private int mobilePrimaryFingerId =
        -1;

    // <변경부분> 한 손가락을 처음 누른 화면 좌표.
    private Vector2 mobileDragStartScreenPosition;

    // <변경부분> 모바일 Tap으로 끝났을 경우를 대비해
    // 최초로 누른 Battle Tile을 저장한다.
    private Tile mobilePressedTile =
        null;


    // 최소 줌 상태에서 이동을 막기 위한 허용 오차
    [SerializeField]
    private float minZoomMoveThreshold =
        0.01f;

    // 최소 줌 기준 화면 중심 위치
    private Vector3 baseCameraPosition;

    // <변경부분> 현재 실제 Camera 이동이 가능한 상태인지 반환한다.
    //
    // Click 판정 자체와 Camera 이동 가능 여부는 분리한다.
    // 최소 Zoom 상태에서도 기물/Tile Click은 정상 처리되고,
    // Drag했을 때 Camera 이동만 발생하지 않는다.
    public bool CanUseManualDrag
    {
        get
        {
            if (isPlayingStartZoomAnimation ||
                isPlayingLastPieceAttackCinematic)
            {
                return false;
            }

            if (BattleManager.Instance != null &&
                BattleManager.Instance
                    .CanUseManualCameraDrag ==
                false)
            {
                return false;
            }

            return
                CanMoveCameraByZoom();
        }
    }

    [Header("Camera Bounds")]
    // 기본 화면 기준 카메라 이동 가능 최소 좌표
    public Vector2 minBounds;

    // 기본 화면 기준 카메라 이동 가능 최대 좌표
    public Vector2 maxBounds;


    // 마지막 적 공격 시네마틱 도중
    // Scene 전환 또는 GameObject 비활성화가 발생해도
    // 느려진 Time.timeScale이 다음 상태까지 남지 않도록 복구한다.
    private void OnDisable()
    {
        // <변경부분> Scene 전환 / Camera 비활성화 시
        // PC Click / Drag 입력 상태를 초기화한다.
        ResetPCDragState();

        // <변경부분> 모바일 Tap / Drag / Pinch 상태도
        // 다음 활성화까지 남지 않도록 함께 초기화한다.
        ResetMobileGestureState();

        // 마지막 적 공격 Cinematic 도중 종료되었을 경우
        // TimeScale과 카메라 상태를 즉시 복원한다.
        RestoreLastPieceAttackCinematicImmediately();
    }


    private void Start()
    {
        // 실제 화면 제어에 사용할 Camera를 가져온다.
        cam = GetComponent<Camera>();

        // 카메라를 Orthographic 모드로 설정한다.
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
        // 시작 확대 애니메이션 중에는
        // 해당 Coroutine이 WorldRoot 배율을 직접 제어한다.
        //
        // 일반 사용자 Zoom / Drag 처리를 함께 실행하지 않는다.
        if (isPlayingStartZoomAnimation)
        {
            ClampCameraByZoom();
            return;
        }

        // 마지막 Enemy 공격 시네마틱 중에는
        // 전용 Coroutine이 카메라 위치와 Zoom을 직접 제어한다.
        //
        // 따라서 일반 사용자 입력은 받지 않는다.
        if (isPlayingLastPieceAttackCinematic)
        {
            return;
        }

        // 기물 이동 / 공격 / 흡수 / 고유스킬 등
        // Battle 행동 연출이 진행 중인지 확인한다.
        //
        // 전투 기물 이동은 현재 Tile의 World Position을 기준으로
        // 목표 위치를 계산하기 때문에,
        // 이동 도중 WorldRoot Scale이 바뀌면
        // 저장된 목표 위치와 실제 Tile 위치가 어긋날 수 있다.
        bool isBattleActionAnimating =
            BattleManager.Instance != null &&
            BattleManager.Instance.IsActionAnimating;

        if (isBattleActionAnimating)
        {
            // 행동 시작 직전에 입력된 Zoom 목표값까지 폐기한다.
            //
            // 단순히 Mouse / Pinch Zoom만 막으면
            // 이전 프레임에 만들어진 targetWorldScale을 향해
            // UpdateWorldZoom()이 계속 보간할 수 있기 때문에
            // 반드시 현재 배율에서 목표 배율을 고정해야 한다.
            targetWorldScale =
                currentWorldScale;
        }
        else
        {
            // 기물 좌표가 움직이지 않는 안전한 상태에서만
            // PC Wheel Zoom 입력을 허용한다.
            HandleMouseZoom();
        }

        // <변경부분> Pinch의 입력 소유권 판정은
        // Battle 행동 중에도 계속 수행한다.
        //
        // 그래야 행동 중 2 Finger -> 1 Finger로 바뀌어도
        // 남은 손가락이 새 Drag / Tap으로 잘못 이어지지 않는다.
        //
        // 실제 WorldRoot Zoom 변경만 행동 연출 중 차단한다.
        HandleMobilePinchZoom(
            isBattleActionAnimating == false
        );

        if (isBattleActionAnimating == false)
        {
            // 목표 Zoom까지 WorldRoot Scale을 부드럽게 적용한다.
            UpdateWorldZoom();
        }

        // Zoom과 달리 Camera 자체의 위치 이동은
        // WorldRoot의 좌표계를 변경하지 않으므로 기존 동작을 유지한다.

        // PC 좌클릭 Click / Camera Drag 처리
        HandlePCDrag();

        // 모바일 한 손가락 Tap / Camera Drag 처리
        HandleMobileDrag();

        // 현재 Zoom 배율에 맞게 카메라 이동 범위를 제한한다.
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

        while (elapsedTime < safeDuration &&
       isPlayingLastPieceAttackCinematic)
        {
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

        // OnDisable 또는 예외 복구로 시네마틱이 이미 종료됐다면
        // 다시 마지막 적 위치를 적용하지 않고 즉시 종료한다.
        if (isPlayingLastPieceAttackCinematic == false)
        {
            yield break;
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

    // 마지막 Enemy 공격 시네마틱을 즉시 종료하고
    // 시작 전에 저장했던 카메라 / 줌 / 시간 상태로 복구한다.
    //
    // 정상적인 공격 종료에서는 아래의
    // RestoreAfterLastPieceAttackCinematicRoutine()을 사용하고,
    // 기물 소실 / Scene 종료 / Controller 비활성화처럼
    // Coroutine을 끝까지 진행할 수 없는 예외 상황에서 사용한다.
    public void RestoreLastPieceAttackCinematicImmediately()
    {
        if (isPlayingLastPieceAttackCinematic == false)
        {
            return;
        }

        // 진행 중인 마지막 공격 Zoom Coroutine을 먼저 정리한다.
        if (lastPieceAttackZoomCoroutine != null)
        {
            StopCoroutine(
                lastPieceAttackZoomCoroutine
            );

            lastPieceAttackZoomCoroutine =
                null;
        }

        // 시네마틱 시작 전에 저장했던
        // 게임 시간 배율과 물리 업데이트 간격을 즉시 복구한다.
        Time.timeScale =
            savedTimeScaleBeforeCinematic;

        Time.fixedDeltaTime =
            savedFixedDeltaTimeBeforeCinematic;

        // 카메라 위치와 World Zoom도
        // 시네마틱 시작 전 상태로 즉시 복구한다.
        transform.position =
            savedCameraPositionBeforeCinematic;

        currentWorldScale =
            savedWorldScaleBeforeCinematic;

        targetWorldScale =
            savedTargetWorldScaleBeforeCinematic;

        ApplyWorldZoom();
        ClampCameraByZoom();

        // 마지막 공격 추적 상태를 완전히 초기화한다.
        lastPieceAttackTarget =
            null;

        isPlayingLastPieceAttackCinematic =
            false;
    }


    // 마지막 Enemy 공격이 끝난 뒤
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

    // <변경부분> 모바일 두 손가락 Pinch 확대/축소 처리.
    //
    // Pinch가 시작되는 순간 기존 1 Finger Tap / Drag 후보를 취소하고,
    // 두 손가락이 모두 떨어질 때까지 1 Finger Gesture 재시작을 막는다.
    private void HandleMobilePinchZoom(
        bool allowZoom)
    {
        // 손가락이 모두 떨어지면
        // 다음 Gesture를 받을 수 있도록 상태를 초기화한다.
        if (Input.touchCount == 0)
        {
            isMobilePinchActive =
                false;

            isMobilePinchAllowed =
                false;

            waitForAllMobileTouchesReleased =
                false;

            return;
        }

        // 한 손가락만 남았다면 Pinch 자체는 종료한다.
        //
        // 단 waitForAllMobileTouchesReleased는 유지한다.
        // 따라서 Pinch 후 남은 한 손가락으로
        // Camera Drag가 즉시 재개되지 않는다.
        if (Input.touchCount < 2)
        {
            isMobilePinchActive =
                false;

            isMobilePinchAllowed =
                false;

            return;
        }

        // 세 손가락 이상 입력은 지원하지 않는다.
        // 기존 1 Finger 후보를 취소하고 모두 뗄 때까지 기다린다.
        if (Input.touchCount != 2)
        {
            CancelMobileSingleTouchGesture();

            isMobilePinchActive =
                false;

            isMobilePinchAllowed =
                false;

            waitForAllMobileTouchesReleased =
                true;

            return;
        }

        Touch touch0 =
            Input.GetTouch(0);

        Touch touch1 =
            Input.GetTouch(1);

        // 두 번째 손가락이 들어온 최초 프레임에
        // Pinch가 이번 Gesture의 입력 소유권을 가져간다.
        if (isMobilePinchActive == false)
        {
            CancelMobileSingleTouchGesture();

            isMobilePinchActive =
                true;

            waitForAllMobileTouchesReleased =
                true;

            // UI 위에 위치한 손가락이 하나라도 있으면
            // 뒤쪽 Battle World Zoom이 반응하지 않도록 한다.
            isMobilePinchAllowed =
                Tile.IsScreenPositionOverUI(
                    touch0.position
                ) == false &&
                Tile.IsScreenPositionOverUI(
                    touch1.position
                ) == false;
        }

        // Battle Action 중이거나
        // UI Touch가 포함된 Pinch라면 실제 Zoom은 하지 않는다.
        //
        // 단 Pinch 입력 소유권 자체는 계속 유지한다.
        if (isMobilePinchAllowed == false ||
            allowZoom == false)
        {
            return;
        }

        // 손가락이 새로 추가되거나 제거되는 프레임에는
        // 이전 위치 계산이 불안정할 수 있으므로 Zoom을 적용하지 않는다.
        if (touch0.phase == TouchPhase.Began ||
            touch1.phase == TouchPhase.Began ||
            touch0.phase == TouchPhase.Ended ||
            touch1.phase == TouchPhase.Ended ||
            touch0.phase == TouchPhase.Canceled ||
            touch1.phase == TouchPhase.Canceled)
        {
            return;
        }

        // 이전 프레임의 두 Touch 위치
        Vector2 previousTouch0 =
            touch0.position -
            touch0.deltaPosition;

        Vector2 previousTouch1 =
            touch1.position -
            touch1.deltaPosition;

        // 이전 프레임의 두 손가락 거리
        float previousDistance =
            Vector2.Distance(
                previousTouch0,
                previousTouch1
            );

        // 현재 프레임의 두 손가락 거리
        float currentDistance =
            Vector2.Distance(
                touch0.position,
                touch1.position
            );

        // 두 손가락 사이 거리 변화량
        float pinchDelta =
            currentDistance -
            previousDistance;

        // 벌리면 확대 / 오므리면 축소
        targetWorldScale +=
            pinchDelta *
            pinchZoomSpeed;

        targetWorldScale =
            Mathf.Clamp(
                targetWorldScale,
                minWorldScale,
                maxWorldScale
            );
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

    // <변경부분> PC Battle 좌클릭을
    // Click과 Camera Drag로 구분하여 처리한다.
    //
    // UI를 제외한 Battle World 어디에서 시작하든
    // 일정 거리 이상 움직이면 Camera Drag,
    // 움직이지 않고 놓으면 기존 Battle Click으로 처리한다.
    private void HandlePCDrag()
    {
        // 좌클릭을 새로 누른 순간
        if (Input.GetMouseButtonDown(0))
        {
            ResetPCDragState();

            // UI에서 시작한 좌클릭은
            // Camera / Battle World 입력이 가져가지 않는다.
            if (Tile.IsPointerOverUI())
            {
                return;
            }

            isPCDragCandidate =
                true;

            pcDragStartScreenPosition =
                Input.mousePosition;

            // <변경부분> Click으로 끝났을 경우를 대비해
            // 최초로 누른 Battle Tile을 저장한다.
            //
            // 보드 바깥에서 시작했다면 null이며,
            // 이 경우 짧게 클릭해도 Battle 행동은 발생하지 않는다.
            pcPressedTile =
                GetTileAtScreenPosition(
                    pcDragStartScreenPosition
                );

            return;
        }

        if (isPCDragCandidate == false)
        {
            return;
        }

        // 좌클릭을 놓은 순간
        if (Input.GetMouseButtonUp(0))
        {
            // <변경부분> Threshold를 넘지 않았다면
            // Drag가 아니라 Click으로 확정한다.
            if (isPCDragging == false &&
                pcPressedTile != null &&
                Tile.IsPointerOverUI() == false &&
                BattleManager.Instance != null)
            {
                Tile releasedTile =
                    GetTileAtScreenPosition(
                        Input.mousePosition
                    );

                // 눌렀던 Tile과 실제로 뗀 Tile이 같을 때만
                // Battle 입력을 실행한다.
                //
                // 따라서 클릭 중 다른 Tile까지 움직였다가 놓는 경우
                // 잘못된 기물 선택/이동이 발생하지 않는다.
                if (releasedTile ==
                    pcPressedTile)
                {
                    BattleManager.Instance
                        .SelectTile(
                            pcPressedTile
                        );
                }
            }

            ResetPCDragState();

            return;
        }

        // 비정상적으로 Mouse Up을 놓친 경우 안전하게 초기화한다.
        if (Input.GetMouseButton(0) == false)
        {
            ResetPCDragState();
            return;
        }

        // 아직 Drag로 확정되지 않았다면
        // 최초 위치에서 움직인 거리를 확인한다.
        if (isPCDragging == false)
        {
            float dragDistance =
                Vector2.Distance(
                    pcDragStartScreenPosition,
                    Input.mousePosition
                );

            if (dragDistance <
                pcDragStartThresholdPixels)
            {
                return;
            }

            // <변경부분> Threshold를 넘는 순간
            // 이번 입력은 완전히 Camera Drag로 전환한다.
            //
            // 이후 Mouse Up 시 Battle Click은 실행되지 않는다.
            isPCDragging =
                true;

            // 기물 선택으로 진행 중이던 자동 Camera Focus가 있다면
            // 사용자의 수동 Drag 입력을 우선한다.
            if (pieceFocusCoroutine != null)
            {
                StopCoroutine(
                    pieceFocusCoroutine
                );

                pieceFocusCoroutine =
                    null;
            }
        }

        // Drag 자체는 확정되었지만
        // 현재 Camera를 움직일 수 없는 상태라면 이동만 하지 않는다.
        //
        // 이 경우에도 Click으로 되돌아가지는 않는다.
        if (CanUseManualDrag == false)
        {
            return;
        }

        float moveX =
            Input.GetAxis(
                "Mouse X"
            );

        float moveY =
            Input.GetAxis(
                "Mouse Y"
            );

        float zoomAdjustedSpeed =
            mouseDragSpeed /
            currentWorldScale;

        transform.position -=
            new Vector3(
                moveX *
                    zoomAdjustedSpeed,
                moveY *
                    zoomAdjustedSpeed,
                0f
            );
    }


// <변경부분> 현재 화면 좌표 아래에 존재하는 Battle Tile을 찾는다.
//
// Piece가 Tile보다 앞에 렌더링되는 상황에서도
// 같은 위치에 존재하는 Tile Collider를 찾을 수 있도록
// OverlapPointAll 결과 전체를 확인한다.
private Tile GetTileAtScreenPosition(
    Vector2 screenPosition)
{
    if (cam == null)
    {
        return null;
    }

    // 현재 Camera에서 보드가 위치한 Z = 0 평면까지의
    // 화면 좌표를 World 좌표로 변환한다.
    float worldPlaneDistance =
        Mathf.Abs(
            transform.position.z
        );

    Vector3 worldPosition =
        cam.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                worldPlaneDistance
            )
        );

    Collider2D[] hitColliders =
        Physics2D.OverlapPointAll(
            new Vector2(
                worldPosition.x,
                worldPosition.y
            )
        );

    for (int i = 0;
         i < hitColliders.Length;
         i++)
    {
        Collider2D hitCollider =
            hitColliders[i];

        if (hitCollider == null)
        {
            continue;
        }

        Tile tile =
            hitCollider.GetComponent<Tile>();

        if (tile == null)
        {
            tile =
                hitCollider
                    .GetComponentInParent<Tile>();
        }

        if (tile != null)
        {
            return tile;
        }
    }

    // Tile이 없다는 것은 보드 바깥 배경 영역이므로
    // BattleManager에서 Drag 가능 영역으로 처리할 수 있다.
    return null;
}


    // <변경부분> 현재 PC 좌클릭의
    // Click / Drag 판정 상태를 완전히 초기화한다.
    private void ResetPCDragState()
    {
        isPCDragCandidate =
            false;

        isPCDragging =
            false;

        pcDragStartScreenPosition =
            Vector2.zero;

        pcPressedTile =
            null;
    }



    // <변경부분> 현재 1 Finger Tap / Drag 후보만 초기화한다.
    //
    // Pinch 소유권과 모든 Touch Release 대기 상태는
    // 별도로 유지한다.
    private void CancelMobileSingleTouchGesture()
    {
        isMobileSingleTouchCandidate =
            false;

        isMobileDragging =
            false;

        mobilePrimaryFingerId =
            -1;

        mobileDragStartScreenPosition =
            Vector2.zero;

        mobilePressedTile =
            null;
    }


    // <변경부분> 모바일 Gesture 상태 전체를 초기화한다.
    private void ResetMobileGestureState()
    {
        CancelMobileSingleTouchGesture();

        isMobilePinchActive =
            false;

        isMobilePinchAllowed =
            false;

        waitForAllMobileTouchesReleased =
            false;
    }


    // <변경부분> 모바일 한 손가락 입력을
    // Tap과 Camera Drag로 구분하여 처리한다.
    //
    // Tap:
    // 같은 Tile에서 Threshold 미만으로 Touch End
    //
    // Drag:
    // Threshold 이상 이동하면 Camera가 입력을 소유한다.
    private void HandleMobileDrag()
    {
        // 모든 손가락이 떨어진 프레임에는
        // 다음 Gesture를 받을 수 있도록 상태를 정리한다.
        if (Input.touchCount == 0)
        {
            CancelMobileSingleTouchGesture();

            if (isMobilePinchActive == false)
            {
                waitForAllMobileTouchesReleased =
                    false;
            }

            return;
        }

        // 한 손가락 Gesture만 여기서 처리한다.
        //
        // 두 손가락 이상은
        // HandleMobilePinchZoom()이 입력을 소유한다.
        if (Input.touchCount != 1)
        {
            return;
        }

        // Pinch가 끝난 뒤 한 손가락만 남아 있거나
        // UI에서 시작한 Touch가 유지 중이면
        // 새 Drag / Tap을 만들지 않는다.
        if (waitForAllMobileTouchesReleased)
        {
            return;
        }

        Touch touch =
            Input.GetTouch(0);

        // 새 1 Finger Gesture 시작
        if (touch.phase == TouchPhase.Began)
        {
            CancelMobileSingleTouchGesture();

            // UI에서 시작한 Touch는
            // 이번 손가락이 완전히 떨어질 때까지
            // Battle World Gesture 후보로 전환하지 않는다.
            if (Tile.IsScreenPositionOverUI(
                    touch.position))
            {
                waitForAllMobileTouchesReleased =
                    true;

                return;
            }

            isMobileSingleTouchCandidate =
                true;

            mobilePrimaryFingerId =
                touch.fingerId;

            mobileDragStartScreenPosition =
                touch.position;

            // 보드 밖에서 시작하면 null을 저장한다.
            //
            // Drag는 가능하지만 짧은 Tap으로
            // Battle 행동은 발생하지 않는다.
            mobilePressedTile =
                GetTileAtScreenPosition(
                    touch.position
                );

            return;
        }

        // 최초 Gesture를 시작한 Finger만 계속 처리한다.
        if (isMobileSingleTouchCandidate == false ||
            touch.fingerId != mobilePrimaryFingerId)
        {
            return;
        }

        // Touch가 끝나면 Drag 여부를 기준으로 Tap을 확정한다.
        if (touch.phase == TouchPhase.Ended ||
            touch.phase == TouchPhase.Canceled)
        {
            bool shouldExecuteTap =
                touch.phase == TouchPhase.Ended &&
                isMobileDragging == false &&
                mobilePressedTile != null &&
                BattleManager.Instance != null &&
                Tile.IsScreenPositionOverUI(
                    touch.position
                ) == false;

            if (shouldExecuteTap)
            {
                Tile releasedTile =
                    GetTileAtScreenPosition(
                        touch.position
                    );

                // 처음 누른 Tile과 손을 뗀 Tile이 같을 때만
                // 기존 Battle Click을 실행한다.
                //
                // 따라서 Drag 직전 또는 손가락 흔들림 때문에
                // 다른 Tile을 잘못 선택하지 않는다.
                if (releasedTile ==
                    mobilePressedTile)
                {
                    BattleManager.Instance
                        .SelectTile(
                            mobilePressedTile
                        );
                }
            }

            CancelMobileSingleTouchGesture();

            return;
        }

        // 움직이지 않은 손가락은 Tap 후보 상태로 계속 유지한다.
        if (touch.phase != TouchPhase.Moved)
        {
            return;
        }

        // 아직 Drag로 확정되지 않았다면
        // 최초 Touch 위치에서 움직인 전체 거리를 검사한다.
        if (isMobileDragging == false)
        {
            float dragDistance =
                Vector2.Distance(
                    mobileDragStartScreenPosition,
                    touch.position
                );

            if (dragDistance <
                mobileDragStartThresholdPixels)
            {
                return;
            }

            // Threshold를 넘은 순간부터
            // 이번 입력은 Tap이 아닌 Camera Drag로 확정한다.
            isMobileDragging =
                true;

            // 기물 선택으로 진행 중이던 자동 Camera Focus보다
            // 사용자의 수동 Drag를 우선한다.
            if (pieceFocusCoroutine != null)
            {
                StopCoroutine(
                    pieceFocusCoroutine
                );

                pieceFocusCoroutine =
                    null;
            }
        }

        // Drag Gesture 자체는 이미 확정했다.
        //
        // 현재 Battle 상태 또는 최소 Zoom 때문에
        // 수동 Camera 이동이 잠겨 있다면 위치만 변경하지 않는다.
        //
        // 이 경우에도 Tap으로 되돌아가지는 않는다.
        if (CanUseManualDrag == false)
        {
            return;
        }

        // 확대된 상태일수록 이동량을 줄여
        // 기존 Camera 조작감을 유지한다.
        float zoomAdjustedSpeed =
            touchDragSpeed /
            Mathf.Max(
                0.01f,
                currentWorldScale
            );

        transform.position -=
            new Vector3(
                touch.deltaPosition.x *
                    zoomAdjustedSpeed,
                touch.deltaPosition.y *
                    zoomAdjustedSpeed,
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