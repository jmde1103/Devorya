using NUnit.Framework.Interfaces;
using UnityEngine;

public class PieceManager : MonoBehaviour
{
    // 보드 정보를 가져오기 위한 BoardManager 참조
    [Header("Manager")]
    [SerializeField] private BoardManager boardManager;

    // 생성한 기물들을 정리해서 담아둘 부모 오브젝트
    [SerializeField] private Transform pieceParent;

    // 모든 기물이 공통으로 사용하는 단일 프리팹
    [Header("Piece Prefab")]
    [SerializeField] private GameObject piecePrefab;

    // 플레이어 기물 스프라이트들
    [Header("Player Piece Sprites")]
    [SerializeField] private Sprite playerPawnSprite;
    [SerializeField] private Sprite playerRookSprite;
    [SerializeField] private Sprite playerKnightSprite;
    [SerializeField] private Sprite playerBishopSprite;
    [SerializeField] private Sprite playerKingSprite;


    // 적 기물 스프라이트들
    [Header("Enemy Piece Sprites")]
    [SerializeField] private Sprite enemyPawnSprite;
    [SerializeField] private Sprite enemyRookSprite;
    [SerializeField] private Sprite enemyKnightSprite;
    [SerializeField] private Sprite enemyBishopSprite;
    [SerializeField] private Sprite enemyKingSprite;

    [Header("Absorbed Jellu Back Sprites")]
    [SerializeField] private Sprite absorbedJelluPawnBackSprite;
    [SerializeField] private Sprite absorbedJelluRookBackSprite;
    [SerializeField] private Sprite absorbedJelluKnightBackSprite;
    [SerializeField] private Sprite absorbedJelluBishopBackSprite;
    [SerializeField] private Sprite absorbedJelluKingBackSprite;

    // Player아이콘 위치
    [Header("Player Type Icon Positions")]
    [SerializeField] private Vector3 playerPawnTypeIconPosition;
    [SerializeField] private Vector3 playerRookTypeIconPosition;
    [SerializeField] private Vector3 playerKnightTypeIconPosition;
    [SerializeField] private Vector3 playerBishopTypeIconPosition;
    [SerializeField] private Vector3 playerKingTypeIconPosition;

    // Enemy 타입 아이콘 위치
    [Header("Enemy Type Icon Positions")]
    [SerializeField] private Vector3 enemyPawnTypeIconPosition;
    [SerializeField] private Vector3 enemyRookTypeIconPosition;
    [SerializeField] private Vector3 enemyKnightTypeIconPosition;
    [SerializeField] private Vector3 enemyBishopTypeIconPosition;
    [SerializeField] private Vector3 enemyKingTypeIconPosition;


    // 흡수된 Jellu 타입 아이콘 위치
    [Header("Absorbed Jellu Type Icon Positions")]
    [SerializeField] private Vector3 absorbedJelluPawnTypeIconPosition;
    [SerializeField] private Vector3 absorbedJelluRookTypeIconPosition;
    [SerializeField] private Vector3 absorbedJelluKnightTypeIconPosition;
    [SerializeField] private Vector3 absorbedJelluBishopTypeIconPosition;
    [SerializeField] private Vector3 absorbedJelluKingTypeIconPosition;

    //중립 기물 스프라이트
    [Header("Neutral Piece Sprites")]
    [SerializeField] private Sprite obstacleSprite;

    //보드 좌표별 기물 저장 배열
    private Piece[,] pieces;

    // 기물이 타일 위에 자연스럽게 올라오도록 Y 위치 보정
    [Header("Position Setting")]
    [SerializeField] private float pieceYOffset = 0.25f;



    // 게임 시작 시 한 번 실행
    private void Start()
    {
        pieces = new Piece[boardManager.Width, boardManager.Height];
        // 테스트용 초기 기물 배치
        SpawnTestPieces();
    }

