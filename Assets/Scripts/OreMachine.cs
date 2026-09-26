using System.Collections;
using UnityEngine;

/// <summary>
/// Turns one Ore into one Product. Attach this to the input trigger.
/// </summary>
public class OreMachine : MonoBehaviour
{
    [Header("Process")]
    [SerializeField] float processDuration = 3f;
    [SerializeField] Transform outputPoint;
    [SerializeField] GameObject productPrefab;

    [Header("Status")]
    [SerializeField] Renderer statusRenderer;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly Color AvailableColor = new Color(0.18f, 0.75f, 0.22f);
    static readonly Color ProcessingColor = new Color(0.95f, 0.78f, 0.12f);

    PlayerCarry playerCarry;
    Material statusMaterial;
    bool busy;

    void Awake()
    {
        playerCarry = FindAnyObjectByType<PlayerCarry>();

        if (statusRenderer == null)
            statusRenderer = FindBodyRenderer();

        if (statusRenderer != null)
            statusMaterial = statusRenderer.material;

        ShowAvailable();
    }

    void OnDestroy()
    {
        if (statusMaterial != null)
            Destroy(statusMaterial);
    }

    void OnTriggerEnter(Collider other)
    {
        TryConsume(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryConsume(other);
    }

    void TryConsume(Collider other)
    {
        if (busy || other == null)
            return;

        Item item = other.GetComponentInParent<Item>();
        if (item == null || item.Kind != ItemKind.Ore)
            return;

        Rigidbody body = other.attachedRigidbody;
        if (playerCarry != null && playerCarry.IsCarrying(body))
            return;

        busy = true;
        Destroy(body != null ? body.gameObject : item.gameObject);
        ShowProcessing();
        StartCoroutine(Process());
    }

    IEnumerator Process()
    {
        yield return new WaitForSeconds(processDuration);

        if (productPrefab != null && outputPoint != null)
            Instantiate(productPrefab, outputPoint.position, outputPoint.rotation);
        else
            Debug.LogWarning("OreMachine needs a product prefab and an output point.", this);

        busy = false;
        ShowAvailable();
    }

    Renderer FindBodyRenderer()
    {
        Renderer[] renderers = transform.root.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Collider collider = renderers[i].GetComponent<Collider>();
            if (collider == null || !collider.isTrigger)
                return renderers[i];
        }

        return null;
    }

    void ShowAvailable()
    {
        SetStatusColor(AvailableColor);
    }

    void ShowProcessing()
    {
        SetStatusColor(ProcessingColor);
    }

    void SetStatusColor(Color color)
    {
        if (statusMaterial == null)
            return;

        statusMaterial.SetColor(BaseColorId, color);
        statusMaterial.EnableKeyword("_EMISSION");
        statusMaterial.SetColor(EmissionColorId, color * 0.45f);
    }
}
