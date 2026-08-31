using UnityEngine;

// <변경부분> AI가 판단하거나 실행할 전투 행동 종류
public enum BattleAIActionType
{
    // 빈 타일로 이동
    Move,

    // 적대 기물이 있는 타일을 공격
    Attack,

    // 기물의 고유스킬 사용
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

    // AI 평가 시스템이 계산한 행동 점수
    public float Score { get; set; }

    // <변경부분> 빈 타일 이동 행동을 생성한다.
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
            UniqueSkillType.None
        );
    }

    // <변경부분> 적대 기물 공격 행동을 생성한다.
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
            UniqueSkillType.None
        );
    }

    // <변경부분> 지정한 기물이 현재 실제로 보유 중인
    // 고유스킬을 그대로 사용하는 공용 행동 데이터를 생성한다.
    //
    // EventSequence처럼 AI의 후보 생성 과정을 거치지 않고
    // 특정 기물의 고유스킬을 명시적으로 실행해야 할 때 사용한다.
    //
    // 실제 사용 가능 여부는 BattleManager.TryExecuteAIUniqueSkill()
    // 내부의 기존 검증을 그대로 사용한다.
    public static BattleAIAction CreateUniqueSkill(
        Piece actingPiece)
    {
        if (actingPiece == null ||
            actingPiece.UniqueSkill ==
                UniqueSkillType.None)
        {
            return null;
        }

        Vector2Int currentPosition =
            new Vector2Int(
                actingPiece.X,
                actingPiece.Y
            );

        return new BattleAIAction(
            BattleAIActionType.UniqueSkill,
            actingPiece,
            currentPosition,
            currentPosition,
            null,
            actingPiece.UniqueSkill
        );
    }

    // <변경부분> 젤루 합성 고유스킬 AI 행동 후보를 생성한다.
    //
    // AI는 특정 합성 재료를 행동 데이터에 저장하지 않고
    // 현재 위치에서 합성을 사용할지만 판단한다.
    //
    // 실제 합성 재료 두 개는
    // BattleSkillManager가 스킬 발동 순간 무작위로 선택한다.
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
            UniqueSkillType.JelluSynthesis
        );
    }

    // <변경부분> 젤루 퇴화 고유스킬 AI 행동 후보를 생성한다.
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
            UniqueSkillType.JelluDegeneration
        );
    }

    // <변경부분> 젤루 킹의 증식 고유스킬 AI 행동 후보를 생성한다.
    //
    // 증식은 별도의 대상을 직접 선택하지 않고
    // 젤루 킹 주변 빈칸 중 하나에 젤루 Pawn을 생성한다.
    //
    // 실제 생성 위치는 BattleSkillManager가
    // 스킬 발동 순간 무작위로 선택한다.
    public static BattleAIAction CreateJelluMultiply(
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
            UniqueSkillType.JelluMultiply
        );
    }

    // <변경부분> 젤루 룩의 뿔 박치기 고유스킬 AI 행동 후보를 생성한다.
    //
    // 뿔 박치기는 별도의 공격 대상을 직접 저장하지 않고
    // Rook 자신에게 Breakthrough 상태를 부여한다.
    //
    // 실제 공격 대상은 스킬 사용 후
    // AI가 변경된 보드를 다시 평가하여 선택한다.
    public static BattleAIAction CreateHornHeadbutt(
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
            UniqueSkillType.HornHeadbutt
        );
    }

    // <변경부분>
    // 잘못된 형태로 행동 데이터가 생성되지 않도록
    // 외부에서는 위의 각 Create 함수를 통해서만 행동을 생성한다.
    //
    // 과거 젤루 합성 AI가 재료 A/B를 직접 결정하던 구조에서 사용하던
    // SkillTargetPieceA/B는 현재 사용되지 않으므로 생성자에서도 제거했다.
    private BattleAIAction(
        BattleAIActionType actionType,
        Piece actingPiece,
        Vector2Int sourcePosition,
        Vector2Int targetPosition,
        Piece targetPiece,
        UniqueSkillType uniqueSkillType)
    {
        ActionType =
            actionType;

        ActingPiece =
            actingPiece;

        SourcePosition =
            sourcePosition;

        TargetPosition =
            targetPosition;

        TargetPiece =
            targetPiece;

        UniqueSkillType =
            uniqueSkillType;

        Score =
            0f;
    }
}