using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class Merge1024Checks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,F).GetValue(owner);
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            var gameOver=UnityEngine.Object.FindAnyObjectByType<BattleGameOverSequence>(FindObjectsInactive.Include);
            Check(gameOver!=null && gameOver.transform.parent==null,"GameOverCanvas is scene root");
            foreach(var name in new[]{"screenGlitchController","signalNoiseUI","corruptionFX","inputBlockerGroup","blackoutGroup","messageGroup","mainMessageText","redGhostText","cyanGhostText"})
                Check(Field<UnityEngine.Object>(gameOver,name)!=null,"Game-over reference "+name);
            var flow=UnityEngine.Object.FindAnyObjectByType<BattleEndFlowController>(FindObjectsInactive.Include);
            Check(Field<BattleGameOverSequence>(flow,"gameOverSequence")==gameOver,"Lose flow references game-over");
            Check(EditorBuildSettings.scenes.Any(s=>s.enabled && s.path=="Assets/Scenes/Title.unity"),"Title enabled");
            var energy=UnityEngine.Object.FindAnyObjectByType<BattleMaxEnergyCountUI>(FindObjectsInactive.Include);
            var icons=Field<Sprite[]>(energy,"energyLevelSprites");
            Check(icons.Length==5 && icons.All(s=>s!=null),"Five energy sprites connected");
            for(int i=0;i<5;i++)
            {
                Check(icons[i].texture.filterMode==FilterMode.Point,"Point-filtered energy sprite");
                typeof(BattleMaxEnergyCountUI).GetMethod("UpdateEnergyIcon",F).Invoke(energy,new object[]{i,4});
                Check(Field<Image>(energy,"iconImage").sprite==icons[i],"Energy icon step "+i);
            }
            var sequence=UnityEngine.Object.FindAnyObjectByType<EventSequenceController>(FindObjectsInactive.Include);
            var group=new GameObject("MergeVisibilityCheck",typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            group.alpha=.3f;group.ignoreParentGroups=false;
            typeof(EventSequenceController).GetMethod("SetBattleUIGroupVisibility",F).Invoke(sequence,new object[]{group,true,0f,"Check"});
            Check(group.alpha==1 && group.ignoreParentGroups,"Child UI independently visible");
            typeof(EventSequenceController).GetMethod("RestoreBattleUIVisibilityImmediately",F).Invoke(sequence,null);
            Check(Mathf.Approximately(group.alpha,.3f) && !group.ignoreParentGroups,"Original UI state restored");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Debug.Log("MERGE1024_PASS: game-over references/root, Title list, energy sprites 0-4, event UI visibility and restore");
            RelicRarityPaletteChecks.Run();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
