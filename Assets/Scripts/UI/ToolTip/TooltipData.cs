using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

// Tooltip Section은 현재 StatusEffect 데이터 기반 Section만 사용한다.
//
// 기존 Asset / Prefab에서 StatusEffect가 정수 1로 직렬화되어 있으므로
// 값 1은 반드시 유지한다.
//
// 과거 Text = 0 타입은 Localization 구조와 분리되는
// 직접 문자열 입력 방식이므로 완전히 폐기한다.
public enum TooltipSectionType
{
    StatusEffect = 1
}

[System.Serializable]
public class TooltipSectionData
{
    // Runtime에서 TooltipPopupUI의 Section prefab routing에 사용한다.
    //
    // Tooltip Section 타입은 현재 StatusEffect 하나뿐이므로
    // Asset authoring 데이터로 직렬화하지 않는다.
    [System.NonSerialized]
    public TooltipSectionType sectionType =
        TooltipSectionType.StatusEffect;

    // Tooltip에 표시할 실제 상태효과 데이터.
    //
    // 이름 / 설명 / 아이콘은 여기서 가져오며
    // 각각의 Tooltip Asset에 문자열을 복사해서 저장하지 않는다.
    public StatusEffectData statusEffectData;

    // Section 배경색은 Localization과 관계없는
    // 순수 UI 표현값이므로 Asset에 저장한다.
    public Color sectionColor =
        Color.white;

    // 아래 값들은 Asset authoring 데이터가 아니다.
    //
    // Runtime에서 StatusEffectData와 TooltipLocalization을
    // Resolve한 결과만 저장한다.
    //
    // NonSerialized이므로 Inspector 또는 Asset에
    // 한국어 고정 문자열이 저장되는 경로가 생기지 않는다.
    [System.NonSerialized]
    public string sectionTitle;

    [System.NonSerialized]
    public string sectionCategory;

    [System.NonSerialized]
    public string sectionDescription;

    [System.NonSerialized]
    public Sprite sectionIcon;

    // 실제 Tooltip 표시용 데이터를 생성한다.
    //
    // overrideStatusEffectData가 있으면 그것을 우선 사용한다.
    // BattleItem처럼 실제 효과 데이터가 이미 별도로 존재할 때
    // 동일한 데이터를 Tooltip에서도 SSOT로 사용하기 위한 구조다.
    public TooltipSectionData CreateResolvedCopy(
        StatusEffectData overrideStatusEffectData = null)
    {
        StatusEffectData resolvedStatusEffectData =
            overrideStatusEffectData != null
                ? overrideStatusEffectData
                : statusEffectData;

        // 실제 데이터가 없는 Section은 표시하지 않는다.
        //
        // 과거 Text Section처럼 Localization되지 않는
        // 레거시 데이터가 Runtime으로 흘러가는 것도 여기서 차단된다.
        if (resolvedStatusEffectData == null)
        {
            return null;
        }

        return new TooltipSectionData
        {
            sectionType =
                TooltipSectionType.StatusEffect,

            statusEffectData =
                resolvedStatusEffectData,

            sectionTitle =
                resolvedStatusEffectData
                    .GetLocalizedEffectName(),

            sectionCategory =
                TooltipLocalization
                    .GetStatusEffectCategory(),

            sectionDescription =
                resolvedStatusEffectData
                    .GetLocalizedDescription(),

            sectionIcon =
                resolvedStatusEffectData.iconSprite,

            sectionColor =
                sectionColor
        };
    }
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

    private static List<TooltipSectionData>
    ResolveTooltipSections(
        List<TooltipSectionData> sourceSections)
    {
        if (sourceSections == null ||
            sourceSections.Count == 0)
        {
            return null;
        }

        List<TooltipSectionData> resolvedSections =
            new List<TooltipSectionData>(
                sourceSections.Count
            );

        for (int i = 0;
             i < sourceSections.Count;
             i++)
        {
            TooltipSectionData sourceSection =
                sourceSections[i];

            if (sourceSection == null)
            {
                continue;
            }

            TooltipSectionData resolvedSection =
                sourceSection.CreateResolvedCopy();

            // 실제 Localization source가 없는
            // 레거시 Section은 표시하지 않는다.
            if (resolvedSection == null)
            {
                continue;
            }

            resolvedSections.Add(
                resolvedSection
            );
        }

        return resolvedSections.Count > 0
            ? resolvedSections
            : null;
    }

