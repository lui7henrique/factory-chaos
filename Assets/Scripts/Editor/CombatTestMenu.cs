using System.IO;
using UnityEditor;
using UnityEngine;

public static class CombatTestMenu
{
    const string MachineMaterialPath = "Assets/Materials/Machine.mat";
    const string AmmoMaterialPath = "Assets/Materials/Ammo.mat";
    const string CannonMaterialPath = "Assets/Materials/Cannon.mat";
    const string TargetMaterialPath = "Assets/Materials/Target.mat";
    const string AmmoPrefabPath = "Assets/Prefabs/Ammo.prefab";

    [MenuItem("GameObject/Factory Chaos/Create Combat Test")]
    static void CreateCombatTest()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Create the combat test in edit mode.");
            return;
        }

        Material ammoMaterial = GetOrCreateMaterial(AmmoMaterialPath, new Color(0.95f, 0.62f, 0.12f), 0.12f);
        Material cannonMaterial = GetOrCreateMaterial(CannonMaterialPath, new Color(0.22f, 0.24f, 0.27f), 0.08f);
        Material targetMaterial = GetOrCreateMaterial(TargetMaterialPath, new Color(0.75f, 0.18f, 0.16f), 0.06f);
        GameObject ammoPrefab = GetOrCreateAmmoPrefab(ammoMaterial);

        GameObject existing = GameObject.Find("CombatTest");
        if (existing != null)
        {
            Debug.LogWarning("CombatTest already exists. Remove it before creating another one.", existing);
            Selection.activeGameObject = existing;
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Combat Test");

        GameObject root = CreateEmpty(null, "CombatTest");
        GameObject machine = AmmoMachineMenu.CreateStation(root.transform, ammoPrefab);
        GameObject cannon = CreateCannon(root.transform, cannonMaterial, ammoMaterial);
        CreateTarget(root.transform, targetMaterial, cannonMaterial);

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = machine;
        Debug.Log("Combat test created east of the factory. Ammo machine at (6, 0, -1), cannon at (6, 0, 3.5), target at (6, 0, 9.2).", root);
    }

    static GameObject CreateCannon(Transform parent, Material bodyMaterial, Material ammoMaterial)
    {
        GameObject root = CreateEmpty(parent, "Cannon");
        root.transform.localPosition = new Vector3(6f, 0f, 3.5f);

        CreatePart(root.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), Quaternion.identity, new Vector3(1.3f, 0.7f, 1.8f), bodyMaterial, ColliderKind.Solid, true);

        Transform yaw = CreateEmpty(root.transform, "YawPivot").transform;
        yaw.localPosition = new Vector3(0f, 0.78f, 0.1f);
        Transform pitch = CreateEmpty(yaw, "PitchPivot").transform;

        CreatePart(pitch, "Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.75f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.28f, 0.65f, 0.28f), bodyMaterial, ColliderKind.Solid, true);
        Transform muzzle = CreateEmpty(pitch, "Muzzle").transform;
        muzzle.localPosition = new Vector3(0f, 0f, 1.5f);
        Transform sight = CreateEmpty(pitch, "Sight").transform;
        sight.localPosition = new Vector3(0f, 0.32f, -0.9f);

        GameObject input = CreatePart(root.transform, "Input", PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.75f), Quaternion.identity, new Vector3(0.85f, 0.45f, 0.7f), ammoMaterial, ColliderKind.Trigger, false);
        CreatePart(root.transform, "Hopper", PrimitiveType.Cube, new Vector3(0f, 0.95f, -0.75f), Quaternion.identity, new Vector3(0.7f, 0.08f, 0.55f), ammoMaterial, ColliderKind.None, true);

        CannonController cannon = Undo.AddComponent<CannonController>(root);
        SerializedObject cannonObject = new SerializedObject(cannon);
        cannonObject.FindProperty("yawPivot").objectReferenceValue = yaw;
        cannonObject.FindProperty("pitchPivot").objectReferenceValue = pitch;
        cannonObject.FindProperty("muzzle").objectReferenceValue = muzzle;
        cannonObject.FindProperty("sight").objectReferenceValue = sight;
        cannonObject.FindProperty("capacity").intValue = 3;
        cannonObject.FindProperty("damage").floatValue = 20f;
        cannonObject.FindProperty("projectileSpeed").floatValue = 22f;
        cannonObject.FindProperty("projectileLifetime").floatValue = 3f;
        cannonObject.FindProperty("projectileMaterial").objectReferenceValue = ammoMaterial;
        cannonObject.ApplyModifiedPropertiesWithoutUndo();

        CannonLoader loader = Undo.AddComponent<CannonLoader>(input);
        SerializedObject loaderObject = new SerializedObject(loader);
        loaderObject.FindProperty("cannon").objectReferenceValue = cannon;
        loaderObject.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    static void CreateTarget(Transform parent, Material boardMaterial, Material baseMaterial)
    {
        GameObject root = CreateEmpty(parent, "Target");
        root.transform.localPosition = new Vector3(6f, 0f, 9.2f);

        CreatePart(root.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), Quaternion.identity, new Vector3(1.6f, 0.3f, 1.2f), baseMaterial, ColliderKind.Solid, true);
        GameObject board = CreatePart(root.transform, "Board", PrimitiveType.Cube, new Vector3(0f, 1.35f, 0f), Quaternion.identity, new Vector3(1.3f, 2.1f, 0.4f), boardMaterial, ColliderKind.Solid, true);

        TargetDummy dummy = Undo.AddComponent<TargetDummy>(root);
        SerializedObject serialized = new SerializedObject(dummy);
        serialized.FindProperty("maxHealth").floatValue = 100f;
        serialized.FindProperty("board").objectReferenceValue = board.GetComponent<Renderer>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
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

        Material source = AssetDatabase.LoadAssetAtPath<Material>(MachineMaterialPath);
        if (source == null)
        {
            Debug.LogWarning("Missing " + MachineMaterialPath + ". Could not create " + path + ".");
            return null;
        }

        Material material = new Material(source);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static GameObject GetOrCreateAmmoPrefab(Material material)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AmmoPrefabPath);
        if (existing != null)
            return existing;

        if (File.Exists(AmmoPrefabPath))
        {
            Debug.LogWarning("Could not load " + AmmoPrefabPath + ". It was left unchanged.");
            return null;
        }

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            temp.name = "Ammo";
            temp.transform.localScale = new Vector3(0.16f, 0.16f, 0.42f);

            MeshRenderer renderer = temp.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            Rigidbody body = temp.AddComponent<Rigidbody>();
            body.mass = 0.4f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Item item = temp.AddComponent<Item>();
            SerializedObject itemObject = new SerializedObject(item);
            itemObject.FindProperty("kind").enumValueIndex = (int)ItemKind.Ammo;
            itemObject.ApplyModifiedPropertiesWithoutUndo();

            return PrefabUtility.SaveAsPrefabAsset(temp, AmmoPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    static GameObject CreateEmpty(Transform parent, string name)
    {
        GameObject empty = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(empty, "Create Combat Test");
        if (parent != null)
            Undo.SetTransformParent(empty.transform, parent, "Create Combat Test");
        empty.transform.localPosition = Vector3.zero;
        empty.transform.localRotation = Quaternion.identity;
        empty.transform.localScale = Vector3.one;
        return empty;
    }

    static GameObject CreatePart(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material,
        ColliderKind colliderKind,
        bool visible)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Undo.RegisterCreatedObjectUndo(part, "Create Combat Test");
        Undo.SetTransformParent(part.transform, parent, "Create Combat Test");
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (colliderKind == ColliderKind.None)
            Undo.DestroyObjectImmediate(collider);
        else if (colliderKind == ColliderKind.Trigger && collider is BoxCollider box)
            box.isTrigger = true;

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.enabled = visible;
        if (visible && material != null)
            renderer.sharedMaterial = material;

        return part;
    }

    enum ColliderKind
    {
        Solid,
        Trigger,
        None
    }
}
