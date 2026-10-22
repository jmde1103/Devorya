using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분>
// 게임오버 전용 화면 손상 연출.
// 검은 화면의 수평 분할 전환과
// TMP 문자의 가로 슬라이스 찢김을 담당한다.
public class GameOverCorruptionFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Image blackoutImage;

    [SerializeField]
    private TextMeshProUGUI mainMessageText;

    [Header("Blackout Corruption")]
    [SerializeField, Range(8, 40)]
    private int blackoutBandCount = 28;

    [SerializeField, Range(0f, 0.5f)]
    private float blackoutRandomness = 0.24f;

    [Header("Text Tearing")]
    [SerializeField, Range(4, 24)]
    private int textSliceCount = 12;

    [SerializeField, Range(10f, 300f)]
    private float textSliceMaxOffset = 125f;

    [SerializeField, Min(0.01f)]
    private float corruptionFrameInterval = 0.045f;

    private Image[] blackoutBands;
    private TextMeshProUGUI[] textSlices;

    private Color originalBlackoutColor;

    private bool isTextTearing;
    private bool isBackgroundBurst;

    private float nextTextUpdateTime;
    private float nextBackgroundUpdateTime;


    private void Awake()
    {
        if (blackoutImage != null)
        {
            originalBlackoutColor =
                blackoutImage.color;

            CreateBlackoutBands();
        }

        if (mainMessageText != null)
        {
            CreateTextSlices();
        }
    }


    private void CreateBlackoutBands()
    {
        int count = Mathf.Max(
            1,
            blackoutBandCount
        );

        blackoutBands = new Image[count];

        RectTransform parentRect =
            blackoutImage.rectTransform;

        for (int i = 0; i < count; i++)
        {
            GameObject bandObject =
                new GameObject(
                    "BlackoutCorruptionBand_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            RectTransform rect =
                bandObject.GetComponent<RectTransform>();

            rect.SetParent(parentRect, false);

            float bottom = (float)i / count;
            float top = (float)(i + 1) / count;

            rect.anchorMin =
                new Vector2(0f, bottom);

            rect.anchorMax =
                new Vector2(1f, top);

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image =
                bandObject.GetComponent<Image>();

            image.color = Color.black;
            image.raycastTarget = false;
            image.enabled = false;

            blackoutBands[i] = image;
        }
    }


    // <변경부분>
    // 부드러운 Fade 대신 수평 밴드 방식으로
    // 검은 화면이 침식되도록 준비한다.
    public void BeginBlackout()
    {
        if (blackoutImage == null)
        {
            return;
        }

        Color invisibleColor =
            originalBlackoutColor;

        invisibleColor.a = 0f;
        blackoutImage.color = invisibleColor;

        SetAllBlackoutBandsVisible(false);
    }


    public void SetBlackoutProgress(float progress)
    {
        if (blackoutBands == null)
        {
            return;
        }

        float p = Mathf.Clamp01(progress);

        for (int i = 0; i < blackoutBands.Length; i++)
        {
            Image band = blackoutBands[i];

            float randomProgress =
                Mathf.Clamp01(
                    p + Random.Range(
                        -blackoutRandomness,
                        blackoutRandomness
                    )
                );

            bool visible =
                p >= 0.98f ||
                Random.value < randomProgress;

            band.enabled = visible;

            if (!visible)
            {
                continue;
            }

            RectTransform rect =
                band.rectTransform;

            // <변경부분>
            // 각 가로 줄의 시작점과 끝점을 흔들어
            // 화면이 수평으로 찢어지는 느낌을 만든다.
            if (p >= 0.98f)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.offsetMin =
                    new Vector2(
                        Random.Range(-100f, 80f),
                        0f
                    );

                rect.offsetMax =
                    new Vector2(
                        Random.Range(-80f, 100f),
                        0f
                    );
            }

            band.color = Color.black;
        }
    }


    public void CompleteBlackout()
    {
        if (blackoutImage != null)
        {
            blackoutImage.color =
                originalBlackoutColor;
        }

        SetAllBlackoutBandsVisible(false);
    }


    // <변경부분>
    // NO SIGNAL이 표시되는 동안에도
    // 검은 배경에 불규칙한 신호 오류를 발생시킨다.
    public void SetBackgroundBurst(bool active)
    {
        if (blackoutBands == null)
        {
            return;
        }

        if (!active)
        {
            if (!isBackgroundBurst)
            {
                return;
            }

            isBackgroundBurst = false;
            SetAllBlackoutBandsVisible(false);
            return;
        }

        isBackgroundBurst = true;

        if (Time.unscaledTime <
            nextBackgroundUpdateTime)
        {
            return;
        }

        nextBackgroundUpdateTime =
            Time.unscaledTime +
            Mathf.Max(0.01f, corruptionFrameInterval);

        for (int i = 0; i < blackoutBands.Length; i++)
        {
            Image band = blackoutBands[i];

            band.enabled =
                Random.value < 0.42f;

            if (!band.enabled)
            {
                continue;
            }

            float brightness =
                Random.Range(0.25f, 0.9f);

            band.color =
                new Color(
                    brightness,
                    brightness,
                    brightness,
                    Random.Range(0.15f, 0.55f)
                );

            RectTransform rect =
                band.rectTransform;

            rect.offsetMin =
                new Vector2(
                    Random.Range(-180f, 40f),
                    0f
                );

            rect.offsetMax =
                new Vector2(
                    Random.Range(-40f, 180f),
                    0f
                );
        }
    }


    private void SetAllBlackoutBandsVisible(
        bool visible)
    {
        if (blackoutBands == null)
        {
            return;
        }

        for (int i = 0; i < blackoutBands.Length; i++)
        {
            blackoutBands[i].enabled = visible;
        }
    }


    // <변경부분>
    // 메인 TMP를 여러 수평 영역으로 복제한다.
    // RectMask2D가 각 영역 밖의 글자를 잘라낸다.
    private void CreateTextSlices()
    {
        int count = Mathf.Max(
            1,
            textSliceCount
        );

        textSlices = new TextMeshProUGUI[count];

        RectTransform parentRect =
            mainMessageText.rectTransform;

        for (int i = 0; i < count; i++)
        {
            GameObject maskObject =
                new GameObject(
                    "TextTearMask_" + i,
                    typeof(RectTransform),
                    typeof(RectMask2D)
                );

            RectTransform maskRect =
                maskObject.GetComponent<RectTransform>();

            maskRect.SetParent(
                parentRect,
                false
            );

            maskRect.anchorMin =
                new Vector2(
                    0f,
                    (float)i / count
                );

            maskRect.anchorMax =
                new Vector2(
                    1f,
                    (float)(i + 1) / count
                );

            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;

            GameObject textObject =
                new GameObject(
                    "TextTearPart_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)
                );

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();

            textRect.SetParent(maskRect, false);

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.right;
            textRect.pivot = new Vector2(0.5f, 0f);

            TextMeshProUGUI sliceText =
                textObject.GetComponent<TextMeshProUGUI>();

            // 기존 TMP의 폰트·스타일·정렬을 유지한다.
            sliceText.font = mainMessageText.font;
            sliceText.fontSharedMaterial =
                mainMessageText.fontSharedMaterial;

            sliceText.fontSize = mainMessageText.fontSize;
            sliceText.fontStyle = mainMessageText.fontStyle;
            sliceText.alignment = mainMessageText.alignment;
            sliceText.enableAutoSizing =
                mainMessageText.enableAutoSizing;

            sliceText.fontSizeMin =
                mainMessageText.fontSizeMin;

            sliceText.fontSizeMax =
                mainMessageText.fontSizeMax;

            sliceText.margin = mainMessageText.margin;
            sliceText.color = mainMessageText.color;
            sliceText.raycastTarget = false;

            sliceText.text = mainMessageText.text;
            sliceText.enabled = false;

            textSlices[i] = sliceText;
        }
    }


    public void SetTextTearing(bool active)
    {
        if (mainMessageText == null ||
            textSlices == null)
        {
            return;
        }

        if (!active)
        {
            if (!isTextTearing)
            {
                return;
            }

            isTextTearing = false;
            mainMessageText.enabled = true;

            for (int i = 0; i < textSlices.Length; i++)
            {
                textSlices[i].enabled = false;
            }

            return;
        }

        if (!isTextTearing)
        {
            isTextTearing = true;
            mainMessageText.enabled = false;

            for (int i = 0; i < textSlices.Length; i++)
            {
                textSlices[i].enabled = true;
            }

            nextTextUpdateTime = 0f;
        }

        if (Time.unscaledTime < nextTextUpdateTime)
        {
            return;
        }

        nextTextUpdateTime =
            Time.unscaledTime +
            Mathf.Max(0.01f, corruptionFrameInterval);

        // <변경부분>
        // 매 Burst마다 현재 Locale의 메인 문구를 복사한다.
        // 기존 Localize String Event는 메인 TMP만 관리한다.
        float height = Mathf.Max(
            1f,
            mainMessageText.rectTransform.rect.height
        );

        float sliceHeight =
            height / textSlices.Length;

        for (int i = 0; i < textSlices.Length; i++)
        {
            TextMeshProUGUI slice = textSlices[i];

            slice.text = mainMessageText.text;
            slice.color = mainMessageText.color;

            RectTransform rect =
                slice.rectTransform;

            rect.sizeDelta = new Vector2(
                0f,
                height
            );

            // <변경부분>
            // 글자의 세로 영역을 유지하면서
            // 각 가로 조각만 좌우로 크게 이동한다.
            float horizontalOffset =
                Random.Range(
                    -textSliceMaxOffset,
                    textSliceMaxOffset
                );

            rect.anchoredPosition =
                new Vector2(
                    horizontalOffset,
                    -i * sliceHeight
                );
        }
    }


    private void OnDisable()
    {
        SetTextTearing(false);
        SetBackgroundBurst(false);
        CompleteBlackout();
    }
}
