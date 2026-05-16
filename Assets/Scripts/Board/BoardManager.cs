using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Size")] //가로 세로 칸수
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 6;

    [Header("Tile")]
    [SerializeField] private GameObject tilePrefab1;// 첫 번째 타일 프리팹
    [SerializeField] private GameObject tilePrefab2;// 두 번째 타일 프리팹
    [SerializeField] private Transform tileParent; // 생성된 타일들을 자식으로 넣을 부모 오브젝트

    [Header("Isometric Setting")] //타일 전체 높이의 절반. 아이소메트리의 2:1 타일 비율
    [SerializeField] private float tileWidthHalf = 0.64f;
    [SerializeField] private float tileHeightHalf = 0.32f;

    private Tile[,] tiles; // 모든 타일을 좌표 기준으로 저장하는 2차원 배열

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        tiles = new Tile[width, height];// 타일 2차원 배열 생성
        GenerateBoard(); // 보드 생성
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void GenerateBoard() // 보드 생성기
    {
        for (int y = 0; y < height; y++) // 타일 높이 6개 만큼 반복
        {
            for (int x = 0; x < width; x++) // 타일 너비 5개 만큼 반복
            {
                Vector3 position = GridToWorld(x, y); // 격자 좌표를 월드 좌표로 변환

                GameObject prefabToSpawn = GetTilePrefab(x, y);
                GameObject tileObject = Instantiate(prefabToSpawn, position, Quaternion.identity, tileParent);  // 프리팹을 복제여 배치

                tileObject.name = $"Tile_{x}_{y}"; // 자식 타일 이름 지정

                SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null) // 아이소메트리 타일 정렬 순서 지정
                {
                    spriteRenderer.sortingOrder = -(x + y);
                }

                Tile tile = tileObject.GetComponent<Tile>(); // 타일 컨포넌트 가져오기

                if (tile == null)
                {
                    Debug.LogError($"{tileObject.name}에 Tile 컴포넌트가 없습니다.");
                    continue;
                }

                TileType tileType = ((x + y) % 2 == 0) ? TileType.Metal : TileType.MetalDark; //초기 지형 타입 결정

                tile.Initialize(x, y, tileType); // 타일에 타입과 좌표를 넣는다

                tiles[x, y] = tile; // 2차원 배열에 저장
            }
        }
    }


    private GameObject GetTilePrefab(int x, int y)
    {
        bool isEven = (x + y) % 2 == 0; //x + y가 짝수이면 첫 번째 프리팹
        return isEven ? tilePrefab1 : tilePrefab2; // 짝수 칸이면 타일 1, 홀수 칸이면 타일 2 반환
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
