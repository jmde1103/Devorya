using System.Collections.Generic;
using UnityEngine;

// <변경부분> AI 행동 후보에 점수를 부여하고
// 가장 높은 점수의 행동을 선택하는 일반 C# 클래스
// MonoBehaviour가 아니므로 GameObject에 부착하지 않는다.
public class BattleAIActionEvaluator
{
    // 일반 이동 행동의 기본 점수
    private const float MoveScore = 0f;

    // <변경부분> 젤루 합성은 Pawn을 상위 기물로 바꾸는
    // 핵심 성장 스킬이므로 기본적으로 적극적으로 평가한다.
    //
    // 주변 재료가 Pawn이나 Neutral처럼 저가치이고,
    // 합성 후 시전자가 즉시 공격받지 않는다면
    // 일반 이동보다 높은 우선순위를 갖도록 설정한다.
    private const float JelluSynthesisBaseScore =
        150f;

    // <변경부분> 합성 후 승급 기물이 즉시 공격받지 않을 때
    // 합성을 적극적으로 선택하도록 부여하는 안전 보너스
    private const float SafeSynthesisPromotionScore =
        250f;

    // <변경부분> 합성 후 승급 기물이 바로 공격받는 경우
    // 무리한 합성을 피하도록 적용하는 위험 감점
    private const float ThreatenedSynthesisPromotionPenalty =
        -450f;

    // <변경부분> 재료 하나가 무작위로 뽑혔을 때의 가치 조정값
    //
    // Pawn과 Neutral Special이 많으면 합성 손실 위험이 낮고,
    // Knight/Bishop/Rook이 많으면 무작위 희생 위험이 높다.
    private const float NeutralSpecialMaterialScore =
        100f;

    private const float PawnMaterialScore =
        50f;

    private const float AlliedSpecialMaterialScore =
        0f;

    private const float KnightMaterialPenalty =
        -300f;

    private const float BishopMaterialPenalty =
        -300f;

    private const float RookMaterialPenalty =
        -500f;

    private const float QueenMaterialPenalty =
     -900f;

    // <변경부분> Enemy 조합에 젤루 합성 사용자가 있을 때
    // 중립 젤루 Special 공격에 적용하는 기본 감점
    //
    // 중립 젤루는 공격 대상이기도 하지만
    // JelluSynthesis의 재료로 사용할 수 있으므로,
    // 합성 조합에서는 일반 이동보다 낮게 평가해 보존한다.
    private const float NeutralJelluAttackPenaltyWithSynthesis =
        -300f;

    // <변경부분> 합성 사용자가 없는 조합에서
    // 중립 젤루 Special을 공격할 때 부여하는 낮은 점수
    //
    // 공격 자체는 허용하지만,
    // 이동 불가능한 중립 기물을 Pawn이나 다른 전투 기물보다
    // 먼저 제거하지 않도록 일반 이동과 비슷한 수준으로 평가한다.
    private const float NeutralJelluAttackScoreWithoutSynthesis =
        10f;

    // <변경부분> 해당 중립 젤루가 현재 합성 Pawn 인접 위치에 있어
    // 즉시 합성 재료로 사용할 수 있을 때 적용하는 추가 감점
    //
    // 단순히 나중에 활용할 수 있는 중립 기물보다
    // 현재 바로 합성 가능한 재료를 더 강하게 보존한다.
    private const float ImmediatelyUsableNeutralJelluPenalty =
        -250f;

    // <변경부분> 인접한 합성 재료 후보 중
    // Knight, Bishop, Rook이 2개 이상 존재할 때 적용하는 추가 감점
    //
    // 실제 재료가 무작위로 선택되므로,
    // 상급 기물이 여러 개 인접해 있으면 중요한 기물을 잃을 위험이 크다.
    //
    // 단, 합성 후 즉시 King을 공격하는 등
    // 압도적인 전술 이득이 있으면 이 감점을 넘어설 수 있도록
    // float.MinValue가 아닌 일반 점수 감점으로 처리한다.
    private const float MultipleAdvancedMaterialPenalty =
        -700f;

    // <변경부분> 퇴화를 사용하면 실행할 가치가 있는
    // 위험한 Knight 행동을 발견했을 때 부여하는 최소 스킬 점수
    private const float DegenerationOpportunityBaseScore =
        450f;

    // <변경부분> 퇴화 상태의 Knight가 행동 후 공격받을 때 적용하는 감점
    //
    // 퇴화 사망 시 더 이상 아군 Pawn이 생성되지 않고
    // 양쪽 모두 공격할 수 있는 중립 Special이 생성된다.
    //
    // 따라서 일반 Knight 사망 손실 -300보다는 조금 완화하지만,
    // 중립 장애물을 만들기 위한 무리한 자살은 피하도록 설정한다.
    private const float DegenerationThreatenedActionPenalty =
        -220f;

    // <변경부분> 퇴화를 먼저 사용할 가치가 있다고 판단한 행동이
    // 현재 최고 일반 행동보다 확실히 우선되도록 추가하는 점수
    private const float DegenerationOpportunityPriorityBonus =
        100f;

    // <변경부분> 퇴화 Knight가 현재 이미 상대 공격 범위에 있고,
    // 이번 턴 최고 일반 행동은 다른 기물의 행동일 때
    // 선제적으로 퇴화를 사용할 확률
    private const float DegenerationCurrentThreatUseChance =
        0.8f;

