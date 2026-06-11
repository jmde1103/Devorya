using UnityEngine;

// <변경부분> BattleRelicType으로 BattleRelicData를 찾는 유물 데이터베이스
[CreateAssetMenu(fileName = "BattleRelicDatabase", menuName = "Devorya/Battle/Relic Database")]
public class BattleRelicDatabase : ScriptableObject
{
    // 전투 유물 데이터 목록
    [SerializeField] private BattleRelicData[] relicDatas;

    // <변경부분> 유물 타입으로 유물 데이터 찾기
    public BattleRelicData GetData(BattleRelicType relicType)
    {
        if (relicType == BattleRelicType.None)
        {
            return null;
        }

        if (relicDatas == null)
        {
            return null;
        }

        foreach (BattleRelicData relicData in relicDatas)
        {
            if (relicData == null)
            {
                continue;
            }

            if (relicData.relicType == relicType)
            {
                return relicData;
            }
        }

        return null;
    }
}
