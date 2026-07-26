using System.Collections.Generic;
using UnityEngine;

// <변경부분> AI 행동 후보에 점수를 부여하고
// 가장 높은 점수의 행동을 선택하는 일반 C# 클래스
// MonoBehaviour가 아니므로 GameObject에 부착하지 않는다.
public class BattleAIActionEvaluator
{
    // 일반 이동 행동의 기본 점수
    private const float MoveScore = 0f;

    // <변경부분> Player King과 거리가 한 칸 가까워질 때마다
    // 추가하는 전진 압박 점수
    private const float ForwardPressureScorePerTile =
        5f;

    // <변경부분> 같은 기물이 직전 Enemy 행동을
    // 그대로 되돌리는 즉시 왕복 행동에 적용하는 감점
    private const float ImmediateReturnPenalty =
        -120f;

    // <변경부분> 같은 기물이 최근에 방문했던 위치로
    // 다시 이동할 때 적용하는 기본 재방문 감점
    private const float RecentPositionRevisitPenalty =
        -35f;

    // <변경부분> 최근 위치 이력 안에서 같은 위치를
    // 여러 번 방문한 횟수마다 추가하는 누적 감점
    private const float RepeatedPositionVisitPenalty =
        -30f;

    // <변경부분> 기물별로 기억할 최근 도착 위치 개수
    // 너무 오래된 이동까지 계속 감점하지 않도록 제한한다.
    private const int MaxRecentPositionHistory =
        6;

    // <변경부분> 행동 후 Enemy King이 다음 턴에 잡힐 수 있을 때
    // 해당 행동이 거의 선택되지 않도록 적용하는 치명적 감점
    private const float KingThreatenedPenalty =
        -100000f;

    // 실제 보드 상태와 가상 King 위험도 판정을 제공하는 매니저
    private readonly BattleManager battleManager;

    // <변경부분> 직전 Enemy 턴에 행동한 기물
    // 같은 기물이 직전 위치로 돌아가는지 판정할 때 사용한다.
    private Piece previousActingPiece;

    // <변경부분> 직전 Enemy 행동의 출발 좌표
    private Vector2Int previousSourcePosition;

    // <변경부분> 직전 Enemy 행동의 목표 좌표
    private Vector2Int previousTargetPosition;

    // <변경부분> Enemy 기물별 최근 도착 위치 이력
    // 다른 기물이 중간에 움직여도 각 기물의 반복 이동을
    // 독립적으로 기억하기 위해 Piece별로 관리한다.
    private readonly Dictionary<Piece, List<Vector2Int>>
        recentPositionHistoryByPiece =
            new Dictionary<Piece, List<Vector2Int>>();

    // <변경부분> 평가기 생성 시 필요한 전투 참조를 한 번 전달받는다.
    public BattleAIActionEvaluator(
     BattleManager manager)
    {
        battleManager = manager;
    }

    // <변경부분> 실제 실행에 성공한 Enemy 행동을 저장한다.
    // 직전 행동 정보와 기물별 최근 위치 이력을 함께 갱신한다.
    // 평가만 하고 실행되지 않은 후보는 기록하지 않는다.
    public void SetPreviousExecutedAction(
        BattleAIAction executedAction)
    {
        if (executedAction == null ||
            executedAction.ActingPiece == null)
        {
            return;
        }

        Piece actingPiece =
            executedAction.ActingPiece;

        // 기존 즉시 왕복 판정용 직전 행동 저장
        previousActingPiece =
            actingPiece;

        previousSourcePosition =
            executedAction.SourcePosition;

        previousTargetPosition =
            executedAction.TargetPosition;

        // 해당 기물의 위치 이력이 아직 없다면 생성한다.
        if (recentPositionHistoryByPiece.TryGetValue(
                actingPiece,
                out List<Vector2Int> positionHistory) ==
            false)
        {
            positionHistory =
                new List<Vector2Int>();

            recentPositionHistoryByPiece.Add(
                actingPiece,
                positionHistory
            );

            // 최초 기록에서는 행동 전 출발 위치도 저장한다.
            // 이후 이 위치로 되돌아오는 행동을 감지할 수 있다.
            positionHistory.Add(
                executedAction.SourcePosition
            );
        }

        // 실제 행동 후 도착 위치 저장
        positionHistory.Add(
            executedAction.TargetPosition
        );

        // 오래된 위치 기록은 제거해서
        // 최근 행동 패턴만 평가하도록 제한한다.
        while (positionHistory.Count >
               MaxRecentPositionHistory)
        {
            positionHistory.RemoveAt(0);
        }
    }


