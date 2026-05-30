using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public int X { get; private set; }// 타일의 X 좌표
    public int Y { get; private set; }// 타일의 Y 좌표
    public TileType TileType { get; private set; }// 현재 타일의 지형 속성

    public List<TileEffectType> TileEffects { get; private set; }
    public bool IsWalkable { get; private set; } = true;// 이 타일 위에 기물이 올라갈 수 있는지 여부
    public bool HasObstacle { get; private set; } = false;// 이 타일 위에 장애물이 있는지 여부


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


    // 타일 생성 직후 초기화하는 함수
    public void Initialize(int x, int y, TileType tileType)
    {
        // 좌표 저장
        X = x;
        Y = y;

        // 지형 속성 저장
        TileType = tileType;
    }
    public void ChangeTileType(TileType newTileType)  // 타일의 지형을 변경하는 함수
    {
        TileType = newTileType; // 새로운 지형 타입 저장

        // TODO: 타일 비주얼(Sprite/Spine) 교체
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


    // <변경부분> 타일을 마우스로 클릭했을 때 Unity가 자동 호출하는 함수
    private void OnMouseDown()
    {
        // BattleManager가 있으면 클릭한 타일 전달
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SelectTile(this);
        }
    }
}
