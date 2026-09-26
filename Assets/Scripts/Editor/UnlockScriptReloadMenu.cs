using UnityEditor;
using UnityEngine;

public static class UnlockScriptReloadMenu
{
    // Unity keeps a lock counter; one unlock may leave reload still blocked.
    const int MaxUnlocks = 32;

    [MenuItem("GameObject/Factory Chaos/Unlock Script Reload")]
    static void UnlockScriptReload()
    {
        for (int i = 0; i < MaxUnlocks; i++)
            EditorApplication.UnlockReloadAssemblies();

        Debug.Log(
            "Factory Chaos: called EditorApplication.UnlockReloadAssemblies() "
            + MaxUnlocks
            + " time(s) to clear a stuck assembly-reload lock. Wait for any compile, then enter Play again.");
    }
}
