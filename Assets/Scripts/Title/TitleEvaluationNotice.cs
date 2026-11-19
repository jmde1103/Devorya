using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Title-only orchestration; uses the existing global locale manager and popup animator.
[DefaultExecutionOrder(-10000)]
public sealed class TitleEvaluationNotice : MonoBehaviour
{
    [SerializeField, InspectorName("안내 팝업")] private GameObject modalRoot;
    [SerializeField, InspectorName("제목")] private TMP_Text titleText;
    [SerializeField, InspectorName("본문")] private TMP_Text bodyText;
    [SerializeField, InspectorName("확인 버튼")] private Button continueButton;
    [SerializeField, InspectorName("열기 애니메이션")] private PopupOpenAnimator popupAnimator;
    [SerializeField, InspectorName("초기화 대기 타이틀 캔버스")] private Canvas[] titleCanvases;
    [SerializeField, InspectorName("뒤쪽 타이틀 입력")] private Selectable[] titleInputs;
    [SerializeField, InspectorName("제목 번역")] private LocalizedString title = new LocalizedString("Event_UI", "title.evaluation.title");
    [SerializeField, InspectorName("본문 번역")] private LocalizedString body = new LocalizedString("Event_UI", "title.evaluation.body");
    [SerializeField, InspectorName("확인 번역")] private LocalizedString confirm = new LocalizedString("Event_UI", "title.evaluation.continue");
    private static bool shownThisSession;
    private readonly Dictionary<Canvas, bool> canvasStates = new Dictionary<Canvas, bool>();
    private readonly Dictionary<Selectable, bool> inputStates = new Dictionary<Selectable, bool>();
    private InputSystemUIInputModule inputModule;
    private bool deselectOnBackgroundClick;
    private bool isOpen;
    private bool titleReady, bodyReady, confirmReady;
    public bool IsOpen => isOpen;
    public bool IsReady { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { shownThisSession = false; }

    private void Awake()
    {
        modalRoot.SetActive(false);
        continueButton.onClick.AddListener(Close);

        if (shownThisSession)
        {
            Canvas noticeCanvas = GetComponent<Canvas>();

            if (noticeCanvas != null)
                noticeCanvas.enabled = false;

            IsReady = true;
            return;
        }

        foreach (var canvas in titleCanvases)
            if (canvas != null) { canvasStates[canvas] = canvas.enabled; canvas.enabled = false; }

        foreach (var input in titleInputs)
            if (input != null) { inputStates[input] = input.interactable; input.interactable = false; }

        StartCoroutine(InitializeAndShow());
    }

    private IEnumerator InitializeAndShow()
    {
        var manager = DevoryaLocalizationManager.Instance;
        if (manager == null)
            manager = new GameObject("LocalizationManager").AddComponent<DevoryaLocalizationManager>();
        yield return manager.InitializeFirstTitleLanguage();
        // Preload before revealing any localized title graphics.
        var tables = LocalizationSettings.StringDatabase.PreloadTables("Event_UI");
        yield return tables;
        title.StringChanged += SetTitle; body.StringChanged += SetBody; confirm.StringChanged += SetConfirm;
        while (!titleReady || !bodyReady || !confirmReady) yield return null;
        yield return null;
        EnsureModalSorting();
        RestoreCanvases();
        shownThisSession = true;
        IsReady = true;
        isOpen = true;
        inputModule = EventSystem.current != null
            ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
        if (inputModule != null)
        {
            deselectOnBackgroundClick = inputModule.deselectOnBackgroundClick;
            inputModule.deselectOnBackgroundClick = false;
        }
        modalRoot.SetActive(true);
        if (popupAnimator != null) popupAnimator.PlayOpen();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
    }

    // Sorting layer precedes sortingOrder in both rendering and UI raycasts.
    // Match the frontmost title layer without changing saved prefab/scene design.
    private void EnsureModalSorting()
    {
        var modalCanvas = GetComponent<Canvas>();
        if (modalCanvas == null) return;
        int layer = modalCanvas.sortingLayerID;
        int order = modalCanvas.sortingOrder;
        foreach (var canvas in titleCanvases)
        {
            if (canvas == null || canvas == modalCanvas) continue;
            int value = SortingLayer.GetLayerValueFromID(canvas.sortingLayerID);
            int current = SortingLayer.GetLayerValueFromID(layer);
            if (value > current)
            {
                layer = canvas.sortingLayerID;
                order = canvas.sortingOrder + 1;
            }
            else if (value == current)
                order = Mathf.Max(order, canvas.sortingOrder + 1);
        }
        modalCanvas.sortingLayerID = layer;
        modalCanvas.sortingOrder = order;
    }
    private void SetTitle(string value) { titleText.text = value; titleReady = true; }
    private void SetBody(string value) { bodyText.text = value; bodyReady = true; }
    private void SetConfirm(string value) { continueButton.GetComponentInChildren<TMP_Text>(true).text = value; confirmReady = true; }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;

        // Close the evaluation modal.
        modalRoot.SetActive(false);

        // Disable the entire notice canvas so it cannot intercept
        // input intended for the title or tutorial choice popup.
        Canvas noticeCanvas = GetComponent<Canvas>();

        if (noticeCanvas != null)
            noticeCanvas.enabled = false;

        RestoreInputs();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
    private void RestoreCanvases()
    {
        foreach (var state in canvasStates) if (state.Key != null) state.Key.enabled = state.Value;
        canvasStates.Clear();
    }
    private void RestoreInputs()
    {
        if (inputModule != null)
        {
            inputModule.deselectOnBackgroundClick = deselectOnBackgroundClick;
            inputModule = null;
        }
        foreach (var state in inputStates) if (state.Key != null) state.Key.interactable = state.Value;
        inputStates.Clear();
    }
    private void OnDestroy()
    {
        title.StringChanged -= SetTitle; body.StringChanged -= SetBody; confirm.StringChanged -= SetConfirm;
        if (continueButton != null) continueButton.onClick.RemoveListener(Close);
        RestoreCanvases(); RestoreInputs();
    }
}
