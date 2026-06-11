using System.Collections.Generic;
using UnityEngine;


public class Piece : MonoBehaviour
{
    public PieceType PieceType { get; private set; } // 현재 기물의 종류
    public PieceTeam Team { get; private set; } // 기물의 소속 진영

    // <변경부분> 이 기물이 보유한 일반스킬 목록
    [SerializeField] private List<OwnedGeneralSkillData> generalSkills = new List<OwnedGeneralSkillData>();

    // <변경부분> 이 기물이 보유한 종족 태그 목록
    // 스킬 / 아이템 / 유물 효과 조건에서 공통으로 사용
    [SerializeField] private List<PieceSpeciesTag> speciesTags = new List<PieceSpeciesTag>();

    // <변경부분> 일반 스킬 최대 레벨
    private const int MaxGeneralSkillLevel = 3;

    // 현재 기물이 보유한 고유 스킬
    public UniqueSkillType UniqueSkill { get; private set; }

    // <변경부분> 이동/공격 판정에만 사용할 임시 기물 타입
    // 실제 PieceType은 바꾸지 않고, 이번 턴 동안만 Queen처럼 이동하는 효과 등에 사용
    private PieceType? temporaryMoveType = null;

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
    
    // <변경부분> 스테이터스 UI에 표시할 현재 기물 이미지
    private Sprite statusUISprite;

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


