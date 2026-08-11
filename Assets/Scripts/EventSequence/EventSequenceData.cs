using System.Collections.Generic;
using UnityEngine;

// <변경부분> 이벤트 시퀀스의 전체 용도를 구분한다.
//
// 실제 실행 로직은 동일하며,
// Inspector에서 데이터를 구분하고 관리하기 위한 용도다.
public enum EventSequenceMode
{
    Tutorial,
    BattleEvent,
    StoryEvent,
    Cutscene
}

// <변경부분> 이벤트 시퀀스가 완료된 뒤
// 어디로 이어질지 결정한다.
public enum EventSequenceCompletionType
{
    // 아무 처리 없이 현재 씬에 남는다.
    None,

    // 일반 로그라이크 월드맵으로 돌아간다.
    WorldMap,

    // 지정한 씬으로 바로 이동한다.
    LoadScene,

    // 일반 Battle 승리 흐름으로 전달한다.
    // 추후 실제 연결 단계에서 처리한다.
    BattleWin
}

// <변경부분> 튜토리얼 / 전투 이벤트 / 스토리 이벤트 /
// 컷씬에서 공통으로 사용할 순차 실행 데이터
//
// 기존 Battle 시스템과 분리된 독립 데이터이며,
// EventSequenceController가 존재할 때만 실행된다.
[CreateAssetMenu(
    fileName = "EventSequenceData",
    menuName = "Devorya/Event/Event Sequence Data")]
public class EventSequenceData : ScriptableObject
{
    [Header("Sequence Info")]
    // Inspector에서 확인할 이벤트 이름
    public string sequenceName;

    // 이벤트의 용도
    public EventSequenceMode sequenceMode =
        EventSequenceMode.Tutorial;

    [TextArea(2, 5)]
    // 개발자가 확인하기 위한 메모
    public string description;

    [Header("Start")]
    // 씬 준비가 완료되면 자동으로 이벤트를 시작할지 여부
    //
    // false인 경우에는 추후 다른 이벤트나
    // 특정 조건에서 수동으로 시작할 수 있다.
    public bool playAutomatically = true;

    [Header("Battle Control")]
    // 이벤트가 진행되는 동안
    // 기존 BattleManager의 일반 승패 판정을 막을지 여부
    //
    // 전투 튜토리얼에서는 일반적으로 true,
    // 일반 BattleEvent에서는 필요에 따라 false를 사용할 수 있다.
    public bool ignoreNormalBattleEnd = true;

    // <변경부분> 이벤트 시퀀스가 진행되는 동안
    // Enemy AI의 자동 행동을 일시정지할지 여부
    //
    // true:
    // 튜토리얼 / 연출이 끝날 때까지 Enemy AI 대기
    //
    // false:
    // Event Sequence가 진행 중이어도 기존 Enemy AI 정상 진행
    public bool pauseEnemyAIWhileSequenceActive = true;

    // <변경부분> Event Sequence가 자동 시작될 때
    // 기존 BattleManager의 플레이어 초기 배치 단계를 건너뛸지 여부
    //
    // true:
    // 튜토리얼 / 이벤트가 곧바로 전투 조작을 제어
    //
    // false:
    // 기존 일반 전투처럼 플레이어 배치를 먼저 진행
    public bool skipNormalPlayerDeployment = true;

    [Header("Steps")]
    // 실제 이벤트가 실행될 순서
    //
    // 위에서 아래 순서대로 EventSequenceController가 실행한다.
    public List<EventSequenceStepData> steps =
        new List<EventSequenceStepData>();

    [Header("Completion")]
    // 모든 Step이 끝났을 때 실행할 종료 방식
    public EventSequenceCompletionType completionType =
        EventSequenceCompletionType.None;

    // completionType이 LoadScene일 때 사용할 씬 이름
    public string completionSceneName;

    // <변경부분> 최소한 실행 가능한 데이터인지 확인한다.
    public bool IsValid()
    {
        if (steps == null ||
            steps.Count == 0)
        {
            return false;
        }

        if (completionType ==
                EventSequenceCompletionType.LoadScene &&
            string.IsNullOrWhiteSpace(
                completionSceneName))
        {
            return false;
        }

        return true;
    }
}