    // <변경부분> 위 조건을 만족해 퇴화를 사용하기로 결정했을 때
    // 현재 최고 일반 행동보다 먼저 선택되도록 추가하는 점수
    private const float DegenerationCurrentThreatPriorityBonus =
        75f;

    // <변경부분> 젤루 킹이 안전하고
    // 실제 증식 조건을 만족할 때 증식을 선택할 확률
    //
    // 증식 사용 후에도 Enemy 턴이 유지되므로
    // 다른 기물의 이동이나 공격을 이어갈 수 있다.
    private const float JelluMultiplyUseChance =
        0.95f;

    // <변경부분> 안전한 상황에서 증식을 높은 우선순위로
    // 평가하기 위한 기본 점수
    //
    // Pawn, Knight, Bishop 일반 공격보다 높지만
    // Rook, Queen, King 처치 같은 중요한 공격보다 낮게 설정한다.
    private const float JelluMultiplyBaseScore =
        420f;

    // <변경부분> 현재 최고 일반 행동보다
    // 증식이 조금 더 높은 점수를 갖도록 추가하는 우선 보너스
    private const float JelluMultiplyPriorityBonus =
        80f;

    // <변경부분> Defence 상태의 공격 대상을 앞에 둔 젤루 룩이
    // 뿔 박치기를 먼저 사용하도록 부여하는 최소 점수
    private const float HornHeadbuttBaseScore =
        650f;

    // <변경부분> 현재 최고 일반 행동보다 뿔 박치기를
    // 우선 선택하도록 추가하는 점수
    private const float HornHeadbuttPriorityBonus =
        120f;

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

    // <변경부분> 젤루 합성 후 Knight/Bishop/Rook으로
    // 승급했을 때의 가상 공격 범위를 계산하는 이동 판정기
    private readonly BattleMoveValidator battleMoveValidator;

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

