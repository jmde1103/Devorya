using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    //다른 스크립트에서 BattleManager에 접근하기 위한 임시 싱글톤
    public static BattleManager Instance { get; private set; }

    // 보드 매니저 참조
    [Header("Manager")]
    [SerializeField] private BoardManager boardManager;

    // 기물 매니저 참조
    [SerializeField] private PieceManager pieceManager;

    // 현재 선택된 기물
    private Piece selectedPiece;
    //현재 전투 턴
    private BattleTurn currentTurn = BattleTurn.Player;
    //현재 전투 결과 상태
    private BattleResult battleResult = BattleResult.None;


    // 흡수 모드가 켜져 있는지 여부
    private bool isAbsorbMode = false;
    // 전투가 끝났는지 여부
    private bool isBattleEnded = false;
    // 현재 턴에 고유 스킬을 이미 사용했는지 여부
    private bool hasUsedUniqueSkillThisTurn = false;

    // 현재 하이라이트된 타일 목록
    private readonly List<Tile> highlightedTiles = new List<Tile>();

    // 현재 선택된 기물이 실제로 이동/공격할 수 있는 타일 목록
    private readonly List<Tile> selectableTiles = new List<Tile>();

    // UI 버튼들
    [Header("UI")]
    [SerializeField] private Button absorbButton;
    [SerializeField] private Button surrenderButton;

    // 흡수 버튼 텍스트
    [SerializeField] private TMP_Text absorbButtonText;

    // 오브젝트 생성 시 한 번 실행
    private void Awake()
    {
        // 싱글톤 등록
        Instance = this;
    }

    private void Start()
    {
        // 흡수 버튼 연결
        if (absorbButton != null)
        {
            absorbButton.onClick.AddListener(ToggleAbsorbMode);
        }

        // 기권 버튼 연결
        if (surrenderButton != null)
        {
            surrenderButton.onClick.AddListener(Surrender);
        }

        // 흡수 버튼 텍스트 초기화
        UpdateAbsorbButtonText();
    }

    private void Update()
    {
        // Space 키를 누르면 턴 종료
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
        }

        // A 키를 누르면 흡수 모드 ON/OFF
        if (Input.GetKeyDown(KeyCode.A))
        {
            ToggleAbsorbMode();
        }

        // Q 키를 누르면 기권
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Surrender();
        }

        // <변경부분> S 키를 누르면 선택된 기물의 고유 스킬 사용
        if (Input.GetKeyDown(KeyCode.S))
        {
            UseSelectedPieceSkill();
        }
    }

    // 흡수 버튼 텍스트를 현재 상태에 맞게 갱신하는 함수
    private void UpdateAbsorbButtonText()
    {
        // 버튼 텍스트가 없으면 종료
        if (absorbButtonText == null)
        {
            return;
        }

        // 흡수 모드 상태에 따라 버튼 텍스트 변경
        absorbButtonText.text = isAbsorbMode ? "흡수 ON" : "흡수 OFF";
    }

    // 기물을 선택하는 함수
    public void SelectPiece(Piece piece)
    {
        // 전투가 끝났으면 더 이상 선택 불가
        if (isBattleEnded)
        {
            return;
        }
        // 이전 하이라이트 제거
        ClearHighlights();

        // 선택한 기물이 없으면 종료
        if (piece == null)
        {
            selectedPiece = null;
            return;
        }

        // 이동할 수 없는 기물은 선택 불가
        if (piece.CanMove == false)
        {
            selectedPiece = null;
            return;
        }

        // 현재 플레이어 턴인데 플레이어 기물이 아니면 선택 불가
        if (currentTurn == BattleTurn.Player && piece.Team != PieceTeam.Player)
        {
            Debug.Log("현재는 플레이어 턴입니다.");
            selectedPiece = null;
            return;
        }

        //현재 적 턴인데 적 기물이 아니면 선택 불가
        if (currentTurn == BattleTurn.Enemy && piece.Team != PieceTeam.Enemy)
        {
            Debug.Log("현재는 적 턴입니다.");
            selectedPiece = null;
            return;
        }

        // 선택 기물 저장
        selectedPiece = piece;

        // 이동 가능 타일 표시
        ShowMovableTiles(selectedPiece);

        // 선택 확인용 로그
        Debug.Log($"선택됨: {piece.Team} / {piece.PieceType} / ({piece.X}, {piece.Y})");
    }

    //타일을 선택했을 때 호출되는 함수
    public void SelectTile(Tile tile)
    {
        //전투가 끝났으면 타일 선택 불가
        if (isBattleEnded)
        {
            return;
        }

        // 선택된 기물이 없으면 종료
        if (selectedPiece == null)
        {
            return;
        }

        // 클릭한 타일이 이동/공격 가능한 타일이 아니면 종료
        if (selectableTiles.Contains(tile) == false)
        {
            return;
        }

        // 해당 타일에 있는 기물 확인
        Piece targetPiece = pieceManager.GetPieceAt(tile.X, tile.Y);

        // 타겟 기물이 있으면 공격 처리
        if (targetPiece != null)
        {
            // 적대 관계가 아니면 공격 불가
            if (selectedPiece.IsEnemyOf(targetPiece) == false)
            {
                return;
            }

            // 흡수 모드이고, 플레이어 기물이 적 기물을 잡는 경우
            // 단, 상대 King은 흡수 대상에서 제외
            if (isAbsorbMode &&
                selectedPiece.Team == PieceTeam.Player &&
                targetPiece.Team == PieceTeam.Enemy &&
                targetPiece.PieceType != PieceType.King)
            {
                PieceType absorbedType = targetPiece.PieceType;

                pieceManager.AbsorbPiece(selectedPiece, targetPiece);
                pieceManager.RemovePiece(targetPiece);

                isAbsorbMode = false;
                UpdateAbsorbButtonText();

                Debug.Log($"흡수 성공: {absorbedType} 데이터를 복사했습니다.");
            }
            else
            {
                // King은 여기로 들어와서 흡수 없이 제거됨
                pieceManager.RemovePiece(targetPiece);
            }
        }

        // 선택한 기물을 해당 타일로 이동
        pieceManager.MovePiece(selectedPiece, tile.X, tile.Y);

        // 이동/공격 후 승패 조건 확인
        CheckBattleEnd();

        // 전투가 끝났으면 턴 종료하지 않음
        if (isBattleEnded)
        {
            return;
        }

        // 이동 후 턴 종료
        EndTurn();
    }

    // 일반 전투에서 기권하는 함수
    public void Surrender()
    {
        // 이미 끝난 전투면 무시
        if (isBattleEnded)
        {
            return;
        }

        // 일반 전투 기권은 패배 처리
        EndBattle(BattleResult.Lose);

        Debug.Log("기권: 일반 전투 패배 / 보상 없음 / 받은 피해와 사망 상태 유지");
    }

    // 흡수 모드를 켜고 끄는 함수
    public void ToggleAbsorbMode()
    {
        // 플레이어 턴이 아니면 사용 불가
        if (currentTurn != BattleTurn.Player)
        {
            Debug.Log("흡수는 플레이어 턴에만 사용할 수 있습니다.");
            return;
        }

        // 흡수 모드 상태 반전
        isAbsorbMode = !isAbsorbMode;

        // 버튼 텍스트 갱신
        UpdateAbsorbButtonText();

        // 상태 로그 출력
        Debug.Log(isAbsorbMode ? "흡수 모드 ON" : "흡수 모드 OFF");
    }

    // 현재 선택된 기물의 고유 스킬을 사용하는 함수
    public void UseSelectedPieceSkill()
    {
        // 전투가 끝났으면 스킬 사용 불가
        if (isBattleEnded)
        {
            return;
        }

        // 선택된 기물이 없으면 스킬 사용 불가
        if (selectedPiece == null)
        {
            Debug.Log("스킬을 사용할 기물을 먼저 선택해야 합니다.");
            return;
        }

        // 현재 턴의 기물이 아니면 스킬 사용 불가
        if (IsCurrentTurnPiece(selectedPiece) == false)
        {
            Debug.Log("현재 턴의 기물만 스킬을 사용할 수 있습니다.");
            return;
        }

        // <변경부분> 이번 턴에 이미 고유 스킬을 사용했으면 모든 기물 고유 스킬 사용 불가
        if (hasUsedUniqueSkillThisTurn)
        {
            Debug.Log("이번 턴에는 이미 고유 스킬을 사용했습니다.");
            return;
        }

        // <변경부분> 선택된 기물의 고유 스킬 사용 가능 여부 확인
        // 여기서는 고유 스킬 없음 / 개별 쿨타임 여부를 검사
        if (selectedPiece.CanUseUniqueSkill() == false)
        {
            Debug.Log("고유 스킬을 사용할 수 없습니다. 쿨타임 중이거나 사용할 수 없는 스킬입니다.");
            return;
        }

        // <변경부분> 실제 스킬 성공 여부 저장
        bool skillUsed = false;

        // 선택된 기물의 고유 스킬 종류에 따라 실행
        switch (selectedPiece.UniqueSkill)
        {
            case UniqueSkillType.JelluMultiply:
                // 젤루 증식 스킬 실행
                skillUsed = UseJelluMultiply(selectedPiece);
                break;

            default:
                // 실행 가능한 고유 스킬이 없으면 실패 처리
                Debug.Log("사용할 수 있는 고유 스킬이 없습니다.");
                break;
        }

        // <변경부분> 스킬이 실제로 성공했을 때만 턴 사용권과 쿨타임 적용
        if (skillUsed)
        {
            // 이번 턴 전체 고유 스킬 사용 완료 처리
            hasUsedUniqueSkillThisTurn = true;

            // 선택된 기물에 고유 스킬 쿨타임 적용
            selectedPiece.MarkUniqueSkillUsed();

            // 고유 스킬 사용 완료 로그
            Debug.Log("고유 스킬 사용 완료: 이번 턴 고유 스킬 사용권 소모 / 선택 기물 쿨타임 적용");
        }
    }

    // <변경부분> Jellu 폰 고유 스킬: 성공 여부를 bool로 반환
    private bool UseJelluMultiply(Piece piece)
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                int targetX = piece.X + offsetX;
                int targetY = piece.Y + offsetY;

                if (IsInsideBoard(targetX, targetY) == false)
                {
                    continue;
                }

                if (pieceManager.IsEmpty(targetX, targetY))
                {
                    emptyPositions.Add(new Vector2Int(targetX, targetY));
                }
            }
        }

        if (emptyPositions.Count == 0)
        {
            Debug.Log("증식 실패: 인접한 빈칸이 없습니다.");
            return false; // <변경부분> 실패했으므로 쿨타임 없음
        }

        int randomIndex = Random.Range(0, emptyPositions.Count);
        Vector2Int selectedPosition = emptyPositions[randomIndex];

        Piece clonedPiece = pieceManager.ClonePieceTo(
            piece,
            selectedPosition.x,
            selectedPosition.y
        );

        if (clonedPiece != null)
        {
            Debug.Log($"증식 성공: ({selectedPosition.x}, {selectedPosition.y})에 {piece.Team} {piece.PieceType} 생성");
            return true; // <변경부분> 성공했으므로 쿨타임 적용
        }

        return false; // <변경부분> 생성 실패 시 쿨타임 없음
    }

    // 선택한 기물의 종류에 따라 이동 가능한 타일을 표시하는 함수
    private void ShowMovableTiles(Piece piece)
    {
        // 기물이 없으면 종료
        if (piece == null)
        {
            return;
        }

        //기물 종류별 이동 규칙 분기
        switch (piece.PieceType)
        {
            case PieceType.Pawn:
                // 폰 이동/공격 표시
                ShowPawnMovableTiles(piece);
                break;

            case PieceType.Rook:
                // 룩 이동/공격 표시
                ShowRookMovableTiles(piece);
                break;

            case PieceType.Bishop:
                // 비숍 이동/공격 표시
                ShowBishopMovableTiles(piece);
                break;

            case PieceType.Knight:
                // 나이트 이동/공격 표시
                ShowKnightMovableTiles(piece);
                break;

            case PieceType.King:
                // 킹 이동/공격 표시
                ShowKingMovableTiles(piece);
                break;
        }
    }

    private void ShowRookMovableTiles(Piece piece)
    {
        // 오른쪽
        CheckLineMovement(piece, 1, 0);

        // 왼쪽
        CheckLineMovement(piece, -1, 0);

        // 위쪽
        CheckLineMovement(piece, 0, 1);

        // 아래쪽
        CheckLineMovement(piece, 0, -1);
    }

    //비숍처럼 대각선 방향으로 이동/공격 가능한 타일 표시
    private void ShowBishopMovableTiles(Piece piece)
    {
        // 오른쪽 위 대각선
        CheckLineMovement(piece, 1, 1);

        // 왼쪽 위 대각선
        CheckLineMovement(piece, -1, 1);

        // 오른쪽 아래 대각선
        CheckLineMovement(piece, 1, -1);

        // 왼쪽 아래 대각선
        CheckLineMovement(piece, -1, -1);
    }

    // 한 방향으로 계속 검사하며 이동/공격 가능한 타일을 찾는 함수
    private void CheckLineMovement(Piece piece, int dirX, int dirY)
    {
        // 현재 기물 위치에서 한 칸 이동한 좌표부터 시작
        int checkX = piece.X + dirX;
        int checkY = piece.Y + dirY;

        // 보드 안쪽인 동안 계속 검사
        while (IsInsideBoard(checkX, checkY))
        {
            // 검사 좌표에 있는 기물 확인
            Piece targetPiece = pieceManager.GetPieceAt(checkX, checkY);

            // 기물이 없는 칸이면 이동 가능
            if (targetPiece == null)
            {
                HighlightTile(checkX, checkY);
            }
            else
            {
                // 적대 기물이 있으면 공격 가능
                if (piece.IsEnemyOf(targetPiece))
                {
                    HighlightTile(checkX, checkY);
                }

                // 기물이 있으면 그 뒤로는 더 이상 이동 불가
                break;
            }

            // 같은 방향으로 다음 칸 검사
            checkX += dirX;
            checkY += dirY;
        }
    }

    // 나이트의 L자 이동/공격 가능한 타일 표시
    private void ShowKnightMovableTiles(Piece piece)
    {
        // 나이트가 이동할 수 있는 8개 상대 좌표
        int[,] knightMoves =
        {
        { 1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 }, { -1, -2 }, { -2, -1 }, { -2, 1 }, { -1, 2 }
    };

        // 8개 좌표를 하나씩 검사
        for (int i = 0; i < knightMoves.GetLength(0); i++)
        {
            // 이동할 좌표 계산
            int targetX = piece.X + knightMoves[i, 0];
            int targetY = piece.Y + knightMoves[i, 1];

            // 단일 칸 이동/공격 가능 여부 검사
            CheckSingleTileMovement(piece, targetX, targetY);
        }
    }

    // 킹의 주변 8칸 이동/공격 가능한 타일 표시
    private void ShowKingMovableTiles(Piece piece)
    {
        // 주변 8칸 검사
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // 자기 위치는 제외
                if (x == 0 && y == 0)
                {
                    continue;
                }

                // 이동할 좌표 계산
                int targetX = piece.X + x;
                int targetY = piece.Y + y;

                // 단일 칸 이동/공격 가능 여부 검사
                CheckSingleTileMovement(piece, targetX, targetY);
            }
        }
    }

    // <변경부분> 한 칸짜리 이동/공격 가능 여부를 검사하는 함수
    private void CheckSingleTileMovement(Piece piece, int x, int y)
    {
        // 보드 밖이면 종료
        if (IsInsideBoard(x, y) == false)
        {
            return;
        }

        // 해당 좌표의 기물 확인
        Piece targetPiece = pieceManager.GetPieceAt(x, y);

        // 비어 있는 칸이면 이동 가능
        if (targetPiece == null)
        {
            HighlightTile(x, y);
            return;
        }

        // 적대 기물이 있으면 공격 가능
        if (piece.IsEnemyOf(targetPiece))
        {
            HighlightTile(x, y);
        }
    }

    private void ShowPawnMovableTiles(Piece piece) // Pawn의 이동 가능 타일 표시
    {
        // 플레이어는 위쪽(y + 1), 적은 아래쪽(y - 1)으로 이동
        int direction = piece.Team == PieceTeam.Player ? 1 : -1;

        // 전진 좌표
        int forwardX = piece.X;
        int forwardY = piece.Y + direction;

        // 앞칸이 비어 있으면 이동 가능
        if (IsInsideBoard(forwardX, forwardY) && pieceManager.IsEmpty(forwardX, forwardY))
        {
            HighlightTile(forwardX, forwardY);
        }

        // 왼쪽 대각선 공격 좌표
        CheckPawnAttackTile(piece, piece.X - 1, piece.Y + direction);

        // 오른쪽 대각선 공격 좌표
        CheckPawnAttackTile(piece, piece.X + 1, piece.Y + direction);
    }

    // Pawn이 공격 가능한 대각선 타일인지 확인
    private void CheckPawnAttackTile(Piece piece, int x, int y)
    {
        // 보드 밖이면 종료
        if (IsInsideBoard(x, y) == false)
        {
            return;
        }

        // 해당 좌표의 기물 확인
        Piece targetPiece = pieceManager.GetPieceAt(x, y);

        //공격 판정 확인용 로그
        Debug.Log($"공격 확인 좌표 ({x}, {y}) / 대상: {targetPiece}");

        // 대상 기물이 있고, 적대 관계라면 공격 가능
        if (targetPiece != null && piece.IsEnemyOf(targetPiece))
        {
            Debug.Log($"공격 가능: {targetPiece.Team} / {targetPiece.PieceType}");

            HighlightTile(x, y);
        }
    }


    public void EndTurn() // 현재 턴을 종료하고 상대 턴으로 넘기는 함수
    {
        //전투가 끝났으면 턴 넘기기 불가
        if (isBattleEnded)
        {
            return;
        }
        // 선택된 기물 해제
        selectedPiece = null;

        // 하이라이트 제거
        ClearHighlights();

        // 턴 종료 시 흡수 모드 해제
        isAbsorbMode = false;

        // 버튼 텍스트 갱신
        UpdateAbsorbButtonText();

        // 현재 턴이 플레이어면 적 턴으로 변경
        if (currentTurn == BattleTurn.Player)
        {
            currentTurn = BattleTurn.Enemy;
            Debug.Log("적 턴 시작");
        }
        // 현재 턴이 적이면 플레이어 턴으로 변경
        else
        {
            currentTurn = BattleTurn.Player;
            Debug.Log("플레이어 턴 시작");
        }
        // 턴 시작 시 고유 스킬 사용 여부 초기화 및 쿨타임 감소
        UpdateAllUniqueSkillTurnState();
    }
 
    //  턴 시작 시 모든 기물의 고유 스킬 상태 갱신
    private void UpdateAllUniqueSkillTurnState()
    {
        // 새 턴이 시작되면 턴 전체 고유 스킬 사용권 초기화
        hasUsedUniqueSkillThisTurn = false;

        // 보드 전체 X 좌표 검사
        for (int x = 0; x < boardManager.Width; x++)
        {
            // 보드 전체 Y 좌표 검사
            for (int y = 0; y < boardManager.Height; y++)
            {
                // 현재 좌표의 기물 가져오기
                Piece piece = pieceManager.GetPieceAt(x, y);

                // 기물이 없으면 다음 칸 검사
                if (piece == null)
                {
                    continue;
                }

                // 현재 턴 고유 스킬 사용 여부 초기화
                piece.ResetUniqueSkillTurnUsage();

                // 고유 스킬 쿨타임 1 감소
                piece.ReduceUniqueSkillCooldown();
            }
        }
    }


    // 특정 좌표의 타일을 하이라이트
    private void HighlightTile(int x, int y)
    {
        // 좌표에 해당하는 타일 가져오기
        Tile tile = boardManager.GetTile(x, y);

        // 타일이 없으면 종료
        if (tile == null)
        {
            return;
        }

        // 타일 하이라이트 표시
        tile.ShowHighlight();

        // 나중에 지우기 위해 하이라이트 목록에 저장
        highlightedTiles.Add(tile);

        // 실제 선택 가능한 타일 목록에도 저장
        selectableTiles.Add(tile);
    }

    // 기존 하이라이트 제거
    private void ClearHighlights()
    {
        // 하이라이트된 타일 전부 원래 색으로 복구
        foreach (Tile tile in highlightedTiles)
        {
            tile.HideHighlight();
        }

        // 하이라이트 목록 비우기
        highlightedTiles.Clear();

        // 선택 가능 타일 목록도 비우기
        selectableTiles.Clear();
    }

    // 현재 선택된 기물이 있는지 반환하는 함수
    public bool HasSelectedPiece()
    {
        // 선택된 기물이 있으면 true
        return selectedPiece != null;
    }

    // 클릭한 기물이 현재 턴에 조작 가능한 기물인지 확인하는 함수
    public bool IsCurrentTurnPiece(Piece piece)
    {
        // 기물이 없으면 false
        if (piece == null)
        {
            return false;
        }

        // 플레이어 턴이면 플레이어 기물만 조작 가능
        if (currentTurn == BattleTurn.Player)
        {
            return piece.Team == PieceTeam.Player;
        }

        // 적 턴이면 적 기물만 조작 가능
        if (currentTurn == BattleTurn.Enemy)
        {
            return piece.Team == PieceTeam.Enemy;
        }

        // 그 외에는 false
        return false;
    }

    // 현재 전투 승패 조건을 확인하는 함수
    private void CheckBattleEnd()
    {
        // 이미 전투가 끝났으면 중복 체크 방지
        if (isBattleEnded)
        {
            return;
        }

        // 상대 King이 제거되었거나, King을 제외한 상대 기물이 전멸하면 승리
        if (pieceManager.HasKing(PieceTeam.Enemy) == false ||
            pieceManager.HasAnyNonKingPiece(PieceTeam.Enemy) == false)
        {
            EndBattle(BattleResult.Win);
            return;
        }

        // 플레이어 King이 사망했거나, King을 제외한 플레이어 기물이 전멸하면 패배
        if (pieceManager.HasKing(PieceTeam.Player) == false ||
            pieceManager.HasAnyNonKingPiece(PieceTeam.Player) == false)
        {
            EndBattle(BattleResult.Lose);
            return;
        }
    }

    // 전투를 종료하는 함수
    private void EndBattle(BattleResult result)
    {
        // 전투 종료 상태 저장
        battleResult = result;
        isBattleEnded = true;

        // 선택 해제
        selectedPiece = null;

        // 하이라이트 제거
        ClearHighlights();

        // 결과 출력
        if (battleResult == BattleResult.Win)
        {
            Debug.Log("전투 승리: 상대 King 제거 또는 상대 기물 전멸");
        }
        else if (battleResult == BattleResult.Lose)
        {
            Debug.Log("일반 전투 패배: 보상 없음 / 받은 피해와 사망 상태 유지");
        }
    }

    // 좌표가 보드 안인지 확인
    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < boardManager.Width && y >= 0 && y < boardManager.Height;
    }
}
