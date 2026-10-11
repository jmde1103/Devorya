using System.Collections.Generic;
using UnityEngine;

// 씬을 이동하는 동안 유지되어야 하는
// 월드맵의 간단한 런타임 진행 상태를 저장한다.
//
// 발표용 최소 구현이므로 게임 종료 후에도 남는
// 영구 세이브 파일은 아직 사용하지 않는다.
public static class WorldMapRuntimeState
{
    // 현재 검은 구체가 위치한 노드 ID
    private static string currentNodeId;

    /// 현재 진입한 전투 노드 ID
    //
    // 전투 승리 후 월드맵으로 돌아왔을 때
    // 어떤 노드를 클리어해야 하는지 확인할 때 사용한다.
    private static string enteredBattleNodeId;

    // 맵 노드에서 선택되어
    // 다음 BattleScene에서 사용할 StageBattleData.
    //
    // ScriptableObject 참조는 씬이 변경되어도
    // 이 static 런타임 상태를 통해 유지된다.
    private static StageBattleData pendingStageBattleData;

    // <변경부분>
    // 월드맵의 Event / RuinsEvent / Shop 노드에서 선택되어
    // 다음 공용 EventScene에서 사용할 EventSceneData.
    //
    // Scene이 변경되어도 static RuntimeState를 통해
    // 선택된 이벤트 데이터를 유지한다.
    private static EventSceneData pendingEventSceneData;


    // ============================================================
    // Shop Node Runtime Data
    // ============================================================

    // <변경부분>
    // WorldMap Shop Node에서 지정한 상품 후보 데이터.
    //
    // EventSceneData는 이벤트 연출과 배경을 담당하고,
    // ShopInventoryData는 판매 가능한 상품 목록을 담당한다.
    //
    // ScriptableObject 원본을 변경하지 않는다.
    private static ShopInventoryData pendingShopInventoryData;

    // <변경부분>
    // WorldMap Shop Node에서 직접 지정한 상점 레벨.
    //
    // 추후 월드맵 진행도에 따른 자동 레벨 결정 기능이
    // 추가되더라도 전달 구조는 그대로 유지한다.
    private static int pendingShopLevel = 1;

    // <변경부분>
    // 다음 EventScene에 전달할 상점 정보가 있는지 구분한다.
    //
    // 일반 Event / RuinsEvent에서는 false다.
    private static bool hasPendingShopNodeData;


    // 현재 전투 승리 결과가 월드맵에 반영되지 않은 상태인지 확인한다.
    private static bool hasPendingBattleWin;


    // <변경부분>
    // 현재 Battle이 WorldMap Node에서 진입한 일반 Battle이 아니라
    // BeginDirectBattle()을 통해 직접 시작된 Battle인지 저장한다.
    //
    // 현재는 엔드 타이머 만료 후 진입하는 End Stage를
    // 일반 Battle과 구분하는 용도로 사용한다.
    private static bool isDirectBattle;


    // 런타임에서 클리어한 노드 ID 목록
    //
    // 시작 노드와 전투에서 승리한 노드를 저장하여
    // 월드맵 복귀 후에도 클리어 상태를 유지한다.
    private static readonly HashSet<string>
        clearedNodeIds =
            new HashSet<string>();

    // 런타임에서 해금된 노드 ID 목록
    private static readonly HashSet<string>
        unlockedNodeIds =
            new HashSet<string>();

    // 맵 ID별로 완전히 탐사된 포그 중심 셀을 저장한다.
    //
    // 실제 그라데이션 셀 전체를 저장하지 않고,
    // 탐사 중심 셀만 저장한 뒤 월드맵 복귀 시 주변 그라데이션을 재생성한다.
    private static readonly Dictionary<
        string,
        HashSet<Vector2Int>>
        exploredFogCellsByMapId =
            new Dictionary<
                string,
                HashSet<Vector2Int>
            >();

    // 맵 ID별로 다음 탐사 후보로 발견된
    // 노드와 길의 Preview 중심 셀을 저장한다.
    private static readonly Dictionary<
        string,
        HashSet<Vector2Int>>
        previewFogCellsByMapId =
            new Dictionary<
                string,
                HashSet<Vector2Int>
            >();

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

    // 현재 맵에서 BattleScene으로 전달한
    // StageBattleData를 반환한다.
    //
    // BattleSetupManager가 전투 시작 시 이 값을 사용한다.
    public static StageBattleData PendingStageBattleData
    {
        get { return pendingStageBattleData; }
    }


