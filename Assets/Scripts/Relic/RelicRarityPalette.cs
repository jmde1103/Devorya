using System;
using UnityEngine;

// One shared asset is resolved by every relic, including newly created data.
public sealed class RelicRarityPalette : ScriptableObject
{
    public const string ResourceName = "유물_희귀도 공통 색상";
    [Serializable]
    public struct Colors
    {
        [InspectorName("호버 아웃라인 색")] public Color hover;
        [InspectorName("선택 발광·아웃라인 색")] public Color selected;
        [InspectorName("흡수 입자 색")] public Color particles;
        [InspectorName("아이콘 글리치 반짝임 색"), Tooltip("알파 0이면 반짝임 색을 덧씌우지 않습니다.")] public Color iconGlitch;
        public static Colors Default => new Colors { hover=Color.white, selected=new Color(1,.8f,.25f), particles=Color.white, iconGlitch=Color.white };
    }
    [SerializeField, InspectorName("희귀도 1")] Colors rarity1 = Colors.Default;
    [SerializeField, InspectorName("희귀도 2")] Colors rarity2 = Colors.Default;
    [SerializeField, InspectorName("희귀도 3")] Colors rarity3 = Colors.Default;
    static RelicRarityPalette shared;
    public static RelicRarityPalette Shared
    {
        get { if(shared==null)shared=Resources.Load<RelicRarityPalette>(ResourceName);return shared; }
    }
    public Colors GetColors(int rarity)
    {
        switch(Mathf.Clamp(rarity,1,3)){case 1:return rarity1;case 2:return rarity2;default:return rarity3;}
    }
}