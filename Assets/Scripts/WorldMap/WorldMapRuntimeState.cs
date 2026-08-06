using System.Collections.Generic;

// 씬을 이동하는 동안 유지되어야 하는
// 월드맵의 간단한 런타임 진행 상태를 저장한다.
//
// 발표용 최소 구현이므로 게임 종료 후에도 남는
// 영구 세이브 파일은 아직 사용하지 않는다.
public static class WorldMapRuntimeState
{
    // 현재 검은 구체가 위치한 노드 ID
    private static string currentNodeId;

    // 현재 진입한 전투 노드 ID
    //
    // 전투 승리 후 월드맵으로 돌아왔을 때
    // 어떤 노드를 클리어해야 하는지 확인할 때 사용한다.
    private static string enteredBattleNodeId;

    // 현재 전투 승리 결과가 월드맵에 반영되지 않은 상태인지 확인한다.
    private static bool hasPendingBattleWin;

    // 런타임에서 클리어한 노드 ID 목록
    private static readonly HashSet<string>
        clearedNodeIds =
            new HashSet<string>();

    // 런타임에서 해금된 노드 ID 목록
    private static readonly HashSet<string>
        unlockedNodeIds =
            new HashSet<string>();

    // 현재 위치한 노드 ID를 반환한다.
    public static string CurrentNodeId
    {
        get { return currentNodeId; }
    }

    // 마지막으로 진입한 전투 노드 ID를 반환한다.
    public static string EnteredBattleNodeId
    {
        get { return enteredBattleNodeId; }
    }

    // 월드맵을 처음 열었거나 런타임 상태가 비어 있을 때
    // 시작 노드의 초기 상태를 등록한다.
    public static void InitializeStartNode(
        string startNodeId)
    {
        if (string.IsNullOrWhiteSpace(
                startNodeId))
        {
            return;
        }

        // 이미 현재 노드가 정해져 있다면
        // 전투 후 맵으로 복귀한 상태이므로 덮어쓰지 않는다.
        if (string.IsNullOrWhiteSpace(
                currentNodeId) == false)
        {
            return;
        }

        currentNodeId =
            startNodeId;

        clearedNodeIds.Add(
            startNodeId
        );

        unlockedNodeIds.Add(
            startNodeId
        );
    }

    // 맵에서 전투 노드로 이동하기 직전에
    // 현재 목적지 노드 정보를 저장한다.
    public static void BeginBattleNode(
        string battleNodeId)
    {
        if (string.IsNullOrWhiteSpace(
                battleNodeId))
        {
            return;
        }

        enteredBattleNodeId =
            battleNodeId;

        hasPendingBattleWin =
            false;
    }

    // 전투 승리 후 월드맵으로 돌아가기 직전에
    // 해당 전투 노드를 클리어 예정 상태로 저장한다.
    public static void MarkBattleWon()
    {
        if (string.IsNullOrWhiteSpace(
                enteredBattleNodeId))
        {
            return;
        }

        hasPendingBattleWin =
            true;
    }

    // 월드맵 씬이 다시 시작됐을 때
    // 대기 중인 전투 승리 결과를 실제 맵 진행도에 반영한다.
    public static string ApplyPendingBattleWin()
    {
        if (hasPendingBattleWin == false ||
            string.IsNullOrWhiteSpace(
                enteredBattleNodeId))
        {
            return null;
        }

        string clearedBattleNodeId =
            enteredBattleNodeId;

        currentNodeId =
            clearedBattleNodeId;

        clearedNodeIds.Add(
            clearedBattleNodeId
        );

        unlockedNodeIds.Add(
            clearedBattleNodeId
        );

        enteredBattleNodeId =
            null;

        hasPendingBattleWin =
            false;

        return clearedBattleNodeId;
    }

    // 지정한 노드를 해금 상태로 저장한다.
    public static void UnlockNode(
        string nodeId)
    {
        if (string.IsNullOrWhiteSpace(
                nodeId))
        {
            return;
        }

        unlockedNodeIds.Add(
            nodeId
        );
    }

    // 지정한 노드가 현재 해금 상태인지 확인한다.
    public static bool IsNodeUnlocked(
        string nodeId)
    {
        if (string.IsNullOrWhiteSpace(
                nodeId))
        {
            return false;
        }

        return unlockedNodeIds.Contains(
            nodeId
        );
    }

    // 지정한 노드가 현재 클리어 상태인지 확인한다.
    public static bool IsNodeCleared(
        string nodeId)
    {
        if (string.IsNullOrWhiteSpace(
                nodeId))
        {
            return false;
        }

        return clearedNodeIds.Contains(
            nodeId
        );
    }

    // 현재 검은 구체 위치를 특정 노드로 직접 변경한다.
    //
    // 노드 이동 코루틴이 목적지에 도착했을 때 사용한다.
    public static void SetCurrentNode(
        string nodeId)
    {
        if (string.IsNullOrWhiteSpace(
                nodeId))
        {
            return;
        }

        currentNodeId =
            nodeId;
    }

    // 새 런을 시작할 때 월드맵 진행 상태를 전부 초기화한다.
    public static void Clear()
    {
        currentNodeId =
            null;

        enteredBattleNodeId =
            null;

        hasPendingBattleWin =
            false;

        clearedNodeIds.Clear();
        unlockedNodeIds.Clear();
    }
}