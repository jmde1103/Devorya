using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// <변경부분>
// EventScene 상점에 진열되는 상품 하나의
// 표시와 Tooltip 입력 상태를 관리한다.
//
// 현재 1차 역할:
// - BattleItemData 아이콘 표시
// - 판매 가격 표시
// - 기존 Tooltip 시스템 연결
// - EventScene Player Interaction Lock 상태에 맞춘
//   상품 Tooltip 입력 차단 / 허용
//
// 실제 상품 선택 / 구매 / Gold 차감은
// 이후 ShopController 단계에서 별도로 구현한다.
public class ShopItemDisplay :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("Shop Item")]

    // <변경부분>
    // 현재 이 진열대에서 판매할 BattleItemData.
    //
    // 현재는 Inspector에서 직접 지정하여 테스트할 수 있고,
    // 이후 ShopController가 Initialize()를 통해
    // Runtime 상품 데이터를 전달할 수도 있다.
    [SerializeField]
    private BattleItemData itemData;

    // <변경부분>
    // 현재 상점에서의 실제 판매 가격.
    //
    // 가격은 BattleItemData 자체에 저장하지 않는다.
    // 같은 아이템이라도 상인 / 할인 / 종족 데이터 등에 따라
    // 가격이 달라질 수 있기 때문이다.
    [SerializeField, Min(0)]
    private int price;


    [Header("Visual")]

    // <변경부분>
    // 진열대 위 실제 아이템 SpriteRenderer.
    //
    // Carpet / Crate가 아니라
    // 상품 아이콘 자체 Renderer를 연결한다.
    [SerializeField]
    private SpriteRenderer itemSpriteRenderer;

    // <변경부분>
    // 현재 판매 가격을 표시할 TMP Text.
    //
    // TextMeshPro 또는 TextMeshProUGUI 모두
    // TMP_Text 기반이므로 사용할 수 있다.
    [SerializeField]
    private TMP_Text priceText;


    [Header("Sold Out Display")]

    // <변경부분>
    // 구매 완료 시 기존 가격 Text에 표시할 문구.
    //
    // 기본값은 SOLD.
    // Inspector에서 OUT, X 등으로 변경할 수 있다.
    [SerializeField]
    private string soldOutText = "SOLD";

    // <변경부분>
    // SOLD 문구에만 적용할 상대 글자 크기(%).
    //
    // 기존 가격 Text의 Font Size는 그대로 두고
    // 구매 완료 문구만 작게 표시한다.
    [SerializeField, Range(50, 100)]
    private int soldOutFontSizePercent = 85;


    [Header("Tooltip")]

    // <변경부분>
    // 기존 공용 Tooltip 시스템을 그대로 재사용한다.
    //
    // ShopItemDisplay의 BoxCollider2D를
    // Main Camera의 Physics2DRaycaster가 감지하면
    // 이 TooltipTrigger가 Pointer 입력을 전달받는다.
    [SerializeField]
    private TooltipTrigger tooltipTrigger;


    [Header("Interaction")]

    // <변경부분>
    // EventScene의 Player Interaction Lock 상태를 확인한다.
    //
    // Inspector 연결을 우선 사용하고,
    // 연결되지 않은 경우 현재 Scene에서 자동으로 찾는다.
    [SerializeField]
    private EventSceneSequenceController
        eventSceneSequenceController;

    // <변경부분>
    // 상품 자체의 Mouse / Touch 입력 판정을 담당하는 Collider.
    //
    // Event 연출 중에는 Collider를 비활성화하고,
    // UnlockPlayerInteraction 이후 다시 활성화한다.
    //
    // 상품 Sprite보다 약간 넓게 설정하면
    // 모바일에서도 누르기 편한 입력 범위를 만들 수 있다.
    [SerializeField]
    private BoxCollider2D interactionCollider;


    [Header("Shop Selection")]

    // <변경부분>
    // 이 상품이 속한 상점의 선택 상태를 관리하는 Controller.
    //
    // Inspector 연결을 사용할 수도 있고,
    // ShopController가 상위 오브젝트에 있다면
    // 자동으로 찾을 수도 있다.
    [SerializeField]
    private ShopController shopController;

    // <변경부분>
    // 상품 선택 시 흰색 Outline 연출을 담당한다.
    [SerializeField]
    private ShopItemVisualAnimator visualAnimator;


    // 마지막으로 적용한 상품 입력 가능 상태.
    //
    // 매 Frame 동일한 Collider 상태를
    // 불필요하게 다시 적용하지 않기 위해 저장한다.
    private bool lastInteractionAllowed;

    private bool hasAppliedInteractionState;


    // <변경부분>
    // 이 상품이 실제 구매로 판매 완료되었는지 저장한다.
    //
    // 처음부터 비어 있는 상품과 판매 완료 상품을 구분한다.
    // Scene 내에서 유지되는 Runtime 상태다.
    private bool isSoldOut;


    // 이후 ShopController에서 현재 상품 정보를
    // 조회할 수 있도록 읽기 전용으로 공개한다.
    public BattleItemData ItemData =>
        itemData;

    public int Price =>
        price;

    public bool HasItem =>
        itemData != null &&
        itemData.itemType !=
            BattleItemType.None;


    private void Awake()
    {
        CacheSceneReferences();
    }


    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;

        CacheSceneReferences();

        RefreshDisplay();

        // <변경부분>
        // ShopRoot가 재활성화되더라도
        // 이미 판매한 상품의 그림자와 Outline은
        // 다시 나타나지 않도록 유지한다.
        if (isSoldOut &&
            visualAnimator != null)
        {
            visualAnimator.SetSoldOut(
                true
            );
        }

        RefreshInteractionState(
            true
        );
    }


    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        // <변경부분>
        // ShopItemDisplay가 비활성화되면
        // 상품 입력도 즉시 차단한다.
        if (interactionCollider != null)
        {
            interactionCollider.enabled =
                false;
        }

        // TooltipTrigger를 비활성화하면
        // 이미 표시 중이던 Tooltip도
        // 기존 TooltipTrigger.OnDisable()에서 정리된다.
        if (tooltipTrigger != null)
        {
            tooltipTrigger.enabled =
                false;
        }
    }


    private void Update()
    {
        // <변경부분>
        // UnlockPlayerInteraction /
        // LockPlayerInteraction Step에 의해
        // EventScene 상태가 변경되었는지 확인한다.
        //
        // 상태가 실제로 바뀐 경우에만
        // 상품 Collider / Tooltip 입력 상태를 갱신한다.
        RefreshInteractionState(
            false
        );
    }


    // <변경부분>
    // Inspector 연결이 빠진 경우
    // 현재 EventScene에서 필요한 참조를 자동으로 보완한다.
    private void CacheSceneReferences()
    {
        if (eventSceneSequenceController == null)
        {
            eventSceneSequenceController =
                UnityEngine.Object
                    .FindFirstObjectByType<
                        EventSceneSequenceController
                    >();
        }


        if (interactionCollider == null)
        {
            interactionCollider =
                GetComponent<BoxCollider2D>();
        }


        if (tooltipTrigger == null)
        {
            tooltipTrigger =
                GetComponent<TooltipTrigger>();
        }


        // <변경부분>
        // ShopItemVisualAnimator는
        // 현재 상품 GameObject에 붙어 있는 구성을 우선 사용한다.
        if (visualAnimator == null)
        {
            visualAnimator =
                GetComponent<
                    ShopItemVisualAnimator
                >();
        }


        // <변경부분>
        // ShopController는 상품들의 공용 부모인
        // ShopRoot에 붙이는 구조를 기준으로 한다.
        if (shopController == null)
        {
            shopController =
                GetComponentInParent<
                    ShopController
                >();
        }
    }


    // <변경부분>
    // ShopController가 상품을 생성하거나 교체할 때
    // ItemData와 가격을 한 번에 전달한다.
    //
    // 새 상품을 진열하면 기존 SOLD 상태는 초기화한다.
    public void Initialize(
        BattleItemData newItemData,
        int newPrice)
    {
        // <변경부분>
        // 새로운 상품 진열이므로 판매 완료 상태를 해제한다.
        isSoldOut = false;

        itemData =
            newItemData;

        price =
            Mathf.Max(
                0,
                newPrice
            );

        RefreshDisplay();

        // <변경부분>
        // 기존 상품 부유 연출 및 그림자를 복구한다.
        if (visualAnimator == null)
        {
            visualAnimator =
                GetComponent<
                    ShopItemVisualAnimator
                >();
        }

        if (visualAnimator != null)
        {
            visualAnimator.SetSoldOut(
                false
            );
        }

        // 상품 유무가 변경될 수 있으므로
        // Collider 입력 상태도 즉시 다시 계산한다.
        RefreshInteractionState(
            true
        );
    }


    // <변경부분>
    // 실제 구매에 성공한 상품을 판매 완료 상태로 전환한다.
    //
    // 처리 내용:
    // - 상품 데이터 및 가격 초기화
    // - 상품 Sprite 제거
    // - 가격 Text를 SOLD 표시로 변경
    // - Tooltip 및 구매 입력 차단
    // - 그림자 / 부유 / Outline 연출 종료
    //
    // 진열대 오브젝트 자체는 유지한다.
    public void MarkAsSoldOut()
    {
        if (isSoldOut)
        {
            return;
        }

        isSoldOut = true;

        itemData = null;
        price = 0;

        // 상품 Sprite / 가격 / Tooltip 갱신.
        RefreshDisplay();

        // Collider와 Tooltip 입력을 즉시 차단한다.
        RefreshInteractionState(
            true
        );

        if (visualAnimator == null)
        {
            visualAnimator =
                GetComponent<
                    ShopItemVisualAnimator
                >();
        }

        // 구매한 아이템의 그림자와 연출도 종료한다.
        if (visualAnimator != null)
        {
            visualAnimator.SetSoldOut(
                true
            );
        }
    }


    // <변경부분>
    // 할인 등으로 가격만 변경해야 할 때 사용한다.
    public void SetPrice(
        int newPrice)
    {
        price =
            Mathf.Max(
                0,
                newPrice
            );

        RefreshPrice();
    }

    // <변경부분>
    // ShopController만 상품의 선택 비주얼을 변경하도록
    // 외부 진입점을 하나로 통일한다.
    public void SetSelected(
        bool selected)
    {
        if (visualAnimator == null)
        {
            visualAnimator =
                GetComponent<
                    ShopItemVisualAnimator
                >();
        }

        if (visualAnimator == null)
        {
            return;
        }

        visualAnimator.SetSelected(
            selected
        );
    }


    // <변경부분>
    // 현재 ItemData를 기준으로
    // 상품 아이콘 / 가격 / Tooltip 전체를 갱신한다.
    public void RefreshDisplay()
    {
        RefreshItemVisual();

        RefreshPrice();

        RefreshTooltip();
    }


    // <변경부분>
    // BattleItemData의 기존 iconSprite를
    // 상점 상품 Sprite에 그대로 사용한다.
    private void RefreshItemVisual()
    {
        if (itemSpriteRenderer == null)
        {
            return;
        }

        bool hasItem =
            HasItem;

        itemSpriteRenderer.sprite =
            hasItem
                ? itemData.iconSprite
                : null;

        itemSpriteRenderer.enabled =
            hasItem &&
            itemData.iconSprite != null;
    }


    // <변경부분>
    // 상품 상태에 따라 가격 또는 SOLD 문구를 표시한다.
    //
    // 판매 중:
    // 50   → 50G
    // 1000 → 1,000G
    //
    // 판매 완료:
    // SOLD
    //
    // SOLD는 기존 가격 Text에 표시하고,
    // 문구의 크기만 별도로 조절한다.
    private void RefreshPrice()
    {
        if (priceText == null)
        {
            return;
        }

        // <변경부분>
        // 실제 구매로 판매 완료된 상품이라면
        // 가격 대신 SOLD 문구를 표시한다.
        if (isSoldOut)
        {
            string displayText =
                string.IsNullOrWhiteSpace(soldOutText)
                    ? "SOLD"
                    : soldOutText;

            int fontSizePercent =
                Mathf.Clamp(
                    soldOutFontSizePercent,
                    50,
                    100
                );

            // TextMeshPro Rich Text로
            // SOLD 글자 크기만 줄여 표시한다.
            priceText.text =
                $"<size={fontSizePercent}%>" +
                displayText +
                "</size>";

            return;
        }

        // 상품이 처음부터 비어 있다면
        // 가격 Text도 비워 둔다.
        if (HasItem == false)
        {
            priceText.text =
                string.Empty;

            return;
        }

        // 판매 중인 상품은 기존 가격 형식을 유지한다.
        priceText.text =
            Mathf.Max(
                0,
                price
            ).ToString("N0") +
            "<space=0.08em>G";
    }


    // <변경부분>
    // 기존 BattleItem Tooltip SSOT를 그대로 재사용한다.
    //
    // 이름 / 설명 / Category / StatusEffect Section까지
    // 기존 Localization 및 Tooltip 구조를 그대로 사용한다.
    private void RefreshTooltip()
    {
        if (tooltipTrigger == null)
        {
            return;
        }

        TooltipViewData tooltipViewData =
            HasItem
                ? TooltipViewData
                    .FromBattleItemData(
                        itemData
                    )
                : null;

        tooltipTrigger.SetTooltipViewData(
            tooltipViewData
        );
    }


    // <변경부분>
    // EventScene Player Interaction Lock 상태에 맞춰
    // 상품 Collider와 Tooltip 입력을 차단하거나 허용한다.
    private void RefreshInteractionState(
        bool forceRefresh)
    {
        bool interactionAllowed =
            HasItem &&
            eventSceneSequenceController != null &&
            eventSceneSequenceController
                .IsPlayerInteractionLocked ==
            false;

        if (forceRefresh == false &&
            hasAppliedInteractionState &&
            lastInteractionAllowed ==
                interactionAllowed)
        {
            return;
        }

        lastInteractionAllowed =
            interactionAllowed;

        hasAppliedInteractionState =
            true;


        // Event 연출 중에는 Collider 자체를 끄므로
        // 월드 상품이 Mouse / Touch 입력을 받지 않는다.
        if (interactionCollider != null)
        {
            interactionCollider.enabled =
                interactionAllowed;
        }


        // Collider가 잠기는 순간
        // 이미 열려 있던 Tooltip도 함께 정리하기 위해
        // TooltipTrigger 활성 상태도 동일하게 맞춘다.
        if (tooltipTrigger != null)
        {
            tooltipTrigger.enabled =
                interactionAllowed;
        }
    }




    // <변경부분>
    // Physics2DRaycaster + BoxCollider2D를 통해 전달된
    // 상품 Click 입력을 ShopController에 전달한다.
    //
    // Tooltip 표시와 실제 Shop 선택 상태는
    // 서로 별개의 기능으로 유지한다.
    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (HasItem == false)
        {
            return;
        }


        // EventScene의 Player Interaction이 잠겨 있거나
        // 상품 Collider가 비활성화된 상태에서는
        // 상품 선택 입력을 처리하지 않는다.
        if (eventSceneSequenceController == null ||
            eventSceneSequenceController
                .IsPlayerInteractionLocked ||
            interactionCollider == null ||
            interactionCollider.enabled == false)
        {
            return;
        }


        if (shopController == null)
        {
            shopController =
                GetComponentInParent<
                    ShopController
                >();
        }


        if (shopController == null)
        {
            Debug.LogWarning(
                "[ShopItemDisplay] " +
                "ShopController를 찾을 수 없습니다.",
                this
            );

            return;
        }


        shopController.HandleItemClicked(
            this
        );
    }


    // <변경부분>
    // Locale가 바뀌면 ItemData 자체는 그대로 유지하면서
    // Tooltip 문자열만 현재 언어로 다시 생성한다.
    private void OnSelectedLocaleChanged(
        Locale selectedLocale)
    {
        RefreshTooltip();
    }
}
