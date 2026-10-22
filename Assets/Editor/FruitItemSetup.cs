using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class FruitItemSetup
{
    public static void Run()
    {
        try
        {
            string[] names={"투지열매","각성열매","활력열매"};
            string[] effects={"공격불가","능력봉인","이동불가"};
            string[] en={"Fighting Fruit","Awakening Fruit","Vitality Fruit"};
            string[] enEffects={"Attack Disabled","Ability Sealed","Move Disabled"};
            string[] ja={"闘志の実","覚醒の実","活力の実"};
            string[] jaEffects={"攻撃不可","能力封印","移動不可"};
            var db=AssetDatabase.LoadAssetAtPath<BattleItemDatabase>("Assets/Devorya/Data/Item/아이템_데이터베이스.asset");
            var sample=AssetDatabase.LoadAssetAtPath<BattleItemData>("Assets/Devorya/Data/Item/아이템_1레벨_부러진 방패.asset");
            var collection=DevoryaLocalizationEditorUtility.GetOrCreateStringTableCollection("Battle_Item");
            var serialized=new SerializedObject(db);var entries=serialized.FindProperty("itemDatas");
            for(int i=0;i<3;i++)
            {
                string path="Assets/Devorya/Data/Item/아이템_"+names[i]+".asset";
                var status=AssetDatabase.LoadAssetAtPath<StatusEffectData>("Assets/Devorya/Data/StatusEffect/상태효과_"+effects[i]+".asset");
                var data=AssetDatabase.LoadAssetAtPath<BattleItemData>(path);
                if(data==null){
                    data=ScriptableObject.CreateInstance<BattleItemData>();
                    data.itemType=(BattleItemType)(3+i);data.itemName=names[i];data.description="선택한 아군 기물의 ["+effects[i]+"] 상태효과를 즉시 해제합니다.";
                    data.iconSprite=sample.iconSprite;data.onlyPlayerPiece=true;data.blockUseOnKing=false;data.useItemEffectList=true;
                    data.itemEffects=new List<BattleItemEffectEntry>{new BattleItemEffectEntry{effectType=BattleItemEffectType.RemoveStatusEffect,statusEffectData=status}};
                    data.tooltipSections=new List<TooltipSectionData>{new TooltipSectionData{statusEffectData=status}};
                    string key="battle_item_"+data.itemType;
                    string[] locales={"ko","en","ja"};string[] titles={names[i],en[i],ja[i]};
                    string[] descriptions={data.description,"Immediately removes ["+enEffects[i]+"] from the selected allied piece.","選択した味方の駒の["+jaEffects[i]+"]を即座に解除します。"};
                    for(int j=0;j<3;j++){var table=DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection,locales[j]);DevoryaLocalizationEditorUtility.SetTableValue(table,key+"_name",titles[j]);DevoryaLocalizationEditorUtility.SetTableValue(table,key+"_description",descriptions[j]);}
                    data.localizedItemName=DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection,key+"_name");data.localizedDescription=DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection,key+"_description");
                    AssetDatabase.CreateAsset(data,path);
                }
                bool exists=false;for(int j=0;j<entries.arraySize;j++)if(entries.GetArrayElementAtIndex(j).objectReferenceValue==data)exists=true;
                if(!exists){int index=entries.arraySize++;entries.GetArrayElementAtIndex(index).objectReferenceValue=data;}
                var go=new GameObject("FruitItemTest");
                var unrelated=ScriptableObject.CreateInstance<StatusEffectData>();unrelated.effectType=StatusEffectType.Defence;
                try{
                    var piece=go.AddComponent<Piece>();var handler=go.AddComponent<BattleItemEffectHandler>();
                    typeof(Piece).GetProperty("Team").SetValue(piece,PieceTeam.Player);
                    typeof(Piece).GetProperty("PieceType").SetValue(piece,PieceType.King);
                    Require(!handler.TryApplyItemEffect(data,piece),"No effect must not consume fruit");
                    Require(!handler.TryApplyItemEffect(data,null),"Null target");
                    piece.AddStatusEffect(status,3);piece.AddStatusEffect(unrelated,2);
                    Require(handler.TryApplyItemEffect(data,piece),"Apply to allied king");
                    Require(!piece.HasStatusEffect(status.effectType),"Remove status immediately");
                    Require(piece.GetStatusEffectDataCopy(StatusEffectType.Defence).remainingTurn==2,"Preserve other effects");
                    Require(!handler.TryApplyItemEffect(data,piece),"Repeated use must fail");
                    piece.AddStatusEffect(status);typeof(Piece).GetProperty("Team").SetValue(piece,PieceTeam.Enemy);
                    Require(!handler.TryApplyItemEffect(data,piece)&&piece.HasStatusEffect(status.effectType),"Reject enemy without mutation");
                    Require(data.iconSprite!=null&&data.tooltipSections[0].statusEffectData==status,"Icon and tooltip");
                }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(unrelated);}
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            for(int i=3;i<=5;i++)Require(db.GetData((BattleItemType)i)!=null,"Database registration");
            Debug.Log("FRUIT_ITEMS_PASS: three fruits, removal, unrelated status preservation, king, invalid targets, repeat use, icons and database.");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
}
