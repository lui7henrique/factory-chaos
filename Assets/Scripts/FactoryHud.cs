using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paper's industrial HUD, adapted to the physical inventory prototype.
/// Presentation only: indicators read gameplay, they never advance it.
/// </summary>
public class FactoryHud : MonoBehaviour
{
    // Exact Paper palette. The bundled font keeps builds independent of OS fonts.
    static readonly Color Panel = new Color32(17, 19, 21, 225);
    static readonly Color Ink = new Color32(244, 241, 232, 255);
    static readonly Color Gold = new Color32(255, 201, 40, 255);
    static readonly Color Muted = new Color32(181, 185, 187, 255);
    static readonly Color Danger = new Color32(255, 107, 44, 255);
    static readonly Color Good = new Color32(124, 255, 107, 255);
    static readonly Color Track = new Color32(63, 67, 71, 255);
    static readonly Color Border = new Color32(244, 241, 232, 42);
    static readonly Color Selected = new Color32(255, 201, 40, 27);

    static FactoryHud instance;
    public static bool IsPresent => instance != null && instance.isActiveAndEnabled;

    DeliveryZone delivery;
    CannonController cannon;
    PlayerMining mining;
    PlayerCarry carry;
    PlayerLoadout loadout;
    OreMachine furnace;
    AmmoMachine press;
    WaveDirector wave;
    RoomSwitch roomSwitch;
    ConveyorBelt[] belts = new ConveyorBelt[0];
    readonly RaycastHit[] hits = new RaycastHit[16];
    float nextBind;
    Camera view;
    Item lookedItem;
    OreMachine lookedFurnace;
    AmmoMachine lookedPress;
    CannonController lookedCannon;
    OreVein aimedVein;
    string promptKey;
    string promptTitle;
    string promptDetail;
    bool promptWarning;
    float promptProgress = -1f;
    GUIStyle titleStyle, bodyStyle, labelStyle, smallStyle, centerStyle, numberStyle, wrappedStyle;
    float width, height, scale;
    Rect safeArea;
    int lastSelection = -2;
    float selectionAt;
    float crosshairActivity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Ensure()
    {
        if (FindAnyObjectByType<FactoryHud>() == null)
            new GameObject("FactoryHud").AddComponent<FactoryHud>();
    }

    void Awake() { instance = this; GameFeedback.Ensure(gameObject); }
    void OnEnable() { instance = this; nextBind = 0f; }
    void OnDestroy() { if (instance == this) instance = null; }

    void LateUpdate()
    {
        // Runtime props may be created after the HUD. Retry absent references at 1Hz.
        if (Time.unscaledTime >= nextBind)
        {
            nextBind = Time.unscaledTime + 1f;
            if (delivery == null) delivery = FindAnyObjectByType<DeliveryZone>();
            if (cannon == null) cannon = FindAnyObjectByType<CannonController>();
            if (mining == null) mining = FindAnyObjectByType<PlayerMining>();
            if (carry == null) carry = FindAnyObjectByType<PlayerCarry>();
            if (loadout == null) loadout = FindAnyObjectByType<PlayerLoadout>();
            if (furnace == null) furnace = FindAnyObjectByType<OreMachine>();
            if (press == null) press = FindAnyObjectByType<AmmoMachine>();
            if (wave == null) wave = FindAnyObjectByType<WaveDirector>();
            if (roomSwitch == null) roomSwitch = FindAnyObjectByType<RoomSwitch>();
            belts = FindObjectsByType<ConveyorBelt>(FindObjectsInactive.Exclude);
        }
        view = Camera.main;
        Look();
        ReadPrompt();
        if (loadout != null && loadout.SelectedIndex != lastSelection)
        {
            lastSelection = loadout.SelectedIndex;
            selectionAt = Time.time;
        }
        float active = !string.IsNullOrEmpty(promptKey) || cannon != null && cannon.IsOperating ? 1f : 0f;
        crosshairActivity = Mathf.MoveTowards(crosshairActivity, active, Time.deltaTime * 10f);
    }

    void OnGUI()
    {
        if (GamePauseMenu.IsOpen || Event.current.type != EventType.Repaint) return;
        EnsureStyles();
        safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
            safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
        scale = Mathf.Max(0.1f, Mathf.Min(safeArea.width / 1600f, safeArea.height / 900f));
        width = safeArea.width / scale;
        height = safeArea.height / scale;
        Matrix4x4 previous = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.color = Color.white;
        GUI.matrix = Matrix4x4.TRS(new Vector3(safeArea.x, Screen.height - safeArea.yMax, 0f),
            Quaternion.identity, new Vector3(scale, scale, 1f));
        try
        {
            DrawWave();
            DrawObjective();
            DrawMoney();
            DrawFactory();
            if (cannon != null && cannon.IsOperating) DrawCannonDeck();
            else DrawHotbar();
            DrawPrompt();
            DrawCrosshair();
            DrawEnemyHealth();
            DrawFeedback();
            DrawResult();
            DrawNavigation();
        }
        finally { GUI.matrix = previous; GUI.color = previousColor; }
    }

