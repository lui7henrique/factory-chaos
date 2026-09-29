using UnityEngine;

/// <summary>
/// Marks a root created by Build Indoor Factory so a later run replaces only that group.
/// Kept in its own file so scenes serialize a persistent MonoScript asset reference.
/// </summary>
public class IndoorGroup : MonoBehaviour
{
    public string groupId;
}
