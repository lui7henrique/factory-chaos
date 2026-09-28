using UnityEngine;

/// <summary>
/// First-person pickaxe. ToolAnchor holds the presentation pose.
/// SwingPivot rotates at the grip. The model has no collider and no rigidbody.
/// </summary>
public class PickaxeVisual : MonoBehaviour
{
    public const float ImpactTime = 0.62f;

    [SerializeField] Transform swingPivot;
    [SerializeField] Vector3 presentationPosition = new Vector3(0.3f, -0.52f, 0.48f);
    [SerializeField] Vector3 presentationEuler = new Vector3(14f, -8f, 0f);
    [SerializeField] float presentationScale = 0.5f;

    static readonly Quaternion Raised = Quaternion.Euler(-48f, 12f, -8f);
    static readonly Quaternion Impact = Quaternion.Euler(70f, -8f, 4f);
    static readonly Quaternion StrikeFacing = Quaternion.Euler(0f, 180f, 0f);

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
        presentationPosition = new Vector3(0.3f, -0.52f, 0.48f);
        presentationEuler = new Vector3(14f, -8f, 0f);
        presentationScale = 0.5f;
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
        Part(visual.transform, "Ring", meshes.ring, palette.ring, created);
        Part(visual.transform, "Socket", meshes.socket, palette.iron, created);
        Part(visual.transform, "Head", meshes.head, palette.iron, created);
        Part(visual.transform, "Peg", meshes.peg, palette.wood, created);
        BuildHand(pivot, palette.skin != null ? palette.skin : Make(new Color(0.82f, 0.58f, 0.42f), 0.08f, 0f), created);
    }

    static void BuildHand(Transform pivot, Material skin, System.Action<GameObject> created)
    {
        Transform existing = pivot.Find("Hand");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        GameObject hand = new GameObject("Hand");
        Track(created, hand);
        hand.transform.SetParent(pivot, false);

        Box(hand.transform, "Palm", new Vector3(0.012f, 0f, -0.05f), Quaternion.identity, new Vector3(0.075f, 0.11f, 0.042f), skin, created);
        for (int i = 0; i < 4; i++)
        {
            float y = -0.038f + i * 0.026f;
            Box(hand.transform, "Knuckle", new Vector3(0.01f, y, -0.018f), Quaternion.identity, new Vector3(0.03f, 0.022f, 0.036f), skin, created);
            Box(hand.transform, "Finger", new Vector3(-0.004f, y, 0.03f), Quaternion.Euler(68f, 0f, 0f), new Vector3(0.026f, 0.02f, 0.05f), skin, created);
        }

        Box(hand.transform, "Thumb", new Vector3(0.052f, -0.02f, -0.008f), Quaternion.Euler(24f, 78f, 36f), new Vector3(0.058f, 0.024f, 0.03f), skin, created);
        Box(hand.transform, "Wrist", new Vector3(0.055f, -0.2f, -0.07f), Quaternion.Euler(16f, -18f, 10f), new Vector3(0.08f, 0.28f, 0.07f), skin, created);
    }

    static void Box(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, System.Action<GameObject> created)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        Track(created, box);
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localRotation = rotation;
        box.transform.localScale = scale;

        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediate(collider);

        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
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
        public Material iron;
        public Material ring;
        public Material skin;
    }

    public static Palette RuntimePalette()
    {
        return new Palette
        {
            wood = Make(new Color(0.588f, 0.376f, 0.224f), 0.12f, 0f),
            grip = Make(new Color(0.286f, 0.192f, 0.153f), 0.08f, 0f),
            iron = Make(new Color(0.659f, 0.706f, 0.769f), 0.18f, 0.12f),
            ring = Make(new Color(0.957f, 0.745f, 0.196f), 0.12f, 0f),
            skin = Make(new Color(0.82f, 0.58f, 0.42f), 0.08f, 0f)
        };
    }

    static Material Make(Color color, float smoothness, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        return material;
    }
}
