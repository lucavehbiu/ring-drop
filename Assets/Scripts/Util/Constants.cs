using UnityEngine;

/// <summary>
/// Game-wide constants. Tuning values for physics materials, forces, and visuals.
/// With Rigidbody physics, Unity handles gravity/collisions natively.
/// We only define force magnitudes and visual parameters here.
/// </summary>
public static class Constants
{
    // Ring forces (applied via Rigidbody.AddForce)
    public const float LIFT_FORCE    = 12f;    // upward force when holding
    public const float H_FORCE       = 7f;     // horizontal steering force
    public const float MAX_VX        = 4.5f;   // horizontal velocity clamp
    public const float MAX_VY        = 6f;     // upward velocity clamp
    public const float MIN_VY        = -10f;   // downward velocity clamp

    // Grace period — auto-float at start so player can orient
    public const float GRACE_DURATION = 1.0f;
    public const float GRACE_LIFT    = 6f;

    // Ring geometry
    public const float RING_RADIUS   = 0.7f;
    public const float RING_TUBE     = 0.11f;
    public const float RING_MASS     = 1.5f;   // Rigidbody mass

    // Physics materials
    public const float RING_BOUNCE   = 0.35f;  // bounciness on ground impact
    public const float RING_FRICTION = 0.5f;
    public const float GROUND_BOUNCE = 0.2f;
    public const float GROUND_FRICTION = 0.6f;

    // Stick geometry
    public const float STICK_RADIUS  = 0.09f;
    public const float STICK_HEIGHT  = 5f;

    // Camera (Cinemachine overrides most of this, but kept for reference)
    public const float BASE_FOV      = 62f;

    // Visual tilt multipliers — applied as torque
    public const float TILT_PITCH    = 0.3f;
    public const float TILT_ROLL     = 0.8f;

    // Colors (neon galaxy palette)
    public static readonly Color CYAN    = new Color(0f, 1f, 1f);
    public static readonly Color MAGENTA = new Color(1f, 0f, 1f);
    public static readonly Color RED     = new Color(1f, 0.13f, 0f);
    public static readonly Color GREEN   = new Color(0f, 1f, 0.4f);
    public static readonly Color GOLD    = new Color(1f, 1f, 0f);
    public static readonly Color BG      = new Color(0.008f, 0f, 0.067f);
}
