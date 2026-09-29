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
        public Mesh gripBands;
        public Mesh buttCap;
        public Mesh collars;
        public Mesh socket;
        public Mesh head;
        public Mesh pickEdge;
        public Mesh chiselEdge;
        public Mesh peg;
    }

    public static Set Create()
    {
        return new Set
        {
            handle = ProfilePrism("PickaxeHandle", new[]
            {
                new Profile { y = 0.18f, radius = 0.034f },
                new Profile { y = 0.3f, radius = 0.032f },
                new Profile { y = 0.54f, radius = 0.029f },
                new Profile { y = 0.7f, radius = 0.033f },
                new Profile { y = 0.75f, radius = 0.036f }
            }, 8),
            grip = Prism("PickaxeGrip", -0.005f, 0.22f, 0.043f, 0.037f, 8),
            gripBands = GripBands(),
            buttCap = Prism("PickaxeButtCap", -0.032f, 0.016f, 0.047f, 0.045f, 8),
            collars = Collars(),
            socket = Prism("PickaxeSocket", 0.695f, 0.832f, 0.056f, 0.052f, 8),
            head = HeadBody(),
            pickEdge = PickEdge(),
            chiselEdge = ChiselEdge(),
            peg = Prism("PickaxePeg", 0.825f, 0.89f, 0.026f, 0.022f, 8)
        };
    }

    struct Profile
    {
        public float y;
        public float radius;
    }

    struct Slice
    {
        public float x;
        public float y;
        public float halfY;
        public float halfZ;
    }

    static Mesh HeadBody()
    {
        Slice[] pick =
        {
            new Slice { x = 0.015f, y = 0.79f, halfY = 0.058f, halfZ = 0.061f },
            new Slice { x = 0.1f, y = 0.8f, halfY = 0.052f, halfZ = 0.057f },
            new Slice { x = 0.22f, y = 0.785f, halfY = 0.04f, halfZ = 0.046f },
            new Slice { x = 0.34f, y = 0.745f, halfY = 0.026f, halfZ = 0.032f },
            new Slice { x = 0.42f, y = 0.685f, halfY = 0.016f, halfZ = 0.021f }
        };
        Slice[] chisel =
        {
            new Slice { x = -0.015f, y = 0.79f, halfY = 0.058f, halfZ = 0.061f },
            new Slice { x = -0.11f, y = 0.795f, halfY = 0.052f, halfZ = 0.062f },
            new Slice { x = -0.22f, y = 0.78f, halfY = 0.044f, halfZ = 0.068f },
            new Slice { x = -0.31f, y = 0.755f, halfY = 0.035f, halfZ = 0.078f }
        };

        var vertices = new List<Vector3>(256);
        var triangles = new List<int>(384);
        Loft(vertices, triangles, pick);
        Loft(vertices, triangles, chisel);

        return Finish("PickaxeHead", vertices, triangles);
    }

    static Mesh PickEdge()
    {
        Slice[] slices =
        {
            new Slice { x = 0.405f, y = 0.695f, halfY = 0.017f, halfZ = 0.022f },
            new Slice { x = 0.465f, y = 0.64f, halfY = 0.01f, halfZ = 0.014f },
            new Slice { x = 0.51f, y = 0.585f, halfY = 0.0025f, halfZ = 0.004f }
        };
        var vertices = new List<Vector3>(64);
        var triangles = new List<int>(96);
        Loft(vertices, triangles, slices);
        return Finish("PickaxePointEdge", vertices, triangles);
    }

    static Mesh ChiselEdge()
    {
        Slice[] slices =
        {
            new Slice { x = -0.295f, y = 0.76f, halfY = 0.036f, halfZ = 0.079f },
            new Slice { x = -0.365f, y = 0.735f, halfY = 0.028f, halfZ = 0.087f },
            new Slice { x = -0.405f, y = 0.72f, halfY = 0.018f, halfZ = 0.091f }
        };
        var vertices = new List<Vector3>(64);
        var triangles = new List<int>(96);
        Loft(vertices, triangles, slices);
        return Finish("PickaxeChiselEdge", vertices, triangles);
    }

    static Mesh GripBands()
    {
        var vertices = new List<Vector3>(320);
        var triangles = new List<int>(480);
        for (int i = 0; i < 6; i++)
        {
            float y = 0.015f + i * 0.037f;
            AppendPrism(vertices, triangles, y, y + 0.014f, 0.046f - i * 0.0012f, 0.045f - i * 0.0012f, 8);
        }
        return Finish("PickaxeGripBands", vertices, triangles);
    }

    static Mesh Collars()
    {
        var vertices = new List<Vector3>(192);
        var triangles = new List<int>(288);
        AppendPrism(vertices, triangles, 0.205f, 0.236f, 0.043f, 0.041f, 8);
        AppendPrism(vertices, triangles, 0.68f, 0.715f, 0.061f, 0.061f, 8);
        AppendPrism(vertices, triangles, 0.81f, 0.842f, 0.059f, 0.057f, 8);
        return Finish("PickaxeCollars", vertices, triangles);
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
        AppendPrism(vertices, triangles, y0, y1, radius0, radius1, sides);
        return Finish(meshName, vertices, triangles);
    }

    static Mesh ProfilePrism(string meshName, Profile[] profile, int sides)
    {
        var vertices = new List<Vector3>(profile.Length * sides * 4);
        var triangles = new List<int>(profile.Length * sides * 6);
        Vector3[][] rings = new Vector3[profile.Length][];
        for (int i = 0; i < profile.Length; i++)
            rings[i] = Ring(profile[i].y, profile[i].radius, sides);

        for (int p = 0; p < profile.Length - 1; p++)
        {
            Vector3 inside = new Vector3(0f, (profile[p].y + profile[p + 1].y) * 0.5f, 0f);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                AddOutwardQuad(vertices, triangles, rings[p][i], rings[p][next], rings[p + 1][next], rings[p + 1][i], inside);
            }
        }

        AddCap(vertices, triangles, rings[0], new Vector3(0f, profile[0].y, 0f), downward: true);
        AddCap(vertices, triangles, rings[rings.Length - 1], new Vector3(0f, profile[profile.Length - 1].y, 0f), downward: false);
        return Finish(meshName, vertices, triangles);
    }

    static void AppendPrism(List<Vector3> vertices, List<int> triangles, float y0, float y1, float radius0, float radius1, int sides)
    {
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
    }

    static Mesh Finish(string meshName, List<Vector3> vertices, List<int> triangles)
    {
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
