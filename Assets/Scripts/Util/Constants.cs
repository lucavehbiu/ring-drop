using UnityEngine;

/// <summary>
/// Game-wide constants. Single source of truth for all tuning values.
/// </summary>
public static class Constants
{
    // Ring physics
    public const float GRAVITY       = -9.8f;
    public const float LIFT_FORCE    = 22f;
    public const float H_FORCE       = 12f;
    public const float DAMPING       = 0.92f;
    public const float MAX_VY        = 8f;
    public const float MIN_VY        = -12f;
    public const float MAX_VX        = 6f;

    // Ring geometry
    public const float RING_RADIUS   = 0.7f;
    public const float RING_TUBE     = 0.11f;

    // Stick geometry
    public const float STICK_RADIUS  = 0.09f;
    public const float STICK_HEIGHT  = 5f;
    public const float VALID_Y_MIN   = 0.4f;
    public const float VALID_Y_MAX   = STICK_HEIGHT - 0.3f;

    // Gameplay
    public const float SLOWMO_DIST   = 8f;    // distance to stick for bullet-time
    public const float SLOWMO_MIN    = 0.35f;  // min speed multiplier
    public const float SHIP_HIT_DIST = 0.9f;
    public const float SHIP_WARN_DIST = 1.8f;

    // Camera
    public const float CAM_FOLLOW_X  = 0.08f;
    public const float CAM_FOLLOW_Y  = 0.09f;
    public const float CAM_FOLLOW_Z  = 0.07f;
    public const float CAM_OFFSET_Y  = 2.2f;
    public const float CAM_OFFSET_Z  = 7f;
    public const float CAM_ROLL_MULT = 0.4f;  // barrel roll intensity
    public const float BASE_FOV      = 62f;

    // Colors (neon galaxy palette)
    public static readonly Color CYAN    = new Color(0f, 1f, 1f);
    public static readonly Color MAGENTA = new Color(1f, 0f, 1f);
    public static readonly Color RED     = new Color(1f, 0.13f, 0f);
    public static readonly Color GREEN   = new Color(0f, 1f, 0.4f);
    public static readonly Color GOLD    = new Color(1f, 1f, 0f);
    public static readonly Color BG      = new Color(0.008f, 0f, 0.067f);
}
