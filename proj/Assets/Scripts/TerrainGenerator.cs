using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]

    //controls the size/detail level of the noise pattern (small, spiky mountains with higher value or vast, rolling, smooth hills with lower value)
    public float noiseScale = 0.012f;

    //number of Perlin noise layers
    public int octaves = 6;

    //how much each successive octave contributes to final noise value
    public float persistence = 0.5f;

    //how much frequency increases with each octave
    public float lacunarity = 2.1f;
    public int seed = 0;



    [Header("Landmark Settings")]
    public float biomeNoiseScale = 0.003f; // Very low frequency for large biome regions
    public float plainsFlatness = 0.8f; // How flat plains should be (0-1)


    [Header("Mountain Settings")]
    public float mountainsHeight = 2.5f; // Height multiplier for mountains
    public float mountainsRoughness = 2.2f; // Additional octaves for mountains
    public float maxPeakHeight = 0.6f; 



    [Header("Bedrock Settings")]
    public float bedrockNoiseScale = 0.05f;




    //WARNING: THESE ARE THE ONLY PARAMETERS THAT SEEM TO WORK TO MAKE GOOD, BUT NOT TOO ABUNDANT, CAVES. DO NOT CHANGE caveNoiseScale AND caveDensityThreshold
    [Header("Cave Settings")]
    public float caveNoiseScale = 0.1f; //controls size of caves. larger value, smaller cave, and viceversa
    public float caveDensityThreshold = 0.35f; // controls how many caves to generate. larger value, less caves, and viceversa
    public int caveOctaves = 2; // controls complexity/detail of caves
    public float cavePersistence = 0.5f;
    public float caveLacunarity = 2.0f; // these two control roughness and variation in cave shapes
    public int caveSeed = 0; // Different seed for caves


    [Header("Surface Cave Entrance Settings")]
    public float caveEntranceThreshold = 0.75f;


    [Header("Portal Settings")]
    public int portalSeed = 0;


    // Initialize with a random seed if not set
    private void Awake()
    {
        if (seed == 0)
        {
            seed = Random.Range(1, 100000);
            Debug.Log($"Using random seed: {seed}");
        }

        if (caveSeed == 0)
        {
            caveSeed = Random.Range(1, 100000);
            Debug.Log($"Using random cave seed: {caveSeed}");
        }

        if (portalSeed == 0)
        {
            portalSeed = Random.Range(1, 100000);
            Debug.Log($"Using random portal seed:  {portalSeed}");
        }
    }

    public bool ShouldGeneratePortal(int2 chunkCoordinate)
    {
        // Generate portal at every multiple of 10 chunks
        return chunkCoordinate.x % 10 == 0 &&
               chunkCoordinate.y % 10 == 0;
    }


    private float BiomeBlendFactor(float biomeValue, float threshold, float blendRange = 0.1f)
    {
        if (biomeValue < threshold - blendRange)
            return 0;
        if (biomeValue > threshold + blendRange)
            return 1;

        // Smooth transition in the blend range
        return (biomeValue - (threshold - blendRange)) / (blendRange * 2);
    }


    // Get terrain height at any world position
    public int GetTerrainHeight(float worldX, float worldZ)
    {
        // Determine biome type using low-frequency noise
        //in this context, biome = hill, plain or mountain
        float biomeValue = Mathf.PerlinNoise(
            (worldX + seed * 0.3f) * biomeNoiseScale,
            (worldZ + seed * 0.3f) * biomeNoiseScale
        );

        // Generate base terrain height
        float baseHeight = GeneratefBm(worldX, worldZ, noiseScale, octaves, persistence, lacunarity);

        // Calculate mountains and plains features
        float plainHeight = Mathf.Lerp(baseHeight, 0.5f, plainsFlatness);

        // Mountain features with reduced extreme peaks
        float extraDetail = GeneratefBm(
            worldX, worldZ,
            noiseScale * 2f,
            Mathf.Min(octaves + 2, 8),
            persistence * 0.8f,
            lacunarity * 1.2f
        ) * 0.3f;

        float peakNoise = Mathf.PerlinNoise(
            (worldX + seed * 0.7f) * noiseScale * 4f,
            (worldZ + seed * 0.7f) * noiseScale * 4f
        );

        // Smoother peak calculation with clamping to prevent extreme heights
        float peakHeight = 0;
        if (peakNoise > 0.8f)
        {
            float peakFactor = Mathf.Pow(peakNoise - 0.8f, 1.5f) * 5f;
            peakHeight = Mathf.Min(peakFactor, maxPeakHeight); // Cap peak height
        }

        float mountainHeight = baseHeight * mountainsHeight + extraDetail + peakHeight;

        // Blend between biomes using smooth transitions
        float plainBlend = BiomeBlendFactor(0.2f - biomeValue, 0, 0.1f); // Plains blend
        float mountainBlend = BiomeBlendFactor(biomeValue - 0.75f, 0, 0.15f); // Mountain blend with wider transition

        // Calculate normal hill height for transitional areas
        float normalHeight = baseHeight;

        // Final height based on blended biomes
        float modifiedHeight;

        if (plainBlend > 0 && mountainBlend > 0)
        {
            // Rare case: both plains and mountains influence (at biome boundaries)
            float totalBlend = plainBlend + mountainBlend;
            modifiedHeight = (plainHeight * plainBlend + mountainHeight * mountainBlend) / totalBlend;
        }
        else if (plainBlend > 0)
        {
            // Transition between plains and normal hills
            modifiedHeight = Mathf.Lerp(normalHeight, plainHeight, plainBlend);
        }
        else if (mountainBlend > 0)
        {
            // Transition between normal hills and mountains
            modifiedHeight = Mathf.Lerp(normalHeight, mountainHeight, mountainBlend);
        }
        else
        {
            // Normal hills
            modifiedHeight = normalHeight;
        }

        int terrainHeight = Mathf.FloorToInt(modifiedHeight * 30) + 50;

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


    public bool IsSurfaceCaveEntrance(float worldX, float worldZ)
    {
        //use perlin noise to determine if this spot should have a cave entrance
        float entranceNoise = Mathf.PerlinNoise((worldX + seed * 0.05f) * 0.02f, (worldZ + seed * 0.05f) * 0.02f);

        //KEEP AROUND THIS VALUE; 0.9 IS WAY TOO HIGH FOR ANYTHING TO SPAWN
        return entranceNoise > caveEntranceThreshold;
    }


    // Determine if a block should be a cave
    public bool IsCaveBlock(float worldX, float worldY, float worldZ)
    {
        int terrainHeight = GetTerrainHeight(worldX, worldZ);

        // Don't generate caves in bedrock layer
        if (worldY <= GetBedrockHeight(worldX, worldZ))
            return false;

        bool canBeSurfaceEntrance = IsSurfaceCaveEntrance(worldX, worldZ);

        // If we're near the surface (within 3 blocks)...
        if (worldY >= terrainHeight - 3)
        {
            // Only allow cave blocks if this is a designated entrance location
            if (!canBeSurfaceEntrance)
                return false;
        }



        // Use 3D Perlin noise for cave generation
        float caveNoise = Generate3DfBm(worldX, worldY, worldZ, caveNoiseScale, caveOctaves, cavePersistence, caveLacunarity);

        // Make caves narrower near the top for more natural shapes (circular-ish)
        //the float after terrainHeight determines how far surface cave generation goes before
        //normal caves start
        if (canBeSurfaceEntrance && worldY >= terrainHeight - 10)
        {
            // Calculate how close we are to surface
            float surfaceProximity = (worldY - (terrainHeight - 10)) / 10f;

            // If close to surface, make it harder to form (therefore making caves narrower near the top)
            //playing with the first float in this part helps with cave shape
            float adjustedThreshold = caveDensityThreshold - (0.05f * (1f - surfaceProximity));

            return caveNoise < adjustedThreshold;
        }

        // Else, just use normal threshold
        // If noise value is below threshold, this is a cave
        return caveNoise < caveDensityThreshold;
    }

    // 3D fBm for cave generation
    private float Generate3DfBm(float x, float y, float z, float scale, int octaves, float persistence, float lacunarity)
    {
        float total = 0;
        float frequency = scale;
        float amplitude = 1;
        float maxValue = 0;

        // Use a different seed for caves
        float offsetX = caveSeed * 0.01f;
        float offsetY = caveSeed * 0.02f;
        float offsetZ = caveSeed * 0.01f;

        for (int i = 0; i < octaves; i++)
        {
            // For 3D noise, we combine multiple 2D noise samples
            float xy = Mathf.PerlinNoise((x + offsetX) * frequency, (y + offsetY) * frequency);
            float yz = Mathf.PerlinNoise((y + offsetY) * frequency, (z + offsetZ) * frequency);
            float xz = Mathf.PerlinNoise((x + offsetX) * frequency, (z + offsetZ) * frequency);

            // Average the noise samples for a pseudo-3D effect
            float noise = (xy + yz + xz) / 3f;

            total += noise * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }
}