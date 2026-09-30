using UnityEngine;

/// <summary>
/// Modular mine wall. Scenery only: one mesh, one box, no gameplay.
/// </summary>
public static class RockWallModules
{
    const string ModelPath = "Assets/Art/Environment/Mine/RockWall/RockWall.fbx";
    const string MaterialPath = "Assets/Art/Environment/Mine/RockWall/RockWall.mat";
    const string PrefabPath = "Assets/Art/Environment/Mine/RockWall/RockWall_A.prefab";
    const float RoomWidth = 4f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (!FactorySite.IsIndoor)
            return;

        GameObject existing = GameObject.Find("RockWalls");
        if (existing != null)
            Object.DestroyImmediate(existing);

        Transform parent = null;
        GameObject shell = GameObject.Find("Shell");
        if (shell != null)
            parent = shell.transform;
        else
        {
            FactorySite site = Object.FindAnyObjectByType<FactorySite>();
            if (site != null)
                parent = site.transform;
        }

        Place(parent);
    }

    // File +Y is the flat backing. Rx(-90) maps that to local -Z, so local +Z is the rough face.
    static readonly Quaternion MeshPose = Quaternion.Euler(-90f, 0f, 0f);
    const float FileWidth = 1.904f;
    const float FileHeight = 1.812f;

    public static void Place(Transform parent)
    {
        if (parent == null)
            return;

        bool runtime = Application.isPlaying;
        if (runtime)
        {
            FactorySite site = Object.FindAnyObjectByType<FactorySite>();
            if (site != null)
                parent = site.transform;
        }

        if (parent.Find("RockWalls") != null)
            return;

        GameObject model = LoadModel();
        if (model == null)
            return;

        Material surface = LoadMaterial();
        Transform group = new GameObject("RockWalls").transform;
        group.SetParent(parent, false);

        float spread = runtime ? IndoorFactory.PlaySpread : 1f;
        float halfW = 6f * spread;
        float halfL = 8f * spread;
        float roomHeight = 4.5f * spread * 0.94f;
        float tunnelHeight = 3f * spread * 0.9f;
        float mouth = 1.75f * spread;
        float inset = 0.35f * spread;

        Along(group, model, surface, "Left", new Vector3(-halfW, 0f, -halfL + inset), new Vector3(-halfW, 0f, halfL - inset), Vector3.right, roomHeight, 11);
        Along(group, model, surface, "Right", new Vector3(halfW, 0f, -halfL + inset), new Vector3(halfW, 0f, halfL - inset), Vector3.left, roomHeight, 23);
        Along(group, model, surface, "Front", new Vector3(-halfW + inset, 0f, -halfL), new Vector3(halfW - inset, 0f, -halfL), Vector3.forward, roomHeight, 37);
        Along(group, model, surface, "BackLeft", new Vector3(-halfW + inset, 0f, halfL), new Vector3(-mouth - inset, 0f, halfL), Vector3.back, roomHeight, 53);
        Along(group, model, surface, "BackRight", new Vector3(mouth + inset, 0f, halfL), new Vector3(halfW - inset, 0f, halfL), Vector3.back, roomHeight, 67);

        float tunnelStart = 8f * spread + inset;
        float tunnelEnd = 22f * spread - inset;
        Along(group, model, surface, "TunnelLeft", new Vector3(-mouth, 0f, tunnelStart), new Vector3(-mouth, 0f, tunnelEnd), Vector3.right, tunnelHeight, 71);
        Along(group, model, surface, "TunnelRight", new Vector3(mouth, 0f, tunnelStart), new Vector3(mouth, 0f, tunnelEnd), Vector3.left, tunnelHeight, 83);
    }

    static void Along(Transform group, GameObject model, Material surface, string prefix, Vector3 from, Vector3 to, Vector3 inward, float height, int seed)
    {
        Vector3 span = to - from;
        float length = span.magnitude;
        if (length < 0.2f)
            return;

        Vector3 dir = span / length;
        float naturalWidth = height * (FileWidth / FileHeight);
        int count = Mathf.Max(1, Mathf.CeilToInt(length / (naturalWidth * 0.9f)));
        float piece = (length / count) / 0.84f;
        bool fillHeight = piece >= naturalWidth * 0.92f;

        Random.State previous = Random.state;
        Random.InitState(seed);
        for (int i = 0; i < count; i++)
        {
            float spacing = length / count;
            float center = (i + 0.5f) * spacing + (Random.value - 0.5f) * spacing * 0.22f;
            center = Mathf.Clamp(center, piece * 0.35f, length - piece * 0.35f);
            float jitter = (Random.value - 0.5f) * 16f;
            float depth = (Random.value - 0.55f) * piece * 0.06f;
            float size = 0.94f + Random.value * 0.1f;
            float width = fillHeight ? 0f : piece * size;
            float tall = fillHeight ? height * size : 0f;
            Spawn(group, model, surface, prefix + i, from + dir * center, inward, jitter, depth, width, tall);
        }

        Random.state = previous;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void SchedulePrefab()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            GameObject model = LoadModel();
            if (model == null)
                return;

            var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            GameObject root = new GameObject("RockWall_A");
            UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(root, preview);
            Material surface = LoadMaterial();
            GameObject mesh = Object.Instantiate(model, root.transform);
            mesh.name = "Mesh";
            Fit(mesh.transform, RoomWidth, 0f);
            Renderer[] renderers = mesh.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (surface != null)
                    renderers[i].sharedMaterial = surface;
            }

            if (TryBounds(mesh.transform, out Bounds bounds))
            {
                GameObject solid = new GameObject("Collider");
                solid.transform.SetParent(root.transform, false);
                BoxCollider box = solid.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = bounds.size;
            }

            root.isStatic = true;
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
        };
    }
