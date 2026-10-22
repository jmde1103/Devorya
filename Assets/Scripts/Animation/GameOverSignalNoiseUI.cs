using System;
using UnityEngine;
using UnityEngine.UI;

// <변경부분>
// 게임오버 Canvas 위에 CRT 신호 잡음과
// 불규칙한 수평 찢김 형태의 노이즈를 출력한다.
//
// RawImage와 Texture는 런타임에 자동 생성한다.
// 기존 글리치 Shader/Material/Popup 효과를 변경하지 않는다.
public class GameOverSignalNoiseUI : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField, Range(8f, 40f)]
    private float framesPerSecond = 22f;

    [SerializeField, Range(0f, 1f)]
    private float noiseDensity = 0.55f;

    [SerializeField, Range(0f, 1f)]
    private float tearLineChance = 0.12f;

    // <변경부분>
    // RGB 라인 노이즈가 등장할 비율.
    // 값이 낮을수록 흰색 라인 위주가 된다.
    [SerializeField, Range(0f, 1f)]
    private float rgbLineRatio = 0.12f;

    // <변경부분>
    // RGB 노이즈 줄무늬의 최대 가로 길이 비율.
    // 0.35는 전체 화면 가로폭의 최대 35%를 의미한다.
    [SerializeField, Range(0.05f, 1f)]
    private float tearLineLengthRatio = 0.35f;

    private const int TextureWidth = 192;

    // <변경부분>
    // 세로 해상도를 2배로 높여
    // 가로 줄무늬의 두께를 기존의 절반으로 줄인다.
    private const int TextureHeight = 216;

    private readonly System.Random noiseRandom =
        new System.Random();

    private Texture2D noiseTexture;
    private Color32[] noisePixels;
    private RawImage noiseImage;

    private bool isVisible;
    private float intensity = 1f;
    private float elapsedTime;


    private void Awake()
    {
        // <변경부분>
        // 전체 Canvas를 덮는 노이즈 RawImage를
        // 마지막 자식으로 자동 생성한다.
        GameObject noiseObject =
            new GameObject(
                "SignalNoiseOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage)
            );

        RectTransform noiseRect =
            noiseObject.GetComponent<RectTransform>();

        noiseRect.SetParent(transform, false);

        noiseRect.anchorMin = Vector2.zero;
        noiseRect.anchorMax = Vector2.one;
        noiseRect.offsetMin = Vector2.zero;
        noiseRect.offsetMax = Vector2.zero;

        noiseRect.SetAsLastSibling();

        noiseImage =
            noiseObject.GetComponent<RawImage>();

        noiseImage.color = Color.white;
        noiseImage.raycastTarget = false;

        noiseTexture =
            new Texture2D(
                TextureWidth,
                TextureHeight,
                TextureFormat.RGBA32,
                false
            );

        noiseTexture.filterMode = FilterMode.Point;
        noiseTexture.wrapMode = TextureWrapMode.Clamp;

        noisePixels =
            new Color32[
                TextureWidth * TextureHeight
            ];

        noiseImage.texture = noiseTexture;
        noiseImage.enabled = false;
    }


    public void SetVisible(bool visible)
    {
        isVisible = visible;
        elapsedTime = 0f;

        if (noiseImage != null)
        {
            noiseImage.enabled = visible;
        }

        if (visible)
        {
            DrawNoise();
        }
    }


    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
    }


    private void Update()
    {
        if (!isVisible || noiseTexture == null)
        {
            return;
        }

        elapsedTime += Time.unscaledDeltaTime;

        float interval =
            1f / Mathf.Max(1f, framesPerSecond);

        if (elapsedTime < interval)
        {
            return;
        }

        elapsedTime = 0f;
        DrawNoise();
    }


    private void DrawNoise()
    {
        float strength = Mathf.Clamp01(intensity);

        for (int y = 0; y < TextureHeight; y++)
        {
            // <변경부분>
            // 프레임마다 일부 수평 밴드를 강하게 출력한다.
            bool isTearLine =
       noiseRandom.NextDouble() <
       tearLineChance * strength;

            // <변경부분>
            // Inspector에서 설정한 길이 비율을 기준으로
            // 짧은 가로 노이즈를 생성한다.
            int maxLineLength =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        TextureWidth *
                        tearLineLengthRatio
                    ),
                    1,
                    TextureWidth
                );

            // <변경부분>
            // 모든 줄무늬가 동일한 길이가 되지 않도록
            // 최대 길이의 50~100% 사이로 무작위 생성.
            int lineLength =
                noiseRandom.Next(
                    Mathf.Max(1, maxLineLength / 2),
                    maxLineLength + 1
                );

            // <변경부분>
            // 화면 전체에서 무작위 시작 위치를 결정한다.
            int tearStart =
                noiseRandom.Next(
                    0,
                    TextureWidth - lineLength + 1
                );

            int tearEnd =
                tearStart + lineLength;

            // <변경부분>
            // Inspector의 rgbLineRatio 값을 실제 색상 선택에 반영한다.
            // 0이면 흰색만, 1이면 RGB 색상만 사용한다.
            // RGB를 선택한 경우 빨강과 청록을 동일한 확률로 사용한다.
            bool useRgbLine =
                noiseRandom.NextDouble() <
                Mathf.Clamp01(rgbLineRatio);

            int tearColor =
                useRgbLine
                    ? noiseRandom.Next(1, 3)
                    : 0;

            for (int x = 0; x < TextureWidth; x++)
            {
                int index =
                    y * TextureWidth + x;

                if (isTearLine &&
                    x >= tearStart &&
                    x < tearEnd)
                {
                    byte alpha = (byte)Mathf.Clamp(
                        noiseRandom.Next(140, 240)
                            * strength,
                        0f,
                        255f
                    );

                    switch (tearColor)
                    {
                        // <변경부분>
                        // 기본은 흰색 라인
                        case 0:
                            noisePixels[index] =
                                new Color32(
                                    255, 255, 255, alpha
                                );
                            break;

                        // <변경부분>
                        // RGB는 강한 원색이 아니라
                        // 약간 바랜 느낌으로 줄인다.
                        case 1:
                            noisePixels[index] =
                                new Color32(
                                    255, 110, 130, alpha
                                );
                            break;

                        default:
                            noisePixels[index] =
                                new Color32(
                                    120, 235, 255, alpha
                                );
                            break;
                    }

                    continue;
                }

                // <변경부분>
                // 불규칙한 밝기와 투명도의 정적 노이즈.
                if (noiseRandom.NextDouble() <
                    noiseDensity * strength)
                {
                    byte brightness =
                        (byte)noiseRandom.Next(60, 256);

                    byte alpha =
                        (byte)Mathf.Clamp(
                            noiseRandom.Next(35, 160)
                                * strength,
                            0f,
                            255f
                        );

                    noisePixels[index] =
                        new Color32(
                            brightness,
                            brightness,
                            brightness,
                            alpha
                        );
                }
                else
                {
                    noisePixels[index] =
                        new Color32(0, 0, 0, 0);
                }
            }
        }

        noiseTexture.SetPixels32(noisePixels);
        noiseTexture.Apply(false, false);
    }


    private void OnDestroy()
    {
        if (noiseTexture != null)
        {
            Destroy(noiseTexture);
            noiseTexture = null;
        }
    }
}
