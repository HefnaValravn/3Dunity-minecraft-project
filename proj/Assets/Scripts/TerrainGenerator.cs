using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

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




    //WARNING: THESE ARE THE ONLY PARAMETERS THAT SEEM TO WORK TO MAKE GOOD, BUT NOT TOO ABUNDANT, CAVES. DO NOT CHANGE caveNoiseScale AND caveDensityThreshold
    [Header("Cave Settings")]
    public float caveNoiseScale = 0.1f; //controls size of caves. larger value, smaller cave, and viceversa
    public float caveDensityThreshold = 0.35f; // controls how many caves to generate. larger value, less caves, and viceversa
    public int caveOctaves = 2; // controls complexity/detail of caves
    public float cavePersistence = 0.5f;
    public float caveLacunarity = 2.0f; // these two control roughness and variation in cave shapes
    public int caveSeed = 54321; // Different seed for caves


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

    public Vector2 GetPortalLocationInChunk(int2 chunkCoordinate)
    {
        for (int x = 4; x < Chunk.CHUNK_SIZE_X - 4; x++)
        {
            for (int z = 4; z < Chunk.CHUNK_SIZE_Z - 4; z++)
            {
            int terrainHeight = GetTerrainHeight(chunkCoordinate.x * Chunk.CHUNK_SIZE_X + x, chunkCoordinate.y * Chunk.CHUNK_SIZE_Z + z);

            bool isFlat = true;
            bool isAboveGround = true;

            // Check a larger 7x7 area around the portal location
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dz = -3; dz <= 3; dz++)
                {
                int neighborHeight = GetTerrainHeight(chunkCoordinate.x * Chunk.CHUNK_SIZE_X + x + dx, chunkCoordinate.y * Chunk.CHUNK_SIZE_Z + z + dz);
                if (Mathf.Abs(terrainHeight - neighborHeight) > 0) // Tighten height difference threshold to 0
                {
                    isFlat = false;
                    break;
                }
                }
                if (!isFlat)
                break;
            }

            // Ensure the portal base is not inside the ground
            if (terrainHeight < Chunk.CHUNK_SIZE_Y - 1 && terrainHeight > GetBedrockHeight(chunkCoordinate.x * Chunk.CHUNK_SIZE_X + x, chunkCoordinate.y * Chunk.CHUNK_SIZE_Z + z) + 1)
            {
                isAboveGround = true;
            }
            else
            {
                isAboveGround = false;
            }

            if (isFlat && isAboveGround)
            {
                return new Vector2(x, z);
            }
            }
        }

        // Fallback to center if no flat surface is found
        return new Vector2(Chunk.CHUNK_SIZE_X / 2, Chunk.CHUNK_SIZE_Z / 2);
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