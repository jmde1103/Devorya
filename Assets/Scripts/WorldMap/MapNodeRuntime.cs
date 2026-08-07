using UnityEngine;

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

    [Header("Node Style")]
    // 노드 종류와 Sprite 정보를 담은 스타일 데이터
    [SerializeField]
    private MapNodeStyleData nodeStyleData;

    [Header("Stage Scene")]
    // 노드를 클릭했을 때 이동할 씬 이름
    [SerializeField]
    private string targetSceneName;

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

    // 현재 포인터가 노드 위에서 눌렸는지 확인한다.
    private bool isPointerPressed;

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
        MapNodeStyleData newStyleData,
        string newTargetSceneName,
        bool unlocked)
    {
        nodeId =
            newNodeId;

        nodeDisplayName =
            newDisplayName;

        nodeStyleData =
            newStyleData;

        targetSceneName =
            newTargetSceneName;

        isUnlocked =
            unlocked;

        ApplyStyle();
    }

    // 노드가 클릭 가능한 상태인지 반환한다.
    public bool CanEnterNode()
    {
        return
            isUnlocked &&
            string.IsNullOrWhiteSpace(
                targetSceneName
            ) == false;
    }

    private void OnMouseDown()
    {
        // 포인터가 노드 위에서 눌렸음을 저장한다.
        //
        // 이후 맵 드래그 기능을 추가할 때
        // 일정 거리 이상 드래그했다면 클릭을 취소하도록 확장한다.
        isPointerPressed =
            true;
    }

    private void OnMouseUpAsButton()
    {
        if (isPointerPressed == false)
        {
            return;
        }

        isPointerPressed =
            false;

        EnterNode();
    }

    // 현재 노드 진입을 월드맵 진행 컨트롤러에 요청한다.
    //
    // 씬을 즉시 불러오지 않고,
    // 검은 구체가 노드까지 이동한 뒤 전투 씬에 진입한다.
    public void EnterNode()
    {
        if (isUnlocked == false)
        {
            Debug.Log(
                $"맵 노드 진입 불가: " +
                $"{nodeDisplayName} 노드는 아직 잠겨 있습니다."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
                targetSceneName))
        {
            Debug.LogWarning(
                $"맵 노드 진입 실패: " +
                $"{nodeDisplayName} 노드의 Target Scene Name이 비어 있습니다."
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

        // 실제 이동 경로 검사, 검은 구체 이동,
        // 전투 씬 전환은 진행 컨트롤러가 담당한다.
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
    }

    // 기존 WorldMapBuilder의 1개 인자 호출과 호환을 유지한다.
    // 초기 생성 단계에서는 클리어 상태만 먼저 저장한다.
    public void SetCleared(
        bool cleared)
    {
        isCleared =
            cleared;
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

    // 노드에 연결된 전투 또는 이벤트 씬 이름을 반환한다.
    public string GetTargetSceneName()
    {
        return targetSceneName;
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



    // 현재 노드 종류를 반환한다.
    public MapNodeType GetNodeType()
    {
        // 스타일 데이터가 아직 연결되지 않았다면
        // 기본 노드 종류인 일반 전투로 반환한다.
        if (nodeStyleData == null)
        {
            return MapNodeType.Battle;
        }

        return nodeStyleData.NodeType;
    }
}
