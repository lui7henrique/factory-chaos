using UnityEngine;

/// <summary>
/// Ground ore deposit. Tracks reserve and strike progress. Does not read player input.
/// </summary>
public class OreVein : MonoBehaviour
{
    [SerializeField] int reserve = 20;
    [SerializeField] int strikesPerUnit = 3;
    [SerializeField] GameObject orePrefab;
    [SerializeField] float spawnRadius = 0.9f;
    [SerializeField] float spawnClearRadius = 0.22f;

    readonly Collider[] overlaps = new Collider[8];

    int strikes;
    bool dropWaiting;
    bool spent;

    public int Reserve => reserve;
    public int Strikes => strikes;
    public int StrikesPerUnit => Mathf.Max(1, strikesPerUnit);
    public bool DropWaiting => dropWaiting;
    public bool IsDepleted => spent || (reserve <= 0 && !dropWaiting);
    public bool CanMine => !IsDepleted;

    public void Configure(GameObject prefab)
    {
        if (prefab != null)
            orePrefab = prefab;
    }

    void Update()
    {
        if (dropWaiting)
            TryReleaseDrop();
    }

    public void ApplyStrike()
    {
        if (IsDepleted)
            return;

        if (dropWaiting)
        {
            TryReleaseDrop();
            return;
        }

        strikes++;
        if (strikes < StrikesPerUnit)
            return;

        strikes = 0;
        dropWaiting = true;
        TryReleaseDrop();
    }

    void TryReleaseDrop()
    {
        if (!dropWaiting || orePrefab == null)
            return;

        if (!TrySpawn(orePrefab))
            return;

        dropWaiting = false;
        reserve = Mathf.Max(0, reserve - 1);
        if (reserve <= 0)
            MarkSpent();
    }

    bool TrySpawn(GameObject prefab)
    {
        float lift = BottomLift(prefab);
        for (int ring = 0; ring < 2; ring++)
        {
            float radius = spawnRadius + ring * 0.45f;
            int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float angle = (i / (float)steps) * Mathf.PI * 2f + ring * 0.4f;
                Vector3 position = transform.position + new Vector3(Mathf.Cos(angle) * radius, lift, Mathf.Sin(angle) * radius);
                if (!IsClear(position))
                    continue;

                Quaternion rotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                Instantiate(prefab, position, rotation);
                return true;
            }
        }

        return false;
    }

    bool IsClear(Vector3 position)
    {
        int count = Physics.OverlapSphereNonAlloc(
            position,
            spawnClearRadius,
            overlaps,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap != null && !overlap.transform.IsChildOf(transform))
                return false;
        }

        return true;
    }

    static float BottomLift(GameObject prefab)
    {
        BoxCollider box = prefab.GetComponent<BoxCollider>();
        if (box == null)
            return 0.2f;

        Vector3 scale = prefab.transform.localScale;
        return -(box.center.y - box.size.y * 0.5f) * scale.y + 0.04f;
    }

    void MarkSpent()
    {
        if (spent)
            return;

        spent = true;
        OreVeinVisual visual = GetComponent<OreVeinVisual>();
        if (visual != null)
            visual.SetSpent();
    }
}
