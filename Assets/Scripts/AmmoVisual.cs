using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the low-poly octagonal ammo shell under a Visual child. Gameplay stays on the root.
/// </summary>
public static class AmmoVisual
{
    public const float Height = 0.6f;
    public const float BodyRadius = 0.13f;
    public const float BaseRadius = 0.15f;

    public const string VisualName = "Visual";

    public struct Palette
    {
        public Material casing;
        public Material band;
        public Material tip;
    }

    public struct MeshSet
    {
        public Mesh baseMesh;
        public Mesh bandMesh;
        public Mesh bodyMesh;
        public Mesh tipMesh;
    }

    public static Palette RuntimePalette()
    {
        return new Palette
        {
            casing = MakeMaterial(Hex(0xF4BE32), 0.08f),
            band = MakeMaterial(Hex(0x343A40), 0.04f),
            tip = MakeMaterial(Hex(0xD9823B), 0.08f)
        };
    }

    public static MeshSet BuildMeshes()
    {
        const float half = Height * 0.5f;
        // Rings measured from the base (0..0.6), then shifted so the shell is centered on the root.
        float Shift(float yFromBase) => yFromBase - half;

        Mesh baseMesh = BuildSegment(
            "AmmoBase",
            new[]
            {
                new Ring(Shift(0.00f), BaseRadius),
                new Ring(Shift(0.02f), BaseRadius),
                new Ring(Shift(0.05f), BodyRadius)
            },
            closeBottom: true,
            closeTop: false);

        Mesh bandMesh = BuildSegment(
            "AmmoBand",
            new[]
            {
                new Ring(Shift(0.05f), BodyRadius),
                new Ring(Shift(0.11f), BodyRadius)
            },
            closeBottom: false,
            closeTop: false);

        Mesh bodyMesh = BuildSegment(
            "AmmoBody",
            new[]
            {
                new Ring(Shift(0.11f), BodyRadius),
                new Ring(Shift(0.40f), BodyRadius)
            },
            closeBottom: false,
            closeTop: false);

        Mesh tipMesh = BuildSegment(
            "AmmoTip",
            new[]
            {
                new Ring(Shift(0.40f), BodyRadius),
                new Ring(Shift(0.48f), 0.072f),
                new Ring(Shift(0.55f), 0.032f),
                new Ring(Shift(0.60f), 0f)
            },
            closeBottom: false,
            closeTop: true);

        return new MeshSet
        {
            baseMesh = baseMesh,
            bandMesh = bandMesh,
            bodyMesh = bodyMesh,
            tipMesh = tipMesh
        };
    }

    public static void Apply(Transform root, Palette palette, MeshSet meshes)
    {
        if (root == null || meshes.baseMesh == null || meshes.bandMesh == null
            || meshes.bodyMesh == null || meshes.tipMesh == null)
            return;

        palette = Fill(palette);

        Transform visual = EnsureVisual(root);
        ClearChildren(visual);

        Part(visual, "Base", meshes.baseMesh, palette.casing);
        Part(visual, "Band", meshes.bandMesh, palette.band);
        Part(visual, "Body", meshes.bodyMesh, palette.casing);
        Part(visual, "Tip", meshes.tipMesh, palette.tip);

        FitCollider(root);
        DisableRootRenderer(root);
    }

    public static void ApplyRuntime(Transform root)
    {
        Apply(root, RuntimePalette(), BuildMeshes());
    }

