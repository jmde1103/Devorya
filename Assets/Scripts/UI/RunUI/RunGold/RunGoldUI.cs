using TMPro;
using UnityEngine;


// <변경부분>
// RunStateManager가 보유하고 있는 현재 Run 금화를
// Battle / WorldMap UI에 표시하는 공용 컴포넌트.
//
// 금화 데이터 자체를 저장하거나 변경하지 않고,
// RunStateManager의 현재 값을 읽어서 화면에 표시한다.
public class RunGoldUI : MonoBehaviour
{
    [Header("UI References")]

    // 현재 보유 금화를 표시할 TMP Text.
    [SerializeField]
    private TMP_Text goldAmountText;


    [Header("Display")]

    // 1000 이상 금액을 1,000 형태로 표시할지 여부.
    [SerializeField]
    private bool useThousandsSeparator =
        true;


    // 마지막으로 UI에 표시한 금액.
    //
    // 실제 값이 바뀌었을 때만 TMP Text를 다시 갱신하여
    // 매 프레임 불필요한 문자열 생성을 피한다.
    private int lastDisplayedGold =
        int.MinValue;


    private void OnEnable()
    {
        RefreshImmediately();
    }


    private void Update()
    {
        RunStateManager runStateManager =
            RunStateManager.Instance;


        if (runStateManager == null)
        {
            return;
        }


        int currentGold =
            runStateManager.GetGoldAmount();


        // 금액이 그대로라면
        // Text 문자열을 다시 만들 필요가 없다.
        if (currentGold ==
            lastDisplayedGold)
        {
            return;
        }


        ApplyGoldAmount(
            currentGold
        );
    }


    // 외부에서 즉시 갱신이 필요한 경우에도 사용할 수 있다.
    public void RefreshImmediately()
    {
        RunStateManager runStateManager =
            RunStateManager.Instance;


        if (runStateManager == null)
        {
            return;
        }


        ApplyGoldAmount(
            runStateManager.GetGoldAmount()
        );
    }


    // 실제 TMP Text에 금액을 적용한다.
    private void ApplyGoldAmount(
        int goldAmount)
    {
        lastDisplayedGold =
            goldAmount;


        if (goldAmountText == null)
        {
            return;
        }


        if (useThousandsSeparator)
        {
            goldAmountText.text =
                goldAmount.ToString("N0");
        }
        else
        {
            goldAmountText.text =
                goldAmount.ToString();
        }
    }
}
