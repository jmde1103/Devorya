using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.EventSystems;

public sealed class TutorialChoicePopup : MonoBehaviour
{
    [SerializeField, InspectorName("질문 문구")] private TMP_Text questionText;
    [SerializeField, InspectorName("예 버튼")] private Button yesButton;
    [SerializeField, InspectorName("아니오 버튼")] private Button noButton;
    [SerializeField, InspectorName("열기 애니메이션")] private PopupOpenAnimator popupAnimator;
    [SerializeField, InspectorName("질문 번역")] private LocalizedString question = new LocalizedString("Event_UI", "title.tutorial.question");
    [SerializeField, InspectorName("예 번역")] private LocalizedString yes = new LocalizedString("Event_UI", "title.tutorial.yes");
    [SerializeField, InspectorName("아니오 번역")] private LocalizedString no = new LocalizedString("Event_UI", "title.tutorial.no");
    private Action<bool> onChosen;
    private bool awaitingChoice;
    public bool IsOpen => awaitingChoice;
    private void Awake()
    {
        yesButton.onClick.AddListener(ChooseYes);
        noButton.onClick.AddListener(ChooseNo);
    }
    private void OnEnable() { question.StringChanged += SetQuestion; yes.StringChanged += SetYes; no.StringChanged += SetNo; }
    private void OnDisable()
    {
        question.StringChanged -= SetQuestion; yes.StringChanged -= SetYes; no.StringChanged -= SetNo;
        awaitingChoice = false; onChosen = null;
    }
    private void SetQuestion(string value) { questionText.text = value; }
    private void SetYes(string value) { yesButton.GetComponentInChildren<TMP_Text>(true).text = value; }
    private void SetNo(string value) { noButton.GetComponentInChildren<TMP_Text>(true).text = value; }
    public void Show(Action<bool> callback)
    {
        if (awaitingChoice) return;
        gameObject.SetActive(true);
        onChosen = callback; awaitingChoice = true;
        yesButton.interactable = noButton.interactable = true;
        if (popupAnimator != null) popupAnimator.PlayOpen();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
    }
    public void ChooseYes() => Choose(true);
    public void ChooseNo() => Choose(false);
    private void Choose(bool tutorial)
    {
        if (!awaitingChoice) return;
        awaitingChoice = false;
        var callback = onChosen;
        gameObject.SetActive(false);
        callback?.Invoke(tutorial);
    }
}
