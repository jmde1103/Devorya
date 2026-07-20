using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// <변경부분> Tooltip을 표시할 위치 계산 방식을 구분한다.
public enum TooltipPositionMode
{
    // 누른 위치를 기준으로 공통 Offset과 개별 Offset을 적용한다.
    PointerOffset,

    // 누른 위치를 무시하고 Canvas 기준 고정 좌표에 표시한다.
    FixedCanvasPosition
}

// <변경부분> UI 아이콘/버튼을 꾹 눌렀을 때 TooltipPopupUI를 표시하는 트리거
public class TooltipTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Tooltip")]
    // <변경부분> 흡수 버튼처럼 별도 Data가 없는 UI에서 사용할 고정 Tooltip 에셋
    [SerializeField] private TooltipData tooltipData;

    // <변경부분> 스킬/아이템/상태효과 데이터에서 런타임으로 만든 표시용 Tooltip 데이터
    private TooltipViewData tooltipViewData;

    [Header("Input")]
    [SerializeField] private float holdDelay = 0.3f;

    [Header("Position")]
    // <변경부분> 이 TooltipTrigger가 사용할 위치 계산 방식
    [SerializeField]
    private TooltipPositionMode positionMode =
    TooltipPositionMode.PointerOffset;

    // <변경부분> PointerOffset 모드에서
    // 기존 자동 위치에 추가할 개별 위치 보정값
    // X 양수는 오른쪽, Y 양수는 위쪽으로 이동한다.
    [SerializeField]
    private Vector2 customPositionOffset;

    // <변경부분> FixedCanvasPosition 모드에서 사용할
    // Root Canvas 기준 고정 Anchored Position
    [SerializeField]
    private Vector2 fixedCanvasPosition;

    private Coroutine holdCoroutine;
    private bool isHolding;

    // <변경부분> Tooltip이 실제로 표시된 상태인지 확인
    // 팝업이 뜬 뒤 PointerExit가 발생해도 즉시 꺼지지 않게 막기 위한 플래그
    private bool isTooltipVisible;

    // <변경부분> 이번 누르기 입력 중 Tooltip이 한 번이라도 표시되었는지 기록
    // Tooltip을 보기 위한 Long Press가 Button Click으로 처리되는 문제를 막기 위해 사용
    private bool wasTooltipShownDuringPress;

    // <변경부분> Long Press 이후 발생하는 Button Click을 한 프레임 동안 차단하기 위한 플래그
    private bool blockNextClick;

    // <변경부분> TooltipTrigger가 붙은 오브젝트 또는 부모에 있는 Button을 임시로 비활성화해서 onClick 실행을 막음
    private Button targetButton;

    // <변경부분> Button의 기존 interactable 상태를 복구하기 위해 저장
    private bool cachedButtonInteractable;


    // <변경부분> TooltipTrigger가 붙은 오브젝트 또는 부모에서 Button을 찾아둔다.
    // 아이콘 자식에 TooltipTrigger가 붙어 있어도 부모 Button을 차단할 수 있게 하기 위함.
    private void Awake()
    {
        targetButton = GetComponent<Button>();

        if (targetButton == null)
        {
            targetButton = GetComponentInParent<Button>();
        }
    }

    // <변경부분> 별도 TooltipData 에셋을 연결
    public void SetTooltipData(TooltipData newTooltipData)
    {
        tooltipData = newTooltipData;
        tooltipViewData = TooltipViewData.FromTooltipData(newTooltipData);
    }

    // <변경부분> 기존 SkillData / ItemData / StatusEffectData에서 만든 TooltipViewData를 연결
    public void SetTooltipViewData(TooltipViewData newTooltipViewData)
    {
        tooltipViewData = newTooltipViewData;

        // 런타임 데이터가 들어오면 고정 TooltipData 에셋은 사용하지 않음
        if (newTooltipViewData != null)
        {
            tooltipData = null;
        }
    }

    // <변경부분> 포인터를 누르기 시작하면 꾹 누름 대기 시작
    // <변경부분> 포인터 입력이 TooltipTrigger까지 실제로 도달하는지 확인
    public void OnPointerDown(
        PointerEventData eventData)
    {

        isHolding = true;
        isTooltipVisible = false;
        wasTooltipShownDuringPress = false;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
        }

        holdCoroutine = StartCoroutine(
            ShowTooltipAfterDelay(
                eventData.position
            )
        );
    }
    // <변경부분> 포인터를 떼면 팝업 숨김
    public void OnPointerUp(PointerEventData eventData)
    {
        // <변경부분> Tooltip이 표시된 상태에서 손을 뗀 경우,
        // 이번 입력은 Tooltip 확인용 Long Press였으므로 Button Click으로 이어지지 않게 차단
        if (wasTooltipShownDuringPress)
        {
            blockNextClick = true;

            eventData.eligibleForClick = false;
            eventData.Use();

            // <변경부분> Unity Button.onClick은 PointerUp 이후 PointerClick 단계에서 실행되므로,
            // 현재 프레임 동안 Button을 임시 비활성화해서 스킬 사용을 확실히 막는다.
            TemporarilyDisableButtonClick();
        }

        StopHold();
    }

    // <변경부분> Button.onClick이 실행되는 PointerClick 단계를 막기 위해 Button을 한 프레임 동안 비활성화
    private void TemporarilyDisableButtonClick()
    {
        if (targetButton == null)
        {
            return;
        }

        cachedButtonInteractable = targetButton.interactable;
        targetButton.interactable = false;

        StartCoroutine(RestoreButtonClickNextFrame());
    }

    // <변경부분> 현재 프레임이 끝난 뒤 Button 상태와 클릭 차단 플래그를 복구
    private IEnumerator RestoreButtonClickNextFrame()
    {
        yield return null;

        if (targetButton != null)
        {
            targetButton.interactable = cachedButtonInteractable;
        }

        blockNextClick = false;
        wasTooltipShownDuringPress = false;
    }

    // <변경부분> Long Press로 Tooltip을 본 경우 같은 입력에서 발생하는 PointerClick을 소비한다.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (blockNextClick)
        {
            eventData.eligibleForClick = false;
            eventData.Use();
        }
    }

    // <변경부분> 누른 상태로 아이콘 밖으로 나가면 팝업 숨김
    public void OnPointerExit(PointerEventData eventData)
    {
        // <변경부분> 아직 Tooltip이 뜨기 전이면 꾹 누르기 대기만 취소
        if (isTooltipVisible == false)
        {
            StopHold();
            return;
        }

        // <변경부분> Tooltip이 이미 뜬 뒤에는 PointerExit로 바로 끄지 않음
        // 팝업 UI가 레이캐스트를 가로채면서 순간적으로 PointerExit가 발생하는 문제 방지
    }

    // <변경부분> 지정 시간 이상 누르고 있으면 Tooltip을 표시한다.
    private IEnumerator ShowTooltipAfterDelay(
        Vector2 screenPosition)
    {
        // Time.timeScale의 영향을 받지 않는 실제 시간 기준 Long Press
        yield return new WaitForSecondsRealtime(
            holdDelay
        );

        // 대기 도중 손을 뗐거나 포인터가 벗어났으면 표시하지 않는다.
        if (isHolding == false)
        {
            yield break;
        }

        // 고정 TooltipData만 연결된 경우 표시용 런타임 데이터 생성
        if (tooltipViewData == null &&
            tooltipData != null)
        {
            tooltipViewData =
                TooltipViewData.FromTooltipData(
                    tooltipData
                );
        }

        if (tooltipViewData == null)
        {
            Debug.LogWarning(
                $"툴팁 표시 실패: " +
                $"{gameObject.name}에 TooltipViewData가 없습니다."
            );

            yield break;
        }

        if (TooltipPopupUI.Instance == null)
        {
            Debug.LogWarning(
                $"툴팁 표시 실패: " +
                $"TooltipPopupUI.Instance가 없습니다. " +
                $"대상 오브젝트: {gameObject.name}"
            );

            yield break;
        }

        // <변경부분> 이 TooltipTrigger의 위치 모드와
        // 개별 Offset 또는 고정 Canvas 위치를 함께 전달한다.
        TooltipPopupUI.Instance.Show(
            tooltipViewData,
            screenPosition,
            positionMode,
            customPositionOffset,
            fixedCanvasPosition
        );

        isTooltipVisible = true;
        wasTooltipShownDuringPress = true;
    }

    // <변경부분> 꾹 누름 상태를 종료하고 팝업을 숨김
    private void StopHold()
    {
        isHolding = false;
        isTooltipVisible = false;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        if (TooltipPopupUI.Instance != null)
        {
            TooltipPopupUI.Instance.Hide();
        }
    }
}