    // <변경부분> 평가기 생성 시 일반 전투 위험도 판정용 BattleManager와
    // 합성 승급 공격 범위 판정용 BattleMoveValidator를 전달받는다.
    public BattleAIActionEvaluator(
        BattleManager manager,
        BattleMoveValidator moveValidator)
    {
        battleManager = manager;
        battleMoveValidator = moveValidator;
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
    //
    // 처리 순서:
    // 1. 퇴화와 증식을 제외한 일반 행동 및 다른 스킬 평가
    // 2. 현재 최고 이동·공격 행동 확인
    // 3. 퇴화의 위험 대응 가치 평가
    // 4. 증식의 안전한 전력 확장 가치 평가
    public void EvaluateActions(
        List<BattleAIAction> actions)
    {
        if (actions == null)
        {
            Debug.LogWarning(
                "AI 행동 평가 실패: 행동 목록이 없습니다."
            );

            return;
        }

        // <변경부분> 퇴화와 증식은
        // 최고 일반 행동이 무엇인지 확인한 뒤 평가해야 한다.
        for (int i = 0;
             i < actions.Count;
             i++)
        {
            BattleAIAction action =
                actions[i];

            if (action == null)
            {
                continue;
            }

            if (IsJelluDegenerationAction(
         action) ||
     IsJelluMultiplyAction(
         action) ||
     IsHornHeadbuttAction(
         action))
            {
                action.Score =
                    float.MinValue;

                continue;
            }

            action.Score =
                EvaluateAction(
                    action
                );
        }

        BattleAIAction bestNormalAction =
            FindBestNormalAction(
                actions
            );

        // <변경부분> 일반 행동 평가가 끝난 뒤
        // 퇴화와 증식을 각각 전용 조건으로 평가한다.
        for (int i = 0;
             i < actions.Count;
             i++)
        {
            BattleAIAction action =
                actions[i];

            if (action == null)
            {
                continue;
            }

            if (IsJelluDegenerationAction(
                    action))
            {
                action.Score =
                    EvaluateJelluDegenerationAction(
                        action,
                        actions,
                        bestNormalAction
                    );

                continue;
            }

            if (IsJelluMultiplyAction(
        action))
            {
                action.Score =
                    EvaluateJelluMultiplyAction(
                        action,
                        bestNormalAction
                    );

                continue;
            }

            if (IsHornHeadbuttAction(
                    action))
            {
                action.Score =
                    EvaluateHornHeadbuttAction(
                        action,
                        bestNormalAction
                    );
            }
        }
    }

    // <변경부분> 전달받은 행동이
    // 젤루 퇴화 고유스킬 후보인지 확인한다.
    private bool IsJelluDegenerationAction(
        BattleAIAction action)
    {
        if (action == null)
        {
            return false;
        }

        return
            action.ActionType ==
                BattleAIActionType.UniqueSkill &&
            action.UniqueSkillType ==
                UniqueSkillType.JelluDegeneration;
    }

    // <변경부분> 전달받은 행동이
    // 젤루 킹의 증식 고유스킬 후보인지 확인한다.
    private bool IsJelluMultiplyAction(
        BattleAIAction action)
    {
        if (action == null)
        {
            return false;
        }

        return
            action.ActionType ==
                BattleAIActionType.UniqueSkill &&
            action.UniqueSkillType ==
                UniqueSkillType.JelluMultiply;
    }

    // <변경부분> 전달받은 행동이
    // 젤루 룩의 뿔 박치기 고유스킬 후보인지 확인한다.
    private bool IsHornHeadbuttAction(
        BattleAIAction action)
    {
        if (action == null)
        {
            return false;
        }

        return
            action.ActionType ==
                BattleAIActionType.UniqueSkill &&
            action.UniqueSkillType ==
                UniqueSkillType.HornHeadbutt;
    }

    // <변경부분> 현재 후보 중 이동 또는 공격 행동만 비교해
    // AI가 실제로 선택할 가능성이 가장 높은 일반 행동을 반환한다.
    //
    // 고유스킬은 제외한다.
    // 따라서 합성이나 다른 고유스킬 점수 때문에
    // 퇴화 필요 여부가 왜곡되지 않는다.
    private BattleAIAction FindBestNormalAction(
        List<BattleAIAction> actions)
    {
        if (actions == null ||
            actions.Count == 0)
        {
            return null;
        }

        BattleAIAction bestNormalAction =
            null;

        float highestNormalScore =
            float.MinValue;

        for (int i = 0;
             i < actions.Count;
             i++)
        {
            BattleAIAction action =
                actions[i];

            if (action == null)
            {
                continue;
            }

            // 퇴화는 Knight가 실제로 이동하거나 공격하려는 상황에만
            // 필요하므로 이동과 공격 후보만 비교한다.
            if (action.ActionType !=
                    BattleAIActionType.Move &&
                action.ActionType !=
                    BattleAIActionType.Attack)
            {
                continue;
            }

            if (bestNormalAction == null ||
                action.Score >
                highestNormalScore)
            {
                bestNormalAction =
                    action;

                highestNormalScore =
                    action.Score;
            }
        }

        return bestNormalAction;
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
            // 빈칸 이동은 현재 단계에서
            // 별도의 기본 가치를 부여하지 않는다.
            case BattleAIActionType.Move:
                score = MoveScore;
                break;

            // 공격은 공격 대상 기물의 종류에 따라
            // 기본 공격 점수를 부여한다.
            case BattleAIActionType.Attack:
                score =
                    EvaluateAttackAction(
                        action
                    );
                break;

            // <변경부분> 고유스킬 행동은
            // 스킬 종류별 전용 평가 함수로 전달한다.
            case BattleAIActionType.UniqueSkill:
                score =
                    EvaluateUniqueSkillAction(
                        action
                    );

                // 고유스킬은 일반 이동 행동이 아니므로
                // 아래의 이동 후 위험도, 재방문, 전진 압박 점수를
                // 현재 단계에서는 적용하지 않는다.
                //
                // 젤루 합성은 시전자가 이동하지 않고
                // 재료 두 개가 제거되는 별도의 보드 변화이므로,
                // 이후 합성 전용 가상 보드 평가에서 따로 처리한다.
                return score;

            // 알 수 없는 행동 타입은
            // 선택되지 않도록 최저 점수를 반환한다.
            default:
                return float.MinValue;
        }

        // <변경부분> 행동 이후 Enemy King이
        // Player 공격 범위에 노출되면 치명적 감점을 적용한다.
        //
        // King 자체가 위험한 위치로 이동하는 행동뿐 아니라,
        // 다른 기물이 이동하여 공격 경로를 열어버리는 행동도 감지한다.
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
        // 행동 후 상대 공격 범위에 들어가는지 검사한다.
        if (action.ActingPiece != null &&
            action.ActingPiece.PieceType != PieceType.King &&
            battleManager != null &&
            battleManager.IsActingPieceThreatenedAfterAIAction(
                action
            ))
        {
            Piece actingPiece =
                action.ActingPiece;

            // <변경부분> 퇴화 상태의 젤루 Knight는
            // 사망 시 Pawn을 남기는 전략적 가치가 있으므로
            // 일반 기물보다 피격 위험 감점을 크게 완화한다.
            if (actingPiece.PieceType ==
                    PieceType.Knight &&
                actingPiece.HasSpeciesTag(
                    PieceSpeciesTag.Jellu) &&
                actingPiece.HasStatusEffect(
                    StatusEffectType.Degeneration))
            {
                score +=
                    DegenerationThreatenedActionPenalty;
            }
            else
            {
                float actingPieceValue =
                    GetPieceValue(
                        actingPiece.PieceType
                    );

                score -=
                    actingPieceValue;
            }
        }

        // 같은 기물이 직전 행동을 정확하게 되돌리는
        // 즉시 왕복 행동이면 큰 감점을 적용한다.
        if (IsImmediateReturnAction(action))
        {
            score += ImmediateReturnPenalty;
        }

        // 최근에 방문했던 위치로 다시 이동하면
        // 방문 횟수에 따라 누적 감점을 적용한다.
        score +=
            EvaluateRecentPositionRevisitPenalty(
                action
            );

        // Player King과 가까워지는 일반 기물 행동에
        // 작은 전진 압박 점수를 적용한다.
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

    // <변경부분> 고유스킬 행동의 종류를 확인하고
    // 해당 스킬 전용 평가 함수로 전달한다.
    private float EvaluateUniqueSkillAction(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null)
        {
            return float.MinValue;
        }

        switch (action.UniqueSkillType)
        {
            case UniqueSkillType.JelluSynthesis:
                return EvaluateJelluSynthesisAction(
                    action
                );

            case UniqueSkillType.JelluDegeneration:
                // 퇴화는 일반 행동 평가가 끝난 뒤
                // 위험한 Knight 행동과 함께 별도로 계산한다.
                return float.MinValue;

            case UniqueSkillType.JelluMultiply:
                // <변경부분> 증식은 일반 행동 평가가 끝난 뒤
                // King 안전과 긴급 회피 행동을 확인하고 별도로 계산한다.
                return float.MinValue;
            case UniqueSkillType.HornHeadbutt:
                // <변경부분> 뿔 박치기는 Defence 공격 대상과
                // 현재 최고 일반 행동을 확인한 뒤 별도로 계산한다.
                return float.MinValue;
        }

        // 아직 AI 평가가 구현되지 않은 고유스킬은
        // 후보가 생성되더라도 선택하지 않는다.
        return float.MinValue;
    }

