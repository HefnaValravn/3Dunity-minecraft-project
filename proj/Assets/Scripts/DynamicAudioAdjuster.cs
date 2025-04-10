using UnityEngine;

public class DynamicAudioAdjuster : MonoBehaviour
{
    private AudioSource audioSource;
    private Transform playerTransform;
    private Vector3 portalPosition;
    private float maxDistance = 80; // Maximum distance for audio to fade out

    public void Initialize(AudioSource source, Vector3 position)
    {
        audioSource = source;
        portalPosition = position;

        // Find the player transform
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError("Player object not found. Ensure the player has the 'Player' tag.");
        }
    }

    private void Update()
    {
        if (playerTransform == null || audioSource == null)
            return;

        // Calculate distance between player and portal
        float distance = Vector3.Distance(playerTransform.position, portalPosition);

        // Adjust volume based on distance
        float volume = distance > maxDistance ? 0 : (distance <= 60 ? 1 : Mathf.Clamp01(1 - ((distance - 60) / (maxDistance - 60))));
        audioSource.volume = volume;

        // Debug log for troubleshooting
        Debug.Log($"Distance to portal: {distance}, Volume: {audioSource.volume}");
    }
}