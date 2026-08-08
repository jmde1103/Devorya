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
    // PC 마우스 우클릭 드래그 이동 속도
    [SerializeField, Min(0.001f)]
    private float mouseDragSpeed =
        0.3f;
    // 모바일 두 손가락 드래그 이동 속도
    [SerializeField, Min(0.0001f)]
    private float touchDragSpeed =
        0.01f;

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
        // 맵 시작 확대 연출 또는 마커 이동 추적 중에는
        // 사용자의 확대·축소 및 드래그 입력을 모두 차단한다.
        if (isPlayingMapStartZoom ||
            isFollowingMarker)
        {
            return;
        }

        // PC 마우스 휠 확대·축소를 처리한다.
        HandleMouseZoom();

        // 모바일 두 손가락 핀치 확대·축소를 처리한다.
        HandleMobilePinchZoom();

        // 현재 배율을 목표 배율로 부드럽게 변경한다.
        UpdateWorldZoom();

        // PC 마우스 우클릭 드래그를 처리한다.
        HandlePCDrag();

        // 모바일 두 손가락 드래그 이동을 처리한다.
        HandleMobileDrag();

        // 현재 배율과 카메라 화면 범위를 기준으로
        // 카메라가 월드맵 바깥으로 이동하지 않도록 제한한다.
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

    // 모바일 두 손가락 핀치 확대·축소 처리
    private void HandleMobilePinchZoom()
    {
        // 두 손가락 터치가 아니면 처리하지 않는다.
        //
        // 한 손가락 터치는 노드 선택에 사용하므로
        // 카메라 조작과 분리한다.
        if (Input.touchCount !=
            2)
        {
            return;
        }

        Touch firstTouch =
            Input.GetTouch(
                0
            );

        Touch secondTouch =
            Input.GetTouch(
                1
            );

        // 첫 번째 손가락의 이전 프레임 위치
        Vector2 previousFirstPosition =
            firstTouch.position -
            firstTouch.deltaPosition;

        // 두 번째 손가락의 이전 프레임 위치
        Vector2 previousSecondPosition =
            secondTouch.position -
            secondTouch.deltaPosition;

        // 이전 프레임의 두 손가락 거리
        float previousDistance =
            Vector2.Distance(
                previousFirstPosition,
                previousSecondPosition
            );

        // 현재 프레임의 두 손가락 거리
        float currentDistance =
            Vector2.Distance(
                firstTouch.position,
                secondTouch.position
            );

        // 두 손가락 사이 거리 변화량
        float pinchDelta =
            currentDistance -
            previousDistance;

        // 두 손가락을 벌리면 확대하고,
        // 오므리면 기본 배율 1까지 축소한다.
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
            // 추적을 시작하는 순간의 실제 배율을
            // 목표 배율로 고정하여 이동 중 줌 변화가 남지 않게 한다.
            targetWorldScale =
                currentWorldScale;

            // 진행 중이던 줌 보간도 현재 값으로 즉시 고정한다.
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

        // 시작 확대 연출 중에는 일반 마커 추적 상태를 사용하지 않는다.
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

    // PC 마우스 우클릭 드래그 이동 처리
    private void HandlePCDrag()
    {
        // 우클릭 중이 아니면 처리하지 않는다.
        if (Input.GetMouseButton(
                1) == false)
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

        // 확대 배율이 높아질수록
        // 같은 마우스 이동량으로 카메라가 너무 빠르게 움직이지 않도록 한다.
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

    // 모바일 두 손가락 드래그 이동 처리
    private void HandleMobileDrag()
    {
        // 한 손가락 터치는 노드 선택에 사용하므로
        // 두 손가락일 때만 카메라를 움직인다.
        if (Input.touchCount !=
            2)
        {
            return;
        }

        Touch firstTouch =
            Input.GetTouch(
                0
            );

        Touch secondTouch =
            Input.GetTouch(
                1
            );

        // 두 손가락 모두 움직이지 않았다면
        // 드래그 처리를 실행하지 않는다.
        if (firstTouch.phase !=
                TouchPhase.Moved &&
            secondTouch.phase !=
                TouchPhase.Moved)
        {
            return;
        }

        // 현재 프레임의 두 손가락 중심점
        Vector2 currentCenter =
            (
                firstTouch.position +
                secondTouch.position
            ) *
            0.5f;

        // 이전 프레임의 두 손가락 중심점
        Vector2 previousCenter =
            (
                firstTouch.position -
                firstTouch.deltaPosition +
                secondTouch.position -
                secondTouch.deltaPosition
            ) *
            0.5f;

        // 두 손가락 중심점 이동량
        Vector2 delta =
            currentCenter -
            previousCenter;

        // 확대 배율이 높을수록
        // 카메라 이동 속도를 줄여 조작감을 유지한다.
        float zoomAdjustedSpeed =
            touchDragSpeed /
            Mathf.Max(
                0.01f,
                currentWorldScale
            );

        transform.position -=
            new Vector3(
                delta.x *
                zoomAdjustedSpeed,
                delta.y *
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
    }
}