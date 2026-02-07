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
        // Cache main camera with null safety
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[RingDrop] Main camera not found. Ensure a Camera is tagged 'MainCamera'.");
            return;
        }

        // Verify URP Lit shader is available
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[RingDrop] URP Lit shader not found. Ensure URP package is installed.");
            return;
        }

        // Background
        mainCam.backgroundColor = Constants.BG;
        mainCam.clearFlags = CameraClearFlags.SolidColor;

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

        // Ring — proper procedural torus mesh
        var ringObj = new GameObject("Ring");
        var meshFilter = ringObj.AddComponent<MeshFilter>();
        var meshRenderer = ringObj.AddComponent<MeshRenderer>();
        meshFilter.mesh = TorusMeshGenerator.Create(
            Constants.RING_RADIUS,
            Constants.RING_TUBE,
            48, 24
        );
        var ringMat = new Material(urpLit);
        ringMat.color = Constants.CYAN;
        ringMat.SetColor("_EmissionColor", Constants.CYAN * 0.7f);
        ringMat.EnableKeyword("_EMISSION");
        meshRenderer.material = ringMat;
        var ringCtrl = ringObj.AddComponent<RingController>();

        // Ring light follows ring
        ringLightObj.transform.SetParent(ringObj.transform);
        ringLightObj.transform.localPosition = new Vector3(0f, 0f, 1.5f);

        // Stick
        var stickCtrl = StickController.CreateStick();

        // Camera
        var camFollow = mainCam.gameObject.AddComponent<CameraFollow>();

        // Game Manager
        var gmObj = new GameObject("GameManager");
        var gm = gmObj.AddComponent<GameManager>();

        // Ground plane (subtle)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        var groundMat = new Material(urpLit);
        groundMat.color = new Color(0.01f, 0.01f, 0.03f);
        ground.GetComponent<Renderer>().material = groundMat;

        Debug.Log("[RingDrop] Scene bootstrapped. Tap/click or press Space to start.");
    }
}
