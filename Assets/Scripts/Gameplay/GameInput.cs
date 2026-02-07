using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Unified input handler using the New Input System (1.18.0+).
/// Works with touch (mobile), keyboard (editor), and mouse (editor testing).
/// Touch zones: left 19.1% = steer left, center 61.8% = rise, right 19.1% = steer right.
/// Keyboard: Space/W/Up = rise, A/D/Left/Right = steer.
/// </summary>
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    /// <summary>True when player is holding (should rise)</summary>
    public bool IsHolding { get; private set; }

    /// <summary>-1 = left, 0 = center, 1 = right</summary>
    public float SteerDirection { get; private set; }

    /// <summary>True for one frame when player taps/presses (for threading drop)</summary>
    public bool WasTapped { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        IsHolding = false;
        SteerDirection = 0f;
        WasTapped = false;

        // --- Keyboard (New Input System) ---
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed)
                IsHolding = true;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                SteerDirection = -1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                SteerDirection = 1f;
            if (kb.spaceKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
                WasTapped = true;
        }

        // --- Touch input (overrides keyboard if active) ---
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            IsHolding = true;
            SteerDirection = 0f;

            float normalized = ts.primaryTouch.position.ReadValue().x / Screen.width;

            if (normalized < GoldenRatio.ZONE_SIDE)
                SteerDirection = -1f;
            else if (normalized > 1f - GoldenRatio.ZONE_SIDE)
                SteerDirection = 1f;
        }
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
            WasTapped = true;

        // --- Mouse (editor testing) ---
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            IsHolding = true;
            float normalized = mouse.position.ReadValue().x / Screen.width;

            if (normalized < GoldenRatio.ZONE_SIDE)
                SteerDirection = -1f;
            else if (normalized > 1f - GoldenRatio.ZONE_SIDE)
                SteerDirection = 1f;
        }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            WasTapped = true;
    }
}
