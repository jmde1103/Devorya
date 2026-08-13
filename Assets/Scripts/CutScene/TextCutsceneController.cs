using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// <변경부분> 검은 화면 텍스트 컷씬 전용 컨트롤러.
//
// 등록된 문장을 순서대로 한 글자씩 출력하고,
// 모든 문장의 출력이 끝나면 지정한 다음 Scene으로 자동 이동한다.
//
// 기존 EventGuideUI와 동일하게
// TMP maxVisibleCharacters 방식으로 타이핑 연출을 처리하지만,
// 전투 Dialogue처럼 클릭을 기다리지 않고 자동으로 진행한다.
public class TextCutsceneController : MonoBehaviour
{
    [Header("Cutscene Data")]
    // <변경부분> 현재 이 Scene에서 실행할 컷씬 데이터.
    //
    // TextCutsceneScene 자체는 공용으로 하나만 사용하고,
    // 실제 출력 문구 / 타이핑 / 커서 / 다음 Scene 설정은
    // TextCutsceneData를 교체하여 재사용한다.
    [SerializeField]
    private TextCutsceneData cutsceneData;

    [Header("Scene References")]
    // 실제 컷씬 문장을 표시할 TMP Text.
    [SerializeField]
    private TMP_Text cutsceneText;

    // <변경부분> 문자열 중간의 <glitch=초> 명령이 실행될 때
    // 실제 위치를 흔들 RectTransform.
    //
    // CutsceneText 자체보다
    // CutsceneText를 감싸는 CutsceneTextRoot를 연결하는 것을 권장한다.
    [SerializeField]
    private RectTransform inlineGlitchTargetRect;

    [Header("End Glitch Animation")]
    // <변경부분> 컷씬 종료 연출을 실행할
    // Scene의 PopupOpenAnimator 참조.
    //
    // 실제 Animation Data는
    // TextCutsceneData에서 가져온다.
    [SerializeField]
    private PopupOpenAnimator endPopupAnimator;

    // <변경부분> Scene 시작 시
    // 컷씬이 중복 실행되는 것을 방지한다.
    private bool isPlaying =
        false;

    // <변경부분> 현재까지 실제로 타이핑된 문자열.
    // 화면 출력 시 이 문자열 뒤에 깜빡이는 "_" 커서를 붙인다.
    private string currentVisibleText =
        string.Empty;

    // <변경부분> 현재 타이핑 커서 표시 여부.
    private bool isCursorVisible =
        true;

    // <변경부분> 독립적으로 커서를 깜빡이는 코루틴.
    private Coroutine cursorBlinkCoroutine =
     null;

    // <변경부분> Inline Glitch가 끝난 뒤
    // 텍스트 Root를 정확한 원래 위치로 복구하기 위한 기준 위치.
    private Vector2 inlineGlitchBasePosition =
    Vector2.zero;

    // <변경부분> 글리치 전 정상적인 Local Scale.
    //
    // 글리치 중 X/Y Scale을 강하게 변경한 뒤
    // 반드시 이 값으로 되돌린다.
    private Vector3 inlineGlitchBaseScale =
        Vector3.one;

    // <변경부분> 글리치 전 정상적인 Local Rotation.
    //
    // 고장 연출 중 약한 회전을 적용한 뒤
    // 정확한 원래 회전값으로 복구한다.
    private Quaternion inlineGlitchBaseRotation =
        Quaternion.identity;

    // <변경부분> Inline Glitch의
    // 위치 / Scale / Rotation 기준값을 저장했는지 확인한다.
    private bool hasCapturedInlineGlitchBaseTransform =
        false;

    private void Awake()
    {
        // <변경부분> 다른 Scene에서 전달받은 컷씬 데이터가 있다면
        // Inspector 기본 Cutscene Data보다 우선 적용한다.
        //
        // 전달 데이터가 없다면
        // Inspector에 연결된 기본값(Prologue_Boot)을 그대로 사용한다.
        ResolveCutsceneData();

        currentVisibleText =
            string.Empty;

        isCursorVisible =
            true;

        if (cutsceneText != null)
        {
            cutsceneText.text =
                string.Empty;
        }

        // Inline Glitch 종료 후
        // 위치뿐 아니라 Scale / Rotation까지 정확히 복구할 수 있도록
        // 정상 Transform 상태를 Scene 시작 시 저장한다.
        if (inlineGlitchTargetRect != null)
        {
            inlineGlitchBasePosition =
                inlineGlitchTargetRect.anchoredPosition;

            inlineGlitchBaseScale =
                inlineGlitchTargetRect.localScale;

            inlineGlitchBaseRotation =
                inlineGlitchTargetRect.localRotation;

            hasCapturedInlineGlitchBaseTransform =
                true;
        }
    }

