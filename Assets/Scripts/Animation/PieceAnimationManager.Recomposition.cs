using UnityEngine;

public partial class PieceAnimationManager
{
    // Same slam timing/feedback assets as battle; no battle callbacks or movement.
    public float RecompositionImpactDelay => Mathf.Max(0,attackDownPreSlamDelay)+Mathf.Max(0,attackSlamDuration);
    public void PlayRecompositionImpact(Vector3 first,Vector3 second)
    {
        PlayAttackImpactFeedback();
        PlayAbsorbBlackPixelBurst(first);
        PlayAbsorbBlackPixelBurst(second);
    }
}
