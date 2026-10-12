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

    [Header("Highlight Overlay (Unlit)")]

    // <변경부분>
    // 조명 영향을 받지 않는 하이라이트 전용 머티리얼.
    // Tile Prefab Inspector에서 Sprite-Unlit 머티리얼을 연결한다.
    [SerializeField]
    private Material highlightOverlayMaterial;

    // <변경부분>
    // 타일의 원래 무늬가 보이도록 하이라이트 투명도를 조절한다.
    [SerializeField, Range(0f, 1f)]
    private float highlightOverlayOpacity = 0.65f;

    // <변경부분>
    // 기존 타일 SpriteRenderer는 지형 및 조명 표현에 그대로 사용한다.
    private SpriteRenderer spriteRenderer;

    // <변경부분>
    // 이동/공격 가능 영역만 표시하는 별도 Unlit Renderer.
    // 처음 필요할 때 한 번 생성하고 재사용한다.
    private SpriteRenderer highlightOverlayRenderer;


    // <변경부분>
    // 기존 Highlight Overlay 외곽에 약한 광휘를 추가한다.
    // 별도의 Light2D를 생성하지 않으며,
    // Highlight Overlay Material을 동일하게 사용한다.
    [Header("Highlight Soft Glow")]

    [SerializeField]
    private bool useHighlightSoftGlow = true;

    // <변경부분>
    // 원본 타일보다 약간 크게 그려 외곽으로 색상이 보이도록 한다.
    [SerializeField, Range(1f, 1.25f)]
    private float highlightGlowScale = 1.08f;

    // <변경부분>
    // 외곽 광휘의 투명도 배율.
    // 메인 하이라이트 투명도에 곱해서 적용한다.
    [SerializeField, Range(0f, 1f)]
    private float highlightGlowOpacity = 0.25f;

    // <변경부분>
    // 메인 하이라이트와 외곽 광휘가 함께 밝아졌다 어두워지는 효과.
    // 기존 SerializedField 이름을 유지하여 Prefab 설정을 보존한다.
    [Header("Highlight Pulse (Main + Glow)")]

    [SerializeField]
    private bool useHighlightGlowPulse = true;

    // <변경부분>
    // Pulse의 변화 깊이.
    // 0 = 깜빡임 없음
    // 0.65 = 최소 밝기 35%, 최대 밝기 100%
    [SerializeField, Range(0f, 1f)]
    private float highlightGlowPulseAmount = 0.65f;

    // <변경부분>
    // Pulse 속도. Sin 파형에 사용하는 각속도이다.
    [SerializeField, Min(0f)]
    private float highlightGlowPulseSpeed = 2.4f;

    // <변경부분>
    // 외곽 광휘 전용 SpriteRenderer.
    // Highlight Overlay와 별도로 한 번 생성해 재사용한다.
    private SpriteRenderer highlightGlowRenderer;

    // <변경부분>
    // 페이드만 적용된 원본 하이라이트 색상.
    // Pulse가 변경한 표시 색상을 다시 페이드 계산에 사용하면
    // 누적 오차가 발생하므로 별도로 보관한다.
    private Color highlightOverlayBaseColor = Color.clear;


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


    // <변경부분>
    // 메인 하이라이트와 외곽 광휘에 동일한 Pulse를 적용한다.
    //
    // 페이드 계산용 원본 색상은 수정하지 않고,
    // 화면에 출력하는 SpriteRenderer.color만 매 프레임 변경한다.
    private void LateUpdate()
    {
        if (highlightOverlayRenderer == null)
        {
            return;
        }

        // <변경부분>
        // 등장/퇴장 Fade가 계산한 원본 색상.
        Color baseColor =
            highlightOverlayBaseColor;

        float pulseMultiplier = 1f;

        if (useHighlightGlowPulse &&
            baseColor.a > 0f)
        {
            // <변경부분>
            // 0~1 사이를 왕복하는 부드러운 Sin 파형.
            float pulse01 =
                0.5f +
                0.5f * Mathf.Sin(
                    Time.unscaledTime *
                    highlightGlowPulseSpeed
                );

            // <변경부분>
            // Pulse Amount가 0.65라면
            // 원본 밝기의 35%~100% 사이에서 변화한다.
            pulseMultiplier = Mathf.Lerp(
                1f - Mathf.Clamp01(highlightGlowPulseAmount),
                1f,
                pulse01
            );
        }

        // <변경부분>
        // 메인 빨강/파랑 하이라이트 전체의 투명도를 변화시킨다.
        // RGB는 유지하여 기존 강조색이 변하지 않도록 한다.
        Color visibleOverlayColor =
            baseColor;

        visibleOverlayColor.a =
            Mathf.Clamp01(
                baseColor.a * pulseMultiplier
            );

        highlightOverlayRenderer.color =
            visibleOverlayColor;

        if (highlightGlowRenderer == null)
        {
            return;
        }

        if (!useHighlightSoftGlow)
        {
            highlightGlowRenderer.color =
                Color.clear;

            return;
        }

        // <변경부분>
        // 외곽 광휘도 같은 Pulse를 따라간다.
        // Glow는 기존 투명도 배율을 추가로 적용한다.
        Color glowColor =
            baseColor;

        glowColor.a =
            Mathf.Clamp01(
                baseColor.a *
                highlightGlowOpacity *
                pulseMultiplier
            );

        highlightGlowRenderer.color =
            glowColor;
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

            // <변경부분>
            // 이미 생성된 하이라이트 Overlay가 있다면
            // 변경된 타일 스프라이트를 그대로 따라가도록 한다.
            if (highlightOverlayRenderer != null)
            {
                highlightOverlayRenderer.sprite =
                    spriteRenderer.sprite;
            }
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
        // <변경부분>
        // 기존 Inspector의 highlightColor를 그대로 사용한다.
        // Unlit 머티리얼이 연결된 경우 별도 Overlay에 표시한다.
        ChangeTileColorSmooth(highlightColor, true);
    }

    // <변경부분> 이동 또는 공격 실행 전
    // 첫 번째 클릭으로 확인된 타일의 전용 색상을 표시한다.
    public void ShowActionConfirmHighlight()
    {
        // <변경부분>
        // 기존 확인 색상과 두 번째 클릭 동작을 그대로 유지한다.
        ChangeTileColorSmooth(
            actionConfirmHighlightColor,
            true
        );
    }


    public void HideHighlight()  // 타일 표시 원상 복구
    {
        // <변경부분>
        // Unlit Overlay는 투명하게 만든다.
        // 타일 본체는 기존 조명과 기본 색상을 유지한다.
        ChangeTileColorSmooth(Color.clear, false);
    }

    // <변경부분>
    // 기존 타일의 SpriteRenderer를 기준으로
    // 하이라이트 전용 Unlit Renderer를 생성하는 공용 함수.
    //
    // Collider를 추가하지 않으며
    // 기존 타일의 클릭 및 전투 판정에는 영향을 주지 않는다.
    private SpriteRenderer CreateHighlightVisual(
        string objectName,
        float visualScale)
    {
        GameObject visualObject =
            new GameObject(objectName);

        // <변경부분>
        // 기존 타일 Renderer에 종속되도록 생성한다.
        visualObject.transform.SetParent(
            spriteRenderer.transform,
            false
        );

        visualObject.transform.localPosition =
            Vector3.zero;

        visualObject.transform.localRotation =
            Quaternion.identity;

        visualObject.transform.localScale =
            new Vector3(
                visualScale,
                visualScale,
                1f
            );

        visualObject.layer =
            spriteRenderer.gameObject.layer;

        SpriteRenderer visualRenderer =
            visualObject.AddComponent<SpriteRenderer>();

        // <변경부분>
        // Unlit 머티리얼은 공유한다.
        // 머티리얼 Asset 자체의 색상은 변경하지 않는다.
        visualRenderer.sharedMaterial =
            highlightOverlayMaterial;

        // <변경부분>
        // 타일과 동일한 Sprite 및 렌더링 정보를 사용한다.
        visualRenderer.sprite =
            spriteRenderer.sprite;

        visualRenderer.drawMode =
            spriteRenderer.drawMode;

        visualRenderer.size =
            spriteRenderer.size;

        visualRenderer.flipX =
            spriteRenderer.flipX;

        visualRenderer.flipY =
            spriteRenderer.flipY;

        visualRenderer.maskInteraction =
            spriteRenderer.maskInteraction;

        // <변경부분>
        // 생성 직후에는 아무것도 표시하지 않는다.
        visualRenderer.color =
            Color.clear;

        return visualRenderer;
    }


    // <변경부분>
    // 메인 하이라이트와 외곽 광휘를 각각 하나씩 생성한다.
    // 한 번 생성한 Renderer는 이후 재사용한다.
    private SpriteRenderer GetOrCreateHighlightOverlay()
    {
        if (spriteRenderer == null)
        {
            return null;
        }

        // <변경부분>
        // 머티리얼이 연결되지 않은 기존 Prefab은
        // 기존 SpriteRenderer 색상 변경 방식으로 동작한다.
        if (highlightOverlayMaterial == null)
        {
            return spriteRenderer;
        }

        // <변경부분>
        // 외곽 광휘는 기존 타일보다 약간 큰 Sprite로 생성한다.
        if (highlightGlowRenderer == null)
        {
            highlightGlowRenderer =
                CreateHighlightVisual(
                    "__BattleTileHighlightGlow",
                    highlightGlowScale
                );
        }

        // <변경부분>
        // 메인 하이라이트는 타일과 동일한 크기로 생성한다.
        if (highlightOverlayRenderer == null)
        {
            highlightOverlayRenderer =
                CreateHighlightVisual(
                    "__BattleTileHighlight",
                    1f
                );
        }

        if (highlightGlowRenderer != null)
        {
            // <변경부분>
            // 생성 이후에도 Inspector의 외곽 크기 설정을 반영한다.
            highlightGlowRenderer.transform.localScale =
                new Vector3(
                    highlightGlowScale,
                    highlightGlowScale,
                    1f
                );

            highlightGlowRenderer.sharedMaterial =
                highlightOverlayMaterial;

            highlightGlowRenderer.sprite =
                spriteRenderer.sprite;

            highlightGlowRenderer.sortingLayerID =
                spriteRenderer.sortingLayerID;

            // <변경부분>
            // 기존 타일보다 앞, 메인 하이라이트보다 뒤에 배치한다.
            highlightGlowRenderer.sortingOrder =
                spriteRenderer.sortingOrder + 1;
        }

        // <변경부분>
        // 메인 하이라이트는 외곽 광휘보다 앞에 표시한다.
        highlightOverlayRenderer.sharedMaterial =
            highlightOverlayMaterial;

        highlightOverlayRenderer.sortingLayerID =
            spriteRenderer.sortingLayerID;

        highlightOverlayRenderer.sortingOrder =
            spriteRenderer.sortingOrder + 2;

        // <변경부분>
        // 전투 중 TileData가 변경되어도
        // 현재 Sprite 형태를 사용한다.
        highlightOverlayRenderer.sprite =
            spriteRenderer.sprite;

        return highlightOverlayRenderer;
    }

    // <변경부분>
    // 기존 타일 색상 변경 기능을 유지하면서
    // Unlit Overlay가 연결된 경우 해당 Renderer만 페이드한다.
    private void ChangeTileColorSmooth(
        Color targetColor,
        bool showHighlight)
    {
        SpriteRenderer targetRenderer =
            GetOrCreateHighlightOverlay();

        if (targetRenderer == null)
        {
            return;
        }

        // <변경부분>
        // 기존 전환 코루틴이 실행 중이라면 정지한다.
        // 빠르게 다른 타일을 선택하거나 확인 색상으로 변경할 때
        // 이전 색상 전환이 남지 않도록 한다.
        if (highlightColorCoroutine != null)
        {
            StopCoroutine(highlightColorCoroutine);
            highlightColorCoroutine = null;
        }

        if (targetRenderer == spriteRenderer)
        {
            // <변경부분>
            // Unlit 머티리얼이 연결되지 않은 경우의 호환 처리.
            // 기존 타일 색상 변경 기능을 그대로 사용한다.
            targetColor =
                showHighlight
                    ? targetColor
                    : originalColor;
        }
        else if (showHighlight)
        {
            // <변경부분>
            // 기존 하이라이트 색상의 RGB는 변경하지 않는다.
            // 투명도만 Inspector의 Overlay Opacity에 맞춰 제한한다.
            targetColor.a =
                Mathf.Clamp01(
                    targetColor.a *
                    highlightOverlayOpacity
                );
        }
        else
        {
            // <변경부분>
            // 숨길 때는 Pulse가 적용된 표시 색상이 아니라
            // 페이드용 원본 색상을 기준으로 Alpha를 0으로 만든다.
            targetColor =
                targetRenderer == highlightOverlayRenderer
                    ? highlightOverlayBaseColor
                    : targetRenderer.color;

            targetColor.a = 0f;
        }

        // <변경부분>
        // 페이드가 비활성화되었거나 시간이 0 이하이면 즉시 적용한다.
        if (useHighlightFade == false ||
            highlightFadeDuration <= 0f)
        {
            if (targetRenderer == highlightOverlayRenderer)
            {
                // <변경부분>
                // 실제 표시 색상은 LateUpdate가 Pulse를 적용한다.
                highlightOverlayBaseColor =
                    targetColor;
            }
            else
            {
                // Unlit 머티리얼이 없는 기존 Prefab 호환 처리.
                targetRenderer.color =
                    targetColor;
            }

            return;
        }

        // <변경부분>
        // 기존 0.12초 SmoothStep 페이드 기능을 유지한다.
        highlightColorCoroutine =
            StartCoroutine(
                ChangeTileColorSmoothRoutine(
                    targetRenderer,
                    targetColor
                )
            );
    }

    // <변경부분>
    // 기존 0.12초 Fade를 유지하되,
    // Unlit Overlay는 Pulse와 충돌하지 않도록
    // 페이드용 원본 색상만 변경한다.
    private IEnumerator ChangeTileColorSmoothRoutine(
        SpriteRenderer targetRenderer,
        Color targetColor)
    {
        if (targetRenderer == null)
        {
            highlightColorCoroutine = null;
            yield break;
        }

        bool isOverlay =
            targetRenderer == highlightOverlayRenderer;

        // <변경부분>
        // Pulse가 적용된 색상을 Fade 시작값으로 사용하지 않는다.
        Color startColor =
            isOverlay
                ? highlightOverlayBaseColor
                : targetRenderer.color;

        float elapsed = 0f;

        float duration =
            Mathf.Max(
                0.001f,
                highlightFadeDuration
            );

        while (elapsed < duration)
        {
            if (targetRenderer == null)
            {
                highlightColorCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / duration);

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

            Color currentColor =
                Color.Lerp(
                    startColor,
                    targetColor,
                    smoothT
                );

            if (isOverlay)
            {
                // <변경부분>
                // Fade 결과를 별도로 저장한다.
                // LateUpdate가 여기에 Pulse를 곱한다.
                highlightOverlayBaseColor =
                    currentColor;
            }
            else
            {
                // 기존 머티리얼 미연결 Prefab 호환 처리.
                targetRenderer.color =
                    currentColor;
            }

            yield return null;
        }

        if (isOverlay)
        {
            highlightOverlayBaseColor =
                targetColor;
        }
        else if (targetRenderer != null)
        {
            targetRenderer.color =
                targetColor;
        }

        highlightColorCoroutine =
            null;
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


    // <변경부분> 지정된 화면 좌표가 Unity UI 위인지
    // Camera Controller에서도 직접 확인할 수 있도록 공개한다.
    public static bool IsScreenPositionOverUI(
        Vector2 screenPosition)
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            return false;
        }

        return IsScreenPositionOverUI(
            eventSystem,
            screenPosition
        );
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
        // <변경부분> PC와 모바일의 Battle World 입력은
        // 모두 PixelCameraController가 통합 처리한다.
        //
        // PC:
        // Mouse Down -> Drag Threshold -> Mouse Up
        //
        // Mobile:
        // Touch Began -> Drag Threshold -> Touch Ended
        //
        // Tile이 여기서 즉시 SelectTile()을 실행하면
        // 모바일에서 한 손가락 Drag를 시작하는 순간에도
        // 기물/타일 선택이 먼저 발생하므로 직접 입력은 실행하지 않는다.
    }
}
