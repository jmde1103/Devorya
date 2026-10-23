using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public enum WorldMapEventStepType
{
    [InspectorName("대화 출력")] Dialogue = 0,
    [InspectorName("플레이어 조작 잠금·해제")] PlayerInput = 1,
    [InspectorName("UI 숨김·표시")] UIVisibility = 2,
    [InspectorName("스크린 글리치")] ScreenGlitch = 3,
    [InspectorName("글로벌 조명·반짝임")] GlobalLighting = 4,
    [InspectorName("대기")] Wait = 5,
    [InspectorName("유물 선택 보상")] RelicReward = 6
}

[Serializable]
public class WorldMapEventStep
{
    [InspectorName("유물 보상 목록")] public RelicRewardListData relicRewardList;
    public bool enabled = true;
    public string stepName = "새 스텝";
    public WorldMapEventStepType type;
    [TextArea(2, 5)] public List<string> dialoguePages = new List<string>();
    public List<LocalizedString> localizedDialoguePages = new List<LocalizedString>();
    [HideInInspector] public List<string> dialoguePageIds = new List<string>();
    public bool lockPlayerInput = true;
    public bool showUI = true;
    [Range(0,1)] public float glitchStrength = .55f;
    [Min(.05f)] public float glitchDuration = .4f;
    public bool waitForComplete = true;
    public bool changeGlobalLight = true;
    public Color lightColor = Color.white;
    [Min(0)] public float lightIntensity = 1;
    [Min(0)] public float lightFadeDuration = .5f;
    public bool screenFlash;
    public Color flashColor = Color.white;
    [Range(0,1)] public float flashStrength = .7f;
    [Min(.01f)] public float flashDuration = .15f;
    [Min(1)] public int flashCount = 1;
    [Min(0)] public float flashInterval = .1f;
    [Min(0)] public float waitDuration = .5f;
}

[CreateAssetMenu(fileName="월드맵_새 이벤트", menuName="Devorya/월드맵/이벤트 데이터")]
public class WorldMapEventData : ScriptableObject
{
    [InspectorName("이벤트 이름")] public string eventName = "월드맵 이벤트";
    [TextArea(2,5), InspectorName("작업 메모")] public string description;
    [InspectorName("종료 시 조명 복원")] public bool restoreLightingOnComplete = true;
    [InspectorName("스텝 목록")] public List<WorldMapEventStep> steps = new List<WorldMapEventStep>();
}
