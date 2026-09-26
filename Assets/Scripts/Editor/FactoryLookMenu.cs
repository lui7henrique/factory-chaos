using UnityEditor;
using UnityEngine;

public static class FactoryLookMenu
{
    const string MachineBodyPath = "Assets/Materials/Machine.mat";
    const string MachineAccentPath = "Assets/Materials/MachineAccent.mat";
    const string MachineLampPath = "Assets/Materials/MachineLamp.mat";
    const string DeliveryBasePath = "Assets/Materials/Delivery - Base.mat";
    const string DeliveryFramePath = "Assets/Materials/DeliveryFrame.mat";
    const string DeliveryPadPath = "Assets/Materials/DeliveryPad.mat";
    const string DeliveryAccentPath = "Assets/Materials/DeliveryAccent.mat";

    [MenuItem("GameObject/Factory Chaos/Restyle Ore And Ground")]
    static void RestyleOreAndGround()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Restyle the factory in edit mode.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restyle Ore And Ground");

        StyleGround();
        StyleOrePrefab();
        StyleProductPrefab();

        Undo.CollapseUndoOperations(group);
    }

    static void StyleGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
            return;

        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        Material groundMaterial = Load("Assets/Materials/Ground.mat");
        if (renderer != null && groundMaterial != null)
        {
            Undo.RecordObject(renderer, "Restyle Ground");
            renderer.sharedMaterial = groundMaterial;
        }

        Undo.RecordObject(ground.transform, "Restyle Ground");
        ground.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
    }

    static void StyleOrePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ore.prefab");
        if (prefab == null)
            return;

        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform existing = root.transform.Find("Visuals");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            MeshRenderer body = root.GetComponent<MeshRenderer>();
            if (body != null)
                body.enabled = false;

            Material rock = Load("Assets/Materials/Ore.mat");
            Material dark = Load("Assets/Materials/OreDark.mat");
            Material vein = Load("Assets/Materials/OreVein.mat");
            Transform visuals = CreateEmpty(root.transform, "Visuals");
            CreateCube(visuals, "Rock", new Vector3(0f, -0.12f, 0f), Quaternion.Euler(8f, 24f, -6f), new Vector3(1.15f, 0.92f, 1.05f), rock);
            CreateCube(visuals, "RockDark", new Vector3(0.18f, -0.2f, 0.08f), Quaternion.Euler(-14f, 70f, 16f), new Vector3(0.72f, 0.55f, 0.64f), dark);
            CreateCube(visuals, "CrystalUp", new Vector3(0.06f, 0.08f, -0.02f), Quaternion.Euler(-12f, 18f, 8f), new Vector3(0.72f, 0.62f, 0.68f), vein);
            CreateCube(visuals, "CrystalSide", new Vector3(-0.22f, -0.02f, 0.08f), Quaternion.Euler(78f, -36f, 12f), new Vector3(0.58f, 0.5f, 0.55f), vein);
            CreateCube(visuals, "CrystalBack", new Vector3(0.16f, -0.04f, -0.2f), Quaternion.Euler(62f, 150f, -18f), new Vector3(0.48f, 0.46f, 0.46f), vein);
            CreateCube(visuals, "CrystalSmall", new Vector3(0.22f, 0.1f, 0.16f), Quaternion.Euler(28f, 48f, -16f), new Vector3(0.4f, 0.36f, 0.38f), vein);
            OreMesh.Apply(root);

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void StyleProductPrefab()
    {
        // Keep product on the iron ingot visual; do not restore the old crystal look.
        Mesh mesh = IronIngotMenu.GetOrUpdateMesh();
        Material material = IronIngotMenu.GetOrUpdateMaterial();
        if (mesh == null || material == null)
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(IronIngotMenu.PrefabPath);
        if (prefab == null)
            return;

        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            IronIngotMenu.ApplyToRoot(root, mesh, material);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("GameObject/Factory Chaos/Restyle Machine And Delivery")]
    static void Restyle()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Restyle the factory in edit mode.");
            return;
        }

        GameObject machine = GameObject.Find("Machine");
        GameObject delivery = GameObject.Find("Delivery");
        if (machine == null || delivery == null)
        {
            Debug.LogWarning("The scene needs the existing Machine and Delivery objects.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restyle Machine And Delivery");

        StyleMachine(machine);
        StyleDelivery(delivery);

        Undo.CollapseUndoOperations(group);
        Selection.objects = new Object[] { machine, delivery };
    }

    static void StyleMachine(GameObject machine)
    {
        Transform root = machine.transform;
        ClearVisuals(root);

        Material body = Load(MachineBodyPath);
        Material accent = Load(MachineAccentPath);
        Material lamp = Load(MachineLampPath);

        Transform bodyTransform = root.Find("Body");
        if (bodyTransform != null)
        {
            MeshRenderer bodyRenderer = bodyTransform.GetComponent<MeshRenderer>();
            if (bodyRenderer != null)
            {
                Undo.RecordObject(bodyRenderer, "Restyle Machine");
                bodyRenderer.sharedMaterial = body;
            }
        }

        Transform visuals = CreateEmpty(root, "Visuals");

        CreateCube(visuals, "HopperBack", new Vector3(0f, 1.18f, -0.48f), Quaternion.Euler(-25f, 0f, 0f), new Vector3(1.15f, 0.08f, 0.55f), body);
        CreateCube(visuals, "HopperFront", new Vector3(0f, 1.18f, 0.48f), Quaternion.Euler(25f, 0f, 0f), new Vector3(1.15f, 0.08f, 0.55f), body);
        CreateCube(visuals, "HopperLeft", new Vector3(-0.48f, 1.18f, 0f), Quaternion.Euler(0f, 0f, 25f), new Vector3(0.55f, 0.08f, 1.15f), body);
        CreateCube(visuals, "HopperRight", new Vector3(0.48f, 1.18f, 0f), Quaternion.Euler(0f, 0f, -25f), new Vector3(0.55f, 0.08f, 1.15f), body);

        CreateCube(visuals, "ChuteFloor", new Vector3(0f, 0.18f, 1.15f), Quaternion.identity, new Vector3(0.62f, 0.06f, 0.9f), body);
        CreateCube(visuals, "ChuteLeft", new Vector3(-0.3f, 0.32f, 1.15f), Quaternion.identity, new Vector3(0.06f, 0.22f, 0.9f), accent);
        CreateCube(visuals, "ChuteRight", new Vector3(0.3f, 0.32f, 1.15f), Quaternion.identity, new Vector3(0.06f, 0.22f, 0.9f), accent);

        CreateCube(visuals, "Stripe", new Vector3(0.72f, 0.52f, 0f), Quaternion.identity, new Vector3(0.08f, 0.28f, 0.9f), accent);
        GameObject lampObject = CreateCube(visuals, "StatusLamp", new Vector3(0.72f, 0.88f, 0.32f), Quaternion.identity, new Vector3(0.08f, 0.16f, 0.16f), lamp);

        CreateCube(visuals, "LegFL", new Vector3(-0.58f, 0.08f, 0.58f), Quaternion.identity, new Vector3(0.18f, 0.16f, 0.18f), accent);
        CreateCube(visuals, "LegFR", new Vector3(0.58f, 0.08f, 0.58f), Quaternion.identity, new Vector3(0.18f, 0.16f, 0.18f), accent);
        CreateCube(visuals, "LegBL", new Vector3(-0.58f, 0.08f, -0.58f), Quaternion.identity, new Vector3(0.18f, 0.16f, 0.18f), accent);
        CreateCube(visuals, "LegBR", new Vector3(0.58f, 0.08f, -0.58f), Quaternion.identity, new Vector3(0.18f, 0.16f, 0.18f), accent);

        OreMachine oreMachine = root.GetComponentInChildren<OreMachine>();
        if (oreMachine != null)
        {
            Undo.RecordObject(oreMachine, "Restyle Machine");
            SerializedObject so = new SerializedObject(oreMachine);
            so.FindProperty("statusRenderer").objectReferenceValue = lampObject.GetComponent<MeshRenderer>();
            so.ApplyModifiedProperties();
        }
    }

    static void StyleDelivery(GameObject delivery)
    {
        Transform root = delivery.transform;
        ClearVisuals(root);

        Material frame = Load(DeliveryFramePath);
        Material pad = Load(DeliveryPadPath);
        Material accent = Load(DeliveryAccentPath);
        Material baseMaterial = Load(DeliveryBasePath);

        Transform baseTransform = root.Find("Base");
        if (baseTransform != null)
        {
            MeshRenderer baseRenderer = baseTransform.GetComponent<MeshRenderer>();
            if (baseRenderer != null)
            {
                Undo.RecordObject(baseRenderer, "Restyle Delivery");
                baseRenderer.sharedMaterial = baseMaterial;
            }
        }

        Transform visuals = CreateEmpty(root, "Visuals");
        CreateCube(visuals, "Pad", new Vector3(0f, 0.22f, 0f), Quaternion.identity, new Vector3(1.15f, 0.04f, 1.15f), pad);

        CreateCube(visuals, "RailLeft", new Vector3(-0.78f, 0.28f, 0f), Quaternion.identity, new Vector3(0.08f, 0.2f, 1.52f), frame);
        CreateCube(visuals, "RailRight", new Vector3(0.78f, 0.28f, 0f), Quaternion.identity, new Vector3(0.08f, 0.2f, 1.52f), frame);
        CreateCube(visuals, "RailBack", new Vector3(0f, 0.28f, 0.78f), Quaternion.identity, new Vector3(1.52f, 0.2f, 0.08f), frame);
        CreateCube(visuals, "RailFront", new Vector3(0f, 0.28f, -0.78f), Quaternion.identity, new Vector3(1.52f, 0.2f, 0.08f), frame);

        CreateCube(visuals, "PostFL", new Vector3(-0.78f, 0.42f, -0.78f), Quaternion.identity, new Vector3(0.12f, 0.48f, 0.12f), accent);
        CreateCube(visuals, "PostFR", new Vector3(0.78f, 0.42f, -0.78f), Quaternion.identity, new Vector3(0.12f, 0.48f, 0.12f), accent);
        CreateCube(visuals, "PostBL", new Vector3(-0.78f, 0.42f, 0.78f), Quaternion.identity, new Vector3(0.12f, 0.48f, 0.12f), accent);
        CreateCube(visuals, "PostBR", new Vector3(0.78f, 0.42f, 0.78f), Quaternion.identity, new Vector3(0.12f, 0.48f, 0.12f), accent);

        CreateCube(visuals, "SignPole", new Vector3(-0.98f, 0.7f, 0f), Quaternion.identity, new Vector3(0.08f, 1.2f, 0.08f), frame);
        CreateCube(visuals, "SignBoard", new Vector3(-0.98f, 1.28f, 0f), Quaternion.identity, new Vector3(0.08f, 0.42f, 0.62f), pad);
        CreateCube(visuals, "SignMark", new Vector3(-0.93f, 1.28f, 0f), Quaternion.identity, new Vector3(0.04f, 0.2f, 0.2f), accent);
    }

    static void ClearVisuals(Transform root)
    {
        Transform existing = root.Find("Visuals");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);
    }

    static Transform CreateEmpty(Transform parent, string name)
    {
        GameObject empty = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(empty, "Restyle Factory");
        Undo.SetTransformParent(empty.transform, parent, "Restyle Factory");
        empty.transform.localPosition = Vector3.zero;
        empty.transform.localRotation = Quaternion.identity;
        empty.transform.localScale = Vector3.one;
        return empty.transform;
    }

    static GameObject CreateCube(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        return CreatePrimitive(parent, name, PrimitiveType.Cube, localPosition, localRotation, localScale, material);
    }

    static void CreateCrystal(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        CreatePrimitive(parent, name + "A", PrimitiveType.Cube, localPosition, localRotation, localScale, material);
        CreatePrimitive(parent, name + "B", PrimitiveType.Cube, localPosition, localRotation * Quaternion.Euler(0f, 45f, 0f), localScale, material);
    }

    static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Undo.RegisterCreatedObjectUndo(part, "Restyle Factory");
        Undo.SetTransformParent(part.transform, parent, "Restyle Factory");
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Undo.DestroyObjectImmediate(collider);

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;

        return part;
    }

    static Material Load(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }
}
