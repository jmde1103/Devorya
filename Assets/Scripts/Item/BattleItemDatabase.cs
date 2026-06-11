using UnityEngine;

// <변경부분> BattleItemType으로 BattleItemData를 찾는 아이템 데이터베이스
[CreateAssetMenu(fileName = "BattleItemDatabase", menuName = "Devorya/Battle/Item Database")]
public class BattleItemDatabase : ScriptableObject
{
    // 전투 아이템 데이터 목록
    [SerializeField] private BattleItemData[] itemDatas;

    // <변경부분> 아이템 타입으로 아이템 데이터 찾기
    public BattleItemData GetData(BattleItemType itemType)
    {
        if (itemType == BattleItemType.None)
        {
            return null;
        }

        if (itemDatas == null)
        {
            return null;
        }

        foreach (BattleItemData itemData in itemDatas)
        {
            if (itemData == null)
            {
                continue;
            }

            if (itemData.itemType == itemType)
            {
                return itemData;
            }
        }

        return null;
    }
}
