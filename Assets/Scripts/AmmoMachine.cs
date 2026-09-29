using System.Collections;
using System.Collections.Generic;
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
        SetStatusLamp(lamp);
    }

    [Header("Process")]
    [SerializeField] float processDuration = 2f;
    [SerializeField] Transform outputPoint;
    [SerializeField] GameObject ammoPrefab;
    [SerializeField] float outputClearRadius = 0.34f;
    [SerializeField] float outputImpulse = 0.65f;

    [Header("Status")]
    [SerializeField] Renderer statusRenderer;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly Color AvailableColor = new Color(0.349f, 0.937f, 0.380f);
    static readonly Color ProcessingColor = new Color(1f, 0.749f, 0.212f);
    static readonly Color BlockedColor = new Color(0.9f, 0.22f, 0.18f);

    readonly Collider[] overlaps = new Collider[8];

    PlayerCarry playerCarry;
    MaterialPropertyBlock statusBlock;
    readonly Queue<GameObject> pendingProducts = new Queue<GameObject>();
    Coroutine processRoutine;
    bool pressing;
    bool outputBlocked;
    float processEnd;

    public bool CanTake(Item item)
    {
        return isActiveAndEnabled && ammoPrefab != null && outputPoint != null
            && item != null && item.enabled && item.Kind == ItemKind.Product;
    }

    public bool TryDeposit(Item item)
    {
        if (!CanTake(item))
            return false;

        Accept(item, item.GetComponentInParent<Rigidbody>());
        return true;
    }

    public bool IsPressing => pressing;
    // Includes the product currently in the press, not loose rounds in the room.
    public int PendingCount => pendingProducts.Count;
    public bool IsOutputBlocked => outputBlocked;
    public float ProcessDuration => processDuration;
    public float ProcessSecondsLeft => pressing ? Mathf.Max(0f, processEnd - Time.time) : 0f;

    public void SetStatusLamp(Renderer lamp)
    {
        statusRenderer = lamp;
        if (outputBlocked) ShowBlocked();
        else if (pressing) ShowProcessing();
        else ShowAvailable();
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
        pressing = false;
        outputBlocked = false;
        if (processRoutine != null)
        {
            StopCoroutine(processRoutine);
            processRoutine = null;
        }
    }

    void OnEnable()
    {
        if (pendingProducts.Count > 0 && processRoutine == null)
            processRoutine = StartCoroutine(ProcessQueue());
    }

    void OnDestroy()
    {
        while (pendingProducts.Count > 0)
        {
            GameObject product = pendingProducts.Dequeue();
            if (product != null) Destroy(product);
        }
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
        if (other == null)
            return;

        Item item = other.GetComponentInParent<Item>();
        if (!CanTake(item))
            return;

        Rigidbody body = other.attachedRigidbody;
        if (playerCarry != null && playerCarry.IsCarrying(body))
            return;

        Accept(item, body);
    }

    void Accept(Item item, Rigidbody body)
    {
        item.enabled = false;
        GameObject product = body != null ? body.gameObject : item.gameObject;
        PrepareQueuedProduct(product, body);
        pendingProducts.Enqueue(product);
        GameFeedback.Play(GameFeedback.Cue.Deposit);
        if (processRoutine == null)
            processRoutine = StartCoroutine(ProcessQueue());
    }

    IEnumerator ProcessQueue()
    {
        while (pendingProducts.Count > 0)
        {
            GameObject product = pendingProducts.Peek();
            pressing = true;
            outputBlocked = false;
            processEnd = Time.time + processDuration;
            ShowProcessing();
            yield return new WaitForSeconds(processDuration);
            pressing = false;

            if (ammoPrefab == null || outputPoint == null)
            {
                Debug.LogWarning("AmmoMachine needs an ammo prefab and an output point.", this);
                break;
            }

            while (OutputBlocked())
            {
                outputBlocked = true;
                ShowBlocked();
                yield return null;
            }

            outputBlocked = false;

            pendingProducts.Dequeue();
            if (product != null)
                Destroy(product);

            GameObject ammo = Instantiate(ammoPrefab, outputPoint.position, outputPoint.rotation);
            ammo.SetActive(true);
            EjectAmmo(ammo);
            GameFeedback.Notify("MUNIÇÃO PRONTA", "Pegue o cartucho e carregue o canhão.");
        }

        processRoutine = null;
        ShowAvailable();
    }

    void PrepareQueuedProduct(GameObject product, Rigidbody body)
    {
        if (product == null)
            return;

        if (body != null)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
        }

        Collider[] colliders = product.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        int slot = pendingProducts.Count % 3;
        float offset = (slot - 1) * 0.28f;
        product.transform.SetPositionAndRotation(
            transform.position + transform.up * 0.34f + transform.forward * offset,
            transform.rotation);
    }

    void EjectAmmo(GameObject ammo)
    {
        if (ammo == null)
            return;

        Rigidbody body = ammo.GetComponent<Rigidbody>();
        if (body == null)
            return;

        Vector3 direction = outputPoint.right;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();
        float clearance = outputClearRadius + ProjectedExtent(ammo, direction) + 0.02f;
        body.position = outputPoint.position + direction * clearance;

        if (outputImpulse > 0f)
            body.AddForce(direction * outputImpulse, ForceMode.Impulse);
    }

    float ProjectedExtent(GameObject target, Vector3 direction)
    {
        float largest = 0f;
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].enabled)
                continue;

            Vector3 extents = colliders[i].bounds.extents;
            float projected = Mathf.Abs(direction.x) * extents.x
                + Mathf.Abs(direction.y) * extents.y
                + Mathf.Abs(direction.z) * extents.z;
            largest = Mathf.Max(largest, projected);
        }

        return largest;
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
            if (overlap != null && !overlap.transform.IsChildOf(transform.parent != null ? transform.parent : transform))
                return true;
        }

        return false;
    }

    Renderer FindBodyRenderer()
    {
        Transform station = transform.parent != null ? transform.parent : transform;
        Transform lens = station.Find("Visuals/StatusLight/Lens");
        return lens != null ? lens.GetComponent<Renderer>() : null;
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
        if (statusRenderer == null)
            return;
        if (statusBlock == null) statusBlock = new MaterialPropertyBlock();
        statusRenderer.GetPropertyBlock(statusBlock);
        statusBlock.SetColor(BaseColorId, color);
        statusBlock.SetColor(EmissionColorId, color * 0.45f);
        statusRenderer.SetPropertyBlock(statusBlock);
    }
}
