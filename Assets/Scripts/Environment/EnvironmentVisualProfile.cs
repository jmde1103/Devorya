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
// 구름 그림자의 맵별 환경 설정.
//
// 실제 Cloud Sprite 구성과 이동 Bounds는
// 공용 CloudShadowRig Prefab이 담당하고,
// 맵마다 달라질 환경값만 Profile에 저장한다.
[System.Serializable]
public class EnvironmentCloudShadowSettings
{
    // 이 환경에서 구름 그림자를 사용할지 여부.
    public bool enabled =
        false;


    // 구름이 흘러갈 월드 방향.
    public Vector2 moveDirection =
        new Vector2(
            1f,
            -0.2f
        );


    // 초당 구름 이동 속도.
    [Min(0f)]
    public float moveSpeed =
        0.25f;


    [Range(0f, 1f)]
    public float opacity =
     1f;
}


// =========================================================
// Fireflies
// =========================================================

[System.Serializable]
public class EnvironmentFireflySettings
{
    public bool enabled =
        false;


    [Header("Visual")]

    public Color spriteColor =
        Color.white;


    [Header("Spot Light")]

    public Color spotLightColor =
        new Color(
            1f,
            0.82f,
            0.42f,
            1f
        );

    [Min(0f)]
    public float spotLightIntensity =
        0.6f;


    [Header("Sprite Light")]

    public Color spriteLightColor =
        new Color(
            1f,
            0.65f,
            0.25f,
            1f
        );

    [Min(0f)]
    public float spriteLightIntensity =
        0.3f;


    [Header("Light Randomness")]

    [Range(0f, 1f)]
    public float intensityRandomness =
        0.2f;


    [Header("Movement")]

    [Min(0f)]
    public float moveSpeed =
        0.18f;

    [Range(0f, 1f)]
    public float moveSpeedRandomness =
        0.35f;

    [Min(0f)]
    public float turnSmoothness =
        1.5f;

    [Min(0f)]
    public float directionChangeIntervalMin =
     0.8f;

    [Min(0f)]
    public float directionChangeIntervalMax =
        2.5f;


    [Header("Avoidance")]

    // 같은 범위에 반딧불이 너무 많이 모이는 것을 줄인다.
    public bool useClusterAvoidance =
     true;

    // WorldRoot Scale 1 기준 군집 판정 거리.
    //
    // 이동 중 반딧불 군집 회피와
    // 반딧불 재등장 위치 판정에서 공통으로 사용한다.
    [Min(0f)]
    public float clusterAvoidanceRadius =
        2f;

    // 같은 영역에 허용할 최대 반딧불 수.
    //
    // 2:
    // 두 마리까지는 자연스럽게 같이 다닐 수 있고
    // 세 번째부터 회피 Steering이 작동한다.
    [Min(2)]
    public int maxFirefliesInCluster =
        2;

    [Min(0f)]
    public float clusterAvoidanceStrength =
        1.35f;

    // 반딧불이 숨었다가 다시 등장할 때
    // 다른 활성 반딧불의 Cluster Avoidance Radius에서
    // 추가로 확보할 거리.
    //
    // 0:
    // Cluster Avoidance Radius 바깥에서 생성.
    //
    // 값이 커질수록 다른 반딧불과 더 떨어진 위치에서
    // 새 반딧불이 다시 나타난다.
    [Min(0f)]
    public float clusterSpawnPadding =
        0.5f;


    // FlameLightFlickerController가 붙은
    // 랜턴 / 횃불 / 모닥불 등의 광원을 피한다.
    public bool useExternalLightAvoidance =
        true;

    // 실제 Point / Spot Light의 Outer Radius보다
    // 어느 정도 여유 있게 피할지 결정한다.
    //
    // 이동 중 외부 광원 회피와
    // 반딧불 재등장 위치 판정에서 공통으로 사용한다.
    [Min(0f)]
    public float externalLightAvoidanceRadiusMultiplier =
        1.15f;

    [Min(0f)]
    public float externalLightAvoidanceStrength =
        1.5f;

    // 반딧불이 숨었다가 다시 등장할 때
    // 외부 광원의 기존 회피 범위에서 추가로 확보할 거리.
    //
    // 0:
    // 이동 중 사용하는 외부 광원 회피 범위와 동일.
    //
    // 값이 커질수록 랜턴 / 횃불 / 모닥불에서
    // 더 떨어진 위치에서 반딧불이 다시 나타난다.
    [Min(0f)]
    public float externalLightSpawnPadding =
        0.5f;

    // Runtime에 새 광원 Prefab이 생성되는 경우를 위해
    // 회피 대상 광원을 다시 검색하는 간격.
    [Min(0.1f)]
    public float externalLightRefreshInterval =
        1f;


    [Header("Visibility Cycle")]

    [Min(0f)]
    public float visibleDurationMin =
        4f;

    [Min(0f)]
    public float visibleDurationMax =
        8f;

    [Min(0f)]
    public float hiddenDurationMin =
        1f;

    [Min(0f)]
    public float hiddenDurationMax =
        3.5f;

    [Min(0f)]
    public float fadeDuration =
        1.25f;

    [Range(0f, 1f)]
    public float startVisibleChance =
        0.6f;


    [Header("Screen Area")]

    [Range(0f, 0.45f)]
    public float spawnViewportPadding =
        0.08f;

    [Range(0f, 0.45f)]
    public float edgeSteerViewportMargin =
        0.1f;
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


    [Header("Character Fill")]

    // 야간 환경에서 Piece가 지나치게 어두워지지 않도록
    // Piece Sorting Layer를 보조하는 캐릭터 전용 Spot Light 설정.
    public EnvironmentPointLightSettings characterFill =
        new EnvironmentPointLightSettings
        {
            enabled = false,
            color = Color.white,
            intensity = 0f
        };


    [Header("Sun Key")]
    public EnvironmentPointLightSettings sunKey =
        new EnvironmentPointLightSettings();


    [Header("Sky Fill")]
    public EnvironmentPointLightSettings skyFill =
        new EnvironmentPointLightSettings();


    // <변경부분>
    [Header("Cloud Shadow")]
    public EnvironmentCloudShadowSettings cloudShadow =
        new EnvironmentCloudShadowSettings();


    [Header("Fireflies")]
    public EnvironmentFireflySettings fireflies =
        new EnvironmentFireflySettings();
}
