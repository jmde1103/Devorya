#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;


public class BackgroundManager : MonoBehaviour
{
    [Header("배경 기본 설정")]
    [SerializeField] private int backgroundWidth = 40;
    [SerializeField] private int backgroundHeight = 40;

    [SerializeField] private float xOffset = 0.48f;
    [SerializeField] private float yOffset = 0.24f;

    [Header("배경 위치 설정")]
    // <변경부분> 생성된 배경 전체의 시작 위치를 조정
    [SerializeField] private Vector3 backgroundOriginOffset = Vector3.zero;

    [Header("배경 부모 오브젝트")]
    [SerializeField] private Transform backgroundTileParent;

    [Header("배경 타일 프리팹 목록")]
    [SerializeField] private List<BackgroundTileSet> backgroundTileSets = new List<BackgroundTileSet>();

    [Header("배경 공통 프리팹")]
    // <변경부분> 모든 배경 타일이 공통으로 사용할 프리팹
    [SerializeField] private GameObject backgroundTilePrefab;

    [Header("배경 색상 설정")]
    [SerializeField] private bool useDarkBackground = true;
    [SerializeField] private Color darkBackgroundColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Header("배경 타일 페인트 테스트")]
    // <변경부분> 테스트로 변경할 배경 타일 타입
    [SerializeField] private BackgroundTileType paintTileType = BackgroundTileType.Water;
    // <변경부분> 테스트로 변경할 배경 타일 X 좌표
    [SerializeField] private int paintX = 0;
    // <변경부분> 테스트로 변경할 배경 타일 Y 좌표
    [SerializeField] private int paintY = 0;

    [Header("배경 타일 브러시 설정")]
    // <변경부분> 씬뷰 페인트 시 한 번에 칠할 배경 타일 범위
    [SerializeField] private int brushSize = 1;

    // 생성된 배경 타일을 좌표 기준으로 관리
    private BackgroundTile[,] backgroundTiles;

    // <변경부분> 배경 타일 타입별 스프라이트 목록을 빠르게 찾기 위한 캐시
    private Dictionary<BackgroundTileType, List<Sprite>> tileSpriteDictionary;

    private void Awake()
    {
        // <변경부분> 배경 타일 타입별 스프라이트 목록을 준비
        BuildTileSpriteDictionary();
    }

    // <변경부분> 배경 타일 타입별 스프라이트 목록을 Dictionary로 정리
    private void BuildTileSpriteDictionary()
    {
        tileSpriteDictionary = new Dictionary<BackgroundTileType, List<Sprite>>();

        for (int i = 0; i < backgroundTileSets.Count; i++)
        {
            BackgroundTileSet tileSet = backgroundTileSets[i];

            if (tileSet == null)
            {
                continue;
            }

            if (!tileSpriteDictionary.ContainsKey(tileSet.TileType))
            {
                tileSpriteDictionary.Add(tileSet.TileType, new List<Sprite>());
            }

            for (int j = 0; j < tileSet.TileSprites.Count; j++)
            {
                Sprite sprite = tileSet.TileSprites[j];

                if (sprite == null)
                {
                    continue;
                }

                tileSpriteDictionary[tileSet.TileType].Add(sprite);
            }
        }
    }

    // 배경 전체를 기본 타입으로 생성
    public void GenerateBackground(BackgroundTileType defaultTileType)
    {
        // <변경부분> 에디터 버튼 실행 시에도 최신 스프라이트 목록을 다시 준비
        BuildTileSpriteDictionary();

        // 기존 배경 타일을 모두 제거
        ClearBackground();

        // 배경 타일 배열을 새로 준비
        backgroundTiles = new BackgroundTile[backgroundWidth, backgroundHeight];

        for (int x = 0; x < backgroundWidth; x++)
        {
            for (int y = 0; y < backgroundHeight; y++)
            {
                // 지정한 기본 타입으로 배경 타일 생성
                SpawnBackgroundTile(defaultTileType, x, y);
            }
        }
    }

    // 지정 좌표에 배경 타일 생성
    private void SpawnBackgroundTile(BackgroundTileType tileType, int x, int y)
    {
        // <변경부분> 배경 타일 타입에 맞는 스프라이트를 가져오기
        Sprite tileSprite = GetRandomTileSprite(tileType);



        if (tileSprite == null)
        {
            Debug.LogWarning($"{tileType} 타입에 연결된 배경 타일 스프라이트가 없습니다.");
            return;
        }

        // <변경부분> 공통 배경 타일 프리팹이 없으면 생성 중단
        if (backgroundTilePrefab == null)
        {
            Debug.LogError("BackgroundTilePrefab이 연결되지 않았습니다.");
            return;
        }

        // 아이소메트릭 배경 좌표 계산
        Vector3 spawnPosition = GridToWorld(x, y);

        // 배경 타일 생성
        GameObject tileObject = Instantiate(backgroundTilePrefab, spawnPosition, Quaternion.identity, backgroundTileParent);

        // <변경부분> 생성된 배경 타일에 선택된 스프라이트 적용
        SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = tileSprite;
        }

        // <변경부분> 배경 타일이 아이소메트릭 깊이에 맞게 겹쳐 보이도록 정렬 순서 적용
        SetBackgroundTileSortingOrder(tileObject, x, y);

        // 배경 타일 이름을 좌표 기준으로 정리
        tileObject.name = $"BackgroundTile_{tileType}_{x}_{y}";

        // 배경을 선택적으로 어둡게 표시
        ApplyBackgroundColor(tileObject);

        // 배경 타일 컴포넌트 가져오기
        BackgroundTile backgroundTile = tileObject.GetComponent<BackgroundTile>();

        if (backgroundTile == null)
        {
            backgroundTile = tileObject.AddComponent<BackgroundTile>();
        }

        // 배경 타일 정보 초기화
        backgroundTile.Initialize(tileType, x, y);

        // 생성된 배경 타일을 배열에 저장
        backgroundTiles[x, y] = backgroundTile;
    }

    // <변경부분> 배경 타일의 Y 좌표 기준으로 아이소메트릭 정렬 순서 계산
    private void SetBackgroundTileSortingOrder(GameObject tileObject, int x, int y)
    {
        SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        // 배경 타일이 전투 타일보다 뒤에 보이도록 낮은 정렬 기준 사용
        int backgroundBaseOrder = -10000;

        // 아래쪽에 있는 배경 타일이 위쪽 타일보다 앞에 보이도록 정렬
        spriteRenderer.sortingOrder = backgroundBaseOrder - (x + y);
    }

    // 배경 좌표를 아이소메트릭 월드 좌표로 변환
    private Vector3 GridToWorld(int x, int y)
    {
        // 배경 좌표를 아이소메트릭 월드 좌표로 변환
        float worldX = (x - y) * xOffset;
        float worldY = (x + y) * yOffset;

        // <변경부분> 배경 전체 생성 위치를 원하는 지점으로 이동
        return new Vector3(worldX, worldY, 0f) + backgroundOriginOffset;
    }

    // <변경부분> 같은 타입 안에서 여러 타일 스프라이트를 랜덤 선택
    private Sprite GetRandomTileSprite(BackgroundTileType tileType)
    {
        if (tileSpriteDictionary == null)
        {
            BuildTileSpriteDictionary();
        }

        if (!tileSpriteDictionary.ContainsKey(tileType))
        {
            return null;
        }

        List<Sprite> sprites = tileSpriteDictionary[tileType];

        if (sprites == null || sprites.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, sprites.Count);
        return sprites[randomIndex];
    }

    // 배경 타일에 어두운 색상 옵션 적용
    private void ApplyBackgroundColor(GameObject tileObject)
    {
        if (!useDarkBackground)
        {
            return;
        }

        SpriteRenderer spriteRenderer = tileObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = darkBackgroundColor;
    }

    // 기존에 생성된 배경 타일 전체 제거
    public void ClearBackground()
    {
        if (backgroundTileParent == null)
        {
            return;
        }

        for (int i = backgroundTileParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(backgroundTileParent.GetChild(i).gameObject);
        }
    }

    // <변경부분> 지정한 좌표의 배경 타일을 선택한 타입으로 교체
    public void PaintBackgroundTile(BackgroundTileType tileType, int x, int y)
    {
        // <변경부분> 에디터 스크립트 리로드 후에도 씬에 남은 배경 타일을 다시 배열에 연결
        if (backgroundTiles == null)
        {
            RebuildBackgroundTileArrayFromScene();
        }

        // <변경부분> 복구 후에도 배열이 없으면 페인트 중단
        if (backgroundTiles == null)
        {
            Debug.LogWarning("생성된 배경이 없습니다.");
            return;
        }

        // 배경 범위를 벗어난 좌표는 페인트 중단
        if (x < 0 || x >= backgroundWidth || y < 0 || y >= backgroundHeight)
        {
            Debug.LogWarning($"배경 좌표 ({x}, {y})가 범위를 벗어났습니다.");
            return;
        }

        // 기존 배경 타일이 있으면 제거
        if (backgroundTiles[x, y] != null)
        {
            DestroyImmediate(backgroundTiles[x, y].gameObject);
            backgroundTiles[x, y] = null;
        }

        // 선택한 타입의 새 배경 타일 생성
        SpawnBackgroundTile(tileType, x, y);
    }

    // <변경부분> 인스펙터에 입력한 좌표와 타입으로 배경 타일 교체 테스트
    public void PaintTestTile()
    {
        PaintBackgroundTile(paintTileType, paintX, paintY);
    }

    // <변경부분> 클릭한 배경 타일을 현재 선택된 페인트 타입으로 교체
    public void PaintBackgroundTileFromClick(BackgroundTile clickedTile)
    {
        // 클릭한 배경 타일이 없으면 페인트 중단
        if (clickedTile == null)
        {
            return;
        }

        // 클릭한 배경 타일 좌표를 현재 선택 타입으로 교체
        PaintBackgroundTile(paintTileType, clickedTile.X, clickedTile.Y);
    }

    // <변경부분> 씬뷰에서 클릭한 월드 위치를 배경 배열 좌표로 변환
    public bool TryGetBackgroundGridPosition(Vector3 worldPosition, out int gridX, out int gridY)
    {
        // 배경 시작 위치 보정을 제거해 원본 아이소메트릭 좌표로 변환
        Vector3 localPosition = worldPosition - backgroundOriginOffset;

        float halfX = localPosition.x / xOffset;
        float halfY = localPosition.y / yOffset;

        gridX = Mathf.RoundToInt((halfY + halfX) * 0.5f);
        gridY = Mathf.RoundToInt((halfY - halfX) * 0.5f);

        // 배경 배열 범위 밖이면 페인트 불가 처리
        if (gridX < 0 || gridX >= backgroundWidth || gridY < 0 || gridY >= backgroundHeight)
        {
            return false;
        }

        return true;
    }

    // <변경부분> 씬뷰에서 클릭한 위치를 중심으로 브러시 크기만큼 배경 타일을 교체
    public void PaintBackgroundTileByWorldPosition(Vector3 worldPosition)
    {
        // 씬뷰 클릭 위치가 배경 배열 안에 있는지 확인
        if (!TryGetBackgroundGridPosition(worldPosition, out int centerX, out int centerY))
        {
            return;
        }

        // 브러시 크기가 1보다 작아져도 최소 1칸은 칠해지도록 보정
        int safeBrushSize = Mathf.Max(1, brushSize);

        // 브러시 중심 기준으로 주변 타일을 칠할 범위 계산
        int brushRadius = safeBrushSize - 1;

        for (int x = centerX - brushRadius; x <= centerX + brushRadius; x++)
        {
            for (int y = centerY - brushRadius; y <= centerY + brushRadius; y++)
            {
                // 배경 범위 밖 좌표는 건너뜀
                if (x < 0 || x >= backgroundWidth || y < 0 || y >= backgroundHeight)
                {
                    continue;
                }

                // 현재 선택된 페인트 타입으로 브러시 범위 안의 타일 교체
                PaintBackgroundTile(paintTileType, x, y);
            }
        }
    }

    // <변경부분> 씬에 이미 생성된 배경 타일을 배열 정보로 다시 연결
    private void RebuildBackgroundTileArrayFromScene()
    {
        // 배경 타일 배열을 새로 준비
        backgroundTiles = new BackgroundTile[backgroundWidth, backgroundHeight];

        // 배경 타일 부모가 없으면 복구 중단
        if (backgroundTileParent == null)
        {
            return;
        }

        for (int i = 0; i < backgroundTileParent.childCount; i++)
        {
            BackgroundTile backgroundTile =
                backgroundTileParent.GetChild(i).GetComponent<BackgroundTile>();

            if (backgroundTile == null)
            {
                continue;
            }

            // 배경 범위 밖 타일은 배열에 연결하지 않음
            if (backgroundTile.X < 0 || backgroundTile.X >= backgroundWidth ||
                backgroundTile.Y < 0 || backgroundTile.Y >= backgroundHeight)
            {
                continue;
            }

            // 씬에 남아 있는 배경 타일을 배열 좌표에 다시 연결
            backgroundTiles[backgroundTile.X, backgroundTile.Y] = backgroundTile;
        }
    }
}



