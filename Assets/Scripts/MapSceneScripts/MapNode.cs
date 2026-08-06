using UnityEngine;
using UnityEngine.SceneManagement;

// 맵 위에서 클릭할 수 있는 스테이지 노드를 관리한다.
[RequireComponent(typeof(Collider2D))]
public class MapNode : MonoBehaviour
{
    [Header("Node Identity")]
    // 노드를 구분하기 위한 고유 ID
    [SerializeField]
    private string nodeId;

    // Inspector와 로그에서 확인할 노드 이름
    [SerializeField]
    private string nodeDisplayName;

    [Header("Stage Scene")]
    // 노드 클릭 시 이동할 전투 씬 이름
    [SerializeField]
    private string targetSceneName;

    [Header("Node State")]
    // 현재 노드가 해금되어 클릭 가능한지 여부
    [SerializeField]
    private bool isUnlocked = true;

    // 이미 클리어한 노드인지 여부
    [SerializeField]
    private bool isCleared = false;

    [Header("Visual")]
    // 잠긴 상태에서 표시할 오브젝트
    [SerializeField]
    private GameObject lockedVisual;

    // 클리어 상태에서 표시할 오브젝트
    [SerializeField]
    private GameObject clearedVisual;

    // 현재 선택 가능한 진행 노드 표시
    [SerializeField]
    private GameObject availableVisual;

    private void Start()
    {
        // 씬 시작 시 현재 노드 상태에 맞게 외형을 갱신한다.
        RefreshVisual();
    }

    private void OnMouseUpAsButton()
    {
        // 잠긴 노드는 클릭해도 실행하지 않는다.
        if (isUnlocked == false)
        {
            Debug.Log(
                $"맵 노드 진입 불가: {nodeDisplayName}은 아직 잠겨 있습니다."
            );

            return;
        }

        // 이동할 씬 이름이 없으면 실행하지 않는다.
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning(
                $"맵 노드 씬 이동 실패: {nodeDisplayName}의 Target Scene Name이 비어 있습니다."
            );

            return;
        }

        Debug.Log(
            $"맵 노드 진입: {nodeDisplayName} → {targetSceneName}"
        );

        // 연결된 전투 또는 이벤트 씬으로 이동한다.
        SceneManager.LoadScene(
            targetSceneName
        );
    }

    // 외부 진행 시스템에서 노드 해금 상태를 변경할 때 사용한다.
    public void SetUnlocked(
        bool unlocked)
    {
        isUnlocked =
            unlocked;

        RefreshVisual();
    }

    // 외부 진행 시스템에서 노드 클리어 상태를 변경할 때 사용한다.
    public void SetCleared(
        bool cleared)
    {
        isCleared =
            cleared;

        RefreshVisual();
    }

    // 현재 노드 상태에 따라 잠금·클리어·진행 가능 표시를 갱신한다.
    private void RefreshVisual()
    {
        if (lockedVisual != null)
        {
            lockedVisual.SetActive(
                isUnlocked == false
            );
        }

        if (clearedVisual != null)
        {
            clearedVisual.SetActive(
                isCleared
            );
        }

        if (availableVisual != null)
        {
            availableVisual.SetActive(
                isUnlocked &&
                isCleared == false
            );
        }
    }
}
