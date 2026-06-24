using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// <변경부분> UI 아이콘/버튼을 꾹 눌렀을 때 TooltipPopupUI를 표시하는 트리거
public class TooltipTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Tooltip")]
    // <변경부분> 흡수 버튼처럼 별도 Data가 없는 UI에서 사용할 고정 Tooltip 에셋
    [SerializeField] private TooltipData tooltipData;

    // <변경부분> 스킬/아이템/상태효과 데이터에서 런타임으로 만든 표시용 Tooltip 데이터
    private TooltipViewData tooltipViewData;

    [Header("Input")]
    [SerializeField] private float holdDelay = 0.3f;

    private Coroutine holdCoroutine;
    private bool isHolding;

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

        // <변경부분> 런타임 TooltipViewData가 없고 고정 TooltipData가 있으면 변환해서 사용
        if (tooltipViewData == null && tooltipData != null)
        {
            tooltipViewData = TooltipViewData.FromTooltipData(tooltipData);
        }

        if (tooltipViewData == null)
        {
            yield break;
        }

        if (TooltipPopupUI.Instance != null)
        {
            TooltipPopupUI.Instance.Show(tooltipViewData, screenPosition);
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