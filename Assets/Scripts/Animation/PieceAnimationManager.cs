using System.Collections;
using UnityEngine;

// <변경부분> 기물의 시각적 이동/공격/스킬 생성/방어 연출만 담당하는 매니저
// 좌표 변경, pieces 배열 변경, 기물 생성/삭제는 PieceManager가 계속 담당한다.
public class PieceAnimationManager : MonoBehaviour
{
    [Header("Piece Move Animation")]
    // <변경부분> 기물이 이동할 때 점프 이동하는 시간
    [SerializeField] private float moveAnimationDuration = 0.25f;

    // <변경부분> 기물이 이동 중 위로 떠오르는 높이
    [SerializeField] private float moveJumpHeight = 0.35f;


    [Header("Piece Attack Animation")]
    // <변경부분> 공격 연출 중 공격자가 항상 위에 보이도록 임시 적용할 Sorting Order
    [SerializeField] private int attackAnimationSortingOrder = 10000;

    // <변경부분> 공격자가 상대 기물 위쪽에 도착할 높이
    [SerializeField] private float attackRiseHeight = 0.45f;

    // <변경부분> 현재 위치에서 상대 기물 위쪽까지 포물선으로 이동하는 시간
    [SerializeField] private float attackMoveDuration = 0.18f;

    // <변경부분> 상대 위로 이동하는 동안 추가로 그려지는 포물선 높이
    [SerializeField] private float attackMoveArcHeight = 0.25f;

    // <변경부분> 상대 기물 위에 도착한 뒤 잠깐 멈춰 있는 시간
    [SerializeField] private float attackHoverWaitDuration = 0.08f;

    // <변경부분> 내려찍기 직전에 살짝 더 올라가는 높이
    [SerializeField] private float attackExtraRiseHeight = 0.18f;

    // <변경부분> 내려찍기 전 추가 상승 시간
    [SerializeField] private float attackExtraRiseDuration = 0.08f;

    // <변경부분> 위에서 아래로 내려찍는 시간
    [SerializeField] private float attackSlamDuration = 0.08f;


    [Header("Defense Bounce Animation")]
    // <변경부분> 방어 성공 시 공격자가 원래 위치에 도달하지 못하고 앞쪽에 떨어지는 거리
    // 값이 클수록 원래 위치에서 타겟 방향으로 더 앞에 떨어짐
    [SerializeField] private float defenseFallShortDistance = 0.25f;

    // <변경부분> 방어에 막힌 지점에서 원래 위치 앞쪽 착지 지점까지 튕겨나가는 시간
    [SerializeField] private float defenseFallBackDuration = 0.12f;

    // <변경부분> 첫 번째로 크게 튀며 원래 위치 쪽으로 이동하는 시간
    [SerializeField] private float defenseFirstBounceDuration = 0.13f;

    // <변경부분> 첫 번째 큰 튐 높이
    [SerializeField] private float defenseFirstBounceHeight = 0.22f;

    // <변경부분> 두 번째로 작게 튀며 원래 위치 쪽으로 이동하는 시간
    [SerializeField] private float defenseSecondBounceDuration = 0.11f;

    // <변경부분> 두 번째 작은 튐 높이
    [SerializeField] private float defenseSecondBounceHeight = 0.11f;

    // <변경부분> 마지막으로 원래 위치에 정확히 복귀하는 시간
    [SerializeField] private float defenseFinalReturnDuration = 0.08f;


    [Header("Skill Spawn Animation")]
    // <변경부분> 스킬로 생성된 기물이 시전자 위치에서 생성 위치까지 날아가는 시간
    [SerializeField] private float skillSpawnAnimationDuration = 0.22f;

    // <변경부분> 스킬 생성 기물이 이동 중 그리는 포물선 높이
    [SerializeField] private float skillSpawnArcHeight = 0.3f;


