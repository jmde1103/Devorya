using System.Collections.Generic;
using UnityEngine;

// <변경부분>
// Event Scene의 기물 편성 관리 전용 보드 컨트롤러.
//
// 실제 타일 생성은 기존 BoardManager가 담당한다.
// 일반 BattleScene의 BoardManager에는 영향을 주지 않는다.
public class EventSceneFormationBoardController : MonoBehaviour
{
    [Header("Formation Board")]

    // 5x2 타일과 편성 기물을 담을 전용 Root.
    [SerializeField]
    private GameObject formationBoardRoot;

    // 기존 배틀 보드 생성 기능을 재사용한다.
    [SerializeField]
    private BoardManager boardManager;

    // <변경부분>
    // 기존 배틀의 기물 생성 및 런타임 데이터 복원 기능을 재사용한다.
    // 편성 전용 PieceManager 인스턴스를 연결한다.
    [SerializeField]
    private PieceManager pieceManager;

    [Header("Tile Type")]

    [SerializeField]
    private TileType tileTypeA = TileType.Metal;

    [SerializeField]
    private TileType tileTypeB = TileType.MetalDark;

    [Header("Board Position")]

    // <변경부분>
    // ON: Board Center Target을 기준으로 자동 중앙 정렬.
    // OFF: FormationBoardRoot의 Inspector 좌표를 그대로 유지.
    [SerializeField]
    private bool autoCenterBoard = false;

    // 편성 보드의 중앙을 배치할 위치.
    // Auto Center Board가 ON일 때만 사용한다.
    // 비어 있으면 월드 좌표 (0, 0)를 중앙으로 사용한다.
    [SerializeField]
    private Transform boardCenterTarget;

#if UNITY_EDITOR
    // <변경부분>
    // Editor에서 RunState에 기물 데이터가 없을 경우에만
    // Inspector에 지정한 PieceData를 테스트용으로 생성한다.
    //
    // 실제 RunState 데이터는 변경하지 않는다.
    [Header("Editor Test Pieces")]
    [SerializeField]
    private bool useTestPiecesWhenRunEmpty = true;

    [SerializeField]
    private List<PieceData> testPieceDataList =
        new List<PieceData>();
#endif

    private bool isBoardGenerated;

    // <변경부분>
    // 같은 이벤트에서 기물 프리팹이 중복 생성되지 않도록 관리한다.
    private bool isPlayerPiecesGenerated;

    private bool isFormationOpen;

    // =====================================================
    // Formation Selection
    // =====================================================

    // <변경부분>
    // 기존 배틀씬의 플레이어 스테이터스 UI를 재사용한다.
    // BattleUIController 없이 상태 표시 기능만 호출한다.
    [Header("Formation Selection")]
    [SerializeField]
    private PieceStatusUIController playerStatusUIController;

    // <변경부분>
    // 기물 선택 시 기존 배틀의 Tile Focus 기능을 사용한다.
    [SerializeField]
    private PixelCameraController pixelCameraController;

    // <변경부분>
    // 카메라 드래그와 기물 클릭을 구분할 최소 이동 거리.
    [SerializeField, Min(1f)]
    private float clickDragThresholdPixels = 12f;

    // 현재 배치 대상으로 선택된 플레이어 기물.
    private Piece selectedFormationPiece;

    // PC 클릭 상태.
    private bool pcClickCandidate;
    private bool pcWasDragged;
    private Vector2 pcPressScreenPosition;
    private Tile pcPressedTile;

    // 모바일 터치 상태.
    private bool touchClickCandidate;
    private int touchFingerId = -1;
    private Vector2 touchPressScreenPosition;
    private Tile touchPressedTile;

    public bool IsFormationOpen => isFormationOpen;

    public BoardManager BoardManager => boardManager;

    private void Awake()
    {
        // 이벤트 시작 전에는 편성 보드를 표시하지 않는다.
        CloseFormationBoard();
    }

    // <변경부분>
    // 편성 화면이 열려 있을 때만 기물 선택 입력을 처리한다.
    //
    // 실제 Camera Drag / Zoom은 기존 PixelCameraController가
    // 그대로 담당한다.
    private void Update()
    {
        if (!isFormationOpen ||
            formationBoardRoot == null ||
            !formationBoardRoot.activeInHierarchy)
        {
            return;
        }

        if (Application.isMobilePlatform)
        {
            HandleMobileFormationInput();
        }
        else
        {
            HandlePCFormationInput();
        }
    }

