using System.Collections.Generic;
using UnityEngine;

// <변경부분> 지정한 진영의 모든 합법 이동 및 공격 후보를 생성하는 일반 C# 클래스
// 후보 생성만 담당하며 실제 이동, 공격, 애니메이션, 턴 종료는 실행하지 않는다.
public class BattleAIActionGenerator
{
    // 보드 크기와 전체 좌표 순회에 사용하는 매니저
    private readonly BoardManager boardManager;

    // 좌표별 기물 확인에 사용하는 매니저
    private readonly PieceManager pieceManager;

    // 플레이어와 AI가 공용으로 사용하는 이동 판정기
    private readonly BattleMoveValidator battleMoveValidator;

    // <변경부분> 필요한 전투 참조를 생성 시 한 번 전달받는다.
    public BattleAIActionGenerator(
        BoardManager board,
        PieceManager pieces,
        BattleMoveValidator moveValidator)
    {
        boardManager = board;
        pieceManager = pieces;
        battleMoveValidator = moveValidator;
    }

    // <변경부분> 지정한 진영의 모든 합법 행동 후보를 생성한다.
    // 전달받은 결과 목록을 Clear한 후 재사용해 불필요한 List 생성을 줄인다.
    public void GenerateActions(
        PieceTeam actingTeam,
        List<BattleAIAction> results)
    {
        // 결과를 저장할 목록이 없으면 후보 생성 불가
        if (results == null)
        {
            Debug.LogWarning(
                "AI 행동 후보 생성 실패: 결과 목록이 없습니다."
            );

            return;
        }

        // 이전 턴의 후보를 제거하고 같은 목록을 재사용
        results.Clear();

        // 필요한 참조가 하나라도 없으면 후보 생성 불가
        if (boardManager == null ||
            pieceManager == null ||
            battleMoveValidator == null)
        {
            Debug.LogWarning(
                "AI 행동 후보 생성 실패: 필요한 전투 참조가 연결되지 않았습니다."
            );

            return;
        }

        // 보드 전체를 한 번 순회한다.
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece actingPiece =
                    pieceManager.GetPieceAt(x, y);

                // 해당 좌표에 기물이 없으면 다음 좌표 검사
                if (actingPiece == null)
                {
                    continue;
                }

                // 현재 행동을 생성할 진영만 검사
                if (actingPiece.Team != actingTeam)
                {
                    continue;
                }

                // 이동 불가능한 벽이나 특수 기물은 제외
                if (actingPiece.CanMove == false)
                {
                    continue;
                }

                AddPieceActions(
                    actingPiece,
                    results
                );
            }
        }
    }

    // <변경부분> 기물 하나의 합법 좌표를 이동과 공격 행동으로 분류한다.
    private void AddPieceActions(
        Piece actingPiece,
        List<BattleAIAction> results)
    {
        if (actingPiece == null)
        {
            return;
        }

        // 플레이어 하이라이트와 같은 공용 이동 판정 결과를 사용
        List<Vector2Int> selectablePositions =
            battleMoveValidator.GetSelectablePositions(
                actingPiece
            );

        Vector2Int sourcePosition =
            new Vector2Int(
                actingPiece.X,
                actingPiece.Y
            );

        for (int i = 0;
             i < selectablePositions.Count;
             i++)
        {
            Vector2Int targetPosition =
                selectablePositions[i];

            Piece targetPiece =
                pieceManager.GetPieceAt(
                    targetPosition.x,
                    targetPosition.y
                );

            // 대상 칸이 비어 있으면 일반 이동 행동
            if (targetPiece == null)
            {
                results.Add(
                    BattleAIAction.CreateMove(
                        actingPiece,
                        sourcePosition,
                        targetPosition
                    )
                );

                continue;
            }

            // 대상 칸에 적대 기물이 있으면 공격 행동
            if (actingPiece.IsEnemyOf(targetPiece))
            {
                results.Add(
                    BattleAIAction.CreateAttack(
                        actingPiece,
                        sourcePosition,
                        targetPosition,
                        targetPiece
                    )
                );
            }
        }
    }

    // <변경부분> 개발 중 생성된 후보 수와 내용을 Console에서 확인하는 함수
    public void DebugLogActions(
        PieceTeam actingTeam,
        List<BattleAIAction> actions)
    {
        if (actions == null)
        {
            Debug.Log(
                "AI 행동 후보 테스트 실패: 행동 목록이 없습니다."
            );

            return;
        }

        int moveCount = 0;
        int attackCount = 0;

        for (int i = 0; i < actions.Count; i++)
        {
            BattleAIAction action = actions[i];

            if (action == null)
            {
                continue;
            }

            if (action.ActionType ==
                BattleAIActionType.Move)
            {
                moveCount++;
            }
            else if (action.ActionType ==
                     BattleAIActionType.Attack)
            {
                attackCount++;
            }
        }

        Debug.Log(
            $"AI 행동 후보 생성 완료: " +
            $"{actingTeam} / " +
            $"전체 {actions.Count}개 / " +
            $"이동 {moveCount}개 / " +
            $"공격 {attackCount}개"
        );

        for (int i = 0; i < actions.Count; i++)
        {
            BattleAIAction action = actions[i];

            if (action == null ||
                action.ActingPiece == null)
            {
                continue;
            }

            string targetText =
                action.TargetPiece == null
                    ? "없음"
                    : $"{action.TargetPiece.Team} " +
                      $"{action.TargetPiece.PieceType}";

            Debug.Log(
                $"AI 후보: " +
                $"{action.ActionType} / " +
                $"{action.ActingPiece.Team} " +
                $"{action.ActingPiece.PieceType} / " +
                $"{action.SourcePosition} → " +
                $"{action.TargetPosition} / " +
                $"대상: {targetText}"
            );
        }
    }
}
