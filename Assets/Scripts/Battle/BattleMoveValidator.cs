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

    // <변경부분> 기물 하나가 현재 보드에서 선택할 수 있는
    // 모든 이동 및 공격 좌표를 반환하는 공용 함수
    // 플레이어 하이라이트와 AI 후보 생성이 이 결과를 함께 사용한다.
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

    // <변경부분> 현재 기물이 이동 또는 공격 가능한 좌표를
    // 하나라도 가지고 있는지 확인하는 함수
    // 실제 좌표 판정은 GetSelectablePositions() 한곳에서 처리한다.
    public bool HasAnySelectableTile(Piece piece)
    {
        return GetSelectablePositions(piece).Count > 0;
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
