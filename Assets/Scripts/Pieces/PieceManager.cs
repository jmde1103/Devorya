using NUnit.Framework.Interfaces;
using UnityEngine;

public class PieceManager : MonoBehaviour
{
    // 보드 정보를 가져오기 위한 BoardManager 참조
    [Header("Manager")]
    [SerializeField] private BoardManager boardManager;

    // 생성한 기물들을 정리해서 담아둘 부모 오브젝트
    [SerializeField] private Transform pieceParent;

    // <변경부분> 모든 기물이 공통으로 사용하는 단일 프리팹
    [Header("Piece Prefab")]
    [SerializeField] private GameObject piecePrefab;

    // 플레이어 폰 스프라이트
    [Header("Piece Sprites")]
    [SerializeField] private Sprite playerPawnSprite;

    // 적 폰 스프라이트
    [SerializeField] private Sprite enemyPawnSprite;

    // 중립 장애물 스프라이트

    [SerializeField] private Sprite obstacleSprite;

    // 기물이 타일 위에 자연스럽게 올라오도록 Y 위치 보정
    [Header("Position Setting")]
    [SerializeField] private float pieceYOffset = 0.25f;

    // 게임 시작 시 한 번 실행
    private void Start()
    {
        // 테스트용 초기 기물 배치
        SpawnTestPieces();
    }

    // 테스트용 기물들을 보드 위에 배치하는 함수
    private void SpawnTestPieces()
    {
        // 플레이어 폰 생성
        SpawnPiece(PieceType.Pawn, PieceTeam.Player,2,0, true);

        // 적 폰 생성
        SpawnPiece(PieceType.Pawn, PieceTeam.Enemy,2,5,true);

        // 중립 장애물 생성
        SpawnPiece(PieceType.Special, PieceTeam.Neutral, 2, 2, false);
    }

    // 기물을 특정 좌표에 생성하는 함수
    private Piece SpawnPiece(PieceType pieceType, PieceTeam team, int x, int y, bool canMove)
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

        // 팀과 기물 종류에 맞는 스프라이트 적용
        ApplyPieceSprite(pieceObject, pieceType, team);

        // 기물 데이터 초기화
        piece.Initialize(pieceType, team, x, y, targetTile, canMove);

        // 기물의 아이소메트리 정렬 순서 설정
        SetPieceSortingOrder(pieceObject, x, y);

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

    //기물 종류와 팀에 맞는 스프라이트를 반환
    private Sprite GetPieceSprite(PieceType pieceType, PieceTeam team)
    {
        //현재는 Pawn과 Special만 테스트
        switch (pieceType)
        {
            
            case PieceType.Pawn:
                //플레이어 폰
                if (team == PieceTeam.Player)
                {
                    return playerPawnSprite;
                }
                //적 폰
                if (team == PieceTeam.Enemy)
                {
                    return enemyPawnSprite;
                }
                break;

            case PieceType.Special:
                //중립 장애물
                if (team == PieceTeam.Neutral)
                {
                    return obstacleSprite;
                }
                break;

        }

        return null;
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
}
