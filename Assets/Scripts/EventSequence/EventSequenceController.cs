using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// <변경부분> EventSequenceData의 Step을 순서대로 실행하는
// 튜토리얼 / 전투 이벤트 / 스토리 이벤트 공용 컨트롤러
//
// 이 컴포넌트와 EventSequenceData가 연결된 경우에만 동작하며,
// 비활성화하거나 데이터가 없으면 기존 Battle 시스템에는 전혀 관여하지 않는다.
public class EventSequenceController : MonoBehaviour
{
    // <변경부분> 현재 스테이지에서 실행할 EventSequenceData.
    //
    // 더 이상 BattleScene Inspector에서 직접 고정하지 않는다.
    // BattleSetupManager가 현재 StageBattleData를 읽은 뒤
    // SetSequenceData()를 통해 전달한다.
    //
    // 따라서 같은 BattleScene을 사용하는 일반 스테이지에서
    // 이전 튜토리얼 Sequence가 자동 실행되는 문제를 방지한다.
    private EventSequenceData sequenceData;

    [Header("Managers")]
    // 기물 생성 Step에서 사용할 기존 PieceManager
    [SerializeField]
    private PieceManager pieceManager;

    // <변경부분> Event Sequence 완료 타입이 BattleWin일 때
    // 기존 Battle 승리 / 보상 흐름으로 연결하기 위한 BattleManager.
    [SerializeField]
    private BattleManager battleManager;

    [Header("Dialogue")]
    // 다음 단계에서 제작할 텍스트 UI 컨트롤러
    //
    // 아직 EventGuideUI가 없는 상태에서도
    // Dialogue Step을 건너뛰며 나머지 기능 테스트가 가능하다.
    [SerializeField]
    private EventGuideUI eventGuideUI;

    [Header("Marker")]
    // <변경부분> ForcePiece / ForceTile / ForceButton Step에서
    // 현재 플레이어가 눌러야 하는 대상을 자동으로 가리키는 공용 마커
    //
    // 연결하지 않으면 입력 강제 기능만 동작하고
    // 마커 표시는 생략된다.
    [SerializeField]
    private EventMarkerUI eventMarkerUI;


    // =====================================================
    // Battle UI Visibility
    // =====================================================

    [Header("Battle UI Visibility")]

    // <변경부분>
    // EndTimerRoot_Battle 오브젝트의 CanvasGroup.
    // UI만 숨기고 타이머 관련 컴포넌트는 활성 상태로 유지한다.
    [SerializeField]
    private CanvasGroup endTimerRootBattleCanvasGroup;

    // <변경부분>
    // GoldRoot 오브젝트의 CanvasGroup.
    [SerializeField]
    private CanvasGroup goldRootCanvasGroup;

    // =====================================================
    // Registered Battle UI Targets
    // =====================================================

    // <변경부분>
    // BattleScene에 존재하는 실제 UI를 등록한다.
    //
    // UI ID는 EventSequenceData에서 대상을 지정할 때
    // 사용하며, 동일한 ID를 중복 등록하면 안 된다.
    [System.Serializable]
    private sealed class BattleUIBinding
    {
        public string uiId;
        public CanvasGroup canvasGroup;
    }

    [Header("Registered Battle UI Targets")]
    [SerializeField]
    private List<BattleUIBinding> battleUIBindings =
        new List<BattleUIBinding>();


    // <변경부분>
    // 이벤트 시작 전 UI 상태를 보관한다.
    // 숨김, 표시를 여러 번 실행해도 최초 상태는 유지한다.
    private sealed class BattleUIVisibilityState
    {
        public float alpha;
        public bool interactable;
        public bool blocksRaycasts;

        // <변경부분>
        // 부모 CanvasGroup의 영향을 무시하는 설정도
        // 이벤트 종료 시 원래대로 복원하기 위해 보관한다.
        public bool ignoreParentGroups;
    }

    private readonly Dictionary<CanvasGroup, BattleUIVisibilityState>
      battleUIVisibilityStates =
          new Dictionary<CanvasGroup, BattleUIVisibilityState>();

    // <변경부분>
    // EndTimerRoot_Battle과 GoldRoot의 Fade Coroutine을
    // 각각 독립적으로 관리한다.
    //
    // 진행 중 반대 방향의 페이드가 요청되면
    // 이전 Coroutine을 중단하고 현재 Alpha에서 다시 시작한다.
    private readonly Dictionary<CanvasGroup, Coroutine>
        battleUIFadeCoroutines =
            new Dictionary<CanvasGroup, Coroutine>();


    // =====================================================
    // Battle UI Visibility
    // =====================================================

    // <변경부분>
    // 기존 EndTimer/Gold 전용 방식과
    // 새 등록 UI 목록 방식을 모두 지원한다.
    //
    // 모든 UI의 Fade는 먼저 동시에 시작하고,
    // Wait For Complete가 true인 경우 완료까지 대기한다.
    private IEnumerator ExecuteSetBattleUIVisibilityStepRoutine(
        EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        float fadeDuration =
            Mathf.Max(0f, step.battleUIFadeDuration);

        List<Coroutine> fades =
            new List<Coroutine>();

        // <변경부분>
        // 같은 CanvasGroup이 한 스텝에서 중복 처리되는 것을 방지.
        HashSet<CanvasGroup> processedGroups =
            new HashSet<CanvasGroup>();

        // =================================================
        // Registered UI Targets
        // =================================================

        if (step.battleUIUseRegisteredTargets)
        {
            if (step.battleUIVisibilityTargets == null ||
                step.battleUIVisibilityTargets.Count == 0)
            {
                Debug.LogWarning(
                    "Battle UI Visibility: " +
                    "등록 UI 모드가 활성화되었지만 " +
                    "스텝에 지정된 UI가 없습니다.",
                    this
                );
            }
            else
            {
                foreach (
                    EventBattleUIVisibilityTarget target
                    in step.battleUIVisibilityTargets)
                {
                    if (target == null)
                    {
                        continue;
                    }

                    CanvasGroup targetGroup;

                    if (!TryGetRegisteredBattleUIGroup(
                            target.uiId,
                            out targetGroup))
                    {
                        continue;
                    }

                    if (!processedGroups.Add(targetGroup))
                    {
                        Debug.LogWarning(
                            "Battle UI Visibility: " +
                            $"같은 UI가 한 스텝에 중복 지정되었습니다: " +
                            $"{target.uiId}",
                            this
                        );

                        continue;
                    }

                    Coroutine fade =
                        SetBattleUIGroupVisibility(
                            targetGroup,
                            target.show,
                            fadeDuration,
                            target.uiId
                        );

                    if (fade != null)
                    {
                        fades.Add(fade);
                    }
                }
            }
        }

        // =================================================
        // Legacy EndTimer / Gold
        // =================================================

        else
        {
            // 기존 이벤트 에셋의 EndTimer 동작 유지.
            if (step.battleUIChangeEndTimerRoot)
            {
                Coroutine fade =
                    SetBattleUIGroupVisibility(
                        endTimerRootBattleCanvasGroup,
                        step.battleUIShowEndTimerRoot,
                        fadeDuration,
                        "EndTimerRoot_Battle"
                    );

                if (fade != null)
                {
                    fades.Add(fade);
                }
            }

            // 기존 이벤트 에셋의 Gold 동작 유지.
            if (step.battleUIChangeGoldRoot)
            {
                Coroutine fade =
                    SetBattleUIGroupVisibility(
                        goldRootCanvasGroup,
                        step.battleUIShowGoldRoot,
                        fadeDuration,
                        "GoldRoot"
                    );

                if (fade != null)
                {
                    fades.Add(fade);
                }
            }
        }

        // =================================================
        // Wait For Complete
        // =================================================

        if (!step.battleUIWaitForComplete)
        {
            yield break;
        }

        for (int i = 0; i < fades.Count; i++)
        {
            if (fades[i] != null)
            {
                yield return fades[i];
            }
        }
    }


    // <변경부분>
    // EventSequenceData의 UI ID를
    // 현재 BattleScene의 CanvasGroup 참조로 변환한다.
    //
    // 잘못된 ID, 중복 ID, 미연결 또는 비활성 UI는
    // 잘못된 오브젝트를 변경하지 않도록 차단한다.
    private bool TryGetRegisteredBattleUIGroup(
        string uiId,
        out CanvasGroup targetGroup)
    {
        targetGroup = null;

        string normalizedId =
            string.IsNullOrWhiteSpace(uiId)
                ? string.Empty
                : uiId.Trim();

        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogWarning(
                "Battle UI Visibility 실행 실패: " +
                "UI ID가 비어 있습니다.",
                this
            );

            return false;
        }

        bool found = false;

        if (battleUIBindings != null)
        {
            foreach (BattleUIBinding binding in battleUIBindings)
            {
                if (binding == null ||
                    string.IsNullOrWhiteSpace(binding.uiId))
                {
                    continue;
                }

                if (!string.Equals(
                        binding.uiId.Trim(),
                        normalizedId,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (found)
                {
                    Debug.LogWarning(
                        "Battle UI Visibility 실행 실패: " +
                        $"중복 등록된 UI ID입니다: {normalizedId}",
                        this
                    );

                    targetGroup = null;
                    return false;
                }

                found = true;
                targetGroup = binding.canvasGroup;
            }
        }

        if (!found || targetGroup == null)
        {
            Debug.LogWarning(
                "Battle UI Visibility 실행 실패: " +
                $"UI ID가 없거나 CanvasGroup이 연결되지 않았습니다: " +
                $"{normalizedId}",
                this
            );

            return false;
        }

        // CanvasGroup은 GameObject 활성화 기능을 대신하지 않는다.
        if (!targetGroup.gameObject.activeInHierarchy ||
            !targetGroup.enabled)
        {
            Debug.LogWarning(
                "Battle UI Visibility 실행 실패: " +
                $"대상 UI가 비활성 상태입니다: {normalizedId}",
                this
            );

            return false;
        }

        return true;
    }


    // <변경부분>
    // 현재 Alpha에서 목표 Alpha까지 Fade를 시작한다.
    //
    // 기존 CanvasGroup의 초기 설정을 한 번만 저장하고,
    // 같은 UI에 새로운 요청이 오면 이전 Fade를 중단한다.
    private Coroutine SetBattleUIGroupVisibility(
        CanvasGroup targetGroup,
        bool visible,
        float fadeDuration,
        string rootName)
    {
        if (targetGroup == null)
        {
            Debug.LogWarning(
                "Battle UI Visibility 실행 실패: " +
                $"{rootName} CanvasGroup이 연결되지 않았습니다.",
                this
            );

            return null;
        }

        // =================================================
        // Initial State
        // =================================================

        BattleUIVisibilityState originalState;

        if (!battleUIVisibilityStates.TryGetValue(
            targetGroup,
            out originalState))
        {
            originalState =
                new BattleUIVisibilityState
                {
                    alpha = targetGroup.alpha,
                    interactable = targetGroup.interactable,
                    blocksRaycasts = targetGroup.blocksRaycasts,

                    // <변경부분>
                    // 기존 Inspector의 Ignore Parent Groups 설정 보존.
                    ignoreParentGroups =
                        targetGroup.ignoreParentGroups
                };

            battleUIVisibilityStates.Add(
                targetGroup,
                originalState
            );
        }

        // =================================================
        // Stop Previous Fade
        // =================================================

        Coroutine previousFade;

        if (battleUIFadeCoroutines.TryGetValue(
                targetGroup,
                out previousFade))
        {
            if (previousFade != null)
            {
                StopCoroutine(previousFade);
            }

            battleUIFadeCoroutines.Remove(
       targetGroup
   );
        }

        // <변경부분>
        // 이벤트에서 명시적으로 표시할 때는
        // 상위 Battle UI CanvasGroup이 숨겨져 있어도
        // 해당 EndTimer / Gold UI만 독립적으로 보이게 한다.
        //
        // 숨김 요청에서는 기존 Inspector 설정으로 되돌린다.
        targetGroup.ignoreParentGroups =
            visible || originalState.ignoreParentGroups;

        float targetAlpha =
            visible ? 1f : 0f;

        // =================================================
        // Immediate
        // =================================================

        // <변경부분>
        // Duration이 0이거나 이미 목표 Alpha라면 즉시 완료.
        if (fadeDuration <= 0f ||
            Mathf.Approximately(
                targetGroup.alpha,
                targetAlpha
            ))
        {
            targetGroup.alpha =
                targetAlpha;

            targetGroup.interactable =
                visible && originalState.interactable;

            targetGroup.blocksRaycasts =
                visible && originalState.blocksRaycasts;

            return null;
        }

        // =================================================
        // Fade
        // =================================================

        // <변경부분>
        // 페이드 중에는 투명한 UI가 입력을 가로채거나
        // 아직 나타나지 않은 버튼이 클릭되지 않도록 한다.
        // 완전히 표시된 다음 원래 입력 설정을 복구한다.
        targetGroup.interactable = false;
        targetGroup.blocksRaycasts = false;

        Coroutine fadeCoroutine =
            StartCoroutine(
                FadeBattleUIGroupRoutine(
                    targetGroup,
                    targetAlpha,
                    fadeDuration,
                    visible,
                    originalState
                )
            );

        battleUIFadeCoroutines[targetGroup] =
            fadeCoroutine;

        return fadeCoroutine;
    }


    // =====================================================
    // Battle UI Fade Coroutine
    // =====================================================

    // <변경부분>
    // 현재 CanvasGroup Alpha에서 목표 Alpha까지
    // SmoothStep 보간으로 자연스럽게 전환한다.
    //
    // Fade Out: 현재 Alpha -> 0
    // Fade In : 현재 Alpha -> 1
    //
    // Time.unscaledDeltaTime을 사용하므로
    // 전투 Time Scale이 변경되어도 일정한 속도로 진행한다.
    private IEnumerator FadeBattleUIGroupRoutine(
        CanvasGroup targetGroup,
        float targetAlpha,
        float duration,
        bool visible,
        BattleUIVisibilityState originalState)
    {
        if (targetGroup == null)
        {
            yield break;
        }

        float startAlpha =
            targetGroup.alpha;

        float elapsed =
            0f;

        while (elapsed < duration &&
               targetGroup != null)
        {
            elapsed += Time.unscaledDeltaTime;

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

            targetGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    easedT
                );

            yield return null;
        }

        // <변경부분>
        // Fade 종료 시 최종 Alpha를 정확히 적용한다.
        if (targetGroup != null)
        {
            targetGroup.alpha =
                targetAlpha;

            // <변경부분>
            // Fade In 완료 후 원래 UI 입력 설정을 복구한다.
            // Fade Out 완료 후에는 입력을 차단한 상태를 유지한다.
            targetGroup.interactable =
                visible && originalState.interactable;

            targetGroup.blocksRaycasts =
                visible && originalState.blocksRaycasts;
        }

        // <변경부분>
        // 완료된 Coroutine 관리 정보를 제거한다.
        battleUIFadeCoroutines.Remove(
            targetGroup
        );
    }


