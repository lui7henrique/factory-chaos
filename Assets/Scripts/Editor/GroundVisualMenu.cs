using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GroundVisualMenu
{
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";
    const string GrassPath = "Assets/Materials/GroundGrass.mat";
    const string GrassLightPath = "Assets/Materials/GroundGrassLight.mat";
    const string GrassDarkPath = "Assets/Materials/GroundGrassDark.mat";
    const string DirtPath = "Assets/Materials/GroundDirt.mat";
    const string DirtLightPath = "Assets/Materials/GroundDirtLight.mat";
    const string StonePath = "Assets/Materials/GroundStone.mat";
    const string SurfacePath = "Assets/Meshes/GroundSurface.asset";
    const string TuftPath = "Assets/Meshes/GroundTufts.asset";
    const string RockPath = "Assets/Meshes/GroundRocks.asset";

    [MenuItem("GameObject/Factory Chaos/Update Ground Visual")]
    static void UpdateGroundVisual()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Update the ground visual in edit mode.");
            return;
        }

        Transform ground = FindGround();
        if (ground == null)
        {
            Debug.LogWarning("Select the Ground, or keep a Ground object in the scene.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Update Ground Visual");
        RebuildExisting(ground);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = ground.gameObject;
    }

    public static void RebuildExisting(Transform ground)
    {
        if (ground == null)
            return;

        MeshFilter filter = ground.GetComponent<MeshFilter>();
        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        MeshCollider collider = ground.GetComponent<MeshCollider>();
        if (filter == null || renderer == null)
        {
            Debug.LogWarning("Ground needs a MeshFilter and a MeshRenderer.", ground);
            return;
        }

        Undo.RecordObject(ground, "Update Ground Visual");
        Undo.RecordObject(filter, "Update Ground Visual");
        Undo.RecordObject(renderer, "Update Ground Visual");
        Mesh colliderMesh = collider != null ? collider.sharedMesh : null;
        if (collider != null)
            Undo.RecordObject(collider, "Update Ground Visual");

        GroundLook look = ground.GetComponent<GroundLook>();
        if (look == null)
            look = Undo.AddComponent<GroundLook>(ground.gameObject);

        GroundSurface.Settings settings = GroundSurface.Read(look, ground);
        Mesh surface = SaveMesh(SurfacePath, "GroundSurface");
        Mesh tufts = SaveMesh(TuftPath, "GroundTufts");
        Mesh rocks = SaveMesh(RockPath, "GroundRocks");
        GroundSurface.FillSurface(surface, settings);
        GroundSurface.KeepOut[] keepOuts = CollectKeepOuts();
        GroundSurface.FillTufts(tufts, settings, keepOuts);
        GroundSurface.FillRocks(rocks, settings, keepOuts);
        EditorUtility.SetDirty(surface);
        EditorUtility.SetDirty(tufts);
        EditorUtility.SetDirty(rocks);

        filter.sharedMesh = surface;
        renderer.sharedMaterials = new[]
        {
            Paint(GrassPath, "GroundGrass", new Color(0.529f, 0.588f, 0.325f)),
            Paint(GrassLightPath, "GroundGrassLight", new Color(0.588f, 0.639f, 0.380f)),
            Paint(GrassDarkPath, "GroundGrassDark", new Color(0.455f, 0.514f, 0.278f)),
            Paint(DirtPath, "GroundDirt", new Color(0.729f, 0.580f, 0.373f)),
            Paint(DirtLightPath, "GroundDirtLight", new Color(0.776f, 0.631f, 0.427f))
        };

        if (collider != null && colliderMesh != null)
            collider.sharedMesh = colliderMesh;

        ReplaceDecoration(ground, tufts, rocks);
    }

    static void ReplaceDecoration(Transform ground, Mesh tufts, Mesh rocks)
    {
        RemoveChild(ground, "GroundDecoration");
        RemoveChild(ground, "Stones");

        GameObject root = new GameObject("GroundDecoration");
        Undo.RegisterCreatedObjectUndo(root, "Update Ground Visual");
        Undo.SetTransformParent(root.transform, ground, "Update Ground Visual");
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        Vector3 scale = ground.localScale;
        root.transform.localScale = new Vector3(1f / Safe(scale.x), 1f, 1f / Safe(scale.z));

        Material dark = Paint(GrassDarkPath, "GroundGrassDark", new Color(0.455f, 0.514f, 0.278f));
        Material light = Paint(GrassLightPath, "GroundGrassLight", new Color(0.588f, 0.639f, 0.380f));
        Material stone = Paint(StonePath, "GroundStone", new Color(0.522f, 0.529f, 0.494f));
        CreateMeshObject(root.transform, "Tufts", tufts, new[] { dark, light });
        CreateMeshObject(root.transform, "Rocks", rocks, new[] { stone });
    }

    static void CreateMeshObject(Transform parent, string name, Mesh mesh, Material[] materials)
    {
        if (mesh == null || mesh.vertexCount == 0)
            return;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Update Ground Visual");
        Undo.SetTransformParent(go.transform, parent, "Update Ground Visual");
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        MeshFilter filter = Undo.AddComponent<MeshFilter>(go);
        filter.sharedMesh = mesh;
        MeshRenderer renderer = Undo.AddComponent<MeshRenderer>(go);
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    static void RemoveChild(Transform ground, string name)
    {
        Transform child = ground.Find(name);
        if (child != null)
            Undo.DestroyObjectImmediate(child.gameObject);
    }

    static GroundSurface.KeepOut[] CollectKeepOuts()
    {
        var list = new List<GroundSurface.KeepOut>();
        AddAll<PlayerMovement>(list, 0.6f, false);
        AddAll<OreMachine>(list, 0.5f, true);
        AddAll<DeliveryZone>(list, 0.5f, true);
        AddAll<ConveyorBelt>(list, 0.35f, true);
        AddAll<OreVein>(list, 0.7f, false);
        AddAll<OreSpawner>(list, 1.2f, false);
        AddAll<CannonController>(list, 0.6f, false);
        AddAll<AmmoMachine>(list, 0.6f, true);
        AddAll<TargetDummy>(list, 0.5f, false);
        list.Add(new GroundSurface.KeepOut { center = new Vector2(6f, -1f), radius = 2.2f });
        list.Add(new GroundSurface.KeepOut { center = new Vector2(6f, 3.5f), radius = 2.4f });
        list.Add(new GroundSurface.KeepOut { center = new Vector2(6f, 9.2f), radius = 1.8f });
        list.Add(new GroundSurface.KeepOut { center = new Vector2(-6f, -5f), radius = 2.2f });
        return list.ToArray();
    }

    static void AddAll<T>(List<GroundSurface.KeepOut> list, float pad, bool includeParent) where T : Component
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            Transform host = found[i].transform;
            if (includeParent && host.parent != null)
                host = host.parent;
            AddKeepOut(host, pad, list);
        }
    }

    static void AddKeepOut(Transform host, float pad, List<GroundSurface.KeepOut> list)
    {
        if (host == null)
            return;

        Collider[] colliders = host.GetComponentsInChildren<Collider>();
        Bounds bounds = new Bounds(host.position, Vector3.zero);
        bool any = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || !colliders[i].enabled)
                continue;
            if (!any)
            {
                bounds = colliders[i].bounds;
                any = true;
            }
            else
                bounds.Encapsulate(colliders[i].bounds);
        }

        Vector3 center = any ? bounds.center : host.position;
        float radius = any ? Mathf.Max(bounds.extents.x, bounds.extents.z) + pad : pad;
        list.Add(new GroundSurface.KeepOut
        {
            center = new Vector2(center.x, center.z),
            radius = radius
        });
    }

    static Transform FindGround()
    {
        Transform selected = Selection.activeTransform;
        if (selected != null)
        {
            if (selected.GetComponent<GroundLook>() != null || selected.name == "Ground")
                return selected;
            if (selected.parent != null && selected.parent.name == "Ground")
                return selected.parent;
        }

        GameObject ground = GameObject.Find("Ground");
        return ground != null ? ground.transform : null;
    }

    static Mesh SaveMesh(string path, string meshName)
    {
        EnsureFolder("Assets/Meshes");
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;

        Mesh mesh = new Mesh { name = meshName };
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static Material Paint(string path, string materialName, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            Apply(existing, color);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (source == null)
        {
            Debug.LogWarning("Missing " + SourceMaterialPath + ".");
            return null;
        }

        Material material = new Material(source) { name = materialName };
        Apply(material, color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void Apply(Material material, Color color)
    {
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.05f);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", null);
        material.DisableKeyword("_EMISSION");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static float Safe(float scale)
    {
        return Mathf.Abs(scale) < 0.0001f ? 1f : scale;
    }
}