    void DrawWave()
    {
        if (wave == null) return;
        Color accent = PhaseColor();
        PanelBox(new Rect(36f, 30f, 330f, 106f), accent, true);
        Text(new Rect(53f, 35f, 290f, 38f), "ONDA 01", titleStyle, Ink);
        string phase = wave.Current == WaveDirector.Phase.Preparing ? "PREPARAÇÃO"
            : wave.Current == WaveDirector.Phase.Incoming ? "INIMIGO NO TÚNEL"
            : wave.Current == WaveDirector.Phase.Cleared ? "ONDA CONCLUÍDA" : "FÁBRICA INVADIDA";
        Text(new Rect(54f, 75f, 285f, 20f), phase, labelStyle, accent);
        if (wave.Current == WaveDirector.Phase.Preparing)
        {
            int seconds = Mathf.CeilToInt(wave.SecondsLeft);
            float fraction = wave.PreparationDuration <= 0f ? 0f : wave.SecondsLeft / wave.PreparationDuration;
            Bar(new Rect(54f, 102f, 222f, 5f), fraction, Gold);
            Text(new Rect(286f, 94f, 64f, 23f), (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"), labelStyle, Ink);
        }
        else Text(new Rect(54f, 99f, 290f, 22f), wave.Current == WaveDirector.Phase.Incoming
            ? "Defenda a entrada da fábrica." : "ESC  /  reiniciar partida", smallStyle, Muted);
    }

    Color PhaseColor()
    {
        if (wave == null || wave.Current == WaveDirector.Phase.Preparing) return Gold;
        return wave.Current == WaveDirector.Phase.Cleared ? Good : Danger;
    }

    void DrawObjective()
    {
        if (wave == null) return;
        string objective = wave.Current == WaveDirector.Phase.Cleared ? "ENTRADA DEFENDIDA"
            : wave.Current == WaveDirector.Phase.Breached ? "A DEFESA CAIU"
            : cannon == null ? "PREPARE A DEFESA"
            : cannon.IsOperating ? "DEFENDA A ENTRADA"
            : wave.Current == WaveDirector.Phase.Incoming && cannon.Rounds == 0 ? "SEM MUNIÇÃO · CARREGUE O CANHÃO"
            : cannon.Rounds == 0 ? PreparationHint()
            : !cannon.IsFull && wave.Current == WaveDirector.Phase.Preparing ? "COMPLETE A CARGA DO CANHÃO"
            : "OPERE O CANHÃO";
        Rect chip = new Rect((width - 500f) * 0.5f, 34f, 500f, 42f);
        Round(chip, Panel, 21f);
        Outline(chip, Alpha(PhaseColor(), 0.4f), 1f, 21f);
        Round(new Rect(chip.x + 18f, chip.y + 17f, 8f, 8f), PhaseColor(), 4f);
        Text(new Rect(chip.x + 34f, chip.y, chip.width - 48f, chip.height), objective, centerStyle, Ink);
        if (wave.Current == WaveDirector.Phase.Preparing)
        {
            Rect chain = new Rect(chip.x, chip.yMax + 9f, chip.width, 25f);
            Round(chain, Alpha(Panel, 0.75f), 6f);
            Text(chain, "MINÉRIO   →   LINGOTE   →   MUNIÇÃO   →   CANHÃO", centerStyle, Muted);
        }
    }

    string PreparationHint()
    {
        bool ore = false, product = false, ammo = false;
        if (loadout != null)
            for (int i = 0; i < PlayerLoadout.SlotCount; i++)
            {
                Item item = loadout.SlotItem(i);
                if (item == null) continue;
                ore |= item.Kind == ItemKind.Ore; product |= item.Kind == ItemKind.Product; ammo |= item.Kind == ItemKind.Ammo;
            }
        if (ammo) return "CARREGUE O CANHÃO COM O CARTUCHO";
        if (press != null && press.IsOutputBlocked) return "LIBERE A SAÍDA DA PRENSA";
        if (press != null && press.IsPressing) return "PRENSA ATIVA · PREPARE A PRÓXIMA CARGA";
        if (product) return "LEVE O LINGOTE À PRENSA";
        if (furnace != null && furnace.IsOutputBlocked) return "RECOLHA O LINGOTE NA SAÍDA DA FORNALHA";
        if (furnace != null && furnace.IsProcessing) return "FUNDIÇÃO EM ANDAMENTO · MINERE MAIS FERRO";
        return ore ? "DEPOSITE O MINÉRIO NA FORNALHA" : "MINERE FERRO E RECOLHA O MINÉRIO";
    }

    void DrawMoney()
    {
        if (delivery == null) return;
        Rect rect = new Rect(width - 208f, 30f, 172f, 76f);
        PanelBox(rect, Gold, true);
        Text(new Rect(rect.x + 18f, rect.y + 10f, 140f, 18f), "ENTREGAS", labelStyle, Muted);
        Text(new Rect(rect.x + 18f, rect.y + 29f, 140f, 35f), "$ " + delivery.Money.ToString("N0"), numberStyle, Ink);
    }

    void DrawFactory()
    {
        bool hasFurnace = furnace != null && furnace.isActiveAndEnabled;
        bool hasPress = press != null && press.isActiveAndEnabled;
        int activeBelts = 0, jammed = 0;
        float wait = 0f;
        for (int i = 0; i < belts.Length; i++)
        {
            if (belts[i] == null || !belts[i].isActiveAndEnabled) continue;
            activeBelts++;
            if (!belts[i].IsJammed) continue;
            jammed++;
            wait = Mathf.Max(wait, belts[i].JamSecondsLeft);
        }
        int rows = (hasFurnace ? 1 : 0) + (hasPress ? 1 : 0) + (activeBelts > 0 ? 1 : 0);
        if (rows == 0) return;
        bool alert = jammed > 0 || hasPress && press.IsOutputBlocked || hasFurnace && furnace.IsOutputBlocked;
        if (GamePreferences.CompactHud && !alert) return;
        Rect panel = new Rect(width - 354f, height - 150f - (44f + rows * 38f), 318f, 44f + rows * 38f);
        PanelBox(panel, alert ? Danger : Gold, false);
        Text(new Rect(panel.x + 16f, panel.y + 10f, 276f, 24f), "LINHA DE PRODUÇÃO", labelStyle, Muted);
        float y = panel.y + 42f;
        if (hasFurnace)
        {
            FactoryRow(panel.x, y, "Fundição", furnace.IsOutputBlocked ? "Retire o lingote" : furnace.IsProcessing ? "Fundindo · " + Mathf.CeilToInt(furnace.ProcessSecondsLeft) + "s" : "Livre", furnace.IsOutputBlocked ? Danger : furnace.IsProcessing ? Gold : Good);
            y += 38f;
        }
        if (hasPress)
        {
            string state = press.IsOutputBlocked ? "Saída bloqueada" : press.IsPressing ? "Prensando · " + Mathf.CeilToInt(press.ProcessSecondsLeft) + "s" : "Livre";
            FactoryRow(panel.x, y, "Prensa", state, press.IsOutputBlocked ? Danger : press.IsPressing ? Gold : Good);
            y += 38f;
        }
        if (activeBelts > 0) FactoryRow(panel.x, y, activeBelts == 1 ? "Esteira" : "Esteiras",
            jammed > 0 ? "Travada · " + Mathf.CeilToInt(wait) + "s" : "Rodando", jammed > 0 ? Danger : Good);
        if (jammed > 0)
            Text(new Rect(panel.x, panel.yMax + 8f, panel.width, 22f), "Retoma automaticamente", smallStyle, Ink);
    }

    void FactoryRow(float x, float y, string name, string state, Color accent)
    {
        Fill(new Rect(x + 16f, y, 282f, 1f), Border);
        Round(new Rect(x + 18f, y + 17f, 7f, 7f), accent, 3.5f);
        Text(new Rect(x + 36f, y + 5f, 100f, 31f), name, bodyStyle, Ink);
        Text(new Rect(x + 144f, y + 5f, 154f, 31f), state, smallStyle, accent, TextAnchor.MiddleRight);
    }

    void DrawHotbar()
    {
        if (loadout == null) return;
        const float slotWidth = 108f, gap = 8f;
        int count = PlayerLoadout.SlotCount + 1;
        float inventoryWidth = count * slotWidth + (count - 1) * gap;
        float deckWidth = inventoryWidth + 36f + (cannon != null ? 168f : 0f);
        Rect deck = new Rect((width - deckWidth) * 0.5f, height - 114f, deckWidth, 88f);
        Round(deck, Panel, 12f);
        Outline(deck, Border, 1f, 12f);
        Item selected = loadout.ToolSelected ? null : loadout.SlotItem(loadout.SelectedIndex);
        string selectedName = loadout.ToolSelected ? "PICARETA" : selected == null ? "MÃOS LIVRES" : ItemName(selected.Kind).ToUpperInvariant();
        Rect tab = new Rect(deck.center.x - 105f, deck.y - 28f, 210f, 28f);
        Round(tab, Gold, 4f);
        Text(tab, selectedName, centerStyle, new Color32(17, 19, 21, 255));
        for (int i = 0; i < count; i++)
        {
            bool tool = i == 0;
            Item item = tool ? null : loadout.SlotItem(i - 1);
            bool active = tool ? loadout.ToolSelected : loadout.SelectedIndex == i - 1;
            DrawSlot(new Rect(deck.x + 18f + i * (slotWidth + gap), deck.y + 10f, slotWidth, 68f), i + 1, tool, active, item);
        }
        if (cannon != null) DrawAmmo(new Rect(deck.xMax - 160f, deck.y + 6f, 142f, 74f));
    }

    void DrawSlot(Rect rect, int key, bool tool, bool selected, Item item)
    {
        if (selected) Round(rect, Alpha(Selected, Mathf.Lerp(0.11f, 0.3f, 1f - Mathf.Clamp01((Time.time - selectionAt) / 0.22f))), 4f);
        Outline(rect, selected ? Gold : Border, selected ? 2f : 1f, 4f);
        Text(new Rect(rect.x + 9f, rect.y + 5f, 22f, 22f), key.ToString(), labelStyle, selected ? Gold : Muted);
        Rect icon = tool
            ? new Rect(rect.center.x - 21f, rect.y + 2f, 42f, 38f)
            : new Rect(rect.center.x - 14f, rect.y + 5f, 28f, 30f);
        if (tool) DrawPickIcon(icon, selected);
        else if (item != null) DrawItemIcon(icon, item.Kind);
        else Outline(new Rect(icon.center.x - 8f, icon.center.y - 8f, 16f, 16f), Track, 1f, 3f);
        Text(new Rect(rect.x + 6f, rect.y + 43f, rect.width - 12f, 22f), tool ? "Picareta" : item == null ? "Vazio" : ItemName(item.Kind), centerStyle, item == null && !tool ? Muted : Ink);
    }

    void DrawAmmo(Rect rect)
    {
        Fill(new Rect(rect.x, rect.y + 3f, 1f, rect.height - 6f), Border);
        Text(new Rect(rect.x + 16f, rect.y, rect.width - 20f, 20f), "CANHÃO", labelStyle, Muted);
        Text(new Rect(rect.x + 16f, rect.y + 20f, rect.width - 20f, 34f), cannon.Rounds + " / " + cannon.Capacity, numberStyle, cannon.Rounds > 0 ? Ink : Danger);
        Segments(new Rect(rect.x + 16f, rect.y + 65f, rect.width - 20f, 6f), cannon.Rounds, cannon.Capacity, Gold);
    }

    void DrawCannonDeck()
    {
        Rect deck = new Rect(width * 0.5f - 288f, height - 140f, 576f, 114f);
        PanelBox(deck, cannon.EmptyWarning ? Danger : Gold, true);
        Text(new Rect(deck.x + 22f, deck.y + 13f, 370f, 26f), "OPERANDO O CANHÃO", bodyStyle, Gold);
        Text(new Rect(deck.x + 22f, deck.y + 42f, 375f, 28f), cannon.Rounds > 0 ? "Clique esquerdo para atirar" : "Sem munição · carregue pela entrada", smallStyle, cannon.Rounds > 0 ? Ink : Danger);
        Text(new Rect(deck.x + 22f, deck.y + 77f, 350f, 22f), "E / ESC   Sair do canhão", smallStyle, Muted);
        DrawAmmo(new Rect(deck.xMax - 158f, deck.y + 14f, 140f, 86f));
    }

    void Look()
    {
        lookedItem = null; lookedFurnace = null; lookedPress = null; lookedCannon = null;
        if (view == null || GamePauseMenu.IsOpen || Cursor.lockState != CursorLockMode.Locked
            || cannon != null && cannon.IsOperating || carry != null && !carry.InputEnabled) return;
        int count = Physics.RaycastNonAlloc(view.transform.position, view.transform.forward, hits, 3.5f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit chosen = default;
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || carry != null && (col.transform.IsChildOf(carry.transform) || carry.IsCarrying(hits[i].rigidbody))) continue;
            if (hits[i].distance >= nearest) continue;
            nearest = hits[i].distance;
            chosen = hits[i];
        }
        if (chosen.collider == null) return;
        Item item = chosen.collider.GetComponentInParent<Item>();
        if (item != null)
        {
            // Same range and kinematic requirement as PlayerLoadout's pickup.
            if (nearest <= (carry != null ? carry.PickupRange : 3f) && item.enabled && chosen.rigidbody != null && !chosen.rigidbody.isKinematic) lookedItem = item;
            return;
        }
        lookedFurnace = PlayerLoadout.FindMachineInput<OreMachine>(chosen.collider.transform);
        lookedPress = PlayerLoadout.FindMachineInput<AmmoMachine>(chosen.collider.transform);
        lookedCannon = chosen.collider.GetComponentInParent<CannonController>();
    }

    void ReadPrompt()
    {
        promptKey = null; promptTitle = null; promptDetail = null;
        promptProgress = -1f; promptWarning = false; aimedVein = null;
        if (GamePauseMenu.IsOpen || cannon != null && cannon.IsOperating) return;
        if (loadout != null && !string.IsNullOrEmpty(loadout.DepositPrompt))
        {
            bool action = loadout.DepositAvailable;
            promptKey = action ? "M2" : null;
            promptTitle = loadout.DepositTitle ?? loadout.DepositPrompt;
            promptDetail = loadout.DepositDetail;
            promptWarning = !action;
            return;
        }
        aimedVein = mining != null ? mining.AimedVein : null;
        if (aimedVein != null)
        {
            promptWarning = aimedVein.IsDepleted || aimedVein.DropWaiting;
            promptKey = promptWarning ? null : "M1";
            promptTitle = aimedVein.IsDepleted ? "VEIO ESGOTADO" : aimedVein.DropWaiting ? "SAÍDA DO VEIO BLOQUEADA" : "MINERAR FERRO";
            promptDetail = aimedVein.IsDepleted ? "A reserva deste veio acabou." : aimedVein.DropWaiting
                ? "Retire os objetos ao redor do veio." : "Clique ou segure o botão esquerdo · reserva: " + aimedVein.Reserve;
            return;
        }
        if (lookedItem != null)
        {
            bool full = loadout != null;
            if (loadout != null)
                for (int i = 0; i < PlayerLoadout.SlotCount; i++)
                    if (loadout.SlotItem(i) == null) full = false;
            promptKey = full ? null : "E";
            promptTitle = full ? "INVENTÁRIO CHEIO" : "PEGAR " + ItemName(lookedItem.Kind).ToUpperInvariant();
            promptDetail = full ? "Libere um dos três espaços." : "Guarda em um espaço livre.";
            promptWarning = full;
            return;
        }
        if (carry != null && carry.IsHolding && carry.InputEnabled)
        {
            Item held = loadout != null && !loadout.ToolSelected ? loadout.SlotItem(loadout.SelectedIndex) : null;
            bool loading = lookedCannon != null && held != null && held.Kind == ItemKind.Ammo;
            promptKey = "E";
            promptTitle = loading ? "SOLTAR MUNIÇÃO" : "SOLTAR ITEM";
            promptDetail = loading ? lookedCannon.IsFull ? "Canhão cheio." : "Solte sobre a entrada de carga do canhão."
                : "Clique esquerdo para arremessar";
            return;
        }
        if (cannon != null && cannon.ShowsOperatePrompt)
        {
            promptKey = "E"; promptTitle = "OPERAR CANHÃO";
            promptDetail = cannon.Rounds + " de " + cannon.Capacity + " tiros carregados";
            return;
        }
        if (lookedFurnace != null)
        {
            promptWarning = lookedFurnace.IsOutputBlocked;
            promptTitle = promptWarning ? "SAÍDA DA FORNALHA BLOQUEADA" : lookedFurnace.IsProcessing ? "FUNDINDO FERRO" : "FORNALHA LIVRE";
            promptDetail = promptWarning ? "Recolha o lingote que está na saída." : lookedFurnace.IsProcessing ? Mathf.CeilToInt(lookedFurnace.ProcessSecondsLeft) + "s para o lingote ficar pronto"
                : "Segure minério e deposite com o botão direito.";
            if (lookedFurnace.IsProcessing) promptProgress = Progress(lookedFurnace.ProcessSecondsLeft, lookedFurnace.ProcessDuration);
            return;
        }
        if (lookedPress != null)
        {
            promptWarning = lookedPress.IsOutputBlocked;
            promptTitle = promptWarning ? "SAÍDA DA PRENSA BLOQUEADA" : lookedPress.IsPressing ? "PRODUZINDO MUNIÇÃO" : "PRENSA LIVRE";
            promptDetail = promptWarning ? "Retire o objeto da saída." : lookedPress.IsPressing
                ? "Na fila: " + lookedPress.PendingCount + " · " + Mathf.CeilToInt(lookedPress.ProcessSecondsLeft) + "s no ciclo"
                : "Segure um lingote e deposite com o botão direito.";
            if (lookedPress.IsPressing) promptProgress = Progress(lookedPress.ProcessSecondsLeft, lookedPress.ProcessDuration);
        }
    }

    void DrawPrompt()
    {
        if (string.IsNullOrEmpty(promptTitle)) return;
        bool segments = aimedVein != null && !aimedVein.IsDepleted;
        bool meter = segments || promptProgress >= 0f;
        float desired = Mathf.Max(bodyStyle.CalcSize(new GUIContent(promptTitle)).x + 92f,
            smallStyle.CalcSize(new GUIContent(promptDetail ?? "")).x + 36f);
        float cardWidth = Mathf.Clamp(desired, 300f, 620f);
        Rect card = new Rect((width - cardWidth) * 0.5f, height * 0.5f + 36f, cardWidth, meter ? 92f : 74f);
        Round(card, Panel, 8f); Outline(card, Border, 1f, 8f);
        Color accent = promptWarning ? Danger : Gold;
        float x = card.x + 16f;
        if (!string.IsNullOrEmpty(promptKey))
        {
            Rect key = new Rect(x, card.y + 12f, 34f, 26f);
            Round(key, Ink, 4f); Text(key, promptKey, centerStyle, Color.black);
            x += 46f;
        }
        Text(new Rect(x, card.y + 10f, card.xMax - x - 16f, 29f), promptTitle, bodyStyle, accent);
        Text(new Rect(card.x + 16f, card.y + 43f, cardWidth - 32f, 23f), promptDetail ?? "", smallStyle, Ink);
        Rect bar = new Rect(card.x + 16f, card.yMax - 16f, cardWidth - 32f, 5f);
        if (segments) Segments(bar, aimedVein.DropWaiting ? aimedVein.StrikesPerUnit : aimedVein.Strikes, aimedVein.StrikesPerUnit, accent);
        else if (promptProgress >= 0f) Bar(bar, promptProgress, accent);
    }

    void DrawCrosshair()
    {
        float x = width * 0.5f, y = height * 0.5f;
        bool active = crosshairActivity > 0.01f;
        Color accent = active ? Gold : Ink;
        Round(new Rect(x - 3f, y - 3f, 6f, 6f), new Color(0f, 0f, 0f, 0.75f), 3f);
        Round(new Rect(x - 1.5f, y - 1.5f, 3f, 3f), accent, 1.5f);
        if (!active && (cannon == null || !cannon.IsOperating)) return;
        float reach = 7f + crosshairActivity * 6f + (cannon != null && cannon.IsOperating ? cannon.ShotRecovery * 9f : 0f);
        accent.a = crosshairActivity;
        Fill(new Rect(x - reach, y - 1f, 6f, 2f), accent);
        Fill(new Rect(x + reach - 6f, y - 1f, 6f, 2f), accent);
        Fill(new Rect(x - 1f, y - reach, 2f, 6f), accent);
        Fill(new Rect(x - 1f, y + reach - 6f, 2f, 6f), accent);
    }

    void DrawFeedback()
    {
        if (GameFeedback.HitLife > 0f)
        {
            float x = width * 0.5f, y = height * 0.5f;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = RotateIcon(previous, new Vector2(x, y), 45f);
            Color tint = GameFeedback.LethalHit ? Danger : Ink;
            Fill(new Rect(x - 16f, y - 1.5f, 8f, 3f), tint);
            Fill(new Rect(x + 8f, y - 1.5f, 8f, 3f), tint);
            Fill(new Rect(x - 1.5f, y - 16f, 3f, 8f), tint);
            Fill(new Rect(x - 1.5f, y + 8f, 3f, 8f), tint);
            GUI.matrix = previous;
        }
        if (GameFeedback.MessageLife <= 0f) return;
        float reveal = Mathf.Clamp01(GameFeedback.MessageAge / 0.15f);
        float alpha = Mathf.Min(reveal, GameFeedback.MessageLife / 0.3f);
        if (alpha <= 0.01f) return;
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        Rect toast = new Rect(36f - (1f - reveal) * 10f, height - 216f, 344f, 112f);
        Round(toast, Alpha(Panel, Panel.a * alpha), 10f);
        Fill(new Rect(toast.x, toast.y, 3f, toast.height), Alpha(GameFeedback.Tint, alpha));
        Text(new Rect(toast.x + 16f, toast.y + 11f, 316f, 28f), GameFeedback.Title, bodyStyle, GameFeedback.Tint);
        Text(new Rect(toast.x + 16f, toast.y + 44f, 310f, 55f), GameFeedback.Detail, wrappedStyle, Ink);
        GUI.color = previousColor;
    }

    void DrawResult()
    {
        if (wave == null || (wave.Current != WaveDirector.Phase.Cleared && wave.Current != WaveDirector.Phase.Breached)) return;
        bool won = wave.Current == WaveDirector.Phase.Cleared;
        Rect card = new Rect(width * 0.5f - 245f, 160f, 490f, 96f);
        PanelBox(card, won ? Good : Danger, true);
        Text(new Rect(card.x + 20f, card.y + 10f, 450f, 35f), won ? "TURNO CONCLUÍDO" : "TENTE MAIS UMA VEZ", titleStyle, won ? Good : Danger);
        Text(new Rect(card.x + 20f, card.y + 53f, 450f, 29f), won ? "Entrada defendida. Explore ou reinicie pelo ESC." : "Prepare três cartuchos. Reinicie pelo ESC.", smallStyle, Ink);
    }

    void DrawEnemyHealth()
    {
        if (view == null || wave == null || wave.Current != WaveDirector.Phase.Incoming || wave.Enemy == null || wave.Enemy.IsDead) return;
        Vector3 world = wave.Enemy.transform.position + Vector3.up * 2.25f;
        Vector3 screen = view.WorldToScreenPoint(world);
        if (screen.z <= 0f) return;
        float x = (screen.x - safeArea.x) / scale;
        float y = (safeArea.yMax - screen.y) / scale;
        if (x < 82f || x > width - 82f || y < 144f || y > height - 178f) return;
        if (Physics.Linecast(view.transform.position, world, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<WaveEnemy>() != wave.Enemy) return;
        Rect panel = new Rect(x - 76f, y, 152f, 30f);
        Round(panel, Panel, 4f);
        Bar(new Rect(panel.x + 8f, panel.y + 10f, 136f, 8f), wave.Enemy.MaxHealth <= 0f ? 0f : wave.Enemy.Health / wave.Enemy.MaxHealth, Danger);
    }

    void DrawNavigation()
    {
        if (cannon == null || !cannon.IsOperating)
            Text(new Rect(36f, height - 54f, 220f, 24f), "ESC   Pausa", smallStyle, Ink);
        if (roomSwitch != null)
        {
            string other = SceneManager.GetActiveScene().name == "IndoorFactory" ? "Pátio" : "Oficina";
            Text(new Rect(width - 296f, height - 54f, 260f, 24f), "T   Comparar · " + other, smallStyle, Ink, TextAnchor.MiddleRight);
        }
    }

    static string ItemName(ItemKind kind) { return kind == ItemKind.Ore ? "Minério" : kind == ItemKind.Product ? "Lingote" : "Munição"; }
    static float Progress(float left, float duration) { return duration <= 0f ? 1f : 1f - Mathf.Clamp01(left / duration); }
    static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }

    static void DrawPickIcon(Rect rect, bool selected)
    {
        Color graphite = new Color32(52, 59, 67, 255);
        Color iron = new Color32(165, 173, 181, 255);
        Color wood = new Color32(125, 75, 38, 255);
        Color woodLight = new Color32(184, 119, 61, 255);
        Color leather = new Color32(63, 38, 28, 255);
        Color accent = selected ? Gold : Ink;

        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = RotateIcon(previous, rect.center, 34f);

        float x = rect.center.x;
        float top = rect.y + 7f;

        // Handle and wrapped grip sit behind the forged head.
        Segment(new Vector2(x, top + 3f), new Vector2(x, rect.yMax - 2f), 8f, graphite, 2f);
        Segment(new Vector2(x, top + 4f), new Vector2(x, rect.yMax - 3f), 5.5f, wood, 1.5f);
        Segment(new Vector2(x - 1.2f, top + 6f), new Vector2(x - 1.2f, rect.yMax - 8f), 1.2f, woodLight, 0.6f);
        Segment(new Vector2(x, rect.yMax - 12f), new Vector2(x, rect.yMax - 2f), 8.5f, leather, 2f);
        Fill(new Rect(x - 4.5f, rect.yMax - 11f, 9f, 1.5f), woodLight);
        Fill(new Rect(x - 4.5f, rect.yMax - 7f, 9f, 1.5f), woodLight);
        Fill(new Rect(x - 4.5f, rect.yMax - 3.5f, 9f, 1.5f), woodLight);

        // Asymmetric head: a curved point on the left and a broad chisel on the right.
        Segment(new Vector2(x - 8f, top + 4f), new Vector2(x + 12f, top + 3f), 9f, graphite, 2.5f);
        Segment(new Vector2(x - 8f, top + 4f), new Vector2(x - 17f, top + 7f), 7f, graphite, 2f);
        Segment(new Vector2(x - 17f, top + 7f), new Vector2(x - 23f, top + 12f), 4.5f, graphite, 1.5f);
        Segment(new Vector2(x - 22f, top + 11f), new Vector2(x - 25f, top + 15f), 2f, accent, 1f);
        Segment(new Vector2(x + 10f, top + 3f), new Vector2(x + 21f, top + 6f), 9f, graphite, 2f);
        Segment(new Vector2(x + 20f, top + 5f), new Vector2(x + 24f, top + 7f), 3f, iron, 1f);
        Segment(new Vector2(x - 16f, top + 4.5f), new Vector2(x + 11f, top + 1.5f), 1.5f, iron, 0.75f);

        Round(new Rect(x - 5f, top - 1f, 10f, 13f), graphite, 2f);
        Fill(new Rect(x - 5f, top + 1f, 10f, 2f), selected ? Gold : iron);
        Fill(new Rect(x - 5f, top + 8f, 10f, 2f), iron);
        GUI.matrix = previous;
    }

    static void DrawItemIcon(Rect rect, ItemKind kind)
    {
        if (kind == ItemKind.Ore)
        {
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = RotateIcon(previous, rect.center, 24f);
            Round(new Rect(rect.x + 6f, rect.y + 6f, 25f, 24f), new Color32(216, 118, 112, 255), 7f);
            Fill(new Rect(rect.x + 12f, rect.y + 8f, 9f, 8f), new Color32(248, 164, 150, 255));
            GUI.matrix = previous;
        }
        else if (kind == ItemKind.Product)
        {
            Round(new Rect(rect.x, rect.y + 13f, 36f, 17f), new Color32(165, 173, 181, 255), 3f);
            Fill(new Rect(rect.x + 4f, rect.y + 10f, 28f, 6f), Ink);
        }
        else
        {
            Round(new Rect(rect.x + 12f, rect.y + 2f, 12f, 18f), Danger, 6f);
            Fill(new Rect(rect.x + 12f, rect.y + 14f, 12f, 17f), Gold);
            Fill(new Rect(rect.x + 9f, rect.y + 29f, 18f, 4f), Ink);
        }
    }

    static void PanelBox(Rect rect, Color accent, bool left)
    {
        Round(rect, Panel, 10f);
        Fill(new Rect(left ? rect.x : rect.xMax - 3f, rect.y, 3f, rect.height), accent);
    }
    static Matrix4x4 RotateIcon(Matrix4x4 matrix, Vector2 center, float angle)
    {
        Vector3 pivot = new Vector3(center.x, center.y, 0f);
        return matrix * Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angle)) * Matrix4x4.Translate(-pivot);
    }
    static void Segment(Vector2 start, Vector2 end, float thickness, Color color, float radius)
    {
        Vector2 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.01f) return;
        Vector2 center = (start + end) * 0.5f;
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = RotateIcon(previous, center, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        Round(new Rect(center.x - length * 0.5f, center.y - thickness * 0.5f, length, thickness), color, radius);
        GUI.matrix = previous;
    }
    static void Fill(Rect rect, Color color) { Round(rect, color, 0f); }
    static void Round(Rect rect, Color color, float radius)
    {
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);
    }
    static void Outline(Rect rect, Color color, float thickness, float radius)
    {
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, thickness, radius);
    }
    static void Bar(Rect rect, float fraction, Color color)
    {
        Round(rect, Track, 2f);
        if (fraction > 0f) Round(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color, 2f);
    }
    static void Segments(Rect rect, int filled, int total, Color color)
    {
        total = Mathf.Max(1, total);
        float gap = 5f;
        float segment = (rect.width - gap * (total - 1)) / total;
        for (int i = 0; i < total; i++) Bar(new Rect(rect.x + i * (segment + gap), rect.y, segment, rect.height), i < filled ? 1f : 0f, color);
    }
    static void Text(Rect rect, string value, GUIStyle style, Color color, TextAnchor? anchor = null)
    {
        Color previous = style.normal.textColor;
        TextAnchor previousAnchor = style.alignment;
        style.normal.textColor = color;
        if (anchor.HasValue) style.alignment = anchor.Value;
        GUI.Label(rect, value, style);
        style.normal.textColor = previous;
        style.alignment = previousAnchor;
    }
    void EnsureStyles()
    {
        if (titleStyle != null) return;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleStyle = MakeStyle(font, 32, FontStyle.Bold);
        bodyStyle = MakeStyle(font, 16, FontStyle.Bold);
        labelStyle = MakeStyle(font, 14, FontStyle.Bold);
        smallStyle = MakeStyle(font, 14, FontStyle.Normal);
        wrappedStyle = MakeStyle(font, 14, FontStyle.Normal, TextAnchor.UpperLeft);
        wrappedStyle.wordWrap = true;
        centerStyle = MakeStyle(font, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        numberStyle = MakeStyle(font, 28, FontStyle.Bold);
    }
    static GUIStyle MakeStyle(Font font, int size, FontStyle weight, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        return new GUIStyle(GUI.skin.label) { font = font, fontSize = size, fontStyle = weight,
            alignment = anchor, padding = new RectOffset(), margin = new RectOffset(), wordWrap = false, richText = false };
    }
}
