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

    Material boardMaterial;
    Color baseColor = new Color(0.75f, 0.18f, 0.16f);
    float health;
    float flash;
    bool destroyed;
    GUIStyle labelStyle;

    public void Configure(Renderer boardRenderer)
    {
        board = boardRenderer;
    }

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsDestroyed => destroyed;

    void Awake()
    {
        health = maxHealth;

        if (board == null)
            board = GetComponentInChildren<Renderer>();

        if (board != null)
        {
            boardMaterial = board.material;
            if (boardMaterial.HasProperty(BaseColorId))
                baseColor = boardMaterial.GetColor(BaseColorId);
        }
    }

    void OnDestroy()
    {
        if (boardMaterial != null)
            Destroy(boardMaterial);
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
        if (boardMaterial == null || destroyed)
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
        if (boardMaterial == null)
            return;

        boardMaterial.SetColor(BaseColorId, color);
        boardMaterial.SetColor("_Color", color);
    }

    void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 18;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
        }

        float width = 240f;
        float x = (Screen.width - width) * 0.5f;
        string title = destroyed ? "Alvo destruído" : $"Alvo: {Mathf.CeilToInt(health)}";
        GUI.Label(new Rect(x, 16f, width, 24f), title, labelStyle);

        Rect bar = new Rect(x, 42f, width, 16f);
        Color previous = GUI.color;
        GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);

        float fraction = maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
        GUI.color = destroyed ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.82f, 0.22f, 0.18f);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
