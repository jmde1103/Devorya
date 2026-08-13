using System.Collections.Generic;
using UnityEngine;

// <변경부분> 공용 TextCutsceneScene에서 사용할
// 텍스트 컷씬 한 편의 설정을 저장하는 ScriptableObject.
//
// Scene 자체는 하나만 유지하고,
// 컷씬마다 이 Data Asset만 교체하여
// 문구 / 타이핑 / 커서 / 종료 처리 / 다음 Scene을 변경한다.
[CreateAssetMenu(
    fileName = "TextCutsceneData",
    menuName = "Devorya/Cutscene/Text Cutscene Data"
)]
public class TextCutsceneData : ScriptableObject
{
    [Header("Cutscene Info")]
    // <변경부분> Inspector에서 구분하기 위한 컷씬 이름.
    public string cutsceneName;

    [TextArea]
    // 제작 메모용 설명.
    public string description;

    [Header("Text Pages")]
    // <변경부분> 순서대로 출력할 텍스트 목록.
    //
    // 문자열 안에서:
    //
    // <wait=1.0>
    //
    // 형식의 대기 태그를 사용할 수 있다.
    [TextArea(3, 15)]
    public List<string> textPages =
        new List<string>();

    [Header("Typing")]
    // 글자 하나가 출력되는 기본 간격.
    [Min(0.001f)]
    public float characterInterval =
        0.04f;

    // 각 Page 출력이 완료된 뒤 유지 시간.
    [Min(0f)]
    public float pageHoldDuration =
        1.2f;

    // Page 사이의 빈 화면 유지 시간.
    [Min(0f)]
    public float betweenPageDelay =
        0.4f;

    // 마지막 Page 출력 후 유지 시간.
    [Min(0f)]
    public float finalHoldDuration =
        1.5f;

    // Time.timeScale과 무관하게 컷씬을 진행할지 여부.
    public bool useUnscaledTime =
        true;

    [Header("Typing Cursor")]
    // 부팅 화면용 타이핑 커서를 사용할지 여부.
    public bool useTypingCursor =
        true;

    // 커서 문자.
    public string cursorCharacter =
        "_";

    // 커서 ON / OFF 간격.
    [Min(0.05f)]
    public float cursorBlinkInterval =
        0.35f;

    [Header("Inline Glitch")]
    // <변경부분> 문자열의 <glitch=초> 명령을 사용할지 설정한다.
    //
    // OFF이면 <glitch=...> 태그가 있어도
    // 중간 글리치 연출을 실행하지 않는다.
    public bool useInlineGlitch =
        false;

    // <변경부분> 글리치 중 텍스트가 순간적으로
    // 좌우 / 상하로 크게 이탈할 수 있는 최대 범위.
    //
    // 이번 고장 연출에서는 Y보다 X를 크게 사용하여
    // 화면 신호가 좌우로 찢어지는 느낌을 만든다.
    public Vector2 glitchPositionJitter =
        new Vector2(35f, 3f);

    // <변경부분> 글리치 중 사용할 X Scale 범위.
    //
    // 예:
    // 0.55 → 글자가 가로로 강하게 압축됨
    // 1.55 → 글자가 가로로 크게 늘어남
    //
    // 각 글리치 프레임마다 이 범위 안에서
    // 랜덤 Scale을 사용하여 좌우로 뒤틀리는 느낌을 만든다.
    public Vector2 glitchScaleXRange =
        new Vector2(0.55f, 1.55f);

    // <변경부분> Y Scale도 약하게 변형하여
    // 완전히 단순한 좌우 이동처럼 보이지 않게 한다.
    //
    // X보다 변화폭을 작게 두는 것을 권장한다.
    public Vector2 glitchScaleYRange =
        new Vector2(0.88f, 1.12f);

    // <변경부분> 글리치 중 텍스트 전체를
    // 약간 비틀 수 있는 최대 Z 회전 각도.
    //
    // 너무 크게 사용하면 UI가 회전하는 느낌이 강해지므로
    // 1~3도 정도의 작은 값을 권장한다.
    [Min(0f)]
    public float glitchRotationRange =
        2f;

    // <변경부분> 글리치가 갱신되는 간격.
    //
    // 0.02 ~ 0.04 정도면
    // 각 프레임이 거의 끊어지듯 전환되어
    // 기계 고장 / 신호 손상 느낌이 강해진다.
    [Min(0.01f)]
    public float glitchFrameInterval =
        0.025f;

    // <변경부분> 평상시 텍스트 색상.
    //
    // 글리치 종료 후 반드시 이 색상으로 복구한다.
    public Color normalTextColor =
     Color.white;

    // <변경부분> 글리치 중 텍스트 색상을
    // 별도로 변경할지 설정한다.
    //
    // OFF이면 글리치 중에도 정상 텍스트 색상을 유지하고
    // 위치 / Scale / 회전 왜곡만 실행한다.
    //
    // 이번 고장 컷씬에서는 OFF를 권장한다.
    public bool useGlitchColorFlicker =
        false;

    // <변경부분> 색상 Flicker를 사용할 경우
    // 섞어 사용할 첫 번째 오류 색상.
    public Color glitchColorA =
        Color.red;

    // <변경부분> 글리치 중 섞어 사용할 두 번째 오류 색상.
    public Color glitchColorB =
        Color.blue;

    // <변경부분> 글리치 순간에
    // 정상 흰색도 랜덤하게 섞을지 설정한다.
    public bool includeNormalColorInGlitch =
        true;

    // <변경부분> 글리치 도중 알파값도 흔들어
    // 화면이 순간적으로 끊기는 느낌을 줄지 설정한다.
    public bool useGlitchAlphaFlicker =
        true;

    // <변경부분> 알파 Flicker 사용 시
    // 순간적으로 낮아질 수 있는 최소 알파값.
    [Range(0f, 1f)]
    public float glitchMinAlpha =
        0.35f;

    [Header("End Animation")]
    // <변경부분> 컷씬 종료 시
    // PopupOpenAnimator에 적용할 Animation Data.
    //
    // None이면 PopupOpenAnimator에
    // 기존 연결된 데이터를 그대로 사용한다.
    public PopupOpenAnimationData endAnimationData;

    [Header("Scene Transition")]
    // 컷씬 종료 후 이동할 Scene.
    public string nextSceneName;

    [Header("Battle Transition")]
    // <변경부분> 다음 Scene이 BattleScene인 경우
    // BattleSetupManager에 전달할 StageBattleData.
    //
    // 일반 Scene 전환이라면 None.
    public StageBattleData nextStageBattleData;

    // <변경부분> nextStageBattleData를 사용하는 경우
    // WorldMapRuntimeState에 함께 등록할 전투 노드 ID.
    //
    // 전투 승리 후 월드맵에서
    // 어느 노드를 클리어해야 하는지 판단하는 데 필요하다.
    public string battleNodeId;

    // <변경부분> 최소 실행 가능한 데이터인지 확인한다.
    public bool IsValid()
    {
        if (textPages == null ||
            textPages.Count == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                nextSceneName))
        {
            return false;
        }

        // StageBattleData를 전달하는 전투 컷씬이라면
        // Battle Node ID도 같이 있어야
        // 기존 WorldMapRuntimeState 흐름을 정상 사용할 수 있다.
        if (nextStageBattleData != null &&
            string.IsNullOrWhiteSpace(
                battleNodeId))
        {
            return false;
        }

        return true;
    }
}