    // <변경부분> TextCutsceneScene에 진입할 때
    // 외부에서 전달된 CutsceneData가 있는지 확인한다.
    //
    // Pending Data가 있으면:
    // 전달된 Data를 이번 컷씬에 사용.
    //
    // Pending Data가 없으면:
    // Inspector에 연결된 기본 CutsceneData를 그대로 사용.
    //
    // Pending Data는 Consume 방식으로 즉시 제거하므로
    // 같은 컷씬이 다음 진입 때 다시 실행되지 않는다.
    private void ResolveCutsceneData()
    {
        TextCutsceneData pendingCutsceneData =
            TextCutsceneRuntimeState
                .ConsumePendingCutsceneData();

        if (pendingCutsceneData == null)
        {
            // <변경부분> 별도 전달 데이터가 없으므로
            // Inspector 기본값을 그대로 유지한다.
            if (cutsceneData != null)
            {
                Debug.Log(
                    $"텍스트 컷씬 기본 데이터 사용: " +
                    $"{cutsceneData.name}"
                );
            }

            return;
        }

        // <변경부분> 외부에서 전달받은 Data가 있다면
        // Inspector 기본값 대신 이번 실행에만 해당 Data를 사용한다.
        cutsceneData =
            pendingCutsceneData;

        Debug.Log(
            $"텍스트 컷씬 전달 데이터 적용: " +
            $"{cutsceneData.name}"
        );
    }

    private void Start()
    {
        // <변경부분> Scene이 시작되면
        // 최종 선택된 컷씬 데이터를 자동 실행한다.
        StartCutscene();
    }

    // <변경부분> 컷씬을 처음부터 시작하는 공개 진입점.
    public void StartCutscene()
    {
        if (isPlaying)
        {
            return;
        }

        if (cutsceneText == null)
        {
            Debug.LogWarning(
                "텍스트 컷씬 시작 실패: " +
                "Cutscene Text가 연결되지 않았습니다."
            );

            return;
        }

        // <변경부분> Scene Inspector의 개별 값이 아니라
        // 현재 연결된 TextCutsceneData 전체를 검사한다.
        if (cutsceneData == null)
        {
            Debug.LogWarning(
                "텍스트 컷씬 시작 실패: " +
                "TextCutsceneData가 연결되지 않았습니다."
            );

            return;
        }

        if (cutsceneData.IsValid() == false)
        {
            Debug.LogWarning(
                $"텍스트 컷씬 시작 실패: " +
                $"{cutsceneData.name} 데이터가 유효하지 않습니다."
            );

            return;
        }

        // <변경부분> 현재 컷씬 Data에 설정된
        // 정상 텍스트 색상을 컷씬 시작 상태로 적용한다.
        cutsceneText.color =
            cutsceneData.normalTextColor;

        //
        // 기존에는 isPlaying이 false인 상태에서
        // CursorBlinkRoutine()이 시작되어
        // while (isPlaying)을 통과하지 못하고 즉시 종료되고 있었다.
        isPlaying =
            true;

        // 컷씬 시작과 함께
        // 부팅 화면용 "_" 커서 깜빡임을 시작한다.
        StartTypingCursorBlink();

        StartCoroutine(
            PlayCutsceneRoutine()
        );
    }

