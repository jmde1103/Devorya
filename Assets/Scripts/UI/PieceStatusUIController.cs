using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> 선택한 필드 기물의 현재 정보를 그대로 UI에 표시하는 컨트롤러
public class PieceStatusUIController : MonoBehaviour
{
    [Header("Piece Image")]
    [SerializeField] private Image pieceImage;

    [Header("Piece Type Icon")]
    [SerializeField] private Image pieceTypeIconImage;

    [Header("General Skill Slots")]
    [SerializeField] private TMP_Text[] generalSkillTexts;

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

        // 선택한 기물이 가진 일반스킬 목록 표시
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

    // <변경부분> 일반스킬 슬롯에 스킬명과 레벨을 표시하는 함수
    private void SetGeneralSkillSlots(List<GeneralSkillData> generalSkills)
    {
        if (generalSkillTexts == null)
        {
            return;
        }

        // 모든 슬롯을 먼저 비움
        for (int i = 0; i < generalSkillTexts.Length; i++)
        {
            if (generalSkillTexts[i] != null)
            {
                generalSkillTexts[i].text = "";
            }
        }

        // 일반스킬이 없으면 종료
        if (generalSkills == null)
        {
            return;
        }

        // 최대 6칸까지만 표시
        int displayCount = Mathf.Min(generalSkills.Count, generalSkillTexts.Length, 6);

        for (int i = 0; i < displayCount; i++)
        {
            GeneralSkillData skillData = generalSkills[i];

            if (generalSkillTexts[i] == null)
            {
                continue;
            }

            // 현재는 테스트용 텍스트 표시
            generalSkillTexts[i].text = GetGeneralSkillShortName(skillData.skillType) + "\nLv" + skillData.level;
        }
    }

    // <변경부분> 일반스킬 종류를 UI용 짧은 이름으로 변환하는 함수
    private string GetGeneralSkillShortName(GeneralSkillType skillType)
    {
        switch (skillType)
        {
            case GeneralSkillType.ChanceAttack:
                return "Ch";

            default:
                return "";
        }
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
    }
}