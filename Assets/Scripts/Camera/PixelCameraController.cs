using UnityEngine;
using UnityEngine.U2D;

// =========================================================
// Pixel Perfect 2D Camera Controller
// =========================================================
// 기능
// - 우클릭 드래그 이동
// - 모바일 드래그 이동
// - 카메라 이동 범위 제한
// - Pixel Perfect 대응
// =========================================================

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(PixelPerfectCamera))]
public class PixelCameraController : MonoBehaviour
{
    // =========================================================
    // Camera 참조
    // =========================================================

    // Camera 컴포넌트
    private Camera cam;

    // Pixel Perfect Camera 컴포넌트
    private PixelPerfectCamera pixelCam;

    // =========================================================
    // 시작 설정
    // =========================================================

    [Header("Start Settings")]

    // 게임 시작 시 카메라 위치
    public Vector2 startPosition = Vector2.zero;

    // =========================================================
    // 이동 설정
    // =========================================================

    [Header("Move")]

    // 드래그 이동 속도
    public float dragSpeed = 0.3f;

    // =========================================================
    // Camera Bounds
    // =========================================================

    [Header("Camera Bounds")]

    // 이동 가능 최소 좌표
    public Vector2 minBounds;

    // 이동 가능 최대 좌표
    public Vector2 maxBounds;

    // =========================================================
    // Start
    // =========================================================

    void Start()
    {
        // Camera 가져오기
        cam = GetComponent<Camera>();

        // Pixel Perfect Camera 가져오기
        pixelCam = GetComponent<PixelPerfectCamera>();

        // Orthographic 강제
        cam.orthographic = true;

        // =====================================================
        // 시작 위치 설정
        // =====================================================

        transform.position = new Vector3(
            startPosition.x,
            startPosition.y,
            -10f
        );
    }

    // =========================================================
    // Update
    // =========================================================

    void Update()
    {
        // PC 드래그 이동
        HandlePCDrag();

        // 모바일 드래그 이동
        HandleMobileDrag();

        // 카메라 제한
        ClampCamera();
    }

    // =========================================================
    // PC 드래그 이동
    // 마우스 우클릭 드래그
    // =========================================================

    void HandlePCDrag()
    {
        // 우클릭 중일 때만 실행
        if (Input.GetMouseButton(1))
        {
            // 마우스 이동량
            float moveX =
                Input.GetAxis("Mouse X");

            float moveY =
                Input.GetAxis("Mouse Y");

            // 카메라 이동
            transform.position -= new Vector3(
                moveX * dragSpeed,
                moveY * dragSpeed,
                0f
            );
        }
    }

    // =========================================================
    // 모바일 드래그 이동
    // =========================================================

    void HandleMobileDrag()
    {
        // 터치 1개일 때만 실행
        if (Input.touchCount == 1)
        {
            Touch touch =
                Input.GetTouch(0);

            // 움직이는 중일 때만
            if (touch.phase == TouchPhase.Moved)
            {
                // 터치 이동량
                Vector2 delta =
                    touch.deltaPosition;

                // 카메라 이동
                transform.position -= new Vector3(
                    delta.x * dragSpeed * 0.01f,
                    delta.y * dragSpeed * 0.01f,
                    0f
                );
            }
        }
    }

    // =========================================================
    // 카메라 이동 제한
    // =========================================================

    void ClampCamera()
    {
        Vector3 pos = transform.position;

        // X 제한
        pos.x =
            Mathf.Clamp(
                pos.x,
                minBounds.x,
                maxBounds.x
            );

        // Y 제한
        pos.y =
            Mathf.Clamp(
                pos.y,
                minBounds.y,
                maxBounds.y
            );

        // 최종 적용
        transform.position = new Vector3(
            pos.x,
            pos.y,
            transform.position.z
        );
    }
}