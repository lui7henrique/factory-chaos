using System.IO;
using UnityEditor;
using UnityEngine;

public static class CannonVisualMenu
{
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";
    const string StructurePath = "Assets/Materials/CannonStructure.mat";
    const string SupportPath = "Assets/Materials/CannonSupport.mat";
    const string AccentPath = "Assets/Materials/CannonAccent.mat";
    const string JointPath = "Assets/Materials/CannonJoint.mat";
    const string BorePath = "Assets/Materials/CannonBore.mat";
    const string LampPath = "Assets/Materials/CannonLamp.mat";
    const string ModelsFolder = "Assets/Models";
    const string CylinderPath = "Assets/Models/CannonCylinder8.asset";
    const string RingPath = "Assets/Models/CannonRing8.asset";
    const string HexPath = "Assets/Models/CannonHex.asset";
    const string TubePath = "Assets/Models/CannonTube8.asset";

    [MenuItem("GameObject/Factory Chaos/Update Cannon Visual")]
    static void UpdateCannonVisual()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Update the cannon visual in edit mode.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Update Cannon Visual");

        CannonController cannon = FindCannon();
        GameObject root;
        bool created = false;
        if (cannon == null)
        {
            Transform parent = GameObject.Find("CombatTest") != null
                ? GameObject.Find("CombatTest").transform
                : null;
            root = CreateVisualCannon(parent);
            created = true;
        }
        else
        {
            root = cannon.gameObject;
            Undo.RecordObject(root.transform, "Update Cannon Visual");
            Undo.RecordObject(cannon, "Update Cannon Visual");
            CannonLoader loader = root.GetComponentInChildren<CannonLoader>(true);
            if (loader != null)
                Undo.RecordObject(loader, "Update Cannon Visual");
            ApplyRebuild(root.transform);
        }

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = root;

        if (created)
        {
            Debug.Log(
                "Nenhum canhão funcional encontrado: criado objeto visual com YawPivot, PitchPivot, MuzzlePoint, Sight e Input. " +
                "Posicione-o na cena e salve manualmente se quiser persistir. Materiais em Assets/Materials, meshes em Assets/Models.",
                root);
        }
        else
        {
            Debug.Log("Cannon visual updated. Scripts and combat settings were preserved.", root);
        }
    }

    public static GameObject CreateVisualCannon(Transform parent)
    {
        GameObject root = new GameObject("Cannon");
        Undo.RegisterCreatedObjectUndo(root, "Update Cannon Visual");
        if (parent != null)
            Undo.SetTransformParent(root.transform, parent, "Update Cannon Visual");

        root.transform.localPosition = new Vector3(6f, 0f, 3.5f);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        ApplyRebuild(root.transform);

        CannonController controller = root.GetComponent<CannonController>();
        if (controller != null)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("capacity").intValue = 3;
            serialized.FindProperty("damage").floatValue = 20f;
            serialized.FindProperty("projectileSpeed").floatValue = 22f;
            serialized.FindProperty("projectileLifetime").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        return root;
    }

    public static void ApplyRebuild(Transform cannonRoot)
    {
        try
        {
            CannonVisual.Created = go => Undo.RegisterCreatedObjectUndo(go, "Update Cannon Visual");
            CannonVisual.Destroyed = go => Undo.DestroyObjectImmediate(go);
            CannonVisual.Rebuild(cannonRoot, LoadPalette(), EnsureMeshes());
        }
        finally
        {
            CannonVisual.Created = null;
            CannonVisual.Destroyed = null;
        }

        EditorUtility.SetDirty(cannonRoot.gameObject);
    }

    static CannonController FindCannon()
    {
        if (Selection.activeGameObject != null)
        {
            CannonController selected = Selection.activeGameObject.GetComponentInParent<CannonController>();
            if (selected == null)
                selected = Selection.activeGameObject.GetComponentInChildren<CannonController>(true);
            if (selected != null)
                return selected;
        }

        GameObject combat = GameObject.Find("CombatTest");
        if (combat != null)
        {
            Transform child = combat.transform.Find("Cannon");
            if (child != null)
            {
                CannonController underCombat = child.GetComponent<CannonController>();
                if (underCombat != null)
                    return underCombat;
            }
        }

        GameObject named = GameObject.Find("Cannon");
        if (named != null)
        {
            CannonController byName = named.GetComponent<CannonController>();
            if (byName != null)
                return byName;
        }

        return Object.FindAnyObjectByType<CannonController>();
    }

    static CannonVisual.Palette LoadPalette()
    {
        return new CannonVisual.Palette
        {
            structure = GetOrUpdate(StructurePath, Hex(0x343A40), 0.06f, false),
            support = GetOrUpdate(SupportPath, Hex(0x397B7D), 0.06f, false),
            accent = GetOrUpdate(AccentPath, Hex(0xF4BE32), 0.08f, false),
            joint = GetOrUpdate(JointPath, Hex(0x858D96), 0.1f, false),
            bore = GetOrUpdate(BorePath, Hex(0x171B20), 0.04f, false),
            lamp = GetOrUpdate(LampPath, Hex(0x59EF61), 0.15f, true)
        };
    }

    static CannonVisual.MeshSet EnsureMeshes()
    {
        if (!AssetDatabase.IsValidFolder(ModelsFolder))
            AssetDatabase.CreateFolder("Assets", "Models");

        CannonVisual.MeshSet built = CannonVisual.BuildMeshes();
        return new CannonVisual.MeshSet
        {
            cylinder8 = SaveOrReplaceMesh(CylinderPath, built.cylinder8),
            ring8 = SaveOrReplaceMesh(RingPath, built.ring8),
            hex = SaveOrReplaceMesh(HexPath, built.hex),
            tube8 = SaveOrReplaceMesh(TubePath, built.tube8)
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

    static Material GetOrUpdate(string path, Color color, float smoothness, bool emissive)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null && File.Exists(path))
        {
            Debug.LogWarning("Could not load " + path + ". It was left unchanged.");
            return null;
        }

        if (material == null)
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
            if (source != null)
                material = new Material(source);
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                material = new Material(shader);
            }

            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.45f);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static Color Hex(int rgb)
    {
        float r = ((rgb >> 16) & 0xFF) / 255f;
        float g = ((rgb >> 8) & 0xFF) / 255f;
        float b = (rgb & 0xFF) / 255f;
        return new Color(r, g, b, 1f);
    }
}
