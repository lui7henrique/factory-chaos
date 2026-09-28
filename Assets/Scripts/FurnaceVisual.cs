using System;
using UnityEngine;

/// <summary>
/// Low-poly furnace shell. Gameplay stays on the existing Input trigger and Output point.
/// Front faces local +X, toward the player. The ingot tray faces local +Z, toward the belt.
/// </summary>
public static class FurnaceVisual
{
    public static Action<GameObject> Created;
    public static Action<GameObject> Destroyed;

    public struct Palette
    {
        public Material body;
        public Material structure;
        public Material tray;
        public Material accent;
        public Material interior;
        public Material flame;
        public Material lamp;
        public Material bore;
    }

    public struct MeshSet
    {
        public Mesh roof;
        public Mesh chimney;
        public Mesh cap;
        public Mesh flame;
    }

    public static Palette RuntimePalette()
    {
        return new Palette
        {
            body = Make(new Color(0.224f, 0.482f, 0.490f), 0.08f, false),
            structure = Make(new Color(0.204f, 0.227f, 0.251f), 0.08f, false),
            tray = Make(new Color(0.522f, 0.553f, 0.588f), 0.1f, false),
            accent = Make(new Color(0.957f, 0.745f, 0.196f), 0.1f, false),
            interior = Make(new Color(0.953f, 0.416f, 0.086f), 0.12f, true),
            flame = Make(new Color(1f, 0.710f, 0.180f), 0.12f, true),
            lamp = Make(new Color(0.349f, 0.937f, 0.380f), 0.15f, true),
            bore = Make(new Color(0.09f, 0.106f, 0.125f), 0.04f, false)
        };
    }

