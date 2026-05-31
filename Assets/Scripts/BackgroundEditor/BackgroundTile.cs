using UnityEngine;

public class BackgroundTile : MonoBehaviour
{
    // 배경 타일이 어떤 지형 타입인지 저장
    public BackgroundTileType TileType { get; private set; }

    // 배경 타일의 배열 좌표를 저장
    public int X { get; private set; }
    public int Y { get; private set; }

    // 배경 타일의 타입과 좌표 정보를 초기화
    public void Initialize(BackgroundTileType tileType, int x, int y)
    {
        TileType = tileType;
        X = x;
        Y = y;
    }

    // 배경 타일 타입을 변경해 에디터 칠하기 기능에 사용
    public void SetTileType(BackgroundTileType tileType)
    {
        TileType = tileType;
    }

   
}
