using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Carries one rigidbody in front of the camera. E picks up or drops. Left mouse throws.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerCarry : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] float pickupRange = 3f;
    [SerializeField] float holdDistance = 1.05f;
    [SerializeField] Transform cameraTransform;

    [Header("Throw")]
    [SerializeField] float throwForce = 10f;

    public void BindCamera(Transform camera)
    {
        if (camera != null)
            cameraTransform = camera;
    }

    const float HoldSurfaceGap = 0.25f;

    readonly RaycastHit[] hits = new RaycastHit[16];

    CharacterController playerCollider;
    Rigidbody heldBody;
    Collider[] heldColliders;
    bool inputEnabled = true;
    int ignoreInteractFrame = -1;

    void Awake()
    {
        playerCollider = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    public bool IsHolding => heldBody != null;

    public bool ThrewThisFrame { get; private set; }

    public bool InputEnabled => inputEnabled;

    public bool IsCarrying(Rigidbody body)
    {
        return body != null && heldBody == body;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void IgnoreInteractThisFrame()
    {
        ignoreInteractFrame = Time.frameCount;
    }

    void Update()
    {
        ThrewThisFrame = false;
        if (!inputEnabled)
            return;

        if (GetComponent<PlayerLoadout>() != null)
            return;

        bool interact = Keyboard.current != null
            && Keyboard.current.eKey.wasPressedThisFrame
            && ignoreInteractFrame != Time.frameCount;
        bool throwPressed = Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame
            && Cursor.lockState == CursorLockMode.Locked;

        if (heldBody != null && throwPressed && cameraTransform != null)
        {
            Release(cameraTransform.forward * throwForce);
            ThrewThisFrame = true;
        }
        else if (interact)
        {
            if (heldBody != null)
                Release(Vector3.zero);
            else
                TryPickup();
        }
    }

    void LateUpdate()
    {
        if (heldBody == null || cameraTransform == null)
            return;

        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;
        float distance = holdDistance;
        float nearest = NearestBlockingDistance(origin, direction, holdDistance);

        if (nearest < holdDistance)
            distance = Mathf.Max(HoldSurfaceGap, nearest - HoldSurfaceGap);

        heldBody.position = origin + direction * distance
            + cameraTransform.right * 0.16f
            - cameraTransform.up * 0.2f;
    }

    public void UseHandHold()
    {
        holdDistance = 1.05f;
    }

    public void Hold(Rigidbody body)
    {
        if (body == null)
            return;

        body.gameObject.SetActive(true);
        Pickup(body);
    }

    public void Pocket()
    {
        if (heldBody == null)
            return;

        GameObject pocketed = heldBody.gameObject;
        heldBody = null;
        heldColliders = null;
        pocketed.SetActive(false);
    }

    public void ForgetHeld()
    {
        heldBody = null;
        heldColliders = null;
    }

    public void DropHeld()
    {
        if (heldBody != null)
            Release(Vector3.zero);
    }

    public void ThrowHeld()
    {
        if (heldBody == null || cameraTransform == null)
            return;

        Release(cameraTransform.forward * throwForce);
        ThrewThisFrame = true;
    }

    void TryPickup()
    {
        if (cameraTransform == null)
            return;

        int count = Physics.RaycastNonAlloc(
            cameraTransform.position,
            cameraTransform.forward,
            hits,
            pickupRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;
        Rigidbody target = null;

        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            Rigidbody body = hits[i].rigidbody;
            if (body == null || body.isKinematic)
                continue;

            if (hits[i].distance < nearest)
            {
                nearest = hits[i].distance;
                target = body;
            }
        }

        if (target != null)
            Pickup(target);
    }

    void Pickup(Rigidbody body)
    {
        heldBody = body;
        heldColliders = body.GetComponentsInChildren<Collider>();
        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.isKinematic = true;
        SetIgnorePlayer(heldColliders, true);
    }

    void Release(Vector3 velocity)
    {
        Rigidbody body = heldBody;
        Collider[] colliders = heldColliders;
        heldBody = null;
        heldColliders = null;

        body.isKinematic = false;
        body.linearVelocity = velocity;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();

        StartCoroutine(RestoreCollisionsWhenClear(body, colliders));
    }

    IEnumerator RestoreCollisionsWhenClear(Rigidbody body, Collider[] colliders)
    {
        const int maxFrames = 45;
        int frames = 0;

        while (body != null && heldBody != body && frames < maxFrames && OverlapsPlayer(colliders))
        {
            frames++;
            yield return null;
        }

        if (body != null && heldBody != body)
            SetIgnorePlayer(colliders, false);
    }

    float NearestBlockingDistance(Vector3 origin, Vector3 direction, float maxDistance)
    {
        int count = Physics.RaycastNonAlloc(
            origin,
            direction,
            hits,
            maxDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearest = maxDistance;
        for (int i = 0; i < count; i++)
        {
            if (IsPlayerOrHeld(hits[i].collider))
                continue;

            if (hits[i].distance < nearest)
                nearest = hits[i].distance;
        }

        return nearest;
    }

    bool IsPlayerOrHeld(Collider col)
    {
        if (col == null || col.transform.IsChildOf(transform))
            return true;

        if (heldColliders == null)
            return false;

        for (int i = 0; i < heldColliders.Length; i++)
        {
            if (heldColliders[i] == col)
                return true;
        }

        return false;
    }

    bool OverlapsPlayer(Collider[] colliders)
    {
        if (playerCollider == null || colliders == null)
            return false;

        Bounds playerBounds = playerCollider.bounds;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col != null && col.enabled && col.bounds.Intersects(playerBounds))
                return true;
        }

        return false;
    }

    void SetIgnorePlayer(Collider[] colliders, bool ignore)
    {
        if (playerCollider == null || colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                Physics.IgnoreCollision(colliders[i], playerCollider, ignore);
        }
    }
}
