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
    // <변경부분> 현재 테스트 전투에서 사용할 플레이어 기물 편성 데이터
    // 나중에 PlayerPartyData가 생기면 이 필드는 테스트용 또는 초기 편성용으로 축소된다.
    public PieceFormationData playerFormationData;

    [Header("Enemy Formation")]
    // <변경부분> 이 스테이지에서 사용할 적 기물 편성 데이터
    // 기물 10개를 StageBattleData에 직접 넣지 않고, PieceFormationData로 분리해서 재사용한다.
    public PieceFormationData enemyFormationData;

    [Header("Enemy General Skill Grant Rules")]
    // <변경부분> 스테이지 시작 시 적 기물에게 랜덤으로 부여할 일반스킬 규칙 목록
    // BattleSetupManager가 이 배열을 읽어서 Enemy 기물에게만 적용한다.
    public EnemyGeneralSkillGrantRule[] enemyGeneralSkillGrantRules;

    // <변경부분> 스테이지 데이터가 최소 실행 가능한 상태인지 확인
    public bool IsValid()
    {
        if (playerFormationData == null)
        {
            return false;
        }

        if (playerFormationData.IsValid() == false)
        {
            return false;
        }

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