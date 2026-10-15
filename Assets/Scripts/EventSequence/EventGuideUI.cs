using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// <변경부분> 이벤트 / 튜토리얼 공용 Dialogue UI
//
// Dialogue 진행 중:
// - 글자가 타이핑되듯 표시
// - 타이핑 중 클릭하면 현재 페이지 즉시 완성
// - 완성된 뒤 새로 한 번 클릭해야 다음 페이지로 진행
//
// InputBlocker Button은 뒤쪽 Battle UI 클릭 차단용으로만 사용하고,
// 실제 페이지 진행 입력은 이 스크립트에서 직접 감지한다.
public class EventGuideUI : MonoBehaviour
{
    [Header("Root")]
    // Dialogue 전체 UI 루트
    [SerializeField]
    private GameObject guideRoot;

    [Header("Input Block")]
    // <변경부분> 뒤쪽 Battle UI와 보드 클릭을 막기 위한
    // 전체 화면 투명 Button
    //
    // Dialogue 페이지 진행 이벤트에는 사용하지 않는다.
    [SerializeField]
    private Button continueButton;

    [Header("Open Animation")]
    // <변경부분> DialoguePanel이 활성화될 때
    // 기존 PopupOpenAnimationData를 사용해
    // 공용 팝업 오픈 애니메이션을 재생한다.
    //
    // DialoguePanel 오브젝트에 PopupOpenAnimator를 붙이고
    // 이 필드에 연결한다.
    [SerializeField]
    private PopupOpenAnimator popupOpenAnimator;

    [Header("Text")]
    // 실제 Dialogue 문장을 표시할 TMP Text
    [SerializeField]
    private TMP_Text guideText;


    // <변경부분>
    // EventUI 아래에 별도로 준비한 선택지 부모 오브젝트.
    // GuideRoot의 형제 오브젝트로 두어도 된다.
    [Header("Dialogue Choices")]
    [SerializeField]
    private GameObject selectPanelRoot;

    // Button 컴포넌트가 있는 SelectDialoguePanel Prefab Asset.
    // 자식 TMP_Text에 선택지 문구를 자동 적용한다.
    [SerializeField]
    private Button selectDialoguePanelPrefab;


    // =====================================================
    // Choice Click Feedback
    // =====================================================

    // <변경부분>
    // 선택지 버튼 클릭 시 확대 잔상 효과의 지속 시간.
    //
    // 효과가 끝난 후 실제 Target Step으로 분기한다.
    [Header("Choice Click Feedback")]
    [SerializeField, Range(0.05f, 0.5f)]
    private float choiceClickFeedbackDuration = 0.18f;

    // <변경부분>
    // 선택한 버튼의 배경 잔상이 확대되는 최종 배율.
    // 1.12 = 원본 대비 12% 확대.
    [SerializeField, Range(1f, 1.5f)]
    private float choiceClickGhostEndScale = 1.12f;

    // <변경부분>
    // 잔상 시작 투명도.
    // 원본 버튼보다 옅게 표시한 뒤 0까지 사라진다.
    [SerializeField, Range(0f, 1f)]
    private float choiceClickGhostAlpha = 0.4f;


    [Header("Continue Arrow")]
    // <변경부분> 현재 Dialogue 페이지의 모든 글자가 출력된 뒤
    // 플레이어의 다음 클릭을 기다리고 있음을 표시하는 화살표.
    //
    // DialoguePanel 하단에 별도의 Image 오브젝트로 배치하며,
    // 타이핑 중에는 숨기고 클릭 대기 상태에서만 표시한다.
    [SerializeField]
    private Image continueArrowImage;

    // <변경부분> 화살표가 가장 흐려졌을 때의 Alpha.
    //
    // 0 = 완전히 투명
    // 1 = 완전히 불투명
    [SerializeField, Range(0f, 1f)]
    private float continueArrowMinAlpha =
        0.25f;

    // <변경부분> 화살표가
    // 밝음 → 흐림 또는 흐림 → 밝음으로 전환되는 시간.
    //
    // 전체 한 번의 깜빡임 주기는 이 값의 약 2배다.
    [SerializeField, Min(0.05f)]
    private float continueArrowFadeDuration =
        0.4f;

    [Header("Typing Animation")]
    // 글자 한 글자가 나타나는 간격
    [SerializeField, Min(0.001f)]
    private float characterInterval =
        0.03f;

    // <변경부분>
    // 타이핑 도중 클릭하여 전체 문장을 표시한 경우,
    // 다음 페이지 입력을 허용하기 전까지 기다리는 시간.
    // TimeScale의 영향을 받지 않는다.
    [SerializeField, Min(0f)]
    private float typingSkipAdvanceDelay = 0.5f;

    // Time.timeScale과 관계없이 Dialogue를 진행할지 여부
    [SerializeField]
    private bool useUnscaledTime =
        true;

    // 현재 Dialogue 전체가 실행 중인지 확인
    private bool isDialoguePlaying =
        false;

