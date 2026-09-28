using UnityEngine;

/// <summary>
/// Short-lived chip from a pickaxe hit. No collider and no rigidbody.
/// </summary>
public class OreChip : MonoBehaviour
{
    Vector3 velocity;
    float life;

    public void Launch(Vector3 direction, float seconds, Material material)
    {
        velocity = direction;
        life = seconds;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        velocity += Vector3.down * 9f * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(420f * Time.deltaTime, 260f * Time.deltaTime, 0f, Space.Self);
    }

    public static void Burst(Vector3 point, Vector3 normal, Material material)
    {
        int count = 6;
        for (int i = 0; i < count; i++)
        {
            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.name = "OreChip";
            chip.transform.position = point + normal * 0.02f;
            chip.transform.localScale = Vector3.one * Random.Range(0.035f, 0.06f);
            chip.transform.rotation = Random.rotation;

            Collider collider = chip.GetComponent<Collider>();
            if (collider != null)
                DestroyImmediate(collider);

            Vector3 spray = (normal + Random.insideUnitSphere * 0.65f).normalized * Random.Range(1.4f, 2.6f);
            chip.AddComponent<OreChip>().Launch(spray, 0.35f, material);
        }
    }
}
