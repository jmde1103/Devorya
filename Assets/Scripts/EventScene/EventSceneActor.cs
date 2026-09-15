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
    // Event Scene Actor가 소유하는 공용 SpeechBubble UI.
    //
    // Battle Piece와 동일한 ActorSpeechBubbleUI를 재사용하지만
    // Battle Piece 시스템에는 의존하지 않는다.
    private ActorSpeechBubbleUI speechBubbleUI;

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
    // SpawnActor가 생성한 공용 SpeechBubble UI를
    // 현재 Event Scene Actor에 연결한다.
    //
    // SpeechBubble Prefab은 Actor Root의 자식이므로
    // Actor 이동 / 공격 / WorldRoot 이동을 별도 추적 없이 따라간다.
    public void SetSpeechBubbleUI(
        ActorSpeechBubbleUI newSpeechBubbleUI)
    {
        speechBubbleUI =
            newSpeechBubbleUI;

        if (speechBubbleUI == null)
        {
            return;
        }

        // 생성 직후에는 말풍선이 보이지 않도록 정리한다.
        speechBubbleUI.HideImmediately();
    }


    // <변경부분>
    // Wait For Complete = true용 SpeechBubble 실행.
    //
    // ActorSpeechBubbleUI의 전체 연출:
    // Fade In / Typewriter / 자동 크기 / Idle Float /
    // Emphasis Shake / Fade Out을 그대로 재사용한다.
    public IEnumerator PlaySpeechBubbleRoutine(
        string text,
        float duration,
        float typingSpeed,
        bool useEmphasisShake,
        float emphasisStrength)
    {
        if (speechBubbleUI == null)
        {
            Debug.LogWarning(
                $"Event Scene SpeechBubble 실행 실패: " +
                $"Actor '{actorId}'에 ActorSpeechBubbleUI가 연결되지 않았습니다."
            );

            yield break;
        }

        yield return
            speechBubbleUI.PlayRoutine(
                text,
                duration,
                typingSpeed,
                useEmphasisShake,
                emphasisStrength
            );
    }


    // <변경부분>
    // Wait For Complete = false용 SpeechBubble 실행.
    //
    // 말풍선은 독립적으로 계속 재생되고
    // Event Scene Sequence는 즉시 다음 Step으로 진행할 수 있다.
    public void PlaySpeechBubbleDetached(
        string text,
        float duration,
        float typingSpeed,
        bool useEmphasisShake,
        float emphasisStrength)
    {
        if (speechBubbleUI == null)
        {
            Debug.LogWarning(
                $"Event Scene SpeechBubble 실행 실패: " +
                $"Actor '{actorId}'에 ActorSpeechBubbleUI가 연결되지 않았습니다."
            );

            return;
        }

        speechBubbleUI.PlayDetached(
            text,
            duration,
            typingSpeed,
            useEmphasisShake,
            emphasisStrength
        );
    }


    // <변경부분>
    // Event Scene Actor의 Spine Animation을
    // Animation 이름으로 직접 실행한다.
    //
    // Unity Animator / AnimationClip을 사용하지 않고
    // Spine SkeletonAnimation.AnimationState를 직접 사용한다.
    //
    // loop = true:
    // 지정 Animation을 반복 재생하고 즉시 다음 Step으로 진행한다.
    //
    // loop = false + waitForComplete = true:
    // Animation 종료까지 기다린다.
    //
    // returnToIdle = true:
    // 비반복 Animation 종료 후 Idle이 존재하면 Idle Loop로 복귀한다.
    public IEnumerator PlayAnimationRoutine(
    string animationName,
    bool loop,
    bool waitForComplete,
    bool returnToIdle,
    float mixDuration)
    {
        if (visualObject == null)
        {
            Debug.LogWarning(
                $"Event Scene Actor Animation 실패: " +
                $"Actor '{actorId}'의 Visual Object가 없습니다."
            );

            yield break;
        }

        SkeletonAnimation skeletonAnimation =
            visualObject
                .GetComponentInChildren<
                    SkeletonAnimation
                >(
                    true
                );

        if (skeletonAnimation == null)
        {
            Debug.LogWarning(
                $"Event Scene Actor Animation 실패: " +
                $"Actor '{actorId}'에서 SkeletonAnimation을 찾을 수 없습니다."
            );

            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                animationName))
        {
            Debug.LogWarning(
                $"Event Scene Actor Animation 실패: " +
                $"Actor '{actorId}'의 Animation Name이 비어 있습니다."
            );

            yield break;
        }

        // <변경부분>
        // 음수 Mix 시간이 들어오지 않도록 보정한다.
        float safeMixDuration =
            Mathf.Max(
                0f,
                mixDuration
            );

        // <변경부분>
        // 일반 Unity AnimationClip이 아니라
        // 실제 Spine SkeletonData 안의 Animation 이름을 검사한다.
        if (HasSpineAnimation(
                skeletonAnimation,
                animationName) == false)
        {
            Debug.LogWarning(
                $"Event Scene Actor Animation 실패: " +
                $"Actor '{actorId}'의 Spine SkeletonData에 " +
                $"'{animationName}' Animation이 없습니다."
            );

            yield break;
        }

        // <변경부분>
        // Spine AnimationState에 직접 Animation을 설정한다.
        Spine.TrackEntry trackEntry =
    skeletonAnimation
        .AnimationState
        .SetAnimation(
            0,
            animationName,
            loop
        );

        // <변경부분>
        // 현재 Track Animation의 Pose에서
        // 새 Animation Pose로 즉시 끊어지지 않고
        // 지정 시간 동안 Spine Mix를 적용한다.
        //
        // AnimationStateData의 전역 DefaultMix를 수정하지 않고
        // 현재 TrackEntry에만 적용하여
        // 같은 SkeletonDataAsset을 사용하는 다른 Actor에는 영향을 주지 않는다.
        if (trackEntry != null)
        {
            trackEntry.MixDuration =
                safeMixDuration;
        }

        if (trackEntry == null ||
            trackEntry.Animation == null)
        {
            Debug.LogWarning(
                $"Event Scene Actor Animation 실패: " +
                $"'{animationName}' 재생 Track을 생성하지 못했습니다."
            );

            yield break;
        }

        // <변경부분>
        // Loop Animation은 종료 시점이 없으므로
        // 재생을 시작한 뒤 즉시 다음 Event Step으로 진행한다.
        if (loop)
        {
            yield break;
        }

        // <변경부분>
        // 완료 대기를 사용하지 않는 경우에도
        // 재생 자체는 이미 시작된 상태다.
        if (waitForComplete == false)
        {
            yield break;
        }

        float animationDuration =
            Mathf.Max(
                0f,
                trackEntry.Animation.Duration
            );

        float elapsedTime =
            0f;

        while (elapsedTime <
               animationDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }

        if (returnToIdle == false)
        {
            yield break;
        }

        // <변경부분>
        // Animation 종료 후 Idle Clip이 실제로 존재하는 경우에만
        // Idle Loop로 복귀한다.
        if (HasSpineAnimation(
                skeletonAnimation,
                "Idle") == false)
        {
            Debug.LogWarning(
                $"Event Scene Actor Animation: " +
                $"Actor '{actorId}'에 Idle Animation이 없어 " +
                $"마지막 상태를 유지합니다."
            );

            yield break;
        }

        // <변경부분>
        // 현재 Animation의 마지막 Pose에서
        // Idle 첫 Pose로 즉시 끊어지지 않도록
        // 동일한 Mix 시간을 적용한다.
        Spine.TrackEntry idleTrackEntry =
            skeletonAnimation
                .AnimationState
                .SetAnimation(
                    0,
                    "Idle",
                    true
                );

        if (idleTrackEntry != null)
        {
            idleTrackEntry.MixDuration =
                safeMixDuration;
        }
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
    // Event Scene Actor를 부드럽게 Fade Out한다.
    //
    // 실제 GameObject 제거와 activeActors Dictionary 정리는
    // EventSceneSequenceController가 담당한다.
    //
    // 이 함수는 Visual 연출만 담당한다.
    public IEnumerator PlayFadeOutRoutine(
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

        SpriteRenderer[] spriteRenderers =
            visualObject
                .GetComponentsInChildren<
                    SpriteRenderer
                >(
                    true
                );

        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        // <변경부분>
        // Duration이 0이면 Fade 과정 없이
        // 바로 완전히 투명하게 만든다.
        if (safeDuration <= 0f)
        {
            SetVisualAlpha(
                skeletonAnimations,
                spriteRenderers,
                0f
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
                1f - t
            );

            yield return null;
        }

        // <변경부분>
        // 마지막 Frame에서 Alpha를 정확히 0으로 고정한다.
        SetVisualAlpha(
            skeletonAnimations,
            spriteRenderers,
            0f
        );
    }

    // <변경부분>
    // Event Scene Actor 전용 간단한 피격 흔들림.
    //
    // Random 값을 사용하지 않고 Sin 곡선을 사용하여
    // 동일한 설정에서는 항상 동일한 흔들림 연출이 나오도록 한다.
    //
    // 연출 종료 후 Actor Root를 반드시 원래 위치로 복귀시킨다.
    public IEnumerator PlayShakeRoutine(
        float duration,
        float intensity)
    {
        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        float safeIntensity =
            Mathf.Max(
                0f,
                intensity
            );

        if (safeDuration <= 0f ||
            safeIntensity <= 0f)
        {
            yield break;
        }

        Vector3 originalPosition =
            transform.position;

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    safeDuration
                );

            // <변경부분>
            // 흔들림이 끝으로 갈수록 자연스럽게 약해진다.
            float damping =
                1f -
                normalizedTime;

            float xOffset =
                Mathf.Sin(
                    normalizedTime *
                    Mathf.PI *
                    8f
                ) *
                safeIntensity *
                damping;

            float yOffset =
                Mathf.Sin(
                    normalizedTime *
                    Mathf.PI *
                    6f
                ) *
                safeIntensity *
                0.35f *
                damping;

            transform.position =
                originalPosition +
                new Vector3(
                    xOffset,
                    yOffset,
                    0f
                );

            yield return null;
        }

        // <변경부분>
        // 누적 좌표 오차가 남지 않도록 정확한 원래 위치로 복귀.
        transform.position =
            originalPosition;
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
    // Event Scene 전용 공격 이동 연출.
    //
    // Success:
    // 공격자가 Target 위치까지 이동
    // → Target 위치/GridPosition 점유
    // → 성공 충돌 Callback 실행
    // → 원래 위치로 돌아가지 않는다.
    //
    // Failure:
    // Target 앞 충돌 지점까지 이동
    // → 실패 충돌 Callback으로 Camera Shake 실행
    // → Battle Defense 방식으로
    //   뒤로 튕김 → 큰 Bounce → 작은 Bounce → 원위치 복귀.
    //
    // Battle Damage / Turn 로직은 사용하지 않는다.
    public IEnumerator PlayAttackRoutine(
        EventSceneActor targetActor,
        EventSceneAttackResult attackResult,
        float approachDuration,
        float failureImpactRatio,
        float arcHeight,
        float failureFallShortDistance,
        float failureFallBackDuration,
        float failureFirstBounceDuration,
        float failureFirstBounceHeight,
        float failureSecondBounceDuration,
        float failureSecondBounceHeight,
        float failureFinalReturnDuration,
        System.Action onSuccessImpact,
        System.Action onFailureImpact)
    {
        if (targetActor == null)
        {
            yield break;
        }

        Vector3 startWorldPosition =
            transform.position;

        // <변경부분>
        // Success Callback에서 Target이 제거되므로
        // 필요한 위치와 Grid 좌표를 제거 전에 미리 저장한다.
        Vector3 targetWorldPosition =
            targetActor.transform.position;

        Vector2Int targetGridPosition =
            targetActor.GridPosition;

        float safeApproachDuration =
            Mathf.Max(
                0f,
                approachDuration
            );

        float safeImpactRatio =
            Mathf.Clamp01(
                failureImpactRatio
            );

        float safeArcHeight =
            Mathf.Max(
                0f,
                arcHeight
            );


        // =====================================================
        // Success
        // =====================================================

        if (attackResult ==
            EventSceneAttackResult.Success)
        {
            // <변경부분>
            // 성공 공격은 Target 위치까지 완전히 이동한다.
            yield return
                MoveArcRoutine(
                    startWorldPosition,
                    targetWorldPosition,
                    safeApproachDuration,
                    safeArcHeight
                );

            transform.position =
                targetWorldPosition;

            // <변경부분>
            // 공격자가 제거된 Target의 Tile을 점유한다.
            gridPosition =
                targetGridPosition;

            // <변경부분>
            // 충돌 순간 Controller에서
            // Screen Shake와 Target 제거를 처리한다.
            onSuccessImpact?.Invoke();

            yield break;
        }


        // =====================================================
        // Failure
        // =====================================================

        // <변경부분>
        // 실패 공격은 Target까지 완전히 들어가지 않고
        // 지정된 충돌 비율 지점에서 방어에 막힌다.
        Vector3 blockedImpactPosition =
            Vector3.Lerp(
                startWorldPosition,
                targetWorldPosition,
                safeImpactRatio
            );

        yield return
            MoveArcRoutine(
                startWorldPosition,
                blockedImpactPosition,
                safeApproachDuration,
                safeArcHeight
            );

        // <변경부분>
        // 방어 충돌 순간 Actor 자체를 흔들지 않고
        // Controller에 Camera Screen Shake를 요청한다.
        onFailureImpact?.Invoke();


        // <변경부분>
        // Battle Defense Bounce와 동일한 방향 기준을 사용한다.
        Vector3 attackDirection =
            targetWorldPosition -
            startWorldPosition;

        if (attackDirection.sqrMagnitude <=
            0.0001f)
        {
            attackDirection =
                Vector3.right;
        }

        attackDirection.Normalize();

        float safeFallShortDistance =
            Mathf.Max(
                0f,
                failureFallShortDistance
            );

        Vector3 fallShortPosition =
            startWorldPosition +
            attackDirection *
            safeFallShortDistance;

        Vector3 firstBounceLandingPosition =
            startWorldPosition +
            attackDirection *
            (
                safeFallShortDistance *
                0.45f
            );

        Vector3 secondBounceLandingPosition =
            startWorldPosition +
            attackDirection *
            (
                safeFallShortDistance *
                0.15f
            );


        // <변경부분>
        // 충돌 지점에서 뒤로 튕겨 첫 착지 지점까지 이동한다.
        yield return
            MoveArcRoutine(
                blockedImpactPosition,
                fallShortPosition,
                Mathf.Max(
                    0f,
                    failureFallBackDuration
                ),
                Mathf.Max(
                    0f,
                    failureSecondBounceHeight
                )
            );


        // <변경부분>
        // 첫 번째 큰 Bounce.
        yield return
            MoveArcRoutine(
                fallShortPosition,
                firstBounceLandingPosition,
                Mathf.Max(
                    0f,
                    failureFirstBounceDuration
                ),
                Mathf.Max(
                    0f,
                    failureFirstBounceHeight
                )
            );


        // <변경부분>
        // 두 번째 작은 Bounce.
        yield return
            MoveArcRoutine(
                firstBounceLandingPosition,
                secondBounceLandingPosition,
                Mathf.Max(
                    0f,
                    failureSecondBounceDuration
                ),
                Mathf.Max(
                    0f,
                    failureSecondBounceHeight
                )
            );


        // <변경부분>
        // 마지막에는 높이 없이 원래 위치에 정확히 복귀한다.
        yield return
            MoveArcRoutine(
                secondBounceLandingPosition,
                startWorldPosition,
                Mathf.Max(
                    0f,
                    failureFinalReturnDuration
                ),
                0f
            );

        transform.position =
            startWorldPosition;

        // Failure에서는 실제 Tile 이동이 아니므로
        // gridPosition은 기존 값을 유지한다.
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
