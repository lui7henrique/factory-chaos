using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the yard and furnace looks when Play starts, if the editor menu was not run.
/// The saved scene is left unchanged. The ground collider stays on the flat plane.
/// </summary>
public static class PlaySceneVisuals
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (FactorySite.IsIndoor)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
            return;

        ApplyGround();
        WorkshopLook.Apply();
        ApplyFurnace();
    }

    static void ApplyGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null || ground.transform.Find("GroundDecoration") != null)
            return;

        MeshFilter filter = ground.GetComponent<MeshFilter>();
        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        if (filter == null || renderer == null)
            return;

        GroundLook look = ground.GetComponent<GroundLook>();
        GroundSurface.Settings settings = GroundSurface.Read(look, ground.transform);
        Mesh surface = new Mesh();
        GroundSurface.FillSurface(surface, settings);
        filter.sharedMesh = surface;
        renderer.sharedMaterials = new[]
        {
            Lit(new Color(0.5f, 0.54f, 0.36f), false),
            Lit(new Color(0.56f, 0.6f, 0.4f), false),
            Lit(new Color(0.42f, 0.47f, 0.3f), false),
            Lit(new Color(0.73f, 0.58f, 0.37f), false),
            Lit(new Color(0.78f, 0.63f, 0.43f), false)
        };

        GroundSurface.KeepOut[] keepOuts = KeepOuts();
        Mesh tufts = new Mesh();
        Mesh rocks = new Mesh();
        GroundSurface.FillTufts(tufts, settings, keepOuts);
        GroundSurface.FillRocks(rocks, settings, keepOuts);

        Transform oldStones = ground.transform.Find("Stones");
        if (oldStones != null)
            Object.Destroy(oldStones.gameObject);

        GameObject decoration = new GameObject("GroundDecoration");
        decoration.transform.SetParent(ground.transform, false);
        Vector3 scale = ground.transform.localScale;
        decoration.transform.localScale = new Vector3(1f / Safe(scale.x), 1f, 1f / Safe(scale.z));

        Prop(decoration.transform, "Tufts", tufts, new[]
        {
            Lit(new Color(0.455f, 0.514f, 0.278f), false),
            Lit(new Color(0.588f, 0.639f, 0.380f), false)
        });
        Prop(decoration.transform, "Rocks", rocks, new[]
        {
            Lit(new Color(0.522f, 0.529f, 0.494f), false)
        });
    }

    static void ApplyFurnace()
    {
        GameObject machine = GameObject.Find("Machine");
        if (machine == null || machine.transform.Find("Visual") != null)
            return;

        FurnaceVisual.Rebuild(machine.transform, ArtMaterials.Furnace(ArtMaterials.Runtime()), FurnaceVisual.BuildMeshes());

        OreMachine oreMachine = machine.GetComponentInChildren<OreMachine>(true);
        Renderer lens = FurnaceVisual.FindLens(machine.transform);
        if (oreMachine != null)
            oreMachine.AssignStatus(lens);

        Transform fire = machine.transform.Find("FireVisual");
        if (fire == null || oreMachine == null)
            return;

        FurnaceFire motion = fire.GetComponent<FurnaceFire>();
        if (motion == null)
            motion = fire.gameObject.AddComponent<FurnaceFire>();
        motion.Bind(oreMachine);
    }

    static GroundSurface.KeepOut[] KeepOuts()
    {
        var list = new List<GroundSurface.KeepOut>();
        Add<PlayerMovement>(list, 0.6f, false);
        Add<OreMachine>(list, 0.5f, true);
        Add<DeliveryZone>(list, 0.5f, true);
        Add<ConveyorBelt>(list, 0.35f, true);
        Add<OreVein>(list, 0.7f, false);
        Add<OreSpawner>(list, 1.2f, false);
        Add<CannonController>(list, 0.6f, false);
        Add<AmmoMachine>(list, 0.6f, true);
        Add<TargetDummy>(list, 0.5f, false);
        list.Add(new GroundSurface.KeepOut { center = new Vector2(6f, -1f), radius = 2.2f });
        list.Add(new GroundSurface.KeepOut { center = new Vector2(6f, 3.5f), radius = 2.4f });
        list.Add(new GroundSurface.KeepOut { center = new Vector2(6f, 9.2f), radius = 1.8f });
        list.Add(new GroundSurface.KeepOut { center = new Vector2(-6f, -5f), radius = 2.2f });
        return list.ToArray();
    }

    static void Add<T>(List<GroundSurface.KeepOut> list, float pad, bool includeParent) where T : Component
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
        for (int i = 0; i < found.Length; i++)
        {
            Transform host = found[i].transform;
            if (includeParent && host.parent != null)
                host = host.parent;

            Collider[] colliders = host.GetComponentsInChildren<Collider>();
            Bounds bounds = new Bounds(host.position, Vector3.zero);
            bool any = false;
            for (int c = 0; c < colliders.Length; c++)
            {
                if (colliders[c] == null || !colliders[c].enabled)
                    continue;
                if (!any)
                {
                    bounds = colliders[c].bounds;
                    any = true;
                }
                else
                    bounds.Encapsulate(colliders[c].bounds);
            }

            Vector3 center = any ? bounds.center : host.position;
            float radius = any ? Mathf.Max(bounds.extents.x, bounds.extents.z) + pad : pad;
            list.Add(new GroundSurface.KeepOut { center = new Vector2(center.x, center.z), radius = radius });
        }
    }

    static void Prop(Transform parent, string name, Mesh mesh, Material[] materials)
    {
        if (mesh == null || mesh.vertexCount == 0)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;
    }

    static Material Lit(Color color, bool emissive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0.05f);
        material.SetFloat("_Metallic", 0f);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.65f);
        }

        return material;
    }

    static float Safe(float scale)
    {
        return Mathf.Abs(scale) < 0.0001f ? 1f : scale;
    }
}