    static Transform EnsureVisual(Transform root)
    {
        Transform visual = root.Find(VisualName);
        if (visual != null)
            return visual;

        GameObject go = new GameObject(VisualName);
        go.transform.SetParent(root, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    static void ClearChildren(Transform visual)
    {
        for (int i = visual.childCount - 1; i >= 0; i--)
        {
            GameObject child = visual.GetChild(i).gameObject;
            if (Application.isPlaying)
                Object.Destroy(child);
            else
                Object.DestroyImmediate(child);
        }
    }

    static void Part(Transform parent, string name, Mesh mesh, Material material)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        MeshFilter filter = part.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;
    }

    static void FitCollider(Transform root)
    {
        BoxCollider box = root.GetComponent<BoxCollider>();
        if (box == null)
            box = root.gameObject.AddComponent<BoxCollider>();

        box.center = Vector3.zero;
        box.size = new Vector3(BaseRadius * 2f, Height, BaseRadius * 2f);
        box.isTrigger = false;

        Collider[] colliders = root.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != box)
            {
                if (Application.isPlaying)
                    Object.Destroy(colliders[i]);
                else
                    Object.DestroyImmediate(colliders[i]);
            }
        }
    }

    static void DisableRootRenderer(Transform root)
    {
        MeshRenderer renderer = root.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    static Mesh BuildSegment(string meshName, Ring[] rings, bool closeBottom, bool closeTop)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        const int sides = 8;

        for (int r = 0; r < rings.Length - 1; r++)
        {
            Ring lower = rings[r];
            Ring upper = rings[r + 1];
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                Vector3 l0 = Point(lower, a0);
                Vector3 l1 = Point(lower, a1);
                Vector3 u0 = Point(upper, a0);
                Vector3 u1 = Point(upper, a1);
                Vector3 outward = (l0 + l1 + u0 + u1) * 0.25f;
                outward.y = 0f;
                if (outward.sqrMagnitude < 0.0001f)
                    outward = Vector3.up;

                if (lower.radius < 0.0001f)
                {
                    AddOutward(vertices, triangles, u0, u1, l0, outward + Vector3.down);
                }
                else if (upper.radius < 0.0001f)
                {
                    AddOutward(vertices, triangles, l0, u0, l1, outward + Vector3.up);
                }
                else
                {
                    AddOutward(vertices, triangles, l0, u0, l1, outward);
                    AddOutward(vertices, triangles, u0, u1, l1, outward);
                }
            }
        }

        if (closeBottom && rings[0].radius > 0.0001f)
            Cap(vertices, triangles, rings[0], Vector3.down);

        if (closeTop)
        {
            Ring top = rings[rings.Length - 1];
            if (top.radius > 0.0001f)
                Cap(vertices, triangles, top, Vector3.up);
        }

        return Finish(meshName, vertices, triangles);
    }

    static void Cap(List<Vector3> vertices, List<int> triangles, Ring ring, Vector3 outward)
    {
        const int sides = 8;
        Vector3 center = new Vector3(0f, ring.y, 0f);
        for (int i = 0; i < sides; i++)
        {
            float a0 = (i / (float)sides) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
            Vector3 p0 = Point(ring, a0);
            Vector3 p1 = Point(ring, a1);
            if (outward.y > 0f)
                AddOutward(vertices, triangles, center, p0, p1, outward);
            else
                AddOutward(vertices, triangles, center, p1, p0, outward);
        }
    }

    static Vector3 Point(Ring ring, float angle)
    {
        return new Vector3(Mathf.Cos(angle) * ring.radius, ring.y, Mathf.Sin(angle) * ring.radius);
    }

    static void AddOutward(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
        {
            Vector3 swap = b;
            b = c;
            c = swap;
        }

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
        var mesh = new Mesh { name = meshName };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Palette Fill(Palette palette)
    {
        Palette fallback = RuntimePalette();
        if (palette.casing == null) palette.casing = fallback.casing;
        if (palette.band == null) palette.band = fallback.band;
        if (palette.tip == null) palette.tip = fallback.tip;
        return palette;
    }

    static Material MakeMaterial(Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        return material;
    }

    static Color Hex(int rgb)
    {
        return new Color32(
            (byte)((rgb >> 16) & 0xFF),
            (byte)((rgb >> 8) & 0xFF),
            (byte)(rgb & 0xFF),
            255);
    }

    struct Ring
    {
        public float y;
        public float radius;

        public Ring(float y, float radius)
        {
            this.y = y;
            this.radius = radius;
        }
    }
}
