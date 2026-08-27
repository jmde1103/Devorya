using UnityEngine;

// <변경부분> 한 전투 스테이지의 전투 구성 데이터를 관리하는 ScriptableObject
// 타일 타입, 승패 조건, 적 편성, 적 일반스킬 랜덤 부여 규칙을 Stage 단위로 분리한다.
[CreateAssetMenu(fileName = "StageBattleData", menuName = "Devorya/Battle/Stage Battle Data")]
public class StageBattleData : ScriptableObject
{
    [Header("Stage Info")]
    // <변경부분> 스테이지 식별용 이름
    public string stageName;

    [TextArea]
    // <변경부분> 인스펙터에서만 확인하는 메모용 설명
    public string description;

    [Header("Event Sequence")]
    // <변경부분> 이 스테이지에서 실행할
    // 튜토리얼 / 전투 이벤트 / 스토리 이벤트 데이터.
    //
    // 일반 전투:
    // None
    //
    // 튜토리얼 / 이벤트 전투:
    // 사용할 EventSequenceData 연결
    //
    // BattleScene 자체에는 특정 튜토리얼 데이터를 고정하지 않고,
    // 현재 StageBattleData가 필요한 Sequence를 결정한다.
    public EventSequenceData eventSequenceData;

    [Header("Background Map")]
    // <변경부분> 이 스테이지에서 사용할 배경 맵 데이터.
    //
    // BackgroundManager에서 제작 후 저장한 BackgroundMapData를 연결한다.
    // BattleSetupManager가 BattleScene 진입 시 이 데이터를
    // BackgroundManager에 전달하여 실제 배경과 장식물을 다시 생성한다.
    //
    // 따라서 같은 BattleScene을 사용하더라도
    // StageBattleData마다 서로 다른 배경 맵을 사용할 수 있다.
    public BackgroundMapData backgroundMapData;

    [Header("Checker Tile Type")]
    // <변경부분> 체크무늬 A칸에 사용할 타일 타입
    // 실제 TileData는 BoardManager가 TileDatabase에서 찾아서 적용한다.
    public TileType checkerTileTypeA = TileType.Metal;

    // <변경부분> 체크무늬 B칸에 사용할 타일 타입
    // 실제 TileData는 BoardManager가 TileDatabase에서 찾아서 적용한다.
    public TileType checkerTileTypeB = TileType.MetalDark;

    [Header("Battle End Condition")]
    // <변경부분> 플레이어 진영 패배 조건
    // 기본 King 전투 예: KingDeath + AllNonKingPiecesDead
    public BattleDefeatConditionType playerDefeatCondition =
        BattleDefeatConditionType.KingDeath | BattleDefeatConditionType.AllNonKingPiecesDead;

    // <변경부분> 적 진영 패배 조건
    // King 없는 일반 전투 예: AllPiecesDead + NoActionablePieces
    public BattleDefeatConditionType enemyDefeatCondition =
        BattleDefeatConditionType.AllPiecesDead | BattleDefeatConditionType.NoActionablePieces;

    [Header("Player Formation")]
    // <변경부분> 저장된 런 기물을 사용하지 않는 스테이지에서
    // 기본적으로 생성할 플레이어 기물 편성 데이터
    public PieceFormationData playerFormationData;

    [Header("Player Run State")]
    // <변경부분> 이 스테이지에 진입할 때
    // RunStateManager에 저장된 플레이어 기물 상태를 불러올지 여부.
    //
    // 일반 전투:
    // true
    //
    // 튜토리얼 / 독립 이벤트 전투:
    // false
    public bool useRunStatePlayerPieces =
        true;

    // <변경부분> 이 스테이지에서 승리했을 때
    // 현재 플레이어 기물 상태를 RunStateManager에 저장할지 여부.
    //
    // 일반 전투:
    // true
    //
    // 튜토리얼 / 독립 이벤트 전투:
    // false
    //
    // false인 스테이지에서는 튜토리얼용 SpawnPiece,
    // 흡수, 사망, 스킬 변화 등이 기존 런의 기물 상태를 덮어쓰지 않는다.
    public bool savePlayerPiecesToRunState =
        true;

