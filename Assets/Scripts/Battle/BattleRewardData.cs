using System.Collections.Generic;
using UnityEngine;

// <변경부분> 전투 승리 후 생성할 보상 후보 데이터를 관리하는 ScriptableObject
[CreateAssetMenu(fileName = "BattleRewardData", menuName = "Devorya/Battle/Battle Reward Data")]
public class BattleRewardData : ScriptableObject
{
    [Header("Reward Info")]
    // <변경부분> 보상 데이터 이름
    public string rewardName;

    [TextArea]
    // <변경부분> 인스펙터 확인용 설명
    public string description;

    [Header("Item Rewards")]
    // <변경부분> 보상 후보로 등장할 수 있는 전투 아이템 드롭 테이블
    public BattleItemRewardDropEntry[] itemDropTable;

    // <변경부분> 이번 전투 보상에서 최대로 뽑을 아이템 후보 수
    public int itemRewardCount = 1;

    [Header("Relic Rewards")]
    // <변경부분> 보상 후보로 등장할 수 있는 유물 드롭 테이블
    public BattleRelicRewardDropEntry[] relicDropTable;

    // <변경부분> 이번 전투 보상에서 최대로 뽑을 유물 후보 수
    public int relicRewardCount = 1;

    [Header("Gold Reward")]
    // <변경부분> 금화 보상을 포함할지 여부
    public bool includeGoldReward = true;

    // <변경부분> 금화 최소 획득량
    public int minGoldAmount = 10;

    // <변경부분> 금화 최대 획득량
    public int maxGoldAmount = 30;

    [Header("Option")]
    // <변경부분> 아이템 보상을 포함할지 여부
    public bool includeItemRewards = true;

    // <변경부분> 유물 보상을 포함할지 여부
    public bool includeRelicRewards = true;

    // <변경부분> 전투 종료 시 실제 보상 후보 목록을 생성하는 함수
    public List<BattleRewardOptionRuntimeData> CreateRewardOptions()
    {
        List<BattleRewardOptionRuntimeData> rewardOptions = new List<BattleRewardOptionRuntimeData>();

        if (includeGoldReward)
        {
            AddGoldReward(rewardOptions);
        }

        if (includeItemRewards)
        {
            AddRandomItemRewards(rewardOptions);
        }

        if (includeRelicRewards)
        {
            AddRandomRelicRewards(rewardOptions);
        }

        return rewardOptions;
    }

    // <변경부분> 금화 보상 범위 안에서 랜덤 금화 보상을 생성하는 함수
    private void AddGoldReward(List<BattleRewardOptionRuntimeData> rewardOptions)
    {
        if (rewardOptions == null)
        {
            return;
        }

        int minAmount = Mathf.Max(0, minGoldAmount);
        int maxAmount = Mathf.Max(minAmount, maxGoldAmount);

        int goldAmount = Random.Range(minAmount, maxAmount + 1);

        if (goldAmount <= 0)
        {
            return;
        }

        rewardOptions.Add(BattleRewardOptionRuntimeData.CreateGoldReward(goldAmount));
    }

    // <변경부분> 아이템 드롭 테이블에서 개별 확률을 통과한 아이템 중 랜덤으로 보상 후보를 추가하는 함수
    private void AddRandomItemRewards(List<BattleRewardOptionRuntimeData> rewardOptions)
    {
        if (rewardOptions == null)
        {
            return;
        }

        if (itemDropTable == null || itemDropTable.Length == 0)
        {
            return;
        }

        List<BattleItemData> passedCandidateList = new List<BattleItemData>();

        // <변경부분> 아이템별 드롭 확률을 각각 독립적으로 판정
        for (int i = 0; i < itemDropTable.Length; i++)
        {
            BattleItemRewardDropEntry dropEntry = itemDropTable[i];

            if (dropEntry == null)
            {
                continue;
            }

            if (dropEntry.itemData == null)
            {
                continue;
            }

            if (dropEntry.RollDrop())
            {
                passedCandidateList.Add(dropEntry.itemData);
            }
        }

        int count = Mathf.Min(itemRewardCount, passedCandidateList.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, passedCandidateList.Count);
            BattleItemData selectedItem = passedCandidateList[randomIndex];

            passedCandidateList.RemoveAt(randomIndex);

            rewardOptions.Add(BattleRewardOptionRuntimeData.CreateItemReward(selectedItem));
        }
    }

    // <변경부분> 유물 드롭 테이블에서 개별 확률을 통과한 유물 중 랜덤으로 보상 후보를 추가하는 함수
    private void AddRandomRelicRewards(List<BattleRewardOptionRuntimeData> rewardOptions)
    {
        if (rewardOptions == null)
        {
            return;
        }

        if (relicDropTable == null || relicDropTable.Length == 0)
        {
            return;
        }

        List<BattleRelicData> passedCandidateList = new List<BattleRelicData>();

        // <변경부분> 유물별 드롭 확률을 각각 독립적으로 판정
        for (int i = 0; i < relicDropTable.Length; i++)
        {
            BattleRelicRewardDropEntry dropEntry = relicDropTable[i];

            if (dropEntry == null)
            {
                continue;
            }

            if (dropEntry.relicData == null)
            {
                continue;
            }

            if (dropEntry.RollDrop())
            {
                passedCandidateList.Add(dropEntry.relicData);
            }
        }

        int count = Mathf.Min(relicRewardCount, passedCandidateList.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, passedCandidateList.Count);
            BattleRelicData selectedRelic = passedCandidateList[randomIndex];

            passedCandidateList.RemoveAt(randomIndex);

            rewardOptions.Add(BattleRewardOptionRuntimeData.CreateRelicReward(selectedRelic));
        }
    }
}

