using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> 전투 화면에서 꾹 누른 아이콘 옆에 설명 팝업을 표시하는 UI
public class TooltipPopupUI : MonoBehaviour
{
    public static TooltipPopupUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform popupRoot;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text categoryText;

    // <변경부분> 일반스킬처럼 레벨이 있는 Tooltip에서 표시할 레벨 텍스트
    [SerializeField] private TMP_Text levelText;

    [SerializeField] private TMP_Text mainDescriptionText;

    // <변경부분>
    // 능력(Ability) 전용 Cost 표시 영역.
    // 다른 종류의 Tooltip에서는 CostRoot 전체를 숨긴다.
    [Header("Ability Cost")]
    [SerializeField] private GameObject costRoot;
    [SerializeField] private TMP_Text costValueText;

    [Header("Open Animation")]
    // <변경부분> Tooltip 팝업이 열릴 때 지지직 오픈 애니메이션을 재생하는 컴포넌트
    [SerializeField] private PopupOpenAnimator popupOpenAnimator;

    [System.Serializable]
    public class TooltipSectionPrefabData
    {
        // <변경부분> 이 프리팹이 담당할 Section 종류
        public TooltipSectionType sectionType;

        // <변경부분> 해당 Section 종류에 사용할 UI 프리팹
        public TooltipSectionItemUI sectionPrefab;
    }

    [Header("Section")]
    // <변경부분> Section 위치 보정을 직접 적용해야 하므로
    // 일반 Transform이 아니라 RectTransform으로 연결한다.
    [SerializeField] private RectTransform sectionParent;

    // <변경부분>
    // SectionParent를 기본 Tooltip 바로 아래에 배치할 때
    // 기준으로 사용할 기본 Tooltip의 Background.
    //
    // Hierarchy의 CommonTooltipPopup/Background를 연결한다.
    [SerializeField]
    private RectTransform baseTooltipBackground;

    // <변경부분>
    // 기본 Tooltip과 첫 번째 Section 사이의 간격.
    // 0이면 두 영역이 바로 이어진다.
    [SerializeField, Min(0f)]
    private float sectionGapBelowBase = 0f;

    // <변경부분> 매칭되는 프리팹이 없을 때 사용할 기본 Section 프리팹
    [SerializeField] private TooltipSectionItemUI defaultSectionPrefab;

    // <변경부분> SectionType별로 사용할 프리팹 목록
    [SerializeField] private TooltipSectionPrefabData[] sectionPrefabs;

    [Header("Position")]
    [SerializeField] private Vector2 popupOffset = new Vector2(30f, 30f);
    [SerializeField] private Vector2 screenPadding = new Vector2(20f, 20f);

    // <변경부분> FixedCanvasPosition 모드에서 사용할 PopupRoot Pivot
    // 기본값 0.5, 0.5는 팝업 중심을 고정 좌표에 맞춘다.
    [SerializeField]
    private Vector2 fixedPositionPivot =
     new Vector2(0.5f, 0.5f);

    [Header("Section Popup Position")]
    // <변경부분> Section 블록이 추가되어 팝업 전체 높이가 길어질 때
    // 기본 Tooltip 기준점을 Y축으로 보정할 값
    //
    // Section 1개당 이 값만큼 PopupRoot 전체 위치가 추가 보정된다.

    // <변경부분> 현재 Tooltip에 표시되는 유효한 Section 개수
    private int currentSectionCount;


    // <변경부분> Tooltip 프리팹에 설정되어 있던
    // SectionParent의 원래 Anchored Position
    //
    // 각 TooltipTrigger의 Section Offset이 누적되지 않도록
    // 항상 이 기본 위치를 기준으로 계산한다.
    private Vector2 defaultSectionParentAnchoredPosition;


    // Keep the existing sibling hierarchy and authored top edges.
    private bool hasCapturedBodySize;
    private float minimumBodyHeight;
    private float minimumDescriptionHeight;
    private float bodyBottomPadding;
    private readonly Vector3[] bodySizingCorners = new Vector3[4];

