using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// <변경부분>
// Event Scene 전용 Sequence 실행 Controller.
//
// 기존 EventSequenceController와 완전히 분리되어 있으며,
// BattleManager / PieceManager / BoardManager를 사용하지 않는다.
//
// 현재 1차 구현:
// - Dialogue
// - Wait
// - CompleteSequence
//
// Actor / Camera 관련 Step은 이후 하나씩 추가한다.
public class EventSceneSequenceController : MonoBehaviour
{
    // <변경부분>
    // 현재 실행 중인 EventSceneData.
    //
    // Inspector에서 별도로 Data를 연결하지 않는다.
    // EventSceneController가 StartSequence() 호출 시
    // 현재 EventSceneData를 직접 전달한다.
    private EventSceneData currentEventSceneData;

    [Header("Scene References")]

    [SerializeField]
    private EventGuideUI eventGuideUI;

    // <변경부분>
    // Event Scene에서 BackgroundTile 좌표를 조회한다.
    [SerializeField]
    private BackgroundManager backgroundManager;

    // <변경부분>
    // 생성된 EventSceneActor를 정리해서 담을 부모.
    //
    // Scene의 EventActors 오브젝트를 연결한다.
    [SerializeField]
    private Transform eventActorsRoot;

    // <변경부분>
    // 모든 EventSceneActor가 공통으로 사용할
    // World Space SpeechBubble Prefab.
    //
    // Prefab Asset 자체를 Inspector에서 연결하기 위해
    // GameObject Reference로 보관한다.
    [SerializeField]
    private GameObject actorSpeechBubblePrefab;


    // <변경부분>
    // Event Scene의 화면 흔들림 대상.
    [SerializeField]
    private Transform cameraShakeTarget;


    // <변경부분>
    // 현재 실행 중인 Event Scene Camera Shake Coroutine.
    //
    // 새로운 충격이 들어오면 기존 Shake를 정리한 뒤
    // 새로운 Shake를 시작한다.
    private Coroutine cameraShakeCoroutine;


    // <변경부분>
    // 현재 실제로 흔들고 있는 Camera Transform.
    private Transform activeCameraShakeTarget;


    // <변경부분>
    // Camera Shake 시작 직전의 정확한 Local Position.
    //
    // Shake가 끝나거나 강제로 중단될 때
    // 반드시 이 좌표로 복구한다.
    private Vector3 cameraShakeBaseLocalPosition;

    private bool isSequenceActive =
        false;

    private int currentStepIndex =
        -1;

    private Coroutine sequenceCoroutine =
        null;

    // <변경부분>
    // Actor ID를 기준으로 현재 Event Scene에 존재하는 Actor를 관리한다.
    //
    // 이후 Move / Attack / Absorb / Animation / Remove에서
    // 동일 Dictionary를 재사용한다.
    private readonly Dictionary<string, EventSceneActor>
        activeActors =
            new Dictionary<string, EventSceneActor>();

    public bool IsSequenceActive =>
        isSequenceActive;

    public int CurrentStepIndex =>
        currentStepIndex;


    // <변경부분>
    // EventSceneController가 전달한 EventSceneData 하나를 기준으로
    // 현재 Event Scene Sequence를 실행한다.
    //
    // 별도의 EventSceneSequenceData Asset은 사용하지 않는다.
    public void StartSequence(
        EventSceneData newEventSceneData)
    {
        if (newEventSceneData == null)
        {
            Debug.LogWarning(
                "Event Scene Sequence 시작 실패: " +
                "전달된 EventSceneData가 null입니다."
            );

            return;
        }

        if (newEventSceneData.IsValid() == false)
        {
            Debug.LogWarning(
                $"Event Scene Sequence 시작 실패: " +
                $"{newEventSceneData.name} 데이터가 유효하지 않습니다."
            );

            return;
        }

        if (isSequenceActive)
        {
            return;
        }

        StopSequenceCoroutine();

        // <변경부분>
        // 실행 중에만 현재 EventSceneData를 보관한다.
        currentEventSceneData =
            newEventSceneData;

        sequenceCoroutine =
            StartCoroutine(
                RunSequenceRoutine()
            );
    }


