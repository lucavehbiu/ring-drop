using UnityEngine;

/// <summary>
/// Generates level configuration. Each level gets harder:
/// stick further away, more wind, tighter tolerance, more ships.
/// Level 1 is gentle and forgiving — difficulty ramps from level 3+.
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
            stickZ     = -(20f + level * 3f),                                          // closer at start (was -25)
            stickX     = level <= 1 ? 0f : Mathf.Sin(level * 1.4f) * Mathf.Min(level * 0.3f, 2.2f),
            speed      = 5.5f + level * 0.4f,                                            // moderate pace
            gravity    = -5.5f - Mathf.Max(0, level - 2) * 0.4f,                       // gentle at first, ramps from lvl 3
            tolerance  = Mathf.Max(0.22f, 0.55f - (level - 1) * 0.03f),                 // level 1 = 0.55, level 12+ = 0.22
            wind       = level <= 1 ? 0.1f : 0.2f + (level - 1) * 0.12f,               // almost no wind level 1
            windGusts  = level >= 3,                                                     // gusts start later (was 2)
            shipCount  = Mathf.Min(Mathf.FloorToInt(level * 1.2f), MAX_SHIPS),
            shipSpeed  = 3f + level * 0.5f
        };
    }
}
