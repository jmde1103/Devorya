using System.Collections;
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

    [Header("Highlight Animation")]
    // <변경부분> 타일 하이라이트 색상 변경을 부드럽게 처리할지 여부
    [SerializeField] private bool useHighlightFade = true;

    // <변경부분> 타일 하이라이트 색상 전환 시간
    [SerializeField] private float highlightFadeDuration = 0.12f;

    // <변경부분> 현재 실행 중인 타일 색상 전환 코루틴
    private Coroutine highlightColorCoroutine;

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
        // <변경부분> 하이라이트 색상으로 부드럽게 전환
        ChangeTileColorSmooth(highlightColor);
    }

    public void HideHighlight()  // 타일 표시 원상 복구
    {
        // <변경부분> 원래 타일 색상으로 부드럽게 복귀
        ChangeTileColorSmooth(originalColor);
    }

    // <변경부분> 타일 색상을 부드럽게 변경하는 함수
    private void ChangeTileColorSmooth(Color targetColor)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // <변경부분> 기존 색상 전환 코루틴이 있으면 중단하고 새 목표 색상으로 전환
        if (highlightColorCoroutine != null)
        {
            StopCoroutine(highlightColorCoroutine);
            highlightColorCoroutine = null;
        }

        // <변경부분> 페이드가 꺼져 있거나 시간이 0 이하이면 즉시 색상 변경
        if (useHighlightFade == false || highlightFadeDuration <= 0f)
        {
            spriteRenderer.color = targetColor;
            return;
        }

        highlightColorCoroutine = StartCoroutine(ChangeTileColorSmoothRoutine(targetColor));
    }

    // <변경부분> 현재 색상에서 목표 색상까지 부드럽게 보간하는 코루틴
    private IEnumerator ChangeTileColorSmoothRoutine(Color targetColor)
    {
        Color startColor = spriteRenderer.color;

        float elapsed = 0f;
        float duration = Mathf.Max(0.001f, highlightFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // <변경부분> 직선 보간보다 조금 더 자연스럽게 보이도록 SmoothStep 적용
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            spriteRenderer.color = Color.Lerp(startColor, targetColor, smoothT);

            yield return null;
        }

        spriteRenderer.color = targetColor;
        highlightColorCoroutine = null;
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
