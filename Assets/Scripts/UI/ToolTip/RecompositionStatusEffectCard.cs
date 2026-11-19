using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RecompositionStatusEffectCard : MonoBehaviour
{
    [SerializeField, InspectorName("상태효과 이름")] private TMP_Text statusNameText;
    [SerializeField, InspectorName("상태효과 설명")] private TMP_Text statusDescriptionText;
    [SerializeField, InspectorName("상태효과 아이콘")] private Image statusIcon;
    [SerializeField, InspectorName("긴 설명 아래로 확장")] private bool growDescription = true;
    private bool initialized, iconEnabled;
    private RecompositionVerticalPanel verticalPanel;
    public void Refresh(TooltipSectionData data)
    {
        if (!initialized)
        {
            initialized=true; iconEnabled=statusIcon!=null&&statusIcon.enabled;
            verticalPanel=new RecompositionVerticalPanel((RectTransform)transform,statusNameText,statusDescriptionText);
        }
        RecompositionTooltipUI.SetText(statusNameText,data.sectionTitle,TooltipTextStyle.StatusRole(data.statusEffectData),true);
        RecompositionTooltipUI.SetText(statusDescriptionText,data.sectionDescription,TooltipTextRole.Body);
        if(statusIcon!=null){statusIcon.sprite=data.sectionIcon;statusIcon.enabled=iconEnabled&&statusIcon.sprite!=null;}
        if(growDescription)verticalPanel.Resize();
        else
        {
            verticalPanel.Restore();
            var layout=GetComponent<LayoutElement>();
            if(layout!=null)layout.minHeight=layout.preferredHeight=((RectTransform)transform).rect.height;
        }
    }
}
