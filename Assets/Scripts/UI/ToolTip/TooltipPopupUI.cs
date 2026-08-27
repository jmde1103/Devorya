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

        // <변경부분> 현재 Tooltip에 포함된 Section 개수를 기록한다.
        // 팝업 전체가 길어진 만큼 기준 위치를 보정할 때 사용한다.
        currentSectionCount =
            GetValidSectionCount(
                tooltipViewData.sections
            );

        RefreshSections(
      tooltipViewData.sections
  );

        // <변경부분> Section의 최종 위치를 먼저 확정한다.
        // Tooltip 위치 계산에서 마지막 Section까지 포함한 실제 Bounds를
        // 사용할 수 있도록 SetPopupPosition보다 먼저 적용한다.
        ApplySectionPositionOffset(
            sectionPositionOffset
        );

        // <변경부분> 기본 Tooltip뿐 아니라 생성된 모든 Section까지 포함한
        // 실제 표시 영역을 기준으로 최종 팝업 위치를 계산한다.
        SetPopupPosition(
            screenPosition,
            positionMode,
            customPositionOffset,
            fixedCanvasPosition,
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

        float offsetX =
            isRightSide
                ? -popupOffset.x
                : popupOffset.x;

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
