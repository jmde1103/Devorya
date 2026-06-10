using UnityEngine;

// <변경부분> 고유스킬 하나의 기본 설정 데이터를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "UniqueSkillData", menuName = "Devorya/Skill/Unique Skill Data")]
public class UniqueSkillData : ScriptableObject
{
    [Header("Basic")]
    // 고유스킬 종류
    public UniqueSkillType skillType = UniqueSkillType.None;

    // 인스펙터와 UI에 표시할 고유스킬 이름
    public string skillName;

    // 고유스킬 아이콘
    public Sprite iconSprite;

    // 고유스킬 설명
    [TextArea]
    public string description;

    [Header("Balance")]
    // <변경부분> 고유스킬 사용 후 적용할 쿨타임 턴 수
    public int cooldownTurn = 1;

    // <변경부분> 이 스킬을 사용하기 위해 필요한 자기 진영 사망 스택 수
    public int requiredDeathStack = 0;

    // <변경부분> 스킬 사용 성공 시 requiredDeathStack만큼 스택을 소모할지 여부
    public bool consumeDeathStackOnUse = false;

    // <변경부분> 한 턴에 한 번만 사용할 수 있는 스킬인지 여부
    public bool oncePerTurn = true;
}
