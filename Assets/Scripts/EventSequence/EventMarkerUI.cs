using UnityEngine;

// <변경부분> 튜토리얼 / 이벤트에서
// 플레이어가 눌러야 할 기물, 타일, UI 버튼을
// World Marker 또는 UI Marker로 가리키는 공용 컨트롤러.
//
// World Marker:
// - SpriteRenderer 기반
// - 기물 / 타일 등 월드 대상에 사용
// - 카메라 Zoom에 따라 보드와 함께 확대 / 축소
//
// UI Marker:
// - RectTransform / Image 기반
// - 전투 버튼 등 Canvas 대상에 사용
// - 화면 UI 크기를 유지
public class EventMarkerUI : MonoBehaviour
{
    [Header("World Marker")]
    // <변경부분> 월드 공간에 표시할 실제 마커 오브젝트.
    //
    // WorldRoot 아래에 별도 GameObject를 만들고
    // SpriteRenderer를 연결한다.
    [SerializeField]
    private Transform worldMarkerTransform;

    // <변경부분> 보드 / 기물과 동일한 확대·축소 좌표계를
    // 사용하기 위한 WorldRoot.
    //
    // 데보리아의 Zoom은 Camera 크기를 변경하는 방식이 아니라
    // WorldRoot의 Scale을 변경하므로,
    // World Marker의 위치 Offset 역시 이 Root의 로컬 좌표계에서
    // 계산해야 기물 / 타일과 동일하게 확대·축소된다.
    [SerializeField]
    private Transform worldMarkerRoot;

    [Header("UI Marker")]
    // <변경부분> Canvas에 표시할 UI 마커 RectTransform.
    [SerializeField]
    private RectTransform uiMarkerRectTransform;

    // UI 마커가 움직일 기준 부모.
    //
    // 보통 MarkerLayer 전체 화면 RectTransform을 연결한다.
    [SerializeField]
    private RectTransform uiMarkerParent;

    // UI 마커가 존재하는 Canvas.
    [SerializeField]
    private Canvas uiMarkerCanvas;

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

    [Header("Camera")]
    // World 대상을 UI Marker로 표시해야 하는 경우
    // 화면 좌표 변환에 사용할 카메라.
    //
    // 비워두면 Camera.main 사용.
    [SerializeField]
    private Camera worldCamera;

    [Header("Float Animation")]
    // <변경부분> World Marker의 상하 부유 거리.
    //
    // 월드 좌표 단위이므로 작은 값을 사용한다.
    [SerializeField, Min(0f)]
    private float worldFloatDistance =
        0.08f;

    // <변경부분> UI Marker의 상하 부유 거리.
    //
    // Canvas 좌표 단위.
    [SerializeField, Min(0f)]
    private float uiFloatDistance =
        6f;

    // World / UI 공통 부유 속도
    [SerializeField, Min(0.01f)]
    private float floatSpeed =
        2.5f;

    // 현재 마커가 따라가고 있는 월드 대상
    private Transform worldTarget;

    // 현재 마커가 따라가고 있는 UI 대상
    private RectTransform uiTarget;

    // <변경부분> 현재 표시 중인 마커 방식
    private EventMarkerDisplayType currentDisplayType =
        EventMarkerDisplayType.World;

    // <변경부분> 현재 Event Step에서 전달받은
    // 마커 전용 위치 Offset
    private Vector2 currentPositionOffset =
        Vector2.zero;

    // 마커 표시 시작 시간
    private float showStartTime =
        0f;

    private void Awake()
    {
        AutoBindReferences();

        // <변경부분> 시작 시 World / UI 마커를
        // 모두 숨긴 상태로 초기화한다.
        Hide();
    }

    private void LateUpdate()
    {
        if (currentDisplayType ==
            EventMarkerDisplayType.World)
        {
            UpdateWorldMarker();

            return;
        }

        UpdateUIMarker();
    }