[System.Serializable]
public class BackgroundTileSet
{
    // 같은 속성으로 묶을 배경 타일 타입
    public BackgroundTileType TileType;

    // <변경부분> 같은 타입 안에서 랜덤으로 사용할 여러 형태의 타일 스프라이트
    public List<Sprite> TileSprites = new List<Sprite>();
}

#if UNITY_EDITOR

[CustomEditor(typeof(BackgroundManager))]
public class BackgroundManagerEditor : Editor
{
    private bool isScenePaintMode = false;

    private void OnEnable()
    {
        // 씬뷰에서 배경 페인트 입력을 감지
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        // 에디터 선택이 해제되면 씬뷰 입력 감지를 중단
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BackgroundManager manager = (BackgroundManager)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Grass Background"))
        {
            manager.GenerateBackground(BackgroundTileType.Grass);
        }

        if (GUILayout.Button("Clear Background"))
        {
            manager.ClearBackground();
        }

        GUILayout.Space(10);

        // <변경부분> 씬뷰에서 직접 배경 타일을 칠할지 선택
        isScenePaintMode = GUILayout.Toggle(isScenePaintMode, "Scene Paint Mode", "Button");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // 씬 페인트 모드가 꺼져 있으면 입력을 받지 않음
        if (!isScenePaintMode)
        {
            return;
        }

        BackgroundManager manager = (BackgroundManager)target;
        Event currentEvent = Event.current;

        // Alt 입력 중에는 씬뷰 카메라 조작을 우선
        if (currentEvent.alt)
        {
            return;
        }

        // 좌클릭 또는 좌클릭 드래그일 때 배경 타일 페인트
        if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) &&
            currentEvent.button == 0)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

            // 씬뷰 마우스 위치를 2D 월드 좌표로 변환
            Vector3 worldPosition = mouseRay.origin;

            // <변경부분> 클릭한 월드 위치 기준으로 배경 타일 교체
            manager.PaintBackgroundTileByWorldPosition(worldPosition);

            // 에디터 변경 사항을 씬에 저장 가능 상태로 표시
            EditorUtility.SetDirty(manager);

            // 클릭 입력이 오브젝트 선택으로 넘어가지 않도록 차단
            currentEvent.Use();
        }
    }
}

#endif

