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
    static readonly Color AvailableColor = new Color(0.349f, 0.937f, 0.380f);
    static readonly Color ProcessingColor = new Color(1f, 0.749f, 0.212f);

    PlayerCarry playerCarry;
    MaterialPropertyBlock statusBlock;
    bool busy;
    float processEnd;
    Coroutine processRoutine;
    float remainingOnDisable;
    readonly Collider[] outputOverlaps = new Collider[16];
    bool outputBlocked;

    public bool CanTake(Item item)
    {
        return isActiveAndEnabled && productPrefab != null && outputPoint != null
            && !busy && item != null && item.enabled && item.Kind == ItemKind.Ore;
    }

    public bool TryDeposit(Item item)
    {
        if (!CanTake(item))
            return false;

        item.enabled = false;
        Rigidbody body = item.GetComponentInParent<Rigidbody>();
        Accept(body != null ? body.gameObject : item.gameObject);
        return true;
    }

    public bool IsProcessing => busy;
    public bool IsOutputBlocked => outputBlocked;
    public float ProcessDuration => processDuration;
    public float ProcessSecondsLeft => busy ? Mathf.Max(0f, processEnd - Time.time) : 0f;

    public void Configure(Transform output, GameObject product)
    {
        if (output != null)
            outputPoint = output;
        if (product != null)
            productPrefab = product;
        if (statusRenderer == null) AssignStatus(FindBodyRenderer());
    }

    public void AssignStatus(Renderer renderer)
    {
        statusRenderer = renderer;
        if (busy)
            ShowProcessing();
        else
            ShowAvailable();
    }

    void Awake()
    {
        playerCarry = FindAnyObjectByType<PlayerCarry>();

        if (statusRenderer == null)
            statusRenderer = FindBodyRenderer();

        ShowAvailable();
    }

    void OnDisable()
    {
        remainingOnDisable = Mathf.Max(0f, processEnd - Time.time);
        if (processRoutine != null) StopCoroutine(processRoutine);
        processRoutine = null;
    }

    void OnEnable()
    {
        if (!busy || processRoutine != null) return;
        processEnd = Time.time + remainingOnDisable;
        processRoutine = StartCoroutine(Process());
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
        if (!CanTake(item))
            return;

        Rigidbody body = other.attachedRigidbody;
        if (playerCarry != null && playerCarry.IsCarrying(body))
            return;

        TryDeposit(item);
    }

    void Accept(GameObject target)
    {
        busy = true;
        processEnd = Time.time + processDuration;
        Destroy(target);
        ShowProcessing();
        GameFeedback.Play(GameFeedback.Cue.Deposit);
        processRoutine = StartCoroutine(Process());
    }

    IEnumerator Process()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, processEnd - Time.time));

        while (OutputBlocked())
        {
            outputBlocked = true;
            SetStatusColor(new Color(1f, 0.42f, 0.17f));
            yield return null;
        }
        outputBlocked = false;

        if (productPrefab != null && outputPoint != null)
        {
            GameObject product = Instantiate(productPrefab, outputPoint.position, outputPoint.rotation);
            product.SetActive(true);
            GameFeedback.Notify("LINGOTE PRONTO", "Leve à prensa para produzir munição ou à entrega para vender.");
        }
        else
            Debug.LogWarning("OreMachine needs a product prefab and an output point.", this);

        busy = false;
        processRoutine = null;
        ShowAvailable();
    }

    Renderer FindBodyRenderer()
    {
        return FurnaceVisual.FindLens(transform.parent != null ? transform.parent : transform);
    }

    bool OutputBlocked()
    {
        if (outputPoint == null) return false;
        int count = Physics.OverlapSphereNonAlloc(outputPoint.position, 0.26f, outputOverlaps,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (outputOverlaps[i] != null && outputOverlaps[i].GetComponentInParent<Item>() != null) return true;
        return false;
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
        if (statusRenderer == null)
            return;
        if (statusBlock == null) statusBlock = new MaterialPropertyBlock();
        statusRenderer.GetPropertyBlock(statusBlock);
        statusBlock.SetColor(BaseColorId, color);
        statusBlock.SetColor(EmissionColorId, color * 0.65f);
        statusRenderer.SetPropertyBlock(statusBlock);
    }
}
