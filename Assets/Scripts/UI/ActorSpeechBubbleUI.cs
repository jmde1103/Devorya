using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분>
// Battle Piece와 Standalone EventSceneActor가
// 공통으로 사용할 수 있는 월드 말풍선 UI.
//
// 대상 Transform을 실시간 추적하지 않는다.
//
// 이 컴포넌트가 Piece / Event Actor의 자식으로 존재하므로
// 부모 이동, WorldRoot 확대/축소, Camera 이동을
// 별도의 위치 계산 없이 그대로 따라간다.
//
// 담당 기능:
// - Text 표시
// - Text 길이에 따른 말풍선 크기 갱신
// - 최대 가로 길이 이후 자동 줄바꿈
// - 일정 시간 표시
// - 즉시 표시 / 숨김
public class ActorSpeechBubbleUI : MonoBehaviour
{
    [Header("Display")]

    // <변경부분>
    // 말풍선 전체 World Space Canvas Root.
    //
    // 이 스크립트를 SpeechBubbleCanvas 자체에 붙이는 경우
    // 비어 있어도 현재 GameObject를 사용한다.
    [SerializeField]
    private GameObject speechBubbleCanvas;

    // <변경부분>
    // Image / ContentSizeFitter / LayoutGroup이 붙어 있는
    // 실제 말풍선 배경 RectTransform.
    [SerializeField]
    private RectTransform bubbleRoot;

    // 말풍선 안에 표시할 TMP Text.
    [SerializeField]
    private TMP_Text bubbleText;

    // <변경부분>
    // TMP Text의 가로/세로 Preferred Size를
    // Layout System에 전달하기 위한 LayoutElement.
    [SerializeField]
    private LayoutElement textLayoutElement;

    // <변경부분>
    // SpeechBubble 전체의 Fade In / Fade Out을 처리한다.
    //
    // SpeechBubbleCanvas에 CanvasGroup을 추가하고 연결한다.
    // Inspector 연결이 없으면 같은 GameObject에서 자동 탐색한다.
    [SerializeField]
    private CanvasGroup bubbleCanvasGroup;


    [Header("Open / Close Animation")]

    // <변경부분>
    // 처음 등장하면서 Alpha 0 -> 1,
    // 작은 크기 -> 순간 확대까지 진행하는 시간.
    [SerializeField, Min(0f)]
    private float fadeInDuration =
        0.08f;

    // 처음 등장할 때의 기본 Scale 배율.
    [SerializeField, Range(0.1f, 1f)]
    private float entryStartScale =
        0.9f;

    // <변경부분>
    // 말풍선이 처음 나타날 때 순간적으로 커지는 배율.
    //
    // 실제 BubbleRoot Scale 자체를 덮어쓰지 않고
    // Inspector 기본 Scale에 이 값을 곱해서 사용한다.
    [SerializeField, Range(1f, 1.5f)]
    private float entryOvershootScale =
        1.12f;

    // 순간 확대 후 원래 크기로 돌아오는 시간.
    [SerializeField, Min(0f)]
    private float entrySettleDuration =
        0.10f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration =
    0.12f;


    [Header("Idle Float Animation")]

    // <변경부분>
    // 말풍선이 표시되어 있는 동안
    // 천천히 위아래로 떠다니는 Idle 연출 사용 여부.
    //
    // Event Step마다 설정하지 않고
    // SpeechBubble 공통 연출값으로 사용한다.
    [SerializeField]
    private bool useIdleFloat =
        true;

    // <변경부분>
    // 기본 위치를 기준으로 위아래로 움직이는 거리.
    //
    // World Space Canvas 내부 UI 단위 기준이다.
    [SerializeField, Min(0f)]
    private float idleFloatAmplitude =
        0.025f;

    // <변경부분>
    // 아래 → 위 → 아래 한 사이클에 걸리는 시간.
    //
    // 값이 클수록 더 천천히 움직인다.
    [SerializeField, Min(0.1f)]
    private float idleFloatCycleDuration =
        2.2f;


    [Header("Text Size")]

    // 아주 짧은 문구에서도 말풍선이 지나치게 작아지지 않도록 한다.
    [SerializeField, Min(1f)]
    private float minTextWidth =
        24f;

