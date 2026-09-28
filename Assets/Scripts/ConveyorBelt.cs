using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pushes Ore and Product rigidbodies along local forward. Attach this to the trigger zone.
/// The belt runs, then jams for a few seconds and warns the player. It starts again on its own.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ConveyorBelt : MonoBehaviour
{
    [Header("Drive")]
    [SerializeField] float speed = 1f;

    [Header("Jam")]
    [SerializeField] float minRunTime = 18f;
    [SerializeField] float maxRunTime = 32f;
    [SerializeField] float jamDuration = 4f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly Color JammedColor = new Color(0.9f, 0.16f, 0.12f);

    readonly Dictionary<Rigidbody, int> overlaps = new Dictionary<Rigidbody, int>();
    readonly List<Rigidbody> expired = new List<Rigidbody>();
    readonly List<Renderer> statusRendererList = new List<Renderer>();

    PlayerCarry playerCarry;
    Renderer[] statusRenderers;
    Material[] statusMaterials;
    Color[] runningColors;
    GUIStyle labelStyle;
    float nextSwitchTime;
    bool jammed;

    public bool IsJammed => jammed;

    void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        playerCarry = FindAnyObjectByType<PlayerCarry>();
        CacheStatus();
        nextSwitchTime = Time.time + NextRunTime();
    }

    void OnDestroy()
    {
        if (statusMaterials == null)
            return;

        for (int i = 0; i < statusMaterials.Length; i++)
        {
            if (statusMaterials[i] != null)
                Destroy(statusMaterials[i]);
        }
    }

    void Update()
    {
        TickJam();
        if (jammed)
            Paint(Color.Lerp(JammedColor, Color.white, Mathf.PingPong(Time.time * 3f, 0.35f)), true);
    }

    void FixedUpdate()
    {
        TickJam();

        Vector3 drive = transform.forward;
        if (drive.sqrMagnitude < 0.0001f)
            return;

        drive.Normalize();
        expired.Clear();

        foreach (KeyValuePair<Rigidbody, int> pair in overlaps)
        {
            Rigidbody body = pair.Key;
            if (body == null)
            {
                expired.Add(body);
                continue;
            }

            if (body.isKinematic || (playerCarry != null && playerCarry.IsCarrying(body)))
                continue;

            Item item = body.GetComponent<Item>();
            if (item == null)
                item = body.GetComponentInParent<Item>();

            if (item == null || (item.Kind != ItemKind.Ore && item.Kind != ItemKind.Product))
                continue;

            Vector3 velocity = body.linearVelocity;
            float along = Vector3.Dot(velocity, drive);
            if (jammed)
                body.linearVelocity = velocity - drive * along;
            else
                body.linearVelocity = velocity - drive * along + drive * speed;
        }

        for (int i = 0; i < expired.Count; i++)
            overlaps.Remove(expired[i]);
    }

    void OnTriggerEnter(Collider other)
    {
        ChangeOverlap(other, 1);
    }

    void OnTriggerExit(Collider other)
    {
        ChangeOverlap(other, -1);
    }

    void TickJam()
    {
        if (Time.time < nextSwitchTime)
            return;

        jammed = !jammed;
        if (jammed)
        {
            nextSwitchTime = Time.time + jamDuration;
            return;
        }

        nextSwitchTime = Time.time + NextRunTime();
        PaintRunning();
    }

    float NextRunTime()
    {
        float min = Mathf.Min(minRunTime, maxRunTime);
        float max = Mathf.Max(minRunTime, maxRunTime);
        return Random.Range(min, max);
    }

    void CacheStatus()
    {
        Transform root = transform.parent;
        if (root == null)
            return;

        Transform belt = root.Find("Base");
        if (belt != null)
        {
            Renderer renderer = belt.GetComponent<Renderer>();
            if (renderer != null)
                statusRendererList.Add(renderer);
        }

        Transform direction = root.Find("Direction");
        if (direction != null)
        {
            Renderer[] arrows = direction.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < arrows.Length; i++)
                statusRendererList.Add(arrows[i]);
        }

        statusRenderers = statusRendererList.ToArray();
        statusMaterials = new Material[statusRenderers.Length];
        runningColors = new Color[statusRenderers.Length];
        for (int i = 0; i < statusRenderers.Length; i++)
        {
            statusMaterials[i] = statusRenderers[i].material;
            runningColors[i] = statusMaterials[i].GetColor(BaseColorId);
        }
    }

    void PaintRunning()
    {
        if (statusMaterials == null)
            return;

        for (int i = 0; i < statusMaterials.Length; i++)
            PaintOne(statusMaterials[i], runningColors[i], false);
    }

    void Paint(Color color, bool emit)
    {
        if (statusMaterials == null)
            return;

        for (int i = 0; i < statusMaterials.Length; i++)
            PaintOne(statusMaterials[i], color, emit);
    }

    static void PaintOne(Material material, Color color, bool emit)
    {
        if (material == null)
            return;

        material.SetColor(BaseColorId, color);
        material.SetColor(ColorId, color);
        if (emit)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionColorId, color * 0.55f);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor(EmissionColorId, Color.black);
        }
    }

    void ChangeOverlap(Collider other, int delta)
    {
        if (other == null)
            return;

        Rigidbody body = other.attachedRigidbody;
        if (body == null)
            return;

        overlaps.TryGetValue(body, out int count);
        count += delta;
        if (count <= 0)
            overlaps.Remove(body);
        else
            overlaps[body] = count;
    }

    void OnGUI()
    {
        if (!jammed)
            return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 22;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = new Color(1f, 0.45f, 0.32f);
        }

        const float width = 420f;
        GUI.Label(new Rect((Screen.width - width) * 0.5f, 52f, width, 36f), "Esteira emperrada", labelStyle);
    }
}
