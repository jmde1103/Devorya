using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement; // <변경부분> 상점 종료 후 월드맵 복귀
using UnityEngine.UI;


// <변경부분>
// Standalone EventScene 상점의
// 상품 선택과 실제 구매 처리를 담당하는 Controller.
//
// 현재 역할:
// 1. 상품 하나만 선택 상태로 유지
// 2. 첫 클릭 시 선택
// 3. 다른 상품 클릭 시 기존 선택 해제
// 4. 같은 상품 재클릭 시 실제 구매 요청
// 5. 구매 결과에 따라 HUD 갱신
// 6. 구매 완료 상품의 기본 표시 및 입력 제거
//
// Gold와 아이템 데이터의 실제 변경은
// RunStateManager가 담당한다.
public class ShopController : MonoBehaviour
{
    [SerializeField, InspectorName("월드맵 이벤트 데이터 (선택)")] private WorldMapEventData returnWorldMapEventData;
    [Header("Shop Items")]

    // <변경부분>
    // 현재 상점에 배치된 상품 Display 목록.
    //
    // Inspector에 직접 넣어도 되고,
    // 비어 있으면 현재 ShopController의 자식에서
    // ShopItemDisplay를 자동으로 찾는다.
    [SerializeField]
    private ShopItemDisplay[] shopItemDisplays;


    [Header("Shop Inventory Test")]

    // <변경부분>
    // WorldMap Shop Node 연결 전 테스트용 설정.
    //
    // 체크하면 BackgroundManager가 상품 진열 프리팹을
    // 생성하고 등록한 직후 랜덤 상품을 초기화한다.
    //
    // 추후 WorldMap에서 ShopInventoryData와 Shop Level을
    // 전달하게 되면 이 옵션을 OFF로 설정한다.
    [SerializeField]
    private bool useShopInventoryTest = false;

    // <변경부분>
    // 테스트에 사용할 레벨별 상품 목록 SO.
    [SerializeField]
    private ShopInventoryData testShopInventoryData;

    // <변경부분>
    // 테스트할 상점 레벨.
    //
    // 실제 WorldMap 연결 후에는
    // Shop Node가 지정한 레벨을 전달한다.
    [SerializeField, Min(1)]
    private int testShopLevel = 1;


    // <변경부분>
    // 현재 배경 로드에서 상품 추첨이 완료되었는지 저장.
    //
    // 구매나 상품 선택 과정에서 재추첨되지 않게 한다.
    private bool hasInitializedShopInventory;


    [Header("Shop Purchase UI")]

    // <변경부분>
    // EventScene의 우측 하단 구매 Button.
    // 구매는 상품을 클릭하는 대신 이 버튼으로만 확정한다.
    [SerializeField]
    private Button purchaseButton;

    // <변경부분>
    // 구매 버튼이 처음 표시될 때 공용 팝업 글리치 연출을 재생한다.
    // Button GameObject에 PopupOpenAnimator를 부착하여 사용한다.
    [SerializeField]
    private PopupOpenAnimator purchaseButtonOpenAnimator;


    // <변경부분>
    // 상점을 나가 WorldMap으로 돌아가는 별도 UI 버튼.
    // 구매 버튼과 무관하게 ShopRoot가 표시될 때 사용할 수 있다.
    [Header("Shop Exit UI")]
    [Header("Shop Exit UI")]
    [SerializeField]
    private Button exitShopButton;

    // <변경부분>
    // Scene 이동 중 버튼 연속 클릭을 방지한다.
    private bool isExitingShop;

    // <변경부분>
    // EventSceneSequenceController에서 호출하여
    // 상점 나가기 버튼의 표시 여부를 제어한다.
    //
    // 버튼이 숨겨진 상태에서는 클릭도 불가능하다.
    // 기존 OnClick 및 월드맵 복귀 로직은 변경하지 않는다.
    public void SetExitShopButtonVisible(
        bool isVisible)
    {
        if (exitShopButton == null)
        {
            return;
        }

        // Button 자체를 비활성화하되
        // ShopController는 항상 활성 상태로 유지한다.
        exitShopButton.gameObject.SetActive(
            isVisible
        );

        exitShopButton.interactable =
            isVisible &&
            !isExitingShop;
    }


    [Header("Selected Item Tooltip")]

    // Tooltip의 기본 위치.
    // 상태효과 Section이 없을 때의 Canvas 기준 좌표다.
    [SerializeField]
    private Vector2 selectedItemTooltipCanvasPosition = Vector2.zero;

    // <변경부분>
    // 상태효과 Section 하나가 추가될 때마다
    // Tooltip 전체를 추가로 이동시킬 거리.
    //
    // X 양수 = 오른쪽
    // Y 양수 = 위쪽
    //
    // 예: Y = 80
    // Section 0개 → 추가 이동 없음
    // Section 1개 → 위로 80
    // Section 2개 → 위로 160
    //
    // 기존 ShopItemDisplay TooltipTrigger에서
    // 설정하던 Popup Offset Per Section과 같은 목적이다.
    [SerializeField]
    private Vector2 selectedItemTooltipOffsetPerSection = Vector2.zero;

    // 공용 TooltipPopupUI에서 상점이 표시한 Tooltip인지 추적한다.
    private bool isSelectedItemTooltipVisible;


    [Header("Run HUD")]

    // <변경부분>
    // 구매 성공 후 보유 아이템 UI를 갱신할 대상.
    //
    // Inspector에 직접 연결할 수 있으며,
    // 비어 있으면 현재 Scene에서 자동으로 찾는다.
    [SerializeField]
    private RunItemBarUI runItemBarUI;

