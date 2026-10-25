using System.Collections.Generic;
using UnityEngine;

// <변경부분> 로그라이크 런 전체 상태를 씬 이동 사이에서 유지하는 매니저
// 현재 1차 구현에서는 플레이어 기물 상태만 저장한다.

// <변경부분>
// 런 전체에서 공통으로 사용하는 낮·밤 상태.
// 실제 상태는 엔드 타이머 눈금으로 계산한다.
public enum RunDayNightState
{
    Day = 0,
    Night = 1
}

public class RunStateManager : MonoBehaviour
{
    public static RunStateManager Instance { get; private set; }


    // <변경부분>
    // Run 전체에서 공통으로 사용하는
    // 전투 아이템 최대 보유 개수.
    //
    // Shop 구매 제한 및 RunItemBarUI에서
    // 동일한 값을 사용하도록 관리한다.
    public const int MaxBattleItemCount = 4;


    // <변경부분>
    // 엔드 타이머의 실제 값이 변경되었을 때
    // Battle / WorldMap UI에 갱신을 알리는 이벤트.
    //
    // UI는 타이머 데이터를 직접 저장하지 않고
    // 이 이벤트를 받은 뒤 RunStateManager의 현재 값을 다시 읽는다.
    public event System.Action EndTimerChanged;

    // <변경부분>
    // 현재 런의 낮·밤 상태가 실제로 바뀌었을 때만 알린다.
    // 향후 환경 조명, 반딧불, 랜턴 등이 이 이벤트를 구독할 수 있다.
    public event System.Action<RunDayNightState> DayNightStateChanged;


    [Header("Player Runtime Pieces")]
    // <변경부분> 현재 런에서 유지되는 플레이어 기물 상태 목록
    [SerializeField]
    private List<PlayerPieceRuntimeData> playerPieceRuntimeDataList =
        new List<PlayerPieceRuntimeData>();
    [Header("Currency")]
    // <변경부분> 현재 런에서 보유 중인 금화
    [SerializeField] private int goldAmount = 0;

    // <변경부분>
    // [TEMP SHOP TEST]
    // 상점 구매 기능을 테스트하기 위한 임시 Gold 설정.
    //
    // Inspector에서 활성화하면 Unity Editor Play 시
    // 테스트용 50G를 지급한다.
    //
    // 일반 게임 플레이 및 배포 빌드에는 적용하지 않는다.
    // 상점 테스트 완료 후 이 설정과 관련 코드를 제거할 예정.
    [Header("TEMP - Shop Test Gold")]
    [SerializeField]
    private bool enableTemporaryShopTestGold = false;

    private const int TemporaryShopTestGoldAmount = 50;

    [Header("Battle Items")]
    // <변경부분> 현재 런에서 보유 중인 전투 아이템 목록
    // 소모성 아이템이므로 같은 종류의 중복 보유를 허용한다.
    [SerializeField]
    private List<BattleItemData> battleItemDataList =
        new List<BattleItemData>();

    [Header("Battle Relics")]
    // <변경부분> 현재 런에서 보유 중인 전투 유물 목록
    // 유물은 같은 BattleRelicType을 중복 보유하지 않는다.
    [SerializeField]
    private List<BattleRelicData> battleRelicDataList =
    new List<BattleRelicData>();


    // <변경부분>
    // 엔드 타이머 한 바퀴를 구성하는 고정 눈금 수.
    //
    // Battle / WorldMap 어디에서 시간이 진행되더라도
    // 동일하게 24눈금을 기준으로 한 사이클을 계산한다.
    public const int EndTimerTicksPerCycle =
     24;

    // <변경부분>
    // 0~11눈금은 낮, 12~23눈금은 밤으로 구분한다.
    // 23에서 0으로 돌아오면 다시 낮이 된다.
    public const int NightStartTick = 12;




    // <변경부분>
    // 새 Run 시작 시 사용할 엔드 타이머 사이클 수.
    //
    // 현재 기획 기본값은 15이며,
    // Inspector에서 밸런싱 단계에 맞춰 변경할 수 있다.
    [Header("엔드 타이머")]
    [SerializeField]
    [Min(1)]
    private int initialEndTimerCycles =
        15;


    // <변경부분>
    // 현재 Run에서 실제로 남아 있는 엔드 타이머 사이클 수.
    //
    // 이 값과 currentEndTimerTick은
    // Battle / WorldMap Scene 이동 사이에도 유지된다.
    [SerializeField]
    [Min(0)]
    private int remainingEndTimerCycles =
        15;


