using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

[RequireComponent(
    typeof(SkeletonGraphic),
    typeof(SkeletonAnimation)
)]
public class PieceSpinePreviewUI : MonoBehaviour
{
    [Header("Spine UI")]
    [SerializeField]
    private SkeletonGraphic skeletonGraphic;

    [SerializeField]
    private SkeletonAnimation skeletonAnimation;

    [SerializeField]
    private string idleAnimationName =
        "Idle";


    private Material originalGraphicMaterial;

    private readonly Dictionary<Texture, Material>
        runtimePreviewMaterials =
            new Dictionary<Texture, Material>(4);


    private static readonly int HuePropertyId =
        Shader.PropertyToID("_Hue");

    private static readonly int SaturationPropertyId =
        Shader.PropertyToID("_Saturation");

    private static readonly int BrightnessPropertyId =
        Shader.PropertyToID("_Brightness");


    private const string ColorAdjustmentKeyword =
        "_COLOR_ADJUST";


    private bool hasWarnedMissingColorAdjustment;


    private void Awake()
    {
        ResolveReferences();

        if (skeletonGraphic != null)
        {
            originalGraphicMaterial =
                skeletonGraphic.material;

            skeletonGraphic.raycastTarget =
                false;
        }


        Hide();
    }


    private void OnDestroy()
    {
        ClearRuntimePreviewMaterials();
    }


    private void ResolveReferences()
    {
        if (skeletonGraphic == null)
        {
            skeletonGraphic =
                GetComponent<SkeletonGraphic>();
        }


        if (skeletonAnimation == null)
        {
            skeletonAnimation =
                GetComponent<SkeletonAnimation>();
        }
    }

    // <변경부분>
    // Status UI에서는 필드 시점과 다르게
    // 플레이어 진영도 "앞모습" 기준으로 표시한다.
    //
    // 따라서 Player 팀 Preview는
    // Player/Absorbed Back Prefab 대신
    // Enemy 쪽 앞모습 Spine Visual Prefab을 우선 사용한다.
    //
    // 단, 회색조 적용 여부는 기존 Piece 상태
    // (team / isAbsorbedPlayerVisual)를 그대로 기준으로 유지한다.
    private GameObject ResolvePreviewSpineVisualPrefab(
        PieceData pieceData,
        PieceTeam team,
        bool isAbsorbedPlayerVisual)
    {
        if (pieceData == null)
        {
            return null;
        }


        // ================================================
        // Status UI Rule
        // Player 팀은 항상 앞모습 기준으로 표시
        // ================================================
        if (team == PieceTeam.Player)
        {
            GameObject frontPreviewPrefab =
                pieceData.GetSpineVisualPrefab(
                    PieceTeam.Enemy,
                    false
                );


            if (frontPreviewPrefab != null)
            {
                return frontPreviewPrefab;
            }
        }


        // Enemy / Neutral는 기존 규칙 유지
        // 또는 Player인데 앞모습 Prefab이 없을 경우 fallback
        return pieceData.GetSpineVisualPrefab(
            team,
            isAbsorbedPlayerVisual
        );
    }

    public bool Show(Piece piece)
    {
        ResolveReferences();


        if (piece == null ||
            skeletonGraphic == null ||
            skeletonAnimation == null)
        {
            Hide();

            return false;
        }


        PieceData pieceData =
            piece.CurrentPieceData;


        if (pieceData == null)
        {
            Hide();

            return false;
        }


        GameObject spineVisualPrefab =
    ResolvePreviewSpineVisualPrefab(
        pieceData,
        piece.Team,
        piece.IsAbsorbedJelluVisual
    );


        if (spineVisualPrefab == null)
        {
            Hide();

            return false;
        }


        SkeletonRenderer sourceSkeletonRenderer =
            spineVisualPrefab
                .GetComponentInChildren<SkeletonRenderer>(
                    true
                );


        if (sourceSkeletonRenderer == null)
        {
            Debug.LogWarning(
                "[PieceSpinePreviewUI] " +
                "Spine Visual Prefab에서 " +
                "SkeletonRenderer를 찾지 못했습니다.",
                this
            );

            Hide();

            return false;
        }


        SkeletonDataAsset skeletonDataAsset =
            sourceSkeletonRenderer
                .SkeletonDataAsset;


        if (skeletonDataAsset == null)
        {
            Debug.LogWarning(
                "[PieceSpinePreviewUI] " +
                "SkeletonDataAsset이 없습니다.",
                this
            );

            Hide();

            return false;
        }


        skeletonGraphic.enabled =
            true;

        skeletonAnimation.enabled =
            true;


        ClearRuntimePreviewMaterials();


        // 필드 Visual Prefab과 동일한
        // SkeletonDataAsset / Skin / Flip 기준을 사용한다.
        skeletonGraphic.SkeletonDataAsset =
            skeletonDataAsset;

        skeletonGraphic.InitialSkinName =
            sourceSkeletonRenderer.InitialSkinName;

        skeletonGraphic.InitialFlipX =
            sourceSkeletonRenderer.InitialFlipX;

        skeletonGraphic.InitialFlipY =
            sourceSkeletonRenderer.InitialFlipY;


        // SkeletonDataAsset이 변경되었으므로
        // Renderer와 AnimationState를 다시 초기화한다.
        skeletonGraphic.Initialize(
            true
        );

        skeletonAnimation
            .InitializeAnimationComponent();


        if (skeletonGraphic.SkeletonData == null ||
            skeletonGraphic
                .SkeletonData
                .FindAnimation(
                    idleAnimationName
                ) == null)
        {
            Debug.LogWarning(
                "[PieceSpinePreviewUI] " +
                $"Idle Animation을 찾지 못했습니다: " +
                $"{idleAnimationName}",
                this
            );

            Hide();

            return false;
        }


        ApplyPreviewMaterials(
            skeletonDataAsset,
            piece
        );


        // Status UI에서는 항상 Idle만 사용한다.
        skeletonAnimation.loop =
            true;

        skeletonAnimation.AnimationName =
            idleAnimationName;


        // 첫 프레임에서도 바로 Idle Pose가 보이도록
        // 현재 상태를 즉시 한 번 평가한다.
        skeletonAnimation.Update(
            0f
        );


        skeletonGraphic.SetMaterialDirty();


        return true;
    }


