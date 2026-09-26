using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConveyorMenu
{
    const string BeltMaterialPath = "Assets/Materials/Conveyor.mat";
    const string ArrowMaterialPath = "Assets/Materials/ConveyorArrow.mat";

    const float BeltWidth = 1.2f;
    const float BeltThickness = 0.2f;
    const float BeltLength = 4f;
    const float SurfaceY = 0.2f;
    const float LandingInset = 0.6f;

    [MenuItem("GameObject/Factory Chaos/Create Conveyor")]
    static void CreateConveyor()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Create the conveyor in edit mode.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Conveyor");

        GameObject root = new GameObject("Conveyor");
        Undo.RegisterCreatedObjectUndo(root, "Create Conveyor");
        Place(root.transform);

        Material beltMaterial = AssetDatabase.LoadAssetAtPath<Material>(BeltMaterialPath);
        Material arrowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArrowMaterialPath);

        CreatePart(root.transform, "Base", new Vector3(0f, BeltThickness * 0.5f, 0f), new Vector3(BeltWidth, BeltThickness, BeltLength), beltMaterial, ColliderKind.Solid, true);

        GameObject zone = CreatePart(
            root.transform,
            "Zone",
            new Vector3(0f, SurfaceY + 0.4f, 0f),
            new Vector3(BeltWidth, 0.8f, BeltLength),
            beltMaterial,
            ColliderKind.Trigger,
            false);
        Undo.AddComponent<ConveyorBelt>(zone);

        Transform direction = CreateEmpty(root.transform, "Direction");
        CreatePart(direction, "Shaft", new Vector3(0.72f, SurfaceY + 0.04f, -0.2f), new Vector3(0.08f, 0.08f, 1.6f), arrowMaterial, ColliderKind.None, true);
        CreatePart(direction, "Head", new Vector3(0.72f, SurfaceY + 0.04f, 0.75f), new Vector3(0.24f, 0.08f, 0.24f), arrowMaterial, ColliderKind.None, true);

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = root;
    }

    public static void CreateConveyorBatch()
    {
        CreateConveyor();
        EditorSceneManager.SaveOpenScenes();
    }

    static void Place(Transform root)
    {
        GameObject output = GameObject.Find("Output");
        if (output == null)
        {
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return;
        }

        Vector3 forward = output.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 landing = output.transform.position;
        landing.y = 0f;
        Vector3 center = landing - forward * LandingInset + forward * (BeltLength * 0.5f);
        root.SetPositionAndRotation(center, Quaternion.LookRotation(forward, Vector3.up));
    }

    static Transform CreateEmpty(Transform parent, string name)
    {
        GameObject empty = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(empty, "Create Conveyor");
        Undo.SetTransformParent(empty.transform, parent, "Create Conveyor");
        empty.transform.localPosition = Vector3.zero;
        empty.transform.localRotation = Quaternion.identity;
        empty.transform.localScale = Vector3.one;
        return empty.transform;
    }

    static GameObject CreatePart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, ColliderKind colliderKind, bool visible)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        Undo.RegisterCreatedObjectUndo(part, "Create Conveyor");
        Undo.SetTransformParent(part.transform, parent, "Create Conveyor");
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
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
