using UnityEngine;

// <변경부분>
// EnvironmentLightingRig 전체가 Main Camera의 XY 이동을 따라가도록 하는 컨트롤러.
//
// Sun_Key / Sky_Fill의 현재 Local Position은 그대로 유지하고
// Rig Root만 Camera와 함께 이동시킨다.
//
// Camera Zoom은 현재 WorldRoot Scale 방식이므로 여기서는 추적하지 않는다.
public class EnvironmentLightingCameraFollower : MonoBehaviour
{
    [Header("Camera")]

    // 추적할 Camera Transform.
    // 비어 있으면 Main Camera를 자동으로 찾는다.
    [SerializeField]
    private Transform cameraTarget;

    [Header("Follow")]

    // Scene에서 이미 맞춰 둔
    // Camera ↔ LightingRig의 최초 상대 위치를 유지한다.
    [SerializeField]
    private bool preserveInitialOffset = true;

    // LightingRig의 Z 위치는 Camera Z를 따라가지 않고
    // Scene/Prefab에서 설정된 값을 그대로 유지한다.
    private float initialZPosition;

    // 시작 시 Camera와 LightingRig 사이의 World Offset.
    private Vector3 cameraToRigOffset;

    // Offset 초기화 여부.
    private bool isInitialized;


    private void Awake()
    {
        initialZPosition =
            transform.position.z;

        ResolveCameraTarget();

        InitializeFollowOffset();
    }


    // Camera의 일반 이동 / CameraShot / Focus 등이 모두 적용된 뒤
    // LightingRig를 따라가게 하기 위해 LateUpdate에서 처리한다.
    private void LateUpdate()
    {
        if (cameraTarget == null)
        {
            ResolveCameraTarget();

            if (cameraTarget == null)
            {
                return;
            }

            InitializeFollowOffset();
        }

        if (isInitialized == false)
        {
            InitializeFollowOffset();
        }

        Vector3 targetPosition =
            transform.position;

        if (preserveInitialOffset)
        {
            targetPosition.x =
                cameraTarget.position.x +
                cameraToRigOffset.x;

            targetPosition.y =
                cameraTarget.position.y +
                cameraToRigOffset.y;
        }
        else
        {
            targetPosition.x =
                cameraTarget.position.x;

            targetPosition.y =
                cameraTarget.position.y;
        }

        // Light2D의 기존 Z 배치를 유지한다.
        targetPosition.z =
            initialZPosition;

        transform.position =
            targetPosition;
    }


    // Inspector 연결이 없을 경우 Main Camera를 자동으로 찾는다.
    private void ResolveCameraTarget()
    {
        if (cameraTarget != null)
        {
            return;
        }

        if (Camera.main == null)
        {
            return;
        }

        cameraTarget =
            Camera.main.transform;
    }


    // 현재 Scene에서 이미 맞춰 둔 조명 위치를 기준으로
    // Camera와 LightingRig 사이의 상대 Offset을 저장한다.
    private void InitializeFollowOffset()
    {
        if (cameraTarget == null)
        {
            isInitialized =
                false;

            return;
        }

        cameraToRigOffset =
            transform.position -
            cameraTarget.position;

        // Camera Z는 추적하지 않으므로 사용하지 않는다.
        cameraToRigOffset.z =
            0f;

        isInitialized =
            true;
    }
}