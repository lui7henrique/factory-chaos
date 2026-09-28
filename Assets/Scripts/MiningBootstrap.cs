using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puts the pickaxe and the inventory on whichever room is playing.
/// The outdoor yard also gets a vein when the scene does not already have one.
/// </summary>
public static class MiningBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Ensure()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "SampleScene" && scene.name != "IndoorFactory")
            return;

        if (!FactorySite.IsIndoor && Object.FindAnyObjectByType<OreVein>() == null)
            CreateVein();

        GameObject player = GameObject.Find("Player");
        if (player == null)
            return;

        if (player.GetComponent<PlayerMining>() == null)
            player.AddComponent<PlayerMining>();
        if (player.GetComponent<PlayerLoadout>() == null)
            player.AddComponent<PlayerLoadout>();
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
