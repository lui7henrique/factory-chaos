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
    [SerializeField] float sprintSpeed = 8f;

    [Header("Look")]
    [SerializeField] float lookSensitivity = 0.12f;
    [SerializeField] float minPitch = -80f;
    [SerializeField] float maxPitch = 80f;
    [SerializeField] Transform cameraTransform;

    [Header("Gravity")]
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpSpeed = 4.5f;

    public void BindCamera(Transform camera)
    {
        if (camera != null)
            cameraTransform = camera;
    }

    CharacterController controller;
    float pitch;
    float verticalVelocity;
    bool inputEnabled = true;
    Vector3 planarVelocity;
    float groundedUntil = -1f;
    float jumpUntil = -1f;
    const float JumpGrace = 0.1f;

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
        if (!enabled) { planarVelocity = Vector3.zero; jumpUntil = -1f; groundedUntil = -1f; }
    }

    void Update()
    {
        if (GamePauseMenu.IsOpen)
        {
            jumpUntil = -1f;
            return;
        }

        if (!inputEnabled)
        {
            ApplyGravity();
            return;
        }

        Look();
        Move();
    }

    void Look()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        float sensitivity = lookSensitivity * GamePreferences.Sensitivity;
        transform.Rotate(0f, delta.x * sensitivity, 0f);

        pitch = Mathf.Clamp(pitch - delta.y * sensitivity * (GamePreferences.InvertY ? -1f : 1f), minPitch, maxPitch);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move()
    {
        Vector2 input = ReadMoveInput();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -2f;
            groundedUntil = Time.time + JumpGrace;
        }
        if (JumpPressed()) jumpUntil = Time.time + JumpGrace;
        if (Time.time < groundedUntil && Time.time < jumpUntil)
        {
            verticalVelocity = jumpSpeed;
            jumpUntil = groundedUntil = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        float speed = SprintHeld() ? sprintSpeed : moveSpeed;
        float acceleration = input.sqrMagnitude > 0f ? 45f : 65f;
        planarVelocity = Vector3.MoveTowards(planarVelocity, move * speed, acceleration * Time.deltaTime);
        Vector3 velocity = planarVelocity;
        velocity.y = verticalVelocity;
        CollisionFlags flags = controller.Move(velocity * Time.deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
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

    static bool SprintHeld()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
    }

    static bool JumpPressed()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

}
