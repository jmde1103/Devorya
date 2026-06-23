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

    [Header("Section")]
    [SerializeField] private Transform sectionParent;
    [SerializeField] private TooltipSectionItemUI sectionItemPrefab;

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

    // <변경부분> TooltipData를 받아 팝업을 표시
    public void Show(TooltipData tooltipData, Vector2 screenPosition)
    {
        if (tooltipData == null || popupRoot == null)
        {
            Hide();
            return;
        }

        popupRoot.gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = tooltipData.title;
        }

        if (categoryText != null)
        {
            categoryText.text = tooltipData.category;
        }

        if (mainDescriptionText != null)
        {
            mainDescriptionText.text = tooltipData.mainDescription;
        }

        RefreshSections(tooltipData);
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

    // <변경부분> 하단 추가 설명 블록을 TooltipData 기준으로 다시 생성
    private void RefreshSections(TooltipData tooltipData)
    {
        if (sectionParent == null || sectionItemPrefab == null)
        {
            return;
        }

        for (int i = sectionParent.childCount - 1; i >= 0; i--)
        {
            Destroy(sectionParent.GetChild(i).gameObject);
        }

        if (tooltipData.sections == null)
        {
            return;
        }

        for (int i = 0; i < tooltipData.sections.Count; i++)
        {
            TooltipSectionItemUI itemUI = Instantiate(sectionItemPrefab, sectionParent);
            itemUI.Refresh(tooltipData.sections[i]);
        }
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