    // <변경부분>
    // 현재 Battle이 BeginDirectBattle()을 통해
    // 직접 시작된 Battle인지 반환한다.
    //
    // End Stage에서 타이머가 계속 0인 상태여도
    // 다시 End Stage로 진입하는 것을 막을 때 사용한다.
    public static bool IsDirectBattle
    {
        get { return isDirectBattle; }
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
     string battleNodeId,
     StageBattleData stageBattleData)
    {
        if (string.IsNullOrWhiteSpace(
                battleNodeId))
        {
            return;
        }

        // 전투 종료 후 어떤 맵 노드를
        // 클리어 처리할지 저장한다.
        enteredBattleNodeId =
            battleNodeId;

        // 현재 노드에서 선택된 실제 StageBattleData를 저장한다.
        //
        // 다음 BattleScene의 BattleSetupManager가
        // 이 데이터를 직접 가져와 전투를 구성한다.
        pendingStageBattleData =
     stageBattleData;

        hasPendingBattleWin =
            false;


        // <변경부분>
        // WorldMap Node에서 정상적으로 진입한 Battle이므로
        // Direct Battle 상태를 해제한다.
        isDirectBattle =
      false;

        // <변경부분>
        // Battle 진입 시 이전 EventScene 전달 데이터가
        // 남아 있지 않도록 정리한다.
        pendingEventSceneData =
            null;

        // <변경부분>
        // 이전 Shop 노드의 상품 정보도 함께 정리한다.
        ClearPendingShopNodeData();
    }


    // <변경부분>
    // WorldMap의 Event / RuinsEvent / Shop 노드에서
    // 공용 EventScene으로 이동하기 직전에
    // 실행할 EventSceneData를 등록한다.
    public static void BeginEventNode(
        EventSceneData eventSceneData)
    {
        if (eventSceneData == null)
        {
            Debug.LogWarning(
                "이벤트 노드 데이터 등록 실패: " +
                "EventSceneData가 없습니다."
            );

            return;
        }

        pendingEventSceneData =
    eventSceneData;


        // <변경부분>
        // 일반 Event 진입 시 이전 상점 정보를 제거한다.
        //
        // Shop 진입 역시 이 함수를 먼저 호출하지만,
        // 이후 BeginShopNode()에서 새로운 상점 정보를
        // 등록하므로 충돌하지 않는다.
        ClearPendingShopNodeData();


        // EventScene 진입은 Battle 진입이 아니므로
        // 이전 Battle 전달 상태가 잘못 남지 않도록 정리한다.
        enteredBattleNodeId =
            null;

        pendingStageBattleData =
            null;

        hasPendingBattleWin =
            false;

        isDirectBattle =
            false;

        Debug.Log(
       $"이벤트 노드 데이터 등록 완료: " +
       $"{eventSceneData.name}"
   );
    }


    // ============================================================
    // Shop Node Entry
    // ============================================================

    // <변경부분>
    // WorldMap의 Shop 노드에서 EventScene으로 진입하기 전에
    // 상점 이벤트와 상품 목록, 상점 레벨을 함께 등록한다.
    //
    // 일반 BeginEventNode()와 달리
    // 상점용 상품 데이터까지 함께 전달한다.
    //
    // 이 함수는 런타임 데이터를 등록하는 역할만 한다.
    // 실제 Scene 이동은 WorldMap의 기존 진입 코드에서 처리한다.
    public static bool BeginShopNode(
        EventSceneData eventSceneData,
        ShopInventoryData shopInventoryData,
        int shopLevel)
    {
        if (eventSceneData == null ||
            shopInventoryData == null ||
            shopLevel < 1)
        {
            Debug.LogWarning(
                "[WorldMapRuntimeState] 상점 진입 실패: " +
                "EventSceneData, ShopInventoryData 또는 " +
                "Shop Level 설정을 확인해주세요."
            );

            return false;
        }


        // <변경부분>
        // 잘못 지정된 레벨의 상품 목록을 전달하지 않는다.
        if (shopInventoryData.GetLevelData(shopLevel) == null)
        {
            Debug.LogWarning(
                "[WorldMapRuntimeState] 상점 진입 실패: " +
                $"{shopInventoryData.name}에 " +
                $"Shop Level {shopLevel} 데이터가 없습니다."
            );

            return false;
        }


        // <변경부분>
        // 기존 이벤트 진입 처리 재사용.
        //
        // Battle 진입 상태 초기화 등 기존 기능을 보존한다.
        BeginEventNode(
            eventSceneData
        );


        // <변경부분>
        // Shop 노드 전용 상품 정보 저장.
        pendingShopInventoryData =
            shopInventoryData;

        pendingShopLevel =
            shopLevel;

        hasPendingShopNodeData =
            true;


        Debug.Log(
            "[WorldMapRuntimeState] 상점 데이터 등록 완료: " +
            $"{eventSceneData.name} / " +
            $"{shopInventoryData.name} / " +
            $"Level {shopLevel}"
        );

        return true;
    }