    // <변경부분>
    // 이 너비까지는 말풍선이 가로로 늘어난다.
    //
    // 이 값을 넘는 문장은 자동 줄바꿈되어
    // 말풍선의 세로 크기가 증가한다.
    [SerializeField, Min(1f)]
    private float maxTextWidth =
        220f;

    // 한 줄짜리 짧은 문구의 최소 Text 높이.
    // <변경부분>
    // TMP 실제 Preferred Height를 그대로 사용하도록
    // 기본 최소 높이를 0으로 둔다.
    //
    // 기존 24에서는 한 줄 Text 아래에
    // 불필요한 빈 공간이 생길 수 있었다.
    [SerializeField, Min(0f)]
    private float minTextHeight =
        0f;


    // =====================================================
    // Runtime
    // =====================================================

    // <변경부분>
    // 동일 Actor에게 새 말풍선이 요청되었을 때
    // 이전 Routine이 새 말풍선을 나중에 숨기는 것을 방지한다.
    private int displayVersion =
        0;

    // Wait For Complete = false 방식의
    // 독립 표시용 Coroutine.
    private Coroutine detachedCoroutine;

    // <변경부분>
    // 강조 효과 Text Shake Coroutine.
    private Coroutine textShakeCoroutine;

    // <변경부분>
    // 말풍선 전체가 천천히 위아래로 움직이는
    // Idle Float Coroutine.
    private Coroutine idleFloatCoroutine;

    // <변경부분>
    // BubbleRoot의 실제 Inspector 기본 Scale.
    private Vector3 bubbleBaseLocalScale =
        Vector3.one;

    // <변경부분>
    // Idle Float이 종료된 뒤 정확히 돌아올
    // BubbleRoot의 Inspector 기본 위치.
    private Vector2 bubbleBaseAnchoredPosition =
        Vector2.zero;

    // Text Shake 종료 후 돌아올 원래 위치.
    private Vector2 bubbleTextBaseAnchoredPosition =
        Vector2.zero;

    // 기본 Scale을 이미 저장했는지 확인.
    private bool isBaseVisualStateCached =
    false;


    // <변경부분>
    // 현재 SpeechBubble에서 타이핑 중인 전체 원문.
    //
    // Text 자체에는 전체 문장을 유지하고
    // maxVisibleCharacters로 표시만 제한한다.
    //
    // Layout 계산만 현재 표시된 글자 수에 맞춰 다시 계산하여
    // 타이핑할수록 말풍선이 자연스럽게 커지게 한다.
    private string currentDisplayText =
        string.Empty;


    private void Awake()
    {
        // SpeechBubbleCanvas는 기본 비활성화 상태로 사용할 수 있다.
        // Awake에서는 표시 상태를 변경하지 않고 Reference만 준비한다.
        AutoBindReferences();

        // <변경부분>
        // Prefab Inspector에 설정된 실제 BubbleRoot Scale을 저장한다.
        CacheBaseVisualState();
    }


    // =====================================================
    // Public
    // =====================================================

    // <변경부분>
    // Fade In + Pop + Typing + Emphasis + Fade Out을 포함한
    // Event SpeechBubble 전체 연출.
    //
    // duration은 Text 타이핑이 끝난 뒤
    // 완성된 말풍선을 유지하는 시간으로 사용한다.
    public IEnumerator PlayRoutine(
        string text,
        float duration,
        float typingSpeed,
        bool useEmphasisShake,
        float emphasisStrength)
    {
        StopDetachedCoroutine();
        StopTextShakeCoroutine();

        // <변경부분>
        // 이전 말풍선의 Idle Float이 남아 있다면
        // 기본 위치로 복원한 뒤 새 표시를 시작한다.
        StopIdleFloatCoroutine();

        int currentVersion =
            ++displayVersion;

        bool prepared =
            PrepareDisplay(
                text,
                typingSpeed
            );

        if (prepared == false)
        {
            yield break;
        }

        yield return
            PlayDisplayRoutine(
                currentVersion,
                duration,
                typingSpeed,
                useEmphasisShake,
                emphasisStrength
            );
    }