    private void RestoreBattleUIVisibilityImmediately()
    {
        // <변경부분>
        // 복구 전에 진행 중인 모든 Fade를 중단한다.
        // 그렇지 않으면 복구한 Alpha를 다음 프레임에
        // Fade Coroutine이 다시 변경할 수 있다.
        foreach (
            KeyValuePair<CanvasGroup, Coroutine> entry
            in battleUIFadeCoroutines)
        {
            if (entry.Value != null)
            {
                StopCoroutine(
                    entry.Value
                );
            }
        }

        battleUIFadeCoroutines.Clear();

        // <변경부분>
        // 최초 CanvasGroup 상태를 복구한다.
        foreach (
            KeyValuePair<CanvasGroup, BattleUIVisibilityState> entry
            in battleUIVisibilityStates)
        {
            CanvasGroup targetGroup =
                entry.Key;

            BattleUIVisibilityState originalState =
                entry.Value;

            if (targetGroup == null ||
                originalState == null)
            {
                continue;
            }

            targetGroup.alpha =
    originalState.alpha;

            targetGroup.interactable =
                originalState.interactable;

            targetGroup.blocksRaycasts =
                originalState.blocksRaycasts;

            // <변경부분>
            // 이벤트가 끝나면 부모 CanvasGroup 무시 설정도
            // 최초 Inspector 상태로 복원한다.
            targetGroup.ignoreParentGroups =
                originalState.ignoreParentGroups;
        }

        battleUIVisibilityStates.Clear();
    }

    // =====================================================
    // Screen Glitch
    // =====================================================

    // =====================================================
    // Screen Shake
    // =====================================================

    [Header("Screen Shake")]

    // <변경부분>
    // BattleScene에서 화면을 흔들 Transform.
    //
    // 기존 전투 카메라 연출과 충돌하지 않도록
    // 가능하면 별도의 Camera Shake Target을 연결한다.
    //
    // 연결하지 않으면 Main Camera Transform을 사용한다.
    [SerializeField]
    private Transform cameraShakeTarget;

    // <변경부분>
    // 현재 실행 중인 화면 흔들림 Coroutine.
    private Coroutine cameraShakeCoroutine;

    // <변경부분>
    // 실제로 흔들고 있는 Transform과
    // 흔들림 시작 전 Local Position을 보관한다.
    private Transform activeCameraShakeTarget;

    private Vector3 cameraShakeBaseLocalPosition;

    // <변경부분>
    // 지속 화면 흔들림 실행 여부.
    //
    // true 상태에서는 일반 시간제 Shake 요청이
    // 지속 흔들림을 중단하지 않도록 보호한다.
    private bool isContinuousCameraShakeActive =
        false;


    // =====================================================
    // Screen Glitch
    // =====================================================

    [Header("Screen Glitch")]

    // <변경부분>
    // BattleScene에 배치된 글리치 컨트롤러.
    // 기존 EventScene과 동일한 컴포넌트를 사용한다.
    [SerializeField]
    private EventScreenGlitchController eventScreenGlitchController;


    // =====================================================
    // Global Lighting
    // =====================================================

    [Header("Global Lighting")]

    // <변경부분>
    // BattleScene의 Global Ambient Light2D.
    [SerializeField]
    private Light2D environmentGlobalAmbient;

    // <변경부분>
    // 조명 페이드와 최초 조명값 보존.
    private Coroutine globalLightingFadeCoroutine;

    private bool hasCapturedGlobalLightingBase;

    private Color globalLightingBaseColor;

    private float globalLightingBaseIntensity;


    // =====================================================
    // Character Fill
    // =====================================================

    // <변경부분>
    // BattleScene의 Character_Fill Light2D를 연결한다.
    [Header("Character Fill")]
    [SerializeField]
    private Light2D characterFillLight;

    // <변경부분>
    // Global Ambient와 독립적인 페이드 및 초기값 보존.
    private Coroutine characterFillFadeCoroutine;
    private bool hasCapturedCharacterFillBase;
    private Color characterFillBaseColor;
    private float characterFillBaseIntensity;


    // =====================================================
    // Screen Flash
    // =====================================================

    // <변경부분>
    // 화면 플래시는 실행 시점에 Overlay Canvas를 생성한다.
    private GameObject globalLightingFlashCanvasObject;

    private Image globalLightingFlashImage;

    private Coroutine globalLightingFlashCoroutine;


    [Header("Debug")]
    // <변경부분> 현재 시퀀스 실행 여부는
    // Scene에 저장되는 설정값이 아니라 순수 런타임 상태다.
    //
    // SerializeField로 저장하면 Inspector에 남아 있던 true 값 때문에
    // StartSequence()가 "이미 실행 중"으로 판단하여
    // 실제 Sequence Coroutine을 시작하지 않는 문제가 발생할 수 있다.
    private bool isSequenceActive =
    false;

    // <변경부분> 현재 Step 번호 역시
    // 실행 중에만 사용하는 런타임 상태로 관리한다.
    private int currentStepIndex =
        -1;

    // 현재 실행 중인 메인 시퀀스 코루틴
    private Coroutine sequenceCoroutine;

    // <변경부분> 현재 ForcePieceSelect Step에서
    // 플레이어의 올바른 기물 선택을 기다리고 있는지 확인한다.
    private bool isWaitingForPieceSelection =
        false;

    // <변경부분> 현재 강제 선택 대상으로 지정된 기물 좌표
    private Vector2Int requiredPiecePosition =
        Vector2Int.zero;

    // <변경부분> 현재 강제 선택 대상으로 지정된 기물 진영
    private PieceTeam requiredPieceTeam =
        PieceTeam.Player;

    // <변경부분> 현재 ForceTileSelect Step에서
    // 지정한 타일 클릭을 기다리고 있는지 확인한다.
    private bool isWaitingForTileSelection =
        false;

    // <변경부분> ForceTileSelect에서
    // 플레이어가 반드시 클릭해야 하는 보드 좌표
    private Vector2Int requiredTilePosition =
        Vector2Int.zero;

    // <변경부분> 현재 ForceButton Step에서
    // 지정한 전투 UI 버튼 입력을 기다리고 있는지 확인한다.
    private bool isWaitingForButtonInput =
        false;

    // <변경부분> ForceButton에서
    // 플레이어가 반드시 눌러야 하는 버튼 종류
    private EventSequenceButtonType requiredButtonType =
        EventSequenceButtonType.None;

    // <변경부분> Force Step 하나가 완료된 뒤
    // 다음 Event Step의 입력 상태가 준비될 때까지
    // 기물 / 타일 / 버튼의 추가 입력을 막는다.
    //
    // 빠르게 연속 클릭했을 때
    // 이전 Step 종료와 다음 Step 시작 사이의 짧은 공백으로
    // 일반 Battle 입력이 통과하는 것을 방지한다.
    private bool isStepTransitionInputLocked =
        false;

    // 외부 Battle 시스템이
    // 이벤트 진행 여부를 확인할 때 사용할 공개 프로퍼티
    public bool IsSequenceActive
    {
        get
        {
            return isSequenceActive;
        }
    }

    public EventSequenceData SequenceData
    {
        get
        {
            return sequenceData;
        }
    }

    // <변경부분> BattleSetupManager가
    // 현재 StageBattleData의 EventSequenceData를 전달할 때 사용한다.
    //
    // 데이터 연결과 실제 실행을 분리하여
    // EventSequenceData의 Play Automatically 설정을
    // BattleSetupManager가 판단할 수 있도록 한다.
    //
    // null을 전달하면 현재 Battle은
    // Event Sequence가 없는 일반 스테이지로 동작한다.
    public void SetSequenceData(
        EventSequenceData newSequenceData)
    {
        // 혹시 기존 Sequence가 실행 중이라면
        // 새 데이터를 적용하기 전에 안전하게 종료한다.
        if (isSequenceActive)
        {
            StopSequence();
        }

        sequenceData =
            newSequenceData;

        if (sequenceData == null)
        {
            Debug.Log(
                "Event Sequence Data 해제: " +
                "현재 스테이지에서는 이벤트를 실행하지 않습니다."
            );

            return;
        }

        Debug.Log(
            $"Event Sequence Data 적용: " +
            $"{sequenceData.name} / " +
            $"PlayAutomatically={sequenceData.playAutomatically}"
        );
    }

    // <변경부분> 현재 Event Sequence가 활성화되어 있고
    // 데이터에서 일반 Battle 승패 무시가 설정되어 있는지 반환한다.
    // 데이터에서 일반 Battle 승패 무시가 설정되어 있는지 반환한다.
    //
    // BattleManager는 이 값만 확인하면 되므로
    // EventSequenceData의 내부 설정을 직접 알 필요가 없다.
    public bool ShouldIgnoreNormalBattleEnd
    {
        get
        {
            return
                isSequenceActive &&
                sequenceData != null &&
                sequenceData.ignoreNormalBattleEnd;
        }
    }

    // <변경부분> 현재 Event Sequence가 활성화되어 있고
    // Enemy AI 일시정지가 설정되어 있는지 반환한다.
    //
    // AI는 시퀀스가 끝날 때까지 기다렸다가
    // 기존 Enemy 턴을 그대로 이어갈 수 있다.
    public bool ShouldPauseEnemyAI
    {
        get
        {
            return
                isSequenceActive &&
                sequenceData != null &&
                sequenceData.pauseEnemyAIWhileSequenceActive;
        }
    }

    // <변경부분> 현재 Event Sequence가
    // Enemy AI를 정지한 상태로 전투 입력을 직접 진행하고 있다면
    // 일반 이동 / 공격 / 흡수 행동이 끝나도
    // BattleManager가 자동으로 턴을 넘기지 않도록 한다.
    //
    // 튜토리얼에서는 여러 ForcePiece / ForceTile / ForceButton Step을
    // 같은 Player Turn 안에서 연속으로 실행해야 하므로 사용한다.
    //
    // Event Sequence가 없거나
    // Pause Enemy AI While Sequence Active가 꺼져 있으면 false이므로
    // 일반 전투의 기존 EndTurn 흐름에는 영향을 주지 않는다.
    public bool ShouldHoldBattleTurnForSequence
    {
        get
        {
            return
                isSequenceActive &&
                sequenceData != null &&
                sequenceData.pauseEnemyAIWhileSequenceActive;
        }
    }

    // <변경부분> 현재 자동 실행 중인 Event Sequence가
    // 기존 플레이어 초기 배치를 건너뛰어야 하는지 반환한다.
    public bool ShouldSkipNormalPlayerDeployment
    {
        get
        {
            return
                isSequenceActive &&
                sequenceData != null &&
                sequenceData.skipNormalPlayerDeployment;
        }
    }

    // <변경부분> 현재 Dialogue가 표시 중이라
    // 기존 Battle 입력을 전부 막아야 하는지 반환한다.
    public bool IsDialogueBlockingBattleInput
    {
        get
        {
            return
                isSequenceActive &&
                eventGuideUI != null &&
                eventGuideUI.IsDialoguePlaying;
        }
    }

    // <변경부분> 현재 튜토리얼 / 이벤트가
    // 특정 보드 입력을 강제로 기다리고 있는지 확인한다.
    //
    // ForcePieceSelect 또는 ForceTileSelect 중에는
    // Space / Q / S 같은 일반 Battle 단축키도 차단한다.
    public bool IsForcedBattleInputActive
    {
        get
        {
            return
                isSequenceActive &&
                (
                    // <변경부분> Dialogue / SpawnPiece / Wait 등
                    // 자동 Event Step 진행 중의 입력 잠금도
                    // BattleManager의 키보드 입력 차단 대상에 포함한다.
                    isStepTransitionInputLocked ||

                    isWaitingForPieceSelection ||
                    isWaitingForTileSelection ||
                    isWaitingForButtonInput
                );
        }
    }

