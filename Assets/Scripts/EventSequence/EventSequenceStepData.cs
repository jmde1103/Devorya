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
    CompleteSequence
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
    DeploymentConfirm,
    EndTurn
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

    // <변경부분> Check Remove Piece Team이 켜져 있을 때
    // 이 진영의 기물만 제거한다.
    public PieceTeam removePieceTeam =
        PieceTeam.Enemy;

    [Header("Wait")]
    // Wait 단계에서 기다릴 시간
    [Min(0f)]
    public float waitDuration = 0.5f;
}