    // 테스트용 기물들을 보드 위에 배치하는 함수
    private void SpawnTestPieces()
    {
        // 플레이어 진영 아래쪽 배치
        SpawnPiece(PieceType.Rook, PieceTeam.Player, 0, 0, true);
        SpawnPiece(PieceType.Knight, PieceTeam.Player, 3, 0, true);
        SpawnPiece(PieceType.Bishop, PieceTeam.Player, 1, 0, true);
        SpawnPiece(PieceType.King, PieceTeam.Player, 2, 0, true);
        SpawnPiece(PieceType.Rook, PieceTeam.Player, 4, 0, true);

        // 플레이어 폰 배치
        for (int x = 0; x < boardManager.Width; x++)
        {
            SpawnPiece(PieceType.Pawn, PieceTeam.Player, x, 1, true);
        }

        // 적 진영 위쪽 배치
        SpawnPiece(PieceType.Rook, PieceTeam.Enemy, 0, 5, true);
        SpawnPiece(PieceType.Knight, PieceTeam.Enemy, 1, 5, true);
        SpawnPiece(PieceType.Bishop, PieceTeam.Enemy, 3, 5, true);
        SpawnPiece(PieceType.King, PieceTeam.Enemy, 2, 5, true);
        SpawnPiece(PieceType.Rook, PieceTeam.Enemy, 4, 5, true);

        // 적 폰 배치
        for (int x = 0; x < boardManager.Width; x++)
        {
            SpawnPiece(PieceType.Pawn, PieceTeam.Enemy, x, 4, true, UniqueSkillType.JelluMultiply);
        }

        // 중립 장애물은 지금은 기본 배치에서 제외
    }

    // <변경부분> 외부에서도 스킬로 기물을 생성할 수 있도록 public으로 변경
    public Piece SpawnPiece(PieceType pieceType, PieceTeam team, int x, int y, bool canMove, UniqueSkillType uniqueSkill = UniqueSkillType.None)
    {
        // 공통 프리팹이 비어 있으면 오류 출력
        if (piecePrefab == null)
        {
            Debug.LogError($"Piece Prefab이 연결되지 않았습니다.");
            return null;
        }

        // 해당 좌표의 타일 가져오기
        Tile targetTile = boardManager.GetTile(x, y);

        // 타일이 없으면 오류 출력
        if (targetTile == null)
        {
            Debug.LogError($"좌표 ({x}, {y})에 타일이 없습니다.");
            return null;
        }

        // 타일의 월드 좌표 가져오기
        Vector3 spawnPosition = boardManager.GridToWorld(x, y);

        // 기물이 타일 위에 보이도록 Y 위치 보정
        spawnPosition.y += pieceYOffset;

        // 공통 프리팹 생성
        GameObject pieceObject = Instantiate(piecePrefab, spawnPosition, Quaternion.identity, pieceParent);

        // 생성된 기물 이름 설정
        pieceObject.name = $"{team}_{pieceType}_{x}_{y}";

        // Piece 컴포넌트 가져오기
        Piece piece = pieceObject.GetComponent<Piece>();

        // Piece 컴포넌트가 없으면 오류 출력
        if (piece == null)
        {
            Debug.LogError($"{pieceObject.name}에 Piece 컴포넌트가 없습니다.");
            return null;
        }

        // 기물 데이터 초기화
        piece.Initialize(pieceType, team, x, y, targetTile, canMove, uniqueSkill);

        // 팀과 기물 종류에 맞는 스프라이트 적용
        ApplyPieceSprite(pieceObject, pieceType, team);

        // <변경부분> 생성된 기물의 현재 외형 상태에 맞는 타입 아이콘 위치 적용
        ApplyCurrentTypeIconPosition(piece);

        // 기물의 아이소메트리 정렬 순서 설정
        SetPieceSortingOrder(pieceObject, x, y);

        //생성된 기물의 좌표 배열저장
        pieces[x, y] = piece;

        // 생성한 Piece 반환
        return piece;
    }

    //기물 종류와 팀에 따라 스프라이트를 설정하는 함수
    private void ApplyPieceSprite(GameObject pieceObject, PieceType pieceType, PieceTeam team)
    {
        //SpriteRenderer 가져오기
        SpriteRenderer spriteRenderer = pieceObject.GetComponent<SpriteRenderer>();

        //SpriteRenderer가 없으면 처리하지 않음
        if (spriteRenderer == null)
        {
            return;
        }

        //적용할 스프라이트를 결정
        Sprite spriteTpApply = GetPieceSprite(pieceType, team);

        //스프라이트가 있으면 적용
        if (spriteTpApply != null)
        {
            spriteRenderer.sprite = spriteTpApply;
        }

    }

