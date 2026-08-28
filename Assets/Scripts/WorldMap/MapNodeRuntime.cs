using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 월드맵에 실제로 생성된 노드의 표시와 클릭을 관리한다.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class MapNodeRuntime : MonoBehaviour
{
    [Header("Node Identity")]
    // 맵 진행도와 노드 연결 관계에서 사용할 고유 ID
    [SerializeField]
    private string nodeId;

    // Inspector와 로그에서 확인할 노드 이름
    [SerializeField]
    private string nodeDisplayName;

    [Header("Node Type")]
    // 노드의 실제 게임플레이 역할.
    //
    // Battle / BossBattle / Event / RuinsEvent / Shop 등의
    // 의미 판정은 Visual Style이 아니라 이 값을 기준으로 한다.
    //
    // Cleared Style로 외형이 변경되어도
    // 원래 노드 역할은 유지된다.
    [SerializeField]
    private MapNodeType nodeType =
        MapNodeType.Battle;

    [Header("Node Style")]
    // 현재 노드에 표시할 Sprite / Color / Collider 등의
    // 시각적 스타일 데이터.
    //
    // 노드의 실제 역할 판정에는 사용하지 않는다.
    [SerializeField]
    private MapNodeStyleData nodeStyleData;

    [Header("Stage Scene")]
    // 노드를 클릭했을 때 이동할 씬 이름
    [SerializeField]
    private string targetSceneName;

    [Header("Battle Stage Data")]
    // 현재 전투 노드에 연결된 실제 StageBattleData.
    //
    // WorldMapBuilder가 MapNodePlacementData의 값을
    // 런타임 노드 생성 시 전달한다.
    [SerializeField]
    private StageBattleData stageBattleData;

    [Header("Node Connection")]
    // 현재 노드에서 연결되는 목적지와
    // 해당 목적지까지 이동할 Route Grid 좌표를 함께 보관한다.
    //
    // WorldMapData의 MapNodePlacementData.connections를
    // WorldMapBuilder가 생성 시 복사하여 전달한다.
    [SerializeField]
    private List<MapNodeConnectionData> connections =
     new List<MapNodeConnectionData>();

    [Header("Node State")]
    // 현재 노드가 해금되어 클릭 가능한지 여부
    [SerializeField]
    private bool isUnlocked = true;

    // 이미 클리어한 노드인지 여부
    [SerializeField]
    private bool isCleared = false;

    [Header("Runtime References")]
    // 실제 노드 Sprite를 표시하는 Renderer
    [SerializeField]
    private SpriteRenderer nodeSpriteRenderer;

    // 노드 클릭 판정을 담당하는 Collider
    [SerializeField]
    private BoxCollider2D nodeCollider;

    [Header("Selectable Node Pulse")]
    // 현재 위치에서 이동 가능한 다음 노드임을 표시할 때
    // 원래 크기에서 얼마나 확대할지 결정한다.
    //
    // 1.08 = 원래 크기의 108%
    [SerializeField, Min(1f)]
    private float selectablePulseScale =
        1.08f;

    // 기본 크기 → 확대 또는 확대 → 기본 크기까지
    // 한 방향으로 변화하는 데 걸리는 시간.
    [SerializeField, Min(0.05f)]
    private float selectablePulseHalfDuration =
        0.7f;

    // 현재 선택 가능 노드 Pulse Coroutine.
    private Coroutine selectablePulseCoroutine;

    // Pulse 시작 직전 노드의 실제 Scale.
    // 프리팹 Scale이 1이 아니더라도 정확히 원래 크기로 복원하기 위해 저장한다.
    private Vector3 selectablePulseBaseScale;

    // 현재 Pulse용 원본 Scale을 저장했는지 확인한다.
    private bool hasSelectablePulseBaseScale;

    // 현재 포인터가 노드 위에서 눌렸는지 확인한다.
    private bool isPointerPressed;
    // 모바일 / PC에서 현재 포인터 위치의 UI Raycast 결과를 재사용한다.
    // 노드를 클릭할 때마다 List를 새로 생성하지 않아 불필요한 GC 할당을 방지한다.
    private static readonly List<RaycastResult> pointerRaycastResults =
        new List<RaycastResult>();

    private void Reset()
    {
        // 스크립트를 처음 추가했을 때
        // 같은 GameObject의 필수 컴포넌트를 자동으로 연결한다.
        nodeSpriteRenderer =
            GetComponent<SpriteRenderer>();

        nodeCollider =
            GetComponent<BoxCollider2D>();
    }

    private void Awake()
    {
        // Inspector 연결이 빠져 있어도
        // 같은 GameObject에서 필수 컴포넌트를 다시 찾는다.
        CacheRequiredComponents();

        // 현재 연결된 스타일 데이터를 노드에 적용한다.
        ApplyStyle();
    }

    private void OnDisable()
    {
        // Scene 전환이나 노드 비활성화 중 Coroutine이 종료될 때
        // 확대된 Scale이 남지 않도록 반드시 원래 크기로 복원한다.
        StopSelectablePulse();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 Style Data를 교체하면
        // 플레이하지 않아도 Sprite와 Collider가 즉시 갱신된다.
        CacheRequiredComponents();
        ApplyStyle();
    }
#endif

    // 필수 컴포넌트 참조를 안전하게 연결한다.
    private void CacheRequiredComponents()
    {
        if (nodeSpriteRenderer == null)
        {
            nodeSpriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (nodeCollider == null)
        {
            nodeCollider =
                GetComponent<BoxCollider2D>();
        }
    }

    // 현재 노드 스타일 데이터를 Sprite와 Collider에 적용한다.
    public void ApplyStyle()
    {
        if (nodeStyleData == null)
        {
            return;
        }

        if (nodeSpriteRenderer != null)
        {
            // 스타일 데이터의 Sprite와 색상을 적용한다.
            nodeSpriteRenderer.sprite =
                nodeStyleData.NodeSprite;

            nodeSpriteRenderer.color =
                nodeStyleData.SpriteColor;
        }

        if (nodeCollider != null)
        {
            // 스타일 데이터에서 지정한 클릭 범위를 적용한다.
            nodeCollider.size =
                nodeStyleData.ColliderSize;

            nodeCollider.offset =
                nodeStyleData.ColliderOffset;
        }
    }

    // 맵 에디터가 노드를 생성할 때
    // 노드의 기본 정보를 한 번에 설정한다.
    public void Initialize(
    string newNodeId,
    string newDisplayName,
    MapNodeType newNodeType,
    MapNodeStyleData newStyleData,
    string newTargetSceneName,
    StageBattleData newStageBattleData,
    List<MapNodeConnectionData> newConnections,
    bool unlocked)
    {
        nodeId =
            newNodeId;

        nodeDisplayName =
            newDisplayName;

        // WorldMapData에 저장된 원래 Node Type을
        // Runtime 노드의 실제 역할로 보관한다.
        //
        // 이후 Cleared Style로 외형이 변경되어도
        // 이 값은 변경하지 않는다.
        nodeType =
            newNodeType;

        // Style Data는 Sprite / Color / Collider 등
        // 현재 시각 표현만 담당한다.
        nodeStyleData =
            newStyleData;

        targetSceneName =
            newTargetSceneName;

        // 전투 노드에서 사용할 StageBattleData를
        // 원본 MapNodePlacementData에서 전달받아 저장한다.
        stageBattleData =
            newStageBattleData;

        // 연결 및 Route 정보는
        // 원본 데이터와 별개의 Runtime 복사본으로 유지한다.
        connections =
            CopyConnections(
                newConnections
            );

        isUnlocked =
            unlocked;

        ApplyStyle();
    }

    // 원본 MapNodeConnectionData 목록을
    // 런타임 노드용 독립 복사본으로 생성한다.
    private List<MapNodeConnectionData> CopyConnections(
        List<MapNodeConnectionData> sourceConnections)
    {
        List<MapNodeConnectionData> copiedConnections =
            new List<MapNodeConnectionData>();

        if (sourceConnections == null)
        {
            return copiedConnections;
        }

        for (int i = 0;
             i < sourceConnections.Count;
             i++)
        {
            MapNodeConnectionData sourceConnection =
                sourceConnections[i];

            if (sourceConnection == null)
            {
                continue;
            }

            MapNodeConnectionData copiedConnection =
                new MapNodeConnectionData();

            copiedConnection.targetNodeId =
                sourceConnection.targetNodeId;

            copiedConnection.routeGridPositions =
                sourceConnection.routeGridPositions != null
                    ? new List<Vector2Int>(
                        sourceConnection.routeGridPositions
                    )
                    : new List<Vector2Int>();

            copiedConnections.Add(
                copiedConnection
            );
        }

        return copiedConnections;
    }

    // 현재 노드가 플레이어가 선택할 수 있는 다음 이동 후보인지에 따라
    // 부드러운 확대/축소 Pulse 효과를 시작하거나 종료한다.
    public void SetSelectablePulse(
        bool shouldPulse)
    {
        // 실제로 이동 가능한 미클리어 노드에서만
        // Pulse 효과를 허용한다.
        bool canPulse =
            shouldPulse &&
            isUnlocked &&
            isCleared == false;

        if (canPulse)
        {
            StartSelectablePulse();

            return;
        }

        StopSelectablePulse();
    }

    // 선택 가능한 다음 노드의 Pulse Coroutine을 시작한다.
    private void StartSelectablePulse()
    {
        // 이미 실행 중이면 중복 Coroutine을 만들지 않는다.
        if (selectablePulseCoroutine != null)
        {
            return;
        }

        // 현재 실제 Scale을 기준값으로 저장한다.
        // 프리팹이나 에디터에서 Scale이 변경돼도
        // Vector3.one으로 강제 복원하지 않는다.
        selectablePulseBaseScale =
            transform.localScale;

        hasSelectablePulseBaseScale =
            true;

        selectablePulseCoroutine =
            StartCoroutine(
                SelectablePulseRoutine()
            );
    }

    // 현재 선택 가능 Pulse를 종료하고
    // 노드 Scale을 시작 전 크기로 정확하게 복원한다.
    private void StopSelectablePulse()
    {
        if (selectablePulseCoroutine != null)
        {
            StopCoroutine(
                selectablePulseCoroutine
            );

            selectablePulseCoroutine =
                null;
        }

        if (hasSelectablePulseBaseScale)
        {
            transform.localScale =
                selectablePulseBaseScale;

            hasSelectablePulseBaseScale =
                false;
        }
    }

    // 선택 가능한 다음 노드를
    // 기본 크기 → 확대 → 기본 크기로 계속 부드럽게 반복한다.
    private IEnumerator SelectablePulseRoutine()
    {
        Vector3 expandedScale =
            selectablePulseBaseScale *
            selectablePulseScale;

        while (true)
        {
            // 기본 크기에서 확대 크기까지 부드럽게 변화한다.
            yield return
                AnimateSelectablePulseScaleRoutine(
                    selectablePulseBaseScale,
                    expandedScale
                );

            // 확대 크기에서 다시 기본 크기로 부드럽게 돌아온다.
            yield return
                AnimateSelectablePulseScaleRoutine(
                    expandedScale,
                    selectablePulseBaseScale
                );
        }
    }

    // 지정된 두 Scale 사이를 SmoothStep으로 보간한다.
    private IEnumerator AnimateSelectablePulseScaleRoutine(
        Vector3 startScale,
        Vector3 targetScale)
    {
        float safeDuration =
            Mathf.Max(
                0.05f,
                selectablePulseHalfDuration
            );

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    safeDuration
                );

            // 시작과 끝에서 갑자기 움직이는 느낌이 없도록
            // 부드러운 Ease In / Ease Out 보간을 적용한다.
            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            transform.localScale =
                Vector3.LerpUnclamped(
                    startScale,
                    targetScale,
                    smoothTime
                );

            yield return null;
        }

        transform.localScale =
            targetScale;
    }

    private void OnMouseDown()
    {
        // <변경부분> PC의 Node Click / Camera Drag 판정은
        // WorldMapCameraController에서 통합 처리한다.
        //
        // 현재 Node 자체 Mouse 이벤트는
        // 모바일 기존 입력 보존용으로만 사용한다.
        if (Application.isMobilePlatform == false)
        {
            isPointerPressed =
                false;

            return;
        }
        // 모바일에서 두 손가락이 사용 중이라면
        // WorldMapCameraController의 Pinch / Drag 제스처로 판단하고
        // 노드 클릭을 시작하지 않는다.
        if (Input.touchCount >= 2)
        {
            isPointerPressed =
                false;

            return;
        }

        // UI 위에서 시작된 Mouse / Touch라면
        // 뒤쪽 WorldMap Node로 입력을 전달하지 않는다.
        if (IsPointerOverUI())
        {
            isPointerPressed =
                false;

            return;
        }

        // 실제 WorldMap Node 위에서 시작된 입력만
        // 노드 클릭 후보로 저장한다.
        isPointerPressed =
            true;
    }


    // 노드를 누른 상태에서 두 번째 손가락이 추가되면
    // 단일 노드 클릭이 아니라 카메라 Pinch / Drag 제스처로 전환된 것으로 판단한다.
    //
    // 첫 번째 손가락으로 노드를 누른 뒤
    // 두 번째 손가락을 추가하는 경우에도
    // 이후 Node 진입이 발생하지 않도록 클릭 상태를 취소한다.
    private void OnMouseDrag()
    {
        // <변경부분> PC Drag는 WorldMapCameraController가 담당한다.
        if (Application.isMobilePlatform == false)
        {
            return;
        }
        if (isPointerPressed == false)
        {
            return;
        }

        if (Input.touchCount >= 2)
        {
            isPointerPressed =
                false;
        }
    }


    private void OnMouseUpAsButton()
    {
        // <변경부분> PC Node Click은 WorldMapCameraController가
        // Mouse Up 시점에 직접 확정한다.
        if (Application.isMobilePlatform == false)
        {
            isPointerPressed =
                false;

            return;
        }
        if (isPointerPressed == false)
        {
            return;
        }

        // 먼저 클릭 상태를 초기화하여
        // 이후 어떤 경로로 return되더라도 상태가 남지 않도록 한다.
        isPointerPressed =
            false;

        // Touch 종료 시점까지 두 손가락 입력이 유지되고 있다면
        // 카메라 제스처이므로 Node 진입을 실행하지 않는다.
        if (Input.touchCount >= 2)
        {
            return;
        }

        // 입력 종료 위치가 UI 위라면
        // 뒤쪽 Node 진입을 실행하지 않는다.
        if (IsPointerOverUI())
        {
            return;
        }

        EnterNode();
    }


    // <변경부분> MapNodeRuntime과 WorldMapCameraController가
    // 동일한 UI Raycast 판정을 공유하도록 공개한다.
    public static bool IsPointerOverUI()
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            return false;
        }

        // 모바일 Touch가 존재하면
        // 현재 활성화된 모든 손가락의 실제 화면 위치를 검사한다.
        if (Input.touchCount > 0)
        {
            for (int i = 0;
                 i < Input.touchCount;
                 i++)
            {
                Touch touch =
                    Input.GetTouch(i);

                if (IsScreenPositionOverUI(
                    eventSystem,
                    touch.position))
                {
                    return true;
                }
            }
        }

        // PC Mouse 또는 Unity가 Mouse Pointer로 변환한 입력도
        // 실제 화면 위치를 기준으로 검사한다.
        if (IsScreenPositionOverUI(
            eventSystem,
            Input.mousePosition))
        {
            return true;
        }

        // 기존 EventSystem 판정도 마지막 보조 안전장치로 유지한다.
        return
            eventSystem.IsPointerOverGameObject();
    }


    // 지정된 화면 좌표에서
    // Unity UI Graphic이 실제로 Raycast되는지 확인한다.
    private static bool IsScreenPositionOverUI(
        EventSystem eventSystem,
        Vector2 screenPosition)
    {
        if (eventSystem == null)
        {
            return false;
        }

        PointerEventData pointerEventData =
            new PointerEventData(
                eventSystem
            );

        pointerEventData.position =
            screenPosition;

        pointerRaycastResults.Clear();

        // 현재 화면 위치에서 EventSystem에 등록된
        // 모든 Raycaster를 대상으로 Raycast를 수행한다.
        eventSystem.RaycastAll(
            pointerEventData,
            pointerRaycastResults
        );

        bool isOverUI =
            false;

        for (int i = 0;
             i < pointerRaycastResults.Count;
             i++)
        {
            RaycastResult raycastResult =
                pointerRaycastResults[i];

            // GraphicRaycaster로 검출된 대상만 UI로 판단한다.
            //
            // PhysicsRaycaster / Physics2DRaycaster로 검출되는
            // WorldMap Node 등의 월드 오브젝트는 여기서 제외한다.
            if (raycastResult.module
                is UnityEngine.UI.GraphicRaycaster)
            {
                isOverUI =
                    true;

                break;
            }
        }

        // 다음 입력 판정에서 이전 결과가 남지 않도록 초기화한다.
        pointerRaycastResults.Clear();

        return
            isOverUI;
    }

    // 현재 노드 진입을 월드맵 진행 컨트롤러에 요청한다.
    //
    // 씬을 즉시 불러오지 않고,
    // 검은 구체가 노드까지 이동한 뒤
    // 미클리어 노드만 전투 또는 이벤트 씬으로 진입한다.
    public void EnterNode()
    {
        // 잠긴 미클리어 노드는 이동할 수 없다.
        //
        // 클리어 노드는 과거에 방문한 지역이므로
        // 현재 해금값과 관계없이 되돌아갈 수 있도록 허용한다.
        if (isUnlocked == false &&
            isCleared == false)
        {
            Debug.Log(
                $"맵 노드 진입 불가: " +
                $"{nodeDisplayName} 노드는 아직 잠겨 있습니다."
            );

            return;
        }

        if (WorldMapProgressController.Instance == null)
        {
            Debug.LogWarning(
                "맵 노드 진입 실패: " +
                "WorldMapProgressController가 씬에 없습니다."
            );

            return;
        }

        // 미클리어 노드는 실제 씬 진입 대상이므로
        // Target Scene Name이 반드시 필요하다.
        //
        // 이미 클리어된 노드는 마커 위치만 이동하므로
        // Target Scene Name이 비어 있어도 정상 처리한다.
        if (isCleared == false &&
    string.IsNullOrWhiteSpace(
        targetSceneName))
        {
            Debug.LogWarning(
                $"맵 노드 진입 실패: " +
                $"{nodeDisplayName} 노드의 Target Scene Name이 비어 있습니다."
            );

            return;
        }

        // Battle / BossBattle 노드는
        // BattleScene과 함께 실제 StageBattleData가 반드시 필요하다.
        //
        // Event, Shop 등 전투가 아닌 노드는
        // StageBattleData가 없어도 정상 진입할 수 있다.
        MapNodeType currentNodeType =
            GetNodeType();

        bool requiresBattleStageData =
            currentNodeType == MapNodeType.Battle ||
            currentNodeType == MapNodeType.BossBattle;

        if (isCleared == false &&
            requiresBattleStageData &&
            stageBattleData == null)
        {
            Debug.LogWarning(
                $"맵 노드 진입 실패: " +
                $"{nodeDisplayName} 노드에 Stage Battle Data가 연결되지 않았습니다."
            );

            return;
        }

        // 실제 연결 관계 검사, 마커 이동,
        // 클리어 노드 재방문 또는 신규 노드 씬 전환은
        // WorldMapProgressController가 담당한다.
        WorldMapProgressController.Instance
            .TryMoveToNode(
                this
            );
    }

    // 외부 진행 시스템에서 노드 해금 상태를 변경한다.
    public void SetUnlocked(
     bool unlocked)
    {
        isUnlocked =
            unlocked;

        // 잠긴 노드는 더 이상 선택 가능한 후보가 아니므로
        // 실행 중인 Pulse를 즉시 종료한다.
        if (isUnlocked == false)
        {
            StopSelectablePulse();
        }
    }

    // 기존 WorldMapBuilder의 1개 인자 호출과 호환을 유지한다.
    // 초기 생성 단계에서는 클리어 상태만 먼저 저장한다.
    public void SetCleared(
    bool cleared)
    {
        isCleared =
            cleared;

        // 클리어된 노드는 다음 신규 이동 후보가 아니므로
        // 선택 가능 Pulse를 종료한다.
        if (isCleared)
        {
            StopSelectablePulse();
        }
    }

    // 외부 진행 시스템에서 노드 클리어 상태를 변경한다.
    //
    // 노드가 클리어 상태가 됐다면
    // 전달받은 Cleared 전용 스타일로 Sprite와 Collider를 변경한다.
    public void SetCleared(
        bool cleared,
        MapNodeStyleData clearedStyleData)
    {
        isCleared =
     cleared;

        if (isCleared == false)
        {
            return;
        }

        // 클리어 상태가 되는 순간
        // 선택 가능한 다음 노드 Pulse를 즉시 종료하고 원래 크기로 복원한다.
        StopSelectablePulse();

        if (clearedStyleData == null)
        {
            Debug.LogWarning(
                $"클리어 노드 스타일 적용 실패: " +
                $"{nodeDisplayName} 노드에 사용할 " +
                $"Cleared Style Data가 연결되지 않았습니다."
            );

            return;
        }

        // 기존 전투·이벤트 스타일을
        // Cleared 전용 스타일로 교체한다.
        nodeStyleData =
            clearedStyleData;

        // 변경된 Cleared Sprite, 색상,
        // Collider 설정을 실제 노드에 즉시 반영한다.
        ApplyStyle();
    }
    // 맵 진행도 저장에 사용할 노드 ID를 반환한다.
    public string GetNodeId()
    {
        return nodeId;
    }

    // Inspector와 로그에 표시할 노드 이름을 반환한다.
    public string GetNodeDisplayName()
    {
        return nodeDisplayName;
    }

    public string GetTargetSceneName()
    {
        return targetSceneName;
    }

    // 현재 노드에 연결된 전투 스테이지 데이터를 반환한다.
    //
    // Battle / BossBattle 노드에서는
    // BattleSetupManager로 전달될 실제 전투 데이터다.
    public StageBattleData GetStageBattleData()
    {
        return stageBattleData;
    }

    // 현재 노드에 설정된
    // 연결 대상 Node ID와 Route 데이터를 반환한다.
    //
    // 원본 런타임 리스트가 외부에서 직접 수정되지 않도록
    // Connection과 Route 좌표를 모두 복사해서 반환한다.
    public List<MapNodeConnectionData> GetConnectionsCopy()
    {
        return CopyConnections(
            connections
        );
    }

    // 현재 노드에서 지정한 목적지 노드로 이어지는
    // Connection 데이터를 찾는다.
    public MapNodeConnectionData GetConnectionToNode(
        string targetNodeId)
    {
        if (connections == null ||
            string.IsNullOrWhiteSpace(
                targetNodeId))
        {
            return null;
        }

        string normalizedTargetNodeId =
            targetNodeId.Trim();

        for (int i = 0;
             i < connections.Count;
             i++)
        {
            MapNodeConnectionData connection =
                connections[i];

            if (connection == null ||
                string.IsNullOrWhiteSpace(
                    connection.targetNodeId))
            {
                continue;
            }

            if (connection.targetNodeId.Trim() ==
                normalizedTargetNodeId)
            {
                return connection;
            }
        }

        return null;
    }

    // 현재 노드가 해금 상태인지 반환한다.
    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    // 현재 노드가 클리어 상태인지 반환한다.
    public bool IsCleared()
    {
        return isCleared;
    }



    // 현재 노드의 실제 게임플레이 역할을 반환한다.
    //
    // Visual Style이 Cleared Style 등으로 변경되어도
    // Runtime에 저장된 원래 Node Type은 변경되지 않는다.
    public MapNodeType GetNodeType()
    {
        return nodeType;
    }
}
