using UnityEngine;

/// <summary>Small, local accessibility settings. No gameplay or save progression.</summary>
public static class GamePreferences
{
    const string Prefix = "FactoryChaos.";
    public static float Volume { get; private set; } = 0.65f;
    public static float Sensitivity { get; private set; } = 1f;
    public static bool InvertY { get; private set; }
    public static bool CompactHud { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Load()
    {
        Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", 0.65f));
        Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Sensitivity", 1f), 0.25f, 2.5f);
        InvertY = PlayerPrefs.GetInt(Prefix + "InvertY", 0) != 0;
        CompactHud = PlayerPrefs.GetInt(Prefix + "CompactHud", 0) != 0;
    }

    public static void Set(float volume, float sensitivity, bool invertY, bool compactHud)
    {
        Volume = Mathf.Clamp01(volume);
        Sensitivity = Mathf.Clamp(sensitivity, 0.25f, 2.5f);
        InvertY = invertY;
        CompactHud = compactHud;
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(Prefix + "Volume", Volume);
        PlayerPrefs.SetFloat(Prefix + "Sensitivity", Sensitivity);
        PlayerPrefs.SetInt(Prefix + "InvertY", InvertY ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "CompactHud", CompactHud ? 1 : 0);
        PlayerPrefs.Save();
    }
}
