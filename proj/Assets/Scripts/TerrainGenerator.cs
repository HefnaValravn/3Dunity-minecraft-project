using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]

    //controls the size/detail level of the noise pattern (small, spiky mountains with higher value or vast, rolling, smooth hills with lower value)
    public float noiseScale = 0.025f;

    //number of Perlin noise layers
    public int octaves = 4;

    //how much each successive octave contributes to final noise value
    public float persistence = 0.5f;

    //how much frequency increases with each octave
    public float lacunarity = 2.0f;
    public int seed = 12345;
    
    [Header("Bedrock Settings")]
    public float bedrockNoiseScale = 0.05f;
    
    // Initialize with a random seed if not set
    private void Awake()
    {
        if (seed == 0)
        {
            seed = Random.Range(1, 100000);
            Debug.Log($"Using random seed: {seed}");
        }
    }
    
    // Get terrain height at any world position
    public int GetTerrainHeight(float worldX, float worldZ)
    {
        // Generate terrain height using fBm Perlin noise
        float height = GeneratefBm(worldX, worldZ, noiseScale, octaves, persistence, lacunarity);
        int terrainHeight = Mathf.FloorToInt(height * 30) + 50; // Scale & offset height
        
        // Clamp to valid range
        return Mathf.Clamp(terrainHeight, GetBedrockHeight(worldX, worldZ) + 1, Chunk.CHUNK_SIZE_Y - 1);
    }
    
    // Get bedrock height at any world position
    public int GetBedrockHeight(float worldX, float worldZ)
    {
        // Use perlin noise with a different scale and offset for bedrock variation
        float offsetX = seed * 0.01f;
        float offsetZ = seed * 0.01f;
        float bedrockNoise = Mathf.PerlinNoise((worldX + offsetX) * bedrockNoiseScale, (worldZ + offsetZ) * bedrockNoiseScale);
        
        // Map noise to bedrock depth (1-3)
        if (bedrockNoise < 0.2f) return 1;
        if (bedrockNoise < 0.8f) return 2;
        return 3;
    }
    
    // fBm function for smooth terrain variation
    private float GeneratefBm(float x, float z, float scale, int octaves, float persistence, float lacunarity)
    {
        float total = 0;
        float frequency = scale;
        float amplitude = 1;
        float maxValue = 0;
        
        // Offset coordinates by seed for different terrain patterns
        float offsetX = seed * 0.01f;
        float offsetZ = seed * 0.01f;

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise((x + offsetX) * frequency, (z + offsetZ) * frequency) * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }
}