using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class BattleItemUXChecks
{
    const string Pending="BattleItemUXChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static BattleItemUXChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {
                SessionState.SetBool(Pending,false);
                new GameObject("ItemUXCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }
    static void Set(object o,string field,object value) => o.GetType().GetField(field,Flags).SetValue(o,value);
    static T Get<T>(object o,string field) => (T)o.GetType().GetField(field,Flags).GetValue(o);
    static void Check(bool pass,string text) { if(!pass)throw new Exception(text); Debug.Log("ITEM_CHECK: "+text); }
    static IEnumerator Guard()
    {
        var checks=Checks();
        while(true)
        {
            object next;
            try{if(!checks.MoveNext())break;next=checks.Current;}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}
            yield return next;
        }
        Debug.Log("ITEM_UX_PLAYMODE_PASS");
        EditorApplication.Exit(0);
    }
    static BattleItemData Item(StatusEffectData status)
    {
        var item=ScriptableObject.CreateInstance<BattleItemData>();
        item.itemType=BattleItemType.ApplyStatusEffectToSelectedPiece;
        item.applyStatusEffectData=status;
        return item;
    }
    static IEnumerator Checks()
    {
        yield return LocalizationSettings.InitializationOperation;
        yield return new WaitForSecondsRealtime(5);
        var bm=BattleManager.Instance;
        Check(bm!=null,"BattleScene manager loaded");
        bm.StopAllCoroutines();
        Set(bm,"battleAIManager",null);
        var eventController=Get<EventSequenceController>(bm,"eventSequenceController");
        if(eventController!=null)eventController.StopAllCoroutines();
        Set(bm,"eventSequenceController",null);
        Set(bm,"isPlayerDeploymentPhase",false);Set(bm,"isBattleAnnouncementPlaying",false);
        Set(bm,"isActionAnimating",false);Set(bm,"isBattleEnded",false);Set(bm,"currentTurn",BattleTurn.Player);
        var pm=Get<PieceManager>(bm,"pieceManager");
        var board=Get<BoardManager>(bm,"boardManager");
        var manager=Get<BattleItemManager>(bm,"battleItemManager");
        var handler=Get<BattleItemEffectHandler>(bm,"battleItemEffectHandler");
        Check(board.Width>=5 && board.Height>=5,"Real board initialized");
        pm.ClearAllPieces();
        var a=pm.SpawnPiece(PieceType.Pawn,PieceTeam.Player,1,1,true);
        var b=pm.SpawnPiece(PieceType.Pawn,PieceTeam.Player,2,1,true);
        var enemy=pm.SpawnPiece(PieceType.Pawn,PieceTeam.Enemy,4,4,true);
        pm.SpawnPiece(PieceType.King,PieceTeam.Enemy,4,0,true);
        var king=pm.SpawnPiece(PieceType.King,PieceTeam.Player,0,0,true);
        var run=RunStateManager.Instance;
        while(run.GetBattleItemsCopy().Count>0)run.RemoveBattleItemAt(0);
        manager.Initialize(bm,Get<BattleUIController>(bm,"battleUIController"));
        var status=ScriptableObject.CreateInstance<StatusEffectData>();
        status.effectType=StatusEffectType.AttackDisabled;status.durationTurn=1;
        var item=Item(status);
        Check(manager.AddBattleItem(item)&&manager.AddBattleItem(item),"Inventory setup");
        var random=UnityEngine.Random.state;
        for(int i=0;i<10;i++)Check(handler.ValidateItemEffect(item,a)==BattleItemFailure.None,"Pure validation");
        Check(random.Equals(UnityEngine.Random.state)&&!a.HasStatusEffect(status.effectType),"Preview has no effects/random draws");
        Check(handler.ValidateItemEffect(item,enemy)==BattleItemFailure.PlayerOnly,"Player-only reason");
        Check(handler.ValidateItemEffect(item,king)==BattleItemFailure.None,"King accepts status item");
        var ui=Get<BattleUIController>(bm,"battleUIController");
        ui.SetLastEnemyAbsorbMode(true);
        var icon=Get<Image>(ui,"absorbIconImage");
        var original=icon.sprite;
        var originalIconSize=icon.rectTransform.sizeDelta;
        bool originalIconAspect=icon.preserveAspect;
        bm.UseItemAtSlot(0);
        Check(Get<bool>(ui,"isItemCancelMode")&&icon.sprite!=original,"Cancel icon overrides final absorption");
        var cancelButtonRect=(RectTransform)Get<Button>(ui,"absorbButton").transform;
        Check(icon.rectTransform.rect.width<=cancelButtonRect.rect.width*.6f && icon.rectTransform.rect.height<=cancelButtonRect.rect.height*.6f && icon.preserveAspect,"Cancel sprite fits button with padding and keeps aspect");
        ui.SetAbsorbModeIcon(false);ui.SetAbsorbButtonVisible(false);
        Check(icon.sprite!=original&&Get<Button>(ui,"absorbButton").gameObject.activeSelf,"Cancel button survives normal refresh");
        ui.GetType().GetMethod("OnClickAbsorbButton",Flags).Invoke(ui,null);
        Check(!bm.IsItemTargeting&&icon.sprite==original&&!Get<bool>(bm,"isActionAnimating"),"Cancel click restores final absorption without executing it");
        Check(icon.rectTransform.sizeDelta==originalIconSize && icon.preserveAspect==originalIconAspect,"Cancel restores authored icon dimensions and aspect");
        ui.SetLastEnemyAbsorbMode(false);
        bm.SelectPiece(a);
        bm.UseItemAtSlot(0);
        Check(bm.IsItemTargeting&&!bm.HasSelectedPiece(),"Slot selects mode and deselects piece");
        var targets=Get<List<Tile>>(bm,"itemTargetTiles");
        Check(targets.Count==3,"Only valid occupied blue tiles");
        var tile=board.GetTile(a.X,a.Y);var tileB=board.GetTile(b.X,b.Y);
        bm.SelectTile(tile);
        Check(run.GetBattleItemsCopy().Count==2&&!a.HasStatusEffect(status.effectType),"First click no consumption/effect");
        Check(Get<Tile>(bm,"confirmedItemTile")==tile && Get<Piece>(bm,"selectedPiece")==a,"First click confirms and selects piece");
        bm.SelectTile(tile);
        Check(run.GetBattleItemsCopy().Count==2,"Duplicate delivery in same frame ignored");
        yield return null;
        bm.SelectTile(tileB);
        Check(Get<Tile>(bm,"confirmedItemTile")==tileB,"Switch candidate");
        yield return null;
        bm.SelectTile(tileB);
        Check(run.GetBattleItemsCopy().Count==1&&b.HasStatusEffect(status.effectType)&&!bm.IsItemTargeting,"Second click consumes once and clears");
        Check(manager.GetItemAtSlot(0)==item&&manager.GetItemAtSlot(1)==null,"RunState and compressed slots match");
        bm.UseItemAtSlot(0);bm.UseItemAtSlot(0);Check(!bm.IsItemTargeting,"Same slot cancels");
        bm.UseItemAtSlot(0);
        yield return null;
        bm.SelectTile(board.GetTile(3,3));Check(bm.IsItemTargeting,"Empty tile keeps mode");
        yield return null;
        bm.SelectTile(board.GetTile(enemy.X,enemy.Y));
        Check(bm.IsItemTargeting&&run.GetBattleItemsCopy().Count==1,"Invalid target keeps inventory");
        Set(bm,"isActionAnimating",true);
        yield return null;Check(!bm.IsItemTargeting,"Action lock cleans mode");
        Set(bm,"isActionAnimating",false);
        bm.UseItemAtSlot(0);Set(bm,"currentTurn",BattleTurn.Enemy);
        yield return null;Check(!bm.IsItemTargeting&&!bm.CanUseBattleItem(),"Enemy turn cleans mode");
        Set(bm,"currentTurn",BattleTurn.Player);
        bm.UseItemAtSlot(0);run.RemoveBattleItemAt(0);
        yield return null;
        Check(!bm.IsItemTargeting&&!a.HasStatusEffect(status.effectType),"RunState mismatch never applies effect");
        manager.Initialize(bm,Get<BattleUIController>(bm,"battleUIController"));
        var remove=Item(status);remove.useItemEffectList=true;
        remove.itemEffects=new List<BattleItemEffectEntry>{new BattleItemEffectEntry{effectType=BattleItemEffectType.RemoveStatusEffect,statusEffectData=status}};
        Check(handler.ValidateItemEffect(remove,a)==BattleItemFailure.MissingStatus,"Missing status rejected");
        Check(handler.TryApplyItemEffect(remove,b),"Status removal executes");
        Check(!b.HasStatusEffect(status.effectType),"Status removed");
        var composite=Item(status);composite.useItemEffectList=true;
        composite.itemEffects=new List<BattleItemEffectEntry>{
            new BattleItemEffectEntry{effectType=BattleItemEffectType.ApplyStatusEffect,statusEffectData=status},
            new BattleItemEffectEntry{effectType=BattleItemEffectType.RemoveStatusEffect,statusEffectData=status}};
        Check(!handler.TryApplyItemEffect(composite,a)&&!a.HasStatusEffect(status.effectType),"Invalid composite cannot partially apply");
        composite.itemEffects[1]=new BattleItemEffectEntry{effectType=BattleItemEffectType.ExtraMove};
        Check(handler.ValidateItemEffect(composite,a)==BattleItemFailure.None,"Boots composite valid");
        Check(handler.TryApplyItemEffect(composite,a)&&a.IsQuickStepMove,"ExtraMove composite grants nonattack movement");
        Set(bm,"itemMovePiece",null);bm.GetType().GetMethod("ClearQuickStepBonus",Flags).Invoke(bm,null);
        var change=Item(status);change.itemType=BattleItemType.ChangeSelectedPieceToJelluPawn;
        change.changeTargetPieceType=PieceType.Rook;
        Check(handler.TryApplyItemEffect(change,b)&&b.PieceType==PieceType.Rook,"Legacy ChangePiece");
        change.changeTargetPieceType=(PieceType)999;
        Check(!handler.TryApplyItemEffect(change,b)&&b.PieceType==PieceType.Rook,"Invalid ChangePiece no mutation");
        manager.AddBattleItem(remove);
        bm.UseItemAtSlot(0);
        Check(bm.IsItemTargeting,"Remaining status yields candidate");
        bm.CancelItemTargeting();a.RemoveStatusEffect(status.effectType);
        bm.UseItemAtSlot(0);Check(!bm.IsItemTargeting,"No candidates refuses entry");
        Check(manager.AddBattleItem(item),"Second test item added");
        bm.UseItemAtSlot(1);
        yield return null;
        bm.SelectTile(tile);
        var grid=Get<Piece[,]>(pm,"pieces");
        grid[a.X,a.Y]=null;a.gameObject.SetActive(false);
        yield return null;
        Check(bm.IsItemTargeting&&Get<Tile>(bm,"confirmedItemTile")==null,"Missing target recomputes blue candidates");
        grid[a.X,a.Y]=a;a.gameObject.SetActive(true);
        bm.CancelItemTargeting();
        bm.UseItemAtSlot(1);
        if(eventController!=null)
        {
            Set(eventController,"isSequenceActive",true);Set(eventController,"isWaitingForTileSelection",true);
            Set(bm,"eventSequenceController",eventController);
            yield return null;
            Check(!bm.IsItemTargeting&&!bm.CanUseBattleItem(),"Event Force input cleans and blocks item mode");
            Set(eventController,"isSequenceActive",false);Set(eventController,"isWaitingForTileSelection",false);
            Set(bm,"eventSequenceController",null);
        }
        bm.UseItemAtSlot(1);bm.enabled=false;
        Check(!bm.IsItemTargeting,"Disable cleans mode");bm.enabled=true;
        // Deployment still uses the standard blue overlay and rejects items.
        Set(bm,"isPlayerDeploymentPhase",true);bm.SelectPiece(a);
        Check(Get<Piece>(bm,"selectedDeploymentPiece")==a,"Deployment selection preserved");
        Check(!bm.CanUseBattleItem(),"Deployment rejects items");
        bm.GetType().GetMethod("ClearDeploymentSelection",Flags).Invoke(bm,null);
        Set(bm,"isPlayerDeploymentPhase",false);
        bm.SelectPiece(b);
        var moves=Get<List<Tile>>(bm,"selectableTiles");
        var destination=moves.First(t=>pm.GetPieceAt(t.X,t.Y)==null);
        var before=new Vector2Int(b.X,b.Y);
        bm.SelectTile(destination);
        Check(b.X==before.x&&b.Y==before.y&&Get<Tile>(bm,"pendingActionTile")==destination,"Normal move first click preserved");
        yield return null;
        bm.SelectTile(destination);
        for(float deadline=Time.realtimeSinceStartup+15;Time.realtimeSinceStartup<deadline&&Get<bool>(bm,"isActionAnimating");)yield return null;
        Check(b.X==destination.X&&b.Y==destination.Y,"Normal move second click executes");
        Set(bm,"currentTurn",BattleTurn.Player);Set(bm,"isBattleAnnouncementPlaying",false);
        var attackTile=Get<BattleMoveValidator>(bm,"battleMoveValidator").GetSelectablePositions(b)
            .Select(pos=>board.GetTile(pos.x,pos.y)).First(t=>pm.GetPieceAt(t.X,t.Y)==null);
        var victim=pm.SpawnPiece(PieceType.Pawn,PieceTeam.Enemy,attackTile.X,attackTile.Y,true);
        bm.SelectPiece(b);bm.SelectTile(attackTile);
        Check(pm.GetPieceAt(attackTile.X,attackTile.Y)==victim,"Normal attack first click preserves target");
        yield return null;bm.SelectTile(attackTile);
        for(float deadline=Time.realtimeSinceStartup+15;Time.realtimeSinceStartup<deadline&&Get<bool>(bm,"isActionAnimating");)yield return null;
        Check(pm.GetPieceAt(attackTile.X,attackTile.Y)==b,"Normal attack second click executes");
        Set(bm,"currentTurn",BattleTurn.Player);Set(bm,"isBattleAnnouncementPlaying",false);
        // A pawn blocked straight ahead can become a rook and then gain a legal extra move.
        var front=pm.GetPieceAt(a.X,a.Y+1);
        if(front==null)pm.SpawnPiece(PieceType.Pawn,PieceTeam.Player,a.X,a.Y+1,true);
        var transformMove=Item(status);transformMove.useItemEffectList=true;
        transformMove.itemEffects=new List<BattleItemEffectEntry>{
            new BattleItemEffectEntry{effectType=BattleItemEffectType.ChangePiece,changeTargetPieceType=PieceType.Rook},
            new BattleItemEffectEntry{effectType=BattleItemEffectType.ExtraMove}};
        Check(handler.ValidateItemEffect(transformMove,a)==BattleItemFailure.None,"Projected transform then move validates");
        Check(handler.TryApplyItemEffect(transformMove,a)&&a.PieceType==PieceType.Rook&&a.IsQuickStepMove,"Projected composite executes without partial failure");
        // The same physical button cancels a committed extra move without refunding the item.
        a.AddStatusEffect(status);
        ui.RefreshSharedCancelButton();
        var cancelButton=Get<Button>(ui,"absorbButton");
        Check(cancelButton.gameObject.activeSelf && Get<bool>(ui,"isItemCancelMode"),"Extra move displays existing X button");
        Check(cancelButton.interactable && icon.preserveAspect,"Extra move cancel is usable and fitted");
        var itemCount=manager.GetItemAtSlot(0);
        ui.GetType().GetMethod("OnClickAbsorbButton",Flags).Invoke(ui,null);
        Check(!a.IsQuickStepMove && bm.GetChanceAttackBonusPiece()==null && !bm.HasPendingPlayerBonusAction,"Item bonus fully cleared");
        Check(Get<BattleTurn>(bm,"currentTurn")==BattleTurn.Player,"Cancel item bonus preserves normal action");
        Check(a.HasStatusEffect(StatusEffectType.AttackDisabled) && manager.GetItemAtSlot(0)==itemCount,"Cancellation retains applied status and inventory");
        Check(Get<Tile>(bm,"pendingActionTile")==null && !bm.TryCancelBonusAction(),"Confirmation cleared and repeated cancel ignored");
        foreach(string activation in new[]{"ActivateQuickStepBonus","ActivateChanceAttackBonus"})
        {
            Set(bm,"currentTurn",BattleTurn.Player);Set(bm,"isActionAnimating",false);
            bm.GetType().GetMethod(activation,Flags).Invoke(bm,new object[]{b});
            ui.RefreshSharedCancelButton();
            Check(bm.CanCancelBonusAction && cancelButton.gameObject.activeSelf,"Shared bonus cancellable: "+activation);
            Set(bm,"isActionAnimating",true);ui.RefreshSharedCancelButton();
            Check(!cancelButton.interactable && !bm.TryCancelBonusAction(),"Animation rejects cancellation");
            Set(bm,"isActionAnimating",false);
            Set(bm,"currentTurn",BattleTurn.Enemy);
            Check(!bm.TryCancelBonusAction(),"Enemy bonus cannot be cancelled by player");
            Set(bm,"currentTurn",BattleTurn.Player);
            Set(bm,"isPlayerDeploymentPhase",true);
            Check(!bm.TryCancelBonusAction(),"Deployment cannot cancel bonus");
            Set(bm,"isPlayerDeploymentPhase",false);
            if(eventController!=null)
            {
                Set(eventController,"isSequenceActive",true);Set(eventController,"isWaitingForTileSelection",true);
                Set(bm,"eventSequenceController",eventController);
                Check(!bm.TryCancelBonusAction(),"Forced event input blocks bonus cancellation");
                Set(eventController,"isSequenceActive",false);Set(eventController,"isWaitingForTileSelection",false);
                Set(bm,"eventSequenceController",null);
            }
            Set(bm,"targetItemSlot",0);ui.SetItemTargetSlot(0);
            ui.GetType().GetMethod("OnClickAbsorbButton",Flags).Invoke(ui,null);
            Check(!bm.IsItemTargeting && bm.HasPendingPlayerBonusAction,"Item target cancel has priority over bonus cancel");
            ui.RefreshSharedCancelButton();
            Check(Get<bool>(ui,"isItemCancelMode"),"X remains for pending bonus after item target cancellation");
            ui.GetType().GetMethod("OnClickAbsorbButton",Flags).Invoke(ui,null);
            Check(Get<BattleTurn>(bm,"currentTurn")==BattleTurn.Enemy && !bm.HasPendingPlayerBonusAction,"Post-action bonus cancellation ends turn");
            Check(!b.IsQuickStepMove && bm.GetChanceAttackBonusPiece()==null,"Bonus flags cleared before next turn");
            ui.RefreshSharedCancelButton();
            Check(!Get<bool>(ui,"isItemCancelMode"),"Shared button exits cancellation mode");
        }
        foreach(var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if(!new[]{"ko","en","ja"}.Contains(locale.Identifier.Code))continue;
            LocalizationSettings.SelectedLocale=locale;
            var instruction=new UnityEngine.Localization.LocalizedString("Battle_UI","battle.ui.item.select_target").GetLocalizedString();
            Check(!string.IsNullOrWhiteSpace(instruction)&&!instruction.Contains("No translation"),"Selection instruction "+locale.Identifier.Code);
            foreach(BattleItemFailure failure in Enum.GetValues(typeof(BattleItemFailure)))
                if(failure!=BattleItemFailure.None)
                {
                    var text=BattleItemFailureText.Get(failure);
                    Check(!string.IsNullOrWhiteSpace(text)&&!text.Contains("No translation"),"Localized reason "+locale.Identifier.Code+" "+failure);
                }
        }
        var slot=new GameObject("ReadOnlySlot",typeof(RectTransform),typeof(Image),typeof(Button)).AddComponent<BattleItemSlotUI>();
        slot.InitializeReadOnly();slot.Refresh(item);slot.SetItemSelected(true);
        Check(!Get<bool>(slot.GetComponent<SlotHoverFeedback>(),"selected"),"ReadOnly ignores selection effect");
        Debug.Log("ITEM_UX_INPUT_NOTE: SelectTile PC/mobile shared routing exercised; physical device interaction not automated.");
    }
}
