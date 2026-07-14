using System.Collections.Generic; // <변경부분> 보상 후보 목록 저장에 사용
using UnityEngine;
using UnityEngine.SceneManagement;

// <변경부분> 전투 종료 후 보상 정산, 맵 복귀, 패배 처리 흐름을 담당하는 컨트롤러
public class BattleEndFlowController : MonoBehaviour
{
    [Header("Scene Move")]
    // <변경부분> 승리 후 바로 맵 씬으로 이동할지 여부
    // 나중에 보상 UI가 생기면 false로 두고, 보상 선택 완료 후 MoveToMapScene()을 호출하면 된다.
    [SerializeField] private bool moveToMapSceneImmediatelyOnWin = false;

    // <변경부분> 전투 승리 후 돌아갈 로그라이크 맵 씬 이름
    [SerializeField] private string mapSceneName = "RoguelikeMapScene";

    // <변경부분> 패배 후 바로 이동할 씬이 필요한 경우 사용
    [SerializeField] private bool moveToMapSceneOnLose = false;

    [Header("Reward")]
    // <변경부분> 아이템 보상으로 보유할 수 있는 최대 아이템 수
    private const int MaxBattleItemCount = 4;

    // <변경부분> 유물 보상으로 보유할 수 있는 최대 유물 수
    private const int MaxBattleRelicCount = 10;

    // <변경부분> 현재 전투 스테이지에서 전달받은 보상 데이터
    // BattleSetupManager가 StageBattleData의 battleRewardData를 전투 시작 시 전달한다.
    private BattleRewardData battleRewardData;

    [Header("Devorya Recovery Reward")]
    // <변경부분> 플레이어가 보유할 수 있는 최대 기물 수
    [SerializeField] private int maxPlayerPieceCount = 10;

    // <변경부분> 흡수 보상으로 Pawn이 생성될 확률
    [SerializeField] private int devoryaPawnRewardPercent = 90;

    // <변경부분> 흡수 보상 90% 후보
    [SerializeField] private PieceData devoryaPawnData;

    // <변경부분> 흡수 보상 10% 후보
    [SerializeField] private PieceData devoryaKnightData;
    [SerializeField] private PieceData devoryaRookData;
    [SerializeField] private PieceData devoryaBishopData;

    // <변경부분> 마지막 전투에서 플레이어가 흡수한 적 기물 수
    private int lastPlayerAbsorbCount = 0;

    // <변경부분> 이번 전투에서 실제 획득에 성공한 아이템·유물 보상 목록
    // 추후 보상 결과 UI가 이 목록을 읽어서 획득 결과를 표시한다.
    private readonly List<BattleRewardOptionRuntimeData> acquiredRewardOptions =
        new List<BattleRewardOptionRuntimeData>();

    // <변경부분> 이번 전투에서 실제 복구에 성공한 기물을
    // PieceData 종류별 수량으로 집계하는 목록
    private readonly List<BattleRecoveryRewardRuntimeData> acquiredRecoveryRewards =
        new List<BattleRecoveryRewardRuntimeData>();

    // <변경부분> 이번 전투에서 실제 획득한 금화량
    private int lastAcquiredGoldAmount = 0;

    // <변경부분> 마지막 전투 결과 저장
    private BattleResult lastBattleResult = BattleResult.None;

    // <변경부분> 현재 StageBattleData가 사용하는 전투 보상 데이터를 전달받는 함수
    // BattleSetupManager가 전투 시작 시 한 번 호출한다.
    public void SetBattleRewardData(BattleRewardData rewardData)
    {
        battleRewardData = rewardData;

        if (battleRewardData == null)
        {
            Debug.LogWarning("전투 보상 데이터 적용 경고: 현재 StageBattleData에 BattleRewardData가 없습니다.");
            return;
        }

        Debug.Log($"전투 보상 데이터 적용: {battleRewardData.rewardName}");
    }

    // <변경부분> BattleManager가 전투 종료 시 호출하는 함수
    public void HandleBattleEnd(BattleResult result, int playerAbsorbCount)
    {
        lastBattleResult = result;
        lastPlayerAbsorbCount = Mathf.Max(0, playerAbsorbCount);

        if (result == BattleResult.Win)
        {
            HandleBattleWin();
            return;
        }

        if (result == BattleResult.Lose)
        {
            HandleBattleLose();
            return;
        }
    }

    // <변경부분> 기존 호출 호환용 함수
    public void HandleBattleEnd(BattleResult result)
    {
        HandleBattleEnd(result, 0);
    }

    // <변경부분> 전투 승리 후 처리
    private void HandleBattleWin()
    {
        Debug.Log("전투 종료 흐름: 승리 / 보상 정산 단계 진입");

        // <변경부분> 이전 전투에서 사용한 보상 결과 기록 초기화
        acquiredRecoveryRewards.Clear();
        acquiredRewardOptions.Clear();
        lastAcquiredGoldAmount = 0;

        // <변경부분> 흡수 횟수 기반 데보리아 기물 회복 보상 적용
        ApplyDevoryaRecoveryReward();

        // <변경부분> 확률 판정을 통과한 모든 전투 보상을
        // 즉시 런 상태에 적용
        CreateAndApplyBattleRewards();

        // <변경부분> 1차 테스트용 즉시 맵 복귀
        // 추후 보상 UI가 생기면 여기서는 보상 UI를 띄우고, 보상 선택 완료 후 MoveToMapScene() 호출
        if (moveToMapSceneImmediatelyOnWin)
        {
            MoveToMapScene();
        }
    }

