using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Size")] //가로 세로 칸수
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 6;

    [Header("Tile")]
    // <변경부분> 모든 지형이 공통으로 사용할 단일 타일 프리팹
    [SerializeField] private GameObject tilePrefab;

    // <변경부분> 생성된 타일들을 자식으로 넣을 부모 오브젝트
    [SerializeField] private Transform tileParent;

    [Header("Checker Tile Type")]
    // <변경부분> 체크무늬 A칸에 사용할 기본 타일 타입
    [SerializeField] private TileType checkerTileTypeA = TileType.Metal;

    // <변경부분> 체크무늬 B칸에 사용할 기본 타일 타입
    [SerializeField] private TileType checkerTileTypeB = TileType.MetalDark;

    [Header("Tile Database")]
    // <변경부분> TileType 기준으로 실제 TileData를 찾는 데이터베이스
    [SerializeField] private TileDatabase tileDatabase;

    [Header("Isometric Setting")] //타일 전체 높이의 절반. 아이소메트리의 2:1 타일 비율
    [SerializeField] private float tileWidthHalf = 0.64f;
    [SerializeField] private float tileHeightHalf = 0.32f;

    private Tile[,] tiles; // 모든 타일을 좌표 기준으로 저장하는 2차원 배열


    private void GenerateBoard() // 보드 생성기
    {
        for (int y = 0; y < height; y++) // 타일 높이만큼 반복
        {
            for (int x = 0; x < width; x++) // 타일 너비만큼 반복
            {
                Vector3 position = GridToWorld(x, y); // 격자 좌표를 월드 좌표로 변환

                // <변경부분> 단일 타일 프리팹을 복제하여 배치
                GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, tileParent);

                tileObject.name = $"Tile_{x}_{y}"; // 자식 타일 이름 지정

                SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();

                if (spriteRenderer == null)
                {
                    spriteRenderer = tileObject.GetComponentInChildren<SpriteRenderer>();
                }

                if (spriteRenderer != null) // 아이소메트리 타일 정렬 순서 지정
                {
                    spriteRenderer.sortingOrder = -(x + y);
                }

                Tile tile = tileObject.GetComponent<Tile>(); // 타일 컴포넌트 가져오기

                if (tile == null)
                {
                    Debug.LogError($"{tileObject.name}에 Tile 컴포넌트가 없습니다.");
                    continue;
                }

                // <변경부분> 체크무늬 규칙에 따라 A/B 타일 데이터를 선택
                TileData tileData = GetCheckerTileData(x, y);

                if (tileData == null)
                {
                    Debug.LogError($"체크무늬 타일 데이터가 없습니다. 좌표: ({x}, {y}) / BoardManager의 checkerTileDataA, checkerTileDataB를 확인하세요.");
                    continue;
                }

                // <변경부분> TileData 기준으로 좌표/지형/스프라이트/효과를 한 번에 초기화
                tile.Initialize(x, y, tileData);

                tiles[x, y] = tile; // 2차원 배열에 저장
            }
        }
    }

    // <변경부분> 체크무늬 규칙에 따라 TileType을 선택하고 TileDatabase에서 TileData를 가져오는 함수
    private TileData GetCheckerTileData(int x, int y)
    {
        TileType selectedTileType = (x + y) % 2 == 0
            ? checkerTileTypeA
            : checkerTileTypeB;

        return GetTileData(selectedTileType);
    }

    // <변경부분> TileType 기준으로 TileData를 가져오는 함수
    private TileData GetTileData(TileType tileType)
    {
        if (tileDatabase == null)
        {
            Debug.LogError("TileDatabase가 BoardManager에 연결되지 않았습니다.");
            return null;
        }

        return tileDatabase.GetData(tileType);
    }

    // <변경부분> 외부 시스템 또는 스킬에서 특정 좌표의 타일 데이터를 교체할 때 사용하는 함수
    public bool ChangeTileData(int x, int y, TileType newTileType)
    {
        Tile tile = GetTile(x, y);

        if (tile == null)
        {
            return false;
        }

        TileData tileData = GetTileData(newTileType);

        if (tileData == null)
        {
            Debug.LogWarning($"{newTileType}에 해당하는 TileData가 없어 타일을 변경할 수 없습니다.");
            return false;
        }

        tile.ApplyTileData(tileData);

        Debug.Log($"타일 변경: ({x}, {y}) → {newTileType}");

        return true;
    }

    // <변경부분> StageBattleData에서 받은 TileType A/B 기준으로 보드를 다시 생성하는 함수
    public void RebuildBoardByTileType(TileType tileTypeA, TileType tileTypeB)
    {
        checkerTileTypeA = tileTypeA;
        checkerTileTypeB = tileTypeB;

        ClearBoard();

        tiles = new Tile[width, height];

        GenerateBoard();

        Debug.Log($"보드 재생성 완료: {checkerTileTypeA} / {checkerTileTypeB}");
    }

    // <변경부분> 기존 보드 타일 오브젝트를 모두 제거하는 함수
    private void ClearBoard()
    {
        if (tileParent != null)
        {
            for (int i = tileParent.childCount - 1; i >= 0; i--)
            {
                Destroy(tileParent.GetChild(i).gameObject);
            }
        }

        tiles = null;
    }

    public Vector3 GridToWorld(int x, int y) // 격자 좌표를 실제 화면 위치로 바꿈
    {
        float worldX = (x - y) * tileWidthHalf;
        float worldY = (x + y) * tileHeightHalf;

        return new Vector3(worldX, worldY, 0f);
    }

    public Tile GetTile(int x, int y) // 특정 좌표의 타일을 반환
    {
        //타일 배열이 아직 생성되지 않았을 경우 방어
        if (tiles == null)
        {
            Debug.Log("Tiles 배열이 아직 생성되지 않았습니다.");
            return null;
        }

        // 범위 검사
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            Debug.Log($"좌표 ({x}, {y})는 보드 범위를 벗어났습니다.");
            return null;
        }

        return tiles[x, y];
    }
    public int Width => width; // 보드판 가로 크기 반환
    public int Height => height; // 보드판 가로 크기 반환
}
