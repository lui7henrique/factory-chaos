using System;
using UnityEngine;

/// <summary>
/// Buried low-poly rocks for an ore vein. Decorative only: no items and no colliders.
/// </summary>
public class OreVeinVisual : MonoBehaviour
{
    static readonly Vector3[] Spots =
    {
        new Vector3(0f, -0.08f, 0.02f),
        new Vector3(0.42f, -0.1f, 0.16f),
        new Vector3(-0.38f, -0.07f, 0.22f),
        new Vector3(0.16f, -0.09f, -0.4f),
        new Vector3(-0.46f, -0.11f, -0.18f),
        new Vector3(0.5f, -0.12f, -0.34f),
        new Vector3(-0.1f, -0.06f, 0.46f)
    };

    MaterialPropertyBlock block;
    bool spent;

    public struct Palette
    {
        public Material rock;
        public Material dark;
        public Material crystal;
    }

    public static Palette RuntimePalette()
    {
        return new Palette
        {
            rock = ArtMaterials.Runtime().rock,
            dark = MakeMaterial(ArtPalette.RockDark),
            crystal = ArtMaterials.Runtime().crystal
        };
    }

    public static void Build(Transform veinRoot, Palette palette, Action<GameObject> created)
    {
        if (ApplyImported(veinRoot))
            return;

        Transform existing = veinRoot.Find("Rocks");
        if (existing != null)
        {
            if (created != null && Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        GameObject rocks = new GameObject("Rocks");
        Track(created, rocks);
        rocks.transform.SetParent(veinRoot, false);

        Mesh rockMesh = OreMesh.SharedRock();
        Mesh crystalMesh = OreMesh.SharedCrystal();
        int seed = Mathf.RoundToInt(veinRoot.position.x * 3f + veinRoot.position.z * 5f);
        for (int i = 0; i < Spots.Length; i++)
        {
            float scale = 0.42f + Hash(i + seed) * 0.38f;
            Quaternion rotation = Quaternion.Euler(Hash(i + seed + 3) * 16f - 8f, Hash(i + seed + 9) * 360f, Hash(i + seed + 5) * 18f - 9f);
            Material body = i % 3 == 0 ? palette.dark : palette.rock;
            GameObject stone = Piece(rocks.transform, "Stone", rockMesh, body, Spots[i], rotation, Vector3.one * scale, created);

            if (i % 2 == 0 && palette.crystal != null)
            {
                Vector3 crystalPos = new Vector3(Hash(i + 2) * 0.12f - 0.04f, 0.28f, Hash(i + 6) * 0.1f);
                float crystalScale = 0.16f + Hash(i + 4) * 0.1f;
                Piece(stone.transform, "Crystal", crystalMesh, palette.crystal, crystalPos, Quaternion.Euler(8f, i * 40f, 12f), Vector3.one * crystalScale, created);
            }
        }
    }

    public void SetSpent()
    {
        spent = true;
        Transform cluster = transform.Find("Cluster");
        if (cluster != null)
        {
            Tint(cluster, 0.35f);
            return;
        }

        Transform rocks = transform.Find("Rocks");
        if (rocks == null)
            return;

        Renderer[] renderers = rocks.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (renderer.gameObject.name == "Crystal")
            {
                renderer.enabled = false;
                continue;
            }

            if (renderer.sharedMaterial == null)
                continue;

            if (block == null)
                block = new MaterialPropertyBlock();

            Color color = renderer.sharedMaterial.HasProperty("_BaseColor")
                ? renderer.sharedMaterial.GetColor("_BaseColor")
                : renderer.sharedMaterial.color;
            color *= 0.32f;
            color.a = 1f;
            block.Clear();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        rocks.localPosition = new Vector3(0f, -0.06f, 0f);
    }

    void Tint(Transform root, float scale)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            if (block == null)
                block = new MaterialPropertyBlock();

            Color color = renderer.sharedMaterial.HasProperty("_BaseColor")
                ? renderer.sharedMaterial.GetColor("_BaseColor")
                : renderer.sharedMaterial.color;
            color = new Color(color.r * scale, color.g * scale, color.b * scale, 1f);
            block.Clear();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }

    const string ModelResource = "OreVein/MeshyCluster";
    const string MaterialResource = "OreVein/CrystalSurface";
    const float ClusterHeight = 1.15f;

    void Start()
    {
        ApplyImported(transform);
        if (spent)
            SetSpent();
    }

    static bool ApplyImported(Transform veinRoot)
    {
        GameObject prefab = Resources.Load<GameObject>(ModelResource);
        if (prefab == null || veinRoot == null || veinRoot.Find("Cluster") != null)
            return prefab != null;

        Transform rocks = veinRoot.Find("Rocks");
        if (rocks != null)
        {
            if (Application.isPlaying)
                Destroy(rocks.gameObject);
            else
                DestroyImmediate(rocks.gameObject);
        }

        GameObject model = UnityEngine.Object.Instantiate(prefab, veinRoot);
        model.name = "Cluster";
        Fit(model.transform, ClusterHeight);

        Material surface = Resources.Load<Material>(MaterialResource);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (surface != null)
                renderers[i].sharedMaterial = surface;
        }

        Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        if (TryBounds(model.transform, out Bounds bounds))
        {
            BoxCollider box = veinRoot.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = bounds.center;
                box.size = bounds.size;
            }
        }

        return true;
    }

    static void Fit(Transform model, float targetHeight)
    {
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;
        model.localPosition = Vector3.zero;
        if (!TryBounds(model, out Bounds bounds))
            return;

        float scale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
        model.localScale = Vector3.one * scale;
        if (!TryBounds(model, out bounds))
            return;

        model.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    static bool TryBounds(Transform model, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        Transform space = model.parent != null ? model.parent : model;
        MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
        bool found = false;
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
                continue;

            Vector3 center = mesh.bounds.center;
            Vector3 extents = mesh.bounds.extents;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = center + new Vector3(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);
                Vector3 local = space.InverseTransformPoint(filters[i].transform.TransformPoint(point));
                if (!found)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }
        }

        return found && bounds.size.y > 0.001f;
    }

    void OnEnable()
    {
        if (spent)
            SetSpent();
    }

    static GameObject Piece(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Action<GameObject> created)
    {
        GameObject piece = new GameObject(name);
        Track(created, piece);
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = position;
        piece.transform.localRotation = rotation;
        piece.transform.localScale = scale;

        MeshFilter filter = piece.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = piece.AddComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;
        return piece;
    }

    static void Track(Action<GameObject> created, GameObject piece)
    {
        if (created != null)
            created(piece);
    }

    static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0.05f);
        material.SetFloat("_Metallic", 0f);
        return material;
    }

    static float Hash(int i)
    {
        float value = Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