    // 현재 페이지가 타이핑 중인지 확인
    private bool isTyping =
        false;

    // 타이핑 중 현재 페이지 전체 표시 요청
    private bool isTypingSkipRequested =
        false;

    // 타이핑이 끝난 뒤 다음 페이지 진행 요청
    private bool isAdvanceRequested =
        false;

    // <변경부분> 현재 페이지의 다음 페이지 입력을
    // 실제로 받을 수 있는 상태인지 확인한다.
    //
    // 타이핑 종료와 같은 프레임의 클릭이
    // 다음 페이지까지 연속으로 넘기는 것을 방지한다.
    private bool canAcceptAdvanceInput =
        false;

    // <변경부분> 페이지가 완전히 표시된 프레임
    //
    // 최소 다음 프레임부터만 페이지 넘김을 허용한다.
    private int pageCompletedFrame =
    -1;

    // <변경부분> ContinueArrow의 반복 깜빡임을 담당하는 Coroutine.
    //
    // 새 페이지 시작 / 클릭 / Dialogue 종료 시
    // 기존 Coroutine을 확실하게 정리하기 위해 저장한다.
    private Coroutine continueArrowBlinkCoroutine =
      null;

    // <변경부분>
    // 생성한 버튼만 추적하여 기존 SelectPanelRoot 자식은 삭제하지 않는다.
    private readonly List<Button> createdChoiceButtons =
        new List<Button>();

    private bool isChoiceSelectionActive = false;
    private bool isChoiceSelected = false;
    private string selectedChoiceTargetStepId = null;

    // <변경부분>
    // 선택지 클릭 잔상 애니메이션 진행 중 여부.
    //
    // true인 동안 다른 선택지를 누르거나
    // 같은 선택지가 중복 실행되지 않게 한다.
    private bool isChoiceClickAnimating = false;

    // BattleManager 등 외부 시스템이
    // Dialogue 입력 차단 여부를 확인할 때 사용
    public bool IsDialoguePlaying
    {
        get
        {
            return isDialoguePlaying;
        }
    }

    private void Awake()
    {
        if (guideRoot == null)
        {
            guideRoot =
                gameObject;
        }

        // <변경부분> PopupOpenAnimator가 Inspector에
        // 직접 연결되지 않은 경우 Dialogue UI 자식에서 자동으로 찾는다.
        //
        // DialoguePanel에 PopupOpenAnimator를 붙여두면
        // GuideRoot 구조가 바뀌어도 재사용할 수 있다.
        if (popupOpenAnimator == null &&
            guideRoot != null)
        {
            popupOpenAnimator =
                guideRoot.GetComponentInChildren<
                    PopupOpenAnimator
                >(
                    true
                );
        }

        // <변경부분> continueButton은 뒤쪽 Battle UI 입력을
        // 차단하기 위한 용도로만 유지한다.
        //
        // Dialogue 페이지 진행은 EventGuideUI.Update()에서
        // 직접 입력을 감지하므로 Button OnClick은 사용하지 않는다.
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
        }

        // <변경부분> 여기서는 ResetDialogueState()를 호출하지 않는다.
        //
        // GuideRoot가 기본 OFF인 경우,
        // PlayDialogueRoutine()이 먼저 실행된 뒤
        // guideRoot.SetActive(true) 시점에 Awake()가 최초 호출될 수 있다.
        //
        // 그 상황에서 상태를 초기화하면
        // 이미 시작한 Dialogue의 isDialoguePlaying이 false로 바뀌면서
        // Element 0 출력 직후 Dialogue Step이 강제로 종료된다.
        //
        // 런타임 필드 자체가 이미 기본값으로 초기화되어 있으므로
        // Awake에서 별도의 상태 초기화는 필요하지 않다.

