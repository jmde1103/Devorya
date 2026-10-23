using UnityEngine;
using System.Collections.Generic;

public sealed class RunRelicBarUI : MonoBehaviour
{
    [SerializeField, InspectorName("유물 슬롯")] BattleRelicSlotUI[] relicSlots;
    float nextRefresh;
    bool initialized;
    void OnEnable(){initialized=false;RefreshFromRunState();}
    void Update()
    {
        if(Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+.2f;
        RefreshFromRunState();
    }
    public void RefreshFromRunState()
    {
        var owned=RunStateManager.Instance!=null?RunStateManager.Instance.GetBattleRelicsCopy():null;
        if(relicSlots==null)return;
        for(int i=0;i<Mathf.Min(4,relicSlots.Length);i++)
        {
            var data=owned!=null&&i<owned.Count?owned[i]:null;
            var slot=relicSlots[i];
            if(slot!=null&&(!initialized||slot.CurrentRelicData!=data||!slot.gameObject.activeSelf))slot.Refresh(data);
        }
        initialized=true;
    }
    public RectTransform PrepareArrival(BattleRelicData data)
    {
        RefreshFromRunState();
        foreach(var slot in relicSlots)if(slot!=null&&slot.CurrentRelicData==data)
        {
            var icon=slot.IconRect;
            var opening=icon!=null?icon.GetComponent<PopupOpenAnimator>():null;
            if(opening!=null)opening.CompleteImmediately();
            return icon;
        }
        return null;
    }
}