    // <변경부분> ForcePieceSelect의 지정 기물을 확인한 뒤,
    // World Marker인 경우에는 기물 Transform이 아니라
    // 해당 기물이 위치한 실제 Board Tile을 기준점으로 사용한다.
    //
    // Piece Transform에는 PieceManager의 pieceYOffset 등
    // 기물 표시용 위치 보정이 이미 들어가 있으므로,
    // Event Sequence의 논리 보드 좌표와 정확히 대응시키려면
    // BoardManager.GetTile(x, y)를 기준으로 삼는 것이 안전하다.
    public bool ShowForPiece(
        Vector2Int boardPosition,
        PieceTeam pieceTeam,
        EventMarkerDisplayType displayType,
        Vector2 positionOffset)
    {
        EnsureControllerActive();

        if (pieceManager == null)
        {
            Debug.LogWarning(
                "이벤트 마커 표시 실패: " +
                "PieceManager가 연결되지 않았습니다."
            );

            Hide();

            return false;
        }

        if (boardManager == null)
        {
            Debug.LogWarning(
                "이벤트 마커 표시 실패: " +
                "BoardManager가 연결되지 않았습니다."
            );

            Hide();

            return false;
        }

        // <변경부분> 먼저 지정 보드 좌표에
        // 실제 기물이 존재하는지 확인한다.
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

        // 지정된 Team의 기물인지 확인한다.
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

        currentDisplayType =
            displayType;

        currentPositionOffset =
            positionOffset;

        // <변경부분> World Marker는
        // Piece의 시각적 Transform 위치가 아니라
        // Event 데이터에 입력한 동일한 (X,Y)의 Tile을 기준점으로 사용한다.
        //
        // 따라서:
        // Target Piece Position (2,1)
        // → Tile (2,1)
        // → Marker 기준 위치
        //
        // 가 정확하게 일치한다.
        if (displayType ==
            EventMarkerDisplayType.World)
        {
            Tile targetTile =
                boardManager.GetTile(
                    boardPosition.x,
                    boardPosition.y
                );

            if (targetTile == null)
            {
                Debug.LogWarning(
                    $"이벤트 World Marker 표시 실패: " +
                    $"{boardPosition}의 Tile을 찾지 못했습니다."
                );

                Hide();

                return false;
            }

            worldTarget =
                targetTile.transform;

            uiTarget =
                null;

            return ShowCurrentMarker();
        }

        // <변경부분> Piece를 UI Marker 방식으로 표시하는
        // 특수 Event Step에서는 기존처럼
        // 실제 Piece Transform을 화면 좌표로 변환한다.
        worldTarget =
            targetPiece.transform;

        uiTarget =
            null;

        return ShowCurrentMarker();
    }

    // <변경부분> ForceTileSelect의 지정 Tile을 찾아
    // Step에서 설정한 World / UI 방식으로 마커를 표시한다.
    public bool ShowForTile(
        Vector2Int boardPosition,
        EventMarkerDisplayType displayType,
        Vector2 positionOffset)
    {
        EnsureControllerActive();

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

        currentDisplayType =
            displayType;

        currentPositionOffset =
            positionOffset;

        worldTarget =
            targetTile.transform;

        uiTarget =
            null;

        return ShowCurrentMarker();
    }

