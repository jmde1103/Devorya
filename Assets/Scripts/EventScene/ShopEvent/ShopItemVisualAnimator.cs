using System.Collections;
using UnityEngine;


// <변경부분>
// 상점 상품의 시각 연출만 담당한다.
//
// 역할:
// 1. Item FloatRoot를 위아래로 부드럽게 부유시킨다.
// 2. Item이 올라갈수록 Shadow를 작게 만든다.
// 3. 선택된 상품의 흰색 Outline을 부드럽게 반짝이게 한다.
//
// 실제 상품 Data / Price / Tooltip / 구매 로직은
// ShopItemDisplay 또는 ShopController가 담당한다.
public class ShopItemVisualAnimator : MonoBehaviour
{
    [Header("Floating Item")]

    // ItemSprite와 SelectionOutline을 함께 넣은 부모 Transform.
    //
    // 이 Transform만 움직이면
    // Item과 Outline이 정확히 같은 타이밍으로 부유한다.
    [SerializeField]
    private Transform floatRoot;

    // 가장 아래 위치에서
    // 위로 얼마나 올라갈지 설정한다.
    [SerializeField, Min(0f)]
    private float floatHeight =
        0.12f;

    // 아래 → 위 → 아래까지
    // 한 번 왕복하는 시간.
    [SerializeField, Min(0.1f)]
    private float floatCycleDuration =
        2.4f;

    // 여러 상품이 완전히 동일한 타이밍으로 움직이지 않게
    // 각 상품마다 시작 위상을 다르게 설정할 수 있다.
    //
    // 예:
    // Green = 0
    // Red   = 0.33
    // Blue  = 0.66
    [SerializeField, Range(0f, 1f)]
    private float floatPhaseOffset =
        0f;


    [Header("Shadow")]

    // 테이블 위에 별도로 배치한 그림자 Transform.
    //
    // 이 오브젝트 자체는 이동하지 않고
    // Item 높이에 맞춰 Scale만 변경한다.
    [SerializeField]
    private Transform shadowTransform;

    // Item이 가장 높은 위치에 있을 때
    // 원래 Shadow Scale에 곱할 비율.
    //
    // X를 더 많이 줄이면
    // 납작한 바닥 그림자 느낌을 유지할 수 있다.
    [SerializeField]
    private Vector2 shadowScaleMultiplierAtTop =
        new Vector2(
            0.72f,
            0.82f
        );


    [Header("Selection Outline")]

    // <변경부분>
    // 실제 판매 Item의 SpriteRenderer.
    //
    // SelectionOutline에 별도 Sprite Asset을 넣지 않고
    // 이 Renderer의 Sprite를 자동으로 복사해서 사용한다.
    [SerializeField]
    private SpriteRenderer itemSpriteRenderer;

    // <변경부분>
    // 자동 Outline을 표시할 SpriteRenderer.
    //
    // FloatRoot 아래의 SelectionOutline 오브젝트를 연결한다.
    // Sprite 항목은 비워두어도 된다.
    [SerializeField]
    private SpriteRenderer selectionOutlineRenderer;

    // <변경부분>
    // Item Sprite의 RGB는 무시하고
    // Alpha 형태만 흰색 Silhouette으로 출력하는 전용 Shader.
    //
    // 아래에서 새로 만드는
    // ShopItemSilhouette.shader를 연결한다.
    [SerializeField]
    private Shader outlineShader;

    // <변경부분>
    // 평상시 상품 Outline 색상.
    //
    // 선택되지 않은 모든 판매 상품은
    // 이 색상으로 계속 반짝인다.
    [SerializeField]
    private Color outlineColor =
        Color.white;

    // <변경부분>
    // 플레이어가 현재 선택한 상품의 Outline 색상.
    //
    // 선택 상태에서도 기존 Pulse Animation은 그대로 유지하고
    // 색상만 붉은색으로 변경한다.
    [SerializeField]
    private Color selectedOutlineColor =
        Color.red;

    // <변경부분>
    // 원본 Item보다 Outline Sprite를
    // 얼마나 크게 표시할지 결정한다.
    //
    // 0.06 = 약 6% 확대.
    [SerializeField, Range(0f, 0.25f)]
    private float outlineBaseScalePadding =
        0.06f;

