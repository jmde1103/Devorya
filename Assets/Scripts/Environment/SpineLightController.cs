using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SpineLightController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField]
    private SkeletonRenderer skeletonRenderer;

    [SpineSlot]
    [SerializeField]
    private string controlSlotName;


    [System.Serializable]
    private class LightTarget
    {
        [SerializeField]
        private Light2D light;

        [SerializeField, Min(0f)]
        private float maxIntensity = 1f;


        public Light2D Light =>
            light;

        public float MaxIntensity =>
            maxIntensity;
    }


    [Header("Light")]
    [SerializeField]
    private LightTarget[] targetLights;

    [SerializeField]
    private bool setIntensityToZeroOnDisable = true;


    private Slot controlSlot;

    private bool hasWarnedMissingRenderer;
    private bool hasWarnedMissingLights;
    private bool hasWarnedMissingSlot;


    private void Awake()
    {
        ResolveReferences();
    }


    private void OnEnable()
    {
        ResolveReferences();

        SubscribeCallbacks();

        CacheControlSlot();

        ApplyCurrentSlotAlpha();
    }


    private void OnDisable()
    {
        UnsubscribeCallbacks();

        controlSlot = null;

        if (setIntensityToZeroOnDisable)
        {
            ApplyIntensityToTargetLights(
                0f
            );
        }
    }


    private void ResolveReferences()
    {
        if (skeletonRenderer == null)
        {
            skeletonRenderer =
                GetComponent<SkeletonRenderer>();

            if (skeletonRenderer == null)
            {
                skeletonRenderer =
                    GetComponentInParent<SkeletonRenderer>();
            }
        }


        if (skeletonRenderer == null &&
    hasWarnedMissingRenderer == false)
        {
            hasWarnedMissingRenderer = true;

            Debug.LogWarning(
                "[SpineLightController] SkeletonRenderer was not found.",
                this
            );
        }


        bool hasValidTargetLight = false;
        bool hasMissingTargetLight = false;


        if (targetLights == null ||
            targetLights.Length == 0)
        {
            hasMissingTargetLight = true;
        }
        else
        {
            for (int i = 0;
                 i < targetLights.Length;
                 i++)
            {
                LightTarget lightTarget =
                    targetLights[i];


                if (lightTarget == null ||
                    lightTarget.Light == null)
                {
                    hasMissingTargetLight = true;

                    continue;
                }


                hasValidTargetLight = true;
            }
        }


        if ((hasValidTargetLight == false ||
             hasMissingTargetLight) &&
            hasWarnedMissingLights == false)
        {
            hasWarnedMissingLights = true;

            Debug.LogWarning(
                "[SpineLightController] One or more Target Light2D references are missing.",
                this
            );
        }
    }


    private void SubscribeCallbacks()
    {
        if (skeletonRenderer == null)
        {
            return;
        }


        // Prevent duplicate subscription.
        skeletonRenderer.OnRebuild -=
            HandleSkeletonRebuild;

        skeletonRenderer.UpdateComplete -=
            HandleSkeletonUpdateComplete;


        skeletonRenderer.OnRebuild +=
            HandleSkeletonRebuild;

        skeletonRenderer.UpdateComplete +=
            HandleSkeletonUpdateComplete;
    }


    private void UnsubscribeCallbacks()
    {
        if (skeletonRenderer == null)
        {
            return;
        }


        skeletonRenderer.OnRebuild -=
            HandleSkeletonRebuild;

        skeletonRenderer.UpdateComplete -=
            HandleSkeletonUpdateComplete;
    }


    private void HandleSkeletonRebuild(
        ISkeletonRenderer renderer)
    {
        controlSlot = null;

        hasWarnedMissingSlot = false;

        CacheControlSlot();

        ApplyCurrentSlotAlpha();
    }


    private void HandleSkeletonUpdateComplete(
        ISkeletonRenderer renderer)
    {
        if (controlSlot == null)
        {
            CacheControlSlot();
        }


        ApplyCurrentSlotAlpha();
    }


    private void CacheControlSlot()
    {
        if (skeletonRenderer == null ||
            skeletonRenderer.Skeleton == null)
        {
            return;
        }


        if (string.IsNullOrWhiteSpace(
                controlSlotName))
        {
            return;
        }


        controlSlot =
            skeletonRenderer.Skeleton.FindSlot(
                controlSlotName
            );


        if (controlSlot == null &&
            hasWarnedMissingSlot == false)
        {
            hasWarnedMissingSlot = true;

            Debug.LogWarning(
                "[SpineLightController] Spine Slot was not found: " +
                controlSlotName,
                this
            );
        }
    }


    private void ApplyCurrentSlotAlpha()
    {
        if (controlSlot == null)
        {
            return;
        }


        UnityEngine.Color slotColor =
            controlSlot.AppliedPose.GetColor();


        float slotAlpha =
            Mathf.Clamp01(
                slotColor.a
            );


        ApplyIntensityToTargetLights(
            slotAlpha
        );
    }


    private void ApplyIntensityToTargetLights(
        float slotAlpha)
    {
        if (targetLights == null)
        {
            return;
        }


        slotAlpha =
            Mathf.Clamp01(
                slotAlpha
            );


        for (int i = 0;
             i < targetLights.Length;
             i++)
        {
            LightTarget lightTarget =
                targetLights[i];


            if (lightTarget == null ||
                lightTarget.Light == null)
            {
                continue;
            }


            lightTarget.Light.intensity =
                slotAlpha *
                lightTarget.MaxIntensity;
        }
    }
}
