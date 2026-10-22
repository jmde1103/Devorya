using System.Collections.Generic; // <변경부분> 보상 후보 목록 저장에 사용
using UnityEngine;
using UnityEngine.SceneManagement;

// <변경부분> 전투 종료 후 보상 정산, 맵 복귀, 패배 처리 흐름을 담당하는 컨트롤러
public class BattleEndFlowController : MonoBehaviour
{
    [Header("Scene Move")]

    // 승리 후 보상 팝업을 생략하고
    // 바로 WorldMap Scene으로 복귀할지 여부.
    //
    // false:
    // 보상 결과 Popup 표시
    // → 확인
    // → MoveToMapScene()
    //
    // true:
    // 보상 적용 직후 바로 MoveToMapScene()
    [SerializeField]
    private bool moveToMapSceneImmediatelyOnWin =
     false;

    // 전투 승리 후 돌아갈 로그라이크 WorldMap Scene 이름.
    //
    // 현재 정식 전투 흐름은
    // Battle → Reward → WorldMap 구조이므로
    // BattleScene에서 다음 BattleScene으로 직접 이동하지 않는다.
    [SerializeField]
    private string mapSceneName =
    "WorldMapScene";

    // <변경부분> 현재 StageBattleData에서 전달받은
    // 실제 승리 후 이동 목적지 Scene.
    //
    // 일반 전투는 WorldMapScene,
    // 최종 보스는 Title Scene 등을 사용할 수 있다.
    //
    // 빈 값이면 기존 mapSceneName을 fallback으로 사용한다.
    private string victorySceneName;

    // <변경부분> 현재 StageBattleData에서 전달받은
    // 승리 후 실행할 TextCutsceneData.
    //
    // null이면 일반 Scene 이동으로 처리한다.
    private TextCutsceneData victoryCutsceneData;
    private WorldMapEventData victoryWorldMapEventData;
    public void SetVictoryWorldMapEventData(WorldMapEventData data) { victoryWorldMapEventData = data; }



    // <변경부분>
    // 이전 BattleEndFlowController Inspector에 저장되어 있던
    // End Stage 설정값은 Serialized 호환을 위해 당분간 보존한다.
    //
    // 이제 실제 런타임에서는 사용하지 않으며,
    // End Stage 설정의 SSOT는 RunStateManager이다.
    //
    // 새 구조의 Runtime 검증이 끝난 뒤
    // 완전히 삭제할지 최종 정리한다.
    [SerializeField, HideInInspector]
    private StageBattleData endStageBattleData;


    // <변경부분>
    // 이전 BattleEndFlowController의 End Stage Scene 설정.
    //
    // 기존 Scene / Prefab Serialized 데이터를 즉시 파괴하지 않기 위해
    // 당분간 숨긴 상태로 유지하며 Runtime에서는 사용하지 않는다.
    [SerializeField, HideInInspector]
    private string endStageBattleSceneName =
        "BattleScene";


    [Header("Reward")]
    // <변경부분> 아이템 보상으로 보유할 수 있는 최대 아이템 수
    private const int MaxBattleItemCount = 4;

    // <변경부분> 유물 보상으로 보유할 수 있는 최대 유물 수
    private const int MaxBattleRelicCount = 10;

    // <변경부분> 현재 전투 스테이지에서 전달받은 보상 데이터
    // BattleSetupManager가 StageBattleData의 battleRewardData를 전투 시작 시 전달한다.
    private BattleRewardData battleRewardData;


    [Header("Reward Popup")]
    // <변경부분> 전투 승리 후 획득 결과를 표시할 보상 팝업
    [SerializeField]
    private BattleRewardPopupUI battleRewardPopupUI;

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

