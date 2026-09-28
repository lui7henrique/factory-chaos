using UnityEngine;

/// <summary>
/// Old runtime stones. The yard decoration now comes from Update Ground Visual.
/// </summary>
public static class GroundDressing
{
    public static void Build(Transform ground)
    {
        Transform existing = ground.Find("Stones");
        if (existing != null)
        {
            if (Application.isPlaying)
                Object.Destroy(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        GameObject root = new GameObject("Stones");
        root.transform.SetParent(ground, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = new Vector3(1f / ground.localScale.x, 1f, 1f / ground.localScale.z);

        Mesh rock = OreMesh.SharedRock();
        Material light = Stone(new Color(0.62f, 0.6f, 0.56f));
        Material dark = Stone(new Color(0.4f, 0.38f, 0.35f));

        Vector3[] spots =
        {
            new Vector3(-8.4f, 0f, 6.2f),
            new Vector3(-9.1f, 0f, 4.6f),
            new Vector3(7.6f, 0f, 7.4f),
            new Vector3(8.8f, 0f, 5.1f),
            new Vector3(9.2f, 0f, -6.8f),
            new Vector3(6.4f, 0f, -8.6f),
            new Vector3(-7.8f, 0f, -8.2f),
            new Vector3(-4.2f, 0f, 8.4f),
            new Vector3(2.8f, 0f, 8.8f),
            new Vector3(-9.4f, 0f, -1.6f),
            new Vector3(4.6f, 0f, -3.8f),
            new Vector3(-1.4f, 0f, 6.8f),
            new Vector3(8.1f, 0f, -1.2f),
            new Vector3(-5.6f, 0f, 3.4f)
        };

        for (int i = 0; i < spots.Length; i++)
        {
            float scale = 0.22f + Hash(i) * 0.28f;
            Quaternion rotation = Quaternion.Euler(Hash(i + 2) * 14f - 4f, Hash(i + 5) * 360f, Hash(i + 8) * 12f - 6f);
            Vector3 position = spots[i] + new Vector3(0f, -0.06f - Hash(i + 3) * 0.05f, 0f);

            GameObject stone = new GameObject(i % 4 == 0 ? "StoneDark" : "Stone");
            stone.transform.SetParent(root.transform, false);
            stone.transform.position = position;
            stone.transform.rotation = rotation;
            stone.transform.localScale = Vector3.one * scale;

            MeshFilter filter = stone.AddComponent<MeshFilter>();
            filter.sharedMesh = rock;
            MeshRenderer renderer = stone.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = i % 3 == 0 ? dark : light;
        }
    }

    static Material Stone(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0.04f);
        material.SetFloat("_Metallic", 0f);
        return material;
    }

    static float Hash(int i)
    {
        float value = Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
