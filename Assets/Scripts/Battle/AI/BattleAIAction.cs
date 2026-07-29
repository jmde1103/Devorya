using UnityEngine;

// <변경부분> AI가 판단하거나 실행할 전투 행동 종류
public enum BattleAIActionType
{
    // 빈 타일로 이동
    Move,

    // 적대 기물이 있는 타일을 공격
    Attack,

    // <변경부분> 기물의 고유스킬 사용
    UniqueSkill
}

// <변경부분> AI 행동 후보 하나의 정보를 저장하는 일반 C# 데이터 클래스
// MonoBehaviour가 아니므로 GameObject에 부착하지 않는다.
public class BattleAIAction
{
    // 이동, 공격 또는 고유스킬 행동 종류
    public BattleAIActionType ActionType { get; }

    // 행동을 실행할 기물
    public Piece ActingPiece { get; }

    // 행동 시작 좌표
    public Vector2Int SourcePosition { get; }

    // 행동 목표 좌표
    public Vector2Int TargetPosition { get; }

    // 공격 대상 기물
    // 이동 및 고유스킬 행동에서는 null이다.
    public Piece TargetPiece { get; }

    // <변경부분> 고유스킬 행동에서 사용할 스킬 종류
    // 이동 및 공격 행동에서는 None이다.
    public UniqueSkillType UniqueSkillType { get; }

    // <변경부분> 고유스킬의 첫 번째 선택 대상
    public Piece SkillTargetPieceA { get; }

    // <변경부분> 고유스킬의 두 번째 선택 대상
    public Piece SkillTargetPieceB { get; }

    // AI 평가 시스템이 계산한 행동 점수
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
            null,
            UniqueSkillType.None,
            null,
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
            targetPiece,
            UniqueSkillType.None,
            null,
            null
        );
    }

    // <변경부분> 젤루 합성 고유스킬 행동 후보를 생성한다.
    //
    // AI는 특정 합성 재료를 선택하지 않고
    // 현재 위치에서 합성을 사용할지만 판단한다.
    //
    // 실제 합성 재료 두 개는 플레이어 사용과 동일하게
    // BattleSkillManager가 발동 순간 무작위로 선택한다.
    public static BattleAIAction CreateJelluSynthesis(
        Piece actingPiece)
    {
        if (actingPiece == null)
        {
            return null;
        }

        return new BattleAIAction(
            BattleAIActionType.UniqueSkill,
            actingPiece,
            new Vector2Int(
                actingPiece.X,
                actingPiece.Y
            ),
            new Vector2Int(
                actingPiece.X,
                actingPiece.Y
            ),
            null,
            UniqueSkillType.JelluSynthesis,

            // 특정 재료를 AI 행동 데이터에 저장하지 않는다.
            null,
            null
        );
    }

    // <변경부분> 젤루 퇴화 고유스킬의 AI 행동 후보를 생성한다.
    //
    // 퇴화는 별도의 대상 없이
    // Knight 자신에게 Degeneration 상태를 부여하므로
    // 시작 좌표와 목표 좌표를 현재 위치로 저장한다.
    public static BattleAIAction CreateJelluDegeneration(
        Piece actingPiece)
    {
        if (actingPiece == null)
        {
            return null;
        }

        return new BattleAIAction(
            BattleAIActionType.UniqueSkill,
            actingPiece,
            new Vector2Int(
                actingPiece.X,
                actingPiece.Y
            ),
            new Vector2Int(
                actingPiece.X,
                actingPiece.Y
            ),
            null,
            UniqueSkillType.JelluDegeneration,
            null,
            null
        );
    }

    // <변경부분> 잘못된 형태로 행동 데이터가 생성되지 않도록
    // 외부에서는 각 Create 함수를 사용한다.
    private BattleAIAction(
        BattleAIActionType actionType,
        Piece actingPiece,
        Vector2Int sourcePosition,
        Vector2Int targetPosition,
        Piece targetPiece,
        UniqueSkillType uniqueSkillType,
        Piece skillTargetPieceA,
        Piece skillTargetPieceB)
    {
        ActionType = actionType;
        ActingPiece = actingPiece;
        SourcePosition = sourcePosition;
        TargetPosition = targetPosition;
        TargetPiece = targetPiece;

        UniqueSkillType = uniqueSkillType;
        SkillTargetPieceA = skillTargetPieceA;
        SkillTargetPieceB = skillTargetPieceB;

        Score = 0f;
    }
}