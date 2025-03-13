using UnityEngine;

public class FrontCameraLook : MonoBehaviour
{
    public Transform player; // Reference to the player's transform
    public float distance = 5.0f; // Distance from the player
    public float xSpeed = 120.0f; // Speed of the camera rotation around the player
    public float ySpeed = 120.0f; // Speed of the camera rotation around the player
    public float yMinLimit = -20f; // Minimum vertical angle (how far you can look)
    public float yMaxLimit = 80f; // Maximum vertical angle (how far you can look, but the other direction)

    private float xRotation = 0.0f;
    private float y = 0.0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        xRotation = angles.y;
        y = angles.x;

        // Make the rigid body not change rotation
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().freezeRotation = true;
        }
    }

    void LateUpdate()
    {
        if (player)
        {
            // Update the xRotation based on the mouse's X movement, speed, and distance
            xRotation += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
            // Update the y rotation based on the mouse's Y movement and speed
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            // Clamp the y rotation to stay within the specified vertical limits
            y = ClampAngle(y, yMinLimit, yMaxLimit);

            // Create a rotation quaternion based on the updated y and xRotation values
            Quaternion rotation = Quaternion.Euler(y, xRotation, 0);

            // Calculate the new position of the camera based on the rotation and distance from the player
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + player.position;

            // Apply the rotation and position to the camera
            transform.rotation = rotation;
            transform.position = position;

            player.rotation = Quaternion.Euler(0, xRotation, 0); // Rotate the player to face the camera direction
        }
    }

    static float ClampAngle(float angle, float min, float max)
    //make angle stay within 1 full rotation (360º)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
    //...then clamp it before returning
        return Mathf.Clamp(angle, min, max);
    }
}