    // 흡수 대상의 데이터를 복사하고 외형을 갱신하는 함수
    public void AbsorbPiece(Piece absorber, Piece targetPiece)
    {
        // 흡수하는 기물이나 대상 기물이 없으면 종료
        if (absorber == null || targetPiece == null)
        {
            return;
        }

        // 흡수자는 Jellu 뒷면 외형 상태로 변경
        absorber.SetAbsorbedJelluVisual(true);

        // 흡수할 대상의 기물 타입 저장
        PieceType absorbedType = targetPiece.PieceType;

        // 대상 기물 데이터를 흡수자에게 복사
        absorber.AbsorbFrom(targetPiece);

        // 흡수자의 SpriteRenderer 가져오기
        SpriteRenderer spriteRenderer = absorber.GetComponent<SpriteRenderer>();

        // SpriteRenderer가 없으면 종료
        if (spriteRenderer == null)
        {
            return;
        }

        // <변경부분> 흡수 시에는 Devorya 기본 스프라이트가 아니라 흡수한 Jellu의 뒷면 스프라이트 적용
        Sprite newSprite = GetAbsorbedBackSprite(absorbedType);

        // 스프라이트가 있으면 교체
        if (newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }

        // <변경부분> 흡수 후 현재 외형 상태에 맞는 타입 아이콘 위치 적용
        ApplyCurrentTypeIconPosition(absorber);
    }

    // 흡수 후 플레이어 진영에서 사용할 Jellu 뒷면 스프라이트 반환
    private Sprite GetAbsorbedBackSprite(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn: return absorbedJelluPawnBackSprite;
            case PieceType.Rook: return absorbedJelluRookBackSprite;
            case PieceType.Knight: return absorbedJelluKnightBackSprite;
            case PieceType.Bishop: return absorbedJelluBishopBackSprite;
            case PieceType.King: return absorbedJelluKingBackSprite;
        }

