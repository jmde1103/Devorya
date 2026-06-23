using System.Collections.Generic;
using UnityEngine;

// <변경부분> 팝업 하단에 붙였다 뗄 수 있는 추가 설명 블록 데이터
[System.Serializable]
public class TooltipSectionData
{
    // 추가 설명 블록 제목
    public string sectionTitle;

    [TextArea(2, 5)]
    // 추가 설명 블록 내용
    public string sectionDescription;

    // 추가 설명 블록 배경색
    public Color sectionColor = Color.white;
}

// <변경부분> 전투 UI에서 아이콘을 꾹 눌렀을 때 표시할 설명 팝업 데이터
[CreateAssetMenu(fileName = "TooltipData_New", menuName = "Devorya/UI/Tooltip Data")]
public class TooltipData : ScriptableObject
{
    [Header("Header")]
    // 팝업 상단에 표시할 이름
    public string title;

    // 일반스킬 / 고유스킬 / 아이템 / 유물 / 상태이상 같은 분류
    public string category;

    [Header("Description")]
    [TextArea(2, 5)]
    // 기본 설명 문장
    public string mainDescription;

    [Header("Sections")]
    // 하단에 동적으로 붙일 추가 설명 블록 목록
    public List<TooltipSectionData> sections = new List<TooltipSectionData>();
}