    private void ResizeBaseTooltipToDescription()
    {
        if (baseTooltipBackground == null && popupRoot != null)
            baseTooltipBackground = popupRoot.Find("Background") as RectTransform;
        if (baseTooltipBackground == null || mainDescriptionText == null) return;
        Canvas.ForceUpdateCanvases();
        var description = mainDescriptionText.rectTransform;
        if (!hasCapturedBodySize)
        {
            minimumBodyHeight = baseTooltipBackground.rect.height;
            minimumDescriptionHeight = description.rect.height;
            description.GetWorldCorners(bodySizingCorners);
            bodyBottomPadding = Mathf.Max(12f,
                baseTooltipBackground.InverseTransformPoint(bodySizingCorners[0]).y
                - baseTooltipBackground.rect.yMin);
            var backgroundImage = baseTooltipBackground.GetComponent<Image>();
            if (backgroundImage != null && backgroundImage.sprite != null)
                backgroundImage.type = Image.Type.Sliced;
            hasCapturedBodySize = true;
        }
        // Measure at the existing width; do not shrink fonts to hide overflow.
        float preferredHeight = mainDescriptionText.GetPreferredValues(
            mainDescriptionText.text, Mathf.Max(1f, description.rect.width), Mathf.Infinity).y;
        ResizeHeightKeepingTop(description, Mathf.Max(minimumDescriptionHeight, Mathf.Ceil(preferredHeight)));
        description.GetWorldCorners(bodySizingCorners);
        float textBottom = baseTooltipBackground.InverseTransformPoint(bodySizingCorners[0]).y;
        float requiredHeight = baseTooltipBackground.rect.yMax - textBottom + bodyBottomPadding;
        ResizeHeightKeepingTop(baseTooltipBackground, Mathf.Max(minimumBodyHeight, requiredHeight));
        mainDescriptionText.ForceMeshUpdate();
    }

    private void ResizeHeightKeepingTop(RectTransform rect, float height)
    {
        rect.GetWorldCorners(bodySizingCorners);
        Vector3 previousTop = bodySizingCorners[1];
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        rect.GetWorldCorners(bodySizingCorners);
        rect.position += previousTop - bodySizingCorners[1];
    }

    private void Awake()
    {
        Instance = this;

        // <변경부분> Tooltip 프리팹에 설정된
        // SectionParent의 기본 위치를 최초 한 번 저장한다.
        //
        // 이후 각 TooltipTrigger의 Section Position Offset은
        // 이 위치를 기준으로 적용된다.
        // <변경부분> SectionParent의 최초 위치를 저장한다.
        if (sectionParent != null)
        {
            defaultSectionParentAnchoredPosition =
                sectionParent.anchoredPosition;
        }

        if (popupRoot != null)
        {
            popupRoot.gameObject.SetActive(false);
        }

        if (popupOpenAnimator == null)
        {
            popupOpenAnimator =
                GetComponent<PopupOpenAnimator>();
        }
    }

    // <변경부분> TooltipData 에셋을 받아 TooltipViewData로 변환 후
    // 공통 위치 설정으로 팝업을 표시한다.
    public void Show(
    TooltipViewData tooltipViewData,
    Vector2 screenPosition,
    Vector2 customPositionOffset)
    {
        Show(
            tooltipViewData,
            screenPosition,
            TooltipPositionMode.PointerOffset,
            customPositionOffset,
            Vector2.zero,
            Vector2.zero,
            0f
        );
    }

    // <변경부분> Tooltip 전체 위치 설정과 별도로
    // SectionParent 전용 위치 보정값을 함께 받는다.
    public void Show(
     TooltipViewData tooltipViewData,
     Vector2 screenPosition,
     TooltipPositionMode positionMode,
     Vector2 customPositionOffset,
     Vector2 fixedCanvasPosition,
     Vector2 sectionPositionOffset,
     float popupOffsetYPerSection)
    {
        if (tooltipViewData == null ||
            popupRoot == null)
        {
            Hide();
            return;
        }

        popupRoot.gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text =
                tooltipViewData.title;
        }

