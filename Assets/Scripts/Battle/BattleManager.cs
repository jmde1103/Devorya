using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    //다른 스크립트에서 BattleManager에 접근하기 위한 임시 싱글톤
    public static BattleManager Instance { get; private set; }

    // 보드 매니저 참조
    [Header("Manager")]
    [SerializeField] private BoardManager boardManager;
    // 기물 매니저 참조
    [SerializeField] private PieceManager pieceManager;

    // 현재 선택된 기물
    private Piece selectedPiece;
    // <변경부분> 공격 전에 한 번 확인한 상대 기물
    private Piece pendingAttackTargetPiece = null;

    // 현재 전투 턴 주체
    [SerializeField] private BattleTurn currentTurn = BattleTurn.Player;
    //현재 전투 결과 상태
    private BattleResult battleResult = BattleResult.None;

    // <변경부분> 현재 전투 턴 번호
    [SerializeField] private int turnCount = 1;


    // <변경부분> 기물 타입 아이콘 표시 상태
    private bool isTypeIconVisible = false;
    // 흡수 모드가 켜져 있는지 여부
    private bool isAbsorbMode = false;
    // 전투가 끝났는지 여부
    private bool isBattleEnded = false;
    // 현재 턴에 고유 스킬을 이미 사용했는지 여부
    private bool hasUsedUniqueSkillThisTurn = false;

    // <변경부분> 찬스어택 발동으로 추가 행동 중인 기물
    private Piece chanceAttackBonusPiece = null;

    // 현재 하이라이트된 타일 목록
    private readonly List<Tile> highlightedTiles = new List<Tile>();

    // 현재 선택된 기물이 실제로 이동/공격할 수 있는 타일 목록
    private readonly List<Tile> selectableTiles = new List<Tile>();

    // <변경부분> 찬스어택이 연속으로 발동된 횟수
    private int chanceAttackContinuousCount = 0;

    // <변경부분> 흡수 유물 찬스어택이 이번 플레이어 턴에 이미 발동했는지 확인
    private bool hasUsedAbsorbChanceAttackRelicThisTurn = false;

    // <변경부분> 전투 아이템 슬롯 최대 개수
    private const int MaxItemSlotCount = 4;

    // <변경부분> 현재 전투에서 보유 중인 소모성 아이템 슬롯
    private BattleItemData[] itemSlots = new BattleItemData[MaxItemSlotCount];

    [Header("Test Item")]
    // <변경부분> 테스트용으로 전투 시작 시 지급할 아이템 데이터
    [SerializeField] private BattleItemData testStartItemData = new BattleItemData();

    // <변경부분> 게임 시작 시 테스트 아이템을 지급할지 여부
    [SerializeField] private bool addTestStartItem = true;

    // <변경부분> 전투 유물 슬롯 최대 개수
    private const int MaxRelicSlotCount = 10;

    // <변경부분> 현재 전투에서 보유 중인 유물 슬롯
    private BattleRelicData[] relicSlots = new BattleRelicData[MaxRelicSlotCount];

    [Header("Test Relic")]
    // <변경부분> 테스트용으로 전투 시작 시 지급하거나 버튼으로 추가할 유물 데이터
    [SerializeField] private BattleRelicData testRelicData = new BattleRelicData();

    // <변경부분> 게임 시작 시 테스트 유물을 지급할지 여부
    [SerializeField] private bool addTestStartRelic = false;

    [Header("UI")]
    [SerializeField] private BattleUIController battleUIController;
    [SerializeField] private Button surrenderButton;

    // <변경부분> 기물 타입 아이콘 표시 버튼
    [SerializeField] private Button typeIconButton;

    [SerializeField] private TurnInfoUIController turnInfoUIController;


    // 오브젝트 생성 시 한 번 실행
    private void Awake()
    {
        // 싱글톤 등록
        Instance = this;
    }

    private void Start()
    {
        // <변경부분> 게임 시작 시 스테이지명과 턴 정보 표시
        if (turnInfoUIController != null)
        {
            turnInfoUIController.SetStageName("젤루의 숲 입구 #1");
            turnInfoUIController.RefreshTurnInfo(turnCount, currentTurn);
        }

        // 기권 버튼 연결
        if (surrenderButton != null)
        {
            surrenderButton.onClick.AddListener(Surrender);
        }

        // 기물 타입 아이콘 버튼 연결
        if (typeIconButton != null)
        {
            typeIconButton.onClick.AddListener(ToggleTypeIcons);
        }

        // <변경부분> 게임 시작 시 액션 버튼 숨김
        if (battleUIController != null)
        {
            battleUIController.HideActionButtons();
        }

        // <변경부분> 게임 시작 시 아이템 슬롯 UI 초기화
        RefreshItemSlotUI();

        // <변경부분> 테스트용 아이템이 설정되어 있으면 전투 시작 시 1개 지급
        if (addTestStartItem &&
            testStartItemData != null &&
            testStartItemData.itemType != BattleItemType.None)
        {
            AddBattleItem(testStartItemData);
        }

        // <변경부분> 게임 시작 시 유물 슬롯 UI 초기화
        RefreshRelicSlotUI();

        // <변경부분> 테스트용 유물이 설정되어 있으면 전투 시작 시 1개 지급
        if (addTestStartRelic &&
            testRelicData != null &&
            testRelicData.relicType != BattleRelicType.None)
        {
            AddBattleRelic(testRelicData);
        }
    }

    private void Update()
    {
        // Space 키를 누르면 턴 종료
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
        }

        // A 키를 누르면 흡수 모드 ON/OFF
        if (Input.GetKeyDown(KeyCode.A))
        {
            ToggleAbsorbMode();
        }

        // Q 키를 누르면 기권
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Surrender();
        }

        // <변경부분> S 키를 누르면 선택된 기물의 고유 스킬 사용
        if (Input.GetKeyDown(KeyCode.S))
        {
            UseSelectedPieceSkill();
        }
    }



    // 기물을 선택하는 함수
    public void SelectPiece(Piece piece)
    {
        // 전투가 끝났으면 더 이상 선택 불가
        if (isBattleEnded)
        {
            return;
        }

        // 이전 하이라이트 제거
        ClearHighlights();

        // 선택한 기물이 없으면 종료
        if (piece == null)
        {
            selectedPiece = null;

            // <변경부분> 선택 해제 시 공격 확인 대상 초기화
            pendingAttackTargetPiece = null;

            // <변경부분> 선택 가능한 기물이 아니면 모든 타입 아이콘 비활성화
            SetAllTypeIconsVisible(false);

            // <변경부분> 선택된 기물이 없으므로 흡수/고유스킬 버튼 숨김
            if (battleUIController != null)
            {
                battleUIController.HideActionButtons();
            }

            return;
        }

        // <변경부분> 찬스어택 추가 행동 중에는 발동한 기물만 다시 선택 가능
        if (chanceAttackBonusPiece != null && piece != chanceAttackBonusPiece)
        {
            Debug.Log("찬스어택 추가 행동 중에는 발동한 기물만 움직일 수 있습니다.");
            return;
        }

        // 이동할 수 없는 기물은 선택 불가
        if (piece.CanMove == false)
        {
            selectedPiece = null;

            // <변경부분> 선택된 기물이 없으므로 액션 버튼 숨김
            if (battleUIController != null)
            {
                battleUIController.HideActionButtons();
            }

            // <변경부분> 선택 가능한 기물이 아니면 모든 타입 아이콘 비활성화
            SetAllTypeIconsVisible(false);
            return;
        }

        // 현재 플레이어 턴인데 플레이어 기물이 아니면 선택 불가
        if (currentTurn == BattleTurn.Player && piece.Team != PieceTeam.Player)
        {
            Debug.Log("상대 기물 정보를 확인합니다.");

            // <변경부분> 플레이어 턴에 상대 기물을 클릭하면 오른쪽 상단 스테이터스 UI에 표시
            if (battleUIController != null)
            {
                battleUIController.RefreshEnemyStatus(piece);
            }

            // <변경부분> 상대 기물은 플레이어 선택 기물로 저장하지 않음
            selectedPiece = null;

            // <변경부분> 상대 기물 클릭 시 플레이어 타입 아이콘만 비활성화
            SetAllTypeIconsVisible(false);

            // 중요: 여기서는 HideActionButtons() 호출 금지
            // HideActionButtons()를 호출하면 EnemyStatusPanel까지 같이 꺼짐

            return;
        }

        //현재 적 턴인데 적 기물이 아니면 선택 불가
        if (currentTurn == BattleTurn.Enemy && piece.Team != PieceTeam.Enemy)
        {
            Debug.Log("현재는 적 턴입니다.");
            
            selectedPiece = null;

            // <변경부분> 선택된 기물이 없으므로 액션 버튼 숨김
            if (battleUIController != null)
            {
                battleUIController.HideActionButtons();
            }
            return;
        }

        // 선택 기물 저장
        selectedPiece = piece;

        // <변경부분> 플레이어 기물을 새로 선택하면 상대 스테이터스 UI 숨김
        if (battleUIController != null)
        {
            battleUIController.ClearEnemyStatus();
        }

        // <변경부분> 선택한 기물에 맞게 흡수/고유스킬 버튼 표시 갱신
        if (battleUIController != null)
        {
            battleUIController.RefreshSelectedPieceButtons(selectedPiece);
        }

        // <변경부분> 새 기물을 선택했으므로 이전 공격 확인 대상 초기화
        pendingAttackTargetPiece = null;

        // 선택한 기물의 타입 아이콘만 표시
        ShowOnlySelectedPieceTypeIcon(selectedPiece);

        // 이동 가능 타일 표시
        ShowMovableTiles(selectedPiece);

        // 선택 확인용 로그
        Debug.Log($"선택됨: {piece.Team} / {piece.PieceType} / ({piece.X}, {piece.Y})");
    }

    //타일을 선택했을 때 호출되는 함수
    public void SelectTile(Tile tile)
    {
        //전투가 끝났으면 타일 선택 불가
        if (isBattleEnded)
        {
            return;
        }

        // <변경부분> 클릭한 타일 위에 있는 기물 확인
        Piece clickedPiece = pieceManager.GetPieceAt(tile.X, tile.Y);

        // <변경부분> 선택된 플레이어 기물이 없고, 타일 위에 기물이 있다면 기물 선택/정보 표시로 처리
        if (selectedPiece == null && clickedPiece != null)
        {
            SelectPiece(clickedPiece);
            return;
        }

        // <변경부분> 이미 선택된 기물이 있는 상태에서 같은 팀 기물을 클릭하면 새 기물 선택으로 처리
        if (selectedPiece != null &&
            clickedPiece != null &&
            clickedPiece.Team == selectedPiece.Team)
        {
            SelectPiece(clickedPiece);
            return;
        }

        // <변경부분> 선택된 플레이어 기물이 있고, 타일 위에 상대/중립 기물이 있다면 공격 확인 대상으로 처리
        if (selectedPiece != null &&
            clickedPiece != null &&
            clickedPiece.Team != selectedPiece.Team)
        {
            // 처음 클릭한 상대 기물이거나 이전 확인 대상과 다르면 정보만 표시
            if (pendingAttackTargetPiece != clickedPiece)
            {
                // <변경부분> 이전에 확인하던 상대 기물 아이콘만 끔
                if (pendingAttackTargetPiece != null)
                {
                    pendingAttackTargetPiece.SetTypeIconVisible(false);
                }

                // <변경부분> 새로 확인한 상대 기물을 저장
                pendingAttackTargetPiece = clickedPiece;

                // <변경부분> 클릭한 상대 기물의 타입 아이콘 표시
                // 플레이어 선택 기물의 타입 아이콘은 유지
                clickedPiece.SetTypeIconVisible(true);

                // 상대 스테이터스 UI 표시
                if (battleUIController != null)
                {
                    battleUIController.RefreshEnemyStatus(clickedPiece);
                }

                Debug.Log("상대 기물 정보 확인: 같은 기물을 한 번 더 클릭하면 공격합니다.");
                return;
            }

            // 같은 상대 기물을 두 번째 클릭했으므로 아래 기존 공격 로직으로 진행
        }


        // 선택된 기물이 없으면 종료
        if (selectedPiece == null)
        {
            return;
        }

        // 클릭한 타일이 이동/공격 가능한 타일이 아니면 종료
        if (selectableTiles.Contains(tile) == false)
        {
            return;
        }

        // 해당 타일에 있는 기물 확인
        Piece targetPiece = pieceManager.GetPieceAt(tile.X, tile.Y);

        // <변경부분> 이번 행동을 실행하는 기물을 미리 저장
        Piece actingPiece = selectedPiece;

        // <변경부분> 흡수/레벨업이 적용되기 전 찬스어택 레벨을 저장
        int chanceAttackLevelBeforeAction = actingPiece.GetGeneralSkillLevel(GeneralSkillType.ChanceAttack);

        // <변경부분> 이번 행동으로 적 기물을 처치했는지 확인하기 위한 값
        bool killedEnemyPiece = false;

        // <변경부분> 이번 행동이 플레이어 흡수 성공 행동인지 확인하기 위한 값
        bool absorbedEnemyPiece = false;

        // <변경부분> 기물이 이동/공격하면 모든 타입 아이콘 비활성화
        SetAllTypeIconsVisible(false);


        // 타겟 기물이 있으면 공격 처리
        if (targetPiece != null)
        {
            // 적대 관계가 아니면 공격 불가
            if (selectedPiece.IsEnemyOf(targetPiece) == false)
            {
                return;
            }

            // 흡수 모드이고, 플레이어 기물이 적 기물을 잡는 경우
            // 단, 상대 King은 흡수 대상에서 제외
            if (isAbsorbMode &&
                selectedPiece.Team == PieceTeam.Player &&
                selectedPiece.PieceType != PieceType.King &&
                targetPiece.Team == PieceTeam.Enemy &&
                targetPiece.PieceType != PieceType.King)
            {
                PieceType absorbedType = targetPiece.PieceType;

                pieceManager.AbsorbPiece(selectedPiece, targetPiece);

                // <변경부분> 흡수로 기물 외형/타입/스킬 정보가 바뀌었으므로 스테이터스 UI 갱신
                if (battleUIController != null)
                {
                    battleUIController.RefreshSelectedPieceButtons(selectedPiece);
                }

                // <변경부분> 적대 기물을 제거했으므로 찬스어택 판정 대상으로 저장
                killedEnemyPiece = true;

                // <변경부분> 플레이어 흡수 성공 행동이므로 유물 효과 판정 대상으로 저장
                absorbedEnemyPiece = true;

                pieceManager.RemovePiece(targetPiece);

                isAbsorbMode = false;

                Debug.Log($"흡수 성공: {absorbedType} 데이터를 복사했습니다.");
            }
            else
            {
                // <변경부분> 적대 기물을 제거했으므로 찬스어택 판정 대상으로 저장
                killedEnemyPiece = true;

                pieceManager.RemovePiece(targetPiece);
            }
        }

        // 선택한 기물을 해당 타일로 이동
        pieceManager.MovePiece(selectedPiece, tile.X, tile.Y);

        // <변경부분> 이동/공격이 실행되었으므로 공격 확인 대상 초기화
        pendingAttackTargetPiece = null;

        // 이동/공격 후 승패 조건 확인
        CheckBattleEnd();

        // 전투가 끝났으면 턴 종료하지 않음
        if (isBattleEnded)
        {
            return;
        }

        // <변경부분> 흡수 유물을 보유 중이고, 이번 행동이 흡수 성공이라면 턴당 1번 찬스어택을 확정 발동
        if (absorbedEnemyPiece && TryActivateAbsorbChanceAttackRelic(actingPiece))
        {
            // <변경부분> 흡수 유물 찬스어택은 이번 플레이어 턴에 이미 사용한 것으로 저장
            hasUsedAbsorbChanceAttackRelicThisTurn = true;

            // <변경부분> 흡수 후 유물 찬스어택으로 추가 행동을 얻었으므로 방금 얻은 고유스킬을 바로 사용할 수 있게 처리
            actingPiece.EnableUniqueSkillAfterAbsorbChanceAttack();

            // <변경부분> 유물 효과로도 일반 찬스어택과 동일하게 추가 행동 상태를 부여
            ActivateChanceAttackBonus(actingPiece);

            Debug.Log("유물 효과 발동: 흡수 성공으로 찬스어택이 확정 발동했습니다.");
            return;
        }

        // <변경부분> 적 기물을 처치했을 때, 행동 시작 전 레벨 기준으로 찬스어택 발동 여부 확인
        if (killedEnemyPiece && TryActivateChanceAttack(actingPiece, chanceAttackLevelBeforeAction))
        {
            // <변경부분> 일반 찬스어택 연속 발동 횟수 증가
            chanceAttackContinuousCount++;

            // <변경부분> 일반 찬스어택과 동일하게 추가 행동 상태를 부여
            ActivateChanceAttackBonus(actingPiece);

            Debug.Log("찬스어택 발동: 턴 종료 없이 한 번 더 이동할 수 있습니다.");
            return;
        }

        // <변경부분> 찬스어택이 실패하거나 발동 조건이 아니면 연속 발동 상태 초기화
        chanceAttackBonusPiece = null;
        chanceAttackContinuousCount = 0;

        // 이동 후 턴 종료
        EndTurn();
    }

    // 일반 전투에서 기권하는 함수
    public void Surrender()
    {
        // 이미 끝난 전투면 무시
        if (isBattleEnded)
        {
            return;
        }

        // 일반 전투 기권은 패배 처리
        EndBattle(BattleResult.Lose);

        Debug.Log("기권: 일반 전투 패배 / 보상 없음 / 받은 피해와 사망 상태 유지");
    }

    public void ToggleAbsorbMode()
    {
        if (currentTurn != BattleTurn.Player)
        {
            Debug.Log("흡수는 플레이어 턴에만 사용할 수 있습니다.");
            return;
        }

        if (selectedPiece == null)
        {
            Debug.Log("흡수할 기물을 먼저 선택해야 합니다.");
            return;
        }

        if (selectedPiece.Team == PieceTeam.Player &&
            selectedPiece.PieceType == PieceType.King)
        {
            isAbsorbMode = false;
            Debug.Log("Player King은 흡수를 사용할 수 없습니다.");
            return;
        }

        // 흡수 모드 상태 반전
        isAbsorbMode = !isAbsorbMode;

        // <변경부분> 흡수 모드 상태에 따라 UI 아이콘 변경
        if (battleUIController != null)
        {
            battleUIController.SetAbsorbModeIcon(isAbsorbMode);
        }

        Debug.Log(isAbsorbMode ? "흡수 모드 ON" : "흡수 모드 OFF");
    }

    // 현재 선택된 기물의 고유 스킬을 사용하는 함수
    public void UseSelectedPieceSkill()
    {
        // 전투가 끝났으면 스킬 사용 불가
        if (isBattleEnded)
        {
            return;
        }

        // 선택된 기물이 없으면 스킬 사용 불가
        if (selectedPiece == null)
        {
            Debug.Log("스킬을 사용할 기물을 먼저 선택해야 합니다.");
            return;
        }

        // 현재 턴의 기물이 아니면 스킬 사용 불가
        if (IsCurrentTurnPiece(selectedPiece) == false)
        {
            Debug.Log("현재 턴의 기물만 스킬을 사용할 수 있습니다.");
            return;
        }

        // <변경부분> 이번 턴에 이미 고유 스킬을 사용했으면 모든 기물 고유 스킬 사용 불가
        if (hasUsedUniqueSkillThisTurn)
        {
            Debug.Log("이번 턴에는 이미 고유 스킬을 사용했습니다.");
            return;
        }

        // <변경부분> 선택된 기물의 고유 스킬 사용 가능 여부 확인
        // 여기서는 고유 스킬 없음 / 개별 쿨타임 여부를 검사
        if (selectedPiece.CanUseUniqueSkill() == false)
        {
            Debug.Log("고유 스킬을 사용할 수 없습니다. 쿨타임 중이거나 사용할 수 없는 스킬입니다.");
            return;
        }

        // <변경부분> 실제 스킬 성공 여부 저장
        bool skillUsed = false;

        // 선택된 기물의 고유 스킬 종류에 따라 실행
        switch (selectedPiece.UniqueSkill)
        {
            case UniqueSkillType.JelluMultiply:
                // 젤루 증식 스킬 실행
                skillUsed = UseJelluMultiply(selectedPiece);
                break;

            default:
                // 실행 가능한 고유 스킬이 없으면 실패 처리
                Debug.Log("사용할 수 있는 고유 스킬이 없습니다.");
                break;
        }

        // <변경부분> 스킬이 실제로 성공했을 때만 턴 사용권과 쿨타임 적용
        if (skillUsed)
        {
            // 이번 턴 전체 고유 스킬 사용 완료 처리
            hasUsedUniqueSkillThisTurn = true;

            // 선택된 기물에 고유 스킬 쿨타임 적용
            selectedPiece.MarkUniqueSkillUsed();

            // 고유 스킬 사용 완료 로그
            Debug.Log("고유 스킬 사용 완료: 이번 턴 고유 스킬 사용권 소모 / 선택 기물 쿨타임 적용");
        }
    }

    // <변경부분> 전투 아이템을 왼쪽 빈 슬롯부터 추가하는 함수
    public void AddBattleItem(BattleItemData itemData)
    {
        // 추가할 아이템 데이터가 없으면 종료
        if (itemData == null || itemData.itemType == BattleItemType.None)
        {
            Debug.LogWarning("추가할 아이템 데이터가 없습니다.");
            return;
        }

        // 왼쪽 슬롯부터 빈칸을 찾음
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null && itemSlots[i].itemType != BattleItemType.None)
            {
                continue;
            }

            itemSlots[i] = itemData;

            // 아이템 획득 후 슬롯 UI 갱신
            RefreshItemSlotUI();

            Debug.Log($"아이템 획득: {itemData.itemName} / 슬롯 {i}");
            return;
        }

        Debug.Log("아이템 슬롯이 가득 찼습니다.");
    }

    // <변경부분> 테스트 버튼에서 호출하는 테스트 아이템 추가 함수
    public void AddTestItemForDebug()
    {
        // 테스트 아이템 데이터가 없으면 추가 불가
        if (testStartItemData == null || testStartItemData.itemType == BattleItemType.None)
        {
            Debug.LogWarning("테스트 아이템 데이터가 설정되지 않았습니다.");
            return;
        }

        // 테스트 아이템을 현재 아이템 슬롯에 추가
        AddBattleItem(testStartItemData);
    }

    // <변경부분> 특정 슬롯의 아이템을 사용하는 함수
    public void UseItemAtSlot(int slotIndex)
    {
        // 전투가 끝났으면 아이템 사용 불가
        if (isBattleEnded)
        {
            return;
        }

        // 아이템은 플레이어 턴에만 사용 가능
        if (currentTurn != BattleTurn.Player)
        {
            Debug.Log("아이템은 플레이어 턴에만 사용할 수 있습니다.");
            return;
        }

        // 슬롯 번호가 잘못되었으면 종료
        if (slotIndex < 0 || slotIndex >= itemSlots.Length)
        {
            Debug.LogWarning($"잘못된 아이템 슬롯 번호입니다: {slotIndex}");
            return;
        }

        // 해당 슬롯에 아이템이 없으면 종료
        BattleItemData itemData = itemSlots[slotIndex];

        if (itemData == null || itemData.itemType == BattleItemType.None)
        {
            Debug.Log("해당 슬롯에 사용할 아이템이 없습니다.");
            return;
        }

        // 아이템 효과 실행
        bool itemUsed = ApplyItemEffect(itemData);

        // 효과가 실패했으면 아이템을 소모하지 않음
        if (itemUsed == false)
        {
            return;
        }

        // 사용한 아이템 제거
        itemSlots[slotIndex] = null;

        // 빈칸이 생기면 왼쪽부터 다시 정렬
        CompressItemSlots();

        // 아이템 사용 후 UI 갱신
        RefreshItemSlotUI();

        Debug.Log($"아이템 사용 완료: {itemData.itemName}");
    }

    // <변경부분> 아이템 종류에 따라 실제 효과를 실행하는 함수
    private bool ApplyItemEffect(BattleItemData itemData)
    {
        // 아이템 데이터가 없으면 실패
        if (itemData == null)
        {
            return false;
        }

        switch (itemData.itemType)
        {
            case BattleItemType.ChangeSelectedPieceToJelluPawn:
                return UseChangeSelectedPieceToJelluPawnItem();

            default:
                Debug.LogWarning($"아직 구현되지 않은 아이템 효과입니다: {itemData.itemType}");
                return false;
        }
    }

    // <변경부분> 선택한 플레이어 기물을 젤루 폰으로 변경하는 아이템 효과
    private bool UseChangeSelectedPieceToJelluPawnItem()
    {
        // 선택된 기물이 없으면 실패
        if (selectedPiece == null)
        {
            Debug.Log("젤루 폰으로 변경할 플레이어 기물을 먼저 선택해야 합니다.");
            return false;
        }

        // 플레이어 기물만 아이템 대상으로 허용
        if (selectedPiece.Team != PieceTeam.Player)
        {
            Debug.Log("플레이어 기물에만 아이템을 사용할 수 있습니다.");
            return false;
        }

        // Player King은 현재 승패 조건과 충돌할 수 있으므로 변경 불가
        if (selectedPiece.PieceType == PieceType.King)
        {
            Debug.Log("Player King은 젤루 폰으로 변경할 수 없습니다.");
            return false;
        }

        // 선택 기물을 젤루 폰 정보로 변경
        pieceManager.ChangePieceToJelluPawn(selectedPiece);

        // 아이템 사용 후 흡수 모드 해제
        isAbsorbMode = false;

        // 아이템 사용 후 공격 확인 대상 초기화
        pendingAttackTargetPiece = null;

        // 변경된 기물 기준으로 이동 가능 타일을 다시 표시
        ClearHighlights();
        ShowOnlySelectedPieceTypeIcon(selectedPiece);
        ShowMovableTiles(selectedPiece);

        // 변경된 기물 정보에 맞게 버튼과 스테이터스 UI 갱신
        if (battleUIController != null)
        {
            battleUIController.SetAbsorbModeIcon(false);
            battleUIController.RefreshSelectedPieceButtons(selectedPiece);
        }

        Debug.Log("아이템 효과 성공: 선택한 기물을 젤루 폰으로 변경했습니다.");

        return true;
    }

    // <변경부분> 아이템 사용 후 빈 슬롯을 제거하고 왼쪽부터 다시 채우는 함수
    private void CompressItemSlots()
    {
        BattleItemData[] compressedSlots = new BattleItemData[MaxItemSlotCount];
        int targetIndex = 0;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null || itemSlots[i].itemType == BattleItemType.None)
            {
                continue;
            }

            compressedSlots[targetIndex] = itemSlots[i];
            targetIndex++;
        }

        itemSlots = compressedSlots;
    }

    // <변경부분> 현재 아이템 슬롯 정보를 UI에 반영하는 함수
    private void RefreshItemSlotUI()
    {
        if (battleUIController == null)
        {
            return;
        }

        battleUIController.RefreshItemSlots(itemSlots);
    }

    // <변경부분> 전투 유물을 왼쪽 빈 슬롯부터 추가하는 함수
    public bool AddBattleRelic(BattleRelicData relicData)
    {
        // 추가할 유물 데이터가 없으면 실패
        if (relicData == null || relicData.relicType == BattleRelicType.None)
        {
            Debug.LogWarning("추가할 유물 데이터가 없습니다.");
            return false;
        }

        // <변경부분> 같은 유물은 중복 획득할 수 없음
        if (HasRelic(relicData.relicType))
        {
            Debug.Log($"유물 획득 실패: 이미 보유 중인 유물입니다. / {relicData.relicName}");
            return false;
        }

        // 왼쪽 슬롯부터 빈칸을 찾음
        for (int i = 0; i < relicSlots.Length; i++)
        {
            if (relicSlots[i] != null && relicSlots[i].relicType != BattleRelicType.None)
            {
                continue;
            }

            relicSlots[i] = relicData;

            // 유물 획득 후 슬롯 UI 갱신
            RefreshRelicSlotUI();

            Debug.Log($"유물 획득: {relicData.relicName} / 슬롯 {i}");
            return true;
        }

        Debug.Log("유물 슬롯이 가득 찼습니다.");
        return false;
    }

    // <변경부분> 특정 유물을 현재 보유 중인지 확인하는 함수
    public bool HasRelic(BattleRelicType relicType)
    {
        // None은 실제 유물이 아니므로 보유 판정하지 않음
        if (relicType == BattleRelicType.None)
        {
            return false;
        }

        // 현재 유물 슬롯 전체를 검사
        for (int i = 0; i < relicSlots.Length; i++)
        {
            if (relicSlots[i] == null)
            {
                continue;
            }

            if (relicSlots[i].relicType == relicType)
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분> 테스트 버튼에서 호출하는 테스트 유물 추가 함수
    public void AddTestRelicForDebug()
    {
        // 테스트 유물 데이터가 없으면 추가 불가
        if (testRelicData == null || testRelicData.relicType == BattleRelicType.None)
        {
            Debug.LogWarning("테스트 유물 데이터가 설정되지 않았습니다.");
            return;
        }

        // 테스트 유물을 현재 유물 슬롯에 추가
        AddBattleRelic(testRelicData);
    }

    // <변경부분> 현재 유물 슬롯 정보를 UI에 반영하는 함수
    private void RefreshRelicSlotUI()
    {
        if (battleUIController == null)
        {
            return;
        }

        battleUIController.RefreshRelicSlots(relicSlots);
    }


    // <변경부분> Jellu 폰 고유 스킬: 성공 여부를 bool로 반환
    private bool UseJelluMultiply(Piece piece)
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                int targetX = piece.X + offsetX;
                int targetY = piece.Y + offsetY;

                if (IsInsideBoard(targetX, targetY) == false)
                {
                    continue;
                }

                if (pieceManager.IsEmpty(targetX, targetY))
                {
                    emptyPositions.Add(new Vector2Int(targetX, targetY));
                }
            }
        }

        if (emptyPositions.Count == 0)
        {
            Debug.Log("증식 실패: 인접한 빈칸이 없습니다.");
            return false; // <변경부분> 실패했으므로 쿨타임 없음
        }

        int randomIndex = Random.Range(0, emptyPositions.Count);
        Vector2Int selectedPosition = emptyPositions[randomIndex];

        Piece clonedPiece = pieceManager.ClonePieceTo(
            piece,
            selectedPosition.x,
            selectedPosition.y
        );

        if (clonedPiece != null)
        {
            Debug.Log($"증식 성공: ({selectedPosition.x}, {selectedPosition.y})에 {piece.Team} {piece.PieceType} 생성");
            return true; // <변경부분> 성공했으므로 쿨타임 적용
        }

        return false; // <변경부분> 생성 실패 시 쿨타임 없음
    }

    // 선택한 기물의 종류에 따라 이동 가능한 타일을 표시하는 함수
    private void ShowMovableTiles(Piece piece)
    {
        // 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        //기물 종류별 이동 규칙 분기
        switch (piece.PieceType)
        {
            case PieceType.Pawn:
                // 폰 이동/공격 표시
                ShowPawnMovableTiles(piece);
                break;

            case PieceType.Rook:
                // 룩 이동/공격 표시
                ShowRookMovableTiles(piece);
                break;

            case PieceType.Bishop:
                // 비숍 이동/공격 표시
                ShowBishopMovableTiles(piece);
                break;

            case PieceType.Knight:
                // 나이트 이동/공격 표시
                ShowKnightMovableTiles(piece);
                break;

            case PieceType.King:
                // 킹 이동/공격 표시
                ShowKingMovableTiles(piece);
                break;
        }
    }

    private void ShowRookMovableTiles(Piece piece)
    {
        // 오른쪽
        CheckLineMovement(piece, 1, 0);

        // 왼쪽
        CheckLineMovement(piece, -1, 0);

        // 위쪽
        CheckLineMovement(piece, 0, 1);

        // 아래쪽
        CheckLineMovement(piece, 0, -1);
    }

    //비숍처럼 대각선 방향으로 이동/공격 가능한 타일 표시
    private void ShowBishopMovableTiles(Piece piece)
    {
        // 오른쪽 위 대각선
        CheckLineMovement(piece, 1, 1);

        // 왼쪽 위 대각선
        CheckLineMovement(piece, -1, 1);

        // 오른쪽 아래 대각선
        CheckLineMovement(piece, 1, -1);

        // 왼쪽 아래 대각선
        CheckLineMovement(piece, -1, -1);
    }

    // 한 방향으로 계속 검사하며 이동/공격 가능한 타일을 찾는 함수
    private void CheckLineMovement(Piece piece, int dirX, int dirY)
    {
        // 현재 기물 위치에서 한 칸 이동한 좌표부터 시작
        int checkX = piece.X + dirX;
        int checkY = piece.Y + dirY;

        // 보드 안쪽인 동안 계속 검사
        while (IsInsideBoard(checkX, checkY))
        {
            // 검사 좌표에 있는 기물 확인
            Piece targetPiece = pieceManager.GetPieceAt(checkX, checkY);

            // 기물이 없는 칸이면 이동 가능
            if (targetPiece == null)
            {
                HighlightTile(checkX, checkY);
            }
            else
            {
                // 적대 기물이 있으면 공격 가능
                if (piece.IsEnemyOf(targetPiece))
                {
                    HighlightTile(checkX, checkY);
                }

                // 기물이 있으면 그 뒤로는 더 이상 이동 불가
                break;
            }

            // 같은 방향으로 다음 칸 검사
            checkX += dirX;
            checkY += dirY;
        }
    }

    // 나이트의 L자 이동/공격 가능한 타일 표시
    private void ShowKnightMovableTiles(Piece piece)
    {
        // 나이트가 이동할 수 있는 8개 상대 좌표
        int[,] knightMoves =
        {
        { 1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 }, { -1, -2 }, { -2, -1 }, { -2, 1 }, { -1, 2 }
    };

        // 8개 좌표를 하나씩 검사
        for (int i = 0; i < knightMoves.GetLength(0); i++)
        {
            // 이동할 좌표 계산
            int targetX = piece.X + knightMoves[i, 0];
            int targetY = piece.Y + knightMoves[i, 1];

            // 단일 칸 이동/공격 가능 여부 검사
            CheckSingleTileMovement(piece, targetX, targetY);
        }
    }

    // 킹의 주변 8칸 이동/공격 가능한 타일 표시
    private void ShowKingMovableTiles(Piece piece)
    {
        // 주변 8칸 검사
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // 자기 위치는 제외
                if (x == 0 && y == 0)
                {
                    continue;
                }

                // 이동할 좌표 계산
                int targetX = piece.X + x;
                int targetY = piece.Y + y;

                // 단일 칸 이동/공격 가능 여부 검사
                CheckSingleTileMovement(piece, targetX, targetY);
            }
        }
    }

    // <변경부분> 한 칸짜리 이동/공격 가능 여부를 검사하는 함수
    private void CheckSingleTileMovement(Piece piece, int x, int y)
    {
        // 보드 밖이면 종료
        if (IsInsideBoard(x, y) == false)
        {
            return;
        }

        // 해당 좌표의 기물 확인
        Piece targetPiece = pieceManager.GetPieceAt(x, y);

        // 비어 있는 칸이면 이동 가능
        if (targetPiece == null)
        {
            HighlightTile(x, y);
            return;
        }

        // 적대 기물이 있으면 공격 가능
        if (piece.IsEnemyOf(targetPiece))
        {
            HighlightTile(x, y);
        }
    }

    private void ShowPawnMovableTiles(Piece piece) // Pawn의 이동 가능 타일 표시
    {
        // 플레이어는 위쪽(y + 1), 적은 아래쪽(y - 1)으로 이동
        int direction = piece.Team == PieceTeam.Player ? 1 : -1;

        // 전진 좌표
        int forwardX = piece.X;
        int forwardY = piece.Y + direction;

        // 앞칸이 비어 있으면 이동 가능
        if (IsInsideBoard(forwardX, forwardY) && pieceManager.IsEmpty(forwardX, forwardY))
        {
            HighlightTile(forwardX, forwardY);
        }

        // 왼쪽 대각선 공격 좌표
        CheckPawnAttackTile(piece, piece.X - 1, piece.Y + direction);

        // 오른쪽 대각선 공격 좌표
        CheckPawnAttackTile(piece, piece.X + 1, piece.Y + direction);
    }

    // Pawn이 공격 가능한 대각선 타일인지 확인
    private void CheckPawnAttackTile(Piece piece, int x, int y)
    {
        // 보드 밖이면 종료
        if (IsInsideBoard(x, y) == false)
        {
            return;
        }

        // 해당 좌표의 기물 확인
        Piece targetPiece = pieceManager.GetPieceAt(x, y);

        //공격 판정 확인용 로그
        Debug.Log($"공격 확인 좌표 ({x}, {y}) / 대상: {targetPiece}");

        // 대상 기물이 있고, 적대 관계라면 공격 가능
        if (targetPiece != null && piece.IsEnemyOf(targetPiece))
        {
            Debug.Log($"공격 가능: {targetPiece.Team} / {targetPiece.PieceType}");

            HighlightTile(x, y);
        }
    }

    // <변경부분> 모든 기물 타입 아이콘 표시 상태 전환
    public void ToggleTypeIcons()
    {
        // 타입 아이콘 표시 상태 반전
        isTypeIconVisible = !isTypeIconVisible;

        // 모든 기물의 타입 아이콘 표시 상태 적용
        SetAllTypeIconsVisible(isTypeIconVisible);
    }

    // <변경부분> 보드 위 모든 기물의 타입 아이콘 표시 상태 설정
    private void SetAllTypeIconsVisible(bool isVisible)
    {
        // 보드 전체 X 좌표 검사
        for (int x = 0; x < boardManager.Width; x++)
        {
            // 보드 전체 Y 좌표 검사
            for (int y = 0; y < boardManager.Height; y++)
            {
                // 현재 좌표의 기물 가져오기
                Piece piece = pieceManager.GetPieceAt(x, y);

                // 기물이 없으면 다음 칸 검사
                if (piece == null)
                {
                    continue;
                }

                // 해당 기물의 타입 아이콘 표시 상태 적용
                piece.SetTypeIconVisible(isVisible);
            }
        }
    }

    // <변경부분> 선택한 기물만 타입 아이콘 표시
    private void ShowOnlySelectedPieceTypeIcon(Piece piece)
    {
        // 모든 기물 타입 아이콘 끄기
        SetAllTypeIconsVisible(false);

        // 선택한 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // 선택한 기물의 타입 아이콘만 켜기
        piece.SetTypeIconVisible(true);
    }

    // <변경부분> 찬스어택이 발동한 기물에게 추가 행동 상태를 부여하는 함수
    private void ActivateChanceAttackBonus(Piece piece)
    {
        // 추가 행동을 받을 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // 찬스어택 발동 기물을 추가 행동 기물로 저장
        chanceAttackBonusPiece = piece;

        // 선택 기물을 유지해서 바로 다음 행동을 이어갈 수 있게 처리
        selectedPiece = piece;

        // 기존 이동/공격 하이라이트 제거
        ClearHighlights();

        // 추가 행동 가능한 기물의 타입 아이콘만 표시
        ShowOnlySelectedPieceTypeIcon(selectedPiece);

        // 추가 이동/공격 가능한 타일 표시
        ShowMovableTiles(selectedPiece);

        // <변경부분> 찬스어택 추가 행동 상태에서도 현재 기물 기준으로 흡수/고유스킬 버튼을 다시 갱신
        // 일반 찬스어택과 유물 찬스어택 모두 여기서 처리됨
        if (battleUIController != null)
        {
            battleUIController.RefreshSelectedPieceButtons(selectedPiece);
        }
    }

    // <변경부분> 흡수 성공 시 유물 효과로 찬스어택을 확정 발동할 수 있는지 검사하는 함수
    private bool TryActivateAbsorbChanceAttackRelic(Piece piece)
    {
        // 검사할 기물이 없으면 발동 불가
        if (piece == null)
        {
            return false;
        }

        // 현재 플레이어 턴이 아니면 발동 불가
        if (currentTurn != BattleTurn.Player)
        {
            return false;
        }

        // 해당 유물을 보유하고 있지 않으면 발동 불가
        if (HasRelic(BattleRelicType.AbsorbChanceAttackOncePerTurn) == false)
        {
            return false;
        }

        // 이번 플레이어 턴에 이미 발동했다면 발동 불가
        if (hasUsedAbsorbChanceAttackRelicThisTurn)
        {
            Debug.Log("유물 효과 발동 실패: 이번 턴에 이미 흡수 찬스어택 유물이 발동했습니다.");
            return false;
        }

        // 추가 행동 가능한 이동/공격 타일이 없으면 발동하지 않음
        if (HasAnySelectableTile(piece) == false)
        {
            Debug.Log("유물 효과 발동 실패: 추가 행동 가능한 이동/공격 타일이 없습니다.");
            return false;
        }

        return true;
    }

    // <변경부분> 찬스어택 발동 여부를 행동 시작 전 레벨 기준으로 판정하는 함수
    private bool TryActivateChanceAttack(Piece piece, int skillLevelBeforeAction)
    {
        // 판정할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 행동 시작 전에 찬스어택이 없었다면 이번 처치에서는 발동 불가
        if (skillLevelBeforeAction <= 0)
        {
            Debug.Log("찬스어택 판정 실패: 이번 행동 시작 시점에는 찬스어택이 없었습니다.");
            return false;
        }

        // <변경부분> 찬스어택으로 추가 행동을 받아도 이동/공격할 수 있는 칸이 없으면 발동하지 않음
        if (HasAnySelectableTile(piece) == false)
        {
            Debug.Log("찬스어택 판정 실패: 추가 행동 가능한 이동/공격 타일이 없습니다.");
            return false;
        }

        // 행동 시작 전 레벨 기준으로 기본 발동 확률 계산
        int baseChancePercent = GetChanceAttackPercent(skillLevelBeforeAction);

        // <변경부분> 연속 발동 횟수에 따라 확률을 1/3씩 감소
        float penaltyMultiplier = Mathf.Pow(1f / 3f, chanceAttackContinuousCount);

        // <변경부분> 최종 발동 확률 계산
        float finalChancePercent = baseChancePercent * penaltyMultiplier;

        // 0~100 사이 랜덤값 생성
        float randomValue = Random.Range(0f, 100f);

        // 최종 확률 안에 들어오면 발동 성공
        bool isActivated = randomValue < finalChancePercent;

        Debug.Log($"찬스어택 판정: 행동전 LV.{skillLevelBeforeAction} / 기본확률 {baseChancePercent}% / 연속횟수 {chanceAttackContinuousCount} / 최종확률 {finalChancePercent:F1}% / 랜덤 {randomValue:F1} / 결과 {isActivated}");

        return isActivated;
    }

    // <변경부분> 찬스어택 레벨에 따른 발동 확률을 반환하는 함수
    private int GetChanceAttackPercent(int skillLevel)
    {
        switch (skillLevel)
        {
            case 1:
                return 30;

            case 2:
                return 50;

            case 3:
                return 80;

            default:
                return 0;
        }
    }

    // <변경부분> 현재 위치에서 해당 기물이 이동 또는 공격 가능한 타일이 하나라도 있는지 검사하는 함수
    private bool HasAnySelectableTile(Piece piece)
    {
        // 검사할 기물이 없으면 추가 행동 불가
        if (piece == null)
        {
            return false;
        }

        // 기물 타입별 이동/공격 가능 여부를 실제 하이라이트 없이 검사
        switch (piece.PieceType)
        {
            case PieceType.Pawn:
                return HasAnyPawnSelectableTile(piece);

            case PieceType.Rook:
                return HasAnyLineSelectableTile(piece, 1, 0) ||
                       HasAnyLineSelectableTile(piece, -1, 0) ||
                       HasAnyLineSelectableTile(piece, 0, 1) ||
                       HasAnyLineSelectableTile(piece, 0, -1);

            case PieceType.Bishop:
                return HasAnyLineSelectableTile(piece, 1, 1) ||
                       HasAnyLineSelectableTile(piece, -1, 1) ||
                       HasAnyLineSelectableTile(piece, 1, -1) ||
                       HasAnyLineSelectableTile(piece, -1, -1);

            case PieceType.Knight:
                return HasAnyKnightSelectableTile(piece);

            case PieceType.King:
                return HasAnyKingSelectableTile(piece);

            default:
                return false;
        }
    }

    // <변경부분> Pawn이 현재 위치에서 이동 또는 공격 가능한 타일이 있는지 검사하는 함수
    private bool HasAnyPawnSelectableTile(Piece piece)
    {
        // 플레이어는 위쪽, 적은 아래쪽으로 전진
        int direction = piece.Team == PieceTeam.Player ? 1 : -1;

        // 전진 이동 가능 여부 검사
        int forwardX = piece.X;
        int forwardY = piece.Y + direction;

        if (IsInsideBoard(forwardX, forwardY) && pieceManager.IsEmpty(forwardX, forwardY))
        {
            return true;
        }

        // 왼쪽 대각선 공격 가능 여부 검사
        if (CanAttackTile(piece, piece.X - 1, piece.Y + direction))
        {
            return true;
        }

        // 오른쪽 대각선 공격 가능 여부 검사
        if (CanAttackTile(piece, piece.X + 1, piece.Y + direction))
        {
            return true;
        }

        return false;
    }

    // <변경부분> Rook/Bishop처럼 한 방향으로 계속 이동하는 기물의 이동 또는 공격 가능 여부를 검사하는 함수
    private bool HasAnyLineSelectableTile(Piece piece, int dirX, int dirY)
    {
        // 현재 위치에서 지정 방향으로 한 칸씩 검사
        int checkX = piece.X + dirX;
        int checkY = piece.Y + dirY;

        while (IsInsideBoard(checkX, checkY))
        {
            Piece targetPiece = pieceManager.GetPieceAt(checkX, checkY);

            // 빈칸이면 이동 가능
            if (targetPiece == null)
            {
                return true;
            }

            // 적대 기물이 있으면 공격 가능
            if (piece.IsEnemyOf(targetPiece))
            {
                return true;
            }

            // 같은 편 기물이 막고 있으면 이 방향은 더 이상 진행 불가
            return false;
        }

        return false;
    }

    // <변경부분> Knight가 현재 위치에서 이동 또는 공격 가능한 타일이 있는지 검사하는 함수
    private bool HasAnyKnightSelectableTile(Piece piece)
    {
        int[,] knightMoves =
        {
        { 1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 },
        { -1, -2 }, { -2, -1 }, { -2, 1 }, { -1, 2 }
    };

        for (int i = 0; i < knightMoves.GetLength(0); i++)
        {
            int targetX = piece.X + knightMoves[i, 0];
            int targetY = piece.Y + knightMoves[i, 1];

            if (CanMoveOrAttackTile(piece, targetX, targetY))
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분> King이 현재 위치에서 이동 또는 공격 가능한 타일이 있는지 검사하는 함수
    private bool HasAnyKingSelectableTile(Piece piece)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // 자기 위치는 검사하지 않음
                if (x == 0 && y == 0)
                {
                    continue;
                }

                int targetX = piece.X + x;
                int targetY = piece.Y + y;

                if (CanMoveOrAttackTile(piece, targetX, targetY))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // <변경부분> 특정 좌표가 이동 또는 공격 가능한 타일인지 검사하는 함수
    private bool CanMoveOrAttackTile(Piece piece, int x, int y)
    {
        // 보드 밖이면 불가능
        if (IsInsideBoard(x, y) == false)
        {
            return false;
        }

        Piece targetPiece = pieceManager.GetPieceAt(x, y);

        // 빈칸이면 이동 가능
        if (targetPiece == null)
        {
            return true;
        }

        // 적대 기물이 있으면 공격 가능
        return piece.IsEnemyOf(targetPiece);
    }

    // <변경부분> 특정 좌표에 공격 가능한 기물이 있는지 검사하는 함수
    private bool CanAttackTile(Piece piece, int x, int y)
    {
        // 보드 밖이면 공격 불가
        if (IsInsideBoard(x, y) == false)
        {
            return false;
        }

        Piece targetPiece = pieceManager.GetPieceAt(x, y);

        // 대상 기물이 있고 적대 관계면 공격 가능
        return targetPiece != null && piece.IsEnemyOf(targetPiece);
    }

    // <변경부분> 테스트용 버튼에서 턴을 강제로 넘기는 함수
    public void DebugForceEndTurn()
    {
        // 전투가 이미 끝났으면 턴 변경 불가
        if (isBattleEnded)
        {
            Debug.Log("전투가 종료되어 턴을 넘길 수 없습니다.");
            return;
        }

        // 현재 턴을 강제로 종료
        EndTurn();
    }

    private void EndTurn()
    {
        // <변경부분> 턴 종료 시 이동/공격 가능 타일 하이라이트 제거
        ClearHighlights();

        // <변경부분> 턴 종료 시 공격 확인 대상 초기화
    pendingAttackTargetPiece = null;

        // 선택된 기물 해제
        selectedPiece = null;

        // 흡수 모드 해제
        isAbsorbMode = false;

        // 모든 타입 아이콘 비활성화
        SetAllTypeIconsVisible(false);

        // 턴 종료 시 액션 버튼 숨김
        if (battleUIController != null)
        {
            battleUIController.HideActionButtons();
        }

        if (currentTurn == BattleTurn.Player)
        {
            currentTurn = BattleTurn.Enemy;
        }
        else
        {
            currentTurn = BattleTurn.Player;
            turnCount++;
        }

        // <변경부분> 새 플레이어 턴이 시작되면 흡수 유물 찬스어택 발동 여부 초기화
        if (currentTurn == BattleTurn.Player)
        {
            hasUsedAbsorbChanceAttackRelicThisTurn = false;
        }

        // <변경부분> 턴이 바뀐 뒤 고유 스킬 사용 상태와 쿨타임 갱신
        UpdateAllUniqueSkillTurnState();

        // 턴 변경 후 UI 갱신
        if (turnInfoUIController != null)
        {
            turnInfoUIController.RefreshTurnInfo(turnCount, currentTurn);
        }

        Debug.Log($"턴 변경: Turn {turnCount} / {currentTurn}");
    }

    //  턴 시작 시 모든 기물의 고유 스킬 상태 갱신
    private void UpdateAllUniqueSkillTurnState()
    {
        // 새 턴이 시작되면 턴 전체 고유 스킬 사용권 초기화
        hasUsedUniqueSkillThisTurn = false;

        // 보드 전체 X 좌표 검사
        for (int x = 0; x < boardManager.Width; x++)
        {
            // 보드 전체 Y 좌표 검사
            for (int y = 0; y < boardManager.Height; y++)
            {
                // 현재 좌표의 기물 가져오기
                Piece piece = pieceManager.GetPieceAt(x, y);

                // 기물이 없으면 다음 칸 검사
                if (piece == null)
                {
                    continue;
                }

                // 고유 스킬 쿨타임 1 감소
                piece.ReduceUniqueSkillCooldown();

                // 현재 턴 고유 스킬 사용 여부 초기화
                piece.ResetUniqueSkillTurnUsage();
            }
        }
    }


    // 특정 좌표의 타일을 하이라이트
    private void HighlightTile(int x, int y)
    {
        // 좌표에 해당하는 타일 가져오기
        Tile tile = boardManager.GetTile(x, y);

        // 타일이 없으면 종료
        if (tile == null)
        {
            return;
        }

        // 타일 하이라이트 표시
        tile.ShowHighlight();

        // 나중에 지우기 위해 하이라이트 목록에 저장
        highlightedTiles.Add(tile);

        // 실제 선택 가능한 타일 목록에도 저장
        selectableTiles.Add(tile);
    }

    // 기존 하이라이트 제거
    private void ClearHighlights()
    {
        // 하이라이트된 타일 전부 원래 색으로 복구
        foreach (Tile tile in highlightedTiles)
        {
            tile.HideHighlight();
        }

        // 하이라이트 목록 비우기
        highlightedTiles.Clear();

        // 선택 가능 타일 목록도 비우기
        selectableTiles.Clear();
    }

    // 현재 선택된 기물이 있는지 반환하는 함수
    public bool HasSelectedPiece()
    {
        // 선택된 기물이 있으면 true
        return selectedPiece != null;
    }

    // 클릭한 기물이 현재 턴에 조작 가능한 기물인지 확인하는 함수
    public bool IsCurrentTurnPiece(Piece piece)
    {
        // 기물이 없으면 false
        if (piece == null)
        {
            return false;
        }

        // 플레이어 턴이면 플레이어 기물만 조작 가능
        if (currentTurn == BattleTurn.Player)
        {
            return piece.Team == PieceTeam.Player;
        }

        // 적 턴이면 적 기물만 조작 가능
        if (currentTurn == BattleTurn.Enemy)
        {
            return piece.Team == PieceTeam.Enemy;
        }

        // 그 외에는 false
        return false;
    }

    // 현재 전투 승패 조건을 확인하는 함수
    private void CheckBattleEnd()
    {
        // 이미 전투가 끝났으면 중복 체크 방지
        if (isBattleEnded)
        {
            return;
        }

        // 상대 King이 제거되었거나, King을 제외한 상대 기물이 전멸하면 승리
        if (pieceManager.HasKing(PieceTeam.Enemy) == false ||
            pieceManager.HasAnyNonKingPiece(PieceTeam.Enemy) == false)
        {
            EndBattle(BattleResult.Win);
            return;
        }

        // 플레이어 King이 사망했거나, King을 제외한 플레이어 기물이 전멸하면 패배
        if (pieceManager.HasKing(PieceTeam.Player) == false ||
            pieceManager.HasAnyNonKingPiece(PieceTeam.Player) == false)
        {
            EndBattle(BattleResult.Lose);
            return;
        }
    }

    // 전투를 종료하는 함수
    private void EndBattle(BattleResult result)
    {
        // 전투 종료 상태 저장
        battleResult = result;
        isBattleEnded = true;

        // 선택 해제
        selectedPiece = null;

        // <변경부분> 선택된 기물이 없으므로 액션 버튼 숨김
        if (battleUIController != null)
        {
            battleUIController.HideActionButtons();
        }

        // 하이라이트 제거
        ClearHighlights();

        // 결과 출력
        if (battleResult == BattleResult.Win)
        {
            Debug.Log("전투 승리: 상대 King 제거 또는 상대 기물 전멸");
        }
        else if (battleResult == BattleResult.Lose)
        {
            Debug.Log("일반 전투 패배: 보상 없음 / 받은 피해와 사망 상태 유지");
        }
    }

    // 좌표가 보드 안인지 확인
    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < boardManager.Width && y >= 0 && y < boardManager.Height;
    }
}
