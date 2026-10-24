using System.Collections.Generic;
using UnityEngine;

// <변경부분>
// Event Scene 완료 후 처리 방식.
//
// Battle 전용 종료 흐름은 포함하지 않는다.
public enum EventSceneCompletionType
{
    None = 0,
    WorldMap = 1,
    LoadScene = 2,
    EventScene = 3
}

// <변경부분>
// 공용 EventScene 하나에서 실행할
// 하나의 이벤트 전체 구성을 저장한다.
//
// EventSceneData 하나가 다음 정보를 모두 소유한다.
//
// - BackgroundMapData
// - Event Scene Steps
// - Completion
//
// 별도의 EventSceneSequenceData Asset을 만들지 않고
// 이 Data Asset 하나를 Event Scene의 SSOT로 사용한다.
[CreateAssetMenu(
    fileName = "EventSceneData",
    menuName = "Devorya/Event/Event Scene Data"
)]
public class EventSceneData : ScriptableObject
{
    [Header("Event Info")]

    // 제작자가 Inspector에서 구분하기 위한 이벤트 이름.
    public string eventName;

    // <변경부분>
    // Event Scene Dialogue Localization에서 사용할
    // 이 EventSceneData 전용 Stable ID.
    //
    // 실제 생성 / 편집은 이후
    // EventSceneDataEditor에서 담당한다.
    [HideInInspector]
    public string localizationId;

    [TextArea(2, 5)]
    // 제작용 설명 / 메모.
    public string description;

    [Header("Background")]

    // <변경부분>
    // EventScene에서 사용할 BackgroundMapData.
    //
    // 전투용 5 x 6 Board가 아니라
    // 이 BackgroundMapData의 BackgroundTile 좌표를 기준으로
    // Actor / Camera 연출을 진행한다.
    public BackgroundMapData backgroundMapData;

    [Header("Event UI")]

    // <변경부분>
    // Player Interaction이 Unlock 상태일 때
    // 월드맵과 동일한 Run HUD를 표시할지 결정한다.
    //
    // false:
    // 일반 이벤트처럼 Run HUD를 계속 숨긴다.
    //
    // true:
    // 상점처럼 UnlockPlayerInteraction Step 실행 시
    // Run HUD를 Fade In한다.
    public bool showRunHUDDuringPlayerInteraction =
        false;

    // <변경부분>
    // 상점 나가기 버튼을 표시할지 결정한다.
    //
    // 기본값 false:
    // 일반 이벤트에서는 버튼을 표시하지 않는다.
    //
    // true:
    // UnlockPlayerInteraction Step이 실행된 후
    // 상점 나가기 버튼을 표시한다.
    //
    // LockPlayerInteraction Step이 실행되면
    // 다시 숨긴다.
    public bool showShopExitButtonDuringPlayerInteraction =
        false;


    [Header("Event Steps")]

    // <변경부분>
    // 현재 Event Scene에서 실행할 연출 순서.
    //
    // 별도의 EventSceneSequenceData를 거치지 않고
    // EventSceneData가 직접 Step 목록을 소유한다.
    public List<EventSceneStepData> steps =
        new List<EventSceneStepData>();


    [Header("Completion")]

    // 모든 Step이 끝난 뒤 실행할 종료 방식.
    public EventSceneCompletionType completionType =
       EventSceneCompletionType.None;

    // <변경부분>
    // 월드맵 Event / RuinsEvent에서 진입한 경우,
    // Event Sequence가 정상 완료되어 Scene을 떠날 때
    // 현재 이벤트 노드를 Cleared로 처리할지 결정한다.
    //
    // true: 월드맵 복귀 시 Cleared 및 다음 연결 노드 해금.
    // false: 기존과 동일하게 노드 상태를 변경하지 않는다.
    //
    // 상점 방문과 StartBattle의 승리 처리는 별도 경로를 사용한다.
    // 기존 Asset 보호를 위해 기본값은 false.
    public bool clearCurrentWorldMapNodeOnCompletion =
        false;

    // completionType이 LoadScene일 때 이동할 Scene 이름.
    public string completionSceneName;

    // <변경부분>
    // LoadScene으로 TextCutsceneScene에 이동할 경우
    // 다음 TextCutscene에 전달할 Data.
    public TextCutsceneData completionCutsceneData;

    // <변경부분>
    // Completion Type이 EventScene일 때
    // 다음 EventScene에서 실행할 이벤트 데이터.
    //
    // 같은 EventScene을 다시 로드하더라도
    // 여기서 지정한 새로운 EventSceneData를 실행한다.
    //
    // 기존 EventSceneData 원본을 변경하지 않는다.
    [InspectorName("Completion Event Scene Data")]
    public EventSceneData completionEventSceneData;

    [InspectorName("월드맵 이벤트 데이터 (선택)")] public WorldMapEventData completionWorldMapEventData;


    // <변경부분>
    // Event Scene 실행에 필요한 최소 데이터 검사.
    public bool IsValid()
    {
        if (backgroundMapData == null)
        {
            return false;
        }

        if (steps == null ||
            steps.Count == 0)
        {
            return false;
        }

        if (completionType ==
                EventSceneCompletionType.LoadScene &&
            string.IsNullOrWhiteSpace(
                completionSceneName))
        {
            return false;
        }

        // <변경부분>
        // 다음 EventScene 데이터가 없으면
        // 씬 이동 후 Inspector 기본 데이터로 되돌아갈 수 있으므로
        // 필수 데이터로 검사한다.
        if (completionType ==
                EventSceneCompletionType.EventScene &&
            (completionEventSceneData == null ||
             completionEventSceneData == this))
        {
            return false;
        }

        return true;
    }
}