using UnityEngine;

// <변경부분> 공통 PieceObject에서 Sprite 외형과 Spine 외형을 교체 관리하는 컨트롤러
public class PieceVisualController : MonoBehaviour
{
    [Header("Sprite Visual")]
    // 기존 PieceObject에 붙어 있던 SpriteRenderer
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Spine Visual")]
    // Spine Visual 프리팹이 생성될 부모 위치
    [SerializeField] private Transform spineVisualRoot;

    // 현재 생성된 Spine Visual 오브젝트
    private GameObject currentSpineVisualObject;

    // 현재 적용 중인 Spine Visual 프리팹
    private GameObject currentSpineVisualPrefab;

    // 현재 Spine 애니메이션 컨트롤러
    private PieceSpineAnimationController currentSpineAnimationController;

    public PieceSpineAnimationController CurrentSpineAnimationController => currentSpineAnimationController;

    private void Awake()
    {
        // 기존 SpriteRenderer 자동 탐색
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // SpineVisualRoot가 없으면 자동 생성
        if (spineVisualRoot == null)
        {
            Transform foundRoot = transform.Find("SpineVisualRoot");

            if (foundRoot != null)
            {
                spineVisualRoot = foundRoot;
            }
            else
            {
                GameObject rootObject = new GameObject("SpineVisualRoot");
                rootObject.transform.SetParent(transform, false);
                rootObject.transform.localPosition = Vector3.zero;
                rootObject.transform.localRotation = Quaternion.identity;
                rootObject.transform.localScale = Vector3.one;

                spineVisualRoot = rootObject.transform;
            }
        }
    }

    // <변경부분> PieceData 기준으로 Sprite 또는 Spine 외형을 적용
    public void ApplyVisual(PieceData pieceData, PieceTeam team, bool isAbsorbedPlayerVisual)
    {
        if (pieceData == null)
        {
            ApplySprite(null);
            return;
        }

        GameObject spineVisualPrefab = pieceData.GetSpineVisualPrefab(team, isAbsorbedPlayerVisual);

        if (spineVisualPrefab != null)
        {
            ApplySpineVisual(spineVisualPrefab);
            return;
        }

        Sprite spriteToApply = pieceData.GetSprite(team, isAbsorbedPlayerVisual);
        ApplySprite(spriteToApply);
    }

    // <변경부분> 기존 SpriteRenderer 방식으로 외형 적용
    private void ApplySprite(Sprite sprite)
    {
        ClearSpineVisual();

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.enabled = sprite != null;
    }

    // <변경부분> Spine Visual 프리팹을 생성해서 외형 적용
    private void ApplySpineVisual(GameObject spineVisualPrefab)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        // 이미 같은 Spine Visual 프리팹을 사용 중이면 새로 생성하지 않음
        if (currentSpineVisualObject != null && currentSpineVisualPrefab == spineVisualPrefab)
        {
            currentSpineVisualObject.SetActive(true);

            if (currentSpineAnimationController != null)
            {
                currentSpineAnimationController.PlayIdle();
            }

            return;
        }

        ClearSpineVisual();

        if (spineVisualRoot == null)
        {
            return;
        }

        currentSpineVisualPrefab = spineVisualPrefab;

        // 프리팹의 로컬 위치/스케일 세팅을 유지한 채 SpineVisualRoot 아래에 생성
        currentSpineVisualObject = Instantiate(spineVisualPrefab, spineVisualRoot, false);

        currentSpineAnimationController =
            currentSpineVisualObject.GetComponentInChildren<PieceSpineAnimationController>();

        if (currentSpineAnimationController != null)
        {
            currentSpineAnimationController.PlayIdle();
        }
    }

    // <변경부분> 현재 Spine Visual 제거
    private void ClearSpineVisual()
    {
        if (currentSpineVisualObject != null)
        {
            Destroy(currentSpineVisualObject);
        }

        currentSpineVisualObject = null;
        currentSpineVisualPrefab = null;
        currentSpineAnimationController = null;
    }

    // <변경부분> 현재 외형 렌더러의 정렬 순서 갱신
    public void SetSortingOrder(int sortingOrder)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (currentSpineAnimationController != null)
        {
            currentSpineAnimationController.SetSortingOrder(sortingOrder);
            return;
        }

        if (currentSpineVisualObject == null)
        {
            return;
        }

        Renderer[] renderers = currentSpineVisualObject.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = sortingOrder;
        }
    }
}