using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DecorationPaletteData",
    menuName = "Devorya/Background/Decoration Palette"
)]
public class DecorationPaletteData : ScriptableObject
{
    [Header("Decoration Sets")]
    [SerializeField]
    private List<DecorationSet> decorationSets =
        new List<DecorationSet>();


    public List<DecorationSet> DecorationSets =>
        decorationSets;
}
