using UnityEngine;

// <변경부분> 전투 시작 시 기물 1개를 생성하기 위한 배치 데이터
// 나중에 NodeBattleData / PlayerPartyData가 이 데이터를 배열로 들고 BattleSetupManager에 넘기게 된다.
[System.Serializable]
public class BattlePieceSpawnData
{
    [Header("Piece")]
    // <변경부분> 생성할 기물 원형 데이터
    public PieceData pieceData;

    // <변경부분> 생성할 진영
    public PieceTeam team;

    [Header("Position")]
    // <변경부분> 생성할 보드 X 좌표
    public int x;

    // <변경부분> 생성할 보드 Y 좌표
    public int y;

    [Header("Override")]
    // <변경부분> PieceData의 canMove 대신 이 값을 사용할지 여부
    public bool overrideCanMove = false;

    // <변경부분> overrideCanMove가 true일 때 사용할 이동 가능 여부
    public bool canMove = true;

    // <변경부분> 흡수된 플레이어 외형으로 생성할지 여부
    // 플레이어 저장 데이터 기반 생성 때 사용 예정
    public bool isAbsorbedPlayerVisual = false;

    // <변경부분> 이 배치 데이터에서 실제로 사용할 이동 가능 여부
    public bool GetCanMove()
    {
        if (pieceData == null)
        {
            return canMove;
        }

        return overrideCanMove ? canMove : pieceData.canMove;
    }
}
