using System.Collections.Generic;
using UnityEngine;

// <변경부분> 상태이상 기본 데이터를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "StatusEffectData", menuName = "Devorya/Status Effect Data")]
public class StatusEffectData : ScriptableObject
{
    // 상태이상 종류
    public StatusEffectType effectType;

    // 상태이상 이름
    public string effectName;

    // 상태이상 설명
    [TextArea]
    public string description;

    // <변경부분> 상태효과 설명 팝업 하단에 추가로 붙일 설명 블록 목록
    // 이름, 설명, 아이콘은 기존 effectName / description / iconSprite를 그대로 사용한다.
    public List<TooltipSectionData> tooltipSections = new List<TooltipSectionData>();

    // <변경부분> 상태이상 UI에 표시할 아이콘
    public Sprite iconSprite;

    // <변경부분> 상태이상 유지 턴
    // 퇴화는 1턴 유지
    public int durationTurn = 1;

    // <변경부분> 상태이상 최대 중첩 수
    // 현재 퇴화는 1개만 의미 있게 사용하지만, 이후 확장을 위해 데이터로 관리
    public int maxStack = 1;
}
