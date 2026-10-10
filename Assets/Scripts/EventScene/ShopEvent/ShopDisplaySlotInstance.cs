
using UnityEngine;

// <변경부분>
// Background Editor에서 배치한 상점 상품의
// 위치 및 원본 Prefab을 보관하는 컴포넌트.
//
// 실제 BattleItemData / 판매 가격 / SOLD 상태는
// 여기에서 관리하지 않는다.
//
// 해당 데이터는 추후 WorldMap의 상점 목록을
// 전달받는 ShopController에서 관리한다.
[DisallowMultipleComponent]
public class ShopDisplaySlotInstance : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string slotId;

    [SerializeField, HideInInspector]
    private GameObject sourcePrefab;

    [SerializeField, HideInInspector]
    private int x;

    [SerializeField, HideInInspector]
    private int y;

    [SerializeField, HideInInspector]
    private int layerOffset;


    public string SlotId => slotId;
    public GameObject SourcePrefab => sourcePrefab;

    public int X => x;
    public int Y => y;

    public int LayerOffset => layerOffset;


    // <변경부분>
    // 배치된 상품 진열 슬롯의 메타데이터 초기화.
    //
    // SlotId는 저장/로드 후에도 동일하게 유지한다.
    public void Initialize(
        string id,
        GameObject prefab,
        int anchorX,
        int anchorY,
        int layer)
    {
        slotId = id;

        sourcePrefab = prefab;

        x = anchorX;
        y = anchorY;

        layerOffset = Mathf.Clamp(
            layer,
            -3,
            3
        );
    }
}