        if (guideText != null)
        {
            guideText.text =
                string.Empty;

            guideText.maxVisibleCharacters =
                int.MaxValue;
        }
        // 차단하기 위한 용도로만 유지한다.
        //
        // Dialogue 페이지 진행은 EventGuideUI.Update()에서
        // 직접 입력을 감지하므로 Button OnClick은 사용하지 않는다.
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
        }

        // <변경부분> 여기서는 ResetDialogueState()를 호출하지 않는다.
        //
        // GuideRoot가 기본 OFF인 경우,
        // PlayDialogueRoutine()이 먼저 실행된 뒤
        // guideRoot.SetActive(true) 시점에 Awake()가 최초 호출될 수 있다.
        //
        // 그 상황에서 상태를 초기화하면
        // 이미 시작한 Dialogue의 isDialoguePlaying이 false로 바뀌면서
        // Element 0 출력 직후 Dialogue Step이 강제로 종료된다.
        //
        // 런타임 필드 자체가 이미 기본값으로 초기화되어 있으므로
        // Awake에서 별도의 상태 초기화는 필요하지 않다.

        if (guideText != null)
        {
            guideText.text =
                string.Empty;

            guideText.maxVisibleCharacters =
                int.MaxValue;
        }

        // <변경부분> Scene 시작 시 ContinueArrow는
        // Dialogue 페이지 출력 완료 전까지 보이지 않도록 초기화한다.
        if (continueArrowImage != null)
        {
            continueArrowImage.raycastTarget =
                false;

            Color arrowColor =
                continueArrowImage.color;

            arrowColor.a =
                1f;

            continueArrowImage.color =
                arrowColor;

            continueArrowImage.gameObject.SetActive(
     false
 );
        }

        // <변경부분>
        // 기본 상태에서는 선택지 부모를 숨겨 놓는다.
        if (selectPanelRoot != null)
        {
            selectPanelRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (isDialoguePlaying == false)
        {
            return;
        }

        // <변경부분>
        // 선택지 표시 중이나 선택 직후의 클릭은
        // 기존 Dialogue 페이지 넘기기 입력으로 처리하지 않는다.
        if (isChoiceSelectionActive || isChoiceSelected)
        {
            return;
        }

        // <변경부분> 현재 페이지의 타이핑이 끝나기 전에는
        // 클릭을 페이지 진행 입력으로 사용하지 않는다.
        //
        // Element 하나의 문장이 모두 출력된 뒤
        // 반드시 새 클릭이 들어와야 다음 Element로 진행한다.
        if (WasDialogueAdvancePressed() == false)
        {
            return;
        }

        // <변경부분>
        // 타이핑 도중 첫 클릭은 다음 페이지 이동이 아니라
        // 현재 페이지의 남은 글자를 모두 표시하는 요청이다.
        if (isTyping)
        {
            isTypingSkipRequested = true;
            return;
        }

        // <변경부분>
        // 타이핑이 끝났더라도 스킵 후 대기 시간이
        // 지나지 않았다면 추가 클릭을 무시한다.
        if (canAcceptAdvanceInput == false)
        {
            return;
        }

        // <변경부분> 페이지 타이핑이 끝난 바로 그 프레임의 입력은
        // 다음 페이지 진행으로 인정하지 않는다.
        if (Time.frameCount <=
            pageCompletedFrame)
        {
            return;
        }

        // 현재 페이지를 플레이어가 직접 넘겼다고 기록한다.
        isAdvanceRequested =
            true;

        // 같은 클릭이 중복 처리되지 않도록 즉시 잠근다.
        canAcceptAdvanceInput =
            false;
    }

    // <변경부분> PC와 모바일에서 공통으로
    // Dialogue 진행 클릭을 감지한다.
    private bool WasDialogueAdvancePressed()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }
#endif

        // 모바일 실제 터치 입력
        if (Input.touchCount > 0)
        {
            Touch touch =
                Input.GetTouch(0);

            if (touch.phase ==
                TouchPhase.Began)
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분>
    // 기존 Battle / Tutorial 호출은 이전 시그니처 그대로 유지한다.
    public IEnumerator PlayDialogueRoutine(
        List<string> dialoguePages)
    {
        return PlayDialogueRoutine(
            dialoguePages,
            null,
            null
        );
    }


    // <변경부분>
    // Event Scene에서는 Dialogue 마지막 페이지가 끝나면
    // 선택지를 보여주고 클릭된 Step ID를 Callback으로 전달한다.
    public IEnumerator PlayDialogueRoutine(
        List<string> dialoguePages,
        List<EventSceneDialogueChoiceData> dialogueChoices,
        System.Action<string> onChoiceSelected)
    {
        if (dialoguePages == null ||
            dialoguePages.Count == 0)
        {
            yield break;
        }

        if (guideText == null)
        {
            Debug.LogWarning(
                "이벤트 Dialogue 실행 실패: " +
                "Guide Text가 연결되지 않았습니다."
            );

            yield break;
        }

        // <변경부분> Dialogue 상태를 설정하기 전에
        // 먼저 비활성화되어 있던 GuideRoot를 활성화한다.
        //
        // EventGuideUI가 GuideRoot 자체에 붙어 있고
        // GuideRoot가 기본 OFF인 경우,
        // 이 SetActive(true) 순간에 Awake()가 최초 실행될 수 있다.
        //
        // 따라서 Awake가 모두 끝난 뒤
        // Dialogue 런타임 상태를 설정해야 한다.
        if (guideRoot != null)
        {
            guideRoot.SetActive(
                true
            );
        }

        // <변경부분> GuideRoot가 활성화되고
        // DialoguePanel의 PopupOpenAnimator가 사용 가능한 상태가 된 뒤
        // 기존 PopupOpenAnimationData 기반 오픈 애니메이션을 실행한다.
        //
        // Dialogue Step 하나가 시작될 때 한 번만 재생되며,
        // 같은 Step 안에서 다음 페이지로 넘길 때마다
        // 다시 재생하지는 않는다.
        if (popupOpenAnimator != null)
        {
            popupOpenAnimator.PlayOpen();
        }

        // <변경부분> GuideRoot 활성화 및 Awake가 모두 끝난 뒤
        // 실제 Dialogue 실행 상태를 초기화한다.
        isDialoguePlaying =
            true;

        isTyping =
            false;

        isTypingSkipRequested =
            false;

        isAdvanceRequested =
            false;

        canAcceptAdvanceInput =
            false;

        pageCompletedFrame =
       -1;

        // <변경부분>
        // 이전 Dialogue에서 생성된 버튼과 선택 상태를 정리한다.
        HideDialogueChoices();

        isChoiceSelected = false;
        selectedChoiceTargetStepId = null;

        for (int i = 0;
       i < dialoguePages.Count;
       i++)
        {
            string pageText =
                dialoguePages[i];

            if (pageText == null)
            {
                pageText =
                    string.Empty;
            }

            // <변경부분> 새로운 Dialogue Element가 시작될 때
            // 이전 페이지의 진행 상태를 완전히 초기화한다.
            isAdvanceRequested =
      false;

            canAcceptAdvanceInput =
                false;

            pageCompletedFrame =
                -1;

            // <변경부분> 새 Dialogue 페이지의 타이핑을 시작하므로
            // 이전 페이지의 ContinueArrow는 즉시 숨긴다.
            HideContinueArrow();

            // <변경부분> 현재 Element의 텍스트를
            // 처음부터 끝까지 타이핑한다.
            yield return
                PlayTypingRoutine(
                    pageText
                );

            if (isDialoguePlaying == false)
            {
                yield break;
            }

            // <변경부분>
            // 타이핑 중 클릭으로 전체 문장을 표시했다면
            // 설정된 시간 동안 다음 페이지 클릭을 무시한다.
            //
            // 이 시점에는 canAcceptAdvanceInput이 false이므로
            // 빠른 연속 클릭으로 다음 대사까지 넘어가지 않는다.
            if (isTypingSkipRequested &&
                typingSkipAdvanceDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    typingSkipAdvanceDelay
                );

                // 대기 중 Dialogue가 종료되었다면
                // 다음 페이지 입력을 활성화하지 않는다.
                if (isDialoguePlaying == false)
                {
                    yield break;
                }
            }

            // <변경부분>
            // 스킵 후 대기가 끝났거나 자연스럽게 타이핑이
            // 완료된 시점의 프레임을 기록한다.
            pageCompletedFrame =
      Time.frameCount;

            // <변경부분>
            // 선택지가 있다면 마지막 페이지 타이핑 완료 후 바로 표시한다.
            // 이 페이지에서는 일반 클릭으로 Dialogue를 종료하지 않는다.
            bool isLastPage =
                i == dialoguePages.Count - 1;

            if (isLastPage &&
                dialogueChoices != null &&
                dialogueChoices.Count > 0)
            {
                HideContinueArrow();

                canAcceptAdvanceInput =
                    false;

                if (!ShowDialogueChoices(
                        dialogueChoices))
                {
                    HideImmediately();
                    yield break;
                }

                while (!isChoiceSelected)
                {
                    if (!isDialoguePlaying)
                    {
                        yield break;
                    }

                    yield return null;
                }

                string targetStepId =
                    selectedChoiceTargetStepId;

                // UI를 정리하기 전에 선택 결과를 Controller로 전달한다.
                onChoiceSelected?.Invoke(
                    targetStepId
                );

                HideImmediately();
                yield break;
            }

            // <변경부분> 여기서부터만 플레이어 클릭을 허용한다.
            //
            // Element 0 타이핑 완료
            // → 클릭 대기
            //
            // Element 1 타이핑 완료
            // → 다시 클릭 대기
            //
            // 마지막 Element도 클릭해야
            // Dialogue Step 자체가 완료된다.
            canAcceptAdvanceInput =
     true;

            // <변경부분> 현재 페이지의 모든 글자가 출력되었고
            // 이제 플레이어의 다음 클릭을 받을 수 있으므로
            // ContinueArrow를 표시하고 깜빡임을 시작한다.
            ShowContinueArrow();

            while (isAdvanceRequested == false)
            {
                if (isDialoguePlaying == false)
                {
                    yield break;
                }

                yield return null;
            }

            // <변경부분> 플레이어가 현재 페이지를 넘겼으므로
            // 다음 페이지가 시작되기 전에 ContinueArrow를 즉시 숨긴다.
            HideContinueArrow();

            // <변경부분> 현재 Element는 플레이어가 직접 넘겼으므로
            // 다음 Element에 이전 클릭 상태가 전달되지 않도록 초기화한다.
            isAdvanceRequested =
                false;

            canAcceptAdvanceInput =
                false;

            // <변경부분> 다음 Element가 같은 프레임에 시작되지 않도록
            // 한 프레임 분리한다.
            yield return null;
        }

        // 모든 페이지를 직접 넘겼을 때만 Dialogue Step 종료
        HideImmediately();
    }

    // <변경부분> TMP maxVisibleCharacters를 사용한
    // 한 페이지 타이핑 연출
    private IEnumerator PlayTypingRoutine(
        string pageText)
    {
        isTyping =
            true;

        isTypingSkipRequested =
            false;

        guideText.text =
            pageText;

        guideText.ForceMeshUpdate();

        int totalCharacterCount =
            guideText.textInfo
                .characterCount;

        guideText.maxVisibleCharacters =
            0;

        if (totalCharacterCount <= 0)
        {
            guideText.maxVisibleCharacters =
                0;

            isTyping =
                false;

            yield break;
        }

        float safeInterval =
            Mathf.Max(
                0.001f,
                characterInterval
            );

        for (int visibleCount = 1;
visibleCount <=
   totalCharacterCount;
visibleCount++)
        {
            // <변경부분>
            // 플레이어가 타이핑 중 클릭했다면
            // 글자별 출력 반복을 즉시 종료한다.
            if (isTypingSkipRequested)
            {
                break;
            }
            // <변경부분> 현재 Dialogue Element는
            // 설정된 속도로 끝까지 타이핑한다.
            //
            // 페이지 이동 입력은 타이핑이 끝난 뒤에만 받는다.
            guideText.maxVisibleCharacters =
                visibleCount;

            float elapsedTime =
                0f;

            // <변경부분>
            // 글자 출력 간격을 기다리는 도중에도
            // 스킵 입력을 받으면 즉시 빠져나온다.
            while (elapsedTime < safeInterval &&
                   isTypingSkipRequested == false)
            {
                elapsedTime +=
                    useUnscaledTime
                        ? Time.unscaledDeltaTime
                        : Time.deltaTime;

                yield return null;
            }
        }

        // <변경부분> 현재 Element의 타이핑이 완료되면
        // 모든 글자가 보이는 상태를 확실하게 유지한다.
        guideText.maxVisibleCharacters =
            totalCharacterCount;

        isTyping =
            false;
    }

    // <변경부분> 현재 Dialogue 페이지의 출력이 완료되어
    // 플레이어 입력을 기다리는 동안 ContinueArrow를 표시한다.
    private void ShowContinueArrow()
    {
        if (continueArrowImage == null)
        {
            return;
        }

        // 이전 깜빡임 Coroutine이 남아 있다면
        // 중복 실행되지 않도록 먼저 종료한다.
        if (continueArrowBlinkCoroutine != null)
        {
            StopCoroutine(
                continueArrowBlinkCoroutine
            );

            continueArrowBlinkCoroutine =
                null;
        }

        continueArrowImage.gameObject.SetActive(
            true
        );

        SetContinueArrowAlpha(
            1f
        );

        continueArrowBlinkCoroutine =
            StartCoroutine(
                ContinueArrowBlinkRoutine()
            );
    }


    // <변경부분> 다음 페이지 시작 또는 Dialogue 종료 시
    // ContinueArrow와 깜빡임 Coroutine을 즉시 정리한다.
    private void HideContinueArrow()
    {
        if (continueArrowBlinkCoroutine != null)
        {
            StopCoroutine(
                continueArrowBlinkCoroutine
            );

            continueArrowBlinkCoroutine =
                null;
        }

        if (continueArrowImage == null)
        {
            return;
        }

        // 다음 표시 때 항상 완전히 밝은 상태에서 시작하도록 복원한다.
        SetContinueArrowAlpha(
            1f
        );

        continueArrowImage.gameObject.SetActive(
            false
        );
    }


    // <변경부분> ContinueArrow Image의 Alpha만 변경한다.
    //
    // Sprite 색상 자체는 유지하고
    // 투명도만 변경하여 깜빡임을 만든다.
    private void SetContinueArrowAlpha(
        float alpha)
    {
        if (continueArrowImage == null)
        {
            return;
        }

        Color arrowColor =
            continueArrowImage.color;

        arrowColor.a =
            Mathf.Clamp01(
                alpha
            );

        continueArrowImage.color =
            arrowColor;
    }


    // <변경부분> Dialogue 페이지 출력 완료 후
    // 플레이어가 다음 클릭을 할 때까지
    // ContinueArrow의 Alpha를 반복해서 변화시킨다.
    //
    // Time.timeScale의 영향을 받지 않도록
    // Dialogue 설정에 맞춰 unscaledDeltaTime도 지원한다.
    private IEnumerator ContinueArrowBlinkRoutine()
    {
        if (continueArrowImage == null)
        {
            continueArrowBlinkCoroutine =
                null;

            yield break;
        }

        float safeFadeDuration =
            Mathf.Max(
                0.05f,
                continueArrowFadeDuration
            );

        float safeMinAlpha =
            Mathf.Clamp01(
                continueArrowMinAlpha
            );

        while (
            isDialoguePlaying &&
            isTyping == false &&
            canAcceptAdvanceInput
        )
        {
            // 밝은 상태 → 흐린 상태
            float elapsedTime =
                0f;

            while (elapsedTime <
                   safeFadeDuration)
            {
                if (isDialoguePlaying == false ||
                    isTyping ||
                    canAcceptAdvanceInput == false)
                {
                    continueArrowBlinkCoroutine =
                        null;

                    yield break;
                }

                elapsedTime +=
                    useUnscaledTime
                        ? Time.unscaledDeltaTime
                        : Time.deltaTime;

                float normalizedTime =
                    Mathf.Clamp01(
                        elapsedTime /
                        safeFadeDuration
                    );

                SetContinueArrowAlpha(
                    Mathf.Lerp(
                        1f,
                        safeMinAlpha,
                        normalizedTime
                    )
                );

                yield return null;
            }

            // 흐린 상태 → 밝은 상태
            elapsedTime =
                0f;

            while (elapsedTime <
                   safeFadeDuration)
            {
                if (isDialoguePlaying == false ||
                    isTyping ||
                    canAcceptAdvanceInput == false)
                {
                    continueArrowBlinkCoroutine =
                        null;

                    yield break;
                }

                elapsedTime +=
                    useUnscaledTime
                        ? Time.unscaledDeltaTime
                        : Time.deltaTime;

                float normalizedTime =
                    Mathf.Clamp01(
                        elapsedTime /
                        safeFadeDuration
                    );

                SetContinueArrowAlpha(
                    Mathf.Lerp(
                        safeMinAlpha,
                        1f,
                        normalizedTime
                    )
                );

                yield return null;
            }
        }

        continueArrowBlinkCoroutine =
    null;
    }


    // <변경부분>
    // SelectPanelRoot 아래에 SelectDialoguePanel 프리팹을 자동 생성한다.
    // 선택지 수 / 표시 텍스트 / 버튼 콜백은 Dialogue 데이터에서 가져온다.
    private bool ShowDialogueChoices(
        List<EventSceneDialogueChoiceData> dialogueChoices)
    {
        HideDialogueChoices();

        if (selectPanelRoot == null ||
            selectDialoguePanelPrefab == null)
        {
            Debug.LogError(
                "Event Guide 선택지 표시 실패: " +
                "Select Panel Root / Select Dialogue Panel Prefab 연결을 확인하세요.",
                this
            );

            return false;
        }

        for (int i = 0;
             i < dialogueChoices.Count;
             i++)
        {
            EventSceneDialogueChoiceData choice =
                dialogueChoices[i];

            if (choice == null ||
                string.IsNullOrWhiteSpace(
                    choice.targetStepId))
            {
                Debug.LogError(
                    $"Event Guide 선택지 표시 실패: " +
                    $"Choice {i + 1}의 Target Step이 비어 있습니다.",
                    this
                );

                HideDialogueChoices();
                return false;
            }

            Button choiceButton =
                Instantiate(
                    selectDialoguePanelPrefab,
                    selectPanelRoot.transform
                );

            choiceButton.gameObject.SetActive(true);
            choiceButton.interactable = true;

            TMP_Text choiceText =
                choiceButton
                    .GetComponentInChildren<TMP_Text>(
                        true
                    );

            if (choiceText == null)
            {
                Debug.LogError(
                    "Event Guide 선택지 표시 실패: " +
                    "SelectDialoguePanel 프리팹에서 TMP_Text를 찾지 못했습니다.",
                    choiceButton
                );

                createdChoiceButtons.Add(choiceButton);

                HideDialogueChoices();
                return false;
            }

            // <변경부분>
            // EventScene_Dialogue 번역값을 사용한다.
            // 번역이 없으면 기존 Korean 원문으로 Fallback.
            choiceText.text =
                choice.GetLocalizedText();

            // 선택지마다 다른 Target Step ID를 캡처한다.
            string capturedTargetStepId =
                choice.targetStepId;

            // <변경부분>
            // 실제 클릭된 버튼에서 잔상 확대를 실행하기 위해
            // 생성된 버튼 인스턴스도 함께 캡처한다.
            Button capturedChoiceButton =
                choiceButton;

            // 동적으로 생성한 인스턴스의 런타임 리스너만 초기화한다.
            // 프리팹 Inspector의 Persistent OnClick은 비워 두어야 한다.
            choiceButton.onClick.RemoveAllListeners();

            choiceButton.onClick.AddListener(
                () => SelectDialogueChoice(
                    capturedTargetStepId,
                    capturedChoiceButton
                )
            );

            createdChoiceButtons.Add(
                choiceButton
            );
        }

        // <변경부분>
        // GuideRoot의 화면 전체 InputBlocker보다 위에 그려지도록 한다.
        selectPanelRoot.transform.SetAsLastSibling();

        CanvasGroup group =
            selectPanelRoot
                .GetComponent<CanvasGroup>();

        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        isChoiceSelected = false;
        selectedChoiceTargetStepId = null;
        isChoiceSelectionActive = true;

        // <변경부분>
        // 생성된 모든 선택지를 먼저 활성화한다.
        selectPanelRoot.SetActive(true);

        // <변경부분>
        // 기존 공용 PopupOpenAnimator를 재사용한다.
        //
        // SelectPanelRoot에 PopupOpenAnimator를 추가하고
        // PopupOpenAnimationData를 지정하면
        // 선택지 전체에 팝업 글리치 효과가 적용된다.
        //
        // 컴포넌트가 없더라도 선택지 기능은 정상 작동한다.
        PopupOpenAnimator choiceOpenAnimator =
            selectPanelRoot
                .GetComponent<PopupOpenAnimator>();

        if (choiceOpenAnimator != null)
        {
            choiceOpenAnimator.PlayOpen();
        }

        return true;
    }


    // <변경부분>
    // <변경부분>
    // 선택지 클릭을 한 번만 접수하고
    // 클릭 피드백 애니메이션을 시작한다.
    //
    // 실제 Target Step 확정은
    // 애니메이션 종료 후에 수행한다.
    private void SelectDialogueChoice(
        string targetStepId,
        Button clickedButton)
    {
        if (!isDialoguePlaying ||
            !isChoiceSelectionActive ||
            isChoiceSelected ||
            isChoiceClickAnimating)
        {
            return;
        }

        // <변경부분>
        // 클릭 효과가 실행되는 동안
        // 다른 버튼을 누를 수 없도록 즉시 잠근다.
        isChoiceClickAnimating = true;
        isChoiceSelectionActive = false;

        for (int i = 0;
             i < createdChoiceButtons.Count;
             i++)
        {
            if (createdChoiceButtons[i] != null)
            {
                createdChoiceButtons[i].interactable =
                    false;
            }
        }

        // <변경부분>
        // 클릭된 버튼만 확대 잔상을 재생한다.
        StartCoroutine(
            PlayChoiceClickFeedbackRoutine(
                clickedButton,
                targetStepId
            )
        );
    }


    // <변경부분>
    // 선택지 버튼 클릭 확대 / 잔상 피드백.
    //
    // 1. 선택한 버튼의 배경 Image를 복제한다.
    // 2. 복제 이미지를 확대하면서 Alpha를 줄인다.
    // 3. 원본 버튼에도 작은 Scale 펄스를 적용한다.
    // 4. 효과가 끝난 뒤 Target Step을 확정한다.
    //
    // 기존 버튼 프리팹의 Image / TMP_Text /
    // Button 컴포넌트 설정은 변경하지 않는다.
    private IEnumerator PlayChoiceClickFeedbackRoutine(
        Button clickedButton,
        string targetStepId)
    {
        if (clickedButton == null)
        {
            selectedChoiceTargetStepId =
                targetStepId;

            isChoiceSelected = true;
            isChoiceClickAnimating = false;

            yield break;
        }

        RectTransform buttonRect =
            clickedButton.transform as RectTransform;

        Vector3 originalButtonScale =
            buttonRect != null
                ? buttonRect.localScale
                : Vector3.one;

        // <변경부분>
        // Button의 Target Graphic을 우선 사용한다.
        //
        // Button.targetGraphic이 TMP_Text인 경우 등에는
        // 버튼 자체의 Image를 대신 사용한다.
        Image sourceImage =
            clickedButton.targetGraphic as Image;

        if (sourceImage == null)
        {
            sourceImage =
                clickedButton.GetComponent<Image>();
        }

        if (sourceImage == null)
        {
            sourceImage =
                clickedButton
                    .GetComponentInChildren<Image>(true);
        }

        GameObject ghostObject = null;
        RectTransform ghostRect = null;
        Image ghostImage = null;

        Color originalGhostColor =
            Color.white;

        // =================================================
        // Afterimage 생성
        // =================================================

        if (sourceImage != null)
        {
            // <변경부분>
            // LayoutGroup의 직접 자식이 아니라
            // 버튼 이미지 내부에 잔상을 생성한다.
            //
            // 따라서 잔상 확대 중에도
            // 다른 선택지 버튼의 레이아웃이 움직이지 않는다.
            ghostObject =
                new GameObject(
                    "ChoiceClickAfterimage",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            ghostRect =
                ghostObject
                    .GetComponent<RectTransform>();

            ghostRect.SetParent(
                sourceImage.transform,
                false
            );

            ghostRect.anchorMin =
                Vector2.zero;

            ghostRect.anchorMax =
                Vector2.one;

            ghostRect.offsetMin =
                Vector2.zero;

            ghostRect.offsetMax =
                Vector2.zero;

            ghostRect.pivot =
                new Vector2(0.5f, 0.5f);

            ghostRect.localScale =
                Vector3.one;

            // 원본 텍스트보다 뒤에 표시한다.
            ghostRect.SetAsFirstSibling();

            ghostImage =
                ghostObject.GetComponent<Image>();

            ghostImage.sprite =
                sourceImage.sprite;

            ghostImage.type =
                sourceImage.type;

            ghostImage.preserveAspect =
                sourceImage.preserveAspect;

            ghostImage.fillCenter =
                sourceImage.fillCenter;

            ghostImage.raycastTarget =
                false;

            // <변경부분>
            // 기존 Image 색상을 유지하되
            // 잔상은 반투명 상태에서 시작한다.
            originalGhostColor =
                sourceImage.color;

            originalGhostColor.a *=
                choiceClickGhostAlpha;

            ghostImage.color =
                originalGhostColor;
        }

        // =================================================
        // Click Animation
        // =================================================

        float safeDuration =
            Mathf.Max(
                0.01f,
                choiceClickFeedbackDuration
            );

        float elapsedTime =
            0f;

        while (elapsedTime < safeDuration &&
               isDialoguePlaying)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsedTime / safeDuration
                );

            // <변경부분>
            // 초반에 조금 빠르게 확대되고
            // 마지막에 부드럽게 감속한다.
            float easedT =
                1f - (1f - t) * (1f - t);

            // =============================================
            // Afterimage Expansion
            // =============================================

            if (ghostRect != null)
            {
                float ghostScale =
                    Mathf.Lerp(
                        1f,
                        choiceClickGhostEndScale,
                        easedT
                    );

                ghostRect.localScale =
                    Vector3.one * ghostScale;

                Color ghostColor =
                    originalGhostColor;

                ghostColor.a =
                    originalGhostColor.a *
                    (1f - t);

                ghostImage.color =
                    ghostColor;
            }

            // =============================================
            // Original Button Pulse
            // =============================================

            if (buttonRect != null)
            {
                // 1 → 1.025 → 1
                // 원본 버튼은 아주 조금만 확대한다.
                float pulse =
                    1f +
                    Mathf.Sin(Mathf.PI * t) * 0.025f;

                buttonRect.localScale =
                    originalButtonScale * pulse;
            }

            yield return null;
        }

        // =================================================
        // Cleanup
        // =================================================

        if (buttonRect != null)
        {
            buttonRect.localScale =
                originalButtonScale;
        }

        if (ghostObject != null)
        {
            Destroy(ghostObject);
        }

        // <변경부분>
        // Dialogue가 외부에서 강제 종료되었다면
        // 선택 결과를 확정하지 않는다.
        if (!isDialoguePlaying)
        {
            isChoiceClickAnimating = false;
            yield break;
        }

        // <변경부분>
        // 클릭 피드백이 완전히 끝난 뒤에만
        // 기존 Dialogue 선택 완료 플래그를 올린다.
        //
        // PlayDialogueRoutine이 이 값을 감지해
        // 기존 EventSceneSequenceController에
        // Target Step ID를 전달한다.
        selectedChoiceTargetStepId =
            targetStepId;

        isChoiceSelected =
            true;

        isChoiceClickAnimating =
            false;
    }


    // <변경부분>
    // 생성한 선택지 버튼만 제거하여 SelectPanelRoot의
    // CanvasGroup / LayoutGroup / 배경 설정은 보존한다.
    private void HideDialogueChoices()
    {
        isChoiceSelectionActive =
            false;

        // <변경부분>
        // 선택지 전체를 정리할 때
        // 클릭 애니메이션 상태도 초기화한다.
        isChoiceClickAnimating =
            false;

        if (selectPanelRoot != null)
        {
            selectPanelRoot.SetActive(false);
        }

        for (int i = 0;
             i < createdChoiceButtons.Count;
             i++)
        {
            Button button =
                createdChoiceButtons[i];

            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();

            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }

        createdChoiceButtons.Clear();
    }


    // <변경부분> Dialogue UI를 즉시 종료하고
    // 모든 입력 상태를 초기화한다.
    public void HideImmediately()
    {
        HideDialogueChoices();
        ResetDialogueState();

        if (guideText != null)
        {
            guideText.text =
                string.Empty;

            guideText.maxVisibleCharacters =
                int.MaxValue;
        }

        if (guideRoot != null)
        {
            guideRoot.SetActive(
                false
            );
        }
    }

    // <변경부분> Dialogue 관련 런타임 상태를
    // 한 곳에서 초기화한다.
    private void ResetDialogueState()
    {
        isDialoguePlaying =
            false;

        isTyping =
            false;

        isTypingSkipRequested =
            false;

        isAdvanceRequested =
            false;

        canAcceptAdvanceInput =
            false;

        pageCompletedFrame =
       -1;

        isChoiceSelected =
            false;

        selectedChoiceTargetStepId =
            null;

        // <변경부분> Dialogue가 정상 종료되거나
        // Scene / GameObject 비활성화 등으로 강제 종료되어도
        // ContinueArrow가 화면에 남지 않도록 함께 정리한다.
        HideContinueArrow();
    }

    private void OnDisable()
    {
        HideDialogueChoices();
        ResetDialogueState();
    }
}