    // <변경부분>
    // 현재 사이클에서 진행된 눈금.
    //
    // 범위:
    // 0 ~ 23
    //
    // 24번째 눈금이 진행되는 순간 0으로 돌아가며
    // remainingEndTimerCycles가 1 감소한다.
    [SerializeField]
    [Range(0, EndTimerTicksPerCycle - 1)]
    private int currentEndTimerTick =
        0;

    // <변경부분> 스테이지 전용 엔드타이머 시간 설정.
    //
    // 실제 런 데이터와 구분하여 관리한다.
    // 해당 스테이지가 끝나면 복구하거나 변경된 시간을 유지한다.
    private bool isStageEndTimerOverrideActive = false;
    private bool isStageEndTimerFrozen = false;

    private StageEndTimerExitMode activeStageEndTimerExitMode =
        StageEndTimerExitMode.RestoreOriginal;

    // <변경부분> 전투 진입 직전의 런 시간.
    // 원래 시간 복구 모드에서 사용한다.
    private int endTimerCyclesBeforeStageOverride = 0;
    private int endTimerTickBeforeStageOverride = 0;


    // <변경부분>
    // Run 전체에서 공통으로 사용할 End Stage 설정.
    //
    // Battle / WorldMap이 각각 최종 스테이지 데이터를
    // 따로 보관하지 않고 RunStateManager의 이 값을 공통으로 사용한다.
    [Header("엔드 스테이지")]
    [SerializeField]
    private StageBattleData endStageBattleData;


    // <변경부분>
    // End Stage를 실행할 Battle Scene 이름.
    //
    // 현재 일반 전투와 동일한 BattleScene을 사용하므로
    // 기본값을 BattleScene으로 둔다.
    [SerializeField]
    private string endStageBattleSceneName =
        "BattleScene";


    // <변경부분> 저장된 플레이어 기물 데이터가 있는지 여부
    public bool HasPlayerPieceRuntimeData
    {
        get
        {
            return playerPieceRuntimeDataList != null &&
                   playerPieceRuntimeDataList.Count > 0;
        }
    }


    // <변경부분>
    // 현재 남아 있는 엔드 타이머 사이클 수.
    public int RemainingEndTimerCycles
    {
        get
        {
            return remainingEndTimerCycles;
        }
    }


    // <변경부분>
    // 현재 사이클에서 진행된 눈금.
    //
    // UI는 이 값을 이용해
    // 24눈금 중 현재 바늘 위치를 계산한다.
    public int CurrentEndTimerTick
    {
        get
        {
            return currentEndTimerTick;
        }
    }

    // <변경부분>
    // 현재 눈금에서 낮·밤을 계산한다. 별도 상태를 저장하지 않으므로
    // 씬 이동 후에도 시계와 낮·밤 상태가 어긋나지 않는다.
    public RunDayNightState CurrentDayNightState
    {
        get
        {
            return currentEndTimerTick < NightStartTick
                ? RunDayNightState.Day
                : RunDayNightState.Night;
        }
    }


    // <변경부분>
    // Run 전체에서 공통으로 사용할
    // End Stage의 StageBattleData를 반환한다.
    public StageBattleData EndStageBattleData
    {
        get
        {
            return endStageBattleData;
        }
    }


    // <변경부분>
    // Run 전체에서 공통으로 사용할
    // End Stage Battle Scene 이름을 반환한다.
    public string EndStageBattleSceneName
    {
        get
        {
            return endStageBattleSceneName;
        }
    }


    // <변경부분>
    // 엔드 타이머가 모두 소진되었는지 여부.
    //
    // Battle / WorldMap은 이 값을 확인한 뒤
    // 각자의 현재 콘텐츠가 끝나는 시점에
    // End Stage 진입 여부를 결정한다.
    public bool IsEndTimerExpired
    {
        get
        {
            return remainingEndTimerCycles <= 0;
        }
    }


