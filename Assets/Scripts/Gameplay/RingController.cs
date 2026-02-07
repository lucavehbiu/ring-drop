using UnityEngine;

/// <summary>
/// Ring physics controller. The ring flies forward automatically.
/// Player holds to rise (fights gravity), steers left/right.
/// Wind pushes horizontally. Slow-motion near the stick.
/// Grace period at start auto-floats so player can orient.
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
    private float _playTime;  // time since Playing state started

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

        transform.position = new Vector3(0f, 4.5f, 2f);  // higher start (was 3.2)
        _vx = 0f;
        _vy = 0f;
    }

    private void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameManager.GameState.Playing) return;

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

        // Grace period: auto-float so the ring doesn't nosedive before player reacts
        if (_playTime < Constants.GRACE_DURATION)
        {
            float graceFade = 1f - (_playTime / Constants.GRACE_DURATION);
            _vy += Constants.GRACE_LIFT * graceFade * dt;
        }

        // Horizontal steering
        _vx += input.SteerDirection * Constants.H_FORCE * dt;

        // Wind (use fixedTime for consistency with FixedUpdate)
        float windBase = Mathf.Sin(Time.fixedTime + gm.Level) * _wind;
        if (_windGusts && Random.value < 0.05f * dt * 60f)
            _windGust = (Random.value - 0.5f) * _wind * 2f;
        _windGust *= 0.97f;
        _vx += (windBase + _windGust) * dt;

        // Damping — smooth exponential decay
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

        // Visual tilt — subtle, using Slerp for smoothness
        Quaternion targetRot = Quaternion.Euler(
            90f + _vy * Constants.TILT_PITCH,
            0f,
            _vx * Constants.TILT_ROLL
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
}