// <변경부분> 아이템 보상 드롭 테이블 1칸
[System.Serializable]
public class BattleItemRewardDropEntry
{
    // <변경부분> 드롭 후보 아이템 데이터
    public BattleItemData itemData;

    // <변경부분> 이 아이템이 보상 후보에 포함될 확률
    [Range(0, 100)]
    public int dropChancePercent = 100;

    // <변경부분> 아이템 드롭 확률 판정
    public bool RollDrop()
    {
        int chance = Mathf.Clamp(dropChancePercent, 0, 100);
        int roll = Random.Range(0, 100);

        return roll < chance;
    }
}

// <변경부분> 유물 보상 드롭 테이블 1칸
[System.Serializable]
public class BattleRelicRewardDropEntry
{
    // <변경부분> 드롭 후보 유물 데이터
    public BattleRelicData relicData;

    // <변경부분> 이 유물이 보상 후보에 포함될 확률
    [Range(0, 100)]
    public int dropChancePercent = 100;

    // <변경부분> 유물 드롭 확률 판정
    public bool RollDrop()
    {
        int chance = Mathf.Clamp(dropChancePercent, 0, 100);
        int roll = Random.Range(0, 100);

        return roll < chance;
    }
}

// <변경부분> 전투 종료 후 실제로 생성된 보상 후보 1개
[System.Serializable]
public class BattleRewardOptionRuntimeData
{
    public BattleRewardOptionType rewardType = BattleRewardOptionType.None;

    public BattleItemData itemData;
    public BattleRelicData relicData;

    // <변경부분> 금화 보상량
    public int goldAmount;

    public static BattleRewardOptionRuntimeData CreateItemReward(BattleItemData itemData)
    {
        BattleRewardOptionRuntimeData rewardOption = new BattleRewardOptionRuntimeData();

        rewardOption.rewardType = BattleRewardOptionType.Item;
        rewardOption.itemData = itemData;

        return rewardOption;
    }

    public static BattleRewardOptionRuntimeData CreateRelicReward(BattleRelicData relicData)
    {
        BattleRewardOptionRuntimeData rewardOption = new BattleRewardOptionRuntimeData();

        rewardOption.rewardType = BattleRewardOptionType.Relic;
        rewardOption.relicData = relicData;

        return rewardOption;
    }

    // <변경부분> 금화 보상 RuntimeData 생성
    public static BattleRewardOptionRuntimeData CreateGoldReward(int goldAmount)
    {
        BattleRewardOptionRuntimeData rewardOption = new BattleRewardOptionRuntimeData();

        rewardOption.rewardType = BattleRewardOptionType.Gold;
        rewardOption.goldAmount = Mathf.Max(0, goldAmount);

        return rewardOption;
    }

    public string GetDebugName()
    {
        if (rewardType == BattleRewardOptionType.Item && itemData != null)
        {
            return $"아이템 보상: {itemData.itemName}";
        }

        if (rewardType == BattleRewardOptionType.Relic && relicData != null)
        {
            return $"유물 보상: {relicData.relicName}";
        }

        if (rewardType == BattleRewardOptionType.Gold)
        {
            return $"금화 보상: {goldAmount}";
        }

        return "알 수 없는 보상";
    }
}

// <변경부분> 보상 후보 종류
public enum BattleRewardOptionType
{
    None,
    Item,
    Relic,
    Gold
}