    // <변경부분> BattleManager가 기물을 선택하기 전에
    // 현재 Event Sequence에서 해당 선택을 허용하는지 검사한다.
    //
    // 시퀀스가 없거나 ForcePieceSelect 단계가 아니라면
    // 기존 Battle 선택을 그대로 허용한다.
    public bool CanSelectPiece(
      Piece piece)
    {
        if (isSequenceActive == false)
        {
            return true;
        }

        // <변경부분> 이전 Force Step이 완료되고
        // 다음 Step 입력 준비가 끝나기 전이라면
        // 빠른 연속 클릭으로 기물이 선택되지 않도록 막는다.
        if (isStepTransitionInputLocked)
        {
            return false;
        }

        // Dialogue 중에는 전투 기물 선택을 전부 막는다.
        if (IsDialogueBlockingBattleInput)
        {
            return false;
        }

        // <변경부분> 특정 타일 또는 특정 버튼 입력을 기다리는 동안에는
        // 새로운 기물 선택으로 현재 튜토리얼 상태를
        // 벗어날 수 없도록 모든 기물 선택을 막는다.
        if (isWaitingForTileSelection ||
            isWaitingForButtonInput)
        {
            return false;
        }

        // ForcePieceSelect가 아니라면
        // 기존 Battle 기물 선택을 그대로 허용한다.
        if (isWaitingForPieceSelection == false)
        {
            return true;
        }

        if (piece == null)
        {
            return false;
        }

        if (piece.Team !=
            requiredPieceTeam)
        {
            return false;
        }

        return
            piece.X ==
                requiredPiecePosition.x &&
            piece.Y ==
                requiredPiecePosition.y;
    }

    // <변경부분> BattleManager가 실제 지정 기물 선택에 성공한 뒤 호출한다.
    //
    // 현재 ForcePieceSelect 조건과 일치하는 경우에만
    // 대기 상태를 해제하여 다음 Event Step으로 진행한다.
    public void NotifyPieceSelected(
        Piece piece)
    {
        if (isSequenceActive == false ||
            isWaitingForPieceSelection == false ||
            piece == null)
        {
            return;
        }

        if (piece.Team !=
            requiredPieceTeam)
        {
            return;
        }

        if (piece.X !=
                requiredPiecePosition.x ||
            piece.Y !=
                requiredPiecePosition.y)
        {
            return;
        }

        // <변경부분> 현재 ForcePieceSelect는 완료하지만
        // 다음 Step의 입력 상태가 준비될 때까지
        // 추가 Battle 입력은 잠근다.
        isWaitingForPieceSelection =
            false;

        isStepTransitionInputLocked =
            true;
    }

    // <변경부분> BattleManager가 타일 입력을 처리하기 전에
    // 현재 Event Sequence에서 해당 타일 클릭을
    // 허용하는지 검사한다.
    //
    // 일반 시퀀스 상태에서는 기존 Battle 입력을 그대로 허용하고,
    // ForceTileSelect 중일 때만 지정 좌표 하나로 제한한다.
    public bool CanSelectTile(
        Tile tile)
    {
        if (isSequenceActive == false)
        {
            return true;
        }

        // <변경부분> Event Step이 넘어가는 짧은 시간에는
        // 보드 타일 입력 전체를 차단한다.
        //
        // 이전 Step에서 빠르게 두 번 클릭한 입력이
        // 다음 Battle 행동으로 이어지는 것을 방지한다.
        if (isStepTransitionInputLocked)
        {
            return false;
        }

        // Dialogue가 표시되는 동안에는
        // 보드의 모든 타일 입력을 막는다.
        if (IsDialogueBlockingBattleInput)
        {
            return false;
        }

        // <변경부분> ForceButton 중에는
        // 보드의 모든 Tile 입력을 차단한다.
        if (isWaitingForButtonInput)
        {
            return false;
        }

        // <변경부분> 현재 데보리아의 기물 클릭은
        // Piece 자체가 아니라 해당 기물이 올라가 있는 Tile 클릭을 통해
        // BattleManager.SelectTile() → SelectPiece() 순서로 처리된다.
        //
        // 따라서 ForcePieceSelect 중에는 모든 Tile을 막으면 안 되고,
        // 요구된 기물이 위치한 좌표의 Tile 하나만 통과시켜야 한다.
        //
        // 이후 BattleManager.SelectTile()이 해당 좌표의 Piece를 찾고
        // SelectPiece()를 호출하면,
        // CanSelectPiece()에서 Team + 좌표를 다시 최종 검증한다.
        if (isWaitingForPieceSelection)
        {
            if (tile == null)
            {
                return false;
            }

            return
                tile.X ==
                    requiredPiecePosition.x &&
                tile.Y ==
                    requiredPiecePosition.y;
        }

        // ForceTileSelect가 아니라면
        // 기존 Battle 타일 입력을 그대로 허용한다.
        if (isWaitingForTileSelection == false)
        {
            return true;
        }

        if (tile == null)
        {
            return false;
        }

        return
            tile.X ==
                requiredTilePosition.x &&
            tile.Y ==
                requiredTilePosition.y;
    }

    // <변경부분> 지정한 ForceTileSelect 타일이
    // 실제 BattleManager의 타일 처리 흐름까지 도달했을 때 호출한다.
    //
    // 첫 클릭 확인과 두 번째 클릭 행동 실행 모두
    // 현재 Step의 지정 좌표와 일치하면 완료할 수 있다.
    public void NotifyTileSelected(
        Tile tile)
    {
        if (isSequenceActive == false ||
            isWaitingForTileSelection == false ||
            tile == null)
        {
            return;
        }

        if (tile.X !=
                requiredTilePosition.x ||
            tile.Y !=
                requiredTilePosition.y)
        {
            return;
        }

        // <변경부분> 현재 ForceTileSelect 완료 직후
        // 다음 Event Step이 준비되기 전까지
        // 연속 타일 입력을 차단한다.
        isWaitingForTileSelection =
            false;

        isStepTransitionInputLocked =
            true;
    }

    // <변경부분> Battle UI가 실제 버튼 기능을 실행하기 전에
    // 현재 Event Sequence에서 해당 버튼 입력을
    // 허용하는지 검사한다.
    //
    // 일반 Sequence 상태에서는 기존 UI 입력을 그대로 허용하고,
    // ForceButton 중일 때만 지정한 버튼 하나로 제한한다.
    public bool CanPressButton(
        EventSequenceButtonType buttonType)
    {
        if (isSequenceActive == false)
        {
            return true;
        }

        // <변경부분> 이전 Force Step 완료 직후에는
        // 다음 Step이 준비될 때까지 모든 Battle UI 버튼을 막는다.
        //
        // 더블 클릭이나 빠른 연타가
        // 다음 Event Step의 버튼 입력으로 잘못 처리되는 것을 방지한다.
        if (isStepTransitionInputLocked)
        {
            return false;
        }

        // Dialogue 중에는 전투 UI 버튼을 모두 막는다.
        if (IsDialogueBlockingBattleInput)
        {
            return false;
        }

        // 기물 또는 타일 강제 입력 중에는
        // 전투 액션 버튼으로 우회하지 못하도록 막는다.
        if (isWaitingForPieceSelection ||
            isWaitingForTileSelection)
        {
            return false;
        }

        // ForceButton Step이 아니라면
        // 기존 버튼 입력은 그대로 허용한다.
        if (isWaitingForButtonInput == false)
        {
            return true;
        }

        if (buttonType ==
            EventSequenceButtonType.None)
        {
            return false;
        }

        // 현재 Step에서 지정한 버튼만 허용한다.
        return
            buttonType ==
            requiredButtonType;
    }

    // <변경부분> 지정된 버튼 클릭이 실제 UI 입력 흐름에서
    // 정상적으로 처리된 뒤 호출한다.
    //
    // 현재 ForceButton의 요구 버튼과 일치할 때만
    // 대기 상태를 해제하여 다음 Sequence Step으로 진행한다.
    public void NotifyButtonPressed(
     EventSequenceButtonType buttonType)
    {
        if (isSequenceActive == false ||
            isWaitingForButtonInput == false)
        {
            return;
        }

        if (buttonType !=
            requiredButtonType)
        {
            return;
        }

        Debug.Log(
            $"이벤트 버튼 입력 완료: " +
            $"{buttonType}"
        );

        // <변경부분> 현재 ForceButton 완료 직후
        // 다음 Event Step 준비 전까지
        // 더블 클릭 / 빠른 버튼 연타를 차단한다.
        isWaitingForButtonInput =
            false;

        requiredButtonType =
            EventSequenceButtonType.None;

        isStepTransitionInputLocked =
            true;
    }

    // <변경부분> Event Sequence의 모든 실행 상태를
    // Scene 시작 시 순수 런타임 기본값으로 초기화한다.
    //
    // Inspector나 이전 Play 상태에 의해
    // Sequence가 이미 실행 중인 것으로 판단되는 문제를 방지한다.
    private void Awake()
    {
        // <변경부분> Inspector 연결이 빠진 경우에도
        // Event Sequence의 BattleWin 완료 처리가 가능하도록
        // 현재 씬의 BattleManager를 자동으로 찾는다.
        if (battleManager == null)
        {
            battleManager =
                FindObjectOfType<BattleManager>();
        }

        isSequenceActive =
            false;

        currentStepIndex =
            -1;

        sequenceCoroutine =
            null;

        isWaitingForPieceSelection =
            false;

        requiredPiecePosition =
            Vector2Int.zero;

        requiredPieceTeam =
            PieceTeam.Player;

        isWaitingForTileSelection =
            false;

        requiredTilePosition =
            Vector2Int.zero;

        isWaitingForButtonInput =
    false;

        requiredButtonType =
            EventSequenceButtonType.None;

        // <변경부분> Scene 시작 시
        // Event Step 입력 잠금 상태를 명시적으로 초기화한다.
        //
        // 실제 Sequence가 시작되면
        // RunSequenceRoutine()에서 즉시 true로 전환한다.
        isStepTransitionInputLocked =
            false;
    }



    // <변경부분> 현재 연결된 EventSequenceData를 처음부터 실행한다.
    public void StartSequence()
    {
        // <변경부분> 자동 시작 또는 외부 호출이
        // 실제 StartSequence까지 도달했는지 확인한다.
        Debug.Log(
            "Event Sequence StartSequence() 진입"
        );

        if (sequenceData == null)
        {
            Debug.LogWarning(
                "이벤트 시퀀스 시작 실패: " +
                "EventSequenceData가 연결되지 않았습니다."
            );

            return;
        }

        if (sequenceData.IsValid() == false)
        {
            Debug.LogWarning(
                $"이벤트 시퀀스 시작 실패: " +
                $"{sequenceData.name} 데이터가 유효하지 않습니다."
            );

            return;
        }

        // 이미 실행 중이라면 중복 시작하지 않는다.
        if (isSequenceActive)
        {
            return;
        }

        StopSequenceCoroutine();

        sequenceCoroutine =
            StartCoroutine(
                RunSequenceRoutine()
            );
    }

    // <변경부분> 특정 EventSequenceData를 외부에서 전달받아
    // 즉시 실행할 수 있도록 하는 진입점
    //
    // 이후 컷씬이나 다른 이벤트에서
    // 같은 Controller를 재사용할 때 사용할 수 있다.
    public void StartSequence(
        EventSequenceData newSequenceData)
    {
        if (newSequenceData == null)
        {
            Debug.LogWarning(
                "이벤트 시퀀스 시작 실패: " +
                "전달된 EventSequenceData가 null입니다."
            );

            return;
        }

        sequenceData =
            newSequenceData;

        StartSequence();
    }

