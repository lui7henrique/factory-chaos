using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the underground workshop: one room, the existing stations, and interior lights.
/// The editor menu saves the result into IndoorFactory. This class has no editor dependency.
/// </summary>
public static class IndoorFactory
{
    public const string ScenePath = "Assets/Scenes/IndoorFactory.unity";
    public const string EnvironmentName = "Environment";
    public const string LightingName = "Lighting";
    public const string GameplayName = "Gameplay";
    public const string PlayerName = "Player";
    public const string UiName = "UI";

    const float Width = 12f;
    const float Length = 16f;
    const float Height = 4.5f;
    const float HalfWidth = Width * 0.5f;
    const float HalfLength = Length * 0.5f;
    const float Wall = 0.28f;
    const float TunnelWidth = 3.5f;
    const float TunnelHeight = 3f;
    const float TunnelDepth = 5f;

    public struct Kit
    {
        public GameObject orePrefab;
        public GameObject productPrefab;
        public Material concrete;
        public Material concreteAlt;
        public Material panel;
        public Material structure;
        public Material rock;
        public Material marking;
        public Material belt;
        public Mesh cube;
        public FurnaceVisual.Palette furnace;
        public OreVeinVisual.Palette vein;
    }

    public static Action<GameObject> Created;

    public static void Build(Kit kit)
    {
        kit = Fill(kit);

        Transform environment = Group(EnvironmentName);
        Transform lighting = Group(LightingName);
        Transform gameplay = Group(GameplayName);
        Transform player = Group(PlayerName);
        Group(UiName);

        FactorySite site = environment.gameObject.AddComponent<FactorySite>();
        site.kind = FactorySite.Kind.Indoor;

        BuildRoom(environment, kit);
        BuildLights(lighting);
        BuildStations(gameplay, kit);
        BuildPlayer(player);
        ApplyAtmosphere();
    }

