#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(DecorationPaletteData))]
public class DecorationPaletteDataEditor : Editor
{
    private SerializedProperty decorationSetsProperty;


    private void OnEnable()
    {
        decorationSetsProperty =
            serializedObject.FindProperty(
                "decorationSets"
            );
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        EditorGUILayout.LabelField(
            "Decoration Palette",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(4f);


        if (decorationSetsProperty == null)
        {
            EditorGUILayout.HelpBox(
                "Decoration Sets property를 찾을 수 없습니다.",
                MessageType.Error
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }


        int removeIndex = -1;


        for (int i = 0;
             i < decorationSetsProperty.arraySize;
             i++)
        {
            SerializedProperty setProperty =
                decorationSetsProperty
                    .GetArrayElementAtIndex(i);


            SerializedProperty setNameProperty =
                setProperty.FindPropertyRelative(
                    "SetName"
                );

            SerializedProperty decorationTypeProperty =
                setProperty.FindPropertyRelative(
                    "DecorationType"
                );


            string setName =
                setNameProperty != null
                    ? setNameProperty.stringValue
                    : string.Empty;


            string typeName =
                "Unknown";

            if (decorationTypeProperty != null &&
                decorationTypeProperty.enumDisplayNames != null &&
                decorationTypeProperty.enumDisplayNames.Length >
                decorationTypeProperty.enumValueIndex)
            {
                typeName =
                    decorationTypeProperty
                        .enumDisplayNames[
                            decorationTypeProperty
                                .enumValueIndex
                        ];
            }


            string foldoutName =
                string.IsNullOrWhiteSpace(setName)
                    ? $"{typeName}"
                    : $"{setName}  ({typeName})";


            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            EditorGUILayout.BeginHorizontal();


            setProperty.isExpanded =
                EditorGUILayout.Foldout(
                    setProperty.isExpanded,
                    foldoutName,
                    true
                );


            if (GUILayout.Button(
                    "X",
                    GUILayout.Width(24f)))
            {
                removeIndex = i;
            }


            EditorGUILayout.EndHorizontal();


            if (setProperty.isExpanded)
            {
                EditorGUI.indentLevel++;


                EditorGUILayout.PropertyField(
                    setNameProperty,
                    new GUIContent(
                        "Set Name"
                    )
                );


                EditorGUILayout.PropertyField(
                    decorationTypeProperty,
                    new GUIContent(
                        "Decoration Type"
                    )
                );


                EditorGUILayout.PropertyField(
                    setProperty.FindPropertyRelative(
                        "DecorationSprites"
                    ),
                    new GUIContent(
                        "Decoration Sprites"
                    ),
                    true
                );


                EditorGUILayout.PropertyField(
                    setProperty.FindPropertyRelative(
                        "ActorOcclusionOffsets"
                    ),
                    new GUIContent(
                        "Actor Occlusion Offsets"
                    ),
                    true
                );


                EditorGUI.indentLevel--;
            }


            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(3f);
        }


        if (removeIndex >= 0)
        {
            decorationSetsProperty
                .DeleteArrayElementAtIndex(
                    removeIndex
                );
        }


        EditorGUILayout.Space(5f);


        if (GUILayout.Button(
                "+ Add Decoration Set"))
        {
            int newIndex =
                decorationSetsProperty.arraySize;

            decorationSetsProperty
                .InsertArrayElementAtIndex(
                    newIndex
                );


            SerializedProperty newSet =
                decorationSetsProperty
                    .GetArrayElementAtIndex(
                        newIndex
                    );


            SerializedProperty newName =
                newSet.FindPropertyRelative(
                    "SetName"
                );

            SerializedProperty newType =
                newSet.FindPropertyRelative(
                    "DecorationType"
                );

            SerializedProperty newSprites =
                newSet.FindPropertyRelative(
                    "DecorationSprites"
                );

            SerializedProperty newOcclusion =
                newSet.FindPropertyRelative(
                    "ActorOcclusionOffsets"
                );


            if (newName != null)
            {
                newName.stringValue =
                    $"Decoration Set {newIndex + 1}";
            }

            if (newType != null)
            {
                newType.enumValueIndex = 0;
            }

            if (newSprites != null)
            {
                newSprites.arraySize = 0;
            }

            if (newOcclusion != null)
            {
                newOcclusion.arraySize = 0;
            }


            newSet.isExpanded = true;
        }


        serializedObject.ApplyModifiedProperties();
    }
}

#endif
