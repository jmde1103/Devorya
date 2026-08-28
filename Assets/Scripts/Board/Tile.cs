using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    // <변경부분> 이동 또는 공격 실행 전
    // 첫 번째 클릭으로 확인한 타일에 표시할 색상
    [SerializeField]
    private Color actionConfirmHighlightColor =
        new Color(1f, 0.65f, 0f, 1f);

    [Header("Highlight Animation")]
    // <변경부분> 타일 하이라이트 색상 변경을 부드럽게 처리할지 여부
    [SerializeField] private bool useHighlightFade = true;

    // <변경부분> 타일 하이라이트 색상 전환 시간
    [SerializeField] private float highlightFadeDuration = 0.12f;

    // <변경부분> 현재 실행 중인 타일 색상 전환 코루틴
    private Coroutine highlightColorCoroutine;

    private SpriteRenderer spriteRenderer;

    // 모바일 / PC 입력 시 현재 포인터 위치의 UI Raycast 결과를 재사용한다.
    // Tile 클릭 때마다 List를 새로 생성하지 않아 불필요한 GC 할당을 방지한다.
    private static readonly List<RaycastResult> pointerRaycastResults =
        new List<RaycastResult>();

    private void Awake()
    {
        // 타일의 표시 Renderer를 먼저 자기 오브젝트에서 찾는다.
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        // 자기 오브젝트에 없으면 자식 Renderer를 사용한다.
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                $"{gameObject.name}에 SpriteRenderer가 없습니다."
            );

            return;
        }

        // Runtime 타일 효과 목록을 한 번 생성한다.
        TileEffects =
            new List<TileEffectType>();

        // Highlight가 끝났을 때 복구할
        // Prefab의 실제 기본 색상을 최초 한 번 저장한다.
        originalColor =
            spriteRenderer.color;
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

        // TileData에 설정된 새로운 타일 스프라이트를 적용한다.
        //
        // TileData는 타일의 기본 Color를 소유하지 않으므로
        // 현재 SpriteRenderer 색상을 originalColor로 다시 저장하지 않는다.
        //
        // 특히 이동 가능 / 행동 확인 Highlight가 표시된 상태에서
        // ApplyTileData()가 실행되면 현재 Highlight 색상이
        // 원본 색상으로 잘못 저장될 수 있으므로,
        // originalColor는 Awake()에서 저장한 Prefab 기본색을 유지한다.
        if (spriteRenderer != null &&
            tileData.tileSprite != null)
        {
            spriteRenderer.sprite =
                tileData.tileSprite;
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

    // <변경부분> 이동 또는 공격 실행 전
    // 첫 번째 클릭으로 확인된 타일의 전용 색상을 표시한다.
    public void ShowActionConfirmHighlight()
    {
        ChangeTileColorSmooth(
            actionConfirmHighlightColor
        );
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

    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(
            X,
            Y
        );
    }


    // 현재 Mouse 또는 Mobile Touch가
    // 실제 Unity UI 위에서 시작됐는지 확인한다.
    //
    // 단순 IsPointerOverGameObject(fingerId) 판정뿐 아니라
    // 현재 화면 좌표에서 EventSystem Raycast를 직접 수행한다.
    //
    // 이렇게 하면 모바일에서 OnMouseDown과 UI EventSystem의
    // Pointer ID 또는 처리 타이밍이 서로 맞지 않는 경우에도
    // 뒤쪽 Battle Tile로 입력이 전달되는 것을 차단할 수 있다.
    public static bool IsPointerOverUI()
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            return false;
        }

        // 모바일 Touch가 존재하면
        // 현재 활성화된 실제 손가락 위치에서 UI Raycast를 직접 검사한다.
        if (Input.touchCount > 0)
        {
            for (int i = 0;
                 i < Input.touchCount;
                 i++)
            {
                Touch touch =
                    Input.GetTouch(i);

                if (IsScreenPositionOverUI(
                    eventSystem,
                    touch.position))
                {
                    return true;
                }
            }
        }

        // PC Mouse 또는 Unity가 Mouse Pointer로 변환한 입력도
        // 실제 화면 좌표 기준으로 UI를 검사한다.
        if (IsScreenPositionOverUI(
            eventSystem,
            Input.mousePosition))
        {
            return true;
        }

        // 기존 EventSystem Pointer 판정도 마지막 보조 안전장치로 유지한다.
        return
            eventSystem.IsPointerOverGameObject();
    }


    // 지정된 화면 좌표에 Unity UI Graphic이 존재하는지 직접 확인한다.
    private static bool IsScreenPositionOverUI(
        EventSystem eventSystem,
        Vector2 screenPosition)
    {
        if (eventSystem == null)
        {
            return false;
        }

        PointerEventData pointerEventData =
            new PointerEventData(
                eventSystem
            );

        pointerEventData.position =
            screenPosition;

        pointerRaycastResults.Clear();

        // 현재 화면 위치에서 EventSystem에 등록된
        // 모든 Raycaster를 기준으로 실제 Raycast를 수행한다.
        eventSystem.RaycastAll(
            pointerEventData,
            pointerRaycastResults
        );

        bool isOverUI =
            false;

        for (int i = 0;
             i < pointerRaycastResults.Count;
             i++)
        {
            RaycastResult raycastResult =
                pointerRaycastResults[i];

            // GraphicRaycaster를 통해 검출된 대상만
            // Unity UI 입력으로 판단한다.
            //
            // PhysicsRaycaster / Physics2DRaycaster를 통해 검출된
            // Battle Tile이나 Piece는 여기서 UI로 처리하지 않는다.
            if (raycastResult.module
                is UnityEngine.UI.GraphicRaycaster)
            {
                isOverUI =
                    true;

                break;
            }
        }

        // 다음 입력 검사에서 이전 Raycast 결과가 남지 않도록 초기화한다.
        pointerRaycastResults.Clear();

        return
            isOverUI;
    }


    private void OnMouseDown()
    {
        // <변경부분> PC Battle 입력은
        // PixelCameraController가 Mouse Down → Drag Threshold →
        // Mouse Up 순서로 Click과 Camera Drag를 구분한다.
        //
        // 따라서 PC에서는 Tile이 MouseDown 순간
        // SelectTile을 직접 실행하지 않는다.
        if (Application.isMobilePlatform == false)
        {
            return;
        }

        // 아래는 현재 모바일 입력을 그대로 유지한다.
        //
        // 모바일의 Tap vs 1 Finger Drag 구조는
        // 다음 입력 작업에서 별도로 변경한다.

        if (IsPointerOverUI())
        {
            return;
        }

        if (BattleManager.Instance == null)
        {
            return;
        }

        BattleManager.Instance.SelectTile(
            this
        );
    }
}