    // <변경부분> 흡수한 적 기물 수의 절반만큼 데보리아 기물을 회복하는 함수
    private void ApplyDevoryaRecoveryReward()
    {
        int rewardCount = lastPlayerAbsorbCount / 2;

        if (rewardCount <= 0)
        {
            Debug.Log($"데보리아 회복 보상 없음: 흡수 수 {lastPlayerAbsorbCount}");
            return;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning("데보리아 회복 보상 실패: RunStateManager가 없습니다.");
            return;
        }

        int createdCount = 0;

        for (int i = 0; i < rewardCount; i++)
        {
            if (RunStateManager.Instance.GetPlayerPieceCount() >= maxPlayerPieceCount)
            {
                Debug.Log($"데보리아 회복 보상 중단: 최대 기물 수 도달 {maxPlayerPieceCount}");
                break;
            }

            PieceData selectedPieceData = RollDevoryaRecoveryPieceData();

            if (selectedPieceData == null)
            {
                Debug.LogWarning("데보리아 회복 보상 실패: 생성할 PieceData가 없습니다.");
                continue;
            }

            PlayerPieceRuntimeData runtimeData =
                PlayerPieceRuntimeData.CreateFromPieceData(selectedPieceData, false);

            if (runtimeData == null)
            {
                continue;
            }

            bool added = RunStateManager.Instance.TryAddPlayerPiece(
                runtimeData,
                maxPlayerPieceCount
            );

            if (added)
            {
                createdCount++;

                // <변경부분> 실제 복구에 성공한 기물을 종류별로 집계
                AddOrIncreaseRecoveryReward(selectedPieceData);
            }
        }

        Debug.Log($"데보리아 회복 보상 완료: 흡수 {lastPlayerAbsorbCount}개 / 생성 {createdCount}개");
    }

    // <변경부분> 같은 PieceData가 이미 집계되어 있으면 수량을 증가시키고,
    // 처음 복구된 종류라면 새로운 결과 항목으로 추가한다.
    private void AddOrIncreaseRecoveryReward(PieceData pieceData)
    {
        if (pieceData == null)
        {
            return;
        }

        for (int i = 0; i < acquiredRecoveryRewards.Count; i++)
        {
            BattleRecoveryRewardRuntimeData recoveryReward =
                acquiredRecoveryRewards[i];

            if (recoveryReward == null)
            {
                continue;
            }

            if (recoveryReward.pieceData == pieceData)
            {
                recoveryReward.amount++;
                return;
            }
        }

        acquiredRecoveryRewards.Add(
            new BattleRecoveryRewardRuntimeData(
                pieceData,
                1
            )
        );
    }

    // <변경부분> 데보리아 회복 보상으로 생성할 PieceData를 확률에 따라 선택하는 함수
    private PieceData RollDevoryaRecoveryPieceData()
    {
        int pawnPercent = Mathf.Clamp(devoryaPawnRewardPercent, 0, 100);
        int roll = Random.Range(0, 100);

        if (roll < pawnPercent)
        {
            return devoryaPawnData;
        }

        List<PieceData> advancedCandidates = new List<PieceData>();

        if (devoryaKnightData != null)
        {
            advancedCandidates.Add(devoryaKnightData);
        }

        if (devoryaRookData != null)
        {
            advancedCandidates.Add(devoryaRookData);
        }

        if (devoryaBishopData != null)
        {
            advancedCandidates.Add(devoryaBishopData);
        }

        if (advancedCandidates.Count == 0)
        {
            return devoryaPawnData;
        }

        int randomIndex = Random.Range(0, advancedCandidates.Count);

        return advancedCandidates[randomIndex];
    }

    // <변경부분> BattleRewardData의 개별 드롭 확률을 판정한 뒤
    // 생성된 금화·아이템·유물 보상을 모두 즉시 런 상태에 적용하는 함수
    private void CreateAndApplyBattleRewards()
    {
        acquiredRewardOptions.Clear();

        if (battleRewardData == null)
        {
            Debug.LogWarning(
                "전투 보상 적용 실패: BattleRewardData가 연결되지 않았습니다."
            );

            return;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "전투 보상 적용 실패: RunStateManager가 없습니다."
            );

            return;
        }

        List<BattleRewardOptionRuntimeData> createdOptions =
            battleRewardData.CreateRewardOptions();

        if (createdOptions == null || createdOptions.Count == 0)
        {
            Debug.Log(
                $"전투 보상 없음: {battleRewardData.rewardName}에서 " +
                "드롭에 성공한 보상이 없습니다."
            );

            return;
        }

