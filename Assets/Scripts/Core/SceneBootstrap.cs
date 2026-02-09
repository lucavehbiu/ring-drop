using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

/// <summary>
/// Scene bootstrap — creates all game objects procedurally.
/// Now uses: Rigidbody physics, Cinemachine cameras, PhysicsMaterials,
/// cinematic post-processing, multi-layer starfield, nebula clouds, grid floor.
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

        // === CAMERA — add Cinemachine Brain for blending ===
        mainCam.backgroundColor = Constants.BG;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.farClipPlane = 500f;

        var brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut, 1.2f
        );

        // === TAA — biggest clarity upgrade ===
        var urpCamData = mainCam.GetUniversalAdditionalCameraData();
        urpCamData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
        urpCamData.antialiasingQuality = AntialiasingQuality.High;

        // === FOG (boosted) ===
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = Constants.FOG_DENSITY;
        RenderSettings.fogColor = Constants.FOG_COLOR;

        // === LIGHTING (boosted) ===
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Constants.AMBIENT_COLOR;

        var dirLight = new GameObject("DirectionalLight");
        var dl = dirLight.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(0.2f, 0.15f, 0.7f);
        dl.intensity = Constants.DIR_LIGHT_INTENSITY;
        dl.shadows = LightShadows.Soft;
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var dlData = dl.GetComponent<UniversalAdditionalLightData>();
        if (dlData != null) dlData.softShadowQuality = SoftShadowQuality.High;

        var fillObj = new GameObject("FillLight");
        var fl = fillObj.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.color = new Color(0.5f, 0.1f, 0.3f);
        fl.intensity = Constants.FILL_LIGHT_INTENSITY;
        fillObj.transform.rotation = Quaternion.Euler(-20f, 120f, 0f);

        var ringLightObj = new GameObject("RingLight");
        var rl = ringLightObj.AddComponent<Light>();
        rl.type = LightType.Point;
        rl.color = Constants.CYAN;
        rl.intensity = Constants.RING_LIGHT_INTENSITY;
        rl.range = Constants.RING_LIGHT_RANGE;

        // Rim light from behind for depth
        var rimLightObj = new GameObject("RimLight");
        var rim = rimLightObj.AddComponent<Light>();
        rim.type = LightType.Point;
        rim.color = new Color(1f, 0.7f, 0.4f);
        rim.intensity = Constants.RIM_LIGHT_INTENSITY;
        rim.range = Constants.RIM_LIGHT_RANGE;
        rimLightObj.transform.position = new Vector3(0f, 3f, 15f);

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
        ringMat.SetFloat("_Metallic", Constants.RING_METALLIC);
        ringMat.SetFloat("_Smoothness", Constants.RING_SMOOTHNESS);
        ringMat.SetColor("_EmissionColor", Constants.CYAN * 1.5f);
        ringMat.EnableKeyword("_EMISSION");
        meshRenderer.material = ringMat;

        // Rigidbody for real physics
        var ringRb = ringObj.AddComponent<Rigidbody>();
        ringRb.mass = Constants.RING_MASS;
        ringRb.linearDamping = 0.3f;
        ringRb.angularDamping = 2f;
        ringRb.interpolation = RigidbodyInterpolation.Interpolate;
        ringRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        CreateRingCompoundCollider(ringObj, Constants.RING_RADIUS, Constants.RING_TUBE, ringPhysMat);

        var ringCtrl = ringObj.AddComponent<RingController>();

        ringLightObj.transform.SetParent(ringObj.transform);
        ringLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // Ring trail particles (child of ring)
        var trailObj = new GameObject("RingTrail");
        trailObj.transform.SetParent(ringObj.transform, false);
        trailObj.transform.localPosition = Vector3.zero;
        trailObj.AddComponent<RingTrailEffect>();

        // === STICK ===
        var stickCtrl = StickController.CreateStick();

        // === GROUND (grid floor) ===
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.tag = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(20f, 1f, 20f);

        var gridTex = TextureGenerator.CreateGridTexture(
            Constants.GRID_TEX_SIZE, Constants.GRID_LINE_WIDTH,
            Constants.GRID_BASE_COLOR, Constants.GRID_LINE_COLOR
        );
        var groundMat = new Material(urpLit);
        groundMat.color = Constants.GRID_BASE_COLOR;
        groundMat.mainTexture = gridTex;
        groundMat.mainTextureScale = new Vector2(Constants.GRID_TILE_COUNT, Constants.GRID_TILE_COUNT);
        groundMat.SetFloat("_Smoothness", Constants.GROUND_SMOOTHNESS);
        groundMat.SetFloat("_Metallic", Constants.GROUND_METALLIC);
        groundMat.SetColor("_EmissionColor", Constants.GRID_LINE_COLOR * 0.3f);
        groundMat.EnableKeyword("_EMISSION");
        ground.GetComponent<Renderer>().material = groundMat;

        var groundCollider = ground.GetComponent<Collider>();
        if (groundCollider != null)
            groundCollider.material = groundPhysMat;

        // Ground pulse effect
        ground.AddComponent<GroundPulse>();

        // === LANDING BURST PARTICLES ===
        var burstObj = new GameObject("LandingBurst");
        burstObj.AddComponent<LandingBurstEffect>();

        // === AMBIENT SPACE DUST ===
        var dustObj = new GameObject("SpaceDust");
        dustObj.AddComponent<AmbientSpaceDust>();

        // === REFLECTION PROBE — ground reflects ring/stars/nebula ===
        var probeObj = new GameObject("ReflectionProbe");
        probeObj.transform.position = new Vector3(0f, 0.5f, -50f);
        var probe = probeObj.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
        probe.size = new Vector3(200f, 30f, 200f);
        probe.resolution = 256;
        probe.hdr = true;
        probe.nearClipPlane = 0.3f;
        probe.farClipPlane = 300f;

        // === INVISIBLE WALLS to keep ring in bounds ===
        CreateWall("WallLeft", new Vector3(-6f, 5f, -100f), new Vector3(0.1f, 10f, 200f));
        CreateWall("WallRight", new Vector3(6f, 5f, -100f), new Vector3(0.1f, 10f, 200f));

        // === CAMERA FOLLOW + CINEMACHINE ===
        var camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
        camFollow.SetupCinemachine(ringObj.transform, stickCtrl.transform);

        // === GAME MANAGER ===
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // === OBSTACLE MANAGER ===
        var obstObj = new GameObject("ObstacleManager");
        obstObj.AddComponent<ObstacleManager>();

        // === UI MANAGER ===
        var uiObj = new GameObject("UIManager");
        uiObj.AddComponent<UIManager>();

        // === SFX MANAGER ===
        var sfxObj = new GameObject("SFXManager");
        sfxObj.AddComponent<SFXManager>();

        // === STARFIELD (3 layers, 4000+ stars) ===
        CreateStarfield(urpLit);

        // === DISTANT NEBULA LIGHTS ===
        CreateNebulaLights();

        // === NEBULA CLOUDS ===
        CreateNebulaClouds(urpLit);

        // === SOLAR SYSTEM ===
        CreateSolarSystem(urpLit);

        // === POST-PROCESSING ===
        ConfigurePostProcessing();

        Debug.Log("[RingDrop] Scene bootstrapped with cinematic visuals, Rigidbody physics + Cinemachine.");
    }

    /// <summary>
    /// Creates a ring-shaped compound collider from BoxColliders arranged in a circle.
    /// </summary>
    private static void CreateRingCompoundCollider(GameObject ringObj, float radius, float tube, PhysicsMaterial mat)
    {
        int segments = 12;
        float angleStep = 360f / segments;
        float arcLength = 2f * Mathf.PI * radius / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            var child = new GameObject($"RingCol_{i}");
            child.transform.SetParent(ringObj.transform, false);
            child.transform.localPosition = new Vector3(x, 0f, z);
            child.transform.localRotation = Quaternion.Euler(0f, -i * angleStep, 0f);

            var box = child.AddComponent<BoxCollider>();
            box.size = new Vector3(arcLength, tube * 2f, tube * 2f);
            box.material = mat;
        }
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        var wall = new GameObject(name);
        wall.transform.position = position;
        var col = wall.AddComponent<BoxCollider>();
        col.size = scale;
    }

    // ========== POST-PROCESSING ==========

    private void ConfigurePostProcessing()
    {
        // Find existing Volume or create one
        var volume = FindAnyObjectByType<Volume>();
        if (volume == null)
        {
            var volObj = new GameObject("PostProcessVolume");
            volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        var profile = volume.profile;

        // Bloom
        if (!profile.TryGet<Bloom>(out var bloom))
            bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(Constants.PP_BLOOM_INTENSITY);
        bloom.threshold.Override(Constants.PP_BLOOM_THRESHOLD);
        bloom.scatter.Override(Constants.PP_BLOOM_SCATTER);
        bloom.highQualityFiltering.Override(true);

        // Tonemapping → ACES
        if (!profile.TryGet<Tonemapping>(out var tonemap))
            tonemap = profile.Add<Tonemapping>();
        tonemap.active = true;
        tonemap.mode.Override(TonemappingMode.ACES);

        // Vignette
        if (!profile.TryGet<Vignette>(out var vignette))
            vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(Constants.PP_VIGNETTE_INTENSITY);

        // Motion Blur
        if (!profile.TryGet<MotionBlur>(out var motionBlur))
            motionBlur = profile.Add<MotionBlur>();
        motionBlur.active = true;
        motionBlur.intensity.Override(Constants.PP_MOTION_BLUR_INTENSITY);
        motionBlur.quality.Override(MotionBlurQuality.High);

        // Film Grain
        if (!profile.TryGet<FilmGrain>(out var filmGrain))
            filmGrain = profile.Add<FilmGrain>();
        filmGrain.active = true;
        filmGrain.intensity.Override(Constants.PP_FILM_GRAIN_INTENSITY);

        // Chromatic Aberration
        if (!profile.TryGet<ChromaticAberration>(out var chromAb))
            chromAb = profile.Add<ChromaticAberration>();
        chromAb.active = true;
        chromAb.intensity.Override(Constants.PP_CHROM_AB_INTENSITY);

        // Screen Space Lens Flare
        if (!profile.TryGet<ScreenSpaceLensFlare>(out var lensFlare))
            lensFlare = profile.Add<ScreenSpaceLensFlare>();
        lensFlare.active = true;
        lensFlare.intensity.Override(Constants.PP_LENS_FLARE_INTENSITY);

        // === Depth of Field (Bokeh) — cinematic focus ===
        if (!profile.TryGet<DepthOfField>(out var dof))
            dof = profile.Add<DepthOfField>();
        dof.active = true;
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(Constants.DOF_FOCUS_DIST);
        dof.aperture.Override(Constants.DOF_APERTURE);
        dof.focalLength.Override(Constants.DOF_FOCAL_LENGTH);
        dof.bladeCurvature.Override(1f);
        dof.bladeCount.Override(6);

        // === Color Adjustments — punchier, slightly cool ===
        if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
            colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.postExposure.Override(Constants.CG_EXPOSURE);
        colorAdj.contrast.Override(Constants.CG_CONTRAST);
        colorAdj.saturation.Override(Constants.CG_SATURATION);
        colorAdj.colorFilter.Override(new Color(0.9f, 0.95f, 1f));

        // === Split Toning — cyan shadows, warm gold highlights ===
        if (!profile.TryGet<SplitToning>(out var split))
            split = profile.Add<SplitToning>();
        split.active = true;
        split.shadows.Override(new Color(0.2f, 0.6f, 0.8f));
        split.highlights.Override(new Color(1f, 0.85f, 0.6f));
        split.balance.Override(-20f);

        // === Lift Gamma Gain — crush blacks, boost highlights ===
        if (!profile.TryGet<LiftGammaGain>(out var lgg))
            lgg = profile.Add<LiftGammaGain>();
        lgg.active = true;
        lgg.lift.Override(new Vector4(0.95f, 0.95f, 1.05f, -0.05f));
        lgg.gain.Override(new Vector4(1.05f, 1.02f, 0.98f, 0.1f));
    }

    // ========== STARFIELD (3 layers, ~4300 stars) ==========
    // Static stars use CombineMeshes for performance.
    // ~30% get individual GameObjects for twinkling animation.

    private void CreateStarfield(Shader shader)
    {
        var parent = new GameObject("Starfield");
        var animator = parent.AddComponent<StarfieldAnimator>();

        CreateStarLayer(parent.transform, animator, shader, "NearStars",
            Constants.STARS_NEAR_COUNT, Constants.STARS_NEAR_MIN_D, Constants.STARS_NEAR_MAX_D,
            Constants.STARS_NEAR_MIN_S, Constants.STARS_NEAR_MAX_S);

        CreateStarLayer(parent.transform, animator, shader, "MidStars",
            Constants.STARS_MID_COUNT, Constants.STARS_MID_MIN_D, Constants.STARS_MID_MAX_D,
            Constants.STARS_MID_MIN_S, Constants.STARS_MID_MAX_S);

        CreateStarLayer(parent.transform, animator, shader, "FarStars",
            Constants.STARS_FAR_COUNT, Constants.STARS_FAR_MIN_D, Constants.STARS_FAR_MAX_D,
            Constants.STARS_FAR_MIN_S, Constants.STARS_FAR_MAX_S);
    }

    private struct StarData
    {
        public Vector3 pos;
        public Quaternion rot;
        public float size;
        public Color color;
        public Color emission;
    }

    private StarData GenerateStar(float minDist, float maxDist, float minSize, float maxSize)
    {
        Vector3 dir = Random.onUnitSphere;
        float dist = Random.Range(minDist, maxDist);
        Vector3 pos = dir * dist;
        if (pos.y < 3f) pos.y = Random.Range(3f, maxDist * 0.6f);

        float roll = Random.value;
        Color starColor;
        if (roll < 0.8f) starColor = Constants.STAR_WHITE;
        else if (roll < 0.95f) starColor = Constants.STAR_BLUE_WHITE;
        else starColor = Constants.STAR_WARM;

        return new StarData
        {
            pos = pos,
            rot = Random.rotation,
            size = Random.Range(minSize, maxSize),
            color = starColor,
            emission = starColor * Random.Range(1.5f, 3f)
        };
    }

    private void CreateStarLayer(Transform parent, StarfieldAnimator animator,
        Shader shader, string name, int count, float minDist, float maxDist,
        float minSize, float maxSize)
    {
        var quadMesh = CreateQuadMesh();
        int twinkleCount = Mathf.RoundToInt(count * Constants.STAR_TWINKLE_FRACTION);
        int staticCount = count - twinkleCount;

        // --- Static stars: combined mesh (single draw call) ---
        var staticMat = new Material(shader);
        staticMat.color = Color.white;
        staticMat.SetColor("_EmissionColor", Color.white * 2f);
        staticMat.EnableKeyword("_EMISSION");

        var combines = new CombineInstance[staticCount];
        for (int i = 0; i < staticCount; i++)
        {
            var s = GenerateStar(minDist, maxDist, minSize, maxSize);
            combines[i].mesh = quadMesh;
            combines[i].transform = Matrix4x4.TRS(s.pos, s.rot, Vector3.one * s.size);
        }

        var staticObj = new GameObject($"{name}_Static");
        staticObj.transform.SetParent(parent, false);
        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combines, true, true);
        combinedMesh.name = $"{name}_StaticMesh";
        var smf = staticObj.AddComponent<MeshFilter>();
        smf.mesh = combinedMesh;
        var smr = staticObj.AddComponent<MeshRenderer>();
        smr.material = staticMat;
        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smr.receiveShadows = false;

        // --- Twinkling stars: individual GameObjects ---
        for (int i = 0; i < twinkleCount; i++)
        {
            var s = GenerateStar(minDist, maxDist, minSize, maxSize);

            var starObj = new GameObject($"{name}_Twinkle_{i}");
            starObj.transform.SetParent(parent, false);
            starObj.transform.position = s.pos;
            starObj.transform.rotation = s.rot;
            starObj.transform.localScale = Vector3.one * s.size;

            var mf = starObj.AddComponent<MeshFilter>();
            mf.sharedMesh = quadMesh;

            var mat = new Material(shader);
            mat.color = s.color;
            mat.SetColor("_EmissionColor", s.emission);
            mat.EnableKeyword("_EMISSION");

            var mr = starObj.AddComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            animator.RegisterStar(mr, s.emission);
        }
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

    // ========== NEBULA LIGHTS ==========

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

    // ========== NEBULA CLOUDS ==========

    private void CreateNebulaClouds(Shader shader)
    {
        Color[] cloudColors = {
            new Color(0.4f, 0f, 0.8f),   // purple
            new Color(0f, 0.3f, 0.8f),   // blue
            new Color(0.8f, 0f, 0.4f),   // pink
            new Color(0f, 0.6f, 0.6f),   // cyan
            new Color(0.5f, 0.1f, 0.7f), // violet
            new Color(0.1f, 0.4f, 0.9f), // azure
            new Color(0.7f, 0.15f, 0.5f),// magenta
            new Color(0.2f, 0.5f, 0.7f), // teal
        };

        var parent = new GameObject("NebulaClouds");
        var quadMesh = CreateQuadMesh();

        for (int i = 0; i < Constants.NEBULA_CLOUD_COUNT; i++)
        {
            float angle = (i / (float)Constants.NEBULA_CLOUD_COUNT) * Mathf.PI * 2f;
            float dist = Random.Range(60f, 140f);
            float height = Random.Range(15f, 55f);
            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * dist,
                height,
                Mathf.Sin(angle) * dist - 80f
            );

            float size = Random.Range(Constants.NEBULA_CLOUD_MIN_S, Constants.NEBULA_CLOUD_MAX_S);
            float alpha = Random.Range(Constants.NEBULA_CLOUD_ALPHA_MIN, Constants.NEBULA_CLOUD_ALPHA_MAX);

            var cloudObj = new GameObject($"NebulaCloud_{i}");
            cloudObj.transform.SetParent(parent.transform, false);
            cloudObj.transform.position = pos;
            cloudObj.transform.rotation = Random.rotation;
            cloudObj.transform.localScale = Vector3.one * size;

            var mf = cloudObj.AddComponent<MeshFilter>();
            mf.sharedMesh = quadMesh;

            var mat = new Material(shader);
            // Set to transparent surface
            mat.SetFloat("_Surface", 1f); // 0=Opaque, 1=Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha blend
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            Color cloudColor = cloudColors[i % cloudColors.Length];
            mat.color = new Color(cloudColor.r, cloudColor.g, cloudColor.b, alpha);
            mat.SetColor("_EmissionColor", cloudColor * 0.4f);
            mat.EnableKeyword("_EMISSION");

            var mr = cloudObj.AddComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Slow rotation
            cloudObj.AddComponent<NebulaCloudRotator>();
        }
    }

    // ========== SOLAR SYSTEM ==========

    private void CreateSolarSystem(Shader shader)
    {
        var root = new GameObject("SolarSystem");
        root.transform.position = new Vector3(-60f, 80f, -120f);

        // Sun — large glowing sphere with point light
        var sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sun.name = "Sun";
        sun.transform.SetParent(root.transform, false);
        sun.transform.localPosition = Vector3.zero;
        sun.transform.localScale = Vector3.one * 12f;
        Object.Destroy(sun.GetComponent<Collider>());

        var sunMat = new Material(shader);
        var sunColor = new Color(1f, 0.85f, 0.3f);
        sunMat.color = sunColor;
        sunMat.SetColor("_EmissionColor", sunColor * 4f);
        sunMat.EnableKeyword("_EMISSION");
        sun.GetComponent<Renderer>().material = sunMat;

        var sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Point;
        sunLight.color = sunColor;
        sunLight.intensity = 2f;
        sunLight.range = 200f;

        // Planet definitions: name, orbit radius, size, color, emission multiplier, orbit speed
        var planets = new[]
        {
            new { name = "Mercury", orbit = 10f,  size = 0.6f,  col = new Color(0.6f, 0.5f, 0.4f),  em = 0.3f, speed = 12f },
            new { name = "Venus",   orbit = 15f,  size = 1.0f,  col = new Color(0.9f, 0.7f, 0.3f),  em = 0.4f, speed = 8f },
            new { name = "Earth",   orbit = 22f,  size = 1.1f,  col = new Color(0.2f, 0.5f, 0.9f),  em = 0.5f, speed = 6f },
            new { name = "Mars",    orbit = 30f,  size = 0.8f,  col = new Color(0.8f, 0.3f, 0.1f),  em = 0.35f, speed = 4.5f },
            new { name = "Jupiter", orbit = 45f,  size = 3.5f,  col = new Color(0.8f, 0.6f, 0.4f),  em = 0.3f, speed = 2f },
            new { name = "Saturn",  orbit = 60f,  size = 3.0f,  col = new Color(0.9f, 0.8f, 0.5f),  em = 0.3f, speed = 1.5f },
            new { name = "Uranus",  orbit = 75f,  size = 2.0f,  col = new Color(0.5f, 0.8f, 0.9f),  em = 0.4f, speed = 1f },
            new { name = "Neptune", orbit = 90f,  size = 1.9f,  col = new Color(0.2f, 0.3f, 0.9f),  em = 0.5f, speed = 0.7f },
        };

        foreach (var p in planets)
        {
            // Orbit pivot — rotates around the sun
            var pivot = new GameObject($"{p.name}_Orbit");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = Vector3.zero;
            // Start each planet at a random angle
            pivot.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var orbiter = pivot.AddComponent<PlanetOrbiter>();
            orbiter.orbitSpeed = p.speed;

            // Planet sphere
            var planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = p.name;
            planet.transform.SetParent(pivot.transform, false);
            planet.transform.localPosition = new Vector3(p.orbit, 0f, 0f);
            planet.transform.localScale = Vector3.one * p.size;
            Object.Destroy(planet.GetComponent<Collider>());

            var mat = new Material(shader);
            mat.color = p.col;
            mat.SetColor("_EmissionColor", p.col * p.em);
            mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Smoothness", 0.6f);
            planet.GetComponent<Renderer>().material = mat;

            // Saturn gets a ring
            if (p.name == "Saturn")
            {
                CreatePlanetRing(planet.transform, shader, p.size, p.col);
            }

            // Earth gets a tiny moon
            if (p.name == "Earth")
            {
                var moonPivot = new GameObject("Moon_Orbit");
                moonPivot.transform.SetParent(planet.transform, false);
                moonPivot.transform.localPosition = Vector3.zero;
                var moonOrbiter = moonPivot.AddComponent<PlanetOrbiter>();
                moonOrbiter.orbitSpeed = 25f;

                var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moon.name = "Moon";
                moon.transform.SetParent(moonPivot.transform, false);
                moon.transform.localPosition = new Vector3(2f, 0f, 0f);
                moon.transform.localScale = Vector3.one * 0.3f;
                Object.Destroy(moon.GetComponent<Collider>());

                var moonMat = new Material(shader);
                moonMat.color = new Color(0.7f, 0.7f, 0.7f);
                moonMat.SetColor("_EmissionColor", new Color(0.7f, 0.7f, 0.7f) * 0.3f);
                moonMat.EnableKeyword("_EMISSION");
                moon.GetComponent<Renderer>().material = moonMat;
            }
        }
    }

    private void CreatePlanetRing(Transform planet, Shader shader, float planetSize, Color color)
    {
        // Ring = flattened torus approximated by a cylinder
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "SaturnRing";
        ring.transform.SetParent(planet, false);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(
            planetSize * 1.8f,  // wide
            0.02f,              // very thin
            planetSize * 1.8f   // wide
        );
        Object.Destroy(ring.GetComponent<Collider>());

        var mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        var ringColor = new Color(color.r * 0.8f, color.g * 0.7f, color.b * 0.5f, 0.5f);
        mat.color = ringColor;
        mat.SetColor("_EmissionColor", ringColor * 0.3f);
        mat.EnableKeyword("_EMISSION");
        ring.GetComponent<Renderer>().material = mat;
    }
}

/// <summary>
/// Slowly rotates a nebula cloud quad for subtle motion.
/// </summary>
public class NebulaCloudRotator : MonoBehaviour
{
    private float _speed;

    private void Start()
    {
        _speed = Random.Range(0.5f, Constants.NEBULA_CLOUD_ROT_SPEED);
        if (Random.value > 0.5f) _speed = -_speed;
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward, _speed * Time.deltaTime, Space.Self);
    }
}

/// <summary>
/// Slowly orbits a planet around its parent (the sun).
/// </summary>
public class PlanetOrbiter : MonoBehaviour
{
    public float orbitSpeed = 5f;

    private void Update()
    {
        transform.Rotate(Vector3.up, orbitSpeed * Time.deltaTime, Space.Self);
    }
}
