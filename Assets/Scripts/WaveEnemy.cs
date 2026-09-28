using UnityEngine;

/// <summary>
/// One walker that leaves the tunnel and heads for the factory. The cannon shot is what stops it.
/// </summary>
public class WaveEnemy : MonoBehaviour
{
    [SerializeField] float maxHealth = 60f;
    [SerializeField] float speed = 1.15f;
    [SerializeField] float stopZ = 8f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    const float FlashDuration = 0.12f;

    Renderer bodyRenderer;
    Material bodyMaterial;
    Color bodyColor = new Color(0.28f, 0.3f, 0.33f);
    float health;
    float flash;
    bool dead;
    bool stopped;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsDead => dead;
    public bool HasArrived => !dead && transform.position.z <= stopZ;

    public void Configure(float goalZ, Renderer body)
    {
        stopZ = goalZ;
        bodyRenderer = body;
        if (bodyMaterial != null)
            Destroy(bodyMaterial);

        bodyMaterial = null;
        if (bodyRenderer == null)
            return;

        bodyMaterial = bodyRenderer.material;
        if (bodyMaterial.HasProperty(BaseColorId))
            bodyColor = bodyMaterial.GetColor(BaseColorId);
    }

    void Awake()
    {
        health = maxHealth;
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();
        if (bodyRenderer != null)
        {
            bodyMaterial = bodyRenderer.material;
            if (bodyMaterial.HasProperty(BaseColorId))
                bodyColor = bodyMaterial.GetColor(BaseColorId);
        }
    }

    void OnDestroy()
    {
        if (bodyMaterial != null)
            Destroy(bodyMaterial);
    }

    public void TakeDamage(float amount)
    {
        if (dead || amount <= 0f)
            return;

        health = Mathf.Max(0f, health - amount);
        flash = FlashDuration;
        if (health <= 0f)
            Die();
    }

    void Update()
    {
        if (!dead && !stopped && transform.position.z > stopZ)
        {
            transform.position += Vector3.back * (speed * Time.deltaTime);
            Transform visual = transform.Find("Body");
            if (visual != null)
            {
                float bob = Mathf.Sin(Time.time * 8f) * 0.04f;
                Vector3 local = visual.localPosition;
                local.y = 0.85f + bob;
                visual.localPosition = local;
            }
        }

        if (bodyMaterial == null || dead)
            return;

        if (flash > 0f)
        {
            flash -= Time.deltaTime;
            Paint(Color.Lerp(bodyColor, Color.white, Mathf.Clamp01(flash / FlashDuration)));
        }
        else
        {
            Paint(bodyColor);
        }
    }

    void Die()
    {
        dead = true;
        stopped = true;
        Paint(new Color(0.16f, 0.15f, 0.15f));
        transform.rotation = Quaternion.Euler(80f, 0f, 0f);

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    void Paint(Color color)
    {
        if (bodyMaterial == null)
            return;

        bodyMaterial.SetColor(BaseColorId, color);
        bodyMaterial.SetColor(ColorId, color);
    }
}
