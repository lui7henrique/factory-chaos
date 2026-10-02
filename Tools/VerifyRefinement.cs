using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

// Run in a fresh SampleScene Play session. This is a disposable integration scene,
// not a shipped component. Stop Play afterwards. No preferences are saved.
public static class VerifyRefinement
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> passed = new List<string>();
    static readonly List<string> errors = new List<string>();
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("Refinement: " + name);
        passed.Add(name);
    }
    static void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
    }
    static async Task Until(Func<bool> condition, string name, float seconds = 5f)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end) throw new Exception("Timed out: " + name);
            await Task.Delay(25);
        }
    }
    static Item MakeItem(ItemKind kind, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Refinement test " + kind;
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.3f;
        go.GetComponent<Renderer>().sharedMaterial = ArtMaterials.Runtime().iron;
        go.AddComponent<Rigidbody>().useGravity = false;
        Item item = go.AddComponent<Item>(); item.SetKind(kind);
        Physics.SyncTransforms();
        return item;
    }
    static GameObject Station(string name, Vector3 position, out Transform input, out Transform output)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.position = position;
        go.GetComponent<Renderer>().sharedMaterial = ArtMaterials.Runtime().shell;
        input = new GameObject("Input").transform; input.SetParent(go.transform, false);
        output = new GameObject("Output").transform; output.SetParent(go.transform, false); output.localPosition = Vector3.right * 2f;
        return go;
    }

    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new Exception("Enter SampleScene Play first.");
        passed.Clear(); errors.Clear();
        Application.logMessageReceived += Log;
        float oldScale = Time.timeScale;
        bool oldBackground = Application.runInBackground;
        float volume = GamePreferences.Volume, sensitivity = GamePreferences.Sensitivity;
        bool invert = GamePreferences.InvertY, compact = GamePreferences.CompactHud;
        Mouse testMouse = null;
        try
        {
            Application.runInBackground = true;
            Time.timeScale = 0f;
            var movement = Object.FindAnyObjectByType<PlayerMovement>();
            var carry = movement.GetComponent<PlayerCarry>();
            var loadout = movement.GetComponent<PlayerLoadout>();
            var mining = movement.GetComponent<PlayerMining>();
            var hud = Object.FindAnyObjectByType<FactoryHud>();
            var pause = Object.FindAnyObjectByType<GamePauseMenu>();
            var cannon = Object.FindAnyObjectByType<CannonController>();
            movement.enabled = false;
            movement.GetComponent<CharacterController>().enabled = false;
            movement.transform.SetPositionAndRotation(new Vector3(0f, 60f, 0f), Quaternion.identity);
            var camera = Camera.main;
            camera.transform.localRotation = Quaternion.identity;
            Vector3 origin = camera.transform.position;
            Cursor.lockState = CursorLockMode.Locked;
            Check(hud.GetComponent<GameFeedback>() != null, "Feedback service is scene-local and starts with HUD");
            GamePreferences.Set(5f, 8f, true, true);
            Check(GamePreferences.Volume == 1f && GamePreferences.Sensitivity == 2.5f, "Settings clamp maximum values");
            GamePreferences.Set(-1f, -1f, false, false);
            Check(GamePreferences.Volume == 0f && GamePreferences.Sensitivity == 0.25f, "Settings clamp minimum values");
            GamePreferences.Set(volume, sensitivity, invert, compact);

            Item ore = MakeItem(ItemKind.Ore, origin + Vector3.forward * 2f);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = origin + Vector3.forward;
            wall.transform.localScale = new Vector3(1f, 1f, 0.1f);
            Physics.SyncTransforms();
            Check(!(bool)Call(loadout, "TryStore"), "Inventory cannot collect an item through a wall");
            Object.Destroy(wall); await Task.Yield();
            Call(loadout, "Select", 0);
            Check((bool)Call(loadout, "TryStore") && carry.IsHolding && ore.gameObject.activeSelf,
                "Pickup into the selected empty pocket immediately equips the item");
            ore.transform.localScale = Vector3.one * 0.7f;
            Call(carry, "LateUpdate"); Physics.SyncTransforms();
            Check(Get<Rigidbody>(carry, "heldBody").isKinematic, "Held item remains kinematic without velocity writes");
            carry.IgnoreInteractThisFrame();
            Check(!carry.CanInteractThisFrame, "Cannon exit can suppress the same E press in the loadout");

            GameObject furnaceRoot = Station("Refinement Furnace", origin + Vector3.forward * 2.8f, out Transform input, out Transform output);
            OreMachine furnace = input.gameObject.AddComponent<OreMachine>();
            Item productTemplate = MakeItem(ItemKind.Product, origin + Vector3.right * 30f);
            productTemplate.gameObject.SetActive(false);
            furnace.Configure(output, productTemplate.gameObject);
            Set(furnace, "processDuration", 0.08f);
            Physics.SyncTransforms();
            Call(loadout, "RefreshPrompt");
            Check(loadout.DepositAvailable && loadout.DepositTitle == "DEPOSITAR MINÉRIO", "Held collider no longer blocks the deposit ray");
            Call(loadout, "TryDeposit");
            Check(loadout.SlotItem(0) == null && !carry.IsHolding && furnace.IsProcessing, "M2 atomically moves held ore into the furnace");
            Check(!furnace.TryDeposit(ore), "A consumed item cannot enter twice in the same frame");
            Item blockage = MakeItem(ItemKind.Product, output.position);
            Time.timeScale = 1f;
            await Until(() => furnace.IsOutputBlocked, "furnace output blockage");
            Check(furnace.IsProcessing, "Furnace preserves its completed product while the output is occupied");
            Object.Destroy(blockage.gameObject);
            await Until(() => !furnace.IsProcessing, "furnace clears output");
            Check(!furnace.IsOutputBlocked, "Furnace resumes exactly once when its output clears");
            Time.timeScale = 0f;
            furnaceRoot.transform.position += Vector3.left * 15f;

            GameObject pressRoot = Station("Refinement Press", origin + Vector3.forward * 2.8f, out Transform pressInput, out Transform pressOutput);
            AmmoMachine press = pressInput.gameObject.AddComponent<AmmoMachine>();
            Item ammoTemplate = MakeItem(ItemKind.Ammo, origin + Vector3.right * 32f);
            ammoTemplate.gameObject.SetActive(false);
            Renderer lamp = pressRoot.GetComponent<Renderer>(); Material shared = lamp.sharedMaterial;
            press.Configure(pressOutput, ammoTemplate.gameObject, lamp);
            Set(press, "processDuration", 0.08f);
            Item ingot = MakeItem(ItemKind.Product, origin + Vector3.forward * 1.8f);
            Call(loadout, "TryStore"); Call(carry, "LateUpdate"); Physics.SyncTransforms();
            Call(loadout, "RefreshPrompt");
            Check(loadout.DepositAvailable && loadout.DepositTitle == "DEPOSITAR LINGOTE", "Held ingot targets only the press");
            Call(loadout, "TryDeposit");
            Check(!carry.IsHolding && press.PendingCount == 1, "M2 deposits a pocketed ingot into the real press queue");
            var lampBlock = new MaterialPropertyBlock(); lamp.GetPropertyBlock(lampBlock);
            Check(lamp.sharedMaterial == shared && lampBlock.GetColor("_BaseColor").r > 0.9f, "Press lamp uses a property block without cloning or damaging its material");
            var sharedStations = new GameObject("Refinement shared station root");
            pressRoot.transform.SetParent(sharedStations.transform, true);
            Item pressBlockage = MakeItem(ItemKind.Ammo, pressOutput.position);
            pressBlockage.transform.SetParent(sharedStations.transform, true);
            Physics.SyncTransforms();
            Time.timeScale = 1f;
            await Until(() => press.IsOutputBlocked, "shared-root blocker");
            Check(press.PendingCount == 1, "A sibling under the shared factory root can block the press output");
            Object.Destroy(pressBlockage.gameObject);
            await Until(() => press.PendingCount == 0, "press unblocks");
            Check(!press.IsOutputBlocked, "Press recovers after the sibling blocker is removed");
            Time.timeScale = 0f; pressRoot.transform.position += Vector3.left * 15f;

            GameObject deliveryRoot = Station("Refinement Delivery", origin + Vector3.forward * 2.8f, out Transform deliveryInput, out _);
            var delivery = deliveryInput.gameObject.AddComponent<DeliveryZone>();
            Item sale = MakeItem(ItemKind.Product, origin + Vector3.forward * 1.8f);
            Call(loadout, "TryStore"); Call(carry, "LateUpdate"); Physics.SyncTransforms(); Call(loadout, "RefreshPrompt");
            Check(loadout.DepositAvailable && loadout.DepositTitle == "ENTREGAR LINGOTE", "Delivery interaction advertises the real sale value");
            Call(loadout, "TryDeposit");
            Check(delivery.Money == delivery.ValuePerProduct && !carry.IsHolding && !delivery.TryDeposit(sale), "Delivery pays once and clears the hand even on same-frame repeats");
            deliveryRoot.transform.position += Vector3.left * 15f;

            cannon.transform.SetPositionAndRotation(origin + Vector3.forward * 2.8f - Vector3.up, Quaternion.identity);
            Item round = MakeItem(ItemKind.Ammo, origin + Vector3.forward * 1.8f);
            Call(loadout, "TryStore"); Call(carry, "LateUpdate");
            // The real cannon's collider may sit above the center ray; aim at its body.
            camera.transform.LookAt(cannon.transform.position + Vector3.up * 1.2f);
            Call(carry, "LateUpdate"); Physics.SyncTransforms(); Call(loadout, "RefreshPrompt");
            Check(loadout.DepositAvailable && loadout.DepositTitle == "CARREGAR CANHÃO", "M2 loading is offered on the real cannon body");
            Call(loadout, "TryDeposit");
            Check(cannon.Rounds == 1 && !carry.IsHolding, "M2 transfers one physical cartridge into the cannon");
            while (!cannon.IsFull) cannon.TryAddRound();
            Item overflow = MakeItem(ItemKind.Ammo, origin + Vector3.right * 20f);
            Check(!cannon.TryDeposit(overflow) && overflow.enabled, "A full cannon cannot consume extra ammunition");
            int rounds = cannon.Rounds;
            Call(cannon, "Fire"); Call(cannon, "Fire");
            Check(cannon.Rounds == rounds - 1 && cannon.ShotRecovery > 0f, "Shot recovery prevents two same-frame shots");

            GameObject enemyRoot = CaveBlockout.CreateEnemy(null, origin + Vector3.right * 10f, -100f);
            var enemy = enemyRoot.GetComponent<WaveEnemy>();
            Renderer enemyBody = enemyRoot.transform.Find("Body").GetComponent<Renderer>();
            Material material = enemyBody.sharedMaterial;
            enemy.Configure(-100f, enemyBody);
            Check(material != null && enemyBody.sharedMaterial == material && material.shader.isSupported, "Enemy configuration preserves a valid shared material");
            GameObject shotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shotObject.transform.position = origin + Vector3.right * 20f;
            shotObject.AddComponent<Rigidbody>();
            var shot = shotObject.AddComponent<Projectile>();
            var cannonCollider = cannon.GetComponentInChildren<Collider>();
            shot.Launch(Vector3.forward, 20f, 2f, new[] { cannonCollider });
            Call(shot, "Hit", cannonCollider);
            Check(!Get<bool>(shot, "spent"), "Projectile sweep and contact share the cannon-ignore filter");
            Call(shot, "Hit", enemyBody.GetComponent<Collider>());
            Check(enemy.Health == 40f && GameFeedback.HitLife > 0f, "Real projectile hit applies damage and visual confirmation");
            enemy.TakeDamage(40f);
            Check(enemy.IsDead && !enemyBody.GetComponent<Collider>().enabled, "Enemy death disables blocking collision immediately");
            Time.timeScale = 1f;
            await Task.Delay(420);
            Check(enemyBody.sharedMaterial != null, "Enemy flash and death never destroy the live material");

            // A full tap, not just a direct ApplyStrike: complete the existing swing
            // with the mouse released and let PlayerMining.Update deliver the impact.
            Time.timeScale = 0f;
            camera.transform.localRotation = Quaternion.identity;
            cannon.transform.position += Vector3.right * 20f;
            Call(loadout, "Select", -1);
            var veinObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            veinObject.name = "Refinement vein";
            veinObject.transform.position = origin + Vector3.forward * 2f;
            veinObject.GetComponent<Renderer>().sharedMaterial = ArtMaterials.Runtime().rock;
            var vein = veinObject.AddComponent<OreVein>();
            Physics.SyncTransforms();
            testMouse = InputSystem.AddDevice<Mouse>();
            Call(mining, "Aim");
            Check(mining.AimedVein == vein, "Mining ray resolves the test vein");
            Call(mining, "BeginSwing");
            Set(mining, "swingStart", Time.time - 0.65f);
            Call(mining, "Update");
            Check(vein.Strikes == 1, "Released mouse completes a committed pickaxe strike");
            Call(mining, "Update");
            Check(vein.Strikes == 1, "A single swing cannot apply its impact twice");
            InputSystem.RemoveDevice(testMouse); testMouse = null;

            Time.timeScale = 1f;
            Call(pause, "PauseGame");
            Check(GamePauseMenu.IsOpen && Time.timeScale == 0f, "New menu still freezes the simulation");
            Call(pause, "Activate", 1);
            Check(Get<object>(pause, "page").ToString() == "Settings", "Settings page opens from the menu");
            Call(pause, "Back"); Call(pause, "Activate", 2);
            Check(Get<object>(pause, "page").ToString() == "Confirm", "Restart requires explicit confirmation");
            Call(pause, "Activate", 0);
            Check(Get<object>(pause, "page").ToString() == "Main", "Cancel restart preserves the current scene");
            Call(pause, "ResumeGame");
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            GamePauseMenu.RequestRoomChange("IndoorFactory");
            Check(GamePauseMenu.IsOpen && Get<object>(pause, "page").ToString() == "Confirm"
                && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == scene, "Room comparison does not discard progress before confirmation");
            Call(pause, "Back"); Call(pause, "ResumeGame");
            Check(!GamePauseMenu.IsOpen && Time.timeScale == 1f, "Resume restores time after settings and confirmation flows");
            await Task.Delay(450);
            Check(errors.Count == 0, "No runtime errors during the expanded integration suite: " + string.Join("; ", errors));
            return new { count = passed.Count, passed = passed.ToArray() };
        }
        finally
        {
            if (testMouse != null) InputSystem.RemoveDevice(testMouse);
            Application.logMessageReceived -= Log;
            GamePreferences.Set(volume, sensitivity, invert, compact);
            Time.timeScale = oldScale;
            Application.runInBackground = oldBackground;
        }
    }
}
