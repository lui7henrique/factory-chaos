using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class IronIngotMenu
{
    public const string PrefabPath = "Assets/Prefabs/Product.prefab";
    public const string MeshPath = "Assets/Models/IronIngot.asset";
    public const string MaterialPath = "Assets/Materials/IronIngot.mat";
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";

    static readonly Color IngotColor = new Color(0xA8 / 255f, 0xB4 / 255f, 0xC4 / 255f, 1f);

    [DidReloadScripts]
    static void ApplyOnceWhenMeshMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            // First import creates the mesh asset and rewires Product; later refreshes use the menu.
            if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath) != null)
                return;
            TryUpdateIronIngotVisual();
        };
    }

    [MenuItem("GameObject/Factory Chaos/Update Iron Ingot Visual")]
    public static void UpdateIronIngotVisual()
    {
        TryUpdateIronIngotVisual();
    }

    public static bool TryUpdateIronIngotVisual()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Update the iron ingot visual in edit mode.");
            return false;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing " + PrefabPath + ".");
            return false;
        }

        Mesh mesh = GetOrUpdateMesh();
        Material material = GetOrUpdateMaterial();
        if (mesh == null || material == null)
            return false;

        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ApplyToRoot(root, mesh, material);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Updated iron ingot visual on " + PrefabPath + ".");
        return true;
    }

    public static void ApplyToRoot(GameObject root, Mesh mesh, Material material)
    {
        if (root == null)
            return;

        DestroyChild(root.transform, "Visual");
        DestroyChild(root.transform, "Visuals");

        MeshRenderer bodyRenderer = root.GetComponent<MeshRenderer>();
        if (bodyRenderer != null)
            bodyRenderer.enabled = false;

        // World sizes are baked into the mesh; keep root at 1 so scale is not applied twice.
        root.transform.localScale = Vector3.one;

        BoxCollider box = root.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = Vector3.zero;
            box.size = new Vector3(IronIngotMesh.Length, IronIngotMesh.Height, IronIngotMesh.BaseWidth);
        }

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        MeshFilter filter = visual.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
    }

    public static Mesh GetOrUpdateMesh()
    {
        EnsureFolder("Assets/Models");

        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null)
        {
            IronIngotMesh.Fill(existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        if (File.Exists(MeshPath))
        {
            Debug.LogWarning("Could not load " + MeshPath + ". It was left unchanged.");
            return null;
        }

        Mesh mesh = IronIngotMesh.Create();
        AssetDatabase.CreateAsset(mesh, MeshPath);
        return mesh;
    }

    public static Material GetOrUpdateMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
        {
            ApplyMaterialColors(existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        if (File.Exists(MaterialPath))
        {
            Debug.LogWarning("Could not load " + MaterialPath + ". It was left unchanged.");
            return null;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (source == null)
        {
            Debug.LogWarning("Missing " + SourceMaterialPath + ". Could not create " + MaterialPath + ".");
            return null;
        }

        Material material = new Material(source);
        material.name = "IronIngot";
        ApplyMaterialColors(material);
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    static void ApplyMaterialColors(Material material)
    {
        material.SetColor("_BaseColor", IngotColor);
        material.SetColor("_Color", IngotColor);
        material.SetFloat("_Metallic", 0.35f);
        material.SetFloat("_Smoothness", 0.25f);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", null);
    }

    static void DestroyChild(Transform root, string childName)
    {
        Transform existing = root.Find(childName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }

    static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string name = Path.GetFileName(assetFolder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            AssetDatabase.CreateFolder(parent, name);
    }
}
