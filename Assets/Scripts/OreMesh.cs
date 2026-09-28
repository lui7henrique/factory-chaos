using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the low-poly rock and crystal meshes used by the ore prefab.
/// </summary>
public class OreMesh : MonoBehaviour
{
    static Mesh rock;
    static Mesh crystal;

    void Awake()
    {
        Apply(gameObject);
    }

    public static Mesh SharedRock()
    {
        return RockMesh();
    }

    public static Mesh SharedCrystal()
    {
        return CrystalMesh();
    }

    public static void Apply(GameObject root)
    {
        if (root == null)
            return;

        Mesh rockMesh = RockMesh();
        Mesh crystalMesh = CrystalMesh();
        Assign(root.transform, "Rock", rockMesh);
        Assign(root.transform, "RockDark", rockMesh);
        Assign(root.transform, "CrystalUp", crystalMesh);
        Assign(root.transform, "CrystalSide", crystalMesh);
        Assign(root.transform, "CrystalBack", crystalMesh);
        Assign(root.transform, "CrystalSmall", crystalMesh);
    }

    static void Assign(Transform root, string childName, Mesh mesh)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != childName)
                continue;

            MeshFilter filter = children[i].GetComponent<MeshFilter>();
            if (filter != null)
                filter.sharedMesh = mesh;
        }
    }

    static Mesh RockMesh()
    {
        if (rock != null)
            return rock;

        float phi = (1f + Mathf.Sqrt(5f)) * 0.5f;
        Vector3[] source =
        {
            new Vector3(-1f, phi, 0f), new Vector3(1f, phi, 0f),
            new Vector3(-1f, -phi, 0f), new Vector3(1f, -phi, 0f),
            new Vector3(0f, -1f, phi), new Vector3(0f, 1f, phi),
            new Vector3(0f, -1f, -phi), new Vector3(0f, 1f, -phi),
            new Vector3(phi, 0f, -1f), new Vector3(phi, 0f, 1f),
            new Vector3(-phi, 0f, -1f), new Vector3(-phi, 0f, 1f)
        };

        int[] faces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };

        Vector3[] shaped = new Vector3[source.Length];
        Vector3 center = Vector3.zero;
        for (int i = 0; i < source.Length; i++)
        {
            float wobble = 0.78f + 0.34f * Hash01(i + 4);
            Vector3 point = source[i].normalized * wobble;
            point.y *= 0.74f;
            shaped[i] = point;
            center += point;
        }

        center /= source.Length;
        float radius = 0.001f;
        for (int i = 0; i < shaped.Length; i++)
        {
            shaped[i] -= center;
            radius = Mathf.Max(radius, shaped[i].magnitude);
        }

        float fit = 0.52f / radius;
        var vertices = new List<Vector3>(faces.Length);
        for (int i = 0; i < faces.Length; i += 3)
        {
            AddOutward(
                vertices,
                shaped[faces[i]] * fit,
                shaped[faces[i + 1]] * fit,
                shaped[faces[i + 2]] * fit,
                Vector3.zero);
        }

        rock = Build("OreRock", vertices);
        return rock;
    }

    static Mesh CrystalMesh()
    {
        if (crystal != null)
            return crystal;

        const int sides = 6;
        Vector3[] ring = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = (i / (float)sides) * Mathf.PI * 2f + 0.35f;
            float radius = i % 2 == 0 ? 0.46f : 0.38f;
            ring[i] = new Vector3(Mathf.Cos(angle) * radius, 0.02f, Mathf.Sin(angle) * radius);
        }

        ring[1].x += 0.03f;
        Vector3 top = new Vector3(0.03f, 1.12f, -0.02f);
        Vector3 bottom = new Vector3(-0.02f, -0.22f, 0.02f);
        Vector3 center = new Vector3(0f, 0.35f, 0f);
        var vertices = new List<Vector3>(sides * 6);
        for (int i = 0; i < sides; i++)
        {
            Vector3 next = ring[(i + 1) % sides];
            AddOutward(vertices, ring[i], top, next, center);
            AddOutward(vertices, ring[i], next, bottom, center);
        }

        crystal = Build("OreCrystal", vertices);
        return crystal;
    }

    static void AddOutward(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c, Vector3 center)
    {
        Vector3 mid = (a + b + c) / 3f;
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), mid - center) < 0f)
        {
            Vector3 swap = a;
            a = c;
            c = swap;
        }

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
    }

    static Mesh Build(string meshName, List<Vector3> vertices)
    {
        var mesh = new Mesh { name = meshName };
        mesh.SetVertices(vertices);
        int[] triangles = new int[vertices.Count];
        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = i;

        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static float Hash01(int i)
    {
        float value = Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