    // <변경부분> 스테이지에서 지정한 엔드타이머 설정을 적용한다.
    //
    // 전투 진입 전의 런 시간을 보관한 후
    // 지정한 눈금으로 현재 시간을 강제로 변경한다.
    //
    // 남은 사이클 수는 변경하지 않는다.
    public bool BeginStageEndTimerOverride(
        int forcedTick,
        bool freezeDuringBattle,
        StageEndTimerExitMode exitMode)
    {
        // 이미 시간 강제 변경이 적용 중이라면
        // 원래 시간을 덮어쓰지 않는다.
        if (isStageEndTimerOverrideActive)
        {
            Debug.LogWarning(
                "스테이지 시간 강제 변경 실패: " +
                "이미 다른 스테이지의 설정이 적용 중입니다.",
                this
            );

            return false;
        }

        RunDayNightState previousDayNightState =
            CurrentDayNightState;

        // <변경부분> 원래 런 시간 저장.
        endTimerCyclesBeforeStageOverride =
            remainingEndTimerCycles;

        endTimerTickBeforeStageOverride =
            currentEndTimerTick;

        // <변경부분> 현재 스테이지 설정 저장.
        activeStageEndTimerExitMode =
            exitMode;

        isStageEndTimerFrozen =
            freezeDuringBattle;

        isStageEndTimerOverrideActive =
            true;

        // <변경부분> 현재 눈금을 지정한 값으로 변경한다.
        currentEndTimerTick =
            Mathf.Clamp(
                forcedTick,
                0,
                EndTimerTicksPerCycle - 1
            );

        // 현재 엔드타이머 UI 갱신.
        EndTimerChanged?.Invoke();

        // 낮과 밤이 변경되었다면 기존 조명 구독자에게 알린다.
        if (previousDayNightState != CurrentDayNightState)
        {
            DayNightStateChanged?.Invoke(
                CurrentDayNightState
            );
        }

        Debug.Log(
            $"스테이지 엔드타이머 강제 변경: " +
            $"눈금 {currentEndTimerTick}, " +
            $"시간 고정 {isStageEndTimerFrozen}, " +
            $"종료 처리 {activeStageEndTimerExitMode}",
            this
        );

        return true;
    }


    // <변경부분> 스테이지를 떠날 때 시간 강제 변경을 종료한다.
    //
    // 원래 시간 복구:
    // 진입 전 사이클과 눈금을 모두 복구한다.
    //
    // 변경된 시간 유지:
    // 전투 종료 당시의 사이클과 눈금을 그대로 유지한다.
    public void EndStageEndTimerOverride()
    {
        if (isStageEndTimerOverrideActive == false)
        {
            return;
        }

        RunDayNightState previousDayNightState =
            CurrentDayNightState;

        bool shouldRestore =
            activeStageEndTimerExitMode ==
            StageEndTimerExitMode.RestoreOriginal;

        // <변경부분> 임시 시간 설정 해제.
        isStageEndTimerOverrideActive = false;
        isStageEndTimerFrozen = false;

        activeStageEndTimerExitMode =
            StageEndTimerExitMode.RestoreOriginal;

        if (shouldRestore)
        {
            remainingEndTimerCycles =
                endTimerCyclesBeforeStageOverride;

            currentEndTimerTick =
                endTimerTickBeforeStageOverride;
        }

        // 엔드타이머 UI 갱신.
        EndTimerChanged?.Invoke();

        // 시간대 변경 여부에 따라 기존 조명 이벤트 발생.
        if (previousDayNightState != CurrentDayNightState)
        {
            DayNightStateChanged?.Invoke(
                CurrentDayNightState
            );
        }

        Debug.Log(
            shouldRestore
                ? "스테이지 종료: 진입 전 엔드타이머 시간으로 복구했습니다."
                : "스테이지 종료: 변경된 엔드타이머 시간을 유지합니다.",
            this
        );
    }


