using UnityEngine;

public enum EndTimerDestinationType
{
    [InspectorName("전투 스테이지")] Battle = 0,
    [InspectorName("이벤트 씬")] Event = 1
}
public partial class RunStateManager
{
    [SerializeField, InspectorName("엔드타이머 종료 후 이동 유형")]
    private EndTimerDestinationType endTimerDestination;
    [SerializeField, InspectorName("종료 이벤트 데이터")]
    private EventSceneData endEventSceneData;
    [SerializeField, InspectorName("종료 이벤트 씬 이름")]
    private string endEventSceneName = "EventScene";
    private bool endDestinationEntered;
    private bool hasPendingEndEvent;
    private EventSceneData pendingEndEvent;
    public EndTimerDestinationType EndTimerDestination => endTimerDestination;
    public bool EndDestinationEntered => endDestinationEntered;
    public static void SetNewRunEndTimer(int cycles, int tick, StageBattleData battle, string battleScene,
        EndTimerDestinationType destination, EventSceneData eventData, string eventScene)
    {
        SetNewRunEndTimer(cycles,tick,battle,battleScene);
        Instance.endTimerDestination=destination;
        Instance.endEventSceneData=eventData;
        Instance.endEventSceneName=string.IsNullOrWhiteSpace(eventScene)?"EventScene":eventScene;
        Instance.endDestinationEntered=false;
        Instance.hasPendingEndEvent=false;
        Instance.pendingEndEvent=null;
    }
    void ResetEndDestination()
    {
        endTimerDestination=EndTimerDestinationType.Battle;
        endStageBattleData=null;endStageBattleSceneName="BattleScene";
        endEventSceneData=null;endEventSceneName="EventScene";
        endDestinationEntered=false;hasPendingEndEvent=false;pendingEndEvent=null;
        hasPendingTitleTimer=false;pendingTitleEndStage=null;pendingTitleEndScene=null;
    }
    static void ClearTransientRunState()
    {
        WorldMapRuntimeState.Clear();
        TextCutsceneRuntimeState.Clear();
        Time.timeScale=1f;
    }
    public static void ResetCurrentRun()
    {
        if(Instance!=null)Instance.ClearRunState();
        else {ClearTransientRunState();ClearPendingTitleTimer();pendingTitleEndStage=null;pendingTitleEndScene=null;}
    }
    // A boolean distinguishes explicit None from an ordinary missing payload.
    public static bool TryConsumeEndEvent(out EventSceneData data)
    {
        data=null;
        if(Instance==null || !Instance.hasPendingEndEvent)return false;
        data=Instance.pendingEndEvent;Instance.pendingEndEvent=null;Instance.hasPendingEndEvent=false;
        return true;
    }
    public bool TryPrepareEndDestination(out string scene)
    {
        scene=null;
        if(!IsEndTimerExpired || endDestinationEntered || WorldMapRuntimeState.IsDirectBattle)return false;
        string target=endTimerDestination==EndTimerDestinationType.Event?endEventSceneName:endStageBattleSceneName;
        if(string.IsNullOrWhiteSpace(target) || !Application.CanStreamedLevelBeLoaded(target))
        {Debug.LogError("엔드타이머 종료 씬을 로드할 수 없습니다: "+target);return false;}
        if(endTimerDestination==EndTimerDestinationType.Event)
        {
            WorldMapRuntimeState.ConsumePendingEventSceneData();
            pendingEndEvent=endEventSceneData;hasPendingEndEvent=true;
        }
        else if(endStageBattleData==null || !WorldMapRuntimeState.BeginDirectBattle(endStageBattleData))return false;
        endDestinationEntered=true;scene=target;return true;
    }
}
