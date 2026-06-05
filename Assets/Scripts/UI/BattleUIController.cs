using UnityEngine;
using UnityEngine.UI;

// <변경부분> 전투 화면의 버튼 UI를 관리하는 컨트롤러
public class BattleUIController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private BattleManager battleManager;

    [Header("Action Buttons")]
    [SerializeField] private Button absorbButton;
    [SerializeField] private Button uniqueSkillButton;

    [Header("Piece Status UI")]
    // <변경부분> 플레이어 선택 기물 정보를 표시하는 스테이터스 UI
    [SerializeField] private PieceStatusUIController playerStatusUIController;

    // <변경부분> 상대 선택 기물 정보를 표시하는 스테이터스 UI
    [SerializeField] private PieceStatusUIController enemyStatusUIController;

    [Header("Absorb Icon")]
    [SerializeField] private Image absorbIconImage;
    [SerializeField] private Sprite absorbOffSprite;
    [SerializeField] private Sprite absorbOnSprite;

    [Header("Unique Skill Icon")]
    [SerializeField] private Image uniqueSkillIconImage;

    // <변경부분> 고유 스킬 종류별 아이콘 목록
    [SerializeField] private UniqueSkillIconData[] uniqueSkillIcons;

    private void Start()
    {
        // 흡수 버튼 클릭 이벤트 연결
        if (absorbButton != null)
        {
            absorbButton.onClick.AddListener(OnClickAbsorbButton);
        }

        // 고유 스킬 버튼 클릭 이벤트 연결
        if (uniqueSkillButton != null)
        {
            uniqueSkillButton.onClick.AddListener(OnClickUniqueSkillButton);
        }

        // 게임 시작 시 액션 버튼 숨김
        HideActionButtons();
    }

    // <변경부분> 흡수 버튼 클릭 시 BattleManager의 흡수 모드 전환 호출
    private void OnClickAbsorbButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.ToggleAbsorbMode();
    }

    // <변경부분> 고유 스킬 버튼 클릭 시 BattleManager의 고유 스킬 사용 호출
    private void OnClickUniqueSkillButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.UseSelectedPieceSkill();
    }

    // 선택된 기물 상태에 따라 버튼 표시 갱신
    public void RefreshSelectedPieceButtons(Piece selectedPiece)
    {

        // <변경부분> 선택한 기물 정보를 스테이터스 UI에 표시
        if (pieceStatusUIController != null)
        {
            pieceStatusUIController.Refresh(selectedPiece);
        }

        // 선택된 기물이 없으면 버튼 숨김
        if (selectedPiece == null)
        {
            HideActionButtons();
            return;
        }

        // 플레이어 기물을 선택하면 흡수 버튼 표시
        SetAbsorbButtonVisible(true);

        // <변경부분> 기물을 새로 선택할 때 흡수 아이콘은 기본 OFF 상태로 표시
        SetAbsorbModeIcon(false);

        // 고유 스킬이 있는 기물만 고유 스킬 버튼 표시
        bool hasUniqueSkill = selectedPiece.UniqueSkill != UniqueSkillType.None;
        SetUniqueSkillButtonVisible(hasUniqueSkill);

        // <변경부분> 고유 스킬이 있으면 해당 스킬 아이콘으로 변경
        if (hasUniqueSkill)
        {
            SetUniqueSkillIcon(selectedPiece.UniqueSkill);
        }
    }

    // <변경부분> 흡수 버튼 표시/숨김
    public void SetAbsorbButtonVisible(bool isVisible)
    {
        if (absorbButton == null)
        {
            return;
        }

        absorbButton.gameObject.SetActive(isVisible);
    }

    // <변경부분> 흡수 모드 상태에 따라 버튼 위 아이콘만 변경
    public void SetAbsorbModeIcon(bool isAbsorbMode)
    {
        if (absorbIconImage == null)
        {
            return;
        }

        absorbIconImage.sprite = isAbsorbMode ? absorbOnSprite : absorbOffSprite;
    }

    // <변경부분> 고유 스킬 종류에 맞는 아이콘을 표시하는 함수
    private void SetUniqueSkillIcon(UniqueSkillType skillType)
    {
        // 고유 스킬 아이콘 이미지가 없으면 종료
        if (uniqueSkillIconImage == null)
        {
            return;
        }

        // 아이콘 목록에서 현재 스킬 타입과 같은 데이터를 찾음
        foreach (UniqueSkillIconData iconData in uniqueSkillIcons)
        {
            if (iconData.skillType == skillType)
            {
                uniqueSkillIconImage.sprite = iconData.iconSprite;
                uniqueSkillIconImage.enabled = iconData.iconSprite != null;
                return;
            }
        }

        // 해당 스킬 아이콘을 찾지 못하면 아이콘 숨김
        uniqueSkillIconImage.sprite = null;
        uniqueSkillIconImage.enabled = false;

        Debug.LogWarning($"고유 스킬 아이콘을 찾지 못했습니다: {skillType}");
    }

    // <변경부분> 고유 스킬 버튼 표시/숨김
    public void SetUniqueSkillButtonVisible(bool isVisible)
    {
        if (uniqueSkillButton == null)
        {
            return;
        }

        uniqueSkillButton.gameObject.SetActive(isVisible);
    }

    // <변경부분> 액션 버튼 전체 숨김
    public void HideActionButtons()
    {
        SetAbsorbButtonVisible(false);
        SetUniqueSkillButtonVisible(false);

        // 흡수 버튼 아이콘을 기본 OFF 상태로 변경
        SetAbsorbModeIcon(false);

        // <변경부분> 고유 스킬 아이콘 숨김
        if (uniqueSkillIconImage != null)
        {
            uniqueSkillIconImage.sprite = null;
            uniqueSkillIconImage.enabled = false;
        }

        // <변경부분> 선택 기물 정보 UI 비우기
        if (pieceStatusUIController != null)
        {
            pieceStatusUIController.Clear();
        }

    }
}