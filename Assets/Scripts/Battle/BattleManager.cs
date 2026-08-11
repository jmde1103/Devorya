using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    // <변경부분> 전투 종료 후 보상 정산 / 맵 복귀 흐름을 담당하는 컨트롤러
    [SerializeField] private BattleEndFlowController battleEndFlowController;


    // <변경부분> 전투 아이템의 실제 효과 실행을 담당하는 핸들러
    [SerializeField] private BattleItemEffectHandler battleItemEffectHandler;

    // <변경부분> 전투 유물의 실제 효과 발동 조건을 판정하는 핸들러
    [SerializeField] private BattleRelicEffectHandler battleRelicEffectHandler;

    // <변경부분> 전투 중 이동/공격 가능 여부를 판정하는 클래스
    [SerializeField] private BattleMoveValidator battleMoveValidator;

    // <변경부분> Enemy 턴의 AI 진행과 행동 선택을 관리하는 컴포넌트
    [SerializeField]
    private BattleAIManager battleAIManager;

    // <변경부분> 기물 선택 포커스와
    // 마지막 기물 공격 연출을 담당하는 카메라 컨트롤러
    [SerializeField]
    private PixelCameraController pixelCameraController;

    [Header("Event Sequence")]
    // <변경부분> 튜토리얼 / 이벤트가 활성화된 경우에만
    // 기존 Battle 입력을 제한하기 위한 별도 이벤트 컨트롤러
    //
    // 연결하지 않거나 컴포넌트를 비활성화하면
    // 기존 일반 전투에는 아무 영향도 주지 않는다.
    [SerializeField]
    private EventSequenceController eventSequenceController;

    // <변경부분> AI의 합법적인 이동 및 공격 후보를 생성하는 클래스
    // 일반 C# 클래스이므로 GameObject에 부착하지 않고 BattleManager가 직접 생성한다.
    private BattleAIActionGenerator battleAIActionGenerator;

    // <변경부분> AI 행동 후보 생성 시 반복해서 재사용하는 목록
    // 후보를 생성할 때마다 새로운 List를 만들지 않아 불필요한 GC 할당을 줄인다.
    private readonly List<BattleAIAction> battleAIActionCandidates =
        new List<BattleAIAction>();

    // 현재 선택된 기물
    private Piece selectedPiece;

    [Header("Player Deployment")]
    // <변경부분> 전투 시작 전에 플레이어 기물의 위치를 정하는
    // 초기 배치 단계를 사용할지 여부
    [SerializeField]
    private bool usePlayerDeploymentPhase = true;

    // <변경부분> 플레이어 배치에 사용할 시작 구역의 세로 칸 수
    // 5x6 보드에서는 아래쪽 2줄인 Y = 0, 1을 사용한다.
    [SerializeField, Min(1)]
    private int playerDeploymentRowCount = 2;

    // <변경부분> 현재 플레이어 초기 배치 단계가 진행 중인지 확인
    private bool isPlayerDeploymentPhase = false;

    // <변경부분> 초기 배치 단계에서 현재 선택한 플레이어 기물
    private Piece selectedDeploymentPiece = null;

    // <변경부분> 이동 또는 공격 실행 전에
    // 첫 번째 클릭으로 확인한 타일
    private Tile pendingActionTile = null;

    // <변경부분> 공격 전에 한 번 확인한 상대 기물
    // 타입 아이콘과 상대 스테이터스 UI 표시에도 사용한다.
    private Piece pendingAttackTargetPiece =
        null;

    // 타입 아이콘 위치에 필드 흡수 버튼이
    // 현재 표시되어 있는 상대 기물
    private Piece fieldAbsorbTargetPiece =
        null;

    // 현재 전투 턴 주체
    [SerializeField] private BattleTurn currentTurn = BattleTurn.Player;
    //현재 전투 결과 상태
    private BattleResult battleResult = BattleResult.None;

    // <변경부분> 현재 전투 턴 번호
    [SerializeField] private int turnCount = 1;

    // <변경부분> 현재 전투에서 사용할 플레이어 진영 패배 조건
    // StageBattleData가 BattleSetupManager를 통해 SetBattleEndCondition()으로 덮어쓴다.
    // Inspector 하드코딩을 막기 위해 SerializeField는 사용하지 않는다.
    private BattleDefeatConditionType playerDefeatCondition =
        BattleDefeatConditionType.KingDeath | BattleDefeatConditionType.AllNonKingPiecesDead;

    // <변경부분> 현재 전투에서 사용할 적 진영 패배 조건
    // StageBattleData가 없는 테스트 상황을 대비한 기본값만 유지한다.
    private BattleDefeatConditionType enemyDefeatCondition =
        BattleDefeatConditionType.AllPiecesDead | BattleDefeatConditionType.NoActionablePieces;



    // <변경부분> 기물 타입 아이콘 표시 상태
    private bool isTypeIconVisible = false;
    // 흡수 모드가 켜져 있는지 여부
    private bool isAbsorbMode = false;
    // 전투가 끝났는지 여부
    private bool isBattleEnded = false;

    // <변경부분> 이동/공격 연출이 진행 중인지 확인하는 값
    // 연출 중 추가 클릭으로 전투 로직이 중복 실행되는 것을 방지
    private bool isActionAnimating = false;
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

    // <변경부분> 이번 전투에서 플레이어가 흡수에 성공한 적 기물 수
    private int playerAbsorbCountThisBattle = 0;
    
    [Header("UI")]
    [SerializeField] private BattleUIController battleUIController;
    [SerializeField] private Button surrenderButton;

    // <변경부분> 기물 타입 아이콘 표시 버튼
    [SerializeField] private Button typeIconButton;

    [SerializeField] private TurnInfoUIController turnInfoUIController;


    // <변경부분> AI 매니저가 현재 턴을 확인할 수 있도록 공개한다.
    public BattleTurn CurrentTurn
    {
        get { return currentTurn; }
    }

    // <변경부분> AI 매니저가 전투 종료 상태를 확인할 수 있도록 공개한다.
    public bool IsBattleEnded
    {
        get { return isBattleEnded; }
    }

    // <변경부분> AI 매니저가 다른 행동 연출 진행 여부를 확인할 수 있도록 공개한다.
    public bool IsActionAnimating
    {
        get { return isActionAnimating; }
    }

    // <변경부분> Enemy AI가 현재 Event Sequence 때문에
    // 자동 행동을 잠시 멈춰야 하는지 반환한다.
    //
    // EventSequenceController가 없거나,
    // 현재 Sequence가 AI 정지를 사용하지 않으면 false다.
    public bool ShouldPauseEnemyAIForEvent
    {
        get
        {
            return
                eventSequenceController != null &&
                eventSequenceController.ShouldPauseEnemyAI;
        }
    }

    // <변경부분> BattleUIController가 흡수 버튼을
    // 배치 완료 체크 버튼으로 사용할지 확인할 수 있도록 공개한다.
    public bool IsPlayerDeploymentPhase
    {
        get { return isPlayerDeploymentPhase; }
    }

    // <변경부분> 현재 ChanceAttack 추가 행동을 받아
    // 같은 턴에 다시 행동해야 하는 기물을 반환한다.
    // 추가 행동 상태가 아니라면 null을 반환한다.
    public Piece GetChanceAttackBonusPiece()
    {
        return chanceAttackBonusPiece;
    }

    // 오브젝트 생성 시 한 번 실행
    private void Awake()
    {
        // 싱글톤 등록
        Instance = this;
    }

    private void Start()
    {
        // <변경부분> EventSequenceController가 Inspector에
        // 연결되지 않은 경우 현재 씬에서 한 번 자동으로 찾는다.
        //
        // 일반 Battle Scene처럼 EventSequenceController가 없으면
        // null 상태로 유지되어 기존 Battle 흐름에 영향이 없다.
        if (eventSequenceController == null)
        {
            eventSequenceController =
                FindObjectOfType<EventSequenceController>();
        }

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
            typeIconButton.onClick.AddListener(
                ToggleTypeIcons
            );
        }

        // <변경부분> 카메라 컨트롤러가 Inspector에 연결되지 않았다면
        // 현재 씬의 PixelCameraController를 자동으로 찾는다.
        if (pixelCameraController == null)
        {
            pixelCameraController =
                FindObjectOfType<PixelCameraController>();
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
            battleMoveValidator.Initialize(
                boardManager,
                pieceManager
            );
        }


        
        // <변경부분> 공용 이동 판정기 초기화가 끝난 뒤
        // AI 행동 후보 생성기를 일반 C# 객체로 생성한다.
        if (boardManager != null &&
            pieceManager != null &&
            battleMoveValidator != null)
        {
            battleAIActionGenerator =
                new BattleAIActionGenerator(
                    boardManager,
                    pieceManager,
                    battleMoveValidator
                );
        }
        else
        {
            Debug.LogWarning(
                "AI 행동 후보 생성기 초기화 실패: " +
                "BoardManager, PieceManager 또는 BattleMoveValidator가 연결되지 않았습니다."
            );
        }

        // <변경부분> Enemy 턴 진행을 담당할 AI 매니저 초기화
        if (battleAIManager != null)
        {
            battleAIManager.Initialize(this);
        }
        else
        {
            Debug.LogWarning(
                "BattleAIManager가 연결되지 않았습니다. " +
                "Enemy 진영은 기존 수동 조작 상태로 유지됩니다."
            );
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

        // <변경부분> BattleSetupManager의 기물 생성이 끝난 다음 프레임에
        // 플레이어 초기 배치 단계를 시작한다.
        //
        // 배치 단계를 사용하지 않는 경우에는
        // 기존처럼 정상 전투 턴을 바로 시작한다.
        StartCoroutine(
            BeginPlayerDeploymentAfterSetupRoutine()
        );
    }

    private void Update()
    {
        // <변경부분> Dialogue 진행 중이거나
        // ForcePieceSelect / ForceTileSelect가 진행 중일 때는
        // 튜토리얼에서 요구하지 않은 Space / Q / S 등의
        // Battle 단축키 입력을 받지 않는다.
        if (eventSequenceController != null &&
            (
                eventSequenceController
                    .IsDialogueBlockingBattleInput ||
                eventSequenceController
                    .IsForcedBattleInputActive
            ))
        {
            return;
        }

        // <변경부분> 플레이어 초기 배치 단계에서는
        // 턴 종료, 흡수, 고유스킬 등의 전투 단축키를 받지 않는다.
        if (isPlayerDeploymentPhase)
        {
            return;
        }

        // Space 키를 누르면 턴 종료
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
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

        // <변경부분> F8 키를 누르면 Enemy 진영의
        // 현재 합법적인 AI 행동 후보를 Console에 출력한다.
        // AI 후보 검증이 끝나면 이 단축키 코드는 제거할 예정이다.
        if (Input.GetKeyDown(KeyCode.F8))
        {
            DebugGenerateEnemyAIActions();
        }
    }




    // 기물을 선택하는 함수
    public void SelectPiece(Piece piece)
    {
        // <변경부분> 튜토리얼 / 이벤트가 특정 기물 선택을
        // 강제하고 있다면 허용된 기물 외의 클릭은
        // 기존 Battle 선택 로직에 전달하지 않는다.
        //
        // EventSequenceController가 없거나
        // 현재 선택 제한 단계가 아니라면 기존과 동일하게 통과한다.
        if (eventSequenceController != null &&
            eventSequenceController.CanSelectPiece(
                piece) ==
            false)
        {
            return;
        }

        // <변경부분> 새 선택 처리 전에 기존 선택 기물을 저장
        Piece previousSelectedPiece =
            selectedPiece;

        // 다른 기물을 선택하거나 선택을 해제하면
        // 기존 상대 기물 위 흡수 버튼을 즉시 초기화한다.
        ClearFieldAbsorbOpportunity();

        // 전투가 종료되었다면 더 이상 선택 불가
        if (isBattleEnded)
        {
            return;
        }

        // <변경부분> 기물을 클릭하면 기물 Transform이 아니라
        // 현재 기물이 올라가 있는 타일 중심으로 카메라를 이동시킨다.
        //
        // Select, 점프, 공격 애니메이션으로 기물 높이가 변해도
        // 카메라 중심은 고정된 타일 좌표를 유지한다.
        if (piece != null &&
            pixelCameraController != null &&
            boardManager != null)
        {
            Tile pieceTile =
                boardManager.GetTile(
                    piece.X,
                    piece.Y
                );

            if (pieceTile != null)
            {
                pixelCameraController.FocusOnTile(
                    pieceTile.transform
                );
            }
        }

        // <변경부분> 초기 배치 단계의 기물 선택은
        // 일반 전투 선택 로직과 완전히 분리한다.
        if (isPlayerDeploymentPhase)
        {
            HandleDeploymentPieceSelection(
                piece
            );

            return;
        }

        // 이전 이동·공격 하이라이트와
        // 첫 번째 클릭으로 확인 중이던 타일을 초기화
        ClearHighlights();

        // 선택한 기물이 없으면 종료
        if (piece == null)
        {
            // <변경부분> 기존 선택 기물이 있었다면 Down 후 Idle로 전환
            if (previousSelectedPiece != null)
            {
                pieceManager.PlayPieceDeselectAnimation(previousSelectedPiece);
            }

            selectedPiece = null;

            pendingAttackTargetPiece = null;

            RefreshTypeIconVisuals();

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

            // 선택된 기물이 없으므로 액션 버튼 숨김
            if (battleUIController != null)
            {
                battleUIController.HideActionButtons();
            }

            // <변경부분> 선택은 해제하지만
            // 전체 타입 아이콘 토글 상태는 유지한다.
            RefreshTypeIconVisuals();
            return;
        }

        // 현재 플레이어 턴인데 플레이어 기물이 아니면 선택 불가
        if (currentTurn == BattleTurn.Player && piece.Team != PieceTeam.Player)
        {
            Debug.Log("상대 기물 정보를 확인합니다.");

            // <변경부분> 상대 기물을 클릭해 선택이 해제되는 경우, 기존 선택 기물은 Down 후 Idle로 전환
            if (previousSelectedPiece != null)
            {
                pieceManager.PlayPieceDeselectAnimation(previousSelectedPiece);
            }

            // <변경부분> 플레이어 턴에 상대 기물을 클릭하면 오른쪽 상단 스테이터스 UI에 표시
            if (battleUIController != null)
            {
                battleUIController.RefreshEnemyStatus(piece);
            }

            selectedPiece = null;

            // <변경부분> 클릭한 상대 기물을
            // 정보 확인용 타입 아이콘 표시 대상으로 저장한다.
            pendingAttackTargetPiece =
                piece;

            // 전체 토글이 켜져 있으면 모든 아이콘을 유지하고,
            // 꺼져 있으면 확인 중인 상대 아이콘만 표시한다.
            RefreshTypeIconVisuals();

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

        // <변경부분> 다른 아군 기물을 새로 선택한 경우, 이전 선택 기물은 Down 후 Idle로 전환
        if (previousSelectedPiece != null && previousSelectedPiece != piece)
        {
            pieceManager.PlayPieceDeselectAnimation(previousSelectedPiece);
        }

        // 현재 기물 선택
        selectedPiece = piece;

        // <변경부분> 현재 턴에 조작 가능한 기물을 선택한 경우에만 Select → Select_Idle 재생
        // 상대 기물 정보 확인 클릭에서는 이 코드까지 오지 않으므로 Select가 실행되지 않는다.
        if (previousSelectedPiece != selectedPiece)
        {
            pieceManager.PlayPieceSelectAnimation(selectedPiece);
        }

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

        // <변경부분> 전체 토글 상태를 유지하면서
        // 선택 기물 아이콘에는 상승과 점멸 연출을 적용한다.
        RefreshTypeIconVisuals();

        // 이동 가능 타일 표시
        ShowMovableTiles(
            selectedPiece
        );

        // <변경부분> 이벤트 시스템이 특정 기물 선택을
        // 기다리고 있었다면 실제 Battle 선택이 성공한 뒤
        // 해당 ForcePieceSelect Step 완료를 통지한다.
        if (eventSequenceController != null)
        {
            eventSequenceController.NotifyPieceSelected(
                selectedPiece
            );
        }

        // 선택 확인용 로그
        Debug.Log(
            $"선택됨: " +
            $"{piece.Team} / " +
            $"{piece.PieceType} / " +
            $"({piece.X}, {piece.Y})"
        );
    }

    // 현재 선택 상태에서
    // 필드 흡수 버튼을 표시할 수 있는 대상인지 확인한다.
    //
    // Player 기물 → 일반 Enemy:
    // 기존처럼 흡수 가능.
    //
    // Player King → Enemy King:
    // King끼리일 때만 Enemy King 흡수를 허용한다.
    //
    // 일반 Player 기물 → Enemy King:
    // 기존처럼 흡수할 수 없다.
    private bool CanShowFieldAbsorbOpportunity(
        Piece targetPiece)
    {
        if (selectedPiece == null ||
            targetPiece == null)
        {
            return false;
        }

        if (selectedPiece.Team !=
            PieceTeam.Player)
        {
            return false;
        }

        if (targetPiece.Team !=
            PieceTeam.Enemy)
        {
            return false;
        }

        // Enemy King은 Player King만 흡수할 수 있다.
        if (targetPiece.PieceType ==
            PieceType.King)
        {
            return
                selectedPiece.PieceType ==
                PieceType.King;
        }

        return true;
    }

    // 공격 가능한 상대 기물 위에
    // OFF 상태의 필드 흡수 버튼을 표시한다.
    private void ShowFieldAbsorbOpportunity(
        Piece targetPiece)
    {
        // 이전 대상, 이전 흡수 모드와 버튼을 먼저 초기화한다.
        ClearFieldAbsorbOpportunity();

        if (CanShowFieldAbsorbOpportunity(
                targetPiece) ==
            false)
        {
            return;
        }

        fieldAbsorbTargetPiece =
            targetPiece;

        targetPiece.ShowFieldAbsorbButton(
            HandleFieldAbsorbModeChanged,
            battleUIController
        );
    }

    // 현재 표시 중인 필드 흡수 버튼을 숨긴다.
    //
    // resetAbsorbMode가 true면
    // 전역 흡수 모드도 함께 OFF로 초기화한다.
    //
    // 실제 빨간 타일 재클릭으로 공격을 시작할 때만
    // false를 전달해서 실행 직전까지 흡수 모드를 유지한다.
    private void ClearFieldAbsorbOpportunity(
        bool resetAbsorbMode = true)
    {
        if (fieldAbsorbTargetPiece != null)
        {
            fieldAbsorbTargetPiece
                .HideFieldAbsorbButton();
        }

        fieldAbsorbTargetPiece =
            null;

        if (resetAbsorbMode == false)
        {
            return;
        }

        isAbsorbMode =
            false;

        // 하단 흡수 버튼은 숨겨져 있지만
        // 혹시 남아 있는 ON 스프라이트 상태도 함께 초기화한다.
        if (battleUIController != null)
        {
            battleUIController.SetAbsorbModeIcon(
                false
            );
        }
    }

    // 필드 흡수 버튼의 OFF / ON 상태 변경을 전달받는다.
    //
    // 이 함수에서는 공격을 실행하지 않는다.
    // 실제 공격은 빨간 타일을 다시 클릭했을 때 실행한다.
    private void HandleFieldAbsorbModeChanged(
        bool isActive)
    {
        Piece targetPiece =
            fieldAbsorbTargetPiece;

        if (selectedPiece == null ||
            targetPiece == null ||
            pendingActionTile == null)
        {
            ClearFieldAbsorbOpportunity();
            return;
        }

        if (isBattleEnded ||
            isActionAnimating ||
            isPlayerDeploymentPhase ||
            currentTurn != BattleTurn.Player)
        {
            ClearFieldAbsorbOpportunity();
            return;
        }

        // 현재 빨간 타일의 공격 대상과
        // 필드 버튼 대상이 동일해야 한다.
        if (pendingAttackTargetPiece !=
            targetPiece)
        {
            ClearFieldAbsorbOpportunity();
            return;
        }

        if (selectableTiles.Contains(
                pendingActionTile) ==
            false)
        {
            ClearFieldAbsorbOpportunity();
            return;
        }

        // 버튼이 표시된 뒤 대상 기물이 이동하거나
        // 제거되지 않았는지 다시 확인한다.
        Piece currentTargetPiece =
            pieceManager.GetPieceAt(
                pendingActionTile.X,
                pendingActionTile.Y
            );

        if (currentTargetPiece !=
            targetPiece)
        {
            ClearFieldAbsorbOpportunity();
            return;
        }

        // OFF / ON 상태만 적용한다.
        //
        // 이 시점에는 공격 코루틴을 실행하지 않는다.
        isAbsorbMode =
            isActive;

        Debug.Log(
            isAbsorbMode
                ? "필드 흡수 모드 ON: 빨간 타일을 다시 클릭하면 흡수합니다."
                : "필드 흡수 모드 OFF: 빨간 타일을 다시 클릭하면 일반 공격합니다."
        );
    }

    public void SelectTile(Tile tile)
    {
        // <변경부분> 튜토리얼 / 이벤트가 특정 타일 입력을
        // 강제하고 있다면 지정 타일 외의 클릭은
        // 기존 Battle 로직에 전달하지 않는다.
        //
        // ForcePieceSelect 중에도 타일 클릭을 차단하여
        // 지정 기물 선택 외의 행동이 발생하지 않도록 한다.
        if (eventSequenceController != null &&
            eventSequenceController.CanSelectTile(
                tile) ==
            false)
        {
            return;
        }

        // 전투가 끝났으면 타일 선택 불가
        if (isBattleEnded)
        {
            return;
        }

        // 클릭한 타일이 없으면 처리할 수 없다.
        if (tile == null)
        {
            return;
        }

        // <변경부분> 초기 배치 단계에서는 일반 이동·공격 판정을 하지 않고
        // 플레이어 기물 선택, 빈칸 이동, 기물 위치 교환만 처리한다.
        if (isPlayerDeploymentPhase)
        {
            HandleDeploymentTileSelection(
                tile
            );

            return;
        }

        // Enemy AI 턴에는 사람의 타일 클릭을 받지 않는다.
        // AI가 꺼져 있으면 Enemy 수동 조작은 허용한다.
        if (currentTurn == BattleTurn.Enemy &&
            battleAIManager != null &&
            battleAIManager.IsEnemyControlledByAI())
        {
            return;
        }

        // 이동·공격 연출 중에는 추가 입력 방지
        if (isActionAnimating)
        {
            return;
        }

        // 클릭한 타일 위에 있는 기물 확인
        Piece clickedPiece =
            pieceManager.GetPieceAt(
                tile.X,
                tile.Y
            );

        // 선택된 기물이 없는 상태에서 기물이 있는 타일을 클릭하면
        // 해당 기물 선택 또는 상대 정보 확인으로 처리한다.
        if (selectedPiece == null)
        {
            if (clickedPiece != null)
            {
                SelectPiece(
                    clickedPiece
                );
            }

            return;
        }
        // 선택된 기물이 있는 상태에서
        // 같은 팀 기물을 클릭하면 새 기물 선택으로 처리한다.
        if (clickedPiece != null &&
            clickedPiece.Team ==
            selectedPiece.Team)
        {
            SelectPiece(
                clickedPiece
            );

            return;
        }

        // <변경부분> 선택된 아군 기물의 공격 범위 밖에 있는
        // 상대 또는 중립 기물을 클릭하면 전투 행동은 실행하지 않고
        // 해당 기물의 스테이터스 정보만 표시한다.
        //
        // 기존 선택 기물과 이동 가능 타일 하이라이트는 유지하므로,
        // 상대 정보를 확인한 뒤 다시 이동 또는 공격을 이어갈 수 있다.
        if (clickedPiece != null &&
    clickedPiece.Team !=
        selectedPiece.Team &&
    selectableTiles.Contains(tile) ==
        false)
        {
            // 공격 범위 밖 상대는 정보 확인만 가능하므로
            // 기존 필드 흡수 버튼을 닫는다.
            ClearFieldAbsorbOpportunity();
            // 이전에 이동 또는 공격 타일을 한 번 확인 중이었다면
            // 확인 전용 색상을 일반 이동 가능 색상으로 되돌린다.
            if (pendingActionTile != null)
            {
                pendingActionTile
                    .ShowHighlight();

                pendingActionTile =
                    null;
            }

            // 클릭한 상대 기물을 정보 확인 대상으로 저장한다.
            // 실제 공격 대상으로 확정된 것은 아니며,
            // 스테이터스 UI와 타입 아이콘 표시에만 사용한다.
            pendingAttackTargetPiece =
                clickedPiece;

            // 선택한 상대 또는 중립 기물의 스테이터스 UI를 표시한다.
            if (battleUIController != null)
            {
                battleUIController
                    .RefreshEnemyStatus(
                        clickedPiece
                    );
            }

            // 전체 타입 아이콘 토글 상태를 유지하면서
            // 현재 정보 확인 중인 상대 기물의 아이콘을 표시한다.
            RefreshTypeIconVisuals();

            Debug.Log(
                $"상대 기물 정보 확인: " +
                $"{clickedPiece.Team} " +
                $"{clickedPiece.PieceType} / " +
                $"({clickedPiece.X}, {clickedPiece.Y})"
            );

            return;
        }

        // <변경부분> 빈칸이거나 상대 기물이 없는 타일이면서
        // 실제 이동·공격 가능 타일도 아니라면 아무 행동도 하지 않는다.
        if (selectableTiles.Contains(tile) ==
       false)
        {
            // 빈 타일이나 다른 위치를 클릭하면
            // 기존 필드 흡수 버튼을 초기화한다.
            ClearFieldAbsorbOpportunity();

            return;
        }

        if (pendingActionTile == tile)
        {
            // 필드 버튼 UI는 닫되
            // 현재 ON 상태인 isAbsorbMode는 행동 판정까지 유지한다.
            //
            // OFF 상태였다면 일반 공격,
            // ON 상태였다면 기존 흡수 공격으로 실행된다.
            ClearFieldAbsorbOpportunity(
                false
            );

            ClearHighlightsExcept(
                tile
            );

            bool actionStarted =
     TryExecuteBattleAction(
         selectedPiece,
         new Vector2Int(
             tile.X,
             tile.Y
         )
     );

            // <변경부분> ForceTileSelect가 같은 타일의
            // 두 번째 확인 클릭을 기다리고 있었다면,
            // 실제 Battle 행동 시작이 승인된 경우에만 Step을 완료한다.
            if (actionStarted &&
                eventSequenceController != null)
            {
                eventSequenceController.NotifyTileSelected(
                    tile
                );
            }

            if (actionStarted == false)
            {
                // 행동 실행에 실패했다면
                // 흡수 모드와 모든 확인 상태를 완전히 초기화한다.
                isAbsorbMode =
                    false;

                pendingActionTile =
                    null;

                pendingAttackTargetPiece =
                    null;

                ClearHighlights();
                RefreshTypeIconVisuals();
            }

            return;
        }

        // <변경부분> 다른 타일을 첫 번째로 클릭했다면
        // 이전 확인 타일은 일반 이동 가능 색상으로 복구한다.
        if (pendingActionTile != null)
        {
            pendingActionTile
                .ShowHighlight();
        }

        // 새 확인 타일 저장
        pendingActionTile =
            tile;

        // 새 확인 타일을 전용 확인 색상으로 변경
        pendingActionTile
            .ShowActionConfirmHighlight();

        // <변경부분> ForceTileSelect가 현재 이 좌표의
        // 첫 번째 확인 클릭을 기다리고 있었다면
        // 실제 Battle 타일 확인까지 성공한 시점에서 Step 완료를 통지한다.
        if (eventSequenceController != null)
        {
            eventSequenceController.NotifyTileSelected(
                tile
            );
        }

        // <변경부분> 상대 또는 중립 기물이 있는 타일이라면
        // 공격 확인 대상으로 저장하고 상대 정보를 표시한다.
        if (clickedPiece != null &&
    clickedPiece.Team !=
    selectedPiece.Team)
        {
            pendingAttackTargetPiece =
                clickedPiece;

            // 공격 가능한 상대 기물의 타입 아이콘을 숨기고
            // 같은 위치에 필드 흡수 버튼을 표시한다.
            ShowFieldAbsorbOpportunity(
                clickedPiece
            );

            // 상대 스테이터스 UI 표시
            if (battleUIController != null)
            {
                battleUIController
                    .RefreshEnemyStatus(
                        clickedPiece
                    );
            }

            Debug.Log(
                "공격 타일 확인: " +
                "같은 타일을 다시 누르면 일반 공격, " +
                "기물 위 흡수 버튼을 누르면 흡수 공격을 실행합니다."
            );
        }
        else
        {
            // 빈칸 이동을 확인하면
            // 기존 상대 기물 위 흡수 버튼을 닫는다.
            ClearFieldAbsorbOpportunity();

            // <변경부분> 빈칸 이동을 확인하는 경우에는
            // 이전 상대 공격 확인 대상을 해제한다.
            pendingAttackTargetPiece =
                null;

            if (battleUIController != null)
            {
                battleUIController
                    .ClearEnemyStatus();
            }

            Debug.Log(
                "이동 타일 확인: 같은 타일을 한 번 더 클릭하면 이동합니다."
            );
        }

        // 선택 기물과 현재 공격 확인 대상에 맞춰
        // 타입 아이콘 표시 및 반투명 상태를 갱신한다.
        RefreshTypeIconVisuals();
    }

    // <변경부분> BattleSetupManager의 Start에서 보드와 기물 생성이 끝날 때까지
    // 한 프레임 기다린 뒤 플레이어 초기 배치 또는 정상 전투를 시작한다.
    private IEnumerator BeginPlayerDeploymentAfterSetupRoutine()
    {
        // BattleSetupManager와 EventSequenceController의
        // Start 초기화가 모두 끝날 수 있도록 한 프레임 기다린다.
        yield return null;

        // <변경부분> 현재 Event Sequence가
        // 일반 플레이어 초기 배치를 사용하지 않도록 설정했다면
        // 기존 배치 UI를 시작하지 않는다.
        //
        // Event Sequence가 직접 Dialogue / ForcePiece / ForceTile 등을
        // 제어하므로 여기서는 정상 전투 턴 상태만 준비한다.
        if (eventSequenceController != null &&
            eventSequenceController.ShouldSkipNormalPlayerDeployment)
        {
            isPlayerDeploymentPhase =
                false;

            if (battleUIController != null)
            {
                battleUIController.SetPlayerDeploymentMode(
                    false
                );

                battleUIController.HideActionButtons();
            }

            StartNormalBattleTurn();

            Debug.Log(
                "Event Sequence 활성화: " +
                "기존 플레이어 초기 배치를 건너뜁니다."
            );

            yield break;
        }

        // 일반 Battle에서는 기존 초기 배치 설정을 그대로 사용한다.
        if (usePlayerDeploymentPhase)
        {
            BeginPlayerDeploymentPhase();
            yield break;
        }

        StartNormalBattleTurn();
    }

    // <변경부분> 플레이어 초기 배치 단계를 시작한다.
    private void BeginPlayerDeploymentPhase()
    {
        isPlayerDeploymentPhase =
            true;

        selectedDeploymentPiece =
            null;

        selectedPiece =
            null;

        pendingActionTile =
            null;

        pendingAttackTargetPiece =
            null;

        ClearHighlights();
        RefreshTypeIconVisuals();

        // <변경부분> 흡수 버튼을 체크 버튼으로 변경하고
        // 고유스킬 버튼은 배치 종료 전까지 숨긴다.
        if (battleUIController != null)
        {
            battleUIController.SetPlayerDeploymentMode(
                true
            );

            // <변경부분> 기존 SkillFailurePopup을 재사용하여
            // 전투 시작 전 플레이어 기물 배치 방법을 안내한다.
            battleUIController.ShowUniqueSkillFailureMessage(
                "기물 자리 배치를 진행하고\n 체크 버튼을 누르세요."
            );
        }

        Debug.Log(
            "플레이어 초기 배치 시작: " +
            "Pawn / Knight / Rook / Bishop의 위치를 정한 뒤 " +
            "체크 버튼을 누르세요."
        );
    }

    // <변경부분> 배치 단계에서 기물을 직접 클릭했을 때
    // 기물 정보 표시와 배치 선택을 처리한다.
    private void HandleDeploymentPieceSelection(
        Piece piece)
    {
        if (piece == null)
        {
            ClearDeploymentSelection();
            return;
        }

        // <변경부분> 배치 단계에서도 Player 기물을 클릭하면
        // 왼쪽 플레이어 스테이터스 UI에 해당 기물 정보를 표시한다.
        //
        // King처럼 배치 이동은 불가능한 기물도
        // 정보 확인 자체는 가능하게 유지한다.
        if (piece.Team ==
            PieceTeam.Player)
        {
            if (battleUIController != null)
            {
                battleUIController.RefreshPlayerStatusOnly(
                    piece
                );

                // 플레이어 기물을 확인할 때
                // 이전에 표시된 상대 스테이터스는 닫는다.
                battleUIController.ClearEnemyStatus();
            }
        }

        if (CanSelectPieceForDeployment(
                piece) == false)
        {
            Debug.Log(
                "배치 선택 불가: " +
                "Player 진영의 Pawn / Knight / Rook / Bishop만 옮길 수 있습니다."
            );

            return;
        }

        SelectDeploymentPiece(
            piece
        );
    }

    // <변경부분> 배치 단계의 타일 클릭을 처리한다.
    //
    // 선택된 기물이 없는 경우:
    // 클릭한 플레이어 기물을 선택
    //
    // 선택된 기물이 있는 경우:
    // 빈칸으로 이동하거나 다른 일반 플레이어 기물과 자리를 교환
    private void HandleDeploymentTileSelection(
        Tile tile)
    {
        if (tile == null ||
            pieceManager == null)
        {
            return;
        }

        Piece clickedPiece =
            pieceManager.GetPieceAt(
                tile.X,
                tile.Y
            );

        // 아직 배치 기물을 선택하지 않은 상태라면
        // 클릭한 일반 플레이어 기물을 선택한다.
        if (selectedDeploymentPiece == null)
        {
            if (clickedPiece != null)
            {
                HandleDeploymentPieceSelection(
                    clickedPiece
                );
            }

            return;
        }

        // 현재 선택한 기물을 다시 클릭하면 선택을 해제한다.
        if (clickedPiece ==
            selectedDeploymentPiece)
        {
            ClearDeploymentSelection();
            return;
        }

        // 플레이어 시작 구역 밖에는 배치할 수 없다.
        if (IsInsidePlayerDeploymentArea(
                tile.X,
                tile.Y) == false)
        {
            Debug.Log(
                $"배치 이동 불가: " +
                $"({tile.X}, {tile.Y})는 플레이어 시작 구역이 아닙니다."
            );

            return;
        }

        // 목표 칸에 King, Enemy, Neutral 등이 있으면
        // 해당 기물과 자리를 교환하지 않는다.
        if (clickedPiece != null &&
            CanSelectPieceForDeployment(
                clickedPiece) == false)
        {
            Debug.Log(
                "배치 이동 불가: " +
                "King 또는 다른 진영 기물과는 자리를 교체할 수 없습니다."
            );

            return;
        }

        Piece movingPiece =
            selectedDeploymentPiece;

        bool moved =
            pieceManager.TryMoveOrSwapPlayerDeploymentPiece(
                movingPiece,
                tile.X,
                tile.Y
            );

        if (moved == false)
        {
            return;
        }

        ClearDeploymentSelection();

        Debug.Log(
            $"플레이어 배치 변경 완료: " +
            $"{movingPiece.PieceType} → ({tile.X}, {tile.Y})"
        );
    }

    // <변경부분> 배치 단계에서 위치를 옮길 수 있는 기물인지 확인한다.
    //
    // Player 진영의 Pawn, Knight, Rook, Bishop만 허용하고
    // King, Queen, Special, Enemy, Neutral은 제외한다.
    private bool CanSelectPieceForDeployment(
        Piece piece)
    {
        if (piece == null ||
            piece.Team != PieceTeam.Player)
        {
            return false;
        }

        switch (piece.PieceType)
        {
            case PieceType.Pawn:
            case PieceType.Knight:
            case PieceType.Rook:
            case PieceType.Bishop:
                return true;
        }

        return false;
    }

    // <변경부분> 플레이어 초기 배치에서 사용할 시작 구역인지 검사한다.
    // 기본값 2에서는 보드 아래쪽 Y = 0, 1만 허용한다.
    private bool IsInsidePlayerDeploymentArea(
        int x,
        int y)
    {
        if (boardManager == null)
        {
            return false;
        }

        if (x < 0 ||
            x >= boardManager.Width)
        {
            return false;
        }

        int deploymentRowCount =
            Mathf.Clamp(
                playerDeploymentRowCount,
                1,
                boardManager.Height
            );

        return
            y >= 0 &&
            y < deploymentRowCount;
    }

    // <변경부분> 배치할 기물을 선택하고
    // 플레이어 스테이터스와 이동 가능한 시작 구역을 표시한다.
    private void SelectDeploymentPiece(
        Piece piece)
    {
        if (selectedDeploymentPiece != null &&
            selectedDeploymentPiece != piece)
        {
            pieceManager.PlayPieceDeselectAnimation(
                selectedDeploymentPiece
            );
        }

        selectedDeploymentPiece =
            piece;

        selectedPiece =
            piece;

        pieceManager.PlayPieceSelectAnimation(
            piece
        );

        // <변경부분> 일반 전투 선택 처리로 들어가지 않는
        // 배치 단계에서도 선택한 기물의 스테이터스를 표시한다.
        //
        // 액션 버튼은 건드리지 않으므로
        // 배치 체크 버튼 상태는 그대로 유지된다.
        if (battleUIController != null)
        {
            battleUIController.RefreshPlayerStatusOnly(
                piece
            );

            battleUIController.ClearEnemyStatus();
        }

        ClearHighlights();
        ShowPlayerDeploymentTiles();
        RefreshTypeIconVisuals();
    }

    // <변경부분> 현재 선택된 배치 기물을 해제하고
    // 플레이어 스테이터스와 하이라이트를 정리한다.
    private void ClearDeploymentSelection()
    {
        if (selectedDeploymentPiece != null &&
            pieceManager != null)
        {
            pieceManager.PlayPieceDeselectAnimation(
                selectedDeploymentPiece
            );
        }

        selectedDeploymentPiece =
            null;

        selectedPiece =
            null;

        // <변경부분> 빈 타일 클릭, 같은 기물 재클릭,
        // 배치 완료 등으로 선택이 해제되면
        // 플레이어 스테이터스 UI도 함께 닫는다.
        if (battleUIController != null)
        {
            battleUIController.ClearPlayerStatusOnly();
        }

        ClearHighlights();
        RefreshTypeIconVisuals();
    }

    // <변경부분> 플레이어 시작 구역에서
    // 빈칸 또는 교환 가능한 일반 플레이어 기물 칸을 하이라이트한다.
    private void ShowPlayerDeploymentTiles()
    {
        if (selectedDeploymentPiece == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return;
        }

        int deploymentRows =
            Mathf.Clamp(
                playerDeploymentRowCount,
                1,
                boardManager.Height
            );

        for (int y = 0;
             y < deploymentRows;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                Piece targetPiece =
                    pieceManager.GetPieceAt(
                        x,
                        y
                    );

                // King, Enemy, Neutral 등이 있는 칸은 표시하지 않는다.
                if (targetPiece != null &&
                    CanSelectPieceForDeployment(
                        targetPiece) == false)
                {
                    continue;
                }

                HighlightTile(
                    x,
                    y
                );
            }
        }
    }

    // <변경부분> 우측 체크 버튼에서 호출하는
    // 플레이어 초기 배치 완료 함수
    public void ConfirmPlayerDeployment()
    {
        if (isPlayerDeploymentPhase == false)
        {
            return;
        }

        ClearDeploymentSelection();

        isPlayerDeploymentPhase =
            false;

        // <변경부분> 체크 아이콘을 기존 흡수 아이콘으로 복구하고
        // 정상 전투 UI 상태로 전환한다.
        if (battleUIController != null)
        {
            battleUIController.SetPlayerDeploymentMode(
                false
            );

            battleUIController.HideActionButtons();
        }

        Debug.Log(
            "플레이어 초기 배치 완료: 전투를 시작합니다."
        );

        StartNormalBattleTurn();
    }

    // <변경부분> 배치 종료 후 현재 턴을
    // 기존 BattleAIManager 전투 흐름에 전달한다.
    private void StartNormalBattleTurn()
    {
        // <변경부분> 정상 전투 시작 시 현재 보드 상태를 기준으로
        // 마지막 Enemy 1기 강제 흡수 버튼 표시 여부를 갱신한다.
        RefreshLastEnemyAbsorbOpportunity();

        if (battleAIManager != null)
        {
            battleAIManager.HandleTurnStarted(
                currentTurn
            );
        }
    }

    // <변경부분> 사람과 AI가 공통으로 사용하는 전투 행동 실행 진입점
    // 행동 기물과 목표 좌표를 직접 전달받아 selectedPiece 의존성을 제거한다.
    public bool TryExecuteBattleAction(
        Piece actingPiece,
        Vector2Int targetPosition)
    {
        // <변경부분> 초기 배치 단계에서는
        // 일반 이동 및 공격 행동을 실행하지 않는다.
        if (isPlayerDeploymentPhase)
        {
            return false;
        }

        // 전투가 종료된 상태에서는 행동을 실행할 수 없다.
        if (isBattleEnded)
        {
            Debug.Log(
                "전투 행동 실행 실패: 전투가 이미 종료되었습니다."
            );

            return false;
        }

        // 다른 이동, 공격 또는 스킬 연출 중에는
        // 새로운 행동을 중복 실행하지 않는다.
        if (isActionAnimating)
        {
            Debug.Log(
                "전투 행동 실행 실패: 다른 행동이 진행 중입니다."
            );

            return false;
        }

        // 행동할 기물이 없으면 실행할 수 없다.
        if (actingPiece == null)
        {
            Debug.LogWarning(
                "전투 행동 실행 실패: 행동 기물이 없습니다."
            );

            return false;
        }

        // 현재 턴에 조작 가능한 진영의 기물인지 검사한다.
        if (IsCurrentTurnPiece(actingPiece) == false)
        {
            Debug.LogWarning(
                $"전투 행동 실행 실패: " +
                $"{actingPiece.Team} {actingPiece.PieceType}은 " +
                $"현재 턴의 기물이 아닙니다."
            );

            return false;
        }

        // 이동 불가능한 기물은 행동할 수 없다.
        if (actingPiece.CanMove == false)
        {
            Debug.LogWarning(
                $"전투 행동 실행 실패: " +
                $"{actingPiece.Team} {actingPiece.PieceType}은 " +
                $"이동할 수 없는 기물입니다."
            );

            return false;
        }

        // 필요한 전투 참조가 없으면 행동을 실행할 수 없다.
        if (boardManager == null ||
            pieceManager == null ||
            battleMoveValidator == null)
        {
            Debug.LogWarning(
                "전투 행동 실행 실패: " +
                "필요한 전투 매니저가 연결되지 않았습니다."
            );

            return false;
        }

        // 공용 이동 판정기를 사용해
        // 요청받은 목표 좌표가 실제 합법 행동인지 검사한다.
        List<Vector2Int> selectablePositions =
            battleMoveValidator.GetSelectablePositions(
                actingPiece
            );

        if (selectablePositions.Contains(targetPosition) == false)
        {
            Debug.LogWarning(
                $"전투 행동 실행 실패: " +
                $"{actingPiece.Team} {actingPiece.PieceType} / " +
                $"({actingPiece.X}, {actingPiece.Y}) → " +
                $"({targetPosition.x}, {targetPosition.y})는 " +
                $"이동 또는 공격 가능한 좌표가 아닙니다."
            );

            return false;
        }

        // 목표 좌표에 대응하는 실제 Tile을 가져온다.
        Tile targetTile =
            boardManager.GetTile(
                targetPosition.x,
                targetPosition.y
            );

        if (targetTile == null)
        {
            Debug.LogWarning(
                $"전투 행동 실행 실패: " +
                $"목표 Tile을 찾을 수 없습니다. " +
                $"({targetPosition.x}, {targetPosition.y})"
            );

            return false;
        }

        // <변경부분> 검증을 통과한 행동만 공용 코루틴으로 실행한다.
        StartCoroutine(
            ExecutePieceActionRoutine(
                actingPiece,
                targetTile
            )
        );

        return true;
    }

    // <변경부분> AI가 선택한 고유스킬 행동을
    // 실제 BattleSkillManager 실행 흐름으로 전달한다.
    //
    // 이동과 공격은 TryExecuteBattleAction()을 사용하고,
    // 고유스킬은 이 함수를 사용하여 실행 경로를 분리한다.
    public bool TryExecuteAIUniqueSkill(
        BattleAIAction action)
    {
        if (CanUseAIUniqueSkillAction(
                action) ==
            false)
        {
            Debug.LogWarning(
                "Enemy AI 고유스킬 실행 실패: " +
                "현재 사용할 수 없는 고유스킬 행동입니다."
            );

            return false;
        }

        StartCoroutine(
            ExecuteAIUniqueSkillRoutine(
                action
            )
        );

        return true;
    }

    // <변경부분> AI 고유스킬의 실제 효과와
    // 쿨타임, 사망 스택, 턴당 사용 상태를 처리한다.
    //
    // 고유스킬 성공 후에는 턴을 종료하지 않는다.
    // BattleAIManager가 변경된 보드 상태로 후보를 다시 생성하여
    // 같은 Enemy 턴에 이동 또는 공격을 이어서 실행한다.
    private IEnumerator ExecuteAIUniqueSkillRoutine(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActingPiece == null ||
            battleSkillManager == null)
        {
            yield break;
        }

        if (isActionAnimating)
        {
            yield break;
        }

        isActionAnimating = true;

        Piece skillPiece =
            action.ActingPiece;

        UniqueSkillData skillData =
            GetUniqueSkillData(
                action.UniqueSkillType
            );

        if (skillData == null)
        {
            isActionAnimating = false;
            yield break;
        }

        bool skillUsed = false;

        // <변경부분> UniqueSkillData의 아이콘을 함께 전달하여
        // 실제 고유스킬보다 아이콘이 먼저 재생되도록 한다.
        yield return
            battleSkillManager
                .TryUseUniqueSkillRoutine(
                    skillPiece,
                    skillData.iconSprite,
                    result =>
                        skillUsed = result
                );
        if (skillUsed == false)
        {
            Debug.LogWarning(
                $"Enemy AI 고유스킬 실행 실패: " +
                $"{action.UniqueSkillType} 내부 조건을 만족하지 못했습니다."
            );

            isActionAnimating = false;
            yield break;
        }

        // 데이터 설정에 따라 턴 전체 고유스킬 사용권을 소모한다.
        if (skillData.oncePerTurn)
        {
            hasUsedUniqueSkillThisTurn =
                true;
        }

        // 데이터 설정에 따라 요구 사망 스택을 소모한다.
        if (skillData.consumeDeathStackOnUse)
        {
            ConsumeDeathStackForUniqueSkill(
                skillPiece.Team,
                skillData.requiredDeathStack
            );
        }

        // 사용한 기물에 고유스킬 쿨타임을 적용한다.
        skillPiece.MarkUniqueSkillUsed(
            skillData.cooldownTurn
        );


        // AI 행동에는 플레이어 클릭 확인 상태가 필요하지 않으므로 초기화한다.
        pendingActionTile = null;
        pendingAttackTargetPiece = null;

        ClearHighlights();
        RefreshTypeIconVisuals();

        // 합성으로 재료 기물이 제거되는 등
        // 보드 상태가 변경되었으므로 승패 조건을 다시 확인한다.
        CheckBattleEnd();

        Debug.Log(
            $"Enemy AI 고유스킬 사용 완료: " +
            $"{action.UniqueSkillType} / " +
            $"쿨타임 {skillData.cooldownTurn} / " +
            "Enemy 턴 유지 후 행동 재평가"
        );

        isActionAnimating = false;
    }

    // <변경부분> 지정한 기물의 이동/공격/흡수를 실행하는 공용 코루틴
    // 사람과 AI가 동일한 전투 실행 흐름을 사용한다.
    // 공격/흡수 시 타겟 제거는 이동 연출 이후에 처리한다.
    private IEnumerator ExecutePieceActionRoutine(
        Piece actingPiece,
        Tile tile)
    {
        // 이미 연출 중이면 중복 실행을 방지한다.
        if (isActionAnimating)
        {
            yield break;
        }

        // 행동 기물이나 목표 타일이 사라졌다면 실행하지 않는다.
        if (actingPiece == null ||
            tile == null)
        {
            yield break;
        }

        isActionAnimating = true;



        // 해당 타일에 있는 기물을 확인한다.
        Piece targetPiece =
            pieceManager.GetPieceAt(
                tile.X,
                tile.Y
            );

        // <변경부분> 이번 공격 대상이 현재 보드의
        // 마지막 Enemy 1기인지 행동 시작 전에 저장한다.
        //
        // 빈칸 이동과 중립 기물 공격에는
        // 마지막 공격 카메라 연출을 적용하지 않는다.
        bool isLastEnemyAttackTarget =
            targetPiece != null &&
            targetPiece.Team == PieceTeam.Enemy &&
            FindSingleRemainingEnemyPiece() ==
                targetPiece;

        // <변경부분> 현재 행동에서 마지막 기물 공격
        // 카메라 연출을 시작했는지 확인한다.
        bool isPlayingLastEnemyAttackCinematic =
            false;

        // <변경부분> 현재 행동에서 선택 기물의
        // 일반 이동·공격 카메라 추적을 시작했는지 확인한다.
        bool isFollowingActingPiece =
            false;

        // 흡수/레벨업이 적용되기 전
        // ChanceAttack 보유 정보만 복사해서 저장한다.
        OwnedGeneralSkillData chanceAttackDataBeforeAction =
            actingPiece.GetGeneralSkillDataCopy(
                GeneralSkillType.ChanceAttack
            );

        // <변경부분> 행동 시작 시점의 Defense 보유 정보를 복사한다.
        // 이번 이동 또는 흡수로 새로 얻은 Defense는
        // 같은 행동에서 즉시 발동하지 않도록 한다.
        //
        // 행동 시작 전부터 Defense를 가지고 있었다면
        // 흡수 행동이어도 기존 Defense는 정상적으로 발동할 수 있다.
        OwnedGeneralSkillData defenseDataBeforeAction =
            actingPiece.GetGeneralSkillDataCopy(
                GeneralSkillType.Defense
            );

        // <변경부분> 공격 시작 시점의 Insight 보유 정보 저장
        // 공격 중 흡수/레벨업으로 얻은 Insight가 즉시 발동하지 않도록
        // 행동 시작 전 상태를 기준으로 판정한다.
        OwnedGeneralSkillData insightDataBeforeAction =
            actingPiece.GetGeneralSkillDataCopy(
                GeneralSkillType.Insight
            );


        // 이번 행동으로 적대 기물을 처치했는지 확인
        bool killedEnemyPiece = false;

        // 이번 행동이 플레이어 흡수 성공 행동인지 확인
        bool absorbedEnemyPiece = false;

        // <변경부분> 이번 행동에서 actingPiece가
        // 실제로 다른 타일로 이동을 완료했는지 확인한다.
        //
        // 빈칸 이동과 공격 성공 후 목표 칸 이동은 true,
        // Defence 상태효과로 공격이 막혀 원래 자리로 복귀한 경우는 false다.
        bool didCompleteMove = false;

        // <변경부분> 흡수로 기물 외형/종류가 변경된 뒤
        // Born 애니메이션을 재생할지 여부
        bool shouldPlayAbsorbBornAnimation = false;

        // 기물이 이동/공격하면 모든 타입 아이콘 비활성화
        RefreshTypeIconVisuals();

        // 타겟 기물이 있으면 공격/흡수 처리
        if (targetPiece != null)
        {
            // 적대 관계가 아니면 공격 불가
            if (actingPiece.IsEnemyOf(targetPiece) == false)
            {
                isActionAnimating = false;
                yield break;
            }

            // 흡수 모드이고 Player 기물이 Enemy 기물을 공격하는 경우
            // 기본적으로 흡수 공격을 허용한다.
            //
            // 단 Enemy King은 예외적으로
            // Player King이 공격할 때만 흡수할 수 있다.
            bool canAbsorbTarget =
                targetPiece.PieceType != PieceType.King ||
                actingPiece.PieceType == PieceType.King;

            bool isAbsorbAction =
                isAbsorbMode &&
                actingPiece.Team == PieceTeam.Player &&
                targetPiece.Team == PieceTeam.Enemy &&
                canAbsorbTarget;

            // 제거될 기물의 소속을 미리 저장
            PieceTeam deadPieceTeam = targetPiece.Team;

            // <변경부분> 타겟이 퇴화 상태였는지 사망 전에 저장
            bool shouldTriggerDegeneration =
                targetPiece.HasStatusEffect(
                    StatusEffectType.Degeneration
                );

            PieceTeam degenerationDeadPieceTeam =
                targetPiece.Team;

            PieceType degenerationDeadPieceType =
                targetPiece.PieceType;

            int degenerationDeadPieceX =
                targetPiece.X;

            int degenerationDeadPieceY =
                targetPiece.Y;

            // <변경부분> 퇴화 생성 연출 시작 위치로 사용할
            // 사망 기물의 월드 위치 저장
            Vector3 degenerationSourceWorldPosition =
                targetPiece.transform.position;



            // <변경부분> 공격 시작 시점에 대상이
            // 확정 방어용 Defence 상태효과를 보유하고 있는지 저장한다.
            bool hasDefenceStatusEffect =
                targetPiece.HasStatusEffect(
                    StatusEffectType.Defence
                );

            // <변경부분> 공격자가 Breakthrough 상태이면
            // 상대가 보유한 Defence 상태효과를 무시한다.
            bool shouldIgnoreDefenseByBreakthrough =
                actingPiece != null &&
                actingPiece.HasStatusEffect(
                    StatusEffectType.Breakthrough
                );

            if (shouldIgnoreDefenseByBreakthrough)
            {
                Debug.Log(
                    $"Breakthrough 발동: " +
                    $"{actingPiece.Team} {actingPiece.PieceType}이 " +
                    $"{targetPiece.Team} {targetPiece.PieceType}의 " +
                    $"Defence 상태효과를 무시합니다."
                );
            }

            // <변경부분> 피격 순간에는 Defence 상태효과만 방어를 발동한다.
            //
            // 단, 현재 보드의 마지막 Enemy 1기를 공격하는
            // 마지막 일격은 Defence 상태효과를 완전히 무시한다.
            //
            // Defense 일반스킬 자체는 공격을 직접 방어하지 않으며,
            // 이동 완료 후 일정 확률로 Defence 상태효과를 부여하는 역할만 한다.
            bool isDefenseActivated =
                isLastEnemyAttackTarget == false &&
                shouldIgnoreDefenseByBreakthrough == false &&
                hasDefenceStatusEffect;

            // <변경부분> 마지막 Enemy를 향한 마지막 일격이라면
            // Defence 상태효과가 남아 있어도 공격을 막지 않는다.
            if (isLastEnemyAttackTarget &&
                hasDefenceStatusEffect)
            {
                Debug.Log(
                    $"마지막 일격 발동: " +
                    $"{targetPiece.Team} {targetPiece.PieceType}의 " +
                    $"Defence 상태효과를 무시합니다."
                );
            }
            else if (isDefenseActivated)
            {
                Debug.Log(
                    $"Defence 상태효과 발동: " +
                    $"{targetPiece.Team} {targetPiece.PieceType}의 " +
                    $"방어가 확정 발동했습니다."
                );
            }

            // <변경부분> Defense가 실제로 발동했을 때만
            // 공격자의 Insight로 무효화를 시도한다.
            bool isDefenseCanceledByInsight = false;

            if (isDefenseActivated)
            {
                isDefenseCanceledByInsight =
                    battleSkillManager != null &&
                    battleSkillManager.TryActivateInsight(
                        actingPiece,
                        insightDataBeforeAction,
                        GeneralSkillType.Defense
                    );

                if (isDefenseCanceledByInsight)
                {
                    // <변경부분> 실제 방어 무효화 흐름을 계속 진행하기 전에
                    // Insight 아이콘의 확대 연출을 먼저 재생한다.
                    yield return
                        battleSkillManager
                            .PlayGeneralSkillActivationBeforeEffectRoutine(
                                actingPiece,
                                GeneralSkillType.Insight
                            );

                    Debug.Log(
                        $"Insight 발동: " +
                        $"{actingPiece.Team} {actingPiece.PieceType}이 " +
                        $"{targetPiece.Team} {targetPiece.PieceType}의 " +
                        $"Defense를 무효화했습니다."
                    );
                }
            }

            // 공격 대상의 월드 위치 저장
            Vector3 targetWorldPosition =
                targetPiece.transform.position;

            // <변경부분> 마지막 Enemy 1기를 공격하는 경우
            // 공격 시작 전에는 카메라 위치만 목표 타일 중심으로 이동시킨다.
            //
            // 이 시점에는 줌과 슬로우 모션을 시작하지 않는다.
            // 공격 기물이 정상 좌표와 정상 속도로 목표 위치까지 이동한 뒤,
            // 실제 내려찍기·타격 순간에 확대와 슬로우 모션을 시작한다.
            if (isLastEnemyAttackTarget &&
                pixelCameraController != null)
            {
                yield return
                    pixelCameraController
                        .PrepareLastPieceAttackCinematicRoutine(
                            tile.transform
                        );

                isPlayingLastEnemyAttackCinematic =
                    true;
            }


            // <변경부분> 마지막 적 공격이 아닌 일반 공격에서는
            // 플레이어가 직접 선택한 공격 기물을 카메라가 따라간다.
            else if (pixelCameraController != null &&
          actingPiece == selectedPiece)
            {
                // <변경부분> 공격 기물의 점프 Transform을 따라가지 않고
                // 공격 목표 타일 중심으로 카메라를 이동시킨다.
                pixelCameraController
                    .StartFollowingMovingTile(
                        tile.transform
                    );

                isFollowingActingPiece =
                    true;
            }

            // Defense가 성공했고 Insight로 무효화되지 않은 경우
            if (isDefenseActivated &&
                isDefenseCanceledByInsight == false)
            {
                // <변경부분> 방어 성공 전용 공격 반동 연출 실행
                yield return
                    pieceManager.PlayPieceBlockedAttackMoveAnimation(
                        actingPiece,
                        targetWorldPosition
                    );

                // 흡수 공격이 방어되었다면 흡수 모드 해제
                if (isAbsorbAction)
                {
                    isAbsorbMode =
                        false;

                    if (battleUIController != null)
                    {
                        battleUIController.SetAbsorbModeIcon(
                            false
                        );
                    }
                }

                // 방어 성공 시 타겟 제거 및 흡수 처리 없음
                killedEnemyPiece =
                    false;

                absorbedEnemyPiece =
                    false;

                Debug.Log(
                    $"Defence 상태효과 발동: " +
                    $"{targetPiece.Team} {targetPiece.PieceType}이 " +
                    $"공격을 방어했습니다."
                );
            }
            else
            {
                // <변경부분> 방어가 없거나 Insight로 무효화됐다면
                // 기존 일반 공격·흡수 공격 연출을 실행한다.
                yield return
     pieceManager.PlayPieceAttackMoveAnimation(
         actingPiece,
         targetWorldPosition,
         isAbsorbAction,
         () =>
         {
             // <변경부분> 마지막 Enemy 공격에서는
             // 기물이 목표 위치까지 이동하고 실제 타격 콜백이 실행되는 순간
             // 확대와 슬로우 모션을 시작한다.
             //
             // 이동 중에는 기존 줌과 정상 속도를 유지하므로
             // WorldRoot 확대에 의해 기물 이동 좌표가 어긋나는 현상을 방지한다.
             if (isLastEnemyAttackTarget &&
                 pixelCameraController != null)
             {
                 pixelCameraController
                     .StartLastPieceAttackSlowMotion();
             }

             // 일반 공격은 대상 숨김 처리가 필요하지 않으므로
             // 아래부터는 흡수 공격일 때만 처리한다.
             if (isAbsorbAction == false)
             {
                 return;
             }

             if (targetPiece == null)
             {
                 return;
             }

             // 흡수 충격 순간 대상만 화면에서 숨긴다.
             targetPiece.gameObject.SetActive(
                 false
             );
         }
     );

                // 실제 흡수 공격인 경우
                if (isAbsorbAction)
                {
                    PieceType absorbedType =
                        targetPiece.PieceType;
                   
                    if (actingPiece.PieceType ==
                        PieceType.King)
                    {
                        if (targetPiece.PieceType ==
                            PieceType.King)
                        {
                            // King → King 완전 흡수
                            //
                            // AbsorbPiece는 대상의 PieceData / 타입 / 고유스킬 /
                            // 종족 태그 / 일반스킬을 모두 복사한다.
                            //
                            // Team은 변경하지 않으므로 Player 소속은 그대로 유지된다.
                            pieceManager.AbsorbPiece(
                                actingPiece,
                                targetPiece
                            );

                            // 외형이 Enemy King 기준으로 변경되므로
                            // 최종 위치에서 Born 애니메이션을 재생한다.
                            shouldPlayAbsorbBornAnimation =
                                true;

                            Debug.Log(
                                $"King 완전 흡수 성공: " +
                                $"{absorbedType}의 외형, 고유스킬, 일반스킬을 모두 흡수했습니다."
                            );
                        }
                        else
                        {
                            // King → 일반 Enemy는 기존 규칙 유지
                            //
                            // King의 외형/타입/고유스킬은 유지하고
                            // 일반스킬만 흡수한다.
                            pieceManager.AbsorbGeneralSkillsOnly(
                                actingPiece,
                                targetPiece
                            );

                            Debug.Log(
                                $"King 일반 흡수 성공: " +
                                $"{absorbedType}의 일반스킬만 흡수했습니다."
                            );
                        }
                    }
                    else
                    {
                        // 일반 기물은 기존처럼
                        // 대상의 타입, 고유스킬, 외형, 일반스킬을 모두 흡수한다.
                        pieceManager.AbsorbPiece(
                            actingPiece,
                            targetPiece
                        );

                        // 외형 변경 후 최종 위치에서 Born 애니메이션 재생
                        shouldPlayAbsorbBornAnimation =
                            true;

                        Debug.Log(
                            $"흡수 성공: " +
                            $"{absorbedType} 데이터를 복사했습니다."
                        );
                    }

                    // 이번 전투의 실제 흡수 성공 횟수 증가
                    playerAbsorbCountThisBattle++;

                    Debug.Log(
                        $"플레이어 흡수 수 증가: " +
                        $"{playerAbsorbCountThisBattle}"
                    );

                    // 흡수 결과를 버튼 및 스테이터스 UI에 반영
                    if (battleUIController != null)
                    {
                        battleUIController
                            .RefreshSelectedPieceButtons(
                                actingPiece
                            );
                    }

                    // 적대 기물을 제거했으므로
                    // ChanceAttack 판정 대상으로 저장
                    killedEnemyPiece = true;

                    // 실제 플레이어 흡수 성공 행동으로 저장
                    absorbedEnemyPiece = true;

                    // 흡수 행동을 완료했으므로 흡수 모드 해제
                    isAbsorbMode = false;
                }
                else
                {
                    // 일반 공격으로 적대 기물을 제거
                    // 중립 기물 처치도 ChanceAttack 판정 대상
                    killedEnemyPiece = true;
                }

                // <변경부분> 공격 애니메이션이 끝난 이후에만
                // 타겟 기물을 실제로 제거한다.
                //
                // 흡수 공격에서는 PieceAnimationManager가
                // Down_Absorb 전체 종료까지 기다리고 복귀하므로
                // Down_Absorb 중간에 타겟 제거 또는 외형 변경이 실행되지 않는다.
                pieceManager.RemovePiece(targetPiece);

                // 실제 사망한 경우에만 퇴화 효과 처리
                TryTriggerDegenerationOnDeath(
                    shouldTriggerDegeneration,
                    degenerationDeadPieceTeam,
                    degenerationDeadPieceType,
                    degenerationDeadPieceX,
                    degenerationDeadPieceY,
                    degenerationSourceWorldPosition
                );

                // 실제 사망한 경우에만 해당 진영 사망 스택 증가
                AddDeathStackForUniqueSkill(
                    deadPieceTeam
                );

                // 실제 공격 성공 시 공격자의 논리 좌표와
                // 월드 위치를 타겟 위치로 확정
                yield return pieceManager.MovePieceRoutine(
                    actingPiece,
                    tile.X,
                    tile.Y,
                    false
                );

                // <변경부분> 공격 성공 후 목표 타일까지
                // 논리 좌표와 월드 위치 이동이 모두 완료됐다.
                didCompleteMove = true;

                // 흡수로 외형과 기물 종류가 변경된 일반 기물은
                // 최종 위치에서 Born 애니메이션 재생
                if (shouldPlayAbsorbBornAnimation)
                {
                    yield return
                        pieceManager.PlayPieceBornAnimation(
                            actingPiece
                        );
                }
            }
        }
        else
        {
            // <변경부분> 플레이어가 직접 선택한 기물이 빈칸으로 이동할 때
            // 기물의 점프 높이가 아니라 최종 이동 타일 중심으로 카메라를 이동시킨다.
            if (pixelCameraController != null &&
                actingPiece == selectedPiece)
            {
                pixelCameraController
                    .StartFollowingMovingTile(
                        tile.transform
                    );

                isFollowingActingPiece =
                    true;
            }

            // 빈칸 이동은 기존 점프 이동 연출 실행
            yield return
                pieceManager.MovePieceRoutine(
                    actingPiece,
                    tile.X,
                    tile.Y,
                    true
                );

            // <변경부분> 빈칸 이동 애니메이션과
            // 좌표 갱신이 모두 완료됐다.
            didCompleteMove =
                true;
        }



        // <변경부분> 일반 이동·공격 추적이 끝났다면
        // 최종 기물 위치를 유지한 채 자동 추적을 종료한다.
        if (isFollowingActingPiece &&
     pixelCameraController != null)
        {
            pixelCameraController
                .StopFollowingMovingTile(
                    true
                );
        }

        // <변경부분> 마지막 Enemy 공격 연출이 끝났다면
        // 이전 카메라 위치, 줌, 시간 배율로 복구한다.
        if (isPlayingLastEnemyAttackCinematic &&
            pixelCameraController != null)
        {
            yield return
                pixelCameraController
                    .RestoreAfterLastPieceAttackCinematicRoutine();
        }

        // <변경부분> 실제 이동을 완료한 경우에만
        // 행동 시작 전부터 보유했던 Defense의
        // Defence 상태효과 부여를 판정한다.
        if (didCompleteMove &&
            battleSkillManager != null)
        {
            bool defenceGranted =
                false;

            // <변경부분> 행동 시작 전에 저장한 Defense 데이터를 전달한다.
            //
            // 이번 흡수로 새로 얻은 Defense는
            // defenseDataBeforeAction에 존재하지 않으므로
            // 같은 흡수 행동에서는 발동하지 않는다.
            yield return
                battleSkillManager
                    .TryGrantDefenceAfterMoveRoutine(
                        actingPiece,
                        defenseDataBeforeAction,
                        result =>
                            defenceGranted = result
                    );
        }

        // <변경부분> 이동 또는 공격이 실행되었으므로
        // 타일 확인 상태와 공격 확인 대상을 모두 초기화한다.
        pendingActionTile =
            null;

        pendingAttackTargetPiece =
            null;

        // 이동/공격 후 승패 조건 확인
        CheckBattleEnd();

        // 전투가 끝났으면 턴 종료하지 않음
        if (isBattleEnded)
        {
            isActionAnimating = false;
            yield break;
        }

        // <변경부분> 흡수 성공 유물 효과는
        // 실제 흡수가 성공했을 때만 발동한다.
        if (absorbedEnemyPiece &&
            TryActivateAbsorbChanceAttackRelic(
                actingPiece
            ))
        {
            BattleRelicData activatedRelicData =
                GetRelicData(
                    BattleRelicType
                        .AbsorbChanceAttackOncePerTurn
                );

            // 유물 데이터가 턴당 1회라면 사용 처리
            if (activatedRelicData == null ||
                activatedRelicData.oncePerTurn)
            {
                hasUsedAbsorbChanceAttackRelicThisTurn =
                    true;
            }

            // 유물 데이터가 허용하면 흡수 직후
            // 고유스킬 사용 제한 해제
            if (activatedRelicData == null ||
                activatedRelicData
                    .enableUniqueSkillAfterAbsorb)
            {
                actingPiece
                    .EnableUniqueSkillAfterAbsorbChanceAttack();
            }

            // 흡수 유물의 추가 행동 상태 적용
            ActivateChanceAttackBonus(
                actingPiece
            );

            Debug.Log(
                $"유물 효과 발동: " +
                $"{activatedRelicData?.relicName} / " +
                $"흡수 성공으로 추가 행동을 얻었습니다."
            );

            isActionAnimating = false;
            yield break;
        }

        // 적대 기물을 처치했을 때
        // 행동 시작 전 ChanceAttack 보유 상태를 기준으로 발동 판정
        bool isChanceAttackActivated =
            killedEnemyPiece &&
            battleSkillManager != null &&
            battleMoveValidator != null &&
            battleMoveValidator.HasAnySelectableTile(
                actingPiece
            ) &&
            battleSkillManager.TryActivateChanceAttack(
                actingPiece,
                chanceAttackDataBeforeAction,
                chanceAttackContinuousCount
            );

        if (isChanceAttackActivated)
        {
            // <변경부분> 추가 행동 상태를 적용하기 전에
            // ChanceAttack 아이콘의 확대 연출을 먼저 재생한다.
            yield return
                battleSkillManager
                    .PlayGeneralSkillActivationBeforeEffectRoutine(
                        actingPiece,
                        GeneralSkillType.ChanceAttack
                    );

            // 아이콘 연출 후 연속 발동 횟수 증가
            chanceAttackContinuousCount++;

            // <변경부분> 아이콘이 먼저 뜬 뒤
            // 실제 추가 행동 상태를 적용한다.
            ActivateChanceAttackBonus(
                actingPiece
            );

            Debug.Log(
                "ChanceAttack 발동: " +
                "턴 종료 없이 한 번 더 이동할 수 있습니다."
            );

            isActionAnimating = false;
            yield break;
        }

        // ChanceAttack이 실패했거나
        // 발동 조건이 아니면 연속 발동 상태 초기화
        chanceAttackBonusPiece = null;
        chanceAttackContinuousCount = 0;

        isActionAnimating = false;

        // 이동 또는 공격 완료 후 턴 종료
        EndTurn();
    }
    // <변경부분> StageBattleData에서 받은 진영별 패배 조건을 적용하는 함수
    public void SetBattleEndCondition(
     BattleDefeatConditionType playerCondition,
     BattleDefeatConditionType enemyCondition)
    {
        playerDefeatCondition =
            playerCondition;

        enemyDefeatCondition =
            enemyCondition;

        Debug.Log(
            $"승패 조건 적용: " +
            $"Player={playerDefeatCondition} / " +
            $"Enemy={enemyDefeatCondition}"
        );
    }

    // <변경부분> StageBattleData에서 설정한
    // Enemy AI 고유스킬 사용 확률을 AI 매니저에 전달한다.
    public void SetEnemyAIUniqueSkillUseChance(
        float chancePercent)
    {
        if (battleAIManager == null)
        {
            Debug.LogWarning(
                "Enemy AI 고유스킬 사용 확률 적용 실패: " +
                "BattleAIManager가 연결되지 않았습니다."
            );

            return;
        }

        battleAIManager.SetUniqueSkillUseChance(
            chancePercent
        );
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

    // <변경부분> 현재 마지막 Enemy 1기 강제 흡수가 가능한지 검사한다.
    //
    // Player 턴이며 다른 행동이 진행 중이지 않고,
    // Player King과 Enemy 기물이 정확히 1기씩 존재해야 한다.
    public bool CanUseLastEnemyAbsorb()
    {
        if (isPlayerDeploymentPhase ||
            isBattleEnded ||
            isActionAnimating ||
            currentTurn != BattleTurn.Player ||
            pieceManager == null ||
            boardManager == null)
        {
            return false;
        }

        return
            FindPlayerKing() != null &&
            FindSingleRemainingEnemyPiece() != null;
    }

    // <변경부분> 흡수 버튼 클릭 시 마지막 Enemy 1기 강제 흡수를 시도한다.
    //
    // 조건이 맞으면 전용 코루틴을 시작하고 true,
    // 조건이 아니면 기존 흡수 모드가 실행될 수 있도록 false를 반환한다.
    public bool TryStartLastEnemyAbsorb()
    {
        if (CanUseLastEnemyAbsorb() == false)
        {
            return false;
        }

        Piece playerKing =
            FindPlayerKing();

        Piece lastEnemyPiece =
            FindSingleRemainingEnemyPiece();

        if (playerKing == null ||
            lastEnemyPiece == null)
        {
            RefreshLastEnemyAbsorbOpportunity();
            return false;
        }

        StartCoroutine(
            ExecuteLastEnemyKingAbsorbRoutine(
                playerKing,
                lastEnemyPiece
            )
        );

        return true;
    }

    // <변경부분> 마지막 Enemy 1기 상태에 맞춰
    // 흡수 버튼 강조 표시를 BattleUIController에 반영한다.
    private void RefreshLastEnemyAbsorbOpportunity()
    {
        if (battleUIController == null)
        {
            return;
        }

        battleUIController.SetLastEnemyAbsorbMode(
            CanUseLastEnemyAbsorb()
        );
    }

    // <변경부분> 보드 전체에서 Player King을 찾는다.
    private Piece FindPlayerKing()
    {
        if (pieceManager == null ||
            boardManager == null)
        {
            return null;
        }

        for (int y = 0;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                Piece piece =
                    pieceManager.GetPieceAt(
                        x,
                        y
                    );

                if (piece != null &&
                    piece.Team == PieceTeam.Player &&
                    piece.PieceType == PieceType.King)
                {
                    return piece;
                }
            }
        }

        return null;
    }

    // <변경부분> Enemy 기물이 정확히 1기 남았을 때만
    // 해당 기물을 반환한다. 0기 또는 2기 이상이면 null을 반환한다.
    private Piece FindSingleRemainingEnemyPiece()
    {
        if (pieceManager == null ||
            boardManager == null)
        {
            return null;
        }

        Piece foundEnemyPiece =
            null;

        for (int y = 0;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                Piece piece =
                    pieceManager.GetPieceAt(
                        x,
                        y
                    );

                if (piece == null ||
                    piece.Team != PieceTeam.Enemy)
                {
                    continue;
                }

                // 두 번째 Enemy 기물을 찾으면
                // 마지막 1기 상태가 아니므로 즉시 실패 처리한다.
                if (foundEnemyPiece != null)
                {
                    return null;
                }

                foundEnemyPiece =
                    piece;
            }
        }

        return foundEnemyPiece;
    }

    // <변경부분> Player King이 현재 위치와 이동 규칙을 무시하고
    // 마지막 Enemy 기물 위치로 이동해 즉시 흡수 공격하는 전용 코루틴이다.
    //
    // 일반 흡수와 달리 마지막 Enemy가 King이어도 마무리 흡수가 가능하며,
    // Player King은 기존 규칙대로 타입·외형·고유스킬을 유지하고
    // 대상의 일반스킬만 흡수한다.
    private IEnumerator ExecuteLastEnemyKingAbsorbRoutine(
        Piece playerKing,
        Piece targetPiece)
    {
        if (playerKing == null ||
            targetPiece == null ||
            CanUseLastEnemyAbsorb() == false)
        {
            RefreshLastEnemyAbsorbOpportunity();
            yield break;
        }

        // 클릭 직후 중복 입력과 일반 전투 행동을 차단한다.
        isActionAnimating =
            true;

        isAbsorbMode =
            false;

        selectedPiece =
            null;

        pendingActionTile =
            null;

        pendingAttackTargetPiece =
            null;

        ClearHighlights();
        RefreshTypeIconVisuals();

        if (battleUIController != null)
        {
            battleUIController.SetLastEnemyAbsorbMode(
                false
            );

            battleUIController.SetAbsorbModeIcon(
                false
            );
        }

        int targetX =
            targetPiece.X;

        int targetY =
            targetPiece.Y;

        Tile targetTile =
            boardManager.GetTile(
                targetX,
                targetY
            );

        if (targetTile == null)
        {
            isActionAnimating =
                false;

            RefreshLastEnemyAbsorbOpportunity();
            yield break;
        }

        Vector3 targetWorldPosition =
            targetPiece.transform.position;

        PieceTeam deadPieceTeam =
            targetPiece.Team;

        bool shouldTriggerDegeneration =
            targetPiece.HasStatusEffect(
                StatusEffectType.Degeneration
            );

        PieceTeam degenerationDeadPieceTeam =
            targetPiece.Team;

        PieceType degenerationDeadPieceType =
            targetPiece.PieceType;

        Vector3 degenerationSourceWorldPosition =
            targetPiece.transform.position;

        // <변경부분> 마무리 흡수 공격 전에는
        // 마지막 적 타일 중심으로 카메라 위치만 먼저 이동시킨다.
        //
        // King이 목표 위치까지 이동하는 동안에는
        // 줌과 시간 배율을 변경하지 않는다.
        if (pixelCameraController != null)
        {
            yield return
                pixelCameraController
                    .PrepareLastPieceAttackCinematicRoutine(
                        targetTile.transform
                    );
        }

        // <변경부분> 기존 흡수 공격의 이동, Absorb, Down_Absorb,
        // 충격 픽셀 이펙트와 화면 흔들림을 그대로 재사용한다.
        yield return
    pieceManager.PlayPieceAttackMoveAnimation(
        playerKing,
        targetWorldPosition,
        true,
        () =>
        {
            // <변경부분> King이 마지막 적 위치까지 이동한 뒤
            // 내려찍기·흡수 충격 순간에 확대와 슬로우 모션을 시작한다.
            //
            // 이동 중에는 WorldRoot 배율을 바꾸지 않으므로
            // King의 이동 위치가 타일 중심에서 어긋나는 현상을 방지한다.
            if (pixelCameraController != null)
            {
                pixelCameraController
                    .StartLastPieceAttackSlowMotion();
            }

            if (targetPiece != null)
            {
                targetPiece.gameObject.SetActive(
                    false
                );
            }
        }
    );

        if (playerKing == null ||
            targetPiece == null)
        {
            isActionAnimating =
                false;

            RefreshLastEnemyAbsorbOpportunity();
            yield break;
        }

        // 마지막 Enemy가 King이면
        // Player King도 일반 기물의 완전 흡수와 동일하게
        // 대상 King의 외형 / PieceData / 고유스킬 / 일반스킬을 모두 흡수한다.
        //
        // 마지막 Enemy가 일반 기물이면
        // 기존 Player King 규칙대로 일반스킬만 흡수한다.
        bool absorbedEnemyKing =
            targetPiece.PieceType ==
            PieceType.King;

        if (absorbedEnemyKing)
        {
            pieceManager.AbsorbPiece(
                playerKing,
                targetPiece
            );

            Debug.Log(
                "마지막 Enemy King 완전 흡수: " +
                "외형 / 고유스킬 / 일반스킬을 모두 흡수했습니다."
            );
        }
        else
        {
            pieceManager.AbsorbGeneralSkillsOnly(
                playerKing,
                targetPiece
            );

            Debug.Log(
                $"마지막 Enemy 일반 흡수: " +
                $"{targetPiece.PieceType}의 일반스킬만 흡수했습니다."
            );
        }

        playerAbsorbCountThisBattle++;

        pieceManager.RemovePiece(
            targetPiece
        );

        TryTriggerDegenerationOnDeath(
            shouldTriggerDegeneration,
            degenerationDeadPieceTeam,
            degenerationDeadPieceType,
            targetX,
            targetY,
            degenerationSourceWorldPosition
        );

        AddDeathStackForUniqueSkill(
            deadPieceTeam
        );

        // 공격 연출로 도착한 위치를
        // 논리 좌표와 pieces 배열에 확정한다.
        yield return
            pieceManager.MovePieceRoutine(
                playerKing,
                targetX,
                targetY,
                false
            );

        // Enemy King을 완전 흡수해 외형이 변경된 경우
        // 일반 흡수와 동일하게 최종 위치에서 Born을 재생한다.
        if (absorbedEnemyKing)
        {
            yield return
                pieceManager.PlayPieceBornAnimation(
                    playerKing
                );
        }

        // 마무리 흡수 공격이 끝난 뒤
        // 공격 전 카메라 위치와 줌, 시간 배율로 복구한다.
        if (pixelCameraController != null)
        {
            yield return
                pixelCameraController
                    .RestoreAfterLastPieceAttackCinematicRoutine();
        }

        Debug.Log(
            $"마지막 적 강제 흡수 완료: " +
            $"Player King → ({targetX}, {targetY}) / " +
            $"누적 흡수 {playerAbsorbCountThisBattle}"
        );

        CheckBattleEnd();

        if (isBattleEnded)
        {
            isActionAnimating =
                false;

            yield break;
        }

        // 예외적으로 전투가 계속된다면 강제 흡수 행동 후 턴을 종료한다.
        isActionAnimating =
            false;

        EndTurn();
    }

    public void ToggleAbsorbMode()
    {

        // <변경부분> 초기 배치 중에는 흡수 버튼을
        // 체크 버튼으로 사용하므로 흡수 모드를 켜지 않는다.
        if (isPlayerDeploymentPhase)
        {
            return;
        }

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

        // Player King도 흡수 모드를 사용할 수 있다.
        //
        // 일반 Enemy를 흡수하면 일반스킬만 획득하고,
        // Enemy King을 흡수하면 King끼리의 완전 흡수로
        // 외형 / PieceData / 고유스킬 / 일반스킬을 모두 획득한다.

        // 흡수 모드 상태 반전
        isAbsorbMode = !isAbsorbMode;

        // <변경부분> 흡수 모드 상태에 따라 UI 아이콘 변경
        if (battleUIController != null)
        {
            battleUIController.SetAbsorbModeIcon(isAbsorbMode);
        }

        Debug.Log(isAbsorbMode ? "흡수 모드 ON" : "흡수 모드 OFF");
    }

    // <변경부분> 고유스킬 실패 메시지를 로그와 UI에 동시에 표시하는 함수
    private void ShowUniqueSkillFailMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        Debug.Log(message);

        if (battleUIController != null)
        {
            battleUIController.ShowUniqueSkillFailureMessage(message);
        }
    }

    // 현재 선택된 기물의 고유 스킬을 사용하는 함수
    public void UseSelectedPieceSkill()
    {
        // <변경부분> 초기 배치가 끝나기 전에는
        // 고유스킬을 사용할 수 없다.
        if (isPlayerDeploymentPhase)
        {
            return;
        }

        // 전투가 끝났으면 스킬 사용 불가
        if (isBattleEnded)
        {
            return;
        }

        // <변경부분> 이동/공격/스킬 연출 중에는 중복 입력 방지
        if (isActionAnimating)
        {
            ShowUniqueSkillFailMessage("현재 다른 행동이 진행 중입니다.");
            return;
        }

        // 선택된 기물이 없으면 스킬 사용 불가
        if (selectedPiece == null)
        {
            ShowUniqueSkillFailMessage("스킬을 사용할 기물을 먼저 선택해야 합니다.");
            return;
        }

        // 현재 턴의 기물이 아니면 스킬 사용 불가
        if (IsCurrentTurnPiece(selectedPiece) == false)
        {
            ShowUniqueSkillFailMessage("현재 턴의 기물만 고유스킬을 사용할 수 있습니다.");
            return;
        }

        // <변경부분> 선택된 고유스킬의 기본 데이터 가져오기
        UniqueSkillData skillData = GetUniqueSkillData(selectedPiece.UniqueSkill);

        // <변경부분> 고유스킬 데이터가 없으면 스킬 사용 불가
        if (skillData == null)
        {
            ShowUniqueSkillFailMessage("고유스킬 데이터를 찾을 수 없습니다.");
            return;
        }

        // <변경부분> 데이터에서 한 턴 1회 제한이 켜져 있고, 이미 이번 턴에 고유스킬을 사용했다면 사용 불가
        if (skillData.oncePerTurn && hasUsedUniqueSkillThisTurn)
        {
            ShowUniqueSkillFailMessage("이번 턴에는\n 이미고유스킬을 사용했습니다.");
            return;
        }

        // <변경부분> 선택된 기물의 고유 스킬 사용 가능 여부 확인
        // 여기서는 고유 스킬 없음 / 개별 쿨타임 여부를 검사
        if (selectedPiece.CanUseUniqueSkill() == false)
        {
            int cooldown = selectedPiece.GetUniqueSkillCooldown();

            if (selectedPiece.UniqueSkill == UniqueSkillType.None)
            {
                ShowUniqueSkillFailMessage("이 기물은 고유스킬이 없습니다.");
            }
            else if (cooldown > 0)
            {
                ShowUniqueSkillFailMessage($"고유스킬 쿨타임이 {cooldown}턴 남았습니다.");
            }
            else
            {
                ShowUniqueSkillFailMessage("현재 고유스킬을 사용할 수 없습니다.");
            }

            return;
        }

        if (HasEnoughDeathStackForUniqueSkill(selectedPiece.Team, skillData.requiredDeathStack) == false)
        {
            ShowUniqueSkillFailMessage("고유스킬 사용 조건이 부족합니다.");
            return;
        }

        // <변경부분> 고유스킬 실제 실행은 BattleSkillManager에 위임
        if (battleSkillManager == null)
        {
            Debug.LogWarning("BattleSkillManager가 연결되지 않아 고유스킬을 사용할 수 없습니다.");
            return;
        }

        // <변경부분> 합성처럼 애니메이션을 기다려야 하는 스킬을 위해 코루틴으로 실행
        StartCoroutine(UseSelectedPieceSkillRoutine(selectedPiece, skillData));
    }

    // <변경부분> 고유스킬 실행 코루틴
    // BattleSkillManager의 스킬 코루틴이 끝난 뒤 쿨타임/UI 갱신을 처리
    private IEnumerator UseSelectedPieceSkillRoutine(Piece skillPiece, UniqueSkillData skillData)
    {
        // 실행할 기물이 없으면 종료
        if (skillPiece == null)
        {
            yield break;
        }

        // 스킬 연출 중 추가 입력 방지
        isActionAnimating = true;

        bool skillUsed = false;

        // <변경부분> 고유스킬 데이터 아이콘을 전달하여
        // 실제 효과보다 아이콘 연출이 먼저 실행되도록 한다.
        yield return
            battleSkillManager.TryUseUniqueSkillRoutine(
                skillPiece,
                skillData.iconSprite,
                result =>
                    skillUsed = result
            );

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
                ConsumeDeathStackForUniqueSkill(skillPiece.Team, skillData.requiredDeathStack);
            }

            // <변경부분> 선택된 기물에 고유스킬 데이터 기준 쿨타임 적용
            skillPiece.MarkUniqueSkillUsed(skillData.cooldownTurn);

            // <변경부분> 고유스킬 사용 전 확인하던
            // 이동·공격 대상 상태를 초기화한다.
            pendingActionTile =
                null;

            pendingAttackTargetPiece =
                null;

            // 고유스킬 사용 후 이동 가능 타일을
            // 현재 기물 정보 기준으로 다시 갱신
            ClearHighlights();
            ShowOnlySelectedPieceTypeIcon(
                skillPiece
            );
            ShowMovableTiles(
                skillPiece
            );

            // <변경부분> 고유스킬 사용 후 버튼과 스테이터스 UI를 현재 기물 정보 기준으로 다시 갱신
            if (battleUIController != null)
            {
                battleUIController.RefreshSelectedPieceButtons(skillPiece);
            }

            Debug.Log($"고유 스킬 사용 완료: {skillPiece.UniqueSkill} / 쿨타임 {skillData.cooldownTurn}");
        }
        else
        {
            // <변경부분> 스킬 내부 조건이 맞지 않아 발동하지 못한 경우
            string failMessage = "조건이 맞지 않아 사용할 수 없습니다.";

            if (skillData != null && string.IsNullOrEmpty(skillData.conditionFailMessage) == false)
            {
                failMessage = skillData.conditionFailMessage;
            }

            ShowUniqueSkillFailMessage(failMessage);
        }

        // 스킬 연출 종료 후 다시 입력 허용
        isActionAnimating = false;
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

    // <변경부분> 디버그 버튼에서 테스트 아이템 추가를 요청하는 함수
    // BattleUIController는 BattleItemManager를 직접 알지 않고 BattleManager를 통해 요청한다.
    public void AddTestItemForDebug()
    {
        // <변경부분> 전투 아이템 매니저가 연결되지 않았으면 테스트 아이템 추가 불가
        if (battleItemManager == null)
        {
            Debug.LogWarning("BattleItemManager가 연결되지 않았습니다.");
            return;
        }

        // <변경부분> 실제 테스트 아이템 추가는 BattleItemManager가 처리
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
        // <변경부분> 초기 배치가 끝나기 전에는
        // 전투 아이템을 사용할 수 없다.
        if (isPlayerDeploymentPhase)
        {
            return false;
        }

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

        // <변경부분> 아이템 사용 후
        // 이전 이동·공격 확인 상태를 초기화한다.
        pendingActionTile =
            null;

        pendingAttackTargetPiece =
            null;

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

    // <변경부분> 현재 보유 중인 유물 데이터를 BattleRelicManager에서 가져오는 함수
    public BattleRelicData GetRelicData(BattleRelicType relicType)
    {
        if (battleRelicManager == null)
        {
            return null;
        }

        return battleRelicManager.GetRelicData(relicType);
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

    // <변경부분> 선택한 기물의 이동 및 공격 가능한 타일을 표시하는 함수
    // 실제 이동 규칙은 BattleMoveValidator 한곳에서만 계산한다.
    // 플레이어 하이라이트와 AI 행동 후보가 동일한 좌표 결과를 공유한다.
    private void ShowMovableTiles(Piece piece)
    {
        // 기물이 없으면 표시할 좌표가 없다.
        if (piece == null)
        {
            return;
        }

        // 공용 이동 판정기가 연결되지 않았다면 하이라이트를 생성할 수 없다.
        if (battleMoveValidator == null)
        {
            Debug.LogWarning(
                "이동 가능 타일 표시 실패: BattleMoveValidator가 연결되지 않았습니다."
            );

            return;
        }

        // 현재 이동 타입과 보드 상태를 반영한
        // 모든 합법 이동 및 공격 좌표를 공용 판정기에서 가져온다.
        List<Vector2Int> selectablePositions =
            battleMoveValidator.GetSelectablePositions(piece);

        for (int i = 0; i < selectablePositions.Count; i++)
        {
            Vector2Int position =
                selectablePositions[i];

            HighlightTile(
                position.x,
                position.y
            );
        }
    }

    // <변경부분> 모든 기물 타입 아이콘 표시 상태 전환
    public void ToggleTypeIcons()
    {
        // 기물 타입 아이콘 표시 버튼 위치에서
        // 검은 픽셀 파티클 재생
        PlayTypeIconButtonPixelBurst();

        // 전체 타입 아이콘 토글 상태 반전
        isTypeIconVisible =
            !isTypeIconVisible;

        // <변경부분> 선택 상태와 상대 정보 확인 상태까지 포함해
        // 전체 보드의 타입 아이콘 표시를 다시 계산한다.
        RefreshTypeIconVisuals();
    }

    // <변경부분> 기물 타입 아이콘 표시 버튼 위치에서 검은 픽셀 파티클을 재생하는 함수
    private void PlayTypeIconButtonPixelBurst()
    {
        if (battleUIController == null)
        {
            return;
        }

        if (typeIconButton == null)
        {
            return;
        }

        RectTransform typeIconButtonRectTransform = typeIconButton.GetComponent<RectTransform>();

        if (typeIconButtonRectTransform == null)
        {
            return;
        }

        battleUIController.PlayIconPixelBurstAt(typeIconButtonRectTransform);
    }

    // <변경부분> 전체 타입 아이콘 토글,
    // 현재 선택 기물과 상대 확인 대상을 기준으로
    // 타입 아이콘 표시, 선택 위치, 비선택 알파값을 갱신한다.
    private void RefreshTypeIconVisuals()
    {
        if (boardManager == null ||
            pieceManager == null)
        {
            return;
        }

        // Player 기물이 선택되어 있으면
        // 전체 토글이 꺼져 있어도 Enemy 타입 아이콘 전체를 표시한다.
        bool shouldShowAllEnemyTypeIcons =
            selectedPiece != null &&
            selectedPiece.Team == PieceTeam.Player;

        // <변경부분> 현재 선택된 기물이 하나라도 있는지 확인한다.
        // 조작 기물 또는 상대 확인 대상 중 하나라도 있으면
        // 선택되지 않은 표시 중 아이콘을 반투명하게 처리한다.
        bool hasSelectedTypeIcon =
            selectedPiece != null ||
            pendingAttackTargetPiece != null;

        for (int x = 0;
             x < boardManager.Width;
             x++)
        {
            for (int y = 0;
                 y < boardManager.Height;
                 y++)
            {
                Piece piece =
                    pieceManager.GetPieceAt(
                        x,
                        y
                    );

                if (piece == null)
                {
                    continue;
                }

                // 다음 조건 중 하나라도 만족하면
                // 해당 기물의 타입 아이콘을 표시한다.
                bool shouldShowTypeIcon =
                    isTypeIconVisible ||
                    piece == selectedPiece ||
                    piece == pendingAttackTargetPiece ||
                    (
                        shouldShowAllEnemyTypeIcons &&
                        piece.Team == PieceTeam.Enemy
                    );

                // 현재 직접 선택된 조작 기물 또는
                // 정보·공격 확인 대상으로 선택된 상대 기물인지 확인한다.
                bool isSelectedTypeIcon =
                    piece == selectedPiece ||
                    piece == pendingAttackTargetPiece;

                piece.SetTypeIconVisible(
                    shouldShowTypeIcon
                );

                // 선택된 기물은 기존처럼 살짝 위로 올라가고,
                // 선택 해제 시 기본 위치로 내려온다.
                piece.SetTypeIconSelected(
                    isSelectedTypeIcon
                );

                // <변경부분> 타입 아이콘 정렬 우선순위를 계산한다.
                //
                // 기본 기물은 우선순위 0,
                // 공격 확인 대상은 우선순위 1,
                // 실제 공격하는 selectedPiece는 우선순위 2로 적용한다.
                int typeIconSortingPriority = 0;

                if (piece == pendingAttackTargetPiece)
                {
                    // 공격받는 대상은 일반 기물보다 앞으로 표시한다.
                    typeIconSortingPriority = 1;
                }

                if (piece == selectedPiece)
                {
                    // 공격 대상이 함께 선택된 공격 확인 상태에서는
                    // 공격하는 기물을 대상보다 한 단계 더 앞으로 표시한다.
                    typeIconSortingPriority =
                        pendingAttackTargetPiece != null
                            ? 2
                            : 1;
                }

                piece.SetTypeIconSortingPriority(
                    typeIconSortingPriority
                );

                // <변경부분> 선택된 기물이 하나 이상 있을 때만,
                // 표시 중이면서 선택되지 않은 타입 아이콘을 반투명 처리한다.
                bool shouldDimTypeIcon =
                    shouldShowTypeIcon &&
                    hasSelectedTypeIcon &&
                    isSelectedTypeIcon == false;

                piece.SetTypeIconDimmed(
                    shouldDimTypeIcon
                );
            }
        }
    }

    // <변경부분> 기존 스킬·아이템·찬스어택 코드의 호출 위치를 유지하면서
    // 지정된 기물을 현재 선택 기물로 설정하고
    // 전체 타입 아이콘 표시 상태를 다시 계산한다.
    private void ShowOnlySelectedPieceTypeIcon(
        Piece piece)
    {
        selectedPiece =
            piece;

        RefreshTypeIconVisuals();
    }

    // <변경부분> 퇴화 상태의 기물이 잡혔을 때
    // 인접한 빈칸에 중립 젤루 Special 기물을 생성한다.
    //
    // 생성되는 기물은 비숍의 JelluWall 스킬과 동일하다.
    // 죽은 기물의 진영과 관계없이 항상 Neutral로 생성되며,
    // 이동할 수 없고 다른 진영이 공격할 수 있는 장애물 역할을 한다.
    private void TryTriggerDegenerationOnDeath(
        bool shouldTriggerDegeneration,
        PieceTeam deadPieceTeam,
        PieceType deadPieceType,
        int deadPieceX,
        int deadPieceY,
        Vector3 sourceWorldPosition)
    {
        // 퇴화 상태가 아니었다면 처리하지 않는다.
        if (shouldTriggerDegeneration == false)
        {
            return;
        }

        // 중립 기물은 현재 퇴화 스킬 사용 대상이 아니다.
        if (deadPieceTeam ==
            PieceTeam.Neutral)
        {
            return;
        }

        List<Vector2Int> emptyPositions =
            new List<Vector2Int>();

        // 사망 위치 주변 8방향을 검사한다.
        for (int offsetY = -1;
             offsetY <= 1;
             offsetY++)
        {
            for (int offsetX = -1;
                 offsetX <= 1;
                 offsetX++)
            {
                // 퇴화는 사망 위치가 아니라
                // 인접한 빈칸에 기물을 생성한다.
                if (offsetX == 0 &&
                    offsetY == 0)
                {
                    continue;
                }

                int targetX =
                    deadPieceX +
                    offsetX;

                int targetY =
                    deadPieceY +
                    offsetY;

                // 보드 밖 좌표는 제외한다.
                if (IsInsideBoard(
                        targetX,
                        targetY) ==
                    false)
                {
                    continue;
                }

                // 실제로 비어 있는 좌표만 생성 후보로 저장한다.
                if (pieceManager.IsEmpty(
                        targetX,
                        targetY))
                {
                    emptyPositions.Add(
                        new Vector2Int(
                            targetX,
                            targetY
                        )
                    );
                }
            }
        }

        // 인접한 빈칸이 없으면 퇴화 효과는 발동하지 않는다.
        if (emptyPositions.Count == 0)
        {
            Debug.Log(
                "퇴화 발동 실패: " +
                "인접한 빈칸이 없습니다."
            );

            return;
        }

        // 인접 빈칸 중 하나를 무작위로 선택한다.
        int randomIndex =
            Random.Range(
                0,
                emptyPositions.Count
            );

        Vector2Int selectedPosition =
            emptyPositions[randomIndex];

        // <변경부분> 비숍의 JelluWall과 동일한
        // Neutral / Special / Jellu 기물을 생성한다.
        //
        // 사망한 기물은 이미 제거될 수 있으므로
        // 저장된 사망 월드 위치에서 생성 연출을 시작한다.
        Piece createdPiece =
            pieceManager
                .SpawnJelluWallFromWorldPosition(
                    selectedPosition.x,
                    selectedPosition.y,
                    sourceWorldPosition
                );

        if (createdPiece == null)
        {
            Debug.LogWarning(
                "퇴화 발동 실패: " +
                "중립 젤루 기물 생성에 실패했습니다."
            );

            return;
        }

        Debug.Log(
            $"퇴화 발동: " +
            $"{deadPieceTeam} {deadPieceType} 사망 → " +
            $"({selectedPosition.x}, {selectedPosition.y})에 " +
            $"중립 젤루 Special 생성"
        );
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

        // 찬스어택 발동 기물을 추가 행동 기물로 선택
        selectedPiece = piece;

        // <변경부분> 추가 행동으로 자동 선택된 기물도 Select → Select_Idle 상태로 표시
        pieceManager.PlayPieceSelectAnimation(selectedPiece);

        // <변경부분> 이전 행동 확인 상태를 초기화한다.
        pendingActionTile =
            null;

        pendingAttackTargetPiece =
            null;

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

    // <변경부분> 흡수 성공 시 유물 효과로 찬스어택을 발동할 수 있는지 검사하는 함수
    private bool TryActivateAbsorbChanceAttackRelic(Piece piece)
    {
        // 유물 효과 핸들러가 없으면 유물 효과 발동 불가
        if (battleRelicEffectHandler == null)
        {
            Debug.LogWarning("BattleRelicEffectHandler가 연결되지 않았습니다.");
            return false;
        }

        // <변경부분> 현재 보유 중인 흡수 찬스어택 유물 데이터를 가져옴
        BattleRelicData relicData = GetRelicData(BattleRelicType.AbsorbChanceAttackOncePerTurn);

        if (relicData == null)
        {
            return false;
        }

        // <변경부분> 유물 데이터 기준으로 추가 행동 가능한 타일 필요 여부를 고려하기 위한 현재 상태 계산
        bool hasAnySelectableTile = battleMoveValidator != null && battleMoveValidator.HasAnySelectableTile(piece);

        // <변경부분> 실제 유물 효과 발동 조건/확률 판정은 BattleRelicEffectHandler에 요청
        return battleRelicEffectHandler.CanActivateAbsorbChanceAttackRelic(
            relicData,
            piece,
            currentTurn,
            hasUsedAbsorbChanceAttackRelicThisTurn,
            hasAnySelectableTile
        );
    }

    // <변경부분> Enemy AI가 ChanceAttack 추가 행동을
    // 더 이상 정상적으로 진행할 수 없을 때 추가 행동 상태를 정리하고
    // 현재 Enemy 턴을 안전하게 종료한다.
    public void FinishEnemyAIChanceAttackTurn()
    {
        if (isBattleEnded)
        {
            return;
        }

        if (currentTurn != BattleTurn.Enemy)
        {
            Debug.LogWarning(
                "Enemy AI 추가 행동 종료 실패: " +
                "현재 턴이 Enemy가 아닙니다."
            );

            return;
        }

        // 추가 행동 제한 상태 초기화
        chanceAttackBonusPiece = null;

        // 연속 ChanceAttack 발동 횟수 초기화
        chanceAttackContinuousCount = 0;

        // 선택 및 공격 확인 상태 초기화
        selectedPiece = null;
        pendingAttackTargetPiece = null;

        // 남아 있는 하이라이트 제거
        ClearHighlights();

        // 타입 아이콘 표시 상태 갱신
        RefreshTypeIconVisuals();

        Debug.Log(
            "Enemy AI ChanceAttack 추가 행동 상태를 정리하고 턴을 종료합니다."
        );

        EndTurn();
    }

    // <변경부분> 지정 진영에 행동 후보가 없을 때
    // 승패 조건을 확인하고 전투가 끝나지 않았다면 턴을 넘긴다.
    public void ResolveNoActionableTurn(
        PieceTeam actingTeam)
    {
        PieceTeam currentTurnTeam =
            currentTurn == BattleTurn.Player
                ? PieceTeam.Player
                : PieceTeam.Enemy;

        // 현재 턴 진영과 요청 진영이 다르면 처리하지 않는다.
        if (actingTeam != currentTurnTeam)
        {
            Debug.LogWarning(
                $"행동 불가 턴 처리 실패: " +
                $"현재 턴 진영은 {currentTurnTeam}이지만 " +
                $"요청 진영은 {actingTeam}입니다."
            );

            return;
        }

        // NoActionablePieces를 포함한 현재 승패 조건을 다시 검사한다.
        CheckBattleEnd();

        // 승패 조건으로 전투가 끝났다면 턴을 넘기지 않는다.
        if (isBattleEnded)
        {
            return;
        }

        Debug.Log(
            $"{actingTeam} 진영에 실행 가능한 행동이 없어 턴을 종료합니다."
        );

        EndTurn();
    }

    // <변경부분> AI 행동 이후 지정한 진영의 King이
    // 상대 공격 범위에 노출되는지 공용 이동 판정기에 요청한다.
    public bool IsKingThreatenedAfterAIAction(
        BattleAIAction action,
        PieceTeam kingTeam)
    {
        if (battleMoveValidator == null)
        {
            Debug.LogWarning(
                "AI King 위험도 판정 실패: " +
                "BattleMoveValidator가 연결되지 않았습니다."
            );

            return false;
        }

        return battleMoveValidator
            .IsKingThreatenedAfterAction(
                action,
                kingTeam
            );
    }

    // <변경부분> 지정한 기물이 이동하기 전 현재 위치에서
    // 상대 공격 범위에 노출되어 있는지 공용 이동 판정기에 요청한다.
    // 퇴화 AI의 선제 사용 조건을 판정할 때 사용한다.
    public bool IsPieceCurrentlyThreatened(
        Piece targetPiece)
    {
        if (battleMoveValidator == null)
        {
            Debug.LogWarning(
                "현재 기물 위험도 판정 실패: " +
                "BattleMoveValidator가 연결되지 않았습니다."
            );

            return false;
        }

        return battleMoveValidator
            .IsPieceCurrentlyThreatened(
                targetPiece
            );
    }

    // <변경부분> AI 행동 이후 행동한 기물이
    // 상대 기본 공격 범위에 노출되는지 공용 이동 판정기에 요청한다.
    public bool IsActingPieceThreatenedAfterAIAction(
        BattleAIAction action)
    {
        if (battleMoveValidator == null)
        {
            Debug.LogWarning(
                "AI 행동 기물 위험도 판정 실패: " +
                "BattleMoveValidator가 연결되지 않았습니다."
            );

            return false;
        }

        return battleMoveValidator
            .IsActingPieceThreatenedAfterAction(
                action
            );
    }

    // <변경부분> 지정한 좌표에서 특정 진영 King까지의
    // 맨해튼 거리를 반환한다.
    // AI가 Player King 쪽으로 전진하는 행동을 평가할 때 사용한다.
    public int GetDistanceToKing(
        Vector2Int position,
        PieceTeam kingTeam)
    {
        if (boardManager == null ||
            pieceManager == null)
        {
            Debug.LogWarning(
                "King 거리 계산 실패: " +
                "BoardManager 또는 PieceManager가 연결되지 않았습니다."
            );

            return -1;
        }

        // 보드 전체에서 지정한 진영의 King을 찾는다.
        for (int y = 0;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                Piece piece =
                    pieceManager.GetPieceAt(
                        x,
                        y
                    );

                if (piece == null)
                {
                    continue;
                }

                if (piece.Team != kingTeam)
                {
                    continue;
                }

                if (piece.PieceType != PieceType.King)
                {
                    continue;
                }

                // 가로 거리와 세로 거리를 더한
                // 맨해튼 거리를 반환한다.
                return
                    Mathf.Abs(position.x - x) +
                    Mathf.Abs(position.y - y);
            }
        }

        // King이 존재하지 않는 전투에서는
        // 전진 압박 점수를 계산하지 않는다.
        return -1;
    }


    // <변경부분> AI 고유스킬 행동이 현재 전투 상태에서
    // 실제로 사용할 수 있는 후보인지 검사한다.
    //
    // 후보 평가 전에 호출하여 사용 불가능한 고유스킬이
    // 최고 점수로 선택된 뒤 실행 실패하는 상황을 방지한다.
    private bool CanUseAIUniqueSkillAction(
        BattleAIAction action)
    {
        if (action == null ||
            action.ActionType !=
                BattleAIActionType.UniqueSkill ||
            action.ActingPiece == null)
        {
            return false;
        }

        Piece skillPiece =
            action.ActingPiece;

        // 전투가 종료됐거나 다른 행동이 진행 중이면 사용할 수 없다.
        if (isBattleEnded ||
            isActionAnimating)
        {
            return false;
        }

        // 현재 턴 진영의 기물만 고유스킬을 사용할 수 있다.
        if (IsCurrentTurnPiece(
                skillPiece) ==
            false)
        {
            return false;
        }

        // 행동 데이터와 기물이 실제로 보유한 스킬이 일치해야 한다.
        if (skillPiece.UniqueSkill !=
            action.UniqueSkillType)
        {
            return false;
        }

        // 개별 쿨타임과 기물별 이번 턴 사용 상태를 검사한다.
        if (skillPiece.CanUseUniqueSkill() ==
            false)
        {
            return false;
        }

        UniqueSkillData skillData =
            GetUniqueSkillData(
                action.UniqueSkillType
            );

        if (skillData == null)
        {
            return false;
        }

        // 데이터에서 턴당 1회 제한을 사용하는 스킬이면
        // 같은 진영 턴에 다른 고유스킬을 이미 사용한 경우 제외한다.
        if (skillData.oncePerTurn &&
            hasUsedUniqueSkillThisTurn)
        {
            return false;
        }

        // 필요한 사망 스택이 부족하면 후보에서 제외한다.
        if (HasEnoughDeathStackForUniqueSkill(
                skillPiece.Team,
                skillData.requiredDeathStack) ==
            false)
        {
            return false;
        }

        if (battleSkillManager == null)
        {
            return false;
        }

        return true;
    }

    // <변경부분> 지정한 진영의 현재 합법적인 AI 행동 후보를 생성한다.
    //
    // 이동 및 공격 후보는 BattleAIActionGenerator 결과를 그대로 사용하고,
    // 고유스킬 후보는 BattleManager의 턴 전체 제한,
    // 스택, 쿨타임, 실제 데이터 조건까지 다시 검사한다.
    public void GenerateAIActions(
        PieceTeam actingTeam,
        List<BattleAIAction> results)
    {
        if (results == null)
        {
            Debug.LogWarning(
                "AI 행동 후보 생성 실패: 결과 목록이 없습니다."
            );

            return;
        }

        if (battleAIActionGenerator == null)
        {
            results.Clear();

            Debug.LogWarning(
                "AI 행동 후보 생성 실패: " +
                "BattleAIActionGenerator가 초기화되지 않았습니다."
            );

            return;
        }

        // 이동, 공격, 기본 고유스킬 후보를 생성한다.
        battleAIActionGenerator.GenerateActions(
            actingTeam,
            results
        );

        // <변경부분> 생성기에서 확인할 수 없는
        // BattleManager 전투 상태 조건을 적용한다.
        //
        // 뒤에서부터 제거하여 인덱스 변경 문제를 피한다.
        for (int i = results.Count - 1;
             i >= 0;
             i--)
        {
            BattleAIAction action =
                results[i];

            if (action == null)
            {
                results.RemoveAt(i);
                continue;
            }

            if (action.ActionType !=
                BattleAIActionType.UniqueSkill)
            {
                continue;
            }

            if (CanUseAIUniqueSkillAction(
                    action) ==
                false)
            {
                results.RemoveAt(i);
            }
        }
    }

    // <변경부분> 지정한 기물만 사용할 수 있는
    // 현재 합법적인 AI 행동 후보를 생성한다.
    //
    // ChanceAttack 추가 행동에서는 다른 Enemy 기물이 아니라
    // 추가 행동을 획득한 동일 기물만 다시 행동해야 하므로 사용한다.
    public void GenerateAIActionsForPiece(
        Piece actingPiece,
        List<BattleAIAction> results)
    {
        if (results == null)
        {
            Debug.LogWarning(
                "기물 지정 AI 행동 후보 생성 실패: " +
                "결과 목록이 없습니다."
            );

            return;
        }

        results.Clear();

        if (actingPiece == null)
        {
            Debug.LogWarning(
                "기물 지정 AI 행동 후보 생성 실패: " +
                "행동 기물이 없습니다."
            );

            return;
        }

        // 우선 현재 진영의 전체 합법 행동 후보를 생성한다.
        GenerateAIActions(
            actingPiece.Team,
            results
        );

        // 뒤에서부터 검사하면서 지정한 기물이 아닌
        // 다른 기물의 행동 후보를 제거한다.
        for (int i = results.Count - 1;
             i >= 0;
             i--)
        {
            BattleAIAction action =
                results[i];

            if (action == null ||
                action.ActingPiece != actingPiece)
            {
                results.RemoveAt(i);
            }
        }
    }

    // <변경부분> 현재 보드 상태를 기준으로
    // Enemy 진영의 모든 합법 이동 및 공격 후보를 생성해 확인하는 함수
    public void DebugGenerateEnemyAIActions()
    {
        // 전투가 종료된 뒤에는 AI 후보를 생성하지 않는다.
        if (isBattleEnded)
        {
            Debug.Log(
                "AI 행동 후보 생성 중단: 전투가 이미 종료되었습니다."
            );

            return;
        }

        // AI 행동 생성기가 초기화되지 않았다면 테스트할 수 없다.
        if (battleAIActionGenerator == null)
        {
            Debug.LogWarning(
                "AI 행동 후보 생성 실패: BattleAIActionGenerator가 초기화되지 않았습니다."
            );

            return;
        }

        GenerateAIActions(
     PieceTeam.Enemy,
     battleAIActionCandidates
 );

        // 생성된 후보의 수와 세부 내용을 Console에 출력한다.
        battleAIActionGenerator.DebugLogActions(
            PieceTeam.Enemy,
            battleAIActionCandidates
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

        // 턴이 바뀌면 현재 상대 기물 위에 표시된
        // 필드 흡수 버튼을 즉시 초기화한다.
        ClearFieldAbsorbOpportunity();

        // 턴 종료 시 이동·공격 가능 타일과
        // 첫 번째 클릭 확인 상태 제거
        ClearHighlights();

        pendingActionTile =
            null;

        // 턴 종료 시 공격 확인 대상 초기화
        pendingAttackTargetPiece =
            null;

        // 선택된 기물 해제
        selectedPiece = null;

        // 흡수 모드 해제
        isAbsorbMode = false;

        // 모든 타입 아이콘 비활성화
        RefreshTypeIconVisuals();

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

        // <변경부분> 턴이 바뀐 뒤 현재 턴 진영의 상태이상 유지 턴 감소
        // 퇴화 1턴 유지: 자기 턴에 사용하면 상대 턴 동안 유지되고, 자기 다음 턴 시작 시 만료
        UpdateAllStatusEffectTurnState();

        // 턴 변경 후 UI 갱신
        if (turnInfoUIController != null)
        {
            turnInfoUIController.RefreshTurnInfo(turnCount, currentTurn);
        }

        Debug.Log($"턴 변경: Turn {turnCount} / {currentTurn}");

        // <변경부분> 새 턴의 주체와 현재 Enemy 기물 수를 기준으로
        // 마지막 적 강제 흡수 버튼 표시 여부를 갱신한다.
        RefreshLastEnemyAbsorbOpportunity();

        // <변경부분> 턴 상태 갱신이 모두 끝난 뒤
        // 새 턴 주체를 AI 매니저에 전달한다.
        if (battleAIManager != null)
        {
            battleAIManager.HandleTurnStarted(
                currentTurn
            );
        }
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

    // <변경부분> 턴 시작 시 현재 턴 진영 기물의 상태이상 유지 턴을 감소시키는 함수
    private void UpdateAllStatusEffectTurnState()
    {
        // 현재 턴 주체 진영 계산
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

                // 현재 턴 진영의 기물만 상태이상 턴 감소
                // 예: Player가 자기 턴에 퇴화를 얻으면 Enemy 턴 동안 유지되고,
                // 다음 Player 턴 시작 시 1턴이 감소하면서 만료됨
                if (piece.Team != currentTurnTeam)
                {
                    continue;
                }

                // 상태이상 턴 감소 및 만료 처리
                piece.ReduceStatusEffectTurnAndRemoveExpired();
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

    // <변경부분> 행동이 확정된 타일 하나만 남기고
    // 나머지 이동·공격 가능 하이라이트를 제거한다.
    private void ClearHighlightsExcept(
        Tile selectedTile)
    {
        // 선택한 타일이 없으면 전체 하이라이트 제거
        if (selectedTile == null)
        {
            ClearHighlights();
            return;
        }

        // 선택한 타일 외의 모든 하이라이트를
        // 원래 타일 색상으로 복구한다.
        foreach (Tile highlightedTile in
                 highlightedTiles)
        {
            if (highlightedTile == null)
            {
                continue;
            }

            if (highlightedTile !=
                selectedTile)
            {
                highlightedTile
                    .HideHighlight();
            }
        }

        // 선택한 타일은 행동 연출이 시작될 때까지
        // 확인 색상 상태로 유지한다.
        selectedTile
            .ShowActionConfirmHighlight();

        // 이후 후처리에서 선택 타일 하나만 정리할 수 있도록
        // 하이라이트 목록을 다시 구성한다.
        highlightedTiles.Clear();

        highlightedTiles.Add(
            selectedTile
        );

        // 행동 실행이 확정되었으므로
        // 더 이상 다른 타일을 선택할 수 없도록 비운다.
        selectableTiles.Clear();

        // 확인 단계가 끝났으므로 참조는 초기화한다.
        pendingActionTile =
            null;
    }

    // <변경부분> 모든 이동·공격 하이라이트와
    // 현재 확인 중인 행동 타일을 초기화한다.
    private void ClearHighlights()
    {
        // 하이라이트된 타일 전부 원래 색으로 복구
        foreach (Tile tile in highlightedTiles)
        {
            if (tile == null)
            {
                continue;
            }

            tile.HideHighlight();
        }

        // 하이라이트 목록 비우기
        highlightedTiles.Clear();

        // 선택 가능 타일 목록 비우기
        selectableTiles.Clear();

        // 첫 번째 클릭으로 확인 중이던 타일 초기화
        pendingActionTile =
            null;
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

        // <변경부분> 현재 Event Sequence에서
        // 일반 Battle 승패 판정을 사용하지 않도록 설정했다면
        // King 사망 / 전체 기물 사망 / 행동 불가 등의
        // 기존 승패 조건을 전부 무시한다.
        //
        // 이벤트 완료 여부는 EventSequenceController의
        // CompleteSequence Step에서 별도로 결정한다.
        if (eventSequenceController != null &&
            eventSequenceController.ShouldIgnoreNormalBattleEnd)
        {
            return;
        }

        // <변경부분> 플레이어 진영이 설정된 패배 조건 중 하나라도 만족했는지 확인
        bool isPlayerDefeated = IsTeamDefeated(PieceTeam.Player, playerDefeatCondition);

        // <변경부분> 적 진영이 설정된 패배 조건 중 하나라도 만족했는지 확인
        bool isEnemyDefeated = IsTeamDefeated(PieceTeam.Enemy, enemyDefeatCondition);

        if (isPlayerDefeated && isEnemyDefeated)
        {
            Debug.Log("전투 종료: 양쪽 모두 패배 조건 충족 / 플레이어 패배 우선 처리");
            EndBattle(BattleResult.Lose);
            return;
        }

        if (isPlayerDefeated)
        {
            EndBattle(BattleResult.Lose);
            return;
        }

        if (isEnemyDefeated)
        {
            EndBattle(BattleResult.Win);
            return;
        }

        // <변경부분> 전투가 끝나지 않았다면
        // 현재 Enemy 기물 수에 맞춰 마지막 적 흡수 UI를 갱신한다.
        RefreshLastEnemyAbsorbOpportunity();
    }

    // <변경부분> 지정한 진영이 설정된 패배 조건 중 하나라도 만족했는지 확인하는 함수
    private bool IsTeamDefeated(PieceTeam team, BattleDefeatConditionType defeatConditions)
    {
        if (defeatConditions == BattleDefeatConditionType.None)
        {
            return false;
        }

        if (defeatConditions.HasFlag(BattleDefeatConditionType.KingDeath) &&
            pieceManager.HasKing(team) == false)
        {
            return true;
        }

        if (defeatConditions.HasFlag(BattleDefeatConditionType.AllPiecesDead) &&
            pieceManager.HasAnyPiece(team) == false)
        {
            return true;
        }

        // "King을 제외한 모든 기물이 사망하면 패배" 조건은
        // Player 진영에게만 적용한다.
        //
        // Enemy는 King 한 기만 남더라도 전투를 계속해서
        // Player King의 마지막 일격 / King 흡수까지 진행할 수 있다.
        if (team == PieceTeam.Player &&
            defeatConditions.HasFlag(
                BattleDefeatConditionType.AllNonKingPiecesDead) &&
            pieceManager.HasAnyNonKingPiece(
                PieceTeam.Player) ==
                false)
        {
            return true;
        }

        if (defeatConditions.HasFlag(BattleDefeatConditionType.NoActionablePieces) &&
            HasNoActionablePieces(team))
        {
            return true;
        }

        return false;
    }

    // <변경부분> 해당 진영에 실제 이동/공격 가능한 기물이 하나도 없는지 확인
    private bool HasNoActionablePieces(PieceTeam team)
    {
        if (pieceManager == null || battleMoveValidator == null || boardManager == null)
        {
            return false;
        }

        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece piece = pieceManager.GetPieceAt(x, y);

                if (piece == null)
                {
                    continue;
                }

                if (piece.Team != team)
                {
                    continue;
                }

                if (piece.CanMove == false)
                {
                    continue;
                }

                if (battleMoveValidator.HasAnySelectableTile(piece))
                {
                    return false;
                }
            }
        }

        return true;
    }

   
    // <변경부분> 전투 종료 후 플레이어 기물 상태를 RunStateManager에 저장하는 함수
    private void SavePlayerPiecesToRunState()
    {
        if (pieceManager == null)
        {
            Debug.LogWarning("플레이어 기물 상태 저장 실패: PieceManager가 연결되지 않았습니다.");
            return;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning("플레이어 기물 상태 저장 실패: RunStateManager가 씬에 없습니다.");
            return;
        }

        List<PlayerPieceRuntimeData> runtimeDataList = pieceManager.CapturePlayerPieceRuntimeData();

        RunStateManager.Instance.SavePlayerPieces(runtimeDataList);
    }

    // <변경부분> 전투 종료 후 보상 정산 / 맵 복귀 흐름을 BattleEndFlowController에 전달하는 함수
    private void NotifyBattleEndFlow(BattleResult result)
    {
        if (battleEndFlowController == null)
        {
            Debug.LogWarning("전투 종료 흐름 처리 실패: BattleEndFlowController가 연결되지 않았습니다.");
            return;
        }

        battleEndFlowController.HandleBattleEnd(result, playerAbsorbCountThisBattle);
    }


    // 전투를 종료하는 함수
    private void EndBattle(BattleResult result)
    {
        // 전투 종료 상태 저장
        battleResult = result;
        isBattleEnded = true;

        // 선택 해제
        selectedPiece = null;

        // <변경부분> 전투 종료 시 마지막 적 흡수 강조 연출을 먼저 중단하고
        // 모든 액션 버튼을 숨긴다.
        if (battleUIController != null)
        {
            battleUIController.SetLastEnemyAbsorbMode(
                false
            );

            battleUIController.HideActionButtons();
        }

        // 하이라이트 제거
        ClearHighlights();

        // 결과 출력
        if (battleResult == BattleResult.Win)
        {
            // <변경부분> 전투 승리 시 현재 플레이어 기물 상태를 런 상태에 저장
            SavePlayerPiecesToRunState();

            Debug.Log("전투 승리: 적 진영 패배 조건 충족 / 플레이어 기물 상태 저장 완료");

            // <변경부분> 저장 완료 후 보상 정산 / 맵 복귀 흐름으로 전달
            NotifyBattleEndFlow(battleResult);
        }
        else if (battleResult == BattleResult.Lose)
        {
            Debug.Log("전투 패배: 플레이어 진영 패배 조건 충족");

            // <변경부분> 패배 결과를 전투 종료 흐름으로 전달
            NotifyBattleEndFlow(battleResult);
        }
    }

    // 좌표가 보드 안인지 확인
    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < boardManager.Width && y >= 0 && y < boardManager.Height;
    }
}