    // <변경부분>
    // Event Scene Step을 위에서 아래 순서대로 실행한다.
    private IEnumerator RunSequenceRoutine()
    {
        isSequenceActive =
            true;

        currentStepIndex =
            -1;

        // <변경부분>
        // EventSceneData 자체가 Event 이름과 Step 목록을 소유한다.
        Debug.Log(
            $"Event Scene Sequence 시작: " +
            $"{currentEventSceneData.eventName}"
        );

        List<EventSceneStepData> steps =
            currentEventSceneData.steps;

        Debug.Log(
            $"Event Scene Step 실행 준비: " +
            $"{(steps != null ? steps.Count : -1)}개"
        );

        if (steps == null ||
            steps.Count == 0)
        {
            Debug.LogWarning(
                "Event Scene Sequence 실행 실패: " +
                "실행할 Step이 없습니다."
            );

            isSequenceActive =
                false;

            sequenceCoroutine =
                null;

            yield break;
        }

        for (int i = 0;
             i < steps.Count;
             i++)
        {
            if (isSequenceActive == false)
            {
                sequenceCoroutine =
                    null;

                yield break;
            }

            EventSceneStepData step =
                steps[i];

            currentStepIndex =
                i;

            if (step == null)
            {
                Debug.LogWarning(
                    $"Event Scene Step 건너뜀: " +
                    $"{i}번 데이터가 null입니다."
                );

                continue;
            }

            Debug.Log(
                $"Event Scene Step 시작: " +
                $"{i} / " +
                $"{step.stepName} / " +
                $"{step.stepType}"
            );

            yield return
                ExecuteStepRoutine(
                    step
                );

            if (isSequenceActive == false)
            {
                sequenceCoroutine =
                    null;

                yield break;
            }
        }

        // 모든 Step을 정상적으로 끝까지 실행한 경우에도
        // Sequence 완료 처리를 실행한다.
        CompleteSequence();

        sequenceCoroutine =
            null;
    }


