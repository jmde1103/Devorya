using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public int X { get; private set; } // 타일의 X 좌표
    public int Y { get; private set; } // 타일의 Y 좌표

    // <변경부분> 현재 타일에 적용된 TileData
    public TileData CurrentTileData { get; private set; }

    // <변경부분> 현재 타일의 지형 타입
    public TileType TileType { get; private set; }

    public List<TileEffectType> TileEffects { get; private set; }

    // <변경부분> 현재 타일 위에 기물이 올라갈 수 있는지 여부
    public bool IsWalkable { get; private set; } = true;

    // <변경부분> 현재 타일 위에 장애물이 있는지 여부
    public bool HasObstacle { get; private set; } = false;


    private Color originalColor; // 원래 타일 색상 적용
    [SerializeField]
    private Color highlightColor = Color.yellowNice; // 이동 가능 타일 표시 색

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // SpriteRenderer를 먼저 자기 오브젝트에서 찾음
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 자기 오브젝트에 없으면 자식 오브젝트에서 찾음
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // 그래도 없으면 오류 출력 후 종료
        if (spriteRenderer == null)
        {
            Debug.LogError($"{gameObject.name}에 SpriteRenderer가 없습니다.");
            return;
        }

        // 타일 효과 리스트 초기화
        TileEffects = new List<TileEffectType>();

        // 원래 색 저장
        originalColor = spriteRenderer.color;
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    // <변경부분> 타일 생성 직후 TileData 기준으로 초기화하는 함수
    public void Initialize(int x, int y, TileData tileData)
    {
        // 좌표 저장
        X = x;
        Y = y;

        // <변경부분> 전달받은 TileData를 실제 타일에 적용
        ApplyTileData(tileData);
    }
    // <변경부분> TileData를 기준으로 타일의 타입/스프라이트/효과/이동 가능 여부를 한 번에 적용
    public void ApplyTileData(TileData tileData)
    {
        if (tileData == null)
        {
            Debug.LogWarning($"{gameObject.name}에 적용할 TileData가 없습니다.");
            return;
        }

        // <변경부분> 현재 적용된 데이터 저장
        CurrentTileData = tileData;

        // <변경부분> 지형 타입 저장
        TileType = tileData.tileType;

        // <변경부분> 이동 가능 여부 / 장애물 여부 저장
        IsWalkable = tileData.isWalkable;
        HasObstacle = tileData.hasObstacle;

        // <변경부분> 기본 타일 효과 목록 갱신
        if (TileEffects == null)
        {
            TileEffects = new List<TileEffectType>();
        }

        TileEffects.Clear();

        if (tileData.defaultTileEffects != null)
        {
            for (int i = 0; i < tileData.defaultTileEffects.Count; i++)
            {
                TileEffectType effectType = tileData.defaultTileEffects[i];

                if (effectType == TileEffectType.None)
                {
                    continue;
                }

                if (TileEffects.Contains(effectType) == false)
                {
                    TileEffects.Add(effectType);
                }
            }
        }

        // <변경부분> 스프라이트 교체
        if (spriteRenderer != null && tileData.tileSprite != null)
        {
            spriteRenderer.sprite = tileData.tileSprite;
        }

        // <변경부분> 하이라이트 복구 기준 색상 갱신
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    // <변경부분> TileType만으로 타일을 바꾸는 기존 함수는 직접 데이터 적용 구조로 이동했으므로 사용하지 않음
    // 타일 변경이 필요하면 BoardManager.ChangeTileData(...) 또는 tile.ApplyTileData(tileData)를 사용
    public void ChangeTileType(TileType newTileType)
    {
        TileType = newTileType;

        Debug.LogWarning("ChangeTileType(TileType)는 TileType만 변경하므로 스프라이트/효과가 갱신되지 않습니다. TileData 기반 ApplyTileData 사용을 권장합니다.");
    }

    public void AddTileEffect(TileEffectType effectType) // 타일 효과 추가
    {
        if (!TileEffects.Contains(effectType)) // 중복 효과 방지
        {
            TileEffects.Add(effectType);
        }
    }

    public void RemoveTileEffect(TileEffectType effectType) // 타일 효과 제거
    {
        TileEffects.Remove(effectType);
    }

    public bool HasTileEffect(TileEffectType effectType) // 특정 효과를 가지고 있는지 확인
    {
        return TileEffects.Contains(effectType);
    }

    public void ShowHighlight() // 이동 가능 타일 표시
    {
        spriteRenderer.color = highlightColor;
    }

    public void HideHighlight()  // 타일 표시 원상 복구
    {
        spriteRenderer.color = originalColor;
    }

    public Vector2Int GetGridPosition()  // 현재 좌표 반환
    {
        return new Vector2Int(X, Y);
    }

    private void OnMouseDown()
    {
        // BattleManager가 없으면 종료
        if (BattleManager.Instance == null)
        {
            return;
        }

        // <변경부분> 타일 클릭을 BattleManager에 전달
        // 타일 위에 기물이 있으면 기물 선택/정보 표시/공격 확인 처리
        // 타일 위에 기물이 없으면 이동 처리
        BattleManager.Instance.SelectTile(this);
    }
}
