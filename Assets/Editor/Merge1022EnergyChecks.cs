using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
public static class Merge1022EnergyChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
    public static void Run()
    {
        try{
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var go=new GameObject("MergeEnergyCheck");go.SetActive(false);
            var manager=go.AddComponent<BattleManager>();
            var data=ScriptableObject.CreateInstance<UniqueSkillData>();data.energyCost=2;data.skillType=UniqueSkillType.JelluSplit;
            Check(manager.GetPlayerAbilityEnergyCost(data)==2,"Base Cost");
            manager.ModifyAllPlayerAbilityCosts(1);manager.ModifyPlayerAbilityCost(data.skillType,-2);
            Check(manager.GetPlayerAbilityEnergyCost(data)==1&&data.energyCost==2,"Modified Cost preserves data");
            typeof(BattleManager).GetMethod("RefreshPlayerAbilityEnergyForTurn",Flags).Invoke(manager,null);
            int maximum=manager.MaxPlayerAbilityEnergy;
            typeof(BattleManager).GetMethod("SpendPlayerAbilityEnergyOnSuccess",Flags).Invoke(manager,new object[]{data,1});
            Check(manager.CurrentPlayerAbilityEnergy==maximum-1,"Success spends current energy");
            var counter=go.AddComponent<BattleMaxEnergyCountUI>();
            var textGo=new GameObject("EnergyText",typeof(RectTransform));textGo.SetActive(false);var text=textGo.AddComponent<TextMeshProUGUI>();
            typeof(BattleMaxEnergyCountUI).GetField("battleManager",Flags).SetValue(counter,manager);
            typeof(BattleMaxEnergyCountUI).GetField("countNumText",Flags).SetValue(counter,text);
            typeof(BattleMaxEnergyCountUI).GetMethod("RefreshMaxEnergy",Flags).Invoke(counter,null);
            Check(text.text==(maximum-1).ToString(),"Counter shows current energy after spending");
            manager.RestorePlayerAbilityEnergy(100);Check(manager.CurrentPlayerAbilityEnergy==maximum,"Restore capped");
            typeof(BattleManager).GetMethod("SpendPlayerAbilityEnergyOnSuccess",Flags).Invoke(manager,new object[]{data,2});
            typeof(BattleManager).GetMethod("RefreshPlayerAbilityEnergyForTurn",Flags).Invoke(manager,null);
            Check(manager.CurrentPlayerAbilityEnergy==maximum,"Turn refresh");
            UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(textGo);UnityEngine.Object.DestroyImmediate(data);
            Debug.Log("MERGE_1022_ENERGY_PASS: cost modifiers, source data, spending, current-energy TMP, restore cap and turn refresh.");
            WornBootsChecks.Run();
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
