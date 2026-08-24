using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// <변경부분> System.Random과 UnityEngine.Random 이름 충돌 방지
using Random = UnityEngine.Random;

// <변경부분> 전투 중 일반스킬과 스킬 발동 판정을 관리하는 매니저
public class BattleSkillManager : MonoBehaviour
{
    // <변경부분> 보드 범위 확인에 사용하는 보드 매니저
    private BoardManager boardManager;

    // <변경부분> 기물 위치 확인과 복제 생성을 담당하는 기물 매니저
    private PieceManager pieceManager;

    // <변경부분> 이동 판정뿐 아니라
    // 젤루 합성 및 주변 빈칸의 공용 판정 기준을 사용하는 Validator 참조
    private BattleMoveValidator battleMoveValidator;

    // <변경부분> 복제/증식 스킬에서 사용할 주변 빈칸 좌표 재사용 목록.
    //
    // 스킬을 사용할 때마다 new List<Vector2Int>()를 만들지 않고
    // 동일한 목록을 Clear 후 다시 사용하여 불필요한 GC 할당을 줄인다.
    private readonly List<Vector2Int> adjacentEmptyPositions =
        new List<Vector2Int>();

    // <변경부분> 일반스킬 데이터베이스
    [SerializeField] private GeneralSkillDatabase generalSkillDatabase;

    // <변경부분> 상태이상 데이터베이스
    // 퇴화 같은 상태이상 기본 지속 턴/중첩 정보를 가져올 때 사용
    [SerializeField] private StatusEffectDatabase statusEffectDatabase;

    // <변경부분> BattleManager에서 전투 시작 시
    // 보드/기물 매니저와 공용 이동·합성 판정기를 함께 전달받는다.
    public void Initialize(
        BoardManager board,
        PieceManager pieceManagerRef,
        BattleMoveValidator moveValidator)
    {
        // 보드 범위 및 타일 확인용
        boardManager =
            board;

        // 기물 생성/제거/위치 확인용
        pieceManager =
            pieceManagerRef;

        // <변경부분>
        // 젤루 합성 재료 조건을 AI와 실제 스킬이 동일하게 사용하기 위한
        // 공용 BattleMoveValidator 참조
        battleMoveValidator =
            moveValidator;
    }

    // <변경부분> ChanceAttack 발동 여부를
    // 행동 시작 전 스킬 보유 상태와 GeneralSkillData의 고정 확률로 판정한다.
    public bool TryActivateChanceAttack(
        Piece piece,
        OwnedGeneralSkillData chanceAttackDataBeforeAction,
        int chanceAttackContinuousCount)
    {
        // 판정할 기물이 없으면 발동할 수 없다.
        if (piece == null)
        {
            return false;
        }

        // 행동 시작 전에 ChanceAttack을 보유하지 않았다면
        // 이번 처치로 새로 흡수했더라도 즉시 발동하지 않는다.
        if (chanceAttackDataBeforeAction == null ||
            chanceAttackDataBeforeAction.skillType !=
            GeneralSkillType.ChanceAttack)
        {
            Debug.Log(
                "ChanceAttack 판정 실패: " +
                "이번 행동 시작 시점에는 ChanceAttack이 없었습니다."
            );

            return false;
        }

        if (generalSkillDatabase == null)
        {
            Debug.LogWarning(
                "GeneralSkillDatabase가 연결되지 않아 " +
                "ChanceAttack을 판정할 수 없습니다."
            );

            return false;
        }

        GeneralSkillData chanceAttackData =
            generalSkillDatabase.GetData(
                GeneralSkillType.ChanceAttack
            );

        if (chanceAttackData == null)
        {
            Debug.LogWarning(
                "GeneralSkillDatabase에서 " +
                "ChanceAttack 데이터를 찾을 수 없습니다."
            );

            return false;
        }

        // 일반스킬 레벨 없이 데이터에 설정된 고정 확률을 사용한다.
        int baseChancePercent =
            chanceAttackData.GetChanceAttackPercent();

        // 연속 발동 횟수에 따른 감소 배율을 적용한다.
        float penaltyMultiplier =
            chanceAttackData
                .GetChanceAttackContinuousPenaltyMultiplier(
                    chanceAttackContinuousCount
                );

        float finalChancePercent =
            baseChancePercent *
            penaltyMultiplier;

        float randomValue =
            Random.Range(
                0f,
                100f
            );

        bool isActivated =
            randomValue <
            finalChancePercent;

        Debug.Log(
    $"ChanceAttack 판정: " +
    $"기본확률 {baseChancePercent}% / " +
    $"연속횟수 {chanceAttackContinuousCount} / " +
    $"감소배율 {penaltyMultiplier:F3} / " +
    $"최종확률 {finalChancePercent:F1}% / " +
    $"랜덤 {randomValue:F1} / " +
    $"결과 {isActivated}"
);

        // <변경부분> 이 함수에서는 확률 판정 결과만 반환한다.
        // 아이콘 선행 연출과 실제 추가 행동 적용은
        // BattleManager 전투 코루틴에서 순서대로 처리한다.
        return isActivated;
    }