    // <변경부분> ForceButton의 실제 UI 대상을 찾아
    // UI Marker를 표시한다.
    public bool ShowForButton(
        EventSequenceButtonType buttonType,
        EventMarkerDisplayType displayType,
        Vector2 positionOffset)
    {
        EnsureControllerActive();

        if (battleUIController == null)
        {
            Debug.LogWarning(
                "이벤트 버튼 마커 표시 실패: " +
                "BattleUIController가 연결되지 않았습니다."
            );

            Hide();

            return false;
        }

        // <변경부분> UI Button은 월드 좌표를 가지지 않으므로
        // World Marker로 표시할 수 없다.
        //
        // 데이터 설정 실수를 Console에서 바로 확인할 수 있도록
        // 경고 후 마커 표시를 생략한다.
        if (displayType !=
            EventMarkerDisplayType.UI)
        {
            Debug.LogWarning(
                $"이벤트 버튼 마커 표시 실패: " +
                $"{buttonType}은 UI 대상이므로 " +
                $"Marker Display Type을 UI로 설정해야 합니다."
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

        currentDisplayType =
            EventMarkerDisplayType.UI;

        currentPositionOffset =
            positionOffset;

        uiTarget =
            targetRectTransform;

        worldTarget =
            null;

        return ShowCurrentMarker();
    }

    // <변경부분> 현재 설정된 Display Type에 맞는
    // 실제 마커 오브젝트를 활성화한다.
    private bool ShowCurrentMarker()
    {
        AutoBindReferences();

        showStartTime =
            Time.unscaledTime;

        if (currentDisplayType ==
            EventMarkerDisplayType.World)
        {
            return ShowWorldMarker();
        }

        return ShowUIMarker();
    }

    // <변경부분> SpriteRenderer 기반 World Marker 표시
    private bool ShowWorldMarker()
    {
        if (worldTarget == null)
        {
            Debug.LogWarning(
                "이벤트 World Marker 표시 실패: " +
                "World Target이 없습니다."
            );

            Hide();

            return false;
        }

        if (worldMarkerTransform == null)
        {
            Debug.LogWarning(
                "이벤트 World Marker 표시 실패: " +
                "World Marker Transform이 연결되지 않았습니다."
            );

            Hide();

            return false;
        }

        if (worldMarkerRoot == null)
        {
            Debug.LogWarning(
                "이벤트 World Marker 표시 실패: " +
                "World Marker Root가 연결되지 않았습니다."
            );

            Hide();

            return false;
        }

        // <변경부분> World Marker는 WorldRoot의 Scale을 공유하도록
        // 동일한 부모 아래에 배치한다.
        //
        // 부모 변경 시 현재 World Position을 유지하여
        // 불필요한 좌표 재해석을 방지한다.
        if (worldMarkerTransform.parent !=
            worldMarkerRoot)
        {
            worldMarkerTransform.SetParent(
                worldMarkerRoot,
                true
            );
        }

        // UI Marker가 표시 중이었다면 먼저 숨긴다.
        if (uiMarkerRectTransform != null)
        {
            uiMarkerRectTransform.gameObject.SetActive(
                false
            );
        }

        worldMarkerTransform.gameObject.SetActive(
            true
        );

        // <변경부분> 활성화된 첫 프레임부터
        // 올바른 위치에 보이도록 즉시 위치를 갱신한다.
        ApplyWorldMarkerPosition(
            0f
        );

        Debug.Log(
            $"이벤트 World Marker 표시 시작: " +
            $"{worldMarkerTransform.name}"
        );

        return true;
    }

    // <변경부분> Canvas 기반 UI Marker 표시
    private bool ShowUIMarker()
    {
        if (uiMarkerRectTransform == null)
        {
            Debug.LogWarning(
                "이벤트 UI Marker 표시 실패: " +
                "UI Marker RectTransform이 연결되지 않았습니다."
            );

            Hide();

            return false;
        }

        if (uiMarkerParent == null)
        {
            Debug.LogWarning(
                "이벤트 UI Marker 표시 실패: " +
                "UI Marker Parent가 연결되지 않았습니다."
            );

            Hide();

            return false;
        }

        if (uiMarkerCanvas == null)
        {
            Debug.LogWarning(
                "이벤트 UI Marker 표시 실패: " +
                "UI Marker Canvas가 연결되지 않았습니다."
            );

            Hide();

            return false;
        }

        if (worldTarget == null &&
            uiTarget == null)
        {
            Debug.LogWarning(
                "이벤트 UI Marker 표시 실패: " +
                "추적할 대상이 없습니다."
            );

            Hide();

            return false;
        }

        // World Marker가 표시 중이었다면 먼저 숨긴다.
        if (worldMarkerTransform != null)
        {
            worldMarkerTransform.gameObject.SetActive(
                false
            );
        }

        // 다른 UI에 가려지지 않도록
        // UI Marker를 MarkerLayer의 마지막으로 이동한다.
        uiMarkerRectTransform.SetAsLastSibling();

        uiMarkerRectTransform.gameObject.SetActive(
            true
        );

        // 첫 프레임부터 대상 위에 표시되도록
        // 즉시 위치 갱신.
        ApplyUIMarkerPosition(
            0f
        );

        Debug.Log(
            $"이벤트 UI Marker 표시 시작: " +
            $"{uiMarkerRectTransform.name}"
        );

        return true;
    }

    // <변경부분> World Marker의 위치와
    // 부유 애니메이션을 갱신한다.
    private void UpdateWorldMarker()
    {
        if (worldMarkerTransform == null ||
            worldMarkerTransform.gameObject.activeSelf ==
                false)
        {
            return;
        }

        if (worldTarget == null)
        {
            Hide();

            return;
        }

        float floatOffset =
            Mathf.Sin(
                (Time.unscaledTime - showStartTime) *
                floatSpeed
            ) *
            worldFloatDistance;

        ApplyWorldMarkerPosition(
            floatOffset
        );
    }

    // <변경부분> World Marker 위치는
    // 실제 Tile / Piece Transform의 World Position을 그대로 사용한다.
    //
    // BoardManager가 생성한 Tile의 transform.position은
    // 현재 화면에 실제 표시되고 있는 정확한 위치이므로,
    // 이를 다시 WorldRoot Local 좌표로 변환하지 않는다.
    //
    // 대신 Event Step의 Offset과 Float Offset만
    // WorldRoot Scale에 맞게 World Vector로 변환하여 더한다.
    //
    // 결과:
    // - 지정 Tile의 실제 위치와 Marker 기준점이 정확히 일치
    // - WorldRoot 확대 / 축소 시 Marker Offset도 같은 비율로 확대 / 축소
    private void ApplyWorldMarkerPosition(
        float floatOffset)
    {
        if (worldMarkerTransform == null ||
            worldTarget == null)
        {
            return;
        }

        if (worldMarkerRoot == null)
        {
            Debug.LogWarning(
                "이벤트 World Marker 위치 갱신 실패: " +
                "World Marker Root가 연결되지 않았습니다."
            );

            return;
        }

        // <변경부분> 현재 대상의 실제 World Position을
        // 마커의 정확한 기준 위치로 사용한다.
        Vector3 targetWorldPosition =
            worldTarget.position;

        // <변경부분> Event Step에서 설정한 Offset과
        // World Marker의 부유 Offset은
        // WorldRoot의 확대 / 축소 비율을 따라가야 한다.
        Vector3 localOffset =
            new Vector3(
                currentPositionOffset.x,
                currentPositionOffset.y +
                    floatOffset,
                0f
            );

        // <변경부분> WorldRoot의 Scale을 반영한
        // 실제 World 공간 Offset으로 변환한다.
        Vector3 scaledWorldOffset =
            worldMarkerRoot.TransformVector(
                localOffset
            );

        // <변경부분> 실제 타일 위치에
        // 확대 / 축소가 반영된 Offset만 더한다.
        worldMarkerTransform.position =
            targetWorldPosition +
            scaledWorldOffset;
    }

    // <변경부분> UI Marker의 위치와
    // 부유 애니메이션을 갱신한다.
    private void UpdateUIMarker()
    {
        if (uiMarkerRectTransform == null ||
            uiMarkerRectTransform.gameObject.activeSelf ==
                false)
        {
            return;
        }

        if (worldTarget == null &&
            uiTarget == null)
        {
            Hide();

            return;
        }

        if (uiTarget != null &&
            uiTarget.gameObject.activeInHierarchy ==
                false)
        {
            Hide();

            return;
        }

        float floatOffset =
            Mathf.Sin(
                (Time.unscaledTime - showStartTime) *
                floatSpeed
            ) *
            uiFloatDistance;

        ApplyUIMarkerPosition(
            floatOffset
        );
    }

    // <변경부분> UI Marker는
    // World 대상이면 WorldToScreenPoint,
    // UI 대상이면 RectTransform 화면 좌표를 사용한다.
    private void ApplyUIMarkerPosition(
        float floatOffset)
    {
        if (uiMarkerRectTransform == null ||
            uiMarkerParent == null)
        {
            return;
        }

        Vector2 targetScreenPosition;

        if (worldTarget != null)
        {
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
            targetScreenPosition =
                GetUIScreenPosition(
                    uiTarget
                );
        }

        Vector2 finalScreenPosition =
            targetScreenPosition +
            currentPositionOffset +
            new Vector2(
                0f,
                floatOffset
            );

        ApplyUIScreenPosition(
            finalScreenPosition
        );
    }

    // UI RectTransform의 중심 위치를
    // 화면 좌표로 변환한다.
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

    // 화면 좌표를 UI Marker Parent의
    // 로컬 Canvas 좌표로 변환한다.
    private void ApplyUIScreenPosition(
        Vector2 screenPosition)
    {
        if (uiMarkerRectTransform == null ||
            uiMarkerParent == null)
        {
            return;
        }

        Camera markerUICamera =
            null;

        if (uiMarkerCanvas != null &&
            uiMarkerCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
        {
            markerUICamera =
                uiMarkerCanvas.worldCamera;
        }

        Vector2 localPosition;

        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    uiMarkerParent,
                    screenPosition,
                    markerUICamera,
                    out localPosition
                );

        if (converted == false)
        {
            return;
        }

        uiMarkerRectTransform.anchoredPosition =
            localPosition;
    }

    // <변경부분> EventMarkerUI가 비활성 상태에서 시작했더라도
    // 외부 EventSequenceController가 Show 함수를 호출하면
    // 추적 Update가 실행될 수 있도록 활성화한다.
    private void EnsureControllerActive()
    {
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(
                true
            );
        }
    }

    // Inspector 연결 누락 시
    // 필요한 참조를 안전하게 자동 보정한다.
    private void AutoBindReferences()
    {
        // <변경부분> World Marker Root가 비어 있고
        // World Marker가 이미 WorldRoot 아래에 있다면
        // 현재 부모를 Root로 자동 사용한다.
        //
        // 실제 Scene에서는 Inspector에서
        // WorldRoot를 직접 연결하는 것을 권장한다.
        if (worldMarkerRoot == null &&
            worldMarkerTransform != null)
        {
            worldMarkerRoot =
                worldMarkerTransform.parent;
        }

        // <변경부분> UI Marker가 직접 연결되지 않았다면
        // EventMarkerUI 아래 첫 번째 RectTransform 자식을 찾는다.
        if (uiMarkerRectTransform == null)
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

                uiMarkerRectTransform =
                    childRectTransform;

                break;
            }
        }

