using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// <변경부분> EventSequenceData의 Step을 순서대로 실행하는
// 튜토리얼 / 전투 이벤트 / 스토리 이벤트 공용 컨트롤러
//
// 이 컴포넌트와 EventSequenceData가 연결된 경우에만 동작하며,
// 비활성화하거나 데이터가 없으면 기존 Battle 시스템에는 전혀 관여하지 않는다.
public class EventSequenceController : MonoBehaviour
{
    // <변경부분> 현재 스테이지에서 실행할 EventSequenceData.
    //
    // 더 이상 BattleScene Inspector에서 직접 고정하지 않는다.
    // BattleSetupManager가 현재 StageBattleData를 읽은 뒤
    // SetSequenceData()를 통해 전달한다.
    //
    // 따라서 같은 BattleScene을 사용하는 일반 스테이지에서
    // 이전 튜토리얼 Sequence가 자동 실행되는 문제를 방지한다.
    private EventSequenceData sequenceData;

    [Header("Managers")]
    // 기물 생성 Step에서 사용할 기존 PieceManager
    [SerializeField]
    private PieceManager pieceManager;

    // <변경부분> Event Sequence 완료 타입이 BattleWin일 때
    // 기존 Battle 승리 / 보상 흐름으로 연결하기 위한 BattleManager.
    [SerializeField]
    private BattleManager battleManager;

    [Header("Dialogue")]
    // 다음 단계에서 제작할 텍스트 UI 컨트롤러
    //
    // 아직 EventGuideUI가 없는 상태에서도
    // Dialogue Step을 건너뛰며 나머지 기능 테스트가 가능하다.
    [SerializeField]
    private EventGuideUI eventGuideUI;

    [Header("Marker")]
    // <변경부분> ForcePiece / ForceTile / ForceButton Step에서
    // 현재 플레이어가 눌러야 하는 대상을 자동으로 가리키는 공용 마커
    //
    // 연결하지 않으면 입력 강제 기능만 동작하고
    // 마커 표시는 생략된다.
    [SerializeField]
    private EventMarkerUI eventMarkerUI;

    [Header("Debug")]
    // <변경부분> 현재 시퀀스 실행 여부는
    // Scene에 저장되는 설정값이 아니라 순수 런타임 상태다.
    //
    // SerializeField로 저장하면 Inspector에 남아 있던 true 값 때문에
    // StartSequence()가 "이미 실행 중"으로 판단하여
    // 실제 Sequence Coroutine을 시작하지 않는 문제가 발생할 수 있다.
    private bool isSequenceActive =
    false;

    // <변경부분> 현재 Step 번호 역시
    // 실행 중에만 사용하는 런타임 상태로 관리한다.
    private int currentStepIndex =
        -1;

    // 현재 실행 중인 메인 시퀀스 코루틴
    private Coroutine sequenceCoroutine;

    // <변경부분> 현재 ForcePieceSelect Step에서
    // 플레이어의 올바른 기물 선택을 기다리고 있는지 확인한다.
    private bool isWaitingForPieceSelection =
        false;

    // <변경부분> 현재 강제 선택 대상으로 지정된 기물 좌표
    private Vector2Int requiredPiecePosition =
        Vector2Int.zero;

    // <변경부분> 현재 강제 선택 대상으로 지정된 기물 진영
    private PieceTeam requiredPieceTeam =
        PieceTeam.Player;

    // <변경부분> 현재 ForceTileSelect Step에서
    // 지정한 타일 클릭을 기다리고 있는지 확인한다.
    private bool isWaitingForTileSelection =
        false;

    // <변경부분> ForceTileSelect에서
    // 플레이어가 반드시 클릭해야 하는 보드 좌표
    private Vector2Int requiredTilePosition =
        Vector2Int.zero;

    // <변경부분> 현재 ForceButton Step에서
    // 지정한 전투 UI 버튼 입력을 기다리고 있는지 확인한다.
    private bool isWaitingForButtonInput =
        false;

    // <변경부분> ForceButton에서
    // 플레이어가 반드시 눌러야 하는 버튼 종류
    private EventSequenceButtonType requiredButtonType =
        EventSequenceButtonType.None;

    // 외부 Battle 시스템이
    // 이벤트 진행 여부를 확인할 때 사용할 공개 프로퍼티
    public bool IsSequenceActive
    {
        get
        {
            return isSequenceActive;
        }
    }

    public EventSequenceData SequenceData
    {
        get
        {
            return sequenceData;
        }
    }

    // <변경부분> BattleSetupManager가
    // 현재 StageBattleData의 EventSequenceData를 전달할 때 사용한다.
    //
    // 데이터 연결과 실제 실행을 분리하여
    // EventSequenceData의 Play Automatically 설정을
    // BattleSetupManager가 판단할 수 있도록 한다.
    //
    // null을 전달하면 현재 Battle은
    // Event Sequence가 없는 일반 스테이지로 동작한다.
    public void SetSequenceData(
        EventSequenceData newSequenceData)
    {
        // 혹시 기존 Sequence가 실행 중이라면
        // 새 데이터를 적용하기 전에 안전하게 종료한다.
        if (isSequenceActive)
        {
            StopSequence();
        }

        sequenceData =
            newSequenceData;

        if (sequenceData == null)
        {
            Debug.Log(
                "Event Sequence Data 해제: " +
                "현재 스테이지에서는 이벤트를 실행하지 않습니다."
            );

            return;
        }

        Debug.Log(
            $"Event Sequence Data 적용: " +
            $"{sequenceData.name} / " +
            $"PlayAutomatically={sequenceData.playAutomatically}"
        );
    }

