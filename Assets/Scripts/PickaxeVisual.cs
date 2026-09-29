using UnityEngine;

/// <summary>
/// First-person pickaxe. ToolAnchor holds the presentation pose.
/// SwingPivot rotates at the grip. The model has no collider and no rigidbody.
/// </summary>
public class PickaxeVisual : MonoBehaviour
{
    public const float ImpactTime = 0.62f;

    [SerializeField] Transform swingPivot;
    [SerializeField] Vector3 presentationPosition = new Vector3(0.29f, -0.56f, 0.62f);
    [SerializeField] Vector3 presentationEuler = new Vector3(18f, -3f, 14f);
    [SerializeField] float presentationScale = 0.62f;

    static Palette runtimePalette;

    static readonly Quaternion Raised = Quaternion.Euler(-48f, 12f, -8f);
    static readonly Quaternion Impact = Quaternion.Euler(70f, -8f, 4f);
    static readonly Quaternion StrikeFacing = Quaternion.Euler(0f, 245f, 0f);

    public Vector3 PresentationPosition => presentationPosition;
    public Vector3 PresentationEuler => presentationEuler;
    public float PresentationScale => presentationScale;

    void OnEnable()
    {
        ApplyPresentation();
        FaceTheStrike();
        SetCycle(0f);
    }

    public void UseHeldPose()
    {
        presentationPosition = new Vector3(0.29f, -0.56f, 0.62f);
        presentationEuler = new Vector3(18f, -3f, 14f);
        presentationScale = 0.62f;
        ApplyPresentation();
        FaceTheStrike();
    }

    public void ApplyPresentation()
    {
        transform.localPosition = presentationPosition;
        transform.localRotation = Quaternion.Euler(presentationEuler);
        transform.localScale = Vector3.one * presentationScale;
    }

    void FaceTheStrike()
    {
        Transform pivot = swingPivot != null ? swingPivot : transform.Find("SwingPivot");
        Transform model = pivot != null ? pivot.Find("Visual") : null;
        if (model == null)
            return;

        model.localRotation = StrikeFacing;
    }

    public void SetCycle(float amount)
    {
        if (swingPivot == null)
            swingPivot = transform.Find("SwingPivot");

        if (swingPivot == null)
            return;

        swingPivot.localPosition = Vector3.zero;
        swingPivot.localRotation = Pose(Mathf.Clamp01(amount));
    }

    public static void BuildRuntime(Transform cameraTransform)
    {
        GameObject anchor = new GameObject("ToolAnchor");
        anchor.transform.SetParent(cameraTransform, false);
        PickaxeVisual visual = anchor.AddComponent<PickaxeVisual>();
        visual.ApplyPresentation();
        PickaxeMesh.Set meshes = PickaxeMesh.Create();
        Palette palette = RuntimePalette();
        visual.Rebuild(palette, meshes, null);
        visual.SetCycle(0f);
    }

    public void Rebuild(Palette palette, PickaxeMesh.Set meshes, System.Action<GameObject> created)
    {
        Transform pivot = transform.Find("SwingPivot");
        if (pivot == null)
        {
            GameObject pivotObject = new GameObject("SwingPivot");
            Track(created, pivotObject);
            pivotObject.transform.SetParent(transform, false);
            pivot = pivotObject.transform;
        }

        swingPivot = pivot;
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;

        Transform model = pivot.Find("Visual");
        if (model != null)
        {
            if (Application.isPlaying)
                Destroy(model.gameObject);
            else
                DestroyImmediate(model.gameObject);
        }

        GameObject visual = new GameObject("Visual");
        Track(created, visual);
        visual.transform.SetParent(pivot, false);
        visual.transform.localPosition = new Vector3(0f, -0.08f, 0f);
        visual.transform.localRotation = StrikeFacing;
        visual.transform.localScale = Vector3.one;

        Part(visual.transform, "Handle", meshes.handle, palette.wood, created);
        Part(visual.transform, "Grip", meshes.grip, palette.grip, created);
        Part(visual.transform, "GripBands", meshes.gripBands, palette.wrap, created);
        Part(visual.transform, "ButtCap", meshes.buttCap, palette.darkIron, created);
        Part(visual.transform, "Collars", meshes.collars, palette.iron, created);
        Part(visual.transform, "Socket", meshes.socket, palette.darkIron, created);
        Part(visual.transform, "Head", meshes.head, palette.darkIron, created);
        Part(visual.transform, "PickEdge", meshes.pickEdge, palette.iron, created);
        Part(visual.transform, "ChiselEdge", meshes.chiselEdge, palette.iron, created);
        Part(visual.transform, "Peg", meshes.peg, palette.wood, created);
        RemoveHand(pivot);
    }

    static void RemoveHand(Transform pivot)
    {
        Transform existing = pivot.Find("Hand");
        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    static Quaternion Pose(float t)
    {
        const float windupEnd = 0.38f;
        if (t <= windupEnd)
        {
            float u = Mathf.SmoothStep(0f, 1f, t / windupEnd);
            return Quaternion.Slerp(Quaternion.identity, Raised, u);
        }

        if (t <= ImpactTime)
        {
            float u = Mathf.SmoothStep(0f, 1f, (t - windupEnd) / (ImpactTime - windupEnd));
            return Quaternion.Slerp(Raised, Impact, u);
        }

        float back = Mathf.SmoothStep(0f, 1f, (t - ImpactTime) / (1f - ImpactTime));
        return Quaternion.Slerp(Impact, Quaternion.identity, back);
    }

    static void Part(Transform parent, string name, Mesh mesh, Material material, System.Action<GameObject> created)
    {
        GameObject part = new GameObject(name);
        Track(created, part);
        part.transform.SetParent(parent, false);
        MeshFilter filter = part.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;
    }

    static void Track(System.Action<GameObject> created, GameObject part)
    {
        if (created != null)
            created(part);
    }

    public struct Palette
    {
        public Material wood;
        public Material grip;
        public Material wrap;
        public Material iron;
        public Material darkIron;
    }

    public static Palette RuntimePalette()
    {
        if (runtimePalette.wood != null)
            return runtimePalette;

        ArtMaterials.Set shared = ArtMaterials.Runtime();
        runtimePalette = new Palette
        {
            wood = Make("PickaxeWood", new Color(0.49f, 0.29f, 0.14f), 0.1f, 0f),
            grip = Make("PickaxeGrip", new Color(0.20f, 0.12f, 0.085f), 0.07f, 0f),
            wrap = Make("PickaxeWrap", new Color(0.34f, 0.19f, 0.11f), 0.06f, 0f),
            iron = shared.iron,
            darkIron = shared.graphite
        };
        return runtimePalette;
    }

    static Material Make(string materialName, Color color, float smoothness, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.name = materialName;
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        return material;
    }
}
