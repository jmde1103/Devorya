using System.Collections.Generic;
using UnityEngine;

// <변경부분> 전투 중 일반스킬과 스킬 발동 판정을 관리하는 매니저
public class BattleSkillManager : MonoBehaviour
{
    // <변경부분> 보드 범위 확인에 사용하는 보드 매니저
    private BoardManager boardManager;

    // <변경부분> 기물 위치 확인과 복제 생성을 담당하는 기물 매니저
    private PieceManager pieceManager;

    // <변경부분> 일반스킬 데이터베이스
    [SerializeField] private GeneralSkillDatabase generalSkillDatabase;

    // <변경부분> BattleManager에서 전투 시작 시 스킬 매니저를 초기화하는 함수
    public void Initialize(BoardManager board, PieceManager pieceManagerRef)
    {
        // 보드 범위 검사용 매니저 저장
        boardManager = board;

        // 기물 복제와 빈칸 확인용 매니저 저장
        pieceManager = pieceManagerRef;
    }

    // <변경부분> ChanceAttack 발동 여부를 행동 시작 전 보유 일반스킬 정보와 GeneralSkillDatabase 기준으로 판정하는 함수
    public bool TryActivateChanceAttack(Piece piece, OwnedGeneralSkillData chanceAttackDataBeforeAction, int chanceAttackContinuousCount)
    {
        // 판정할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 행동 시작 전에 ChanceAttack이 없었다면 이번 처치에서는 발동 불가
        if (chanceAttackDataBeforeAction == null ||
            chanceAttackDataBeforeAction.skillType != GeneralSkillType.ChanceAttack ||
            chanceAttackDataBeforeAction.level <= 0)
        {
            Debug.Log("ChanceAttack 판정 실패: 이번 행동 시작 시점에는 ChanceAttack이 없었습니다.");
            return false;
        }

        // <변경부분> 일반스킬 데이터베이스가 없으면 발동 불가
        if (generalSkillDatabase == null)
        {
            Debug.LogWarning("GeneralSkillDatabase가 연결되지 않아 ChanceAttack을 판정할 수 없습니다.");
            return false;
        }

        // <변경부분> ChanceAttack 설정 데이터 가져오기
        GeneralSkillData chanceAttackData = generalSkillDatabase.GetData(GeneralSkillType.ChanceAttack);

        if (chanceAttackData == null)
        {
            Debug.LogWarning("GeneralSkillDatabase에서 ChanceAttack 데이터를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 데이터베이스에 저장된 레벨별 확률 사용
        int baseChancePercent = chanceAttackData.GetChanceAttackPercent(chanceAttackDataBeforeAction.level);

        // <변경부분> 데이터베이스에 저장된 연속 발동 감소 배율 사용
        float penaltyMultiplier = chanceAttackData.GetChanceAttackContinuousPenaltyMultiplier(chanceAttackContinuousCount);

        // 최종 발동 확률 계산
        float finalChancePercent = baseChancePercent * penaltyMultiplier;

        // 0~100 사이 랜덤값 생성
        float randomValue = Random.Range(0f, 100f);

        // 최종 확률 안에 들어오면 발동 성공
        bool isActivated = randomValue < finalChancePercent;

        Debug.Log($"ChanceAttack 판정: 행동전 LV.{chanceAttackDataBeforeAction.level} / 기본확률 {baseChancePercent}% / 연속횟수 {chanceAttackContinuousCount} / 감소배율 {penaltyMultiplier:F3} / 최종확률 {finalChancePercent:F1}% / 랜덤 {randomValue:F1} / 결과 {isActivated}");

        return isActivated;
    }

    // <변경부분> 고유스킬 종류에 따라 실제 효과를 실행하는 함수
    public bool TryUseUniqueSkill(Piece piece)
    {
        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        switch (piece.UniqueSkill)
        {
            case UniqueSkillType.JelluMultiply:
                return UseJelluMultiply(piece);

            // <변경부분> King 전용 고유스킬: 이번 턴 동안 이동/공격만 Queen처럼 처리
            case UniqueSkillType.KingQueenMove:
                return UseKingQueenMove(piece);

            default:
                Debug.Log("사용할 수 있는 고유 스킬이 없습니다.");
                return false;
        }
    }

    // <변경부분> Jellu 폰 고유 스킬: 인접한 빈칸 중 랜덤 위치에 자신과 같은 정보를 가진 기물을 복제
    private bool UseJelluMultiply(Piece piece)
    {
        // 필요한 매니저가 연결되지 않았으면 스킬 실행 불가
        if (boardManager == null || pieceManager == null)
        {
            Debug.LogWarning("BattleSkillManager 초기화가 완료되지 않아 JelluMultiply를 사용할 수 없습니다.");
            return false;
        }

        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        List<Vector2Int> emptyPositions = new List<Vector2Int>();

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                // 자기 위치는 제외
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                int targetX = piece.X + offsetX;
                int targetY = piece.Y + offsetY;

                // 보드 밖 좌표는 제외
                if (IsInsideBoard(targetX, targetY) == false)
                {
                    continue;
                }

                // 인접한 빈칸만 후보로 저장
                if (pieceManager.IsEmpty(targetX, targetY))
                {
                    emptyPositions.Add(new Vector2Int(targetX, targetY));
                }
            }
        }

        // 인접한 빈칸이 없으면 스킬 실패
        if (emptyPositions.Count == 0)
        {
            Debug.Log("증식 실패: 인접한 빈칸이 없습니다.");
            return false;
        }

        // 후보 빈칸 중 랜덤 위치 선택
        int randomIndex = Random.Range(0, emptyPositions.Count);
        Vector2Int selectedPosition = emptyPositions[randomIndex];

        // 선택된 위치에 현재 기물과 같은 정보를 가진 기물 복제
        Piece clonedPiece = pieceManager.ClonePieceTo(
            piece,
            selectedPosition.x,
            selectedPosition.y
        );

        // 복제 성공 시 스킬 성공 처리
        if (clonedPiece != null)
        {
            Debug.Log($"증식 성공: ({selectedPosition.x}, {selectedPosition.y})에 {piece.Team} {piece.PieceType} 생성");
            return true;
        }

        // 최대 기물 수 제한 등으로 복제에 실패하면 스킬 실패 처리
        return false;
    }

    // <변경부분> King 전용 고유스킬: 실제 타입은 유지하고 이번 턴 동안 이동/공격만 Queen처럼 처리
    private bool UseKingQueenMove(Piece piece)
    {
        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // 실제 기물 타입이 King인 경우에만 사용 가능
        if (piece.PieceType != PieceType.King)
        {
            Debug.Log("KingQueenMove 스킬은 King 타입 기물만 사용할 수 있습니다.");
            return false;
        }

        // <변경부분> 실제 PieceType은 King으로 유지하고 이동/공격 판정만 Queen으로 변경
        piece.SetTemporaryMoveType(PieceType.Queen);

        Debug.Log("KingQueenMove 스킬 성공: 이번 턴 동안 King이 Queen처럼 이동/공격합니다.");

        return true;
    }

    // <변경부분> 특정 좌표가 보드 안쪽인지 확인하는 함수
    private bool IsInsideBoard(int x, int y)
    {
        // 보드 매니저가 없으면 좌표 검사 불가
        if (boardManager == null)
        {
            return false;
        }

        return x >= 0 &&
               x < boardManager.Width &&
               y >= 0 &&
               y < boardManager.Height;
    }
}
