using UnityEngine;

// <변경부분> 모바일 노치/라운드 모서리 영역을 피해서 UI 루트 크기를 Safe Area에 맞추는 스크립트
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    // Safe Area가 적용될 RectTransform
    private RectTransform rectTransform;

    // 마지막으로 적용한 Safe Area 값
    private Rect lastSafeArea;

    // 마지막으로 적용한 화면 크기
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        // 현재 오브젝트의 RectTransform 저장
        rectTransform = GetComponent<RectTransform>();

        // 최초 1회 Safe Area 적용
        ApplySafeArea();
    }

    private void Update()
    {
        // 화면 회전이나 해상도 변경이 생기면 Safe Area를 다시 적용
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height)
        {
            ApplySafeArea();
        }
    }

    // <변경부분> Screen.safeArea 값을 Canvas Anchor 기준으로 변환해서 적용
    private void ApplySafeArea()
    {
        // 현재 기기의 Safe Area
        Rect safeArea = Screen.safeArea;

        // 현재 화면 크기 저장
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        // 현재 Safe Area 저장
        lastSafeArea = safeArea;

        // Safe Area의 최소/최대 좌표를 화면 비율로 변환
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Safe Area를 RectTransform Anchor에 적용
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        // Anchor만으로 크기를 맞추기 때문에 Offset은 0으로 초기화
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
