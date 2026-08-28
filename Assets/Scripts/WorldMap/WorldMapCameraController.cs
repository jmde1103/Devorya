using System.Collections;
using UnityEngine;
using UnityEngine.U2D;

// 월드맵 전용 카메라 컨트롤러
//
// Pixel Perfect Camera의 기준 화면은 유지하고,
// WorldMapRoot의 Scale을 변경하여 확대·축소한다.
//
// PC:
// 마우스 휠 확대·축소
// 마우스 우클릭 드래그 이동
//
// 모바일:
// 두 손가락 핀치 확대·축소
// 두 손가락 드래그 이동
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(PixelPerfectCamera))]
public class WorldMapCameraController : MonoBehaviour
{
    // 실제 월드맵을 촬영하는 Camera
    private Camera cam;

    // 도트 이미지의 픽셀 정렬을 담당하는 Pixel Perfect Camera
    private PixelPerfectCamera pixelPerfectCamera;

    [Header("Start Settings")]
    // 월드맵 진입 시 카메라가 시작할 월드 좌표
    [SerializeField]
    private Vector2 startPosition =
        Vector2.zero;

    // 2D 카메라의 Z축 위치
    [SerializeField]
    private float cameraZPosition =
        -10f;

    [Header("World Map Root")]
    // 맵, 노드, 경로, Player Marker, Fog를 포함하는
    // 월드맵 최상위 부모 Transform
    [SerializeField]
    private Transform worldMapRoot;

    [Header("World Zoom")]
    // 가장 멀리 축소한 기본 배율
    //
    // 1보다 작게 설정하지 않아
    // 노드와 도트 이미지가 지나치게 축소되지 않도록 한다.
    [SerializeField, Min(0.01f)]
    private float minWorldScale =
        1f;

    // 가장 가까이 확대할 수 있는 최대 배율
    [SerializeField, Min(0.01f)]
    private float maxWorldScale =
        2f;

    // 월드맵 진입 시 적용할 시작 배율
    [SerializeField, Min(0.01f)]
    private float startWorldScale =
        1f;

    // PC 마우스 휠 확대·축소 속도
    [SerializeField, Min(0.001f)]
    private float mouseZoomSpeed =
        0.15f;

    // 모바일 두 손가락 핀치 확대·축소 속도
    [SerializeField, Min(0.0001f)]
    private float pinchZoomSpeed =
        0.005f;

    // 현재 배율이 목표 배율을 따라가는 속도
    [SerializeField, Min(0.01f)]
    private float zoomSmoothSpeed =
        10f;

    [Header("Camera Move")]
    // <변경부분> PC WorldMap Camera Drag 감도
    [SerializeField, Min(0.001f)]
    private float mouseDragSpeed =
     0.3f;

    // <변경부분> 모바일 WorldMap Camera Drag 감도
    [SerializeField, Min(0.0001f)]
    private float touchDragSpeed =
        0.01f;

    [Header("PC Left Drag")]
    // <변경부분> 좌클릭을 Camera Drag로 판정할
    // 최소 화면 이동 거리.
    //
    // Battle과 동일하게 기본 8px을 사용한다.
    [SerializeField, Min(0f)]
    private float pcDragStartThresholdPixels =
        8f;

    // UI가 아닌 WorldMap 영역에서
    // 현재 좌클릭이 시작되었는지 여부.
    private bool isPCDragCandidate =
        false;

    // 현재 입력이 Threshold를 넘어
    // 실제 Camera Drag로 전환되었는지 여부.
    private bool isPCDragging =
        false;

    // 현재 좌클릭을 처음 누른 화면 좌표.
    private Vector2 pcDragStartScreenPosition;

    // <변경부분> 좌클릭 시작 위치에 있던 WorldMap Node.
    //
    // Drag 없이 동일 Node에서 Mouse Up 되었을 때만
    // Node Click으로 확정한다.
    private MapNodeRuntime pcPressedNode =
    null;


    [Header("Mobile Touch Input")]

    // <변경부분> 한 손가락 Tap과 Camera Drag를 구분하는 최소 화면 이동 거리.
    // 손가락 입력 오차를 고려해 PC보다 큰 값을 사용한다.
    [SerializeField, Min(0f)]
    private float mobileDragStartThresholdPixels =
        24f;

