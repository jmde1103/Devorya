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
        // 유물 슬롯 UI 갱신을 요청할 BattleUIController 저장
        battleUIController = uiController;

        // 게임 시작 시 유물 슬롯 UI 초기화
        RefreshRelicSlotUI();

        // <변경부분> 테스트용 유물 타입이 설정되어 있으면 Database에서 찾아 전투 시작 시 1개 지급
        if (addTestStartRelic && testRelicType != BattleRelicType.None)
        {
            AddBattleRelicByType(testRelicType);
        }
    }

    // <변경부분> 유물 타입을 받아 Database에서 BattleRelicData를 찾은 뒤 슬롯에 추가하는 함수
    public bool AddBattleRelicByType(BattleRelicType relicType)
    {
        if (relicType == BattleRelicType.None)
        {
            Debug.LogWarning("추가할 유물 타입이 None입니다.");
            return false;
        }

        if (battleRelicDatabase == null)
        {
            Debug.LogWarning("BattleRelicDatabase가 연결되지 않아 유물을 추가할 수 없습니다.");
            return false;
        }

        BattleRelicData relicData = battleRelicDatabase.GetData(relicType);

        if (relicData == null)
        {
            Debug.LogWarning($"BattleRelicDatabase에서 유물 데이터를 찾을 수 없습니다: {relicType}");
            return false;
        }

        return AddBattleRelic(relicData);
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

        // 같은 유물은 중복 획득할 수 없음
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
