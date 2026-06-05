using UnityEngine;

// <변경부분> 고유 스킬 종류와 아이콘을 연결하는 데이터
[System.Serializable]
public class UniqueSkillIconData
{
    // 고유 스킬 종류
    public UniqueSkillType skillType;

    // 해당 고유 스킬에 표시할 아이콘
    public Sprite iconSprite;
}