    public void Hide()
    {
        ClearRuntimePreviewMaterials();


        if (skeletonAnimation != null)
        {
            skeletonAnimation
                .ClearAnimationState();

            skeletonAnimation.enabled =
                false;
        }


        if (skeletonGraphic != null)
        {
            skeletonGraphic.enabled =
                false;
        }
    }


    private void ApplyPreviewMaterials(
        SkeletonDataAsset skeletonDataAsset,
        Piece piece)
    {
        if (skeletonDataAsset == null ||
            skeletonDataAsset.atlasAssets == null)
        {
            return;
        }


        bool shouldApplyColorAdjustment =
            false;

        float targetHue =
            0f;

        float targetSaturation =
            1f;

        float targetBrightness =
            1f;


        PieceVisualController visualController =
            piece.GetComponent<
                PieceVisualController
            >();


        if (visualController != null)
        {
            shouldApplyColorAdjustment =
                visualController
                    .TryGetPlayerColorAdjustmentForPreview(
                        piece.Team,
                        piece.IsAbsorbedJelluVisual,
                        out targetHue,
                        out targetSaturation,
                        out targetBrightness
                    );
        }


        Material firstRuntimeMaterial =
            null;


        for (int atlasIndex = 0;
             atlasIndex <
             skeletonDataAsset.atlasAssets.Length;
             atlasIndex++)
        {
            AtlasAssetBase atlasAsset =
                skeletonDataAsset
                    .atlasAssets[
                        atlasIndex
                    ];


            if (atlasAsset == null)
            {
                continue;
            }


            Material sourceMaterial =
                atlasAsset.PrimaryMaterial;


            if (sourceMaterial == null)
            {
                continue;
            }


            Texture sourceTexture =
                sourceMaterial.mainTexture;


            if (sourceTexture == null ||
                runtimePreviewMaterials
                    .ContainsKey(
                        sourceTexture
                    ))
            {
                continue;
            }


            // 필드 Spine Material을 복제하므로
            // Texture / Shader / 기존 Material 설정을
            // 그대로 유지한다.
            Material runtimeMaterial =
                new Material(
                    sourceMaterial
                );


            runtimeMaterial.name =
                sourceMaterial.name +
                " (Status UI Preview Runtime)";


            if (shouldApplyColorAdjustment)
            {
                ApplyColorAdjustment(
                    runtimeMaterial,
                    targetHue,
                    targetSaturation,
                    targetBrightness
                );
            }


            runtimePreviewMaterials.Add(
                sourceTexture,
                runtimeMaterial
            );


            skeletonGraphic
                .CustomMaterialOverride[
                    sourceTexture
                ] =
                    runtimeMaterial;


            if (firstRuntimeMaterial == null)
            {
                firstRuntimeMaterial =
                    runtimeMaterial;
            }
        }


        if (firstRuntimeMaterial != null)
        {
            skeletonGraphic.material =
                firstRuntimeMaterial;
        }
    }


    private void ApplyColorAdjustment(
        Material material,
        float hue,
        float saturation,
        float brightness)
    {
        if (material == null)
        {
            return;
        }


        bool hasHue =
            material.HasProperty(
                HuePropertyId
            );

        bool hasSaturation =
            material.HasProperty(
                SaturationPropertyId
            );

        bool hasBrightness =
            material.HasProperty(
                BrightnessPropertyId
            );


        if (hasHue == false ||
            hasSaturation == false ||
            hasBrightness == false)
        {
            if (hasWarnedMissingColorAdjustment ==
                false)
            {
                hasWarnedMissingColorAdjustment =
                    true;

                Debug.LogWarning(
                    "[PieceSpinePreviewUI] " +
                    "Preview Material에 " +
                    "Color Adjustment Property가 없습니다.",
                    this
                );
            }

            return;
        }


        if (material.IsKeywordEnabled(
                ColorAdjustmentKeyword) == false)
        {
            material.EnableKeyword(
                ColorAdjustmentKeyword
            );
        }


        material.SetFloat(
            HuePropertyId,
            hue
        );

        material.SetFloat(
            SaturationPropertyId,
            saturation
        );

        material.SetFloat(
            BrightnessPropertyId,
            brightness
        );
    }


    private void ClearRuntimePreviewMaterials()
    {
        if (skeletonGraphic != null &&
            runtimePreviewMaterials.Count > 0)
        {
            Dictionary<Texture, Material>
                customMaterialOverride =
                    skeletonGraphic
                        .CustomMaterialOverride;


            foreach (
                KeyValuePair<Texture, Material>
                    materialPair
                in runtimePreviewMaterials)
            {
                if (materialPair.Key != null)
                {
                    customMaterialOverride.Remove(
                        materialPair.Key
                    );
                }
            }
        }


        foreach (
            KeyValuePair<Texture, Material>
                materialPair
            in runtimePreviewMaterials)
        {
            if (materialPair.Value != null)
            {
                Destroy(
                    materialPair.Value
                );
            }
        }


        runtimePreviewMaterials.Clear();


        if (skeletonGraphic != null)
        {
            skeletonGraphic.material =
                originalGraphicMaterial;
        }
    }
}