        return null;
    }

    // <변경부분> 기물의 현재 외형 상태에 맞춰 스프라이트를 다시 적용하는 함수
    private void ApplyCurrentVisual(Piece piece)
    {
        // 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // SpriteRenderer 가져오기
        SpriteRenderer spriteRenderer = piece.GetComponent<SpriteRenderer>();

        // SpriteRenderer가 없으면 종료
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite spriteToApply;

        // 흡수된 Jellu 외형이면 Jellu 뒷면 스프라이트 사용
        if (piece.IsAbsorbedJelluVisual)
        {
            spriteToApply = GetAbsorbedBackSprite(piece.PieceType);
        }
        else
        {
            spriteToApply = GetPieceSprite(piece.PieceType, piece.Team);
        }

        // 스프라이트 적용
        if (spriteToApply != null)
        {
            spriteRenderer.sprite = spriteToApply;
        }
    }

    // <변경부분> 기물의 현재 외형 상태에 맞는 타입 아이콘 위치 적용
    private void ApplyCurrentTypeIconPosition(Piece piece)
    {
        // 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // 현재 기물 상태에 맞는 타입 아이콘 위치 가져오기
        Vector3 iconPosition = GetTypeIconPosition(piece);

        // 기물에 타입 아이콘 위치 적용
        piece.SetTypeIconLocalPosition(iconPosition);
    }

    // <변경부분> 현재 기물 상태에 맞는 타입 아이콘 위치 반환
    private Vector3 GetTypeIconPosition(Piece piece)
    {
        // 기물이 없으면 기본 위치 반환
        if (piece == null)
        {
            return Vector3.zero;
        }

        // 흡수된 Jellu 외형이면 흡수된 Jellu 아이콘 위치 사용
        if (piece.IsAbsorbedJelluVisual)
        {
            return GetAbsorbedJelluTypeIconPosition(piece.PieceType);
        }

        // Player 기물이면 Player 아이콘 위치 사용
        if (piece.Team == PieceTeam.Player)
        {
            return GetPlayerTypeIconPosition(piece.PieceType);
        }

        // Enemy 기물이면 Enemy 아이콘 위치 사용
        if (piece.Team == PieceTeam.Enemy)
        {
            return GetEnemyTypeIconPosition(piece.PieceType);
        }

        // 그 외 기물은 기본 위치 사용
        return Vector3.zero;
    }

    // <변경부분> Player 기물 타입에 맞는 아이콘 위치 반환
    private Vector3 GetPlayerTypeIconPosition(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return playerPawnTypeIconPosition;

            case PieceType.Rook:
                return playerRookTypeIconPosition;

            case PieceType.Knight:
                return playerKnightTypeIconPosition;

            case PieceType.Bishop:
                return playerBishopTypeIconPosition;

            case PieceType.King:
                return playerKingTypeIconPosition;
        }

        // 타입이 맞지 않으면 기본 위치 반환
        return Vector3.zero;
    }

    // <변경부분> Enemy 기물 타입에 맞는 아이콘 위치 반환
    private Vector3 GetEnemyTypeIconPosition(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return enemyPawnTypeIconPosition;

            case PieceType.Rook:
                return enemyRookTypeIconPosition;

            case PieceType.Knight:
                return enemyKnightTypeIconPosition;

            case PieceType.Bishop:
                return enemyBishopTypeIconPosition;

            case PieceType.King:
                return enemyKingTypeIconPosition;
        }

        // 타입이 맞지 않으면 기본 위치 반환
        return Vector3.zero;
    }

    // <변경부분> 흡수된 Jellu 기물 타입에 맞는 아이콘 위치 반환
    private Vector3 GetAbsorbedJelluTypeIconPosition(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return absorbedJelluPawnTypeIconPosition;

            case PieceType.Rook:
                return absorbedJelluRookTypeIconPosition;

            case PieceType.Knight:
                return absorbedJelluKnightTypeIconPosition;

            case PieceType.Bishop:
                return absorbedJelluBishopTypeIconPosition;

            case PieceType.King:
                return absorbedJelluKingTypeIconPosition;
        }

        // 타입이 맞지 않으면 기본 위치 반환
        return Vector3.zero;
    }

    public Piece GetPieceAt(int x, int y)
    {
        // 좌표가 보드 밖이면 null 반환
        if (x < 0 || x >= boardManager.Width || y < 0 || y >= boardManager.Height)
        {
            return null;
        }

        return pieces[x, y];
    }

    //특정 좌표가 비어있는지 확인
    public bool IsEmpty(int x, int y)
    {
        // 해당 좌표에 기물이 없으면 true 반환
        return GetPieceAt(x, y) == null;
    }


    // 기물 종류와 팀에 맞는 스프라이트를 반환
    private Sprite GetPieceSprite(PieceType pieceType, PieceTeam team)
    {
        if (team == PieceTeam.Player)
        {
            switch (pieceType)
            {
                case PieceType.Pawn: return playerPawnSprite;
                case PieceType.Rook: return playerRookSprite;
                case PieceType.Knight: return playerKnightSprite;
                case PieceType.Bishop: return playerBishopSprite;
                case PieceType.King: return playerKingSprite;
            }
        }

        if (team == PieceTeam.Enemy)
        {
            switch (pieceType)
            {
                case PieceType.Pawn: return enemyPawnSprite;
                case PieceType.Rook: return enemyRookSprite;
                case PieceType.Knight: return enemyKnightSprite;
                case PieceType.Bishop: return enemyBishopSprite;
                case PieceType.King: return enemyKingSprite;
            }
        }

        if (team == PieceTeam.Neutral)
        {
            if (pieceType == PieceType.Special)
            {
                return obstacleSprite;
            }
        }

        return null;
    }

    // 기물을 특정 좌표로 이동시키는 함수
    public void MovePiece(Piece piece, int targetX, int targetY)
    {
        // 이동할 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // 기존 좌표의 배열 비우기
        pieces[piece.X, piece.Y] = null;

        // 이동할 타일 가져오기
        Tile targetTile = boardManager.GetTile(targetX, targetY);

        // 타일이 없으면 종료
        if (targetTile == null)
        {
            return;
        }

        // 월드 좌표 계산
        Vector3 targetPosition = boardManager.GridToWorld(targetX, targetY);

        // 기물이 타일 위에 자연스럽게 올라오도록 Y 위치 보정
        targetPosition.y += pieceYOffset;

        // 실제 오브젝트 위치 이동
        piece.transform.position = targetPosition;

        // Piece 내부 좌표 갱신
        piece.SetPosition(targetX, targetY, targetTile);

        // 새 좌표에 기물 저장
        pieces[targetX, targetY] = piece;

        // 이동 후 정렬 순서 갱신
        SetPieceSortingOrder(piece.gameObject, targetX, targetY);
    }

    // 기물을 보드에서 제거하는 함수
    public void RemovePiece(Piece piece)
    {
        // 제거할 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // 배열에서 해당 좌표 비우기
        pieces[piece.X, piece.Y] = null;

        // 실제 오브젝트 제거
        Destroy(piece.gameObject);
    }
    // <변경부분> 특정 진영의 King이 살아있는지 확인하는 함수
    public bool HasKing(PieceTeam team)
    {
        // 모든 좌표 순회
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                // 현재 좌표의 기물
                Piece piece = pieces[x, y];

                // 기물이 없으면 다음 칸으로
                if (piece == null)
                {
                    continue;
                }

                // 해당 진영의 King이 있으면 true
                if (piece.Team == team && piece.PieceType == PieceType.King)
                {
                    return true;
                }
            }
        }

        // King을 찾지 못하면 false
        return false;
    }

    // 특정 진영의 기물이 하나라도 살아있는지 확인하는 함수
    public bool HasAnyPiece(PieceTeam team)
    {
        // 모든 좌표 순회
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                // 현재 좌표의 기물
                Piece piece = pieces[x, y];

                // 해당 진영 기물이 하나라도 있으면 true
                if (piece != null && piece.Team == team)
                {
                    return true;
                }
            }
        }

        // 하나도 없으면 false
        return false;
    }

    //특정 진영에서 King을 제외한 기물이 하나라도 살아있는지 확인하는 함수
    public bool HasAnyNonKingPiece(PieceTeam team)
    {
        // 모든 좌표 순회
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                // 현재 좌표의 기물 가져오기
                Piece piece = pieces[x, y];

                // 기물이 없으면 다음 칸으로
                if (piece == null)
                {
                    continue;
                }

                // 해당 진영이고 King이 아닌 기물이 있으면 true
                if (piece.Team == team && piece.PieceType != PieceType.King)
                {
                    return true;
                }
            }
        }

        // King을 제외한 기물이 하나도 없으면 false
        return false;
    }


    // 기물의 화면 정렬 순서를 설정하는 함수
    private void SetPieceSortingOrder(GameObject pieceObject, int x, int y)
    {
        // SpriteRenderer 가져오기
        SpriteRenderer spriteRenderer = pieceObject.GetComponent<SpriteRenderer>();

        // SpriteRenderer가 없으면 처리하지 않음
        if (spriteRenderer == null)
        {
            return;
        }

        // 타일보다 기물이 앞에 보이도록 큰 값을 더함
        spriteRenderer.sortingOrder = 100 - (x + y);
    }


    //Skill

    // 기준 기물과 동일한 정보를 가진 기물을 새 좌표에 복제 생성하는 함수
    public Piece ClonePieceTo(Piece sourcePiece, int targetX, int targetY)
    {
        // 원본 기물이 없으면 종료
        if (sourcePiece == null)
        {
            return null;
        }

        // 이미 기물이 있는 칸이면 생성 불가
        if (IsEmpty(targetX, targetY) == false)
        {
            return null;
        }

        // 원본 기물의 타입, 진영, 이동 가능 여부, 고유 스킬을 그대로 복사해서 생성
        Piece clonedPiece = SpawnPiece(
        sourcePiece.PieceType,
        sourcePiece.Team,
        targetX,
        targetY,
        sourcePiece.CanMove,
        sourcePiece.UniqueSkill
        );

        // <변경부분> 흡수 외형 상태 복사
        clonedPiece.SetAbsorbedJelluVisual(sourcePiece.IsAbsorbedJelluVisual);

        // <변경부분> 복사된 외형 상태 반영
        ApplyCurrentVisual(clonedPiece);

        // <변경부분> 복제된 기물의 현재 외형 상태에 맞는 타입 아이콘 위치 적용
        ApplyCurrentTypeIconPosition(clonedPiece);

        return clonedPiece;
    }
}
