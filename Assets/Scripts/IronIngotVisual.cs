using UnityEngine;

/// <summary>
/// Swaps the carried iron bar for the imported metal block. Feet stay at the old bar bottom so the belt spawn still clears the tray.
/// </summary>
public static class IronIngotVisual
{
    const string ModelResource = "Ingot/MeshyIngot";
    const float TargetLength = 0.5f;

    public static void Apply(GameObject root)
    {
        if (root == null || root.transform.Find("Imported") != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(ModelResource);
        if (prefab == null)
            return;

        Material iron = HideOld(root);
        GameObject model = Object.Instantiate(prefab, root.transform);
        model.name = "Imported";
        Fit(model.transform, TargetLength);
        Paint(model, iron);
        DisableColliders(model);

        if (!TryBounds(model.transform, out Bounds bounds))
            return;

        float bottom = -IronIngotMesh.Height * 0.5f;
        Vector3 lift = new Vector3(0f, bottom - bounds.min.y, 0f);
        model.transform.localPosition += lift;
        bounds.center += lift;

        BoxCollider box = root.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = bounds.center;
            box.size = bounds.size;
        }
    }

    static Material HideOld(GameObject root)
    {
        Material iron = null;
        Transform visual = root.transform.Find("Visual");
        if (visual != null)
        {
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                iron = renderer.sharedMaterial;
                renderer.enabled = false;
            }
        }

        MeshRenderer rootRenderer = root.GetComponent<MeshRenderer>();
        if (rootRenderer != null)
            rootRenderer.enabled = false;

        return iron;
    }

    static void Paint(GameObject model, Material iron)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (iron != null)
                renderers[i].sharedMaterial = iron;
        }
    }

    static void DisableColliders(GameObject model)
    {
        Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    static void Fit(Transform model, float targetLength)
    {
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;
        model.localPosition = Vector3.zero;
        if (!TryBounds(model, out Bounds bounds))
            return;

        float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        float scale = targetLength / Mathf.Max(0.001f, longest);
        model.localScale = Vector3.one * scale;
        if (!TryBounds(model, out bounds))
            return;

        model.localPosition = new Vector3(-bounds.center.x, -bounds.center.y, -bounds.center.z);
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

        return found && bounds.size.sqrMagnitude > 0.0001f;
    }
}
