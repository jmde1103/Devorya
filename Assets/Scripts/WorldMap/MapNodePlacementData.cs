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

    [Header("Battle Stage Data")]
    // 전투 노드에 진입했을 때
    // BattleScene에서 실제 전투 구성에 사용할 스테이지 데이터.
    //
    // Battle / BossBattle 노드에서는 이 값을 연결하고,
    // Event / Shop 등 전투가 아닌 노드에서는 비워둘 수 있다.
    public StageBattleData stageBattleData;

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
    // 현재 노드에서 이동할 수 있는 다음 노드와
    // 해당 노드까지 이동할 Route 좌표를 한 세트로 저장한다.
    //
    // 연결 대상과 이동 경로를 따로 관리하지 않도록
    // Connection 하나가 Target Node ID와 Route를 모두 가진다.
    public List<MapNodeConnectionData> connections =
    new List<MapNodeConnectionData>();

    [Header("Fog Reveal")]
    // 노드에 실제로 방문했을 때
    // 중심을 기준으로 완전 탐사 상태로 밝힐 Grid 반경.
    //
    // 0 = 중심 1칸
    // 1 = 3×3
    // 2 = 5×5
    [Min(0)]
    public int revealRadius = 2;
}

// 월드맵 노드 하나에서 다른 노드로 이어지는
// 연결 관계와 실제 이동 Route를 함께 저장한다.
[Serializable]
public class MapNodeConnectionData
{
    [Header("Target Node")]
    // 현재 노드에서 연결되는 목적지 Node ID
    public string targetNodeId;

    [Header("Route Grid Positions")]
    // 현재 노드와 목적지 노드 사이에서
    // Player Marker가 순서대로 통과할 Grid 좌표 목록.
    //
    // 출발 노드와 목적지 노드 좌표는 자동으로 처리하므로
    // 중간 경유점만 등록한다.
    public List<Vector2Int> routeGridPositions =
        new List<Vector2Int>();
}