    // <변경부분> 모든 문장을 순서대로 출력하고
    // 마지막 문장까지 끝난 뒤 다음 Scene으로 이동한다.
    private IEnumerator PlayCutsceneRoutine()
    {
        // <변경부분> 현재 컷씬 Data에 등록된
        // Text Pages를 순서대로 실행한다.
        for (int i = 0;
             i < cutsceneData.textPages.Count;
             i++)
        {
            string pageText =
                cutsceneData.textPages[i];

            if (pageText == null)
            {
                pageText =
                    string.Empty;
            }

            // <변경부분> 현재 문장을
            // 처음부터 끝까지 타이핑한다.
            yield return
                PlayTypingRoutine(
                    pageText
                );

            bool isLastPage =
     i ==
     cutsceneData.textPages.Count - 1;

            if (isLastPage)
            {
                // <변경부분> 마지막 문장은 별도의 유지 시간을 사용한다.
                yield return
                    WaitRoutine(
    cutsceneData.finalHoldDuration
);

                break;
            }

            // <변경부분> 일반 문장은 완전히 출력된 상태로
            // 지정된 시간만큼 화면에 유지한다.
            yield return
                WaitRoutine(
    cutsceneData.pageHoldDuration
);

            // <변경부분> 다음 문장이 나오기 전에
            // 실제 타이핑된 문자열만 비운다.
            //
            // 커서는 별도 코루틴에서 계속 깜빡이므로
            // 문장이 없는 동안에는 검은 화면에 "_"만 깜빡이게 된다.
            currentVisibleText =
                string.Empty;

            RefreshCutsceneText();

            // <변경부분> 문장 사이에 짧은 검은 화면 간격을 둔다.
            yield return
                WaitRoutine(
    cutsceneData.betweenPageDelay
);
        }

        // <변경부분> 모든 타이핑과 마지막 유지 시간이 끝난 뒤
        // 전체 텍스트에 PopupOpenAnimator 기반
        // 글리치 / 노이즈 소멸 애니메이션을 실행한다.
        //
        // 애니메이션이 완전히 끝난 뒤에만
        // 다음 Scene으로 이동한다.
        yield return
            PlayEndGlitchRoutine();

        // 지정된 다음 Scene으로 이동한다.
        LoadNextScene();
    }

    // <변경부분> 문자열을 실제로 한 글자씩 출력한다.
    //
    // 문자열 안에:
    //
    // <wait=1.0>
    //
    // 형식의 제어 태그가 있으면
    // 해당 태그는 화면에 출력하지 않고
    // 지정한 시간만큼 그 위치에서 타이핑을 멈춘다.
    //
    // 대기 중에도 별도 Cursor 코루틴은 계속 실행되므로
    // 현재 입력 위치의 "_"는 계속 깜빡인다.
    private IEnumerator PlayTypingRoutine(
        string pageText)
    {
        currentVisibleText =
            string.Empty;

        RefreshCutsceneText();

        if (string.IsNullOrEmpty(
                pageText))
        {
            yield break;
        }

        // <변경부분> 타이핑 속도는
        // 현재 TextCutsceneData 설정을 사용한다.
        float safeInterval =
            Mathf.Max(
                0.001f,
                cutsceneData.characterInterval
            );

        int textIndex =
            0;

        while (textIndex <
       pageText.Length)
        {
            // <변경부분> 현재 위치에 <wait=초> 태그가 있다면
            // 화면에는 출력하지 않고 지정 시간만 대기한 뒤
            // 태그 다음 문자부터 타이핑을 이어간다.
            if (TryReadWaitTag(
                    pageText,
                    textIndex,
                    out float waitDuration,
                    out int waitNextTextIndex))
            {
                yield return
                    WaitRoutine(
                        waitDuration
                    );

                textIndex =
                    waitNextTextIndex;

                continue;
            }

            // <변경부분> 현재 위치에 <glitch=초> 태그가 있다면
            // 태그 자체는 화면에 출력하지 않고
            // 현재까지 출력된 전체 텍스트에 오류 연출을 실행한다.
            if (TryReadGlitchTag(
                    pageText,
                    textIndex,
                    out float glitchDuration,
                    out int glitchNextTextIndex))
            {
                if (cutsceneData.useInlineGlitch)
                {
                    yield return
                        PlayInlineGlitchRoutine(
                            glitchDuration
                        );
                }

                textIndex =
                    glitchNextTextIndex;

                continue;
            }

            // 일반 문자는 기존처럼 한 글자씩 출력한다.
            currentVisibleText +=
                pageText[textIndex];

            RefreshCutsceneText();

            textIndex++;

            yield return
                WaitRoutine(
                    safeInterval
                );
        }

        // 모든 글자가 출력된 뒤에도
        // 마지막 입력 위치에서 커서를 계속 표시한다.
        RefreshCutsceneText();
    }

