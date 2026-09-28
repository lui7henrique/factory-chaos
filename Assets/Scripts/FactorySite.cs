using UnityEngine;

/// <summary>
/// Marks a scene as the outdoor yard or the indoor mine workshop.
/// Outdoor bootstraps read this instead of guessing from object names.
/// </summary>
public class FactorySite : MonoBehaviour
{
    public enum Kind
    {
        Outdoor = 0,
        Indoor = 1
    }

    public Kind kind = Kind.Outdoor;

    public static bool IsIndoor
    {
        get
        {
            FactorySite site = FindAnyObjectByType<FactorySite>();
            return site != null && site.kind == Kind.Indoor;
        }
    }
}
