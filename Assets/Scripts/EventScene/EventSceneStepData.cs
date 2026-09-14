using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

// <변경부분>
// Event Scene 전용 Sequence에서 사용할 Step 종류.
//
// 기존 EventSequenceStepType은
// Battle / Tutorial 전용으로 그대로 유지하고,
// Event Scene 연출은 이 Enum만 사용한다.
//
// 중요:
// 기존 Asset의 Serialized 값 보호를 위해
// 이후 새로운 Step을 추가할 때는
// 기존 순서를 변경하지 말고 항상 마지막에 추가한다.
public enum EventSceneStepType
{
    None = 0,

    // EventGuideUI를 사용하여 Dialogue를 출력한다.
    Dialogue = 1,

    // 지정된 시간 동안 다음 Step으로 진행하지 않는다.
    Wait = 2,

    // BackgroundTile 좌표에 Event Actor를 생성한다.
    SpawnActor = 3,

    // 지정된 Actor를 다른 BackgroundTile 좌표로 이동시킨다.
    MoveActor = 4,

    // Actor가 다른 Actor를 공격하는 연출을 실행한다.
    AttackActor = 5,

    // Actor 사이의 흡수 연출을 실행한다.
    AbsorbActor = 6,

    // 지정 Actor의 Spine Animation Clip을 실행한다.
    PlayActorAnimation = 7,

    // 지정 Actor 위에 Text / Emotion 말풍선을 표시한다.
    SpeechBubble = 8,

    // 지정 Actor를 Event Scene에서 제거한다.
    RemoveActor = 9,

    // 카메라 위치 이동과 Zoom을 실행한다.
    CameraShot = 10,

    // 지정된 시간 동안 화면 흔들림을 실행한다.
    ScreenShake = 11,

    // 현재 Event Scene Sequence를 즉시 완료한다.
    CompleteSequence = 12
}


// <변경부분>
// RemoveActor Step에서 Actor를 제거하는 방식을 정의한다.
//
// FadeOut을 0번으로 두어
// 새 필드가 추가된 기존 EventSceneData에서도
// 기본적으로 즉시 사라지는 것보다 안전한 FadeOut을 사용한다.
public enum EventSceneRemoveMode
{
    FadeOut = 0,
    Immediate = 1
}

// <변경부분>
// AttackActor Step의 연출 결과.
//
// 실제 Battle 공격 성공 여부와는 관계없이
// Event Scene 제작자가 공격 연출 결과를 직접 지정한다.
//
// 기존 Serialized 값 보호를 위해
// 이후 값을 추가할 경우 기존 순서를 변경하지 않는다.
public enum EventSceneAttackResult
{
    Success = 0,
    Failure = 1
}

// <변경부분>
// Event Scene Dialogue 한 Page의 Localization Metadata.
//
// 실제 한국어 원문은 dialoguePages에 유지하고,
// 이 데이터는 Stable Localization ID와
// String Table 참조만 담당한다.
[Serializable]
public class EventSceneDialoguePageLocalizationData
{
    [HideInInspector]
    public string localizationId;

    [HideInInspector]
    public LocalizedString localizedText =
        new LocalizedString();
}


// <변경부분>
// Event Scene Sequence의 Step 하나를 정의한다.
//
// 현재 1단계에서는:
// Dialogue
// Wait
// CompleteSequence
//
// 실행에 필요한 데이터만 먼저 만든다.
//
// Spawn / Move / Attack / Camera 등의 세부 데이터는
// 각 기능을 실제 구현하는 단계에서 하나씩 추가한다.
[Serializable]
public class EventSceneStepData
{
    [Header("Basic")]

    // Inspector에서 Step 용도를 확인하기 위한 제작자용 이름.
    public string stepName;

    // 현재 Step에서 실행할 Event Scene 연출 종류.
    public EventSceneStepType stepType =
        EventSceneStepType.None;


    [Header("Dialogue")]

    // <변경부분>
    // Dialogue Step의 한국어 원문.
    //
    // 기존 EventSequence와 동일하게
    // 한국어 authoring 원문은 Data에 유지하고,
    // 현재 Locale 문자열은 Localization Table에서 가져온다.
    [HideInInspector]
    [TextArea(2, 6)]
    public List<string> dialoguePages =
        new List<string>();

