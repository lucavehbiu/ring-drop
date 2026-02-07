using UnityEngine;

/// <summary>
/// Ring physics controller. The ring flies forward automatically.
/// Player holds to rise (fights gravity), steers left/right.
/// On success, animates sliding down the stick.
/// Ring is HORIZONTAL (flat like a frisbee) — hole faces up for stick threading.
/// </summary>
public class RingController : MonoBehaviour
{
    [Header("Runtime State")]
    private float _vx;
    private float _vy;
    private float _forwardSpeed;
    private float _gravity;
    private float _wind;
    private bool _windGusts;
    private float _windGust;
    private float _targetZ;
    private float _targetX;
    private float _playTime;

    // Success animation state
    private float _successTime;
    private Vector3 _successStartPos;
    private float _successTargetY;
    private float _spinAngle;

    private LevelData _cfg;

    public float VX => _vx;
    public float VY => _vy;
    public float Progress => 1f - Mathf.Abs(_targetZ - transform.position.z) / Mathf.Abs(_targetZ - 2f);
    public float DistanceToStick => Mathf.Abs(_targetZ - transform.position.z);
    public float SpeedMultiplier { get; private set; } = 1f;

    public void Setup(LevelData cfg)
    {
        _cfg = cfg;
        _forwardSpeed = cfg.speed;
        _gravity = cfg.gravity;
        _wind = cfg.wind;
        _windGusts = cfg.windGusts;
        _targetZ = cfg.stickZ;
        _targetX = cfg.stickX;
        _windGust = 0f;
        _playTime = 0f;
        _successTime = 0f;
        _spinAngle = 0f;

        transform.position = new Vector3(0f, 4.5f, 2f);
        transform.rotation = Quaternion.identity; // flat horizontal
        _vx = 0f;
        _vy = 0f;
    }

    /// <summary>Called by GameManager when success state starts.</summary>
    public void BeginSuccessAnimation(Vector3 stickPos)
    {
        _successTime = 0f;
        _successStartPos = transform.position;
        // Slide down to just above the lower guide band
        _successTargetY = Constants.VALID_Y_MIN + 0.3f;
    }

    private void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.State == GameManager.GameState.Playing)
            UpdatePlaying();
        else if (gm.State == GameManager.GameState.Success)
            UpdateSuccessAnimation();
        else if (gm.State == GameManager.GameState.Countdown)
            UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        // Gentle hover during countdown
        float dt = Time.fixedDeltaTime;
        _spinAngle += 45f * dt;
        transform.rotation = Quaternion.Euler(0f, _spinAngle, 0f);
    }

    private void UpdatePlaying()
    {
        var input = GameInput.Instance;
        if (input == null) return;

        float dt = Time.fixedDeltaTime;
        _playTime += dt;

        // Slow-motion near stick
        float dist = DistanceToStick;
        SpeedMultiplier = dist < Constants.SLOWMO_DIST
            ? Mathf.Max(Constants.SLOWMO_MIN, dist / Constants.SLOWMO_DIST)
            : 1f;

        // Gravity + lift
        _vy += _gravity * dt;
        if (input.IsHolding)
            _vy += Constants.LIFT_FORCE * dt;

        // Grace period
        if (_playTime < Constants.GRACE_DURATION)
        {
            float graceFade = 1f - (_playTime / Constants.GRACE_DURATION);
            _vy += Constants.GRACE_LIFT * graceFade * dt;
        }

        // Horizontal steering
        _vx += input.SteerDirection * Constants.H_FORCE * dt;

        // Wind
        var gm = GameManager.Instance;
        float windBase = Mathf.Sin(Time.fixedTime + gm.Level) * _wind;
        if (_windGusts && Random.value < 0.05f * dt * 60f)
            _windGust = (Random.value - 0.5f) * _wind * 2f;
        _windGust *= 0.97f;
        _vx += (windBase + _windGust) * dt;

        // Damping
        _vx *= Mathf.Pow(Constants.DAMPING, dt * 60f);
        _vy *= Mathf.Pow(Constants.VY_DAMPING, dt * 60f);

        // Clamp
        _vy = Mathf.Clamp(_vy, Constants.MIN_VY, Constants.MAX_VY);
        _vx = Mathf.Clamp(_vx, -Constants.MAX_VX, Constants.MAX_VX);

        // Apply movement
        Vector3 pos = transform.position;
        pos.x += _vx * dt;
        pos.y += _vy * dt;
        pos.z -= _forwardSpeed * SpeedMultiplier * dt;
        pos.x = Mathf.Clamp(pos.x, -5f, 5f);
        transform.position = pos;

        // Visual tilt — HORIZONTAL base (0°), subtle tilt from velocity
        _spinAngle += 90f * SpeedMultiplier * dt; // gentle spin while flying
        Quaternion targetRot = Quaternion.Euler(
            _vy * Constants.TILT_PITCH,    // pitch from vertical speed (base = 0 = flat)
            _spinAngle,                     // spin around Y for visual flair
            _vx * Constants.TILT_ROLL       // roll from horizontal speed
        );
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * dt);

        // Check fail: hit ground
        if (pos.y < -0.3f)
            gm.OnFail("ground");

        // Check fail/success: reached stick Z
        if (pos.z <= _targetZ)
        {
            float hDist = Mathf.Abs(pos.x - _targetX);
            bool inAlignment = hDist < _cfg.tolerance;
            bool inHeight = pos.y >= Constants.VALID_Y_MIN && pos.y <= Constants.VALID_Y_MAX;

            gm.OnRingReachedStick(inAlignment && inHeight);
        }

        // Soft ceiling
        if (pos.y > 12f)
            _vy = -2f;
    }

    private void UpdateSuccessAnimation()
    {
        float dt = Time.fixedDeltaTime;
        _successTime += dt;

        float duration = 1.2f;
        float t = Mathf.Clamp01(_successTime / duration);
        // Smooth ease-out curve
        float ease = 1f - Mathf.Pow(1f - t, 3f);

        // Slide to stick position and down
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(_successStartPos.x, _targetX, ease);
        pos.z = Mathf.Lerp(_successStartPos.z, _targetZ, ease * 0.5f); // subtle Z adjust
        pos.y = Mathf.Lerp(_successStartPos.y, _successTargetY, ease);
        transform.position = pos;

        // Spin and flatten to perfectly horizontal
        _spinAngle += 360f * dt; // celebratory fast spin
        Quaternion targetRot = Quaternion.Euler(0f, _spinAngle, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * dt);
    }
}
