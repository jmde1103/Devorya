using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class TooltipTextStyle
{
    static TooltipStylePalette palette;
    public static TooltipStylePalette Palette
    {
        get
        {
            if (palette == null) palette = Resources.Load<TooltipStylePalette>("TooltipStylePalette");
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<TooltipStylePalette>();
                palette.hideFlags = HideFlags.HideAndDontSave;
            }
            return palette;
        }
    }

    public static void Apply(TMP_Text target, TooltipTextRole role, bool bold = false)
    {
        if (target == null) return;
        target.richText = true;
        target.color = Palette.Get(role);
        target.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
    }

    public static TooltipTextRole StatusRole(StatusEffectData data)
    {
        if (data == null) return TooltipTextRole.Body;
        switch (data.effectType)
        {
            case StatusEffectType.AttackDisabled:
            case StatusEffectType.ActionDisabled:
            case StatusEffectType.MoveDisabled:
            case StatusEffectType.AbilitySealed: return TooltipTextRole.Negative;
            case StatusEffectType.None: return TooltipTextRole.Body;
            default: return TooltipTextRole.Positive;
        }
    }

    public static string Format(LocalizedString reference, string text, int? value = null)
    {
        if (string.IsNullOrEmpty(text) || reference == null || reference.IsEmpty) return text;
        var result = LocalizationSettings.StringDatabase.GetTableEntry(
            reference.TableReference, reference.TableEntryReference);
        return Format(text, result.Entry?.GetMetadata<TooltipSemanticMetadata>(), value);
    }

    public static string Format(string text, TooltipSemanticMetadata metadata, int? value = null)
    {
        if (string.IsNullOrEmpty(text) || metadata == null || metadata.spans == null) return text;
        // Match only translator-authored fragments. Never infer statuses or numbers from prose.
        var ranges = new List<(int start, int length, TooltipTextRole role)>();
        foreach (var span in metadata.spans)
        {
            if (span == null || string.IsNullOrEmpty(span.text)) continue;
            string fragment = span.text;
            if (value.HasValue) fragment = fragment.Replace("{percent}", value.Value.ToString()).Replace("{value}", value.Value.ToString());
            int start = 0;
            while ((start = text.IndexOf(fragment, start, StringComparison.Ordinal)) >= 0)
            {
                int end = start + fragment.Length;
                // Do not insert tags into existing TMP tags or overlap another annotated span.
                bool insideTag = text.LastIndexOf('<', start) > text.LastIndexOf('>', start);
                bool overlap = ranges.Exists(r => start < r.start + r.length && end > r.start);
                if (!insideTag && !overlap && fragment.IndexOf('<') < 0 && fragment.IndexOf('>') < 0)
                    ranges.Add((start, fragment.Length, span.role));
                start = end;
            }
        }
        ranges.Sort((a,b) => a.start.CompareTo(b.start));
        var output = new StringBuilder();
        int cursor = 0;
        foreach (var range in ranges)
        {
            output.Append(text, cursor, range.start - cursor);
            output.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Palette.Get(range.role))).Append('>');
            output.Append(text, range.start, range.length).Append("</color>");
            cursor = range.start + range.length;
        }
        output.Append(text, cursor, text.Length - cursor);
        return output.ToString();
    }
}