    // <변경부분>
    // ItemSprite보다 뒤에 그리기 위한
    // Sorting Order 차이.
    //
    // 기본 -1:
    // ItemSprite Order가 30이면 Outline은 29.
    [SerializeField]
    private int outlineSortingOrderOffset =
        -1;

    // 현재 단계에서 Outline 연출을 테스트하기 위한 초기값.
    //
    // 이후 ShopController가 생기면
    // SetSelected()를 호출하여 실제 선택 상태를 제어한다.
    [SerializeField]
    private bool startSelected =
        false;

    // Outline이 가장 어두울 때의 Alpha.
    [SerializeField, Range(0f, 1f)]
    private float outlineMinAlpha =
        0.35f;

    // Outline이 가장 밝을 때의 Alpha.
    [SerializeField, Range(0f, 1f)]
    private float outlineMaxAlpha =
        1f;

    // Outline 한 번의 반짝임 주기.
    [SerializeField, Min(0.1f)]
    private float outlinePulseDuration =
        0.9f;

    // 밝아질 때 Outline을 아주 조금 확대하여
    // 빛이 퍼지는 듯한 느낌을 만든다.
    [SerializeField, Range(0f, 0.25f)]
    private float outlineScalePulseAmount =
        0.04f;

    private Vector3 baseFloatLocalPosition;

    private Vector3 baseShadowLocalScale;

    private Vector3 baseOutlineLocalScale;

    private Color baseOutlineColor;


    // <변경부분>
    // 그림자 오브젝트의 최초 활성화 상태.
    //
    // SOLD 이후 새로운 상품을 진열할 때
    // 원래 Inspector 활성화 상태로 복구하기 위해 저장한다.
    private bool shadowInitiallyActive;


    // <변경부분>
    // 현재 상품이 판매 완료되어
    // 시각 연출을 정지해야 하는지 나타낸다.
    private bool isSoldOut;


    // <변경부분>
    // SelectionOutline 전용 Runtime Material.
    //
    // 각 Shop Item이 독립적으로 Alpha를 변경할 수 있게
    // 개별 Material Instance를 사용한다.
    private Material runtimeOutlineMaterial;

    private const string OutlineShaderName =
        "Devorya/ShopItemSilhouette";


    private bool isSelected;


    private Coroutine floatCoroutine;

    private Coroutine outlineCoroutine;


    public bool IsSelected =>
        isSelected;


    private void Awake()
    {
        // <변경부분>
        // Item Sprite를 기반으로
        // SelectionOutline을 자동 구성한다.
        PrepareSelectionOutline();

        CaptureBaseVisualState();
    }


    private void OnEnable()
    {
        StopFloatCoroutine();

        // <변경부분>
        // 이미 판매 완료된 상품은 ShopRoot가 재활성화되어도
        // 그림자 및 부유 애니메이션을 다시 시작하지 않는다.
        if (isSoldOut)
        {
            SetSoldOut(
                true
            );

            return;
        }

        if (floatRoot != null)
        {
            floatCoroutine =
                StartCoroutine(
                    FloatRoutine()
                );
        }

        SetSelected(
            startSelected
        );
    }


    private void OnDisable()
    {
        StopFloatCoroutine();

        StopOutlineCoroutine();

        RestoreBaseVisualState();
    }

    // <변경부분>
    // Runtime 전용 Outline Material 정리.
    private void OnDestroy()
    {
        if (runtimeOutlineMaterial == null)
        {
            return;
        }

        Destroy(
            runtimeOutlineMaterial
        );

        runtimeOutlineMaterial =
            null;
    }


