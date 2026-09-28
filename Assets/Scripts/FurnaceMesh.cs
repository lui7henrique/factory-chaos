using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flat-shaded meshes for the furnace roof, chimney and flames.
/// </summary>
public static class FurnaceMesh
{
    public static Mesh Roof()
    {
        return ChamferBox("FurnaceRoof", 1.28f, 0.28f, 1.48f, 0.1f);
    }

    public static Mesh Chimney()
    {
        return ChamferBox("FurnaceChimney", 0.46f, 0.62f, 0.46f, 0.04f);
    }

    public static Mesh Cap()
    {
        return ChamferBox("FurnaceCap", 0.22f, 0.16f, 0.22f, 0.04f);
    }

    public static Mesh Flame()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        Vector3 top = new Vector3(0f, 0.22f, 0f);
        Vector3 bottom = new Vector3(0f, -0.08f, 0f);
        Vector3[] ring =
        {
            new Vector3(0.08f, 0.04f, 0f),
            new Vector3(0f, 0.04f, 0.06f),
            new Vector3(-0.08f, 0.04f, 0f),
            new Vector3(0f, 0.04f, -0.06f)
        };

        for (int i = 0; i < ring.Length; i++)
        {
            Vector3 a = ring[i];
            Vector3 b = ring[(i + 1) % ring.Length];
            AddOutward(vertices, triangles, a, top, b, a + Vector3.up);
            AddOutward(vertices, triangles, a, b, bottom, a + Vector3.down);
        }

        return Finish("FurnaceFlame", vertices, triangles);
    }

    static Mesh ChamferBox(string meshName, float width, float height, float depth, float chamfer)
    {
        float hx = width * 0.5f;
        float hy = height * 0.5f;
        float hz = depth * 0.5f;
        float c = Mathf.Min(chamfer, hx * 0.45f, hy * 0.45f, hz * 0.45f);

        Vector3[] ring =
        {
            new Vector3(hx - c, hy, hz),
            new Vector3(hx, hy - c, hz),
            new Vector3(hx, -hy, hz),
            new Vector3(-hx, -hy, hz),
            new Vector3(-hx, hy - c, hz),
            new Vector3(-hx + c, hy, hz)
        };

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        Cap(vertices, triangles, ring, 1f, Vector3.forward);
        Cap(vertices, triangles, ring, -1f, Vector3.back);

        for (int i = 0; i < ring.Length; i++)
        {
            Vector3 a = ring[i];
            Vector3 b = ring[(i + 1) % ring.Length];
            Vector3 c0 = new Vector3(a.x, a.y, -a.z);
            Vector3 d = new Vector3(b.x, b.y, -b.z);
            Vector3 outward = (a + b) * 0.5f;
            outward.z = 0f;
            AddOutward(vertices, triangles, a, b, d, outward);
            AddOutward(vertices, triangles, a, d, c0, outward);
        }

        return Finish(meshName, vertices, triangles);
    }

    static void Cap(List<Vector3> vertices, List<int> triangles, Vector3[] ring, float zSign, Vector3 outward)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < ring.Length; i++)
            center += new Vector3(ring[i].x, ring[i].y, ring[i].z * zSign);
        center /= ring.Length;

        for (int i = 0; i < ring.Length; i++)
        {
            Vector3 a = new Vector3(ring[i].x, ring[i].y, ring[i].z * zSign);
            Vector3 b = new Vector3(ring[(i + 1) % ring.Length].x, ring[(i + 1) % ring.Length].y, ring[(i + 1) % ring.Length].z * zSign);
            AddOutward(vertices, triangles, center, a, b, outward);
        }
    }

    static void AddOutward(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
        {
            Vector3 swap = b;
            b = c;
            c = swap;
        }

        Add(vertices, triangles, a, b, c);
    }

    static void Add(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
    }

    static Mesh Finish(string meshName, List<Vector3> vertices, List<int> triangles)
    {
        Mesh mesh = new Mesh { name = meshName };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