    // <변경부분>
    // EventSceneController가 Shop 전용 데이터를
    // 한 번 가져간 뒤 RuntimeState에서 제거한다.
    //
    // EventSceneData는 기존
    // ConsumePendingEventSceneData()로 별도 수신한다.
    //
    // 반환값이 false이면 일반 Event로 취급할 수 있다.
    public static bool TryConsumePendingShopNodeData(
        out ShopInventoryData inventoryData,
        out int shopLevel)
    {
        inventoryData =
            null;

        shopLevel =
            1;


        if (!hasPendingShopNodeData)
        {
            return false;
        }


        inventoryData =
            pendingShopInventoryData;

        shopLevel =
            pendingShopLevel;


        // <변경부분>
        // 다음 EventScene에 이전 상점 데이터가
        // 전달되지 않도록 소비 후 정리한다.
        ClearPendingShopNodeData();

        return inventoryData != null;
    }


    // <변경부분>
    // 이전 Shop 노드의 전달 정보만 초기화한다.
    //
    // EventSceneData나 WorldMap 진행 상태는 변경하지 않는다.
    private static void ClearPendingShopNodeData()
    {
        pendingShopInventoryData =
            null;

        pendingShopLevel =
            1;

        hasPendingShopNodeData =
            false;
    }


    // <변경부분>
    // EventSceneController가 현재 대기 중인 EventSceneData를
    // 한 번 가져간 뒤 RuntimeState에서 제거한다.
    //
    // 이전 EventSceneData가 다음 EventScene 진입에
    // 잘못 재사용되는 것을 방지한다.
    public static EventSceneData
        ConsumePendingEventSceneData()
    {
        EventSceneData result =
            pendingEventSceneData;

        pendingEventSceneData =
            null;

        return result;
    }