    // <변경부분>
    // SelectionOutline의 Sprite / Material / Transform /
    // Sorting 상태를 실제 ItemSprite 기준으로 자동 구성한다.
    private void PrepareSelectionOutline()
    {
        if (itemSpriteRenderer == null ||
            selectionOutlineRenderer == null)
        {
            return;
        }


        Shader targetShader =
            outlineShader != null
                ? outlineShader
                : Shader.Find(
                    OutlineShaderName
                );


        if (targetShader == null)
        {
            Debug.LogWarning(
                "[ShopItemVisualAnimator] " +
                "ShopItemSilhouette Shader를 찾지 못했습니다.",
                this
            );

            return;
        }


        if (runtimeOutlineMaterial == null)
        {
            runtimeOutlineMaterial =
                new Material(
                    targetShader
                );

            runtimeOutlineMaterial.name =
                $"{gameObject.name}_Outline_Runtime";
        }


        selectionOutlineRenderer.sharedMaterial =
            runtimeOutlineMaterial;


        // ItemSprite와 동일한 위치 / 회전을 사용한다.
        Transform itemTransform =
            itemSpriteRenderer.transform;

        Transform outlineTransform =
            selectionOutlineRenderer.transform;


        outlineTransform.localPosition =
            itemTransform.localPosition;

        outlineTransform.localRotation =
            itemTransform.localRotation;


        float baseScaleMultiplier =
            1f +
            outlineBaseScalePadding;

        outlineTransform.localScale =
            new Vector3(
                itemTransform.localScale.x *
                    baseScaleMultiplier,

                itemTransform.localScale.y *
                    baseScaleMultiplier,

                itemTransform.localScale.z
            );


        baseOutlineLocalScale =
            outlineTransform.localScale;

        baseOutlineColor =
            outlineColor;


        SyncSelectionOutlineSource();

        ApplyOutlineColor(
       baseOutlineColor
   );

        // <변경부분>
        // Outline은 선택 여부와 관계없이
        // 판매 Item Sprite가 존재하는 동안 항상 표시한다.
        selectionOutlineRenderer.enabled =
            itemSpriteRenderer.sprite != null;
    }


    // <변경부분>
    // ItemData가 Runtime에서 변경되어
    // ItemSprite가 바뀌더라도 Outline이 자동으로 따라가게 한다.
    private void SyncSelectionOutlineSource()
    {
        if (itemSpriteRenderer == null ||
            selectionOutlineRenderer == null)
        {
            return;
        }


        selectionOutlineRenderer.sprite =
            itemSpriteRenderer.sprite;

        selectionOutlineRenderer.flipX =
            itemSpriteRenderer.flipX;

        selectionOutlineRenderer.flipY =
            itemSpriteRenderer.flipY;

        selectionOutlineRenderer.sortingLayerID =
            itemSpriteRenderer.sortingLayerID;

        selectionOutlineRenderer.sortingOrder =
            itemSpriteRenderer.sortingOrder +
            outlineSortingOrderOffset;
    }


    // <변경부분>
    // Outline 전용 Material에 실제 색상과 Alpha를 적용한다.
    private void ApplyOutlineColor(
        Color targetColor)
    {
        if (runtimeOutlineMaterial != null &&
            runtimeOutlineMaterial.HasProperty(
                "_Color"
            ))
        {
            runtimeOutlineMaterial.SetColor(
                "_Color",
                targetColor
            );
        }

        // Renderer 자체 Tint는 항상 흰색으로 유지한다.
        selectionOutlineRenderer.color =
            Color.white;
    }


    // <변경부분>
    // 최초 Scene / Prefab 배치 상태를 기준값으로 저장한다.
    private void CaptureBaseVisualState()
    {
        if (floatRoot != null)
        {
            baseFloatLocalPosition =
                floatRoot.localPosition;
        }

        if (shadowTransform != null)
        {
            baseShadowLocalScale =
                shadowTransform.localScale;

            // <변경부분>
            // 구매 후 그림자를 숨기더라도
            // 새로운 상품 진열 시 원래 상태로 복원한다.
            shadowInitiallyActive =
                shadowTransform.gameObject.activeSelf;
        }

        if (selectionOutlineRenderer != null)
        {
            baseOutlineLocalScale =
                selectionOutlineRenderer
                    .transform
                    .localScale;

            // <변경부분>
            // Renderer의 기존 Tint가 아니라
            // Inspector의 자동 Outline 색상을 기준으로 사용한다.
            baseOutlineColor =
                outlineColor;
        }
    }


    // <변경부분>
    // 비활성화 또는 Scene 전환 시
    // Runtime Animation이 남긴 Transform 값을 원래대로 복원한다.
    private void RestoreBaseVisualState()
    {
        if (floatRoot != null)
        {
            floatRoot.localPosition =
                baseFloatLocalPosition;
        }

        if (shadowTransform != null)
        {
            shadowTransform.localScale =
                baseShadowLocalScale;
        }

        if (selectionOutlineRenderer != null)
        {
            selectionOutlineRenderer
                .transform
                .localScale =
                    baseOutlineLocalScale;

            ApplyOutlineColor(
                baseOutlineColor
            );

            selectionOutlineRenderer.enabled =
                false;
        }
    }