    // <변경부분> 실제 이동을 완료한 기물의 Defense 발동을 판정하고,
    // 성공하면 아이콘 확대 연출을 먼저 보여준 뒤
    // 실제 Defence 상태효과를 부여한다.
    //
    // Defense 보유 여부는 이동 완료 후 현재 상태가 아니라
    // 행동 시작 전에 복사한 데이터를 기준으로 판정한다.
    public IEnumerator TryGrantDefenceAfterMoveRoutine(
        Piece movedPiece,
        OwnedGeneralSkillData defenseDataBeforeAction,
        Action<bool> onComplete)
    {
        if (movedPiece == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분> 행동 시작 시점에 Defense를 보유하지 않았다면
        // 이번 행동에서는 Defense를 판정하지 않는다.
        //
        // 이번 이동이나 흡수 과정에서 새로 획득한 Defense는
        // 다음 행동부터 발동할 수 있다.
        if (defenseDataBeforeAction == null ||
            defenseDataBeforeAction.skillType !=
                GeneralSkillType.Defense)
        {
            Debug.Log(
                "Defense 판정 생략: " +
                "이번 행동 시작 시점에는 Defense가 없었습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        if (generalSkillDatabase == null)
        {
            Debug.LogWarning(
                "GeneralSkillDatabase가 연결되지 않아 " +
                "Defense 일반스킬을 판정할 수 없습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        GeneralSkillData defenseData =
            generalSkillDatabase.GetData(
                GeneralSkillType.Defense
            );

        if (defenseData == null)
        {
            Debug.LogWarning(
                "GeneralSkillDatabase에서 " +
                "Defense 데이터를 찾을 수 없습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        int defenseGrantChancePercent =
            defenseData.GetDefenseGrantChancePercent();

        if (defenseGrantChancePercent <= 0)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        float randomValue =
            Random.Range(
                0f,
                100f
            );

        bool isActivated =
            randomValue <
            defenseGrantChancePercent;

        Debug.Log(
            $"Defense 이동 완료 판정: " +
            $"확률 {defenseGrantChancePercent}% / " +
            $"랜덤 {randomValue:F1} / " +
            $"결과 {isActivated}"
        );

        if (isActivated == false)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        if (statusEffectDatabase == null)
        {
            Debug.LogWarning(
                "StatusEffectDatabase가 연결되지 않아 " +
                "Defence 상태효과를 부여할 수 없습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        StatusEffectData defenceStatusEffectData =
            statusEffectDatabase.GetData(
                StatusEffectType.Defence
            );

        if (defenceStatusEffectData == null)
        {
            Debug.LogWarning(
                "StatusEffectDatabase에서 " +
                "Defence 상태효과 데이터를 찾을 수 없습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분> 실제 상태효과를 부여하기 전에
        // Defense 아이콘의 확대 등장 연출을 먼저 재생한다.
        yield return
            movedPiece
                .PlaySkillActivationIconBeforeEffectRoutine(
                    defenseData.iconSprite
                );

        // 아이콘이 먼저 뜬 뒤 실제 Defence 상태효과 적용
        movedPiece.AddStatusEffect(
            defenceStatusEffectData
        );

        Debug.Log(
            $"Defense 일반스킬 발동: " +
            $"{movedPiece.Team} {movedPiece.PieceType}에게 " +
            $"Defence 상태효과를 부여했습니다."
        );

        onComplete?.Invoke(true);
    }

    // <변경부분> Insight 발동 여부를
    // 행동 시작 전 보유 상태와 GeneralSkillData의 고정 확률로 판정한다.
    public bool TryActivateInsight(
        Piece attackerPiece,
        OwnedGeneralSkillData insightDataBeforeAction,
        GeneralSkillType targetCanceledSkillType)
    {
        if (attackerPiece == null)
        {
            return false;
        }

        // 행동 시작 시점에 Insight를 보유하지 않았다면
        // 이번 공격에서는 발동하지 않는다.
        if (insightDataBeforeAction == null ||
            insightDataBeforeAction.skillType !=
            GeneralSkillType.Insight)
        {
            return false;
        }

        // 현재 Insight는 Defence 상태효과로 발생한
        // 방어 판정을 무효화하는 데 사용한다.
        if (targetCanceledSkillType !=
            GeneralSkillType.Defense)
        {
            return false;
        }

        if (generalSkillDatabase == null)
        {
            Debug.LogWarning(
                "GeneralSkillDatabase가 연결되지 않아 " +
                "Insight를 판정할 수 없습니다."
            );

            return false;
        }

        GeneralSkillData insightData =
            generalSkillDatabase.GetData(
                GeneralSkillType.Insight
            );

        if (insightData == null)
        {
            Debug.LogWarning(
                "GeneralSkillDatabase에서 " +
                "Insight 데이터를 찾을 수 없습니다."
            );

            return false;
        }

        // 일반스킬 레벨 없이 고정 확률을 사용한다.
        int insightChancePercent =
            insightData.GetInsightPercent();

        if (insightChancePercent <= 0)
        {
            return false;
        }

        float randomValue =
            Random.Range(
                0f,
                100f
            );

        bool isActivated =
            randomValue <
            insightChancePercent;

        Debug.Log(
     $"Insight 판정: " +
     $"대상 {targetCanceledSkillType} / " +
     $"확률 {insightChancePercent}% / " +
     $"랜덤 {randomValue:F1} / " +
     $"결과 {isActivated}"
 );

        // <변경부분> 이 함수에서는 Insight 확률 판정 결과만 반환한다.
        // 아이콘 연출 후 실제 방어 무효화 흐름은
        // BattleManager 전투 코루틴에서 처리한다.
        return isActivated;
    }

    // <변경부분> 일반스킬 데이터베이스에서 아이콘을 가져와
    // 실제 효과가 적용되기 전에 확대 등장 연출을 재생한다.
    public IEnumerator PlayGeneralSkillActivationBeforeEffectRoutine(
        Piece piece,
        GeneralSkillType skillType)
    {
        if (piece == null ||
            skillType == GeneralSkillType.None ||
            generalSkillDatabase == null)
        {
            yield break;
        }

        GeneralSkillData skillData =
            generalSkillDatabase.GetData(
                skillType
            );

        if (skillData == null)
        {
            yield break;
        }

        yield return
            piece
                .PlaySkillActivationIconBeforeEffectRoutine(
                    skillData.iconSprite
                );
    }



    // <변경부분> 고유스킬 아이콘 표시 전에
    // 실제 스킬 사용에 필요한 필수 조건을 미리 검사한다.
    //
    // 이 함수에서는 기물 생성, 상태이상 부여,
    // 이동 타입 변경 등의 실제 효과는 실행하지 않는다.
    //
    // 복제/증식의 주변 빈칸 판정과
    // 합성의 재료 판정은 BattleMoveValidator를 공용 기준으로 사용한다.
    private bool CanUseUniqueSkillEffect(
        Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        switch (piece.UniqueSkill)
        {
            case UniqueSkillType.JelluClone:
                {
                    if (pieceManager == null ||
                        battleMoveValidator == null)
                    {
                        return false;
                    }

                    // 복제는 새로운 기물 한 기를 생성하므로
                    // 해당 진영이 최대 기물 수에 도달했다면 사용할 수 없다.
                    if (pieceManager.CanCreatePieceForTeam(
                            piece.Team) == false)
                    {
                        return false;
                    }

                    // <변경부분>
                    // 기존 BattleSkillManager 내부의 중복 8칸 탐색 대신
                    // BattleMoveValidator의 공용 주변 빈칸 판정을 사용한다.
                    return battleMoveValidator
                        .HasAdjacentEmptyPosition(
                            piece
                        );
                }

            case UniqueSkillType.JelluMultiply:
                {
                    if (pieceManager == null ||
                        battleMoveValidator == null)
                    {
                        return false;
                    }

                    // 증식은 새로운 Jellu Pawn 한 기를 생성하므로
                    // 해당 진영이 최대 기물 수에 도달했다면 사용할 수 없다.
                    if (pieceManager.CanCreatePieceForTeam(
                            piece.Team) == false)
                    {
                        return false;
                    }

                    // <변경부분>
                    // AI 증식 판정과 실제 스킬 사용 가능 판정이
                    // 동일한 BattleMoveValidator 기준을 사용한다.
                    return battleMoveValidator
                        .HasAdjacentEmptyPosition(
                            piece
                        );
                }

            case UniqueSkillType.KingQueenMove:
                {
                    return
                        piece.PieceType ==
                        PieceType.King;
                }

            case UniqueSkillType.JelluSynthesis:
                {
                    if (battleMoveValidator == null)
                    {
                        return false;
                    }

                    // <변경부분>
                    // AI 및 실제 합성 실행과 동일한
                    // BattleMoveValidator의 합성 재료 규칙을 사용한다.
                    List<Piece> synthesisCandidates =
                        battleMoveValidator
                            .GetJelluSynthesisMaterialCandidates(
                                piece
                            );

                    return
                        synthesisCandidates != null &&
                        synthesisCandidates.Count >= 2;
                }

            case UniqueSkillType.JelluWall:
                {
                    if (boardManager == null ||
                        pieceManager == null ||
                        piece.Team ==
                            PieceTeam.Neutral ||
                        piece.HasSpeciesTag(
                            PieceSpeciesTag.Jellu
                        ) == false)
                    {
                        return false;
                    }

                    // Player는 위쪽으로,
                    // Enemy는 아래쪽으로 진행한다.
                    int directionY =
                        piece.Team ==
                        PieceTeam.Player
                            ? 1
                            : -1;

                    int targetWallX =
                        piece.X;

                    int targetWallY =
                        piece.Y +
                        directionY;

                    return
                        IsInsideBoard(
                            targetWallX,
                            targetWallY
                        ) &&
                        pieceManager.IsEmpty(
                            targetWallX,
                            targetWallY
                        );
                }

            case UniqueSkillType.JelluDegeneration:
                {
                    return
                        piece.PieceType ==
                            PieceType.Knight &&
                        piece.Team !=
                            PieceTeam.Neutral &&
                        piece.HasSpeciesTag(
                            PieceSpeciesTag.Jellu
                        ) &&
                        statusEffectDatabase != null &&
                        statusEffectDatabase.GetData(
                            StatusEffectType.Degeneration
                        ) != null;
                }

            case UniqueSkillType.HornHeadbutt:
                {
                    return
                        piece.CurrentTile != null &&
                        (
                            piece.CurrentTile.TileType ==
                                TileType.Water ||
                            piece.CurrentTile.TileType ==
                                TileType.Swamp
                        ) &&
                        statusEffectDatabase != null &&
                        statusEffectDatabase.GetData(
                            StatusEffectType.Breakthrough
                        ) != null;
                }
        }

        return false;
    }

    // <변경부분> 고유스킬 종류에 따라 실제 효과를 실행하는 함수
    public bool TryUseUniqueSkill(Piece piece)
    {
        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        switch (piece.UniqueSkill)
        {
            case UniqueSkillType.JelluClone:
                return UseJelluClone(piece);

            case UniqueSkillType.JelluMultiply:
                return UseJelluMultiply(piece);

            case UniqueSkillType.KingQueenMove:
                return UseKingQueenMove(piece);

            case UniqueSkillType.JelluSynthesis:
                return UseJelluSynthesis(piece);

            case UniqueSkillType.JelluWall:
                return UseJelluWall(piece);

            case UniqueSkillType.JelluDegeneration:
                return UseJelluDegeneration(piece);

            // <변경부분> 뿔 박치기: 물/늪 타일 위에서 자신에게 돌파 상태 1턴 부여
            case UniqueSkillType.HornHeadbutt:
                return UseHornHeadbutt(piece);
        }

        // <변경부분> 처리할 수 없는 고유스킬이면 스킬 사용 실패 처리
        return false;
    }

    // <변경부분> 고유스킬을 코루틴으로 실행하는 함수
    // 합성처럼 애니메이션 종료 후 실제 효과가 적용되어야 하는 스킬을 처리하기 위해 사용
    public IEnumerator TryUseUniqueSkillRoutine(
    Piece piece,
    Sprite skillIcon,
    Action<bool> onComplete)
    {
        if (piece == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분> 실패하는 고유스킬에는
        // 아이콘이 먼저 뜨지 않도록 내부 조건부터 검사한다.
        if (CanUseUniqueSkillEffect(
                piece) == false)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분> 실제 고유스킬 효과보다 먼저
        // 아이콘의 확대 등장과 기본 크기 복귀를 재생한다.
        yield return
            piece
                .PlaySkillActivationIconBeforeEffectRoutine(
                    skillIcon
                );

        bool skillUsed = false;

        switch (piece.UniqueSkill)
        {
            // <변경부분> 젤루 합성은 재료 이동 연출 후 승급되어야 하므로 코루틴으로 처리
            case UniqueSkillType.JelluSynthesis:
                yield return UseJelluSynthesisRoutine(piece, result => skillUsed = result);
                onComplete?.Invoke(skillUsed);
                yield break;

            // <변경부분> 나머지 고유스킬은 기존 즉시 실행 함수를 그대로 사용
            default:
                skillUsed = TryUseUniqueSkill(piece);
                onComplete?.Invoke(skillUsed);
                yield break;
        }
    }

    // <변경부분> 복제 스킬:
    // 주변 빈칸 중 하나를 랜덤 선택하여
    // 시전자와 동일한 정보를 가진 기물을 복제한다.
    //
    // 주변 빈칸 판정은 BattleMoveValidator를 단일 기준으로 사용한다.
    private bool UseJelluClone(
        Piece piece)
    {
        // 실제 복제 처리와 공용 빈칸 판정에 필요한
        // 참조가 초기화되지 않았다면 실행할 수 없다.
        if (pieceManager == null ||
            battleMoveValidator == null)
        {
            Debug.LogWarning(
                "BattleSkillManager 초기화가 완료되지 않아 " +
                "JelluClone을 사용할 수 없습니다."
            );

            return false;
        }

        if (piece == null)
        {
            return false;
        }

        // 진영 최대 기물 수에 도달했다면
        // 빈칸이 있더라도 새 기물을 생성할 수 없다.
        if (pieceManager.CanCreatePieceForTeam(
                piece.Team) == false)
        {
            Debug.Log(
                $"복제 실패: " +
                $"{piece.Team} 진영이 최대 기물 수에 도달했습니다."
            );

            return false;
        }

        // <변경부분>
        // 기존처럼 함수 내부에서 매번 새 List를 만들고
        // 주변 8칸을 다시 계산하지 않는다.
        //
        // 클래스가 보유한 재사용 List에
        // BattleMoveValidator가 공용 규칙으로 빈칸을 채운다.
        battleMoveValidator
            .FillAdjacentEmptyPositions(
                piece,
                adjacentEmptyPositions
            );

        if (adjacentEmptyPositions.Count == 0)
        {
            Debug.Log(
                "복제 실패: 인접한 빈칸이 없습니다."
            );

            return false;
        }

        // 후보 빈칸 중 하나를 랜덤 선택한다.
        int randomIndex =
            Random.Range(
                0,
                adjacentEmptyPositions.Count
            );

        Vector2Int selectedPosition =
            adjacentEmptyPositions[
                randomIndex
            ];

        // 복제 기물이 시전자 위치에서
        // 선택된 생성 위치까지 이동하도록 생성한다.
        Piece clonedPiece =
            pieceManager.ClonePieceToFromSource(
                piece,
                selectedPosition.x,
                selectedPosition.y
            );

        if (clonedPiece != null)
        {
            Debug.Log(
                $"복제 성공: " +
                $"({selectedPosition.x}, {selectedPosition.y})에 " +
                $"{piece.Team} {piece.PieceType} 복제"
            );

            return true;
        }

        return false;
    }

    // <변경부분> 젤루 폰 고유스킬: 코루틴 실행 전용 안내 함수
    // 실제 합성은 재료 이동 애니메이션을 기다려야 하므로 UseJelluSynthesisRoutine에서 처리
    private bool UseJelluSynthesis(Piece piece)
    {
        Debug.LogWarning("JelluSynthesis는 코루틴 기반 스킬입니다. TryUseUniqueSkillRoutine을 통해 실행해야 합니다.");
        return false;
    }

    // <변경부분> 젤루 합성:
    // 주변의 유효한 Jellu 재료 2개를 서로 겹치지 않게 랜덤 선택한 뒤
    // 시전자 Pawn 위치로 이동시키고 제거하여 랜덤 상위 젤루 기물로 승급한다.
    //
    // 합성 재료 판정은 BattleMoveValidator를 단일 기준으로 사용한다.
    private IEnumerator UseJelluSynthesisRoutine(
        Piece piece,
        Action<bool> onComplete)
    {
        // 실제 합성에는 기물 처리 매니저와
        // 공용 합성 재료 판정기가 모두 필요하다.
        if (pieceManager == null ||
            battleMoveValidator == null)
        {
            Debug.LogWarning(
                "BattleSkillManager 초기화가 완료되지 않아 " +
                "JelluSynthesis를 사용할 수 없습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        if (piece == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분>
        // 합성 재료 조건을 이 함수에서 다시 계산하지 않고
        // BattleMoveValidator의 공용 판정 결과를 사용한다.
        List<Piece> synthesisCandidates =
            battleMoveValidator
                .GetJelluSynthesisMaterialCandidates(
                    piece
                );

        // 합성에는 서로 다른 유효 재료가 최소 2개 필요하다.
        if (synthesisCandidates == null ||
            synthesisCandidates.Count < 2)
        {
            Debug.Log(
                $"젤루 합성 실패: " +
                $"사용 가능한 인접 젤루 재료가 부족합니다. " +
                $"현재 {synthesisCandidates?.Count ?? 0}개 / 필요 2개"
            );

            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분>
        // 첫 번째 재료를 전체 후보에서 랜덤 선택한다.
        int firstMaterialIndex =
            Random.Range(
                0,
                synthesisCandidates.Count
            );

        // <변경부분>
        // 두 번째 재료는 후보 수 - 1 범위에서 선택한 뒤
        // 첫 번째 인덱스를 건너뛰도록 보정한다.
        //
        // 기존처럼 RemoveAt()으로 후보 List 자체를 줄이지 않으므로
        // 선택 도중 List 인덱스가 깨지는 문제를 방지한다.
        int secondMaterialIndex =
            Random.Range(
                0,
                synthesisCandidates.Count - 1
            );

        if (secondMaterialIndex >=
            firstMaterialIndex)
        {
            secondMaterialIndex++;
        }

        Piece selectedMaterialA =
            synthesisCandidates[
                firstMaterialIndex
            ];

        Piece selectedMaterialB =
            synthesisCandidates[
                secondMaterialIndex
            ];

        // <변경부분>
        // 예상하지 못한 상태에서 재료가 유실됐거나
        // 동일한 기물이 선택된 경우 실제 합성을 시작하지 않는다.
        if (selectedMaterialA == null ||
            selectedMaterialB == null ||
            selectedMaterialA == selectedMaterialB)
        {
            Debug.LogWarning(
                "젤루 합성 실패: " +
                "선택된 합성 재료가 유효하지 않습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        // 현재 준비된 젤루 상위 기물 중 랜덤 승급
        List<PieceType> promotionTypes =
            new List<PieceType>
            {
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Rook
            };

        PieceType selectedPromotionType =
            promotionTypes[
                Random.Range(
                    0,
                    promotionTypes.Count
                )
            ];

        // 승급 타입에 맞는 젤루 고유스킬 결정
        UniqueSkillType promotedUniqueSkill =
            GetJelluPromotionUniqueSkill(
                selectedPromotionType
            );

        // <변경부분>
        // 선택한 두 재료를 List 인덱스로 다시 꺼내지 않고
        // 확정된 Piece 참조를 직접 전달한다.
        //
        // 따라서 selectedMaterials[0], [1]에서 발생할 수 있는
        // ArgumentOutOfRangeException 경로 자체를 제거한다.
        yield return
            pieceManager
                .PlaySynthesisMaterialMoveAnimation(
                    selectedMaterialA,
                    selectedMaterialB,
                    piece
                );

        // 연출 중 스킬 사용자 Pawn이 사라졌다면
        // 이후 승급을 실행할 수 없다.
        if (piece == null)
        {
            Debug.LogWarning(
                "젤루 합성 실패: " +
                "연출 중 스킬 사용자가 사라졌습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분>
        // 선택된 재료 두 개를 직접 제거한다.
        //
        // 별도의 selectedMaterials List가 필요하지 않으므로
        // 불필요한 List 할당과 인덱싱도 함께 제거한다.
        if (selectedMaterialA != null)
        {
            pieceManager.RemovePiece(
                selectedMaterialA
            );
        }

        if (selectedMaterialB != null)
        {
            pieceManager.RemovePiece(
                selectedMaterialB
            );
        }

        // 추후 Spine 승급 애니메이션을 연결할 자리
        yield return
            PlayJelluSynthesisPromotionEffect(
                piece
            );

        // 스킬을 사용한 Jellu Pawn을
        // 선택된 상위 Jellu 타입으로 승급시킨다.
        bool promoteSuccess =
            pieceManager
                .PromotePieceToJelluType(
                    piece,
                    selectedPromotionType,
                    promotedUniqueSkill
                );

        if (promoteSuccess == false)
        {
            Debug.LogWarning(
                "젤루 합성 실패: " +
                "승급 처리에 실패했습니다."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        Debug.Log(
            $"젤루 합성 성공: " +
            $"아군/중립 젤루 소재 2개 이동 및 제거 후 " +
            $"{selectedPromotionType}으로 승급"
        );

        onComplete?.Invoke(true);
    }

    // <변경부분> 젤루 합성 승급 연출 자리
    // 지금은 임시 대기만 넣고, 나중에 Spine 승급 애니메이션을 이 함수 안에 연결하면 됨
    private IEnumerator PlayJelluSynthesisPromotionEffect(Piece piece)
    {
        // 승급할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // <변경부분> 나중에 Spine 승급 애니메이션 호출 위치
        // 예시:
        // yield return pieceSpineController.PlayPromotionAnimation(piece);

        // 현재는 승급 타이밍이 너무 즉시 바뀌지 않도록 짧은 임시 대기만 적용
        yield return new WaitForSeconds(0.15f);
    }

    // <변경부분> 젤루 합성 승급 타입에 맞는 고유스킬을 반환하는 함수
    private UniqueSkillType GetJelluPromotionUniqueSkill(PieceType promotedType)
    {
        switch (promotedType)
        {
            // <변경부분> 젤루 Knight 고유스킬: 퇴화
            case PieceType.Knight:
                return UniqueSkillType.JelluDegeneration;

            // 젤루 벽은 Bishop 고유스킬로 이동한 상태
            case PieceType.Rook:
                return UniqueSkillType.HornHeadbutt;

            // <변경부분> 젤루 Bishop 고유스킬: 젤루 벽
            case PieceType.Bishop:
                return UniqueSkillType.JelluWall;
        }

        return UniqueSkillType.None;
    }

    // <변경부분> 증식 스킬:
    // 주변 빈칸 중 하나를 랜덤 선택하여
    // 같은 진영의 Jellu Pawn 한 기를 생성한다.
    //
    // 주변 빈칸 판정은 BattleMoveValidator를 단일 기준으로 사용한다.
    private bool UseJelluMultiply(
        Piece piece)
    {
        if (pieceManager == null ||
            battleMoveValidator == null)
        {
            Debug.LogWarning(
                "BattleSkillManager 초기화가 완료되지 않아 " +
                "JelluMultiply를 사용할 수 없습니다."
            );

            return false;
        }

        if (piece == null)
        {
            return false;
        }

        // <변경부분>
        // 실제 스킬 실행 시에도 최대 기물 수를 다시 확인한다.
        //
        // CanUseUniqueSkillEffect에서 이미 검사하지만,
        // 실행 직전 상태가 변경됐을 가능성까지 방어하기 위해 유지한다.
        if (pieceManager.CanCreatePieceForTeam(
                piece.Team) == false)
        {
            Debug.Log(
                $"증식 실패: " +
                $"{piece.Team} 진영이 최대 기물 수에 도달했습니다."
            );

            return false;
        }

        // <변경부분>
        // 공용 BattleMoveValidator가 주변 빈칸 목록을 채운다.
        //
        // adjacentEmptyPositions는 클래스에서 재사용하므로
        // 스킬 사용마다 List를 새로 할당하지 않는다.
        battleMoveValidator
            .FillAdjacentEmptyPositions(
                piece,
                adjacentEmptyPositions
            );

        if (adjacentEmptyPositions.Count == 0)
        {
            Debug.Log(
                "증식 실패: 인접한 빈칸이 없습니다."
            );

            return false;
        }

        int randomIndex =
            Random.Range(
                0,
                adjacentEmptyPositions.Count
            );

        Vector2Int selectedPosition =
            adjacentEmptyPositions[
                randomIndex
            ];

        // 젤루 Pawn이 시전자 위치에서
        // 선택된 생성 위치까지 이동하도록 생성한다.
        Piece createdPawn =
            pieceManager.SpawnJelluPawnFromSource(
                piece,
                piece.Team,
                selectedPosition.x,
                selectedPosition.y
            );

        if (createdPawn != null)
        {
            Debug.Log(
                $"증식 성공: " +
                $"({selectedPosition.x}, {selectedPosition.y})에 " +
                $"{piece.Team} 젤루 Pawn 생성"
            );

            return true;
        }

        return false;
    }



    // <변경부분> King 전용 고유스킬: 실제 타입은 유지하고 이번 턴 동안 이동/공격만 Queen처럼 처리
    private bool UseKingQueenMove(Piece piece)
    {
        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // 실제 기물 타입이 King인 경우에만 사용 가능
        if (piece.PieceType != PieceType.King)
        {
            Debug.Log("KingQueenMove 스킬은 King 타입 기물만 사용할 수 있습니다.");
            return false;
        }

        // <변경부분> 실제 PieceType은 King으로 유지하고 이동/공격 판정만 Queen으로 변경
        piece.SetTemporaryMoveType(PieceType.Queen);

        Debug.Log("KingQueenMove 스킬 성공: 이번 턴 동안 King이 Queen처럼 이동/공격합니다.");

        return true;
    }

    // <변경부분> 젤루 벽 스킬: 젤루 Rook의 진행방향 1칸 앞에 중립 Special 벽을 생성
    private bool UseJelluWall(Piece piece)
    {
        // 필요한 매니저가 연결되지 않았으면 스킬 실행 불가
        if (boardManager == null || pieceManager == null)
        {
            Debug.LogWarning("BattleSkillManager 초기화가 완료되지 않아 JelluWall을 사용할 수 없습니다.");
            return false;
        }

        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 젤루 벽은 특정 PieceType에 고정하지 않음
        // 어떤 기물이든 JelluWall 고유스킬을 가지고 있고, 젤루 태그가 있으면 사용할 수 있음
        if (piece.HasSpeciesTag(PieceSpeciesTag.Jellu) == false)
        {
            Debug.Log("젤루 벽 실패: 젤루 태그가 없는 기물입니다.");
            return false;
        }

        // <변경부분> 중립 기물은 스킬 사용자로 허용하지 않음
        if (piece.Team == PieceTeam.Neutral)
        {
            Debug.Log("젤루 벽 실패: 중립 기물은 사용할 수 없습니다.");
            return false;
        }

        // <변경부분> 진행방향 계산
        // Player는 위쪽으로 전진하므로 Y + 1
        // Enemy는 아래쪽으로 전진하므로 Y - 1
        int directionY = piece.Team == PieceTeam.Player ? 1 : -1;

        int targetX = piece.X;
        int targetY = piece.Y + directionY;

        // 보드 밖이면 실패
        if (IsInsideBoard(targetX, targetY) == false)
        {
            Debug.Log($"젤루 벽 실패: 생성 위치가 보드 밖입니다. ({targetX}, {targetY})");
            return false;
        }

        // 앞칸에 이미 기물이 있으면 실패
        if (pieceManager.IsEmpty(targetX, targetY) == false)
        {
            Debug.Log($"젤루 벽 실패: 앞칸에 이미 기물이 있습니다. ({targetX}, {targetY})");
            return false;
        }

        // <변경부분> 젤루 벽이 시전자 위치에서 생성 위치까지 포물선으로 이동하도록 생성
        Piece wallPiece = pieceManager.SpawnJelluWallFromSource(piece, targetX, targetY);

        // 생성 실패 시 스킬 실패
        if (wallPiece == null)
        {
            Debug.LogWarning("젤루 벽 실패: 벽 생성에 실패했습니다.");
            return false;
        }

        Debug.Log($"젤루 벽 성공: ({targetX}, {targetY})에 중립 젤루 벽 생성");

        return true;
    }

    // <변경부분> 퇴화 스킬: 젤루 Knight가 자기 자신에게 퇴화 상태이상을 1개 얻음
    private bool UseJelluDegeneration(Piece piece)
    {
        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 퇴화는 Knight 전용 스킬
        if (piece.PieceType != PieceType.Knight)
        {
            Debug.Log("퇴화 실패: Knight 타입만 사용할 수 있습니다.");
            return false;
        }

        // <변경부분> 젤루 태그를 가진 Knight만 사용할 수 있음
        if (piece.HasSpeciesTag(PieceSpeciesTag.Jellu) == false)
        {
            Debug.Log("퇴화 실패: 젤루 태그가 없는 Knight입니다.");
            return false;
        }

        // 중립 기물은 고유스킬 사용자로 허용하지 않음
        if (piece.Team == PieceTeam.Neutral)
        {
            Debug.Log("퇴화 실패: 중립 기물은 사용할 수 없습니다.");
            return false;
        }

        // 상태이상 데이터베이스가 없으면 상태이상 부여 불가
        if (statusEffectDatabase == null)
        {
            Debug.LogWarning("StatusEffectDatabase가 연결되지 않아 퇴화 상태이상을 부여할 수 없습니다.");
            return false;
        }

        // <변경부분> 퇴화 상태이상 데이터 가져오기
        StatusEffectData degenerationData = statusEffectDatabase.GetData(StatusEffectType.Degeneration);

        if (degenerationData == null)
        {
            Debug.LogWarning("StatusEffectDatabase에서 Degeneration 데이터를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 자기 자신에게 퇴화 상태이상 부여
        piece.AddStatusEffect(degenerationData);

        Debug.Log($"{piece.Team} {piece.PieceType}에게 퇴화 상태이상을 부여했습니다.");

        return true;
    }

    // <변경부분> 뿔 박치기 고유스킬
    // 스킬을 사용하는 기물이 Water 또는 Swamp 타일 위에 있을 때 자신에게 Breakthrough 상태이상 1턴을 부여
    private bool UseHornHeadbutt(Piece piece)
    {
        // 필요한 매니저가 연결되지 않았으면 스킬 실행 불가
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 현재 기물이 올라간 타일 정보가 없으면 스킬 실패
        if (piece.CurrentTile == null)
        {
            Debug.Log("뿔 박치기 실패: 현재 타일 정보를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 물 또는 늪 타일 위에서만 사용 가능
        if (piece.CurrentTile.TileType != TileType.Water &&
            piece.CurrentTile.TileType != TileType.Swamp)
        {
            Debug.Log($"뿔 박치기 실패: 현재 타일이 {piece.CurrentTile.TileType}입니다. Water 또는 Swamp 타일에서만 사용할 수 있습니다.");
            return false;
        }

        // <변경부분> 돌파 상태이상 데이터가 없으면 스킬 실패
        if (statusEffectDatabase == null)
        {
            Debug.LogWarning("StatusEffectDatabase가 연결되지 않아 뿔 박치기를 사용할 수 없습니다.");
            return false;
        }

        // <변경부분> 돌파 상태이상 데이터 가져오기
        StatusEffectData breakthroughData = statusEffectDatabase.GetData(StatusEffectType.Breakthrough);

        if (breakthroughData == null)
        {
            Debug.LogWarning("StatusEffectDatabase에서 Breakthrough 데이터를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 자신에게 돌파 상태이상 부여
        piece.AddStatusEffect(breakthroughData);

        Debug.Log($"뿔 박치기 성공: {piece.Team} {piece.PieceType}에게 Breakthrough 상태이상 1턴 부여");

        return true;
    }



    // <변경부분> 특정 좌표가 보드 안쪽인지 확인하는 함수
    private bool IsInsideBoard(int x, int y)
    {
        // 보드 매니저가 없으면 좌표 검사 불가
        if (boardManager == null)
        {
            return false;
        }

        return x >= 0 &&
               x < boardManager.Width &&
               y >= 0 &&
               y < boardManager.Height;
    }
}