    // <변경부분>
    // WorldMap Node를 거치지 않고
    // StageBattleData를 직접 다음 BattleScene으로 전달한다.
    //
    // 엔드 타이머가 0이 된 뒤 현재 Battle의 보상 정산까지 끝나면
    // End Stage를 강제로 시작할 때 사용한다.
    //
    // 일반 월드맵 Battle과 달리 특정 Node를 클리어 대상으로
    // 등록하면 안 되므로 enteredBattleNodeId는 비운다.
    public static bool BeginDirectBattle(
        StageBattleData stageBattleData)
    {
        if (stageBattleData == null)
        {
            Debug.LogWarning(
                "직접 전투 시작 데이터 등록 실패: " +
                "StageBattleData가 없습니다."
            );

            return false;
        }


        // <변경부분>
        // 직접 시작하는 전투는 특정 WorldMap Node에서
        // 진입한 전투가 아니므로 이전 Node 참조를 제거한다.
        //
        // 이렇게 해야 End Stage 승리 후
        // 기존 월드맵 Node가 잘못 클리어되는 것을 막을 수 있다.
        enteredBattleNodeId =
            null;


        // <변경부분>
        // 다음 BattleScene의 BattleSetupManager가
        // 기존과 동일한 PendingStageBattleData 경로로 읽는다.
        pendingStageBattleData =
       stageBattleData;

        // <변경부분>
        // 직접 Battle을 시작할 때도 이전 EventScene 전달 데이터가
        // 남아 있지 않도록 정리한다.
        pendingEventSceneData =
            null;

        // <변경부분>
        // Direct Battle에도 Shop 데이터가 전달되지 않게 한다.
        ClearPendingShopNodeData();


        // 새 Battle이므로 이전 승리 대기 상태도 초기화한다.
        hasPendingBattleWin =
            false;


        // <변경부분>
        // 이 전투가 WorldMap Node와 무관하게
        // 직접 시작된 Battle임을 기록한다.
        //
        // End Stage 종료 시 타이머가 계속 0이어도
        // 다시 End Stage를 호출하지 않도록 구분하는 값이다.
        isDirectBattle =
            true;


        Debug.Log(
            $"직접 전투 데이터 등록 완료: " +
            $"{stageBattleData.name}"
        );


        return true;
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

        // 전투가 끝나고 월드맵에 복귀했으므로
        // 이전 BattleScene에 전달했던 StageBattleData 참조도 제거한다.
        pendingStageBattleData =
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

    // 지정한 맵의 셀을 완전 탐사 상태로 저장한다.
    //
    // 완전 탐사된 셀은 Preview 목록에서 제거하며,
    // 이후 다시 어두운 상태로 내려가지 않는다.
    public static void RegisterExploredFogCell(
        string mapId,
        Vector2Int cellPosition)
    {
        if (string.IsNullOrWhiteSpace(
                mapId))
        {
            return;
        }

        HashSet<Vector2Int> exploredCells =
            GetOrCreateFogCellSet(
                exploredFogCellsByMapId,
                mapId
            );

        exploredCells.Add(
            cellPosition
        );

        HashSet<Vector2Int> previewCells;

        if (previewFogCellsByMapId.TryGetValue(
                mapId,
                out previewCells))
        {
            previewCells.Remove(
                cellPosition
            );
        }
    }

    // 지정한 맵의 셀을 다음 탐사 후보 Preview 상태로 저장한다.
    //
    // 이미 완전히 탐사된 셀은 Preview 상태로 되돌리지 않는다.
    public static void RegisterPreviewFogCell(
        string mapId,
        Vector2Int cellPosition)
    {
        if (string.IsNullOrWhiteSpace(
                mapId))
        {
            return;
        }

        HashSet<Vector2Int> exploredCells;

        if (
            exploredFogCellsByMapId.TryGetValue(
                mapId,
                out exploredCells) &&
            exploredCells.Contains(
                cellPosition
            )
        )
        {
            return;
        }

        HashSet<Vector2Int> previewCells =
            GetOrCreateFogCellSet(
                previewFogCellsByMapId,
                mapId
            );

        previewCells.Add(
            cellPosition
        );
    }

    // 지정한 맵의 완전 탐사 셀을
    // 전달받은 목록에 복사한다.
    public static void CopyExploredFogCells(
        string mapId,
        List<Vector2Int> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        if (string.IsNullOrWhiteSpace(
                mapId))
        {
            return;
        }

        HashSet<Vector2Int> exploredCells;

        if (exploredFogCellsByMapId.TryGetValue(
                mapId,
                out exploredCells))
        {
            result.AddRange(
                exploredCells
            );
        }
    }

    // 지정한 맵의 Preview 셀을
    // 전달받은 목록에 복사한다.
    public static void CopyPreviewFogCells(
        string mapId,
        List<Vector2Int> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        if (string.IsNullOrWhiteSpace(
                mapId))
        {
            return;
        }

        HashSet<Vector2Int> previewCells;

        if (previewFogCellsByMapId.TryGetValue(
                mapId,
                out previewCells))
        {
            result.AddRange(
                previewCells
            );
        }
    }

    // 지정한 맵 ID의 포그 셀 목록을 가져오거나
    // 아직 없다면 새 목록을 생성한다.
    private static HashSet<Vector2Int>
        GetOrCreateFogCellSet(
            Dictionary<
                string,
                HashSet<Vector2Int>>
                fogCellsByMapId,
            string mapId)
    {
        HashSet<Vector2Int> fogCells;

        if (fogCellsByMapId.TryGetValue(
                mapId,
                out fogCells))
        {
            return fogCells;
        }

        fogCells =
            new HashSet<Vector2Int>();

        fogCellsByMapId.Add(
            mapId,
            fogCells
        );

        return fogCells;
    }

    // 새 런을 시작할 때 월드맵 진행 상태를 전부 초기화한다.
    public static void Clear()
    {
        currentNodeId =
            null;

        enteredBattleNodeId =
            null;

        // 새 런 시작 시 이전에 전달했던
        // 전투 / 이벤트 Scene 데이터 참조를 모두 제거한다.
        pendingStageBattleData =
            null;

        pendingEventSceneData =
       null;

        // <변경부분>
        // 새 런에서 이전 Shop 데이터를 사용하지 않도록 초기화한다.
        ClearPendingShopNodeData();

        hasPendingBattleWin =
            false;


        // <변경부분>
        // 새 Run에서는 이전 Direct Battle 상태도 제거한다.
        isDirectBattle =
            false;


        clearedNodeIds.Clear();
        unlockedNodeIds.Clear();

        // 새 런에서는 이전 월드맵에서 탐사한
        // 포그 영역과 Preview 영역도 모두 초기화한다.
        exploredFogCellsByMapId.Clear();
        previewFogCellsByMapId.Clear();
    }
}