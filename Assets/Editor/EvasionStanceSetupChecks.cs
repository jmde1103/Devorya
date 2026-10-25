using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

[InitializeOnLoad]
public static class EvasionStanceSetupChecks
{
    const string Pending = "EvasionStanceChecks.Pending";
    const string PathName = "Assets/Devorya/Data/Skills/General/특성_회피 자세.asset";
    static EvasionStanceSetupChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {
                SessionState.SetBool(Pending,false);
                new GameObject("EvasionStanceCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
            }
        };
    }
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void SetupAndRun()
    {
        try
        {
            var data = AssetDatabase.LoadAssetAtPath<GeneralSkillData>(PathName);
            if(data == null)
            {
                AssetDatabase.CopyAsset("Assets/Devorya/Data/Skills/General/특성_방어 자세.asset", PathName);
                data = AssetDatabase.LoadAssetAtPath<GeneralSkillData>(PathName);
            }
            var effect = AssetDatabase.LoadAssetAtPath<StatusEffectData>("Assets/Devorya/Data/StatusEffect/상태효과_회피.asset");
            data.skillType = GeneralSkillType.EvasionStance;
            data.skillName = "회피 자세";
            data.description = data.tooltipDescriptionFormat = "이동 시 {percent}% 확률로 [회피] 상태효과를 부여합니다.";
            data.iconSprite = effect.iconSprite;
            data.tooltipSections[0].statusEffectData = effect;
            data.localizedSkillName = new LocalizedString("General_Skill", "general.evasion_stance.name");
            data.localizedDescription = new LocalizedString("General_Skill", "general.evasion_stance.desc");
            data.localizedTooltipDescriptionFormat = new LocalizedString("General_Skill", "general.evasion_stance.tooltip");
            var collection = LocalizationEditorSettings.GetStringTableCollection("General_Skill");
            string[] locales = {"ko","en","ja"};
            string[] names = {"회피 자세","Evasive Stance","回避の構え"};
            string[] descriptions = { data.description,
                "When moving, has a {percent}% chance to gain [Evasion].",
                "移動時、{percent}%の確率で［回避］状態効果を付与します。"};
            string[] effects = {"[회피]","[Evasion]","［回避］"};
            for(int i=0;i<3;i++)
            {
                var table = (StringTable)collection.GetTable(new LocaleIdentifier(locales[i]));
                table.AddEntry("general.evasion_stance.name", names[i]);
                foreach(var key in new[]{"general.evasion_stance.desc","general.evasion_stance.tooltip"})
                {
                    var entry = table.AddEntry(key,descriptions[i]);
                    var md = entry.GetMetadata<TooltipSemanticMetadata>();
                    if(md == null) { md = new TooltipSemanticMetadata(); entry.AddMetadata(md); }
                    md.spans.Clear();
                    md.spans.Add(new TooltipSemanticMetadata.Span{text="{percent}%",role=TooltipTextRole.Value});
                    md.spans.Add(new TooltipSemanticMetadata.Span{text=effects[i],role=TooltipTextRole.Positive});
                }
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(data);
            foreach(var guid in AssetDatabase.FindAssets("t:GeneralSkillDatabase"))
            {
                var db = AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>(AssetDatabase.GUIDToAssetPath(guid));
                if(db.GetData(GeneralSkillType.EvasionStance) != null) continue;
                var so = new SerializedObject(db);
                var list = so.FindProperty("generalSkillDatas");
                int index = list.arraySize; list.arraySize++;
                list.GetArrayElementAtIndex(index).objectReferenceValue = data;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(db);
            }
            AssetDatabase.SaveAssets();
            SessionState.SetBool(Pending,true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static IEnumerator Guard()
    {
        var routine = Checks();
        while(true)
        {
            object next;
            try { if(!routine.MoveNext()) break; next=routine.Current; }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
            yield return next;
        }
        Debug.Log("EVASION_STANCE_CARD_PASS");
        EditorApplication.Exit(0);
    }
    static IEnumerator Checks()
    {
        yield return LocalizationSettings.InitializationOperation;
        var data = AssetDatabase.LoadAssetAtPath<GeneralSkillData>(PathName);
        var original = data.evasionGrantChancePercent;
        var f = new SynthesisAbilityChecks.Fixture();
        try
        {
            var db = AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>(
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:GeneralSkillDatabase")[0]));
            typeof(BattleSkillManager).GetField("generalSkillDatabase",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(f.skill,db);
            foreach(var team in new[]{PieceTeam.Player,PieceTeam.Enemy})
            {
                var piece=f.Spawn("Devorya Pawn",team,2,2);
                piece.AddGeneralSkill(GeneralSkillType.EvasionStance);
                var owned=piece.GetGeneralSkillDataCopy(GeneralSkillType.EvasionStance);
                data.evasionGrantChancePercent=0;
                yield return f.skill.TryGrantEvasionAfterMoveRoutine(piece,owned,null);
                Check(!piece.HasStatusEffect(StatusEffectType.Evasion),"Zero chance");
                data.evasionGrantChancePercent=100;
                yield return f.skill.TryGrantEvasionAfterMoveRoutine(piece,null,null);
                Check(!piece.HasStatusEffect(StatusEffectType.Evasion),"Acquired during action cannot activate");
                yield return f.skill.TryGrantEvasionAfterMoveRoutine(piece,owned,null);
                Check(piece.HasStatusEffect(StatusEffectType.Evasion),"Guaranteed grant");
                piece.ReduceActionRestrictionTurnAndRemoveExpired();
                Check(piece.HasStatusEffect(StatusEffectType.Evasion),"Survives current turn end");
                piece.ReduceStatusEffectTurnAndRemoveExpired();
                Check(!piece.HasStatusEffect(StatusEffectType.Evasion),"Expires next own turn");
                UnityEngine.Object.Destroy(piece.gameObject); yield return null;
            }
            data.evasionGrantChancePercent=37;
            foreach(var locale in new[]{"ko","en","ja"})
            {
                LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(locale);
                yield return null;
                var formatted=TooltipTextStyle.Format(data.localizedTooltipDescriptionFormat,data.GetLocalizedTooltipDescription(),data.GetTooltipMainValue());
                Check(formatted.Contains("<color=#E4C668>37%</color>") && formatted.Contains("<color=#75C9D6>"),"Localized chance and status colors: "+locale);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RelicUI/RelicRewardCard.prefab");
                var go=UnityEngine.Object.Instantiate(prefab);
                var card=go.GetComponent<RelicRewardCardUI>();
                foreach(var guid in AssetDatabase.FindAssets("t:BattleRelicData"))
                {
                    var relic=AssetDatabase.LoadAssetAtPath<BattleRelicData>(AssetDatabase.GUIDToAssetPath(guid));
                    card.Bind(relic,c=>{});
                    yield return null;
                    var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                    var title=(TMPro.TMP_Text)typeof(RelicRewardCardUI).GetField("title",flags).GetValue(card);
                    var body=(TMPro.TMP_Text)typeof(RelicRewardCardUI).GetField("description",flags).GetValue(card);
                    Check(title.color==TooltipTextStyle.Palette.title && body.color==TooltipTextStyle.Palette.body,"Card base colors");
                    Check(body.text==TooltipTextStyle.Format(relic.localizedDescription,relic.GetLocalizedDescription()),"Card localized semantic colors");
                }
                UnityEngine.Object.Destroy(go);
            }
        }
        finally { data.evasionGrantChancePercent=original; f.Dispose(); }
    }
}