        if (categoryText != null)
        {
            categoryText.text =
                tooltipViewData.category;
        }

        if (levelText != null)
        {
            levelText.text =
                tooltipViewData.levelText;

            levelText.gameObject.SetActive(
                string.IsNullOrEmpty(
                    tooltipViewData.levelText
                ) == false
            );
        }

        if (mainDescriptionText != null)
        {
            mainDescriptionText.text =
                tooltipViewData.mainDescription;
        }

        // <변경부분>
        // 능력 Tooltip에 Cost 데이터가 있는 경우에만
        // CostRoot를 표시한다.
        //
        // Cost 0은 정상적인 값이므로 표시하고,
        // null일 때만 Cost 영역을 숨긴다.
        bool hasAbilityCost =
            tooltipViewData.energyCost.HasValue;

        if (costRoot != null)
        {
            costRoot.SetActive(
                hasAbilityCost
            );
        }

        // <변경부분>
        // 숫자는 TooltipViewData에서 전달된 값을 사용한다.
        // CostLabel은 별도의 TMP/Localization으로 관리한다.
        if (costValueText != null)
        {
            costValueText.text =
                hasAbilityCost
                    ? tooltipViewData.energyCost.Value.ToString()
                    : string.Empty;
        }

        // <변경부분> 현재 Tooltip에 포함된 Section 개수를 기록한다.
        // 팝업 전체가 길어진 만큼 기준 위치를 보정할 때 사용한다.
        currentSectionCount =
            GetValidSectionCount(
                tooltipViewData.sections
            );

        ResizeBaseTooltipToDescription();

        RefreshSections(
      tooltipViewData.sections
  );

        // <변경부분>
        // Section 생성 및 높이 계산이 끝난 뒤
        // SectionParent를 기본 Tooltip 바로 아래에 배치한다.
        //
        // SectionParent 내부의 개별 Section은
        // Vertical Layout Group이 자동 정렬한다.
        AlignSectionParentBelowBaseTooltip();

        // <변경부분>
        // 기본 Tooltip과 모든 Section의 위치가 확정된 뒤
        // 기존 커서 기준점 및 화면 경계 보정을 실행한다.
        //
        // 하단 아이콘에서는 마지막 Section의 실제 하단 Bounds가
        // 기준점으로 사용되도록 기존 알고리즘을 유지한다.
        SetPopupPosition(
            screenPosition,
            positionMode,
            customPositionOffset,
            fixedCanvasPosition,
            sectionPositionOffset.x,
            popupOffsetYPerSection
        );

