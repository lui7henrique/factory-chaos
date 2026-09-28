using UnityEngine;

/// <summary>
/// Deepens the mob tunnel, paints the room walls as rock, and scatters extra ore veins.
/// </summary>
public static class MineDressing
{
    const float TunnelStart = 8f;
    const float TunnelDepth = 14f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        if (!FactorySite.IsIndoor)
            return;

        Expand();
        DeepenTunnel();
        PaintWalls();
        ScatterVeins();
        SeatChimneyDuct();
        ClearFurnaceFixture();
        ConveyorVisual.AlignToMachines();
        GameObject runner = new GameObject("TunnelDarkDress");
        runner.hideFlags = HideFlags.HideAndDontSave;
        runner.AddComponent<TunnelDarkDress>();
    }

    static void Expand()
    {
        GameObject shellObject = GameObject.Find("Shell");
        if (shellObject == null || shellObject.transform.localScale.x > 1.2f)
            return;

        shellObject.transform.localScale = Vector3.one * IndoorFactory.PlaySpread;
        SpreadRoots(GameObject.Find("Gameplay"));
        SpreadRoots(GameObject.Find("Lighting"));

        GameObject player = GameObject.Find("Player");
        if (player == null)
            return;

        Vector3 position = player.transform.position;
        player.transform.position = new Vector3(position.x * IndoorFactory.PlaySpread, position.y, position.z * IndoorFactory.PlaySpread);
    }

    static void SpreadRoots(GameObject root)
    {
        if (root == null)
            return;

        float spread = IndoorFactory.PlaySpread;
        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (child.name == "ChimneyDuct")
            {
                SpreadRoots(child.gameObject);
                continue;
            }

            Vector3 position = child.position;
            child.position = new Vector3(position.x * spread, position.y * spread, position.z * spread);
        }
    }

    static void ClearFurnaceFixture()
    {
        GameObject glow = GameObject.Find("FurnaceGlow");
        if (glow == null)
            return;

        HideChild(glow.transform, "Housing");
        HideChild(glow.transform, "Shade");
    }

    static void HideChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            child.gameObject.SetActive(false);
    }

    static void SeatChimneyDuct()
    {
        Transform duct = FindTransform("ChimneyDuct");
        Transform machine = FindTransform("Machine");
        if (duct == null || machine == null)
            return;

        Transform chimney = machine.Find("Visual/Chimney");
        if (chimney == null)
            return;

        Vector3 center = chimney.position;
        float halfHeight = 0.31f * Mathf.Abs(chimney.lossyScale.y);
        Vector3 top = new Vector3(center.x, center.y + halfHeight, center.z);

        float ceiling = 4.5f * IndoorFactory.PlaySpread;
        Transform shell = FindTransform("Shell");
        if (shell != null)
            ceiling = 4.5f * Mathf.Abs(shell.lossyScale.y);

        const float girth = 0.36f;
        float spread = IndoorFactory.PlaySpread;
        float runLength = 2.8f * spread;
        float runY = ceiling - girth * 0.5f - 0.05f;
        float bottom = top.y - 0.06f;
        float height = Mathf.Max(0.4f, runY - bottom);

        Transform riser = duct.Find("Riser");
        if (riser != null)
        {
            riser.position = new Vector3(top.x, bottom + height * 0.5f, top.z);
            riser.localScale = new Vector3(girth, height, girth);
        }

        Transform run = duct.Find("Run");
        if (run != null)
        {
            float centerZ = top.z - girth * 0.15f + runLength * 0.5f;
            run.position = new Vector3(top.x, runY, centerZ);
            run.localScale = new Vector3(girth, girth, runLength);
        }
    }

    static Transform FindTransform(string name)
    {
        GameObject found = GameObject.Find(name);
        return found != null ? found.transform : null;
    }

    static void DeepenTunnel()
    {
        GameObject tunnelObject = GameObject.Find("Tunnel");
        if (tunnelObject == null)
            return;

        Transform tunnel = tunnelObject.transform;
        Transform end = tunnel.Find("End");
        if (end != null && end.localPosition.z > 18f)
            return;

        float center = TunnelStart + TunnelDepth * 0.5f;
        Stretch(tunnel, "Floor", center, TunnelDepth);
        Stretch(tunnel, "FloorPlate", center, TunnelDepth - 0.04f);
        Stretch(tunnel, "RockLeft", center, TunnelDepth);
        Stretch(tunnel, "RockRight", center, TunnelDepth);
        Stretch(tunnel, "RockCeiling", center, TunnelDepth);

        if (end != null)
        {
            Vector3 position = end.localPosition;
            position.z = TunnelStart + TunnelDepth + 0.16f;
            end.localPosition = position;
        }

        Nudge(tunnel, "FacetA", 4.5f);
        Nudge(tunnel, "FacetB", 4.5f);
        Nudge(tunnel, "FacetC", 4.5f);

        GameObject target = GameObject.Find("Target");
        if (target != null)
        {
            Vector3 position = target.transform.position;
            position.z = TunnelStart * IndoorFactory.PlaySpread + 12f;
            target.transform.position = position;
        }
    }

    static void Stretch(Transform tunnel, string name, float centerZ, float length)
    {
        Transform piece = tunnel.Find(name);
        if (piece == null)
            return;

        Vector3 scale = piece.localScale;
        scale.z = length;
        piece.localScale = scale;

        Vector3 position = piece.localPosition;
        position.z = centerZ;
        piece.localPosition = position;
    }

    static void Nudge(Transform tunnel, string name, float deltaZ)
    {
        Transform piece = tunnel.Find(name);
        if (piece == null)
            return;

        Vector3 position = piece.localPosition;
        position.z += deltaZ;
        piece.localPosition = position;
    }

    static void PaintWalls()
    {
        Material rock = ArtMaterials.Runtime().rock;
        if (rock == null)
            return;

        string[] names =
        {
            "WallFront",
            "WallRight",
            "WallLeftMetal",
            "WallLeftRock",
            "BackLeft",
            "BackRight"
        };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject wall = GameObject.Find(names[i]);
            if (wall == null)
                continue;

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = rock;
        }
    }

    static void ScatterVeins()
    {
        if (GameObject.Find("OreClusters") != null)
            return;

        OreVein source = Object.FindAnyObjectByType<OreVein>();
        if (source == null || source.OrePrefab == null)
            return;

        Transform parent = new GameObject("OreClusters").transform;
        FactorySite site = Object.FindAnyObjectByType<FactorySite>();
        if (site != null)
            parent.SetParent(site.transform, false);

        float spread = IndoorFactory.PlaySpread;
        float moduleHeight = 4.5f * spread * 0.94f;
        float halfThick = (0.529f / 1.812f) * moduleHeight * 0.5f;
        float wall = -6f * spread;
        float baseX = wall + halfThick * 2f - 0.45f;
        const int count = 15;
        float zCenter = -3.9f * spread;
        Vector3[] spots = new Vector3[count];
        int placed = 0;
        int guard = 0;
        Random.State previous = Random.state;
        Random.InitState(41);
        while (placed < count && guard < 80)
        {
            guard++;
            float z = zCenter + (Random.value - 0.5f) * 2.8f;
            float y = 0.12f + Random.value * Random.value * 3.3f;
            float x = baseX + (Random.value - 0.45f) * 0.7f;
            Vector3 candidate = new Vector3(x, y, z);
            bool crowded = false;
            for (int i = 0; i < placed; i++)
            {
                if ((spots[i] - candidate).sqrMagnitude < 0.38f * 0.38f)
                {
                    crowded = true;
                    break;
                }
            }

            if (crowded)
                continue;

            spots[placed] = candidate;
            placed++;
        }

        for (int i = 0; i < placed; i++)
        {
            // Cluster front is local +Z. Yaw stays near 90 so the flat back stays on the wall.
            float yaw = 90f + (Random.value - 0.5f) * 46f;
            float tilt = (Random.value - 0.5f) * 22f;
            float roll = (Random.value - 0.5f) * 28f;
            float scale = 0.5f + Random.value * 0.72f;
            Quaternion rotation = Quaternion.Euler(tilt, yaw, roll);
            if (i == 0)
            {
                source.transform.SetPositionAndRotation(spots[i], rotation);
                source.transform.localScale = Vector3.one * scale;
                source.SetSpawnOffset(new Vector3(1.05f, 0f, 0f));
                continue;
            }

            AddVein(parent, source, spots[i], yaw, scale);
            parent.GetChild(parent.childCount - 1).localRotation = rotation;
        }

        Random.state = previous;
    }

    static void AddVein(Transform parent, OreVein source, Vector3 position, float yaw, float scale)
    {
        GameObject vein = new GameObject("OreVein");
        vein.transform.SetParent(parent, false);
        vein.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        vein.transform.localScale = Vector3.one * scale;
        vein.AddComponent<BoxCollider>();

        OreVein ore = vein.AddComponent<OreVein>();
        ore.Configure(source.OrePrefab);
        ore.SetSpawnOffset(new Vector3(1.05f, 0f, 0f));
        vein.AddComponent<OreVeinVisual>();
    }

    public static void DarkenHole()
    {
        if (GameObject.Find("TunnelDark") != null)
            return;

        WarmMouth("TunnelLeft");
        WarmMouth("TunnelRight");

        Transform tunnel = null;
        GameObject tunnelObject = GameObject.Find("Tunnel");
        if (tunnelObject != null)
            tunnel = tunnelObject.transform;

        if (tunnel != null)
        {
            Tint(tunnel.Find("Floor"), 0.9f);
            Tint(tunnel.Find("FloorPlate"), 0.9f);
            Tint(tunnel.Find("RockLeft"), 0.85f);
            Tint(tunnel.Find("RockRight"), 0.85f);
            Tint(tunnel.Find("RockCeiling"), 0.92f);
            Tint(tunnel.Find("End"), 1f);
            Tint(tunnel.Find("FacetA"), 0.8f);
            Tint(tunnel.Find("FacetB"), 0.8f);
            Tint(tunnel.Find("FacetC"), 0.85f);
        }

        GameObject walls = GameObject.Find("RockWalls");
        if (walls != null)
        {
            float spread = IndoorFactory.PlaySpread;
            float mouth = TunnelStart * spread;
            float end = (TunnelStart + TunnelDepth) * spread;
            for (int i = 0; i < walls.transform.childCount; i++)
            {
                Transform piece = walls.transform.GetChild(i);
                if (!piece.name.StartsWith("Tunnel"))
                    continue;

                float depth = Mathf.InverseLerp(mouth, end, piece.position.z);
                Tint(piece, Mathf.Lerp(0.55f, 1f, depth));
            }
        }

        HangShade();
        HangRedEye();
    }

    static void WarmMouth(string name)
    {
        GameObject lamp = GameObject.Find(name);
        if (lamp == null)
            return;

        Light[] lights = lamp.GetComponentsInChildren<Light>(true);
        Color warm = new Color(1f, 0.72f, 0.42f);
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            light.enabled = true;
            light.type = LightType.Point;
            light.color = warm;
            light.intensity = 1.6f;
            light.range = 4.2f;
            light.shadows = LightShadows.None;
        }
    }

    static void HangRedEye()
    {
        float spread = IndoorFactory.PlaySpread;
        float z = (TunnelStart + TunnelDepth * 0.72f) * spread;
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Collider collider = eye.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        eye.name = "TunnelEye";
        eye.transform.SetPositionAndRotation(new Vector3(0f, 1.35f, z), Quaternion.identity);
        eye.transform.localScale = Vector3.one * 0.22f;
        FactorySite site = Object.FindAnyObjectByType<FactorySite>();
        if (site != null)
            eye.transform.SetParent(site.transform, true);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            return;

        Material glow = new Material(shader);
        Color red = new Color(1.4f, 0.08f, 0.05f);
        glow.SetColor("_BaseColor", red);
        glow.SetColor("_Color", red);
        glow.SetColor("_EmissionColor", red * 4f);
        glow.EnableKeyword("_EMISSION");
        glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        MeshRenderer renderer = eye.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = glow;
    }

    static void Tint(Transform piece, float darkness)
    {
        if (piece == null)
            return;

        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Color color = Color.Lerp(new Color(0.09f, 0.085f, 0.08f), new Color(0.008f, 0.008f, 0.009f), darkness);
        for (int i = 0; i < renderers.Length; i++)
        {
            block.Clear();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderers[i].SetPropertyBlock(block);
        }
    }

    static void HangShade()
    {
        Shader shader = Shader.Find("FactoryChaos/TunnelShade");
        if (shader == null)
            return;

        Material shade = new Material(shader);
        float spread = IndoorFactory.PlaySpread;
        float mouth = TunnelStart * spread + 1.2f;
        float end = (TunnelStart + TunnelDepth) * spread - 0.6f;
        float width = 3.7f * spread;
        float height = 3.15f * spread;

        Transform group = new GameObject("TunnelDark").transform;
        FactorySite site = Object.FindAnyObjectByType<FactorySite>();
        if (site != null)
            group.SetParent(site.transform, false);

        const int slices = 7;
        for (int i = 0; i < slices; i++)
        {
            float t = (i + 1f) / (slices + 1f);
            GameObject card = new GameObject("Shade" + i);
            card.transform.SetParent(group, false);
            card.transform.SetPositionAndRotation(
                new Vector3(0f, height * 0.5f, Mathf.Lerp(mouth, end, t)),
                Quaternion.Euler(0f, 180f, 0f));
            card.transform.localScale = new Vector3(width, height, 1f);

            MeshFilter filter = card.AddComponent<MeshFilter>();
            filter.sharedMesh = ShadeMesh();
            MeshRenderer renderer = card.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = shade;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            float alpha = Mathf.Lerp(0.12f, 0.5f, t);
            block.SetColor("_Color", new Color(0.004f, 0.004f, 0.005f, alpha));
            renderer.SetPropertyBlock(block);
        }
    }

    static Mesh shadeMesh;

    static Mesh ShadeMesh()
    {
        if (shadeMesh != null)
            return shadeMesh;

        shadeMesh = new Mesh { name = "TunnelShade" };
        shadeMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        shadeMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        shadeMesh.RecalculateBounds();
        return shadeMesh;
    }
}

public class TunnelDarkDress : MonoBehaviour
{
    void LateUpdate()
    {
        MineDressing.DarkenHole();
        Destroy(gameObject);
    }
}
