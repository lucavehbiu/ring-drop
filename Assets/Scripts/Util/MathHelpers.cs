using UnityEngine;

/// <summary>
/// Common math utilities used across the game.
/// </summary>
public static class MathHelpers
{
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float t = Mathf.InverseLerp(fromMin, fromMax, value);
        return Mathf.Lerp(toMin, toMax, t);
    }

    public static float SmoothDamp01(float current, float target, float speed, float dt)
    {
        return current + (target - current) * Mathf.Min(speed * dt, 1f);
    }

    public static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
