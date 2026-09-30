using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Indoor mood from the reference: dark cool ambient, warm lamps, furnace glow, and a light grade.
/// Lights stay realtime because the room is dressed when Play starts.
/// </summary>
public class IndoorMood : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!FactorySite.IsIndoor)
            return;

        GameObject runner = new GameObject("IndoorMoodDress");
        runner.hideFlags = HideFlags.HideAndDontSave;
        runner.AddComponent<IndoorMood>();
    }

    void LateUpdate()
    {
        Apply();
        Destroy(gameObject);
    }

    public static void Apply()
    {
        if (!FactorySite.IsIndoor || GameObject.Find("IndoorMood") != null)
            return;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.31f, 0.29f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.22f, 0.21f, 0.19f);
        RenderSettings.fogDensity = 0.01f;

        Light[] lights = Object.FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light != null && light.type == LightType.Directional)
                light.enabled = false;
        }

        GameObject furnace = GameObject.Find("FurnaceGlow");
        if (furnace != null)
        {
            Light glow = furnace.GetComponent<Light>();
            if (glow != null)
            {
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.48f, 0.16f);
                glow.intensity = 18f;
                glow.range = 8f;
                glow.shadows = LightShadows.None;
            }
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            if (data != null)
                data.renderPostProcessing = true;
        }

        GameObject volumeObject = new GameObject("IndoorMood");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        Bloom bloom = profile.Add<Bloom>();
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.4f);
        bloom.scatter.Override(0.62f);

        Tonemapping tonemapping = profile.Add<Tonemapping>();
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments grade = profile.Add<ColorAdjustments>();
        grade.postExposure.Override(0.08f);
        grade.contrast.Override(8f);
        grade.saturation.Override(-4f);

        Vignette vignette = profile.Add<Vignette>();
        vignette.intensity.Override(0.12f);
        vignette.smoothness.Override(0.38f);
    }
}
