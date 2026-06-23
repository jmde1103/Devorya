using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// <변경부분> UI 아이콘/버튼을 꾹 눌렀을 때 TooltipPopupUI를 표시하는 트리거
public class TooltipTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Tooltip")]
    [SerializeField] private TooltipData tooltipData;

    [Header("Input")]
    [SerializeField] private float holdDelay = 0.3f;

    private Coroutine holdCoroutine;
    private bool isHolding;

    // <변경부분> 외부 UI 갱신 코드에서 현재 아이콘에 맞는 TooltipData를 주입
    public void SetTooltipData(TooltipData newTooltipData)
    {
        tooltipData = newTooltipData;
    }

    // <변경부분> 포인터를 누르기 시작하면 꾹 누름 대기 시작
    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
        }

        holdCoroutine = StartCoroutine(ShowTooltipAfterDelay(eventData.position));
    }

    // <변경부분> 포인터를 떼면 팝업 숨김
    public void OnPointerUp(PointerEventData eventData)
    {
        StopHold();
    }

    // <변경부분> 누른 상태로 아이콘 밖으로 나가면 팝업 숨김
    public void OnPointerExit(PointerEventData eventData)
    {
        StopHold();
    }

    // <변경부분> 지정 시간 이상 누르고 있으면 팝업 표시
    private IEnumerator ShowTooltipAfterDelay(Vector2 screenPosition)
    {
        yield return new WaitForSeconds(holdDelay);

        if (isHolding == false)
        {
            yield break;
        }

        if (tooltipData == null)
        {
            yield break;
        }

        if (TooltipPopupUI.Instance != null)
        {
            TooltipPopupUI.Instance.Show(tooltipData, screenPosition);
        }
    }

    // <변경부분> 꾹 누름 상태를 종료하고 팝업을 숨김
    private void StopHold()
    {
        isHolding = false;

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