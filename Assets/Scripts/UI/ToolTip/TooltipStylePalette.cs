using UnityEngine;

public enum TooltipSurface
{
    [InspectorName("어두운 배경")] DarkSurface = 0,
    [InspectorName("밝은 배경")] LightSurface = 1
}

[CreateAssetMenu(fileName = "TooltipStylePalette", menuName = "Devorya/UI/툴팁 공통 색상")]
public sealed class TooltipStylePalette : ScriptableObject
{
    [InspectorName("제목")] public Color title = new Color32(255,255,255,255);
    [InspectorName("본문")] public Color body = new Color32(201,206,202,255);
    [InspectorName("보조 정보")] public Color secondary = new Color32(115,122,117,255);
    [InspectorName("긍정 효과")] public Color positive = new Color32(117,201,214,255);
    [InspectorName("부정 효과")] public Color negative = new Color32(216,121,105,255);
    [InspectorName("수치")] public Color value = new Color32(228,198,104,255);

    [InspectorName("기물 타입")] public Color pieceType = new Color32(183,166,217,255);

    [Header("밝은 배경")]
    [InspectorName("제목")] public Color lightTitle = new Color32(31,31,31,255);
    [InspectorName("본문")] public Color lightBody = new Color32(31,31,31,255);
    [InspectorName("보조 정보")] public Color lightSecondary = new Color32(102,102,102,255);
    [InspectorName("긍정 효과")] public Color lightPositive = new Color32(47,127,141,255);
    [InspectorName("부정 효과")] public Color lightNegative = new Color32(166,79,67,255);
    [InspectorName("수치")] public Color lightValue = new Color32(138,106,0,255);
    [InspectorName("기물 타입")] public Color lightPieceType = new Color32(112,89,156,255);

    public Color Get(TooltipTextRole role, TooltipSurface surface = TooltipSurface.DarkSurface)
    {
        if (surface == TooltipSurface.LightSurface)
        {
            switch (role)
            {
                case TooltipTextRole.Title: return lightTitle;
                case TooltipTextRole.Secondary: return lightSecondary;
                case TooltipTextRole.Positive: return lightPositive;
                case TooltipTextRole.Negative: return lightNegative;
                case TooltipTextRole.Value: return lightValue;
                case TooltipTextRole.PieceType: return lightPieceType;
                default: return lightBody;
            }
        }
        switch(role)
        {
            case TooltipTextRole.Title: return title;
            case TooltipTextRole.Secondary: return secondary;
            case TooltipTextRole.Positive: return positive;
            case TooltipTextRole.Negative: return negative;
            case TooltipTextRole.Value: return value;
            case TooltipTextRole.PieceType: return pieceType;
            default: return body;
        }
    }
}
