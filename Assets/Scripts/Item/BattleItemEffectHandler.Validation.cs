using System;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleItemEffectHandler
{
    public bool TryApplyItemEffect(BattleItemData item, Piece target, out BattleItemFailure failure)
    {
        failure = ValidateItemEffect(item, target);
        if (failure != BattleItemFailure.None) return false;
        if (TryApplyItemEffect(item, target)) return true;
        failure = BattleItemFailure.Execution;
        return false;
    }

    // No random draws, status changes, temporary GameObjects or inventory writes.
    public BattleItemFailure ValidateItemEffect(BattleItemData item, Piece target)
    {
        if (item == null || item.itemType == BattleItemType.None) return BattleItemFailure.MissingItem;
        if (target == null || !target.isActiveAndEnabled || pieceManager == null ||
            pieceManager.GetPieceAt(target.X, target.Y) != target) return BattleItemFailure.MissingTarget;
        if (item.onlyPlayerPiece && target.Team != PieceTeam.Player) return BattleItemFailure.PlayerOnly;
        if (ChangesPiece(item) && target.PieceType == PieceType.King) return BattleItemFailure.King;
        if (!item.useItemEffectList)
        {
            if (item.itemType == BattleItemType.ChangeSelectedPieceToJelluPawn)
                return ValidChange(item.changeTargetPieceData, item.changeTargetPieceType,
                    item.changeTargetUniqueSkill, item.changeTargetGeneralSkill) ? BattleItemFailure.None : BattleItemFailure.Requirements;
            if (item.itemType == BattleItemType.ApplyStatusEffectToSelectedPiece)
                return ValidStatus(item.applyStatusEffectData) ? BattleItemFailure.None : BattleItemFailure.Requirements;
            return BattleItemFailure.Requirements;
        }
        if (item.itemEffects == null || item.itemEffects.Count == 0) return BattleItemFailure.Requirements;
        var removed = new HashSet<StatusEffectType>();
        PieceType? projectedType = null;
        bool extraMove = false, blocksMovement = false;
        foreach (var effect in item.itemEffects)
        {
            if (effect == null) return BattleItemFailure.Requirements;
            switch (effect.effectType)
            {
                case BattleItemEffectType.ChangePiece:
                    if(projectedType==PieceType.King)return BattleItemFailure.King;
                    if (!ValidChange(effect.changeTargetPieceData, effect.changeTargetPieceType,
                        effect.changeTargetUniqueSkill, effect.changeTargetGeneralSkill)) return BattleItemFailure.Requirements;
                    projectedType = effect.changeTargetPieceData != null ? effect.changeTargetPieceData.pieceType : effect.changeTargetPieceType;
                    break;
                case BattleItemEffectType.ApplyStatusEffect:
                    if (!ValidStatus(effect.statusEffectData)) return BattleItemFailure.Requirements;
                    // Resistance can prevent an applied status; never count it as a guaranteed prerequisite.
                    if (effect.statusEffectData.effectType == StatusEffectType.ActionDisabled ||
                        effect.statusEffectData.effectType == StatusEffectType.MoveDisabled) blocksMovement = true;
                    break;
                case BattleItemEffectType.RemoveStatusEffect:
                    if (!ValidStatus(effect.statusEffectData) || !target.HasStatusEffect(effect.statusEffectData.effectType) ||
                        !removed.Add(effect.statusEffectData.effectType)) return BattleItemFailure.MissingStatus;
                    break;
                case BattleItemEffectType.ExtraMove:
                    if (extraMove || blocksMovement || battleManager == null || !battleManager.CanGrantItemMove(target, projectedType))
                        return BattleItemFailure.ExtraMove;
                    extraMove = true;
                    break;
                default: return BattleItemFailure.Requirements;
            }
        }
        return BattleItemFailure.None;
    }

    public static bool ChangesPiece(BattleItemData item)
    {
        if(item==null)return false;
        if(!item.useItemEffectList)return item.itemType==BattleItemType.ChangeSelectedPieceToJelluPawn;
        if(item.itemEffects!=null)foreach(var effect in item.itemEffects)
            if(effect!=null && effect.effectType==BattleItemEffectType.ChangePiece)return true;
        return false;
    }

    private static bool ValidStatus(StatusEffectData data) =>
        data != null && data.effectType != StatusEffectType.None && Enum.IsDefined(typeof(StatusEffectType), data.effectType);

    private static bool ValidChange(PieceData data, PieceType type, UniqueSkillType ability, GeneralSkillType trait) =>
        Enum.IsDefined(typeof(PieceType), data != null ? data.pieceType : type) &&
        Enum.IsDefined(typeof(UniqueSkillType), data != null ? data.uniqueSkill : ability) &&
        Enum.IsDefined(typeof(GeneralSkillType), trait);
}
