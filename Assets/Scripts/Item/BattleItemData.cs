using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

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

    // <변경부분> 아이템 설명.
    //
    // 기존 한국어 원문 데이터는 그대로 유지한다.
    // Localization이 연결되지 않았거나 번역 결과가 비어 있을 경우
    // 이 값을 fallback으로 사용한다.
    [TextArea]
    public string description;

    [Header("Localization")]

    // <변경부분> 플레이어에게 표시할 아이템 이름 Localization 참조.
    //
    // 기존 itemName은 삭제하지 않는다.
    // 이 값이 비어 있으면 기존 itemName을 그대로 사용한다.
    public LocalizedString localizedItemName =
        new LocalizedString();

    // <변경부분> 플레이어에게 표시할 아이템 설명 Localization 참조.
    //
    // 기존 description은 삭제하지 않는다.
    // 이 값이 비어 있으면 기존 description을 그대로 사용한다.
    public LocalizedString localizedDescription =
        new LocalizedString();

    // <변경부분> 아이템 설명 팝업 하단에 추가로 붙일 설명 블록 목록.
    //
    // Tooltip Section 다국어화는 기본 이름/설명 검증 후
    // 다음 단계에서 별도로 확장한다.
    public List<TooltipSectionData> tooltipSections =
        new List<TooltipSectionData>();

    // <변경부분> 현재 선택된 Locale 기준으로
    // 실제 UI에 표시할 아이템 이름을 반환한다.
    //
    // Localization 참조가 아직 연결되지 않은 기존 데이터에서는
    // 기존 itemName을 그대로 사용하므로 이전 데이터와 호환된다.
    public string GetLocalizedItemName()
    {
        return GetLocalizedTextOrFallback(
            localizedItemName,
            itemName
        );
    }

    // <변경부분> 현재 선택된 Locale 기준으로
    // 실제 UI에 표시할 아이템 설명을 반환한다.
    //
    // Localization 참조가 아직 연결되지 않은 기존 데이터에서는
    // 기존 description을 그대로 사용한다.
    public string GetLocalizedDescription()
    {
        return GetLocalizedTextOrFallback(
            localizedDescription,
            description
        );
    }

    // <변경부분> LocalizedString이 실제로 연결되어 있다면
    // 현재 Locale의 문자열을 반환하고,
    // 사용할 번역이 없다면 기존 한국어 원문을 fallback으로 사용한다.
    private string GetLocalizedTextOrFallback(
        LocalizedString localizedString,
        string fallbackText)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return fallbackText;
        }

        string localizedText =
            localizedString.GetLocalizedString();

        if (string.IsNullOrWhiteSpace(
                localizedText))
        {
            return fallbackText;
        }

        return localizedText;
    }

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