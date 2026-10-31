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

    public bool IsFormationOpen => isFormationOpen;

    public BoardManager BoardManager => boardManager;

    private void Awake()
    {
        // 이벤트 시작 전에는 편성 보드를 표시하지 않는다.
        CloseFormationBoard();
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

        Debug.Log(
            "기물 편성 보드 활성화 완료: 5x2 / 10 Tiles",
            this
        );

        return true;
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
        isFormationOpen = false;

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
