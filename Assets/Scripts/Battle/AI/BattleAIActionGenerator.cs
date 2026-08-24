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

    // <변경부분> 젤루 합성 후보를 생성할 때
    // 인접한 합성 가능 재료를 임시로 저장하는 재사용 목록
    //
    // 기물마다 새로운 List를 생성하지 않도록
    // 후보 생성기 내부에서 하나의 목록을 반복해서 사용한다.
    private readonly List<Piece> synthesisMaterialCandidates =
        new List<Piece>();

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

    // <변경부분> 기물 하나의 이동, 공격,
    // 고유스킬 행동 후보를 모두 생성한다.
    private void AddPieceActions(
        Piece actingPiece,
        List<BattleAIAction> results)
    {
        if (actingPiece == null ||
            results == null)
        {
            return;
        }

        // 플레이어 하이라이트와 같은
        // 공용 이동 판정 결과를 사용한다.
        List<Vector2Int> selectablePositions =
            battleMoveValidator.GetSelectablePositions(
                actingPiece
            );

        Vector2Int sourcePosition =
            new Vector2Int(
                actingPiece.X,
                actingPiece.Y
            );

        // 현재 기물이 사용할 수 있는
        // 일반 이동 및 공격 후보를 생성한다.
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

            // 대상 칸이 비어 있으면 일반 이동 행동이다.
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

            // 대상 칸에 적대 기물이 있으면 공격 행동이다.
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

        // <변경부분> 일반 이동·공격 후보 생성이 끝난 뒤
        // 현재 기물이 사용할 수 있는 고유스킬 후보를 추가한다.
        //
        // 고유스킬 후보 생성은 기물마다 한 번만 호출해야 한다.
        // 합성 함수 내부에서 다시 호출하면 무한 재귀가 발생한다.
        AddUniqueSkillActions(
            actingPiece,
            results
        );
    }

    // <변경부분> 기물이 보유한 고유스킬 종류에 따라
    // 해당 스킬 전용 AI 행동 후보를 생성한다.
    //
    // 각 스킬의 조건과 대상 형태가 다르므로
    // 스킬별 후보 생성 함수로 분리한다.
    private void AddUniqueSkillActions(
            Piece actingPiece,
            List<BattleAIAction> results)
        {
            if (actingPiece == null ||
                results == null)
            {
                return;
            }

            // 개별 기물의 쿨타임 또는 이번 턴 사용 상태로
            // 고유스킬을 사용할 수 없다면 후보를 만들지 않는다.
            if (actingPiece.CanUseUniqueSkill() == false)
            {
                return;
            }

        switch (actingPiece.UniqueSkill)
        {
            case UniqueSkillType.JelluSynthesis:
                AddJelluSynthesisActions(
                    actingPiece,
                    results
                );
                break;

            case UniqueSkillType.JelluDegeneration:
                AddJelluDegenerationActions(
                    actingPiece,
                    results
                );
                break;

            case UniqueSkillType.JelluMultiply:
                AddJelluMultiplyActions(
                    actingPiece,
                    results
                );
                break;

            case UniqueSkillType.HornHeadbutt:
                AddHornHeadbuttActions(
                    actingPiece,
                    results
                );
                break;
        }
    }

    // <변경부분> 현재 기물이 젤루 합성을 사용할 수 있다면
    // 해당 기물에 대한 합성 AI 행동 후보를 하나 생성한다.
    //
    // 실제 합성 재료 조건은 BattleMoveValidator의
    // FillJelluSynthesisMaterialCandidates()를 단일 기준으로 사용한다.
    //
    // AI는 특정 재료 조합을 직접 선택하지 않는다.
    // 실제 재료 2개는 스킬 발동 순간 BattleSkillManager에서 랜덤 선택한다.
    private void AddJelluSynthesisActions(
        Piece actingPiece,
        List<BattleAIAction> results)
    {
        if (actingPiece == null ||
            results == null ||
            battleMoveValidator == null)
        {
            return;
        }

        // <변경부분>
        // 합성 재료 판정을 직접 반복하지 않고
        // 공용 BattleMoveValidator의 단일 판정 규칙을 사용한다.
        //
        // 기존 List를 재사용하므로 AI 후보 생성마다
        // 새로운 List를 할당하지 않는다.
        battleMoveValidator
            .FillJelluSynthesisMaterialCandidates(
                actingPiece,
                synthesisMaterialCandidates
            );

        // 합성에는 서로 다른 유효 재료가 최소 2개 필요하다.
        if (synthesisMaterialCandidates.Count < 2)
        {
            return;
        }

        BattleAIAction synthesisAction =
            BattleAIAction.CreateJelluSynthesis(
                actingPiece
            );

        if (synthesisAction == null)
        {
            return;
        }

        results.Add(
            synthesisAction
        );
    }

    // <변경부분> 현재 기물이 젤루 퇴화를 사용할 수 있다면
    // 자신에게 적용하는 고유스킬 행동 후보를 하나 생성한다.
    //
    // 실제 사용 여부는 평가기에서
    // Knight의 이동 후 피격 위험을 기준으로 판단한다.
    private void AddJelluDegenerationActions(
        Piece actingPiece,
        List<BattleAIAction> results)
    {
        if (actingPiece == null ||
            results == null)
        {
            return;
        }

        // 퇴화는 Knight 타입 전용이다.
        if (actingPiece.PieceType !=
            PieceType.Knight)
        {
            return;
        }

        // 젤루 종족 Knight만 사용할 수 있다.
        if (actingPiece.HasSpeciesTag(
                PieceSpeciesTag.Jellu) ==
            false)
        {
            return;
        }

        // Neutral 기물은 고유스킬 사용 대상이 아니다.
        if (actingPiece.Team ==
            PieceTeam.Neutral)
        {
            return;
        }

        // 이미 퇴화 상태라면 같은 상태를 중복으로 부여하지 않는다.
        if (actingPiece.HasStatusEffect(
                StatusEffectType.Degeneration))
        {
            return;
        }

        BattleAIAction degenerationAction =
            BattleAIAction.CreateJelluDegeneration(
                actingPiece
            );

        if (degenerationAction == null)
        {
            return;
        }

        results.Add(
            degenerationAction
        );
    }

    // <변경부분> 현재 기물이 젤루 증식을 사용할 수 있다면
    // 증식 AI 행동 후보를 하나 생성한다.
    //
    // 실제 주변 빈칸 판정은 BattleMoveValidator의
    // 공용 규칙을 사용하여 실제 스킬과 AI 조건을 일치시킨다.
    private void AddJelluMultiplyActions(
        Piece actingPiece,
        List<BattleAIAction> results)
    {
        if (actingPiece == null ||
            results == null ||
            pieceManager == null ||
            battleMoveValidator == null)
        {
            return;
        }

        // 증식 AI 후보는 King 타입과
        // 실제 보유 고유스킬 JelluMultiply를 기준으로 생성한다.
        if (actingPiece.PieceType !=
                PieceType.King ||
            actingPiece.UniqueSkill !=
                UniqueSkillType.JelluMultiply)
        {
            return;
        }

        // Neutral은 실제 전투 행동 주체가 아니다.
        if (actingPiece.Team ==
            PieceTeam.Neutral)
        {
            return;
        }

        // 진영 최대 기물 수에 도달했다면
        // 실제 증식도 실패하므로 AI 후보를 생성하지 않는다.
        if (pieceManager.CanCreatePieceForTeam(
                actingPiece.Team) == false)
        {
            return;
        }

        // <변경부분>
        // 기존 AI 내부의 주변 8칸 직접 탐색을 제거하고
        // 실제 스킬과 동일한 BattleMoveValidator 판정을 사용한다.
        //
        // 존재 여부만 검사하므로 List 할당도 발생하지 않는다.
        if (battleMoveValidator
                .HasAdjacentEmptyPosition(
                    actingPiece
                ) == false)
        {
            return;
        }

        BattleAIAction multiplyAction =
            BattleAIAction.CreateJelluMultiply(
                actingPiece
            );

        if (multiplyAction == null)
        {
            return;
        }

        results.Add(
            multiplyAction
        );
    }

    // <변경부분> 젤루 룩이 Water 또는 Swamp 위에 있고,
    // 현재 공격 가능한 대상 중 Defence 상태효과 보유자가 있다면
    // 뿔 박치기 고유스킬 후보를 생성한다.
    //
    // 스킬 사용 후 같은 Enemy 턴에 행동 후보를 다시 평가하므로,
    // Breakthrough를 얻은 뒤 해당 방어 기물을 공격하게 된다.
    private void AddHornHeadbuttActions(
        Piece actingPiece,
        List<BattleAIAction> results)
    {
        if (actingPiece == null ||
            results == null ||
            pieceManager == null ||
            battleMoveValidator == null)
        {
            return;
        }

        // 실제 보유 스킬과 Rook 타입을 기준으로 검사한다.
        if (actingPiece.PieceType !=
                PieceType.Rook ||
            actingPiece.UniqueSkill !=
                UniqueSkillType.HornHeadbutt)
        {
            return;
        }

        // 실제 스킬과 동일하게 Water 또는 Swamp 위에서만 사용 가능하다.
        if (actingPiece.CurrentTile == null ||
            (actingPiece.CurrentTile.TileType != TileType.Water &&
             actingPiece.CurrentTile.TileType != TileType.Swamp))
        {
            return;
        }

        // 이미 Breakthrough 상태라면 같은 효과를 다시 사용할 필요가 없다.
        if (actingPiece.HasStatusEffect(
                StatusEffectType.Breakthrough))
        {
            return;
        }

        List<Vector2Int> selectablePositions =
            battleMoveValidator.GetSelectablePositions(
                actingPiece
            );

        bool hasDefendedAttackTarget =
            false;

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

            if (targetPiece == null ||
                actingPiece.IsEnemyOf(
                    targetPiece) == false)
            {
                continue;
            }

            if (targetPiece.HasStatusEffect(
                    StatusEffectType.Defence))
            {
                hasDefendedAttackTarget =
                    true;

                break;
            }
        }

        // 지금 공격 가능한 Defence 대상이 없으면 스킬을 아낀다.
        if (hasDefendedAttackTarget == false)
        {
            return;
        }

        BattleAIAction hornHeadbuttAction =
            BattleAIAction.CreateHornHeadbutt(
                actingPiece
            );

        if (hornHeadbuttAction == null)
        {
            return;
        }

        results.Add(
            hornHeadbuttAction
        );
    }

    // <변경부분> 개발 중 생성된 후보 수와 내용을 Console에서 확인하는 함수
    // <변경부분> 개발 중 생성된 이동, 공격,
    // 고유스킬 후보 수와 상세 내용을 Console에서 확인하는 함수
    public void DebugLogActions(
        PieceTeam actingTeam,
        List<BattleAIAction> actions)
    {
        // 행동 목록 자체가 없으면 로그를 출력할 수 없다.
        if (actions == null)
        {
            Debug.Log(
                "AI 행동 후보 테스트 실패: 행동 목록이 없습니다."
            );

            return;
        }

        // 생성된 행동 종류별 후보 개수
        int moveCount = 0;
        int attackCount = 0;
        int uniqueSkillCount = 0;

        // <변경부분> 모든 행동 후보를 순회하면서
        // 이동, 공격, 고유스킬 개수를 각각 집계한다.
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

            switch (action.ActionType)
            {
                case BattleAIActionType.Move:
                    moveCount++;
                    break;

                case BattleAIActionType.Attack:
                    attackCount++;
                    break;

                case BattleAIActionType.UniqueSkill:
                    uniqueSkillCount++;
                    break;
            }
        }

        // <변경부분> 생성된 전체 후보와
        // 행동 종류별 후보 개수를 한 번에 출력한다.
        Debug.Log(
            $"AI 행동 후보 생성 완료: " +
            $"{actingTeam} / " +
            $"전체 {actions.Count}개 / " +
            $"이동 {moveCount}개 / " +
            $"공격 {attackCount}개 / " +
            $"고유스킬 {uniqueSkillCount}개"
        );

        // <변경부분> 생성된 각 행동 후보의
        // 시전자, 위치, 공격 대상, 고유스킬 대상을 출력한다.
        for (int i = 0;
             i < actions.Count;
             i++)
        {
            BattleAIAction action =
                actions[i];

            if (action == null ||
                action.ActingPiece == null)
            {
                continue;
            }

            // 일반 공격 행동의 대상 정보
            string targetText =
                action.TargetPiece == null
                    ? "없음"
                    : $"{action.TargetPiece.Team} " +
                      $"{action.TargetPiece.PieceType} " +
                      $"({action.TargetPiece.X}, " +
                      $"{action.TargetPiece.Y})";

            // 고유스킬이 아닌 행동은
            // 고유스킬 상세 내용을 "없음"으로 표시한다.
            string uniqueSkillText =
                "없음";

        if (action.ActionType ==
     BattleAIActionType.UniqueSkill &&
 action.UniqueSkillType !=
     UniqueSkillType.None)
        {
            // <변경부분> 젤루 합성은 AI가 특정 재료를 선택하지 않는다.
            // 실제 재료는 스킬 발동 순간 무작위로 선택된다.
            if (action.UniqueSkillType ==
                UniqueSkillType.JelluSynthesis)
            {
                uniqueSkillText =
                    $"{action.UniqueSkillType} / " +
                    "실제 재료 2개는 발동 시 무작위 선택";
            }
            else
            {
                uniqueSkillText =
                    action.UniqueSkillType.ToString();
            }
        }

        Debug.Log(
                $"AI 후보: " +
                $"{action.ActionType} / " +
                $"{action.ActingPiece.Team} " +
                $"{action.ActingPiece.PieceType} / " +
                $"{action.SourcePosition} → " +
                $"{action.TargetPosition} / " +
                $"공격 대상: {targetText} / " +
                $"고유스킬: {uniqueSkillText}"
            );
        }
    }
}