    // <변경부분>
    // Battle Turn, WorldMap 이동 등에서 공통으로 사용하는
    // Run 시간 진행 함수.
    //
    // 시간의 실제 소유자는 RunStateManager 하나뿐이며,
    // Battle / WorldMap은 각자 타이머를 계산하지 않고
    // 이 함수에 소모할 눈금 수만 전달한다.
    public bool AdvanceEndTimer(
       int tickAmount)
    {
        if (tickAmount <= 0)
        {
            Debug.LogWarning(
                $"엔드 타이머 진행 실패: " +
                $"tickAmount는 1 이상이어야 합니다. / " +
                $"{tickAmount}"
            );

            return false;
        }

        // <변경부분> 스테이지에서 엔드타이머 시간을 고정했다면
        // 기존 턴 진행은 유지하고 시간 진행만 차단한다.
        //
        // 최종전투처럼 남은 사이클이 0인 상태에서도
        // 강제로 설정한 밤 시간대가 유지되도록
        // 기존 시간 소진 판정보다 먼저 확인한다.
        if (isStageEndTimerOverrideActive &&
            isStageEndTimerFrozen)
        {
            return false;
        }

        // 이미 시간이 모두 소진된 Run에서는
        // 추가 시간 진행을 허용하지 않는다.
        if (remainingEndTimerCycles <= 0)
        {
            remainingEndTimerCycles =
                0;

            currentEndTimerTick =
                0;

            return false;
        }


        // <변경부분>
        // 눈금을 변경하기 전에 현재 낮·밤 상태를 기록한다.
        // 이후 최종 상태와 비교해 실제 전환이 있을 때만 이벤트를 발생시킨다.
        RunDayNightState previousDayNightState =
            CurrentDayNightState;

        int totalTick =
            currentEndTimerTick +
            tickAmount;


        // 한 번에 여러 눈금이 들어오는 경우도 지원한다.
        //
        // 예:
        // 현재 23눈금 + 2
        // → 사이클 1 감소
        // → 현재 눈금 1
        while (totalTick >= EndTimerTicksPerCycle &&
               remainingEndTimerCycles > 0)
        {
            totalTick -=
                EndTimerTicksPerCycle;

            remainingEndTimerCycles--;
        }


        // 마지막 사이클까지 모두 소모되면
        // 상태를 정확히 0 / 0으로 고정한다.
        if (remainingEndTimerCycles <= 0)
        {
            remainingEndTimerCycles =
                0;

            currentEndTimerTick =
                0;
        }
        else
        {
            currentEndTimerTick =
                totalTick;
        }


        // <변경부분>
        // 기존 타이머 로그에 현재 낮·밤 상태도 함께 표시한다.
        Debug.Log(
     $"런 엔드 타이머 진행: " +
     $"남은 사이클 {remainingEndTimerCycles}, " +
     $"현재 눈금 {currentEndTimerTick}/" +
     $"{EndTimerTicksPerCycle}, " +
     $"시간대 {CurrentDayNightState}"
 );


        // <변경부분>
        // 실제 엔드 타이머 값이 변경되었으므로
        // 현재 Scene의 타이머 UI에 즉시 갱신을 알린다.
        EndTimerChanged?.Invoke();

        // <변경부분>
        // 11→12(밤) 또는 23→0(낮) 등
        // 최종 시간대가 달라졌을 때만 이벤트를 발생시킨다.
        // 한 번에 여러 눈금을 진행하더라도 최종 상태를 기준으로 한다.
        if (previousDayNightState != CurrentDayNightState)
        {
            DayNightStateChanged?.Invoke(CurrentDayNightState);
        }


        return true;
    }


    private static bool hasPendingTitleTimer;
    private static int pendingTitleCycles, pendingTitleTick;
    private static StageBattleData pendingTitleEndStage;
    private static string pendingTitleEndScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearPendingTitleTimer() { hasPendingTitleTimer = false; }

    public static void SetNewRunEndTimer(int cycles, int tick, StageBattleData endStage = null, string endScene = "BattleScene")
    {
        pendingTitleCycles = Mathf.Max(1, cycles);
        pendingTitleTick = Mathf.Clamp(tick, 0, EndTimerTicksPerCycle - 1);
        pendingTitleEndStage = endStage;
        pendingTitleEndScene = endScene;
        hasPendingTitleTimer = true;
        if (Instance != null) Instance.ApplyPendingTitleTimer();
        else new GameObject("RunStateManager").AddComponent<RunStateManager>();
    }

    private void ApplyPendingTitleTimer()
    {
        if (!hasPendingTitleTimer) return;
        hasPendingTitleTimer = false;
        var previous = CurrentDayNightState;

        // <변경부분> 새로운 런 시작 시 이전 전투의
        // 임시 시간 강제 변경 설정을 모두 해제한다.
        isStageEndTimerOverrideActive = false;
        isStageEndTimerFrozen = false;

        activeStageEndTimerExitMode =
            StageEndTimerExitMode.RestoreOriginal;

        if (pendingTitleEndStage != null)
        {
            endStageBattleData = pendingTitleEndStage;
            endStageBattleSceneName = string.IsNullOrWhiteSpace(pendingTitleEndScene) ? "BattleScene" : pendingTitleEndScene;
        }
        pendingTitleEndStage = null;
        pendingTitleEndScene = null;
        initialEndTimerCycles = pendingTitleCycles;
        remainingEndTimerCycles = pendingTitleCycles;
        currentEndTimerTick = pendingTitleTick;
        EndTimerChanged?.Invoke();
        if (previous != CurrentDayNightState)
            DayNightStateChanged?.Invoke(CurrentDayNightState);
    }