        // UI Marker의 부모 자동 연결
        if (uiMarkerParent == null &&
            uiMarkerRectTransform != null)
        {
            uiMarkerParent =
                uiMarkerRectTransform.parent
                    as RectTransform;
        }

        // Canvas 자동 연결
        if (uiMarkerCanvas == null)
        {
            uiMarkerCanvas =
                GetComponentInParent<Canvas>();
        }

        // World → UI 변환에 사용할 카메라 자동 연결
        if (worldCamera == null)
        {
            worldCamera =
                Camera.main;
        }
    }

    // <변경부분> 현재 World / UI 마커와
    // 모든 추적 상태를 한 번에 초기화한다.
    public void Hide()
    {
        worldTarget =
            null;

        uiTarget =
            null;

        currentDisplayType =
            EventMarkerDisplayType.World;

        currentPositionOffset =
            Vector2.zero;

        if (worldMarkerTransform != null)
        {
            worldMarkerTransform.gameObject.SetActive(
                false
            );
        }

        if (uiMarkerRectTransform != null)
        {
            uiMarkerRectTransform.gameObject.SetActive(
                false
            );
        }
    }

    private void OnDisable()
    {
        worldTarget =
            null;

        uiTarget =
            null;

        currentPositionOffset =
            Vector2.zero;

        if (worldMarkerTransform != null)
        {
            worldMarkerTransform.gameObject.SetActive(
                false
            );
        }

        if (uiMarkerRectTransform != null)
        {
            uiMarkerRectTransform.gameObject.SetActive(
                false
            );
        }
    }
}