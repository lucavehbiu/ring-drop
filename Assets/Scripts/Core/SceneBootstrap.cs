using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Scene bootstrap — creates all game objects procedurally.
/// Now uses: Rigidbody physics, Cinemachine cameras, PhysicsMaterials.
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

        // Ring (layer 8) and Stick (layer 9) don't collide with each other.
        // The ring's convex MeshCollider can't represent a hole, so we let
        // the ring pass through the stick entirely. Success is detected by
        // position math when the ring hits the ground.
        Physics.IgnoreLayerCollision(8, 9, true);

        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpLit == null)
        {
            Debug.LogError("[RingDrop] URP Lit shader not found.");
            return;
        }

        // === CAMERA — add Cinemachine Brain for blending ===
        mainCam.backgroundColor = Constants.BG;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.farClipPlane = 500f;

        var brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut, 1.2f
        );

        // === FOG ===
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.fogColor = new Color(0.01f, 0.005f, 0.04f);

        // === LIGHTING ===
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.03f, 0.12f);

        var dirLight = new GameObject("DirectionalLight");
        var dl = dirLight.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(0.2f, 0.15f, 0.7f);
        dl.intensity = 0.4f;
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var fillObj = new GameObject("FillLight");
        var fl = fillObj.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.color = new Color(0.5f, 0.1f, 0.3f);
        fl.intensity = 0.15f;
        fillObj.transform.rotation = Quaternion.Euler(-20f, 120f, 0f);

        var ringLightObj = new GameObject("RingLight");
        var rl = ringLightObj.AddComponent<Light>();
        rl.type = LightType.Point;
        rl.color = Constants.CYAN;
        rl.intensity = 3f;
        rl.range = 18f;

        // === INPUT ===
        var inputObj = new GameObject("GameInput");
        inputObj.AddComponent<GameInput>();

        // === PHYSICS MATERIALS ===
        var ringPhysMat = new PhysicsMaterial("RingPhysMat")
        {
            bounciness = Constants.RING_BOUNCE,
            dynamicFriction = Constants.RING_FRICTION,
            staticFriction = Constants.RING_FRICTION,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Average
        };

        var groundPhysMat = new PhysicsMaterial("GroundPhysMat")
        {
            bounciness = Constants.GROUND_BOUNCE,
            dynamicFriction = Constants.GROUND_FRICTION,
            staticFriction = Constants.GROUND_FRICTION,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Average
        };

        // === RING (torus with Rigidbody) ===
        var ringObj = new GameObject("Ring");
        var meshFilter = ringObj.AddComponent<MeshFilter>();
        var meshRenderer = ringObj.AddComponent<MeshRenderer>();
        var torusMesh = TorusMeshGenerator.Create(Constants.RING_RADIUS, Constants.RING_TUBE, 48, 24);
        meshFilter.mesh = torusMesh;

        var ringMat = new Material(urpLit);
        ringMat.color = Constants.CYAN;
        ringMat.SetColor("_EmissionColor", Constants.CYAN * 0.8f);
        ringMat.EnableKeyword("_EMISSION");
        meshRenderer.material = ringMat;

        // Rigidbody for real physics
        var ringRb = ringObj.AddComponent<Rigidbody>();
        ringRb.mass = Constants.RING_MASS;
        ringRb.linearDamping = 0.3f;
        ringRb.angularDamping = 2f;
        ringRb.interpolation = RigidbodyInterpolation.Interpolate;
        ringRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // MeshCollider convex for physics
        var ringCollider = ringObj.AddComponent<MeshCollider>();
        ringCollider.sharedMesh = torusMesh;
        ringCollider.convex = true;
        ringCollider.material = ringPhysMat;

        ringObj.layer = 8; // "Ring" layer
        var ringCtrl = ringObj.AddComponent<RingController>();

        ringLightObj.transform.SetParent(ringObj.transform);
        ringLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // === STICK ===
        var stickCtrl = StickController.CreateStick();
        SetLayerRecursive(stickCtrl.gameObject, 9); // "Stick" layer

        // === GROUND (with collider and tag) ===
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.tag = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        var groundMat = new Material(urpLit);
        groundMat.color = new Color(0.015f, 0.008f, 0.04f);
        groundMat.SetFloat("_Smoothness", 0.85f);
        groundMat.SetFloat("_Metallic", 0.3f);
        ground.GetComponent<Renderer>().material = groundMat;

        // Ground collider already exists from CreatePrimitive, just set physics material
        var groundCollider = ground.GetComponent<Collider>();
        if (groundCollider != null)
            groundCollider.material = groundPhysMat;

        // === INVISIBLE WALLS to keep ring in bounds ===
        CreateWall("WallLeft", new Vector3(-6f, 5f, -100f), new Vector3(0.1f, 10f, 200f));
        CreateWall("WallRight", new Vector3(6f, 5f, -100f), new Vector3(0.1f, 10f, 200f));

        // === CAMERA FOLLOW + CINEMACHINE ===
        var camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
        camFollow.SetupCinemachine(ringObj.transform, stickCtrl.transform);

        // === GAME MANAGER ===
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // === UI MANAGER ===
        var uiObj = new GameObject("UIManager");
        uiObj.AddComponent<UIManager>();

        // === SFX MANAGER ===
        var sfxObj = new GameObject("SFXManager");
        sfxObj.AddComponent<SFXManager>();

        // === STARFIELD ===
        CreateStarfield(urpUnlit ?? urpLit);

        // === DISTANT NEBULA LIGHTS ===
        CreateNebulaLights();

        Debug.Log("[RingDrop] Scene bootstrapped with Rigidbody physics + Cinemachine. Tap/click or press Space to start.");
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        var wall = new GameObject(name);
        wall.transform.position = position;
        var col = wall.AddComponent<BoxCollider>();
        col.size = scale;
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

        int starCount = 300;
        CombineInstance[] combines = new CombineInstance[starCount];
        var starMesh = CreateQuadMesh();

        for (int i = 0; i < starCount; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            float dist = Random.Range(60f, 200f);
            Vector3 pos = dir * dist;
            if (pos.y < 5f) pos.y = Random.Range(5f, 80f);
            float size = Random.Range(0.1f, 0.5f);
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
        Color[] nebulaColors = {
            new Color(0.4f, 0f, 0.8f),
            new Color(0f, 0.3f, 0.8f),
            new Color(0.8f, 0f, 0.4f),
            new Color(0f, 0.6f, 0.6f),
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
