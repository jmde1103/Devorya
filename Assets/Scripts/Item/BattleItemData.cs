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
}