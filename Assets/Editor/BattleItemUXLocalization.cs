using System;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class BattleItemUXLocalization
{
    public static void Apply()
    {
        try
        {
            string[,] rows = {
                {"WrongTurn","현재 아이템을 사용할 수 없습니다.","Items cannot be used right now.","現在アイテムを使用できません。"},
                {"Busy","현재 다른 행동이 진행 중이므로 아이템을 사용할 수 없습니다.","Items cannot be used while another action is in progress.","ほかの行動中はアイテムを使用できません。"},
                {"InvalidPiece","해당 기물에는 아이템을 사용할 수 없습니다.","This item cannot be used on that piece.","この駒にはアイテムを使用できません。"},
                {"King","[킹]에게는 해당 아이템을 사용할 수 없습니다.","This item cannot be used on a [King].","［キング］にはこのアイテムを使用できません。"},
                {"PlayerOnly","플레이어 기물에만 사용할 수 있습니다.","This item can only be used on your pieces.","プレイヤーの駒にのみ使用できます。"},
                {"MissingStatus","해제할 상태효과가 없습니다.","There is no matching status effect to remove.","解除できる状態効果がありません。"},
                {"ExtraMove","해당 기물은 추가 이동 기회를 얻을 수 없습니다.","This piece cannot gain an extra move.","この駒は追加移動を獲得できません。"},
                {"Requirements","아이템 사용 조건을 충족하지 못했습니다.","The requirements for using this item are not met.","アイテムの使用条件を満たしていません。"},
                {"NoCandidates","이 아이템을 사용할 수 있는 기물이 없습니다.","There are no valid targets for this item.","このアイテムを使用できる駒がありません。"},
                {"MissingItem","선택한 아이템을 사용할 수 없습니다.","The selected item is no longer available.","選択したアイテムは使用できなくなりました。"},
                {"MissingTarget","선택한 대상이 더 이상 존재하지 않습니다.","The selected target is no longer available.","選択した対象が存在しなくなりました。"},
                {"Execution","아이템 사용에 실패했습니다.","The item could not be used.","アイテムの使用に失敗しました。"}
            };
            var collection = LocalizationEditorSettings.GetStringTableCollection("Battle_UI");
            string[] locales = {"ko","en","ja"};
            for (int l=0;l<3;l++)
            {
                var table = (StringTable)collection.GetTable(new LocaleIdentifier(locales[l]));
                for (int i=0;i<rows.GetLength(0);i++)
                    table.AddEntry("battle.ui.item_fail."+rows[i,0],rows[i,l+1]);
                string[] instructions = {
                    "아이템을 사용할 기물을 선택해 주세요.",
                    "Select a piece to use this item on.",
                    "アイテムを使用する駒を選択してください。"};
                table.AddEntry("battle.ui.item.select_target", instructions[l]);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            Debug.Log("ITEM_UX_LOCALIZATION_PASS: 12 reasons x 3 locales");
            EditorApplication.Exit(0);
        }
        catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
