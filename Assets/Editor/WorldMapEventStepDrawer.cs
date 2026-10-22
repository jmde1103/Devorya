using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WorldMapEventStep))]
public class WorldMapEventStepDrawer : PropertyDrawer
{
    public static IEnumerable<(string field,string label)> Fields(SerializedProperty p)
    {
        yield return ("enabled","사용");yield return ("stepName","스텝 이름");yield return ("type","스텝 종류");
        switch((WorldMapEventStepType)p.FindPropertyRelative("type").enumValueIndex){
            case WorldMapEventStepType.Dialogue:yield return ("dialoguePages","대화 페이지");yield return ("localizedDialoguePages","번역 연결 (선택)");break;
            case WorldMapEventStepType.PlayerInput:yield return ("lockPlayerInput","조작 잠금 (끄면 해제)");break;
            case WorldMapEventStepType.UIVisibility:yield return ("showUI","UI 표시 (끄면 숨김)");break;
            case WorldMapEventStepType.ScreenGlitch:yield return ("glitchStrength","강도");yield return ("glitchDuration","지속 시간");yield return ("waitForComplete","완료 대기");break;
            case WorldMapEventStepType.GlobalLighting:
                yield return ("changeGlobalLight","글로벌 조명 변경");
                if(p.FindPropertyRelative("changeGlobalLight").boolValue){yield return ("lightColor","목표 색상");yield return ("lightIntensity","목표 밝기");yield return ("lightFadeDuration","전환 시간");}
                yield return ("screenFlash","화면 반짝임");
                if(p.FindPropertyRelative("screenFlash").boolValue){yield return ("flashColor","반짝임 색상");yield return ("flashStrength","반짝임 강도");yield return ("flashDuration","한 번의 지속 시간");yield return ("flashCount","반복 횟수");yield return ("flashInterval","반복 간격");}
                yield return ("waitForComplete","완료 대기");break;
            case WorldMapEventStepType.Wait:yield return ("waitDuration","대기 시간");break;
        }
    }
    public override float GetPropertyHeight(SerializedProperty property,GUIContent label)
    {
        float h=EditorGUIUtility.singleLineHeight+4;
        if(property.isExpanded)foreach(var field in Fields(property))h+=EditorGUI.GetPropertyHeight(property.FindPropertyRelative(field.field),true)+4;
        return h;
    }
    public override void OnGUI(Rect position,SerializedProperty property,GUIContent label)
    {
        EditorGUI.BeginProperty(position,label,property);
        var line=new Rect(position.x,position.y,position.width,EditorGUIUtility.singleLineHeight);
        property.isExpanded=EditorGUI.Foldout(line,property.isExpanded,property.FindPropertyRelative("stepName").stringValue,true);
        if(property.isExpanded){EditorGUI.indentLevel++;foreach(var field in Fields(property)){
            line.y+=line.height+4;var child=property.FindPropertyRelative(field.field);line.height=EditorGUI.GetPropertyHeight(child,true);
            EditorGUI.PropertyField(line,child,new GUIContent(field.label),true);
        }EditorGUI.indentLevel--;}
        EditorGUI.EndProperty();
    }
}