    public void Initialize(PieceType pieceType, PieceTeam team, int x, int y, Tile currentTile, bool canMove = true, UniqueSkillType uniqueSkill = UniqueSkillType.None, params PieceSpeciesTag[] initialSpeciesTags)
    {
        PieceType = pieceType;  // 기물 종류 저장
        Team = team; // 진영 저장
        X = x; // 현재 좌표 저장
        Y = y;
        CurrentTile = currentTile; // 현재 타일 저장
        CanMove = canMove; //이동 가능 여부 저장
        UniqueSkill = uniqueSkill;

        // <변경부분> 생성 시 전달받은 종족 태그를 초기화
        SetSpeciesTags(initialSpeciesTags);
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

    // <변경부분> 고유 스킬 사용 완료 처리
    // UniqueSkillData에서 가져온 쿨타임 값을 직접 적용할 때 사용
    public void MarkUniqueSkillUsed(int cooldownTurn)
    {
        // 이번 턴에 고유 스킬 사용 완료 표시
        hasUsedUniqueSkillThisTurn = true;

        // 데이터에서 받은 쿨타임 값을 0 이상으로 보정해서 적용
        uniqueSkillCooldown = Mathf.Max(0, cooldownTurn);

        // <변경부분> 테스트용 쿨타임 적용 로그
        Debug.Log($"고유스킬 쿨타임 적용: {UniqueSkill} / Cooldown {uniqueSkillCooldown}");
    }

    // 턴 시작 시 고유 스킬 사용 여부 초기화
    public void ResetUniqueSkillTurnUsage()
    {
        // 이번 턴 고유 스킬 사용 여부 초기화
        hasUsedUniqueSkillThisTurn = false;
    }

    // <변경부분> 흡수 후 유물 효과로 추가 행동을 얻었을 때 고유스킬을 바로 사용할 수 있게 여는 함수
    public void EnableUniqueSkillAfterAbsorbChanceAttack()
    {
        // 흡수 직후 막아둔 이번 턴 고유스킬 사용 상태를 해제
        hasUsedUniqueSkillThisTurn = false;

        // 흡수 직후 적용된 고유스킬 쿨타임을 제거
        uniqueSkillCooldown = 0;
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

    // <변경부분> 현재 이동/공격 판정에 사용할 타입을 반환하는 함수
    // 임시 이동 타입이 있으면 그것을 사용하고, 없으면 실제 PieceType을 사용
    public PieceType GetCurrentMoveType()
    {
        return temporaryMoveType.HasValue ? temporaryMoveType.Value : PieceType;
    }

    // <변경부분> 이동/공격 판정용 임시 타입을 적용하는 함수
    // 실제 PieceType은 변경하지 않음
    public void SetTemporaryMoveType(PieceType moveType)
    {
        temporaryMoveType = moveType;
    }

    // <변경부분> 이동/공격 판정용 임시 타입을 제거하는 함수
    public void ClearTemporaryMoveType()
    {
        temporaryMoveType = null;
    }

    // <변경부분> 특정 종족 태그를 가지고 있는지 확인하는 함수
    public bool HasSpeciesTag(PieceSpeciesTag speciesTag)
    {
        if (speciesTag == PieceSpeciesTag.None)
        {
            return false;
        }

        return speciesTags.Contains(speciesTag);
    }

    // <변경부분> 종족 태그를 추가하는 함수
    public void AddSpeciesTag(PieceSpeciesTag speciesTag)
    {
        if (speciesTag == PieceSpeciesTag.None)
        {
            return;
        }

        if (speciesTags.Contains(speciesTag))
        {
            return;
        }

        speciesTags.Add(speciesTag);
    }

    // <변경부분> 종족 태그를 제거하는 함수
    public void RemoveSpeciesTag(PieceSpeciesTag speciesTag)
    {
        if (speciesTag == PieceSpeciesTag.None)
        {
            return;
        }

        speciesTags.Remove(speciesTag);
    }

    // <변경부분> 종족 태그 목록을 새 값으로 교체하는 함수
    public void SetSpeciesTags(params PieceSpeciesTag[] newSpeciesTags)
    {
        speciesTags.Clear();

        if (newSpeciesTags == null)
        {
            return;
        }

        for (int i = 0; i < newSpeciesTags.Length; i++)
        {
            AddSpeciesTag(newSpeciesTags[i]);
        }
    }

    // <변경부분> 현재 종족 태그 목록 복사본을 반환하는 함수
    // 복제 / 흡수 / 데이터 이전 시 사용
    public PieceSpeciesTag[] GetSpeciesTagsCopy()
    {
        return speciesTags.ToArray();
    }

    public void ChangePieceData(PieceType newPieceType, UniqueSkillType newUniqueSkill, bool isAbsorbedJelluVisual, params PieceSpeciesTag[] newSpeciesTags)
    {
        // 아이템 효과로 변경될 새 기물 타입 저장
        PieceType = newPieceType;

        // 아이템 효과로 변경될 새 고유 스킬 저장
        UniqueSkill = newUniqueSkill;

        // 젤루 외형 사용 여부 저장
        IsAbsorbedJelluVisual = isAbsorbedJelluVisual;

        // <변경부분> 변경된 기물 데이터에 맞춰 종족 태그 갱신
        SetSpeciesTags(newSpeciesTags);

        // <변경부분> 현재는 젤루 외형을 사용하는 기물은 젤루 태그도 가진 것으로 처리
        // 추후 종족 데이터화 시 외형과 종족 태그를 분리할 예정
        if (isAbsorbedJelluVisual)
        {
            AddSpeciesTag(PieceSpeciesTag.Jellu);
        }

        // 아이템으로 얻은 고유 스킬은 현재 턴에 바로 사용할 수 있게 초기화
        uniqueSkillCooldown = 0;

        // 현재 턴 고유 스킬 사용 상태 초기화
        hasUsedUniqueSkillThisTurn = false;
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

        // <변경부분> 흡수로 새로 얻은 고유스킬은 이번 턴에는 바로 사용할 수 없도록 처리
        hasUsedUniqueSkillThisTurn = true;

        // <변경부분> 흡수 직후 쿨타임은 1턴으로 설정해서 다음 턴부터 사용 가능하게 함
        uniqueSkillCooldown = 1;

        // Jellu를 흡수한 상태로 표시
        IsAbsorbedJelluVisual = true;

        // <변경부분> 흡수 대상의 종족 태그를 복사
        SetSpeciesTags(targetPiece.GetSpeciesTagsCopy());

        // <변경부분> 현재는 흡수 외형이 젤루 외형이므로 젤루 태그도 보장
        // 추후 종족 데이터화 시 외형과 종족 태그를 분리할 예정
        AddSpeciesTag(PieceSpeciesTag.Jellu);

        // TODO: 나중에 대상의 고유 능력 복사
        // TODO: 나중에 대상의 외형 데이터 복사
        // TODO: 나중에 같은 계열 흡수 시 스킬 강화 처리
    }

    public void SetAbsorbedJelluVisual(bool value)  // <변경부분> 흡수 외형 상태를 설정하는 함수
    {
        IsAbsorbedJelluVisual = value;

        // <변경부분> 현재는 젤루 외형을 사용하는 기물은 젤루 태그도 가진 것으로 처리
        // 추후 종족 데이터화 시 외형과 종족 태그를 분리할 예정
        if (value)
        {
            AddSpeciesTag(PieceSpeciesTag.Jellu);
        }
    }

    // <변경부분> PieceManager가 정한 스테이터스 UI용 스프라이트를 저장하는 함수
    public void SetStatusUISprite(Sprite sprite)
    {
        statusUISprite = sprite;
    }

    // <변경부분> 스테이터스 UI에 표시할 현재 기물 이미지를 반환하는 함수
    public Sprite GetStatusUISprite()
    {
        return statusUISprite;
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

    // <변경부분> 현재 기물 타입 아이콘 스프라이트를 최신 상태로 갱신한 뒤 반환하는 함수
    public Sprite GetCurrentTypeIconSprite()
    {
        // 타입 아이콘 렌더러가 없으면 null 반환
        if (typeIconRenderer == null)
        {
            return null;
        }

        // <변경부분> 스테이터스 UI에서 사용할 때도 현재 PieceType 기준으로 아이콘을 최신화
        UpdateTypeIconSprite();

        // 현재 기물이 실제로 사용 중인 타입 아이콘 스프라이트 반환
        return typeIconRenderer.sprite;
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

            case PieceType.Queen:
                // <변경부분> Queen 아이콘 적용
                typeIconRenderer.sprite = QueenIconSprite;
                break;

            case PieceType.Special:
                // Special 아이콘 적용
                typeIconRenderer.sprite = specialIconSprite;
                break;

        }
    }

    // <변경부분> 특정 일반스킬을 가지고 있는지 확인하는 함수
    public bool HasGeneralSkill(GeneralSkillType skillType)
    {
        foreach (OwnedGeneralSkillData skillData in generalSkills)
        {
            if (skillData.skillType == skillType)
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분> 특정 일반스킬의 레벨을 반환하는 함수
    public int GetGeneralSkillLevel(GeneralSkillType skillType)
    {
        foreach (OwnedGeneralSkillData skillData in generalSkills)
        {
            if (skillData.skillType == skillType)
            {
                return skillData.level;
            }
        }

        return 0;
    }

    // <변경부분> 현재 보유 중인 일반스킬 데이터를 복사해서 반환하는 함수
    // 흡수로 레벨이 오르기 전의 상태를 저장해야 할 때 사용
    public OwnedGeneralSkillData GetGeneralSkillDataCopy(GeneralSkillType skillType)
    {
        foreach (OwnedGeneralSkillData skillData in generalSkills)
        {
            if (skillData.skillType == skillType)
            {
                return new OwnedGeneralSkillData(skillData);
            }
        }

        return null;
    }

    // <변경부분> 현재 기물이 보유한 일반스킬 목록을 반환하는 함수
    public List<OwnedGeneralSkillData> GetGeneralSkills()
    {
        return generalSkills;
    }

    // <변경부분> 다른 기물이 가진 일반스킬을 흡수해서 획득하거나 성장시키는 함수
    public void AbsorbGeneralSkillsFrom(Piece targetPiece)
    {
        // 흡수 대상이 없으면 종료
        if (targetPiece == null)
        {
            return;
        }

        // 대상이 가진 일반스킬 목록을 하나씩 검사
        foreach (OwnedGeneralSkillData targetSkill in targetPiece.generalSkills)
        {
            // 대상의 일반스킬을 현재 기물에게 획득 또는 레벨업 처리
            AddOrLevelUpGeneralSkill(targetSkill.skillType);
        }
    }

    // <변경부분> 테스트용으로 일반스킬을 강제로 부여하는 함수
    public void SetTestGeneralSkill(GeneralSkillType skillType, int level)
    {
        // 일반스킬 없음은 저장하지 않음
        if (skillType == GeneralSkillType.None)
        {
            return;
        }

        // 레벨을 1~3 사이로 제한
        int clampedLevel = Mathf.Clamp(level, 1, MaxGeneralSkillLevel);

        // 이미 같은 스킬이 있으면 레벨만 갱신
        foreach (OwnedGeneralSkillData skillData in generalSkills)
        {
            if (skillData.skillType == skillType)
            {
                skillData.level = clampedLevel;
                return;
            }
        }

        // 같은 스킬이 없으면 새로 추가
        generalSkills.Add(new OwnedGeneralSkillData(skillType, clampedLevel));
    }

    // <변경부분> 일반스킬을 추가하거나 같은 스킬이 있으면 레벨업하는 함수
    public void AddOrLevelUpGeneralSkill(GeneralSkillType skillType)
    {
        // 일반스킬 없음은 저장하지 않음
        if (skillType == GeneralSkillType.None)
        {
            return;
        }

        // 이미 같은 일반스킬을 가지고 있는지 검사
        foreach (OwnedGeneralSkillData skillData in generalSkills)
        {
            if (skillData.skillType == skillType)
            {
                // 같은 스킬이 있으면 최대 레벨 안에서 레벨업
                skillData.level = Mathf.Min(skillData.level + 1, MaxGeneralSkillLevel);

                Debug.Log($"일반스킬 레벨업: {skillType} / LV.{skillData.level}");
                return;
            }
        }

        // 같은 스킬이 없으면 LV1로 새로 추가
        generalSkills.Add(new OwnedGeneralSkillData(skillType, 1));

        Debug.Log($"일반스킬 획득: {skillType} / LV.1");
    }

    // 마우스로 이 기물을 클릭했을 때 Unity가 자동 호출하는 함수
    /*private void OnMouseDown()
    {
        // BattleManager가 없으면 종료
        if (BattleManager.Instance == null)
        {
            return;
        }

        // <변경부분> 클릭한 기물 판단은 BattleManager에서 처리하도록 전달
        // 플레이어 기물 클릭: 플레이어 선택 UI 표시
        // 상대 기물 클릭: 상대 스테이터스 UI 표시
        // 선택된 플레이어 기물이 있는 상태에서 상대 기물 클릭: 공격 확인/실행 처리
        BattleManager.Instance.SelectPiece(this);
    }*/
}
