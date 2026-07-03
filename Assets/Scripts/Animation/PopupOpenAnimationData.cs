using UnityEngine;

// <변경부분> 팝업 UI 오픈 애니메이션 설정값을 데이터화하는 ScriptableObject
[CreateAssetMenu(
    fileName = "PopupOpenAnimationData",
    menuName = "Devorya/UI/Popup Open Animation Data"
)]
public class PopupOpenAnimationData : ScriptableObject
{
    [Header("Time")]
    // <변경부분> 팝업 오픈 애니메이션 전체 시간
    public float duration = 0.16f;

    // <변경부분> Time.timeScale 영향을 받지 않고 UI 애니메이션을 재생할지 여부
    public bool useUnscaledTime = true;

    [Header("Scale")]
    // <변경부분> 시작 스케일
    public Vector2 startScale = new Vector2(0.96f, 1.04f);

    // <변경부분> 종료 스케일
    public Vector2 endScale = Vector2.one;

    // <변경부분> 스케일 변화 곡선
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Alpha Flicker")]
    // <변경부분> 지지직 느낌을 위한 알파 깜빡임 사용 여부
    public bool useAlphaFlicker = true;

    // <변경부분> 시작 알파값
    [Range(0f, 1f)] public float startAlpha = 0.15f;

    // <변경부분> 종료 알파값
    [Range(0f, 1f)] public float endAlpha = 1f;

    // <변경부분> 깜빡임 중 최소 알파값
    [Range(0f, 1f)] public float flickerMinAlpha = 0.25f;

    // <변경부분> 깜빡임 횟수
    public int flickerCount = 7;

    [Header("Position Jitter")]
    // <변경부분> 지지직 느낌을 위한 위치 흔들림 사용 여부
    public bool usePositionJitter = true;

    // <변경부분> 시작 시 최대 흔들림 범위
    public Vector2 jitterRange = new Vector2(14f, 6f);

    [Header("Canvas Group")]
    // <변경부분> 팝업이 Raycast를 막을지 여부
    // Tooltip 팝업은 false 권장
    public bool blocksRaycasts = false;

    // <변경부분> 팝업이 상호작용 가능한 UI인지 여부
    // Tooltip 팝업은 false 권장
    public bool interactable = false;
}
