using UnityEngine;

public enum ItemKind
{
    Ore,
    Product,
    Ammo
}

/// <summary>
/// Marks a physical object as ore, a finished product, or ammunition.
/// </summary>
public class Item : MonoBehaviour
{
    [SerializeField] ItemKind kind = ItemKind.Ore;

    public ItemKind Kind => kind;

    public void SetKind(ItemKind value)
    {
        kind = value;
    }

    void Awake()
    {
        if (kind == ItemKind.Product)
            IronIngotVisual.Apply(gameObject);
    }
}
