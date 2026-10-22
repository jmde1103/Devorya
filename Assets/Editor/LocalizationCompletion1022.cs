using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
public static class LocalizationCompletion1022
{
    static int changed;
    static void Set(string collection,long id,string locale,string value)
    {
        var c=LocalizationEditorSettings.GetStringTableCollection(collection);
        var t=(StringTable)c.GetTable(locale);
        if(c.SharedData.GetEntry(id)==null)throw new Exception("Missing original key: "+id);
        var e=t.GetEntry(id);if(e!=null&&e.Value==value)return;
        t.AddEntry(id,value);EditorUtility.SetDirty(t);changed++;
    }
    static void Pair(string c,long id,string en,string ja){Set(c,id,"en",en);Set(c,id,"ja",ja);}
    public static void Run()
    {
        try {
            Pair("EventScene_Dialogue",734662656,"Hello.","こんにちは。");
            Pair("EventScene_Dialogue",1453953196363776,"The screen shakes.","画面が揺れます。");
            Pair("EventScene_Dialogue",12328324995616768,"!@#$!@$!$!@$!","!@#$!@$!$!@$!");
            Set("EventScene_Dialogue",12347867402997760,"ko","쓸 만한 것을 발견했다!");
            Pair("EventScene_Dialogue",12361164965679104,"!@#!$!@$!@$@~~~","!@#!$!@$!@$@~~~");
            Pair("EventScene_Dialogue",12564537832771584,"Reboot complete... GPS system error... Signal anomaly. Communication with the laboratory has been lost.","再起動完了…GPSシステムエラー…信号異常。研究所との通信が途絶えました。");
            Pair("EventScene_Dialogue",13493486704267264,"By absorbing the energy remaining in the high-output energy storage unit,","高出力エネルギー貯蔵装置に残ったエネルギーを吸収することで、");
            Pair("EventScene_Dialogue",14177455842136064,"You can implement various features like these directly in the scene.","このように、さまざまな機能をシーンに直接組み込んで実装できます。");
            Pair("EventScene_Dialogue",14177483818143744,"Life signs detected nearby.","周辺に生命体の反応を検知しました。");
            Pair("EventScene_Dialogue",14177483818143745,"!@#!@$!!!!!!!!!!!","!@#!@$!!!!!!!!!!!");
            Set("Event_Dialogue",12882623169433600,"ko","이번 전투에서 흡수한 기물 수에 따라 데보리아가 회복됩니다.");
            Pair("Event_Dialogue",12882623169433600,"Devorya recovers based on the number of pieces absorbed during this battle.","この戦闘で吸収した駒の数に応じて、デボリアが回復します。");
            Set("Event_Dialogue",15071080922877952,"ko","[폰] 타입의 생명체에서 특이 행동이 감지되었습니다.");
            Set("Event_Dialogue",15071705370857472,"ko","분석 중... 해당 개체는 분열, 증식, 합성 등 다양한 능력을 가진 액체 형태의 생명체로 확인됩니다.");
            Pair("Event_Dialogue",15113949066956800,"Absorbing and reconstructing the [Bishop]-type entity.","[ビショップ]タイプの個体を吸収し、再構成します。");
            Pair("Event_Dialogue",15113949142454272,"Reconstruction of the absorbed entity's form, ability, traits, and type complete!","吸収した個体の形態・能力・特性・タイプの再構成が完了しました！");
            Pair("Event_Dialogue",15113949142454273,"The opposing entity is preparing to attack.","敵の個体が攻撃の準備をしています。");
            Pair("Event_Dialogue",15113949142454274,"Use the reconstructed [Bishop]-type ability to minimize damage.","再構成した[ビショップ]タイプの能力を活用し、被害を最小限に抑えます。");
            Set("Event_Dialogue",15167850965241856,"ko","주변 생명체의 반응이 감지되지 않습니다. 새로운 목적지를 탐색하고 이동합니다.");
            Set("Piece_Data",332976420950016,"ko","데보리아의 분열체 타입: [비숍]");
            Set("Piece_Data",335905739640832,"ko","데보리아의 분열체 타입: [폰]");
            Set("Piece_Data",667822833930240,"ko","정체를 알 수 없는 검은 존재. 생명체를 흡수하여 그 모습과 능력을 자신의 것으로 만든다.");
            // Correct translations that still describe an older item effect.
            Pair("Battle_Item",192863145984,"Transforms the selected allied piece into a Jellu [Pawn].","選択した味方の駒をゼル［ポーン］に変形させます。");
            Set("Battle_Item",11941842944,"ja","青いスライムの欠片");
            Pair("Battle_Item",34076602535936,"Broken Shield","壊れた盾");
            Pair("Battle_Item",34076606730240,"Applies [Defense] and [Action Disabled] to the selected piece for 1 turn.","選択した駒に[防御]と[行動不可]を1ターン付与します。");
            Set("Unique_Skill",88777704476674,"en","The conditions are not met, so this skill cannot be used.");
            var effect=AssetDatabase.LoadAssetAtPath<StatusEffectData>("Assets/Devorya/Data/StatusEffect/상태효과_행동불가.asset");
            Bind(effect,"localizedEffectName","Status_Effect","status_effect.action_disabled.name",effect.effectName,"Action Disabled","行動不可");
            Bind(effect,"localizedDescription","Status_Effect","status_effect.action_disabled.description",effect.description,"While [Action Disabled], this piece cannot move or attack this turn.","[行動不可]状態の駒は、このターン移動も攻撃もできません。");
            var item=AssetDatabase.LoadAssetAtPath<BattleItemData>("Assets/Devorya/Data/Item/아이템_테스트.asset");
            Bind(item,"localizedItemName","Battle_Item","battle_item.test.name",item.itemName,"Test","テスト");
            Bind(item,"localizedDescription","Battle_Item","battle_item.test.description",item.description,"Cannot act.","行動できません。");
            AssetDatabase.SaveAssets();
            int entries=0,refs=0;
            foreach(var c in LocalizationEditorSettings.GetStringTableCollections())
                foreach(var shared in c.SharedData.Entries)
                    foreach(var locale in new[]{"ko","en","ja"}) {
                        var table=c.GetTable(locale) as StringTable;
                        if(table==null)throw new Exception("Missing locale: "+c.TableCollectionName+"/"+locale);
                        var entry=table.GetEntry(shared.Id);
                        if(entry==null||string.IsNullOrWhiteSpace(entry.Value))throw new Exception("Blank translation: "+c.TableCollectionName+"/"+shared.Key+"/"+locale);
                        entries++;
                    }
            foreach(var type in new[]{"UniqueSkillData","GeneralSkillData","StatusEffectData","BattleItemData","BattleRelicData"})
                foreach(var guid in AssetDatabase.FindAssets("t:"+type)) {
                    var asset=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    var so=new SerializedObject(asset);var it=so.GetIterator();
                    while(it.NextVisible(true)) {
                        if(it.type!="LocalizedString")continue;
                        var table=it.FindPropertyRelative("m_TableReference.m_TableCollectionName");
                        var id=it.FindPropertyRelative("m_TableEntryReference.m_KeyId");
                        if(table==null||string.IsNullOrEmpty(table.stringValue)||id.longValue==0)throw new Exception("Unbound localization: "+asset.name+"/"+it.propertyPath);
                        refs++;
                    }
                }
            Debug.Log("LOCALIZATION_COMPLETION_PASS: "+changed+" values filled/corrected; "+entries+" table entries checked; "+refs+" asset references checked.");
            EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Bind(UnityEngine.Object asset,string field,string collection,string key,string ko,string en,string ja)
    {
        var c=LocalizationEditorSettings.GetStringTableCollection(collection);
        var values=new[]{ko,en,ja};var locales=new[]{"ko","en","ja"};
        for(int i=0;i<3;i++){
            var table=(StringTable)c.GetTable(locales[i]);var entry=table.GetEntry(key);
            if(entry==null||string.IsNullOrWhiteSpace(entry.Value)){table.AddEntry(key,values[i]);EditorUtility.SetDirty(table);changed++;}
        }
        EditorUtility.SetDirty(c.SharedData);
        var so=new SerializedObject(asset);var prop=so.FindProperty(field);
        prop.FindPropertyRelative("m_TableReference.m_TableCollectionName").stringValue="GUID:"+c.SharedData.TableCollectionNameGuid.ToString("N");
        prop.FindPropertyRelative("m_TableEntryReference.m_KeyId").longValue=c.SharedData.GetEntry(key).Id;
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
    }
}
