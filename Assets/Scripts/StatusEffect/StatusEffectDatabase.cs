using System.Collections.Generic;
using UnityEngine;

// <변경부분> 상태이상 데이터 목록을 관리하는 데이터베이스
[CreateAssetMenu(fileName = "StatusEffectDatabase", menuName = "Devorya/Status Effect Database")]
public class StatusEffectDatabase : ScriptableObject
{
    // 등록된 상태이상 데이터 목록
    [SerializeField] private List<StatusEffectData> statusEffectDataList = new List<StatusEffectData>();

    // <변경부분> 상태이상 타입으로 데이터 찾기
    public StatusEffectData GetData(StatusEffectType effectType)
    {
        for (int i = 0; i < statusEffectDataList.Count; i++)
        {
            StatusEffectData data = statusEffectDataList[i];

            if (data == null)
            {
                continue;
            }

            if (data.effectType == effectType)
            {
                return data;
            }
        }

        return null;
    }
}