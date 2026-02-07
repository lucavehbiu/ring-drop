using UnityEngine;

/// <summary>
/// Generates a procedural torus mesh.
/// Major radius = distance from center to tube center.
/// Minor radius = tube thickness.
/// </summary>
public static class TorusMeshGenerator
{
    /// <summary>
    /// Create a torus mesh with given radii and segment counts.
    /// </summary>
    /// <param name="majorRadius">Ring radius (center to tube center)</param>
    /// <param name="minorRadius">Tube radius (tube thickness)</param>
    /// <param name="majorSegments">Segments around the ring (smoothness of circle)</param>
    /// <param name="minorSegments">Segments around the tube (smoothness of tube)</param>
    public static Mesh Create(float majorRadius, float minorRadius, int majorSegments = 48, int minorSegments = 24)
    {
        var mesh = new Mesh();
        mesh.name = "Torus";

        int vertCount = (majorSegments + 1) * (minorSegments + 1);
        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        int idx = 0;
        for (int i = 0; i <= majorSegments; i++)
        {
            float majorAngle = 2f * Mathf.PI * i / majorSegments;
            float cosMaj = Mathf.Cos(majorAngle);
            float sinMaj = Mathf.Sin(majorAngle);

            // Center of tube cross-section
            Vector3 center = new Vector3(cosMaj * majorRadius, 0f, sinMaj * majorRadius);

            for (int j = 0; j <= minorSegments; j++)
            {
                float minorAngle = 2f * Mathf.PI * j / minorSegments;
                float cosMin = Mathf.Cos(minorAngle);
                float sinMin = Mathf.Sin(minorAngle);

                // Point on tube surface
                Vector3 pos = center + new Vector3(
                    cosMaj * minorRadius * cosMin,
                    sinMin * minorRadius,
                    sinMaj * minorRadius * cosMin
                );

                vertices[idx] = pos;
                normals[idx] = (pos - center).normalized;
                uvs[idx] = new Vector2((float)i / majorSegments, (float)j / minorSegments);
                idx++;
            }
        }

        // Triangles
        int triCount = majorSegments * minorSegments * 6;
        var triangles = new int[triCount];
        int tri = 0;

        for (int i = 0; i < majorSegments; i++)
        {
            for (int j = 0; j < minorSegments; j++)
            {
                int current = i * (minorSegments + 1) + j;
                int next = current + minorSegments + 1;

                triangles[tri++] = current;
                triangles[tri++] = next + 1;
                triangles[tri++] = next;

                triangles[tri++] = current;
                triangles[tri++] = current + 1;
                triangles[tri++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }
}
