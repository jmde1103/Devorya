using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    //다른 스크립트에서 BattleManager에 접근하기 위한 임시 싱글톤
    public static BattleManager Instance { get; private set; }

    [Header("Manager")]
    [SerializeField] private BoardManager boardManager;
    // 기물 매니저 참조
    [SerializeField] private PieceManager pieceManager;

    // <변경부분> 일반스킬과 스킬 발동 판정을 관리하는 매니저
    [SerializeField] private BattleSkillManager battleSkillManager;

    // <변경부분> 고유스킬 기본 데이터를 관리하는 데이터베이스
    [SerializeField] private UniqueSkillDatabase uniqueSkillDatabase;

    // <변경부분> 전투 아이템 슬롯과 아이템 사용 흐름을 관리하는 매니저
    [SerializeField] private BattleItemManager battleItemManager;

    // <변경부분> 전투 유물 슬롯과 중복 획득 방지를 관리하는 매니저
    [SerializeField] private BattleRelicManager battleRelicManager;


    // <변경부분> 전투 아이템의 실제 효과 실행을 담당하는 핸들러
    [SerializeField] private BattleItemEffectHandler battleItemEffectHandler;

    // <변경부분> 전투 유물의 실제 효과 발동 조건을 판정하는 핸들러
    [SerializeField] private BattleRelicEffectHandler battleRelicEffectHandler;

    // <변경부분> 전투 중 이동/공격 가능 여부를 판정하는 클래스
    [SerializeField] private BattleMoveValidator battleMoveValidator;

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

    // <변경부분> 플레이어 진영 기물이 잡힌 누적 수
    private int playerDeathStackForUniqueSkill = 0;

    // <변경부분> 적 진영 기물이 잡힌 누적 수
    private int enemyDeathStackForUniqueSkill = 0;

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

        // <변경부분> 게임 시작 시 아이템 매니저 초기화
        if (battleItemManager != null)
        {
            battleItemManager.Initialize(this, battleUIController);
        }

        // <변경부분> 게임 시작 시 유물 매니저 초기화
        if (battleRelicManager != null)
        {
            battleRelicManager.Initialize(battleUIController);
        }

        // <변경부분> 게임 시작 시 이동 판정기 초기화
        if (battleMoveValidator != null)
        {
            battleMoveValidator.Initialize(boardManager, pieceManager);
        }

        // < 변경부분 > 게임 시작 시 스킬 매니저 초기화
        if (battleSkillManager != null)
        {
            battleSkillManager.Initialize(boardManager, pieceManager);
        }

        // <변경부분> 게임 시작 시 아이템 효과 핸들러 초기화
        if (battleItemEffectHandler != null)
        {
            battleItemEffectHandler.Initialize(pieceManager);
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

        // <변경부분> 흡수/레벨업이 적용되기 전 ChanceAttack 보유 정보를 복사해서 저장
        // 이번 행동에서 발동 판정은 행동 시작 전 레벨 기준으로 처리
        OwnedGeneralSkillData chanceAttackDataBeforeAction = actingPiece.GetGeneralSkillDataCopy(GeneralSkillType.ChanceAttack);

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

                // <변경부분> 제거될 기물의 소속을 먼저 저장
                PieceTeam absorbedDeadPieceTeam = targetPiece.Team;

                pieceManager.RemovePiece(targetPiece);

                // <변경부분> 해당 진영 기물이 잡힌 스택 증가
                AddDeathStackForUniqueSkill(absorbedDeadPieceTeam);

                isAbsorbMode = false;

                Debug.Log($"흡수 성공: {absorbedType} 데이터를 복사했습니다.");
            }
            else
            {
                // <변경부분> 적대 기물을 제거했으므로 찬스어택 판정 대상으로 저장
                killedEnemyPiece = true;

                // <변경부분> 제거될 기물의 소속을 먼저 저장
                PieceTeam attackedDeadPieceTeam = targetPiece.Team;

                pieceManager.RemovePiece(targetPiece);

                // <변경부분> 해당 진영 기물이 잡힌 스택 증가
                AddDeathStackForUniqueSkill(attackedDeadPieceTeam);
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
        if (killedEnemyPiece &&
        battleSkillManager != null &&
        battleMoveValidator != null &&
        battleMoveValidator.HasAnySelectableTile(actingPiece) &&
        battleSkillManager.TryActivateChanceAttack(actingPiece, chanceAttackDataBeforeAction, chanceAttackContinuousCount))
        {
            // <변경부분> 일반 ChanceAttack 연속 발동 횟수 증가
            chanceAttackContinuousCount++;

            // <변경부분> 일반 ChanceAttack으로 추가 행동 상태를 부여
            ActivateChanceAttackBonus(actingPiece);

            Debug.Log("ChanceAttack 발동: 턴 종료 없이 한 번 더 이동할 수 있습니다.");
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

        // <변경부분> 선택된 고유스킬의 기본 데이터 가져오기
        UniqueSkillData skillData = GetUniqueSkillData(selectedPiece.UniqueSkill);

        // <변경부분> 고유스킬 데이터가 없으면 스킬 사용 불가
        if (skillData == null)
        {
            return;
        }

        // <변경부분> 데이터에서 한 턴 1회 제한이 켜져 있고, 이미 이번 턴에 고유스킬을 사용했다면 사용 불가
        if (skillData.oncePerTurn && hasUsedUniqueSkillThisTurn)
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

        // <변경부분> 데이터에 설정된 사망 스택 조건 확인
        if (HasEnoughDeathStackForUniqueSkill(selectedPiece.Team, skillData.requiredDeathStack) == false)
        {
            Debug.Log($"고유스킬 사용 실패: 사망 스택이 부족합니다. 필요 스택 {skillData.requiredDeathStack}");
            return;
        }

        // <변경부분> 실제 스킬 성공 여부 저장
        bool skillUsed = false;

        // <변경부분> 고유스킬 실제 실행은 BattleSkillManager에 위임
        // JelluMultiply / KingQueenMove 같은 실제 효과는 BattleSkillManager.TryUseUniqueSkill()에서 처리
        if (battleSkillManager == null)
        {
            Debug.LogWarning("BattleSkillManager가 연결되지 않아 고유스킬을 사용할 수 없습니다.");
            return;
        }

        skillUsed = battleSkillManager.TryUseUniqueSkill(selectedPiece);

        // <변경부분> 스킬이 실제로 성공했을 때만 턴 사용권과 쿨타임 적용
        if (skillUsed)
        {
            // <변경부분> 데이터 설정에 따라 한 턴 1회 사용권 소모
            if (skillData.oncePerTurn)
            {
                hasUsedUniqueSkillThisTurn = true;
            }

            // <변경부분> 데이터 설정에 따라 사망 스택 소모
            if (skillData.consumeDeathStackOnUse)
            {
                ConsumeDeathStackForUniqueSkill(selectedPiece.Team, skillData.requiredDeathStack);
            }

            // <변경부분> 선택된 기물에 고유스킬 데이터 기준 쿨타임 적용
            selectedPiece.MarkUniqueSkillUsed(skillData.cooldownTurn);

            // <변경부분> 고유스킬 사용 후 이동 가능 타일을 현재 기물 정보 기준으로 다시 갱신
            ClearHighlights();
            ShowOnlySelectedPieceTypeIcon(selectedPiece);
            ShowMovableTiles(selectedPiece);

            // <변경부분> 고유스킬 사용 후 버튼과 스테이터스 UI를 현재 기물 정보 기준으로 다시 갱신
            if (battleUIController != null)
            {
                battleUIController.RefreshSelectedPieceButtons(selectedPiece);
            }

            Debug.Log($"고유 스킬 사용 완료: {selectedPiece.UniqueSkill} / 쿨타임 {skillData.cooldownTurn}");
        }
    }

    // <변경부분> 특정 고유스킬 타입에 맞는 기본 데이터를 가져오는 함수
    private UniqueSkillData GetUniqueSkillData(UniqueSkillType skillType)
    {
        // 데이터베이스가 연결되지 않았으면 데이터 없음 처리
        if (uniqueSkillDatabase == null)
        {
            Debug.LogWarning("UniqueSkillDatabase가 연결되지 않았습니다.");
            return null;
        }

        // 데이터베이스에서 해당 고유스킬 데이터 검색
        UniqueSkillData skillData = uniqueSkillDatabase.GetData(skillType);

        if (skillData == null)
        {
            Debug.LogWarning($"고유스킬 데이터를 찾지 못했습니다: {skillType}");
        }

        return skillData;
    }

    // <변경부분> 기물이 잡혔을 때 해당 진영의 고유스킬용 사망 스택을 증가시키는 함수
    private void AddDeathStackForUniqueSkill(PieceTeam deadPieceTeam)
    {
        if (deadPieceTeam == PieceTeam.Player)
        {
            playerDeathStackForUniqueSkill++;
            Debug.Log($"Player 고유스킬 사망 스택 증가: {playerDeathStackForUniqueSkill}");
            return;
        }

        if (deadPieceTeam == PieceTeam.Enemy)
        {
            enemyDeathStackForUniqueSkill++;
            Debug.Log($"Enemy 고유스킬 사망 스택 증가: {enemyDeathStackForUniqueSkill}");
            return;
        }
    }

    // <변경부분> 특정 진영이 고유스킬 사용에 필요한 사망 스택을 충분히 가지고 있는지 확인하는 함수
    private bool HasEnoughDeathStackForUniqueSkill(PieceTeam team, int requiredDeathStack)
    {
        // 요구 스택이 0 이하라면 조건 없이 사용 가능
        if (requiredDeathStack <= 0)
        {
            return true;
        }

        if (team == PieceTeam.Player)
        {
            return playerDeathStackForUniqueSkill >= requiredDeathStack;
        }

        if (team == PieceTeam.Enemy)
        {
            return enemyDeathStackForUniqueSkill >= requiredDeathStack;
        }

        return false;
    }

    // <변경부분> 고유스킬 사용 후 필요한 사망 스택을 소모하는 함수
    private void ConsumeDeathStackForUniqueSkill(PieceTeam team, int consumeCount)
    {
        // 소모할 스택이 없으면 처리하지 않음
        if (consumeCount <= 0)
        {
            return;
        }

        if (team == PieceTeam.Player)
        {
            playerDeathStackForUniqueSkill -= consumeCount;

            if (playerDeathStackForUniqueSkill < 0)
            {
                playerDeathStackForUniqueSkill = 0;
            }

            Debug.Log($"Player 고유스킬 사망 스택 소모 후 남은 수: {playerDeathStackForUniqueSkill}");
            return;
        }

        if (team == PieceTeam.Enemy)
        {
            enemyDeathStackForUniqueSkill -= consumeCount;

            if (enemyDeathStackForUniqueSkill < 0)
            {
                enemyDeathStackForUniqueSkill = 0;
            }

            Debug.Log($"Enemy 고유스킬 사망 스택 소모 후 남은 수: {enemyDeathStackForUniqueSkill}");
            return;
        }
    }


    // <변경부분> 외부에서 전투 아이템을 추가할 때 BattleItemManager에 전달하는 함수
    public void AddBattleItem(BattleItemData itemData)
    {
        // 전투 아이템 매니저가 연결되지 않았으면 아이템 추가 불가
        if (battleItemManager == null)
        {
            Debug.LogWarning("BattleItemManager가 연결되지 않았습니다.");
            return;
        }

        // 실제 아이템 슬롯 추가는 BattleItemManager가 처리
        battleItemManager.AddBattleItem(itemData);
    }

    // <변경부분> 테스트 버튼에서 호출하는 테스트 아이템 추가 함수
    public void AddTestItemForDebug()
    {
        // 전투 아이템 매니저가 연결되지 않았으면 테스트 아이템 추가 불가
        if (battleItemManager == null)
        {
            Debug.LogWarning("BattleItemManager가 연결되지 않았습니다.");
            return;
        }

        // 테스트 아이템 추가는 BattleItemManager가 처리
        battleItemManager.AddTestItemForDebug();
    }

    // <변경부분> 아이템 슬롯 클릭 시 BattleItemManager에 아이템 사용 요청
    public void UseItemAtSlot(int slotIndex)
    {
        // 전투 아이템 매니저가 연결되지 않았으면 아이템 사용 불가
        if (battleItemManager == null)
        {
            Debug.LogWarning("BattleItemManager가 연결되지 않았습니다.");
            return;
        }

        // 실제 아이템 슬롯 검사와 소모 처리는 BattleItemManager가 처리
        battleItemManager.UseItemAtSlot(slotIndex);
    }

    // <변경부분> BattleItemManager가 아이템 사용 가능 상태인지 확인할 때 호출하는 함수
    public bool CanUseBattleItem()
    {
        // 전투가 끝났으면 아이템 사용 불가
        if (isBattleEnded)
        {
            return false;
        }

        // 아이템은 플레이어 턴에만 사용 가능
        if (currentTurn != BattleTurn.Player)
        {
            Debug.Log("아이템은 플레이어 턴에만 사용할 수 있습니다.");
            return false;
        }

        return true;
    }

    // <변경부분> BattleItemManager가 실제 아이템 효과 실행을 요청할 때 호출하는 함수
    public bool TryApplyBattleItemEffect(BattleItemData itemData)
    {
        // 아이템 효과 핸들러가 없으면 효과 실행 실패
        if (battleItemEffectHandler == null)
        {
            Debug.LogWarning("BattleItemEffectHandler가 연결되지 않았습니다.");
            return false;
        }

        // <변경부분> 실제 아이템 효과 실행은 BattleItemEffectHandler에 요청
        bool itemUsed = battleItemEffectHandler.TryApplyItemEffect(itemData, selectedPiece);

        // 효과가 실패했으면 후처리하지 않음
        if (itemUsed == false)
        {
            return false;
        }

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

        return true;
    }

    // <변경부분> 외부에서 전투 유물을 추가할 때 BattleRelicManager에 전달하는 함수
    public bool AddBattleRelic(BattleRelicData relicData)
    {
        // 전투 유물 매니저가 연결되지 않았으면 유물 추가 실패
        if (battleRelicManager == null)
        {
            Debug.LogWarning("BattleRelicManager가 연결되지 않았습니다.");
            return false;
        }

        // 실제 유물 슬롯 추가와 중복 검사는 BattleRelicManager가 처리
        return battleRelicManager.AddBattleRelic(relicData);
    }

    // <변경부분> 특정 유물을 현재 보유 중인지 확인하는 함수
    public bool HasRelic(BattleRelicType relicType)
    {
        // 전투 유물 매니저가 연결되지 않았으면 유물 미보유로 처리
        if (battleRelicManager == null)
        {
            return false;
        }

        // 실제 유물 보유 여부 검사는 BattleRelicManager가 처리
        return battleRelicManager.HasRelic(relicType);
    }

    // <변경부분> 테스트 버튼에서 호출하는 테스트 유물 추가 함수
    public void AddTestRelicForDebug()
    {
        // 전투 유물 매니저가 연결되지 않았으면 테스트 유물 추가 불가
        if (battleRelicManager == null)
        {
            Debug.LogWarning("BattleRelicManager가 연결되지 않았습니다.");
            return;
        }

        // 테스트 유물 추가는 BattleRelicManager가 처리
        battleRelicManager.AddTestRelicForDebug();
    }

    // 선택한 기물의 종류에 따라 이동 가능한 타일을 표시하는 함수
    private void ShowMovableTiles(Piece piece)
    {
        // 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        // <변경부분> 실제 기물 타입이 아니라 현재 이동 판정 타입 기준으로 이동 하이라이트 표시
        switch (piece.GetCurrentMoveType())
        {
            case PieceType.Pawn:
                ShowPawnMovableTiles(piece);
                break;

            case PieceType.Rook:
                ShowRookMovableTiles(piece);
                break;

            case PieceType.Bishop:
                ShowBishopMovableTiles(piece);
                break;

            case PieceType.Knight:
                ShowKnightMovableTiles(piece);
                break;

            case PieceType.King:
                ShowKingMovableTiles(piece);
                break;

            // <변경부분> Queen은 Rook + Bishop 이동 방식을 모두 사용
            case PieceType.Queen:
                ShowQueenMovableTiles(piece);
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

    // <변경부분> Queen처럼 직선 + 대각선 방향으로 이동/공격 가능한 타일 표시
    private void ShowQueenMovableTiles(Piece piece)
    {
        // Queen은 Rook 이동 방식 사용
        ShowRookMovableTiles(piece);

        // Queen은 Bishop 이동 방식 사용
        ShowBishopMovableTiles(piece);
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
        // 유물 효과 핸들러가 없으면 유물 효과 발동 불가
        if (battleRelicEffectHandler == null)
        {
            Debug.LogWarning("BattleRelicEffectHandler가 연결되지 않았습니다.");
            return false;
        }

        // <변경부분> 유물 효과 발동에 필요한 현재 전투 상태를 계산
        bool hasRelic = HasRelic(BattleRelicType.AbsorbChanceAttackOncePerTurn);
        bool hasAnySelectableTile = battleMoveValidator != null && battleMoveValidator.HasAnySelectableTile(piece);

        // <변경부분> 실제 유물 효과 발동 조건 판정은 BattleRelicEffectHandler에 요청
        return battleRelicEffectHandler.CanActivateAbsorbChanceAttackRelic(
            piece,
            currentTurn,
            hasRelic,
            hasUsedAbsorbChanceAttackRelicThisTurn,
            hasAnySelectableTile
        );
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

        // <변경부분> 턴이 끝나는 진영의 임시 이동 타입 초기화
        // currentTurn이 바뀌기 전에 실행해야 정확히 이번 턴 주체의 임시 이동 타입이 제거됨
        if (pieceManager != null)
        {
            PieceTeam endingTeam = currentTurn == BattleTurn.Player ? PieceTeam.Player : PieceTeam.Enemy;
            pieceManager.ClearTemporaryMoveTypes(endingTeam);
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

    // <변경부분> 턴 시작 시 현재 턴 진영 기물의 고유 스킬 상태만 갱신
    private void UpdateAllUniqueSkillTurnState()
    {
        // 새 턴이 시작되면 턴 전체 고유 스킬 사용권 초기화
        hasUsedUniqueSkillThisTurn = false;

        // <변경부분> 현재 턴 주체 진영 계산
        PieceTeam currentTurnTeam = currentTurn == BattleTurn.Player ? PieceTeam.Player : PieceTeam.Enemy;

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

                // <변경부분> 현재 턴 진영의 기물만 갱신
                if (piece.Team != currentTurnTeam)
                {
                    continue;
                }

                // <변경부분> 현재 턴 진영 기물의 고유 스킬 쿨타임만 1 감소
                piece.ReduceUniqueSkillCooldown();

                // <변경부분> 현재 턴 진영 기물의 이번 턴 고유 스킬 사용 여부 초기화
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
