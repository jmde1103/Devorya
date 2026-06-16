using System.Collections; // <변경부분> 기물 이동 연출 코루틴 사용
using UnityEngine;
public class PieceManager : MonoBehaviour
{
    // 보드 정보를 가져오기 위한 BoardManager 참조
    [Header("Manager")]
    [SerializeField] private BoardManager boardManager;

    // <변경부분> 기물 이동/공격/방어/스킬 생성 등 시각 연출을 담당하는 매니저
    [SerializeField] private PieceAnimationManager pieceAnimationManager;

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

    // <변경부분> 중립 기물 타입 아이콘 위치
    // 나중에는 PieceData에 포함시킬 예정이므로, 현재는 임시로 Special 위치만 PieceManager에서 관리
    [Header("Neutral Type Icon Positions")]
    [SerializeField] private Vector3 neutralSpecialTypeIconPosition;

    [Header("Player Status UI Sprites")]
    [SerializeField] private Sprite playerPawnStatusSprite;
    [SerializeField] private Sprite playerRookStatusSprite;
    [SerializeField] private Sprite playerKnightStatusSprite;
    [SerializeField] private Sprite playerBishopStatusSprite;
    [SerializeField] private Sprite playerKingStatusSprite;

    [Header("Absorbed Jellu Status UI Sprites")]
    [SerializeField] private Sprite absorbedJelluPawnStatusSprite;
    [SerializeField] private Sprite absorbedJelluRookStatusSprite;
    [SerializeField] private Sprite absorbedJelluKnightStatusSprite;
    [SerializeField] private Sprite absorbedJelluBishopStatusSprite;
    [SerializeField] private Sprite absorbedJelluKingStatusSprite;

    //중립 기물 스프라이트
    [Header("Neutral Piece Sprites")]
    [SerializeField] private Sprite obstacleSprite;

    // <변경부분> 젤루 벽 전용 스프라이트
    // Neutral + Special + Jellu 태그를 가진 벽 기물에 사용
    [SerializeField] private Sprite jelluWallSprite;

    //보드 좌표별 기물 저장 배열
    private Piece[,] pieces;

    // 기물이 타일 위에 자연스럽게 올라오도록 Y 위치 보정
    [Header("Position Setting")]
    [SerializeField] private float pieceYOffset = 0.25f;

    [Header("Piece Limit")]
    // <변경부분> 플레이어 진영이 보유할 수 있는 최대 기물 수
    [SerializeField] private int maxPlayerPieceCount = 10;

    // <변경부분> 적 진영이 보유할 수 있는 최대 기물 수
    [SerializeField] private int maxEnemyPieceCount = 10;

    private void Start()
    {
        pieces = new Piece[boardManager.Width, boardManager.Height];

        // <변경부분> PieceAnimationManager 참조가 비어 있으면 같은 오브젝트에서 자동으로 찾음
        GetPieceAnimationManager();

        // 테스트용 초기 기물 배치
        SpawnTestPieces();
    }

