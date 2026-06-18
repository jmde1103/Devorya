using System.Collections.Generic;
using UnityEngine;

// <변경부분> 타일 1종류의 데이터 묶음
// TileType, Sprite, 효과, 이동 가능 여부를 ScriptableObject로 관리한다.
[CreateAssetMenu(fileName = "TileData", menuName = "Devorya/Tile/Tile Data")]
public class TileData : ScriptableObject
{
    [Header("Basic")]
    // <변경부분> 이 데이터가 의미하는 지형 타입
    public TileType tileType;

    // <변경부분> 타일에 표시할 스프라이트
    public Sprite tileSprite;

    [Header("Rule")]
    // <변경부분> 이 타일 위에 기물이 올라갈 수 있는지 여부
    public bool isWalkable = true;

    // <변경부분> 장애물 여부
    public bool hasObstacle = false;

    [Header("Effects")]
    // <변경부분> 타일이 기본으로 가지는 효과 목록
    public List<TileEffectType> defaultTileEffects = new List<TileEffectType>();
}
