
using UnityEngine;
using UnityEngine.UI;

// 이벤트 마커 전용 아웃라인 반짝임 효과.
//
// World Marker:
// ShopItemVisualAnimator와 동일한
// ShopItemSilhouette Shader를 이용하여
// Sprite 외곽선의 Alpha와 Scale을 반복 변경한다.
//
// UI Marker:
// Canvas Image에 Unity UI Outline을 적용하여
// Alpha와 외곽선 두께를 반복 변경한다.
//
// EventMarkerUI가 관리하는 기존 위치 추적,
// Fade In/Out, Float Animation에는 관여하지 않는다.
public class EventMarkerOutlinePulse : MonoBehaviour
{
    private enum MarkerRenderType
    {
        World = 0,
        UI = 1
    }

    [Header("Marker Type")]
    [SerializeField]
    private MarkerRenderType markerRenderType =
        MarkerRenderType.World;

    [Header("World Marker")]
    [SerializeField]
    private SpriteRenderer worldMarkerRenderer;

    // ShopItemSilhouette.shader를 연결한다.
    // 비어 있으면 Shader.Find로 찾는다.
    [SerializeField]
    private Shader outlineShader;

    // 원본 Sprite보다 뒤에 표시할 Sorting Order 차이.
    [SerializeField]
    private int worldOutlineSortingOffset = -1;

    [Header("UI Marker")]
    // UI Marker의 실제 Image를 연결한다.
    [SerializeField]
    private Image uiMarkerImage;

    [Header("Outline Color")]
    // 기존 Shop 아이템과 동일한 기본 흰색.
    [SerializeField]
    private Color outlineColor = Color.white;

    [Header("Outline Pulse")]
    [SerializeField, Range(0f, 1f)]
    private float outlineMinAlpha = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float outlineMaxAlpha = 1f;

    [SerializeField, Min(0.1f)]
    private float outlinePulseDuration = 0.9f;

    [Header("World Outline Scale")]
    // Shop 아이템과 동일한 기본 외곽선 확대율.
    [SerializeField, Range(0f, 0.25f)]
    private float outlineBaseScalePadding = 0.06f;

    // Shop 아이템과 동일한 Scale Pulse.
    [SerializeField, Range(0f, 0.25f)]
    private float outlineScalePulseAmount = 0.04f;

    [Header("UI Outline Width")]
    // Canvas 기준 기본 외곽선 두께.
    [SerializeField, Min(0f)]
    private float uiOutlineBaseWidth = 3f;

    // 반짝일 때 추가되는 외곽선 두께.
    [SerializeField, Min(0f)]
    private float uiOutlinePulseWidth = 1f;

    // World Marker의 자동 생성 아웃라인.
    private SpriteRenderer worldOutlineRenderer;

    // World Marker 전용 Runtime Material.
    private Material runtimeOutlineMaterial;

    // UI Marker에 적용할 Outline 컴포넌트.
    private UnityEngine.UI.Outline uiOutline;

    // 기존 UI Outline이 존재한 경우 원래 상태 보관.
    private bool createdUIOutline;
    private bool capturedUIOutlineState;
    private bool originalUIOutlineEnabled;
    private bool originalUIUseGraphicAlpha;
    private Color originalUIOutlineColor;
    private Vector2 originalUIOutlineDistance;

    // 현재 Pulse 진행 시간.
    private float pulseElapsedTime;

    private const string OutlineShaderName =
        "Devorya/ShopItemSilhouette";

    private void Awake()
    {
        PrepareOutline();
    }

    private void OnEnable()
    {
        pulseElapsedTime = 0f;

        PrepareOutline();
    }

    private void Update()
    {
        // 비활성화된 마커에는 효과를 적용하지 않는다.
        if (markerRenderType == MarkerRenderType.World)
        {
            if (worldOutlineRenderer == null)
            {
                return;
            }
        }
        else
        {
            if (uiOutline == null)
            {
                return;
            }
        }

        // ShopItemVisualAnimator와 동일한
        // Sin 기반 Pulse 계산 방식.
        float safeDuration =
            Mathf.Max(0.1f, outlinePulseDuration);

        float normalizedTime =
            Mathf.Repeat(
                pulseElapsedTime / safeDuration,
                1f
            );

        float pulseRate =
            (
                Mathf.Sin(
                    normalizedTime * Mathf.PI * 2f -
                    Mathf.PI * 0.5f
                ) +
                1f
            ) * 0.5f;

        float pulseAlpha =
            Mathf.Lerp(
                outlineMinAlpha,
                outlineMaxAlpha,
                pulseRate
            );

        if (markerRenderType == MarkerRenderType.World)
        {
            UpdateWorldOutline(
                pulseRate,
                pulseAlpha
            );
        }
        else
        {
            UpdateUIOutline(
                pulseRate,
                pulseAlpha
            );
        }

        // EventSequence가 TimeScale을 멈추더라도
        // 마커의 반짝임은 계속 유지한다.
        pulseElapsedTime += Time.unscaledDeltaTime;
    }

