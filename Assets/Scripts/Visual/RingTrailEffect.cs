using UnityEngine;

/// <summary>
/// Glowing cyan speed streaks trailing behind the ring during flight.
/// Disabled during menu/countdown, enabled during Playing.
/// </summary>
public class RingTrailEffect : MonoBehaviour
{
    private ParticleSystem _ps;

    private void Awake()
    {
        _ps = gameObject.AddComponent<ParticleSystem>();
        var main = _ps.main;
        main.startLifetime = 0.5f;
        main.startSize = 0.1f;
        main.startSpeed = 0f;
        main.startColor = new Color(0f, 1f, 1f, 0.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.gravityModifier = 0f;

        var emission = _ps.emission;
        emission.rateOverTime = Constants.TRAIL_RATE;

        var shape = _ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.3f;

        // Particles inherit ring velocity for streaky look
        var inheritVel = _ps.inheritVelocity;
        inheritVel.enabled = true;
        inheritVel.mode = ParticleSystemInheritVelocityMode.Initial;
        inheritVel.curveMultiplier = -0.3f;

        // Trail sub-module for streaky look
        var trails = _ps.trails;
        trails.enabled = true;
        trails.lifetime = 0.3f;
        trails.dieWithParticles = true;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Size over lifetime — shrink
        var sizeOverLifetime = _ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Color over lifetime — fade alpha
        var colorOverLifetime = _ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Constants.CYAN, 0f), new GradientColorKey(Constants.CYAN, 1f) },
            new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        // Renderer — additive blending for glow
        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial();
        renderer.trailMaterial = CreateParticleMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        _ps.Stop();
    }

    public void EnableTrail(bool enable)
    {
        if (enable && !_ps.isPlaying) _ps.Play();
        else if (!enable && _ps.isPlaying) _ps.Stop();
    }

    private Material CreateParticleMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        var mat = new Material(shader);
        mat.SetFloat("_Surface", 1f); // Transparent
        mat.SetFloat("_Blend", 1f);   // Additive
        mat.color = new Color(0f, 1f, 1f, 0.6f);
        mat.SetColor("_EmissionColor", Constants.CYAN * 2f);
        mat.EnableKeyword("_EMISSION");
        return mat;
    }
}
