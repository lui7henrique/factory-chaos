using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the ammo bench, cannon, and target when Play starts if the scene does not already have them.
/// </summary>
public static class CombatTestSpawner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (FactorySite.IsIndoor)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
            return;

        if (GameObject.Find("CombatTest") != null)
            return;

        Create();
    }

    static void Create()
    {
        Material ammoMaterial = MakeMaterial(new Color(0.95f, 0.62f, 0.12f), 0.12f);
        Material cannonMaterial = MakeMaterial(new Color(0.22f, 0.24f, 0.27f), 0.08f);
        Material targetMaterial = MakeMaterial(new Color(0.75f, 0.18f, 0.16f), 0.06f);

        GameObject root = new GameObject("CombatTest");
        root.SetActive(false);

        GameObject ammoPrefab = CreateAmmoTemplate(root.transform, ammoMaterial);
        CreateAmmoMachine(root.transform, ammoPrefab);
        CreateCannon(root.transform, cannonMaterial, ammoMaterial);
        CreateTarget(root.transform, targetMaterial, cannonMaterial);

        root.SetActive(true);
    }

    static GameObject CreateAmmoTemplate(Transform parent, Material material)
    {
        GameObject ammo = new GameObject("AmmoTemplate");
        ammo.transform.SetParent(parent, false);

        Rigidbody body = ammo.AddComponent<Rigidbody>();
        body.mass = 0.4f;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Item item = ammo.AddComponent<Item>();
        item.SetKind(ItemKind.Ammo);
        AmmoVisual.ApplyRuntime(ammo.transform);
        ammo.SetActive(false);
        return ammo;
    }

    static void CreateAmmoMachine(Transform parent, GameObject ammoPrefab)
    {
        GameObject root = new GameObject("AmmoMachine");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(6f, 0f, -1f);
        root.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

        GameObject input = new GameObject("Input");
        input.transform.SetParent(root.transform, false);
        BoxCollider box = input.AddComponent<BoxCollider>();
        box.isTrigger = true;
        AmmoMachine machine = input.AddComponent<AmmoMachine>();

        GameObject output = new GameObject("Output");
        output.transform.SetParent(root.transform, false);

        AmmoMachineVisual.Rebuild(root.transform, AmmoMachineVisual.RuntimePalette());
        Transform lens = root.transform.Find("Visuals/StatusLight/Lens");
        machine.Configure(output.transform, ammoPrefab, lens != null ? lens.GetComponent<Renderer>() : null);
    }

    static void CreateCannon(Transform parent, Material bodyMaterial, Material ammoMaterial)
    {
        GameObject root = new GameObject("Cannon");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(6f, 0f, 3.5f);

        CannonVisual.Rebuild(root.transform, CannonVisual.RuntimePalette(), CannonVisual.BuildMeshes());

        CannonController controller = root.GetComponent<CannonController>();
        if (controller == null)
            controller = root.AddComponent<CannonController>();

        Transform yaw = root.transform.Find("YawPivot");
        Transform pitch = yaw != null ? yaw.Find("PitchPivot") : null;
        Transform muzzle = pitch != null ? pitch.Find("MuzzlePoint") : null;
        Transform sight = pitch != null ? pitch.Find("Sight") : null;
        controller.Configure(yaw, pitch, muzzle, sight, ammoMaterial);
    }

    static void CreateTarget(Transform parent, Material boardMaterial, Material baseMaterial)
    {
        GameObject root = new GameObject("Target");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(6f, 0f, 9.2f);

        CreatePart(root.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), Quaternion.identity, new Vector3(1.6f, 0.3f, 1.2f), baseMaterial, true, false);
        GameObject board = CreatePart(root.transform, "Board", PrimitiveType.Cube, new Vector3(0f, 1.35f, 0f), Quaternion.identity, new Vector3(1.3f, 2.1f, 0.4f), boardMaterial, true, false);
        root.AddComponent<TargetDummy>().Configure(board.GetComponent<Renderer>());
    }

    static GameObject CreatePart(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material,
        bool visible,
        bool trigger)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (!visible && !trigger && collider != null)
            Object.Destroy(collider);
        else if (trigger && collider is BoxCollider box)
            box.isTrigger = true;

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
            if (visible && material != null)
                renderer.sharedMaterial = material;
        }

        return part;
    }

    static Material MakeMaterial(Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        return material;
    }
}
