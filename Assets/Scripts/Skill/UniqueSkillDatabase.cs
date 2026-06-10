using UnityEngine;

// <변경부분> 고유스킬 타입에 맞는 UniqueSkillData를 찾아주는 데이터베이스
[CreateAssetMenu(fileName = "UniqueSkillDatabase", menuName = "Devorya/Skill/Unique Skill Database")]
public class UniqueSkillDatabase : ScriptableObject
{
    // 고유스킬 데이터 목록
    [SerializeField] private UniqueSkillData[] uniqueSkillDatas;

    // <변경부분> 고유스킬 타입으로 데이터 찾기
    public UniqueSkillData GetData(UniqueSkillType skillType)
    {
        // None은 데이터 없음 처리
        if (skillType == UniqueSkillType.None)
        {
            return null;
        }

        // 등록된 데이터 목록에서 같은 타입 찾기
        foreach (UniqueSkillData skillData in uniqueSkillDatas)
        {
            if (skillData == null)
            {
                continue;
            }

            if (skillData.skillType == skillType)
            {
                return skillData;
            }
        }

        // 못 찾으면 null 반환
        return null;
    }
}
