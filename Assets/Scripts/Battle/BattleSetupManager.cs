using System.Collections.Generic; // <변경부분> RunStateManager의 플레이어 기물 목록을 가져올 때 사용
using UnityEngine;

// <변경부분> StageBattleData를 읽어서 전투 시작 구성을 세팅하는 매니저
// 보드 타일, 적 기물 편성, 승패 조건, 적 일반스킬 랜덤 부여를 한곳에서 연결한다.
public class BattleSetupManager : MonoBehaviour
{
    [Header("Stage Data")]
    // <변경부분> 현재 맵 노드에서 전달받아
    // 이번 BattleScene에서 실제로 사용할 StageBattleData.
    //
    // 일반 게임 진행에서는
    // WorldMapRuntimeState.PendingStageBattleData를 우선 사용한다.
    private StageBattleData stageBattleData;

    [Header("Direct Scene Test")]
    // <변경부분> BattleScene을 월드맵을 거치지 않고
    // Unity Editor에서 직접 Play할 때 사용할 테스트용 StageBattleData.
    //
    // 일반 게임에서는 사용되지 않으며,
    // WorldMapRuntimeState.PendingStageBattleData가 없는 경우에만 사용한다.
    [SerializeField]
    private StageBattleData directTestStageBattleData;

    [Header("Managers")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PieceManager pieceManager;
    [SerializeField] private BattleManager battleManager;

    // <변경부분> StageBattleData의 보상 데이터를 전달할 전투 종료 흐름 컨트롤러
    [SerializeField] private BattleEndFlowController battleEndFlowController;

    [Header("Run State")]
    // <변경부분> 저장된 플레이어 기물 상태가 있으면 StageBattleData의 playerFormationData 대신 사용할지 여부
    [SerializeField] private bool useRunStatePlayerPieces = true;

    private void Start()
    {
        SetupBattle();
    }

    // <변경부분> 현재 StageBattleData 기준으로 전투를 구성하는 함수
    public void SetupBattle()
    {
        // <변경부분> 일반 게임 진행에서는
        // 월드맵 전투 노드가 전달한 StageBattleData를 최우선으로 사용한다.
        stageBattleData =
            WorldMapRuntimeState
                .PendingStageBattleData;

        // <변경부분> 월드맵을 거치지 않고
        // BattleScene을 직접 Play한 테스트 상황이라면
        // Inspector에 연결된 테스트용 StageBattleData를 대신 사용한다.
        if (stageBattleData == null)
        {
            stageBattleData =
                directTestStageBattleData;

            if (stageBattleData != null)
            {
                Debug.Log(
                    $"BattleSetupManager 직접 씬 테스트: " +
                    $"{stageBattleData.name} 사용"
                );
            }
        }

        if (stageBattleData == null)
        {
            Debug.LogError(
                "BattleSetupManager 실패: " +
                "월드맵에서 전달된 StageBattleData도 없고 " +
                "Direct Test Stage Battle Data도 연결되지 않았습니다."
            );

            return;
        }

        if (IsStageBattleDataValidForSetup() == false)
        {
            Debug.LogError($"BattleSetupManager 실패: StageBattleData가 유효하지 않습니다. {stageBattleData.name}");
            return;
        }

        if (boardManager == null ||
     pieceManager == null ||
     battleManager == null ||
     battleEndFlowController == null)
        {
            Debug.LogError(
                "BattleSetupManager 실패: " +
                "BoardManager / PieceManager / BattleManager / " +
                "BattleEndFlowController 연결을 확인하세요."
            );

            return;
        }

        // <변경부분> 현재 StageBattleData의 스테이지 이름을
        // 좌측 상단 TurnInfo UI에 표시하도록 BattleManager에 전달한다.
        //
        // 스테이지가 바뀌면 동일한 BattleScene을 사용하더라도
        // 각 StageBattleData의 stageName이 자동으로 표시된다.
        battleManager.SetStageName(
            stageBattleData.stageName
        );

        // <변경부분> StageBattleData의 TileType A/B를 BoardManager에 전달해 보드를 재생성
        boardManager.RebuildBoardByTileType(
            stageBattleData.checkerTileTypeA,
            stageBattleData.checkerTileTypeB
        );

        // <변경부분> 기존 테스트/이전 기물 제거 후 현재 보드 크기에 맞춰 기물 배열 초기화
        pieceManager.ClearAllPieces();

        // <변경부분> 저장된 런 상태가 있으면 저장된 플레이어 기물을 사용하고, 없으면 기본 편성을 사용
        SpawnPlayerPieces();

        // <변경부분> PieceFormationData의 배치 방식에 따라
        // 수동 좌표 배치 또는 상대 시작 10칸 랜덤 배치를 생성한다.
        SpawnEnemyPieces();

        // <변경부분> StageBattleData의 승패 조건을 BattleManager에 전달
        battleManager.SetBattleEndCondition(
            stageBattleData.playerDefeatCondition,
            stageBattleData.enemyDefeatCondition
        );

        // <변경부분> 현재 스테이지에서 Enemy AI가
        // 고유스킬 사용을 고려할 확률을 BattleManager를 통해 전달한다.
        //
        // 0%면 Enemy AI는 고유스킬을 전혀 사용하지 않고,
        // 100%면 기존 AI 판단을 그대로 사용한다.
        battleManager.SetEnemyAIUniqueSkillUseChance(
            stageBattleData.enemyUniqueSkillUseChance
        );

        // <변경부분> 현재 스테이지의 전투 보상 데이터를 BattleEndFlowController에 전달
        battleEndFlowController.SetBattleRewardData(
            stageBattleData.battleRewardData
        );

        // <변경부분> 스테이지별 적 일반스킬 랜덤 부여 규칙 적용
        ApplyEnemyGeneralSkillGrantRules();

        Debug.Log($"전투 세팅 완료: {stageBattleData.stageName}");
    }

    // <변경부분> 현재 StageBattleData가 전투 세팅에 사용할 수 있는 상태인지 확인하는 함수
    // RunStateManager에 저장된 플레이어 기물이 있으면 playerFormationData가 없어도 플레이어 배치를 진행할 수 있다.
    private bool IsStageBattleDataValidForSetup()
    {
        if (stageBattleData == null)
        {
            return false;
        }

        bool hasRunStatePlayerPieces =
            useRunStatePlayerPieces &&
            RunStateManager.Instance != null &&
            RunStateManager.Instance.HasPlayerPieceRuntimeData;

        // <변경부분> 저장된 플레이어 기물이 없을 때만 기본 플레이어 편성 데이터가 필수
        if (hasRunStatePlayerPieces == false)
        {
            if (stageBattleData.playerFormationData == null)
            {
                return false;
            }

            if (stageBattleData.playerFormationData.IsValid() == false)
            {
                return false;
            }
        }

        // <변경부분> 적 편성은 모든 전투에서 필수
        if (stageBattleData.enemyFormationData == null)
        {
            return false;
        }

        if (stageBattleData.enemyFormationData.IsValid() == false)
        {
            return false;
        }

        return true;
    }

    // <변경부분> 저장된 플레이어 기물이 있으면 런 저장 데이터를 사용하고, 없으면 StageBattleData 기본 편성을 사용
    private void SpawnPlayerPieces()
    {
        if (useRunStatePlayerPieces &&
            RunStateManager.Instance != null &&
            RunStateManager.Instance.HasPlayerPieceRuntimeData)
        {
            List<PlayerPieceRuntimeData> playerRuntimePieces =
                RunStateManager.Instance.GetPlayerPiecesCopy();

            pieceManager.SpawnPlayerPiecesFromRuntimeData(
                playerRuntimePieces,
                false
            );

            Debug.Log($"저장된 런 상태 플레이어 기물 배치 완료: {playerRuntimePieces.Count}개");
            return;
        }

        if (stageBattleData.playerFormationData == null)
        {
            Debug.LogWarning("플레이어 기본 편성 배치 실패: playerFormationData가 없습니다.");
            return;
        }

        pieceManager.SpawnPiecesFromDataList(
            stageBattleData.playerFormationData.spawnDataList
        );

        Debug.Log($"StageBattleData 기본 플레이어 편성 배치 완료: {stageBattleData.playerFormationData.formationName}");
    }

    // <변경부분> 현재 적 PieceFormationData의 배치 방식에 따라
    // 기존 수동 좌표 배치 또는 상대 시작 진영 랜덤 배치를 실행한다.
    private void SpawnEnemyPieces()
    {
        PieceFormationData enemyFormationData =
            stageBattleData.enemyFormationData;

        if (enemyFormationData == null)
        {
            Debug.LogWarning(
                "적 기물 배치 실패: enemyFormationData가 없습니다."
            );

            return;
        }

        if (enemyFormationData.spawnMode ==
            PieceFormationSpawnMode.Manual)
        {
            // 기존 PieceFormationData의 좌표를 그대로 사용하는 수동 배치
            pieceManager.SpawnPiecesFromDataList(
                enemyFormationData.spawnDataList
            );

            Debug.Log(
                $"적 수동 편성 배치 완료: " +
                $"{enemyFormationData.formationName}"
            );

            return;
        }

        SpawnRandomEnemyStartFormation(
            enemyFormationData
        );
    }

    // <변경부분> 보드 상단 2줄을 상대 시작 진영 10칸으로 사용하고,
    // PieceFormationData에 등록된 PieceData를 설정 비율대로 뽑아
    // 서로 다른 무작위 빈칸에 Enemy 기물로 생성한다.
    //
    // 5x6 보드 기준 사용 좌표:
    // y = Height - 2, Height - 1 / 각 줄 x = 0 ~ Width - 1
    private void SpawnRandomEnemyStartFormation(
        PieceFormationData formationData)
    {
        if (formationData == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return;
        }

        List<Vector2Int> availablePositions =
            new List<Vector2Int>();

        int firstEnemyRowY =
            Mathf.Max(
                0,
                boardManager.Height - 2
            );

        // 상대 시작 영역의 빈칸만 후보로 수집한다.
        for (int y = firstEnemyRowY;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                if (pieceManager.IsEmpty(
                        x,
                        y) == false)
                {
                    continue;
                }

                availablePositions.Add(
                    new Vector2Int(x, y)
                );
            }
        }

        if (availablePositions.Count == 0)
        {
            Debug.LogWarning(
                "적 랜덤 편성 배치 실패: " +
                "상대 시작 영역에 빈칸이 없습니다."
            );

            return;
        }

        // 위치 목록을 먼저 섞어 같은 좌표가 중복 선택되지 않게 한다.
        ShufflePositions(
            availablePositions
        );

        int spawnCount =
            Mathf.Clamp(
                formationData.randomPieceCount,
                1,
                availablePositions.Count
            );

        int spawnedCount = 0;

        for (int i = 0;
             i < spawnCount;
             i++)
        {
            PieceData selectedPieceData =
                formationData.RollRandomPieceData();

            if (selectedPieceData == null)
            {
                Debug.LogWarning(
                    "적 랜덤 편성 배치 중단: " +
                    "비율 추첨 가능한 PieceData가 없습니다."
                );

                break;
            }

            Vector2Int spawnPosition =
                availablePositions[i];

            // 기존 PieceManager의 PieceData 직접 생성 함수를 사용한다.
            // 이 함수는 PieceData의 외형, 고유스킬, 기본 일반스킬까지 적용한다.
            Piece spawnedPiece =
                pieceManager.SpawnPieceFromData(
                    selectedPieceData,
                    PieceTeam.Enemy,
                    spawnPosition.x,
                    spawnPosition.y,
                    true,
                    false
                );

            if (spawnedPiece != null)
            {
                spawnedCount++;
            }
        }

        Debug.Log(
            $"적 랜덤 편성 배치 완료: " +
            $"{formationData.formationName} / " +
            $"요청 {spawnCount}개 / 생성 {spawnedCount}개"
        );
    }

    // <변경부분> 상대 시작 위치 후보를 Fisher-Yates 방식으로 섞는다.
    private void ShufflePositions(
        List<Vector2Int> positions)
    {
        if (positions == null)
        {
            return;
        }

        for (int i = positions.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            Vector2Int temporaryPosition =
                positions[i];

            positions[i] =
                positions[randomIndex];

            positions[randomIndex] =
                temporaryPosition;
        }
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

            // <변경부분> 일반스킬 레벨 없이
            // 해당 적 기물에 스킬을 중복되지 않게 부여한다.
            bool skillAdded =
                piece.AddGeneralSkill(
                    rule.skillType
                );

            if (skillAdded)
            {
                Debug.Log(
                    $"스테이지 적 일반스킬 부여: " +
                    $"{piece.PieceType} / " +
                    $"{rule.skillType}"
                );
            }
        }
    }
}