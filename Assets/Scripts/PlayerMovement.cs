using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Prototype first-person movement. WASD moves relative to facing, the mouse looks around,
/// and gravity is applied through CharacterController.Move.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;

    [Header("Look")]
    [SerializeField] float lookSensitivity = 0.12f;
    [SerializeField] float minPitch = -80f;
    [SerializeField] float maxPitch = 80f;
    [SerializeField] Transform cameraTransform;

    [Header("Gravity")]
    [SerializeField] float gravity = -20f;

    CharacterController controller;
    float pitch;
    float verticalVelocity;
    bool inputEnabled = true;
    int ignoreEscapeFrame = -1;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void IgnoreEscapeThisFrame()
    {
        ignoreEscapeFrame = Time.frameCount;
    }

    void Update()
    {
        if (!inputEnabled)
        {
            ApplyGravity();
            return;
        }

        if (Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame
            && ignoreEscapeFrame != Time.frameCount)
            ToggleCursor();

        Look();
        Move();
    }

    void Look()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        transform.Rotate(0f, delta.x * lookSensitivity, 0f);

        pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move()
    {
        Vector2 input = ReadMoveInput();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    static Vector2 ReadMoveInput()
    {
        if (Keyboard.current == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;
        return new Vector2(x, y);
    }

    void ToggleCursor()
    {
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }
}
