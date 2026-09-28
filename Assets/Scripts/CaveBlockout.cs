using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// A readable low-poly cave hall: faceted stone, a low ceiling, warm lamps, and a dark tunnel.
/// The yard mesh stays underneath as the walk collider.
/// </summary>
public static class CaveBlockout
{
    public const float TunnelX = 1.6f;
    public const float SpawnZ = 16.2f;
    public const float BreachZ = 8f;

    const float MinX = -8.6f;
    const float MaxX = 10.4f;
    const float MinZ = -7.4f;
    const float MaxZ = 11.2f;
    const float GapX0 = 0.25f;
    const float GapX1 = 2.95f;

    public static void Ensure()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
            return;

        DressGround();
        DressAtmosphere();
        if (GameObject.Find("CaveShell") != null)
            return;

        Material rock = Lit(new Color(0.45f, 0.4f, 0.46f), false);
        Material rockWarm = Lit(new Color(0.55f, 0.45f, 0.36f), false);
        Material rockDark = Lit(new Color(0.28f, 0.25f, 0.3f), false);
        Material floor = Lit(new Color(0.42f, 0.39f, 0.36f), false);
        Material floorDark = Lit(new Color(0.33f, 0.31f, 0.29f), false);
        Material stripe = Lit(new Color(0.86f, 0.58f, 0.16f), false);
        Material wood = Lit(new Color(0.62f, 0.4f, 0.22f), false);
        Material woodDark = Lit(new Color(0.4f, 0.25f, 0.14f), false);
        Material shade = Lit(new Color(0.22f, 0.16f, 0.12f), false);
        Material bulb = Lit(new Color(1f, 0.78f, 0.42f), true);
        Material teal = Lit(new Color(0.16f, 0.48f, 0.5f), false);
        Material tealDark = Lit(new Color(0.1f, 0.22f, 0.26f), false);
        Material lampGreen = Lit(new Color(0.4f, 0.92f, 0.45f), true);

        GameObject shell = new GameObject("CaveShell");
        Transform root = shell.transform;

        BuildHall(root, rock, rockWarm, rockDark, floor, floorDark);
        BuildTunnel(root, rockDark, out Light eye);
        PaintStripes(root, stripe);
        PlaceTimber(root, wood, woodDark);
        HangLamp(root, new Vector3(-2.1f, 3.15f, 2.4f), shade, bulb, wood);
        HangLamp(root, new Vector3(1.6f, 3.15f, 5.2f), shade, bulb, wood);
        HangLamp(root, new Vector3(6.4f, 3.15f, 1.8f), shade, bulb, wood);
        BuildGenerator(root, new Vector3(8.3f, 0f, 1.1f), teal, tealDark, lampGreen);

