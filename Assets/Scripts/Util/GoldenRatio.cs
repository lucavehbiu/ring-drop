/// <summary>
/// Golden ratio (phi = 1.618) design system constants.
/// Every spacing, font size, and layout proportion derives from phi.
/// </summary>
public static class GoldenRatio
{
    public const float PHI = 1.6180339887f;
    public const float PHI_INV = 0.6180339887f; // 1/phi

    // Spacing scale (base: 8)
    public const float XS  = 5f;   // 8/phi
    public const float SM  = 8f;   // base
    public const float MD  = 13f;  // 8*phi
    public const float LG  = 21f;  // 13*phi
    public const float XL  = 34f;  // 21*phi
    public const float XXL = 55f;  // 34*phi
    public const float XXXL = 89f; // 55*phi

    // Typography scale (base: 12)
    public const float FONT_CAPTION  = 7f;
    public const float FONT_SMALL    = 10f;
    public const float FONT_BODY     = 12f;
    public const float FONT_SUBTITLE = 19f;
    public const float FONT_TITLE    = 31f;
    public const float FONT_HERO     = 50f;

    // Layout proportions
    public const float MAJOR = 0.618f;  // 1/phi — dominant section
    public const float MINOR = 0.382f;  // 1 - 1/phi — secondary section

    // Touch zones (screen width fractions)
    public const float ZONE_SIDE   = 0.191f;  // MINOR/2
    public const float ZONE_CENTER = 0.618f;   // MAJOR
}
