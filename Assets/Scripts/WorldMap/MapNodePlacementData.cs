using System;
using System.Collections.Generic;
using UnityEngine;

// 월드맵에 배치되는 노드 하나의
// 위치, 종류, 연결 관계, 스테이지 정보를 저장한다.
[Serializable]
public class MapNodePlacementData
{
    [Header("Node Identity")]
    // 맵 진행도와 노드 연결에서 사용할 고유 ID
    //
    // 예:
    // Forest_01
    // Forest_Event_01
    // Forest_Boss
    public string nodeId;

    // Inspector와 맵 에디터에서 확인할 노드 이름
    public string nodeDisplayName;

    [Header("Grid Position")]
    // 16×16 Grid 기준 노드 배치 좌표
    //
    // 맵 왼쪽 아래를 (0, 0)으로 사용하고,
    // 800×480 맵에서는 최대 (49, 29)까지 사용한다.
    public Vector2Int gridPosition;

    [Header("Node Style")]
    // 노드의 역할을 구분하는 타입
    public MapNodeType nodeType =
        MapNodeType.Battle;

    // 노드에 사용할 Sprite와 Collider 정보를 가진 스타일 데이터
    public MapNodeStyleData nodeStyleData;

    [Header("Stage Scene")]
    // 노드 클릭 시 이동할 전투 또는 이벤트 씬 이름
    public string targetSceneName;

    [Header("Initial State")]
    // 맵을 처음 시작했을 때
    // 해당 노드가 바로 선택 가능한지 여부
    public bool initiallyUnlocked;

    // 맵을 처음 시작했을 때
    // 해당 노드가 이미 클리어된 상태인지 여부
    //
    // 시작 지점은 이 값을 true로 사용한다.
    public bool initiallyCleared;

    [Header("Node Connection")]
    // 현재 노드를 클리어했을 때
    // 다음으로 해금할 노드 ID 목록
    public List<string> connectedNodeIds =
        new List<string>();

    [Header("Fog Reveal")]
    // 추후 포그 시스템에서
    // 노드 방문 시 주변을 밝힐 Grid 반경
    [Min(0)]
    public int revealRadius = 2;
}
