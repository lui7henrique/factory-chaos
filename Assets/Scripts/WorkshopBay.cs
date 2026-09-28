using System;
using UnityEngine;

/// <summary>
/// Short workshop bay behind the furnace. Visual sample only: no gameplay scripts.
/// Walls keep colliders. Trays, belt and mouth stay clear.
/// </summary>
public static class WorkshopBay
{
    public const string RootName = "WorkshopBay";
    public static Action<GameObject> Created;
    public static Action<GameObject> Destroyed;

    public static Transform Find()
    {
        GameObject existing = GameObject.Find(RootName);
        return existing != null ? existing.transform : null;
    }

    public static Transform Build(Vector3 origin, ArtMaterials.Set materials)
    {
        Transform existing = Find();
        if (existing != null)
            DestroyObject(existing.gameObject);

        GameObject root = new GameObject(RootName);
        Track(root);
        root.transform.position = origin;
        root.transform.rotation = Quaternion.identity;

        Cube(root.transform, "Pad", new Vector3(-0.15f, 0.02f, 0.05f), new Vector3(2.5f, 0.04f, 2.7f), materials.concrete, false);
        Cube(root.transform, "WallBack", new Vector3(-1.12f, 1.15f, 0.05f), new Vector3(0.18f, 2.3f, 3.1f), materials.wall, true);
        Cube(root.transform, "WallSouth", new Vector3(0.05f, 1.15f, -1.52f), new Vector3(2.15f, 2.3f, 0.16f), materials.wall, true);
        Cube(root.transform, "BaseboardBack", new Vector3(-1.02f, 0.18f, 0.05f), new Vector3(0.08f, 0.36f, 3.1f), materials.graphite, true);
        Cube(root.transform, "BaseboardSouth", new Vector3(0.05f, 0.18f, -1.42f), new Vector3(2.15f, 0.36f, 0.08f), materials.graphite, true);
        Cube(root.transform, "Lintel", new Vector3(-1.12f, 2.22f, 0.05f), new Vector3(0.22f, 0.14f, 3.2f), materials.graphite, false);
        Cube(root.transform, "Mark", new Vector3(-1.02f, 1.7f, -0.9f), new Vector3(0.04f, 0.28f, 0.28f), materials.marking, false);

        GameObject lamp = Cube(root.transform, "LampHousing", new Vector3(0.35f, 2.42f, -0.35f), new Vector3(0.28f, 0.1f, 0.28f), materials.graphite, false);
        Cube(lamp.transform, "Shade", new Vector3(0f, -0.08f, 0f), new Vector3(0.22f, 0.08f, 0.22f), materials.marking, false);

        GameObject fill = new GameObject("FillLight");
        Track(fill);
        fill.transform.SetParent(lamp.transform, false);
        fill.transform.localPosition = new Vector3(0f, -0.16f, 0.1f);
        Light light = fill.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = ArtPalette.Fill;
        light.intensity = 3.2f;
        light.range = 8.5f;
        light.shadows = LightShadows.Soft;
        return root.transform;
    }

    static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool solid)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        Track(part);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = scale;

        if (!solid)
        {
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
        }

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        return part;
    }

    static void DestroyObject(GameObject go)
    {
        if (Destroyed != null)
            Destroyed(go);
        else
            UnityEngine.Object.DestroyImmediate(go);
    }

    static void Track(GameObject go)
    {
        Created?.Invoke(go);
    }
}
