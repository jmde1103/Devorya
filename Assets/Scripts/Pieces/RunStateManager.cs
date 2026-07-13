using System.Collections.Generic;
using UnityEngine;

// <변경부분> 로그라이크 런 전체 상태를 씬 이동 사이에서 유지하는 매니저
// 현재 1차 구현에서는 플레이어 기물 상태만 저장한다.
public class RunStateManager : MonoBehaviour
{
    public static RunStateManager Instance { get; private set; }

    [Header("Player Runtime Pieces")]
    // <변경부분> 현재 런에서 유지되는 플레이어 기물 상태 목록
    [SerializeField]
    private List<PlayerPieceRuntimeData> playerPieceRuntimeDataList =
        new List<PlayerPieceRuntimeData>();

    [Header("Currency")]
    // <변경부분> 현재 런에서 보유 중인 금화
    [SerializeField] private int goldAmount = 0;

    // <변경부분> 저장된 플레이어 기물 데이터가 있는지 여부
    public bool HasPlayerPieceRuntimeData
    {
        get
        {
            return playerPieceRuntimeDataList != null &&
                   playerPieceRuntimeDataList.Count > 0;
        }
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

    // <변경부분> 새 런 시작 또는 디버그 초기화용
    public void ClearRunState()
    {
        playerPieceRuntimeDataList.Clear();

        // <변경부분> 새 런 시작 시 금화도 초기화
        goldAmount = 0;

        Debug.Log("런 상태 초기화 완료");
    }
}
