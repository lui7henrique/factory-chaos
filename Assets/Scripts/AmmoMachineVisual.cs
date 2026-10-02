using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the low-poly ammo press. Gameplay stays on the Input trigger and Output point.
/// </summary>
public static class AmmoMachineVisual
{
    public const string VisualsName = "Visuals";

    public static Action<GameObject> Created;
    public static Action<GameObject> Destroyed;

    static Mesh cylinder8;
    static Mesh cylinder6;
    static Mesh gem;

    public struct Palette
    {
        public Material structure;
        public Material panel;
        public Material accent;
        public Material metal;
        public Material belt;
        public Material lamp;
    }

    public static Palette RuntimePalette()
    {
        return new Palette
        {
            structure = MakeMaterial(new Color(0.204f, 0.227f, 0.251f), 0.06f, false),
            panel = MakeMaterial(new Color(0.224f, 0.482f, 0.490f), 0.06f, false),
            accent = MakeMaterial(new Color(0.957f, 0.745f, 0.196f), 0.08f, false),
            metal = MakeMaterial(new Color(0.522f, 0.553f, 0.588f), 0.1f, false),
            belt = MakeMaterial(new Color(0.145f, 0.161f, 0.180f), 0.04f, false),
            lamp = MakeMaterial(new Color(0.349f, 0.937f, 0.380f), 0.15f, true)
        };
    }

