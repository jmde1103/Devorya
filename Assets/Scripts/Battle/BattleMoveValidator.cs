using System.Collections.Generic;
using UnityEngine;

// <변경부분> 전투 중 기물이 이동 또는 공격 가능한 상태인지 판정하는 클래스
public class BattleMoveValidator : MonoBehaviour
{
    // 보드 범위 확인에 사용하는 보드 매니저
    private BoardManager boardManager;

    // 기물 위치 확인에 사용하는 기물 매니저
    private PieceManager pieceManager;

    // <변경부분> BattleManager에서 전투 시작 시 이동 판정기를 초기화하는 함수
    public void Initialize(BoardManager board, PieceManager pieceManagerRef)
    {
        // 보드 크기 확인용 매니저 저장
        boardManager = board;

        // 좌표별 기물 확인용 매니저 저장
        pieceManager = pieceManagerRef;
    }

    // <변경부분> 지정한 기물 주변 8칸 중
    // 현재 생성 가능한 빈칸이 하나라도 존재하는지 확인한다.
    //
    // JelluClone / JelluMultiply의 실제 스킬 사용 가능 판정과
    // AI 행동 후보 생성이 동일한 기준을 사용하도록 공용화했다.
    //
    // 목록이 필요하지 않으므로 새 List를 생성하지 않고
    // 첫 번째 빈칸을 찾는 순간 즉시 true를 반환한다.
    public bool HasAdjacentEmptyPosition(
        Piece centerPiece)
    {
        return CollectAdjacentEmptyPositions(
            centerPiece,
            null,
            true
        );
    }

    // <변경부분> 지정한 기물 주변 8칸의
    // 모든 빈칸 좌표를 전달받은 List에 채운다.
    //
    // 실제 복제/증식 위치를 랜덤 선택해야 하는 곳에서 사용한다.
    // 호출자가 기존 List를 재사용할 수 있으므로
    // 스킬 발동마다 새로운 List를 생성하지 않아도 된다.
    public void FillAdjacentEmptyPositions(
        Piece centerPiece,
        List<Vector2Int> results)
    {
        if (results == null)
        {
            return;
        }

        // 이전 호출 결과가 남지 않도록 먼저 초기화한다.
        results.Clear();

        CollectAdjacentEmptyPositions(
            centerPiece,
            results,
            false
        );
    }

    // <변경부분> 주변 8칸 빈칸 탐색의 실제 단일 구현.
    //
    // stopAfterFirst == true:
    // 빈칸 존재 여부만 필요하므로 첫 번째 발견 즉시 종료.
    //
    // stopAfterFirst == false:
    // 발견한 모든 빈칸을 results에 저장한다.
    //
    // 이렇게 함으로써 빈칸 존재 검사와 실제 후보 수집이
    // 서로 다른 규칙으로 갈라지는 것을 방지한다.
    private bool CollectAdjacentEmptyPositions(
        Piece centerPiece,
        List<Vector2Int> results,
        bool stopAfterFirst)
    {
        if (centerPiece == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return false;
        }

        bool foundEmptyPosition =
            false;

        for (int offsetY = -1;
             offsetY <= 1;
             offsetY++)
        {
            for (int offsetX = -1;
                 offsetX <= 1;
                 offsetX++)
            {
                // 중심 기물 자신의 위치는 제외한다.
                if (offsetX == 0 &&
                    offsetY == 0)
                {
                    continue;
                }

                int targetX =
                    centerPiece.X +
                    offsetX;

                int targetY =
                    centerPiece.Y +
                    offsetY;

                // 보드 밖 좌표는 후보가 아니다.
                if (IsInsideBoard(
                        targetX,
                        targetY) ==
                    false)
                {
                    continue;
                }

                // 기존 Clone / Multiply 규칙과 동일하게
                // 해당 좌표에 기물이 없는 경우만 빈칸으로 취급한다.
                if (pieceManager.IsEmpty(
                        targetX,
                        targetY) ==
                    false)
                {
                    continue;
                }

                foundEmptyPosition =
                    true;

                // 존재 여부만 확인하는 경우
                // 첫 빈칸을 발견하는 즉시 종료한다.
                if (stopAfterFirst)
                {
                    return true;
                }

                results?.Add(
                    new Vector2Int(
                        targetX,
                        targetY
                    )
                );
            }
        }

        return foundEmptyPosition;
    }

    // <변경부분> 기물 하나가 현재 보드에서 선택할 수 있는
    // 모든 이동 및 공격 좌표를 반환하는 공용 함수
    public List<Vector2Int> GetSelectablePositions(Piece piece)
    {
        List<Vector2Int> selectablePositions =
            new List<Vector2Int>();

        // 기물이 없으면 빈 목록 반환
        if (piece == null)
        {
            return selectablePositions;
        }

        // 이동 불가능한 중립 기물 등은 행동 후보를 생성하지 않음
        if (piece.CanMove == false)
        {
            return selectablePositions;
        }

        // 필요한 매니저가 초기화되지 않았다면 판정 불가
        if (boardManager == null ||
            pieceManager == null)
        {
            Debug.LogWarning(
                "BattleMoveValidator 초기화가 완료되지 않았습니다."
            );

            return selectablePositions;
        }

        // 실제 기물 타입이 아니라
        // KingQueenMove 등의 임시 이동 타입까지 반영한 타입 사용
        PieceType moveType =
            piece.GetCurrentMoveType();

        switch (moveType)
        {
            case PieceType.Pawn:
                AddPawnSelectablePositions(
                    piece,
                    selectablePositions
                );
                break;

            case PieceType.Rook:
                AddLineSelectablePositions(
                    piece,
                    1,
                    0,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    -1,
                    0,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    0,
                    1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    0,
                    -1,
                    selectablePositions
                );
                break;

            case PieceType.Bishop:
                AddLineSelectablePositions(
                    piece,
                    1,
                    1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    -1,
                    1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    1,
                    -1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    -1,
                    -1,
                    selectablePositions
                );
                break;

            case PieceType.Knight:
                AddKnightSelectablePositions(
                    piece,
                    selectablePositions
                );
                break;

            case PieceType.King:
                AddKingSelectablePositions(
                    piece,
                    selectablePositions
                );
                break;

            case PieceType.Queen:
                // Queen은 Rook과 Bishop의 모든 방향을 사용
                AddLineSelectablePositions(
                    piece,
                    1,
                    0,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    -1,
                    0,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    0,
                    1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    0,
                    -1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    1,
                    1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    -1,
                    1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    1,
                    -1,
                    selectablePositions
                );

                AddLineSelectablePositions(
                    piece,
                    -1,
                    -1,
                    selectablePositions
                );
                break;
        }

        return selectablePositions;
    }

