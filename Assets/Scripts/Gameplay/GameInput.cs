using UnityEngine;

/// <summary>
/// Unified input handler. Works with touch (mobile) and keyboard (editor/web).
/// Touch zones: left 19.1% = steer left, center 61.8% = rise, right 19.1% = steer right.
/// Keyboard: Space = rise, A/D = steer.
/// </summary>
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    /// <summary>True when player is holding (should rise)</summary>
    public bool IsHolding { get; private set; }

    /// <summary>-1 = left, 0 = center, 1 = right</summary>
    public float SteerDirection { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        IsHolding = false;
        SteerDirection = 0f;

        // Keyboard
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            IsHolding = true;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            SteerDirection = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            SteerDirection = 1f;

        // Touch input (overrides keyboard if active)
        if (Input.touchCount > 0)
        {
            IsHolding = true; // Any touch = rise
            SteerDirection = 0f;

            // Use the first touch to determine steering
            Touch t = Input.GetTouch(0);
            float normalized = t.position.x / Screen.width;

            if (normalized < GoldenRatio.ZONE_SIDE)
                SteerDirection = -1f;
            else if (normalized > 1f - GoldenRatio.ZONE_SIDE)
                SteerDirection = 1f;
            // else: center zone, just rise (steer = 0)
        }

        // Mouse (editor testing)
        if (Input.GetMouseButton(0))
        {
            IsHolding = true;
            float normalized = Input.mousePosition.x / Screen.width;

            if (normalized < GoldenRatio.ZONE_SIDE)
                SteerDirection = -1f;
            else if (normalized > 1f - GoldenRatio.ZONE_SIDE)
                SteerDirection = 1f;
        }
    }
}
