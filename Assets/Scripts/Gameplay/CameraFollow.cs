using UnityEngine;

/// <summary>
/// Smooth camera that follows the ring with:
/// - Lerp-based position tracking
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

    public void Reset()
    {
        if (_cam != null)
            _cam.fieldOfView = Constants.BASE_FOV;
    }

    private void LateUpdate()
    {
        if (ring == null || GameManager.Instance == null) return;

        var state = GameManager.Instance.State;
        Vector3 ringPos = ring.transform.position;
        float dt = Time.deltaTime;

        if (state == GameManager.GameState.Playing)
        {
            // Target position: behind and above the ring
            float speedMult = ring.SpeedMultiplier;
            float offsetY = Constants.CAM_OFFSET_Y + (1f - speedMult) * 1f;
            float offsetZ = Constants.CAM_OFFSET_Z + (1f - speedMult) * 2f;

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
            float stickX = stick != null ? stick.transform.position.x : 0f;
            float stickZ = stick != null ? stick.transform.position.z : ringPos.z - 15f;

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

            // FOV speed effect
            float targetFOV = Constants.BASE_FOV + ring.SpeedMultiplier * 4f + (1f - speedMult) * -6f;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, 2f * dt);
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
    }
}
