using UnityEngine;

// <변경부분> StageBattleData를 읽어서 전투 시작 구성을 세팅하는 매니저
// 보드 타일, 적 기물 편성, 승패 조건, 적 일반스킬 랜덤 부여를 한곳에서 연결한다.
public class BattleSetupManager : MonoBehaviour
{
    [Header("Stage Data")]
    // <변경부분> 현재 전투에 사용할 스테이지 데이터
    [SerializeField] private StageBattleData stageBattleData;

    [Header("Managers")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PieceManager pieceManager;
    [SerializeField] private BattleManager battleManager;

    private void Start()
    {
        SetupBattle();
    }

    // <변경부분> 현재 StageBattleData 기준으로 전투를 구성하는 함수
    public void SetupBattle()
    {
        if (stageBattleData == null)
        {
            Debug.LogError("BattleSetupManager 실패: StageBattleData가 연결되지 않았습니다.");
            return;
        }

        if (stageBattleData.IsValid() == false)
        {
            Debug.LogError($"BattleSetupManager 실패: StageBattleData가 유효하지 않습니다. {stageBattleData.name}");
            return;
        }

        if (boardManager == null || pieceManager == null || battleManager == null)
        {
            Debug.LogError("BattleSetupManager 실패: BoardManager / PieceManager / BattleManager 연결을 확인하세요.");
            return;
        }

        // <변경부분> StageBattleData의 TileType A/B를 BoardManager에 전달해 보드를 재생성
        boardManager.RebuildBoardByTileType(
            stageBattleData.checkerTileTypeA,
            stageBattleData.checkerTileTypeB
        );

        // <변경부분> 기존 테스트/이전 기물 제거 후 현재 보드 크기에 맞춰 기물 배열 초기화
        pieceManager.ClearAllPieces();

        // <변경부분> StageBattleData의 플레이어 편성 데이터를 기준으로 플레이어 기물 생성
        pieceManager.SpawnPiecesFromDataList(stageBattleData.playerFormationData.spawnDataList);

        // <변경부분> StageBattleData의 적 편성 데이터를 기준으로 적 기물 생성
        pieceManager.SpawnPiecesFromDataList(stageBattleData.enemyFormationData.spawnDataList);

        // <변경부분> StageBattleData의 승패 조건을 BattleManager에 전달
        battleManager.SetBattleEndCondition(
            stageBattleData.playerDefeatCondition,
            stageBattleData.enemyDefeatCondition
        );

        // <변경부분> 스테이지별 적 일반스킬 랜덤 부여 규칙 적용
        ApplyEnemyGeneralSkillGrantRules();

        Debug.Log($"전투 세팅 완료: {stageBattleData.stageName}");
    }

    // <변경부분> StageBattleData에 등록된 규칙을 기준으로 적 기물에게 일반스킬을 랜덤 부여
    private void ApplyEnemyGeneralSkillGrantRules()
    {
        EnemyGeneralSkillGrantRule[] rules = stageBattleData.enemyGeneralSkillGrantRules;

        if (rules == null || rules.Length == 0)
        {
            return;
        }

        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece piece = pieceManager.GetPieceAt(x, y);

                if (piece == null)
                {
                    continue;
                }

                if (piece.Team != PieceTeam.Enemy)
                {
                    continue;
                }

                ApplyRulesToEnemyPiece(piece, rules);
            }
        }
    }

    // <변경부분> 적 기물 하나에 일반스킬 랜덤 부여 규칙들을 독립적으로 적용
    private void ApplyRulesToEnemyPiece(Piece piece, EnemyGeneralSkillGrantRule[] rules)
    {
        if (piece == null || rules == null)
        {
            return;
        }

        for (int i = 0; i < rules.Length; i++)
        {
            EnemyGeneralSkillGrantRule rule = rules[i];

            if (rule == null)
            {
                continue;
            }

            if (rule.IsValid() == false)
            {
                continue;
            }

            if (piece.PieceType == PieceType.King && rule.allowKing == false)
            {
                continue;
            }

            if (rule.RollGrant() == false)
            {
                continue;
            }

            int level = rule.RollLevel();

            piece.SetTestGeneralSkill(rule.skillType, level);

            Debug.Log($"스테이지 적 일반스킬 부여: {piece.PieceType} / {rule.skillType} LV.{level}");
        }
    }
}