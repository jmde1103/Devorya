using UnityEngine;

// 맵 노드와 아이콘을 16×16 Grid 셀 중앙에 자동 정렬한다.
[ExecuteAlways]
public class MapGridSnap : MonoBehaviour
{
    [Header("Grid Reference")]
    // 노드를 정렬할 Unity Grid
    [SerializeField]
    private Grid targetGrid;

    [Header("Snap Settings")]
    // Inspector에서 위치를 변경할 때 자동으로 셀 중앙에 정렬할지 여부
    [SerializeField]
    private bool snapAutomatically = true;

    // Grid 셀 중앙에서 추가로 보정할 위치
    [SerializeField]
    private Vector3 positionOffset =
        Vector3.zero;

    private void OnValidate()
    {
        if (snapAutomatically == false)
        {
            return;
        }

        SnapToGrid();
    }

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid()
    {
        if (targetGrid == null)
        {
            return;
        }

        // 현재 월드 위치가 속한 Grid 셀을 찾는다.
        Vector3Int cellPosition =
            targetGrid.WorldToCell(
                transform.position
            );

        // 해당 셀의 정확한 월드 중심 위치를 가져온다.
        Vector3 cellCenterPosition =
            targetGrid.GetCellCenterWorld(
                cellPosition
            );

        // 셀 중심과 추가 보정값을 적용한다.
        transform.position =
            cellCenterPosition +
            positionOffset;
    }
}
