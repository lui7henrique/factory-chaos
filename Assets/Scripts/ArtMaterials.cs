using UnityEngine;

/// <summary>
/// Shared URP/Lit materials for painted metal, iron, stone and concrete.
/// Runtime clones are reused. Editor menus save the same recipes under Assets/Art/Materials.
/// </summary>
public static class ArtMaterials
{
    public const string Folder = "Assets/Art/Materials";
    public const string ShellPath = Folder + "/PaintedShell.mat";
    public const string GraphitePath = Folder + "/Graphite.mat";
    public const string WallPath = Folder + "/Wall.mat";
    public const string IronPath = Folder + "/Iron.mat";
    public const string MarkingPath = Folder + "/Marking.mat";
    public const string ConcretePath = Folder + "/Concrete.mat";
    public const string RockPath = Folder + "/Rock.mat";
    public const string CrystalPath = Folder + "/OreCrystal.mat";
    public const string BorePath = Folder + "/Bore.mat";
    public const string FirePath = Folder + "/Fire.mat";
    public const string FireCorePath = Folder + "/FireCore.mat";
    public const string LampReadyPath = Folder + "/LampReady.mat";
    public const string LampBusyPath = Folder + "/LampBusy.mat";

    static Set runtime;

    public struct Set
    {
        public Material shell;
        public Material graphite;
        public Material wall;
        public Material iron;
        public Material marking;
        public Material concrete;
        public Material rock;
        public Material crystal;
        public Material bore;
        public Material fire;
        public Material fireCore;
        public Material lampReady;
        public Material lampBusy;
    }

    public static Set Runtime()
    {
        if (runtime.shell != null)
            return runtime;

        runtime = new Set
        {
            shell = Make("PaintedShell", ArtPalette.Shell, 0.16f, 0.05f, false),
            graphite = Make("Graphite", ArtPalette.Graphite, 0.12f, 0.18f, false),
            wall = Make("Wall", ArtPalette.Wall, 0.08f, 0f, false),
            iron = Make("Iron", ArtPalette.Iron, 0.22f, 0.32f, false),
            marking = Make("Marking", ArtPalette.Marking, 0.14f, 0.04f, false),
            concrete = Make("Concrete", ArtPalette.Concrete, 0.06f, 0f, false),
            rock = Make("Rock", ArtPalette.Rock, 0.05f, 0f, false),
            crystal = Make("OreCrystal", ArtPalette.Crystal, 0.28f, 0.08f, false),
            bore = Make("Bore", ArtPalette.Bore, 0.04f, 0.05f, false),
            fire = Make("Fire", ArtPalette.Fire, 0.12f, 0f, true, 0.55f),
            fireCore = Make("FireCore", ArtPalette.FireCore, 0.1f, 0f, true, 0.7f),
            lampReady = Make("LampReady", ArtPalette.LampReady, 0.15f, 0f, true, 0.65f),
            lampBusy = Make("LampBusy", ArtPalette.LampBusy, 0.15f, 0f, true, 0.65f)
        };
        return runtime;
    }

    public static FurnaceVisual.Palette Furnace(Set set)
    {
        return new FurnaceVisual.Palette
        {
            body = set.shell,
            structure = set.graphite,
            tray = set.iron,
            accent = set.marking,
            interior = set.fireCore,
            flame = set.fire,
            lamp = set.lampReady,
            bore = set.bore
        };
    }

    public static Material Make(string materialName, Color color, float smoothness, float metallic, bool emissive, float emission = 0.65f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.name = materialName;
        Paint(material, color, smoothness, metallic, emissive, emission);
        return material;
    }

    public static void Paint(Material material, Color color, float smoothness, float metallic, bool emissive, float emission = 0.65f)
    {
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", null);

        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }
    }
}
