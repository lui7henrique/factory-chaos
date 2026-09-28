using UnityEngine;

/// <summary>
/// Spawns ore prefabs once in a natural pile when play starts.
/// </summary>
public class OreSpawner : MonoBehaviour
{
    [SerializeField] GameObject orePrefab;
    [SerializeField] int count = 10;
    [SerializeField] float spacing = 0.42f;
    [SerializeField] float layerHeight = 0.32f;
    [SerializeField] bool spawnOnStart;

    public GameObject OrePrefab => orePrefab;

    public void AssignPrefab(GameObject prefab)
    {
        if (prefab != null)
            orePrefab = prefab;
    }

    const float GroundClearance = 0.15f;

    void Start()
    {
        if (!spawnOnStart)
            return;

        if (orePrefab == null)
        {
            Debug.LogWarning("OreSpawner needs an ore prefab.", this);
            return;
        }

        if (count <= 0 || spacing <= 0f || layerHeight <= 0f)
            return;

        float lift = GroundClearance - PrefabBottomOffset(orePrefab);
        int remaining = count;
        int layer = 0;

        while (remaining > 0)
        {
            int inLayer = CountInLayer(remaining, layer);
            float radius = inLayer <= 1 ? 0f : spacing * (inLayer >= 5 ? 1f : 0.62f);
            float y = lift + layer * layerHeight;

            for (int i = 0; i < inLayer; i++)
            {
                float angle = inLayer == 1
                    ? 0f
                    : (i / (float)inLayer) * Mathf.PI * 2f + layer * 0.4f;
                float jitter = spacing * 0.06f;
                Vector3 local = new Vector3(
                    Mathf.Cos(angle) * radius + Random.Range(-jitter, jitter),
                    y,
                    Mathf.Sin(angle) * radius + Random.Range(-jitter, jitter));

                Vector3 position = transform.position + transform.rotation * local;
                Quaternion rotation = Quaternion.Euler(
                    Random.Range(-14f, 14f),
                    Random.Range(0f, 360f),
                    Random.Range(-14f, 14f));

                Instantiate(orePrefab, position, rotation, transform);
            }

            remaining -= inLayer;
            layer++;
        }
    }

    static int CountInLayer(int remaining, int layer)
    {
        if (remaining <= 1)
            return remaining;

        if (layer == 0)
        {
            if (remaining >= 6)
                return 6;
            if (remaining >= 4)
                return 3;
            return remaining;
        }

        if (remaining >= 4)
            return 3;

        return remaining;
    }

    static float PrefabBottomOffset(GameObject prefab)
    {
        BoxCollider box = prefab.GetComponentInChildren<BoxCollider>();
        if (box == null)
            return 0f;

        Vector3 scale = box.transform.lossyScale;
        return (box.center.y - box.size.y * 0.5f) * scale.y;
    }
}