    // <변경부분>
    // ShowPlayerFormation Step에서 호출한다.
    public bool OpenFormationBoard()
    {
        if (isFormationOpen)
        {
            return true;
        }

        if (formationBoardRoot == null ||
     boardManager == null ||
     pieceManager == null)
        {
            Debug.LogError(
                "기물 편성 시작 실패: " +
                "Formation Board Root, BoardManager 또는 " +
                "PieceManager가 연결되지 않았습니다.",
                this
            );

            return false;
        }

        // <변경부분>
        // 편성 기물도 보드와 함께 표시/숨김 처리되어야 한다.
        if (!pieceManager.transform.IsChildOf(
                formationBoardRoot.transform))
        {
            Debug.LogError(
                "기물 편성 시작 실패: " +
                "PieceManager는 FormationBoardRoot 아래에 있어야 합니다.",
                this
            );

            return false;
        }

        // 컨트롤러는 비활성화할 보드 Root 외부에 있어야 한다.
        if (formationBoardRoot == gameObject ||
            transform.IsChildOf(formationBoardRoot.transform))
        {
            Debug.LogError(
                "기물 편성 보드 시작 실패: " +
                "Controller는 FormationBoardRoot 외부에 배치해야 합니다.",
                this
            );

            return false;
        }

        // 보드가 실제 편성 Root 안에 있어야
        // 중앙 정렬과 표시 전환이 정상 동작한다.
        if (!boardManager.transform.IsChildOf(
                formationBoardRoot.transform))
        {
            Debug.LogError(
                "기물 편성 보드 시작 실패: " +
                "BoardManager가 FormationBoardRoot의 자식이 아닙니다.",
                this
            );

            return false;
        }

        if (boardManager.Width != 5 ||
            boardManager.Height != 2)
        {
            Debug.LogError(
                "기물 편성 보드 시작 실패: " +
                "BoardManager 크기를 Width 5 / Height 2로 설정해야 합니다.",
                this
            );

            return false;
        }

        formationBoardRoot.SetActive(true);

        if (!isBoardGenerated)
        {
            // 기존 Battle Tile 생성 함수를 그대로 사용한다.
            boardManager.RebuildBoardByTileType(
                tileTypeA,
                tileTypeB
            );

            // 실제 10칸이 모두 생성되었는지 확인한다.
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    Tile tile = boardManager.GetTile(x, y);

                    if (tile != null &&
                        tile.transform.IsChildOf(
                            formationBoardRoot.transform))
                    {
                        continue;
                    }

                    Debug.LogError(
                        $"기물 편성 타일 생성 실패: ({x}, {y})",
                        this
                    );

                    CloseFormationBoard();
                    return false;
                }
            }

            // <변경부분>
            // Auto Center Board가 ON일 때만 중앙 정렬한다.
            // OFF이면 FormationBoardRoot의 Inspector 좌표를 유지한다.
            if (autoCenterBoard)
            {
                CenterFormationBoard();
            }

