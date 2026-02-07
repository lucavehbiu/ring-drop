using UnityEngine;

/// <summary>
/// Stick placement and visual guide bands.
/// Now keeps colliders on the stick body (for potential ring-stick collision).
/// Guide bands are visual only.
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

    public static StickController CreateStick()
    {
        GameObject root = new GameObject("Stick");
        var ctrl = root.AddComponent<StickController>();

        // Main cylinder — keep collider for ring-stick interaction
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "StickBody";
        body.tag = "Stick";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0f, Constants.STICK_HEIGHT / 2f, 0f);
        body.transform.localScale = new Vector3(
            Constants.STICK_RADIUS * 2f,
            Constants.STICK_HEIGHT / 2f,
            Constants.STICK_RADIUS * 2f
        );
        SetEmissiveColor(body, Constants.MAGENTA, 0.55f, keepCollider: true);
        ctrl.stickBody = body;

        // Base — keep collider
        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "StickBase";
        baseObj.tag = "Stick";
        baseObj.transform.SetParent(root.transform);
        baseObj.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        baseObj.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);
        SetEmissiveColor(baseObj, Constants.MAGENTA * 0.6f, 0.4f, keepCollider: true);
        ctrl.stickBase = baseObj;

        // Cap (sphere on top) — keep collider
        var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "StickCap";
        cap.tag = "Stick";
        cap.transform.SetParent(root.transform);
        cap.transform.localPosition = new Vector3(0f, Constants.STICK_HEIGHT, 0f);
        cap.transform.localScale = Vector3.one * 0.26f;
        SetEmissiveColor(cap, Constants.MAGENTA, 0.85f, keepCollider: true);
        ctrl.stickCap = cap;

        // Guide bands — visual only, no colliders
        ctrl.lowerBand = CreateGuideBand(root.transform, Constants.STICK_HEIGHT * 0.1f, "LowerBand");
        ctrl.upperBand = CreateGuideBand(root.transform, Constants.STICK_HEIGHT * 0.8f, "UpperBand");

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

    private static void SetEmissiveColor(GameObject obj, Color color, float emissiveIntensity, bool keepCollider = false)
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

        if (!keepCollider)
        {
            var col = obj.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
    }
}
