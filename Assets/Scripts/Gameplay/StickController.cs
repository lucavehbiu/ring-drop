using UnityEngine;

/// <summary>
/// Stick placement and visual guide bands.
/// The stick is placed at a Z distance each level.
/// Green torus bands show the valid Y range for threading.
/// </summary>
public class StickController : MonoBehaviour
{
    [Header("References — assign in Inspector or create at runtime")]
    [SerializeField] private GameObject stickBody;
    [SerializeField] private GameObject stickBase;
    [SerializeField] private GameObject stickCap;
    [SerializeField] private GameObject lowerBand;
    [SerializeField] private GameObject upperBand;

    private LevelData _cfg;

    // Cache the URP shader once for all material creation
    private static Shader _urpLit;

    private static Shader GetURPLit()
    {
        if (_urpLit == null)
            _urpLit = Shader.Find("Universal Render Pipeline/Lit");
        return _urpLit;
    }

    public void Setup(LevelData cfg)
    {
        _cfg = cfg;
        transform.position = new Vector3(cfg.stickX, 0f, cfg.stickZ);
    }

    /// <summary>
    /// Call this from a scene setup script to procedurally generate the stick.
    /// Returns the fully built stick GameObject.
    /// </summary>
    public static StickController CreateStick()
    {
        GameObject root = new GameObject("Stick");
        var ctrl = root.AddComponent<StickController>();

        // Main cylinder
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "StickBody";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0f, Constants.STICK_HEIGHT / 2f, 0f);
        body.transform.localScale = new Vector3(
            Constants.STICK_RADIUS * 2f,
            Constants.STICK_HEIGHT / 2f,
            Constants.STICK_RADIUS * 2f
        );
        SetEmissiveColor(body, Constants.MAGENTA, 0.55f);
        ctrl.stickBody = body;

        // Base
        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "StickBase";
        baseObj.transform.SetParent(root.transform);
        baseObj.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        baseObj.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);
        SetEmissiveColor(baseObj, Constants.MAGENTA * 0.6f, 0.4f);
        ctrl.stickBase = baseObj;

        // Cap (sphere on top)
        var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "StickCap";
        cap.transform.SetParent(root.transform);
        cap.transform.localPosition = new Vector3(0f, Constants.STICK_HEIGHT, 0f);
        cap.transform.localScale = Vector3.one * 0.26f;
        SetEmissiveColor(cap, Constants.MAGENTA, 0.85f);
        ctrl.stickCap = cap;

        // Guide bands (spheres scaled flat as torus stand-ins — proper torus via mesh later)
        ctrl.lowerBand = CreateGuideBand(root.transform, Constants.VALID_Y_MIN, "LowerBand");
        ctrl.upperBand = CreateGuideBand(root.transform, Constants.VALID_Y_MAX, "UpperBand");

        return ctrl;
    }

    private static GameObject CreateGuideBand(Transform parent, float y, string name)
    {
        var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = name;
        band.transform.SetParent(parent);
        band.transform.localPosition = new Vector3(0f, y, 0f);
        band.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);

        var shader = GetURPLit();
        if (shader == null)
        {
            Debug.LogError("[RingDrop] URP Lit shader not found at band creation.");
            return band;
        }

        var r = band.GetComponent<Renderer>();
        var mat = new Material(shader);
        mat.color = Constants.GREEN;
        mat.SetColor("_EmissionColor", Constants.GREEN * 0.35f);
        mat.EnableKeyword("_EMISSION");
        r.material = mat;

        // Remove collider — bands are visual only
        var col = band.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        return band;
    }

    private static void SetEmissiveColor(GameObject obj, Color color, float emissiveIntensity)
    {
        var shader = GetURPLit();
        if (shader == null)
        {
            Debug.LogError("[RingDrop] URP Lit shader not found for stick material.");
            return;
        }

        var r = obj.GetComponent<Renderer>();
        var mat = new Material(shader);
        mat.color = color;
        mat.SetColor("_EmissionColor", color * emissiveIntensity);
        mat.EnableKeyword("_EMISSION");
        r.material = mat;

        // Remove collider (we do our own collision detection)
        var col = obj.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
    }
}
