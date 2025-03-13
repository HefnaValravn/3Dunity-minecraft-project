using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private Camera[] cameras;
    [SerializeField] private KeyCode switchKey = KeyCode.C; // Or use Input System
    private int currentCameraIndex = 0;

    void Start()
    {
        // Disable all cameras except the first one
        for (int i = 1; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Switch camera when key is pressed
        if (Input.GetKeyDown(switchKey))
        {
            // Disable current camera
            cameras[currentCameraIndex].gameObject.SetActive(false);
            
            // Move to next camera (loop back to 0 if we're at the end)
            currentCameraIndex = (currentCameraIndex + 1) % cameras.Length;
            
            // Enable new current camera
            cameras[currentCameraIndex].gameObject.SetActive(true);
        }
    }
}