using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the low-poly fixed cannon look. Gameplay pivots, muzzle, sight, and Input stay on the scripts.
/// </summary>
public static class CannonVisual
{
    public const string BaseVisualName = "BaseVisual";
    public const string SupportVisualName = "SupportVisual";
    public const string LoadingTrayName = "LoadingTray";
    public const string StatusLightName = "StatusLight";
    public const string BarrelVisualName = "BarrelVisual";
    public const string MuzzlePointName = "MuzzlePoint";
    public const string SightName = "Sight";
    public const string YawPivotName = "YawPivot";
    public const string PitchPivotName = "PitchPivot";
    public const string InputName = "Input";

    public static Action<GameObject> Created;
    public static Action<GameObject> Destroyed;

    public struct Palette
    {
        public Material structure;
        public Material support;
        public Material accent;
        public Material joint;
        public Material bore;
        public Material lamp;
    }

    public struct MeshSet
    {
        public Mesh cylinder8;
        public Mesh ring8;
        public Mesh hex;
        public Mesh tube8;
    }

    public static Palette RuntimePalette()
    {
        return new Palette
        {
            structure = MakeMaterial(Hex(0x343A40), 0.06f, false),
            support = MakeMaterial(Hex(0x397B7D), 0.06f, false),
            accent = MakeMaterial(Hex(0xF4BE32), 0.08f, false),
            joint = MakeMaterial(Hex(0x858D96), 0.1f, false),
            bore = MakeMaterial(Hex(0x171B20), 0.04f, false),
            lamp = MakeMaterial(Hex(0x59EF61), 0.15f, true)
        };
    }

    public static MeshSet BuildMeshes()
    {
        return new MeshSet
        {
            cylinder8 = BuildCylinder(8, "CannonCylinder8", closed: true),
            ring8 = BuildRing(8, 0.5f, 0.36f, 0.18f, "CannonRing8"),
            hex = BuildCylinder(6, "CannonHex", closed: true),
            tube8 = BuildTube(8, 0.5f, 0.34f, 1f, "CannonTube8")
        };
    }

    public static void Rebuild(Transform cannonRoot, Palette palette, MeshSet meshes)
    {
        if (cannonRoot == null)
            return;

        palette = Fill(palette);
        meshes = Fill(meshes);
        cannonRoot.localScale = Vector3.one;

        RemoveLegacyPlaceholders(cannonRoot);

        Transform yaw = EnsureEmpty(cannonRoot, YawPivotName);
        yaw.localPosition = new Vector3(0f, 1.3f, 0f);
        yaw.localRotation = Quaternion.identity;
        yaw.localScale = Vector3.one;

        Transform pitch = EnsureEmpty(yaw, PitchPivotName);
        pitch.localPosition = Vector3.zero;
        pitch.localRotation = Quaternion.identity;
        pitch.localScale = Vector3.one;

        ReplaceChild(cannonRoot, BaseVisualName, parent => BuildBase(parent, palette, meshes));
        ReplaceChild(yaw, SupportVisualName, parent => BuildSupport(parent, palette, meshes));
        ReplaceChild(yaw, LoadingTrayName, parent => BuildTray(parent, palette));
        Renderer lampLens = null;
        ReplaceChild(yaw, StatusLightName, parent => lampLens = BuildLamp(parent, palette, meshes));
        Transform barrelVisual = null;
        ReplaceChild(pitch, BarrelVisualName, parent =>
        {
            BuildBarrel(parent, palette, meshes);
            barrelVisual = parent;
        });

        Transform muzzle = EnsureEmpty(pitch, MuzzlePointName);
        muzzle.localPosition = new Vector3(0f, 0f, 1.22f);
        muzzle.localRotation = Quaternion.identity;
        muzzle.localScale = Vector3.one;

        // Prefer MuzzlePoint; remove old Muzzle empty if present.
        Transform legacyMuzzle = pitch.Find("Muzzle");
        if (legacyMuzzle != null && legacyMuzzle != muzzle)
            DestroyObject(legacyMuzzle.gameObject);

        Transform sight = EnsureEmpty(pitch, SightName);
        sight.localPosition = new Vector3(0f, 0.28f, -0.55f);
        sight.localRotation = Quaternion.identity;
        sight.localScale = Vector3.one;

        Transform input = EnsureInput(cannonRoot);
        input.localPosition = new Vector3(0f, 1.15f, -0.95f);
        input.localRotation = Quaternion.identity;
        input.localScale = new Vector3(0.95f, 0.7f, 0.75f);

        BindController(cannonRoot, yaw, pitch, muzzle, sight);
        BindFx(cannonRoot, barrelVisual, lampLens);
        ApplyImported(cannonRoot);
    }

