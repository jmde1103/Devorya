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


            case EventSceneStepType.MoveActor:
            case EventSceneStepType.AttackActor:
            case EventSceneStepType.AbsorbActor:
            case EventSceneStepType.PlayActorAnimation:
            case EventSceneStepType.SpeechBubble:
            case EventSceneStepType.RemoveActor:
            case EventSceneStepType.CameraShot:
            case EventSceneStepType.ScreenShake:

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

        isSequenceActive =
            false;

        currentStepIndex =
            -1;

        // <변경부분>
        // Scene 종료 / 비활성화 시 Runtime Data 참조 제거.
        currentEventSceneData =
            null;
    }
}
