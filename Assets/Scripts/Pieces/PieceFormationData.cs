using UnityEngine;

// <변경부분> PieceFormationData가 기물을 배치하는 방식을 구분한다.
public enum PieceFormationSpawnMode
{
    // 기존처럼 각 기물의 좌표를 직접 지정하는 방식
    Manual,

    // 상대 진영 시작 10칸 안에 등록 기물을 비율에 따라 랜덤 배치
    RandomEnemyStartZone
}

// <변경부분> 랜덤 편성에서 사용할 PieceData와 출현 비율을 저장한다.
[System.Serializable]
public class RandomFormationPieceEntry
{
    // 랜덤 생성 후보로 사용할 기물 데이터
    public PieceData pieceData;

    // 다른 후보와 비교할 상대적인 출현 비율
    // 예: Pawn 60 / Rook 10 / Knight 15 / Bishop 15
    [Min(0)]
    public int weight = 1;

    // <변경부분> 일반 무킹 전투에서 허용하는 기물인지 확인한다.
    public bool IsValid()
    {
        if (pieceData == null ||
            weight <= 0)
        {
            return false;
        }

        // 랜덤 일반 스테이지에는 King을 절대 생성하지 않는다.
        switch (pieceData.pieceType)
        {
            case PieceType.Pawn:
            case PieceType.Rook:
            case PieceType.Knight:
            case PieceType.Bishop:
                return true;
        }

        return false;
    }
}

// <변경부분> 여러 기물 배치 데이터를 하나의 편성 세트로 묶는 ScriptableObject
// 수동 좌표 배치와 상대 시작 진영 랜덤 배치를 모두 지원한다.
[CreateAssetMenu(
    fileName = "PieceFormationData",
    menuName = "Devorya/Battle/Piece Formation Data"
)]
public class PieceFormationData : ScriptableObject
{
    [Header("Formation Info")]
    // <변경부분> 편성 데이터 식별용 이름
    public string formationName;

    [TextArea]
    // <변경부분> 인스펙터에서만 확인하는 메모용 설명
    public string description;

    [Header("Spawn Mode")]
    // <변경부분> 직접 좌표 배치 또는 상대 시작 영역 랜덤 배치 선택
    public PieceFormationSpawnMode spawnMode =
        PieceFormationSpawnMode.Manual;

    [Header("Manual Spawn Data")]
    // <변경부분> Manual 모드에서 사용하는 기존 기물 배치 목록
    // 기존 PieceFormationData 에셋과의 호환을 위해 필드명을 유지한다.
    public BattlePieceSpawnData[] spawnDataList;

    [Header("Random Enemy Start Zone")]
    // <변경부분> RandomEnemyStartZone 모드에서 생성할 적 기물 수
    // 현재 상대 시작 영역 최대 칸 수에 맞춰 1~10개로 제한한다.
    [Range(1, 10)]
    public int randomPieceCount = 5;

    // <변경부분> 랜덤 생성 후보 PieceData와 각 후보의 출현 비율
    public RandomFormationPieceEntry[] randomPieceEntries;

    // <변경부분> 현재 모드의 편성 데이터가 실제 사용 가능한지 확인한다.
    public bool IsValid()
    {
        if (spawnMode ==
            PieceFormationSpawnMode.Manual)
        {
            return
                spawnDataList != null &&
                spawnDataList.Length > 0;
        }

        if (randomPieceCount <= 0 ||
            randomPieceEntries == null ||
            randomPieceEntries.Length == 0)
        {
            return false;
        }

        // 유효한 랜덤 후보가 하나라도 있어야 한다.
        for (int i = 0;
             i < randomPieceEntries.Length;
             i++)
        {
            RandomFormationPieceEntry entry =
                randomPieceEntries[i];

            if (entry != null &&
                entry.IsValid())
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분> 등록된 가중치 비율에 따라 PieceData 하나를 추첨한다.
    // King, Special, Queen 또는 weight 0 후보는 자동 제외한다.
    public PieceData RollRandomPieceData()
    {
        if (randomPieceEntries == null ||
            randomPieceEntries.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;

        for (int i = 0;
             i < randomPieceEntries.Length;
             i++)
        {
            RandomFormationPieceEntry entry =
                randomPieceEntries[i];

            if (entry == null ||
                entry.IsValid() == false)
            {
                continue;
            }

            totalWeight +=
                entry.weight;
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll =
            Random.Range(0, totalWeight);

        int accumulatedWeight = 0;

        for (int i = 0;
             i < randomPieceEntries.Length;
             i++)
        {
            RandomFormationPieceEntry entry =
                randomPieceEntries[i];

            if (entry == null ||
                entry.IsValid() == false)
            {
                continue;
            }

            accumulatedWeight +=
                entry.weight;

            if (roll < accumulatedWeight)
            {
                return entry.pieceData;
            }
        }

        return null;
    }
}