    private void Awake()
    {
        // <변경부분> 씬 이동 중에도 하나의 RunStateManager만 유지
        if (Instance != null && Instance != this)
        {
            if (Instance.endStageBattleData == null && endStageBattleData != null)
            {
                Instance.endStageBattleData = endStageBattleData;
                Instance.endStageBattleSceneName = endStageBattleSceneName;
            }
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyPendingTitleTimer();

        // <변경부분>
        // Unity Editor 테스트 중에만 임시 Shop Gold를 지급한다.
        // Singleton으로 유지되는 실제 RunStateManager에서만 실행된다.
        ApplyTemporaryShopTestGold();
    }

    // <변경부분>
    // [TEMP SHOP TEST]
    // Editor Play 시작 또는 새 Run 초기화 시
    // 임시 테스트 Gold를 50G로 설정한다.
    //
    // Scene 이동에서는 다시 호출하지 않으므로
    // Gold가 반복 지급되지 않는다.
    private void ApplyTemporaryShopTestGold()
    {
#if UNITY_EDITOR
        if (enableTemporaryShopTestGold == false)
        {
            return;
        }

        goldAmount = TemporaryShopTestGoldAmount;

        Debug.Log(
            $"[TEMP Shop Test] " +
            $"테스트용 Gold 지급: {goldAmount}G"
        );
#endif
    }

    // <변경부분> 현재 플레이어 기물 상태를 런 상태로 저장
    public void SavePlayerPieces(List<PlayerPieceRuntimeData> runtimeDataList)
    {
        playerPieceRuntimeDataList.Clear();

        if (runtimeDataList == null)
        {
            Debug.LogWarning("플레이어 기물 런타임 저장 실패: 전달된 데이터가 null입니다.");
            return;
        }

        for (int i = 0; i < runtimeDataList.Count; i++)
        {
            PlayerPieceRuntimeData runtimeData = runtimeDataList[i];

            if (runtimeData == null)
            {
                continue;
            }

            playerPieceRuntimeDataList.Add(runtimeData.Clone());
        }

        Debug.Log($"플레이어 기물 상태 저장 완료: {playerPieceRuntimeDataList.Count}개");
    }

    // <변경부분> 저장된 플레이어 기물 상태 복사본 반환
    public List<PlayerPieceRuntimeData> GetPlayerPiecesCopy()
    {
        List<PlayerPieceRuntimeData> copiedList = new List<PlayerPieceRuntimeData>();

        for (int i = 0; i < playerPieceRuntimeDataList.Count; i++)
        {
            PlayerPieceRuntimeData runtimeData = playerPieceRuntimeDataList[i];

            if (runtimeData == null)
            {
                continue;
            }

            copiedList.Add(runtimeData.Clone());
        }

        return copiedList;
    }

    // <변경부분> 현재 런에서 보유 중인 플레이어 기물 수를 반환하는 함수
    public int GetPlayerPieceCount()
    {
        if (playerPieceRuntimeDataList == null)
        {
            return 0;
        }

        return playerPieceRuntimeDataList.Count;
    }

    // <변경부분> 현재 런에서 보유 중인 금화량을 반환하는 함수
    public int GetGoldAmount()
    {
        return goldAmount;
    }

    // <변경부분> 금화 보상을 현재 런 상태에 추가하는 함수
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        goldAmount += amount;

        Debug.Log($"금화 획득: +{amount} / 현재 보유 금화 {goldAmount}");
    }

