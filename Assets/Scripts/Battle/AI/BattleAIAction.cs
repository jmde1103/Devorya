using UnityEngine;

// <변경부분> AI가 판단하거나 실행할 전투 행동 종류
public enum BattleAIActionType
{
    // 빈 타일로 이동
    Move,

    // 적대 기물이 있는 타일을 공격
    Attack
}

// <변경부분> AI 행동 후보 하나의 정보를 저장하는 일반 C# 데이터 클래스
// MonoBehaviour가 아니므로 GameObject에 부착하지 않는다.
public class BattleAIAction
{
    // 이동 또는 공격 행동 종류
    public BattleAIActionType ActionType { get; }

    // 행동을 실행할 기물
    public Piece ActingPiece { get; }

    // 행동 시작 좌표
    public Vector2Int SourcePosition { get; }

    // 행동 목표 좌표
    public Vector2Int TargetPosition { get; }

    // 공격 대상 기물
    // 이동 행동에서는 null이다.
    public Piece TargetPiece { get; }

    // 향후 AI 평가 시스템이 계산할 행동 점수
    public float Score { get; set; }

    // <변경부분> 빈 타일 이동 행동을 생성하는 함수
    public static BattleAIAction CreateMove(
        Piece actingPiece,
        Vector2Int sourcePosition,
        Vector2Int targetPosition)
    {
        return new BattleAIAction(
            BattleAIActionType.Move,
            actingPiece,
            sourcePosition,
            targetPosition,
            null
        );
    }

    // <변경부분> 적대 기물 공격 행동을 생성하는 함수
    public static BattleAIAction CreateAttack(
        Piece actingPiece,
        Vector2Int sourcePosition,
        Vector2Int targetPosition,
        Piece targetPiece)
    {
        return new BattleAIAction(
            BattleAIActionType.Attack,
            actingPiece,
            sourcePosition,
            targetPosition,
            targetPiece
        );
    }

    // <변경부분> 잘못된 형태로 행동 데이터가 생성되지 않도록
    // 외부에서는 CreateMove 또는 CreateAttack만 사용한다.
    private BattleAIAction(
        BattleAIActionType actionType,
        Piece actingPiece,
        Vector2Int sourcePosition,
        Vector2Int targetPosition,
        Piece targetPiece)
    {
        ActionType = actionType;
        ActingPiece = actingPiece;
        SourcePosition = sourcePosition;
        TargetPosition = targetPosition;
        TargetPiece = targetPiece;
        Score = 0f;
    }
}
