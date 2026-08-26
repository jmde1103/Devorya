using Spine.Unity;
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

    [Header("Spine Attack Timing")]
    // <변경부분> Down 애니메이션을 먼저 재생한 뒤 실제 내려찍기 Transform 이동을 시작하기 전 대기 시간
    // 값이 클수록 Down 모션을 더 보여준 뒤 내려찍는다.
    [SerializeField] private float attackDownPreSlamDelay = 0.03f;


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


    [Header("Absorb Impact Pixel Burst")]
    // <변경부분> 흡수 공격 내려찍기 충격 순간 생성할
    // BlackPixelBurstEffect 프리팹
    [SerializeField]
    private PixelBurstEffect blackPixelBurstEffectPrefab;

    // <변경부분> 버튼에서 사용하는 원본 프리팹 크기보다
    // 작게 표시하기 위한 스케일 배율
    [Range(0.1f, 2f)]
    [SerializeField]
    private float absorbBlackPixelBurstScale = 0.8f;


    [Header("Attack Impact Feedback")]
    // <변경부분> 화면 흔들림 대상. 비어 있으면 Main Camera 사용
    [SerializeField] private Transform cameraShakeTarget;

    // <변경부분> 공격/방어 충격 시 화면 흔들림 시간
    [SerializeField] private float cameraShakeDuration = 0.12f;

    // <변경부분> 공격/방어 충격 시 화면 흔들림 세기
    [SerializeField] private float cameraShakeStrength = 0.06f;

    // 모바일 빌드에서 공격/방어 충격 시 진동 사용 여부
    [SerializeField] private bool enableMobileVibration = true;


    // 현재 실행 중인 카메라 흔들림 코루틴.
    // 새로운 충격이 발생하면 기존 흔들림을 먼저 정리한 뒤 새 흔들림을 시작한다.
    private Coroutine cameraShakeCoroutine;

    // 현재 흔들림이 적용되고 있는 실제 Transform.
    private Transform activeCameraShakeTarget;

    // 현재 흔들림을 시작하기 전의 정확한 기준 Local Position.
    // 중첩 흔들림이나 비활성화 시 반드시 이 위치로 복구한다.
    private Vector3 cameraShakeBaseLocalPosition;




    private void OnDisable()
    {
        // 컴포넌트가 비활성화되면서 Unity가 코루틴을 중단하더라도
        // 카메라가 마지막 흔들림 좌표에 남지 않도록 즉시 원위치로 복구한다.
        StopCameraShakeImmediately();
    }


    // 기물이 시작 위치에서 목표 위치까지 살짝 떠서 이동하는 기본 이동 연출
    public IEnumerator PlayPieceJumpMoveAnimation(
            Piece piece,
        Vector3 targetPosition)
    {
        // 이동할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // <변경부분> Sprite 및 Spine 기물을 동일하게 정렬하기 위해
        // PieceVisualController와 기존 SpriteRenderer를 함께 가져온다.
        PieceVisualController moveVisualController =
            piece.GetComponent<PieceVisualController>();

        SpriteRenderer movePieceRenderer =
            piece.GetComponent<SpriteRenderer>();

        int originalSortingOrder = 0;
        bool changedSortingOrder = false;

        // <변경부분> 이동 중 다른 기물 뒤에 가려지지 않도록
        // 이동 기물의 Sorting Order를 임시로 최상단으로 올린다.
        ApplyTemporaryTopSortingOrder(
            moveVisualController,
            movePieceRenderer,
            ref originalSortingOrder,
            ref changedSortingOrder
        );

        // 시작 위치 저장
        Vector3 startPosition =
            piece.transform.position;

        // Spine 애니메이션 컨트롤러 가져오기
        PieceSpineAnimationController spineAnimator =
            GetSpineAnimator(piece);

        // 이동 방향 계산
        bool isRightDirection =
            IsRightDirection(
                startPosition,
                targetPosition
            );

        // Spine 이동 애니메이션 재생
        if (spineAnimator != null)
        {
            spineAnimator.PlayMoveByDirection(
                isRightDirection
            );
        }

        // 연출 시간이 0 이하라면 즉시 이동
        if (moveAnimationDuration <= 0f)
        {
            piece.transform.position =
                targetPosition;

            if (spineAnimator != null)
            {
                yield return spineAnimator
                    .PlayDownToIdleRoutine();
            }

            // <변경부분> 즉시 이동이 끝난 뒤 원래 정렬 순서 복구
            RestoreSortingOrder(
                moveVisualController,
                movePieceRenderer,
                originalSortingOrder,
                changedSortingOrder
            );

            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < moveAnimationDuration)
        {
            // 이동 연출 중 기물이 제거되었으면 종료
            if (piece == null)
            {
                RestoreSortingOrder(
                    moveVisualController,
                    movePieceRenderer,
                    originalSortingOrder,
                    changedSortingOrder
                );

                yield break;
            }

            elapsedTime +=
                Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    moveAnimationDuration
                );

            Vector3 currentPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    normalizedTime
                );

            float jumpOffset =
                Mathf.Sin(
                    normalizedTime *
                    Mathf.PI
                ) * moveJumpHeight;

            currentPosition.y +=
                jumpOffset;

            piece.transform.position =
                currentPosition;

            yield return null;
        }

        if (piece != null)
        {
            piece.transform.position =
                targetPosition;
        }

        // <변경부분> 이동이 끝난 뒤 착지 애니메이션 재생
        if (spineAnimator != null)
        {
            yield return spineAnimator
                .PlayDownToIdleRoutine();
        }

        // <변경부분> 일반 이동 연출이 끝났으므로
        // 이동 시작 전의 Sorting Order로 복구한다.
        RestoreSortingOrder(
            moveVisualController,
            movePieceRenderer,
            originalSortingOrder,
            changedSortingOrder
        );
    }


    // <변경부분> 일반 공격과 흡수 공격의 이동 및 Spine 애니메이션을 처리하는 코루틴
    //
    // 일반 공격:
    // Left 또는 Right
    // → 이동 방향에 맞는 Stop 1회
    // → Down
    // → 내려찍기
    //
    // 흡수 공격:
    // Left 또는 Right
    // → 이동 방향에 맞는 Stop 1회
    // → Absorb
    // → Down_Absorb와 실제 내려찍기 동시 진행
    // → 충격 순간 onImpact 호출
    // → Down_Absorb가 완전히 끝날 때까지 대기
    public IEnumerator PlayPieceAttackMoveAnimation(
        Piece piece,
        Vector3 targetWorldPosition,
        bool isAbsorbAction,
        System.Action onImpact = null)
    {
        // 공격할 기물이 없으면 종료
        if (piece == null)
        {
            yield break;
        }

        // Spine / Sprite 외형 정렬에 사용할 컴포넌트
        PieceVisualController attackVisualController =
            piece.GetComponent<PieceVisualController>();

        SpriteRenderer attackPieceRenderer =
            piece.GetComponent<SpriteRenderer>();

        int originalSortingOrder = 0;
        bool changedSortingOrder = false;

        // 공격 중 공격자가 타겟 뒤에 가려지지 않도록 정렬 순서 상승
        ApplyTemporaryTopSortingOrder(
            attackVisualController,
            attackPieceRenderer,
            ref originalSortingOrder,
            ref changedSortingOrder
        );

        // 공격 시작 위치 저장
        Vector3 startPosition = piece.transform.position;

        // 현재 기물의 Spine 애니메이션 컨트롤러
        PieceSpineAnimationController spineAnimator =
            GetSpineAnimator(piece);

        // 타겟이 오른쪽에 있는지 확인
        bool isRightDirection =
            IsRightDirection(
                startPosition,
                targetWorldPosition
            );

        // 왼쪽 이동이면 Left, 오른쪽 이동이면 Right 재생
        if (spineAnimator != null)
        {
            spineAnimator.PlayMoveByDirection(
                isRightDirection
            );
        }

        // 1단계:
        // 현재 위치에서 타겟 위쪽까지 포물선 이동
        Vector3 hoverTargetPosition =
            targetWorldPosition +
            Vector3.up * attackRiseHeight;

        yield return MoveTransformArcRoutine(
            piece.transform,
            startPosition,
            hoverTargetPosition,
            attackMoveDuration,
            attackMoveArcHeight
        );

        // 이동 중 기물이 제거되었다면 정렬 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(
                attackVisualController,
                attackPieceRenderer,
                originalSortingOrder,
                changedSortingOrder
            );

            yield break;
        }

        // <변경부분> 이동 방향에 맞는 Stop을 정확히 한 번만 실행
        //
        // 왼쪽 이동:
        // Stop_Left
        //
        // 오른쪽 이동:
        // Stop_Right
        if (spineAnimator != null)
        {
            yield return spineAnimator.PlayStopOnlyRoutine(
                isRightDirection
            );

            // 흡수 공격일 때만 Stop 이후 Absorb 실행
            if (isAbsorbAction)
            {
                yield return spineAnimator.PlayAbsorbRoutine();
            }
        }

        // 2단계:
        // 타겟 위에서 잠깐 정지
        if (attackHoverWaitDuration > 0f)
        {
            yield return new WaitForSeconds(
                attackHoverWaitDuration
            );
        }

        // 대기 중 기물이 제거되었다면 정렬 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(
                attackVisualController,
                attackPieceRenderer,
                originalSortingOrder,
                changedSortingOrder
            );

            yield break;
        }

        // 3단계:
        // 내려찍기 직전에 조금 더 상승
        Vector3 extraRisePosition =
            hoverTargetPosition +
            Vector3.up * attackExtraRiseHeight;

        yield return MoveTransformRoutine(
            piece.transform,
            hoverTargetPosition,
            extraRisePosition,
            attackExtraRiseDuration
        );

        // 상승 중 기물이 제거되었다면 정렬 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(
                attackVisualController,
                attackPieceRenderer,
                originalSortingOrder,
                changedSortingOrder
            );

            yield break;
        }

        // <변경부분> 흡수 공격의 Down_Absorb 코루틴 참조
        // 내려찍기 Transform 이동과 동시에 실행한 뒤,
        // 충격 처리 후 남은 애니메이션이 끝날 때까지 기다린다.
        Coroutine absorbDownAnimationCoroutine = null;

        // <변경부분> 실제 내려찍기 직전에 Spine 내려찍기 애니메이션을 시작한다.
        // 일반 공격은 Down, 흡수 공격은 Down_Absorb를 사용한다.
        if (spineAnimator != null)
        {
            if (isAbsorbAction)
            {
                // <변경부분> Down_Absorb 코루틴 참조를 저장한다.
                // 충격 이후 이 코루틴이 완전히 끝날 때까지 기다려
                // 흡수 외형 변경과 Born이 Down_Absorb를 덮어쓰지 않게 한다.
                absorbDownAnimationCoroutine = StartCoroutine(
                    spineAnimator.PlayDownAbsorbToIdleRoutine()
                );
            }
            else
            {
                // 일반 공격은 기존처럼 내려찍기와 충격 연출이 끝날 때까지
                // 임시 최상단 Sorting Order를 유지한다.
                StartCoroutine(
                    spineAnimator.PlayDownToIdleRoutine()
                );
            }

            // Spine 모션이 먼저 보인 뒤 실제 Transform 내려찍기를 시작한다.
            if (attackDownPreSlamDelay > 0f)
            {
                yield return new WaitForSeconds(
                    attackDownPreSlamDelay
                );
            }
        }

        // 4단계: 상대 기물 위치로 빠르게 내려찍기
        yield return MoveTransformRoutine(
            piece.transform,
            extraRisePosition,
            targetWorldPosition,
            attackSlamDuration
        );

        // 5단계: 내려찍기 충격 피드백
        // 이 함수가 호출되는 순간 화면 흔들림과 모바일 진동이 시작된다.
        PlayAttackImpactFeedback();

        // <변경부분> 화면 흔들림이 시작되는 동일한 충격 순간에
        // BattleManager가 전달한 처리를 실행한다.
        // 흡수 공격에서는 이 콜백으로 상대 기물을 화면에서 즉시 숨긴다.
        onImpact?.Invoke();

        // <변경부분> 흡수 공격일 때만 화면 흔들림이 시작되는
        // 동일한 충격 순간에 상대 기물 위치에서
        // BlackPixelBurstEffect를 생성한다.
        if (isAbsorbAction)
        {
            PlayAbsorbBlackPixelBurst(
                targetWorldPosition
            );
        }

        // <변경부분> 흡수 공격은 Down_Absorb가 완전히 끝난 뒤에만
        // BattleManager로 복귀해 흡수 데이터 적용, 타겟 제거,
        // 외형 변경, Born을 실행한다.
        if (isAbsorbAction &&
            absorbDownAnimationCoroutine != null)
        {
            yield return absorbDownAnimationCoroutine;
        }

        // 공격 연출 종료 후 원래 정렬 순서로 복구
        RestoreSortingOrder(
            attackVisualController,
            attackPieceRenderer,
            originalSortingOrder,
            changedSortingOrder
        );
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

        // <변경부분> Spine / Sprite 공통 정렬 처리를 위해 VisualController와 SpriteRenderer를 함께 가져옴
        PieceVisualController attackVisualController = piece.GetComponent<PieceVisualController>();
        SpriteRenderer attackPieceRenderer = piece.GetComponent<SpriteRenderer>();

        int originalSortingOrder = 0;
        bool changedSortingOrder = false;

        // <변경부분> 방어 연출 중 공격자가 타겟 뒤에 가려지지 않도록 Sorting Order 임시 상승
        ApplyTemporaryTopSortingOrder(
            attackVisualController,
            attackPieceRenderer,
            ref originalSortingOrder,
            ref changedSortingOrder
        );

        // 공격 시작 위치 저장
        Vector3 startPosition = piece.transform.position;

        // <변경부분> Spine 애니메이션 컨트롤러 가져오기
        PieceSpineAnimationController spineAnimator = GetSpineAnimator(piece);

        // <변경부분> 공격 방향 계산
        bool isRightDirection = IsRightDirection(startPosition, targetWorldPosition);

        // <변경부분> 공격 접근 이동 애니메이션 재생
        if (spineAnimator != null)
        {
            spineAnimator.PlayMoveByDirection(isRightDirection);
        }

        // 기존 공격과 동일하게 타겟 위쪽까지 포물선 이동
        Vector3 hoverTargetPosition = targetWorldPosition + Vector3.up * attackRiseHeight;
        yield return MoveTransformArcRoutine(piece.transform, startPosition, hoverTargetPosition, attackMoveDuration, attackMoveArcHeight);

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackVisualController, attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // <변경부분> 방어 공격 중에도 Stop 후 Idle로 복귀하지 않고 Down으로 이어지도록 Stop만 재생
        if (spineAnimator != null)
        {
            yield return spineAnimator.PlayStopOnlyRoutine(isRightDirection);
        }

        // 타겟 위에서 잠깐 멈춤
        if (attackHoverWaitDuration > 0f)
        {
            yield return new WaitForSeconds(attackHoverWaitDuration);
        }

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackVisualController, attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // 내려찍기 직전 살짝 더 상승
        Vector3 extraRisePosition = hoverTargetPosition + Vector3.up * attackExtraRiseHeight;
        yield return MoveTransformRoutine(piece.transform, hoverTargetPosition, extraRisePosition, attackExtraRiseDuration);

        // 기물이 중간에 제거되었으면 Sorting Order 복구 후 종료
        if (piece == null)
        {
            RestoreSortingOrder(attackVisualController, attackPieceRenderer, originalSortingOrder, changedSortingOrder);
            yield break;
        }

        // <변경부분> 방어에 막히는 공격도 Down 후 무조건 Idle로 복귀
        if (spineAnimator != null)
        {
            StartCoroutine(spineAnimator.PlayDownToIdleRoutine());

            // <변경부분> Spine Down 모션과 실제 내려찍기 타이밍을 맞추기 위한 조절값
            if (attackDownPreSlamDelay > 0f)
            {
                yield return new WaitForSeconds(attackDownPreSlamDelay);
            }
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
            RestoreSortingOrder(attackVisualController, attackPieceRenderer, originalSortingOrder, changedSortingOrder);
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
            RestoreSortingOrder(attackVisualController, attackPieceRenderer, originalSortingOrder, changedSortingOrder);
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
            RestoreSortingOrder(attackVisualController, attackPieceRenderer, originalSortingOrder, changedSortingOrder);
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
            RestoreSortingOrder(attackVisualController, attackPieceRenderer, originalSortingOrder, changedSortingOrder);
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

        // <변경부분> 방어 연출 후 기본 대기 상태로 복귀
        if (spineAnimator != null)
        {
            spineAnimator.PlayReturnIdle();
        }

        // 방어 연출이 끝나면 임시 Sorting Order 복구
        RestoreSortingOrder(attackVisualController, attackPieceRenderer, originalSortingOrder, changedSortingOrder);
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

        // <변경부분> 생성된 기물의 Spine 애니메이션 컨트롤러 가져오기
        PieceSpineAnimationController spineAnimator = GetSpineAnimator(spawnedPiece);

        // <변경부분> 생성 위치까지 날아가는 방향 계산
        bool isRightDirection = IsRightDirection(startWorldPosition, targetWorldPosition);

        // <변경부분> 생성 기물이 이동 중일 때 방향 이동 애니메이션 재생
        if (spineAnimator != null)
        {
            spineAnimator.PlayMoveByDirection(isRightDirection);
        }

        // 연출 시간이 0 이하이면 즉시 최종 위치로 보정
        if (skillSpawnAnimationDuration <= 0f)
        {
            spawnedPiece.transform.position = targetWorldPosition;

            // <변경부분> 최종 위치에 도착한 뒤 Born 애니메이션 재생
            if (spineAnimator != null)
            {
                yield return spineAnimator.PlayBornRoutine();
            }

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

        // <변경부분> 생성 위치에 도착한 뒤 Born 애니메이션 재생
        if (spineAnimator != null)
        {
            yield return spineAnimator.PlayBornRoutine();
        }
    }


    // <변경부분> 기물 생성 또는 변형 시 Born 애니메이션을 재생하는 함수
    // <변경부분> 기물 생성 또는 변형 시
    // Born 애니메이션을 재생하는 함수
    public IEnumerator PlayPieceBornAnimation(Piece piece)
    {
        PieceSpineAnimationController spineAnimator =
            GetSpineAnimator(piece);

        if (spineAnimator == null)
        {
            yield break;
        }

        yield return spineAnimator.PlayBornRoutine();
    }


    // <변경부분> 흡수 공격 내려찍기 충격 순간
    // 상대 기물 위치에서 검은 픽셀 파티클을 생성해 재생한다.
    private void PlayAbsorbBlackPixelBurst(
        Vector3 targetWorldPosition)
    {
        // 프리팹이 연결되지 않았다면 실행하지 않음
        if (blackPixelBurstEffectPrefab == null)
        {
            Debug.LogWarning(
                "흡수 공격 픽셀 이펙트 재생 실패: " +
                "BlackPixelBurstEffect 프리팹이 연결되지 않았습니다."
            );

            return;
        }

        // <변경부분> 버튼에서 사용하는 프리팹 원본과
        // 별도의 파티클 인스턴스를 생성한다.
        PixelBurstEffect effectInstance =
            Instantiate(
                blackPixelBurstEffectPrefab
            );

        // <변경부분> 프리팹의 기존 스케일을 기준으로
        // Inspector의 설정 배율을 적용한다.
        // 기본값 0.8이면 원본 프리팹 크기의 80%로 표시된다.
        float appliedScale =
            Mathf.Max(
                0.01f,
                absorbBlackPixelBurstScale
            );

        effectInstance.transform.localScale *=
            appliedScale;

        // <변경부분> 상대 기물이 있던 월드 위치에서 재생하고,
        // 파티클 수명이 끝나면 생성된 인스턴스를 자동 제거한다.
        effectInstance.PlayAtPositionAndDestroy(
            targetWorldPosition
        );
    }


    // <변경부분> 공격/방어 충격 피드백 실행
    private void PlayAttackImpactFeedback()
    {
        // 기존 흔들림이 실행 중이라면 먼저 원위치로 복구한 뒤
        // 이번 충격 기준으로 새로운 흔들림을 시작한다.
        StartCameraShake();

        // 모바일 빌드에서 진동 실행
        if (enableMobileVibration)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }


    // 공격 충격용 카메라 흔들림을 시작한다.
    //
    // 이미 흔들림이 실행 중이라면 기존 코루틴을 중단하고
    // 기존 흔들림 시작 전의 정확한 위치로 먼저 복구한 뒤
    // 새로운 충격 기준으로 흔들림 시간을 다시 시작한다.
    private void StartCameraShake()
    {
        // 이전 흔들림이 실행 중이었다면
        // 반드시 기준 위치를 복구한 뒤 종료한다.
        StopCameraShakeImmediately();

        Transform shakeTarget =
            cameraShakeTarget;

        // Inspector에 별도 대상이 없으면 Main Camera를 사용한다.
        if (shakeTarget == null &&
            Camera.main != null)
        {
            shakeTarget =
                Camera.main.transform;
        }

        if (shakeTarget == null)
        {
            return;
        }

        // 흔들림 설정이 비활성 상태라면
        // 코루틴을 새로 시작하지 않는다.
        if (cameraShakeDuration <= 0f ||
            cameraShakeStrength <= 0f)
        {
            return;
        }

        activeCameraShakeTarget =
            shakeTarget;

        // 이번 흔들림이 시작되는 정확한 위치를
        // 새로운 기준 위치로 저장한다.
        cameraShakeBaseLocalPosition =
            shakeTarget.localPosition;

        cameraShakeCoroutine =
            StartCoroutine(
                ShakeCameraRoutine(
                    shakeTarget,
                    cameraShakeBaseLocalPosition
                )
            );
    }


    // 지정된 기준 위치를 중심으로 카메라를 짧게 흔든다.
    //
    // 기준 위치는 코루틴 시작 시 한 번만 전달받기 때문에
    // 흔들리는 도중의 임시 위치가 새로운 원점으로 사용되지 않는다.
    private IEnumerator ShakeCameraRoutine(
        Transform shakeTarget,
        Vector3 baseLocalPosition)
    {
        float elapsedTime =
            0f;

        while (elapsedTime <
               cameraShakeDuration)
        {
            // 흔들림 대상이 연출 도중 제거됐다면 안전하게 종료한다.
            if (shakeTarget == null)
            {
                cameraShakeCoroutine =
                    null;

                activeCameraShakeTarget =
                    null;

                yield break;
            }

            elapsedTime +=
                Time.deltaTime;

            float randomX =
                Random.Range(
                    -cameraShakeStrength,
                    cameraShakeStrength
                );

            float randomY =
                Random.Range(
                    -cameraShakeStrength,
                    cameraShakeStrength
                );

            shakeTarget.localPosition =
                baseLocalPosition +
                new Vector3(
                    randomX,
                    randomY,
                    0f
                );

            yield return null;
        }

        // 흔들림이 정상적으로 끝나면
        // 시작 전 기준 위치로 정확히 복구한다.
        if (shakeTarget != null)
        {
            shakeTarget.localPosition =
                baseLocalPosition;
        }

        cameraShakeCoroutine =
            null;

        activeCameraShakeTarget =
            null;
    }


    // 실행 중인 카메라 흔들림을 즉시 중단하고
    // 흔들림 시작 전의 정확한 위치로 복구한다.
    private void StopCameraShakeImmediately()
    {
        if (cameraShakeCoroutine != null)
        {
            StopCoroutine(
                cameraShakeCoroutine
            );

            cameraShakeCoroutine =
                null;
        }

        if (activeCameraShakeTarget != null)
        {
            activeCameraShakeTarget.localPosition =
                cameraShakeBaseLocalPosition;
        }

        activeCameraShakeTarget =
            null;
    }

    // <변경부분> 기물 선택 시 Spine Select 애니메이션을 재생하는 함수
    public void PlayPieceSelectAnimation(Piece piece)
    {
        PieceSpineAnimationController spineAnimator = GetSpineAnimator(piece);

        if (spineAnimator == null)
        {
            return;
        }

        StartCoroutine(spineAnimator.PlaySelectRoutine());
    }

    // <변경부분> 다른 기물을 선택하거나 선택이 해제될 때 기존 선택 기물을 Down 후 Idle로 전환
    public void PlayPieceDeselectAnimation(Piece piece)
    {
        PieceSpineAnimationController spineAnimator = GetSpineAnimator(piece);

        if (spineAnimator == null)
        {
            return;
        }

        StartCoroutine(spineAnimator.PlayDownToIdleRoutine());
    }

    // <변경부분> 기물을 강제로 Idle 상태로 되돌리는 함수
    public void PlayPieceIdleAnimation(Piece piece)
    {
        PieceSpineAnimationController spineAnimator = GetSpineAnimator(piece);

        if (spineAnimator == null)
        {
            return;
        }

        spineAnimator.PlayIdle();
    }

    // <변경부분> Piece에 연결된 Spine 애니메이션 컨트롤러를 가져오는 함수
    private PieceSpineAnimationController GetSpineAnimator(Piece piece)
    {
        if (piece == null)
        {
            return null;
        }

        PieceVisualController visualController = piece.GetComponent<PieceVisualController>();

        if (visualController != null &&
            visualController.CurrentSpineAnimationController != null)
        {
            return visualController.CurrentSpineAnimationController;
        }

        return piece.GetComponentInChildren<PieceSpineAnimationController>();
    }

    // <변경부분> 시작 위치와 목표 위치를 비교해서 오른쪽 방향 이동인지 판단하는 함수
    private bool IsRightDirection(Vector3 startPosition, Vector3 targetPosition)
    {
        return targetPosition.x >= startPosition.x;
    }

    // <변경부분> 공격자 Sorting Order를 임시로 최상단으로 올림
    // SpriteRenderer 기물과 Spine 기물을 모두 처리한다.
    private void ApplyTemporaryTopSortingOrder(
        PieceVisualController visualController,
        SpriteRenderer spriteRenderer,
        ref int originalSortingOrder,
        ref bool changedSortingOrder
    )
    {
        // 기존 SpriteRenderer의 정렬값을 원본 기준값으로 저장
        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
        }

        // Spine/Sprite 통합 외형 컨트롤러가 있으면 해당 컨트롤러를 통해 정렬 적용
        if (visualController != null)
        {
            visualController.SetSortingOrder(attackAnimationSortingOrder);
            changedSortingOrder = true;
            return;
        }

        // 기존 SpriteRenderer만 있는 기물을 위한 fallback
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = attackAnimationSortingOrder;
            changedSortingOrder = true;
        }
    }


    // <변경부분> 임시로 올렸던 Sorting Order를 원래 값으로 복구
    // SpriteRenderer 기물과 Spine 기물을 모두 처리한다.
    private void RestoreSortingOrder(
        PieceVisualController visualController,
        SpriteRenderer spriteRenderer,
        int originalSortingOrder,
        bool changedSortingOrder
    )
    {
        if (changedSortingOrder == false)
        {
            return;
        }

        // Spine/Sprite 통합 외형 컨트롤러가 있으면 해당 컨트롤러를 통해 복구
        if (visualController != null)
        {
            visualController.SetSortingOrder(originalSortingOrder);
            return;
        }

        // 기존 SpriteRenderer만 있는 기물을 위한 fallback
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
        }
    }
}