    // <변경부분>
    // 현재 런에서 Gold를 안전하게 차감하는 함수.
    //
    // 사용 목적:
    // - ShopEvent에서 상품 구매 시 Gold 차감
    // - 이후 Gold를 소비하는 다른 시스템에서도 재사용 가능
    //
    // 처리 규칙:
    // 1. 차감 금액이 0 이하이면 실패
    // 2. 보유 Gold가 부족하면 실패
    // 3. 모든 조건을 만족하면 Gold 차감
    // 4. 실패한 경우 기존 Gold 값은 변경하지 않는다.
    //
    // 반환값:
    // true  = Gold 차감 성공
    // false = Gold 차감 실패
    public bool TrySpendGold(int amount)
    {
        // 유효하지 않은 차감 금액은 처리하지 않는다.
        if (amount <= 0)
        {
            Debug.LogWarning(
                $"Gold 차감 실패: 올바르지 않은 차감 금액 {amount}"
            );

            return false;
        }

        // 현재 보유 Gold가 부족하면 차감하지 않는다.
        if (goldAmount < amount)
        {
            Debug.Log(
                $"Gold 차감 실패: 보유 Gold 부족 / " +
                $"필요 {amount}G / 보유 {goldAmount}G"
            );

            return false;
        }

        // 모든 조건을 통과했으므로 실제 Gold를 차감한다.
        goldAmount -= amount;

        Debug.Log(
            $"Gold 차감 완료: -{amount}G / " +
            $"현재 보유 Gold {goldAmount}G"
        );

        return true;
    }

    // <변경부분> 보상으로 획득한 플레이어 기물을 런 상태에 추가하는 함수
    // 최대 기물 수를 넘으면 추가하지 않는다.
    public bool TryAddPlayerPiece(PlayerPieceRuntimeData runtimeData, int maxPieceCount)
    {
        if (runtimeData == null)
        {
            Debug.LogWarning("플레이어 기물 추가 실패: runtimeData가 null입니다.");
            return false;
        }

        if (playerPieceRuntimeDataList.Count >= maxPieceCount)
        {
            Debug.Log($"플레이어 기물 추가 실패: 최대 기물 수 도달 {playerPieceRuntimeDataList.Count} / {maxPieceCount}");
            return false;
        }

        playerPieceRuntimeDataList.Add(runtimeData.Clone());

        Debug.Log($"플레이어 기물 추가 완료: {runtimeData.pieceData?.pieceId} / 현재 {playerPieceRuntimeDataList.Count}개");

        return true;
    }

    // <변경부분> 현재 런에서 보유 중인 전투 아이템 목록 복사본 반환
    // 외부에서 원본 리스트를 직접 수정하지 못하도록 새 List로 반환한다.
    public List<BattleItemData> GetBattleItemsCopy()
    {
        return new List<BattleItemData>(battleItemDataList);
    }

    // <변경부분> 전투 아이템을 런 보유 목록에 추가
    // 아이템은 소모품이므로 같은 종류의 중복 획득을 허용한다.
    public bool TryAddBattleItem(BattleItemData itemData, int maxItemCount)
    {
        // 추가할 아이템 데이터가 없거나 유효하지 않으면 저장하지 않는다.
        if (itemData == null || itemData.itemType == BattleItemType.None)
        {
            Debug.LogWarning("런 아이템 추가 실패: 유효한 BattleItemData가 아닙니다.");
            return false;
        }

        // 현재 아이템 수가 최대 슬롯 수에 도달했다면 추가하지 않는다.
        if (battleItemDataList.Count >= maxItemCount)
        {
            Debug.Log(
                $"런 아이템 추가 실패: 최대 아이템 수 도달 " +
                $"{battleItemDataList.Count} / {maxItemCount}"
            );

            return false;
        }

        // 아이템 ScriptableObject 참조를 런 보유 목록에 저장한다.
        battleItemDataList.Add(itemData);

        Debug.Log(
            $"런 아이템 저장 완료: {itemData.itemName} / " +
            $"현재 {battleItemDataList.Count}개"
        );

        return true;
    }


    // ============================================================
    // Shop Battle Item Purchase
    // ============================================================

    // <변경부분>
    // 상점 구매 결과.
    //
    // 구매 성공 여부뿐만 아니라 실패 이유도 반환한다.
    // 이후 SkillFailurePopup Localization 연결 시
    // 이 결과를 기준으로 안내 문구를 선택한다.
    public enum BattleItemPurchaseResult
    {
        Success,
        InvalidItem,
        InvalidPrice,
        InvalidCapacity,
        InventoryFull,
        InsufficientGold
    }


