using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fixed cannon. E operates it, the mouse aims, and left click fires a loaded round.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CannonController : MonoBehaviour
{
    [Header("Aim")]
    [SerializeField] Transform yawPivot;
    [SerializeField] Transform pitchPivot;
    [SerializeField] Transform muzzle;
    [SerializeField] Transform sight;
    [SerializeField] float lookSensitivity = 0.12f;
    [SerializeField] float yawLimit = 60f;
    [SerializeField] float minPitch = -12f;
    [SerializeField] float maxPitch = 28f;
    [SerializeField] float interactRange = 3.5f;

    [Header("Ammo")]
    [SerializeField] int capacity = 3;

    [Header("Shot")]
    [SerializeField] float damage = 20f;
    [SerializeField] float projectileSpeed = 22f;
    [SerializeField] float projectileLifetime = 3f;
    [SerializeField] Material projectileMaterial;

    PlayerMovement playerMovement;
    PlayerCarry playerCarry;
    Collider[] cannonColliders;
    Transform cameraTransform;
    Transform savedParent;
    Vector3 savedLocalPosition;
    Quaternion savedLocalRotation;
    float yaw;
    float pitch;
    int rounds;
    bool operating;
    bool showPrompt;
    float emptyUntil;
    GUIStyle labelStyle;

    public void Configure(Transform yaw, Transform pitch, Transform muzzlePoint, Transform sightPoint, Material shotMaterial)
    {
        yawPivot = yaw;
        pitchPivot = pitch;
        muzzle = muzzlePoint;
        sight = sightPoint;
        if (shotMaterial != null)
            projectileMaterial = shotMaterial;
    }

    public int Rounds => rounds;
    public int Capacity => capacity;
    public bool IsFull => rounds >= capacity;

    void Awake()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        playerCarry = FindAnyObjectByType<PlayerCarry>();
        cannonColliders = GetComponentsInChildren<Collider>(true);
    }

    void OnDisable()
    {
        if (operating)
            Exit();
    }

    public bool TryAddRound()
    {
        if (rounds >= capacity)
            return false;

        rounds++;
        return true;
    }

    void Update()
    {
        if (operating)
        {
            Aim();

            if (Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame
                && Cursor.lockState == CursorLockMode.Locked)
                Fire();

            if (ExitPressed())
                Exit();

            return;
        }

        showPrompt = CanOperate();
        if (showPrompt && InteractPressed())
            Enter();
    }

    void Enter()
    {
        if (playerCarry != null && playerCarry.IsHolding)
            return;

        Camera main = Camera.main;
        if (main == null || sight == null)
            return;

        cameraTransform = main.transform;
        savedParent = cameraTransform.parent;
        savedLocalPosition = cameraTransform.localPosition;
        savedLocalRotation = cameraTransform.localRotation;

        if (playerMovement != null)
            playerMovement.SetInputEnabled(false);
        if (playerCarry != null)
            playerCarry.SetInputEnabled(false);

        cameraTransform.SetParent(sight, false);
        cameraTransform.localPosition = Vector3.zero;
        cameraTransform.localRotation = Quaternion.identity;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        operating = true;
        showPrompt = false;
    }

    void Exit()
    {
        operating = false;

        if (cameraTransform != null)
        {
            cameraTransform.SetParent(savedParent, false);
            cameraTransform.localPosition = savedLocalPosition;
            cameraTransform.localRotation = savedLocalRotation;
            cameraTransform = null;
        }

        if (playerCarry != null)
        {
            playerCarry.IgnoreInteractThisFrame();
            playerCarry.SetInputEnabled(true);
        }

        if (playerMovement != null)
        {
            playerMovement.IgnoreEscapeThisFrame();
            playerMovement.SetInputEnabled(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Aim()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw = Mathf.Clamp(yaw + delta.x * lookSensitivity, -yawLimit, yawLimit);
        pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);

        if (yawPivot != null)
            yawPivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (pitchPivot != null)
            pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Fire()
    {
        if (rounds <= 0 || muzzle == null)
        {
            emptyUntil = Time.time + 1.4f;
            return;
        }

        rounds--;

        GameObject shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shot.name = "Projectile";
        shot.transform.SetPositionAndRotation(muzzle.position + muzzle.forward * 0.25f, muzzle.rotation);
        shot.transform.localScale = Vector3.one * 0.22f;

        MeshRenderer renderer = shot.GetComponent<MeshRenderer>();
        if (renderer != null && projectileMaterial != null)
            renderer.sharedMaterial = projectileMaterial;

        shot.AddComponent<Rigidbody>();
        Projectile projectile = shot.AddComponent<Projectile>();
        projectile.Launch(muzzle.forward * projectileSpeed, damage, projectileLifetime, cannonColliders);

        CannonVisualFx visual = GetComponent<CannonVisualFx>();
        if (visual != null)
            visual.PlayRecoil();
    }

    bool CanOperate()
    {
        if (playerCarry != null && playerCarry.IsHolding)
            return false;

        Camera main = Camera.main;
        if (main == null)
            return false;

        Ray ray = new Ray(main.transform.position, main.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider != null && hit.collider.GetComponentInParent<CannonController>() == this;
    }

    static bool InteractPressed()
    {
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }

    static bool ExitPressed()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 20;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
        }

        float width = 420f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - 88f;

        if (operating)
        {
            GUI.Label(new Rect(x, y, width, 28f), $"Munição: {rounds}/{capacity}", labelStyle);
            GUI.Label(new Rect(x, y + 26f, width, 28f), "E ou Esc — sair", labelStyle);
            if (Time.time < emptyUntil)
                GUI.Label(new Rect(x, y - 28f, width, 28f), "Sem munição", labelStyle);
        }
        else if (showPrompt)
        {
            GUI.Label(new Rect(x, y, width, 28f), "E — operar", labelStyle);
        }
        else if (Time.time < emptyUntil)
        {
            GUI.Label(new Rect(x, y, width, 28f), "Sem munição", labelStyle);
        }
    }
}
