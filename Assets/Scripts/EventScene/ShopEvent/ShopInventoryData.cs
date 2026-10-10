
using System;
using System.Collections.Generic;
using UnityEngine;


// ============================================================
// Shop Inventory Data
// ============================================================

// <변경부분>
// WorldMap Shop Node에서 지정한 상점 레벨에 따라
// 판매 가능한 BattleItemData 후보를 관리한다.
//
// 이 데이터는 상품 종류, 가격, 등장 가중치만 소유한다.
//
// 상품 프리팹과 배치 위치는 BackgroundMapData의
// ShopDisplaySlots가 별도로 관리한다.
//
// 실제 랜덤 추첨 및 구매 상태는
// 추후 ShopController에서 처리한다.
[CreateAssetMenu(
    fileName = "ShopInventoryData",
    menuName = "Devorya/Shop/Shop Inventory Data"
)]
public class ShopInventoryData : ScriptableObject
{
    [Header("Shop Inventory Levels")]

    // <변경부분>
    // 레벨별 판매 상품 후보 목록.
    //
    // WorldMap에서 Shop Level을 지정하면
    // 해당 레벨과 일치하는 목록을 사용한다.
    //
    // 레벨 개수는 고정하지 않는다.
    public List<ShopInventoryLevelData> levels =
        new List<ShopInventoryLevelData>();


    // <변경부분>
    // 지정된 상점 레벨의 데이터만 반환한다.
    //
    // 현재는 정확히 일치하는 레벨만 사용한다.
    // 존재하지 않는 레벨이면 null을 반환한다.
    //
    // 진행도에 따른 자동 레벨 계산은
    // 여기에서 처리하지 않는다.
    public ShopInventoryLevelData GetLevelData(int shopLevel)
    {
        if (levels == null)
        {
            return null;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            ShopInventoryLevelData levelData =
                levels[i];

            if (levelData == null)
            {
                continue;
            }

            if (levelData.shopLevel == shopLevel)
            {
                return levelData;
            }
        }

        return null;
    }
}


// ============================================================
// Shop Inventory Level Data
// ============================================================

// <변경부분>
// 특정 상점 레벨에서 등장할 수 있는 상품 후보 목록.
//
// 예:
// Shop Level 1 → 초급 아이템 후보
// Shop Level 2 → 중급 아이템 후보
// Shop Level 3 → 고급 아이템 후보
//
// 실제 레벨은 WorldMap Shop Node에서 직접 지정한다.
[Serializable]
public class ShopInventoryLevelData
{
    [Header("Shop Level")]

    [Min(1)]
    public int shopLevel = 1;


    [Header("Item Candidates")]

    // <변경부분>
    // 해당 레벨에서 판매 가능한 상품 후보.
    //
    // 이 목록의 순서가 실제 진열 순서를 결정하지 않는다.
    // 추후 가중치 기반 랜덤 추첨으로 상품을 결정한다.
    public List<ShopInventoryItemEntry> itemCandidates =
        new List<ShopInventoryItemEntry>();
}


// ============================================================
// Shop Inventory Item Entry
// ============================================================

// <변경부분>
// 상점에서 판매할 아이템 후보 하나.
//
// BattleItemData 원본은 변경하지 않으며
// 판매 가격과 등장 확률 가중치를 별도로 관리한다.
//
// 동일한 BattleItemData라도 상점 데이터에 따라
// 다른 가격을 설정할 수 있다.
[Serializable]
public class ShopInventoryItemEntry
{
    [Header("Item")]

    // 실제 판매할 전투 아이템 데이터.
    public BattleItemData itemData;


    [Header("Purchase Price")]

    // <변경부분>
    // 상점에서 판매할 Gold 가격.
    //
    // 0 Gold도 허용한다.
    // 음수는 허용하지 않는다.
    [Min(0)]
    public int price = 0;


    [Header("Appearance Weight")]

    // <변경부분>
    // 해당 상품이 랜덤 추첨에서 선택될 상대 가중치.
    //
    // 0 = 추첨에서 제외
    // 1 이상 = 추첨 후보
    //
    // 예:
    // A = 50
    // B = 30
    // C = 20
    //
    // 세 상품이 모두 추첨 가능한 상태라면
    // 각 상품의 최초 선택 확률은
    // 50% / 30% / 20%이다.
    //
    // 실제 추첨 로직은 이후 ShopController에서 구현한다.
    [Min(0)]
    public int weight = 100;
}

