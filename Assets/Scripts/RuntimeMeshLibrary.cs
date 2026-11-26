using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Малі додаткові меші для деталей зброї, яких немає серед стандартних примітивів.
/// Використовуються разом із готовими CC0-моделями та як fallback.
/// </summary>
public static class RuntimeMeshLibrary
{
    static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();

    public static Mesh TaperedCylinder(float radiusTop, float radiusBottom, float height, int segments = 10)
    {
        string key = "Cyl_" + radiusTop + "_" + radiusBottom + "_" + height + "_" + segments;
        Mesh cached;
        if (_cache.TryGetValue(key, out cached) && cached != null)
            return cached;

        segments = Mathf.Max(3, segments);
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float half = height * 0.5f;

        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * radiusBottom, -half, Mathf.Sin(a) * radiusBottom));
            verts.Add(new Vector3(Mathf.Cos(a) * radiusTop, half, Mathf.Sin(a) * radiusTop));
        }

        for (int i = 0; i < segments; i++)
        {
            int b = i * 2;
            int n = ((i + 1) % segments) * 2;
            tris.Add(b); tris.Add(n); tris.Add(b + 1);
            tris.Add(b + 1); tris.Add(n); tris.Add(n + 1);
        }

        int bottomCenter = verts.Count;
        verts.Add(new Vector3(0f, -half, 0f));
        int topCenter = verts.Count;
        verts.Add(new Vector3(0f, half, 0f));

        for (int i = 0; i < segments; i++)
        {
            int b = i * 2;
            int n = ((i + 1) % segments) * 2;
            tris.Add(bottomCenter); tris.Add(n); tris.Add(b);
            tris.Add(topCenter); tris.Add(b + 1); tris.Add(n + 1);
        }

        var mesh = new Mesh { name = key };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _cache[key] = mesh;
        return mesh;
    }

    /// <summary>
    /// Клинок/наконечник: звужується до кінчика вздовж осі Z.
    /// </summary>
    public static Mesh WedgeBlade(float length, float width, float thickness)
    {
        string key = "Blade_" + length + "_" + width + "_" + thickness;
        Mesh cached;
        if (_cache.TryGetValue(key, out cached) && cached != null)
            return cached;

        float z0 = -length * 0.5f;
        float z1 = length * 0.5f;
        float w = width * 0.5f;
        float t = thickness * 0.5f;
        float tipW = w * 0.16f;
        float tipT = t * 0.5f;

        var verts = new List<Vector3>();
        var tris = new List<int>();

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        // Кожна грань має власні вершини — нормалі не згладжуються й клинок не «зникає».
        AddQuad(new Vector3(-w, -t, z0), new Vector3(-w, t, z0),
                new Vector3(w, t, z0), new Vector3(w, -t, z0));                    // -Z
        AddQuad(new Vector3(-tipW, -tipT, z1), new Vector3(tipW, -tipT, z1),
                new Vector3(tipW, tipT, z1), new Vector3(-tipW, tipT, z1));        // +Z
        AddQuad(new Vector3(-w, -t, z0), new Vector3(w, -t, z0),
                new Vector3(tipW, -tipT, z1), new Vector3(-tipW, -tipT, z1));      // -Y
        AddQuad(new Vector3(-w, t, z0), new Vector3(-tipW, tipT, z1),
                new Vector3(tipW, tipT, z1), new Vector3(w, t, z0));               // +Y
        AddQuad(new Vector3(w, -t, z0), new Vector3(w, t, z0),
                new Vector3(tipW, tipT, z1), new Vector3(tipW, -tipT, z1));        // +X
        AddQuad(new Vector3(-w, -t, z0), new Vector3(-tipW, -tipT, z1),
                new Vector3(-tipW, tipT, z1), new Vector3(-w, t, z0));             // -X

        var mesh = new Mesh { name = key };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _cache[key] = mesh;
        return mesh;
    }

    /// <summary>
    /// Кільце у площині XY — для магічних/енергетичних деталей.
    /// </summary>
    public static Mesh Ring(float outerRadius, float innerRadius, int segments = 20)
    {
        string key = "Ring_" + outerRadius + "_" + innerRadius + "_" + segments;
        Mesh cached;
        if (_cache.TryGetValue(key, out cached) && cached != null)
            return cached;

        segments = Mathf.Max(6, segments);
        if (innerRadius > outerRadius)
        {
            float swap = innerRadius;
            innerRadius = outerRadius;
            outerRadius = swap;
        }

        var verts = new List<Vector3>();
        var tris = new List<int>();

        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * innerRadius, Mathf.Sin(a) * innerRadius, 0f));
            verts.Add(new Vector3(Mathf.Cos(a) * outerRadius, Mathf.Sin(a) * outerRadius, 0f));
        }

        for (int i = 0; i < segments; i++)
        {
            int c = i * 2;
            int n = ((i + 1) % segments) * 2;
            tris.Add(c); tris.Add(c + 1); tris.Add(n + 1);
            tris.Add(c); tris.Add(n + 1); tris.Add(n);
        }

        var mesh = new Mesh { name = key };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _cache[key] = mesh;
        return mesh;
    }

    /// <summary>
    /// Дуга у площині XY — для клинкового слешу. Тоншає з кінців, як у реального удару.
    /// </summary>
    public static Mesh ArcRibbon(float outerRadius, float innerRadius, float arcDegrees, int segments = 20)
    {
        string key = "Arc_" + outerRadius + "_" + innerRadius + "_" + arcDegrees + "_" + segments;
        Mesh cached;
        if (_cache.TryGetValue(key, out cached) && cached != null)
            return cached;

        segments = Mathf.Max(6, segments);
        arcDegrees = Mathf.Clamp(arcDegrees, 10f, 350f);
        float span = arcDegrees * Mathf.Deg2Rad;
        float start = -span * 0.5f;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float a = start + span * t;
            float taper = 0.45f + 0.55f * Mathf.Sin(t * Mathf.PI);
            float dirX = Mathf.Cos(a);
            float dirY = Mathf.Sin(a);
            verts.Add(new Vector3(dirX * innerRadius * taper, dirY * innerRadius * taper, 0f));
            verts.Add(new Vector3(dirX * outerRadius * taper, dirY * outerRadius * taper, 0f));
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));
        }

        for (int i = 0; i < segments; i++)
        {
            int c = i * 2;
            int n = c + 2;
            tris.Add(c); tris.Add(c + 1); tris.Add(n + 1);
            tris.Add(c); tris.Add(n + 1); tris.Add(n);
        }

        var mesh = new Mesh { name = key };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _cache[key] = mesh;
        return mesh;
    }
}
