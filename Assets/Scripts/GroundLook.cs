using UnityEngine;

/// <summary>
/// Tunables for the low-poly yard. World meters, relative to the Ground position.
/// Path angle 0 runs along +Z.
/// </summary>
public class GroundLook : MonoBehaviour
{
    public int seed = 17;
    public Vector2 clearingCenter = new Vector2(0.5f, 2.5f);
    public Vector2 clearingSize = new Vector2(15f, 13f);
    public float pathWidth = 5f;
    public float pathAngle = 4f;
}
