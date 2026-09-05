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
// <변경부분> PC에서는 PointerEnter Hover로 Tooltip을 열고,
// 모바일에서는 기존 Long Press 입력을 유지한다.
public class TooltipTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Tooltip")]
    // <변경부분> 흡수 버튼처럼 별도 Data가 없는 UI에서 사용할 고정 Tooltip 에셋
    [SerializeField] private TooltipData tooltipData;

    // <변경부분> 스킬/아이템/상태효과 데이터에서 런타임으로 만든 표시용 Tooltip 데이터
    private TooltipViewData tooltipViewData;

    [Header("Input")]

    // Tooltip 입력 시간은 모든 TooltipTrigger가
    // 동일한 공용값을 사용한다.
    //
    // Inspector 개별 설정을 사용하지 않으므로
    // 기존 Scene / Prefab에 저장된 SerializeField 값의 영향을 받지 않는다.
    //
    // Mobile Long Press
    private const float HoldDelay =
      0.3f;

    // PC Mouse Hover
    private const float HoverDelay =
        0.5f;

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

    [Header("Section Position")]
    // <변경부분> 기본 Tooltip 본체 위치는 유지하면서
    // SectionParent에만 적용할 개별 위치 보정값
    [SerializeField]
    private Vector2 sectionPositionOffset =
     Vector2.zero;

    // <변경부분> Section이 추가되어 Tooltip 전체 길이가 길어질 때
    // Section 1개당 PopupRoot 전체 위치를 얼마나 보정할지 설정한다.
    //
    // 상단 Tooltip처럼 별도 보정이 필요 없으면 0,
    // 하단 Tooltip처럼 위로 밀어야 하면 양수 값을 사용한다.
    [SerializeField]
    private float popupOffsetYPerSection;

    // 모바일 Long Press 대기 Coroutine
    private Coroutine holdCoroutine;

    // 모바일에서 현재 Pointer를 누르고 있는지 확인한다.
    private bool isHolding;


    // <변경부분> PC Hover Tooltip 표시 대기 Coroutine.
    private Coroutine hoverCoroutine;

    // <변경부분> 현재 PC Mouse Pointer가
    // 이 TooltipTrigger 위에 있는지 확인한다.
    private bool isPointerInside;

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

    // TooltipTrigger가 붙은 UI가 비활성화될 때
    // 현재 이 Trigger에서 표시 중이던 Tooltip과
    // Hover / Long Press 입력 상태를 함께 정리한다.
    //
    // 턴 전환처럼 Button GameObject 자체가 SetActive(false) 되는 경우에는
    // 정상적인 PointerExit가 발생하지 않을 수 있으므로
    // OnDisable에서도 반드시 정리해야 한다.
    private void OnDisable()
    {
        // 이 Trigger가 실제 Tooltip을 표시하고 있었는지 먼저 저장한다.
        //
        // 다른 TooltipTrigger가 표시한 Popup을
        // 관계없는 UI의 비활성화 때문에 닫지 않도록 하기 위함이다.
        bool wasShowingTooltip =
            isTooltipVisible;

        isPointerInside =
            false;

        isHolding =
            false;

        isTooltipVisible =
            false;

        wasTooltipShownDuringPress =
            false;

        // PC Hover 표시를 기다리고 있었다면 취소한다.
        if (hoverCoroutine != null)
        {
            StopCoroutine(
                hoverCoroutine
            );

            hoverCoroutine =
                null;
        }

        // 모바일 Long Press 표시를 기다리고 있었다면 취소한다.
        if (holdCoroutine != null)
        {
            StopCoroutine(
                holdCoroutine
            );

            holdCoroutine =
                null;
        }

        // 모바일 Long Press 직후 Button을 임시로 비활성화한 상태에서
        // UI 자체가 꺼지면 다음 프레임 복구 Coroutine이 중단될 수 있다.
        //
        // 그 경우 Button이 다시 켜졌을 때 계속 interactable=false로
        // 남지 않도록 기존 상태를 즉시 복구한다.
        if (blockNextClick &&
            targetButton != null)
        {
            targetButton.interactable =
                cachedButtonInteractable;
        }

        blockNextClick =
            false;

        // 실제로 이 Trigger가 열었던 Tooltip만 닫는다.
        if (wasShowingTooltip &&
            TooltipPopupUI.Instance != null)
        {
            TooltipPopupUI.Instance.Hide();
        }
    }

    // <변경부분> 별도 TooltipData Asset을 연결.
    //
    // TooltipData 기반 Tooltip은 여기서 미리 TooltipViewData로 변환하지 않는다.
    //
    // 이유:
    // TooltipViewData에는 이미 resolve된 string이 저장되므로
    // Scene 초기화 시점에 미리 생성하면 당시 Locale의 문자열이 캐시될 수 있다.
    //
    // TooltipData 자체를 source로 유지하고,
    // 실제 Tooltip을 표시하는 순간 현재 Locale 기준으로
    // TooltipViewData를 다시 생성한다.
    public void SetTooltipData(
        TooltipData newTooltipData)
    {
        tooltipData =
            newTooltipData;

        // 이전에 만들어진 Locale 문자열이 남지 않도록 제거한다.
        //
        // TooltipData 기반 Tooltip은 ShowTooltip()에서
        // 현재 Locale 기준으로 다시 생성한다.
        tooltipViewData =
            null;
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

    // <변경부분> PC에서는 마우스 커서가 Tooltip 대상 위에 올라오면
    // 별도의 클릭 없이 Hover Delay 후 Tooltip을 자동으로 표시한다.
    //
    // 모바일에는 Hover 개념이 없으므로
    // 기존 Long Press 방식만 사용한다.
    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (Application.isMobilePlatform)
        {
            return;
        }

        isPointerInside =
            true;

        // 이전 Hover 대기가 남아 있다면 중지한다.
        if (hoverCoroutine != null)
        {
            StopCoroutine(
                hoverCoroutine
            );

            hoverCoroutine =
                null;
        }

        hoverCoroutine =
            StartCoroutine(
                ShowTooltipAfterHoverDelay()
            );
    }

    // <변경부분> 모바일에서만 Long Press Tooltip 입력을 시작한다.
    //
    // PC는 PointerEnter Hover가 Tooltip 표시를 담당하므로
    // Mouse Down으로 Long Press Coroutine을 시작하지 않는다.
    public void OnPointerDown(
        PointerEventData eventData)
    {
        if (Application.isMobilePlatform == false)
        {
            return;
        }

        isHolding =
            true;

        isTooltipVisible =
            false;

        wasTooltipShownDuringPress =
            false;

        if (holdCoroutine != null)
        {
            StopCoroutine(
                holdCoroutine
            );
        }

        holdCoroutine =
            StartCoroutine(
                ShowTooltipAfterDelay(
                    eventData.position
                )
            );
    }
    // <변경부분> 포인터를 떼면 팝업 숨김
    // <변경부분> PointerUp 기반 Tooltip 종료와
    // Long Press 이후 Button Click 차단은 모바일에서만 사용한다.
    //
    // PC Tooltip은 Hover 상태로 유지되며
    // 실제 PointerExit에서 닫힌다.
    public void OnPointerUp(
        PointerEventData eventData)
    {
        if (Application.isMobilePlatform == false)
        {
            return;
        }

        // Tooltip이 표시된 상태에서 손을 뗐다면
        // 이번 입력은 Tooltip 확인용 Long Press였으므로
        // Button Click으로 이어지지 않게 차단한다.
        if (wasTooltipShownDuringPress)
        {
            blockNextClick =
                true;

            eventData.eligibleForClick =
                false;

            eventData.Use();

            // Unity Button.onClick이 뒤이어 실행되지 않도록
            // 현재 프레임 동안 Button을 임시 비활성화한다.
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

    // <변경부분> Long Press 이후 Button Click 차단은
    // 모바일 Tooltip 입력에서만 사용한다.
    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (Application.isMobilePlatform == false)
        {
            return;
        }

        if (blockNextClick)
        {
            eventData.eligibleForClick =
                false;

            eventData.Use();
        }
    }

    // <변경부분> PC에서는 Tooltip 대상에서 마우스가 벗어나는 순간
    // Tooltip을 즉시 닫는다.
    //
    // 모바일은 기존 Long Press 동작을 유지하며,
    // 손가락을 떼는 OnPointerUp에서 Tooltip을 닫는다.
    public void OnPointerExit(
        PointerEventData eventData)
    {
        if (Application.isMobilePlatform == false)
        {
            isPointerInside =
                false;

            // 아직 Tooltip이 뜨기 전이라면
            // 진행 중이던 Hover Delay를 취소한다.
            if (hoverCoroutine != null)
            {
                StopCoroutine(
                    hoverCoroutine
                );

                hoverCoroutine =
                    null;
            }

            // 이미 Tooltip이 표시되어 있다면
            // 대상에서 Mouse가 벗어난 즉시 닫는다.
            isTooltipVisible =
                false;

            if (TooltipPopupUI.Instance != null)
            {
                TooltipPopupUI.Instance.Hide();
            }

            return;
        }

        // 모바일에서는 아직 Long Press 완료 전이라면
        // 대상 밖으로 손가락이 나갔을 때 대기만 취소한다.
        if (isTooltipVisible == false)
        {
            StopHold();
            return;
        }

        // 모바일에서 Tooltip이 이미 표시된 뒤에는
        // 기존 동작대로 PointerUp까지 유지한다.
    }

    // <변경부분> PC Mouse Hover가 일정 시간 유지되었을 때
    // Tooltip을 자동으로 표시한다.
    private IEnumerator ShowTooltipAfterHoverDelay()
    {
        yield return new WaitForSecondsRealtime(
    HoverDelay
);

        // Delay 동안 Mouse가 다른 곳으로 이동했다면
        // Tooltip을 표시하지 않는다.
        if (isPointerInside == false)
        {
            hoverCoroutine =
                null;

            yield break;
        }

        // <변경부분> Tooltip을 실제로 표시하는 공통 함수 사용.
        //
        // Hover 중 Mouse가 약간 움직였을 수도 있으므로
        // PointerEnter 당시 좌표가 아니라
        // 현재 Mouse 위치를 기준으로 표시한다.
        ShowTooltip(
            Input.mousePosition,
            false
        );

        hoverCoroutine =
            null;
    }

    // <변경부분> 모바일 Long Press가 지정 시간 이상 유지되었을 때
    // Tooltip을 표시한다.
    private IEnumerator ShowTooltipAfterDelay(
        Vector2 screenPosition)
    {
        // Time.timeScale과 관계없는 실제 시간을 사용한다.
        yield return new WaitForSecondsRealtime(
        HoldDelay
        );

        // 기다리는 동안 손가락을 뗐거나
        // Pointer가 유효하지 않게 되었다면 표시하지 않는다.
        if (isHolding == false)
        {
            holdCoroutine =
                null;

            yield break;
        }

        // 모바일 Long Press Tooltip 표시.
        ShowTooltip(
            screenPosition,
            true
        );

        holdCoroutine =
            null;
    }

    // <변경부분> PC Hover와 Mobile Long Press가
    // 동일한 Tooltip 표시 파이프라인을 사용하도록 공통 처리한다.
    //
    // isLongPress가 true일 때만
    // 모바일 Button Click 차단용 기록을 남긴다.
    private void ShowTooltip(
        Vector2 screenPosition,
        bool isLongPress)
    {
        // <변경부분>
        // TooltipData가 source로 연결된 Tooltip은
        // Tooltip을 실제로 표시할 때마다 현재 Locale 기준으로 다시 resolve한다.
        //
        // TooltipViewData는 이미 번역된 일반 string을 가지고 있기 때문에
        // 이전 Locale에서 생성된 ViewData를 재사용하지 않는다.
        //
        // SetTooltipViewData()를 사용하는 Skill / Item 등의
        // Runtime ViewData 경로는 tooltipData가 null이므로
        // 기존 동작에 영향을 주지 않는다.
        if (tooltipData != null)
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

            return;
        }

        if (TooltipPopupUI.Instance == null)
        {
            Debug.LogWarning(
                $"툴팁 표시 실패: " +
                $"TooltipPopupUI.Instance가 없습니다. " +
                $"대상 오브젝트: {gameObject.name}"
            );

            return;
        }

        TooltipPopupUI.Instance.Show(
            tooltipViewData,
            screenPosition,
            positionMode,
            customPositionOffset,
            fixedCanvasPosition,
            sectionPositionOffset,
            popupOffsetYPerSection
        );

        isTooltipVisible =
            true;

        // <변경부분> Long Press로 열린 경우에만
        // 이후 Button Click을 막기 위한 기록을 남긴다.
        //
        // PC Hover Tooltip은 Button Click을 방해하지 않는다.
        if (isLongPress)
        {
            wasTooltipShownDuringPress =
                true;
        }
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