    // <변경부분> 전달받은 모든 AI 행동 후보의 점수를 계산한다.
    public void EvaluateActions(
        List<BattleAIAction> actions)
    {
        // 행동 목록이 없으면 평가할 수 없다.
        if (actions == null)
        {
            Debug.LogWarning(
                "AI 행동 평가 실패: 행동 목록이 없습니다."
            );

            return;
        }

        // 모든 행동 후보를 한 번씩 평가한다.
        for (int i = 0; i < actions.Count; i++)
        {
            BattleAIAction action = actions[i];

            // 비어 있는 행동 후보는 건너뛴다.
            if (action == null)
            {
                continue;
            }

            action.Score =
                EvaluateAction(action);
        }
    }

    // <변경부분> 행동 하나의 최종 점수를 계산한다.
    private float EvaluateAction(
    BattleAIAction action)
    {
        if (action == null)
        {
            return float.MinValue;
        }

        float score;

        switch (action.ActionType)
        {
            // 빈칸 이동은 현재 단계에서 추가 가치를 부여하지 않는다.
            case BattleAIActionType.Move:
                score = MoveScore;
                break;

            // 공격은 공격 대상 기물의 종류에 따라 점수를 부여한다.
            case BattleAIActionType.Attack:
                score = EvaluateAttackAction(action);
                break;

            default:
                return float.MinValue;
        }

        // <변경부분> 행동 이후 Enemy King이 Player 공격 범위에 노출되면
        // 공격 대상 가치보다 훨씬 큰 치명적 감점을 적용한다.
        //
        // King 자체가 위험한 위치로 이동하는 행동뿐 아니라,
        // 다른 기물이 이동하여 Rook/Bishop/Queen의 공격 경로를
        // 열어버리는 행동도 함께 감지한다.
        bool isEnemyKingThreatened = battleManager != null && battleManager.IsKingThreatenedAfterAIAction(action, PieceTeam.Enemy);

        if (isEnemyKingThreatened)
        {
            score += KingThreatenedPenalty;
        }

        // <변경부분> King이 아닌 일반 행동 기물이
        // 행동 후 상대 공격 범위에 들어간다면
        // 해당 기물의 기본 가치만큼 감점한다.
        //
        // 공격으로 얻는 점수와 행동 기물을 잃는 점수를 함께 계산하므로
        // 단순 회피가 아니라 실제 교환 손익을 평가할 수 있다.
        if (action.ActingPiece != null && action.ActingPiece.PieceType != PieceType.King && battleManager != null && battleManager.IsActingPieceThreatenedAfterAIAction(
        action))
        {
            float actingPieceValue = GetPieceValue(action.ActingPiece.PieceType);
            score -= actingPieceValue;
        }

        // <변경부분> 직전 Enemy 행동을 같은 기물이
        // 즉시 되돌리는 정확한 왕복 행동이면 큰 감점을 적용한다.
        //
        // 예:
        // 이전 행동 (3, 4) → (2, 4)
        // 현재 후보 (2, 4) → (3, 4)
        if (IsImmediateReturnAction(action))
        {
            score += ImmediateReturnPenalty;
        }

        // <변경부분> 즉시 왕복이 아니더라도
        // 같은 기물이 최근 방문했던 위치로 다시 이동한다면
        // 최근 위치 이력과 반복 횟수에 따라 추가 감점을 적용한다.
        score +=
            EvaluateRecentPositionRevisitPenalty(
                action
            );

        // <변경부분> Enemy 일반 기물이 Player King과 가까워지는 행동에
        // 거리 감소량만큼 작은 전진 압박 점수를 추가한다.
        //
        // King은 무리하게 앞으로 나서는 부작용을 막기 위해 제외한다.
        // 위험한 위치로 이동하는 행동에는 기존 기물 손실 감점과
        // King 위험 감점이 먼저 적용되므로 안전 판단은 그대로 유지된다.
        score +=
            EvaluateForwardPressureScore(
                action
            );

        return score;
    }