    // <변경부분> AI가 일반 이동 후보만 따로 평가할 수 있도록
    // 빈칸인 이동 좌표만 반환하는 함수
    public List<Vector2Int> GetMovePositions(Piece piece)
    {
        List<Vector2Int> selectablePositions =
            GetSelectablePositions(piece);

        List<Vector2Int> movePositions =
            new List<Vector2Int>();

        if (pieceManager == null)
        {
            return movePositions;
        }

        for (int i = 0;
             i < selectablePositions.Count;
             i++)
        {
            Vector2Int position =
                selectablePositions[i];

            Piece targetPiece =
                pieceManager.GetPieceAt(
                    position.x,
                    position.y
                );

            // 대상 칸이 비어 있으면 일반 이동 후보
            if (targetPiece == null)
            {
                movePositions.Add(position);
            }
        }

        return movePositions;
    }

    // <변경부분> AI가 공격 가치를 별도로 평가할 수 있도록
    // 적대 기물이 있는 공격 좌표만 반환하는 함수
    public List<Vector2Int> GetAttackPositions(Piece piece)
    {
        List<Vector2Int> selectablePositions =
            GetSelectablePositions(piece);

        List<Vector2Int> attackPositions =
            new List<Vector2Int>();

        if (piece == null ||
            pieceManager == null)
        {
            return attackPositions;
        }

        for (int i = 0;
             i < selectablePositions.Count;
             i++)
        {
            Vector2Int position =
                selectablePositions[i];

            Piece targetPiece =
                pieceManager.GetPieceAt(
                    position.x,
                    position.y
                );

            if (targetPiece != null &&
                piece.IsEnemyOf(targetPiece))
            {
                attackPositions.Add(position);
            }
        }

        return attackPositions;
    }

