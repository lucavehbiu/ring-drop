using UnityEngine;

/// <summary>
/// Scene bootstrap — creates all game objects procedurally.
/// Attach this to an empty GameObject in the scene.
/// This is the "drop in and play" entry point for the barebones version.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        // Background
        Camera.main.backgroundColor = Constants.BG;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        // Lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.03f, 0.01f, 0.08f);

        var dirLight = new GameObject("DirectionalLight");
        var dl = dirLight.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(0.13f, 0.13f, 0.67f);
        dl.intensity = 0.3f;
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Ring light
        var ringLightObj = new GameObject("RingLight");
        var rl = ringLightObj.AddComponent<Light>();
        rl.type = LightType.Point;
        rl.color = Constants.CYAN;
        rl.intensity = 2.5f;
        rl.range = 14f;

        // Input
        var inputObj = new GameObject("GameInput");
        inputObj.AddComponent<GameInput>();

        // Ring (procedural torus — using a sphere placeholder for now)
        var ringObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ringObj.name = "Ring";
        ringObj.transform.localScale = new Vector3(
            Constants.RING_RADIUS * 2f,
            Constants.RING_TUBE * 2f,
            Constants.RING_RADIUS * 2f
        );
        var ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                   ?? Shader.Find("Standard"));
        ringMat.color = Constants.CYAN;
        ringMat.SetColor("_EmissionColor", Constants.CYAN * 0.7f);
        ringMat.EnableKeyword("_EMISSION");
        ringObj.GetComponent<Renderer>().material = ringMat;
        var ringCollider = ringObj.GetComponent<Collider>();
        if (ringCollider != null) Destroy(ringCollider);
        var ringCtrl = ringObj.AddComponent<RingController>();

        // Ring light follows ring
        ringLightObj.transform.SetParent(ringObj.transform);
        ringLightObj.transform.localPosition = new Vector3(0f, 0f, 1.5f);

        // Stick
        var stickCtrl = StickController.CreateStick();

        // Camera
        var cam = Camera.main;
        var camFollow = cam.gameObject.AddComponent<CameraFollow>();
        // Use reflection-free approach: set via serialized fields would be ideal,
        // but for procedural setup, we access private fields through public setup
        // For now, CameraFollow finds ring/stick via FindObjectOfType

        // Game Manager
        var gmObj = new GameObject("GameManager");
        var gm = gmObj.AddComponent<GameManager>();

        // Ground plane (subtle)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                     ?? Shader.Find("Standard"));
        groundMat.color = new Color(0.01f, 0.01f, 0.03f);
        ground.GetComponent<Renderer>().material = groundMat;

        Debug.Log("[RingDrop] Scene bootstrapped. Tap/click or press Space to start.");
    }
}
