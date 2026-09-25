using System.Collections.Generic;
using UnityEngine;

// <변경부분> 로그라이크 런 전체 상태를 씬 이동 사이에서 유지하는 매니저
// 현재 1차 구현에서는 플레이어 기물 상태만 저장한다.
public class RunStateManager : MonoBehaviour
{
    public static RunStateManager Instance { get; private set; }


    // <변경부분>
    // 엔드 타이머의 실제 값이 변경되었을 때
    // Battle / WorldMap UI에 갱신을 알리는 이벤트.
    //
    // UI는 타이머 데이터를 직접 저장하지 않고
    // 이 이벤트를 받은 뒤 RunStateManager의 현재 값을 다시 읽는다.
    public event System.Action EndTimerChanged;


    [Header("Player Runtime Pieces")]
    // <변경부분> 현재 런에서 유지되는 플레이어 기물 상태 목록
    [SerializeField]
    private List<PlayerPieceRuntimeData> playerPieceRuntimeDataList =
        new List<PlayerPieceRuntimeData>();

    [Header("Currency")]
    // <변경부분> 현재 런에서 보유 중인 금화
    [SerializeField] private int goldAmount = 0;

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


        Debug.Log(
     $"런 엔드 타이머 진행: " +
     $"남은 사이클 {remainingEndTimerCycles}, " +
     $"현재 눈금 {currentEndTimerTick}/" +
     $"{EndTimerTicksPerCycle}"
 );


        // <변경부분>
        // 실제 엔드 타이머 값이 변경되었으므로
        // 현재 Scene의 타이머 UI에 즉시 갱신을 알린다.
        EndTimerChanged?.Invoke();


        return true;
    }


    private void Awake()
    {
        // <변경부분> 씬 이동 중에도 하나의 RunStateManager만 유지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        // <변경부분> 새 런 시작 시 저장된 플레이어 기물 상태 초기화
        playerPieceRuntimeDataList.Clear();

        // <변경부분> 새 런 시작 시 보유 아이템 초기화
        battleItemDataList.Clear();

        // <변경부분> 새 런 시작 시 보유 유물 초기화
        battleRelicDataList.Clear();

        // <변경부분> 새 런 시작 시 금화도 초기화
        goldAmount = 0;


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
    }
}