#endif

    static void Spawn(Transform parent, GameObject model, Material surface, string name, Vector3 wallPoint, Vector3 inward, float jitter, float depth, float targetWidth, float targetHeight)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.isStatic = true;

        GameObject mesh = Object.Instantiate(model, root.transform);
        mesh.name = "Mesh";
        Fit(mesh.transform, targetWidth, targetHeight);
        mesh.isStatic = true;

        Renderer[] renderers = mesh.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (surface != null)
                renderers[i].sharedMaterial = surface;
        }

        Collider[] imported = mesh.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < imported.Length; i++)
            imported[i].enabled = false;

        if (!TryBounds(mesh.transform, out Bounds bounds))
            return;

        Vector3 intoRoom = inward.sqrMagnitude > 0.01f ? inward.normalized : Vector3.forward;
        root.transform.rotation = Quaternion.LookRotation(intoRoom, Vector3.up) * Quaternion.Euler(0f, jitter, 0f);
        root.transform.position = wallPoint + intoRoom * (bounds.extents.z + depth);

        GameObject solid = new GameObject("Collider");
        solid.transform.SetParent(root.transform, false);
        solid.isStatic = true;
        BoxCollider box = solid.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
    }

    static void Fit(Transform model, float targetWidth, float targetHeight)
    {
        model.localRotation = MeshPose;
        model.localScale = Vector3.one;
        model.localPosition = Vector3.zero;
        if (!TryBounds(model, out Bounds bounds))
            return;

        float scale = targetHeight > 0.01f
            ? targetHeight / Mathf.Max(0.001f, bounds.size.y)
            : targetWidth / Mathf.Max(0.001f, bounds.size.x);
        model.localScale = Vector3.one * scale;
        if (!TryBounds(model, out bounds))
            return;

        model.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    static bool TryBounds(Transform model, out Bounds bounds)
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

    static GameObject LoadModel()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
#else
        return null;
#endif
    }

    static Material LoadMaterial()
    {
#if UNITY_EDITOR
        Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
            return material;
#endif
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            return null;

        Material fallback = new Material(shader);
        fallback.color = ArtPalette.Graphite;
        fallback.SetFloat("_Metallic", 0f);
        fallback.SetFloat("_Smoothness", 0.22f);
        return fallback;
    }
}