    // <변경부분>
    // 현재 Event Scene Step Type에 따라 실행 기능을 분기한다.
    private IEnumerator ExecuteStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        switch (step.stepType)
        {
            case EventSceneStepType.None:
                yield break;

            case EventSceneStepType.Dialogue:
                yield return
                    ExecuteDialogueStepRoutine(
                        step
                    );

                yield break;

            case EventSceneStepType.Wait:
                yield return
                    ExecuteWaitStepRoutine(
                        step
                    );

                yield break;

            case EventSceneStepType.CompleteSequence:
                CompleteSequence();

                yield break;

            // <변경부분>
            // 아래 Step들은 Enum 구조만 먼저 확보한 상태이며
            // 실제 기능은 다음 단계부터 하나씩 구현한다.
            case EventSceneStepType.SpawnActor:

                yield return
                    ExecuteSpawnActorStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            case EventSceneStepType.MoveActor:

                yield return
                    ExecuteMoveActorStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // 지정 Actor의 Spine Animation을 실행한다.
            case EventSceneStepType.PlayActorAnimation:

                yield return
                    ExecutePlayActorAnimationStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // 지정 Actor를 Event Scene에서 제거한다.
            case EventSceneStepType.RemoveActor:

                yield return
                    ExecuteRemoveActorStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // Event Scene 전용 공격 연출.
            case EventSceneStepType.AttackActor:

                yield return
                    ExecuteAttackActorStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // 독립 Screen Shake 연출.
            case EventSceneStepType.ScreenShake:

                yield return
                    ExecuteScreenShakeStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // 지정 Event Actor 위에 SpeechBubble을 표시한다.
            case EventSceneStepType.SpeechBubble:

                yield return
                    ExecuteSpeechBubbleStepRoutine(
                        step
                    );

                yield break;


            case EventSceneStepType.AbsorbActor:
            case EventSceneStepType.CameraShot:

                Debug.LogWarning(
                    $"Event Scene Step 미구현: " +
                    $"{step.stepType}"
                );

                yield break;
        }
    }


    // <변경부분>
    // SpawnActor Step 실행.
    //
    // PieceData의 Battle 로직은 생성하지 않고
    // 지정된 Spine Visual Prefab만 Event Actor 아래에 생성한다.
    private IEnumerator ExecuteSpawnActorStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                step.spawnActorId))
        {
            Debug.LogWarning(
                "Event Scene SpawnActor 실패: " +
                "Actor ID가 비어 있습니다."
            );

            yield break;
        }

        if (step.spawnActorPieceData == null)
        {
            Debug.LogWarning(
                $"Event Scene SpawnActor 실패: " +
                $"{step.spawnActorId}의 PieceData가 없습니다."
            );

            yield break;
        }

        if (backgroundManager == null)
        {
            Debug.LogWarning(
                "Event Scene SpawnActor 실패: " +
                "BackgroundManager가 연결되지 않았습니다."
            );

            yield break;
        }

        if (eventActorsRoot == null)
        {
            Debug.LogWarning(
                "Event Scene SpawnActor 실패: " +
                "EventActors Root가 연결되지 않았습니다."
            );

            yield break;
        }

        // <변경부분>
        // 이후 다른 Step에서 Actor ID 하나가
        // 정확히 하나의 Actor만 가리키도록 중복 생성을 막는다.
        if (activeActors.ContainsKey(
                step.spawnActorId))
        {
            Debug.LogWarning(
                $"Event Scene SpawnActor 실패: " +
                $"Actor ID '{step.spawnActorId}'가 이미 존재합니다."
            );

            yield break;
        }

        BackgroundTile spawnTile =
            backgroundManager
                .GetBackgroundTileAt(
                    step.spawnActorPosition.x,
                    step.spawnActorPosition.y
                );

        if (spawnTile == null)
        {
            Debug.LogWarning(
                $"Event Scene SpawnActor 실패: " +
                $"BackgroundTile " +
                $"({step.spawnActorPosition.x}, " +
                $"{step.spawnActorPosition.y})를 찾을 수 없습니다."
            );

            yield break;
        }

        GameObject visualPrefab =
            step.spawnActorPieceData
                .GetSpineVisualPrefab(
                    step.spawnActorTeam,
                    step.spawnActorUseAbsorbedPlayerVisual
                );

        if (visualPrefab == null)
        {
            Debug.LogWarning(
                $"Event Scene SpawnActor 실패: " +
                $"{step.spawnActorPieceData.name}에 " +
                $"{step.spawnActorTeam} Spine Visual Prefab이 없습니다."
            );

            yield break;
        }

        // <변경부분>
        // 실제 Actor Root 생성.
        GameObject actorObject =
            new GameObject(
                $"EventActor_{step.spawnActorId}"
            );

        Transform actorTransform =
            actorObject.transform;

        actorTransform.SetParent(
            eventActorsRoot,
            false
        );

        actorTransform.position =
            spawnTile.transform.position;

        // <변경부분>
        // PieceData가 이미 관리 중인 Spine Visual Prefab만 재사용한다.
        GameObject visualObject =
    Instantiate(
        visualPrefab,
        actorTransform
    );

        // <변경부분>
        // Position과 Scale은 EventSceneActor가
        // Visual Offset / Flip X 설정을 기준으로 적용한다.
        visualObject.transform.localRotation =
            Quaternion.identity;

        EventSceneActor actor =
    actorObject.AddComponent<
        EventSceneActor
    >();

        actor.Initialize(
            step.spawnActorId,
            step.spawnActorPieceData,
            step.spawnActorTeam,
            step.spawnActorUseAbsorbedPlayerVisual,
            step.spawnActorPosition,
            step.spawnActorVisualOffset,
            step.spawnActorFlipX,
            visualObject
        );


        // <변경부분>
        // Battle Piece에서 검증한 공용 SpeechBubble Prefab을
        // Event Actor Root의 자식으로 생성한다.
        //
        // Prefab Asset은 GameObject로 연결하고,
        // 생성된 Instance 내부에서 ActorSpeechBubbleUI를 찾는다.
        if (actorSpeechBubblePrefab != null)
        {
            GameObject speechBubbleObject =
                Instantiate(
                    actorSpeechBubblePrefab,
                    actorTransform,
                    false
                );

            speechBubbleObject.name =
                "SpeechBubbleCanvas";

            ActorSpeechBubbleUI speechBubbleUI =
                speechBubbleObject
                    .GetComponentInChildren<
                        ActorSpeechBubbleUI
                    >(
                        true
                    );

            if (speechBubbleUI == null)
            {
                Debug.LogWarning(
                    $"Event Scene SpawnActor: " +
                    $"ActorSpeechBubblePrefab에서 " +
                    $"ActorSpeechBubbleUI를 찾을 수 없습니다. " +
                    $"Actor '{step.spawnActorId}'"
                );

                Destroy(
                    speechBubbleObject
                );
            }
            else
            {
                // <변경부분>
                // 현재 Event Actor가 사용하는 PieceData의
                // 기물별 SpeechBubble 높이 Offset을 적용한다.
                speechBubbleUI.SetPieceDataOffsetY(
                    step.spawnActorPieceData
                        .speechBubbleOffsetY
                );

                actor.SetSpeechBubbleUI(
                    speechBubbleUI
                );
            }
        }
        else
        {
            // SpeechBubble Prefab이 없어도
            // 기존 Event Actor Spawn 자체는 정상적으로 유지한다.
            Debug.LogWarning(
                $"Event Scene SpawnActor: " +
                $"ActorSpeechBubblePrefab이 연결되지 않았습니다. " +
                $"Actor '{step.spawnActorId}'는 말풍선을 사용할 수 없습니다."
            );
        }
       
        activeActors.Add(
            step.spawnActorId,
            actor
        );

        // <변경부분>
        // SpawnActor 등장 연출.
        //
        // Born Clip이 있으면:
        // Born → Idle
        //
        // Born Clip이 없으면:
        // Fade In
        //
        // 두 경우 모두 연출이 끝난 뒤
        // 다음 Event Step으로 진행한다.
        yield return
            actor.PlaySpawnAppearanceRoutine(
                step.spawnActorFadeInDuration
            );

        Debug.Log(
                    $"Event Scene Actor 생성 완료: " +
            $"{step.spawnActorId} / " +
            $"Tile ({step.spawnActorPosition.x}, " +
            $"{step.spawnActorPosition.y})"
        );

        yield break;
    }

    // <변경부분>
    // MoveActor Step 실행.
    //
    // 지정 Actor를 현재 위치에서 목적지 BackgroundTile까지
    // 포물선으로 이동시킨다.
    //
    // 목적지가 현재 Actor GridPosition과 동일하면
    // 제자리 수직 Jump로 처리한다.
    private IEnumerator ExecuteMoveActorStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                step.moveActorId))
        {
            Debug.LogWarning(
                "Event Scene MoveActor 실패: " +
                "Actor ID가 비어 있습니다."
            );

            yield break;
        }

        if (activeActors.TryGetValue(
                step.moveActorId,
                out EventSceneActor actor) == false ||
            actor == null)
        {
            Debug.LogWarning(
                $"Event Scene MoveActor 실패: " +
                $"Actor ID '{step.moveActorId}'를 찾을 수 없습니다."
            );

            yield break;
        }

        if (backgroundManager == null)
        {
            Debug.LogWarning(
                "Event Scene MoveActor 실패: " +
                "BackgroundManager가 연결되지 않았습니다."
            );

            yield break;
        }

        BackgroundTile destinationTile =
            backgroundManager
                .GetBackgroundTileAt(
                    step.moveActorDestination.x,
                    step.moveActorDestination.y
                );

        if (destinationTile == null)
        {
            Debug.LogWarning(
                $"Event Scene MoveActor 실패: " +
                $"BackgroundTile " +
                $"({step.moveActorDestination.x}, " +
                $"{step.moveActorDestination.y})를 찾을 수 없습니다."
            );

            yield break;
        }

        // <변경부분>
        // 이 MoveActor Step에서 방향 변경이 지정되어 있다면
        // 포물선 이동을 시작하기 전에 Actor의 Flip X를 적용한다.
        //
        // Event Scene에서는 이동 방향을 자동 판단하지 않고
        // 제작자가 지정한 방향을 그대로 사용한다.
        if (step.moveActorChangeFlipX)
        {
            actor.SetFlipX(
                step.moveActorFlipX
            );
        }

        bool isJump =
            actor.GridPosition ==
            step.moveActorDestination;

        Debug.Log(
                    isJump
                ? $"Event Scene Actor 제자리 Jump: {step.moveActorId}"
                : $"Event Scene Actor 이동: " +
                  $"{step.moveActorId} / " +
                  $"{actor.GridPosition} → " +
                  $"{step.moveActorDestination}"
        );

        yield return
            actor.PlayMoveRoutine(
                destinationTile.transform.position,
                step.moveActorDestination,
                step.moveActorDuration,
                step.moveActorArcHeight
            );
    }

    // <변경부분>
    // AttackActor Step 실행.
    //
    // Event Scene의 두 Actor를 ID로 찾아
    // Battle 판정 없이 순수 공격 연출만 실행한다.
    //
    // Success:
    // Target 접근 → Target Shake → 공격자 원위치.
    //
    // Failure:
    // Target 옆으로 빗나감 → 공격자 원위치.
    //
    // Target 제거는 이 Step에서 자동으로 하지 않는다.
    private IEnumerator ExecuteAttackActorStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                step.attackActorId))
        {
            Debug.LogWarning(
                "Event Scene AttackActor 실패: " +
                "Attacker ID가 비어 있습니다."
            );

            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                step.attackTargetActorId))
        {
            Debug.LogWarning(
                "Event Scene AttackActor 실패: " +
                "Target ID가 비어 있습니다."
            );

            yield break;
        }

        if (step.attackActorId ==
            step.attackTargetActorId)
        {
            Debug.LogWarning(
                $"Event Scene AttackActor 실패: " +
                $"Attacker와 Target이 동일한 Actor입니다. " +
                $"'{step.attackActorId}'"
            );

            yield break;
        }

        // <변경부분>
        // 공격자 Actor 조회.
        if (activeActors.TryGetValue(
                step.attackActorId,
                out EventSceneActor attacker) == false ||
            attacker == null)
        {
            Debug.LogWarning(
                $"Event Scene AttackActor 실패: " +
                $"Attacker ID '{step.attackActorId}'를 찾을 수 없습니다."
            );

            yield break;
        }

        // <변경부분>
        // Target Actor 조회.
        if (activeActors.TryGetValue(
                step.attackTargetActorId,
                out EventSceneActor targetActor) == false ||
            targetActor == null)
        {
            Debug.LogWarning(
                $"Event Scene AttackActor 실패: " +
                $"Target ID '{step.attackTargetActorId}'를 찾을 수 없습니다."
            );

            yield break;
        }

        // <변경부분>
        // 제작자가 이 Attack Step에서 방향 변경을 지정한 경우에만
        // 공격 시작 전에 Flip X를 적용한다.
        if (step.attackChangeFlipX)
        {
            attacker.SetFlipX(
                step.attackFlipX
            );
        }

        Debug.Log(
            $"Event Scene AttackActor 실행: " +
            $"{step.attackActorId} → " +
            $"{step.attackTargetActorId} / " +
            $"{step.attackResult}"
        );

        // <변경부분>
        // Success 충돌 순간 Target을 실제 Runtime Actor 목록에서도 제거한다.
        //
        // 공격자 EventSceneActor는 Target의 GridPosition을 미리 저장한 뒤
        // 이 Callback을 실행하므로 Target GameObject가 제거되어도
        // 공격자는 해당 위치를 정상적으로 점유할 수 있다.
        // <변경부분>
        // Success 충돌 순간:
        //
        // 1. 화면 흔들림
        // 2. Target Runtime 목록 제거
        // 3. Target 즉시 숨김 / 제거
        //
        // 순서로 처리한다.
        System.Action onSuccessImpact =
            () =>
            {
                if (step.attackResult !=
                    EventSceneAttackResult.Success)
                {
                    return;
                }

                // <변경부분>
                // 성공 공격의 실제 타격 순간에
                // Actor가 아니라 화면 전체를 흔든다.
                StartCameraShake(
                    step.attackTargetShakeDuration,
                    step.attackTargetShakeIntensity
                );

                activeActors.Remove(
                    step.attackTargetActorId
                );

                if (targetActor != null &&
                    targetActor.gameObject != null)
                {
                    targetActor.gameObject.SetActive(
                        false
                    );

                    Destroy(
                        targetActor.gameObject
                    );
                }

                Debug.Log(
                    $"Event Scene AttackActor Target 제거: " +
                    $"{step.attackTargetActorId}"
                );
            };

        // <변경부분>
        // Failure도 공격이 빗나간 것이 아니라
        // Defense에 실제로 충돌한 연출이므로
        // 충돌 순간 동일한 Screen Shake를 실행한다.
        //
        // Target Actor 자체는 흔들지 않는다.
        System.Action onFailureImpact =
            () =>
            {
                if (step.attackResult !=
                    EventSceneAttackResult.Failure)
                {
                    return;
                }

                StartCameraShake(
                    step.attackTargetShakeDuration,
                    step.attackTargetShakeIntensity
                );
            };

        yield return
