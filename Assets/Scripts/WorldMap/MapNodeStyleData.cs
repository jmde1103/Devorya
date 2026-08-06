using UnityEngine;

// 노드 종류별 Sprite와 기본 표시 정보를 저장한다.
[CreateAssetMenu(
    fileName = "MapNodeStyleData",
    menuName = "Devorya/World Map/Node Style Data"
)]
public class MapNodeStyleData : ScriptableObject
{
    [Header("Node Type")]
    // 이 데이터가 나타내는 노드 종류
    [SerializeField]
    private MapNodeType nodeType =
        MapNodeType.Battle;

    [Header("Visual")]
    // 노드에 표시할 기본 Sprite
    [SerializeField]
    private Sprite nodeSprite;

    // 노드 Sprite의 기본 색상
    [SerializeField]
    private Color spriteColor =
        Color.white;

    [Header("Collider")]
    // 노드 클릭 판정에 사용할 Collider 크기
    //
    // Sprite 전체 크기보다 클릭 범위를 조금 작게 만들고 싶다면
    // 여기에서 직접 조절한다.
    [SerializeField]
    private Vector2 colliderSize =
        Vector2.one;

    // 노드 중심에서 Collider가 이동할 보정 위치
    [SerializeField]
    private Vector2 colliderOffset =
        Vector2.zero;

    // 외부에서 노드 종류를 확인한다.
    public MapNodeType NodeType
    {
        get { return nodeType; }
    }

    // 외부에서 노드 Sprite를 가져온다.
    public Sprite NodeSprite
    {
        get { return nodeSprite; }
    }

    // 외부에서 Sprite 색상을 가져온다.
    public Color SpriteColor
    {
        get { return spriteColor; }
    }

    // 외부에서 Collider 크기를 가져온다.
    public Vector2 ColliderSize
    {
        get { return colliderSize; }
    }

    // 외부에서 Collider 보정 위치를 가져온다.
    public Vector2 ColliderOffset
    {
        get { return colliderOffset; }
    }
}
