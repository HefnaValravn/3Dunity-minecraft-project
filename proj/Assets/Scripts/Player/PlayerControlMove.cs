using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Transform bodyTransform; // Reference to the body object
    private Transform headTransform; //head child for aligning rotation with it
    private Rigidbody rb; //player's own rigidbody
    private Vector3 lastHeadForward; //Store last head forward direction

    void Start()
    {
        headTransform = transform.Find("head");  // Assuming "head" is a direct child of the player object
        rb = GetComponent<Rigidbody>();           // Get the Rigidbody component on the player

        // Configure rigidbody for character control
        rb.freezeRotation = true; // Prevent physics from rotating the player

        // Initialize last head forward
        lastHeadForward = headTransform.forward;
        lastHeadForward.y = 0;
        lastHeadForward.Normalize();
    }

    void FixedUpdate() // Use FixedUpdate for Rigidbody movement
    {
        float moveX = 0f;
        float moveZ = 0f;
        float moveY = 0f;

        // Get input from the keyboard for movement
        if (Keyboard.current.wKey.isPressed) moveZ = 1f;  // Forward
        if (Keyboard.current.sKey.isPressed) moveZ = -1f; // Backward
        if (Keyboard.current.aKey.isPressed) moveX = -1f; // Left
        if (Keyboard.current.dKey.isPressed) moveX = 1f;  // Right
        if (Keyboard.current.spaceKey.isPressed) moveY = 1f; // Up
        if (Keyboard.current.leftCtrlKey.isPressed) moveY = -1f; // Down

        // Check if any movement key is pressed
        bool isMoving = moveX != 0f || moveZ != 0f || moveY != 0f;

        // Get movement direction based on head's forward direction
        Vector3 forward = headTransform.forward;
        Vector3 right = headTransform.right;
        forward.y = 0; // Ensure no vertical movement
        right.y = 0;   // Ensure no vertical movement

        // Only update rotation if the head direction has changed significantly
        if (Vector3.Angle(forward.normalized, lastHeadForward) > 0.5f)
        {
            float headYRotation = headTransform.eulerAngles.y;
            bodyTransform.rotation = Quaternion.Euler(0f, headYRotation + 90f, 0f);
            lastHeadForward = forward.normalized;
        }

        if (isMoving)
        {
            // Normalize direction vectors
            forward.Normalize();
            right.Normalize();

            // Calculate movement direction
            Vector3 moveDirection = (right * moveX + forward * moveZ).normalized;
            moveDirection.y = moveY;

            // Apply movement using Rigidbody
            rb.linearVelocity = new Vector3(moveDirection.x * speed, moveDirection.y * speed, moveDirection.z * speed);
        }
        else
        {
            // Halt horizontal movement but preserve vertical velocity
            rb.linearVelocity = new Vector3(0f, 0f, 0f);
        }
    }
}