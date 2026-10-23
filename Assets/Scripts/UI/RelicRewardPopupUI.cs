using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class RelicRewardPopupUI : MonoBehaviour
{
    [SerializeField, InspectorName("카드 프리팹")] RelicRewardCardUI cardPrefab;
    [SerializeField, InspectorName("안전 영역")] RectTransform safeArea;
    [SerializeField, InspectorName("카드 배열")] RectTransform cardRoot;
    [SerializeField, Min(0), InspectorName("카드 간격")] float spacing = 44;
    [SerializeField, Min(0), InspectorName("카드 등장 간격")] float stagger = .22f;
    [SerializeField, Range(1,64), InspectorName("흡수 입자 개수")] int particleCount = 30;
    readonly List<RelicRewardCardUI> cards = new List<RelicRewardCardUI>();
    MonoBehaviour owner;
    BattleManager battle;
    bool chosen, accepting, selecting;
    CanvasGroup arrivalGroup;
    float arrivalAlpha;
    RectTransform arrivalTarget;
    Vector3 arrivalScale;
    public static RelicRewardPopupUI Active { get; private set; }
    public static bool IsOpen => Active != null;
    public static void CloseOwned(MonoBehaviour owner)
    {
        if (Active != null && Active.owner == owner) Active.Close();
    }
    void Close()
    {
        accepting = false;
        if (Active == this) Active = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    void RestoreArrival()
    {
        if (arrivalGroup != null) arrivalGroup.alpha = arrivalAlpha;
        if (arrivalTarget != null) arrivalTarget.localScale = arrivalScale;
    }
    void OnDestroy() { RestoreArrival(); if (Active == this) Active = null; }
    public static IEnumerator ShowRoutine(MonoBehaviour owner, RelicRewardListData data, BattleManager battle = null)
    {
        if (data == null) { Debug.LogWarning("유물 보상 목록이 연결되지 않았습니다.", owner); yield break; }
        if (RunStateManager.Instance == null) { Debug.LogError("유물 보상: RunStateManager가 필요합니다.", owner); yield break; }
        var choices = data.CreateChoices(RunStateManager.Instance);
        if (choices.Count == 0) yield break;
        while (Active != null) yield return null;
        var prefab = Resources.Load<RelicRewardPopupUI>("RelicRewardPopup");
        if (prefab == null) { Debug.LogError("RelicRewardPopup 프리팹이 없습니다.", owner); yield break; }
        var popup = Instantiate(prefab);
        popup.owner = owner; popup.battle = battle; Active = popup;
        try
        {
            yield return popup.Open(choices);
            while (popup != null && !popup.chosen) yield return null;
        }
        finally { if (popup != null) popup.Close(); }
    }
    IEnumerator Open(List<BattleRelicData> choices)
    {
        foreach (var data in choices)
        {
            var card = Instantiate(cardPrefab, cardRoot);
            card.Bind(data, Choose); cards.Add(card);
        }
        Layout();
        foreach (var card in cards)
        {
            if (this == null || !isActiveAndEnabled) yield break;
            StartCoroutine(card.Reveal());
            yield return new WaitForSecondsRealtime(stagger);
        }
        while (this != null && isActiveAndEnabled && cards.Exists(c => c != null && !c.Ready)) yield return null;
        if (this == null || !isActiveAndEnabled) yield break;
        accepting = true;
        foreach (var card in cards) card.SetSelectable(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
    }
    void Update()
    {
        if (owner == null || !owner.isActiveAndEnabled) { Close(); return; }
        Layout();
    }
    void Layout()
    {
        var rect = Screen.safeArea;
        safeArea.anchorMin = new Vector2(rect.xMin / Mathf.Max(1, Screen.width), rect.yMin / Mathf.Max(1, Screen.height));
        safeArea.anchorMax = new Vector2(rect.xMax / Mathf.Max(1, Screen.width), rect.yMax / Mathf.Max(1, Screen.height));
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        if (cards.Count == 0 || selecting) return;
        var size = ((RectTransform)cardPrefab.transform).sizeDelta;
        float width = cards.Count * size.x + (cards.Count - 1) * spacing;
        float scale = Mathf.Min(1, (safeArea.rect.width - 48) / width, (safeArea.rect.height - 48) / size.y);
        cardRoot.localScale = Vector3.one * Mathf.Max(.01f, scale);
        for (int i = 0; i < cards.Count; i++)
            ((RectTransform)cards[i].transform).anchoredPosition = new Vector2((i - (cards.Count - 1) * .5f) * (size.x + spacing), 0);
    }
    public static Vector3 ParticleDestination(RectTransform target, RectTransform particleRoot)
    {
        // Screen coordinates bridge Overlay, Camera and World Space canvases.
        var targetCanvas=target.GetComponentInParent<Canvas>()?.rootCanvas;
        var popupCanvas=particleRoot.GetComponentInParent<Canvas>()?.rootCanvas;
        var targetCamera=targetCanvas!=null&&targetCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?targetCanvas.worldCamera:null;
        var popupCamera=popupCanvas!=null&&popupCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?popupCanvas.worldCamera:null;
        Vector2 screen=RectTransformUtility.WorldToScreenPoint(targetCamera,target.TransformPoint(target.rect.center));
        RectTransformUtility.ScreenPointToWorldPointInRectangle(particleRoot,screen,popupCamera,out var point);
        return point;
    }
    IEnumerator Collect(RelicRewardCardUI selected)
    {
        selecting = true;
        selected.MarkSelected();
        if(owner is EventSceneSequenceController sceneOwner)sceneOwner.ShowHUDForRelicReward();
        if(owner is WorldMapEventController worldOwner)worldOwner.ShowHUDForRelicReward();
        var selectedRect = (RectTransform)selected.transform;
        Vector2 from = selectedRect.anchoredPosition;
        var groups = new List<CanvasGroup>();
        foreach (var card in cards) groups.Add(card.GetComponent<CanvasGroup>());
        for (float t = 0; t < .38f; t += Time.unscaledDeltaTime)
        {
            float p = Mathf.SmoothStep(0, 1, t / .38f);
            selectedRect.anchoredPosition = Vector2.Lerp(from, Vector2.zero, p);
            for (int i = 0; i < cards.Count; i++) if (cards[i] != selected) groups[i].alpha = 1 - p;
            yield return null;
        }
        selectedRect.anchoredPosition = Vector2.zero;
        RectTransform target = RelicRewardArrivalHUD.ResolveTarget(selected.Data);
        if (target == null) { chosen = true; yield break; }
        arrivalTarget = target; arrivalScale = target.localScale;
        arrivalGroup = target.GetComponent<CanvasGroup>();
        if (arrivalGroup == null) arrivalGroup = target.gameObject.AddComponent<CanvasGroup>();
        arrivalAlpha = arrivalGroup.alpha; arrivalGroup.alpha = 0;
        var particles = new List<RectTransform>();
        var starts = new List<Vector3>();
        var images = new List<Image>();
        for (int i = 0; i < particleCount; i++)
        {
            var obj = new GameObject("RelicParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)obj.transform; rect.SetParent(transform, false);
            rect.sizeDelta = Vector2.one * Random.Range(4f, 9f);
            var image = obj.GetComponent<Image>(); image.color = selected.ParticleColor; image.raycastTarget = false;
            var point = selectedRect.TransformPoint(new Vector3(Random.Range(-150f, 150f), Random.Range(-210f, 210f), 0));
            rect.position = point; particles.Add(rect); starts.Add(point); images.Add(image);
        }
        var selectedGroup = selected.GetComponent<CanvasGroup>();
        for (float elapsed = 0; elapsed < .9f; elapsed += Time.unscaledDeltaTime)
        {
            selectedGroup.alpha = 1 - Mathf.Clamp01(elapsed / .22f);
            Vector3 end = target != null ? ParticleDestination(target, (RectTransform)transform) : transform.position;
            for (int i = 0; i < particles.Count; i++)
            {
                float t = Mathf.Clamp01((elapsed - i * .003f) / .7f);
                Vector3 mid = (starts[i] + end) * .5f + new Vector3(0, 130 + i % 5 * 13, 0);
                particles[i].position = (1-t)*(1-t)*starts[i] + 2*(1-t)*t*mid + t*t*end;
                var color = selected.ParticleColor; color.a *= 1 - Mathf.Clamp01((t - .85f) / .15f); images[i].color = color;
            }
            yield return null;
        }
        foreach (var particle in particles) Destroy(particle.gameObject);
        if (target != null)
        {
            arrivalGroup.alpha = arrivalAlpha;
            var arrivedSlot = target.GetComponentInParent<BattleRelicSlotUI>();
            if(arrivedSlot != null) arrivedSlot.PlayArrival();
            var opening = target.GetComponent<PopupOpenAnimator>();
            while(opening != null && opening.IsPlaying) yield return null;
        }
        chosen = true;
    }
    void Choose(RelicRewardCardUI card)
    {
        if (!accepting || chosen || !cards.Contains(card)) return;
        accepting = false;
        bool success = battle != null ? battle.AddBattleRelic(card.Data) : RunStateManager.Instance.TryAddBattleRelic(card.Data, 4);
        if (success)
        {
            StartCoroutine(Collect(card));
            foreach (var candidate in cards) candidate.SetSelectable(false);
        }
        else
        {
            // Another system may have granted a relic while the popup was open.
            // End safely if no choice is still receivable.
            var run = RunStateManager.Instance;
            foreach (var candidate in cards)
            {
                bool eligible = run != null && run.GetBattleRelicsCopy().Count < 10 && !run.HasBattleRelic(candidate.Data.relicType);
                candidate.SetSelectable(eligible); accepting |= eligible;
            }
            if (!accepting) chosen = true;
        }
    }
}