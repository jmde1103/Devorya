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
    [SerializeField] private Transform sectionParent;

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

    // <변경부분> Section 블록이 1개 추가될 때마다 Tooltip 위치를 Y축으로 더 밀어낼 값
    [SerializeField] private float additionalOffsetYPerSection = 100f;

    // <변경부분> 현재 표시 중인 Tooltip의 Section 블록 개수
    private int currentSectionCount;


    private void Awake()
    {
        Instance = this;

        if (popupRoot != null)
        {
            popupRoot.gameObject.SetActive(false);
        }

        if (popupOpenAnimator == null)
        {
            popupOpenAnimator = GetComponent<PopupOpenAnimator>();
        }
    }

    // <변경부분> TooltipData 에셋을 받아 TooltipViewData로 변환 후
    // 공통 위치 설정으로 팝업을 표시한다.
    public void Show(
        TooltipData tooltipData,
        Vector2 screenPosition)
    {
        Show(
            TooltipViewData.FromTooltipData(
                tooltipData
            ),
            screenPosition,
            Vector2.zero
        );
    }

    // <변경부분> 기존 호출부와의 호환성을 유지한다.
    // 이 함수로 호출하면 기존 PointerOffset 방식으로 표시한다.
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
            Vector2.zero
        );
    }

    // <변경부분> Tooltip 위치 모드와
    // 개별 Offset 또는 고정 Canvas 위치를 받아 팝업을 표시한다.
    public void Show(
        TooltipViewData tooltipViewData,
        Vector2 screenPosition,
        TooltipPositionMode positionMode,
        Vector2 customPositionOffset,
        Vector2 fixedCanvasPosition)
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

        // 현재 Tooltip에 붙는 Section 블록 개수를 기록
        currentSectionCount =
            tooltipViewData.sections != null
                ? tooltipViewData.sections.Count
                : 0;

        RefreshSections(
            tooltipViewData.sections
        );

        // <변경부분> 위치 모드에 따라
        // 자동 위치 또는 고정 Canvas 위치를 적용한다.
        SetPopupPosition(
            screenPosition,
            positionMode,
            customPositionOffset,
            fixedCanvasPosition
        );

        if (popupOpenAnimator != null)
        {
            popupOpenAnimator.PlayOpen();
        }
    }

    // <변경부분> 팝업을 숨김
    public void Hide()
    {
        // <변경부분> Tooltip이 꺼질 때 Section 개수 기록 초기화
        currentSectionCount = 0;

        if (popupRoot != null)
        {
            popupRoot.gameObject.SetActive(false);
        }
    }

    // <변경부분> 하단 추가 설명 블록을 sections 순서대로 다시 생성
    private void RefreshSections(List<TooltipSectionData> sections)
    {
        if (sectionParent == null)
        {
            return;
        }

        // 기존에 생성되어 있던 Section 블록 제거
        for (int i = sectionParent.childCount - 1; i >= 0; i--)
        {
            Destroy(sectionParent.GetChild(i).gameObject);
        }

        if (sections == null)
        {
            return;
        }

        // TooltipData에 들어있는 순서대로 Section 블록 생성
        for (int i = 0; i < sections.Count; i++)
        {
            TooltipSectionData sectionData = sections[i];

            if (sectionData == null)
            {
                continue;
            }

            // <변경부분> SectionType에 맞는 프리팹 선택
            TooltipSectionItemUI sectionPrefab = GetSectionPrefab(sectionData.sectionType);

            if (sectionPrefab == null)
            {
                Debug.LogWarning($"Tooltip Section 프리팹을 찾지 못했습니다: {sectionData.sectionType}");
                continue;
            }

            // <변경부분> 선택한 Section 프리팹을 SectionParent 아래에 순서대로 생성
            TooltipSectionItemUI itemUI = Instantiate(sectionPrefab, sectionParent);
            itemUI.Refresh(sectionData);
        }
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
        Vector2 fixedCanvasPosition)
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

            // Section 블록 개수에 따라 Y Offset 증가
            float dynamicOffsetY =
                popupOffset.y +
                (
                    currentSectionCount *
                    additionalOffsetYPerSection
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
