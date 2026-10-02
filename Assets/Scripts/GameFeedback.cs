using UnityEngine;

/// <summary>
/// Bounded, scene-local feedback. Clips are synthesized once, reused and disposed.
/// Messages report completed actions; they never mutate gameplay.
/// </summary>
public sealed class GameFeedback : MonoBehaviour
{
    public enum Cue { Pickup, Deposit, Ready, Mine, Shot, Hit, Warning, Victory }
    static GameFeedback instance;
    AudioSource source;
    readonly AudioClip[] clips = new AudioClip[8];
    readonly float[] lastSound = new float[8];
    string title, detail;
    Color tint;
    float messageUntil, messageStart, hitUntil;
    bool lethal;

    public static string Title => instance != null ? instance.title : null;
    public static string Detail => instance != null ? instance.detail : null;
    public static Color Tint => instance != null ? instance.tint : Color.white;
    public static float MessageLife => instance != null ? Mathf.Max(0f, instance.messageUntil - Time.time) : 0f;
    public static float MessageAge => instance != null ? Time.time - instance.messageStart : 0f;
    public static float HitLife => instance != null ? Mathf.Max(0f, instance.hitUntil - Time.time) : 0f;
    public static bool LethalHit => instance != null && instance.lethal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    public static void Ensure(GameObject host)
    {
        if (instance == null) host.AddComponent<GameFeedback>();
    }

    void Awake()
    {
        instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i] = MakeClip((Cue)i);
            lastSound[i] = -10f;
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < clips.Length; i++) if (clips[i] != null) Destroy(clips[i]);
        if (instance == this) instance = null;
    }

    public static void Play(Cue cue)
    {
        if (instance == null || GamePauseMenu.IsOpen) return;
        int i = (int)cue;
        if (Time.time - instance.lastSound[i] < 0.07f) return;
        instance.lastSound[i] = Time.time;
        instance.source.volume = GamePreferences.Volume;
        instance.source.PlayOneShot(instance.clips[i], cue == Cue.Shot ? 0.7f : 0.42f);
    }

    public static void Notify(string title, string detail, Cue cue = Cue.Ready, bool warning = false)
    {
        if (instance == null) return;
        instance.title = title;
        instance.detail = detail;
        instance.tint = warning ? new Color32(255, 107, 44, 255) : new Color32(255, 201, 40, 255);
        instance.messageStart = Time.time;
        instance.messageUntil = Time.time + (warning ? 3.5f : 2.6f);
        Play(cue);
    }

    public static void ConfirmHit(bool killed)
    {
        if (instance == null) return;
        instance.hitUntil = Time.time + (killed ? 0.4f : 0.2f);
        instance.lethal = killed;
        Play(Cue.Hit);
    }

    static AudioClip MakeClip(Cue cue)
    {
        const int rate = 22050;
        float length = cue == Cue.Victory ? 0.65f : cue == Cue.Shot ? 0.3f : 0.16f;
        float frequency = cue == Cue.Pickup ? 680f : cue == Cue.Deposit ? 350f : cue == Cue.Ready ? 880f
            : cue == Cue.Mine ? 170f : cue == Cue.Shot ? 85f : cue == Cue.Hit ? 540f : cue == Cue.Warning ? 220f : 660f;
        var random = new System.Random(47 + (int)cue);
        var samples = new float[Mathf.CeilToInt(rate * length)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate, u = t / length;
            float envelope = Mathf.Min(1f, t / 0.004f) * Mathf.Pow(1f - u, 2f);
            float pitch = cue == Cue.Victory ? (t < 0.2f ? 1f : t < 0.4f ? 1.25f : 1.5f) : 1f;
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * pitch * t);
            float noise = (float)random.NextDouble() * 2f - 1f;
            float grit = cue == Cue.Shot ? 0.7f : cue == Cue.Mine ? 0.55f : cue == Cue.Hit ? 0.25f : 0.02f;
            samples[i] = (tone * (1f - grit) + noise * grit) * envelope * 0.65f;
        }
        AudioClip clip = AudioClip.Create("Factory " + cue, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