    private static List<TooltipSectionData>
     ResolveBattleItemTooltipSections(
         BattleItemData itemData)
    {
        if (itemData == null)
        {
            return null;
        }

        List<TooltipSectionData> resolvedSections =
            new List<TooltipSectionData>();

        // ApplyStatusEffectToSelectedPiece 아이템은
        // 실제 게임 효과에 사용하는 applyStatusEffectData를
        // 기본 Tooltip Section의 SSOT로 사용한다.
        //
        // 다른 BattleItemType에 오래된 applyStatusEffectData 값이
        // 남아 있더라도 자동 Section을 만들지 않도록
        // Item Type까지 함께 검사한다.
        if (itemData.itemType ==
                BattleItemType.ApplyStatusEffectToSelectedPiece &&
            itemData.applyStatusEffectData != null)
        {
            TooltipSectionData appliedStatusEffectSection =
                new TooltipSectionData
                {
                    sectionColor =
                        itemData
                            .applyStatusEffectTooltipSectionColor
                }
                .CreateResolvedCopy(
                    itemData.applyStatusEffectData
                );

            if (appliedStatusEffectSection != null)
            {
                resolvedSections.Add(
                    appliedStatusEffectSection
                );
            }
        }

        // BattleItemData.tooltipSections는 이제
        // 실제 적용 효과와 별개의 "추가 Tooltip Section" 목록이다.
        //
        // 다른 Skill과 동일하게 각 StatusEffectData를
        // 자유롭게 연결할 수 있다.
        if (itemData.tooltipSections != null)
        {
            for (int i = 0;
                 i < itemData.tooltipSections.Count;
                 i++)
            {
                TooltipSectionData sourceSection =
                    itemData.tooltipSections[i];

                if (sourceSection == null ||
                    sourceSection.statusEffectData == null)
                {
                    continue;
                }

                // 이전 Asset에 실제 적용 상태효과를
                // Tooltip Section에도 중복 연결해 두었던 데이터가
                // 남아 있을 수 있다.
                //
                // 실제 효과와 동일한 StatusEffectData는
                // 위에서 이미 자동 생성되므로 중복 표시하지 않는다.
                if (itemData.itemType ==
                        BattleItemType.ApplyStatusEffectToSelectedPiece &&
                    itemData.applyStatusEffectData != null &&
                    sourceSection.statusEffectData ==
                        itemData.applyStatusEffectData)
                {
                    continue;
                }

                TooltipSectionData resolvedSection =
                    sourceSection.CreateResolvedCopy();

                if (resolvedSection == null)
                {
                    continue;
                }

                resolvedSections.Add(
                    resolvedSection
                );
            }
        }

        return resolvedSections.Count > 0
            ? resolvedSections
            : null;
    }


