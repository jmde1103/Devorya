using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public sealed class WorldMapEventController : MonoBehaviour
{
    public static WorldMapEventController Instance { get; private set; }
    public static bool InputLocked => Instance != null && (Instance.inputLocked || Instance.dialogueActive);
    public bool IsPlaying { get; private set; }
    [SerializeField, InspectorName("수동 실행용 이벤트 데이터")] WorldMapEventData eventData;

    [SerializeField, InspectorName("대화 UI")] EventGuideUI dialogueUI;
    [SerializeField, InspectorName("숨김·잠금 대상 UI")] CanvasGroup[] controlledUI;
    [SerializeField, InspectorName("글리치 효과")] EventScreenGlitchController glitch;
    [SerializeField, InspectorName("글로벌 조명")] Light2D globalLight;
    [SerializeField, InspectorName("낮밤 조명 관리자")] WorldMapDayNightLightingController dayNight;
    [SerializeField, InspectorName("반짝임 화면")] Image flashImage;
    bool inputLocked, dialogueActive, uiVisible = true, lightCaptured, dayNightWasEnabled;
    Color savedLightColor;
    float savedLightIntensity;
    Coroutine lightRoutine, flashRoutine;
    readonly List<(CanvasGroup group,float alpha,bool interactable,bool raycasts)> uiSnapshot = new List<(CanvasGroup,float,bool,bool)>();

    void Awake() { if(Instance != null && Instance != this) { enabled=false; return; } Instance=this; }
    IEnumerator Start()
    {
        yield return null;
        while(WorldMapProgressController.Instance != null && !WorldMapProgressController.Instance.IsReadyForEvents) yield return null;
        var received = WorldMapEventRuntimeState.Consume();
        if(received != null) Play(received);
    }
    [ContextMenu("이벤트 실행")]
    public void PlayEvent() { Play(eventData); }
    public void Play(WorldMapEventData data)
    {
        if(!Application.isPlaying || !isActiveAndEnabled || data==null || data.steps==null || data.steps.Count==0) return;
        StopEvent();
        uiSnapshot.Clear();
        if(controlledUI!=null) foreach(var group in controlledUI) if(group!=null)
            uiSnapshot.Add((group,group.alpha,group.interactable,group.blocksRaycasts));
        IsPlaying=true;
        StartCoroutine(Run(data));
    }
    IEnumerator Run(WorldMapEventData data)
    {
        foreach(var step in data.steps)
        {
            if(step==null || !step.enabled) continue;
            switch(step.type)
            {
                case WorldMapEventStepType.Dialogue:
                    if(dialogueUI==null){Debug.LogWarning("월드맵 이벤트: 대화 UI가 연결되지 않았습니다.",this);break;}
                    dialogueActive=true; ApplyUI();
                    var pages=new List<string>();
                    int count=Mathf.Max(step.dialoguePages?.Count??0,step.localizedDialoguePages?.Count??0);
                    for(int i=0;i<count;i++){
                        string value=i<(step.dialoguePages?.Count??0)?step.dialoguePages[i]:"";
                        var localized=i<(step.localizedDialoguePages?.Count??0)?step.localizedDialoguePages[i]:null;
                        if(localized!=null&&!localized.IsEmpty){var handle=localized.GetLocalizedStringAsync();yield return handle;if(handle.Status==UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)value=handle.Result;}
                        if(!string.IsNullOrWhiteSpace(value)) pages.Add(value);
                    }
                    yield return dialogueUI.PlayDialogueRoutine(pages);
                    dialogueActive=false;ApplyUI();
                    break;
                case WorldMapEventStepType.PlayerInput: SetInputLocked(step.lockPlayerInput); break;
                case WorldMapEventStepType.UIVisibility: uiVisible=step.showUI;ApplyUI();break;
                case WorldMapEventStepType.ScreenGlitch:
                    if(glitch==null){Debug.LogWarning("월드맵 이벤트: 글리치 효과가 연결되지 않았습니다.",this);break;}
                    glitch.PlayGlitch(step.glitchStrength,step.glitchDuration);
                    if(step.waitForComplete) while(glitch!=null&&glitch.IsPlaying) yield return null;
                    break;
                case WorldMapEventStepType.GlobalLighting:
                    if(lightRoutine!=null) {StopCoroutine(lightRoutine);lightRoutine=null;}
                    if(step.changeGlobalLight && globalLight!=null){CaptureLight();lightRoutine=StartCoroutine(FadeLight(step));}
                    if(step.screenFlash && flashImage!=null){if(flashRoutine!=null)StopCoroutine(flashRoutine);flashRoutine=StartCoroutine(Flash(step));}
                    if(step.waitForComplete) {if(lightRoutine!=null)yield return lightRoutine;if(flashRoutine!=null)yield return flashRoutine;}
                    break;
                case WorldMapEventStepType.Wait: yield return new WaitForSecondsRealtime(Mathf.Max(0,step.waitDuration));break;
            }
        }
        // A non-blocking visual still completes before restoring the event's state.
        if(lightRoutine!=null)yield return lightRoutine;
        if(flashRoutine!=null)yield return flashRoutine;
        while(glitch!=null&&glitch.IsPlaying)yield return null;
        Finish(data.restoreLightingOnComplete);
    }
    public void SetInputLocked(bool locked) { inputLocked=locked;ApplyUI(); }
    void ApplyUI()
    {
        foreach(var saved in uiSnapshot) if(saved.group!=null){
            saved.group.alpha=uiVisible?saved.alpha:0;
            saved.group.interactable=uiVisible&&!InputLocked&&saved.interactable;
            saved.group.blocksRaycasts=uiVisible&&saved.raycasts;
        }
    }
    void CaptureLight()
    {
        if(lightCaptured)return;
        lightCaptured=true;savedLightColor=globalLight.color;savedLightIntensity=globalLight.intensity;
        dayNightWasEnabled=dayNight!=null&&dayNight.enabled;
        if(dayNightWasEnabled) dayNight.enabled=false;
    }
    IEnumerator FadeLight(WorldMapEventStep step)
    {
        var from=globalLight.color;float intensity=globalLight.intensity;
        for(float elapsed=0;elapsed<step.lightFadeDuration;elapsed+=Time.unscaledDeltaTime){
            float t=Mathf.SmoothStep(0,1,elapsed/step.lightFadeDuration);
            globalLight.color=Color.Lerp(from,step.lightColor,t);globalLight.intensity=Mathf.Lerp(intensity,step.lightIntensity,t);yield return null;
        }
        globalLight.color=step.lightColor;globalLight.intensity=Mathf.Max(0,step.lightIntensity);lightRoutine=null;
    }
    IEnumerator Flash(WorldMapEventStep step)
    {
        flashImage.gameObject.SetActive(true);flashImage.raycastTarget=false;
        for(int i=0;i<Mathf.Max(1,step.flashCount);i++){
            for(float elapsed=0;elapsed<Mathf.Max(.01f,step.flashDuration);elapsed+=Time.unscaledDeltaTime){
                var color=step.flashColor;color.a=step.flashStrength*(1-elapsed/Mathf.Max(.01f,step.flashDuration));flashImage.color=color;yield return null;
            }
            flashImage.color=Color.clear;
            if(i+1<step.flashCount)yield return new WaitForSecondsRealtime(Mathf.Max(0,step.flashInterval));
        }
        flashImage.gameObject.SetActive(false);flashRoutine=null;
    }
    [ContextMenu("이벤트 중단·복원")]
    public void StopEvent(){StopAllCoroutines();Finish(true);}
    void Finish(bool restoreLight)
    {
        if(IsPlaying&&dialogueUI!=null)dialogueUI.HideImmediately();
        if(glitch!=null)glitch.StopGlitch();
        if(flashImage!=null)flashImage.gameObject.SetActive(false);
        if(lightCaptured){
            if(restoreLight&&globalLight!=null){globalLight.color=savedLightColor;globalLight.intensity=savedLightIntensity;}
            Color finalColor=globalLight!=null?globalLight.color:Color.white;
            float finalIntensity=globalLight!=null?globalLight.intensity:1;
            if(dayNight!=null&&dayNightWasEnabled)dayNight.enabled=true;
            if(globalLight!=null){globalLight.color=finalColor;globalLight.intensity=finalIntensity;}
        }
        lightCaptured=false;lightRoutine=null;flashRoutine=null;
        foreach(var saved in uiSnapshot) if(saved.group!=null){saved.group.alpha=saved.alpha;saved.group.interactable=saved.interactable;saved.group.blocksRaycasts=saved.raycasts;}
        uiSnapshot.Clear();inputLocked=false;dialogueActive=false;uiVisible=true;IsPlaying=false;
    }
    void OnDisable(){StopEvent();if(Instance==this)Instance=null;}
    void OnEnable(){if(Instance==null)Instance=this;}
}
