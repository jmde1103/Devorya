using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// An editable, scene-local destination for event relic acquisition.
public sealed class RelicRewardArrivalHUD : MonoBehaviour
{
    [SerializeField, InspectorName("안전 영역")] RectTransform safeArea;
    [SerializeField, InspectorName("유물 슬롯 배열")] RectTransform slotRoot;
    [SerializeField, InspectorName("슬롯 원본")] Image slotTemplate;
    readonly Dictionary<BattleRelicType, RectTransform> slots = new Dictionary<BattleRelicType, RectTransform>();
    void LateUpdate()
    {
        var rect = Screen.safeArea;
        safeArea.anchorMin = new Vector2(rect.xMin / Mathf.Max(1,Screen.width), rect.yMin / Mathf.Max(1,Screen.height));
        safeArea.anchorMax = new Vector2(rect.xMax / Mathf.Max(1,Screen.width), rect.yMax / Mathf.Max(1,Screen.height));
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }
    public static RectTransform ResolveTarget(BattleRelicData data)
    {
        foreach(var bar in FindObjectsByType<RunRelicBarUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var authoredTarget=bar.PrepareArrival(data);
            if(authoredTarget!=null)return authoredTarget;
        }
        foreach (var slot in FindObjectsByType<BattleRelicSlotUI>(FindObjectsSortMode.None))
            if (slot.CurrentRelicData == data) return slot.IconRect;
        var hud = FindFirstObjectByType<RelicRewardArrivalHUD>();
        if (hud == null)
        {
            var prefab = Resources.Load<RelicRewardArrivalHUD>("RelicInventoryHUD");
            if (prefab == null) return null;
            hud = Instantiate(prefab);
        }
        foreach (var owned in RunStateManager.Instance.GetBattleRelicsCopy())
        {
            if (owned == null || hud.slots.ContainsKey(owned.relicType)) continue;
            var image = Instantiate(hud.slotTemplate, hud.slotRoot);
            image.name = "OwnedRelicIcon"; image.sprite = owned.iconSprite;
            image.gameObject.SetActive(true);
            image.GetComponent<TooltipTrigger>().SetTooltipViewData(TooltipViewData.FromBattleRelicData(owned));
            hud.slots.Add(owned.relicType, image.rectTransform);
        }
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(hud.slotRoot);
        return hud.slots.TryGetValue(data.relicType, out var target) ? target : null;
    }
}