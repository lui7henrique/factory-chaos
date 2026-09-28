using System.IO;
using UnityEditor;
using UnityEngine;

public static class FurnaceVisualMenu
{
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
            FurnaceVisual.Rebuild(machine, ArtMaterials.Furnace(ArtAssetsMenu.LoadOrCreate()), LoadMeshes());
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
