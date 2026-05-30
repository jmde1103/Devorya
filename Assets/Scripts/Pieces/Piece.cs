using UnityEngine;

public class Piece : MonoBehaviour
{
    public PieceType PieceType { get; private set; } // 현재 기물의 종류
    public PieceTeam Team { get; private set; } // 기물의 소속 진영

    // 현재 기물이 보유한 고유 스킬
    public UniqueSkillType UniqueSkill { get; private set; }

    // 고유스킬 현재 쿨타임
    // 0이면 사용 가능, 1 이상이면 사용 불가
    [SerializeField] private int uniqueSkillCooldown = 0;
    // 고유스킬 기본 쿨타임
    // 현재 기획상 "1턴 후 재사용 가능"이므로 1로 고정
    [SerializeField] private int uniqueSkillMaxCooldown = 1;

    // 현재 턴에 고유 스킬 사용 여부
    [SerializeField] private bool hasUsedUniqueSkillThisTurn = false;

    [Header("Type Icon")]
    // 기물 타입 아이콘 전체 오브젝트
    [SerializeField] private GameObject typeIconRoot;
    // 기물 타입 아이콘 이미지
    [SerializeField] private SpriteRenderer typeIconRenderer;
    // Pawn 아이콘
    [SerializeField] private Sprite pawnIconSprite;
    // Rook 아이콘
    [SerializeField] private Sprite rookIconSprite;
    // Bishop 아이콘
    [SerializeField] private Sprite bishopIconSprite;
    // Knight 아이콘
    [SerializeField] private Sprite knightIconSprite;
    // King 아이콘
    [SerializeField] private Sprite kingIconSprite;
    // Queen 아이콘
    [SerializeField] private Sprite QueenIconSprite;
    // Special 아이콘
    [SerializeField] private Sprite specialIconSprite;

    public bool CanMove { get; private set; } // 기물의 소속 진영
    public int X { get; private set; } // 현재 보드 X 좌표
    public int Y { get; private set; } // 현재 보드 Y 좌표
    public Tile CurrentTile { get; private set; } // 현재 기물이 위치한 타일

    private SpriteRenderer spriteRenderer; // SpriteRenderer 캐싱




