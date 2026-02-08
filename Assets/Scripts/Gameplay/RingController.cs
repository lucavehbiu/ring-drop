using UnityEngine;

/// <summary>
/// Ring physics controller using Unity's Rigidbody.
/// Player holds to rise, releases to let gravity pull ring down.
/// Steers left/right. Success = ring lands on ground with stick inside hole.
/// Unity handles gravity, collisions, bouncing via Rigidbody + PhysicsMaterials.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RingController : MonoBehaviour
{
    private Rigidbody _rb;
    private float _playTime;
    private bool _landed;
    private float _targetZ;
    private float _targetX;
    private float _forwardSpeed;
    private float _wind;
    private bool _windGusts;
    private float _windGust;
    private LevelData _cfg;

    // Success settle animation (post-physics)
    private bool _settling;
    private float _settleTime;

    public float VX => _rb != null ? _rb.linearVelocity.x : 0f;
    public float VY => _rb != null ? _rb.linearVelocity.y : 0f;
    public float Progress => 1f - Mathf.Abs(_targetZ - transform.position.z) / Mathf.Abs(_targetZ - 2f);
    public float DistanceToStick => Mathf.Abs(_targetZ - transform.position.z);

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Setup(LevelData cfg)
    {
        _cfg = cfg;
        _forwardSpeed = cfg.speed;
        _wind = cfg.wind;
        _windGusts = cfg.windGusts;
        _targetZ = cfg.stickZ;
        _targetX = cfg.stickX;
        _windGust = 0f;
        _playTime = 0f;
        _landed = false;
        _settling = false;
        _settleTime = 0f;

        transform.position = new Vector3(0f, 4.5f, 2f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }
    }

    /// <summary>Called by GameManager on success — let physics settle the ring.</summary>
    public void BeginSuccessAnimation(Vector3 stickPos)
    {
        _settling = true;
        _settleTime = 0f;
        // Ring is already on the ground from physics — just let it be
    }

    /// <summary>Called by GameManager on fail — ring is already falling via physics.</summary>
    public void BeginFailAnimation()
    {
        // Add some random tumble torque for drama
        if (_rb != null)
        {
            _rb.AddTorque(
                Random.Range(-5f, 5f),
                Random.Range(-3f, 3f),
                Random.Range(-5f, 5f),
                ForceMode.Impulse
            );
            // Slight sideways kick
            _rb.AddForce(
                Random.Range(-2f, 2f),
                0f,
                Random.Range(-1f, 1f),
                ForceMode.Impulse
            );
        }
    }

    /// <summary>Freeze the ring (menu, countdown idle).</summary>
    public void Freeze()
    {
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>Unfreeze for gameplay.</summary>
    public void Unfreeze()
    {
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }
    }

    private void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || _rb == null) return;

        if (gm.State == GameManager.GameState.Playing)
            UpdatePlaying();
        else if (gm.State == GameManager.GameState.Success)
            UpdateSuccess();
        // Fail state: physics runs on its own, no custom code needed
    }

    private void UpdatePlaying()
    {
        var input = GameInput.Instance;
        if (input == null) return;

        float dt = Time.fixedDeltaTime;
        _playTime += dt;

        // Lift force when holding
        if (input.IsHolding)
            _rb.AddForce(Vector3.up * Constants.LIFT_FORCE, ForceMode.Acceleration);

        // Grace period auto-lift
        if (_playTime < Constants.GRACE_DURATION)
        {
            float graceFade = 1f - (_playTime / Constants.GRACE_DURATION);
            _rb.AddForce(Vector3.up * Constants.GRACE_LIFT * graceFade, ForceMode.Acceleration);
        }

        // Horizontal steering
        float steer = input.SteerDirection * Constants.H_FORCE;
        _rb.AddForce(Vector3.right * steer, ForceMode.Acceleration);

        // Wind
        var gm = GameManager.Instance;
        float windBase = Mathf.Sin(Time.fixedTime + gm.Level) * _wind;
        if (_windGusts && Random.value < 0.05f * dt * 60f)
            _windGust = (Random.value - 0.5f) * _wind * 2f;
        _windGust *= 0.97f;
        _rb.AddForce(Vector3.right * (windBase + _windGust), ForceMode.Acceleration);

        // Forward movement — constant speed, ring never stops
        Vector3 vel = _rb.linearVelocity;
        vel.z = -_forwardSpeed;

        // If ring flew past the stick and hits the ground, OnCollisionEnter handles it.
        // If ring flew way past (no ground hit), fail out.
        if (transform.position.z < _targetZ - 15f)
        {
            gm.OnFail("flew past");
        }

        // Clamp velocities
        vel.x = Mathf.Clamp(vel.x, -Constants.MAX_VX, Constants.MAX_VX);
        vel.y = Mathf.Clamp(vel.y, Constants.MIN_VY, Constants.MAX_VY);
        _rb.linearVelocity = vel;

        // Clamp horizontal position
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x) > 5f)
        {
            pos.x = Mathf.Clamp(pos.x, -5f, 5f);
            transform.position = pos;
            vel.x = 0f;
            _rb.linearVelocity = vel;
        }

        // Visual tilt via torque
        float pitchTorque = vel.y * Constants.TILT_PITCH;
        float rollTorque = -vel.x * Constants.TILT_ROLL;
        _rb.AddTorque(pitchTorque, 0f, rollTorque, ForceMode.Acceleration);

        // Dampen angular velocity so ring doesn't spin wildly
        _rb.angularVelocity *= 0.95f;

        // Soft ceiling
        if (pos.y > 12f)
        {
            vel.y = Mathf.Min(vel.y, -2f);
            _rb.linearVelocity = vel;
        }

        // Fell off screen
        if (pos.y < -5f)
            gm.OnFail("fell");
    }

    private void UpdateSuccess()
    {
        if (!_settling) return;

        float dt = Time.fixedDeltaTime;
        _settleTime += dt;

        // After a moment, gently pull ring to center on stick
        if (_settleTime > 0.3f && _rb != null)
        {
            Vector3 pos = transform.position;
            Vector3 target = new Vector3(_targetX, pos.y, _targetZ);
            Vector3 toCenter = (target - pos);
            toCenter.y = 0f;

            // Gentle spring toward stick center
            _rb.AddForce(toCenter * 8f, ForceMode.Acceleration);

            // Heavy damping to settle
            _rb.linearVelocity *= 0.92f;
            _rb.angularVelocity *= 0.9f;
        }
    }

    /// <summary>
    /// Called by Unity physics when the ring collides with something.
    /// We use this to detect ground landing.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (_landed) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameManager.GameState.Playing) return;

        // Check if we hit the ground
        if (collision.gameObject.CompareTag("Ground"))
        {
            _landed = true;

            // Check: is the stick base inside the ring hole?
            Vector3 pos = transform.position;
            float dx = pos.x - _targetX;
            float dz = pos.z - _targetZ;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

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
        }
    }
}
