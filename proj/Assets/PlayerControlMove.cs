using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Transform bodyTransform; // Reference to the body object
    private Transform headTransform; //head child for aligning rotation with it
    private Rigidbody rb; //player's own rigidbody
    
    void Start()
    {
        headTransform = transform.Find("head");  // Assuming "head" is a direct child of the player object
        rb = GetComponent<Rigidbody>();           // Get the Rigidbody component on the player
    }
    
    void FixedUpdate() // Use FixedUpdate for Rigidbody movement
    {
        float moveX = 0f;
        float moveZ = 0f;
        
        // Get input from the keyboard for movement
        if (Keyboard.current.wKey.isPressed) moveZ = 1f;  // Forward
        if (Keyboard.current.sKey.isPressed) moveZ = -1f; // Backward
        if (Keyboard.current.aKey.isPressed) moveX = -1f; // Left
        if (Keyboard.current.dKey.isPressed) moveX = 1f;  // Right

        // Get movement direction based on head's forward direction
        Vector3 forward = headTransform.forward;
        Vector3 right = headTransform.right;
        forward.y = 0; // Ensure no vertical movement
        right.y = 0;   // Ensure no vertical movement
        forward.Normalize();
        right.Normalize();
        
        // Calculate movement direction
        Vector3 moveDirection = (right * moveX + forward * moveZ).normalized;
        
        // Apply movement using Rigidbody
        rb.linearVelocity = new Vector3(moveDirection.x * speed, rb.linearVelocity.y, moveDirection.z * speed);
        
        // Update body rotation to match head's horizontal rotation only
        // Extract only the Y rotation from the head
        float headYRotation = headTransform.eulerAngles.y;
        
        // Set the body rotation to only rotate on Y axis
        bodyTransform.rotation = Quaternion.Euler(0f, headYRotation + 90f, 0f);
    }
}