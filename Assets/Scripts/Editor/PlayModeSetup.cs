using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Opens the factory scene when the editor is sitting on an empty one,
/// and spawns the combat props even if play mode skips a scene reload.
/// </summary>
[InitializeOnLoad]
static class PlayModeSetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    static PlayModeSetup()
    {
        if (EditorSettings.enterPlayModeOptionsEnabled)
            EditorSettings.enterPlayModeOptionsEnabled = false;

        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.delayCall += OpenFactorySceneIfEmpty;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            CombatTestSpawner.Ensure();
            CaveBlockout.Ensure();
        }
    }

    static void OpenFactorySceneIfEmpty()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(scene.path))
            return;

        EditorSceneManager.OpenScene(ScenePath);
    }
}
