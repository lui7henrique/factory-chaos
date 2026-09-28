using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manual pickaxe mining. The vein keeps reserve and strike progress.
/// </summary>
[DefaultExecutionOrder(40)]
public class PlayerMining : MonoBehaviour
{
    [SerializeField] float range = 2.5f;
    [SerializeField] float strikeInterval = 0.45f;
    [SerializeField] float swingDuration = 1.15f;
    [SerializeField] Transform cameraTransform;

    readonly RaycastHit[] hits = new RaycastHit[16];

    PlayerCarry carry;
    PickaxeVisual pickaxe;
    OreVein aimed;
    Vector3 aimPoint;
    Vector3 aimNormal;
    bool inputEnabled = true;
    bool swinging;
    bool impactApplied;
    float swingStart;
    float nextSwingTime = -1f;

    public OreVein AimedVein => aimed;

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled)
            CancelSwing();
    }

    void Awake()
    {
        carry = GetComponent<PlayerCarry>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        EnsurePickaxe();
    }

    void Update()
    {
        bool handsFree = carry == null || !carry.IsHolding;
        bool blocked = !inputEnabled || !handsFree || carry != null && !carry.InputEnabled;
        if (pickaxe != null)
            pickaxe.gameObject.SetActive(!blocked);

        if (blocked)
        {
            CancelSwing();
            aimed = null;
            return;
        }

        Aim();
        if (carry != null && carry.ThrewThisFrame)
        {
            CancelSwing();
            return;
        }

        bool holdingButton = Mouse.current != null
            && Mouse.current.leftButton.isPressed
            && Cursor.lockState == CursorLockMode.Locked;

        if (!holdingButton)
        {
            CancelSwing();
            return;
        }

        if (!swinging && aimed != null && aimed.CanMine && Time.time >= nextSwingTime)
            BeginSwing();

        if (!swinging)
            return;

        float motion = Mathf.Max(swingDuration, 1.15f);
        float amount = motion <= 0f ? 1f : (Time.time - swingStart) / motion;
        if (pickaxe != null)
            pickaxe.SetCycle(Mathf.Clamp01(amount));

        if (!impactApplied && amount >= PickaxeVisual.ImpactTime)
        {
            impactApplied = true;
            TryImpact();
        }

        if (amount < 1f)
            return;

        swinging = false;
        nextSwingTime = swingStart + Mathf.Max(strikeInterval, motion);
        if (pickaxe != null)
            pickaxe.SetCycle(0f);
    }

    void TryImpact()
    {
        if (aimed == null || !aimed.CanMine || !StillOnVein(aimed))
            return;

        aimed.ApplyStrike();
        Material chip = null;
        Renderer renderer = aimed.GetComponentInChildren<Renderer>();
        if (renderer != null)
            chip = renderer.sharedMaterial;
        OreChip.Burst(aimPoint, aimNormal, chip);
    }

    void BeginSwing()
    {
        swinging = true;
        impactApplied = false;
        swingStart = Time.time;
        if (pickaxe != null)
            pickaxe.SetCycle(0f);
    }

    void CancelSwing()
    {
        swinging = false;
        impactApplied = false;
        if (pickaxe != null)
            pickaxe.SetCycle(0f);
    }

    void Aim()
    {
        aimed = null;
        if (cameraTransform == null)
            return;

        int count = Physics.RaycastNonAlloc(
            cameraTransform.position,
            cameraTransform.forward,
            hits,
            range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;
        RaycastHit chosen = default;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            if (hits[i].distance < nearest)
            {
                nearest = hits[i].distance;
                chosen = hits[i];
                found = true;
            }
        }

        if (!found)
            return;

        OreVein vein = chosen.collider.GetComponentInParent<OreVein>();
        if (vein == null)
            return;

        aimed = vein;
        aimPoint = chosen.point;
        aimNormal = chosen.normal;
    }

    bool StillOnVein(OreVein vein)
    {
        Aim();
        return aimed == vein;
    }

    void EnsurePickaxe()
    {
        if (cameraTransform == null)
            return;

        Transform existing = cameraTransform.Find("ToolAnchor");
        if (existing != null)
        {
            pickaxe = existing.GetComponent<PickaxeVisual>();
            if (pickaxe == null)
                pickaxe = existing.gameObject.AddComponent<PickaxeVisual>();
            if (existing.Find("SwingPivot/Hand") == null)
                pickaxe.Rebuild(PickaxeVisual.RuntimePalette(), PickaxeMesh.Create(), null);
            pickaxe.UseHeldPose();
            pickaxe.SetCycle(0f);
            return;
        }

        PickaxeVisual.BuildRuntime(cameraTransform);
        pickaxe = cameraTransform.Find("ToolAnchor").GetComponent<PickaxeVisual>();
    }
}