    // <변경부분> 문자열의 현재 위치가
    // <wait=초> 제어 태그인지 검사한다.
    //
    // 지원 예:
    //
    // <wait=1>
    // <wait=1.0>
    // <wait=0.35>
    //
    // 올바른 태그:
    // true 반환
    // waitDuration에 대기 시간 저장
    // nextTextIndex에 태그 다음 문자 위치 저장
    //
    // 잘못 작성된 태그:
    // false 반환
    // 일반 문자열처럼 그대로 타이핑된다.
    private bool TryReadWaitTag(
        string sourceText,
        int startIndex,
        out float waitDuration,
        out int nextTextIndex)
    {
        waitDuration =
            0f;

        nextTextIndex =
            startIndex;

        if (string.IsNullOrEmpty(
                sourceText))
        {
            return false;
        }

        if (startIndex < 0 ||
            startIndex >=
                sourceText.Length)
        {
            return false;
        }

        const string waitPrefix =
            "<wait=";

        // 현재 위치에 "<wait="가 정확히 시작되는지 검사한다.
        if (startIndex +
                waitPrefix.Length >
            sourceText.Length)
        {
            return false;
        }

        bool hasWaitPrefix =
            string.Compare(
                sourceText,
                startIndex,
                waitPrefix,
                0,
                waitPrefix.Length,
                StringComparison.OrdinalIgnoreCase
            ) == 0;

        if (hasWaitPrefix == false)
        {
            return false;
        }

        // 닫는 ">" 위치를 찾는다.
        int closeIndex =
            sourceText.IndexOf(
                '>',
                startIndex +
                waitPrefix.Length
            );

        if (closeIndex < 0)
        {
            return false;
        }

        int valueStartIndex =
            startIndex +
            waitPrefix.Length;

        int valueLength =
            closeIndex -
            valueStartIndex;

        if (valueLength <= 0)
        {
            return false;
        }

        string waitValueText =
            sourceText
                .Substring(
                    valueStartIndex,
                    valueLength
                )
                .Trim();

        // <변경부분> Unity Editor의 PC 언어 설정과 관계없이
        // 소수점 "." 형식으로 안정적으로 시간을 읽는다.
        bool parsed =
            float.TryParse(
                waitValueText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out waitDuration
            );

        if (parsed == false)
        {
            return false;
        }

        waitDuration =
            Mathf.Max(
                0f,
                waitDuration
            );

        nextTextIndex =
            closeIndex + 1;

        return true;
    }

    // <변경부분> 문자열 현재 위치의
    // <glitch=초> 제어 태그를 읽는다.
    //
    // 지원 예:
    //
    // <glitch=0.15>
    // <glitch=0.35>
    // <glitch=1.0>
    //
    // 태그 자체는 화면에 출력되지 않으며,
    // 값은 글리치가 유지될 시간을 의미한다.
    private bool TryReadGlitchTag(
        string sourceText,
        int startIndex,
        out float glitchDuration,
        out int nextTextIndex)
    {
        glitchDuration =
            0f;

        nextTextIndex =
            startIndex;

        if (string.IsNullOrEmpty(
                sourceText))
        {
            return false;
        }

        if (startIndex < 0 ||
            startIndex >=
                sourceText.Length)
        {
            return false;
        }

        const string glitchPrefix =
            "<glitch=";

        if (startIndex +
                glitchPrefix.Length >
            sourceText.Length)
        {
            return false;
        }

        bool hasGlitchPrefix =
            string.Compare(
                sourceText,
                startIndex,
                glitchPrefix,
                0,
                glitchPrefix.Length,
                StringComparison.OrdinalIgnoreCase
            ) == 0;

        if (hasGlitchPrefix == false)
        {
            return false;
        }

        int closeIndex =
            sourceText.IndexOf(
                '>',
                startIndex +
                glitchPrefix.Length
            );

        if (closeIndex < 0)
        {
            return false;
        }

        int valueStartIndex =
            startIndex +
            glitchPrefix.Length;

        int valueLength =
            closeIndex -
            valueStartIndex;

        if (valueLength <= 0)
        {
            return false;
        }

        string glitchValueText =
            sourceText
                .Substring(
                    valueStartIndex,
                    valueLength
                )
                .Trim();

        bool parsed =
            float.TryParse(
                glitchValueText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out glitchDuration
            );

        if (parsed == false)
        {
            return false;
        }

        glitchDuration =
            Mathf.Max(
                0f,
                glitchDuration
            );

        nextTextIndex =
            closeIndex + 1;

        return true;
    }

