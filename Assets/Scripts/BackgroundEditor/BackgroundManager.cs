#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;


public enum BackgroundTileBrushSourceMode
{
    RandomByType = 0,
    ExactSprite = 1,
    ExactPrefab = 2
}


public enum DecorationBrushSourceMode
{
    RandomByType = 0,
    ExactSprite = 1,
    ExactPrefab = 2
}


public enum DecorationPlacementMode
{
    Grid = 0,
    Free = 1
}


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

    // <변경부분>
    // 타입 안에서 랜덤 Sprite를 사용할지,
    // 직접 지정한 Sprite를 사용할지 선택한다.
    [SerializeField]
    private BackgroundTileBrushSourceMode backgroundTileBrushSourceMode =
     BackgroundTileBrushSourceMode.RandomByType;

    // 씬뷰 페인트에 사용할 배경 타일 타입.
    //
    // Exact Sprite 모드에서도 타입 정보 자체는 필요하므로
    // Sprite와 별도로 직접 지정한다.
    [SerializeField]
    private BackgroundTileType paintTileType =
        BackgroundTileType.Forest;

    // <변경부분>
    // Exact Sprite 모드에서 직접 칠할 타일 Sprite.
    [SerializeField]
    private Sprite paintTileSprite;

    // <변경부분>
    // Exact Prefab 모드에서 직접 배치할
    // Background Visual Prefab.
    [SerializeField]
    private GameObject paintTilePrefab;

    [SerializeField]
    private int paintX = 0;

    // 좌표 입력 방식으로 변경할 배경 타일 Y 좌표
    [SerializeField]
    private int paintY = 0;

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

    // <변경부분>
    // 기존 Scene에 저장되어 있던 DecorationSet 데이터를
    // 바로 삭제하지 않고 Legacy fallback으로 보존한다.
    //
    // BackgroundMapData에 DecorationPalette가 연결되어 있으면
    // Palette 데이터를 우선 사용한다.
    [SerializeField, HideInInspector]
    private List<DecorationSet> decorationSets =
     new List<DecorationSet>();


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

    // <변경부분>
    // 타입 랜덤 / 정확한 Sprite 중 어떤 소스를 사용할지 선택
    [SerializeField]
    private DecorationBrushSourceMode decorationBrushSourceMode =
    DecorationBrushSourceMode.RandomByType;

    // <변경부분>
    // 씬뷰에서 장식물 브러시를 사용할 때 배치할 장식물 타입
    [SerializeField]
    private DecorationType paintDecorationType =
        DecorationType.Tree;

    // Exact Sprite 모드에서 직접 배치할 장식물 Sprite
    [SerializeField]
    private Sprite paintDecorationSprite;

    // <변경부분>
    // Exact Prefab 모드에서 직접 배치할
    // Decoration Visual Prefab.
    [SerializeField]
    private GameObject paintDecorationPrefab;


    [SerializeField]
    [Range(-3, 3)]
    private int paintDecorationLayerOffset = 0;


    // Grid 중심 배치 / Anchor Tile 기준 자유 배치 선택
    [SerializeField]
    private DecorationPlacementMode decorationPlacementMode =
        DecorationPlacementMode.Grid;

    // <변경부분>
    // Free Placement에서 Scene 클릭으로 지정한 기준 타일
    [SerializeField, HideInInspector]
    private bool hasFreePlacementAnchor = false;

    [SerializeField, HideInInspector]
    private int freePlacementAnchorX = 0;

    [SerializeField, HideInInspector]
    private int freePlacementAnchorY = 0;

    // <변경부분>
    // Grid Placement에서 같은 좌표에 장식물이 중복 생성되지 않도록 제한
    [SerializeField]
    private bool preventDuplicateDecoration = true;

    [Header("장식물 자동 생성 규칙")]
    // <변경부분> 배경 타일 타입별로 자동 생성할 장식물 규칙
    [SerializeField] private List<DecorationSpawnRule> decorationSpawnRules = new List<DecorationSpawnRule>();

    [Header("배경 맵 데이터 저장/불러오기")]
    // 현재 배경과 장식물 배치를 저장하거나 불러올 데이터 에셋
    [SerializeField] private BackgroundMapData currentMapData;


    [Header("Environment Lighting")]

    [SerializeField]
    private EnvironmentLightingController
     environmentLightingController;


 
    // <변경부분>
    [Header("Environment Cloud Shadow")]

    [SerializeField]
    private CloudShadowController
        cloudShadowController;


    [Header("Environment Fireflies")]

    [SerializeField]
    private FireflyEnvironmentController
        fireflyEnvironmentController;


    [Header("맵 데이터 자동 생성 설정")]
    // 새 맵 데이터 에셋을 만들 때 사용할 파일 이름
    [SerializeField] private string newMapDataName = "NewBackgroundMapData";

    // <변경부분> 새 맵 데이터 에셋이 저장될 폴더 경로
    [SerializeField] private string mapDataSaveFolder = "Assets/Devorya/BackgroundMaps";


    // <변경부분> 장식물 타입별 스프라이트 목록을 빠르게 찾기 위한 캐시
    private Dictionary<DecorationType, List<Sprite>> decorationSpriteDictionary;

    // 생성된 배경 타일을 좌표 기준으로 관리
    private BackgroundTile[,] backgroundTiles;

    // <변경부분> 배경 타일 타입별 스프라이트 목록을 빠르게 찾기 위한 캐시
    private Dictionary<BackgroundTileType, List<Sprite>> tileSpriteDictionary;


    // <변경부분>
    // Standalone Event Scene에서 Decoration과 Event Actor가
    // 동일한 아이소메트릭 깊이 공간 안에서 서로 앞뒤로 교차할 수 있도록
    // 공용 World Sorting 기준을 정의한다.
    //
    // Background Tile은 기존 -10000 영역,
    // Battle Tile / Battle Piece는 0 / 100 영역을 사용하므로
    // Event World는 기존 Decoration 영역인 -5000대를 그대로 사용한다.
    private const int EventWorldBaseSortingOrder =
        -5000;

    // 한 Grid Depth마다 Actor / Decoration 두 칸을 확보한다.
    //
    // 예:
    // Actor      = depth 기준값
    // Decoration = depth 기준값 + 1
    //
    // 따라서 같은 타일에서는 Decoration이 Actor보다 앞에 보이지만,
    // Actor가 화면 아래쪽 타일로 이동하면 자연스럽게 Decoration 앞으로 나온다.
    // <변경부분>
    // 하나의 Grid Depth 안에서
    // Actor와 여러 Decoration Layer가 서로 충돌하지 않도록
    // Sorting Order 공간을 10칸씩 확보한다.
    //
    // 한 Grid 기준:
    //
    // Actor              = +0
    //
    // Decoration Layer
    // -3                 = +2
    // -2                 = +3
    // -1                 = +4
    //  0                 = +5
    // +1                 = +6
    // +2                 = +7
    // +3                 = +8
    //
    // 다음 Grid Depth는 10칸 아래에서 시작하므로
    // 서로 다른 Grid의 Sorting 영역이 겹치지 않는다.
    private const int EventWorldDepthStep =
        10;


    // 같은 Grid에 있는 Event Actor의 기본 위치
    private const int EventActorSortingOffset =
        0;


    // <변경부분>
    // Decoration Layer 0의 기본 Sorting 위치.
    private const int DecorationSortingBaseOffset =
        5;


    // <변경부분>
    // 동일 Anchor 내부에서 사용할 수 있는
    // Decoration Layer 최소/최대 범위.
    private const int DecorationLayerOffsetMin =
        -3;

    private const int DecorationLayerOffsetMax =
        3;


    private void Awake()
    {
        BuildTileSpriteDictionary();

        BuildDecorationSpriteDictionary();
    }


    // <변경부분>
    // Scene에 이미 BackgroundMapData가 연결된 상태로 Play가 시작될 경우에도
    // 현재 Map의 EnvironmentVisualProfile을 Runtime Controller들에 적용한다.
    //
    // Battle/Event 진행 중 LoadMapFromData()로 다른 Map을 불러오는 경우에는
    // 해당 함수에서 다시 Profile을 적용하므로 여기서는 초기 Scene 상태만 처리한다.
    private void Start()
    {
        if (currentMapData == null)
        {
            return;
        }


        ApplyCurrentLightingProfile();
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
        // <변경부분> 실제 플레이 중일 때만 에디터용 배경 생성 기능을 막음
        if (Application.isPlaying && !Application.isEditor)
        {
            Debug.LogWarning("플레이 모드 중에는 배경 타일을 생성할 수 없습니다.");
            return;
        }

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

    private void SpawnBackgroundTile(
     BackgroundTileType tileType,
     int x,
     int y,
     Sprite savedTileSprite = null,
     GameObject savedTilePrefab = null)
    {
        if (x < 0 ||
            x >= backgroundWidth ||
            y < 0 ||
            y >= backgroundHeight)
        {
            Debug.LogWarning(
                $"배경 타일 생성 건너뜀: " +
                $"좌표 ({x}, {y})가 " +
                $"현재 배경 범위 " +
                $"({backgroundWidth} x {backgroundHeight})를 " +
                $"벗어났습니다."
            );

            return;
        }


        // All은 실제 타일 타입이 아니므로
        // 실제 배치 가능한 타일 타입으로 변환한다.
        BackgroundTileType actualTileType =
            GetActualBackgroundTileType(
                tileType
            );


        // <변경부분>
        // 저장된 Sprite가 있으면 그대로 사용한다.
        //
        // 새 맵 생성 또는 기존 구형 데이터처럼
        // 저장 Sprite가 없는 경우에만 랜덤으로 선택한다.
        bool usePrefab =
    savedTilePrefab != null;


        Sprite tileSprite =
            null;


        // Prefab이 없는 경우에만
        // 기존 Sprite 경로를 사용한다.
        if (!usePrefab)
        {
            tileSprite =
                savedTileSprite != null
                    ? savedTileSprite
                    : GetRandomTileSprite(
                        actualTileType
                    );


            if (tileSprite == null)
            {
                Debug.LogWarning(
                    $"{actualTileType} 타입에 사용할 배경 타일 Sprite가 없습니다."
                );

                return;
            }
        }


        if (backgroundTilePrefab == null)
        {
            Debug.LogError(
                "BackgroundTilePrefab이 연결되지 않았습니다."
            );

            return;
        }


        if (backgroundTileParent == null)
        {
            Debug.LogError(
                "BackgroundTileParent가 연결되지 않았습니다."
            );

            return;
        }


        Vector3 spawnPosition =
            GridToWorld(
                x,
                y
            );


        GameObject tileObject =
            Instantiate(
                backgroundTilePrefab,
                spawnPosition,
                Quaternion.identity,
                backgroundTileParent
            );


        BackgroundTile backgroundTile =
            tileObject.GetComponent<BackgroundTile>();

        if (backgroundTile == null)
        {
            backgroundTile =
                tileObject.AddComponent<BackgroundTile>();
        }


        backgroundTile.Initialize(
            actualTileType,
            x,
            y
        );


        if (usePrefab)
        {
            ApplyBackgroundTilePrefabVisual(
                backgroundTile,
                actualTileType,
                x,
                y,
                savedTilePrefab
            );
        }
        else
        {
            ApplyBackgroundTileVisual(
                backgroundTile,
                actualTileType,
                x,
                y,
                tileSprite
            );
        }


        if (backgroundTiles != null)
        {
            backgroundTiles[x, y] =
                backgroundTile;
        }
    }

    // <변경부분> 요청된 배경 타일 타입을 실제 배치 가능한 타입으로 변환
    private BackgroundTileType GetActualBackgroundTileType(BackgroundTileType requestedTileType)
    {
        // All은 직접 배치되는 타일이 아니라 비율 랜덤 규칙으로만 사용
        if (requestedTileType == BackgroundTileType.All)
        {
            return GetWeightedRandomTileTypeFromAll();
        }

        return requestedTileType;
    }

    // =========================================================
    // Background / Decoration Visual 공통 관리
    // =========================================================

    // Sprite / Prefab 출처를 저장하는
    // BackgroundVisualInstance를 가져오거나 생성한다.
    private BackgroundVisualInstance GetOrAddVisualInstance(
        GameObject owner)
    {
        if (owner == null)
        {
            return null;
        }


        BackgroundVisualInstance visualInstance =
            owner.GetComponent<BackgroundVisualInstance>();

        if (visualInstance == null)
        {
            visualInstance =
                owner.AddComponent<BackgroundVisualInstance>();
        }


        return visualInstance;
    }


    // 이전에 생성된 Prefab Visual 자식을 제거한다.
    private void ClearSpawnedVisual(
        GameObject owner)
    {
        if (owner == null)
        {
            return;
        }


        BackgroundVisualInstance visualInstance =
            owner.GetComponent<BackgroundVisualInstance>();


        if (visualInstance != null &&
            visualInstance.SpawnedVisualRoot != null)
        {
            DestroyBackgroundObject(
                visualInstance.SpawnedVisualRoot
            );

            visualInstance.ClearSpawnedVisualReference();

            return;
        }


        // 이전 버전이나 Script Reload 등으로
        // 직접 참조를 잃은 경우 이름을 기준으로 안전하게 정리한다.
        for (int i =
                 owner.transform.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                owner.transform.GetChild(i);

            if (child == null)
            {
                continue;
            }


            if (!child.name.StartsWith(
                    "__BackgroundVisual_"))
            {
                continue;
            }


            DestroyBackgroundObject(
                child.gameObject
            );
        }


        if (visualInstance != null)
        {
            visualInstance.ClearSpawnedVisualReference();
        }
    }


    // Prefab 내부 SpriteRenderer의 상대 Sorting Order를 유지하면서
    // BackgroundManager가 계산한 World Sorting 영역에 배치한다.
    private void ApplyPrefabSpriteRendererSettings(
        GameObject visualRoot,
        SpriteRenderer anchorRenderer,
        int baseSortingOrder,
        Color colorMultiplier)
    {
        if (visualRoot == null ||
            anchorRenderer == null)
        {
            return;
        }


        SpriteRenderer[] renderers =
            visualRoot.GetComponentsInChildren<SpriteRenderer>(
                true
            );


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                renderers[i];

            if (renderer == null)
            {
                continue;
            }


            // Prefab 안에서 설정한 상대 Order는 유지한다.
            int relativeSortingOrder =
                renderer.sortingOrder;


            // Event World와 동일한 Sorting Layer를 사용한다.
            renderer.sortingLayerID =
                anchorRenderer.sortingLayerID;


            renderer.sortingOrder =
                baseSortingOrder +
                relativeSortingOrder;


            // Prefab이 원래 가지고 있던 색상은 유지하면서
            // 현재 Background/Decoration 밝기만 곱한다.
            Color originalColor =
                renderer.color;

            renderer.color =
                new Color(
                    originalColor.r *
                    colorMultiplier.r,

                    originalColor.g *
                    colorMultiplier.g,

                    originalColor.b *
                    colorMultiplier.b,

                    originalColor.a *
                    colorMultiplier.a
                );
        }
    }

    // <변경부분> 배경 타일 오브젝트를 유지한 채 외형과 데이터 표시를 갱신
    private void ApplyBackgroundTileVisual(
     BackgroundTile backgroundTile,
     BackgroundTileType tileType,
     int x,
     int y,
     Sprite tileSprite)
    {
        if (backgroundTile == null)
        {
            return;
        }


        GameObject tileObject =
            backgroundTile.gameObject;


        // 이전에 Prefab 방식으로 사용했던 Visual이 있다면 제거한다.
        ClearSpawnedVisual(
            tileObject
        );


        SpriteRenderer spriteRenderer =
            tileObject.GetComponent<SpriteRenderer>();


        if (spriteRenderer != null)
        {
            spriteRenderer.sprite =
                tileSprite;

            spriteRenderer.color =
                Color.white;
        }


        BackgroundVisualInstance visualInstance =
            GetOrAddVisualInstance(
                tileObject
            );


        if (visualInstance != null)
        {
            visualInstance.SetSpriteSource(
                tileSprite
            );
        }


        ApplyBackgroundColor(
            tileObject
        );


        tileObject.name =
            $"BackgroundTile_{tileType}_{x}_{y}";


        SetBackgroundTileSortingOrder(
            tileObject,
            x,
            y
        );
    }

    // <변경부분>
    // Sprite 대신 Prefab 자체를 Background Tile Visual로 사용한다.
    private void ApplyBackgroundTilePrefabVisual(
        BackgroundTile backgroundTile,
        BackgroundTileType tileType,
        int x,
        int y,
        GameObject visualPrefab)
    {
        if (backgroundTile == null ||
            visualPrefab == null)
        {
            return;
        }


        GameObject tileObject =
            backgroundTile.gameObject;


        ClearSpawnedVisual(
            tileObject
        );


        SpriteRenderer anchorRenderer =
            tileObject.GetComponent<SpriteRenderer>();


        if (anchorRenderer == null)
        {
            Debug.LogError(
                "BackgroundTilePrefab에 SpriteRenderer가 없습니다."
            );

            return;
        }


        // 공통 Prefab의 SpriteRenderer는
        // Sorting 기준점으로만 남긴다.
        anchorRenderer.sprite =
            null;

        anchorRenderer.color =
            Color.white;


        SetBackgroundTileSortingOrder(
            tileObject,
            x,
            y
        );


        GameObject visualRoot =
            Instantiate(
                visualPrefab,
                tileObject.transform,
                false
            );


        visualRoot.name =
            $"__BackgroundVisual_{visualPrefab.name}";


        BackgroundVisualInstance visualInstance =
            GetOrAddVisualInstance(
                tileObject
            );


        if (visualInstance != null)
        {
            visualInstance.SetPrefabSource(
                visualPrefab,
                visualRoot
            );
        }


        Color colorMultiplier =
            useDarkBackground
                ? darkBackgroundColor
                : Color.white;


        ApplyPrefabSpriteRendererSettings(
            visualRoot,
            anchorRenderer,
            anchorRenderer.sortingOrder,
            colorMultiplier
        );


        tileObject.name =
            $"BackgroundTile_{tileType}_{x}_{y}";
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

    // <변경부분> BackgroundManager에서 생성한 오브젝트를
    // Edit Mode와 Play Mode 모두에서 안전하게 제거한다.
    //
    // Edit Mode:
    // 즉시 Scene 편집 결과에 반영해야 하므로 DestroyImmediate 사용.
    //
    // Play Mode:
    // Unity 런타임 규칙에 맞게 Destroy를 사용하고,
    // 같은 프레임에 새 맵이 생성될 때 기존 오브젝트가
    // 잠시 겹쳐 보이지 않도록 먼저 비활성화한다.
    private void DestroyBackgroundObject(
        GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            targetObject.SetActive(
                false
            );

            Destroy(
                targetObject
            );

            return;
        }

        DestroyImmediate(
            targetObject
        );
    }

    // 기존에 생성된 배경 타일 전체 제거
    public void ClearBackground()
    {
        if (backgroundTileParent == null)
        {
            backgroundTiles =
                null;

            return;
        }

        // <변경부분> Editor 제작 작업뿐 아니라
        // 실제 BattleScene 런타임에서도 기존 배경을 제거할 수 있게 한다.
        //
        // StageBattleData의 BackgroundMapData를 새로 불러오기 전에
        // Scene에 저장되어 있던 기존 배경을 먼저 정리한다.
        for (int i =
                 backgroundTileParent.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject backgroundObject =
                backgroundTileParent
                    .GetChild(i)
                    .gameObject;

            DestroyBackgroundObject(
                backgroundObject
            );
        }

        // 배경 타일 삭제 후 배열 정보도 초기화한다.
        backgroundTiles =
            null;
    }

    // <변경부분>
    // 지정 좌표에 존재하는 BackgroundTile을 반환한다.
    //
    // 기존 Background Editor뿐 아니라
    // Event Scene Actor 배치에서도 동일한 좌표 조회를 재사용한다.
    public BackgroundTile GetBackgroundTileAt(
        int x,
        int y)
    {
        if (backgroundTileParent == null)
        {
            return null;
        }

        // <변경부분>
        // Runtime에서 BackgroundMapData를 불러온 뒤에는
        // 좌표 배열이 이미 구성되어 있으므로 우선 배열을 사용한다.
        if (backgroundTiles != null &&
            x >= 0 &&
            x < backgroundWidth &&
            y >= 0 &&
            y < backgroundHeight)
        {
            BackgroundTile cachedTile =
                backgroundTiles[x, y];

            if (cachedTile != null)
            {
                return cachedTile;
            }
        }

        // Editor Script Reload 등으로 배열이 없는 경우에는
        // 기존 Scene Object 탐색 방식으로 fallback한다.
        for (int i = 0;
             i < backgroundTileParent.childCount;
             i++)
        {
            BackgroundTile tile =
                backgroundTileParent
                    .GetChild(i)
                    .GetComponent<BackgroundTile>();

            if (tile == null)
            {
                continue;
            }

            if (tile.X == x &&
                tile.Y == y)
            {
                return tile;
            }
        }

        return null;
    }

    // <변경부분> 같은 좌표에 남아 있는 중복 배경 타일을 기준 타일 하나만 남기고 제거
    private void RemoveExtraBackgroundTilesAt(int x, int y, BackgroundTile tileToKeep)
    {
        if (backgroundTileParent == null)
        {
            return;
        }

        for (int i = backgroundTileParent.childCount - 1; i >= 0; i--)
        {
            Transform child = backgroundTileParent.GetChild(i);
            BackgroundTile tile = child.GetComponent<BackgroundTile>();

            if (tile == null)
            {
                continue;
            }

            if (tile.X != x || tile.Y != y)
            {
                continue;
            }

            if (tileToKeep != null &&
    tile == tileToKeep)
            {
                continue;
            }

            // <변경부분> Editor / Runtime 공용 삭제 함수를 사용한다.
            DestroyBackgroundObject(
                child.gameObject
            );
        }
    }
    // 지정한 좌표의 배경 타일을 선택한 타입으로 교체.
    //
    // 기존 외부 호출은 계속 타입 랜덤 방식으로 동작한다.
    public void PaintBackgroundTile(
        BackgroundTileType tileType,
        int x,
        int y)
    {
        PaintBackgroundTileInternal(
    tileType,
    x,
    y,
    null,
    null
);
    }


    // <변경부분>
    // 타일 페인트의 실제 공통 처리.
    //
    // exactTileSprite가 null이면:
    // 해당 TileType 안에서 기존처럼 랜덤 Sprite를 선택한다.
    //
    // exactTileSprite가 있으면:
    // 지정된 Sprite를 그대로 사용한다.
    private void PaintBackgroundTileInternal(
     BackgroundTileType tileType,
     int x,
     int y,
     Sprite exactTileSprite,
     GameObject exactTilePrefab)
    {
        // All은 실제 저장용 TileType이 아니므로
        // 랜덤 규칙을 통해 실제 타입으로 변환한다.
        BackgroundTileType actualTileType =
            GetActualBackgroundTileType(
                tileType
            );


        bool usePrefab =
     exactTilePrefab != null;


        Sprite resolvedTileSprite =
            exactTileSprite;


        if (!usePrefab &&
            resolvedTileSprite == null)
        {
            if (!HasTileSprites(
                    actualTileType))
            {
                Debug.LogWarning(
                    $"{actualTileType} 타입에 연결된 배경 타일 스프라이트가 없습니다."
                );

                return;
            }


            resolvedTileSprite =
                GetRandomTileSprite(
                    actualTileType
                );
        }


        if (!usePrefab &&
            resolvedTileSprite == null)
        {
            Debug.LogWarning(
                $"{actualTileType} 타입에 사용할 배경 타일 Sprite가 없습니다."
            );

            return;
        }


        // 에디터 스크립트 리로드 후에도
        // Scene에 남아 있는 배경 타일을 다시 배열에 연결한다.
        if (backgroundTiles == null)
        {
            RebuildBackgroundTileArrayFromScene();
        }


        if (backgroundTiles == null)
        {
            Debug.LogWarning(
                "생성된 배경이 없습니다."
            );

            return;
        }


        if (x < 0 ||
            x >= backgroundWidth ||
            y < 0 ||
            y >= backgroundHeight)
        {
            Debug.LogWarning(
                $"배경 좌표 ({x}, {y})가 범위를 벗어났습니다."
            );

            return;
        }


        BackgroundTile existingTile =
            GetBackgroundTileAt(
                x,
                y
            );


        if (existingTile != null)
        {
            RemoveExtraBackgroundTilesAt(
                x,
                y,
                existingTile
            );


            existingTile.ChangeTileType(
                actualTileType
            );


            // <변경부분>
            // Random / Exact 어느 방식이든
            // 위에서 결정된 최종 Sprite를 그대로 적용한다.
            if (usePrefab)
            {
                ApplyBackgroundTilePrefabVisual(
                    existingTile,
                    actualTileType,
                    x,
                    y,
                    exactTilePrefab
                );
            }
            else
            {
                ApplyBackgroundTileVisual(
                    existingTile,
                    actualTileType,
                    x,
                    y,
                    resolvedTileSprite
                );
            }


            backgroundTiles[x, y] =
                existingTile;

            return;
        }


        // 기존 타일이 없는 좌표에서도
        // 이미 결정된 Sprite를 그대로 사용해 새 타일을 생성한다.
        SpawnBackgroundTile(
    actualTileType,
    x,
    y,
    resolvedTileSprite,
    exactTilePrefab
);
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

    // <변경부분>
    // Inspector에서 지정한 좌표에 현재 Tile Brush 설정으로 페인트한다.
    public void PaintSelectedTileByInput()
    {
        if (backgroundTileBrushSourceMode ==
            BackgroundTileBrushSourceMode.ExactSprite)
        {
            if (paintTileType ==
                BackgroundTileType.All)
            {
                Debug.LogWarning(
                    "Exact Sprite 모드에서는 All이 아니라 실제 Tile Type을 지정해주세요."
                );

                return;
            }


            if (paintTileSprite == null)
            {
                Debug.LogWarning(
                    "Exact Sprite 모드에서 사용할 Tile Sprite가 지정되지 않았습니다."
                );

                return;
            }


            PaintBackgroundTileInternal(
                paintTileType,
                paintX,
                paintY,
                paintTileSprite,
                null
            );

            return;
        }


        if (backgroundTileBrushSourceMode ==
            BackgroundTileBrushSourceMode.ExactPrefab)
        {
            if (paintTileType ==
                BackgroundTileType.All)
            {
                Debug.LogWarning(
                    "Exact Prefab 모드에서는 All이 아니라 실제 Tile Type을 지정해주세요."
                );

                return;
            }


            if (paintTilePrefab == null)
            {
                Debug.LogWarning(
                    "Exact Prefab 모드에서 사용할 Tile Prefab이 지정되지 않았습니다."
                );

                return;
            }


            PaintBackgroundTileInternal(
                paintTileType,
                paintX,
                paintY,
                null,
                paintTilePrefab
            );

            return;
        }


        PaintBackgroundTile(
            paintTileType,
            paintX,
            paintY
        );
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

    // <변경부분>
    // Scene View에서 클릭한 위치를 중심으로
    // 현재 Tile Brush 설정에 따라 배경 타일을 교체한다.
    public void PaintBackgroundTileByWorldPosition(
        Vector3 worldPosition)
    {
        if (!TryGetBackgroundGridPosition(
                worldPosition,
                out int centerX,
                out int centerY))
        {
            return;
        }


        bool useExactSprite =
      backgroundTileBrushSourceMode ==
      BackgroundTileBrushSourceMode.ExactSprite;

        bool useExactPrefab =
            backgroundTileBrushSourceMode ==
            BackgroundTileBrushSourceMode.ExactPrefab;

        if (useExactPrefab)
        {
            if (paintTileType ==
                BackgroundTileType.All)
            {
                Debug.LogWarning(
                    "Exact Prefab 모드에서는 All이 아니라 실제 Tile Type을 지정해주세요."
                );

                return;
            }


            if (paintTilePrefab == null)
            {
                Debug.LogWarning(
                    "Exact Prefab 모드에서 사용할 Tile Prefab이 지정되지 않았습니다."
                );

                return;
            }
        }


        if (useExactSprite)
        {
            if (paintTileType ==
                BackgroundTileType.All)
            {
                Debug.LogWarning(
                    "Exact Sprite 모드에서는 All이 아니라 실제 Tile Type을 지정해주세요."
                );

                return;
            }


            if (paintTileSprite == null)
            {
                Debug.LogWarning(
                    "Exact Sprite 모드에서 사용할 Tile Sprite가 지정되지 않았습니다."
                );

                return;
            }
        }


        int safeBrushSize =
            Mathf.Max(
                1,
                brushSize
            );


        int brushRadius =
            safeBrushSize - 1;


        for (int x =
                 centerX - brushRadius;
             x <=
                 centerX + brushRadius;
             x++)
        {
            for (int y =
                     centerY - brushRadius;
                 y <=
                     centerY + brushRadius;
                 y++)
            {
                if (x < 0 ||
                    x >= backgroundWidth ||
                    y < 0 ||
                    y >= backgroundHeight)
                {
                    continue;
                }


                if (useExactSprite)
                {
                    PaintBackgroundTileInternal(
                        paintTileType,
                        x,
                        y,
                        paintTileSprite,
                        null
                    );
                }
                else if (useExactPrefab)
                {
                    PaintBackgroundTileInternal(
                        paintTileType,
                        x,
                        y,
                        null,
                        paintTilePrefab
                    );
                }
                else
                {
                    PaintBackgroundTile(
                        paintTileType,
                        x,
                        y
                    );
                }
            }
        }
    }

    // <변경부분> 씬에 이미 생성된 배경 타일을 배열 정보로 다시 연결
    private void RebuildBackgroundTileArrayFromScene()
    {
        backgroundTiles =
            new BackgroundTile[
                backgroundWidth,
                backgroundHeight
            ];

        if (backgroundTileParent == null)
        {
            return;
        }

        for (int i = 0;
             i < backgroundTileParent.childCount;
             i++)
        {
            BackgroundTile backgroundTile =
                backgroundTileParent
                    .GetChild(i)
                    .GetComponent<BackgroundTile>();

            if (backgroundTile == null)
            {
                continue;
            }

            if (backgroundTile.X < 0 ||
                backgroundTile.X >= backgroundWidth ||
                backgroundTile.Y < 0 ||
                backgroundTile.Y >= backgroundHeight)
            {
                Debug.LogWarning(
                    $"{backgroundTile.name}의 배경 좌표가 범위를 벗어났습니다. " +
                    $"삭제하지 않고 건너뜁니다."
                );

                continue;
            }

            if (backgroundTiles[
                    backgroundTile.X,
                    backgroundTile.Y
                ] != null)
            {
                Debug.LogWarning(
                    $"배경 좌표 " +
                    $"({backgroundTile.X}, {backgroundTile.Y})에 " +
                    $"중복 타일이 있습니다. " +
                    $"삭제하지 않고 건너뜁니다."
                );

                continue;
            }

            backgroundTiles[
                backgroundTile.X,
                backgroundTile.Y
            ] =
                backgroundTile;
        }
    }


    // <변경부분>
    // 현재 BackgroundMapData에 DecorationPalette가 연결되어 있으면
    // 해당 Palette의 DecorationSet 목록을 사용한다.
    //
    // 아직 Palette가 연결되지 않은 기존 맵은
    // BackgroundManager에 직렬화되어 있던 Legacy 목록을 사용한다.
    private List<DecorationSet> GetActiveDecorationSets()
    {
        if (currentMapData != null &&
            currentMapData.DecorationPalette != null &&
            currentMapData.DecorationPalette.DecorationSets != null)
        {
            return
                currentMapData
                    .DecorationPalette
                    .DecorationSets;
        }

        return decorationSets;
    }


    // <변경부분>
    // DecorationType에 대응하는 현재 DecorationSet을 반환한다.
    //
    // Occlusion 설정도 DecorationSet에서 관리하므로
    // Sprite 목록과 동일한 활성 Palette 데이터를 사용한다.
    private DecorationSet GetDecorationSet(
        DecorationType decorationType)
    {
        List<DecorationSet> activeDecorationSets =
            GetActiveDecorationSets();

        if (activeDecorationSets == null)
        {
            return null;
        }

        for (int i = 0;
             i < activeDecorationSets.Count;
             i++)
        {
            DecorationSet decorationSet =
                activeDecorationSets[i];

            if (decorationSet == null)
            {
                continue;
            }

            if (decorationSet.DecorationType ==
                decorationType)
            {
                return decorationSet;
            }
        }

        return null;
    }


    // <변경부분>
    // 지정한 Event Actor Grid가
    // 이 Decoration의 가림 영향권 안에 있는지 확인한다.
    //
    // Decoration 자신의 배치 타일은 항상 영향권으로 처리하고,
    // 그 외의 타일은 DecorationSet.ActorOcclusionOffsets를 사용한다.
    private bool IsGridInsideDecorationOcclusion(
        Decoration decoration,
        DecorationSet decorationSet,
        int actorGridX,
        int actorGridY)
    {
        if (decoration == null)
        {
            return false;
        }

        // Decoration 자신의 타일은 항상 Actor를 가릴 수 있다.
        if (actorGridX == decoration.X &&
            actorGridY == decoration.Y)
        {
            return true;
        }

        if (decorationSet == null ||
            decorationSet.ActorOcclusionOffsets == null)
        {
            return false;
        }

        for (int i = 0;
             i < decorationSet.ActorOcclusionOffsets.Count;
             i++)
        {
            Vector2Int offset =
                decorationSet.ActorOcclusionOffsets[i];

            int occlusionGridX =
                decoration.X +
                offset.x;

            int occlusionGridY =
                decoration.Y +
                offset.y;

            if (actorGridX == occlusionGridX &&
                actorGridY == occlusionGridY)
            {
                return true;
            }
        }

        return false;
    }


    // <변경부분>
    // 현재 Map의 DecorationPalette를 기준으로
    // 장식물 타입별 Sprite 목록을 Dictionary로 정리한다.
    //
    // Palette가 없는 기존 맵은 Legacy decorationSets를 사용한다.
    private void BuildDecorationSpriteDictionary()
    {
        decorationSpriteDictionary =
            new Dictionary<
                DecorationType,
                List<Sprite>
            >();


        List<DecorationSet> activeDecorationSets =
            GetActiveDecorationSets();

        if (activeDecorationSets == null)
        {
            return;
        }


        for (int i = 0;
             i < activeDecorationSets.Count;
             i++)
        {
            DecorationSet decorationSet =
                activeDecorationSets[i];

            if (decorationSet == null)
            {
                continue;
            }


            if (!decorationSpriteDictionary.ContainsKey(
                    decorationSet.DecorationType))
            {
                decorationSpriteDictionary.Add(
                    decorationSet.DecorationType,
                    new List<Sprite>()
                );
            }


            if (decorationSet.DecorationSprites == null)
            {
                continue;
            }


            for (int j = 0;
                 j < decorationSet.DecorationSprites.Count;
                 j++)
            {
                Sprite sprite =
                    decorationSet.DecorationSprites[j];

                if (sprite == null)
                {
                    continue;
                }


                decorationSpriteDictionary[
                        decorationSet.DecorationType
                    ]
                    .Add(
                        sprite
                    );
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

    // <변경부분>
    // 지정한 배경 좌표에 선택한 타입의 장식물을 랜덤 생성.
    //
    // 기존 자동 생성 / 테스트 생성 경로는 그대로 유지한다.
    public void SpawnDecoration(
       DecorationType decorationType,
       int x,
       int y)
    {
        // 자동 생성이나 기존 랜덤 생성은
        // Sprite 랜덤 방식 그대로 사용한다.
        // Prefab은 사용하지 않으므로 null을 전달한다.
        SpawnDecorationInternal(
            decorationType,
            x,
            y,
            null,
            null,
            Vector3.zero,
            0,
            preventDuplicateDecoration
        );
    }


    // <변경부분>
    // 실제 장식물 생성 공통 경로.
    //
    // decorationSprite가 null이면 기존처럼 해당 타입에서 랜덤 선택한다.
    //
    // localPositionOffset은
    // Anchor Tile 중심에서 자유 배치된 상대 위치다.
    //
    // removeExistingAtAnchor가 false이면
    // 같은 Anchor에 여러 장식물을 허용한다.
    private void SpawnDecorationInternal(
    DecorationType decorationType,
    int x,
    int y,
    Sprite decorationSprite,
    GameObject decorationVisualPrefab,
    Vector3 localPositionOffset,
    int layerOffset,
    bool removeExistingAtAnchor)
    {
        if (decorationPrefab == null)
        {
            Debug.LogError(
                "DecorationPrefab이 연결되지 않았습니다."
            );

            return;
        }

        if (decorationParent == null)
        {
            Debug.LogError(
                "DecorationParent가 연결되지 않았습니다."
            );

            return;
        }

        if (x < 0 ||
            x >= backgroundWidth ||
            y < 0 ||
            y >= backgroundHeight)
        {
            Debug.LogWarning(
                $"장식물 Anchor 좌표 ({x}, {y})가 배경 범위를 벗어났습니다."
            );

            return;
        }


              bool usePrefab =
            decorationVisualPrefab != null;


        Sprite resolvedDecorationSprite =
            decorationSprite;


        if (!usePrefab &&
            resolvedDecorationSprite == null)
        {
            // 기존 랜덤 생성 또는 구형 저장 데이터 fallback.
            BuildDecorationSpriteDictionary();

            resolvedDecorationSprite =
                GetRandomDecorationSprite(
                    decorationType
                );
        }

        if (!usePrefab &&
    resolvedDecorationSprite == null)
        {
            Debug.LogWarning(
                $"{decorationType} 타입에 연결된 장식물 스프라이트가 없습니다."
            );

            return;
        }


        if (removeExistingAtAnchor)
        {
            RemoveDecorationsAt(
                x,
                y
            );
        }


        Vector3 spawnPosition =
            GridToWorld(
                x,
                y
            ) +
            decorationOffset +
            localPositionOffset;


        GameObject decorationObject =
            Instantiate(
                decorationPrefab,
                spawnPosition,
                Quaternion.identity,
                decorationParent
            );


        decorationObject.name =
            $"Decoration_{decorationType}_{x}_{y}";


        SpriteRenderer spriteRenderer =
     decorationObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError(
                $"장식물 생성 실패: " +
                $"DecorationPrefab에 SpriteRenderer가 없습니다. " +
                $"Type: {decorationType}, " +
                $"Position: ({x}, {y})"
            );

            DestroyBackgroundObject(
                decorationObject
            );

            return;
        }


        Decoration decoration =
            decorationObject.GetComponent<Decoration>();

        if (decoration == null)
        {
            decoration =
                decorationObject.AddComponent<Decoration>();
        }


        // 저장 데이터나 외부 호출에서
        // 허용 범위를 벗어난 Layer 값이 들어와도
        // 다른 Grid의 Sorting 공간을 침범하지 않도록 제한한다.
        int safeLayerOffset =
            ClampDecorationLayerOffset(
                layerOffset
            );


        decoration.Initialize(
            decorationType,
            x,
            y,
            safeLayerOffset
        );


        // 먼저 Root Decoration의 최종 Sorting Order를 계산한다.
        //
        // Prefab 방식에서는 이 값을 기준으로
        // 자식 SpriteRenderer의 상대 Sorting Order가 더해진다.
        SetDecorationSortingOrder(
            decorationObject,
            x,
            y,
            safeLayerOffset
        );


        // <변경부분>
        // Prefab이 지정되어 있으면 Prefab Visual을 생성하고,
        // 그렇지 않으면 기존 Sprite 방식을 그대로 사용한다.
        if (usePrefab)
        {
            ApplyDecorationPrefabVisual(
                decorationObject,
                decorationVisualPrefab
            );
        }
        else
        {
            ApplyDecorationSpriteVisual(
                decorationObject,
                resolvedDecorationSprite
            );
        }
    }


    private void ApplyDecorationSpriteVisual(
    GameObject decorationObject,
    Sprite decorationSprite)
    {
        if (decorationObject == null)
        {
            return;
        }


        ClearSpawnedVisual(
            decorationObject
        );


        SpriteRenderer spriteRenderer =
            decorationObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }


        spriteRenderer.sprite =
            decorationSprite;


        if (useDarkDecoration)
        {
            float brightness =
                Mathf.Clamp01(
                    decorationBrightness
                );


            spriteRenderer.color =
                new Color(
                    brightness,
                    brightness,
                    brightness,
                    1f
                );
        }
        else
        {
            spriteRenderer.color =
                Color.white;
        }


        BackgroundVisualInstance visualInstance =
            GetOrAddVisualInstance(
                decorationObject
            );


        if (visualInstance != null)
        {
            visualInstance.SetSpriteSource(
                decorationSprite
            );
        }
    }


    private void ApplyDecorationPrefabVisual(
        GameObject decorationObject,
        GameObject visualPrefab)
    {
        if (decorationObject == null ||
            visualPrefab == null)
        {
            return;
        }


        ClearSpawnedVisual(
            decorationObject
        );


        SpriteRenderer anchorRenderer =
            decorationObject.GetComponent<SpriteRenderer>();

        if (anchorRenderer == null)
        {
            Debug.LogError(
                "DecorationPrefab에 SpriteRenderer가 없습니다."
            );

            return;
        }


        anchorRenderer.sprite =
            null;

        anchorRenderer.color =
            Color.white;


        GameObject visualRoot =
            Instantiate(
                visualPrefab,
                decorationObject.transform,
                false
            );


        visualRoot.name =
            $"__BackgroundVisual_{visualPrefab.name}";


        BackgroundVisualInstance visualInstance =
            GetOrAddVisualInstance(
                decorationObject
            );


        if (visualInstance != null)
        {
            visualInstance.SetPrefabSource(
                visualPrefab,
                visualRoot
            );
        }


        float brightness =
            useDarkDecoration
                ? Mathf.Clamp01(
                    decorationBrightness
                )
                : 1f;


        Color colorMultiplier =
            new Color(
                brightness,
                brightness,
                brightness,
                1f
            );


        ApplyPrefabSpriteRendererSettings(
            visualRoot,
            anchorRenderer,
            anchorRenderer.sortingOrder,
            colorMultiplier
        );
    }

    // <변경부분>
    // 현재 Brush Source 설정에 따라 실제로 배치할 Sprite를 반환한다.
    private Sprite GetDecorationBrushSprite()
    {
        if (decorationBrushSourceMode ==
            DecorationBrushSourceMode.ExactSprite)
        {
            return paintDecorationSprite;
        }


        BuildDecorationSpriteDictionary();


        return GetRandomDecorationSprite(
            paintDecorationType
        );
    }


    // <변경부분>
    // Scene View에서 클릭한 위치를 기준으로 장식물을 생성한다.
    public void PaintDecorationByWorldPosition(
        Vector3 worldPosition)
    {
        Sprite decorationSprite =
     null;

        GameObject decorationVisualPrefab =
            null;


        if (decorationBrushSourceMode ==
            DecorationBrushSourceMode.ExactPrefab)
        {
            decorationVisualPrefab =
                paintDecorationPrefab;


            if (decorationVisualPrefab == null)
            {
                Debug.LogWarning(
                    "Exact Prefab 모드에서 사용할 Decoration Prefab이 지정되지 않았습니다."
                );

                return;
            }
        }
        else
        {
            decorationSprite =
                GetDecorationBrushSprite();


            if (decorationSprite == null)
            {
                if (decorationBrushSourceMode ==
                    DecorationBrushSourceMode.ExactSprite)
                {
                    Debug.LogWarning(
                        "Exact Sprite 모드에서 배치할 Decoration Sprite가 지정되지 않았습니다."
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"{paintDecorationType} 타입에 연결된 Decoration Sprite가 없습니다."
                    );
                }

                return;
            }
        }


        // =========================================================
        // Grid Placement
        // =========================================================
        if (decorationPlacementMode ==
            DecorationPlacementMode.Grid)
        {
            if (!TryGetBackgroundGridPosition(
                    worldPosition,
                    out int gridX,
                    out int gridY))
            {
                return;
            }


            SpawnDecorationInternal(
        paintDecorationType,
        gridX,
        gridY,
        decorationSprite,
        decorationVisualPrefab,
        Vector3.zero,
        paintDecorationLayerOffset,
        preventDuplicateDecoration
    );

            return;
        }


        // =========================================================
        // Free Placement
        // =========================================================
        if (!hasFreePlacementAnchor)
        {
            Debug.LogWarning(
                "Free Placement를 사용하려면 Scene View에서 Anchor Tile을 먼저 선택해야 합니다."
            );

            return;
        }


        BackgroundTile anchorTile =
            GetBackgroundTileAt(
                freePlacementAnchorX,
                freePlacementAnchorY
            );

        if (anchorTile == null)
        {
            Debug.LogWarning(
                "현재 선택된 Free Placement Anchor Tile을 찾을 수 없습니다. Anchor를 다시 선택해주세요."
            );

            return;
        }


        Vector3 anchorBasePosition =
            GridToWorld(
                freePlacementAnchorX,
                freePlacementAnchorY
            ) +
            decorationOffset;


        Vector3 localPositionOffset =
            worldPosition -
            anchorBasePosition;

        localPositionOffset.z =
            0f;


        // Free Placement는 같은 Anchor Tile에
        // 여러 장식물을 허용한다.
        SpawnDecorationInternal(
     paintDecorationType,
     freePlacementAnchorX,
     freePlacementAnchorY,
     decorationSprite,
     decorationVisualPrefab,
     localPositionOffset,
     paintDecorationLayerOffset,
     false
 );
    }


    // <변경부분>
    // Scene View에서 클릭한 실제 BackgroundTile을
    // Free Placement Anchor로 지정한다.
    public bool SetFreePlacementAnchorByWorldPosition(
        Vector3 worldPosition)
    {
        if (!TryGetBackgroundGridPosition(
                worldPosition,
                out int gridX,
                out int gridY))
        {
            return false;
        }


        BackgroundTile anchorTile =
            GetBackgroundTileAt(
                gridX,
                gridY
            );

        if (anchorTile == null)
        {
            return false;
        }


        freePlacementAnchorX =
            anchorTile.X;

        freePlacementAnchorY =
            anchorTile.Y;

        hasFreePlacementAnchor =
            true;


#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(
                this
            );
        }
#endif


        return true;
    }


    public bool HasFreePlacementAnchor =>
        hasFreePlacementAnchor;

    public int FreePlacementAnchorX =>
        freePlacementAnchorX;

    public int FreePlacementAnchorY =>
        freePlacementAnchorY;

    public bool UsesFreeDecorationPlacement =>
        decorationPlacementMode ==
        DecorationPlacementMode.Free;


    // <변경부분>
    // Scene View Anchor 표시용 중심 좌표를 반환한다.
    public bool TryGetFreePlacementAnchorWorldPosition(
        out Vector3 worldPosition)
    {
        worldPosition =
            Vector3.zero;

        if (!hasFreePlacementAnchor)
        {
            return false;
        }


        BackgroundTile anchorTile =
            GetBackgroundTileAt(
                freePlacementAnchorX,
                freePlacementAnchorY
            );

        if (anchorTile == null)
        {
            return false;
        }


        worldPosition =
            GridToWorld(
                freePlacementAnchorX,
                freePlacementAnchorY
            );


        return true;
    }


    // Free Placement에서 Exact Sprite / Exact Prefab
    // Ghost Preview에 필요한 시각 정보를 반환한다.
    public bool TryGetFreeDecorationPreviewData(
        out Sprite sprite,
        out Material material,
        out int sortingLayerId,
        out int sortingOrder,
        out Color color)
    {
        sprite =
            null;

        material =
            null;

        sortingLayerId =
            0;

        sortingOrder =
            0;

        color =
            Color.white;


        // Free Placement이 아니거나
        // Anchor / 공통 DecorationPrefab이 없으면
        // Preview를 표시하지 않는다.
        if (decorationPlacementMode !=
                DecorationPlacementMode.Free ||
            !hasFreePlacementAnchor ||
            decorationPrefab == null)
        {
            return false;
        }


        // 공통 DecorationPrefab의 Root SpriteRenderer는
        // 실제 Decoration과 동일한 Sorting Layer 기준으로 사용한다.
        SpriteRenderer prefabRenderer =
            decorationPrefab.GetComponent<SpriteRenderer>();

        if (prefabRenderer == null)
        {
            return false;
        }


        int relativePreviewOrder =
            0;


        // =========================================================
        // Exact Sprite Preview
        // =========================================================
        if (decorationBrushSourceMode ==
            DecorationBrushSourceMode.ExactSprite)
        {
            if (paintDecorationSprite == null)
            {
                return false;
            }


            sprite =
                paintDecorationSprite;

            material =
                prefabRenderer.sharedMaterial;
        }

        // =========================================================
        // Exact Prefab Preview
        // =========================================================
        else if (decorationBrushSourceMode ==
                 DecorationBrushSourceMode.ExactPrefab)
        {
            if (paintDecorationPrefab == null)
            {
                return false;
            }


            SpriteRenderer[] previewRenderers =
                paintDecorationPrefab
                    .GetComponentsInChildren<SpriteRenderer>(
                        true
                    );


            SpriteRenderer previewRenderer =
                null;


            // Prefab 내부에서 실제 Sprite를 가지고 있는
            // 첫 번째 SpriteRenderer를 Preview 대표 이미지로 사용한다.
            for (int i = 0;
                 i < previewRenderers.Length;
                 i++)
            {
                SpriteRenderer currentRenderer =
                    previewRenderers[i];

                if (currentRenderer == null ||
                    currentRenderer.sprite == null)
                {
                    continue;
                }


                previewRenderer =
                    currentRenderer;

                break;
            }


            if (previewRenderer == null)
            {
                return false;
            }


            sprite =
                previewRenderer.sprite;

            material =
                previewRenderer.sharedMaterial;

            relativePreviewOrder =
                previewRenderer.sortingOrder;
        }
        else
        {
            // Random By Type은 Ghost Preview 대상이 아니다.
            return false;
        }


        sortingLayerId =
            prefabRenderer.sortingLayerID;


        int safeLayerOffset =
            ClampDecorationLayerOffset(
                paintDecorationLayerOffset
            );


        // 실제 배치될 Decoration과 동일한
        // Anchor / Layer 기반 Sorting Order를 계산한다.
        sortingOrder =
            GetEventWorldSortingOrder(
                freePlacementAnchorX,
                freePlacementAnchorY,
                DecorationSortingBaseOffset +
                safeLayerOffset
            ) +
            relativePreviewOrder;


        float brightness =
            useDarkDecoration
                ? Mathf.Clamp01(
                    decorationBrightness
                )
                : 1f;


        color =
            new Color(
                brightness,
                brightness,
                brightness,
                0.45f
            );


        return true;
    }


// <변경부분>
// Free Placement + Exact Prefab 모드에서
// Prefab 전체 Ghost Preview에 필요한 정보를 반환한다.
//
// 실제 Prefab Asset 자체를 Editor Preview 쪽에 전달하며,
// 실제 배치 데이터나 Prefab Asset 자체는 수정하지 않는다.
public bool TryGetFreeDecorationPrefabPreviewData(
    out GameObject visualPrefab,
    out int sortingLayerId,
    out int baseSortingOrder,
    out Color previewColor)
{
    visualPrefab =
        null;

    sortingLayerId =
        0;

    baseSortingOrder =
        0;

    previewColor =
        Color.white;


    if (decorationPlacementMode !=
            DecorationPlacementMode.Free ||
        !hasFreePlacementAnchor ||
        decorationBrushSourceMode !=
            DecorationBrushSourceMode.ExactPrefab ||
        paintDecorationPrefab == null ||
        decorationPrefab == null)
    {
        return false;
    }


    // 실제 Decoration Root와 동일한
    // Sorting Layer 기준을 Preview에도 사용한다.
    SpriteRenderer prefabRenderer =
        decorationPrefab.GetComponent<SpriteRenderer>();

    if (prefabRenderer == null)
    {
        return false;
    }


    visualPrefab =
        paintDecorationPrefab;


    sortingLayerId =
        prefabRenderer.sortingLayerID;


    int safeLayerOffset =
        ClampDecorationLayerOffset(
            paintDecorationLayerOffset
        );


    baseSortingOrder =
        GetEventWorldSortingOrder(
            freePlacementAnchorX,
            freePlacementAnchorY,
            DecorationSortingBaseOffset +
            safeLayerOffset
        );


    float brightness =
        useDarkDecoration
            ? Mathf.Clamp01(
                decorationBrightness
            )
            : 1f;


    previewColor =
        new Color(
            brightness,
            brightness,
            brightness,
            0.45f
        );


    return true;
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
            if (decoration.X == x &&
    decoration.Y == y)
            {
                // <변경부분> Play Mode에서도 안전하게
                // 기존 장식물을 제거할 수 있도록 공용 삭제 함수를 사용한다.
                DestroyBackgroundObject(
                    child.gameObject
                );
            }
        }
    }

    // <변경부분> 생성된 장식물 전체를 제거
    public void ClearDecorations()
    {
        if (decorationParent == null)
        {
            return;
        }

        // <변경부분> Editor 제작 상태뿐 아니라
        // StageBattleData에서 다른 BackgroundMapData를
        // 런타임에 불러올 때도 기존 장식물을 안전하게 제거한다.
        for (int i =
                 decorationParent.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject decorationObject =
                decorationParent
                    .GetChild(i)
                    .gameObject;

            DestroyBackgroundObject(
                decorationObject
            );
        }
    }

    // <변경부분> 현재 배경 타일 배치에 맞춰 장식물을 자동 생성
    public void GenerateDecorationsByRules()
    {
        // 기존 장식물을 모두 제거하고 새 규칙으로 다시 생성
        ClearDecorations();

        // 배경 배열이 없으면 씬에 있는 배경 타일을 다시 연결
        if (backgroundTiles == null)
        {
            RebuildBackgroundTileArrayFromScene();
        }

        // 배경 배열이 없으면 자동 장식물 생성을 중단
        if (backgroundTiles == null)
        {
            Debug.LogWarning("생성된 배경이 없어 장식물을 자동 생성할 수 없습니다.");
            return;
        }

        for (int x = 0; x < backgroundWidth; x++)
        {
            for (int y = 0; y < backgroundHeight; y++)
            {
                BackgroundTile tile = backgroundTiles[x, y];

                if (tile == null)
                {
                    continue;
                }

                TrySpawnDecorationByTile(tile);
            }
        }
    }

    // <변경부분> 배경 타일 타입에 맞는 장식물 규칙을 확인하고 확률에 따라 생성
    private void TrySpawnDecorationByTile(BackgroundTile tile)
    {
        if (tile == null)
        {
            return;
        }

        for (int i = 0; i < decorationSpawnRules.Count; i++)
        {
            DecorationSpawnRule rule = decorationSpawnRules[i];

            if (rule == null)
            {
                continue;
            }

            // 현재 타일 타입과 맞지 않는 규칙은 건너뜀
            if (rule.TargetTileType != tile.TileType)
            {
                continue;
            }

            // 설정한 확률에 걸리지 않으면 생성하지 않음
            if (Random.value > rule.SpawnChance)
            {
                continue;
            }

            // 현재 타일 좌표에 규칙에 맞는 장식물 생성
            SpawnDecoration(
                rule.DecorationType,
                tile.X,
                tile.Y
            );

            // 한 타일에 장식물이 여러 개 생기지 않도록 첫 생성 후 중단
            return;
        }
    }


    // Decoration 자신과 Prefab Visual 자식에 포함된
    // 모든 Renderer의 실제 표시 영역을 하나의 Bounds로 계산한다.
    //
    // 기존 SpriteRenderer Decoration뿐 아니라
    // Animator / Spine / MeshRenderer 등을 사용하는
    // Prefab Decoration도 삭제 클릭 판정에 사용할 수 있다.
    private bool TryGetCombinedDecorationBounds(
        GameObject targetObject,
        out Bounds combinedBounds)
    {
        combinedBounds =
            new Bounds();


        if (targetObject == null)
        {
            return false;
        }


        Renderer[] renderers =
            targetObject.GetComponentsInChildren<Renderer>(
                true
            );


        bool hasBounds =
            false;


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer renderer =
                renderers[i];

            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }


            Bounds rendererBounds =
                renderer.bounds;


            // 크기가 완전히 0인 Renderer는
            // 실제 클릭 영역으로 사용하지 않는다.
            if (rendererBounds.size.sqrMagnitude <=
                Mathf.Epsilon)
            {
                continue;
            }


            if (!hasBounds)
            {
                combinedBounds =
                    rendererBounds;

                hasBounds =
                    true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    rendererBounds
                );
            }
        }


        return hasBounds;
    }

    // <변경부분>
    // Free Placement 삭제에서 현재 마우스 위치와 겹치는
    // Decoration 하나를 찾는다.
    //
    // 여러 Decoration이 겹쳐 있는 경우:
    // 1. Sorting Layer가 더 앞인 것
    // 2. Sorting Order가 더 높은 것
    // 3. 그래도 같으면 Sprite 중심이 클릭 위치에 더 가까운 것
    //
    // 순서로 삭제 대상을 결정한다.
    private Decoration FindDecorationAtWorldPosition(
        Vector3 worldPosition)
    {
        if (decorationParent == null)
        {
            return null;
        }


        Decoration bestDecoration =
            null;

        int bestSortingLayerValue =
            int.MinValue;

        int bestSortingOrder =
            int.MinValue;

        float bestDistance =
            float.MaxValue;


        for (int i = 0;
             i < decorationParent.childCount;
             i++)
        {
            Transform child =
                decorationParent.GetChild(i);

            if (child == null)
            {
                continue;
            }


            Decoration decoration =
                child.GetComponent<Decoration>();

            if (decoration == null)
            {
                continue;
            }


            SpriteRenderer spriteRenderer =
     child.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                continue;
            }


            bool hasVisualBounds =
                TryGetCombinedDecorationBounds(
                    child.gameObject,
                    out Bounds bounds
                );


            if (!hasVisualBounds)
            {
                // Renderer가 전혀 없는 Light2D 전용 Prefab 등도
                // 최소한 Decoration Root 근처를 클릭하면
                // 삭제할 수 있도록 작은 fallback 영역을 사용한다.
                const float fallbackPickSize =
                    0.3f;


                bounds =
                    new Bounds(
                        child.position,
                        new Vector3(
                            fallbackPickSize,
                            fallbackPickSize,
                            0.1f
                        )
                    );
            }


            // Renderer.bounds는 3D Bounds이므로
            // 2D 편집에서는 X / Y 영역만 검사한다.
            if (worldPosition.x < bounds.min.x ||
                worldPosition.x > bounds.max.x ||
                worldPosition.y < bounds.min.y ||
                worldPosition.y > bounds.max.y)
            {
                continue;
            }


            int sortingLayerValue =
                SortingLayer.GetLayerValueFromID(
                    spriteRenderer.sortingLayerID
                );

            int sortingOrder =
                spriteRenderer.sortingOrder;


            Vector2 clickPosition =
                new Vector2(
                    worldPosition.x,
                    worldPosition.y
                );

            Vector2 spriteCenter =
                new Vector2(
                    bounds.center.x,
                    bounds.center.y
                );

            float distance =
                Vector2.SqrMagnitude(
                    clickPosition -
                    spriteCenter
                );


            bool isBetterCandidate =
                false;


            if (bestDecoration == null)
            {
                isBetterCandidate =
                    true;
            }
            else if (sortingLayerValue >
                     bestSortingLayerValue)
            {
                isBetterCandidate =
                    true;
            }
            else if (sortingLayerValue ==
                     bestSortingLayerValue &&
                     sortingOrder >
                     bestSortingOrder)
            {
                isBetterCandidate =
                    true;
            }
            else if (sortingLayerValue ==
                     bestSortingLayerValue &&
                     sortingOrder ==
                     bestSortingOrder &&
                     distance <
                     bestDistance)
            {
                isBetterCandidate =
                    true;
            }


            if (!isBetterCandidate)
            {
                continue;
            }


            bestDecoration =
                decoration;

            bestSortingLayerValue =
                sortingLayerValue;

            bestSortingOrder =
                sortingOrder;

            bestDistance =
                distance;
        }


        return bestDecoration;
    }


    // <변경부분>
    // Free Placement에서 실제 클릭한 Decoration 하나만 삭제한다.
    private void EraseSingleDecorationByWorldPosition(
        Vector3 worldPosition)
    {
        Decoration decoration =
            FindDecorationAtWorldPosition(
                worldPosition
            );

        if (decoration == null)
        {
            return;
        }


        DestroyBackgroundObject(
            decoration.gameObject
        );
    }


    // <변경부분>
    // Scene View에서 클릭한 위치를 기준으로 Decoration을 제거한다.
    //
    // Grid Placement:
    // 기존 방식대로 해당 Grid에 연결된 Decoration 전체 삭제.
    //
    // Free Placement:
    // 마우스로 직접 클릭한 Decoration 하나만 삭제.
    public void EraseDecorationByWorldPosition(
    Vector3 worldPosition)
    {
        // =========================================================
        // Free Placement
        // =========================================================
        //
        // 실제 클릭한 Decoration 하나만 제거한다.
        if (decorationPlacementMode ==
            DecorationPlacementMode.Free)
        {
            EraseSingleDecorationByWorldPosition(
                worldPosition
            );

            return;
        }


        // =========================================================
        // Grid Placement
        // =========================================================
        //
        // Prefab Decoration은 실제 외형이 Anchor Tile보다
        // 훨씬 클 수 있으므로 먼저 화면에서 클릭한
        // Decoration을 직접 찾는다.
        Decoration clickedDecoration =
            FindDecorationAtWorldPosition(
                worldPosition
            );


        if (clickedDecoration != null)
        {
            // Grid Placement의 기존 규칙은 유지한다.
            //
            // 즉 클릭한 Decoration 하나만 지우는 것이 아니라
            // 해당 Decoration이 속한 Anchor Grid의
            // Decoration 전체를 삭제한다.
            RemoveDecorationsAt(
                clickedDecoration.X,
                clickedDecoration.Y
            );

            return;
        }


        // 실제 Decoration 외형을 클릭하지 않은 경우에는
        // 기존 Grid 좌표 삭제 방식으로 fallback한다.
        if (!TryGetBackgroundGridPosition(
                worldPosition,
                out int gridX,
                out int gridY))
        {
            return;
        }


        RemoveDecorationsAt(
            gridX,
            gridY
        );
    }

    // <변경부분>
    // Decoration Layer 값을 현재 허용 범위로 제한한다.
    private int ClampDecorationLayerOffset(
        int layerOffset)
    {
        return Mathf.Clamp(
            layerOffset,
            DecorationLayerOffsetMin,
            DecorationLayerOffsetMax
        );
    }


    // <변경부분>
    // 장식물의 Anchor Grid와 Layer를 기준으로
    // 실제 Sorting Order를 적용한다.
    private void SetDecorationSortingOrder(
        GameObject decorationObject,
        int x,
        int y,
        int layerOffset)
    {
        SpriteRenderer spriteRenderer =
            decorationObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            return;
        }


        int safeLayerOffset =
            ClampDecorationLayerOffset(
                layerOffset
            );


        spriteRenderer.sortingOrder =
            GetEventWorldSortingOrder(
                x,
                y,
                DecorationSortingBaseOffset +
                safeLayerOffset
            );
    }


    // Event Scene의 Actor / Decoration이 공통으로 사용할
    // 아이소메트릭 World Sorting Order를 계산한다.
    private int GetEventWorldSortingOrder(
        int x,
        int y,
        int localOffset)
    {
        int depth =
            x + y;

        return
            EventWorldBaseSortingOrder -
            (
                depth *
                EventWorldDepthStep
            ) +
            localOffset;
    }


    // <변경부분>
    // EventSceneActor가 현재 Background World와 동일한
    // Sorting Layer / Order를 사용할 수 있도록 정렬 정보를 제공한다.
    //
    // Sorting Layer는 Scene 이름이나 문자열을 하드코딩하지 않고
    // 실제 Decoration Prefab의 SpriteRenderer 설정을 기준으로 가져온다.
    public bool TryGetEventActorSortingSettings(
    int x,
    int y,
    out int sortingLayerId,
    out int sortingOrder)
    {
        sortingLayerId =
            0;

        // <변경부분>
        // 우선 Actor 자신의 Grid Depth를 기준으로
        // 기존 아이소메트릭 Sorting 값을 계산한다.
        sortingOrder =
            GetEventWorldSortingOrder(
                x,
                y,
                EventActorSortingOffset
            );

        if (decorationPrefab == null)
        {
            return false;
        }

        SpriteRenderer decorationRenderer =
            decorationPrefab
                .GetComponent<SpriteRenderer>();

        if (decorationRenderer == null)
        {
            return false;
        }

        // Event Actor와 Decoration이 서로 Order로 교차될 수 있도록
        // 동일한 Sorting Layer를 사용한다.
        sortingLayerId =
            decorationRenderer.sortingLayerID;


        // <변경부분>
        // 현재 Actor가 어떤 Decoration의 가림 영향권 안에 있다면
        // 해당 Decoration보다 Actor가 반드시 뒤에 그려지도록 한다.
        //
        // 여러 Decoration의 영향권이 겹친 경우에는
        // Actor를 그중 가장 뒤에 필요한 Order까지 내려
        // 모든 해당 Decoration이 Actor 앞에 보이도록 한다.
        if (decorationParent == null)
        {
            return true;
        }

        for (int i = 0;
             i < decorationParent.childCount;
             i++)
        {
            Transform decorationTransform =
                decorationParent.GetChild(i);

            if (decorationTransform == null)
            {
                continue;
            }

            Decoration decoration =
                decorationTransform
                    .GetComponent<Decoration>();

            if (decoration == null)
            {
                continue;
            }

            DecorationSet decorationSet =
                GetDecorationSet(
                    decoration.DecorationType
                );

            if (IsGridInsideDecorationOcclusion(
                    decoration,
                    decorationSet,
                    x,
                    y) == false)
            {
                continue;
            }

            SpriteRenderer activeDecorationRenderer =
                decorationTransform
                    .GetComponent<SpriteRenderer>();

            if (activeDecorationRenderer == null)
            {
                continue;
            }

            // 다른 Sorting Layer라면 Order 비교 자체가 의미가 없으므로
            // 현재 Event World Layer와 동일한 Decoration만 처리한다.
            if (activeDecorationRenderer.sortingLayerID !=
                sortingLayerId)
            {
                continue;
            }

            int behindDecorationOrder =
                activeDecorationRenderer.sortingOrder -
                1;

            sortingOrder =
                Mathf.Min(
                    sortingOrder,
                    behindDecorationOrder
                );
        }

        return true;
    }


    // <변경부분>
    // 이동 중인 Event Actor의 지면 World Position을
    // 현재 Background Grid 좌표로 변환한 뒤 정렬 정보를 반환한다.
    //
    // EventSceneActor의 점프 Arc 높이는 이 함수에 전달하지 않고,
    // 포물선 높이를 더하기 전의 지면 위치만 전달한다.
    public bool TryGetEventActorSortingSettings(
        Vector3 groundWorldPosition,
        out int sortingLayerId,
        out int sortingOrder)
    {
        sortingLayerId =
            0;

        sortingOrder =
            0;

        if (TryGetBackgroundGridPosition(
                groundWorldPosition,
                out int gridX,
                out int gridY) == false)
        {
            return false;
        }

        return
            TryGetEventActorSortingSettings(
                gridX,
                gridY,
                out sortingLayerId,
                out sortingOrder
            );
    }

    // <변경부분> 인스펙터에 입력한 좌표와 타입으로 장식물 생성 테스트
    public void SpawnTestDecoration()
    {
        SpawnDecoration(
            testDecorationType,
            testDecorationX,
            testDecorationY
        );
    }


    // <변경부분>
    // 현재 BackgroundMapData에 연결된 EnvironmentVisualProfile을
    // EnvironmentLightingRig에 적용한다.
    //
    // Inspector 버튼을 통한 Editor Preview에서도 재사용한다.
    public void ApplyCurrentLightingProfile()
    {
        if (currentMapData == null)
        {
            Debug.LogWarning(
                "환경 비주얼 적용 실패: " +
                "Current Map Data가 연결되어 있지 않습니다."
            );

            return;
        }


        EnvironmentVisualProfile visualProfile =
            currentMapData.VisualProfile;


        if (visualProfile == null)
        {
            // <변경부분>
            // Profile이 없는 Map으로 교체될 때
            // 이전 Map의 구름이 계속 남아 있는 것을 방지한다.
            if (cloudShadowController != null)
            {
                cloudShadowController.ApplyProfile(
                    null
                );
            }


            if (fireflyEnvironmentController != null)
            {
                fireflyEnvironmentController.ApplyProfile(
                    null
                );
            }


            return;
        }


        // 기존 환경 Lighting.
        if (environmentLightingController != null)
        {
            environmentLightingController.ApplyProfile(
                visualProfile
            );
        }
        else
        {
            Debug.LogWarning(
                $"환경 조명 자동 적용 실패: " +
                $"{currentMapData.name}에는 EnvironmentVisualProfile이 있지만 " +
                "EnvironmentLightingController가 연결되어 있지 않습니다."
            );
        }


        if (fireflyEnvironmentController != null)
        {
            fireflyEnvironmentController.ApplyProfile(
                visualProfile
            );
        }
        else if (visualProfile.fireflies != null &&
                 visualProfile.fireflies.enabled)
        {
            Debug.LogWarning(
                $"Firefly 환경 자동 적용 실패: " +
                $"{currentMapData.name}에는 Fireflies가 활성화되어 있지만 " +
                "FireflyEnvironmentController가 연결되어 있지 않습니다."
            );
        }


        // <변경부분>
        // Battle Scene / Event Scene 모두
        // BackgroundManager의 동일한 Map Load 경로를 이용해
        // Cloud Shadow 설정을 자동 적용한다.
        if (cloudShadowController != null)
        {
            cloudShadowController.ApplyProfile(
                visualProfile
            );
        }
        else if (visualProfile.cloudShadow != null &&
                 visualProfile.cloudShadow.enabled)
        {
            Debug.LogWarning(
                $"Cloud Shadow 자동 적용 실패: " +
                $"{currentMapData.name}에는 Cloud Shadow가 활성화되어 있지만 " +
                "CloudShadowController가 연결되어 있지 않습니다."
            );
        }
    }


    // <변경부분>
    // Runtime의 자동 Map Load에서는
    // 기존 BackgroundMapData에 아직 Profile이 없는 경우도 허용한다.
    //
    // 따라서 Profile이 없는 Map은 경고 없이
    // 현재 EnvironmentLightingRig 설정을 그대로 유지한다.
    private void ApplyEnvironmentVisualProfileFromCurrentMap()
    {
        if (currentMapData == null)
        {
            if (cloudShadowController != null)
            {
                cloudShadowController.ApplyProfile(
                    null
                );
            }


            if (fireflyEnvironmentController != null)
            {
                fireflyEnvironmentController.ApplyProfile(
                    null
                );
            }


            return;
        }


        EnvironmentVisualProfile visualProfile =
            currentMapData.VisualProfile;


        if (visualProfile == null)
        {
            // <변경부분>
            // Profile이 없는 Map으로 교체될 때
            // 이전 Map의 구름이 계속 남아 있는 것을 방지한다.
            if (cloudShadowController != null)
            {
                cloudShadowController.ApplyProfile(
                    null
                );
            }


            if (fireflyEnvironmentController != null)
            {
                fireflyEnvironmentController.ApplyProfile(
                    null
                );
            }


            return;


            // 기존 환경 Lighting.
            if (environmentLightingController != null)
            {
                environmentLightingController.ApplyProfile(
                    visualProfile
                );
            }
            else
            {
                Debug.LogWarning(
                    $"환경 조명 자동 적용 실패: " +
                    $"{currentMapData.name}에는 EnvironmentVisualProfile이 있지만 " +
                    "EnvironmentLightingController가 연결되어 있지 않습니다."
                );
            }

            if (fireflyEnvironmentController != null)
            {
                fireflyEnvironmentController.ApplyProfile(
                    visualProfile
                );
            }
            else if (visualProfile.fireflies != null &&
                     visualProfile.fireflies.enabled)
            {
                Debug.LogWarning(
                    $"Firefly 환경 자동 적용 실패: " +
                    $"{currentMapData.name}에는 Fireflies가 활성화되어 있지만 " +
                    "FireflyEnvironmentController가 연결되어 있지 않습니다."
                );
            }


            // <변경부분>
            // Battle Scene / Event Scene 모두
            // BackgroundManager의 동일한 Map Load 경로를 이용해
            // Cloud Shadow 설정을 자동 적용한다.
            if (cloudShadowController != null)
            {
                cloudShadowController.ApplyProfile(
                    visualProfile
                );
            }
            else if (visualProfile.cloudShadow != null &&
                     visualProfile.cloudShadow.enabled)
            {
                Debug.LogWarning(
                    $"Cloud Shadow 자동 적용 실패: " +
                    $"{currentMapData.name}에는 Cloud Shadow가 활성화되어 있지만 " +
                    "CloudShadowController가 연결되어 있지 않습니다."
                );
            }
        }
    }

    // <변경부분>
    // 현재 Scene에서 직접 튜닝한 EnvironmentLightingRig 값을
    // 현재 BackgroundMapData의 EnvironmentVisualProfile Asset에 저장한다.
    public void SaveCurrentLightingToProfile()
    {
#if UNITY_EDITOR
        if (currentMapData == null)
        {
            Debug.LogWarning(
                "환경 조명 저장 실패: " +
                "Current Map Data가 연결되어 있지 않습니다."
            );

            return;
        }

        EnvironmentVisualProfile visualProfile =
            currentMapData.VisualProfile;

        if (visualProfile == null)
        {
            Debug.LogWarning(
                $"환경 조명 저장 실패: " +
                $"{currentMapData.name}에 " +
                "EnvironmentVisualProfile을 먼저 연결하세요."
            );

            return;
        }

        if (environmentLightingController == null)
        {
            Debug.LogWarning(
                "환경 조명 저장 실패: " +
                "EnvironmentLightingController가 연결되어 있지 않습니다."
            );

            return;
        }

        UnityEditor.Undo.RecordObject(
            visualProfile,
            "Save Environment Lighting Profile"
        );

        if (environmentLightingController
            .CaptureCurrentToProfile(
                visualProfile
            ) == false)
        {
            return;
        }

        UnityEditor.EditorUtility.SetDirty(
            visualProfile
        );

        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log(
            $"환경 조명 Profile 저장 완료: " +
            $"{visualProfile.name}"
        );
#else
    Debug.LogWarning(
        "환경 조명 Profile Asset 저장은 " +
        "Unity Editor에서만 사용할 수 있습니다."
    );
#endif
    }


    // 현재 씬에 배치된 배경 타일과 장식물을 맵 데이터에 저장
    public void SaveCurrentMapToData()
    {
        if (currentMapData == null)
        {
            Debug.LogWarning("저장할 BackgroundMapData가 연결되지 않았습니다.");
            return;
        }

        RebuildBackgroundTileArrayFromScene();

        currentMapData.Width = backgroundWidth;
        currentMapData.Height = backgroundHeight;
        currentMapData.BackgroundOriginOffset = backgroundOriginOffset;

        currentMapData.Tiles.Clear();
        currentMapData.Decorations.Clear();

        if (backgroundTiles != null)
        {
            for (int x = 0; x < backgroundWidth; x++)
            {
                for (int y = 0; y < backgroundHeight; y++)
                {
                    BackgroundTile tile =
     backgroundTiles[x, y];

                    if (tile == null)
                    {
                        continue;
                    }


                    SpriteRenderer tileSpriteRenderer =
                        tile.GetComponent<SpriteRenderer>();


                    BackgroundTileSaveData tileData =
                        new BackgroundTileSaveData();

                    tileData.X =
                        tile.X;

                    tileData.Y =
                        tile.Y;

                    tileData.TileType =
                        tile.TileType;

                    // <변경부분>
                    // 현재 Scene에 실제로 표시되고 있는
                    // 타일 Sprite를 그대로 저장한다.
                    BackgroundVisualInstance tileVisualInstance =
      tile.GetComponent<BackgroundVisualInstance>();


                    if (tileVisualInstance != null &&
                        tileVisualInstance.UsesPrefab)
                    {
                        tileData.TileSprite =
                            null;

                        tileData.TilePrefab =
                            tileVisualInstance.SourcePrefab;
                    }
                    else
                    {
                        tileData.TileSprite =
                            tileVisualInstance != null &&
                            tileVisualInstance.SourceSprite != null
                                ? tileVisualInstance.SourceSprite
                                : tileSpriteRenderer != null
                                    ? tileSpriteRenderer.sprite
                                    : null;

                        tileData.TilePrefab =
                            null;
                    }


                    currentMapData.Tiles.Add(
                        tileData
                    );
                }
            }
        }

        SaveDecorationsToData();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(currentMapData);
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log("현재 배경 맵 데이터를 저장했습니다.");
    }

    // <변경부분> 현재 씬에 배치된 장식물 정보를 맵 데이터에 저장
    private void SaveDecorationsToData()
    {
        if (currentMapData == null)
        {
            return;
        }

        if (decorationParent == null)
        {
            return;
        }

        for (int i = 0;
     i < decorationParent.childCount;
     i++)
        {
            Decoration decoration =
                decorationParent
                    .GetChild(i)
                    .GetComponent<Decoration>();

            if (decoration == null)
            {
                continue;
            }


            SpriteRenderer spriteRenderer =
                decoration.GetComponent<SpriteRenderer>();


            DecorationSaveData decorationData =
                new DecorationSaveData();


            decorationData.X =
                decoration.X;

            decorationData.Y =
                decoration.Y;

            decorationData.DecorationType =
                decoration.DecorationType;

            BackgroundVisualInstance decorationVisualInstance =
       decoration.GetComponent<BackgroundVisualInstance>();


            if (decorationVisualInstance != null &&
                decorationVisualInstance.UsesPrefab)
            {
                decorationData.DecorationSprite =
                    null;

                decorationData.DecorationPrefab =
                    decorationVisualInstance.SourcePrefab;
            }
            else
            {
                decorationData.DecorationSprite =
                    decorationVisualInstance != null &&
                    decorationVisualInstance.SourceSprite != null
                        ? decorationVisualInstance.SourceSprite
                        : spriteRenderer != null
                            ? spriteRenderer.sprite
                            : null;

                decorationData.DecorationPrefab =
                    null;
            }


            Vector3 anchorBasePosition =
                GridToWorld(
                    decoration.X,
                    decoration.Y
                ) +
                decorationOffset;


            decorationData.LocalPositionOffset =
     decoration.transform.position -
     anchorBasePosition;


            // <변경부분>
            // 동일 Anchor 내부에서 설정했던
            // 장식물의 앞뒤 Layer도 함께 저장한다.
            decorationData.LayerOffset =
                decoration.LayerOffset;


            currentMapData.Decorations.Add(
                decorationData
            );
        }
    }

    // <변경부분> StageBattleData에서 전달받은
    // BackgroundMapData를 현재 맵 데이터로 적용하고 즉시 불러온다.
    //
    // 기존 Inspector의 Current Map Data는
    // 배경 제작 / 저장 / 에디터 테스트용으로 계속 사용할 수 있고,
    // 실제 BattleScene에서는 현재 StageBattleData가 전달한 값으로 교체된다.
    public void LoadMapFromData(
        BackgroundMapData mapData)
    {
        if (mapData == null)
        {
            Debug.LogWarning(
                "배경 맵 데이터 적용 실패: " +
                "전달된 BackgroundMapData가 없습니다."
            );

            return;
        }

        currentMapData =
            mapData;

        LoadMapFromData();
    }

    public void LoadMapFromData()
    {
        if (currentMapData == null)
        {
            Debug.LogWarning(
                "불러올 BackgroundMapData가 연결되지 않았습니다."
            );

            return;
        }

        // 저장된 맵 크기가 유효하지 않으면
        // 배열 생성이나 잘못된 Runtime 상태로 진입하기 전에 중단한다.
        if (currentMapData.Width <= 0 ||
            currentMapData.Height <= 0)
        {
            Debug.LogError(
                $"배경 맵 데이터 불러오기 실패: " +
                $"유효하지 않은 맵 크기입니다. " +
                $"{currentMapData.Width} x " +
                $"{currentMapData.Height}"
            );

            return;
        }

        // 검증된 BackgroundMapData의 기본 설정을 적용한다.
        backgroundWidth =
            currentMapData.Width;

        backgroundHeight =
            currentMapData.Height;

        backgroundOriginOffset =
            currentMapData.BackgroundOriginOffset;

        // 기존 Scene 배경과 장식물을 정리한 뒤
        // 저장 데이터 기준으로 다시 구성한다.
        ClearBackground();
        ClearDecorations();

        BuildTileSpriteDictionary();
        BuildDecorationSpriteDictionary();

        backgroundTiles =
            new BackgroundTile[
                backgroundWidth,
                backgroundHeight
            ];

        // 저장된 배경 타일을 다시 생성한다.
        //
        // SpawnBackgroundTile() 자체에서도 좌표를 검증하므로
        // 잘못된 개별 데이터는 건너뛰고 나머지 맵은 계속 불러온다.
        if (currentMapData.Tiles != null)
        {
            for (int i = 0;
                 i < currentMapData.Tiles.Count;
                 i++)
            {
                BackgroundTileSaveData tileData =
                    currentMapData.Tiles[i];

                if (tileData == null)
                {
                    continue;
                }

                SpawnBackgroundTile(
      tileData.TileType,
      tileData.X,
      tileData.Y,
      tileData.TileSprite,
      tileData.TilePrefab
  );
            }
        }

        // 저장된 장식물을 다시 생성한다.
        //
        // SpawnDecoration() 내부에 동일한 좌표 범위 검사가 있으므로
        // 잘못된 장식물 데이터도 안전하게 건너뛴다.
        if (currentMapData.Decorations != null)
        {
            for (int i = 0;
                 i < currentMapData.Decorations.Count;
                 i++)
            {
                DecorationSaveData decorationData =
                    currentMapData.Decorations[i];

                if (decorationData == null)
                {
                    continue;
                }

                SpawnDecorationInternal(
    decorationData.DecorationType,
    decorationData.X,
    decorationData.Y,
    decorationData.DecorationSprite,
    decorationData.DecorationPrefab,
    decorationData.LocalPositionOffset,
    decorationData.LayerOffset,
    false
);
            }
        }


        // <변경부분>
        // 배경과 Decoration 구성이 완료된 뒤
        // 이 BackgroundMapData 전용 환경 조명 Profile을 적용한다.
        //
        // Event Scene / Battle Scene 모두 BackgroundManager.LoadMapFromData()를
        // 공통으로 사용하므로 별도 Scene별 조명 코드를 만들 필요가 없다.
        ApplyEnvironmentVisualProfileFromCurrentMap();


        Debug.Log(
            "배경 맵 데이터를 불러왔습니다."
        );
    }

    // <변경부분> 새 BackgroundMapData 에셋을 생성하고 현재 맵 데이터로 연결
    public void CreateNewMapDataAsset()
    {
#if UNITY_EDITOR
        // 저장 폴더가 없으면 자동으로 생성
        if (!UnityEditor.AssetDatabase.IsValidFolder(mapDataSaveFolder))
        {
            CreateFolderPath(mapDataSaveFolder);
        }

        // 새 맵 데이터 에셋 생성
        BackgroundMapData newMapData = ScriptableObject.CreateInstance<BackgroundMapData>();

        // 같은 이름의 에셋이 있으면 Unity가 자동으로 번호를 붙여 고유 경로 생성
        string assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
            $"{mapDataSaveFolder}/{newMapDataName}.asset"
        );

        UnityEditor.AssetDatabase.CreateAsset(newMapData, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        // 생성한 맵 데이터를 현재 BackgroundManager에 자동 연결
        currentMapData = newMapData;

        UnityEditor.EditorUtility.SetDirty(this);

        Debug.Log($"새 배경 맵 데이터가 생성되었습니다: {assetPath}");
#else
    Debug.LogWarning("맵 데이터 에셋 생성은 Unity 에디터에서만 사용할 수 있습니다.");
#endif
    }

