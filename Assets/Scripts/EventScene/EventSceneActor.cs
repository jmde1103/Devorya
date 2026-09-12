using System.Collections;
using Spine.Unity;
using UnityEngine;

// <변경부분>
// Event Scene에만 존재하는 경량 Actor.
//
// Battle Piece의 이동 / 공격 / 스킬 / 상태 시스템을 가지지 않고,
// Event 연출에 필요한 식별 정보와 Visual만 관리한다.
public class EventSceneActor : MonoBehaviour
{
    private string actorId;

    private PieceData pieceData;

    private PieceTeam visualTeam =
        PieceTeam.Neutral;

    private bool useAbsorbedPlayerVisual =
        false;

    private Vector2Int gridPosition;

    private GameObject visualObject;

    // <변경부분>
    // 현재 Event Actor Visual 위치 보정값.
    private Vector2 visualOffset =
        Vector2.zero;

    // <변경부분>
    // 현재 Visual X축 반전 여부.
    private bool isFlippedX =
        false;

    // <변경부분>
    // Visual Prefab이 원래 가지고 있던 Local Position.
    //
    // Visual Offset은 이 위치를 덮어쓰는 값이 아니라
    // 이 기준 위치에서 추가로 이동시키는 상대 Offset으로 사용한다.
    private Vector3 visualBaseLocalPosition =
        Vector3.zero;

    // <변경부분>
    // Prefab 원래 Scale.
    //
    // Flip X를 여러 번 변경하더라도 Scale이 누적 반전되지 않도록
    // 최초 Scale을 별도로 보관한다.
    private Vector3 visualBaseScale =
        Vector3.one;


    public string ActorId =>
        actorId;

    public PieceData PieceData =>
        pieceData;

    public PieceTeam VisualTeam =>
        visualTeam;

    public bool UseAbsorbedPlayerVisual =>
        useAbsorbedPlayerVisual;

    public Vector2Int GridPosition =>
        gridPosition;

    public GameObject VisualObject =>
        visualObject;

    public Vector2 VisualOffset =>
    visualOffset;

    public bool IsFlippedX =>
        isFlippedX;


    // <변경부분>
    // SpawnActor Step이 생성한 Actor 정보를 초기화한다.
    public void Initialize(
     string newActorId,
     PieceData newPieceData,
     PieceTeam newVisualTeam,
     bool newUseAbsorbedPlayerVisual,
     Vector2Int newGridPosition,
     Vector2 newVisualOffset,
     bool newFlipX,
     GameObject newVisualObject)
    {
        actorId =
            newActorId;

        pieceData =
            newPieceData;

        visualTeam =
            newVisualTeam;

        useAbsorbedPlayerVisual =
            newUseAbsorbedPlayerVisual;

        gridPosition =
    newGridPosition;

        visualObject =
            newVisualObject;

        // <변경부분>
        visualOffset =
            newVisualOffset;

        isFlippedX =
            newFlipX;

        // <변경부분>
        // PieceData Visual Prefab에 지정되어 있던 원래 Scale을 보존한다.
        if (visualObject != null)
        {
            // <변경부분>
            // Instantiate된 Visual Prefab의 원래 Local Position을 보존한다.
            visualBaseLocalPosition =
                visualObject.transform.localPosition;

            visualBaseScale =
                visualObject.transform.localScale;

            ApplyVisualTransform();
        }

        gameObject.name =
            $"EventActor_{actorId}";
    }


