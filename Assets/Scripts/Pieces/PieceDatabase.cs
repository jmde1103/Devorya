using UnityEngine;

// <변경부분> PieceType 기준으로 기본 PieceData를 찾아주는 데이터베이스
// 1차 구조에서는 타입 기준 검색만 제공하고, 나중에 종족/세력별 검색으로 확장 가능하다.
[CreateAssetMenu(fileName = "PieceDatabase", menuName = "Devorya/Piece/Piece Database")]
public class PieceDatabase : ScriptableObject
{
    [Header("Piece Data List")]
    // <변경부분> 프로젝트에서 사용하는 모든 기본 기물 데이터 목록
    [SerializeField] private PieceData[] pieceDataList;

    // <변경부분> PieceType 기준으로 PieceData 검색
    // 같은 PieceType 데이터가 여러 개 있을 수 있으므로 정확한 검색에는 GetData(string pieceId)를 사용하는 것을 권장
    public PieceData GetData(PieceType pieceType)
    {
        if (pieceDataList == null)
        {
            return null;
        }

        for (int i = 0; i < pieceDataList.Length; i++)
        {
            PieceData data = pieceDataList[i];

            if (data == null)
            {
                continue;
            }

            if (data.pieceType == pieceType)
            {
                return data;
            }
        }

        return null;
    }

    // <변경부분> pieceId 기준으로 PieceData를 정확히 검색
    // DevoryaPawn / JelluPawn처럼 같은 Pawn 타입 안에서도 원하는 데이터를 구분할 때 사용
    public PieceData GetData(string pieceId)
    {
        if (string.IsNullOrEmpty(pieceId))
        {
            return null;
        }

        if (pieceDataList == null)
        {
            return null;
        }

        for (int i = 0; i < pieceDataList.Length; i++)
        {
            PieceData data = pieceDataList[i];

            if (data == null)
            {
                continue;
            }

            if (data.pieceId == pieceId)
            {
                return data;
            }
        }

        Debug.LogWarning($"PieceDatabase에서 pieceId {pieceId}에 해당하는 PieceData를 찾지 못했습니다.");

        return null;
    }
}
