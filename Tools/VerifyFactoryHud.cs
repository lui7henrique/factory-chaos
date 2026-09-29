using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Run through the Unity MCP run_script tool in a fresh SampleScene Play session.
/// Exercises real components and HUD binding. It mutates only the disposable Play
/// session; stop Play afterwards. Kept outside Assets so it is never shipped.
/// </summary>
public static class VerifyFactoryHud
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> passed = new List<string>();
    static object Call(object target, string method, params object[] args)
    {
        return target.GetType().GetMethod(method, Private).Invoke(target, args);
    }
    static T Get<T>(object target, string field)
    {
        return (T)target.GetType().GetField(field, Private).GetValue(target);
    }
    static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, Private).SetValue(target, value);
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("HUD verification failed: " + message);
        passed.Add(message);
    }
    static async Task Until(Func<bool> condition, string description, float timeout = 7f)
    {
        float limit = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > limit) throw new Exception("Timeout: " + description);
            await Task.Delay(50);
        }
    }
    static Item ItemAt(ItemKind kind, Vector3 position)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = "HUD verification " + kind;
        item.transform.position = position;
        item.transform.localScale = Vector3.one * 0.24f;
        item.AddComponent<Rigidbody>().useGravity = false;
        Item component = item.AddComponent<Item>();
        component.SetKind(kind);
        Physics.SyncTransforms();
        return component;
    }

    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new Exception("Enter Play in SampleScene first.");
        passed.Clear();
        FactoryHud hud = Object.FindAnyObjectByType<FactoryHud>();
        PlayerLoadout loadout = Object.FindAnyObjectByType<PlayerLoadout>();
        PlayerMovement movement = Object.FindAnyObjectByType<PlayerMovement>();
        PlayerCarry carry = Object.FindAnyObjectByType<PlayerCarry>();
        Camera camera = Camera.main;
        CannonController cannon = Object.FindAnyObjectByType<CannonController>();
        WaveDirector wave = Object.FindAnyObjectByType<WaveDirector>();
        OreMachine furnace = Object.FindAnyObjectByType<OreMachine>();
        AmmoMachine press = Object.FindAnyObjectByType<AmmoMachine>();
        ConveyorBelt belt = Object.FindAnyObjectByType<ConveyorBelt>();
        Check(hud != null && FactoryHud.IsPresent && loadout != null, "HUD starts automatically with the existing loadout");
        float oldScale = Time.timeScale;
        bool oldBackground = Application.runInBackground;
        try
        {
            Application.runInBackground = true;
            Time.timeScale = 0f;
            movement.enabled = false;
            movement.GetComponent<CharacterController>().enabled = false;
            movement.transform.SetPositionAndRotation(new Vector3(0f, 60f, 0f), Quaternion.identity);
            camera.transform.localRotation = Quaternion.identity;
            Cursor.lockState = CursorLockMode.Locked;
            Call(hud, "LateUpdate");

            Item ore = ItemAt(ItemKind.Ore, camera.transform.position + Vector3.forward * 3.3f);
            Call(hud, "LateUpdate");
            Check(Get<Item>(hud, "lookedItem") == null, "No pickup prompt beyond the real 3m reach");
            ore.transform.position = camera.transform.position + Vector3.forward * 2f;
            Physics.SyncTransforms(); Call(hud, "LateUpdate");
            Check(Get<string>(hud, "promptKey") == "E" && Get<string>(hud, "promptTitle").Contains("MINÉRIO"), "Pickup prompt comes from the raycast item");
            Check((bool)Call(loadout, "TryStore") && loadout.SlotItem(0) == ore, "Pickup populates the first physical inventory slot");
            Item ingot = ItemAt(ItemKind.Product, camera.transform.position + Vector3.forward * 2f);
            Call(loadout, "TryStore");
            Item ammo = ItemAt(ItemKind.Ammo, camera.transform.position + Vector3.forward * 2f);
            Call(loadout, "TryStore");
            Check(loadout.SlotItem(1) == ingot && loadout.SlotItem(2) == ammo, "All three item kinds bind to the real pockets");
            Item extra = ItemAt(ItemKind.Ore, camera.transform.position + Vector3.forward * 2f);
            Call(hud, "LateUpdate");
            Check(Get<string>(hud, "promptTitle") == "INVENTÁRIO CHEIO" && Get<string>(hud, "promptKey") == null, "Full inventory does not advertise an impossible pickup");
            Object.Destroy(extra.gameObject);
            await Task.Yield();
            Call(loadout, "Select", 1); Call(hud, "LateUpdate");
            Check(loadout.SelectedIndex == 1 && carry.IsHolding && Get<string>(hud, "promptTitle") == "SOLTAR ITEM", "Selected slot and held-item action stay in sync");
            Call(loadout, "Select", -1);

            // Two stations under one shared root catch the previous root-search bug.
            GameObject stations = new GameObject("HUD verification stations");
            stations.transform.position = new Vector3(20f, 60f, 0f);
            GameObject a = new GameObject("Furnace"); a.transform.SetParent(stations.transform, false);
            GameObject input = new GameObject("Input"); input.transform.SetParent(a.transform, false);
            OreMachine testFurnace = input.AddComponent<OreMachine>();
            GameObject b = new GameObject("Press"); b.transform.SetParent(stations.transform, false);
            GameObject bInput = new GameObject("Input"); bInput.transform.SetParent(b.transform, false);
            AmmoMachine testPress = bInput.AddComponent<AmmoMachine>();
            GameObject body = new GameObject("VisibleBody"); body.transform.SetParent(b.transform, false);
            Check(PlayerLoadout.FindMachineInput<OreMachine>(body.transform) == null
                && PlayerLoadout.FindMachineInput<AmmoMachine>(body.transform) == testPress,
                "Shared station root cannot route a press interaction to the furnace");
            Object.Destroy(stations);

            Check(wave.Current == WaveDirector.Phase.Preparing && wave.SecondsLeft <= wave.PreparationDuration,
                "Preparation HUD binds to the real wave clock");
            Time.timeScale = 1f;
            Item inputOre = ItemAt(ItemKind.Ore, new Vector3(0f, 80f, 0f));
            Check(furnace.TryDeposit(inputOre) && furnace.IsProcessing && furnace.ProcessSecondsLeft > 0f,
                "Furnace process and remaining time are live");
            await Until(() => !furnace.IsProcessing, "furnace finishes");
            Check(furnace.ProcessSecondsLeft == 0f, "Furnace returns to free when production finishes");

            Transform output = Get<Transform>(press, "outputPoint");
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "HUD verification output blocker";
            blocker.transform.position = output.position;
            blocker.transform.localScale = Vector3.one * 0.3f;
            Physics.SyncTransforms();
            Item inputIngot = ItemAt(ItemKind.Product, new Vector3(0f, 80f, 2f));
            Check(press.TryDeposit(inputIngot) && press.IsPressing && press.PendingCount == 1,
                "Press telemetry follows its real queue and cycle");
            await Until(() => press.IsOutputBlocked, "press output blockage");
            Check(!press.IsPressing && press.PendingCount == 1, "Blocked output preserves the queued product");
            Object.Destroy(blocker);
            await Until(() => press.PendingCount == 0, "press recovers after unblocking");
            Check(!press.IsOutputBlocked, "Blockage indicator clears after the item is ejected");

            Set(belt, "jammed", false); Set(belt, "nextSwitchTime", Time.time - 1f); Call(belt, "TickJam");
            Check(belt.IsJammed && belt.JamSecondsLeft > 0f, "Conveyor alert reads the real jam and recovery timer");
            Set(belt, "nextSwitchTime", Time.time - 1f); Call(belt, "TickJam");
            Check(!belt.IsJammed && belt.JamSecondsLeft == 0f, "Conveyor automatically returns to running");

            while (!cannon.IsFull) cannon.TryAddRound();
            Check(cannon.Rounds == cannon.Capacity && !cannon.TryAddRound(), "Ammo counter respects actual cannon capacity");
            Call(cannon, "Enter"); Call(hud, "LateUpdate");
            Check(cannon.IsOperating && Get<string>(hud, "promptTitle") == null, "Cannon mode suppresses inventory interaction prompts");
            int rounds = cannon.Rounds; Call(cannon, "Fire");
            Check(cannon.Rounds == rounds - 1, "A real shot decrements the displayed ammunition source");
            Call(cannon, "Exit");
            Check(!cannon.IsOperating && carry.InputEnabled, "Leaving cannon restores normal player interactions");

            Time.timeScale = 0f;
            Call(wave, "Release");
            Check(wave.Current == WaveDirector.Phase.Incoming && wave.Enemy != null, "Wave transitions to the real incoming enemy");
            wave.Enemy.TakeDamage(20f);
            Check(wave.Enemy.Health == 40f, "Enemy health source reflects damage");
            wave.Enemy.TakeDamage(40f); Call(wave, "Update");
            Check(wave.Current == WaveDirector.Phase.Cleared, "Victory is driven by the enemy's death");
            Call(wave, "Release");
            wave.Enemy.transform.position = new Vector3(1.6f, 0f, 7f); Call(wave, "Update");
            Check(wave.Current == WaveDirector.Phase.Breached, "Defeat is driven by the enemy reaching the factory");

            var pause = Object.FindAnyObjectByType<GamePauseMenu>();
            Time.timeScale = 1f; Call(pause, "PauseGame");
            Check(GamePauseMenu.IsOpen && Time.timeScale == 0f, "Existing pause freezes gameplay and gates the HUD");
            Call(pause, "ResumeGame");
            Check(!GamePauseMenu.IsOpen && Time.timeScale == 1f, "Resume restores gameplay timing");
            return new { count = passed.Count, passed = passed.ToArray() };
        }
        finally { Time.timeScale = oldScale; Application.runInBackground = oldBackground; }
    }
}
