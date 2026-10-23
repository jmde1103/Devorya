using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "유물보상_목록", menuName = "Devorya/보상/유물 보상 목록")]
public sealed class RelicRewardListData : ScriptableObject
{
    [InspectorName("보상 이름")] public string rewardName;
    [TextArea, InspectorName("설명")] public string description;
    // Same independent percentage roll and entry type as BattleRewardData's item/relic tables.
    [InspectorName("유물 드롭 목록")] public BattleRelicRewardDropEntry[] relicDropTable;
    [Range(1, 3), InspectorName("최대 선택지 수")] public int maxChoices = 3;
    public List<BattleRelicData> CreateChoices(RunStateManager run)
    {
        var choices = new List<BattleRelicData>();
        var seen = new HashSet<BattleRelicType>();
        if (relicDropTable == null || (run != null && run.GetBattleRelicsCopy().Count >= 4)) return choices;
        foreach (var entry in relicDropTable)
        {
            if (entry == null || entry.relicData == null || entry.relicData.relicType == BattleRelicType.None ||
                (run != null && run.HasBattleRelic(entry.relicData.relicType)) || !seen.Add(entry.relicData.relicType)) continue;
            if (entry.RollDrop()) choices.Add(entry.relicData);
        }
        for (int i = choices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var item = choices[i]; choices[i] = choices[j]; choices[j] = item;
        }
        int limit = Mathf.Clamp(maxChoices, 1, 3);
        if (choices.Count > limit) choices.RemoveRange(limit, choices.Count - limit);
        return choices;
    }
}