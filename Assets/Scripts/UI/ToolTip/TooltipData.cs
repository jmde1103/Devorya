using System.Collections.Generic;
using UnityEngine;

// <변경부분> Tooltip 하단에 생성할 추가 설명 블록 종류
public enum TooltipSectionType
{
    // 제목 + 설명만 표시하는 기본 텍스트 블록
    Text,

    // 아이콘 + 이름 + 분류 + 설명을 표시하는 상태효과 블록
    StatusEffect
}

// <변경부분> 팝업 하단에 붙였다 뗄 수 있는 추가 설명 블록 데이터
[System.Serializable]
public class TooltipSectionData
{
    // <변경부분> 이 설명 블록이 사용할 UI 프리팹 종류
    public TooltipSectionType sectionType = TooltipSectionType.Text;

    // 추가 설명 블록 제목
    public string sectionTitle;

    // <변경부분> 상태효과 / 키워드 / 태그 같은 보조 분류 텍스트
    public string sectionCategory;

    [TextArea(2, 5)]
    // 추가 설명 블록 내용
    public string sectionDescription;

    // <변경부분> 상태효과나 키워드 설명에 표시할 아이콘
    public Sprite sectionIcon;

    // 추가 설명 블록 배경색
    public Color sectionColor = Color.white;
}

// <변경부분> 실제 TooltipPopupUI가 표시할 최종 가공 데이터
// 기존 SkillData / ItemData / RelicData / StatusEffectData의 이름, 설명, 아이콘을 재사용하기 위해 만든 런타임 데이터
public class TooltipViewData
{
    // 팝업 상단 제목
    public string title;

    // 팝업 상단 분류
    public string category;

    // <변경부분> 레벨 또는 단계 정보가 필요한 다른 Tooltip에서 사용할 보조 텍스트
    // 일반스킬은 레벨 시스템이 제거되어 빈 문자열을 사용한다.
    public string levelText;

    // 기본 설명
    public string mainDescription;

    // 팝업 대표 아이콘
    public Sprite icon;

    // 하단 추가 설명 블록 목록
    public List<TooltipSectionData> sections;

    // <변경부분> 별도 TooltipData 에셋을 TooltipViewData로 변환
    public static TooltipViewData FromTooltipData(TooltipData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            title = data.title,
            category = data.category,
            mainDescription = data.mainDescription,
            icon = data.icon,
            sections = data.sections
        };
    }

    // <변경부분> 일반스킬 데이터의 이름, 아이콘,
    // 고정 확률이 반영된 설명을 Tooltip 기본 정보로 사용한다.
    //
    // 일반스킬 레벨 시스템이 제거되었으므로
    // 레벨 텍스트와 레벨 인수는 사용하지 않는다.
    public static TooltipViewData FromGeneralSkillData(
        GeneralSkillData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            title =
                data.skillName,

            category =
                "일반스킬",

            // 일반스킬 레벨 표시를 사용하지 않는다.
            levelText =
                string.Empty,

            mainDescription =
                data.GetTooltipDescription(),

            icon =
                data.iconSprite,

            sections =
                data.tooltipSections
        };
    }

    // <변경부분> 고유스킬 데이터의 기존 이름/설명/아이콘을 Tooltip 기본 정보로 사용
    public static TooltipViewData FromUniqueSkillData(UniqueSkillData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            title = data.skillName,
            category = "고유스킬",
            mainDescription = data.description,
            icon = data.iconSprite,
            sections = data.tooltipSections
        };
    }

    // <변경부분> 아이템 데이터에서 Tooltip 표시 데이터를 생성한다.
    //
    // 이름과 기본 설명은 현재 Locale에 맞는 Localization 값을 사용하며,
    // Localization이 연결되지 않은 기존 아이템은
    // BattleItemData의 기존 한국어 원문으로 자동 fallback한다.
    public static TooltipViewData FromBattleItemData(
        BattleItemData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            title =
                data.GetLocalizedItemName(),

            // <변경부분> 공용 Category 문자열의 Localization은
            // 이후 Tooltip 공통 텍스트 작업에서 별도로 처리한다.
            category =
                "아이템",

            mainDescription =
                data.GetLocalizedDescription(),

            icon =
                data.iconSprite,

            sections =
                data.tooltipSections
        };
    }

    // <변경부분> 상태효과 데이터의 기존 이름/설명/아이콘을 Tooltip 기본 정보로 사용
    public static TooltipViewData FromStatusEffectData(StatusEffectData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            title = data.effectName,
            category = "상태효과",
            mainDescription = data.description,
            icon = data.iconSprite,
            sections = data.tooltipSections
        };
    }

    // <변경부분> 유물 데이터의 기존 이름/설명/아이콘을 Tooltip 기본 정보로 사용
    public static TooltipViewData FromBattleRelicData(BattleRelicData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            title = data.relicName,
            category = "유물",
            mainDescription = data.description,
            icon = data.iconSprite,
            sections = data.tooltipSections
        };
    }

    // <변경부분> 기물 복구 보상에 표시할 PieceData 기반 Tooltip 생성
    public static TooltipViewData FromPieceData(PieceData data)
    {
        if (data == null)
        {
            return null;
        }

        // <변경부분> 별도 표시 이름이 없으므로 pieceId를 우선 사용
        string displayName = string.IsNullOrEmpty(data.pieceId)
            ? data.pieceType.ToString()
            : data.pieceId;

        // <변경부분> PieceData에 별도 설명 필드가 없으므로
        // 복구 기물이라는 기본 설명과 고유스킬 정보를 조합
        string description =
            $"전투 종료 후 복구된 {data.pieceType} 기물입니다.";

        if (data.uniqueSkill != UniqueSkillType.None)
        {
            description +=
                $"\n기본 고유스킬: {data.uniqueSkill}";
        }

        // <변경부분> 보상 아이콘은 상태 UI용 스프라이트를 우선 사용
        Sprite displayIcon = data.playerStatusSprite != null
            ? data.playerStatusSprite
            : data.playerSprite;

        return new TooltipViewData
        {
            title = displayName,
            category = "기물 복구",
            mainDescription = description,
            icon = displayIcon,
            sections = new List<TooltipSectionData>()
        };
    }
}

// <변경부분> 별도 데이터가 없는 버튼/설명 전용 Tooltip 에셋
// 스킬/아이템/유물/상태효과처럼 이미 Data가 있는 대상에는 필수로 만들 필요 없다.
[CreateAssetMenu(fileName = "TooltipData_New", menuName = "Devorya/UI/Tooltip Data")]
public class TooltipData : ScriptableObject
{
    [Header("Header")]
    // 팝업 상단에 표시할 이름
    public string title;

    // 버튼 / 전투 설명 / 기물 정보 같은 분류
    public string category;

    // <변경부분> 별도 TooltipData를 쓸 때 표시할 대표 아이콘
    public Sprite icon;

    [Header("Description")]
    [TextArea(2, 5)]
    // 기본 설명 문장
    public string mainDescription;

    [Header("Sections")]
    // 하단에 동적으로 붙일 추가 설명 블록 목록
    public List<TooltipSectionData> sections = new List<TooltipSectionData>();
}