    public bool IsAbsorbedJelluVisual { get; private set; } // 이 기물이 흡수된 Jellu 뒷면 외형을 사용하는지 여부


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRenderer를 한 번만 찾아 저장
        // <변경부분> 기물 생성 직후 타입 아이콘 비활성화
        if (typeIconRoot != null)
        {
            typeIconRoot.SetActive(false);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public void Initialize(PieceType pieceType, PieceTeam team, int x, int y, Tile currentTile, bool canMove = true, UniqueSkillType uniqueSkill = UniqueSkillType.None)
    {
        PieceType = pieceType;  // 기물 종류 저장
        Team = team; // 진영 저장
        X = x; // 현재 좌표 저장
        Y = y;
        CurrentTile = currentTile; // 현재 타일 저장
        CanMove = canMove; //이동 가능 여부 저장
        UniqueSkill = uniqueSkill;
    }

    public void SetPosition(int x, int y, Tile newTile)  // 기물의 보드 좌표와 현재 타일 정보를 갱신하는 함수
    {
        // 새 X 좌표 저장
        X = x;

        // 새 Y 좌표 저장
        Y = y;

        // 현재 위치한 타일 갱신
        CurrentTile = newTile;
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

    // 고유 스킬 사용 가능 여부
    public bool CanUseUniqueSkill()
    {
        // 고유 스킬이 없으면 사용 불가
        if (UniqueSkill == UniqueSkillType.None)
        {
            return false;
        }

        // 고유 스킬 쿨타임이 남아 있으면 사용 불가
        if (uniqueSkillCooldown > 0)
        {
            return false;
        }

        // 이번 턴에 이미 고유 스킬을 사용했으면 사용 불가
        if (hasUsedUniqueSkillThisTurn)
        {
            return false;
        }

        // 고유 스킬 사용 가능
        return true;
    }

    // 고유 스킬 사용 완료 처리
    public void MarkUniqueSkillUsed()
    {
        // 이번 턴에 고유 스킬 사용 완료 표시
        hasUsedUniqueSkillThisTurn = true;

        // 고유 스킬 쿨타임 적용
        uniqueSkillCooldown = uniqueSkillMaxCooldown;
    }

    // 턴 시작 시 고유 스킬 사용 여부 초기화
    public void ResetUniqueSkillTurnUsage()
    {
        // 이번 턴 고유 스킬 사용 여부 초기화
        hasUsedUniqueSkillThisTurn = false;
    }

    // 고유스킬 사용 후 쿨타임 적용
    public void StartUniqueSkillCooldown()
    {
        uniqueSkillCooldown = uniqueSkillMaxCooldown;
    }

    // 턴이 지날 때 고유 스킬 쿨타임 감소
    public void ReduceUniqueSkillCooldown()
    {
        // 쿨타임이 남아 있을 때만 감소
        if (uniqueSkillCooldown > 0)
        {
            // 고유 스킬 쿨타임 1 감소
            uniqueSkillCooldown--;
        }
    }

    // UI 표시용 현재 쿨타임 반환
    public int GetUniqueSkillCooldown()
    {
        return uniqueSkillCooldown;
    }


    public Vector2Int GetGridPosition() // 현재 보드 좌표 반환
    {
        return new Vector2Int(X, Y);
    }

    // <변경부분> 기물 타입을 다른 타입으로 변경하는 함수
    public void ChangePieceType(PieceType newPieceType)
    {
        // 새로운 기물 타입 저장
        PieceType = newPieceType;
    }

    // <변경부분> 다른 기물의 핵심 데이터를 흡수해서 현재 기물에 적용하는 함수
    public void AbsorbFrom(Piece targetPiece)
    {
        // 흡수할 대상이 없으면 종료
        if (targetPiece == null)
        {
            return;
        }

        // 대상의 기물 타입 복사
        // 현재 구조에서는 PieceType이 이동 규칙과 외형 데이터의 기준이 됨
        PieceType = targetPiece.PieceType;

        // 대상의 고유 스킬 복사
        UniqueSkill = targetPiece.UniqueSkill;

        // Jellu를 흡수한 상태로 표시
        IsAbsorbedJelluVisual = true;

        // TODO: 나중에 대상의 고유 능력 복사
        // TODO: 나중에 대상의 외형 데이터 복사
        // TODO: 나중에 같은 계열 흡수 시 스킬 강화 처리

        // 주의:
        // 일반 스킬은 그대로 복사하지 않음
        // Team, X, Y, CurrentTile, CanMove는 복사하지 않음
    }

    public void SetAbsorbedJelluVisual(bool value)  // <변경부분> 흡수 외형 상태를 설정하는 함수
    {
        IsAbsorbedJelluVisual = value;
    }

    // <변경부분> 기물 타입 아이콘 표시 여부 설정
    public void SetTypeIconVisible(bool isVisible)
    {
        // 타입 아이콘 오브젝트가 없으면 종료
        if (typeIconRoot == null)
        {
            return;
        }

        // 타입 아이콘 표시 상태 변경
        typeIconRoot.SetActive(isVisible);

        // 타입 아이콘을 켤 때 현재 기물 타입에 맞는 이미지 적용
        if (isVisible)
        {
            UpdateTypeIconSprite();
        }
    }

    // <변경부분> 타입 아이콘 위치 설정
    public void SetTypeIconLocalPosition(Vector3 localPosition)
    {
        // 타입 아이콘 오브젝트가 없으면 종료
        if (typeIconRoot == null)
        {
            return;
        }

        // 타입 아이콘 위치 적용
        typeIconRoot.transform.localPosition = localPosition;
    }

    // <변경부분> 현재 기물 타입에 맞는 아이콘 스프라이트 적용
    private void UpdateTypeIconSprite()
    {
        // 타입 아이콘 이미지가 없으면 종료
        if (typeIconRenderer == null)
        {
            return;
        }

        // 현재 기물 타입에 맞는 아이콘 선택
        switch (PieceType)
        {
            case PieceType.Pawn:
                // Pawn 아이콘 적용
                typeIconRenderer.sprite = pawnIconSprite;
                break;

            case PieceType.Rook:
                // Rook 아이콘 적용
                typeIconRenderer.sprite = rookIconSprite;
                break;

            case PieceType.Bishop:
                // Bishop 아이콘 적용
                typeIconRenderer.sprite = bishopIconSprite;
                break;

            case PieceType.Knight:
                // Knight 아이콘 적용
                typeIconRenderer.sprite = knightIconSprite;
                break;

            case PieceType.King:
                // King 아이콘 적용
                typeIconRenderer.sprite = kingIconSprite;
                break;

            case PieceType.Special:
                // Special 아이콘 적용
                typeIconRenderer.sprite = specialIconSprite;
                break;
        }
    }


    // 마우스로 이 기물을 클릭했을 때 Unity가 자동 호출하는 함수
    private void OnMouseDown()
    {
        // BattleManager가 없으면 종료
        if (BattleManager.Instance == null)
        {
            return;
        }

        // 클릭한 기물이 현재 턴에 조작 가능한 기물이라면
        if (BattleManager.Instance.IsCurrentTurnPiece(this))
        {
            // 기존 선택을 취소하고 이 기물을 새로 선택
            BattleManager.Instance.SelectPiece(this);
            return;
        }

        // 이미 선택된 기물이 있고, 상대/중립 기물을 클릭한 경우
        if (BattleManager.Instance.HasSelectedPiece())
        {
            // 이 기물이 올라가 있는 타일을 선택한 것처럼 처리
            BattleManager.Instance.SelectTile(CurrentTile);
            return;
        }

        // 아무것도 선택되지 않았고 현재 턴 기물도 아니면 아무 처리하지 않음
    }
}