#if UNITY_EDITOR
    // <변경부분> 지정한 Assets 하위 폴더 경로가 없으면 단계별로 생성
    private void CreateFolderPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            return;
        }

        if (!fullPath.StartsWith("Assets"))
        {
            Debug.LogWarning("맵 데이터 저장 경로는 Assets 폴더 안이어야 합니다.");
            return;
        }

        string[] folders = fullPath.Split('/');
        string currentPath = "Assets";

        for (int i = 1; i < folders.Length; i++)
        {
            string nextPath = $"{currentPath}/{folders[i]}";

            if (!UnityEditor.AssetDatabase.IsValidFolder(nextPath))
            {
                UnityEditor.AssetDatabase.CreateFolder(currentPath, folders[i]);
            }

            currentPath = nextPath;
        }
    }
#endif
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

    // <변경부분> 씬뷰에서 장식물 삭제 브러시를 사용할지 저장
    private bool isDecorationEraseMode = false;

    // <변경부분>
    // Free Placement의 기준 타일을 Scene View에서 선택 중인지 저장
    private bool isSelectingDecorationAnchor = false;

    // <변경부분>
    // Exact Sprite / Exact Prefab Free Placement용 Ghost Preview.
    private GameObject decorationPreviewObject;

    // Exact Sprite Preview에서만 사용한다.
    private SpriteRenderer decorationPreviewRenderer;

    // <변경부분>
    // Exact Prefab Preview에서 현재 복제 중인
    // 원본 Prefab Asset을 기억한다.
    //
    // Inspector에서 다른 Prefab으로 변경했을 때
    // Preview를 자동 재생성하기 위해 사용한다.
    private GameObject decorationPreviewSourcePrefab;

    // <변경부분>
    // Prefab Preview 안의 Renderer 목록과
    // Prefab 원본의 상대 Sorting Order를 기억한다.
    private Renderer[] decorationPreviewRenderers;

    private int[] decorationPreviewRelativeSortingOrders;

    // SpriteRenderer의 원래 Color.
    //
    // Preview Alpha / 밝기를 반복 적용해도
    // 색상값이 계속 누적해서 어두워지지 않도록 보존한다.
    private Color[] decorationPreviewOriginalSpriteColors;

    // <변경부분>
    // Prefab Root/Pivot에서 실제로 화면에 보이는
    // Renderer 전체 Bounds 중심까지의 상대 Offset.
    //
    // Prefab Pivot 위치와 관계없이
    // Ghost Preview의 시각적 중심이 마우스에 오도록 사용한다.
    private Vector3 decorationPreviewVisualCenterOffset =
        Vector3.zero;


    private void OnEnable()
    {
        // 씬뷰에서 배경 에디터 입력을 감지
        SceneView.duringSceneGui += HandleSceneGUI;
    }

    private void OnDisable()
    {
        // 에디터 선택이 해제되면 씬뷰 입력 감지를 중단
        SceneView.duringSceneGui -= HandleSceneGUI;

        DestroyDecorationPreview();
    }


    // <변경부분>
    // Sprite / Prefab Ghost Preview를 모두 즉시 정리한다.
    private void DestroyDecorationPreview()
    {
        if (decorationPreviewObject != null)
        {
            DestroyImmediate(
                decorationPreviewObject
            );
        }


        decorationPreviewObject =
            null;

        decorationPreviewRenderer =
            null;

        decorationPreviewSourcePrefab =
            null;

        decorationPreviewRenderers =
            null;

        decorationPreviewRelativeSortingOrders =
     null;

        decorationPreviewOriginalSpriteColors =
            null;

        decorationPreviewVisualCenterOffset =
            Vector3.zero;
    }


    // <변경부분>
    // Preview Object와 모든 자식을
    // Scene / Prefab 저장 대상에서 제외한다.
    private void ApplyPreviewHideFlags(
        GameObject previewObject)
    {
        if (previewObject == null)
        {
            return;
        }


        Transform[] previewTransforms =
            previewObject.GetComponentsInChildren<Transform>(
                true
            );


        for (int i = 0;
             i < previewTransforms.Length;
             i++)
        {
            Transform previewTransform =
                previewTransforms[i];

            if (previewTransform == null)
            {
                continue;
            }


            previewTransform.gameObject.hideFlags =
                HideFlags.HideAndDontSave;
        }
    }


    // <변경부분>
    // Prefab을 Ghost Preview로 복제했을 때
    // Scene 편집에 실제 영향을 줄 수 있는 Component는 비활성화한다.
    //
    // 렌더링 자체에 필요한 Spine / Renderer 계열은 건드리지 않는다.
    private void DisablePrefabPreviewSideEffects(
        GameObject previewObject)
    {
        if (previewObject == null)
        {
            return;
        }


        Component[] components =
            previewObject.GetComponentsInChildren<Component>(
                true
            );


        for (int i = 0;
             i < components.Length;
             i++)
        {
            Component component =
                components[i];

            if (component == null)
            {
                continue;
            }


            Collider2D collider2D =
                component as Collider2D;

            if (collider2D != null)
            {
                collider2D.enabled =
                    false;

                continue;
            }


            Collider collider3D =
                component as Collider;

            if (collider3D != null)
            {
                collider3D.enabled =
                    false;

                continue;
            }


            AudioSource audioSource =
                component as AudioSource;

            if (audioSource != null)
            {
                audioSource.enabled =
                    false;

                continue;
            }


            ParticleSystem particleSystem =
                component as ParticleSystem;

            if (particleSystem != null)
            {
                particleSystem.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );

                continue;
            }


            // URP Light2D를 직접 참조하지 않아
            // BackgroundManager.cs에 추가 using을 만들지 않고 처리한다.
            //
            // Preview Light가 마우스를 따라 움직이며
            // Scene 실제 조명을 바꾸지 않도록 비활성화한다.
            if (component.GetType().FullName ==
                    "UnityEngine.Rendering.Universal.Light2D" &&
                component is Behaviour lightBehaviour)
            {
                lightBehaviour.enabled =
                    false;
            }
        }
    }


    // <변경부분>
    // 새 Prefab Ghost Preview를 생성하고
    // Prefab 원본의 Renderer 상대 정렬값을 기억한다.
    private void CreatePrefabDecorationPreview(
        GameObject previewPrefab)
    {
        DestroyDecorationPreview();


        if (previewPrefab == null)
        {
            return;
        }


        decorationPreviewObject =
            UnityEngine.Object.Instantiate(
                previewPrefab
            );


        if (decorationPreviewObject == null)
        {
            return;
        }


        decorationPreviewObject.name =
            "__DecorationPrefabPreview";


        decorationPreviewSourcePrefab =
            previewPrefab;


        ApplyPreviewHideFlags(
            decorationPreviewObject
        );


        DisablePrefabPreviewSideEffects(
            decorationPreviewObject
        );


        decorationPreviewRenderers =
            decorationPreviewObject
                .GetComponentsInChildren<Renderer>(
                    true
                );


        decorationPreviewRelativeSortingOrders =
            new int[
                decorationPreviewRenderers.Length
            ];


        decorationPreviewOriginalSpriteColors =
            new Color[
                decorationPreviewRenderers.Length
            ];


        bool hasVisualBounds =
    false;

        Bounds combinedVisualBounds =
            new Bounds();


        for (int i = 0;
             i < decorationPreviewRenderers.Length;
             i++)
        {
            Renderer previewRenderer =
                decorationPreviewRenderers[i];


            if (previewRenderer == null)
            {
                continue;
            }


            // Prefab Asset 안에서 설정해 둔 상대 Order를 보존한다.
            decorationPreviewRelativeSortingOrders[i] =
                previewRenderer.sortingOrder;


            SpriteRenderer spriteRenderer =
                previewRenderer as SpriteRenderer;


            if (spriteRenderer != null)
            {
                decorationPreviewOriginalSpriteColors[i] =
                    spriteRenderer.color;
            }


            // <변경부분>
            // SpriteRenderer / Spine MeshRenderer 등
            // 실제로 화면에 표시되는 모든 Renderer의 Bounds를 합친다.
            if (!previewRenderer.enabled ||
                !previewRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }


            Bounds rendererBounds =
                previewRenderer.bounds;


            if (rendererBounds.size.sqrMagnitude <=
                Mathf.Epsilon)
            {
                continue;
            }


            if (!hasVisualBounds)
            {
                combinedVisualBounds =
                    rendererBounds;

                hasVisualBounds =
                    true;
            }
            else
            {
                combinedVisualBounds.Encapsulate(
                    rendererBounds
                );
            }
        }


        // <변경부분>
        // Prefab Root/Pivot에서 실제 시각적 중심까지의
        // 상대 위치를 한 번 저장한다.
        //
        // 이후 마우스를 움직일 때 이 Offset을 반대로 적용하면
        // Pivot 위치에 관계없이 오브젝트 중심이 커서에 맞는다.
        if (hasVisualBounds)
        {
            decorationPreviewVisualCenterOffset =
                combinedVisualBounds.center -
                decorationPreviewObject.transform.position;

            // 2D 배치에서는 X / Y 중심만 보정한다.
            decorationPreviewVisualCenterOffset.z =
                0f;
        }
        else
        {
            // 표시 가능한 Renderer가 없는 Prefab은
            // 기존처럼 Root/Pivot 기준 Preview를 사용한다.
            decorationPreviewVisualCenterOffset =
                Vector3.zero;
        }
    }


    // <변경부분>
    // Prefab Preview의 시각 설정을 실제 Decoration 배치 규칙과 맞춘다.
    private void ApplyPrefabDecorationPreviewVisual(
        int sortingLayerId,
        int baseSortingOrder,
        Color previewColor)
    {
        if (decorationPreviewRenderers == null ||
            decorationPreviewRelativeSortingOrders == null ||
            decorationPreviewOriginalSpriteColors == null)
        {
            return;
        }


        int rendererCount =
            Mathf.Min(
                decorationPreviewRenderers.Length,
                decorationPreviewRelativeSortingOrders.Length
            );


        for (int i = 0;
             i < rendererCount;
             i++)
        {
            Renderer previewRenderer =
                decorationPreviewRenderers[i];


            if (previewRenderer == null)
            {
                continue;
            }


            SpriteRenderer spriteRenderer =
                previewRenderer as SpriteRenderer;


            if (spriteRenderer != null)
            {
                // 실제 Prefab Decoration 배치와 동일하게
                // SpriteRenderer는 Decoration Sorting 기준으로 맞춘다.
                spriteRenderer.sortingLayerID =
                    sortingLayerId;

                spriteRenderer.sortingOrder =
                    baseSortingOrder +
                    decorationPreviewRelativeSortingOrders[i];


                Color originalColor =
                    decorationPreviewOriginalSpriteColors[i];


                spriteRenderer.color =
                    new Color(
                        originalColor.r *
                        previewColor.r,

                        originalColor.g *
                        previewColor.g,

                        originalColor.b *
                        previewColor.b,

                        originalColor.a *
                        previewColor.a
                    );
            }

            // Spine SkeletonRenderer가 사용하는 MeshRenderer 등은
            // 실제 BackgroundManager 배치 코드에서도
            // Sorting Layer / Material을 강제로 변경하지 않으므로
            // Preview에서도 원본 설정을 그대로 유지한다.
        }
    }


    // <변경부분>
    // Scene View 마우스 위치를 실제 Background 평면 Z=0으로 변환한다.
    private bool TryGetSceneWorldPosition(
        Event currentEvent,
        out Vector3 worldPosition)
    {
        worldPosition =
            Vector3.zero;

        if (currentEvent == null)
        {
            return false;
        }


        Ray mouseRay =
            HandleUtility.GUIPointToWorldRay(
                currentEvent.mousePosition
            );


        Plane worldPlane =
            new Plane(
                Vector3.forward,
                Vector3.zero
            );


        if (!worldPlane.Raycast(
                mouseRay,
                out float enter))
        {
            return false;
        }


        worldPosition =
            mouseRay.GetPoint(
                enter
            );

        worldPosition.z =
            0f;


        return true;
    }


    // <변경부분>
    // Free Placement의 Exact Sprite / Exact Prefab Ghost Preview를
    // 현재 Scene View 마우스 위치에 표시한다.
    private void UpdateDecorationPreview(
        BackgroundManager manager,
        Vector3 worldPosition)
    {
        if (!isDecorationPaintMode ||
            manager == null)
        {
            DestroyDecorationPreview();
            return;
        }


        // =========================================================
        // Exact Prefab
        // =========================================================
        //
        // Prefab 전체를 Ghost Preview로 복제한다.
        if (manager.TryGetFreeDecorationPrefabPreviewData(
                out GameObject previewPrefab,
                out int prefabSortingLayerId,
                out int prefabBaseSortingOrder,
                out Color prefabPreviewColor))
        {
            // 처음 Preview를 만들거나,
            // Inspector에서 다른 Prefab을 선택했다면
            // 전체 Preview를 다시 생성한다.
            if (decorationPreviewObject == null ||
                decorationPreviewSourcePrefab !=
                    previewPrefab)
            {
                CreatePrefabDecorationPreview(
                    previewPrefab
                );
            }


            if (decorationPreviewObject == null)
            {
                return;
            }


            // <변경부분>
            // Prefab Root/Pivot가 아니라
            // 실제 Renderer 전체의 시각적 중심이
            // 마우스 커서에 오도록 위치를 보정한다.
            Vector3 centeredPreviewPosition =
                worldPosition -
                decorationPreviewVisualCenterOffset;


            centeredPreviewPosition.z =
                worldPosition.z;


            decorationPreviewObject.transform.position =
                centeredPreviewPosition;


            ApplyPrefabDecorationPreviewVisual(
                prefabSortingLayerId,
                prefabBaseSortingOrder,
                prefabPreviewColor
            );


            return;
        }


        // =========================================================
        // Exact Sprite
        // =========================================================
        if (!manager.TryGetFreeDecorationPreviewData(
                out Sprite previewSprite,
                out Material previewMaterial,
                out int sortingLayerId,
                out int sortingOrder,
                out Color previewColor))
        {
            DestroyDecorationPreview();
            return;
        }


        // 직전에 Prefab Preview를 사용 중이었다면
        // Sprite Preview로 전환하기 전에 정리한다.
        if (decorationPreviewSourcePrefab != null)
        {
            DestroyDecorationPreview();
        }


        if (decorationPreviewObject == null ||
            decorationPreviewRenderer == null)
        {
            decorationPreviewObject =
                new GameObject(
                    "__DecorationPreview"
                );


            decorationPreviewObject.hideFlags =
                HideFlags.HideAndDontSave;


            decorationPreviewRenderer =
                decorationPreviewObject
                    .AddComponent<SpriteRenderer>();
        }


        decorationPreviewObject.transform.position =
            worldPosition;


        decorationPreviewRenderer.sprite =
            previewSprite;


        decorationPreviewRenderer.sharedMaterial =
            previewMaterial;


        decorationPreviewRenderer.sortingLayerID =
            sortingLayerId;


        decorationPreviewRenderer.sortingOrder =
            sortingOrder;


        decorationPreviewRenderer.color =
            previewColor;
    }


    // <변경부분>
    // 현재 BackgroundMapData가 사용할 DecorationPalette를
    // BackgroundManager Inspector에서도 바로 지정할 수 있게 한다.
    private void DrawDecorationPaletteField()
    {
        SerializedProperty currentMapDataProperty =
            serializedObject.FindProperty(
                "currentMapData"
            );


        BackgroundMapData mapData =
            currentMapDataProperty != null
                ? currentMapDataProperty.objectReferenceValue
                    as BackgroundMapData
                : null;


        if (mapData == null)
        {
            EditorGUILayout.HelpBox(
                "Current Map Data를 먼저 연결하면 " +
                "스테이지용 Decoration Palette를 지정할 수 있습니다.",
                MessageType.Info
            );

            return;
        }


        EditorGUI.BeginChangeCheck();


        DecorationPaletteData newPalette =
            EditorGUILayout.ObjectField(
                "Decoration Palette",
                mapData.DecorationPalette,
                typeof(DecorationPaletteData),
                false
            ) as DecorationPaletteData;


        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                mapData,
                "Change Decoration Palette"
            );

            mapData.DecorationPalette =
                newPalette;

            EditorUtility.SetDirty(
                mapData
            );
        }


        // Palette가 아직 없는 기존 Scene은
        // 기존 decorationSets를 계속 사용할 수 있게 보여준다.
        if (mapData.DecorationPalette == null)
        {
            EditorGUILayout.HelpBox(
                "Decoration Palette가 아직 연결되지 않았습니다. " +
                "현재는 기존 Legacy Decoration Sets를 사용합니다.",
                MessageType.Warning
            );


            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(
                    "decorationSets"
                ),
                new GUIContent(
                    "Legacy Decoration Sets"
                ),
                true
            );
        }
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        BackgroundManager manager =
            (BackgroundManager)target;


        // =========================================================
        // 맵 타일
        // =========================================================
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUILayout.LabelField(
            "맵 타일",
            EditorStyles.boldLabel
        );

        GUILayout.Space(5);


        // 맵 기본 설정
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "backgroundWidth"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "backgroundHeight"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "xOffset"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "yOffset"
            )
        );


        GUILayout.Space(5);


        // 배경 위치
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "backgroundOriginOffset"
            )
        );


        GUILayout.Space(5);


        // 배경 타일 기본 구성
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "backgroundTileParent"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "backgroundTilePrefab"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "backgroundTileSets"
            ),
            true
        );


        GUILayout.Space(5);


        // 배경 타일 색상
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "useDarkBackground"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "darkBackgroundColor"
            )
        );


        GUILayout.Space(5);


        // 타일 페인트 설정
        SerializedProperty tileBrushSourceModeProperty =
            serializedObject.FindProperty(
                "backgroundTileBrushSourceMode"
            );


        EditorGUILayout.PropertyField(
            tileBrushSourceModeProperty,
            new GUIContent(
                "Source Mode"
            )
        );


        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "paintTileType"
            ),
            new GUIContent(
                "Tile Type"
            )
        );


        if (tileBrushSourceModeProperty.enumValueIndex ==
     (int)BackgroundTileBrushSourceMode.ExactSprite)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(
                    "paintTileSprite"
                ),
                new GUIContent(
                    "Exact Sprite"
                )
            );


            SerializedProperty paintTileTypeProperty =
                serializedObject.FindProperty(
                    "paintTileType"
                );


            if (paintTileTypeProperty.enumValueIndex ==
                (int)BackgroundTileType.All)
            {
                EditorGUILayout.HelpBox(
                    "Exact Sprite 모드에서는 All이 아니라 실제 Tile Type을 지정해주세요.",
                    MessageType.Warning
                );
            }
        }
        else if (tileBrushSourceModeProperty.enumValueIndex ==
                 (int)BackgroundTileBrushSourceMode.ExactPrefab)
        {
            SerializedProperty paintTilePrefabProperty =
                serializedObject.FindProperty(
                    "paintTilePrefab"
                );


            // Scene Object가 아니라
            // Project의 Prefab Asset만 선택할 수 있도록 한다.
            paintTilePrefabProperty.objectReferenceValue =
                EditorGUILayout.ObjectField(
                    "Exact Prefab",
                    paintTilePrefabProperty.objectReferenceValue,
                    typeof(GameObject),
                    false
                );


            SerializedProperty paintTileTypeProperty =
                serializedObject.FindProperty(
                    "paintTileType"
                );


            if (paintTileTypeProperty.enumValueIndex ==
                (int)BackgroundTileType.All)
            {
                EditorGUILayout.HelpBox(
                    "Exact Prefab 모드에서는 All이 아니라 실제 Tile Type을 지정해주세요.",
                    MessageType.Warning
                );
            }
        }


        GUILayout.Space(3);


        GUILayout.Space(3);


        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "paintX"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "paintY"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "brushSize"
            )
        );


        GUILayout.Space(5);


        // All 타입 랜덤 생성 비율
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "allTileWeights"
            ),
            true
        );


        GUILayout.Space(8);


        EditorGUILayout.LabelField(
            "맵 데이터",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "currentMapData"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "newMapDataName"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "mapDataSaveFolder"
            )
        );


        // 버튼을 누르기 전에
        // 위 Inspector에서 변경한 값을 실제 Object에 먼저 반영한다.
        serializedObject.ApplyModifiedProperties();


        GUILayout.Space(10);


        if (GUILayout.Button(
                "전체 배경 생성"))
        {
            Debug.Log(
                "전체 배경 생성 버튼 클릭됨"
            );

            manager.GenerateBackground(
                BackgroundTileType.All
            );
        }


        if (GUILayout.Button(
                "배경 타일 전체 삭제"))
        {
            manager.ClearBackground();
        }


        GUILayout.Space(5);


        if (GUILayout.Button(
                "새 맵 데이터 생성"))
        {
            manager.CreateNewMapDataAsset();
        }


        if (GUILayout.Button(
                "현재 맵 데이터 저장"))
        {
            manager.SaveCurrentMapToData();
        }


        if (GUILayout.Button(
                "맵 데이터 불러오기"))
        {
            manager.LoadMapFromData();
        }


        GUILayout.Space(5);


        if (GUILayout.Button(
                "입력 좌표 타일 칠하기"))
        {
            manager.PaintSelectedTileByInput();
        }


        isScenePaintMode =
            GUILayout.Toggle(
                isScenePaintMode,
                "씬 페인트 모드",
                "Button"
            );


        EditorGUILayout.EndVertical();


        GUILayout.Space(10);


        // 다음 Serialized Property를 그리기 전에
        // SerializedObject 상태를 다시 동기화한다.
        serializedObject.Update();


        // =========================================================
        // 데코레이션
        // =========================================================
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUILayout.LabelField(
            "데코레이션",
            EditorStyles.boldLabel
        );

        GUILayout.Space(5);


        // 데코레이션 기본 설정
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "decorationPrefab"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "decorationParent"
            )
        );


        GUILayout.Space(5);


        DrawDecorationPaletteField();


        GUILayout.Space(5);


        // 데코레이션 위치 / 테스트 설정
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "testDecorationType"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "testDecorationX"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "testDecorationY"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "decorationOffset"
            )
        );


        GUILayout.Space(5);


        // 데코레이션 색상 설정
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "useDarkDecoration"
            )
        );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "decorationBrightness"
            )
        );


        GUILayout.Space(5);


        // 데코레이션 브러시 설정
        SerializedProperty brushSourceModeProperty =
            serializedObject.FindProperty(
                "decorationBrushSourceMode"
            );

        SerializedProperty placementModeProperty =
            serializedObject.FindProperty(
                "decorationPlacementMode"
            );


        EditorGUILayout.PropertyField(
            brushSourceModeProperty,
            new GUIContent(
                "Source Mode"
            )
        );


        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "paintDecorationType"
            ),
            new GUIContent(
                "Decoration Type"
            )
        );


        if (brushSourceModeProperty.enumValueIndex ==
      (int)DecorationBrushSourceMode.ExactSprite)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(
                    "paintDecorationSprite"
                ),
                new GUIContent(
                    "Exact Sprite"
                )
            );
        }

        else if (brushSourceModeProperty.enumValueIndex ==
         (int)DecorationBrushSourceMode.ExactPrefab)
        {
            SerializedProperty paintDecorationPrefabProperty =
                serializedObject.FindProperty(
                    "paintDecorationPrefab"
                );


            paintDecorationPrefabProperty.objectReferenceValue =
                EditorGUILayout.ObjectField(
                    "Exact Prefab",
                    paintDecorationPrefabProperty.objectReferenceValue,
                    typeof(GameObject),
                    false
                );
        }


        GUILayout.Space(3);


        // <변경부분>
        // 동일 Anchor 안에서 새로 배치할 장식물의
        // 앞뒤 순서를 선택한다.
        EditorGUILayout.IntSlider(
            serializedObject.FindProperty(
                "paintDecorationLayerOffset"
            ),
            -3,
            3,
            new GUIContent(
                "Decoration Layer"
            )
        );


        EditorGUILayout.HelpBox(
            "-3 = 뒤쪽 / 0 = 기본 / +3 = 앞쪽",
            MessageType.None
        );


        GUILayout.Space(3);


        EditorGUILayout.PropertyField(
            placementModeProperty,
            new GUIContent(
                "Placement Mode"
            )
        );


        if (placementModeProperty.enumValueIndex ==
            (int)DecorationPlacementMode.Free)
        {
            GUILayout.Space(3);


            if (manager.HasFreePlacementAnchor)
            {
                EditorGUILayout.HelpBox(
                    $"Anchor Tile : ({manager.FreePlacementAnchorX}, {manager.FreePlacementAnchorY})",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Free Placement를 사용하려면 Scene View에서 Anchor Tile을 먼저 선택해주세요.",
                    MessageType.Warning
                );
            }


            string anchorButtonLabel =
                isSelectingDecorationAnchor
                    ? "Anchor Tile 선택 취소"
                    : "Scene에서 Anchor Tile 선택";


            if (GUILayout.Button(
                    anchorButtonLabel))
            {
                isSelectingDecorationAnchor =
                    !isSelectingDecorationAnchor;

                if (isSelectingDecorationAnchor)
                {
                    isScenePaintMode =
                        false;

                    isDecorationPaintMode =
                        false;

                    isDecorationEraseMode =
                        false;

                    DestroyDecorationPreview();
                }


                SceneView.RepaintAll();
            }


            if (brushSourceModeProperty.enumValueIndex ==
           (int)DecorationBrushSourceMode.ExactSprite ||
       brushSourceModeProperty.enumValueIndex ==
           (int)DecorationBrushSourceMode.ExactPrefab)
            {
                EditorGUILayout.HelpBox(
                    "배치 모드를 켜면 선택한 Sprite가 반투명 Preview로 마우스를 따라갑니다.",
                    MessageType.None
                );
            }


            EditorGUILayout.HelpBox(
                "Free Placement에서는 같은 Anchor Tile에 여러 장식물을 배치할 수 있습니다.",
                MessageType.None
            );
        }
        else
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(
                    "preventDuplicateDecoration"
                )
            );
        }


        GUILayout.Space(5);


        // 자동 생성 규칙
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "decorationSpawnRules"
            ),
            true
        );


        serializedObject.ApplyModifiedProperties();


        GUILayout.Space(10);


        if (GUILayout.Button(
                "데코레이션 전체 삭제"))
        {
            manager.ClearDecorations();
        }


        if (GUILayout.Button(
                "규칙 기반 데코레이션 생성"))
        {
            Debug.Log(
                "규칙 기반 데코레이션 생성 버튼 클릭됨"
            );

            manager.GenerateDecorationsByRules();
        }


        if (GUILayout.Button(
                "테스트 데코레이션 생성"))
        {
            manager.SpawnTestDecoration();
        }


        GUILayout.Space(5);


        bool newDecorationPaintMode =
    GUILayout.Toggle(
        isDecorationPaintMode,
        "데코레이션 배치 모드",
        "Button"
    );


        if (newDecorationPaintMode !=
            isDecorationPaintMode)
        {
            isDecorationPaintMode =
                newDecorationPaintMode;

            if (isDecorationPaintMode)
            {
                isScenePaintMode =
                    false;

                isDecorationEraseMode =
                    false;

                isSelectingDecorationAnchor =
                    false;
            }
            else
            {
                DestroyDecorationPreview();
            }
        }


        bool newDecorationEraseMode =
            GUILayout.Toggle(
                isDecorationEraseMode,
                "데코레이션 삭제 모드",
                "Button"
            );


        if (newDecorationEraseMode !=
            isDecorationEraseMode)
        {
            isDecorationEraseMode =
                newDecorationEraseMode;

            if (isDecorationEraseMode)
            {
                isScenePaintMode =
                    false;

                isDecorationPaintMode =
                    false;

                isSelectingDecorationAnchor =
                    false;

                DestroyDecorationPreview();
            }
        }


        EditorGUILayout.EndVertical();


        GUILayout.Space(10);


        serializedObject.Update();


        // =========================================================
        // 라이트 환경값
        // =========================================================
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        EditorGUILayout.LabelField(
            "라이트 환경값",
            EditorStyles.boldLabel
        );

        GUILayout.Space(5);


        EditorGUILayout.PropertyField(
    serializedObject.FindProperty(
        "environmentLightingController"
    )
);


        // <변경부분>
        // 현재 Scene에서 사용할 공용 CloudShadowRig의
        // CloudShadowController 연결.
        EditorGUILayout.PropertyField(
       serializedObject.FindProperty(
           "cloudShadowController"
       )
   );


        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(
                "fireflyEnvironmentController"
            )
        );


        serializedObject.ApplyModifiedProperties();


        GUILayout.Space(10);


        // 현재 Scene의 LightingRig 값을
        // EnvironmentVisualProfile에 저장한다.
        if (GUILayout.Button(
                "조명 프로필 저장"))
        {
            manager.SaveCurrentLightingToProfile();
        }


        // EnvironmentVisualProfile의 값을
        // 현재 Scene LightingRig로 다시 불러온다.
        if (GUILayout.Button(
                "조명 프로필 로드"))
        {
            manager.ApplyCurrentLightingProfile();
        }


        EditorGUILayout.EndVertical();
    }



    private void HandleSceneGUI(SceneView sceneView)
    {
        BackgroundManager manager =
            (BackgroundManager)target;

        Event currentEvent =
            Event.current;


        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            DestroyDecorationPreview();
            return;
        }


        // Ghost Preview가 마우스 이동 이벤트를 계속 받을 수 있게 한다.
        sceneView.wantsMouseMove =
            true;


        // 현재 Free Placement Anchor를 Scene View에 표시한다.
        if (manager.UsesFreeDecorationPlacement &&
            manager.TryGetFreePlacementAnchorWorldPosition(
                out Vector3 anchorWorldPosition))
        {
            Handles.Label(
                anchorWorldPosition +
                new Vector3(
                    0f,
                    0.18f,
                    0f
                ),
                $"ANCHOR ({manager.FreePlacementAnchorX}, {manager.FreePlacementAnchorY})"
            );

            Handles.DrawWireDisc(
                anchorWorldPosition,
                Vector3.forward,
                0.08f
            );
        }


        if (!isScenePaintMode &&
            !isDecorationPaintMode &&
            !isDecorationEraseMode &&
            !isSelectingDecorationAnchor)
        {
            DestroyDecorationPreview();
            return;
        }


        // Alt는 Scene View 카메라 조작 우선.
        if (currentEvent.alt)
        {
            return;
        }


        bool hasFreeWorldPosition =
            TryGetSceneWorldPosition(
                currentEvent,
                out Vector3 freeWorldPosition
            );


        // Free + Exact Sprite일 때만 내부적으로 Preview가 표시된다.
        if (hasFreeWorldPosition)
        {
            UpdateDecorationPreview(
                manager,
                freeWorldPosition
            );
        }
        else
        {
            DestroyDecorationPreview();
        }


        if (currentEvent.type ==
            EventType.MouseMove)
        {
            sceneView.Repaint();
        }


        // =========================================================
        // Anchor Tile 선택
        // 다른 Paint / Erase보다 항상 우선 처리한다.
        // =========================================================
        if (isSelectingDecorationAnchor &&
            currentEvent.type == EventType.MouseDown &&
            currentEvent.button == 0)
        {
            if (hasFreeWorldPosition &&
                manager.SetFreePlacementAnchorByWorldPosition(
                    freeWorldPosition))
            {
                isSelectingDecorationAnchor =
                    false;

                EditorUtility.SetDirty(
                    manager
                );

                Repaint();
                SceneView.RepaintAll();
            }
            else
            {
                Debug.LogWarning(
                    "Anchor Tile 선택 실패: 실제 BackgroundTile 위를 클릭해주세요."
                );
            }


            currentEvent.Use();
            return;
        }


        bool isLeftMouseDown =
            currentEvent.type == EventType.MouseDown &&
            currentEvent.button == 0;

        bool isLeftMouseDrag =
            currentEvent.type == EventType.MouseDrag &&
            currentEvent.button == 0;


        if (!isLeftMouseDown &&
            !isLeftMouseDrag)
        {
            return;
        }


        // Free Placement는 정밀 배치이므로
        // Drag 연속 생성 없이 클릭만 사용한다.
        if (isDecorationPaintMode &&
            manager.UsesFreeDecorationPlacement &&
            isLeftMouseDrag)
        {
            return;
        }


        Ray mouseRay =
     HandleUtility.GUIPointToWorldRay(
         currentEvent.mousePosition
     );


        // 기존 Grid Paint / Erase는
        // 기존 좌표 계산 방식을 그대로 유지한다.
        Vector3 worldPosition =
            mouseRay.origin;


        if (isDecorationEraseMode)
        {
            // Sprite / Prefab, Grid / Free 여부와 관계없이
            // 실제 Scene View의 Z=0 Background Plane 좌표를 사용한다.
            //
            // 이렇게 해야 Anchor Tile보다 크게 그려지는
            // Prefab Decoration의 실제 외형을 정확하게 클릭할 수 있다.
            if (!hasFreeWorldPosition)
            {
                return;
            }


            manager.EraseDecorationByWorldPosition(
                freeWorldPosition
            );
        }
        else if (isDecorationPaintMode)
        {
            if (manager.UsesFreeDecorationPlacement)
            {
                if (!hasFreeWorldPosition)
                {
                    return;
                }

                manager.PaintDecorationByWorldPosition(
                    freeWorldPosition
                );
            }
            else
            {
                manager.PaintDecorationByWorldPosition(
                    worldPosition
                );
            }
        }
        else if (isScenePaintMode)
        {
            manager.PaintBackgroundTileByWorldPosition(
                worldPosition
            );
        }


        EditorUtility.SetDirty(
            manager
        );


        currentEvent.Use();
    }
}

