using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Reflection;

public static class RelicRarityPaletteChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var palette=RelicRarityPalette.Shared;
        BattleRelicData a=null,b=null;
        GameObject cardObject=null;
        string original=null;
        int exitCode=0;
        try
        {
            Check(palette!=null,"Shared palette loads through Resources");
            original=EditorJsonUtility.ToJson(palette);
            a=ScriptableObject.CreateInstance<BattleRelicData>();b=ScriptableObject.CreateInstance<BattleRelicData>();
            a.rarity=b.rarity=2;
            var so=new SerializedObject(palette);
            var second=so.FindProperty("rarity2");
            second.FindPropertyRelative("hover").colorValue=Color.cyan;
            second.FindPropertyRelative("selected").colorValue=Color.magenta;
            second.FindPropertyRelative("particles").colorValue=Color.green;
            so.ApplyModifiedPropertiesWithoutUndo();
            Check(a.RarityColors.hover==Color.cyan && b.RarityColors.hover==Color.cyan,"One change applies to all same-rarity relics");
            b.rarity=1;Check(b.RarityColors.hover==palette.GetColors(1).hover,"Changing rarity switches palette");
            b.rarity=3;Check(b.RarityColors.particles==palette.GetColors(3).particles,"Rarity three resolves");
            a.rarity=-1;Check(a.RarityColors.hover==palette.GetColors(1).hover,"Low values clamp");
            a.rarity=99;Check(a.RarityColors.hover==palette.GetColors(3).hover,"High values clamp");
            a.rarity=2;
            cardObject=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RelicUI/RelicRewardCard.prefab"));
            var card=cardObject.GetComponent<RelicRewardCardUI>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(RelicRewardCardUI).GetField("<Data>k__BackingField",flags).SetValue(card,a);
            Check(card.ParticleColor==Color.green,"Card particle color comes from relic data");
            typeof(RelicRewardCardUI).GetField("hovered",flags).SetValue(card,true);
            typeof(RelicRewardCardUI).GetMethod("Update",flags).Invoke(card,null);
            var outlines=(Outline[])typeof(RelicRewardCardUI).GetField("outlines",flags).GetValue(card);
            Check(outlines.Length>0 && outlines[0].effectColor.r==0 && outlines[0].effectColor.g==1,"Card hover uses shared color");
            card.MarkSelected();typeof(RelicRewardCardUI).GetMethod("Update",flags).Invoke(card,null);
            Check(outlines[0].effectColor.r==1 && outlines[0].effectColor.g==0,"Card selected uses shared color");
            second.FindPropertyRelative("particles").colorValue=Color.blue;so.ApplyModifiedPropertiesWithoutUndo();
            Check(card.ParticleColor==Color.blue,"Existing card follows shared edits");
            Check(new SerializedObject(card).FindProperty("rarityColors")==null,"No duplicated card palette");
            Debug.Log("RELIC_RARITY_PALETTE_PASS: shared settings, new relics, rarity switching, card hover/selection/particles, live propagation");
        }
        catch(Exception e){Debug.LogException(e);exitCode=1;}
        finally
        {
            if(palette!=null&&original!=null){EditorJsonUtility.FromJsonOverwrite(original,palette);EditorUtility.ClearDirty(palette);}
            if(cardObject!=null)UnityEngine.Object.DestroyImmediate(cardObject);
            if(a!=null)UnityEngine.Object.DestroyImmediate(a);
            if(b!=null)UnityEngine.Object.DestroyImmediate(b);
        }
        EditorApplication.Exit(exitCode);
    }
}