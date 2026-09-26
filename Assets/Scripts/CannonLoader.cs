using UnityEngine;

/// <summary>
/// Loads one loose Ammo into the cannon. Attach this to the input trigger.
/// </summary>
public class CannonLoader : MonoBehaviour
{
    [SerializeField] CannonController cannon;

    PlayerCarry playerCarry;

    void Awake()
    {
        if (cannon == null)
            cannon = GetComponentInParent<CannonController>();

        playerCarry = FindAnyObjectByType<PlayerCarry>();
    }

    void OnTriggerEnter(Collider other)
    {
        TryLoad(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryLoad(other);
    }

    void TryLoad(Collider other)
    {
        if (cannon == null || other == null || cannon.IsFull)
            return;

        Item item = other.GetComponentInParent<Item>();
        if (item == null || !item.enabled || item.Kind != ItemKind.Ammo)
            return;

        Rigidbody body = other.attachedRigidbody;
        if (playerCarry != null && playerCarry.IsCarrying(body))
            return;

        if (!cannon.TryAddRound())
            return;

        item.enabled = false;
        Destroy(body != null ? body.gameObject : item.gameObject);
    }
}
