using UnityEngine;

// <변경부분> 튜토리얼 / 이벤트에서
// 플레이어가 눌러야 할 기물, 타일, UI 버튼을
// 하나의 공용 마커로 가리키는 컨트롤러
//
// EventSequenceStepData에는 실제 Transform을 저장하지 않고
// 좌표 또는 EventSequenceButtonType만 저장한다.
//
// EventMarkerUI가 런타임에 실제 대상을 찾아
// 동일한 마커 오브젝트를 이동시켜 재사용한다.
public class EventMarkerUI : MonoBehaviour
{
    [Header("Marker")]
    // 실제 화면에 표시할 마커 RectTransform
    //
    // 화살표, 손가락, 픽셀 아이콘 등
    // 원하는 Image 오브젝트를 연결한다.
    [SerializeField]
    private RectTransform markerRectTransform;

    // 마커가 위치할 UI 부모
    //
    // 보통 EventGuideCanvas 내부의
    // 전체 화면 Stretch RectTransform을 사용한다.
    [SerializeField]
    private RectTransform markerParent;

    [Header("Canvas")]
    // 마커가 존재하는 Canvas
    [SerializeField]
    private Canvas markerCanvas;

    [Header("Managers")]
    // 보드 좌표로 Tile을 찾기 위한 매니저
    [SerializeField]
    private BoardManager boardManager;

    // 보드 좌표로 Piece를 찾기 위한 매니저
    [SerializeField]
    private PieceManager pieceManager;

    // ForceButton의 실제 UI RectTransform을 찾기 위한 컨트롤러
    [SerializeField]
    private BattleUIController battleUIController;

    [Header("World Target")]
    // 월드에 존재하는 기물 / 타일을
    // 화면 좌표로 변환할 때 사용할 카메라
    //
    // 비워두면 Camera.main을 사용한다.
    [SerializeField]
    private Camera worldCamera;

    [Header("Position")]
    // 타겟 화면 위치에서 마커를 얼마나 이동시킬지 설정
    //
    // 예:
    // (0, 35)라면 대상보다 35px 위에 표시
    [SerializeField]
    private Vector2 screenPositionOffset =
        new Vector2(0f, 35f);

    [Header("Float Animation")]
    // 마커가 위아래로 움직이는 거리
    [SerializeField, Min(0f)]
    private float floatDistance =
        6f;

    // 한 번 위아래로 움직이는 속도
    [SerializeField, Min(0.01f)]
    private float floatSpeed =
        2.5f;

    // 현재 추적 중인 월드 Transform
    private Transform worldTarget;

    // 현재 추적 중인 UI RectTransform
    private RectTransform uiTarget;

    // 현재 월드 타겟을 사용 중인지 확인
    private bool isWorldTarget =
        false;

    // 마커가 표시되기 시작한 시간
    private float showStartTime =
        0f;

    private void Awake()
    {
        AutoBindReferences();

        // <변경부분> EventMarkerUI가 붙어 있는 MarkerLayer가 아니라
        // 실제 MarkerImage만 초기 비활성화한다.
        if (markerRectTransform != null)
        {
            markerRectTransform.gameObject.SetActive(
                false
            );
        }
    }

    private void LateUpdate()
    {
        if (markerRectTransform == null ||
            markerRectTransform.gameObject.activeSelf == false)
        {
            return;
        }

        Vector2 targetScreenPosition;

        if (isWorldTarget)
        {
            if (worldTarget == null)
            {
                Hide();
                return;
            }

            Camera targetCamera =
                worldCamera != null
                    ? worldCamera
                    : Camera.main;

            if (targetCamera == null)
            {
                return;
            }

            targetScreenPosition =
                targetCamera.WorldToScreenPoint(
                    worldTarget.position
                );
        }
        else
        {
            if (uiTarget == null ||
                uiTarget.gameObject.activeInHierarchy == false)
            {
                Hide();
                return;
            }

            targetScreenPosition =
                GetUIScreenPosition(
                    uiTarget
                );
        }

        // <변경부분> 마커가 너무 정적으로 보이지 않도록
        // unscaledTime 기준 위아래 부유 연출을 추가한다.
        float floatOffset =
            Mathf.Sin(
                (Time.unscaledTime - showStartTime) *
                floatSpeed
            ) *
            floatDistance;

        Vector2 finalScreenPosition =
            targetScreenPosition +
            screenPositionOffset +
            new Vector2(
                0f,
                floatOffset
            );

        ApplyScreenPosition(
            finalScreenPosition
        );
    }

