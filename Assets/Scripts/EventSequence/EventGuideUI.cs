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

    [Header("Typing Animation")]
    // 글자 한 글자가 나타나는 간격
    [SerializeField, Min(0.001f)]
    private float characterInterval =
        0.03f;

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
    }

    private void Update()
    {
        if (isDialoguePlaying == false)
        {
            return;
        }

        // <변경부분> 현재 페이지의 타이핑이 끝나기 전에는
        // 클릭을 페이지 진행 입력으로 사용하지 않는다.
        //
        // Element 하나의 문장이 모두 출력된 뒤
        // 반드시 새 클릭이 들어와야 다음 Element로 진행한다.
        if (isTyping ||
            canAcceptAdvanceInput == false)
        {
            return;
        }

        if (WasDialogueAdvancePressed() == false)
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

    // <변경부분> 전달받은 Dialogue 페이지를
    // 플레이어 입력에 따라 순서대로 표시한다.
    public IEnumerator PlayDialogueRoutine(
        List<string> dialoguePages)
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

            // <변경부분> 현재 Element의 모든 글자가 출력된 프레임을 기록한다.
            pageCompletedFrame =
                Time.frameCount;

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

            while (isAdvanceRequested == false)
            {
                if (isDialoguePlaying == false)
                {
                    yield break;
                }

                yield return null;
            }

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
            // <변경부분> 현재 Dialogue Element는
            // 설정된 속도로 끝까지 타이핑한다.
            //
            // 페이지 이동 입력은 타이핑이 끝난 뒤에만 받는다.
            guideText.maxVisibleCharacters =
                visibleCount;

            float elapsedTime =
                0f;

            while (elapsedTime <
                   safeInterval)
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

    // <변경부분> Dialogue UI를 즉시 종료하고
    // 모든 입력 상태를 초기화한다.
    public void HideImmediately()
    {
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
    }

    private void OnDisable()
    {
        ResetDialogueState();
    }
}