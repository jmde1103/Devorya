using System.Collections; // <변경부분> 기물 이동 연출 코루틴 사용
using System.Collections.Generic; // <변경부분> 런타임 기물 상태 목록 저장에 사용
using UnityEngine;
public class PieceManager : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private BoardManager boardManager;

    // <변경부분> 기물 이동/공격/방어/스킬 생성 등 시각 연출을 담당하는 매니저
    [SerializeField] private PieceAnimationManager pieceAnimationManager;

    // <변경부분> PieceType 기준 기본 PieceData를 찾아주는 데이터베이스
    // 1차 데이터화에서는 선택 사용이며, SpawnPieceFromData는 BattlePieceSpawnData의 pieceData를 우선 사용한다.
    [SerializeField] private PieceDatabase pieceDatabase;

    // 생성한 기물들을 정리해서 담아둘 부모 오브젝트
    [SerializeField] private Transform pieceParent;

    // 모든 기물이 공통으로 사용하는 단일 프리팹
    [Header("Piece Prefab")]
    [SerializeField] private GameObject piecePrefab;

    //보드 좌표별 기물 저장 배열
    private Piece[,] pieces;

    // 기물이 타일 위에 자연스럽게 올라오도록 Y 위치 보정
    [Header("Position Setting")]
    [SerializeField] private float pieceYOffset = 0.25f;

    [Header("Piece Limit")]
    // <변경부분> 플레이어 진영이 보유할 수 있는 최대 기물 수
    [SerializeField] private int maxPlayerPieceCount = 10;

    // <변경부분> 적 진영이 보유할 수 있는 최대 기물 수
    [SerializeField] private int maxEnemyPieceCount = 10;

    private void Start()
    {
        // <변경부분> BattleSetupManager가 먼저 실행되어 이미 pieces 배열을 만든 경우 다시 초기화하지 않음
        // Start 실행 순서에 따라 생성된 기물 배열이 비어버리는 문제를 방지
        if (pieces == null)
        {
            InitializePieceGrid();
        }

        // <변경부분> PieceAnimationManager 참조가 비어 있으면 같은 오브젝트에서 자동으로 찾음
        GetPieceAnimationManager();
    }

    // <변경부분> 현재 BoardManager 크기에 맞춰 기물 배열을 초기화하는 함수
    public void InitializePieceGrid()
    {
        if (boardManager == null)
        {
            Debug.LogError("PieceManager 초기화 실패: BoardManager가 연결되어 있지 않습니다.");
            return;
        }

        pieces = new Piece[boardManager.Width, boardManager.Height];

        Debug.Log($"PieceManager 기물 배열 초기화: {boardManager.Width} x {boardManager.Height}");
    }

    // <변경부분> 현재 생성된 모든 기물을 제거하고 기물 배열을 다시 초기화하는 함수
    public void ClearAllPieces()
    {
        if (pieces != null)
        {
            for (int y = 0; y < boardManager.Height; y++)
            {
                for (int x = 0; x < boardManager.Width; x++)
                {
                    Piece piece = pieces[x, y];

                    if (piece == null)
                    {
                        continue;
                    }

                    Destroy(piece.gameObject);
                }
            }
        }

        InitializePieceGrid();
    }

    // <변경부분> 현재 보드에 살아있는 플레이어 기물 상태를 런타임 데이터로 저장하는 함수
    public List<PlayerPieceRuntimeData> CapturePlayerPieceRuntimeData()
    {
        return CaptureTeamPieceRuntimeData(PieceTeam.Player);
    }

    // <변경부분> 특정 진영의 현재 기물 상태를 런타임 데이터로 저장하는 함수
    private List<PlayerPieceRuntimeData> CaptureTeamPieceRuntimeData(PieceTeam team)
    {
        List<PlayerPieceRuntimeData> runtimeDataList = new List<PlayerPieceRuntimeData>();

        if (pieces == null)
        {
            return runtimeDataList;
        }

        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece piece = pieces[x, y];

                if (piece == null)
                {
                    continue;
                }

                if (piece.Team != team)
                {
                    continue;
                }

                PlayerPieceRuntimeData runtimeData = PlayerPieceRuntimeData.FromPiece(piece);

                if (runtimeData == null)
                {
                    continue;
                }

                runtimeDataList.Add(runtimeData);
            }
        }

        Debug.Log($"{team} 기물 런타임 데이터 캡처 완료: {runtimeDataList.Count}개");

        return runtimeDataList;
    }

    // <변경부분> 특정 진영의 기물만 제거하는 함수
    // 다음 전투 시작 시 기존 테스트 플레이어 편성을 제거하고 런 저장 데이터를 배치할 때 사용
    public void ClearPiecesByTeam(PieceTeam team)
    {
        if (pieces == null)
        {
            return;
        }

        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece piece = pieces[x, y];

                if (piece == null)
                {
                    continue;
                }

                if (piece.Team != team)
                {
                    continue;
                }

                Destroy(piece.gameObject);
                pieces[x, y] = null;
            }
        }

        Debug.Log($"{team} 진영 기물 제거 완료");
    }

    // <변경부분> 저장된 런타임 데이터 기준으로 플레이어 기물을 자동 배치해서 다시 생성하는 함수
    // 전투 중 좌표는 저장하지 않고, 다음 전투 시작 시 플레이어 진영 기본 진형에 다시 배치한다.
    public void SpawnPlayerPiecesFromRuntimeData(
        List<PlayerPieceRuntimeData> runtimeDataList,
        bool clearExistingPlayerPieces = true)
    {
        if (runtimeDataList == null)
        {
            Debug.LogWarning("플레이어 기물 복원 실패: runtimeDataList가 null입니다.");
            return;
        }

        if (clearExistingPlayerPieces)
        {
            ClearPiecesByTeam(PieceTeam.Player);
        }

        List<PlayerPieceRuntimeData> kingPieces = new List<PlayerPieceRuntimeData>();
        List<PlayerPieceRuntimeData> backRowPreferredPieces = new List<PlayerPieceRuntimeData>();
        List<PlayerPieceRuntimeData> frontRowPreferredPieces = new List<PlayerPieceRuntimeData>();

        // <변경부분> 저장된 기물을 타입 기준으로 자동 배치 그룹에 분류
        for (int i = 0; i < runtimeDataList.Count; i++)
        {
            PlayerPieceRuntimeData runtimeData = runtimeDataList[i];

            if (runtimeData == null)
            {
                continue;
            }

            if (runtimeData.pieceType == PieceType.King)
            {
                kingPieces.Add(runtimeData);
                continue;
            }

            if (runtimeData.pieceType == PieceType.Pawn)
            {
                frontRowPreferredPieces.Add(runtimeData);
                continue;
            }

            // <변경부분> Knight / Rook / Bishop / 기타 비-Pawn 기물은 우선 뒷열 배치
            backRowPreferredPieces.Add(runtimeData);
        }

        int spawnedCount = 0;

        int backRowY = 0;
        int frontRowY = 1;
        int kingX = boardManager.Width / 2;

        Vector2Int kingPosition = new Vector2Int(kingX, backRowY);

        List<Vector2Int> backRowSlots = new List<Vector2Int>();
        List<Vector2Int> frontRowSlots = new List<Vector2Int>();

        // <변경부분> 뒷열 슬롯 구성
        // King 위치는 King이 있으면 비워두고, King이 없으면 일반 슬롯으로 사용한다.
        for (int x = 0; x < boardManager.Width; x++)
        {
            if (kingPieces.Count > 0 && x == kingX)
            {
                continue;
            }

            backRowSlots.Add(new Vector2Int(x, backRowY));
        }

        // <변경부분> 앞열 슬롯 구성
        for (int x = 0; x < boardManager.Width; x++)
        {
            frontRowSlots.Add(new Vector2Int(x, frontRowY));
        }

        // <변경부분> King은 항상 고정 위치에 먼저 배치
        if (kingPieces.Count > 0)
        {
            Piece kingPiece = SpawnPlayerRuntimePieceAt(kingPieces[0], kingPosition.x, kingPosition.y);

            if (kingPiece != null)
            {
                spawnedCount++;
            }

            // <변경부분> 혹시 King이 여러 개면 첫 번째만 고정 King으로 처리하고 나머지는 뒷열 선호 기물로 처리
            for (int i = 1; i < kingPieces.Count; i++)
            {
                backRowPreferredPieces.Add(kingPieces[i]);
            }
        }

        // <변경부분> Knight / Rook / Bishop 계열은 우선 뒷열에 배치
        spawnedCount += SpawnRuntimePiecesToSlots(backRowPreferredPieces, backRowSlots);

        // <변경부분> Pawn 계열은 우선 앞열에 배치
        spawnedCount += SpawnRuntimePiecesToSlots(frontRowPreferredPieces, frontRowSlots);

        // <변경부분> 뒷열 선호 기물이 남으면 앞열 남은 칸에 배치
        spawnedCount += SpawnRuntimePiecesToSlots(backRowPreferredPieces, frontRowSlots);

        // <변경부분> Pawn이 남으면 뒷열 남은 칸에 배치
        spawnedCount += SpawnRuntimePiecesToSlots(frontRowPreferredPieces, backRowSlots);

        Debug.Log($"플레이어 기물 런타임 데이터 자동 배치 복원 완료: {spawnedCount}개 / 저장 데이터 {runtimeDataList.Count}개");
    }

    // <변경부분> 런타임 기물 목록을 지정 슬롯 목록에 순서대로 생성하는 함수
    private int SpawnRuntimePiecesToSlots(
        List<PlayerPieceRuntimeData> runtimePieces,
        List<Vector2Int> targetSlots)
    {
        if (runtimePieces == null || targetSlots == null)
        {
            return 0;
        }

        int spawnedCount = 0;

        while (runtimePieces.Count > 0 && targetSlots.Count > 0)
        {
            PlayerPieceRuntimeData runtimeData = runtimePieces[0];
            Vector2Int targetSlot = targetSlots[0];

            runtimePieces.RemoveAt(0);
            targetSlots.RemoveAt(0);

            Piece spawnedPiece = SpawnPlayerRuntimePieceAt(runtimeData, targetSlot.x, targetSlot.y);

            if (spawnedPiece != null)
            {
                spawnedCount++;
            }
        }

        return spawnedCount;
    }

    // <변경부분> 런타임 데이터 1개를 지정 좌표에 플레이어 기물로 생성하는 함수
    private Piece SpawnPlayerRuntimePieceAt(PlayerPieceRuntimeData runtimeData, int x, int y)
    {
        if (runtimeData == null)
        {
            return null;
        }

        if (runtimeData.pieceData == null)
        {
            Debug.LogWarning("플레이어 기물 복원 실패: PieceData가 null입니다.");
            return null;
        }

        if (IsEmpty(x, y) == false)
        {
            Debug.LogWarning($"플레이어 기물 복원 실패: ({x}, {y}) 좌표에 이미 기물이 있습니다.");
            return null;
        }

        Piece spawnedPiece = SpawnPieceFromData(
            runtimeData.pieceData,
            PieceTeam.Player,
            x,
            y,
            runtimeData.canMove,
            runtimeData.isAbsorbedPlayerVisual
        );

        if (spawnedPiece == null)
        {
            Debug.LogWarning($"플레이어 기물 복원 실패: {runtimeData.pieceData.pieceId} / ({x}, {y})");
            return null;
        }

        // <변경부분> PieceData 기본 일반스킬이 아니라 전투 종료 시 저장된 일반스킬 상태로 복원
        spawnedPiece.ApplyGeneralSkillRuntimeData(runtimeData.generalSkills);

        // <변경부분> 일반스킬 복원 후 외형/정렬 상태 재갱신
        RefreshPieceVisual(spawnedPiece);

        return spawnedPiece;
    }


    // <변경부분> PieceAnimationManager 참조를 안전하게 가져오는 함수
    private PieceAnimationManager GetPieceAnimationManager()
    {
        // 인스펙터에 직접 연결되어 있으면 그대로 사용
        if (pieceAnimationManager != null)
        {
            return pieceAnimationManager;
        }

        // 같은 GameObject에 붙어 있는 PieceAnimationManager 자동 탐색
        pieceAnimationManager = GetComponent<PieceAnimationManager>();

        if (pieceAnimationManager == null)
        {
            Debug.LogError("PieceAnimationManager가 연결되지 않았습니다. PieceManager와 같은 GameObject에 PieceAnimationManager를 추가하거나 인스펙터에 연결하세요.");
        }

        return pieceAnimationManager;
    }

    // <변경부분> 여러 개의 BattlePieceSpawnData를 순서대로 생성하는 함수
    // 현재는 테스트 배치용으로 사용하고, 나중에는 BattleSetupManager가 이 함수를 호출하게 된다.
    public void SpawnPiecesFromDataList(BattlePieceSpawnData[] spawnDataList)
    {
        if (spawnDataList == null)
        {
            Debug.LogWarning("SpawnPiecesFromDataList 실패: spawnDataList가 null입니다.");
            return;
        }

        for (int i = 0; i < spawnDataList.Length; i++)
        {
            BattlePieceSpawnData spawnData = spawnDataList[i];

            if (spawnData == null)
            {
                Debug.LogWarning($"SpawnPiecesFromDataList: {i}번 배치 데이터가 null입니다.");
                continue;
            }

            Piece spawnedPiece = SpawnPieceFromData(spawnData);

            if (spawnedPiece == null)
            {
                Debug.LogWarning($"SpawnPiecesFromDataList: {i}번 배치 데이터 생성 실패");
            }
        }
    }

    // <변경부분> 외부에서도 스킬로 기물을 생성할 수 있도록 public으로 변경
    public Piece SpawnPiece(PieceType pieceType, PieceTeam team, int x, int y, bool canMove, UniqueSkillType uniqueSkill = UniqueSkillType.None, params PieceSpeciesTag[] speciesTags)
    {
        // 공통 프리팹이 비어 있으면 오류 출력
        if (piecePrefab == null)
        {
            Debug.LogError($"Piece Prefab이 연결되지 않았습니다.");
            return null;
        }

        // 해당 좌표의 타일 가져오기
        Tile targetTile = boardManager.GetTile(x, y);

        // 타일이 없으면 오류 출력
        if (targetTile == null)
        {
            Debug.LogError($"좌표 ({x}, {y})에 타일이 없습니다.");
            return null;
        }

        // <변경부분> 현재 WorldRoot 확대 상태가 반영된 타일 위치 기준으로 기물 생성 위치 계산
        Vector3 spawnPosition = GetPieceWorldPosition(targetTile);

        // <변경부분> 현재 타일의 실제 월드 위치에 기물 생성
        GameObject pieceObject = Instantiate(piecePrefab, spawnPosition, Quaternion.identity, pieceParent);

        // 생성된 기물 이름 설정
        pieceObject.name = $"{team}_{pieceType}_{x}_{y}";

        // Piece 컴포넌트 가져오기
        Piece piece = pieceObject.GetComponent<Piece>();

        // Piece 컴포넌트가 없으면 오류 출력
        if (piece == null)
        {
            Debug.LogError($"{pieceObject.name}에 Piece 컴포넌트가 없습니다.");
            return null;
        }

        // 기물 데이터 초기화
        // <변경부분> 생성 시 종족 태그도 함께 초기화
        piece.Initialize(pieceType, team, x, y, targetTile, canMove, uniqueSkill, speciesTags);

        // <변경부분> 기존 SpawnPiece로 생성된 기물도 가능한 경우 PieceData를 연결
        PieceData resolvedPieceData = ResolvePieceDataForLegacySpawn(pieceType, team, speciesTags);

        if (resolvedPieceData != null)
        {
            piece.SetCurrentPieceData(resolvedPieceData);
        }

        // <변경부분> 생성된 기물의 PieceData가 있으면 데이터 기준으로,
        // 없으면 기존 하드코딩 기준으로 외형/UI/아이콘 위치 적용
        RefreshPieceVisual(piece);

        // 기물의 아이소메트리 정렬 순서 설정
        SetPieceSortingOrder(pieceObject, x, y);

        //생성된 기물의 좌표 배열저장
        pieces[x, y] = piece;

        // 생성한 Piece 반환
        return piece;
    }

    // <변경부분> 기존 SpawnPiece 경로에서 PieceType / Team / SpeciesTag를 기준으로 PieceData를 찾아주는 임시 변환 함수
    // 최종적으로 모든 생성이 SpawnPieceFromData로 바뀌면 삭제 대상
    private PieceData ResolvePieceDataForLegacySpawn(PieceType pieceType, PieceTeam team, PieceSpeciesTag[] speciesTags)
    {
        if (pieceDatabase == null)
        {
            return null;
        }

        // <변경부분> 중립 젤루 벽
        if (team == PieceTeam.Neutral &&
            pieceType == PieceType.Special &&
            HasSpeciesTagInArray(speciesTags, PieceSpeciesTag.Jellu))
        {
            PieceData wallData = pieceDatabase.GetData("Jellu Netral");

            if (wallData != null)
            {
                return wallData;
            }
        }

        // <변경부분> 젤루 계열 기물
        if (HasSpeciesTagInArray(speciesTags, PieceSpeciesTag.Jellu))
        {
            string jelluPieceId = GetJelluPieceId(pieceType);

            PieceData jelluData = pieceDatabase.GetData(jelluPieceId);

            if (jelluData != null)
            {
                return jelluData;
            }
        }

        // <변경부분> 기본 플레이어 데보리아 계열 기물
        if (team == PieceTeam.Player)
        {
            string devoryaPieceId = GetDevoryaPieceId(pieceType);

            PieceData devoryaData = pieceDatabase.GetData(devoryaPieceId);

            if (devoryaData != null)
            {
                return devoryaData;
            }
        }

        // <변경부분> 그 외에는 PieceType 기준 fallback
        return pieceDatabase.GetData(pieceType);
    }

    // <변경부분> 종족 태그 배열에 특정 태그가 있는지 확인
    private bool HasSpeciesTagInArray(PieceSpeciesTag[] speciesTags, PieceSpeciesTag targetTag)
    {
        if (speciesTags == null)
        {
            return false;
        }

        for (int i = 0; i < speciesTags.Length; i++)
        {
            if (speciesTags[i] == targetTag)
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분> PieceType 기준 젤루 PieceData ID 반환
    private string GetJelluPieceId(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return "Jellu Pawn";

            case PieceType.Rook:
                return "Jellu Rook";

            case PieceType.Knight:
                return "Jellu Knight";

            case PieceType.Bishop:
                return "Jellu Bishop";

            case PieceType.King:
                return "Jellu King";

            case PieceType.Special:
                return "Jellu Netral";
        }

        return string.Empty;
    }

    // <변경부분> PieceType 기준 데보리아 기본 PieceData ID 반환
    private string GetDevoryaPieceId(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return "Devorya Pawn";

            case PieceType.Rook:
                return "Devorya Rook";

            case PieceType.Knight:
                return "Devorya Knight";

            case PieceType.Bishop:
                return "Devorya Bishop";

            case PieceType.King:
                return "Devorya King";
        }

        return string.Empty;
    }

    // <변경부분> BattlePieceSpawnData 기준으로 기물을 생성하는 데이터 기반 생성 함수
    // 기존 SpawnPiece는 유지하고, 노드/스테이지/플레이어 저장 데이터 기반 생성은 이 함수로 점진 전환한다.
    public Piece SpawnPieceFromData(BattlePieceSpawnData spawnData)
    {
        if (spawnData == null)
        {
            Debug.LogError("SpawnPieceFromData 실패: spawnData가 null입니다.");
            return null;
        }

        if (spawnData.pieceData == null)
        {
            Debug.LogError("SpawnPieceFromData 실패: spawnData.pieceData가 null입니다.");
            return null;
        }

        return SpawnPieceFromData(
            spawnData.pieceData,
            spawnData.team,
            spawnData.x,
            spawnData.y,
            spawnData.GetCanMove(),
            spawnData.isAbsorbedPlayerVisual
        );
    }

    // <변경부분> PieceData에 설정된 기본 일반스킬 목록을 Piece에 적용
    private void ApplyDefaultGeneralSkillsFromData(Piece piece, PieceData pieceData)
    {
        if (piece == null || pieceData == null)
        {
            return;
        }

        if (pieceData.defaultGeneralSkills == null)
        {
            return;
        }

        for (int i = 0; i < pieceData.defaultGeneralSkills.Length; i++)
        {
            OwnedGeneralSkillData skillData = pieceData.defaultGeneralSkills[i];

            if (skillData == null)
            {
                continue;
            }

            if (skillData.skillType == GeneralSkillType.None)
            {
                continue;
            }

            // <변경부분> 현재 Piece에 일반스킬을 지정 레벨로 부여
            piece.SetTestGeneralSkill(skillData.skillType, skillData.level);
        }
    }

    // <변경부분> PieceData 기준으로 기물을 생성하는 핵심 함수
    // 기존 SpawnPiece와 달리 스프라이트/상태 UI/타입 아이콘/기본 일반스킬을 PieceData에서 적용한다.
    public Piece SpawnPieceFromData(PieceData pieceData, PieceTeam team, int x, int y, bool canMove, bool isAbsorbedPlayerVisual = false)
    {
        if (pieceData == null)
        {
            Debug.LogError("SpawnPieceFromData 실패: pieceData가 null입니다.");
            return null;
        }

        if (piecePrefab == null)
        {
            Debug.LogError("SpawnPieceFromData 실패: Piece Prefab이 연결되지 않았습니다.");
            return null;
        }

        Tile targetTile = boardManager.GetTile(x, y);

        if (targetTile == null)
        {
            Debug.LogError($"SpawnPieceFromData 실패: 좌표 ({x}, {y})에 타일이 없습니다.");
            return null;
        }

        if (pieces[x, y] != null)
        {
            Debug.LogWarning($"SpawnPieceFromData 실패: 좌표 ({x}, {y})에 이미 기물이 있습니다.");
            return null;
        }

        Vector3 spawnPosition = GetPieceWorldPosition(targetTile);

        GameObject pieceObject = Instantiate(piecePrefab, spawnPosition, Quaternion.identity, pieceParent);

        pieceObject.name = $"{team}_{pieceData.pieceType}_{x}_{y}_Data";

        Piece piece = pieceObject.GetComponent<Piece>();

        if (piece == null)
        {
            Debug.LogError($"{pieceObject.name}에 Piece 컴포넌트가 없습니다.");
            Destroy(pieceObject);
            return null;
        }

        // <변경부분> PieceData의 기본 전투 정보로 Piece 초기화
        piece.Initialize(
         pieceData.pieceType,
         team,
          x,
          y,
         targetTile,
         canMove,
         pieceData.uniqueSkill,
         pieceData.speciesTags
        );

        // <변경부분> 이 기물이 어떤 PieceData를 기반으로 생성되었는지 저장
        piece.SetCurrentPieceData(pieceData);

        // <변경부분> 플레이어 흡수 외형인 경우 PieceData 외형 선택 전에 상태를 먼저 저장
        piece.SetAbsorbedJelluVisual(isAbsorbedPlayerVisual);

        // <변경부분> PieceData에 정의된 기본 일반스킬을 적용
        ApplyDefaultGeneralSkillsFromData(piece, pieceData);

        // <변경부분> PieceData 기준으로 필드 스프라이트, 상태 UI, 타입 아이콘 위치를 한 번에 적용
        RefreshPieceVisual(piece);

        // 기물의 아이소메트리 정렬 순서 설정
        SetPieceSortingOrder(pieceObject, x, y);

        pieces[x, y] = piece;

        Debug.Log($"데이터 기반 기물 생성: {team} / {pieceData.pieceType} / ({x}, {y})");

        return piece;
    }

    // 흡수 대상의 데이터를 복사하고 외형을 갱신하는 함수
    public void AbsorbPiece(Piece absorber, Piece targetPiece)
    {
        // 흡수하는 기물이나 대상 기물이 없으면 종료
        if (absorber == null || targetPiece == null)
        {
            return;
        }

        // 흡수자는 Jellu 뒷면 외형 상태로 변경
        absorber.SetAbsorbedJelluVisual(true);

        // 대상 기물 데이터를 흡수자에게 복사
        absorber.AbsorbFrom(targetPiece);

        // <변경부분> 대상이 가진 일반 스킬을 흡수자 일반 스킬 슬롯에 저장하거나 성장시킴
        absorber.AbsorbGeneralSkillsFrom(targetPiece);

        // <변경부분> 흡수 후 외형/상태 UI/타입 아이콘 위치를 PieceData 기준으로 갱신
        // CurrentPieceData가 없는 레거시 기물은 RefreshPieceVisual 내부 fallback으로 기존 방식 처리
        RefreshPieceVisual(absorber);
    }

    // <변경부분> King 전용 흡수 처리 함수
    // King은 PieceType / UniqueSkill / 외형을 유지하고, 대상의 일반스킬만 획득하거나 레벨업함
    public void AbsorbGeneralSkillsOnly(Piece absorber, Piece targetPiece)
    {
        // 흡수하는 기물이나 대상 기물이 없으면 종료
        if (absorber == null || targetPiece == null)
        {
            return;
        }

        // <변경부분> 이 함수는 King 성장용이므로 King이 아닌 기물은 처리하지 않음
        if (absorber.PieceType != PieceType.King)
        {
            Debug.LogWarning("AbsorbGeneralSkillsOnly는 King 기물에게만 사용해야 합니다.");
            return;
        }

        // <변경부분> King은 대상의 타입/고유스킬/외형을 복사하지 않고 일반스킬만 흡수
        absorber.AbsorbGeneralSkillsFrom(targetPiece);

        // <변경부분> King의 외형과 타입 아이콘은 그대로 유지하되, 혹시 모를 UI 갱신을 위해 현재 상태 재적용
        RefreshPieceVisual(absorber);

        Debug.Log($"King 일반스킬 흡수 완료: 대상 {targetPiece.PieceType}");
    }

    // <변경부분> PieceData 기준으로 필드 스프라이트, 스테이터스 UI, 타입 아이콘 위치를 한 번에 적용하는 함수
    // 데이터화 이후 외형 갱신의 1차 기준 함수
    private bool ApplyPieceDataVisual(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        PieceData pieceData = piece.CurrentPieceData;

        if (pieceData == null)
        {
            return false;
        }

        // <변경부분> 필드 외형 적용
        // Spine Visual이 등록된 PieceData는 Spine으로 표시하고, 없으면 기존 SpriteRenderer 방식으로 표시
        PieceVisualController visualController = piece.GetComponent<PieceVisualController>();

        if (visualController != null)
        {
            visualController.ApplyVisual(pieceData, piece.Team, piece.IsAbsorbedJelluVisual);
        }
        else
        {
            // <변경부분> PieceVisualController가 없는 기존 프리팹을 위한 SpriteRenderer fallback
            SpriteRenderer spriteRenderer = piece.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                Sprite spriteToApply = pieceData.GetSprite(piece.Team, piece.IsAbsorbedJelluVisual);

                if (spriteToApply != null)
                {
                    spriteRenderer.sprite = spriteToApply;
                    spriteRenderer.enabled = true;
                }
            }
        }

        // <변경부분> 스테이터스 UI 스프라이트 적용
        Sprite statusSprite = pieceData.GetStatusSprite(piece.Team, piece.IsAbsorbedJelluVisual);

        if (statusSprite != null)
        {
            piece.SetStatusUISprite(statusSprite);
        }

        // <변경부분> 타입 아이콘 위치 적용
        Vector3 typeIconPosition =
            pieceData.GetTypeIconPosition(
                piece.Team,
                piece.IsAbsorbedJelluVisual
            );

        piece.SetTypeIconLocalPosition(
            typeIconPosition
        );

        // <변경부분> 필드 상태효과 아이콘 위치도
        // 타입 아이콘과 동일하게 PieceData 기준으로 적용한다.
        Vector3 fieldStatusEffectPosition =
            pieceData.GetFieldStatusEffectPosition(
                piece.Team,
                piece.IsAbsorbedJelluVisual
            );

        piece.SetFieldStatusEffectLocalPosition(
            fieldStatusEffectPosition
        );

        return true;
    }

    public Piece GetPieceAt(int x, int y)
    {
        // 좌표가 보드 밖이면 null 반환
        if (x < 0 || x >= boardManager.Width || y < 0 || y >= boardManager.Height)
        {
            return null;
        }

        return pieces[x, y];
    }

    //특정 좌표가 비어있는지 확인
    public bool IsEmpty(int x, int y)
    {
        // 해당 좌표에 기물이 없으면 true 반환
        return GetPieceAt(x, y) == null;
    }

    // 기물을 특정 좌표로 이동시키는 함수
    // 기존 외부 호출 호환용 함수
    public void MovePiece(Piece piece, int targetX, int targetY)
    {
        // <변경부분> 기본 이동은 연출을 포함해서 실행
        StartCoroutine(MovePieceRoutine(piece, targetX, targetY, true));
    }

    // <변경부분> 기물을 특정 좌표로 이동시키고, 호출한 쪽에서 연출 종료까지 기다릴 수 있는 코루틴
    public IEnumerator MovePieceRoutine(Piece piece, int targetX, int targetY, bool playAnimation)
    {
        // 이동할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // 기존 좌표의 기물 정보를 비워 이동 전 상태를 정리
        pieces[piece.X, piece.Y] = null;

        // 이동할 좌표의 타일 정보를 가져와 실제 이동 위치 계산에 사용
        Tile targetTile = boardManager.GetTile(targetX, targetY);

        // 이동할 타일이 없으면 이동 처리 중단
        if (targetTile == null)
        {
            yield break;
        }

        // 현재 WorldRoot 확대 상태가 반영된 타일 위치 기준으로 최종 이동 위치 계산
        Vector3 targetPosition = GetPieceWorldPosition(targetTile);

        // <변경부분> playAnimation이 true면 목표 위치까지 점프 이동 연출을 기다림
        if (playAnimation)
        {
            PieceAnimationManager animationManager = GetPieceAnimationManager();

            if (animationManager != null)
            {
                yield return animationManager.PlayPieceJumpMoveAnimation(piece, targetPosition);
            }
            else
            {
                // <변경부분> 애니메이션 매니저가 없으면 연출 없이 즉시 위치 보정
                piece.transform.position = targetPosition;
            }
        }
        else
        {
            // <변경부분> 이미 공격 연출로 목표 위치에 도착한 경우 즉시 위치 보정만 처리
            piece.transform.position = targetPosition;
        }

        // 기물의 논리 좌표와 현재 타일 정보를 갱신
        piece.SetPosition(targetX, targetY, targetTile);

        // 이동한 좌표에 기물 정보를 다시 저장
        pieces[targetX, targetY] = piece;

        // 이동한 좌표 기준으로 기물 표시 순서를 갱신
        SetPieceSortingOrder(piece.gameObject, targetX, targetY);
    }

    // <변경부분> 현재 WorldRoot 확대 상태가 반영된 타일의 실제 월드 위치를 기준으로 기물 위치 계산
    private Vector3 GetPieceWorldPosition(Tile targetTile)
    {
        // 타일이 없으면 기본 위치 반환
        if (targetTile == null)
        {
            return Vector3.zero;
        }

        // 현재 화면 확대와 이동이 반영된 타일의 실제 위치 가져오기
        Vector3 worldPosition = targetTile.transform.position;

        // 기물이 타일 위에 자연스럽게 올라오도록 Y 위치 보정
        worldPosition.y += pieceYOffset * targetTile.transform.lossyScale.y;

        return worldPosition;
    }

    // 기물을 보드에서 제거하는 함수
    public void RemovePiece(Piece piece)
    {
        // 제거할 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // 배열에서 해당 좌표 비우기
        pieces[piece.X, piece.Y] = null;

        // 실제 오브젝트 제거
        Destroy(piece.gameObject);
    }
    // <변경부분> 특정 진영의 왕 역할 기물이 살아있는지 확인하는 함수
    // KingToQueen처럼 King이 Queen 타입으로 변한 경우도 승패 조건상 생존으로 인정
    public bool HasKing(PieceTeam team)
    {
        // 모든 좌표 순회
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                // 현재 좌표의 기물
                Piece piece = pieces[x, y];

                // 기물이 없으면 다음 칸으로
                if (piece == null)
                {
                    continue;
                }

                // <변경부분> 해당 진영의 King 또는 Queen이 있으면 왕 역할 기물이 살아있는 것으로 처리
                if (piece.Team == team && piece.PieceType == PieceType.King)
                {
                    return true;
                }
            }
        }

        // 왕 역할 기물을 찾지 못하면 false
        return false;
    }

    // 특정 진영의 기물이 하나라도 살아있는지 확인하는 함수
    public bool HasAnyPiece(PieceTeam team)
    {
        // 모든 좌표 순회
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                // 현재 좌표의 기물
                Piece piece = pieces[x, y];

                // 해당 진영 기물이 하나라도 있으면 true
                if (piece != null && piece.Team == team)
                {
                    return true;
                }
            }
        }

        // 하나도 없으면 false
        return false;
    }

    // <변경부분> 지정한 진영에 이동 가능한 기물이 하나라도 있는지 확인하는 함수
    // NoMovablePieces 패배 조건에서 사용한다.
    public bool HasAnyMovablePiece(PieceTeam team)
    {
        for (int x = 0; x < boardManager.Width; x++)
        {
            for (int y = 0; y < boardManager.Height; y++)
            {
                Piece piece = pieces[x, y];

                if (piece == null)
                {
                    continue;
                }

                if (piece.Team != team)
                {
                    continue;
                }

                // <변경부분> CanMove가 true인 기물이 하나라도 있으면 아직 패배 아님
                if (piece.CanMove)
                {
                    return true;
                }
            }
        }

        return false;
    }

    //특정 진영에서 왕 역할 기물을 제외한 기물이 하나라도 살아있는지 확인하는 함수
    public bool HasAnyNonKingPiece(PieceTeam team)
    {
        // 모든 좌표 순회
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                // 현재 좌표의 기물 가져오기
                Piece piece = pieces[x, y];

                // 기물이 없으면 다음 칸으로
                if (piece == null)
                {
                    continue;
                }

                // <변경부분> King 또는 Queen은 왕 역할 기물로 보고 제외
                if (piece.Team == team && piece.PieceType != PieceType.King)
                {
                    return true;
                }
            }
        }

        // 왕 역할 기물을 제외한 기물이 하나도 없으면 false
        return false;
    }

    // <변경부분> 특정 진영 기물들의 임시 이동 타입을 초기화하는 함수
    public void ClearTemporaryMoveTypes(PieceTeam team)
    {
        // 모든 좌표 순회
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                // 현재 좌표의 기물 확인
                Piece piece = pieces[x, y];

                // 기물이 없으면 다음 칸으로
                if (piece == null)
                {
                    continue;
                }

                // 해당 진영 기물의 임시 이동 타입 제거
                if (piece.Team == team)
                {
                    piece.ClearTemporaryMoveType();
                }
            }
        }
    }

    // <변경부분> 특정 진영의 현재 기물 수를 계산하는 함수
    public int GetPieceCount(PieceTeam team)
    {
        int count = 0;

        // 보드 전체를 돌면서 해당 진영 기물 수를 계산
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece piece = pieces[x, y];

                if (piece != null && piece.Team == team)
                {
                    count++;
                }
            }
        }

        return count;
    }

    // <변경부분> 특정 진영의 최대 기물 수를 반환하는 함수
    private int GetMaxPieceCount(PieceTeam team)
    {
        if (team == PieceTeam.Player)
        {
            return maxPlayerPieceCount;
        }

        if (team == PieceTeam.Enemy)
        {
            return maxEnemyPieceCount;
        }

        // 중립 기물은 현재 복제 제한 대상이 아니므로 제한 없음 처리
        return int.MaxValue;
    }

    // <변경부분> 특정 진영이 새 기물을 추가로 생성할 수 있는지 검사하는 함수
    public bool CanCreatePieceForTeam(PieceTeam team)
    {
        int currentCount = GetPieceCount(team);
        int maxCount = GetMaxPieceCount(team);

        return currentCount < maxCount;
    }


    // 기물의 화면 정렬 순서를 설정하는 함수
    // <변경부분> 기물과 타입 아이콘의 화면 정렬 순서를
    // 현재 타일 위치 기준으로 함께 설정하는 함수
    private void SetPieceSortingOrder(
     GameObject pieceObject,
     int x,
     int y)
    {
        if (pieceObject == null)
        {
            return;
        }

        // 타일보다 기물이 앞에 보이도록 큰 값을 더함
        int sortingOrder =
            100 - (x + y);

        // Sprite / Spine 외형 컨트롤러가 있으면
        // 해당 컨트롤러를 통해 정렬 순서 적용
        PieceVisualController visualController =
            pieceObject.GetComponent<PieceVisualController>();

        if (visualController != null)
        {
            visualController.SetSortingOrder(
                sortingOrder
            );

            return;
        }

        // 기존 SpriteRenderer만 있는 프리팹을 위한 fallback
        SpriteRenderer spriteRenderer =
            pieceObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder =
                sortingOrder;
        }
    }


    //Skill

    public void RefreshPieceVisual(Piece piece)
    {
        // 갱신할 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // <변경부분> PieceData 기준으로만 외형/UI/타입 아이콘 위치를 갱신
        // 이제 PieceManager의 하드코딩 스프라이트/아이콘 위치 fallback은 사용하지 않는다.
        bool appliedByPieceData = ApplyPieceDataVisual(piece);

        if (appliedByPieceData == false)
        {
            Debug.LogWarning($"RefreshPieceVisual 실패: {piece.Team} {piece.PieceType}에 CurrentPieceData가 연결되어 있지 않습니다.");
        }

        // <변경부분> 현재 좌표 기준 정렬 순서 갱신
        SetPieceSortingOrder(piece.gameObject, piece.X, piece.Y);
    }

    // <변경부분> 증식 스킬로 젤루 Pawn을 생성하는 함수
    // 기존 SpawnPiece 경유 없이 "Jellu Pawn" PieceData 기준으로 직접 생성한다.
    public Piece SpawnJelluPawn(PieceTeam team, int x, int y)
    {
        // <변경부분> Player / Enemy 진영이 최대 기물 수에 도달했다면 젤루 Pawn 생성 불가
        // JelluMultiply, Degeneration 사망 효과 모두 이 함수를 사용하므로 여기서 공통 제한 처리
        if (CanCreatePieceForTeam(team) == false)
        {
            Debug.Log($"젤루 Pawn 생성 실패: {team} 진영의 최대 기물 수에 도달했습니다. 현재 {GetPieceCount(team)} / 최대 {GetMaxPieceCount(team)}");
            return null;
        }

        if (pieceDatabase == null)
        {
            Debug.LogWarning("젤루 Pawn 생성 실패: PieceDatabase가 연결되어 있지 않습니다.");
            return null;
        }

        // <변경부분> 실제 PieceData ID 기준으로 Jellu Pawn 데이터 검색
        PieceData jelluPawnData = pieceDatabase.GetData("Jellu Pawn");

        if (jelluPawnData == null)
        {
            Debug.LogWarning("젤루 Pawn 생성 실패: PieceDatabase에서 'Jellu Pawn' PieceData를 찾을 수 없습니다.");
            return null;
        }

        // <변경부분> Player가 생성한 젤루는 흡수 젤루 뒷면 외형 사용
        // Enemy가 생성한 젤루는 적 앞면 외형 사용
        bool useBackSprite = team == PieceTeam.Player;

        // <변경부분> SpawnPiece 경유 없이 PieceData 기반 생성 함수로 직접 생성
        Piece createdPiece = SpawnPieceFromData(
            jelluPawnData,
            team,
            x,
            y,
            jelluPawnData.canMove,
            useBackSprite
        );

        if (createdPiece == null)
        {
            return null;
        }

        Debug.Log($"젤루 Pawn 생성 완료: {team} / ({x}, {y})");

        return createdPiece;
    }

    // <변경부분> 젤루 벽 스킬로 중립 Special 기물을 생성하는 함수
    // PieceDatabase의 "Jellu Netral" PieceData 기준으로 생성함
    public Piece SpawnJelluWall(int x, int y)
    {
        if (pieceDatabase == null)
        {
            Debug.LogWarning("젤루 벽 생성 실패: PieceDatabase가 연결되어 있지 않습니다.");
            return null;
        }

        // <변경부분> PieceDatabase에 등록된 실제 중립 젤루 pieceId 기준으로 데이터 검색
        PieceData jelluNeutralData = pieceDatabase.GetData("Jellu Netral");

        if (jelluNeutralData == null)
        {
            Debug.LogWarning("젤루 벽 생성 실패: PieceDatabase에서 'Jellu Netral' PieceData를 찾을 수 없습니다.");
            return null;
        }

        // <변경부분> SpawnPiece 경유 없이 PieceData 기반 생성 함수로 바로 생성
        Piece createdWall = SpawnPieceFromData(
            jelluNeutralData,
            PieceTeam.Neutral,
            x,
            y,
            false,
            false
        );

        if (createdWall == null)
        {
            return null;
        }

        Debug.Log($"젤루 벽 생성 완료: Neutral Special / ({x}, {y})");

        return createdWall;
    }



    // <변경부분> 젤루 합성으로 기물을 상위 젤루 타입으로 승급시키는 함수
    public bool PromotePieceToJelluType(Piece piece, PieceType promotedType, UniqueSkillType promotedUniqueSkill = UniqueSkillType.None)
    {
        // 승급할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 젤루 합성 승급은 Pawn / King / Special을 제외
        if (promotedType == PieceType.Pawn ||
            promotedType == PieceType.King ||
            promotedType == PieceType.Special)
        {
            Debug.LogWarning($"젤루 합성 승급 실패: 허용되지 않는 타입입니다. {promotedType}");
            return false;
        }

        // <변경부분> Player 젤루는 뒷면, Enemy 젤루는 앞면으로 표시
        // Enemy 승급 시 true를 넣으면 흡수 젤루 뒷면 스프라이트가 적용되는 버그가 생김
        bool useBackSprite = piece.Team == PieceTeam.Player;

        string promotedPieceId = GetJelluPieceId(promotedType);

        PieceData promotedPieceData = null;

        if (pieceDatabase != null)
        {
            promotedPieceData = pieceDatabase.GetData(promotedPieceId);
        }

        if (promotedPieceData == null)
        {
            Debug.LogWarning($"젤루 합성 승급 실패: {promotedPieceId} PieceData를 찾을 수 없습니다.");
            return false;
        }

        piece.ChangePieceData(promotedPieceData, useBackSprite);

        RefreshPieceVisual(piece);

        Debug.Log($"젤루 합성 승급 완료: {promotedType}");

        return true;
    }



    // 기준 기물과 동일한 정보를 가진 기물을 새 좌표에 복제 생성하는 함수
    public Piece ClonePieceTo(Piece sourcePiece, int targetX, int targetY)
    {
        // 원본 기물이 없으면 종료
        if (sourcePiece == null)
        {
            return null;
        }

        // <변경부분> 원본 기물의 소속 진영이 최대 기물 수에 도달했다면 복제 불가
        if (CanCreatePieceForTeam(sourcePiece.Team) == false)
        {
            Debug.Log($"복제 실패: {sourcePiece.Team} 진영의 최대 기물 수에 도달했습니다. 현재 {GetPieceCount(sourcePiece.Team)} / 최대 {GetMaxPieceCount(sourcePiece.Team)}");
            return null;
        }

        // 이미 기물이 있는 칸이면 생성 불가
        if (IsEmpty(targetX, targetY) == false)
        {
            return null;
        }

        // 원본 기물의 타입, 진영, 이동 가능 여부, 고유스킬, 종족 태그를 그대로 복사해서 생성
        // <변경부분> 스킬 / 아이템 / 유물 조건 판정을 위해 종족 태그도 복사
        Piece clonedPiece = SpawnPiece(
            sourcePiece.PieceType,
            sourcePiece.Team,
            targetX,
            targetY,
            sourcePiece.CanMove,
            sourcePiece.UniqueSkill,
            sourcePiece.GetSpeciesTagsCopy()
        );

        // 생성 실패 시 종료
        if (clonedPiece == null)
        {
            return null;
        }

        // <변경부분> 원본 기물의 PieceData 참조 복사
        clonedPiece.SetCurrentPieceData(sourcePiece.CurrentPieceData);

        // <변경부분> 흡수 외형 상태 복사
        clonedPiece.SetAbsorbedJelluVisual(sourcePiece.IsAbsorbedJelluVisual);

        // <변경부분> 복제된 기물의 외형/상태 UI/타입 아이콘 위치를 현재 PieceData 기준으로 갱신
        RefreshPieceVisual(clonedPiece);

        return clonedPiece;
    }

    // <변경부분> 복제 스킬 전용 생성 함수
    // 생성된 복제 기물이 원본 기물 위치에서 생성 위치까지 포물선으로 이동
    public Piece ClonePieceToFromSource(Piece sourcePiece, int targetX, int targetY)
    {
        // 기존 복제 생성 로직 재사용
        Piece clonedPiece = ClonePieceTo(sourcePiece, targetX, targetY);

        // 생성 성공 시 원본 기물 위치에서 생성 위치까지 연출
        PlaySkillSpawnAnimationFromSource(clonedPiece, sourcePiece);

        return clonedPiece;
    }

    // <변경부분> 젤루 Pawn 생성 스킬 전용 함수
    // 증식처럼 시전자가 있는 스킬에서 사용
    public Piece SpawnJelluPawnFromSource(Piece sourcePiece, PieceTeam team, int x, int y)
    {
        // 기존 젤루 Pawn 생성 로직 재사용
        Piece createdPiece = SpawnJelluPawn(team, x, y);

        // 생성 성공 시 시전자 위치에서 생성 위치까지 연출
        PlaySkillSpawnAnimationFromSource(createdPiece, sourcePiece);

        return createdPiece;
    }

    // <변경부분> 젤루 Pawn 생성 상태이상 전용 함수
    // 퇴화처럼 생성 주체 Piece가 이미 제거될 수 있는 경우 월드 좌표 기준으로 연출
    public Piece SpawnJelluPawnFromWorldPosition(PieceTeam team, int x, int y, Vector3 sourceWorldPosition)
    {
        // 기존 젤루 Pawn 생성 로직 재사용
        Piece createdPiece = SpawnJelluPawn(team, x, y);

        // 생성 성공 시 저장된 월드 위치에서 생성 위치까지 연출
        PlaySkillSpawnAnimationFromWorldPosition(createdPiece, sourceWorldPosition);

        return createdPiece;
    }

    // <변경부분> 젤루 벽 생성 스킬 전용 함수
    // 벽이 시전자 위치에서 생성 위치까지 포물선으로 이동
    public Piece SpawnJelluWallFromSource(Piece sourcePiece, int x, int y)
    {
        // 기존 젤루 벽 생성 로직 재사용
        Piece createdWall = SpawnJelluWall(x, y);

        // 생성 성공 시 시전자 위치에서 생성 위치까지 연출
        PlaySkillSpawnAnimationFromSource(createdWall, sourcePiece);

        return createdWall;
    }

    // <변경부분> 기존 내부 호출 호환용 래퍼
    // 실제 스킬 생성 포물선 연출은 PieceAnimationManager가 처리
    private void PlaySkillSpawnAnimationFromSource(Piece spawnedPiece, Piece sourcePiece)
    {
        // 생성된 기물이나 시전자가 없으면 연출 불가
        if (spawnedPiece == null || sourcePiece == null)
        {
            return;
        }

        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            return;
        }

        animationManager.PlaySkillSpawnAnimationFromSource(spawnedPiece, sourcePiece);
    }

    // <변경부분> 기존 내부 호출 호환용 래퍼
    // 실제 스킬 생성 포물선 연출은 PieceAnimationManager가 처리
    private void PlaySkillSpawnAnimationFromWorldPosition(Piece spawnedPiece, Vector3 sourceWorldPosition)
    {
        // 생성된 기물이 없으면 연출 불가
        if (spawnedPiece == null)
        {
            return;
        }

        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            return;
        }

        animationManager.PlaySkillSpawnAnimationFromWorldPosition(spawnedPiece, sourceWorldPosition);
    }

    // <변경부분> 기물 선택 시 Select → Select_Idle 애니메이션을 요청하는 외부 호출 함수
    public void PlayPieceSelectAnimation(Piece piece)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            return;
        }

        animationManager.PlayPieceSelectAnimation(piece);
    }

    // <변경부분> 기물 선택 해제 시 Down → Idle 애니메이션을 요청하는 외부 호출 함수
    public void PlayPieceDeselectAnimation(Piece piece)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            return;
        }

        animationManager.PlayPieceDeselectAnimation(piece);
    }

    // <변경부분> 기물을 강제로 Idle 상태로 되돌리는 외부 호출 함수
    public void PlayPieceIdleAnimation(Piece piece)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            return;
        }

        animationManager.PlayPieceIdleAnimation(piece);
    }

    // <변경부분> 기물 생성 또는 흡수 후 외형 변경 시 Born 애니메이션을 요청하는 외부 호출 함수
    public IEnumerator PlayPieceBornAnimation(Piece piece)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            yield break;
        }

        yield return animationManager.PlayPieceBornAnimation(piece);
    }
    // <변경부분> 기존 2개 인수 호출을 유지하기 위한 호환용 래퍼
    // 흡수 여부를 전달하지 않은 기존 호출은 일반 공격으로 처리한다.
    public IEnumerator PlayPieceAttackMoveAnimation(
        Piece piece,
        Vector3 targetWorldPosition)
    {
        yield return PlayPieceAttackMoveAnimation(
            piece,
            targetWorldPosition,
            false,
            null
        );
    }

    // <변경부분> 기존 3개 인수 호출을 유지하기 위한 호환용 래퍼
    // 충격 콜백이 필요 없는 호출은 기존 동작을 그대로 사용한다.
    public IEnumerator PlayPieceAttackMoveAnimation(
        Piece piece,
        Vector3 targetWorldPosition,
        bool isAbsorbAction)
    {
        yield return PlayPieceAttackMoveAnimation(
            piece,
            targetWorldPosition,
            isAbsorbAction,
            null
        );
    }

    // <변경부분> 일반 공격/흡수 공격 여부와 충격 순간 실행할 콜백을
    // PieceAnimationManager에 전달하는 실제 공격 연출 중계 함수
    public IEnumerator PlayPieceAttackMoveAnimation(
        Piece piece,
        Vector3 targetWorldPosition,
        bool isAbsorbAction,
        System.Action onImpact)
    {
        PieceAnimationManager animationManager =
            GetPieceAnimationManager();

        if (animationManager == null)
        {
            // 애니메이션 매니저가 없어도 공격 기물 위치는 타겟 위치로 보정한다.
            if (piece != null)
            {
                piece.transform.position =
                    targetWorldPosition;
            }

            // <변경부분> 연출이 없어도 충격 순간 처리 자체는 실행한다.
            onImpact?.Invoke();

            yield break;
        }

        // <변경부분> 흡수 공격 여부와 충격 콜백을
        // 실제 공격 애니메이션 담당자에게 그대로 전달한다.
        yield return
            animationManager.PlayPieceAttackMoveAnimation(
                piece,
                targetWorldPosition,
                isAbsorbAction,
                onImpact
            );
    }

    // <변경부분> 기존 외부 호출 호환용 래퍼
    // 실제 방어 성공 튕김 연출은 PieceAnimationManager가 처리
    public IEnumerator PlayPieceBlockedAttackMoveAnimation(Piece piece, Vector3 targetWorldPosition)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            yield break;
        }

        yield return animationManager.PlayPieceBlockedAttackMoveAnimation(piece, targetWorldPosition);
    }

    // <변경부분> 기존 외부 호출 호환용 래퍼
    // 실제 젤루 합성 재료 이동 연출은 PieceAnimationManager가 처리
    public IEnumerator PlaySynthesisMaterialMoveAnimation(Piece firstMaterial, Piece secondMaterial, Piece targetPawn)
    {
        PieceAnimationManager animationManager = GetPieceAnimationManager();

        if (animationManager == null)
        {
            yield break;
        }

        yield return animationManager.PlaySynthesisMaterialMoveAnimation(firstMaterial, secondMaterial, targetPawn);
    }
}


