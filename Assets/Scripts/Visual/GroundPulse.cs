using UnityEngine;

/// <summary>
/// Subtle animated emission pulse on the grid floor.
/// Sin wave on emission intensity creates a living, breathing floor.
/// Uses MaterialPropertyBlock for performance.
/// </summary>
public class GroundPulse : MonoBehaviour
{
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private Color _baseEmission;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        _baseEmission = Constants.GRID_LINE_COLOR;
    }

    private void Update()
    {
        if (_renderer == null || _mpb == null) return;

        float t = Mathf.Sin(Time.time * (2f * Mathf.PI / Constants.GROUND_PULSE_PERIOD));
        float intensity = Mathf.Lerp(Constants.GROUND_PULSE_MIN, Constants.GROUND_PULSE_MAX, (t + 1f) * 0.5f);

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_EmissionColor", _baseEmission * intensity);
        _renderer.SetPropertyBlock(_mpb);
    }
}