    // <변경부분> 현재 Event Sequence가 활성화되어 있고
    // 데이터에서 일반 Battle 승패 무시가 설정되어 있는지 반환한다.
    // 데이터에서 일반 Battle 승패 무시가 설정되어 있는지 반환한다.
    //
    // BattleManager는 이 값만 확인하면 되므로
    // EventSequenceData의 내부 설정을 직접 알 필요가 없다.
    public bool ShouldIgnoreNormalBattleEnd
    {
        get
        {
            return
                isSequenceActive &&
                sequenceData != null &&
                sequenceData.ignoreNormalBattleEnd;
        }
    }

    // <변경부분> 현재 Event Sequence가 활성화되어 있고
    // Enemy AI 일시정지가 설정되어 있는지 반환한다.
    //
    // AI는 시퀀스가 끝날 때까지 기다렸다가
    // 기존 Enemy 턴을 그대로 이어갈 수 있다.
    public bool ShouldPauseEnemyAI
    {
        get
        {
            return
                isSequenceActive &&
                sequenceData != null &&
                sequenceData.pauseEnemyAIWhileSequenceActive;
        }
    }

    // <변경부분> 현재 Event Sequence가
    // Enemy AI를 정지한 상태로 전투 입력을 직접 진행하고 있다면
    // 일반 이동 / 공격 / 흡수 행동이 끝나도
    // BattleManager가 자동으로 턴을 넘기지 않도록 한다.
    //
    // 튜토리얼에서는 여러 ForcePiece / ForceTile / ForceButton Step을
    // 같은 Player Turn 안에서 연속으로 실행해야 하므로 사용한다.
    //
    // Event Sequence가 없거나
    // Pause Enemy AI While Sequence Active가 꺼져 있으면 false이므로
    // 일반 전투의 기존 EndTurn 흐름에는 영향을 주지 않는다.
    public bool ShouldHoldBattleTurnForSequence
    {
        get
        {
            return
                isSequenceActive &&
                sequenceData != null &&
                sequenceData.pauseEnemyAIWhileSequenceActive;
        }
    }

    // <변경부분> 현재 자동 실행 중인 Event Sequence가
    // 기존 플레이어 초기 배치를 건너뛰어야 하는지 반환한다.
    public bool ShouldSkipNormalPlayerDeployment
    {
        get
        {
            return
                isSequenceActive &&
                sequenceData != null &&
                sequenceData.skipNormalPlayerDeployment;
        }
    }

    // <변경부분> 현재 Dialogue가 표시 중이라
    // 기존 Battle 입력을 전부 막아야 하는지 반환한다.
    public bool IsDialogueBlockingBattleInput
    {
        get
        {
            return
                isSequenceActive &&
                eventGuideUI != null &&
                eventGuideUI.IsDialoguePlaying;
        }
    }

    // <변경부분> 현재 튜토리얼 / 이벤트가
    // 특정 보드 입력을 강제로 기다리고 있는지 확인한다.
    //
    // ForcePieceSelect 또는 ForceTileSelect 중에는
    // Space / Q / S 같은 일반 Battle 단축키도 차단한다.
    public bool IsForcedBattleInputActive
    {
        get
        {
            return
                isSequenceActive &&
                (
                    isWaitingForPieceSelection ||
                    isWaitingForTileSelection ||
                    isWaitingForButtonInput
                );
        }
    }

    // <변경부분> BattleManager가 기물을 선택하기 전에
    // 현재 Event Sequence에서 해당 선택을 허용하는지 검사한다.
    //
    // 시퀀스가 없거나 ForcePieceSelect 단계가 아니라면
    // 기존 Battle 선택을 그대로 허용한다.
    public bool CanSelectPiece(
      Piece piece)
    {
        if (isSequenceActive == false)
        {
            return true;
        }

        // Dialogue 중에는 전투 기물 선택을 전부 막는다.
        if (IsDialogueBlockingBattleInput)
        {
            return false;
        }

        // <변경부분> 특정 타일 또는 특정 버튼 입력을 기다리는 동안에는
        // 새로운 기물 선택으로 현재 튜토리얼 상태를
        // 벗어날 수 없도록 모든 기물 선택을 막는다.
        if (isWaitingForTileSelection ||
            isWaitingForButtonInput)
        {
            return false;
        }

        // ForcePieceSelect가 아니라면
        // 기존 Battle 기물 선택을 그대로 허용한다.
        if (isWaitingForPieceSelection == false)
        {
            return true;
        }

        if (piece == null)
        {
            return false;
        }

        if (piece.Team !=
            requiredPieceTeam)
        {
            return false;
        }

        return
            piece.X ==
                requiredPiecePosition.x &&
            piece.Y ==
                requiredPiecePosition.y;
    }

    // <변경부분> BattleManager가 실제 지정 기물 선택에 성공한 뒤 호출한다.
    //
    // 현재 ForcePieceSelect 조건과 일치하는 경우에만
    // 대기 상태를 해제하여 다음 Event Step으로 진행한다.
    public void NotifyPieceSelected(
        Piece piece)
    {
        if (isSequenceActive == false ||
            isWaitingForPieceSelection == false ||
            piece == null)
        {
            return;
        }

        if (piece.Team !=
            requiredPieceTeam)
        {
            return;
        }

        if (piece.X !=
                requiredPiecePosition.x ||
            piece.Y !=
                requiredPiecePosition.y)
        {
            return;
        }

        isWaitingForPieceSelection =
            false;
    }

