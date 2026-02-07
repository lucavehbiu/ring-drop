using UnityEngine;

/// <summary>
/// Wind simulation. Base oscillating wind + random gusts.
/// Feeds into RingController via direct reference.
/// Kept separate for tuning and visual feedback.
/// </summary>
public class WindSystem : MonoBehaviour
{
    private float _baseStrength;
    private bool _gustsEnabled;
    private float _gustForce;

    public float CurrentWind { get; private set; }

    public void Setup(LevelData cfg)
    {
        _baseStrength = cfg.wind;
        _gustsEnabled = cfg.windGusts;
        _gustForce = 0f;
    }

    private void Update()
    {
        float t = Time.time;

        // Oscillating base wind
        float baseWind = Mathf.Sin(t * 0.7f + GameManager.Instance.Level) * _baseStrength;

        // Random gusts
        if (_gustsEnabled && Random.value < 0.005f)
            _gustForce = (Random.value - 0.5f) * _baseStrength * 3f;
        _gustForce *= 0.97f;

        CurrentWind = baseWind + _gustForce;
    }
}
