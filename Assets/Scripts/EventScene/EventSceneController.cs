using UnityEngine;

// <변경부분>
// 공용 EventScene의 초기 세팅을 담당하는 Controller.
//
// BattleSetupManager를 사용하지 않고:
//
// EventSceneData
// → BackgroundMapData 적용
// → EventSequenceData 적용
// → EventSequence 시작
//
// 순서만 담당한다.
//
// 기물 생성 / 이동 / 공격 / 카메라 연출 같은
// 실제 이벤트 연출은 이후 EventSequence Step에서 처리한다.
public class EventSceneController : MonoBehaviour
{
    [Header("Event Scene Data")]

    // <변경부분>
    // 현재 EventScene에서 실행할 이벤트 데이터.
    //
    // 현재는 Scene 단독 테스트를 위해 Inspector에서 직접 연결한다.
    // 향후 WorldMap Event Node 연결 시 RuntimeState 전달 구조를 추가한다.
    [SerializeField]
    private EventSceneData eventSceneData;

    [Header("Scene References")]

    // EventScene의 BackgroundMapData를 실제 생성하는 기존 Manager.
    [SerializeField]
    private BackgroundManager backgroundManager;

    // Dialogue / Wait / Completion 및
    // 향후 Event Scene Step을 실행할 기존 Sequence Controller.
    [SerializeField]
    private EventSequenceController eventSequenceController;

    [Header("Startup")]

    // <변경부분>
    // Scene 단독 테스트 시 자동으로 EventScene을 초기화할지 여부.
    [SerializeField]
    private bool startAutomatically =
        true;

    private bool hasStarted =
        false;

    private void Start()
    {
        if (startAutomatically == false)
        {
            return;
        }

        StartEventScene();
    }

    // <변경부분>
    // 현재 연결된 EventSceneData를 기준으로
    // EventScene을 처음부터 시작한다.
    public void StartEventScene()
    {
        if (hasStarted)
        {
            return;
        }

        if (eventSceneData == null)
        {
            Debug.LogWarning(
                "Event Scene 시작 실패: " +
                "EventSceneData가 연결되지 않았습니다."
            );

            return;
        }

        if (eventSceneData.IsValid() == false)
        {
            Debug.LogWarning(
                $"Event Scene 시작 실패: " +
                $"{eventSceneData.name} 데이터가 유효하지 않습니다."
            );

            return;
        }

        if (backgroundManager == null)
        {
            Debug.LogWarning(
                "Event Scene 시작 실패: " +
                "BackgroundManager가 연결되지 않았습니다."
            );

            return;
        }

        if (eventSequenceController == null)
        {
            Debug.LogWarning(
                "Event Scene 시작 실패: " +
                "EventSequenceController가 연결되지 않았습니다."
            );

            return;
        }

        hasStarted =
            true;

        // <변경부분>
        // 전투 StageBattleData를 통하지 않고
        // EventSceneData의 BackgroundMapData를
        // 직접 BackgroundManager에 적용한다.
        backgroundManager.LoadMapFromData(
            eventSceneData.backgroundMapData
        );

        // <변경부분>
        // BattleSetupManager 대신
        // EventSceneData의 EventSequenceData를 직접 전달한다.
        eventSequenceController.SetSequenceData(
            eventSceneData.eventSequenceData
        );

        Debug.Log(
            $"Event Scene 세팅 완료: " +
            $"{eventSceneData.eventName}"
        );

        // <변경부분>
        // 기존 EventSequenceData의 Play Automatically 설정을 유지한다.
        //
        // false이면 데이터만 연결하고,
        // 외부에서 StartSequence()를 호출할 때까지 기다린다.
        if (eventSceneData
                .eventSequenceData
                .playAutomatically == false)
        {
            Debug.Log(
                $"Event Sequence 자동 시작 안 함: " +
                $"{eventSceneData.eventSequenceData.name}"
            );

            return;
        }

        eventSequenceController.StartSequence();
    }
}
