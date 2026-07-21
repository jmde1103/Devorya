using System.Collections.Generic;
using UnityEngine;

// <변경부분> AI 행동 후보에 점수를 부여하고
// 가장 높은 점수의 행동을 선택하는 일반 C# 클래스
// MonoBehaviour가 아니므로 GameObject에 부착하지 않는다.
public class BattleAIActionEvaluator
{
    // 일반 이동 행동의 기본 점수
    private const float MoveScore = 0f;

    // <변경부분> 행동 후 Enemy King이 다음 턴에 잡힐 수 있을 때
    // 해당 행동이 거의 선택되지 않도록 적용하는 치명적 감점
    private const float KingThreatenedPenalty =
        -100000f;

    // 실제 보드 상태와 가상 King 위험도 판정을 제공하는 매니저
    private readonly BattleManager battleManager;

    // <변경부분> 평가기 생성 시 필요한 전투 참조를 한 번 전달받는다.
    public BattleAIActionEvaluator(
        BattleManager manager)
    {
        battleManager = manager;
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
        bool isEnemyKingThreatened =
            battleManager != null &&
            battleManager.IsKingThreatenedAfterAIAction(
                action,
                PieceTeam.Enemy
            );

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
        if (action.ActingPiece != null &&
            action.ActingPiece.PieceType != PieceType.King &&
            battleManager != null &&
            battleManager.IsActingPieceThreatenedAfterAIAction(
                action))
        {
            float actingPieceValue =
                GetPieceValue(
                    action.ActingPiece.PieceType
                );

            score -= actingPieceValue;
        }

        return score;
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

            Debug.Log(
                $"AI 점수 평가: " +
                $"{action.ActionType} / " +
                $"{action.ActingPiece.Team} " +
                $"{action.ActingPiece.PieceType} / " +
                $"{action.SourcePosition} → " +
                $"{action.TargetPosition} / " +
                $"대상: {targetText} / " +
                $"점수: {action.Score}"
            );
        }
    }
}