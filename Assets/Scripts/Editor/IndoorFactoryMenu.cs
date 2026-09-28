using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IndoorFactoryMenu
{
    const string CubePath = "Assets/Meshes/IndoorCube.asset";
    const string SourceMaterialPath = "Assets/Materials/Machine.mat";
    const string BeltPath = "Assets/Materials/Conveyor.mat";

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
        {
            Debug.LogWarning("IndoorFactory already exists at " + IndoorFactory.ScenePath + ". Open that scene and run the command again to refresh Environment, Lighting, Gameplay, Player and UI. The file was not overwritten.");
            return;
        }

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
        if (!exists)
        {
            EditorSceneManager.SaveScene(scene, IndoorFactory.ScenePath);
            AddToBuildSettings(IndoorFactory.ScenePath);
            Debug.Log("Created " + IndoorFactory.ScenePath + ".");
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Refreshed the generated groups in IndoorFactory. Save the scene to keep the result.");
        }
    }

    public static void BuildFromCommandLine()
    {
        BuildIndoorFactory();
        EditorApplication.Exit(0);
    }

    static IndoorFactory.Kit LoadKit()
    {
        ArtMaterials.Set art = ArtAssetsMenu.LoadOrCreate();
        Material rock = Save("Assets/Art/Materials/IndoorRock.mat", "IndoorRock", Hex(0x514D4B), 0.05f, 0f);
        GameObject ore = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ore.prefab");
        GameObject product = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Product.prefab");
        if (ore == null || product == null)
            Debug.LogWarning("Indoor factory is missing the Ore or Product prefab.");

        return new IndoorFactory.Kit
        {
            orePrefab = ore,
            productPrefab = product,
            concrete = Save("Assets/Art/Materials/IndoorConcrete.mat", "IndoorConcrete", Hex(0x73716B), 0.06f, 0f),
            concreteAlt = Save("Assets/Art/Materials/IndoorConcreteAlt.mat", "IndoorConcreteAlt", Hex(0x6A6862), 0.06f, 0f),
            panel = Save("Assets/Art/Materials/IndoorPanel.mat", "IndoorPanel", Hex(0x686D72), 0.16f, 0.22f),
            structure = Save("Assets/Art/Materials/IndoorStructure.mat", "IndoorStructure", Hex(0x343A40), 0.12f, 0.18f),
            rock = rock,
            marking = Save("Assets/Art/Materials/IndoorMarking.mat", "IndoorMarking", Hex(0xF4BE32), 0.12f, 0.04f),
            belt = AssetDatabase.LoadAssetAtPath<Material>(BeltPath),
            cube = LoadCube(),
            furnace = ArtMaterials.Furnace(art),
            vein = new OreVeinVisual.Palette
            {
                rock = rock,
                dark = rock,
                crystal = art.crystal
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

    static Material Save(string path, string materialName, Color color, float smoothness, float metallic)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            ArtMaterials.Paint(existing, color, smoothness, metallic, false);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        Material material = source != null
            ? new Material(source) { name = materialName }
            : ArtMaterials.Make(materialName, color, smoothness, metallic, false);
        ArtMaterials.Paint(material, color, smoothness, metallic, false);
        string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        AssetDatabase.CreateAsset(material, path);
        return material;
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

    static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
