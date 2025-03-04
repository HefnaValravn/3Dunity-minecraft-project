using UnityEngine;

public class FrontCameraLook : MonoBehaviour
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

        // Set the camera's position around the player
        Vector3 direction = new Vector3(0, 0, -distanceFromPlayer);
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation + 180f, 0f);

        transform.position = player.position + rotation * direction;
        transform.LookAt(player.position); // Look at the player's center

        // Rotate player *only horizontally* (yaw), keeping movement flat
        player.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // Apply vertical rotation *only for visuals*, not movement
        player.localRotation = Quaternion.Euler(-xRotation, yRotation, 0f);
    }
}