    // <변경부분> 행동 전후 Player King과의 거리를 비교해
    // 전진 또는 후퇴에 따른 압박 점수를 계산한다.
    private float EvaluateForwardPressureScore(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null ||
            battleManager == null)
        {
            return 0f;
        }

        // Enemy King은 전진 압박 점수 대상에서 제외한다.
        // King의 이동은 생존과 안전도 평가를 우선한다.
        if (action.ActingPiece.PieceType ==
            PieceType.King)
        {
            return 0f;
        }

        int sourceDistance =
            battleManager.GetDistanceToKing(
                action.SourcePosition,
                PieceTeam.Player
            );

        int targetDistance =
            battleManager.GetDistanceToKing(
                action.TargetPosition,
                PieceTeam.Player
            );

        // Player King이 없는 전투에서는
        // 전진 압박 점수를 적용하지 않는다.
        if (sourceDistance < 0 ||
            targetDistance < 0)
        {
            return 0f;
        }

        // 양수: Player King과 가까워짐
        // 음수: Player King에게서 멀어짐
        int distanceReduction =
            sourceDistance -
            targetDistance;

        return
            distanceReduction *
            ForwardPressureScorePerTile;
    }

    // <변경부분> 현재 후보가 직전 Enemy 행동을
    // 그대로 되돌리는 즉시 왕복 행동인지 검사한다.
    private bool IsImmediateReturnAction(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null)
        {
            return false;
        }

        // 직전 행동 기록이 없거나,
        // 직전 행동 기물이 이미 제거되었다면 왕복 판정하지 않는다.
        if (previousActingPiece == null)
        {
            return false;
        }

        // 직전에 움직인 기물과 같은 기물이어야 한다.
        if (action.ActingPiece !=
            previousActingPiece)
        {
            return false;
        }

        // 현재 출발점이 직전 행동의 도착점이고,
        // 현재 도착점이 직전 행동의 출발점이면 즉시 왕복이다.
        return
            action.SourcePosition ==
            previousTargetPosition &&
            action.TargetPosition ==
            previousSourcePosition;
    }

    // <변경부분> 같은 기물이 최근 방문했던 위치로
    // 다시 이동하는 행동의 누적 감점을 계산한다.
    private float EvaluateRecentPositionRevisitPenalty(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null)
        {
            return 0f;
        }

        // 이 기물의 이전 위치 이력이 없다면
        // 반복 행동으로 볼 수 없다.
        if (recentPositionHistoryByPiece.TryGetValue(
                action.ActingPiece,
                out List<Vector2Int> positionHistory) ==
            false)
        {
            return 0f;
        }

        if (positionHistory == null ||
            positionHistory.Count == 0)
        {
            return 0f;
        }

        int visitCount = 0;

        // 현재 후보의 목표 위치를
        // 최근 위치 이력에서 몇 번 방문했는지 계산한다.
        for (int i = 0;
             i < positionHistory.Count;
             i++)
        {
            if (positionHistory[i] ==
                action.TargetPosition)
            {
                visitCount++;
            }
        }

        // 한 번도 방문하지 않은 새 위치라면 감점하지 않는다.
        if (visitCount <= 0)
        {
            return 0f;
        }

        // 최근 방문 위치로 돌아가는 기본 감점
        float penalty =
            RecentPositionRevisitPenalty;

        // 같은 위치를 여러 번 방문한 기록이 있을수록
        // 추가 누적 감점을 적용한다.
        penalty +=
            visitCount *
            RepeatedPositionVisitPenalty;

        return penalty;
    }

    // <변경부분> 공격 대상 기물의 가치에 따라 공격 점수를 계산한다.
    private float EvaluateAttackAction(
        BattleAIAction action)
    {
        // 공격 행동인데 대상 기물이 없다면 잘못된 후보이므로
        // 선택되지 않도록 매우 낮은 점수를 반환한다.
        if (action.TargetPiece == null)
        {
            return float.MinValue;
        }

        return GetPieceValue(
            action.TargetPiece.PieceType
        );
    }

    // <변경부분> 기물 타입별 기본 공격 가치를 반환한다.
    private float GetPieceValue(
        PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return 100f;

            case PieceType.Knight:
                return 300f;

            case PieceType.Bishop:
                return 300f;

            case PieceType.Rook:
                return 500f;

            case PieceType.Queen:
                return 900f;

            case PieceType.King:
                return 10000f;

            case PieceType.Special:
                return 50f;
        }

        return 0f;
    }

    // <변경부분> 가장 높은 점수의 행동들을 추려낸 뒤
    // 동점 후보 중 하나를 랜덤으로 선택한다.
    public BattleAIAction SelectBestAction(
        List<BattleAIAction> actions,
        List<BattleAIAction> bestActions)
    {
        // 원본 행동 목록이나 최고 점수 후보 목록이 없으면 선택 불가
        if (actions == null ||
            bestActions == null)
        {
            Debug.LogWarning(
                "AI 최고 점수 행동 선택 실패: 필요한 목록이 없습니다."
            );

            return null;
        }

        // 이전 턴의 동점 후보를 제거하고 목록을 재사용한다.
        bestActions.Clear();

        // 후보가 없다면 선택할 행동도 없다.
        if (actions.Count == 0)
        {
            return null;
        }

        float highestScore =
            float.MinValue;

        // 모든 행동을 순회하며 최고 점수 후보를 수집한다.
        for (int i = 0; i < actions.Count; i++)
        {
            BattleAIAction action = actions[i];

            if (action == null)
            {
                continue;
            }

            // 현재 최고 점수보다 높은 행동을 발견했다면
            // 기존 동점 후보를 모두 제거하고 새 후보만 저장한다.
            if (action.Score > highestScore)
            {
                highestScore =
                    action.Score;

                bestActions.Clear();
                bestActions.Add(action);

                continue;
            }

            // 현재 최고 점수와 같은 행동은 동점 후보로 추가한다.
            if (Mathf.Approximately(
                    action.Score,
                    highestScore))
            {
                bestActions.Add(action);
            }
        }

        // 유효한 최고 점수 후보가 없다면 선택 실패
        if (bestActions.Count == 0)
        {
            return null;
        }

        // 최고 점수가 같은 행동 중 하나를 랜덤으로 선택한다.
        int selectedIndex =
            Random.Range(
                0,
                bestActions.Count
            );

        return bestActions[selectedIndex];
    }

    // <변경부분> 점수 계산 결과를 확인하기 위한 개발용 로그
    public void DebugLogEvaluatedActions(
        List<BattleAIAction> actions)
    {
        if (actions == null)
        {
            return;
        }

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

            bool isImmediateReturn =
    IsImmediateReturnAction(
        action
    );

            float revisitPenalty =
                EvaluateRecentPositionRevisitPenalty(
                    action
                );

            Debug.Log(
                $"AI 점수 평가: " +
                $"{action.ActionType} / " +
                $"{action.ActingPiece.Team} " +
                $"{action.ActingPiece.PieceType} / " +
                $"{action.SourcePosition} → " +
                $"{action.TargetPosition} / " +
                $"대상: {targetText} / " +
                $"즉시 왕복: {isImmediateReturn} / " +
                $"재방문 감점: {revisitPenalty} / " +
                $"최종 점수: {action.Score}"
            );
        }
    }
}