    // <변경부분> 지정 좌표의 특정 Team 기물을 찾아
    // 마커가 해당 기물을 계속 따라가도록 한다.
    public bool ShowForPiece(
        Vector2Int boardPosition,
        PieceTeam pieceTeam)
    {
        if (pieceManager == null)
        {
            Debug.LogWarning(
                "이벤트 마커 표시 실패: " +
                "PieceManager가 연결되지 않았습니다."
            );

            Hide();
            return false;
        }

        Piece targetPiece =
            pieceManager.GetPieceAt(
                boardPosition.x,
                boardPosition.y
            );

        if (targetPiece == null)
        {
            Debug.LogWarning(
                $"이벤트 마커 표시 실패: " +
                $"{boardPosition}에 기물이 없습니다."
            );

            Hide();
            return false;
        }

        if (targetPiece.Team !=
            pieceTeam)
        {
            Debug.LogWarning(
                $"이벤트 마커 표시 실패: " +
                $"{boardPosition}의 기물은 " +
                $"{targetPiece.Team} 진영이며, " +
                $"설정 대상은 {pieceTeam}입니다."
            );

            Hide();
            return false;
        }

        return
            ShowWorldTarget(
                targetPiece.transform
            );
    }

    // <변경부분> 지정 좌표의 Tile을 찾아
    // 해당 타일을 마커가 가리키도록 한다.
    public bool ShowForTile(
        Vector2Int boardPosition)
    {
        if (boardManager == null)
        {
            Debug.LogWarning(
                "이벤트 마커 표시 실패: " +
                "BoardManager가 연결되지 않았습니다."
            );

            Hide();
            return false;
        }

        Tile targetTile =
            boardManager.GetTile(
                boardPosition.x,
                boardPosition.y
            );

        if (targetTile == null)
        {
            Debug.LogWarning(
                $"이벤트 마커 표시 실패: " +
                $"{boardPosition}의 Tile을 찾지 못했습니다."
            );

            Hide();
            return false;
        }

        return
            ShowWorldTarget(
                targetTile.transform
            );
    }

    // <변경부분> EventSequenceButtonType에 맞는
    // 실제 전투 UI RectTransform을 찾아 마커를 표시한다.
    public bool ShowForButton(
        EventSequenceButtonType buttonType)
    {
        if (battleUIController == null)
        {
            Debug.LogWarning(
                "이벤트 버튼 마커 표시 실패: " +
                "BattleUIController가 연결되지 않았습니다."
            );

            Hide();
            return false;
        }

        RectTransform targetRectTransform =
            battleUIController
                .GetEventButtonMarkerTarget(
                    buttonType
                );

        if (targetRectTransform == null)
        {
            Debug.LogWarning(
                $"이벤트 버튼 마커 표시 실패: " +
                $"{buttonType}의 UI 대상을 찾지 못했습니다."
            );

            Hide();
            return false;
        }

        uiTarget =
            targetRectTransform;

        worldTarget =
            null;

        isWorldTarget =
            false;

        ShowMarkerObject();

        return true;
    }

    // <변경부분> 월드 Transform을 공용 방식으로
    // 마커 추적 대상으로 설정한다.
    private bool ShowWorldTarget(
        Transform targetTransform)
    {
        if (targetTransform == null)
        {
            Hide();
            return false;
        }

        worldTarget =
            targetTransform;

        uiTarget =
            null;

        isWorldTarget =
            true;

        ShowMarkerObject();

        return true;
    }

