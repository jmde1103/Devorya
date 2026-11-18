using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public sealed partial class EventSceneRecompositionCandidateController
{
    private Candidate hoveredCandidate;
    private TooltipViewData candidateTooltipView;
    private RecompositionTooltipUI candidatePopup;
    private UniqueSkillDatabase candidateAbilityDatabase;
    private Locale tooltipLocale;
    private Vector2 tooltipScreenPosition;
    public TooltipViewData CandidateTooltipView => candidateTooltipView;

    private static string CommonText(string key) => new LocalizedString("Tooltip_Common", key).GetLocalizedString();
    public TooltipViewData BuildCandidateTooltip(PieceData data)
    {
        if (data == null) return null;
        var view = TooltipViewData.FromPieceData(data);
        view.useRecompositionTypography = true;
        var typeName = CommonText("tooltip.piece_type." + data.pieceType.ToString().ToLowerInvariant());
        view.category = "<color=#" + ColorUtility.ToHtmlStringRGB(TooltipTextStyle.Palette.pieceType) + ">" + typeName + "</color>";
        // Preview the gained ability only. Candidate traits are never part of the reward.
        view.mainDescription = string.Empty;
        var section = new TooltipSectionData { sectionType = TooltipSectionType.Ability,
            sectionCategory = TooltipLocalization.GetUniqueSkillCategory() };
        if (data.uniqueSkill == UniqueSkillType.None) return view;
        {
            if (candidateAbilityDatabase == null)
                foreach (var status in FindObjectsByType<PieceStatusUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (status.AbilityDatabase != null) { candidateAbilityDatabase = status.AbilityDatabase; break; }
            var skill = candidateAbilityDatabase != null ? candidateAbilityDatabase.GetData(data.uniqueSkill) : null;
            if (skill == null) { Debug.LogWarning("Candidate ability data is unavailable: " + data.uniqueSkill, this); return null; }
            var ability = TooltipViewData.FromUniqueSkillData(skill);
            section.sectionTitle = ability.title;
            section.sectionDescription = ability.mainDescription;
            section.sectionIcon = ability.icon;
            view.sections.Add(section);
            if (ability.sections != null) view.sections.AddRange(ability.sections);
            return view;
        }
    }
    // Retained API for older callers: pointer hover never controls selected information.
    public void UpdateCandidateHover(Camera camera, Vector2 screenPosition) { }
    private void ShowCandidateTooltip(Candidate candidate, Vector2 screenPosition)
    {
        HideCandidateTooltip();
        if (!CanSelect || candidate == null || !candidates.Contains(candidate)) return;
        candidatePopup = FindFirstObjectByType<RecompositionTooltipUI>(FindObjectsInactive.Include);
        if (candidatePopup == null) return;
        var data = BuildCandidateTooltip(candidate.Data);
        if (data == null) return;
        hoveredCandidate = candidate; candidateTooltipView = data;
        tooltipLocale = LocalizationSettings.SelectedLocale; tooltipScreenPosition = screenPosition;
        candidatePopup.Show(data);
    }
    private void RefreshCandidateTooltipLocale()
    {
        if (candidatePopup != null && candidatePopup.IsShowing(candidateTooltipView) &&
            tooltipLocale != LocalizationSettings.SelectedLocale)
            ShowCandidateTooltip(hoveredCandidate, tooltipScreenPosition);
    }
    private void HideCandidateTooltip()
    {
        if (candidatePopup != null) candidatePopup.HideIfShowing(candidateTooltipView);
        candidatePopup = null; candidateTooltipView = null; hoveredCandidate = null; tooltipLocale = null;
    }
}
