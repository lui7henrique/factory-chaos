using UnityEngine;

/// <summary>
/// Covers the indoor room floor with the industrial plate. The walk collider stays.
/// The tunnel floor is left dark.
/// </summary>
public class FloorVisual : MonoBehaviour
{
    const string ModelResource = "Floor/MeshyFloor";
    const string MaterialResource = "Floor/FloorSurface";
    const float Tile = 5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!FactorySite.IsIndoor)
            return;

        GameObject runner = new GameObject("FloorDress");
        runner.hideFlags = HideFlags.HideAndDontSave;
        runner.AddComponent<FloorVisual>();
    }

    void LateUpdate()
    {
        Apply();
        Destroy(gameObject);
    }

    public static void Apply()
    {
        if (!FactorySite.IsIndoor || GameObject.Find("FloorPlates") != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(ModelResource);
        if (prefab == null)
            return;

        Transform shell = null;
        GameObject shellObject = GameObject.Find("Shell");
        if (shellObject != null)
            shell = shellObject.transform;

        if (shell != null)
        {
            HideRenderer(shell, "Floor");
            HideRenderer(shell, "FloorPlateA");
            HideRenderer(shell, "FloorPlateB");
            HideRenderer(shell, "FloorPlateC");
            HideRenderer(shell, "FloorPlateD");
        }

        Material surface = Resources.Load<Material>(MaterialResource);
        Transform group = new GameObject("FloorPlates").transform;
        FactorySite site = Object.FindAnyObjectByType<FactorySite>();
        if (site != null)
            group.SetParent(site.transform, false);

        float spread = IndoorFactory.PlaySpread;
        float roomW = 12f * spread;
        float roomL = 16f * spread;
        int columns = Mathf.CeilToInt(roomW / Tile);
        int rows = Mathf.CeilToInt(roomL / Tile);
        float originX = -roomW * 0.5f + Tile * 0.5f;
        float originZ = -roomL * 0.5f + Tile * 0.5f;

        Random.State previous = Random.state;
        Random.InitState(7);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Vector3 center = new Vector3(originX + column * Tile, 0f, originZ + row * Tile);
                float yaw = 90f * Random.Range(0, 4);
                Lay(group, prefab, surface, center, yaw);
            }
        }

        Random.state = previous;
    }

    static void Lay(Transform parent, GameObject prefab, Material surface, Vector3 center, float yaw)
    {
        GameObject mesh = Object.Instantiate(prefab, parent);
        mesh.name = "Plate";
        Transform model = mesh.transform;
        model.localRotation = Quaternion.Euler(-90f, yaw, 0f);
        model.localScale = Vector3.one;
        model.localPosition = Vector3.zero;
        if (!TryBounds(model, out Bounds bounds))
            return;

        float scale = Tile / Mathf.Max(0.001f, Mathf.Max(bounds.size.x, bounds.size.z));
        model.localScale = Vector3.one * scale;
        if (!TryBounds(model, out bounds))
            return;

        model.localPosition = new Vector3(center.x - bounds.center.x, -bounds.max.y, center.z - bounds.center.z);

        Renderer[] renderers = mesh.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (surface != null)
                renderers[i].sharedMaterial = surface;
        }

        Collider[] colliders = mesh.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    static void HideRenderer(Transform shell, string name)
    {
        Transform piece = shell.Find(name);
        if (piece == null)
            return;

        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;
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

        return found && bounds.size.sqrMagnitude > 0.001f;
    }
}
