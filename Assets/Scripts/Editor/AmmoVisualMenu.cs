using System.IO;
using UnityEditor;
using UnityEngine;

public static class AmmoVisualMenu
{
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";
    const string CasingPath = "Assets/Materials/AmmoCasing.mat";
    const string BandPath = "Assets/Materials/AmmoBand.mat";
    const string TipPath = "Assets/Materials/AmmoTip.mat";
    const string MeshFolder = "Assets/Meshes";
    const string BaseMeshPath = "Assets/Meshes/AmmoBase.asset";
    const string BandMeshPath = "Assets/Meshes/AmmoBand.asset";
    const string BodyMeshPath = "Assets/Meshes/AmmoBody.asset";
    const string TipMeshPath = "Assets/Meshes/AmmoTip.asset";
    const string AmmoPrefabPath = "Assets/Prefabs/Ammo.prefab";

    [MenuItem("GameObject/Factory Chaos/Update Ammo Visual")]
    static void UpdateAmmoVisual()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Update the ammo visual in edit mode.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Update Ammo Visual");

        AmmoVisual.Palette palette = LoadPalette();
        AmmoVisual.MeshSet meshes = EnsureMeshes();
        GameObject prefab = EnsureAmmoPrefab(palette, meshes);
        if (prefab == null)
        {
            Undo.CollapseUndoOperations(group);
            return;
        }

        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Update Ammo Visual");
            EnsureGameplay(root);
            AmmoVisual.Apply(root.transform, palette, meshes);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Undo.CollapseUndoOperations(group);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(AmmoPrefabPath);
        Debug.Log("Ammo visual updated on " + AmmoPrefabPath + ". Materials and meshes were reused under Assets/Materials and Assets/Meshes.", Selection.activeObject);
    }

    public static GameObject EnsureAmmoPrefab()
    {
        AmmoVisual.Palette palette = LoadPalette();
        AmmoVisual.MeshSet meshes = EnsureMeshes();
        return EnsureAmmoPrefab(palette, meshes);
    }

    static GameObject EnsureAmmoPrefab(AmmoVisual.Palette palette, AmmoVisual.MeshSet meshes)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AmmoPrefabPath);
        if (existing != null)
            return existing;

        if (File.Exists(AmmoPrefabPath))
        {
            Debug.LogWarning("Could not load " + AmmoPrefabPath + ". It was left unchanged.");
            return null;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject temp = new GameObject("Ammo");
        try
        {
            EnsureGameplay(temp);
            AmmoVisual.Apply(temp.transform, palette, meshes);
            return PrefabUtility.SaveAsPrefabAsset(temp, AmmoPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    static void EnsureGameplay(GameObject root)
    {
        Item item = root.GetComponent<Item>();
        if (item == null)
        {
            item = root.AddComponent<Item>();
            SerializedObject itemObject = new SerializedObject(item);
            itemObject.FindProperty("kind").enumValueIndex = (int)ItemKind.Ammo;
            itemObject.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            SerializedObject itemObject = new SerializedObject(item);
            SerializedProperty kind = itemObject.FindProperty("kind");
            if (kind.enumValueIndex != (int)ItemKind.Ammo)
            {
                kind.enumValueIndex = (int)ItemKind.Ammo;
                itemObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        Rigidbody body = root.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = root.AddComponent<Rigidbody>();
            body.mass = 0.4f;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (root.GetComponent<BoxCollider>() == null
            && root.GetComponent<CapsuleCollider>() == null
            && root.GetComponent<SphereCollider>() == null
            && root.GetComponent<MeshCollider>() == null)
        {
            root.AddComponent<BoxCollider>();
        }
    }

    static AmmoVisual.Palette LoadPalette()
    {
        return new AmmoVisual.Palette
        {
            casing = GetOrCreateMaterial(CasingPath, new Color(0.957f, 0.745f, 0.196f), 0.08f),
            band = GetOrCreateMaterial(BandPath, new Color(0.204f, 0.227f, 0.251f), 0.04f),
            tip = GetOrCreateMaterial(TipPath, new Color(0.851f, 0.510f, 0.231f), 0.08f)
        };
    }

    static AmmoVisual.MeshSet EnsureMeshes()
    {
        if (!AssetDatabase.IsValidFolder(MeshFolder))
            AssetDatabase.CreateFolder("Assets", "Meshes");

        AmmoVisual.MeshSet built = AmmoVisual.BuildMeshes();
        return new AmmoVisual.MeshSet
        {
            baseMesh = SaveOrReplaceMesh(BaseMeshPath, built.baseMesh),
            bandMesh = SaveOrReplaceMesh(BandMeshPath, built.bandMesh),
            bodyMesh = SaveOrReplaceMesh(BodyMeshPath, built.bodyMesh),
            tipMesh = SaveOrReplaceMesh(TipMeshPath, built.tipMesh)
        };
    }

    static Mesh SaveOrReplaceMesh(string path, Mesh source)
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

    static Material GetOrCreateMaterial(string path, Color color, float smoothness)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        if (File.Exists(path))
        {
            Debug.LogWarning("Could not load " + path + ". It was left unchanged.");
            return null;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        Material material;
        if (source != null)
            material = new Material(source);
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader != null ? shader : Shader.Find("Standard"));
        }

        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
