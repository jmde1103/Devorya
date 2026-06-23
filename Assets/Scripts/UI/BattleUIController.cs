using UnityEngine;
using UnityEngine.UI;
using TMPro;

// <변경부분> 전투 화면의 버튼 UI를 관리하는 컨트롤러
public class BattleUIController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private BattleManager battleManager;

    [Header("Action Buttons")]
    [SerializeField] private Button absorbButton;
    [SerializeField] private Button uniqueSkillButton;

    // <변경부분> 테스트용 아이템 추가 버튼
    [SerializeField] private Button debugAddItemButton;
    // <변경부분> 테스트용 강제 턴 넘기기 버튼
    [SerializeField] private Button debugForceEndTurnButton;
    // <변경부분> 테스트용 유물 추가 버튼
    [SerializeField] private Button debugAddRelicButton;


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
    // <변경부분> 고유스킬 쿨타임 숫자 뒤에 표시할 검정 배경 이미지
    [SerializeField] private GameObject uniqueSkillCooldownImageObject;
    // <변경부분> 고유스킬 버튼 위에 표시할 쿨타임 숫자 텍스트
    [SerializeField] private TMP_Text uniqueSkillCooldownText;

    [Header("Unique Skill Database")]
    // <변경부분> 고유스킬 타입으로 아이콘 데이터를 찾기 위한 데이터베이스
    [SerializeField] private UniqueSkillDatabase uniqueSkillDatabase;

    [Header("Tooltip")]
    // <변경부분> 흡수 버튼을 꾹 눌렀을 때 표시할 Tooltip 데이터
    [SerializeField] private TooltipData absorbTooltipData;

    // <변경부분> 흡수 버튼에 붙어 있는 TooltipTrigger
    [SerializeField] private TooltipTrigger absorbTooltipTrigger;

    // <변경부분> 고유스킬 버튼에 붙어 있는 TooltipTrigger
    [SerializeField] private TooltipTrigger uniqueSkillTooltipTrigger;

    // <변경부분> 전투 중 사용하는 아이템 슬롯 UI 목록
    [Header("Item Slots")]
    [SerializeField] private BattleItemSlotUI[] itemSlotUIs;

    [Header("Relic Slots")]
    // <변경부분> 전투 중 보유한 유물을 표시하는 유물 슬롯 UI 목록
    [SerializeField] private BattleRelicSlotUI[] relicSlotUIs;

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

        // <변경부분> 테스트용 아이템 추가 버튼 클릭 이벤트 연결
        if (debugAddItemButton != null)
        {
            debugAddItemButton.onClick.AddListener(OnClickDebugAddItemButton);
        }

        // <변경부분> 테스트용 강제 턴 넘기기 버튼 클릭 이벤트 연결
        if (debugForceEndTurnButton != null)
        {
            debugForceEndTurnButton.onClick.AddListener(OnClickDebugForceEndTurnButton);
        }

        // <변경부분> 테스트용 유물 추가 버튼 클릭 이벤트 연결
        if (debugAddRelicButton != null)
        {
            debugAddRelicButton.onClick.AddListener(OnClickDebugAddRelicButton);
        }

        // <변경부분> 아이템 슬롯 버튼 클릭 이벤트 연결
        InitializeItemSlots();

        // <변경부분> 흡수/고유스킬 버튼 Tooltip 초기화
        InitializeActionButtonTooltips();

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

    // <변경부분> 아이템 슬롯 UI를 초기화하는 함수
    private void InitializeItemSlots()
    {
        // 아이템 슬롯 배열이 없으면 종료
        if (itemSlotUIs == null)
        {
            return;
        }

        // 각 슬롯에 자신의 번호와 상위 UI를 알려줌
        for (int i = 0; i < itemSlotUIs.Length; i++)
        {
            if (itemSlotUIs[i] == null)
            {
                continue;
            }

            itemSlotUIs[i].Initialize(this, i);
        }
    }

    // <변경부분> 전투 액션 버튼에 TooltipData를 연결하는 함수
    private void InitializeActionButtonTooltips()
    {
        // 흡수 버튼은 전투 내내 같은 설명을 사용하므로 시작 시 한 번만 연결
        if (absorbTooltipTrigger != null)
        {
            absorbTooltipTrigger.SetTooltipData(absorbTooltipData);
        }

        // 고유스킬 버튼은 선택한 기물에 따라 Tooltip이 바뀌므로 초기에는 비움
        if (uniqueSkillTooltipTrigger != null)
        {
            uniqueSkillTooltipTrigger.SetTooltipData(null);
        }
    }

    // <변경부분> 아이템 슬롯 클릭 시 BattleManager에 아이템 사용 요청
    public void OnClickItemSlot(int slotIndex)
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.UseItemAtSlot(slotIndex);
    }

    // <변경부분> 아이템 슬롯 UI 전체를 현재 아이템 목록에 맞게 갱신
    public void RefreshItemSlots(BattleItemData[] itemSlots)
    {
        if (itemSlotUIs == null)
        {
            return;
        }

        for (int i = 0; i < itemSlotUIs.Length; i++)
        {
            if (itemSlotUIs[i] == null)
            {
                continue;
            }

            BattleItemData itemData = null;

            if (itemSlots != null && i < itemSlots.Length)
            {
                itemData = itemSlots[i];
            }

            itemSlotUIs[i].Refresh(itemData);
        }
    }

    // <변경부분> 현재 유물 슬롯 정보를 UI에 반영하는 함수
    public void RefreshRelicSlots(BattleRelicData[] relicSlots)
    {
        // 유물 슬롯 UI 배열이 없으면 갱신할 대상이 없음
        if (relicSlotUIs == null)
        {
            return;
        }

        // 슬롯 UI 개수만큼 유물 아이콘 표시 상태를 갱신
        for (int i = 0; i < relicSlotUIs.Length; i++)
        {
            if (relicSlotUIs[i] == null)
            {
                continue;
            }

            BattleRelicData relicData = null;

            // 실제 유물 배열에 해당 슬롯 데이터가 있으면 가져옴
            if (relicSlots != null && i < relicSlots.Length)
            {
                relicData = relicSlots[i];
            }

            relicSlotUIs[i].Refresh(relicData);
        }
    }


    // 선택된 기물 상태에 따라 버튼 표시 갱신
    public void RefreshSelectedPieceButtons(Piece selectedPiece)
    {
        // <변경부분> 선택한 플레이어 기물 정보를 왼쪽 하단 스테이터스 UI에 표시
        if (playerStatusUIController != null)
        {
            playerStatusUIController.Refresh(selectedPiece);
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

        // <변경부분> 선택된 기물의 고유스킬 쿨타임 숫자 갱신
        RefreshUniqueSkillCooldownText(selectedPiece);
    }

    // <변경부분> 상대 기물 정보를 오른쪽 상단 스테이터스 UI에 표시하는 함수
    public void RefreshEnemyStatus(Piece enemyPiece)
    {
        // <변경부분> 상대 스테이터스 갱신 호출 확인
        Debug.Log("상대 스테이터스 갱신 호출: " + enemyPiece.PieceType);

        if (enemyStatusUIController != null)
        {
            enemyStatusUIController.Refresh(enemyPiece);
        }
        else
        {
            Debug.LogWarning("Enemy Status UI Controller가 연결되지 않았습니다.");
        }
    }

    // <변경부분> 상대 기물 스테이터스 UI를 숨기는 함수
    public void ClearEnemyStatus()
    {
        if (enemyStatusUIController != null)
        {
            enemyStatusUIController.Clear();
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

        // 데이터베이스가 없으면 아이콘 숨김
        if (uniqueSkillDatabase == null)
        {
            uniqueSkillIconImage.sprite = null;
            uniqueSkillIconImage.enabled = false;

            Debug.LogWarning("BattleUIController에 UniqueSkillDatabase가 연결되지 않았습니다.");
            return;
        }

        // 고유스킬 데이터 검색
        UniqueSkillData skillData = uniqueSkillDatabase.GetData(skillType);

        // 데이터가 없거나 아이콘이 없으면 아이콘 숨김
        if (skillData == null || skillData.iconSprite == null)
        {
            uniqueSkillIconImage.sprite = null;
            uniqueSkillIconImage.enabled = false;

            // <변경부분> 표시할 고유스킬 데이터가 없으면 Tooltip도 비움
            if (uniqueSkillTooltipTrigger != null)
            {
                uniqueSkillTooltipTrigger.SetTooltipData(null);
            }

            Debug.LogWarning($"고유 스킬 아이콘을 찾지 못했습니다: {skillType}");
            return;
        }

        // 데이터에 등록된 아이콘 적용
        uniqueSkillIconImage.sprite = skillData.iconSprite;
        uniqueSkillIconImage.enabled = true;

        // <변경부분> 현재 선택된 고유스킬 설명 Tooltip 연결
        if (uniqueSkillTooltipTrigger != null)
        {
            uniqueSkillTooltipTrigger.SetTooltipData(skillData.tooltipData);
        }
    }

    // <변경부분> 선택된 기물의 고유스킬 쿨타임 숫자와 배경 이미지를 갱신하는 함수
    private void RefreshUniqueSkillCooldownText(Piece selectedPiece)
    {
        // 선택 기물이 없거나 고유스킬이 없으면 쿨타임 UI 숨김
        if (selectedPiece == null || selectedPiece.UniqueSkill == UniqueSkillType.None)
        {
            HideUniqueSkillCooldownUI();
            return;
        }

        // 선택 기물의 현재 고유스킬 쿨타임 가져오기
        int cooldown = selectedPiece.GetUniqueSkillCooldown();

        // 쿨타임이 없으면 쿨타임 UI 숨김
        if (cooldown <= 0)
        {
            HideUniqueSkillCooldownUI();
            return;
        }

        // <변경부분> 쿨타임이 남아 있으면 검정 배경 Image 활성화
        if (uniqueSkillCooldownImageObject != null)
        {
            uniqueSkillCooldownImageObject.SetActive(true);
        }

        // <변경부분> 쿨타임 숫자 표시
        if (uniqueSkillCooldownText != null)
        {
            uniqueSkillCooldownText.text = cooldown.ToString();
            uniqueSkillCooldownText.gameObject.SetActive(true);
        }
    }

    // <변경부분> 고유스킬 쿨타임 배경 이미지와 숫자를 숨기는 함수
    private void HideUniqueSkillCooldownUI()
    {
        // 쿨타임 배경 Image 오브젝트 숨김
        if (uniqueSkillCooldownImageObject != null)
        {
            uniqueSkillCooldownImageObject.SetActive(false);
        }

        // 쿨타임 숫자 텍스트 숨김
        if (uniqueSkillCooldownText != null)
        {
            uniqueSkillCooldownText.text = "";
            uniqueSkillCooldownText.gameObject.SetActive(false);
        }
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

        if (uniqueSkillIconImage != null)
        {
            uniqueSkillIconImage.sprite = null;
            uniqueSkillIconImage.enabled = false;
        }

        // <변경부분> 선택 기물이 사라지면 고유스킬 Tooltip도 비움
        if (uniqueSkillTooltipTrigger != null)
        {
            uniqueSkillTooltipTrigger.SetTooltipData(null);
        }

        // <변경부분> 고유스킬 쿨타임 배경 이미지와 숫자 숨김
        HideUniqueSkillCooldownUI();

        // <변경부분> 플레이어 스테이터스 UI 숨김
        if (playerStatusUIController != null)
        {
            playerStatusUIController.Clear();
        }

        // <변경부분> 상대 스테이터스 UI 숨김
        if (enemyStatusUIController != null)
        {
            enemyStatusUIController.Clear();
        }
    }

    // <변경부분> 테스트용 아이템 추가 버튼 클릭 시 BattleManager에 아이템 추가 요청
    private void OnClickDebugAddItemButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.AddTestItemForDebug();
    }

    // <변경부분> 테스트용 강제 턴 넘기기 버튼 클릭 시 BattleManager에 턴 종료 요청
    private void OnClickDebugForceEndTurnButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.DebugForceEndTurn();
    }

    // <변경부분> 테스트용 유물 추가 버튼 클릭 시 BattleManager에 유물 추가 요청
    private void OnClickDebugAddRelicButton()
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.AddTestRelicForDebug();
    }
}