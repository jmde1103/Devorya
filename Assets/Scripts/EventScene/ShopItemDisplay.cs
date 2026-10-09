using TMPro;
using UnityEngine;
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
public class ShopItemDisplay : MonoBehaviour
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


    // <변경부분>
    // 마지막으로 적용한 상품 입력 가능 상태.
    //
    // 매 Frame 동일한 Collider 상태를
    // 불필요하게 다시 적용하지 않기 위해 저장한다.
    private bool lastInteractionAllowed;

    private bool hasAppliedInteractionState;


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

        // 상품 입력 Collider는
        // ShopItemDisplay가 붙은 동일 GameObject에서 우선 찾는다.
        if (interactionCollider == null)
        {
            interactionCollider =
                GetComponent<BoxCollider2D>();
        }

        // TooltipTrigger 역시
        // 동일 GameObject에 붙이는 현재 상점 구조를 기준으로 한다.
        if (tooltipTrigger == null)
        {
            tooltipTrigger =
                GetComponent<TooltipTrigger>();
        }
    }


    // <변경부분>
    // 이후 ShopController가 상품을 생성하거나 교체할 때
    // ItemData와 가격을 한 번에 전달한다.
    public void Initialize(
        BattleItemData newItemData,
        int newPrice)
    {
        itemData =
            newItemData;

        price =
            Mathf.Max(
                0,
                newPrice
            );

        RefreshDisplay();

        // <변경부분>
        // 상품 유무가 변경될 수 있으므로
        // Collider 입력 상태도 즉시 다시 계산한다.
        RefreshInteractionState(
            true
        );
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
    // 판매 가격은 숫자만 표시한다.
    //
    // Gold Icon 등의 시각 요소는
    // Prefab에서 PriceText 옆에 별도로 배치한다.
    private void RefreshPrice()
    {
        if (priceText == null)
        {
            return;
        }

        if (HasItem == false)
        {
            priceText.text =
                string.Empty;

            return;
        }

        priceText.text =
            Mathf.Max(
                0,
                price
            ).ToString("N0");
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
    // Locale가 바뀌면 ItemData 자체는 그대로 유지하면서
    // Tooltip 문자열만 현재 언어로 다시 생성한다.
    private void OnSelectedLocaleChanged(
        Locale selectedLocale)
    {
        RefreshTooltip();
    }
}
