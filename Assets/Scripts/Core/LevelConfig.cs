using UnityEngine;

/// <summary>
/// Generates level configuration. Each level gets harder:
/// stick further away, more wind, tighter tolerance, more ships.
/// </summary>
[System.Serializable]
public struct LevelData
{
    public float stickZ;
    public float stickX;
    public float speed;
    public float gravity;
    public float tolerance;
    public float wind;
    public bool windGusts;
    public int shipCount;
    public float shipSpeed;
}

public static class LevelConfig
{
    public const int MAX_SHIPS = 12;

    public static LevelData Get(int level)
    {
        return new LevelData
        {
            stickZ     = -(25f + level * 3f),
            stickX     = level <= 1 ? 0f : Mathf.Sin(level * 1.4f) * Mathf.Min(level * 0.3f, 2.2f),
            speed      = 8f + level * 0.5f,
            gravity    = -9.8f - (level - 1) * 0.3f,
            tolerance  = Mathf.Max(0.28f, 0.85f - (level - 1) * 0.055f),
            wind       = 0.3f + level * 0.15f,
            windGusts  = level >= 2,
            shipCount  = Mathf.Min(Mathf.FloorToInt(level * 1.2f), MAX_SHIPS),
            shipSpeed  = 3f + level * 0.5f
        };
    }
}