    [Header("Purchase Failure Popup")]

    // <변경부분>
    // 상점 구매 실패 시 안내 메시지를 표시하는 팝업.
    //
    // Canvas 아래에 독립적으로 배치한
    // ShopFailurePopupUI를 Inspector에서 연결한다.
    //
    // 기존 BattleUIRoot / BattleUIController는 사용하지 않는다.
    [SerializeField]
    private ShopFailurePopupUI shopFailurePopupUI;


    // ============================================================
    // Event UI Localization
    // ============================================================

    // <변경부분>
    // 상점 UI 문구는 Battle_UI가 아닌
    // Event_UI String Table Collection에서 관리한다.
    //
    // 별도의 EventUILocalization 클래스를 생성하지 않고
    // ShopController에서 직접 LocalizedString을 조회한다.
    //
    // KR / EN / JA 번역 데이터의 SSOT는
    // Unity Event_UI String Table이다.

    // 보유 Gold 부족.
    private static readonly LocalizedString
        shopInsufficientGold =
            new LocalizedString(
                "Event_UI",
                "event.ui.shop.insufficient_gold"
            );

    // 아이템 보유 슬롯 부족.
    private static readonly LocalizedString
        shopInventoryFull =
            new LocalizedString(
                "Event_UI",
                "event.ui.shop.inventory_full"
            );


    // 현재 선택된 상품.
    //
    // 상점 전체에서 선택 상태는
    // 이 하나의 참조만 SSOT로 사용한다.
    private ShopItemDisplay
        selectedItemDisplay;


    public ShopItemDisplay
        SelectedItemDisplay =>
            selectedItemDisplay;


    private void Awake()
    {
        // 기존 구매 버튼 자동 연결 유지.
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(
                OnPurchaseButtonClicked
            );

            purchaseButton.onClick.AddListener(
                OnPurchaseButtonClicked
            );
        }
        else
        {
            Debug.LogWarning(
                "[ShopController] Purchase Button 참조가 비어 있습니다.",
                this
            );
        }

        // <변경부분>
        // 상점 나가기 버튼 자동 연결.
        // Inspector에서는 Button 참조만 연결하면 된다.
        if (exitShopButton != null)
        {
            exitShopButton.onClick.RemoveListener(
                OnExitShopButtonClicked
            );

            exitShopButton.onClick.AddListener(
                OnExitShopButtonClicked
            );
        }
        else
        {
            Debug.LogWarning(
                "[ShopController] Exit Shop Button 참조가 비어 있습니다.",
                this
            );
        }

        // <변경부분>
        // EventScene 진입 직후에는
        // 상점 나가기 버튼을 무조건 숨긴다.
        //
        // 이후 EventSceneSequenceController가
        // 필요한 이벤트에서만 다시 표시한다.
        SetExitShopButtonVisible(false);

        CacheShopItems();