    // <변경부분> PieceAnimationManager 참조를 안전하게 가져오는 함수
    private PieceAnimationManager GetPieceAnimationManager()
    {
        // 인스펙터에 직접 연결되어 있으면 그대로 사용
        if (pieceAnimationManager != null)
        {
            return pieceAnimationManager;
        }

        // 같은 GameObject에 붙어 있는 PieceAnimationManager 자동 탐색
        pieceAnimationManager = GetComponent<PieceAnimationManager>();

        if (pieceAnimationManager == null)
        {
            Debug.LogError("PieceAnimationManager가 연결되지 않았습니다. PieceManager와 같은 GameObject에 PieceAnimationManager를 추가하거나 인스펙터에 연결하세요.");
        }

        return pieceAnimationManager;
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
        // <변경부분> 현재 테스트 단계에서는 적 진영을 젤루 종족 태그 보유 기물로 생성
        SpawnPiece(PieceType.Rook, PieceTeam.Enemy, 4, 5, true, UniqueSkillType.None, PieceSpeciesTag.Jellu);
        SpawnPiece(PieceType.Knight, PieceTeam.Enemy, 1, 5, true, UniqueSkillType.JelluDegeneration, PieceSpeciesTag.Jellu);
        SpawnPiece(PieceType.Bishop, PieceTeam.Enemy, 3, 5, true, UniqueSkillType.JelluWall, PieceSpeciesTag.Jellu);

        // <변경부분> 기존 증식 스킬은 젤루 King 테스트용으로 이동
        SpawnPiece(PieceType.King, PieceTeam.Enemy, 2, 5, true, UniqueSkillType.JelluMultiply, PieceSpeciesTag.Jellu);

        SpawnPiece(PieceType.Rook, PieceTeam.Enemy, 0, 5, true, UniqueSkillType.None, PieceSpeciesTag.Jellu);

        // 적 폰 배치
        for (int x = 0; x < boardManager.Width; x++)
        {
            // <변경부분> 젤루 Pawn의 새 고유스킬은 젤루 합성
            SpawnPiece(PieceType.Pawn, PieceTeam.Enemy, x, 4, true, UniqueSkillType.JelluSynthesis, PieceSpeciesTag.Jellu);
        }

        // 중립 장애물은 지금은 기본 배치에서 제외
    }

    // <변경부분> 외부에서도 스킬로 기물을 생성할 수 있도록 public으로 변경
    public Piece SpawnPiece(PieceType pieceType, PieceTeam team, int x, int y, bool canMove, UniqueSkillType uniqueSkill = UniqueSkillType.None, params PieceSpeciesTag[] speciesTags)
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

        // <변경부분> 현재 WorldRoot 확대 상태가 반영된 타일 위치 기준으로 기물 생성 위치 계산
        Vector3 spawnPosition = GetPieceWorldPosition(targetTile);

        // <변경부분> 현재 타일의 실제 월드 위치에 기물 생성
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

        // <변경부분> 생성된 기물의 실제 전투 데이터 초기화
        // 이 코드가 빠지면 Team / PieceType / 좌표 / CurrentTile / CanMove / UniqueSkill / 종족태그가 설정되지 않아 기물 선택이 막힘
        piece.Initialize(
            pieceType,
            team,
            x,
            y,
            targetTile,
            canMove,
            uniqueSkill,
            speciesTags
        );

        // <변경부분> 테스트용: 적 기물은 King을 제외하고 일반스킬을 각각 확률적으로 보유
        // 여러 일반스킬 흡수와 발동 흐름을 테스트하기 위한 임시 로직
        if (team == PieceTeam.Enemy && pieceType != PieceType.King)
        {
            // <변경부분> 테스트 단계에서는 80% 확률로 ChanceAttack을 부여
            if (Random.Range(0, 100) < 80)
            {
                piece.SetTestGeneralSkill(GeneralSkillType.ChanceAttack, 1);

                Debug.Log($"적 기물 일반 스킬 부여: {pieceType} / ChanceAttack LV.1");
            }

            // <변경부분> 테스트 단계에서는 80% 확률로 Defense를 부여
            if (Random.Range(0, 100) < 80)
            {
                piece.SetTestGeneralSkill(GeneralSkillType.Defense, 1);

                Debug.Log($"적 기물 일반 스킬 부여: {pieceType} / Defense LV.1");
            }

            // <변경부분> 테스트 단계에서는 80% 확률로 Insight를 부여
            // 적 기물을 흡수했을 때 플레이어가 Insight를 얻는지 확인하기 위한 테스트용
            if (Random.Range(0, 100) < 80)
            {
                piece.SetTestGeneralSkill(GeneralSkillType.Insight, 1);

                Debug.Log($"적 기물 일반 스킬 부여: {pieceType} / Insight LV.1");
            }
        }

        // 팀과 기물 종류에 맞는 스프라이트 적용
        ApplyPieceSprite(pieceObject, pieceType, team);

        // <변경부분> 생성된 기물의 스테이터스 UI용 스프라이트 적용
        ApplyStatusUISprite(piece);

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

