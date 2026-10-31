using System;
using UnityEngine;
using UnityEngine.UI;

public partial class FieldAbsorbButton
{
    private Action contextAction;
    public void ShowContextAction(Action action, Sprite icon)
    {
        battleUIController = null;
        Show(null, null);
        contextAction = action;
        if (absorbTooltipTrigger != null) absorbTooltipTrigger.enabled = false;
        SetContextIcon(icon);
    }
    public void SetContextIcon(Sprite icon)
    {
        if (absorbIconImage == null || icon == null) return;
        absorbIconImage.sprite = icon; absorbIconImage.type = Image.Type.Simple;
        absorbIconImage.preserveAspect = true;
        absorbIconImage.color = Color.white;
    }
    public void ClearContextAction() { contextAction = null; Hide(); }
}
