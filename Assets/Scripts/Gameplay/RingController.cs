using UnityEngine;

/// <summary>
/// Ring physics controller.
/// Phase 1 (Playing): Ring flies forward, player holds to rise, steers left/right.
/// Phase 2 (Threading): Ring hovers near stick, player aligns and taps to drop.
/// Phase 3 (Success): Real physics: freefall down the stick, bounce off base, wobble, settle.
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

    // Threading phase
    private float _threadingTime;

    // Success animation — physics-based fall
    private float _successTime;
    private float _fallVelocity;        // Y velocity during stick fall
    private float _restY;               // where the ring settles (base of stick)
    private bool _aligned;              // has the ring centered over the stick?
    private float _wobbleAngle;         // tilt wobble during fall
    private float _wobbleVelocity;      // angular velocity of wobble
    private int _bounceCount;
    private float _spinAngle;

    // Fail animation — tumble and fall
    private float _failTime;
    private float _failVelocityY;
    private float _failVelocityX;
    private float _failTumbleX;
    private float _failTumbleZ;
    private float _failTumbleSpeedX;
    private float _failTumbleSpeedZ;
    private bool _failHitGround;
    private int _failBounceCount;

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
        _threadingTime = 0f;
        _spinAngle = 0f;
        _bounceCount = 0;

        transform.position = new Vector3(0f, 4.5f, 2f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        _vx = 0f;
        _vy = 0f;
    }

    /// <summary>Called by GameManager when threading state starts.</summary>
    public void BeginThreading()
    {
        _threadingTime = 0f;
        // Kill vertical velocity — ring should hover smoothly
        _vy = 0f;

        // Random horizontal offset so player always has alignment work
        int level = GameManager.Instance != null ? GameManager.Instance.Level : 1;
        float offset = 0.2f + (level - 1) * 0.05f;
        float sign = Random.value > 0.5f ? 1f : -1f;
        Vector3 pos = transform.position;
        pos.x += sign * offset;
        transform.position = pos;
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
        transform.localScale = Vector3.one;

        // Rest position: just above the stick base (base top ~0.12 + ring tube 0.11)
        _restY = 0.22f;
    }

    /// <summary>Called by GameManager when fail state starts.</summary>
    public void BeginFailAnimation()
    {
        _failTime = 0f;
        _failVelocityY = _vy < 0f ? _vy : -1f; // seed from current velocity or small downward
        _failVelocityX = (Random.value - 0.5f) * 3f; // random sideways kick
        _failTumbleX = 0f;
        _failTumbleZ = 0f;
        _failTumbleSpeedX = (Random.value - 0.5f) * 400f; // chaotic tumble
        _failTumbleSpeedZ = (Random.value - 0.5f) * 300f;
        _failHitGround = false;
        _failBounceCount = 0;
        transform.localScale = Vector3.one;
    }

    private void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.State == GameManager.GameState.Playing)
            UpdatePlaying();
        else if (gm.State == GameManager.GameState.Threading)
            UpdateThreading();
        else if (gm.State == GameManager.GameState.Success)
            UpdateSuccessPhysics();
        else if (gm.State == GameManager.GameState.Fail)
            UpdateFail();
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

        // Check: close enough to stick → enter threading phase
        float distZ = pos.z - _targetZ;
        if (distZ <= Constants.THREADING_TRIGGER_DIST && distZ > -1f)
        {
            gm.EnterThreading();
        }

        // Soft ceiling
        if (pos.y > 12f)
            _vy = -2f;
    }

    /// <summary>
    /// Threading phase: ring hovers near stick, player steers to align.
    /// Drop is handled by GameManager checking input in Update().
    /// Wind drift pushes ring off-center for challenge.
    /// </summary>
    private void UpdateThreading()
    {
        var input = GameInput.Instance;
        if (input == null) return;

        float dt = Time.fixedDeltaTime;
        _threadingTime += dt;

        Vector3 pos = transform.position;

        // Slow forward drift (ring slowly passes if player doesn't drop)
        pos.z -= Constants.THREADING_DRIFT * dt;

        // Precise horizontal steering
        if (input.SteerDirection != 0f)
            pos.x += input.SteerDirection * Constants.THREADING_STEER * dt;

        // Wind drift — sinusoidal push that makes alignment a real challenge
        int level = GameManager.Instance != null ? GameManager.Instance.Level : 1;
        float windStrength = Constants.THREADING_WIND_BASE + (level - 1) * Constants.THREADING_WIND_PER_LVL;
        float windDrift = Mathf.Sin(_threadingTime * Constants.THREADING_WIND_FREQ * Mathf.PI * 2f) * windStrength;
        pos.x += windDrift * dt;

        pos.x = Mathf.Clamp(pos.x, -5f, 5f);

        // Ease Y toward hover height (above stick top)
        float hoverTarget = Constants.THREADING_HOVER_Y;
        pos.y = Mathf.Lerp(pos.y, hoverTarget, 3f * dt);

        transform.position = pos;

        // Scale up for top-down visibility
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * 1.3f, 4f * dt);

        // Gentle flat rotation — keep ring horizontal for visibility, slow spin
        _spinAngle += 30f * dt;
        Quaternion targetRot = Quaternion.Euler(0f, _spinAngle, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * dt);

        // Speed multiplier stays low during threading (for camera/effects)
        SpeedMultiplier = 0.3f;
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

    /// <summary>
    /// Fail animation: ring tumbles and falls to ground, bounces, settles.
    /// </summary>
    private void UpdateFail()
    {
        float dt = Time.fixedDeltaTime;
        _failTime += dt;

        Vector3 pos = transform.position;

        // Gravity
        _failVelocityY += -12f * dt;
        _failVelocityY = Mathf.Max(_failVelocityY, -15f); // terminal velocity

        pos.y += _failVelocityY * dt;
        pos.x += _failVelocityX * dt;

        // Dampen horizontal drift
        _failVelocityX *= Mathf.Pow(0.98f, dt * 60f);

        // Ground bounce
        float groundY = Constants.RING_TUBE;
        if (pos.y <= groundY)
        {
            pos.y = groundY;
            _failHitGround = true;
            _failBounceCount++;

            float restitution = 0.3f * Mathf.Pow(0.25f, _failBounceCount - 1);

            if (Mathf.Abs(_failVelocityY) > 0.4f)
            {
                _failVelocityY = -_failVelocityY * restitution;
                // Dampen tumble on impact
                _failTumbleSpeedX *= 0.4f;
                _failTumbleSpeedZ *= 0.4f;
            }
            else
            {
                _failVelocityY = 0f;
            }
        }

        transform.position = pos;

        // Tumble rotation while falling
        if (!_failHitGround)
        {
            // Chaotic tumble in the air
            _failTumbleX += _failTumbleSpeedX * dt;
            _failTumbleZ += _failTumbleSpeedZ * dt;
        }
        else
        {
            // After hitting ground, ease toward flat
            _failTumbleX = Mathf.Lerp(_failTumbleX, 0f, 3f * dt);
            _failTumbleZ = Mathf.Lerp(_failTumbleZ, 0f, 3f * dt);
            _failTumbleSpeedX = Mathf.Lerp(_failTumbleSpeedX, 0f, 4f * dt);
            _failTumbleSpeedZ = Mathf.Lerp(_failTumbleSpeedZ, 0f, 4f * dt);
        }

        // Spin slows over ~2 seconds
        float spinDecay = Mathf.Max(0f, 1f - _failTime * 0.5f);
        _spinAngle += 180f * spinDecay * dt;

        transform.rotation = Quaternion.Euler(_failTumbleX, _spinAngle, _failTumbleZ);
    }
}