    // <변경부분> 현재까지 출력된 텍스트 전체에
    // 고장난 시스템처럼 보이는 짧은 글리치 연출을 적용한다.
    //
    // 연출:
    // - 위치 랜덤 Jitter
    // - 정상색 / 빨강 / 파랑 빠른 전환
    // - 선택적으로 Alpha Flicker
    //
    // 지정 시간이 끝나면
    // 위치 / 색상 / Alpha를 반드시 정상 상태로 복구한다.
    private IEnumerator PlayInlineGlitchRoutine(
        float duration)
    {
        if (cutsceneData == null ||
            cutsceneText == null)
        {
            yield break;
        }

        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        if (safeDuration <= 0f)
        {
            yield break;
        }

        // <변경부분> Inspector 연결이 빠진 경우에도
        // CutsceneText 자신의 RectTransform을 최소 대체 대상으로 사용한다.
        RectTransform glitchRect =
            inlineGlitchTargetRect;

        if (glitchRect == null)
        {
            glitchRect =
                cutsceneText.rectTransform;
        }

        // <변경부분> Inspector 연결 상태 때문에 Awake에서
        // 기준 Transform을 저장하지 못했다면
        // 실제 글리치 실행 직전에 현재 정상 상태를 저장한다.
        if (hasCapturedInlineGlitchBaseTransform ==
            false)
        {
            inlineGlitchBasePosition =
                glitchRect.anchoredPosition;

            inlineGlitchBaseScale =
                glitchRect.localScale;

            inlineGlitchBaseRotation =
                glitchRect.localRotation;

            hasCapturedInlineGlitchBaseTransform =
                true;
        }

        float safeFrameInterval =
            Mathf.Max(
                0.01f,
                cutsceneData.glitchFrameInterval
            );

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            // <변경부분> 글리치 프레임마다
            // 텍스트 전체를 좌우 중심으로 크게 튕긴다.
            //
            // 작은 흔들림이 아니라
            // 영상 신호가 순간적으로 옆으로 밀려나는 느낌을 만든다.
            Vector2 jitterOffset =
                new Vector2(
                    UnityEngine.Random.Range(
                        -cutsceneData
                            .glitchPositionJitter.x,
                        cutsceneData
                            .glitchPositionJitter.x
                    ),
                    UnityEngine.Random.Range(
                        -cutsceneData
                            .glitchPositionJitter.y,
                        cutsceneData
                            .glitchPositionJitter.y
                    )
                );

            glitchRect.anchoredPosition =
                inlineGlitchBasePosition +
                jitterOffset;

            // <변경부분> 위치 이동과 동시에
            // X Scale을 강하게 압축하거나 늘려
            // 글자들이 좌우 방향으로 찢어지고 뒤틀리는 느낌을 만든다.
            float randomScaleX =
                UnityEngine.Random.Range(
                    cutsceneData.glitchScaleXRange.x,
                    cutsceneData.glitchScaleXRange.y
                );

            float randomScaleY =
                UnityEngine.Random.Range(
                    cutsceneData.glitchScaleYRange.x,
                    cutsceneData.glitchScaleYRange.y
                );

            glitchRect.localScale =
                new Vector3(
                    inlineGlitchBaseScale.x *
                    randomScaleX,

                    inlineGlitchBaseScale.y *
                    randomScaleY,

                    inlineGlitchBaseScale.z
                );

            // <변경부분> 화면 전체가 돌아가는 느낌이 되지 않는 범위에서
            // 약한 회전 노이즈도 섞어 기계적으로 고장난 왜곡감을 추가한다.
            float randomRotationZ =
                UnityEngine.Random.Range(
                    -cutsceneData.glitchRotationRange,
                    cutsceneData.glitchRotationRange
                );

            glitchRect.localRotation =
                inlineGlitchBaseRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    randomRotationZ
                );