    // <변경부분> 젤루 룩의 뿔 박치기를 지금 사용할지 평가한다.
    //
    // 후보 생성 단계에서 이미 다음 조건을 확인한다.
    // 1. 시전자가 HornHeadbutt를 보유한 Rook
    // 2. 현재 타일이 Water 또는 Swamp
    // 3. 현재 공격 가능한 대상 중 Defence 보유자가 존재
    //
    // 스킬 사용 후 Enemy 턴이 유지되므로
    // Breakthrough를 얻은 뒤 공격 후보를 다시 평가한다.
    private float EvaluateHornHeadbuttAction(
        BattleAIAction hornHeadbuttAction,
        BattleAIAction bestNormalAction)
    {
        if (hornHeadbuttAction == null ||
            hornHeadbuttAction.ActingPiece == null)
        {
            return float.MinValue;
        }

        Piece jelluRook =
            hornHeadbuttAction.ActingPiece;

        if (jelluRook.PieceType !=
                PieceType.Rook ||
            jelluRook.UniqueSkill !=
                UniqueSkillType.HornHeadbutt)
        {
            return float.MinValue;
        }

        float bestNormalScore =
            bestNormalAction == null
                ? 0f
                : bestNormalAction.Score;

        // Defence 대상 공격보다 스킬을 먼저 사용하도록
        // 현재 최고 일반 행동보다 확실히 높은 점수를 준다.
        return Mathf.Max(
            HornHeadbuttBaseScore,
            bestNormalScore +
            HornHeadbuttPriorityBonus
        );
    }

    // <변경부분> 젤루 킹의 증식을 지금 사용할지 평가한다.
    //
    // 증식 사용 조건:
    // 1. 시전자가 Jellu King이어야 한다.
    // 2. 증식 가능한 인접 빈칸이 있어 후보가 생성되어야 한다.
    // 3. 고유스킬 쿨타임 및 턴당 사용 조건을 충족해야 한다.
    // 4. 위 조건을 만족하면 높은 확률로 증식을 먼저 사용한다.
    //
    // 증식 사용 후 Enemy 턴은 종료되지 않으므로
    // Pawn 생성 후 이동, 공격 또는 King 회피 행동을 이어갈 수 있다.
    private float EvaluateJelluMultiplyAction(
        BattleAIAction multiplyAction,
        BattleAIAction bestNormalAction)
    {
        if (multiplyAction == null ||
            multiplyAction.ActingPiece == null ||
            battleManager == null ||
            battleMoveValidator == null)
        {
            return float.MinValue;
        }

        Piece jelluKing =
            multiplyAction.ActingPiece;
        // <변경부분> 증식 평가는 King 타입과
        // 실제 보유 고유스킬 JelluMultiply를 기준으로 검증한다.
        //
        // 후보 생성과 실제 스킬 실행 기준을 동일하게 맞춰,
        // 종족 태그 누락만으로 증식 점수가 제외되지 않도록 한다.
        if (jelluKing.PieceType !=
                PieceType.King ||
            jelluKing.UniqueSkill !=
                UniqueSkillType.JelluMultiply)
        {
            return float.MinValue;
        }

        // <변경부분> 증식은 사용 후 Enemy 턴을 종료하지 않는다.
        //
        // 따라서 King이 현재 위험하거나 이동해야 하는 상황이어도
        // 증식을 먼저 사용한 뒤 같은 턴에 King 이동,
        // 공격자 제거 또는 다른 회피 행동을 이어갈 수 있다.
        //
        // 증식 후보 생성 조건과 고유스킬 사용 가능 조건만 충족하면
        // 킹의 현재 위험 여부와 관계없이 확률 평가를 진행한다.

        // 조건을 만족하면 높은 확률로 증식을 먼저 사용한다.
        if (Random.value >
            JelluMultiplyUseChance)
        {
            return float.MinValue;
        }

        float bestNormalScore =
            bestNormalAction == null
                ? 0f
                : bestNormalAction.Score;

        // 일반 행동보다 높은 점수를 부여하여
        // 증식을 먼저 사용한 뒤 행동 후보를 다시 평가한다.
        return Mathf.Max(
            JelluMultiplyBaseScore,
            bestNormalScore +
            JelluMultiplyPriorityBonus
        );
    }

