using UnityEngine;

/// <summary>
/// Game-wide constants. Single source of truth for all tuning values.
/// Tuned for smooth, floaty space-feel physics.
/// </summary>
public static class Constants
{
    // Ring physics — floaty, forgiving, smooth
    public const float GRAVITY       = -5.5f;   // gentle space gravity (was -9.8)
    public const float LIFT_FORCE    = 15f;     // responsive but not jerky (was 22)
    public const float H_FORCE       = 8f;      // smooth horizontal steering (was 12)
    public const float DAMPING       = 0.95f;   // less drag, more momentum (was 0.92)
    public const float VY_DAMPING    = 0.985f;  // smooth vertical (was 0.98)
    public const float MAX_VY        = 6f;      // gentler cap (was 8)
    public const float MIN_VY        = -8f;     // can't freefall as hard (was -12)
    public const float MAX_VX        = 4.5f;    // less wild sideways (was 6)

    // Grace period — auto-float at start so player can orient
    public const float GRACE_DURATION = 1.0f;   // seconds of auto-lift after playing starts
    public const float GRACE_LIFT    = 8f;      // gentle upward push during grace

    // Ring geometry
    public const float RING_RADIUS   = 0.7f;
    public const float RING_TUBE     = 0.11f;

    // Stick geometry
    public const float STICK_RADIUS  = 0.09f;
    public const float STICK_HEIGHT  = 5f;
    public const float VALID_Y_MIN   = 0.4f;
    public const float VALID_Y_MAX   = STICK_HEIGHT - 0.3f;

    // Gameplay
    public const float SLOWMO_DIST   = 8f;
    public const float SLOWMO_MIN    = 0.35f;
    public const float SHIP_HIT_DIST = 0.9f;
    public const float SHIP_WARN_DIST = 1.8f;

    // Camera
    public const float CAM_FOLLOW_X  = 0.08f;
    public const float CAM_FOLLOW_Y  = 0.09f;
    public const float CAM_FOLLOW_Z  = 0.07f;
    public const float CAM_OFFSET_Y  = 2.2f;
    public const float CAM_OFFSET_Z  = 7f;
    public const float CAM_ROLL_MULT = 0.25f;   // subtler barrel roll (was 0.4)
    public const float BASE_FOV      = 62f;

    // Visual tilt multipliers — subtle, not nauseating
    public const float TILT_PITCH    = 2.5f;    // pitch from vertical speed (was 5)
    public const float TILT_ROLL     = 8f;      // roll from horizontal speed (was 15)

    // Threading phase — precision drop window
    public const float THREADING_TRIGGER_DIST = 6f;    // Z distance to stick to enter threading
    public const float THREADING_DURATION     = 3.0f;  // seconds to align and drop
    public const float THREADING_DRIFT        = 0.4f;  // slow forward drift during threading
    public const float THREADING_STEER        = 5f;    // precise steering force
    public const float THREADING_HOVER_Y      = STICK_HEIGHT + 1f;  // hover height above stick top
    public const float THREADING_CAM_HEIGHT   = 12f;   // camera height for top-down view
    public const float THREADING_CAM_LERP     = 2.5f;  // camera transition speed
    public const float THREADING_FOV          = 45f;   // tighter FOV for focus

    // Colors (neon galaxy palette)
    public static readonly Color CYAN    = new Color(0f, 1f, 1f);
    public static readonly Color MAGENTA = new Color(1f, 0f, 1f);
    public static readonly Color RED     = new Color(1f, 0.13f, 0f);
    public static readonly Color GREEN   = new Color(0f, 1f, 0.4f);
    public static readonly Color GOLD    = new Color(1f, 1f, 0f);
    public static readonly Color BG      = new Color(0.008f, 0f, 0.067f);
}