        for (int i = 0; i < createdOptions.Count; i++)
        {
            BattleRewardOptionRuntimeData rewardOption =
                createdOptions[i];

            if (rewardOption == null)
            {
                continue;
            }

            // <변경부분> 금화는 기존과 동일하게 즉시 런 상태에 적립한다.
            if (rewardOption.rewardType ==
                BattleRewardOptionType.Gold)
            {
                ApplyGoldReward(rewardOption);
                continue;
            }

            bool rewardApplied =
                TryApplyDroppedReward(rewardOption);

            if (rewardApplied == false)
            {
                Debug.LogWarning(
                    $"드롭 보상 획득 실패: " +
                    $"{rewardOption.GetDebugName()}"
                );

                continue;
            }

            // <변경부분> 실제 저장에 성공한 아이템·유물만
            // 결과 UI 표시 목록에 보관한다.
            acquiredRewardOptions.Add(rewardOption);

            Debug.Log(
                $"드롭 보상 획득 완료: " +
                $"{rewardOption.GetDebugName()}"
            );
        }
    }

    // <변경부분> 드롭된 아이템 또는 유물을
    // RunStateManager에 즉시 저장하는 함수
    private bool TryApplyDroppedReward(
        BattleRewardOptionRuntimeData rewardOption)
    {
        if (rewardOption == null ||
            RunStateManager.Instance == null)
        {
            return false;
        }

        if (rewardOption.rewardType ==
            BattleRewardOptionType.Item)
        {
            return RunStateManager.Instance.TryAddBattleItem(
                rewardOption.itemData,
                MaxBattleItemCount
            );
        }

        if (rewardOption.rewardType ==
            BattleRewardOptionType.Relic)
        {
            return RunStateManager.Instance.TryAddBattleRelic(
                rewardOption.relicData,
                MaxBattleRelicCount
            );
        }

        return false;
    }

    // <변경부분> 금화 보상을 RunStateManager에 즉시 적립하는 함수
    private void ApplyGoldReward(
        BattleRewardOptionRuntimeData rewardOption)
    {
        if (rewardOption == null ||
            rewardOption.goldAmount <= 0)
        {
            return;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "금화 보상 적용 실패: RunStateManager가 없습니다."
            );

            return;
        }

        RunStateManager.Instance.AddGold(
            rewardOption.goldAmount
        );

        Debug.Log(
            $"금화 보상 적용 완료: " +
            $"{rewardOption.goldAmount}"
        );
    }

    // <변경부분> 이번 전투에서 실제 복구된 기물 목록을
    // 외부 UI가 안전하게 읽을 수 있도록 복사본으로 반환
    public List<BattleRecoveryRewardRuntimeData>
        GetAcquiredRecoveryRewardsCopy()
    {
        List<BattleRecoveryRewardRuntimeData> copiedRewards =
            new List<BattleRecoveryRewardRuntimeData>();

        for (int i = 0; i < acquiredRecoveryRewards.Count; i++)
        {
            BattleRecoveryRewardRuntimeData reward =
                acquiredRecoveryRewards[i];

            if (reward == null)
            {
                continue;
            }

            copiedRewards.Add(
                reward.Clone()
            );
        }

        return copiedRewards;
    }

    // <변경부분> 추후 보상 결과 UI가 이번 전투에서 실제 획득한
    // 아이템·유물 목록을 읽을 때 사용하는 함수
    public List<BattleRewardOptionRuntimeData>
        GetAcquiredRewardOptionsCopy()
    {
        return new List<BattleRewardOptionRuntimeData>(
            acquiredRewardOptions
        );
    }

    // <변경부분> 전투 패배 후 처리
    private void HandleBattleLose()
    {
        Debug.Log("전투 종료 흐름: 패배 / 런 실패 또는 패배 처리 단계 진입 준비");

        if (moveToMapSceneOnLose)
        {
            MoveToMapScene();
        }
    }

    // <변경부분> 보상 선택 완료 후 로그라이크 맵 씬으로 이동할 때 호출할 함수
    public void MoveToMapScene()
    {
        if (string.IsNullOrEmpty(mapSceneName))
        {
            Debug.LogWarning("맵 씬 이동 실패: mapSceneName이 비어 있습니다.");
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }

    // <변경부분> 마지막 전투 결과 반환
    public BattleResult GetLastBattleResult()
    {
        return lastBattleResult;
    }
}

// <변경부분> 전투 종료 시 실제 복구된 기물 종류와 수량을
// 보상 결과 UI에 전달하기 위한 런타임 데이터
[System.Serializable]
public class BattleRecoveryRewardRuntimeData
{
    // <변경부분> 실제 복구된 기물 데이터
    public PieceData pieceData;

    // <변경부분> 같은 종류의 기물이 복구된 수량
    public int amount;

    // <변경부분> 복구 기물 결과 데이터 생성
    public BattleRecoveryRewardRuntimeData(
        PieceData pieceData,
        int amount)
    {
        this.pieceData = pieceData;
        this.amount = Mathf.Max(0, amount);
    }

    // <변경부분> 외부에서 원본 결과를 직접 수정하지 못하도록 복사본 생성
    public BattleRecoveryRewardRuntimeData Clone()
    {
        return new BattleRecoveryRewardRuntimeData(
            pieceData,
            amount
        );
    }
}
