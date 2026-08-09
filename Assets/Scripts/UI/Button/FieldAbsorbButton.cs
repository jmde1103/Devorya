using System;
using UnityEngine;
using UnityEngine.UI;

// 상대 기물 위에 표시되는 필드 흡수 버튼을 관리한다.
//
// 공격 가능한 상대 기물을 첫 번째로 선택했을 때 표시되고,
// 버튼을 누르면 BattleManager에 현재 대상 흡수를 요청한다.
public class FieldAbsorbButton : MonoBehaviour
{
    [Header("Button")]
    // 실제 클릭 입력을 받는 버튼
    [SerializeField]
    private Button absorbButton;

    [Header("Open Animation")]
    // 버튼이 나타날 때 기존 팝업 오픈 애니메이션을 재생한다.
    [SerializeField]
    private PopupOpenAnimator popupOpenAnimator;

    [Header("Click Animation")]
    // 버튼 클릭 시 기존 아이콘 노이즈 애니메이션을 재생한다.
    [SerializeField]
    private UIButtonNoiseAnimator buttonNoiseAnimator;

    // 현재 버튼을 눌렀을 때 실행할 콜백
    private Action onClickAction;

    // 현재 버튼이 표시 중인지 확인한다.
    public bool IsVisible
    {
        get
        {
            return
                gameObject.activeSelf;
        }
    }

    private void Awake()
    {
        AutoBindReferences();

        // 전투 시작 시 필드 흡수 버튼은 숨긴다.
        gameObject.SetActive(
            false
        );
    }

    private void OnEnable()
    {
        AutoBindReferences();

        if (absorbButton != null)
        {
            absorbButton.onClick.RemoveListener(
                HandleButtonClicked
            );

            absorbButton.onClick.AddListener(
                HandleButtonClicked
            );
        }
    }

    private void OnDisable()
    {
        if (absorbButton != null)
        {
            absorbButton.onClick.RemoveListener(
                HandleButtonClicked
            );
        }

        onClickAction =
            null;
    }

    // Inspector 연결이 빠진 참조를 자동으로 찾는다.
    private void AutoBindReferences()
    {
        if (absorbButton == null)
        {
            absorbButton =
                GetComponentInChildren<Button>(
                    true
                );
        }

        if (popupOpenAnimator == null)
        {
            popupOpenAnimator =
                GetComponent<PopupOpenAnimator>();
        }

        if (buttonNoiseAnimator == null)
        {
            buttonNoiseAnimator =
                GetComponentInChildren<UIButtonNoiseAnimator>(
                    true
                );
        }
    }

    // 필드 흡수 버튼을 표시하고
    // 이번 대상에 사용할 클릭 콜백을 연결한다.
    public void Show(
        Action clickAction)
    {
        AutoBindReferences();

        onClickAction =
            clickAction;

        gameObject.SetActive(
            true
        );

        if (absorbButton != null)
        {
            absorbButton.interactable =
                true;
        }

        // 기존 팝업 오픈 애니메이션을 그대로 재생한다.
        if (popupOpenAnimator != null)
        {
            popupOpenAnimator.PlayOpen();
        }
    }

    // 버튼과 현재 클릭 콜백을 즉시 초기화한다.
    public void Hide()
    {
        onClickAction =
            null;

        if (absorbButton != null)
        {
            absorbButton.interactable =
                false;
        }

        gameObject.SetActive(
            false
        );
    }

    // 실제 버튼 클릭 처리
    private void HandleButtonClicked()
    {
        if (absorbButton != null)
        {
            // 같은 프레임의 중복 클릭을 막는다.
            absorbButton.interactable =
                false;
        }

        // 기존 UIButtonNoiseAnimator는 Button의
        // OnClick에도 연결되므로 그대로 재사용할 수 있다.
        if (buttonNoiseAnimator != null)
        {
            buttonNoiseAnimator.PlayNoise();
        }

        Action cachedClickAction =
            onClickAction;

        onClickAction =
            null;

        cachedClickAction?.Invoke();
    }
}
