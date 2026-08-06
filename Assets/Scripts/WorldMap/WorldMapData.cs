using System.Collections.Generic;
using UnityEngine;

// 월드맵 하나의 배경, 크기,
// 노드 배치 정보를 저장하는 데이터이다.
[CreateAssetMenu(
    fileName = "WorldMapData",
    menuName = "Devorya/World Map/World Map Data"
)]
public class WorldMapData : ScriptableObject
{
    [Header("Map Identity")]
    // 맵 데이터를 구분하기 위한 고유 ID
    //
    // 예:
    // ForestMap_01
    public string mapId;

    // Inspector와 UI에서 확인할 맵 이름
    public string mapDisplayName;

    [Header("Map Background")]
    // 맵 씬의 BaseMap SpriteRenderer에 표시할 배경 이미지
    public Sprite backgroundSprite;

    [Header("Grid Size")]
    // 16×16 기준 맵의 가로 셀 수
    //
    // 800픽셀 ÷ 16픽셀 = 50칸
    [Min(1)]
    public int gridWidth = 50;

    // 16×16 기준 맵의 세로 셀 수
    //
    // 480픽셀 ÷ 16픽셀 = 30칸
    [Min(1)]
    public int gridHeight = 30;

    [Header("Node Placement")]
    // 맵 위에 배치된 모든 노드 정보
    public List<MapNodePlacementData> nodePlacements =
        new List<MapNodePlacementData>();
}
