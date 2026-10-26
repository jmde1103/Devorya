using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class StatusAbilitiesSetup
{
    const string Root="Assets/Devorya/Data/";
    static T Asset<T>(string path) where T:ScriptableObject
    {
        var data=AssetDatabase.LoadAssetAtPath<T>(path);
        if(data==null){data=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(data,path);}
        return data;
    }
    static void Register<T>(string field, UnityEngine.Object data) where T:ScriptableObject
    {
        foreach(var guid in AssetDatabase.FindAssets("t:"+typeof(T).Name))
        {
            var db=AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            var so=new SerializedObject(db);var list=so.FindProperty(field);
            bool exists=false;
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==data)exists=true;
            if(!exists){int i=list.arraySize++;list.GetArrayElementAtIndex(i).objectReferenceValue=data;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(db);}
        }
    }
    static void Text(string collectionName,string key,string[] values,string[][] spans=null,TooltipTextRole[] roles=null)
    {
        var collection=LocalizationEditorSettings.GetStringTableCollection(collectionName);
        string[] locales={"ko","en","ja"};
        for(int i=0;i<3;i++)
        {
            var table=(StringTable)collection.GetTable(new LocaleIdentifier(locales[i]));
            var entry=table.AddEntry(key,values[i]);
            if(spans!=null)
            {
                var md=entry.GetMetadata<TooltipSemanticMetadata>();
                if(md==null){md=new TooltipSemanticMetadata();entry.AddMetadata(md);}
                md.spans.Clear();
                for(int j=0;j<spans[i].Length;j++)md.spans.Add(new TooltipSemanticMetadata.Span{text=spans[i][j],role=roles[j]});
            }
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);
    }
    public static void UpdateVirusOnly()
    {
        var data=AssetDatabase.LoadAssetAtPath<UniqueSkillData>(Root+"Skills/Unique/능력_바이러스.asset");
        data.description="상대 기물 중 최대 3개를 무작위로 선택하여 각 기물에 [공격불가] 또는 [이동불가] 중 하나를 무작위로 부여합니다.";
        Text("Unique_Skill","unique_skill.virus.description",new[]{data.description,"Randomly selects up to 3 enemy pieces and inflicts either [Attack Disabled] or [Move Disabled] on each.","相手の駒をランダムで最大3体選び、それぞれに［攻撃不可］または［移動不可］のいずれかをランダムで付与します。"},
            new[]{new[]{"[공격불가]","[이동불가]","3개"},new[]{"[Attack Disabled]","[Move Disabled]","3 enemy pieces"},new[]{"［攻撃不可］","［移動不可］","3体"}},
            new[]{TooltipTextRole.Negative,TooltipTextRole.Negative,TooltipTextRole.Value});
        EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
        StatusAbilitiesChecks.Run();
    }
    public static void Run()
    {
        try
        {
            var teleport=Asset<StatusEffectData>(Root+"StatusEffect/상태효과_공간이동.asset");
            teleport.effectType=StatusEffectType.Teleport;teleport.effectName="공간이동";
            teleport.description="자신의 진영 턴이 종료되면 필드의 빈 타일 중 한 곳으로 무작위 이동한 뒤 [공간이동] 상태가 해제됩니다.";
            teleport.iconSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Icon/증식.png");
            teleport.localizedEffectName=new LocalizedString("Status_Effect","status_effect.teleport.name");
            teleport.localizedDescription=new LocalizedString("Status_Effect","status_effect.teleport.description");
            Text("Status_Effect","status_effect.teleport.name",new[]{"공간이동","Teleportation","空間移動"});
            Text("Status_Effect","status_effect.teleport.description",new[]{teleport.description,"At the end of its team's turn, moves to a random empty tile on the field, then removes [Teleportation].","自陣営のターン終了時、フィールドの空いているマスのいずれかにランダムで移動し、その後［空間移動］状態が解除されます。"});
            Register<StatusEffectDatabase>("statusEffectDataList",teleport);EditorUtility.SetDirty(teleport);
            var defence=AssetDatabase.LoadAssetAtPath<StatusEffectData>(Root+"StatusEffect/상태효과_방어.asset");
            var evasion=AssetDatabase.LoadAssetAtPath<StatusEffectData>(Root+"StatusEffect/상태효과_회피.asset");
            var attack=AssetDatabase.LoadAssetAtPath<StatusEffectData>(Root+"StatusEffect/상태효과_공격불가.asset");
            var move=AssetDatabase.LoadAssetAtPath<StatusEffectData>(Root+"StatusEffect/상태효과_이동불가.asset");
            var statuses=new[]{defence,teleport,attack,evasion};
            var types=new[]{UniqueSkillType.GuardSelf,UniqueSkillType.TeleportSelf,UniqueSkillType.Virus,UniqueSkillType.EvadeSelf};
            string[] keys={"guard_self","teleport_self","virus","evade_self"};
            string[][] names={new[]{"방어하기","Defend","防御する"},new[]{"공간이동","Teleportation","空間移動"},new[]{"바이러스","Virus","ウイルス"},new[]{"회피하기","Evade","回避する"}};
            string[][] descriptions={
                new[]{"자신에게 [방어]를 부여합니다.","Grants [Defense] to itself.","自身に［防御］を付与します。"},
                new[]{"자신에게 [공간이동]을 부여합니다.","Grants [Teleportation] to itself.","自身に［空間移動］を付与します。"},
                new[]{"상대 기물 중 최대 3개를 무작위로 선택하여 각 기물에 [공격불가] 또는 [이동불가] 중 하나를 무작위로 부여합니다.","Randomly selects up to 3 enemy pieces and inflicts either [Attack Disabled] or [Move Disabled] on each.","相手の駒をランダムで最大3体選び、それぞれに［攻撃不可］または［移動不可］のいずれかをランダムで付与します。"},
                new[]{"자신에게 [회피]를 부여합니다.","Grants [Evasion] to itself.","自身に［回避］を付与します。"}};
            string[][][] spans={
                new[]{new[]{"[방어]"},new[]{"[Defense]"},new[]{"［防御］"}},
                new[]{new[]{"[공간이동]"},new[]{"[Teleportation]"},new[]{"［空間移動］"}},
                new[]{new[]{"[공격불가]","[이동불가]","3개"},new[]{"[Attack Disabled]","[Move Disabled]","3 enemy pieces"},new[]{"［攻撃不可］","［移動不可］","3体"}},
                new[]{new[]{"[회피]"},new[]{"[Evasion]"},new[]{"［回避］"}}};
            int[] costs={3,2,3,2};
            for(int i=0;i<4;i++)
            {
                var data=Asset<UniqueSkillData>(Root+"Skills/Unique/능력_"+names[i][0]+".asset");
                data.skillType=types[i];data.skillName=names[i][0];data.description=descriptions[i][0];
                data.energyCost=costs[i];data.iconSprite=statuses[i].iconSprite;
                data.localizedSkillName=new LocalizedString("Unique_Skill","unique_skill."+keys[i]+".name");
                data.localizedDescription=new LocalizedString("Unique_Skill","unique_skill."+keys[i]+".description");
                data.localizedConditionFailMessage=new LocalizedString("Unique_Skill","unique_skill."+keys[i]+".condition_fail");
                data.conditionFailMessage=i==2?"효과를 부여할 상대 기물이 없습니다.":"조건이 맞지 않아 사용할 수 없습니다.";
                data.tooltipSections.Clear();
                data.tooltipSections.Add(new TooltipSectionData{statusEffectData=statuses[i]});
                if(i==2)
                {
                    data.tooltipSections.Add(new TooltipSectionData{statusEffectData=move});
                    data.targetPixelBurstPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effect/BlackPixelBurstEffect 2.prefab").GetComponent<PixelBurstEffect>();
                }
                Text("Unique_Skill","unique_skill."+keys[i]+".name",names[i]);
                Text("Unique_Skill","unique_skill."+keys[i]+".description",descriptions[i],spans[i],
                    i==2?new[]{TooltipTextRole.Negative,TooltipTextRole.Negative,TooltipTextRole.Value}:new[]{TooltipTextRole.Positive});
                Text("Unique_Skill","unique_skill."+keys[i]+".condition_fail",
                    i==2?new[]{data.conditionFailMessage,"There are no enemy pieces to affect.","効果を付与できる相手の駒がいません。"}:new[]{data.conditionFailMessage,"The conditions are not met, so this skill cannot be used.","使用条件を満たしていないため、使用できません。"});
                Register<UniqueSkillDatabase>("uniqueSkillDatas",data);EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("STATUS_ABILITIES_SETUP_PASS");
            StatusAbilitiesChecks.Run();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