    // <변경부분>
    // Dialogue Step 자체의 Stable Localization ID.
    //
    // Step 순서를 변경하더라도
    // 기존 Localization Key가 유지되도록 사용한다.
    [HideInInspector]
    public string dialogueLocalizationId;

    // <변경부분>
    // dialoguePages와 동일한 순서로 대응하는
    // Page별 Localization Metadata.
    [HideInInspector]
    public List<EventSceneDialoguePageLocalizationData>
        dialogueLocalizationPages =
            new List<EventSceneDialoguePageLocalizationData>();


    [Header("Spawn Actor")]

    // <변경부분>
    // 이후 Move / Attack / Absorb / Animation / Remove Step에서
    // 이 Actor를 찾기 위한 Event Scene 내부 고유 ID.
    //
    // 예:
    // jellu_01
    // scientist_01
    // devorya_king
    public string spawnActorId;

    public PieceData spawnActorPieceData;

    // <변경부분>
    // PieceData에서 어떤 진영의 Visual Prefab을 사용할지 지정한다.
    //
    // 실제 Battle Team 상태를 생성하는 용도가 아니라
    // Event Scene의 외형 선택 용도다.
    public PieceTeam spawnActorTeam =
        PieceTeam.Neutral;

    // <변경부분>
    // Player 진영으로 생성할 때
    // 흡수된 Player 외형을 사용할지 여부.
    public bool spawnActorUseAbsorbedPlayerVisual =
    false;

    // <변경부분>
    // Event Actor Visual의 로컬 위치 보정값.
    //
    // PieceData의 Visual Prefab Pivot이 발 중앙이 아닌 경우
    // Battle Prefab 자체를 수정하지 않고 Event Scene에서만 보정한다.
    public Vector2 spawnActorVisualOffset =
        Vector2.zero;

    // <변경부분>
    // PieceData Visual Prefab의 기본 방향을 기준으로
    // X축을 반전할지 여부.
    //
    // Event Scene에서는 같은 Actor도 상황에 따라
    // 좌 / 우 방향을 다르게 바라보게 할 수 있다.
    public bool spawnActorFlipX =
    false;

    // <변경부분>
    // SpawnActor에 Born Animation이 없을 때 사용할
    // fallback Fade In 시간.
    //
    // Born Clip이 존재하면 이 값은 사용하지 않고
    // Born Animation을 우선 재생한다.
    //
    // 0이면 Born이 없는 Actor도 즉시 표시한다.
    [Min(0f)]
    public float spawnActorFadeInDuration =
        0.35f;

    public Vector2Int spawnActorPosition =
     Vector2Int.zero;


    [Header("Move Actor")]

    // <변경부분>
    // 이동시킬 Event Actor의 고유 ID.
    //
    // 이전 SpawnActor에서 생성한 Actor ID를 사용한다.
    public string moveActorId;

    // <변경부분>
    // Actor가 이동할 BackgroundTile 좌표.
    //
    // 현재 Actor 좌표와 동일한 좌표를 지정하면
    // 다른 타일로 이동하지 않고 제자리에서 위로 뛰었다 내려온다.
    public Vector2Int moveActorDestination =
        Vector2Int.zero;

    // <변경부분>
    // 포물선 이동 또는 제자리 점프에 걸리는 전체 시간.
    [Min(0f)]
    public float moveActorDuration =
        0.6f;

    // 포물선의 최대 높이.
    //
    // 다른 타일 이동과 제자리 점프 모두 동일하게 사용한다.
    [Min(0f)]
    public float moveActorArcHeight =
        0.5f;

    // <변경부분>
    // 이 MoveActor Step에서 Actor의 Flip X 상태를
    // 새로 지정할지 여부.
    //
    // false이면 현재 Actor가 가지고 있는 방향을 그대로 유지한다.
    public bool moveActorChangeFlipX =
        false;

    // <변경부분>
    // moveActorChangeFlipX가 true일 때 적용할 Flip X 값.
    //
    // false = 기본 방향
    // true = X축 반전
    public bool moveActorFlipX =
        false;


    [Header("Attack Actor")]

    // <변경부분>
    // 공격 연출을 실행할 Actor ID.
    public string attackActorId;

    // <변경부분>
    // 공격 대상 Actor ID.
    public string attackTargetActorId;

    // <변경부분>
    // Event Scene에서 보여줄 공격 결과.
    //
    // Battle 판정과는 연결하지 않는다.
    public EventSceneAttackResult attackResult =
        EventSceneAttackResult.Success;