#endif

[System.Serializable]
public class DecorationSet
{
    // <변경부분>
    // Inspector에서 이 장식물 묶음을 구분하기 위한 이름.
    //
    // 예:
    // Forest Trees
    // Forest Rocks
    // Shop Props
    public string SetName;


    // 장식물 종류를 구분하는 타입
    public DecorationType DecorationType;


    // 같은 타입 안에서 랜덤으로 사용할 여러 장식물 스프라이트
    public List<Sprite> DecorationSprites = new List<Sprite>();


    // <변경부분>
    // 이 Decoration이 자신의 배치 타일 이외에
    // 추가로 어떤 Background Grid 위의 Event Actor를
    // 자신의 뒤로 가릴 수 있는지 지정한다.
    //
    // 좌표는 Decoration이 배치된 Grid를 (0, 0)으로 보는 상대 좌표다.
    //
    // 자기 자신의 타일 (0, 0)은 코드에서 항상 자동으로 포함되므로
    // 이 목록에는 "추가 영향 타일"만 넣으면 된다.
    //
    // 현재 DEVORYA 아이소메트릭 기준으로
    // 바로 위쪽의 3개 인접 타일은:
    //
    // (1, 0)
    // (0, 1)
    // (1, 1)
    //
    // 큰 나무처럼 더 넓은 Decoration은
    // 필요한 좌표를 추가해서 영향권을 자유롭게 확장할 수 있다.
    public List<Vector2Int> ActorOcclusionOffsets =
        new List<Vector2Int>();
}

[System.Serializable]
public class DecorationSpawnRule
{
    // <변경부분> 장식물이 생성될 수 있는 배경 타일 타입
    public BackgroundTileType TargetTileType;

    // <변경부분> 생성할 장식물 타입
    public DecorationType DecorationType;

    // <변경부분> 해당 장식물이 생성될 확률
    [Range(0f, 1f)]
    public float SpawnChance = 0.3f;
}

