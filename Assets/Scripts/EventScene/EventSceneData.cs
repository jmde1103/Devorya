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
    LoadScene = 2
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

    // completionType이 LoadScene일 때 이동할 Scene 이름.
    public string completionSceneName;

    // <변경부분>
    // LoadScene으로 TextCutsceneScene에 이동할 경우
    // 다음 TextCutscene에 전달할 Data.
    public TextCutsceneData completionCutsceneData;


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

        return true;
    }
}