    // <변경부분>
    // Event Actor Visual의 Offset과 좌우 반전을 적용한다.
    //
    // X Flip 시 단순히 Scale만 반전하면
    // Prefab Pivot이 캐릭터 중앙에서 벗어나 있는 경우
    // 실제 그림이 Pivot 반대편으로 크게 이동한다.
    //
    // 따라서 Flip 전후 Renderer Bounds 중심을 비교해서
    // 실제 캐릭터가 화면상 같은 위치를 유지하도록 자동 보정한다.
    private void ApplyVisualTransform()
    {
        if (visualObject == null)
        {
            return;
        }

        // <변경부분>
        // 먼저 항상 원래 Scale / 원래 위치 기준으로 되돌린다.
        // SetFlipX가 여러 번 호출되어도 보정값이 누적되지 않게 한다.
        visualObject.transform.localPosition =
            visualBaseLocalPosition +
            new Vector3(
                visualOffset.x,
                visualOffset.y,
                0f
            );

        visualObject.transform.localScale =
            visualBaseScale;

        if (isFlippedX == false)
        {
            return;
        }

        // <변경부분>
        // 반전 전 실제 Visual의 화면상 중심점을 저장한다.
        bool hasReferenceCenter =
            TryGetVisualBoundsCenterInActorLocal(
                out Vector3 referenceCenter
            );

        // X축 반전.
        visualObject.transform.localScale =
            new Vector3(
                -visualBaseScale.x,
                visualBaseScale.y,
                visualBaseScale.z
            );

        if (hasReferenceCenter == false)
        {
            return;
        }

        // <변경부분>
        // 반전 후 실제 Renderer 중심을 다시 계산한다.
        if (TryGetVisualBoundsCenterInActorLocal(
                out Vector3 flippedCenter) == false)
        {
            return;
        }

        // <변경부분>
        // Flip 때문에 Renderer 중심이 이동한 거리만큼
        // Visual Transform을 반대로 이동시켜
        // 캐릭터가 같은 위치에서 좌우만 반전되도록 한다.
        Vector3 centerCorrection =
            referenceCenter -
            flippedCenter;

        visualObject.transform.localPosition +=
            new Vector3(
                centerCorrection.x,
                0f,
                0f
            );
    }

    // <변경부분>
    // 현재 Visual에 포함된 모든 Renderer의 Bounds 중심을 계산한다.
    //
    // World Bounds를 EventSceneActor의 Local 좌표로 변환해서
    // WorldRoot 확대 / 축소 여부와 관계없이
    // 동일한 Actor 좌표계에서 Flip 보정을 계산한다.
    private bool TryGetVisualBoundsCenterInActorLocal(
        out Vector3 localCenter)
    {
        localCenter =
            Vector3.zero;

        if (visualObject == null)
        {
            return false;
        }

        Renderer[] renderers =
            visualObject
                .GetComponentsInChildren<
                    Renderer
                >(
                    true
                );

        if (renderers == null ||
            renderers.Length == 0)
        {
            return false;
        }

        bool hasBounds =
            false;

        Bounds combinedBounds =
            new Bounds();

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer renderer =
                renderers[i];

            if (renderer == null ||
                renderer.enabled == false)
            {
                continue;
            }

            if (hasBounds == false)
            {
                combinedBounds =
                    renderer.bounds;

                hasBounds =
                    true;

                continue;
            }

            combinedBounds.Encapsulate(
                renderer.bounds
            );
        }

        if (hasBounds == false)
        {
            return false;
        }

        localCenter =
            transform.InverseTransformPoint(
                combinedBounds.center
            );

