using UnityEngine;

/// <summary>
/// Hangs the industrial ceiling lamp from the three room lights.
/// The old point light becomes a spot under the shade. Tunnel and furnace lights stay.
/// </summary>
public class CeilingLampVisual : MonoBehaviour
{
    const string ModelResource = "CeilingLamp/MeshyLamp";
    const string MaterialResource = "CeilingLamp/LampSurface";
    const float TargetWidth = 1.1f;

    static readonly string[] Lamps = { "LampFront", "LampCenter", "LampBack" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!FactorySite.IsIndoor)
            return;

        GameObject runner = new GameObject("CeilingLampDress");
        runner.hideFlags = HideFlags.HideAndDontSave;
        runner.AddComponent<CeilingLampVisual>();
    }

    void LateUpdate()
    {
        Apply();
        Destroy(gameObject);
    }

    public static void Apply()
    {
        if (!FactorySite.IsIndoor)
            return;

        GameObject prefab = Resources.Load<GameObject>(ModelResource);
        if (prefab == null)
            return;

        Material surface = Resources.Load<Material>(MaterialResource);
        for (int i = 0; i < Lamps.Length; i++)
            Dress(GameObject.Find(Lamps[i]), prefab, surface);
    }

    static void Dress(GameObject lamp, GameObject prefab, Material surface)
    {
        if (lamp == null || lamp.transform.Find("CeilingLamp") != null)
            return;

        HideCube(lamp.transform, "Housing");
        HideCube(lamp.transform, "Shade");

        Transform root = new GameObject("CeilingLamp").transform;
        root.SetParent(lamp.transform, false);
        float ceiling = 4.5f * IndoorFactory.PlaySpread;
        root.localPosition = new Vector3(0f, ceiling - lamp.transform.position.y, 0f);

        GameObject mesh = Object.Instantiate(prefab, root);
        mesh.name = "Mesh";
        Fit(mesh.transform, TargetWidth);

        Renderer[] renderers = mesh.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (surface != null)
                renderers[i].sharedMaterial = surface;
        }

        Collider[] colliders = mesh.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        if (!TryBounds(mesh.transform, out Bounds bounds))
            return;

        float bottom = bounds.min.y - 0.15f;
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Collider glowCollider = glow.GetComponent<Collider>();
        if (glowCollider != null)
            Object.DestroyImmediate(glowCollider);
        glow.name = "EmissiveSurface";
        glow.transform.SetParent(root, false);
        glow.transform.localPosition = new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);
        glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        glow.transform.localScale = new Vector3(TargetWidth * 0.32f, TargetWidth * 0.32f, 1f);
        MeshRenderer glowRenderer = glow.GetComponent<MeshRenderer>();
        if (glowRenderer != null)
            glowRenderer.sharedMaterial = GlowMaterial();

        Light old = lamp.GetComponent<Light>();
        GameObject spotObject = new GameObject("SpotLight");
        spotObject.transform.SetParent(root, false);
        spotObject.transform.localPosition = new Vector3(bounds.center.x, bottom, bounds.center.z);
        Light spot = spotObject.AddComponent<Light>();
        spot.type = LightType.Point;
        spot.color = new Color(1f, 0.816f, 0.541f);
        bool byTheHole = lamp.name == "LampBack";
        spot.intensity = byTheHole ? 6f : 8.5f;
        spot.range = byTheHole ? 10f : 14f;
        spot.shadows = !byTheHole && old != null && old.shadows == LightShadows.Soft
            ? LightShadows.Soft
            : LightShadows.None;
        spot.bounceIntensity = 0.35f;
        if (old != null)
            old.enabled = false;
    }

    static Material glow;

    static Material GlowMaterial()
    {
        if (glow != null)
            return glow;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        glow = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        Color color = new Color(1f, 0.788f, 0.42f, 1f);
        glow.SetColor("_BaseColor", color);
        glow.SetColor("_Color", color);
        glow.SetColor("_EmissionColor", color * 2.4f);
        glow.EnableKeyword("_EMISSION");
        glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return glow;
    }

    static void HideCube(Transform lamp, string name)
    {
        Transform piece = lamp.Find(name);
        if (piece == null)
            return;

        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;
    }

    static void Fit(Transform model, float targetWidth)
    {
        // The shade is file -Z. Pitch -90 points that opening at the floor; the other end stays on the ceiling.
        model.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        model.localScale = Vector3.one;
        model.localPosition = Vector3.zero;
        if (!TryBounds(model, out Bounds bounds))
            return;

        float scale = targetWidth / Mathf.Max(0.001f, bounds.size.x);
        model.localScale = Vector3.one * scale;
        if (!TryBounds(model, out bounds))
            return;

        model.localPosition = new Vector3(-bounds.center.x, -bounds.max.y, -bounds.center.z);
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
}
