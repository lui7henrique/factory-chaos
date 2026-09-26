using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pushes Ore and Product rigidbodies along local forward. Attach this to the trigger zone.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] float speed = 1f;

    readonly Dictionary<Rigidbody, int> overlaps = new Dictionary<Rigidbody, int>();
    readonly List<Rigidbody> expired = new List<Rigidbody>();

    PlayerCarry playerCarry;

    void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        playerCarry = FindAnyObjectByType<PlayerCarry>();
    }

    void OnTriggerEnter(Collider other)
    {
        ChangeOverlap(other, 1);
    }

    void OnTriggerExit(Collider other)
    {
        ChangeOverlap(other, -1);
    }

    void FixedUpdate()
    {
        Vector3 drive = transform.forward;
        if (drive.sqrMagnitude < 0.0001f)
            return;

        drive.Normalize();
        expired.Clear();

        foreach (KeyValuePair<Rigidbody, int> pair in overlaps)
        {
            Rigidbody body = pair.Key;
            if (body == null)
            {
                expired.Add(body);
                continue;
            }

            if (body.isKinematic || (playerCarry != null && playerCarry.IsCarrying(body)))
                continue;

            Item item = body.GetComponent<Item>();
            if (item == null)
                item = body.GetComponentInParent<Item>();

            if (item == null || (item.Kind != ItemKind.Ore && item.Kind != ItemKind.Product))
                continue;

            Vector3 velocity = body.linearVelocity;
            float along = Vector3.Dot(velocity, drive);
            body.linearVelocity = velocity - drive * along + drive * speed;
        }

        for (int i = 0; i < expired.Count; i++)
            overlaps.Remove(expired[i]);
    }

    void ChangeOverlap(Collider other, int delta)
    {
        if (other == null)
            return;

        Rigidbody body = other.attachedRigidbody;
        if (body == null)
            return;

        overlaps.TryGetValue(body, out int count);
        count += delta;
        if (count <= 0)
            overlaps.Remove(body);
        else
            overlaps[body] = count;
    }
}
