using UnityEngine;

// <변경부분> TileType 기준으로 TileData를 찾아주는 데이터베이스
[CreateAssetMenu(fileName = "TileDatabase", menuName = "Devorya/Tile/Tile Database")]
public class TileDatabase : ScriptableObject
{
    [Header("Tile Data List")]
    // <변경부분> 프로젝트에서 사용하는 모든 타일 데이터 목록
    [SerializeField] private TileData[] tileDataList;

    // <변경부분> TileType 기준으로 TileData를 검색
    public TileData GetData(TileType tileType)
    {
        if (tileDataList == null)
        {
            return null;
        }

        for (int i = 0; i < tileDataList.Length; i++)
        {
            TileData data = tileDataList[i];

            if (data == null)
            {
                continue;
            }

            if (data.tileType == tileType)
            {
                return data;
            }
        }

        return null;
    }
}