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
        GameObject ammo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ammo.name = "AmmoTemplate";
        ammo.transform.SetParent(parent, false);
        ammo.transform.localScale = new Vector3(0.16f, 0.16f, 0.42f);

        MeshRenderer renderer = ammo.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Rigidbody body = ammo.AddComponent<Rigidbody>();
        body.mass = 0.4f;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Item item = ammo.AddComponent<Item>();
        item.SetKind(ItemKind.Ammo);
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

        CreatePart(root.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), Quaternion.identity, new Vector3(1.3f, 0.7f, 1.8f), bodyMaterial, true, false);

        GameObject yaw = new GameObject("YawPivot");
        yaw.transform.SetParent(root.transform, false);
        yaw.transform.localPosition = new Vector3(0f, 0.78f, 0.1f);

        GameObject pitch = new GameObject("PitchPivot");
        pitch.transform.SetParent(yaw.transform, false);

        CreatePart(pitch.transform, "Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.75f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.28f, 0.65f, 0.28f), bodyMaterial, true, false);

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(pitch.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 1.5f);

        GameObject sight = new GameObject("Sight");
        sight.transform.SetParent(pitch.transform, false);
        sight.transform.localPosition = new Vector3(0f, 0.32f, -0.9f);

        GameObject input = CreatePart(root.transform, "Input", PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.75f), Quaternion.identity, new Vector3(0.85f, 0.45f, 0.7f), ammoMaterial, false, true);
        CreatePart(root.transform, "Hopper", PrimitiveType.Cube, new Vector3(0f, 0.95f, -0.75f), Quaternion.identity, new Vector3(0.7f, 0.08f, 0.55f), ammoMaterial, true, false);

        root.AddComponent<CannonController>().Configure(yaw.transform, pitch.transform, muzzle.transform, sight.transform, ammoMaterial);
        input.AddComponent<CannonLoader>();
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