    // <변경부분> BattleManager가 타일 입력을 처리하기 전에
    // 현재 Event Sequence에서 해당 타일 클릭을
    // 허용하는지 검사한다.
    //
    // 일반 시퀀스 상태에서는 기존 Battle 입력을 그대로 허용하고,
    // ForceTileSelect 중일 때만 지정 좌표 하나로 제한한다.
    public bool CanSelectTile(
        Tile tile)
    {
        if (isSequenceActive == false)
        {
            return true;
        }

        // Dialogue가 표시되는 동안에는
        // 보드의 모든 타일 입력을 막는다.
        if (IsDialogueBlockingBattleInput)
        {
            return false;
        }

        // <변경부분> ForceButton 중에는
        // 보드의 모든 Tile 입력을 차단한다.
        if (isWaitingForButtonInput)
        {
            return false;
        }

        // <변경부분> 현재 데보리아의 기물 클릭은
        // Piece 자체가 아니라 해당 기물이 올라가 있는 Tile 클릭을 통해
        // BattleManager.SelectTile() → SelectPiece() 순서로 처리된다.
        //
        // 따라서 ForcePieceSelect 중에는 모든 Tile을 막으면 안 되고,
        // 요구된 기물이 위치한 좌표의 Tile 하나만 통과시켜야 한다.
        //
        // 이후 BattleManager.SelectTile()이 해당 좌표의 Piece를 찾고
        // SelectPiece()를 호출하면,
        // CanSelectPiece()에서 Team + 좌표를 다시 최종 검증한다.
        if (isWaitingForPieceSelection)
        {
            if (tile == null)
            {
                return false;
            }

            return
                tile.X ==
                    requiredPiecePosition.x &&
                tile.Y ==
                    requiredPiecePosition.y;
        }

        // ForceTileSelect가 아니라면
        // 기존 Battle 타일 입력을 그대로 허용한다.
        if (isWaitingForTileSelection == false)
        {
            return true;
        }

        if (tile == null)
        {
            return false;
        }

        return
            tile.X ==
                requiredTilePosition.x &&
            tile.Y ==
                requiredTilePosition.y;
    }

    // <변경부분> 지정한 ForceTileSelect 타일이
    // 실제 BattleManager의 타일 처리 흐름까지 도달했을 때 호출한다.
    //
    // 첫 클릭 확인과 두 번째 클릭 행동 실행 모두
    // 현재 Step의 지정 좌표와 일치하면 완료할 수 있다.
    public void NotifyTileSelected(
        Tile tile)
    {
        if (isSequenceActive == false ||
            isWaitingForTileSelection == false ||
            tile == null)
        {
            return;
        }

        if (tile.X !=
                requiredTilePosition.x ||
            tile.Y !=
                requiredTilePosition.y)
        {
            return;
        }

        isWaitingForTileSelection =
            false;
    }

    // <변경부분> Battle UI가 실제 버튼 기능을 실행하기 전에
    // 현재 Event Sequence에서 해당 버튼 입력을
    // 허용하는지 검사한다.
    //
    // 일반 Sequence 상태에서는 기존 UI 입력을 그대로 허용하고,
    // ForceButton 중일 때만 지정한 버튼 하나로 제한한다.
    public bool CanPressButton(
        EventSequenceButtonType buttonType)
    {
        if (isSequenceActive == false)
        {
            return true;
        }

        // Dialogue 중에는 전투 UI 버튼을 모두 막는다.
        if (IsDialogueBlockingBattleInput)
        {
            return false;
        }

        // 기물 또는 타일 강제 입력 중에는
        // 전투 액션 버튼으로 우회하지 못하도록 막는다.
        if (isWaitingForPieceSelection ||
            isWaitingForTileSelection)
        {
            return false;
        }

        // ForceButton Step이 아니라면
        // 기존 버튼 입력은 그대로 허용한다.
        if (isWaitingForButtonInput == false)
        {
            return true;
        }

        if (buttonType ==
            EventSequenceButtonType.None)
        {
            return false;
        }

        // 현재 Step에서 지정한 버튼만 허용한다.
        return
            buttonType ==
            requiredButtonType;
    }

    // <변경부분> 지정된 버튼 클릭이 실제 UI 입력 흐름에서
    // 정상적으로 처리된 뒤 호출한다.
    //
    // 현재 ForceButton의 요구 버튼과 일치할 때만
    // 대기 상태를 해제하여 다음 Sequence Step으로 진행한다.
    public void NotifyButtonPressed(
     EventSequenceButtonType buttonType)
    {
        if (isSequenceActive == false ||
            isWaitingForButtonInput == false)
        {
            return;
        }

        if (buttonType !=
            requiredButtonType)
        {
            return;
        }

        Debug.Log(
            $"이벤트 버튼 입력 완료: " +
            $"{buttonType}"
        );

        isWaitingForButtonInput =
            false;

        requiredButtonType =
            EventSequenceButtonType.None;
    }

