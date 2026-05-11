using UnityEngine;

public class Piece : MonoBehaviour
{
    public PieceType PieceType { get; private set; } // 현재 기물의 종류
    public PieceTeam Team { get; private set; } // 기물의 소속 진영
    public bool CanMove { get; private set; } // 기물의 소속 진영
    public int X { get; private set; } // 현재 보드 X 좌표
    public int Y { get; private set; } // 현재 보드 Y 좌표
    public Tile CurrentTile { get; private set; } // 현재 기물이 위치한 타일

    private SpriteRenderer spriteRenderer; // SpriteRenderer 캐싱

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRenderer를 한 번만 찾아 저장
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public void Initialize(PieceType pieceType, PieceTeam team, int x, int y, Tile currentTile, bool canMove = true)
    {
        PieceType = pieceType;  // 기물 종류 저장
        Team = team; // 진영 저장
        X = x; // 현재 좌표 저장
        Y = y;
        CurrentTile = currentTile; // 현재 타일 저장
        CanMove = canMove; //이동 가능 여부 저장
    }

    public bool IsEnemyOf(Piece otherPiece) // 이 기물이 특정 대상과 적대 관계인지 확인하는 함수
    {
        if (otherPiece == null)  // 대상이 없으면 적이 아님
        {
            return false;
        }

        if (Team == PieceTeam.Neutral || otherPiece.Team == PieceTeam.Neutral) // 중립 기물은 플레이어와 적 모두에게 적
        {
            return Team != otherPiece.Team;
        }

        return Team != otherPiece.Team; // 일반 진영은 서로 다르면 적
    }

    public Vector2Int GetGridPosition() // 현재 보드 좌표 반환
    {
        return new Vector2Int(X, Y);
    }
}