    const string CannonModelResource = "Cannon/MeshyCannon";
    const string CannonMaterialResource = "Cannon/CannonSurface";
    const float CannonPitch = -90f;
    const float CannonYaw = -90f;
    const float CannonHeight = 1.8f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureImported()
    {
        if (Resources.Load<GameObject>(CannonModelResource) == null)
            return;

        CannonController[] cannons = UnityEngine.Object.FindObjectsByType<CannonController>(FindObjectsInactive.Exclude);
        for (int i = 0; i < cannons.Length; i++)
        {
            if (cannons[i] != null)
                ApplyImported(cannons[i].transform);
        }
    }

    static void ApplyImported(Transform cannonRoot)
    {
        GameObject prefab = Resources.Load<GameObject>(CannonModelResource);
        if (prefab == null || cannonRoot == null)
            return;

        HideNamed(cannonRoot, BaseVisualName);
        HideNamed(cannonRoot, SupportVisualName);
        HideNamed(cannonRoot, LoadingTrayName);
        HideNamed(cannonRoot, StatusLightName);
        HideNamed(cannonRoot, BarrelVisualName);

        DestroyNow(cannonRoot.Find("ImportedVisual"));
        DestroyNow(cannonRoot.Find("CannonBody"));

        Transform visual = Empty(cannonRoot, "ImportedVisual");
        GameObject model = UnityEngine.Object.Instantiate(prefab, visual);
        model.name = "Model";
        Track(model);
        FitCannon(model.transform, CannonHeight);

        Material surface = Resources.Load<Material>(CannonMaterialResource);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (surface != null)
                renderers[i].sharedMaterial = surface;
        }

        if (!TryCannonBounds(model.transform, out Bounds bounds))
            return;

        Transform pitch = cannonRoot.Find(YawPivotName + "/" + PitchPivotName);
        Transform yaw = cannonRoot.Find(YawPivotName);
        if (pitch != null)
        {
            Transform muzzle = pitch.Find(MuzzlePointName);
            if (muzzle != null)
            {
                Vector3 tip = cannonRoot.TransformPoint(new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.62f, bounds.max.z - 0.08f));
                muzzle.position = tip;
                muzzle.localRotation = Quaternion.identity;
            }

