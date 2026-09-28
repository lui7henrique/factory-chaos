using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Swaps the outdoor yard and the underground workshop while playing, so the two rooms can be compared.
/// </summary>
public class RoomSwitch : MonoBehaviour
{
    const string Yard = "SampleScene";
    const string Workshop = "IndoorFactory";

    GUIStyle labelStyle;
    bool loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene != Yard && scene != Workshop)
            return;

        if (FindAnyObjectByType<RoomSwitch>() != null)
            return;

        new GameObject("RoomSwitch").AddComponent<RoomSwitch>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (loading || Keyboard.current == null || !Keyboard.current.tKey.wasPressedThisFrame)
            return;

        string scene = SceneManager.GetActiveScene().name;
        if (scene != Yard && scene != Workshop)
            return;

        loading = true;
        SceneManager.LoadScene(scene == Workshop ? Yard : Workshop);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += Loaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= Loaded;
    }

    void Loaded(Scene scene, LoadSceneMode mode)
    {
        loading = false;
        PlaySceneVisuals.Ensure();
        CombatTestSpawner.Ensure();
        MiningBootstrap.Ensure();
        FactoryHud.Ensure();
    }

    void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 14;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = new Color(0.96f, 0.94f, 0.9f);
        }

        string scene = SceneManager.GetActiveScene().name;
        string other = scene == Workshop ? "pátio" : "oficina";
        const float width = 220f;
        GUI.Label(new Rect((Screen.width - width) * 0.5f, 8f, width, 22f), "T — " + other, labelStyle);
    }
}
