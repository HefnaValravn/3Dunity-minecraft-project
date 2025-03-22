using System.Collections;
using UnityEngine;

public class RandomMusicPlayer : MonoBehaviour
{
    public AudioSource audioSource;   // Audio Source to play music
    private AudioClip[] musicTracks;  // Array of music tracks

    void Start()
    {
        // Load all music tracks from "Resources/Music" folder
        musicTracks = Resources.LoadAll<AudioClip>("Music");

        if (musicTracks.Length > 0)
        {
            StartCoroutine(PlayRandomMusicLoop());
        }
        else
        {
            Debug.LogWarning("No music tracks found in Resources/Music!");
        }
    }

    IEnumerator PlayRandomMusicLoop()
    {
        while (true) // Infinite loop
        {
            // Pick a random song
            AudioClip randomSong = musicTracks[Random.Range(0, musicTracks.Length)];
            audioSource.clip = randomSong;
            audioSource.Play();

            // Wait for the song to finish
            yield return new WaitForSeconds(randomSong.length);

            // Wait a random time (10-20 seconds) before playing next song
            float randomDelay = Random.Range(10f, 20f);
            yield return new WaitForSeconds(randomDelay);
        }
    }
}
