using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

// <변경부분>

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

    // <변경부분>
    // 편성 보드의 타일/기물 등장 연출 전용 컴포넌트.
    [Header("Formation Intro")]
    [SerializeField]
    private EventSceneFormationIntroAnimator formationIntroAnimator;

    // <변경부분>
    // 연출이 끝났을 때 편성 UI 버튼을 다시 활성화할지 관리한다.
    private bool unlockFormationButtonsAfterIntro;

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
    // 편성 타일 클릭 좌표를 계산할 때 사용할 카메라 참조.
    // 기물 선택에 따른 카메라 자동 포커스는 실행하지 않는다.
    [SerializeField]
    private PixelCameraController pixelCameraController;

    // <변경부분>
    // 카메라 드래그와 기물 클릭을 구분할 최소 이동 거리.
    [SerializeField, Min(1f)]
    private float clickDragThresholdPixels = 12f;

    // 현재 배치 대상으로 선택된 플레이어 기물.
    private Piece selectedFormationPiece;

 
    // <변경부분>
    // PieceSetting 전용 기물정보 아이콘 표시 모드.
    // All: 플레이어 기물 전체 표시.
    // Hidden: 플레이어 기물 전체 숨김.
    //
    // BattleScene의 3단계 표시 모드는 변경하지 않는다.
    private enum TypeIconDisplayMode
    {
        All = 0,
        Hidden = 1
    }

    // <변경부분>
    // PieceSetting 전용 표시 설정.
    // BattleManager의 PlayerPrefs와 분리하여
    // 배틀 기물정보 표시 설정에 영향을 주지 않는다.
    private const string TypeIconDisplayModePrefsKey =
        "DEVORYA.Formation.TypeIconDisplayMode.v1";

    [Header("Formation Type Icon")]

    // 편성 화면에서 사용할 기물정보 보기 버튼.
    [SerializeField]
    private Button typeIconButton;

    [Header("Formation Exit")]

    // <변경부분>
    // 기존 상점 Exit 버튼을 편성 종료 버튼으로 재활용한다.
    // 기존 Confirm Formation Button 연결값이 있으면 유지한다.
    [FormerlySerializedAs("confirmFormationButton")]
    [SerializeField]
    private Button exitFormationButton;

    // <변경부분>
    // 기존 상점 Exit 클릭 처리를 편성 중에만 잠시 분리한다.
    [SerializeField]
    private ShopController sharedShopController;

    // <변경부분>
    // 편성 종료 후 EventScene의 기존 완료 흐름으로
    // 월드맵에 복귀하기 위한 컨트롤러.
    [SerializeField]
    private EventSceneSequenceController eventSceneSequenceController;

    private bool shopExitListenerDetached;

    private TypeIconDisplayMode typeIconDisplayMode =
        TypeIconDisplayMode.All;

    // 편성 중 강조한 타일을 별도로 관리한다.
    // BattleManager의 하이라이트 목록과는 공유하지 않는다.
    private readonly List<Tile> formationHighlightedTiles =
        new List<Tile>();

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

    // <변경부분>
    // 다음 Event Step이 등장 연출 완료를 기다릴 때 사용한다.
    public bool IsFormationIntroPlaying =>
        formationIntroAnimator != null &&
        formationIntroAnimator.IsPlaying;

    public BoardManager BoardManager => boardManager;

    // <변경부분>
    // 편성 입력의 임시 진단 로그.
    // UNITY_EDITOR 환경에서만 출력한다.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogFormationDiagnostic(string message)
    {
        Debug.Log(
            $"[FormationDiag] {message}",
            this
        );
    }

    private void Awake()
    {
        // 이벤트 시작 전에는 편성 보드를 표시하지 않는다.
        CloseFormationBoard();
    }

    // <변경부분>
    // 타입 아이콘 버튼은 기존대로 연결한다.
    // Exit 버튼은 편성 화면을 열 때 별도로 연결한다.
    private void OnEnable()
    {
        if (typeIconButton != null)
        {
            typeIconButton.onClick.RemoveListener(
                ToggleFormationTypeIcons
            );

            typeIconButton.onClick.AddListener(
                ToggleFormationTypeIcons
            );
        }
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

        // <변경부분>
        // 타일 및 기물 등장 연출 중에는 편성 클릭을 처리하지 않는다.
        if (IsFormationIntroPlaying)
        {
            return;
        }

        // <변경부분>
        // 등장 연출이 끝나면 편성 버튼을 다시 활성화한다.
        if (unlockFormationButtonsAfterIntro)
        {
            unlockFormationButtonsAfterIntro = false;

            if (exitFormationButton != null)
            {
                exitFormationButton.interactable = true;
            }

            if (typeIconButton != null)
            {
                typeIconButton.interactable = true;
            }
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
            // <변경부분> 편성 보드는 부모 로컬 좌표 기준으로 생성한다.
            boardManager.RebuildBoardByTileType(
                tileTypeA,
                tileTypeB,
                true
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
        // 편성에서는 상점의 기존 Exit 버튼을 사용한다.
        if (exitFormationButton == null)
        {
            Debug.LogError(
                "편성 시작 실패: Formation Exit Button이 연결되지 않았습니다.",
                this
            );

            CloseFormationBoard();
            return false;
        }

        // Inspector 연결이 없으면 비활성 오브젝트도 검색한다.
        if (sharedShopController == null)
        {
            sharedShopController =
                FindFirstObjectByType<ShopController>(
                    FindObjectsInactive.Include
                );
        }

        // <변경부분>
        // ShopController의 기존 Exit 클릭 이벤트는
        // 편성 중에만 제거한다.
        // 상점 방문 완료 처리가 잘못 실행되지 않게 한다.
        if (sharedShopController != null)
        {
            exitFormationButton.onClick.RemoveListener(
                sharedShopController.OnExitShopButtonClicked
            );

            shopExitListenerDetached = true;
        }

        // <변경부분>
        // 동일 버튼에 편성 전용 종료 동작을 연결한다.
        exitFormationButton.onClick.RemoveListener(
            ExitFormationToWorldMap
        );

        exitFormationButton.onClick.AddListener(
            ExitFormationToWorldMap
        );

        exitFormationButton.gameObject.SetActive(true);
        exitFormationButton.interactable = true;

        // <변경부분>
        // PieceSetting 전용 기물정보 아이콘 표시 상태를 복원한다.
        // 처음 실행하는 경우에는 전체 표시를 기본값으로 사용한다.
        int savedDisplayMode = PlayerPrefs.GetInt(
            TypeIconDisplayModePrefsKey,
            (int)TypeIconDisplayMode.All
        );

        if (savedDisplayMode < (int)TypeIconDisplayMode.All ||
            savedDisplayMode > (int)TypeIconDisplayMode.Hidden)
        {
            savedDisplayMode = (int)TypeIconDisplayMode.All;
        }

        typeIconDisplayMode =
            (TypeIconDisplayMode)savedDisplayMode;

        // 현재 편성된 플레이어 기물 전체에 표시 상태를 적용한다.
        RefreshFormationTypeIconVisuals();

        // 편성 중에는 기물정보 보기 버튼을 사용할 수 있다.
        if (typeIconButton != null)
        {
            typeIconButton.gameObject.SetActive(true);
            typeIconButton.interactable = true;
        }

        // <변경부분>
        // 편성 보드가 실제로 입력 가능한 상태인지 진단한다.
        LogFormationDiagnostic(
            $"OPEN | " +
            $"ControllerEnabled={isActiveAndEnabled} | " +
            $"RootActive={formationBoardRoot.activeInHierarchy} | " +
            $"Board={boardManager.name} | " +
            $"PieceManager={pieceManager.name}"
        );

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

        // <변경부분>
        // 타일과 기물의 실제 생성은 기존 방식으로 완료하고,
        // 화면에 표시되는 등장 순서만 연출한다.
        //
        // Animator가 Inspector에 연결되지 않았더라도
        // 연출을 조용히 건너뛰지 않고 자동으로 생성한다.
        if (formationIntroAnimator == null)
        {
            formationIntroAnimator =
                GetComponent<EventSceneFormationIntroAnimator>();

            if (formationIntroAnimator == null)
            {
                formationIntroAnimator =
                    gameObject.AddComponent<EventSceneFormationIntroAnimator>();

                Debug.Log(
                    "[FormationIntro] 등장 애니메이터 컴포넌트를 자동 추가했습니다.",
                    this
                );
            }
        }

        if (formationIntroAnimator == null ||
            !formationIntroAnimator.isActiveAndEnabled)
        {
            Debug.LogError(
                "[FormationIntro] 등장 연출을 시작할 수 없습니다. " +
                "Animator 컴포넌트의 활성 상태를 확인하세요.",
                this
            );
        }
        else
        {
            formationIntroAnimator.Play(
                boardManager,
                isPlayerPiecesGenerated ? pieceManager : null
            );

            if (formationIntroAnimator.IsPlaying)
            {
                unlockFormationButtonsAfterIntro = true;

                if (exitFormationButton != null)
                {
                    exitFormationButton.interactable = false;
                }

                if (typeIconButton != null)
                {
                    typeIconButton.interactable = false;
                }

                Debug.Log(
                    "[FormationIntro] 편성 보드 등장 연출이 시작되었습니다.",
                    this
                );
            }
            else
            {
                Debug.LogWarning(
                    "[FormationIntro] Animator.Play() 호출 후에도 " +
                    "연출이 시작되지 않았습니다. " +
                    "Play Intro 설정과 타일 참조를 확인하세요.",
                    this
                );
            }
        }

        return true;
    }

    // =====================================================
    // PC Selection Input
    // =====================================================

    // <변경부분>
    // 클릭을 차단하는 UI GraphicRaycaster 대상을 진단한다.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogUIRaycastBlockers(Vector2 screenPosition)
    {
        var eventSystem =
            UnityEngine.EventSystems.EventSystem.current;

        if (eventSystem == null)
        {
            LogFormationDiagnostic(
                "UI DEBUG | EventSystem 없음"
            );
            return;
        }

        var pointerData =
            new UnityEngine.EventSystems.PointerEventData(
                eventSystem
            );

        pointerData.position = screenPosition;

        var results =
            new List<UnityEngine.EventSystems.RaycastResult>();

        eventSystem.RaycastAll(pointerData, results);

        int graphicCount = 0;

        foreach (var result in results)
        {
            if (!(result.module is
                UnityEngine.UI.GraphicRaycaster))
            {
                continue;
            }

            GameObject target = result.gameObject;

            if (target == null)
            {
                continue;
            }

            string hierarchyPath = target.name;

            Transform parent = target.transform.parent;

            while (parent != null)
            {
                hierarchyPath =
                    parent.name + "/" + hierarchyPath;

                parent = parent.parent;
            }

            var graphic =
                target.GetComponent<UnityEngine.UI.Graphic>();

            LogFormationDiagnostic(
                $"UI BLOCKER #{graphicCount + 1} | " +
                $"Object={hierarchyPath} | " +
                $"Graphic={graphic?.GetType().Name} | " +
                $"RaycastTarget={graphic?.raycastTarget}"
            );

            graphicCount++;

            if (graphicCount >= 10)
            {
                break;
            }
        }

        if (graphicCount == 0)
        {
            LogFormationDiagnostic(
                "UI DEBUG | GraphicRaycaster 대상 없음"
            );
        }
    }

    // <변경부분>
    // 기존 카메라와 동일하게 클릭 시작/종료 타일이 같고
    // 드래그가 발생하지 않은 경우에만 선택한다.
    //
    // 임시 진단:
    // MouseDown → UI 차단 → Tile 탐색 →
    // MouseUp → 클릭 확정 여부를 출력한다.
    private void HandlePCFormationInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pcClickCandidate = false;
            pcWasDragged = false;
            pcPressedTile = null;

            // <변경부분>
            // EventSystem의 이전 Pointer 상태를 사용하지 않고
            // 현재 마우스 화면 좌표에서 UI를 직접 검사한다.
            bool isOverUI =
                Tile.IsScreenPositionOverUI(
                    Input.mousePosition
                );

            LogFormationDiagnostic(
                $"MOUSE DOWN | " +
                $"Position={Input.mousePosition} | " +
                $"UIBlocked={isOverUI}"
            );

            // UI 버튼 위에서 시작한 클릭은 무시한다.
            if (isOverUI)
            {
                LogFormationDiagnostic(
                    "BLOCKED | UI Raycast가 클릭을 차단했습니다."
                );

                // <변경부분>
                // 어떤 UI가 입력을 차단했는지 확인한다.
                LogUIRaycastBlockers(Input.mousePosition);

                return;
            }

            pcClickCandidate = true;

            pcPressScreenPosition =
                Input.mousePosition;

            pcPressedTile =
                GetFormationTileAtScreenPosition(
                    pcPressScreenPosition
                );

            LogFormationDiagnostic(
                "MOUSE DOWN TILE | " +
                (pcPressedTile != null
                    ? $"({pcPressedTile.X}, {pcPressedTile.Y})"
                    : "NULL")
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

        // <변경부분>
        // 클릭 종료 시에도 현재 화면 좌표의 UI만 검사한다.
        bool isOverUIOnRelease =
            Tile.IsScreenPositionOverUI(
                Input.mousePosition
            );

        float clickDistance =
            Vector2.Distance(
                pcPressScreenPosition,
                Input.mousePosition
            );

        bool isValidClick =
            !pcWasDragged &&
            pressedTile != null &&
            !isOverUIOnRelease &&
            clickDistance <
            clickDragThresholdPixels;

        LogFormationDiagnostic(
            $"MOUSE UP | " +
            $"Dragged={pcWasDragged} | " +
            $"PressedTile={(pressedTile != null ? pressedTile.name : "NULL")} | " +
            $"UIBlocked={isOverUIOnRelease} | " +
            $"Distance={clickDistance:F1} | " +
            $"Valid={isValidClick}"
        );

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

        LogFormationDiagnostic(
            "MOUSE UP TILE | " +
            (releasedTile != null
                ? $"({releasedTile.X}, {releasedTile.Y})"
                : "NULL")
        );

        if (releasedTile == pressedTile)
        {
            LogFormationDiagnostic(
                "TILE CLICK CONFIRMED | 기물 조회로 전달합니다."
            );

            HandleFormationTileClick(
                releasedTile
            );
        }
        else
        {
            LogFormationDiagnostic(
                "BLOCKED | 클릭 시작 타일과 종료 타일이 다릅니다."
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
    //
    // 임시 진단:
    // Screen 좌표, World 좌표, Collider 검출 수,
    // 편성 Tile 식별 결과를 확인한다.
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
            LogFormationDiagnostic(
                $"RAYCAST FAILED | " +
                $"Camera={(selectionCamera != null)} | " +
                $"BoardManager={(boardManager != null)} | " +
                $"Root={(formationBoardRoot != null)}"
            );

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

        int tileColliderCount = 0;
        int formationTileCount = 0;

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

            if (tile == null)
            {
                continue;
            }

            tileColliderCount++;

            if (!tile.transform.IsChildOf(
                    formationBoardRoot.transform))
            {
                continue;
            }

            formationTileCount++;

            // 다른 BoardManager의 타일과 혼동하지 않는다.
            if (boardManager.GetTile(
                    tile.X,
                    tile.Y) == tile)
            {
                LogFormationDiagnostic(
                    $"RAYCAST SUCCESS | " +
                    $"Camera={selectionCamera.name} | " +
                    $"World=({worldPosition.x:F2}, {worldPosition.y:F2}) | " +
                    $"Tile={tile.name} | " +
                    $"ColliderCount={colliders.Length}"
                );

                return tile;
            }
        }

        LogFormationDiagnostic(
     $"RAYCAST NULL | " +
     $"Camera={selectionCamera.name} | " +
     $"World=({worldPosition.x:F2}, {worldPosition.y:F2}) | " +
     $"ColliderCount={colliders.Length} | " +
     $"TileColliders={tileColliderCount} | " +
     $"FormationTiles={formationTileCount}"
 );

        // <변경부분>
        // 화면상 마우스 위치와 가장 가까운 편성 타일을 찾는다.
        // 클릭 처리에는 영향을 주지 않는 진단 코드.
        Tile nearestTile = null;
        float nearestDistance = float.MaxValue;

        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Tile tile = boardManager.GetTile(x, y);

                if (tile == null)
                {
                    continue;
                }

                Vector3 tileScreen =
                    selectionCamera.WorldToScreenPoint(
                        tile.transform.position
                    );

                float distance = Vector2.Distance(
                    screenPosition,
                    new Vector2(tileScreen.x, tileScreen.y)
                );

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTile = tile;
                }
            }
        }

        if (nearestTile != null)
        {
            Collider2D tileCollider =
                nearestTile.GetComponentInChildren<Collider2D>(true);

            if (tileCollider != null)
            {
                Vector3 center = tileCollider.bounds.center;

                int centerHits = Physics2D.OverlapPointAll(
                    new Vector2(center.x, center.y)
                ).Length;

                LogFormationDiagnostic(
                    $"NEAREST TILE | " +
                    $"Name={nearestTile.name} | " +
                    $"ScreenDistance={nearestDistance:F1}px | " +
                    $"TileWorld={nearestTile.transform.position} | " +
                    $"ColliderCenter={center} | " +
                    $"ColliderSize={tileCollider.bounds.size} | " +
                    $"ColliderEnabled={tileCollider.enabled} | " +
                    $"ColliderActive={tileCollider.gameObject.activeInHierarchy} | " +
                    $"Layer={LayerMask.LayerToName(tileCollider.gameObject.layer)} | " +
                    $"PointInside={tileCollider.OverlapPoint(worldPosition)} | " +
                    $"CenterHits={centerHits}"
                );
            }
            else
            {
                LogFormationDiagnostic(
                    $"NEAREST TILE | {nearestTile.name} | " +
                    "Collider2D를 찾을 수 없습니다."
                );
            }
        }

        return null;
    }

    // =====================================================
    // Formation Piece Selection
    // =====================================================

    // <변경부분>
    // PixelCameraController가 전달한 타일이
    // 실제 편성 보드의 타일인지 확인한다.
    public void SelectFormationTile(Tile tile)
    {
        if (!isFormationOpen ||
            tile == null ||
            boardManager == null)
        {
            return;
        }

        if (boardManager.GetTile(tile.X, tile.Y) != tile)
        {
            return;
        }

        HandleFormationTileClick(tile);
    }

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

        // <변경부분>
        // 타일 좌표로 실제 기물을 찾았는지 확인한다.
        LogFormationDiagnostic(
            $"PIECE LOOKUP | " +
            $"Tile=({tile.X}, {tile.Y}) | " +
            $"Piece={(clickedPiece != null ? clickedPiece.name : "NULL")} | " +
            $"Team={(clickedPiece != null ? clickedPiece.Team.ToString() : "NULL")}"
        );

        // <변경부분>
        // 동일한 기물을 다시 클릭하면 선택을 해제한다.
        if (selectedFormationPiece != null &&
            clickedPiece == selectedFormationPiece)
        {
            ClearFormationSelection();
            return;
        }

        // <변경부분>
        // 배치 가능한 Player 기물을 선택한 상태에서
        // 다른 타일을 클릭하면 이동 또는 자리 교환을 시도한다.
        //
        // 빈 타일:
        // 선택한 기물을 해당 위치로 이동한다.
        //
        // 다른 일반 Player 기물:
        // 두 기물의 위치를 서로 교환한다.
        //
        // King / Queen / Special:
        // 배치 대상으로 사용하지 않는다.
        if (selectedFormationPiece != null &&
      CanSelectFormationPiece(selectedFormationPiece) &&
      (clickedPiece == null ||
       CanSelectFormationPiece(clickedPiece)))
        {
            // <변경부분>
            // 저장 실패 시 원래 자리로 복구할 수 있도록
            // 이동 전 좌표를 보관한다.
            int previousX = selectedFormationPiece.X;
            int previousY = selectedFormationPiece.Y;

            bool moved =
                pieceManager.TryMoveOrSwapPlayerDeploymentPiece(
                    selectedFormationPiece,
                    tile.X,
                    tile.Y
                );

            if (moved)
            {
                // <변경부분>
                // 실제 RunState가 존재하면 자리 이동 직후
                // 변경된 편성 좌표를 자동 저장한다.
                RunStateManager runState =
                    RunStateManager.Instance;

                if (runState != null &&
                    runState.HasPlayerPieceRuntimeData)
                {
                    bool saved =
                        TrySaveCurrentFormationToRunState(
                            false
                        );

                    if (!saved)
                    {
                        // <변경부분>
                        // 자동 저장에 실패하면 이동 또는 교환을
                        // 이전 좌표로 되돌린다.
                        bool restored =
                            pieceManager.TryMoveOrSwapPlayerDeploymentPiece(
                                selectedFormationPiece,
                                previousX,
                                previousY
                            );

                        Debug.LogError(
                            "편성 자동 저장 실패. " +
                            (restored
                                ? "이전 배치로 복구했습니다."
                                : "이전 배치 복구에도 실패했습니다."),
                            this
                        );
                    }
                }

                // Editor Test Pieces만 있는 경우에는
                // 기존대로 배치 테스트만 허용한다.
                // 실제 RunState는 생성하거나 변경하지 않는다.

                ClearFormationSelection();
            }

            return;
        }

        // <변경부분>
        // 선택할 기물이 없는 빈 타일이나
        // Player가 아닌 기물은 선택하지 않는다.
        if (clickedPiece == null ||
            clickedPiece.Team != PieceTeam.Player)
        {
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

        // <변경부분>
        // 이전 기물의 배치 가능 타일 강조를 모두 제거한다.
        ClearFormationTileHighlights();

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

        // <변경부분>
        // EventScene에서는 기물을 선택해도 카메라를 이동시키지 않는다.
        // 카메라 위치와 구도는 EventScene의 Camera Move/Shot Step이 담당한다.
        //
        // 기물 선택, 스테이터스 표시, 선택 애니메이션,
        // 타입 아이콘 강조 및 타일 하이라이트는 그대로 유지한다.

        // <변경부분>
        // 보스(King)를 포함한 모든 Player 기물은
        // 배치 이동 가능 여부와 관계없이 선택할 수 있다.
        //
        // 선택과 배치 이동 가능 여부를 분리한다.
        selectedFormationPiece = clickedPiece;

        // 기존 배틀 초기배치의 Select -> Select_Idle 사용.
        // 보스도 동일한 선택 애니메이션을 실행한다.
        pieceManager.PlayPieceSelectAnimation(
            selectedFormationPiece
        );

        // <변경부분>
        // Pawn / Knight / Rook / Bishop만
        // 배치 가능한 타일을 하이라이트한다.
        //
        // King / Queen / Special 등은
        // 선택 연출과 정보 확인만 허용한다.
        if (CanSelectFormationPiece(clickedPiece))
        {
            ShowFormationDeploymentTiles();
        }

        // <변경부분>
        // 모든 Player 기물에 선택 타입 아이콘 연출 적용.
        // 배치 이동 불가 기물도 선택 아이콘을 표시한다.
        RefreshFormationTypeIconVisuals();
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

        // <변경부분>
        // 편성 타일의 하이라이트를 모두 원상복구한다.
        ClearFormationTileHighlights();

        if (playerStatusUIController != null)
        {
            playerStatusUIController.Clear();
        }

        // 편성 화면이 열린 상태에서만 아이콘 표시를 복원한다.
        if (isFormationOpen)
        {
            RefreshFormationTypeIconVisuals();
        }
    }

    // =====================================================
    // Formation Type Icons
    // =====================================================

    // <변경부분>
    // PieceSetting에서는 기물정보 아이콘을 2단계로 전환한다.
    //
    // All -> Hidden -> All
    //
    // 기존 BattleScene의 3단계 표시 모드와는 독립적이다.
    public void ToggleFormationTypeIcons()
    {
        if (!isFormationOpen)
        {
            return;
        }

        // <변경부분>
        // 현재 전체 표시 상태라면 숨기고,
        // 숨김 상태라면 전체 표시로 복귀한다.
        typeIconDisplayMode =
            typeIconDisplayMode == TypeIconDisplayMode.All
                ? TypeIconDisplayMode.Hidden
                : TypeIconDisplayMode.All;

        // <변경부분>
        // 편성 전용 저장 키에만 표시 상태를 저장한다.
        // 기존 배틀 설정은 변경하지 않는다.
        PlayerPrefs.SetInt(
            TypeIconDisplayModePrefsKey,
            (int)typeIconDisplayMode
        );

        PlayerPrefs.Save();

        // 현재 편성 보드의 기물 전체에 즉시 반영한다.
        RefreshFormationTypeIconVisuals();
    }

    // <변경부분>
    // PieceSetting의 플레이어 기물 전체를 대상으로
    // 기물정보 아이콘 ON/OFF를 적용한다.
    //
    // 기존 Piece의 타입 아이콘 표시/선택/정렬 API를 재사용한다.
    //
    // 아이콘을 숨겨도 실제 기물 선택, 이동, 자리 교환,
    // RunState 저장 기능에는 영향을 주지 않는다.
    private void RefreshFormationTypeIconVisuals()
    {
        if (boardManager == null ||
            pieceManager == null ||
            !isPlayerPiecesGenerated)
        {
            return;
        }

        bool showAllTypeIcons =
            typeIconDisplayMode == TypeIconDisplayMode.All;

        bool hasFocusedTypeIcon =
            selectedFormationPiece != null;

        for (int x = 0; x < boardManager.Width; x++)
        {
            for (int y = 0; y < boardManager.Height; y++)
            {
                Piece piece =
                    pieceManager.GetPieceAt(x, y);

                // <변경부분>
                // PieceSetting에서는 플레이어 기물만 제어한다.
                // 적/중립 기물에는 영향을 주지 않는다.
                if (piece == null ||
                    piece.Team != PieceTeam.Player)
                {
                    continue;
                }


                bool isFocusedTypeIcon =
                    piece == selectedFormationPiece;

                // <변경부분>
                // ON 상태에서는 모든 플레이어 기물의 아이콘을 표시한다.
                // OFF 상태에서는 선택된 기물의 아이콘만 표시한다.
                //
                // 선택 해제 시에는 OFF 상태의 모든 아이콘이 숨겨진다.
                bool shouldShowTypeIcon =
                    showAllTypeIcons ||
                    isFocusedTypeIcon;

                // <변경부분>
                // ON/OFF 상태와 관계없이
                // 선택된 기물의 아이콘에는 기존 강조 연출을 적용한다.
                bool shouldSelectTypeIcon =
                    shouldShowTypeIcon &&
                    isFocusedTypeIcon;

                piece.SetTypeIconSelected(
                    shouldSelectTypeIcon
                );


                piece.SetTypeIconSortingPriority(
                    shouldSelectTypeIcon ? 1 : 0
                );

                // 전체 표시 상태에서는 기존처럼
                // 선택된 기물 이외의 아이콘을 흐리게 처리한다.
                bool shouldDimTypeIcon =
                    shouldShowTypeIcon &&
                    hasFocusedTypeIcon &&
                    !isFocusedTypeIcon;

                piece.SetTypeIconDimmed(
                    shouldDimTypeIcon
                );

                // <변경부분>
                // 표시 여부를 마지막에 적용하여
                // 선택 상태 갱신 후에도 숨김 설정을 보장한다.
                piece.SetTypeIconVisible(
                    shouldShowTypeIcon
                );
            }
        }
    }

    // =====================================================
    // Formation Tile Highlight
    // =====================================================

    // <변경부분>
    // 배틀씬 초기배치의 ShowPlayerDeploymentTiles와
    // 동일한 규칙으로 편성 가능한 타일을 강조한다.
    private void ShowFormationDeploymentTiles()
    {
        if (selectedFormationPiece == null ||
            boardManager == null ||
            pieceManager == null)
        {
            return;
        }

        ClearFormationTileHighlights();

        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece targetPiece =
                    pieceManager.GetPieceAt(x, y);

                // King, Queen, Special 및 다른 진영 기물이
                // 있는 타일은 배치 대상에서 제외한다.
                if (targetPiece != null &&
                    !CanSelectFormationPiece(targetPiece))
                {
                    continue;
                }

                Tile tile = boardManager.GetTile(x, y);

                if (tile == null)
                {
                    continue;
                }

                tile.ShowHighlight();

                formationHighlightedTiles.Add(tile);
            }
        }
    }

    // <변경부분>
    // 편성 시스템이 강조한 타일만 원래 색상으로 복원한다.
    private void ClearFormationTileHighlights()
    {
        foreach (Tile tile in formationHighlightedTiles)
        {
            if (tile == null)
            {
                continue;
            }

            tile.HideHighlight();
        }

        formationHighlightedTiles.Clear();
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

    // =====================================================
    // Formation Auto Save / Exit
    // =====================================================

    // <변경부분>
    // 현재 편성 보드의 기물 위치를 RunState에 저장한다.
    //
    // 기존 PieceData, Trait, Ability, CanMove,
    // Absorbed Visual 등의 런 데이터는 유지하고
    // formationX/Y 좌표만 변경한다.
    //
    // 이 함수는 화면을 닫거나 Scene을 전환하지 않는다.
    private bool TrySaveCurrentFormationToRunState(
        bool logSuccess)
    {
        if (!isFormationOpen ||
            !isPlayerPiecesGenerated ||
            boardManager == null ||
            pieceManager == null)
        {
            return false;
        }

        RunStateManager runState =
            RunStateManager.Instance;

        // 실제 런 데이터가 없으면 저장하지 않는다.
        // Editor Test Pieces는 RunState를 덮어쓰지 않는다.
        if (runState == null ||
            !runState.HasPlayerPieceRuntimeData)
        {
            return false;
        }

        List<PlayerPieceRuntimeData> confirmedPieces =
            new List<PlayerPieceRuntimeData>();

        // <변경부분>
        // 현재 5x2 편성 보드의 기물을 좌표 순서대로 읽는다.
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Piece piece =
                    pieceManager.GetPieceAt(x, y);

                if (piece == null)
                {
                    continue;
                }

                if (piece.Team != PieceTeam.Player)
                {
                    Debug.LogError(
                        $"편성 자동 저장 실패: ({x}, {y})에 " +
                        "Player가 아닌 기물이 있습니다.",
                        this
                    );

                    return false;
                }

                // <변경부분>
                // 기존 RunState에서 생성한 원본 데이터를 복사한다.
                // 기물을 이동·교환해도 원본 정보가 유지된다.
                PlayerPieceRuntimeData runtimeData =
                    pieceManager.GetSpawnedRuntimeSourceCopy(
                        piece
                    );

                if (runtimeData == null ||
                    runtimeData.pieceData == null)
                {
                    Debug.LogError(
                        $"편성 자동 저장 실패: ({x}, {y}) 기물의 " +
                        "원본 RunState 데이터를 찾을 수 없습니다.",
                        this
                    );

                    return false;
                }

                // <변경부분>
                // 현재 타일 좌표만 변경한다.
                runtimeData.formationX = x;
                runtimeData.formationY = y;

                confirmedPieces.Add(runtimeData);
            }
        }

        int storedPieceCount =
            runState.GetPlayerPieceCount();

        // <변경부분>
        // 기물 수가 다르면 기존 런 데이터를 유지한다.
        if (confirmedPieces.Count == 0 ||
            confirmedPieces.Count != storedPieceCount)
        {
            Debug.LogError(
                "편성 자동 저장 실패: 기물 수가 일치하지 않습니다. " +
                $"편성 보드 {confirmedPieces.Count}개 / " +
                $"RunState {storedPieceCount}개",
                this
            );

            return false;
        }

        // 모든 검증 완료 후 RunState를 갱신한다.
        runState.SavePlayerPieces(
            confirmedPieces
        );

        if (logSuccess)
        {
            Debug.Log(
                $"편성 자동 저장 확인 완료: " +
                $"{confirmedPieces.Count}개",
                this
            );
        }

        return true;
    }

    // <변경부분>
    // 기존 상점 Exit 버튼을 눌렀을 때 실행한다.
    //
    // 실제 RunState가 있으면 마지막으로 저장 상태를 확인한다.
    // 저장 성공 후 기존 EventScene 완료 흐름을 실행한다.
    //
    // 상점 방문 완료 로직은 호출하지 않는다.
    public void ExitFormationToWorldMap()
    {
        if (!isFormationOpen)
        {
            return;
        }

        RunStateManager runState =
            RunStateManager.Instance;

        if (runState != null &&
            runState.HasPlayerPieceRuntimeData)
        {
            // <변경부분>
            // 마지막 변경 위치까지 저장되었는지 최종 확인한다.
            // 저장 실패 시에는 편성 화면을 유지한다.
            if (!TrySaveCurrentFormationToRunState(true))
            {
                return;
            }
        }
        else
        {
#if UNITY_EDITOR
            // <변경부분>
            // Editor Test Pieces는 런 저장 없이
            // UI 및 이벤트 종료 테스트만 허용한다.
            Debug.LogWarning(
                "Editor Test Pieces로 편성을 종료합니다. " +
                "실제 RunState 저장은 진행하지 않았습니다.",
                this
            );
#else
            Debug.LogError(
                "편성 종료 실패: 저장할 RunState 기물이 없습니다.",
                this
            );

            return;
#endif
        }

        // <변경부분>
        // 기존 EventSceneSequenceController를 통해
        // 정상적인 이벤트 완료 및 월드맵 복귀를 실행한다.
        if (eventSceneSequenceController == null)
        {
            eventSceneSequenceController =
                FindFirstObjectByType<
                    EventSceneSequenceController
                >();
        }

        if (eventSceneSequenceController == null ||
            !eventSceneSequenceController.IsSequenceActive)
        {
            Debug.LogWarning(
                "EventScene Sequence가 실행 중이지 않습니다. " +
                "편성 보드만 종료합니다.",
                this
            );

            // 단독 Editor 테스트에서도 Exit 동작은 확인할 수 있다.
            CloseFormationBoard();
            return;
        }

        // <변경부분>
        // 편성 Exit는 편성 화면만 닫는다.
        // 이후 CameraShot 등의 Step이 남아 있다면 계속 실행하고,
        // 모든 Step이 끝난 뒤 EventSceneSequenceController가
        // EventSceneData의 Completion 설정에 따라 종료한다.
        CloseFormationBoard();
    }

    // <변경부분>
    // 편성 보드의 공통 종료 처리.
    // 단순 종료 시에는 RunState에 저장하지 않는다.
    public void CloseFormationBoard()
    {
        // <변경부분>
        // 연출 중 강제로 닫히더라도
        // 모든 타일과 기물의 원래 투명도/위치를 복구한다.
        if (formationIntroAnimator != null)
        {
            formationIntroAnimator.StopAndRestore();
        }

        unlockFormationButtonsAfterIntro = false;

        // <변경부분>
        // 편성 종료 시 선택된 기물의 애니메이션과
        // 스테이터스 UI를 원래 상태로 돌린다.
        ClearFormationSelection();

        isFormationOpen = false;

        // <변경부분>
        // 편성 종료 시 기물정보 보기 버튼을 숨긴다.
        if (typeIconButton != null)
        {
            typeIconButton.gameObject.SetActive(false);
        }

        // <변경부분>
        // 편성 종료 시 공용 Exit 버튼의
        // 편성 전용 이벤트를 제거한다.
        if (exitFormationButton != null)
        {
            exitFormationButton.onClick.RemoveListener(
                ExitFormationToWorldMap
            );

            // 편성 중 분리했던 상점 Exit 이벤트를 복원한다.
            if (shopExitListenerDetached &&
                sharedShopController != null)
            {
                exitFormationButton.onClick.RemoveListener(
                    sharedShopController.OnExitShopButtonClicked
                );

                exitFormationButton.onClick.AddListener(
                    sharedShopController.OnExitShopButtonClicked
                );
            }

            exitFormationButton.gameObject.SetActive(false);
        }

        shopExitListenerDetached = false;

        // 진행 중이던 클릭/터치 입력을 폐기한다.
        pcClickCandidate = false;
        pcWasDragged = false;
        pcPressedTile = null;
        ResetFormationTouchInput();

        if (formationBoardRoot != null)
        {
            formationBoardRoot.SetActive(false);
        }

        // <변경부분>
        // 편성 종료 시 수동 카메라 잠금을 원래 상태로 복구한다.
        // 다음 Event Step 진행 여부는 SequenceController가 판단한다.
        if (eventSceneSequenceController != null)
        {
            eventSceneSequenceController.OnFormationBoardClosed();
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
        // <변경부분>
        // 타입 아이콘 버튼 이벤트 정리.
        if (typeIconButton != null)
        {
            typeIconButton.onClick.RemoveListener(
                ToggleFormationTypeIcons
            );
        }

        // <변경부분>
        // 편성 전용 Exit 이벤트 정리.
        if (exitFormationButton != null)
        {
            exitFormationButton.onClick.RemoveListener(
                ExitFormationToWorldMap
            );
        }

        CloseFormationBoard();
    }
}
