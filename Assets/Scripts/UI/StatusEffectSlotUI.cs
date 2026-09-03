using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// <변경부분> 스테이터스 창에 표시되는 상태이상 슬롯 UI
// 텍스트 표기는 사용하지 않고, 아이콘과 애니메이션으로 상태를 표시
public class StatusEffectSlotUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Icon")]
    [SerializeField] private Image iconImage;

    [Header("Tooltip")]
    // <변경부분> 상태효과 아이콘을 꾹 눌렀을 때 상태효과 설명 팝업을 표시할 TooltipTrigger
    [SerializeField] private TooltipTrigger tooltipTrigger;

    [Header("Warning Animation")]
    // <변경부분> 남은 턴이 1턴인 상태이상일 때 깜빡임 애니메이션을 제어하는 Animator
    [SerializeField] private Animator warningAnimator;

    // <변경부분> Animator bool 파라미터 이름
    [SerializeField] private string warningBoolParameterName = "IsWarning";

    // <변경부분> 현재 이 슬롯에 표시 중인 상태효과 기본 데이터.
    //
    // Locale 변경 시 Tooltip 이름/설명을
    // 새로운 언어로 다시 생성하기 위해 보관한다.
    private StatusEffectData currentStatusEffectData;

    // <변경부분> 현재 상태효과의 남은 턴 / 중첩 정보.
    //
    // Locale 변경 시 기존 남은 턴 정보를 유지하면서
    // TooltipViewData만 다시 만들기 위해 보관한다.
    private OwnedStatusEffectData currentOwnedStatusEffectData;

    // <변경부분> 슬롯이 활성화되어 있는 동안
    // Unity Localization의 Locale 변경 이벤트를 구독한다.
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    // <변경부분> 슬롯 비활성화 또는 제거 시
    // Locale 변경 이벤트 구독을 해제한다.
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;
    }

    // <변경부분> 실행 중 Locale이 변경되면
    // 아이콘과 경고 애니메이션은 건드리지 않고
    // 현재 상태효과 Tooltip 문자열만 다시 생성한다.
    private void OnSelectedLocaleChanged(
        Locale locale)
    {
        RefreshCurrentTooltip();
    }

    // <변경부분> 상태이상 슬롯을 빈 상태로 초기화하는 함수
    public void Clear()
    {
        // <변경부분> 현재 Tooltip 재생성용 데이터도 함께 제거한다.
        currentStatusEffectData =
            null;

        currentOwnedStatusEffectData =
            null;

        // 슬롯 루트 비활성화
        if (root != null)
        {
            root.SetActive(false);
        }

        // 아이콘 초기화
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        // <변경부분> 상태효과 슬롯이 비었을 때 Tooltip 데이터도 제거
        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetTooltipViewData(null);
        }

        // <변경부분> 경고 깜빡임 애니메이션 비활성화
        SetWarningAnimation(false);
    }

    // <변경부분> 상태이상 데이터를 슬롯에 표시하는 함수
    public void Refresh(
    StatusEffectData statusEffectData,
    OwnedStatusEffectData ownedStatusEffectData)
    {
        // 표시할 상태이상 정보가 없으면 슬롯 비움
        if (statusEffectData == null ||
            ownedStatusEffectData == null)
        {
            Clear();
            return;
        }

        // <변경부분> 현재 슬롯이 표시하고 있는 데이터를 저장한다.
        //
        // Locale 변경 시 이 데이터를 기준으로
        // Tooltip만 새로운 언어로 다시 만든다.
        currentStatusEffectData =
            statusEffectData;

        currentOwnedStatusEffectData =
            ownedStatusEffectData;

        // 슬롯 루트 활성화
        if (root != null)
        {
            root.SetActive(true);
        }

        // 상태이상 아이콘 표시
        if (iconImage != null)
        {
            iconImage.sprite = statusEffectData.iconSprite;
            iconImage.enabled = statusEffectData.iconSprite != null;
            iconImage.preserveAspect = true;
        }

        // <변경부분> 현재 Locale 기준 이름/설명과
        // 현재 남은 턴 정보를 사용해 Tooltip을 갱신한다.
        RefreshCurrentTooltip();

        // <변경부분> 남은 턴이 1턴 이하인 상태이상은 깜빡임 애니메이션 활성화
        bool shouldWarningBlink = ownedStatusEffectData.remainingTurn <= 1;
        SetWarningAnimation(shouldWarningBlink);
    }

    // <변경부분> TooltipTrigger가 Inspector에 연결되지 않은 경우
    // 현재 오브젝트 또는 자식에서 자동으로 찾는다.
    private void AutoBindTooltipTrigger()
    {
        if (tooltipTrigger != null)
        {
            return;
        }

        tooltipTrigger =
            GetComponent<TooltipTrigger>();

        if (tooltipTrigger == null)
        {
            tooltipTrigger =
                GetComponentInChildren<TooltipTrigger>();
        }
    }

    // <변경부분> 현재 슬롯에 저장된 상태효과 정보를 기준으로
    // TooltipViewData를 다시 만들어 TooltipTrigger에 적용한다.
    //
    // Locale 변경 시에도 이 함수만 다시 호출하므로
    // 아이콘 / Warning Animator / 슬롯 활성 상태에는 영향을 주지 않는다.
    private void RefreshCurrentTooltip()
    {
        AutoBindTooltipTrigger();

        if (tooltipTrigger == null)
        {
            return;
        }

        if (currentStatusEffectData == null ||
            currentOwnedStatusEffectData == null)
        {
            tooltipTrigger.SetTooltipViewData(
                null
            );

            return;
        }

        tooltipTrigger.SetTooltipViewData(
            CreateStatusEffectTooltipViewData(
                currentStatusEffectData,
                currentOwnedStatusEffectData
            )
        );
    }

    // <변경부분> 공용 TooltipData 변환을 통해
    // 현재 Locale 기준 상태효과 이름/설명을 먼저 구성한 뒤,
    // 이 슬롯만의 런타임 정보인 남은 턴을 추가한다.
    private TooltipViewData CreateStatusEffectTooltipViewData(
        StatusEffectData statusEffectData,
        OwnedStatusEffectData ownedStatusEffectData)
    {
        if (statusEffectData == null)
        {
            return null;
        }

        TooltipViewData viewData =
            TooltipViewData.FromStatusEffectData(
                statusEffectData
            );

        if (viewData == null)
        {
            return null;
        }

        // <변경부분> 남은 턴 전체 문장을
        // Tooltip_Common Localization에서 가져온다.
        //
        // 언어마다 숫자의 위치나 "턴" 표현이 달라도
        // {turn} placeholder를 이용해 각 Locale에서 자유롭게 구성할 수 있다.
        if (ownedStatusEffectData != null)
        {
            viewData.levelText =
                TooltipLocalization
                    .GetRemainingTurnText(
                        ownedStatusEffectData.remainingTurn
                    );
        }
        else
        {
            viewData.levelText =
                string.Empty;
        }

        return viewData;
    }

    // <변경부분> Animator bool 값을 통해 깜빡임 애니메이션을 켜고 끄는 함수
    private void SetWarningAnimation(bool isWarning)
    {
        if (warningAnimator == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(warningBoolParameterName))
        {
            return;
        }

        warningAnimator.SetBool(warningBoolParameterName, isWarning);
    }
}