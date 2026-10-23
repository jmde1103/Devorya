using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Historical class/serialized field names retained for existing popup data compatibility.
// This effect only changes the target UI's image materials. It never captures or overlays the screen.
public sealed class PopupScreenGlitchEffect
{
    sealed class Entry { public Graphic graphic; public Material original; public Material runtime; }
    readonly List<Entry> entries = new List<Entry>();
    float strength;
    static readonly int Amount = Shader.PropertyToID("_GlitchAmount");
    static readonly int Phase = Shader.PropertyToID("_GlitchPhase");
    public void Begin(RectTransform target, PopupOpenAnimationData data, Color? flashColor = null)
    {
        Stop();
        if (target == null || !data.useScreenGlitch || data.screenGlitchMaterial == null || data.screenGlitchStrength <= 0 ||
            data.screenGlitchMaterial.shader.name != "Devorya/UI/PopupLocalGlitch") return;
        strength = Mathf.Clamp01(data.screenGlitchStrength);
        foreach (var graphic in target.GetComponentsInChildren<Graphic>(true))
        {
            // SDF text needs its own font shader; preserve fonts and their existing popup flicker/jitter.
            if (!(graphic is Image) && !(graphic is RawImage)) continue;
            var entry = new Entry { graphic = graphic, original = graphic.material,
                runtime = new Material(data.screenGlitchMaterial) { name = "PopupLocalGlitchMaterial" } };
            if (graphic is Image image && image.overrideSprite != null)
                entry.runtime.SetVector("_UVRect", UnityEngine.Sprites.DataUtility.GetOuterUV(image.overrideSprite));
            entry.runtime.SetColor("_FlashColor", flashColor ?? Color.clear);
            entry.runtime.SetFloat(Amount, 0);
            entries.Add(entry);graphic.material = entry.runtime;
        }
    }
    public void Update(float progress)
    {
        float envelope = progress < .2f ? progress / .2f : progress < .7f ? 1 : (1-progress)/.3f;
        foreach (var entry in entries)
        {
            if (entry.graphic == null) continue;
            Set(entry.runtime, envelope);
            // Masked UI may render using Unity's stencil material copy.
            var rendered = entry.graphic.materialForRendering;
            if (rendered != entry.runtime) Set(rendered, envelope);
        }
    }
    void Set(Material material, float envelope)
    {
        if (material == null) return;
        material.SetFloat(Amount, strength*Mathf.Clamp01(envelope));
        material.SetFloat(Phase, Time.unscaledTime);
    }
    public void Stop()
    {
        foreach (var entry in entries)
        {
            if (entry.graphic != null && entry.graphic.material == entry.runtime) entry.graphic.material = entry.original;
            if (entry.runtime != null) Object.Destroy(entry.runtime);
        }
        entries.Clear();
    }
    public void Dispose() { Stop(); }
}
