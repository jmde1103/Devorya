using System.Collections.Generic;
using UnityEngine;

// <변경부분> 전투 중 보유하는 유물 슬롯과 중복 획득 방지를 관리하는 매니저
public class BattleRelicManager : MonoBehaviour
{
    // 전투 유물 슬롯 최대 개수
    private const int MaxRelicSlotCount = 10;

    // 현재 전투에서 보유 중인 유물 슬롯
    private BattleRelicData[] relicSlots = new BattleRelicData[MaxRelicSlotCount];

    // 유물 슬롯 UI 갱신을 요청할 UI 컨트롤러
    private BattleUIController battleUIController;

    [Header("Relic Database")]
    // <변경부분> BattleRelicType으로 BattleRelicData를 찾는 유물 데이터베이스
    [SerializeField] private BattleRelicDatabase battleRelicDatabase;

    [Header("Test Relic")]
    // <변경부분> 테스트용으로 전투 시작 시 지급할 유물 타입
    [SerializeField] private BattleRelicType testRelicType = BattleRelicType.AbsorbChanceAttackOncePerTurn;

    // 게임 시작 시 테스트 유물을 지급할지 여부
    [SerializeField] private bool addTestStartRelic = false;

    // <변경부분> BattleManager에서 전투 시작 시 유물 매니저를 초기화하는 함수
    public void Initialize(BattleUIController uiController)
    {
        battleUIController = uiController;

        // <변경부분> 이전 전투에서 RunStateManager에 저장한
        // 유물을 현재 전투 슬롯에 복원
        RestoreRelicsFromRunState();

        RefreshRelicSlotUI();

        if (addTestStartRelic &&
            testRelicType != BattleRelicType.None)
        {
            AddBattleRelicByType(testRelicType);
        }
    }

    // <변경부분> RunStateManager에 저장된 유물을
    // 현재 전투 슬롯로 복원
    private void RestoreRelicsFromRunState()
    {
        relicSlots = new BattleRelicData[MaxRelicSlotCount];

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "유물 복원 생략: RunStateManager가 없습니다."
            );

            return;
        }

        List<BattleRelicData> savedRelics =
            RunStateManager.Instance.GetBattleRelicsCopy();

        int restoreCount =
            Mathf.Min(savedRelics.Count, MaxRelicSlotCount);

        for (int i = 0; i < restoreCount; i++)
        {
            relicSlots[i] = savedRelics[i];
        }

        Debug.Log($"런 유물 복원 완료: {restoreCount}개");
    }

    // <변경부분> 유물 타입을 받아 BattleRelicDatabase에서
    // BattleRelicData를 찾은 뒤 실제 유물 추가 함수로 전달한다.
    public bool AddBattleRelicByType(BattleRelicType relicType)
    {
        // None은 실제 유물 타입이 아니므로 추가하지 않는다.
        if (relicType == BattleRelicType.None)
        {
            Debug.LogWarning("추가할 유물 타입이 None입니다.");
            return false;
        }

        // 유물 데이터베이스가 연결되지 않았다면
        // 타입으로 BattleRelicData를 찾을 수 없다.
        if (battleRelicDatabase == null)
        {
            Debug.LogWarning(
                "BattleRelicDatabase가 연결되지 않아 유물을 추가할 수 없습니다."
            );

            return false;
        }

        // 전달받은 타입과 일치하는 유물 데이터를 데이터베이스에서 찾는다.
        BattleRelicData relicData =
            battleRelicDatabase.GetData(relicType);

        if (relicData == null)
        {
            Debug.LogWarning(
                $"BattleRelicDatabase에서 유물 데이터를 찾을 수 없습니다: {relicType}"
            );

            return false;
        }

        // 찾은 유물 데이터를 실제 전투 및 런 상태에 추가한다.
        return AddBattleRelic(relicData);
    }

    // <변경부분> 전투 유물을 왼쪽 빈 슬롯부터 추가하고 RunStateManager에도 저장
    // 전투 슬롯에 실제 빈칸이 있는지 먼저 확인한 뒤 런 상태와 슬롯을 함께 갱신한다.
    public bool AddBattleRelic(BattleRelicData relicData)
    {
        // 추가할 유물 데이터가 없거나 None 타입이면 획득 처리하지 않는다.
        if (relicData == null ||
            relicData.relicType == BattleRelicType.None)
        {
            Debug.LogWarning("추가할 유물 데이터가 없습니다.");
            return false;
        }

        // 현재 전투 슬롯에 같은 타입의 유물이 있으면 중복 획득을 막는다.
        if (HasRelic(relicData.relicType))
        {
            Debug.Log(
                $"유물 획득 실패: 이미 보유 중인 유물입니다. / " +
                $"{relicData.relicName}"
            );

            return false;
        }

        // 런 상태 매니저가 없으면 씬 이동 후 유물을 유지할 수 없으므로 획득하지 않는다.
        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "유물 획득 실패: RunStateManager가 없습니다."
            );

            return false;
        }

        // <변경부분> 전투 유물 슬롯에서 실제로 사용할 빈 슬롯 위치를 먼저 찾는다.
        int emptySlotIndex = -1;

        for (int i = 0; i < relicSlots.Length; i++)
        {
            bool isEmptySlot =
                relicSlots[i] == null ||
                relicSlots[i].relicType == BattleRelicType.None;

            if (isEmptySlot)
            {
                emptySlotIndex = i;
                break;
            }
        }

        // 전투 슬롯이 가득 찼다면 런 상태에도 유물을 추가하지 않는다.
        if (emptySlotIndex < 0)
        {
            Debug.Log("유물 슬롯이 가득 찼습니다.");
            return false;
        }

        // <변경부분> 런 상태에 유물을 먼저 저장한다.
        // RunStateManager가 최대 개수와 중복 여부를 최종적으로 다시 검사한다.
        bool addedToRunState =
            RunStateManager.Instance.TryAddBattleRelic(
                relicData,
                MaxRelicSlotCount
            );

        if (addedToRunState == false)
        {
            return false;
        }

        // <변경부분> 런 상태 저장에 성공한 경우에만 전투 슬롯에도 같은 유물을 추가한다.
        relicSlots[emptySlotIndex] = relicData;

        // 유물 획득 결과를 UI에 즉시 반영한다.
        RefreshRelicSlotUI();

        Debug.Log(
            $"유물 획득: {relicData.relicName} / " +
            $"슬롯 {emptySlotIndex}"
        );

        return true;
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

    // <변경부분> 현재 보유 중인 유물 데이터를 타입으로 찾아 반환하는 함수
    public BattleRelicData GetRelicData(BattleRelicType relicType)
    {
        if (relicType == BattleRelicType.None)
        {
            return null;
        }

        for (int i = 0; i < relicSlots.Length; i++)
        {
            BattleRelicData relicData = relicSlots[i];

            if (relicData == null)
            {
                continue;
            }

            if (relicData.relicType == relicType)
            {
                return relicData;
            }
        }

        return null;
    }


    public void AddTestRelicForDebug()
    {
        // <변경부분> 테스트 유물 타입이 없으면 추가 불가
        if (testRelicType == BattleRelicType.None)
        {
            Debug.LogWarning("테스트 유물 타입이 설정되지 않았습니다.");
            return;
        }

        // <변경부분> 테스트 유물 타입으로 Database에서 데이터를 찾아 슬롯에 추가
        AddBattleRelicByType(testRelicType);
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
}
