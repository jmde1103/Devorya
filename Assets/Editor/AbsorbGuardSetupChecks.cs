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
public static class AbsorbGuardSetupChecks
{
    const string Pending = "AbsorbGuardChecks.Pending";
    const string PathName = "Assets/Devorya/Data/Skills/General/특성_흡수 방어.asset";
    static AbsorbGuardSetupChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {
                SessionState.SetBool(Pending,false);
                new GameObject("AbsorbGuardCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
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
            var effect = AssetDatabase.LoadAssetAtPath<StatusEffectData>("Assets/Devorya/Data/StatusEffect/상태효과_방어.asset");
            data.skillType = GeneralSkillType.AbsorbGuard;
            data.skillName = "흡수 방어";
            data.description = data.tooltipDescriptionFormat = "흡수 공격을 막고 공격한 기물을 튕겨냅니다. 마무리 흡수도 막습니다.";
            data.iconSprite = effect.iconSprite;
            data.tooltipSections.Clear();
            data.localizedSkillName = new LocalizedString("General_Skill", "general.absorb_guard.name");
            data.localizedDescription = new LocalizedString("General_Skill", "general.absorb_guard.desc");
            data.localizedTooltipDescriptionFormat = new LocalizedString("General_Skill", "general.absorb_guard.tooltip");
            var collection = LocalizationEditorSettings.GetStringTableCollection("General_Skill");
            string[] locales = {"ko","en","ja"};
            string[] names = {"흡수 방어","Absorption Guard","吸収防御"};
            string[] descriptions = { data.description,
                "Blocks absorption attacks and knocks the attacker back. Also blocks finishing absorption.",
                "吸収攻撃を防ぎ、攻撃した駒を弾き返します。とどめの吸収も防ぎます。"};
            string[] effects = {"흡수 공격","absorption attacks","吸収攻撃"};
            for(int i=0;i<3;i++)
            {
                var table = (StringTable)collection.GetTable(new LocaleIdentifier(locales[i]));
                table.AddEntry("general.absorb_guard.name", names[i]);
                foreach(var key in new[]{"general.absorb_guard.desc","general.absorb_guard.tooltip"})
                {
                    var entry = table.AddEntry(key,descriptions[i]);
                    var md = entry.GetMetadata<TooltipSemanticMetadata>();
                    if(md == null) { md = new TooltipSemanticMetadata(); entry.AddMetadata(md); }
                    md.spans.Clear();

                    md.spans.Add(new TooltipSemanticMetadata.Span{text=effects[i],role=TooltipTextRole.Positive});
                }
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(data);
            foreach(var guid in AssetDatabase.FindAssets("t:GeneralSkillDatabase"))
            {
                var db = AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>(AssetDatabase.GUIDToAssetPath(guid));
                if(db.GetData(GeneralSkillType.AbsorbGuard) != null) continue;
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
        var stack=new System.Collections.Generic.Stack<IEnumerator>(); stack.Push(Checks());
        while(stack.Count>0)
        {
            object next;
            try { if(!stack.Peek().MoveNext()){stack.Pop();continue;} next=stack.Peek().Current; }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
            if(next is IEnumerator nested) stack.Push(nested); else yield return next;
        }
        Debug.Log("ABSORB_GUARD_PLAY_PASS");
        EditorApplication.Exit(0);
    }
    static void Set(object o,string field,object v) { o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v); }
    static IEnumerator Checks()
    {
        yield return LocalizationSettings.InitializationOperation;
        foreach(var mode in new[]{"absorb","normal","lastAttack","finish","breakthrough"})
        {
            var f=new SynthesisAbilityChecks.Fixture();
            var battle=f.root.AddComponent<BattleManager>(); battle.enabled=false;
            Set(battle,"boardManager",f.board);Set(battle,"pieceManager",f.manager);
            Set(battle,"battleMoveValidator",f.validator);Set(battle,"battleSkillManager",f.skill);
            Set(battle,"isBattleEnded",mode!="finish");
            Set(battle,"currentTurn",BattleTurn.Player);
            Set(battle,"isPlayerDeploymentPhase",false);
            var db=AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:GeneralSkillDatabase")[0]));
            Set(f.skill,"generalSkillDatabase",db);
            for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++){
                typeof(Tile).GetProperty("X").SetValue(f.board.GetTile(x,y),x);
                typeof(Tile).GetProperty("Y").SetValue(f.board.GetTile(x,y),y);
            }
            var attacker=f.Spawn(mode=="finish"?"Devorya King":"Devorya Rook",PieceTeam.Player,2,0);
            var target=f.Spawn("Devorya Bishop",PieceTeam.Enemy,2,2);
            target.AddGeneralSkill(GeneralSkillType.AbsorbGuard);
            if(mode=="absorb"||mode=="normal"||mode=="breakthrough")f.Spawn("Devorya King",PieceTeam.Enemy,4,5);
            if(mode=="breakthrough")attacker.AddStatusEffect(f.statuses.GetData(StatusEffectType.Breakthrough));
            Set(battle,"isAbsorbMode",mode!="normal");
            var origin=attacker.transform.position;
            string method=mode=="finish"?"ExecuteLastEnemyKingAbsorbRoutine":"ExecutePieceActionRoutine";
            object[] args=mode=="finish"?new object[]{attacker,target}:new object[]{attacker,f.board.GetTile(2,2)};
            yield return (IEnumerator)typeof(BattleManager).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,args);
            yield return null;
            if(mode=="normal") Check(target==null && f.manager.GetPieceAt(2,2)==attacker,"Normal attack still captures");
            else
            {
                Check(target!=null && f.manager.GetPieceAt(2,2)==target,"Absorb target survives: "+mode);
                Check(f.manager.GetPieceAt(2,0)==attacker && Vector3.Distance(origin,attacker.transform.position)<.01f,"Bounce returns attacker: "+mode);
                Check(!attacker.HasGeneralSkill(GeneralSkillType.AbsorbGuard),"No copied traits");
                Check((int)typeof(BattleManager).GetField("playerAbsorbCountThisBattle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle)==0,"No absorb count");
                Check(!(bool)typeof(BattleManager).GetField("isActionAnimating",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle),"Input unlocked");
            }
            f.Dispose(); yield return null;
        }
        var data=AssetDatabase.LoadAssetAtPath<GeneralSkillData>(PathName);
        foreach(var locale in new[]{"ko","en","ja"})
        {
            LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(locale);yield return null;
            Check(!string.IsNullOrEmpty(data.GetLocalizedSkillName()),"Translated name");
            Check(TooltipTextStyle.Format(data.localizedTooltipDescriptionFormat,data.GetLocalizedTooltipDescription()).Contains("<color=#75C9D6>"),"Semantic color");
        }
    }
}
