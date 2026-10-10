using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;


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
        CacheShopItems();

        ClearSelection();
    }


    private void OnEnable()
    {
        CacheShopItems();

        ClearSelection();
    }


    private void OnDisable()
    {
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
        // 한 번 방문에서 상품을 다시 추첨하지 않도록 고정한다.
        hasInitializedShopInventory = true;


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
    // ShopItemDisplay에서 상품 Click을 전달받는다.
    //
    // 첫 클릭:
    // - 상품 선택
    // - 선택 Outline 활성화
    //
    // 같은 상품 재클릭:
    // - 실제 구매 처리
    public void HandleItemClicked(
        ShopItemDisplay clickedItem)
    {
        if (clickedItem == null ||
            clickedItem.HasItem == false)
        {
            return;
        }


        // <변경부분>
        // 이미 선택된 상품을 다시 클릭했다면
        // 실제 구매 처리를 요청한다.
        if (selectedItemDisplay ==
            clickedItem)
        {
            TryPurchaseSelectedItem(
                clickedItem
            );

            return;
        }


        // 기존 선택 상품이 있다면
        // 먼저 Outline을 해제한다.
        if (selectedItemDisplay != null)
        {
            selectedItemDisplay.SetSelected(
                false
            );
        }


        selectedItemDisplay =
            clickedItem;


        // 새 상품의 자동 Outline을 활성화한다.
        selectedItemDisplay.SetSelected(
            true
        );


        Debug.Log(
            "[ShopController] " +
            $"상품 선택: " +
            $"{selectedItemDisplay.ItemData?.itemName} / " +
            $"{selectedItemDisplay.Price} Gold",
            selectedItemDisplay
        );
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
        clickedItem.MarkAsSoldOut();


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