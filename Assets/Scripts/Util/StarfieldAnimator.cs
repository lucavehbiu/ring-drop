using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Twinkling star animation. Attached to the Starfield parent.
/// All registered stars twinkle (filtering happens in SceneBootstrap).
/// </summary>
public class StarfieldAnimator : MonoBehaviour
{
    private struct TwinkleStar
    {
        public Renderer renderer;
        public MaterialPropertyBlock block;
        public Color baseEmission;
        public float speed;
        public float phase;
        public float minScale;
    }

    private List<TwinkleStar> _stars = new List<TwinkleStar>();
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public void RegisterStar(Renderer rend, Color baseEmission)
    {
        _stars.Add(new TwinkleStar
        {
            renderer = rend,
            block = new MaterialPropertyBlock(),
            baseEmission = baseEmission,
            speed = Random.Range(0.5f, 2.5f),
            phase = Random.Range(0f, Mathf.PI * 2f),
            minScale = Random.Range(0.1f, 0.5f)
        });
    }

    private void Start()
    {
        StartCoroutine(TwinkleLoop());
    }

    private IEnumerator TwinkleLoop()
    {
        while (true)
        {
            float t = Time.time;
            for (int i = 0; i < _stars.Count; i++)
            {
                var star = _stars[i];
                if (star.renderer == null) continue;

                float wave = (Mathf.Sin(t * star.speed + star.phase) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(star.minScale, 1f, wave);

                star.block.SetColor(EmissionColorId, star.baseEmission * intensity);
                star.renderer.SetPropertyBlock(star.block);
            }
            // Update every 3 frames to save perf
            yield return null;
            yield return null;
            yield return null;
        }
    }
}
