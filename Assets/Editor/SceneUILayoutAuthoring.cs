using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneUILayoutAuthoring
{
    static SceneUILayoutAuthoring()
    {
        EditorSceneManager.sceneSaving += (scene, path) => Capture(scene);
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            for (int i = 0; i < SceneManager.sceneCount; i++) Capture(SceneManager.GetSceneAt(i));
        };
    }
    public static void Capture(Scene scene)
    {
        if (EditorApplication.isPlaying || !scene.IsValid() || !scene.isLoaded) return;
        Canvas.ForceUpdateCanvases();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var layout in root.GetComponentsInChildren<ResponsiveScreenLayout>(true))
            {
                if (!layout.followScenePlacement) continue;
                layout.CaptureScenePlacement();
                EditorUtility.SetDirty(layout);
            }
    }
}

[CustomEditor(typeof(ResponsiveScreenLayout))]
public class SceneUILayoutInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var layout = (ResponsiveScreenLayout)target;
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("followScenePlacement"));
        if (!layout.followScenePlacement) DrawPropertiesExcluding(serializedObject, "m_Script", "followScenePlacement");
        else
        {
            EditorGUILayout.HelpBox("씬에서 대상 UI를 직접 이동하거나 크기를 바꾸세요. 씬 저장 또는 플레이 시작 시 배치가 자동으로 기록됩니다. 화면 비율이 바뀌면 상대 위치를 유지하고 화면 밖으로 나가지 않게 보정합니다. 기존 기준점·여백·기준 UI 설정은 사용하지 않습니다.", MessageType.Info);
            var entries = serializedObject.FindProperty("elements");
            for (int i = 0; i < entries.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(entries.GetArrayElementAtIndex(i).FindPropertyRelative("target"), new GUIContent("대상 UI " + (i + 1)));
                if (GUILayout.Button("제외", GUILayout.Width(45))) { entries.DeleteArrayElementAtIndex(i); EditorGUILayout.EndHorizontal(); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("대상 UI 추가"))
            {
                int index = entries.arraySize++;
                entries.GetArrayElementAtIndex(index).FindPropertyRelative("target").objectReferenceValue = null;
            }
        }
        serializedObject.ApplyModifiedProperties();
        if (!EditorApplication.isPlaying && layout.followScenePlacement && GUILayout.Button("현재 씬 배치 기록"))
        {
            Undo.RecordObject(layout,"Capture UI placement");
            Canvas.ForceUpdateCanvases();layout.CaptureScenePlacement();
            EditorUtility.SetDirty(layout);EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
        }
    }
}
