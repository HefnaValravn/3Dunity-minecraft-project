// CameraLook.cs - Attach to the head object
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 1f;
    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Read mouse movement, adapted to use time so it's standardized independently of the amount of frames
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;

        // Handle both vertical and horizontal rotation on the head
        xRotation -= mouseDelta.y;
        yRotation += mouseDelta.x;
        //prevent looking up or down too much
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply both rotations to the head
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}