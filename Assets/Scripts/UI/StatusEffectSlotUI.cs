using UnityEngine;
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

    // <변경부분> 상태이상 슬롯을 빈 상태로 초기화하는 함수
    public void Clear()
    {
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
    public void Refresh(StatusEffectData statusEffectData, OwnedStatusEffectData ownedStatusEffectData)
    {
        // 표시할 상태이상 정보가 없으면 슬롯 비움
        if (statusEffectData == null || ownedStatusEffectData == null)
        {
            Clear();
            return;
        }

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

        // <변경부분> TooltipTrigger가 인스펙터에 연결되지 않았다면 현재 오브젝트 또는 자식에서 찾음
        if (tooltipTrigger == null)
        {
            tooltipTrigger = GetComponent<TooltipTrigger>();

            if (tooltipTrigger == null)
            {
                tooltipTrigger = GetComponentInChildren<TooltipTrigger>();
            }
        }

        // <변경부분> 상태효과 데이터와 현재 남은 턴 정보를 함께 Tooltip에 연결
        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetTooltipViewData(
                CreateStatusEffectTooltipViewData(statusEffectData, ownedStatusEffectData)
            );
        }

        // <변경부분> 남은 턴이 1턴 이하인 상태이상은 깜빡임 애니메이션 활성화
        bool shouldWarningBlink = ownedStatusEffectData.remainingTurn <= 1;
        SetWarningAnimation(shouldWarningBlink);
    }

    // <변경부분> 상태효과 기본 데이터와 현재 보유 상태 데이터를 합쳐 Tooltip 표시용 데이터를 생성
    private TooltipViewData CreateStatusEffectTooltipViewData(
        StatusEffectData statusEffectData,
        OwnedStatusEffectData ownedStatusEffectData
    )
    {
        // 상태효과 데이터가 없으면 Tooltip 생성 불가
        if (statusEffectData == null)
        {
            return null;
        }

        // 남은 턴 텍스트 기본값
        string remainingTurnText = string.Empty;

        // 현재 보유 상태 데이터가 있으면 남은 턴을 Tooltip의 LevelText 위치에 표시
        if (ownedStatusEffectData != null)
        {
            remainingTurnText = $"남은 턴: {ownedStatusEffectData.remainingTurn}턴";
        }

        // 상태효과 Tooltip 표시 데이터 생성
        return new TooltipViewData
        {
            title = statusEffectData.effectName,
            category = "상태효과",
            levelText = remainingTurnText,
            mainDescription = statusEffectData.description,
            icon = statusEffectData.iconSprite,
            sections = statusEffectData.tooltipSections
        };
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