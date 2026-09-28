using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Applies workshop lighting and the furnace-bay sample at Play.
/// Does not move machines, colliders of gameplay, or the ground plane.
/// </summary>
public static class WorkshopLook
{
    public static void Apply()
    {
        if (FactorySite.IsIndoor)
            return;

        ApplyLighting();
        ApplyBayAndFurnace();
    }

    public static void ApplyLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ArtPalette.AmbientSky;
        RenderSettings.ambientEquatorColor = ArtPalette.AmbientEquator;
        RenderSettings.ambientGroundColor = ArtPalette.AmbientGround;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = ArtPalette.Fog;
        RenderSettings.fogDensity = 0.012f;

        Light sun = Object.FindAnyObjectByType<Light>();
        Light[] lights = Object.FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
            {
                sun = lights[i];
                break;
            }
        }

        if (sun == null)
            return;

        sun.color = ArtPalette.Sun;
        sun.intensity = 1.15f;
        sun.useColorTemperature = true;
        sun.colorTemperature = 4250f;
        sun.shadows = LightShadows.Soft;
    }

    public static void DisableGameplayBlur(Volume volume)
    {
        if (volume == null || volume.sharedProfile == null)
            return;

        VolumeProfile profile = volume.sharedProfile;
        if (profile.TryGet(out MotionBlur blur))
            blur.active = false;
        if (profile.TryGet(out DepthOfField depth))
            depth.active = false;
    }

    static void ApplyBayAndFurnace()
    {
        GameObject machine = GameObject.Find("Machine");
        if (machine == null)
            return;

        ArtMaterials.Set materials = ArtMaterials.Runtime();
        if (WorkshopBay.Find() == null)
            WorkshopBay.Build(machine.transform.position, materials);

        Volume volume = Object.FindAnyObjectByType<Volume>();
        DisableGameplayBlur(volume);
    }
}
