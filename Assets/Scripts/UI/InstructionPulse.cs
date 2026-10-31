using UnityEngine;
public static class InstructionPulse
{
    public static float Alpha(float elapsed, float halfDuration = 1.2f, float min = 0.5f, float max = 1f)
    {
        float t = Mathf.PingPong(elapsed / Mathf.Max(0.05f, halfDuration), 1f);
        return Mathf.Lerp(max, min, Mathf.SmoothStep(0f, 1f, t));
    }
}
