using UnityEngine;

// <변경부분> 여러 기물 배치 데이터를 하나의 편성 세트로 묶는 ScriptableObject
// 예: Jellu 구성 세트 1번, Jellu 돌격 세트, 보스 호위 세트 등
[CreateAssetMenu(fileName = "PieceFormationData", menuName = "Devorya/Battle/Piece Formation Data")]
public class PieceFormationData : ScriptableObject
{
    [Header("Formation Info")]
    // <변경부분> 편성 데이터 식별용 이름
    public string formationName;

    [TextArea]
    // <변경부분> 인스펙터에서만 확인하는 메모용 설명
    public string description;

    [Header("Spawn Data")]
    // <변경부분> 이 편성에 포함된 기물 배치 목록
    // StageBattleData는 기물을 직접 들지 않고 이 Formation을 참조한다.
    public BattlePieceSpawnData[] spawnDataList;

    // <변경부분> 편성 데이터가 실제로 사용할 수 있는 상태인지 확인
    public bool IsValid()
    {
        if (spawnDataList == null)
        {
            return false;
        }

        if (spawnDataList.Length == 0)
        {
            return false;
        }

        return true;
    }
}
