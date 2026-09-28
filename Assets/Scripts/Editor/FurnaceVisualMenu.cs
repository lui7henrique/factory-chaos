using System.IO;
using UnityEditor;
using UnityEngine;

public static class FurnaceVisualMenu
{
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";
    const string BodyPath = "Assets/Materials/FurnaceBody.mat";
    const string StructurePath = "Assets/Materials/FurnaceStructure.mat";
    const string TrayPath = "Assets/Materials/FurnaceTray.mat";
    const string AccentPath = "Assets/Materials/FurnaceAccent.mat";
    const string InteriorPath = "Assets/Materials/FurnaceInterior.mat";
    const string FlamePath = "Assets/Materials/FurnaceFlame.mat";
    const string LampPath = "Assets/Materials/FurnaceLamp.mat";
    const string BorePath = "Assets/Materials/FurnaceBore.mat";

    const string RoofPath = "Assets/Meshes/FurnaceRoof.asset";
    const string ChimneyPath = "Assets/Meshes/FurnaceChimney.asset";
    const string CapPath = "Assets/Meshes/FurnaceCap.asset";
    const string FlameMeshPath = "Assets/Meshes/FurnaceFlame.asset";

    [MenuItem("GameObject/Factory Chaos/Update Furnace Visual")]
    static void UpdateFurnaceVisual()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Update the furnace visual in edit mode.");
            return;
        }

        Transform machine = FindStation();
        if (machine == null)
        {
            Debug.LogWarning("Select the furnace, or keep the Machine object in the scene.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Update Furnace Visual");
        RebuildExisting(machine);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = machine.gameObject;
    }

    public static void RebuildExisting(Transform machine)
    {
        if (machine == null)
            return;

        Record(machine);
        Transform body = machine.Find("Body");
        if (body != null)
        {
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyRenderer != null)
                Undo.RecordObject(bodyRenderer, "Update Furnace Visual");
            if (bodyCollider != null)
                Undo.RecordObject(bodyCollider, "Update Furnace Visual");
        }

        Transform input = machine.Find("Input");
        Transform output = machine.Find("Output");
        if (input != null)
        {
            Undo.RecordObject(input, "Update Furnace Visual");
            Collider inputCollider = input.GetComponent<Collider>();
            if (inputCollider != null)
                Undo.RecordObject(inputCollider, "Update Furnace Visual");
        }

        if (output != null)
            Undo.RecordObject(output, "Update Furnace Visual");

        OreMachine oreMachine = machine.GetComponentInChildren<OreMachine>(true);
        if (oreMachine != null)
            Undo.RecordObject(oreMachine, "Update Furnace Visual");

        try
        {
            FurnaceVisual.Created = go => Undo.RegisterCreatedObjectUndo(go, "Update Furnace Visual");
            FurnaceVisual.Destroyed = go => Undo.DestroyObjectImmediate(go);
            FurnaceVisual.Rebuild(machine, LoadPalette(), LoadMeshes());
        }
        finally
        {
            FurnaceVisual.Created = null;
            FurnaceVisual.Destroyed = null;
        }

        Transform fire = machine.Find("FireVisual");
        if (fire != null && oreMachine != null)
        {
            FurnaceFire motion = fire.GetComponent<FurnaceFire>();
            if (motion == null)
                motion = Undo.AddComponent<FurnaceFire>(fire.gameObject);
            Undo.RecordObject(motion, "Update Furnace Visual");
            motion.Bind(oreMachine);
            EditorUtility.SetDirty(motion);
        }

        Renderer lens = FurnaceVisual.FindLens(machine);
        if (oreMachine != null && lens != null)
        {
            SerializedObject serialized = new SerializedObject(oreMachine);
            serialized.FindProperty("statusRenderer").objectReferenceValue = lens;
            serialized.ApplyModifiedProperties();
        }
    }

    static void Record(Transform machine)
    {
        Undo.RecordObject(machine, "Update Furnace Visual");
    }

    static Transform FindStation()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            OreMachine onSelection = selected.GetComponent<OreMachine>();
            if (onSelection == null)
                onSelection = selected.GetComponentInParent<OreMachine>();
            if (onSelection == null)
                onSelection = selected.GetComponentInChildren<OreMachine>();
            if (onSelection != null)
                return onSelection.transform.parent != null ? onSelection.transform.parent : onSelection.transform;
        }

        GameObject machine = GameObject.Find("Machine");
        return machine != null ? machine.transform : null;
    }

    static FurnaceVisual.Palette LoadPalette()
    {
        return new FurnaceVisual.Palette
        {
            body = Material(BodyPath, "FurnaceBody", new Color(0.224f, 0.482f, 0.490f), 0.08f, false),
            structure = Material(StructurePath, "FurnaceStructure", new Color(0.204f, 0.227f, 0.251f), 0.08f, false),
            tray = Material(TrayPath, "FurnaceTray", new Color(0.522f, 0.553f, 0.588f), 0.1f, false),
            accent = Material(AccentPath, "FurnaceAccent", new Color(0.957f, 0.745f, 0.196f), 0.1f, false),
            interior = Material(InteriorPath, "FurnaceInterior", new Color(0.953f, 0.416f, 0.086f), 0.12f, true),
            flame = Material(FlamePath, "FurnaceFlame", new Color(1f, 0.710f, 0.180f), 0.12f, true),
            lamp = Material(LampPath, "FurnaceLamp", new Color(0.349f, 0.937f, 0.380f), 0.15f, true),
            bore = Material(BorePath, "FurnaceBore", new Color(0.09f, 0.106f, 0.125f), 0.04f, false)
        };
    }

    static FurnaceVisual.MeshSet LoadMeshes()
    {
        FurnaceVisual.MeshSet fresh = FurnaceVisual.BuildMeshes();
        return new FurnaceVisual.MeshSet
        {
            roof = SaveMesh(RoofPath, fresh.roof),
            chimney = SaveMesh(ChimneyPath, fresh.chimney),
            cap = SaveMesh(CapPath, fresh.cap),
            flame = SaveMesh(FlameMeshPath, fresh.flame)
        };
    }

    static Mesh SaveMesh(string path, Mesh source)
    {
        EnsureFolder("Assets/Meshes");
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            existing.SetVertices(source.vertices);
            existing.SetTriangles(source.triangles, 0);
            existing.RecalculateNormals();
            existing.RecalculateBounds();
            existing.name = source.name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(source);
            return existing;
        }

        if (File.Exists(path))
        {
            Debug.LogWarning("Could not load " + path + ". A temporary mesh was used.");
            return source;
        }

        AssetDatabase.CreateAsset(source, path);
        return source;
    }

    static Material Material(string path, string materialName, Color color, float smoothness, bool emissive)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            Paint(existing, color, smoothness, emissive);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        if (File.Exists(path))
        {
            Debug.LogWarning("Could not load " + path + ".");
            return null;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (source == null)
        {
            Debug.LogWarning("Missing " + SourceMaterialPath + ".");
            return null;
        }

        Material material = new Material(source) { name = materialName };
        Paint(material, color, smoothness, emissive);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void Paint(Material material, Color color, float smoothness, bool emissive)
    {
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", null);

        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            material.SetColor("_EmissionColor", color * 0.65f);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }
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
}
