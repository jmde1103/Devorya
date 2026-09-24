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
}

[System.Serializable]
public class DecorationSaveData
{
    // 저장할 장식물 X 좌표
    public int X;

    // 저장할 장식물 Y 좌표
    public int Y;

    // 저장할 장식물 타입
    public DecorationType DecorationType;
}
