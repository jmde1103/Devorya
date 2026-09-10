using UnityEngine;

// <변경부분>
// 공용 EventScene에서 실행할 하나의 이벤트 구성을 저장한다.
//
// EventScene 자체는 하나만 유지하고,
// 이벤트마다 이 Data Asset을 교체하여:
//
// BackgroundMapData
// → EventSequenceData
//
// 순서로 이벤트를 구성한다.
[CreateAssetMenu(
    fileName = "EventSceneData",
    menuName = "Devorya/Event/Event Scene Data"
)]
public class EventSceneData : ScriptableObject
{
    [Header("Event Info")]

    // 제작자가 Inspector에서 구분하기 위한 이벤트 이름.
    public string eventName;

    [TextArea]
    // 제작 메모용 설명.
    public string description;

    [Header("Background")]

    // <변경부분>
    // EventScene에서 표시할 배경 맵.
    //
    // 전투용 5 x 6 Board를 사용하지 않고
    // 이 BackgroundMapData의 BackgroundTile 위에서
    // 이벤트 연출을 진행한다.
    public BackgroundMapData backgroundMapData;

    [Header("Event Sequence")]

    // <변경부분>
    // 실제 Dialogue / Wait / 향후 Event Actor 연출 순서를
    // 정의할 기존 EventSequenceData.
    public EventSequenceData eventSequenceData;

    // <변경부분>
    // EventScene 실행에 필요한 최소 데이터 검사.
    public bool IsValid()
    {
        if (backgroundMapData == null)
        {
            return false;
        }

        if (eventSequenceData == null)
        {
            return false;
        }

        return true;
    }
}