        ClearSelection();
    }


    // <변경부분>
    // 구매 버튼과 나가기 버튼의 런타임 이벤트를 정리한다.
    private void OnDestroy()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(
                OnPurchaseButtonClicked
            );
        }

        // <변경부분>
        if (exitShopButton != null)
        {
            exitShopButton.onClick.RemoveListener(
                OnExitShopButtonClicked
            );
        }
    }


    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;

        // <변경부분>
        // 상점 UI가 다시 활성화될 때 종료 상태 초기화.
        isExitingShop = false;

        if (exitShopButton != null)
        {
            exitShopButton.interactable = true;
        }

        CacheShopItems();

        ClearSelection();
    }


    private void OnDisable()
    {
        // <변경부분>
        // ShopRoot 비활성화 시 선택 Tooltip을 정리한다.
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        HideSelectedItemTooltip();

        // <변경부분>
        // ShopRoot / EventScene이 비활성화되는 과정에서는
        // 자식 ShopItemDisplay들도 이미 비활성 상태일 수 있다.
        //
        // 이 시점에 ClearSelection()을 호출하면
        // ShopItemVisualAnimator.SetSelected()가
        // 비활성 GameObject에서 Coroutine을 다시 시작하려고 할 수 있다.
        //
        // 실제 선택 비주얼 초기화는
        // 다음 OnEnable()의 ClearSelection()에서 안전하게 처리한다.
        selectedItemDisplay =
     null;

        // <변경부분>
        // 다른 EventScene에 구매 버튼이 남지 않게 한다.
        UpdatePurchaseButtonState();

        // <변경부분>
        // 상점 컨트롤러가 비활성화되면
        // 종료 버튼도 남아 있지 않도록 숨긴다.
        SetExitShopButtonVisible(false);
    }


    // <변경부분>
    // Inspector 배열이 비어 있으면
    // ShopController 아래의 모든 상품을 자동 수집한다.
    private void CacheShopItems()
    {
        if (shopItemDisplays != null &&
            shopItemDisplays.Length > 0)
        {
            return;
        }


        shopItemDisplays =
            GetComponentsInChildren<
                ShopItemDisplay
            >(
                true
            );
    }


    // ============================================================
    // Runtime Shop Item Registration
    // ============================================================

    // <변경부분>
    // BackgroundManager가 ShopRoot 아래에
    // 상품 진열 프리팹을 생성한 뒤 호출한다.
    //
    // 기존 CacheShopItems()와 달리
    // Inspector 배열에 이전 참조가 남아 있어도
    // 현재 Hierarchy를 기준으로 상품 목록을 다시 구성한다.
    //
    // 수집 대상:
    // - ShopController의 하위에 존재
    // - 현재 활성화된 ShopItemDisplay
    // - ShopDisplaySlotInstance가 부착된 상품
    //
    // 다른 배경에서 제거된 비활성 상품과
    // 수동 배치된 일반 오브젝트는 제외한다.
    //
    // 실제 아이템과 가격 초기화는
    // 이후 ShopInventoryData 연결 단계에서 처리한다.
    public void RefreshShopItemsFromHierarchy()
    {
        // <변경부분>
        // 이전 배경에 속한 선택 참조를 정리한다.
        //
        // 이전 상품은 이미 비활성화되었을 수 있으므로
        // SetSelected()를 호출하지 않는다.
        HideSelectedItemTooltip();
        selectedItemDisplay = null;


        ShopItemDisplay[] discoveredItems =
            GetComponentsInChildren<ShopItemDisplay>(
                true
            );


        // <변경부분>
        // BackgroundManager가 생성한
        // 활성 Shop 상품만 새 목록으로 등록한다.
        shopItemDisplays =
            System.Array.FindAll(
                discoveredItems,
                itemDisplay =>
                    itemDisplay != null &&
                    itemDisplay.gameObject.activeInHierarchy &&
                    itemDisplay.GetComponent<
                        ShopDisplaySlotInstance
                    >() != null
            );


        // <변경부분>
        // 새 배경에서 상품을 수집했으므로
        // 이번 상점의 초기화 상태를 다시 준비한다.
        hasInitializedShopInventory = false;


        // <변경부분>
        // 새 상품들의 선택 연출을 초기화한다.
        //
        // 이 시점에는 아직 아이템 데이터를 변경하지 않는다.
        ClearSelection();


        Debug.Log(
            "[ShopController] 런타임 상품 등록 완료: " +
            $"{shopItemDisplays.Length}개",
            this
        );


        // <변경부분>
        // WorldMap 연결 전 임시 테스트 경로.
        //
        // BackgroundManager의 상품 생성이 완료된 뒤
        // 호출되는 함수이므로 이 시점에 추첨할 수 있다.
        //
        // 실제 WorldMap 연결 후에는
        // useShopInventoryTest를 OFF로 설정하고
        // 외부에서 InitializeShopInventory()를 호출한다.
        if (useShopInventoryTest)
        {
            InitializeShopInventory(
                testShopInventoryData,
                testShopLevel
            );
        }
    }

    // ============================================================
    // Shop Inventory Initialization
    // ============================================================

    // <변경부분>
    // 지정된 ShopInventoryData의 레벨에서
    // 판매할 상품을 무작위로 선택해 진열한다.
    //
    // 현재는 Inspector 테스트에서 사용하고,
    // 추후 WorldMap Shop Node가 EventScene으로 전달한
    // ShopInventoryData와 Shop Level을 이 함수에 전달한다.
    //
    // 규칙:
    // - 가중치가 0 이하인 상품은 제외
    // - BattleItemData가 없는 상품은 제외
    // - 동일 BattleItemData 중복 진열 방지
    // - 가격은 ShopInventoryData에 설정한 값 사용
    // - 남는 진열대는 비활성화
    // - 동일한 배경 로드에서는 중복 초기화 방지
    public void InitializeShopInventory(
        ShopInventoryData inventoryData,
        int shopLevel)
    {
        // <변경부분>
        // 이미 이번 방문의 상품을 결정했다면
        // 구매 상태가 초기화되지 않도록 다시 추첨하지 않는다.
        if (hasInitializedShopInventory)
        {
            Debug.Log(
                "[ShopController] " +
                "이미 상품 초기화가 완료되어 재추첨을 건너뜁니다.",
                this
            );

            return;
        }


        if (inventoryData == null)
        {
            Debug.LogWarning(
                "[ShopController] ShopInventoryData가 없습니다.",
                this
            );

            return;
        }


        // <변경부분>
        // 정확히 일치하는 상점 레벨의 후보 목록을 조회한다.
        ShopInventoryLevelData levelData =
            inventoryData.GetLevelData(
                shopLevel
            );


        // <변경부분>
        // 현재 등록된 진열대가 없다면 추첨하지 않는다.
        if (shopItemDisplays == null ||
            shopItemDisplays.Length == 0)
        {
            Debug.LogWarning(
                "[ShopController] 등록된 상품 진열대가 없습니다.",
                this
            );

            return;
        }


        // <변경부분>
        // WorldMap 상점에 이전 방문의 재고가 있다면
        // 최초 추첨 상품·가격·SOLD 상태를 그대로 복원한다.
        // 단독 EventScene 테스트에는 저장 정보가 없으므로 기존 추첨 경로를 사용한다.
        IReadOnlyList<WorldMapRuntimeState.ShopStockSlot> savedSlots;
        if (WorldMapRuntimeState.TryGetActiveShopStock(out savedSlots))
        {
            ClearSelection();

            if (savedSlots.Count != shopItemDisplays.Length)
            {
                Debug.LogWarning(
                    "[ShopController] 상점 진열대 개수가 최초 방문과 다릅니다. " +
                    $"저장 {savedSlots.Count} / 현재 {shopItemDisplays.Length}",
                    this
                );
            }

            for (int i = 0; i < shopItemDisplays.Length; i++)
            {
                ShopItemDisplay display = shopItemDisplays[i];
                if (display == null)
                {
                    continue;
                }

                WorldMapRuntimeState.ShopStockSlot slot =
                    i < savedSlots.Count ? savedSlots[i] : null;

                if (slot == null || slot.ItemData == null)
                {
                    // <변경부분>
                    // 최초 방문 때 비어 있던 진열대는 계속 비워둔다.
                    display.Initialize(null, 0);
                    display.gameObject.SetActive(false);
                    continue;
                }

                // <변경부분>
                // 최초 추첨에서 결정된 상품과 가격 복원.
                display.Initialize(slot.ItemData, slot.Price);

                if (slot.IsSoldOut)
                {
                    // <변경부분>
                    // 실제 구매 후 사용하던 기존 SOLD 처리 함수를 재사용.
                    // 아이콘 제거, SOLD 문구, Collider 차단,
                    // 그림자와 Float 비활성화를 함께 복원한다.
                    display.MarkAsSoldOut();
                }
            }

            // <변경부분>
            // 복원도 초기화 완료로 간주하여 재추첨을 방지한다.
            hasInitializedShopInventory = true;

            Debug.Log(
                "[ShopController] 기존 상점 상품 및 SOLD 상태 복원 완료",
                this
            );

            return;
        }


        // <변경부분>
        // 원본 ScriptableObject의 후보 목록은
        // 변경하지 않고 별도의 런타임 목록을 구성한다.
        List<ShopInventoryItemEntry> candidates =
            new List<ShopInventoryItemEntry>();

        HashSet<BattleItemData> registeredItems =
            new HashSet<BattleItemData>();


        if (levelData != null &&
            levelData.itemCandidates != null)
        {
            for (int i = 0;
                 i < levelData.itemCandidates.Count;
                 i++)
            {
                ShopInventoryItemEntry entry =
                    levelData.itemCandidates[i];


                if (entry == null ||
                    entry.itemData == null ||
                    entry.itemData.itemType ==
                        BattleItemType.None ||
                    entry.weight <= 0)
                {
                    continue;
                }


                // <변경부분>
                // 같은 BattleItemData가 후보 목록에
                // 여러 번 들어 있더라도 최초 항목만 등록한다.
                //
                // 가격·가중치를 다르게 지정하려면
                // 서로 다른 BattleItemData를 사용해야 한다.
                if (!registeredItems.Add(
                        entry.itemData))
                {
                    continue;
                }


                candidates.Add(
                    entry
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[ShopController] " +
                $"Shop Level {shopLevel}의 데이터가 없습니다.",
                this
            );
        }


        // <변경부분>
        // 이번 방문의 모든 상품 선택 상태를 초기화한다.
        ClearSelection();


        int assignedCount = 0;


        for (int i = 0;
             i < shopItemDisplays.Length;
             i++)
        {
            ShopItemDisplay display =
                shopItemDisplays[i];


            if (display == null)
            {
                continue;
            }


            // <변경부분>
            // 남은 후보가 없으면 진열대를 비운 뒤 숨긴다.
            //
            // 프리팹에 Inspector 테스트용 아이템이
            // 남아 있더라도 구매할 수 없도록 초기화한다.
            if (candidates.Count == 0)
            {
                display.Initialize(
                    null,
                    0
                );

                display.gameObject.SetActive(
                    false
                );

                continue;
            }


            // <변경부분>
            // 남아 있는 후보들의 가중치를 기준으로
            // 이번 진열대에 배정할 상품을 추첨한다.
            int selectedIndex =
                GetWeightedRandomShopItemIndex(
                    candidates
                );


            if (selectedIndex < 0)
            {
                display.Initialize(
                    null,
                    0
                );

                display.gameObject.SetActive(
                    false
                );

                continue;
            }


            ShopInventoryItemEntry selectedEntry =
                candidates[selectedIndex];


            // <변경부분>
            // 기존 ShopItemDisplay 초기화 함수를 사용한다.
            //
            // 아이콘, 가격, Tooltip, Collider,
            // SOLD 해제 및 기본 연출까지
            // ShopItemDisplay에서 처리한다.
            display.Initialize(
                selectedEntry.itemData,
                selectedEntry.price
            );


            assignedCount++;


            // <변경부분>
            // 선택된 후보는 제거하여
            // 같은 상품이 중복 진열되지 않도록 한다.
            candidates.RemoveAt(
                selectedIndex
            );
        }


        // <변경부분>
        // 최초 방문의 전체 진열대 상태를 노드별 런타임 재고로 저장한다.
        // 비어 있어 숨긴 진열대도 null 슬롯으로 저장해 재방문 때 그대로 복원한다.
        if (!string.IsNullOrWhiteSpace(WorldMapRuntimeState.ActiveShopNodeId))
        {
            List<WorldMapRuntimeState.ShopStockSlot> initialSlots =
                new List<WorldMapRuntimeState.ShopStockSlot>();

            for (int i = 0; i < shopItemDisplays.Length; i++)
            {
                ShopItemDisplay display = shopItemDisplays[i];

                initialSlots.Add(
                    new WorldMapRuntimeState.ShopStockSlot(
                        display != null && display.HasItem
                            ? display.ItemData
                            : null,
                        display != null && display.HasItem
                            ? display.Price
                            : 0
                    )
                );
            }

            if (!WorldMapRuntimeState.TrySaveActiveShopStock(initialSlots))
            {
                Debug.LogWarning(
                    "[ShopController] 최초 상점 재고 저장 실패: " +
                    WorldMapRuntimeState.ActiveShopNodeId,
                    this
                );
            }
        }


        // <변경부분>
        // 한 번 방문에서 상품을 다시 추첨하지 않도록 고정한다.
        hasInitializedShopInventory = true;


        Debug.Log(
            "[ShopController] 상품 랜덤 초기화 완료: " +
            $"Level {shopLevel} / " +
            $"{assignedCount}개 진열",
            this
        );


        Debug.Log(
            "[ShopController] 상품 랜덤 초기화 완료: " +
            $"Level {shopLevel} / " +
            $"{assignedCount}개 진열",
            this
        );
    }


    // ============================================================
    // Weighted Random Selection
    // ============================================================

    // <변경부분>
    // 현재 남은 상품 후보의 Weight 비율에 따라
    // 하나의 후보 인덱스를 반환한다.
    //
    // 리스트 자체는 이 함수에서 변경하지 않는다.
    //
    // 반환값:
    // 0 이상 = 선택된 후보 인덱스
    // -1 = 유효한 후보 없음
    private int GetWeightedRandomShopItemIndex(
        List<ShopInventoryItemEntry> candidates)
    {
        if (candidates == null ||
            candidates.Count == 0)
        {
            return -1;
        }


        // <변경부분>
        // 많은 후보와 큰 Weight 값에도
        // 합계가 int 범위를 넘지 않도록 long 사용.
        long totalWeight = 0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            ShopInventoryItemEntry entry =
                candidates[i];

            if (entry == null ||
                entry.weight <= 0)
            {
                continue;
            }

            totalWeight +=
                entry.weight;
        }


        if (totalWeight <= 0)
        {
            return -1;
        }


        // <변경부분>
        // 0 ~ 전체 가중치 사이에서 무작위 값 선택.
        double randomValue =
            UnityEngine.Random.value *
            (double)totalWeight;


        long cumulativeWeight = 0;

        int lastValidIndex = -1;


        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            ShopInventoryItemEntry entry =
                candidates[i];

            if (entry == null ||
                entry.weight <= 0)
            {
                continue;
            }


            cumulativeWeight +=
                entry.weight;

            lastValidIndex =
                i;


            if (randomValue <
                cumulativeWeight)
            {
                return i;
            }
        }


        // <변경부분>
        // Random.value의 최댓값 또는
        // 부동소수점 반올림으로 경계값이 나온 경우
        // 마지막 유효한 후보를 반환한다.
        return lastValidIndex;
    }

    // <변경부분>
    // 상품 클릭은 선택과 정보 표시만 담당한다.
    // 같은 상품을 다시 클릭해도 구매하지 않는다.
    public void HandleItemClicked(
        ShopItemDisplay clickedItem)
    {
        if (clickedItem == null ||
            clickedItem.HasItem == false)
        {
            return;
        }

        // <변경부분>
        // 다른 상품으로 변경할 때만 기존 Outline을 정리한다.
        if (selectedItemDisplay != clickedItem)
        {
            if (selectedItemDisplay != null)
            {
                selectedItemDisplay.SetSelected(false);
            }

            selectedItemDisplay = clickedItem;
            selectedItemDisplay.SetSelected(true);
        }

        // <변경부분>
        // 선택 정보는 화면의 고정 위치에 표시한다.
        ShowSelectedItemTooltip();
        UpdatePurchaseButtonState();

        Debug.Log(
            "[ShopController] " +
            $"상품 선택: " +
            $"{selectedItemDisplay.ItemData?.itemName} / " +
            $"{selectedItemDisplay.Price} Gold",
            selectedItemDisplay
        );
    }

    // ============================================================
    // Shop Selection Tooltip / Purchase Button
    // ============================================================

    // <변경부분>
    // EventScene 구매 Button의 OnClick에서 호출한다.
    // 상품 클릭은 이 함수를 직접 호출하지 않는다.
    public void OnPurchaseButtonClicked()
    {
        // <변경부분>
        // 버튼의 클릭 이벤트가 실제로 전달되는지 확인한다.
        Debug.Log(
            "[ShopController] 구매 버튼 OnClick 수신",
            this
        );

        if (selectedItemDisplay == null ||
            selectedItemDisplay.HasItem == false)
        {
            Debug.LogWarning(
                "[ShopController] 구매 취소: 선택된 판매 상품이 없습니다.",
                this
            );

            return;
        }

        // <변경부분>
        // EventSequence 잠금 또는 Collider 문제로 구매가
        // 중단되는 경우 원인을 Console에서 확인할 수 있게 한다.
        if (selectedItemDisplay.IsInteractionAllowed == false)
        {
            Debug.LogWarning(
                "[ShopController] 구매 취소: EventScene 입력이 잠겨 있거나 " +
                "상품 Collider가 비활성화되어 있습니다.",
                this
            );

            return;
        }

        // 기존 RunState 구매 / SOLD / 실패 팝업 처리 재사용.
        TryPurchaseSelectedItem(
            selectedItemDisplay
        );
    }


    // <변경부분>
    // 상점 나가기 버튼의 OnClick.
    //
    // WorldMap에서 방문한 상점만 정상 종료할 수 있다.
    // 현재 Gold, Item, End Timer 값은 변경하지 않는다.
    public void OnExitShopButtonClicked()
    {
        // <변경부분>
        // 이미 Scene 이동을 요청했거나
        // ShopController가 비활성화되어 있다면 실행하지 않는다.
        if (isExitingShop || !isActiveAndEnabled)
        {
            return;
        }

        // <변경부분>
        // 이벤트 연출이 플레이어 상호작용을 잠근 상태라면
        // 상점 종료를 허용하지 않는다.
        EventSceneSequenceController sequenceController =
            FindFirstObjectByType<EventSceneSequenceController>();

        if (sequenceController != null &&
            sequenceController.IsPlayerInteractionLocked)
        {
            Debug.LogWarning(
                "[ShopController] 상점 나가기 대기: " +
                "이벤트 연출이 진행 중입니다.",
                this
            );

            return;
        }

        // <변경부분>
        // 상점에 들어오기 직전 실제 월드맵 Scene 이름을 사용한다.
        // Scene 이름을 코드에 하드코딩하지 않는다.
        string returnSceneName =
            WorldMapRuntimeState.ShopReturnSceneName;

        // <변경부분>
        // 월드맵을 통해 진입하지 않은 단독 EventScene 테스트,
        // 잘못된 Scene 등록 등을 차단한다.
        if (string.IsNullOrWhiteSpace(returnSceneName) ||
            returnSceneName == gameObject.scene.name ||
            !Application.CanStreamedLevelBeLoaded(returnSceneName))
        {
            Debug.LogWarning(
                "[ShopController] 상점 나가기 실패: " +
                "복귀할 WorldMap Scene이 없거나 " +
                "Build Profiles Scene List에 등록되지 않았습니다. " +
                $"Scene: {returnSceneName}",
                this
            );

            return;
        }

        // <변경부분>
        // 상점 이용 완료를 WorldMapRuntimeState에 기록한다.
        //
        // 이 단계에서 다음 노드를 직접 해금하지 않는다.
        // 월드맵이 다시 시작되면 완료 정보를 소비하여 처리한다.
        if (!WorldMapRuntimeState.TryCompleteShopVisit())
        {
            Debug.LogWarning(
                "[ShopController] 상점 나가기 실패: " +
                "월드맵 상점 진입 정보가 없습니다.",
                this
            );

            return;
        }

        // <변경부분>
        // 중복 종료 및 구매 입력 방지.
        isExitingShop = true;

        if (exitShopButton != null)
        {
            exitShopButton.interactable = false;
        }

        if (purchaseButton != null)
        {
            purchaseButton.interactable = false;
        }

        // <변경부분>
        // Scene을 떠나기 전에 상점 Tooltip을 정리한다.
        // 기존 선택 비주얼 전체 초기화는 강제하지 않는다.
        HideSelectedItemTooltip();

        // <변경부분>
        // 기존 RunStateManager를 초기화하지 않고 Scene만 전환한다.
        WorldMapEventRuntimeState.Queue(returnWorldMapEventData);
        SceneManager.LoadScene(returnSceneName);
    }


    // <변경부분>
    // 선택된 아이템의 공용 TooltipViewData를 사용하되
    // 마우스가 아닌 Root Canvas의 고정 좌표에 표시한다.
    private void ShowSelectedItemTooltip()
    {
        if (selectedItemDisplay == null ||
            selectedItemDisplay.HasItem == false)
        {
            HideSelectedItemTooltip();
            return;
        }

        if (TooltipPopupUI.Instance == null)
        {
            Debug.LogWarning(
                "[ShopController] TooltipPopupUI.Instance가 없습니다.",
                this
            );
            return;
        }

        TooltipViewData viewData =
            TooltipViewData.FromBattleItemData(
                selectedItemDisplay.ItemData
            );

        if (viewData == null)
        {
            HideSelectedItemTooltip();
            return;
        }

        // <변경부분>
        // 실제 표시할 Tooltip Section 개수를 계산한다.
        // null Section은 위치 보정에 포함하지 않는다.
        int validSectionCount = 0;

        if (viewData.sections != null)
        {
            for (int i = 0; i < viewData.sections.Count; i++)
            {
                if (viewData.sections[i] != null)
                {
                    validSectionCount++;
                }
            }
        }

        // <변경부분>
        // 기본 고정 위치에 Section 개수별 이동값을 더한다.
        //
        // Section이 늘어날수록 기본 Tooltip부터
        // 모든 Section이 하나의 묶음으로 위로 이동한다.
        Vector2 adjustedTooltipPosition =
            selectedItemTooltipCanvasPosition +
            selectedItemTooltipOffsetPerSection *
            validSectionCount;

        // <변경부분>
        // 상점의 상품 선택 Tooltip은 기존처럼
        // FixedCanvasPosition 모드를 사용한다.
        //
        // PopupRoot 전체 좌표를 변경하므로
        // SectionParent 내부 자동 정렬과 충돌하지 않는다.
        TooltipPopupUI.Instance.Show(
            viewData,
            Vector2.zero,
            TooltipPositionMode.FixedCanvasPosition,
            Vector2.zero,
            adjustedTooltipPosition,
            Vector2.zero,
            0f
        );

        isSelectedItemTooltipVisible = true;
    }


    // <변경부분>
    // 구매 또는 선택 해제 시 상점 Tooltip을 정리한다.
    private void HideSelectedItemTooltip()
    {
        if (isSelectedItemTooltipVisible &&
            TooltipPopupUI.Instance != null)
        {
            TooltipPopupUI.Instance.Hide();
        }

        isSelectedItemTooltipVisible = false;
    }
    // <변경부분>
    // 선택한 상품이 있을 때만 구매 버튼을 표시한다.
    // TooltipPopupUI.Show() 직후 호출되므로 팝업과 같은 프레임에 등장한다.
    // 같은 상품 재클릭이나 다른 상품 선택 때도 글리치 연출을 재생한다.
    private void UpdatePurchaseButtonState()
    {
        if (purchaseButton == null)
        {
            return;
        }

        bool hasSelection =
            selectedItemDisplay != null &&
            selectedItemDisplay.HasItem;

        bool wasButtonVisible =
            purchaseButton.gameObject.activeSelf;

        purchaseButton.interactable = hasSelection;

        if (hasSelection == false)
        {
            if (wasButtonVisible)
            {
                purchaseButton.gameObject.SetActive(false);
            }

            return;
        }

        // <변경부분>
        // 최초 선택 시에만 버튼을 활성화한다.
        // 이미 보이는 버튼은 비활성화하지 않아 클릭 입력을 유지한다.
        if (wasButtonVisible == false)
        {
            purchaseButton.gameObject.SetActive(true);
        }

        // <변경부분>
        // Inspector에 다른 UI의 Animator를 잘못 연결한 경우에도
        // 실제 구매 버튼에 부착된 PopupOpenAnimator를 사용한다.
        PopupOpenAnimator buttonAnimator =
            purchaseButton.GetComponent<PopupOpenAnimator>();

        if (purchaseButtonOpenAnimator != buttonAnimator)
        {
            purchaseButtonOpenAnimator =
                buttonAnimator;
        }

        // <변경부분>
        // 버튼의 현재 표시 여부와 관계없이 상품을 클릭할 때마다
        // TooltipPopupUI와 같은 프레임에 글리치 오픈 연출을 재생한다.
        if (purchaseButtonOpenAnimator != null &&
            purchaseButtonOpenAnimator.isActiveAndEnabled)
        {
            purchaseButtonOpenAnimator.PlayOpen();
        }
        else
        {
            Debug.LogWarning(
                "[ShopController] 구매 버튼 GameObject에 활성화된 " +
                "PopupOpenAnimator가 없습니다. 같은 오브젝트에 추가하고 " +
                "Animation Data를 연결해주세요.",
                this
            );
        }
    }


    // <변경부분>
    // 실행 중 언어가 변경되면 현재 BattleItemData를 기준으로
    // 선택한 아이템의 Tooltip 문자열을 다시 생성한다.
    private void OnSelectedLocaleChanged(Locale selectedLocale)
    {
        if (selectedItemDisplay != null &&
            selectedItemDisplay.HasItem)
        {
            ShowSelectedItemTooltip();
        }
    }


    // <변경부분>
    // 현재 선택된 상품의 실제 구매를 처리한다.
    //
    // RunStateManager의 구매 함수를 통해
    // Gold 차감과 아이템 지급을 함께 처리한다.
    //
    // 구매 성공:
    // - 상품 선택 해제
    // - 구매한 상품 표시 및 입력 제거
    // - Run Item Bar 갱신
    //
    // 구매 실패:
    // - 기존 선택 상태 유지
    // - Gold 및 아이템 데이터 변경 없음
    // - 현재 단계에서는 Console 로그로 실패 이유 확인
    private void TryPurchaseSelectedItem(
        ShopItemDisplay clickedItem)
    {
        if (clickedItem == null ||
            clickedItem.HasItem == false)
        {
            return;
        }


        RunStateManager runStateManager =
            RunStateManager.Instance;


        if (runStateManager == null)
        {
            Debug.LogWarning(
                "[ShopController] 구매 실패: " +
                "RunStateManager가 존재하지 않습니다.",
                this
            );

            return;
        }


        // 구매 완료 후 상품 데이터를 비울 예정이므로
        // 구매 전 상품명과 가격을 미리 보관한다.
        string purchasedItemName =
            clickedItem.ItemData.itemName;

        int purchasedPrice =
            clickedItem.Price;


        // <변경부분>
        // 실제 아이템 지급 및 Gold 차감을
        // RunStateManager에서 한 번에 처리한다.
        RunStateManager.BattleItemPurchaseResult result =
            runStateManager.TryPurchaseBattleItem(
                clickedItem.ItemData,
                purchasedPrice,
                RunStateManager.MaxBattleItemCount
            );


        // <변경부분>
        // 구매에 실패하면 상품 선택 상태를 유지하고
        // 실제 RunState 데이터를 추가로 변경하지 않는다.
        if (result !=
            RunStateManager.BattleItemPurchaseResult.Success)
        {
            switch (result)
            {
                case RunStateManager.BattleItemPurchaseResult.InsufficientGold:

                    // <변경부분>
                    // Event_UI 테이블에서
                    // 현재 Locale의 Gold 부족 문구를 가져온다.
                    ShowPurchaseFailurePopup(
                        GetShopLocalizedText(
                            shopInsufficientGold,
                            "금화가 부족합니다."
                        )
                    );

                    Debug.Log(
                        "[ShopController] 구매 실패: Gold 부족 / " +
                        $"필요 {purchasedPrice}G / " +
                        $"보유 {runStateManager.GetGoldAmount()}G",
                        clickedItem
                    );

                    break;


                case RunStateManager.BattleItemPurchaseResult.InventoryFull:

                    // <변경부분>
                    // Event_UI 테이블에서
                    // 현재 Locale의 아이템 슬롯 부족 문구를 가져온다.
                    ShowPurchaseFailurePopup(
                        GetShopLocalizedText(
                            shopInventoryFull,
                            "아이템을 더 보유할 수 없습니다."
                        )
                    );

                    Debug.Log(
                        "[ShopController] 구매 실패: " +
                        $"아이템 슬롯 부족 / " +
                        $"최대 {RunStateManager.MaxBattleItemCount}개",
                        clickedItem
                    );

                    break;


                default:

                    // 예상하지 못한 구매 실패는 기존 로그를 유지한다.
                    Debug.LogWarning(
                        "[ShopController] 구매 실패: " +
                        $"{result}",
                        clickedItem
                    );

                    break;
            }

            return;
        }


        // ========================================================
        // 구매 성공
        // ========================================================

        // <변경부분>
        // 구매한 상품의 선택 Outline을 해제한다.
        clickedItem.SetSelected(
            false
        );

        // ShopController의 선택 상태도 초기화한다.
        selectedItemDisplay =
            null;

        // <변경부분>
        // 구매 성공한 상품의 정보창과 구매 버튼을 닫는다.
        HideSelectedItemTooltip();
        UpdatePurchaseButtonState();


        // <변경부분>
        // 구매 완료 상품을 SOLD 상태로 변경한다.
        //
        // ShopItemDisplay.MarkAsSoldOut()에서:
        // - 아이템 Sprite 제거
        // - 가격 대신 SOLD Text 표시
        // - Tooltip 및 Collider 차단
        // - 그림자 제거
        // - Float / Outline 애니메이션 종료
        //
        // 진열대 GameObject 자체는 유지한다.

        // <변경부분>
        // 구매로 Gold/아이템 처리가 성공한 뒤에만
        // 이번 런의 해당 상점 진열대를 SOLD로 저장한다.
        // UI에서 itemData를 제거하기 전에 원본 참조로 기록한다.
        if (!string.IsNullOrWhiteSpace(WorldMapRuntimeState.ActiveShopNodeId))
        {
            int soldSlotIndex =
                System.Array.IndexOf(shopItemDisplays, clickedItem);

            if (!WorldMapRuntimeState.TryMarkActiveShopStockSold(
                    soldSlotIndex,
                    clickedItem.ItemData,
                    clickedItem.Price))
            {
                Debug.LogError(
                    "[ShopController] 구매는 성공했지만 상점 SOLD 기록에 실패했습니다. " +
                    $"Slot: {soldSlotIndex}",
                    this
                );
            }
        }

        // <변경부분>
        // 현재 Scene의 상품 표시도 기존대로 SOLD 처리한다.
        clickedItem.MarkAsSoldOut();


        // <변경부분>
        // 현재 Scene의 RunItemBarUI를 찾아서


        // <변경부분>
        // 현재 Scene의 RunItemBarUI를 찾아서
        // 구매 후 보유 아이템 표시를 즉시 갱신한다.
        // 구매 후 보유 아이템 표시를 즉시 갱신한다.
        if (runItemBarUI == null)
        {
            runItemBarUI =
                UnityEngine.Object
                    .FindFirstObjectByType<
                        RunItemBarUI
                    >();
        }


        if (runItemBarUI != null)
        {
            runItemBarUI.RefreshFromRunState();
        }
        else
        {
            Debug.LogWarning(
                "[ShopController] 구매는 성공했지만 " +
                "RunItemBarUI를 찾지 못했습니다.",
                this
            );
        }


        // RunGoldUI는 Update()에서
        // RunStateManager의 Gold 변경을 자동 감지하므로
        // 별도의 Refresh 호출이 필요하지 않다.

        Debug.Log(
      "[ShopController] 구매 완료: " +
      $"{purchasedItemName} / " +
      $"가격 {purchasedPrice}G / " +
      $"남은 Gold {runStateManager.GetGoldAmount()}G",
      this
  );
    }


    // <변경부분>
    // 상점 구매 실패 팝업을 표시하는 공통 진입점.
    //
    // 구매 실패 이유에 대한 Localization은
    // 각 구매 결과 분기에서 처리한다.
    //
    // 이 함수는 전달받은 메시지를
    // 기존 Popup 디자인으로 표시하기만 한다.
    private void ShowPurchaseFailurePopup(
        string message)
    {
        if (shopFailurePopupUI == null)
        {
            Debug.LogWarning(
                "[ShopController] " +
                "ShopFailurePopupUI가 연결되지 않았습니다.",
                this
            );

            return;
        }

        shopFailurePopupUI.Show(
        message
    );
    }


    // <변경부분>
    // ShopController에서 Event_UI의 문자열을 직접 조회한다.
    //
    // - 실제 번역은 Event_UI String Table을 SSOT로 사용
    // - 현재 Unity Locale의 번역을 반환
    // - 문자열이 비어 있으면 한국어 기본 문구를 사용
    //
    // 다른 Localization 전용 클래스를 거치지 않는다.
    private static string GetShopLocalizedText(
        LocalizedString localizedString,
        string fallbackText)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return fallbackText;
        }

        string localizedText =
            localizedString.GetLocalizedString();

        if (string.IsNullOrWhiteSpace(localizedText))
        {
            return fallbackText;
        }

        return localizedText;
    }


    // <변경부분>
    // 현재 상품 선택을 모두 초기화한다.
    public void ClearSelection()
    {
        if (selectedItemDisplay != null)
        {
            selectedItemDisplay.SetSelected(
                false
            );
        }


        selectedItemDisplay =
    null;

        // <변경부분>
        // 상품 선택을 지우면 Tooltip과 구매 버튼도 함께 숨긴다.
        HideSelectedItemTooltip();
        UpdatePurchaseButtonState();


        // Scene 시작 시
        // ShopItemVisualAnimator의 테스트용
        // Start Selected 값이 남아 있는 경우까지 포함하여
        // 모든 상품의 선택 비주얼을 OFF로 정리한다.
        if (shopItemDisplays == null)
        {
            return;
        }


        for (int i = 0;
             i < shopItemDisplays.Length;
             i++)
        {
            ShopItemDisplay itemDisplay =
                shopItemDisplays[i];

            if (itemDisplay == null)
            {
                continue;
            }


            itemDisplay.SetSelected(
                false
            );
        }
    }
}