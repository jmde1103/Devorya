using System;
using System.Collections.Generic;
using UnityEngine;

// <변경부분> 이벤트 시퀀스에서 실행할
// 한 단계의 행동 종류
public enum EventSequenceStepType
{
    // 아무 작업도 하지 않음
    None,

    // 설명창에 텍스트를 표시한다.
    Dialogue,

    // 특정 기물을 선택하도록 강제한다.
    ForcePieceSelect,

    // 특정 타일을 누르도록 강제한다.
    ForceTileSelect,

    // 특정 전투 UI 버튼을 누르도록 강제한다.
    ForceButton,

    // 원하는 위치에 기물을 생성한다.
    SpawnPiece,

    // <변경부분> 지정한 위치에 존재하는 기물을 제거한다.
    //
    // Player / Enemy / Neutral 구분 없이 사용할 수 있으며,
    // 필요하면 제거 대상 Team까지 검증할 수 있다.
    RemovePiece,

    // 일정 시간 동안 기다린다.
    Wait,

    // 시퀀스를 즉시 완료한다.
    CompleteSequence,

    // <변경부분> 현재 보드에 존재하는
    // Player 기물 상태 전체를 RunState에 즉시 저장한다.
    //
    // 튜토리얼에서 Spawn / 흡수 / 변형한 결과 중
    // 실제 다음 전투에 가져갈 상태가 완성된 시점에 사용한다.
    //
    // StageBattleData의 자동 저장 설정과는 별개로
    // 이 Step이 실행되는 순간 명시적으로 한 번 저장한다.
    CommitPlayerPiecesToRunState,

    // <변경부분> EventSequence가 현재 전투 턴을
    // 기존 BattleManager의 정상 EndTurn 흐름을 통해 다음 진영으로 넘긴다.
    //
    // currentTurn을 직접 변경하지 않으며,
    // 턴 UI / 고유스킬 쿨타임 / 상태이상 /
    // AI 턴 시작 통지 등 기존 턴 전환 후처리를 그대로 사용한다.
    AdvanceBattleTurn,

    // <변경부분> 지정한 기물을 지정 좌표로
    // 자동 이동 또는 공격시킨다.
    //
    // 목표 위치가 빈 타일이면 이동,
    // 적대 기물이 있다면 공격으로 처리하며
    // 실제 판정과 실행은 BattleManager의
    // 기존 공용 전투 행동 파이프라인을 그대로 사용한다.
    ExecutePieceAction,

    // <변경부분> 지정한 기물이 현재 실제로 보유 중인
    // 고유스킬을 EventSequence에서 자동으로 사용한다.
    //
    // EventSequenceData에서 별도의 Skill Type을 지정하지 않고
    // 해당 기물의 Piece.UniqueSkill을 그대로 사용한다.
    //
    // 쿨타임 / 턴당 사용 제한 / 사망 스택 /
    // 실제 스킬 발동 조건은 기존 BattleManager와
    // BattleSkillManager의 정상 판정을 그대로 사용한다.
    ExecutePieceUniqueSkill
}


// <변경부분> 튜토리얼에서 지정할 수 있는
// 공용 전투 버튼 종류
//
// 실제 Button 오브젝트를 EventSequenceData에 직접 저장하지 않고,
// 종류만 저장해서 씬의 BattleUIController와 연결한다.
public enum EventSequenceButtonType
{
    None,

    Absorb,
    UniqueSkill,

    // <변경부분> 보드 위 기물들의
    // 타입 정보 아이콘 표시 / 숨김 버튼.
    //
    // 튜토리얼 ForceButton에서
    // 플레이어에게 타입 정보 확인 버튼을
    // 직접 누르도록 강제할 때 사용한다.
    TypeInfo,

    DeploymentConfirm,
    EndTurn
}

// <변경부분> Event Step의 마커를
// 월드 오브젝트로 표시할지,
// Canvas UI로 표시할지 결정한다.
//
// World:
// - 기물 / 타일 등 월드 대상용
// - SpriteRenderer 사용
// - 카메라 Zoom에 따라 기물과 함께 확대 / 축소
//
// UI:
// - 전투 버튼 등 Canvas 대상용
// - Image / RectTransform 사용
// - 화면 UI 크기를 그대로 유지
public enum EventMarkerDisplayType
{
    World,
    UI
}

// <변경부분> 이벤트에서 기물을 생성할 때
// PieceData의 기본 설정을 덮어쓸지 여부와
// 덮어쓸 값을 저장한다.
[Serializable]
public class EventPieceSpawnOverrideData
{
    [Header("Movement")]
    // PieceData의 기본 CanMove 대신
    // 이벤트에서 직접 이동 가능 여부를 지정할지 여부
    public bool overrideCanMove = false;

    public bool canMove = true;

    [Header("Unique Skill")]
    // PieceData의 기본 고유스킬 대신
    // 이벤트 전용 고유스킬을 사용할지 여부
    public bool overrideUniqueSkill = false;

    public UniqueSkillType uniqueSkill =
        UniqueSkillType.None;

    [Header("General Skills")]
    // false:
    // PieceData의 기본 일반스킬을 그대로 사용
    //
    // true:
    // 아래 generalSkills 목록으로 교체
    public bool overrideGeneralSkills = false;

    public List<GeneralSkillType> generalSkills =
        new List<GeneralSkillType>();

    [Header("Player Visual")]
    // Player 진영에 Enemy/Jellu PieceData를 생성하는
    // 특수 이벤트에서 흡수 후면 외형을 사용할지 여부
    public bool useAbsorbedPlayerVisual = false;
}

