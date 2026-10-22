using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class Merge1022Checks
{
    static void Require(bool value,string label){if(!value)throw new Exception(label);}
    public static void Run()
    {
        try {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            var components=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null).ToArray();
            var ui=components.OfType<BattleUIController>().First();
            Require(new SerializedObject(ui).FindProperty("uniqueSkillCostText").objectReferenceValue!=null,"Skill Cost TMP reference");
            var energy=components.OfType<BattleMaxEnergyCountUI>().First();
            var energySO=new SerializedObject(energy);
            Require(energySO.FindProperty("countNumText").objectReferenceValue!=null,"Energy counter TMP reference");
            Require(energySO.FindProperty("battleManager").objectReferenceValue!=null,"Energy counter manager reference");
            var layout=energy.GetComponentInParent<ResponsiveScreenLayout>();
            Require(layout!=null,"Energy canvas responsive layout");
            var target=(RectTransform)energy.transform;
            if(!layout.elements.Any(e=>e.target!=null&&(target==e.target||target.IsChildOf(e.target))))
                layout.elements=layout.elements.Concat(new[]{new ResponsiveScreenLayout.Element{target=target}}).ToArray();
            EditorUtility.SetDirty(layout);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene("Assets/Scenes/EventScene.unity");
            var reward=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EventSceneRewardPopupUI>(true)).First();
            Require(new SerializedObject(reward).FindProperty("confirmButtonText").objectReferenceValue!=null,"Event reward single TMP reference");
            Debug.Log("MERGE_1022_REFERENCES_PASS: energy counter, skill Cost TMP, event reward TMP; energy HUD registered without moving it.");
            SceneUILayoutChecks.Run();
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
