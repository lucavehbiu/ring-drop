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

    // Ground landing detection
    private bool _landed;

    // Success animation
    private float _successTime;
    private float _restY;
    private float _wobbleAngle;
    private float _wobbleVelocity;
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
        _landed = false;
        _spinAngle = 0f;

        transform.position = new Vector3(0f, 4.5f, 2f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        _vx = 0f;
        _vy = 0f;
    }

    /// <summary>Called by GameManager when ring lands on stick successfully.</summary>
    public void BeginSuccessAnimation(Vector3 stickPos)
    {
        _successTime = 0f;
        _wobbleAngle = 0f;
        _wobbleVelocity = (Random.value - 0.5f) * 60f;
        transform.localScale = Vector3.one;
        _restY = Constants.RING_RADIUS;
    }

    /// <summary>Called by GameManager when fail state starts.</summary>
    public void BeginFailAnimation()
    {
        _failTime = 0f;
        _failVelocityY = Mathf.Min(_vy, -2f); // always start falling
        _failVelocityX = (Random.value - 0.5f) * 2f;
        _failTumbleX = 0f;
        _failTumbleZ = 0f;
        _failTumbleSpeedX = (Random.value - 0.5f) * 300f;
        _failTumbleSpeedZ = (Random.value - 0.5f) * 200f;
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

        // No slow-mo — constant speed
        SpeedMultiplier = 1f;

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

        // Ring touches the ground — check if stick is inside the ring hole
        float groundLevel = Constants.RING_RADIUS;
        if (!_landed && pos.y <= groundLevel)
        {
            _landed = true;
            pos.y = groundLevel;
            transform.position = pos;

            // Check: is the stick base inside the ring?
            // Horizontal distance from ring center to stick
            float dx = pos.x - _targetX;
            float dz = pos.z - _targetZ;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            // The ring hole fits over the stick if the stick center is within the ring hole
            // Ring inner radius = RING_RADIUS - RING_TUBE
            float holeRadius = Constants.RING_RADIUS - Constants.RING_TUBE;
            var cfg = LevelConfig.Get(gm.Level);

            if (dist < holeRadius * cfg.tolerance * 2f)
            {
                gm.OnSuccess();
            }
            else
            {
                gm.OnFail("missed");
            }
            return;
        }

        // Fell off screen
        if (pos.y < -5f)
            gm.OnFail("fell");

        // Soft ceiling
        if (pos.y > 12f)
            _vy = -2f;
    }

    /// <summary>
    /// Success animation: ring is on the ground around the stick.
    /// Ease to center, wobble, settle.
    /// </summary>
    private void UpdateSuccessPhysics()
    {
        float dt = Time.fixedDeltaTime;
        _successTime += dt;

        Vector3 pos = transform.position;

        // Ease ring to center on stick
        pos.x = Mathf.Lerp(pos.x, _targetX, 6f * dt);
        pos.z = Mathf.Lerp(pos.z, _targetZ, 6f * dt);
        pos.y = _restY;

        transform.position = pos;

        // Wobble: spring-damper settling
        float wobbleStiffness = 120f;
        float wobbleDamping = 8f;
        _wobbleVelocity -= _wobbleAngle * wobbleStiffness * dt;
        _wobbleVelocity *= (1f - wobbleDamping * dt);
        _wobbleAngle += _wobbleVelocity * dt;
        _wobbleAngle = Mathf.Clamp(_wobbleAngle, -15f, 15f);

        // Spin slows down
        float spinSpeed = 60f * Mathf.Max(0f, 1f - _successTime * 0.5f) + 5f;
        _spinAngle += spinSpeed * dt;

        Quaternion targetRot = Quaternion.Euler(
            _wobbleAngle,
            _spinAngle,
            _wobbleAngle * 0.6f
        );
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * dt);
    }

    /// <summary>
    /// Fail animation: ring falls to ground with heavy impact, bounces, settles.
    /// </summary>
    private void UpdateFail()
    {
        float dt = Time.fixedDeltaTime;
        _failTime += dt;

        Vector3 pos = transform.position;

        // Strong gravity — feels heavy
        _failVelocityY += -18f * dt;
        _failVelocityY = Mathf.Max(_failVelocityY, -20f);

        pos.y += _failVelocityY * dt;
        pos.x += _failVelocityX * dt;

        // Keep moving forward a bit
        pos.z -= _forwardSpeed * 0.3f * dt;

        _failVelocityX *= Mathf.Pow(0.96f, dt * 60f);

        // Ground level — ring radius so it sits on top of ground visibly
        float groundY = Constants.RING_RADIUS;
        if (pos.y <= groundY)
        {
            pos.y = groundY;
            _failHitGround = true;
            _failBounceCount++;

            // Decent bounces — first one is visible
            float restitution = 0.4f * Mathf.Pow(0.3f, _failBounceCount - 1);

            if (Mathf.Abs(_failVelocityY) > 0.5f)
            {
                _failVelocityY = -_failVelocityY * restitution;
                _failTumbleSpeedX *= 0.5f;
                _failTumbleSpeedZ *= 0.5f;
            }
            else
            {
                _failVelocityY = 0f;
            }
        }

        transform.position = pos;

        // Tumble while in the air
        if (!_failHitGround)
        {
            _failTumbleX += _failTumbleSpeedX * dt;
            _failTumbleZ += _failTumbleSpeedZ * dt;
        }
        else
        {
            // Settle flat after ground hit
            _failTumbleX = Mathf.Lerp(_failTumbleX, 90f, 3f * dt); // land on side
            _failTumbleZ = Mathf.Lerp(_failTumbleZ, 0f, 3f * dt);
            _failTumbleSpeedX = Mathf.Lerp(_failTumbleSpeedX, 0f, 5f * dt);
            _failTumbleSpeedZ = Mathf.Lerp(_failTumbleSpeedZ, 0f, 5f * dt);
        }

        float spinDecay = Mathf.Max(0f, 1f - _failTime * 0.5f);
        _spinAngle += 180f * spinDecay * dt;

        transform.rotation = Quaternion.Euler(_failTumbleX, _spinAngle, _failTumbleZ);
    }
}