    [Header("Jellu Synthesis Animation")]
    // <변경부분> 젤루 합성 재료 기물이 Pawn 위치로 모이는 시간
    [SerializeField] private float synthesisMaterialAnimationDuration = 0.22f;

    // <변경부분> 젤루 합성 재료 기물이 이동 중 그리는 포물선 높이
    [SerializeField] private float synthesisMaterialArcHeight = 0.3f;


    [Header("Attack Impact Feedback")]
    // <변경부분> 화면 흔들림 대상. 비어 있으면 Main Camera 사용
    [SerializeField] private Transform cameraShakeTarget;

    // <변경부분> 공격/방어 충격 시 화면 흔들림 시간
    [SerializeField] private float cameraShakeDuration = 0.12f;

    // <변경부분> 공격/방어 충격 시 화면 흔들림 세기
    [SerializeField] private float cameraShakeStrength = 0.06f;

    // <변경부분> 모바일 빌드에서 공격/방어 충격 시 진동 사용 여부
    [SerializeField] private bool enableMobileVibration = true;


    // <변경부분> 기물이 시작 위치에서 목표 위치까지 살짝 떠서 이동하는 기본 이동 연출
    public IEnumerator PlayPieceJumpMoveAnimation(Piece piece, Vector3 targetPosition)
    {
        // 이동할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // 시작 위치 저장
        Vector3 startPosition = piece.transform.position;

        // 연출 시간이 0 이하이면 즉시 이동
        if (moveAnimationDuration <= 0f)
        {
            piece.transform.position = targetPosition;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < moveAnimationDuration)
        {
            // 기물이 연출 중 제거되었으면 중단
            if (piece == null)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;

            // 0~1 이동 진행률
            float normalizedTime = Mathf.Clamp01(elapsedTime / moveAnimationDuration);

            // 시작 위치에서 목표 위치까지 선형 이동
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, normalizedTime);

            // 중간 지점에서 가장 높게 떠오르는 포물선 높이 계산
            float jumpOffset = Mathf.Sin(normalizedTime * Mathf.PI) * moveJumpHeight;

            // Y축으로 점프 높이 적용
            currentPosition.y += jumpOffset;

            // 실제 시각 위치 적용
            piece.transform.position = currentPosition;

            yield return null;
        }

