using UnityEngine;

/// <summary>
/// Replaces the conveyor cube with the imported belt. The trigger and the solid deck stay.
/// </summary>
public static class ConveyorVisual
{
    const string ModelResource = "Conveyor/MeshyConveyor";
    const string MaterialResource = "Conveyor/ConveyorSurface";
    const float ModelPitch = -90f;
    const float ModelYaw = -90f;
    const int Segments = 3;
    const float Overlap = 0.16f;
    const float HeightOverLength = 0.365f;
    const float DeckHeight = 0.83f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureAll()
    {
        if (Resources.Load<GameObject>(ModelResource) == null)
            return;

        if (FactorySite.IsIndoor)
        {
            if (UnityEngine.Object.FindAnyObjectByType<BeltLine>() == null)
            {
                GameObject marker = new GameObject("BeltLine");
                marker.AddComponent<BeltLine>();
            }

            return;
        }

        ConveyorBelt[] belts = UnityEngine.Object.FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude);
        for (int i = 0; i < belts.Length; i++)
        {
            if (belts[i] != null && belts[i].transform.parent != null)
                Ensure(belts[i].transform.parent);
        }
    }

    public static bool AlignToMachines()
    {
        if (!FactorySite.IsIndoor)
            return false;

        Transform furnace = Machine("Machine");
        Transform press = Machine("AmmoMachine");
        Transform belt = Machine("Conveyor");
        if (furnace == null || press == null || belt == null)
            return false;
        if (!TryLocalBounds(furnace, "Visual/Model", out Bounds furnaceBounds))
            return false;
        if (!TryLocalBounds(press, "Visuals/Model", out Bounds pressBounds))
            return false;

        Vector3 forward = furnace.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        const float targetSurface = 0.62f;
        float segment = targetSurface / (HeightOverLength * DeckHeight);
        float chain = segment * (1f + (Segments - 1) * (1f - Overlap));

        // The exit tray sits beside the body center. The belt follows the tray, not the shell.
        const float furnaceTrayOffset = -0.18f;
        const float pressTrayOffset = -0.2f;
        float furnaceTrayX = furnaceBounds.center.x + furnaceTrayOffset;
        float pressTrayZ = pressBounds.center.z + pressTrayOffset;
        Vector3 exit = new Vector3(furnaceTrayX, 0f, furnaceBounds.max.z - 0.55f);
        Vector3 start = furnace.TransformPoint(exit);
        start.y = 0f;
        Vector3 end = start + forward * chain;

        Quaternion pressYaw = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 90f, 0f);
        const float tuck = 0.45f;
        Vector3 entry = new Vector3(pressBounds.max.x - tuck, 0f, pressTrayZ);
        Vector3 entryOffset = pressYaw * entry;
        press.SetPositionAndRotation(end - entryOffset, pressYaw);

        Transform deck = belt.Find("Base");
        if (deck != null)
            deck.localScale = new Vector3(1.15f, Mathf.Max(0.2f, deck.localScale.y), chain);

        Vector3 mid = (start + end) * 0.5f;
        belt.SetPositionAndRotation(new Vector3(mid.x, 0f, mid.z), Quaternion.LookRotation(forward));
        float surface = Ensure(belt);
        if (surface <= 0f)
            surface = targetSurface;

        Transform output = furnace.Find("Output");
        if (output != null)
            output.localPosition = new Vector3(furnaceTrayX, surface + 0.08f, furnaceBounds.max.z - 0.2f);

        Transform input = press.Find("Input");
        if (input != null)
        {
            input.localPosition = new Vector3(pressBounds.max.x - 0.15f, surface + 0.28f, pressTrayZ);
            input.localRotation = Quaternion.identity;
            input.localScale = new Vector3(1.35f, 1f, 1.2f);
        }

        Transform ammoOut = press.Find("Output");
        if (ammoOut != null)
            ammoOut.localPosition = new Vector3(pressBounds.min.x - 0.08f, surface + 0.04f, pressTrayZ);

        return true;
    }

    static bool TryLocalBounds(Transform machine, string modelPath, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        Transform model = machine.Find(modelPath);
        if (model == null)
            return false;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds local = renderers[i].localBounds;
            Vector3 center = local.center;
            Vector3 extents = local.extents;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = center + new Vector3(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);
                Vector3 onMachine = machine.InverseTransformPoint(renderers[i].transform.TransformPoint(point));
                if (!found)
                {
                    bounds = new Bounds(onMachine, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(onMachine);
                }
            }
        }

        return found && bounds.size.sqrMagnitude > 0.01f;
    }

    static Transform Machine(string name)
    {
        GameObject found = GameObject.Find(name);
        return found != null ? found.transform : null;
    }

    public static float Ensure(Transform beltRoot)
    {
        if (beltRoot == null)
            return 0f;

        GameObject prefab = Resources.Load<GameObject>(ModelResource);
        if (prefab == null)
            return 0f;

        Transform deck = beltRoot.Find("Base");
        if (deck == null)
            return 0f;

        HideRenderers(deck);
        HideRenderers(beltRoot.Find("Direction"));

        float length = Mathf.Max(0.5f, deck.localScale.z);
        float segmentLength = length / (1f + (Segments - 1) * (1f - Overlap));
        float spacing = segmentLength * (1f - Overlap);
        float height = segmentLength * HeightOverLength;
        float surface = height * DeckHeight;
        deck.localPosition = new Vector3(deck.localPosition.x, surface * 0.5f, deck.localPosition.z);
        deck.localScale = new Vector3(Mathf.Max(deck.localScale.x, 1.05f), surface, length);

        Transform zone = beltRoot.Find("Zone");
        if (zone != null)
        {
            zone.localPosition = new Vector3(zone.localPosition.x, surface + 0.35f, zone.localPosition.z);
            zone.localScale = new Vector3(zone.localScale.x, 0.8f, length);
        }

        for (int child = beltRoot.childCount - 1; child >= 0; child--)
        {
            Transform old = beltRoot.GetChild(child);
            if (old.name == "Visual")
                UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(beltRoot, false);
        Material surfaceMaterial = Resources.Load<Material>(MaterialResource);
        float start = -length * 0.5f + segmentLength * 0.5f;
        for (int i = 0; i < Segments; i++)
        {
            GameObject model = UnityEngine.Object.Instantiate(prefab, visual);
            model.name = "Segment" + i;
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                if (surfaceMaterial != null)
                    renderers[r].sharedMaterial = surfaceMaterial;
            }

            PlaceSegment(model.transform, segmentLength, start + spacing * i);
        }

        return surface;
    }

    static void PlaceSegment(Transform model, float segmentLength, float centerZ)
    {
        model.localRotation = Quaternion.Euler(ModelPitch, ModelYaw, 0f);
        model.localScale = Vector3.one;
        model.localPosition = Vector3.zero;
        if (!TryParentBounds(model, out Bounds bounds))
            return;

        float scale = segmentLength / Mathf.Max(0.001f, bounds.size.z);
        model.localScale = Vector3.one * scale;
        if (!TryParentBounds(model, out bounds))
            return;

        model.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, centerZ - bounds.center.z);
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

        return found && bounds.size.z > 0.001f;
    }

    static void HideRenderers(Transform node)
    {
        if (node == null)
            return;

        Renderer[] renderers = node.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;
    }
}

/// <summary>
/// Waits until both machine models exist, then centers the belt on their ports.
/// </summary>
public class BeltLine : MonoBehaviour
{
    void LateUpdate()
    {
        if (ConveyorVisual.AlignToMachines())
            enabled = false;
    }
}

