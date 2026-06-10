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

    // <변경부분> 일반스킬 아이콘/이름/설명을 찾기 위한 데이터베이스
    [SerializeField] private GeneralSkillDatabase generalSkillDatabase;

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
    }
}