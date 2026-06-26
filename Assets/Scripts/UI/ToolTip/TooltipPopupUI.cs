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
    }

    // <변경부분> TooltipData 에셋을 받아 TooltipViewData로 변환 후 팝업 표시
    public void Show(TooltipData tooltipData, Vector2 screenPosition)
    {
        Show(TooltipViewData.FromTooltipData(tooltipData), screenPosition);
    }

    // <변경부분> 실제 표시용 TooltipViewData를 받아 팝업 표시
    public void Show(TooltipViewData tooltipViewData, Vector2 screenPosition)
    {
        if (tooltipViewData == null || popupRoot == null)
        {
            Hide();
            return;
        }

        popupRoot.gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = tooltipViewData.title;
        }

        if (categoryText != null)
        {
            categoryText.text = tooltipViewData.category;
        }

        if (mainDescriptionText != null)
        {
            mainDescriptionText.text = tooltipViewData.mainDescription;
        }
        // <변경부분> 현재 Tooltip에 붙는 Section 블록 개수를 기록
        currentSectionCount = tooltipViewData.sections != null ? tooltipViewData.sections.Count : 0;

        RefreshSections(tooltipViewData.sections);
        SetPopupPosition(screenPosition);
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

    // <변경부분> 화면 4분할 기준으로 Tooltip 팝업의 코너가 커서 위치에 오도록 배치
    private void SetPopupPosition(Vector2 screenPosition)
    {
        if (rootCanvas == null || popupRoot == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        // Tooltip 내용 길이에 따라 PopupRoot 크기를 먼저 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRoot);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out localPoint
        );

        // <변경부분> 커서가 화면 오른쪽 절반에 있는지 확인
        bool isRightSide = localPoint.x >= 0f;

        // <변경부분> 커서가 화면 위쪽 절반에 있는지 확인
        bool isTopSide = localPoint.y >= 0f;

        // <변경부분> 커서가 있는 위치에 따라 팝업의 코너 Pivot을 결정
        // 오른쪽 아래: pivot = (1, 0) → 오른쪽 아래 코너가 커서에 붙고 왼쪽 위로 펼쳐짐
        // 오른쪽 위:   pivot = (1, 1) → 오른쪽 위 코너가 커서에 붙고 왼쪽 아래로 펼쳐짐
        // 왼쪽 위:     pivot = (0, 1) → 왼쪽 위 코너가 커서에 붙고 오른쪽 아래로 펼쳐짐
        // 왼쪽 아래:   pivot = (0, 0) → 왼쪽 아래 코너가 커서에 붙고 오른쪽 위로 펼쳐짐
        popupRoot.pivot = new Vector2(
            isRightSide ? 1f : 0f,
            isTopSide ? 1f : 0f
        );

        // <변경부분> 팝업 코너를 커서 위치에 맞춤
        // popupOffset은 커서와 팝업이 완전히 겹치지 않도록 아주 살짝만 밀어내는 용도
        // <변경부분> Section 블록 개수만큼 Y Offset을 추가
        float dynamicOffsetY = popupOffset.y + (currentSectionCount * additionalOffsetYPerSection);

        // <변경부분> 커서가 있는 사분면의 반대 방향으로 팝업을 밀어낸다.
        // X는 기존 Offset 그대로 사용
        float offsetX = isRightSide ? -popupOffset.x : popupOffset.x;

        // <변경부분> Y는 Section 개수에 따라 증가한 Offset 사용
        // 위쪽 UI에서는 아래로, 아래쪽 UI에서는 위로 밀어낸다.
        float offsetY = isTopSide ? -dynamicOffsetY : dynamicOffsetY;

        Vector2 targetPosition = localPoint + new Vector2(offsetX, offsetY);

        float halfCanvasWidth = canvasRect.rect.width * 0.5f;
        float halfCanvasHeight = canvasRect.rect.height * 0.5f;

        float popupWidth = popupRoot.rect.width;
        float popupHeight = popupRoot.rect.height;

        // <변경부분> Pivot 기준으로 팝업이 화면 밖으로 나가지 않도록 보정
        float minX = -halfCanvasWidth + screenPadding.x + popupWidth * popupRoot.pivot.x;
        float maxX = halfCanvasWidth - screenPadding.x - popupWidth * (1f - popupRoot.pivot.x);
        float minY = -halfCanvasHeight + screenPadding.y + popupHeight * popupRoot.pivot.y;
        float maxY = halfCanvasHeight - screenPadding.y - popupHeight * (1f - popupRoot.pivot.y);

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        popupRoot.anchoredPosition = targetPosition;
    }
}
