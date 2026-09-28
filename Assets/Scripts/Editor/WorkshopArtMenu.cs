using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class WorkshopArtMenu
{
    [MenuItem("GameObject/Factory Chaos/Apply Workshop Art Sample")]
    static void ApplySample()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Apply the workshop art sample in edit mode.");
            return;
        }

        if (FactorySite.IsIndoor)
        {
            Debug.LogWarning("The workshop sample stays on the outdoor yard. IndoorFactory keeps its own room and lights.");
            return;
        }

        GameObject machine = GameObject.Find("Machine");
        if (machine == null)
        {
            Debug.LogWarning("The scene needs the existing Machine object.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Workshop Art Sample");

        ArtMaterials.Set materials = ArtAssetsMenu.LoadOrCreate();
        Light sun = FindSun();
        if (sun != null)
        {
            Undo.RecordObject(sun, "Apply Workshop Art Sample");
            sun.color = ArtPalette.Sun;
            sun.intensity = 1.15f;
            sun.useColorTemperature = true;
            sun.colorTemperature = 4250f;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ArtPalette.AmbientSky;
        RenderSettings.ambientEquatorColor = ArtPalette.AmbientEquator;
        RenderSettings.ambientGroundColor = ArtPalette.AmbientGround;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = ArtPalette.Fog;
        RenderSettings.fogDensity = 0.012f;

        Volume volume = Object.FindAnyObjectByType<Volume>();
        if (volume != null)
            Undo.RecordObject(volume, "Apply Workshop Art Sample");
        WorkshopLook.DisableGameplayBlur(volume);

        try
        {
            WorkshopBay.Created = go => Undo.RegisterCreatedObjectUndo(go, "Apply Workshop Art Sample");
            WorkshopBay.Destroyed = go => Undo.DestroyObjectImmediate(go);
            WorkshopBay.Build(machine.transform.position, materials);
        }
        finally
        {
            WorkshopBay.Created = null;
            WorkshopBay.Destroyed = null;
        }

        FurnaceVisualMenu.RebuildExisting(machine.transform);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = machine;
    }

    static Light FindSun()
    {
        Light[] lights = Object.FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
                return lights[i];
        }

        return null;
    }
}