    const string ModelResource = "AmmoPress/MeshyPress";
    const string MaterialResource = "AmmoPress/PressSurface";
    const float ModelPitch = -90f;
    const float ModelYaw = 0f;
    const float TargetHeight = 2.15f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureImported()
    {
        if (Resources.Load<GameObject>(ModelResource) == null)
            return;

        AmmoMachine[] machines = UnityEngine.Object.FindObjectsByType<AmmoMachine>(FindObjectsInactive.Exclude);
        for (int i = 0; i < machines.Length; i++)
        {
            Transform root = StationRoot(machines[i].transform);
            Transform existing = root.Find(VisualsName + "/Model");
            if (existing != null)
            {
                FitToMachine(existing, TargetHeight);
                Collider[] modelColliders = existing.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < modelColliders.Length; c++)
                    modelColliders[c].enabled = false;
                PlaceImportedPoints(root);
                ConveyorVisual.AlignToMachines();
                continue;
            }

            Rebuild(root, RuntimePalette());
        }
    }

    public static void Rebuild(Transform machineRoot, Palette palette)
    {
        if (machineRoot == null)
            return;

        palette = Fill(palette);
        machineRoot.localScale = Vector3.one;

        RemoveLegacyPlaceholders(machineRoot);
        Transform old = machineRoot.Find(VisualsName);
        if (old != null)
            DestroyObject(old.gameObject);
        Transform solids = machineRoot.Find("PressSolids");
        if (solids != null)
            DestroyObject(solids.gameObject);

        if (TryBuildImported(machineRoot))
            return;

        Transform visuals = Empty(machineRoot, VisualsName);
        BuildBase(visuals, palette);
        BuildFrame(visuals, palette);
        Transform pressHead = BuildPress(visuals, palette);
        BuildHopper(visuals, palette);
        BuildBelt(visuals, palette);
        Renderer lens = BuildLamp(visuals, palette);
        BuildBolts(visuals, palette);

        Transform input = EnsureInput(machineRoot);
        Transform output = EnsureOutput(machineRoot);
        input.localPosition = new Vector3(-1.22f, 1.05f, 0f);
        input.localRotation = Quaternion.identity;
        input.localScale = new Vector3(0.62f, 0.42f, 0.5f);
        output.localPosition = new Vector3(1.42f, 0.78f, 0f);
        output.localRotation = Quaternion.identity;
        output.localScale = Vector3.one;

        AmmoMachine machine = machineRoot.GetComponentInChildren<AmmoMachine>(true);
        if (machine != null)
        {
            machine.SetStatusLamp(lens);
            AmmoPressMotion motion = pressHead.gameObject.AddComponent<AmmoPressMotion>();
            motion.Bind(machine, 1.58f, 1.18f);
        }
    }

    static bool TryBuildImported(Transform machineRoot)
    {
        GameObject prefab = Resources.Load<GameObject>(ModelResource);
        if (prefab == null)
            return false;

        Transform visuals = Empty(machineRoot, VisualsName);
        GameObject model = UnityEngine.Object.Instantiate(prefab, visuals);
        model.name = "Model";
        Track(model);
        FitToMachine(model.transform, TargetHeight);

        Material surface = Resources.Load<Material>(MaterialResource);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (surface != null)
                renderers[i].sharedMaterial = surface;
        }

        Collider[] modelColliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < modelColliders.Length; i++)
            modelColliders[i].enabled = false;

        Transform solids = Empty(machineRoot, "PressSolids");
        Solid(solids, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(1.25f, 2f, 1.45f));
        Solid(solids, "OutTray", new Vector3(-1.05f, 0.2f, 0f), new Vector3(0.55f, 0.28f, 0.55f));
        PlaceImportedPoints(machineRoot);
        ConveyorVisual.AlignToMachines();
        return true;
    }

    static void PlaceImportedPoints(Transform machineRoot)
    {
        Transform input = EnsureInput(machineRoot);
        Transform output = EnsureOutput(machineRoot);
        input.localPosition = new Vector3(0.15f, 0.95f, 0.15f);
        input.localRotation = Quaternion.identity;
        input.localScale = new Vector3(1.2f, 0.9f, 1.15f);
        output.localPosition = new Vector3(0.15f, 0.82f, -1.45f);
        output.localRotation = Quaternion.Euler(0f, 180f, 0f);
        output.localScale = Vector3.one;
        EnsureChute(output);

        Transform tray = machineRoot.Find("PressSolids/OutTray");
        if (tray != null)
            tray.localPosition = new Vector3(0.15f, 0.38f, -1.2f);
    }

    static void EnsureChute(Transform output)
    {
        Transform mark = output.Find("Chute");
        if (mark == null)
        {
            GameObject chute = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chute.name = "Chute";
            chute.transform.SetParent(output, false);
            Collider collider = chute.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);
            mark = chute.transform;
            Renderer renderer = chute.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = MakeMaterial(new Color(0.835f, 0.651f, 0.227f), 0.08f, false);
        }

        mark.localPosition = new Vector3(0f, -0.16f, 0.15f);
        mark.localRotation = Quaternion.identity;
        mark.localScale = new Vector3(0.62f, 0.08f, 0.42f);
    }

    static Transform StationRoot(Transform machine)
    {
        if (machine.parent != null && machine.name != "AmmoMachine")
            return machine.parent;
        return machine;
    }

    static void FitToMachine(Transform model, float targetHeight)
    {
        model.localRotation = Quaternion.Euler(ModelPitch, ModelYaw, 0f);
        model.localScale = Vector3.one;
        model.localPosition = Vector3.zero;
        if (!TryParentBounds(model, out Bounds bounds))
            return;

        float scale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
        model.localScale = Vector3.one * scale;
        if (!TryParentBounds(model, out bounds))
            return;

        model.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    static bool TryParentBounds(Transform model, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        Transform space = model.parent != null ? model.parent : model;
        MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
        bool found = false;
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
                continue;

            Vector3 center = mesh.bounds.center;
            Vector3 extents = mesh.bounds.extents;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = center + new Vector3(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);
                Vector3 local = space.InverseTransformPoint(filters[i].transform.TransformPoint(point));
                if (!found)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }
        }

        return found && bounds.size.y > 0.001f;
    }

    static void Solid(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        BoxCollider box = part.AddComponent<BoxCollider>();
        box.size = size;
        Track(part);
    }

    static void BuildBase(Transform visuals, Palette palette)
    {
        Transform group = Empty(visuals, "Base");
        Cube(group, "Chassis", new Vector3(0.08f, 0.5f, 0f), Quaternion.identity, new Vector3(1.55f, 0.42f, 1.05f), palette.structure, true);
        Cube(group, "FrontPanel", new Vector3(0.08f, 0.52f, 0.5f), Quaternion.identity, new Vector3(1.28f, 0.48f, 0.08f), palette.panel, true);
        Cube(group, "FrontStripe", new Vector3(0.08f, 0.34f, 0.55f), Quaternion.identity, new Vector3(1.34f, 0.08f, 0.04f), palette.accent, false);
        Cube(group, "Bed", new Vector3(0.05f, 0.78f, 0f), Quaternion.identity, new Vector3(0.72f, 0.1f, 0.62f), palette.metal, true);

        Vector3[] feet = { new Vector3(-0.52f, 0f, -0.42f), new Vector3(-0.52f, 0f, 0.42f), new Vector3(0.68f, 0f, -0.42f), new Vector3(0.68f, 0f, 0.42f) };
        for (int i = 0; i < feet.Length; i++)
        {
            Cube(group, "Foot" + i, feet[i] + new Vector3(0f, 0.05f, 0f), Quaternion.identity, new Vector3(0.28f, 0.1f, 0.28f), palette.accent, true);
            Cube(group, "Post" + i, feet[i] + new Vector3(0f, 0.24f, 0f), Quaternion.identity, new Vector3(0.18f, 0.3f, 0.18f), palette.structure, true);
        }
    }

    static void BuildFrame(Transform visuals, Palette palette)
    {
        Transform group = Empty(visuals, "Frame");
        Cube(group, "ColumnL", new Vector3(-0.38f, 1.48f, 0f), Quaternion.identity, new Vector3(0.28f, 1.28f, 0.32f), palette.structure, true);
        Cube(group, "ColumnR", new Vector3(0.5f, 1.48f, 0f), Quaternion.identity, new Vector3(0.28f, 1.28f, 0.32f), palette.structure, true);
        Cube(group, "ShoulderL", new Vector3(-0.16f, 2.02f, 0f), Quaternion.Euler(0f, 0f, 38f), new Vector3(0.62f, 0.2f, 0.3f), palette.structure, true);
        Cube(group, "ShoulderR", new Vector3(0.28f, 2.02f, 0f), Quaternion.Euler(0f, 0f, -38f), new Vector3(0.62f, 0.2f, 0.3f), palette.structure, true);
        Cube(group, "Arch", new Vector3(0.06f, 2.16f, 0f), Quaternion.identity, new Vector3(1.2f, 0.22f, 0.34f), palette.structure, true);
    }

    static Transform BuildPress(Transform visuals, Palette palette)
    {
        Transform group = Empty(visuals, "Press");
        Cube(group, "Housing", new Vector3(0.06f, 1.9f, 0f), Quaternion.identity, new Vector3(0.62f, 0.28f, 0.46f), palette.panel, false);
        Cube(group, "Band", new Vector3(0.06f, 1.72f, 0f), Quaternion.identity, new Vector3(0.72f, 0.08f, 0.5f), palette.accent, false);
        Faceted(group, "Cap", Cylinder8(), new Vector3(0.06f, 2.28f, 0f), Quaternion.identity, new Vector3(0.36f, 0.1f, 0.36f), palette.accent, false);

        Transform head = Empty(group, "PressHead", false);
        head.localPosition = new Vector3(0.06f, 1.58f, 0f);
        Faceted(head, "Rod", Cylinder8(), new Vector3(0f, 0.16f, 0f), Quaternion.identity, new Vector3(0.16f, 0.42f, 0.16f), palette.metal, false);
        Cube(head, "Platen", new Vector3(0f, -0.08f, 0f), Quaternion.identity, new Vector3(0.55f, 0.1f, 0.48f), palette.metal, false);
        Faceted(head, "Foot", Cylinder8(), new Vector3(0f, -0.18f, 0f), Quaternion.identity, new Vector3(0.22f, 0.12f, 0.22f), palette.accent, false);
        Track(head.gameObject);
        return head;
    }

    static void BuildHopper(Transform visuals, Palette palette)
    {
        Transform group = Empty(visuals, "Input");
        Cube(group, "Floor", new Vector3(-1.22f, 0.8f, 0f), Quaternion.identity, new Vector3(0.78f, 0.1f, 0.7f), palette.panel, true);
        Cube(group, "Rim", new Vector3(-1.22f, 0.86f, 0f), Quaternion.identity, new Vector3(0.9f, 0.06f, 0.82f), palette.accent, false);
        Cube(group, "WallBack", new Vector3(-1.62f, 1.08f, 0f), Quaternion.identity, new Vector3(0.1f, 0.48f, 0.9f), palette.panel, true);
        Cube(group, "WallFar", new Vector3(-1.22f, 1.08f, 0.38f), Quaternion.identity, new Vector3(0.78f, 0.48f, 0.1f), palette.panel, true);
        Cube(group, "WallNear", new Vector3(-1.22f, 1.08f, -0.38f), Quaternion.identity, new Vector3(0.78f, 0.48f, 0.1f), palette.panel, true);
        Cube(group, "LipBack", new Vector3(-1.62f, 1.34f, 0f), Quaternion.identity, new Vector3(0.12f, 0.08f, 0.96f), palette.accent, false);
        Cube(group, "LipFar", new Vector3(-1.22f, 1.34f, 0.44f), Quaternion.identity, new Vector3(0.86f, 0.08f, 0.08f), palette.accent, false);
        Cube(group, "LipNear", new Vector3(-1.22f, 1.34f, -0.44f), Quaternion.identity, new Vector3(0.86f, 0.08f, 0.08f), palette.accent, false);
        Cube(group, "Channel", new Vector3(-0.68f, 0.72f, 0f), Quaternion.Euler(0f, 0f, 12f), new Vector3(0.42f, 0.08f, 0.36f), palette.structure, true);
    }

    static void BuildBelt(Transform visuals, Palette palette)
    {
        Transform group = Empty(visuals, "Output");
        Cube(group, "Belt", new Vector3(1.15f, 0.6f, 0f), Quaternion.Euler(0f, 0f, -5f), new Vector3(1.15f, 0.06f, 0.4f), palette.belt, true);
        Cube(group, "RailFar", new Vector3(1.15f, 0.66f, 0.24f), Quaternion.identity, new Vector3(1.15f, 0.1f, 0.08f), palette.panel, true);
        Cube(group, "RailNear", new Vector3(1.15f, 0.66f, -0.24f), Quaternion.identity, new Vector3(1.15f, 0.1f, 0.08f), palette.panel, true);
        Cube(group, "GuardIn", new Vector3(0.58f, 0.68f, 0f), Quaternion.identity, new Vector3(0.1f, 0.22f, 0.62f), palette.accent, true);
        Cube(group, "GuardOut", new Vector3(1.7f, 0.64f, 0f), Quaternion.identity, new Vector3(0.1f, 0.18f, 0.62f), palette.accent, true);
        Faceted(group, "RollerIn", Cylinder8(), new Vector3(0.78f, 0.54f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.14f, 0.34f, 0.14f), palette.metal, false);
        Faceted(group, "RollerOut", Cylinder8(), new Vector3(1.48f, 0.5f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.14f, 0.34f, 0.14f), palette.metal, false);
        Cube(group, "LegFar", new Vector3(1.35f, 0.26f, 0.32f), Quaternion.identity, new Vector3(0.12f, 0.52f, 0.12f), palette.structure, true);
        Cube(group, "LegNear", new Vector3(1.35f, 0.26f, -0.32f), Quaternion.identity, new Vector3(0.12f, 0.52f, 0.12f), palette.structure, true);
        Cube(group, "ShoeFar", new Vector3(1.35f, 0.04f, 0.32f), Quaternion.identity, new Vector3(0.22f, 0.08f, 0.22f), palette.accent, true);
        Cube(group, "ShoeNear", new Vector3(1.35f, 0.04f, -0.32f), Quaternion.identity, new Vector3(0.22f, 0.08f, 0.22f), palette.accent, true);
    }

    static Renderer BuildLamp(Transform visuals, Palette palette)
    {
        Transform group = Empty(visuals, "StatusLight");
        Cube(group, "Arm", new Vector3(0.72f, 1.72f, 0.22f), Quaternion.identity, new Vector3(0.28f, 0.08f, 0.16f), palette.structure, false);
        Cube(group, "Base", new Vector3(0.86f, 1.8f, 0.28f), Quaternion.identity, new Vector3(0.14f, 0.1f, 0.14f), palette.structure, false);
        GameObject lens = Faceted(group, "Lens", Gem(), new Vector3(0.86f, 1.96f, 0.28f), Quaternion.identity, new Vector3(0.16f, 0.2f, 0.16f), palette.lamp, false);
        return lens.GetComponent<Renderer>();
    }

    static void BuildBolts(Transform visuals, Palette palette)
    {
        Transform group = Empty(visuals, "Decoration");
        Vector3[] bolts =
        {
            new Vector3(-0.4f, 0.62f, 0.56f),
            new Vector3(0.55f, 0.62f, 0.56f),
            new Vector3(-0.4f, 0.4f, 0.56f),
            new Vector3(0.55f, 0.4f, 0.56f),
            new Vector3(-0.2f, 2.16f, 0.2f),
            new Vector3(0.32f, 2.16f, 0.2f)
        };

        for (int i = 0; i < bolts.Length; i++)
            Faceted(group, "Bolt" + i, Cylinder6(), bolts[i], Quaternion.identity, new Vector3(0.08f, 0.04f, 0.08f), palette.metal, false);
    }

    static Transform EnsureInput(Transform machineRoot)
    {
        Transform input = machineRoot.Find("Input");
        if (input == null)
        {
            GameObject created = new GameObject("Input");
            created.transform.SetParent(machineRoot, false);
            created.AddComponent<BoxCollider>();
            if (created.GetComponent<AmmoMachine>() == null)
                created.AddComponent<AmmoMachine>();
            Track(created);
            input = created.transform;
        }

        MeshRenderer renderer = input.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;

        BoxCollider box = input.GetComponent<BoxCollider>();
        if (box == null)
            box = input.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = Vector3.one;
        return input;
    }

    static Transform EnsureOutput(Transform machineRoot)
    {
        Transform output = machineRoot.Find("Output");
        if (output != null)
            return output;

        return Empty(machineRoot, "Output");
    }

    static void RemoveLegacyPlaceholders(Transform machineRoot)
    {
        string[] names = { "Body", "Chute", "StatusLamp" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = machineRoot.Find(names[i]);
            if (child == null || child.GetComponent<AmmoMachine>() != null)
                continue;

            if (child.GetComponent<MonoBehaviour>() != null)
                continue;

            DestroyObject(child.gameObject);
        }
    }

    static Transform Empty(Transform parent, string name, bool track = true)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (track)
            Track(go);
        return go.transform;
    }

    static GameObject Cube(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool solid)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation;
        part.transform.localScale = scale;

        if (!solid)
            RemoveCollider(part);

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        Track(part);
        return part;
    }

    static GameObject Faceted(Transform parent, string name, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool solid)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation;
        part.transform.localScale = scale;

        MeshFilter filter = part.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;

        if (solid)
        {
            BoxCollider box = part.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;
        }

        Track(part);
        return part;
    }

    static void RemoveCollider(GameObject part)
    {
        Collider collider = part.GetComponent<Collider>();
        if (collider == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(collider);
        else
            UnityEngine.Object.DestroyImmediate(collider);
    }

    static void DestroyObject(GameObject go)
    {
        if (Destroyed != null)
            Destroyed(go);
        else if (Application.isPlaying)
            UnityEngine.Object.Destroy(go);
        else
            UnityEngine.Object.DestroyImmediate(go);
    }

    static void Track(GameObject go)
    {
        Created?.Invoke(go);
    }

    static Palette Fill(Palette palette)
    {
        Palette fallback = default;
        bool needsFallback = palette.structure == null || palette.panel == null || palette.accent == null
            || palette.metal == null || palette.belt == null || palette.lamp == null;
        if (needsFallback)
            fallback = RuntimePalette();

        if (palette.structure == null) palette.structure = fallback.structure;
        if (palette.panel == null) palette.panel = fallback.panel;
        if (palette.accent == null) palette.accent = fallback.accent;
        if (palette.metal == null) palette.metal = fallback.metal;
        if (palette.belt == null) palette.belt = fallback.belt;
        if (palette.lamp == null) palette.lamp = fallback.lamp;
        return palette;
    }

    static Material MakeMaterial(Color color, float smoothness, bool emissive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.45f);
        }

        return material;
    }

    static Mesh Cylinder8()
    {
        if (cylinder8 == null)
            cylinder8 = BuildCylinder(8);
        return cylinder8;
    }

    static Mesh Cylinder6()
    {
        if (cylinder6 == null)
            cylinder6 = BuildCylinder(6);
        return cylinder6;
    }

    static Mesh Gem()
    {
        if (gem == null)
            gem = BuildGem(6);
        return gem;
    }

    static Mesh BuildCylinder(int sides)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        const float height = 0.5f;
        Vector3 top = new Vector3(0f, height, 0f);
        Vector3 bottom = new Vector3(0f, -height, 0f);

        for (int i = 0; i < sides; i++)
        {
            float a0 = (i / (float)sides) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
            Vector3 ring0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
            Vector3 ring1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
            Vector3 top0 = ring0 + Vector3.up * height;
            Vector3 top1 = ring1 + Vector3.up * height;
            Vector3 bottom0 = ring0 - Vector3.up * height;
            Vector3 bottom1 = ring1 - Vector3.up * height;

            Vector3 outward = ring0 + ring1;
            AddOutward(vertices, triangles, top0, bottom1, bottom0, outward);
            AddOutward(vertices, triangles, top0, top1, bottom1, outward);
            AddOutward(vertices, triangles, top, top1, top0, Vector3.up);
            AddOutward(vertices, triangles, bottom, bottom0, bottom1, Vector3.down);
        }

        return Finish(sides == 8 ? "Cylinder8" : "Cylinder6", vertices, triangles);
    }

    static Mesh BuildGem(int sides)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        Vector3 top = new Vector3(0f, 0.55f, 0f);
        Vector3 bottom = new Vector3(0f, -0.35f, 0f);

        for (int i = 0; i < sides; i++)
        {
            float a0 = (i / (float)sides) * Mathf.PI * 2f + 0.2f;
            float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f + 0.2f;
            float radius = i % 2 == 0 ? 0.5f : 0.38f;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * (i % 2 == 0 ? 0.38f : 0.5f), 0f, Mathf.Sin(a1) * (i % 2 == 0 ? 0.38f : 0.5f));
            AddOutward(vertices, triangles, p0, top, p1, (p0 + p1) * 0.5f + Vector3.up);
            AddOutward(vertices, triangles, p0, p1, bottom, (p0 + p1) * 0.5f + Vector3.down);
        }

        return Finish("StatusGem", vertices, triangles);
    }

    static void AddOutward(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f)
        {
            Vector3 swap = b;
            b = c;
            c = swap;
        }

        AddTriangle(vertices, triangles, a, b, c);
    }

    static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
    }

    static Mesh Finish(string meshName, List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh { name = meshName };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
