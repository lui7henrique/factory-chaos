using UnityEngine;

/// <summary>
/// Small fire wisps from the furnace mouth and chimney. Stronger while ore is smelting.
/// </summary>
public class FurnaceSparks : MonoBehaviour
{
    const float IdleMouth = 4f;
    const float IdleChimney = 6f;
    const float BusyMouth = 20f;
    const float BusyChimney = 16f;

    static Material sharedMaterial;

    OreMachine machine;
    ParticleSystem mouth;
    ParticleSystem chimney;

    public static void Ensure(Transform machineRoot)
    {
        if (machineRoot == null)
            return;

        Transform existing = machineRoot.Find("FurnaceSparks");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        GameObject root = new GameObject("FurnaceSparks");
        root.transform.SetParent(machineRoot, false);
        root.AddComponent<FurnaceSparks>();
    }

    void Awake()
    {
        Transform root = transform.parent != null ? transform.parent : transform;
        machine = root.GetComponentInChildren<OreMachine>(true);
        mouth = CreateStream("Mouth", new Vector3(0.72f, 1.02f, 0f), Quaternion.Euler(0f, 90f, 0f), 0.55f, 0.08f);
        chimney = CreateStream("Chimney", new Vector3(-0.05f, 2.08f, -0.12f), Quaternion.Euler(-90f, 0f, 0f), 0.85f, 0.05f);
    }

    void Update()
    {
        bool busy = machine != null && machine.IsProcessing;
        SetRate(mouth, busy ? BusyMouth : IdleMouth);
        SetRate(chimney, busy ? BusyChimney : IdleChimney);
    }

    static void SetRate(ParticleSystem system, float rate)
    {
        if (system == null)
            return;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = rate;
    }

    ParticleSystem CreateStream(string streamName, Vector3 localPosition, Quaternion localRotation, float lifetime, float size)
    {
        GameObject go = new GameObject(streamName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;

        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
        main.startColor = new ParticleSystem.MinMaxGradient(ArtPalette.FireCore, ArtPalette.Fire);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 48;
        main.gravityModifier = -0.2f;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = IdleMouth;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 0.06f;

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(ArtPalette.FireCore, 0f),
                new GradientColorKey(ArtPalette.Fire, 0.35f),
                new GradientColorKey(new Color(0.25f, 0.22f, 0.2f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.65f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = system.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.7f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0.15f)));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = SharedMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        system.Play();
        return system;
    }

    static Material SharedMaterial()
    {
        if (sharedMaterial != null)
            return sharedMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        sharedMaterial = new Material(shader);
        Texture2D spark = SoftCircle();
        sharedMaterial.SetTexture("_BaseMap", spark);
        sharedMaterial.SetTexture("_MainTex", spark);
        sharedMaterial.SetColor("_BaseColor", Color.white);
        sharedMaterial.SetColor("_Color", Color.white);
        if (sharedMaterial.HasProperty("_Surface"))
            sharedMaterial.SetFloat("_Surface", 1f);
        if (sharedMaterial.HasProperty("_Blend"))
            sharedMaterial.SetFloat("_Blend", 2f);
        if (sharedMaterial.HasProperty("_SrcBlend"))
            sharedMaterial.SetFloat("_SrcBlend", 1f);
        if (sharedMaterial.HasProperty("_DstBlend"))
            sharedMaterial.SetFloat("_DstBlend", 1f);
        if (sharedMaterial.HasProperty("_ZWrite"))
            sharedMaterial.SetFloat("_ZWrite", 0f);
        sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sharedMaterial.renderQueue = 3000;
        return sharedMaterial;
    }

    static Texture2D SoftCircle()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }
}
