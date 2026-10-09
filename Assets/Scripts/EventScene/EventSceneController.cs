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
    // WorldMap Event Node에서 진입한 경우:
    // WorldMapRuntimeState의 Pending EventSceneData를 우선 사용한다.
    //
    // Pending 데이터가 없는 경우:
    // Inspector 연결값을 사용하여 Scene 단독 테스트를 지원한다.
    [SerializeField]
    private EventSceneData eventSceneData;

    [Header("Scene References")]

    // EventScene의 BackgroundMapData를 실제 생성하는 기존 Manager.
    [SerializeField]
    private BackgroundManager backgroundManager;

    // <변경부분>
    // Battle / Tutorial용 EventSequenceController가 아니라
    // Event Scene 전용 Sequence Controller를 사용한다.
    [SerializeField]
    private EventSceneSequenceController
        eventSceneSequenceController;

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

        // <변경부분>
        // WorldMap의 Event / RuinsEvent / Shop 노드에서
        // 전달된 EventSceneData가 있다면 가장 먼저 사용한다.
        //
        // Consume 방식으로 한 번 읽은 뒤 RuntimeState에서 제거하여
        // 이전 이벤트 데이터가 다음 EventScene에 재사용되지 않게 한다.
        EventSceneData runtimeEventSceneData =
            WorldMapRuntimeState
                .ConsumePendingEventSceneData();

        if (runtimeEventSceneData != null)
        {
            eventSceneData =
                runtimeEventSceneData;

            Debug.Log(
                $"Event Scene Runtime Data 적용: " +
                $"{eventSceneData.name}"
            );
        }

        // Runtime 전달 데이터가 없는 경우에는
        // 기존 Inspector 연결값을 그대로 사용한다.
        //
        // 따라서 EventScene 단독 테스트 방식도 유지된다.
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

        // <변경부분>
        // Event Scene 전용 Controller 연결 검사.
        if (eventSceneSequenceController == null)
        {
            Debug.LogWarning(
                "Event Scene 시작 실패: " +
                "EventSceneSequenceController가 연결되지 않았습니다."
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

        Debug.Log(
            $"Event Scene 세팅 완료: " +
            $"{eventSceneData.eventName}"
        );

        // <변경부분>
        // EventSceneData 하나가
        // Background / Steps / Completion을 모두 소유하는 SSOT다.
        //
        // 별도의 EventSceneSequenceData를 거치지 않고
        // 현재 EventSceneData 자체를 Sequence Controller에 전달한다.
        eventSceneSequenceController.StartSequence(
            eventSceneData
        );
    }
}