    // 마커 활성화 및 부유 애니메이션 시간 초기화
    private void ShowMarkerObject()
    {
        AutoBindReferences();

        // <변경부분> 실제 MarkerImage 참조가 없으면
        // 마커를 표시하지 않고 원인을 Console에 남긴다.
        if (markerRectTransform == null)
        {
            Debug.LogWarning(
                "이벤트 마커 표시 실패: " +
                "MarkerImage RectTransform을 찾지 못했습니다."
            );

            return;
        }

        if (markerParent == null)
        {
            Debug.LogWarning(
                "이벤트 마커 표시 실패: " +
                "Marker Parent를 찾지 못했습니다."
            );

            return;
        }

        if (markerCanvas == null)
        {
            Debug.LogWarning(
                "이벤트 마커 표시 실패: " +
                "Marker Canvas를 찾지 못했습니다."
            );

            return;
        }

        // <변경부분> 다른 UI 뒤에 가려지지 않도록
        // 실제 MarkerImage를 형제 목록의 마지막으로 이동시킨다.
        markerRectTransform.SetAsLastSibling();

        showStartTime =
            Time.unscaledTime;

        markerRectTransform.gameObject.SetActive(
            true
        );

        Debug.Log(
            $"이벤트 마커 표시 시작: " +
            $"{markerRectTransform.name}"
        );
    }

    // <변경부분> 현재 마커와 모든 타겟 참조를 초기화한다.
    public void Hide()
    {
        worldTarget =
            null;

        uiTarget =
            null;

        isWorldTarget =
            false;

        if (markerRectTransform != null)
        {
            markerRectTransform.gameObject.SetActive(
                false
            );
        }
    }

    // <변경부분> UI RectTransform의 중심 위치를
    // 현재 Canvas에 사용할 화면 좌표로 변환한다.
    private Vector2 GetUIScreenPosition(
        RectTransform targetRectTransform)
    {
        if (targetRectTransform == null)
        {
            return Vector2.zero;
        }

        Canvas targetCanvas =
            targetRectTransform
                .GetComponentInParent<Canvas>();

        Camera targetCamera =
            null;

        if (targetCanvas != null &&
            targetCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
        {
            targetCamera =
                targetCanvas.worldCamera;
        }

        return
            RectTransformUtility.WorldToScreenPoint(
                targetCamera,
                targetRectTransform.position
            );
    }

    // <변경부분> 화면 좌표를 마커 부모의
    // 로컬 UI 좌표로 변환하여 실제 마커 위치에 적용한다.
    private void ApplyScreenPosition(
        Vector2 screenPosition)
    {
        if (markerRectTransform == null ||
            markerParent == null)
        {
            return;
        }

        Camera markerUICamera =
            null;

        if (markerCanvas != null &&
            markerCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
        {
            markerUICamera =
                markerCanvas.worldCamera;
        }

        Vector2 localPosition;

        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    markerParent,
                    screenPosition,
                    markerUICamera,
                    out localPosition
                );

        if (converted == false)
        {
            return;
        }

        markerRectTransform.anchoredPosition =
            localPosition;
    }

    // Inspector 연결 누락 시
    // 현재 오브젝트 기준으로 기본 참조를 자동 보정한다.
    private void AutoBindReferences()
    {
        // <변경부분> 실제 마커 이미지는
        // EventMarkerUI가 붙어 있는 MarkerLayer 자체가 아니라
        // 그 아래의 MarkerImage를 사용한다.
        //
        // Inspector 연결이 빠진 경우에도
        // 첫 번째 자식 RectTransform을 자동으로 찾는다.
        if (markerRectTransform == null)
        {
            for (int i = 0;
                 i < transform.childCount;
                 i++)
            {
                RectTransform childRectTransform =
                    transform.GetChild(i)
                        as RectTransform;

                if (childRectTransform == null)
                {
                    continue;
                }

                markerRectTransform =
                    childRectTransform;

                break;
            }
        }

        // <변경부분> 마커 부모는
        // 실제 MarkerImage의 부모인 MarkerLayer를 사용한다.
        if (markerParent == null &&
            markerRectTransform != null)
        {
            markerParent =
                markerRectTransform.parent
                    as RectTransform;
        }

        if (markerCanvas == null)
        {
            markerCanvas =
                GetComponentInParent<Canvas>();
        }

        if (worldCamera == null)
        {
            worldCamera =
                Camera.main;
        }
    }

    private void OnDisable()
    {
        worldTarget =
            null;

        uiTarget =
            null;

        isWorldTarget =
            false;
    }
}
