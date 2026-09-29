using UnityEngine;


// <변경부분>
// Battle Scene / Event Scene에서 공용으로 사용할
// 구름 그림자 이동 Controller.
//
// 구름 Sprite 자체는 자식 오브젝트로 배치하고,
// 이 Controller는 각 구름을 지정된 방향으로 계속 이동시킨다.
//
// 이동 범위를 벗어난 구름은 반대편으로 재배치하여
// Instantiate / Destroy 없이 반복 사용한다.
public class CloudShadowController : MonoBehaviour
{
    [Header("Cloud Shadows")]

    // 실제로 이동시킬 구름 그림자 Transform 목록.
    [SerializeField]
    private Transform[] cloudShadows;


    [Header("Movement")]

    // 구름이 이동할 월드 방향.
    //
    // 예:
    // (1, 0)     = 오른쪽
    // (1, -0.2)  = 오른쪽 아래
    [SerializeField]
    private Vector2 moveDirection =
        new Vector2(
            1f,
            -0.2f
        );


    // 초당 이동 속도.
    [SerializeField, Min(0f)]
    private float moveSpeed =
        0.25f;


    [Header("Loop Bounds")]

    // 구름이 이동 가능한 월드 X 최소값.
    [SerializeField]
    private float minX =
        -12f;


    // 구름이 이동 가능한 월드 X 최대값.
    [SerializeField]
    private float maxX =
        12f;


    // 구름이 이동 가능한 월드 Y 최소값.
    [SerializeField]
    private float minY =
        -8f;


    // 구름이 이동 가능한 월드 Y 최대값.
    [SerializeField]
    private float maxY =
        8f;


    // <변경부분>
    // 현재 CloudShadowRig 아래의 SpriteRenderer를 캐시한다.
    //
    // EnvironmentVisualProfile의 Opacity를
    // 모든 구름 Sprite에 공통 적용하기 위해 사용한다.
    private SpriteRenderer[] cloudSpriteRenderers;


    private void Awake()
    {
        CacheCloudSpriteRenderers();
    }


    // <변경부분>
    // 현재 BackgroundMapData에 연결된
    // EnvironmentVisualProfile의 Cloud Shadow 설정을 적용한다.
    //
    // 같은 CloudShadowRig Prefab을
    // Battle Scene / Event Scene에서 공용으로 사용할 수 있다.
    public void ApplyProfile(
        EnvironmentVisualProfile visualProfile)
    {
        // Profile 자체가 없는 맵으로 변경될 경우
        // 이전 맵의 구름이 남지 않도록 비활성화한다.
        if (visualProfile == null ||
            visualProfile.cloudShadow == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }


        EnvironmentCloudShadowSettings settings =
            visualProfile.cloudShadow;


        // 구름을 사용하지 않는 환경이면
        // Rig 전체를 꺼서 Update 비용도 발생하지 않게 한다.
        if (settings.enabled == false)
        {
            gameObject.SetActive(
                false
            );

            return;
        }


        // 이전 맵에서 비활성화되어 있었다면 다시 켠다.
        gameObject.SetActive(
            true
        );


        moveDirection =
            settings.moveDirection;

        moveSpeed =
            settings.moveSpeed;


        CacheCloudSpriteRenderers();


        ApplyOpacity(
            settings.opacity
        );
    }


    // <변경부분>
    // CloudShadowRig 안의 모든 구름 SpriteRenderer를 확보한다.
    //
    // 자식이 비활성 상태여도 포함한다.
    private void CacheCloudSpriteRenderers()
    {
        cloudSpriteRenderers =
            GetComponentsInChildren<SpriteRenderer>(
                true
            );
    }


    // <변경부분>
    // PNG 내부의 Alpha 형태는 그대로 유지하면서,
    // SpriteRenderer의 전체 Alpha를 이용해
    // Profile의 구름 그림자 강도를 적용한다.
    private void ApplyOpacity(
        float opacity)
    {
        if (cloudSpriteRenderers == null ||
            cloudSpriteRenderers.Length == 0)
        {
            return;
        }


        float safeOpacity =
            Mathf.Clamp01(
                opacity
            );


        for (int i = 0;
             i < cloudSpriteRenderers.Length;
             i++)
        {
            SpriteRenderer cloudRenderer =
                cloudSpriteRenderers[i];


            if (cloudRenderer == null)
            {
                continue;
            }


            Color color =
                cloudRenderer.color;


            color.a =
                safeOpacity;


            cloudRenderer.color =
                color;
        }
    }


    private void Update()
    {
        if (cloudShadows == null ||
            cloudShadows.Length == 0)
        {
            return;
        }


        if (moveSpeed <= 0f)
        {
            return;
        }


        Vector2 normalizedDirection =
            moveDirection.sqrMagnitude > 0f
                ? moveDirection.normalized
                : Vector2.right;


        Vector3 movement =
            new Vector3(
                normalizedDirection.x,
                normalizedDirection.y,
                0f
            ) *
            moveSpeed *
            Time.deltaTime;


        for (int i = 0;
             i < cloudShadows.Length;
             i++)
        {
            Transform cloudShadow =
                cloudShadows[i];


            if (cloudShadow == null)
            {
                continue;
            }


            cloudShadow.position +=
                movement;


            WrapCloudPosition(
                cloudShadow,
                normalizedDirection
            );
        }
    }


    // <변경부분>
    // 지정된 이동 영역을 벗어난 구름을
    // 반대편으로 재배치한다.
    private void WrapCloudPosition(
        Transform cloudShadow,
        Vector2 normalizedDirection)
    {
        Vector3 position =
            cloudShadow.position;


        if (normalizedDirection.x > 0f &&
            position.x > maxX)
        {
            position.x =
                minX;
        }
        else if (normalizedDirection.x < 0f &&
                 position.x < minX)
        {
            position.x =
                maxX;
        }


        if (normalizedDirection.y > 0f &&
            position.y > maxY)
        {
            position.y =
                minY;
        }
        else if (normalizedDirection.y < 0f &&
                 position.y < minY)
        {
            position.y =
                maxY;
        }


        cloudShadow.position =
            position;
    }
}