    // <변경부분> 실제 Step들을 순서대로 실행하는 메인 코루틴
    private IEnumerator RunSequenceRoutine()
    {
        isSequenceActive =
            true;

        currentStepIndex =
            -1;

        // <변경부분> Event Sequence가 시작되는 순간부터
        // 기본 Battle 입력을 잠근다.
        //
        // 튜토리얼 초반의
        // Dialogue → SpawnPiece → Born → 다음 Step
        // 구간에서는 아직 Force Step이 한 번도 완료되지 않았기 때문에
        // 기존 Step 전환 잠금이 활성화되지 않는 문제가 있었다.
        //
        // 이제 Sequence 전체는 기본 LOCK 상태로 시작하고,
        // 실제 ForcePiece / ForceTile / ForceButton이
        // 입력 준비를 끝낸 순간에만 잠금을 해제한다.
        isStepTransitionInputLocked =
            true;

        Debug.Log(
            $"이벤트 시퀀스 시작: " +
            $"{sequenceData.sequenceName}"
        );

        // <변경부분> 실제 Step 실행 직전에
        // 현재 직렬화된 Step 개수를 다시 확인한다.
        List<EventSequenceStepData> steps =
            sequenceData.steps;

        Debug.Log(
            $"이벤트 시퀀스 Step 실행 준비: " +
            $"{(steps != null ? steps.Count : -1)}개"
        );

        if (steps == null ||
            steps.Count == 0)
        {
            Debug.LogError(
                "이벤트 시퀀스 실행 실패: " +
                "실행할 Step이 없습니다."
            );

            isSequenceActive =
                false;

            yield break;
        }

        for (int i = 0;
             i < steps.Count;
             i++)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            EventSequenceStepData step =
    steps[i];

            currentStepIndex =
                i;

            // <변경부분> 새로운 Event Step이 시작될 때마다
            // Battle 입력을 우선 잠근 상태로 만든다.
            //
            // Dialogue / SpawnPiece / RemovePiece / Wait처럼
            // 플레이어 입력을 요구하지 않는 자동 Step 동안에는
            // 이 LOCK이 계속 유지된다.
            //
            // ForcePiece / ForceTile / ForceButton Step은
            // 각 함수 내부에서 목표 데이터 세팅이 완료된 뒤에만
            // isStepTransitionInputLocked = false로 바꾸므로
            // 정확히 입력 가능한 순간에만 조작이 허용된다.
            isStepTransitionInputLocked =
                true;

            if (step == null)
            {
                Debug.LogWarning(
                    $"이벤트 Step 건너뜀: " +
                    $"{i}번 데이터가 null입니다."
                );

                continue;
            }

            Debug.Log(
                $"이벤트 Step 시작: " +
                $"{i} / " +
                $"{step.stepName} / " +
                $"{step.stepType}"
            );

            yield return
                ExecuteStepRoutine(
                    step
                );

            // CompleteSequence Step이 실행되면
            // 내부에서 isSequenceActive가 false가 되므로 즉시 종료한다.
            if (isSequenceActive == false)
            {
                yield break;
            }
        }