            Transform sight = pitch.Find(SightName);
            if (sight != null)
            {
                Vector3 eye = cannonRoot.TransformPoint(new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.72f, bounds.min.z - 0.45f));
                sight.position = eye;
                sight.localRotation = Quaternion.identity;
            }
        }

        Transform input = cannonRoot.Find(InputName);
        if (input != null)
            input.localPosition = new Vector3(bounds.center.x, 1.05f, bounds.min.z - 0.15f);

        CannonYawFollow follow = visual.GetComponent<CannonYawFollow>();
        if (follow == null)
            follow = visual.gameObject.AddComponent<CannonYawFollow>();
        follow.Bind(yaw);

        Transform solids = Empty(cannonRoot, "CannonBody");
        GameObject body = new GameObject("Collider");
        body.transform.SetParent(solids, false);
        body.transform.localPosition = new Vector3(bounds.center.x, bounds.size.y * 0.28f, bounds.center.z - bounds.size.z * 0.08f);
        BoxCollider box = body.AddComponent<BoxCollider>();
        box.size = new Vector3(bounds.size.x * 0.72f, bounds.size.y * 0.55f, bounds.size.z * 0.42f);
        Track(body);
        Track(solids.gameObject);
    }

    static void FitCannon(Transform model, float targetHeight)
    {
        model.localRotation = Quaternion.Euler(CannonPitch, CannonYaw, 0f);
        model.localScale = Vector3.one;
        model.localPosition = Vector3.zero;
        if (!TryCannonBounds(model, out Bounds bounds))
            return;

        float scale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
        model.localScale = Vector3.one * scale;
        if (!TryCannonBounds(model, out bounds))
            return;

        model.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    static bool TryCannonBounds(Transform model, out Bounds bounds)
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

    static void HideNamed(Transform cannonRoot, string name)
    {
        Transform[] all = cannonRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name != name)
                continue;

            Renderer[] renderers = all[i].GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
                renderers[r].enabled = false;

            Collider[] colliders = all[i].GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < colliders.Length; c++)
                colliders[c].enabled = false;
        }
    }

    static void BuildBase(Transform parent, Palette palette, MeshSet meshes)
    {
        Cube(parent, "Slab", new Vector3(0f, 0.18f, 0f), Quaternion.identity, new Vector3(1.35f, 0.28f, 1.35f), palette.structure, true);
        Cube(parent, "Bevel", new Vector3(0f, 0.34f, 0f), Quaternion.identity, new Vector3(1.2f, 0.08f, 1.2f), palette.structure, false);

        Vector3[] feet =
        {
            new Vector3(-0.62f, 0f, -0.62f),
            new Vector3(-0.62f, 0f, 0.62f),
            new Vector3(0.62f, 0f, -0.62f),
            new Vector3(0.62f, 0f, 0.62f)
        };

        for (int i = 0; i < feet.Length; i++)
        {
            Cube(parent, "Foot" + i, feet[i] + new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Vector3(0.28f, 0.16f, 0.28f), palette.structure, true);
            Cube(parent, "FootCap" + i, feet[i] + new Vector3(0f, 0.02f, 0f), Quaternion.identity, new Vector3(0.34f, 0.06f, 0.34f), palette.accent, true);
        }

        Faceted(parent, "YawRing", meshes.ring8, new Vector3(0f, 0.42f, 0f), Quaternion.identity, new Vector3(1.05f, 1f, 1.05f), palette.accent, false);
        Faceted(parent, "YawHub", meshes.cylinder8, new Vector3(0f, 0.48f, 0f), Quaternion.identity, new Vector3(0.55f, 0.1f, 0.55f), palette.joint, false);
    }

    static void BuildSupport(Transform parent, Palette palette, MeshSet meshes)
    {
        // U cradle relative to yaw origin at barrel axis height.
        Cube(parent, "CradleFloor", new Vector3(0f, -0.28f, -0.05f), Quaternion.identity, new Vector3(0.72f, 0.14f, 0.55f), palette.support, true);
        Cube(parent, "ArmL", new Vector3(-0.42f, -0.02f, -0.02f), Quaternion.identity, new Vector3(0.18f, 0.55f, 0.42f), palette.support, true);
        Cube(parent, "ArmR", new Vector3(0.42f, -0.02f, -0.02f), Quaternion.identity, new Vector3(0.18f, 0.55f, 0.42f), palette.support, true);
        Cube(parent, "BraceBack", new Vector3(0f, -0.12f, -0.28f), Quaternion.identity, new Vector3(0.85f, 0.22f, 0.12f), palette.support, false);

        BuildJoint(parent, "JointL", new Vector3(-0.55f, 0f, 0f), palette, meshes);
        BuildJoint(parent, "JointR", new Vector3(0.55f, 0f, 0f), palette, meshes);
    }

    static void BuildJoint(Transform parent, string name, Vector3 position, Palette palette, MeshSet meshes)
    {
        Transform joint = Empty(parent, name);
        joint.localPosition = position;
        Faceted(joint, "Disk", meshes.cylinder8, Vector3.zero, Quaternion.Euler(0f, 0f, 90f), new Vector3(0.32f, 0.08f, 0.32f), palette.joint, false);
        Faceted(joint, "Bolt", meshes.hex, Vector3.zero, Quaternion.Euler(0f, 0f, 90f), new Vector3(0.14f, 0.12f, 0.14f), palette.accent, false);
    }

    static void BuildTray(Transform parent, Palette palette)
    {
        // Visual only; sits on the side and yaws with the cradle. No solid collider in the load path.
        Transform tray = Empty(parent, "Parts");
        tray.localPosition = new Vector3(0.72f, -0.35f, -0.35f);
        tray.localRotation = Quaternion.Euler(0f, -18f, 0f);

        Cube(tray, "Floor", new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(0.42f, 0.06f, 0.7f), palette.accent, false);
        Cube(tray, "WallOuter", new Vector3(0.2f, 0.12f, 0f), Quaternion.identity, new Vector3(0.06f, 0.22f, 0.7f), palette.accent, false);
        Cube(tray, "WallBack", new Vector3(0f, 0.12f, -0.34f), Quaternion.identity, new Vector3(0.42f, 0.22f, 0.06f), palette.accent, false);
        Cube(tray, "LipFront", new Vector3(0f, 0.04f, 0.36f), Quaternion.identity, new Vector3(0.38f, 0.05f, 0.05f), palette.structure, false);
    }

    static Renderer BuildLamp(Transform parent, Palette palette, MeshSet meshes)
    {
        Transform group = Empty(parent, "Parts");
        group.localPosition = new Vector3(0.78f, -0.05f, -0.55f);

        Cube(group, "Post", new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(0.08f, 0.18f, 0.08f), palette.structure, false);
        Cube(group, "Base", new Vector3(0f, 0.12f, 0f), Quaternion.identity, new Vector3(0.12f, 0.06f, 0.12f), palette.structure, false);
        GameObject lens = Faceted(group, "Lens", meshes.hex, new Vector3(0f, 0.22f, 0f), Quaternion.identity, new Vector3(0.12f, 0.1f, 0.12f), palette.lamp, false);
        return lens.GetComponent<Renderer>();
    }

    static void BuildBarrel(Transform parent, Palette palette, MeshSet meshes)
    {
        // Body along local +Z (cylinder mesh is Y-up, rotated 90° around X).
        const float length = 1.4f;
        const float diameter = 0.45f;
        float half = length * 0.5f;
        // Pitch axis through the rear third so muzzle clears forward.
        Vector3 bodyCenter = new Vector3(0f, 0f, 0.35f);

        Faceted(
            parent,
            "Body",
            meshes.tube8,
            bodyCenter,
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(diameter, length, diameter),
            palette.structure,
            true);

        Faceted(
            parent,
            "RearCap",
            meshes.cylinder8,
            new Vector3(0f, 0f, bodyCenter.z - half + 0.04f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(diameter * 0.92f, 0.08f, diameter * 0.92f),
            palette.structure,
            false);

        float muzzleZ = bodyCenter.z + half;
        Faceted(
            parent,
            "MuzzleRim",
            meshes.ring8,
            new Vector3(0f, 0f, muzzleZ - 0.02f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(diameter * 1.12f, 0.7f, diameter * 1.12f),
            palette.accent,
            false);

        Faceted(
            parent,
            "BoreWall",
            meshes.tube8,
            new Vector3(0f, 0f, muzzleZ - 0.12f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(diameter * 0.62f, 0.22f, diameter * 0.62f),
            palette.bore,
            false);

        Faceted(
            parent,
            "BoreBottom",
            meshes.cylinder8,
            new Vector3(0f, 0f, muzzleZ - 0.22f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(diameter * 0.55f, 0.04f, diameter * 0.55f),
            palette.bore,
            false);

        // Decorative rear crank.
        Transform crank = Empty(parent, "Crank");
        crank.localPosition = new Vector3(0.22f, 0.05f, bodyCenter.z - half - 0.02f);
        Cube(crank, "Arm", new Vector3(0.12f, 0f, 0f), Quaternion.identity, new Vector3(0.28f, 0.05f, 0.05f), palette.joint, false);
        Cube(crank, "Handle", new Vector3(0.26f, 0.08f, 0f), Quaternion.identity, new Vector3(0.06f, 0.16f, 0.06f), palette.accent, false);
    }

    static void BindController(Transform cannonRoot, Transform yaw, Transform pitch, Transform muzzle, Transform sight)
    {
        CannonController controller = cannonRoot.GetComponent<CannonController>();
        if (controller == null)
            controller = cannonRoot.gameObject.AddComponent<CannonController>();

        controller.Configure(yaw, pitch, muzzle, sight, null);
    }

    static void BindFx(Transform cannonRoot, Transform barrelVisual, Renderer lampLens)
    {
        CannonVisualFx fx = cannonRoot.GetComponent<CannonVisualFx>();
        if (fx == null)
            fx = cannonRoot.gameObject.AddComponent<CannonVisualFx>();

        fx.Bind(barrelVisual, lampLens);
    }

    static Transform EnsureInput(Transform cannonRoot)
    {
        Transform input = cannonRoot.Find(InputName);
        if (input == null)
        {
            GameObject created = new GameObject(InputName);
            created.transform.SetParent(cannonRoot, false);
            Track(created);
            input = created.transform;
        }

        MeshRenderer renderer = input.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;

        MeshFilter filter = input.GetComponent<MeshFilter>();
        if (filter != null)
            DestroyObjectComponent(filter);

        BoxCollider box = input.GetComponent<BoxCollider>();
        if (box == null)
            box = input.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = Vector3.one;

        // Strip leftover solid colliders from the old cube primitive.
        Collider[] colliders = input.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != box)
                DestroyObjectComponent(colliders[i]);
        }

        CannonLoader loader = input.GetComponent<CannonLoader>();
        if (loader == null)
            loader = input.gameObject.AddComponent<CannonLoader>();

        CannonController cannon = cannonRoot.GetComponent<CannonController>();
        if (cannon != null)
            loader.Bind(cannon);

        return input;
    }

    static void RemoveLegacyPlaceholders(Transform cannonRoot)
    {
        string[] names = { "Base", "Hopper", "Barrel", "Visuals" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = cannonRoot.Find(names[i]);
            if (child == null)
                continue;

            // Never delete gameplay nodes.
            if (child.GetComponent<CannonLoader>() != null || child.GetComponent<CannonController>() != null)
                continue;

            DestroyObject(child.gameObject);
        }

        // Old barrel under pitch.
        Transform yaw = cannonRoot.Find(YawPivotName);
        if (yaw == null)
            return;

        Transform pitch = yaw.Find(PitchPivotName);
        if (pitch == null)
            return;

        Transform oldBarrel = pitch.Find("Barrel");
        if (oldBarrel != null)
            DestroyObject(oldBarrel.gameObject);
    }

    static void ReplaceChild(Transform parent, string name, Action<Transform> build)
    {
        Transform old = parent.Find(name);
        if (old != null)
            DestroyObject(old.gameObject);

        Transform group = Empty(parent, name);
        build(group);
    }

    static Transform EnsureEmpty(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        return Empty(parent, name);
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

        if (solid && mesh != null)
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
        if (collider != null)
            DestroyObjectComponent(collider);
    }

    static void DestroyNow(Transform child)
    {
        if (child != null)
            UnityEngine.Object.DestroyImmediate(child.gameObject);
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

    static void DestroyObjectComponent(Component component)
    {
        if (component == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(component);
        else
            UnityEngine.Object.DestroyImmediate(component);
    }

    static void Track(GameObject go)
    {
        Created?.Invoke(go);
    }

    static Palette Fill(Palette palette)
    {
        Palette fallback = default;
        bool needsFallback = palette.structure == null || palette.support == null || palette.accent == null
            || palette.joint == null || palette.bore == null || palette.lamp == null;
        if (needsFallback)
            fallback = RuntimePalette();

        if (palette.structure == null) palette.structure = fallback.structure;
        if (palette.support == null) palette.support = fallback.support;
        if (palette.accent == null) palette.accent = fallback.accent;
        if (palette.joint == null) palette.joint = fallback.joint;
        if (palette.bore == null) palette.bore = fallback.bore;
        if (palette.lamp == null) palette.lamp = fallback.lamp;
        return palette;
    }

    static MeshSet Fill(MeshSet meshes)
    {
        MeshSet fallback = default;
        bool needsFallback = meshes.cylinder8 == null || meshes.ring8 == null || meshes.hex == null || meshes.tube8 == null;
        if (needsFallback)
            fallback = BuildMeshes();

        if (meshes.cylinder8 == null) meshes.cylinder8 = fallback.cylinder8;
        if (meshes.ring8 == null) meshes.ring8 = fallback.ring8;
        if (meshes.hex == null) meshes.hex = fallback.hex;
        if (meshes.tube8 == null) meshes.tube8 = fallback.tube8;
        return meshes;
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

    static Color Hex(int rgb)
    {
        float r = ((rgb >> 16) & 0xFF) / 255f;
        float g = ((rgb >> 8) & 0xFF) / 255f;
        float b = (rgb & 0xFF) / 255f;
        return new Color(r, g, b, 1f);
    }

    static Mesh BuildCylinder(int sides, string meshName, bool closed)
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
            if (closed)
            {
                AddOutward(vertices, triangles, top, top1, top0, Vector3.up);
                AddOutward(vertices, triangles, bottom, bottom0, bottom1, Vector3.down);
            }
        }

        return Finish(meshName, vertices, triangles);
    }

    static Mesh BuildTube(int sides, float outerRadius, float innerRadius, float height, string meshName)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float half = height * 0.5f;

        for (int i = 0; i < sides; i++)
        {
            float a0 = (i / (float)sides) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
            Vector3 o0 = new Vector3(Mathf.Cos(a0) * outerRadius, 0f, Mathf.Sin(a0) * outerRadius);
            Vector3 o1 = new Vector3(Mathf.Cos(a1) * outerRadius, 0f, Mathf.Sin(a1) * outerRadius);
            Vector3 i0 = new Vector3(Mathf.Cos(a0) * innerRadius, 0f, Mathf.Sin(a0) * innerRadius);
            Vector3 i1 = new Vector3(Mathf.Cos(a1) * innerRadius, 0f, Mathf.Sin(a1) * innerRadius);

            Vector3 oTop0 = o0 + Vector3.up * half;
            Vector3 oTop1 = o1 + Vector3.up * half;
            Vector3 oBot0 = o0 - Vector3.up * half;
            Vector3 oBot1 = o1 - Vector3.up * half;
            Vector3 iTop0 = i0 + Vector3.up * half;
            Vector3 iTop1 = i1 + Vector3.up * half;
            Vector3 iBot0 = i0 - Vector3.up * half;
            Vector3 iBot1 = i1 - Vector3.up * half;

            Vector3 outward = o0 + o1;
            AddOutward(vertices, triangles, oTop0, oBot1, oBot0, outward);
            AddOutward(vertices, triangles, oTop0, oTop1, oBot1, outward);

            Vector3 inward = -(i0 + i1);
            AddOutward(vertices, triangles, iTop0, iBot0, iBot1, inward);
            AddOutward(vertices, triangles, iTop0, iBot1, iTop1, inward);

            AddOutward(vertices, triangles, oTop0, iTop0, iTop1, Vector3.up);
            AddOutward(vertices, triangles, oTop0, iTop1, oTop1, Vector3.up);
            AddOutward(vertices, triangles, oBot0, oBot1, iBot1, Vector3.down);
            AddOutward(vertices, triangles, oBot0, iBot1, iBot0, Vector3.down);
        }

        return Finish(meshName, vertices, triangles);
    }

    static Mesh BuildRing(int sides, float outerRadius, float innerRadius, float height, string meshName)
    {
        return BuildTube(sides, outerRadius, innerRadius, height, meshName);
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

public class CannonYawFollow : MonoBehaviour
{
    Transform yaw;

    public void Bind(Transform yawPivot)
    {
        yaw = yawPivot;
    }

    void LateUpdate()
    {
        if (yaw == null)
            return;

        transform.localRotation = Quaternion.Euler(0f, yaw.localEulerAngles.y, 0f);
    }
}