    static Material Make(Color color, float smoothness, bool emissive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        if (!emissive)
            return material;

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 0.65f);
        return material;
    }

    public static MeshSet BuildMeshes()
    {
        return new MeshSet
        {
            roof = FurnaceMesh.Roof(),
            chimney = FurnaceMesh.Chimney(),
            cap = FurnaceMesh.Cap(),
            flame = FurnaceMesh.Flame()
        };
    }

    public static void Rebuild(Transform machineRoot, Palette palette, MeshSet meshes)
    {
        if (machineRoot == null)
            return;

        if (meshes.roof == null)
            meshes = BuildMeshes();

        RemoveGenerated(machineRoot);
        HidePlaceholder(machineRoot);

        Transform visual = Empty(machineRoot, "Visual");
        Transform solids = Empty(machineRoot, "SolidColliders");
        BuildBase(visual, solids, palette);
        BuildShell(visual, solids, palette, meshes);
        BuildMouth(visual, palette);
        BuildFrontTray(visual, solids, palette);
        BuildSideExit(visual, solids, palette);
        BuildFire(machineRoot, palette, meshes);
        BuildLamp(machineRoot, palette);
        PlaceInput(machineRoot);
        PlaceOutput(machineRoot);
    }

    public static Renderer FindLens(Transform machineRoot)
    {
        Transform lens = machineRoot.Find("StatusLight/Lens");
        return lens != null ? lens.GetComponent<Renderer>() : null;
    }

    static void BuildBase(Transform visual, Transform solids, Palette palette)
    {
        Cube(visual, "FootBackLeft", new Vector3(-0.42f, 0.12f, -0.5f), Quaternion.identity, new Vector3(0.36f, 0.24f, 0.34f), palette.structure);
        Cube(visual, "FootBackRight", new Vector3(-0.42f, 0.12f, 0.5f), Quaternion.identity, new Vector3(0.36f, 0.24f, 0.34f), palette.structure);
        Cube(visual, "FootFrontLeft", new Vector3(0.38f, 0.12f, -0.5f), Quaternion.identity, new Vector3(0.36f, 0.24f, 0.34f), palette.structure);
        Cube(visual, "FootFrontRight", new Vector3(0.38f, 0.12f, 0.5f), Quaternion.identity, new Vector3(0.36f, 0.24f, 0.34f), palette.structure);
        Solid(solids, "FootBackLeft", new Vector3(-0.42f, 0.12f, -0.5f), new Vector3(0.36f, 0.24f, 0.34f));
        Solid(solids, "FootBackRight", new Vector3(-0.42f, 0.12f, 0.5f), new Vector3(0.36f, 0.24f, 0.34f));
        Solid(solids, "FootFrontLeft", new Vector3(0.38f, 0.12f, -0.5f), new Vector3(0.36f, 0.24f, 0.34f));
        Solid(solids, "FootFrontRight", new Vector3(0.38f, 0.12f, 0.5f), new Vector3(0.36f, 0.24f, 0.34f));

        Cube(visual, "Plinth", new Vector3(-0.02f, 0.28f, 0f), Quaternion.identity, new Vector3(1.05f, 0.12f, 1.2f), palette.structure);
        Solid(solids, "Plinth", new Vector3(-0.02f, 0.28f, 0f), new Vector3(1.05f, 0.12f, 1.2f));
    }

    static void BuildShell(Transform visual, Transform solids, Palette palette, MeshSet meshes)
    {
        Cube(visual, "Back", new Vector3(-0.38f, 0.88f, 0f), Quaternion.identity, new Vector3(0.34f, 1.02f, 1.28f), palette.body);
        Cube(visual, "Left", new Vector3(0.02f, 0.9f, -0.56f), Quaternion.identity, new Vector3(0.78f, 0.98f, 0.24f), palette.body);
        Cube(visual, "RightUpper", new Vector3(0.02f, 1.18f, 0.56f), Quaternion.identity, new Vector3(0.78f, 0.42f, 0.24f), palette.body);
        Cube(visual, "RightLower", new Vector3(-0.08f, 0.5f, 0.56f), Quaternion.identity, new Vector3(0.5f, 0.28f, 0.24f), palette.body);
        Solid(solids, "Back", new Vector3(-0.38f, 0.88f, 0f), new Vector3(0.34f, 1.02f, 1.28f));
        Solid(solids, "Left", new Vector3(0.02f, 0.9f, -0.56f), new Vector3(0.78f, 0.98f, 0.24f));
        Solid(solids, "RightUpper", new Vector3(0.02f, 1.18f, 0.56f), new Vector3(0.78f, 0.42f, 0.24f));
        Solid(solids, "RightLower", new Vector3(-0.08f, 0.5f, 0.56f), new Vector3(0.5f, 0.28f, 0.24f));

        MeshPart(visual, "Roof", meshes.roof, new Vector3(0.02f, 1.36f, 0f), Quaternion.identity, Vector3.one, palette.body);
        Solid(solids, "Roof", new Vector3(0.02f, 1.36f, 0f), new Vector3(1.2f, 0.22f, 1.4f));

        Cube(visual, "ShoulderLeft", new Vector3(0.02f, 1.28f, -0.62f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.9f, 0.1f, 0.16f), palette.body);
        Cube(visual, "ShoulderRight", new Vector3(0.02f, 1.28f, 0.62f), Quaternion.identity, new Vector3(0.9f, 0.1f, 0.16f), palette.body);
        Cube(visual, "ShoulderFront", new Vector3(0.5f, 1.3f, 0f), Quaternion.Euler(0f, 0f, -28f), new Vector3(0.28f, 0.12f, 1.15f), palette.body);
        Cube(visual, "ShoulderBack", new Vector3(-0.48f, 1.3f, 0f), Quaternion.Euler(0f, 0f, 28f), new Vector3(0.28f, 0.12f, 1.15f), palette.body);

        MeshPart(visual, "CapLeft", meshes.cap, new Vector3(0.42f, 1.4f, -0.56f), Quaternion.identity, Vector3.one, palette.accent);
        MeshPart(visual, "CapRight", meshes.cap, new Vector3(0.42f, 1.4f, 0.56f), Quaternion.identity, Vector3.one, palette.accent);

        MeshPart(visual, "Chimney", meshes.chimney, new Vector3(-0.02f, 1.78f, -0.02f), Quaternion.identity, Vector3.one, palette.structure);
        Cube(visual, "ChimneyRim", new Vector3(-0.02f, 2.02f, -0.02f), Quaternion.identity, new Vector3(0.62f, 0.1f, 0.62f), palette.structure);
        Cube(visual, "ChimneyHole", new Vector3(-0.02f, 2.08f, -0.02f), Quaternion.identity, new Vector3(0.28f, 0.04f, 0.28f), palette.bore);
        Solid(solids, "Chimney", new Vector3(-0.02f, 1.78f, -0.02f), new Vector3(0.46f, 0.62f, 0.46f));
    }

    static void BuildMouth(Transform visual, Palette palette)
    {
        Cube(visual, "Sill", new Vector3(0.56f, 0.66f, 0f), Quaternion.identity, new Vector3(0.12f, 0.12f, 0.96f), palette.structure);
        Cube(visual, "Lintel", new Vector3(0.56f, 1.22f, 0f), Quaternion.identity, new Vector3(0.12f, 0.14f, 0.96f), palette.structure);
        Cube(visual, "JambLeft", new Vector3(0.56f, 0.94f, -0.4f), Quaternion.identity, new Vector3(0.12f, 0.48f, 0.14f), palette.structure);
        Cube(visual, "JambRight", new Vector3(0.56f, 0.94f, 0.4f), Quaternion.identity, new Vector3(0.12f, 0.48f, 0.14f), palette.structure);
        Cube(visual, "CornerLeft", new Vector3(0.58f, 1.12f, -0.28f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.1f, 0.12f, 0.16f), palette.structure);
        Cube(visual, "CornerRight", new Vector3(0.58f, 1.12f, 0.28f), Quaternion.identity, new Vector3(0.1f, 0.12f, 0.16f), palette.structure);
        Cube(visual, "CornerCutLeft", new Vector3(0.6f, 1.14f, -0.3f), Quaternion.Euler(0f, 45f, 0f), new Vector3(0.1f, 0.1f, 0.16f), palette.structure);
        Cube(visual, "CornerCutRight", new Vector3(0.6f, 1.14f, 0.3f), Quaternion.Euler(0f, -45f, 0f), new Vector3(0.1f, 0.1f, 0.16f), palette.structure);

        Cube(visual, "Interior", new Vector3(0.08f, 0.94f, 0f), Quaternion.identity, new Vector3(0.06f, 0.46f, 0.58f), palette.interior);
        Cube(visual, "InteriorFloor", new Vector3(0.28f, 0.7f, 0f), Quaternion.identity, new Vector3(0.36f, 0.04f, 0.52f), palette.interior);
    }

    static void BuildFrontTray(Transform visual, Transform solids, Palette palette)
    {
        Cube(visual, "InTray", new Vector3(0.94f, 0.72f, 0f), Quaternion.identity, new Vector3(0.62f, 0.08f, 0.82f), palette.tray);
        Cube(visual, "InLipLeft", new Vector3(0.94f, 0.8f, -0.38f), Quaternion.identity, new Vector3(0.56f, 0.1f, 0.06f), palette.tray);
        Cube(visual, "InLipRight", new Vector3(0.94f, 0.8f, 0.38f), Quaternion.identity, new Vector3(0.56f, 0.1f, 0.06f), palette.tray);
        Solid(solids, "InTray", new Vector3(0.94f, 0.72f, 0f), new Vector3(0.62f, 0.08f, 0.82f));
        Solid(solids, "InLipLeft", new Vector3(0.94f, 0.8f, -0.38f), new Vector3(0.56f, 0.1f, 0.06f));
        Solid(solids, "InLipRight", new Vector3(0.94f, 0.8f, 0.38f), new Vector3(0.56f, 0.1f, 0.06f));
    }

    static void BuildSideExit(Transform visual, Transform solids, Palette palette)
    {
        Cube(visual, "OutFrameTop", new Vector3(0.05f, 0.98f, 0.62f), Quaternion.identity, new Vector3(0.48f, 0.1f, 0.1f), palette.structure);
        Cube(visual, "OutFrameLeft", new Vector3(-0.18f, 0.78f, 0.62f), Quaternion.identity, new Vector3(0.1f, 0.32f, 0.1f), palette.structure);
        Cube(visual, "OutFrameRight", new Vector3(0.28f, 0.78f, 0.62f), Quaternion.identity, new Vector3(0.1f, 0.32f, 0.1f), palette.structure);
        Cube(visual, "OutGlow", new Vector3(0.05f, 0.78f, 0.5f), Quaternion.identity, new Vector3(0.28f, 0.2f, 0.04f), palette.interior);

        Cube(visual, "OutTray", new Vector3(0.08f, 0.7f, 1.08f), Quaternion.identity, new Vector3(0.82f, 0.08f, 0.62f), palette.tray);
        Cube(visual, "OutLipBack", new Vector3(-0.3f, 0.78f, 1.08f), Quaternion.identity, new Vector3(0.06f, 0.1f, 0.56f), palette.tray);
        Cube(visual, "OutLipFront", new Vector3(0.46f, 0.78f, 1.08f), Quaternion.identity, new Vector3(0.06f, 0.1f, 0.56f), palette.tray);
        Solid(solids, "OutTray", new Vector3(0.08f, 0.7f, 1.08f), new Vector3(0.82f, 0.08f, 0.62f));
        Solid(solids, "OutLipBack", new Vector3(-0.3f, 0.78f, 1.08f), new Vector3(0.06f, 0.1f, 0.56f));
        Solid(solids, "OutLipFront", new Vector3(0.46f, 0.78f, 1.08f), new Vector3(0.06f, 0.1f, 0.56f));
    }

    static Transform BuildFire(Transform machineRoot, Palette palette, MeshSet meshes)
    {
        Transform fire = Empty(machineRoot, "FireVisual");
        Flame(fire, "FlameA", meshes.flame, new Vector3(0.22f, 0.86f, -0.08f), 1.1f, palette.flame);
        Flame(fire, "FlameB", meshes.flame, new Vector3(0.26f, 0.96f, 0.08f), 0.85f, palette.flame);
        Flame(fire, "FlameC", meshes.flame, new Vector3(0.2f, 0.8f, 0.12f), 0.7f, palette.flame);
        return fire;
    }

    static Renderer BuildLamp(Transform machineRoot, Palette palette)
    {
        Transform light = Empty(machineRoot, "StatusLight");
        Cube(light, "Housing", new Vector3(0.58f, 0.98f, 0.5f), Quaternion.identity, new Vector3(0.1f, 0.16f, 0.14f), palette.structure);
        GameObject lens = Cube(light, "Lens", new Vector3(0.64f, 0.98f, 0.5f), Quaternion.identity, new Vector3(0.04f, 0.08f, 0.08f), palette.lamp);
        return lens.GetComponent<Renderer>();
    }

    static void PlaceInput(Transform machineRoot)
    {
        Transform input = machineRoot.Find("Input");
        if (input == null)
            return;

        input.localPosition = new Vector3(0.94f, 1.02f, 0f);
        input.localRotation = Quaternion.identity;
        input.localScale = Vector3.one;

        BoxCollider box = input.GetComponent<BoxCollider>();
        if (box == null)
            box = input.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = new Vector3(0.7f, 0.5f, 0.74f);

        Renderer renderer = input.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    static void PlaceOutput(Transform machineRoot)
    {
        Transform output = machineRoot.Find("Output");
        if (output == null)
            return;

        output.localPosition = new Vector3(0.08f, 0.9f, 1.08f);
        output.localRotation = Quaternion.identity;
        output.localScale = Vector3.one;
    }

    static void HidePlaceholder(Transform machineRoot)
    {
        Transform body = machineRoot.Find("Body");
        if (body == null)
            return;

        Renderer renderer = body.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;

        Collider collider = body.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
    }

    static void RemoveGenerated(Transform machineRoot)
    {
        string[] names = { "Visual", "Visuals", "SolidColliders", "StatusLight", "FireVisual" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = machineRoot.Find(names[i]);
            if (child != null)
                DestroyObject(child.gameObject);
        }
    }

    static void Flame(Transform parent, string name, Mesh mesh, Vector3 position, float scale, Material material)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one * scale;
        MeshFilter filter = part.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;
        Track(part);
    }

    static GameObject Cube(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation;
        part.transform.localScale = scale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(collider);
            else
                UnityEngine.Object.DestroyImmediate(collider);
        }

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        Track(part);
        return part;
    }

    static void MeshPart(Transform parent, string name, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
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
        Track(part);
    }

    static void Solid(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;
        BoxCollider box = part.AddComponent<BoxCollider>();
        box.size = size;
        Track(part);
    }

    static Transform Empty(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        Track(go);
        return go.transform;
    }

    static void DestroyObject(GameObject go)
    {
        if (Destroyed != null)
            Destroyed(go);
        else
            UnityEngine.Object.DestroyImmediate(go);
    }

    static void Track(GameObject go)
    {
        Created?.Invoke(go);
    }
}
