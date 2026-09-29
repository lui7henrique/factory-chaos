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

    Renderer[] renderers;
    Color[] colors;
    MaterialPropertyBlock block;
    Transform bodyVisual, leftLeg, rightLeg, leftArm, rightArm;
    Quaternion deathRotation;
    float deathStarted;
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
        CacheVisuals();
    }

    void Awake()
    {
        health = maxHealth;
        CacheVisuals();
    }

    void CacheVisuals()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colors = new Color[renderers.Length];
        block = new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            colors[i] = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.gray;
        }
        bodyVisual = transform.Find("Body");
        leftLeg = transform.Find("LegL"); rightLeg = transform.Find("LegR");
        leftArm = transform.Find("ArmL"); rightArm = transform.Find("ArmR");
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
        if (dead)
        {
            float t = Mathf.SmoothStep(0f, 1f, (Time.time - deathStarted) / 0.32f);
            transform.rotation = deathRotation * Quaternion.Euler(80f * t, 0f, 0f);
            return;
        }
        if (!dead && !stopped && transform.position.z > stopZ)
        {
            Vector3 position = transform.position;
            position.z = Mathf.Max(stopZ, position.z - speed * Time.deltaTime);
            transform.position = position;
            float step = Mathf.Sin(Time.time * 8f);
            if (bodyVisual != null)
            {
                Vector3 local = bodyVisual.localPosition;
                local.y = 0.85f + Mathf.Abs(step) * 0.035f;
                bodyVisual.localPosition = local;
            }
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(step * 18f, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-step * 18f, 0f, 0f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-step * 12f, 0f, -8f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(step * 12f, 0f, 8f);
        }
        if (flash > 0f)
        {
            flash -= Time.deltaTime;
            Paint(Mathf.Clamp01(flash / FlashDuration), false);
        }
    }

    void Die()
    {
        dead = true;
        stopped = true;
        Paint(0f, true);
        deathRotation = transform.rotation;
        deathStarted = Time.time;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    void Paint(float flashAmount, bool fallen)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color color = fallen ? colors[i] * 0.45f : Color.Lerp(colors[i], Color.white, flashAmount);
            color.a = 1f;
            renderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            if (fallen) block.SetColor("_EmissionColor", Color.black);
            renderers[i].SetPropertyBlock(block);
        }
    }
}
