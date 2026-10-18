using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal; // <변경부분> URP 2D Global Light
using UnityEngine.UI; // <변경부분> 화면 전체 색상 플래시용 Image

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
    // 이벤트 종류에 따라 상점 나가기 버튼의
    // 활성화 / 비활성화를 요청할 컨트롤러.
    [SerializeField]
    private ShopController shopController;

    // <변경부분>
    // Event Scene 전용 선택형 보상창.
    // 기존 BattleRewardPopupUI와는 별개의 UI를 사용한다.
    [SerializeField]
    private EventSceneRewardPopupUI eventSceneRewardPopupUI;

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
    // Event Scene 전용 URP 2D Screen Glitch Controller.
    // Inspector에서 Scene에 배치된 컴포넌트를 연결한다.
    [Header("Screen Glitch")]
    [SerializeField]
    private EventScreenGlitchController eventScreenGlitchController;


    // <변경부분>
    // Standalone Event Scene CameraShot에서 사용할
    // 실제 PixelCameraController.
    //
    // Inspector 연결을 우선 사용하고,
    // 비어 있으면 Main Camera에서 자동 탐색한다.
    [SerializeField]
    private PixelCameraController pixelCameraController;

    // <변경부분>
    // 현재 Event Scene의 EnvironmentLightingRig에 있는 Global Ambient.
    // Inspector에서 해당 Light2D 컴포넌트를 연결한다.
    [Header("Global Lighting")]
    [SerializeField]
    private Light2D environmentGlobalAmbient;

    // <변경부분>
    // 페이드 중첩 방지 및 최초 조명값 복구에 사용한다.
    private Coroutine globalLightingFadeCoroutine;
    private bool hasCapturedGlobalLightingBase;
    private Color globalLightingBaseColor;
    private float globalLightingBaseIntensity;

    // <변경부분>
    // GlobalLighting Step의 화면 전체 색상 플래시.
    // Canvas는 연출할 때만 현재 Scene 안에 생성한다.
    private GameObject globalLightingFlashCanvasObject;
    private Image globalLightingFlashImage;
    private Coroutine globalLightingFlashCoroutine;


    [Header("Run HUD")]

    // <변경부분>
    // 월드맵에서 가져온
    // 턴 시계 / Gold / Item Bar 전체를 묶은 부모 CanvasGroup.
    //
    // EventScene에서는 오브젝트를 비활성화하지 않고
    // Alpha / Raycast만 제어하여 Fade한다.
    [SerializeField]
    private CanvasGroup runHUDCanvasGroup;

    // <변경부분>
    // Run HUD Fade In / Out 시간.
    [SerializeField, Min(0f)]
    private float runHUDFadeDuration =
        0.25f;

    // <변경부분>
    // 현재 실행 중인 Run HUD Fade Coroutine.
    private Coroutine runHUDFadeCoroutine;


    // 현재 실행 중인 Event Scene Camera Shake Coroutine.
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

    // <변경부분>
    // 선택지에서 지정한 목적지의 Step Index.
    // -1이면 기존 순서대로 다음 스텝을 실행한다.
    private int pendingBranchStepIndex =
        -1;

    private Coroutine sequenceCoroutine =
        null;

    // <변경부분>
    // 현재 Event Scene에서 일반 플레이어 조작이 잠겨 있는지 관리한다.
    //
    // true:
    // 연출 진행 중 상태.
    // Camera 수동 이동 / 확대 / 축소와
    // ShopItem 같은 Event Scene 상호작용을 차단한다.
    //
    // false:
    // 상점 / 휴식 / 조사 등 자유 상호작용을 허용한다.
    //
    // 설정 / 종료 같은 System UI는
    // 이 값과 별도로 항상 접근 가능하도록 구성한다.
    private bool isPlayerInteractionLocked =
        true;

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
    // ShopItemDisplay 등 Event Scene 상호작용 오브젝트가
    // 현재 입력 가능 여부를 확인할 때 사용하는 공용 상태.
    public bool IsPlayerInteractionLocked =>
      isPlayerInteractionLocked;


    // <변경부분>
    // EventScene이 활성화되는 최초 순간에는
    // Run HUD를 무조건 숨긴 상태로 시작한다.
    //
    // 이후 UnlockPlayerInteraction Step에서
    // 현재 EventSceneData 설정에 따라 Fade In한다.
    private void Awake()
    {
        SetRunHUDVisibleImmediately(
            false
        );

        // <변경부분>
        // 이벤트씬 진입 시 모든 이벤트는
        // 기본적으로 상점 나가기 버튼을 숨긴다.
        SetShopExitButtonVisible(false);
    }


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
        // 이전 이벤트의 표시 상태가 남아 있더라도
        // 새 이벤트 시작 시 버튼은 항상 숨김으로 초기화한다.
        SetShopExitButtonVisible(false);

        // <변경부분>
        // Standalone Event Scene이 시작되면
        // 일반 플레이어 조작을 기본적으로 잠근다.
        //
        // 현재는 수동 Camera 입력에 즉시 반영되고,
        // 이후 ShopItemDisplay 등의 Event Scene 상호작용도
        // 동일한 IsPlayerInteractionLocked 상태를 사용한다.
        //
        // 설정 / 종료 같은 System UI는
        // 이 Lock과 별도로 항상 사용할 수 있게 구성한다.
        SetEventScenePlayerInteractionLocked(
            true
        );

        // <변경부분>
        // <변경부분>
        // 실행 중에만 현재 EventSceneData를 보관한다.
        currentEventSceneData =
            newEventSceneData;

        // <변경부분>
        // 이전 시퀀스의 분기 요청이 남지 않도록 초기화한다.
        pendingBranchStepIndex =
            -1;

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

            // <변경부분>
            // 선택지에서 요청한 Step으로 다음 반복 위치를 옮긴다.
            // for문의 i++까지 고려해 목적지 인덱스 - 1로 설정한다.
            if (pendingBranchStepIndex >= 0)
            {
                int branchIndex =
                    pendingBranchStepIndex;

                pendingBranchStepIndex =
                    -1;

                if (branchIndex >= steps.Count)
                {
                    Debug.LogError(
                        "Event Scene Dialogue 분기 실패: " +
                        "목적지 Step Index가 범위를 벗어났습니다.",
                        this
                    );

                    isSequenceActive =
                        false;

                    sequenceCoroutine =
                        null;

                    yield break;
                }

                i = branchIndex - 1;
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
            // <변경부분>
            // Event Scene 전용 Camera 위치 / Zoom 연출.
            case EventSceneStepType.CameraShot:

                yield return
                    ExecuteCameraShotStepRoutine(
                        step
                    );

                yield break;


            // 독립 Screen Shake 연출.
            case EventSceneStepType.ScreenShake:

                yield return
                    ExecuteScreenShakeStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // Event Scene 전용 화면 글리치 연출.
            case EventSceneStepType.ScreenGlitch:

                yield return
                    ExecuteScreenGlitchStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // Global Ambient 조명 페이드 연출.
            case EventSceneStepType.GlobalLighting:

                yield return
                    ExecuteGlobalLightingStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // 지정 Event Actor 위에 SpeechBubble을 표시한다.
            // Git 작업 과정에서 누락된 기존 실행 분기를 복구한다.
            case EventSceneStepType.SpeechBubble:

                yield return
                    ExecuteSpeechBubbleStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // Event 연출 Lock을 해제하고
            // 플레이어의 일반 Scene 조작을 허용한다.
            //
            // 이 상태는 다음 LockPlayerInteraction Step이 실행되거나
            // Event Scene이 종료될 때까지 유지된다.
            case EventSceneStepType.UnlockPlayerInteraction:

                // <변경부분>
                // 일반 상호작용은 Unlock하되
                // Camera 수동 입력은 Step 옵션에 따라 잠금 유지 가능.
                SetEventScenePlayerInteractionLocked(
                    false,
                    step.unlockPlayerInteractionLockManualCamera
                );

                // <변경부분>
                // 현재 EventSceneData가 Run HUD 표시를 허용한 경우에만
                // 플레이어 조작 Unlock과 함께 HUD를 Fade In한다.
                SetRunHUDVisibleWithFade(
     currentEventSceneData != null &&
     currentEventSceneData
         .showRunHUDDuringPlayerInteraction
 );

                // <변경부분>
                // EventSceneData의 옵션이 ON인 경우에만
                // 상점 나가기 버튼을 표시한다.
                SetShopExitButtonVisible(
                    currentEventSceneData != null &&
                    currentEventSceneData
                        .showShopExitButtonDuringPlayerInteraction
                );

                Debug.Log(
                    "Event Scene Player Interaction Unlock"
                );

                yield break;


            // <변경부분>
            // 플레이어의 일반 Scene 조작을 다시 잠근다.
            //
            // 자유 상호작용 이후 Dialogue / CameraShot 등의
            // Event 연출로 다시 진입할 때 사용한다.
            case EventSceneStepType.LockPlayerInteraction:

                SetEventScenePlayerInteractionLocked(
                    true
                );

                // <변경부분>
                // <변경부분>
                // 다시 Event 연출 상태로 들어가면
                // Run HUD도 함께 Fade Out한다.
                SetRunHUDVisibleWithFade(
                    false
                );

                // <변경부분>
                // 이벤트 연출 중에는
                // 상점 나가기 버튼을 숨긴다.
                SetShopExitButtonVisible(false);

                Debug.Log(
      "Event Scene Player Interaction Lock"
  );

                yield break;


            // <변경부분>
            // 이벤트씬에서 전리품 선택창을 표시하고
            // 수령 완료 또는 Confirm까지 대기한다.
            case EventSceneStepType.ShowRewardSelection:

                yield return
                    ExecuteShowRewardSelectionStepRoutine(
                        step
                    );

                yield break;


            // <변경부분>
            // StageBattleData를 다음 BattleScene으로 전달하고
            // 현재 Event Scene Sequence를 종료한다.
            case EventSceneStepType.StartBattle:

                ExecuteStartBattleStep(
                    step
                );

                yield break;


            case EventSceneStepType.AbsorbActor:

                Debug.LogWarning(
                    $"Event Scene Step 미구현: " +
                    $"{step.stepType}"
                );

                yield break;
        }
    }


    // <변경부분>
    // Event Scene의 ScreenGlitch Step을 실행한다.
    //
    // 실제 화면 효과는 EventScreenGlitchController가 담당한다.
    // Wait OFF에서는 효과 실행 중에도 다음 Step으로 진행한다.
    private IEnumerator ExecuteScreenGlitchStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        // <변경부분>
        // 필수 Controller가 없거나 비활성화되었으면
        // 연출을 실행하지 않은 채 다음 Step으로 넘어가지 않는다.
        if (eventScreenGlitchController == null ||
            !eventScreenGlitchController.isActiveAndEnabled)
        {
            Debug.LogError(
                "Event Scene ScreenGlitch 실행 실패: " +
                "EventScreenGlitchController가 연결되지 않았거나 " +
                "비활성화되어 있습니다.",
                this
            );

            StopSequence();
            yield break;
        }

        // <변경부분>
        // 기존 컨트롤러의 PlayGlitch를 재사용한다.
        // 컨트롤러가 자체적으로 기존 글리치를 정리하고
        // 새 효과를 시작한다.
        eventScreenGlitchController.PlayGlitch(
            Mathf.Clamp01(step.screenGlitchStrength),
            Mathf.Max(0.05f, step.screenGlitchDuration)
        );

        // <변경부분>
        // Material 또는 RawImage 설정 누락 등으로
        // 글리치가 시작되지 않았다면 Sequence를 중단한다.
        if (!eventScreenGlitchController.IsPlaying)
        {
            Debug.LogError(
                "Event Scene ScreenGlitch 실행 실패: " +
                "글리치 효과가 시작되지 않았습니다. " +
                "Screen Overlay와 Glitch Material 연결을 확인하세요.",
                this
            );

            StopSequence();
            yield break;
        }

        // <변경부분>
        // OFF:
        // 글리치 효과는 별도의 Controller에서 계속 진행하고
        // Event Sequence는 즉시 다음 Step으로 진행한다.
        if (!step.screenGlitchWaitForComplete)
        {
            yield break;
        }

        // <변경부분>
        // ON:
        // 글리치 효과가 끝날 때까지 대기한다.
        // Time Scale에 관계없이 효과 자체의 완료 상태를 확인한다.
        while (eventScreenGlitchController != null &&
               eventScreenGlitchController.IsPlaying)
        {
            yield return null;
        }

        // <변경부분>
        // 대기 도중 Controller가 사라지거나 비활성화되었다면
        // 정상 완료로 간주하지 않는다.
        if (eventScreenGlitchController == null ||
            !eventScreenGlitchController.isActiveAndEnabled)
        {
            Debug.LogError(
                "Event Scene ScreenGlitch 대기 중단: " +
                "Controller가 사라지거나 비활성화되었습니다.",
                this
            );

            StopSequence();
        }
    }


    // <변경부분>
    // Global Light와 Screen Flash를 각각 독립적으로 실행한다.
    // 두 효과 모두 활성화되어 있으면 동시에 시작한다.
    private IEnumerator ExecuteGlobalLightingStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        Coroutine flashToWait = null;
        Coroutine lightingToWait = null;

        // =====================================================
        // Screen Flash
        // =====================================================

        // <변경부분>
        // Global Light 사용 여부와 관계없이 실행한다.
        // Light2D 참조가 없어도 Screen Flash는 정상 동작한다.
        if (step.globalLightingUseScreenFlash)
        {
            StartGlobalLightingFlash(
                step.globalLightingFlashColor,
                Mathf.Clamp01(
                    step.globalLightingFlashStrength
                ),
                Mathf.Max(
                    0f,
                    step.globalLightingFlashDuration
                )
            );

            flashToWait =
                globalLightingFlashCoroutine;
        }

        // =====================================================
        // Global Light
        // =====================================================

        // <변경부분>
        // 체크박스가 OFF이면 기존 조명값과
        // 진행 중인 조명 페이드를 건드리지 않는다.
        if (step.globalLightingUseGlobalLight)
        {
            if (environmentGlobalAmbient == null)
            {
                Debug.LogWarning(
                    "Event Scene GlobalLighting 실패: " +
                    "Global Ambient Light2D가 연결되지 않았습니다.",
                    this
                );
            }
            else
            {
                // <변경부분>
                // 첫 조명 연출 직전에만 원래 값을 기록한다.
                if (!hasCapturedGlobalLightingBase)
                {
                    globalLightingBaseColor =
                        environmentGlobalAmbient.color;

                    globalLightingBaseIntensity =
                        environmentGlobalAmbient.intensity;

                    hasCapturedGlobalLightingBase =
                        true;
                }

                // <변경부분>
                // 이전 조명 페이드가 진행 중이라면 중단한다.
                // 현재 색상과 밝기에서 새 페이드를 시작한다.
                if (globalLightingFadeCoroutine != null)
                {
                    StopCoroutine(
                        globalLightingFadeCoroutine
                    );

                    globalLightingFadeCoroutine =
                        null;
                }

                float duration =
                    Mathf.Max(
                        0f,
                        step.globalLightingFadeDuration
                    );

                Color targetColor =
                    step.globalLightingTargetColor;

                float targetIntensity =
                    Mathf.Max(
                        0f,
                        step.globalLightingTargetIntensity
                    );

                // <변경부분>
                // 시간이 0이면 조명을 즉시 변경한다.
                if (duration <= 0f)
                {
                    environmentGlobalAmbient.color =
                        targetColor;

                    environmentGlobalAmbient.intensity =
                        targetIntensity;
                }
                else
                {
                    // <변경부분>
                    // 기존 조명 페이드 코루틴을 그대로 재사용한다.
                    globalLightingFadeCoroutine =
                        StartCoroutine(
                            FadeGlobalLightingRoutine(
                                targetColor,
                                targetIntensity,
                                duration
                            )
                        );

                    lightingToWait =
                        globalLightingFadeCoroutine;
                }
            }
        }

        // =====================================================
        // Wait For Complete
        // =====================================================

        // <변경부분>
        // OFF이면 두 효과를 시작하고 다음 스텝으로 넘어간다.
        if (!step.globalLightingWaitForComplete)
        {
            yield break;
        }

        // <변경부분>
        // ON이면 실행된 효과들이 끝날 때까지 기다린다.
        if (lightingToWait != null)
        {
            yield return lightingToWait;
        }

        if (flashToWait != null)
        {
            yield return flashToWait;
        }
    }


    // <변경부분>
    // 조명값을 Ease In/Out 보간으로 자연스럽게 변경한다.
    private IEnumerator FadeGlobalLightingRoutine(
        Color targetColor,
        float targetIntensity,
        float duration)
    {
        Color startColor =
            environmentGlobalAmbient.color;

        float startIntensity =
            environmentGlobalAmbient.intensity;

        float elapsed =
            0f;

        while (elapsed < duration &&
               environmentGlobalAmbient != null)
        {
            // <변경부분>
            // Time Scale에 관계없이 연출 시간을 계산한다.
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float easedT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            environmentGlobalAmbient.color =
                Color.Lerp(
                    startColor,
                    targetColor,
                    easedT
                );

            environmentGlobalAmbient.intensity =
                Mathf.Lerp(
                    startIntensity,
                    targetIntensity,
                    easedT
                );

            yield return null;
        }

        // <변경부분>
        // 페이드 종료 시 목표값을 정확하게 적용한다.
        if (environmentGlobalAmbient != null)
        {
            environmentGlobalAmbient.color =
                targetColor;

            environmentGlobalAmbient.intensity =
                targetIntensity;
        }

        globalLightingFadeCoroutine =
            null;
    }


    // <변경부분>
    // GlobalLighting의 화면 전체 색상 플래시를 실행한다.
    // Scene에만 존재하는 Overlay Canvas를 사용하므로
    // EnvironmentLightingRig나 Profile Asset 값에는 영향이 없다.
    private void StartGlobalLightingFlash(
        Color flashColor,
        float strength,
        float duration)
    {
        StopGlobalLightingFlashImmediately();

        if (!isActiveAndEnabled ||
            !gameObject.activeInHierarchy ||
            strength <= 0f)
        {
            return;
        }

        // <변경부분>
        // 모든 UI 위에 출력하되, 기존 버튼 클릭은 막지 않는다.
        globalLightingFlashCanvasObject =
            new GameObject(
                "EventScene_GlobalLightingFlash",
                typeof(RectTransform),
                typeof(Canvas)
            );

        Canvas flashCanvas =
            globalLightingFlashCanvasObject.GetComponent<Canvas>();

        flashCanvas.renderMode =
            RenderMode.ScreenSpaceOverlay;
        flashCanvas.overrideSorting = true;
        flashCanvas.sortingOrder = 32000;

        GameObject flashImageObject =
            new GameObject(
                "FlashImage",
                typeof(RectTransform),
                typeof(Image)
            );

        RectTransform flashRect =
            flashImageObject.GetComponent<RectTransform>();

        flashRect.SetParent(
            globalLightingFlashCanvasObject.transform,
            false
        );

        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        globalLightingFlashImage =
            flashImageObject.GetComponent<Image>();

        globalLightingFlashImage.raycastTarget = false;

        flashColor.a = 0f;
        globalLightingFlashImage.color = flashColor;

        globalLightingFlashCoroutine = StartCoroutine(
            PlayGlobalLightingFlashRoutine(
                flashColor,
                strength,
                duration
            )
        );
    }


    // <변경부분>
    // 설정한 색상이 빠르게 나타났다가 서서히 사라진다.
    // 전체 Duration의 앞 20%는 Fade In, 뒤 80%는 Fade Out이다.
    private IEnumerator PlayGlobalLightingFlashRoutine(
        Color flashColor,
        float strength,
        float duration)
    {
        if (duration <= 0.05f)
        {
            // <변경부분>
            // 매우 짧은 Flash도 최소 한 프레임은 보이게 한다.
            flashColor.a = strength;
            globalLightingFlashImage.color = flashColor;

            yield return null;
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < duration &&
                   globalLightingFlashImage != null)
            {
                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                float alpha =
                    t < 0.2f
                        ? Mathf.SmoothStep(
                            0f,
                            strength,
                            t / 0.2f
                        )
                        : Mathf.SmoothStep(
                            strength,
                            0f,
                            (t - 0.2f) / 0.8f
                        );

                flashColor.a = alpha;
                globalLightingFlashImage.color = flashColor;

                elapsed += Time.unscaledDeltaTime;

                yield return null;
            }
        }

        // <변경부분>
        // Flash 종료 시 투명하게 만든 뒤 Overlay 표시를 끈다.
        if (globalLightingFlashImage != null)
        {
            flashColor.a = 0f;
            globalLightingFlashImage.color = flashColor;
        }

        if (globalLightingFlashCanvasObject != null)
        {
            globalLightingFlashCanvasObject.SetActive(false);
        }

        globalLightingFlashCoroutine = null;
    }


    // <변경부분>
    // 새로운 Flash 또는 Scene 전환 시 이전 Overlay를 즉시 제거한다.
    private void StopGlobalLightingFlashImmediately()
    {
        if (globalLightingFlashCoroutine != null)
        {
            StopCoroutine(globalLightingFlashCoroutine);
            globalLightingFlashCoroutine = null;
        }

        if (globalLightingFlashCanvasObject != null)
        {
            globalLightingFlashCanvasObject.SetActive(false);

            Destroy(globalLightingFlashCanvasObject);
        }

        globalLightingFlashCanvasObject = null;
        globalLightingFlashImage = null;
    }


    // <변경부분>
    // Scene 종료 또는 Controller 비활성화 시
    // 조명 연출을 중지하고 원래 값을 복구한다.
    private void RestoreGlobalLightingImmediately()
    {
        // <변경부분>
        // Scene 전환 시 화면 Flash가 남지 않게 정리한다.
        StopGlobalLightingFlashImmediately();

        if (globalLightingFadeCoroutine != null)
        {
            StopCoroutine(
                globalLightingFadeCoroutine
            );

            globalLightingFadeCoroutine =
                null;
        }

        if (hasCapturedGlobalLightingBase &&
            environmentGlobalAmbient != null)
        {
            environmentGlobalAmbient.color =
                globalLightingBaseColor;

            environmentGlobalAmbient.intensity =
                globalLightingBaseIntensity;
        }

        hasCapturedGlobalLightingBase =
            false;
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
visualObject,
backgroundManager
);

        // <변경부분>
        // SpawnActor에 지정된 실제 Sorting Layer를 적용한다.
        //
        // 빈 문자열은 Auto이므로 기존 BackgroundManager 정렬을
        // 그대로 사용한다.
        //
        // "Piece"가 지정되었다면 Born Animation이 시작되기 전에
        // 생성된 Actor Visual을 Piece Layer로 변경한다.
        actor.SetSortingLayerOverride(
            step.spawnActorSortingLayerName
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

        // <변경부분>
        // Change Sorting Layer가 켜져 있을 때만 변경한다.
        //
        // OFF:
        // SpawnActor 또는 이전 MoveActor에서 지정한 Layer 유지.
        //
        // ON + Auto:
        // 기존 BackgroundManager 자동 정렬로 복귀.
        //
        // ON + Piece:
        // 이동을 시작하기 전 Piece Sorting Layer로 변경.
        //
        // 선택된 Layer는 MoveArcRoutine과 공격 이동 중에도 유지된다.
        if (step.moveActorChangeSortingLayer)
        {
            actor.SetSortingLayerOverride(
                step.moveActorSortingLayerName
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

    // =====================================================
    // Camera Shot
    // =====================================================

    // <변경부분>
    // Standalone Event Scene CameraShot Step 실행.
    //
    // BackgroundTile:
    // BackgroundManager의 기존 좌표 조회를 그대로 재사용한다.
    //
    // Actor:
    // activeActors Dictionary에 현재 존재하는 EventSceneActor를 사용한다.
    //
    // 실제 Camera 이동 / WorldRoot Zoom은
    // PixelCameraController 한 곳에서만 담당한다.
    private IEnumerator ExecuteCameraShotStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        PixelCameraController cameraController =
            GetEventPixelCameraController();

        if (cameraController == null)
        {
            Debug.LogWarning(
                $"Event Scene CameraShot 실행 실패: " +
                $"PixelCameraController를 찾을 수 없습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        Transform targetTransform =
            null;

        switch (step.cameraShotTargetType)
        {
            case EventSceneCameraShotTargetType.BackgroundTile:

                if (backgroundManager == null)
                {
                    Debug.LogWarning(
                        $"Event Scene CameraShot 실행 실패: " +
                        $"BackgroundManager가 연결되지 않았습니다. / " +
                        $"{step.stepName}"
                    );

                    yield break;
                }

                BackgroundTile targetTile =
                    backgroundManager
                        .GetBackgroundTileAt(
                            step.cameraShotTargetPosition.x,
                            step.cameraShotTargetPosition.y
                        );

                if (targetTile == null)
                {
                    Debug.LogWarning(
                        $"Event Scene CameraShot 실행 실패: " +
                        $"BackgroundTile " +
                        $"({step.cameraShotTargetPosition.x}, " +
                        $"{step.cameraShotTargetPosition.y})를 찾을 수 없습니다. / " +
                        $"{step.stepName}"
                    );

                    yield break;
                }

                targetTransform =
                    targetTile.transform;

                break;


            case EventSceneCameraShotTargetType.Actor:

                if (string.IsNullOrWhiteSpace(
                        step.cameraShotTargetActorId))
                {
                    Debug.LogWarning(
                        $"Event Scene CameraShot 실행 실패: " +
                        $"Actor ID가 비어 있습니다. / " +
                        $"{step.stepName}"
                    );

                    yield break;
                }

                if (activeActors.TryGetValue(
                        step.cameraShotTargetActorId,
                        out EventSceneActor targetActor) == false ||
                    targetActor == null)
                {
                    Debug.LogWarning(
                        $"Event Scene CameraShot 실행 실패: " +
                        $"Actor ID '{step.cameraShotTargetActorId}'를 " +
                        $"찾을 수 없습니다. / " +
                        $"{step.stepName}"
                    );

                    yield break;
                }

                targetTransform =
                    targetActor.transform;

                break;


            default:

                Debug.LogWarning(
                    $"Event Scene CameraShot 실행 실패: " +
                    $"지원하지 않는 Target Type입니다. " +
                    $"{step.cameraShotTargetType}"
                );

                yield break;
        }

        // <변경부분>
        // 기존 ScreenShake가 남아 있는 상태에서 CameraShot이 시작되면
        // 두 Coroutine이 Camera Transform을 동시에 수정할 수 있으므로
        // 먼저 Shake 기준 위치로 정확하게 복구한다.
        StopCameraShakeImmediately();

        // <변경부분>
        // Event CameraShot의 기본 규칙:
        //
        // BackgroundTile Target
        // → 선택한 Tile 중심을 화면 정중앙에 표시.
        //
        // Actor Target
        // → 선택한 Actor 위치를 화면 정중앙에 표시.
        //
        // 과거 cameraShotWorldOffset 값은
        // Serialized Asset 보호를 위해 Data에는 유지하지만
        // Event CameraShot Runtime에서는 더 이상 사용하지 않는다.
        cameraController.StartEventCameraShot(
            targetTransform,
            Vector2.zero,
            step.cameraShotWorldScale,
            step.cameraShotDuration
        );

        if (step.cameraShotWaitForComplete == false)
        {
            yield break;
        }

        // <변경부분>
        // PixelCameraController가 Shot을 완료할 때까지
        // 현재 Event Step을 유지한다.
        while (cameraController.IsEventCameraShotPlaying)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }
    }


    // <변경부분>
    // Inspector 연결을 우선 사용하고,
    // 연결이 없을 경우 Main Camera에서 PixelCameraController를 찾는다.
    private PixelCameraController GetEventPixelCameraController()
    {
        if (pixelCameraController != null)
        {
            return pixelCameraController;
        }

        if (Camera.main == null)
        {
            return null;
        }

        pixelCameraController =
            Camera.main
                .GetComponent<
                    PixelCameraController
                >();

        return pixelCameraController;
    }


    // <변경부분>
    // EventSceneData의 설정에 따라
    // 상점 나가기 버튼의 표시 여부를 제어한다.
    //
    // 실제 Button과 OnClick은 ShopController가 관리한다.
    private void SetShopExitButtonVisible(
        bool isVisible)
    {
        // Inspector 연결을 우선 사용한다.
        // 연결이 없으면 비활성 오브젝트도 포함하여 찾는다.
        if (shopController == null)
        {
            shopController =
                FindFirstObjectByType<ShopController>(
                    FindObjectsInactive.Include
                );
        }

        if (shopController == null)
        {
            return;
        }

        shopController.SetExitShopButtonVisible(
            isVisible
        );
    }


    // <변경부분>
    // Event Scene의 일반 상호작용과 수동 카메라 입력을 제어한다.
    //
    // isLocked = true:
    // 일반 상호작용 및 수동 카메라 입력 모두 잠금.
    //
    // isLocked = false,
    // lockManualCameraWhenUnlocked = false:
    // 일반 상호작용과 수동 카메라 입력 모두 허용.
    //
    // isLocked = false,
    // lockManualCameraWhenUnlocked = true:
    // 일반 상호작용 허용, 수동 카메라 입력만 잠금.
    //
    // 두 번째 인자의 기본값은 false로 유지하여
    // 기존 호출부의 동작을 변경하지 않는다.
    private void SetEventScenePlayerInteractionLocked(
        bool isLocked,
        bool lockManualCameraWhenUnlocked = false)
    {
        // <변경부분>
        // 버튼 등의 일반 상호작용 허용 여부는
        // 기존 상태값을 그대로 사용한다.
        isPlayerInteractionLocked =
            isLocked;

        // <변경부분>
        // 일반 상호작용이 잠겨 있으면 카메라도 잠근다.
        //
        // 일반 상호작용이 허용된 상태에서는
        // 해당 Unlock Step의 카메라 잠금 옵션을 따른다.
        bool shouldLockManualCamera =
            isLocked ||
            lockManualCameraWhenUnlocked;

        SetEventSceneManualCameraInputLocked(
            shouldLockManualCamera
        );
    }


    // <변경부분>
    // Run HUD를 Fade In / Out한다.
    //
    // Fade Out 시작 시에는 즉시 Raycast를 차단하고,
    // Fade In 완료 후에만 Raycast를 허용하여
    // 보이지 않는 UI가 입력을 가로채지 않도록 한다.
    private void SetRunHUDVisibleWithFade(
        bool isVisible)
    {
        if (runHUDCanvasGroup == null)
        {
            return;
        }

        StopRunHUDFade();

        float targetAlpha =
            isVisible
                ? 1f
                : 0f;

        if (runHUDFadeDuration <= 0f)
        {
            SetRunHUDVisibleImmediately(
                isVisible
            );

            return;
        }

        // Fade Out은 시작 순간부터 입력을 차단한다.
        //
        // Fade In은 완전히 표시된 뒤 입력을 허용한다.
        runHUDCanvasGroup.interactable =
            false;

        runHUDCanvasGroup.blocksRaycasts =
            false;

        runHUDFadeCoroutine =
            StartCoroutine(
                RunHUDFadeRoutine(
                    targetAlpha,
                    isVisible
                )
            );
    }


    // <변경부분>
    // 현재 Alpha에서 목표 Alpha까지 부드럽게 보간한다.
    //
    // EventScene Pause 기능이 이후 추가되더라도
    // UI Fade가 Time Scale에 묶이지 않도록
    // unscaledDeltaTime을 사용한다.
    private IEnumerator RunHUDFadeRoutine(
        float targetAlpha,
        bool enableInteractionAfterFade)
    {
        float startAlpha =
            runHUDCanvasGroup.alpha;

        float elapsedTime =
            0f;

        while (elapsedTime <
               runHUDFadeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    runHUDFadeDuration
                );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            runHUDCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    smoothTime
                );

            yield return null;
        }

        runHUDCanvasGroup.alpha =
            targetAlpha;

        runHUDCanvasGroup.interactable =
            enableInteractionAfterFade;

        runHUDCanvasGroup.blocksRaycasts =
            enableInteractionAfterFade;

        runHUDFadeCoroutine =
            null;
    }


    // <변경부분>
    // Fade 없이 Run HUD 상태를 즉시 적용한다.
    //
    // EventScene 최초 진입 또는 Scene 종료 정리에 사용한다.
    private void SetRunHUDVisibleImmediately(
        bool isVisible)
    {
        if (runHUDCanvasGroup == null)
        {
            return;
        }

        StopRunHUDFade();

        runHUDCanvasGroup.alpha =
            isVisible
                ? 1f
                : 0f;

        runHUDCanvasGroup.interactable =
            isVisible;

        runHUDCanvasGroup.blocksRaycasts =
            isVisible;
    }


    // <변경부분>
    // 이전 Run HUD Fade가 실행 중이면 안전하게 중단한다.
    private void StopRunHUDFade()
    {
        if (runHUDFadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            runHUDFadeCoroutine
        );

        runHUDFadeCoroutine =
            null;
    }


    // <변경부분>
    // Event Scene 전체에서 플레이어의 수동 Camera 입력을


    // <변경부분>
    // Event Scene 전체에서 플레이어의 수동 Camera 입력을
    // 잠그거나 해제한다.
    //
    // CameraShot / ScreenShake 같은 연출 Camera 제어는
    // 별도 Runtime API이므로 그대로 사용할 수 있다.
    private void SetEventSceneManualCameraInputLocked(
        bool isLocked)
    {
        PixelCameraController cameraController =
            GetEventPixelCameraController();

        if (cameraController == null)
        {
            return;
        }

        cameraController
            .SetManualCameraInputLocked(
                isLocked
            );
    }

    // <변경부분>
    // Sequence 종료/비활성화 등에서 Detached CameraShot이 남지 않도록
    // 현재 Shot만 중지한다.
    //
    // Camera 위치/Zoom은 현재 프레임 상태를 유지한다.
    private void StopEventCameraShotImmediately()
    {
        PixelCameraController cameraController =
            GetEventPixelCameraController();

        if (cameraController == null)
        {
            return;
        }

        cameraController
            .StopEventCameraShotImmediately();
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

        // <변경부분>
        // Wait For Complete = false인 CameraShot과
        // ScreenShake가 같은 Camera Transform을 동시에 수정하지 않도록
        // CameraShot을 현재 프레임 위치에서 먼저 확정한다.
        //
        // Camera는 이전 위치로 돌아가지 않는다.
        PixelCameraController cameraController =
            GetEventPixelCameraController();

        if (cameraController != null &&
            cameraController.IsEventCameraShotPlaying)
        {
            cameraController
                .StopEventCameraShotImmediately();
        }

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
    // <변경부분>
    // Event Scene Dialogue 실행.
    //
    // Localization Resolve는 StepData가 담당하고,
    // EventGuideUI는 페이지와 선택지 UI를 표시한다.
    //
    // 선택지가 없다면 기존 방식으로 다음 Step에 진행한다.
    // 선택지가 있다면 플레이어가 지정한 Target Step으로 분기한다.
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

        bool hasChoices =
            step.dialogueChoices != null &&
            step.dialogueChoices.Count > 0;

        string selectedTargetStepId =
            null;

        // <변경부분>
        // 선택지가 없는 기존 Dialogue는 이전 방식 그대로 진행한다.
        // 선택지가 있으면 UI가 마지막 페이지에서 선택 결과를 전달한다.
        yield return
            eventGuideUI.PlayDialogueRoutine(
                resolvedDialoguePages,
                hasChoices
                    ? step.dialogueChoices
                    : null,
                targetId =>
                {
                    selectedTargetStepId =
                        targetId;
                }
            );

        if (!hasChoices ||
            !isSequenceActive)
        {
            yield break;
        }

        // <변경부분>
        // 타깃 누락은 다른 Step으로 조용히 진행하지 않고 오류로 정지한다.
        if (string.IsNullOrWhiteSpace(
                selectedTargetStepId) ||
            currentEventSceneData == null ||
            currentEventSceneData.steps == null)
        {
            Debug.LogError(
                "Event Scene Dialogue 분기 실패: " +
                "선택 결과가 없거나 이벤트 Step 목록이 없습니다.",
                this
            );

            isSequenceActive =
                false;

            yield break;
        }

        List<EventSceneStepData> steps =
            currentEventSceneData.steps;

        // <변경부분>
        // Step 번호가 아닌 Stable Step ID로 실제 목적지를 검색한다.
        for (int i = 0;
             i < steps.Count;
             i++)
        {
            EventSceneStepData targetStep =
                steps[i];

            if (targetStep == null ||
                targetStep.stepId !=
                    selectedTargetStepId)
            {
                continue;
            }

            pendingBranchStepIndex =
                i;

            Debug.Log(
                $"Event Scene Dialogue 선택 분기: " +
                $"{step.stepName} → " +
                $"Step{i + 1:D2} / {targetStep.stepName}",
                this
            );

            yield break;
        }

        // <변경부분>
        // Target Step이 삭제된 경우 등에 엉뚱한 Step을 실행하지 않는다.
        Debug.LogError(
            $"Event Scene Dialogue 분기 실패: " +
            $"Target Step ID '{selectedTargetStepId}'를 찾지 못했습니다.",
            this
        );

        isSequenceActive =
            false;
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
    // ShowRewardSelection Step 실행.
    //
    // 현재 Event Scene을 유지한 상태에서
    // 독립적인 보상 팝업을 표시한다.
    //
    // 플레이어 조작 Lock / Unlock 상태는 변경하지 않는다.
    // 기존 UnlockPlayerInteraction / LockPlayerInteraction
    // Step에서 결정한 상태를 그대로 유지한다.
    //
    // 모든 보상 수령 또는 Confirm 이후
    // 현재 Step을 종료하여 다음 Step으로 진행한다.
    private IEnumerator ExecuteShowRewardSelectionStepRoutine(
        EventSceneStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        // <변경부분>
        // 두 보상 모두 OFF면 팝업 없이 다음 Step으로 진행한다.
        if (!step.rewardSelectionUseDropRewards &&
            !step.rewardSelectionUseRestoration)
        {
            yield break;
        }

        // <변경부분>
        // 전리품을 사용하지 않는 정비 Step에서는
        // BattleRewardData가 없어도 된다.
        if (step.rewardSelectionUseDropRewards &&
     step.rewardSelectionData == null)
        {
            Debug.LogError(
                "Event Scene Reward Selection 실패: " +
                "Use Drop Rewards가 ON이지만 BattleRewardData가 없습니다.",
                this
            );

            // <변경부분>
            // 보상 지급 실패 후 이벤트를 정상 완료하지 않도록 중단한다.
            isSequenceActive = false;

            yield break;
        }

        if (eventSceneRewardPopupUI == null)
        {
            Debug.LogError(
                "Event Scene Reward Selection 실패: " +
                "EventSceneRewardPopupUI가 연결되지 않았습니다.",
                this
            );

            // <변경부분>
            // 보상 UI 참조가 없으면 정상 완료 경로를 차단한다.
            isSequenceActive = false;

            yield break;
        }

        // <변경부분>
        // 보상 팝업 자체는 Player Interaction Lock을
        // 변경하지 않는다.
        //
        // 따라서 이전 Step이 UnlockPlayerInteraction이면
        // 카메라 이동 / 확대 / 축소 허용 상태가 유지된다.
        //
        // 이전 Step이 Lock 상태였다면
        // 그 상태 역시 그대로 유지된다.
        bool rewardSelectionFinished =
            false;

        bool started =
                        eventSceneRewardPopupUI.TryShow(
                step.rewardSelectionData,
                step.rewardSelectionUseDropRewards,
                step.rewardSelectionUseRestoration,
                Mathf.Max(0, step.rewardSelectionRestorationAbsorbCount),
                () =>
                {
                    rewardSelectionFinished = true;
                }
            );

        if (!started)
        {
            // <변경부분>
            // 보상 팝업이 열리지 못했는데 정비 노드가
            // 완료되는 상황을 방지한다.
            Debug.LogError(
                "Event Scene Reward Selection 실패: " +
                "보상 팝업을 열지 못해 이벤트 진행을 중단합니다.",
                this
            );

            isSequenceActive = false;

            yield break;
        }

        // <변경부분>
        // 플레이어가 보상창에서 수령을 마칠 때까지
        // 시퀀스의 다음 Step 실행을 정지한다.
        while (!rewardSelectionFinished)
        {
            if (!isSequenceActive)
            {
                eventSceneRewardPopupUI.HideImmediately();

                yield break;
            }

            yield return null;
        }

        // <변경부분>
        // 팝업 완료 후에도 기존 조작 상태를 유지한다.
        // 다음 Lock / Unlock은 해당 Step에서만 수행한다.
        Debug.Log(
            "Event Scene Reward Selection 완료: " +
            "다음 Event Step으로 진행합니다."
        );
    }


    // <변경부분>
    // StartBattle Step 실행.
    //
    // EventScene에서 StageBattleData를 지정하고
    // 기존 BattleScene으로 이동한다.
    //
    // 현재 월드맵 노드와 연결된 전투인지,
    // 독립적으로 발생한 전투인지 구분하여
    // WorldMapRuntimeState에 등록한다.
    //
    // 실제 배경, 기물, 승패 조건, 전투 보상은
    // 기존 BattleSetupManager가 담당한다.
    private void ExecuteStartBattleStep(
        EventSceneStepData step)
    {
        if (step == null ||
    step.startBattleStageData == null)
        {
            Debug.LogError(
                "Event Scene StartBattle 실패: " +
                "StageBattleData가 연결되지 않았습니다.",
                this
            );

            isSequenceActive = false;
            return;
        }

        const string battleSceneName =
            "BattleScene";

        // <변경부분>
        // Scene이 빌드에 등록되어 있는지 먼저 확인한다.
        // 등록되지 않았다면 RunState를 변경하지 않는다.
        if (!Application.CanStreamedLevelBeLoaded(
                battleSceneName))
        {
            Debug.LogError(
                "Event Scene StartBattle 실패: " +
                $"'{battleSceneName}' Scene을 로드할 수 없습니다. " +
                "Build Profiles의 Scene 목록을 확인해 주세요.",
                this
            );

            isSequenceActive = false;
            return;
        }

        StageBattleData targetBattleData =
            step.startBattleStageData;

        string currentNodeId =
            WorldMapRuntimeState.CurrentNodeId;

        // <변경부분>
        // 일반 Event Node에서 발생한 전투라면
        // 전투 승리 후 현재 월드맵 노드를
        // 클리어할 수 있도록 Node ID를 함께 전달한다.
        bool registerAsWorldMapNodeBattle =
            step.clearCurrentWorldMapNodeOnVictory &&
            !string.IsNullOrWhiteSpace(currentNodeId);

        if (registerAsWorldMapNodeBattle)
        {
            WorldMapRuntimeState.BeginBattleNode(
                currentNodeId,
                targetBattleData
            );

            Debug.Log(
                "Event Scene StartBattle: " +
                $"월드맵 노드 연결 전투 / {currentNodeId} / " +
                $"{targetBattleData.name}",
                this
            );
        }
        else
        {
            // <변경부분>
            // WorldMap Node와 관계없는 전투이거나
            // EventScene 단독 테스트인 경우.
            //
            // 기존 End Stage에서도 사용 중인
            // Direct Battle 등록 API를 재사용한다.
            bool registered =
                WorldMapRuntimeState.BeginDirectBattle(
                    targetBattleData
                );

            if (!registered)
            {
                Debug.LogError(
                    "Event Scene StartBattle 실패: " +
                    "Direct Battle 등록에 실패했습니다.",
                    this
                );

                isSequenceActive = false;
                return;
            }

            Debug.Log(
                "Event Scene StartBattle: " +
                $"독립 전투 / {targetBattleData.name}",
                this
            );
        }

        // <변경부분>
        // Scene 이동이 발생하므로
        // 현재 Event Sequence가 뒤쪽 Step이나
        // CompletionType을 별도로 실행하지 않게 한다.
        isSequenceActive = false;

        Debug.Log(
            "Event Scene StartBattle: " +
            $"{battleSceneName}으로 이동합니다.",
            this
        );

        // <변경부분>
        // BattleScene에 별도 초기화 인자를 직접 전달하지 않는다.
        // 기존 BattleSetupManager가 PendingStageBattleData를 읽는다.
        SceneManager.LoadScene(
            battleSceneName
        );
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

        // <변경부분>
        pendingBranchStepIndex =
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

                // <변경부분>
                // 정상 종료되는 Event / RuinsEvent의
                // 월드맵 노드 완료를 예약한다.
                RegisterCurrentEventNodeCompletionIfRequested();

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
                // 이동할 Scene이 실제로 등록되지 않았다면
                // 노드 완료를 기록하지 않는다.
                if (!Application.CanStreamedLevelBeLoaded(
                        currentEventSceneData.completionSceneName))
                {
                    Debug.LogWarning(
                        "Event Scene 완료 Scene 이동 실패: " +
                        $"'{currentEventSceneData.completionSceneName}' " +
                        "Scene을 로드할 수 없습니다.",
                        this
                    );

                    return;
                }

                // <변경부분>
                // LoadScene도 Sequence의 정상적인 종료이므로
                // 완료 옵션이 켜져 있으면 월드맵 노드 완료를 예약한다.
                RegisterCurrentEventNodeCompletionIfRequested();

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


// <변경부분>
// 월드맵 이벤트에서 정상적으로 진입한 경우에 한해
// EventSceneData의 체크박스로 현재 노드 클리어를 예약한다.
//
// Cleared 적용 및 다음 노드 해금은 월드맵 복귀 시 실행한다.
// StartBattle / 강제 종료 / CompletionType.None에서는 호출하지 않는다.
private void RegisterCurrentEventNodeCompletionIfRequested()
{
    if (currentEventSceneData == null ||
        !currentEventSceneData.clearCurrentWorldMapNodeOnCompletion)
    {
        return;
    }

    if (!WorldMapRuntimeState.TryCompleteEventVisit(
            currentEventSceneData))
    {
        Debug.LogWarning(
            "Event Scene 노드 클리어 예약 실패: " +
            "월드맵에서 정상 진입한 일반 이벤트가 아니거나 " +
            "현재 노드 정보가 일치하지 않습니다.",
            this
        );
    }
}


    // Event Scene Sequence를 외부에서 즉시 정지한다.
    public void StopSequence()
    {
        isSequenceActive =
            false;

        currentStepIndex =
     -1;

        // <변경부분>
        pendingBranchStepIndex =
            -1;

        StopSequenceCoroutine();

        // <변경부분>
        // Sequence 강제 종료 시 Detached Glitch도 즉시 정리한다.
        if (eventScreenGlitchController != null)
        {
            eventScreenGlitchController.StopGlitch();
        }

        // <변경부분>
        // 선택형 전리품창이 열린 상태에서 시퀀스가
        // 강제 종료되어도 보상 UI와 Callback을 정리한다.
        if (eventSceneRewardPopupUI != null)
        {
            eventSceneRewardPopupUI.HideImmediately();
        }

        // <변경부분>
        // Detached CameraShot이 Sequence 종료 후에도 남지 않도록 정리.
        StopEventCameraShotImmediately();

        if (eventGuideUI != null)
        {
            eventGuideUI.HideImmediately();
        }

        // <변경부분>
        // Sequence 강제 종료 시
        // Run HUD Fade도 정리하고 즉시 숨긴다.
        SetRunHUDVisibleImmediately(
            false
        );

        // <변경부분>
        // Sequence를 강제로 중단한 뒤에도
        // 플레이어 입력 Lock이 남아 있지 않도록 해제한다.
        SetEventScenePlayerInteractionLocked(
            false
        );

        // <변경부분>
        // 강제 종료 시 상점 나가기 버튼도 숨긴다.
        SetShopExitButtonVisible(false);

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
        // Scene 전환이나 Controller 비활성화 시
        // 화면 글리치가 남지 않도록 즉시 종료한다.
        if (eventScreenGlitchController != null)
        {
            eventScreenGlitchController.StopGlitch();
        }

        // <변경부분>
        // 씬 전환이나 Controller 비활성화 시
        // 이벤트 연출용 조명값을 복구한다.
        RestoreGlobalLightingImmediately();

        // <변경부분>
        // 이벤트씬 비활성화 시 보상창과 대기 Callback을 정리한다.
        if (eventSceneRewardPopupUI != null)
        {
            eventSceneRewardPopupUI.HideImmediately();
        }

        // <변경부분>
        // Event Scene Controller가 비활성화될 // ShowRewardSelection Step 실행.
        // Detached CameraShot Coroutine도 남기지 않는다.
        StopEventCameraShotImmediately();

        // Camera Shake 도중 Scene 전환 또는 비활성화가 발생해도
        // Camera가 흔들린 좌표에 남지 않도록 즉시 원위치 복구.
        StopCameraShakeImmediately();

        // <변경부분>
        // Scene 전환 / 비활성화 시
        // Run HUD Coroutine과 표시 상태를 함께 정리한다.
        SetRunHUDVisibleImmediately(
      false
  );

        // <변경부분>
        // EventScene Controller가 비활성화되면
        // 상점 나가기 버튼도 숨긴다.
        SetShopExitButtonVisible(false);

        // <변경부분>
        // Event Scene Controller가 비활성화되면
        // Event Scene 전체의 일반 플레이어 조작 Lock을 해제한다.
        //
        // Camera 입력도 동일한 공용 상태를 통해 함께 해제된다.
        SetEventScenePlayerInteractionLocked(
            false
        );

        isSequenceActive =
    false;

        currentStepIndex =
            -1;

        // <변경부분>
        pendingBranchStepIndex =
            -1;

        currentEventSceneData =
            null;
    }
}
