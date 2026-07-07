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
    // <변경부분> 보상 후보로 등장할 수 있는 전투 아이템 목록
    public BattleItemData[] itemCandidates;

    // <변경부분> 이번 전투 보상에서 뽑을 아이템 후보 수
    public int itemRewardCount = 1;

    [Header("Relic Rewards")]
    // <변경부분> 보상 후보로 등장할 수 있는 유물 목록
    public BattleRelicData[] relicCandidates;

    // <변경부분> 이번 전투 보상에서 뽑을 유물 후보 수
    public int relicRewardCount = 1;

    [Header("Option")]
    // <변경부분> 아이템 보상을 포함할지 여부
    public bool includeItemRewards = true;

    // <변경부분> 유물 보상을 포함할지 여부
    public bool includeRelicRewards = true;

    // <변경부분> 전투 종료 시 실제 보상 후보 목록을 생성하는 함수
    public List<BattleRewardOptionRuntimeData> CreateRewardOptions()
    {
        List<BattleRewardOptionRuntimeData> rewardOptions = new List<BattleRewardOptionRuntimeData>();

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

    // <변경부분> 아이템 후보 중 랜덤으로 보상 선택지를 추가하는 함수
    private void AddRandomItemRewards(List<BattleRewardOptionRuntimeData> rewardOptions)
    {
        if (itemCandidates == null || itemCandidates.Length == 0)
        {
            return;
        }

        List<BattleItemData> candidateList = new List<BattleItemData>();

        for (int i = 0; i < itemCandidates.Length; i++)
        {
            if (itemCandidates[i] != null)
            {
                candidateList.Add(itemCandidates[i]);
            }
        }

        int count = Mathf.Min(itemRewardCount, candidateList.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, candidateList.Count);
            BattleItemData selectedItem = candidateList[randomIndex];

            candidateList.RemoveAt(randomIndex);

            rewardOptions.Add(BattleRewardOptionRuntimeData.CreateItemReward(selectedItem));
        }
    }

    // <변경부분> 유물 후보 중 랜덤으로 보상 선택지를 추가하는 함수
    private void AddRandomRelicRewards(List<BattleRewardOptionRuntimeData> rewardOptions)
    {
        if (relicCandidates == null || relicCandidates.Length == 0)
        {
            return;
        }

        List<BattleRelicData> candidateList = new List<BattleRelicData>();

        for (int i = 0; i < relicCandidates.Length; i++)
        {
            if (relicCandidates[i] != null)
            {
                candidateList.Add(relicCandidates[i]);
            }
        }

        int count = Mathf.Min(relicRewardCount, candidateList.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, candidateList.Count);
            BattleRelicData selectedRelic = candidateList[randomIndex];

            candidateList.RemoveAt(randomIndex);

            rewardOptions.Add(BattleRewardOptionRuntimeData.CreateRelicReward(selectedRelic));
        }
    }
}

// <변경부분> 전투 종료 후 실제로 생성된 보상 후보 1개
[System.Serializable]
public class BattleRewardOptionRuntimeData
{
    public BattleRewardOptionType rewardType = BattleRewardOptionType.None;

    public BattleItemData itemData;
    public BattleRelicData relicData;

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

        return "알 수 없는 보상";
    }
}

// <변경부분> 보상 후보 종류
public enum BattleRewardOptionType
{
    None,
    Item,
    Relic
}
