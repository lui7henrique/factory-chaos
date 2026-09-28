using UnityEngine;

/// <summary>
/// One screen layout for money, the target, and the current action.
/// </summary>
public class FactoryHud : MonoBehaviour
{
    static readonly Color Panel = new Color(0.09f, 0.1f, 0.11f, 0.88f);
    static readonly Color Ink = new Color(0.96f, 0.94f, 0.9f, 1f);
    static readonly Color Gold = new Color(0.957f, 0.745f, 0.196f, 1f);
    static readonly Color Muted = new Color(0.66f, 0.69f, 0.72f, 1f);
    static readonly Color Danger = new Color(0.82f, 0.29f, 0.22f, 1f);
    static readonly Color Track = new Color(0.16f, 0.17f, 0.18f, 1f);

    DeliveryZone delivery;
    CannonController cannon;
    PlayerMining mining;
    PlayerCarry carry;
    PlayerLoadout loadout;
    OreMachine furnace;
    WaveDirector wave;
    Item lookedItem;
    bool lookingAtFurnace;
    GUIStyle titleStyle;
    GUIStyle bodyStyle;
    GUIStyle captionStyle;
    bool promptActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (FindAnyObjectByType<FactoryHud>() != null)
            return;

        GameObject root = new GameObject("FactoryHud");
        root.AddComponent<FactoryHud>();
    }

    void LateUpdate()
    {
        if (delivery == null)
            delivery = FindAnyObjectByType<DeliveryZone>();
        if (cannon == null)
            cannon = FindAnyObjectByType<CannonController>();
        if (mining == null)
            mining = FindAnyObjectByType<PlayerMining>();
        if (carry == null)
            carry = FindAnyObjectByType<PlayerCarry>();
        if (loadout == null)
            loadout = FindAnyObjectByType<PlayerLoadout>();
        if (furnace == null)
            furnace = FindAnyObjectByType<OreMachine>();
        if (wave == null)
            wave = FindAnyObjectByType<WaveDirector>();

        Look();
    }

    void OnGUI()
    {
        EnsureStyles();
        DrawWave();
        DrawMoney();
        promptActive = DrawPrompt();
        DrawFurnaceBar();
        DrawCrosshair();
        DrawHotbar();
        DrawEnemyHealth();
    }

    void DrawWave()
    {
        if (wave == null)
            return;

        string detail;
        bool danger = false;
        if (wave.Current == WaveDirector.Phase.Preparing)
            detail = "Preparação  " + Mathf.CeilToInt(wave.SecondsLeft) + " s";
        else if (wave.Current == WaveDirector.Phase.Incoming)
        {
            detail = "Saiu do túnel";
            danger = true;
        }
        else if (wave.Current == WaveDirector.Phase.Cleared)
            detail = "Onda limpa";
        else
        {
            detail = "Chegou na fábrica";
            danger = true;
        }

        bool bar = wave.Enemy != null && !wave.Enemy.IsDead && wave.Current == WaveDirector.Phase.Incoming;
        Rect panel = new Rect(16f, 16f, 236f, bar ? 92f : 64f);
        DrawPanel(panel);
        Label(new Rect(panel.x + 16f, panel.y + 6f, 204f, 28f), "ONDA 1", titleStyle, Ink);
        Label(new Rect(panel.x + 16f, panel.y + 34f, 204f, 22f), detail, bodyStyle, danger ? Danger : Muted);
        if (!bar)
            return;

        DrawHealthChunks(new Rect(panel.x + 16f, panel.y + 64f, 204f, 14f), wave.Enemy.Health, wave.Enemy.MaxHealth);
    }

    void DrawEnemyHealth()
    {
        if (wave == null || wave.Enemy == null || wave.Enemy.IsDead)
            return;

        Camera view = Camera.main;
        if (view == null)
            return;

        Vector3 screen = view.WorldToScreenPoint(wave.Enemy.transform.position + Vector3.up * 2.25f);
        if (screen.z <= 0f)
            return;

        DrawHealthChunks(new Rect(screen.x - 72f, Screen.height - screen.y, 144f, 16f), wave.Enemy.Health, wave.Enemy.MaxHealth);
    }

    void DrawHealthChunks(Rect rect, float health, float maxHealth)
    {
        const int chunks = 3;
        float fraction = maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
        int filled = Mathf.CeilToInt(fraction * chunks);
        float gap = 4f;
        float width = (rect.width - gap * (chunks - 1)) / chunks;
        for (int i = 0; i < chunks; i++)
            DrawBar(new Rect(rect.x + i * (width + gap), rect.y, width, rect.height), i < filled ? 1f : 0f, Danger);
    }

    void DrawMoney()
    {
        if (delivery == null)
            return;

        Rect panel = new Rect(Screen.width - 156f, 16f, 140f, 40f);
        DrawPanel(panel);
        Label(new Rect(panel.x + 12f, panel.y + 4f, 116f, 32f), "$" + delivery.Money, bodyStyle, Gold);
    }

    bool DrawPrompt()
    {
        if (loadout != null && !string.IsNullOrEmpty(loadout.DepositPrompt))
        {
            DrawCard(loadout.DepositPrompt, false, false);
            return true;
        }

        if (cannon != null && cannon.IsOperating)
        {
            string ammo = "Munição " + cannon.Rounds + "/" + cannon.Capacity + "  ·  E — Sair";
            if (cannon.EmptyWarning)
                ammo = "Sem munição  ·  E — Sair";
            DrawBottom(ammo, cannon.EmptyWarning);
            return true;
        }

        OreVein vein = mining != null ? mining.AimedVein : null;
        if (vein != null)
        {
            if (vein.IsDepleted)
            {
                DrawCard("Veio esgotado", true, false);
                return true;
            }

            int shown = vein.DropWaiting ? vein.StrikesPerUnit : vein.Strikes;
            string detail = vein.DropWaiting
                ? "Sem espaço para o minério"
                : "Segure clique esquerdo — Minerar    Reserva " + vein.Reserve;
            Rect card = DrawCard(detail, vein.DropWaiting, true);
            DrawSegments(card, shown, vein.StrikesPerUnit);
            return true;
        }

        if (carry != null && carry.IsHolding && carry.InputEnabled)
        {
            DrawCard("E — Soltar  ·  Clique esquerdo — Arremessar", false, false);
            return true;
        }

        if (lookedItem != null)
        {
            DrawCard(PickupText(lookedItem), false, false);
            return true;
        }

        if (cannon != null && cannon.ShowsOperatePrompt)
        {
            DrawCard("E — Operar", false, false);
            return true;
        }

        if (lookingAtFurnace && furnace != null && furnace.IsProcessing)
        {
            DrawCard("Fundindo… " + Mathf.CeilToInt(furnace.ProcessSecondsLeft) + " s", false, false);
            return true;
        }

        return false;
    }

    void DrawHotbar()
    {
        if (loadout == null)
            return;

        const float tool = 46f;
        const float slot = 62f;
        const float gap = 8f;
        float width = tool + gap + slot * PlayerLoadout.SlotCount + gap * (PlayerLoadout.SlotCount - 1);
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - 86f;

        DrawSlot(new Rect(x, y + (slot - tool) * 0.5f, tool, tool), "1", true, loadout.ToolSelected, null);
        for (int i = 0; i < PlayerLoadout.SlotCount; i++)
        {
            float slotX = x + tool + gap + i * (slot + gap);
            Item item = loadout.SlotItem(i);
            DrawSlot(new Rect(slotX, y, slot, slot), (i + 2).ToString(), false, loadout.SelectedIndex == i, item);
        }
    }

    void DrawSlot(Rect rect, string key, bool toolSlot, bool selected, Item item)
    {
        DrawPanel(rect);
        if (selected)
        {
            Color previous = GUI.color;
            GUI.color = Gold;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 3f, rect.y, 3f, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        if (toolSlot)
            DrawPickIcon(rect);
        else if (item != null)
            DrawItemSwatch(rect, item.Kind);

        Label(new Rect(rect.x + 4f, rect.y + 1f, 18f, 16f), key, captionStyle, Muted);
    }

    void DrawPickIcon(Rect rect)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0.59f, 0.38f, 0.22f);
        GUI.DrawTexture(new Rect(rect.center.x - 3f, rect.y + 14f, 6f, rect.height - 24f), Texture2D.whiteTexture);
        GUI.color = new Color(0.72f, 0.75f, 0.8f);
        GUI.DrawTexture(new Rect(rect.center.x - 12f, rect.y + 12f, 22f, 7f), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void DrawItemSwatch(Rect rect, ItemKind kind)
    {
        Color previous = GUI.color;
        if (kind == ItemKind.Ore)
            GUI.color = new Color(0.55f, 0.34f, 0.28f);
        else if (kind == ItemKind.Product)
            GUI.color = new Color(0.74f, 0.76f, 0.8f);
        else
            GUI.color = new Color(0.95f, 0.62f, 0.12f);

        float pad = 16f;
        GUI.DrawTexture(new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void DrawFurnaceBar()
    {
        if (!lookingAtFurnace || furnace == null || !furnace.IsProcessing)
            return;

        Camera view = Camera.main;
        if (view == null)
            return;

        Transform host = furnace.transform.parent != null ? furnace.transform.parent : furnace.transform;
        Vector3 world = host.position + Vector3.up * 2.05f;
        Vector3 screen = view.WorldToScreenPoint(world);
        if (screen.z <= 0f)
            return;

        float fraction = furnace.ProcessDuration <= 0f
            ? 1f
            : 1f - Mathf.Clamp01(furnace.ProcessSecondsLeft / furnace.ProcessDuration);
        Rect bar = new Rect(screen.x - 48f, Screen.height - screen.y, 96f, 8f);
        DrawBar(bar, fraction, Gold);
    }

    void Look()
    {
        lookedItem = null;
        lookingAtFurnace = false;
        if (cannon != null && cannon.IsOperating)
            return;
        if (carry != null && (!carry.InputEnabled || carry.IsHolding))
            return;

        Camera view = Camera.main;
        if (view == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        if (!Physics.Raycast(view.transform.position, view.transform.forward, out RaycastHit hit, 3.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider == null)
            return;

        Item item = hit.collider.GetComponentInParent<Item>();
        if (item != null && item.enabled)
        {
            lookedItem = item;
            return;
        }

        if (furnace != null && hit.collider.transform.IsChildOf(furnace.transform.root))
            lookingAtFurnace = true;
    }

    static string PickupText(Item item)
    {
        if (item.Kind == ItemKind.Ore)
            return "E — Pegar ferro";
        if (item.Kind == ItemKind.Product)
            return "E — Pegar lingote";
        if (item.Kind == ItemKind.Ammo)
            return "E — Pegar munição";
        return "E — Pegar";
    }

    Rect DrawCard(string detail, bool warning, bool meter)
    {
        float width = 520f;
        float height = meter ? 58f : 36f;
        float y = Screen.height * 0.5f + 22f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, y, width, height);
        DrawPanel(panel);
        Label(new Rect(panel.x + 14f, panel.y + 6f, width - 28f, 24f), detail, captionStyle, warning ? Danger : Ink);
        return panel;
    }

    void DrawBottom(string detail, bool warning)
    {
        float width = 420f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height - 64f, width, 40f);
        DrawPanel(panel);
        Label(new Rect(panel.x + 14f, panel.y + 8f, width - 28f, 24f), detail, captionStyle, warning ? Danger : Ink);
    }

    void DrawSegments(Rect card, int filled, int total)
    {
        total = Mathf.Max(1, total);
        filled = Mathf.Clamp(filled, 0, total);
        float gap = 6f;
        float inner = card.width - 36f;
        float barWidth = Mathf.Min(42f, (inner - gap * (total - 1)) / total);
        float x = card.x + 18f;
        float y = card.yMax - 18f;
        for (int i = 0; i < total; i++)
            DrawBar(new Rect(x + i * (barWidth + gap), y, barWidth, 8f), i < filled ? 1f : 0f, Gold);
    }

    void DrawCrosshair()
    {
        const float arm = 5f;
        const float gap = 3f;
        const float thickness = 2f;
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        Color color = promptActive ? Gold : Ink;

        DrawMark(new Rect(centerX - gap - arm, centerY - thickness * 0.5f, arm, thickness), color);
        DrawMark(new Rect(centerX + gap, centerY - thickness * 0.5f, arm, thickness), color);
        DrawMark(new Rect(centerX - thickness * 0.5f, centerY - gap - arm, thickness, arm), color);
        DrawMark(new Rect(centerX - thickness * 0.5f, centerY + gap, thickness, arm), color);
    }

    void DrawPanel(Rect rect)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), Texture2D.whiteTexture);
        GUI.color = Panel;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Gold;
        GUI.DrawTexture(new Rect(rect.x, rect.y, 4f, rect.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void DrawBar(Rect rect, float fraction, Color fill)
    {
        Color previous = GUI.color;
        GUI.color = Track;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        if (fraction > 0f)
        {
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), Texture2D.whiteTexture);
        }

        GUI.color = previous;
    }

    void DrawMark(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void Label(Rect rect, string value, GUIStyle style, Color color)
    {
        Color previous = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.55f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), value, style);
        style.normal.textColor = color;
        GUI.Label(rect, value, style);
        style.normal.textColor = previous;
    }

    void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleStyle = MakeStyle(font, 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        bodyStyle = MakeStyle(font, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        captionStyle = MakeStyle(font, 15, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    static GUIStyle MakeStyle(Font font, int size, FontStyle style, TextAnchor anchor)
    {
        GUIStyle result = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = size,
            fontStyle = style,
            alignment = anchor
        };
        result.normal.textColor = Ink;
        return result;
    }
}
