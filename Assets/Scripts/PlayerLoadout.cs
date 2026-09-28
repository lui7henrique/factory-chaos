using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pickaxe on a small slot, plus three single-item pockets.
/// Prompt strings stay in Portuguese until the game is localized.
/// </summary>
[DefaultExecutionOrder(-20)]
public class PlayerLoadout : MonoBehaviour
{
    public const int SlotCount = 3;

    readonly RaycastHit[] hits = new RaycastHit[16];
    readonly Item[] slots = new Item[SlotCount];

    PlayerCarry carry;
    int selected = -1;
    string depositPrompt;
    string notice;
    float noticeUntil;

    public bool ToolSelected => selected < 0;
    public int SelectedIndex => selected;
    public string DepositPrompt => depositPrompt;

    public Item SlotItem(int index)
    {
        if (index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }

    void Awake()
    {
        carry = GetComponent<PlayerCarry>();
    }

    void Start()
    {
        if (carry != null)
            carry.UseHandHold();
    }

    void Update()
    {
        depositPrompt = null;
        if (carry == null || !carry.InputEnabled)
            return;

        ReadSelection();
        RefreshPrompt();
        ReadActions();
    }

    void ReadSelection()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            Select(-1);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            Select(0);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            Select(1);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            Select(2);
    }

    void ReadActions()
    {
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        bool deposit = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && locked;
        bool throwPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && locked;
        bool interact = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (deposit)
            TryDeposit();

        if (!ToolSelected && carry.IsHolding && throwPressed)
            ThrowSelected();
        else if (interact && !TryStore() && carry.IsHolding)
            DropSelected();
    }

    void RefreshPrompt()
    {
        if (Time.time < noticeUntil)
            depositPrompt = notice;

        Item item = CurrentItem();
        if (item == null || carry == null || !carry.IsHolding)
            return;

        if (!LookAtMachine(out OreMachine furnace, out AmmoMachine ammo))
            return;

        if (item.Kind == ItemKind.Ore && furnace != null && furnace.CanTake(item))
            depositPrompt = "Botão direito — Deposite o minério";
        else if (item.Kind == ItemKind.Product && ammo != null && ammo.CanTake(item))
            depositPrompt = "Botão direito — Deposite o lingote";
    }

    void TryDeposit()
    {
        Item item = CurrentItem();
        if (item == null || !LookAtMachine(out OreMachine furnace, out AmmoMachine ammo))
            return;

        bool ore = item.Kind == ItemKind.Ore && furnace != null && furnace.CanTake(item);
        bool ingot = item.Kind == ItemKind.Product && ammo != null && ammo.CanTake(item);
        if (!ore && !ingot)
            return;

        slots[selected] = null;
        carry.ForgetHeld();
        if (ore)
            furnace.TryDeposit(item);
        else
            ammo.TryDeposit(item);
    }

    bool TryStore()
    {
        Rigidbody body = LookAtItem();
        if (body == null)
            return false;

        Item item = body.GetComponent<Item>();
        if (item == null)
            item = body.GetComponentInParent<Item>();
        if (item == null || !item.enabled)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == item)
                return false;
        }

        int index = FirstEmpty();
        if (index < 0)
        {
            notice = "Inventário cheio";
            noticeUntil = Time.time + 1.2f;
            depositPrompt = notice;
            return true;
        }

        slots[index] = item;
        Stow(body);
        return true;
    }

    static void Stow(Rigidbody body)
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        body.gameObject.SetActive(false);
    }

    int FirstEmpty()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return i;
        }

        return -1;
    }

    void ThrowSelected()
    {
        if (selected < 0)
            return;

        slots[selected] = null;
        carry.ThrowHeld();
    }

    void DropSelected()
    {
        if (selected < 0)
            return;

        slots[selected] = null;
        carry.DropHeld();
    }

    void Select(int index)
    {
        if (selected == index)
        {
            ShowSelected();
            return;
        }

        if (selected >= 0 && slots[selected] != null)
            carry.Pocket();

        selected = index;
        ShowSelected();
    }

    void ShowSelected()
    {
        if (selected < 0 || slots[selected] == null)
            return;

        Rigidbody body = slots[selected].GetComponent<Rigidbody>();
        if (body == null)
            body = slots[selected].GetComponentInParent<Rigidbody>();
        carry.Hold(body);
    }

    Item CurrentItem()
    {
        if (selected < 0 || selected >= slots.Length)
            return null;

        return slots[selected];
    }

    bool LookAtMachine(out OreMachine furnace, out AmmoMachine ammo)
    {
        furnace = null;
        ammo = null;
        if (!Look(3.5f, out RaycastHit hit))
            return false;

        furnace = hit.collider.GetComponentInParent<OreMachine>();
        if (furnace == null)
            furnace = hit.collider.transform.root.GetComponentInChildren<OreMachine>();

        ammo = hit.collider.GetComponentInParent<AmmoMachine>();
        if (ammo == null)
            ammo = hit.collider.transform.root.GetComponentInChildren<AmmoMachine>();

        return furnace != null || ammo != null;
    }

    Rigidbody LookAtItem()
    {
        if (!Look(carry != null ? 3f : 3f, out RaycastHit hit))
            return null;

        Rigidbody body = hit.rigidbody;
        if (body == null || body.isKinematic)
            return null;

        return body;
    }

    bool Look(float range, out RaycastHit chosen)
    {
        chosen = default;
        Camera view = Camera.main;
        if (view == null)
            return false;

        int count = Physics.RaycastNonAlloc(
            view.transform.position,
            view.transform.forward,
            hits,
            range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;
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

        return found;
    }
}
