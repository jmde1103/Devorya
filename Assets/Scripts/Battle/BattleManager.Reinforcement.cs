using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    private Piece reinforcementCaster;
    private readonly List<Piece> reinforcementTargets = new List<Piece>();

    private void BeginReinforcementSelection(Piece caster, UniqueSkillData data)
    {
        if (!battleSkillManager.CanUseUniqueSkillEffect(caster))
        {
            ShowUniqueSkillFailMessage(data.GetLocalizedConditionFailMessage());
            return;
        }
        ClearFieldAbsorbOpportunity();
        reinforcementCaster = caster;
        pendingActionTile = null;
        pendingAttackTargetPiece = null;
        ClearHighlights();
        JelluSynthesisRules.FillReinforcementMaterials(caster, boardManager, pieceManager, reinforcementTargets);
        foreach (Piece target in reinforcementTargets) HighlightTile(target.X, target.Y);
        ShowUniqueSkillFailMessage(data.GetLocalizedConditionFailMessage());
    }

    private void CancelReinforcementSelection(bool restoreMovement)
    {
        bool wasSelecting = reinforcementCaster != null;
        reinforcementCaster = null;
        reinforcementTargets.Clear();
        if (!wasSelecting) return;
        ClearHighlights();
        if (restoreMovement && selectedPiece != null && !isBattleEnded && !isActionAnimating)
            ShowMovableTiles(selectedPiece);
    }

    private bool HandleReinforcementSelection(Piece target)
    {
        if (reinforcementCaster == null) return false;
        if (isActionAnimating) return true;
        Piece caster = reinforcementCaster;
        if (target == caster) { CancelReinforcementSelection(true); return true; }
        UniqueSkillData data = GetUniqueSkillData(caster.UniqueSkill);
        if (isBattleEnded || isPlayerDeploymentPhase || !IsCurrentTurnPiece(caster) ||
            !caster.CanUseUniqueSkill() || data == null ||
            (data.oncePerTurn && hasUsedUniqueSkillThisTurn) ||
            !HasEnoughDeathStackForUniqueSkill(caster.Team, data.requiredDeathStack))
        {
            CancelReinforcementSelection(true);
            return true;
        }
        if (!battleSkillManager.CanUseReinforcementTarget(caster, target))
        {
            ShowUniqueSkillFailMessage(data.GetLocalizedConditionFailMessage());
            return true;
        }
        CancelReinforcementSelection(false);
        StartCoroutine(UseSelectedPieceSkillWithTargetRoutine(caster, data, target));
        return true;
    }
}