attacker.PlayAttackRoutine(
  targetActor,
  step.attackResult,
  step.attackApproachDuration,
  step.attackApproachRatio,
  step.attackArcHeight,
  step.attackFailureFallShortDistance,
  step.attackFailureFallBackDuration,
  step.attackFailureFirstBounceDuration,
  step.attackFailureFirstBounceHeight,
  step.attackFailureSecondBounceDuration,
  step.attackFailureSecondBounceHeight,
  step.attackFailureFinalReturnDuration,
  onSuccessImpact,
  onFailureImpact
);
    }

    // <변경부분>
    // Standalone Event Scene의 SpeechBubble Step 실행.
    //
    // Battle처럼 Board 좌표로 Piece를 찾지 않고,
    // SpawnActor에서 등록한 activeActors Dictionary의
    // Actor ID를 기준으로 EventSceneActor를 찾는다.
    //
    // 실제 UI 연출은 EventSceneActor가 소유한
    // ActorSpeechBubbleUI 공용 컴포넌트를 그대로 사용한다.
    private IEnumerator ExecuteSpeechBubbleStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                step.speechBubbleActorId))
        {
            Debug.LogWarning(
                $"Event Scene SpeechBubble 실행 실패: " +
                $"Actor ID가 비어 있습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분>
        // 이전 SpawnActor에서 등록된 Runtime Actor를 찾는다.
        if (activeActors.TryGetValue(
                step.speechBubbleActorId,
                out EventSceneActor actor) == false ||
            actor == null)
        {
            Debug.LogWarning(
                $"Event Scene SpeechBubble 실행 실패: " +
                $"Actor ID '{step.speechBubbleActorId}'를 찾을 수 없습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        string resolvedText =
            step.GetLocalizedSpeechBubbleText();

        if (string.IsNullOrWhiteSpace(
                resolvedText))
        {
            Debug.LogWarning(
                $"Event Scene SpeechBubble 건너뜀: " +
                $"표시할 문자열이 비어 있습니다. / " +
                $"Actor={step.speechBubbleActorId} / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분>
        // 음수 Inspector 값이 Runtime으로 전달되지 않도록 보정한다.
        float safeDuration =
            Mathf.Max(
                0f,
                step.speechBubbleDuration
            );

        float safeTypingSpeed =
            Mathf.Max(
                0f,
                step.speechBubbleTypingSpeed
            );

        float safeEmphasisStrength =
            Mathf.Max(
                0f,
                step.speechBubbleEmphasisStrength
            );

        if (step.speechBubbleWaitForComplete)
        {
            // <변경부분>
            // Fade In → Typewriter → 유지 → Fade Out까지
            // 현재 Event Step에서 기다린다.
            yield return
                actor.PlaySpeechBubbleRoutine(
                    resolvedText,
                    safeDuration,
                    safeTypingSpeed,
                    step.speechBubbleUseEmphasisShake,
                    safeEmphasisStrength
                );

            yield break;
        }

        // <변경부분>
        // 말풍선은 Actor에서 독립적으로 계속 재생하고
        // Sequence는 즉시 다음 Step으로 진행한다.
        //
        // 서로 다른 Actor에게 연속 Detached SpeechBubble을 실행하면
        // 여러 Actor의 말풍선을 동시에 표시할 수도 있다.
        actor.PlaySpeechBubbleDetached(
            resolvedText,
            safeDuration,
            safeTypingSpeed,
            step.speechBubbleUseEmphasisShake,
            safeEmphasisStrength
        );
    }

    // <변경부분>
    // 독립 ScreenShake Step 실행.
    //
    // 실제 화면 흔들림 구현은 AttackActor에서 이미 검증된
    // StartCameraShake()를 그대로 재사용한다.
    //
    // Wait For Complete가 true이면
    // Shake Coroutine이 끝날 때까지 현재 Step에서 기다린다.
    //
    // false이면 Shake를 시작한 직후
    // 다음 Event Step으로 진행한다.
    private IEnumerator ExecuteScreenShakeStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        StartCameraShake(
            step.screenShakeDuration,
            step.screenShakeStrength
        );

        if (step.screenShakeWaitForComplete == false)
        {
            yield break;
        }

        // <변경부분>
        // StartCameraShake()가 실제 Shake Coroutine을
        // 생성한 경우에만 완료될 때까지 기다린다.
        //
        // Duration / Strength가 0이거나
        // Camera Target을 찾지 못한 경우에는
        // Coroutine이 생성되지 않으므로 즉시 다음 Step으로 진행한다.
        while (cameraShakeCoroutine != null)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }
    }


    // <변경부분>
    // Event Scene 공용 Screen Shake를 시작한다.
    //
    // Battle의 공격 Impact Feedback과 같은 원칙으로
    // Camera Transform의 Local Position을 짧게 흔든다.
    //
    // 이미 Shake가 실행 중이라면
    // 기존 Camera를 먼저 정확한 기준 위치로 복구한 뒤
    // 새로운 Shake를 시작한다.
    private void StartCameraShake(
        float duration,
        float strength)
    {
        StopCameraShakeImmediately();

        Transform shakeTarget =
            cameraShakeTarget;

        // <변경부분>
        // Inspector에 별도 Camera가 연결되지 않았다면
        // Main Camera를 자동 사용한다.
        if (shakeTarget == null &&
            Camera.main != null)
        {
            shakeTarget =
                Camera.main.transform;
        }

        if (shakeTarget == null)
        {
            Debug.LogWarning(
                "Event Scene Camera Shake 실패: " +
                "Camera Shake Target 또는 Main Camera를 찾을 수 없습니다."
            );

            return;
        }

        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        float safeStrength =
            Mathf.Max(
                0f,
                strength
            );

        if (safeDuration <= 0f ||
            safeStrength <= 0f)
        {
            return;
        }

        activeCameraShakeTarget =
            shakeTarget;

        // <변경부분>
        // 흔들림 도중 위치를 새 기준점으로 잡는 Drift를 방지하기 위해
        // 시작 직전 위치를 한 번만 저장한다.
        cameraShakeBaseLocalPosition =
            shakeTarget.localPosition;

        cameraShakeCoroutine =
            StartCoroutine(
                PlayCameraShakeRoutine(
                    shakeTarget,
                    cameraShakeBaseLocalPosition,
                    safeDuration,
                    safeStrength
                )
            );
    }


    // <변경부분>
    // 지정 Camera Transform을 기준 위치 주변에서 짧게 흔든다.
    private IEnumerator PlayCameraShakeRoutine(
        Transform shakeTarget,
        Vector3 baseLocalPosition,
        float duration,
        float strength)
    {
        float elapsedTime =
            0f;

        while (elapsedTime <
               duration)
        {
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
                    -strength,
                    strength
                );

            float randomY =
                Random.Range(
                    -strength,
                    strength
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

        // <변경부분>
        // Shake 종료 후 기준 위치로 정확히 복구한다.
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


    // <변경부분>
    // 현재 실행 중인 Screen Shake를 즉시 종료하고
    // Shake 시작 전 Camera 위치로 정확하게 복구한다.
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

    // <변경부분>
    // PlayActorAnimation Step 실행.
    //
    // 이전 SpawnActor에서 생성된 Actor를 Actor ID로 찾고,
    // 해당 Actor가 가지고 있는 Spine SkeletonAnimation에
    // 지정 Animation을 직접 실행한다.
    private IEnumerator ExecutePlayActorAnimationStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                step.playAnimationActorId))
        {
            Debug.LogWarning(
                "Event Scene PlayActorAnimation 실패: " +
                "Actor ID가 비어 있습니다."
            );

            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                step.playAnimationName))
        {
            Debug.LogWarning(
                $"Event Scene PlayActorAnimation 실패: " +
                $"Actor '{step.playAnimationActorId}'의 " +
                $"Animation Name이 비어 있습니다."
            );

            yield break;
        }

        // <변경부분>
        // SpawnActor에서 등록한 Runtime Event Actor를 찾는다.
        if (activeActors.TryGetValue(
                step.playAnimationActorId,
                out EventSceneActor actor) == false ||
            actor == null)
        {
            Debug.LogWarning(
                $"Event Scene PlayActorAnimation 실패: " +
                $"Actor ID '{step.playAnimationActorId}'를 찾을 수 없습니다."
            );

            yield break;
        }

        Debug.Log(
            $"Event Scene Actor Animation 실행: " +
            $"{step.playAnimationActorId} / " +
            $"{step.playAnimationName} / " +
            $"Loop={step.playAnimationLoop}"
        );

        // <변경부분>
        // EventSceneActor가 Spine SkeletonAnimation을 직접 제어한다.
        yield return
    actor.PlayAnimationRoutine(
        step.playAnimationName,
        step.playAnimationLoop,
        step.playAnimationWaitForComplete,
        step.playAnimationReturnToIdle,
        step.playAnimationMixDuration
    );
    }

    // <변경부분>
    // RemoveActor Step 실행.
    //
    // SpawnActor에서 생성하여 activeActors에 등록된 Actor를
    // Actor ID로 찾은 뒤 지정된 방식으로 제거한다.
    //
    // FadeOut:
    // Visual Fade가 완료된 후 제거.
    //
    // Immediate:
    // 즉시 비활성화 후 제거.
    private IEnumerator ExecuteRemoveActorStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(
                step.removeActorId))
        {
            Debug.LogWarning(
                "Event Scene RemoveActor 실패: " +
                "Actor ID가 비어 있습니다."
            );

            yield break;
        }

        // <변경부분>
        // SpawnActor에서 등록한 Runtime Actor를 찾는다.
        if (activeActors.TryGetValue(
                step.removeActorId,
                out EventSceneActor actor) == false ||
            actor == null)
        {
            Debug.LogWarning(
                $"Event Scene RemoveActor 실패: " +
                $"Actor ID '{step.removeActorId}'를 찾을 수 없습니다."
            );

            yield break;
        }

        // <변경부분>
        // FadeOut 방식이면 실제 제거 전에
        // Actor Visual이 완전히 사라질 때까지 기다린다.
        if (step.removeActorMode ==
            EventSceneRemoveMode.FadeOut)
        {
            yield return
                actor.PlayFadeOutRoutine(
                    step.removeActorFadeOutDuration
                );
        }

        // <변경부분>
        // Destroy 전에 Dictionary에서 먼저 제거한다.
        //
        // 이후 동일한 Actor ID로 다시 SpawnActor를 실행해도
        // 중복 Actor ID 검사에 걸리지 않도록 한다.
        activeActors.Remove(
            step.removeActorId
        );

        if (actor != null &&
            actor.gameObject != null)
        {
            // <변경부분>
            // Unity Destroy는 Frame 종료 시 실제 파괴되므로
            // Immediate에서도 화면에 한 Frame 남지 않도록
            // 먼저 비활성화한다.
            actor.gameObject.SetActive(
                false
            );

            Destroy(
                actor.gameObject
            );
        }

        Debug.Log(
            $"Event Scene Actor 제거 완료: " +
            $"{step.removeActorId} / " +
            $"{step.removeActorMode}"
        );
    }

    // <변경부분>
    // Event Scene Dialogue 실행.
    //
    // Localization Resolve는 StepData가 담당하고
    // EventGuideUI는 최종 문자열 목록만 전달받는다.
    private IEnumerator ExecuteDialogueStepRoutine(
        EventSceneStepData step)
    {
        if (eventGuideUI == null)
        {
            Debug.LogWarning(
                "Event Scene Dialogue 실행 실패: " +
                "EventGuideUI가 연결되지 않았습니다."
            );

            yield break;
        }

        List<string> resolvedDialoguePages =
            step.GetLocalizedDialoguePages();

        if (resolvedDialoguePages == null ||
            resolvedDialoguePages.Count == 0)
        {
            Debug.LogWarning(
                "Event Scene Dialogue 건너뜀: " +
                "Dialogue Page가 없습니다."
            );

            yield break;
        }

        yield return
            eventGuideUI.PlayDialogueRoutine(
                resolvedDialoguePages
            );
    }


    // <변경부분>
    // Wait Step 실행.
    private IEnumerator ExecuteWaitStepRoutine(
        EventSceneStepData step)
    {
        float safeDuration =
            Mathf.Max(
                0f,
                step.waitDuration
            );

        if (safeDuration <= 0f)
        {
            yield break;
        }

        float elapsedTime =
            0f;

        while (elapsedTime <
               safeDuration)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }
    }


    // <변경부분>
    // 현재 Event Scene Sequence 정상 완료.
    public void CompleteSequence()
    {
        if (isSequenceActive == false)
        {
            return;
        }

        isSequenceActive =
            false;

        currentStepIndex =
            -1;

        if (eventGuideUI != null)
        {
            eventGuideUI.HideImmediately();
        }

        Debug.Log(
    $"Event Scene Sequence 완료: " +
    $"{currentEventSceneData?.eventName}"
);

        HandleSequenceCompletion();
    }


    // <변경부분>
    // EventSceneData가 직접 소유한 Completion 설정을 처리한다.
    private void HandleSequenceCompletion()
    {
        if (currentEventSceneData == null)
        {
            return;
        }

        switch (currentEventSceneData.completionType)
        {
            case EventSceneCompletionType.None:
                return;

            case EventSceneCompletionType.WorldMap:

                SceneManager.LoadScene(
                    "WorldMapScene"
                );

                return;

            case EventSceneCompletionType.LoadScene:

                if (string.IsNullOrWhiteSpace(
                        currentEventSceneData
                            .completionSceneName))
                {
                    Debug.LogWarning(
                        "Event Scene 완료 Scene 이동 실패: " +
                        "Completion Scene Name이 비어 있습니다."
                    );

                    return;
                }

                // <변경부분>
                // 다음 Scene이 TextCutsceneScene이고
                // Cutscene Data가 지정된 경우
                // 기존 Runtime 전달 구조를 그대로 재사용한다.
                if (currentEventSceneData
                        .completionCutsceneData != null)
                {
                    TextCutsceneRuntimeState
                        .SetPendingCutsceneData(
                            currentEventSceneData
                                .completionCutsceneData
                        );
                }

                SceneManager.LoadScene(
                    currentEventSceneData
                        .completionSceneName
                );

                return;
        }
    }


    // Event Scene Sequence를 외부에서 즉시 정지한다.
    public void StopSequence()
    {
        isSequenceActive =
            false;

        currentStepIndex =
            -1;

        StopSequenceCoroutine();

        if (eventGuideUI != null)
        {
            eventGuideUI.HideImmediately();
        }

        // <변경부분>
        // 강제 종료 시 현재 Event Data Runtime 참조도 정리한다.
        currentEventSceneData =
            null;

        Debug.Log(
            "Event Scene Sequence 강제 종료"
        );
    }


    private void StopSequenceCoroutine()
    {
        if (sequenceCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            sequenceCoroutine
        );

        sequenceCoroutine =
            null;
    }


    private void OnDisable()
    {
        StopSequenceCoroutine();

        // <변경부분>
        // Camera Shake 도중 Scene 전환 또는 비활성화가 발생해도
        // Camera가 흔들린 좌표에 남지 않도록 즉시 원위치 복구.
        StopCameraShakeImmediately();

        isSequenceActive =
            false;

        currentStepIndex =
            -1;

        currentEventSceneData =
            null;
    }
}
