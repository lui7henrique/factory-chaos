using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the low-poly chamfered iron ingot mesh (world-space meters, centered).
/// </summary>
public static class IronIngotMesh
{
    public const float Length = 0.6f;
    public const float BaseWidth = 0.28f;
    public const float TopWidth = 0.22f;
    public const float Height = 0.14f;
    public const float TopLength = 0.52f;
    public const float Chamfer = 0.01f;
    public const float CornerCutBase = 0.028f;
    public const float CornerCutTop = 0.022f;

    public static Mesh Create()
    {
        var mesh = new Mesh { name = "IronIngot" };
        Fill(mesh);
        return mesh;
    }

    public static void Fill(Mesh mesh)
    {
        if (mesh == null)
            return;

        var vertices = new List<Vector3>(256);
        var triangles = new List<int>(384);

        float yBottom = -Height * 0.5f;
        float yLow = yBottom + Chamfer;
        float yHigh = Height * 0.5f - Chamfer;
        float yTop = Height * 0.5f;

        Vector3[] ringBottom = OutlineAt(yBottom);
        Vector3[] ringLow = OutlineAt(yLow);
        Vector3[] ringHigh = OutlineAt(yHigh);
        Vector3[] ringTop = OutlineAt(yTop);

        AddCap(vertices, triangles, ringBottom, downward: true);
        AddCap(vertices, triangles, ringTop, downward: false);
        ConnectRings(vertices, triangles, ringBottom, ringLow);
        ConnectRings(vertices, triangles, ringLow, ringHigh);
        ConnectRings(vertices, triangles, ringHigh, ringTop);

        mesh.Clear();
        mesh.name = "IronIngot";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    static Vector3[] OutlineAt(float y)
    {
        float t = Mathf.InverseLerp(-Height * 0.5f, Height * 0.5f, y);
        float length = Mathf.Lerp(Length, TopLength, t);
        float width = Mathf.Lerp(BaseWidth, TopWidth, t);
        float cut = Mathf.Lerp(CornerCutBase, CornerCutTop, t);
        float maxCut = Mathf.Min(length, width) * 0.45f;
        cut = Mathf.Min(cut, maxCut);
        return CutRect(length * 0.5f, width * 0.5f, cut, y);
    }

    /// <summary>Eight points, CCW when viewed from above (length on X, width on Z).</summary>
    static Vector3[] CutRect(float halfLength, float halfWidth, float cut, float y)
    {
        return new[]
        {
            new Vector3(halfLength - cut, y, halfWidth),
            new Vector3(halfLength, y, halfWidth - cut),
            new Vector3(halfLength, y, -halfWidth + cut),
            new Vector3(halfLength - cut, y, -halfWidth),
            new Vector3(-halfLength + cut, y, -halfWidth),
            new Vector3(-halfLength, y, -halfWidth + cut),
            new Vector3(-halfLength, y, halfWidth - cut),
            new Vector3(-halfLength + cut, y, halfWidth)
        };
    }

    static void AddCap(List<Vector3> vertices, List<int> triangles, Vector3[] ring, bool downward)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < ring.Length; i++)
            center += ring[i];
        center /= ring.Length;

        for (int i = 0; i < ring.Length; i++)
        {
            Vector3 a = ring[i];
            Vector3 b = ring[(i + 1) % ring.Length];
            if (downward)
                AddTriangle(vertices, triangles, a, center, b);
            else
                AddTriangle(vertices, triangles, a, b, center);
        }
    }

    static void ConnectRings(List<Vector3> vertices, List<int> triangles, Vector3[] lower, Vector3[] upper)
    {
        int count = lower.Length;
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            Vector3 a = lower[i];
            Vector3 b = lower[next];
            Vector3 c = upper[next];
            Vector3 d = upper[i];
            AddTriangle(vertices, triangles, a, b, c);
            AddTriangle(vertices, triangles, a, c, d);
        }
    }

    static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
    }
}
