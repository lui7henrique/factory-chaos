using UnityEditor;
using UnityEngine;

public static class OreVeinMenu
{
    const string OrePrefabPath = "Assets/Prefabs/Ore.prefab";
    const string RockPath = "Assets/Materials/Ore.mat";
    const string DarkPath = "Assets/Materials/OreDark.mat";
    const string CrystalPath = "Assets/Materials/OreVein.mat";

    [MenuItem("GameObject/Factory Chaos/Create Ore Vein")]
    static void CreateOreVein()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Create the ore vein in edit mode.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OrePrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing " + OrePrefabPath + ". The vein was not created.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Ore Vein");

        OreVein[] existing = Object.FindObjectsByType<OreVein>();
        Vector3 position = new Vector3(-6f + existing.Length * 2.4f, 0f, -5f);

        GameObject root = new GameObject("OreVein");
        Undo.RegisterCreatedObjectUndo(root, "Create Ore Vein");
        root.transform.position = position;

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.12f, 0f);
        box.size = new Vector3(1.7f, 0.36f, 1.7f);

        OreVein vein = root.AddComponent<OreVein>();
        root.AddComponent<OreVeinVisual>();
        SerializedObject serialized = new SerializedObject(vein);
        serialized.FindProperty("orePrefab").objectReferenceValue = prefab;
        serialized.FindProperty("reserve").intValue = 20;
        serialized.FindProperty("strikesPerUnit").intValue = 3;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        OreVeinVisual.Palette palette = new OreVeinVisual.Palette
        {
            rock = AssetDatabase.LoadAssetAtPath<Material>(RockPath),
            dark = AssetDatabase.LoadAssetAtPath<Material>(DarkPath),
            crystal = AssetDatabase.LoadAssetAtPath<Material>(CrystalPath)
        };
        OreVeinVisual.Build(root.transform, palette, go => Undo.RegisterCreatedObjectUndo(go, "Create Ore Vein"));

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = root;
        Debug.Log("Ore vein created. It uses the existing ore prefab and does not save the scene.", root);
    }

    [MenuItem("GameObject/Factory Chaos/Setup Player Pickaxe")]
    static void SetupPlayerPickaxe()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Set up the pickaxe in edit mode.");
            return;
        }

        PlayerMovement movement = Object.FindAnyObjectByType<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogWarning("No Player with PlayerMovement was found.");
            return;
        }

        PlayerMining mining = movement.GetComponent<PlayerMining>();
        if (mining != null)
        {
            Selection.activeGameObject = movement.gameObject;
            Debug.Log("Player already has a pickaxe. No duplicate component was added.", mining);
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Player Pickaxe");
        mining = Undo.AddComponent<PlayerMining>(movement.gameObject);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = movement.gameObject;
        Debug.Log("Pickaxe mining added to the player. The tool appears when Play starts with empty hands.", mining);
    }
}
