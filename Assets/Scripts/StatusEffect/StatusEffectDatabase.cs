using System.Collections.Generic;
using UnityEngine;

// <변경부분> 상태이상 데이터 목록을 관리하는 데이터베이스
[CreateAssetMenu(fileName = "\uc0c1\ud0dc\ud6a8\uacfc_\ub370\uc774\ud130\ubca0\uc774\uc2a4", menuName = "Devorya/\uc0c1\ud0dc\ud6a8\uacfc/\ub370\uc774\ud130\ubca0\uc774\uc2a4")]
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