    // <변경부분> 퇴화를 사용하면 실행할 가치가 생기는
    // 젤루 Knight의 위험한 이동·공격 행동을 찾는다.
    //
    // 기존 최종 점수는 Knight 사망 위험으로 -300이 적용되어 있으므로,
    // 가상 퇴화 평가에서는 그 감점을 다시 복원하여
    // 행동 자체의 공격 가치와 전진 가치를 비교한다.
    //
    // 퇴화를 사용해도 가치 없는 무의미한 위험 이동이라면
    // 스킬을 사용하지 않는다.
    private float EvaluateJelluDegenerationAction(
        BattleAIAction degenerationAction,
        List<BattleAIAction> allActions,
        BattleAIAction bestNormalAction)
    {
        if (degenerationAction == null ||
            degenerationAction.ActingPiece == null ||
            allActions == null ||
            battleManager == null)
        {
            return float.MinValue;
        }

        Piece degenerationKnight =
            degenerationAction.ActingPiece;

        // 퇴화는 젤루 Knight 전용이다.
        if (degenerationKnight.PieceType !=
                PieceType.Knight ||
            degenerationKnight.HasSpeciesTag(
                PieceSpeciesTag.Jellu) ==
            false)
        {
            return float.MinValue;
        }

        // 이미 퇴화 상태라면 다시 사용하지 않는다.
        if (degenerationKnight.HasStatusEffect(
                StatusEffectType.Degeneration))
        {
            return float.MinValue;
        }

        // <변경부분> Knight가 현재 위치에서 이미 공격받을 수 있고,
        // 이번 턴 최고 일반 행동은 다른 기물을 사용하는 행동이라면
        // Knight를 움직이지 않는 동안의 사망 위험에 대비해
        // 높은 확률로 퇴화를 먼저 사용한다.
        bool isCurrentlyThreatened =
            battleManager
                .IsPieceCurrentlyThreatened(
                    degenerationKnight
                );

        bool bestActionUsesAnotherPiece =
            bestNormalAction != null &&
            bestNormalAction.ActingPiece !=
                degenerationKnight;

        if (isCurrentlyThreatened &&
            bestActionUsesAnotherPiece &&
            Random.value <=
                DegenerationCurrentThreatUseChance)
        {
            return Mathf.Max(
                DegenerationOpportunityBaseScore,
                bestNormalAction.Score +
                DegenerationCurrentThreatPriorityBonus
            );
        }

        float bestRiskyKnightStrategicScore =
            float.MinValue;

        bool foundValuableRiskyAction =
            false;

        // <변경부분> 이 Knight가 실행할 수 있는
        // 모든 이동과 공격 행동을 검사한다.
        for (int i = 0;
             i < allActions.Count;
             i++)
        {
            BattleAIAction candidateAction =
                allActions[i];

            if (candidateAction == null ||
                candidateAction.ActingPiece !=
                    degenerationKnight)
            {
                continue;
            }

            if (candidateAction.ActionType !=
                    BattleAIActionType.Move &&
                candidateAction.ActionType !=
                    BattleAIActionType.Attack)
            {
                continue;
            }

            bool isThreatened =
                battleManager
                    .IsActingPieceThreatenedAfterAIAction(
                        candidateAction
                    );

            // 안전한 행동은 퇴화 사용 이유가 아니다.
            if (isThreatened == false)
            {
                continue;
            }

            // 기존 평가에서는 Knight 사망 위험으로
            // Knight 가치 300점이 감점되어 있다.
            //
            // 퇴화 사용 가능성을 평가할 때는 이 감점을 복원하여
            // 행동 자체의 전략적 가치를 확인한다.
            float strategicScoreWithDegeneration =
                candidateAction.Score +
                GetPieceValue(
                    PieceType.Knight
                ) +
                DegenerationThreatenedActionPenalty;

            if (foundValuableRiskyAction == false ||
                strategicScoreWithDegeneration >
                bestRiskyKnightStrategicScore)
            {
                foundValuableRiskyAction =
                    true;

                bestRiskyKnightStrategicScore =
                    strategicScoreWithDegeneration;
            }
        }

        // 위험한 Knight 행동이 없다면
        // 퇴화를 사용할 필요가 없다.
        if (foundValuableRiskyAction == false)
        {
            return float.MinValue;
        }

        float bestNormalScore =
            bestNormalAction == null
                ? float.MinValue
                : bestNormalAction.Score;

        // <변경부분> 퇴화를 사용해도 위험 행동의 전략적 점수가
        // 현재 안전한 최고 행동보다 낮다면 스킬을 낭비하지 않는다.
        if (bestRiskyKnightStrategicScore <
            bestNormalScore)
        {
            return float.MinValue;
        }

        // 퇴화 기회 점수가 현재 일반 행동보다 높도록 만들어
        // 먼저 퇴화를 사용한 뒤 보드를 다시 평가하게 한다.
        return Mathf.Max(
            DegenerationOpportunityBaseScore,
            bestRiskyKnightStrategicScore +
            DegenerationOpportunityPriorityBonus
        );
    }

    // <변경부분> 젤루 합성을 지금 사용하는 것이 유리한지 평가한다.
    //
    // AI는 특정 재료를 직접 선택하지 않는다.
    //
    // 계산 요소:
    // 1. 합성 기본 점수
    // 2. 주변 전체 재료 중 2개가 무작위 선택될 때의 희생 기대값
    // 3. Knight/Bishop/Rook 무작위 승급 후 공격 기대값
    // 4. 무작위 승급 후 즉시 상대에게 공격받는 위험 기대값
    //
    // 저가치 재료를 사용하고 승급 후 안전하다면
    // 당장 공격할 대상이 없어도 합성을 적극적으로 선택한다.
    private float EvaluateJelluSynthesisAction(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null ||
            battleMoveValidator == null)
        {
            return float.MinValue;
        }

        Piece actingPiece =
            action.ActingPiece;

        List<Piece> materialCandidates =
            battleMoveValidator
                .GetJelluSynthesisMaterialCandidates(
                    actingPiece
                );