    // <변경부분> 현재 한 손가락 입력이
    // Tap / Drag 판정 후보인지 확인한다.
    private bool isMobileSingleTouchCandidate =
        false;

    // <변경부분> Threshold를 넘어
    // 실제 Camera Drag로 확정되었는지 확인한다.
    private bool isMobileDragging =
        false;

    // <변경부분> 현재 두 손가락 Pinch가
    // Gesture 입력을 소유하고 있는지 확인한다.
    private bool isMobilePinchActive =
        false;

    // <변경부분> 현재 Pinch가 실제 WorldMap Zoom을
    // 수행할 수 있는 입력인지 저장한다.
    //
    // 두 손가락 중 하나라도 UI에서 시작했다면 false.
    private bool isMobilePinchAllowed =
        false;

    // <변경부분> Pinch 또는 UI Touch 이후
    // 모든 손가락이 화면에서 떨어질 때까지
    // 새로운 한 손가락 Gesture를 시작하지 않는다.
    //
    // 2 Finger -> 1 Finger 전환 직후
    // Camera가 갑자기 움직이는 현상을 방지한다.
    private bool waitForAllMobileTouchesReleased =
        false;

    // <변경부분> 현재 한 손가락 Gesture를 소유한 Finger ID.
    private int mobilePrimaryFingerId =
        -1;

    // <변경부분> 한 손가락을 처음 누른 화면 좌표.
    private Vector2 mobileDragStartScreenPosition;

    // <변경부분> Tap으로 끝날 가능성이 있는
    // 최초 입력 위치의 WorldMap Node.
    private MapNodeRuntime mobilePressedNode =
        null;


    [Header("Map Start Zoom")]
    // 맵 씬이 시작될 때 적용할 초기 배율
    [SerializeField, Min(0.01f)]
    private float mapStartZoomFrom =
        1f;

    // 맵 씬 시작 연출이 끝날 때 도달할 배율
    [SerializeField, Min(0.01f)]
    private float mapStartZoomTo =
        2f;

    // 1배율에서 2배율까지 확대하는 데 걸리는 시간
    [SerializeField, Min(0.01f)]
    private float mapStartZoomDuration =
        0.8f;

    // 맵 시작 카메라 연출이 실행 중인지 확인한다.
    private bool isPlayingMapStartZoom;

    [Header("Map Close Zoom")]
    // 전투 씬으로 이동하기 전에
    // 현재 확대 상태에서 축소할 최종 배율
    [SerializeField, Min(0.01f)]
    private float mapCloseZoomTo =
        1f;

    // 맵 종료 축소 연출에 걸리는 시간
    [SerializeField, Min(0.01f)]
    private float mapCloseZoomDuration =
        0.65f;

    // 맵 종료 축소 연출이 실행 중인지 확인한다.
    private bool isPlayingMapCloseZoom;

    [Header("Marker Follow")]
    // 마커 이동 중 카메라가
    // 마커 위치를 따라가는 속도
    [SerializeField, Min(0.01f)]
    private float markerFollowSmoothSpeed =
        8f;

    // 현재 카메라가 추적할 Player Marker
    private Transform markerFollowTarget;

    // 마커 이동 중 카메라 추적과
    // 사용자 줌·드래그 입력 잠금이 활성화됐는지 확인한다.
    private bool isFollowingMarker;

    [Header("Map Bounds")]
    // WorldMapRoot Scale이 1일 때
    // 월드맵의 왼쪽 아래 로컬 좌표
    [SerializeField]
    private Vector2 mapMinBounds =
    new Vector2(
        -25f,
        -15f
    );

    // WorldMapRoot Scale이 1일 때
    // 월드맵의 오른쪽 위 로컬 좌표
    [SerializeField]
    private Vector2 mapMaxBounds =
        new Vector2(
            25f,
            15f
        );

    // 현재 화면에 적용된 WorldMapRoot 배율
    private float currentWorldScale =
        1f;

    // 마우스 휠이나 핀치 입력으로 변경되는 목표 배율
    //
    // 마커 이동 중에는 현재 배율과 같은 값으로 고정하여
    // 이동 도중 WorldMapRoot Scale이 변하지 않도록 한다.
    private float targetWorldScale =
        1f;

