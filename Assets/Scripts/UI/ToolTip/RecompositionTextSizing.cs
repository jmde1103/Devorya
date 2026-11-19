using TMPro;
using UnityEngine;

// A reversible, runtime-only presentation override. Never edits shared prefabs or styles.
public sealed class RecompositionTextSizing
{
    readonly TMP_Text text;
    readonly float size, min, max;
    readonly bool auto;
    readonly TextWrappingModes wrapping;
    readonly TextOverflowModes overflow;
    readonly Vector4 margin;
    public RecompositionTextSizing(TMP_Text target)
    {
        text = target; size = target.fontSize; min = target.fontSizeMin; max = target.fontSizeMax;
        auto = target.enableAutoSizing; wrapping = target.textWrappingMode;
        overflow = target.overflowMode; margin = target.margin;
    }
    public void Apply(bool button = false)
    {
        text.fontSizeMax = Mathf.Max(1f, size - 2f);
        text.fontSizeMin = Mathf.Min(text.fontSizeMax, Mathf.Max(18f, Mathf.Floor(text.fontSizeMax * .8f)));
        text.fontSize = text.fontSizeMax;
        text.enableAutoSizing = true;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        if (button) text.margin = new Vector4(Mathf.Max(8, margin.x), margin.y, Mathf.Max(8, margin.z), margin.w);
    }
    public void Restore()
    {
        if (text == null) return;
        text.enableAutoSizing = auto; text.fontSizeMin = min; text.fontSizeMax = max;
        text.fontSize = size; text.textWrappingMode = wrapping; text.overflowMode = overflow; text.margin = margin;
    }
}