    // <변경부분>
    // 아래 → 위 → 아래를 계속 반복한다.
    //
    // Coroutine 기반이지만
    // Mathf.Cos의 연속 곡선을 사용하므로
    // 방향이 바뀌는 상단 / 하단에서도 끊기지 않는다.
    private IEnumerator FloatRoutine()
    {
        float elapsedTime =
            floatCycleDuration *
            floatPhaseOffset;

        while (true)
        {
            float normalizedTime =
                Mathf.Repeat(
                    elapsedTime /
                    floatCycleDuration,
                    1f
                );

            // 0 → 1 → 0.
            //
            // 시작점과 최고점에서 속도가 자연스럽게 줄어든다.
            float liftRate =
                (
                    1f -
                    Mathf.Cos(
                        normalizedTime *
                        Mathf.PI *
                        2f
                    )
                ) *
                0.5f;


            if (floatRoot != null)
            {
                floatRoot.localPosition =
                    baseFloatLocalPosition +
                    Vector3.up *
                    (
                        floatHeight *
                        liftRate
                    );
            }


            // Item이 올라갈수록
            // 바닥 그림자를 작게 만든다.
            if (shadowTransform != null)
            {
                float shadowScaleX =
                    Mathf.Lerp(
                        1f,
                        shadowScaleMultiplierAtTop.x,
                        liftRate
                    );

                float shadowScaleY =
                    Mathf.Lerp(
                        1f,
                        shadowScaleMultiplierAtTop.y,
                        liftRate
                    );

                shadowTransform.localScale =
                    new Vector3(
                        baseShadowLocalScale.x *
                            shadowScaleX,

                        baseShadowLocalScale.y *
                            shadowScaleY,

                        baseShadowLocalScale.z
                    );
            }


            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }

    // <변경부분>
    // 상품의 판매 완료 상태에 따라
    // 그림자 및 시각 애니메이션을 정리하거나 복구한다.
    //
    // soldOut = true:
    // - 상품 부유 중지
    // - 그림자 비활성화
    // - Outline Pulse 중지
    // - FloatRoot / Shadow Transform 기본 상태 복원
    //
    // soldOut = false:
    // - 원래 그림자 활성화 상태 복원
    // - 부유 및 Outline 연출 재시작
    //
    // 진열대 전체 GameObject는 비활성화하지 않는다.
    public void SetSoldOut(
        bool soldOut)
    {
        isSoldOut =
            soldOut;


        // <변경부분>
        // Inspector에 연결된 기존 Shadow 오브젝트만
        // 활성화하거나 비활성화한다.
        //
        // 실수로 이 스크립트가 붙은 부모 GameObject가
        // Shadow로 연결되어 있어도
        // 상품 전체가 꺼지는 일을 방지한다.
        if (shadowTransform != null &&
            shadowTransform.gameObject != gameObject)
        {
            shadowTransform.gameObject.SetActive(
                soldOut == false &&
                shadowInitiallyActive
            );
        }


        if (soldOut)
        {
            // 판매 완료 상품은 더 이상 선택 상태가 아니다.
            isSelected = false;

            // 부유 및 Outline Coroutine을 모두 중지한다.
            StopFloatCoroutine();

            StopOutlineCoroutine();

            // 애니메이션이 변경한 Transform을 복구하고
            // Outline Renderer도 비활성화한다.
            RestoreBaseVisualState();

            return;
        }


        // <변경부분>
        // 새 상품 진열 시에만 애니메이션을 다시 시작한다.
        //
        // 비활성 GameObject에서는 Coroutine을 시작하지 않는다.
        if (isActiveAndEnabled == false ||
            gameObject.activeInHierarchy == false)
        {
            return;
        }


        StopFloatCoroutine();

        if (floatRoot != null)
        {
            floatCoroutine =
                StartCoroutine(
                    FloatRoutine()
                );
        }

        // 새 상품은 미선택 Outline 상태에서 시작한다.
        SetSelected(
            false
        );
    }


    // <변경부분>
    // 상품 선택 여부를 변경한다.
    //
    // Outline 자체는 항상 유지한다.
    //
    // 미선택:
    // 흰색 Outline + Pulse
    //
    // 선택:
    // 붉은색 Outline + 동일한 Pulse
    public void SetSelected(
      bool selected)
    {
        isSelected =
            selected;


        // Runtime Item Sprite를 기준으로
        // Outline Sprite / Material / Sorting을 다시 동기화한다.
        PrepareSelectionOutline();


        if (selectionOutlineRenderer == null)
        {
            return;
        }


        SyncSelectionOutlineSource();


        // Item Sprite가 존재하는 판매 상품이라면
        // 선택 여부와 관계없이 Outline은 항상 표시한다.
        selectionOutlineRenderer.enabled =
            itemSpriteRenderer != null &&
            itemSpriteRenderer.sprite != null;


        if (selectionOutlineRenderer.enabled == false)
        {
            StopOutlineCoroutine();

            return;
        }


        // <변경부분>
        // GameObject 또는 이 Component가 비활성 상태라면
        // Coroutine을 시작할 수 없다.
        //
        // Scene 종료 / ShopRoot 비활성화 과정에서
        // 외부 Controller가 SetSelected()를 호출하더라도
        // 여기서 안전하게 종료한다.
        if (isActiveAndEnabled == false ||
            gameObject.activeInHierarchy == false)
        {
            return;
        }


        // 기존 Pulse Coroutine이 이미 실행 중이라면
        // 다시 시작할 필요가 없다.
        //
        // Coroutine 내부에서 isSelected를 매 Frame 확인하여
        // 흰색 / 붉은색을 자동 전환한다.
        if (outlineCoroutine == null)
        {
            outlineCoroutine =
                StartCoroutine(
                    SelectionOutlineRoutine()
                );
        }
    }


    // <변경부분>
    // 판매 중인 상품의 Outline은 항상 Pulse한다.
    //
    // 선택 상태에 따라:
    //
    // 미선택 = 흰색
    // 선택   = 붉은색
    //
    // 색상만 변경하고
    // Alpha / Scale Pulse Animation은 동일하게 유지한다.
    private IEnumerator SelectionOutlineRoutine()
    {
        float elapsedTime =
            0f;


        while (true)
        {
            // Runtime에 Item Sprite가 없는 경우에는
            // Outline을 잠시 숨기고 다음 Frame을 기다린다.
            bool hasItemSprite =
                itemSpriteRenderer != null &&
                itemSpriteRenderer.sprite != null;


            if (selectionOutlineRenderer != null)
            {
                selectionOutlineRenderer.enabled =
                    hasItemSprite;
            }


            if (hasItemSprite == false)
            {
                yield return null;

                continue;
            }


            float normalizedTime =
                Mathf.Repeat(
                    elapsedTime /
                    outlinePulseDuration,
                    1f
                );


            float pulseRate =
                (
                    Mathf.Sin(
                        normalizedTime *
                        Mathf.PI *
                        2f -
                        Mathf.PI *
                        0.5f
                    ) +
                    1f
                ) *
                0.5f;


            // Runtime에서 Item Sprite가 교체되어도
            // Outline이 현재 Item과 동일한 Sprite를 계속 따라간다.
            SyncSelectionOutlineSource();


            // <변경부분>
            // 선택 여부에 따라 Outline의 기본 색상만 변경한다.
            Color currentOutlineColor =
                isSelected
                    ? selectedOutlineColor
                    : baseOutlineColor;


            // 기존 반짝임 Alpha Animation은 그대로 사용한다.
            currentOutlineColor.a =
                Mathf.Lerp(
                    outlineMinAlpha,
                    outlineMaxAlpha,
                    pulseRate
                );


            ApplyOutlineColor(
                currentOutlineColor
            );


            float scaleMultiplier =
                1f +
                (
                    outlineScalePulseAmount *
                    pulseRate
                );


            selectionOutlineRenderer
                .transform
                .localScale =
                    baseOutlineLocalScale *
                    scaleMultiplier;


            elapsedTime +=
                Time.unscaledDeltaTime;


            yield return null;
        }
    }


    private void StopFloatCoroutine()
    {
        if (floatCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            floatCoroutine
        );

        floatCoroutine =
            null;
    }


    private void StopOutlineCoroutine()
    {
        if (outlineCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            outlineCoroutine
        );

        outlineCoroutine =
            null;
    }
}
