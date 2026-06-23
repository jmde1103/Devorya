using UnityEngine;

// <변경부분> 전투 중 보유하면 지속 효과를 제공하는 유물 하나의 기본 데이터를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "BattleRelicData", menuName = "Devorya/Battle/Relic Data")]
public class BattleRelicData : ScriptableObject
{
    [Header("Basic")]
    // 유물 종류
    public BattleRelicType relicType = BattleRelicType.None;

    // 인스펙터와 로그에서 확인할 유물 이름
    public string relicName;

    // 유물 슬롯에 표시할 아이콘 이미지
    public Sprite iconSprite;

    // 유물 효과 설명
    [TextArea]
    public string description;

    // <변경부분> 유물 아이콘을 꾹 눌렀을 때 표시할 Tooltip 데이터
    public TooltipData tooltipData;

    [Header("Absorb Chance Attack Effect")]
    // <변경부분> 유물 효과가 플레이어 턴에만 발동 가능한지 여부
    public bool onlyPlayerTurn = true;

    // <변경부분> 한 플레이어 턴에 1번만 발동 가능한지 여부
    public bool oncePerTurn = true;

    // <변경부분> 추가 행동 가능한 이동/공격 타일이 있어야만 발동할지 여부
    public bool requireSelectableTile = true;

    // <변경부분> 유물 발동 확률
    // 100이면 확정 발동, 50이면 50% 확률
    public float triggerChancePercent = 100f;

    // <변경부분> 유물 발동 시 부여할 추가 행동 횟수
    // 현재 전투 구조에서는 1회 추가 행동만 사용
    public int bonusActionCount = 1;

    // <변경부분> 유물 발동 시 흡수 직후 고유스킬 사용 제한을 풀지 여부
    public bool enableUniqueSkillAfterAbsorb = true;
}