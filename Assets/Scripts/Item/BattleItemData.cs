using UnityEngine;

// <변경부분> 전투 중 사용하는 소모성 아이템 하나의 기본 데이터를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "BattleItemData", menuName = "Devorya/Battle/Item Data")]
public class BattleItemData : ScriptableObject
{
    [Header("Basic")]
    // 아이템 종류
    public BattleItemType itemType = BattleItemType.None;

    // 인스펙터와 로그에서 확인할 아이템 이름
    public string itemName;

    // 아이템 슬롯에 표시할 아이콘 이미지
    public Sprite iconSprite;

    // <변경부분> 아이템 설명
    [TextArea]
    public string description;

    [Header("Change Piece Effect")]
    // <변경부분> 아이템 사용 시 변경할 기물 타입
    public PieceType changeTargetPieceType = PieceType.Pawn;

    // <변경부분> 아이템 사용 시 부여할 고유스킬
    public UniqueSkillType changeTargetUniqueSkill = UniqueSkillType.None;

    // <변경부분> 아이템 사용 시 부여할 일반스킬
    public GeneralSkillType changeTargetGeneralSkill = GeneralSkillType.None;

    // <변경부분> 아이템 사용 시 부여할 일반스킬 레벨
    public int changeTargetGeneralSkillLevel = 1;

    // <변경부분> 아이템 사용 후 흡수된 젤루 외형으로 표시할지 여부
    public bool useAbsorbedJelluVisual = true;

    // <변경부분> King 타입 기물에게 사용을 금지할지 여부
    public bool blockUseOnKing = true;

    // <변경부분> 플레이어 기물에게만 사용 가능한지 여부
    public bool onlyPlayerPiece = true;
}