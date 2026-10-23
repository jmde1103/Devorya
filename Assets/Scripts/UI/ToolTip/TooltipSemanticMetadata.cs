using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Metadata;

public enum TooltipTextRole
{
    [InspectorName("제목")] Title,
    [InspectorName("본문")] Body,
    [InspectorName("보조 정보")] Secondary,
    [InspectorName("긍정 효과")] Positive,
    [InspectorName("부정 효과")] Negative,
    [InspectorName("수치")] Value
}

// Attached to a locale's existing String Table entry, never a second translation source.
// Only explicitly authored spans are styled; ordinary strings and other UI remain unchanged.
[Serializable, Metadata]
public sealed class TooltipSemanticMetadata : IMetadata
{
    [Serializable]
    public sealed class Span
    {
        [Tooltip("현재 번역에 포함된 강조할 원문. {percent}, {value} 사용 가능.")]
        public string text;
        public TooltipTextRole role;
    }
    public List<Span> spans = new List<Span>();
}
