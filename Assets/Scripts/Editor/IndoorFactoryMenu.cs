using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IndoorFactoryMenu
{
    const string CubePath = "Assets/Meshes/IndoorCube.asset";

    [MenuItem("GameObject/Factory Chaos/Build Indoor Factory")]
    public static void BuildIndoorFactory()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Build the indoor factory in edit mode.");
            return;
        }

        bool exists = File.Exists(IndoorFactory.ScenePath);
        Scene active = SceneManager.GetActiveScene();
        bool openIndoor = active.path == IndoorFactory.ScenePath;

        if (exists && !openIndoor)
            EditorSceneManager.OpenScene(IndoorFactory.ScenePath, OpenSceneMode.Single);

        if (!exists)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Indoor Factory");

        IndoorGroup[] previous = Object.FindObjectsByType<IndoorGroup>(FindObjectsInactive.Include);
        for (int i = 0; i < previous.Length; i++)
        {
            if (previous[i] != null)
                Undo.DestroyObjectImmediate(previous[i].gameObject);
        }

        System.Action<GameObject> register = go =>
        {
            if (go != null)
                Undo.RegisterCreatedObjectUndo(go, "Build Indoor Factory");
        };

        System.Action<GameObject> previousFurnace = FurnaceVisual.Created;
        System.Action<GameObject> previousCannon = CannonVisual.Created;
        System.Action<GameObject> previousAmmo = AmmoMachineVisual.Created;
        IndoorFactory.Created = register;
        FurnaceVisual.Created = register;
        CannonVisual.Created = register;
        AmmoMachineVisual.Created = register;

        try
        {
            IndoorFactory.Build(LoadKit());
        }
        finally
        {
            IndoorFactory.Created = null;
            FurnaceVisual.Created = previousFurnace;
            CannonVisual.Created = previousCannon;
            AmmoMachineVisual.Created = previousAmmo;
        }

        Undo.CollapseUndoOperations(group);

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, IndoorFactory.ScenePath);
        AddToBuildSettings(IndoorFactory.ScenePath);
        Debug.Log(exists
            ? "Refreshed the generated groups in IndoorFactory."
            : "Created " + IndoorFactory.ScenePath + ".");
    }

    public static void BuildFromCommandLine()
    {
        BuildIndoorFactory();
        EditorApplication.Exit(0);
    }

    static IndoorFactory.Kit LoadKit()
    {
        ArtMaterials.Set art = ArtAssetsMenu.LoadOrCreate();
        GameObject ore = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ore.prefab");
        GameObject product = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Product.prefab");
        if (ore == null || product == null)
            Debug.LogWarning("Indoor factory is missing the Ore or Product prefab.");

        return new IndoorFactory.Kit
        {
            orePrefab = ore,
            productPrefab = product,
            concrete = art.concrete,
            wall = art.wall,
            structure = art.graphite,
            iron = art.iron,
            rock = art.rock,
            marking = art.marking,
            belt = art.graphite,
            cube = LoadCube(),
            furnace = ArtMaterials.Furnace(art),
            vein = new OreVeinVisual.Palette
            {
                rock = art.rock,
                dark = art.rock,
                crystal = art.crystal
            },
            press = new AmmoMachineVisual.Palette
            {
                structure = art.graphite,
                panel = art.shell,
                accent = art.marking,
                metal = art.iron,
                belt = art.graphite,
                lamp = art.lampReady
            },
            cannon = new CannonVisual.Palette
            {
                structure = art.graphite,
                support = art.shell,
                accent = art.marking,
                joint = art.iron,
                bore = art.bore,
                lamp = art.lampReady
            }
        };
    }

    static Mesh LoadCube()
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(CubePath);
        if (existing != null)
            return existing;

        string folder = "Assets/Meshes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Meshes");

        Mesh mesh = IndoorFactory.CreateFlatCube();
        AssetDatabase.CreateAsset(mesh, CubePath);
        return mesh;
    }

    static void AddToBuildSettings(string path)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].path == path)
                return;
        }

        var scenes = new List<EditorBuildSettingsScene>(current);
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