    private void Awake()
    {
        // 카메라 컴포넌트를 가져온다.
        cam =
            GetComponent<Camera>();

        // Pixel Perfect Camera 컴포넌트를 가져온다.
        pixelPerfectCamera =
            GetComponent<PixelPerfectCamera>();

        // 월드맵은 2D 화면이므로
        // 항상 Orthographic 카메라로 사용한다.
        cam.orthographic =
            true;

        // 최소·최대 배율이 Inspector에서 뒤집혀 있어도
        // 정상적인 순서로 자동 보정한다.
        float safeMinScale =
            Mathf.Min(
                minWorldScale,
                maxWorldScale
            );

        float safeMaxScale =
            Mathf.Max(
                minWorldScale,
                maxWorldScale
            );

        minWorldScale =
            safeMinScale;

        maxWorldScale =
            safeMaxScale;

        // 월드맵은 기본 크기보다 작아지지 않도록
        // 최소 배율을 반드시 1 이상으로 제한한다.
        minWorldScale =
            Mathf.Max(
                1f,
                minWorldScale
            );

        maxWorldScale =
            Mathf.Max(
                minWorldScale,
                maxWorldScale
            );

        // 시작 배율을 최소·최대 범위 안으로 제한한다.
        currentWorldScale =
            Mathf.Clamp(
                startWorldScale,
                minWorldScale,
                maxWorldScale
            );

        targetWorldScale =
            currentWorldScale;

        // 시작 카메라 위치를 적용한다.
        transform.position =
            new Vector3(
                startPosition.x,
                startPosition.y,
                cameraZPosition
            );

        // 시작 배율을 WorldMapRoot에 즉시 적용한다.
        ApplyWorldScale();
    }

    private void Start()
    {
        // Pixel Perfect Camera가 기준 해상도를 적용한 뒤
        // 현재 화면 크기에 맞춰 카메라 위치를 제한한다.
        ClampCameraPosition();
    }

    private void Update()
    {
        // Map Start Zoom / Map Close Zoom / Marker Follow 중에는
        // 자동 연출이 Camera와 WorldMapRoot를 직접 제어한다.
        if (isPlayingMapStartZoom ||
            isPlayingMapCloseZoom ||
            isFollowingMarker)
        {
            return;
        }

        // PC Mouse Wheel Zoom
        HandleMouseZoom();

        // <변경부분> 모바일 두 손가락은
        // Zoom 전용 Gesture로 처리한다.
        HandleMobilePinchZoom();

        // 현재 Zoom 배율을 부드럽게 적용한다.
        UpdateWorldZoom();

        // PC Left Click / Drag
        HandlePCDrag();

        // <변경부분> 모바일 한 손가락
        // Tap / Camera Drag를 통합 처리한다.
        HandleMobileDrag();

        // 현재 WorldMap Scale과 화면 크기에 맞춰
        // Camera 위치를 최종 제한한다.
        ClampCameraPosition();
    }
    // Player Marker의 이동이 현재 프레임에 적용된 다음
    // 카메라가 마커의 최신 월드 위치를 따라가도록 한다.
    private void LateUpdate()
    {
        if (isFollowingMarker == false ||
            markerFollowTarget == null)
        {
            return;
        }

        Vector3 currentCameraPosition =
            transform.position;

        Vector3 targetCameraPosition =
            new Vector3(
                markerFollowTarget.position.x,
                markerFollowTarget.position.y,
                cameraZPosition
            );

        // 프레임 속도와 관계없이
        // 카메라가 마커를 부드럽게 따라가도록 보간한다.
        float followLerpAmount =
            1f -
            Mathf.Exp(
                -markerFollowSmoothSpeed *
                Time.unscaledDeltaTime
            );

        transform.position =
            Vector3.Lerp(
                currentCameraPosition,
                targetCameraPosition,
                followLerpAmount
            );

        // 카메라가 마커를 따라가더라도
        // 현재 월드맵의 표시 범위를 벗어나지 않도록 제한한다.
        ClampCameraPosition();
    }

    // PC 마우스 휠 확대·축소 처리
    private void HandleMouseZoom()
    {
        float scroll =
            Input.mouseScrollDelta.y;

        // 휠 입력이 없으면 처리하지 않는다.
        if (Mathf.Abs(
                scroll) <=
            0.01f)
        {
            return;
        }

        // 휠을 위로 올리면 WorldMapRoot 배율이 증가하여 확대된다.
        // 휠을 아래로 내리면 기본 배율 1까지 축소된다.
        targetWorldScale +=
            scroll *
            mouseZoomSpeed;

        targetWorldScale =
            Mathf.Clamp(
                targetWorldScale,
                minWorldScale,
                maxWorldScale
            );
    }

