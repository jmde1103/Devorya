using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundMapData", menuName = "Devorya/Background Map Data")]
public class BackgroundMapData : ScriptableObject
{
    [Header("맵 기본 정보")]
    // 저장된 배경 맵의 가로 크기
    public int Width;

    // 저장된 배경 맵의 세로 크기
    public int Height;

    // 배경 전체 위치 보정값 저장
    public Vector3 BackgroundOriginOffset;


    [Header("Environment Visual")]

    // <변경부분>
    // 이 BackgroundMapData를 사용할 때 함께 적용할
    // 환경 조명 Profile.
    //
    // Event Scene과 Battle Scene 모두 동일한 BackgroundMapData를 사용하면
    // 동일한 환경 조명이 자동으로 적용된다.
    public EnvironmentVisualProfile VisualProfile;


    [Header("Decoration Palette")]

    // <변경부분>
    // 이 맵에서 사용할 장식물 Sprite 목록과
    // DecorationType별 설정을 보관하는 Palette.
    //
    // 스테이지마다 서로 다른 Palette를 연결할 수 있다.
    public DecorationPaletteData DecorationPalette;


    [Header("배경 타일 데이터")]
    // 배경 타일 타입을 1차원 리스트로 저장
    public List<BackgroundTileSaveData> Tiles = new List<BackgroundTileSaveData>();

    [Header("장식물 데이터")]
    // 장식물 타입과 좌표를 저장
    public List<DecorationSaveData> Decorations = new List<DecorationSaveData>();


    [Header("Shop Display Slots")]

    // <변경부분>
    // 이 배경맵에 배치한 상점 상품 진열 위치 목록.
    //
    // 일반 Decoration과 별도로 관리한다.
    //
    // Background Editor에서는:
    // - ShopItemDisplay Prefab
    // - Anchor 좌표
    // - Free Placement Offset
    // - Layer Offset
    // 정보를 저장한다.
    //
    // 실제 판매 BattleItemData와 가격은 저장하지 않는다.
    //
    // 상점 진입 시 WorldMap에서 전달된
    // ShopInventoryData와 Shop Level을 기준으로
    // 판매 상품을 랜덤 결정한다.
    //
    // Runtime에서는 ShopRoot 아래에
    // ShopItemDisplay를 생성하는 데 사용한다.
    public List<ShopDisplaySlotSaveData> ShopDisplaySlots =
        new List<ShopDisplaySlotSaveData>();
}

[System.Serializable]
public class BackgroundTileSaveData
{
    // 저장할 배경 타일 X 좌표
    public int X;

    // 저장할 배경 타일 Y 좌표
    public int Y;

    // 저장할 배경 타일 타입
    public BackgroundTileType TileType;

    // 저장 당시 실제로 사용된 배경 타일 Sprite.
    //
    // Prefab 방식에서는 null이다.
    public Sprite TileSprite;

    // <변경부분>
    // Prefab 방식의 배경 타일일 경우
    // 실제로 사용한 원본 Prefab Asset을 저장한다.
    //
    // 기존 Sprite 기반 데이터에서는 null이다.
    public GameObject TilePrefab;
}

[System.Serializable]
public class DecorationSaveData
{
    // Anchor로 사용할 배경 타일 X 좌표
    public int X;

    // Anchor로 사용할 배경 타일 Y 좌표
    public int Y;

    // 저장할 장식물 타입
    public DecorationType DecorationType;

    // 저장 당시 실제로 사용된 장식물 Sprite.
    //
    // Prefab 방식에서는 null이다.
    public Sprite DecorationSprite;

    // <변경부분>
    // Prefab 방식의 Decoration일 경우
    // 실제로 사용한 원본 Prefab Asset을 저장한다.
    //
    // 기존 Sprite 기반 데이터에서는 null이다.
    public GameObject DecorationPrefab;

    // Anchor Tile 중심에서 실제 장식물 위치까지의 상대 좌표.
    //
    // Grid Placement는 Vector3.zero,
    // Free Placement는 자유 배치된 위치 차이가 저장된다.
    public Vector3 LocalPositionOffset;

    // <변경부분>
    // 동일한 Anchor Tile 안에서 장식물끼리 앞뒤 순서를 조절하는 값.
    //
    // 0 = 기본
    // 음수 = 뒤
    // 양수 = 앞
    //
    // 현재 사용 범위는 -3 ~ +3.
    // 기존 저장 데이터에서는 기본값 0으로 처리된다.
    public int LayerOffset;
}


// ============================================================
// Shop Display Slot Save Data
// ============================================================

// <변경부분>
// BackgroundMapData에 저장되는 상점 상품 진열 위치.
//
// 일반 Decoration과 구분되는 전용 배치 데이터다.
//
// 실제 아이템과 가격은 이 데이터에 저장하지 않는다.
// 따라서 같은 배경맵을 여러 레벨의 상점에서 재사용할 수 있다.
[System.Serializable]
public class ShopDisplaySlotSaveData
{
    // <변경부분>
    // 각 진열 슬롯의 고유 식별자.
    //
    // 향후 에디터에서 최초 배치할 때 생성하며
    // 위치나 순서가 변경되어도 같은 ID를 유지한다.
    public string SlotId;


    // <변경부분>
    // 이 위치에 생성할 ShopItemDisplay Prefab.
    //
    // 서로 다른 디자인의 상점에서
    // 다양한 상품 진열 프리팹을 사용할 수 있다.
    public GameObject ShopDisplayPrefab;


    // 배경 타일 Anchor X 좌표.
    public int X;

    // 배경 타일 Anchor Y 좌표.
    public int Y;


    // <변경부분>
    // Anchor Tile 중심에서 실제 진열 위치까지의 Offset.
    //
    // 기존 Decoration의 Free Placement 방식과
    // 동일한 좌표 기준을 사용한다.
    public Vector3 LocalPositionOffset;


    // <변경부분>
    // 진열 상품의 앞뒤 Sorting 보정값.
    //
    // 기존 Decoration Layer Offset과
    // 동일하게 -3 ~ +3 범위를 사용한다.
    [Range(-3, 3)]
    public int LayerOffset;
}