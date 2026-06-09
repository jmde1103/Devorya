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

    // <변경부분> 현재 위치에서 해당 기물이 이동 또는 공격 가능한 타일이 하나라도 있는지 검사하는 함수
    public bool HasAnySelectableTile(Piece piece)
    {
        // 검사할 기물이 없으면 추가 행동 불가
        if (piece == null)
        {
            return false;
        }

        // 필요한 매니저가 없으면 판정 불가
        if (boardManager == null || pieceManager == null)
        {
            Debug.LogWarning("BattleMoveValidator 초기화가 완료되지 않았습니다.");
            return false;
        }

        // <변경부분> 실제 기물 타입이 아니라 현재 이동 판정 타입 기준으로 검사
        switch (piece.GetCurrentMoveType())
        {
            case PieceType.Pawn:
                return HasAnyPawnSelectableTile(piece);

            case PieceType.Rook:
                return HasAnyLineSelectableTile(piece, 1, 0) ||
                       HasAnyLineSelectableTile(piece, -1, 0) ||
                       HasAnyLineSelectableTile(piece, 0, 1) ||
                       HasAnyLineSelectableTile(piece, 0, -1);

            case PieceType.Bishop:
                return HasAnyLineSelectableTile(piece, 1, 1) ||
                       HasAnyLineSelectableTile(piece, -1, 1) ||
                       HasAnyLineSelectableTile(piece, 1, -1) ||
                       HasAnyLineSelectableTile(piece, -1, -1);

            case PieceType.Knight:
                return HasAnyKnightSelectableTile(piece);

            case PieceType.King:
                return HasAnyKingSelectableTile(piece);

            // <변경부분> Queen은 Rook + Bishop 방향을 모두 검사
            case PieceType.Queen:
                return HasAnyLineSelectableTile(piece, 1, 0) ||
                       HasAnyLineSelectableTile(piece, -1, 0) ||
                       HasAnyLineSelectableTile(piece, 0, 1) ||
                       HasAnyLineSelectableTile(piece, 0, -1) ||
                       HasAnyLineSelectableTile(piece, 1, 1) ||
                       HasAnyLineSelectableTile(piece, -1, 1) ||
                       HasAnyLineSelectableTile(piece, 1, -1) ||
                       HasAnyLineSelectableTile(piece, -1, -1);

            default:
                return false;
        }
    }

    // <변경부분> Pawn이 현재 위치에서 이동 또는 공격 가능한 타일이 있는지 검사하는 함수
    private bool HasAnyPawnSelectableTile(Piece piece)
    {
        // 플레이어는 위쪽, 적은 아래쪽으로 전진
        int direction = piece.Team == PieceTeam.Player ? 1 : -1;

        // 전진 이동 가능 여부 검사
        int forwardX = piece.X;
        int forwardY = piece.Y + direction;

        if (IsInsideBoard(forwardX, forwardY) && pieceManager.IsEmpty(forwardX, forwardY))
        {
            return true;
        }

        // 왼쪽 대각선 공격 가능 여부 검사
        if (CanAttackTile(piece, piece.X - 1, piece.Y + direction))
        {
            return true;
        }

        // 오른쪽 대각선 공격 가능 여부 검사
        if (CanAttackTile(piece, piece.X + 1, piece.Y + direction))
        {
            return true;
        }

        return false;
    }

    // <변경부분> Rook/Bishop처럼 한 방향으로 계속 이동하는 기물의 이동 또는 공격 가능 여부를 검사하는 함수
    private bool HasAnyLineSelectableTile(Piece piece, int dirX, int dirY)
    {
        // 현재 위치에서 지정 방향으로 한 칸씩 검사
        int checkX = piece.X + dirX;
        int checkY = piece.Y + dirY;

        while (IsInsideBoard(checkX, checkY))
        {
            Piece targetPiece = pieceManager.GetPieceAt(checkX, checkY);

            // 빈칸이면 이동 가능
            if (targetPiece == null)
            {
                return true;
            }

            // 적대 기물이 있으면 공격 가능
            if (piece.IsEnemyOf(targetPiece))
            {
                return true;
            }

            // 같은 편 기물이 막고 있으면 이 방향은 더 이상 진행 불가
            return false;
        }

        return false;
    }

    // <변경부분> Knight가 현재 위치에서 이동 또는 공격 가능한 타일이 있는지 검사하는 함수
    private bool HasAnyKnightSelectableTile(Piece piece)
    {
        int[,] knightMoves =
        {
            { 1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 },
            { -1, -2 }, { -2, -1 }, { -2, 1 }, { -1, 2 }
        };

        for (int i = 0; i < knightMoves.GetLength(0); i++)
        {
            int targetX = piece.X + knightMoves[i, 0];
            int targetY = piece.Y + knightMoves[i, 1];

            if (CanMoveOrAttackTile(piece, targetX, targetY))
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분> King이 현재 위치에서 이동 또는 공격 가능한 타일이 있는지 검사하는 함수
    private bool HasAnyKingSelectableTile(Piece piece)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // 자기 위치는 검사하지 않음
                if (x == 0 && y == 0)
                {
                    continue;
                }

                int targetX = piece.X + x;
                int targetY = piece.Y + y;

                if (CanMoveOrAttackTile(piece, targetX, targetY))
                {
                    return true;
                }
            }
        }

        return false;
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
