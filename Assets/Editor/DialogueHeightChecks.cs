using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;

[InitializeOnLoad]
public static class DialogueHeightChecks
{
    const string Pending="DialogueHeightChecks";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static DialogueHeightChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);var host=new GameObject("DialogueHeightChecks").AddComponent<PieceAnimationManager>();UnityEngine.Object.DontDestroyOnLoad(host.gameObject);host.StartCoroutine(Guard(host));}};}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,F).GetValue(o);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("DIALOGUE_HEIGHT_CHECK: "+message);}
    static IEnumerator Guard(MonoBehaviour host){var e=Checks(host);while(true){object n;try{if(!e.MoveNext())break;n=e.Current;}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);yield break;}yield return n;}Debug.Log("DIALOGUE_HEIGHT_PASS");EditorApplication.Exit(0);}
    static IEnumerator Checks(MonoBehaviour host)
    {
        foreach(string scene in new[]{"BattleScene","EventScene","WorldMapScene"})
        {
            if(SceneManager.GetActiveScene().name!=scene)yield return SceneManager.LoadSceneAsync(scene);
            yield return LocalizationSettings.InitializationOperation;
            yield return new WaitForSecondsRealtime(3);
            foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(c is EventSequenceController||c is EventSceneSequenceController||c is WorldMapEventController||c is BattleSetupManager||c is BattleManager)c.StopAllCoroutines();
            var ui=UnityEngine.Object.FindFirstObjectByType<EventGuideUI>(FindObjectsInactive.Include);Check(ui!=null,scene+" shared dialogue present");
            ui.StopAllCoroutines();ui.HideImmediately();
            var text=Get<TMP_Text>(ui,"guideText");var panel=(RectTransform)text.transform.parent;
            float baseHeight=panel.rect.height,baseWidth=panel.rect.width;
            foreach(string locale in new[]{"ko","en","ja"})
            {
                LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(locale);yield return null;
                string line=locale=="ja"?"隣接する味方の駒を選択してください。攻撃できない場合でも移動することができます。":locale=="en"?"Select an adjacent allied piece. You can move even when an attack is not available.":"인접한 아군 기물을 선택하세요. 공격할 수 없는 경우에도 이동할 수 있습니다.";
                string page=string.Join("\n",new[]{line,line,"<b>"+line+"</b>",line,line,line});
                var play=host.StartCoroutine(ui.PlayDialogueRoutine(new List<string>{page}));yield return new WaitForSecondsRealtime(.2f);
                Check(text.text==page,scene+" "+locale+" full localized page retained");
                Check(panel.rect.height>baseHeight,scene+" "+locale+" panel grows for long page");
                text.ForceMeshUpdate();float lowest=float.PositiveInfinity;
                for(int i=0;i<text.textInfo.lineCount;i++)lowest=Mathf.Min(lowest,text.textInfo.lineInfo[i].descender);
                Check(lowest>=text.rectTransform.rect.yMin-2,scene+" "+locale+" rendered line descenders stay inside text rect");
                Check(text.maxVisibleCharacters<text.textInfo.characterCount,scene+" "+locale+" typing remains active without resizing per character");
                var footer=Get<RectTransform>(ui,"dialogueHeightFooter");float bottomGap=footer.localPosition.y-panel.rect.yMin;
                float tall=panel.rect.height;panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,baseWidth*.65f);yield return null;yield return null;
                Check(panel.rect.height>=tall,scene+" "+locale+" narrower width recomputes height");
                Check(Mathf.Abs((footer.localPosition.y-panel.rect.yMin)-bottomGap)<.1f,scene+" "+locale+" continue arrow follows lower edge");
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,baseWidth);
                host.StopCoroutine(play);ui.HideImmediately();
                play=host.StartCoroutine(ui.PlayDialogueRoutine(new List<string>{"OK"}));yield return new WaitForSecondsRealtime(.1f);
                Check(Mathf.Abs(panel.rect.height-baseHeight)<.1f,scene+" "+locale+" short page restores authored minimum height");
                host.StopCoroutine(play);ui.HideImmediately();
            }
        }
    }
}