    // <변경부분> 모바일 두 손가락 Pinch 확대/축소를 처리한다.
    //
    // 두 번째 손가락이 들어오는 순간
    // 기존 1 Finger Tap / Drag 후보를 취소한다.
    //
    // 이후 한 손가락만 남더라도
    // 모든 손가락이 완전히 떨어질 때까지
    // 새로운 1 Finger Gesture를 시작하지 않는다.
    private void HandleMobilePinchZoom()
    {
        // 모든 손가락이 떨어지면
        // 다음 Gesture를 받을 수 있도록 초기화한다.
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

        // Pinch 후 한 손가락만 남은 상태.
        //
        // Pinch 자체는 종료하지만
        // 모든 Touch Release 대기는 유지한다.
        if (Input.touchCount < 2)
        {
            isMobilePinchActive =
                false;

            isMobilePinchAllowed =
                false;

            return;
        }

        // 3 Finger 이상은 지원하지 않는다.
        //
        // 현재 Gesture를 취소하고
        // 모든 손가락이 떨어질 때까지 기다린다.
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

        Touch firstTouch =
            Input.GetTouch(0);

        Touch secondTouch =
            Input.GetTouch(1);

        // 두 번째 손가락이 들어온 순간
        // Pinch가 이번 Gesture의 입력 소유권을 가져간다.
        if (isMobilePinchActive == false)
        {
            CancelMobileSingleTouchGesture();

            isMobilePinchActive =
                true;

            waitForAllMobileTouchesReleased =
                true;

            // 두 손가락 중 하나라도 UI 위에 있다면
            // 뒤쪽 WorldMap Zoom을 실행하지 않는다.
            isMobilePinchAllowed =
                MapNodeRuntime.IsScreenPositionOverUI(
                    firstTouch.position
                ) == false &&
                MapNodeRuntime.IsScreenPositionOverUI(
                    secondTouch.position
                ) == false;
        }

        if (isMobilePinchAllowed == false)
        {
            return;
        }

        // 손가락이 추가/제거되는 프레임에는
        // 이전 위치 계산이 불안정할 수 있으므로 Zoom하지 않는다.
        if (firstTouch.phase == TouchPhase.Began ||
            secondTouch.phase == TouchPhase.Began ||
            firstTouch.phase == TouchPhase.Ended ||
            secondTouch.phase == TouchPhase.Ended ||
            firstTouch.phase == TouchPhase.Canceled ||
            secondTouch.phase == TouchPhase.Canceled)
        {
            return;
        }

        // 이전 프레임 첫 번째 손가락 위치
        Vector2 previousFirstPosition =
            firstTouch.position -
            firstTouch.deltaPosition;

        // 이전 프레임 두 번째 손가락 위치
        Vector2 previousSecondPosition =
            secondTouch.position -
            secondTouch.deltaPosition;

        float previousDistance =
            Vector2.Distance(
                previousFirstPosition,
                previousSecondPosition
            );

        float currentDistance =
            Vector2.Distance(
                firstTouch.position,
                secondTouch.position
            );

        float pinchDelta =
            currentDistance -
            previousDistance;

        // 손가락을 벌리면 확대,
        // 오므리면 축소한다.
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

    // 현재 WorldMapRoot 배율을
    // 목표 배율까지 부드럽게 변경한다.
    private void UpdateWorldZoom()
    {
        currentWorldScale =
            Mathf.Lerp(
                currentWorldScale,
                targetWorldScale,
                Time.unscaledDeltaTime *
                zoomSmoothSpeed
            );

        // 목표 배율과 거의 같아지면
        // 소수점 오차 없이 목표값으로 고정한다.
        if (Mathf.Abs(
                currentWorldScale -
                targetWorldScale) <
            0.0001f)
        {
            currentWorldScale =
                targetWorldScale;
        }

        ApplyWorldScale();
    }

    // 현재 배율을 WorldMapRoot 전체에 적용한다.
   private void ApplyWorldScale()
{
    if (worldMapRoot == null)
    {
        return;
    }

    // 맵, 노드, 경로, Player Marker, Fog를
    // 동일한 배율로 함께 확대·축소한다.
    worldMapRoot.localScale =
        new Vector3(
            currentWorldScale,
            currentWorldScale,
            1f
        );
    }

    // 마커 이동에 맞춰 카메라 추적과
    // 사용자 카메라 조작 잠금 상태를 변경한다.
    public void SetMarkerFollow(
        Transform markerTarget,
        bool shouldFollow)
    {
        isFollowingMarker =
            shouldFollow &&
            markerTarget != null;

        markerFollowTarget =
            isFollowingMarker
                ? markerTarget
                : null;

        if (isFollowingMarker)
        {
            // <변경부분> Marker 자동 추적이 시작되면
            // 진행 중이던 PC 입력을 취소한다.
            ResetPCDragState();

            // <변경부분> 모바일 Tap / Drag / Pinch 상태도
            // 함께 초기화해 자동 Camera 연출과 충돌하지 않게 한다.
            ResetMobileGestureState();

            targetWorldScale =
                currentWorldScale;

            ApplyWorldScale();
        }
    }

    // 맵 씬이 시작된 뒤 카메라를 Player Marker 중심으로 맞추고,
    // 마커를 화면 중심에 유지한 채 1배율에서 2배율까지 확대한다.
    public IEnumerator PlayMapStartZoomRoutine(
        Transform playerMarker)
    {
        if (playerMarker == null ||
            worldMapRoot == null)
        {
            yield break;
        }

        isPlayingMapStartZoom =
    true;

        // <변경부분> Map Start Zoom이 시작되면
        // 진행 중이던 PC Click / Drag를 취소한다.
        ResetPCDragState();

        // <변경부분> 모바일 Gesture도 함께 취소하여
        // 자동 Zoom 종료 후 이전 Touch 상태가 이어지지 않게 한다.
        ResetMobileGestureState();

        markerFollowTarget =
            null;

        isFollowingMarker =
            false;

        float safeStartScale =
            Mathf.Clamp(
                mapStartZoomFrom,
                minWorldScale,
                maxWorldScale
            );

        float safeTargetScale =
            Mathf.Clamp(
                mapStartZoomTo,
                minWorldScale,
                maxWorldScale
            );

        float safeDuration =
            Mathf.Max(
                0.01f,
                mapStartZoomDuration
            );

        // 시작 배율을 즉시 1배율 상태로 적용한다.
        currentWorldScale =
            safeStartScale;

        targetWorldScale =
            safeStartScale;

        ApplyWorldScale();

        // WorldMapRoot Scale 적용 후 변경된 마커의 실제 월드 위치를 기준으로
        // 카메라를 마커 중심에 즉시 배치한다.
        CenterCameraOnMarker(
            playerMarker
        );

        yield return null;

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    safeDuration
                );

            // 시작과 끝이 급격하게 변하지 않도록
            // SmoothStep으로 확대 속도를 부드럽게 만든다.
            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            currentWorldScale =
                Mathf.Lerp(
                    safeStartScale,
                    safeTargetScale,
                    smoothTime
                );

            targetWorldScale =
                currentWorldScale;

            // WorldMapRoot를 확대하면 자식인 마커의 월드 위치도 변하므로
            // Scale을 먼저 적용한 다음 카메라를 다시 마커 중심에 맞춘다.
            ApplyWorldScale();

            CenterCameraOnMarker(
                playerMarker
            );

            yield return null;
        }

