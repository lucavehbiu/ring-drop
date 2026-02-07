using UnityEngine;

/// <summary>
/// Smooth camera that follows the ring with:
/// - Lerp-based position tracking (Playing)
/// - Top-down precision view (Threading)
/// - FOV speed effect (wider when fast, tighter in slow-mo)
/// - Subtle barrel roll based on horizontal velocity
/// - Look-ahead toward the stick as progress increases
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private RingController ring;
    [SerializeField] private StickController stick;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
            _cam = Camera.main;
    }

    private void Start()
    {
        // Auto-wire references if not assigned in Inspector (procedural bootstrap)
        if (ring == null) ring = FindAnyObjectByType<RingController>();
        if (stick == null) stick = FindAnyObjectByType<StickController>();
    }

    public void Reset()
    {
        if (_cam != null)
            _cam.fieldOfView = Constants.BASE_FOV;
    }

    private void LateUpdate()
    {
        if (ring == null || stick == null || GameManager.Instance == null) return;

        var state = GameManager.Instance.State;
        Vector3 ringPos = ring.transform.position;
        float dt = Time.deltaTime;

        if (state == GameManager.GameState.Playing || state == GameManager.GameState.Threading)
        {
            UpdatePlayingCamera(ringPos, dt);
        }
        else if (state == GameManager.GameState.Menu)
        {
            // Gentle float in menu
            float t = Time.time;
            transform.position = new Vector3(
                Mathf.Sin(t * 0.3f) * 0.5f,
                5.5f + Mathf.Sin(t * 1f) * 0.3f,
                10f
            );
            transform.LookAt(new Vector3(0f, 3f, -10f));
        }
        else if (state == GameManager.GameState.Countdown)
        {
            // Ease toward play position
            Vector3 targetPos = new Vector3(0f, ringPos.y + 2.5f, ringPos.z + 9f);
            transform.position = Vector3.Lerp(transform.position, targetPos, 2f * dt);
            transform.LookAt(ringPos + Vector3.forward * -5f);
        }
        else if (state == GameManager.GameState.Fail)
        {
            UpdatePlayingCamera(ringPos, dt);
        }
        else if (state == GameManager.GameState.Success)
        {
            // During success, follow ring falling down stick — side angle
            UpdateSuccessCamera(ringPos, dt);
        }
    }

    private void UpdatePlayingCamera(Vector3 ringPos, float dt)
    {
        // Target position: behind and above the ring
        float offsetY = Constants.CAM_OFFSET_Y;
        float offsetZ = Constants.CAM_OFFSET_Z;

        Vector3 targetPos = new Vector3(
            ringPos.x * 0.4f,
            ringPos.y + offsetY,
            ringPos.z + offsetZ
        );

        // Smooth follow
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetPos.x, Constants.CAM_FOLLOW_X * dt * 60f);
        pos.y = Mathf.Lerp(pos.y, targetPos.y, Constants.CAM_FOLLOW_Y * dt * 60f);
        pos.z = Mathf.Lerp(pos.z, targetPos.z, Constants.CAM_FOLLOW_Z * dt * 60f);
        transform.position = pos;

        // Look-ahead: blend between ring and stick as progress increases
        float progress = Mathf.Clamp01(ring.Progress);
        float stickX = stick.transform.position.x;
        float stickZ = stick.transform.position.z;

        float lookZ = Mathf.Max(ringPos.z - 12f, stickZ);
        Vector3 lookTarget = new Vector3(
            ringPos.x * 0.25f + stickX * progress * 0.3f,
            ringPos.y * 0.4f + 1.6f,
            lookZ
        );
        transform.LookAt(lookTarget);

        // Barrel roll
        Vector3 euler = transform.eulerAngles;
        float targetRoll = -ring.VX * Constants.CAM_ROLL_MULT;
        // Smooth the roll
        float currentRoll = transform.localEulerAngles.z;
        if (currentRoll > 180f) currentRoll -= 360f;
        float smoothRoll = Mathf.Lerp(currentRoll, targetRoll, 5f * dt);
        transform.Rotate(0f, 0f, smoothRoll - currentRoll, Space.Self);

        // Keep FOV constant
        _cam.fieldOfView = Constants.BASE_FOV;
    }

    /// <summary>
    /// Success camera: follows the ring falling down the stick from a nice angle.
    /// </summary>
    private void UpdateSuccessCamera(Vector3 ringPos, float dt)
    {
        Vector3 stickPos = stick.transform.position;

        // Orbit slightly around the stick for drama
        float angle = Time.time * 0.5f;
        Vector3 targetPos = new Vector3(
            stickPos.x + Mathf.Sin(angle) * 3f,
            ringPos.y + 2f,
            stickPos.z + Mathf.Cos(angle) * 3f + 2f
        );

        transform.position = Vector3.Lerp(transform.position, targetPos, 3f * dt);
        Vector3 lookAt = new Vector3(stickPos.x, ringPos.y * 0.5f + 0.5f, stickPos.z);
        transform.LookAt(lookAt);

        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 50f, 2f * dt);
    }

}
