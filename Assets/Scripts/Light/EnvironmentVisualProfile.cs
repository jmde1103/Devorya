using UnityEngine;


// Global Light 2D에 저장할 환경 조명값.
[System.Serializable]
public class EnvironmentGlobalLightSettings
{
    public bool enabled = true;

    public Color color =
        Color.white;

    [Min(0f)]
    public float intensity =
        1f;
}


// Sun_Key / Sky_Fill처럼
// 위치와 범위를 가지는 2D Light에 저장할 환경 조명값.
[System.Serializable]
public class EnvironmentPointLightSettings
{
    public bool enabled = true;

    public Color color =
        Color.white;

    [Min(0f)]
    public float intensity =
        1f;

    [Range(0f, 1f)]
    public float falloffIntensity =
        0.5f;


    [Header("Transform")]

    // EnvironmentLightingRig 기준 Local Position.
    //
    // Rig 자체가 CameraFollower를 통해 카메라를 따라가므로
    // Profile에서는 월드 좌표가 아니라 상대 위치만 저장한다.
    public Vector3 localPosition =
        Vector3.zero;

    public Vector3 localEulerAngles =
        Vector3.zero;


    [Header("Point / Spot Shape")]

    [Min(0f)]
    public float innerRadius =
        0f;

    [Min(0f)]
    public float outerRadius =
        5f;

    [Range(0f, 360f)]
    public float innerAngle =
        360f;

    [Range(0f, 360f)]
    public float outerAngle =
        360f;
}


// <변경부분>
// BackgroundMapData마다 연결하여 사용할
// 환경 비주얼 / 조명 Profile.
//
// EnvironmentLightingRig Prefab 자체는 모든 Scene에서 공용으로 유지하고,
// 배경마다 달라지는 조명 파라미터만 이 Asset에 저장한다.
[CreateAssetMenu(
    fileName = "EnvironmentVisualProfile",
    menuName = "Devorya/Environment/Environment Visual Profile"
)]
public class EnvironmentVisualProfile : ScriptableObject
{
    [Header("Global Ambient")]
    public EnvironmentGlobalLightSettings globalAmbient =
        new EnvironmentGlobalLightSettings();


    [Header("Sun Key")]
    public EnvironmentPointLightSettings sunKey =
        new EnvironmentPointLightSettings();


    [Header("Sky Fill")]
    public EnvironmentPointLightSettings skyFill =
        new EnvironmentPointLightSettings();
}