    public static TooltipViewData FromTooltipData(
      TooltipData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            // <변경부분>
            // TooltipData가 자기 콘텐츠 문자열의 Localization SSOT가 된다.
            //
            // 현재 Locale의 번역값을 우선 사용하고,
            // Localization이 아직 연결되지 않았거나 번역값이 비어 있으면
            // TooltipData에 기존부터 저장되어 있던 한국어 원문으로 fallback한다.
            title =
                data.GetLocalizedTitle(),

            category =
                data.GetLocalizedCategory(),

            mainDescription =
                data.GetLocalizedMainDescription(),

            icon =
                data.icon,

            // Tooltip Section은 기존과 동일하게
            // StatusEffectData 기반 공통 Resolver를 사용한다.
            sections =
                ResolveTooltipSections(
                    data.sections
                )
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
            // <변경부분> 현재 Locale 기준 일반스킬 이름을 사용한다.
            //
            // Localization이 연결되지 않은 기존 Data는
            // GeneralSkillData 내부에서 기존 skillName으로 fallback한다.
            title =
         data.GetLocalizedSkillName(),

            // <변경부분> Tooltip Category Localization은
            // 이후 공용 Tooltip 문자열 작업에서 별도로 처리한다.
            category =
TooltipLocalization
 .GetGeneralSkillCategory(),

            // 일반스킬 레벨 표시를 사용하지 않는다.
            levelText =
         string.Empty,

            // <변경부분> 현재 Locale 기준 일반스킬 Tooltip 설명을
            // 명시적으로 가져온다.
            //
            // GeneralSkillData 내부에서
            // 현재 Locale Tooltip → 현재 Locale Description →
            // 기존 한국어 Tooltip → 기존 한국어 Description 순으로 처리한다.
            mainDescription =
         data.GetLocalizedTooltipDescription(),

            icon =
         data.iconSprite,

            // <변경부분> 일반스킬 Tooltip에 연결된
            // StatusEffect Section은 StatusEffectData를 SSOT로 사용한다.
            //
            // 예:
            // Defense 일반스킬
            // → Defence StatusEffectData
            // → 현재 Locale의 이름/설명/아이콘 자동 사용
            sections =
         ResolveTooltipSections(
             data.tooltipSections
         )
        };
    }

    // <변경부분> 고유스킬 Tooltip을
    // 현재 선택된 Locale 기준 이름/설명으로 구성한다.
    public static TooltipViewData FromUniqueSkillData(
        UniqueSkillData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            // <변경부분> Localization이 없는 기존 Data는
            // UniqueSkillData 내부에서 기존 skillName으로 fallback한다.
            title =
                data.GetLocalizedSkillName(),

            // <변경부분> Tooltip Category Localization은
            // 이후 Tooltip 공용 문자열 작업에서 별도로 처리한다.
            category =
    TooltipLocalization
        .GetUniqueSkillCategory(),

            // <변경부분> 현재 Locale의 Description을 사용한다.
            mainDescription =
                data.GetLocalizedDescription(),

            icon =
                data.iconSprite,

            // <변경부분> 고유스킬에서도
            // 상태효과 Section은 동일한 SSOT 구조를 사용한다.
            sections =
                ResolveTooltipSections(
                    data.tooltipSections
                )
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
    TooltipLocalization
        .GetItemCategory(),

            mainDescription =
                data.GetLocalizedDescription(),

            icon =
                data.iconSprite,


            // <변경부분> 상태효과 부여 아이템은
            // Tooltip Section에 별도 StatusEffectData를 복사하지 않고
            // 실제 효과에 사용하는 applyStatusEffectData를 SSOT로 사용한다.
            sections =
        ResolveBattleItemTooltipSections(
            data
        )

        };
    }

    // <변경부분> 상태효과 Tooltip을
    // 현재 선택된 Locale 기준 이름/설명으로 구성한다.
    public static TooltipViewData FromStatusEffectData(
        StatusEffectData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            // <변경부분> 현재 Locale의 상태효과 이름.
            // Localization이 없는 기존 Data는 effectName으로 fallback한다.
            title =
                data.GetLocalizedEffectName(),

            // <변경부분> 공용 Tooltip Category Localization은
            // 이후 Tooltip Common 작업에서 별도로 처리한다.
            category =
    TooltipLocalization
        .GetStatusEffectCategory(),

            // <변경부분> 현재 Locale의 상태효과 설명.
            mainDescription =
                data.GetLocalizedDescription(),

            icon =
                data.iconSprite,

            sections =
                ResolveTooltipSections(
                    data.tooltipSections
                )
        };
    }

    public static TooltipViewData FromBattleRelicData(
    BattleRelicData data)
    {
        if (data == null)
        {
            return null;
        }

        return new TooltipViewData
        {
            // 현재 Locale 기준 유물 이름.
            //
            // Localization이 연결되지 않은 기존 Asset은
            // BattleRelicData 내부에서 relicName으로 fallback한다.
            title =
                data.GetLocalizedRelicName(),

            // 유물 Category는 Tooltip_Common의
            // 공용 Localization을 사용한다.
            category =
                TooltipLocalization
                    .GetRelicCategory(),

            // 현재 Locale 기준 유물 설명.
            //
            // Localization이 연결되지 않은 기존 Asset은
            // BattleRelicData 내부에서 description으로 fallback한다.
            mainDescription =
                data.GetLocalizedDescription(),

            icon =
                data.iconSprite,

            // 추가 Section은 기존 StatusEffectData SSOT 구조를 유지한다.
            sections =
                ResolveTooltipSections(
                    data.tooltipSections
                )
        };
    }

    // <변경부분> 기물 복구 보상에 표시할 PieceData 기반 Tooltip 생성
    //
    // 현재 Reward 1차 Localization에서는
    // Recovery Piece 자체의 Localization까지 함께 처리하지 않는다.
    //
    // 따라서 공용 TooltipViewData가 UniqueSkillDatabase나
    // Reward 전용 Localization Table을 직접 참조하지 않도록
    // Reward 작업 전의 단순 PieceData 기반 구조를 유지한다.
    //
    // Recovery Piece Tooltip Localization은
    // Reward 3차 작업에서 Piece 표시명의 실제 SSOT를 확인한 뒤 별도로 처리한다.
    public static TooltipViewData FromPieceData(
        PieceData data)
    {
        if (data == null)
        {
            return null;
        }

        // <변경부분> 별도 표시 이름이 없으므로 pieceId를 우선 사용한다.
        string displayName =
            string.IsNullOrEmpty(
                data.pieceId)
                ? data.pieceType.ToString()
                : data.pieceId;

        // <변경부분> PieceData에 별도 설명 필드가 없으므로
        // 기존 Recovery Tooltip 문구를 그대로 사용한다.
        string description =
            $"전투 종료 후 복구된 {data.pieceType} 기물입니다.";

        if (data.uniqueSkill !=
            UniqueSkillType.None)
        {
            description +=
                $"\n기본 고유스킬: {data.uniqueSkill}";
        }

        // <변경부분> 보상 아이콘은 상태 UI용 스프라이트를 우선 사용한다.
        Sprite displayIcon =
            data.playerStatusSprite != null
                ? data.playerStatusSprite
                : data.playerSprite;

        return new TooltipViewData
        {
            title =
        displayName,

            category =
        "기물 복구",

            mainDescription =
        description,

            icon =
        displayIcon,

            sections =
        new List<TooltipSectionData>()
        };
    }
}