    public static Mesh CreateFlatCube()
    {
        Vector3[] corners =
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f)
        };

        int[][] faces =
        {
            new[] { 0, 3, 2, 1 },
            new[] { 4, 5, 6, 7 },
            new[] { 0, 1, 5, 4 },
            new[] { 1, 2, 6, 5 },
            new[] { 2, 3, 7, 6 },
            new[] { 3, 0, 4, 7 }
        };

        Vector3[] vertices = new Vector3[24];
        Vector3[] normals = new Vector3[24];
        int[] triangles = new int[36];
        int v = 0;
        int t = 0;
        for (int face = 0; face < faces.Length; face++)
        {
            int[] ids = faces[face];
            Vector3 a = corners[ids[0]];
            Vector3 b = corners[ids[1]];
            Vector3 c = corners[ids[2]];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            for (int i = 0; i < 4; i++)
            {
                vertices[v] = corners[ids[i]];
                normals[v] = normal;
                v++;
            }

            int start = face * 4;
            triangles[t++] = start;
            triangles[t++] = start + 1;
            triangles[t++] = start + 2;
            triangles[t++] = start;
            triangles[t++] = start + 2;
            triangles[t++] = start + 3;
        }

        Mesh mesh = new Mesh { name = "IndoorCube" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void BuildRoom(Transform root, Kit kit)
    {
        Transform shell = Empty(root, "Shell");
        Box(shell, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(Width, 0.2f, Length), kit.concrete, true, false);
        Slab(shell, "FloorPlateA", new Vector3(-3f, 0.012f, -4f), new Vector3(5.96f, 0.02f, 7.96f), kit.concrete);
        Slab(shell, "FloorPlateB", new Vector3(3f, 0.014f, -4f), new Vector3(5.96f, 0.02f, 7.96f), kit.concreteAlt);
        Slab(shell, "FloorPlateC", new Vector3(-3f, 0.014f, 4f), new Vector3(5.96f, 0.02f, 7.96f), kit.concreteAlt);
        Slab(shell, "FloorPlateD", new Vector3(3f, 0.012f, 4f), new Vector3(5.96f, 0.02f, 7.96f), kit.concrete);

        Box(shell, "WallFront", new Vector3(0f, Height * 0.5f, -HalfLength - Wall * 0.5f), new Vector3(Width + Wall * 2f, Height, Wall), kit.panel, true, false);
        Box(shell, "WallRight", new Vector3(HalfWidth + Wall * 0.5f, Height * 0.5f, 0f), new Vector3(Wall, Height, Length), kit.panel, true, false);
        Box(shell, "WallLeftMetal", new Vector3(-HalfWidth - Wall * 0.5f, Height * 0.5f, 2.5f), new Vector3(Wall, Height, 11f), kit.panel, true, false);
        Box(shell, "WallLeftRock", new Vector3(-HalfWidth - Wall * 0.5f, Height * 0.5f, -5.5f), new Vector3(Wall, Height, 5f), kit.rock, true, false);
        BuildBackWall(shell, kit);
        Box(shell, "Ceiling", new Vector3(0f, Height + 0.08f, 0f), new Vector3(Width + Wall * 2f, 0.16f, Length + Wall), kit.structure, true, false);

        Transform beams = Empty(shell, "Beams");
        Beam(beams, -4.2f, kit);
        Beam(beams, 0.4f, kit);
        Beam(beams, 5.2f, kit);

        Box(shell, "ConduitLeft", new Vector3(-5.55f, 4.15f, 0f), new Vector3(0.14f, 0.14f, 14.5f), kit.structure, false, false);
        Box(shell, "ConduitRight", new Vector3(5.55f, 4.15f, 0.4f), new Vector3(0.14f, 0.14f, 13.5f), kit.structure, false, false);

        Transform marks = Empty(shell, "Marks");
        Stripe(marks, "LineNorth", new Vector3(-3.7f, 0.03f, 1.6f), new Vector3(0.12f, 0.01f, 5.4f), kit.marking);
        Stripe(marks, "LineSouth", new Vector3(-3.7f, 0.03f, -1.15f), new Vector3(2.6f, 0.01f, 0.12f), kit.marking);
        Stripe(marks, "Delivery", new Vector3(4.2f, 0.03f, -4.55f), new Vector3(1.8f, 0.01f, 0.12f), kit.marking);
        Stripe(marks, "Cannon", new Vector3(0.15f, 0.03f, 2.35f), new Vector3(2.2f, 0.01f, 0.12f), kit.marking);

        Bolt(shell, new Vector3(-5.7f, 2.2f, -3.2f), kit.structure);
        Bolt(shell, new Vector3(-5.7f, 2.2f, 2.2f), kit.structure);
        Bolt(shell, new Vector3(5.7f, 2.2f, -3.2f), kit.structure);
        Bolt(shell, new Vector3(5.7f, 2.2f, 4.4f), kit.structure);
        Bolt(shell, new Vector3(0f, 3.35f, HalfLength), kit.structure);

        BuildTunnel(shell, kit);
    }

    static void BuildBackWall(Transform shell, Kit kit)
    {
        float back = HalfLength + Wall * 0.5f;
        float side = (Width - TunnelWidth) * 0.5f;
        float sideCenter = TunnelWidth * 0.5f + side * 0.5f;
        Box(shell, "BackLeft", new Vector3(-sideCenter, Height * 0.5f, back), new Vector3(side, Height, Wall), kit.panel, true, false);
        Box(shell, "BackRight", new Vector3(sideCenter, Height * 0.5f, back), new Vector3(side, Height, Wall), kit.panel, true, false);
        float lintel = Height - TunnelHeight;
        Box(shell, "BackLintel", new Vector3(0f, TunnelHeight + lintel * 0.5f, back), new Vector3(TunnelWidth, lintel, Wall), kit.structure, true, false);

        float frame = 0.22f;
        Box(shell, "TunnelFrameLeft", new Vector3(-TunnelWidth * 0.5f - frame * 0.15f, TunnelHeight * 0.5f, HalfLength - 0.02f), new Vector3(frame, TunnelHeight + 0.2f, 0.34f), kit.structure, true, false);
        Box(shell, "TunnelFrameRight", new Vector3(TunnelWidth * 0.5f + frame * 0.15f, TunnelHeight * 0.5f, HalfLength - 0.02f), new Vector3(frame, TunnelHeight + 0.2f, 0.34f), kit.structure, true, false);
        Box(shell, "TunnelFrameTop", new Vector3(0f, TunnelHeight + 0.08f, HalfLength - 0.02f), new Vector3(TunnelWidth + frame * 2f, frame, 0.34f), kit.structure, true, false);
    }

    static void BuildTunnel(Transform shell, Kit kit)
    {
        Transform tunnel = Empty(shell, "Tunnel");
        float start = HalfLength;
        float center = start + TunnelDepth * 0.5f;
        Box(tunnel, "Floor", new Vector3(0f, -0.1f, center), new Vector3(TunnelWidth, 0.2f, TunnelDepth), kit.concrete, true, false);
        Slab(tunnel, "FloorPlate", new Vector3(0f, 0.01f, center), new Vector3(TunnelWidth - 0.04f, 0.02f, TunnelDepth), kit.concreteAlt);

        float wallCenter = TunnelWidth * 0.5f + 0.22f;
        Box(tunnel, "RockLeft", new Vector3(-wallCenter, TunnelHeight * 0.5f, center), new Vector3(0.44f, TunnelHeight, TunnelDepth), kit.rock, true, false);
        Box(tunnel, "RockRight", new Vector3(wallCenter, TunnelHeight * 0.5f, center), new Vector3(0.44f, TunnelHeight, TunnelDepth), kit.rock, true, false);
        Box(tunnel, "RockCeiling", new Vector3(0f, TunnelHeight + 0.16f, center), new Vector3(TunnelWidth + 0.88f, 0.32f, TunnelDepth), kit.rock, true, false);
        Box(tunnel, "End", new Vector3(0f, TunnelHeight * 0.5f, start + TunnelDepth + 0.16f), new Vector3(TunnelWidth + 0.9f, TunnelHeight + 0.4f, 0.32f), kit.rock, true, false);

        Mesh rock = OreMesh.SharedRock();
        if (rock != null)
        {
            Chunk(tunnel, "FacetA", rock, kit.rock, new Vector3(-1.55f, 1.3f, 10.2f), new Vector3(1.1f, 1.4f, 0.8f));
            Chunk(tunnel, "FacetB", rock, kit.rock, new Vector3(1.5f, 1.6f, 11.1f), new Vector3(1.2f, 1.1f, 0.9f));
            Chunk(tunnel, "FacetC", rock, kit.rock, new Vector3(0.2f, 2.55f, 12.2f), new Vector3(1.4f, 0.7f, 0.8f));
        }
    }

    static void BuildLights(Transform root)
    {
        Color warm = new Color(1f, 0.78f, 0.55f);
        Lamp(root, "LampFront", new Vector3(0f, 4.22f, -3.2f), warm, 14f, 14f, false, true);
        Lamp(root, "LampCenter", new Vector3(0f, 4.22f, 1.2f), warm, 16f, 14f, true, true);
        Lamp(root, "LampBack", new Vector3(0f, 4.22f, 5.6f), warm, 14f, 14f, false, true);
        Lamp(root, "TunnelLeft", new Vector3(-1.35f, 2.55f, 8.35f), warm, 2.2f, 5f, false, true);
        Lamp(root, "TunnelRight", new Vector3(1.35f, 2.55f, 8.35f), warm, 2.2f, 5f, false, true);
        Lamp(root, "FurnaceGlow", new Vector3(-3.2f, 1.12f, -0.35f), new Color(1f, 0.42f, 0.12f), 3.5f, 3.8f, false, false);
    }

    static void BuildStations(Transform root, Kit kit)
    {
        GameObject spawner = Empty(root, "OreSpawner").gameObject;
        OreSpawner oreSpawner = spawner.AddComponent<OreSpawner>();
        oreSpawner.AssignPrefab(kit.orePrefab);

        BuildVein(root, kit);
        BuildFurnace(root, kit);
        BuildConveyor(root, kit);
        BuildPress(root, kit);
        BuildDelivery(root, kit);
        BuildCannon(root, kit);
    }

    static void BuildVein(Transform root, Kit kit)
    {
        GameObject vein = Empty(root, "OreVein").gameObject;
        vein.transform.position = new Vector3(-4.7f, 0f, -3.6f);

        BoxCollider box = vein.AddComponent<BoxCollider>();
        box.center = new Vector3(0.2f, 0.32f, 0f);
        box.size = new Vector3(1.15f, 0.62f, 1.35f);

        OreVein ore = vein.AddComponent<OreVein>();
        ore.Configure(kit.orePrefab);
        ore.SetSpawnOffset(new Vector3(0.7f, 0f, 0f));
        vein.AddComponent<OreVeinVisual>();
        OreVeinVisual.Palette veinPalette = kit.vein.rock != null ? kit.vein : OreVeinVisual.RuntimePalette();
        OreVeinVisual.Build(vein.transform, veinPalette, Created);

        Mesh rock = OreMesh.SharedRock();
        if (rock != null)
            Chunk(vein.transform, "WallRock", rock, kit.rock, new Vector3(-0.75f, 0.7f, 0f), new Vector3(1.5f, 1.7f, 1.6f));
    }

    static void BuildFurnace(Transform root, Kit kit)
    {
        GameObject machine = Empty(root, "Machine").gameObject;
        machine.transform.position = new Vector3(-4.15f, 0f, -0.35f);

        GameObject input = Empty(machine.transform, "Input").gameObject;
        BoxCollider trigger = input.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        OreMachine furnace = input.AddComponent<OreMachine>();

        Transform output = Empty(machine.transform, "Output");
        FurnaceVisual.Palette furnacePalette = kit.furnace.body != null ? kit.furnace : FurnaceVisual.RuntimePalette();
        FurnaceVisual.Rebuild(machine.transform, furnacePalette, FurnaceVisual.BuildMeshes());
        furnace.Configure(output, kit.productPrefab);

        Vector3 chimney = machine.transform.TransformPoint(new Vector3(-0.02f, 2.02f, -0.02f));
        Transform duct = Empty(root, "ChimneyDuct");
        Box(duct, "Riser", chimney + new Vector3(0f, 1.1f, 0f), new Vector3(0.34f, 2.2f, 0.34f), kit.structure, false, false);
        Box(duct, "Run", new Vector3(chimney.x, 4.22f, chimney.z + 1.4f), new Vector3(0.22f, 0.16f, 2.8f), kit.structure, false, false);
    }

    static void BuildConveyor(Transform root, Kit kit)
    {
        GameObject conveyor = Empty(root, "Conveyor").gameObject;
        conveyor.transform.position = new Vector3(-4.07f, 0f, 2.55f);
        Box(conveyor.transform, "Base", new Vector3(0f, 0.1f, 0f), new Vector3(1.2f, 0.2f, 4f), kit.belt, true, false);
        GameObject zone = Box(conveyor.transform, "Zone", new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 0.8f, 4f), kit.belt, true, true);
        zone.GetComponent<MeshRenderer>().enabled = false;
        zone.AddComponent<ConveyorBelt>();
        Box(conveyor.transform, "Direction", new Vector3(0.42f, 0.22f, 0.2f), new Vector3(0.08f, 0.02f, 1.4f), kit.marking, false, false);
    }

    static void BuildPress(Transform root, Kit kit)
    {
        GameObject machine = Empty(root, "AmmoMachine").gameObject;
        machine.transform.position = new Vector3(-1.7f, 0f, 6.15f);
        machine.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        GameObject input = Empty(machine.transform, "Input").gameObject;
        BoxCollider trigger = input.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        AmmoMachine press = input.AddComponent<AmmoMachine>();

        Transform output = Empty(machine.transform, "Output");
        GameObject template = CreateAmmoTemplate(root);
        AmmoMachineVisual.Rebuild(machine.transform, AmmoMachineVisual.RuntimePalette());
        Transform lens = machine.transform.Find("Visuals/StatusLight/Lens");
        press.Configure(output, template, lens != null ? lens.GetComponent<Renderer>() : null);
    }

    static GameObject CreateAmmoTemplate(Transform root)
    {
        GameObject ammo = Empty(root, "AmmoTemplate").gameObject;
        Rigidbody body = ammo.AddComponent<Rigidbody>();
        body.mass = 0.4f;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        ammo.AddComponent<Item>().SetKind(ItemKind.Ammo);
        AmmoVisual.ApplyRuntime(ammo.transform);
        ammo.SetActive(false);
        return ammo;
    }

    static void BuildDelivery(Transform root, Kit kit)
    {
        GameObject delivery = Empty(root, "Delivery").gameObject;
        delivery.transform.position = new Vector3(4.25f, 0f, -4.2f);

        Box(delivery.transform, "Counter", new Vector3(0f, 0.48f, 0f), new Vector3(1.7f, 0.96f, 0.7f), kit.structure, true, false);
        Box(delivery.transform, "Top", new Vector3(0f, 0.98f, 0f), new Vector3(1.82f, 0.06f, 0.82f), kit.panel, true, false);
        Stripe(delivery.transform, "Lip", new Vector3(0f, 0.86f, 0.38f), new Vector3(1.7f, 0.08f, 0.04f), kit.marking);

        GameObject zone = Box(delivery.transform, "Zone", new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 0.45f, 0.7f), kit.panel, true, true);
        zone.GetComponent<MeshRenderer>().enabled = false;
        zone.AddComponent<DeliveryZone>();
    }

    static void BuildCannon(Transform root, Kit kit)
    {
        GameObject cannon = Empty(root, "Cannon").gameObject;
        cannon.transform.position = new Vector3(0.15f, 0f, 3.55f);
        CannonVisual.Rebuild(cannon.transform, CannonVisual.RuntimePalette(), CannonVisual.BuildMeshes());

        CannonController controller = cannon.GetComponent<CannonController>();
        if (controller == null)
            controller = cannon.AddComponent<CannonController>();

        Transform yaw = cannon.transform.Find("YawPivot");
        Transform pitch = yaw != null ? yaw.Find("PitchPivot") : null;
        Transform muzzle = pitch != null ? pitch.Find("MuzzlePoint") : null;
        Transform sight = pitch != null ? pitch.Find("Sight") : null;
        controller.Configure(yaw, pitch, muzzle, sight, kit.marking);

        GameObject target = Empty(root, "Target").gameObject;
        target.transform.position = new Vector3(0.15f, 0f, 12.15f);
        Box(target.transform, "Base", new Vector3(0f, 0.15f, 0f), new Vector3(1.5f, 0.3f, 0.8f), kit.structure, true, false);
        Material boardMaterial = ArtMaterials.Make("TargetBoard", new Color(0.75f, 0.18f, 0.16f), 0.06f, 0f, false);
        GameObject board = Box(target.transform, "Board", new Vector3(0f, 1.3f, 0f), new Vector3(1.2f, 1.9f, 0.28f), boardMaterial, true, false);
        target.AddComponent<TargetDummy>().Configure(board.GetComponent<Renderer>());
    }

    static void BuildPlayer(Transform root)
    {
        root.position = new Vector3(0.15f, 1.01f, -5.55f);

        CharacterController body = root.gameObject.AddComponent<CharacterController>();
        body.height = 2f;
        body.radius = 0.5f;
        body.center = Vector3.zero;
        body.stepOffset = 0.3f;
        body.skinWidth = 0.08f;

        GameObject cameraObject = Empty(root, "Camera").gameObject;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.3f;
        camera.fieldOfView = 60f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.1f, 0.11f);
        cameraObject.AddComponent<AudioListener>();

        PlayerMovement movement = root.gameObject.AddComponent<PlayerMovement>();
        PlayerCarry carry = root.gameObject.AddComponent<PlayerCarry>();
        PlayerMining mining = root.gameObject.AddComponent<PlayerMining>();
        movement.BindCamera(cameraObject.transform);
        carry.BindCamera(cameraObject.transform);
        mining.BindCamera(cameraObject.transform);
    }

    static void ApplyAtmosphere()
    {
        RenderSettings.skybox = null;
        RenderSettings.sun = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.32f, 0.3f, 0.27f);
        RenderSettings.ambientEquatorColor = new Color(0.22f, 0.2f, 0.18f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.09f, 0.08f);
        RenderSettings.ambientIntensity = 1.1f;
        RenderSettings.fog = false;
    }

    static void Lamp(Transform parent, string name, Vector3 position, Color color, float intensity, float range, bool shadows, bool warmTemperature)
    {
        Transform housing = Empty(parent, name);
        housing.position = position;
        Box(housing, "Housing", new Vector3(0f, 0.12f, 0f), new Vector3(0.7f, 0.08f, 0.28f), structureMaterial, false, false);
        Light light = housing.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = warmTemperature ? Color.white : color;
        light.useColorTemperature = warmTemperature;
        if (warmTemperature)
            light.colorTemperature = 3500f;
        light.intensity = intensity;
        light.range = range;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        light.bounceIntensity = 0.4f;
    }

    static void Beam(Transform parent, float z, Kit kit)
    {
        Box(parent, "Beam", new Vector3(0f, 4.28f, z), new Vector3(11.2f, 0.16f, 0.22f), kit.structure, false, false);
    }

    static void Stripe(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        Box(parent, name, position, size, material, false, false);
    }

    static void Bolt(Transform parent, Vector3 position, Material material)
    {
        Box(parent, "Bolt", position, new Vector3(0.16f, 0.16f, 0.08f), material, false, false);
    }

    static void Slab(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        Box(parent, name, position, size, material, false, false);
    }

    static void Chunk(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
    {
        GameObject piece = Empty(parent, name).gameObject;
        piece.transform.localPosition = position;
        piece.transform.localScale = scale;
        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = piece.AddComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;
    }

    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid, bool trigger)
    {
        GameObject part = Empty(parent, name).gameObject;
        part.transform.localPosition = position;
        part.transform.localScale = size;
        MeshFilter filter = part.AddComponent<MeshFilter>();
        filter.sharedMesh = SharedCube();
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;
        else
            renderer.enabled = false;

        if (solid || trigger)
        {
            BoxCollider box = part.AddComponent<BoxCollider>();
            box.isTrigger = trigger;
        }

        return part;
    }

    static Mesh SharedCube()
    {
        return cubeMesh != null ? cubeMesh : (cubeMesh = CreateFlatCube());
    }

    static Mesh cubeMesh;
    static Material structureMaterial;

    static Kit Fill(Kit kit)
    {
        if (kit.cube != null)
            cubeMesh = kit.cube;

        if (kit.concrete == null)
            kit.concrete = ArtMaterials.Make("IndoorConcrete", Hex(0x73716B), 0.06f, 0f, false);
        if (kit.concreteAlt == null)
            kit.concreteAlt = ArtMaterials.Make("IndoorConcreteAlt", Hex(0x6A6862), 0.06f, 0f, false);
        if (kit.panel == null)
            kit.panel = ArtMaterials.Make("IndoorPanel", Hex(0x686D72), 0.16f, 0.22f, false);
        if (kit.structure == null)
            kit.structure = ArtMaterials.Make("IndoorStructure", Hex(0x343A40), 0.12f, 0.18f, false);
        structureMaterial = kit.structure;
        if (kit.rock == null)
            kit.rock = ArtMaterials.Make("IndoorRock", Hex(0x514D4B), 0.05f, 0f, false);
        if (kit.marking == null)
            kit.marking = ArtMaterials.Make("IndoorMarking", Hex(0xF4BE32), 0.12f, 0.04f, false);
        if (kit.belt == null)
            kit.belt = kit.structure;
        return kit;
    }

    static Transform Group(string name)
    {
        GameObject group = new GameObject(name);
        Created?.Invoke(group);
        group.AddComponent<IndoorGroup>().groupId = name;
        return group.transform;
    }

    static Transform Empty(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        if (parent != null)
            child.transform.SetParent(parent, false);
        Created?.Invoke(child);
        return child.transform;
    }

    static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
