using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Audio.ProcessorInstance;

// <변경부분> 선택한 필드 기물의 현재 정보를 그대로 UI에 표시하는 컨트롤러
public class PieceStatusUIController : MonoBehaviour
{
    [Header("Piece Image")]
    [SerializeField] private Image pieceImage;

    [Header("Piece Type Icon")]
    [SerializeField] private Image pieceTypeIconImage;

    [Header("General Skill Slots")]
    [SerializeField] private TMP_Text[] generalSkillTexts;

    // <변경부분> 일반스킬 슬롯에 표시할 아이콘 이미지 배열
    [SerializeField] private Image[] generalSkillIconImages;

    // <변경부분> 일반스킬 슬롯을 꾹 눌렀을 때 설명을 표시할 TooltipTrigger 배열
    [SerializeField] private TooltipTrigger[] generalSkillTooltipTriggers;

    // <변경부분> 일반스킬 아이콘/이름/설명을 찾기 위한 데이터베이스
    [SerializeField] private GeneralSkillDatabase generalSkillDatabase;

    [Header("Status Effect Slots")]
    // <변경부분> 상태이상 아이콘/이름/설명을 찾기 위한 데이터베이스
    [SerializeField] private StatusEffectDatabase statusEffectDatabase;

    // <변경부분> 스테이터스 창에 표시할 상태이상 슬롯 배열
    [SerializeField] private StatusEffectSlotUI[] statusEffectSlots;

    [Header("Root")]
    [SerializeField] private GameObject statusRoot;

    private void Start()
    {

    }

    // <변경부분> 선택한 기물 정보를 필드 기물에서 직접 받아 UI에 표시하는 함수
    public void Refresh(Piece selectedPiece)
    {
        // 선택한 기물이 없으면 UI를 숨김
        if (selectedPiece == null)
        {
            Clear();
            return;
        }

        // <변경부분> 선택한 기물이 있으면 스테이터스 창 표시
        if (statusRoot != null)
        {
            statusRoot.SetActive(true);
        }

        // 선택한 필드 기물의 현재 외형 스프라이트를 그대로 표시
        SetPieceImageFromSelectedPiece(selectedPiece);

        // 선택한 필드 기물의 현재 타입 아이콘 스프라이트를 그대로 표시
        SetPieceTypeIconFromSelectedPiece(selectedPiece);

        // <변경부분> 기물이 보유한 일반스킬 목록을 UI 슬롯에 표시
        SetGeneralSkillSlots(selectedPiece.GetGeneralSkills());

        // <변경부분> 기물이 보유한 상태이상 목록을 UI 슬롯에 표시
        SetStatusEffectSlots(selectedPiece);
    }

    // <변경부분> 선택한 필드 기물의 현재 SpriteRenderer 이미지를 UI에 복사하는 함수
    private void SetPieceImageFromSelectedPiece(Piece selectedPiece)
    {
        if (pieceImage == null)
        {
            return;
        }

        // <변경부분> 필드 이미지가 아니라 PieceManager가 넣어준 스테이터스 UI용 이미지를 가져옴
        Sprite statusSprite = selectedPiece.GetStatusUISprite();

        if (statusSprite == null)
        {
            pieceImage.sprite = null;
            pieceImage.enabled = false;
            return;
        }

        // <변경부분> 필드 스프라이트가 아니라 UI용 앞면 스프라이트를 표시
        pieceImage.sprite = statusSprite;
        pieceImage.enabled = true;
        pieceImage.preserveAspect = true;
    }

    // <변경부분> 선택한 필드 기물의 현재 타입 아이콘을 UI에 복사하는 함수
    private void SetPieceTypeIconFromSelectedPiece(Piece selectedPiece)
    {
        if (pieceTypeIconImage == null)
        {
            return;
        }

        // Piece가 현재 들고 있는 타입 아이콘 스프라이트를 가져옴
        Sprite currentTypeIconSprite = selectedPiece.GetCurrentTypeIconSprite();

        // 타입 아이콘이 없으면 숨김
        if (currentTypeIconSprite == null)
        {
            pieceTypeIconImage.sprite = null;
            pieceTypeIconImage.enabled = false;
            return;
        }

        // 현재 필드 기물에 연결된 타입 아이콘을 UI에 그대로 표시
        pieceTypeIconImage.sprite = currentTypeIconSprite;
        pieceTypeIconImage.enabled = true;
        pieceTypeIconImage.preserveAspect = true;
    }

    // <변경부분> 일반스킬 슬롯에 아이콘과 레벨을 표시하는 함수
    private void SetGeneralSkillSlots(List<OwnedGeneralSkillData> generalSkills)
    {
        // 모든 일반스킬 슬롯을 먼저 빈 상태로 초기화
        ClearGeneralSkillSlots();

        // 일반스킬이 없으면 종료
        if (generalSkills == null)
        {
            return;
        }

        int textSlotCount = generalSkillTexts != null ? generalSkillTexts.Length : 0;
        int iconSlotCount = generalSkillIconImages != null ? generalSkillIconImages.Length : 0;
        int maxSlotCount = Mathf.Max(textSlotCount, iconSlotCount);

        int displayCount = Mathf.Min(generalSkills.Count, maxSlotCount, 6);

        for (int i = 0; i < displayCount; i++)
        {
            OwnedGeneralSkillData ownedSkillData = generalSkills[i];

            if (ownedSkillData == null || ownedSkillData.skillType == GeneralSkillType.None)
            {
                continue;
            }

            // <변경부분> Database에서 일반스킬 표시 데이터 가져오기
            GeneralSkillData skillData = GetGeneralSkillData(ownedSkillData.skillType);

            // <변경부분> 일반스킬 슬롯 Tooltip에 현재 일반스킬 설명 데이터 연결
            if (generalSkillTooltipTriggers != null &&
            i < generalSkillTooltipTriggers.Length &&
             generalSkillTooltipTriggers[i] != null)
            {
                // <변경부분> 일반스킬 데이터의 기존 이름/아이콘과 현재 보유 레벨에 맞는 설명으로 Tooltip을 자동 구성
                generalSkillTooltipTriggers[i].SetTooltipViewData(
                    TooltipViewData.FromGeneralSkillData(skillData, ownedSkillData.level)
                );
            }

            // <변경부분> 아이콘 이미지 표시
            if (generalSkillIconImages != null &&
                i < generalSkillIconImages.Length &&
                generalSkillIconImages[i] != null)
            {
                Sprite skillIconSprite = skillData != null ? skillData.iconSprite : null;

                generalSkillIconImages[i].sprite = skillIconSprite;
                generalSkillIconImages[i].enabled = skillIconSprite != null;
                generalSkillIconImages[i].preserveAspect = true;
            }

            // <변경부분> 레벨 텍스트 표시
            if (generalSkillTexts != null &&
                i < generalSkillTexts.Length &&
                generalSkillTexts[i] != null)
            {
                generalSkillTexts[i].text = "Lv" + ownedSkillData.level;
            }
        }
    }

