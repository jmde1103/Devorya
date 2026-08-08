using UnityEngine;

// 월드맵 Route의 Waypoint 오브젝트를
// 가장 가까운 Grid 셀 중심에 자동으로 정렬한다.
//
// Scene View에서 Point 오브젝트를 직접 움직이면
// 해당 위치에서 가장 가까운 셀 중심으로 자동 스냅된다.
[ExecuteAlways]
public class WorldMapRoutePoint : MonoBehaviour
{
    [Header("Grid Reference")]
    // 월드맵의 셀 위치를 계산할 Unity Grid
    //
    // WorldMapBuilder에 연결된 Map Grid와
    // 동일한 Grid 오브젝트를 연결한다.
    [SerializeField]
    private Grid mapGrid;

    [Header("Snap Settings")]
    // Edit Mode에서 Point 위치가 변경되면
    // 가장 가까운 Grid 셀 중심으로 자동 정렬할지 여부
    [SerializeField]
    private bool snapToGridCenter =
        true;

    // Point의 Z 위치를 고정할 값
    //
    // 2D 월드맵에서는 일반적으로 0을 사용한다.
    [SerializeField]
    private float fixedZPosition =
        0f;

    [Header("Current Grid Position")]
    // 현재 Point가 위치한 Grid 셀 좌표
    //
    // Scene View에서 Point를 움직인 뒤
    // 어떤 셀에 배치됐는지 Inspector에서 확인할 수 있다.
    [SerializeField]
    private Vector2Int currentGridPosition;

    // 마지막으로 정렬된 월드 위치
    //
    // 같은 위치에서 OnValidate가 반복 실행되는 것을 방지한다.
    private Vector3 lastSnappedWorldPosition;

    // 현재 Point가 위치한 Grid 좌표를 반환한다.
    public Vector2Int CurrentGridPosition
    {
        get { return currentGridPosition; }
    }

    private void OnEnable()
    {
        // 컴포넌트가 활성화될 때 현재 위치를 기준으로
        // Grid 좌표와 셀 중심 위치를 처음 갱신한다.
        SnapToNearestGridCenter();
    }

    private void OnValidate()
    {
        if (snapToGridCenter == false)
        {
            UpdateCurrentGridPosition();

            return;
        }

        // Inspector 값이나 Transform 위치가 변경되면
        // 가장 가까운 Grid 셀 중심으로 다시 정렬한다.
        SnapToNearestGridCenter();
    }

    private void Update()
    {
        // 플레이 중에는 마커 이동 경로가 변경되지 않으므로
        // 자동 스냅 검사를 실행하지 않는다.
        if (Application.isPlaying)
        {
            return;
        }

        if (snapToGridCenter == false ||
            mapGrid == null)
        {
            return;
        }

        // Scene View의 이동 도구로 Point를 움직였을 때
        // Transform 변경을 감지해 즉시 셀 중심으로 정렬한다.
        if (transform.hasChanged == false)
        {
            return;
        }

        transform.hasChanged =
            false;

        SnapToNearestGridCenter();
    }

    // 현재 Point를 가장 가까운 Grid 셀 중심으로 이동한다.
    [ContextMenu("Snap To Nearest Grid Center")]
    public void SnapToNearestGridCenter()
    {
        if (mapGrid == null)
        {
            return;
        }

        // 현재 월드 위치가 포함된 Grid 셀을 찾는다.
        Vector3Int nearestCell =
            mapGrid.WorldToCell(
                transform.position
            );

        nearestCell.z =
            0;

        // 해당 셀의 정확한 월드 중심 위치를 가져온다.
        Vector3 snappedWorldPosition =
            mapGrid.GetCellCenterWorld(
                nearestCell
            );

        snappedWorldPosition.z =
            fixedZPosition;

        currentGridPosition =
            new Vector2Int(
                nearestCell.x,
                nearestCell.y
            );

        // 이미 같은 셀 중심에 위치했다면
        // Transform을 다시 갱신하지 않는다.
        if (Vector3.SqrMagnitude(
                transform.position -
                snappedWorldPosition) <=
            0.000001f)
        {
            lastSnappedWorldPosition =
                snappedWorldPosition;

            return;
        }

        transform.position =
            snappedWorldPosition;

        lastSnappedWorldPosition =
            snappedWorldPosition;

        transform.hasChanged =
            false;
    }

    // 자동 스냅을 끈 상태에서도
    // 현재 Point가 속한 Grid 좌표는 Inspector에 표시한다.
    private void UpdateCurrentGridPosition()
    {
        if (mapGrid == null)
        {
            return;
        }

        Vector3Int currentCell =
            mapGrid.WorldToCell(
                transform.position
            );

        currentGridPosition =
            new Vector2Int(
                currentCell.x,
                currentCell.y
            );
    }

    private void OnDrawGizmos()
    {
        if (mapGrid == null)
        {
            return;
        }

        Vector3Int currentCell =
            mapGrid.WorldToCell(
                transform.position
            );

        currentCell.z =
            0;

        Vector3 cellCenter =
            mapGrid.GetCellCenterWorld(
                currentCell
            );

        Vector3 cellSize =
            mapGrid.cellSize;

        cellCenter.z =
            fixedZPosition;

        // Scene View에서 Point가 들어간 셀의 범위를
        // 사각형 Gizmo로 표시한다.
        Gizmos.DrawWireCube(
            cellCenter,
            new Vector3(
                cellSize.x,
                cellSize.y,
                0f
            )
        );

        // 셀 중심 위치를 작은 점으로 표시해
        // Point가 정확히 중앙에 배치됐는지 확인할 수 있게 한다.
        Gizmos.DrawSphere(
            cellCenter,
            Mathf.Min(
                cellSize.x,
                cellSize.y
            ) *
            0.08f
        );
    }
}