            // <변경부분> 색상 노이즈를 사용하는 컷씬에서만
            // 빨강 / 파랑 / 정상색 Flicker를 실행한다.
            //
            // OFF라면 정상 텍스트 색상을 계속 유지하고,
            // 위치 / Scale / 회전 왜곡만으로 글리치를 표현한다.
            if (cutsceneData.useGlitchColorFlicker)
            {
                int colorVariantCount =
                    cutsceneData
                        .includeNormalColorInGlitch
                        ? 3
                        : 2;

                int colorIndex =
                    UnityEngine.Random.Range(
                        0,
                        colorVariantCount
                    );

                Color glitchColor;

                if (colorIndex == 0)
                {
                    glitchColor =
                        cutsceneData.glitchColorA;
                }
                else if (colorIndex == 1)
                {
                    glitchColor =
                        cutsceneData.glitchColorB;
                }
                else
                {
                    glitchColor =
                        cutsceneData.normalTextColor;
                }

                if (cutsceneData.useGlitchAlphaFlicker)
                {
                    glitchColor.a =
                        UnityEngine.Random.Range(
                            cutsceneData.glitchMinAlpha,
                            1f
                        );
                }
                else
                {
                    glitchColor.a =
                        cutsceneData.normalTextColor.a;
                }

                cutsceneText.color =
                    glitchColor;
            }
            else
            {
                // <변경부분> 색상 노이즈를 사용하지 않는 경우
                // 글자색은 항상 정상 상태를 유지한다.
                cutsceneText.color =
                    cutsceneData.normalTextColor;
            }

            yield return
                WaitRoutine(
                    safeFrameInterval
                );