    // 마커 종류에 따라 아웃라인을 준비한다.
    private void PrepareOutline()
    {
        if (markerRenderType == MarkerRenderType.World)
        {
            PrepareWorldOutline();
        }
        else
        {
            PrepareUIOutline();
        }
    }

    // 기존 World Sprite와 동일한 Sprite를 사용하는
    // 별도 아웃라인 Renderer를 자동 생성한다.
    private void PrepareWorldOutline()
    {
        if (worldMarkerRenderer == null)
        {
            worldMarkerRenderer =
                GetComponent<SpriteRenderer>();

            if (worldMarkerRenderer == null)
            {
                worldMarkerRenderer =
                    GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (worldMarkerRenderer == null)
        {
            Debug.LogWarning(
                "[EventMarkerOutlinePulse] " +
                "World Marker SpriteRenderer가 없습니다.",
                this
            );

            return;
        }

        if (worldOutlineRenderer != null)
        {
            worldOutlineRenderer.enabled = true;
            return;
        }

        Shader targetShader =
            outlineShader != null
                ? outlineShader
                : Shader.Find(OutlineShaderName);

        if (targetShader == null)
        {
            Debug.LogWarning(
                "[EventMarkerOutlinePulse] " +
                "ShopItemSilhouette Shader를 찾지 못했습니다.",
                this
            );

            return;
        }

        runtimeOutlineMaterial = new Material(targetShader);

        runtimeOutlineMaterial.name =
            gameObject.name + "_EventOutline_Runtime";

        GameObject outlineObject =
            new GameObject("EventMarker_Outline");

        // 기존 마커 SpriteRenderer의 자식으로 만든다.
        // WorldRoot Zoom 및 Float 이동을 자동으로 공유한다.
        outlineObject.transform.SetParent(
            worldMarkerRenderer.transform,
            false
        );

        outlineObject.transform.localPosition =
            Vector3.zero;

        outlineObject.transform.localRotation =
            Quaternion.identity;

        outlineObject.transform.localScale =
            Vector3.one * (1f + outlineBaseScalePadding);

        worldOutlineRenderer =
            outlineObject.AddComponent<SpriteRenderer>();

        worldOutlineRenderer.sharedMaterial =
            runtimeOutlineMaterial;

        worldOutlineRenderer.color = Color.white;

        SyncWorldOutlineSource();
    }

    // 원본 Sprite와 Sorting 설정을 매 프레임 동기화한다.
    private void SyncWorldOutlineSource()
    {
        if (worldMarkerRenderer == null ||
            worldOutlineRenderer == null)
        {
            return;
        }

        worldOutlineRenderer.sprite =
            worldMarkerRenderer.sprite;

        worldOutlineRenderer.flipX =
            worldMarkerRenderer.flipX;

        worldOutlineRenderer.flipY =
            worldMarkerRenderer.flipY;

        worldOutlineRenderer.sortingLayerID =
            worldMarkerRenderer.sortingLayerID;

        worldOutlineRenderer.sortingOrder =
            worldMarkerRenderer.sortingOrder +
            worldOutlineSortingOffset;

        worldOutlineRenderer.drawMode =
            worldMarkerRenderer.drawMode;

        if (worldMarkerRenderer.drawMode !=
            SpriteDrawMode.Simple)
        {
            worldOutlineRenderer.size =
                worldMarkerRenderer.size;
        }

        worldOutlineRenderer.maskInteraction =
            worldMarkerRenderer.maskInteraction;
    }

    // World Outline의 밝기와 크기 변경.
    private void UpdateWorldOutline(
        float pulseRate,
        float pulseAlpha)
    {
        if (worldMarkerRenderer == null ||
            worldOutlineRenderer == null)
        {
            return;
        }

        SyncWorldOutlineSource();

        worldOutlineRenderer.enabled =
            worldMarkerRenderer.enabled &&
            worldMarkerRenderer.sprite != null;

        // EventMarkerUI의 World Fade Alpha를 함께 적용한다.
        // 따라서 Fade In/Out 중 외곽선만 따로 남지 않는다.
        float markerFadeAlpha =
            worldMarkerRenderer.color.a;

        Color currentColor = outlineColor;

        currentColor.a =
            Mathf.Clamp01(
                pulseAlpha * markerFadeAlpha
            );

        if (runtimeOutlineMaterial != null &&
            runtimeOutlineMaterial.HasProperty("_Color"))
        {
            runtimeOutlineMaterial.SetColor(
                "_Color",
                currentColor
            );
        }

        float scaleMultiplier =
            1f +
            outlineBaseScalePadding;

        float pulseScale =
            1f +
            outlineScalePulseAmount * pulseRate;

        worldOutlineRenderer.transform.localScale =
            Vector3.one *
            scaleMultiplier *
            pulseScale;
    }

    // UI Marker의 Image에 UI 전용 Outline을 적용한다.
    private void PrepareUIOutline()
    {
        if (uiMarkerImage == null)
        {
            uiMarkerImage = GetComponent<Image>();

            if (uiMarkerImage == null)
            {
                uiMarkerImage =
                    GetComponentInChildren<Image>(true);
            }
        }

        if (uiMarkerImage == null)
        {
            Debug.LogWarning(
                "[EventMarkerOutlinePulse] " +
                "UI Marker Image가 없습니다.",
                this
            );

            return;
        }

        if (uiOutline == null)
        {
            uiOutline =
                uiMarkerImage.GetComponent<
                    UnityEngine.UI.Outline>();

            if (uiOutline == null)
            {
                uiOutline =
                    uiMarkerImage.gameObject.AddComponent<
                        UnityEngine.UI.Outline>();

                createdUIOutline = true;
            }
        }

        // 기존 Outline이 있었다면 원래 설정을 저장한다.
        if (capturedUIOutlineState == false)
        {
            originalUIOutlineEnabled =
                createdUIOutline
                    ? false
                    : uiOutline.enabled;

            originalUIOutlineColor =
                uiOutline.effectColor;

            originalUIOutlineDistance =
                uiOutline.effectDistance;

            originalUIUseGraphicAlpha =
                uiOutline.useGraphicAlpha;

            capturedUIOutlineState = true;
        }

        uiOutline.useGraphicAlpha = true;

        uiOutline.enabled = true;
    }

    // UI Outline의 밝기와 두께 변경.
    private void UpdateUIOutline(
        float pulseRate,
        float pulseAlpha)
    {
        if (uiOutline == null ||
            uiMarkerImage == null)
        {
            return;
        }

        uiOutline.enabled =
            uiMarkerImage.enabled;

        Color currentColor = outlineColor;

        currentColor.a = Mathf.Clamp01(pulseAlpha);

        uiOutline.effectColor = currentColor;

        float currentWidth =
            uiOutlineBaseWidth +
            uiOutlinePulseWidth * pulseRate;

        uiOutline.effectDistance =
            new Vector2(
                currentWidth,
                currentWidth
            );

        // UI Marker 전체의 Fade는 기존 CanvasGroup이
        // 담당하므로 여기서 Alpha를 다시 곱하지 않는다.
    }

    private void OnDisable()
    {
        // World Marker가 사라지면 Outline도 숨긴다.
        if (worldOutlineRenderer != null)
        {
            worldOutlineRenderer.enabled = false;
        }

        // UI Marker가 사라지면 기존 Outline 상태를 복구한다.
        if (uiOutline != null &&
            capturedUIOutlineState)
        {
            uiOutline.effectColor =
                originalUIOutlineColor;

            uiOutline.effectDistance =
                originalUIOutlineDistance;

            uiOutline.useGraphicAlpha =
                originalUIUseGraphicAlpha;

            uiOutline.enabled =
                originalUIOutlineEnabled;
        }
    }

    private void OnDestroy()
    {
        // Runtime Material 누수 방지.
        if (runtimeOutlineMaterial != null)
        {
            Destroy(runtimeOutlineMaterial);
            runtimeOutlineMaterial = null;
        }

        // 런타임에서 직접 생성한 UI Outline만 정리한다.
        if (createdUIOutline && uiOutline != null)
        {
            Destroy(uiOutline);
            uiOutline = null;
        }
    }
}