    // <변경부분>
    // Wait For Complete = false용.
    //
    // 동일한 말풍선 연출은 계속 실행되지만
    // EventSequence는 즉시 다음 Step으로 진행한다.
    public void PlayDetached(
        string text,
        float duration,
        float typingSpeed,
        bool useEmphasisShake,
        float emphasisStrength)
    {
        StopDetachedCoroutine();
        StopTextShakeCoroutine();
        StopIdleFloatCoroutine();

        int currentVersion =
            ++displayVersion;

        // 비활성화된 SpeechBubbleCanvas에서는
        // Coroutine을 직접 시작할 수 없으므로
        // 먼저 표시 준비를 완료한다.
        bool prepared =
            PrepareDisplay(
                text,
                typingSpeed
            );

        if (prepared == false)
        {
            return;
        }

        detachedCoroutine =
            StartCoroutine(
                PlayDetachedRoutine(
                    currentVersion,
                    duration,
                    typingSpeed,
                    useEmphasisShake,
                    emphasisStrength
                )
            );
    }


    // <변경부분>
    // 시간 제한 없이 말풍선을 표시한다.
    //
    // 추후 Event 연출에서
    // Show → 다른 Step들 → Hide 방식이 필요할 때도 사용할 수 있다.
    public void Show(
    string text)
    {
        StopDetachedCoroutine();
        StopTextShakeCoroutine();
        StopIdleFloatCoroutine();

        displayVersion++;

        bool prepared =
            PrepareDisplay(
                text,
                0f
            );

        if (prepared == false)
        {
            return;
        }

        bubbleText.maxVisibleCharacters =
            int.MaxValue;

        SetBubbleAlpha(
    1f
);

        bubbleRoot.localScale =
            bubbleBaseLocalScale;

        // <변경부분>
        // 시간 제한 없는 Show에서도
        // 동일한 Idle Float 연출을 유지한다.
        StartIdleFloat(
            displayVersion
        );
    }


    // 현재 말풍선을 즉시 숨긴다.
    public void HideImmediately()
    {
        StopDetachedCoroutine();
        StopTextShakeCoroutine();

        // <변경부분>
        // 강제 종료 시 말풍선 위치도
        // 반드시 Inspector 기본 위치로 복원한다.
        StopIdleFloatCoroutine();

        displayVersion++;

        HideInternal();
    }


    // =====================================================
    // Display
    // =====================================================

    // <변경부분>
    // 말풍선 실제 표시 직전 상태를 준비한다.
    //
    // 타이핑 사용 시:
    // - Text에는 전체 문장을 저장
    // - 처음에는 표시 글자 수 0
    // - Bubble은 최소 크기로 시작
    // - 이후 글자가 늘어날 때마다 Layout을 다시 계산
    //
    // 따라서 말풍선이 가운데 기준으로
    // 타이핑과 함께 자연스럽게 확장될 수 있다.
    private bool PrepareDisplay(
        string text,
        float typingSpeed)
    {
        AutoBindReferences();
        CacheBaseVisualState();

        if (speechBubbleCanvas == null ||
            bubbleRoot == null ||
            bubbleText == null ||
            textLayoutElement == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                "SpeechBubble UI Reference가 연결되지 않았습니다."
            );

            return false;
        }

        if (speechBubbleCanvas.activeSelf ==
            false)
        {
            speechBubbleCanvas.SetActive(
                true
            );
        }

        currentDisplayText =
            text ??
            string.Empty;

        // <변경부분>
        // TMP에는 전체 문장을 유지한다.
        //
        // 이후 Typewriter에서는 문자열 자체를 잘라내지 않고
        // maxVisibleCharacters만 변경한다.
        bubbleText.text =
            currentDisplayText;

        bubbleText.enableWordWrapping =
            true;

        // <변경부분>
        // 전체 TextInfo를 미리 생성한다.
        //
        // 이전 SpeechBubble에서 maxVisibleCharacters가
        // 제한되어 있었을 가능성이 있으므로
        // 먼저 전체 표시 상태로 Mesh 정보를 계산한다.
        bubbleText.maxVisibleCharacters =
            int.MaxValue;

        bubbleText.ForceMeshUpdate();

