using System.Collections;
using TMPro;
using UnityEngine;


// <변경부분>
// Standalone EventScene에서 사용하는
// 상점 구매 실패 안내 팝업.
//
// 기존 SkillFailurePopup의 UI 외형과
// PopupOpenAnimator를 그대로 재사용한다.
//
// 역할:
// 1. 실패 안내 문구 표시
// 2. 기존 글리치 오픈 애니메이션 실행
// 3. 일정 시간 메시지 유지
// 4. CanvasGroup Fade Out
// 5. 연속 호출 시 이전 표시 작업 안전하게 중단
//
// 전투용 BattleUIController와는 독립적으로 동작한다.
[RequireComponent(typeof(CanvasGroup))]
public class ShopFailurePopupUI : MonoBehaviour
{
    [Header("UI References")]

    // 실패 안내 문구를 표시할 기존 TMP Text.
    [SerializeField]
    private TMP_Text messageText;

    // 팝업 전체의 표시 및 Fade Out을 제어한다.
    [SerializeField]
    private CanvasGroup popupCanvasGroup;

    // 기존 글리치 오픈 애니메이션 컴포넌트.
    [SerializeField]
    private PopupOpenAnimator popupOpenAnimator;


    [Header("Display Timing")]

    // 오픈 애니메이션 완료 후 메시지 유지 시간.
    [SerializeField, Min(0f)]
    private float holdDuration = 0.75f;

    // 메시지가 사라지는 Fade Out 시간.
    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.25f;

    // 애니메이션 데이터가 잘못되어 종료되지 않는 상황을 방지한다.
    [SerializeField, Min(0.1f)]
    private float maximumOpenWaitDuration = 3f;


    // 현재 메시지를 표시 중인 Coroutine.
    private Coroutine displayCoroutine;


    private void Awake()
    {
        CacheReferences();

        HideVisualImmediately();
    }


    private void OnEnable()
    {
        CacheReferences();

        // Scene 활성화 시 이전 메시지가 남아 있지 않도록 한다.
        HideVisualImmediately();
    }


    private void OnDisable()
    {
        // Scene 종료 또는 Shop UI 정리 중
        // 실행 중인 Coroutine을 안전하게 종료한다.
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        HideVisualImmediately();
    }


    // <변경부분>
    // Inspector 참조가 빠졌을 경우 같은 오브젝트와
    // 자식에서 기존 컴포넌트를 찾는다.
    private void CacheReferences()
    {
        if (popupCanvasGroup == null)
        {
            popupCanvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (popupOpenAnimator == null)
        {
            popupOpenAnimator =
                GetComponent<PopupOpenAnimator>();
        }

        if (messageText == null)
        {
            messageText =
                GetComponentInChildren<TMP_Text>(
                    true
                );
        }
    }


    // <변경부분>
    // 외부 ShopController에서 실패 메시지를 전달받는다.
    //
    // 기존 팝업이 실행 중이더라도 새로운 메시지로 교체하고
    // 글리치 애니메이션을 처음부터 다시 재생한다.
    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (isActiveAndEnabled == false ||
            gameObject.activeInHierarchy == false)
        {
            Debug.LogWarning(
                "[ShopFailurePopupUI] " +
                "팝업 GameObject가 비활성화되어 있습니다.",
                this
            );

            return;
        }

        CacheReferences();

        if (messageText == null ||
            popupCanvasGroup == null)
        {
            Debug.LogWarning(
                "[ShopFailurePopupUI] " +
                "TMP Text 또는 CanvasGroup 연결이 필요합니다.",
                this
            );

            return;
        }

        // 기존 유지 / Fade Out Coroutine이 남아 있다면 종료.
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        // 새 메시지 표시 준비.
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        popupCanvasGroup.alpha = 1f;
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = false;

        // <변경부분>
        // 기존 PopupOpenAnimator가 이전 오픈 Coroutine을
        // 자체적으로 중단하고 새 애니메이션을 시작한다.
        if (popupOpenAnimator != null)
        {
            popupOpenAnimator.PlayOpen();
        }

        displayCoroutine =
            StartCoroutine(
                DisplayRoutine()
            );
    }


    // <변경부분>
    // 글리치 오픈이 끝난 뒤
    // Hold → Fade Out 순서로 메시지를 정리한다.
    private IEnumerator DisplayRoutine()
    {
        float openWaitElapsed = 0f;

        // 기존 PopupOpenAnimator와 Fade Out이
        // 동시에 CanvasGroup Alpha를 수정하지 않도록 한다.
        while (popupOpenAnimator != null &&
               popupOpenAnimator.IsPlaying &&
               openWaitElapsed < maximumOpenWaitDuration)
        {
            openWaitElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 애니메이션이 제한 시간 내 끝나지 않았다면
        // 안전하게 최종 상태로 마무리한다.
        if (popupOpenAnimator != null &&
            popupOpenAnimator.IsPlaying)
        {
            popupOpenAnimator.CompleteImmediately();
        }

        popupCanvasGroup.alpha = 1f;
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = false;


        // 메시지 유지.
        float holdElapsed = 0f;

        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }


        // Fade Out.
        float fadeElapsed = 0f;

        float safeFadeDuration =
            Mathf.Max(
                0.01f,
                fadeDuration
            );

        while (fadeElapsed < safeFadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;

            float fadeRate =
                Mathf.Clamp01(
                    fadeElapsed / safeFadeDuration
                );

            popupCanvasGroup.alpha =
                Mathf.Lerp(
                    1f,
                    0f,
                    fadeRate
                );

            yield return null;
        }

        HideVisualImmediately();

        displayCoroutine = null;
    }


    // <변경부분>
    // 팝업 오브젝트는 활성화 상태로 유지하면서
    // 화면과 입력에서만 완전히 숨긴다.
    private void HideVisualImmediately()
    {
        if (messageText != null)
        {
            messageText.text = string.Empty;
            messageText.gameObject.SetActive(false);
        }

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.interactable = false;
            popupCanvasGroup.blocksRaycasts = false;
        }
    }
}