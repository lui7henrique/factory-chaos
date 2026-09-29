using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Small runtime pause menu. Escape opens it, pauses the simulation, and exposes
/// only the actions that belong to the current prototype milestone.
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class GamePauseMenu : MonoBehaviour
{
    static readonly Color Overlay = new Color(0.025f, 0.03f, 0.035f, 0.82f);
    static readonly Color Panel = new Color(0.09f, 0.1f, 0.11f, 0.98f);
    static readonly Color Ink = new Color(0.96f, 0.94f, 0.9f, 1f);
    static readonly Color Muted = new Color(0.56f, 0.59f, 0.62f, 1f);
    static readonly Color Gold = new Color32(255, 201, 40, 255);
    static readonly Color Button = new Color(0.19f, 0.22f, 0.24f, 1f);
    static readonly Color ButtonHover = new Color(0.24f, 0.28f, 0.3f, 1f);

    static GamePauseMenu instance;
    static int ignoredEscapeFrame = -1;
    static int handledEscapeFrame = -1;

    GUIStyle titleStyle;
    GUIStyle subtitleStyle;
    GUIStyle buttonStyle;
    GUIStyle buttonDetailStyle;
    GUIStyle footerStyle;
    bool open;
    bool restarting;
    float previousTimeScale = 1f;
    bool previousAudioPause;
    enum Page { Main, Settings, Confirm }
    Page page;
    string destinationScene;
    bool settingsDirty;
    int selectedButton;
    WaveDirector wave;

    public static bool IsOpen => instance != null && instance.open;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        ignoredEscapeFrame = -1;
        handledEscapeFrame = -1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Ensure()
    {
        if (instance != null)
            return;

        GamePauseMenu existing = FindAnyObjectByType<GamePauseMenu>();
        if (existing != null)
        {
            instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        new GameObject("GamePauseMenu").AddComponent<GamePauseMenu>();
    }

    /// <summary>
    /// Prevents one Escape press from both leaving an interaction and opening the menu.
    /// </summary>
    public static void IgnoreEscapeThisFrame()
    {
        ignoredEscapeFrame = Time.frameCount;
    }

    /// <summary>
    /// Lets another gameplay component restore and open the menu if its runtime
    /// object was lost during a scene or script reload.
    /// </summary>
    public static void HandleEscapePressed()
    {
        if (ignoredEscapeFrame == Time.frameCount || handledEscapeFrame == Time.frameCount)
            return;

        Ensure();
        if (instance == null || instance.restarting)
            return;

        handledEscapeFrame = Time.frameCount;
        if (instance.open)
            instance.Back();
        else
            instance.PauseGame();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += SceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    void OnDestroy()
    {
        if (instance != this)
            return;

        if (open)
            ResumeGame();

        instance = null;
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        restarting = false;
    }

    void Update()
    {
        if (restarting || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            HandleEscapePressed();
        if (!open || page == Page.Settings) return;
        int count = page == Page.Confirm ? 2 : 3;
        if (Keyboard.current.downArrowKey.wasPressedThisFrame) selectedButton = (selectedButton + 1) % count;
        if (Keyboard.current.upArrowKey.wasPressedThisFrame) selectedButton = (selectedButton + count - 1) % count;
        if (Keyboard.current.enterKey.wasPressedThisFrame) Activate(selectedButton);
    }

    void OnApplicationFocus(bool focused)
    {
#if !UNITY_EDITOR
        if (!focused && !restarting && !open) PauseGame();
#endif
    }

    public static void RequestRoomChange(string scene)
    {
        if (!Application.CanStreamedLevelBeLoaded(scene)) return;
        Ensure();
        instance.PauseGame();
        instance.destinationScene = scene;
        instance.page = Page.Confirm;
        instance.selectedButton = 0;
    }

    void Back()
    {
        if (page == Page.Main) ResumeGame();
        else { SaveSettings(); page = Page.Main; selectedButton = 0; destinationScene = null; }
    }

    void SaveSettings()
    {
        if (!settingsDirty) return;
        GamePreferences.Save();
        settingsDirty = false;
    }

    void Activate(int index)
    {
        if (page == Page.Confirm)
        {
            if (index == 0) { Back(); return; }
            if (destinationScene == null) RestartGame();
            else
            {
                string scene = destinationScene;
                restarting = true;
                ResumeGame();
                SceneManager.LoadScene(scene);
            }
        }
        else if (page == Page.Main)
        {
            if (index == 0) ResumeGame();
            else if (index == 1) { page = Page.Settings; selectedButton = 0; }
            else { page = Page.Confirm; destinationScene = null; selectedButton = 0; }
        }
    }

    void PauseGame()
    {
        if (open)
            return;

        open = true;
        page = Page.Main;
        selectedButton = 0;
        wave = FindAnyObjectByType<WaveDirector>();
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
    {
        if (!open)
            return;

        open = false;
        SaveSettings();
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void RestartGame()
    {
        if (restarting)
            return;

        restarting = true;
        ResumeGame();

        if (SceneManager.sceneCountInBuildSettings > 0)
            SceneManager.LoadScene(0);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnGUI()
    {
        if (!open)
            return;

        EnsureStyles();

        int previousDepth = GUI.depth;
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.depth = -1000;
        GUI.color = Color.white;
        DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), Overlay);
        Rect safe = Screen.safeArea;
        float scale = Mathf.Max(0.1f, Mathf.Min(safe.width / 1600f, safe.height / 900f));
        GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0f), Quaternion.identity, Vector3.one * scale);
        const float width = 570f;
        const float height = 650f;
        Rect panel = new Rect(
            Mathf.Round((safe.width / scale - width) * 0.5f),
            Mathf.Round((safe.height / scale - height) * 0.5f),
            width,
            height);

        DrawRect(new Rect(panel.x + 8f, panel.y + 10f, panel.width, panel.height), new Color(0f, 0f, 0f, 0.42f));
        DrawRect(panel, Panel);
        DrawRect(new Rect(panel.x, panel.y, 6f, panel.height), Gold);

        string heading = page == Page.Settings ? "DO SEU JEITO" : page == Page.Confirm ? "DESCARTAR A PARTIDA?"
            : wave != null && wave.Current == WaveDirector.Phase.Cleared ? "FÁBRICA DEFENDIDA"
            : wave != null && wave.Current == WaveDirector.Phase.Breached ? "A DEFESA CAIU" : "PAUSA NO TURNO";
        GUI.Label(new Rect(panel.x + 38f, panel.y + 30f, panel.width - 76f, 42f), heading, titleStyle);
        GUI.Label(new Rect(panel.x + 38f, panel.y + 75f, panel.width - 76f, 28f), "FACTORY CHAOS  /  " + (page == Page.Settings ? "PREFERÊNCIAS" : "SIMULAÇÃO PAUSADA"), subtitleStyle);
        if (page == Page.Settings) DrawSettings(panel);
        else if (page == Page.Confirm)
        {
            GUI.Label(new Rect(panel.x + 38f, panel.y + 130f, 494f, 90f),
                destinationScene == null ? "Reiniciar zera inventário, dinheiro e a onda atual.\nEsta ação não pode ser desfeita."
                    : "As salas são demonstrações independentes.\nTrocar de sala zera inventário, dinheiro e a onda atual.", buttonDetailStyle);
            DrawMenuButton(panel, 0, 258f, "CANCELAR", "Voltar ao menu sem perder nada.");
            DrawMenuButton(panel, 1, 346f, destinationScene == null ? "SIM, REINICIAR" : "SIM, TROCAR DE SALA", "Descartar o estado atual.");
        }
        else
        {
            DrawMenuButton(panel, 0, 130f, "CONTINUAR", "Retomar exatamente de onde parou.");
            DrawMenuButton(panel, 1, 218f, "CONFIGURAÇÕES", "Áudio, mouse e densidade da HUD.");
            DrawMenuButton(panel, 2, 306f, "REINICIAR PARTIDA", "Uma nova tentativa, desde o início.");
            GUI.Label(new Rect(panel.x + 38f, panel.y + 416f, 494f, 30f), "CONTROLES", buttonStyle);
            GUI.Label(new Rect(panel.x + 38f, panel.y + 452f, 494f, 125f),
                "WASD  Andar       SHIFT  Correr       ESPAÇO  Pular\nE  Pegar / soltar       1–4 ou roda  Selecionar\nM1  Minerar / arremessar / atirar\nM2  Depositar / carregar / entregar\nT  Comparar salas (descarta a partida)", buttonDetailStyle);
        }
        GUI.Label(new Rect(panel.x + 38f, panel.yMax - 45f, panel.width - 76f, 24f), page == Page.Main ? "ESC  CONTINUAR     ↑ ↓  NAVEGAR     ENTER  SELECIONAR" : "ESC  VOLTAR", footerStyle);
        GUI.depth = previousDepth;
        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
    }

    void DrawMenuButton(Rect panel, int index, float y, string title, string detail)
    {
        Rect rect = new Rect(panel.x + 38f, panel.y + y, panel.width - 76f, 72f);
        if (index == selectedButton) DrawRect(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), Gold);
        if (DrawButton(rect, title, detail)) Activate(index);
    }

    void DrawSettings(Rect panel)
    {
        float x = panel.x + 38f, w = panel.width - 76f;
        float volume = SettingValue(new Rect(x, panel.y + 132f, w, 68f), "VOLUME DOS EFEITOS", GamePreferences.Volume, 0f, 1f, 0.05f, Mathf.RoundToInt(GamePreferences.Volume * 100f) + "%");
        float sensitivity = SettingValue(new Rect(x, panel.y + 224f, w, 68f), "SENSIBILIDADE DO MOUSE", GamePreferences.Sensitivity, 0.25f, 2.5f, 0.1f, GamePreferences.Sensitivity.ToString("0.00") + "×");
        bool invert = GamePreferences.InvertY, compact = GamePreferences.CompactHud;
        if (DrawButton(new Rect(x, panel.y + 318f, w, 66f), "INVERTER EIXO VERTICAL: " + (invert ? "SIM" : "NÃO"), "Aplica ao jogador e à mira do canhão.")) invert = !invert;
        if (DrawButton(new Rect(x, panel.y + 402f, w, 66f), "HUD COMPACTA: " + (compact ? "SIM" : "NÃO"), "Oculta o painel de produção quando não há alertas.")) compact = !compact;
        if (!Mathf.Approximately(volume, GamePreferences.Volume) || !Mathf.Approximately(sensitivity, GamePreferences.Sensitivity)
            || invert != GamePreferences.InvertY || compact != GamePreferences.CompactHud)
        {
            GamePreferences.Set(volume, sensitivity, invert, compact);
            settingsDirty = true;
        }
        if (DrawButton(new Rect(x, panel.y + 500f, w, 66f), "VOLTAR", "Preferências salvas neste computador.")) Back();
    }

    float SettingValue(Rect rect, string name, float value, float min, float max, float step, string label)
    {
        GUI.Label(new Rect(rect.x, rect.y, rect.width - 100f, 26f), name, buttonStyle);
        GUI.Label(new Rect(rect.xMax - 100f, rect.y, 100f, 26f), label, footerStyle);
        Rect track = new Rect(rect.x + 60f, rect.y + 42f, rect.width - 120f, 6f);
        DrawRect(track, Button);
        DrawRect(new Rect(track.x, track.y, track.width * Mathf.InverseLerp(min, max, value), track.height), Gold);
        if (GUI.Button(new Rect(rect.x, rect.y + 30f, 42f, 34f), "−", buttonStyle)) value -= step;
        if (GUI.Button(new Rect(rect.xMax - 42f, rect.y + 30f, 42f, 34f), "+", buttonStyle)) value += step;
        return Mathf.Clamp(value, min, max);
    }

    bool DrawButton(Rect rect, string title, string detail)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        DrawRect(rect, hover ? ButtonHover : Button);
        DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), Gold);
        GUI.Label(new Rect(rect.x + 20f, rect.y + 10f, rect.width - 40f, 26f), title, buttonStyle);
        GUI.Label(new Rect(rect.x + 20f, rect.y + 38f, rect.width - 40f, 20f), detail, buttonDetailStyle);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    static void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleStyle = MakeStyle(font, 30, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
        subtitleStyle = MakeStyle(font, 15, FontStyle.Normal, TextAnchor.MiddleLeft, Muted);
        buttonStyle = MakeStyle(font, 18, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
        buttonDetailStyle = MakeStyle(font, 15, FontStyle.Normal, TextAnchor.MiddleLeft, Muted);
        footerStyle = MakeStyle(font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, Muted);
    }

    static GUIStyle MakeStyle(Font font, int size, FontStyle style, TextAnchor anchor, Color color)
    {
        GUIStyle result = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = size,
            fontStyle = style,
            alignment = anchor
        };
        result.normal.textColor = color;
        return result;
    }
}