        // 연출 종료 후 정확한 목표 위치로 보정
        if (piece != null)
        {
            piece.transform.position = targetPosition;
        }
    }


    // <변경부분> 공격/흡수 연출용 단계형 이동 함수
    // 보드 좌표는 갱신하지 않고, 기물 Transform만 목표 월드 위치까지 이동시킨다.
    public IEnumerator PlayPieceAttackMoveAnimation(Piece piece, Vector3 targetWorldPosition)
    {
        // 이동할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // 공격 연출 중 공격자가 타겟 뒤에 가려지지 않도록 Sorting Order 임시 상승
        SpriteRenderer attackPieceRenderer = piece.GetComponent<SpriteRenderer>();
        int originalSortingOrder = 0;
        bool changedSortingOrder = false;

        ApplyTemporaryTopSortingOrder(attackPieceRenderer, ref originalSortingOrder, ref changedSortingOrder);

        // 공격 시작 위치 저장
        Vector3 startPosition = piece.transform.position;

        // 1단계: 현재 위치에서 상대 기물 위쪽까지 포물선으로 이동
        Vector3 hoverTargetPosition = targetWorldPosition + Vector3.up * attackRiseHeight;
        yield return MoveTransformArcRoutine(piece.transform, startPosition, hoverTargetPosition, attackMoveDuration, attackMoveArcHeight);

        // 2단계: 상대 기물 위에서 잠깐 멈춤
        if (attackHoverWaitDuration > 0f)
        {
            yield return new WaitForSeconds(attackHoverWaitDuration);
        }

        // 기물이 중간에 제거되었으면 Sorting Order를 복구하고 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 3단계: 내려찍기 직전에 살짝 더 위로 상승
        Vector3 extraRisePosition = hoverTargetPosition + Vector3.up * attackExtraRiseHeight;
        yield return MoveTransformRoutine(piece.transform, hoverTargetPosition, extraRisePosition, attackExtraRiseDuration);

        // 기물이 중간에 제거되었으면 Sorting Order를 복구하고 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 4단계: 상대 기물 위치로 빠르게 내려찍기
        yield return MoveTransformRoutine(piece.transform, extraRisePosition, targetWorldPosition, attackSlamDuration);

        // 5단계: 내려찍기 충격 피드백
        PlayAttackImpactFeedback();

        // 공격 연출이 끝나면 임시 Sorting Order 복구
        RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
    }


    // <변경부분> Defense 성공 시 공격자가 내려찍기 도중 막히고,
    // 원래 위치에 도달하지 못한 앞쪽 지점에 떨어진 뒤 두 번 튀며 원위치로 복귀하는 연출
    public IEnumerator PlayPieceBlockedAttackMoveAnimation(Piece piece, Vector3 targetWorldPosition)
    {
        // 이동할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // 방어 연출 중 공격자가 타겟 뒤에 가려지지 않도록 Sorting Order 임시 상승
        SpriteRenderer attackPieceRenderer = piece.GetComponent<SpriteRenderer>();
        int originalSortingOrder = 0;
        bool changedSortingOrder = false;

        ApplyTemporaryTopSortingOrder(attackPieceRenderer, ref originalSortingOrder, ref changedSortingOrder);

        // 공격 시작 위치 저장
        Vector3 startPosition = piece.transform.position;

        // 기존 공격과 동일하게 타겟 위쪽까지 포물선 이동
        Vector3 hoverTargetPosition = targetWorldPosition + Vector3.up * attackRiseHeight;
        yield return MoveTransformArcRoutine(piece.transform, startPosition, hoverTargetPosition, attackMoveDuration, attackMoveArcHeight);

        // 타겟 위에서 잠깐 멈춤
        if (attackHoverWaitDuration > 0f)
        {
            yield return new WaitForSeconds(attackHoverWaitDuration);
        }

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 내려찍기 직전 살짝 더 상승
        Vector3 extraRisePosition = hoverTargetPosition + Vector3.up * attackExtraRiseHeight;
        yield return MoveTransformRoutine(piece.transform, hoverTargetPosition, extraRisePosition, attackExtraRiseDuration);

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 내려찍는 도중 방어에 막히는 지점
        // 완전히 타겟 위치까지 내려가지 않고 중간 지점까지만 내려감
        Vector3 blockedImpactPosition = Vector3.Lerp(extraRisePosition, targetWorldPosition, 0.65f);
        yield return MoveTransformRoutine(piece.transform, extraRisePosition, blockedImpactPosition, attackSlamDuration * 0.65f);

        // 방어 충격 피드백
        PlayAttackImpactFeedback();

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 공격 시작 위치에서 타겟으로 향하는 방향 계산
        Vector3 attackDirection = targetWorldPosition - startPosition;

        // 방향값이 너무 작으면 기본 방향 사용
        if (attackDirection.sqrMagnitude <= 0.0001f)
        {
            attackDirection = Vector3.right;
        }

        attackDirection.Normalize();

        // 원래 위치에 도달하지 못한 앞쪽 착지 지점
        Vector3 fallShortPosition = startPosition + attackDirection * defenseFallShortDistance;

        // 첫 번째 튐 후 도착할 위치
        Vector3 firstBounceLandingPosition = startPosition + attackDirection * (defenseFallShortDistance * 0.45f);

        // 두 번째 튐 후 도착할 위치
        Vector3 secondBounceLandingPosition = startPosition + attackDirection * (defenseFallShortDistance * 0.15f);

        // 방어에 막힌 공격자가 뒤로 튕겨나오지만, 원래 위치를 지나치지 않고 앞쪽에 떨어짐
        yield return MoveTransformArcRoutine(
            piece.transform,
            blockedImpactPosition,
            fallShortPosition,
            defenseFallBackDuration,
            defenseSecondBounceHeight
        );

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 첫 번째 반동: 크게 튀면서 원래 위치 쪽으로 이동
        yield return MoveTransformArcRoutine(
            piece.transform,
            fallShortPosition,
            firstBounceLandingPosition,
            defenseFirstBounceDuration,
            defenseFirstBounceHeight
        );

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 두 번째 반동: 작게 튀면서 원래 위치에 더 가까워짐
        yield return MoveTransformArcRoutine(
            piece.transform,
            firstBounceLandingPosition,
            secondBounceLandingPosition,
            defenseSecondBounceDuration,
            defenseSecondBounceHeight
        );

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 마지막으로 원래 위치에 정확히 복귀
        yield return MoveTransformRoutine(
            piece.transform,
            secondBounceLandingPosition,
            startPosition,
            defenseFinalReturnDuration
        );

        // 연출 종료 후 정확히 원래 위치로 보정
        if (piece != null)
        {
            piece.transform.position = startPosition;
        }

        // 방어 연출이 끝나면 임시 Sorting Order 복구
        RestoreSortingOrder(attackPieceRenderer, originalSortingOrder, changedSortingOrder);
    }


    // <변경부분> 생성된 기물을 시전자 위치에서 생성 위치까지 날아가게 하는 함수
    public void PlaySkillSpawnAnimationFromSource(Piece spawnedPiece, Piece sourcePiece)
    {
        // 생성된 기물이나 시전자가 없으면 연출 불가
        if (spawnedPiece == null || sourcePiece == null)
        {
            return;
        }

        PlaySkillSpawnAnimationFromWorldPosition(spawnedPiece, sourcePiece.transform.position);
    }


    // <변경부분> 생성된 기물을 특정 월드 위치에서 생성 위치까지 날아가게 하는 함수
    public void PlaySkillSpawnAnimationFromWorldPosition(Piece spawnedPiece, Vector3 sourceWorldPosition)
    {
        // 생성된 기물이 없으면 연출 불가
        if (spawnedPiece == null)
        {
            return;
        }

        // SpawnPiece로 이미 생성 위치에 배치된 최종 위치 저장
        Vector3 targetWorldPosition = spawnedPiece.transform.position;

        // 화면상 시작 위치를 시전자/사망 기물 위치로 이동
        spawnedPiece.transform.position = sourceWorldPosition;

        // 시작 위치에서 최종 생성 위치까지 포물선 연출
        StartCoroutine(PlaySkillSpawnAnimationRoutine(spawnedPiece, sourceWorldPosition, targetWorldPosition));
    }


    // <변경부분> 젤루 합성 재료 2개가 스킬을 사용한 Pawn 위치로 동시에 포물선 이동하는 코루틴
    public IEnumerator PlaySynthesisMaterialMoveAnimation(Piece firstMaterial, Piece secondMaterial, Piece targetPawn)
    {
        // 목표 Pawn이 없으면 연출할 수 없으므로 종료
        if (targetPawn == null)
        {
            yield break;
        }

        // 첫 번째 재료 Transform 저장
        Transform firstMaterialTransform = firstMaterial != null ? firstMaterial.transform : null;

        // 두 번째 재료 Transform 저장
        Transform secondMaterialTransform = secondMaterial != null ? secondMaterial.transform : null;

        // 이동할 재료가 둘 다 없으면 종료
        if (firstMaterialTransform == null && secondMaterialTransform == null)
        {
            yield break;
        }

        // 첫 번째 재료 시작 위치 저장
        Vector3 firstStartPosition = firstMaterialTransform != null ? firstMaterialTransform.position : Vector3.zero;

        // 두 번째 재료 시작 위치 저장
        Vector3 secondStartPosition = secondMaterialTransform != null ? secondMaterialTransform.position : Vector3.zero;

        // 목표 위치는 스킬을 사용한 Pawn의 현재 위치
        Vector3 targetWorldPosition = targetPawn.transform.position;

        // 연출 시간이 0 이하이면 즉시 Pawn 위치로 이동
        if (synthesisMaterialAnimationDuration <= 0f)
        {
            if (firstMaterialTransform != null)
            {
                firstMaterialTransform.position = targetWorldPosition;
            }

            if (secondMaterialTransform != null)
            {
                secondMaterialTransform.position = targetWorldPosition;
            }

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < synthesisMaterialAnimationDuration)
        {
            // 목표 Pawn이 중간에 사라졌으면 연출 중단
            if (targetPawn == null)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;

            // 0~1 진행률 계산
            float normalizedTime = Mathf.Clamp01(elapsedTime / synthesisMaterialAnimationDuration);

            // 이동 진행을 부드럽게 처리
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

            // 중간 지점에서 가장 높아지는 포물선 높이
            float arcOffset = Mathf.Sin(normalizedTime * Mathf.PI) * synthesisMaterialArcHeight;

            // Pawn이 연출 중 이동할 가능성까지 고려해 매 프레임 목표 위치 갱신
            targetWorldPosition = targetPawn.transform.position;

            // 첫 번째 재료 이동 처리
            if (firstMaterialTransform != null)
            {
                Vector3 firstCurrentPosition = Vector3.Lerp(firstStartPosition, targetWorldPosition, easedTime);
                firstCurrentPosition.y += arcOffset;
                firstMaterialTransform.position = firstCurrentPosition;
            }

            // 두 번째 재료 이동 처리
            if (secondMaterialTransform != null)
            {
                Vector3 secondCurrentPosition = Vector3.Lerp(secondStartPosition, targetWorldPosition, easedTime);
                secondCurrentPosition.y += arcOffset;
                secondMaterialTransform.position = secondCurrentPosition;
            }

            yield return null;
        }

        // 연출 종료 후 재료 위치를 Pawn 위치로 정확히 보정
        if (targetPawn != null)
        {
            targetWorldPosition = targetPawn.transform.position;

            if (firstMaterialTransform != null)
            {
                firstMaterialTransform.position = targetWorldPosition;
            }

            if (secondMaterialTransform != null)
            {
                secondMaterialTransform.position = targetWorldPosition;
            }
        }
    }


    // <변경부분> 특정 Transform을 시작 위치에서 목표 위치까지 선형 이동시키는 공통 코루틴
    private IEnumerator MoveTransformRoutine(Transform targetTransform, Vector3 startPosition, Vector3 endPosition, float duration)
    {
        if (targetTransform == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            targetTransform.position = endPosition;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (targetTransform == null)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

            // SmoothStep으로 시작/끝 이동을 부드럽게 처리
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

            targetTransform.position = Vector3.Lerp(startPosition, endPosition, easedTime);

            yield return null;
        }

        if (targetTransform != null)
        {
            targetTransform.position = endPosition;
        }
    }


    // <변경부분> 특정 Transform을 시작 위치에서 목표 위치까지 포물선으로 이동시키는 공통 코루틴
    private IEnumerator MoveTransformArcRoutine(Transform targetTransform, Vector3 startPosition, Vector3 endPosition, float duration, float arcHeight)
    {
        if (targetTransform == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            targetTransform.position = endPosition;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (targetTransform == null)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;

            // 0~1 이동 진행률
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

            // 이동 진행 자체는 부드럽게 처리
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

            // 시작 위치에서 목표 위치까지 기본 이동
            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, easedTime);

            // 중간 지점에서 가장 높아지는 포물선 보정값
            float arcOffset = Mathf.Sin(normalizedTime * Mathf.PI) * arcHeight;

            currentPosition.y += arcOffset;

            targetTransform.position = currentPosition;

            yield return null;
        }

        if (targetTransform != null)
        {
            targetTransform.position = endPosition;
        }
    }


    // <변경부분> 스킬 생성 기물이 시작 위치에서 목표 위치까지 포물선으로 이동하는 코루틴
    private IEnumerator PlaySkillSpawnAnimationRoutine(Piece spawnedPiece, Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        // 생성된 기물이 없으면 종료
        if (spawnedPiece == null)
        {
            yield break;
        }

        // 연출 시간이 0 이하이면 즉시 최종 위치로 보정
        if (skillSpawnAnimationDuration <= 0f)
        {
            spawnedPiece.transform.position = targetWorldPosition;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < skillSpawnAnimationDuration)
        {
            // 연출 중 기물이 제거되면 종료
            if (spawnedPiece == null)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;

            // 0~1 진행률 계산
            float normalizedTime = Mathf.Clamp01(elapsedTime / skillSpawnAnimationDuration);

            // 부드러운 이동 진행률
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

            // 시작 위치에서 목표 위치까지 기본 이동
            Vector3 currentPosition = Vector3.Lerp(startWorldPosition, targetWorldPosition, easedTime);

            // 중간 지점에서 가장 높아지는 포물선 높이
            float arcOffset = Mathf.Sin(normalizedTime * Mathf.PI) * skillSpawnArcHeight;

            currentPosition.y += arcOffset;

            // 실제 위치 적용
            spawnedPiece.transform.position = currentPosition;

            yield return null;
        }

        // 연출 종료 후 정확한 최종 생성 위치로 보정
        if (spawnedPiece != null)
        {
            spawnedPiece.transform.position = targetWorldPosition;
        }
    }


    // <변경부분> 공격/방어 충격 피드백 실행
    private void PlayAttackImpactFeedback()
    {
        // 화면 흔들림 실행
        StartCoroutine(ShakeCameraRoutine());

        // 모바일 빌드에서 진동 실행
        if (enableMobileVibration)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }


    // <변경부분> 카메라 또는 지정된 Transform을 짧게 흔드는 코루틴
    private IEnumerator ShakeCameraRoutine()
    {
        Transform shakeTarget = cameraShakeTarget;

        // 별도 흔들림 대상이 없으면 Main Camera 사용
        if (shakeTarget == null && Camera.main != null)
        {
            shakeTarget = Camera.main.transform;
        }

        if (shakeTarget == null)
        {
            yield break;
        }

        Vector3 originalPosition = shakeTarget.localPosition;

        if (cameraShakeDuration <= 0f || cameraShakeStrength <= 0f)
        {
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < cameraShakeDuration)
        {
            elapsedTime += Time.deltaTime;

            float randomX = Random.Range(-cameraShakeStrength, cameraShakeStrength);
            float randomY = Random.Range(-cameraShakeStrength, cameraShakeStrength);

            shakeTarget.localPosition = originalPosition + new Vector3(randomX, randomY, 0f);

            yield return null;
        }

        shakeTarget.localPosition = originalPosition;
    }


    // <변경부분> 공격자 Sorting Order를 임시로 최상단으로 올림
    private void ApplyTemporaryTopSortingOrder(SpriteRenderer spriteRenderer, ref int originalSortingOrder, ref bool changedSortingOrder)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        originalSortingOrder = spriteRenderer.sortingOrder;
        spriteRenderer.sortingOrder = attackAnimationSortingOrder;
        changedSortingOrder = true;
    }


    // <변경부분> 임시로 올렸던 Sorting Order를 원래 값으로 복구
    private void RestoreSortingOrder(SpriteRenderer spriteRenderer, int originalSortingOrder, bool changedSortingOrder)
    {
        if (spriteRenderer == null || changedSortingOrder == false)
        {
            return;
        }

        spriteRenderer.sortingOrder = originalSortingOrder;
    }
}