using UnityEngine;

/// <summary>
/// Procedural texture generation for ground grid and other visual effects.
/// </summary>
public static class TextureGenerator
{
    public static Texture2D CreateGridTexture(int size, int lineWidth, Color baseColor, Color lineColor)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        tex.name = "ProceduralGrid";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onLine = x < lineWidth || y < lineWidth
                    || x >= size - lineWidth || y >= size - lineWidth;

                // Sub-grid lines at midpoint
                bool onSubLine = Mathf.Abs(x - size / 2) < lineWidth
                    || Mathf.Abs(y - size / 2) < lineWidth;

                if (onLine)
                    pixels[y * size + x] = lineColor;
                else if (onSubLine)
                    pixels[y * size + x] = lineColor * 0.4f;
                else
                    pixels[y * size + x] = baseColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(true);
        return tex;
    }
}
