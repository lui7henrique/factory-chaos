using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gives the sample scene one ore vein and a pickaxe when they were not placed in edit mode.
/// </summary>
public static class MiningBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (FactorySite.IsIndoor)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
            return;

        if (Object.FindAnyObjectByType<OreVein>() == null)
            CreateVein();

        GameObject player = GameObject.Find("Player");
        if (player != null && player.GetComponent<PlayerMining>() == null)
            player.AddComponent<PlayerMining>();
    }

    static void CreateVein()
    {
        GameObject root = new GameObject("OreVein");
        root.transform.position = new Vector3(-6f, 0f, -5f);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.12f, 0f);
        box.size = new Vector3(1.7f, 0.36f, 1.7f);

        OreVein vein = root.AddComponent<OreVein>();
        root.AddComponent<OreVeinVisual>();
        OreSpawner spawner = Object.FindAnyObjectByType<OreSpawner>();
        if (spawner != null)
            vein.Configure(spawner.OrePrefab);

        OreVeinVisual.Build(root.transform, OreVeinVisual.RuntimePalette(), null);
    }
}