        // <변경부분> 대상이 가진 일반 스킬을 흡수자 일반 스킬 슬롯에 저장하거나 성장시킴
        absorber.AbsorbGeneralSkillsFrom(targetPiece);

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

        // <변경부분> 흡수 후 스테이터스 UI에는 흡수한 Jellu의 앞면 이미지를 표시
        ApplyStatusUISprite(absorber);

        // <변경부분> 흡수 후 현재 외형 상태에 맞는 타입 아이콘 위치 적용
        ApplyCurrentTypeIconPosition(absorber);
    }

    // <변경부분> King 전용 흡수 처리 함수
    // King은 PieceType / UniqueSkill / 외형을 유지하고, 대상의 일반스킬만 획득하거나 레벨업함
    public void AbsorbGeneralSkillsOnly(Piece absorber, Piece targetPiece)
    {
        // 흡수하는 기물이나 대상 기물이 없으면 종료
        if (absorber == null || targetPiece == null)
        {
            return;
        }

        // <변경부분> 이 함수는 King 성장용이므로 King이 아닌 기물은 처리하지 않음
        if (absorber.PieceType != PieceType.King)
        {
            Debug.LogWarning("AbsorbGeneralSkillsOnly는 King 기물에게만 사용해야 합니다.");
            return;
        }

        // <변경부분> King은 대상의 타입/고유스킬/외형을 복사하지 않고 일반스킬만 흡수
        absorber.AbsorbGeneralSkillsFrom(targetPiece);

        // <변경부분> King의 외형과 타입 아이콘은 그대로 유지하되, 혹시 모를 UI 갱신을 위해 현재 상태 재적용
        RefreshPieceVisual(absorber);

        Debug.Log($"King 일반스킬 흡수 완료: 대상 {targetPiece.PieceType}");
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
        // <변경부분> 젤루 벽 전용 외형 적용
        // Neutral + Special + Jellu 태그 기물은 일반 obstacleSprite가 아니라 jelluWallSprite를 우선 사용
        else if (piece.Team == PieceTeam.Neutral &&
                 piece.PieceType == PieceType.Special &&
                 piece.HasSpeciesTag(PieceSpeciesTag.Jellu))
        {
            spriteToApply = jelluWallSprite != null ? jelluWallSprite : obstacleSprite;
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

        // <변경부분> Neutral 기물이면 Neutral 아이콘 위치 사용
        // 현재는 젤루 벽 같은 Special 중립 기물용 임시 처리
        if (piece.Team == PieceTeam.Neutral)
        {
            return GetNeutralTypeIconPosition(piece.PieceType);
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

    // <변경부분> Neutral 기물 타입에 맞는 아이콘 위치 반환
    // 현재는 젤루 벽처럼 Neutral + Special 기물만 사용하므로 Special 위치만 임시 관리
    private Vector3 GetNeutralTypeIconPosition(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Special:
                return neutralSpecialTypeIconPosition;
        }

        // 아직 중립 Pawn/Rook/Knight/Bishop/King은 사용하지 않으므로 기본 위치 반환
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

    // <변경부분> 현재 기물 상태에 맞는 스테이터스 UI용 스프라이트를 적용하는 함수
    private void ApplyStatusUISprite(Piece piece)
    {
        // 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // 현재 기물 상태에 맞는 스테이터스 UI용 스프라이트 결정
        Sprite statusSprite = GetStatusUISprite(piece);

        // 결정된 스테이터스 UI용 스프라이트를 Piece에 저장
        piece.SetStatusUISprite(statusSprite);
    }

    // <변경부분> 현재 기물 상태에 맞는 스테이터스 UI용 스프라이트를 반환하는 함수
    private Sprite GetStatusUISprite(Piece piece)
    {
        // 기물이 없으면 null 반환
        if (piece == null)
        {
            return null;
        }

        // 흡수된 Jellu 외형이면 흡수한 Jellu의 앞면 UI 스프라이트 사용
        if (piece.IsAbsorbedJelluVisual)
        {
            switch (piece.PieceType)
            {
                case PieceType.Pawn: return absorbedJelluPawnStatusSprite;
                case PieceType.Rook: return absorbedJelluRookStatusSprite;
                case PieceType.Knight: return absorbedJelluKnightStatusSprite;
                case PieceType.Bishop: return absorbedJelluBishopStatusSprite;
                case PieceType.King: return absorbedJelluKingStatusSprite;
            }
        }

        // 플레이어 기물이면 Devorya 앞면 UI 스프라이트 사용
        if (piece.Team == PieceTeam.Player)
        {
            switch (piece.PieceType)
            {
                case PieceType.Pawn: return playerPawnStatusSprite;
                case PieceType.Rook: return playerRookStatusSprite;
                case PieceType.Knight: return playerKnightStatusSprite;
                case PieceType.Bishop: return playerBishopStatusSprite;
                case PieceType.King: return playerKingStatusSprite;
            }
        }

        // <변경부분> 젤루 벽은 스테이터스 UI에서도 젤루 벽 전용 스프라이트 사용
        if (piece.Team == PieceTeam.Neutral &&
            piece.PieceType == PieceType.Special &&
            piece.HasSpeciesTag(PieceSpeciesTag.Jellu))
        {
            return jelluWallSprite != null ? jelluWallSprite : obstacleSprite;
        }

        // 적 기물은 기존 Enemy 스프라이트를 스테이터스 UI에도 사용
        return GetPieceSprite(piece.PieceType, piece.Team);
    }

    // 기물을 특정 좌표로 이동시키는 함수
    // 기존 외부 호출 호환용 함수
    public void MovePiece(Piece piece, int targetX, int targetY)
    {
        // <변경부분> 기본 이동은 연출을 포함해서 실행
        StartCoroutine(MovePieceRoutine(piece, targetX, targetY, true));
    }

    // <변경부분> 기물을 특정 좌표로 이동시키고, 호출한 쪽에서 연출 종료까지 기다릴 수 있는 코루틴
    public IEnumerator MovePieceRoutine(Piece piece, int targetX, int targetY, bool playAnimation)
    {
        // 이동할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // 기존 좌표의 기물 정보를 비워 이동 전 상태를 정리
        pieces[piece.X, piece.Y] = null;

        // 이동할 좌표의 타일 정보를 가져와 실제 이동 위치 계산에 사용
        Tile targetTile = boardManager.GetTile(targetX, targetY);

        // 이동할 타일이 없으면 이동 처리 중단
        if (targetTile == null)
        {
            yield break;
        }

        // 현재 WorldRoot 확대 상태가 반영된 타일 위치 기준으로 최종 이동 위치 계산
        Vector3 targetPosition = GetPieceWorldPosition(targetTile);

        // <변경부분> playAnimation이 true면 목표 위치까지 점프 이동 연출을 기다림
        if (playAnimation)
        {
            PieceAnimationManager animationManager = GetPieceAnimationManager();

            if (animationManager != null)
            {
                yield return animationManager.PlayPieceJumpMoveAnimation(piece, targetPosition);
            }
            else
            {
                // <변경부분> 애니메이션 매니저가 없으면 연출 없이 즉시 위치 보정
                piece.transform.position = targetPosition;
            }
        }
        else
        {
            // <변경부분> 이미 공격 연출로 목표 위치에 도착한 경우 즉시 위치 보정만 처리
            piece.transform.position = targetPosition;
        }

        // 기물의 논리 좌표와 현재 타일 정보를 갱신
        piece.SetPosition(targetX, targetY, targetTile);

        // 이동한 좌표에 기물 정보를 다시 저장
        pieces[targetX, targetY] = piece;

        // 이동한 좌표 기준으로 기물 표시 순서를 갱신
        SetPieceSortingOrder(piece.gameObject, targetX, targetY);
    }

    // <변경부분> 현재 WorldRoot 확대 상태가 반영된 타일의 실제 월드 위치를 기준으로 기물 위치 계산
    private Vector3 GetPieceWorldPosition(Tile targetTile)
    {
        // 타일이 없으면 기본 위치 반환
        if (targetTile == null)
        {
            return Vector3.zero;
        }

        // 현재 화면 확대와 이동이 반영된 타일의 실제 위치 가져오기
        Vector3 worldPosition = targetTile.transform.position;

        // 기물이 타일 위에 자연스럽게 올라오도록 Y 위치 보정
        worldPosition.y += pieceYOffset * targetTile.transform.lossyScale.y;

        return worldPosition;
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
    // <변경부분> 특정 진영의 왕 역할 기물이 살아있는지 확인하는 함수
    // KingToQueen처럼 King이 Queen 타입으로 변한 경우도 승패 조건상 생존으로 인정
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

                // <변경부분> 해당 진영의 King 또는 Queen이 있으면 왕 역할 기물이 살아있는 것으로 처리
                if (piece.Team == team && piece.PieceType == PieceType.King)
                {
                    return true;
                }
            }
        }

        // 왕 역할 기물을 찾지 못하면 false
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

    //특정 진영에서 왕 역할 기물을 제외한 기물이 하나라도 살아있는지 확인하는 함수
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

                // <변경부분> King 또는 Queen은 왕 역할 기물로 보고 제외
                if (piece.Team == team && piece.PieceType != PieceType.King)
                {
                    return true;
                }
            }
        }

        // 왕 역할 기물을 제외한 기물이 하나도 없으면 false
        return false;
    }

    // <변경부분> 특정 진영 기물들의 임시 이동 타입을 초기화하는 함수
    public void ClearTemporaryMoveTypes(PieceTeam team)
    {
        // 모든 좌표 순회
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                // 현재 좌표의 기물 확인
                Piece piece = pieces[x, y];

                // 기물이 없으면 다음 칸으로
                if (piece == null)
                {
                    continue;
                }

                // 해당 진영 기물의 임시 이동 타입 제거
                if (piece.Team == team)
                {
                    piece.ClearTemporaryMoveType();
                }
            }
        }
    }

    // <변경부분> 특정 진영의 현재 기물 수를 계산하는 함수
    public int GetPieceCount(PieceTeam team)
    {
        int count = 0;

        // 보드 전체를 돌면서 해당 진영 기물 수를 계산
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece piece = pieces[x, y];

                if (piece != null && piece.Team == team)
                {
                    count++;
                }
            }
        }

        return count;
    }

    // <변경부분> 특정 진영의 최대 기물 수를 반환하는 함수
    private int GetMaxPieceCount(PieceTeam team)
    {
        if (team == PieceTeam.Player)
        {
            return maxPlayerPieceCount;
        }

        if (team == PieceTeam.Enemy)
        {
            return maxEnemyPieceCount;
        }

        // 중립 기물은 현재 복제 제한 대상이 아니므로 제한 없음 처리
        return int.MaxValue;
    }

    // <변경부분> 특정 진영이 새 기물을 추가로 생성할 수 있는지 검사하는 함수
    public bool CanCreatePieceForTeam(PieceTeam team)
    {
        int currentCount = GetPieceCount(team);
        int maxCount = GetMaxPieceCount(team);

        return currentCount < maxCount;
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

    public void RefreshPieceVisual(Piece piece)
    {
        // 갱신할 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // <변경부분> 현재 기물 데이터에 맞는 필드 스프라이트 갱신
        ApplyCurrentVisual(piece);

        // <변경부분> 현재 기물 데이터에 맞는 스테이터스 UI 이미지 갱신
        ApplyStatusUISprite(piece);

        // <변경부분> 현재 기물 데이터에 맞는 타입 아이콘 위치 갱신
        ApplyCurrentTypeIconPosition(piece);

        // <변경부분> 현재 좌표 기준 정렬 순서 갱신
        SetPieceSortingOrder(piece.gameObject, piece.X, piece.Y);
    }

    // <변경부분> 증식 스킬로 젤루 Pawn을 생성하는 함수
    // 기존 복제 스킬과 달리 원본 타입을 복사하지 않고 항상 Pawn을 생성함
    public Piece SpawnJelluPawn(PieceTeam team, int x, int y)
    {
        // <변경부분> Player / Enemy 진영이 최대 기물 수에 도달했다면 젤루 Pawn 생성 불가
        // JelluMultiply, Degeneration 사망 효과 모두 이 함수를 사용하므로 여기서 공통 제한 처리
        if (CanCreatePieceForTeam(team) == false)
        {
            Debug.Log($"젤루 Pawn 생성 실패: {team} 진영의 최대 기물 수에 도달했습니다. 현재 {GetPieceCount(team)} / 최대 {GetMaxPieceCount(team)}");
            return null;
        }

        // <변경부분> 젤루 Pawn은 Pawn 전용 고유스킬인 JelluSynthesis를 가진 상태로 생성
        Piece createdPiece = SpawnPiece(
            PieceType.Pawn,
            team,
            x,
            y,
            true,
            UniqueSkillType.JelluSynthesis,
            PieceSpeciesTag.Jellu
        );

        // 생성 실패 시 종료
        if (createdPiece == null)
        {
            return null;
        }

        // <변경부분> Player 젤루는 뒷면, Enemy 젤루는 앞면으로 표시
        bool useBackSprite = team == PieceTeam.Player;

        // <변경부분> Player가 만든 젤루 Pawn만 흡수 젤루 뒷면 외형 사용
        createdPiece.SetAbsorbedJelluVisual(useBackSprite);

        // <변경부분> 생성 직후 현재 진영/외형 상태에 맞게 스프라이트와 UI 갱신
        RefreshPieceVisual(createdPiece);

        return createdPiece;
    }

    // <변경부분> 젤루 벽 스킬로 중립 Special 벽을 생성하는 함수
    public Piece SpawnJelluWall(int x, int y)
    {
        // <변경부분> 벽은 중립 / Special / 이동 불가 / 고유스킬 없음 / 젤루 태그 보유 상태로 생성
        Piece createdWall = SpawnPiece(
            PieceType.Special,
            PieceTeam.Neutral,
            x,
            y,
            false,
            UniqueSkillType.None,
            PieceSpeciesTag.Jellu
        );

        // 생성 실패 시 종료
        if (createdWall == null)
        {
            return null;
        }

        // <변경부분> 중립 벽은 흡수 젤루 뒷면 외형을 쓰지 않고 obstacleSprite 기반 외형을 사용
        createdWall.SetAbsorbedJelluVisual(false);

        // <변경부분> 생성 직후 중립 Special 스프라이트 / Special 아이콘 / 스테이터스 UI 갱신
        RefreshPieceVisual(createdWall);

        Debug.Log($"젤루 벽 생성 완료: Neutral Special / ({x}, {y})");

        return createdWall;
    }



    // <변경부분> 젤루 합성으로 기물을 상위 젤루 타입으로 승급시키는 함수
    public bool PromotePieceToJelluType(Piece piece, PieceType promotedType, UniqueSkillType promotedUniqueSkill = UniqueSkillType.None)
    {
        // 승급할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 젤루 합성 승급은 Pawn / King / Special을 제외
        if (promotedType == PieceType.Pawn ||
            promotedType == PieceType.King ||
            promotedType == PieceType.Special)
        {
            Debug.LogWarning($"젤루 합성 승급 실패: 허용되지 않는 타입입니다. {promotedType}");
            return false;
        }

        // <변경부분> Player 젤루는 뒷면, Enemy 젤루는 앞면으로 표시
        // Enemy 승급 시 true를 넣으면 흡수 젤루 뒷면 스프라이트가 적용되는 버그가 생김
        bool useBackSprite = piece.Team == PieceTeam.Player;

        // <변경부분> 젤루 승급 후에는 젤루 종족 태그를 유지하되,
        // 실제 필드 스프라이트는 진영에 따라 Player=뒷면 / Enemy=앞면으로 분기
        piece.ChangePieceData(promotedType, promotedUniqueSkill, useBackSprite, PieceSpeciesTag.Jellu);

        // <변경부분> 변경된 타입/외형/아이콘/UI를 즉시 갱신
        RefreshPieceVisual(piece);

        Debug.Log($"젤루 합성 승급 완료: {promotedType}");

        return true;
    }



    // 기준 기물과 동일한 정보를 가진 기물을 새 좌표에 복제 생성하는 함수
    public Piece ClonePieceTo(Piece sourcePiece, int targetX, int targetY)
    {
        // 원본 기물이 없으면 종료
        if (sourcePiece == null)
        {
            return null;
        }

        // <변경부분> 원본 기물의 소속 진영이 최대 기물 수에 도달했다면 복제 불가
        if (CanCreatePieceForTeam(sourcePiece.Team) == false)
        {
            Debug.Log($"복제 실패: {sourcePiece.Team} 진영의 최대 기물 수에 도달했습니다. 현재 {GetPieceCount(sourcePiece.Team)} / 최대 {GetMaxPieceCount(sourcePiece.Team)}");
            return null;
        }

        // 이미 기물이 있는 칸이면 생성 불가
        if (IsEmpty(targetX, targetY) == false)
        {
            return null;
        }

        // 원본 기물의 타입, 진영, 이동 가능 여부, 고유스킬, 종족 태그를 그대로 복사해서 생성
        // <변경부분> 스킬 / 아이템 / 유물 조건 판정을 위해 종족 태그도 복사
        Piece clonedPiece = SpawnPiece(
            sourcePiece.PieceType,
            sourcePiece.Team,
            targetX,
            targetY,
            sourcePiece.CanMove,
            sourcePiece.UniqueSkill,
            sourcePiece.GetSpeciesTagsCopy()
        );

        // 생성 실패 시 종료
        if (clonedPiece == null)
        {
            return null;
        }

        // <변경부분> 흡수 외형 상태 복사
        clonedPiece.SetAbsorbedJelluVisual(sourcePiece.IsAbsorbedJelluVisual);

        // <변경부분> 복제된 외형 상태 반영
        ApplyCurrentVisual(clonedPiece);

        // <변경부분> 복제된 기물의 스테이터스 UI 이미지도 현재 외형 상태에 맞게 다시 적용
        ApplyStatusUISprite(clonedPiece);

        // <변경부분> 복제된 기물의 타입 아이콘 위치도 현재 외형 상태에 맞게 다시 적용
        ApplyCurrentTypeIconPosition(clonedPiece);

        return clonedPiece;
    }

    // <변경부분> 복제 스킬 전용 생성 함수
    // 생성된 복제 기물이 원본 기물 위치에서 생성 위치까지 포물선으로 이동
    public Piece ClonePieceToFromSource(Piece sourcePiece, int targetX, int targetY)
    {
        // 기존 복제 생성 로직 재사용
        Piece clonedPiece = ClonePieceTo(sourcePiece, targetX, targetY);

        // 생성 성공 시 원본 기물 위치에서 생성 위치까지 연출
        PlaySkillSpawnAnimationFromSource(clonedPiece, sourcePiece);

        return clonedPiece;
    }

    // <변경부분> 젤루 Pawn 생성 스킬 전용 함수
    // 증식처럼 시전자가 있는 스킬에서 사용
    public Piece SpawnJelluPawnFromSource(Piece sourcePiece, PieceTeam team, int x, int y)
    {
        // 기존 젤루 Pawn 생성 로직 재사용
        Piece createdPiece = SpawnJelluPawn(team, x, y);

        // 생성 성공 시 시전자 위치에서 생성 위치까지 연출
        PlaySkillSpawnAnimationFromSource(createdPiece, sourcePiece);

        return createdPiece;
    }

    // <변경부분> 젤루 Pawn 생성 상태이상 전용 함수
    // 퇴화처럼 생성 주체 Piece가 이미 제거될 수 있는 경우 월드 좌표 기준으로 연출
    public Piece SpawnJelluPawnFromWorldPosition(PieceTeam team, int x, int y, Vector3 sourceWorldPosition)
    {
        // 기존 젤루 Pawn 생성 로직 재사용
        Piece createdPiece = SpawnJelluPawn(team, x, y);

        // 생성 성공 시 저장된 월드 위치에서 생성 위치까지 연출
        PlaySkillSpawnAnimationFromWorldPosition(createdPiece, sourceWorldPosition);

        return createdPiece;
    }

    // <변경부분> 젤루 벽 생성 스킬 전용 함수
    // 벽이 시전자 위치에서 생성 위치까지 포물선으로 이동
    public Piece SpawnJelluWallFromSource(Piece sourcePiece, int x, int y)
    {
        // 기존 젤루 벽 생성 로직 재사용
        Piece createdWall = SpawnJelluWall(x, y);

        // 생성 성공 시 시전자 위치에서 생성 위치까지 연출
        PlaySkillSpawnAnimationFromSource(createdWall, sourcePiece);

        return createdWall;
    }

    // <변경부분> 기존 내부 호출 호환용 래퍼
    // 실제 스킬 생성 포물선 연출은 PieceAnimationManager가 처리
    private void PlaySkillSpawnAnimationFromSource(Piece spawnedPiece, Piece sourcePiece)
    {
        // 생성된 기물이나 시전자가 없으면 연출 불가
        if (spawnedPiece == null || sourcePiece == null)
        {
            return;
        }

        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            return;
        }

        animationManager.PlaySkillSpawnAnimationFromSource(spawnedPiece, sourcePiece);
    }

    // <변경부분> 기존 내부 호출 호환용 래퍼
    // 실제 스킬 생성 포물선 연출은 PieceAnimationManager가 처리
    private void PlaySkillSpawnAnimationFromWorldPosition(Piece spawnedPiece, Vector3 sourceWorldPosition)
    {
        // 생성된 기물이 없으면 연출 불가
        if (spawnedPiece == null)
        {
            return;
        }

        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            return;
        }

        animationManager.PlaySkillSpawnAnimationFromWorldPosition(spawnedPiece, sourceWorldPosition);
    }

    // <변경부분> 기존 외부 호출 호환용 래퍼
    // 실제 이동 연출은 PieceAnimationManager가 처리
    public IEnumerator PlayPieceJumpMoveAnimation(Piece piece, Vector3 targetPosition)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            if (piece != null)
            {
                piece.transform.position = targetPosition;
            }

            yield break;
        }

        yield return animationManager.PlayPieceJumpMoveAnimation(piece, targetPosition);
    }

    // <변경부분> 기존 외부 호출 호환용 래퍼
    // 실제 공격/흡수 연출은 PieceAnimationManager가 처리
    public IEnumerator PlayPieceAttackMoveAnimation(Piece piece, Vector3 targetWorldPosition)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            if (piece != null)
            {
                piece.transform.position = targetWorldPosition;
            }

            yield break;
        }

        yield return animationManager.PlayPieceAttackMoveAnimation(piece, targetWorldPosition);
    }

    // <변경부분> 기존 외부 호출 호환용 래퍼
    // 실제 방어 성공 튕김 연출은 PieceAnimationManager가 처리
    public IEnumerator PlayPieceBlockedAttackMoveAnimation(Piece piece, Vector3 targetWorldPosition)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            yield break;
        }

        yield return animationManager.PlayPieceBlockedAttackMoveAnimation(piece, targetWorldPosition);
    }

    // <변경부분> 기존 외부 호출 호환용 래퍼
    // 실제 젤루 합성 재료 이동 연출은 PieceAnimationManager가 처리
    public IEnumerator PlaySynthesisMaterialMoveAnimation(Piece firstMaterial, Piece secondMaterial, Piece targetPawn)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            yield break;
        }

        yield return animationManager.PlaySynthesisMaterialMoveAnimation(firstMaterial, secondMaterial, targetPawn);
    }
}


