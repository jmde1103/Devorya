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
    [SerializeField] private Vector2 screenPadding = new Vector2(40f, 40f);

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

        RefreshSections(tooltipViewData.sections);
        SetPopupPosition(screenPosition);
    }

    // <변경부분> 팝업을 숨김
    public void Hide()
    {
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

    // <변경부분> 누른 위치 옆에 팝업을 배치하고 화면 밖으로 나가지 않게 보정
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRoot);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out localPoint
        );

        Vector2 targetPosition = localPoint + popupOffset;

        float halfCanvasWidth = canvasRect.rect.width * 0.5f;
        float halfCanvasHeight = canvasRect.rect.height * 0.5f;

        float popupWidth = popupRoot.rect.width;
        float popupHeight = popupRoot.rect.height;

        float minX = -halfCanvasWidth + screenPadding.x + popupWidth * popupRoot.pivot.x;
        float maxX = halfCanvasWidth - screenPadding.x - popupWidth * (1f - popupRoot.pivot.x);
        float minY = -halfCanvasHeight + screenPadding.y + popupHeight * popupRoot.pivot.y;
        float maxY = halfCanvasHeight - screenPadding.y - popupHeight * (1f - popupRoot.pivot.y);

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        popupRoot.anchoredPosition = targetPosition;
    }
}