        // 위치 계산이 모두 끝난 뒤 오픈 애니메이션을 실행한다.
        if (popupOpenAnimator != null)
        {
            popupOpenAnimator.PlayOpen();
        }
    }

    // <변경부분> 팝업을 숨김
    public void Hide()
    {
        // <변경부분> 다음 Tooltip에서 이전 Section 개수가
        // 위치 계산에 남지 않도록 초기화한다.
        currentSectionCount = 0;

        if (popupRoot != null)
        {
            popupRoot.gameObject.SetActive(false);
        }
    }

    // 하단 추가 설명 블록을 sections 순서대로 다시 생성한다.
    //
    // 기존 Section은 Destroy 예약만 해두면 현재 프레임 동안
    // Hierarchy와 Layout 계산에 남을 수 있으므로,
    // 먼저 비활성화한 뒤 Destroy하여 새 Section과 겹쳐 계산되지 않게 한다.
    private void RefreshSections(
        List<TooltipSectionData> sections)
    {
        if (sectionParent == null)
        {
            return;
        }

        // 기존에 생성되어 있던 Section 블록을 정리한다.
        //
        // Unity의 Destroy는 실제 삭제가 프레임 종료 시점에 처리되므로
        // 먼저 SetActive(false)를 적용하여 현재 프레임의
        // LayoutGroup / ContentSizeFitter 계산에서 즉시 제외한다.
        for (int i = sectionParent.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject sectionObject =
                sectionParent
                    .GetChild(i)
                    .gameObject;

            sectionObject.SetActive(
                false
            );

            Destroy(
                sectionObject
            );
        }

        if (sections == null)
        {
            return;
        }

        // TooltipData에 들어있는 순서대로
        // 새로운 Section 블록을 생성한다.
        for (int i = 0;
             i < sections.Count;
             i++)
        {
            TooltipSectionData sectionData =
                sections[i];

            if (sectionData == null)
            {
                continue;
            }

            // SectionType에 맞는 프리팹을 선택한다.
            TooltipSectionItemUI sectionPrefab =
                GetSectionPrefab(
                    sectionData.sectionType
                );

            if (sectionPrefab == null)
            {
                Debug.LogWarning(
                    $"Tooltip Section 프리팹을 찾지 못했습니다: " +
                    $"{sectionData.sectionType}"
                );

                continue;
            }

            // 새 Section을 현재 SectionParent 아래에 생성한다.
            TooltipSectionItemUI itemUI =
                Instantiate(
                    sectionPrefab,
                    sectionParent
                );

            itemUI.Refresh(
                sectionData
            );
        }

        // 새로 생성된 Section의 Layout을 먼저 확정한다.
        //
        // 이후 SetPopupPosition()에서 popupRoot 전체 Layout을 다시 계산하므로
        // SectionParent → PopupRoot 순서로 크기가 안정적으로 반영된다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(
      sectionParent
  );
    }


    // <변경부분>
    // 기본 Tooltip의 Background 바로 아래에
    // SectionParent의 상단을 자동으로 맞춘다.
    //
    // Section이 몇 개이든 기본 Tooltip 바로 아래에서 시작하며,
    // 개별 Section의 세로 배치는 Vertical Layout Group에 맡긴다.
    //
    // World Corner를 사용하므로 기존 RectTransform의
    // Anchor, Pivot, Scale 설정을 직접 변경하지 않는다.
    private void AlignSectionParentBelowBaseTooltip()
    {
        if (popupRoot == null ||
            sectionParent == null ||
            baseTooltipBackground == null)
        {
            return;
        }

        // <변경부분>
        // Section 및 기본 Tooltip의 최신 레이아웃을 확정한다.
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            sectionParent
        );

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            popupRoot
        );

        // <변경부분>
        // 기본 Tooltip Background와 SectionParent의
        // 실제 월드 좌표상 네 모서리를 가져온다.
        Vector3[] baseCorners = new Vector3[4];
        Vector3[] sectionCorners = new Vector3[4];

        baseTooltipBackground.GetWorldCorners(
            baseCorners
        );

        sectionParent.GetWorldCorners(
            sectionCorners
        );

        // <변경부분>
        // 서로 다른 Anchor/Pivot을 사용하더라도
        // SectionParent의 부모 좌표계로 통일하여 계산한다.
        Transform parent = sectionParent.parent;

        Vector3 baseBottomLeft =
            parent.InverseTransformPoint(
                baseCorners[0]
            );

        Vector3 baseBottomRight =
            parent.InverseTransformPoint(
                baseCorners[3]
            );

        Vector3 sectionTopLeft =
            parent.InverseTransformPoint(
                sectionCorners[1]
            );

        Vector3 sectionTopRight =
            parent.InverseTransformPoint(
                sectionCorners[2]
            );

        // <변경부분>
        // 기본 Tooltip의 하단 중앙을 수평 정렬 기준으로 사용한다.
        float baseCenterX =
            (baseBottomLeft.x + baseBottomRight.x) * 0.5f;

        float sectionCenterX =
            (sectionTopLeft.x + sectionTopRight.x) * 0.5f;

        // <변경부분>
        // 기본 Tooltip의 하단과 SectionParent 상단 사이에
        // Inspector에서 지정한 간격만 남긴다.
        float baseBottomY =
            (baseBottomLeft.y + baseBottomRight.y) * 0.5f;

        float sectionTopY =
            (sectionTopLeft.y + sectionTopRight.y) * 0.5f;

        float gap =
            Mathf.Max(0f, sectionGapBelowBase);

        Vector2 alignmentOffset =
            new Vector2(
                baseCenterX - sectionCenterX,
                baseBottomY - gap - sectionTopY
            );

        // <변경부분>
        // 계산된 차이만큼 SectionParent를 이동한다.
        //
        // 매번 현재 실제 모서리 좌표를 다시 계산하므로
        // 이전 Tooltip의 이동량이 누적되지 않는다.
        sectionParent.anchoredPosition +=
            alignmentOffset;
    }


    // <변경부분> Tooltip Popup 내부의 모든 UI Graphic을
    // Pointer Raycast 대상에서 제외한다.
    //
    // 현재 Tooltip은 정보 표시 전용이며
    // Button / Scroll / Drag 같은 직접 상호작용이 없으므로
    // Popup이 원본 TooltipTrigger의 Hover를 가로챌 필요가 없다.
    //
    // Runtime으로 생성되는 Section의 Image / TMP_Text까지
    // 모두 함께 처리한다.
    private void DisableTooltipRaycastTargets()
    {
        if (popupRoot == null)
        {
            return;
        }

        Graphic[] tooltipGraphics =
            popupRoot.GetComponentsInChildren<Graphic>(
                true
            );

        for (int i = 0;
             i < tooltipGraphics.Length;
             i++)
        {
            Graphic graphic =
                tooltipGraphics[i];

            if (graphic == null)
            {
                continue;
            }

            graphic.raycastTarget =
                false;
        }
    }

    // <변경부분> 실제로 생성 가능한 Section 데이터 개수를 반환한다.
    // null 데이터는 Section 위치 보정 개수에서 제외한다.
    private int GetValidSectionCount(
        List<TooltipSectionData> sections)
    {
        if (sections == null)
        {
            return 0;
        }

        int validCount = 0;

        for (int i = 0;
             i < sections.Count;
             i++)
        {
            if (sections[i] != null)
            {
                validCount++;
            }
        }

        return validCount;
    }

    // <변경부분> TooltipTrigger에서 전달된 Offset을
    // SectionParent의 Anchored Position에 직접 적용한다.
    private void ApplySectionPositionOffset(
        Vector2 sectionPositionOffset)
    {
        if (sectionParent == null)
        {
            Debug.LogWarning(
                "[Tooltip] SectionParent가 연결되지 않았습니다."
            );

            return;
        }

        sectionParent.anchoredPosition =
            defaultSectionParentAnchoredPosition +
            sectionPositionOffset;
    }

    // <변경부분> SectionType에 맞는 Section 프리팹을 찾아 반환
    private TooltipSectionItemUI GetSectionPrefab(TooltipSectionType sectionType)
    {
        if (sectionPrefabs != null)
        {
            for (int i = 0; i < sectionPrefabs.Length; i++)
            {
                if (sectionPrefabs[i] == null)
                {
                    continue;
                }

                if (sectionPrefabs[i].sectionType == sectionType)
                {
                    return sectionPrefabs[i].sectionPrefab;
                }
            }
        }

        // 등록된 타입별 프리팹이 없으면 기본 프리팹 사용
        return defaultSectionPrefab;
    }

    // <변경부분> 위치 모드에 따라
    // 포인터 기준 자동 위치 또는 Canvas 기준 고정 위치를 적용한다.
    //
    // 기본 PopupRoot Rect 크기만 사용하는 대신,
    // 실제 생성된 모든 Section까지 포함한 전체 표시 Bounds를 기준으로 배치한다.
    private void SetPopupPosition(
       Vector2 screenPosition,
       TooltipPositionMode positionMode,
       Vector2 customPositionOffset,
       Vector2 fixedCanvasPosition,
       float popupOffsetXPerSection,
       float popupOffsetYPerSection)
    {
        if (rootCanvas == null ||
            popupRoot == null)
        {
            return;
        }

        RectTransform canvasRect =
            rootCanvas.transform as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        // Tooltip 및 Section의 최신 크기를 Layout에 반영한다.
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            popupRoot
        );

        // <변경부분> 고정 위치 Tooltip은 기존 고정 좌표를 유지한 뒤,
        // 실제 Tooltip 전체 Bounds가 화면 밖으로 나가는 경우에만 보정한다.
        if (positionMode ==
            TooltipPositionMode.FixedCanvasPosition)
        {
            popupRoot.pivot =
                new Vector2(
                    Mathf.Clamp01(
                        fixedPositionPivot.x
                    ),
                    Mathf.Clamp01(
                        fixedPositionPivot.y
                    )
                );

            popupRoot.anchoredPosition =
                fixedCanvasPosition;

            ClampPopupVisualBoundsToCanvas(
                canvasRect
            );

            return;
        }

        Vector2 localPoint;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                rootCanvas.renderMode ==
                    RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera,
                out localPoint
            );

        // 포인터가 화면 오른쪽에 있으면
        // Tooltip을 포인터의 왼쪽 방향으로 배치한다.
        bool isRightSide =
            localPoint.x >= 0f;

        // 포인터가 화면 위쪽에 있으면
        // Tooltip을 포인터의 아래쪽 방향으로 배치한다.
        bool isTopSide =
            localPoint.y >= 0f;

        popupRoot.pivot =
            new Vector2(
                isRightSide ? 1f : 0f,
                isTopSide ? 1f : 0f
            );

        // <변경부분> 기존 Trigger별 Section 보정값도 유지한다.
        // 필요하지 않은 Tooltip에서는 Inspector 값을 0으로 두면 된다.
        float dynamicOffsetY =
            popupOffset.y +
            (
                currentSectionCount *
                popupOffsetYPerSection
            );

        // <변경부분>
        // Section 개수에 따라 PopupRoot 전체 위치를
        // X / Y 양쪽으로 추가 보정한다.
        //
        // 이 방식은 SectionParent만 따로 움직이지 않기 때문에
        // 기본 Tooltip과 하단 Section이 항상 함께 이동한다.
        float dynamicOffsetX =
            popupOffset.x +
            (
                currentSectionCount *
                popupOffsetXPerSection
            );


        float offsetX =
            isRightSide
                ? -dynamicOffsetX
                : dynamicOffsetX;

        float offsetY =
            isTopSide
                ? -dynamicOffsetY
                : dynamicOffsetY;

        // Tooltip 전체 Bounds가 맞춰질 기준 위치.
        Vector2 referencePosition =
            localPoint +
            new Vector2(
                offsetX,
                offsetY
            ) +
            customPositionOffset;

        popupRoot.anchoredPosition =
            referencePosition;

        // Section의 실제 위치와 ContentSizeFitter 결과까지 반영한다.
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            popupRoot
        );

        // <변경부분> 기본 Tooltip뿐 아니라
        // 마지막 Section까지 포함한 실제 전체 Bounds를 계산한다.
        Bounds visualBounds =
            RectTransformUtility
                .CalculateRelativeRectTransformBounds(
                    canvasRect,
                    popupRoot
                );

        Vector2 visualBoundsAlignment =
            Vector2.zero;

        // 좌/우 방향 역시 전체 Tooltip의 실제 끝 모서리를 기준으로 맞춘다.
        visualBoundsAlignment.x =
            isRightSide
                ? referencePosition.x -
                  visualBounds.max.x
                : referencePosition.x -
                  visualBounds.min.x;

        // <변경부분> 화면 아래쪽에서는 기본 Tooltip 하단이 아니라
        // 마지막 Section까지 포함한 실제 전체 Tooltip의 최하단을 기준점으로 사용한다.
        //
        // 따라서 Player Status처럼 화면 하단에 있는 Tooltip도
        // 추가 Section이 화면 밖으로 잘리지 않는 위치에서 열린다.
        visualBoundsAlignment.y =
            isTopSide
                ? referencePosition.y -
                  visualBounds.max.y
                : referencePosition.y -
                  visualBounds.min.y;

        popupRoot.anchoredPosition +=
            visualBoundsAlignment;

        // 마지막으로 전체 Tooltip Bounds가 화면을 벗어나는 경우
        // 필요한 거리만큼 Canvas 안쪽으로 보정한다.
        ClampPopupVisualBoundsToCanvas(
            canvasRect
        );
    }

    // <변경부분> 기본 Tooltip과 모든 추가 Section을 포함한
    // 실제 시각적 Bounds가 Canvas 영역 밖으로 나가지 않도록 보정한다.
    private void ClampPopupVisualBoundsToCanvas(
        RectTransform canvasRect)
    {
        if (canvasRect == null ||
            popupRoot == null)
        {
            return;
        }

        // Section 텍스트 길이와 ContentSizeFitter 결과까지
        // 현재 프레임의 RectTransform 크기에 확실하게 반영한다.
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            popupRoot
        );

        Bounds visualBounds =
            RectTransformUtility
                .CalculateRelativeRectTransformBounds(
                    canvasRect,
                    popupRoot
                );

        float allowedMinX =
            canvasRect.rect.xMin +
            screenPadding.x;

        float allowedMaxX =
            canvasRect.rect.xMax -
            screenPadding.x;

        float allowedMinY =
            canvasRect.rect.yMin +
            screenPadding.y;

        float allowedMaxY =
            canvasRect.rect.yMax -
            screenPadding.y;

        Vector2 correction =
            Vector2.zero;

        // Tooltip 전체가 Canvas보다 작은 일반적인 경우에는
        // 넘쳐난 방향만큼 정확하게 안쪽으로 이동시킨다.
        float availableWidth =
            allowedMaxX -
            allowedMinX;

        if (visualBounds.size.x <=
            availableWidth)
        {
            if (visualBounds.min.x <
                allowedMinX)
            {
                correction.x =
                    allowedMinX -
                    visualBounds.min.x;
            }
            else if (visualBounds.max.x >
                     allowedMaxX)
            {
                correction.x =
                    allowedMaxX -
                    visualBounds.max.x;
            }
        }
        else
        {
            // Tooltip 자체가 Canvas보다 넓은 예외 상황에서는
            // 한쪽으로 치우치지 않도록 허용 영역 중앙에 맞춘다.
            correction.x =
                (
                    allowedMinX +
                    allowedMaxX
                ) * 0.5f -
                visualBounds.center.x;
        }

        float availableHeight =
            allowedMaxY -
            allowedMinY;

        if (visualBounds.size.y <=
            availableHeight)
        {
            if (visualBounds.min.y <
                allowedMinY)
            {
                correction.y =
                    allowedMinY -
                    visualBounds.min.y;
            }
            else if (visualBounds.max.y >
                     allowedMaxY)
            {
                correction.y =
                    allowedMaxY -
                    visualBounds.max.y;
            }
        }
        else
        {
            // Tooltip 자체가 Canvas보다 높은 예외 상황에서도
            // 가능한 범위 안에서 중앙에 위치시킨다.
            correction.y =
                (
                    allowedMinY +
                    allowedMaxY
                ) * 0.5f -
                visualBounds.center.y;
        }

        popupRoot.anchoredPosition +=
            correction;
    }
}