    // <변경부분> 일반스킬 슬롯 아이콘과 텍스트를 모두 비우는 함수
    private void ClearGeneralSkillSlots()
    {
        // 일반스킬 레벨 텍스트 초기화
        if (generalSkillTexts != null)
        {
            for (int i = 0; i < generalSkillTexts.Length; i++)
            {
                if (generalSkillTexts[i] != null)
                {
                    generalSkillTexts[i].text = "";
                }
            }
        }

        // 일반스킬 아이콘 초기화
        if (generalSkillIconImages != null)
        {
            for (int i = 0; i < generalSkillIconImages.Length; i++)
            {
                if (generalSkillIconImages[i] != null)
                {
                    generalSkillIconImages[i].sprite = null;
                    generalSkillIconImages[i].enabled = false;
                }
            }
        }

        // <변경부분> 일반스킬 Tooltip 데이터 초기화
        if (generalSkillTooltipTriggers != null)
        {
            for (int i = 0; i < generalSkillTooltipTriggers.Length; i++)
            {
                if (generalSkillTooltipTriggers[i] != null)
                {
                    generalSkillTooltipTriggers[i].SetTooltipViewData(null);
                }
            }
        }
    }

    // <변경부분> 상태이상 슬롯에 아이콘/남은 턴/중첩을 표시하는 함수
    private void SetStatusEffectSlots(Piece selectedPiece)
    {
        // 먼저 모든 상태이상 슬롯을 비움
        ClearStatusEffectSlots();

        // 선택 기물이 없으면 종료
        if (selectedPiece == null)
        {
            return;
        }

        // 상태이상 슬롯 배열이 없으면 종료
        if (statusEffectSlots == null || statusEffectSlots.Length == 0)
        {
            return;
        }

        // 상태이상 데이터베이스가 없으면 표시 데이터 검색 불가
        if (statusEffectDatabase == null)
        {
            Debug.LogWarning("PieceStatusUIController에 StatusEffectDatabase가 연결되지 않았습니다.");
            return;
        }

        // 선택 기물이 현재 보유한 상태이상 목록 가져오기
        List<OwnedStatusEffectData> ownedStatusEffects = selectedPiece.GetStatusEffectsCopy();

        if (ownedStatusEffects == null)
        {
            return;
        }

        int displayCount = Mathf.Min(ownedStatusEffects.Count, statusEffectSlots.Length);

        for (int i = 0; i < displayCount; i++)
        {
            OwnedStatusEffectData ownedStatusEffect = ownedStatusEffects[i];

            if (ownedStatusEffect == null || ownedStatusEffect.effectType == StatusEffectType.None)
            {
                continue;
            }

            // 상태이상 타입에 맞는 데이터 검색
            StatusEffectData statusEffectData = statusEffectDatabase.GetData(ownedStatusEffect.effectType);

            if (statusEffectSlots[i] != null)
            {
                statusEffectSlots[i].Refresh(statusEffectData, ownedStatusEffect);
            }
        }
    }

    // <변경부분> 상태이상 슬롯을 모두 빈 상태로 초기화하는 함수
    private void ClearStatusEffectSlots()
    {
        if (statusEffectSlots == null)
        {
            return;
        }

        for (int i = 0; i < statusEffectSlots.Length; i++)
        {
            if (statusEffectSlots[i] != null)
            {
                statusEffectSlots[i].Clear();
            }
        }
    }

    // <변경부분> 일반스킬 타입에 맞는 GeneralSkillData를 Database에서 찾는 함수
    private GeneralSkillData GetGeneralSkillData(GeneralSkillType skillType)
    {
        if (generalSkillDatabase == null)
        {
            return null;
        }

        return generalSkillDatabase.GetData(skillType);
    }

    // <변경부분> 선택 기물이 없을 때 UI를 비우는 함수
    public void Clear()
    {
        // <변경부분> 선택한 기물이 없으면 스테이터스 창 숨김
        if (statusRoot != null)
        {
            statusRoot.SetActive(false);
        }

        if (pieceImage != null)
        {
            pieceImage.sprite = null;
            pieceImage.enabled = false;
        }

        if (pieceTypeIconImage != null)
        {
            pieceTypeIconImage.sprite = null;
            pieceTypeIconImage.enabled = false;
        }

        // <변경부분> 일반스킬 슬롯 아이콘과 텍스트 초기화
        ClearGeneralSkillSlots();

        // <변경부분> 상태이상 슬롯 초기화
        ClearStatusEffectSlots();
    }
}