            isBoardGenerated = true;
        }

        // <변경부분>
        // RunState의 플레이어 기물을 우선 복원한다.
        // Editor에서 RunState가 비어 있는 경우에만
        // Inspector 테스트용 PieceData를 대신 사용한다.
        if (!isPlayerPiecesGenerated)
        {
            RunStateManager runState =
                RunStateManager.Instance;

            List<PlayerPieceRuntimeData> runtimePieces =
                runState != null
                    ? runState.GetPlayerPiecesCopy()
                    : new List<PlayerPieceRuntimeData>();

#if UNITY_EDITOR
            // <변경부분>
            // 테스트 기물은 실제 런 데이터가 없을 때만 사용한다.
            // 기존 RunState의 저장 목록은 수정하지 않는다.
            if (runtimePieces.Count == 0 &&
                useTestPiecesWhenRunEmpty &&
                testPieceDataList != null)
            {
                for (int i = 0;
                     i < testPieceDataList.Count &&
                     runtimePieces.Count < 10;
                     i++)
                {
                    PieceData pieceData =
                        testPieceDataList[i];

                    if (pieceData == null)
                    {
                        continue;
                    }

                    PlayerPieceRuntimeData testData =
                        PlayerPieceRuntimeData.CreateFromPieceData(
                            pieceData
                        );

                    if (testData != null)
                    {
                        runtimePieces.Add(testData);
                    }
                }

                if (runtimePieces.Count > 0)
                {
                    Debug.Log(
                        $"기물 편성 Editor 테스트 데이터 사용: " +
                        $"{runtimePieces.Count}개",
                        this
                    );
                }
            }
#endif

            if (runtimePieces.Count == 0)
            {
                Debug.LogWarning(
                    "기물 편성: 생성할 플레이어 기물이 없습니다. " +
                    "RunState 또는 Editor Test Pieces를 확인하세요.",
                    this
                );
            }
            else
            {
                if (runtimePieces.Count > 10)
                {
                    Debug.LogWarning(
                        "기물 편성: 보유 기물이 10개를 초과합니다. " +
                        "기존 배치 규칙에 따라 최대 10개를 생성합니다.",
                        this
                    );
                }

                // 기존 PieceManager의 기물 배열을 초기화한다.
                pieceManager.InitializePieceGrid();

                // 기존 전투 초기배치 및 기물 복원 기능 재사용.
                pieceManager.SpawnPlayerPiecesFromRuntimeData(
                    runtimePieces,
                    false
                );

                isPlayerPiecesGenerated = true;

                Debug.Log(
                    $"기물 편성 복원 요청 완료: " +
                    $"{runtimePieces.Count}개 / 5x2 Board",
                    this
                );
            }
        }

        isFormationOpen = true;

        // <변경부분>
        // 편성 화면이 열릴 때 이전 스테이터스 표시를 정리한다.
        if (playerStatusUIController != null)
        {
            playerStatusUIController.Clear();
        }

        Debug.Log(
            "기물 편성 보드 활성화 완료: 5x2 / 10 Tiles",
            this
        );

        return true;
    }

    // =====================================================
    // PC Selection Input
    // =====================================================

    // <변경부분>
    // 기존 카메라와 동일하게 클릭 시작/종료 타일이 같고
    // 드래그가 발생하지 않은 경우에만 선택한다.
    private void HandlePCFormationInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pcClickCandidate = false;
            pcWasDragged = false;
            pcPressedTile = null;

            // UI 버튼 위에서 시작한 클릭은 무시한다.
            if (Tile.IsPointerOverUI())
            {
                return;
            }

            pcClickCandidate = true;

            pcPressScreenPosition =
                Input.mousePosition;

            pcPressedTile =
                GetFormationTileAtScreenPosition(
                    pcPressScreenPosition
                );

            return;
        }

        if (!pcClickCandidate)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            if (Vector2.Distance(
                    pcPressScreenPosition,
                    Input.mousePosition) >=
                clickDragThresholdPixels)
            {
                pcWasDragged = true;
            }
        }

        if (!Input.GetMouseButtonUp(0))
        {
            return;
        }

        Tile pressedTile = pcPressedTile;

        bool isValidClick =
            !pcWasDragged &&
            pressedTile != null &&
            !Tile.IsPointerOverUI() &&
            Vector2.Distance(
                pcPressScreenPosition,
                Input.mousePosition) <
            clickDragThresholdPixels;

        pcClickCandidate = false;
        pcWasDragged = false;
        pcPressedTile = null;

        if (!isValidClick)
        {
            return;
        }

        Tile releasedTile =
            GetFormationTileAtScreenPosition(
                Input.mousePosition
            );

        if (releasedTile == pressedTile)
        {
            HandleFormationTileClick(
                releasedTile
            );
        }
    }

    // =====================================================
    // Mobile Selection Input
    // =====================================================

    // <변경부분>
    // 모바일에서는 한 손가락 Tap만 선택으로 처리한다.
    // 두 손가락 핀치 또는 드래그는 기존 카메라에 위임한다.
    private void HandleMobileFormationInput()
    {
        if (Input.touchCount == 0)
        {
            ResetFormationTouchInput();
            return;
        }

        // 멀티터치가 시작되면 기물 선택 후보를 취소한다.
        if (Input.touchCount != 1)
        {
            ResetFormationTouchInput();
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            ResetFormationTouchInput();

            if (Tile.IsScreenPositionOverUI(
                    touch.position))
            {
                return;
            }

            touchClickCandidate = true;
            touchFingerId = touch.fingerId;

            touchPressScreenPosition =
                touch.position;

            touchPressedTile =
                GetFormationTileAtScreenPosition(
                    touch.position
                );

            return;
        }

        if (!touchClickCandidate ||
            touch.fingerId != touchFingerId)
        {
            return;
        }

        if (touch.phase == TouchPhase.Moved)
        {
            if (Vector2.Distance(
                    touchPressScreenPosition,
                    touch.position) >=
                clickDragThresholdPixels)
            {
                ResetFormationTouchInput();
            }

            return;
        }

        if (touch.phase == TouchPhase.Canceled)
        {
            ResetFormationTouchInput();
            return;
        }

        if (touch.phase != TouchPhase.Ended)
        {
            return;
        }

        Tile pressedTile = touchPressedTile;

        bool isValidTap =
            pressedTile != null &&
            !Tile.IsScreenPositionOverUI(
                touch.position) &&
            Vector2.Distance(
                touchPressScreenPosition,
                touch.position) <
            clickDragThresholdPixels;

        ResetFormationTouchInput();

        if (!isValidTap)
        {
            return;
        }

        Tile releasedTile =
            GetFormationTileAtScreenPosition(
                touch.position
            );

        if (releasedTile == pressedTile)
        {
            HandleFormationTileClick(
                releasedTile
            );
        }
    }

    private void ResetFormationTouchInput()
    {
        touchClickCandidate = false;
        touchFingerId = -1;
        touchPressedTile = null;
        touchPressScreenPosition = Vector2.zero;
    }

    // =====================================================
    // Formation Tile Detection
    // =====================================================

    // <변경부분>
    // 클릭한 화면 좌표에서 편성용 Tile만 조회한다.
    // 기존 Event BackgroundTile이나 다른 Collider는 무시한다.
    private Tile GetFormationTileAtScreenPosition(
        Vector2 screenPosition)
    {
        Camera selectionCamera =
            pixelCameraController != null
                ? pixelCameraController.GetComponent<Camera>()
                : null;

        if (selectionCamera == null)
        {
            selectionCamera = Camera.main;
        }

        if (selectionCamera == null ||
            boardManager == null ||
            formationBoardRoot == null)
        {
            return null;
        }

        float worldPlaneDistance =
            Mathf.Abs(
                selectionCamera.transform.position.z
            );

        Vector3 worldPosition =
            selectionCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    worldPlaneDistance
                )
            );

        Collider2D[] colliders =
            Physics2D.OverlapPointAll(
                new Vector2(
                    worldPosition.x,
                    worldPosition.y
                )
            );

        foreach (Collider2D collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            Tile tile = collider.GetComponent<Tile>();

            if (tile == null)
            {
                tile =
                    collider.GetComponentInParent<Tile>();
            }

            if (tile == null ||
                !tile.transform.IsChildOf(
                    formationBoardRoot.transform))
            {
                continue;
            }

            // 다른 BoardManager의 타일과 혼동하지 않는다.
            if (boardManager.GetTile(
                    tile.X,
                    tile.Y) == tile)
            {
                return tile;
            }
        }

        return null;
    }

    // =====================================================
    // Formation Piece Selection
    // =====================================================

    // <변경부분>
    // 기물 선택과 스테이터스 확인만 처리한다.
    //
    // 이번 단계에서는 일반 이동/공격/배치 이동을 실행하지 않는다.
    private void HandleFormationTileClick(
        Tile tile)
    {
        if (!isFormationOpen ||
            tile == null ||
            pieceManager == null)
        {
            return;
        }

        Piece clickedPiece =
            pieceManager.GetPieceAt(
                tile.X,
                tile.Y
            );

        // 빈칸은 다음 단계의 자리배치 대상으로 남겨둔다.
        if (clickedPiece == null ||
            clickedPiece.Team != PieceTeam.Player)
        {
            return;
        }

        // 동일한 기물을 다시 클릭하면 선택을 취소한다.
        if (clickedPiece == selectedFormationPiece)
        {
            ClearFormationSelection();
            return;
        }

        // 이전에 선택한 기물이 있으면 먼저 Down -> Idle.
        if (selectedFormationPiece != null)
        {
            pieceManager.PlayPieceDeselectAnimation(
                selectedFormationPiece
            );

            selectedFormationPiece = null;
        }

        // 기존 플레이어 스테이터스 UI 표시.
        // King 등 배치 이동 불가 기물도 정보 확인은 가능하다.
        if (playerStatusUIController != null)
        {
            playerStatusUIController.Refresh(
                clickedPiece
            );
        }
        else
        {
            Debug.LogWarning(
                "기물 편성: Player Status UI가 연결되지 않았습니다.",
                this
            );
        }

        // 기존 BattleScene의 기물 선택 카메라 포커스 재사용.
        if (pixelCameraController == null &&
            Camera.main != null)
        {
            pixelCameraController =
                Camera.main.GetComponent<
                    PixelCameraController>();
        }

        if (pixelCameraController != null)
        {
            pixelCameraController.FocusOnTile(
                tile.transform
            );
        }

        // King, Queen, Special은 정보 확인만 허용한다.
        if (!CanSelectFormationPiece(clickedPiece))
        {
            return;
        }

        selectedFormationPiece = clickedPiece;

        // 기존 배틀 초기배치의 Select -> Select_Idle 사용.
        pieceManager.PlayPieceSelectAnimation(
            selectedFormationPiece
        );
    }

    // <변경부분>
    // 일반 배틀 초기배치와 동일한 선택 허용 규칙.
    private bool CanSelectFormationPiece(Piece piece)
    {
        if (piece == null ||
            piece.Team != PieceTeam.Player)
        {
            return false;
        }

        switch (piece.PieceType)
        {
            case PieceType.Pawn:
            case PieceType.Knight:
            case PieceType.Rook:
            case PieceType.Bishop:
                return true;
        }

        return false;
    }

    // <변경부분>
    // 선택 해제 시 기존 Down -> Idle 애니메이션을 실행하고
    // 플레이어 스테이터스창을 닫는다.
    private void ClearFormationSelection()
    {
        if (selectedFormationPiece != null &&
            pieceManager != null)
        {
            pieceManager.PlayPieceDeselectAnimation(
                selectedFormationPiece
            );
        }

        selectedFormationPiece = null;

        if (playerStatusUIController != null)
        {
            playerStatusUIController.Clear();
        }
    }

    // <변경부분>
    // 기존 타일의 실제 월드 위치를 이용해 보드를 중앙에 배치한다.
    private void CenterFormationBoard()
    {
        Tile firstTile = boardManager.GetTile(0, 0);
        Tile lastTile = boardManager.GetTile(4, 1);

        if (firstTile == null || lastTile == null)
        {
            return;
        }

        Vector3 currentCenter =
            (firstTile.transform.position +
             lastTile.transform.position) * 0.5f;

        Vector3 targetCenter =
            boardCenterTarget != null
                ? boardCenterTarget.position
                : Vector3.zero;

        // 화면의 Z축 깊이는 기존 배틀 타일 설정을 유지한다.
        Vector3 offset =
            targetCenter - currentCenter;

        offset.z = 0f;

        formationBoardRoot.transform.position += offset;
    }

    // <변경부분>
    // 실제 편성 종료 처리에서 호출할 공통 종료 함수.
    //
    // 현재 단계에서는 배치 저장 기능이 없으므로
    // 보드를 숨기고 대기 중인 Event Step을 해제하는 역할만 한다.
    public void CloseFormationBoard()
    {
        // <변경부분>
        // 편성 종료 시 선택된 기물의 애니메이션과
        // 스테이터스 UI를 원래 상태로 돌린다.
        ClearFormationSelection();

        isFormationOpen = false;

        // 진행 중이던 클릭/터치 입력을 폐기한다.
        pcClickCandidate = false;
        pcWasDragged = false;
        pcPressedTile = null;
        ResetFormationTouchInput();

        if (formationBoardRoot != null)
        {
            formationBoardRoot.SetActive(false);
        }
    }

    // <변경부분>
    // 현재 보드 단계만 Play Mode에서 시험하기 위한 임시 명령.
    // 런타임 중 컴포넌트 메뉴에서 실행할 수 있다.
    // 편성 저장 기능은 실행하지 않는다.
    [ContextMenu("Debug - Close Formation Preview")]
    private void DebugCloseFormationPreview()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        CloseFormationBoard();
    }

    private void OnDisable()
    {
        CloseFormationBoard();
    }
}
