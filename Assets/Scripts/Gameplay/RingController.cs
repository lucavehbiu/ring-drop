using UnityEngine;

/// <summary>
/// Ring physics controller. The ring flies forward automatically.
/// Player holds to rise (fights gravity), steers left/right.
/// On success, real physics: freefall down the stick, bounce off base, wobble, settle.
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

    // Success animation — physics-based fall
    private float _successTime;
    private float _fallVelocity;        // Y velocity during stick fall
    private float _restY;               // where the ring settles (base of stick)
    private bool _aligned;              // has the ring centered over the stick?
    private float _wobbleAngle;         // tilt wobble during fall
    private float _wobbleVelocity;      // angular velocity of wobble
    private int _bounceCount;
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
        _bounceCount = 0;

        transform.position = new Vector3(0f, 4.5f, 2f);
        transform.rotation = Quaternion.identity;
        _vx = 0f;
        _vy = 0f;
    }

    /// <summary>Called by GameManager when success state starts.</summary>
    public void BeginSuccessAnimation(Vector3 stickPos)
    {
        _successTime = 0f;
        _fallVelocity = 0f;
        _aligned = false;
        _wobbleAngle = 0f;
        _wobbleVelocity = 0f;
        _bounceCount = 0;

        // Rest position: just above the stick base (base top ~0.12 + ring tube 0.11)
        _restY = 0.22f;
    }

    private void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.State == GameManager.GameState.Playing)
            UpdatePlaying();
        else if (gm.State == GameManager.GameState.Success)
            UpdateSuccessPhysics();
        else if (gm.State == GameManager.GameState.Countdown)
            UpdateCountdown();
    }

    private void UpdateCountdown()
    {
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

        // Visual tilt
        _spinAngle += 90f * SpeedMultiplier * dt;
        Quaternion targetRot = Quaternion.Euler(
            _vy * Constants.TILT_PITCH,
            _spinAngle,
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

    /// <summary>
    /// Physics-based success animation.
    /// Phase 1: Quick snap to center over stick (0.2s)
    /// Phase 2: Freefall with real gravity down the pole
    /// Phase 3: Bounce off the base with restitution
    /// Phase 4: Wobble dampens, ring settles
    /// </summary>
    private void UpdateSuccessPhysics()
    {
        float dt = Time.fixedDeltaTime;
        _successTime += dt;

        Vector3 pos = transform.position;

        // --- Phase 1: Snap to stick center (quick ease, ~0.2s) ---
        float alignSpeed = 12f;
        pos.x = Mathf.Lerp(pos.x, _targetX, alignSpeed * dt);
        pos.z = Mathf.Lerp(pos.z, _targetZ, alignSpeed * dt);

        // Mark aligned once close enough
        if (!_aligned && Mathf.Abs(pos.x - _targetX) < 0.05f)
            _aligned = true;

        // --- Phase 2 & 3: Gravity fall + bounce ---
        // Real gravity acceleration (slightly stronger for satisfying fall)
        float fallGravity = -14f;
        _fallVelocity += fallGravity * dt;

        // Terminal velocity clamp
        _fallVelocity = Mathf.Max(_fallVelocity, -12f);

        pos.y += _fallVelocity * dt;

        // Bounce when hitting the base
        if (pos.y <= _restY)
        {
            pos.y = _restY;
            _bounceCount++;

            // Restitution: each bounce loses energy
            // First bounce is biggest, then rapidly diminishes
            float restitution = 0.45f * Mathf.Pow(0.35f, _bounceCount - 1);

            if (Mathf.Abs(_fallVelocity) > 0.3f)
            {
                _fallVelocity = -_fallVelocity * restitution;

                // Kick the wobble on each bounce
                _wobbleVelocity += (Random.value - 0.5f) * 80f * restitution;
            }
            else
            {
                // Settled — kill all motion
                _fallVelocity = 0f;
            }
        }

        transform.position = pos;

        // --- Wobble: tilt oscillation during fall ---
        // Spring-damper for wobble angle
        float wobbleStiffness = 120f;
        float wobbleDamping = 8f;

        _wobbleVelocity -= _wobbleAngle * wobbleStiffness * dt; // spring restoring force
        _wobbleVelocity *= (1f - wobbleDamping * dt);           // damping
        _wobbleAngle += _wobbleVelocity * dt;

        // Clamp wobble so it doesn't go crazy
        _wobbleAngle = Mathf.Clamp(_wobbleAngle, -15f, 15f);

        // If falling fast, add some natural wobble from air resistance
        if (_fallVelocity < -2f && _bounceCount == 0)
        {
            _wobbleVelocity += Mathf.Sin(_successTime * 25f) * 15f * dt;
        }

        // --- Spin: slows down as ring settles ---
        float spinSpeed;
        if (_bounceCount == 0)
            spinSpeed = 200f; // fast spin during freefall
        else if (_fallVelocity != 0f)
            spinSpeed = 120f / _bounceCount; // slowing with each bounce
        else
            spinSpeed = 5f; // gentle drift when settled

        _spinAngle += spinSpeed * dt;

        // --- Final rotation: flat base + wobble + spin ---
        Quaternion targetRot = Quaternion.Euler(
            _wobbleAngle,           // wobble on X axis
            _spinAngle,             // spin around pole
            _wobbleAngle * 0.6f     // coupled wobble on Z for natural feel
        );
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * dt);
    }
}