        // 마지막 프레임에서 정확히 목표 배율로 고정한다.
        currentWorldScale =
            safeTargetScale;

        targetWorldScale =
            safeTargetScale;

        ApplyWorldScale();

        CenterCameraOnMarker(
            playerMarker
        );

        isPlayingMapStartZoom =
            false;
    }

    // 전투 씬으로 이동하기 전에
    // 현재 확대 배율에서 1배율까지 축소한다.
    //
    // 축소 중에도 Player Marker가 화면 중심에 유지되도록
    // 매 프레임 카메라 위치를 다시 맞춘다.
    public IEnumerator PlayMapCloseZoomRoutine(
        Transform playerMarker,
        float requestedDuration = -1f)
    {
        if (playerMarker == null ||
            worldMapRoot == null)
        {
            yield break;
        }

        isPlayingMapCloseZoom =
     true;

        // <변경부분> Map Close Zoom 시작 시
        // 진행 중인 PC 입력을 취소한다.
        ResetPCDragState();

        // <변경부분> 모바일 입력도 함께 초기화한다.
        ResetMobileGestureState();

        markerFollowTarget =
            null;

        isFollowingMarker =
            false;

        float startScale =
            currentWorldScale;

        float targetScale =
            Mathf.Clamp(
                mapCloseZoomTo,
                minWorldScale,
                maxWorldScale
            );

        float safeDuration =
            requestedDuration > 0f
                ? requestedDuration
                : mapCloseZoomDuration;

        safeDuration =
            Mathf.Max(
                0.01f,
                safeDuration
            );

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    safeDuration
                );

            // 시작은 부드럽고 마지막에는 천천히 멈추도록 처리한다.
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

            // WorldMapRoot 축소 후 달라진 마커 월드 좌표를 기준으로
            // 카메라를 계속 마커 중심에 유지한다.
            ApplyWorldScale();

            CenterCameraOnMarker(
                playerMarker
            );

            yield return null;
        }

        currentWorldScale =
            targetScale;

        targetWorldScale =
            targetScale;

        ApplyWorldScale();

        CenterCameraOnMarker(
            playerMarker
        );

        isPlayingMapCloseZoom =
            false;
    }

    // 지정한 Player Marker가 화면 중심에 오도록
    // 카메라의 X·Y 위치를 즉시 맞춘다.
    private void CenterCameraOnMarker(
        Transform playerMarker)
    {
        if (playerMarker == null)
        {
            return;
        }

        transform.position =
            new Vector3(
                playerMarker.position.x,
                playerMarker.position.y,
                cameraZPosition
            );

        // 마커가 맵 가장자리 근처에 있을 경우에는
        // 카메라 화면이 월드맵 바깥으로 나가지 않도록 최종 제한한다.
        ClampCameraPosition();
    }

    private void HandlePCDrag()
    {
        // <변경부분> Android / iOS에서는 Touch가
        // Legacy Mouse 입력으로 함께 전달될 수 있다.
        //
        // 모바일 WorldMap 입력은
        // HandleMobileDrag() / HandleMobilePinchZoom()이 전담하므로
        // PC Mouse 처리기가 Touch를 중복으로 받지 않도록 한다.
        //
        // 특히 두 손가락 Pinch 중 첫 번째 Touch가
        // Mouse Drag로 해석되어 Camera가 같이 움직이는 현상을 방지한다.
        if (Application.isMobilePlatform)
        {
            ResetPCDragState();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            ResetPCDragState();

            if (MapNodeRuntime.IsPointerOverUI())
            {
                return;
            }

            isPCDragCandidate =
                true;

            pcDragStartScreenPosition =
                Input.mousePosition;

            pcPressedNode =
                GetNodeAtScreenPosition(
                    pcDragStartScreenPosition
                );

            return;
        }

        if (isPCDragCandidate == false)
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isPCDragging == false &&
                pcPressedNode != null &&
                MapNodeRuntime.IsPointerOverUI() ==
                    false)
            {
                MapNodeRuntime releasedNode =
                    GetNodeAtScreenPosition(
                        Input.mousePosition
                    );

                if (releasedNode ==
                    pcPressedNode)
                {
                    pcPressedNode.EnterNode();
                }
            }

            ResetPCDragState();

            return;
        }

        if (Input.GetMouseButton(0) == false)
        {
            ResetPCDragState();
            return;
        }

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

            isPCDragging =
                true;
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
            Mathf.Max(
                0.01f,
                currentWorldScale
            );

        transform.position -=
            new Vector3(
                moveX *
                    zoomAdjustedSpeed,
                moveY *
                    zoomAdjustedSpeed,
                0f
            );
    }

    // <변경부분> 지정한 화면 좌표 아래에 존재하는
    // WorldMap Node를 찾아 반환한다.
    private MapNodeRuntime GetNodeAtScreenPosition(
        Vector2 screenPosition)
    {
        if (cam == null)
        {
            return null;
        }

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

            MapNodeRuntime node =
                hitCollider
                    .GetComponent<MapNodeRuntime>();

            if (node == null)
            {
                node =
                    hitCollider
                        .GetComponentInParent<MapNodeRuntime>();
            }

            if (node != null)
            {
                return node;
            }
        }

        return null;
    }


    // <변경부분> 현재 PC WorldMap
    // Click / Drag 판정 상태를 초기화한다.
    private void ResetPCDragState()
    {
        isPCDragCandidate =
            false;

        isPCDragging =
            false;

        pcDragStartScreenPosition =
            Vector2.zero;

        pcPressedNode =
            null;
    }

    // <변경부분> 현재 1 Finger Tap / Drag 후보만 초기화한다.
    //
    // Pinch 상태와 모든 Touch Release 대기 상태는
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

        mobilePressedNode =
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
    // Node Tap과 Camera Drag로 구분한다.
    //
    // Tap:
    // 같은 Node에서 Threshold 미만으로 Touch 종료.
    //
    // Drag:
    // WorldMap 어느 위치에서든 Threshold 이상 이동하면
    // Camera Drag로 확정한다.
    private void HandleMobileDrag()
    {
        // 모든 손가락이 떨어지면
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

        // 한 손가락만 처리한다.
        //
        // 두 손가락 이상은
        // HandleMobilePinchZoom()이 소유한다.
        if (Input.touchCount != 1)
        {
            return;
        }

        // Pinch 직후 한 손가락이 남아 있거나
        // UI에서 시작한 Touch가 아직 유지 중이라면
        // 새로운 Tap / Drag를 시작하지 않는다.
        if (waitForAllMobileTouchesReleased)
        {
            return;
        }

        Touch touch =
            Input.GetTouch(0);

        // 새로운 한 손가락 Gesture 시작.
        if (touch.phase == TouchPhase.Began)
        {
            CancelMobileSingleTouchGesture();

            // UI에서 시작한 Touch는
            // 이번 손가락이 완전히 떨어질 때까지
            // WorldMap 입력으로 전환하지 않는다.
            if (MapNodeRuntime.IsScreenPositionOverUI(
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

            // Node 위에서 시작했다면 Tap 후보로 저장한다.
            //
            // 빈 Map 영역에서 시작했다면 null.
            // 이 경우 짧게 Tap해도 아무 행동은 없지만
            // Drag는 정상적으로 가능하다.
            mobilePressedNode =
                GetNodeAtScreenPosition(
                    touch.position
                );

            return;
        }

        // Gesture를 처음 시작한 Finger만 계속 처리한다.
        if (isMobileSingleTouchCandidate == false ||
            touch.fingerId != mobilePrimaryFingerId)
        {
            return;
        }

        // 손가락을 뗀 순간 Tap 여부를 확정한다.
        if (touch.phase == TouchPhase.Ended ||
            touch.phase == TouchPhase.Canceled)
        {
            bool shouldExecuteTap =
                touch.phase == TouchPhase.Ended &&
                isMobileDragging == false &&
                mobilePressedNode != null &&
                MapNodeRuntime.IsScreenPositionOverUI(
                    touch.position
                ) == false;

            if (shouldExecuteTap)
            {
                MapNodeRuntime releasedNode =
                    GetNodeAtScreenPosition(
                        touch.position
                    );

                // 처음 누른 Node와
                // 손을 뗀 Node가 동일할 때만 EnterNode().
                //
                // 손가락이 다른 Node까지 움직였다면
                // 잘못된 Node 진입을 발생시키지 않는다.
                if (releasedNode ==
                    mobilePressedNode)
                {
                    mobilePressedNode.EnterNode();
                }
            }

            CancelMobileSingleTouchGesture();

            return;
        }

        // 움직이지 않았다면 계속 Tap 후보 상태로 유지한다.
        if (touch.phase != TouchPhase.Moved)
        {
            return;
        }

        // 아직 Camera Drag로 확정되지 않았다면
        // 최초 Touch 위치부터의 전체 이동 거리를 검사한다.
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

            // Threshold를 넘은 순간
            // 이번 입력은 Node Tap이 아니라
            // Camera Drag로 완전히 확정된다.
            isMobileDragging =
                true;
        }

        // 현재 Zoom 배율에 맞춰
        // Camera 이동 감도를 보정한다.
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

    // 현재 WorldMapRoot 배율과
    // Pixel Perfect Camera가 실제로 보여주는 화면 크기를 기준으로
    // 카메라의 이동 범위를 제한한다.
    private void ClampCameraPosition()
    {
        if (cam == null ||
            worldMapRoot == null)
        {
            return;
        }

        // Orthographic Camera가 화면에 표시하는 세로 절반 크기
        float cameraHalfHeight =
            cam.orthographicSize;

        // 현재 화면 비율을 반영한 가로 절반 크기
        float cameraHalfWidth =
            cameraHalfHeight *
            cam.aspect;

        // WorldMapRoot의 현재 월드 중심 위치
        Vector3 rootPosition =
            worldMapRoot.position;

        // Scale이 적용된 실제 월드맵 왼쪽 경계
        float scaledMapMinX =
            rootPosition.x +
            mapMinBounds.x *
            currentWorldScale;

        // Scale이 적용된 실제 월드맵 오른쪽 경계
        float scaledMapMaxX =
            rootPosition.x +
            mapMaxBounds.x *
            currentWorldScale;

        // Scale이 적용된 실제 월드맵 아래쪽 경계
        float scaledMapMinY =
            rootPosition.y +
            mapMinBounds.y *
            currentWorldScale;

        // Scale이 적용된 실제 월드맵 위쪽 경계
        float scaledMapMaxY =
            rootPosition.y +
            mapMaxBounds.y *
            currentWorldScale;

        // 카메라 화면이 맵 바깥을 보여주지 않는
        // X축 이동 가능 범위
        float cameraMinX =
            scaledMapMinX +
            cameraHalfWidth;

        float cameraMaxX =
            scaledMapMaxX -
            cameraHalfWidth;

        // 카메라 화면이 맵 바깥을 보여주지 않는
        // Y축 이동 가능 범위
        float cameraMinY =
            scaledMapMinY +
            cameraHalfHeight;

        float cameraMaxY =
            scaledMapMaxY -
            cameraHalfHeight;

        Vector3 currentPosition =
            transform.position;

        // 현재 화면이 맵의 가로보다 넓다면
        // X축은 맵 중앙에 고정한다.
        float clampedX =
            cameraMinX >
            cameraMaxX
                ? (
                    scaledMapMinX +
                    scaledMapMaxX
                  ) *
                  0.5f
                : Mathf.Clamp(
                    currentPosition.x,
                    cameraMinX,
                    cameraMaxX
                );

        // 현재 화면이 맵의 세로보다 높다면
        // Y축은 맵 중앙에 고정한다.
        float clampedY =
            cameraMinY >
            cameraMaxY
                ? (
                    scaledMapMinY +
                    scaledMapMaxY
                  ) *
                  0.5f
                : Mathf.Clamp(
                    currentPosition.y,
                    cameraMinY,
                    cameraMaxY
                );

        transform.position =
            new Vector3(
                clampedX,
                clampedY,
                cameraZPosition
            );
    }

    private void OnDisable()
    {
        // <변경부분> Scene 전환 / Camera 비활성화 시
        // PC Click / Drag 상태를 초기화한다.
        ResetPCDragState();

        // <변경부분> 모바일 Tap / Drag / Pinch 상태도
        // 다음 활성화까지 남지 않도록 초기화한다.
        ResetMobileGestureState();
    }

    private void OnValidate()
    {
        // Inspector에서 값을 수정할 때도
        // 최소 배율이 1보다 작아지지 않도록 유지한다.
        minWorldScale =
            Mathf.Max(
                1f,
                minWorldScale
            );

        maxWorldScale =
            Mathf.Max(
                minWorldScale,
                maxWorldScale
            );

        startWorldScale =
      Mathf.Clamp(
          startWorldScale,
          minWorldScale,
          maxWorldScale
      );

        // 맵 시작 확대 배율도 현재 최소·최대 배율 범위를 벗어나지 않게 한다.
        mapStartZoomFrom =
            Mathf.Clamp(
                mapStartZoomFrom,
                minWorldScale,
                maxWorldScale
            );

        mapStartZoomTo =
    Mathf.Clamp(
        mapStartZoomTo,
        minWorldScale,
        maxWorldScale
    );

        // 맵 종료 축소 배율도 현재 최소·최대 범위 안으로 제한한다.
        mapCloseZoomTo =
            Mathf.Clamp(
                mapCloseZoomTo,
                minWorldScale,
                maxWorldScale
            );
    }
}