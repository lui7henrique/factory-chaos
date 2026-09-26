using UnityEngine;

/// <summary>
/// Consumes one Product dropped in the trigger and adds its value to the balance.
/// Attach this to the trigger above the visible base.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DeliveryZone : MonoBehaviour
{
    [SerializeField] int valuePerProduct = 10;

    PlayerCarry playerCarry;
    int money;
    GUIStyle labelStyle;

    void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        playerCarry = FindAnyObjectByType<PlayerCarry>();
    }

    void OnTriggerEnter(Collider other)
    {
        TryDeliver(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryDeliver(other);
    }

    void TryDeliver(Collider other)
    {
        if (other == null)
            return;

        Item item = other.GetComponentInParent<Item>();
        if (item == null || !item.enabled || item.Kind != ItemKind.Product)
            return;

        Rigidbody body = other.attachedRigidbody;
        if (playerCarry != null && playerCarry.IsCarrying(body))
            return;

        item.enabled = false;
        money += valuePerProduct;
        Destroy(body != null ? body.gameObject : item.gameObject);
    }

    void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 22;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(16f, 16f, 320f, 36f), $"Dinheiro: ${money}", labelStyle);
    }
}
