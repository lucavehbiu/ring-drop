using UnityEngine;

/// <summary>
/// Floating tiny debris particles for sense of scale.
/// Large box shape centered on camera, very faint white particles.
/// </summary>
public class AmbientSpaceDust : MonoBehaviour
{
    private ParticleSystem _ps;
    private Transform _camTransform;

    private void Awake()
    {
        _camTransform = Camera.main != null ? Camera.main.transform : null;

        _ps = gameObject.AddComponent<ParticleSystem>();
        var main = _ps.main;
        main.startLifetime = 10f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor = new Color(1f, 1f, 1f, 0.2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Constants.DUST_COUNT;
        main.gravityModifier = 0f;
        main.loop = true;

        var emission = _ps.emission;
        emission.rateOverTime = Constants.DUST_COUNT / 10f; // refill over lifetime

        var shape = _ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(100f, 50f, 100f);

        // Color over lifetime — fade in/out
        var colorOverLifetime = _ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.25f, 0.2f), new GradientAlphaKey(0.25f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        // Noise for random drift
        var noise = _ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.1f;

        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateDustMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        _ps.Play();
    }

    private void LateUpdate()
    {
        // Loosely follow camera so particles are always around the player
        if (_camTransform != null)
            transform.position = _camTransform.position;
    }

    private Material CreateDustMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        var mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 1f); // Additive
        mat.color = new Color(1f, 1f, 1f, 0.2f);
        return mat;
    }
}
