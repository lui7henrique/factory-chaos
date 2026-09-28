using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Low-poly pickaxe meshes in meters. Handle runs along +Y, the point along +X.
/// </summary>
public static class PickaxeMesh
{
    public struct Set
    {
        public Mesh handle;
        public Mesh grip;
        public Mesh ring;
        public Mesh socket;
        public Mesh head;
        public Mesh peg;
    }

    public static Set Create()
    {
        return new Set
        {
            handle = Prism("PickaxeHandle", 0.16f, 0.7f, 0.0225f, 0.0225f, 6),
            grip = Prism("PickaxeGrip", 0f, 0.16f, 0.03f, 0.026f, 6),
            ring = Prism("PickaxeRing", 0.655f, 0.69f, 0.034f, 0.034f, 6),
            socket = Prism("PickaxeSocket", 0.69f, 0.8f, 0.05f, 0.046f, 6),
            head = Head(),
            peg = Prism("PickaxePeg", 0.8f, 0.86f, 0.016f, 0.014f, 6)
        };
    }

    struct Slice
    {
        public float x;
        public float y;
        public float halfY;
        public float halfZ;
    }

    static Mesh Head()
    {
        Slice[] pick =
        {
            new Slice { x = 0.02f, y = 0.78f, halfY = 0.05f, halfZ = 0.046f },
            new Slice { x = 0.08f, y = 0.785f, halfY = 0.04f, halfZ = 0.036f },
            new Slice { x = 0.15f, y = 0.75f, halfY = 0.026f, halfZ = 0.022f },
            new Slice { x = 0.22f, y = 0.68f, halfY = 0.014f, halfZ = 0.012f },
            new Slice { x = 0.3f, y = 0.6f, halfY = 0.004f, halfZ = 0.003f }
        };
        Slice[] chisel =
        {
            new Slice { x = 0.02f, y = 0.78f, halfY = 0.05f, halfZ = 0.046f },
            new Slice { x = -0.05f, y = 0.775f, halfY = 0.042f, halfZ = 0.044f },
            new Slice { x = -0.11f, y = 0.75f, halfY = 0.026f, halfZ = 0.052f },
            new Slice { x = -0.18f, y = 0.72f, halfY = 0.01f, halfZ = 0.058f }
        };

        var vertices = new List<Vector3>(256);
        var triangles = new List<int>(384);
        Loft(vertices, triangles, pick);
        Loft(vertices, triangles, chisel);

        var mesh = new Mesh { name = "PickaxeHead" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void Loft(List<Vector3> vertices, List<int> triangles, Slice[] slices)
    {
        Vector3[][] rings = new Vector3[slices.Length][];
        for (int i = 0; i < slices.Length; i++)
            rings[i] = Rect(slices[i]);

        for (int i = 0; i < slices.Length - 1; i++)
        {
            Vector3 inside = new Vector3((slices[i].x + slices[i + 1].x) * 0.5f, (slices[i].y + slices[i + 1].y) * 0.5f, 0f);
            for (int c = 0; c < 4; c++)
            {
                int next = (c + 1) % 4;
                AddOutwardQuad(vertices, triangles, rings[i][c], rings[i][next], rings[i + 1][next], rings[i + 1][c], inside);
            }
        }

        float endSign = Mathf.Sign(slices[slices.Length - 1].x - slices[0].x);
        if (Mathf.Abs(endSign) < 0.01f)
            endSign = 1f;
        Vector3 endInside = RectCenter(slices[slices.Length - 1]) - new Vector3(endSign * 0.05f, 0f, 0f);
        AddCap(vertices, triangles, rings[rings.Length - 1], RectCenter(slices[slices.Length - 1]), endInside);
    }

    static Vector3[] Rect(Slice slice)
    {
        return new[]
        {
            new Vector3(slice.x, slice.y + slice.halfY, slice.halfZ),
            new Vector3(slice.x, slice.y + slice.halfY, -slice.halfZ),
            new Vector3(slice.x, slice.y - slice.halfY, -slice.halfZ),
            new Vector3(slice.x, slice.y - slice.halfY, slice.halfZ)
        };
    }

    static Vector3 RectCenter(Slice slice)
    {
        return new Vector3(slice.x, slice.y, 0f);
    }

    static Mesh Prism(string meshName, float y0, float y1, float radius0, float radius1, int sides)
    {
        var vertices = new List<Vector3>(sides * 8);
        var triangles = new List<int>(sides * 12);
        Vector3[] bottom = Ring(y0, radius0, sides);
        Vector3[] top = Ring(y1, radius1, sides);
        Vector3 inside = new Vector3(0f, (y0 + y1) * 0.5f, 0f);

        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            AddOutwardQuad(vertices, triangles, bottom[i], bottom[next], top[next], top[i], inside);
        }

        AddCap(vertices, triangles, bottom, new Vector3(0f, y0, 0f), downward: true);
        AddCap(vertices, triangles, top, new Vector3(0f, y1, 0f), downward: false);

        var mesh = new Mesh { name = meshName };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Vector3[] Ring(float y, float radius, int sides)
    {
        var points = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = (i / (float)sides) * Mathf.PI * 2f + Mathf.PI / 6f;
            points[i] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        return points;
    }

    static void AddCap(List<Vector3> vertices, List<int> triangles, Vector3[] ring, Vector3 center, Vector3 inside)
    {
        for (int i = 0; i < ring.Length; i++)
        {
            int next = (i + 1) % ring.Length;
            AddOutwardTri(vertices, triangles, center, ring[i], ring[next], inside);
        }
    }

    static void AddCap(List<Vector3> vertices, List<int> triangles, Vector3[] ring, Vector3 center, bool downward)
    {
        Vector3 inside = center + (downward ? Vector3.up : Vector3.down) * 0.05f;
        for (int i = 0; i < ring.Length; i++)
        {
            int next = (i + 1) % ring.Length;
            if (downward)
                AddOutwardTri(vertices, triangles, center, ring[next], ring[i], inside);
            else
                AddOutwardTri(vertices, triangles, center, ring[i], ring[next], inside);
        }
    }

    static void AddOutwardQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
    {
        int index = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        Vector3 normal = Vector3.Cross(b - a, c - a);
        Vector3 mid = (a + b + c + d) * 0.25f;
        if (Vector3.Dot(normal, mid - inside) >= 0f)
        {
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }
        else
        {
            triangles.Add(index);
            triangles.Add(index + 3);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);
        }
    }

    static void AddOutwardTri(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
    {
        int index = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        Vector3 normal = Vector3.Cross(b - a, c - a);
        Vector3 mid = (a + b + c) / 3f;
        if (Vector3.Dot(normal, mid - inside) < 0f)
        {
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);
        }
        else
        {
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }
    }
}