        // 실제 합성에 필요한 재료가 부족하면
        // 실행할 수 없는 후보로 처리한다.
        if (materialCandidates == null ||
            materialCandidates.Count < 2)
        {
            return float.MinValue;
        }

        float score =
     JelluSynthesisBaseScore;

        // <변경부분> 인접 재료 후보 전체에
        // Knight, Bishop, Rook이 몇 개 있는지 확인한다.
        //
        // 실제 합성은 재료 두 개를 무작위로 선택하므로,
        // 상급 기물이 2개 이상 있으면 중요한 전력 손실 위험이
        // 단순 조합 평균보다 체감상 훨씬 크다고 판단한다.
        int advancedMaterialCount =
            CountAdvancedSynthesisMaterials(
                materialCandidates
            );

        if (advancedMaterialCount >= 2)
        {
            score +=
                MultipleAdvancedMaterialPenalty;
        }

        // 실제 합성에서는 전체 후보 중
        // 서로 다른 재료 두 개가 동일 확률로 선택된다.
        //
        // 따라서 가능한 모든 재료 조합의
        // 평균 기대값을 계산한다.
        float totalPairExpectedValue =
            0f;

        int pairCount =
            0;

        for (int i = 0;
             i <
             materialCandidates.Count - 1;
             i++)
        {
            Piece materialA =
                materialCandidates[i];

            if (materialA == null)
            {
                continue;
            }

            for (int j = i + 1;
                 j <
                 materialCandidates.Count;
                 j++)
            {
                Piece materialB =
                    materialCandidates[j];

                if (materialB == null ||
                    materialA == materialB)
                {
                    continue;
                }

                // 무작위로 이 두 재료가 선택됐을 때의
                // 희생 가치 보너스 또는 감점을 계산한다.
                float materialPairValue =
                    EvaluateSynthesisMaterialValue(
                        materialA
                    ) +
                    EvaluateSynthesisMaterialValue(
                        materialB
                    );

                // 해당 재료가 제거된 뒤
                // Knight/Bishop/Rook 중 하나로 승급했을 때
                // 즉시 잡을 수 있는 대상의 기대값을 계산한다.
                float promotionAttackValue =
                    EvaluateSynthesisPromotionAttackExpectedValue(
                        actingPiece,
                        materialA,
                        materialB
                    );

                // <변경부분> 해당 재료가 제거된 뒤
                // Knight/Bishop/Rook 중 하나로 승급했을 때
                // 즉시 상대에게 잡힐 위험의 기대값을 계산한다.
                float promotionSafetyValue =
                    EvaluateSynthesisPromotionSafetyExpectedValue(
                        actingPiece,
                        materialA,
                        materialB
                    );

                totalPairExpectedValue +=
                    materialPairValue +
                    promotionAttackValue +
                    promotionSafetyValue;

                pairCount++;
            }
        }

        if (pairCount <= 0)
        {
            return float.MinValue;
        }

        // 가능한 모든 무작위 재료 조합의 평균을
        // 현재 합성 행동의 최종 기대값으로 사용한다.
        float randomMaterialExpectedValue =
            totalPairExpectedValue /
            pairCount;

        score +=
            randomMaterialExpectedValue;

