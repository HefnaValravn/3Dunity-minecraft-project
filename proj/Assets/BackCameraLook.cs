using UnityEngine;

public class BackCameraLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 50f;
    public Transform player; // The player object
    public float distanceFromPlayer = 3f;

    private float xRotation = 0f; // Pitch (up/down)
    private float yRotation = 0f; // Yaw (left/right)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Update rotation values
        xRotation += mouseY;
        xRotation = Mathf.Clamp(xRotation, -30f, 45f); // Prevent extreme looking up/down
        yRotation += mouseX;

        // Set the camera's position behind the player
        Vector3 direction = new Vector3(0, 0, distanceFromPlayer); // Distance from the player, behind
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // Set the camera's position and make it look at the player
        transform.position = player.position - rotation * direction; // Behind the player
        transform.LookAt(player.position); // Look at the player's center

        // Apply horizontal rotation (yaw) to the player to make it follow the camera
        player.rotation = Quaternion.Euler(0f, yRotation, 0f);
        
        // Apply vertical rotation (pitch) to the player’s upper body if needed (for head/torso tilt)
        player.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
