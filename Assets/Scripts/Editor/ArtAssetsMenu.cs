using System.IO;
using UnityEditor;
using UnityEngine;

public static class ArtAssetsMenu
{
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";

    public static ArtMaterials.Set LoadOrCreate()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder(ArtMaterials.Folder);

        return new ArtMaterials.Set
        {
            shell = Save(ArtMaterials.ShellPath, "PaintedShell", ArtPalette.Shell, 0.16f, 0.05f, false),
            graphite = Save(ArtMaterials.GraphitePath, "Graphite", ArtPalette.Graphite, 0.12f, 0.18f, false),
            wall = Save(ArtMaterials.WallPath, "Wall", ArtPalette.Wall, 0.08f, 0f, false),
            iron = Save(ArtMaterials.IronPath, "Iron", ArtPalette.Iron, 0.22f, 0.32f, false),
            marking = Save(ArtMaterials.MarkingPath, "Marking", ArtPalette.Marking, 0.14f, 0.04f, false),
            concrete = Save(ArtMaterials.ConcretePath, "Concrete", ArtPalette.Concrete, 0.06f, 0f, false),
            rock = Save(ArtMaterials.RockPath, "Rock", ArtPalette.Rock, 0.05f, 0f, false),
            crystal = Save(ArtMaterials.CrystalPath, "OreCrystal", ArtPalette.Crystal, 0.28f, 0.08f, false),
            bore = Save(ArtMaterials.BorePath, "Bore", ArtPalette.Bore, 0.04f, 0.05f, false),
            fire = Save(ArtMaterials.FirePath, "Fire", ArtPalette.Fire, 0.12f, 0f, true),
            fireCore = Save(ArtMaterials.FireCorePath, "FireCore", ArtPalette.FireCore, 0.1f, 0f, true),
            lampReady = Save(ArtMaterials.LampReadyPath, "LampReady", ArtPalette.LampReady, 0.15f, 0f, true),
            lampBusy = Save(ArtMaterials.LampBusyPath, "LampBusy", ArtPalette.LampBusy, 0.15f, 0f, true)
        };
    }

    static Material Save(string path, string materialName, Color color, float smoothness, float metallic, bool emissive)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            ArtMaterials.Paint(existing, color, smoothness, metallic, emissive);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        if (File.Exists(path))
        {
            Debug.LogWarning("Could not load " + path + ".");
            return null;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (source == null)
        {
            Debug.LogWarning("Missing " + SourceMaterialPath + ".");
            return ArtMaterials.Make(materialName, color, smoothness, metallic, emissive);
        }

        Material material = new Material(source) { name = materialName };
        ArtMaterials.Paint(material, color, smoothness, metallic, emissive);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