    // <변경부분> Event Sequence의 모든 실행 상태를
    // Scene 시작 시 순수 런타임 기본값으로 초기화한다.
    //
    // Inspector나 이전 Play 상태에 의해
    // Sequence가 이미 실행 중인 것으로 판단되는 문제를 방지한다.
    private void Awake()
    {
        // <변경부분> Inspector 연결이 빠진 경우에도
        // Event Sequence의 BattleWin 완료 처리가 가능하도록
        // 현재 씬의 BattleManager를 자동으로 찾는다.
        if (battleManager == null)
        {
            battleManager =
                FindObjectOfType<BattleManager>();
        }

        isSequenceActive =
            false;

        currentStepIndex =
            -1;

        sequenceCoroutine =
            null;

        isWaitingForPieceSelection =
            false;

        requiredPiecePosition =
            Vector2Int.zero;

        requiredPieceTeam =
            PieceTeam.Player;

        isWaitingForTileSelection =
            false;

        requiredTilePosition =
            Vector2Int.zero;

        isWaitingForButtonInput =
            false;

        requiredButtonType =
            EventSequenceButtonType.None;
    }

   

    // <변경부분> 현재 연결된 EventSequenceData를 처음부터 실행한다.
    public void StartSequence()
    {
        // <변경부분> 자동 시작 또는 외부 호출이
        // 실제 StartSequence까지 도달했는지 확인한다.
        Debug.Log(
            "Event Sequence StartSequence() 진입"
        );

        if (sequenceData == null)
        {
            Debug.LogWarning(
                "이벤트 시퀀스 시작 실패: " +
                "EventSequenceData가 연결되지 않았습니다."
            );

            return;
        }

        if (sequenceData.IsValid() == false)
        {
            Debug.LogWarning(
                $"이벤트 시퀀스 시작 실패: " +
                $"{sequenceData.name} 데이터가 유효하지 않습니다."
            );

            return;
        }

        // 이미 실행 중이라면 중복 시작하지 않는다.
        if (isSequenceActive)
        {
            return;
        }

        StopSequenceCoroutine();

        sequenceCoroutine =
            StartCoroutine(
                RunSequenceRoutine()
            );
    }

    // <변경부분> 특정 EventSequenceData를 외부에서 전달받아
    // 즉시 실행할 수 있도록 하는 진입점
    //
    // 이후 컷씬이나 다른 이벤트에서
    // 같은 Controller를 재사용할 때 사용할 수 있다.
    public void StartSequence(
        EventSequenceData newSequenceData)
    {
        if (newSequenceData == null)
        {
            Debug.LogWarning(
                "이벤트 시퀀스 시작 실패: " +
                "전달된 EventSequenceData가 null입니다."
            );

            return;
        }

        sequenceData =
            newSequenceData;

        StartSequence();
    }

    // <변경부분> 실제 Step들을 순서대로 실행하는 메인 코루틴
    private IEnumerator RunSequenceRoutine()
    {
        isSequenceActive =
            true;

        currentStepIndex =
            -1;

        Debug.Log(
            $"이벤트 시퀀스 시작: " +
            $"{sequenceData.sequenceName}"
        );

        // <변경부분> 실제 Step 실행 직전에
        // 현재 직렬화된 Step 개수를 다시 확인한다.
        List<EventSequenceStepData> steps =
            sequenceData.steps;

        Debug.Log(
            $"이벤트 시퀀스 Step 실행 준비: " +
            $"{(steps != null ? steps.Count : -1)}개"
        );

        if (steps == null ||
            steps.Count == 0)
        {
            Debug.LogError(
                "이벤트 시퀀스 실행 실패: " +
                "실행할 Step이 없습니다."
            );

            isSequenceActive =
                false;

            yield break;
        }

        for (int i = 0;
             i < steps.Count;
             i++)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            EventSequenceStepData step =
                steps[i];

            currentStepIndex =
                i;

            if (step == null)
            {
                Debug.LogWarning(
                    $"이벤트 Step 건너뜀: " +
                    $"{i}번 데이터가 null입니다."
                );

                continue;
            }

            Debug.Log(
                $"이벤트 Step 시작: " +
                $"{i} / " +
                $"{step.stepName} / " +
                $"{step.stepType}"
            );

            yield return
                ExecuteStepRoutine(
                    step
                );

            // CompleteSequence Step이 실행되면
            // 내부에서 isSequenceActive가 false가 되므로 즉시 종료한다.
            if (isSequenceActive == false)
            {
                yield break;
            }
        }

