using UnityEditor;
using UnityEngine;

// TooltipSectionData 전용 Inspector.
//
// Tooltip Section에서는 문자열을 직접 입력하지 않는다.
// 이름 / 설명 / 아이콘 / Category는
// StatusEffectData와 Localization에서 자동으로 가져온다.
//
// TooltipSectionData를 사용하는 모든 Data에서
// 동일한 Inspector 구조를 사용한다.
[CustomPropertyDrawer(
    typeof(TooltipSectionData))]
public class TooltipSectionDataDrawer :
    PropertyDrawer
{
    private const float Spacing =
        2f;

    private const float HelpBoxHeight =
        38f;

    private const float BottomPadding =
        4f;

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(
            position,
            label,
            property
        );

        float lineHeight =
            EditorGUIUtility.singleLineHeight;

        float currentY =
            position.y;

        Rect foldoutRect =
            new Rect(
                position.x,
                currentY,
                position.width,
                lineHeight
            );

        property.isExpanded =
            EditorGUI.Foldout(
                foldoutRect,
                property.isExpanded,
                label,
                true
            );

        if (property.isExpanded == false)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        SerializedProperty statusEffectDataProperty =
            property.FindPropertyRelative(
                "statusEffectData"
            );

        SerializedProperty sectionColorProperty =
            property.FindPropertyRelative(
                "sectionColor"
            );

        currentY +=
            lineHeight +
            Spacing;

        // Status Effect
        if (statusEffectDataProperty != null)
        {
            GUIContent statusEffectLabel =
                new GUIContent(
                    "Status Effect"
                );

            float statusEffectHeight =
                EditorGUI.GetPropertyHeight(
                    statusEffectDataProperty,
                    statusEffectLabel,
                    false
                );

            Rect statusEffectRect =
                new Rect(
                    position.x,
                    currentY,
                    position.width,
                    statusEffectHeight
                );

            EditorGUI.PropertyField(
                statusEffectRect,
                statusEffectDataProperty,
                statusEffectLabel,
                false
            );

            currentY +=
                statusEffectHeight +
                Spacing;

            if (statusEffectDataProperty
                    .objectReferenceValue ==
                null)
            {
                Rect helpRect =
                    new Rect(
                        position.x,
                        currentY,
                        position.width,
                        HelpBoxHeight
                    );

                EditorGUI.HelpBox(
                    helpRect,
                    "Status Effect Data를 연결해주세요.\n" +
                    "이름 / 설명 / 아이콘은 해당 Data와 Localization에서 자동으로 가져옵니다.",
                    MessageType.Warning
                );

                currentY +=
                    HelpBoxHeight +
                    Spacing;
            }
        }

        // Section Color
        if (sectionColorProperty != null)
        {
            GUIContent sectionColorLabel =
                new GUIContent(
                    "Section Color"
                );

            float sectionColorHeight =
                EditorGUI.GetPropertyHeight(
                    sectionColorProperty,
                    sectionColorLabel,
                    false
                );

            Rect sectionColorRect =
                new Rect(
                    position.x,
                    currentY,
                    position.width,
                    sectionColorHeight
                );

            EditorGUI.PropertyField(
                sectionColorRect,
                sectionColorProperty,
                sectionColorLabel,
                false
            );
        }

        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        float lineHeight =
            EditorGUIUtility.singleLineHeight;

        if (property.isExpanded == false)
        {
            return lineHeight;
        }

        SerializedProperty statusEffectDataProperty =
            property.FindPropertyRelative(
                "statusEffectData"
            );

        SerializedProperty sectionColorProperty =
            property.FindPropertyRelative(
                "sectionColor"
            );

        float height =
            lineHeight;

        height +=
            Spacing;

        if (statusEffectDataProperty != null)
        {
            GUIContent statusEffectLabel =
                new GUIContent(
                    "Status Effect"
                );

            height +=
                EditorGUI.GetPropertyHeight(
                    statusEffectDataProperty,
                    statusEffectLabel,
                    false
                );

            height +=
                Spacing;

            if (statusEffectDataProperty
                    .objectReferenceValue ==
                null)
            {
                height +=
                    HelpBoxHeight +
                    Spacing;
            }
        }

        if (sectionColorProperty != null)
        {
            GUIContent sectionColorLabel =
                new GUIContent(
                    "Section Color"
                );

            height +=
                EditorGUI.GetPropertyHeight(
                    sectionColorProperty,
                    sectionColorLabel,
                    false
                );
        }

        height +=
            BottomPadding;

        return height;
    }
}