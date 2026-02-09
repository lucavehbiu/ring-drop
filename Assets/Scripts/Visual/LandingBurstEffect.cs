using UnityEngine;

/// <summary>
/// One-shot particle burst when ring hits the ground.
/// Cyan/green for success, red/orange for fail.
/// Call Burst() from RingController.OnCollisionEnter().
/// </summary>
public class LandingBurstEffect : MonoBehaviour
{
    private static LandingBurstEffect _instance;
    public static LandingBurstEffect Instance => _instance;

    private ParticleSystem _ps;
    private ParticleSystem.MainModule _main;

    private void Awake()
    {
        _instance = this;

        _ps = gameObject.AddComponent<ParticleSystem>();
        _main = _ps.main;
        _main.startLifetime = 0.8f;
        _main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        _main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        _main.startColor = Constants.CYAN;
        _main.simulationSpace = ParticleSystemSimulationSpace.World;
        _main.maxParticles = 200;
        _main.gravityModifier = 1f;
        _main.loop = false;
        _main.playOnAwake = false;

        var emission = _ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, Constants.BURST_COUNT) });

        var shape = _ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.5f;

        // Size over lifetime — shrink to 0
        var sizeOverLifetime = _ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Color over lifetime — fade
        var colorOverLifetime = _ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        _ps.Stop();
    }

    /// <summary>
    /// Trigger burst at position. Pass true for success (cyan/green), false for fail (red/orange).
    /// </summary>
    public void Burst(Vector3 position, bool success)
    {
        transform.position = position;

        if (success)
            _main.startColor = new ParticleSystem.MinMaxGradient(Constants.CYAN, Constants.GREEN);
        else
            _main.startColor = new ParticleSystem.MinMaxGradient(Constants.RED, Constants.GOLD);

        _ps.Clear();
        _ps.Play();
    }

    private Material CreateParticleMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        var mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 1f); // Additive
        mat.color = Color.white;
        return mat;
    }
}