    // <변경부분>
    // 전투 승리 후 생성됐지만 아직 수령하지 않은 보상 후보 목록.
    //
    // 금화·아이템·유물은 슬롯을 클릭하기 전까지
    // RunStateManager에 지급하지 않는다.
    //
    // 성공적으로 수령한 후보는 이 목록에서 제거하므로
    // 중복 클릭으로 같은 보상을 두 번 지급받을 수 없다.
    private readonly List<BattleRewardOptionRuntimeData> pendingRewardOptions =
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

    // <변경부분> 현재 StageBattleData에 지정된
    // 승리 후 이동 Scene 이름을 전달받는다.
    //
    // Scene 이름이 비어 있으면 기존 WorldMap 이동 구조를
    // 그대로 유지하기 위해 mapSceneName을 사용한다.
    public void SetVictorySceneName(
        string sceneName)
    {
        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            victorySceneName =
                mapSceneName;

            Debug.LogWarning(
                "승리 후 이동 Scene이 비어 있어 " +
                $"기본 Scene을 사용합니다. / {mapSceneName}"
            );

            return;
        }

        victorySceneName =
            sceneName;

        Debug.Log(
            $"승리 후 이동 Scene 적용: " +
            $"{victorySceneName}"
        );
    }

    // <변경부분> 현재 StageBattleData에 지정된
    // 승리 후 TextCutsceneData를 전달받는다.
    //
    // 실제 Scene 이동은 victorySceneName을 사용하며,
    // 이 데이터는 Scene Load 직전에
    // TextCutsceneRuntimeState에 Pending Data로 등록한다.
    public void SetVictoryCutsceneData(
        TextCutsceneData cutsceneData)
    {
        victoryCutsceneData =
            cutsceneData;

        if (victoryCutsceneData == null)
        {
            Debug.Log(
                "승리 후 컷씬 데이터 없음: " +
                "일반 Scene 이동을 사용합니다."
            );

            return;
        }

        Debug.Log(
            $"승리 후 컷씬 데이터 적용: " +
            $"{victoryCutsceneData.name}"
        );
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

    // <변경부분>
    // 전투 승리 시 기물 복구는 기존대로 즉시 지급한다.
    //
    // 금화·아이템·유물은 후보만 생성하고,
    // 플레이어가 보상 슬롯을 클릭했을 때 지급한다.
    private void HandleBattleWin()
    {
        Debug.Log("전투 종료 흐름: 승리 / 보상 정산 단계 진입");

        // 이전 전투의 보상 상태를 초기화한다.
        acquiredRecoveryRewards.Clear();
        acquiredRewardOptions.Clear();
        pendingRewardOptions.Clear();
        lastAcquiredGoldAmount = 0;

        // 기존 기물 복구 정산은 그대로 유지한다.
        ApplyDevoryaRecoveryReward();

        // 금화·아이템·유물은 아직 지급하지 않고
        // 드롭 후보만 확정한다.
        CreatePendingBattleRewards();

        // 기존 즉시 이동 테스트 옵션은 예외적으로
        // 종전처럼 가능한 보상을 자동 지급한다.
        // 일반적인 보상 선택 흐름에는 사용하지 않는다.
        if (moveToMapSceneImmediatelyOnWin)
        {
            ApplyAllPendingRewardsForImmediateMove();

            DiscardPendingBattleRewards();

            MoveToMapScene();
            return;
        }

        if (battleRewardPopupUI == null)
        {
            Debug.LogWarning(
                "전투 보상 팝업 표시 실패: " +
                "BattleEndFlowController의 Battle Reward Popup UI가 연결되지 않았습니다."
            );

            return;
        }

        // 팝업 표시용 후보 금화 수량 계산.
        // 이 값은 아직 실제 보유 금화에 추가되지 않았다.
        int pendingGoldAmount = 0;

        for (int i = 0; i < pendingRewardOptions.Count; i++)
        {
            BattleRewardOptionRuntimeData reward =
                pendingRewardOptions[i];

            if (reward != null &&
                reward.rewardType == BattleRewardOptionType.Gold)
            {
                pendingGoldAmount +=
                    Mathf.Max(0, reward.goldAmount);
            }
        }

        // 기물 복구 목록은 지급 완료 결과이며,
        // 금화·아이템·유물 목록은 미수령 후보이다.
        //
        // 기존 흡수 정산 애니메이션에 필요한
        // lastPlayerAbsorbCount는 그대로 전달한다.
        battleRewardPopupUI.Show(
            this,
            GetAcquiredRecoveryRewardsCopy(),
            pendingGoldAmount,
            GetPendingBattleRewardOptionsCopy(),
            lastPlayerAbsorbCount
        );

        Debug.Log(
            $"전투 보상 선택 팝업 Show 호출 완료: " +
            $"복구 {acquiredRecoveryRewards.Count}종 / " +
            $"금화 후보 {pendingGoldAmount} / " +
            $"전체 미수령 후보 {pendingRewardOptions.Count}개"
        );
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

    // <변경부분>
    // BattleRewardData의 드롭 확률을 판정해
    // 금화·아이템·유물 후보만 확정한다.
    //
    // 이 단계에서는 실제 보상을 지급하지 않는다.
    private void CreatePendingBattleRewards()
    {
        pendingRewardOptions.Clear();

        if (battleRewardData == null)
        {
            Debug.LogWarning(
                "전투 보상 생성 실패: BattleRewardData가 연결되지 않았습니다."
            );

            return;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "전투 보상 생성 실패: RunStateManager가 없습니다."
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

            if (rewardOption.rewardType == BattleRewardOptionType.Gold &&
                rewardOption.goldAmount <= 0)
            {
                continue;
            }

            if (rewardOption.rewardType == BattleRewardOptionType.Item &&
                rewardOption.itemData == null)
            {
                continue;
            }

            if (rewardOption.rewardType == BattleRewardOptionType.Relic &&
                rewardOption.relicData == null)
            {
                continue;
            }

            if (rewardOption.rewardType != BattleRewardOptionType.Gold &&
                rewardOption.rewardType != BattleRewardOptionType.Item &&
                rewardOption.rewardType != BattleRewardOptionType.Relic)
            {
                continue;
            }

            pendingRewardOptions.Add(rewardOption);
        }

        Debug.Log(
            $"전투 보상 후보 생성 완료: " +
            $"{pendingRewardOptions.Count}개 / 아직 지급되지 않음"
        );
    }


    // <변경부분>
    // 보상 UI에서 슬롯을 클릭했을 때 호출한다.
    //
    // true:
    // 실제 지급에 성공했으며 미수령 목록에서 제거했다.
    //
    // false:
    // 지급하지 않았으며 후보를 유지한다.
    //
    // 미수령 목록에 없는 보상은 절대 지급하지 않는다.
    public bool TryClaimPendingBattleReward(
        BattleRewardOptionRuntimeData rewardOption)
    {
        if (rewardOption == null ||
            pendingRewardOptions.Contains(rewardOption) == false)
        {
            return false;
        }

        if (RunStateManager.Instance == null)
        {
            Debug.LogWarning(
                "보상 수령 실패: RunStateManager가 없습니다."
            );

            return false;
        }

        bool rewardApplied = false;

        if (rewardOption.rewardType ==
            BattleRewardOptionType.Gold)
        {
            if (rewardOption.goldAmount > 0)
            {
                ApplyGoldReward(rewardOption);
                rewardApplied = true;
            }
        }
        else
        {
            // 기존 아이템 4개 / 유물 10개 제한과
            // 유물 중복 검사 로직을 그대로 사용한다.
            rewardApplied =
                TryApplyDroppedReward(rewardOption);

            if (rewardApplied)
            {
                acquiredRewardOptions.Add(rewardOption);
            }
        }

        if (rewardApplied == false)
        {
            Debug.LogWarning(
                $"보상 수령 실패: {rewardOption.GetDebugName()} / " +
                "인벤토리 제한 또는 유효하지 않은 보상입니다."
            );

            return false;
        }

        // 성공적으로 지급된 보상을 후보에서 제거한다.
        // 이후 같은 참조로 재요청해도 지급되지 않는다.
        pendingRewardOptions.Remove(rewardOption);

        Debug.Log(
            $"보상 선택 수령 완료: " +
            $"{rewardOption.GetDebugName()} / " +
            $"남은 보상 {pendingRewardOptions.Count}개"
        );

        return true;
    }


    // <변경부분>
    // 팝업에서 사용할 미수령 후보 목록의 복사본.
    //
    // List는 복사하되 후보 객체 참조는 유지한다.
    // 클릭 시 원본 후보와 동일한 객체인지 검사할 수 있다.
    public List<BattleRewardOptionRuntimeData>
        GetPendingBattleRewardOptionsCopy()
    {
        return new List<BattleRewardOptionRuntimeData>(
            pendingRewardOptions
        );
    }


    // <변경부분>
    // 확인 / 건너뛰기 시 남은 후보를 지급하지 않고 폐기한다.
    //
    // 이미 수령한 보상은 RunStateManager에 남는다.
    public void DiscardPendingBattleRewards()
    {
        if (pendingRewardOptions.Count > 0)
        {
            Debug.Log(
                $"미수령 전투 보상 {pendingRewardOptions.Count}개를 " +
                "획득하지 않고 넘어갑니다."
            );
        }

        pendingRewardOptions.Clear();
    }


    // <변경부분>
    // 기존 즉시 맵 이동 테스트 옵션 전용 예외 처리.
    //
    // 보상창을 사용하지 않을 때만 과거처럼
    // 가능한 보상을 모두 자동 지급한다.
    private void ApplyAllPendingRewardsForImmediateMove()
    {
        List<BattleRewardOptionRuntimeData> candidates =
            GetPendingBattleRewardOptionsCopy();

        for (int i = 0; i < candidates.Count; i++)
        {
            TryClaimPendingBattleReward(candidates[i]);
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

    // <변경부분> 금화 보상을 RunStateManager에 즉시 적립하고
    // 이번 전투에서 획득한 총 금화량을 팝업 표시용으로 집계한다.
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

        // <변경부분> 보상 팝업에는 현재 총 보유량이 아니라
        // 이번 전투에서 실제 획득한 금화량을 표시한다.
        lastAcquiredGoldAmount +=
            rewardOption.goldAmount;

        Debug.Log(
            $"금화 보상 적용 완료: " +
            $"이번 획득 {rewardOption.goldAmount} / " +
            $"전투 총 획득 {lastAcquiredGoldAmount}"
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

    // <변경부분> 전투 패배 후 현재 런 데이터를 초기화하고
    // 현재 전투 씬을 다시 로드한다.
    // 추후 타이틀 씬이 완성되면 씬 이동 부분만 타이틀 씬으로 교체한다.
    private void HandleBattleLose()
    {
        Debug.Log(
            "전투 종료 흐름: 패배 / " +
            "런 데이터 초기화 후 현재 전투 씬 재시작"
        );

        // 패배한 런에서 저장된 기물, 금화, 아이템, 유물 데이터를 제거한다.
        if (RunStateManager.Instance != null)
        {
            RunStateManager.Instance.ClearRunState();
        }
        else
        {
            Debug.LogWarning(
                "패배 후 런 데이터 초기화 실패: " +
                "RunStateManager가 없습니다."
            );
        }

        // 현재 활성화된 전투 씬 정보를 가져온다.
        Scene currentScene =
            SceneManager.GetActiveScene();

        if (string.IsNullOrEmpty(currentScene.name))
        {
            Debug.LogWarning(
                "패배 후 전투 씬 재시작 실패: " +
                "현재 씬 이름을 확인할 수 없습니다."
            );

            return;
        }

        // 현재 전투 씬을 처음부터 다시 로드한다.
        SceneManager.LoadScene(
            currentScene.name
        );
    }

    // <변경부분>
    // Run 엔드 타이머가 만료된 일반 월드맵 Battle이라면
    // 기존 승리 목적지 대신 End Stage Battle로 전환한다.
    //
    // true:
    // End Stage Scene Load를 시작했음.
    //
    // false:
    // 일반 Battle 승리 흐름을 그대로 진행해야 함.
    private bool TryMoveToEndStageAfterTimerExpired()
    {
        // 승리했을 때만 엔드 스테이지 강제 진입을 적용한다.
        //
        // 패배는 기존 패배 처리 흐름을 그대로 사용한다.
        if (lastBattleResult !=
            BattleResult.Win)
        {
            return false;
        }


        // <변경부분>
        // End Timer 상태와 End Stage 설정의 SSOT는
        // RunStateManager 하나만 사용한다.
        RunStateManager runStateManager =
            RunStateManager.Instance;


        // RunStateManager가 없거나
        // 엔드 타이머가 아직 남아 있다면 일반 승리 흐름이다.
        if (runStateManager == null ||
            runStateManager.IsEndTimerExpired == false)
        {
            return false;
        }


        // 이미 BeginDirectBattle()을 통해 시작된 Battle이라면
        // 현재 전투가 End Stage이므로 다시 End Stage로 보내지 않는다.
        if (WorldMapRuntimeState.IsDirectBattle)
        {
            return false;
        }


        // <변경부분>
        // BattleEndFlowController가 별도로 End Stage 설정을
        // 소유하지 않고 RunStateManager의 공통 설정을 가져온다.
        StageBattleData targetEndStageBattleData =
            runStateManager.EndStageBattleData;

        string targetEndStageSceneName =
            runStateManager.EndStageBattleSceneName;


        // 여기까지 왔다면
        // "승리 + 엔드 타이머 만료"는 이미 확정된 상태다.
        //
        // End Stage 설정 오류 때문에 기존 WorldMap으로
        // 잘못 fallback하지 않도록 현재 이동 흐름을 중단한다.
        if (targetEndStageBattleData == null)
        {
            Debug.LogError(
                "엔드 스테이지 이동 실패: " +
                "RunStateManager의 " +
                "End Stage Battle Data가 연결되지 않았습니다."
            );

            return true;
        }


        if (string.IsNullOrWhiteSpace(
                targetEndStageSceneName))
        {
            Debug.LogError(
                "엔드 스테이지 이동 실패: " +
                "RunStateManager의 " +
                "End Stage Battle Scene Name이 비어 있습니다."
            );

            return true;
        }


        // 특정 WorldMap Node에 종속시키지 않고
        // End Stage의 StageBattleData만 다음 BattleScene에 전달한다.
        bool directBattleRegistered =
            WorldMapRuntimeState.BeginDirectBattle(
                targetEndStageBattleData
            );


        if (directBattleRegistered == false)
        {
            Debug.LogError(
                "엔드 스테이지 이동 실패: " +
                "End Stage Battle Data 등록에 실패했습니다."
            );

            return true;
        }


        Debug.Log(
            $"엔드 타이머 만료: " +
            $"현재 Battle 보상 정산 완료 후 " +
            $"End Stage로 이동합니다. / " +
            $"{targetEndStageBattleData.name} / " +
            $"{targetEndStageSceneName}"
        );


        WorldMapEventRuntimeState.Queue(victoryWorldMapEventData);
        SceneManager.LoadScene(
            targetEndStageSceneName
        );


        return true;
    }


    // <변경부분> 전투 보상 확인 후
    // 현재 StageBattleData에서 지정한 승리 목적지 Scene으로 이동한다.
    //
    // 기존 BattleRewardPopupUI 등의 호출 연결을 보호하기 위해
    // 함수 이름 MoveToMapScene()은 그대로 유지한다.
    //
    // WorldMap으로 돌아가는 일반 전투일 때만
    // 기존 WorldMap 노드 승리 기록을 적용한다.
    public void MoveToMapScene()
    {
        // <변경부분>
        // 엔드 타이머가 모두 소진된 일반 Battle이라면
        // 기존 WorldMap / Cutscene / 지정 Scene보다
        // End Stage 진입을 우선한다.
        //
        // 보상 계산과 지급은 이미 HandleBattleWin()에서 끝난 뒤
        // 이 함수가 호출되므로 기존 보상 흐름은 그대로 유지된다.
        if (TryMoveToEndStageAfterTimerExpired())
        {
            return;
        }


        // <변경부분> StageBattleData에서 전달받은 목적지가 있으면 우선 사용하고,
        // 없다면 기존 WorldMap Scene 설정을 fallback으로 사용한다.
        string targetSceneName =
            string.IsNullOrWhiteSpace(
                victorySceneName)
                ? mapSceneName
                : victorySceneName;

        if (string.IsNullOrWhiteSpace(
                targetSceneName))
        {
            Debug.LogWarning(
                "승리 후 Scene 이동 실패: " +
                "이동할 Scene 이름이 비어 있습니다."
            );

            return;
        }

        // <변경부분> 실제 목적지가 기존 WorldMap Scene일 때만
        // 현재 전투 노드를 클리어 예정 상태로 기록한다.
        //
        // 최종 보스처럼 Title Scene 등으로 바로 나가는 경우에는
        // WorldMapRuntimeState를 불필요하게 변경하지 않는다.
        bool isReturningToWorldMap =
            string.Equals(
                targetSceneName,
                mapSceneName,
                System.StringComparison.Ordinal
            );

        if (lastBattleResult ==
                BattleResult.Win &&
            isReturningToWorldMap)
        {
            WorldMapRuntimeState.MarkBattleWon();

            Debug.Log(
                "월드맵 진행도 기록 완료: " +
                "현재 전투 노드를 클리어 예정 상태로 저장했습니다."
            );
        }

        // <변경부분> 승리 후 컷씬 데이터가 지정되어 있다면
        // Scene을 이동하기 전에 Pending Cutscene Data로 등록한다.
        //
        // TextCutsceneScene은 시작 시 이 Pending Data를 가져와
        // Inspector 기본 데이터보다 우선하여 실행한다.
        if (victoryCutsceneData != null)
        {
            TextCutsceneRuntimeState
                .SetPendingCutsceneData(
                    victoryCutsceneData
                );

            Debug.Log(
                $"승리 후 컷씬 데이터 전달 완료: " +
                $"{victoryCutsceneData.name}"
            );
        }

        Debug.Log(
            $"리워드 확인 완료: " +
            $"승리 후 Scene으로 이동합니다. / " +
            $"{targetSceneName}"
        );

        WorldMapEventRuntimeState.Queue(victoryWorldMapEventData);
        SceneManager.LoadScene(
            targetSceneName
        );
    }

    // <변경부분> 마지막 전투 결과 반환
    public BattleResult GetLastBattleResult()
    {
        return lastBattleResult;
    }
}

// <변경부분> 전투 종료 시 실제 복구된 기물 종류와 수량을
// 보상 결과 UI에 전달하기 위한 런타임 데이터
//
// BattleRewardPopupUI에서도 사용하는 공용 데이터이므로
// BattleEndFlowController 클래스 바깥에 public 클래스로 선언한다.
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
        this.pieceData =
            pieceData;

        this.amount =
            Mathf.Max(
                0,
                amount
            );
    }

    // <변경부분> 외부 UI가 원본 결과 데이터를
    // 직접 수정하지 못하도록 복사본을 생성한다.
    public BattleRecoveryRewardRuntimeData Clone()
    {
        return new BattleRecoveryRewardRuntimeData(
            pieceData,
            amount
        );
    }
}

