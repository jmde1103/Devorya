using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AbsorbButtonLayoutFix
{
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
            var layout = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ResponsiveScreenLayout>(true))
                .First(l => l.elements.Any(e => e.target != null && e.target.name.Trim() == "Skill Button"));
            var skill = layout.elements.First(e => e.target != null && e.target.name.Trim() == "Skill Button");
            var absorb = layout.elements.First(e => e.target != null && e.target.name.Trim() == "Absorb Button");
            Debug.Log("ABSORB_BEFORE anchor=" + absorb.anchor + " inset=" + absorb.inset);
            absorb.relativeTo = skill.target;
            var canvasRoot = (RectTransform)layout.transform;
            var absorbBounds = ResponsiveScreenLayout.BoundsInCanvas(canvasRoot, absorb.target, absorb.contentBounds);
            var skillBounds = ResponsiveScreenLayout.BoundsInCanvas(canvasRoot, skill.target, ResponsiveScreenLayout.MeasureContent(skill.target));
            absorb.relativeOffset = new Vector2(-.75f * (absorbBounds.extents.x + skillBounds.extents.x), -(absorbBounds.extents.y + skillBounds.extents.y + 8));
            Debug.Log("ABSORB_OFFSET " + absorb.relativeOffset);
            absorb.anchor = skill.anchor;
            EditorUtility.SetDirty(layout);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var canvas = layout.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            layout.GetComponent<CanvasScaler>().enabled = false;
            var root = (RectTransform)canvas.transform;
            root.localScale = Vector3.one;
            root.position = Vector3.zero;
            root.rotation = Quaternion.identity;
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1024,768),new Vector2(2560,1080),new Vector2(2340,1080),new Vector2(1080,2340)})
            {
                var logical = size / Mathf.Min(size.x/1920,size.y/1080);
                root.sizeDelta = logical;
                Canvas.ForceUpdateCanvases();
                var safe = new Rect(-logical/2,logical);
                if(size.x==2340){safe.xMin+=96;safe.xMax-=96;safe.yMin+=32;}
                foreach(bool visible in new[]{false,true})
                {
                    absorb.target.gameObject.SetActive(visible);
                    layout.ApplyLayout(safe);
                    var a=ResponsiveScreenLayout.BoundsInCanvas(root,absorb.target,absorb.contentBounds);
                    var b=ResponsiveScreenLayout.BoundsInCanvas(root,skill.target,ResponsiveScreenLayout.MeasureContent(skill.target));
                    if(Vector2.Distance((Vector2)(a.center-b.center),absorb.relativeOffset)>1)throw new Exception("Incorrect relative placement " + size);
                    if(a.min.x<safe.xMin || a.min.y<safe.yMin || a.max.x>safe.xMax || a.max.y>safe.yMax)throw new Exception("Absorb clipped " + size);
                    if(new Rect(a.min,a.size).Overlaps(new Rect(b.min,b.size)))throw new Exception("Buttons overlap " + size);
                }
            }
            Debug.Log("ABSORB_LAYOUT_PASS: 5 viewport sizes, hidden/visible, relative offset, bounds and overlap checks");
            EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
