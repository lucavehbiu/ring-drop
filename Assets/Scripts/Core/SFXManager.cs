using UnityEngine;

/// <summary>
/// Minimal procedural SFX singleton.
/// Generates audio clips at runtime — no asset files needed.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private AudioSource _source;
    private AudioClip _failClip;
    private AudioClip _successClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.volume = 0.5f;

        _failClip = GenerateFailClip();
        _successClip = GenerateSuccessClip();
    }

    public void PlayFail()
    {
        _source.PlayOneShot(_failClip, 0.6f);
    }

    public void PlaySuccess()
    {
        _source.PlayOneShot(_successClip, 0.5f);
    }

    /// <summary>Descending frequency sweep 400→80Hz over 0.5s with noise.</summary>
    private AudioClip GenerateFailClip()
    {
        int sampleRate = 44100;
        float duration = 0.5f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;

            // Descending sweep 400 → 80 Hz
            float freq = Mathf.Lerp(400f, 80f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            float tone = Mathf.Sin(phase) * 0.6f;

            // Add noise for clatter feel
            float noise = (Random.value * 2f - 1f) * 0.25f * (1f - progress);

            // Envelope: quick attack, steady decay
            float envelope = Mathf.Clamp01(1f - progress * 0.8f);
            envelope *= Mathf.Min(t * 50f, 1f); // 20ms attack

            samples[i] = (tone + noise) * envelope;
        }

        var clip = AudioClip.Create("FailSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>Ascending sweep 300→800Hz over 0.3s with bell envelope.</summary>
    private AudioClip GenerateSuccessClip()
    {
        int sampleRate = 44100;
        float duration = 0.3f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;

            // Ascending sweep 300 → 800 Hz
            float freq = Mathf.Lerp(300f, 800f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            float tone = Mathf.Sin(phase) * 0.5f;

            // Add harmonic for brightness
            float harmonic = Mathf.Sin(phase * 2f) * 0.15f;

            // Bell envelope: fast attack, exponential decay
            float envelope = Mathf.Exp(-progress * 3f);
            envelope *= Mathf.Min(t * 100f, 1f); // 10ms attack

            samples[i] = (tone + harmonic) * envelope;
        }

        var clip = AudioClip.Create("SuccessSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
