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

        // <변경부분> Section 생성 후 Tooltip 전체 레이아웃과
        // 기본 팝업 위치를 먼저 확정한다.
        //
        // SetPopupPosition 내부에서 LayoutRebuilder가 실행되므로
        // Section 전용 Offset보다 먼저 호출해야 한다.
        SetPopupPosition(
     screenPosition,
     positionMode,
     customPositionOffset,
     fixedCanvasPosition,
     popupOffsetYPerSection
 );

        // 오픈 애니메이션에서 초기 RectTransform 상태를
        // 먼저 적용하도록 애니메이션을 선행 실행한다.
        if (popupOpenAnimator != null)
        {
            popupOpenAnimator.PlayOpen();
        }

        // <변경부분> 오픈 애니메이션 초기화 이후
        // SectionParent에만 Trigger별 위치 보정값을 적용한다.
        ApplySectionPositionOffset(
            sectionPositionOffset
        );
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

        // Tooltip 내용 길이에 따라 PopupRoot 크기를 먼저 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            popupRoot
        );

        Vector2 targetPosition;

        // <변경부분> 고정 위치 모드에서는
        // 포인터 위치와 사분면 계산을 사용하지 않는다.
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

            targetPosition =
                fixedCanvasPosition;
        }
        else
        {
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

            // 커서가 화면 오른쪽 절반에 있는지 확인
            bool isRightSide =
                localPoint.x >= 0f;

            // 커서가 화면 위쪽 절반에 있는지 확인
            bool isTopSide =
                localPoint.y >= 0f;

            // 포인터 위치에 따라 팝업 Pivot 결정
            popupRoot.pivot =
                new Vector2(
                    isRightSide ? 1f : 0f,
                    isTopSide ? 1f : 0f
                );

            // <변경부분> Section이 추가되어 팝업 전체 높이가 길어지면
            // Section 개수에 비례해 PopupRoot 전체 기준 위치를 보정한다.
            //
            // SectionParent 자체의 미세 위치는 별도의
            // sectionPositionOffset으로 처리한다.
            // <변경부분> 공통값이 아니라 현재 TooltipTrigger에서 전달한
            // Section 1개당 PopupRoot 보정값을 사용한다.
            float dynamicOffsetY =
                popupOffset.y +
                (
                    currentSectionCount *
                    popupOffsetYPerSection
                );

            // 포인터가 있는 사분면의 반대 방향으로 이동
            float offsetX =
                isRightSide
                    ? -popupOffset.x
                    : popupOffset.x;

            float offsetY =
                isTopSide
                    ? -dynamicOffsetY
                    : dynamicOffsetY;

            // 기존 자동 위치에 Trigger별 개별 Offset 적용
            targetPosition =
                localPoint +
                new Vector2(
                    offsetX,
                    offsetY
                ) +
                customPositionOffset;
        }

        float halfCanvasWidth =
            canvasRect.rect.width * 0.5f;

        float halfCanvasHeight =
            canvasRect.rect.height * 0.5f;

        float popupWidth =
            popupRoot.rect.width;

        float popupHeight =
            popupRoot.rect.height;

        // <변경부분> 자동 위치와 고정 위치 모두
        // 팝업이 화면 밖으로 나가지 않도록 최종 보정한다.
        float minX =
            -halfCanvasWidth +
            screenPadding.x +
            popupWidth *
            popupRoot.pivot.x;

        float maxX =
            halfCanvasWidth -
            screenPadding.x -
            popupWidth *
            (
                1f -
                popupRoot.pivot.x
            );

        float minY =
            -halfCanvasHeight +
            screenPadding.y +
            popupHeight *
            popupRoot.pivot.y;

        float maxY =
            halfCanvasHeight -
            screenPadding.y -
            popupHeight *
            (
                1f -
                popupRoot.pivot.y
            );

        targetPosition.x =
            Mathf.Clamp(
                targetPosition.x,
                minX,
                maxX
            );

        targetPosition.y =
            Mathf.Clamp(
                targetPosition.y,
                minY,
                maxY
            );

        popupRoot.anchoredPosition =
            targetPosition;
    }
}