            elapsedTime +=
                safeFrameInterval;
        }

        // <변경부분> 글리치가 끝나는 순간
        // 위치 / Scale / Rotation / Color를
        // 글리치 이전의 정확한 정상 상태로 모두 복구한다.
        glitchRect.anchoredPosition =
            inlineGlitchBasePosition;

        glitchRect.localScale =
            inlineGlitchBaseScale;

        glitchRect.localRotation =
            inlineGlitchBaseRotation;

        cutsceneText.color =
            cutsceneData.normalTextColor;

        RefreshCutsceneText();
    }

    // <변경부분> 현재까지 타이핑된 문자열과
    // "_" 커서 상태를 합쳐 TMP Text에 표시한다.
    private void RefreshCutsceneText()
    {
        if (cutsceneText == null)
        {
            return;
        }

        // <변경부분> 커서 사용 여부와 문자는
        // 현재 TextCutsceneData 설정을 사용한다.
        string cursorText =
            cutsceneData != null &&
            cutsceneData.useTypingCursor &&
            isCursorVisible
                ? cutsceneData.cursorCharacter
                : string.Empty;

        cutsceneText.text =
            currentVisibleText +
            cursorText;
    }

    // <변경부분> 컷씬 전체에서 사용할
    // 타이핑 커서 깜빡임을 시작한다.
    private void StartTypingCursorBlink()
    {
        // <변경부분> 현재 Data에서
        // 타이핑 커서를 사용하지 않는 컷씬이면
        // 커서 코루틴을 시작하지 않는다.
        if (cutsceneData == null ||
            cutsceneData.useTypingCursor == false)
        {
            return;
        }

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(
                cursorBlinkCoroutine
            );
        }

        isCursorVisible =
            true;

        RefreshCutsceneText();

        cursorBlinkCoroutine =
            StartCoroutine(
                CursorBlinkRoutine()
            );
    }

    // <변경부분> 현재 타이핑 위치의 "_" 커서를
    // 일정 간격으로 표시 / 숨김 전환한다.
    //
    // 타이핑 중뿐 아니라
    // 문장 완료 대기와 문장 사이의 검은 화면에서도
    // 계속 깜빡이므로 컴퓨터 부팅 화면처럼 보인다.
    private IEnumerator CursorBlinkRoutine()
    {
        // <변경부분> 커서 깜빡임 속도도
        // 현재 TextCutsceneData를 사용한다.
        float safeBlinkInterval =
            Mathf.Max(
                0.05f,
                cutsceneData.cursorBlinkInterval
            );

        while (isPlaying)
        {
            yield return
                WaitRoutine(
                    safeBlinkInterval
                );

            isCursorVisible =
                !isCursorVisible;

            RefreshCutsceneText();
        }

        cursorBlinkCoroutine =
            null;
    }

    // <변경부분> 현재 실행 중인 커서 깜빡임을 종료한다.
    //
    // hideCursor가 true이면 마지막 "_"도 화면에서 제거한다.
    // 종료 글리치 시작 직전에 사용하여
    // PopupOpenAnimator가 움직이는 동안 TMP 문자열이 다시 갱신되지 않게 한다.
    private void StopTypingCursorBlink(
        bool hideCursor)
    {
        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(
                cursorBlinkCoroutine
            );

            cursorBlinkCoroutine =
                null;
        }

        if (hideCursor)
        {
            isCursorVisible =
                false;

            RefreshCutsceneText();
        }
    }

    // <변경부분> scaled / unscaled time 설정을
    // 모든 컷씬 대기 구간에서 공통으로 사용한다.
    private IEnumerator WaitRoutine(
        float duration)
    {
        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        if (safeDuration <= 0f)
        {
            yield break;
        }

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            // <변경부분> 컷씬마다
            // scaled / unscaled time 사용 여부를 Data에서 결정한다.
            elapsedTime +=
                cutsceneData != null &&
                cutsceneData.useUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            yield return null;
        }
    }

    // <변경부분> 마지막 텍스트 출력 후
    // PopupOpenAnimator를 사용해 전체 텍스트를
    // 노이즈 / 글리치 상태로 소멸시킨다.
    //
    // PopupOpenAnimator.IsPlaying을 기다리므로
    // 애니메이션 도중 Scene이 먼저 전환되지 않는다.
    private IEnumerator PlayEndGlitchRoutine()
    {
        // <변경부분> 글리치가 시작된 뒤
        // CursorBlinkRoutine이 TMP 문자열을 다시 갱신하지 않도록
        // 먼저 커서를 완전히 정지하고 숨긴다.
        StopTypingCursorBlink(
            true
        );

        if (endPopupAnimator == null)
        {
            Debug.LogWarning(
                "텍스트 컷씬 종료 글리치 생략: " +
                "End Popup Animator가 연결되지 않았습니다."
            );

            yield break;
        }

        // <변경부분> 현재 컷씬 Data에
        // 종료용 PopupOpenAnimationData가 연결되어 있다면
        // 그 데이터를 사용한다.
        if (cutsceneData != null &&
            cutsceneData.endAnimationData != null)
        {
            endPopupAnimator.SetAnimationData(
                cutsceneData.endAnimationData
            );
        }

        // 기존 PopupOpenAnimator의
        // Scale / Alpha Flicker / Position Jitter 기능을 그대로 실행한다.
        endPopupAnimator.PlayOpen();

        // <변경부분> PopupOpenAnimator의 코루틴이
        // 완전히 끝날 때까지 Scene 이동을 보류한다.
        while (endPopupAnimator.IsPlaying)
        {
            yield return null;
        }

        Debug.Log(
            "텍스트 컷씬 종료 글리치 완료"
        );
    }

    // <변경부분> 현재 TextCutsceneData의 종료 설정에 따라
    // StageBattleData를 전달한 뒤 다음 Scene으로 이동한다.
    private void LoadNextScene()
    {
        isPlaying =
            false;

        StopTypingCursorBlink(
            true
        );

        if (cutsceneData == null)
        {
            Debug.LogWarning(
                "텍스트 컷씬 종료 실패: " +
                "TextCutsceneData가 없습니다."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
                cutsceneData.nextSceneName))
        {
            Debug.LogWarning(
                "텍스트 컷씬 종료: " +
                "Next Scene Name이 비어 있어 Scene을 이동하지 않습니다."
            );

            return;
        }

        // <변경부분> 다음 Scene이 전투 Scene이고
        // StageBattleData가 지정되어 있다면
        // 기존 월드맵 전투 진입과 동일하게 BeginBattleNode()를 사용한다.
        //
        // StageBattleData뿐 아니라 enteredBattleNodeId도 같이 저장되므로
        // 전투 승리 후 월드맵 노드 클리어 흐름까지 유지된다.
        if (cutsceneData.nextStageBattleData != null)
        {
            if (string.IsNullOrWhiteSpace(
                    cutsceneData.battleNodeId))
            {
                Debug.LogWarning(
                    "텍스트 컷씬 전투 이동 실패: " +
                    "StageBattleData는 있지만 Battle Node ID가 없습니다."
                );

                return;
            }

            WorldMapRuntimeState.BeginBattleNode(
                cutsceneData.battleNodeId,
                cutsceneData.nextStageBattleData
            );

            Debug.Log(
                $"텍스트 컷씬 전투 데이터 전달: " +
                $"{cutsceneData.battleNodeId} / " +
                $"{cutsceneData.nextStageBattleData.name}"
            );
        }

        Debug.Log(
            $"텍스트 컷씬 완료: " +
            $"{cutsceneData.nextSceneName} Scene으로 이동합니다."
        );

        SceneManager.LoadScene(
            cutsceneData.nextSceneName
        );
    }
}
