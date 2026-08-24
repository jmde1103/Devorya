using System.Collections.Generic;
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
    // <변경부분> 아이템 설명 팝업 하단에 추가로 붙일 설명 블록 목록
    // 이름, 설명, 아이콘은 기존 itemName / description / iconSprite를 그대로 사용한다.
    public List<TooltipSectionData> tooltipSections = new List<TooltipSectionData>();

    [Header("Change Piece Effect")]
    // <변경부분> 아이템 사용 시 변경할 기물 데이터
    // PieceManager.RefreshPieceVisual()이 CurrentPieceData 기준으로 외형을 갱신하므로 반드시 연결해야 한다.
    public PieceData changeTargetPieceData;

    // <변경부분> 아이템 사용 시 변경할 기물 타입
    // changeTargetPieceData가 비어 있을 때만 사용하는 구버전 보조값이다.
    public PieceType changeTargetPieceType = PieceType.Pawn;

    // <변경부분> 아이템 사용 시 부여할 고유스킬
    // changeTargetPieceData가 비어 있을 때만 사용하는 구버전 보조값이다.
    public UniqueSkillType changeTargetUniqueSkill = UniqueSkillType.None;

    // 아이템 사용 시 부여할 일반스킬.
    //
    // 현재 일반스킬은 레벨 시스템을 사용하지 않으며,
    // 동일 스킬의 중복 보유도 허용하지 않는다.
    public GeneralSkillType changeTargetGeneralSkill =
        GeneralSkillType.None;

    // 아이템 사용 후 흡수된 젤루 외형으로 표시할지 여부
    public bool useAbsorbedJelluVisual = true;

    // <변경부분> King 타입 기물에게 사용을 금지할지 여부
    public bool blockUseOnKing = true;

    // <변경부분> 플레이어 기물에게만 사용 가능한지 여부
    public bool onlyPlayerPiece = true;

    [Header("Status Effect")]
    // <변경부분> 상태효과 부여 아이템이
    // 선택한 기물에 적용할 StatusEffectData
    public StatusEffectData applyStatusEffectData;
}