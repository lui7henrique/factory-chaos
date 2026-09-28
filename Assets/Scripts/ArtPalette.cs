using UnityEngine;

/// <summary>
/// Shared FactoryChaos colors. Values live in docs/ArtDirection.md.
/// </summary>
public static class ArtPalette
{
    public static readonly Color Shell = Hex(0x35696B);
    public static readonly Color Graphite = Hex(0x343B43);
    public static readonly Color Wall = Hex(0x858078);
    public static readonly Color Iron = Hex(0xA5ADB5);
    public static readonly Color Marking = Hex(0xD5A63A);
    public static readonly Color Crystal = Hex(0xD87670);
    public static readonly Color Rock = new Color(0.38f, 0.34f, 0.31f);
    public static readonly Color RockDark = new Color(0.22f, 0.2f, 0.18f);
    public static readonly Color Concrete = new Color(0.42f, 0.4f, 0.37f);
    public static readonly Color Bore = new Color(0.09f, 0.1f, 0.11f);
    public static readonly Color Fire = new Color(1f, 0.71f, 0.18f);
    public static readonly Color FireCore = new Color(0.953f, 0.416f, 0.086f);
    public static readonly Color LampReady = new Color(0.349f, 0.937f, 0.380f);
    public static readonly Color LampBusy = new Color(1f, 0.749f, 0.212f);
    public static readonly Color Sun = new Color(1f, 0.92f, 0.78f);
    public static readonly Color Fill = new Color(1f, 0.78f, 0.55f);
    public static readonly Color AmbientSky = new Color(0.42f, 0.44f, 0.46f);
    public static readonly Color AmbientEquator = new Color(0.32f, 0.3f, 0.26f);
    public static readonly Color AmbientGround = new Color(0.18f, 0.16f, 0.12f);
    public static readonly Color Fog = new Color(0.55f, 0.52f, 0.45f);

    public static Color Hex(int rgb)
    {
        float r = ((rgb >> 16) & 255) / 255f;
        float g = ((rgb >> 8) & 255) / 255f;
        float b = (rgb & 255) / 255f;
        return new Color(r, g, b, 1f);
    }
}
