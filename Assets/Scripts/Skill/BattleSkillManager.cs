using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// <변경부분> System.Random과 UnityEngine.Random 이름 충돌 방지
using Random = UnityEngine.Random;

// <변경부분> 전투 중 일반스킬과 스킬 발동 판정을 관리하는 매니저
public class BattleSkillManager : MonoBehaviour
{
    // <변경부분> 보드 범위 확인에 사용하는 보드 매니저
    private BoardManager boardManager;

    // <변경부분> 기물 위치 확인과 복제 생성을 담당하는 기물 매니저
    private PieceManager pieceManager;

    // <변경부분> 일반스킬 데이터베이스
    [SerializeField] private GeneralSkillDatabase generalSkillDatabase;

    // <변경부분> 상태이상 데이터베이스
    // 퇴화 같은 상태이상 기본 지속 턴/중첩 정보를 가져올 때 사용
    [SerializeField] private StatusEffectDatabase statusEffectDatabase;

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

    // <변경부분> Defense 발동 여부를 행동 시작 시점의 방어 스킬 정보와 GeneralSkillDatabase 기준으로 판정하는 함수
    public bool TryActivateDefense(Piece defenderPiece, OwnedGeneralSkillData defenseDataBeforeAction)
    {
        // 방어할 기물이 없으면 실패
        if (defenderPiece == null)
        {
            return false;
        }

        // <변경부분> 공격 시작 시점에 Defense가 없었다면 방어 불가
        if (defenseDataBeforeAction == null ||
            defenseDataBeforeAction.skillType != GeneralSkillType.Defense ||
            defenseDataBeforeAction.level <= 0)
        {
            return false;
        }

        // <변경부분> 일반스킬 데이터베이스가 없으면 방어 판정 불가
        if (generalSkillDatabase == null)
        {
            Debug.LogWarning("GeneralSkillDatabase가 연결되지 않아 Defense를 판정할 수 없습니다.");
            return false;
        }

        // <변경부분> Defense 설정 데이터 가져오기
        GeneralSkillData defenseData = generalSkillDatabase.GetData(GeneralSkillType.Defense);

        if (defenseData == null)
        {
            Debug.LogWarning("GeneralSkillDatabase에서 Defense 데이터를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 데이터베이스에 저장된 레벨별 확률 사용
        int defenseChancePercent = defenseData.GetDefensePercent(defenseDataBeforeAction.level);

        // 확률이 0 이하이면 방어 실패
        if (defenseChancePercent <= 0)
        {
            return false;
        }

        // 0~100 사이 랜덤값 생성
        float randomValue = Random.Range(0f, 100f);

        // 최종 확률 안에 들어오면 방어 성공
        bool isActivated = randomValue < defenseChancePercent;

        Debug.Log($"Defense 판정: 행동전 LV.{defenseDataBeforeAction.level} / 확률 {defenseChancePercent}% / 랜덤 {randomValue:F1} / 결과 {isActivated}");

        return isActivated;
    }

    // <변경부분> Insight 발동 여부를 행동 시작 시점의 간파 스킬 정보와 GeneralSkillDatabase 기준으로 판정하는 함수
    // targetCanceledSkillType은 이번에 무효화하려는 상대 일반스킬 타입이다.
    // 현재는 Defense를 대상으로 사용하고, 나중에 Evasion 추가 시 같은 함수로 확장 가능하다.
    public bool TryActivateInsight(Piece attackerPiece, OwnedGeneralSkillData insightDataBeforeAction, GeneralSkillType targetCanceledSkillType)
    {
        // 공격자가 없으면 간파 불가
        if (attackerPiece == null)
        {
            return false;
        }

        // <변경부분> 행동 시작 시점에 Insight가 없었다면 이번 공격에서는 발동 불가
        if (insightDataBeforeAction == null ||
            insightDataBeforeAction.skillType != GeneralSkillType.Insight ||
            insightDataBeforeAction.level <= 0)
        {
            return false;
        }

        // <변경부분> 현재 간파가 무효화할 수 있는 스킬만 허용
        // 회피는 아직 구현되지 않았으므로 지금은 Defense만 처리
        if (targetCanceledSkillType != GeneralSkillType.Defense)
        {
            return false;
        }

        // <변경부분> 일반스킬 데이터베이스가 없으면 간파 판정 불가
        if (generalSkillDatabase == null)
        {
            Debug.LogWarning("GeneralSkillDatabase가 연결되지 않아 Insight를 판정할 수 없습니다.");
            return false;
        }

        // <변경부분> Insight 설정 데이터 가져오기
        GeneralSkillData insightData = generalSkillDatabase.GetData(GeneralSkillType.Insight);

        if (insightData == null)
        {
            Debug.LogWarning("GeneralSkillDatabase에서 Insight 데이터를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 데이터베이스에 저장된 레벨별 확률 사용
        int insightChancePercent = insightData.GetInsightPercent(insightDataBeforeAction.level);

        // 확률이 0 이하이면 간파 실패
        if (insightChancePercent <= 0)
        {
            return false;
        }

        // 0~100 사이 랜덤값 생성
        float randomValue = Random.Range(0f, 100f);

        // 최종 확률 안에 들어오면 간파 성공
        bool isActivated = randomValue < insightChancePercent;

        Debug.Log($"Insight 판정: 행동전 LV.{insightDataBeforeAction.level} / 대상 {targetCanceledSkillType} / 확률 {insightChancePercent}% / 랜덤 {randomValue:F1} / 결과 {isActivated}");

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
            case UniqueSkillType.JelluClone:
                return UseJelluClone(piece);

            case UniqueSkillType.JelluMultiply:
                return UseJelluMultiply(piece);

            case UniqueSkillType.KingQueenMove:
                return UseKingQueenMove(piece);

            case UniqueSkillType.JelluSynthesis:
                return UseJelluSynthesis(piece);

            case UniqueSkillType.JelluWall:
                return UseJelluWall(piece);

            case UniqueSkillType.JelluDegeneration:
                return UseJelluDegeneration(piece);

            // <변경부분> 뿔 박치기: 물/늪 타일 위에서 자신에게 돌파 상태 1턴 부여
            case UniqueSkillType.HornHeadbutt:
                return UseHornHeadbutt(piece);
        }

        // <변경부분> 처리할 수 없는 고유스킬이면 스킬 사용 실패 처리
        return false;
    }

    // <변경부분> 고유스킬을 코루틴으로 실행하는 함수
    // 합성처럼 애니메이션 종료 후 실제 효과가 적용되어야 하는 스킬을 처리하기 위해 사용
    public IEnumerator TryUseUniqueSkillRoutine(Piece piece, Action<bool> onComplete)
    {
        // 스킬을 사용할 기물이 없으면 실패 처리
        if (piece == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        bool skillUsed = false;

        switch (piece.UniqueSkill)
        {
            // <변경부분> 젤루 합성은 재료 이동 연출 후 승급되어야 하므로 코루틴으로 처리
            case UniqueSkillType.JelluSynthesis:
                yield return UseJelluSynthesisRoutine(piece, result => skillUsed = result);
                onComplete?.Invoke(skillUsed);
                yield break;

            // <변경부분> 나머지 고유스킬은 기존 즉시 실행 함수를 그대로 사용
            default:
                skillUsed = TryUseUniqueSkill(piece);
                onComplete?.Invoke(skillUsed);
                yield break;
        }
    }

    /// <변경부분> 복제 스킬: 인접한 빈칸 중 랜덤 위치에 자신과 같은 정보를 가진 기물을 복제
    private bool UseJelluClone(Piece piece)
    {
        // 필요한 매니저가 연결되지 않았으면 스킬 실행 불가
        if (boardManager == null || pieceManager == null)
        {
            Debug.LogWarning("BattleSkillManager 초기화가 완료되지 않아 JelluMClone를 사용할 수 없습니다.");
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
            Debug.Log("복제 실패: 인접한 빈칸이 없습니다.");
            return false;
        }

        // 후보 빈칸 중 랜덤 위치 선택
        int randomIndex = Random.Range(0, emptyPositions.Count);
        Vector2Int selectedPosition = emptyPositions[randomIndex];

        // <변경부분> 복제 기물이 시전자 위치에서 생성 위치까지 포물선으로 이동하도록 생성
        Piece clonedPiece = pieceManager.ClonePieceToFromSource(
            piece,
            selectedPosition.x,
            selectedPosition.y
        );

        // 복제 성공 시 스킬 성공 처리
        if (clonedPiece != null)
        {
            Debug.Log($"복제 성공: ({selectedPosition.x}, {selectedPosition.y})에 {piece.Team} {piece.PieceType} 복제");
            return true;
        }

        // 최대 기물 수 제한 등으로 복제에 실패하면 스킬 실패 처리
        return false;
    }

    // <변경부분> 젤루 폰 고유스킬: 코루틴 실행 전용 안내 함수
    // 실제 합성은 재료 이동 애니메이션을 기다려야 하므로 UseJelluSynthesisRoutine에서 처리
    private bool UseJelluSynthesis(Piece piece)
    {
        Debug.LogWarning("JelluSynthesis는 코루틴 기반 스킬입니다. TryUseUniqueSkillRoutine을 통해 실행해야 합니다.");
        return false;
    }

    // <변경부분> 젤루 폰 고유스킬: 인접한 아군/중립 젤루 태그 기물 2개가 Pawn으로 이동한 뒤 랜덤 상위 젤루 기물로 승급
    private IEnumerator UseJelluSynthesisRoutine(Piece piece, Action<bool> onComplete)
    {
        // 필요한 매니저가 연결되지 않았으면 스킬 실행 불가
        if (boardManager == null || pieceManager == null)
        {
            Debug.LogWarning("BattleSkillManager 초기화가 완료되지 않아 JelluSynthesis를 사용할 수 없습니다.");
            onComplete?.Invoke(false);
            yield break;
        }

        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분> 젤루 합성은 젤루 Pawn 전용 스킬
        if (piece.PieceType != PieceType.Pawn)
        {
            Debug.Log("젤루 합성 실패: Pawn 타입만 사용할 수 있습니다.");
            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분> 스킬 사용자도 젤루 태그를 가지고 있어야 함
        if (piece.HasSpeciesTag(PieceSpeciesTag.Jellu) == false)
        {
            Debug.Log("젤루 합성 실패: 젤루 태그가 없는 기물입니다.");
            onComplete?.Invoke(false);
            yield break;
        }

        List<Piece> synthesisCandidates = new List<Piece>();

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

                Piece candidatePiece = pieceManager.GetPieceAt(targetX, targetY);

                // 인접 칸에 기물이 없으면 제외
                if (candidatePiece == null)
                {
                    continue;
                }

                // <변경부분> 젤루 태그가 없는 기물은 합성 소재에서 제외
                if (candidatePiece.HasSpeciesTag(PieceSpeciesTag.Jellu) == false)
                {
                    continue;
                }

                // <변경부분> King은 승패 조건이 꼬일 수 있으므로 합성 소재에서 제외
                if (candidatePiece.PieceType == PieceType.King)
                {
                    continue;
                }

                // <변경부분> 합성 소재는 아군 또는 중립 젤루 태그 기물만 허용
                if (candidatePiece.Team != piece.Team &&
                    candidatePiece.Team != PieceTeam.Neutral)
                {
                    continue;
                }

                synthesisCandidates.Add(candidatePiece);
            }
        }

        // <변경부분> 합성 소재가 2개 미만이면 스킬 실패
        if (synthesisCandidates.Count < 2)
        {
            Debug.Log($"젤루 합성 실패: 인접한 아군/중립 젤루 소재가 부족합니다. 현재 {synthesisCandidates.Count}개 / 필요 2개");
            onComplete?.Invoke(false);
            yield break;
        }

        List<Piece> selectedMaterials = new List<Piece>();

        // <변경부분> 인접한 젤루 소재 중 2개를 중복 없이 랜덤 선택
        for (int i = 0; i < 2; i++)
        {
            int randomIndex = Random.Range(0, synthesisCandidates.Count);
            selectedMaterials.Add(synthesisCandidates[randomIndex]);
            synthesisCandidates.RemoveAt(randomIndex);
        }

        // <변경부분> 현재 고유스킬이 준비된 젤루 상위 기물 중 랜덤 승급
        // Rook은 전용 스킬이 생기기 전까지 후보에서 제외
        // Queen은 현재 실물/흡수/UI 스프라이트가 없으면 제외 유지
        List<PieceType> promotionTypes = new List<PieceType>
    {
        PieceType.Knight,
        PieceType.Bishop
    };

        PieceType selectedPromotionType = promotionTypes[Random.Range(0, promotionTypes.Count)];

        // <변경부분> 승급 타입에 맞는 젤루 고유스킬 결정
        UniqueSkillType promotedUniqueSkill = GetJelluPromotionUniqueSkill(selectedPromotionType);

        // <변경부분> 선택된 재료 2개가 스킬을 사용한 Pawn 위치로 포물선 이동
        yield return pieceManager.PlaySynthesisMaterialMoveAnimation(
            selectedMaterials[0],
            selectedMaterials[1],
            piece
        );

        // 연출 중 스킬 사용자 Pawn이 사라졌으면 실패
        if (piece == null)
        {
            Debug.LogWarning("젤루 합성 실패: 연출 중 스킬 사용자가 사라졌습니다.");
            onComplete?.Invoke(false);
            yield break;
        }

        // <변경부분> 선택된 소재 2개 제거
        foreach (Piece materialPiece in selectedMaterials)
        {
            if (materialPiece != null)
            {
                pieceManager.RemovePiece(materialPiece);
            }
        }

        // <변경부분> 나중에 Spine 승급 애니메이션을 연결할 자리
        yield return PlayJelluSynthesisPromotionEffect(piece);

        // <변경부분> 스킬을 사용한 젤루 Pawn을 랜덤 상위 젤루 기물로 승급
        // 승급 후에는 승급 타입에 맞는 젤루 고유스킬을 부여
        bool promoteSuccess = pieceManager.PromotePieceToJelluType(piece, selectedPromotionType, promotedUniqueSkill);

        if (promoteSuccess == false)
        {
            Debug.LogWarning("젤루 합성 실패: 승급 처리에 실패했습니다.");
            onComplete?.Invoke(false);
            yield break;
        }

        Debug.Log($"젤루 합성 성공: 아군/중립 젤루 소재 2개 이동 및 제거 후 {selectedPromotionType}으로 승급");

        onComplete?.Invoke(true);
    }

    // <변경부분> 젤루 합성 승급 연출 자리
    // 지금은 임시 대기만 넣고, 나중에 Spine 승급 애니메이션을 이 함수 안에 연결하면 됨
    private IEnumerator PlayJelluSynthesisPromotionEffect(Piece piece)
    {
        // 승급할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // <변경부분> 나중에 Spine 승급 애니메이션 호출 위치
        // 예시:
        // yield return pieceSpineController.PlayPromotionAnimation(piece);

        // 현재는 승급 타이밍이 너무 즉시 바뀌지 않도록 짧은 임시 대기만 적용
        yield return new WaitForSeconds(0.15f);
    }

    // <변경부분> 젤루 합성 승급 타입에 맞는 고유스킬을 반환하는 함수
    private UniqueSkillType GetJelluPromotionUniqueSkill(PieceType promotedType)
    {
        switch (promotedType)
        {
            // <변경부분> 젤루 Knight 고유스킬: 퇴화
            case PieceType.Knight:
                return UniqueSkillType.JelluDegeneration;

            // <변경부분> 젤루 Bishop 고유스킬: 젤루 벽
            // 기존 Rook 스킬이 Bishop 스킬로 이동했으므로 Bishop에게 부여
            case PieceType.Bishop:
                return UniqueSkillType.JelluWall;
        }

        // <변경부분> 아직 고유스킬이 정해지지 않은 승급 타입은 None
        // Rook 전용 스킬이 생기면 여기 case를 추가하면 됨
        return UniqueSkillType.None;
    }

    // <변경부분> 증식 스킬: 인접한 빈칸 중 랜덤 위치에 젤루 Pawn을 생성
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

        // <변경부분> 젤루 Pawn이 시전자 위치에서 생성 위치까지 포물선으로 이동하도록 생성
        Piece createdPawn = pieceManager.SpawnJelluPawnFromSource(
            piece,
            piece.Team,
            selectedPosition.x,
            selectedPosition.y
        );

        // 젤루 Pawn 생성 성공 시 스킬 성공 처리
        if (createdPawn != null)
        {
            Debug.Log($"증식 성공: ({selectedPosition.x}, {selectedPosition.y})에 {piece.Team} 젤루 Pawn 생성");
            return true;
        }

        // 최대 기물 수 제한 등으로 생성에 실패하면 스킬 실패 처리
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

    // <변경부분> 젤루 벽 스킬: 젤루 Rook의 진행방향 1칸 앞에 중립 Special 벽을 생성
    private bool UseJelluWall(Piece piece)
    {
        // 필요한 매니저가 연결되지 않았으면 스킬 실행 불가
        if (boardManager == null || pieceManager == null)
        {
            Debug.LogWarning("BattleSkillManager 초기화가 완료되지 않아 JelluWall을 사용할 수 없습니다.");
            return false;
        }

        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 젤루 벽은 특정 PieceType에 고정하지 않음
        // 어떤 기물이든 JelluWall 고유스킬을 가지고 있고, 젤루 태그가 있으면 사용할 수 있음
        if (piece.HasSpeciesTag(PieceSpeciesTag.Jellu) == false)
        {
            Debug.Log("젤루 벽 실패: 젤루 태그가 없는 기물입니다.");
            return false;
        }

        // <변경부분> 중립 기물은 스킬 사용자로 허용하지 않음
        if (piece.Team == PieceTeam.Neutral)
        {
            Debug.Log("젤루 벽 실패: 중립 기물은 사용할 수 없습니다.");
            return false;
        }

        // <변경부분> 진행방향 계산
        // Player는 위쪽으로 전진하므로 Y + 1
        // Enemy는 아래쪽으로 전진하므로 Y - 1
        int directionY = piece.Team == PieceTeam.Player ? 1 : -1;

        int targetX = piece.X;
        int targetY = piece.Y + directionY;

        // 보드 밖이면 실패
        if (IsInsideBoard(targetX, targetY) == false)
        {
            Debug.Log($"젤루 벽 실패: 생성 위치가 보드 밖입니다. ({targetX}, {targetY})");
            return false;
        }

        // 앞칸에 이미 기물이 있으면 실패
        if (pieceManager.IsEmpty(targetX, targetY) == false)
        {
            Debug.Log($"젤루 벽 실패: 앞칸에 이미 기물이 있습니다. ({targetX}, {targetY})");
            return false;
        }

        // <변경부분> 젤루 벽이 시전자 위치에서 생성 위치까지 포물선으로 이동하도록 생성
        Piece wallPiece = pieceManager.SpawnJelluWallFromSource(piece, targetX, targetY);

        // 생성 실패 시 스킬 실패
        if (wallPiece == null)
        {
            Debug.LogWarning("젤루 벽 실패: 벽 생성에 실패했습니다.");
            return false;
        }

        Debug.Log($"젤루 벽 성공: ({targetX}, {targetY})에 중립 젤루 벽 생성");

        return true;
    }

    // <변경부분> 퇴화 스킬: 젤루 Knight가 자기 자신에게 퇴화 상태이상을 1개 얻음
    private bool UseJelluDegeneration(Piece piece)
    {
        // 스킬을 사용할 기물이 없으면 실패
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 퇴화는 Knight 전용 스킬
        if (piece.PieceType != PieceType.Knight)
        {
            Debug.Log("퇴화 실패: Knight 타입만 사용할 수 있습니다.");
            return false;
        }

        // <변경부분> 젤루 태그를 가진 Knight만 사용할 수 있음
        if (piece.HasSpeciesTag(PieceSpeciesTag.Jellu) == false)
        {
            Debug.Log("퇴화 실패: 젤루 태그가 없는 Knight입니다.");
            return false;
        }

        // 중립 기물은 고유스킬 사용자로 허용하지 않음
        if (piece.Team == PieceTeam.Neutral)
        {
            Debug.Log("퇴화 실패: 중립 기물은 사용할 수 없습니다.");
            return false;
        }

        // 상태이상 데이터베이스가 없으면 상태이상 부여 불가
        if (statusEffectDatabase == null)
        {
            Debug.LogWarning("StatusEffectDatabase가 연결되지 않아 퇴화 상태이상을 부여할 수 없습니다.");
            return false;
        }

        // <변경부분> 퇴화 상태이상 데이터 가져오기
        StatusEffectData degenerationData = statusEffectDatabase.GetData(StatusEffectType.Degeneration);

        if (degenerationData == null)
        {
            Debug.LogWarning("StatusEffectDatabase에서 Degeneration 데이터를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 자기 자신에게 퇴화 상태이상 부여
        piece.AddStatusEffect(degenerationData);

        Debug.Log($"{piece.Team} {piece.PieceType}에게 퇴화 상태이상을 부여했습니다.");

        return true;
    }

    // <변경부분> 뿔 박치기 고유스킬
    // 스킬을 사용하는 기물이 Water 또는 Swamp 타일 위에 있을 때 자신에게 Breakthrough 상태이상 1턴을 부여
    private bool UseHornHeadbutt(Piece piece)
    {
        // 필요한 매니저가 연결되지 않았으면 스킬 실행 불가
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 현재 기물이 올라간 타일 정보가 없으면 스킬 실패
        if (piece.CurrentTile == null)
        {
            Debug.Log("뿔 박치기 실패: 현재 타일 정보를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 물 또는 늪 타일 위에서만 사용 가능
        if (piece.CurrentTile.TileType != TileType.Water &&
            piece.CurrentTile.TileType != TileType.Swamp)
        {
            Debug.Log($"뿔 박치기 실패: 현재 타일이 {piece.CurrentTile.TileType}입니다. Water 또는 Swamp 타일에서만 사용할 수 있습니다.");
            return false;
        }

        // <변경부분> 돌파 상태이상 데이터가 없으면 스킬 실패
        if (statusEffectDatabase == null)
        {
            Debug.LogWarning("StatusEffectDatabase가 연결되지 않아 뿔 박치기를 사용할 수 없습니다.");
            return false;
        }

        // <변경부분> 돌파 상태이상 데이터 가져오기
        StatusEffectData breakthroughData = statusEffectDatabase.GetData(StatusEffectType.Breakthrough);

        if (breakthroughData == null)
        {
            Debug.LogWarning("StatusEffectDatabase에서 Breakthrough 데이터를 찾을 수 없습니다.");
            return false;
        }

        // <변경부분> 자신에게 돌파 상태이상 부여
        piece.AddStatusEffect(breakthroughData);

        Debug.Log($"뿔 박치기 성공: {piece.Team} {piece.PieceType}에게 Breakthrough 상태이상 1턴 부여");

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
