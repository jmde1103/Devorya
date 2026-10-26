using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

// <변경부분> 고유스킬 하나의 기본 설정 데이터를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "\ub2a5\ub825_\uc0c8 \ub2a5\ub825", menuName = "Devorya/\ub2a5\ub825/\ub2a5\ub825 \ub370\uc774\ud130")]
public class UniqueSkillData : ScriptableObject
{
    [Header("Basic")]
    // 고유스킬 종류
    public UniqueSkillType skillType = UniqueSkillType.None;

    // 인스펙터와 UI에 표시할 고유스킬 이름
    public string skillName;

    // 고유스킬 아이콘
    public Sprite iconSprite;

    [TextArea]
    public string description;

    // <변경부분> 고유스킬 설명 팝업 하단에 추가로 붙일 설명 블록 목록
    //
    // Tooltip Section 자체의 Localization은
    // 기본 고유스킬 이름/설명 Localization 검증 후 별도 단계에서 처리한다.
    public List<TooltipSectionData> tooltipSections =
        new List<TooltipSectionData>();

    [Header("Localization")]

    // <변경부분> 현재 Locale에 맞는 고유스킬 이름.
    //
    // Localization이 연결되지 않았거나 번역 값이 비어 있으면
    // 기존 skillName을 한국어 fallback으로 사용한다.
    public LocalizedString localizedSkillName =
        new LocalizedString();

    // <변경부분> 현재 Locale에 맞는 고유스킬 설명.
    //
    // 기존 description은 삭제하지 않고
    // 한국어 원문 + fallback 데이터로 계속 유지한다.
    public LocalizedString localizedDescription =
        new LocalizedString();

    // <변경부분> 고유스킬 내부 조건 불충족 시 표시되는
    // 스킬별 실패 메시지 Localization.
    //
    // 기존 conditionFailMessage를 한국어 fallback으로 유지한다.
    public LocalizedString localizedConditionFailMessage =
        new LocalizedString();

    [Header("능력 연출")]
    [InspectorName("대상 파티클 프리팹 (바이러스)")] public PixelBurstEffect targetPixelBurstPrefab;

    [Header("Balance")]
    // <변경부분> 능력의 기본 에너지 코스트.
    // 현재는 플레이어가 능력을 사용할 때만 에너지를 소비한다.
    // 아이템/유물/이벤트에 의한 변경은 BattleManager가 별도로 계산한다.
    [Min(0)]
    public int energyCost = 1;

    // <변경부분> 고유스킬 사용 후 적용할 쿨타임 턴 수
    public int cooldownTurn = 1;

    // <변경부분> 이 스킬을 사용하기 위해 필요한 자기 진영 사망 스택 수
    public int requiredDeathStack = 0;

    // <변경부분> 스킬 사용 성공 시 requiredDeathStack만큼 스택을 소모할지 여부
    public bool consumeDeathStackOnUse = false;

    // <변경부분> 한 턴에 한 번만 사용할 수 있는 스킬인지 여부
    public bool oncePerTurn = true;

    [Header("Failure Message")]
    // <변경부분> 스킬 내부 조건이 맞지 않아 발동하지 못했을 때 표시할 기본 실패 문구
    [TextArea]
    public string conditionFailMessage =
      "조건이 맞지 않아 사용할 수 없습니다.";

    // <변경부분> 현재 Locale 기준 고유스킬 이름을 반환한다.
    //
    // Localization이 없는 기존 UniqueSkillData도
    // 기존 skillName을 그대로 표시할 수 있도록 fallback한다.
    public string GetLocalizedSkillName()
    {
        return GetLocalizedTextOrFallback(
            localizedSkillName,
            skillName
        );
    }

    // <변경부분> 현재 Locale 기준 고유스킬 설명을 반환한다.
    public string GetLocalizedDescription()
    {
        return GetLocalizedTextOrFallback(
            localizedDescription,
            description
        );
    }

    // <변경부분> 현재 Locale 기준 고유스킬 조건 실패 메시지를 반환한다.
    //
    // 번역이 없으면 기존 conditionFailMessage를 그대로 사용한다.
    public string GetLocalizedConditionFailMessage()
    {
        return GetLocalizedTextOrFallback(
            localizedConditionFailMessage,
            conditionFailMessage
        );
    }

    // <변경부분> LocalizedString에서 현재 Locale 문자열을 가져오고,
    // 사용할 수 없는 경우 기존 한국어 원문으로 안전하게 fallback한다.
    private string GetLocalizedTextOrFallback(
        LocalizedString localizedString,
        string fallbackText)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return fallbackText ?? string.Empty;
        }

        string localizedText =
            localizedString.GetLocalizedString();

        if (string.IsNullOrWhiteSpace(
                localizedText))
        {
            return fallbackText ?? string.Empty;
        }

        return localizedText;
    }
}