    // <변경부분>
    // 공격자가 Target 쪽으로 접근하는 데 걸리는 시간.
    [Min(0f)]
    public float attackApproachDuration =
        0.22f;

    // <변경부분>
    // 기존 초기 AttackActor의 단순 원위치 복귀 시간.
    //
    // 현재 Failure는 Defense Bounce 구조를 사용하므로
    // Runtime에서는 더 이상 사용하지 않는다.
    // 기존 EventSceneData Serialized 값 보호를 위해 필드는 유지한다.
    [HideInInspector]
    [Min(0f)]
    public float attackReturnDuration =
        0.22f;

    // <변경부분>
    // Failure일 때 공격자가 Target까지 완전히 도달하기 전에
    // 방어에 막히는 충돌 지점의 비율.
    //
    // Success에서는 사용하지 않고
    // 공격자가 Target 위치까지 완전히 이동한다.
    [Range(0f, 1f)]
    public float attackApproachRatio =
        0.78f;

    // 공격 접근 포물선 높이.
    [Min(0f)]
    public float attackArcHeight =
        0.18f;

    // <변경부분>
    // 초기 Failure 빗나감 연출용 Legacy 값.
    //
    // 현재 Failure는 옆으로 빗나가지 않고
    // Defense처럼 충돌 후 튕겨나가므로 Runtime에서는 사용하지 않는다.
    [HideInInspector]
    [Min(0f)]
    public float attackFailureMissOffset =
        0.25f;


    // <변경부분>
    // Failure 충돌 순간 Target에게 적용할 흔들림 시간.
    //
    // 공격 실패도 방어에 부딪히는 타격감이 있어야 하므로
    // 충돌 지점에서 Target Shake를 실행한다.
    [Min(0f)]
    public float attackTargetShakeDuration =
        0.12f;

    // <변경부분>
    // Failure 충돌 순간 Target 흔들림 강도.
    [Min(0f)]
    public float attackTargetShakeIntensity =
        0.08f;


    // <변경부분>
    // 이하 값은 Battle의 Defense Bounce 연출을 기준으로 한
    // Event Scene 전용 반동 설정이다.

    // 방어 충돌 후 공격자가 원위치 바로 앞쪽에 떨어지는 거리.
    [Min(0f)]
    public float attackFailureFallShortDistance =
        0.25f;

    // 충돌 지점에서 첫 착지 지점까지 튕겨나가는 시간.
    [Min(0f)]
    public float attackFailureFallBackDuration =
        0.12f;

    // 첫 번째 큰 바운스 시간.
    [Min(0f)]
    public float attackFailureFirstBounceDuration =
        0.13f;

    // 첫 번째 큰 바운스 높이.
    [Min(0f)]
    public float attackFailureFirstBounceHeight =
        0.22f;

    // 두 번째 작은 바운스 시간.
    [Min(0f)]
    public float attackFailureSecondBounceDuration =
        0.11f;

    // 두 번째 작은 바운스 높이.
    [Min(0f)]
    public float attackFailureSecondBounceHeight =
        0.11f;

    // 마지막으로 원위치에 정확히 붙는 시간.
    [Min(0f)]
    public float attackFailureFinalReturnDuration =
        0.08f;

    // <변경부분>
    // 이 AttackActor Step에서 공격자의 Flip X 상태를
    // 새로 지정할지 여부.
    //
    // false이면 현재 방향을 유지한다.
    public bool attackChangeFlipX =
        false;

    // <변경부분>
    // attackChangeFlipX가 true일 때 적용할 Flip X.
    public bool attackFlipX =
        false;


    [Header("Play Actor Animation")]

    // <변경부분>
    // Animation을 실행할 Event Actor의 고유 ID.
    //
    // 이전 SpawnActor에서 생성한 Actor ID를 사용한다.
    public string playAnimationActorId;

    // <변경부분>
    // 실행할 Spine Animation Clip 이름.
    //
    // 특정 공용 Animation Enum으로 제한하지 않고
    // 실제 SkeletonData에 존재하는 Clip 이름을 직접 지정한다.
    //
    // 예:
    // Idle
    // Select
    // Down
    // Death
    public string playAnimationName;

