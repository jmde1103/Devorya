using UnityEngine;

// <변경부분> 일반스킬 타입에 맞는 GeneralSkillData를 찾아주는 데이터베이스
[CreateAssetMenu(fileName = "\ud2b9\uc131_\ub370\uc774\ud130\ubca0\uc774\uc2a4", menuName = "Devorya/\ud2b9\uc131/\ub370\uc774\ud130\ubca0\uc774\uc2a4")]
public class GeneralSkillDatabase : ScriptableObject
{
    // 일반스킬 데이터 목록
    [SerializeField] private GeneralSkillData[] generalSkillDatas;

    // <변경부분> 일반스킬 타입으로 데이터 찾기
    public GeneralSkillData GetData(GeneralSkillType skillType)
    {
        if (skillType == GeneralSkillType.None)
        {
            return null;
        }

        if (generalSkillDatas == null)
        {
            return null;
        }

        foreach (GeneralSkillData skillData in generalSkillDatas)
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

        return null;
    }
}
