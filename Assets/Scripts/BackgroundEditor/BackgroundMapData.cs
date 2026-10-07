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