    // <변경부분>
    // 지정 Animation을 반복 재생할지 여부.
    //
    // true이면 Animation을 Loop 상태로 시작한 뒤
    // Sequence는 바로 다음 Step으로 진행한다.
    public bool playAnimationLoop =
        false;

    // <변경부분>
    // Loop가 아닌 Animation의 재생 완료까지
    // 현재 Event Sequence가 기다릴지 여부.
    //
    // false이면 Animation 재생을 시작한 직후
    // 바로 다음 Step으로 진행한다.
    public bool playAnimationWaitForComplete =
        true;

    // 비반복 Animation 재생 완료 후
    // Idle Animation으로 복귀할지 여부.
    //
    // 실제 Idle Clip이 존재하는 경우에만 적용한다.
    public bool playAnimationReturnToIdle =
        true;

    // <변경부분>
    // 현재 Spine Animation에서 새 Animation으로 전환될 때
    // 사용할 Mix 시간.
    //
    // 0이면 즉시 전환한다.
    // 값이 클수록 이전 Pose와 새 Pose가 더 부드럽게 연결된다.
    [Min(0f)]
    public float playAnimationMixDuration =
        0.12f;


    [Header("Remove Actor")]

    // <변경부분>
    // Event Scene에서 제거할 Actor의 고유 ID.
    //
    // 이전 SpawnActor에서 생성한 Actor ID를 사용한다.
    public string removeActorId;

    // <변경부분>
    // Actor 제거 방식.
    //
    // FadeOut:
    // 지정 시간 동안 투명해진 뒤 제거.
    //
    // Immediate:
    // Fade 없이 즉시 화면에서 제거.
    public EventSceneRemoveMode removeActorMode =
        EventSceneRemoveMode.FadeOut;

    // <변경부분>
    // FadeOut 제거 시 사용할 시간.
    //
    // Immediate에서는 사용하지 않는다.
    [Min(0f)]
    public float removeActorFadeOutDuration =
        0.35f;


    // =====================================================
    // Screen Shake
    // =====================================================

    [Header("Screen Shake")]

    // <변경부분>
    // 독립 ScreenShake Step에서 화면을 흔들 시간.
    //
    // AttackActor의 Impact Shake와 동일한
    // Event Scene 공용 Camera Shake 시스템을 사용한다.
    [Min(0f)]
    public float screenShakeDuration =
        0.12f;

    // <변경부분>
    // 화면 흔들림 강도.
    //
    // 실제 Camera Shake Target의 localPosition에
    // 적용되는 최대 X/Y Offset이다.
    [Min(0f)]
    public float screenShakeStrength =
        0.08f;

    // <변경부분>
    // true:
    // 화면 흔들림이 끝날 때까지 현재 Step에서 기다린다.
    //
    // false:
    // Shake만 시작하고 즉시 다음 Event Step으로 진행한다.
    public bool screenShakeWaitForComplete =
        true;


    [Header("Wait")]

    // Wait Step에서 대기할 시간.
    [Min(0f)]
    public float waitDuration =
        0.5f;


    // <변경부분>
    // 현재 Locale 기준 Dialogue Page 목록을 반환한다.
    //
    // Localization 참조가 아직 없거나
    // 현재 Locale 번역값이 비어 있으면
    // 기존 dialoguePages의 한국어 원문을 fallback으로 사용한다.
    public List<string> GetLocalizedDialoguePages()
    {
        List<string> resolvedPages =
            new List<string>();

        if (dialoguePages == null)
        {
            return resolvedPages;
        }

        for (int i = 0;
             i < dialoguePages.Count;
             i++)
        {
            string fallbackText =
                dialoguePages[i] ??
                string.Empty;

            string resolvedText =
                fallbackText;

            if (dialogueLocalizationPages != null &&
                i < dialogueLocalizationPages.Count)
            {
                EventSceneDialoguePageLocalizationData
                    localizationData =
                        dialogueLocalizationPages[i];

                if (localizationData != null &&
                    localizationData.localizedText != null &&
                    localizationData.localizedText.IsEmpty == false)
                {
                    string localizedText =
                        localizationData
                            .localizedText
                            .GetLocalizedString();

                    if (string.IsNullOrWhiteSpace(
                            localizedText) == false)
                    {
                        resolvedText =
                            localizedText;
                    }
                }
            }

            resolvedPages.Add(
                resolvedText
            );
        }

        return resolvedPages;
    }
}
