using System.Collections.Generic; // <변경부분> RunStateManager의 플레이어 기물 목록을 가져올 때 사용
using UnityEngine;

// <변경부분> StageBattleData를 읽어서 전투 시작 구성을 세팅하는 매니저
// 보드 타일, 적 기물 편성, 승패 조건, 적 일반스킬 랜덤 부여를 한곳에서 연결한다.
public class BattleSetupManager : MonoBehaviour
{
    [Header("Stage Data")]
    // <변경부분> 현재 맵 노드에서 전달받아
    // 이번 BattleScene에서 실제로 사용할 StageBattleData.
    //
    // 일반 게임 진행에서는
    // WorldMapRuntimeState.PendingStageBattleData를 우선 사용한다.
    private StageBattleData stageBattleData;

    [Header("Direct Scene Test")]
    // <변경부분> BattleScene을 월드맵을 거치지 않고
    // Unity Editor에서 직접 Play할 때 사용할 테스트용 StageBattleData.
    //
    // 일반 게임에서는 사용되지 않으며,
    // WorldMapRuntimeState.PendingStageBattleData가 없는 경우에만 사용한다.
    [SerializeField]
    private StageBattleData directTestStageBattleData;

    [Header("Managers")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PieceManager pieceManager;
    [SerializeField] private BattleManager battleManager;

    // <변경부분> StageBattleData에 연결된 BackgroundMapData를
    // 실제 BattleScene 배경으로 불러올 BackgroundManager.
    [SerializeField]
    private BackgroundManager backgroundManager;

    // <변경부분>
    // EventScene에서 검증한 타일/기물 등장 애니메이터를
    // BattleScene에서도 그대로 재사용한다.
    [Header("Battle Board Intro")]
    [SerializeField] private bool playBattleBoardIntro = true;
    [SerializeField]
    private EventSceneFormationIntroAnimator battleBoardIntroAnimator;

    // <변경부분>
    // BattleScene 시작 카메라 확대가 완료된 뒤
    // 타일과 기물 등장 애니메이션을 실행하기 위한 참조.
    // Inspector 연결이 없으면 실행 시 자동으로 검색한다.
    [SerializeField]
    private PixelCameraController battleStartCameraController;

    // <변경부분> 현재 StageBattleData에 연결된
    // EventSequenceData를 실제 실행할 공용 이벤트 컨트롤러.
    //
    // 일반 스테이지에서는 Sequence Data가 null이므로
    // Controller가 존재해도 아무 이벤트도 실행하지 않는다.
    [SerializeField]
    private EventSequenceController eventSequenceController;

    // <변경부분> StageBattleData의 보상 데이터를 전달할 전투 종료 흐름 컨트롤러
    [SerializeField]
    private BattleEndFlowController battleEndFlowController;

    [Header("Battle UI")]
    // <변경부분>
    // BattleScene의 일반 전투 UI 전체를 묶는 부모 CanvasGroup.
    // UI 스크립트 자체는 비활성화하지 않고
    // 화면 표시와 입력만 제어한다.
    [SerializeField]
    private CanvasGroup battleUICanvasGroup;

    // <변경부분>
    // 전투 UI를 최초로 변경하기 전 CanvasGroup의 원래 상태를 보관한다.
    // 표시 상태를 복구할 때 기존 Inspector 설정을 덮어쓰지 않기 위함이다.
    private bool hasCachedBattleUIOriginalState = false;

    private float battleUIOriginalAlpha = 1f;
    private bool battleUIOriginalInteractable = true;
    private bool battleUIOriginalBlocksRaycasts = true;

    // <변경부분> 이번 전투에서 시간 강제 변경을 적용했는지 기록한다.
    private bool hasAppliedStageEndTimerOverride = false;

    private void Start()
    {
        SetupBattle();
    }

    // <변경부분> 실제로 전투 씬을 떠날 때 시간 설정을 종료한다.
    //
    // 전투가 승리/패배로 끝났더라도
    // 보상 팝업이나 결과 연출 중에는 현재 시간대를 유지한다.
    //
    // Scene 이동으로 BattleSetupManager가 제거되는 시점에
    // StageBattleData에서 설정한 복구/유지 방식을 적용한다.
    private void OnDestroy()
    {
        if (hasAppliedStageEndTimerOverride == false)
        {
            return;
        }

        if (RunStateManager.Instance != null)
        {
            RunStateManager.Instance
                .EndStageEndTimerOverride();
        }

        hasAppliedStageEndTimerOverride = false;
    }

    // <변경부분>
    // 전투 UI의 표시 및 입력 상태를 제어한다.
    //
    // GameObject 자체는 비활성화하지 않으며,
    // BattleUIController와 하위 UI 스크립트의 동작은 유지한다.
    //
    // 숨김:
    // Alpha 0, Interactable false, BlocksRaycasts false.
    //
    // 표시:
    // 최초 CanvasGroup 설정값으로 복구한다.
    public void SetBattleUIVisible(bool isVisible)
    {
        if (battleUICanvasGroup == null)
        {
            Debug.LogWarning(
                "Battle UI 표시 변경 실패: " +
                "Battle UI Canvas Group이 연결되지 않았습니다.",
                this
            );

            return;
        }

        // <변경부분>
        // 최초 호출 시에만 CanvasGroup의 원래 설정을 저장한다.
        if (hasCachedBattleUIOriginalState == false)
        {
            battleUIOriginalAlpha =
                battleUICanvasGroup.alpha;

            battleUIOriginalInteractable =
                battleUICanvasGroup.interactable;

            battleUIOriginalBlocksRaycasts =
                battleUICanvasGroup.blocksRaycasts;

            hasCachedBattleUIOriginalState = true;
        }

        if (isVisible)
        {
            // <변경부분>
            // UI를 표시할 때 모든 속성을 강제로 활성화하지 않고
            // 최초 CanvasGroup 설정으로 되돌린다.
            battleUICanvasGroup.alpha =
                battleUIOriginalAlpha;

            battleUICanvasGroup.interactable =
                battleUIOriginalInteractable;

            battleUICanvasGroup.blocksRaycasts =
                battleUIOriginalBlocksRaycasts;
        }
        else
        {
            // <변경부분>
            // UI 표시와 입력만 차단한다.
            // GameObject와 UI 스크립트는 활성 상태로 유지한다.
            battleUICanvasGroup.alpha = 0f;
            battleUICanvasGroup.interactable = false;
            battleUICanvasGroup.blocksRaycasts = false;
        }
    }

    // <변경부분> 현재 StageBattleData 기준으로 전투를 구성하는 함수
    public void SetupBattle()
    {
        // <변경부분> 일반 게임 진행에서는
        // 월드맵 전투 노드가 전달한 StageBattleData를 최우선으로 사용한다.
        stageBattleData =
            WorldMapRuntimeState
                .PendingStageBattleData;

        // <변경부분> 월드맵을 거치지 않고
        // BattleScene을 직접 Play한 테스트 상황이라면
        // Inspector에 연결된 테스트용 StageBattleData를 대신 사용한다.
        if (stageBattleData == null)
        {
            stageBattleData =
                directTestStageBattleData;

            if (stageBattleData != null)
            {
                Debug.Log(
                    $"BattleSetupManager 직접 씬 테스트: " +
                    $"{stageBattleData.name} 사용"
                );
            }
        }

        if (stageBattleData == null)
        {
            Debug.LogError(
                "BattleSetupManager 실패: " +
                "월드맵에서 전달된 StageBattleData도 없고 " +
                "Direct Test Stage Battle Data도 연결되지 않았습니다."
            );

            return;
        }

        // 이번 전투에서 실제 RunState Player 기물을
        // 시작 편성으로 사용할 수 있는지 먼저 확정한다.
        bool useRunStatePlayerPiecesForSetup =
            ShouldUseRunStatePlayerPieces();

        // StageBattleData의 단일 Validation 규칙에
        // 현재 Runtime 상황만 전달하여 전투 실행 가능 여부를 검사한다.
        if (stageBattleData.IsValid(
                useRunStatePlayerPiecesForSetup
            ) == false)
        {
            Debug.LogError(
                $"BattleSetupManager 실패: " +
                $"StageBattleData가 현재 전투 조건에서 유효하지 않습니다. " +
                $"{stageBattleData.name}"
            );

            return;
        }

        if (boardManager == null ||
pieceManager == null ||
battleManager == null ||
backgroundManager == null ||
battleEndFlowController == null)
        {
            Debug.LogError(
                "BattleSetupManager 실패: " +
                "BoardManager / PieceManager / BattleManager / " +
                "BackgroundManager / BattleEndFlowController 연결을 확인하세요."
            );

            return;
        }

        // <변경부분> 스테이지 데이터에서 시간 강제 변경을 요청했다면
        // 배경을 불러오기 전에 현재 엔드타이머 눈금을 변경한다.
        //
        // 일반 스테이지는 기본값이 false이므로
        // 기존 런의 시간을 그대로 유지한다.
        if (stageBattleData.forceEndTimerOnStart)
        {
            if (RunStateManager.Instance != null)
            {
                hasAppliedStageEndTimerOverride =
                    RunStateManager.Instance
                        .BeginStageEndTimerOverride(
                            stageBattleData.forcedEndTimerTick,
                            stageBattleData.freezeEndTimerDuringBattle,
                            stageBattleData.endTimerExitMode
                        );
            }
            else
            {
                Debug.LogWarning(
                    "스테이지 시간 강제 변경 실패: " +
                    "RunStateManager가 존재하지 않습니다.",
                    this
                );
            }
        }

        battleManager.SetStageName(
     stageBattleData.stageName
 );

        // <변경부분> 현재 StageBattleData에서 지정한
        // 전투 시작 Announcement 종류를 BattleManager에 전달한다.
        //
        // 일반 전투:
        // BattleStart
        //
        // 보스 전투:
        // Warning
        battleManager.SetStartAnnouncementType(
            stageBattleData.startAnnouncementType
        );

        // <변경부분> 현재 StageBattleData에 연결된
        // BackgroundMapData를 BattleScene 배경으로 불러온다.
        backgroundManager.LoadMapFromData(
            stageBattleData.backgroundMapData
        );

        // <변경부분> StageBattleData의 TileType A/B를
        // BoardManager에 전달해 전투 보드를 재생성한다.
        boardManager.RebuildBoardByTileType(
            stageBattleData.checkerTileTypeA,
            stageBattleData.checkerTileTypeB
        );

        // <변경부분> 기존 테스트/이전 기물 제거 후 현재 보드 크기에 맞춰 기물 배열 초기화
        pieceManager.ClearAllPieces();

        // <변경부분> 저장된 런 상태가 있으면 저장된 플레이어 기물을 사용하고, 없으면 기본 편성을 사용
        SpawnPlayerPieces();

        // <변경부분> PieceFormationData의 배치 방식에 따라
        // 수동 좌표 배치 또는 상대 시작 10칸 랜덤 배치를 생성한다.
        SpawnEnemyPieces();

        // <변경부분> StageBattleData에서 지정한 타입 아이콘 초기 표시 모드를 적용한다.
        // 기물이 생성된 뒤 내부 버튼 모드와 실제 아이콘 표시를 함께 갱신한다.
        // 따라서 SelectedOnly로 시작하면 첫 클릭 시 All로 정상 전환된다.
        battleManager.ApplyStageTypeIconInitialMode(
            stageBattleData.initialTypeIconDisplayMode
        );

        // <변경부분>
        // StageBattleData의 설정에 따라
        // 전투 UI의 초기 표시 상태를 적용한다.
        //
        // CanvasGroup이 없고 숨김 기능도 요청하지 않았다면
        // 일반 전투 흐름을 그대로 유지한다.
        if (battleUICanvasGroup != null)
        {
            SetBattleUIVisible(
                !stageBattleData.hideBattleUIOnStart
            );
        }
        else if (stageBattleData.hideBattleUIOnStart)
        {
            Debug.LogWarning(
                "Battle UI 초기 숨김 실패: " +
                "StageBattleData에서 Hide Battle UI On Start가 켜져 있지만 " +
                "BattleSetupManager의 Battle UI Canvas Group이 연결되지 않았습니다.",
                this
            );
        }

        // <변경부분> StageBattleData의 승패 조건을 BattleManager에 전달
        battleManager.SetBattleEndCondition(
            stageBattleData.playerDefeatCondition,
            stageBattleData.enemyDefeatCondition
        );

        // <변경부분> 현재 스테이지가 종료될 때
        // 플레이어 기물 상태를 RunState에 저장할지
        // StageBattleData 설정을 BattleManager에 전달한다.
        //
        // 튜토리얼에서는 false로 설정하여
        // 튜토리얼 SpawnPiece 및 전투 결과가
        // 실제 런 기물 상태를 덮어쓰지 않도록 한다.
        battleManager.SetSavePlayerPiecesToRunState(
            stageBattleData.savePlayerPiecesToRunState
        );

        battleManager.SetEnemyAIUniqueSkillUseChance(
      stageBattleData.enemyUniqueSkillUseChance
  );

        // <변경부분> StageBattleData에서 지정한
        // Enemy AI의 턴당 최대 능력 성공 횟수를 전달한다.
        battleManager.SetEnemyAIMaxUniqueSkillUsesPerTurn(
            stageBattleData.enemyMaxUniqueSkillUsesPerTurn
        );

        // <변경부분> 현재 스테이지의 전투 보상 데이터를
        // BattleEndFlowController에 전달한다.
        battleEndFlowController.SetBattleRewardData(
            stageBattleData.battleRewardData
        );

        // <변경부분> 현재 StageBattleData에 지정된
        // 승리 후 이동 Scene을 BattleEndFlowController에 전달한다.
        //
        // 일반 Stage는 WorldMapScene,
        // 최종 보스 Stage는 Title Scene 등
        // 스테이지마다 서로 다른 목적지를 사용할 수 있다.
        battleEndFlowController.SetVictorySceneName(
    stageBattleData.victorySceneName
);

        // <변경부분> 승리 후 컷씬이 지정되어 있다면
        // 해당 TextCutsceneData도 BattleEndFlowController에 전달한다.
        //
        // null인 일반 전투에서는 아무 컷씬 데이터도 등록하지 않는다.
        battleEndFlowController.SetVictoryWorldMapEventData(stageBattleData.victoryWorldMapEventData);
        battleEndFlowController.SetVictoryCutsceneData(
            stageBattleData.victoryCutsceneData
        );

        // <변경부분> 스테이지별 적 일반스킬 랜덤 부여 규칙 적용
        ApplyEnemyGeneralSkillGrantRules();

        // <변경부분>
        // 타일 30칸과 양쪽 기물 생성, 전투 데이터 설정이 끝났다.
        // 등장 연출이 완료되기 전에는 초기 배치와 입력을 시작하지 않는다.
        battleManager.SetBattleIntroActive(true);

        if (playBattleBoardIntro)
        {
            if (battleBoardIntroAnimator == null)
            {
                battleBoardIntroAnimator =
                    GetComponent<EventSceneFormationIntroAnimator>();
            }

            // <변경부분>
            // Inspector 연결 누락으로 연출이 조용히 생략되지 않도록 한다.
            if (battleBoardIntroAnimator == null)
            {
                battleBoardIntroAnimator =
                    gameObject.AddComponent<EventSceneFormationIntroAnimator>();

                Debug.Log(
                    "[BattleIntro] 등장 애니메이터 컴포넌트를 자동 추가했습니다.",
                    this
                );
            }

            if (battleBoardIntroAnimator != null &&
                battleBoardIntroAnimator.isActiveAndEnabled)
            {
                // <변경부분>
                // 실제 재생하지 않고, 모든 타일과 기물을 먼저 숨긴다.
                // true: Player → Enemy 팀별 등장 모드.
                bool prepared =
                    battleBoardIntroAnimator.Prepare(
                        boardManager,
                        pieceManager,
                        true
                    );

                Debug.Log(
                    $"[BattleIntro] PREPARE RESULT | " +
                    $"Tiles={boardManager.Width * boardManager.Height} | " +
                    $"IsPrepared={prepared}",
                    this
                );

                if (!prepared)
                {
                    Debug.LogWarning(
                        "[BattleIntro] 등장 연출 준비에 실패했습니다. " +
                        "기존 전투 시작 흐름은 계속 진행합니다.",
                        this
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "[BattleIntro] 등장 애니메이터가 비활성화되어 있습니다.",
                    this
                );
            }
        }

        // <변경부분>
        // EventSequenceData 연결은 기존 시점에 유지한다.
        // 실제 자동 실행은 등장 연출 완료 후에 시작한다.
        SetupEventSequence();

        // <변경부분>
        // 연출 종료 → 전투 입력 잠금 해제 → EventSequence 시작.
        StartCoroutine(
            CompleteBattleStartupAfterIntroRoutine()
        );

        Debug.Log(
    $"전투 세팅 완료: " +
    $"{stageBattleData.stageName}"
);
    }

    // <변경부분> 현재 StageBattleData에 연결된
    // EventSequenceData를 EventSequenceController에 전달한다.
    //
    // 일반 스테이지:
    // eventSequenceData == null
    // → Sequence를 비우고 실행하지 않는다.
    //
    // 튜토리얼 / 이벤트 스테이지:
    // eventSequenceData != null
    // → 해당 데이터만 연결하고,
    //   Play Automatically 설정에 따라 실행한다.
    private void SetupEventSequence()
    {
        // Event Sequence Controller 자체가 없는 BattleScene이라면
        // 이벤트 데이터가 없는 일반 전투에서는 그냥 통과한다.
        if (eventSequenceController == null)
        {
            if (stageBattleData.eventSequenceData != null)
            {
                Debug.LogWarning(
                    "Event Sequence 적용 실패: " +
                    "StageBattleData에는 EventSequenceData가 있지만 " +
                    "BattleSetupManager의 EventSequenceController가 연결되지 않았습니다."
                );
            }

            return;
        }

        // <변경부분> 이전 Scene Inspector 설정이나
        // 다른 테스트 데이터에 의존하지 않고,
        // 현재 StageBattleData의 데이터로 Sequence를 완전히 교체한다.
        eventSequenceController.SetSequenceData(
            stageBattleData.eventSequenceData
        );

        // 일반 스테이지는 여기서 종료한다.
        if (stageBattleData.eventSequenceData == null)
        {
            Debug.Log(
                "Event Sequence 없음: " +
                "현재 스테이지는 일반 Battle로 진행합니다."
            );

            return;
        }

        // EventSequenceData가 자동 실행을 사용하지 않는 경우
        // 데이터만 연결하고 외부 호출을 기다린다.
        if (stageBattleData.eventSequenceData.playAutomatically == false)
        {
            Debug.Log(
                $"Event Sequence 자동 시작 안 함: " +
                $"{stageBattleData.eventSequenceData.name}"
            );

            return;
        }

        // <변경부분>
        // EventSequenceData는 위에서 이미 연결했다.
        // 실제 StartSequence() 호출은 등장 연출 완료 후 실행한다.
    }

    // <변경부분>
    // 기존 전투 시작 순서를 보존하면서
    // 카메라 확대 → 타일 등장 → Player 등장 → Enemy 등장
    // 순서가 끝날 때까지 기다린다.
    private System.Collections.IEnumerator CompleteBattleStartupAfterIntroRoutine()
    {
        // <변경부분>
        // 실제 Prepare가 성공했을 때만 카메라 완료를 기다린다.
        // Play Battle Board Intro가 OFF라면 기존 시작 흐름을 사용한다.
        if (playBattleBoardIntro &&
            battleBoardIntroAnimator != null &&
            battleBoardIntroAnimator.IsPrepared)
        {
            // <변경부분>
            // Inspector 연결이 없으면 현재 Scene의 카메라를 검색한다.
            if (battleStartCameraController == null)
            {
                battleStartCameraController =
                    FindObjectOfType<PixelCameraController>();
            }

            if (battleStartCameraController != null &&
                battleStartCameraController.isActiveAndEnabled)
            {
                Debug.Log(
                    "[BattleIntro] 카메라 시작 확대 완료 대기",
                    this
                );

                // <변경부분>
                // Camera의 Start()가 아직 실행되지 않은 경우에도
                // 실제 확대 완료까지 기다리므로 초기화 순서에 안전하다.
                while (battleStartCameraController != null &&
                       battleStartCameraController.isActiveAndEnabled &&
                       !battleStartCameraController.IsStartZoomComplete)
                {
                    yield return null;
                }

                Debug.Log(
                    "[BattleIntro] 카메라 시작 확대 완료",
                    this
                );
            }
            else
            {
                // <변경부분>
                // 카메라가 없거나 비활성화되더라도
                // 전투 진행이 영구적으로 멈추지 않게 한다.
                Debug.LogWarning(
                    "[BattleIntro] 활성 카메라를 찾지 못했습니다. " +
                    "카메라 대기를 생략합니다.",
                    this
                );

                yield return null;
            }

            // <변경부분>
            // 카메라 확대가 끝난 뒤 숨겨 둔 타일과 기물의
            // 실제 등장 애니메이션을 실행한다.
            if (battleBoardIntroAnimator != null &&
                battleBoardIntroAnimator.isActiveAndEnabled &&
                battleBoardIntroAnimator.IsPrepared)
            {
                battleBoardIntroAnimator.PlayPrepared();

                Debug.Log(
                    $"[BattleIntro] PLAY RESULT | " +
                    $"Tiles={boardManager.Width * boardManager.Height} | " +
                    $"IsPlaying={battleBoardIntroAnimator.IsPlaying}",
                    this
                );

                // <변경부분>
                // 타일 → Player → Enemy의 모든 연출이 끝날 때까지
                // 기존 전투 입력과 자리배치 시작을 잠근다.
                while (battleBoardIntroAnimator != null &&
                       battleBoardIntroAnimator.IsPlaying)
                {
                    yield return null;
                }
            }
            else if (battleBoardIntroAnimator != null &&
                     battleBoardIntroAnimator.IsPrepared)
            {
                // <변경부분>
                // 대기 중 Animator가 비활성화되었다면
                // 숨겨진 타일과 기물을 원래대로 복원한다.
                battleBoardIntroAnimator.StopAndRestore();
            }
        }

        // <변경부분>
        // EventSequence가 첫 프레임부터 기물/타일을 제어할 수 있도록
        // Sequence 시작 직전에 전투 등장 잠금을 해제한다.
        //
        // BattleManager의 초기 배치 루틴은 이 코루틴이 끝난 뒤
        // Sequence 활성 상태를 확인하도록 별도로 대기한다.
        battleManager.SetBattleIntroActive(false);

        Debug.Log(
            "[BattleIntro] 타일 및 기물 등장 연출 완료",
            this
        );

        // 일반 전투이거나 수동 실행 EventSequence라면
        // 기존 규칙대로 별도 Sequence를 시작하지 않는다.
        if (eventSequenceController == null ||
            stageBattleData == null ||
            stageBattleData.eventSequenceData == null ||
            !stageBattleData.eventSequenceData.playAutomatically)
        {
            yield break;
        }

        // <변경부분>
        // 자동 실행 EventSequence는 등장 연출이 끝난 뒤 시작한다.
        eventSequenceController.StartSequence();

        Debug.Log(
            $"Stage Event Sequence 시작: " +
            $"{stageBattleData.eventSequenceData.name}"
        );
    }

    // 현재 StageBattleData가 실제 RunState Player 기물을
    // 이번 전투의 시작 편성으로 사용할 수 있는지 확인한다.
    //
    // Validation과 실제 Spawn이 동일한 조건을 사용하도록
    // RunState 사용 여부 판단을 이 함수 한 곳에서 관리한다.
    private bool ShouldUseRunStatePlayerPieces()
    {
        if (stageBattleData == null)
        {
            return false;
        }

        // 이 Stage가 RunState Player 기물 사용을 허용하지 않으면
        // 저장된 데이터가 존재하더라도 사용하지 않는다.
        if (stageBattleData.useRunStatePlayerPieces == false)
        {
            return false;
        }

        // RunStateManager가 없는 경우 저장된 기물을 사용할 수 없다.
        if (RunStateManager.Instance == null)
        {
            return false;
        }

        // 실제 Player Runtime Piece 데이터가 있을 때만 사용한다.
        return
            RunStateManager.Instance
                .HasPlayerPieceRuntimeData;
    }

    // 현재 Stage 설정과 RunState 상태에 따라
    // Player 기물을 RunState 또는 기본 Formation에서 생성한다.
    private void SpawnPlayerPieces()
    {
        // Validation과 동일한 단일 조건을 사용하여
        // 실제 RunState Player 기물을 사용할지 결정한다.
        if (ShouldUseRunStatePlayerPieces())
        {
            List<PlayerPieceRuntimeData> playerRuntimePieces =
                RunStateManager.Instance
                    .GetPlayerPiecesCopy();

            pieceManager.SpawnPlayerPiecesFromRuntimeData(
                playerRuntimePieces,
                false
            );

            Debug.Log(
                $"저장된 런 상태 플레이어 기물 배치 완료: " +
                $"{playerRuntimePieces.Count}개"
            );

            return;
        }

        // RunState를 사용할 수 없다면
        // StageBattleData의 기본 Player Formation을 사용한다.
        //
        // SetupBattle의 StageBattleData.IsValid()에서 이미
        // 유효성을 확인했지만 Runtime 방어를 위해 null 검사를 유지한다.
        if (stageBattleData.playerFormationData == null)
        {
            Debug.LogWarning(
                "플레이어 기본 편성 배치 실패: " +
                "playerFormationData가 없습니다."
            );

            return;
        }

        PieceFormationData playerFormationData =
            stageBattleData.playerFormationData;

        // <변경부분>
        // Empty 편성은 전투 시작 시 아무 기물도 생성하지 않는 것이 정상이다.
        //
        // 이후 튜토리얼 EventSequence의 SpawnPiece Step 등에서
        // 필요한 기물을 별도로 생성할 수 있다.
        if (playerFormationData.spawnMode ==
            PieceFormationSpawnMode.Empty)
        {
            Debug.Log(
                $"플레이어 빈 편성 적용 완료: " +
                $"{playerFormationData.formationName}"
            );

            return;
        }

        if (playerFormationData.spawnMode ==
            PieceFormationSpawnMode.Manual)
        {
            pieceManager.SpawnPiecesFromDataList(
                playerFormationData.spawnDataList
            );

            Debug.Log(
                $"StageBattleData 기본 플레이어 편성 배치 완료: " +
                $"{playerFormationData.formationName}"
            );

            return;
        }

        // <변경부분>
        // RandomEnemyStartZone는 Enemy 전용 배치 방식이므로
        // Player Formation에 잘못 지정된 경우 조용히 잘못된 데이터를 사용하지 않는다.
        Debug.LogWarning(
            $"플레이어 편성 배치 실패: " +
            $"{playerFormationData.formationName}의 Spawn Mode " +
            $"{playerFormationData.spawnMode}는 Player에서 지원하지 않습니다."
        );
    }

    // <변경부분> 현재 적 PieceFormationData의 배치 방식에 따라
    // 기존 수동 좌표 배치 / 상대 시작 진영 랜덤 배치 / 빈 편성을 실행한다.
    private void SpawnEnemyPieces()
    {
        PieceFormationData enemyFormationData =
            stageBattleData.enemyFormationData;

        if (enemyFormationData == null)
        {
            Debug.LogWarning(
                "적 기물 배치 실패: enemyFormationData가 없습니다."
            );

            return;
        }

        // <변경부분>
        // Empty 편성은 전투 시작 시 적 기물을 생성하지 않는다.
        //
        // 튜토리얼 EventSequence에서 이후 SpawnPiece Step으로
        // 적 기물을 생성하는 흐름에 사용한다.
        if (enemyFormationData.spawnMode ==
            PieceFormationSpawnMode.Empty)
        {
            Debug.Log(
                $"적 빈 편성 적용 완료: " +
                $"{enemyFormationData.formationName}"
            );

            return;
        }

        if (enemyFormationData.spawnMode ==
            PieceFormationSpawnMode.Manual)
        {
            // 기존 PieceFormationData의 좌표를 그대로 사용하는 수동 배치
            pieceManager.SpawnPiecesFromDataList(
                enemyFormationData.spawnDataList
            );

            Debug.Log(
                $"적 수동 편성 배치 완료: " +
                $"{enemyFormationData.formationName}"
            );

            return;
        }

        SpawnRandomEnemyStartFormation(
            enemyFormationData
        );
    }

    // <변경부분> 보드 상단 2줄을 상대 시작 진영 10칸으로 사용하고,
    // PieceFormationData에 등록된 PieceData를 설정 비율대로 뽑아
    // 서로 다른 무작위 빈칸에 Enemy 기물로 생성한다.
    //
    // 5x6 보드 기준 사용 좌표:
    // y = Height - 2, Height - 1 / 각 줄 x = 0 ~ Width - 1
    private void SpawnRandomEnemyStartFormation(
        PieceFormationData formationData)
    {
        if (formationData == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return;
        }

        List<Vector2Int> availablePositions =
            new List<Vector2Int>();

        int firstEnemyRowY =
            Mathf.Max(
                0,
                boardManager.Height - 2
            );

        // 상대 시작 영역의 빈칸만 후보로 수집한다.
        for (int y = firstEnemyRowY;
             y < boardManager.Height;
             y++)
        {
            for (int x = 0;
                 x < boardManager.Width;
                 x++)
            {
                if (pieceManager.IsEmpty(
                        x,
                        y) == false)
                {
                    continue;
                }

                availablePositions.Add(
                    new Vector2Int(x, y)
                );
            }
        }

        if (availablePositions.Count == 0)
        {
            Debug.LogWarning(
                "적 랜덤 편성 배치 실패: " +
                "상대 시작 영역에 빈칸이 없습니다."
            );

            return;
        }

        // 위치 목록을 먼저 섞어 같은 좌표가 중복 선택되지 않게 한다.
        ShufflePositions(
            availablePositions
        );

        int spawnCount =
            Mathf.Clamp(
                formationData.randomPieceCount,
                1,
                availablePositions.Count
            );

        int spawnedCount = 0;

        for (int i = 0;
             i < spawnCount;
             i++)
        {
            PieceData selectedPieceData =
                formationData.RollRandomPieceData();

            if (selectedPieceData == null)
            {
                Debug.LogWarning(
                    "적 랜덤 편성 배치 중단: " +
                    "비율 추첨 가능한 PieceData가 없습니다."
                );

                break;
            }

            Vector2Int spawnPosition =
                availablePositions[i];

            // 기존 PieceManager의 PieceData 직접 생성 함수를 사용한다.
            // 이 함수는 PieceData의 외형, 고유스킬, 기본 일반스킬까지 적용한다.
            Piece spawnedPiece =
                pieceManager.SpawnPieceFromData(
                    selectedPieceData,
                    PieceTeam.Enemy,
                    spawnPosition.x,
                    spawnPosition.y,
                    true,
                    false
                );

            if (spawnedPiece != null)
            {
                spawnedCount++;
            }
        }

        Debug.Log(
            $"적 랜덤 편성 배치 완료: " +
            $"{formationData.formationName} / " +
            $"요청 {spawnCount}개 / 생성 {spawnedCount}개"
        );
    }

    // <변경부분> 상대 시작 위치 후보를 Fisher-Yates 방식으로 섞는다.
    private void ShufflePositions(
        List<Vector2Int> positions)
    {
        if (positions == null)
        {
            return;
        }

        for (int i = positions.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            Vector2Int temporaryPosition =
                positions[i];

            positions[i] =
                positions[randomIndex];

            positions[randomIndex] =
                temporaryPosition;
        }
    }

    // <변경부분> StageBattleData에 등록된 규칙을 기준으로 적 기물에게 일반스킬을 랜덤 부여
    private void ApplyEnemyGeneralSkillGrantRules()
    {
        EnemyGeneralSkillGrantRule[] rules = stageBattleData.enemyGeneralSkillGrantRules;

        if (rules == null || rules.Length == 0)
        {
            return;
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

                if (piece.Team != PieceTeam.Enemy)
                {
                    continue;
                }

                ApplyRulesToEnemyPiece(piece, rules);
            }
        }
    }

    // <변경부분> 적 기물 하나에 일반스킬 랜덤 부여 규칙들을 독립적으로 적용
    private void ApplyRulesToEnemyPiece(Piece piece, EnemyGeneralSkillGrantRule[] rules)
    {
        if (piece == null || rules == null)
        {
            return;
        }

        for (int i = 0; i < rules.Length; i++)
        {
            EnemyGeneralSkillGrantRule rule = rules[i];

            if (rule == null)
            {
                continue;
            }

            if (rule.IsValid() == false)
            {
                continue;
            }

            if (piece.PieceType == PieceType.King && rule.allowKing == false)
            {
                continue;
            }

            if (rule.RollGrant() == false)
            {
                continue;
            }

            // <변경부분> 일반스킬 레벨 없이
            // 해당 적 기물에 스킬을 중복되지 않게 부여한다.
            bool skillAdded =
                piece.AddGeneralSkill(
                    rule.skillType
                );

            if (skillAdded)
            {
                Debug.Log(
                    $"스테이지 적 일반스킬 부여: " +
                    $"{piece.PieceType} / " +
                    $"{rule.skillType}"
                );
            }
        }
    }
}