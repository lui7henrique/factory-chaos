using System.Collections;
using UnityEngine;

/// <summary>
/// Turns one loose Product into one Ammo. Attach this to the input trigger.
/// </summary>
public class AmmoMachine : MonoBehaviour
{
    public void Configure(Transform output, GameObject prefab, Renderer lamp)
    {
        outputPoint = output;
        ammoPrefab = prefab;
        statusRenderer = lamp;
    }

    [Header("Process")]
    [SerializeField] float processDuration = 2f;
    [SerializeField] Transform outputPoint;
    [SerializeField] GameObject ammoPrefab;
    [SerializeField] float outputClearRadius = 0.34f;

    [Header("Status")]
    [SerializeField] Renderer statusRenderer;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly Color AvailableColor = new Color(0.349f, 0.937f, 0.380f);
    static readonly Color ProcessingColor = new Color(1f, 0.749f, 0.212f);
    static readonly Color BlockedColor = new Color(0.9f, 0.22f, 0.18f);

    readonly Collider[] overlaps = new Collider[8];

    PlayerCarry playerCarry;
    Material statusMaterial;
    bool busy;
    bool pressing;

    public bool IsPressing => pressing;

    public void SetStatusLamp(Renderer lamp)
    {
        statusRenderer = lamp;
    }

    void Awake()
    {
        playerCarry = FindAnyObjectByType<PlayerCarry>();

        if (statusRenderer == null)
            statusRenderer = FindBodyRenderer();

        if (statusRenderer != null)
            statusMaterial = statusRenderer.material;

        ShowAvailable();
    }

    void OnDisable()
    {
        pressing = false;
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
        if (item == null || !item.enabled || item.Kind != ItemKind.Product)
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
        pressing = true;
        yield return new WaitForSeconds(processDuration);
        pressing = false;

        if (ammoPrefab == null || outputPoint == null)
        {
            Debug.LogWarning("AmmoMachine needs an ammo prefab and an output point.", this);
            busy = false;
            ShowAvailable();
            yield break;
        }

        while (OutputBlocked())
        {
            ShowBlocked();
            yield return null;
        }

        GameObject ammo = Instantiate(ammoPrefab, outputPoint.position, outputPoint.rotation);
        ammo.SetActive(true);
        busy = false;
        ShowAvailable();
    }

    bool OutputBlocked()
    {
        if (outputPoint == null || outputClearRadius <= 0f)
            return false;

        int count = Physics.OverlapSphereNonAlloc(
            outputPoint.position,
            outputClearRadius,
            overlaps,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap != null && !overlap.transform.IsChildOf(transform.root))
                return true;
        }

        return false;
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

    void ShowBlocked()
    {
        SetStatusColor(BlockedColor);
    }

    void SetStatusColor(Color color)
    {
        if (statusMaterial == null)
            return;

        statusMaterial.SetColor(BaseColorId, color);
        statusMaterial.SetColor("_Color", color);
        statusMaterial.EnableKeyword("_EMISSION");
        statusMaterial.SetColor(EmissionColorId, color * 0.45f);
    }
}
