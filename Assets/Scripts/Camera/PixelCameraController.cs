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