// <변경부분> 이벤트 시퀀스 한 단계의 설정 데이터
//
// 현재는 공용 데이터 필드를 가지고 있지만,
// 추후 CustomEditor에서 stepType에 필요한 항목만
// Inspector에 표시하도록 정리한다.
[Serializable]
public class EventSequenceStepData
{
    [Header("Basic")]
    // Inspector에서 단계의 용도를 쉽게 확인하기 위한 이름
    public string stepName;

    // 이 단계에서 실행할 행동
    public EventSequenceStepType stepType =
        EventSequenceStepType.None;

    [Header("Dialogue")]
    // Dialogue 단계에서 순서대로 표시할 문장 목록
    //
    // 한 Step에 여러 페이지를 넣을 수 있다.
    [TextArea(2, 6)]
    public List<string> dialoguePages =
        new List<string>();

    [Header("Piece Target")]
    // ForcePieceSelect에서 사용할 대상 좌표
    public Vector2Int targetPiecePosition =
        Vector2Int.zero;

    // 특정 진영의 기물만 허용할 때 사용
    public PieceTeam targetPieceTeam =
        PieceTeam.Player;

    [Header("Tile Target")]
    // ForceTileSelect에서 사용할 타일 좌표
    public Vector2Int targetTilePosition =
        Vector2Int.zero;

    [Header("Button Target")]
    // ForceButton에서 사용할 버튼 종류
    public EventSequenceButtonType targetButton =
        EventSequenceButtonType.None;

    [Header("Marker")]
    // 현재 단계에서 목표 위치에
    // 튜토리얼 마커를 표시할지 여부
    public bool showMarker = true;

    // <변경부분> 현재 Step에서 사용할 마커 표시 방식.
    //
    // World:
    // 기물 / 타일처럼 월드 공간에 존재하는 대상을 가리킬 때 사용.
    //
    // UI:
    // 전투 버튼처럼 Canvas에 존재하는 대상을 가리킬 때 사용.
    public EventMarkerDisplayType markerDisplayType =
        EventMarkerDisplayType.World;

    // <변경부분> 현재 Step에서만 적용할 마커 위치 Offset.
    //
    // World Marker일 경우:
    // 월드 좌표 단위.
    // 예: (0, 0.35)
    //
    // UI Marker일 경우:
    // Canvas 화면 좌표 단위.
    // 예: (0, 50)
    //
    // 각 기물 / 타일 / 버튼마다 마커 높이를
    // 독립적으로 조절할 수 있다.
    public Vector2 markerPositionOffset =
        Vector2.zero;

    [Header("Spawn Piece")]
    // SpawnPiece 단계에서 생성할 PieceData
    public PieceData spawnPieceData;

    // Player / Enemy / Neutral 모두 직접 지정 가능
    public PieceTeam spawnPieceTeam =
        PieceTeam.Enemy;

    // 생성 좌표
    public Vector2Int spawnPosition =
        Vector2Int.zero;

    // PieceData 기본 설정을 변경해야 할 때만 사용
    public EventPieceSpawnOverrideData spawnOverride =
        new EventPieceSpawnOverrideData();

    [Header("Remove Piece")]
    // <변경부분> RemovePiece Step에서 제거할 기물의 보드 좌표
    //
    // 기본적으로 이 좌표에 존재하는 기물을
    // 진영과 종류에 관계없이 제거한다.
    public Vector2Int removePiecePosition =
        Vector2Int.zero;

    // <변경부분> 제거 전에 대상 기물의 Team까지 확인할지 여부
    //
    // 일반 이벤트에서는 false로 두고 좌표만 지정하면 되며,
    // 잘못된 기물 제거를 방지해야 할 때만 true로 사용한다.
    public bool checkRemovePieceTeam =
        false;

    public PieceTeam removePieceTeam =
    PieceTeam.Enemy;

    [Header("Execute Piece Action")]

    // <변경부분> 자동으로 행동시킬 기물의 진영.
    //
    // 현재 목적은 Enemy Tutorial 행동이지만
    // EventSequence 공용 기능으로 Player / Enemy 모두 지정 가능하다.
    public PieceTeam actionPieceTeam =
        PieceTeam.Enemy;

    // <변경부분> 자동 행동시킬 기물의
    // 현재 보드 좌표.
    public Vector2Int actionPiecePosition =
        Vector2Int.zero;

    // <변경부분> 해당 기물이 이동 또는 공격할 목표 좌표.
    //
    // 빈 타일 = 이동
    // 적대 기물 위치 = 공격
    public Vector2Int actionTargetPosition =
    Vector2Int.zero;

    [Header("Execute Piece Unique Skill")]

    // <변경부분> 고유스킬을 자동 사용할 기물의 진영.
    //
    // 현재 Tutorial에서는 주로 Enemy를 사용하지만
    // EventSequence 공용 기능으로 Player도 지정할 수 있다.
    public PieceTeam uniqueSkillPieceTeam =
        PieceTeam.Enemy;

    // <변경부분> 고유스킬을 사용할 기물의
    // 현재 보드 좌표.
    //
    // 이 좌표의 실제 Piece.UniqueSkill을 읽어 사용한다.
    public Vector2Int uniqueSkillPiecePosition =
        Vector2Int.zero;

    [Header("Wait")]
    // Wait 단계에서 기다릴 시간
    [Min(0f)]
    public float waitDuration = 0.5f;
}