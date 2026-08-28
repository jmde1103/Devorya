using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Piece : MonoBehaviour
{
    public PieceType PieceType { get; private set; } // 현재 기물의 종류
    public PieceTeam Team { get; private set; } // 기물의 소속 진영

    // <변경부분> 이 기물이 어떤 PieceData를 기반으로 생성/변경되었는지 저장
    // 외형, 상태 UI 이미지, 타입 아이콘 위치를 PieceData 기준으로 갱신할 때 사용
    public PieceData CurrentPieceData { get; private set; }

    // <변경부분> 이 기물이 보유한 일반스킬 목록
    [SerializeField] private List<OwnedGeneralSkillData> generalSkills = new List<OwnedGeneralSkillData>();

    // <변경부분> 이 기물이 보유한 종족 태그 목록
    // 스킬 / 아이템 / 유물 효과 조건에서 공통으로 사용
    [SerializeField] private List<PieceSpeciesTag> speciesTags = new List<PieceSpeciesTag>();

    // <변경부분> 이 기물이 현재 보유 중인 상태이상 목록
    // 퇴화, 독, 기절 같은 전투 중 임시 효과를 관리
    [SerializeField] private List<OwnedStatusEffectData> statusEffects = new List<OwnedStatusEffectData>();

    [Header("Field Status Effect UI")]
    // <변경부분> 필드 위에 현재 상태효과 아이콘을 표시하는 UI 컴포넌트
    [SerializeField]
    private PieceFieldStatusEffectUI fieldStatusEffectUI;

    [Header("Skill Activation Icon")]
    // <변경부분> 일반스킬 또는 고유스킬 발동 시
    // 기물 위에 스킬 아이콘 연출을 재생하는 컴포넌트
    [SerializeField]
    private PieceSkillActivationIcon skillActivationIcon;

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

    // <변경부분> 타입 아이콘 뒤쪽 박스 이미지
    // TypeIconBox 오브젝트의 SpriteRenderer를 Inspector에서 연결한다.
    [SerializeField]
    private SpriteRenderer typeIconBoxRenderer;

    [Header("Type Icon Box Sprites")]
    // <변경부분> Player 기물에 사용할 전용 타입 아이콘 배경 박스.
    //
    // Enemy / Neutral은 기존 공용 Piece Prefab에 들어 있는
    // TypeIconBox Sprite를 그대로 사용한다.
    [SerializeField]
    private Sprite playerTypeIconBoxSprite;

    // <변경부분> 공용 Piece Prefab에 원래 설정되어 있던
    // 기본 TypeIconBox Sprite를 Awake에서 저장한다.
    //
    // Enemy / Neutral 또는 Player 전용 Sprite가 비어 있을 때
    // 안전하게 기존 배경으로 복원하기 위해 사용한다.
    private Sprite defaultTypeIconBoxSprite;

    [Header("Field Absorb Button")]
    // 공격 가능한 상대 기물을 선택했을 때
    // 타입 아이콘 위치에 표시할 필드 흡수 버튼
    [SerializeField]
    private FieldAbsorbButton fieldAbsorbButton;

    // 타입 아이콘이 사라질 때 사용할 페이드 시간
    [SerializeField, Min(0f)]
    private float fieldAbsorbTypeIconFadeDuration =
        0.12f;

    // BattleManager가 마지막으로 요청한
    // 타입 아이콘 표시 상태를 저장한다.
    private bool requestedTypeIconVisible;

    // 현재 필드 흡수 버튼이 표시 중인지 확인한다.
    private bool isFieldAbsorbButtonVisible;

    // 타입 아이콘과 필드 흡수 버튼 전환 코루틴
    private Coroutine fieldAbsorbVisualCoroutine;

    [Header("Selected Type Icon Visual")]
    // <변경부분> 선택된 기물의 타입 아이콘을
    // 기본 위치보다 위로 올리는 로컬 Y 거리
    [SerializeField]
    private float selectedTypeIconRaiseAmount =
    0.08f;

    // <변경부분> 아무 기물도 포커스되지 않은 일반 상태에서
    // 표시 중인 타입 아이콘과 배경 박스에 적용할 기본 알파값.
    //
    // 0.8 = 80% 불투명도
    [Range(0f, 1f)]
    [SerializeField]
    private float normalTypeIconAlpha =
        0.8f;

    // <변경부분> 다른 기물이 선택 / 확인 / AI 행동 중일 때
    // 포커스되지 않은 표시 중 타입 아이콘에 적용할 알파값.
    //
    // 기존 45% 값을 그대로 유지한다.
    [Range(0f, 1f)]
    [SerializeField]
    private float unselectedTypeIconAlpha =
        0.45f;

    // <변경부분> 선택 또는 선택 해제 시
    // 타입 아이콘이 목표 위치까지 부드럽게 이동하는 시간
    [SerializeField] private float selectedTypeIconMoveDuration = 0.16f;

    [Header("Selected Type Icon Sorting")]
    // <변경부분> 기물이 선택됐을 때
    // 타입 아이콘 박스와 아이콘의 기존 Order in Layer에 더할 값
    [SerializeField] private int selectedTypeIconSortingOrderOffset = 100;

    // <변경부분> TypeIconImage의 Inspector 기본 Order in Layer
    // 선택 해제 시 정확한 원래 값으로 복구하기 위해 Awake에서 저장한다.
    private int typeIconBaseSortingOrder;

    // <변경부분> TypeIconBox의 Inspector 기본 Order in Layer
    // 선택 해제 시 정확한 원래 값으로 복구하기 위해 Awake에서 저장한다.
    private int typeIconBoxBaseSortingOrder;

    // <변경부분> PieceData 적용 후 정해진
    // 타입 아이콘의 기본 로컬 위치
    private Vector3 typeIconBaseLocalPosition;

    // <변경부분> 타입 아이콘 이미지의 원래 색상
    private Color typeIconBaseColor = Color.white;

    // <변경부분> 타입 아이콘 박스의 원래 색상
    // 선택 해제 시 박스 고유 색상과 기본 알파값으로 복구할 때 사용한다.
    private Color typeIconBoxBaseColor = Color.white;

    // <변경부분> 현재 이 기물이 선택 상태인지 확인
    private bool isTypeIconSelected = false;

    // <변경부분> 현재 실행 중인 타입 아이콘 위치 이동 코루틴
    private Coroutine typeIconMoveCoroutine;

    // <변경부분> Player / Neutral에서 사용할 기존 회색 타입 아이콘 세트
    [Header("Player / Neutral Type Icon Sprites")]
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

    // <변경부분> Enemy에서 사용할 블랙 타입 아이콘 세트
    // 나중에는 PieceData에 포함시킬 예정이므로 현재는 Piece 프리팹에서 임시 관리
    [Header("Enemy Type Icon Sprites")]
    [SerializeField] private Sprite enemyPawnIconSprite;
    [SerializeField] private Sprite enemyRookIconSprite;
    [SerializeField] private Sprite enemyBishopIconSprite;
    [SerializeField] private Sprite enemyKnightIconSprite;
    [SerializeField] private Sprite enemyKingIconSprite;
    [SerializeField] private Sprite enemyQueenIconSprite;
    [SerializeField] private Sprite enemySpecialIconSprite;

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
        // SpriteRenderer를 한 번만 찾아 저장
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        // <변경부분> 타입 아이콘의 초기 위치와 색상을 저장한다.
        // PieceData에서 위치를 다시 적용하면
        // SetTypeIconLocalPosition()에서 기본 위치가 갱신된다.
        if (typeIconRoot != null)
        {
            typeIconBaseLocalPosition =
                typeIconRoot.transform.localPosition;

            typeIconRoot.SetActive(false);
        }

        if (typeIconRenderer != null)
        {
            // 타입 아이콘의 Inspector 기본 색상 저장
            typeIconBaseColor =
                typeIconRenderer.color;

            // <변경부분> 타입 아이콘의 Inspector 기본
            // Order in Layer를 선택 해제 복구용으로 저장한다.
            typeIconBaseSortingOrder =
                typeIconRenderer.sortingOrder;
        }

        // <변경부분> TypeIconBox의 Inspector 기본 Sprite,
        // 색상, Order in Layer를 별도로 저장한다.
        if (typeIconBoxRenderer != null)
        {
            // <변경부분> 공용 Piece Prefab에 원래 들어 있던
            // TypeIconBox Sprite를 Enemy / Neutral용 기본 배경으로 저장한다.
            defaultTypeIconBoxSprite =
                typeIconBoxRenderer.sprite;

            // 타입 아이콘 박스의 Inspector 기본 색상 저장
            //
            // Player / Enemy 배경 구분에는 Color를 사용하지 않고
            // Sprite 자체만 교체한다.
            typeIconBoxBaseColor =
                typeIconBoxRenderer.color;

            // 타입 아이콘 박스의 Inspector 기본
            // Order in Layer를 선택 해제 복구용으로 저장한다.
            typeIconBoxBaseSortingOrder =
                typeIconBoxRenderer.sortingOrder;
        }

        // <변경부분> 필드 상태효과 UI가 Inspector에 연결되지 않았다면
        // 비활성화된 자식 오브젝트까지 포함해 자동으로 찾는다.
        if (fieldStatusEffectUI == null)
        {
            fieldStatusEffectUI =
                GetComponentInChildren<PieceFieldStatusEffectUI>(
                    true
                );
        }

        // <변경부분> 스킬 발동 아이콘 연출이 Inspector에 연결되지 않았다면
        if (skillActivationIcon == null)
        {
            skillActivationIcon =
                GetComponentInChildren<PieceSkillActivationIcon>(
                    true
                );
        }

        // Inspector에서 연결되지 않았다면
        // 비활성화된 자식까지 포함해 필드 흡수 버튼을 찾는다.
        if (fieldAbsorbButton == null)
        {
            fieldAbsorbButton =
                GetComponentInChildren<FieldAbsorbButton>(
                    true
                );
        }

        // 부모 World Space Canvas는 항상 활성화 상태로 유지하고,
        // 실제 FieldAbsorbButtonRoot만 숨긴다.
        //
        // 부모와 자식을 동시에 활성화·비활성화할 때 발생하던
        // 첫 표시 활성화 순서 충돌을 방지한다.
        if (fieldAbsorbButton != null)
        {
            fieldAbsorbButton.Hide();
        }
    }

    private void OnDisable()
    {
        // 기물이 비활성화될 때 위치 이동 코루틴 정리
        StopTypeIconMove();

        // 재사용될 때 알파값이 남지 않도록
        // 타입 아이콘 색상을 기본 상태로 복구
        RestoreTypeIconColor();

        // <변경부분> 선택 상태의 높은 Order in Layer가
        // 비활성화 이후에도 남지 않도록 기본값으로 복구한다.
        RestoreTypeIconSortingOrder();

        // 기물이 제거되거나 비활성화될 때
        // 필드 흡수 버튼 전환 코루틴을 중단한다.
        StopFieldAbsorbVisualCoroutine();

        isFieldAbsorbButtonVisible =
            false;

        if (fieldAbsorbButton != null)
        {
            fieldAbsorbButton.Hide();
        }
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

        // 생성 시 전달받은 종족 태그를 초기화
        SetSpeciesTags(
            initialSpeciesTags
        );

        // <변경부분> 기물 생성 직후 현재 상태효과 기준으로
        // 필드 상태효과 아이콘 표시를 초기화한다.
        RefreshFieldStatusEffectUI();
    }

    // <변경부분> 현재 기물이 참조할 PieceData를 저장하는 함수
    // PieceManager가 SpawnPieceFromData / 흡수 / 승급 / 복제 후 외형 갱신 기준으로 사용한다.
    public void SetCurrentPieceData(PieceData pieceData)
    {
        CurrentPieceData = pieceData;
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

    // <변경부분> 튜토리얼 / 이벤트에서 생성한 기물의
    // 고유스킬만 별도로 변경할 때 사용하는 전용 진입점
    //
    // PieceData, PieceType, Team, 외형은 변경하지 않고
    // 현재 기물이 사용할 고유스킬만 교체한다.
    public void SetUniqueSkillForEvent(
        UniqueSkillType uniqueSkill)
    {
        UniqueSkill =
            uniqueSkill;

        // 이벤트에서 새로 지정한 고유스킬은
        // 즉시 정상적인 초기 상태로 사용할 수 있도록
        // 기존 쿨타임과 턴 사용 상태를 초기화한다.
        uniqueSkillCooldown =
            0;

        hasUsedUniqueSkillThisTurn =
            false;

        Debug.Log(
            $"이벤트 고유스킬 적용: " +
            $"{Team} {PieceType} / " +
            $"{UniqueSkill}"
        );
    }


    public Vector2Int GetGridPosition()
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

    // <변경부분> 특정 종족 태그를 제거하는 함수
    public void RemoveSpeciesTag(PieceSpeciesTag speciesTag)
    {
        if (speciesTag == PieceSpeciesTag.None)
        {
            return;
        }

        speciesTags.Remove(speciesTag);
    }

    // <변경부분> 상태이상을 추가하거나 이미 있으면 지속 턴과 중첩 수를 갱신하는 함수
    public void AddStatusEffect(StatusEffectData statusEffectData)
    {
        // 상태이상 데이터가 없으면 처리 불가
        if (statusEffectData == null)
        {
            return;
        }

        // None 상태이상은 추가하지 않음
        if (statusEffectData.effectType == StatusEffectType.None)
        {
            return;
        }

        // 최소 1턴은 유지되도록 보정
        int durationTurn = Mathf.Max(1, statusEffectData.durationTurn);

        // 최소 1중첩은 가능하도록 보정
        int maxStack = Mathf.Max(1, statusEffectData.maxStack);

        // 이미 같은 상태이상을 가지고 있는지 확인
        OwnedStatusEffectData existingStatusEffect = null;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i].effectType == statusEffectData.effectType)
            {
                existingStatusEffect = statusEffects[i];
                break;
            }
        }

        // 이미 있으면 지속 턴 갱신 + 중첩 증가
        if (existingStatusEffect != null)
        {
            existingStatusEffect.remainingTurn =
                durationTurn;

            existingStatusEffect.stackCount =
                Mathf.Min(
                    existingStatusEffect.stackCount + 1,
                    maxStack
                );

            // <변경부분> 상태효과 지속시간이나 중첩이 갱신됐으므로
            // 필드 위 아이콘과 경고 애니메이션도 즉시 갱신한다.
            RefreshFieldStatusEffectUI();

            Debug.Log(
                $"상태이상 갱신: " +
                $"{statusEffectData.effectName} / " +
                $"남은 턴 {existingStatusEffect.remainingTurn} / " +
                $"중첩 {existingStatusEffect.stackCount}"
            );

            return;
        }

        statusEffects.Add(
     new OwnedStatusEffectData(
         statusEffectData.effectType,
         durationTurn,
         1
     )
 );

        // <변경부분> 새 상태효과가 추가됐으므로
        // 필드 상태효과 아이콘을 즉시 갱신한다.
        RefreshFieldStatusEffectUI();

        Debug.Log(
            $"상태이상 추가: " +
            $"{statusEffectData.effectName} / " +
            $"남은 턴 {durationTurn}"
        );
    }

    // <변경부분> 특정 상태이상을 가지고 있는지 확인하는 함수
    public bool HasStatusEffect(StatusEffectType statusEffectType)
    {
        if (statusEffectType == StatusEffectType.None)
        {
            return false;
        }

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i].effectType == statusEffectType &&
                statusEffects[i].remainingTurn > 0 &&
                statusEffects[i].stackCount > 0)
            {
                return true;
            }
        }

        return false;
    }


    // <변경부분> 스테이터스 UI 표시용으로 현재 보유 중인 상태이상 목록 전체의 복사본을 반환하는 함수
    public List<OwnedStatusEffectData> GetStatusEffectsCopy()
    {
        // UI에서 원본 리스트를 직접 수정하지 못하도록 복사 리스트 생성
        List<OwnedStatusEffectData> copiedStatusEffects = new List<OwnedStatusEffectData>();

        // 현재 보유 중인 상태이상 목록 검사
        for (int i = 0; i < statusEffects.Count; i++)
        {
            OwnedStatusEffectData statusEffect = statusEffects[i];

            // 비어 있는 데이터는 제외
            if (statusEffect == null)
            {
                continue;
            }

            // 만료되었거나 중첩이 없는 상태이상은 UI에 표시하지 않음
            if (statusEffect.remainingTurn <= 0 || statusEffect.stackCount <= 0)
            {
                continue;
            }

            // 원본을 직접 넘기지 않고 복사본을 넘김
            copiedStatusEffects.Add(statusEffect.Clone());
        }

        return copiedStatusEffects;
    }

    // <변경부분> 현재 상태효과 목록을 기준으로
    // 필드 위 상태효과 아이콘 표시를 갱신한다.
    private void RefreshFieldStatusEffectUI()
    {
        if (fieldStatusEffectUI == null)
        {
            fieldStatusEffectUI =
                GetComponentInChildren<PieceFieldStatusEffectUI>(
                    true
                );
        }

        if (fieldStatusEffectUI == null)
        {
            return;
        }

        fieldStatusEffectUI.Refresh();
    }

    // <변경부분> 특정 상태이상의 보유 정보를 복사해서 반환하는 함수
    public OwnedStatusEffectData GetStatusEffectDataCopy(StatusEffectType statusEffectType)
    {
        if (statusEffectType == StatusEffectType.None)
        {
            return null;
        }

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i].effectType == statusEffectType)
            {
                return statusEffects[i].Clone();
            }
        }

        return null;
    }

    // <변경부분> 현재 기물의 상태효과 유지 턴을 1 감소시키고
    // 만료된 상태효과를 제거한 뒤 필드 UI를 갱신한다.
    public void ReduceStatusEffectTurnAndRemoveExpired()
    {
        for (int i = statusEffects.Count - 1;
             i >= 0;
             i--)
        {
            OwnedStatusEffectData statusEffect =
                statusEffects[i];

            if (statusEffect == null)
            {
                statusEffects.RemoveAt(i);
                continue;
            }

            statusEffect.remainingTurn--;

            if (statusEffect.remainingTurn <= 0)
            {
                Debug.Log(
                    $"상태이상 만료: " +
                    $"{statusEffect.effectType}"
                );

                statusEffects.RemoveAt(i);
            }
        }

        // <변경부분> 남은 턴 표시, 경고 애니메이션,
        // 만료로 제거된 슬롯 상태를 필드 UI에 반영한다.
        RefreshFieldStatusEffectUI();
    }

    // <변경부분> 현재 종족 태그 목록 복사본을 반환하는 함수
    // 복제 / 흡수 / 데이터 이전 시 사용
    public PieceSpeciesTag[] GetSpeciesTagsCopy()
    {
        return speciesTags.ToArray();
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

    // <변경부분> PieceData 기준으로 기물의 핵심 데이터를 변경하는 함수
    // 젤루 합성 승급, 아이템 변환, 추후 저장 데이터 복원에서 사용한다.
    public void ChangePieceData(PieceData newPieceData, bool isAbsorbedJelluVisual)
    {
        if (newPieceData == null)
        {
            Debug.LogWarning("ChangePieceData 실패: newPieceData가 null입니다.");
            return;
        }

        // <변경부분> 현재 기물이 참조할 PieceData 갱신
        CurrentPieceData = newPieceData;

        // <변경부분> PieceData의 타입/고유스킬을 현재 기물에 반영
        PieceType = newPieceData.pieceType;
        UniqueSkill = newPieceData.uniqueSkill;

        // <변경부분> 흡수 젤루 외형 여부 저장
        IsAbsorbedJelluVisual = isAbsorbedJelluVisual;

        // <변경부분> PieceData에 정의된 종족 태그로 갱신
        SetSpeciesTags(newPieceData.speciesTags);

        // <변경부분> 흡수 외형이면 젤루 태그 보장
        if (isAbsorbedJelluVisual)
        {
            AddSpeciesTag(PieceSpeciesTag.Jellu);
        }

        // <변경부분> 새 고유스킬은 다음 턴부터 자연스럽게 사용할 수 있도록 쿨타임 초기화
        uniqueSkillCooldown = 0;
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

        // <변경부분> 흡수 대상의 PieceData를 현재 기물에 복사
        // 이후 외형/상태 UI/타입 아이콘 위치는 이 데이터 기준으로 갱신된다.
        CurrentPieceData = targetPiece.CurrentPieceData;

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
    public void SetTypeIconVisible(
    bool isVisible)
    {
        // BattleManager가 요청한
        // 원래 타입 아이콘 표시 상태를 저장한다.
        requestedTypeIconVisible =
            isVisible;

        // 필드 흡수 상태가 아니라면
        // 실제 버튼 루트만 명시적으로 숨긴다.
        //
        // 부모 World Space Canvas는 활성화 상태를 유지한다.
        if (isFieldAbsorbButtonVisible == false &&
            fieldAbsorbButton != null)
        {
            fieldAbsorbButton.Hide();
        }

        if (typeIconRoot == null)
        {
            return;
        }

        // 필드 흡수 버튼이 표시 중이면
        // TypeIconRoot를 유지하여 기존 선택 상승 애니메이션을 공유한다.
        bool shouldKeepTypeIconRootActive =
            isVisible ||
            isFieldAbsorbButtonVisible;

        typeIconRoot.SetActive(
            shouldKeepTypeIconRootActive
        );

        // 필드 흡수 버튼이 표시 중이면
        // 타입 아이콘과 박스 SpriteRenderer만 숨긴다.
        bool shouldShowTypeIconRenderers =
            isVisible &&
            isFieldAbsorbButtonVisible == false;

        SetTypeIconRenderersEnabled(
            shouldShowTypeIconRenderers
        );

        if (shouldShowTypeIconRenderers)
        {
            UpdateTypeIconSprite();

            if (isTypeIconSelected)
            {
                ApplySelectedTypeIconSortingOrder();
                ApplySelectedTypeIconPosition();
            }
        }
    }

    // 공격 가능한 상대 기물을 선택했을 때
    // 타입 아이콘을 페이드아웃하고
    // 같은 TypeIconRoot 안의 흡수 버튼을 표시한다.
    public void ShowFieldAbsorbButton(
        System.Action<bool> onAbsorbModeChanged,
        BattleUIController battleUIController)
    {
        if (fieldAbsorbButton == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                "FieldAbsorbButton이 연결되지 않았습니다."
            );

            return;
        }

        StopFieldAbsorbVisualCoroutine();

        fieldAbsorbVisualCoroutine =
            StartCoroutine(
                ShowFieldAbsorbButtonRoutine(
                    onAbsorbModeChanged,
                    battleUIController
                )
            );
    }

    // 타입 아이콘 이미지와 박스만 사라지게 한 뒤
    // 필드 흡수 버튼을 OFF 상태로 표시한다.
    private IEnumerator ShowFieldAbsorbButtonRoutine(
        System.Action<bool> onAbsorbModeChanged,
        BattleUIController battleUIController)
    {
        isFieldAbsorbButtonVisible =
            true;

        // FieldAbsorbButton이 TypeIconRoot 아래에 있으므로
        // 부모 루트는 절대로 끄지 않는다.
        if (typeIconRoot != null)
        {
            typeIconRoot.SetActive(
                true
            );
        }

        // 현재 아이콘이 꺼져 있었다면
        // 페이드 시작을 위해 렌더러만 임시 활성화한다.
        SetTypeIconRenderersEnabled(
            true
        );

        float duration =
            Mathf.Max(
                0f,
                fieldAbsorbTypeIconFadeDuration
            );

        Color iconStartColor =
            typeIconRenderer != null
                ? typeIconRenderer.color
                : typeIconBaseColor;

        Color boxStartColor =
            typeIconBoxRenderer != null
                ? typeIconBoxRenderer.color
                : typeIconBoxBaseColor;

        float elapsedTime =
            0f;

        while (elapsedTime <
               duration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                duration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsedTime /
                        duration
                    );

            float easedTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            if (typeIconRenderer != null)
            {
                Color iconColor =
                    iconStartColor;

                iconColor.a =
                    Mathf.Lerp(
                        iconStartColor.a,
                        0f,
                        easedTime
                    );

                typeIconRenderer.color =
                    iconColor;
            }

            if (typeIconBoxRenderer != null)
            {
                Color boxColor =
                    boxStartColor;

                boxColor.a =
                    Mathf.Lerp(
                        boxStartColor.a,
                        0f,
                        easedTime
                    );

                typeIconBoxRenderer.color =
                    boxColor;
            }

            yield return null;
        }
        // 부모 TypeIconRoot는 유지하고
        // 타입 아이콘 SpriteRenderer 두 개만 숨긴다.
        SetTypeIconRenderersEnabled(
            false
        );

        // 부모 World Space Canvas는 항상 활성화 상태이므로
        // 실제 FieldAbsorbButtonRoot만 활성화한다.
        //
        // 부모·자식 활성화 순서 충돌 없이
        // 첫 번째 선택부터 오픈 노이즈 코루틴을 실행할 수 있다.
        fieldAbsorbButton.Show(
            onAbsorbModeChanged,
            battleUIController
        );

        fieldAbsorbVisualCoroutine =
            null;
    }

    // 다른 기물 또는 타일을 선택하면
    // 필드 흡수 버튼을 숨기고 타입 아이콘 상태를 복원한다.
    public void HideFieldAbsorbButton()
    {
        StopFieldAbsorbVisualCoroutine();

        isFieldAbsorbButtonVisible =
            false;

        // 부모 Canvas는 유지하고
        // 실제 필드 흡수 버튼 루트만 숨긴다.
        if (fieldAbsorbButton != null)
        {
            fieldAbsorbButton.Hide();
        }

        RestoreTypeIconColor();

        if (typeIconRoot == null)
        {
            return;
        }

        // 기존 BattleManager가 요청했던
        // 타입 아이콘 표시 상태로 복원한다.
        typeIconRoot.SetActive(
            requestedTypeIconVisible
        );

        SetTypeIconRenderersEnabled(
            requestedTypeIconVisible
        );

        if (requestedTypeIconVisible)
        {
            UpdateTypeIconSprite();

            if (isTypeIconSelected)
            {
                ApplySelectedTypeIconSortingOrder();
                ApplySelectedTypeIconPosition();
            }
        }
    }

    // TypeIconRoot는 유지한 채
    // 실제 아이콘과 박스 렌더러만 표시하거나 숨긴다.
    private void SetTypeIconRenderersEnabled(
        bool isEnabled)
    {
        if (typeIconRenderer != null)
        {
            typeIconRenderer.enabled =
                isEnabled;
        }

        if (typeIconBoxRenderer != null)
        {
            typeIconBoxRenderer.enabled =
                isEnabled;
        }
    }

    // 현재 실행 중인 타입 아이콘 페이드 전환을 중단한다.
    private void StopFieldAbsorbVisualCoroutine()
    {
        if (fieldAbsorbVisualCoroutine ==
            null)
        {
            return;
        }

        StopCoroutine(
            fieldAbsorbVisualCoroutine
        );

        fieldAbsorbVisualCoroutine =
            null;
    }

    // <변경부분> 현재 기물의 타입 아이콘에
    // 선택 위치와 Order in Layer 연출을 적용하거나 해제한다.
    public void SetTypeIconSelected(bool isSelected)
    {
        if (typeIconRoot == null)
        {
            return;
        }

        // 동일한 선택 상태가 다시 들어와도
        // 위치와 정렬 순서를 현재 상태에 맞게 다시 보정한다.
        if (isTypeIconSelected == isSelected)
        {
            if (isSelected)
            {
                // <변경부분> 선택 상태라면
                // 아이콘과 박스를 다른 기물보다 앞으로 올린다.
                ApplySelectedTypeIconSortingOrder();

                if (typeIconRoot.activeSelf)
                {
                    ApplySelectedTypeIconPosition();
                }
            }
            else
            {
                // <변경부분> 선택 해제 상태라면
                // Inspector에 설정된 기본 정렬 순서로 복구한다.
                RestoreTypeIconSortingOrder();
            }

            return;
        }

        isTypeIconSelected =
            isSelected;

        if (isTypeIconSelected)
        {
            // <변경부분> 선택된 아이콘과 박스의
            // Order in Layer를 기본값보다 높여 앞으로 표시한다.
            ApplySelectedTypeIconSortingOrder();

            // 선택된 아이콘을 기본 위치보다
            // 살짝 위쪽으로 부드럽게 이동한다.
            ApplySelectedTypeIconPosition();

            return;
        }

        // <변경부분> 선택 해제 시 아이콘과 박스를
        // Inspector에 설정된 기본 Order in Layer로 복구한다.
        RestoreTypeIconSortingOrder();

        // 선택이 해제되면 기본 위치까지
        // 부드럽게 내려온다.
        ResetTypeIconSelectionVisual();
    }

    // <변경부분> 선택된 타입 아이콘과 박스를
    // 기본 선택 우선순위 1로 다른 일반 기물보다 앞에 표시한다.
    private void ApplySelectedTypeIconSortingOrder()
    {
        SetTypeIconSortingPriority(
            1
        );
    }

    // <변경부분> 타입 아이콘의 정렬 우선순위를 외부에서 지정한다.
    //
    // 우선순위 0: 기본 정렬 순서
    // 우선순위 1: 선택된 기물 또는 공격 대상
    // 우선순위 2: 공격하는 기물
    public void SetTypeIconSortingPriority(
        int sortingPriority)
    {
        // 음수 우선순위가 들어오지 않도록 0 이상으로 보정한다.
        int clampedPriority =
            Mathf.Max(
                0,
                sortingPriority
            );

        // Inspector에 설정된 선택 정렬 증가값을 기준으로
        // 우선순위 단계마다 Order in Layer를 추가한다.
        int sortingOrderOffset =
            Mathf.Max(
                0,
                selectedTypeIconSortingOrderOffset
            ) *
            clampedPriority;

        // 타입 아이콘 박스 정렬 순서 적용
        if (typeIconBoxRenderer != null)
        {
            typeIconBoxRenderer.sortingOrder =
                typeIconBoxBaseSortingOrder +
                sortingOrderOffset;
        }

        // 타입 아이콘 이미지 정렬 순서 적용
        if (typeIconRenderer != null)
        {
            typeIconRenderer.sortingOrder =
                typeIconBaseSortingOrder +
                sortingOrderOffset;
        }
    }

    // <변경부분> 타입 아이콘과 박스의 Order in Layer를
    // Awake에서 저장한 Inspector 기본값으로 복구한다.
    private void RestoreTypeIconSortingOrder()
    {
        // 타입 아이콘 박스 기본 정렬 순서 복구
        if (typeIconBoxRenderer != null)
        {
            typeIconBoxRenderer.sortingOrder =
                typeIconBoxBaseSortingOrder;
        }

        // 타입 아이콘 기본 정렬 순서 복구
        if (typeIconRenderer != null)
        {
            typeIconRenderer.sortingOrder =
                typeIconBaseSortingOrder;
        }
    }

    // <변경부분> 선택된 타입 아이콘을
    // 기본 로컬 위치보다 위쪽 목표 위치까지 부드럽게 이동시킨다.
    private void ApplySelectedTypeIconPosition()
    {
        if (typeIconRoot == null)
        {
            return;
        }

        Vector3 selectedLocalPosition =
            typeIconBaseLocalPosition +
            Vector3.up *
            selectedTypeIconRaiseAmount;

        StartTypeIconMove(
            selectedLocalPosition
        );
    }
    // <변경부분> 타입 아이콘 위치 이동 코루틴을 시작한다.
    // 기존 이동 코루틴이 실행 중이면 중단하고
    // 현재 위치에서 새 목표 위치로 다시 이동한다.
    private void StartTypeIconMove(
        Vector3 targetLocalPosition)
    {
        if (typeIconRoot == null)
        {
            return;
        }

        StopTypeIconMove();

        typeIconMoveCoroutine =
            StartCoroutine(
                TypeIconMoveRoutine(
                    targetLocalPosition
                )
            );
    }

    // <변경부분> 현재 실행 중인 타입 아이콘 위치 이동 코루틴을 중단한다.
    private void StopTypeIconMove()
    {
        if (typeIconMoveCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            typeIconMoveCoroutine
        );

        typeIconMoveCoroutine = null;
    }

    // <변경부분> 타입 아이콘을 현재 위치에서 목표 위치까지
    // SmoothStep을 사용해 부드럽게 이동시키는 코루틴
    private IEnumerator TypeIconMoveRoutine(
        Vector3 targetLocalPosition)
    {
        if (typeIconRoot == null)
        {
            typeIconMoveCoroutine = null;
            yield break;
        }

        Vector3 startLocalPosition =
            typeIconRoot.transform.localPosition;

        float moveDuration =
            Mathf.Max(
                0f,
                selectedTypeIconMoveDuration
            );

        // 이동 시간이 0이면 즉시 목표 위치로 이동
        if (moveDuration <= 0f)
        {
            typeIconRoot.transform.localPosition =
                targetLocalPosition;

            typeIconMoveCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            if (typeIconRoot == null)
            {
                typeIconMoveCoroutine = null;
                yield break;
            }

            elapsedTime +=
                Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    moveDuration
                );

            float easedTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            typeIconRoot.transform.localPosition =
                Vector3.Lerp(
                    startLocalPosition,
                    targetLocalPosition,
                    easedTime
                );

            yield return null;
        }

        if (typeIconRoot != null)
        {
            typeIconRoot.transform.localPosition =
                targetLocalPosition;
        }

        typeIconMoveCoroutine = null;
    }

    // <변경부분> 현재 타입 아이콘의 포커스 상태에 따라
    // 기본 / 선택 / 비선택 알파값을 적용한다.
    public void SetTypeIconDimmed(
        bool isDimmed)
    {
        // 다른 기물이 포커스된 상태에서
        // 현재 기물이 비선택 상태라면 기존 45% 알파를 사용한다.
        if (isDimmed)
        {
            SetTypeIconAlpha(
                unselectedTypeIconAlpha
            );

            return;
        }

        // <변경부분> 선택된 Player 기물,
        // 확인 중인 상대 기물,
        // 현재 행동 중인 Enemy AI 기물은
        // 완전 불투명 상태로 강조한다.
        if (isTypeIconSelected)
        {
            SetTypeIconAlpha(
                1f
            );

            return;
        }

        // <변경부분> 아무 포커스도 받지 않은 일반 표시 상태는
        // 기본 80% 알파를 사용한다.
        SetTypeIconAlpha(
            normalTypeIconAlpha
        );
    }

    // <변경부분> 타입 아이콘 이미지와 TypeIconBox의
    // RGB 색상은 유지하고 알파값만 동일하게 변경한다.
    private void SetTypeIconAlpha(float alpha)
    {
        float clampedAlpha =
            Mathf.Clamp01(alpha);

        // 타입 아이콘 이미지 알파값 적용
        if (typeIconRenderer != null)
        {
            Color iconColor =
                typeIconBaseColor;

            iconColor.a =
                clampedAlpha;

            typeIconRenderer.color =
                iconColor;
        }

        // TypeIconBox 알파값 적용
        if (typeIconBoxRenderer != null)
        {
            Color boxColor =
                typeIconBoxBaseColor;

            boxColor.a =
                clampedAlpha;

            typeIconBoxRenderer.color =
                boxColor;
        }
    }

    // <변경부분> 타입 아이콘 이미지와 TypeIconBox를
    // 각각 Inspector에 설정돼 있던 원래 색상으로 복구한다.
    private void RestoreTypeIconColor()
    {
        if (typeIconRenderer != null)
        {
            typeIconRenderer.color =
                typeIconBaseColor;
        }

        if (typeIconBoxRenderer != null)
        {
            typeIconBoxRenderer.color =
                typeIconBoxBaseColor;
        }
    }

    // <변경부분> 타입 아이콘 선택 연출을 해제한다.
    // 점멸은 즉시 중단하고 위치는 기본 위치까지 부드럽게 내려온다.
    private void ResetTypeIconSelectionVisual()
    {
        if (typeIconRoot == null)
        {
            return;
        }

        // 선택이 해제되면 타입 아이콘을
        // 기본 로컬 위치까지 부드럽게 내려보낸다.
        StartTypeIconMove(
            typeIconBaseLocalPosition
        );
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
    public void SetTypeIconLocalPosition(
     Vector3 localPosition)
    {
        if (typeIconRoot == null)
        {
            return;
        }

        // PieceData에서 전달된 위치를
        // 선택 연출 전 기본 위치로 저장한다.
        typeIconBaseLocalPosition =
            localPosition;

        // 선택 중이라면 새 기본 위치를 기준으로
        // 상승 위치까지 부드럽게 이동한다.
        if (isTypeIconSelected)
        {
            ApplySelectedTypeIconPosition();
            return;
        }

        // 선택 중이 아니라면 기존 이동 코루틴을 정리하고
        // 새 기본 위치를 즉시 적용한다.
        StopTypeIconMove();

        typeIconRoot.transform.localPosition =
            typeIconBaseLocalPosition;
    }

    // <변경부분> PieceData에 설정된 기물별
    // 필드 상태효과 아이콘 위치를 적용한다.
    // 타입 아이콘 위치 적용 방식과 동일하게 동작한다.
    public void SetFieldStatusEffectLocalPosition(
        Vector3 localPosition)
    {
        // Inspector 연결이 비어 있으면
        // 비활성화된 자식 오브젝트까지 포함해 자동으로 찾는다.
        if (fieldStatusEffectUI == null)
        {
            fieldStatusEffectUI =
                GetComponentInChildren<PieceFieldStatusEffectUI>(
                    true
                );
        }

        if (fieldStatusEffectUI == null)
        {
            return;
        }

        fieldStatusEffectUI.SetLocalPosition(
            localPosition
        );
    }



    // <변경부분> 현재 기물 타입과 소속 진영에 맞는
    // 타입 아이콘 및 배경 박스 Sprite를 최신 상태로 갱신한다.
    private void UpdateTypeIconSprite()
    {
        // <변경부분> 내부 기물 아이콘과 별개로
        // Team 기준 TypeIconBox Sprite부터 갱신한다.
        UpdateTypeIconBoxSprite();

        if (typeIconRenderer == null)
        {
            return;
        }

        // 현재 기물 소속과 타입에 맞는
        // 내부 기물 타입 아이콘을 가져온다.
        Sprite iconSprite =
            GetTypeIconSpriteByTeamAndType();

        if (iconSprite != null)
        {
            typeIconRenderer.sprite =
                iconSprite;
        }
    }

    // <변경부분> 기물 Team에 따라
    // TypeIconBox의 배경 Sprite만 교체한다.
    //
    // Player:
    // 새로 연결한 검정 배경 박스 사용
    //
    // Enemy / Neutral:
    // 공용 Piece Prefab의 기존 배경 박스 사용
    private void UpdateTypeIconBoxSprite()
    {
        if (typeIconBoxRenderer == null)
        {
            return;
        }

        // Player는 전용 검정 배경 Sprite를 사용한다.
        if (Team == PieceTeam.Player &&
            playerTypeIconBoxSprite != null)
        {
            typeIconBoxRenderer.sprite =
                playerTypeIconBoxSprite;

            return;
        }

        // Enemy / Neutral은
        // Prefab에 원래 설정되어 있던 기존 박스를 사용한다.
        //
        // Player 전용 Sprite 연결이 빠진 경우에도
        // 기존 박스로 안전하게 대체된다.
        if (defaultTypeIconBoxSprite != null)
        {
            typeIconBoxRenderer.sprite =
                defaultTypeIconBoxSprite;
        }
    }

    // <변경부분> Player는 회색 아이콘, Enemy는 블랙 아이콘, Neutral은 회색 아이콘을 반환
    private Sprite GetTypeIconSpriteByTeamAndType()
    {
        // Enemy 기물은 블랙 타입 아이콘 세트 사용
        if (Team == PieceTeam.Enemy)
        {
            return GetEnemyTypeIconSprite(PieceType);
        }

        // Player / Neutral 기물은 기존 회색 타입 아이콘 세트 사용
        return GetDefaultTypeIconSprite(PieceType);
    }

    // <변경부분> 기존 회색 타입 아이콘 반환
    private Sprite GetDefaultTypeIconSprite(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return pawnIconSprite;

            case PieceType.Rook:
                return rookIconSprite;

            case PieceType.Bishop:
                return bishopIconSprite;

            case PieceType.Knight:
                return knightIconSprite;

            case PieceType.King:
                return kingIconSprite;

            case PieceType.Queen:
                return QueenIconSprite;

            case PieceType.Special:
                return specialIconSprite;
        }

        return null;
    }

    // <변경부분> Enemy 전용 블랙 타입 아이콘 반환
    // 혹시 Inspector에 블랙 아이콘이 비어 있으면 기존 회색 아이콘으로 대체
    private Sprite GetEnemyTypeIconSprite(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return enemyPawnIconSprite != null ? enemyPawnIconSprite : pawnIconSprite;

            case PieceType.Rook:
                return enemyRookIconSprite != null ? enemyRookIconSprite : rookIconSprite;

            case PieceType.Bishop:
                return enemyBishopIconSprite != null ? enemyBishopIconSprite : bishopIconSprite;

            case PieceType.Knight:
                return enemyKnightIconSprite != null ? enemyKnightIconSprite : knightIconSprite;

            case PieceType.King:
                return enemyKingIconSprite != null ? enemyKingIconSprite : kingIconSprite;

            case PieceType.Queen:
                return enemyQueenIconSprite != null ? enemyQueenIconSprite : QueenIconSprite;

            case PieceType.Special:
                return enemySpecialIconSprite != null ? enemySpecialIconSprite : specialIconSprite;
        }

        return null;
    }

    // <변경부분> 일반스킬 또는 고유스킬 발동 시
    // 데이터에서 받은 아이콘을 기물 위 연출 컴포넌트에 전달한다.
    public void PlaySkillActivationIcon(
        Sprite skillIcon)
    {
        if (skillIcon == null)
        {
            return;
        }

        // Inspector 연결이 빠졌다면
        // 비활성화된 자식까지 포함해 다시 자동 탐색한다.
        if (skillActivationIcon == null)
        {
            skillActivationIcon =
                GetComponentInChildren<PieceSkillActivationIcon>(
                    true
                );
        }

        if (skillActivationIcon == null)
        {
            Debug.LogWarning(
                $"스킬 발동 아이콘 연출 실패: " +
                $"{Team} {PieceType}에 " +
                $"PieceSkillActivationIcon이 없습니다."
            );

            return;
        }

        skillActivationIcon.Play(
            skillIcon
        );
    }

    // <변경부분> 스킬 아이콘의 확대 등장과
    // 기본 크기 복귀가 끝날 때까지 기다린다.
    //
    // 이 코루틴이 끝난 뒤 실제 스킬 효과를 실행하면
    // 아이콘이 먼저 뜨고 스킬이 발동하는 순서를 만들 수 있다.
    public IEnumerator PlaySkillActivationIconBeforeEffectRoutine(
        Sprite skillIcon)
    {
        if (skillIcon == null)
        {
            yield break;
        }

        // Inspector 연결이 비어 있다면
        // 비활성화된 자식 오브젝트까지 포함해 다시 찾는다.
        if (skillActivationIcon == null)
        {
            skillActivationIcon =
                GetComponentInChildren<PieceSkillActivationIcon>(
                    true
                );
        }

        if (skillActivationIcon == null)
        {
            Debug.LogWarning(
                $"스킬 선행 아이콘 연출 실패: " +
                $"{Team} {PieceType}에 " +
                $"PieceSkillActivationIcon이 없습니다."
            );

            yield break;
        }

        // 아이콘의 선행 확대 연출이 끝날 때까지 대기
        yield return
            skillActivationIcon
                .PlayBeforeEffectRoutine(
                    skillIcon
                );
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

    // <변경부분> 현재 기물이 보유한 일반스킬 목록을 모두 제거하는 함수
    // 런 저장 데이터 복원 시 PieceData 기본 일반스킬을 덮어쓰기 위해 사용
    public void ClearGeneralSkills()
    {
        generalSkills.Clear();
    }


    // <변경부분> 런 저장 데이터에 기록된 일반스킬 목록을
    // 레벨 없이 현재 기물에 복원한다.
    public void ApplyGeneralSkillRuntimeData(
        List<GeneralSkillRuntimeData> runtimeGeneralSkills)
    {
        // PieceData 기본 일반스킬을 제거하고
        // 저장된 실제 보유 상태로 다시 구성한다.
        ClearGeneralSkills();

        if (runtimeGeneralSkills == null)
        {
            return;
        }

        for (int i = 0;
             i < runtimeGeneralSkills.Count;
             i++)
        {
            GeneralSkillRuntimeData runtimeSkill =
                runtimeGeneralSkills[i];

            if (runtimeSkill == null)
            {
                continue;
            }

            if (runtimeSkill.skillType ==
                GeneralSkillType.None)
            {
                continue;
            }

            // <변경부분> 동일 스킬이 저장 데이터에 여러 번 있어도
            // AddGeneralSkill 내부에서 중복을 차단한다.
            AddGeneralSkill(
                runtimeSkill.skillType
            );
        }
    }

    // <변경부분> 대상 기물이 가진 일반스킬 중
    // 현재 기물에 없는 새로운 스킬만 흡수한다.
    //
    // 이미 같은 일반스킬을 보유 중이면
    // 복사하거나 강화하지 않고 그대로 무시한다.
    public void AbsorbGeneralSkillsFrom(
        Piece targetPiece)
    {
        if (targetPiece == null)
        {
            return;
        }

        foreach (OwnedGeneralSkillData targetSkill in
                 targetPiece.generalSkills)
        {
            if (targetSkill == null)
            {
                continue;
            }

            if (targetSkill.skillType ==
                GeneralSkillType.None)
            {
                continue;
            }

            AddGeneralSkill(
                targetSkill.skillType
            );
        }
    }

    // <변경부분> 일반스킬을 중복 없이 추가한다.
    //
    // 이미 같은 스킬을 가지고 있으면
    // 아무 변화 없이 false를 반환한다.
    //
    // 새로운 스킬을 추가한 경우에만 true를 반환한다.
    public bool AddGeneralSkill(
        GeneralSkillType skillType)
    {
        if (skillType ==
            GeneralSkillType.None)
        {
            return false;
        }

        // 동일 스킬 중복 보유 차단
        if (HasGeneralSkill(skillType))
        {
            Debug.Log(
                $"일반스킬 중복 무시: {skillType}"
            );

            return false;
        }

        generalSkills.Add(
            new OwnedGeneralSkillData(
                skillType
            )
        );

        Debug.Log(
            $"일반스킬 획득: {skillType}"
        );

        return true;
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