    // <변경부분>
    // 상점에서 BattleItemData를 구매하는 공용 함수.
    //
    // 구매 순서:
    // 1. 아이템 데이터 검증
    // 2. 가격 및 최대 슬롯 수 검증
    // 3. 현재 아이템 보유 개수 검증
    // 4. 보유 Gold 검증
    // 5. 아이템 지급 및 Gold 차감
    //
    // 모든 구매 조건을 확인한 다음에만
    // 실제 RunState 데이터를 변경한다.
    //
    // 구매 실패 시:
    // - Gold 변경 없음
    // - 아이템 목록 변경 없음
    //
    // 판매 가격 0G는 무료 상품으로 허용한다.
    // 무료 상품에는 Gold 차감이 발생하지 않는다.
    public BattleItemPurchaseResult TryPurchaseBattleItem(
        BattleItemData itemData,
        int price,
        int maxItemCount)
    {
        // 유효하지 않은 아이템은 구매할 수 없다.
        if (itemData == null ||
            itemData.itemType == BattleItemType.None)
        {
            return BattleItemPurchaseResult.InvalidItem;
        }

        // 음수 가격은 허용하지 않는다.
        if (price < 0)
        {
            return BattleItemPurchaseResult.InvalidPrice;
        }

        // 최대 슬롯 수 설정 자체가 잘못된 경우.
        if (maxItemCount <= 0)
        {
            return BattleItemPurchaseResult.InvalidCapacity;
        }

        // 아이템 슬롯이 이미 가득 찬 경우.
        if (battleItemDataList.Count >= maxItemCount)
        {
            return BattleItemPurchaseResult.InventoryFull;
        }

        // Gold가 부족한 경우.
        if (goldAmount < price)
        {
            return BattleItemPurchaseResult.InsufficientGold;
        }

        // <변경부분>
        // 모든 검증이 끝났으므로 구매를 확정한다.
        //
        // 중간에 다른 UI나 이벤트를 호출하지 않고
        // RunState 내부에서 두 데이터를 함께 변경한다.

        // 먼저 구매 아이템을 런 목록에 추가한다.
        battleItemDataList.Add(itemData);

        // 구매 가격만큼 Gold를 차감한다.
        // 0G 상품은 기존 Gold가 유지된다.
        goldAmount -= price;

        Debug.Log(
            $"[RunStateManager] 아이템 구매 성공: " +
            $"{itemData.itemName} / " +
            $"가격 {price}G / " +
            $"남은 Gold {goldAmount}G / " +
            $"아이템 {battleItemDataList.Count}/{maxItemCount}"
        );

        return BattleItemPurchaseResult.Success;
    }


    // <변경부분> 사용한 전투 아이템을 런 보유 목록의 같은 슬롯에서 제거
    // 전투 슬롯과 런 저장 목록이 같은 순서로 유지되는 것을 기준으로 처리한다.
    public bool RemoveBattleItemAt(int slotIndex)
    {
        // 런 아이템 목록 범위를 벗어난 슬롯 번호는 처리하지 않는다.
        if (slotIndex < 0 || slotIndex >= battleItemDataList.Count)
        {
            Debug.LogWarning($"런 아이템 제거 실패: 잘못된 슬롯 번호 {slotIndex}");
            return false;
        }

        // 로그 출력을 위해 제거 전 아이템 데이터를 저장한다.
        BattleItemData removedItem = battleItemDataList[slotIndex];

        // 사용한 아이템을 런 보유 목록에서 제거한다.
        battleItemDataList.RemoveAt(slotIndex);

        Debug.Log(
            $"런 아이템 소모 반영 완료: {removedItem?.itemName} / " +
            $"현재 {battleItemDataList.Count}개"
        );

        return true;
    }

    // <변경부분> 현재 런에서 보유 중인 전투 유물 목록 복사본 반환
    // 외부에서 원본 리스트를 직접 수정하지 못하도록 새 List로 반환한다.
    public List<BattleRelicData> GetBattleRelicsCopy()
    {
        return new List<BattleRelicData>(battleRelicDataList);
    }

    // <변경부분> 특정 유물 타입을 현재 런에서 보유 중인지 확인
    // 같은 타입의 유물이 중복 저장되는 것을 방지할 때 사용한다.
    public bool HasBattleRelic(BattleRelicType relicType)
    {
        // None은 실제 유물이 아니므로 보유 중으로 판정하지 않는다.
        if (relicType == BattleRelicType.None)
        {
            return false;
        }

        // 현재 런에서 보유 중인 모든 유물을 검사한다.
        for (int i = 0; i < battleRelicDataList.Count; i++)
        {
            BattleRelicData relicData = battleRelicDataList[i];

            if (relicData == null)
            {
                continue;
            }

            // 같은 유물 타입이 하나라도 있으면 이미 보유 중이다.
            if (relicData.relicType == relicType)
            {
                return true;
            }
        }

        return false;
    }