        return true;
    }


    // <변경부분>
    // 이후 Move / Attack / 연출 Step에서도
    // Actor의 방향을 변경할 수 있도록 공용 함수로 둔다.
    public void SetFlipX(
        bool flipX)
    {
        isFlippedX =
            flipX;

        ApplyVisualTransform();
    }

    // <변경부분>
    // SpawnActor의 최종 등장 연출을 결정한다.
    //
    // 1. Spine에 Born Clip이 존재하면 Born을 우선 재생한다.
    // 2. Born이 없으면 기존 Fade In을 fallback으로 사용한다.
    //
    // PieceSpineAnimationController가 존재하는 경우에는
    // Battle과 동일한 PlayBornRoutine()을 재사용한다.
    public IEnumerator PlaySpawnAppearanceRoutine(
        float fallbackFadeInDuration)
    {
        if (visualObject == null)
        {
            yield break;
        }

        SkeletonAnimation skeletonAnimation =
            visualObject
                .GetComponentInChildren<
                    SkeletonAnimation
                >(
                    true
                );

        // <변경부분>
        // 실제 SkeletonData에 Born Animation이 존재하는지 검사한다.
        bool hasBornAnimation =
            HasSpineAnimation(
                skeletonAnimation,
                "Born"
            );

        if (hasBornAnimation)
        {
            // <변경부분>
            // Fade 상태가 혹시 남아 있더라도
            // Born Animation은 완전히 보이는 상태에서 시작한다.
            SetCurrentVisualAlpha(
                1f
            );

            PieceSpineAnimationController
                spineAnimator =
                    visualObject
                        .GetComponentInChildren<
                            PieceSpineAnimationController
                        >(
                            true
                        );

            // <변경부분>
            // 기존 기물 Animation Controller가 있다면
            // Battle과 동일한 Born → Idle 흐름을 그대로 재사용한다.
            if (spineAnimator != null)
            {
                yield return
                    spineAnimator
                        .PlayBornRoutine();

                yield break;
            }

            // <변경부분>
            // 구형 / 특수 Visual처럼
            // PieceSpineAnimationController는 없지만
            // 실제 Skeleton에 Born Clip이 존재하는 경우를 위한 fallback.
            yield return
                PlayBornDirectRoutine(
                    skeletonAnimation
                );

            yield break;
        }

        // <변경부분>
        // Born Clip 자체가 없는 Actor만 Fade In으로 등장한다.
        yield return
            PlayFadeInRoutine(
                fallbackFadeInDuration
            );
    }

    // <변경부분>
    // 지정된 Spine Animation Clip이
    // 실제 SkeletonData 안에 존재하는지 확인한다.
    private bool HasSpineAnimation(
        SkeletonAnimation skeletonAnimation,
        string animationName)
    {
        if (skeletonAnimation == null ||
            skeletonAnimation.Skeleton == null ||
            skeletonAnimation.Skeleton.Data == null ||
            string.IsNullOrWhiteSpace(
                animationName))
        {
            return false;
        }

        return
            skeletonAnimation
                .Skeleton
                .Data
                .FindAnimation(
                    animationName
                ) != null;
    }

    // <변경부분>
    // PieceSpineAnimationController가 없는 특수 Visual에서도
    // Born Clip 자체가 있다면 직접 재생한다.
    //
    // Born 종료 후 Idle Clip이 존재하면 Idle로 전환한다.
    private IEnumerator PlayBornDirectRoutine(
        SkeletonAnimation skeletonAnimation)
    {
        if (skeletonAnimation == null ||
            skeletonAnimation.Skeleton == null)
        {
            yield break;
        }

        Spine.TrackEntry trackEntry =
            skeletonAnimation
                .AnimationState
                .SetAnimation(
                    0,
                    "Born",
                    false
                );

        if (trackEntry == null ||
            trackEntry.Animation == null)
        {
            yield break;
        }

        yield return
            new WaitForSeconds(
                trackEntry.Animation.Duration
            );

        // <변경부분>
        // Born 이후 Idle이 존재하는 경우에만
        // 자연스럽게 Idle Loop로 복귀한다.
        if (HasSpineAnimation(
                skeletonAnimation,
                "Idle"))
        {
            skeletonAnimation
                .AnimationState
                .SetAnimation(
                    0,
                    "Idle",
                    true
                );
        }
    }

    // <변경부분>
    // SpawnActor 생성 시 Spine Visual을 부드럽게 Fade In한다.
    //
    // 기존 프로젝트의 SpineFadeController와 동일하게
    // Skeleton RGB는 유지하고 Alpha만 조절한다.
    public IEnumerator PlayFadeInRoutine(
        float duration)
    {
        if (visualObject == null)
        {
            yield break;
        }

        SkeletonAnimation[] skeletonAnimations =
            visualObject
                .GetComponentsInChildren<
                    SkeletonAnimation
                >(
                    true
                );

        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        // <변경부분>
        // Spine Visual이 아닌 경우를 위한 Sprite fallback.
        SpriteRenderer[] spriteRenderers =
            visualObject
                .GetComponentsInChildren<
                    SpriteRenderer
                >(
                    true
                );

        SetVisualAlpha(
            skeletonAnimations,
            spriteRenderers,
            0f
        );

        if (safeDuration <= 0f)
        {
            SetVisualAlpha(
                skeletonAnimations,
                spriteRenderers,
                1f
            );

            yield break;
        }

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsedTime /
                    safeDuration
                );

            SetVisualAlpha(
                skeletonAnimations,
                spriteRenderers,
                t
            );

            yield return null;
        }

        SetVisualAlpha(
            skeletonAnimations,
            spriteRenderers,
            1f
        );
    }

    // <변경부분>
    // 현재 Event Actor Visual 전체의 Alpha를 즉시 변경한다.
    //
    // Born Animation 실행 전 Alpha를 1로 복구하거나
    // 이후 다른 Event 연출에서도 공용으로 사용할 수 있다.
    private void SetCurrentVisualAlpha(
        float alpha)
    {
        if (visualObject == null)
        {
            return;
        }

        SkeletonAnimation[] skeletonAnimations =
            visualObject
                .GetComponentsInChildren<
                    SkeletonAnimation
                >(
                    true
                );

        SpriteRenderer[] spriteRenderers =
            visualObject
                .GetComponentsInChildren<
                    SpriteRenderer
                >(
                    true
                );

        SetVisualAlpha(
            skeletonAnimations,
            spriteRenderers,
            alpha
        );
    }


    // <변경부분>
    // Spine / Sprite Visual의 RGB는 그대로 유지하고
    // Alpha만 변경한다.
    private void SetVisualAlpha(
        SkeletonAnimation[] skeletonAnimations,
        SpriteRenderer[] spriteRenderers,
        float alpha)
    {
        float safeAlpha =
            Mathf.Clamp01(
                alpha
            );

        if (skeletonAnimations != null)
        {
            for (int i = 0;
                 i < skeletonAnimations.Length;
                 i++)
            {
                SkeletonAnimation skeletonAnimation =
                    skeletonAnimations[i];

                if (skeletonAnimation == null ||
                    skeletonAnimation.Skeleton == null)
                {
                    continue;
                }

                Color skeletonColor =
                    skeletonAnimation
                        .Skeleton
                        .GetColor();

                skeletonColor.a =
                    safeAlpha;

                skeletonAnimation
                    .Skeleton
                    .SetColor(
                        skeletonColor
                    );
            }
        }

        if (spriteRenderers != null)
        {
            for (int i = 0;
                 i < spriteRenderers.Length;
                 i++)
            {
                SpriteRenderer spriteRenderer =
                    spriteRenderers[i];

                if (spriteRenderer == null)
                {
                    continue;
                }

                Color spriteColor =
                    spriteRenderer.color;

                spriteColor.a =
                    safeAlpha;

                spriteRenderer.color =
                    spriteColor;
            }
        }
    }

    // <변경부분>
    // Event Actor를 목표 BackgroundTile까지
    // 순수한 포물선 좌표 이동으로 이동시킨다.
    //
    // Battle Piece 이동과 달리
    // Left / Right / Stop 등의 Spine 이동 Animation은 실행하지 않는다.
    //
    // 목적지가 현재 GridPosition과 동일하면
    // X/Y 이동 없이 포물선 높이만 적용되어
    // 제자리에서 위로 뛰었다 다시 내려오는 Jump가 된다.
    public IEnumerator PlayMoveRoutine(
        Vector3 targetWorldPosition,
        Vector2Int targetGridPosition,
        float duration,
        float arcHeight)
    {
        Vector3 startWorldPosition =
            transform.position;

        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        float safeArcHeight =
            Mathf.Max(
                0f,
                arcHeight
            );

        // <변경부분>
        // Event Scene에서는 Battle 이동 Animation을 사용하지 않는다.
        // 현재 Actor Visual Animation 상태는 그대로 유지한 채
        // Actor Root의 위치만 포물선으로 변경한다.
        yield return
            MoveArcRoutine(
                startWorldPosition,
                targetWorldPosition,
                safeDuration,
                safeArcHeight
            );

        // <변경부분>
        // 부동소수점 누적 오차 없이
        // 최종 위치를 목적지 BackgroundTile에 정확히 고정한다.
        transform.position =
            targetWorldPosition;

        // <변경부분>
        // 이후 MoveActor / AttackActor 등의 Step에서
        // 현재 위치를 참조할 수 있도록 Grid 좌표를 갱신한다.
        gridPosition =
            targetGridPosition;
    }


    // <변경부분>
    // Event Actor 전용 포물선 이동.
    //
    // 시작 위치와 도착 위치가 동일해도
    // Sin 곡선의 Y Offset은 유지되므로
    // 자연스럽게 제자리 수직 Jump가 된다.
    private IEnumerator MoveArcRoutine(
        Vector3 startPosition,
        Vector3 endPosition,
        float duration,
        float arcHeight)
    {
        if (duration <= 0f)
        {
            transform.position =
                endPosition;

            yield break;
        }

        float elapsedTime =
            0f;

        while (elapsedTime <
               duration)
        {
            elapsedTime +=
                Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );

            float easedTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            Vector3 currentPosition =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    easedTime
                );

            float arcOffset =
                Mathf.Sin(
                    normalizedTime *
                    Mathf.PI
                ) *
                arcHeight;

            currentPosition.y +=
                arcOffset;

            transform.position =
                currentPosition;

            yield return null;
        }

        transform.position =
            endPosition;
    }

    // <변경부분>
    // 이후 MoveActor 구현 시
    // Actor의 현재 BackgroundTile 좌표를 갱신한다.
    public void SetGridPosition(
        Vector2Int newGridPosition)
    {
        gridPosition =
            newGridPosition;
    }
}
