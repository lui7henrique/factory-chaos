using UnityEngine;

/// <summary>
/// Static practice target. Five basic shots destroy it at the default values.
/// </summary>
public class TargetDummy : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] Renderer board;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly Color DestroyedColor = new Color(0.18f, 0.16f, 0.16f);
    const float FlashDuration = 0.12f;

    MaterialPropertyBlock block;
    Color baseColor = new Color(0.75f, 0.18f, 0.16f);
    float health;
    float flash;
    bool destroyed;

    public void Configure(Renderer boardRenderer)
    {
        board = boardRenderer;
        CacheBoard();
    }

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsDestroyed => destroyed;

    void Awake()
    {
        health = maxHealth;

        if (board == null)
            board = GetComponentInChildren<Renderer>();

        CacheBoard();
    }

    void CacheBoard()
    {
        if (board == null) return;
        block = new MaterialPropertyBlock();
        Material material = board.sharedMaterial;
        if (material != null && material.HasProperty(BaseColorId)) baseColor = material.GetColor(BaseColorId);
    }

    public void TakeDamage(float amount)
    {
        if (destroyed || amount <= 0f)
            return;

        health = Mathf.Max(0f, health - amount);
        flash = FlashDuration;

        if (health <= 0f)
            DestroyTarget();
    }

    void Update()
    {
        if (board == null || destroyed)
            return;

        if (flash > 0f)
        {
            flash -= Time.deltaTime;
            Color color = Color.Lerp(baseColor, Color.white, Mathf.Clamp01(flash / FlashDuration));
            SetBoardColor(color);
        }
        else
        {
            SetBoardColor(baseColor);
        }
    }

    void DestroyTarget()
    {
        destroyed = true;
        SetBoardColor(DestroyedColor);

        if (board != null)
            board.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    void SetBoardColor(Color color)
    {
        if (board == null)
            return;
        if (block == null) block = new MaterialPropertyBlock();
        board.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor("_Color", color);
        board.SetPropertyBlock(block);
    }
}