    // <변경부분> 전투 유물을 런 보유 목록에 추가
    // 같은 유물 타입은 중복 저장하지 않는다.
    public bool TryAddBattleRelic(
        BattleRelicData relicData,
        int maxRelicCount)
    {
        // 추가할 유물 데이터가 없거나 유효하지 않으면 저장하지 않는다.
        if (relicData == null ||
            relicData.relicType == BattleRelicType.None)
        {
            Debug.LogWarning("런 유물 추가 실패: 유효한 BattleRelicData가 아닙니다.");
            return false;
        }

        // 같은 타입의 유물을 이미 보유 중이면 중복 추가하지 않는다.
        if (HasBattleRelic(relicData.relicType))
        {
            Debug.Log(
                $"런 유물 추가 실패: 이미 보유 중인 유물입니다. / " +
                $"{relicData.relicName}"
            );

            return false;
        }

        // 현재 유물 수가 최대 슬롯 수에 도달했다면 추가하지 않는다.
        if (battleRelicDataList.Count >= maxRelicCount)
        {
            Debug.Log(
                $"런 유물 추가 실패: 최대 유물 수 도달 " +
                $"{battleRelicDataList.Count} / {maxRelicCount}"
            );

            return false;
        }

        // 유물 ScriptableObject 참조를 런 보유 목록에 저장한다.
        battleRelicDataList.Add(relicData);

        Debug.Log(
            $"런 유물 저장 완료: {relicData.relicName} / " +
            $"현재 {battleRelicDataList.Count}개"
        );

        return true;
    }

    // <변경부분> 새 런 시작 또는 디버그 초기화용
    public void ClearRunState()
    {
        WorldMapEventRuntimeState.Clear();
        // <변경부분>
        // 초기화 전의 낮·밤 상태를 기억해 밤→낮 전환을 알린다.
        RunDayNightState previousDayNightState =
            CurrentDayNightState;

        // <변경부분> 새 런 시작 시
        // 이전 전투의 시간 강제 변경 상태도 초기화한다.
        isStageEndTimerOverrideActive = false;
        isStageEndTimerFrozen = false;

        activeStageEndTimerExitMode =
            StageEndTimerExitMode.RestoreOriginal;

        // <변경부분> 새 런 시작 시 저장된 플레이어 기물 상태 초기화
        playerPieceRuntimeDataList.Clear();

        // <변경부분> 새 런 시작 시 보유 아이템 초기화
        battleItemDataList.Clear();

        // <변경부분> 새 런 시작 시 보유 유물 초기화
        battleRelicDataList.Clear();

        // <변경부분> 새 런 시작 시 금화도 초기화
        goldAmount = 0;

        // <변경부분>
        // [TEMP SHOP TEST]
        // 새 Run 시작 시에도 테스트 옵션이 켜져 있다면
        // 임시 테스트 Gold를 다시 50G로 설정한다.
        //
        // 일반 실행 및 배포 빌드에는 영향을 주지 않는다.
        ApplyTemporaryShopTestGold();


        // <변경부분>
        // 새 Run 시작 시 엔드 타이머도
        // 초기 사이클 / 첫 번째 눈금 상태로 되돌린다.
        remainingEndTimerCycles =
            Mathf.Max(
                1,
                initialEndTimerCycles
            );

        currentEndTimerTick =
            0;


        Debug.Log(
    $"런 상태 초기화 완료 / " +
    $"엔드 타이머 " +
    $"{remainingEndTimerCycles}, " +
    $"눈금 {currentEndTimerTick}/" +
    $"{EndTimerTicksPerCycle}"
);


        // <변경부분>
        // Run 초기화로 엔드 타이머 값이 변경되었으므로
        // 현재 표시 중인 타이머 UI도 즉시 초기 상태로 갱신한다.
        EndTimerChanged?.Invoke();

        // <변경부분>
        // 밤 중에 새 런을 시작했다면 낮 전환을 구독자에게 알린다.
        if (previousDayNightState != CurrentDayNightState)
        {
            // <변경부분>
            // 구독자가 없어도 Console에서 시간대 전환을 확인할 수 있다.
            Debug.Log(
                $"[DayNight] 시간대 전환: " +
                $"{previousDayNightState} → {CurrentDayNightState} / " +
                $"눈금 {currentEndTimerTick}/{EndTimerTicksPerCycle}",
                this
            );

            DayNightStateChanged?.Invoke(CurrentDayNightState);
        }
    }
}