        return score;
    }

    // <변경부분> 지정 타입으로 승급했을 때
    // 현재 위치에서 즉시 상대에게 공격받는지 검사하고
    // 안전 보너스 또는 위험 감점을 반환한다.
    private float EvaluateSimulatedPromotionSafetyValue(
        Piece actingPiece,
        PieceType simulatedType,
        Piece materialA,
        Piece materialB)
    {
        if (actingPiece == null ||
            battleMoveValidator == null)
        {
            return 0f;
        }

        bool isThreatened =
            battleMoveValidator
                .IsSimulatedSynthesisPieceThreatened(
                    actingPiece,
                    simulatedType,
                    materialA,
                    materialB
                );

        if (isThreatened)
        {
            return
                ThreatenedSynthesisPromotionPenalty;
        }

        return
            SafeSynthesisPromotionScore;
    }

    // <변경부분> 젤루 합성 후 무작위 승급되는
    // Knight, Bishop, Rook 각각의 생존 안전도를 계산한다.
    //
    // 안전한 승급 결과에는 보너스를 부여하고,
    // 즉시 공격받는 승급 결과에는 큰 감점을 적용한다.
    //
    // 최종 안전 기대값:
    // Knight 안전 점수
    // + Bishop 안전 점수
    // + Rook 안전 점수
    // 를 3으로 나눈 평균
    private float EvaluateSynthesisPromotionSafetyExpectedValue(
        Piece actingPiece,
        Piece materialA,
        Piece materialB)
    {
        if (actingPiece == null ||
            battleMoveValidator == null)
        {
            return 0f;
        }

        float knightSafetyValue =
            EvaluateSimulatedPromotionSafetyValue(
                actingPiece,
                PieceType.Knight,
                materialA,
                materialB
            );

        float bishopSafetyValue =
            EvaluateSimulatedPromotionSafetyValue(
                actingPiece,
                PieceType.Bishop,
                materialA,
                materialB
            );

        float rookSafetyValue =
            EvaluateSimulatedPromotionSafetyValue(
                actingPiece,
                PieceType.Rook,
                materialA,
                materialB
            );

        return
            (
                knightSafetyValue +
                bishopSafetyValue +
                rookSafetyValue
            ) / 3f;
    }

    // <변경부분> 젤루 합성 후 무작위 승급될 수 있는
    // Knight, Bishop, Rook의 공격 기대값을 계산한다.
    //
    // 각 타입으로 승급했을 때 현재 위치에서 공격 가능한 대상 중
    // 가장 가치가 높은 기물 하나의 가치를 가져온다.
    //
    // 최종 기대값:
    // (Knight 최고 공격 가치
    //  + Bishop 최고 공격 가치
    //  + Rook 최고 공격 가치) / 3
    private float EvaluateSynthesisPromotionAttackExpectedValue(
        Piece actingPiece,
        Piece materialA,
        Piece materialB)
    {
        if (actingPiece == null ||
            battleMoveValidator == null)
        {
            return 0f;
        }

        float knightAttackValue =
            EvaluateBestAttackValueForSimulatedType(
                actingPiece,
                PieceType.Knight,
                materialA,
                materialB
            );

        float bishopAttackValue =
            EvaluateBestAttackValueForSimulatedType(
                actingPiece,
                PieceType.Bishop,
                materialA,
                materialB
            );

        float rookAttackValue =
            EvaluateBestAttackValueForSimulatedType(
                actingPiece,
                PieceType.Rook,
                materialA,
                materialB
            );

        return
            (
                knightAttackValue +
                bishopAttackValue +
                rookAttackValue
            ) / 3f;
    }

    // <변경부분> 지정한 타입으로 가상 승급했을 때
    // 공격 가능한 대상 중 가장 높은 기물 가치를 반환한다.
    //
    // 실제 PieceType과 실제 보드는 변경하지 않는다.
    // 합성 재료 A와 B는 제거된 가상 보드 상태로 계산한다.
    private float EvaluateBestAttackValueForSimulatedType(
        Piece actingPiece,
        PieceType simulatedType,
        Piece materialA,
        Piece materialB)
    {
        if (actingPiece == null ||
            battleMoveValidator == null)
        {
            return 0f;
        }

        List<Vector2Int> attackPositions =
            battleMoveValidator
                .GetAttackPositionsForSimulatedType(
                    actingPiece,
                    simulatedType,
                    materialA,
                    materialB
                );

        if (attackPositions == null ||
            attackPositions.Count == 0)
        {
            return 0f;
        }

        float highestAttackValue = 0f;

        for (int i = 0;
             i < attackPositions.Count;
             i++)
        {
            Vector2Int attackPosition =
                attackPositions[i];

            // 합성 재료 좌표는 BattleMoveValidator에서
            // 이미 빈칸으로 처리됐으므로 반환 목록에는
            // 실제 적대 기물 좌표만 들어온다.
            Piece targetPiece =
                GetTargetPieceAtSimulatedAttackPosition(
                    attackPosition,
                    materialA,
                    materialB
                );

            if (targetPiece == null)
            {
                continue;
            }

            float targetValue =
                GetPieceValue(
                    targetPiece.PieceType
                );

            if (targetValue >
                highestAttackValue)
            {
                highestAttackValue =
                    targetValue;
            }
        }

        return highestAttackValue;
    }

    // <변경부분> 가상 합성 이후 공격 좌표에 존재하는
    // 실제 적대 기물을 BattleMoveValidator를 통해 가져온다.
    private Piece GetTargetPieceAtSimulatedAttackPosition(
        Vector2Int attackPosition,
        Piece materialA,
        Piece materialB)
    {
        if (battleMoveValidator == null)
        {
            return null;
        }

        return battleMoveValidator
            .GetPieceAtAfterSimulatedSynthesis(
                attackPosition,
                materialA,
                materialB
            );
    }

    // <변경부분> 현재 합성 재료 후보 전체에서
    // 희생 위험이 높은 상급 기물 개수를 계산한다.
    //
    // Knight, Bishop, Rook만 위험 재료로 집계한다.
    // Pawn과 Special은 저가치 재료로 취급하며,
    // Queen은 현재 일반적인 합성 재료 구성에는 없지만
    // 개별 재료 평가에서 별도로 큰 감점을 유지한다.
    private int CountAdvancedSynthesisMaterials(
        List<Piece> materialCandidates)
    {
        if (materialCandidates == null ||
            materialCandidates.Count == 0)
        {
            return 0;
        }

        int advancedMaterialCount =
            0;

        for (int i = 0;
             i < materialCandidates.Count;
             i++)
        {
            Piece material =
                materialCandidates[i];

            if (material == null)
            {
                continue;
            }

            switch (material.PieceType)
            {
                case PieceType.Knight:
                case PieceType.Bishop:
                case PieceType.Rook:
                    advancedMaterialCount++;
                    break;
            }
        }

        return advancedMaterialCount;
    }

    // <변경부분> 젤루 합성에 사용되는 재료 한 개의
    // 전략적 가치를 점수로 변환한다.
    //
    // 낮은 가치의 Pawn이나 Neutral Special을 사용하면 보너스,
    // 이미 전투력이 높은 Knight, Bishop, Rook을 사용하면 감점한다.
    private float EvaluateSynthesisMaterialValue(
        Piece material)
    {
        if (material == null)
        {
            return float.MinValue;
        }

        // Neutral Jellu Special은 아군 전투력을 소모하지 않으므로
        // 가장 효율적인 합성 재료로 평가한다.
        if (material.Team == PieceTeam.Neutral &&
            material.PieceType == PieceType.Special)
        {
            return NeutralSpecialMaterialScore;
        }

        switch (material.PieceType)
        {
            case PieceType.Pawn:
                return PawnMaterialScore;

            case PieceType.Special:
                return AlliedSpecialMaterialScore;

            case PieceType.Knight:
                return KnightMaterialPenalty;

            case PieceType.Bishop:
                return BishopMaterialPenalty;

            case PieceType.Rook:
                return RookMaterialPenalty;

            case PieceType.Queen:
                return QueenMaterialPenalty;

            // King은 후보 생성 단계에서도 제외하지만
            // 평가기에서도 다시 방어한다.
            case PieceType.King:
                return float.MinValue;
        }

        return 0f;
    }

    // <변경부분> 공격 대상 기물의 기본 가치에 따라
    // 해당 공격 행동의 점수를 계산한다.
    //
    // Neutral Jellu Special은 일반 적 기물과 다르게 처리한다.
    //
    // 1. 현재 진영에 JelluSynthesis 사용자가 존재함
    //    → 합성 재료로 보존하기 위해 공격 점수를 감점한다.
    //
    // 2. 합성 사용자가 존재하지 않음
    //    → 공격은 허용하지만 전투 기물보다 낮은 점수를 부여한다.
    private float EvaluateAttackAction(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null ||
            action.TargetPiece == null)
        {
            return float.MinValue;
        }

        Piece targetPiece =
            action.TargetPiece;

        // <변경부분> 비숍의 젤루 벽 또는
        // 퇴화 사망 효과로 생성된 중립 젤루인지 확인한다.
        bool isNeutralJelluSpecial =
            targetPiece.Team ==
                PieceTeam.Neutral &&
            targetPiece.PieceType ==
                PieceType.Special &&
            targetPiece.HasSpeciesTag(
                PieceSpeciesTag.Jellu
            );

        if (isNeutralJelluSpecial)
        {
            PieceTeam actingTeam =
                action.ActingPiece.Team;

            bool hasSynthesisUser =
                battleMoveValidator != null &&
                battleMoveValidator
                    .HasJelluSynthesisUser(
                        actingTeam
                    );

            // <변경부분> 해당 진영에 합성 사용자가 있다면
            // 중립 젤루를 미래 또는 현재 합성 재료로 보존한다.
            if (hasSynthesisUser)
            {
                float score =
                    NeutralJelluAttackPenaltyWithSynthesis;

                // 현재 합성 Pawn 바로 옆에 있는 중립 젤루는
                // 즉시 사용할 수 있는 실제 합성 재료이므로
                // 추가 감점을 적용해 더욱 강하게 보존한다.
                bool isImmediatelyUsableMaterial =
                    battleMoveValidator
                        .IsNeutralJelluImmediatelyUsableForSynthesis(
                            targetPiece,
                            actingTeam
                        );

                if (isImmediatelyUsableMaterial)
                {
                    score +=
                        ImmediatelyUsableNeutralJelluPenalty;
                }

                return score;
            }

            // <변경부분> 합성 기물이 없는 조합에서는
            // 중립 젤루를 공격하는 행동 자체는 허용한다.
            //
            // 다만 중립 Special은 이동하거나 직접 공격하지 않으므로
            // Pawn, Knight, Bishop, Rook 등의 실질적인 위협보다
            // 우선해서 제거하지 않도록 낮은 점수만 부여한다.
            return
                NeutralJelluAttackScoreWithoutSynthesis;
        }

        // 일반 Player 기물 공격은 기존 기물 가치 기준을 유지한다.
        return GetPieceValue(
            targetPiece.PieceType
        );
    }

    // <변경부분> 기물 타입별 기본 전략 가치를 반환한다.
    //
    // 이 값은 다음 평가에 공용으로 사용된다.
    // 1. 공격 대상의 가치
    // 2. 행동 기물이 피격 위험에 노출될 때의 손실 가치
    // 3. 이후 합성 승급 공격 기대값 평가
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

        // 정의되지 않은 기물 타입은
        // 전략적 가치를 부여하지 않는다.
        return 0f;
    }

    // <변경부분> 가장 높은 점수의 유효한 행동들을 추려낸 뒤
    // 동점 후보 중 하나를 랜덤으로 선택한다.
    //
    // float.MinValue는 AI 평가기에서
    // "현재 상황에서는 실행하면 안 되는 행동"을 의미하는 예약 점수로 사용한다.
    // 따라서 실제 최고 점수 후보에 포함시키지 않는다.
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
            BattleAIAction action =
                actions[i];

            if (action == null)
            {
                continue;
            }

            // <변경부분>
            // float.MinValue는 평가 단계에서 실행 금지로 판정된 행동이다.
            //
            // 이 값을 후보에 포함하면 모든 행동이 실행 불가인 상황에서
            // float.MinValue 행동들이 서로 동점으로 처리되어
            // 실제 실행 대상으로 잘못 선택될 수 있으므로 반드시 제외한다.
            if (action.Score == float.MinValue)
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

            // 현재 최고 점수와 같은 유효 행동은 동점 후보로 추가한다.
            if (Mathf.Approximately(
                    action.Score,
                    highestScore))
            {
                bestActions.Add(action);
            }
        }

        // <변경부분>
        // 원본 후보가 존재하더라도 모두 실행 금지 점수였다면
        // 선택할 수 있는 실제 행동이 없는 것으로 처리한다.
        if (bestActions.Count == 0)
        {
            return null;
        }

        // 최고 점수가 같은 유효 행동 중 하나를 랜덤으로 선택한다.
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