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

    [Header("배경 타일 페인트 설정")]
    // <변경부분> 씬뷰 페인트에 사용할 배경 타일 타입
    [SerializeField] private BackgroundTileType paintTileType = BackgroundTileType.Forest;
    // <변경부분> 좌표 입력 방식으로 변경할 배경 타일 X 좌표
    [SerializeField] private int paintX = 0;
    // <변경부분> 좌표 입력 방식으로 변경할 배경 타일 Y 좌표
    [SerializeField] private int paintY = 0;

    [Header("배경 타일 브러시 설정")]
    // <변경부분> 씬뷰 페인트 시 한 번에 칠할 배경 타일 범위
    [SerializeField] private int brushSize = 1;

    [Header("All 타일 랜덤 비율 설정")]
    // <변경부분> All 타입으로 배경을 생성할 때 섞어서 사용할 실제 타일 비율
    [SerializeField] private List<BackgroundTileWeight> allTileWeights = new List<BackgroundTileWeight>();

    [Header("장식물 기본 설정")]
    // <변경부분> 모든 장식물이 공통으로 사용할 프리팹
    [SerializeField] private GameObject decorationPrefab;
    // <변경부분> 생성된 장식물을 정리해서 담을 부모 오브젝트
    [SerializeField] private Transform decorationParent;

    [Header("장식물 스프라이트 목록")]
    // <변경부분> 장식물 타입별로 사용할 스프라이트 목록
    [SerializeField] private List<DecorationSet> decorationSets = new List<DecorationSet>();

    [Header("장식물 생성 테스트")]
    // <변경부분> 테스트로 생성할 장식물 타입
    [SerializeField] private DecorationType testDecorationType = DecorationType.Tree;
    // <변경부분> 테스트로 장식물을 생성할 배경 타일 X 좌표
    [SerializeField] private int testDecorationX = 0;
    // <변경부분> 테스트로 장식물을 생성할 배경 타일 Y 좌표
    [SerializeField] private int testDecorationY = 0;
    // <변경부분> 장식물이 배경 타일 위에 자연스럽게 올라오도록 위치 보정
    [SerializeField] private Vector3 decorationOffset = Vector3.zero;

    [Header("장식물 색상 설정")]
    // <변경부분> 장식물을 배경과 동일하게 어둡게 처리할지 설정
    [SerializeField]
    private bool useDarkDecoration = true;
    // <변경부분> 장식물 색상 밝기
    [SerializeField]
    [Range(0f, 1f)]
    private float decorationBrightness = 0.65f;

    [Header("장식물 브러시 설정")]
    // <변경부분> 씬뷰에서 장식물 브러시를 사용할 때 배치할 장식물 타입
    [SerializeField] private DecorationType paintDecorationType = DecorationType.Tree;
    // <변경부분> 같은 좌표에 장식물이 중복 생성되지 않도록 제한
    [SerializeField] private bool preventDuplicateDecoration = true;


    // <변경부분> 장식물 타입별 스프라이트 목록을 빠르게 찾기 위한 캐시
    private Dictionary<DecorationType, List<Sprite>> decorationSpriteDictionary;

    // 생성된 배경 타일을 좌표 기준으로 관리
    private BackgroundTile[,] backgroundTiles;

    // <변경부분> 배경 타일 타입별 스프라이트 목록을 빠르게 찾기 위한 캐시
    private Dictionary<BackgroundTileType, List<Sprite>> tileSpriteDictionary;

    private void Awake()
    {
        // 배경 타일 타입별 스프라이트 목록을 준비
        BuildTileSpriteDictionary();

        // <변경부분> 장식물 타입별 스프라이트 목록을 준비
        BuildDecorationSpriteDictionary();
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

        // <변경부분> All 타입은 설정된 비율에 따라 실제 배치 타입으로 변환
        if (tileType == BackgroundTileType.All)
        {
            tileType = GetWeightedRandomTileTypeFromAll();
        }

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

    // <변경부분> All 배경 생성용 비율 설정에서 실제 배치할 타일 타입을 선택
    private BackgroundTileType GetWeightedRandomTileTypeFromAll()
    {
        int totalWeight = 0;

        for (int i = 0; i < allTileWeights.Count; i++)
        {
            BackgroundTileWeight tileWeight = allTileWeights[i];

            if (tileWeight == null)
            {
                continue;
            }

            if (tileWeight.TileType == BackgroundTileType.All)
            {
                continue;
            }

            if (tileWeight.Weight <= 0)
            {
                continue;
            }

            totalWeight += tileWeight.Weight;
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning("All 타일 비율 설정이 비어 있습니다. Forest로 대체합니다.");
            return BackgroundTileType.Forest;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < allTileWeights.Count; i++)
        {
            BackgroundTileWeight tileWeight = allTileWeights[i];

            if (tileWeight == null)
            {
                continue;
            }

            if (tileWeight.TileType == BackgroundTileType.All)
            {
                continue;
            }

            if (tileWeight.Weight <= 0)
            {
                continue;
            }

            currentWeight += tileWeight.Weight;

            if (randomValue < currentWeight)
            {
                return tileWeight.TileType;
            }
        }

        return BackgroundTileType.Forest;
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

        // <변경부분> 선택한 타입에 사용할 스프라이트가 없으면 페인트 중단
        if (!HasTileSprites(tileType))
        {
            Debug.LogWarning($"{tileType} 타입에 연결된 배경 타일 스프라이트가 없습니다.");
            return;
        }

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

        // <변경부분> 같은 좌표에 남아 있는 기존 배경 타일을 모두 제거
        RemoveBackgroundTilesAt(x, y);

        // 선택한 타입의 새 배경 타일 생성
        SpawnBackgroundTile(tileType, x, y);
    }

    // <변경부분> 선택한 배경 타일 타입에 스프라이트가 등록되어 있는지 확인
    public bool HasTileSprites(BackgroundTileType tileType)
    {
        // 에디터에서 변경한 스프라이트 목록을 최신 상태로 갱신
        BuildTileSpriteDictionary();

        if (tileSpriteDictionary == null)
        {
            return false;
        }

        if (!tileSpriteDictionary.ContainsKey(tileType))
        {
            return false;
        }

        return tileSpriteDictionary[tileType] != null &&
               tileSpriteDictionary[tileType].Count > 0;
    }

    // <변경부분> 인스펙터에 입력한 좌표와 타입으로 배경 타일 교체
    public void PaintSelectedTileByInput()
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

    // <변경부분> 같은 배경 좌표에 남아 있는 기존 타일 오브젝트를 모두 제거
    private void RemoveBackgroundTilesAt(int x, int y)
    {
        // 배경 타일 부모가 없으면 제거 중단
        if (backgroundTileParent == null)
        {
            return;
        }

        for (int i = backgroundTileParent.childCount - 1; i >= 0; i--)
        {
            Transform child = backgroundTileParent.GetChild(i);
            BackgroundTile backgroundTile = child.GetComponent<BackgroundTile>();

            if (backgroundTile == null)
            {
                continue;
            }

            // 같은 좌표에 존재하는 배경 타일은 모두 제거
            if (backgroundTile.X == x && backgroundTile.Y == y)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 배열에 남아 있는 같은 좌표 정보도 비움
        if (backgroundTiles != null &&
            x >= 0 && x < backgroundWidth &&
            y >= 0 && y < backgroundHeight)
        {
            backgroundTiles[x, y] = null;
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

    // <변경부분> 장식물 타입별 스프라이트 목록을 Dictionary로 정리
    private void BuildDecorationSpriteDictionary()
    {
        decorationSpriteDictionary = new Dictionary<DecorationType, List<Sprite>>();

        for (int i = 0; i < decorationSets.Count; i++)
        {
            DecorationSet decorationSet = decorationSets[i];

            if (decorationSet == null)
            {
                continue;
            }

            if (!decorationSpriteDictionary.ContainsKey(decorationSet.DecorationType))
            {
                decorationSpriteDictionary.Add(decorationSet.DecorationType, new List<Sprite>());
            }

            for (int j = 0; j < decorationSet.DecorationSprites.Count; j++)
            {
                Sprite sprite = decorationSet.DecorationSprites[j];

                if (sprite == null)
                {
                    continue;
                }

                decorationSpriteDictionary[decorationSet.DecorationType].Add(sprite);
            }
        }
    }

    // <변경부분> 같은 타입 안에서 여러 장식물 스프라이트를 랜덤 선택
    private Sprite GetRandomDecorationSprite(DecorationType decorationType)
    {
        if (decorationSpriteDictionary == null)
        {
            BuildDecorationSpriteDictionary();
        }

        if (!decorationSpriteDictionary.ContainsKey(decorationType))
        {
            return null;
        }

        List<Sprite> sprites = decorationSpriteDictionary[decorationType];

        if (sprites == null || sprites.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, sprites.Count);
        return sprites[randomIndex];
    }

    // <변경부분> 지정한 배경 좌표에 선택한 타입의 장식물을 생성
    public void SpawnDecoration(DecorationType decorationType, int x, int y)
    {
        // <변경부분> 에디터에서 변경한 장식물 스프라이트 목록을 최신 상태로 갱신
        BuildDecorationSpriteDictionary();

        // 장식물 프리팹이 없으면 생성 중단
        if (decorationPrefab == null)
        {
            Debug.LogError("DecorationPrefab이 연결되지 않았습니다.");
            return;
        }

        // 장식물 부모가 없으면 생성 중단
        if (decorationParent == null)
        {
            Debug.LogError("DecorationParent가 연결되지 않았습니다.");
            return;
        }

        // 장식물 타입에 맞는 스프라이트를 가져오기
        Sprite decorationSprite = GetRandomDecorationSprite(decorationType);

        if (decorationSprite == null)
        {
            Debug.LogWarning($"{decorationType} 타입에 연결된 장식물 스프라이트가 없습니다.");
            return;
        }

        // 배경 범위 밖 좌표는 장식물 생성 중단
        if (x < 0 || x >= backgroundWidth || y < 0 || y >= backgroundHeight)
        {
            Debug.LogWarning($"장식물 좌표 ({x}, {y})가 배경 범위를 벗어났습니다.");
            return;
        }

        // <변경부분> 같은 좌표에 장식물이 중복 생성되지 않도록 기존 장식물 제거
        if (preventDuplicateDecoration)
        {
            RemoveDecorationsAt(x, y);
        }


        // 배경 좌표 기준으로 장식물 위치 계산
        Vector3 spawnPosition = GridToWorld(x, y) + decorationOffset;

        // 공통 장식물 프리팹 생성
        GameObject decorationObject = Instantiate(decorationPrefab, spawnPosition, Quaternion.identity, decorationParent);

        // 생성된 장식물 이름을 좌표 기준으로 정리
        decorationObject.name = $"Decoration_{decorationType}_{x}_{y}";

        // 생성된 장식물에 선택된 스프라이트 적용
        SpriteRenderer spriteRenderer = decorationObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = decorationSprite;
        }

        // <변경부분> 장식물을 배경 분위기에 맞게 어둡게 표시
        if (useDarkDecoration)
        {
            float brightness = Mathf.Clamp01(decorationBrightness);

            spriteRenderer.color =
                new Color(
                    brightness,
                    brightness,
                    brightness,
                    1f);
        }
        else
        {
            spriteRenderer.color = Color.white;
        }

        // 장식물 데이터 초기화
        Decoration decoration = decorationObject.GetComponent<Decoration>();

        if (decoration == null)
        {
            decoration = decorationObject.AddComponent<Decoration>();
        }

        decoration.Initialize(decorationType, x, y);

        // 생성된 장식물이 배경 위에 자연스럽게 겹치도록 정렬 순서 적용
        SetDecorationSortingOrder(decorationObject, x, y);
    }

    // <변경부분> 씬뷰에서 클릭한 위치를 기준으로 장식물 생성
    public void PaintDecorationByWorldPosition(Vector3 worldPosition)
    {
        // 씬뷰 클릭 위치가 배경 배열 안에 있는지 확인
        if (!TryGetBackgroundGridPosition(worldPosition, out int gridX, out int gridY))
        {
            return;
        }

        // 현재 선택된 장식물 타입으로 해당 좌표에 장식물 생성
        SpawnDecoration(paintDecorationType, gridX, gridY);
    }

    // <변경부분> 같은 배경 좌표에 이미 존재하는 장식물을 모두 제거
    private void RemoveDecorationsAt(int x, int y)
    {
        // 장식물 부모가 없으면 제거 중단
        if (decorationParent == null)
        {
            return;
        }

        for (int i = decorationParent.childCount - 1; i >= 0; i--)
        {
            Transform child = decorationParent.GetChild(i);
            Decoration decoration = child.GetComponent<Decoration>();

            if (decoration == null)
            {
                continue;
            }

            // 같은 좌표에 존재하는 장식물은 모두 제거
            if (decoration.X == x && decoration.Y == y)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    // <변경부분> 생성된 장식물 전체를 제거
    public void ClearDecorations()
    {
        // 장식물 부모가 없으면 제거 중단
        if (decorationParent == null)
        {
            return;
        }

        for (int i = decorationParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(decorationParent.GetChild(i).gameObject);
        }
    }

    // <변경부분> 장식물이 배경 타일 위에 보이도록 아이소메트릭 정렬 순서 계산
    private void SetDecorationSortingOrder(GameObject decorationObject, int x, int y)
    {
        SpriteRenderer spriteRenderer = decorationObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }

        // 장식물은 배경 타일보다 앞, 전투 타일보다 뒤에 보이도록 정렬
        int decorationBaseOrder = -5000;

        // 아래쪽에 있는 장식물이 위쪽 장식물보다 앞에 보이도록 정렬
        spriteRenderer.sortingOrder = decorationBaseOrder - (x + y);
    }

    // <변경부분> 인스펙터에 입력한 좌표와 타입으로 장식물 생성 테스트
    public void SpawnTestDecoration()
    {
        SpawnDecoration(testDecorationType, testDecorationX, testDecorationY);
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

[System.Serializable]
public class BackgroundTileWeight
{
    // <변경부분> All 생성 시 실제로 배치될 배경 타일 타입
    public BackgroundTileType TileType;

    // <변경부분> 해당 타입이 랜덤 생성에 선택될 비율
    public int Weight = 1;
}

#if UNITY_EDITOR

[CustomEditor(typeof(BackgroundManager))]
public class BackgroundManagerEditor : Editor
{
    // <변경부분> 씬뷰에서 장식물 브러시를 사용할지 저장
    private bool isDecorationPaintMode = false;
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

        if (GUILayout.Button("Generate All Background"))
        {
            manager.GenerateBackground(BackgroundTileType.All);
        }

        if (GUILayout.Button("Clear Background"))
        {
            manager.ClearBackground();
        }

        if (GUILayout.Button("Clear Decorations"))
        {
            manager.ClearDecorations();
        }

        // <변경부분> 입력한 좌표의 배경 타일을 현재 선택한 타입으로 교체
        if (GUILayout.Button("Paint Selected Tile By Input"))
        {
            manager.PaintSelectedTileByInput();
        }

        if (GUILayout.Button("Spawn Test Decoration"))
        {
            manager.SpawnTestDecoration();
        }

        GUILayout.Space(10);

        // <변경부분> 씬뷰에서 직접 배경 타일을 칠할지 선택
        isScenePaintMode = GUILayout.Toggle(isScenePaintMode, "Scene Paint Mode", "Button");

        // <변경부분> 씬뷰에서 직접 장식물을 배치할지 선택
        isDecorationPaintMode = GUILayout.Toggle(isDecorationPaintMode, "Decoration Paint Mode", "Button");
    }



    private void OnSceneGUI(SceneView sceneView)
    {
        // <변경부분> 배경 타일 페인트와 장식물 페인트가 모두 꺼져 있으면 입력을 받지 않음
        if (!isScenePaintMode && !isDecorationPaintMode)
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

            // <변경부분> 장식물 페인트 모드가 켜져 있으면 장식물을 배치
            if (isDecorationPaintMode)
            {
                manager.PaintDecorationByWorldPosition(worldPosition);
            }
            // <변경부분> 배경 타일 페인트 모드가 켜져 있으면 배경 타일을 교체
            else if (isScenePaintMode)
            {
                manager.PaintBackgroundTileByWorldPosition(worldPosition);
            }

            // 에디터 변경 사항을 씬에 저장 가능 상태로 표시
            EditorUtility.SetDirty(manager);

            // 클릭 입력이 오브젝트 선택으로 넘어가지 않도록 차단
            currentEvent.Use();
        }
    }
}

#endif

[System.Serializable]
public class DecorationSet
{
    // 장식물 종류를 구분하는 타입
    public DecorationType DecorationType;

    // 같은 타입 안에서 랜덤으로 사용할 여러 장식물 스프라이트
    public List<Sprite> DecorationSprites = new List<Sprite>();
}

