using System.IO;
using UnityEditor;
using UnityEngine;

public static class AmmoMachineMenu
{
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";
    const string StructurePath = "Assets/Materials/AmmoStructure.mat";
    const string PanelPath = "Assets/Materials/AmmoPanel.mat";
    const string AccentPath = "Assets/Materials/AmmoAccent.mat";
    const string MetalPath = "Assets/Materials/AmmoPressMetal.mat";
    const string BeltPath = "Assets/Materials/AmmoBelt.mat";
    const string LampPath = "Assets/Materials/AmmoStatusLamp.mat";
    const string AmmoPrefabPath = "Assets/Prefabs/Ammo.prefab";

    [MenuItem("GameObject/Factory Chaos/Restyle Ammo Machine")]
    static void Restyle()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Restyle the ammo machine in edit mode.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restyle Ammo Machine");

        AmmoMachine machine = FindMachine();
        GameObject station;
        if (machine == null)
        {
            Transform parent = GameObject.Find("CombatTest") != null ? GameObject.Find("CombatTest").transform : null;
            station = CreateStation(parent, AssetDatabase.LoadAssetAtPath<GameObject>(AmmoPrefabPath));
        }
        else
        {
            station = StationRoot(machine).gameObject;
            Undo.RecordObject(station.transform, "Restyle Ammo Machine");
            Transform input = station.transform.Find("Input");
            Transform output = station.transform.Find("Output");
            if (input != null)
                Undo.RecordObject(input, "Restyle Ammo Machine");
            if (output != null)
                Undo.RecordObject(output, "Restyle Ammo Machine");
            Undo.RecordObject(machine, "Restyle Ammo Machine");
            ApplyRebuild(station.transform);
        }

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = station;
    }

    public static GameObject CreateStation(Transform parent, GameObject ammoPrefab)
    {
        GameObject root = new GameObject("AmmoMachine");
        Undo.RegisterCreatedObjectUndo(root, "Restyle Ammo Machine");
        if (parent != null)
            Undo.SetTransformParent(root.transform, parent, "Restyle Ammo Machine");

        root.transform.localPosition = new Vector3(6f, 0f, -1f);
        root.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        root.transform.localScale = Vector3.one;

        GameObject input = new GameObject("Input");
        Undo.RegisterCreatedObjectUndo(input, "Restyle Ammo Machine");
        Undo.SetTransformParent(input.transform, root.transform, "Restyle Ammo Machine");
        BoxCollider box = input.AddComponent<BoxCollider>();
        box.isTrigger = true;
        AmmoMachine machine = input.AddComponent<AmmoMachine>();

        GameObject output = new GameObject("Output");
        Undo.RegisterCreatedObjectUndo(output, "Restyle Ammo Machine");
        Undo.SetTransformParent(output.transform, root.transform, "Restyle Ammo Machine");

        ApplyRebuild(root.transform);

        Transform lens = root.transform.Find("Visuals/StatusLight/Lens");
        SerializedObject serialized = new SerializedObject(machine);
        serialized.FindProperty("processDuration").floatValue = 2f;
        serialized.FindProperty("outputPoint").objectReferenceValue = output.transform;
        serialized.FindProperty("ammoPrefab").objectReferenceValue = ammoPrefab;
        serialized.FindProperty("outputClearRadius").floatValue = 0.34f;
        serialized.FindProperty("statusRenderer").objectReferenceValue = lens != null ? lens.GetComponent<Renderer>() : null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    static void ApplyRebuild(Transform station)
    {
        try
        {
            AmmoMachineVisual.Created = go => Undo.RegisterCreatedObjectUndo(go, "Restyle Ammo Machine");
            AmmoMachineVisual.Destroyed = go => Undo.DestroyObjectImmediate(go);
            AmmoMachineVisual.Rebuild(station, LoadPalette());
        }
        finally
        {
            AmmoMachineVisual.Created = null;
            AmmoMachineVisual.Destroyed = null;
        }
    }

    static AmmoMachine FindMachine()
    {
        if (Selection.activeGameObject != null)
        {
            AmmoMachine selected = Selection.activeGameObject.GetComponentInParent<AmmoMachine>();
            if (selected == null)
                selected = Selection.activeGameObject.GetComponentInChildren<AmmoMachine>(true);
            if (selected != null)
                return selected;
        }

        return Object.FindAnyObjectByType<AmmoMachine>();
    }

    static Transform StationRoot(AmmoMachine machine)
    {
        if (machine.transform.name == "AmmoMachine")
            return machine.transform;

        if (machine.transform.parent != null && machine.transform.parent.name == "AmmoMachine")
            return machine.transform.parent;

        return machine.transform;
    }

    static AmmoMachineVisual.Palette LoadPalette()
    {
        return new AmmoMachineVisual.Palette
        {
            structure = GetOrCreate(StructurePath, new Color(0.204f, 0.227f, 0.251f), 0.06f, false),
            panel = GetOrCreate(PanelPath, new Color(0.224f, 0.482f, 0.490f), 0.06f, false),
            accent = GetOrCreate(AccentPath, new Color(0.957f, 0.745f, 0.196f), 0.08f, false),
            metal = GetOrCreate(MetalPath, new Color(0.522f, 0.553f, 0.588f), 0.1f, false),
            belt = GetOrCreate(BeltPath, new Color(0.145f, 0.161f, 0.180f), 0.04f, false),
            lamp = GetOrCreate(LampPath, new Color(0.349f, 0.937f, 0.380f), 0.15f, true)
        };
    }

    static Material GetOrCreate(string path, Color color, float smoothness, bool emissive)
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
        if (source == null)
        {
            Debug.LogWarning("Missing " + SourceMaterialPath + ". Could not create " + path + ".");
            return null;
        }

        Material material = new Material(source);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.45f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
