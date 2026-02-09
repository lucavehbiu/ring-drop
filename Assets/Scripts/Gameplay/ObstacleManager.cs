using UnityEngine;

/// <summary>
/// Spawns asteroid obstacles along the ring's flight path.
/// Asteroids are sphere primitives with random scale distortion for a rocky look.
/// Ring hitting an asteroid = fail. Asteroids start from level 2.
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance { get; private set; }

    private GameObject _asteroidParent;
    private static Shader _urpLit;

    private static readonly Color[] ASTEROID_COLORS = new Color[]
    {
        new Color(0.4f, 0.25f, 0.15f),  // brown
        new Color(0.35f, 0.3f, 0.25f),  // dark tan
        new Color(0.25f, 0.2f, 0.2f),   // dark grey-brown
        new Color(0.5f, 0.35f, 0.2f),   // rust
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Clear existing asteroids and spawn new ones for the current level.
    /// </summary>
    public void SpawnForLevel(LevelData cfg)
    {
        Clear();

        if (cfg.asteroidCount <= 0) return;

        _asteroidParent = new GameObject("Asteroids");

        // Flight path: ring starts at Z=10, stick is at cfg.stickZ (negative)
        // Leave grace zone near start (first 15 units) and approach zone near stick (last 8 units)
        float startZ = 10f - 15f;              // Z = -5
        float endZ = cfg.stickZ + 8f;          // 8 units before stick

        if (startZ <= endZ) return; // path too short

        float pathLength = startZ - endZ;

        if (_urpLit == null)
            _urpLit = Shader.Find("Universal Render Pipeline/Lit");

        for (int i = 0; i < cfg.asteroidCount; i++)
        {
            // Distribute along the path with some randomness
            float t = (float)i / cfg.asteroidCount;
            float z = Mathf.Lerp(startZ, endZ, t) + Random.Range(-2f, 2f);
            z = Mathf.Clamp(z, endZ, startZ);

            // Random horizontal position within the play area (-4.5 to 4.5)
            float x = Random.Range(-4f, 4f);

            // Random height — should be in the ring's flight range
            float y = Random.Range(1.5f, 8f);

            // Random size — small to medium rocks
            float baseSize = Random.Range(0.4f, 1.2f);

            CreateAsteroid(new Vector3(x, y, z), baseSize, i);
        }
    }

    private void CreateAsteroid(Vector3 position, float baseSize, int index)
    {
        var asteroid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        asteroid.name = $"Asteroid_{index}";
        asteroid.tag = "Obstacle";
        asteroid.transform.SetParent(_asteroidParent.transform);
        asteroid.transform.position = position;

        // Distort scale for rocky look (non-uniform)
        float sx = baseSize * Random.Range(0.7f, 1.4f);
        float sy = baseSize * Random.Range(0.6f, 1.2f);
        float sz = baseSize * Random.Range(0.7f, 1.4f);
        asteroid.transform.localScale = new Vector3(sx, sy, sz);

        // Random rotation
        asteroid.transform.rotation = Random.rotation;

        // Material — dark rocky with subtle emission
        if (_urpLit != null)
        {
            var r = asteroid.GetComponent<Renderer>();
            var mat = new Material(_urpLit);
            var color = ASTEROID_COLORS[index % ASTEROID_COLORS.Length];
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.2f);
            mat.SetFloat("_Metallic", 0.1f);
            // Faint emissive edge glow
            mat.SetColor("_EmissionColor", color * 0.15f);
            mat.EnableKeyword("_EMISSION");
            r.material = mat;
        }

        // Add a slow tumble for visual life
        var tumble = asteroid.AddComponent<AsteroidTumble>();
        tumble.rotSpeed = new Vector3(
            Random.Range(-15f, 15f),
            Random.Range(-15f, 15f),
            Random.Range(-15f, 15f)
        );
    }

    public void Clear()
    {
        if (_asteroidParent != null)
        {
            Destroy(_asteroidParent);
            _asteroidParent = null;
        }
    }
}

/// <summary>Simple slow rotation for asteroid visual life.</summary>
public class AsteroidTumble : MonoBehaviour
{
    public Vector3 rotSpeed;

    private void Update()
    {
        transform.Rotate(rotSpeed * Time.deltaTime);
    }
}
