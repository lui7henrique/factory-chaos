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
    public string DepositTitle { get; private set; }
    public string DepositDetail { get; private set; }
    public bool DepositAvailable { get; private set; }

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
        DepositTitle = DepositDetail = null;
        DepositAvailable = false;
        if (carry == null || !carry.InputEnabled || GamePauseMenu.IsOpen)
            return;

        ReadSelection();
        RefreshPrompt();
        ReadActions();
    }

    void ReadSelection()
    {
        if (Keyboard.current == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            Select(-1);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            Select(0);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            Select(1);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            Select(2);
        else if (Mouse.current != null && Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.01f)
        {
            int direction = Mouse.current.scroll.ReadValue().y > 0f ? -1 : 1;
            Select((selected + 1 + direction + SlotCount + 1) % (SlotCount + 1) - 1);
        }
    }

    void ReadActions()
    {
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        bool deposit = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && locked;
        bool throwPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && locked;
        bool interact = locked && carry.CanInteractThisFrame && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (deposit) { TryDeposit(); return; }

        if (!ToolSelected && carry.IsHolding && throwPressed)
            ThrowSelected();
        else if (interact && !TryStore() && carry.IsHolding)
            DropSelected();
    }

    void RefreshPrompt()
    {
        if (Time.time < noticeUntil)
        {
            depositPrompt = notice;
            DepositTitle = notice;
            DepositDetail = "Selecione um espaço ocupado e solte um item.";
        }

        Item item = CurrentItem();
        if (item == null || carry == null || !carry.IsHolding)
            return;

        ReadDepositTarget(item, false);
    }

    void TryDeposit()
    {
        Item item = CurrentItem();
        if (item == null || !carry.IsHolding)
            return;
        if (ReadDepositTarget(item, true))
        {
            slots[selected] = null;
            carry.ForgetHeld();
            depositPrompt = null;
            DepositTitle = DepositDetail = null;
        }
        else if (!string.IsNullOrEmpty(DepositTitle)) GameFeedback.Play(GameFeedback.Cue.Warning);
    }

    bool ReadDepositTarget(Item item, bool execute)
    {
        if (!Look(3.5f, out RaycastHit hit)) return false;
        Transform target = hit.collider.transform;
        OreMachine furnace = FindMachineInput<OreMachine>(target);
        AmmoMachine press = FindMachineInput<AmmoMachine>(target);
        CannonController cannon = target.GetComponentInParent<CannonController>();
        DeliveryZone delivery = FindMachineInput<DeliveryZone>(target);
        bool accepted = false;
        if (furnace != null)
        {
            DepositAvailable = furnace.CanTake(item);
            DepositTitle = item.Kind != ItemKind.Ore ? "A FORNALHA ACEITA MINÉRIO"
                : furnace.IsProcessing ? "AGUARDE A FUNDIÇÃO" : "DEPOSITAR MINÉRIO";
            DepositDetail = furnace.IsProcessing ? "O ciclo atual precisa terminar." : "1 minério → 1 lingote";
            if (execute && DepositAvailable) accepted = furnace.TryDeposit(item);
        }
        else if (press != null)
        {
            DepositAvailable = press.CanTake(item);
            DepositTitle = DepositAvailable ? "DEPOSITAR LINGOTE" : "A PRENSA ACEITA LINGOTES";
            DepositDetail = "1 lingote → 1 munição · fila: " + press.PendingCount;
            if (execute && DepositAvailable) accepted = press.TryDeposit(item);
        }
        else if (cannon != null)
        {
            DepositAvailable = item.Kind == ItemKind.Ammo && !cannon.IsFull;
            DepositTitle = item.Kind != ItemKind.Ammo ? "O CANHÃO ACEITA MUNIÇÃO" : cannon.IsFull ? "CANHÃO CHEIO" : "CARREGAR CANHÃO";
            DepositDetail = cannon.Rounds + " / " + cannon.Capacity + " tiros · um cartucho por carga";
            if (execute && DepositAvailable) accepted = cannon.TryDeposit(item);
        }
        else if (delivery != null)
        {
            DepositAvailable = item.Kind == ItemKind.Product;
            DepositTitle = DepositAvailable ? "ENTREGAR LINGOTE" : "A ENTREGA ACEITA LINGOTES";
            DepositDetail = "+$ " + delivery.ValuePerProduct + " por lingote · minério e munição não são vendidos";
            if (execute && DepositAvailable) accepted = delivery.TryDeposit(item);
        }
        else return false;
        depositPrompt = DepositTitle;
        return accepted;
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
            GameFeedback.Notify("INVENTÁRIO CHEIO", "Libere um dos três espaços antes de coletar.", GameFeedback.Cue.Warning, true);
            return true;
        }

        slots[index] = item;
        Stow(body);
        if (selected == index) ShowSelected();
        GameFeedback.Notify("ITEM GUARDADO", ItemLabel(item.Kind) + " · espaço " + (index + 2), GameFeedback.Cue.Pickup);
        return true;
    }

    static void Stow(Rigidbody body)
    {
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
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
        if (index < -1 || index >= SlotCount) return;
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

        furnace = FindMachineInput<OreMachine>(hit.collider.transform);
        ammo = FindMachineInput<AmmoMachine>(hit.collider.transform);

        return furnace != null || ammo != null;
    }

    // Inputs are direct children of their station, alongside its visible geometry.
    // Searching the whole scene root can deposit into an unrelated machine indoors.
    public static T FindMachineInput<T>(Transform hit) where T : Component
    {
        for (Transform part = hit; part != null; part = part.parent)
        {
            T input = part.GetComponent<T>();
            if (input != null)
                return input;

            for (int i = 0; i < part.childCount; i++)
            {
                input = part.GetChild(i).GetComponent<T>();
                if (input != null)
                    return input;
            }
        }

        return null;
    }

    Rigidbody LookAtItem()
    {
        if (!Look(carry != null ? carry.PickupRange : 3f, out RaycastHit hit))
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
            if (col == null || col.transform.IsChildOf(transform) || carry != null && carry.IsCarrying(hits[i].rigidbody))
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

    public static string ItemLabel(ItemKind kind) => kind == ItemKind.Ore ? "Minério" : kind == ItemKind.Product ? "Lingote" : "Munição";
}