    [Header("Enemy Formation")]
    // <변경부분> 이 스테이지에서 사용할 적 기물 편성 데이터
    // 기물 10개를 StageBattleData에 직접 넣지 않고, PieceFormationData로 분리해서 재사용한다.
    public PieceFormationData enemyFormationData;

    [Header("Battle Reward")]
    // <변경부분> 이 스테이지 전투 승리 후 사용할 보상 데이터
    // 여러 StageBattleData가 같은 BattleRewardData를 공유할 수 있다.
    public BattleRewardData battleRewardData;

    [Header("Enemy AI")]
    // Enemy AI가 Enemy 턴 시작 시
    // 고유스킬 후보를 고려할지 결정하는 확률.
    //
    // 이 확률은 Enemy 턴 시작 순간 한 번만 판정하며,
    // 같은 Enemy 턴 안에서 고유스킬 사용 후 재평가하거나
    // ChanceAttack 추가 행동이 발생해도 다시 추첨하지 않는다.
    //
    // 0%:
    // 해당 스테이지의 Enemy AI는 고유스킬을 고려하지 않는다.
    //
    // 100%:
    // 사용 가능한 고유스킬 후보를 항상 전술 평가에 포함한다.
    //
    // 실제 어떤 고유스킬을 사용할지는
    // BattleAIActionEvaluator의 전술 점수로 결정하며,
    // Evaluator에서는 별도의 고유스킬 사용 확률을 다시 적용하지 않는다.
    [Range(0f, 100f)]
    public float enemyUniqueSkillUseChance =
    100f;

    [Header("Enemy General Skill Grant Rules")]
    // <변경부분> 스테이지 시작 시 적 기물에게 랜덤으로 부여할 일반스킬 규칙 목록
    // BattleSetupManager가 이 배열을 읽어서 Enemy 기물에게만 적용한다.
    public EnemyGeneralSkillGrantRule[] enemyGeneralSkillGrantRules;

    // StageBattleData 자체만 검사할 때 사용하는 기본 Validation.
    //
    // Runtime RunState가 존재한다고 가정하지 않기 때문에
    // playerFormationData까지 정상적으로 연결되어 있어야 유효한 것으로 판단한다.
    //
    // Editor 검사나 Runtime Context가 없는 외부 검사에서도
    // 기존 IsValid() 호출 방식을 그대로 유지할 수 있다.
    public bool IsValid()
    {
        return IsValid(
            false
        );
    }


    // 현재 전투 진입 상황까지 고려하여
    // StageBattleData가 실제 Battle Setup에 사용 가능한지 검사한다.
    //
    // hasRunStatePlayerPieces:
    // 현재 RunStateManager에 실제로 사용할 수 있는
    // Player 기물 데이터가 존재하는지를 BattleSetupManager가 전달한다.
    //
    // StageBattleData는 RunStateManager를 직접 참조하지 않고
    // 전투 데이터 자체의 Validation 규칙만 담당한다.
    public bool IsValid(
        bool hasRunStatePlayerPieces)
    {
        // 모든 전투 스테이지는
        // 사용할 BackgroundMapData가 반드시 필요하다.
        if (backgroundMapData == null)
        {
            return false;
        }

        // 저장된 Player RunState를 사용할 수 있는지는
        // Stage 설정과 실제 Runtime 데이터가 모두 만족해야 한다.
        bool canUseRunStatePlayerPieces =
            useRunStatePlayerPieces &&
            hasRunStatePlayerPieces;

        // 실제 사용할 RunState 기물이 없다면
        // Stage의 기본 Player Formation이 반드시 필요하다.
        if (canUseRunStatePlayerPieces == false)
        {
            if (playerFormationData == null)
            {
                return false;
            }

            if (playerFormationData.IsValid() == false)
            {
                return false;
            }
        }

        // Enemy Formation은 RunState와 관계없이
        // 모든 전투에서 반드시 필요하다.
        if (enemyFormationData == null)
        {
            return false;
        }

        if (enemyFormationData.IsValid() == false)
        {
            return false;
        }

        return true;
    }
}