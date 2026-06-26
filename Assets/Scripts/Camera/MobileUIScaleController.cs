using UnityEngine;

// <변경부분> 모바일 환경에서 전투 UI 전체 배율을 키워 가독성을 보정하는 스크립트
public class MobileUIScaleController : MonoBehaviour
{
    [Header("Scale")]
    // PC 또는 에디터에서 사용할 기본 UI 배율
    [SerializeField] private float defaultScale = 1f;

    // 모바일 기기에서 사용할 UI 배율
    [SerializeField] private float mobileScale = 1.2f;

    [Header("Editor Test")]
    // 에디터에서도 모바일 배율을 강제로 테스트할지 여부
    [SerializeField] private bool forceMobileScaleInEditor = false;

    private void Awake()
    {
        ApplyScale();
    }

    private void OnValidate()
    {
        // 인스펙터 값 변경 시 에디터에서도 바로 크기 확인
        ApplyScale();
    }

    // <변경부분> 실행 환경에 따라 UI 루트 배율 적용
    private void ApplyScale()
    {
        float targetScale = defaultScale;

#if UNITY_EDITOR
        if (forceMobileScaleInEditor)
        {
            targetScale = mobileScale;
        }
#else
        if (Application.isMobilePlatform)
        {
            targetScale = mobileScale;
        }
#endif

        transform.localScale = new Vector3(targetScale, targetScale, 1f);
    }
}