    // <변경부분> 지정한 진영에 젤루 합성 스킬을 가진
    // Jellu Pawn이 하나라도 존재하는지 확인한다.
    //
    // 현재 쿨타임이 남아 있더라도 조합 자체에는
    // 중립 젤루를 합성 재료로 활용하는 전략이 존재하므로,
    // 중립 기물 보존 판단에서는 스킬 보유 여부를 기준으로 한다.
    public bool HasJelluSynthesisUser(
        PieceTeam team)
    {
        if (boardManager == null ||
            pieceManager == null)
        {
            return false;
        }

        for (int y = 0;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                Piece piece =
                    pieceManager.GetPieceAt(
                        x,
                        y
                    );

                if (piece == null ||
                    piece.Team != team)
                {
                    continue;
                }

                // 젤루 합성은 Jellu Pawn 전용이다.
                if (piece.PieceType !=
                        PieceType.Pawn ||
                    piece.HasSpeciesTag(
                        PieceSpeciesTag.Jellu) ==
                    false)
                {
                    continue;
                }

                if (piece.UniqueSkill ==
                    UniqueSkillType.JelluSynthesis)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // <변경부분> 지정한 중립 젤루 Special이
    // 현재 해당 진영의 JelluSynthesis Pawn 인접 8칸에 있어
    // 즉시 합성 재료로 사용할 수 있는지 확인한다.
    public bool IsNeutralJelluImmediatelyUsableForSynthesis(
        Piece neutralJelluPiece,
        PieceTeam synthesisTeam)
    {
        if (neutralJelluPiece == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return false;
        }

        // 실제 중립 젤루 Special만 검사한다.
        if (neutralJelluPiece.Team !=
                PieceTeam.Neutral ||
            neutralJelluPiece.PieceType !=
                PieceType.Special ||
            neutralJelluPiece.HasSpeciesTag(
                PieceSpeciesTag.Jellu) ==
            false)
        {
            return false;
        }

        // 중립 기물 주변 8칸에
        // 해당 진영의 합성 Pawn이 있는지 확인한다.
        for (int offsetY = -1;
             offsetY <= 1;
             offsetY++)
        {
            for (int offsetX = -1;
                 offsetX <= 1;
                 offsetX++)
            {
                if (offsetX == 0 &&
                    offsetY == 0)
                {
                    continue;
                }

                int checkX =
                    neutralJelluPiece.X +
                    offsetX;

                int checkY =
                    neutralJelluPiece.Y +
                    offsetY;

                if (IsInsideBoard(
                        checkX,
                        checkY) ==
                    false)
                {
                    continue;
                }

                Piece candidatePawn =
                    pieceManager.GetPieceAt(
                        checkX,
                        checkY
                    );

                if (candidatePawn == null ||
                    candidatePawn.Team !=
                        synthesisTeam)
                {
                    continue;
                }

                if (candidatePawn.PieceType !=
                        PieceType.Pawn ||
                    candidatePawn.HasSpeciesTag(
                        PieceSpeciesTag.Jellu) ==
                    false)
                {
                    continue;
                }

                if (candidatePawn.UniqueSkill ==
                    UniqueSkillType.JelluSynthesis)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // <변경부분> 젤루 합성에 사용할 수 있는 재료 목록을 새 List로 반환한다.
    //
    // AI 평가나 실제 스킬 실행처럼 결과 목록을 따로 보관해야 하는 곳에서 사용한다.
    // 실제 합성 재료 규칙은 아래 FillJelluSynthesisMaterialCandidates() 한 곳에서만 판정한다.
    public List<Piece> GetJelluSynthesisMaterialCandidates(
        Piece actingPiece)
    {
        List<Piece> materials =
            new List<Piece>();

        FillJelluSynthesisMaterialCandidates(
            actingPiece,
            materials
        );

        return materials;
    }

    // <변경부분> 전달받은 List에 현재 사용할 수 있는 젤루 합성 재료를 채운다.
    //
    // AI 후보 생성처럼 매번 새 List를 만들 필요가 없는 곳에서는
    // 기존 List를 재사용하여 불필요한 GC 할당을 줄일 수 있다.
    //
    // 젤루 합성 재료 판정의 단일 기준:
    // - 시전자 = Jellu Pawn
    // - 시전자 주변 8칸
    // - Jellu 태그 보유
    // - King 제외
    // - 시전자와 같은 팀 또는 Neutral
    public void FillJelluSynthesisMaterialCandidates(
        Piece actingPiece,
        List<Piece> results)
    {
        if (results == null)
        {
            return;
        }

        // 이전 판정 결과가 남지 않도록 항상 먼저 초기화한다.
        results.Clear();

        if (actingPiece == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return;
        }

        // 젤루 합성은 Jellu Pawn 전용이다.
        if (actingPiece.PieceType !=
                PieceType.Pawn ||
            actingPiece.HasSpeciesTag(
                PieceSpeciesTag.Jellu) ==
            false)
        {
            return;
        }

        // 시전자 주변 8칸을 검사한다.
        for (int offsetY = -1;
             offsetY <= 1;
             offsetY++)
        {
            for (int offsetX = -1;
                 offsetX <= 1;
                 offsetX++)
            {
                // 시전자 자신의 위치는 재료에서 제외한다.
                if (offsetX == 0 &&
                    offsetY == 0)
                {
                    continue;
                }

                int targetX =
                    actingPiece.X +
                    offsetX;

                int targetY =
                    actingPiece.Y +
                    offsetY;

                if (IsInsideBoard(
                        targetX,
                        targetY) ==
                    false)
                {
                    continue;
                }

                Piece candidatePiece =
                    pieceManager.GetPieceAt(
                        targetX,
                        targetY
                    );

                if (candidatePiece == null)
                {
                    continue;
                }

                // Jellu 태그가 없는 기물은 합성 재료가 아니다.
                if (candidatePiece.HasSpeciesTag(
                        PieceSpeciesTag.Jellu) ==
                    false)
                {
                    continue;
                }

                // King은 승패 조건 보호를 위해 합성 재료에서 제외한다.
                if (candidatePiece.PieceType ==
                    PieceType.King)
                {
                    continue;
                }

                // 시전자와 같은 팀 또는 Neutral Jellu만 허용한다.
                if (candidatePiece.Team !=
                        actingPiece.Team &&
                    candidatePiece.Team !=
                        PieceTeam.Neutral)
                {
                    continue;
                }

                results.Add(
                    candidatePiece
                );
            }
        }
    }


    // <변경부분> 젤루 합성 후 시전자가 지정한 타입으로 승급했다고 가정하고
    // 현재 위치에서 즉시 공격 가능한 적대 기물 좌표를 반환한다.
    //
    // 실제 PieceType, 임시 이동 타입, 좌표,
    // PieceManager의 보드 배열은 절대 변경하지 않는다.
    //
    // 합성에 사용된 materialA와 materialB는
    // 승급 시점에 제거되는 기물이므로 가상 보드에서 빈칸으로 처리한다.
    public List<Vector2Int> GetAttackPositionsForSimulatedType(
        Piece piece,
        PieceType simulatedType,
        Piece materialA,
        Piece materialB)
    {
        List<Vector2Int> attackPositions =
            new List<Vector2Int>();

        // 평가할 시전자나 필요한 매니저가 없으면
        // 가상 공격 범위를 계산할 수 없다.
        if (piece == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return attackPositions;
        }

        // 합성 후 공격 기대값은
        // 실제 이동 가능한 기물만 평가한다.
        if (piece.CanMove == false)
        {
            return attackPositions;
        }

        switch (simulatedType)
        {
            case PieceType.Knight:
                AddSimulatedKnightAttackPositions(
                    piece,
                    materialA,
                    materialB,
                    attackPositions
                );
                break;

            case PieceType.Bishop:
                AddSimulatedLineAttackPositions(
                    piece,
                    1,
                    1,
                    materialA,
                    materialB,
                    attackPositions
                );

                AddSimulatedLineAttackPositions(
                    piece,
                    -1,
                    1,
                    materialA,
                    materialB,
                    attackPositions
                );

                AddSimulatedLineAttackPositions(
                    piece,
                    1,
                    -1,
                    materialA,
                    materialB,
                    attackPositions
                );

                AddSimulatedLineAttackPositions(
                    piece,
                    -1,
                    -1,
                    materialA,
                    materialB,
                    attackPositions
                );
                break;

            case PieceType.Rook:
                AddSimulatedLineAttackPositions(
                    piece,
                    1,
                    0,
                    materialA,
                    materialB,
                    attackPositions
                );

                AddSimulatedLineAttackPositions(
                    piece,
                    -1,
                    0,
                    materialA,
                    materialB,
                    attackPositions
                );

                AddSimulatedLineAttackPositions(
                    piece,
                    0,
                    1,
                    materialA,
                    materialB,
                    attackPositions
                );

                AddSimulatedLineAttackPositions(
                    piece,
                    0,
                    -1,
                    materialA,
                    materialB,
                    attackPositions
                );
                break;
        }

        return attackPositions;
    }

    // <변경부분> AI 평가기가 지정한 실제 보드 좌표의 기물을 조회한다.
    //
    // PieceManager를 평가기에 직접 노출하지 않고,
    // 이동 판정기를 통해 현재 좌표의 기물만 안전하게 반환한다.
    public Piece GetPieceAt(
        Vector2Int position)
    {
        if (pieceManager == null)
        {
            return null;
        }

        return pieceManager.GetPieceAt(
            position.x,
            position.y
        );
    }



    // <변경부분> 합성 승급 공격 기대값 평가 중
    // 지정 좌표의 실제 기물을 외부 평가기에 반환한다.
    //
    // 합성 재료 A와 B는 합성 후 제거되므로
    // 해당 기물이라면 null을 반환한다.
    public Piece GetPieceAtAfterSimulatedSynthesis(
        Vector2Int position,
        Piece materialA,
        Piece materialB)
    {
        if (pieceManager == null)
        {
            return null;
        }

        return GetPieceAtAfterSynthesis(
            position.x,
            position.y,
            materialA,
            materialB
        );
    }

    // <변경부분> 젤루 합성 후 시전자가 지정 타입으로 승급했을 때
    // 현재 위치에서 상대 기물에게 즉시 공격받는지 검사한다.
    //
    // 실제 PieceType과 보드는 변경하지 않는다.
    // 합성 재료 A와 B는 제거된 가상 보드 상태로 처리한다.
    public bool IsSimulatedSynthesisPieceThreatened(
        Piece synthesisPiece,
        PieceType simulatedType,
        Piece materialA,
        Piece materialB)
    {
        if (synthesisPiece == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return false;
        }

        Vector2Int synthesisPosition =
            new Vector2Int(
                synthesisPiece.X,
                synthesisPiece.Y
            );

        // 합성 후에도 시전자의 팀은 그대로 유지된다.
        PieceTeam synthesisTeam =
            synthesisPiece.Team;

        // 보드 전체의 상대 기물을 검사한다.
        for (int y = 0;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                Piece attacker =
                    GetPieceAtAfterSynthesis(
                        x,
                        y,
                        materialA,
                        materialB
                    );

                if (attacker == null)
                {
                    continue;
                }

                // 합성 시전자 자신은 공격자로 검사하지 않는다.
                if (attacker == synthesisPiece)
                {
                    continue;
                }

                // 이동 불가능한 벽 또는 특수 기물은
                // 현재 전투 규칙상 공격 위협에서 제외한다.
                if (attacker.CanMove == false)
                {
                    continue;
                }

                // 같은 팀 기물은 위협이 아니다.
                if (attacker.Team == synthesisTeam)
                {
                    continue;
                }

                // Neutral은 현재 서로 적대 관계가 아닐 수 있으므로
                // 실제 적대 관계 판정을 사용한다.
                if (attacker.IsEnemyOf(
                        synthesisPiece) ==
                    false)
                {
                    continue;
                }

                Vector2Int attackerPosition =
                    new Vector2Int(
                        attacker.X,
                        attacker.Y
                    );

                if (CanPieceAttackPositionAfterSynthesis(
                        attacker,
                        attackerPosition,
                        synthesisPosition,
                        materialA,
                        materialB))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // <변경부분> 현재 기물이 이동 또는 공격 가능한 좌표를
    // 하나라도 가지고 있는지 확인하는 함수
    // 실제 좌표 판정은 GetSelectablePositions() 한곳에서 처리한다.
    public bool HasAnySelectableTile(Piece piece)
    {
        return GetSelectablePositions(piece).Count > 0;
    }

    // <변경부분> AI 행동이 실행된 이후의 가상 보드 상태에서
    // 지정한 진영의 King이 상대 기물에게 공격받는지 검사한다.
    //
    // 실제 Piece 좌표나 PieceManager 배열은 변경하지 않는다.
    // AI 평가 중 애니메이션, 사망 처리, 스킬 효과가 발생하지 않도록
    // SourcePosition과 TargetPosition만 가상으로 반영한다.
    public bool IsKingThreatenedAfterAction(
        BattleAIAction action,
        PieceTeam kingTeam)
    {
        if (action == null ||
            action.ActingPiece == null)
        {
            return false;
        }

        if (boardManager == null ||
            pieceManager == null)
        {
            Debug.LogWarning(
                "King 위험도 판정 실패: " +
                "BattleMoveValidator가 초기화되지 않았습니다."
            );

            return false;
        }

        Piece kingPiece =
            FindKingPieceAfterAction(
                action,
                kingTeam
            );

        // 해당 진영에 King이 없는 전투에서는
        // King 위험도 점수를 적용하지 않는다.
        if (kingPiece == null)
        {
            return false;
        }

        Vector2Int kingPosition =
            GetVirtualPiecePosition(
                kingPiece,
                action
            );

        // 가상 행동 이후 보드에 남아 있는 모든 적대 기물을 검사한다.
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece attacker =
                    GetVirtualPieceAt(
                        x,
                        y,
                        action
                    );

                if (attacker == null)
                {
                    continue;
                }

                if (attacker == kingPiece)
                {
                    continue;
                }

                // 이동 불가능한 벽이나 특수 기물은
                // 현재 전투 규칙상 공격 위협으로 계산하지 않는다.
                if (attacker.CanMove == false)
                {
                    continue;
                }

                if (attacker.IsEnemyOf(kingPiece) == false)
                {
                    continue;
                }

                Vector2Int attackerPosition =
                    GetVirtualPiecePosition(
                        attacker,
                        action
                    );

                if (CanPieceAttackPositionInVirtualBoard(
                        attacker,
                        attackerPosition,
                        kingPosition,
                        action))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // <변경부분> 지정한 기물이 현재 위치에서
    // 상대 기본 공격 범위에 노출되어 있는지 검사한다.
    //
    // 가상 이동을 만들지 않고 실제 보드 상태를 그대로 사용하므로,
    // 퇴화 AI가 현재 이미 공격받을 위치에 있는지 판단할 때 사용한다.
    public bool IsPieceCurrentlyThreatened(
        Piece targetPiece)
    {
        if (targetPiece == null)
        {
            return false;
        }

        if (boardManager == null ||
            pieceManager == null)
        {
            Debug.LogWarning(
                "현재 기물 위험도 판정 실패: " +
                "BattleMoveValidator가 초기화되지 않았습니다."
            );

            return false;
        }

        Vector2Int targetPosition =
            new Vector2Int(
                targetPiece.X,
                targetPiece.Y
            );

        for (int y = 0;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                Piece attacker =
                    pieceManager.GetPieceAt(
                        x,
                        y
                    );

                if (attacker == null ||
                    attacker == targetPiece)
                {
                    continue;
                }

                // 이동 불가능한 중립 벽과 Special은
                // 현재 전투 규칙상 공격 위협으로 계산하지 않는다.
                if (attacker.CanMove == false)
                {
                    continue;
                }

                if (attacker.IsEnemyOf(
                        targetPiece) ==
                    false)
                {
                    continue;
                }

                Vector2Int attackerPosition =
                    new Vector2Int(
                        attacker.X,
                        attacker.Y
                    );

                if (CanPieceAttackPositionInVirtualBoard(
                        attacker,
                        attackerPosition,
                        targetPosition,
                        null))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // <변경부분> AI 행동 이후 행동한 기물이
    // 상대 기물의 기본 공격 범위에 노출되는지 검사한다.
    //
    // 실제 Piece 좌표와 PieceManager 배열은 변경하지 않고,
    // 기존 가상 보드 판정 함수를 그대로 재사용한다.
    public bool IsActingPieceThreatenedAfterAction(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null)
        {
            return false;
        }

        if (boardManager == null ||
            pieceManager == null)
        {
            Debug.LogWarning(
                "행동 기물 위험도 판정 실패: " +
                "BattleMoveValidator가 초기화되지 않았습니다."
            );

            return false;
        }

        Piece actingPiece =
            action.ActingPiece;

        // 행동이 완료된 뒤 행동 기물은 목표 위치에 있다고 가정한다.
        Vector2Int actingPiecePosition =
            action.TargetPosition;

        // 가상 행동 이후 보드에 남아 있는 모든 기물을 검사한다.
        for (int y = 0;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                Piece attacker =
                    GetVirtualPieceAt(
                        x,
                        y,
                        action
                    );

                if (attacker == null)
                {
                    continue;
                }

                // 행동 기물 자신은 공격자로 검사하지 않는다.
                if (attacker == actingPiece)
                {
                    continue;
                }

                // 이동 불가능한 벽과 특수 기물은
                // 현재 전투 규칙상 공격 위협으로 계산하지 않는다.
                if (attacker.CanMove == false)
                {
                    continue;
                }

                // 행동 기물과 적대 관계가 아닌 기물은 제외한다.
                if (attacker.IsEnemyOf(actingPiece) == false)
                {
                    continue;
                }

                Vector2Int attackerPosition =
                    GetVirtualPiecePosition(
                        attacker,
                        action
                    );

                // 가상 행동 이후 상대 기물이 행동 기물의 위치를
                // 공격할 수 있다면 다음 턴에 잡힐 위험이 있다고 판단한다.
                if (CanPieceAttackPositionInVirtualBoard(
                        attacker,
                        attackerPosition,
                        actingPiecePosition,
                        action))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // <변경부분> AI 행동 이후에도 살아 있는
    // 지정 진영의 King 기물을 찾는다.
    private Piece FindKingPieceAfterAction(
        BattleAIAction action,
        PieceTeam kingTeam)
    {
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece piece =
                    GetVirtualPieceAt(
                        x,
                        y,
                        action
                    );

                if (piece == null)
                {
                    continue;
                }

                if (piece.Team != kingTeam)
                {
                    continue;
                }

                if (piece.PieceType == PieceType.King)
                {
                    return piece;
                }
            }
        }

        return null;
    }

    // <변경부분> 지정한 기물의 가상 행동 이후 좌표를 반환한다.
    private Vector2Int GetVirtualPiecePosition(
        Piece piece,
        BattleAIAction action)
    {
        if (piece == null)
        {
            return new Vector2Int(-1, -1);
        }

        if (action != null &&
            piece == action.ActingPiece)
        {
            return action.TargetPosition;
        }

        return new Vector2Int(
            piece.X,
            piece.Y
        );
    }

    // <변경부분> AI 행동 이후의 가상 보드에서
    // 특정 좌표에 존재하는 기물을 반환한다.
    private Piece GetVirtualPieceAt(
        int x,
        int y,
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null)
        {
            return pieceManager.GetPieceAt(x, y);
        }

        Vector2Int position =
            new Vector2Int(x, y);

        // 행동 기물의 기존 위치는 가상 상태에서 비어 있다.
        if (position == action.SourcePosition)
        {
            return null;
        }

        // 목표 위치에는 이동한 행동 기물이 존재한다.
        // 공격 대상 기물은 제거된 것으로 처리한다.
        if (position == action.TargetPosition)
        {
            return action.ActingPiece;
        }

        return pieceManager.GetPieceAt(x, y);
    }

    // <변경부분> 젤루 합성 이후의 가상 보드에서
    // 특정 기물이 합성 시전자의 위치를 공격할 수 있는지 검사한다.
    //
    // 합성 재료 두 개는 제거된 빈칸으로 처리하며,
    // 공격자의 실제 이동 타입을 사용한다.
    private bool CanPieceAttackPositionAfterSynthesis(
        Piece attacker,
        Vector2Int attackerPosition,
        Vector2Int targetPosition,
        Piece materialA,
        Piece materialB)
    {
        if (attacker == null)
        {
            return false;
        }

        PieceType moveType =
            attacker.GetCurrentMoveType();

        int deltaX =
            targetPosition.x -
            attackerPosition.x;

        int deltaY =
            targetPosition.y -
            attackerPosition.y;

        switch (moveType)
        {
            case PieceType.Pawn:
                {
                    int direction =
                        attacker.Team == PieceTeam.Player
                            ? 1
                            : -1;

                    return
                        deltaY == direction &&
                        Mathf.Abs(deltaX) == 1;
                }

            case PieceType.Knight:
                {
                    int absoluteX =
                        Mathf.Abs(deltaX);

                    int absoluteY =
                        Mathf.Abs(deltaY);

                    return
                        (
                            absoluteX == 1 &&
                            absoluteY == 2
                        ) ||
                        (
                            absoluteX == 2 &&
                            absoluteY == 1
                        );
                }

            case PieceType.King:
                {
                    return
                        Mathf.Abs(deltaX) <= 1 &&
                        Mathf.Abs(deltaY) <= 1 &&
                        (
                            deltaX != 0 ||
                            deltaY != 0
                        );
                }

            case PieceType.Rook:
                {
                    if (deltaX != 0 &&
                        deltaY != 0)
                    {
                        return false;
                    }

                    return
                        IsLineClearAfterSynthesis(
                            attackerPosition,
                            targetPosition,
                            materialA,
                            materialB
                        );
                }

            case PieceType.Bishop:
                {
                    if (Mathf.Abs(deltaX) !=
                        Mathf.Abs(deltaY))
                    {
                        return false;
                    }

                    return
                        IsLineClearAfterSynthesis(
                            attackerPosition,
                            targetPosition,
                            materialA,
                            materialB
                        );
                }

            case PieceType.Queen:
                {
                    bool isStraight =
                        deltaX == 0 ||
                        deltaY == 0;

                    bool isDiagonal =
                        Mathf.Abs(deltaX) ==
                        Mathf.Abs(deltaY);

                    if (isStraight == false &&
                        isDiagonal == false)
                    {
                        return false;
                    }

                    return
                        IsLineClearAfterSynthesis(
                            attackerPosition,
                            targetPosition,
                            materialA,
                            materialB
                        );
                }
        }

        return false;
    }

    // <변경부분> 합성 재료가 제거된 가상 보드에서
    // Rook, Bishop, Queen의 공격 경로가 막혀 있는지 검사한다.
    private bool IsLineClearAfterSynthesis(
        Vector2Int sourcePosition,
        Vector2Int targetPosition,
        Piece materialA,
        Piece materialB)
    {
        int directionX =
            targetPosition.x ==
            sourcePosition.x
                ? 0
                : targetPosition.x >
                  sourcePosition.x
                    ? 1
                    : -1;

        int directionY =
            targetPosition.y ==
            sourcePosition.y
                ? 0
                : targetPosition.y >
                  sourcePosition.y
                    ? 1
                    : -1;

        int checkX =
            sourcePosition.x +
            directionX;

        int checkY =
            sourcePosition.y +
            directionY;

        while (checkX !=
                   targetPosition.x ||
               checkY !=
                   targetPosition.y)
        {
            Piece blockingPiece =
                GetPieceAtAfterSynthesis(
                    checkX,
                    checkY,
                    materialA,
                    materialB
                );

            if (blockingPiece != null)
            {
                return false;
            }

            checkX +=
                directionX;

            checkY +=
                directionY;
        }

        return true;
    }

    // <변경부분> 가상 보드 상태에서 특정 기물이
    // 목표 좌표를 공격할 수 있는지 검사한다.
    //
    // 이동 가능 위치가 아니라 실제 공격 범위만 검사하므로
    // Pawn의 전진 이동은 공격으로 계산하지 않는다.
    private bool CanPieceAttackPositionInVirtualBoard(
        Piece attacker,
        Vector2Int attackerPosition,
        Vector2Int targetPosition,
        BattleAIAction action)
    {
        if (attacker == null)
        {
            return false;
        }

        PieceType moveType =
            attacker.GetCurrentMoveType();

        int deltaX =
            targetPosition.x -
            attackerPosition.x;

        int deltaY =
            targetPosition.y -
            attackerPosition.y;

        switch (moveType)
        {
            case PieceType.Pawn:
                {
                    int direction =
                        attacker.Team == PieceTeam.Player
                            ? 1
                            : -1;

                    return deltaY == direction &&
                           Mathf.Abs(deltaX) == 1;
                }

            case PieceType.Knight:
                {
                    int absoluteX =
                        Mathf.Abs(deltaX);

                    int absoluteY =
                        Mathf.Abs(deltaY);

                    return
                        (absoluteX == 1 && absoluteY == 2) ||
                        (absoluteX == 2 && absoluteY == 1);
                }

            case PieceType.King:
                {
                    return
                        Mathf.Abs(deltaX) <= 1 &&
                        Mathf.Abs(deltaY) <= 1 &&
                        (deltaX != 0 || deltaY != 0);
                }

            case PieceType.Rook:
                {
                    if (deltaX != 0 &&
                        deltaY != 0)
                    {
                        return false;
                    }

                    return IsVirtualLineClear(
                        attackerPosition,
                        targetPosition,
                        action
                    );
                }

            case PieceType.Bishop:
                {
                    if (Mathf.Abs(deltaX) !=
                        Mathf.Abs(deltaY))
                    {
                        return false;
                    }

                    return IsVirtualLineClear(
                        attackerPosition,
                        targetPosition,
                        action
                    );
                }

            case PieceType.Queen:
                {
                    bool isStraight =
                        deltaX == 0 ||
                        deltaY == 0;

                    bool isDiagonal =
                        Mathf.Abs(deltaX) ==
                        Mathf.Abs(deltaY);

                    if (isStraight == false &&
                        isDiagonal == false)
                    {
                        return false;
                    }

                    return IsVirtualLineClear(
                        attackerPosition,
                        targetPosition,
                        action
                    );
                }
        }

        return false;
    }

    // <변경부분> Rook, Bishop, Queen의 공격 경로가
    // 가상 보드 상태에서 막혀 있지 않은지 검사한다.
    private bool IsVirtualLineClear(
        Vector2Int sourcePosition,
        Vector2Int targetPosition,
        BattleAIAction action)
    {
        int directionX =
            targetPosition.x == sourcePosition.x
                ? 0
                : targetPosition.x > sourcePosition.x
                    ? 1
                    : -1;

        int directionY =
            targetPosition.y == sourcePosition.y
                ? 0
                : targetPosition.y > sourcePosition.y
                    ? 1
                    : -1;

        int checkX =
            sourcePosition.x + directionX;

        int checkY =
            sourcePosition.y + directionY;

        while (checkX != targetPosition.x ||
               checkY != targetPosition.y)
        {
            Piece blockingPiece =
                GetVirtualPieceAt(
                    checkX,
                    checkY,
                    action
                );

            if (blockingPiece != null)
            {
                return false;
            }

            checkX += directionX;
            checkY += directionY;
        }

        return true;
    }

    // <변경부분> 합성 후 Knight로 승급했다고 가정하고
    // 현재 위치에서 공격 가능한 적대 기물 좌표를 추가한다.
    //
    // 빈칸 이동은 공격 기대값에 포함하지 않으며,
    // 적대 기물이 존재하는 좌표만 결과에 저장한다.
    private void AddSimulatedKnightAttackPositions(
        Piece piece,
        Piece materialA,
        Piece materialB,
        List<Vector2Int> results)
    {
        if (piece == null ||
            results == null)
        {
            return;
        }

        int[,] knightMoves =
        {
        { 1, 2 },
        { 2, 1 },
        { 2, -1 },
        { 1, -2 },
        { -1, -2 },
        { -2, -1 },
        { -2, 1 },
        { -1, 2 }
    };

        for (int i = 0;
             i < knightMoves.GetLength(0);
             i++)
        {
            int targetX =
                piece.X +
                knightMoves[i, 0];

            int targetY =
                piece.Y +
                knightMoves[i, 1];

            if (IsInsideBoard(
                    targetX,
                    targetY) ==
                false)
            {
                continue;
            }

            Piece targetPiece =
                GetPieceAtAfterSynthesis(
                    targetX,
                    targetY,
                    materialA,
                    materialB
                );

            // 빈칸은 이동 가능하지만
            // 이번 함수는 공격 기대값만 계산하므로 제외한다.
            if (targetPiece == null)
            {
                continue;
            }

            if (piece.IsEnemyOf(targetPiece))
            {
                results.Add(
                    new Vector2Int(
                        targetX,
                        targetY
                    )
                );
            }
        }
    }

    // <변경부분> 합성 후 Bishop 또는 Rook으로 승급했다고 가정하고
    // 지정한 한 방향의 공격 가능한 첫 적대 기물 좌표를 추가한다.
    //
    // 재료 A와 B는 합성으로 제거되므로
    // 해당 좌표를 빈칸으로 취급하여 그 뒤의 공격 경로도 계속 검사한다.
    private void AddSimulatedLineAttackPositions(
        Piece piece,
        int dirX,
        int dirY,
        Piece materialA,
        Piece materialB,
        List<Vector2Int> results)
    {
        if (piece == null ||
            results == null)
        {
            return;
        }

        int checkX =
            piece.X +
            dirX;

        int checkY =
            piece.Y +
            dirY;

        while (IsInsideBoard(
            checkX,
            checkY))
        {
            Piece targetPiece =
                GetPieceAtAfterSynthesis(
                    checkX,
                    checkY,
                    materialA,
                    materialB
                );

            // 합성 후 빈칸이라면
            // 같은 방향의 다음 좌표를 계속 검사한다.
            if (targetPiece == null)
            {
                checkX += dirX;
                checkY += dirY;

                continue;
            }

            // 처음 만난 기물이 적대 기물이라면
            // 해당 좌표를 공격 기대값 후보로 추가한다.
            if (piece.IsEnemyOf(targetPiece))
            {
                results.Add(
                    new Vector2Int(
                        checkX,
                        checkY
                    )
                );
            }

            // 아군 또는 적군 여부와 관계없이
            // 처음 만난 기물 뒤쪽은 공격할 수 없다.
            break;
        }
    }

    // <변경부분> 젤루 합성 이후의 가상 보드에서
    // 지정 좌표에 존재하는 기물을 반환한다.
    //
    // 합성 재료 A와 B가 있는 좌표는 실제 보드를 수정하지 않고
    // 평가 중에만 빈칸으로 취급한다.
    private Piece GetPieceAtAfterSynthesis(
        int x,
        int y,
        Piece materialA,
        Piece materialB)
    {
        Piece piece =
            pieceManager.GetPieceAt(
                x,
                y
            );

        if (piece == null)
        {
            return null;
        }

        // 첫 번째 합성 재료는
        // 합성 완료 후 제거된 것으로 가정한다.
        if (piece == materialA)
        {
            return null;
        }

        // 두 번째 합성 재료도
        // 합성 완료 후 제거된 것으로 가정한다.
        if (piece == materialB)
        {
            return null;
        }

        return piece;
    }

    // <변경부분> Pawn의 전진 이동과 대각선 공격 좌표를 추가한다.
    private void AddPawnSelectablePositions(
        Piece piece,
        List<Vector2Int> results)
    {
        int direction =
            piece.Team == PieceTeam.Player
                ? 1
                : -1;

        // 전진 이동 좌표
        int forwardX = piece.X;
        int forwardY = piece.Y + direction;

        if (IsInsideBoard(forwardX, forwardY) &&
            pieceManager.IsEmpty(
                forwardX,
                forwardY
            ))
        {
            results.Add(
                new Vector2Int(
                    forwardX,
                    forwardY
                )
            );
        }

        // 왼쪽 대각선 공격 좌표
        TryAddAttackPosition(
            piece,
            piece.X - 1,
            piece.Y + direction,
            results
        );

        // 오른쪽 대각선 공격 좌표
        TryAddAttackPosition(
            piece,
            piece.X + 1,
            piece.Y + direction,
            results
        );
    }

    // <변경부분> Rook, Bishop, Queen처럼
    // 한 방향으로 연속 이동하는 기물의 좌표를 추가한다.
    private void AddLineSelectablePositions(
        Piece piece,
        int dirX,
        int dirY,
        List<Vector2Int> results)
    {
        int checkX = piece.X + dirX;
        int checkY = piece.Y + dirY;

        while (IsInsideBoard(checkX, checkY))
        {
            Piece targetPiece =
                pieceManager.GetPieceAt(
                    checkX,
                    checkY
                );

            // 빈칸은 이동 가능
            if (targetPiece == null)
            {
                results.Add(
                    new Vector2Int(
                        checkX,
                        checkY
                    )
                );
            }
            else
            {
                // 적대 기물이 있는 첫 칸은 공격 가능
                if (piece.IsEnemyOf(targetPiece))
                {
                    results.Add(
                        new Vector2Int(
                            checkX,
                            checkY
                        )
                    );
                }

                // 기물이 있으면 그 뒤는 이동 불가
                break;
            }

            checkX += dirX;
            checkY += dirY;
        }
    }

    // <변경부분> Knight의 8개 이동 후보를 검사해 추가한다.
    private void AddKnightSelectablePositions(
        Piece piece,
        List<Vector2Int> results)
    {
        int[,] knightMoves =
        {
        { 1, 2 },
        { 2, 1 },
        { 2, -1 },
        { 1, -2 },
        { -1, -2 },
        { -2, -1 },
        { -2, 1 },
        { -1, 2 }
    };

        for (int i = 0;
             i < knightMoves.GetLength(0);
             i++)
        {
            int targetX =
                piece.X + knightMoves[i, 0];

            int targetY =
                piece.Y + knightMoves[i, 1];

            TryAddMoveOrAttackPosition(
                piece,
                targetX,
                targetY,
                results
            );
        }
    }

    // <변경부분> King의 주변 8칸 이동 및 공격 좌표를 추가한다.
    private void AddKingSelectablePositions(
        Piece piece,
        List<Vector2Int> results)
    {
        for (int offsetX = -1;
             offsetX <= 1;
             offsetX++)
        {
            for (int offsetY = -1;
                 offsetY <= 1;
                 offsetY++)
            {
                // 현재 위치는 제외
                if (offsetX == 0 &&
                    offsetY == 0)
                {
                    continue;
                }

                TryAddMoveOrAttackPosition(
                    piece,
                    piece.X + offsetX,
                    piece.Y + offsetY,
                    results
                );
            }
        }
    }

    // <변경부분> 단일 좌표가 이동 또는 공격 가능한 경우 목록에 추가한다.
    private void TryAddMoveOrAttackPosition(
        Piece piece,
        int x,
        int y,
        List<Vector2Int> results)
    {
        if (CanMoveOrAttackTile(
                piece,
                x,
                y))
        {
            results.Add(
                new Vector2Int(x, y)
            );
        }
    }

    // <변경부분> 단일 좌표가 공격 가능한 경우에만 목록에 추가한다.
    private void TryAddAttackPosition(
        Piece piece,
        int x,
        int y,
        List<Vector2Int> results)
    {
        if (CanAttackTile(
                piece,
                x,
                y))
        {
            results.Add(
                new Vector2Int(x, y)
            );
        }
    }

    // <변경부분> 특정 좌표가 이동 또는 공격 가능한 타일인지 검사하는 함수
    private bool CanMoveOrAttackTile(Piece piece, int x, int y)
    {
        // 보드 밖이면 불가능
        if (IsInsideBoard(x, y) == false)
        {
            return false;
        }

        Piece targetPiece = pieceManager.GetPieceAt(x, y);

        // 빈칸이면 이동 가능
        if (targetPiece == null)
        {
            return true;
        }

        // 적대 기물이 있으면 공격 가능
        return piece.IsEnemyOf(targetPiece);
    }

    // <변경부분> 특정 좌표에 공격 가능한 기물이 있는지 검사하는 함수
    private bool CanAttackTile(Piece piece, int x, int y)
    {
        // 보드 밖이면 공격 불가
        if (IsInsideBoard(x, y) == false)
        {
            return false;
        }

        Piece targetPiece = pieceManager.GetPieceAt(x, y);

        // 대상 기물이 있고 적대 관계면 공격 가능
        return targetPiece != null && piece.IsEnemyOf(targetPiece);
    }

    // <변경부분> AI 공용 이동 판정 개발 중
    // 특정 기물의 이동 및 공격 좌표를 Console에서 확인하는 테스트 함수
    public void DebugLogSelectablePositions(
        Piece piece)
    {
        if (piece == null)
        {
            Debug.Log(
                "AI 이동 좌표 테스트 실패: 기물이 없습니다."
            );

            return;
        }

        List<Vector2Int> movePositions =
            GetMovePositions(piece);

        List<Vector2Int> attackPositions =
            GetAttackPositions(piece);

        Debug.Log(
            $"AI 이동 좌표 테스트: " +
            $"{piece.Team} {piece.PieceType} / " +
            $"이동 {movePositions.Count}개 / " +
            $"공격 {attackPositions.Count}개"
        );

        for (int i = 0;
             i < movePositions.Count;
             i++)
        {
            Debug.Log(
                $"이동 후보: " +
                $"({movePositions[i].x}, " +
                $"{movePositions[i].y})"
            );
        }

        for (int i = 0;
             i < attackPositions.Count;
             i++)
        {
            Debug.Log(
                $"공격 후보: " +
                $"({attackPositions[i].x}, " +
                $"{attackPositions[i].y})"
            );
        }
    }

    // <변경부분> 좌표가 보드 안쪽인지 확인하는 함수
    private bool IsInsideBoard(int x, int y)
    {
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