        // 모든 Step을 끝까지 실행했다면
        // SequenceData의 완료 설정을 처리한다.
        CompleteSequence();
    }

    // <변경부분> Step Type에 따라 실제 기능을 분기한다.
    private IEnumerator ExecuteStepRoutine(
        EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        switch (step.stepType)
        {
            case EventSequenceStepType.None:
                yield break;

            case EventSequenceStepType.Dialogue:
                yield return
                    ExecuteDialogueStepRoutine(
                        step
                    );
                yield break;

            case EventSequenceStepType.SpawnPiece:
                // <변경부분> 이벤트 기물이 생성된 뒤
                // Born 애니메이션이 끝날 때까지 기다리고
                // 다음 Event Step으로 진행한다.
                yield return
                    ExecuteSpawnPieceStepRoutine(
                        step
                    );

                yield break;

            case EventSequenceStepType.RemovePiece:
                // <변경부분> 지정 좌표의 기물을
                // 이벤트 데이터 기준으로 제거한다.
                ExecuteRemovePieceStep(
                    step
                );
                yield break;

            case EventSequenceStepType.Wait:
                yield return
                    ExecuteWaitStepRoutine(
                        step
                    );
                yield break;

            // <변경부분>
            // 기존 Battle 승리 흐름을 통해
            // 리워드 팝업을 표시한다.
            //
            // CompleteSequence()는 호출하지 않으므로
            // 이후 Wait / Dialogue Step이 계속 실행된다.
            case EventSequenceStepType.ShowBattleRewardPopup:
                ExecuteShowBattleRewardPopupStep(
                    step
                );
                yield break;

            case EventSequenceStepType.CompleteSequence:
                CompleteSequence();
                yield break;

            case EventSequenceStepType.CommitPlayerPiecesToRunState:
                // <변경부분> 현재 시점의 Player 기물 상태를
                // RunState에 명시적으로 확정 저장한다.
                ExecuteCommitPlayerPiecesToRunStateStep(
                    step
                );

                yield break;

            case EventSequenceStepType.AdvanceBattleTurn:
                // <변경부분>
                // 진행 중인 전투 행동의 모든 후처리가 끝난 다음
                // BattleManager의 기존 EndTurn 파이프라인을 실행한다.
                //
                // 고정 Wait 시간이 아니라 실제 행동 상태를 기다린다.
                yield return
                    ExecuteAdvanceBattleTurnStepRoutine(
                        step
                    );

                yield break;

            case EventSequenceStepType.ExecutePieceAction:
                // <변경부분> 지정 좌표의 기물을 찾아
                // 기존 Battle 이동/공격 파이프라인으로 자동 행동시킨다.
                yield return
                    ExecutePieceActionStepRoutine(
                        step
                    );

                yield break;

            case EventSequenceStepType.ExecutePieceUniqueSkill:
                // 지정 좌표의 기물이
                // 현재 실제로 보유 중인 고유스킬을 자동 사용한다.
                yield return
                    ExecutePieceUniqueSkillStepRoutine(
                        step
                    );

                yield break;

            case EventSequenceStepType.SpeechBubble:
                // <변경부분>
                // 지정한 Battle Piece 위에 Event SpeechBubble을 표시한다.
                //
                // Wait For Complete 설정에 따라
                // 표시 종료까지 기다리거나 즉시 다음 Step으로 진행한다.
                yield return
                    ExecuteSpeechBubbleStepRoutine(
                        step
                    );

                yield break;

            case EventSequenceStepType.ForcePieceSelect:
                // <변경부분> 지정한 기물을 실제로 선택할 때까지
                // 현재 Step에서 대기한다.
                yield return
                    ExecuteForcePieceSelectStepRoutine(
                        step
                    );
                yield break;

            case EventSequenceStepType.ForceTileSelect:
                // <변경부분> 지정된 보드 타일이
                // 실제로 클릭될 때까지 현재 Step에서 대기한다.
                yield return
                    ExecuteForceTileSelectStepRoutine(
                        step
                    );
                yield break;

            case EventSequenceStepType.ForceButton:
                // <변경부분> 지정한 전투 UI 버튼이
                // 실제로 눌릴 때까지 현재 Step에서 대기한다.
                yield return
                    ExecuteForceButtonStepRoutine(
                        step
                    );
                yield break;

            // <변경부분>
            // EventScene과 동일한 독립 화면 흔들림 Step.
            case EventSequenceStepType.ScreenShake:
                yield return
                    ExecuteScreenShakeStepRoutine(
                        step
                    );
                yield break;

            // <변경부분>
            // 진행 중인 시간제 또는 지속 화면 흔들림을 종료한다.
            case EventSequenceStepType.StopScreenShake:
                StopCameraShakeImmediately();
                yield break;

            // <변경부분>
            // BattleScene 화면 글리치 연출..
            case EventSequenceStepType.ScreenGlitch:
                yield return
                    ExecuteScreenGlitchStepRoutine(
                        step
                    );
                yield break;

            // <변경부분>
            // BattleScene 조명 및 화면 플래시 연출.
            case EventSequenceStepType.GlobalLighting:
                yield return
                    ExecuteGlobalLightingStepRoutine(
                        step
                    );
                yield break;

            // <변경부분>
            // 전투 HUD의 Fade In / Fade Out을 실행한다.
            // Wait For Complete 옵션도 함께 처리한다.
            case EventSequenceStepType.SetBattleUIVisibility:
                yield return
                    ExecuteSetBattleUIVisibilityStepRoutine(
                        step
                    );
                yield break;
        }
    }

    // =====================================================
    // Screen Shake
    // =====================================================

    // <변경부분>
    // EventScene의 ScreenShake Step과 동일하게
    // 화면 흔들림을 실행한다.
    //
    // Wait For Complete가 true이면
    // 실제 Coroutine이 끝날 때까지 대기한다.
    private IEnumerator ExecuteScreenShakeStepRoutine(
       EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        StartCameraShake(
            step.screenShakeDuration,
            step.screenShakeStrength,
            step.screenShakeContinuous
        );

        // <변경부분>
        // 지속 모드는 종료 시점을 알 수 없으므로
        // 무조건 다음 Step으로 즉시 진행한다.
        if (step.screenShakeContinuous)
        {
            yield break;
        }

        // 기존 시간제 흔들림의 대기 설정은 유지한다.
        if (step.screenShakeWaitForComplete == false)
        {
            yield break;
        }

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
    // 실제 Camera Shake를 시작한다.
    //
    // 이미 흔들리고 있다면 기존 흔들림을 종료하고
    // 원래 위치로 복원한 다음 새 흔들림을 시작한다.
    private void StartCameraShake(
      float duration,
      float strength,
      bool continuous = false)
    {
        // <변경부분>
        // 지속 흔들림이 진행 중이면
        // 일반 시간제 흔들림이 이를 덮어쓰지 않는다.
        if (isContinuousCameraShakeActive &&
            !continuous)
        {
            return;
        }

        // 새 지속 흔들림을 시작하는 경우에는
        // 기존 흔들림을 복구한 뒤 새 설정을 적용한다.
        StopCameraShakeImmediately();

        Transform shakeTarget =
            cameraShakeTarget;

        // Inspector에 연결되지 않은 경우
        // 기존 EventScene처럼 Main Camera를 사용한다.
        if (shakeTarget == null &&
            Camera.main != null)
        {
            shakeTarget =
                Camera.main.transform;
        }

        if (shakeTarget == null)
        {
            Debug.LogWarning(
                "Battle Event ScreenShake 실행 실패: " +
                "Camera Shake Target 또는 Main Camera를 찾을 수 없습니다.",
                this
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

        // 지속 모드에서는 Duration = 0이어도 실행한다.
        if ((!continuous && safeDuration <= 0f) ||
            safeStrength <= 0f)
        {
            return;
        }

        activeCameraShakeTarget =
            shakeTarget;

        isContinuousCameraShakeActive =
            continuous;

        // 흔들리는 동안 현재 위치를 다시 기준으로 잡지 않는다.
        // 흔들림 누적으로 발생하는 Camera Drift를 방지한다.
        cameraShakeBaseLocalPosition =
            shakeTarget.localPosition;

        cameraShakeCoroutine =
     StartCoroutine(
         PlayCameraShakeRoutine(
             shakeTarget,
             cameraShakeBaseLocalPosition,
             safeDuration,
             safeStrength,
             continuous
         )
     );
    }


    // <변경부분>
    // EventScene과 동일한 Random X/Y Offset 방식.
    //
    // 시작 시점의 Local Position을 기준으로 흔들며,
    // 종료 후 반드시 정확한 위치로 복원한다.
    private IEnumerator PlayCameraShakeRoutine(
     Transform shakeTarget,
     Vector3 baseLocalPosition,
     float duration,
     float strength,
     bool continuous)
    {
        float elapsedTime =
            0f;

        // <변경부분>
        // 시간제 모드는 Duration 종료 시 복구.
        // 지속 모드는 StopCoroutine으로 중단할 때까지 반복.
        while (continuous || elapsedTime < duration)
        {
            if (shakeTarget == null)
            {
                cameraShakeCoroutine =
                    null;

                activeCameraShakeTarget =
                    null;

                isContinuousCameraShakeActive =
                    false;

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

        if (shakeTarget != null)
        {
            shakeTarget.localPosition =
                baseLocalPosition;
        }

        cameraShakeCoroutine =
        null;

        activeCameraShakeTarget =
            null;

        isContinuousCameraShakeActive =
            false;
    }


    // <변경부분>
    // 진행 중인 Camera Shake를 즉시 종료하고
    // 시작 전 Local Position으로 복원한다.
    //
    // Event 종료 / 중단 / Scene 비활성화에서도 호출한다.
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

        isContinuousCameraShakeActive =
            false;
    }


    // =====================================================
    // Screen Glitch
    // =====================================================

    // <변경부분>
    // 기존 EventScreenGlitchController를 재사용한다.
    private IEnumerator ExecuteScreenGlitchStepRoutine(
        EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (eventScreenGlitchController == null ||
            !eventScreenGlitchController.isActiveAndEnabled)
        {
            Debug.LogError(
                "Battle Event ScreenGlitch 실행 실패: " +
                "EventScreenGlitchController가 연결되지 않았거나 " +
                "비활성화되어 있습니다.",
                this
            );

            StopSequence();
            yield break;
        }

        eventScreenGlitchController.PlayGlitch(
            Mathf.Clamp01(step.screenGlitchStrength),
            Mathf.Max(0.05f, step.screenGlitchDuration)
        );

        if (!eventScreenGlitchController.IsPlaying)
        {
            Debug.LogError(
                "Battle Event ScreenGlitch 실행 실패: " +
                "글리치 효과가 시작되지 않았습니다. " +
                "Screen Overlay와 Glitch Material을 확인하세요.",
                this
            );

            StopSequence();
            yield break;
        }

        if (!step.screenGlitchWaitForComplete)
        {
            yield break;
        }

        while (eventScreenGlitchController != null &&
               eventScreenGlitchController.IsPlaying)
        {
            yield return null;
        }

        if (eventScreenGlitchController == null ||
            !eventScreenGlitchController.isActiveAndEnabled)
        {
            Debug.LogError(
                "Battle Event ScreenGlitch 대기 중단: " +
                "Controller가 사라지거나 비활성화되었습니다.",
                this
            );

            StopSequence();
        }
    }


    // =====================================================
    // Global Lighting
    // =====================================================

    // <변경부분>
    // Global Ambient와 Screen Flash를 독립적으로 실행한다.
    // 둘 다 활성화되어 있으면 동시에 시작한다.
    private IEnumerator ExecuteGlobalLightingStepRoutine(
        EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        Coroutine flashToWait = null;
        Coroutine lightingToWait = null;

        // <변경부분>
        // Character_Fill은 Global Ambient와 독립적으로 진행된다.
        Coroutine characterFillToWait = null;

        // Screen Flash는 Global Light 연결 여부와 무관하게 실행.
        if (step.globalLightingUseScreenFlash)
        {
            StartGlobalLightingFlash(
                step.globalLightingFlashColor,
                Mathf.Clamp01(step.globalLightingFlashStrength),
                Mathf.Max(0f, step.globalLightingFlashDuration)
            );

            flashToWait = globalLightingFlashCoroutine;
        }

        // Global Light 옵션이 OFF라면 현재 조명을 건드리지 않는다.
        if (step.globalLightingUseGlobalLight)
        {
            if (environmentGlobalAmbient == null)
            {
                Debug.LogWarning(
                    "Battle Event GlobalLighting 실패: " +
                    "Global Ambient Light2D가 연결되지 않았습니다.",
                    this
                );
            }
            else
            {
                // 최초 조명값은 한 번만 보존한다.
                if (!hasCapturedGlobalLightingBase)
                {
                    globalLightingBaseColor =
                        environmentGlobalAmbient.color;

                    globalLightingBaseIntensity =
                        environmentGlobalAmbient.intensity;

                    hasCapturedGlobalLightingBase = true;
                }

                // 진행 중인 페이드는 중단하고 현재값부터 다시 시작.
                if (globalLightingFadeCoroutine != null)
                {
                    StopCoroutine(globalLightingFadeCoroutine);
                    globalLightingFadeCoroutine = null;
                }

                float duration =
                    Mathf.Max(0f, step.globalLightingFadeDuration);

                Color targetColor =
                    step.globalLightingTargetColor;

                float targetIntensity =
                    Mathf.Max(0f, step.globalLightingTargetIntensity);

                if (duration <= 0f)
                {
                    environmentGlobalAmbient.color =
                        targetColor;

                    environmentGlobalAmbient.intensity =
                        targetIntensity;
                }
                else
                {
                    globalLightingFadeCoroutine =
                        StartCoroutine(
                            FadeGlobalLightingRoutine(
                                targetColor,
                                targetIntensity,
                                duration
                            )
                        );

                    lightingToWait = globalLightingFadeCoroutine;
                }
            }
        }

        // <변경부분>
        // Character_Fill 실행.
        // 아래 별도 공용 코드를 이 위치에 삽입한다.

        if (!step.globalLightingWaitForComplete)
        {
            yield break;
        }

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
    // Time Scale에 영향받지 않는 SmoothStep 조명 페이드.
    private IEnumerator FadeGlobalLightingRoutine(
        Color targetColor,
        float targetIntensity,
        float duration)
    {
        if (environmentGlobalAmbient == null)
        {
            globalLightingFadeCoroutine = null;
            yield break;
        }

        Color startColor =
            environmentGlobalAmbient.color;

        float startIntensity =
            environmentGlobalAmbient.intensity;

        float elapsed = 0f;

        while (elapsed < duration &&
               environmentGlobalAmbient != null)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            environmentGlobalAmbient.color =
                Color.Lerp(startColor, targetColor, easedT);

            environmentGlobalAmbient.intensity =
                Mathf.Lerp(
                    startIntensity,
                    targetIntensity,
                    easedT
                );

            yield return null;
        }

        if (environmentGlobalAmbient != null)
        {
            environmentGlobalAmbient.color =
                targetColor;

            environmentGlobalAmbient.intensity =
                targetIntensity;
        }

        globalLightingFadeCoroutine = null;
    }

    // =====================================================
    // Character Fill Fade
    // =====================================================

    // <변경부분>
    // Character_Fill 전용 조명 페이드.
    //
    // Global Ambient와 별도의 Coroutine으로 실행되며
    // Time Scale에 영향받지 않는다.
    private IEnumerator FadeCharacterFillRoutine(
        Color targetColor,
        float targetIntensity,
        float duration)
    {
        if (characterFillLight == null)
        {
            characterFillFadeCoroutine = null;
            yield break;
        }

        Color startColor =
            characterFillLight.color;

        float startIntensity =
            characterFillLight.intensity;

        float elapsed = 0f;

        while (elapsed < duration &&
               characterFillLight != null)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(elapsed / duration);

            float easedT =
                Mathf.SmoothStep(0f, 1f, t);

            characterFillLight.color =
                Color.Lerp(
                    startColor,
                    targetColor,
                    easedT
                );

            characterFillLight.intensity =
                Mathf.Lerp(
                    startIntensity,
                    targetIntensity,
                    easedT
                );

            yield return null;
        }

        // <변경부분>
        // 페이드 종료 시 목표값을 정확히 적용한다.
        if (characterFillLight != null)
        {
            characterFillLight.color =
                targetColor;

            characterFillLight.intensity =
                targetIntensity;
        }

        characterFillFadeCoroutine = null;
    }

    // =====================================================
    // Screen Flash
    // =====================================================

    // <변경부분>
    // 기존 EventScene과 동일한 Screen Space Overlay 방식.
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

        globalLightingFlashCanvasObject =
            new GameObject(
                "BattleEvent_GlobalLightingFlash",
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

        globalLightingFlashCoroutine =
            StartCoroutine(
                PlayGlobalLightingFlashRoutine(
                    flashColor,
                    strength,
                    duration
                )
            );
    }


    // <변경부분>
    // 처음 20%는 Fade In, 나머지 80%는 Fade Out.
    private IEnumerator PlayGlobalLightingFlashRoutine(
        Color flashColor,
        float strength,
        float duration)
    {
        if (duration <= 0.05f)
        {
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
                float t = Mathf.Clamp01(elapsed / duration);

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
    // 진행 중인 Flash와 Canvas를 즉시 정리한다.
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
    // 강제 종료 또는 컴포넌트 비활성화 시 최초 조명값 복구.
    private void RestoreGlobalLightingImmediately()
    {
        StopGlobalLightingFlashImmediately();

        if (globalLightingFadeCoroutine != null)
        {
            StopCoroutine(globalLightingFadeCoroutine);
            globalLightingFadeCoroutine = null;
        }

        if (hasCapturedGlobalLightingBase &&
            environmentGlobalAmbient != null)
        {
            environmentGlobalAmbient.color =
                globalLightingBaseColor;

            environmentGlobalAmbient.intensity =
                globalLightingBaseIntensity;
        }

        hasCapturedGlobalLightingBase = false;

        // <변경부분>
        // Character_Fill 페이드 중지.
        if (characterFillFadeCoroutine != null)
        {
            StopCoroutine(
                characterFillFadeCoroutine
            );

            characterFillFadeCoroutine = null;
        }

        // <변경부분>
        // 연출 시작 전 Character_Fill의 원래 값 복구.
        if (hasCapturedCharacterFillBase &&
            characterFillLight != null)
        {
            characterFillLight.color =
                characterFillBaseColor;

            characterFillLight.intensity =
                characterFillBaseIntensity;
        }

        hasCapturedCharacterFillBase = false;
    }


    // <변경부분>
    // Event Sequence 진행 중 기존 Battle 승리 흐름을 실행한다.
    //
    // 기존 BattleManager의 승리 처리 함수를 재사용하므로
    // 기물 상태 저장, 회복 기물 지급, 드롭 보상 계산,
    // 리워드 팝업 표시 방식은 변경하지 않는다.
    //
    // 중요:
    // CompleteSequence()를 호출하지 않는다.
    // 이 Step 이후의 Dialogue 등을 계속 실행하기 위해서다.
    private void ExecuteShowBattleRewardPopupStep(
        EventSequenceStepData step)
    {
        // <변경부분>
        // Inspector 참조가 없는 경우 기존 방식대로 자동 검색.
        if (battleManager == null)
        {
            battleManager =
                FindObjectOfType<BattleManager>();
        }

        if (battleManager == null)
        {
            Debug.LogError(
                "ShowBattleRewardPopup 실행 실패: " +
                "BattleManager가 연결되지 않았습니다. / " +
                $"{step.stepName}"
            );

            return;
        }

        // <변경부분>
        // 이미 전투가 종료됐다면 보상을 다시 지급하지 않는다.
        //
        // 일반 승패 판정이 먼저 실행되었거나
        // 해당 Step이 중복 배치된 상황을 방어한다.
        if (battleManager.IsBattleEnded)
        {
            Debug.LogWarning(
                "ShowBattleRewardPopup 중복 실행 방지: " +
                "이미 전투 종료 처리가 완료되었습니다. / " +
                $"{step.stepName}"
            );

            return;
        }

        // <변경부분>
        // 기존 Battle 승리 처리 파이프라인 실행.
        //
        // BattleManager
        // → BattleEndFlowController
        // → BattleRewardPopupUI
        //
        // 여기서 보상은 한 번만 지급된다.
        battleManager.CompleteBattleWinFromEvent();

        Debug.Log(
            "ShowBattleRewardPopup 실행 완료: " +
            "전투 승리 및 리워드 팝업 표시를 요청했습니다. " +
            "Event Sequence는 계속 진행합니다."
        );
    }


    // <변경부분>
    // Battle EventSequence의 SpeechBubble Step 실행.
    //
    // EventSequenceData에 저장된 Team + Board Position으로
    // 실제 Battle Piece를 찾은 뒤,
    // Piece가 이미 가지고 있는 ActorSpeechBubbleUI 공용 경로를 사용한다.
    //
    // 별도의 Screen Space 추적 UI를 만들지 않으며,
    // Piece 자식의 World Space SpeechBubbleCanvas가
    // Piece 이동 / Camera 이동 / WorldRoot Zoom을 그대로 따라간다.
    private IEnumerator ExecuteSpeechBubbleStepRoutine(
        EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (pieceManager == null)
        {
            Debug.LogWarning(
                $"Event SpeechBubble Step 실패: " +
                $"PieceManager가 연결되지 않았습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분>
        // 지정 좌표에 현재 존재하는 실제 Piece를 찾는다.
        Piece targetPiece =
            pieceManager.GetPieceAt(
                step.speechBubblePiecePosition.x,
                step.speechBubblePiecePosition.y
            );

        if (targetPiece == null)
        {
            Debug.LogWarning(
                $"Event SpeechBubble Step 실패: " +
                $"{step.speechBubblePiecePosition}에 " +
                $"기물이 없습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분>
        // 같은 좌표에 예상과 다른 진영의 기물이 들어온 경우
        // 잘못된 Actor에게 대사가 표시되지 않도록 차단한다.
        if (targetPiece.Team !=
            step.speechBubblePieceTeam)
        {
            Debug.LogWarning(
                $"Event SpeechBubble Step 실패: " +
                $"대상 Team이 일치하지 않습니다. / " +
                $"요청={step.speechBubblePieceTeam} / " +
                $"실제={targetPiece.Team} / " +
                $"위치={step.speechBubblePiecePosition} / " +
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
                $"Event SpeechBubble Step 건너뜀: " +
                $"표시할 문자열이 비어 있습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분>
        // <변경부분>
        // SpeechBubble Step에서 조정 가능한 표시 시간.
        float safeDuration =
            Mathf.Max(
                0f,
                step.speechBubbleDuration
            );

        // <변경부분>
        // SpeechBubble Step에서 조정 가능한 타이핑 속도.
        float safeTypingSpeed =
            Mathf.Max(
                0f,
                step.speechBubbleTypingSpeed
            );

        // <변경부분>
        // Text 강조 흔들림 세기는 음수가 되지 않도록 보정한다.
        float safeEmphasisStrength =
            Mathf.Max(
                0f,
                step.speechBubbleEmphasisStrength
            );

        if (step.speechBubbleWaitForComplete)
        {
            // <변경부분>
            // 말풍선 등장 → 타이핑 → 유지 → Fade Out이 끝날 때까지
            // 현재 Event Step에서 기다린다.
            yield return
                targetPiece
                    .PlaySpeechBubbleRoutine(
                        resolvedText,
                        safeDuration,
                        safeTypingSpeed,
                        step.speechBubbleUseEmphasisShake,
                        safeEmphasisStrength
                    );

            yield break;
        }

        // <변경부분>
        // SpeechBubble은 독립 Coroutine으로 계속 연출하고,
        // EventSequence는 즉시 다음 Step으로 진행한다.
        //
        // 서로 다른 Piece에 연속으로 Detached SpeechBubble을 실행하면
        // 여러 기물의 말풍선을 동시에 표시할 수 있다.
        targetPiece.PlaySpeechBubbleDetached(
            resolvedText,
            safeDuration,
            safeTypingSpeed,
            step.speechBubbleUseEmphasisShake,
            safeEmphasisStrength
        );
    }



    // <변경부분> ForcePieceSelect Step 실행
    //
    // 지정한 Team + 보드 좌표의 기물 외에는
    // BattleManager에서 선택을 허용하지 않는다.
    //
    // 올바른 기물을 실제로 선택했다는 통지를 받을 때까지
    // 이 Step은 완료되지 않는다.
    private IEnumerator ExecuteForcePieceSelectStepRoutine(
        EventSequenceStepData step)
    {
        requiredPiecePosition =
    step.targetPiecePosition;

        requiredPieceTeam =
            step.targetPieceTeam;

        isWaitingForPieceSelection =
            true;

        // <변경부분> 다음 ForcePieceSelect의
        // 대상 좌표와 Team, 대기 상태가 모두 준비됐으므로
        // 이제 플레이어 입력을 다시 허용한다.
        isStepTransitionInputLocked =
            false;

        // <변경부분> Step에서 Show Marker를 켠 경우
        // 현재 강제 선택 대상 기물을 자동으로 가리킨다.
        if (step.showMarker &&
            eventMarkerUI != null)
        {
            // <변경부분> ForcePieceSelect는
            // 기존 기물 좌표 + Team을 기준으로 마커를 표시한다.
            eventMarkerUI.ShowForPiece(
                requiredPiecePosition,
                requiredPieceTeam,
                step.markerDisplayType,
                step.markerPositionOffset
            );
        }

        Debug.Log(
                    $"이벤트 기물 선택 대기 시작: " +
            $"{requiredPieceTeam} / " +
            $"{requiredPiecePosition}"
        );

        while (isWaitingForPieceSelection)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }

        // <변경부분> 지정 타일 클릭 완료 후
        // World / UI Marker가 부드럽게 사라진 뒤
        // 다음 Event Step으로 진행한다.
        if (eventMarkerUI != null)
        {
            yield return
                eventMarkerUI
                    .HideWithFadeRoutine();
        }

        Debug.Log(
            $"이벤트 기물 선택 완료: " +
            $"{requiredPieceTeam} / " +
            $"{requiredPiecePosition}"
        );
    }

    // <변경부분> ForceTileSelect Step 실행
    //
    // 지정한 좌표 외의 타일 클릭과
    // 새로운 기물 선택을 모두 차단하고,
    // 올바른 타일 클릭이 들어올 때까지 현재 Step에서 대기한다.
    private IEnumerator ExecuteForceTileSelectStepRoutine(
        EventSequenceStepData step)
    {
        requiredTilePosition =
     step.targetTilePosition;

        isWaitingForTileSelection =
            true;

        // <변경부분> 다음 ForceTileSelect의
        // 목표 좌표와 입력 대기가 준비되었으므로
        // 타일 입력 잠금을 해제한다.
        isStepTransitionInputLocked =
            false;

        // <변경부분> Show Marker가 켜진 ForceTileSelect라면
        // 지정된 보드 타일을 자동으로 가리킨다.
        if (step.showMarker &&
    eventMarkerUI != null)
        {
            // <변경부분> 타일 마커도
            // 현재 Step의 표시 방식과 위치를 그대로 사용한다.
            eventMarkerUI.ShowForTile(
                requiredTilePosition,
                step.markerDisplayType,
                step.markerPositionOffset
            );
        }

        Debug.Log(
                    $"이벤트 타일 선택 대기 시작: " +
            $"{requiredTilePosition}"
        );

        while (isWaitingForTileSelection)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }

        // <변경부분> 지정 타일 클릭이 끝났으므로
        // 현재 타일 마커를 숨긴다.
        if (eventMarkerUI != null)
        {
            eventMarkerUI.Hide();
        }

        Debug.Log(
            $"이벤트 타일 선택 완료: " +
            $"{requiredTilePosition}"
        );
    }

    // <변경부분> ForceButton Step 실행
    //
    // EventSequenceStepData에서 지정한 버튼 하나만 허용하고,
    // 해당 버튼 입력이 실제 UI에서 처리될 때까지
    // 현재 Sequence Step에서 대기한다.
    private IEnumerator ExecuteForceButtonStepRoutine(
        EventSequenceStepData step)
    {
        if (step.targetButton ==
            EventSequenceButtonType.None)
        {
            Debug.LogWarning(
                $"ForceButton 실행 실패: " +
                $"{step.stepName}의 Target Button이 None입니다."
            );

            yield break;
        }

        requiredButtonType =
    step.targetButton;

        isWaitingForButtonInput =
            true;

        // <변경부분> 다음 ForceButton의 대상 버튼과
        // 대기 상태가 완전히 준비되었으므로
        // 버튼 입력 잠금을 해제한다.
        //
        // 이후에는 기존 CanPressButton()이
        // requiredButtonType 하나만 허용한다.
        isStepTransitionInputLocked =
            false;

        // <변경부분> 현재 ForceButton Step의 Show Marker가 켜져 있다면

        // <변경부분> 현재 ForceButton Step의 Show Marker가 켜져 있다면
        // 실제 버튼이 활성화될 때까지 잠시 기다리며 Marker 표시를 재시도한다.
        //
        // FieldAbsorbButton은 이전 공격 타일 선택 직후
        // 타입 아이콘 전환 연출을 거친 뒤 늦게 활성화될 수 있으므로,
        // Step 시작 프레임에 한 번만 찾고 포기하지 않는다.
        if (step.showMarker &&
            eventMarkerUI != null)
        {
            yield return
                ShowForceButtonMarkerWhenReadyRoutine(
                    step
                );
        }

        Debug.Log(
                    $"이벤트 버튼 입력 대기 시작: " +
            $"{requiredButtonType}"
        );

        while (isWaitingForButtonInput)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }

        // <변경부분> 지정 버튼 입력 완료 후
        // UI Marker Fade Out이 끝날 때까지 기다린 뒤
        // 다음 Step으로 넘어간다.
        if (eventMarkerUI != null)
        {
            yield return
                eventMarkerUI
                    .HideWithFadeRoutine();
        }

        Debug.Log(
            $"이벤트 버튼 입력 대기 종료: " +
            $"{step.targetButton}"
        );
    }

    // <변경부분> ForceButton 대상 UI가 실제로 활성화될 때까지
    // 짧은 시간 동안 Marker 표시를 반복 시도한다.
    //
    // FieldAbsorbButton은 공격 타일 선택 직후
    // 타입 아이콘 Fade 전환을 거친 뒤 표시되기 때문에
    // 다음 Event Step 시작 순간에는 아직 IsVisible == false일 수 있다.
    //
    // 일반 버튼은 첫 시도에서 바로 성공하고,
    // 늦게 나타나는 동적 UI만 잠시 기다린다.
    private IEnumerator ShowForceButtonMarkerWhenReadyRoutine(
        EventSequenceStepData step)
    {
        if (step == null ||
            eventMarkerUI == null)
        {
            yield break;
        }

        const float markerTargetWaitTimeout =
            1f;

        float elapsedTime =
            0f;

        while (elapsedTime <
               markerTargetWaitTimeout)
        {
            // <변경부분> 기다리는 사이 사용자가
            // 실제 버튼을 먼저 눌러 Step이 완료됐다면
            // 더 이상 Marker를 표시하지 않는다.
            if (isSequenceActive == false ||
                isWaitingForButtonInput == false)
            {
                yield break;
            }

            bool markerShown =
                eventMarkerUI.ShowForButton(
                    requiredButtonType,
                    step.markerDisplayType,
                    step.markerPositionOffset
                );

            if (markerShown)
            {
                // 실제 버튼을 찾아 Marker 표시가 성공했으므로 종료한다.
                yield break;
            }

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        Debug.LogWarning(
            $"ForceButton Marker 표시 실패: " +
            $"{requiredButtonType}의 실제 UI 대상을 " +
            $"{markerTargetWaitTimeout}초 동안 찾지 못했습니다."
        );
    }

    // <변경부분> Dialogue Step 실행
    //
    // 아직 EventGuideUI가 연결되지 않은 경우에는
    // 경고 후 해당 Step만 건너뛴다.
    private IEnumerator ExecuteDialogueStepRoutine(
      EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        // <변경부분>
        // EventSequenceStepData가 현재 Locale 기준으로
        // 최종 Dialogue Page 문자열을 Resolve한다.
        //
        // Localization이 연결되지 않은 기존 데이터는
        // 기존 한국어 dialoguePages를 fallback으로 반환한다.
        List<string> resolvedDialoguePages =
            step.GetLocalizedDialoguePages();

        if (resolvedDialoguePages == null ||
            resolvedDialoguePages.Count == 0)
        {
            yield break;
        }

        if (eventGuideUI == null)
        {
            Debug.LogWarning(
                $"Dialogue Step 실행 실패: " +
                $"EventGuideUI가 연결되지 않았습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // EventGuideUI는 Localization을 직접 알 필요 없이
        // 최종 문자열 목록만 받아 기존 방식대로 출력한다.
        yield return
            eventGuideUI.PlayDialogueRoutine(
                resolvedDialoguePages
            );
    }

    // <변경부분> Wait Step 실행
    //
    // 이벤트 / UI 연출은 Time.timeScale과 관계없이
    // 진행할 수 있도록 unscaledDeltaTime을 사용한다.
    private IEnumerator ExecuteWaitStepRoutine(
        EventSequenceStepData step)
    {
        float duration =
            Mathf.Max(
                0f,
                step.waitDuration
            );

        if (duration <= 0f)
        {
            yield break;
        }

        float elapsedTime =
            0f;

        while (elapsedTime <
               duration)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }

    // <변경부분> SpawnPiece Step 실행
    //
    // Player / Enemy / Neutral 모두 동일한
    // PieceManager.SpawnPieceFromData()를 사용한다.
    //
    // 생성 직후 기존 Piece Born 애니메이션을 재생하고,
    // Born이 끝난 뒤 다음 Event Step으로 진행한다.
    private IEnumerator ExecuteSpawnPieceStepRoutine(
        EventSequenceStepData step)
    {
        if (pieceManager == null)
        {
            Debug.LogWarning(
                "이벤트 기물 생성 실패: " +
                "PieceManager가 연결되지 않았습니다."
            );

            yield break;
        }

        if (step.spawnPieceData == null)
        {
            Debug.LogWarning(
                $"이벤트 기물 생성 실패: " +
                $"{step.stepName}의 PieceData가 없습니다."
            );

            yield break;
        }

        EventPieceSpawnOverrideData overrideData =
            step.spawnOverride;

        // 기본값은 PieceData의 설정을 그대로 사용한다.
        bool canMove =
            step.spawnPieceData.canMove;

        bool useAbsorbedPlayerVisual =
            false;

        if (overrideData != null)
        {
            if (overrideData.overrideCanMove)
            {
                canMove =
                    overrideData.canMove;
            }

            useAbsorbedPlayerVisual =
                overrideData
                    .useAbsorbedPlayerVisual;
        }

        Piece spawnedPiece =
            pieceManager.SpawnPieceFromData(
                step.spawnPieceData,
                step.spawnPieceTeam,
                step.spawnPosition.x,
                step.spawnPosition.y,
                canMove,
                useAbsorbedPlayerVisual
            );

        if (spawnedPiece == null)
        {
            Debug.LogWarning(
                $"이벤트 기물 생성 실패: " +
                $"{step.spawnPieceData.name} / " +
                $"{step.spawnPieceTeam} / " +
                $"{step.spawnPosition}"
            );

            yield break;
        }

        // <변경부분> 이벤트에서 고유스킬 Override를 사용하면
        // PieceData의 기본 고유스킬 대신 지정한 값을 적용한다.
        if (overrideData != null &&
            overrideData.overrideUniqueSkill)
        {
            spawnedPiece.SetUniqueSkillForEvent(
                overrideData.uniqueSkill
            );
        }

        // <변경부분> 이벤트용 일반스킬 Override 적용
        if (overrideData != null &&
            overrideData.overrideGeneralSkills)
        {
            spawnedPiece.ClearGeneralSkills();

            if (overrideData.generalSkills != null)
            {
                for (int i = 0;
                     i < overrideData.generalSkills.Count;
                     i++)
                {
                    GeneralSkillType skillType =
                        overrideData.generalSkills[i];

                    if (skillType ==
                        GeneralSkillType.None)
                    {
                        continue;
                    }

                    spawnedPiece.AddGeneralSkill(
                        skillType
                    );
                }
            }
        }

        // Override 적용 후
        // 외형과 상태 UI를 현재 데이터 기준으로 갱신한다.
        pieceManager.RefreshPieceVisual(
            spawnedPiece
        );

        Debug.Log(
            $"이벤트 기물 생성 완료: " +
            $"{step.spawnPieceTeam} / " +
            $"{step.spawnPieceData.pieceType} / " +
            $"{step.spawnPosition}"
        );

        // <변경부분> 이벤트에서 새로 등장한 기물이
        // 갑자기 화면에 나타나지 않도록
        // 기존 Piece Born 애니메이션을 재사용한다.
        yield return
            pieceManager.PlayPieceBornAnimation(
                spawnedPiece
            );

        Debug.Log(
            $"이벤트 기물 Born 완료: " +
            $"{step.spawnPieceTeam} / " +
            $"{step.spawnPieceData.pieceType} / " +
            $"{step.spawnPosition}"
        );
    }

    // <변경부분> RemovePiece Step 실행
    //
    // 지정한 보드 좌표에 존재하는 기물을 찾아 제거한다.
    // Player / Enemy / Neutral 모두 동일한 방식으로 처리한다.
    //
    // Check Remove Piece Team이 켜진 경우에는
    // 지정한 Team과 실제 기물의 Team이 일치할 때만 제거한다.
    private void ExecuteRemovePieceStep(
        EventSequenceStepData step)
    {
        if (pieceManager == null)
        {
            Debug.LogWarning(
                "이벤트 기물 제거 실패: " +
                "PieceManager가 연결되지 않았습니다."
            );

            return;
        }

        Vector2Int removePosition =
            step.removePiecePosition;

        Piece targetPiece =
            pieceManager.GetPieceAt(
                removePosition.x,
                removePosition.y
            );

        if (targetPiece == null)
        {
            Debug.LogWarning(
                $"이벤트 기물 제거 실패: " +
                $"{removePosition}에 기물이 없습니다."
            );

            return;
        }

        // Team 검사 옵션을 켠 경우에는
        // 지정한 진영의 기물인지 먼저 확인한다.
        if (step.checkRemovePieceTeam &&
            targetPiece.Team !=
                step.removePieceTeam)
        {
            Debug.LogWarning(
                $"이벤트 기물 제거 취소: " +
                $"{removePosition}의 기물은 " +
                $"{targetPiece.Team} 진영이며, " +
                $"설정된 제거 대상은 " +
                $"{step.removePieceTeam} 진영입니다."
            );

            return;
        }

        PieceTeam removedTeam =
            targetPiece.Team;

        PieceType removedPieceType =
            targetPiece.PieceType;

        // <변경부분> 기존 PieceManager의 공용 제거 함수를 사용하여
        // 보드 배열과 실제 Piece 오브젝트를 함께 정리한다.
        pieceManager.RemovePiece(
            targetPiece
        );

        Debug.Log(
            $"이벤트 기물 제거 완료: " +
            $"{removedTeam} / " +
            $"{removedPieceType} / " +
            $"{removePosition}"
        );
    }

    // <변경부분> 현재 Event Sequence 시점의
    // Player 기물 상태 전체를 RunState에 저장한다.
    //
    // SpawnPiece / 흡수 / 변형 등이 모두 끝난 뒤
    // 다음 전투에 실제로 전달하고 싶은 시점에 배치한다.
    private void ExecuteCommitPlayerPiecesToRunStateStep(
        EventSequenceStepData step)
    {
        if (battleManager == null)
        {
            Debug.LogWarning(
                $"Event Player 기물 저장 Step 실패: " +
                $"BattleManager가 연결되지 않았습니다. / " +
                $"{step?.stepName}"
            );

            return;
        }

        bool saved =
            battleManager
                .CommitPlayerPiecesToRunStateFromEvent();

        if (saved == false)
        {
            Debug.LogWarning(
                $"Event Player 기물 저장 Step 실패: " +
                $"{step?.stepName}"
            );

            return;
        }

        Debug.Log(
            $"Event Player 기물 저장 Step 완료: " +
            $"{step?.stepName}"
        );
    }

    // <변경부분>
    // 이전 전투 행동이 완전히 종료될 때까지 기다린 후
    // 기존 BattleManager의 정상 EndTurn 파이프라인을 실행한다.
    //
    // 턴 전환에 실패하면 후속 Event Step을 실행하지 않는다.
    private IEnumerator ExecuteAdvanceBattleTurnStepRoutine(
        EventSequenceStepData step)
    {
        if (battleManager == null)
        {
            Debug.LogError(
                $"Event 턴 전환 Step 실패: " +
                $"BattleManager가 연결되지 않았습니다. / " +
                $"{step?.stepName}"
            );

            StopSequence();
            yield break;
        }

        // <변경부분>
        // ForceTileSelect 등에서 실제 전투 행동을 시작한 직후
        // Event Step이 먼저 넘어오는 경우를 처리한다.
        if (battleManager.IsActionAnimating)
        {
            Debug.Log(
                $"Event 턴 전환 대기 시작: " +
                $"이전 전투 행동이 아직 진행 중입니다. / " +
                $"{step?.stepName}"
            );
        }

        while (battleManager != null &&
               battleManager.IsActionAnimating)
        {
            if (isSequenceActive == false ||
                battleManager.IsBattleEnded)
            {
                yield break;
            }

            yield return null;
        }

        if (isSequenceActive == false ||
            battleManager == null ||
            battleManager.IsBattleEnded)
        {
            yield break;
        }

        // <변경부분>
        // Event Sequence 설정은 AI 일시정지인데
        // BattleManager가 이를 인식하지 못한다면
        // 잘못된 Controller 참조가 연결되었을 수 있다.
        if (sequenceData != null &&
            sequenceData.pauseEnemyAIWhileSequenceActive &&
            battleManager.ShouldPauseEnemyAIForEvent == false)
        {
            Debug.LogError(
                "Event 턴 전환 실패: " +
                "BattleManager가 Event Sequence의 " +
                "Enemy AI 일시정지 상태를 인식하지 못합니다. " +
                "Inspector의 EventSequenceController 참조를 확인하세요."
            );

            StopSequence();
            yield break;
        }

        BattleTurn previousTurn =
            battleManager.CurrentTurn;

        bool advanced =
            battleManager.TryAdvanceBattleTurnFromEvent();

        if (advanced == false)
        {
            // <변경부분>
            // 실패한 턴 전환을 성공한 것으로 간주하지 않는다.
            Debug.LogError(
                $"Event 턴 전환 Step 실패: " +
                $"{step?.stepName} / " +
                $"현재 턴 {previousTurn}"
            );

            StopSequence();
            yield break;
        }

        Debug.Log(
            $"Event 턴 전환 Step 완료: " +
            $"{step?.stepName} / " +
            $"{previousTurn} -> " +
            $"{battleManager.CurrentTurn} / " +
            $"AI Pause={battleManager.ShouldPauseEnemyAIForEvent}"
        );
    }

    // <변경부분> EventSequence에 지정된 기물을 찾아
    // 지정 좌표로 자동 이동 또는 공격시킨다.
    //
    // EventSequenceController 내부에서
    // 이동 규칙이나 공격 규칙을 별도로 구현하지 않고,
    // BattleManager.TryExecuteBattleAction()을 그대로 사용하여
    // 일반 Battle / AI와 동일한 실행 파이프라인을 유지한다.
    private IEnumerator ExecutePieceActionStepRoutine(
        EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (pieceManager == null ||
      battleManager == null)
        {
            Debug.LogWarning(
                $"Event 기물 행동 Step 실패: " +
                $"PieceManager 또는 BattleManager가 연결되지 않았습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분>
        // 다른 이동 / 공격 / 흡수 연출이 진행 중이라면
        // 새 Event 전투 행동을 요청하기 전에 완료를 기다린다.
        //
        // 기물 조회 역시 대기 이후에 수행하여
        // 이전 행동으로 변경된 보드 상태를 기준으로 실행한다.
        if (battleManager.IsActionAnimating)
        {
            Debug.Log(
                $"Event 기물 행동 대기 시작: " +
                $"{step.stepName}"
            );
        }

        while (battleManager != null &&
               battleManager.IsActionAnimating)
        {
            if (isSequenceActive == false ||
                battleManager.IsBattleEnded)
            {
                yield break;
            }

            yield return null;
        }

        if (isSequenceActive == false ||
            battleManager == null ||
            battleManager.IsBattleEnded)
        {
            yield break;
        }

        // <변경부분> EventSequence 데이터에 지정된
        // 현재 보드 좌표에서 실제 행동 기물을 찾는다.
        Piece actingPiece =
            pieceManager.GetPieceAt(
                step.actionPiecePosition.x,
                step.actionPiecePosition.y
            );

        if (actingPiece == null)
        {
            Debug.LogWarning(
                $"Event 기물 행동 Step 실패: " +
                $"{step.actionPiecePosition}에 " +
                $"행동시킬 기물이 없습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분> 좌표에 다른 진영의 기물이 들어와 있을 경우
        // 잘못된 Tutorial 행동이 실행되지 않도록 Team까지 확인한다.
        if (actingPiece.Team !=
            step.actionPieceTeam)
        {
            Debug.LogWarning(
                $"Event 기물 행동 Step 실패: " +
                $"지정 Team={step.actionPieceTeam} / " +
                $"실제 Team={actingPiece.Team} / " +
                $"{step.actionPiecePosition} / " +
                $"{step.stepName}"
            );

            yield break;
        }

        Vector2Int sourcePosition =
            new Vector2Int(
                actingPiece.X,
                actingPiece.Y
            );

        // <변경부분> 일반 Battle과 AI가 사용하는
        // 기존 공용 행동 실행 함수를 그대로 사용한다.
        //
        // 목표가 빈칸이면 이동,
        // 목표에 적대 기물이 있으면 공격으로 자동 처리된다.
        bool actionStarted =
            battleManager.TryExecuteBattleAction(
                actingPiece,
                step.actionTargetPosition
            );

        if (actionStarted == false)
        {
            Debug.LogWarning(
                $"Event 기물 행동 Step 실패: " +
                $"{step.actionPieceTeam} / " +
                $"{sourcePosition} -> " +
                $"{step.actionTargetPosition} / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분>
        // BattleManager의 행동 코루틴이 시작된 상태를
        // 다음 프레임에서 확실하게 확인한다.
        //
        // StartCoroutine 직후 실행 타이밍 차이로
        // 아직 IsActionAnimating이 갱신되지 않은 경우를 방어한다.
        yield return null;

        // <변경부분> 이동 / 공격 / Defense / Insight /
        // ChanceAttack 등 기존 전투 행동 처리가 완전히 끝날 때까지
        // 다음 Event Step으로 넘어가지 않는다.
        while (battleManager != null &&
               battleManager.IsActionAnimating)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }

        Debug.Log(
            $"Event 기물 행동 Step 완료: " +
            $"{step.actionPieceTeam} / " +
            $"{sourcePosition} -> " +
            $"{step.actionTargetPosition} / " +
            $"{step.stepName}"
        );
    }

    // <변경부분> EventSequence에 지정된 기물이
    // 현재 실제로 보유 중인 고유스킬을 자동으로 사용한다.
    //
    // 스킬 효과나 사용 조건을 EventSequenceController에서
    // 다시 구현하지 않고,
    // 기존 BattleManager.TryExecuteAIUniqueSkill() 경로를 사용한다.
    private IEnumerator ExecutePieceUniqueSkillStepRoutine(
        EventSequenceStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        if (pieceManager == null ||
            battleManager == null)
        {
            Debug.LogWarning(
                $"Event 고유스킬 Step 실패: " +
                $"PieceManager 또는 BattleManager가 연결되지 않았습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분> EventSequence 데이터에 지정된 좌표에서
        // 실제 스킬 사용 기물을 찾는다.
        Piece skillPiece =
            pieceManager.GetPieceAt(
                step.uniqueSkillPiecePosition.x,
                step.uniqueSkillPiecePosition.y
            );

        if (skillPiece == null)
        {
            Debug.LogWarning(
                $"Event 고유스킬 Step 실패: " +
                $"{step.uniqueSkillPiecePosition}에 " +
                $"기물이 없습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분> 잘못된 좌표에 다른 진영의 기물이 들어와도
        // 의도하지 않은 스킬이 발동하지 않도록 Team을 검증한다.
        if (skillPiece.Team !=
            step.uniqueSkillPieceTeam)
        {
            Debug.LogWarning(
                $"Event 고유스킬 Step 실패: " +
                $"지정 Team={step.uniqueSkillPieceTeam} / " +
                $"실제 Team={skillPiece.Team} / " +
                $"{step.uniqueSkillPiecePosition} / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // 고유스킬 자체가 없는 기물은 실행할 수 없다.
        if (skillPiece.UniqueSkill ==
            UniqueSkillType.None)
        {
            Debug.LogWarning(
                $"Event 고유스킬 Step 실패: " +
                $"{step.uniqueSkillPiecePosition}의 " +
                $"{skillPiece.PieceType} 기물이 " +
                $"고유스킬을 보유하고 있지 않습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        UniqueSkillType skillType =
            skillPiece.UniqueSkill;

        // <변경부분> 기물이 현재 실제로 가지고 있는
        // UniqueSkill을 그대로 BattleAIAction에 담는다.
        BattleAIAction skillAction =
            BattleAIAction.CreateUniqueSkill(
                skillPiece
            );

        if (skillAction == null)
        {
            Debug.LogWarning(
                $"Event 고유스킬 Step 실패: " +
                $"고유스킬 행동 데이터를 생성하지 못했습니다. / " +
                $"{skillType} / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분> 기존 AI 고유스킬 실행 진입점을 그대로 사용하여
        // 현재 턴 / 쿨타임 / 턴당 사용 제한 /
        // 사망 스택 / 실제 Skill Effect 조건을 모두 검증한다.
        bool skillStarted =
            battleManager.TryExecuteAIUniqueSkill(
                skillAction
            );

        if (skillStarted == false)
        {
            Debug.LogWarning(
                $"Event 고유스킬 Step 실패: " +
                $"{step.uniqueSkillPieceTeam} / " +
                $"{step.uniqueSkillPiecePosition} / " +
                $"{skillType} / " +
                $"현재 스킬 사용 조건을 만족하지 못했습니다. / " +
                $"{step.stepName}"
            );

            yield break;
        }

        // <변경부분> 스킬 아이콘 연출과 실제 스킬 효과,
        // 쿨타임 및 후처리가 모두 끝날 때까지 기다린다.
        while (battleManager != null &&
               battleManager.IsActionAnimating)
        {
            if (isSequenceActive == false)
            {
                yield break;
            }

            yield return null;
        }

        Debug.Log(
            $"Event 고유스킬 Step 완료: " +
            $"{step.uniqueSkillPieceTeam} / " +
            $"{step.uniqueSkillPiecePosition} / " +
            $"{skillType} / " +
            $"{step.stepName}"
        );
    }

    // <변경부분> 현재 시퀀스를 정상 완료 처리한다.
    public void CompleteSequence()
    {
        if (isSequenceActive == false)
        {
            return;
        }

        isSequenceActive =
    false;

        // <변경부분> Event Sequence가 정상 종료되면
        // 남아 있던 Step 전환 입력 잠금도 해제한다.
        isStepTransitionInputLocked =
            false;

        // <변경부분>
        // 이벤트에서 숨기거나 표시했던 전투 UI를
        // 이벤트 실행 전 상태로 복구한다.
        RestoreBattleUIVisibilityImmediately();

        // <변경부분>
        // 화면 흔들림 도중 시퀀스가 완료되어도
        // 카메라가 흔들린 좌표에 남지 않도록 복원한다.
        StopCameraShakeImmediately();

        Debug.Log(
            $"이벤트 시퀀스 완료: " +
            $"{sequenceData?.sequenceName}"
        );

        HandleSequenceCompletion();
    }

    // <변경부분> Event Sequence에서 Scene을 변경하기 전에
    // 전역 시간 배율을 반드시 정상 상태로 복구한다.
    //
    // 마지막 보스 공격의 슬로우 모션 도중
    // Sequence가 먼저 완료되어 Scene이 변경되면,
    // PixelCameraController의 정상 복구 코루틴이 끝나기 전에
    // 기존 Scene이 제거될 수 있다.
    //
    // Time.timeScale과 Time.fixedDeltaTime은
    // Scene이 변경되어도 전역 값으로 유지되므로
    // 다음 Scene 전체가 느려지는 현상을 방지하기 위해
    // Scene Load 직전에 강제로 정상화한다.
    private void RestoreGlobalTimeScaleBeforeSceneChange()
    {
        Time.timeScale =
            1f;

        Time.fixedDeltaTime =
            0.02f;

        Debug.Log(
            "Event Sequence Scene 전환 준비: " +
            "TimeScale / FixedDeltaTime을 정상 상태로 복구했습니다."
        );
    }


    // <변경부분> EventSequenceData의 Completion 설정에 따라
    // 시퀀스 종료 후 이동 경로를 처리한다.
    private void HandleSequenceCompletion()
    {
        if (sequenceData == null)
        {
            return;
        }

        switch (sequenceData.completionType)
        {
            case EventSequenceCompletionType.None:
                return;

            case EventSequenceCompletionType.WorldMap:

                // <변경부분> 마지막 공격 슬로우 모션이
                // 아직 복구되지 않은 상태에서도
                // 다음 Scene이 정상 속도로 시작하도록 한다.
                RestoreGlobalTimeScaleBeforeSceneChange();

                WorldMapEventRuntimeState.Queue(sequenceData.completionWorldMapEventData);
                SceneManager.LoadScene(
                    "WorldMapScene"
                );

                return;

            case EventSequenceCompletionType.LoadScene:
                if (string.IsNullOrWhiteSpace(
                        sequenceData
                            .completionSceneName))
                {
                    Debug.LogWarning(
                        "이벤트 완료 씬 이동 실패: " +
                        "completionSceneName이 비어 있습니다."
                    );

                    return;
                }

                // <변경부분> EventSequenceData에
                // 다음 TextCutsceneData가 지정되어 있는 경우에만
                // Scene 이동 직전에 Pending Cutscene Data로 등록한다.
                //
                // null이면 아무것도 하지 않으므로
                // 기존 EventSequenceData와 일반 Scene 이동에는
                // 전혀 영향을 주지 않는다.
                if (sequenceData.completionCutsceneData != null)
                {
                    TextCutsceneRuntimeState
                        .SetPendingCutsceneData(
                            sequenceData.completionCutsceneData
                        );

                    Debug.Log(
                        $"이벤트 완료 컷씬 데이터 전달: " +
                        $"{sequenceData.completionCutsceneData.name}"
                    );
                }

                // <변경부분> CutsceneData 등록이 필요한 경우
                // 반드시 등록을 먼저 끝낸 뒤 Scene을 이동한다.

                // <변경부분> 보스 마지막 공격의 슬로우 모션이
                // 아직 진행 중이어도 다음 Scene에는
                // 변경된 TimeScale이 전달되지 않도록
                // Scene Load 직전에 전역 시간을 정상화한다.
                RestoreGlobalTimeScaleBeforeSceneChange();

                WorldMapEventRuntimeState.Queue(sequenceData.completionWorldMapEventData);
                SceneManager.LoadScene(
    sequenceData
        .completionSceneName
);

                return;

            // =====================================================
            // EventScene
            // =====================================================

            case EventSequenceCompletionType.EventScene:
                {
                    // <변경부분>
                    // EventSequenceData에서 선택한 EventSceneData를 사용한다.
                    EventSceneData nextEventSceneData =
                        sequenceData.completionEventSceneData;

                    // 필수 데이터 검증.
                    if (nextEventSceneData == null)
                    {
                        Debug.LogWarning(
                            "이벤트 완료 EventScene 이동 실패: " +
                            "Completion Event Scene Data가 없습니다.",
                            this
                        );

                        return;
                    }

                    if (!nextEventSceneData.IsValid())
                    {
                        Debug.LogWarning(
                            "이벤트 완료 EventScene 이동 실패: " +
                            "지정한 EventSceneData가 유효하지 않습니다. / " +
                            nextEventSceneData.name,
                            this
                        );

                        return;
                    }

                    // <변경부분>
                    // 공용 EventScene의 실제 Unity Scene 이름.
                    const string eventSceneName =
                        "EventScene";

                    // 씬이 Build Profiles의 Scene List에
                    // 등록되어 있는지 확인한다.
                    if (!Application.CanStreamedLevelBeLoaded(
                            eventSceneName))
                    {
                        Debug.LogWarning(
                            "이벤트 완료 EventScene 이동 실패: " +
                            "EventScene이 Build Profiles의 " +
                            "Scene List에 등록되어 있지 않습니다.",
                            this
                        );

                        return;
                    }

                    // <변경부분>
                    // Scene 로드 전에 실행할 EventSceneData를 전달한다.
                    //
                    // 기존 WorldMap 노드 및 Battle 상태는 변경하지 않는다.
                    if (!WorldMapRuntimeState
                            .SetPendingEventSceneDataFromBattleEvent(
                                nextEventSceneData))
                    {
                        Debug.LogWarning(
                            "이벤트 완료 EventScene 이동 실패: " +
                            "EventSceneData 전달에 실패했습니다.",
                            this
                        );

                        return;
                    }

                    // <변경부분>
                    // 기존 Scene 이동과 동일하게
                    // 슬로우 모션이 다음 Scene에 남지 않게 복구한다.
                    RestoreGlobalTimeScaleBeforeSceneChange();

                    Debug.Log(
                        "Battle Event 완료 - EventScene 이동: " +
                        nextEventSceneData.name,
                        this
                    );

                    SceneManager.LoadScene(
                        eventSceneName
                    );

                    return;
                }

            case EventSequenceCompletionType.BattleWin:
                WorldMapEventRuntimeState.Queue(sequenceData.completionWorldMapEventData);
                // <변경부분> Event Sequence가 정상 완료되면
                // 일반 Battle의 승리 처리 흐름으로 연결한다.
                //
                // 이 경로를 사용하면 기존 전투와 동일하게:
                // 플레이어 기물 상태 저장
                // → BattleEndFlowController
                // → StageBattleData 보상 정산
                // → BattleRewardPopupUI 표시
                // 순서로 진행된다.
                if (battleManager == null)
                {
                    Debug.LogWarning(
                        "이벤트 BattleWin 완료 실패: " +
                        "BattleManager가 연결되지 않았습니다."
                    );

                    return;
                }

                battleManager
                    .CompleteBattleWinFromEvent();

                Debug.Log(
                    "이벤트 BattleWin 완료: " +
                    "일반 전투 승리 / 보상 흐름으로 전달했습니다."
                );

                return;
        }
    }

    // <변경부분> 현재 실행 중인 이벤트를 강제로 정지한다.
    //
    // 씬 테스트 또는 외부 이벤트 취소에 사용할 수 있다.
    public void StopSequence()
    {
        isSequenceActive =
            false;

        // <변경부분> 진행 중이던 강제 입력 대기 상태도
        // 시퀀스 종료와 함께 즉시 해제한다.
        isWaitingForPieceSelection =
    false;

        requiredPiecePosition =
            Vector2Int.zero;

        requiredPieceTeam =
            PieceTeam.Player;

        // <변경부분> 진행 중이던
        // ForceTileSelect 상태도 함께 초기화한다.
        isWaitingForTileSelection =
    false;

        requiredTilePosition =
     Vector2Int.zero;

        // <변경부분> 진행 중이던 ForceButton 입력 제한도
        // Sequence 종료와 함께 즉시 해제한다.
        isWaitingForButtonInput =
    false;

        requiredButtonType =
    EventSequenceButtonType.None;

        // <변경부분> 이벤트를 강제로 종료하는 경우에도
        // Step 전환 입력 잠금이 남지 않도록 초기화한다.
        isStepTransitionInputLocked =
            false;

        StopSequenceCoroutine();

        // <변경부분>
        // 강제 종료 시 화면 흔들림도 종료하고
        // Camera Transform을 원래 위치로 복구한다.
        StopCameraShakeImmediately();

        // <변경부분>
        // 강제 종료 시 글리치가 화면에 남지 않도록 한다.
        if (eventScreenGlitchController != null)
        {
            eventScreenGlitchController.StopGlitch();
        }

        // <변경부분>
        // 화면 Flash 제거 및 최초 Global Light 값 복구.
        RestoreGlobalLightingImmediately();

        // <변경부분>
        // 이벤트 강제 종료 시 두 전투 UI의 상태도 복구한다.
        RestoreBattleUIVisibilityImmediately();

        currentStepIndex =
            -1;

        if (eventGuideUI != null)
        {
            eventGuideUI.HideImmediately();
        }

        // <변경부분> 이벤트가 중간에 종료되더라도
        // 현재 표시 중이던 기물 / 타일 / 버튼 마커를 즉시 제거한다.
        if (eventMarkerUI != null)
        {
            eventMarkerUI.Hide();
        }

        Debug.Log(
                    "이벤트 시퀀스 강제 종료"
        );
    }

    // 현재 실행 중인 메인 코루틴 정리
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
        // 씬 전환 또는 Controller 비활성화 시
        // 흔들린 카메라 좌표가 남지 않도록 복구한다.
        StopCameraShakeImmediately();

        // <변경부분>
        // 씬 전환 및 비활성화 시 화면 연출을 정리한다.
        if (eventScreenGlitchController != null)
        {
            eventScreenGlitchController.StopGlitch();
        }

        RestoreGlobalLightingImmediately();

        // <변경부분>
        // Scene 전환 또는 Controller 비활성화 시
        // 남아 있는 Battle UI 숨김 상태를 정리한다.
        RestoreBattleUIVisibilityImmediately();

        isSequenceActive =
            false;

        // <변경부분> 컴포넌트 비활성화 시에도
        // 이벤트 입력 제한 상태가 남지 않도록 초기화한다.
        isWaitingForPieceSelection =
     false;

        requiredPiecePosition =
            Vector2Int.zero;

        requiredPieceTeam =
            PieceTeam.Player;

        // <변경부분> 컴포넌트가 꺼지는 경우에도
        // 강제 타일 입력 상태가 남지 않도록 초기화한다.
        isWaitingForTileSelection =
            false;

        requiredTilePosition =
            Vector2Int.zero;

        // <변경부분> 컴포넌트 비활성화 시에도
        // ForceButton 입력 제한 상태가 남지 않도록 초기화한다.
        isWaitingForButtonInput =
            false;

        requiredButtonType =
    EventSequenceButtonType.None;

        // <변경부분> Controller 비활성화 시에도
        // Step 전환 입력 잠금 상태를 완전히 초기화한다.
        isStepTransitionInputLocked =
            false;

        // <변경부분> 씬 종료 또는 Controller 비활성화 시
        // 마커가 화면에 남지 않도록 함께 정리한다.
        if (eventMarkerUI != null)
        {
            eventMarkerUI.Hide();
        }

        // <변경부분> Scene이 예상하지 못한 경로로 종료되더라도
        // 마지막 공격 슬로우 모션의 전역 TimeScale이
        // 다음 Scene까지 남지 않도록 최종 안전 복구한다.
        //
        // 일반적인 정상 종료에서는 이미
        // HandleSequenceCompletion()에서 한 번 복구되므로
        // 같은 값을 다시 적용해도 부작용은 없다.
        RestoreGlobalTimeScaleBeforeSceneChange();
    }
}