        if (typingSpeed <= 0f)
        {
            // 타이핑을 사용하지 않는 경우에는
            // 처음부터 전체 문장 크기로 표시한다.
            RebuildBubbleLayout(
                currentDisplayText
            );

            bubbleText.maxVisibleCharacters =
                int.MaxValue;
        }
        else
        {
            // <변경부분>
            // 타이핑을 사용하는 경우에는
            // 처음에는 최소 말풍선 크기로 시작한다.
            RebuildBubbleLayout(
                string.Empty
            );

            bubbleText.maxVisibleCharacters =
                0;
        }

        // Layout 계산이 끝난 시점의 위치를
        // Text Shake 기준 위치로 저장한다.
        bubbleTextBaseAnchoredPosition =
            bubbleText.rectTransform
                .anchoredPosition;

        // 등장 애니메이션 시작 상태.
        SetBubbleAlpha(
            0f
        );

        bubbleRoot.localScale =
            bubbleBaseLocalScale *
            entryStartScale;

        return true;
    }

    // =====================================================
    // Speech Bubble Animation
    // =====================================================

    // <변경부분>
    // SpeechBubble 한 번의 전체 표시 흐름.
    //
    // 등장 Pop과 Typewriter를 동시에 시작하여
    // 글자가 나타나는 동안 말풍선 / Text / Tail 전체도
    // 함께 확대됐다가 원래 크기로 돌아온다.
    private IEnumerator PlayDisplayRoutine(
    int currentVersion,
    float duration,
    float typingSpeed,
    bool useEmphasisShake,
    float emphasisStrength)
    {
        // <변경부분>
        // 말풍선이 화면에 등장하는 순간부터
        // 전체 Bubble을 천천히 위아래로 움직인다.
        //
        // Typewriter / Pop / Text Shake와 독립된 Coroutine이며
        // BubbleRoot 위치만 담당한다.
        StartIdleFloat(
            currentVersion
        );

        // Typewriter를 별도 Coroutine으로 먼저 시작한다.
        //
        // 따라서:
        // - Text가 한 글자씩 나타나고
        // - Bubble 크기가 글자 수에 따라 증가하는 동안
        // - Entry Pop 애니메이션도 동시에 진행된다.
        //
        // Text는 BubbleRoot의 자식이므로
        // BubbleRoot Scale 변화에 따라 같이 확대/축소된다.
        Coroutine typingCoroutine =
            StartCoroutine(
                PlayTypingRoutine(
                    currentVersion,
                    typingSpeed
                )
            );

        // <변경부분>
        // Typewriter와 동시에
        // Fade In + 순간 확대 + 원래 Scale 복귀를 진행한다.
        yield return
            PlayEntryAnimationRoutine(
                currentVersion
            );

        if (currentVersion !=
            displayVersion)
        {
            yield break;
        }

        // <변경부분>
        // Pop 애니메이션이 먼저 끝났더라도
        // 아직 Typewriter가 진행 중이라면
        // 타이핑 완료까지 기다린다.
        if (typingCoroutine != null)
        {
            yield return
                typingCoroutine;
        }

        if (currentVersion !=
            displayVersion)
        {
            yield break;
        }

        // <변경부분>
        // Layout 변경이 모두 끝난 뒤
        // 강조 효과가 켜져 있다면 Text만 흔든다.
        //
        // 이렇게 해야 타이핑 중 LayoutGroup의 위치 갱신과
        // Text Shake가 서로 충돌하지 않는다.
        if (useEmphasisShake &&
            emphasisStrength > 0f)
        {
            StartTextShake(
                currentVersion,
                emphasisStrength
            );
        }

        // 타이핑이 끝난 완성 문장을 유지한다.
        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            if (currentVersion !=
                displayVersion)
            {
                yield break;
            }

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        StopTextShakeCoroutine();

        if (currentVersion !=
            displayVersion)
        {
            yield break;
        }

        // 말풍선 종료 Fade Out.
        yield return
            PlayFadeOutRoutine(
                currentVersion
            );

        if (currentVersion !=
            displayVersion)
        {
            yield break;
        }

        HideInternal();
    }


    // <변경부분>
    // 처음에는 살짝 작게 등장하면서 Fade In,
    // 원래 크기보다 조금 크게 튄 뒤
    // 부드럽게 기본 크기로 돌아온다.
    private IEnumerator PlayEntryAnimationRoutine(
        int currentVersion)
    {
        Vector3 startScale =
            bubbleBaseLocalScale *
            entryStartScale;

        Vector3 overshootScale =
            bubbleBaseLocalScale *
            entryOvershootScale;

        float safeFadeInDuration =
            Mathf.Max(
                0f,
                fadeInDuration
            );

        if (safeFadeInDuration <= 0f)
        {
            SetBubbleAlpha(
                1f
            );

            bubbleRoot.localScale =
                overshootScale;
        }
        else
        {
            float elapsedTime =
                0f;

            while (elapsedTime <
                   safeFadeInDuration)
            {
                if (currentVersion !=
                    displayVersion)
                {
                    yield break;
                }

                elapsedTime +=
                    Time.unscaledDeltaTime;

                float normalizedTime =
                    Mathf.Clamp01(
                        elapsedTime /
                        safeFadeInDuration
                    );

                float easedTime =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        normalizedTime
                    );

                SetBubbleAlpha(
                    easedTime
                );

                bubbleRoot.localScale =
                    Vector3.Lerp(
                        startScale,
                        overshootScale,
                        easedTime
                    );

                yield return null;
            }
        }

        float safeSettleDuration =
            Mathf.Max(
                0f,
                entrySettleDuration
            );

        if (safeSettleDuration <= 0f)
        {
            bubbleRoot.localScale =
                bubbleBaseLocalScale;

            yield break;
        }

        float settleElapsed =
            0f;

        while (settleElapsed <
               safeSettleDuration)
        {
            if (currentVersion !=
                displayVersion)
            {
                yield break;
            }

            settleElapsed +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    settleElapsed /
                    safeSettleDuration
                );

            float easedTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            bubbleRoot.localScale =
                Vector3.Lerp(
                    overshootScale,
                    bubbleBaseLocalScale,
                    easedTime
                );

            yield return null;
        }

        bubbleRoot.localScale =
            bubbleBaseLocalScale;
    }


    // <변경부분>
    // TMP maxVisibleCharacters를 사용하는 Typewriter.
    //
    // 글자가 하나씩 표시될 때마다
    // 현재까지 표시된 문자열 기준으로 Bubble Layout도 다시 계산한다.
    //
    // 따라서:
    // 짧은 문장 → 좌우로 증가
    // Max Width 도달 → 가로 고정
    // 이후 줄바꿈 → 세로 증가
    private IEnumerator PlayTypingRoutine(
        int currentVersion,
        float typingSpeed)
    {
        if (bubbleText == null)
        {
            yield break;
        }

        if (typingSpeed <= 0f)
        {
            bubbleText.maxVisibleCharacters =
                int.MaxValue;

            RebuildBubbleLayout(
                currentDisplayText
            );

            yield break;
        }

        // 전체 문자열 기준 TMP Character 정보를 준비한다.
        bubbleText.maxVisibleCharacters =
            int.MaxValue;

        bubbleText.ForceMeshUpdate();

        int totalCharacterCount =
            bubbleText.textInfo.characterCount;

        bubbleText.maxVisibleCharacters =
            0;

        if (totalCharacterCount <= 0)
        {
            yield break;
        }

        float visibleCharacterProgress =
            0f;

        int previousVisibleCount =
            0;

        while (previousVisibleCount <
               totalCharacterCount)
        {
            if (currentVersion !=
                displayVersion)
            {
                yield break;
            }

            visibleCharacterProgress +=
                typingSpeed *
                Time.unscaledDeltaTime;

            int visibleCount =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        visibleCharacterProgress
                    ),
                    0,
                    totalCharacterCount
                );

            // <변경부분>
            // 같은 글자 수가 유지되는 Frame에서는
            // 불필요한 Layout Rebuild를 반복하지 않는다.
            if (visibleCount !=
                previousVisibleCount)
            {
                previousVisibleCount =
                    visibleCount;

                bubbleText.maxVisibleCharacters =
                    visibleCount;

                string visibleLayoutText =
                    GetVisibleTextForLayout(
                        visibleCount
                    );

                RebuildBubbleLayout(
                    visibleLayoutText
                );

                // <변경부분>
                // Layout이 변경된 뒤의 중앙 위치를
                // Text Shake 기준 위치로 갱신한다.
                //
                // 강조 Shake는 타이핑 완료 후 시작하므로
                // LayoutGroup과 위치 경쟁이 발생하지 않는다.
                bubbleTextBaseAnchoredPosition =
                    bubbleText.rectTransform
                        .anchoredPosition;
            }

            yield return null;
        }

        bubbleText.maxVisibleCharacters =
            int.MaxValue;

        // 마지막에는 전체 문자열 기준으로
        // 최종 크기를 정확하게 한 번 더 보정한다.
        RebuildBubbleLayout(
            currentDisplayText
        );

        bubbleTextBaseAnchoredPosition =
            bubbleText.rectTransform
                .anchoredPosition;
    }

    // <변경부분>
    // TMP Text에는 전체 문장을 유지하면서,
    // 현재 보이는 마지막 글자의 실제 문자열 Index까지만 잘라
    // Layout 계산용 문자열을 만든다.
    //
    // 단순 Substring(0, visibleCharacterCount)을 사용하지 않는 이유는
    // TMP Rich Text Tag나 여러 글자 단위 문자가 들어왔을 때
    // 표시 문자 수와 원본 문자열 Index가 다를 수 있기 때문이다.
    private string GetVisibleTextForLayout(
        int visibleCharacterCount)
    {
        if (bubbleText == null ||
            string.IsNullOrEmpty(
                currentDisplayText) ||
            visibleCharacterCount <= 0)
        {
            return string.Empty;
        }

        TMP_TextInfo textInfo =
            bubbleText.textInfo;

        if (textInfo == null ||
            textInfo.characterCount <= 0)
        {
            return string.Empty;
        }

        if (visibleCharacterCount >=
            textInfo.characterCount)
        {
            return currentDisplayText;
        }

        int lastCharacterIndex =
            visibleCharacterCount - 1;

        TMP_CharacterInfo lastCharacter =
            textInfo.characterInfo[
                lastCharacterIndex
            ];

        int endStringIndex =
            lastCharacter.index +
            lastCharacter.stringLength;

        endStringIndex =
            Mathf.Clamp(
                endStringIndex,
                0,
                currentDisplayText.Length
            );

        return currentDisplayText.Substring(
            0,
            endStringIndex
        );
    }


    // <변경부분>
    // 말풍선 종료 Fade Out.
    private IEnumerator PlayFadeOutRoutine(
        int currentVersion)
    {
        float safeFadeOutDuration =
            Mathf.Max(
                0f,
                fadeOutDuration
            );

        if (safeFadeOutDuration <= 0f)
        {
            SetBubbleAlpha(
                0f
            );

            yield break;
        }

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeFadeOutDuration)
        {
            if (currentVersion !=
                displayVersion)
            {
                yield break;
            }

            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    safeFadeOutDuration
                );

            SetBubbleAlpha(
                1f -
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                )
            );

            yield return null;
        }

        SetBubbleAlpha(
            0f
        );
    }


    // <변경부분>
    // Bubble은 고정한 채 TMP Text만 흔드는 강조 효과.
    private void StartTextShake(
        int currentVersion,
        float strength)
    {
        StopTextShakeCoroutine();

        if (bubbleText == null)
        {
            return;
        }

        bubbleTextBaseAnchoredPosition =
            bubbleText.rectTransform
                .anchoredPosition;

        textShakeCoroutine =
            StartCoroutine(
                PlayTextShakeRoutine(
                    currentVersion,
                    Mathf.Max(
                        0f,
                        strength
                    )
                )
            );
    }


    private IEnumerator PlayTextShakeRoutine(
        int currentVersion,
        float strength)
    {
        RectTransform textRect =
            bubbleText != null
                ? bubbleText.rectTransform
                : null;

        if (textRect == null)
        {
            textShakeCoroutine =
                null;

            yield break;
        }

        while (currentVersion ==
               displayVersion)
        {
            Vector2 shakeOffset =
                Random.insideUnitCircle *
                strength;

            textRect.anchoredPosition =
                bubbleTextBaseAnchoredPosition +
                shakeOffset;

            yield return null;
        }

        textRect.anchoredPosition =
            bubbleTextBaseAnchoredPosition;

        textShakeCoroutine =
            null;
    }


    private void StopTextShakeCoroutine()
    {
        if (textShakeCoroutine != null)
        {
            StopCoroutine(
                textShakeCoroutine
            );

            textShakeCoroutine =
                null;
        }

        if (bubbleText != null)
        {
            bubbleText.rectTransform
                .anchoredPosition =
                    bubbleTextBaseAnchoredPosition;
        }
    }


    // =====================================================
    // Idle Float
    // =====================================================

    // <변경부분>
    // 현재 SpeechBubble의 Idle Float을 시작한다.
    //
    // BubbleRoot 전체를 움직이므로
    // Background / Text / Tail이 함께 움직인다.
    private void StartIdleFloat(
        int currentVersion)
    {
        StopIdleFloatCoroutine();

        if (useIdleFloat == false ||
            bubbleRoot == null ||
            idleFloatAmplitude <= 0f)
        {
            return;
        }

        idleFloatCoroutine =
            StartCoroutine(
                PlayIdleFloatRoutine(
                    currentVersion
                )
            );
    }


    // <변경부분>
    // Sin 곡선을 사용하여 급격한 방향 전환 없이
    // 천천히 위아래로 떠다니는 느낌을 만든다.
    //
    // Time.unscaledDeltaTime을 사용하므로
    // Battle의 TimeScale 연출에도 영향을 받지 않는다.
    private IEnumerator PlayIdleFloatRoutine(
        int currentVersion)
    {
        if (bubbleRoot == null)
        {
            idleFloatCoroutine =
                null;

            yield break;
        }

        float safeCycleDuration =
            Mathf.Max(
                0.1f,
                idleFloatCycleDuration
            );

        float elapsedTime =
            0f;

        while (currentVersion ==
               displayVersion)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                elapsedTime /
                safeCycleDuration;

            float phase =
                normalizedTime *
                Mathf.PI *
                2f;

            float verticalOffset =
                Mathf.Sin(
                    phase
                ) *
                idleFloatAmplitude;

            bubbleRoot.anchoredPosition =
                bubbleBaseAnchoredPosition +
                new Vector2(
                    0f,
                    verticalOffset
                );

            yield return null;
        }

        // Coroutine이 자연 종료되더라도
        // 항상 원래 위치로 돌려놓는다.
        if (bubbleRoot != null)
        {
            bubbleRoot.anchoredPosition =
                bubbleBaseAnchoredPosition;
        }

        idleFloatCoroutine =
            null;
    }


    // <변경부분>
    // 새 SpeechBubble 표시 / 강제 종료 / Hide 시
    // Idle Coroutine을 정리하고 정확한 기본 위치로 복원한다.
    private void StopIdleFloatCoroutine()
    {
        if (idleFloatCoroutine != null)
        {
            StopCoroutine(
                idleFloatCoroutine
            );

            idleFloatCoroutine =
                null;
        }

        if (bubbleRoot != null &&
            isBaseVisualStateCached)
        {
            bubbleRoot.anchoredPosition =
                bubbleBaseAnchoredPosition;
        }
    }

    private void SetBubbleAlpha(
        float alpha)
    {
        if (bubbleCanvasGroup == null)
        {
            return;
        }

        bubbleCanvasGroup.alpha =
            Mathf.Clamp01(
                alpha
            );
    }


    // <변경부분>
    // World Space Canvas에서 사용 중인 실제 Scale을 보존한다.
    private void CacheBaseVisualState()
    {
        if (isBaseVisualStateCached ||
            bubbleRoot == null)
        {
            return;
        }

        // <변경부분>
        // Pop용 Scale뿐 아니라
        // Idle Float이 돌아올 기본 위치도 함께 저장한다.
        bubbleBaseLocalScale =
            bubbleRoot.localScale;

        bubbleBaseAnchoredPosition =
            bubbleRoot.anchoredPosition;

        isBaseVisualStateCached =
            true;
    }

    private void HideInternal()
    {
        StopTextShakeCoroutine();

        // <변경부분>
        // 말풍선이 사라질 때
        // Idle Float을 정지하고 원래 위치로 복원한다.
        StopIdleFloatCoroutine();

        if (bubbleText != null)
        {
            bubbleText.maxVisibleCharacters =
                int.MaxValue;
        }

        if (bubbleRoot != null &&
            isBaseVisualStateCached)
        {
            bubbleRoot.localScale =
                bubbleBaseLocalScale;
        }

        SetBubbleAlpha(
            0f
        );

        if (speechBubbleCanvas == null)
        {
            return;
        }

        speechBubbleCanvas.SetActive(
            false
        );
    }


    // =====================================================
    // Layout
    // =====================================================

    // <변경부분>
    // Text 길이에 따라:
    //
    // 짧은 문구:
    // 말풍선 가로 크기 증가
    //
    // 긴 문구:
    // maxTextWidth에서 가로 증가 중단
    // → 자동 줄바꿈
    // → 세로 크기 증가
    //
    // BubbleRoot의 실제 Padding은
    // HorizontalLayoutGroup Inspector 값으로 관리한다.
    private void RebuildBubbleLayout(
        string text)
    {
        if (bubbleRoot == null ||
            bubbleText == null ||
            textLayoutElement == null)
        {
            return;
        }

        float safeMinWidth =
            Mathf.Max(
                1f,
                minTextWidth
            );

        float safeMaxWidth =
            Mathf.Max(
                safeMinWidth,
                maxTextWidth
            );

        // <변경부분>
        // 우선 줄바꿈 제한이 없는 자연스러운 Text Width를 확인한다.
        Vector2 naturalPreferredSize =
            bubbleText.GetPreferredValues(
                text
            );

        float targetTextWidth =
            Mathf.Clamp(
                naturalPreferredSize.x,
                safeMinWidth,
                safeMaxWidth
            );

        // <변경부분>
        // 최종 Width를 기준으로 실제 줄바꿈된 Height를 다시 계산한다.
        Vector2 wrappedPreferredSize =
            bubbleText.GetPreferredValues(
                text,
                targetTextWidth,
                0f
            );

        float targetTextHeight =
            Mathf.Max(
                minTextHeight,
                wrappedPreferredSize.y
            );

        // LayoutGroup에 최종 Text 크기를 전달한다.
        textLayoutElement.minWidth =
            safeMinWidth;

        textLayoutElement.preferredWidth =
            targetTextWidth;

        textLayoutElement.preferredHeight =
            targetTextHeight;

        textLayoutElement.flexibleWidth =
            0f;

        textLayoutElement.flexibleHeight =
            0f;

        // TMP 자체 Mesh 정보도 즉시 갱신한다.
        bubbleText.ForceMeshUpdate();

        // <변경부분>
        // LayoutGroup + ContentSizeFitter 계산을
        // 같은 Frame 안에서 즉시 반영한다.
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            bubbleRoot
        );
    }


    // =====================================================
    // Detached
    // =====================================================

    private IEnumerator PlayDetachedRoutine(
     int currentVersion,
     float duration,
     float typingSpeed,
     bool useEmphasisShake,
     float emphasisStrength)
    {
        yield return
            PlayDisplayRoutine(
                currentVersion,
                duration,
                typingSpeed,
                useEmphasisShake,
                emphasisStrength
            );

        detachedCoroutine =
            null;
    }


    private void StopDetachedCoroutine()
    {
        if (detachedCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            detachedCoroutine
        );

        detachedCoroutine =
            null;
    }


    // =====================================================
    // Reference
    // =====================================================

    // <변경부분>
    // PiecePrefab 또는 Event Actor에서
    // Inspector Reference가 빠졌을 때를 대비한 최소 fallback.
    //
    // 기본적으로는 Inspector에서 직접 연결하는 것을 권장한다.
    private void AutoBindReferences()
    {
        if (speechBubbleCanvas == null)
        {
            speechBubbleCanvas =
                gameObject;
        }

        if (bubbleText == null)
        {
            bubbleText =
                GetComponentInChildren<
                    TMP_Text
                >(
                    true
                );
        }

        if (textLayoutElement == null &&
            bubbleText != null)
        {
            textLayoutElement =
                bubbleText.GetComponent<
                    LayoutElement
                >();
        }

        if (bubbleRoot == null &&
            bubbleText != null)
        {
            bubbleRoot =
                bubbleText.transform.parent
                    as RectTransform;
        }

        // <변경부분>
        // SpeechBubbleCanvas에 추가한 CanvasGroup을 자동 연결한다.
        if (bubbleCanvasGroup == null &&
            speechBubbleCanvas != null)
        {
            bubbleCanvasGroup =
                speechBubbleCanvas
                    .GetComponent<CanvasGroup>();
        }
    }
}
