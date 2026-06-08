using UnityEngine;

// <변경부분> 전투 중 보유하면 지속 효과를 제공하는 유물 하나의 기본 데이터
[System.Serializable]
public class BattleRelicData
{
    // 유물 종류
    public BattleRelicType relicType = BattleRelicType.None;

    // 인스펙터와 로그에서 확인할 유물 이름
    public string relicName;

    // 유물 슬롯에 표시할 아이콘 이미지
    public Sprite iconSprite;

    // 유물 효과 설명
    [TextArea]
    public string description;
}