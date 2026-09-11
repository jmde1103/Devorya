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

    // Actor를 생성할 BackgroundTile 좌표.
    public Vector2Int spawnActorPosition =
        Vector2Int.zero;

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
