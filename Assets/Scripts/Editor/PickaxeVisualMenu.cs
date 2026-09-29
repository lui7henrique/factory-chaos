using System.IO;
using UnityEditor;
using UnityEngine;

public static class PickaxeVisualMenu
{
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";
    const string WoodPath = "Assets/Materials/PickaxeWood.mat";
    const string GripPath = "Assets/Materials/PickaxeGrip.mat";
    const string WrapPath = "Assets/Materials/PickaxeWrap.mat";
    const string MeshFolder = "Assets/Meshes";
    const string PrefabPath = "Assets/Prefabs/PickaxeVisual.prefab";

    static readonly Color Wood = new Color(0.49f, 0.29f, 0.14f, 1f);
    static readonly Color Grip = new Color(0.20f, 0.12f, 0.085f, 1f);
    static readonly Color Wrap = new Color(0.34f, 0.19f, 0.11f, 1f);

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
            gripBands = SaveMesh(MeshFolder + "/PickaxeGripBands.asset", built.gripBands),
            buttCap = SaveMesh(MeshFolder + "/PickaxeButtCap.asset", built.buttCap),
            collars = SaveMesh(MeshFolder + "/PickaxeCollars.asset", built.collars),
            socket = SaveMesh(MeshFolder + "/PickaxeSocket.asset", built.socket),
            head = SaveMesh(MeshFolder + "/PickaxeHead.asset", built.head),
            pickEdge = SaveMesh(MeshFolder + "/PickaxePointEdge.asset", built.pickEdge),
            chiselEdge = SaveMesh(MeshFolder + "/PickaxeChiselEdge.asset", built.chiselEdge),
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
            wrap = Material(WrapPath, Wrap, 0.06f, 0f),
            iron = SharedMaterial(ArtMaterials.IronPath, ArtPalette.Iron, 0.22f, 0.32f),
            darkIron = SharedMaterial(ArtMaterials.GraphitePath, ArtPalette.Graphite, 0.12f, 0.18f)
        };
    }

    static Material SharedMaterial(string path, Color color, float smoothness, float metallic)
    {
        Material shared = AssetDatabase.LoadAssetAtPath<Material>(path);
        return shared != null ? shared : Material(path, color, smoothness, metallic);
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
