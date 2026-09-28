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