// <변경부분> 별도 데이터가 없는 버튼/설명 전용 Tooltip 에셋
// 스킬/아이템/유물/상태효과처럼 이미 Data가 있는 대상에는 필수로 만들 필요 없음.
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

    [Header("Localization")]

    // <변경부분>
    // 현재 Locale 기준 Tooltip 제목 Localization 참조.
    //
    // 기존 title은 삭제하지 않으며
    // 한국어 authoring 원문 및 Localization 누락 시 fallback으로 유지한다.
    public LocalizedString localizedTitle =
new LocalizedString();

    // <변경부분>
    // 현재 Locale 기준 Tooltip 분류 Localization 참조.
    //
    // 기존 category는 삭제하지 않고
    // 한국어 authoring 원문 및 fallback으로 유지한다.
    public LocalizedString localizedCategory =
        new LocalizedString();

    // <변경부분>
    // 현재 Locale 기준 Tooltip 기본 설명 Localization 참조.
    //
    // 기존 mainDescription은 삭제하지 않고
    // 한국어 authoring 원문 및 fallback으로 유지한다.
    public LocalizedString localizedMainDescription =
        new LocalizedString();

    [Header("Sections")]
    // 하단에 동적으로 붙일 추가 설명 블록 목록
    public List<TooltipSectionData> sections = new List<TooltipSectionData>();

    // <변경부분>
    // 현재 선택된 Locale 기준 Tooltip 제목을 반환한다.
    //
    // Localization이 연결되지 않은 기존 TooltipData Asset도
    // 기존 title을 그대로 표시할 수 있도록 fallback한다.
    public string GetLocalizedTitle()
    {
        return GetLocalizedTextOrFallback(
            localizedTitle,
            title
        );
    }

    // <변경부분>
    // 현재 선택된 Locale 기준 Tooltip 분류를 반환한다.
    public string GetLocalizedCategory()
    {
        return GetLocalizedTextOrFallback(
            localizedCategory,
            category
        );
    }

    // <변경부분>
    // 현재 선택된 Locale 기준 Tooltip 기본 설명을 반환한다.
    public string GetLocalizedMainDescription()
    {
        return GetLocalizedTextOrFallback(
            localizedMainDescription,
            mainDescription
        );
    }

    // <변경부분>
    // LocalizedString이 정상 연결되어 있으면 현재 Locale 값을 사용하고,
    // 참조가 없거나 현재 Locale 문자열이 비어 있으면
    // 기존 한국어 raw 값을 안전한 fallback으로 사용한다.
    private string GetLocalizedTextOrFallback(
        LocalizedString localizedString,
        string fallbackText)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return fallbackText ?? string.Empty;
        }

        string localizedText =
            localizedString.GetLocalizedString();

        if (string.IsNullOrWhiteSpace(
                localizedText))
        {
            return fallbackText ?? string.Empty;
        }

        return localizedText;
    }

}