using UnityEngine;

/// <summary>
/// Scene bootstrap — creates all game objects procedurally.
/// Now includes: starfield, fog, better lighting, UI manager.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[RingDrop] Main camera not found.");
            return;
        }

        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpLit == null)
        {
            Debug.LogError("[RingDrop] URP Lit shader not found.");
            return;
        }

        // === CAMERA ===
        mainCam.backgroundColor = Constants.BG;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.farClipPlane = 500f;

        // === FOG ===
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.fogColor = new Color(0.01f, 0.005f, 0.04f);

        // === LIGHTING ===
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.03f, 0.12f);

        // Main directional (moonlight feel)
        var dirLight = new GameObject("DirectionalLight");
        var dl = dirLight.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(0.2f, 0.15f, 0.7f);
        dl.intensity = 0.4f;
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Fill light (warm rim from below-right)
        var fillObj = new GameObject("FillLight");
        var fl = fillObj.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.color = new Color(0.5f, 0.1f, 0.3f);
        fl.intensity = 0.15f;
        fillObj.transform.rotation = Quaternion.Euler(-20f, 120f, 0f);

        // Ring point light
        var ringLightObj = new GameObject("RingLight");
        var rl = ringLightObj.AddComponent<Light>();
        rl.type = LightType.Point;
        rl.color = Constants.CYAN;
        rl.intensity = 3f;
        rl.range = 18f;

        // === INPUT ===
        var inputObj = new GameObject("GameInput");
        inputObj.AddComponent<GameInput>();

        // === RING (torus) ===
        var ringObj = new GameObject("Ring");
        var meshFilter = ringObj.AddComponent<MeshFilter>();
        var meshRenderer = ringObj.AddComponent<MeshRenderer>();
        meshFilter.mesh = TorusMeshGenerator.Create(Constants.RING_RADIUS, Constants.RING_TUBE, 48, 24);
        var ringMat = new Material(urpLit);
        ringMat.color = Constants.CYAN;
        ringMat.SetColor("_EmissionColor", Constants.CYAN * 0.8f);
        ringMat.EnableKeyword("_EMISSION");
        meshRenderer.material = ringMat;
        ringObj.AddComponent<RingController>();

        ringLightObj.transform.SetParent(ringObj.transform);
        ringLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // === STICK ===
        StickController.CreateStick();

        // === CAMERA FOLLOW ===
        mainCam.gameObject.AddComponent<CameraFollow>();

        // === GAME MANAGER ===
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // === UI MANAGER ===
        var uiObj = new GameObject("UIManager");
        uiObj.AddComponent<UIManager>();

        // === GROUND ===
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        var groundMat = new Material(urpLit);
        groundMat.color = new Color(0.015f, 0.008f, 0.04f);
        groundMat.SetFloat("_Smoothness", 0.85f);
        groundMat.SetFloat("_Metallic", 0.3f);
        ground.GetComponent<Renderer>().material = groundMat;

        // === STARFIELD ===
        CreateStarfield(urpUnlit ?? urpLit);

        // === DISTANT NEBULA LIGHTS ===
        CreateNebulaLights();

        Debug.Log("[RingDrop] Scene bootstrapped. Tap/click or press Space to start.");
    }

    private void CreateStarfield(Shader shader)
    {
        var parent = new GameObject("Starfield");
        var mat = new Material(shader);
        mat.color = Color.white;
        if (shader.name.Contains("Lit"))
        {
            mat.SetColor("_EmissionColor", Color.white * 2f);
            mat.EnableKeyword("_EMISSION");
        }

        // Create a combined mesh for all stars (much better performance than individual objects)
        int starCount = 300;
        CombineInstance[] combines = new CombineInstance[starCount];
        var starMesh = CreateQuadMesh();

        for (int i = 0; i < starCount; i++)
        {
            // Scatter in a sphere shell around play area
            Vector3 dir = Random.onUnitSphere;
            float dist = Random.Range(60f, 200f);
            Vector3 pos = dir * dist;

            // Keep stars above ground and in front-ish hemisphere
            if (pos.y < 5f) pos.y = Random.Range(5f, 80f);

            float size = Random.Range(0.1f, 0.5f);
            // Bigger stars when further
            if (dist > 120f) size *= 1.5f;

            Matrix4x4 matrix = Matrix4x4.TRS(pos, Random.rotation, Vector3.one * size);
            combines[i].mesh = starMesh;
            combines[i].transform = matrix;
        }

        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combines, true, true);
        combinedMesh.name = "StarfieldMesh";

        var mf = parent.AddComponent<MeshFilter>();
        mf.mesh = combinedMesh;
        var mr = parent.AddComponent<MeshRenderer>();
        mr.material = mat;
    }

    private Mesh CreateQuadMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.normals = new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
        mesh.uv = new Vector2[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        return mesh;
    }

    private void CreateNebulaLights()
    {
        // Distant colored point lights to simulate nebula glow
        Color[] nebulaColors = {
            new Color(0.4f, 0f, 0.8f),   // purple
            new Color(0f, 0.3f, 0.8f),   // deep blue
            new Color(0.8f, 0f, 0.4f),   // magenta
            new Color(0f, 0.6f, 0.6f),   // teal
        };

        Vector3[] positions = {
            new Vector3(-40f, 30f, -80f),
            new Vector3(50f, 20f, -120f),
            new Vector3(-30f, 50f, -60f),
            new Vector3(60f, 40f, -150f),
        };

        for (int i = 0; i < nebulaColors.Length; i++)
        {
            var obj = new GameObject($"NebulaLight_{i}");
            obj.transform.position = positions[i];
            var light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = nebulaColors[i];
            light.intensity = 0.8f;
            light.range = 80f;
        }
    }
}