        WaveDirector director = shell.AddComponent<WaveDirector>();
        director.Configure(new Vector3(TunnelX, 0f, SpawnZ), BreachZ, eye);
    }

    public static GameObject CreateEnemy(Transform parent, Vector3 position, float stopZ)
    {
        GameObject root = new GameObject("WaveEnemy");
        root.transform.SetParent(parent, false);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));

        Material skin = Lit(new Color(0.62f, 0.44f, 0.32f), false);
        Material cloth = Lit(new Color(0.18f, 0.42f, 0.46f), false);
        Material eye = Lit(new Color(0.95f, 0.22f, 0.12f), true);

        GameObject body = Cube(root.transform, "Body", new Vector3(0f, 0.85f, 0f), Quaternion.identity, new Vector3(0.72f, 0.9f, 0.46f), cloth, true);
        Cube(root.transform, "Head", new Vector3(0f, 1.52f, 0.02f), Quaternion.identity, new Vector3(0.4f, 0.38f, 0.38f), skin, false);
        Cube(root.transform, "EyeL", new Vector3(-0.1f, 1.56f, 0.2f), Quaternion.identity, new Vector3(0.08f, 0.08f, 0.05f), eye, false);
        Cube(root.transform, "EyeR", new Vector3(0.1f, 1.56f, 0.2f), Quaternion.identity, new Vector3(0.08f, 0.08f, 0.05f), eye, false);

        WaveEnemy enemy = root.AddComponent<WaveEnemy>();
        enemy.Configure(stopZ, body.GetComponent<Renderer>());
        return root;
    }

    static void DressGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
            return;

        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;

        Transform decoration = ground.transform.Find("GroundDecoration");
        if (decoration != null)
            decoration.gameObject.SetActive(false);
    }

    static void DressAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.28f, 0.3f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.2f, 0.16f, 0.22f);
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 36f;

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null || lights[i].type != LightType.Directional)
                continue;

            lights[i].intensity = 0.22f;
            lights[i].color = new Color(0.72f, 0.78f, 0.92f);
            lights[i].shadows = LightShadows.Soft;
        }

        Camera view = Camera.main;
        if (view == null)
            return;

        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = RenderSettings.fogColor;
    }

    static void BuildHall(Transform parent, Material rock, Material rockWarm, Material rockDark, Material floor, Material floorDark)
    {
        Draft stones = new Draft();
        Draft warm = new Draft();
        Draft dark = new Draft();
        Draft ground = new Draft();
        Draft groundDark = new Draft();

        FillFloor(ground, groundDark, MinX - 0.4f, MaxX + 0.4f, MinZ - 0.4f, 17.4f);
        Wall(stones, warm, new Vector3(MinX, 0f, MinZ), new Vector3(0f, 0f, MaxZ - MinZ), Vector3.left, 11);
        Wall(stones, warm, new Vector3(MaxX, 0f, MinZ), new Vector3(0f, 0f, MaxZ - MinZ), Vector3.right, 29);
        Wall(warm, stones, new Vector3(MinX, 0f, MinZ), new Vector3(MaxX - MinX, 0f, 0f), Vector3.back, 47);
        Wall(stones, dark, new Vector3(MinX, 0f, MaxZ), new Vector3(GapX0 - MinX, 0f, 0f), Vector3.forward, 71);
        Wall(warm, dark, new Vector3(GapX1, 0f, MaxZ), new Vector3(MaxX - GapX1, 0f, 0f), Vector3.forward, 83);
        Ceiling(dark, stones, MinX, MaxX, MinZ, MaxZ);

        Publish(parent, "CaveRock", stones, rock, true);
        Publish(parent, "CaveWarm", warm, rockWarm, true);
        Publish(parent, "CaveDark", dark, rockDark, false);
        Publish(parent, "CaveFloor", ground, floor, false);
        Publish(parent, "CaveFloorDark", groundDark, floorDark, false);

        Boulder(parent, new Vector3(-7.7f, 0.7f, -6.5f), new Vector3(1.6f, 1.3f, 1.4f), rock, 18f);
        Boulder(parent, new Vector3(9.2f, 0.55f, -6.2f), new Vector3(1.3f, 1.1f, 1.5f), rockWarm, -24f);
        Boulder(parent, new Vector3(-7.5f, 0.8f, 9.4f), new Vector3(1.7f, 1.5f, 1.3f), rockDark, 12f);
        Boulder(parent, new Vector3(9.1f, 0.65f, 9.6f), new Vector3(1.4f, 1.2f, 1.6f), rock, -16f);
    }

    static void FillFloor(Draft light, Draft dark, float x0, float x1, float z0, float z1)
    {
        const float cell = 1.7f;
        int column = 0;
        for (float x = x0; x < x1 - 0.01f; x += cell)
        {
            int row = 0;
            float xNext = Mathf.Min(x + cell, x1);
            for (float z = z0; z < z1 - 0.01f; z += cell)
            {
                float zNext = Mathf.Min(z + cell, z1);
                Draft target = (column + row) % 2 == 0 ? light : dark;
                float lift = 0.02f + Hash(column * 13 + row) * 0.015f;
                target.Quad(
                    new Vector3(x, lift, z),
                    new Vector3(xNext, lift, z),
                    new Vector3(xNext, lift, zNext),
                    new Vector3(x, lift, zNext));
                row++;
            }

            column++;
        }
    }

    static void Wall(Draft primary, Draft accent, Vector3 origin, Vector3 along, Vector3 outward, int seed, float baseHeight = 3.15f)
    {
        float length = along.magnitude;
        if (length < 0.2f)
            return;

        Vector3 dir = along / length;
        int panels = Mathf.Max(1, Mathf.RoundToInt(length / 1.45f));
        float panel = length / panels;
        for (int i = 0; i < panels; i++)
        {
            float depth = 0.55f + Hash(seed + i) * 0.85f;
            float height = baseHeight + Hash(seed + i * 3) * 0.45f;
            float lean = (Hash(seed + i * 5) - 0.5f) * 0.35f;
            Vector3 inner = origin + dir * (panel * i);
            Vector3 innerEnd = inner + dir * panel;
            Vector3 outer = outward * depth;
            Draft draft = i % 3 == 0 ? accent : primary;
            Block(draft, inner, innerEnd, outer, height, lean);
        }
    }

    static void Block(Draft draft, Vector3 a, Vector3 b, Vector3 outward, float height, float lean)
    {
        Vector3 topOut = outward + Vector3.up * lean;
        Vector3 a0 = a;
        Vector3 b0 = b;
        Vector3 a1 = a + Vector3.up * height;
        Vector3 b1 = b + Vector3.up * height;
        Vector3 c0 = a + outward;
        Vector3 d0 = b + outward;
        Vector3 c1 = a + Vector3.up * height + topOut;
        Vector3 d1 = b + Vector3.up * height + topOut;

        draft.Quad(a0, b0, b1, a1);
        draft.Quad(d0, c0, c1, d1);
        draft.Quad(a1, b1, d1, c1);
        draft.Quad(c0, d0, b0, a0);
        draft.Quad(c0, a0, a1, c1);
        draft.Quad(b0, d0, d1, b1);
    }

    static void Ceiling(Draft dark, Draft stone, float x0, float x1, float z0, float z1)
    {
        const float cell = 1.8f;
        int column = 0;
        for (float x = x0; x < x1 - 0.01f; x += cell)
        {
            int row = 0;
            float xNext = Mathf.Min(x + cell, x1);
            for (float z = z0; z < z1 - 0.01f; z += cell)
            {
                float zNext = Mathf.Min(z + cell, z1);
                Draft draft = (column + row) % 4 == 0 ? stone : dark;
                draft.Quad(
                    new Vector3(x, Roof(x, z), z),
                    new Vector3(xNext, Roof(xNext, z), z),
                    new Vector3(xNext, Roof(xNext, zNext), zNext),
                    new Vector3(x, Roof(x, zNext), zNext));
                row++;
            }

            column++;
        }
    }

    static float Roof(float x, float z)
    {
        float edgeX = Mathf.Min(x - MinX, MaxX - x);
        float edgeZ = Mathf.Min(z - MinZ, MaxZ - z);
        float edge = Mathf.Clamp01(Mathf.Min(edgeX, edgeZ) / 3.2f);
        float jitter = (Hash(Mathf.RoundToInt(x * 4f) + Mathf.RoundToInt(z * 9f)) - 0.5f) * 0.28f;
        return Mathf.Lerp(3.15f, 4.05f, edge) + jitter;
    }

    static void BuildTunnel(Transform parent, Material rockDark, out Light eye)
    {
        Draft tunnel = new Draft();
        Vector3 left = new Vector3(GapX0, 0f, MaxZ);
        Vector3 right = new Vector3(GapX1, 0f, MaxZ);
        Vector3 span = new Vector3(0f, 0f, 6.2f);
        Wall(tunnel, tunnel, left, span, Vector3.left, 101, 2.45f);
        Wall(tunnel, tunnel, right, span, Vector3.right, 113, 2.45f);
        Block(
            tunnel,
            new Vector3(GapX0, 0f, MaxZ + 6.2f),
            new Vector3(GapX1, 0f, MaxZ + 6.2f),
            new Vector3(0f, 0f, 0.6f),
            2.45f,
            0f);

        float roof = 2.7f;
        tunnel.Quad(
            new Vector3(GapX0, roof, MaxZ),
            new Vector3(GapX1, roof, MaxZ),
            new Vector3(GapX1, roof, MaxZ + 6.4f),
            new Vector3(GapX0, roof, MaxZ + 6.4f));
        Publish(parent, "Tunnel", tunnel, rockDark, true);

        GameObject eyeObject = new GameObject("TunnelEye");
        eyeObject.transform.SetParent(parent, false);
        eyeObject.transform.position = new Vector3(TunnelX, 1.45f, MaxZ + 5.2f);
        eye = eyeObject.AddComponent<Light>();
        eye.type = LightType.Point;
        eye.color = new Color(0.95f, 0.22f, 0.12f);
        eye.range = 8f;
        eye.intensity = 1.1f;
        eye.shadows = LightShadows.None;
        Cube(parent, "TunnelGlow", eyeObject.transform.localPosition, Quaternion.identity, new Vector3(0.22f, 0.22f, 0.22f), Lit(eye.color, true), false);
    }

    static void PaintStripes(Transform parent, Material stripe)
    {
        Cube(parent, "Stripe", new Vector3(0.2f, 0.045f, 2.4f), Quaternion.identity, new Vector3(0.22f, 0.02f, 12f), stripe, false);
        Cube(parent, "Stripe", new Vector3(-1.4f, 0.045f, 1.15f), Quaternion.identity, new Vector3(9.5f, 0.02f, 0.22f), stripe, false);
    }

    static void PlaceTimber(Transform parent, Material wood, Material woodDark)
    {
        Post(parent, new Vector3(-8f, 0f, -6.6f), wood);
        Post(parent, new Vector3(-8f, 0f, 10.2f), wood);
        Post(parent, new Vector3(9.5f, 0f, -6.6f), woodDark);
        Post(parent, new Vector3(9.5f, 0f, 10.2f), wood);
        Cube(parent, "Beam", new Vector3(0.75f, 3.05f, -6.6f), Quaternion.identity, new Vector3(17.6f, 0.22f, 0.22f), woodDark, false);
        Cube(parent, "Beam", new Vector3(0.75f, 3.05f, 10.2f), Quaternion.identity, new Vector3(17.6f, 0.22f, 0.22f), wood, false);
    }

    static void Post(Transform parent, Vector3 position, Material wood)
    {
        Cube(parent, "Post", position + new Vector3(0f, 1.55f, 0f), Quaternion.identity, new Vector3(0.28f, 3.1f, 0.28f), wood, true);
    }

    static void Boulder(Transform parent, Vector3 position, Vector3 scale, Material material, float yaw)
    {
        Cube(parent, "Boulder", position, Quaternion.Euler(8f, yaw, 6f), scale, material, true);
    }

    static void BuildGenerator(Transform parent, Vector3 position, Material body, Material dark, Material lamp)
    {
        Cube(parent, "Generator", position + new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(1.5f, 1.1f, 0.9f), body, true);
        Cube(parent, "GeneratorTop", position + new Vector3(0f, 1.2f, 0f), Quaternion.identity, new Vector3(1.2f, 0.28f, 0.7f), dark, false);
        Cube(parent, "GeneratorLamp", position + new Vector3(0.42f, 0.7f, 0.42f), Quaternion.identity, new Vector3(0.16f, 0.16f, 0.08f), lamp, false);
    }

    static void HangLamp(Transform parent, Vector3 position, Material shade, Material bulb, Material wood)
    {
        Cube(parent, "Cord", position + new Vector3(0f, 0.35f, 0f), Quaternion.identity, new Vector3(0.06f, 0.7f, 0.06f), wood, false);
        Cube(parent, "Shade", position, Quaternion.identity, new Vector3(0.7f, 0.16f, 0.7f), shade, false);
        Cube(parent, "Bulb", position + new Vector3(0f, -0.16f, 0f), Quaternion.identity, new Vector3(0.24f, 0.24f, 0.24f), bulb, false);

        GameObject lightObject = new GameObject("Lamp");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position + new Vector3(0f, -0.35f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.74f, 0.42f);
        light.range = 8.5f;
        light.intensity = 3.6f;
        light.shadows = LightShadows.None;
    }

    static void Publish(Transform parent, string name, Draft draft, Material material, bool solid)
    {
        Mesh mesh = draft.Build(name);
        if (mesh.vertexCount == 0)
        {
            Object.Destroy(mesh);
            return;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        if (!solid)
            return;

        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    static GameObject Cube(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material,
        bool solid)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation;
        part.transform.localScale = scale;

        if (!solid)
        {
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        return part;
    }

    static Material Lit(Color color, bool emissive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0.04f);
        material.SetFloat("_Metallic", 0f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.85f);
        }

        return material;
    }

    static float Hash(int value)
    {
        float n = Mathf.Sin(value * 127.1f + 311.7f) * 43758.55f;
        return n - Mathf.Floor(n);
    }

    sealed class Draft
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddFace(a, b, c, d);
            AddFace(a, d, c, b);
        }

        void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
