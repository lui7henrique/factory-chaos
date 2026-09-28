using System.IO;
using UnityEditor;
using UnityEngine;

public static class PickaxeVisualMenu
{
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";
    const string WoodPath = "Assets/Materials/PickaxeWood.mat";
    const string GripPath = "Assets/Materials/PickaxeGrip.mat";
    const string IronPath = "Assets/Materials/PickaxeIron.mat";
    const string RingPath = "Assets/Materials/PickaxeRing.mat";
    const string SkinPath = "Assets/Materials/PickaxeSkin.mat";
    const string MeshFolder = "Assets/Meshes";
    const string PrefabPath = "Assets/Prefabs/PickaxeVisual.prefab";

    static readonly Color Wood = new Color(0.588f, 0.376f, 0.224f, 1f);
    static readonly Color Grip = new Color(0.286f, 0.192f, 0.153f, 1f);
    static readonly Color Iron = new Color(0.659f, 0.706f, 0.769f, 1f);
    static readonly Color Ring = new Color(0.957f, 0.745f, 0.196f, 1f);
    static readonly Color Skin = new Color(0.82f, 0.58f, 0.42f, 1f);

    [MenuItem("GameObject/Factory Chaos/Setup Pickaxe Visual")]
    static void SetupPickaxeVisual()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Set up the pickaxe visual in edit mode.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Pickaxe Visual");

        PickaxeVisual.Palette palette = LoadPalette();
        PickaxeMesh.Set meshes = SaveMeshes(PickaxeMesh.Create());
        GameObject prefab = SavePrefab(palette, meshes);
        if (prefab == null)
        {
            Undo.CollapseUndoOperations(group);
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            Undo.CollapseUndoOperations(group);
            Debug.LogWarning("Pickaxe prefab saved at " + PrefabPath + ". No Main Camera was found to attach it.");
            return;
        }

        Transform anchor = camera.transform.Find("ToolAnchor");
        if (anchor == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Setup Pickaxe Visual");
            anchor = instance.transform;
        }
        else if (!PrefabUtility.IsPartOfPrefabInstance(anchor.gameObject))
        {
            Undo.RecordObject(anchor, "Setup Pickaxe Visual");
            PickaxeVisual visual = anchor.GetComponent<PickaxeVisual>();
            if (visual == null)
                visual = Undo.AddComponent<PickaxeVisual>(anchor.gameObject);
            visual.Rebuild(palette, meshes, go => Undo.RegisterCreatedObjectUndo(go, "Setup Pickaxe Visual"));
            visual.ApplyPresentation();
            visual.SetCycle(0f);
            EditorUtility.SetDirty(visual);
        }

        PlayerMining mining = Object.FindAnyObjectByType<PlayerMining>();
        Undo.CollapseUndoOperations(group);
        Selection.activeTransform = anchor;
        if (mining == null)
            Debug.Log("Pickaxe visual is on the camera. PlayerMining is not in the scene yet; Play adds it, or use Setup Player Pickaxe. Mining range was not changed.", anchor);
        else
            Debug.Log("Pickaxe visual updated on the player camera. Existing mining settings were left as they are.", anchor);
    }

    static GameObject SavePrefab(PickaxeVisual.Palette palette, PickaxeMesh.Set meshes)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing == null && File.Exists(PrefabPath))
        {
            Debug.LogWarning("Could not load " + PrefabPath + ". It was left unchanged.");
            return null;
        }

        if (existing == null)
        {
            GameObject temp = new GameObject("ToolAnchor");
            try
            {
                PickaxeVisual visual = temp.AddComponent<PickaxeVisual>();
                visual.ApplyPresentation();
                visual.Rebuild(palette, meshes, null);
                return PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            root.name = "ToolAnchor";
            PickaxeVisual visual = root.GetComponent<PickaxeVisual>();
            if (visual == null)
                visual = root.AddComponent<PickaxeVisual>();
            visual.Rebuild(palette, meshes, null);
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static PickaxeMesh.Set SaveMeshes(PickaxeMesh.Set built)
    {
        if (!AssetDatabase.IsValidFolder(MeshFolder))
            AssetDatabase.CreateFolder("Assets", "Meshes");

        return new PickaxeMesh.Set
        {
            handle = SaveMesh(MeshFolder + "/PickaxeHandle.asset", built.handle),
            grip = SaveMesh(MeshFolder + "/PickaxeGrip.asset", built.grip),
            ring = SaveMesh(MeshFolder + "/PickaxeRing.asset", built.ring),
            socket = SaveMesh(MeshFolder + "/PickaxeSocket.asset", built.socket),
            head = SaveMesh(MeshFolder + "/PickaxeHead.asset", built.head),
            peg = SaveMesh(MeshFolder + "/PickaxePeg.asset", built.peg)
        };
    }

    static Mesh SaveMesh(string path, Mesh source)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            existing.name = source.name;
            existing.SetVertices(source.vertices);
            existing.SetTriangles(source.triangles, 0);
            existing.RecalculateNormals();
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(source);
            return existing;
        }

        AssetDatabase.CreateAsset(source, path);
        return source;
    }

    static PickaxeVisual.Palette LoadPalette()
    {
        return new PickaxeVisual.Palette
        {
            wood = Material(WoodPath, Wood, 0.12f, 0f),
            grip = Material(GripPath, Grip, 0.08f, 0f),
            iron = Material(IronPath, Iron, 0.18f, 0.12f),
            ring = Material(RingPath, Ring, 0.12f, 0f),
            skin = Material(SkinPath, Skin, 0.08f, 0f)
        };
    }

    static Material Material(string path, Color color, float smoothness, float metallic)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.SetColor("_BaseColor", color);
            existing.SetColor("_Color", color);
            existing.SetFloat("_Smoothness", smoothness);
            existing.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        if (File.Exists(path))
        {
            Debug.LogWarning("Could not load " + path + ". It was left unchanged.");
            return null;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (source == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            source = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
        }

        Material material = new Material(source);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