        // 모든 Step을 끝까지 실행했다면
        // SequenceData의 완료 설정을 처리한다.
        CompleteSequence();
    }

    // <변경부분> Step Type에 따라 실제 기능을 분기한다.
    private IEnumerator ExecuteStepRoutine(
        EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        switch (step.stepType)
        {
            case EventSequenceStepType.None:
                yield break;

            case EventSequenceStepType.Dialogue:
                yield return
                    ExecuteDialogueStepRoutine(
                        step
                    );
                yield break;

            case EventSequenceStepType.SpawnPiece:
                // <변경부분> 이벤트 기물이 생성된 뒤
                // Born 애니메이션이 끝날 때까지 기다리고
                // 다음 Event Step으로 진행한다.
                yield return
                    ExecuteSpawnPieceStepRoutine(
                        step
                    );

                yield break;

            case EventSequenceStepType.RemovePiece:
                // <변경부분> 지정 좌표의 기물을
                // 이벤트 데이터 기준으로 제거한다.
                ExecuteRemovePieceStep(
                    step
                );
                yield break;

            case EventSequenceStepType.Wait:
                yield return
                    ExecuteWaitStepRoutine(
                        step
                    );
                yield break;

            case EventSequenceStepType.CompleteSequence:
                CompleteSequence();
                yield break;

            case EventSequenceStepType.ForcePieceSelect:
                // <변경부분> 지정한 기물을 실제로 선택할 때까지
                // 현재 Step에서 대기한다.
                yield return
                    ExecuteForcePieceSelectStepRoutine(
                        step
                    );
                yield break;

            case EventSequenceStepType.ForceTileSelect:
                // <변경부분> 지정된 보드 타일이
                // 실제로 클릭될 때까지 현재 Step에서 대기한다.
                yield return
                    ExecuteForceTileSelectStepRoutine(
                        step
                    );
                yield break;

            case EventSequenceStepType.ForceButton:
                // <변경부분> 지정한 전투 UI 버튼이
                // 실제로 눌릴 때까지 현재 Step에서 대기한다.
                yield return
                    ExecuteForceButtonStepRoutine(
                        step
                    );
                yield break;
        }
    }

    // <변경부분> ForcePieceSelect Step 실행
    //
    // 지정한 Team + 보드 좌표의 기물 외에는
    // BattleManager에서 선택을 허용하지 않는다.
    //
    // 올바른 기물을 실제로 선택했다는 통지를 받을 때까지
    // 이 Step은 완료되지 않는다.
    private IEnumerator ExecuteForcePieceSelectStepRoutine(
        EventSequenceStepData step)
    {
        requiredPiecePosition =
            step.targetPiecePosition;

        requiredPieceTeam =
    step.targetPieceTeam;

        isWaitingForPieceSelection =
            true;

        // <변경부분> Step에서 Show Marker를 켠 경우
        // 현재 강제 선택 대상 기물을 자동으로 가리킨다.
        if (step.showMarker &&
            eventMarkerUI != null)
        {
            // <변경부분> ForcePieceSelect는
            // 기존 기물 좌표 + Team을 기준으로 마커를 표시한다.
            eventMarkerUI.ShowForPiece(
                requiredPiecePosition,
                requiredPieceTeam,
                step.markerDisplayType,
                step.markerPositionOffset
            );
        }

        Debug.Log(
                    $"이벤트 기물 선택 대기 시작: " +
            $"{requiredPieceTeam} / " +
            $"{requiredPiecePosition}"
        );

        while (isWaitingForPieceSelection)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }

        // <변경부분> 지정 타일 클릭 완료 후
        // World / UI Marker가 부드럽게 사라진 뒤
        // 다음 Event Step으로 진행한다.
        if (eventMarkerUI != null)
        {
            yield return
                eventMarkerUI
                    .HideWithFadeRoutine();
        }

        Debug.Log(
            $"이벤트 기물 선택 완료: " +
            $"{requiredPieceTeam} / " +
            $"{requiredPiecePosition}"
        );
    }

    // <변경부분> ForceTileSelect Step 실행
    //
    // 지정한 좌표 외의 타일 클릭과
    // 새로운 기물 선택을 모두 차단하고,
    // 올바른 타일 클릭이 들어올 때까지 현재 Step에서 대기한다.
    private IEnumerator ExecuteForceTileSelectStepRoutine(
        EventSequenceStepData step)
    {
        requiredTilePosition =
    step.targetTilePosition;

        isWaitingForTileSelection =
            true;

        // <변경부분> Show Marker가 켜진 ForceTileSelect라면
        // 지정된 보드 타일을 자동으로 가리킨다.
        if (step.showMarker &&
    eventMarkerUI != null)
        {
            // <변경부분> 타일 마커도
            // 현재 Step의 표시 방식과 위치를 그대로 사용한다.
            eventMarkerUI.ShowForTile(
                requiredTilePosition,
                step.markerDisplayType,
                step.markerPositionOffset
            );
        }

        Debug.Log(
                    $"이벤트 타일 선택 대기 시작: " +
            $"{requiredTilePosition}"
        );

        while (isWaitingForTileSelection)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }

        // <변경부분> 지정 타일 클릭이 끝났으므로
        // 현재 타일 마커를 숨긴다.
        if (eventMarkerUI != null)
        {
            eventMarkerUI.Hide();
        }

        Debug.Log(
            $"이벤트 타일 선택 완료: " +
            $"{requiredTilePosition}"
        );
    }

    // <변경부분> ForceButton Step 실행
    //
    // EventSequenceStepData에서 지정한 버튼 하나만 허용하고,
    // 해당 버튼 입력이 실제 UI에서 처리될 때까지
    // 현재 Sequence Step에서 대기한다.
    private IEnumerator ExecuteForceButtonStepRoutine(
        EventSequenceStepData step)
    {
        if (step.targetButton ==
            EventSequenceButtonType.None)
        {
            Debug.LogWarning(
                $"ForceButton 실행 실패: " +
                $"{step.stepName}의 Target Button이 None입니다."
            );

            yield break;
        }

        requiredButtonType =
    step.targetButton;

        isWaitingForButtonInput =
            true;

        // <변경부분> 현재 ForceButton Step의 Show Marker가 켜져 있다면
        // 실제 버튼이 활성화될 때까지 잠시 기다리며 Marker 표시를 재시도한다.
        //
        // FieldAbsorbButton은 이전 공격 타일 선택 직후
        // 타입 아이콘 전환 연출을 거친 뒤 늦게 활성화될 수 있으므로,
        // Step 시작 프레임에 한 번만 찾고 포기하지 않는다.
        if (step.showMarker &&
            eventMarkerUI != null)
        {
            yield return
                ShowForceButtonMarkerWhenReadyRoutine(
                    step
                );
        }

        Debug.Log(
                    $"이벤트 버튼 입력 대기 시작: " +
            $"{requiredButtonType}"
        );

        while (isWaitingForButtonInput)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }

        // <변경부분> 지정 버튼 입력 완료 후
        // UI Marker Fade Out이 끝날 때까지 기다린 뒤
        // 다음 Step으로 넘어간다.
        if (eventMarkerUI != null)
        {
            yield return
                eventMarkerUI
                    .HideWithFadeRoutine();
        }

        Debug.Log(
            $"이벤트 버튼 입력 대기 종료: " +
            $"{step.targetButton}"
        );
    }

    // <변경부분> ForceButton 대상 UI가 실제로 활성화될 때까지
    // 짧은 시간 동안 Marker 표시를 반복 시도한다.
    //
    // FieldAbsorbButton은 공격 타일 선택 직후
    // 타입 아이콘 Fade 전환을 거친 뒤 표시되기 때문에
    // 다음 Event Step 시작 순간에는 아직 IsVisible == false일 수 있다.
    //
    // 일반 버튼은 첫 시도에서 바로 성공하고,
    // 늦게 나타나는 동적 UI만 잠시 기다린다.
    private IEnumerator ShowForceButtonMarkerWhenReadyRoutine(
        EventSequenceStepData step)
    {
        if (step == null ||
            eventMarkerUI == null)
        {
            yield break;
        }

        const float markerTargetWaitTimeout =
            1f;

        float elapsedTime =
            0f;

        while (elapsedTime <
               markerTargetWaitTimeout)
        {
            // <변경부분> 기다리는 사이 사용자가
            // 실제 버튼을 먼저 눌러 Step이 완료됐다면
            // 더 이상 Marker를 표시하지 않는다.
            if (isSequenceActive == false ||
                isWaitingForButtonInput == false)
            {
                yield break;
            }

            bool markerShown =
                eventMarkerUI.ShowForButton(
                    requiredButtonType,
                    step.markerDisplayType,
                    step.markerPositionOffset
                );

            if (markerShown)
            {
                // 실제 버튼을 찾아 Marker 표시가 성공했으므로 종료한다.
                yield break;
            }

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        Debug.LogWarning(
            $"ForceButton Marker 표시 실패: " +
            $"{requiredButtonType}의 실제 UI 대상을 " +
            $"{markerTargetWaitTimeout}초 동안 찾지 못했습니다."
        );
    }

    // <변경부분> Dialogue Step 실행
    //
    // 아직 EventGuideUI가 연결되지 않은 경우에는
    // 경고 후 해당 Step만 건너뛴다.
    private IEnumerator ExecuteDialogueStepRoutine(
        EventSequenceStepData step)
    {
        if (step.dialoguePages == null ||
            step.dialoguePages.Count == 0)
        {
            yield break;
        }

        if (eventGuideUI == null)
        {
            Debug.LogWarning(
                $"Dialogue Step 실행 실패: " +
                $"EventGuideUI가 연결되지 않았습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // EventGuideUI가 모든 페이지 표시를 마칠 때까지 기다린다.
        yield return
            eventGuideUI.PlayDialogueRoutine(
                step.dialoguePages
            );
    }

    // <변경부분> Wait Step 실행
    //
    // 이벤트 / UI 연출은 Time.timeScale과 관계없이
    // 진행할 수 있도록 unscaledDeltaTime을 사용한다.
    private IEnumerator ExecuteWaitStepRoutine(
        EventSequenceStepData step)
    {
        float duration =
            Mathf.Max(
                0f,
                step.waitDuration
            );

        if (duration <= 0f)
        {
            yield break;
        }

        float elapsedTime =
            0f;

        while (elapsedTime <
               duration)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }

    // <변경부분> SpawnPiece Step 실행
    //
    // Player / Enemy / Neutral 모두 동일한
    // PieceManager.SpawnPieceFromData()를 사용한다.
    //
    // 생성 직후 기존 Piece Born 애니메이션을 재생하고,
    // Born이 끝난 뒤 다음 Event Step으로 진행한다.
    private IEnumerator ExecuteSpawnPieceStepRoutine(
        EventSequenceStepData step)
    {
        if (pieceManager == null)
        {
            Debug.LogWarning(
                "이벤트 기물 생성 실패: " +
                "PieceManager가 연결되지 않았습니다."
            );

            yield break;
        }

        if (step.spawnPieceData == null)
        {
            Debug.LogWarning(
                $"이벤트 기물 생성 실패: " +
                $"{step.stepName}의 PieceData가 없습니다."
            );

            yield break;
        }

        EventPieceSpawnOverrideData overrideData =
            step.spawnOverride;

        // 기본값은 PieceData의 설정을 그대로 사용한다.
        bool canMove =
            step.spawnPieceData.canMove;

        bool useAbsorbedPlayerVisual =
            false;

        if (overrideData != null)
        {
            if (overrideData.overrideCanMove)
            {
                canMove =
                    overrideData.canMove;
            }

            useAbsorbedPlayerVisual =
                overrideData
                    .useAbsorbedPlayerVisual;
        }

        Piece spawnedPiece =
            pieceManager.SpawnPieceFromData(
                step.spawnPieceData,
                step.spawnPieceTeam,
                step.spawnPosition.x,
                step.spawnPosition.y,
                canMove,
                useAbsorbedPlayerVisual
            );

        if (spawnedPiece == null)
        {
            Debug.LogWarning(
                $"이벤트 기물 생성 실패: " +
                $"{step.spawnPieceData.name} / " +
                $"{step.spawnPieceTeam} / " +
                $"{step.spawnPosition}"
            );

            yield break;
        }

        // <변경부분> 이벤트에서 고유스킬 Override를 사용하면
        // PieceData의 기본 고유스킬 대신 지정한 값을 적용한다.
        if (overrideData != null &&
            overrideData.overrideUniqueSkill)
        {
            spawnedPiece.SetUniqueSkillForEvent(
                overrideData.uniqueSkill
            );
        }

        // <변경부분> 이벤트용 일반스킬 Override 적용
        if (overrideData != null &&
            overrideData.overrideGeneralSkills)
        {
            spawnedPiece.ClearGeneralSkills();

            if (overrideData.generalSkills != null)
            {
                for (int i = 0;
                     i < overrideData.generalSkills.Count;
                     i++)
                {
                    GeneralSkillType skillType =
                        overrideData.generalSkills[i];

                    if (skillType ==
                        GeneralSkillType.None)
                    {
                        continue;
                    }

                    spawnedPiece.AddGeneralSkill(
                        skillType
                    );
                }
            }
        }

        // Override 적용 후
        // 외형과 상태 UI를 현재 데이터 기준으로 갱신한다.
        pieceManager.RefreshPieceVisual(
            spawnedPiece
        );

        Debug.Log(
            $"이벤트 기물 생성 완료: " +
            $"{step.spawnPieceTeam} / " +
            $"{step.spawnPieceData.pieceType} / " +
            $"{step.spawnPosition}"
        );

        // <변경부분> 이벤트에서 새로 등장한 기물이
        // 갑자기 화면에 나타나지 않도록
        // 기존 Piece Born 애니메이션을 재사용한다.
        yield return
            pieceManager.PlayPieceBornAnimation(
                spawnedPiece
            );

        Debug.Log(
            $"이벤트 기물 Born 완료: " +
            $"{step.spawnPieceTeam} / " +
            $"{step.spawnPieceData.pieceType} / " +
            $"{step.spawnPosition}"
        );
    }

    // <변경부분> RemovePiece Step 실행
    //
    // 지정한 보드 좌표에 존재하는 기물을 찾아 제거한다.
    // Player / Enemy / Neutral 모두 동일한 방식으로 처리한다.
    //
    // Check Remove Piece Team이 켜진 경우에는
    // 지정한 Team과 실제 기물의 Team이 일치할 때만 제거한다.
    private void ExecuteRemovePieceStep(
        EventSequenceStepData step)
    {
        if (pieceManager == null)
        {
            Debug.LogWarning(
                "이벤트 기물 제거 실패: " +
                "PieceManager가 연결되지 않았습니다."
            );

            return;
        }

        Vector2Int removePosition =
            step.removePiecePosition;

        Piece targetPiece =
            pieceManager.GetPieceAt(
                removePosition.x,
                removePosition.y
            );

        if (targetPiece == null)
        {
            Debug.LogWarning(
                $"이벤트 기물 제거 실패: " +
                $"{removePosition}에 기물이 없습니다."
            );

            return;
        }

        // Team 검사 옵션을 켠 경우에는
        // 지정한 진영의 기물인지 먼저 확인한다.
        if (step.checkRemovePieceTeam &&
            targetPiece.Team !=
                step.removePieceTeam)
        {
            Debug.LogWarning(
                $"이벤트 기물 제거 취소: " +
                $"{removePosition}의 기물은 " +
                $"{targetPiece.Team} 진영이며, " +
                $"설정된 제거 대상은 " +
                $"{step.removePieceTeam} 진영입니다."
            );

            return;
        }

        PieceTeam removedTeam =
            targetPiece.Team;

        PieceType removedPieceType =
            targetPiece.PieceType;

        // <변경부분> 기존 PieceManager의 공용 제거 함수를 사용하여
        // 보드 배열과 실제 Piece 오브젝트를 함께 정리한다.
        pieceManager.RemovePiece(
            targetPiece
        );

        Debug.Log(
            $"이벤트 기물 제거 완료: " +
            $"{removedTeam} / " +
            $"{removedPieceType} / " +
            $"{removePosition}"
        );
    }

    // <변경부분> 현재 시퀀스를 정상 완료 처리한다.
    public void CompleteSequence()
    {
        if (isSequenceActive == false)
        {
            return;
        }

        isSequenceActive =
            false;

        Debug.Log(
            $"이벤트 시퀀스 완료: " +
            $"{sequenceData?.sequenceName}"
        );

        HandleSequenceCompletion();
    }

    // <변경부분> EventSequenceData의 Completion 설정에 따라
    // 시퀀스 종료 후 이동 경로를 처리한다.
    private void HandleSequenceCompletion()
    {
        if (sequenceData == null)
        {
            return;
        }

        switch (sequenceData.completionType)
        {
            case EventSequenceCompletionType.None:
                return;

            case EventSequenceCompletionType.WorldMap:
                SceneManager.LoadScene(
                    "WorldMapScene"
                );
                return;

            case EventSequenceCompletionType.LoadScene:
                if (string.IsNullOrWhiteSpace(
                        sequenceData
                            .completionSceneName))
                {
                    Debug.LogWarning(
                        "이벤트 완료 씬 이동 실패: " +
                        "completionSceneName이 비어 있습니다."
                    );

                    return;
                }

                // <변경부분> EventSequenceData에
                // 다음 TextCutsceneData가 지정되어 있는 경우에만
                // Scene 이동 직전에 Pending Cutscene Data로 등록한다.
                //
                // null이면 아무것도 하지 않으므로
                // 기존 EventSequenceData와 일반 Scene 이동에는
                // 전혀 영향을 주지 않는다.
                if (sequenceData.completionCutsceneData != null)
                {
                    TextCutsceneRuntimeState
                        .SetPendingCutsceneData(
                            sequenceData.completionCutsceneData
                        );

                    Debug.Log(
                        $"이벤트 완료 컷씬 데이터 전달: " +
                        $"{sequenceData.completionCutsceneData.name}"
                    );
                }

                // <변경부분> CutsceneData 등록이 필요한 경우
                // 반드시 등록을 먼저 끝낸 뒤 Scene을 이동한다.
                SceneManager.LoadScene(
                    sequenceData
                        .completionSceneName
                );

                return;

            case EventSequenceCompletionType.BattleWin:
                // <변경부분> Event Sequence가 정상 완료되면
                // 일반 Battle의 승리 처리 흐름으로 연결한다.
                //
                // 이 경로를 사용하면 기존 전투와 동일하게:
                // 플레이어 기물 상태 저장
                // → BattleEndFlowController
                // → StageBattleData 보상 정산
                // → BattleRewardPopupUI 표시
                // 순서로 진행된다.
                if (battleManager == null)
                {
                    Debug.LogWarning(
                        "이벤트 BattleWin 완료 실패: " +
                        "BattleManager가 연결되지 않았습니다."
                    );

                    return;
                }

                battleManager
                    .CompleteBattleWinFromEvent();

                Debug.Log(
                    "이벤트 BattleWin 완료: " +
                    "일반 전투 승리 / 보상 흐름으로 전달했습니다."
                );

                return;
        }
    }

    // <변경부분> 현재 실행 중인 이벤트를 강제로 정지한다.
    //
    // 씬 테스트 또는 외부 이벤트 취소에 사용할 수 있다.
    public void StopSequence()
    {
        isSequenceActive =
            false;

        // <변경부분> 진행 중이던 강제 입력 대기 상태도
        // 시퀀스 종료와 함께 즉시 해제한다.
        isWaitingForPieceSelection =
    false;

        requiredPiecePosition =
            Vector2Int.zero;

        requiredPieceTeam =
            PieceTeam.Player;

        // <변경부분> 진행 중이던
        // ForceTileSelect 상태도 함께 초기화한다.
        isWaitingForTileSelection =
    false;

        requiredTilePosition =
     Vector2Int.zero;

        // <변경부분> 진행 중이던 ForceButton 입력 제한도
        // Sequence 종료와 함께 즉시 해제한다.
        isWaitingForButtonInput =
            false;

        requiredButtonType =
            EventSequenceButtonType.None;

        StopSequenceCoroutine();

        currentStepIndex =
            -1;

        if (eventGuideUI != null)
        {
            eventGuideUI.HideImmediately();
        }

        // <변경부분> 이벤트가 중간에 종료되더라도
        // 현재 표시 중이던 기물 / 타일 / 버튼 마커를 즉시 제거한다.
        if (eventMarkerUI != null)
        {
            eventMarkerUI.Hide();
        }

        Debug.Log(
                    "이벤트 시퀀스 강제 종료"
        );
    }

    // 현재 실행 중인 메인 코루틴 정리
    private void StopSequenceCoroutine()
    {
        if (sequenceCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            sequenceCoroutine
        );

        sequenceCoroutine =
            null;
    }

    private void OnDisable()
    {
        StopSequenceCoroutine();

        isSequenceActive =
            false;

        // <변경부분> 컴포넌트 비활성화 시에도
        // 이벤트 입력 제한 상태가 남지 않도록 초기화한다.
        isWaitingForPieceSelection =
     false;

        requiredPiecePosition =
            Vector2Int.zero;

        requiredPieceTeam =
            PieceTeam.Player;

        // <변경부분> 컴포넌트가 꺼지는 경우에도
        // 강제 타일 입력 상태가 남지 않도록 초기화한다.
        isWaitingForTileSelection =
            false;

        requiredTilePosition =
            Vector2Int.zero;

        // <변경부분> 컴포넌트 비활성화 시에도
        // ForceButton 입력 제한 상태가 남지 않도록 초기화한다.
        isWaitingForButtonInput =
            false;

        requiredButtonType =
     EventSequenceButtonType.None;

        // <변경부분> 씬 종료 또는 Controller 비활성화 시
        // 마커가 화면에 남지 않도록 함께 정리한다.
        if (eventMarkerUI != null)
        {
            eventMarkerUI.Hide();
        }
    }
}