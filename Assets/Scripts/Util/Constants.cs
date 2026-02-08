using UnityEngine;

/// <summary>
/// Game-wide constants. Tuning values for physics materials, forces, and visuals.
/// With Rigidbody physics, Unity handles gravity/collisions natively.
/// We only define force magnitudes and visual parameters here.
/// </summary>
public static class Constants
{
    // Ring forces (applied via Rigidbody.AddForce)
    public const float FLAP_IMPULSE  = 5.5f;   // upward impulse per tap (flappy style)
    public const float LIFT_FORCE    = 12f;    // (legacy, unused)
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

    // === Post-Processing ===
    public const float PP_BLOOM_INTENSITY       = 1.5f;
    public const float PP_BLOOM_THRESHOLD       = 0.8f;
    public const float PP_BLOOM_SCATTER         = 0.7f;
    public const float PP_VIGNETTE_INTENSITY    = 0.38f;
    public const float PP_MOTION_BLUR_INTENSITY = 0.5f;
    public const float PP_FILM_GRAIN_INTENSITY  = 0.3f;
    public const float PP_CHROM_AB_INTENSITY    = 0.15f;
    public const float PP_LENS_FLARE_INTENSITY  = 0.5f;

    // === Starfield Layers ===
    public const int   STARS_NEAR_COUNT  = 800;
    public const float STARS_NEAR_MIN_D  = 40f;
    public const float STARS_NEAR_MAX_D  = 80f;
    public const float STARS_NEAR_MIN_S  = 0.05f;
    public const float STARS_NEAR_MAX_S  = 0.2f;

    public const int   STARS_MID_COUNT   = 1500;
    public const float STARS_MID_MIN_D   = 80f;
    public const float STARS_MID_MAX_D   = 150f;
    public const float STARS_MID_MIN_S   = 0.15f;
    public const float STARS_MID_MAX_S   = 0.5f;

    public const int   STARS_FAR_COUNT   = 2000;
    public const float STARS_FAR_MIN_D   = 150f;
    public const float STARS_FAR_MAX_D   = 300f;
    public const float STARS_FAR_MIN_S   = 0.3f;
    public const float STARS_FAR_MAX_S   = 0.8f;

    public const float STAR_TWINKLE_FRACTION = 0.3f;

    // Star colors
    public static readonly Color STAR_WHITE      = Color.white;
    public static readonly Color STAR_BLUE_WHITE = new Color(0.7f, 0.85f, 1f);
    public static readonly Color STAR_WARM       = new Color(1f, 0.8f, 0.5f);

    // === Nebula Clouds ===
    public const int   NEBULA_CLOUD_COUNT  = 8;
    public const float NEBULA_CLOUD_MIN_S  = 15f;
    public const float NEBULA_CLOUD_MAX_S  = 30f;
    public const float NEBULA_CLOUD_ALPHA_MIN = 0.08f;
    public const float NEBULA_CLOUD_ALPHA_MAX = 0.2f;
    public const float NEBULA_CLOUD_ROT_SPEED = 1.5f;

    // === Ground Grid ===
    public const int   GRID_TEX_SIZE    = 256;
    public const int   GRID_LINE_WIDTH  = 1;
    public const float GRID_TILE_COUNT  = 20f;
    public const float GROUND_METALLIC  = 0.7f;
    public const float GROUND_SMOOTHNESS = 0.92f;
    public static readonly Color GRID_BASE_COLOR = new Color(0.01f, 0.005f, 0.03f);
    public static readonly Color GRID_LINE_COLOR = new Color(0f, 0.6f, 0.8f, 0.15f);

    // === Lighting (boosted) ===
    public const float DIR_LIGHT_INTENSITY   = 0.7f;
    public const float FILL_LIGHT_INTENSITY  = 0.25f;
    public const float RING_LIGHT_INTENSITY  = 5f;
    public const float RING_LIGHT_RANGE      = 25f;
    public const float RIM_LIGHT_INTENSITY   = 0.4f;
    public const float RIM_LIGHT_RANGE       = 40f;
    public static readonly Color AMBIENT_COLOR = new Color(0.08f, 0.04f, 0.16f);
    public const float FOG_DENSITY = 0.012f;
    public static readonly Color FOG_COLOR = new Color(0.015f, 0.005f, 0.06f);
}
