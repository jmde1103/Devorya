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
    // <변경부분> 전투 승리 후 생성할 보상 데이터
    [SerializeField] private BattleRewardData battleRewardData;

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

    // <변경부분> 현재 전투 승리 후 생성된 보상 후보 목록
    private readonly List<BattleRewardOptionRuntimeData> pendingRewardOptions =
        new List<BattleRewardOptionRuntimeData>();

    // <변경부분> 마지막 전투 결과 저장
    private BattleResult lastBattleResult = BattleResult.None;

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

        // <변경부분> 흡수 횟수 기반 데보리아 기물 회복 보상 적용
        ApplyDevoryaRecoveryReward();

        // <변경부분> 전투 승리 보상 후보 생성
        CreatePendingRewardOptions();

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
            }
        }

        Debug.Log($"데보리아 회복 보상 완료: 흡수 {lastPlayerAbsorbCount}개 / 생성 {createdCount}개");
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

    // <변경부분> BattleRewardData를 기준으로 현재 전투의 보상 후보를 생성하는 함수
    private void CreatePendingRewardOptions()
    {
        pendingRewardOptions.Clear();

        if (battleRewardData == null)
        {
            Debug.LogWarning("보상 후보 생성 실패: BattleRewardData가 연결되지 않았습니다.");
            return;
        }

        List<BattleRewardOptionRuntimeData> createdOptions =
            battleRewardData.CreateRewardOptions();

        if (createdOptions == null || createdOptions.Count == 0)
        {
            Debug.LogWarning($"보상 후보 생성 실패: {battleRewardData.rewardName}에서 생성된 보상이 없습니다.");
            return;
        }

        for (int i = 0; i < createdOptions.Count; i++)
        {
            BattleRewardOptionRuntimeData rewardOption = createdOptions[i];

            if (rewardOption == null)
            {
                continue;
            }

            // <변경부분> 금화는 선택지가 아니라 확정 보상으로 즉시 획득 처리
            if (rewardOption.rewardType == BattleRewardOptionType.Gold)
            {
                ApplyGoldReward(rewardOption);
                continue;
            }

            pendingRewardOptions.Add(rewardOption);

            Debug.Log($"보상 후보 생성: {i} / {rewardOption.GetDebugName()}");
        }
    }

    // <변경부분> 금화 보상을 RunStateManager에 즉시 적립하는 함수
    private void ApplyGoldReward(BattleRewardOptionRuntimeData rewardOption)
    {
        if (rewardOption == null)
        {
            return;
        }

        if (rewardOption.goldAmount <= 0)
        {
            return;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning("금화 보상 적용 실패: RunStateManager가 없습니다.");
            return;
        }

        RunStateManager.Instance.AddGold(rewardOption.goldAmount);

        Debug.Log($"금화 보상 적용 완료: {rewardOption.goldAmount}");
    }

    // <변경부분> 나중에 보상 UI가 현재 보상 후보 목록을 읽을 때 사용할 함수
    public List<BattleRewardOptionRuntimeData> GetPendingRewardOptionsCopy()
    {
        List<BattleRewardOptionRuntimeData> copiedList =
            new List<BattleRewardOptionRuntimeData>();

        for (int i = 0; i < pendingRewardOptions.Count; i++)
        {
            if (pendingRewardOptions[i] == null)
            {
                continue;
            }

            copiedList.Add(pendingRewardOptions[i]);
        }

        return copiedList;
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
